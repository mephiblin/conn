using Conn.Core.Scenes;
using Conn.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Conn.Editor.UI
{
    public static class RuntimeUiPrefabBuilder
    {
        public const string PrefabFolder = "Assets/Conn/UI/Prefabs";
        public const string RuntimeCanvasPrefabPath = PrefabFolder + "/RuntimeCanvas.prefab";
        public const string TitleBackgroundSpritePath = "Assets/Conn/UI/Art/title_background_v1.png";
        public const string NpcBackgroundBlacksmithPath = "Assets/Conn/2D/NPC_Background/대장간.png";
        public const string NpcBackgroundSkillMerchantPath = "Assets/Conn/2D/NPC_Background/스킬마차.png";
        public const string NpcBackgroundInnPath = "Assets/Conn/2D/NPC_Background/여관.png";
        public const string NpcBackgroundApothecaryPath = "Assets/Conn/2D/NPC_Background/약초상점.png";
        public const string NpcBackgroundScholarPath = "Assets/Conn/2D/NPC_Background/학자의방.png";

        public static GameObject InstantiateRuntimeCanvasPrefab(GameSceneId sceneId)
        {
            EnsureRuntimeCanvasPrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeCanvasPrefabPath);
            if (prefab == null)
            {
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = RuntimeCanvasUiBuilder.CanvasName;
            ApplyScenePanelVisibility(instance.transform, sceneId);
            return instance;
        }

        public static void EnsureRuntimeCanvasPrefab()
        {
            EnsureFolder("Assets/Conn");
            EnsureFolder("Assets/Conn/UI");
            EnsureFolder(PrefabFolder);

            var root = new GameObject(RuntimeCanvasUiBuilder.CanvasName);
            try
            {
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;

                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = RuntimeCanvasUiBuilder.ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                root.AddComponent<GraphicRaycaster>();

                AddTitleBackground(root.transform);
                AddPanels(root.transform, RuntimeCanvasUiBuilder.CommonPanelNames);
                AddPanels(root.transform, RuntimeCanvasUiBuilder.TitlePanelNames);
                AddPanels(root.transform, RuntimeCanvasUiBuilder.TownPanelNames);
                AddPanels(root.transform, RuntimeCanvasUiBuilder.DungeonPanelNames);
                AddPanels(root.transform, RuntimeCanvasUiBuilder.CombatPanelNames);
                AddPanels(root.transform, RuntimeCanvasUiBuilder.EndingPanelNames);

                PrefabUtility.SaveAsPrefabAsset(root, RuntimeCanvasPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AddPanels(Transform root, string[] panelNames)
        {
            for (var i = 0; i < panelNames.Length; i++)
            {
                if (root.Find(panelNames[i]) != null)
                {
                    continue;
                }

                var panel = new GameObject(panelNames[i], typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(VerticalLayoutGroup));
                panel.transform.SetParent(root, false);

                var rect = (RectTransform)panel.transform;
                ApplyNormalizedRect(rect, RuntimeCanvasUiBuilder.NormalizedSafeRectForPanel(panelNames[i]));

                var group = panel.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                var image = panel.GetComponent<Image>();
                image.color = InitialPanelColor(panelNames[i]);

                var layout = panel.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(12, 12, 10, 10);
                layout.spacing = 6f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
            }
        }

        private static Color InitialPanelColor(string panelName)
        {
            if (panelName == "TownNpcBackdropPanel")
            {
                return new Color(0.015f, 0.012f, 0.01f, 0.96f);
            }

            if (panelName == "TownNpcStandingCgPanel")
            {
                return new Color(0.08f, 0.08f, 0.1f, 0.76f);
            }

            return new Color(0.04f, 0.05f, 0.07f, 0.84f);
        }

        private static void AddTitleBackground(Transform root)
        {
            var background = new GameObject("TitleBackground", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            background.transform.SetParent(root, false);
            background.transform.SetAsFirstSibling();

            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var group = background.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var image = background.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = LoadTitleBackgroundSprite();
        }

        public static void ConfigureRuntimeCanvasUiSprites(RuntimeCanvasUi ui)
        {
            if (ui == null)
            {
                return;
            }

            ui.ConfigureNpcBackgroundSprites(
                LoadSprite(NpcBackgroundBlacksmithPath),
                LoadSprite(NpcBackgroundSkillMerchantPath),
                LoadSprite(NpcBackgroundInnPath),
                LoadSprite(NpcBackgroundApothecaryPath),
                LoadSprite(NpcBackgroundScholarPath));
        }

        public static Sprite LoadSprite(string spritePath)
        {
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                var changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.BottomCenter)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                    importer.SetTextureSettings(settings);
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        public static Texture2D LoadTexture(string texturePath)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                var changed = false;
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        private static Sprite LoadTitleBackgroundSprite()
        {
            return LoadSprite(TitleBackgroundSpritePath);
        }

        private static void ApplyScenePanelVisibility(Transform root, GameSceneId sceneId)
        {
            var titleBackground = root.Find("TitleBackground");
            if (titleBackground != null)
            {
                var group = titleBackground.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = sceneId == GameSceneId.Title ? 1f : 0f;
                }

                titleBackground.gameObject.SetActive(sceneId == GameSceneId.Title);
            }

            SetPanelSetActive(root, RuntimeCanvasUiBuilder.TitlePanelNames, sceneId == GameSceneId.Title);
            SetPanelSetActive(root, RuntimeCanvasUiBuilder.TownPanelNames, sceneId == GameSceneId.Town);
            SetPanelSetActive(root, RuntimeCanvasUiBuilder.DungeonPanelNames, sceneId == GameSceneId.Dungeon);
            SetPanelSetActive(root, RuntimeCanvasUiBuilder.CombatPanelNames, sceneId == GameSceneId.Combat);
            SetPanelSetActive(root, RuntimeCanvasUiBuilder.EndingPanelNames, sceneId == GameSceneId.Ending);
        }

        private static void SetPanelSetActive(Transform root, string[] panelNames, bool active)
        {
            for (var i = 0; i < panelNames.Length; i++)
            {
                var child = root.Find(panelNames[i]);
                if (child != null)
                {
                    child.gameObject.SetActive(active);
                }
            }
        }

        private static void ApplyNormalizedRect(RectTransform rect, Rect normalized)
        {
            rect.anchorMin = new Vector2(normalized.xMin, normalized.yMin);
            rect.anchorMax = new Vector2(normalized.xMax, normalized.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}

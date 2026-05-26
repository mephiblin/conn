using Conn.Core.Scenes;
using Conn.Editor.Maps;
using Conn.Editor.Content;
using Conn.Editor.UI;
using Conn.Editor.World;
using Conn.Rendering.Interaction;
using Conn.Rendering.Player;
using Conn.Runtime.Scenes;
using Conn.Runtime.World;
using Conn.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Conn.Editor.Tools
{
    public static class P0SceneBuilder
    {
        private const string SceneFolder = "Assets/Conn/Scenes";
        private const string QuestBoardSpritePath = "Assets/Conn/2D/NPC_2D/게시판.png";
        private const string GateSpritePath = "Assets/Conn/2D/NPC_2D/gate.png";
        private const string BlacksmithSpritePath = "Assets/Conn/2D/NPC_2D/대장장이.png";
        private const string SkillMerchantSpritePath = "Assets/Conn/2D/NPC_2D/스킬상인.png";
        private const string InnSpritePath = "Assets/Conn/2D/NPC_2D/여관주인.png";
        private const string ApothecarySpritePath = "Assets/Conn/2D/NPC_2D/약재상.png";
        private const string ScholarSpritePath = "Assets/Conn/2D/NPC_2D/학자.png";

        private static readonly TownNpcDefinition[] TownNpcDefinitions =
        {
            new TownNpcDefinition(
                "Quest Board",
                typeof(QuestBoardInteractable),
                new Vector3(-2f, 0f, 2f),
                QuestBoardSpritePath,
                new Color(0.55f, 0.44f, 0.28f, 1f),
                1.4f,
                false,
                new Vector3(1.5f, 1.4f, 0.25f),
                new Vector3(0f, 1f, 0f)),
            new TownNpcDefinition(
                "Dungeon Gate",
                typeof(GateInteractable),
                new Vector3(2f, 0f, 2f),
                GateSpritePath,
                new Color(0.35f, 0.36f, 0.42f, 1f),
                3.8f,
                false,
                new Vector3(1.2f, 2.5f, 0.3f),
                new Vector3(0f, 1.25f, 0f)),
            new TownNpcDefinition(
                "Blacksmith",
                typeof(BlacksmithInteractable),
                new Vector3(0f, 0f, 3.5f),
                BlacksmithSpritePath,
                new Color(0.7f, 0.36f, 0.26f, 1f),
                1.75f,
                true,
                new Vector3(1.4f, 1.3f, 0.5f),
                new Vector3(0f, 1f, 0f)),
            new TownNpcDefinition(
                "Skill Merchant",
                typeof(SkillMerchantInteractable),
                new Vector3(-3.5f, 0f, 0f),
                SkillMerchantSpritePath,
                new Color(0.35f, 0.48f, 0.78f, 1f),
                1.75f,
                true,
                new Vector3(1f, 1.4f, 1f),
                new Vector3(0f, 1f, 0f)),
            new TownNpcDefinition(
                "Inn",
                typeof(TownServiceInteractable),
                new Vector3(3.5f, 0f, 0f),
                InnSpritePath,
                new Color(0.66f, 0.48f, 0.3f, 1f),
                1.75f,
                true,
                new Vector3(1f, 1.3f, 1f),
                new Vector3(0f, 1f, 0f),
                true,
                TownServiceKind.Inn,
                3),
            new TownNpcDefinition(
                "Trainer",
                typeof(TownServiceInteractable),
                new Vector3(3.5f, 0f, -2f),
                string.Empty,
                new Color(0.62f, 0.34f, 0.28f, 1f),
                1.75f,
                true,
                new Vector3(1f, 1.3f, 1f),
                new Vector3(0f, 1f, 0f),
                true,
                TownServiceKind.Trainer,
                5),
            new TownNpcDefinition(
                "Apothecary",
                typeof(TownServiceInteractable),
                new Vector3(-3.5f, 0f, -2f),
                ApothecarySpritePath,
                new Color(0.28f, 0.62f, 0.38f, 1f),
                1.75f,
                true,
                new Vector3(1f, 1.3f, 1f),
                new Vector3(0f, 1f, 0f),
                true,
                TownServiceKind.Apothecary,
                4),
            new TownNpcDefinition(
                "Scholar",
                typeof(TownServiceInteractable),
                new Vector3(0f, 0f, -3.5f),
                ScholarSpritePath,
                new Color(0.48f, 0.42f, 0.72f, 1f),
                1.75f,
                true,
                new Vector3(1f, 1.3f, 1f),
                new Vector3(0f, 1f, 0f),
                true,
                TownServiceKind.Scholar,
                0)
        };

        [MenuItem("Conn/Samples/Rebuild P0 Sample Scenes (Overwrite)")]
        public static void BuildP0Scenes()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild P0 sample scenes?",
                    "This regenerates the P0 sample scenes and can overwrite developer-authored scene layout changes. Use this only for sample recovery or smoke-test fixture generation.",
                    "Rebuild Samples",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new System.OperationCanceledException("P0 scene build canceled because current scene changes were not saved.");
            }

            var restorePath = SceneManager.GetActiveScene().path;
            try
            {
                EnsureFolder("Assets/Conn");
                EnsureFolder(SceneFolder);
                EnsureCompiledMapAsset();
                EnsureRuntimeMapGenerationBundleAsset();
                RuntimeUiPrefabBuilder.EnsureRuntimeCanvasPrefab();
                var townEnvironmentResult = TownEnvironmentPrefabBuilder.EnsureDefaultTownEnvironmentAssets();
                if (townEnvironmentResult.HasErrors)
                {
                    throw new System.InvalidOperationException(string.Join("\n", townEnvironmentResult.Errors));
                }

                CreateScene(GameSceneId.Title, false);
                CreateScene(GameSceneId.Town, true, SceneContent.Town);
                CreateScene(GameSceneId.Dungeon, true, SceneContent.Dungeon);
                CreateScene(GameSceneId.Combat, false);
                CreateScene(GameSceneId.Ending, false);

                EditorBuildSettings.scenes = new[]
                {
                    BuildScene("Title"),
                    BuildScene("Town"),
                    BuildScene("Dungeon"),
                    BuildScene("Combat"),
                    BuildScene("Ending")
                };

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (!string.IsNullOrEmpty(restorePath))
                {
                    EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
                }
            }
        }

        private static EditorBuildSettingsScene BuildScene(string sceneName)
        {
            return new EditorBuildSettingsScene($"{SceneFolder}/{sceneName}.unity", true);
        }

        private static void EnsureCompiledMapAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<Conn.Core.Maps.CompiledMapAsset>(ChapterTwoBuildValidator.DefaultCompiledMapAssetPath) != null)
            {
                return;
            }

            var profile = Conn.Core.Maps.MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = Conn.Core.Maps.MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = Conn.Core.Maps.MapGenerationService.Generate(profile, chunks, Conn.Runtime.Maps.CompiledMapDungeonRuntimeService.DefaultDungeonSeed);
            var compiled = Conn.Core.Maps.MapGenerationService.Compile(profile, draft);
            ChapterTwoBuildValidator.SaveCompiledMapAsset(compiled, Conn.Runtime.Maps.CompiledMapDungeonRuntimeService.DefaultDungeonSeed);
        }

        private static void EnsureRuntimeMapGenerationBundleAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<Conn.Core.Maps.RuntimeMapGenerationBundleAsset>(RuntimeMapGenerationBundleBuilder.DefaultBundleAssetPath) != null)
            {
                return;
            }

            ChapterTwoBuildValidator.SaveRuntimeMapGenerationBundleAsset();
        }

        private static void CreateScene(GameSceneId sceneId, bool includePlayer, SceneContent content = SceneContent.None)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneId.ToString();

            var bootstrapObject = new GameObject("Scene Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<SceneBootstrap>();
            bootstrap.SceneId = sceneId;
            bootstrap.ContentDatabase = AssetDatabase.LoadAssetAtPath<Conn.Core.Content.ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            bootstrap.CompiledMaps = new[]
            {
                AssetDatabase.LoadAssetAtPath<Conn.Core.Maps.CompiledMapAsset>(ChapterTwoBuildValidator.DefaultCompiledMapAssetPath)
            };
            bootstrap.RuntimeMapGenerationBundles = new[]
            {
                AssetDatabase.LoadAssetAtPath<Conn.Core.Maps.RuntimeMapGenerationBundleAsset>(Conn.Editor.Maps.RuntimeMapGenerationBundleBuilder.DefaultBundleAssetPath)
            };

            var overlay = bootstrapObject.AddComponent<P0SceneOverlay>();
            overlay.SceneId = sceneId;
            RuntimeUiPrefabBuilder.InstantiateRuntimeCanvasPrefab(sceneId);
            var runtimeCanvasUi = RuntimeCanvasUiBuilder.EnsureRuntimeCanvas(bootstrapObject, sceneId);
            RuntimeUiPrefabBuilder.ConfigureRuntimeCanvasUiSprites(runtimeCanvasUi);

            CreateLight();
            if (includePlayer)
            {
                CreatePlayer();
                CreateGround(sceneId);
            }
            else
            {
                CreateCamera();
            }

            if (content == SceneContent.Town)
            {
                CreateTownInteractables();
            }
            else if (content == SceneContent.Dungeon)
            {
                bootstrapObject.AddComponent<DungeonPlayerSpawnRestorer>();
            }

            if (!EditorSceneManager.SaveScene(scene, $"{SceneFolder}/{sceneId}.unity"))
            {
                throw new System.InvalidOperationException($"Failed to save generated scene: {SceneFolder}/{sceneId}.unity");
            }
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.position = new Vector3(0f, 2.2f, -6f);
            cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        private static void CreatePlayer()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -4f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cameraObject.AddComponent<Camera>();

            player.AddComponent<FpsPlayerController>();
            player.AddComponent<PlayerWorldInteractor>();
        }

        private static void CreateGround(GameSceneId sceneId)
        {
            if (sceneId == GameSceneId.Town)
            {
                var environmentPrefab = TownEnvironmentPrefabBuilder.LoadEnvironmentPrefab()
                    ?? TownEnvironmentPrefabBuilder.EnsureDefaultTownEnvironmentAssets().EnvironmentPrefab;
                if (environmentPrefab == null)
                {
                    throw new System.InvalidOperationException("Town environment prefab is missing.");
                }

                var environment = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab);
                environment.name = "Town Environment";
                environment.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                environment.transform.localScale = Vector3.one;
                return;
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = $"{sceneId} Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
        }

        private static void CreateTownInteractables()
        {
            for (var i = 0; i < TownNpcDefinitions.Length; i++)
            {
                var definition = TownNpcDefinitions[i];
                var npc = CreateTownNpc(definition);
                if (!definition.IsService)
                {
                    continue;
                }

                var interactable = RequireTownNpcComponent<TownServiceInteractable>(npc, definition.Name);
                interactable.ServiceName = definition.Name;
                interactable.ServiceKind = definition.ServiceKind;
                interactable.Cost = definition.ServiceCost;
            }
        }

        private static GameObject CreateTownNpc(TownNpcDefinition definition)
        {
            var root = NpcWorldPrefabBuilder.InstantiateTownNpcPrefab(
                definition.Name,
                definition.Position,
                definition.TexturePath,
                definition.FallbackColor,
                definition.VisualHeight,
                definition.FaceCamera,
                definition.InteractableType,
                definition.ColliderSize,
                definition.ColliderCenter);
            if (root == null)
            {
                throw new System.InvalidOperationException($"NpcWorldPrefabBuilder failed to instantiate town NPC prefab: {definition.Name}");
            }

            root.name = definition.Name;
            root.transform.SetPositionAndRotation(definition.Position, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            RequireTownNpcComponent(root, definition.InteractableType, definition.Name);
            return root;
        }

        private static T RequireTownNpcComponent<T>(GameObject root, string name) where T : Component
        {
            var component = root.GetComponent<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException($"NpcWorldPrefabBuilder did not attach {typeof(T).Name} to town NPC prefab: {name}");
            }

            return component;
        }

        private static void RequireTownNpcComponent(GameObject root, System.Type componentType, string name)
        {
            if (root.GetComponent(componentType) == null)
            {
                throw new System.InvalidOperationException($"NpcWorldPrefabBuilder did not attach {componentType.Name} to town NPC prefab: {name}");
            }
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = slash > 0 ? path.Substring(0, slash) : "Assets";
            var folder = slash > 0 ? path.Substring(slash + 1) : path;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private enum SceneContent
        {
            None,
            Town,
            Dungeon
        }

        private readonly struct TownNpcDefinition
        {
            public readonly string Name;
            public readonly System.Type InteractableType;
            public readonly Vector3 Position;
            public readonly string TexturePath;
            public readonly Color FallbackColor;
            public readonly float VisualHeight;
            public readonly bool FaceCamera;
            public readonly Vector3 ColliderSize;
            public readonly Vector3 ColliderCenter;
            public readonly bool IsService;
            public readonly TownServiceKind ServiceKind;
            public readonly int ServiceCost;

            public TownNpcDefinition(
                string name,
                System.Type interactableType,
                Vector3 position,
                string texturePath,
                Color fallbackColor,
                float visualHeight,
                bool faceCamera,
                Vector3 colliderSize,
                Vector3 colliderCenter,
                bool isService = false,
                TownServiceKind serviceKind = TownServiceKind.Inn,
                int serviceCost = 0)
            {
                Name = name;
                InteractableType = interactableType;
                Position = position;
                TexturePath = texturePath;
                FallbackColor = fallbackColor;
                VisualHeight = visualHeight;
                FaceCamera = faceCamera;
                ColliderSize = colliderSize;
                ColliderCenter = colliderCenter;
                IsService = isService;
                ServiceKind = serviceKind;
                ServiceCost = serviceCost;
            }
        }
    }
}

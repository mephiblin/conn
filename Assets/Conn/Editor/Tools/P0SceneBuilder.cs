using Conn.Core.Scenes;
using Conn.Editor.Maps;
using Conn.Editor.Content;
using Conn.Editor.UI;
using Conn.Rendering.Interaction;
using Conn.Rendering.Player;
using Conn.Rendering.World;
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

        [MenuItem("Conn/Build P0 Scenes")]
        public static void BuildP0Scenes()
        {
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
            RuntimeCanvasUiBuilder.EnsureRuntimeCanvas(bootstrapObject, sceneId);

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
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = $"{sceneId} Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
        }

        private static void CreateTownInteractables()
        {
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Quest Board";
            board.transform.position = new Vector3(-2f, 1f, 2f);
            board.transform.localScale = new Vector3(1.5f, 1.4f, 0.25f);
            board.AddComponent<QuestBoardInteractable>();
            ConfigureTownNpcVisual(board, "Quest Board CG", new Color(0.55f, 0.44f, 0.28f, 1f), new Vector2(1.5f, 1.7f));

            var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "Dungeon Gate";
            gate.transform.position = new Vector3(2f, 1.25f, 2f);
            gate.transform.localScale = new Vector3(1.2f, 2.5f, 0.3f);
            gate.AddComponent<GateInteractable>();

            var smith = GameObject.CreatePrimitive(PrimitiveType.Cube);
            smith.name = "Blacksmith";
            smith.transform.position = new Vector3(0f, 1f, 3.5f);
            smith.transform.localScale = new Vector3(1.4f, 1.3f, 0.5f);
            smith.AddComponent<BlacksmithInteractable>();
            ConfigureTownNpcVisual(smith, "Blacksmith CG", new Color(0.7f, 0.36f, 0.26f, 1f), new Vector2(1.35f, 1.85f));

            var skillMerchant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skillMerchant.name = "Skill Merchant";
            skillMerchant.transform.position = new Vector3(-3.5f, 1f, 0f);
            skillMerchant.transform.localScale = new Vector3(1f, 1.4f, 1f);
            skillMerchant.AddComponent<SkillMerchantInteractable>();
            ConfigureTownNpcVisual(skillMerchant, "Skill Merchant CG", new Color(0.35f, 0.48f, 0.78f, 1f), new Vector2(1.25f, 1.85f));

            CreateTownService("Inn", TownServiceKind.Inn, 3, new Vector3(3.5f, 1f, 0f));
            CreateTownService("Trainer", TownServiceKind.Trainer, 5, new Vector3(3.5f, 1f, -2f));
            CreateTownService("Apothecary", TownServiceKind.Apothecary, 4, new Vector3(-3.5f, 1f, -2f));
            CreateTownService("Scholar", TownServiceKind.Scholar, 0, new Vector3(0f, 1f, -3.5f));
        }

        private static void CreateTownService(string name, TownServiceKind kind, int cost, Vector3 position)
        {
            var service = GameObject.CreatePrimitive(PrimitiveType.Cube);
            service.name = name;
            service.transform.position = position;
            service.transform.localScale = new Vector3(1f, 1.3f, 1f);
            var interactable = service.AddComponent<TownServiceInteractable>();
            interactable.ServiceName = name;
            interactable.ServiceKind = kind;
            interactable.Cost = cost;
            ConfigureTownNpcVisual(service, $"{name} CG", ColorForTownService(kind), new Vector2(1.2f, 1.8f));
        }

        private static void ConfigureTownNpcVisual(GameObject root, string visualName, Color color, Vector2 size)
        {
            var renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            var visual = new GameObject(visualName, typeof(SpriteRenderer), typeof(NpcWorldBillboard));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            visual.transform.localRotation = Quaternion.identity;

            var billboard = visual.GetComponent<NpcWorldBillboard>();
            billboard.FallbackColor = color;
            billboard.Size = new Vector2(
                size.x / Mathf.Max(0.01f, root.transform.localScale.x),
                size.y / Mathf.Max(0.01f, root.transform.localScale.y));
        }

        private static Color ColorForTownService(TownServiceKind kind)
        {
            return kind switch
            {
                TownServiceKind.Inn => new Color(0.66f, 0.48f, 0.3f, 1f),
                TownServiceKind.Trainer => new Color(0.62f, 0.34f, 0.28f, 1f),
                TownServiceKind.Apothecary => new Color(0.28f, 0.62f, 0.38f, 1f),
                TownServiceKind.Scholar => new Color(0.48f, 0.42f, 0.72f, 1f),
                _ => new Color(0.78f, 0.68f, 0.52f, 1f)
            };
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
    }
}

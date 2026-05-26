using Conn.Authoring.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Conn.Editor.Content
{
    [CustomEditor(typeof(MonsterGeneratorWorkspace))]
    public sealed class MonsterGeneratorWorkspaceEditor : UnityEditor.Editor
    {
        private int seed = 2001;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var workspace = (MonsterGeneratorWorkspace)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            seed = EditorGUILayout.IntField("Spawn Table Seed", seed);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Monster"))
                {
                    workspace.PreviewMonster();
                    MarkSceneDirty(workspace);
                }

                if (GUILayout.Button("Preview Encounter"))
                {
                    workspace.PreviewEncounter();
                    MarkSceneDirty(workspace);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Spawn Table"))
                {
                    workspace.PreviewSpawnTable(seed);
                    MarkSceneDirty(workspace);
                }

                if (GUILayout.Button("Clear Preview"))
                {
                    workspace.ClearPreview();
                    MarkSceneDirty(workspace);
                }
            }
        }

        private static void MarkSceneDirty(MonsterGeneratorWorkspace workspace)
        {
            if (workspace != null && workspace.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(workspace.gameObject.scene);
            }
        }
    }

    public static class MonsterGeneratorWorkspaceSceneBuilder
    {
        private const string ScenePath = "Assets/Conn/Scenes/Editor/MonsterGenerator.unity";

        [MenuItem("Conn/Authoring/Create Monster Generator Workspace Scene")]
        public static void CreateWorkspaceScene()
        {
            CreateWorkspaceSceneAsset(promptToSaveOpenScenes: true);
        }

        public static void CreateWorkspaceSceneBatch()
        {
            CreateWorkspaceSceneAsset(promptToSaveOpenScenes: false);
        }

        private static void CreateWorkspaceSceneAsset(bool promptToSaveOpenScenes)
        {
            if (promptToSaveOpenScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder("Assets/Conn/Scenes");
            EnsureFolder("Assets/Conn/Scenes/Editor");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MonsterGenerator";

            var workspaceObject = new GameObject("Monster Generator Workspace");
            var workspace = workspaceObject.AddComponent<MonsterGeneratorWorkspace>();

            var previewRoot = new GameObject("Preview Root").transform;
            previewRoot.SetParent(workspaceObject.transform, false);
            workspace.PreviewRoot = previewRoot;

            var spawnPoints = new Transform[4];
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var point = new GameObject($"Spawn Point {i + 1}").transform;
                point.SetParent(workspaceObject.transform, false);
                point.localPosition = new Vector3((i - 1.5f) * 2f, 0f, 0f);
                spawnPoints[i] = point;
            }

            workspace.SpawnPoints = spawnPoints;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Preview Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(1.6f, 1f, 1.6f);

            var cameraObject = new GameObject("Preview Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.position = new Vector3(0f, 3.5f, -8f);
            cameraObject.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

            var lightObject = new GameObject("Preview Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException($"Failed to save monster generator workspace scene: {ScenePath}");
            }

            Selection.activeObject = workspaceObject;
            EditorGUIUtility.PingObject(workspaceObject);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}

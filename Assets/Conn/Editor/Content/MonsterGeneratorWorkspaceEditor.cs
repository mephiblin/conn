using Conn.Authoring.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Conn.Editor.Content
{
    [CustomEditor(typeof(MonsterGeneratorWorkspace))]
    public sealed class MonsterGeneratorWorkspaceEditor : UnityEditor.Editor
    {
        private int seed = 2001;

        public override void OnInspectorGUI()
        {
            var workspace = (MonsterGeneratorWorkspace)target;
            serializedObject.Update();
            DrawCreateMonsterSection(workspace);
            EditorGUILayout.Space();
            DrawExistingPreviewSection(workspace);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCreateMonsterSection(MonsterGeneratorWorkspace workspace)
        {
            EditorGUILayout.LabelField("Create Monster", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateId)), new GUIContent("Id"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateDisplayName)), new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateVisualImage)), new GUIContent("Visual Image"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateSpecies)), new GUIContent("Species"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateGrade)), new GUIContent("Grade"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateDefaultGroupCount)), new GUIContent("Default Group Count"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateMaxHp)), new GUIContent("HP"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateAttackPower)), new GUIContent("Attack"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateDefense)), new GUIContent("Defense"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateEvasionRate)), new GUIContent("Evasion Rate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateXpReward)), new GUIContent("XP Reward"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateAi)), new GUIContent("AI"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreateAssetFolder)), new GUIContent("Asset Folder"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.CreatePrefabFolder)), new GUIContent("Prefab Folder"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.LastCreatedMonster)), new GUIContent("Last Created"));

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(workspace.CreateId)))
            {
                if (GUILayout.Button("Create Monster"))
                {
                    serializedObject.ApplyModifiedProperties();
                    CreateMonster(workspace);
                    MarkSceneDirty(workspace);
                }
            }
        }

        private void DrawExistingPreviewSection(MonsterGeneratorWorkspace workspace)
        {
            EditorGUILayout.LabelField("Preview Existing Assets", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.Monster)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.Encounter)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.SpawnTable)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.PreviewRoot)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.SpawnPoints)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.EncounterSpacing)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterGeneratorWorkspace.ClearBeforePreview)));

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

        private static void CreateMonster(MonsterGeneratorWorkspace workspace)
        {
            EnsureFolder(workspace.CreateAssetFolder);
            EnsureFolder(workspace.CreatePrefabFolder);

            var monster = CreateInstance<MonsterDefinitionAsset>();
            monster.Id = NormalizeId(workspace.CreateId);
            monster.DisplayName = string.IsNullOrWhiteSpace(workspace.CreateDisplayName)
                ? ObjectNames.NicifyVariableName(monster.Id)
                : workspace.CreateDisplayName.Trim();
            monster.VisualImage = workspace.CreateVisualImage;
            monster.Species = workspace.CreateSpecies;
            monster.Grade = workspace.CreateGrade;
            monster.DefaultGroupCount = Mathf.Max(1, workspace.CreateDefaultGroupCount);
            monster.MaxHp = Mathf.Max(1, workspace.CreateMaxHp);
            monster.AttackPower = Mathf.Max(1, workspace.CreateAttackPower);
            monster.Defense = Mathf.Max(0, workspace.CreateDefense);
            monster.EvasionRate = Mathf.Clamp01(workspace.CreateEvasionRate);
            monster.XpReward = Mathf.Max(0, workspace.CreateXpReward);
            monster.Boss = monster.Grade == MonsterGrade.Boss;
            monster.Ai = string.IsNullOrWhiteSpace(workspace.CreateAi) ? "Attack" : workspace.CreateAi.Trim();
            monster.ThemeTags = TagsForSpecies(monster.Species);
            monster.SpawnRoleTags = TagsForGrade(monster.Grade);
            monster.CompatibilityTags = new[] { "dungeon" };

            var prefab = CreateMonsterPrefab(workspace, monster);
            monster.Prefab = prefab;

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{workspace.CreateAssetFolder}/{monster.Id}.asset");
            AssetDatabase.CreateAsset(monster, assetPath);
            EditorUtility.SetDirty(monster);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            workspace.Monster = monster;
            workspace.LastCreatedMonster = monster;
            workspace.PreviewMonster();
            EditorUtility.SetDirty(workspace);
            Selection.activeObject = monster;
            EditorGUIUtility.PingObject(monster);
        }

        private static GameObject CreateMonsterPrefab(MonsterGeneratorWorkspace workspace, MonsterDefinitionAsset monster)
        {
            var root = new GameObject(monster.Id);
            try
            {
                var collider = root.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 0.7f, 0f);
                collider.radius = 0.35f;
                collider.height = 1.4f;

                var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
                visual.name = "Visual Plane";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = VisualScale(monster.VisualImage);

                var visualCollider = visual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    DestroyImmediate(visualCollider);
                }

                var renderer = visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = CreateMonsterMaterial(workspace, monster);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                var prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{workspace.CreatePrefabFolder}/{monster.Id}.prefab");
                return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                DestroyImmediate(root);
            }
        }

        private static Material CreateMonsterMaterial(MonsterGeneratorWorkspace workspace, MonsterDefinitionAsset monster)
        {
            var materialFolder = $"{workspace.CreatePrefabFolder}/Materials";
            EnsureFolder(materialFolder);

            var material = new Material(FindTransparentShader())
            {
                name = $"{monster.Id}_mat",
                mainTexture = monster.VisualImage
            };

            if (monster.VisualImage != null)
            {
                SetTextureIfPresent(material, "_MainTex", monster.VisualImage);
                SetTextureIfPresent(material, "_BaseMap", monster.VisualImage);
            }

            SetColorIfPresent(material, "_Color", Color.white);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            var materialPath = AssetDatabase.GenerateUniqueAssetPath($"{materialFolder}/{monster.Id}.mat");
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static Vector3 VisualScale(Texture2D image)
        {
            var height = 1.4f;
            var aspect = image != null && image.height > 0 ? (float)image.width / image.height : 0.7f;
            return new Vector3(height * aspect, height, 1f);
        }

        private static string NormalizeId(string value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "monster_new" : value.Trim().ToLowerInvariant();
            var chars = text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                {
                    chars[i] = '_';
                }
            }

            var normalized = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(normalized) ? "monster_new" : normalized;
        }

        private static string[] TagsForSpecies(MonsterSpecies species)
        {
            return new[] { species.ToString().ToLowerInvariant() };
        }

        private static string[] TagsForGrade(MonsterGrade grade)
        {
            return new[] { grade.ToString().ToLowerInvariant() };
        }

        private static Shader FindTransparentShader()
        {
            return Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Standard");
        }

        private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
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

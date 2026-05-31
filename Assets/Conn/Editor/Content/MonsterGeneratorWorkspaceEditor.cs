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
        private const float ButtonHeight = 28f;
        private const string AssetFolderHelp = "생성된 MonsterDefinitionAsset 저장 위치입니다.";
        private const string PrefabFolderHelp = "자동 생성되는 프리팹과 머티리얼 저장 위치입니다.";

        private int seed = 2001;

        public override void OnInspectorGUI()
        {
            var workspace = (MonsterGeneratorWorkspace)target;
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "몬스터 생성, 에셋 프리뷰, 조우/스폰 테이블 테스트를 한 곳에서 처리합니다. 위에서 새 몬스터를 만들고, 아래에서 기존 에셋을 배치해 바로 확인하세요.",
                MessageType.Info);

            DrawCreateMonsterSection(workspace);
            EditorGUILayout.Space();
            DrawExistingPreviewSection(workspace);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCreateMonsterSection(MonsterGeneratorWorkspace workspace)
        {
            DrawSectionHeader("새 몬스터 만들기");
            EditorGUILayout.HelpBox("필수값은 ID와 이름입니다. 이미지는 없어도 만들 수 있지만, 등록하면 프리팹 평면에 바로 적용됩니다.", MessageType.None);

            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateId)), new GUIContent("몬스터 ID", "소문자/숫자/밑줄 기준으로 정규화되어 저장됩니다."));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateDisplayName)), new GUIContent("표시 이름", "게임 UI에 노출되는 이름입니다."));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateVisualImage)), new GUIContent("비주얼 이미지", "프리팹의 투명 쿼드 머티리얼에 적용할 이미지입니다."));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateCombatCgResourcePath)), new GUIContent("전투 CG 리소스 경로", "Resources 아래 경로입니다. 비워두면 VisualImage가 Resources 안에 있을 때 자동 계산합니다."));

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawEnumPopup(Prop(nameof(MonsterGeneratorWorkspace.CreateSpecies)), new GUIContent("종족"), new[] { "인간", "야수", "이형", "언데드" });
                DrawEnumPopup(Prop(nameof(MonsterGeneratorWorkspace.CreateGrade)), new GUIContent("등급"), new[] { "일반", "정예", "보스" });
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateDefaultGroupCount)), new GUIContent("기본 무리 수"));
                EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateMaxHp)), new GUIContent("최대 HP"));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateAttackPower)), new GUIContent("공격력"));
                EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateDefense)), new GUIContent("방어력"));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateEvasionRate)), new GUIContent("회피율"));
                EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateXpReward)), new GUIContent("경험치 보상"));
            }

            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateAi)), new GUIContent("AI 패턴", "기본값은 Attack입니다."));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreateAssetFolder)), new GUIContent("에셋 저장 폴더", AssetFolderHelp));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.CreatePrefabFolder)), new GUIContent("프리팹 저장 폴더", PrefabFolderHelp));

            DrawCreateReadiness(workspace);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(workspace.CreateId)))
                {
                    if (GUILayout.Button(new GUIContent("몬스터 생성 + 미리보기", "에셋, 프리팹, 머티리얼을 만들고 프리뷰 씬에 배치합니다."), GUILayout.Height(ButtonHeight)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        CreateMonster(workspace);
                        MarkSceneDirty(workspace);
                    }
                }

                using (new EditorGUI.DisabledScope(workspace.LastCreatedMonster == null))
                {
                    if (GUILayout.Button(new GUIContent("마지막 생성 선택", "마지막으로 만든 몬스터 에셋을 Project 창에서 선택합니다."), GUILayout.Height(ButtonHeight)))
                    {
                        Selection.activeObject = workspace.LastCreatedMonster;
                        EditorGUIUtility.PingObject(workspace.LastCreatedMonster);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.LastCreatedMonster)), new GUIContent("마지막 생성 몬스터"));
            }
        }

        private void DrawExistingPreviewSection(MonsterGeneratorWorkspace workspace)
        {
            DrawSectionHeader("기존 에셋 프리뷰");
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.Monster)), new GUIContent("몬스터", "단일 몬스터 프리뷰 대상입니다."));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.Encounter)), new GUIContent("조우", "PrimaryMonster와 EnemySlots를 함께 배치합니다."));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.SpawnTable)), new GUIContent("스폰 테이블", "시드 기반으로 조우 또는 직접 몬스터 항목을 선택합니다."));

            DrawPreviewSummary(workspace);

            EditorGUILayout.Space(4f);
            DrawSectionHeader("프리뷰 배치 설정");
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.PreviewRoot)), new GUIContent("프리뷰 루트"));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.SpawnPoints)), new GUIContent("스폰 위치 목록"), true);
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.EncounterSpacing)), new GUIContent("자동 배치 간격"));
            EditorGUILayout.PropertyField(Prop(nameof(MonsterGeneratorWorkspace.ClearBeforePreview)), new GUIContent("실행 전 프리뷰 비우기"));

            EditorGUILayout.Space();
            DrawSectionHeader("프리뷰 실행");
            seed = EditorGUILayout.IntField(new GUIContent("스폰 테이블 시드", "같은 시드는 같은 항목을 선택합니다."), seed);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(workspace.Monster == null))
                {
                    if (GUILayout.Button(new GUIContent("몬스터 보기", "선택한 단일 몬스터를 프리뷰합니다."), GUILayout.Height(ButtonHeight)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        PreviewWithUndo(workspace, "몬스터 프리뷰", workspace.PreviewMonster);
                        MarkSceneDirty(workspace);
                    }
                }

                using (new EditorGUI.DisabledScope(workspace.Encounter == null))
                {
                    if (GUILayout.Button(new GUIContent("조우 보기", "선택한 조우의 몬스터 구성을 프리뷰합니다."), GUILayout.Height(ButtonHeight)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        PreviewWithUndo(workspace, "조우 프리뷰", workspace.PreviewEncounter);
                        MarkSceneDirty(workspace);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(workspace.SpawnTable == null))
                {
                    if (GUILayout.Button(new GUIContent("스폰 테이블 보기", "시드로 스폰 테이블 항목을 뽑아 프리뷰합니다."), GUILayout.Height(ButtonHeight)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        PreviewWithUndo(workspace, "스폰 테이블 프리뷰", () => workspace.PreviewSpawnTable(seed));
                        MarkSceneDirty(workspace);
                    }
                }

                using (new EditorGUI.DisabledScope(ResolvePreviewRoot(workspace).childCount == 0))
                {
                    if (GUILayout.Button(new GUIContent("프리뷰 비우기", "현재 프리뷰 루트 아래의 생성 오브젝트를 삭제합니다."), GUILayout.Height(ButtonHeight)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        ClearPreviewWithUndo(workspace, "프리뷰 비우기");
                        MarkSceneDirty(workspace);
                    }
                }
            }
        }

        private SerializedProperty Prop(string propertyName)
        {
            return serializedObject.FindProperty(propertyName);
        }

        private static void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawEnumPopup(SerializedProperty property, GUIContent label, string[] displayNames)
        {
            var index = Mathf.Clamp(property.enumValueIndex, 0, displayNames.Length - 1);
            property.enumValueIndex = EditorGUILayout.Popup(label, index, displayNames);
        }

        private static void DrawCreateReadiness(MonsterGeneratorWorkspace workspace)
        {
            var normalizedId = NormalizeId(workspace.CreateId);
            var assetPath = $"{workspace.CreateAssetFolder}/{normalizedId}.asset";
            var prefabPath = $"{workspace.CreatePrefabFolder}/{normalizedId}.prefab";
            var hasExistingAsset = AssetDatabase.LoadAssetAtPath<MonsterDefinitionAsset>(assetPath) != null;
            var hasExistingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;

            var status = $"저장 예정 ID: {normalizedId}\n에셋: {assetPath}\n프리팹: {prefabPath}";
            EditorGUILayout.HelpBox(status, hasExistingAsset || hasExistingPrefab ? MessageType.Warning : MessageType.None);

            if (string.IsNullOrWhiteSpace(workspace.CreateId))
            {
                EditorGUILayout.HelpBox("몬스터 ID를 입력해야 생성할 수 있습니다.", MessageType.Warning);
            }
            else if (hasExistingAsset || hasExistingPrefab)
            {
                EditorGUILayout.HelpBox("같은 이름의 파일이 있어도 Unity가 자동으로 고유 경로를 생성합니다. 덮어쓰지는 않습니다.", MessageType.Info);
            }
        }

        private static void DrawPreviewSummary(MonsterGeneratorWorkspace workspace)
        {
            var root = ResolvePreviewRoot(workspace);
            var monsterText = workspace.Monster != null ? FormatMonster(workspace.Monster) : "몬스터 미선택";
            var encounterText = workspace.Encounter != null ? FormatEncounter(workspace.Encounter) : "조우 미선택";
            var spawnTableText = workspace.SpawnTable != null ? FormatSpawnTable(workspace.SpawnTable) : "스폰 테이블 미선택";
            var previewText = $"프리뷰 루트: {root.name} / 현재 배치 수: {root.childCount}";

            EditorGUILayout.HelpBox($"{monsterText}\n{encounterText}\n{spawnTableText}\n{previewText}", MessageType.None);
        }

        private static string FormatMonster(MonsterDefinitionAsset monster)
        {
            return $"몬스터: {DisplayNameOrId(monster)} / HP {monster.MaxHp}, 공격 {monster.AttackPower}, 방어 {monster.Defense}, 등급 {monster.Grade}";
        }

        private static string FormatEncounter(EncounterDefinitionAsset encounter)
        {
            var slotCount = encounter.EnemySlots != null ? encounter.EnemySlots.Length : 0;
            var primary = encounter.PrimaryMonster != null ? DisplayNameOrId(encounter.PrimaryMonster) : encounter.PrimaryMonsterId;
            return $"조우: {DisplayNameOrId(encounter.DisplayName, encounter.Id)} / 주 대상 {Fallback(primary, "없음")}, 슬롯 {slotCount}개";
        }

        private static string FormatSpawnTable(SpawnTableAsset spawnTable)
        {
            var encounterCount = spawnTable.EncounterEntries != null ? spawnTable.EncounterEntries.Length : 0;
            var monsterCount = spawnTable.DirectMonsterEntries != null ? spawnTable.DirectMonsterEntries.Length : 0;
            return $"스폰 테이블: {DisplayNameOrId(spawnTable.DisplayName, spawnTable.Id)} / 조우 {encounterCount}개, 직접 몬스터 {monsterCount}개, 총 가중치 {SpawnTableWeight(spawnTable)}";
        }

        private static int SpawnTableWeight(SpawnTableAsset spawnTable)
        {
            var total = 0;
            foreach (var entry in spawnTable.EncounterEntries ?? System.Array.Empty<SpawnEncounterEntry>())
            {
                if (entry != null && entry.Weight > 0)
                {
                    total += entry.Weight;
                }
            }

            foreach (var entry in spawnTable.DirectMonsterEntries ?? System.Array.Empty<SpawnMonsterEntry>())
            {
                if (entry != null && entry.Weight > 0)
                {
                    total += entry.Weight;
                }
            }

            return total;
        }

        private static string DisplayNameOrId(string displayName, string id)
        {
            return !string.IsNullOrWhiteSpace(displayName) ? displayName : Fallback(id, "(id 없음)");
        }

        private static string DisplayNameOrId(MonsterDefinitionAsset monster)
        {
            if (monster == null)
            {
                return "없음";
            }

            if (!string.IsNullOrWhiteSpace(monster.DisplayName))
            {
                return monster.DisplayName;
            }

            return !string.IsNullOrWhiteSpace(monster.Id) ? monster.Id : monster.name;
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static void CreateMonster(MonsterGeneratorWorkspace workspace)
        {
            Undo.RecordObject(workspace, "몬스터 생성");
            EnsureFolder(workspace.CreateAssetFolder);
            EnsureFolder(workspace.CreatePrefabFolder);

            var monster = CreateInstance<MonsterDefinitionAsset>();
            monster.Id = NormalizeId(workspace.CreateId);
            monster.DisplayName = string.IsNullOrWhiteSpace(workspace.CreateDisplayName)
                ? ObjectNames.NicifyVariableName(monster.Id)
                : workspace.CreateDisplayName.Trim();
            monster.VisualImage = workspace.CreateVisualImage;
            monster.CombatCgResourcePath = ResolveCombatCgResourcePath(workspace);
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
            PreviewWithUndo(workspace, "몬스터 생성", workspace.PreviewMonster);
            EditorUtility.SetDirty(workspace);
            Selection.activeObject = monster;
            EditorGUIUtility.PingObject(monster);
        }

        private static string ResolveCombatCgResourcePath(MonsterGeneratorWorkspace workspace)
        {
            if (!string.IsNullOrWhiteSpace(workspace.CreateCombatCgResourcePath))
            {
                return workspace.CreateCombatCgResourcePath.Trim();
            }

            if (workspace.CreateVisualImage == null)
            {
                return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(workspace.CreateVisualImage);
            var resourcesIndex = assetPath.IndexOf("/Resources/");
            if (resourcesIndex < 0)
            {
                return string.Empty;
            }

            var resourcePath = assetPath.Substring(resourcesIndex + "/Resources/".Length);
            var extensionIndex = resourcePath.LastIndexOf('.');
            return extensionIndex > 0 ? resourcePath.Substring(0, extensionIndex) : resourcePath;
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
            ConfigureTransparentMaterial(material);

            var materialPath = AssetDatabase.GenerateUniqueAssetPath($"{materialFolder}/{monster.Id}.mat");
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHATEST_ON");
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
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
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

        private static void PreviewWithUndo(MonsterGeneratorWorkspace workspace, string undoName, System.Action previewAction)
        {
            if (workspace == null || previewAction == null)
            {
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RecordObject(workspace, undoName);

            var previewRoot = ResolvePreviewRoot(workspace);
            var existingChildren = CapturePreviewChildren(previewRoot);
            var clearBeforePreview = workspace.ClearBeforePreview;

            if (clearBeforePreview)
            {
                ClearPreviewWithUndo(workspace, undoName);
                workspace.ClearBeforePreview = false;
            }

            try
            {
                previewAction();
            }
            finally
            {
                if (clearBeforePreview)
                {
                    workspace.ClearBeforePreview = true;
                }
            }

            RegisterCreatedPreviewObjects(previewRoot, existingChildren, undoName);
            EditorUtility.SetDirty(workspace);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void ClearPreviewWithUndo(MonsterGeneratorWorkspace workspace, string undoName)
        {
            if (workspace == null)
            {
                return;
            }

            Undo.SetCurrentGroupName(undoName);
            var root = ResolvePreviewRoot(workspace);
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
            }
        }

        private static Transform ResolvePreviewRoot(MonsterGeneratorWorkspace workspace)
        {
            return workspace.PreviewRoot != null ? workspace.PreviewRoot : workspace.transform;
        }

        private static GameObject[] CapturePreviewChildren(Transform root)
        {
            var children = new GameObject[root.childCount];
            for (var i = 0; i < root.childCount; i++)
            {
                children[i] = root.GetChild(i).gameObject;
            }

            return children;
        }

        private static void RegisterCreatedPreviewObjects(Transform root, GameObject[] existingChildren, string undoName)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i).gameObject;
                if (!Contains(existingChildren, child))
                {
                    Undo.RegisterCreatedObjectUndo(child, undoName);
                }
            }
        }

        private static bool Contains(GameObject[] objects, GameObject candidate)
        {
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] == candidate)
                {
                    return true;
                }
            }

            return false;
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

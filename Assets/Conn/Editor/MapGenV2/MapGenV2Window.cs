using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public sealed class MapGenV2Window : EditorWindow
    {
        private const string ProfilePathKey = "Conn.MapGenV2.Window.ProfilePath";
        private const string DraftPathKey = "Conn.MapGenV2.Window.DraftPath";
        private const string PreviewCellSizeKey = "Conn.MapGenV2.Window.PreviewCellSize";
        private const string OutputModeKey = "Conn.MapGenV2.Window.OutputMode";
        private const string ShowPropOverlayKey = "Conn.MapGenV2.Window.ShowPropOverlay";
        private static readonly Color EmptyColor = new Color(0.04f, 0.08f, 0.9f, 1f);
        private static readonly Color RoomColor = new Color(0.9f, 0f, 0f, 1f);
        private static readonly Color CorridorColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color BlockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color ConnectorColor = CorridorColor;
        private static readonly Color ReservedColor = BlockedColor;
        private static readonly Color GridLineColor = new Color(0f, 0f, 0f, 0.28f);
        private static readonly Color SelectedRegionColor = new Color(1f, 1f, 1f, 0.38f);
        private static readonly Color SelectedConnectorColor = new Color(1f, 0.72f, 0.05f, 0.58f);
        private static readonly Color AdjacentLinkColor = new Color(0.1f, 0.95f, 1f, 0.48f);
        private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color PropOverlayColor = new Color(0.1f, 1f, 0.35f, 0.9f);
        private static readonly Color BlockerPropOverlayColor = new Color(1f, 0.45f, 0.05f, 0.9f);
        private static readonly Color ObjectivePropOverlayColor = new Color(1f, 0.95f, 0.05f, 0.9f);
        private MapGenProfileAsset profile;
        private MapGenMockupDraftAsset draft;
        private Vector2 scroll;
        private Vector2 previewScroll;
        private Vector2Int hoveredCell;
        private Vector2Int selectedCell;
        private bool hasHoveredCell;
        private bool hasSelectedCell;
        private int selectedRegionId = -1;
        private float previewCellSize = 18f;
        private GameObject selectedMaterializedRoot;
        private MapGenV2SceneOutputMode outputMode = MapGenV2SceneOutputMode.ReplacePreviousRoot;
        private bool showPropPlacementOverlay = true;
        private string lastOperationResult = "아직 실행한 작업이 없습니다. / No operation has run yet.";

        [MenuItem("Conn/MapGenV2/Map Generator")]
        public static void Open()
        {
            GetWindow<MapGenV2Window>("MapGenV2");
        }

        public static void Open(MapGenProfileAsset selectedProfile, MapGenMockupDraftAsset selectedDraft)
        {
            var window = GetWindow<MapGenV2Window>("MapGenV2");
            window.profile = selectedProfile;
            window.draft = selectedDraft;
            window.Repaint();
        }

        private void OnEnable()
        {
            previewCellSize = EditorPrefs.GetFloat(PreviewCellSizeKey, previewCellSize);
            outputMode = (MapGenV2SceneOutputMode)EditorPrefs.GetInt(OutputModeKey, (int)outputMode);
            showPropPlacementOverlay = EditorPrefs.GetBool(ShowPropOverlayKey, showPropPlacementOverlay);
            profile = LoadAssetFromEditorPrefs<MapGenProfileAsset>(ProfilePathKey);
            draft = LoadAssetFromEditorPrefs<MapGenMockupDraftAsset>(DraftPathKey);
        }

        private void OnDisable()
        {
            SaveWindowState();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("MapGenV2", EditorStyles.boldLabel);
            var workflow = MapGenV2WorkflowStatus.From(profile, draft);
            DrawWorkflowStatus(workflow);
            DrawReferenceFields();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Starter Setup"))
                {
                    var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup();
                    profile = setup.Profile;
                    draft = setup.Draft;
                    Selection.activeObject = draft != null ? draft : profile;
                    lastOperationResult = "Starter setup created. Next: Generate Mockup.";
                    SaveWindowState();
                }

                if (GUILayout.Button("Create Default Folders"))
                {
                    MapGenV2AssetFolderUtility.CreateDefaultFolders();
                    lastOperationResult = "Default MapGenV2 folders created or already existed.";
                }

                if (GUILayout.Button("Create Draft"))
                {
                    CreateDraft();
                    SaveWindowState();
                }
            }

            workflow = MapGenV2WorkflowStatus.From(profile, draft);
            DrawNextAction(workflow);
            DrawProfileValidation();
            DrawDraftActions(workflow);
            DrawOutputPaths();
            DrawLinkedAssetShortcuts();
            DrawSceneOutputControls(workflow);
            DrawDraftSummaryAndPreview();
            EditorGUILayout.EndScrollView();
        }

        private void DrawWorkflowStatus(MapGenV2WorkflowStatus workflow)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("워크플로우 상태 / Workflow Status", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawStepBadge("Setup", workflow.HasProfile && workflow.ProfileValid && workflow.HasDraft, !workflow.HasProfile || !workflow.ProfileValid || !workflow.HasDraft);
                    DrawStepBadge("Generate", workflow.HasGeneratedMockup, workflow.CanGenerate && !workflow.HasGeneratedMockup);
                    DrawStepBadge("Post-Process", workflow.HasGeneratedMockup, workflow.CanPostProcess && !workflow.Accepted);
                    DrawStepBadge("Accept", workflow.Accepted && workflow.AcceptedCurrent, workflow.CanAccept && (!workflow.Accepted || !workflow.AcceptedCurrent));
                    DrawStepBadge("Materialize", false, workflow.CanMaterialize);
                    DrawStepBadge("Bake", false, workflow.CanBakeRuntime);
                }

                var profileName = profile != null ? profile.ProfileId : "(none)";
                var draftName = draft != null ? draft.name : "(none)";
                var seed = draft != null ? draft.Seed.ToString() : "-";
                EditorGUILayout.LabelField("현재 / Current", $"Profile {profileName}, Draft {draftName}, Seed {seed}");
                EditorGUILayout.LabelField("마지막 작업 / Last Result", lastOperationResult);
            }
        }

        private static void DrawStepBadge(string label, bool complete, bool active)
        {
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = complete
                ? new Color(0.38f, 0.72f, 0.42f, 1f)
                : active ? new Color(0.95f, 0.73f, 0.24f, 1f) : new Color(0.42f, 0.42f, 0.42f, 1f);
            GUILayout.Label(label, EditorStyles.miniButton, GUILayout.MinWidth(92f));
            GUI.backgroundColor = previousColor;
        }

        private void DrawReferenceFields()
        {
            EditorGUI.BeginChangeCheck();
            profile = (MapGenProfileAsset)EditorGUILayout.ObjectField("프로필 / Profile", profile, typeof(MapGenProfileAsset), false);
            draft = (MapGenMockupDraftAsset)EditorGUILayout.ObjectField("드래프트 / Draft", draft, typeof(MapGenMockupDraftAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                SaveWindowState();
            }
        }

        private void DrawNextAction(MapGenV2WorkflowStatus workflow)
        {
            EditorGUILayout.HelpBox($"다음 작업 / Next Action: {workflow.NextAction}", MessageType.Info);
        }

        private void DrawProfileValidation()
        {
            if (profile == null)
            {
                EditorGUILayout.HelpBox("Assign a profile.", MessageType.Info);
                return;
            }

            MapGenValidationReportEditorGUI.Draw(profile.Validate(), profile, "Profile is valid.");
        }

        private void DrawOutputPaths()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("출력 경로 / Output Paths", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Draft Folder", GetDraftFolder());
                EditorGUILayout.LabelField("Selected Draft", draft != null ? AssetDatabase.GetAssetPath(draft) : "(none)");
                EditorGUILayout.LabelField("Materialized Prefab Folder", GetMaterializedPrefabFolder());
                EditorGUILayout.LabelField("Baked Asset", BuildExpectedBakedAssetPath());
            }
        }

        private void DrawLinkedAssetShortcuts()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("연결 에셋 / Linked Assets", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawObjectShortcutRow("Profile", profile);
                DrawObjectShortcutRow("Draft", draft);
                DrawObjectShortcutRow("Materialized Root", selectedMaterializedRoot);
                DrawObjectShortcutRow("Baked Asset", LoadExpectedBakedAsset());

                if (profile == null)
                {
                    return;
                }

                DrawObjectShortcutRow("Rule Set", profile.LayoutRules);
                DrawObjectShortcutRow("Style Set", profile.StyleSet);
                DrawObjectShortcutRow("Module Set", profile.StyleSet != null ? profile.StyleSet.ModuleSet : null);

                var roomShapes = profile.RoomShapes ?? System.Array.Empty<MapGenRoomShapeAsset>();
                for (var i = 0; i < roomShapes.Length; i++)
                {
                    DrawObjectShortcutRow($"Room Shape {i}", roomShapes[i]);
                }

                if (profile.StyleSet != null)
                {
                    var roomTemplates = profile.StyleSet.RoomTemplates ?? System.Array.Empty<MapGenRoomTemplateAsset>();
                    for (var i = 0; i < roomTemplates.Length; i++)
                    {
                        DrawObjectShortcutRow($"Room Template {i}", roomTemplates[i]);
                    }

                    var corridorTemplates = profile.StyleSet.CorridorTemplates ?? System.Array.Empty<MapGenCorridorTemplateAsset>();
                    for (var i = 0; i < corridorTemplates.Length; i++)
                    {
                        DrawObjectShortcutRow($"Corridor Template {i}", corridorTemplates[i]);
                    }
                }
            }
        }

        private static void DrawObjectShortcutRow(string label, Object target)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(label, target, typeof(Object), false);
                using (new EditorGUI.DisabledScope(target == null))
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    {
                        EditorGUIUtility.PingObject(target);
                    }

                    if (GUILayout.Button("Select", GUILayout.Width(58f)))
                    {
                        Selection.activeObject = target;
                    }

                    if (GUILayout.Button("Open", GUILayout.Width(52f)))
                    {
                        AssetDatabase.OpenAsset(target);
                    }
                }
            }
        }

        private void DrawSceneOutputControls(MapGenV2WorkflowStatus workflow)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("씬 출력 / Scene Output", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                outputMode = (MapGenV2SceneOutputMode)EditorGUILayout.EnumPopup("출력 모드 / Output Mode", outputMode);
                selectedMaterializedRoot = (GameObject)EditorGUILayout.ObjectField("Materialized Root", selectedMaterializedRoot, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                {
                    SaveWindowState();
                }

                DrawMaterializedPrefabFolderField();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Find Previous Root"))
                    {
                        var marker = MapGenMockupMaterializer.FindExistingMarker(draft);
                        selectedMaterializedRoot = marker != null ? marker.gameObject : null;
                        lastOperationResult = selectedMaterializedRoot != null
                            ? $"Found materialized root: {selectedMaterializedRoot.name}."
                            : "No previous materialized root found for the selected draft.";
                    }

                    using (new EditorGUI.DisabledScope(selectedMaterializedRoot == null))
                    {
                        if (GUILayout.Button("Select Materialized Root"))
                        {
                            Selection.activeGameObject = selectedMaterializedRoot;
                            EditorGUIUtility.PingObject(selectedMaterializedRoot);
                        }

                        if (GUILayout.Button("Frame Materialized Root"))
                        {
                            FrameSelectedRoot();
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(selectedMaterializedRoot == null))
                    {
                        if (GUILayout.Button("Clear Previous Materialization"))
                        {
                            MapGenMockupMaterializer.ClearRoot(selectedMaterializedRoot);
                            selectedMaterializedRoot = null;
                            lastOperationResult = "Cleared selected materialized root.";
                        }

                        if (GUILayout.Button("Save Materialized As Prefab"))
                        {
                            SaveSelectedRootAsPrefab();
                        }
                    }
                }

                if (!workflow.CanMaterialize)
                {
                    EditorGUILayout.HelpBox($"Materialize To Scene is unavailable: {workflow.MaterializeReason}", MessageType.Info);
                }
            }
        }

        private void DrawMaterializedPrefabFolderField()
        {
            using (new EditorGUI.DisabledScope(profile == null))
            {
                var currentFolder = GetMaterializedPrefabFolder();
                EditorGUI.BeginChangeCheck();
                var newFolder = EditorGUILayout.TextField("Materialized Prefab Folder", currentFolder);
                if (EditorGUI.EndChangeCheck() && profile != null)
                {
                    Undo.RecordObject(profile, "Change MapGen Materialized Prefab Folder");
                    profile.OutputSettings.MaterializedPrefabFolder = newFolder;
                    EditorUtility.SetDirty(profile);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ensure Prefab Folder", GUILayout.Width(170f)))
                    {
                        MapGenV2AssetFolderUtility.EnsureAssetFolder(GetMaterializedPrefabFolder());
                        lastOperationResult = $"Materialized prefab folder ready: {GetMaterializedPrefabFolder()}.";
                    }
                }
            }
        }

        private void FrameSelectedRoot()
        {
            if (selectedMaterializedRoot == null)
            {
                return;
            }

            Selection.activeGameObject = selectedMaterializedRoot;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private void SaveSelectedRootAsPrefab()
        {
            if (selectedMaterializedRoot == null)
            {
                return;
            }

            MapGenV2AssetFolderUtility.CreateDefaultFolders();
            var prefabFolder = GetMaterializedPrefabFolder();
            MapGenV2AssetFolderUtility.EnsureAssetFolder(prefabFolder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{prefabFolder}/{selectedMaterializedRoot.name}.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(selectedMaterializedRoot, path);
            lastOperationResult = prefab != null
                ? $"Saved materialized prefab: {path}."
                : "Save Materialized As Prefab failed.";
        }

        private void DrawDraftActions(MapGenV2WorkflowStatus workflow)
        {
            if (draft == null)
            {
                EditorGUILayout.HelpBox("Create or assign a draft.", MessageType.Info);
                return;
            }

            if (profile != null && draft.Profile != profile)
            {
                if (GUILayout.Button("Assign Profile To Draft"))
                {
                    Undo.RecordObject(draft, "Assign MapGen Profile");
                    draft.Profile = profile;
                    EditorUtility.SetDirty(draft);
                    lastOperationResult = "Selected profile assigned to draft.";
                    SaveWindowState();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                {
                    if (GUILayout.Button("Generate Mockup"))
                    {
                        GenerateMockup("Generate Mockup");
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                {
                    if (GUILayout.Button("Regenerate Same Seed"))
                    {
                        GenerateMockup("Regenerate Same Seed", preserveLockedRegions: true);
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanPostProcess))
                {
                    if (GUILayout.Button("Run Post-Process"))
                    {
                        Undo.RecordObject(draft, "Post-Process Mockup");
                        var report = draft.ApplyPostProcessingFromProfile();
                        EditorUtility.SetDirty(draft);
                        lastOperationResult = $"Post-process complete. Direct routes +{report.DirectRouteCellsAdded}, dead ends removed {report.DeadEndCorridorsRemoved}, isolated rooms removed {report.IsolatedRoomsRemoved}.";
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanAccept))
                {
                    if (GUILayout.Button("Accept Mockup"))
                    {
                        Undo.RecordObject(draft, "Accept Mockup");
                        draft.Accept();
                        EditorUtility.SetDirty(draft);
                        lastOperationResult = $"Accepted mockup signature {draft.AcceptedSignature}.";
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(draft == null))
                {
                    if (GUILayout.Button("Randomize Seed"))
                    {
                        Undo.RecordObject(draft, "Randomize MapGen Seed");
                        draft.Seed = CreateRandomSeed();
                        draft.ClearDraft();
                        ClearSelection();
                        EditorUtility.SetDirty(draft);
                        lastOperationResult = $"Randomized seed to {draft.Seed}. Generate Mockup to preview it.";
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                {
                    if (GUILayout.Button("Randomize Seed + Generate"))
                    {
                        Undo.RecordObject(draft, "Randomize Seed And Generate Mockup");
                        draft.Seed = CreateRandomSeed();
                        GenerateMockup("Randomize Seed + Generate", recordUndo: false);
                    }
                }

                using (new EditorGUI.DisabledScope(draft == null))
                {
                    if (GUILayout.Button("Clear Draft"))
                    {
                        Undo.RecordObject(draft, "Clear MapGen Draft");
                        draft.ClearDraft();
                        ClearSelection();
                        EditorUtility.SetDirty(draft);
                        lastOperationResult = "Cleared draft cells, generated signature, post-process report, and acceptance.";
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!workflow.CanMaterialize))
                {
                    if (GUILayout.Button("Materialize To Scene"))
                    {
                        var root = MapGenMockupMaterializer.Materialize(draft, outputMode, selectedMaterializedRoot);
                        selectedMaterializedRoot = root;
                        lastOperationResult = root != null
                            ? $"Materialized scene root: {root.name}."
                            : "Materialize To Scene failed. Check accepted state and module coverage.";
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanBakeRuntime))
                {
                    if (GUILayout.Button("Bake Runtime Asset"))
                    {
                        var bakedAsset = MapGenRuntimeBakeUtility.Bake(draft);
                        lastOperationResult = bakedAsset != null
                            ? $"Baked runtime asset: {AssetDatabase.GetAssetPath(bakedAsset)}."
                            : "Bake Runtime Asset failed. Check accepted state.";
                    }
                }
            }

            if (!workflow.CanGenerate)
            {
                EditorGUILayout.HelpBox($"Generate Mockup disabled: {workflow.GenerateReason}", MessageType.Info);
            }

            if (!workflow.CanMaterialize)
            {
                EditorGUILayout.HelpBox($"Materialize To Scene disabled: {workflow.MaterializeReason}", MessageType.Info);
            }

            if (!workflow.CanBakeRuntime)
            {
                EditorGUILayout.HelpBox($"Bake Runtime Asset disabled: {workflow.BakeRuntimeReason}", MessageType.Info);
            }
        }

        private void GenerateMockup(string operationName, bool recordUndo = true, bool preserveLockedRegions = false)
        {
            if (draft == null)
            {
                return;
            }

            if (recordUndo)
            {
                Undo.RecordObject(draft, operationName);
            }

            var report = preserveLockedRegions
                ? draft.RegenerateUnlockedFromProfile()
                : draft.GenerateFromProfile();
            ClearSelection();
            EditorUtility.SetDirty(draft);
            var preview = MapGenMockupPreviewData.FromDraft(draft);
            lastOperationResult = report.IsValid
                ? $"{operationName} complete. Seed {draft.Seed}, Retry 0, Rooms {preview.Summary.RoomCells}, Corridors {preview.Summary.CorridorCells}."
                : $"{operationName} failed. See validation messages above.";
        }

        private static int CreateRandomSeed()
        {
            return System.Guid.NewGuid().GetHashCode() & 0x7fffffff;
        }

        private void CreateDraft()
        {
            MapGenV2AssetFolderUtility.CreateDefaultFolders();
            var draftFolder = GetDraftFolder();
            MapGenV2AssetFolderUtility.EnsureAssetFolder(draftFolder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{draftFolder}/MapGenMockupDraft.asset");
            draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            draft.Profile = profile;
            AssetDatabase.CreateAsset(draft, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = draft;
            lastOperationResult = $"Created draft: {path}.";
        }

        private string BuildExpectedBakedAssetPath()
        {
            if (draft == null || draft.Profile == null)
            {
                return GetBakedAssetFolder();
            }

            return $"{GetBakedAssetFolder()}/{draft.Profile.ProfileId}_{draft.Seed}_BakedMap.asset";
        }

        private MapGenBakedMapAsset LoadExpectedBakedAsset()
        {
            var path = BuildExpectedBakedAssetPath();
            return AssetDatabase.LoadAssetAtPath<MapGenBakedMapAsset>(path);
        }

        private string GetDraftFolder()
        {
            return profile != null && !string.IsNullOrWhiteSpace(profile.OutputSettings.DraftFolder)
                ? profile.OutputSettings.DraftFolder
                : MapGenOutputSettings.Defaults().DraftFolder;
        }

        private string GetMaterializedPrefabFolder()
        {
            return profile != null && !string.IsNullOrWhiteSpace(profile.OutputSettings.MaterializedPrefabFolder)
                ? profile.OutputSettings.MaterializedPrefabFolder
                : MapGenOutputSettings.Defaults().MaterializedPrefabFolder;
        }

        private string GetBakedAssetFolder()
        {
            return profile != null && !string.IsNullOrWhiteSpace(profile.OutputSettings.BakedAssetFolder)
                ? profile.OutputSettings.BakedAssetFolder
                : MapGenOutputSettings.Defaults().BakedAssetFolder;
        }

        private void SaveWindowState()
        {
            SaveAssetToEditorPrefs(ProfilePathKey, profile);
            SaveAssetToEditorPrefs(DraftPathKey, draft);
            EditorPrefs.SetFloat(PreviewCellSizeKey, previewCellSize);
            EditorPrefs.SetInt(OutputModeKey, (int)outputMode);
            EditorPrefs.SetBool(ShowPropOverlayKey, showPropPlacementOverlay);
        }

        private static void SaveAssetToEditorPrefs(string key, Object asset)
        {
            if (asset == null)
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            EditorPrefs.SetString(key, AssetDatabase.GetAssetPath(asset));
        }

        private static T LoadAssetFromEditorPrefs<T>(string key) where T : Object
        {
            var path = EditorPrefs.GetString(key, string.Empty);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private void DrawDraftSummaryAndPreview()
        {
            if (draft == null)
            {
                return;
            }

            var previewData = MapGenMockupPreviewData.FromDraft(draft);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("목업 요약 / Mockup Summary", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("프로필 / Profile", string.IsNullOrEmpty(previewData.ProfileId) ? "(none)" : previewData.ProfileId);
                EditorGUILayout.LabelField("시드 / Seed", previewData.Seed.ToString());
                EditorGUILayout.LabelField("그리드 / Grid", $"{previewData.Width} x {previewData.Height}");
                EditorGUILayout.LabelField("생성 서명 / Generated Signature", string.IsNullOrEmpty(previewData.LastGeneratedSignature) ? "(none)" : previewData.LastGeneratedSignature);
                EditorGUILayout.LabelField("현재 서명 / Current Signature", previewData.CurrentSignature);
                EditorGUILayout.LabelField("수락 서명 / Accepted Signature", string.IsNullOrEmpty(previewData.AcceptedSignature) ? "(none)" : previewData.AcceptedSignature);
                EditorGUILayout.LabelField(
                    "상태 / State",
                    previewData.Accepted
                        ? previewData.AcceptedSignatureCurrent ? "수락됨 / Accepted" : "변경됨: 다시 수락 필요 / Stale"
                        : "미수락 / Not accepted");
                EditorGUILayout.LabelField(
                    "셀 / Cells",
                    $"Rooms {previewData.Summary.RoomCells}, Corridors {previewData.Summary.CorridorCells}, Blocked {previewData.Summary.BlockedCells}, Connectors {previewData.Summary.ConnectorCells}, Reserved {previewData.Summary.ReservedCells}");
                EditorGUILayout.LabelField("리전 / Regions", previewData.Summary.RegionCount.ToString());
            }

            var propPlacement = MapGenPropPlacementPlanner.BuildForDraft(draft);
            DrawPropPlacementPreviewSummary(propPlacement);
            DrawMockupPreview(previewData);
            DrawCellDetails(previewData);
        }

        private void DrawMockupPreview(MapGenMockupPreviewData previewData)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("목업 미리보기 / Mockup Preview", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("확대 / Zoom", GUILayout.Width(72f));
                previewCellSize = EditorGUILayout.Slider(previewCellSize, 6f, 32f, GUILayout.Width(180f));
                showPropPlacementOverlay = EditorGUILayout.ToggleLeft("프롭 오버레이 / Props", showPropPlacementOverlay, GUILayout.Width(150f));
            }

            DrawLegend();

            if (previewData.Width <= 0 || previewData.Height <= 0)
            {
                EditorGUILayout.HelpBox("생성된 목업이 없습니다. Generate Mockup을 누르면 이 영역에 파랑/빨강/검정/회색 그리드가 표시됩니다.", MessageType.Info);
                return;
            }

            var previewWidth = previewData.Width * previewCellSize;
            var previewHeight = previewData.Height * previewCellSize;
            var viewHeight = Mathf.Min(Mathf.Max(180f, previewHeight + 20f), 520f);
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.MinHeight(viewHeight), GUILayout.MaxHeight(viewHeight));
            var rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            var propPlacement = showPropPlacementOverlay ? MapGenPropPlacementPlanner.BuildForDraft(draft) : null;
            HandlePreviewInput(previewData, rect);
            DrawPreviewCells(previewData, rect);
            DrawPropPlacementOverlay(previewData, rect, propPlacement);
            EditorGUILayout.EndScrollView();
        }

        private void DrawLegend()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                DrawLegendItem(EmptyColor, "파랑 Empty");
                DrawLegendItem(RoomColor, "빨강 Room");
                DrawLegendItem(CorridorColor, "검정 Corridor");
                DrawLegendItem(BlockedColor, "회색 Blocked");
                DrawLegendItem(ConnectorColor, "검정 Connector");
                DrawLegendItem(ReservedColor, "회색 Reserved");
                DrawLegendItem(PropOverlayColor, "초록 Prop");
                DrawLegendItem(BlockerPropOverlayColor, "주황 Blocker");
            }
        }

        private static void DrawPropPlacementPreviewSummary(MapGenPropPlacementResult propPlacement)
        {
            if (propPlacement == null || propPlacement.PlacedProps.Length == 0 && propPlacement.Report.TotalCandidateCells == 0)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("프롭 배치 미리보기 / Prop Placement Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "요약 / Summary",
                    $"Candidates {propPlacement.Report.TotalCandidateCells}, Placed {propPlacement.Report.PlacedCount}, Spacing Rejects {propPlacement.Report.RejectedBySpacing}, Blocker Issues {propPlacement.Report.BlockerTraversalIssues}");
                if (!propPlacement.Report.IsValid)
                {
                    EditorGUILayout.HelpBox("프롭 배치에 문제가 있습니다. Materialize 전에 rule/filter/blocker 설정을 확인하세요.", MessageType.Warning);
                }
            }
        }

        private static void DrawLegendItem(Color color, string label)
        {
            var rect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.LabelField(label, GUILayout.Width(104f));
        }

        private void DrawPreviewCells(MapGenMockupPreviewData previewData, Rect rect)
        {
            for (var y = previewData.Height - 1; y >= 0; y--)
            {
                for (var x = 0; x < previewData.Width; x++)
                {
                    previewData.TryGetCell(x, y, out var cell);
                    var cellRect = RectForCell(previewData, rect, x, y);
                    EditorGUI.DrawRect(cellRect, ColorForCell(cell.State));

                    if (hasSelectedCell && IsSelectedCellOrRegion(cell, x, y))
                    {
                        EditorGUI.DrawRect(cellRect, SelectedRegionColor);
                    }

                    if (hasSelectedCell && IsSelectedRegionConnector(cell))
                    {
                        EditorGUI.DrawRect(cellRect, SelectedConnectorColor);
                    }

                    if (hasSelectedCell && IsAdjacentRegionLink(previewData, cell, x, y))
                    {
                        EditorGUI.DrawRect(cellRect, AdjacentLinkColor);
                    }

                    if (hasHoveredCell && hoveredCell.x == x && hoveredCell.y == y)
                    {
                        EditorGUI.DrawRect(cellRect, HoverColor);
                    }

                    if (previewCellSize >= 8f)
                    {
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1f, cellRect.width, 1f), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.xMax - 1f, cellRect.y, 1f, cellRect.height), GridLineColor);
                    }
                }
            }
        }

        private void DrawPropPlacementOverlay(
            MapGenMockupPreviewData previewData,
            Rect rect,
            MapGenPropPlacementResult propPlacement)
        {
            if (propPlacement == null || propPlacement.PlacedProps == null || propPlacement.PlacedProps.Length == 0)
            {
                return;
            }

            var placed = new HashSet<MapGenGridCoord>();
            foreach (var prop in propPlacement.PlacedProps)
            {
                if (!placed.Add(prop.Coord))
                {
                    continue;
                }

                var cellRect = RectForCell(previewData, rect, prop.Coord.X, prop.Coord.Y);
                var size = Mathf.Max(4f, previewCellSize * 0.42f);
                var markerRect = new Rect(
                    cellRect.center.x - size * 0.5f,
                    cellRect.center.y - size * 0.5f,
                    size,
                    size);
                EditorGUI.DrawRect(markerRect, ColorForProp(prop));
            }
        }

        private Rect RectForCell(MapGenMockupPreviewData previewData, Rect rect, int x, int y)
        {
            return new Rect(
                rect.x + x * previewCellSize,
                rect.y + (previewData.Height - 1 - y) * previewCellSize,
                Mathf.Ceil(previewCellSize),
                Mathf.Ceil(previewCellSize));
        }

        private void HandlePreviewInput(MapGenMockupPreviewData previewData, Rect rect)
        {
            var current = Event.current;
            if (current == null)
            {
                return;
            }

            var inside = rect.Contains(current.mousePosition);
            if (!inside)
            {
                if (hasHoveredCell && current.type == EventType.MouseMove)
                {
                    hasHoveredCell = false;
                    Repaint();
                }

                return;
            }

            var x = Mathf.FloorToInt((current.mousePosition.x - rect.x) / previewCellSize);
            var drawY = Mathf.FloorToInt((current.mousePosition.y - rect.y) / previewCellSize);
            var y = previewData.Height - 1 - drawY;
            var coord = new Vector2Int(x, y);
            if (!previewData.TryGetCell(coord, out var cell))
            {
                return;
            }

            if (!hasHoveredCell || hoveredCell != coord)
            {
                hoveredCell = coord;
                hasHoveredCell = true;
                Repaint();
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                selectedCell = coord;
                hasSelectedCell = true;
                selectedRegionId = cell.RegionId;
                current.Use();
                Repaint();
            }
        }

        private void DrawCellDetails(MapGenMockupPreviewData previewData)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("선택/호버 정보 / Selection", EditorStyles.boldLabel);
                DrawCellLine("호버 / Hover", previewData, hasHoveredCell, hoveredCell);
                DrawCellLine("선택 / Selected", previewData, hasSelectedCell, selectedCell);
                if (hasSelectedCell)
                {
                    EditorGUILayout.LabelField("선택 리전 / Selected Region", selectedRegionId >= 0 ? selectedRegionId.ToString() : "(none)");
                }
            }

            DrawSelectedRegionInspector(previewData);
        }

        private void DrawSelectedRegionInspector(MapGenMockupPreviewData previewData)
        {
            if (draft == null || !hasSelectedCell || selectedRegionId < 0)
            {
                return;
            }

            var cellCount = 0;
            var roomCount = 0;
            var corridorCount = 0;
            var connectorCount = 0;
            var adjacentLinkCount = 0;
            var blockedCount = 0;
            var reservedCount = 0;
            var category = MapGenRoomCategory.Main;
            var sourceTemplateId = string.Empty;
            var sourceShapeId = string.Empty;
            var sawCategory = false;
            for (var y = 0; y < previewData.Height; y++)
            {
                for (var x = 0; x < previewData.Width; x++)
                {
                    if (!previewData.TryGetCell(x, y, out var cell) || cell.RegionId != selectedRegionId)
                    {
                        continue;
                    }

                    cellCount++;
                    if (!sawCategory)
                    {
                        category = cell.RoomCategory;
                        sawCategory = true;
                    }

                    if (string.IsNullOrEmpty(sourceTemplateId) && !string.IsNullOrEmpty(cell.SourceTemplateId))
                    {
                        sourceTemplateId = cell.SourceTemplateId;
                    }

                    if (string.IsNullOrEmpty(sourceShapeId) && !string.IsNullOrEmpty(cell.SourceShapeId))
                    {
                        sourceShapeId = cell.SourceShapeId;
                    }

                    switch (cell.State)
                    {
                        case MapGenCellState.Room:
                            roomCount++;
                            break;
                        case MapGenCellState.Corridor:
                            corridorCount++;
                            break;
                        case MapGenCellState.Connector:
                            connectorCount++;
                            break;
                        case MapGenCellState.Blocked:
                            blockedCount++;
                            break;
                        case MapGenCellState.Reserved:
                            reservedCount++;
                            break;
                    }
                }
            }

            adjacentLinkCount = CountAdjacentRegionLinks(previewData);
            draft.TryGetRegionOverride(selectedRegionId, out var regionOverride);
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("선택 리전 편집 / Selected Region", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Region Id", selectedRegionId.ToString());
                EditorGUILayout.LabelField("Type / 타입", DescribeRegionType(roomCount, corridorCount, connectorCount, blockedCount, reservedCount));
                EditorGUILayout.LabelField("Cell Count", cellCount.ToString());
                EditorGUILayout.LabelField(
                    "State Counts",
                    $"Room {roomCount}, Corridor {corridorCount}, Connector {connectorCount}, Blocked {blockedCount}, Reserved {reservedCount}");
                EditorGUILayout.LabelField("Connector Count", connectorCount.ToString());
                EditorGUILayout.LabelField("Adjacent Links / 인접 링크", adjacentLinkCount.ToString());
                EditorGUILayout.LabelField("Locked", regionOverride.Locked ? "Yes" : "No");
                EditorGUILayout.LabelField(
                    "Override",
                    regionOverride.HasCategoryOverride ? $"Category {regionOverride.CategoryOverride}" : "(none)");
                EditorGUILayout.LabelField("Source Template", string.IsNullOrEmpty(sourceTemplateId) ? "(none)" : sourceTemplateId);
                EditorGUILayout.LabelField("Source Shape", string.IsNullOrEmpty(sourceShapeId) ? "(none)" : sourceShapeId);
                EditorGUILayout.LabelField(
                    "Materialization",
                    DescribeMaterializationHint(roomCount, corridorCount, connectorCount, blockedCount, reservedCount));

                EditorGUI.BeginChangeCheck();
                var newCategory = (MapGenRoomCategory)EditorGUILayout.EnumPopup("Room Category", category);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(draft, "Change MapGen Region Category");
                    draft.SetRegionCategory(selectedRegionId, newCategory);
                    EditorUtility.SetDirty(draft);
                    lastOperationResult = $"Changed region {selectedRegionId} category to {newCategory}. Accepted output is now stale until reaccepted.";
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(regionOverride.Locked ? "Unlock Region" : "Lock Region"))
                    {
                        Undo.RecordObject(draft, "Toggle MapGen Region Lock");
                        draft.SetRegionLocked(selectedRegionId, !regionOverride.Locked);
                        EditorUtility.SetDirty(draft);
                        lastOperationResult = $"{(regionOverride.Locked ? "Unlocked" : "Locked")} region {selectedRegionId}.";
                    }

                    if (GUILayout.Button("Clear Region Override"))
                    {
                        Undo.RecordObject(draft, "Clear MapGen Region Override");
                        draft.ClearRegionOverride(selectedRegionId);
                        EditorUtility.SetDirty(draft);
                        lastOperationResult = $"Cleared region {selectedRegionId} override metadata.";
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(regionOverride.Locked || roomCount <= 0))
                    {
                        if (GUILayout.Button("Regenerate Room / 방 재생성"))
                        {
                            RegenerateSelectedRoomRegion();
                        }
                    }

                    if (GUILayout.Button("Delete Region / 삭제"))
                    {
                        ChangeSelectedRegionState(MapGenCellState.Empty, "Deleted");
                        ClearSelection();
                    }

                    if (GUILayout.Button("Mark Blocked / 차단"))
                    {
                        ChangeSelectedRegionState(MapGenCellState.Blocked, "Marked blocked");
                    }

                    if (GUILayout.Button("Mark Reserved / 예약"))
                    {
                        ChangeSelectedRegionState(MapGenCellState.Reserved, "Marked reserved");
                    }
                }
            }
        }

        private void RegenerateSelectedRoomRegion()
        {
            if (draft == null || selectedRegionId < 0)
            {
                return;
            }

            Undo.RecordObject(draft, "Regenerate MapGen Room Region");
            draft.Seed = CreateRandomSeed();
            var report = draft.RegenerateRegionFromProfile(selectedRegionId);
            EditorUtility.SetDirty(draft);
            var preview = MapGenMockupPreviewData.FromDraft(draft);
            lastOperationResult = report.IsValid
                ? $"Regenerated room region {selectedRegionId}. Seed {draft.Seed}, Rooms {preview.Summary.RoomCells}, Corridors {preview.Summary.CorridorCells}."
                : $"Regenerate region failed: {report.Issues[0].Message}";
            Repaint();
        }

        private static string DescribeRegionType(
            int roomCount,
            int corridorCount,
            int connectorCount,
            int blockedCount,
            int reservedCount)
        {
            if (roomCount > 0)
            {
                return connectorCount > 0 ? "Room with connectors / 방+커넥터" : "Room / 방";
            }

            if (corridorCount > 0)
            {
                return "Corridor / 복도";
            }

            if (blockedCount > 0)
            {
                return "Blocked / 차단";
            }

            if (reservedCount > 0)
            {
                return "Reserved / 예약";
            }

            return "Unknown / 알 수 없음";
        }

        private static string DescribeMaterializationHint(
            int roomCount,
            int corridorCount,
            int connectorCount,
            int blockedCount,
            int reservedCount)
        {
            if (roomCount + corridorCount + connectorCount > 0)
            {
                return "Navigable cells stamp floors/walls; connectors request doors. / 이동 가능 셀은 바닥/벽, 커넥터는 문 요청";
            }

            if (blockedCount + reservedCount > 0)
            {
                return "Non-navigable; skipped by current materialization/runtime bake. / 현재 출력과 런타임 베이크에서 제외";
            }

            return "No materialization output expected. / 출력 없음";
        }

        private void ChangeSelectedRegionState(MapGenCellState state, string actionLabel)
        {
            if (draft == null || selectedRegionId < 0)
            {
                return;
            }

            Undo.RecordObject(draft, $"MapGen {actionLabel} Region");
            draft.SetRegionState(selectedRegionId, state);
            EditorUtility.SetDirty(draft);
            lastOperationResult = $"{actionLabel} region {selectedRegionId}. Accepted output is now stale until reaccepted.";
            Repaint();
        }

        private static void DrawCellLine(string label, MapGenMockupPreviewData previewData, bool hasCell, Vector2Int coord)
        {
            if (!hasCell || !previewData.TryGetCell(coord, out var cell))
            {
                EditorGUILayout.LabelField(label, "(none)");
                return;
            }

            var socketId = string.IsNullOrEmpty(cell.SocketId) ? "-" : cell.SocketId;
            var propChannel = string.IsNullOrEmpty(cell.PropChannel) ? "-" : cell.PropChannel;
            EditorGUILayout.LabelField(
                label,
                $"({coord.x}, {coord.y}) State {cell.State}, Region {cell.RegionId}, Category {cell.RoomCategory}, Socket {cell.SocketKind}/{socketId}, Prop {propChannel}");
        }

        private bool IsSelectedCellOrRegion(MapGenMockupCell cell, int x, int y)
        {
            if (!hasSelectedCell)
            {
                return false;
            }

            if (selectedRegionId >= 0)
            {
                return cell.RegionId == selectedRegionId;
            }

            return selectedCell.x == x && selectedCell.y == y;
        }

        private bool IsSelectedRegionConnector(MapGenMockupCell cell)
        {
            return selectedRegionId >= 0
                && cell.RegionId == selectedRegionId
                && cell.State == MapGenCellState.Connector;
        }

        private bool IsAdjacentRegionLink(MapGenMockupPreviewData previewData, MapGenMockupCell cell, int x, int y)
        {
            if (selectedRegionId < 0 || cell.RegionId == selectedRegionId || !IsLinkState(cell.State))
            {
                return false;
            }

            return HasSelectedNeighbor(previewData, x + 1, y)
                || HasSelectedNeighbor(previewData, x - 1, y)
                || HasSelectedNeighbor(previewData, x, y + 1)
                || HasSelectedNeighbor(previewData, x, y - 1);
        }

        private bool HasSelectedNeighbor(MapGenMockupPreviewData previewData, int x, int y)
        {
            return previewData.TryGetCell(x, y, out var neighbor)
                && neighbor.RegionId == selectedRegionId
                && IsLinkAnchorState(neighbor.State);
        }

        private int CountAdjacentRegionLinks(MapGenMockupPreviewData previewData)
        {
            if (selectedRegionId < 0)
            {
                return 0;
            }

            var count = 0;
            for (var y = 0; y < previewData.Height; y++)
            {
                for (var x = 0; x < previewData.Width; x++)
                {
                    if (!previewData.TryGetCell(x, y, out var cell))
                    {
                        continue;
                    }

                    if (IsAdjacentRegionLink(previewData, cell, x, y))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool IsLinkState(MapGenCellState state)
        {
            return state == MapGenCellState.Corridor || state == MapGenCellState.Connector;
        }

        private static bool IsLinkAnchorState(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }

        private void ClearSelection()
        {
            hasHoveredCell = false;
            hasSelectedCell = false;
            selectedRegionId = -1;
        }

        private static Color ColorForCell(MapGenCellState state)
        {
            switch (state)
            {
                case MapGenCellState.Room:
                    return RoomColor;
                case MapGenCellState.Corridor:
                case MapGenCellState.Wall:
                    return CorridorColor;
                case MapGenCellState.Blocked:
                    return BlockedColor;
                case MapGenCellState.Connector:
                    return ConnectorColor;
                case MapGenCellState.Reserved:
                    return ReservedColor;
                default:
                    return EmptyColor;
            }
        }

        private static Color ColorForProp(MapGenPlacedProp prop)
        {
            switch (prop.ChannelKind)
            {
                case MapGenPropPlacementChannelKind.Blocker:
                    return BlockerPropOverlayColor;
                case MapGenPropPlacementChannelKind.Objective:
                    return ObjectivePropOverlayColor;
                default:
                    return PropOverlayColor;
            }
        }
    }
}

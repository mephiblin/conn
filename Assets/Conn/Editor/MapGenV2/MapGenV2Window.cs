using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
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
        private const string DraftFolder = "Assets/Conn/Authoring/MapGenV2/Drafts";
        private const string MaterializedPrefabFolder = "Assets/Conn/Authoring/MapGenV2/MaterializedPrefabs";
        private const string BakedMapFolder = "Assets/Conn/Core/MapGenV2/BakedMaps";
        private static readonly Color EmptyColor = new Color(0.04f, 0.08f, 0.9f, 1f);
        private static readonly Color RoomColor = new Color(0.9f, 0f, 0f, 1f);
        private static readonly Color CorridorColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color BlockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color ConnectorColor = CorridorColor;
        private static readonly Color ReservedColor = BlockedColor;
        private static readonly Color GridLineColor = new Color(0f, 0f, 0f, 0.28f);
        private static readonly Color SelectedRegionColor = new Color(1f, 1f, 1f, 0.38f);
        private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.22f);
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
                EditorGUILayout.LabelField("Draft Folder", DraftFolder);
                EditorGUILayout.LabelField("Selected Draft", draft != null ? AssetDatabase.GetAssetPath(draft) : "(none)");
                EditorGUILayout.LabelField("Materialized Prefab Folder", MaterializedPrefabFolder);
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
            var path = AssetDatabase.GenerateUniqueAssetPath($"{MaterializedPrefabFolder}/{selectedMaterializedRoot.name}.prefab");
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
                        Undo.RecordObject(draft, "Generate Mockup");
                        var report = draft.GenerateFromProfile();
                        ClearSelection();
                        EditorUtility.SetDirty(draft);
                        var preview = MapGenMockupPreviewData.FromDraft(draft);
                        lastOperationResult = report.IsValid
                            ? $"Generated mockup. Seed {draft.Seed}, Retry 0, Rooms {preview.Summary.RoomCells}, Corridors {preview.Summary.CorridorCells}."
                            : "Generate Mockup failed. See validation messages above.";
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

        private void CreateDraft()
        {
            MapGenV2AssetFolderUtility.CreateDefaultFolders();
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Conn/Authoring/MapGenV2/Drafts/MapGenMockupDraft.asset");
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
                return BakedMapFolder;
            }

            return $"{BakedMapFolder}/{draft.Profile.ProfileId}_{draft.Seed}_BakedMap.asset";
        }

        private void SaveWindowState()
        {
            SaveAssetToEditorPrefs(ProfilePathKey, profile);
            SaveAssetToEditorPrefs(DraftPathKey, draft);
            EditorPrefs.SetFloat(PreviewCellSizeKey, previewCellSize);
            EditorPrefs.SetInt(OutputModeKey, (int)outputMode);
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
            HandlePreviewInput(previewData, rect);
            DrawPreviewCells(previewData, rect);
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
                    var cellRect = new Rect(
                        rect.x + x * previewCellSize,
                        rect.y + (previewData.Height - 1 - y) * previewCellSize,
                        Mathf.Ceil(previewCellSize),
                        Mathf.Ceil(previewCellSize));
                    EditorGUI.DrawRect(cellRect, ColorForCell(cell.State));

                    if (hasSelectedCell && IsSelectedCellOrRegion(cell, x, y))
                    {
                        EditorGUI.DrawRect(cellRect, SelectedRegionColor);
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
    }
}

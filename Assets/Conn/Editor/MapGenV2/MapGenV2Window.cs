using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public sealed class MapGenV2Window : EditorWindow
    {
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

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("MapGenV2", EditorStyles.boldLabel);
            profile = (MapGenProfileAsset)EditorGUILayout.ObjectField("Profile", profile, typeof(MapGenProfileAsset), false);
            draft = (MapGenMockupDraftAsset)EditorGUILayout.ObjectField("Draft", draft, typeof(MapGenMockupDraftAsset), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Starter Setup"))
                {
                    var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup();
                    profile = setup.Profile;
                    draft = setup.Draft;
                    Selection.activeObject = draft != null ? draft : profile;
                }

                if (GUILayout.Button("Create Default Folders"))
                {
                    MapGenV2AssetFolderUtility.CreateDefaultFolders();
                }

                if (GUILayout.Button("Create Draft"))
                {
                    CreateDraft();
                }
            }

            DrawProfileValidation();
            DrawDraftActions();
            DrawDraftSummaryAndPreview();
            EditorGUILayout.EndScrollView();
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

        private void DrawDraftActions()
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
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Mockup"))
                {
                    Undo.RecordObject(draft, "Generate Mockup");
                    draft.GenerateFromProfile();
                    ClearSelection();
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayout.Button("Run Post-Process"))
                {
                    Undo.RecordObject(draft, "Post-Process Mockup");
                    draft.ApplyPostProcessingFromProfile();
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayout.Button("Accept Mockup"))
                {
                    Undo.RecordObject(draft, "Accept Mockup");
                    draft.Accept();
                    EditorUtility.SetDirty(draft);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!draft.Accepted || !draft.IsAcceptedSignatureCurrent))
                {
                    if (GUILayout.Button("Materialize"))
                    {
                        MapGenMockupMaterializer.Materialize(draft);
                    }

                    if (GUILayout.Button("Bake Runtime"))
                    {
                        MapGenRuntimeBakeUtility.Bake(draft);
                    }
                }
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

using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenMockupDraftAsset))]
    public sealed class MapGenMockupDraftAssetEditor : UnityEditor.Editor
    {
        private static readonly Color EmptyColor = new Color(0.04f, 0.08f, 0.9f, 1f);
        private static readonly Color RoomColor = new Color(0.9f, 0f, 0f, 1f);
        private static readonly Color CorridorColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color BlockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color ConnectorColor = CorridorColor;
        private static readonly Color ReservedColor = BlockedColor;
        private static readonly Color GridLineColor = new Color(0f, 0f, 0f, 0.28f);
        private MapGenValidationReport lastGenerationReport;
        private int selectedRegionId = -1;

        public override void OnInspectorGUI()
        {
            var draft = (MapGenMockupDraftAsset)target;
            DrawPreview(draft, ref selectedRegionId);
            DrawDraftStatus(draft, selectedRegionId);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayoutButton("Generate From Profile"))
                {
                    Undo.RecordObject(draft, "Generate Mockup Draft");
                    lastGenerationReport = draft.GenerateFromProfile();
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayoutButton("Accept Mockup"))
                {
                    Undo.RecordObject(draft, "Accept Mockup Draft");
                    draft.Accept();
                    EditorUtility.SetDirty(draft);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayoutButton("Run Post-Process"))
                {
                    Undo.RecordObject(draft, "Post-Process Mockup Draft");
                    draft.ApplyPostProcessingFromProfile();
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayoutButton("Reject Mockup"))
                {
                    Undo.RecordObject(draft, "Reject Mockup Draft");
                    draft.Reject();
                    EditorUtility.SetDirty(draft);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!draft.Accepted || !draft.IsAcceptedSignatureCurrent))
                {
                    if (GUILayoutButton("Materialize"))
                    {
                        MapGenMockupMaterializer.Materialize(draft);
                    }

                    if (GUILayoutButton("Bake Runtime"))
                    {
                        MapGenRuntimeBakeUtility.Bake(draft);
                    }
                }
            }

            if (lastGenerationReport != null)
            {
                MapGenValidationReportEditorGUI.Draw(lastGenerationReport, draft, "Mockup generated.");
            }

            MapGenValidationReportEditorGUI.Draw(
                MapGenPropPlacementValidator.Validate(draft.Width, draft.Height, draft.Cells),
                draft,
                "Prop channels are valid.");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Raw Draft Data", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }

        public static string BuildInspectorSummary(MapGenMockupDraftAsset draft, int selectedRegionId)
        {
            if (draft == null)
            {
                return "Draft: (none)";
            }

            draft.EnsureCellArray();
            var builder = new StringBuilder();
            builder.Append($"Grid {draft.Width}x{draft.Height}");
            builder.Append($", Cells {CountCells(draft)}");
            builder.Append($", Regions {CountRegions(draft)}");
            builder.Append($", Selected {FormatSelectedRegion(draft, selectedRegionId)}");
            builder.Append($", Overrides {Count(draft.RegionOverrides)}");
            builder.Append($", Accepted {(draft.Accepted ? "Yes" : "No")}");
            builder.Append($", Stale {(draft.IsAcceptedSignatureCurrent ? "No" : "Yes")}");
            builder.Append($", Materialization {FormatMaterializationReadiness(draft)}");
            builder.Append($", Signature {FormatValue(draft.ComputeSignature())}");
            return builder.ToString();
        }

        public static string BuildSelectedRegionSummary(MapGenMockupDraftAsset draft, int selectedRegionId)
        {
            if (draft == null || selectedRegionId < 0)
            {
                return "Selected region: none";
            }

            draft.EnsureCellArray();
            var found = false;
            var cellCount = 0;
            var connectorCount = 0;
            var state = MapGenCellState.Empty;
            var category = MapGenRoomCategory.Main;
            var sourceTemplateId = string.Empty;
            var sourceShapeId = string.Empty;
            foreach (var cell in draft.Cells ?? System.Array.Empty<MapGenMockupCell>())
            {
                if (cell.RegionId != selectedRegionId)
                {
                    continue;
                }

                found = true;
                cellCount++;
                state = cell.State;
                category = cell.RoomCategory;
                if (cell.SocketKind != MapGenSocketKind.None)
                {
                    connectorCount++;
                }

                if (string.IsNullOrWhiteSpace(sourceTemplateId))
                {
                    sourceTemplateId = cell.SourceTemplateId;
                }

                if (string.IsNullOrWhiteSpace(sourceShapeId))
                {
                    sourceShapeId = cell.SourceShapeId;
                }
            }

            if (!found)
            {
                return $"Selected region {selectedRegionId}: not found";
            }

            var builder = new StringBuilder();
            builder.Append($"Selected region {selectedRegionId}");
            builder.Append($", State {state}");
            builder.Append($", Category {category}");
            builder.Append($", Cells {cellCount}");
            builder.Append($", Connectors {connectorCount}");
            builder.Append($", Template {FormatValue(sourceTemplateId)}");
            builder.Append($", Shape {FormatValue(sourceShapeId)}");
            if (draft.TryGetRegionOverride(selectedRegionId, out var regionOverride))
            {
                builder.Append($", Locked {(regionOverride.Locked ? "Yes" : "No")}");
                builder.Append($", Category override {(regionOverride.HasCategoryOverride ? regionOverride.CategoryOverride.ToString() : "No")}");
            }
            else
            {
                builder.Append(", Override none");
            }

            return builder.ToString();
        }

        private static void DrawDraftStatus(MapGenMockupDraftAsset draft, int selectedRegionId)
        {
            EditorGUILayout.LabelField("드래프트 상태 / Draft Status", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(BuildInspectorSummary(draft, selectedRegionId), style);
                EditorGUILayout.LabelField(BuildSelectedRegionSummary(draft, selectedRegionId), style);
                EditorGUILayout.HelpBox(
                    "Preview 셀을 클릭하면 region id, category, template/shape, lock/override 상태를 확인할 수 있습니다.",
                    MessageType.Info);
            }
        }

        private static bool GUILayoutButton(string text)
        {
            return UnityEngine.GUILayout.Button(text);
        }

        private static void DrawPreview(MapGenMockupDraftAsset draft, ref int selectedRegionId)
        {
            draft.EnsureCellArray();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("목업 프리뷰 / Mockup Preview", EditorStyles.boldLabel);

            var width = draft.Width;
            var height = draft.Height;
            var availableWidth = EditorGUIUtility.currentViewWidth - 36f;
            var cellSize = Mathf.Clamp(availableWidth / Mathf.Max(1, width), 4f, 24f);
            var previewWidth = cellSize * width;
            var previewHeight = cellSize * height;
            var rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false));

            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = (y * width) + x;
                    var cell = index >= 0 && index < draft.Cells.Length
                        ? draft.Cells[index]
                        : MapGenMockupCell.Empty;
                    var cellRect = new Rect(
                        rect.x + x * cellSize,
                        rect.y + (height - 1 - y) * cellSize,
                        Mathf.Ceil(cellSize),
                        Mathf.Ceil(cellSize));
                    EditorGUI.DrawRect(cellRect, ColorForCell(cell.State));

                    if (cellSize >= 8f)
                    {
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1f, cellRect.width, 1f), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.xMax - 1f, cellRect.y, 1f, cellRect.height), GridLineColor);
                    }

                    if (cell.RegionId == selectedRegionId && selectedRegionId >= 0)
                    {
                        Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.yellow);
                    }
                }
            }

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition))
            {
                var x = Mathf.Clamp((int)((currentEvent.mousePosition.x - rect.x) / cellSize), 0, width - 1);
                var yFromTop = Mathf.Clamp((int)((currentEvent.mousePosition.y - rect.y) / cellSize), 0, height - 1);
                var y = height - 1 - yFromTop;
                var index = (y * width) + x;
                selectedRegionId = index >= 0 && index < draft.Cells.Length ? draft.Cells[index].RegionId : -1;
                currentEvent.Use();
            }
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

        private static int CountCells(MapGenMockupDraftAsset draft)
        {
            return draft.Cells?.Length ?? 0;
        }

        private static int CountRegions(MapGenMockupDraftAsset draft)
        {
            var regionIds = new HashSet<int>();
            foreach (var cell in draft.Cells ?? System.Array.Empty<MapGenMockupCell>())
            {
                if (cell.RegionId >= 0)
                {
                    regionIds.Add(cell.RegionId);
                }
            }

            return regionIds.Count;
        }

        private static int Count(MapGenMockupRegionOverride[] overrides)
        {
            return overrides?.Length ?? 0;
        }

        private static string FormatSelectedRegion(MapGenMockupDraftAsset draft, int selectedRegionId)
        {
            return selectedRegionId >= 0 && RegionExists(draft, selectedRegionId)
                ? selectedRegionId.ToString()
                : "(none)";
        }

        private static bool RegionExists(MapGenMockupDraftAsset draft, int selectedRegionId)
        {
            foreach (var cell in draft.Cells ?? System.Array.Empty<MapGenMockupCell>())
            {
                if (cell.RegionId == selectedRegionId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatMaterializationReadiness(MapGenMockupDraftAsset draft)
        {
            if (!draft.Accepted)
            {
                return "Blocked: accept mockup first";
            }

            if (!draft.IsAcceptedSignatureCurrent)
            {
                return "Blocked: accepted mockup is stale";
            }

            return "Ready";
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}

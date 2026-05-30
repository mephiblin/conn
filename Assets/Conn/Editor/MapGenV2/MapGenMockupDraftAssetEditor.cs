using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
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
        private static readonly Color ConnectorColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color ReservedColor = new Color(1f, 0.8f, 0.1f, 1f);
        private static readonly Color GridLineColor = new Color(0f, 0f, 0f, 0.28f);
        private MapGenValidationReport lastGenerationReport;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var draft = (MapGenMockupDraftAsset)target;
            DrawPreview(draft);
            EditorGUILayout.LabelField("Current Signature", draft.ComputeSignature());
            EditorGUILayout.LabelField("Accepted Current", draft.IsAcceptedSignatureCurrent ? "Yes" : "No");

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
        }

        private static bool GUILayoutButton(string text)
        {
            return UnityEngine.GUILayout.Button(text);
        }

        private static void DrawPreview(MapGenMockupDraftAsset draft)
        {
            draft.EnsureCellArray();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mockup Preview", EditorStyles.boldLabel);

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
                }
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
    }
}

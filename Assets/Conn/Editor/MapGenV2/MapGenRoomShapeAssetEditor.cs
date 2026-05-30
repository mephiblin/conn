using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenRoomShapeAsset))]
    public sealed class MapGenRoomShapeAssetEditor : UnityEditor.Editor
    {
        private static readonly Color EmptyColor = new Color(0.04f, 0.08f, 0.9f, 1f);
        private static readonly Color RoomColor = new Color(0.9f, 0f, 0f, 1f);
        private static readonly Color ConnectorColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color BlockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color GridLineColor = new Color(0f, 0f, 0f, 0.35f);

        private MapGenCellState brushState = MapGenCellState.Room;
        private MapGenSocketKind socketKind = MapGenSocketKind.Door;
        private string socketId = string.Empty;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var shape = (MapGenRoomShapeAsset)target;

            DrawIdentityFields();
            serializedObject.ApplyModifiedProperties();
            DrawDimensionField(shape);
            DrawBrushFields();
            DrawGrid(shape);
            DrawValidation(shape);
        }

        private void DrawIdentityFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomShapeAsset.ShapeId)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomShapeAsset.Category)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomShapeAsset.Weight)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomShapeAsset.PreviewSprite)));
        }

        private static void DrawDimensionField(MapGenRoomShapeAsset shape)
        {
            EditorGUI.BeginChangeCheck();
            var dimensions = EditorGUILayout.Vector2IntField("Dimensions", shape.Dimensions);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(shape, "Resize Room Shape");
            shape.Resize(dimensions);
            EditorUtility.SetDirty(shape);
        }

        private void DrawBrushFields()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Brush", EditorStyles.boldLabel);
            brushState = (MapGenCellState)EditorGUILayout.EnumPopup("State", brushState);
            using (new EditorGUI.DisabledScope(brushState != MapGenCellState.Connector))
            {
                socketKind = (MapGenSocketKind)EditorGUILayout.EnumPopup("Socket Kind", socketKind);
                socketId = EditorGUILayout.TextField("Socket Id", socketId);
            }
        }

        private void DrawGrid(MapGenRoomShapeAsset shape)
        {
            shape.EnsureCellArray();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape Grid", EditorStyles.boldLabel);

            var width = shape.Width;
            var height = shape.Height;
            var availableWidth = EditorGUIUtility.currentViewWidth - 36f;
            var cellSize = Mathf.Clamp(availableWidth / Mathf.Max(1, width), 18f, 42f);
            var gridWidth = cellSize * width;
            var gridHeight = cellSize * height;
            var rect = GUILayoutUtility.GetRect(gridWidth, gridHeight, GUILayout.ExpandWidth(false));

            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = 0; x < width; x++)
                {
                    var cellRect = new Rect(
                        rect.x + x * cellSize,
                        rect.y + (height - 1 - y) * cellSize,
                        cellSize,
                        cellSize);
                    var cell = shape.GetCell(x, y);
                    EditorGUI.DrawRect(cellRect, ColorForCell(cell));
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), GridLineColor);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1f, cellRect.width, 1f), GridLineColor);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), GridLineColor);
                    EditorGUI.DrawRect(new Rect(cellRect.xMax - 1f, cellRect.y, 1f, cellRect.height), GridLineColor);
                }
            }

            HandleGridInput(shape, rect, cellSize);
        }

        private void HandleGridInput(MapGenRoomShapeAsset shape, Rect rect, float cellSize)
        {
            var current = Event.current;
            if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag)
            {
                return;
            }

            if (current.button != 0 || !rect.Contains(current.mousePosition))
            {
                return;
            }

            var x = Mathf.FloorToInt((current.mousePosition.x - rect.x) / cellSize);
            var y = shape.Height - 1 - Mathf.FloorToInt((current.mousePosition.y - rect.y) / cellSize);
            PaintCell(shape, x, y);
            current.Use();
        }

        private void PaintCell(MapGenRoomShapeAsset shape, int x, int y)
        {
            Undo.RecordObject(shape, "Paint Room Shape Cell");
            var cell = shape.GetCell(x, y);
            cell.State = NormalizeBrushState(brushState);
            cell.SocketKind = cell.State == MapGenCellState.Connector ? NormalizeSocketKind(socketKind) : MapGenSocketKind.None;
            cell.SocketId = cell.State == MapGenCellState.Connector ? socketId : string.Empty;
            shape.SetCell(x, y, cell);
            EditorUtility.SetDirty(shape);
        }

        private static void DrawValidation(MapGenRoomShapeAsset shape)
        {
            EditorGUILayout.Space();
            var report = shape.Validate();
            if (report.IsValid)
            {
                EditorGUILayout.HelpBox("Room shape is valid.", MessageType.Info);
                return;
            }

            foreach (var issue in report.Issues)
            {
                var cellText = issue.Cell.HasValue ? $" Cell {issue.Cell.Value}." : string.Empty;
                EditorGUILayout.HelpBox($"{issue.Message}{cellText}\nFix: {issue.SuggestedFix}", MessageType.Warning);
            }
        }

        private static MapGenCellState NormalizeBrushState(MapGenCellState state)
        {
            switch (state)
            {
                case MapGenCellState.Room:
                case MapGenCellState.Connector:
                case MapGenCellState.Blocked:
                    return state;
                default:
                    return MapGenCellState.Empty;
            }
        }

        private static MapGenSocketKind NormalizeSocketKind(MapGenSocketKind kind)
        {
            return kind == MapGenSocketKind.None || kind == MapGenSocketKind.Blocked
                ? MapGenSocketKind.Door
                : kind;
        }

        private static Color ColorForCell(MapGenShapeCell cell)
        {
            switch (cell.State)
            {
                case MapGenCellState.Room:
                    return RoomColor;
                case MapGenCellState.Connector:
                    return ConnectorColor;
                case MapGenCellState.Blocked:
                    return BlockedColor;
                default:
                    return EmptyColor;
            }
        }
    }
}

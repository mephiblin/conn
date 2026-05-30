using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
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
        private readonly MapGenV2AuthoringPreviewTextureCache previewCache = new MapGenV2AuthoringPreviewTextureCache();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var shape = (MapGenRoomShapeAsset)target;

            DrawIdentityFields();
            serializedObject.ApplyModifiedProperties();
            DrawDimensionField(shape);
            DrawTransformActions(shape);
            DrawBrushFields(shape);
            DrawCachedPreview(shape);
            DrawGrid(shape);
            DrawValidation(shape);
        }

        private void OnDisable()
        {
            previewCache.Dispose();
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

        private static void DrawTransformActions(MapGenRoomShapeAsset shape)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape Variants", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rotate 90 CW"))
                {
                    Undo.RecordObject(shape, "Rotate Room Shape");
                    shape.RotateClockwise();
                    EditorUtility.SetDirty(shape);
                }

                if (GUILayout.Button("Flip Horizontal"))
                {
                    Undo.RecordObject(shape, "Flip Room Shape Horizontal");
                    shape.FlipHorizontal();
                    EditorUtility.SetDirty(shape);
                }

                if (GUILayout.Button("Flip Vertical"))
                {
                    Undo.RecordObject(shape, "Flip Room Shape Vertical");
                    shape.FlipVertical();
                    EditorUtility.SetDirty(shape);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Rotated Variant"))
                {
                    CreateVariant(shape, "_rot90", variant => variant.RotateClockwise());
                }

                if (GUILayout.Button("Create Flipped H Variant"))
                {
                    CreateVariant(shape, "_flip_h", variant => variant.FlipHorizontal());
                }

                if (GUILayout.Button("Create Flipped V Variant"))
                {
                    CreateVariant(shape, "_flip_v", variant => variant.FlipVertical());
                }
            }
        }

        private static void CreateVariant(
            MapGenRoomShapeAsset shape,
            string suffix,
            Action<MapGenRoomShapeAsset> transform)
        {
            var sourcePath = AssetDatabase.GetAssetPath(shape);
            var folder = string.IsNullOrEmpty(sourcePath)
                ? "Assets/Conn/Authoring/MapGenV2/RoomShapes"
                : System.IO.Path.GetDirectoryName(sourcePath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(folder))
            {
                folder = "Assets/Conn/Authoring/MapGenV2/RoomShapes";
            }

            MapGenV2AssetFolderUtility.EnsureAssetFolder(folder);
            var variant = CreateInstance<MapGenRoomShapeAsset>();
            variant.CopyFrom(shape);
            variant.ShapeId = $"{shape.ShapeId}{suffix}";
            transform?.Invoke(variant);

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{shape.name}{suffix}.asset");
            AssetDatabase.CreateAsset(variant, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = variant;
        }

        private void DrawBrushFields(MapGenRoomShapeAsset shape)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Brush", EditorStyles.boldLabel);
            brushState = (MapGenCellState)EditorGUILayout.EnumPopup("State", brushState);
            using (new EditorGUI.DisabledScope(brushState != MapGenCellState.Connector))
            {
                socketKind = (MapGenSocketKind)EditorGUILayout.EnumPopup("Socket Kind", socketKind);
                socketId = EditorGUILayout.TextField("Socket Id", socketId);
            }

            DrawConnectorSideWarning(shape);
        }

        private static void DrawConnectorSideWarning(MapGenRoomShapeAsset shape)
        {
            var invalidConnectors = CountInvalidConnectorCells(shape);
            if (invalidConnectors > 0)
            {
                EditorGUILayout.HelpBox(
                    $"커넥터는 외곽 셀에만 배치할 수 있습니다. / Connectors must be on an outer edge. Invalid connectors: {invalidConnectors}.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                "커넥터 브러시는 외곽 셀에 문/복도 소켓을 표시할 때 사용합니다. / Use Connector only on edge cells for door/corridor sockets.",
                MessageType.Info);
        }

        private void DrawCachedPreview(MapGenRoomShapeAsset shape)
        {
            shape.EnsureCellArray();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Preview Cache", EditorStyles.boldLabel);
            var texture = previewCache.GetOrCreate(
                BuildPreviewKey(shape),
                shape.Width,
                shape.Height,
                (x, y) => ColorForCell(shape.GetCell(x, y)),
                "MapGenV2RoomShapePreviewCache");
            if (texture == null)
            {
                return;
            }

            var size = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.28f, 48f, 96f);
            GUILayout.Label(texture, GUILayout.Width(size), GUILayout.Height(size));
            EditorGUILayout.LabelField(
                "Cache",
                previewCache.LastRequestWasCacheHit ? "Hit / 재사용" : "Rebuilt / 갱신");
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
            MapGenValidationReportEditorGUI.Draw(shape.Validate(), shape, "Room shape is valid.");
        }

        private static int CountInvalidConnectorCells(MapGenRoomShapeAsset shape)
        {
            if (shape == null)
            {
                return 0;
            }

            var invalid = 0;
            for (var y = 0; y < shape.Height; y++)
            {
                for (var x = 0; x < shape.Width; x++)
                {
                    var cell = shape.GetCell(x, y);
                    if (cell.State != MapGenCellState.Connector)
                    {
                        continue;
                    }

                    if (x > 0 && y > 0 && x < shape.Width - 1 && y < shape.Height - 1)
                    {
                        invalid++;
                    }
                }
            }

            return invalid;
        }

        private static string BuildPreviewKey(MapGenRoomShapeAsset shape)
        {
            var key = $"{shape.ShapeId}:{shape.Category}:{shape.Weight}:{shape.Width}x{shape.Height}";
            for (var y = 0; y < shape.Height; y++)
            {
                for (var x = 0; x < shape.Width; x++)
                {
                    var cell = shape.GetCell(x, y);
                    key += $"|{x},{y},{cell.State},{cell.SocketKind},{cell.SocketId}";
                }
            }

            return key;
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

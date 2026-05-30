using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenRoomTemplateAsset))]
    public sealed class MapGenRoomTemplateAssetEditor : UnityEditor.Editor
    {
        private readonly MapGenV2AuthoringPreviewTextureCache previewCache = new MapGenV2AuthoringPreviewTextureCache();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var template = (MapGenRoomTemplateAsset)target;

            EditorGUILayout.LabelField("방 템플릿 요약 / Room Template Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Template Id", string.IsNullOrWhiteSpace(template.TemplateId) ? "(missing)" : template.TemplateId);
                EditorGUILayout.LabelField("Footprint", $"{template.Footprint.x} x {template.Footprint.y}");
                EditorGUILayout.LabelField("Category", template.RoomCategory.ToString());
                EditorGUILayout.LabelField("Floor Cells", (template.FloorCells?.Length ?? 0).ToString());
                EditorGUILayout.LabelField("Connectors", (template.Connectors?.Length ?? 0).ToString());
                EditorGUILayout.LabelField("Source Shapes", CountAssignedShapes(template).ToString());
            }

            DrawValidationSummary(template);
            DrawFootprintPreview(template);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("기본 정보 / Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.TemplateId)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.Footprint)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.RoomCategory)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.SizeClass)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.Weight)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("소스 룸 셰이프 / Source Room Shapes", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "템플릿이 어떤 룸 셰이프에서 만들어졌는지 추적합니다. / Reference the room shape assets used to author this template.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.SourceRoomShapes)), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("셀/커넥터 데이터 / Cells And Connectors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.Connectors)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.FloorCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.WallCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.BlockedCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.DoorHintCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.PropChannels)), true);

            serializedObject.ApplyModifiedProperties();
            MapGenValidationReportEditorGUI.Draw(template.Validate(), template, "Room template is valid.");
        }

        private void OnDisable()
        {
            previewCache.Dispose();
        }

        private static void DrawValidationSummary(MapGenRoomTemplateAsset template)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("검증 요약 / Validation Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var occupied = CountUniqueOccupiedCells(template);
                var invalidSockets = CountInvalidConnectors(template);
                EditorGUILayout.LabelField("Footprint / 풋프린트", $"{template.Footprint.x} x {template.Footprint.y}");
                EditorGUILayout.LabelField("Occupied Cells / 점유 셀", occupied.ToString());
                EditorGUILayout.LabelField("Connector Count / 커넥터 수", (template.Connectors?.Length ?? 0).ToString());
                EditorGUILayout.LabelField("Invalid Socket Cells / 잘못된 소켓", invalidSockets.ToString());
            }
        }

        private void DrawFootprintPreview(MapGenRoomTemplateAsset template)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("풋프린트 미리보기 / Footprint Preview", EditorStyles.boldLabel);
            var width = Mathf.Max(1, template.Footprint.x);
            var height = Mathf.Max(1, template.Footprint.y);
            var texture = previewCache.GetOrCreate(
                BuildPreviewKey(template),
                width,
                height,
                (x, y) => ColorForCell(template, new Vector2Int(x, y)),
                "MapGenV2RoomTemplatePreviewCache");
            if (texture != null)
            {
                var size = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.28f, 48f, 96f);
                GUILayout.Label(texture, GUILayout.Width(size), GUILayout.Height(size));
                EditorGUILayout.LabelField(
                    "Cache",
                    previewCache.LastRequestWasCacheHit ? "Hit / 재사용" : "Rebuilt / 갱신");
            }

            var availableWidth = EditorGUIUtility.currentViewWidth - 36f;
            var cellSize = Mathf.Clamp(availableWidth / width, 12f, 30f);
            var rect = GUILayoutUtility.GetRect(width * cellSize, height * cellSize, GUILayout.ExpandWidth(false));
            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var cellRect = new Rect(
                        rect.x + x * cellSize,
                        rect.y + (height - 1 - y) * cellSize,
                        cellSize,
                        cellSize);
                    EditorGUI.DrawRect(cellRect, ColorForCell(template, cell));
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1f, cellRect.width, 1f), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.xMax - 1f, cellRect.y, 1f, cellRect.height), Color.black);
                }
            }
        }

        private static Color ColorForCell(MapGenRoomTemplateAsset template, Vector2Int cell)
        {
            if (Contains(template.Connectors, cell))
            {
                return Color.white;
            }

            if (Contains(template.BlockedCells, cell))
            {
                return new Color(0.45f, 0.45f, 0.45f, 1f);
            }

            if (Contains(template.WallCells, cell))
            {
                return new Color(0.2f, 0.32f, 0.85f, 1f);
            }

            if (Contains(template.FloorCells, cell))
            {
                return new Color(0.9f, 0f, 0f, 1f);
            }

            return new Color(0.04f, 0.08f, 0.9f, 1f);
        }

        private static bool Contains(Vector2Int[] cells, Vector2Int cell)
        {
            foreach (var candidate in cells ?? System.Array.Empty<Vector2Int>())
            {
                if (candidate == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(MapGenConnector[] connectors, Vector2Int cell)
        {
            foreach (var connector in connectors ?? System.Array.Empty<MapGenConnector>())
            {
                if (connector.LocalCell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildPreviewKey(MapGenRoomTemplateAsset template)
        {
            return string.Join(
                "|",
                template.TemplateId,
                template.RoomCategory,
                template.SizeClass,
                template.Weight,
                template.Footprint.x,
                template.Footprint.y,
                CellsKey("floor", template.FloorCells),
                CellsKey("wall", template.WallCells),
                CellsKey("blocked", template.BlockedCells),
                CellsKey("door", template.DoorHintCells),
                ConnectorKey(template.Connectors));
        }

        private static string CellsKey(string label, Vector2Int[] cells)
        {
            var key = label;
            foreach (var cell in cells ?? System.Array.Empty<Vector2Int>())
            {
                key += $":{cell.x},{cell.y}";
            }

            return key;
        }

        private static string ConnectorKey(MapGenConnector[] connectors)
        {
            var key = "connectors";
            foreach (var connector in connectors ?? System.Array.Empty<MapGenConnector>())
            {
                key += $":{connector.LocalCell.x},{connector.LocalCell.y},{connector.Side},{connector.SocketKind},{connector.SocketId},{connector.Width}";
            }

            return key;
        }

        private static int CountUniqueOccupiedCells(MapGenRoomTemplateAsset template)
        {
            var occupied = new System.Collections.Generic.HashSet<Vector2Int>();
            AddCells(occupied, template.FloorCells);
            AddCells(occupied, template.WallCells);
            AddCells(occupied, template.BlockedCells);
            AddCells(occupied, template.DoorHintCells);
            return occupied.Count;
        }

        private static void AddCells(System.Collections.Generic.HashSet<Vector2Int> occupied, Vector2Int[] cells)
        {
            foreach (var cell in cells ?? System.Array.Empty<Vector2Int>())
            {
                occupied.Add(cell);
            }
        }

        private static int CountInvalidConnectors(MapGenRoomTemplateAsset template)
        {
            var invalid = 0;
            for (var i = 0; i < (template.Connectors?.Length ?? 0); i++)
            {
                var connector = template.Connectors[i];
                if (!MapGenTemplateValidationUtility.IsInBounds(connector.LocalCell, template.Footprint)
                    || !IsOnSide(connector.LocalCell, template.Footprint, connector.Side)
                    || connector.SocketKind == MapGenSocketKind.None
                    || connector.Width <= 0)
                {
                    invalid++;
                }
            }

            return invalid;
        }

        private static bool IsOnSide(Vector2Int cell, Vector2Int footprint, MapGenGridDirection side)
        {
            switch (side)
            {
                case MapGenGridDirection.North:
                    return cell.y == footprint.y - 1;
                case MapGenGridDirection.East:
                    return cell.x == footprint.x - 1;
                case MapGenGridDirection.South:
                    return cell.y == 0;
                case MapGenGridDirection.West:
                    return cell.x == 0;
                default:
                    return false;
            }
        }

        private static int CountAssignedShapes(MapGenRoomTemplateAsset template)
        {
            var count = 0;
            foreach (var shape in template.SourceRoomShapes ?? System.Array.Empty<MapGenRoomShapeAsset>())
            {
                if (shape != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

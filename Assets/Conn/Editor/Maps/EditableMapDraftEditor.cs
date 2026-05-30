using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    [CustomEditor(typeof(EditableMapDraftAsset))]
    public sealed class EditableMapDraftEditor : UnityEditor.Editor
    {
        private static readonly Color HoverColor = new Color(0.2f, 0.7f, 1f, 0.18f);
        private static readonly Color ValidationColor = new Color(1f, 0.2f, 0.2f, 0.18f);
        private static readonly Color FloorColor = new Color(0.34f, 0.38f, 0.37f, 1f);
        private static readonly Color WallColor = new Color(0.1f, 0.11f, 0.11f, 1f);
        private static readonly Color GapColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color SlopeColor = new Color(0.53f, 0.48f, 0.31f, 1f);
        private static readonly Color StairColor = new Color(0.42f, 0.46f, 0.57f, 1f);
        private static readonly Color GridLineColor = new Color(0f, 0f, 0f, 0.22f);

        private enum BrushMode
        {
            Terrain = 0,
            Height = 1,
            Material = 2,
            Object = 3,
            RoomSocket = 4,
            ValidationOverlay = 5
        }

        private BrushMode brushMode;
        private RoomChunkCellType selectedTerrain = RoomChunkCellType.Floor;
        private string selectedMaterialId = "stone";
        private string selectedObjectPaletteId = string.Empty;
        private int brushSize = 1;
        private MapDirection direction = MapDirection.North;
        private int targetHeight;
        private int heightDelta = 1;
        private bool useHeightDelta;
        private bool eraseMode;
        private int cellX;
        private int cellY;
        private bool scenePaintEnabled = true;
        private bool scenePaintActive;
        private bool previewPaintActive;
        private bool showRawData;
        private Vector2Int hoveredCell = new Vector2Int(-1, -1);
        private Vector2Int lastScenePaintCell = new Vector2Int(int.MinValue, int.MinValue);
        private Vector2Int lastPreviewPaintCell = new Vector2Int(int.MinValue, int.MinValue);
        private MapValidationReport lastValidationReport;

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var draft = (EditableMapDraftAsset)target;

            DrawMapPreview(draft);
            DrawDraftSummary(draft);
            DrawDraftActions(draft);
            DrawBrushControls(draft);
            DrawValidationReport();
            DrawAuthoringFields();
            DrawRawDataFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMapPreview(EditableMapDraftAsset draft)
        {
            EditorGUILayout.LabelField("Map Preview", EditorStyles.boldLabel);
            var previewWidth = EditorGUIUtility.currentViewWidth - 36f;
            var aspect = draft.Width > 0 ? draft.Height / (float)draft.Width : 1f;
            var previewHeight = Mathf.Clamp(previewWidth * aspect, 96f, 280f);
            var rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

            if (draft.Cells == null || draft.Cells.Length == 0 || draft.Width <= 0 || draft.Height <= 0)
            {
                EditorGUI.LabelField(rect, "No cells", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var cellWidth = rect.width / draft.Width;
            var cellHeight = rect.height / draft.Height;
            foreach (var cell in draft.Cells)
            {
                if (!draft.IsInBounds(cell.X, cell.Y))
                {
                    continue;
                }

                var cellRect = new Rect(
                    rect.x + cell.X * cellWidth,
                    rect.y + (draft.Height - cell.Y - 1) * cellHeight,
                    Mathf.Ceil(cellWidth),
                    Mathf.Ceil(cellHeight));
                EditorGUI.DrawRect(cellRect, ColorForTerrain(cell.Terrain));
            }

            if (draft.Width <= 96 && draft.Height <= 64)
            {
                DrawPreviewGrid(rect, draft.Width, draft.Height, cellWidth, cellHeight);
            }

            HandlePreviewPainting(draft, rect, Event.current);
        }

        public static bool TryGetCellFromPreviewRect(
            Rect previewRect,
            int width,
            int height,
            Vector2 mousePosition,
            out Vector2Int cell)
        {
            cell = default;
            if (width <= 0 || height <= 0 || previewRect.width <= 0f || previewRect.height <= 0f)
            {
                return false;
            }

            if (!previewRect.Contains(mousePosition))
            {
                return false;
            }

            var normalizedX = Mathf.Clamp01((mousePosition.x - previewRect.x) / previewRect.width);
            var normalizedY = Mathf.Clamp01((mousePosition.y - previewRect.y) / previewRect.height);
            var x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * width), 0, width - 1);
            var y = Mathf.Clamp(height - 1 - Mathf.FloorToInt(normalizedY * height), 0, height - 1);
            cell = new Vector2Int(x, y);
            return true;
        }

        private static void DrawPreviewGrid(Rect rect, int width, int height, float cellWidth, float cellHeight)
        {
            for (var x = 0; x <= width; x++)
            {
                var line = new Rect(rect.x + x * cellWidth, rect.y, 1f, rect.height);
                EditorGUI.DrawRect(line, GridLineColor);
            }

            for (var y = 0; y <= height; y++)
            {
                var line = new Rect(rect.x, rect.y + y * cellHeight, rect.width, 1f);
                EditorGUI.DrawRect(line, GridLineColor);
            }
        }

        private static Color ColorForTerrain(RoomChunkCellType terrain)
        {
            switch (terrain)
            {
                case RoomChunkCellType.Floor:
                    return FloorColor;
                case RoomChunkCellType.Wall:
                    return WallColor;
                case RoomChunkCellType.Slope:
                    return SlopeColor;
                case RoomChunkCellType.Stair:
                    return StairColor;
                default:
                    return GapColor;
            }
        }

        private void DrawDraftSummary(EditableMapDraftAsset draft)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Draft", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Id", string.IsNullOrWhiteSpace(draft.Id) ? "(none)" : draft.Id);
            EditorGUILayout.LabelField("Size", $"{draft.Width} x {draft.Height} cells");
            EditorGUILayout.LabelField("Rooms", (draft.Rooms?.Length ?? 0).ToString());
            EditorGUILayout.LabelField("Objects", (draft.Objects?.Length ?? 0).ToString());
            EditorGUILayout.LabelField("Sockets", (draft.Sockets?.Length ?? 0).ToString());
        }

        private void DrawDraftActions(EditableMapDraftAsset draft)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Draft Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Playable From Drawing"))
                {
                    try
                    {
                        Undo.RecordObject(draft, "Build Playable Editable Map Metadata");
                        EditableMapDraftMetadataBuilder.BuildPlayableMetadataFromDrawing(draft);
                        EditorUtility.SetDirty(draft);
                        ValidateDraft(draft);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                if (GUILayout.Button("Reinitialize Blank Grid"))
                {
                    Undo.RecordObject(draft, "Reinitialize Editable Map Draft");
                    draft.InitializeBlank(draft.Width, draft.Height, draft.CellSize, draft.HeightStep);
                    EditorUtility.SetDirty(draft);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Scene Map"))
                {
                    EditableMapPreviewMeshBuilder.RebuildPreview(draft);
                }

                if (GUILayout.Button("Validate"))
                {
                    ValidateDraft(draft);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bake Runtime Map"))
                {
                    try
                    {
                        var compiled = EditableMapBakeService.Bake(draft);
                        Debug.Log($"Baked editable map draft {draft.name} to runtime payload with {compiled.Cells.Count} cells and {compiled.Objects.Count} objects.");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                if (GUILayout.Button("Save Compiled Map Asset"))
                {
                    try
                    {
                        var asset = EditableMapBakeService.SaveCompiledMapAsset(draft);
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Preview"))
                {
                    EditableMapPreviewMeshBuilder.ClearPreview(draft);
                }

                if (GUILayout.Button("Clear Grid"))
                {
                    ClearGrid(draft);
                }

                if (GUILayout.Button("Ping Draft Folder"))
                {
                    var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(EditableMapDraftAsset.DefaultDraftFolder);
                    if (folder != null)
                    {
                        EditorGUIUtility.PingObject(folder);
                    }
                }
            }
        }

        private void DrawBrushControls(EditableMapDraftAsset draft)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            brushMode = (BrushMode)EditorGUILayout.EnumPopup("Mode", brushMode);
            selectedTerrain = (RoomChunkCellType)EditorGUILayout.EnumPopup("Terrain", selectedTerrain);
            selectedMaterialId = EditorGUILayout.TextField("Material Id", selectedMaterialId ?? string.Empty);
            selectedObjectPaletteId = EditorGUILayout.TextField("Object Palette Id", selectedObjectPaletteId ?? string.Empty);
            brushSize = Mathf.Max(1, EditorGUILayout.IntField("Brush Size", brushSize));
            direction = (MapDirection)EditorGUILayout.EnumPopup("Direction", direction);
            targetHeight = EditorGUILayout.IntField("Absolute Height", targetHeight);
            useHeightDelta = EditorGUILayout.Toggle("Use Height Delta", useHeightDelta);
            heightDelta = EditorGUILayout.IntField("Height Delta", heightDelta);
            eraseMode = EditorGUILayout.Toggle("Erase Mode", eraseMode);
            cellX = EditorGUILayout.IntField("Cell X", cellX);
            cellY = EditorGUILayout.IntField("Cell Y", cellY);
            scenePaintEnabled = EditorGUILayout.Toggle("Scene Paint Enabled", scenePaintEnabled);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Brush To Cell"))
                {
                    PaintBrush(draft, cellX, cellY);
                }

                if (GUILayout.Button("Fill Grid"))
                {
                    FillGrid(draft);
                }
            }
        }

        private void DrawValidationReport()
        {
            if (lastValidationReport == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            if (lastValidationReport.Passed)
            {
                EditorGUILayout.HelpBox("Validation passed.", MessageType.Info);
            }

            foreach (var error in lastValidationReport.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (var warning in lastValidationReport.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawAuthoringFields()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authoring Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Id)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.SourceProfileId)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Seed)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Floor)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Difficulty)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Version)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Width)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Height)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.CellSize)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.HeightStep)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.TilePalette)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.ObjectPalette)));
        }

        private void DrawRawDataFoldout()
        {
            EditorGUILayout.Space();
            showRawData = EditorGUILayout.Foldout(showRawData, "Raw Data", true);
            if (!showRawData)
            {
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Cells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Objects)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Rooms)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Zones)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(EditableMapDraftAsset.Sockets)), true);
        }

        private void ValidateDraft(EditableMapDraftAsset draft)
        {
            lastValidationReport = EditableMapValidationService.Validate(draft);
            SceneView.RepaintAll();
        }

        private void HandlePreviewPainting(EditableMapDraftAsset draft, Rect previewRect, Event currentEvent)
        {
            if (currentEvent == null)
            {
                return;
            }

            if (!TryGetCellFromPreviewRect(previewRect, draft.Width, draft.Height, currentEvent.mousePosition, out var cell))
            {
                if (currentEvent.type == EventType.MouseUp)
                {
                    previewPaintActive = false;
                    lastPreviewPaintCell = new Vector2Int(int.MinValue, int.MinValue);
                }

                return;
            }

            cellX = cell.x;
            cellY = cell.y;
            if (currentEvent.button != 0 || currentEvent.alt)
            {
                return;
            }

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    previewPaintActive = true;
                    lastPreviewPaintCell = new Vector2Int(int.MinValue, int.MinValue);
                    PaintPreviewCellIfNeeded(draft, cell);
                    currentEvent.Use();
                    break;
                case EventType.MouseDrag:
                    if (!previewPaintActive)
                    {
                        break;
                    }

                    PaintPreviewCellIfNeeded(draft, cell);
                    currentEvent.Use();
                    break;
                case EventType.MouseUp:
                    previewPaintActive = false;
                    lastPreviewPaintCell = new Vector2Int(int.MinValue, int.MinValue);
                    currentEvent.Use();
                    break;
            }
        }

        private void PaintPreviewCellIfNeeded(EditableMapDraftAsset draft, Vector2Int cell)
        {
            if (lastPreviewPaintCell == cell)
            {
                return;
            }

            PaintBrush(draft, cell.x, cell.y);
            lastPreviewPaintCell = cell;
            Repaint();
        }

        private void PaintBrush(EditableMapDraftAsset draft, int centerX, int centerY)
        {
            Undo.RecordObject(draft, "Paint Editable Map Draft");
            var radius = brushSize - 1;
            for (var y = centerY - radius; y <= centerY + radius; y++)
            {
                for (var x = centerX - radius; x <= centerX + radius; x++)
                {
                    ApplyBrushToCell(draft, x, y);
                }
            }

            EditorUtility.SetDirty(draft);
        }

        private void FillGrid(EditableMapDraftAsset draft)
        {
            Undo.RecordObject(draft, "Fill Editable Map Draft");
            for (var y = 0; y < draft.Height; y++)
            {
                for (var x = 0; x < draft.Width; x++)
                {
                    ApplyBrushToCell(draft, x, y);
                }
            }

            EditorUtility.SetDirty(draft);
        }

        private void ClearGrid(EditableMapDraftAsset draft)
        {
            Undo.RecordObject(draft, "Clear Editable Map Draft");
            draft.InitializeBlank(draft.Width, draft.Height, draft.CellSize, draft.HeightStep);
            EditorUtility.SetDirty(draft);
        }

        private void ApplyBrushToCell(EditableMapDraftAsset draft, int x, int y)
        {
            if (!draft.TryGetCell(x, y, out var cell))
            {
                return;
            }

            cell.X = x;
            cell.Y = y;

            switch (brushMode)
            {
                case BrushMode.Height:
                    cell.Height = useHeightDelta ? cell.Height + heightDelta : targetHeight;
                    break;
                case BrushMode.Material:
                    cell.MaterialId = eraseMode ? string.Empty : (selectedMaterialId ?? string.Empty);
                    break;
                case BrushMode.Object:
                    StampObject(draft, x, y);
                    return;
                case BrushMode.RoomSocket:
                case BrushMode.ValidationOverlay:
                    break;
                default:
                    cell.Terrain = eraseMode ? RoomChunkCellType.Gap : selectedTerrain;
                    cell.Direction = direction;
                    if (!eraseMode)
                    {
                        cell.MaterialId = selectedMaterialId ?? string.Empty;
                    }

                    if (!useHeightDelta)
                    {
                        cell.Height = targetHeight;
                    }
                    else
                    {
                        cell.Height += heightDelta;
                    }
                    break;
            }

            draft.TrySetCell(cell);
        }

        private void StampObject(EditableMapDraftAsset draft, int x, int y)
        {
            var objects = new List<EditableMapObjectPlacement>(draft.Objects ?? Array.Empty<EditableMapObjectPlacement>());
            objects.RemoveAll(placement => placement.X == x && placement.Y == y);
            if (!eraseMode)
            {
                var entry = draft.ObjectPalette?.Objects?.FirstOrDefault(candidate => candidate != null && candidate.Id == selectedObjectPaletteId);
                if (entry != null)
                {
                    objects.Add(new EditableMapObjectPlacement
                    {
                        Id = $"{entry.Id}_{x}_{y}",
                        PaletteObjectId = entry.Id,
                        Kind = entry.Kind,
                        X = x,
                        Y = y,
                        Height = targetHeight,
                        Width = Mathf.Max(1, entry.FootprintWidth),
                        Depth = Mathf.Max(1, entry.FootprintDepth),
                        Direction = direction,
                        BlocksMovement = entry.BlocksMovement,
                        RuntimeReferenceId = entry.RuntimeReferenceId
                    });
                }
            }

            draft.Objects = objects.ToArray();
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            if (Selection.activeObject != target || !scenePaintEnabled)
            {
                return;
            }

            var draft = target as EditableMapDraftAsset;
            if (draft == null)
            {
                return;
            }

            var currentEvent = Event.current;
            if (TryGetHoveredCell(draft, currentEvent, out var cell))
            {
                hoveredCell = cell;
                cellX = cell.x;
                cellY = cell.y;
                DrawCellHighlight(draft, cell, HoverColor);

                if (brushMode == BrushMode.ValidationOverlay && lastValidationReport != null)
                {
                    DrawValidationMarkers(draft, lastValidationReport);
                }

                HandleScenePainting(draft, currentEvent, cell);
                Repaint();
            }
            else if (brushMode == BrushMode.ValidationOverlay && lastValidationReport != null)
            {
                DrawValidationMarkers(draft, lastValidationReport);
            }
        }

        private bool TryGetHoveredCell(EditableMapDraftAsset draft, Event currentEvent, out Vector2Int cell)
        {
            cell = default;
            if (currentEvent == null)
            {
                return false;
            }

            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out var distance))
            {
                return false;
            }

            var world = ray.GetPoint(distance);
            return EditableMapDraftSceneTools.TryGetCellFromWorld(draft, world, out cell);
        }

        private void HandleScenePainting(EditableMapDraftAsset draft, Event currentEvent, Vector2Int cell)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            if (currentEvent.button != 0 || currentEvent.alt)
            {
                return;
            }

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    scenePaintActive = true;
                    lastScenePaintCell = new Vector2Int(int.MinValue, int.MinValue);
                    PaintSceneCellIfNeeded(draft, cell);
                    currentEvent.Use();
                    break;
                case EventType.MouseDrag:
                    if (!scenePaintActive)
                    {
                        break;
                    }

                    PaintSceneCellIfNeeded(draft, cell);
                    currentEvent.Use();
                    break;
                case EventType.MouseUp:
                    scenePaintActive = false;
                    lastScenePaintCell = new Vector2Int(int.MinValue, int.MinValue);
                    currentEvent.Use();
                    break;
            }
        }

        private void PaintSceneCellIfNeeded(EditableMapDraftAsset draft, Vector2Int cell)
        {
            if (lastScenePaintCell == cell)
            {
                return;
            }

            PaintBrush(draft, cell.x, cell.y);
            lastScenePaintCell = cell;
        }

        private static void DrawCellHighlight(EditableMapDraftAsset draft, Vector2Int cell, Color color)
        {
            var cellSize = Mathf.Max(0.01f, draft.CellSize);
            var center = new Vector3(
                cell.x * cellSize + cellSize * 0.5f,
                0.02f,
                cell.y * cellSize + cellSize * 0.5f);
            var size = new Vector3(cellSize, 0.01f, cellSize);
            Handles.color = color;
            Handles.DrawSolidRectangleWithOutline(new[]
            {
                center + new Vector3(-size.x * 0.5f, 0f, -size.z * 0.5f),
                center + new Vector3(-size.x * 0.5f, 0f, size.z * 0.5f),
                center + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f),
                center + new Vector3(size.x * 0.5f, 0f, -size.z * 0.5f)
            }, color, Color.cyan);
            Handles.Label(center + Vector3.up * 0.05f, $"Cell {cell.x},{cell.y}");
        }

        private static void DrawValidationMarkers(EditableMapDraftAsset draft, MapValidationReport report)
        {
            foreach (var marker in EditableMapDraftSceneTools.BuildValidationMarkers(draft, report))
            {
                DrawCellHighlight(draft, marker.Position, ValidationColor);
                var cellSize = Mathf.Max(0.01f, draft.CellSize);
                var position = new Vector3(
                    marker.Position.x * cellSize + cellSize * 0.5f,
                    0.15f,
                    marker.Position.y * cellSize + cellSize * 0.5f);
                Handles.color = Color.red;
                Handles.Label(position, marker.Label);
            }
        }
    }
}

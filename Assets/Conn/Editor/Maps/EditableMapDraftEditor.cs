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
        private MapValidationReport lastValidationReport;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var draft = (EditableMapDraftAsset)target;

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

            EditorGUILayout.LabelField("Draft Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reinitialize Blank Grid"))
                {
                    Undo.RecordObject(draft, "Reinitialize Editable Map Draft");
                    draft.InitializeBlank(draft.Width, draft.Height, draft.CellSize, draft.HeightStep);
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayout.Button("Rebuild Preview"))
                {
                    EditableMapPreviewMeshBuilder.RebuildPreview(draft);
                }

                if (GUILayout.Button("Validate"))
                {
                    lastValidationReport = EditableMapValidationService.Validate(draft);
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

            if (lastValidationReport != null)
            {
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

            serializedObject.ApplyModifiedProperties();
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
    }
}

using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Room Shape", fileName = "MapGenRoomShape")]
    public sealed class MapGenRoomShapeAsset : ScriptableObject
    {
        public string ShapeId = string.Empty;
        public Vector2Int Dimensions = new Vector2Int(3, 3);
        public MapGenShapeCell[] Cells = Array.Empty<MapGenShapeCell>();
        public Texture2D PreviewSprite;
        public MapGenRoomCategory Category = MapGenRoomCategory.Main;
        public int Weight = 1;

        public int Width => Mathf.Max(1, Dimensions.x);

        public int Height => Mathf.Max(1, Dimensions.y);

        public MapGenValidationReport Validate()
        {
            EnsureCellArray();
            return MapGenRoomShapeValidator.Validate(Width, Height, Cells);
        }

        public MapGenShapeCell GetCell(int x, int y)
        {
            EnsureCellArray();
            if (!IsInBounds(x, y))
            {
                return MapGenShapeCell.Empty;
            }

            return Cells[(y * Width) + x];
        }

        public void SetCell(int x, int y, MapGenShapeCell cell)
        {
            EnsureCellArray();
            if (!IsInBounds(x, y))
            {
                return;
            }

            Cells[(y * Width) + x] = cell;
        }

        public void Resize(Vector2Int dimensions)
        {
            var newWidth = Mathf.Max(1, dimensions.x);
            var newHeight = Mathf.Max(1, dimensions.y);
            var oldWidth = Width;
            var oldHeight = Height;
            var oldCells = Cells ?? Array.Empty<MapGenShapeCell>();
            var newCells = CreateEmptyCells(newWidth, newHeight);

            var copyWidth = Mathf.Min(oldWidth, newWidth);
            var copyHeight = Mathf.Min(oldHeight, newHeight);
            for (var y = 0; y < copyHeight; y++)
            {
                for (var x = 0; x < copyWidth; x++)
                {
                    var oldIndex = (y * oldWidth) + x;
                    var newIndex = (y * newWidth) + x;
                    if (oldIndex >= 0 && oldIndex < oldCells.Length)
                    {
                        newCells[newIndex] = oldCells[oldIndex];
                    }
                }
            }

            Dimensions = new Vector2Int(newWidth, newHeight);
            Cells = newCells;
        }

        public void EnsureCellArray()
        {
            var width = Width;
            var height = Height;
            if (Cells != null && Cells.Length == width * height)
            {
                return;
            }

            Resize(new Vector2Int(width, height));
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        private void OnValidate()
        {
            if (Weight < 0)
            {
                Weight = 0;
            }

            EnsureCellArray();
        }

        private static MapGenShapeCell[] CreateEmptyCells(int width, int height)
        {
            var cells = new MapGenShapeCell[width * height];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenShapeCell.Empty;
            }

            return cells;
        }
    }
}

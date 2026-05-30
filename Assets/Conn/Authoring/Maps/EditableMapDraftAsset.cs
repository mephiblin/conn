using Conn.Core.Maps;
using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Editable Map Draft", fileName = "EditableMapDraft")]
    public sealed class EditableMapDraftAsset : ScriptableObject
    {
        public const string DefaultDraftFolder = "Assets/Conn/Authoring/Maps/Drafts";

        [Header("Identity")]
        public string Id = string.Empty;
        public string SourceProfileId = string.Empty;
        public int Seed;
        public int Floor = 1;
        public int Difficulty;
        public int Version = 1;

        [Header("Dimensions")]
        public int Width = 1;
        public int Height = 1;
        public float CellSize = 1f;
        public float HeightStep = 1f;

        [Header("Palettes")]
        public MapTilePaletteAsset TilePalette;
        public MapObjectPaletteAsset ObjectPalette;

        [Header("Layers")]
        public EditableMapCell[] Cells = Array.Empty<EditableMapCell>();
        public EditableMapObjectPlacement[] Objects = Array.Empty<EditableMapObjectPlacement>();
        public EditableMapRoom[] Rooms = Array.Empty<EditableMapRoom>();
        public EditableMapZone[] Zones = Array.Empty<EditableMapZone>();
        public EditableMapSocket[] Sockets = Array.Empty<EditableMapSocket>();

        public int CellCount => Cells?.Length ?? 0;

        public void InitializeBlank(
            int width,
            int height,
            float cellSize,
            float heightStep)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            CellSize = Mathf.Max(0.01f, cellSize);
            HeightStep = Mathf.Max(0.01f, heightStep);
            Cells = new EditableMapCell[Width * Height];
            Objects = Array.Empty<EditableMapObjectPlacement>();
            Rooms = Array.Empty<EditableMapRoom>();
            Zones = Array.Empty<EditableMapZone>();
            Sockets = Array.Empty<EditableMapSocket>();

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    Cells[GetIndexUnchecked(x, y)] = EditableMapCell.CreateDefault(x, y);
                }
            }
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public bool TryGetIndex(int x, int y, out int index)
        {
            if (!IsInBounds(x, y))
            {
                index = -1;
                return false;
            }

            index = GetIndexUnchecked(x, y);
            return true;
        }

        public int GetIndex(int x, int y)
        {
            if (!TryGetIndex(x, y, out var index))
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x}, {y}) is outside {Width}x{Height}.");
            }

            return index;
        }

        public bool TryGetCell(int x, int y, out EditableMapCell cell)
        {
            if (!TryGetIndex(x, y, out var index) || Cells == null || index >= Cells.Length)
            {
                cell = default;
                return false;
            }

            cell = Cells[index];
            return true;
        }

        public EditableMapCell GetCell(int x, int y)
        {
            return Cells[GetIndex(x, y)];
        }

        public bool TrySetCell(EditableMapCell cell)
        {
            if (!TryGetIndex(cell.X, cell.Y, out var index) || Cells == null || index >= Cells.Length)
            {
                return false;
            }

            Cells[index] = cell;
            return true;
        }

        private int GetIndexUnchecked(int x, int y)
        {
            return y * Width + x;
        }
    }

    [Serializable]
    public struct EditableMapCell
    {
        public int X;
        public int Y;
        public string RoomId;
        public string ZoneId;
        public RoomChunkCellType Terrain;
        public int Height;
        public MapDirection Direction;
        public string MaterialId;
        public string FloorVariantId;
        public string WallVariantId;
        public int Flags;

        public static EditableMapCell CreateDefault(int x, int y)
        {
            return new EditableMapCell
            {
                X = x,
                Y = y,
                RoomId = string.Empty,
                ZoneId = string.Empty,
                Terrain = RoomChunkCellType.Gap,
                Height = 0,
                Direction = MapDirection.North,
                MaterialId = string.Empty,
                FloorVariantId = string.Empty,
                WallVariantId = string.Empty,
                Flags = 0
            };
        }
    }

    [Serializable]
    public struct EditableMapObjectPlacement
    {
        public string Id;
        public string PaletteObjectId;
        public RoomChunkObjectKind Kind;
        public int X;
        public int Y;
        public int Height;
        public int Width;
        public int Depth;
        public MapDirection Direction;
        public bool BlocksMovement;
        public string RuntimeReferenceId;
        public string MaterialId;
    }

    [Serializable]
    public struct EditableMapRoom
    {
        public string Id;
        public MapRoomRole Role;
        public RoomChunkLayoutKind LayoutKind;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public MapDirection SocketMask;
        public int HeightLevel;
        public string ZoneId;
        public string ChunkId;
    }

    [Serializable]
    public struct EditableMapZone
    {
        public string Id;
        public string ThemeId;
        public int IntendedDifficulty;
        public string Purpose;
    }

    [Serializable]
    public struct EditableMapSocket
    {
        public string Id;
        public string RoomId;
        public int X;
        public int Y;
        public MapDirection Direction;
        public int Width;
        public string TargetRoomId;
        public string LockedDoorKeyId;
    }
}

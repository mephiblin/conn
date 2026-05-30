using Conn.Core.Maps;
using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Map Tile Palette", fileName = "MapTilePalette")]
    public sealed class MapTilePaletteAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string ThemeId = string.Empty;
        public MapTilePaletteEntry[] Tiles = Array.Empty<MapTilePaletteEntry>();
    }

    [Serializable]
    public sealed class MapTilePaletteEntry
    {
        public string Id = string.Empty;
        public RoomChunkCellType TerrainType = RoomChunkCellType.Floor;
        public Material EditorMaterial;
        public string RuntimeMaterialId = string.Empty;
        public bool DefaultWalkable = true;
        public int DefaultHeightCost = 1;
    }
}

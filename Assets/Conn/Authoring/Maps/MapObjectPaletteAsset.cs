using Conn.Core.Maps;
using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Map Object Palette", fileName = "MapObjectPalette")]
    public sealed class MapObjectPaletteAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string ThemeId = string.Empty;
        public MapObjectPaletteEntry[] Objects = Array.Empty<MapObjectPaletteEntry>();
    }

    [Serializable]
    public sealed class MapObjectPaletteEntry
    {
        public string Id = string.Empty;
        public RoomChunkObjectKind Kind;
        public GameObject Prefab;
        public Material PreviewMaterial;
        public int FootprintWidth = 1;
        public int FootprintDepth = 1;
        public bool BlocksMovement;
        public string RuntimeReferenceId = string.Empty;
    }
}

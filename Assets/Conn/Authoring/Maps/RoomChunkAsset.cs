using Conn.Core.Maps;
using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Room Chunk", fileName = "RoomChunk")]
    public class RoomChunkAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string ThemeId = string.Empty;
        public Vector2Int Size = new Vector2Int(8, 8);
        public RoomChunkLayoutKind LayoutKind = RoomChunkLayoutKind.Room;
        public int CorridorLength;
        public int CorridorWidth;
        public int DeadEndDepth;
        public MapDirection OpenSides;
        public MapDirection DoorSockets;
        public RoomChunkSocketDefinition[] SocketDefinitions = Array.Empty<RoomChunkSocketDefinition>();
        public bool PopulationAllowed = true;
        public string[] RoleTags = Array.Empty<string>();
        public AuthoringChunkAnchor[] Anchors = Array.Empty<AuthoringChunkAnchor>();
        public RoomChunkCell[] Cells = Array.Empty<RoomChunkCell>();
        public RoomChunkObjectPlacement[] Objects = Array.Empty<RoomChunkObjectPlacement>();
        public GameObject RoomPrefab;
        public UnityEngine.Object TilemapReference;
        public Texture2D PreviewThumbnail;
    }
}

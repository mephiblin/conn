using Conn.Core.Maps;
using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [Serializable]
    public sealed class MapRoomPoolRule
    {
        public MapRoomPoolRole Role;
        public RoomChunkLayoutKind LayoutKind = RoomChunkLayoutKind.Room;
        public int MinCount;
        public int MaxCount;
        public int Weight = 1;
        public bool Required;
        public RoomChunkAsset[] AllowedChunks = Array.Empty<RoomChunkAsset>();
    }
}

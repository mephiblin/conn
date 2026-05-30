using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenMockupCell
    {
        public MapGenCellState State;
        public int RegionId;
        public MapGenRoomCategory RoomCategory;
        public MapGenSocketKind SocketKind;
        public string SocketId;
        public string PropChannel;

        public static MapGenMockupCell Empty => new MapGenMockupCell
        {
            State = MapGenCellState.Empty,
            RegionId = -1,
            RoomCategory = MapGenRoomCategory.Main,
            SocketKind = MapGenSocketKind.None,
            SocketId = string.Empty,
            PropChannel = string.Empty
        };
    }
}

using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenBakedCell
    {
        public MapGenGridCoord Coord;
        public MapGenCellState State;
        public int RegionId;
        public MapGenRoomCategory RoomCategory;
    }
}

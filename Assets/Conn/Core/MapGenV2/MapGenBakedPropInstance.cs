using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenBakedPropInstance
    {
        public MapGenGridCoord Coord;
        public string Channel;
        public int RegionId;
        public MapGenRoomCategory RoomCategory;
        public string ChannelKind;
        public string DistributionMode;
        public bool BlocksTraversal;
    }
}

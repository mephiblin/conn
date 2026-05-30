using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenBakedMarker
    {
        public string MarkerId;
        public MapGenGridCoord Coord;
        public int RegionId;
        public string Channel;
    }
}

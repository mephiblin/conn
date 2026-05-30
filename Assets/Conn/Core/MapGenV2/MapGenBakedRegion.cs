using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenBakedRegion
    {
        public int RegionId;
        public MapGenRoomCategory RoomCategory;
        public int CellCount;
        public string SourceTemplateId;
        public string SourceShapeId;
    }
}

using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenModuleRequest
    {
        public MapGenModuleCategory Category;
        public MapGenGridCoord Coord;
        public MapGenGridDirection Direction;
        public int RegionId;
        public string SourceTemplateId;
    }
}

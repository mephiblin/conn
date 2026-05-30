using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenBakedConnector
    {
        public MapGenGridCoord Coord;
        public int RegionId;
        public MapGenSocketKind SocketKind;
        public string SocketId;
        public int SocketWidth;
        public string SourceTemplateId;
    }
}

using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenTraversalEdge
    {
        public MapGenGridCoord From;
        public MapGenGridCoord To;
    }
}

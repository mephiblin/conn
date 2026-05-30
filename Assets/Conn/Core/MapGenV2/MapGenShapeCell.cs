using System;

namespace Conn.MapGenV2.Core
{
    [Serializable]
    public struct MapGenShapeCell
    {
        public MapGenCellState State;
        public MapGenSocketKind SocketKind;
        public string SocketId;
        public string[] Tags;

        public static MapGenShapeCell Empty => new MapGenShapeCell
        {
            State = MapGenCellState.Empty,
            SocketKind = MapGenSocketKind.None,
            SocketId = string.Empty,
            Tags = Array.Empty<string>()
        };
    }
}

using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [Serializable]
    public struct MapGenConnector
    {
        public MapGenGridDirection Side;
        public Vector2Int LocalCell;
        public string SocketId;
        public MapGenSocketKind SocketKind;
        public int Width;
        public bool Required;
        public string[] Tags;

        public static MapGenConnector Door(MapGenGridDirection side, Vector2Int localCell, string socketId)
        {
            return new MapGenConnector
            {
                Side = side,
                LocalCell = localCell,
                SocketId = socketId ?? string.Empty,
                SocketKind = MapGenSocketKind.Door,
                Width = 1,
                Required = false,
                Tags = Array.Empty<string>()
            };
        }
    }
}

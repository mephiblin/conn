using System;
using UnityEngine;

namespace Conn.MapGenV2.Core
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Baked Map", fileName = "MapGenBakedMap")]
    public sealed class MapGenBakedMapAsset : ScriptableObject
    {
        public string ProfileId = string.Empty;
        public int Seed;
        public string SourceSignature = string.Empty;
        public int Width;
        public int Height;
        public MapGenBakedCell[] Cells = Array.Empty<MapGenBakedCell>();
        public MapGenTraversalEdge[] TraversalEdges = Array.Empty<MapGenTraversalEdge>();
        public string[] SpawnMarkerIds = Array.Empty<string>();
        public string[] ObjectiveMarkerIds = Array.Empty<string>();
    }
}

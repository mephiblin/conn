using System;
using UnityEngine;

namespace Conn.MapGenV2.Core
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Baked Map", fileName = "MapGenBakedMap")]
    public sealed class MapGenBakedMapAsset : ScriptableObject
    {
        public int Version = 1;
        public string ProfileId = string.Empty;
        public string StyleId = string.Empty;
        public string RuleSetId = string.Empty;
        public int Seed;
        public string SourceSignature = string.Empty;
        public int Width;
        public int Height;
        public MapGenBakedCell[] Cells = Array.Empty<MapGenBakedCell>();
        public MapGenBakedRegion[] Regions = Array.Empty<MapGenBakedRegion>();
        public MapGenBakedConnector[] Connectors = Array.Empty<MapGenBakedConnector>();
        public MapGenTraversalEdge[] TraversalEdges = Array.Empty<MapGenTraversalEdge>();
        public MapGenBakedPropInstance[] Props = Array.Empty<MapGenBakedPropInstance>();
        public MapGenBakedMarker[] SpawnMarkers = Array.Empty<MapGenBakedMarker>();
        public MapGenBakedMarker[] ObjectiveMarkers = Array.Empty<MapGenBakedMarker>();
    }
}

using System;

namespace Conn.MapGenV2.Core
{
    public sealed class MapGenMaterializationPlan
    {
        public int Width;
        public int Height;
        public float CellSize;
        public string SourceSignature = string.Empty;
        public MapGenModuleRequest[] Requests = Array.Empty<MapGenModuleRequest>();

        public int RequestCount => Requests?.Length ?? 0;
    }

    public static class MapGenMaterializationPlanner
    {
        public static MapGenMaterializationPlan Build(
            int width,
            int height,
            float cellSize,
            string sourceSignature,
            MapGenMockupCell[] cells)
        {
            var requests = MapGenMaterializationClassifier.Classify(width, height, cells);
            return new MapGenMaterializationPlan
            {
                Width = width,
                Height = height,
                CellSize = cellSize,
                SourceSignature = sourceSignature ?? string.Empty,
                Requests = requests.ToArray()
            };
        }
    }
}

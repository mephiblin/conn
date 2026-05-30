namespace Conn.MapGenV2.Core
{
    public enum MapGenPostProcessPassKind
    {
        AddDirectRoutes,
        FillEnclosedEmptySpace,
        ReduceDeadEnds,
        RemoveSmallRooms,
        SplitLargeRooms,
        ConsolidatePaths,
        AddLoops,
        NormalizeRouteLengths,
        WidenCleanCorridors,
        MergeCompatibleAdjacentRooms
    }

    public struct MapGenPostProcessOptions
    {
        public bool UseDirectRoutes;
        public bool ReduceDeadEnds;
        public bool RemoveSmallRooms;
        public bool SplitLargeRooms;
        public bool ConsolidatePaths;
        public bool AddLoops;
        public bool NormalizeRouteLengths;
        public bool WidenCleanCorridors;
        public bool MergeCompatibleAdjacentRooms;
        public bool FillEnclosedEmptySpace;
        public bool FillReservedMasks;
        public int MaxPasses;
        public MapGenPostProcessPassKind[] PassOrder;
    }
}

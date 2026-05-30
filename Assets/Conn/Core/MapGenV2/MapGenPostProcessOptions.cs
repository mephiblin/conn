namespace Conn.MapGenV2.Core
{
    public enum MapGenPostProcessPassKind
    {
        AddDirectRoutes,
        FillEnclosedEmptySpace,
        ReduceDeadEnds,
        RemoveSmallRooms
    }

    public struct MapGenPostProcessOptions
    {
        public bool UseDirectRoutes;
        public bool ReduceDeadEnds;
        public bool RemoveSmallRooms;
        public bool FillEnclosedEmptySpace;
        public bool FillReservedMasks;
        public int MaxPasses;
        public MapGenPostProcessPassKind[] PassOrder;
    }
}

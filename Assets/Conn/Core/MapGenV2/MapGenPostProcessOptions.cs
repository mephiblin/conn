namespace Conn.MapGenV2.Core
{
    public struct MapGenPostProcessOptions
    {
        public bool UseDirectRoutes;
        public bool ReduceDeadEnds;
        public bool RemoveSmallRooms;
        public int MaxPasses;
    }
}

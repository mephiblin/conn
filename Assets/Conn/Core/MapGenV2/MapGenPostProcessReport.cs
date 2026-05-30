namespace Conn.MapGenV2.Core
{
    public sealed class MapGenPostProcessReport
    {
        public int DirectRouteCellsAdded { get; set; }

        public int DeadEndCorridorsRemoved { get; set; }

        public int IsolatedRoomsRemoved { get; set; }

        public int PassesRun { get; set; }

        public bool Changed => DirectRouteCellsAdded > 0
            || DeadEndCorridorsRemoved > 0
            || IsolatedRoomsRemoved > 0;
    }
}

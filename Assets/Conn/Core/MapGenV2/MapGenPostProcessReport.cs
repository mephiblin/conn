namespace Conn.MapGenV2.Core
{
    public sealed class MapGenPostProcessPassReport
    {
        public MapGenPostProcessPassKind PassKind { get; set; }

        public int PassIndex { get; set; }

        public int ChangedCells { get; set; }

        public bool RolledBack { get; set; }

        public bool ConnectivityValid { get; set; } = true;

        public string BeforeSignature { get; set; } = string.Empty;

        public string AfterSignature { get; set; } = string.Empty;

        public MapGenGridCoord[] ChangedCoords { get; set; } = System.Array.Empty<MapGenGridCoord>();
    }

    public sealed class MapGenPostProcessReport
    {
        private MapGenPostProcessPassReport[] passReports = System.Array.Empty<MapGenPostProcessPassReport>();

        public MapGenPostProcessPassReport[] PassReports => passReports;

        public int DirectRouteCellsAdded { get; set; }

        public int DeadEndCorridorsRemoved { get; set; }

        public int IsolatedRoomsRemoved { get; set; }

        public int EnclosedEmptyCellsFilled { get; set; }

        public int ReservedMaskCellsFilled { get; set; }

        public int PassesRun { get; set; }

        public int Rollbacks { get; set; }

        public bool Cancelled { get; set; }

        public bool RequiredConnectivityValid { get; set; } = true;

        public bool Changed => DirectRouteCellsAdded > 0
            || DeadEndCorridorsRemoved > 0
            || IsolatedRoomsRemoved > 0
            || EnclosedEmptyCellsFilled > 0
            || ReservedMaskCellsFilled > 0;

        public void AddPassReport(MapGenPostProcessPassReport passReport)
        {
            if (passReport == null)
            {
                return;
            }

            System.Array.Resize(ref passReports, passReports.Length + 1);
            passReports[passReports.Length - 1] = passReport;
        }
    }
}

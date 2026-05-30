namespace Conn.MapGenV2.Core
{
    public static class MapGenRoomShapeValidator
    {
        public static MapGenValidationReport Validate(int width, int height, MapGenShapeCell[] cells)
        {
            var report = new MapGenValidationReport();

            if (!MapGenGridCoord.IsValidSize(width, height))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_shape_invalid_dimensions",
                    "Room shape dimensions must be positive.",
                    "Set both width and height to at least 1."));
                return report;
            }

            var expectedLength = width * height;
            if (cells == null || cells.Length != expectedLength)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_shape_cell_count_mismatch",
                    "Room shape cell count does not match its dimensions.",
                    "Resize the room shape grid so cells length equals width * height."));
                return report;
            }

            var hasOccupiedCell = false;
            for (var index = 0; index < cells.Length; index++)
            {
                var coord = MapGenGridCoord.FromIndex(index, width);
                var cell = cells[index];
                if (cell.State == MapGenCellState.Room || cell.State == MapGenCellState.Connector)
                {
                    hasOccupiedCell = true;
                }

                ValidateConnectorCell(report, width, height, coord, cell);
            }

            if (!hasOccupiedCell)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_shape_missing_occupied_cell",
                    "Room shape must contain at least one occupied or connector cell.",
                    "Paint at least one room or connector cell."));
            }

            return report;
        }

        private static void ValidateConnectorCell(
            MapGenValidationReport report,
            int width,
            int height,
            MapGenGridCoord coord,
            MapGenShapeCell cell)
        {
            if (cell.State != MapGenCellState.Connector && cell.SocketKind == MapGenSocketKind.None)
            {
                return;
            }

            if (cell.State != MapGenCellState.Connector)
            {
                return;
            }

            if (cell.SocketKind == MapGenSocketKind.None || cell.SocketKind == MapGenSocketKind.Blocked)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_shape_connector_missing_socket",
                    "Connector cells must use Door, Corridor, or Wildcard socket kind.",
                    "Change the connector socket kind.",
                    coord));
            }

            if (!IsEdgeCell(coord, width, height))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_shape_connector_not_on_edge",
                    "Connector cells must be placed on the room shape edge.",
                    "Move the connector to the outer edge of the room shape grid.",
                    coord));
            }
        }

        private static bool IsEdgeCell(MapGenGridCoord coord, int width, int height)
        {
            return coord.X == 0 || coord.Y == 0 || coord.X == width - 1 || coord.Y == height - 1;
        }
    }
}

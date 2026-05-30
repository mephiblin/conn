namespace Conn.MapGenV2.Core
{
    public static class MapGenPropPlacementValidator
    {
        public static MapGenValidationReport Validate(int width, int height, MapGenMockupCell[] cells)
        {
            var report = new MapGenValidationReport();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.PlaceProps,
                    "prop_placement_invalid_grid",
                    "Prop placement validation requires a valid mockup grid.",
                    "Generate or resize the mockup draft before placing props."));
                return report;
            }

            for (var index = 0; index < cells.Length; index++)
            {
                var cell = cells[index];
                if (string.IsNullOrWhiteSpace(cell.PropChannel) || IsNavigable(cell.State))
                {
                    continue;
                }

                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.PlaceProps,
                    "prop_channel_on_non_navigable_cell",
                    "Prop channels must be placed on navigable cells.",
                    "Move the prop channel to a room, corridor, or connector cell.",
                    MapGenGridCoord.FromIndex(index, width)));
            }

            return report;
        }

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }
    }
}

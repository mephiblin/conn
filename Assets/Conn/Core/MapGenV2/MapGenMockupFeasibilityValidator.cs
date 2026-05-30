using System;

namespace Conn.MapGenV2.Core
{
    public static class MapGenMockupFeasibilityValidator
    {
        public static MapGenValidationReport Validate(int width, int height, MapGenMockupCell[] cells)
        {
            var report = new MapGenValidationReport();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "mockup_feasibility_invalid_grid",
                    "Mockup feasibility validation requires a valid grid.",
                    "Generate or resize the mockup before validating blocked-region feasibility."));
                return report;
            }

            if (!TryFindRoom(cells, width, MapGenRoomCategory.Start, out var start)
                || !TryFindRoom(cells, width, MapGenRoomCategory.Exit, out var exit))
            {
                return report;
            }

            if (!HasPath(width, height, cells, start, exit))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "mockup_blocked_required_traversal",
                    "Blocked or empty regions prevent traversal from Start to Exit.",
                    "Regenerate the mockup, unlock blocking edits, or add corridor/connector cells between required rooms.",
                    cell: start,
                    contextPath: "Draft.Cells"));
            }

            return report;
        }

        private static bool TryFindRoom(
            MapGenMockupCell[] cells,
            int width,
            MapGenRoomCategory category,
            out MapGenGridCoord coord)
        {
            for (var i = 0; i < (cells?.Length ?? 0); i++)
            {
                if (cells[i].State == MapGenCellState.Room && cells[i].RoomCategory == category)
                {
                    coord = MapGenGridCoord.FromIndex(i, width);
                    return true;
                }
            }

            coord = default;
            return false;
        }

        private static bool HasPath(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord start,
            MapGenGridCoord goal)
        {
            var visited = new bool[cells.Length];
            var queue = new MapGenGridCoord[cells.Length];
            var head = 0;
            var tail = 0;
            queue[tail++] = start;
            visited[start.ToIndex(width)] = true;
            while (head < tail)
            {
                var current = queue[head++];
                if (current == goal)
                {
                    return true;
                }

                foreach (MapGenGridDirection direction in Enum.GetValues(typeof(MapGenGridDirection)))
                {
                    var next = current.Offset(direction);
                    if (!next.IsInBounds(width, height))
                    {
                        continue;
                    }

                    var index = next.ToIndex(width);
                    if (visited[index] || !IsNavigable(cells[index].State))
                    {
                        continue;
                    }

                    visited[index] = true;
                    queue[tail++] = next;
                }
            }

            return false;
        }

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }
    }
}

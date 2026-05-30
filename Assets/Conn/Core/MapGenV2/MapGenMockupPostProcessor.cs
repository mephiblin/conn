using System;

namespace Conn.MapGenV2.Core
{
    public static class MapGenMockupPostProcessor
    {
        public static MapGenPostProcessReport Apply(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenPostProcessOptions options)
        {
            var report = new MapGenPostProcessReport();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return report;
            }

            var maxPasses = Math.Max(1, options.MaxPasses);
            for (var pass = 0; pass < maxPasses; pass++)
            {
                var snapshot = CopyCells(cells);
                var requiresConnectivity = HasRequiredRooms(width, snapshot);
                var changed = false;
                var directRouteCellsAdded = 0;
                var deadEndCorridorsRemoved = 0;
                var isolatedRoomsRemoved = 0;
                var enclosedEmptyCellsFilled = 0;
                if (options.UseDirectRoutes)
                {
                    directRouteCellsAdded = AddDirectRoute(width, height, cells);
                    changed |= directRouteCellsAdded > 0;
                }

                if (options.FillEnclosedEmptySpace)
                {
                    enclosedEmptyCellsFilled = FillEnclosedEmptySpace(width, height, cells);
                    changed |= enclosedEmptyCellsFilled > 0;
                }

                if (options.ReduceDeadEnds)
                {
                    deadEndCorridorsRemoved = RemoveDeadEndCorridors(width, height, cells);
                    changed |= deadEndCorridorsRemoved > 0;
                }

                if (options.RemoveSmallRooms)
                {
                    isolatedRoomsRemoved = RemoveIsolatedRooms(width, height, cells);
                    changed |= isolatedRoomsRemoved > 0;
                }

                report.PassesRun++;
                report.RequiredConnectivityValid = HasRequiredTraversal(width, height, cells, requiresConnectivity);
                if (changed && !report.RequiredConnectivityValid)
                {
                    Array.Copy(snapshot, cells, snapshot.Length);
                    report.RequiredConnectivityValid = HasRequiredTraversal(width, height, cells, requiresConnectivity);
                    report.Rollbacks++;
                    break;
                }

                report.DirectRouteCellsAdded += directRouteCellsAdded;
                report.DeadEndCorridorsRemoved += deadEndCorridorsRemoved;
                report.IsolatedRoomsRemoved += isolatedRoomsRemoved;
                report.EnclosedEmptyCellsFilled += enclosedEmptyCellsFilled;
                if (!changed)
                {
                    break;
                }
            }

            return report;
        }

        public static MapGenValidationReport ValidateSafety(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenPostProcessOptions options)
        {
            var report = new MapGenValidationReport();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.PostProcess,
                    "post_process_safety_invalid_grid",
                    "Post-process safety validation requires a valid mockup grid.",
                    "Generate or resize the mockup before running post-process validation."));
                return report;
            }

            var copy = CopyCells(cells);
            var postProcessReport = Apply(width, height, copy, options);
            if (postProcessReport.Rollbacks > 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.PostProcess,
                    "post_process_pass_requires_rollback",
                    "Post-process settings would break required traversal and require rollback.",
                    "Disable the unsafe pass or adjust the mockup/rules before accepting the result.",
                    severity: MapGenIssueSeverity.Warning,
                    contextPath: nameof(MapGenPostProcessOptions)));
            }

            if (!postProcessReport.RequiredConnectivityValid)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.PostProcess,
                    "post_process_required_connectivity_invalid",
                    "Post-process result does not preserve required Start to Exit traversal.",
                    "Add valid traversal before running post-process or adjust the post-process rules.",
                    contextPath: nameof(MapGenPostProcessOptions)));
            }

            return report;
        }

        private static int FillEnclosedEmptySpace(int width, int height, MapGenMockupCell[] cells)
        {
            var candidates = new bool[cells.Length];
            var count = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i].State != MapGenCellState.Empty)
                {
                    continue;
                }

                var coord = MapGenGridCoord.FromIndex(i, width);
                if (CountNavigableNeighbors(width, height, cells, coord) < 3)
                {
                    continue;
                }

                candidates[i] = true;
                count++;
            }

            for (var i = 0; i < candidates.Length; i++)
            {
                if (!candidates[i])
                {
                    continue;
                }

                cells[i] = new MapGenMockupCell
                {
                    State = MapGenCellState.Corridor,
                    RegionId = -1
                };
            }

            return count;
        }

        private static MapGenMockupCell[] CopyCells(MapGenMockupCell[] cells)
        {
            var copy = new MapGenMockupCell[cells.Length];
            Array.Copy(cells, copy, cells.Length);
            return copy;
        }

        private static int AddDirectRoute(int width, int height, MapGenMockupCell[] cells)
        {
            if (!TryFindRoom(cells, width, MapGenRoomCategory.Start, out var start)
                || !TryFindRoom(cells, width, MapGenRoomCategory.Exit, out var exit))
            {
                return 0;
            }

            var added = 0;
            var x = start.X;
            var y = start.Y;
            while (x != exit.X)
            {
                x += x < exit.X ? 1 : -1;
                added += SetCorridorIfEmpty(cells, width, x, y);
            }

            while (y != exit.Y)
            {
                y += y < exit.Y ? 1 : -1;
                added += SetCorridorIfEmpty(cells, width, x, y);
            }

            return added;
        }

        private static int RemoveDeadEndCorridors(int width, int height, MapGenMockupCell[] cells)
        {
            var removed = 0;
            var changed = true;
            while (changed)
            {
                changed = false;
                for (var i = 0; i < cells.Length; i++)
                {
                    if (cells[i].State != MapGenCellState.Corridor)
                    {
                        continue;
                    }

                    var coord = MapGenGridCoord.FromIndex(i, width);
                    if (CountNavigableNeighbors(width, height, cells, coord) <= 1)
                    {
                        cells[i] = MapGenMockupCell.Empty;
                        removed++;
                        changed = true;
                    }
                }
            }

            return removed;
        }

        private static int RemoveIsolatedRooms(int width, int height, MapGenMockupCell[] cells)
        {
            var removed = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i].State != MapGenCellState.Room)
                {
                    continue;
                }

                var coord = MapGenGridCoord.FromIndex(i, width);
                if (CountNavigableNeighbors(width, height, cells, coord) == 0)
                {
                    cells[i] = MapGenMockupCell.Empty;
                    removed++;
                }
            }

            return removed;
        }

        private static int SetCorridorIfEmpty(MapGenMockupCell[] cells, int width, int x, int y)
        {
            var index = new MapGenGridCoord(x, y).ToIndex(width);
            if (cells[index].State != MapGenCellState.Empty)
            {
                return 0;
            }

            cells[index].State = MapGenCellState.Corridor;
            cells[index].RegionId = -1;
            return 1;
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

        private static bool HasRequiredRooms(int width, MapGenMockupCell[] cells)
        {
            return TryFindRoom(cells, width, MapGenRoomCategory.Start, out _)
                && TryFindRoom(cells, width, MapGenRoomCategory.Exit, out _);
        }

        private static bool HasRequiredTraversal(int width, int height, MapGenMockupCell[] cells, bool requireRequiredRooms)
        {
            if (!TryFindRoom(cells, width, MapGenRoomCategory.Start, out var start)
                || !TryFindRoom(cells, width, MapGenRoomCategory.Exit, out var exit))
            {
                return !requireRequiredRooms;
            }

            var visited = new bool[cells.Length];
            var queue = new MapGenGridCoord[cells.Length];
            var head = 0;
            var tail = 0;
            queue[tail++] = start;
            visited[start.ToIndex(width)] = true;

            while (head < tail)
            {
                var coord = queue[head++];
                if (coord == exit)
                {
                    return true;
                }

                foreach (MapGenGridDirection direction in Enum.GetValues(typeof(MapGenGridDirection)))
                {
                    var neighbor = coord.Offset(direction);
                    if (!neighbor.IsInBounds(width, height))
                    {
                        continue;
                    }

                    var index = neighbor.ToIndex(width);
                    if (visited[index] || !IsNavigable(cells[index].State))
                    {
                        continue;
                    }

                    visited[index] = true;
                    queue[tail++] = neighbor;
                }
            }

            return false;
        }

        private static int CountNavigableNeighbors(int width, int height, MapGenMockupCell[] cells, MapGenGridCoord coord)
        {
            var count = 0;
            foreach (MapGenGridDirection direction in Enum.GetValues(typeof(MapGenGridDirection)))
            {
                var neighbor = coord.Offset(direction);
                if (!neighbor.IsInBounds(width, height))
                {
                    continue;
                }

                var state = cells[neighbor.ToIndex(width)].State;
                if (IsNavigable(state))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }
    }
}

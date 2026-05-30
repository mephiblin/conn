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
                var changed = false;
                if (options.UseDirectRoutes)
                {
                    var added = AddDirectRoute(width, height, cells);
                    report.DirectRouteCellsAdded += added;
                    changed |= added > 0;
                }

                if (options.ReduceDeadEnds)
                {
                    var removed = RemoveDeadEndCorridors(width, height, cells);
                    report.DeadEndCorridorsRemoved += removed;
                    changed |= removed > 0;
                }

                if (options.RemoveSmallRooms)
                {
                    var removed = RemoveIsolatedRooms(width, height, cells);
                    report.IsolatedRoomsRemoved += removed;
                    changed |= removed > 0;
                }

                report.PassesRun++;
                if (!changed)
                {
                    break;
                }
            }

            return report;
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
                if (state == MapGenCellState.Room || state == MapGenCellState.Corridor || state == MapGenCellState.Connector)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

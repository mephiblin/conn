using System;

namespace Conn.MapGenV2.Core
{
    public static class MapGenMockupSolver
    {
        public static MapGenMockupSolverResult Generate(
            int width,
            int height,
            int seed,
            MapGenRoomCategory[] requiredCategories,
            Func<bool> shouldCancel = null)
        {
            var report = new MapGenValidationReport();
            if (IsCancelled(shouldCancel, report))
            {
                return Failed(width, height, seed, report);
            }

            if (!MapGenGridCoord.IsValidSize(width, height))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "solver_invalid_grid_size",
                    "Mockup solver requires a positive grid size.",
                    "Use a profile map size of at least 1x1."));
                return Failed(width, height, seed, report);
            }

            var categories = NormalizeRequiredCategories(requiredCategories);
            if (categories.Length > width * height)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "solver_not_enough_cells",
                    "Grid is too small for required rooms.",
                    "Increase map size or reduce required room categories."));
                return Failed(width, height, seed, report);
            }

            var cells = CreateEmptyCells(width, height);
            var usedRooms = new bool[cells.Length];
            var rng = new MapGenRandom(seed).Fork("solver");
            var previous = default(MapGenGridCoord);
            for (var i = 0; i < categories.Length; i++)
            {
                if (IsCancelled(shouldCancel, report))
                {
                    return Failed(width, height, seed, report);
                }

                var room = PickRoomCoord(width, height, i, categories.Length, ref rng);
                room = FindFreeRoomCoord(width, height, room, usedRooms);
                usedRooms[room.ToIndex(width)] = true;
                if (i > 0)
                {
                    CarveCorridor(cells, width, previous, room);
                }

                var roomIndex = room.ToIndex(width);
                cells[roomIndex].State = MapGenCellState.Room;
                cells[roomIndex].RegionId = i;
                cells[roomIndex].RoomCategory = categories[i];
                previous = room;
            }

            return new MapGenMockupSolverResult
            {
                Success = true,
                Width = width,
                Height = height,
                Seed = seed,
                Cells = cells,
                Report = report
            };
        }

        private static bool IsCancelled(Func<bool> shouldCancel, MapGenValidationReport report)
        {
            if (shouldCancel == null || !shouldCancel())
            {
                return false;
            }

            report.Add(new MapGenIssue(
                MapGenGenerationPhase.SolveMockup,
                "solver_cancelled",
                "Mockup generation was cancelled.",
                "Run generation again when ready.",
                severity: MapGenIssueSeverity.Warning));
            return true;
        }

        private static MapGenMockupSolverResult Failed(int width, int height, int seed, MapGenValidationReport report)
        {
            return new MapGenMockupSolverResult
            {
                Success = false,
                Width = Math.Max(0, width),
                Height = Math.Max(0, height),
                Seed = seed,
                Cells = Array.Empty<MapGenMockupCell>(),
                Report = report
            };
        }

        private static MapGenRoomCategory[] NormalizeRequiredCategories(MapGenRoomCategory[] requiredCategories)
        {
            if (requiredCategories == null || requiredCategories.Length == 0)
            {
                return new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
            }

            return requiredCategories;
        }

        private static MapGenMockupCell[] CreateEmptyCells(int width, int height)
        {
            var cells = new MapGenMockupCell[width * height];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            return cells;
        }

        private static MapGenGridCoord PickRoomCoord(
            int width,
            int height,
            int index,
            int count,
            ref MapGenRandom rng)
        {
            if (count <= 1)
            {
                return new MapGenGridCoord(width / 2, height / 2);
            }

            var x = count == 1 ? width / 2 : (index * Math.Max(1, width - 1)) / Math.Max(1, count - 1);
            var y = height <= 1 ? 0 : rng.NextInt(0, height);
            return new MapGenGridCoord(x, y);
        }

        private static MapGenGridCoord FindFreeRoomCoord(
            int width,
            int height,
            MapGenGridCoord preferred,
            bool[] usedRooms)
        {
            var preferredIndex = preferred.ToIndex(width);
            if (!usedRooms[preferredIndex])
            {
                return preferred;
            }

            for (var offset = 1; offset < usedRooms.Length; offset++)
            {
                var index = (preferredIndex + offset) % usedRooms.Length;
                if (!usedRooms[index])
                {
                    return MapGenGridCoord.FromIndex(index, width);
                }
            }

            return preferred;
        }

        private static void CarveCorridor(MapGenMockupCell[] cells, int width, MapGenGridCoord from, MapGenGridCoord to)
        {
            var x = from.X;
            var y = from.Y;
            while (x != to.X)
            {
                x += x < to.X ? 1 : -1;
                SetCorridor(cells, width, x, y);
            }

            while (y != to.Y)
            {
                y += y < to.Y ? 1 : -1;
                SetCorridor(cells, width, x, y);
            }
        }

        private static void SetCorridor(MapGenMockupCell[] cells, int width, int x, int y)
        {
            var index = new MapGenGridCoord(x, y).ToIndex(width);
            if (cells[index].State == MapGenCellState.Empty)
            {
                cells[index].State = MapGenCellState.Corridor;
                cells[index].RegionId = -1;
            }
        }
    }
}

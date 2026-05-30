using System;

namespace Conn.MapGenV2.Core
{
    public static class MapGenMockupPostProcessor
    {
        public static MapGenPostProcessReport Apply(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenPostProcessOptions options,
            Func<bool> shouldCancel = null)
        {
            var report = new MapGenPostProcessReport();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return report;
            }

            var maxPasses = Math.Max(1, options.MaxPasses);
            var passOrder = ResolvePassOrder(options);
            for (var pass = 0; pass < maxPasses; pass++)
            {
                if (shouldCancel != null && shouldCancel())
                {
                    report.Cancelled = true;
                    break;
                }

                var snapshot = CopyCells(cells);
                var requiresConnectivity = HasRequiredRooms(width, snapshot);
                var changed = false;
                for (var passOrderIndex = 0; passOrderIndex < passOrder.Length; passOrderIndex++)
                {
                    var passKind = passOrder[passOrderIndex];
                    if (!IsPassEnabled(options, passKind))
                    {
                        continue;
                    }

                    var beforePass = CopyCells(cells);
                    var beforeSignature = BuildCellStateSignature(beforePass);
                    ApplyPass(passKind, width, height, cells, options);
                    var changedCoords = BuildChangedCoords(beforePass, cells, width);
                    var changedCells = changedCoords.Length;
                    var reservedMaskCells = passKind == MapGenPostProcessPassKind.FillEnclosedEmptySpace
                        ? CountChangedReservedMasks(beforePass, changedCoords, width)
                        : 0;
                    changed |= changedCells > 0;

                    report.PassesRun++;
                    var connectivityValid = HasRequiredTraversal(width, height, cells, requiresConnectivity);
                    var rolledBack = changedCells > 0 && !connectivityValid;
                    if (rolledBack)
                    {
                        Array.Copy(beforePass, cells, beforePass.Length);
                        connectivityValid = HasRequiredTraversal(width, height, cells, requiresConnectivity);
                        report.Rollbacks++;
                    }

                    AddPassCount(report, passKind, rolledBack ? 0 : changedCells, rolledBack ? 0 : reservedMaskCells);
                    report.RequiredConnectivityValid = connectivityValid;
                    report.AddPassReport(new MapGenPostProcessPassReport
                    {
                        PassKind = passKind,
                        PassIndex = pass,
                        ChangedCells = rolledBack ? 0 : changedCells,
                        RolledBack = rolledBack,
                        ConnectivityValid = connectivityValid,
                        BeforeSignature = beforeSignature,
                        AfterSignature = BuildCellStateSignature(cells),
                        ChangedCoords = changedCoords
                    });

                    if (rolledBack)
                    {
                        break;
                    }
                }

                if (!changed || report.Rollbacks > 0)
                {
                    if (report.Rollbacks > 0)
                    {
                        Array.Copy(snapshot, cells, snapshot.Length);
                        report.RequiredConnectivityValid = HasRequiredTraversal(width, height, cells, requiresConnectivity);
                    }

                    break;
                }
            }

            return report;
        }

        private static MapGenPostProcessPassKind[] ResolvePassOrder(MapGenPostProcessOptions options)
        {
            return options.PassOrder != null && options.PassOrder.Length > 0
                ? options.PassOrder
                : new[]
                {
                    MapGenPostProcessPassKind.AddDirectRoutes,
                    MapGenPostProcessPassKind.FillEnclosedEmptySpace,
                    MapGenPostProcessPassKind.ReduceDeadEnds,
                    MapGenPostProcessPassKind.RemoveSmallRooms,
                    MapGenPostProcessPassKind.SplitLargeRooms,
                    MapGenPostProcessPassKind.ConsolidatePaths,
                    MapGenPostProcessPassKind.AddLoops,
                    MapGenPostProcessPassKind.NormalizeRouteLengths,
                    MapGenPostProcessPassKind.WidenCleanCorridors,
                    MapGenPostProcessPassKind.MergeCompatibleAdjacentRooms
                };
        }

        private static bool IsPassEnabled(MapGenPostProcessOptions options, MapGenPostProcessPassKind passKind)
        {
            switch (passKind)
            {
                case MapGenPostProcessPassKind.AddDirectRoutes:
                    return options.UseDirectRoutes;
                case MapGenPostProcessPassKind.FillEnclosedEmptySpace:
                    return options.FillEnclosedEmptySpace || options.FillReservedMasks;
                case MapGenPostProcessPassKind.ReduceDeadEnds:
                    return options.ReduceDeadEnds;
                case MapGenPostProcessPassKind.RemoveSmallRooms:
                    return options.RemoveSmallRooms;
                case MapGenPostProcessPassKind.SplitLargeRooms:
                    return options.SplitLargeRooms;
                case MapGenPostProcessPassKind.ConsolidatePaths:
                    return options.ConsolidatePaths;
                case MapGenPostProcessPassKind.AddLoops:
                    return options.AddLoops;
                case MapGenPostProcessPassKind.NormalizeRouteLengths:
                    return options.NormalizeRouteLengths;
                case MapGenPostProcessPassKind.WidenCleanCorridors:
                    return options.WidenCleanCorridors;
                case MapGenPostProcessPassKind.MergeCompatibleAdjacentRooms:
                    return options.MergeCompatibleAdjacentRooms;
                default:
                    return false;
            }
        }

        private static int ApplyPass(
            MapGenPostProcessPassKind passKind,
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenPostProcessOptions options)
        {
            switch (passKind)
            {
                case MapGenPostProcessPassKind.AddDirectRoutes:
                    return AddDirectRoute(width, height, cells);
                case MapGenPostProcessPassKind.FillEnclosedEmptySpace:
                    return FillEnclosedEmptySpace(width, height, cells, options.FillEnclosedEmptySpace, options.FillReservedMasks);
                case MapGenPostProcessPassKind.ReduceDeadEnds:
                    return RemoveDeadEndCorridors(width, height, cells);
                case MapGenPostProcessPassKind.RemoveSmallRooms:
                    return RemoveIsolatedRooms(width, height, cells);
                case MapGenPostProcessPassKind.SplitLargeRooms:
                    return SplitLargeRooms(width, height, cells);
                case MapGenPostProcessPassKind.ConsolidatePaths:
                    return ConsolidatePaths(width, height, cells);
                case MapGenPostProcessPassKind.AddLoops:
                    return AddLoop(width, height, cells);
                case MapGenPostProcessPassKind.NormalizeRouteLengths:
                    return NormalizeRouteLengths(width, height, cells);
                case MapGenPostProcessPassKind.WidenCleanCorridors:
                    return WidenCleanCorridors(width, height, cells);
                case MapGenPostProcessPassKind.MergeCompatibleAdjacentRooms:
                    return MergeCompatibleAdjacentRooms(width, height, cells);
                default:
                    return 0;
            }
        }

        private static void AddPassCount(
            MapGenPostProcessReport report,
            MapGenPostProcessPassKind passKind,
            int changedCells,
            int reservedMaskCells)
        {
            switch (passKind)
            {
                case MapGenPostProcessPassKind.AddDirectRoutes:
                    report.DirectRouteCellsAdded += changedCells;
                    break;
                case MapGenPostProcessPassKind.FillEnclosedEmptySpace:
                    report.EnclosedEmptyCellsFilled += changedCells;
                    report.ReservedMaskCellsFilled += reservedMaskCells;
                    break;
                case MapGenPostProcessPassKind.ReduceDeadEnds:
                    report.DeadEndCorridorsRemoved += changedCells;
                    break;
                case MapGenPostProcessPassKind.RemoveSmallRooms:
                    report.IsolatedRoomsRemoved += changedCells;
                    break;
            }
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

        private static int FillEnclosedEmptySpace(
            int width,
            int height,
            MapGenMockupCell[] cells,
            bool includeEnclosedEmpty,
            bool includeReservedMasks)
        {
            var candidates = new bool[cells.Length];
            var count = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i].State == MapGenCellState.Reserved && includeReservedMasks)
                {
                    var reservedCoord = MapGenGridCoord.FromIndex(i, width);
                    if (CountNavigableNeighbors(width, height, cells, reservedCoord) <= 0)
                    {
                        continue;
                    }

                    candidates[i] = true;
                    count++;
                    continue;
                }

                if (!includeEnclosedEmpty || cells[i].State != MapGenCellState.Empty)
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

        private static string BuildCellStateSignature(MapGenMockupCell[] cells)
        {
            unchecked
            {
                var hash = 1469598103934665603UL;
                foreach (var cell in cells ?? Array.Empty<MapGenMockupCell>())
                {
                    hash ^= (ulong)(int)cell.State;
                    hash *= 1099511628211UL;
                    hash ^= (ulong)Math.Max(0, cell.RegionId + 1);
                    hash *= 1099511628211UL;
                    hash ^= (ulong)(int)cell.RoomCategory;
                    hash *= 1099511628211UL;
                }

                return hash.ToString("X16");
            }
        }

        private static MapGenGridCoord[] BuildChangedCoords(MapGenMockupCell[] before, MapGenMockupCell[] after, int width)
        {
            if (before == null || after == null || before.Length != after.Length)
            {
                return Array.Empty<MapGenGridCoord>();
            }

            var changed = new MapGenGridCoord[after.Length];
            var count = 0;
            for (var i = 0; i < after.Length; i++)
            {
                if (before[i].State == after[i].State
                    && before[i].RegionId == after[i].RegionId
                    && before[i].RoomCategory == after[i].RoomCategory)
                {
                    continue;
                }

                changed[count++] = MapGenGridCoord.FromIndex(i, width);
            }

            Array.Resize(ref changed, count);
            return changed;
        }

        private static int CountChangedReservedMasks(MapGenMockupCell[] before, MapGenGridCoord[] changedCoords, int width)
        {
            var count = 0;
            foreach (var coord in changedCoords ?? Array.Empty<MapGenGridCoord>())
            {
                var index = coord.ToIndex(width);
                if (index >= 0 && index < (before?.Length ?? 0) && before[index].State == MapGenCellState.Reserved)
                {
                    count++;
                }
            }

            return count;
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

        private static int SplitLargeRooms(int width, int height, MapGenMockupCell[] cells)
        {
            var changed = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i].State != MapGenCellState.Room)
                {
                    continue;
                }

                var coord = MapGenGridCoord.FromIndex(i, width);
                var horizontalRoomSpan = IsRoomAt(width, height, cells, coord.X - 1, coord.Y)
                    && IsRoomAt(width, height, cells, coord.X + 1, coord.Y);
                var verticalRoomSpan = IsRoomAt(width, height, cells, coord.X, coord.Y - 1)
                    && IsRoomAt(width, height, cells, coord.X, coord.Y + 1);
                if (!horizontalRoomSpan || !verticalRoomSpan)
                {
                    continue;
                }

                cells[i].State = MapGenCellState.Corridor;
                cells[i].SourceTemplateId = "post_split_large_room";
                changed++;
            }

            return changed;
        }

        private static int ConsolidatePaths(int width, int height, MapGenMockupCell[] cells)
        {
            var candidates = new bool[cells.Length];
            var changed = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i].State != MapGenCellState.Empty)
                {
                    continue;
                }

                var coord = MapGenGridCoord.FromIndex(i, width);
                var eastWest = IsNavigableAt(width, height, cells, coord.X - 1, coord.Y)
                    && IsNavigableAt(width, height, cells, coord.X + 1, coord.Y);
                var northSouth = IsNavigableAt(width, height, cells, coord.X, coord.Y - 1)
                    && IsNavigableAt(width, height, cells, coord.X, coord.Y + 1);
                if (!eastWest && !northSouth)
                {
                    continue;
                }

                candidates[i] = true;
                changed++;
            }

            ApplyCorridorCandidates(cells, candidates, "post_consolidate_paths");
            return changed;
        }

        private static int AddLoop(int width, int height, MapGenMockupCell[] cells)
        {
            if (!TryFindRoom(cells, width, MapGenRoomCategory.Start, out var start)
                || !TryFindRoom(cells, width, MapGenRoomCategory.Exit, out var exit))
            {
                return 0;
            }

            var loopY = FindLoopY(height, start.Y, exit.Y);
            if (loopY < 0)
            {
                return 0;
            }

            var changed = 0;
            changed += CarveCorridorLine(cells, width, start, new MapGenGridCoord(start.X, loopY), "post_add_loop");
            changed += CarveCorridorLine(cells, width, new MapGenGridCoord(start.X, loopY), new MapGenGridCoord(exit.X, loopY), "post_add_loop");
            changed += CarveCorridorLine(cells, width, new MapGenGridCoord(exit.X, loopY), exit, "post_add_loop");
            return changed;
        }

        private static int NormalizeRouteLengths(int width, int height, MapGenMockupCell[] cells)
        {
            return HasRequiredTraversal(width, height, cells, true)
                ? 0
                : AddDirectRoute(width, height, cells);
        }

        private static int WidenCleanCorridors(int width, int height, MapGenMockupCell[] cells)
        {
            var candidates = new bool[cells.Length];
            var changed = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i].State != MapGenCellState.Corridor)
                {
                    continue;
                }

                var coord = MapGenGridCoord.FromIndex(i, width);
                foreach (MapGenGridDirection direction in Enum.GetValues(typeof(MapGenGridDirection)))
                {
                    var neighbor = coord.Offset(direction);
                    if (!neighbor.IsInBounds(width, height))
                    {
                        continue;
                    }

                    var index = neighbor.ToIndex(width);
                    if (cells[index].State == MapGenCellState.Empty && CountNavigableNeighbors(width, height, cells, neighbor) >= 2)
                    {
                        candidates[index] = true;
                    }
                }
            }

            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i])
                {
                    changed++;
                }
            }

            ApplyCorridorCandidates(cells, candidates, "post_widen_clean_corridors");
            return changed;
        }

        private static int MergeCompatibleAdjacentRooms(int width, int height, MapGenMockupCell[] cells)
        {
            var changed = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i].State != MapGenCellState.Corridor && cells[i].State != MapGenCellState.Connector)
                {
                    continue;
                }

                var coord = MapGenGridCoord.FromIndex(i, width);
                if (!TryGetRoomNeighbor(width, height, cells, coord.X - 1, coord.Y, out var left)
                    || !TryGetRoomNeighbor(width, height, cells, coord.X + 1, coord.Y, out var right))
                {
                    if (!TryGetRoomNeighbor(width, height, cells, coord.X, coord.Y - 1, out left)
                        || !TryGetRoomNeighbor(width, height, cells, coord.X, coord.Y + 1, out right))
                    {
                        continue;
                    }
                }

                if (left.RegionId == right.RegionId || left.RoomCategory != right.RoomCategory)
                {
                    continue;
                }

                cells[i].State = MapGenCellState.Room;
                cells[i].RoomCategory = left.RoomCategory;
                cells[i].RegionId = left.RegionId;
                cells[i].SourceTemplateId = "post_merge_adjacent_rooms";
                changed++;
            }

            return changed;
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

        private static int CarveCorridorLine(
            MapGenMockupCell[] cells,
            int width,
            MapGenGridCoord from,
            MapGenGridCoord to,
            string sourceTemplateId)
        {
            var changed = 0;
            var x = from.X;
            var y = from.Y;
            while (x != to.X)
            {
                x += x < to.X ? 1 : -1;
                changed += SetCorridorIfEmpty(cells, width, x, y, sourceTemplateId);
            }

            while (y != to.Y)
            {
                y += y < to.Y ? 1 : -1;
                changed += SetCorridorIfEmpty(cells, width, x, y, sourceTemplateId);
            }

            return changed;
        }

        private static int SetCorridorIfEmpty(MapGenMockupCell[] cells, int width, int x, int y, string sourceTemplateId)
        {
            var index = new MapGenGridCoord(x, y).ToIndex(width);
            if (cells[index].State != MapGenCellState.Empty)
            {
                return 0;
            }

            cells[index].State = MapGenCellState.Corridor;
            cells[index].RegionId = -1;
            cells[index].SourceTemplateId = sourceTemplateId ?? string.Empty;
            return 1;
        }

        private static void ApplyCorridorCandidates(MapGenMockupCell[] cells, bool[] candidates, string sourceTemplateId)
        {
            for (var i = 0; i < candidates.Length; i++)
            {
                if (!candidates[i])
                {
                    continue;
                }

                cells[i].State = MapGenCellState.Corridor;
                cells[i].RegionId = -1;
                cells[i].SourceTemplateId = sourceTemplateId ?? string.Empty;
            }
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

        private static int FindLoopY(int height, int startY, int exitY)
        {
            var above = Math.Max(startY, exitY) + 2;
            if (above < height)
            {
                return above;
            }

            var below = Math.Min(startY, exitY) - 2;
            if (below >= 0)
            {
                return below;
            }

            return -1;
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

        private static bool IsRoomAt(int width, int height, MapGenMockupCell[] cells, int x, int y)
        {
            var coord = new MapGenGridCoord(x, y);
            return coord.IsInBounds(width, height) && cells[coord.ToIndex(width)].State == MapGenCellState.Room;
        }

        private static bool IsNavigableAt(int width, int height, MapGenMockupCell[] cells, int x, int y)
        {
            var coord = new MapGenGridCoord(x, y);
            return coord.IsInBounds(width, height) && IsNavigable(cells[coord.ToIndex(width)].State);
        }

        private static bool TryGetRoomNeighbor(
            int width,
            int height,
            MapGenMockupCell[] cells,
            int x,
            int y,
            out MapGenMockupCell cell)
        {
            var coord = new MapGenGridCoord(x, y);
            if (coord.IsInBounds(width, height))
            {
                cell = cells[coord.ToIndex(width)];
                return cell.State == MapGenCellState.Room;
            }

            cell = default;
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

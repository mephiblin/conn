using Conn.MapGenV2.Core;
using System;
using System.Collections.Generic;

namespace Conn.MapGenV2.Authoring
{
    public static class MapGenPropPlacementPlanner
    {
        public static MapGenPropPlacementResult BuildForDraft(MapGenMockupDraftAsset draft)
        {
            if (draft == null || draft.Profile == null || draft.Profile.LayoutRules == null)
            {
                return new MapGenPropPlacementResult();
            }

            return Build(
                draft.Width,
                draft.Height,
                draft.Cells,
                draft.Profile.LayoutRules.PropPlacementRules,
                draft.Seed);
        }

        public static MapGenPropPlacementResult Build(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenPropPlacementRules[] rules,
            int seed)
        {
            var result = new MapGenPropPlacementResult();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                result.Report.Issues.Add(new MapGenIssue(
                    MapGenGenerationPhase.PlaceProps,
                    "prop_placement_invalid_grid",
                    "Prop placement requires a valid mockup grid.",
                    "Generate or resize the mockup draft before placing props."));
                return result;
            }

            var occupied = new HashSet<MapGenGridCoord>();
            var placed = new List<MapGenPlacedProp>();
            for (var ruleIndex = 0; ruleIndex < (rules?.Length ?? 0); ruleIndex++)
            {
                var rule = rules[ruleIndex];
                if (string.IsNullOrWhiteSpace(rule.Channel))
                {
                    result.Report.Issues.Add(new MapGenIssue(
                        MapGenGenerationPhase.PlaceProps,
                        "prop_placement_rule_missing_channel",
                        $"Prop placement rule {ruleIndex} has no channel.",
                        "Assign a prop channel before planning prop placement."));
                    continue;
                }

                var candidates = CollectCandidates(width, height, cells, rule);
                result.Report.TotalCandidateCells += candidates.Count;
                if (candidates.Count == 0)
                {
                    if (rule.RequiredUnique || rule.DistributionMode == MapGenPropDistributionMode.RequiredUnique)
                    {
                        result.Report.MissingRequiredUniqueProps++;
                        result.Report.Issues.Add(new MapGenIssue(
                            MapGenGenerationPhase.PlaceProps,
                            "prop_placement_required_unique_missing_candidate",
                            $"Required unique prop channel '{rule.Channel}' has no legal candidate cells.",
                            "Add a matching prop channel marker or relax the rule filters."));
                    }

                    continue;
                }

                var selected = SelectCandidates(candidates, rule, seed, ruleIndex);
                foreach (var coord in selected)
                {
                    if (!PassesSpacing(coord, occupied, rule.MinSpacingCells))
                    {
                        result.Report.RejectedBySpacing++;
                        continue;
                    }

                    if (rule.ChannelKind == MapGenPropPlacementChannelKind.Blocker
                        && !rule.AllowTraversalBlocking
                        && !TraversalRemainsConnected(width, height, cells, coord))
                    {
                        result.Report.BlockerTraversalIssues++;
                        result.Report.Issues.Add(new MapGenIssue(
                            MapGenGenerationPhase.PlaceProps,
                            "prop_placement_blocker_breaks_traversal",
                            $"Blocker prop channel '{rule.Channel}' would break traversal at {coord}.",
                            "Move the blocker, allow traversal blocking explicitly, or add an alternate route.",
                            coord));
                        continue;
                    }

                    var cell = cells[coord.ToIndex(width)];
                    occupied.Add(coord);
                    placed.Add(new MapGenPlacedProp
                    {
                        Coord = coord,
                        Channel = rule.Channel,
                        RuleIndex = ruleIndex,
                        RegionId = cell.RegionId,
                        RoomCategory = cell.RoomCategory,
                        ChannelKind = rule.ChannelKind,
                        DistributionMode = rule.DistributionMode,
                        BlocksTraversal = rule.ChannelKind == MapGenPropPlacementChannelKind.Blocker
                    });
                }
            }

            result.PlacedProps = placed.ToArray();
            result.Report.PlacedCount = result.PlacedProps.Length;
            return result;
        }

        private static List<PropCandidate> CollectCandidates(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenPropPlacementRules rule)
        {
            var candidates = new List<PropCandidate>();
            for (var index = 0; index < cells.Length; index++)
            {
                var coord = MapGenGridCoord.FromIndex(index, width);
                var cell = cells[index];
                if (!IsNavigable(cell.State) || !MatchesFilters(cell, rule))
                {
                    continue;
                }

                if (MatchesChannelKind(width, height, cells, coord, cell, rule))
                {
                    candidates.Add(new PropCandidate(coord, cell.RegionId));
                }
            }

            return candidates;
        }

        private static MapGenGridCoord[] SelectCandidates(
            List<PropCandidate> candidates,
            MapGenPropPlacementRules rule,
            int seed,
            int ruleIndex)
        {
            var ordered = new List<PropCandidate>(candidates);
            switch (rule.DistributionMode)
            {
                case MapGenPropDistributionMode.Grid:
                    return PickGrid(ordered, Math.Max(1, rule.MinSpacingCells + 1));
                case MapGenPropDistributionMode.OnePerRegion:
                    return PickOnePerRegion(ordered);
                case MapGenPropDistributionMode.RequiredUnique:
                    return new[] { PickRandom(ordered, seed, ruleIndex) };
                case MapGenPropDistributionMode.Random:
                case MapGenPropDistributionMode.WeightedRandom:
                case MapGenPropDistributionMode.Perimeter:
                case MapGenPropDistributionMode.MarkerBased:
                default:
                    return PickByDensity(ordered, seed, ruleIndex, rule.DensityPercent <= 0 ? 100 : rule.DensityPercent);
            }
        }

        private static MapGenGridCoord[] PickByDensity(List<PropCandidate> candidates, int seed, int ruleIndex, int densityPercent)
        {
            var rng = new MapGenRandom(seed).Fork($"props:{ruleIndex}");
            Shuffle(candidates, ref rng);
            var count = Math.Max(1, (candidates.Count * Math.Min(100, Math.Max(0, densityPercent)) + 99) / 100);
            if (count >= candidates.Count)
            {
                return ToCoords(candidates, candidates.Count);
            }

            return ToCoords(candidates, count);
        }

        private static MapGenGridCoord PickRandom(List<PropCandidate> candidates, int seed, int ruleIndex)
        {
            var rng = new MapGenRandom(seed).Fork($"props:unique:{ruleIndex}");
            return candidates[rng.NextInt(0, candidates.Count)].Coord;
        }

        private static MapGenGridCoord[] PickGrid(List<PropCandidate> candidates, int step)
        {
            candidates.Sort(CompareCoords);
            var picked = new List<MapGenGridCoord>();
            foreach (var candidate in candidates)
            {
                var coord = candidate.Coord;
                if (((coord.X + coord.Y) % step) == 0)
                {
                    picked.Add(coord);
                }
            }

            return picked.ToArray();
        }

        private static MapGenGridCoord[] PickOnePerRegion(List<PropCandidate> candidates)
        {
            candidates.Sort(CompareCoords);
            var seen = new HashSet<int>();
            var picked = new List<MapGenGridCoord>();
            foreach (var candidate in candidates)
            {
                if (seen.Add(candidate.RegionId))
                {
                    picked.Add(candidate.Coord);
                }
            }

            return picked.ToArray();
        }

        private static bool MatchesChannelKind(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord,
            MapGenMockupCell cell,
            MapGenPropPlacementRules rule)
        {
            switch (rule.ChannelKind)
            {
                case MapGenPropPlacementChannelKind.Floor:
                    return cell.State == MapGenCellState.Room || cell.State == MapGenCellState.Corridor;
                case MapGenPropPlacementChannelKind.RoomCenter:
                    return cell.State == MapGenCellState.Room && CountNavigableNeighbors(width, height, cells, coord) >= 4;
                case MapGenPropPlacementChannelKind.CorridorEdge:
                    return cell.State == MapGenCellState.Corridor && CountBoundaryNeighbors(width, height, cells, coord) > 0;
                case MapGenPropPlacementChannelKind.Entrance:
                    return cell.State == MapGenCellState.Connector;
                case MapGenPropPlacementChannelKind.Blocker:
                    return string.Equals(cell.PropChannel ?? string.Empty, rule.Channel, StringComparison.Ordinal);
                case MapGenPropPlacementChannelKind.Objective:
                case MapGenPropPlacementChannelKind.Custom:
                case MapGenPropPlacementChannelKind.Wall:
                case MapGenPropPlacementChannelKind.Corner:
                default:
                    return string.Equals(cell.PropChannel ?? string.Empty, rule.Channel, StringComparison.Ordinal);
            }
        }

        private static bool MatchesFilters(MapGenMockupCell cell, MapGenPropPlacementRules rule)
        {
            return MatchesRoomFilter(cell, rule) && MatchesCorridorFilter(cell, rule);
        }

        private static bool MatchesRoomFilter(MapGenMockupCell cell, MapGenPropPlacementRules rule)
        {
            var filters = rule.RoomCategoryFilters ?? Array.Empty<string>();
            if (filters.Length == 0)
            {
                return true;
            }

            if (cell.State != MapGenCellState.Room)
            {
                return false;
            }

            var category = cell.RoomCategory.ToString();
            foreach (var filter in filters)
            {
                if (string.Equals(filter, category, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesCorridorFilter(MapGenMockupCell cell, MapGenPropPlacementRules rule)
        {
            var filters = rule.CorridorKindFilters ?? Array.Empty<string>();
            if (filters.Length == 0)
            {
                return true;
            }

            if (cell.State != MapGenCellState.Corridor)
            {
                return false;
            }

            foreach (var filter in filters)
            {
                if (string.Equals(filter, cell.SourceTemplateId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PassesSpacing(MapGenGridCoord coord, HashSet<MapGenGridCoord> occupied, int minSpacingCells)
        {
            var spacing = Math.Max(0, minSpacingCells);
            foreach (var other in occupied)
            {
                if (Math.Abs(coord.X - other.X) + Math.Abs(coord.Y - other.Y) <= spacing)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TraversalRemainsConnected(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord blockedCoord)
        {
            var start = default(MapGenGridCoord);
            var hasStart = false;
            var total = 0;
            for (var index = 0; index < cells.Length; index++)
            {
                var coord = MapGenGridCoord.FromIndex(index, width);
                if (coord == blockedCoord || !IsNavigable(cells[index].State))
                {
                    continue;
                }

                total++;
                if (!hasStart)
                {
                    start = coord;
                    hasStart = true;
                }
            }

            if (!hasStart)
            {
                return true;
            }

            var visited = new HashSet<MapGenGridCoord> { start };
            var queue = new Queue<MapGenGridCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var coord = queue.Dequeue();
                foreach (var direction in CardinalDirections)
                {
                    var next = coord.Offset(direction);
                    if (!next.IsInBounds(width, height) || next == blockedCoord || visited.Contains(next))
                    {
                        continue;
                    }

                    if (!IsNavigable(cells[next.ToIndex(width)].State))
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return visited.Count == total;
        }

        private static int CountNavigableNeighbors(int width, int height, MapGenMockupCell[] cells, MapGenGridCoord coord)
        {
            var count = 0;
            foreach (var direction in CardinalDirections)
            {
                var neighbor = coord.Offset(direction);
                if (neighbor.IsInBounds(width, height) && IsNavigable(cells[neighbor.ToIndex(width)].State))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountBoundaryNeighbors(int width, int height, MapGenMockupCell[] cells, MapGenGridCoord coord)
        {
            var count = 0;
            foreach (var direction in CardinalDirections)
            {
                var neighbor = coord.Offset(direction);
                if (!neighbor.IsInBounds(width, height) || !IsNavigable(cells[neighbor.ToIndex(width)].State))
                {
                    count++;
                }
            }

            return count;
        }

        private static void Shuffle(List<PropCandidate> coords, ref MapGenRandom rng)
        {
            for (var i = coords.Count - 1; i > 0; i--)
            {
                var j = rng.NextInt(0, i + 1);
                (coords[i], coords[j]) = (coords[j], coords[i]);
            }
        }

        private static MapGenGridCoord[] ToCoords(List<PropCandidate> candidates, int count)
        {
            var coords = new MapGenGridCoord[count];
            for (var i = 0; i < count; i++)
            {
                coords[i] = candidates[i].Coord;
            }

            return coords;
        }

        private static int CompareCoords(PropCandidate left, PropCandidate right)
        {
            var y = left.Coord.Y.CompareTo(right.Coord.Y);
            return y != 0 ? y : left.Coord.X.CompareTo(right.Coord.X);
        }

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }

        private static readonly MapGenGridDirection[] CardinalDirections =
        {
            MapGenGridDirection.North,
            MapGenGridDirection.East,
            MapGenGridDirection.South,
            MapGenGridDirection.West
        };

        private readonly struct PropCandidate
        {
            public readonly MapGenGridCoord Coord;
            public readonly int RegionId;

            public PropCandidate(MapGenGridCoord coord, int regionId)
            {
                Coord = coord;
                RegionId = regionId;
            }
        }
    }

    [Serializable]
    public sealed class MapGenPropPlacementResult
    {
        public MapGenPlacedProp[] PlacedProps = Array.Empty<MapGenPlacedProp>();
        public MapGenPropPlacementReport Report = new MapGenPropPlacementReport();
    }

    [Serializable]
    public struct MapGenPlacedProp
    {
        public MapGenGridCoord Coord;
        public string Channel;
        public int RuleIndex;
        public int RegionId;
        public MapGenRoomCategory RoomCategory;
        public MapGenPropPlacementChannelKind ChannelKind;
        public MapGenPropDistributionMode DistributionMode;
        public bool BlocksTraversal;
    }

    [Serializable]
    public sealed class MapGenPropPlacementReport
    {
        public int TotalCandidateCells;
        public int PlacedCount;
        public int RejectedBySpacing;
        public int BlockerTraversalIssues;
        public int MissingRequiredUniqueProps;
        public List<MapGenIssue> Issues = new List<MapGenIssue>();

        public bool IsValid => BlockerTraversalIssues == 0 && MissingRequiredUniqueProps == 0 && Issues.Count == 0;
    }
}

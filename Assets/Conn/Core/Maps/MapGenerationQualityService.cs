using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
    public static class MapGenerationQualityService
    {
        public static MapValidationReport ValidateProductionShape(MapProfile profile, GeneratedMapDraft draft)
        {
            var report = new MapValidationReport();
            if (profile == null)
            {
                report.Errors.Add("Map generation quality validation requires a profile.");
                return report;
            }

            if (draft?.Graph == null)
            {
                report.Errors.Add("Map generation quality validation requires a generated graph.");
                return report;
            }

            ExpectNoDuplicateRoomCoordinates(draft.Graph, report);
            ExpectRequiredLayoutKinds(draft.Graph, report);
            ExpectBranchCoverage(profile, draft.Graph, report);
            ExpectLoopCoverage(profile, draft.Graph, report);
            return report;
        }

        public static MapValidationReport ValidateCompiledExplorationRhythm(MapProfile profile, CompiledMap compiled)
        {
            var report = new MapValidationReport();
            if (compiled == null)
            {
                report.Errors.Add("Compiled exploration rhythm validation requires a compiled map.");
                return report;
            }

            if (compiled.Cells == null || compiled.Cells.Count == 0)
            {
                report.Errors.Add("Compiled exploration rhythm validation requires baked cell payload.");
                return report;
            }

            var walkable = BuildWalkableCellSet(compiled);
            var start = FindPlacement(compiled, MapPlacementKind.Start);
            var quest = FindPlacement(compiled, MapPlacementKind.QuestTarget);
            var boss = FindPlacement(compiled, MapPlacementKind.Boss);
            var exit = FindPlacement(compiled, MapPlacementKind.Exit);
            ExpectRequiredRoute(compiled, walkable, start, quest, "quest target", report);
            ExpectRequiredRoute(compiled, walkable, start, boss, "boss", report);
            ExpectRequiredRoute(compiled, walkable, start, exit, "exit", report);

            if (start == null || quest == null || boss == null || exit == null)
            {
                return report;
            }

            var startDistances = BuildDistanceMap(compiled, walkable, start.X, start.Y);
            var questDistance = DistanceTo(startDistances, quest);
            var bossDistance = DistanceTo(startDistances, boss);
            var exitDistance = DistanceTo(startDistances, exit);
            var minimumQuestDistance = Math.Max(6, (profile != null && profile.RoomWidth > 0) ? profile.RoomWidth / 2 : 0);
            if (questDistance >= 0 && questDistance < minimumQuestDistance)
            {
                report.Errors.Add($"Compiled map reaches quest target after {questDistance} cells, expected at least {minimumQuestDistance} for readable dungeon pacing.");
            }

            if (questDistance >= 0 && bossDistance >= 0 && bossDistance <= questDistance)
            {
                report.Warnings.Add($"Boss route distance {bossDistance} is not beyond quest target distance {questDistance}.");
            }

            if (bossDistance >= 0 && exitDistance >= 0 && exitDistance <= bossDistance)
            {
                report.Warnings.Add($"Exit route distance {exitDistance} is not beyond boss distance {bossDistance}.");
            }

            ExpectReadableChoicePoints(compiled, walkable, report);
            ExpectReachableRewardsAndEncounters(compiled, walkable, startDistances, report);
            return report;
        }

        private static void ExpectNoDuplicateRoomCoordinates(RoomGraph graph, MapValidationReport report)
        {
            var occupied = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                var key = $"{node.GridX},{node.GridY}";
                if (occupied.TryGetValue(key, out var previousRoomId))
                {
                    report.Errors.Add($"Generated room {node.Id} overlaps room {previousRoomId} at graph coordinate ({node.GridX}, {node.GridY}).");
                    continue;
                }

                occupied.Add(key, node.Id ?? string.Empty);
            }
        }

        private static void ExpectRequiredLayoutKinds(RoomGraph graph, MapValidationReport report)
        {
            if (!HasLayoutKind(graph, RoomChunkLayoutKind.Hub))
            {
                report.Errors.Add("Generated map has no hub room.");
            }

            if (!HasLayoutKind(graph, RoomChunkLayoutKind.Corridor))
            {
                report.Errors.Add("Generated map has no corridor room.");
            }

            if (!HasLayoutKind(graph, RoomChunkLayoutKind.DeadEnd))
            {
                report.Errors.Add("Generated map has no dead-end room.");
            }

            if (!HasLayoutKind(graph, RoomChunkLayoutKind.HeightTransition))
            {
                report.Errors.Add("Generated map has no height-transition room.");
            }
        }

        private static void ExpectBranchCoverage(MapProfile profile, RoomGraph graph, MapValidationReport report)
        {
            var sideBranchRooms = 0;
            var northBranches = 0;
            var southBranches = 0;
            var criticalPathY = FindCriticalPathY(graph);
            foreach (var node in graph.Nodes)
            {
                if (node.Role != MapRoomRole.SideBranch)
                {
                    continue;
                }

                sideBranchRooms++;
                if (node.GridY > criticalPathY)
                {
                    northBranches++;
                }
                else if (node.GridY < criticalPathY)
                {
                    southBranches++;
                }
            }

            var expectedBranchRooms = Math.Max(0, profile.SideBranchCount) * 2;
            if (sideBranchRooms < expectedBranchRooms)
            {
                report.Errors.Add($"Generated map has {sideBranchRooms} side-branch rooms, expected at least {expectedBranchRooms}.");
            }

            if (profile.SideBranchCount >= 2 && (northBranches == 0 || southBranches == 0))
            {
                report.Errors.Add("Generated side branches do not cover both north and south of the main path.");
            }
        }

        private static void ExpectLoopCoverage(MapProfile profile, RoomGraph graph, MapValidationReport report)
        {
            var loopEdges = 0;
            if (profile.SideBranchCount < 2 && profile.LoopMin > 0)
            {
                report.Errors.Add($"Generated map has 0 loop edge(s), expected at least {profile.LoopMin}.");
                return;
            }

            foreach (var edge in graph.Edges)
            {
                if (string.Equals(edge.Kind, "merge", StringComparison.Ordinal))
                {
                    loopEdges++;
                }
            }

            if (loopEdges < Math.Max(0, profile.LoopMin))
            {
                report.Errors.Add($"Generated map has {loopEdges} loop edge(s), expected at least {profile.LoopMin}.");
            }
        }

        private static int FindCriticalPathY(RoomGraph graph)
        {
            var total = 0f;
            var count = 0;
            foreach (var node in graph.Nodes)
            {
                if (node.PathIndex >= 0)
                {
                    total += node.GridY;
                    count++;
                }
            }

            return count > 0 ? (int)Math.Round(total / count, MidpointRounding.AwayFromZero) : 0;
        }

        private static bool HasLayoutKind(RoomGraph graph, RoomChunkLayoutKind layoutKind)
        {
            foreach (var node in graph.Nodes)
            {
                if (node.LayoutKind == layoutKind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ExpectRequiredRoute(
            CompiledMap compiled,
            HashSet<string> walkable,
            MapPlacement start,
            MapPlacement target,
            string targetLabel,
            MapValidationReport report)
        {
            if (start == null)
            {
                report.Errors.Add("Compiled exploration rhythm requires a start placement.");
                return;
            }

            if (target == null)
            {
                report.Errors.Add($"Compiled exploration rhythm requires a {targetLabel} placement.");
                return;
            }

            var distances = BuildDistanceMap(compiled, walkable, start.X, start.Y);
            if (!distances.ContainsKey(CellKey(target.X, target.Y)))
            {
                report.Errors.Add($"Compiled map has no walkable route from start to {targetLabel}.");
            }
        }

        private static void ExpectReadableChoicePoints(CompiledMap compiled, HashSet<string> walkable, MapValidationReport report)
        {
            var choicePoints = 0;
            foreach (var key in walkable)
            {
                if (!TryParseCellKey(key, out var x, out var y))
                {
                    continue;
                }

                if (CountWalkableNeighbors(compiled, walkable, x, y) >= 3)
                {
                    choicePoints++;
                }
            }

            if (choicePoints == 0)
            {
                report.Warnings.Add("Compiled map has no 3-way walkable choice point, so exploration may feel like a single corridor.");
            }
        }

        private static void ExpectReachableRewardsAndEncounters(
            CompiledMap compiled,
            HashSet<string> walkable,
            Dictionary<string, int> startDistances,
            MapValidationReport report)
        {
            var reachableInteractiveObjects = 0;
            foreach (var placement in compiled.Objects ?? new List<CompiledMapObjectPlacement>())
            {
                if (placement == null || !IsInteractiveObject(placement.Kind))
                {
                    continue;
                }

                if (startDistances.ContainsKey(CellKey(placement.X, placement.Y)))
                {
                    reachableInteractiveObjects++;
                }
            }

            if ((compiled.Objects?.Count ?? 0) > 0 && reachableInteractiveObjects == 0)
            {
                report.Warnings.Add("Compiled map has objects but no reachable chest/barrel reward from start.");
            }

            var reachableMonsters = 0;
            foreach (var placement in compiled.Placements ?? new List<MapPlacement>())
            {
                if (placement == null || placement.Kind != MapPlacementKind.Monster)
                {
                    continue;
                }

                if (startDistances.ContainsKey(CellKey(placement.X, placement.Y)))
                {
                    reachableMonsters++;
                }
            }

            if (reachableMonsters == 0)
            {
                report.Warnings.Add("Compiled map has no reachable optional monster placement from start.");
            }
        }

        private static HashSet<string> BuildWalkableCellSet(CompiledMap compiled)
        {
            var walkable = new HashSet<string>();
            foreach (var cell in compiled.Cells ?? new List<CompiledMapCell>())
            {
                if (cell == null || cell.X < 0 || cell.Y < 0 || cell.X >= compiled.Width || cell.Y >= compiled.Height)
                {
                    continue;
                }

                if (cell.Terrain != RoomChunkCellType.Wall && cell.Terrain != RoomChunkCellType.Gap)
                {
                    walkable.Add(CellKey(cell.X, cell.Y));
                }
            }

            return walkable;
        }

        private static Dictionary<string, int> BuildDistanceMap(CompiledMap compiled, HashSet<string> walkable, int startX, int startY)
        {
            var distances = new Dictionary<string, int>();
            var startKey = CellKey(startX, startY);
            if (!walkable.Contains(startKey))
            {
                return distances;
            }

            var open = new Queue<string>();
            distances[startKey] = 0;
            open.Enqueue(startKey);
            while (open.Count > 0)
            {
                var current = open.Dequeue();
                if (!TryParseCellKey(current, out var x, out var y))
                {
                    continue;
                }

                TryVisit(compiled, walkable, distances, open, x + 1, y, distances[current] + 1);
                TryVisit(compiled, walkable, distances, open, x, y + 1, distances[current] + 1);
                TryVisit(compiled, walkable, distances, open, x - 1, y, distances[current] + 1);
                TryVisit(compiled, walkable, distances, open, x, y - 1, distances[current] + 1);
            }

            return distances;
        }

        private static void TryVisit(
            CompiledMap compiled,
            HashSet<string> walkable,
            Dictionary<string, int> distances,
            Queue<string> open,
            int x,
            int y,
            int distance)
        {
            if (x < 0 || y < 0 || x >= compiled.Width || y >= compiled.Height)
            {
                return;
            }

            var key = CellKey(x, y);
            if (!walkable.Contains(key) || distances.ContainsKey(key))
            {
                return;
            }

            distances[key] = distance;
            open.Enqueue(key);
        }

        private static int CountWalkableNeighbors(CompiledMap compiled, HashSet<string> walkable, int x, int y)
        {
            var count = 0;
            if (x + 1 < compiled.Width && walkable.Contains(CellKey(x + 1, y)))
            {
                count++;
            }

            if (y + 1 < compiled.Height && walkable.Contains(CellKey(x, y + 1)))
            {
                count++;
            }

            if (x - 1 >= 0 && walkable.Contains(CellKey(x - 1, y)))
            {
                count++;
            }

            if (y - 1 >= 0 && walkable.Contains(CellKey(x, y - 1)))
            {
                count++;
            }

            return count;
        }

        private static MapPlacement FindPlacement(CompiledMap compiled, MapPlacementKind kind)
        {
            foreach (var placement in compiled.Placements ?? new List<MapPlacement>())
            {
                if (placement != null && placement.Kind == kind)
                {
                    return placement;
                }
            }

            return null;
        }

        private static int DistanceTo(Dictionary<string, int> distances, MapPlacement placement)
        {
            return placement != null && distances.TryGetValue(CellKey(placement.X, placement.Y), out var distance)
                ? distance
                : -1;
        }

        private static bool IsInteractiveObject(RoomChunkObjectKind kind)
        {
            return kind == RoomChunkObjectKind.Chest || kind == RoomChunkObjectKind.Barrel;
        }

        private static string CellKey(int x, int y)
        {
            return $"{x}:{y}";
        }

        private static bool TryParseCellKey(string key, out int x, out int y)
        {
            x = 0;
            y = 0;
            var parts = (key ?? string.Empty).Split(':');
            return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
        }
    }
}

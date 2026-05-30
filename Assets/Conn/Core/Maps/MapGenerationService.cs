using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
    public static class MapGenerationService
    {
        private const int MaxGenerateRetries = 12;

        public static GeneratedMapDraft Generate(MapProfile profile, IReadOnlyList<ChunkPreset> chunks, int seed)
        {
            ValidateProfile(profile);

            var roomCountMin = ResolveRoomCountMin(profile);
            var roomCountMax = ResolveRoomCountMax(profile, roomCountMin);
            var pathMin = Math.Max(4, Math.Min(profile.CriticalPathMin, roomCountMax));
            var pathMax = Math.Max(pathMin, Math.Min(profile.CriticalPathMax, roomCountMax));
            var roomPools = ResolveRoomPools(profile, chunks);
            var templates = BuildTemplates(roomPools, chunks);

            string lastFailure = "unknown contradiction";
            for (var retryIndex = 0; retryIndex < MaxGenerateRetries; retryIndex++)
            {
                var attemptSeed = seed + (retryIndex * 7919);
                var random = new Random(attemptSeed);

                try
                {
                    return GenerateAttempt(
                        profile,
                        chunks,
                        roomPools,
                        templates,
                        seed,
                        attemptSeed,
                        retryIndex,
                        roomCountMin,
                        roomCountMax,
                        pathMin,
                        pathMax,
                        random);
                }
                catch (InvalidOperationException exception)
                {
                    lastFailure = exception.Message;
                }
            }

            throw new InvalidOperationException(
                $"Map generation failed for profile {profile.ProfileId} after {MaxGenerateRetries} deterministic retries. Last contradiction: {lastFailure}");
        }

        public static CompiledMap Compile(MapProfile profile, GeneratedMapDraft draft)
        {
            return new CompiledMap
            {
                MapId = $"{draft.ProfileId}_{draft.Seed}",
                ProfileId = draft.ProfileId,
                Seed = draft.Seed,
                Width = profile.Width,
                Height = profile.Height,
                Rooms = new List<RoomGraphNode>(draft.Graph.Nodes),
                Doors = new List<RoomGraphEdge>(draft.Graph.Edges),
                Placements = new List<MapPlacement>(draft.Placements)
            };
        }

        public static CompiledMap GenerateCompiled(MapProfile profile, IReadOnlyList<ChunkPreset> chunks, int seed)
        {
            return Compile(profile, Generate(profile, chunks, seed));
        }

        private static GeneratedMapDraft GenerateAttempt(
            MapProfile profile,
            IReadOnlyList<ChunkPreset> chunks,
            IReadOnlyList<RuntimeMapRoomPoolRule> roomPools,
            IReadOnlyList<SolverTemplate> templates,
            int seed,
            int attemptSeed,
            int retryIndex,
            int roomCountMin,
            int roomCountMax,
            int pathMin,
            int pathMax,
            Random random)
        {
            var targetRoomCount = random.Next(roomCountMin, roomCountMax + 1);
            var targetLoops = Math.Min(
                Math.Max(0, random.Next(profile.LoopMin, profile.LoopMax + 1)),
                Math.Max(0, targetRoomCount / 3));
            var gridWidth = Math.Max(1, profile.Width / Math.Max(1, profile.RoomWidth));
            var gridHeight = Math.Max(1, profile.Height / Math.Max(1, profile.RoomHeight));
            var occupiedCells = BuildOccupiedCells(gridWidth, gridHeight, targetRoomCount, targetLoops, random);
            var neighborLookup = BuildNeighborLookup(occupiedCells);
            var criticalPath = FindCriticalPath(occupiedCells, neighborLookup, pathMin, pathMax, attemptSeed);
            var plan = BuildNodePlan(profile, occupiedCells, neighborLookup, criticalPath, roomPools, attemptSeed);
            var states = BuildSolverStates(plan, templates);
            SolveStates(states, templates, attemptSeed, retryIndex);
            return BuildDraft(profile, seed, states, templates);
        }

        private static List<CellCoordinate> BuildOccupiedCells(int width, int height, int targetRoomCount, int targetLoops, Random random)
        {
            var start = new CellCoordinate(width / 2, height / 2);
            var occupied = new List<CellCoordinate> { start };
            var occupiedSet = new HashSet<string>(StringComparer.Ordinal) { CellKey(start.X, start.Y) };
            var center = start;

            while (occupied.Count < targetRoomCount)
            {
                var options = BuildFrontierOptions(occupied, occupiedSet, width, height, center, targetLoops);
                if (options.Count == 0)
                {
                    throw new InvalidOperationException("Active-cell solver ran out of frontier cells before reaching the target room count.");
                }

                var selected = options[ChooseWeightedOption(options, random)];
                occupied.Add(selected.Cell);
                occupiedSet.Add(CellKey(selected.Cell.X, selected.Cell.Y));
                if (selected.AdjacentActiveCount >= 2 && targetLoops > 0)
                {
                    targetLoops--;
                }
            }

            return occupied;
        }

        private static List<FrontierOption> BuildFrontierOptions(
            List<CellCoordinate> occupied,
            HashSet<string> occupiedSet,
            int width,
            int height,
            CellCoordinate center,
            int remainingLoops)
        {
            var optionsByKey = new Dictionary<string, FrontierOption>(StringComparer.Ordinal);
            for (var i = 0; i < occupied.Count; i++)
            {
                foreach (var neighbor in EnumerateNeighbors(occupied[i]))
                {
                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height)
                    {
                        continue;
                    }

                    var key = CellKey(neighbor.X, neighbor.Y);
                    if (occupiedSet.Contains(key))
                    {
                        continue;
                    }

                    var adjacentActiveCount = CountAdjacentOccupied(neighbor, occupiedSet);
                    var distanceFromCenter = Math.Abs(neighbor.X - center.X) + Math.Abs(neighbor.Y - center.Y);
                    var weight = 4 + distanceFromCenter;
                    if (adjacentActiveCount == 1)
                    {
                        weight += 3;
                    }

                    if (remainingLoops > 0 && adjacentActiveCount >= 2)
                    {
                        weight += 7;
                    }

                    optionsByKey[key] = new FrontierOption
                    {
                        Cell = neighbor,
                        AdjacentActiveCount = adjacentActiveCount,
                        Weight = Math.Max(1, weight)
                    };
                }
            }

            return new List<FrontierOption>(optionsByKey.Values);
        }

        private static int ChooseWeightedOption(List<FrontierOption> options, Random random)
        {
            var totalWeight = 0;
            for (var i = 0; i < options.Count; i++)
            {
                totalWeight += Math.Max(1, options[i].Weight);
            }

            var roll = random.Next(0, totalWeight);
            for (var i = 0; i < options.Count; i++)
            {
                var weight = Math.Max(1, options[i].Weight);
                if (roll < weight)
                {
                    return i;
                }

                roll -= weight;
            }

            return 0;
        }

        private static Dictionary<string, List<CellCoordinate>> BuildNeighborLookup(List<CellCoordinate> occupiedCells)
        {
            var occupiedSet = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < occupiedCells.Count; i++)
            {
                occupiedSet.Add(CellKey(occupiedCells[i].X, occupiedCells[i].Y));
            }

            var lookup = new Dictionary<string, List<CellCoordinate>>(StringComparer.Ordinal);
            for (var i = 0; i < occupiedCells.Count; i++)
            {
                var cell = occupiedCells[i];
                var neighbors = new List<CellCoordinate>();
                foreach (var neighbor in EnumerateNeighbors(cell))
                {
                    if (occupiedSet.Contains(CellKey(neighbor.X, neighbor.Y)))
                    {
                        neighbors.Add(neighbor);
                    }
                }

                lookup.Add(CellKey(cell.X, cell.Y), neighbors);
            }

            return lookup;
        }

        private static List<CellCoordinate> FindCriticalPath(
            List<CellCoordinate> occupiedCells,
            Dictionary<string, List<CellCoordinate>> neighborLookup,
            int pathMin,
            int pathMax,
            int seed)
        {
            List<CellCoordinate> bestPath = null;
            for (var i = 0; i < occupiedCells.Count; i++)
            {
                for (var j = i + 1; j < occupiedCells.Count; j++)
                {
                    var path = FindShortestPath(occupiedCells[i], occupiedCells[j], neighborLookup);
                    if (path.Count < pathMin || path.Count > pathMax)
                    {
                        continue;
                    }

                    if (bestPath == null
                        || path.Count > bestPath.Count
                        || (path.Count == bestPath.Count && PathTieBreak(path, bestPath, seed) < 0))
                    {
                        bestPath = path;
                    }
                }
            }

            if (bestPath == null)
            {
                throw new InvalidOperationException(
                    $"Grid candidate solver could not find a critical path within range {pathMin}-{pathMax}.");
            }

            return OrderCriticalPath(bestPath);
        }

        private static List<CellCoordinate> OrderCriticalPath(List<CellCoordinate> path)
        {
            if (path.Count == 0)
            {
                return path;
            }

            var first = path[0];
            var last = path[path.Count - 1];
            if (first.X < last.X || (first.X == last.X && first.Y <= last.Y))
            {
                return path;
            }

            path.Reverse();
            return path;
        }

        private static int PathTieBreak(List<CellCoordinate> first, List<CellCoordinate> second, int seed)
        {
            return PositiveHash(seed, DescribePath(first), "critical_path")
                .CompareTo(PositiveHash(seed, DescribePath(second), "critical_path"));
        }

        private static string DescribePath(List<CellCoordinate> path)
        {
            var parts = new string[path.Count];
            for (var i = 0; i < path.Count; i++)
            {
                parts[i] = CellKey(path[i].X, path[i].Y);
            }

            return string.Join(">", parts);
        }

        private static List<CellCoordinate> FindShortestPath(
            CellCoordinate start,
            CellCoordinate target,
            Dictionary<string, List<CellCoordinate>> neighborLookup)
        {
            var startKey = CellKey(start.X, start.Y);
            var targetKey = CellKey(target.X, target.Y);
            var parents = new Dictionary<string, string>(StringComparer.Ordinal);
            var open = new Queue<CellCoordinate>();
            open.Enqueue(start);
            parents[startKey] = string.Empty;

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                var currentKey = CellKey(current.X, current.Y);
                if (currentKey == targetKey)
                {
                    break;
                }

                if (!neighborLookup.TryGetValue(currentKey, out var neighbors))
                {
                    continue;
                }

                for (var i = 0; i < neighbors.Count; i++)
                {
                    var neighborKey = CellKey(neighbors[i].X, neighbors[i].Y);
                    if (parents.ContainsKey(neighborKey))
                    {
                        continue;
                    }

                    parents[neighborKey] = currentKey;
                    open.Enqueue(neighbors[i]);
                }
            }

            if (!parents.ContainsKey(targetKey))
            {
                throw new InvalidOperationException($"Active-cell graph is disconnected between {startKey} and {targetKey}.");
            }

            var path = new List<CellCoordinate>();
            var cursor = targetKey;
            while (!string.IsNullOrEmpty(cursor))
            {
                path.Add(ParseCell(cursor));
                cursor = parents[cursor];
            }

            path.Reverse();
            return path;
        }

        private static NodePlan BuildNodePlan(
            MapProfile profile,
            List<CellCoordinate> occupiedCells,
            Dictionary<string, List<CellCoordinate>> neighborLookup,
            List<CellCoordinate> criticalPath,
            IReadOnlyList<RuntimeMapRoomPoolRule> roomPools,
            int seed)
        {
            var plan = new NodePlan();
            var pathIndexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < criticalPath.Count; i++)
            {
                pathIndexByKey[CellKey(criticalPath[i].X, criticalPath[i].Y)] = i;
            }

            var parentByKey = BuildBranchParents(criticalPath, neighborLookup);
            var sideBranchCount = 0;
            var northBranchRooms = 0;
            var southBranchRooms = 0;
            var criticalPathY = criticalPath[0].Y;

            for (var i = 0; i < occupiedCells.Count; i++)
            {
                var cell = occupiedCells[i];
                var key = CellKey(cell.X, cell.Y);
                var neighbors = neighborLookup[key];
                var sockets = BuildSocketMask(cell, neighbors);
                var pathIndex = pathIndexByKey.TryGetValue(key, out var foundPathIndex) ? foundPathIndex : -1;
                var parentCellKey = parentByKey.TryGetValue(key, out var resolvedParentKey) ? resolvedParentKey : string.Empty;
                var role = ResolveRoleForPathIndex(pathIndex, criticalPath.Count);
                if (pathIndex < 0)
                {
                    role = MapRoomRole.SideBranch;
                    sideBranchCount++;
                    if (cell.Y > criticalPathY)
                    {
                        northBranchRooms++;
                    }
                    else if (cell.Y < criticalPathY)
                    {
                        southBranchRooms++;
                    }
                }

                plan.Nodes.Add(new PlannedNode
                {
                    Cell = cell,
                    Role = role,
                    RequiredSockets = sockets,
                    PathIndex = pathIndex,
                    BranchDepth = ComputeBranchDepth(key, parentByKey, pathIndexByKey),
                    ParentCellKey = parentCellKey,
                    NeighborCells = neighbors
                });
            }

            var expectedBranchRooms = Math.Max(0, profile.SideBranchCount) * 2;
            if (sideBranchCount < expectedBranchRooms)
            {
                throw new InvalidOperationException(
                    $"Grid candidate solver produced {sideBranchCount} side-branch rooms, expected at least {expectedBranchRooms}.");
            }

            if (profile.SideBranchCount >= 2 && (northBranchRooms == 0 || southBranchRooms == 0))
            {
                throw new InvalidOperationException("Grid candidate solver did not cover both north and south branch space.");
            }

            ApplyForcedPoolRoles(plan, roomPools, seed);
            BuildNeighborLinks(plan);
            var mergeEdgeCount = CountMergeEdges(plan.Nodes);
            if (mergeEdgeCount < Math.Max(0, profile.LoopMin))
            {
                throw new InvalidOperationException(
                    $"Grid candidate solver produced {mergeEdgeCount} merge edge(s), expected at least {profile.LoopMin}.");
            }

            return plan;
        }

        private static Dictionary<string, string> BuildBranchParents(
            List<CellCoordinate> criticalPath,
            Dictionary<string, List<CellCoordinate>> neighborLookup)
        {
            var parents = new Dictionary<string, string>(StringComparer.Ordinal);
            var open = new Queue<CellCoordinate>();
            for (var i = 0; i < criticalPath.Count; i++)
            {
                var key = CellKey(criticalPath[i].X, criticalPath[i].Y);
                parents[key] = string.Empty;
                open.Enqueue(criticalPath[i]);
            }

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                var currentKey = CellKey(current.X, current.Y);
                if (!neighborLookup.TryGetValue(currentKey, out var neighbors))
                {
                    continue;
                }

                for (var i = 0; i < neighbors.Count; i++)
                {
                    var neighborKey = CellKey(neighbors[i].X, neighbors[i].Y);
                    if (parents.ContainsKey(neighborKey))
                    {
                        continue;
                    }

                    parents[neighborKey] = currentKey;
                    open.Enqueue(neighbors[i]);
                }
            }

            return parents;
        }

        private static int ComputeBranchDepth(
            string cellKey,
            Dictionary<string, string> parentByKey,
            Dictionary<string, int> pathIndexByKey)
        {
            if (pathIndexByKey.ContainsKey(cellKey))
            {
                return 0;
            }

            var depth = 0;
            var cursor = cellKey;
            while (parentByKey.TryGetValue(cursor, out var parent) && !string.IsNullOrEmpty(parent))
            {
                depth++;
                cursor = parent;
            }

            return depth;
        }

        private static void ApplyForcedPoolRoles(NodePlan plan, IReadOnlyList<RuntimeMapRoomPoolRule> roomPools, int seed)
        {
            var hasHub = HasPool(roomPools, MapRoomPoolRole.Hub);
            var hasCorridor = HasPool(roomPools, MapRoomPoolRole.Corridor);
            var hasHeightTransition = HasPool(roomPools, MapRoomPoolRole.HeightTransition);
            var hasDeadEnd = HasPool(roomPools, MapRoomPoolRole.DeadEnd);

            if (hasHub)
            {
                var hubIndex = FindNodeIndex(plan.Nodes, node => node.PathIndex > 0 && node.PathIndex < plan.CriticalPathLength - 1 && node.NeighborCells.Count >= 3);
                if (hubIndex < 0)
                {
                    throw new InvalidOperationException("Grid candidate solver could not reserve a hub cell on the critical path.");
                }

                plan.Nodes[hubIndex].ForcedPoolRole = MapRoomPoolRole.Hub;
            }

            if (hasCorridor)
            {
                var corridorIndex = FindNodeIndex(plan.Nodes, node =>
                    node.PathIndex > 0
                    && node.PathIndex < plan.CriticalPathLength - 1
                    && node.ForcedPoolRole == null
                    && node.NeighborCells.Count == 2
                    && IsOpposingSockets(node.RequiredSockets));
                if (corridorIndex < 0)
                {
                    throw new InvalidOperationException("Grid candidate solver could not reserve a corridor cell with opposing sockets.");
                }

                plan.Nodes[corridorIndex].ForcedPoolRole = MapRoomPoolRole.Corridor;
            }

            if (hasHeightTransition)
            {
                var candidates = new List<int>();
                for (var i = 0; i < plan.Nodes.Count; i++)
                {
                    var node = plan.Nodes[i];
                    if (node.PathIndex > 1
                        && node.PathIndex < plan.CriticalPathLength - 2
                        && node.ForcedPoolRole == null)
                    {
                        candidates.Add(i);
                    }
                }

                if (candidates.Count == 0)
                {
                    throw new InvalidOperationException("Grid candidate solver could not reserve a height-transition cell on the interior critical path.");
                }

                var selected = candidates[PositiveHash(seed, "height_transition", candidates.Count.ToString()) % candidates.Count];
                plan.Nodes[selected].ForcedPoolRole = MapRoomPoolRole.HeightTransition;
            }

            if (hasDeadEnd)
            {
                var deadEndIndex = FindNodeIndex(plan.Nodes, node =>
                    node.Role == MapRoomRole.SideBranch
                    && node.NeighborCells.Count == 1);
                if (deadEndIndex < 0)
                {
                    throw new InvalidOperationException("Grid candidate solver could not reserve a side-branch dead-end cell.");
                }

                plan.Nodes[deadEndIndex].ForcedPoolRole = MapRoomPoolRole.DeadEnd;
            }
        }

        private static bool HasPool(IReadOnlyList<RuntimeMapRoomPoolRule> roomPools, MapRoomPoolRole role)
        {
            for (var i = 0; i < roomPools.Count; i++)
            {
                if (roomPools[i] != null && roomPools[i].Role == role && roomPools[i].AllowedChunkIds.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindNodeIndex(List<PlannedNode> nodes, Predicate<PlannedNode> predicate)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (predicate(nodes[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void BuildNeighborLinks(NodePlan plan)
        {
            var nodeIndexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < plan.Nodes.Count; i++)
            {
                nodeIndexByKey.Add(CellKey(plan.Nodes[i].Cell.X, plan.Nodes[i].Cell.Y), i);
            }

            for (var i = 0; i < plan.Nodes.Count; i++)
            {
                var node = plan.Nodes[i];
                for (var j = 0; j < node.NeighborCells.Count; j++)
                {
                    var neighbor = node.NeighborCells[j];
                    var neighborIndex = nodeIndexByKey[CellKey(neighbor.X, neighbor.Y)];
                    plan.Nodes[i].Neighbors.Add(new NeighborLink
                    {
                        NeighborIndex = neighborIndex,
                        DirectionFromSelf = DirectionBetween(node.Cell, neighbor)
                    });
                }
            }
        }

        private static int CountMergeEdges(List<PlannedNode> nodes)
        {
            var count = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                for (var j = 0; j < nodes[i].Neighbors.Count; j++)
                {
                    var neighborIndex = nodes[i].Neighbors[j].NeighborIndex;
                    if (neighborIndex <= i)
                    {
                        continue;
                    }

                    if (ClassifyEdge(nodes[i], nodes[neighborIndex]) == "merge")
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static List<SolverTemplate> BuildTemplates(IReadOnlyList<RuntimeMapRoomPoolRule> roomPools, IReadOnlyList<ChunkPreset> chunks)
        {
            var templates = new List<SolverTemplate>();
            for (var i = 0; i < roomPools.Count; i++)
            {
                var pool = roomPools[i];
                if (pool == null)
                {
                    continue;
                }

                for (var j = 0; j < pool.AllowedChunkIds.Count; j++)
                {
                    var chunk = FindChunkById(chunks, pool.AllowedChunkIds[j]);
                    if (chunk == null)
                    {
                        continue;
                    }

                    templates.Add(new SolverTemplate
                    {
                        Index = templates.Count,
                        PoolRole = pool.Role,
                        RoomRole = RoomRoleForPool(pool.Role),
                        LayoutKind = pool.LayoutKind,
                        Weight = Math.Max(1, pool.Weight),
                        Chunk = chunk
                    });
                }
            }

            return templates;
        }

        private static List<SolverNodeState> BuildSolverStates(NodePlan plan, IReadOnlyList<SolverTemplate> templates)
        {
            var states = new List<SolverNodeState>();
            for (var i = 0; i < plan.Nodes.Count; i++)
            {
                var node = plan.Nodes[i];
                var allowedPoolRoles = ResolveAllowedPoolRoles(node, plan.CriticalPathLength);
                var candidateIndices = new List<int>();
                for (var j = 0; j < templates.Count; j++)
                {
                    var template = templates[j];
                    if (!ContainsRole(allowedPoolRoles, template.PoolRole))
                    {
                        continue;
                    }

                    if (template.RoomRole != node.Role)
                    {
                        continue;
                    }

                    if (!template.Chunk.SupportsRequiredSockets(node.RequiredSockets))
                    {
                        continue;
                    }

                    candidateIndices.Add(template.Index);
                }

                if (candidateIndices.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Node {CellKey(node.Cell.X, node.Cell.Y)} has no candidate chunks. role={node.Role}, pools={DescribePoolRoles(allowedPoolRoles)}, sockets={node.RequiredSockets}");
                }

                states.Add(new SolverNodeState
                {
                    Node = node,
                    CandidateTemplateIndices = candidateIndices
                });
            }

            return states;
        }

        private static MapRoomPoolRole[] ResolveAllowedPoolRoles(PlannedNode node, int criticalPathLength)
        {
            if (node.ForcedPoolRole.HasValue)
            {
                return new[] { node.ForcedPoolRole.Value };
            }

            switch (node.Role)
            {
                case MapRoomRole.Start:
                    return new[] { MapRoomPoolRole.Start };
                case MapRoomRole.QuestTarget:
                    return new[] { MapRoomPoolRole.Quest };
                case MapRoomRole.Boss:
                    return new[] { MapRoomPoolRole.Boss };
                case MapRoomRole.Exit:
                    return new[] { MapRoomPoolRole.Exit };
                case MapRoomRole.SideBranch:
                    if (node.NeighborCells.Count <= 1)
                    {
                        return new[] { MapRoomPoolRole.Side, MapRoomPoolRole.DeadEnd };
                    }

                    return new[] { MapRoomPoolRole.Side };
                case MapRoomRole.MainPath:
                    var values = new List<MapRoomPoolRole> { MapRoomPoolRole.Main };
                    if (node.NeighborCells.Count >= 3)
                    {
                        values.Add(MapRoomPoolRole.Hub);
                    }

                    if (node.NeighborCells.Count == 2 && IsOpposingSockets(node.RequiredSockets))
                    {
                        values.Add(MapRoomPoolRole.Corridor);
                    }

                    if (node.PathIndex > 1 && node.PathIndex < criticalPathLength - 2)
                    {
                        values.Add(MapRoomPoolRole.HeightTransition);
                    }

                    return values.ToArray();
                default:
                    throw new InvalidOperationException($"Unsupported room role for candidate generation: {node.Role}");
            }
        }

        private static void SolveStates(
            List<SolverNodeState> states,
            IReadOnlyList<SolverTemplate> templates,
            int seed,
            int retryIndex)
        {
            PropagateConstraints(states, templates);

            while (true)
            {
                var nextIndex = SelectLowestEntropyState(states, seed, retryIndex);
                if (nextIndex < 0)
                {
                    return;
                }

                var selectedTemplateIndex = ChooseCandidate(states[nextIndex], templates, seed, retryIndex);
                states[nextIndex].CandidateTemplateIndices = new List<int> { selectedTemplateIndex };
                PropagateConstraints(states, templates);
            }
        }

        private static void PropagateConstraints(List<SolverNodeState> states, IReadOnlyList<SolverTemplate> templates)
        {
            var queue = new Queue<int>();
            for (var i = 0; i < states.Count; i++)
            {
                queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var state = states[index];
                for (var i = 0; i < state.Node.Neighbors.Count; i++)
                {
                    var neighborLink = state.Node.Neighbors[i];
                    if (PruneNeighborCandidates(state, states[neighborLink.NeighborIndex], templates, neighborLink.DirectionFromSelf))
                    {
                        if (states[neighborLink.NeighborIndex].CandidateTemplateIndices.Count == 0)
                        {
                            throw new InvalidOperationException(
                                $"Constraint propagation removed every candidate from node {CellKey(states[neighborLink.NeighborIndex].Node.Cell.X, states[neighborLink.NeighborIndex].Node.Cell.Y)}.");
                        }

                        queue.Enqueue(neighborLink.NeighborIndex);
                    }
                }
            }
        }

        private static bool PruneNeighborCandidates(
            SolverNodeState source,
            SolverNodeState neighbor,
            IReadOnlyList<SolverTemplate> templates,
            MapDirection directionFromSource)
        {
            var removed = false;
            var opposite = Opposite(directionFromSource);
            for (var i = neighbor.CandidateTemplateIndices.Count - 1; i >= 0; i--)
            {
                var neighborTemplate = templates[neighbor.CandidateTemplateIndices[i]];
                var compatible = false;
                for (var j = 0; j < source.CandidateTemplateIndices.Count; j++)
                {
                    var sourceTemplate = templates[source.CandidateTemplateIndices[j]];
                    var sourceSocket = sourceTemplate.Chunk.FindSocket(directionFromSource);
                    var neighborSocket = neighborTemplate.Chunk.FindSocket(opposite);
                    if (RoomChunkSocketRules.AreCompatible(sourceSocket, neighborSocket))
                    {
                        compatible = true;
                        break;
                    }
                }

                if (!compatible)
                {
                    neighbor.CandidateTemplateIndices.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

        private static int SelectLowestEntropyState(List<SolverNodeState> states, int seed, int retryIndex)
        {
            var selectedIndex = -1;
            var selectedEntropy = int.MaxValue;
            var selectedHash = int.MaxValue;
            for (var i = 0; i < states.Count; i++)
            {
                var entropy = states[i].CandidateTemplateIndices.Count;
                if (entropy <= 1)
                {
                    continue;
                }

                var tieHash = PositiveHash(
                    seed + retryIndex,
                    CellKey(states[i].Node.Cell.X, states[i].Node.Cell.Y),
                    "entropy");
                if (entropy < selectedEntropy || (entropy == selectedEntropy && tieHash < selectedHash))
                {
                    selectedEntropy = entropy;
                    selectedHash = tieHash;
                    selectedIndex = i;
                }
            }

            return selectedIndex;
        }

        private static int ChooseCandidate(SolverNodeState state, IReadOnlyList<SolverTemplate> templates, int seed, int retryIndex)
        {
            var totalWeight = 0;
            for (var i = 0; i < state.CandidateTemplateIndices.Count; i++)
            {
                totalWeight += Math.Max(1, templates[state.CandidateTemplateIndices[i]].Weight);
            }

            var roll = PositiveHash(
                seed + retryIndex,
                CellKey(state.Node.Cell.X, state.Node.Cell.Y),
                "collapse") % totalWeight;
            for (var i = 0; i < state.CandidateTemplateIndices.Count; i++)
            {
                var candidateIndex = state.CandidateTemplateIndices[i];
                var weight = Math.Max(1, templates[candidateIndex].Weight);
                if (roll < weight)
                {
                    return candidateIndex;
                }

                roll -= weight;
            }

            return state.CandidateTemplateIndices[0];
        }

        private static GeneratedMapDraft BuildDraft(
            MapProfile profile,
            int seed,
            List<SolverNodeState> states,
            IReadOnlyList<SolverTemplate> templates)
        {
            var draft = new GeneratedMapDraft
            {
                ProfileId = profile.ProfileId,
                Seed = seed
            };

            var idByCell = BuildRoomIds(states);
            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var template = templates[state.CandidateTemplateIndices[0]];
                var roomId = idByCell[CellKey(state.Node.Cell.X, state.Node.Cell.Y)];
                draft.Graph.Nodes.Add(new RoomGraphNode
                {
                    Id = roomId,
                    GridX = state.Node.Cell.X,
                    GridY = state.Node.Cell.Y,
                    Role = state.Node.Role,
                    LayoutKind = template.LayoutKind,
                    BranchDepth = state.Node.BranchDepth,
                    PathIndex = state.Node.PathIndex,
                    SocketMask = state.Node.RequiredSockets,
                    ChunkId = template.Chunk.Id
                });
            }

            AddCriticalPathIds(draft.Graph);

            for (var i = 0; i < states.Count; i++)
            {
                var from = states[i];
                var fromId = idByCell[CellKey(from.Node.Cell.X, from.Node.Cell.Y)];
                if (from.Node.Role == MapRoomRole.SideBranch && from.Node.NeighborCells.Count == 1)
                {
                    draft.Graph.SideBranches.Add(fromId);
                }

                for (var j = 0; j < from.Node.Neighbors.Count; j++)
                {
                    var neighborLink = from.Node.Neighbors[j];
                    if (neighborLink.NeighborIndex <= i)
                    {
                        continue;
                    }

                    var to = states[neighborLink.NeighborIndex];
                    var toId = idByCell[CellKey(to.Node.Cell.X, to.Node.Cell.Y)];
                    draft.Graph.Edges.Add(new RoomGraphEdge
                    {
                        FromNodeId = fromId,
                        ToNodeId = toId,
                        Kind = ClassifyEdge(from.Node, to.Node)
                    });
                }
            }

            AssignPlacements(draft, profile, states, templates, idByCell);
            return draft;
        }

        private static void AddCriticalPathIds(RoomGraph graph)
        {
            var ordered = new List<RoomGraphNode>();
            for (var i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].PathIndex >= 0)
                {
                    ordered.Add(graph.Nodes[i]);
                }
            }

            ordered.Sort((left, right) => left.PathIndex.CompareTo(right.PathIndex));
            for (var i = 0; i < ordered.Count; i++)
            {
                graph.CriticalPath.Add(ordered[i].Id);
            }
        }

        private static Dictionary<string, string> BuildRoomIds(List<SolverNodeState> states)
        {
            var ids = new Dictionary<string, string>(StringComparer.Ordinal);
            var sideIndex = 0;

            for (var i = 0; i < states.Count; i++)
            {
                var node = states[i].Node;
                var key = CellKey(node.Cell.X, node.Cell.Y);
                if (node.PathIndex == 0)
                {
                    ids[key] = "start";
                }
                else if (node.Role == MapRoomRole.QuestTarget)
                {
                    ids[key] = "quest";
                }
                else if (node.Role == MapRoomRole.Boss)
                {
                    ids[key] = "boss";
                }
                else if (node.Role == MapRoomRole.Exit)
                {
                    ids[key] = "exit";
                }
                else if (node.Role == MapRoomRole.MainPath)
                {
                    ids[key] = $"main_{node.PathIndex}";
                }
                else
                {
                    ids[key] = $"side_{sideIndex++}";
                }
            }

            return ids;
        }

        private static string ClassifyEdge(PlannedNode from, PlannedNode to)
        {
            if (from.PathIndex >= 0 && to.PathIndex >= 0 && Math.Abs(from.PathIndex - to.PathIndex) == 1)
            {
                return "main";
            }

            var fromKey = CellKey(from.Cell.X, from.Cell.Y);
            var toKey = CellKey(to.Cell.X, to.Cell.Y);
            if (string.Equals(from.ParentCellKey, toKey, StringComparison.Ordinal)
                || string.Equals(to.ParentCellKey, fromKey, StringComparison.Ordinal))
            {
                return "branch";
            }

            if (from.PathIndex >= 0 && to.PathIndex >= 0)
            {
                return "merge";
            }

            return from.PathIndex < 0 && to.PathIndex < 0 ? "merge" : "branch";
        }

        private static void AssignPlacements(
            GeneratedMapDraft draft,
            MapProfile profile,
            List<SolverNodeState> states,
            IReadOnlyList<SolverTemplate> templates,
            Dictionary<string, string> idByCell)
        {
            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var template = templates[state.CandidateTemplateIndices[0]];
                var roomId = idByCell[CellKey(state.Node.Cell.X, state.Node.Cell.Y)];
                var runtimeNode = FindNode(draft.Graph, roomId);
                if (TryRequiredPlacementKind(runtimeNode.Role, out var placementKind))
                {
                    AddPlacement(draft, profile, runtimeNode, template.Chunk, placementKind, placementKind.ToString().ToLowerInvariant());
                }

                if (runtimeNode.Role == MapRoomRole.MainPath && HasAnchor(template.Chunk, MapAnchorKind.Monster))
                {
                    AddPlacement(draft, profile, runtimeNode, template.Chunk, MapPlacementKind.Monster, $"monster_{roomId}");
                }

                if (runtimeNode.Role == MapRoomRole.SideBranch && HasAnchor(template.Chunk, MapAnchorKind.Loot))
                {
                    AddPlacement(draft, profile, runtimeNode, template.Chunk, MapPlacementKind.Loot, $"loot_{roomId}");
                }
            }
        }

        private static void AddPlacement(
            GeneratedMapDraft draft,
            MapProfile profile,
            RoomGraphNode node,
            ChunkPreset chunk,
            MapPlacementKind placementKind,
            string placementId)
        {
            var anchor = FindAnchor(chunk, AnchorKindForPlacement(placementKind));
            draft.Placements.Add(new MapPlacement
            {
                Id = placementId,
                Kind = placementKind,
                RoomId = node.Id,
                X = node.GridX * profile.RoomWidth + anchor.X,
                Y = node.GridY * profile.RoomHeight + anchor.Y,
                ReferenceId = placementKind.ToString()
            });
        }

        private static ChunkAnchor FindAnchor(ChunkPreset chunk, MapAnchorKind kind)
        {
            for (var i = 0; i < chunk.Anchors.Count; i++)
            {
                if (chunk.Anchors[i].Kind == kind)
                {
                    return chunk.Anchors[i];
                }
            }

            throw new InvalidOperationException($"Chunk {chunk.Id} is missing required {kind} anchor.");
        }

        private static bool HasAnchor(ChunkPreset chunk, MapAnchorKind kind)
        {
            for (var i = 0; i < chunk.Anchors.Count; i++)
            {
                if (chunk.Anchors[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryRequiredPlacementKind(MapRoomRole role, out MapPlacementKind kind)
        {
            if (role == MapRoomRole.Start)
            {
                kind = MapPlacementKind.Start;
                return true;
            }

            if (role == MapRoomRole.QuestTarget)
            {
                kind = MapPlacementKind.QuestTarget;
                return true;
            }

            if (role == MapRoomRole.Boss)
            {
                kind = MapPlacementKind.Boss;
                return true;
            }

            if (role == MapRoomRole.Exit)
            {
                kind = MapPlacementKind.Exit;
                return true;
            }

            kind = MapPlacementKind.Start;
            return false;
        }

        private static MapAnchorKind AnchorKindForPlacement(MapPlacementKind kind)
        {
            return (MapAnchorKind)(int)kind;
        }

        private static IReadOnlyList<RuntimeMapRoomPoolRule> ResolveRoomPools(MapProfile profile, IReadOnlyList<ChunkPreset> chunks)
        {
            if (profile.RoomPools != null && profile.RoomPools.Count > 0)
            {
                return profile.RoomPools;
            }

            return BuildLegacyBridgeRoomPools(chunks);
        }

        private static List<RuntimeMapRoomPoolRule> BuildLegacyBridgeRoomPools(IReadOnlyList<ChunkPreset> chunks)
        {
            return new List<RuntimeMapRoomPoolRule>
            {
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Start, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Start, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Main, RoomChunkLayoutKind.Room, 1, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Corridor, RoomChunkLayoutKind.Corridor, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Corridor),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Hub, RoomChunkLayoutKind.Hub, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Hub),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Side, RoomChunkLayoutKind.Room, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.DeadEnd, RoomChunkLayoutKind.DeadEnd, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.DeadEnd),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Quest, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.QuestTarget, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Boss, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Boss, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.Exit, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Exit, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(chunks, MapRoomPoolRole.HeightTransition, RoomChunkLayoutKind.HeightTransition, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.HeightTransition)
            };
        }

        private static RuntimeMapRoomPoolRule CreateLegacyBridgePool(
            IReadOnlyList<ChunkPreset> chunks,
            MapRoomPoolRole poolRole,
            RoomChunkLayoutKind poolLayoutKind,
            int minCount,
            int maxCount,
            bool required,
            MapRoomRole chunkRole,
            RoomChunkLayoutKind chunkLayoutKind)
        {
            var pool = new RuntimeMapRoomPoolRule
            {
                Role = poolRole,
                LayoutKind = poolLayoutKind,
                MinCount = minCount,
                MaxCount = maxCount,
                Weight = 1,
                Required = required
            };

            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                if (chunk != null && chunk.LayoutKind == chunkLayoutKind && chunk.RoleTags.Contains(chunkRole))
                {
                    pool.AllowedChunkIds.Add(chunk.Id);
                }
            }

            return pool;
        }

        private static ChunkPreset FindChunkById(IReadOnlyList<ChunkPreset> chunks, string chunkId)
        {
            for (var i = 0; i < chunks.Count; i++)
            {
                if (chunks[i] != null && chunks[i].Id == chunkId)
                {
                    return chunks[i];
                }
            }

            return null;
        }

        private static MapRoomRole RoomRoleForPool(MapRoomPoolRole role)
        {
            switch (role)
            {
                case MapRoomPoolRole.Start:
                    return MapRoomRole.Start;
                case MapRoomPoolRole.Quest:
                    return MapRoomRole.QuestTarget;
                case MapRoomPoolRole.Boss:
                    return MapRoomRole.Boss;
                case MapRoomPoolRole.Exit:
                    return MapRoomRole.Exit;
                case MapRoomPoolRole.Side:
                case MapRoomPoolRole.DeadEnd:
                    return MapRoomRole.SideBranch;
                default:
                    return MapRoomRole.MainPath;
            }
        }

        private static bool ContainsRole(MapRoomPoolRole[] roles, MapRoomPoolRole role)
        {
            for (var i = 0; i < roles.Length; i++)
            {
                if (roles[i] == role)
                {
                    return true;
                }
            }

            return false;
        }

        private static string DescribePoolRoles(MapRoomPoolRole[] roles)
        {
            var values = new string[roles.Length];
            for (var i = 0; i < roles.Length; i++)
            {
                values[i] = roles[i].ToString();
            }

            return string.Join(",", values);
        }

        private static RoomGraphNode FindNode(RoomGraph graph, string id)
        {
            for (var i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].Id == id)
                {
                    return graph.Nodes[i];
                }
            }

            throw new InvalidOperationException($"Missing graph node {id}.");
        }

        private static MapRoomRole ResolveRoleForPathIndex(int index, int pathLength)
        {
            if (index == 0)
            {
                return MapRoomRole.Start;
            }

            if (index == pathLength - 3)
            {
                return MapRoomRole.QuestTarget;
            }

            if (index == pathLength - 2)
            {
                return MapRoomRole.Boss;
            }

            if (index == pathLength - 1)
            {
                return MapRoomRole.Exit;
            }

            return index >= 0 ? MapRoomRole.MainPath : MapRoomRole.SideBranch;
        }

        private static int ResolveRoomCountMin(MapProfile profile)
        {
            if (profile.RoomCountMin > 0)
            {
                return Math.Max(4, profile.RoomCountMin);
            }

            return Math.Max(4, profile.TargetModuleCount);
        }

        private static int ResolveRoomCountMax(MapProfile profile, int roomCountMin)
        {
            if (profile.RoomCountMax > 0)
            {
                return Math.Max(roomCountMin, profile.RoomCountMax);
            }

            return Math.Max(roomCountMin, Math.Max(profile.TargetModuleCount, profile.CriticalPathMax + (profile.SideBranchCount * 2)));
        }

        private static CellCoordinate ParseCell(string key)
        {
            var parts = key.Split(',');
            return new CellCoordinate(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        private static int CountAdjacentOccupied(CellCoordinate cell, HashSet<string> occupiedSet)
        {
            var count = 0;
            foreach (var neighbor in EnumerateNeighbors(cell))
            {
                if (occupiedSet.Contains(CellKey(neighbor.X, neighbor.Y)))
                {
                    count++;
                }
            }

            return count;
        }

        private static IEnumerable<CellCoordinate> EnumerateNeighbors(CellCoordinate cell)
        {
            yield return new CellCoordinate(cell.X, cell.Y + 1);
            yield return new CellCoordinate(cell.X + 1, cell.Y);
            yield return new CellCoordinate(cell.X, cell.Y - 1);
            yield return new CellCoordinate(cell.X - 1, cell.Y);
        }

        private static MapDirection BuildSocketMask(CellCoordinate from, List<CellCoordinate> neighbors)
        {
            var mask = MapDirection.None;
            for (var i = 0; i < neighbors.Count; i++)
            {
                mask |= DirectionBetween(from, neighbors[i]);
            }

            return mask;
        }

        private static MapDirection DirectionBetween(CellCoordinate from, CellCoordinate to)
        {
            if (to.X > from.X)
            {
                return MapDirection.East;
            }

            if (to.X < from.X)
            {
                return MapDirection.West;
            }

            return to.Y > from.Y ? MapDirection.North : MapDirection.South;
        }

        private static MapDirection Opposite(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.North:
                    return MapDirection.South;
                case MapDirection.East:
                    return MapDirection.West;
                case MapDirection.South:
                    return MapDirection.North;
                case MapDirection.West:
                    return MapDirection.East;
                default:
                    return MapDirection.None;
            }
        }

        private static string CellKey(int x, int y)
        {
            return $"{x},{y}";
        }

        private static bool IsOpposingSockets(MapDirection sockets)
        {
            return sockets == (MapDirection.East | MapDirection.West)
                || sockets == (MapDirection.North | MapDirection.South);
        }

        private static void ValidateProfile(MapProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (profile.CriticalPathMin < 4 || profile.CriticalPathMax < profile.CriticalPathMin)
            {
                throw new InvalidOperationException("Map profile requires a critical path of at least 4 rooms.");
            }
        }

        private static int PositiveHash(int seed, string left, string right)
        {
            unchecked
            {
                var hash = seed;
                hash = (hash * 397) ^ StableHash(left);
                hash = (hash * 397) ^ StableHash(right);
                return hash == int.MinValue ? 0 : Math.Abs(hash);
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < (value?.Length ?? 0); i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private struct CellCoordinate
        {
            public int X;
            public int Y;

            public CellCoordinate(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private sealed class FrontierOption
        {
            public CellCoordinate Cell;
            public int AdjacentActiveCount;
            public int Weight;
        }

        private sealed class NeighborLink
        {
            public int NeighborIndex;
            public MapDirection DirectionFromSelf;
        }

        private sealed class PlannedNode
        {
            public CellCoordinate Cell;
            public MapRoomRole Role;
            public int PathIndex = -1;
            public int BranchDepth;
            public MapDirection RequiredSockets;
            public MapRoomPoolRole? ForcedPoolRole;
            public string ParentCellKey = string.Empty;
            public List<CellCoordinate> NeighborCells = new List<CellCoordinate>();
            public List<NeighborLink> Neighbors = new List<NeighborLink>();
        }

        private sealed class NodePlan
        {
            public List<PlannedNode> Nodes = new List<PlannedNode>();

            public int CriticalPathLength
            {
                get
                {
                    var count = 0;
                    for (var i = 0; i < Nodes.Count; i++)
                    {
                        if (Nodes[i].PathIndex >= 0)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }
        }

        private sealed class SolverTemplate
        {
            public int Index;
            public MapRoomPoolRole PoolRole;
            public MapRoomRole RoomRole;
            public RoomChunkLayoutKind LayoutKind;
            public int Weight;
            public ChunkPreset Chunk;
        }

        private sealed class SolverNodeState
        {
            public PlannedNode Node;
            public List<int> CandidateTemplateIndices = new List<int>();
        }
    }
}

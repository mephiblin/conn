using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
    public static class MapGenerationService
    {
        public static GeneratedMapDraft Generate(MapProfile profile, IReadOnlyList<ChunkPreset> chunks, int seed)
        {
            ValidateProfile(profile);
            var random = new Random(seed);
            var draft = new GeneratedMapDraft { ProfileId = profile.ProfileId, Seed = seed };
            var roomCountMin = ResolveRoomCountMin(profile);
            var roomCountMax = ResolveRoomCountMax(profile, roomCountMin);
            var pathMin = Math.Max(4, Math.Min(profile.CriticalPathMin, roomCountMax));
            var pathMax = Math.Max(pathMin, Math.Min(profile.CriticalPathMax, roomCountMax));
            var pathLength = random.Next(pathMin, pathMax + 1);
            var sideBranchCount = ResolveSideBranchCount(profile, pathLength, roomCountMin, roomCountMax);
            var x = 0;
            var y = profile.Height / profile.RoomHeight / 2;
            var hubAttachIndex = Math.Max(1, Math.Min(pathLength - 5, pathLength / 2));

            for (var i = 0; i < pathLength; i++)
            {
                var role = RoleForPathIndex(i, pathLength);
                AddNode(draft.Graph, $"main_{i}", x, y, role, 0, i);
                x++;
                if (i > 0)
                {
                    AddEdge(draft.Graph, $"main_{i - 1}", $"main_{i}", "main");
                }
            }

            var maxAttachIndex = Math.Max(1, pathLength - 4);
            var northBranchCount = 0;
            var southBranchCount = 0;
            for (var i = 0; i < sideBranchCount; i++)
            {
                var direction = i % 2 == 0 ? 1 : -1;
                var attachIndex = direction > 0
                    ? Math.Max(1, hubAttachIndex - northBranchCount++)
                    : Math.Min(maxAttachIndex, hubAttachIndex + 1 - southBranchCount++);
                var branchY = y + direction;
                var branchEndY = branchY + direction;
                var branchId = $"side_{i}_a";
                var branchEndId = $"side_{i}_b";
                AddNode(draft.Graph, branchId, attachIndex, branchY, MapRoomRole.SideBranch, 1, -1);
                AddNode(draft.Graph, branchEndId, attachIndex, branchEndY, MapRoomRole.SideBranch, 2, -1);
                AddEdge(draft.Graph, $"main_{attachIndex}", branchId, "branch");
                AddEdge(draft.Graph, branchId, branchEndId, "branch");
                draft.Graph.SideBranches.Add(branchEndId);
            }

            var loopBudget = Math.Min(Math.Max(0, random.Next(profile.LoopMin, profile.LoopMax + 1)), Math.Max(0, draft.Graph.SideBranches.Count - 1));
            var loopsAdded = 0;
            for (var i = 0; loopsAdded < loopBudget && i < draft.Graph.SideBranches.Count; i++)
            {
                for (var j = i + 1; loopsAdded < loopBudget && j < draft.Graph.SideBranches.Count; j++)
                {
                    var fromId = draft.Graph.SideBranches[i];
                    var toId = draft.Graph.SideBranches[j];
                    var from = FindNode(draft.Graph, fromId);
                    var to = FindNode(draft.Graph, toId);
                    if (ManhattanDistance(from, to) != 1 || HasEdge(draft.Graph, fromId, toId))
                    {
                        continue;
                    }

                    AddEdge(draft.Graph, fromId, toId, "merge");
                    loopsAdded++;
                }
            }

            AssignSockets(draft.Graph);
            AssignLayoutKinds(draft.Graph, random);
            AssignChunksAndPlacements(draft, profile, chunks);
            return draft;
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

        private static int ResolveSideBranchCount(MapProfile profile, int pathLength, int roomCountMin, int roomCountMax)
        {
            var maxBranchesByRoomCount = Math.Max(0, (roomCountMax - pathLength) / 2);
            var minBranchesByRoomCount = Math.Max(0, (roomCountMin - pathLength + 1) / 2);
            var requested = Math.Max(0, profile.SideBranchCount);
            return Math.Min(maxBranchesByRoomCount, Math.Max(requested, minBranchesByRoomCount));
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

        private static MapRoomRole RoleForPathIndex(int index, int pathLength)
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

            return MapRoomRole.MainPath;
        }

        private static void AddNode(RoomGraph graph, string id, int x, int y, MapRoomRole role, int branchDepth, int pathIndex)
        {
            graph.Nodes.Add(new RoomGraphNode
            {
                Id = id,
                GridX = x,
                GridY = y,
                Role = role,
                BranchDepth = branchDepth,
                PathIndex = pathIndex
            });

            if (pathIndex >= 0)
            {
                graph.CriticalPath.Add(id);
            }
        }

        private static void AddEdge(RoomGraph graph, string from, string to, string kind)
        {
            graph.Edges.Add(new RoomGraphEdge { FromNodeId = from, ToNodeId = to, Kind = kind });
        }

        private static bool HasEdge(RoomGraph graph, string from, string to)
        {
            for (var i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];
                if ((edge.FromNodeId == from && edge.ToNodeId == to) || (edge.FromNodeId == to && edge.ToNodeId == from))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssignSockets(RoomGraph graph)
        {
            for (var i = 0; i < graph.Edges.Count; i++)
            {
                var from = FindNode(graph, graph.Edges[i].FromNodeId);
                var to = FindNode(graph, graph.Edges[i].ToNodeId);
                var fromDirection = DirectionBetween(from, to);
                var toDirection = Opposite(fromDirection);
                from.SocketMask |= fromDirection;
                to.SocketMask |= toDirection;
            }
        }

        private static void AssignChunksAndPlacements(GeneratedMapDraft draft, MapProfile profile, IReadOnlyList<ChunkPreset> chunks)
        {
            var roomPools = ResolveRoomPools(profile, chunks);
            for (var i = 0; i < draft.Graph.Nodes.Count; i++)
            {
                var node = draft.Graph.Nodes[i];
                var chunk = FindChunk(roomPools, chunks, node, profile, draft.Seed);
                node.ChunkId = chunk.Id;

                if (TryRequiredPlacementKind(node.Role, out var placementKind))
                {
                    AddPlacement(draft, profile, node, chunk, placementKind, placementKind.ToString().ToLowerInvariant());
                }

                if (node.Role == MapRoomRole.MainPath && HasAnchor(chunk, MapAnchorKind.Monster))
                {
                    AddPlacement(draft, profile, node, chunk, MapPlacementKind.Monster, $"monster_{node.Id}");
                }

                if (node.Role == MapRoomRole.SideBranch && HasAnchor(chunk, MapAnchorKind.Loot))
                {
                    AddPlacement(draft, profile, node, chunk, MapPlacementKind.Loot, $"loot_{node.Id}");
                }
            }
        }

        private static void AssignLayoutKinds(RoomGraph graph, Random random)
        {
            if (graph == null)
            {
                return;
            }

            var preferredHeightTransitionId = FindPreferredHeightTransitionNodeId(graph, random);
            for (var i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                var degree = CountConnections(graph, node.Id);
                var sockets = node.SocketMask;

                if (node.Role == MapRoomRole.SideBranch)
                {
                    node.LayoutKind = degree == 1
                        ? RoomChunkLayoutKind.DeadEnd
                        : RoomChunkLayoutKind.Room;
                    continue;
                }

                if (node.Role != MapRoomRole.MainPath)
                {
                    node.LayoutKind = RoomChunkLayoutKind.Room;
                    continue;
                }

                if (node.Id == preferredHeightTransitionId)
                {
                    node.LayoutKind = RoomChunkLayoutKind.HeightTransition;
                    continue;
                }

                if (degree >= 3)
                {
                    node.LayoutKind = RoomChunkLayoutKind.Hub;
                    continue;
                }

                if (degree == 2 && IsOpposingSockets(sockets))
                {
                    node.LayoutKind = RoomChunkLayoutKind.Corridor;
                    continue;
                }

                node.LayoutKind = RoomChunkLayoutKind.Room;
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

        private static ChunkPreset FindChunk(IReadOnlyList<ChunkPreset> chunks, MapRoomRole role, RoomChunkLayoutKind layoutKind, MapDirection sockets, string theme, int width, int height)
        {
            for (var i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].LayoutKind == layoutKind
                    && chunks[i].Supports(role, sockets, theme, width, height))
                {
                    return chunks[i];
                }
            }

            for (var i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].Supports(role, sockets, theme, width, height))
                {
                    return chunks[i];
                }
            }

            throw new InvalidOperationException($"No chunk supports role {role} with layout {layoutKind} and sockets {sockets}.");
        }

        private static ChunkPreset FindChunk(
            IReadOnlyList<RuntimeMapRoomPoolRule> roomPools,
            IReadOnlyList<ChunkPreset> chunks,
            RoomGraphNode node,
            MapProfile profile,
            int seed)
        {
            var poolRole = ResolvePoolRole(node);
            var matchingPools = new List<RuntimeMapRoomPoolRule>();
            foreach (var pool in roomPools)
            {
                if (pool != null && pool.Role == poolRole && pool.LayoutKind == node.LayoutKind)
                {
                    matchingPools.Add(pool);
                }
            }

            if (matchingPools.Count == 0)
            {
                throw new InvalidOperationException($"No room pool supports role {poolRole} with layout {node.LayoutKind}.");
            }

            var pool = ChoosePool(matchingPools, seed, node.Id);
            var candidates = new List<ChunkPreset>();
            foreach (var chunkId in pool.AllowedChunkIds ?? new List<string>())
            {
                var chunk = FindChunkById(chunks, chunkId);
                if (chunk != null
                    && chunk.LayoutKind == node.LayoutKind
                    && SupportsPoolRole(chunk, poolRole, node.Role, node.SocketMask, profile.Theme, profile.RoomWidth, profile.RoomHeight))
                {
                    candidates.Add(chunk);
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException($"Room pool {pool.Role}/{pool.LayoutKind} has no compatible chunk for node {node.Id} sockets {node.SocketMask}.");
            }

            return candidates[PositiveHash(seed, node.Id, pool.Role.ToString()) % candidates.Count];
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

        private static RuntimeMapRoomPoolRule ChoosePool(IReadOnlyList<RuntimeMapRoomPoolRule> pools, int seed, string nodeId)
        {
            var totalWeight = 0;
            for (var i = 0; i < pools.Count; i++)
            {
                totalWeight += Math.Max(0, pools[i].Weight);
            }

            if (totalWeight <= 0)
            {
                return pools[0];
            }

            var roll = PositiveHash(seed, nodeId, "room_pool") % totalWeight;
            for (var i = 0; i < pools.Count; i++)
            {
                var weight = Math.Max(0, pools[i].Weight);
                if (roll < weight)
                {
                    return pools[i];
                }

                roll -= weight;
            }

            return pools[0];
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

        private static bool SupportsPoolRole(
            ChunkPreset chunk,
            MapRoomPoolRole poolRole,
            MapRoomRole roomRole,
            MapDirection sockets,
            string theme,
            int width,
            int height)
        {
            return chunk.Width == width
                && chunk.Height == height
                && (string.IsNullOrEmpty(chunk.Theme) || chunk.Theme == theme)
                && (chunk.OpenSides & sockets) == sockets
                && (chunk.DoorSockets & sockets) == sockets
                && ChunkSupportsPoolRole(chunk, poolRole, roomRole);
        }

        private static bool ChunkSupportsPoolRole(ChunkPreset chunk, MapRoomPoolRole poolRole, MapRoomRole roomRole)
        {
            switch (poolRole)
            {
                case MapRoomPoolRole.Start:
                    return chunk.RoleTags.Contains(MapRoomRole.Start);
                case MapRoomPoolRole.Main:
                case MapRoomPoolRole.Corridor:
                case MapRoomPoolRole.Hub:
                case MapRoomPoolRole.HeightTransition:
                    return chunk.RoleTags.Contains(MapRoomRole.MainPath);
                case MapRoomPoolRole.Side:
                case MapRoomPoolRole.DeadEnd:
                    return chunk.RoleTags.Contains(MapRoomRole.SideBranch);
                case MapRoomPoolRole.Quest:
                    return chunk.RoleTags.Contains(MapRoomRole.QuestTarget);
                case MapRoomPoolRole.Boss:
                    return chunk.RoleTags.Contains(MapRoomRole.Boss);
                case MapRoomPoolRole.Exit:
                    return chunk.RoleTags.Contains(MapRoomRole.Exit);
                default:
                    return chunk.RoleTags.Contains(roomRole);
            }
        }

        private static MapRoomPoolRole ResolvePoolRole(RoomGraphNode node)
        {
            switch (node.Role)
            {
                case MapRoomRole.Start:
                    return MapRoomPoolRole.Start;
                case MapRoomRole.QuestTarget:
                    return MapRoomPoolRole.Quest;
                case MapRoomRole.Boss:
                    return MapRoomPoolRole.Boss;
                case MapRoomRole.Exit:
                    return MapRoomPoolRole.Exit;
                case MapRoomRole.SideBranch:
                    return node.LayoutKind == RoomChunkLayoutKind.DeadEnd
                        ? MapRoomPoolRole.DeadEnd
                        : MapRoomPoolRole.Side;
                case MapRoomRole.MainPath:
                    switch (node.LayoutKind)
                    {
                        case RoomChunkLayoutKind.Corridor:
                            return MapRoomPoolRole.Corridor;
                        case RoomChunkLayoutKind.Hub:
                            return MapRoomPoolRole.Hub;
                        case RoomChunkLayoutKind.HeightTransition:
                            return MapRoomPoolRole.HeightTransition;
                        default:
                            return MapRoomPoolRole.Main;
                    }
                default:
                    throw new InvalidOperationException($"Unsupported room role for pool resolution: {node.Role}");
            }
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

        private static MapDirection DirectionBetween(RoomGraphNode from, RoomGraphNode to)
        {
            if (to.GridX > from.GridX)
            {
                return MapDirection.East;
            }

            if (to.GridX < from.GridX)
            {
                return MapDirection.West;
            }

            return to.GridY > from.GridY ? MapDirection.North : MapDirection.South;
        }

        private static MapDirection Opposite(MapDirection direction)
        {
            if (direction == MapDirection.North)
            {
                return MapDirection.South;
            }

            if (direction == MapDirection.East)
            {
                return MapDirection.West;
            }

            return direction == MapDirection.South ? MapDirection.North : MapDirection.East;
        }

        private static string FindPreferredHeightTransitionNodeId(RoomGraph graph, Random random)
        {
            var candidates = new List<RoomGraphNode>();
            var fallbackCandidates = new List<RoomGraphNode>();
            for (var i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                if (node.Role == MapRoomRole.MainPath && node.PathIndex > 1 && node.PathIndex < graph.CriticalPath.Count - 2)
                {
                    fallbackCandidates.Add(node);
                    if (CountConnections(graph, node.Id) < 3)
                    {
                        candidates.Add(node);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                if (fallbackCandidates.Count == 0)
                {
                    return string.Empty;
                }

                return fallbackCandidates[random.Next(0, fallbackCandidates.Count)].Id;
            }

            return candidates[random.Next(0, candidates.Count)].Id;
        }

        private static int CountConnections(RoomGraph graph, string nodeId)
        {
            var count = 0;
            for (var i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];
                if (edge.FromNodeId == nodeId || edge.ToNodeId == nodeId)
                {
                    count++;
                }
            }

            return count;
        }

        private static int ManhattanDistance(RoomGraphNode first, RoomGraphNode second)
        {
            return Math.Abs(first.GridX - second.GridX) + Math.Abs(first.GridY - second.GridY);
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
    }
}

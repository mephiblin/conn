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
            var pathLength = random.Next(profile.CriticalPathMin, profile.CriticalPathMax + 1);
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

            for (var i = 0; i < profile.SideBranchCount; i++)
            {
                var attachIndex = hubAttachIndex;
                var branchY = y + (i % 2 == 0 ? 1 : -1);
                var branchId = $"side_{i}_a";
                var branchEndId = $"side_{i}_b";
                AddNode(draft.Graph, branchId, attachIndex, branchY, MapRoomRole.SideBranch, 1, -1);
                AddNode(draft.Graph, branchEndId, attachIndex + 1, branchY, MapRoomRole.SideBranch, 2, -1);
                AddEdge(draft.Graph, $"main_{attachIndex}", branchId, "branch");
                AddEdge(draft.Graph, branchId, branchEndId, "branch");
                draft.Graph.SideBranches.Add(branchEndId);
            }

            var loopBudget = Math.Min(Math.Max(0, random.Next(profile.LoopMin, profile.LoopMax + 1)), Math.Max(0, draft.Graph.SideBranches.Count - 1));
            for (var i = 0; i < loopBudget && i < draft.Graph.SideBranches.Count - 1; i++)
            {
                var sideId = draft.Graph.SideBranches[i];
                var side = FindNode(draft.Graph, sideId);
                var targetIndex = Math.Max(1, Math.Min(side.GridX, pathLength - 4));
                var target = $"main_{targetIndex}";
                if (!HasEdge(draft.Graph, sideId, target))
                {
                    AddEdge(draft.Graph, sideId, target, "merge");
                }
            }

            AssignSockets(draft.Graph);
            AssignLayoutKinds(draft.Graph, random);
            AssignChunksAndPlacements(draft, profile, chunks);
            return draft;
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
            for (var i = 0; i < draft.Graph.Nodes.Count; i++)
            {
                var node = draft.Graph.Nodes[i];
                var chunk = FindChunk(chunks, node.Role, node.LayoutKind, node.SocketMask, profile.Theme, profile.RoomWidth, profile.RoomHeight);
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
                if (node.Role == MapRoomRole.MainPath && node.PathIndex > 0 && node.PathIndex < graph.CriticalPath.Count - 2)
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
    }
}

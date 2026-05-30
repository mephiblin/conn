using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapDraftBuilder
    {
        public static EditableMapDraftAsset CreateBlankDraftAsset(
            string assetPath,
            string id,
            string sourceProfileId,
            int width,
            int height,
            float cellSize = 1f,
            float heightStep = 1f)
        {
            EnsureDraftFolders();

            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.Id = string.IsNullOrWhiteSpace(id) ? "editable_map_draft" : id.Trim();
            draft.SourceProfileId = sourceProfileId ?? string.Empty;
            draft.InitializeBlank(width, height, cellSize, heightStep);

            AssetDatabase.CreateAsset(draft, assetPath);
            AssetDatabase.SaveAssets();
            return draft;
        }

        public static EditableMapDraftAsset CreateDraftAssetFromGenerated(
            string assetPath,
            GeneratedMapDraft generatedDraft,
            MapProfile profile,
            IReadOnlyList<ChunkPreset> runtimeChunks,
            int floor,
            int difficulty,
            float cellSize = 1f,
            float heightStep = 1f)
        {
            EnsureDraftFolders();

            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            PopulateFromGeneratedDraft(draft, generatedDraft, profile, runtimeChunks, floor, difficulty, cellSize, heightStep);
            AssetDatabase.CreateAsset(draft, assetPath);
            AssetDatabase.SaveAssets();
            return draft;
        }

        public static EditableMapDraftAsset CreateDraftAssetFromSource(
            string assetPath,
            EditableMapDraftAsset sourceDraft)
        {
            EnsureDraftFolders();

            if (sourceDraft == null)
            {
                throw new ArgumentNullException(nameof(sourceDraft));
            }

            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            PopulateFromDraft(draft, sourceDraft);
            if (GeneratedMapPaletteLibrary.UsesGeneratedPalettes(sourceDraft))
            {
                GeneratedMapPaletteLibrary.AssignGeneratedPalettes(draft, persistAssets: true);
            }

            AssetDatabase.CreateAsset(draft, assetPath);
            AssetDatabase.SaveAssets();
            return draft;
        }

        public static EditableMapDraftAsset BuildGeneratedDraft(
            MapProfile profile,
            IReadOnlyList<ChunkPreset> runtimeChunks,
            int seed,
            int floor,
            int difficulty,
            float cellSize = 1f,
            float heightStep = 1f)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (runtimeChunks == null)
            {
                throw new ArgumentNullException(nameof(runtimeChunks));
            }

            var generated = MapGenerationService.Generate(profile, runtimeChunks, seed);
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            PopulateFromGeneratedDraft(draft, generated, profile, runtimeChunks, floor, difficulty, cellSize, heightStep);
            return draft;
        }

        public static void PopulateFromGeneratedDraft(
            EditableMapDraftAsset target,
            GeneratedMapDraft generatedDraft,
            MapProfile profile,
            IReadOnlyList<ChunkPreset> runtimeChunks,
            int floor,
            int difficulty,
            float cellSize = 1f,
            float heightStep = 1f)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (generatedDraft == null)
            {
                throw new ArgumentNullException(nameof(generatedDraft));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var chunkLookup = BuildChunkLookup(runtimeChunks);
            var roomBounds = CalculateRoomBounds(generatedDraft.Graph);
            var slotSize = ResolveSlotSize(profile, generatedDraft.Graph, chunkLookup);
            var mapWidth = roomBounds.width * slotSize.x;
            var mapHeight = roomBounds.height * slotSize.y;

            target.Id = $"{generatedDraft.ProfileId}_{generatedDraft.Seed}_draft";
            target.SourceProfileId = generatedDraft.ProfileId ?? string.Empty;
            target.Seed = generatedDraft.Seed;
            target.GenerationRetryCount = Mathf.Max(0, generatedDraft.RetryCount);
            target.GenerationFailureReason = generatedDraft.FailureReason ?? string.Empty;
            target.Floor = Mathf.Max(1, floor);
            target.Difficulty = Mathf.Max(0, difficulty);
            target.Version = 1;
            target.InitializeBlank(mapWidth, mapHeight, cellSize, heightStep);

            var zoneId = BuildZoneId(profile, floor, difficulty);
            var rooms = new List<EditableMapRoom>();
            var objects = new List<EditableMapObjectPlacement>();
            var sockets = new List<EditableMapSocket>();
            var edges = BuildEdgeLookup(generatedDraft.Graph);
            var nodes = BuildNodeLookup(generatedDraft.Graph);

            foreach (var node in generatedDraft.Graph.Nodes)
            {
                var roomOriginX = (node.GridX - roomBounds.xMin) * slotSize.x;
                var roomOriginY = (node.GridY - roomBounds.yMin) * slotSize.y;
                var layoutKind = node.LayoutKind;
                var roomWidth = slotSize.x;
                var roomHeight = slotSize.y;
                if (chunkLookup.TryGetValue(node.ChunkId ?? string.Empty, out var chunk))
                {
                    layoutKind = chunk.LayoutKind;
                    roomWidth = chunk.Width;
                    roomHeight = chunk.Height;
                    StampChunk(target, chunk, roomOriginX, roomOriginY, node.Id, zoneId, objects);
                }
                else
                {
                    FillRoomFallback(target, roomOriginX, roomOriginY, roomWidth, roomHeight, node.Id, zoneId);
                }

                rooms.Add(new EditableMapRoom
                {
                    Id = node.Id ?? string.Empty,
                    Role = node.Role,
                    LayoutKind = layoutKind,
                    X = roomOriginX,
                    Y = roomOriginY,
                    Width = roomWidth,
                    Height = roomHeight,
                    SocketMask = node.SocketMask,
                    HeightLevel = 0,
                    ZoneId = zoneId,
                    ChunkId = node.ChunkId ?? string.Empty
                });

                CreateSocketsForNode(node, chunkLookup, roomOriginX, roomOriginY, roomWidth, roomHeight, edges, nodes, sockets);
            }

            CarveSocketConnections(target, sockets);
            target.Rooms = rooms.ToArray();
            target.Objects = objects.ToArray();
            target.Sockets = sockets.ToArray();
            target.Zones = new[]
            {
                new EditableMapZone
                {
                    Id = zoneId,
                    ThemeId = profile.Theme ?? string.Empty,
                    IntendedDifficulty = Mathf.Max(0, difficulty),
                    Purpose = "generated_main"
                }
            };
        }

        public static void PopulateFromDraft(EditableMapDraftAsset target, EditableMapDraftAsset sourceDraft)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (sourceDraft == null)
            {
                throw new ArgumentNullException(nameof(sourceDraft));
            }

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(sourceDraft), target);
        }

        public static string BuildDefaultAssetPath(string baseName)
        {
            EnsureDraftFolders();
            var fileName = string.IsNullOrWhiteSpace(baseName) ? "EditableMapDraft.asset" : $"{SanitizeFileName(baseName)}.asset";
            return AssetDatabase.GenerateUniqueAssetPath($"{EditableMapDraftAsset.DefaultDraftFolder}/{fileName}");
        }

        private static void EnsureDraftFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Conn/Authoring/Maps/Drafts"))
            {
                AssetDatabase.CreateFolder("Assets/Conn/Authoring/Maps", "Drafts");
            }
        }

        private static Dictionary<string, RoomChunkSource> BuildChunkLookup(IReadOnlyList<ChunkPreset> runtimeChunks)
        {
            var lookup = new Dictionary<string, RoomChunkSource>(StringComparer.Ordinal);
            foreach (var runtimeChunk in runtimeChunks ?? Array.Empty<ChunkPreset>())
            {
                if (runtimeChunk == null || string.IsNullOrWhiteSpace(runtimeChunk.Id) || lookup.ContainsKey(runtimeChunk.Id))
                {
                    continue;
                }

                lookup.Add(runtimeChunk.Id, RoomChunkSource.FromRuntime(runtimeChunk));
            }

            var guids = new List<string>();
            guids.AddRange(AssetDatabase.FindAssets("t:RoomChunkAsset"));
            guids.AddRange(AssetDatabase.FindAssets("t:LandmarkRoomAsset"));

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var chunk = AssetDatabase.LoadAssetAtPath<RoomChunkAsset>(path);
                if (chunk == null || string.IsNullOrWhiteSpace(chunk.Id) || lookup.ContainsKey(chunk.Id))
                {
                    continue;
                }

                lookup.Add(chunk.Id, RoomChunkSource.FromAuthoring(chunk));
            }

            return lookup;
        }

        private static RectInt CalculateRoomBounds(RoomGraph graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                return new RectInt(0, 0, 1, 1);
            }

            var minX = graph.Nodes[0].GridX;
            var maxX = graph.Nodes[0].GridX;
            var minY = graph.Nodes[0].GridY;
            var maxY = graph.Nodes[0].GridY;
            foreach (var node in graph.Nodes)
            {
                minX = Mathf.Min(minX, node.GridX);
                maxX = Mathf.Max(maxX, node.GridX);
                minY = Mathf.Min(minY, node.GridY);
                maxY = Mathf.Max(maxY, node.GridY);
            }

            return new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        }

        private static Dictionary<string, List<RoomGraphEdge>> BuildEdgeLookup(RoomGraph graph)
        {
            var lookup = new Dictionary<string, List<RoomGraphEdge>>(StringComparer.Ordinal);
            if (graph == null)
            {
                return lookup;
            }

            foreach (var edge in graph.Edges)
            {
                AddEdgeLookup(lookup, edge.FromNodeId, edge);
                AddEdgeLookup(lookup, edge.ToNodeId, edge);
            }

            return lookup;
        }

        private static Dictionary<string, RoomGraphNode> BuildNodeLookup(RoomGraph graph)
        {
            var lookup = new Dictionary<string, RoomGraphNode>(StringComparer.Ordinal);
            if (graph == null)
            {
                return lookup;
            }

            foreach (var node in graph.Nodes)
            {
                if (!string.IsNullOrWhiteSpace(node.Id))
                {
                    lookup[node.Id] = node;
                }
            }

            return lookup;
        }

        private static void AddEdgeLookup(Dictionary<string, List<RoomGraphEdge>> lookup, string key, RoomGraphEdge edge)
        {
            if (!lookup.TryGetValue(key, out var edges))
            {
                edges = new List<RoomGraphEdge>();
                lookup.Add(key, edges);
            }

            edges.Add(edge);
        }

        private static void FillRoomFallback(
            EditableMapDraftAsset target,
            int originX,
            int originY,
            int roomWidth,
            int roomHeight,
            string roomId,
            string zoneId)
        {
            for (var y = 0; y < roomHeight; y++)
            {
                for (var x = 0; x < roomWidth; x++)
                {
                    var cell = EditableMapCell.CreateDefault(originX + x, originY + y);
                    cell.RoomId = roomId ?? string.Empty;
                    cell.ZoneId = zoneId ?? string.Empty;
                    cell.Terrain = RoomChunkCellType.Floor;
                    target.TrySetCell(cell);
                }
            }
        }

        private static void StampChunk(
            EditableMapDraftAsset target,
            RoomChunkSource chunk,
            int originX,
            int originY,
            string roomId,
            string zoneId,
            List<EditableMapObjectPlacement> objects)
        {
            foreach (var sourceCell in chunk.Cells)
            {
                EnsureStampTarget(target, originX + sourceCell.X, originY + sourceCell.Y, roomId);
                var cell = EditableMapCell.CreateDefault(originX + sourceCell.X, originY + sourceCell.Y);
                cell.RoomId = roomId ?? string.Empty;
                cell.ZoneId = zoneId ?? string.Empty;
                cell.Terrain = sourceCell.Type;
                cell.Height = sourceCell.Height;
                cell.Direction = sourceCell.Direction;
                cell.MaterialId = sourceCell.MaterialId ?? string.Empty;
                target.TrySetCell(cell);
            }

            foreach (var sourceObject in chunk.Objects)
            {
                objects.Add(new EditableMapObjectPlacement
                {
                    Id = BuildPlacedObjectId(roomId, sourceObject.Id),
                    PaletteObjectId = sourceObject.PrefabId ?? string.Empty,
                    Kind = sourceObject.Kind,
                    X = originX + sourceObject.X,
                    Y = originY + sourceObject.Y,
                    Height = sourceObject.Height,
                    Width = Mathf.Max(1, sourceObject.Width),
                    Depth = Mathf.Max(1, sourceObject.Depth),
                    Direction = sourceObject.Direction,
                    BlocksMovement = sourceObject.BlocksMovement,
                    RuntimeReferenceId = sourceObject.PrefabId ?? string.Empty,
                    MaterialId = sourceObject.MaterialId ?? string.Empty
                });
            }
        }

        private static void EnsureStampTarget(EditableMapDraftAsset target, int x, int y, string roomId)
        {
            if (!target.TryGetCell(x, y, out var existing))
            {
                throw new InvalidOperationException($"Chunk stamp exceeds draft bounds at ({x}, {y}) for room {roomId}.");
            }

            if (!string.IsNullOrWhiteSpace(existing.RoomId)
                && !string.Equals(existing.RoomId, roomId, StringComparison.Ordinal)
                && existing.Terrain != RoomChunkCellType.Gap)
            {
                throw new InvalidOperationException(
                    $"Chunk stamp overlap at ({x}, {y}) between room {existing.RoomId} and room {roomId}.");
            }
        }

        private static Vector2Int ResolveSlotSize(
            MapProfile profile,
            RoomGraph graph,
            Dictionary<string, RoomChunkSource> chunkLookup)
        {
            var width = Mathf.Max(1, profile.RoomWidth);
            var height = Mathf.Max(1, profile.RoomHeight);
            if (graph == null)
            {
                return new Vector2Int(width, height);
            }

            foreach (var node in graph.Nodes)
            {
                if (chunkLookup.TryGetValue(node.ChunkId ?? string.Empty, out var chunk))
                {
                    width = Mathf.Max(width, chunk.Width);
                    height = Mathf.Max(height, chunk.Height);
                }
            }

            return new Vector2Int(width, height);
        }

        private static string BuildZoneId(MapProfile profile, int floor, int difficulty)
        {
            return $"{profile.ProfileId}_zone_f{Mathf.Max(1, floor)}_d{Mathf.Max(0, difficulty)}";
        }

        private static void CreateSocketsForNode(
            RoomGraphNode node,
            Dictionary<string, RoomChunkSource> chunkLookup,
            int roomOriginX,
            int roomOriginY,
            int roomWidth,
            int roomHeight,
            Dictionary<string, List<RoomGraphEdge>> edgeLookup,
            Dictionary<string, RoomGraphNode> nodeLookup,
            List<EditableMapSocket> sockets)
        {
            if (!edgeLookup.TryGetValue(node.Id, out var edges))
            {
                return;
            }

            foreach (var edge in edges)
            {
                var isFrom = string.Equals(edge.FromNodeId, node.Id, StringComparison.Ordinal);
                var otherId = isFrom ? edge.ToNodeId : edge.FromNodeId;
                var direction = ResolveSocketDirection(node, otherId, nodeLookup);
                var socketPosition = FindSocketPosition(
                    chunkLookup.TryGetValue(node.ChunkId ?? string.Empty, out var chunk) ? chunk : default,
                    roomOriginX,
                    roomOriginY,
                    roomWidth,
                    roomHeight,
                    direction);
                sockets.Add(new EditableMapSocket
                {
                    Id = $"{node.Id}_{direction}_{otherId}",
                    RoomId = node.Id ?? string.Empty,
                    X = socketPosition.x,
                    Y = socketPosition.y,
                    Direction = direction,
                    Width = 1,
                    TargetRoomId = otherId ?? string.Empty,
                    LockedDoorKeyId = edge.Locked ? "locked" : string.Empty
                });
            }
        }

        private static MapDirection ResolveSocketDirection(RoomGraphNode node, string otherId, Dictionary<string, RoomGraphNode> nodeLookup)
        {
            if (nodeLookup.TryGetValue(otherId ?? string.Empty, out var other))
            {
                if (other.GridX > node.GridX)
                {
                    return MapDirection.East;
                }

                if (other.GridX < node.GridX)
                {
                    return MapDirection.West;
                }

                return other.GridY > node.GridY ? MapDirection.North : MapDirection.South;
            }

            return MapDirection.North;
        }

        private static Vector2Int FindSocketPosition(
            RoomChunkSource chunk,
            int originX,
            int originY,
            int roomWidth,
            int roomHeight,
            MapDirection direction)
        {
            var boundaryCells = new List<Vector2Int>();
            foreach (var cell in chunk.Cells ?? Array.Empty<RoomChunkCell>())
            {
                if (!IsBoundaryCell(cell, roomWidth, roomHeight, direction))
                {
                    continue;
                }

                if (cell.Type == RoomChunkCellType.Wall || cell.Type == RoomChunkCellType.Gap)
                {
                    continue;
                }

                boundaryCells.Add(new Vector2Int(originX + cell.X, originY + cell.Y));
            }

            if (boundaryCells.Count > 0)
            {
                return boundaryCells[boundaryCells.Count / 2];
            }

            var centerX = originX + Mathf.Max(0, roomWidth / 2);
            var centerY = originY + Mathf.Max(0, roomHeight / 2);

            switch (direction)
            {
                case MapDirection.East:
                    return new Vector2Int(originX + Mathf.Max(0, roomWidth - 1), centerY);
                case MapDirection.South:
                    return new Vector2Int(centerX, originY);
                case MapDirection.West:
                    return new Vector2Int(originX, centerY);
                default:
                    return new Vector2Int(centerX, originY + Mathf.Max(0, roomHeight - 1));
            }
        }

        private static bool IsBoundaryCell(RoomChunkCell cell, int roomWidth, int roomHeight, MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return cell.X == Mathf.Max(0, roomWidth - 1);
                case MapDirection.South:
                    return cell.Y == 0;
                case MapDirection.West:
                    return cell.X == 0;
                default:
                    return cell.Y == Mathf.Max(0, roomHeight - 1);
            }
        }

        private static string BuildPlacedObjectId(string roomId, string sourceId)
        {
            var localId = string.IsNullOrWhiteSpace(sourceId) ? "object" : sourceId.Trim();
            return string.IsNullOrWhiteSpace(roomId) ? localId : $"{roomId}_{localId}";
        }

        private static void CarveSocketConnections(EditableMapDraftAsset target, List<EditableMapSocket> sockets)
        {
            if (target == null || sockets == null || sockets.Count == 0)
            {
                return;
            }

            CarveSocketInteriorConnections(target, sockets);

            var lookup = new Dictionary<string, EditableMapSocket>(StringComparer.Ordinal);
            for (var i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i];
                if (string.IsNullOrWhiteSpace(socket.RoomId) || string.IsNullOrWhiteSpace(socket.TargetRoomId))
                {
                    continue;
                }

                lookup[$"{socket.RoomId}->{socket.TargetRoomId}"] = socket;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i];
                if (string.IsNullOrWhiteSpace(socket.RoomId) || string.IsNullOrWhiteSpace(socket.TargetRoomId))
                {
                    continue;
                }

                var doorKey = BuildRoomPairKey(socket.RoomId, socket.TargetRoomId);
                if (!seen.Add(doorKey))
                {
                    continue;
                }

                if (!lookup.TryGetValue($"{socket.TargetRoomId}->{socket.RoomId}", out var other))
                {
                    continue;
                }

                CarveConnection(target, socket, other);
            }
        }

        private static void CarveSocketInteriorConnections(EditableMapDraftAsset target, List<EditableMapSocket> sockets)
        {
            foreach (var socket in sockets)
            {
                var inward = Opposite(socket.Direction);
                var current = new Vector2Int(socket.X, socket.Y);
                var points = new List<Vector2Int> { current };
                while (target.IsInBounds(current.x, current.y))
                {
                    if (target.TryGetCell(current.x, current.y, out var currentCell)
                        && currentCell.Terrain != RoomChunkCellType.Wall
                        && currentCell.Terrain != RoomChunkCellType.Gap
                        && points.Count > 1)
                    {
                        CarveSocketInteriorPath(target, points, currentCell);
                        break;
                    }

                    current += DirectionOffset(inward);
                    if (!target.IsInBounds(current.x, current.y))
                    {
                        break;
                    }

                    if (target.TryGetCell(current.x, current.y, out var nextCell)
                        && !string.Equals(nextCell.RoomId, socket.RoomId, StringComparison.Ordinal))
                    {
                        break;
                    }

                    points.Add(current);
                }
            }
        }

        private static void CarveSocketInteriorPath(EditableMapDraftAsset target, List<Vector2Int> points, EditableMapCell targetCell)
        {
            foreach (var point in points)
            {
                if (!target.TryGetCell(point.x, point.y, out var cell))
                {
                    continue;
                }

                cell.Terrain = RoomChunkCellType.Floor;
                cell.Height = targetCell.Height;
                cell.Direction = targetCell.Direction;
                if (string.IsNullOrWhiteSpace(cell.MaterialId))
                {
                    cell.MaterialId = targetCell.MaterialId ?? string.Empty;
                }

                target.TrySetCell(cell);
            }
        }

        private static void CarveConnection(EditableMapDraftAsset target, EditableMapSocket from, EditableMapSocket to)
        {
            var points = BuildAxisAlignedPath(new Vector2Int(from.X, from.Y), new Vector2Int(to.X, to.Y));
            if (points.Count == 0)
            {
                return;
            }

            if (!target.TryGetCell(from.X, from.Y, out var fromCell) || !target.TryGetCell(to.X, to.Y, out var toCell))
            {
                return;
            }

            var materialId = !string.IsNullOrWhiteSpace(fromCell.MaterialId) ? fromCell.MaterialId : toCell.MaterialId;
            var previousHeight = fromCell.Height;
            for (var i = 0; i < points.Count; i++)
            {
                if (!target.TryGetCell(points[i].x, points[i].y, out var cell))
                {
                    continue;
                }

                var nextHeight = points.Count == 1
                    ? fromCell.Height
                    : Mathf.RoundToInt(Mathf.Lerp(fromCell.Height, toCell.Height, i / (float)(points.Count - 1)));
                if (i > 0)
                {
                    nextHeight = Mathf.Clamp(nextHeight, previousHeight - 1, previousHeight + 1);
                }

                cell.Terrain = RoomChunkCellType.Floor;
                cell.Height = nextHeight;
                cell.Direction = ResolveStepDirection(points, i);
                if (string.IsNullOrWhiteSpace(cell.MaterialId))
                {
                    cell.MaterialId = materialId ?? string.Empty;
                }

                target.TrySetCell(cell);
                previousHeight = nextHeight;
            }
        }

        private static List<Vector2Int> BuildAxisAlignedPath(Vector2Int from, Vector2Int to)
        {
            var points = new List<Vector2Int>();
            var current = from;
            points.Add(current);

            while (current.x != to.x)
            {
                current.x += current.x < to.x ? 1 : -1;
                points.Add(current);
            }

            while (current.y != to.y)
            {
                current.y += current.y < to.y ? 1 : -1;
                points.Add(current);
            }

            return points;
        }

        private static MapDirection ResolveStepDirection(List<Vector2Int> points, int index)
        {
            if (points == null || points.Count <= 1)
            {
                return MapDirection.North;
            }

            var current = points[index];
            var other = index < points.Count - 1 ? points[index + 1] : points[index - 1];
            var delta = other - current;
            if (delta.x > 0)
            {
                return MapDirection.East;
            }

            if (delta.x < 0)
            {
                return MapDirection.West;
            }

            return delta.y > 0 ? MapDirection.North : MapDirection.South;
        }

        private static Vector2Int DirectionOffset(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return new Vector2Int(1, 0);
                case MapDirection.South:
                    return new Vector2Int(0, -1);
                case MapDirection.West:
                    return new Vector2Int(-1, 0);
                default:
                    return new Vector2Int(0, 1);
            }
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
                default:
                    return MapDirection.East;
            }
        }

        private static string BuildRoomPairKey(string firstRoomId, string secondRoomId)
        {
            if (string.CompareOrdinal(firstRoomId, secondRoomId) <= 0)
            {
                return $"{firstRoomId}|{secondRoomId}";
            }

            return $"{secondRoomId}|{firstRoomId}";
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var sanitized = value;
            foreach (var invalid in invalidChars)
            {
                sanitized = sanitized.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "EditableMapDraft" : sanitized;
        }

        private readonly struct RoomChunkSource
        {
            public readonly RoomChunkLayoutKind LayoutKind;
            public readonly int Width;
            public readonly int Height;
            public readonly IReadOnlyList<RoomChunkCell> Cells;
            public readonly IReadOnlyList<RoomChunkObjectPlacement> Objects;

            private RoomChunkSource(
                RoomChunkLayoutKind layoutKind,
                int width,
                int height,
                IReadOnlyList<RoomChunkCell> cells,
                IReadOnlyList<RoomChunkObjectPlacement> objects)
            {
                LayoutKind = layoutKind;
                Width = Mathf.Max(1, width);
                Height = Mathf.Max(1, height);
                Cells = cells ?? Array.Empty<RoomChunkCell>();
                Objects = objects ?? Array.Empty<RoomChunkObjectPlacement>();
            }

            public static RoomChunkSource FromRuntime(ChunkPreset chunk)
            {
                return new RoomChunkSource(chunk.LayoutKind, chunk.Width, chunk.Height, chunk.Cells, chunk.Objects);
            }

            public static RoomChunkSource FromAuthoring(RoomChunkAsset chunk)
            {
                return new RoomChunkSource(chunk.LayoutKind, chunk.Size.x, chunk.Size.y, chunk.Cells, chunk.Objects);
            }
        }
    }
}

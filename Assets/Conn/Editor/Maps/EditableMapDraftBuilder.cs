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
            var mapWidth = roomBounds.width * Mathf.Max(1, profile.RoomWidth);
            var mapHeight = roomBounds.height * Mathf.Max(1, profile.RoomHeight);

            target.Id = $"{generatedDraft.ProfileId}_{generatedDraft.Seed}_draft";
            target.SourceProfileId = generatedDraft.ProfileId ?? string.Empty;
            target.Seed = generatedDraft.Seed;
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
                var roomOriginX = (node.GridX - roomBounds.xMin) * profile.RoomWidth;
                var roomOriginY = (node.GridY - roomBounds.yMin) * profile.RoomHeight;
                var layoutKind = node.LayoutKind;
                if (chunkLookup.TryGetValue(node.ChunkId ?? string.Empty, out var chunk))
                {
                    layoutKind = chunk.LayoutKind;
                    StampChunk(target, chunk, roomOriginX, roomOriginY, node.Id, zoneId, objects);
                }
                else
                {
                    FillRoomFallback(target, roomOriginX, roomOriginY, profile.RoomWidth, profile.RoomHeight, node.Id, zoneId);
                }

                rooms.Add(new EditableMapRoom
                {
                    Id = node.Id ?? string.Empty,
                    Role = node.Role,
                    LayoutKind = layoutKind,
                    X = roomOriginX,
                    Y = roomOriginY,
                    Width = profile.RoomWidth,
                    Height = profile.RoomHeight,
                    SocketMask = node.SocketMask,
                    HeightLevel = 0,
                    ZoneId = zoneId,
                    ChunkId = node.ChunkId ?? string.Empty
                });

                CreateSocketsForNode(target, node, roomOriginX, roomOriginY, profile, edges, nodes, sockets);
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

        private static string BuildZoneId(MapProfile profile, int floor, int difficulty)
        {
            return $"{profile.ProfileId}_zone_f{Mathf.Max(1, floor)}_d{Mathf.Max(0, difficulty)}";
        }

        private static void CreateSocketsForNode(
            EditableMapDraftAsset target,
            RoomGraphNode node,
            int roomOriginX,
            int roomOriginY,
            MapProfile profile,
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
                var socketPosition = FindSocketPosition(target, roomOriginX, roomOriginY, profile.RoomWidth, profile.RoomHeight, direction);
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
            EditableMapDraftAsset target,
            int originX,
            int originY,
            int roomWidth,
            int roomHeight,
            MapDirection direction)
        {
            var centerX = originX + Mathf.Max(0, roomWidth / 2);
            var centerY = originY + Mathf.Max(0, roomHeight / 2);
            if (TryFindSocketPositionOnSide(target, originX, originY, roomWidth, roomHeight, direction, out var found))
            {
                return found;
            }

            switch (direction)
            {
                case MapDirection.East:
                    return new Vector2Int(originX + Mathf.Max(0, roomWidth - 2), centerY);
                case MapDirection.South:
                    return new Vector2Int(centerX, originY + Mathf.Min(1, Mathf.Max(0, roomHeight - 1)));
                case MapDirection.West:
                    return new Vector2Int(originX + Mathf.Min(1, Mathf.Max(0, roomWidth - 1)), centerY);
                default:
                    return new Vector2Int(centerX, originY + Mathf.Max(0, roomHeight - 2));
            }
        }

        private static bool TryFindSocketPositionOnSide(
            EditableMapDraftAsset target,
            int originX,
            int originY,
            int roomWidth,
            int roomHeight,
            MapDirection direction,
            out Vector2Int position)
        {
            if (target == null)
            {
                position = default;
                return false;
            }

            if (direction == MapDirection.East || direction == MapDirection.West)
            {
                var step = direction == MapDirection.East ? -1 : 1;
                var startX = direction == MapDirection.East ? originX + roomWidth - 1 : originX;
                for (var yOffset = 0; yOffset < roomHeight; yOffset++)
                {
                    var y = originY + OrderedOffset(yOffset, roomHeight / 2);
                    for (var x = startX; x >= originX && x < originX + roomWidth; x += step)
                    {
                        if (IsWalkableSocketCell(target, x, y))
                        {
                            position = new Vector2Int(x, y);
                            return true;
                        }
                    }
                }
            }
            else
            {
                var step = direction == MapDirection.North ? -1 : 1;
                var startY = direction == MapDirection.North ? originY + roomHeight - 1 : originY;
                for (var xOffset = 0; xOffset < roomWidth; xOffset++)
                {
                    var x = originX + OrderedOffset(xOffset, roomWidth / 2);
                    for (var y = startY; y >= originY && y < originY + roomHeight; y += step)
                    {
                        if (IsWalkableSocketCell(target, x, y))
                        {
                            position = new Vector2Int(x, y);
                            return true;
                        }
                    }
                }
            }

            position = default;
            return false;
        }

        private static int OrderedOffset(int index, int center)
        {
            if (index == 0)
            {
                return center;
            }

            var delta = (index + 1) / 2;
            return index % 2 == 1 ? center - delta : center + delta;
        }

        private static bool IsWalkableSocketCell(EditableMapDraftAsset target, int x, int y)
        {
            if (!target.TryGetCell(x, y, out var cell))
            {
                return false;
            }

            return cell.Terrain != RoomChunkCellType.Wall && cell.Terrain != RoomChunkCellType.Gap;
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
            public readonly IReadOnlyList<RoomChunkCell> Cells;
            public readonly IReadOnlyList<RoomChunkObjectPlacement> Objects;

            private RoomChunkSource(RoomChunkLayoutKind layoutKind, IReadOnlyList<RoomChunkCell> cells, IReadOnlyList<RoomChunkObjectPlacement> objects)
            {
                LayoutKind = layoutKind;
                Cells = cells ?? Array.Empty<RoomChunkCell>();
                Objects = objects ?? Array.Empty<RoomChunkObjectPlacement>();
            }

            public static RoomChunkSource FromRuntime(ChunkPreset chunk)
            {
                return new RoomChunkSource(chunk.LayoutKind, chunk.Cells, chunk.Objects);
            }

            public static RoomChunkSource FromAuthoring(RoomChunkAsset chunk)
            {
                return new RoomChunkSource(chunk.LayoutKind, chunk.Cells, chunk.Objects);
            }
        }
    }
}

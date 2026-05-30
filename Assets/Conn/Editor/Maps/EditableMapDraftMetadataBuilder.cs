using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapDraftMetadataBuilder
    {
        private static readonly Vector2Int[] CardinalOffsets =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public static void BuildPlayableMetadataFromDrawing(EditableMapDraftAsset draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            var component = FindLargestWalkableComponent(draft);
            if (component.Count < 4)
            {
                throw new InvalidOperationException("Playable metadata requires at least four connected walkable cells.");
            }

            var path = FindLongestPath(draft, component);
            if (path.Count < 4)
            {
                throw new InvalidOperationException("Playable metadata requires a connected path with at least four cells.");
            }

            var anchors = PickRouteAnchors(path);
            var zoneId = string.IsNullOrWhiteSpace(draft.SourceProfileId)
                ? "drawn_map_zone"
                : $"{draft.SourceProfileId}_drawn_zone";
            var areaRoomId = "drawn_area";
            var bounds = CalculateBounds(component);

            ApplyCellOwnership(draft, component, areaRoomId, zoneId);

            draft.Zones = new[]
            {
                new EditableMapZone
                {
                    Id = zoneId,
                    ThemeId = string.IsNullOrWhiteSpace(draft.SourceProfileId) ? "drawn" : draft.SourceProfileId,
                    IntendedDifficulty = Mathf.Max(0, draft.Difficulty),
                    Purpose = "drawn_playable"
                }
            };

            draft.Rooms = new[]
            {
                BuildAnchorRoom("drawn_start", MapRoomRole.Start, anchors[0], zoneId),
                BuildAnchorRoom("drawn_quest", MapRoomRole.QuestTarget, anchors[1], zoneId),
                BuildAnchorRoom("drawn_boss", MapRoomRole.Boss, anchors[2], zoneId),
                BuildAnchorRoom("drawn_exit", MapRoomRole.Exit, anchors[3], zoneId),
                new EditableMapRoom
                {
                    Id = areaRoomId,
                    Role = MapRoomRole.MainPath,
                    LayoutKind = RoomChunkLayoutKind.Hub,
                    X = bounds.xMin,
                    Y = bounds.yMin,
                    Width = bounds.width,
                    Height = bounds.height,
                    SocketMask = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West,
                    HeightLevel = 0,
                    ZoneId = zoneId,
                    ChunkId = "drawn"
                }
            };

            draft.Sockets = BuildRouteSockets(anchors, areaRoomId).ToArray();
        }

        private static List<Vector2Int> FindLargestWalkableComponent(EditableMapDraftAsset draft)
        {
            var visited = new bool[draft.Width, draft.Height];
            var largest = new List<Vector2Int>();
            for (var y = 0; y < draft.Height; y++)
            {
                for (var x = 0; x < draft.Width; x++)
                {
                    if (visited[x, y] || !IsWalkable(draft, x, y))
                    {
                        continue;
                    }

                    var component = FloodFill(draft, new Vector2Int(x, y), visited);
                    if (component.Count > largest.Count)
                    {
                        largest = component;
                    }
                }
            }

            return largest;
        }

        private static List<Vector2Int> FloodFill(EditableMapDraftAsset draft, Vector2Int start, bool[,] visited)
        {
            var cells = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cells.Add(current);
                foreach (var offset in CardinalOffsets)
                {
                    var next = current + offset;
                    if (!draft.IsInBounds(next.x, next.y) || visited[next.x, next.y] || !IsWalkable(draft, next.x, next.y))
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return cells;
        }

        private static List<Vector2Int> FindLongestPath(EditableMapDraftAsset draft, List<Vector2Int> component)
        {
            var first = component[0];
            var farthest = FindFarthestCell(draft, first, out _);
            var end = FindFarthestCell(draft, farthest, out var parents);
            return ReconstructPath(farthest, end, parents);
        }

        private static Vector2Int FindFarthestCell(EditableMapDraftAsset draft, Vector2Int start, out Dictionary<Vector2Int, Vector2Int> parents)
        {
            parents = new Dictionary<Vector2Int, Vector2Int>();
            var distances = new Dictionary<Vector2Int, int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            distances[start] = 0;
            parents[start] = start;
            var farthest = start;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (distances[current] > distances[farthest])
                {
                    farthest = current;
                }

                foreach (var offset in CardinalOffsets)
                {
                    var next = current + offset;
                    if (!draft.IsInBounds(next.x, next.y) || !IsWalkable(draft, next.x, next.y) || distances.ContainsKey(next))
                    {
                        continue;
                    }

                    distances[next] = distances[current] + 1;
                    parents[next] = current;
                    queue.Enqueue(next);
                }
            }

            return farthest;
        }

        private static List<Vector2Int> ReconstructPath(
            Vector2Int start,
            Vector2Int end,
            Dictionary<Vector2Int, Vector2Int> parents)
        {
            var path = new List<Vector2Int>();
            var current = end;
            path.Add(current);
            while (current != start && parents.TryGetValue(current, out var parent))
            {
                current = parent;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static Vector2Int[] PickRouteAnchors(List<Vector2Int> path)
        {
            return new[]
            {
                path[0],
                path[Mathf.Clamp(Mathf.RoundToInt((path.Count - 1) / 3f), 1, path.Count - 1)],
                path[Mathf.Clamp(Mathf.RoundToInt((path.Count - 1) * 2f / 3f), 1, path.Count - 1)],
                path[path.Count - 1]
            };
        }

        private static RectInt CalculateBounds(List<Vector2Int> cells)
        {
            var minX = cells[0].x;
            var maxX = cells[0].x;
            var minY = cells[0].y;
            var maxY = cells[0].y;
            foreach (var cell in cells)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            return new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        }

        private static void ApplyCellOwnership(EditableMapDraftAsset draft, List<Vector2Int> component, string roomId, string zoneId)
        {
            foreach (var position in component)
            {
                var cell = draft.GetCell(position.x, position.y);
                cell.RoomId = roomId;
                cell.ZoneId = zoneId;
                draft.TrySetCell(cell);
            }
        }

        private static EditableMapRoom BuildAnchorRoom(string id, MapRoomRole role, Vector2Int anchor, string zoneId)
        {
            return new EditableMapRoom
            {
                Id = id,
                Role = role,
                LayoutKind = RoomChunkLayoutKind.Room,
                X = anchor.x,
                Y = anchor.y,
                Width = 1,
                Height = 1,
                SocketMask = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West,
                HeightLevel = 0,
                ZoneId = zoneId,
                ChunkId = "drawn_anchor"
            };
        }

        private static List<EditableMapSocket> BuildRouteSockets(Vector2Int[] anchors, string areaRoomId)
        {
            var sockets = new List<EditableMapSocket>
            {
                BuildSocket("drawn_area_entry", areaRoomId, anchors[0], MapDirection.East, "drawn_start"),
                BuildSocket("drawn_start_to_area", "drawn_start", anchors[0], MapDirection.West, areaRoomId)
            };
            AddBidirectionalSockets(sockets, "drawn_start", anchors[0], "drawn_quest", anchors[1]);
            AddBidirectionalSockets(sockets, "drawn_quest", anchors[1], "drawn_boss", anchors[2]);
            AddBidirectionalSockets(sockets, "drawn_boss", anchors[2], "drawn_exit", anchors[3]);
            return sockets;
        }

        private static void AddBidirectionalSockets(
            List<EditableMapSocket> sockets,
            string fromRoomId,
            Vector2Int from,
            string toRoomId,
            Vector2Int to)
        {
            var forward = DirectionBetween(from, to);
            var backward = Opposite(forward);
            sockets.Add(BuildSocket($"{fromRoomId}_to_{toRoomId}", fromRoomId, from, forward, toRoomId));
            sockets.Add(BuildSocket($"{toRoomId}_to_{fromRoomId}", toRoomId, to, backward, fromRoomId));
        }

        private static EditableMapSocket BuildSocket(
            string id,
            string roomId,
            Vector2Int position,
            MapDirection direction,
            string targetRoomId)
        {
            return new EditableMapSocket
            {
                Id = id,
                RoomId = roomId,
                X = position.x,
                Y = position.y,
                Direction = direction,
                Width = 1,
                TargetRoomId = targetRoomId,
                LockedDoorKeyId = string.Empty
            };
        }

        private static MapDirection DirectionBetween(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return delta.x >= 0 ? MapDirection.East : MapDirection.West;
            }

            return delta.y >= 0 ? MapDirection.North : MapDirection.South;
        }

        private static MapDirection Opposite(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return MapDirection.West;
                case MapDirection.South:
                    return MapDirection.North;
                case MapDirection.West:
                    return MapDirection.East;
                default:
                    return MapDirection.South;
            }
        }

        private static bool IsWalkable(EditableMapDraftAsset draft, int x, int y)
        {
            if (!draft.TryGetCell(x, y, out var cell))
            {
                return false;
            }

            return cell.Terrain != RoomChunkCellType.Gap && cell.Terrain != RoomChunkCellType.Wall;
        }
    }
}

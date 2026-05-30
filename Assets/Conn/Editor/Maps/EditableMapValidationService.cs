using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapValidationService
    {
        private static readonly Vector2Int[] CardinalOffsets =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public static MapValidationReport Validate(EditableMapDraftAsset draft)
        {
            var report = new MapValidationReport();
            if (draft == null)
            {
                report.Errors.Add("Editable map draft is missing.");
                return report;
            }

            ValidateCells(draft, report);
            ValidatePaletteReferences(draft, report);
            var walkable = BuildWalkabilityMap(draft, report);
            ValidateObjects(draft, report);
            ValidateSockets(draft, walkable, report);
            ValidateRequiredRoutes(draft, walkable, report);
            ValidateHeightTransitions(draft, report);
            return report;
        }

        private static void ValidateCells(EditableMapDraftAsset draft, MapValidationReport report)
        {
            if (draft.Cells == null || draft.Cells.Length != draft.Width * draft.Height)
            {
                report.Errors.Add($"Draft cell array size mismatch: expected {draft.Width * draft.Height}, got {draft.Cells?.Length ?? 0}.");
                return;
            }

            foreach (var cell in draft.Cells)
            {
                if (!draft.IsInBounds(cell.X, cell.Y))
                {
                    report.Errors.Add($"Cell out of bounds at ({cell.X}, {cell.Y}).");
                }
            }
        }

        private static void ValidatePaletteReferences(EditableMapDraftAsset draft, MapValidationReport report)
        {
            var tileIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in draft.TilePalette?.Tiles ?? Array.Empty<MapTilePaletteEntry>())
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                {
                    tileIds.Add(entry.Id);
                }
            }

            var objectIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in draft.ObjectPalette?.Objects ?? Array.Empty<MapObjectPaletteEntry>())
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                {
                    objectIds.Add(entry.Id);
                }
            }

            foreach (var cell in draft.Cells ?? Array.Empty<EditableMapCell>())
            {
                if (!string.IsNullOrWhiteSpace(cell.MaterialId) && tileIds.Count > 0 && !tileIds.Contains(cell.MaterialId))
                {
                    report.Errors.Add($"Cell ({cell.X}, {cell.Y}) references missing tile palette id {cell.MaterialId}.");
                }
            }

            foreach (var placement in draft.Objects ?? Array.Empty<EditableMapObjectPlacement>())
            {
                if (!string.IsNullOrWhiteSpace(placement.PaletteObjectId) && objectIds.Count > 0 && !objectIds.Contains(placement.PaletteObjectId))
                {
                    report.Errors.Add($"Object {placement.Id} references missing object palette id {placement.PaletteObjectId}.");
                }
            }
        }

        private static bool[,] BuildWalkabilityMap(EditableMapDraftAsset draft, MapValidationReport report)
        {
            var walkable = new bool[draft.Width, draft.Height];
            foreach (var cell in draft.Cells ?? Array.Empty<EditableMapCell>())
            {
                if (!draft.IsInBounds(cell.X, cell.Y))
                {
                    continue;
                }

                walkable[cell.X, cell.Y] = cell.Terrain != RoomChunkCellType.Wall && cell.Terrain != RoomChunkCellType.Gap;
            }

            foreach (var placement in draft.Objects ?? Array.Empty<EditableMapObjectPlacement>())
            {
                if (!placement.BlocksMovement)
                {
                    continue;
                }

                for (var dy = 0; dy < Mathf.Max(1, placement.Depth); dy++)
                {
                    for (var dx = 0; dx < Mathf.Max(1, placement.Width); dx++)
                    {
                        var x = placement.X + dx;
                        var y = placement.Y + dy;
                        if (!draft.IsInBounds(x, y))
                        {
                            report.Errors.Add($"Blocking object {placement.Id} footprint is outside bounds at ({x}, {y}).");
                            continue;
                        }

                        walkable[x, y] = false;
                    }
                }
            }

            return walkable;
        }

        private static void ValidateObjects(EditableMapDraftAsset draft, MapValidationReport report)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placement in draft.Objects ?? Array.Empty<EditableMapObjectPlacement>())
            {
                if (!string.IsNullOrWhiteSpace(placement.Id) && !ids.Add(placement.Id))
                {
                    report.Errors.Add($"Duplicate object id: {placement.Id}.");
                }

                for (var dy = 0; dy < Mathf.Max(1, placement.Depth); dy++)
                {
                    for (var dx = 0; dx < Mathf.Max(1, placement.Width); dx++)
                    {
                        var x = placement.X + dx;
                        var y = placement.Y + dy;
                        if (!draft.TryGetCell(x, y, out var cell))
                        {
                            report.Errors.Add($"Object {placement.Id} footprint leaves the draft at ({x}, {y}).");
                            continue;
                        }

                        if (cell.Terrain == RoomChunkCellType.Wall || cell.Terrain == RoomChunkCellType.Gap)
                        {
                            report.Errors.Add($"Object {placement.Id} overlaps non-walkable cell ({x}, {y}).");
                        }
                    }
                }
            }
        }

        private static void ValidateSockets(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            foreach (var socket in draft.Sockets ?? Array.Empty<EditableMapSocket>())
            {
                if (!draft.IsInBounds(socket.X, socket.Y))
                {
                    report.Errors.Add($"Socket {socket.Id} is outside bounds at ({socket.X}, {socket.Y}).");
                    continue;
                }

                if (!walkable[socket.X, socket.Y])
                {
                    report.Errors.Add($"Socket {socket.Id} does not touch a walkable cell at ({socket.X}, {socket.Y}).");
                }
            }
        }

        private static void ValidateRequiredRoutes(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            if (!TryFindRoomCenter(draft, MapRoomRole.Start, out var start, out var startRoom))
            {
                report.Errors.Add("Missing start room.");
                return;
            }

            if (!TryFindRoomCenter(draft, MapRoomRole.QuestTarget, out var quest, out var questRoom))
            {
                report.Errors.Add("Missing quest room.");
                return;
            }

            if (!TryFindRoomCenter(draft, MapRoomRole.Boss, out var boss, out var bossRoom))
            {
                report.Errors.Add("Missing boss room.");
                return;
            }

            if (!TryFindRoomCenter(draft, MapRoomRole.Exit, out var exit, out var exitRoom))
            {
                report.Errors.Add("Missing exit room.");
                return;
            }

            ValidateRoute("start-to-quest", startRoom.Id, start, questRoom.Id, quest, draft, walkable, report);
            ValidateRoute("quest-to-boss", questRoom.Id, quest, bossRoom.Id, boss, draft, walkable, report);
            ValidateRoute("boss-to-exit", bossRoom.Id, boss, exitRoom.Id, exit, draft, walkable, report);
        }

        private static void ValidateRoute(
            string label,
            string fromRoomId,
            Vector2Int from,
            string toRoomId,
            Vector2Int to,
            EditableMapDraftAsset draft,
            bool[,] walkable,
            MapValidationReport report)
        {
            if (!CanReach(from, to, draft, walkable))
            {
                report.Errors.Add($"Required route {label} is blocked between room {fromRoomId} and room {toRoomId}.");
            }
        }

        private static bool CanReach(Vector2Int start, Vector2Int goal, EditableMapDraftAsset draft, bool[,] walkable)
        {
            if (!draft.IsInBounds(start.x, start.y) || !draft.IsInBounds(goal.x, goal.y))
            {
                return false;
            }

            if (!walkable[start.x, start.y] || !walkable[goal.x, goal.y])
            {
                return false;
            }

            var visited = new bool[draft.Width, draft.Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == goal)
                {
                    return true;
                }

                var currentCell = draft.GetCell(current.x, current.y);
                foreach (var offset in CardinalOffsets)
                {
                    var next = current + offset;
                    if (!draft.IsInBounds(next.x, next.y) || visited[next.x, next.y] || !walkable[next.x, next.y])
                    {
                        continue;
                    }

                    var nextCell = draft.GetCell(next.x, next.y);
                    if (Mathf.Abs(nextCell.Height - currentCell.Height) > 1)
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static void ValidateHeightTransitions(EditableMapDraftAsset draft, MapValidationReport report)
        {
            foreach (var cell in draft.Cells ?? Array.Empty<EditableMapCell>())
            {
                if (cell.Terrain != RoomChunkCellType.Slope && cell.Terrain != RoomChunkCellType.Stair)
                {
                    continue;
                }

                var next = ForwardCell(cell);
                if (!draft.TryGetCell(next.x, next.y, out var nextCell))
                {
                    report.Errors.Add($"{cell.Terrain} cell at ({cell.X}, {cell.Y}) points outside bounds.");
                    continue;
                }

                if (nextCell.Height - cell.Height != 1)
                {
                    report.Errors.Add($"{cell.Terrain} cell at ({cell.X}, {cell.Y}) requires a +1 height step toward {cell.Direction}.");
                }
            }
        }

        private static Vector2Int ForwardCell(EditableMapCell cell)
        {
            switch (cell.Direction)
            {
                case MapDirection.East:
                    return new Vector2Int(cell.X + 1, cell.Y);
                case MapDirection.South:
                    return new Vector2Int(cell.X, cell.Y - 1);
                case MapDirection.West:
                    return new Vector2Int(cell.X - 1, cell.Y);
                default:
                    return new Vector2Int(cell.X, cell.Y + 1);
            }
        }

        private static bool TryFindRoomCenter(EditableMapDraftAsset draft, MapRoomRole role, out Vector2Int center, out EditableMapRoom room)
        {
            foreach (var candidate in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (candidate.Role != role)
                {
                    continue;
                }

                var x = candidate.X + Mathf.Max(0, candidate.Width / 2);
                var y = candidate.Y + Mathf.Max(0, candidate.Height / 2);
                center = new Vector2Int(x, y);
                room = candidate;
                return true;
            }

            center = default;
            room = default;
            return false;
        }
    }
}

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
            ValidateRoomsAndZones(draft, report);
            ValidatePaletteReferences(draft, report);
            var walkable = BuildWalkabilityMap(draft, report);
            ValidateObjects(draft, report);
            ValidateSockets(draft, walkable, report);
            ValidateRoomEntrySocketsReachAnchors(draft, walkable, report);
            ValidateRequiredRoutes(draft, walkable, report);
            ValidateOptionalRoutes(draft, walkable, report);
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

        private static void ValidateRoomsAndZones(EditableMapDraftAsset draft, MapValidationReport report)
        {
            var roomIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (string.IsNullOrWhiteSpace(room.Id))
                {
                    report.Errors.Add("Room id is missing.");
                }
                else if (!roomIds.Add(room.Id))
                {
                    report.Errors.Add($"Duplicate room id: {room.Id}.");
                }

                if (room.Width <= 0 || room.Height <= 0)
                {
                    report.Errors.Add($"Room {room.Id} has invalid size {room.Width}x{room.Height}.");
                }

                if (room.X < 0 || room.Y < 0 || room.X + room.Width > draft.Width || room.Y + room.Height > draft.Height)
                {
                    report.Errors.Add($"Room {room.Id} bounds leave the draft at ({room.X}, {room.Y}) size {room.Width}x{room.Height}.");
                }

                if (room.Width > 0 && room.Height > 0 && !RoomContainsWalkableCell(draft, room))
                {
                    report.Errors.Add($"Room {room.Id} contains no walkable cells.");
                }
            }

            var zoneIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var zone in draft.Zones ?? Array.Empty<EditableMapZone>())
            {
                if (string.IsNullOrWhiteSpace(zone.Id))
                {
                    report.Errors.Add("Zone id is missing.");
                }
                else if (!zoneIds.Add(zone.Id))
                {
                    report.Errors.Add($"Duplicate zone id: {zone.Id}.");
                }
            }

            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (!string.IsNullOrWhiteSpace(room.ZoneId) && !zoneIds.Contains(room.ZoneId))
                {
                    report.Errors.Add($"Room {room.Id} references missing zone id {room.ZoneId}.");
                }
            }

            foreach (var cell in draft.Cells ?? Array.Empty<EditableMapCell>())
            {
                if (!string.IsNullOrWhiteSpace(cell.RoomId) && !roomIds.Contains(cell.RoomId))
                {
                    report.Errors.Add($"Cell ({cell.X}, {cell.Y}) references missing room id {cell.RoomId}.");
                }

                if (!string.IsNullOrWhiteSpace(cell.ZoneId) && !zoneIds.Contains(cell.ZoneId))
                {
                    report.Errors.Add($"Cell ({cell.X}, {cell.Y}) references missing zone id {cell.ZoneId}.");
                }
            }
        }

        private static bool RoomContainsWalkableCell(EditableMapDraftAsset draft, EditableMapRoom room)
        {
            for (var y = room.Y; y < room.Y + room.Height; y++)
            {
                for (var x = room.X; x < room.X + room.Width; x++)
                {
                    if (!draft.TryGetCell(x, y, out var cell))
                    {
                        continue;
                    }

                    if (cell.Terrain != RoomChunkCellType.Wall && cell.Terrain != RoomChunkCellType.Gap)
                    {
                        return true;
                    }
                }
            }

            return false;
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

            var objectEntries = new Dictionary<string, MapObjectPaletteEntry>(StringComparer.Ordinal);
            foreach (var entry in draft.ObjectPalette?.Objects ?? Array.Empty<MapObjectPaletteEntry>())
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                {
                    objectEntries[entry.Id] = entry;
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
                if (string.IsNullOrWhiteSpace(placement.PaletteObjectId) || objectEntries.Count == 0)
                {
                    continue;
                }

                if (!objectEntries.TryGetValue(placement.PaletteObjectId, out var entry))
                {
                    report.Errors.Add($"Object {placement.Id} references missing object palette id {placement.PaletteObjectId}.");
                    continue;
                }

                if (placement.Kind != entry.Kind)
                {
                    report.Errors.Add($"Object {placement.Id} kind {placement.Kind} does not match palette {placement.PaletteObjectId} kind {entry.Kind}.");
                }

                if (Mathf.Max(1, placement.Width) != Mathf.Max(1, entry.FootprintWidth)
                    || Mathf.Max(1, placement.Depth) != Mathf.Max(1, entry.FootprintDepth))
                {
                    report.Errors.Add($"Object {placement.Id} footprint {Mathf.Max(1, placement.Width)}x{Mathf.Max(1, placement.Depth)} does not match palette {placement.PaletteObjectId} footprint {Mathf.Max(1, entry.FootprintWidth)}x{Mathf.Max(1, entry.FootprintDepth)}.");
                }

                if (placement.BlocksMovement != entry.BlocksMovement)
                {
                    report.Errors.Add($"Object {placement.Id} blocking flag {placement.BlocksMovement} does not match palette {placement.PaletteObjectId} blocking flag {entry.BlocksMovement}.");
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
            var occupiedCells = new Dictionary<Vector2Int, string>();
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

                        var position = new Vector2Int(x, y);
                        if (occupiedCells.TryGetValue(position, out var existingId))
                        {
                            report.Errors.Add($"Object {placement.Id} overlaps object {existingId} at ({x}, {y}).");
                        }
                        else
                        {
                            occupiedCells.Add(position, placement.Id ?? string.Empty);
                        }
                    }
                }
            }
        }

        private static void ValidateSockets(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            var roomIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (!string.IsNullOrWhiteSpace(room.Id))
                {
                    roomIds.Add(room.Id);
                }
            }

            foreach (var socket in draft.Sockets ?? Array.Empty<EditableMapSocket>())
            {
                if (!string.IsNullOrWhiteSpace(socket.Id) && !socketIds.Add(socket.Id))
                {
                    report.Errors.Add($"Duplicate socket id: {socket.Id}.");
                }

                if (string.IsNullOrWhiteSpace(socket.RoomId) || !roomIds.Contains(socket.RoomId))
                {
                    report.Errors.Add($"Socket {socket.Id} references missing room id {socket.RoomId}.");
                }

                if (!string.IsNullOrWhiteSpace(socket.TargetRoomId) && !roomIds.Contains(socket.TargetRoomId))
                {
                    report.Errors.Add($"Socket {socket.Id} references missing target room id {socket.TargetRoomId}.");
                }

                if (!draft.IsInBounds(socket.X, socket.Y))
                {
                    report.Errors.Add($"Socket {socket.Id} is outside bounds at ({socket.X}, {socket.Y}).");
                    continue;
                }

                if (!walkable[socket.X, socket.Y])
                {
                    report.Errors.Add($"Socket {socket.Id} does not touch a walkable cell at ({socket.X}, {socket.Y}).");
                }

                if (TryFindRoom(draft, socket.RoomId, out var room) && !IsInsideRoom(socket.X, socket.Y, room))
                {
                    report.Errors.Add($"Socket {socket.Id} is outside its room {socket.RoomId}.");
                }

                if (!string.IsNullOrWhiteSpace(socket.TargetRoomId) && !HasReciprocalSocket(draft, socket))
                {
                    report.Errors.Add($"Socket {socket.Id} from room {socket.RoomId} to {socket.TargetRoomId} has no reciprocal socket.");
                }
            }
        }

        private static void ValidateRequiredRoutes(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            if (!TryFindRequiredRouteAnchor(draft, walkable, MapRoomRole.Start, out var start, out var startRoom))
            {
                report.Errors.Add("Missing start room.");
                return;
            }

            if (!TryFindRequiredRouteAnchor(draft, walkable, MapRoomRole.QuestTarget, out var quest, out var questRoom))
            {
                report.Errors.Add("Missing quest room.");
                return;
            }

            if (!TryFindRequiredRouteAnchor(draft, walkable, MapRoomRole.Boss, out var boss, out var bossRoom))
            {
                report.Errors.Add("Missing boss room.");
                return;
            }

            if (!TryFindRequiredRouteAnchor(draft, walkable, MapRoomRole.Exit, out var exit, out var exitRoom))
            {
                report.Errors.Add("Missing exit room.");
                return;
            }

            ValidateRoute("start-to-quest", startRoom.Id, start, questRoom.Id, quest, draft, walkable, report);
            ValidateRoute("quest-to-boss", questRoom.Id, quest, bossRoom.Id, boss, draft, walkable, report);
            ValidateRoute("boss-to-exit", bossRoom.Id, boss, exitRoom.Id, exit, draft, walkable, report);
        }

        private static void ValidateRoomEntrySocketsReachAnchors(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            ValidateRequiredRoomEntrySockets(draft, walkable, MapRoomRole.Start, report, requireEntrySocket: false);
            ValidateRequiredRoomEntrySockets(draft, walkable, MapRoomRole.QuestTarget, report, requireEntrySocket: true);
            ValidateRequiredRoomEntrySockets(draft, walkable, MapRoomRole.Boss, report, requireEntrySocket: true);
            ValidateRequiredRoomEntrySockets(draft, walkable, MapRoomRole.Exit, report, requireEntrySocket: true);
        }

        private static void ValidateRequiredRoomEntrySockets(
            EditableMapDraftAsset draft,
            bool[,] walkable,
            MapRoomRole role,
            MapValidationReport report,
            bool requireEntrySocket)
        {
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (room.Role != role)
                {
                    continue;
                }

                var sockets = FindRoomSockets(draft, room.Id);
                if (requireEntrySocket && sockets.Count == 0)
                {
                    report.Errors.Add($"Room {room.Id} has no entry socket for required role {role}.");
                    continue;
                }

                if (!TryResolveRoomTarget(draft, room, walkable, RoomCenter(room), out var anchor))
                {
                    report.Errors.Add($"Room {room.Id} has no walkable anchor cell for required role {role}.");
                    continue;
                }

                foreach (var socket in sockets)
                {
                    if (!CanReachWithinRoom(new Vector2Int(socket.X, socket.Y), anchor, room, draft, walkable))
                    {
                        report.Errors.Add($"Socket {socket.Id} in room {room.Id} cannot reach the required anchor for role {role}.");
                    }
                }
            }
        }

        private static void ValidateOptionalRoutes(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            ValidateOptionalDeadEndRooms(draft, walkable, report);
            ValidateOptionalTreasureRoutes(draft, walkable, report);
        }

        private static void ValidateOptionalDeadEndRooms(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (room.LayoutKind != RoomChunkLayoutKind.DeadEnd)
                {
                    continue;
                }

                var sockets = FindRoomSockets(draft, room.Id);
                if (sockets.Count == 0)
                {
                    report.Errors.Add($"Room {room.Id} is a dead-end but has no entry socket.");
                    continue;
                }

                if (!TryResolveRoomTarget(draft, room, walkable, RoomCenter(room), out var target))
                {
                    report.Errors.Add($"Room {room.Id} is a dead-end but has no reachable anchor cell.");
                    continue;
                }

                foreach (var socket in sockets)
                {
                    if (!CanReachWithinRoom(new Vector2Int(socket.X, socket.Y), target, room, draft, walkable))
                    {
                        report.Errors.Add($"Socket {socket.Id} in dead-end room {room.Id} cannot reach the optional route target.");
                    }
                }
            }
        }

        private static void ValidateOptionalTreasureRoutes(EditableMapDraftAsset draft, bool[,] walkable, MapValidationReport report)
        {
            foreach (var placement in draft.Objects ?? Array.Empty<EditableMapObjectPlacement>())
            {
                if (placement.Kind != RoomChunkObjectKind.Chest)
                {
                    continue;
                }

                var room = FindRoomForPosition(draft, placement.X, placement.Y);
                if (room == null)
                {
                    report.Errors.Add($"Object {placement.Id} is marked as treasure but is not inside a room.");
                    continue;
                }

                var sockets = FindRoomSockets(draft, room.Value.Id);
                if (sockets.Count == 0)
                {
                    report.Errors.Add($"Object {placement.Id} is in room {room.Value.Id} with no entry socket.");
                    continue;
                }

                if (!TryResolveRoomTarget(draft, room.Value, walkable, new Vector2Int(placement.X, placement.Y), out var target))
                {
                    report.Errors.Add($"Object {placement.Id} has no reachable walkable anchor cell in room {room.Value.Id}.");
                    continue;
                }

                foreach (var socket in sockets)
                {
                    if (!CanReachWithinRoom(new Vector2Int(socket.X, socket.Y), target, room.Value, draft, walkable))
                    {
                        report.Errors.Add($"Socket {socket.Id} in room {room.Value.Id} cannot reach optional treasure object {placement.Id}.");
                    }
                }
            }
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

        private static bool CanReachWithinRoom(
            Vector2Int start,
            Vector2Int goal,
            EditableMapRoom room,
            EditableMapDraftAsset draft,
            bool[,] walkable)
        {
            if (!draft.IsInBounds(start.x, start.y) || !draft.IsInBounds(goal.x, goal.y))
            {
                return false;
            }

            if (!walkable[start.x, start.y] || !walkable[goal.x, goal.y])
            {
                return false;
            }

            if (!IsInsideRoom(start.x, start.y, room) || !IsInsideRoom(goal.x, goal.y, room))
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
                    if (!draft.IsInBounds(next.x, next.y)
                        || visited[next.x, next.y]
                        || !walkable[next.x, next.y]
                        || !IsInsideRoom(next.x, next.y, room))
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

        private static bool TryFindRequiredRouteAnchor(
            EditableMapDraftAsset draft,
            bool[,] walkable,
            MapRoomRole role,
            out Vector2Int target,
            out EditableMapRoom room)
        {
            if (TryFindRoomCenter(draft, role, out var center, out room)
                && TryResolveRoomTarget(draft, room, walkable, center, out target))
            {
                return true;
            }

            target = default;
            room = default;
            return false;
        }

        private static List<EditableMapSocket> FindRoomSockets(EditableMapDraftAsset draft, string roomId)
        {
            var sockets = new List<EditableMapSocket>();
            foreach (var socket in draft.Sockets ?? Array.Empty<EditableMapSocket>())
            {
                if (socket.RoomId == roomId)
                {
                    sockets.Add(socket);
                }
            }

            return sockets;
        }

        private static bool TryResolveRoomTarget(
            EditableMapDraftAsset draft,
            EditableMapRoom room,
            bool[,] walkable,
            Vector2Int preferred,
            out Vector2Int target)
        {
            if (draft.IsInBounds(preferred.x, preferred.y)
                && IsInsideRoom(preferred.x, preferred.y, room)
                && walkable[preferred.x, preferred.y])
            {
                target = preferred;
                return true;
            }

            var bestDistance = int.MaxValue;
            var best = default(Vector2Int);
            var found = false;
            for (var y = room.Y; y < room.Y + room.Height; y++)
            {
                for (var x = room.X; x < room.X + room.Width; x++)
                {
                    if (!draft.IsInBounds(x, y) || !walkable[x, y])
                    {
                        continue;
                    }

                    var distance = Mathf.Abs(preferred.x - x) + Mathf.Abs(preferred.y - y);
                    if (!found || distance < bestDistance)
                    {
                        found = true;
                        bestDistance = distance;
                        best = new Vector2Int(x, y);
                    }
                }
            }

            target = best;
            return found;
        }

        private static EditableMapRoom? FindRoomForPosition(EditableMapDraftAsset draft, int x, int y)
        {
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (IsInsideRoom(x, y, room))
                {
                    return room;
                }
            }

            return null;
        }

        private static bool TryFindRoom(EditableMapDraftAsset draft, string roomId, out EditableMapRoom room)
        {
            foreach (var candidate in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (candidate.Id == roomId)
                {
                    room = candidate;
                    return true;
                }
            }

            room = default;
            return false;
        }

        private static bool HasReciprocalSocket(EditableMapDraftAsset draft, EditableMapSocket socket)
        {
            foreach (var candidate in draft.Sockets ?? Array.Empty<EditableMapSocket>())
            {
                if (candidate.RoomId == socket.TargetRoomId && candidate.TargetRoomId == socket.RoomId)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2Int RoomCenter(EditableMapRoom room)
        {
            return new Vector2Int(room.X + Mathf.Max(0, room.Width / 2), room.Y + Mathf.Max(0, room.Height / 2));
        }

        private static bool IsInsideRoom(int x, int y, EditableMapRoom room)
        {
            return x >= room.X && y >= room.Y && x < room.X + room.Width && y < room.Y + room.Height;
        }
    }
}

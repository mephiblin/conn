using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
    using Conn.Core.Quests;

    public sealed class MapValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public bool Passed => Errors.Count == 0;
    }

    public static class MapValidationService
    {
        public static MapValidationReport Validate(MapProfile profile, GeneratedMapDraft draft)
        {
            var report = new MapValidationReport();
            if (profile == null)
            {
                report.Errors.Add("Map profile is missing.");
                return report;
            }

            if (draft == null)
            {
                report.Errors.Add("Generated map draft is missing.");
                return report;
            }

            ExpectRequiredPlacements(profile, draft, report);
            ExpectReachability(draft, report);
            ExpectCriticalPathOrder(draft, report);
            ExpectSockets(draft, report);
            return report;
        }

        public static MapValidationReport ValidateCompiled(MapProfile profile, CompiledMap compiled)
        {
            var report = new MapValidationReport();
            if (profile == null)
            {
                report.Errors.Add("Map profile is missing.");
                return report;
            }

            if (compiled == null)
            {
                report.Errors.Add("Compiled map is missing.");
                return report;
            }

            ExpectCompiledHeader(profile, compiled, report);
            var walkableCells = BuildCompiledWalkableCellSet(compiled, report);
            ExpectCompiledRoomRecords(compiled, report);
            ExpectCompiledZoneRecords(compiled, report);
            ExpectCompiledPlacements(profile, compiled, walkableCells, report);
            ExpectCompiledEncounterPlacements(compiled, report);
            ExpectCompiledSocketsAndDoors(compiled, walkableCells, report);
            ExpectCompiledCellsAndObjects(compiled, walkableCells, report);
            ExpectCompiledHeightTransitions(compiled, report);
            return report;
        }

        public static MapValidationReport ValidateQuestMapContract(QuestDefinition quest, MapProfile profile, CompiledMap compiled)
        {
            var report = ValidateCompiled(profile, compiled);
            if (quest == null)
            {
                report.Errors.Add("Quest definition is missing.");
                return report;
            }

            if (profile == null || compiled == null)
            {
                return report;
            }

            if (!string.IsNullOrEmpty(quest.MapProfileId) && quest.MapProfileId != profile.ProfileId)
            {
                report.Errors.Add($"Quest {quest.QuestId} requires map profile {quest.MapProfileId}, but profile is {profile.ProfileId}.");
            }

            if (!HasPlacement(compiled, quest.RequiredMapPlacement))
            {
                report.Errors.Add($"Quest {quest.QuestId} requires compiled map placement {quest.RequiredMapPlacement}.");
            }

            return report;
        }

        public static void ThrowIfFailed(MapValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (!report.Passed)
            {
                throw new InvalidOperationException(string.Join("\n", report.Errors.ToArray()));
            }
        }

        private static void ExpectRequiredPlacements(MapProfile profile, GeneratedMapDraft draft, MapValidationReport report)
        {
            for (var i = 0; i < profile.RequiredAnchors.Count; i++)
            {
                if (!TryPlacementKind(profile.RequiredAnchors[i], out var expectedKind))
                {
                    report.Errors.Add($"Required anchor {profile.RequiredAnchors[i]} cannot become a runtime placement.");
                    continue;
                }

                if (!HasPlacement(draft, expectedKind))
                {
                    report.Errors.Add($"Missing required placement: {expectedKind}.");
                }
            }
        }

        private static bool HasPlacement(GeneratedMapDraft draft, MapPlacementKind kind)
        {
            for (var i = 0; i < draft.Placements.Count; i++)
            {
                if (draft.Placements[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPlacement(CompiledMap compiled, MapPlacementKind kind)
        {
            for (var i = 0; i < compiled.Placements.Count; i++)
            {
                if (compiled.Placements[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryPlacementKind(MapAnchorKind anchor, out MapPlacementKind kind)
        {
            if ((int)anchor >= 0 && (int)anchor <= (int)MapPlacementKind.Loot)
            {
                kind = (MapPlacementKind)(int)anchor;
                return true;
            }

            kind = MapPlacementKind.Start;
            return false;
        }

        private static void ExpectCompiledHeader(MapProfile profile, CompiledMap compiled, MapValidationReport report)
        {
            if (string.IsNullOrEmpty(compiled.MapId))
            {
                report.Errors.Add("Compiled map id is missing.");
            }

            if (compiled.ProfileId != profile.ProfileId)
            {
                report.Errors.Add($"Compiled map profile mismatch: expected {profile.ProfileId}, got {compiled.ProfileId}.");
            }

            if (compiled.Width != profile.Width || compiled.Height != profile.Height)
            {
                report.Errors.Add("Compiled map dimensions do not match profile.");
            }
        }

        private static void ExpectCompiledPlacements(
            MapProfile profile,
            CompiledMap compiled,
            HashSet<string> walkableCells,
            MapValidationReport report)
        {
            for (var i = 0; i < profile.RequiredAnchors.Count; i++)
            {
                if (!TryPlacementKind(profile.RequiredAnchors[i], out var expectedKind))
                {
                    report.Errors.Add($"Required anchor {profile.RequiredAnchors[i]} cannot become a runtime placement.");
                    continue;
                }

                if (!HasPlacement(compiled, expectedKind))
                {
                    report.Errors.Add($"Compiled map missing required placement: {expectedKind}.");
                }
            }

            var ids = new HashSet<string>();
            for (var i = 0; i < compiled.Placements.Count; i++)
            {
                var placement = compiled.Placements[i];
                if (string.IsNullOrEmpty(placement.Id))
                {
                    report.Errors.Add("Compiled map contains a placement with no id.");
                }
                else if (!ids.Add(placement.Id))
                {
                    report.Errors.Add($"Compiled map contains duplicate placement id: {placement.Id}.");
                }

                if (FindNode(compiled.Rooms, placement.RoomId) == null)
                {
                    report.Errors.Add($"Compiled map placement {placement.Id} references missing room {placement.RoomId}.");
                }

                if (placement.X < 0 || placement.Y < 0 || placement.X >= compiled.Width || placement.Y >= compiled.Height)
                {
                    report.Errors.Add($"Compiled map placement {placement.Id} is outside map bounds.");
                }
                else if (compiled.Cells.Count > 0 && !walkableCells.Contains(CellKey(placement.X, placement.Y)))
                {
                    report.Errors.Add($"Compiled map placement {placement.Id} is on a non-walkable or missing cell ({placement.X}, {placement.Y}).");
                }
            }
        }

        private static void ExpectCompiledSocketsAndDoors(
            CompiledMap compiled,
            HashSet<string> walkableCells,
            MapValidationReport report)
        {
            var roomIds = BuildCompiledRoomIdSet(compiled);
            var roomRecords = BuildCompiledRoomRecordLookup(compiled);
            var doorPairs = new HashSet<string>();
            for (var i = 0; i < compiled.Doors.Count; i++)
            {
                var edge = compiled.Doors[i];
                var from = FindNode(compiled.Rooms, edge.FromNodeId);
                var to = FindNode(compiled.Rooms, edge.ToNodeId);
                if (from == null || to == null)
                {
                    report.Errors.Add($"Compiled map door {edge.FromNodeId}->{edge.ToNodeId} references a missing room.");
                }

                doorPairs.Add(RoomPairKey(edge.FromNodeId, edge.ToNodeId));
            }

            var socketIds = new HashSet<string>();
            var socketPairs = new HashSet<string>();
            foreach (var socket in compiled.Sockets ?? new List<CompiledMapSocketRecord>())
            {
                if (string.IsNullOrWhiteSpace(socket.Id))
                {
                    report.Errors.Add("Compiled map contains a socket with no id.");
                }
                else if (!socketIds.Add(socket.Id))
                {
                    report.Errors.Add($"Compiled map contains duplicate socket id: {socket.Id}.");
                }

                if (string.IsNullOrWhiteSpace(socket.RoomId) || !roomIds.Contains(socket.RoomId))
                {
                    report.Errors.Add($"Compiled map socket {socket.Id} references missing room {socket.RoomId}.");
                }

                if (!string.IsNullOrWhiteSpace(socket.TargetRoomId) && !roomIds.Contains(socket.TargetRoomId))
                {
                    report.Errors.Add($"Compiled map socket {socket.Id} references missing target room {socket.TargetRoomId}.");
                }

                if (!IsSingleCardinalDirection(socket.Direction))
                {
                    report.Errors.Add($"Compiled map socket {socket.Id} has invalid direction {socket.Direction}.");
                }

                var width = Math.Max(1, socket.Width);
                for (var offset = 0; offset < width; offset++)
                {
                    var x = socket.X;
                    var y = socket.Y;
                    if (socket.Direction == MapDirection.North || socket.Direction == MapDirection.South)
                    {
                        x += offset;
                    }
                    else
                    {
                        y += offset;
                    }

                    if (x < 0 || y < 0 || x >= compiled.Width || y >= compiled.Height)
                    {
                        report.Errors.Add($"Compiled map socket {socket.Id} width leaves map bounds at ({x}, {y}).");
                        continue;
                    }

                    if (compiled.Cells.Count > 0 && !walkableCells.Contains(CellKey(x, y)))
                    {
                        report.Errors.Add($"Compiled map socket {socket.Id} includes non-walkable or missing cell ({x}, {y}).");
                    }

                    if (roomRecords.TryGetValue(socket.RoomId ?? string.Empty, out var room)
                        && (!IsInsideRoomRecord(x, y, room) || !IsOnSocketBoundary(x, y, socket.Direction, room)))
                    {
                        report.Errors.Add($"Compiled map socket {socket.Id} direction {socket.Direction} is not on the matching room boundary at ({x}, {y}).");
                    }
                }

                if (string.IsNullOrWhiteSpace(socket.TargetRoomId))
                {
                    continue;
                }

                socketPairs.Add(RoomPairKey(socket.RoomId, socket.TargetRoomId));
                if (!TryFindReciprocalSocket(compiled, socket, out var reciprocal))
                {
                    report.Errors.Add($"Compiled map socket {socket.Id} from room {socket.RoomId} to {socket.TargetRoomId} has no reciprocal socket.");
                    continue;
                }

                if (reciprocal.Direction != OppositeDirection(socket.Direction))
                {
                    report.Errors.Add($"Compiled map socket {socket.Id} reciprocal socket {reciprocal.Id} has direction {reciprocal.Direction}, expected {OppositeDirection(socket.Direction)}.");
                }

                if (!string.Equals(socket.LockedDoorKeyId ?? string.Empty, reciprocal.LockedDoorKeyId ?? string.Empty, StringComparison.Ordinal))
                {
                    report.Errors.Add($"Compiled map socket {socket.Id} locked key does not match reciprocal socket {reciprocal.Id}.");
                }
            }

            if ((compiled.Sockets?.Count ?? 0) > 0)
            {
                foreach (var doorPair in doorPairs)
                {
                    if (!socketPairs.Contains(doorPair))
                    {
                        report.Errors.Add($"Compiled map door {doorPair} has no matching socket pair.");
                    }
                }

                foreach (var socketPair in socketPairs)
                {
                    if (!doorPairs.Contains(socketPair))
                    {
                        report.Errors.Add($"Compiled map socket pair {socketPair} has no baked door.");
                    }
                }
            }
        }

        private static void ExpectCompiledRoomRecords(CompiledMap compiled, MapValidationReport report)
        {
            if ((compiled.RoomRecords?.Count ?? 0) == 0)
            {
                return;
            }

            var graphRoomIds = BuildCompiledRoomIdSet(compiled);
            var recordIds = new HashSet<string>();
            foreach (var room in compiled.RoomRecords ?? new List<CompiledMapRoomRecord>())
            {
                if (string.IsNullOrWhiteSpace(room.Id))
                {
                    report.Errors.Add("Compiled map contains a room record with no id.");
                }
                else if (!recordIds.Add(room.Id))
                {
                    report.Errors.Add($"Compiled map contains duplicate room record id: {room.Id}.");
                }

                if (!string.IsNullOrWhiteSpace(room.Id) && !graphRoomIds.Contains(room.Id))
                {
                    report.Errors.Add($"Compiled map room record {room.Id} has no matching graph room.");
                }

                if (room.Width <= 0 || room.Height <= 0)
                {
                    report.Errors.Add($"Compiled map room record {room.Id} has invalid size {room.Width}x{room.Height}.");
                    continue;
                }

                if (room.X < 0 || room.Y < 0 || room.X + room.Width > compiled.Width || room.Y + room.Height > compiled.Height)
                {
                    report.Errors.Add($"Compiled map room record {room.Id} bounds leave the map at ({room.X}, {room.Y}) size {room.Width}x{room.Height}.");
                }
            }

            foreach (var roomId in graphRoomIds)
            {
                if (!recordIds.Contains(roomId))
                {
                    report.Errors.Add($"Compiled map graph room {roomId} has no room record.");
                }
            }

            var roomRecords = BuildCompiledRoomRecordLookup(compiled);
            foreach (var cell in compiled.Cells ?? new List<CompiledMapCell>())
            {
                if (string.IsNullOrWhiteSpace(cell.RoomId))
                {
                    continue;
                }

                if (!roomRecords.TryGetValue(cell.RoomId, out var room))
                {
                    continue;
                }

                if (!IsInsideRoomRecord(cell.X, cell.Y, room))
                {
                    report.Errors.Add($"Compiled map cell ({cell.X}, {cell.Y}) is outside room record {cell.RoomId}.");
                }
            }
        }

        private static void ExpectCompiledZoneRecords(CompiledMap compiled, MapValidationReport report)
        {
            var zoneIds = new HashSet<string>();
            foreach (var zone in compiled.Zones ?? new List<CompiledMapZoneRecord>())
            {
                if (string.IsNullOrWhiteSpace(zone.Id))
                {
                    report.Errors.Add("Compiled map contains a zone record with no id.");
                }
                else if (!zoneIds.Add(zone.Id))
                {
                    report.Errors.Add($"Compiled map contains duplicate zone record id: {zone.Id}.");
                }
            }

            foreach (var room in compiled.RoomRecords ?? new List<CompiledMapRoomRecord>())
            {
                if (!string.IsNullOrWhiteSpace(room.ZoneId) && !zoneIds.Contains(room.ZoneId))
                {
                    report.Errors.Add($"Compiled map room record {room.Id} references missing zone {room.ZoneId}.");
                }
            }

            foreach (var cell in compiled.Cells ?? new List<CompiledMapCell>())
            {
                if (!string.IsNullOrWhiteSpace(cell.ZoneId) && !zoneIds.Contains(cell.ZoneId))
                {
                    report.Errors.Add($"Compiled map cell ({cell.X}, {cell.Y}) references missing zone {cell.ZoneId}.");
                }
            }
        }

        private static void ExpectCompiledCellsAndObjects(
            CompiledMap compiled,
            HashSet<string> walkableCells,
            MapValidationReport report)
        {
            var roomIds = new HashSet<string>();
            for (var i = 0; i < compiled.Rooms.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(compiled.Rooms[i].Id))
                {
                    roomIds.Add(compiled.Rooms[i].Id);
                }
            }

            foreach (var cell in compiled.Cells ?? new List<CompiledMapCell>())
            {
                if (!string.IsNullOrWhiteSpace(cell.RoomId) && !roomIds.Contains(cell.RoomId))
                {
                    report.Errors.Add($"Compiled map cell ({cell.X}, {cell.Y}) references missing room {cell.RoomId}.");
                }

            }

            var objectIds = new HashSet<string>();
            var occupiedObjectCells = new Dictionary<string, string>();
            foreach (var placement in compiled.Objects ?? new List<CompiledMapObjectPlacement>())
            {
                if (string.IsNullOrWhiteSpace(placement.PlacementId))
                {
                    report.Errors.Add("Compiled map contains an object placement with no id.");
                }
                else if (!objectIds.Add(placement.PlacementId))
                {
                    report.Errors.Add($"Compiled map contains duplicate object placement id: {placement.PlacementId}.");
                }

                for (var dy = 0; dy < Math.Max(1, placement.Depth); dy++)
                {
                    for (var dx = 0; dx < Math.Max(1, placement.Width); dx++)
                    {
                        var x = placement.X + dx;
                        var y = placement.Y + dy;
                        if (x < 0 || y < 0 || x >= compiled.Width || y >= compiled.Height)
                        {
                            report.Errors.Add($"Compiled map object {placement.PlacementId} footprint is outside map bounds at ({x}, {y}).");
                            continue;
                        }

                        if (compiled.Cells.Count > 0 && !walkableCells.Contains(CellKey(x, y)))
                        {
                            report.Errors.Add($"Compiled map object {placement.PlacementId} overlaps non-walkable or missing cell ({x}, {y}).");
                        }

                        var key = CellKey(x, y);
                        if (occupiedObjectCells.TryGetValue(key, out var existingId))
                        {
                            report.Errors.Add($"Compiled map object {placement.PlacementId} overlaps object {existingId} at ({x}, {y}).");
                        }
                        else
                        {
                            occupiedObjectCells.Add(key, placement.PlacementId ?? string.Empty);
                        }
                    }
                }
            }
        }

        private static HashSet<string> BuildCompiledRoomIdSet(CompiledMap compiled)
        {
            var ids = new HashSet<string>();
            foreach (var room in compiled.Rooms ?? new List<RoomGraphNode>())
            {
                if (!string.IsNullOrWhiteSpace(room.Id))
                {
                    ids.Add(room.Id);
                }
            }

            return ids;
        }

        private static Dictionary<string, CompiledMapRoomRecord> BuildCompiledRoomRecordLookup(CompiledMap compiled)
        {
            var records = new Dictionary<string, CompiledMapRoomRecord>();
            foreach (var room in compiled.RoomRecords ?? new List<CompiledMapRoomRecord>())
            {
                if (!string.IsNullOrWhiteSpace(room.Id))
                {
                    records[room.Id] = room;
                }
            }

            return records;
        }

        private static HashSet<string> BuildCompiledWalkableCellSet(CompiledMap compiled, MapValidationReport report)
        {
            var walkableCells = new HashSet<string>();
            if (compiled.Width <= 0 || compiled.Height <= 0)
            {
                report.Errors.Add($"Compiled map dimensions must be positive, got {compiled.Width}x{compiled.Height}.");
                return walkableCells;
            }

            var expectedCount = compiled.Width * compiled.Height;
            if (compiled.Cells == null || compiled.Cells.Count == 0)
            {
                return walkableCells;
            }

            if (compiled.Cells.Count != expectedCount)
            {
                report.Errors.Add($"Compiled map cell count mismatch: expected {expectedCount}, got {compiled.Cells.Count}.");
            }

            var seen = new bool[compiled.Width, compiled.Height];
            foreach (var cell in compiled.Cells ?? new List<CompiledMapCell>())
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= compiled.Width || cell.Y >= compiled.Height)
                {
                    report.Errors.Add($"Compiled map cell ({cell.X}, {cell.Y}) is outside map bounds.");
                    continue;
                }

                if (seen[cell.X, cell.Y])
                {
                    report.Errors.Add($"Compiled map contains duplicate cell coordinate ({cell.X}, {cell.Y}).");
                    continue;
                }

                seen[cell.X, cell.Y] = true;
                if (cell.Terrain != RoomChunkCellType.Wall && cell.Terrain != RoomChunkCellType.Gap)
                {
                    walkableCells.Add(CellKey(cell.X, cell.Y));
                }
            }

            for (var y = 0; y < compiled.Height; y++)
            {
                for (var x = 0; x < compiled.Width; x++)
                {
                    if (!seen[x, y])
                    {
                        report.Errors.Add($"Compiled map is missing cell coordinate ({x}, {y}).");
                    }
                }
            }

            return walkableCells;
        }

        private static void ExpectCompiledHeightTransitions(CompiledMap compiled, MapValidationReport report)
        {
            if (compiled.Cells == null || compiled.Cells.Count == 0)
            {
                return;
            }

            var cells = new Dictionary<string, CompiledMapCell>();
            foreach (var cell in compiled.Cells)
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= compiled.Width || cell.Y >= compiled.Height)
                {
                    continue;
                }

                cells[CellKey(cell.X, cell.Y)] = cell;
            }

            foreach (var cell in compiled.Cells)
            {
                if (cell.Terrain != RoomChunkCellType.Slope && cell.Terrain != RoomChunkCellType.Stair)
                {
                    continue;
                }

                if (!IsSingleCardinalDirection(cell.Direction))
                {
                    report.Errors.Add($"Compiled map {cell.Terrain} cell at ({cell.X}, {cell.Y}) has invalid direction {cell.Direction}.");
                    continue;
                }

                var next = ForwardCell(cell.X, cell.Y, cell.Direction);
                if (next.x < 0 || next.y < 0 || next.x >= compiled.Width || next.y >= compiled.Height)
                {
                    report.Errors.Add($"Compiled map {cell.Terrain} cell at ({cell.X}, {cell.Y}) points outside bounds.");
                    continue;
                }

                if (!cells.TryGetValue(CellKey(next.x, next.y), out var nextCell)
                    || nextCell.Height - cell.Height != 1)
                {
                    report.Errors.Add($"Compiled map {cell.Terrain} cell at ({cell.X}, {cell.Y}) requires a +1 height step toward {cell.Direction}.");
                }
            }
        }

        private static void ExpectCompiledEncounterPlacements(CompiledMap compiled, MapValidationReport report)
        {
            var ids = new HashSet<string>();
            foreach (var placement in compiled.EncounterPlacements ?? new List<CompiledEncounterPlacement>())
            {
                if (string.IsNullOrEmpty(placement.PlacementId))
                {
                    report.Errors.Add("Compiled map contains an encounter placement with no id.");
                }
                else if (!ids.Add(placement.PlacementId))
                {
                    report.Errors.Add($"Compiled map contains duplicate encounter placement id: {placement.PlacementId}.");
                }

                if (FindPlacement(compiled.Placements, placement.MapPlacementId) == null)
                {
                    report.Errors.Add($"Compiled encounter placement {placement.PlacementId} references missing map placement {placement.MapPlacementId}.");
                }

                if (FindNode(compiled.Rooms, placement.RoomId) == null)
                {
                    report.Errors.Add($"Compiled encounter placement {placement.PlacementId} references missing room {placement.RoomId}.");
                }

                if (string.IsNullOrWhiteSpace(placement.EncounterId))
                {
                    report.Errors.Add($"Compiled encounter placement {placement.PlacementId} encounter id must not be empty.");
                }

                if (string.IsNullOrWhiteSpace(placement.PrimaryMonsterId))
                {
                    report.Errors.Add($"Compiled encounter placement {placement.PlacementId} primary monster id must not be empty.");
                }
            }
        }

        private static void ExpectReachability(GeneratedMapDraft draft, MapValidationReport report)
        {
            var start = FindNodeByRole(draft.Graph, MapRoomRole.Start);
            if (start == null)
            {
                report.Errors.Add("Missing start room.");
                return;
            }

            var reached = Walk(draft.Graph, start.Id);
            for (var i = 0; i < draft.Placements.Count; i++)
            {
                if (!reached.Contains(draft.Placements[i].RoomId))
                {
                    report.Errors.Add($"Placement {draft.Placements[i].Id} is not reachable from start.");
                }
            }
        }

        private static void ExpectCriticalPathOrder(GeneratedMapDraft draft, MapValidationReport report)
        {
            var quest = FindNodeByRole(draft.Graph, MapRoomRole.QuestTarget);
            var boss = FindNodeByRole(draft.Graph, MapRoomRole.Boss);
            var exit = FindNodeByRole(draft.Graph, MapRoomRole.Exit);
            if (quest == null || boss == null || exit == null)
            {
                report.Errors.Add("Critical path must contain quest target, boss, and exit rooms.");
                return;
            }

            if (!(quest.PathIndex < boss.PathIndex && boss.PathIndex < exit.PathIndex))
            {
                report.Errors.Add("Quest target, boss, and exit must appear in that critical path order.");
            }
        }

        private static void ExpectSockets(GeneratedMapDraft draft, MapValidationReport report)
        {
            for (var i = 0; i < draft.Graph.Nodes.Count; i++)
            {
                var node = draft.Graph.Nodes[i];
                if (string.IsNullOrEmpty(node.ChunkId))
                {
                    report.Errors.Add($"Room {node.Id} has no selected chunk.");
                }
            }

            for (var i = 0; i < draft.Graph.Edges.Count; i++)
            {
                var edge = draft.Graph.Edges[i];
                var from = FindNode(draft.Graph, edge.FromNodeId);
                var to = FindNode(draft.Graph, edge.ToNodeId);
                if (from == null || to == null)
                {
                    report.Errors.Add($"Edge {edge.FromNodeId}->{edge.ToNodeId} references a missing room.");
                    continue;
                }

                var distance = Math.Abs(from.GridX - to.GridX) + Math.Abs(from.GridY - to.GridY);
                if (distance != 1)
                {
                    report.Errors.Add($"Edge {edge.FromNodeId}->{edge.ToNodeId} must connect adjacent rooms.");
                }
            }
        }

        private static HashSet<string> Walk(RoomGraph graph, string startId)
        {
            var reached = new HashSet<string>();
            var open = new Queue<string>();
            open.Enqueue(startId);
            reached.Add(startId);

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                for (var i = 0; i < graph.Edges.Count; i++)
                {
                    var edge = graph.Edges[i];
                    var next = string.Empty;
                    if (edge.FromNodeId == current)
                    {
                        next = edge.ToNodeId;
                    }
                    else if (edge.ToNodeId == current)
                    {
                        next = edge.FromNodeId;
                    }

                    if (!string.IsNullOrEmpty(next) && reached.Add(next))
                    {
                        open.Enqueue(next);
                    }
                }
            }

            return reached;
        }

        private static string CellKey(int x, int y)
        {
            return $"{x}:{y}";
        }

        private static string RoomPairKey(string firstRoomId, string secondRoomId)
        {
            return string.CompareOrdinal(firstRoomId, secondRoomId) <= 0
                ? $"{firstRoomId}->{secondRoomId}"
                : $"{secondRoomId}->{firstRoomId}";
        }

        private static bool IsSingleCardinalDirection(MapDirection direction)
        {
            return direction == MapDirection.North
                || direction == MapDirection.East
                || direction == MapDirection.South
                || direction == MapDirection.West;
        }

        private static (int x, int y) ForwardCell(int x, int y, MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return (x + 1, y);
                case MapDirection.South:
                    return (x, y - 1);
                case MapDirection.West:
                    return (x - 1, y);
                default:
                    return (x, y + 1);
            }
        }

        private static MapDirection OppositeDirection(MapDirection direction)
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

        private static bool IsInsideRoomRecord(int x, int y, CompiledMapRoomRecord room)
        {
            return x >= room.X && y >= room.Y && x < room.X + room.Width && y < room.Y + room.Height;
        }

        private static bool IsOnSocketBoundary(int x, int y, MapDirection direction, CompiledMapRoomRecord room)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return x == room.X + room.Width - 1;
                case MapDirection.South:
                    return y == room.Y;
                case MapDirection.West:
                    return x == room.X;
                default:
                    return y == room.Y + room.Height - 1;
            }
        }

        private static bool TryFindReciprocalSocket(CompiledMap compiled, CompiledMapSocketRecord socket, out CompiledMapSocketRecord reciprocal)
        {
            foreach (var candidate in compiled.Sockets ?? new List<CompiledMapSocketRecord>())
            {
                if (candidate.RoomId == socket.TargetRoomId && candidate.TargetRoomId == socket.RoomId)
                {
                    reciprocal = candidate;
                    return true;
                }
            }

            reciprocal = null;
            return false;
        }

        private static RoomGraphNode FindNodeByRole(RoomGraph graph, MapRoomRole role)
        {
            for (var i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].Role == role)
                {
                    return graph.Nodes[i];
                }
            }

            return null;
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

            return null;
        }

        private static RoomGraphNode FindNode(List<RoomGraphNode> nodes, string id)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == id)
                {
                    return nodes[i];
                }
            }

            return null;
        }

        private static MapPlacement FindPlacement(List<MapPlacement> placements, string id)
        {
            for (var i = 0; i < placements.Count; i++)
            {
                if (placements[i].Id == id)
                {
                    return placements[i];
                }
            }

            return null;
        }
    }
}

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
            ExpectCompiledPlacements(profile, compiled, report);
            ExpectCompiledEncounterPlacements(compiled, report);
            ExpectCompiledDoors(compiled, report);
            ExpectCompiledCellsAndObjects(compiled, report);
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

        private static void ExpectCompiledPlacements(MapProfile profile, CompiledMap compiled, MapValidationReport report)
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
            }
        }

        private static void ExpectCompiledDoors(CompiledMap compiled, MapValidationReport report)
        {
            for (var i = 0; i < compiled.Doors.Count; i++)
            {
                var edge = compiled.Doors[i];
                var from = FindNode(compiled.Rooms, edge.FromNodeId);
                var to = FindNode(compiled.Rooms, edge.ToNodeId);
                if (from == null || to == null)
                {
                    report.Errors.Add($"Compiled map door {edge.FromNodeId}->{edge.ToNodeId} references a missing room.");
                }
            }
        }

        private static void ExpectCompiledCellsAndObjects(CompiledMap compiled, MapValidationReport report)
        {
            var roomIds = new HashSet<string>();
            for (var i = 0; i < compiled.Rooms.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(compiled.Rooms[i].Id))
                {
                    roomIds.Add(compiled.Rooms[i].Id);
                }
            }

            var zoneIds = new HashSet<string>();
            foreach (var zone in compiled.Zones ?? new List<CompiledMapZoneRecord>())
            {
                if (!string.IsNullOrWhiteSpace(zone.Id))
                {
                    zoneIds.Add(zone.Id);
                }
            }

            var walkableCells = new HashSet<string>();
            foreach (var cell in compiled.Cells ?? new List<CompiledMapCell>())
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= compiled.Width || cell.Y >= compiled.Height)
                {
                    report.Errors.Add($"Compiled map cell ({cell.X}, {cell.Y}) is outside map bounds.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(cell.RoomId) && !roomIds.Contains(cell.RoomId))
                {
                    report.Errors.Add($"Compiled map cell ({cell.X}, {cell.Y}) references missing room {cell.RoomId}.");
                }

                if (!string.IsNullOrWhiteSpace(cell.ZoneId) && zoneIds.Count > 0 && !zoneIds.Contains(cell.ZoneId))
                {
                    report.Errors.Add($"Compiled map cell ({cell.X}, {cell.Y}) references missing zone {cell.ZoneId}.");
                }

                if (cell.Terrain != RoomChunkCellType.Wall && cell.Terrain != RoomChunkCellType.Gap)
                {
                    walkableCells.Add(CellKey(cell.X, cell.Y));
                }
            }

            var objectIds = new HashSet<string>();
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
                    }
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

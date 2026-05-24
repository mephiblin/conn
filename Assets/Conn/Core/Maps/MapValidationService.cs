using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
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
                var expectedKind = (MapPlacementKind)(int)profile.RequiredAnchors[i];
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
    }
}

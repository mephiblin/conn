using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
    public static class MapGenerationQualityService
    {
        public static MapValidationReport ValidateProductionShape(MapProfile profile, GeneratedMapDraft draft)
        {
            var report = new MapValidationReport();
            if (profile == null)
            {
                report.Errors.Add("Map generation quality validation requires a profile.");
                return report;
            }

            if (draft?.Graph == null)
            {
                report.Errors.Add("Map generation quality validation requires a generated graph.");
                return report;
            }

            ExpectNoDuplicateRoomCoordinates(draft.Graph, report);
            ExpectRequiredLayoutKinds(draft.Graph, report);
            ExpectBranchCoverage(profile, draft.Graph, report);
            ExpectLoopCoverage(profile, draft.Graph, report);
            return report;
        }

        private static void ExpectNoDuplicateRoomCoordinates(RoomGraph graph, MapValidationReport report)
        {
            var occupied = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                var key = $"{node.GridX},{node.GridY}";
                if (occupied.TryGetValue(key, out var previousRoomId))
                {
                    report.Errors.Add($"Generated room {node.Id} overlaps room {previousRoomId} at graph coordinate ({node.GridX}, {node.GridY}).");
                    continue;
                }

                occupied.Add(key, node.Id ?? string.Empty);
            }
        }

        private static void ExpectRequiredLayoutKinds(RoomGraph graph, MapValidationReport report)
        {
            if (!HasLayoutKind(graph, RoomChunkLayoutKind.Hub))
            {
                report.Errors.Add("Generated map has no hub room.");
            }

            if (!HasLayoutKind(graph, RoomChunkLayoutKind.Corridor))
            {
                report.Errors.Add("Generated map has no corridor room.");
            }

            if (!HasLayoutKind(graph, RoomChunkLayoutKind.DeadEnd))
            {
                report.Errors.Add("Generated map has no dead-end room.");
            }

            if (!HasLayoutKind(graph, RoomChunkLayoutKind.HeightTransition))
            {
                report.Errors.Add("Generated map has no height-transition room.");
            }
        }

        private static void ExpectBranchCoverage(MapProfile profile, RoomGraph graph, MapValidationReport report)
        {
            var sideBranchRooms = 0;
            var northBranches = 0;
            var southBranches = 0;
            var criticalPathY = FindCriticalPathY(graph);
            foreach (var node in graph.Nodes)
            {
                if (node.Role != MapRoomRole.SideBranch)
                {
                    continue;
                }

                sideBranchRooms++;
                if (node.GridY > criticalPathY)
                {
                    northBranches++;
                }
                else if (node.GridY < criticalPathY)
                {
                    southBranches++;
                }
            }

            var expectedBranchRooms = Math.Max(0, profile.SideBranchCount) * 2;
            if (sideBranchRooms < expectedBranchRooms)
            {
                report.Errors.Add($"Generated map has {sideBranchRooms} side-branch rooms, expected at least {expectedBranchRooms}.");
            }

            if (profile.SideBranchCount >= 2 && (northBranches == 0 || southBranches == 0))
            {
                report.Errors.Add("Generated side branches do not cover both north and south of the main path.");
            }
        }

        private static void ExpectLoopCoverage(MapProfile profile, RoomGraph graph, MapValidationReport report)
        {
            var loopEdges = 0;
            foreach (var edge in graph.Edges)
            {
                if (string.Equals(edge.Kind, "merge", StringComparison.Ordinal))
                {
                    loopEdges++;
                }
            }

            if (loopEdges < Math.Max(0, profile.LoopMin))
            {
                report.Errors.Add($"Generated map has {loopEdges} loop edge(s), expected at least {profile.LoopMin}.");
            }
        }

        private static int FindCriticalPathY(RoomGraph graph)
        {
            foreach (var node in graph.Nodes)
            {
                if (node.PathIndex == 0)
                {
                    return node.GridY;
                }
            }

            return 0;
        }

        private static bool HasLayoutKind(RoomGraph graph, RoomChunkLayoutKind layoutKind)
        {
            foreach (var node in graph.Nodes)
            {
                if (node.LayoutKind == layoutKind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

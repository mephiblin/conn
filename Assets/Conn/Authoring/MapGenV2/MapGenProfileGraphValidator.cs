using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public static class MapGenProfileGraphValidator
    {
        public static MapGenValidationReport Validate(MapGenProfileAsset profile)
        {
            var report = new MapGenValidationReport();
            if (profile == null || profile.LayoutRules == null || profile.StyleSet == null)
            {
                return report;
            }

            var quantity = profile.LayoutRules.QuantityRules;
            var requiredCategories = MapGenRequiredLandmarkReservation.GetRequiredCategories(profile);
            var roomTemplates = profile.StyleSet.RoomTemplates ?? Array.Empty<MapGenRoomTemplateAsset>();
            var corridorTemplates = profile.StyleSet.CorridorTemplates ?? Array.Empty<MapGenCorridorTemplateAsset>();

            ValidateRequiredPools(report, profile, quantity, requiredCategories, roomTemplates);
            ValidateImpossibleRanges(report, profile, quantity);
            ValidateConnectorCompatibility(report, roomTemplates, corridorTemplates);
            return report;
        }

        private static void ValidateRequiredPools(
            MapGenValidationReport report,
            MapGenProfileAsset profile,
            MapGenQuantityRules quantity,
            MapGenRoomCategory[] requiredCategories,
            MapGenRoomTemplateAsset[] roomTemplates)
        {
            if (requiredCategories == null || requiredCategories.Length == 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_missing_required_categories",
                    "Rule set has no required room categories.",
                    "Add at least Start and Exit to required categories.",
                    contextPath: "LayoutRules.QuantityRules.RequiredCategories"));
                return;
            }

            if (quantity.MaxRooms < requiredCategories.Length)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_required_categories_exceed_max_rooms",
                    $"Required room category count {requiredCategories.Length} exceeds MaxRooms {quantity.MaxRooms}.",
                    "Raise MaxRooms or remove required categories.",
                    contextPath: "LayoutRules.QuantityRules.MaxRooms"));
            }

            if (CountNonNull(roomTemplates) == 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_missing_room_template_pool",
                    "Style set has no room templates for required room categories.",
                    "Add room templates for required categories before using the production solver.",
                    severity: MapGenIssueSeverity.Warning,
                    contextPath: "StyleSet.RoomTemplates"));
                return;
            }

            foreach (var category in requiredCategories)
            {
                if (CountCategoryCandidateCells(profile.MapSize.x, profile.MapSize.y, roomTemplates, category) > 0)
                {
                    continue;
                }

                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_required_category_has_no_template",
                    $"Required room category {category} has no room template that fits the map.",
                    $"Add a {category} room template that fits the profile map size, or add a Main fallback template.",
                    contextPath: $"StyleSet.RoomTemplates/{category}"));
            }
        }

        private static void ValidateImpossibleRanges(
            MapGenValidationReport report,
            MapGenProfileAsset profile,
            MapGenQuantityRules quantity)
        {
            var cellCount = Mathf.Max(0, profile.MapSize.x) * Mathf.Max(0, profile.MapSize.y);
            if (quantity.MinRooms > cellCount)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_min_rooms_exceeds_map_cells",
                    $"MinRooms {quantity.MinRooms} exceeds available map cells {cellCount}.",
                    "Lower MinRooms or increase the map size.",
                    contextPath: "LayoutRules.QuantityRules.MinRooms"));
            }

            if (quantity.MinCorridorCells > cellCount)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_min_corridors_exceeds_map_cells",
                    $"MinCorridorCells {quantity.MinCorridorCells} exceeds available map cells {cellCount}.",
                    "Lower MinCorridorCells or increase the map size.",
                    contextPath: "LayoutRules.QuantityRules.MinCorridorCells"));
            }

            var maxManhattanDistance = Mathf.Max(0, profile.MapSize.x - 1) + Mathf.Max(0, profile.MapSize.y - 1);
            if (profile.LayoutRules.DistanceRules.MinStartToExitDistance > maxManhattanDistance)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_start_exit_distance_impossible",
                    $"Start-to-exit distance {profile.LayoutRules.DistanceRules.MinStartToExitDistance} exceeds map maximum {maxManhattanDistance}.",
                    "Lower MinStartToExitDistance or increase the map size.",
                    contextPath: "LayoutRules.DistanceRules.MinStartToExitDistance"));
            }
        }

        private static void ValidateConnectorCompatibility(
            MapGenValidationReport report,
            MapGenRoomTemplateAsset[] roomTemplates,
            MapGenCorridorTemplateAsset[] corridorTemplates)
        {
            var hasRoomConnector = false;
            var hasCompatibleConnector = false;
            foreach (var room in roomTemplates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                if (room == null)
                {
                    continue;
                }

                foreach (var roomConnector in room.Connectors ?? Array.Empty<MapGenConnector>())
                {
                    hasRoomConnector = true;
                    if (HasCompatibleCorridorConnector(roomConnector, corridorTemplates))
                    {
                        hasCompatibleConnector = true;
                        break;
                    }
                }

                if (hasCompatibleConnector)
                {
                    break;
                }
            }

            if (hasRoomConnector && (corridorTemplates == null || corridorTemplates.Length == 0))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_missing_corridor_templates_for_connectors",
                    "Room templates define connectors but the style set has no corridor templates.",
                    "Add corridor templates with compatible opposite connectors.",
                    contextPath: "StyleSet.CorridorTemplates"));
                return;
            }

            if (hasRoomConnector && !hasCompatibleConnector)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_graph_no_compatible_connector_matrix",
                    "No room connector can connect to any corridor connector.",
                    "Match corridor connector side, socket kind, socket id, and width to at least one room connector.",
                    contextPath: "StyleSet.RoomTemplates/StyleSet.CorridorTemplates"));
            }
        }

        private static bool HasCompatibleCorridorConnector(
            MapGenConnector roomConnector,
            MapGenCorridorTemplateAsset[] corridorTemplates)
        {
            foreach (var corridor in corridorTemplates ?? Array.Empty<MapGenCorridorTemplateAsset>())
            {
                if (corridor == null)
                {
                    continue;
                }

                foreach (var corridorConnector in corridor.Connectors ?? Array.Empty<MapGenConnector>())
                {
                    if (MapGenTemplateValidationUtility.AreCompatible(roomConnector, corridorConnector))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CountCategoryCandidateCells(
            int width,
            int height,
            MapGenRoomTemplateAsset[] templates,
            MapGenRoomCategory category)
        {
            var exactCount = 0;
            var fallbackCount = 0;
            foreach (var template in templates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                if (template == null || template.Footprint.x <= 0 || template.Footprint.y <= 0)
                {
                    continue;
                }

                var candidateCount = Mathf.Max(0, width - template.Footprint.x + 1)
                    * Mathf.Max(0, height - template.Footprint.y + 1);
                if (template.RoomCategory == category)
                {
                    exactCount += candidateCount;
                }
                else if (template.RoomCategory == MapGenRoomCategory.Main)
                {
                    fallbackCount += candidateCount;
                }
            }

            return exactCount > 0 ? exactCount : fallbackCount;
        }

        private static int CountNonNull<T>(T[] values) where T : class
        {
            var count = 0;
            foreach (var value in values ?? Array.Empty<T>())
            {
                if (value != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

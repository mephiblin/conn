using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Rule Set", fileName = "MapGenRuleSet")]
    public sealed class MapGenRuleSetAsset : ScriptableObject
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public MapGenRoomCategory[] RequiredRoomCategories =
        {
            MapGenRoomCategory.Start,
            MapGenRoomCategory.Exit
        };

        public MapGenRoomCategory[] OptionalRoomCategories = Array.Empty<MapGenRoomCategory>();
        public int MinRooms = 4;
        public int MaxRooms = 12;
        public int MinCorridorCells = 4;
        public int MaxCorridorCells = 64;
        public int LoopRate;
        public bool ReduceDeadEnds;
        public bool UseDirectRoutes;
        public bool SplitLargeRooms;
        public bool RemoveSmallRooms;
        public MapGenQuantityRules QuantityRules = MapGenQuantityRules.Defaults();
        public MapGenDistanceRules DistanceRules = MapGenDistanceRules.Defaults();
        public MapGenPostProcessRules PostProcessRules = MapGenPostProcessRules.Defaults();
        public MapGenPropPlacementRules[] PropPlacementRules = Array.Empty<MapGenPropPlacementRules>();

        public MapGenValidationReport Validate()
        {
            var report = new MapGenValidationReport();
            SyncStructuredRulesFromLegacyFields();
            ValidateQuantityRules(report);
            ValidateDistanceRules(report);
            ValidateTopologyRules(report);
            ValidatePostProcessRules(report);
            ValidatePropPlacementRules(report);
            return report;
        }

        private void OnValidate()
        {
            MinRooms = Mathf.Max(1, MinRooms);
            MaxRooms = Mathf.Max(MinRooms, MaxRooms);
            MinCorridorCells = Mathf.Max(0, MinCorridorCells);
            MaxCorridorCells = Mathf.Max(MinCorridorCells, MaxCorridorCells);
            LoopRate = Mathf.Max(0, LoopRate);
            SyncStructuredRulesFromLegacyFields();
        }

        private void ValidateQuantityRules(MapGenValidationReport report)
        {
            if (QuantityRules.MinRooms < 1)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_min_rooms",
                    "Minimum room count must be at least 1.",
                    "Set MinRooms to 1 or higher."));
            }

            if (QuantityRules.MaxRooms < QuantityRules.MinRooms)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_room_range",
                    "Maximum room count cannot be lower than minimum room count.",
                    "Set MaxRooms greater than or equal to MinRooms."));
            }

            if (QuantityRules.MinCorridorCells < 0 || QuantityRules.MaxCorridorCells < QuantityRules.MinCorridorCells)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_corridor_range",
                    "Corridor cell range is invalid.",
                    "Set corridor min/max values to a valid non-negative range."));
            }

            if (QuantityRules.TargetRoomDensityPercent < 0 || QuantityRules.TargetRoomDensityPercent > 100)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_room_density",
                    "Target room density must be between 0 and 100.",
                    "Set TargetRoomDensityPercent within 0..100."));
            }

            if (QuantityRules.TargetCorridorDensityPercent < 0 || QuantityRules.TargetCorridorDensityPercent > 100)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_corridor_density",
                    "Target corridor density must be between 0 and 100.",
                    "Set TargetCorridorDensityPercent within 0..100."));
            }
        }

        private void ValidateDistanceRules(MapGenValidationReport report)
        {
            if (DistanceRules.MinStartToExitDistance < 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_start_exit_distance",
                    "Start-to-exit minimum distance cannot be negative.",
                    "Set MinStartToExitDistance to zero or higher."));
            }
        }

        private void ValidatePostProcessRules(MapGenValidationReport report)
        {
            if (PostProcessRules.MaxPasses < 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_post_process_passes",
                    "Post-process max passes cannot be negative.",
                    "Set MaxPasses to zero or higher."));
            }
        }

        private void ValidateTopologyRules(MapGenValidationReport report)
        {
            if (LoopRate < 0 || LoopRate > 100)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_loop_rate",
                    "Loop rate must be between 0 and 100.",
                    "Set LoopRate within 0..100."));
            }
        }

        private void ValidatePropPlacementRules(MapGenValidationReport report)
        {
            for (var i = 0; i < (PropPlacementRules?.Length ?? 0); i++)
            {
                var rule = PropPlacementRules[i];
                if (string.IsNullOrWhiteSpace(rule.Channel))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "rule_set_prop_rule_missing_channel",
                        $"Prop placement rule {i} has no channel.",
                        "Assign a prop placement channel id."));
                }

                if (rule.DensityPercent < 0 || rule.DensityPercent > 100)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "rule_set_prop_rule_invalid_density",
                        $"Prop placement rule {i} has invalid density.",
                        "Set DensityPercent within 0..100."));
                }

                if (rule.MinSpacingCells < 0)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "rule_set_prop_rule_invalid_spacing",
                        $"Prop placement rule {i} has invalid spacing.",
                        "Set MinSpacingCells to zero or higher."));
                }

                if (rule.RequiredUnique || rule.DistributionMode == MapGenPropDistributionMode.RequiredUnique)
                {
                    var hasPrefabHint = !string.IsNullOrWhiteSpace(rule.Channel);
                    if (!hasPrefabHint)
                    {
                        report.Add(new MapGenIssue(
                            MapGenGenerationPhase.ValidateProfile,
                            "rule_set_prop_rule_required_unique_missing_channel",
                            $"Required unique prop placement rule {i} has no channel.",
                            "Assign a stable channel id so the unique prop can be tracked."));
                    }
                }
            }
        }

        private void SyncStructuredRulesFromLegacyFields()
        {
            QuantityRules.MinRooms = MinRooms;
            QuantityRules.MaxRooms = MaxRooms;
            QuantityRules.MinCorridorCells = MinCorridorCells;
            QuantityRules.MaxCorridorCells = MaxCorridorCells;
            QuantityRules.RequiredCategories = RequiredRoomCategories ?? Array.Empty<MapGenRoomCategory>();
            QuantityRules.OptionalCategories = OptionalRoomCategories ?? Array.Empty<MapGenRoomCategory>();
            PostProcessRules.UseDirectRoutes = UseDirectRoutes;
            PostProcessRules.ReduceDeadEnds = ReduceDeadEnds;
            PostProcessRules.SplitLargeRooms = SplitLargeRooms;
            PostProcessRules.RemoveSmallRooms = RemoveSmallRooms;
        }
    }
}

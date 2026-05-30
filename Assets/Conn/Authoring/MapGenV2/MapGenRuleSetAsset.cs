using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Rule Set", fileName = "MapGenRuleSet")]
    public sealed class MapGenRuleSetAsset : ScriptableObject
    {
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

        public MapGenValidationReport Validate()
        {
            var report = new MapGenValidationReport();
            if (MinRooms < 1)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_min_rooms",
                    "Minimum room count must be at least 1.",
                    "Set MinRooms to 1 or higher."));
            }

            if (MaxRooms < MinRooms)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_room_range",
                    "Maximum room count cannot be lower than minimum room count.",
                    "Set MaxRooms greater than or equal to MinRooms."));
            }

            if (MinCorridorCells < 0 || MaxCorridorCells < MinCorridorCells)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "rule_set_invalid_corridor_range",
                    "Corridor cell range is invalid.",
                    "Set corridor min/max values to a valid non-negative range."));
            }

            return report;
        }

        private void OnValidate()
        {
            MinRooms = Mathf.Max(1, MinRooms);
            MaxRooms = Mathf.Max(MinRooms, MaxRooms);
            MinCorridorCells = Mathf.Max(0, MinCorridorCells);
            MaxCorridorCells = Mathf.Max(MinCorridorCells, MaxCorridorCells);
            LoopRate = Mathf.Max(0, LoopRate);
        }
    }
}

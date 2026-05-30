using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [Serializable]
    public struct MapGenQuantityRules
    {
        public int MinRooms;
        public int MaxRooms;
        public int MinCorridorCells;
        public int MaxCorridorCells;
        public int TargetRoomDensityPercent;
        public int TargetCorridorDensityPercent;
        public MapGenRoomCategory[] RequiredCategories;
        public MapGenRoomCategory[] OptionalCategories;

        public static MapGenQuantityRules Defaults()
        {
            return new MapGenQuantityRules
            {
                MinRooms = 4,
                MaxRooms = 12,
                MinCorridorCells = 4,
                MaxCorridorCells = 64,
                TargetRoomDensityPercent = 20,
                TargetCorridorDensityPercent = 15,
                RequiredCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit },
                OptionalCategories = Array.Empty<MapGenRoomCategory>()
            };
        }
    }

    [Serializable]
    public struct MapGenDistanceRules
    {
        public int MinStartToExitDistance;

        public static MapGenDistanceRules Defaults()
        {
            return new MapGenDistanceRules
            {
                MinStartToExitDistance = 0
            };
        }
    }

    [Serializable]
    public struct MapGenPostProcessRules
    {
        public bool UseDirectRoutes;
        public bool ReduceDeadEnds;
        public bool SplitLargeRooms;
        public bool RemoveSmallRooms;
        public bool FillEnclosedEmptySpace;
        public int MaxPasses;

        public static MapGenPostProcessRules Defaults()
        {
            return new MapGenPostProcessRules
            {
                UseDirectRoutes = false,
                ReduceDeadEnds = false,
                SplitLargeRooms = false,
                RemoveSmallRooms = false,
                FillEnclosedEmptySpace = false,
                MaxPasses = 1
            };
        }
    }

    [Serializable]
    public struct MapGenPropPlacementRules
    {
        public string Channel;
        public string[] RoomCategoryFilters;
        public string[] CorridorKindFilters;
        public int DensityPercent;
        public int MinSpacingCells;
        public bool AllowTraversalBlocking;

        public static MapGenPropPlacementRules Defaults()
        {
            return new MapGenPropPlacementRules
            {
                Channel = string.Empty,
                RoomCategoryFilters = Array.Empty<string>(),
                CorridorKindFilters = Array.Empty<string>(),
                DensityPercent = 0,
                MinSpacingCells = 1,
                AllowTraversalBlocking = false
            };
        }
    }
}

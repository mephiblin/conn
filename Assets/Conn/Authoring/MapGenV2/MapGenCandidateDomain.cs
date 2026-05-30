using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public sealed class MapGenCandidateDomain
    {
        public int Width;
        public int Height;
        public int CellCount;
        public int RoomTemplateCount;
        public int CorridorTemplateCount;
        public int RequiredCategoryCount;
        public int OptionalCategoryCount;
        public int MinRooms;
        public int MaxRooms;
        public int MinCorridorCells;
        public int MaxCorridorCells;
        public int TargetRoomDensityPercent;
        public int TargetCorridorDensityPercent;
        public int BlockedTemplateCellCount;
        public int RoomFootprintCandidateCells;
        public int CorridorCandidateCells;
    }

    public static class MapGenCandidateDomainBuilder
    {
        public static MapGenCandidateDomain Build(MapGenProfileAsset profile)
        {
            var width = profile != null ? Mathf.Max(0, profile.MapSize.x) : 0;
            var height = profile != null ? Mathf.Max(0, profile.MapSize.y) : 0;
            var ruleSet = profile != null ? profile.LayoutRules : null;
            var quantity = ruleSet != null ? ruleSet.QuantityRules : MapGenQuantityRules.Defaults();
            var roomTemplates = profile != null && profile.StyleSet != null
                ? profile.StyleSet.RoomTemplates ?? Array.Empty<MapGenRoomTemplateAsset>()
                : Array.Empty<MapGenRoomTemplateAsset>();
            var corridorTemplates = profile != null && profile.StyleSet != null
                ? profile.StyleSet.CorridorTemplates ?? Array.Empty<MapGenCorridorTemplateAsset>()
                : Array.Empty<MapGenCorridorTemplateAsset>();

            return new MapGenCandidateDomain
            {
                Width = width,
                Height = height,
                CellCount = width * height,
                RoomTemplateCount = CountNonNull(roomTemplates),
                CorridorTemplateCount = CountNonNull(corridorTemplates),
                RequiredCategoryCount = CountCategories(quantity.RequiredCategories),
                OptionalCategoryCount = CountCategories(quantity.OptionalCategories),
                MinRooms = Mathf.Max(0, quantity.MinRooms),
                MaxRooms = Mathf.Max(0, quantity.MaxRooms),
                MinCorridorCells = Mathf.Max(0, quantity.MinCorridorCells),
                MaxCorridorCells = Mathf.Max(0, quantity.MaxCorridorCells),
                TargetRoomDensityPercent = Mathf.Clamp(quantity.TargetRoomDensityPercent, 0, 100),
                TargetCorridorDensityPercent = Mathf.Clamp(quantity.TargetCorridorDensityPercent, 0, 100),
                BlockedTemplateCellCount = CountBlockedTemplateCells(roomTemplates),
                RoomFootprintCandidateCells = CountRoomFootprintCandidateCells(width, height, roomTemplates),
                CorridorCandidateCells = CountCorridorCandidateCells(width, height, corridorTemplates)
            };
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

        private static int CountCategories(MapGenRoomCategory[] categories)
        {
            return categories?.Length ?? 0;
        }

        private static int CountBlockedTemplateCells(MapGenRoomTemplateAsset[] templates)
        {
            var count = 0;
            foreach (var template in templates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                count += template != null ? template.BlockedCells?.Length ?? 0 : 0;
            }

            return count;
        }

        private static int CountRoomFootprintCandidateCells(int width, int height, MapGenRoomTemplateAsset[] templates)
        {
            var count = 0;
            foreach (var template in templates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                if (template == null || template.Footprint.x <= 0 || template.Footprint.y <= 0)
                {
                    continue;
                }

                count += Mathf.Max(0, width - template.Footprint.x + 1)
                    * Mathf.Max(0, height - template.Footprint.y + 1);
            }

            return count;
        }

        private static int CountCorridorCandidateCells(int width, int height, MapGenCorridorTemplateAsset[] templates)
        {
            if (templates == null || templates.Length == 0)
            {
                return width * height;
            }

            var count = 0;
            foreach (var template in templates)
            {
                if (template == null)
                {
                    continue;
                }

                count += Mathf.Max(0, width - Mathf.Max(1, template.LengthRange.x) + 1)
                    * Mathf.Max(0, height - Mathf.Max(1, template.Width) + 1);
            }

            return count;
        }
    }
}

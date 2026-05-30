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
        public int LowestEntropyLandmarkIndex;
        public MapGenRoomCategory LowestEntropyLandmarkCategory;
        public int LowestEntropyLandmarkCandidateCount;
        public MapGenLandmarkEntropy[] LandmarkEntropies = Array.Empty<MapGenLandmarkEntropy>();
    }

    public struct MapGenLandmarkEntropy
    {
        public int LandmarkIndex;
        public MapGenRoomCategory Category;
        public int CandidateCount;
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

            var landmarkEntropies = BuildLandmarkEntropies(width, height, quantity.RequiredCategories, roomTemplates);
            var lowestEntropyIndex = FindLowestEntropyIndex(landmarkEntropies);

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
                CorridorCandidateCells = CountCorridorCandidateCells(width, height, corridorTemplates),
                LowestEntropyLandmarkIndex = lowestEntropyIndex >= 0 ? landmarkEntropies[lowestEntropyIndex].LandmarkIndex : -1,
                LowestEntropyLandmarkCategory = lowestEntropyIndex >= 0
                    ? landmarkEntropies[lowestEntropyIndex].Category
                    : default,
                LowestEntropyLandmarkCandidateCount = lowestEntropyIndex >= 0
                    ? landmarkEntropies[lowestEntropyIndex].CandidateCount
                    : 0,
                LandmarkEntropies = landmarkEntropies
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

        private static MapGenLandmarkEntropy[] BuildLandmarkEntropies(
            int width,
            int height,
            MapGenRoomCategory[] categories,
            MapGenRoomTemplateAsset[] templates)
        {
            if (categories == null || categories.Length == 0)
            {
                return Array.Empty<MapGenLandmarkEntropy>();
            }

            var entropies = new MapGenLandmarkEntropy[categories.Length];
            for (var i = 0; i < categories.Length; i++)
            {
                entropies[i] = new MapGenLandmarkEntropy
                {
                    LandmarkIndex = i,
                    Category = categories[i],
                    CandidateCount = CountCategoryCandidateCells(width, height, templates, categories[i])
                };
            }

            return entropies;
        }

        private static int FindLowestEntropyIndex(MapGenLandmarkEntropy[] entropies)
        {
            var best = -1;
            for (var i = 0; i < (entropies?.Length ?? 0); i++)
            {
                if (best < 0 || entropies[i].CandidateCount < entropies[best].CandidateCount)
                {
                    best = i;
                }
            }

            return best;
        }

        private static int CountCategoryCandidateCells(
            int width,
            int height,
            MapGenRoomTemplateAsset[] templates,
            MapGenRoomCategory category)
        {
            var exactCount = 0;
            var mainCount = 0;
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
                    mainCount += candidateCount;
                }
            }

            return exactCount > 0 ? exactCount : mainCount;
        }
    }
}

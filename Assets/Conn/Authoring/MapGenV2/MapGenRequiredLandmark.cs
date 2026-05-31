using Conn.MapGenV2.Core;
using System;

namespace Conn.MapGenV2.Authoring
{
    public readonly struct MapGenRequiredLandmark
    {
        public readonly int Index;
        public readonly MapGenRoomCategory Category;
        public readonly string LandmarkId;

        public MapGenRequiredLandmark(int index, MapGenRoomCategory category)
        {
            Index = index;
            Category = category;
            LandmarkId = $"{index}_{category}";
        }
    }

    public static class MapGenRequiredLandmarkReservation
    {
        public static MapGenRequiredLandmark[] Build(MapGenProfileAsset profile)
        {
            var categories = GetRequiredCategories(profile);
            return Build(categories);
        }

        public static MapGenRequiredLandmark[] Build(MapGenMockupDraftAsset draft)
        {
            var categories = GetRequiredCategories(draft);
            return Build(categories);
        }

        private static MapGenRequiredLandmark[] Build(MapGenRoomCategory[] categories)
        {
            var landmarks = new MapGenRequiredLandmark[categories.Length];
            for (var i = 0; i < categories.Length; i++)
            {
                landmarks[i] = new MapGenRequiredLandmark(i, categories[i]);
            }

            return landmarks;
        }

        public static MapGenRoomCategory[] GetRequiredCategories(MapGenProfileAsset profile)
        {
            if (profile != null
                && profile.LayoutRules != null
                && profile.LayoutRules.QuantityRules.RequiredCategories != null
                && profile.LayoutRules.QuantityRules.RequiredCategories.Length > 0)
            {
                return profile.LayoutRules.QuantityRules.RequiredCategories;
            }

            if (profile != null
                && profile.LayoutRules != null
                && profile.LayoutRules.RequiredRoomCategories != null
                && profile.LayoutRules.RequiredRoomCategories.Length > 0)
            {
                return profile.LayoutRules.RequiredRoomCategories;
            }

            return new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
        }

        public static MapGenRoomCategory[] GetRequiredCategories(MapGenMockupDraftAsset draft)
        {
            return draft != null
                ? draft.GetRequiredRoomCategories()
                : new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
        }
    }
}

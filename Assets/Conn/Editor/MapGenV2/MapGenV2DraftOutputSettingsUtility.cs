using Conn.MapGenV2.Authoring;
using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    internal static class MapGenV2DraftOutputSettingsUtility
    {
        public static bool HasDraftOutputSettings(MapGenMockupDraftAsset draft)
        {
            return draft != null;
        }

        public static MapGenOutputSettings Get(MapGenMockupDraftAsset draft, MapGenProfileAsset fallbackProfile = null)
        {
            if (draft != null)
            {
                return Normalize(draft.GetOutputSettings());
            }

            var profile = fallbackProfile;
            return profile != null ? Normalize(profile.OutputSettings) : MapGenOutputSettings.Defaults();
        }

        public static void Set(
            MapGenMockupDraftAsset draft,
            MapGenProfileAsset fallbackProfile,
            MapGenOutputSettings outputSettings,
            string undoName)
        {
            outputSettings = Normalize(outputSettings);
            if (draft != null)
            {
                Undo.RecordObject(draft, undoName);
                draft.OutputSettings = outputSettings;
                EditorUtility.SetDirty(draft);
                return;
            }

            if (fallbackProfile == null)
            {
                return;
            }

            Undo.RecordObject(fallbackProfile, undoName);
            fallbackProfile.OutputSettings = outputSettings;
            EditorUtility.SetDirty(fallbackProfile);
        }

        public static string GetBakedAssetPath(MapGenMockupDraftAsset draft, MapGenProfileAsset fallbackProfile = null)
        {
            var settings = Get(draft, fallbackProfile);
            if (draft == null)
            {
                return settings.BakedAssetFolder;
            }

            var mapId = string.IsNullOrWhiteSpace(draft.GetMapId()) ? draft.name : draft.GetMapId();
            return $"{settings.BakedAssetFolder}/{mapId}_{draft.Seed}_BakedMap.asset";
        }

        private static MapGenOutputSettings Normalize(MapGenOutputSettings outputSettings)
        {
            var defaults = MapGenOutputSettings.Defaults();
            if (string.IsNullOrWhiteSpace(outputSettings.DraftFolder))
            {
                outputSettings.DraftFolder = defaults.DraftFolder;
            }

            if (string.IsNullOrWhiteSpace(outputSettings.MaterializedPrefabFolder))
            {
                outputSettings.MaterializedPrefabFolder = defaults.MaterializedPrefabFolder;
            }

            if (string.IsNullOrWhiteSpace(outputSettings.BakedAssetFolder))
            {
                outputSettings.BakedAssetFolder = defaults.BakedAssetFolder;
            }

            return outputSettings;
        }
    }
}

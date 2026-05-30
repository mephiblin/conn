using Conn.MapGenV2.Core;
using System;

namespace Conn.MapGenV2.Authoring
{
    [Serializable]
    public struct MapGenOutputSettings
    {
        public string DraftFolder;
        public string MaterializedPrefabFolder;
        public string BakedAssetFolder;
        public MapGenV2OutputOverwriteMode OverwriteMode;

        public static MapGenOutputSettings Defaults()
        {
            return new MapGenOutputSettings
            {
                DraftFolder = "Assets/Conn/Authoring/MapGenV2/Drafts",
                MaterializedPrefabFolder = "Assets/Conn/Authoring/MapGenV2/MaterializedPrefabs",
                BakedAssetFolder = "Assets/Conn/Core/MapGenV2/BakedMaps",
                OverwriteMode = MapGenV2OutputOverwriteMode.ReplacePrevious
            };
        }

        public void Validate(MapGenValidationReport report)
        {
            ValidateFolder(report, "output_settings_missing_draft_folder", DraftFolder, "draft");
            ValidateFolder(report, "output_settings_missing_materialized_folder", MaterializedPrefabFolder, "materialized prefab");
            ValidateFolder(report, "output_settings_missing_baked_folder", BakedAssetFolder, "baked asset");
        }

        private static void ValidateFolder(MapGenValidationReport report, string code, string folder, string label)
        {
            if (!string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            report.Add(new MapGenIssue(
                MapGenGenerationPhase.ValidateProfile,
                code,
                $"Output settings require a {label} folder.",
                $"Set the {label} output folder path."));
        }
    }

    public enum MapGenV2OutputOverwriteMode
    {
        CreateUnique,
        ReplacePrevious,
        UpdateSelected
    }
}

using Conn.MapGenV2.Core;
using System;
using UnityEngine;

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

    public struct MapGenV2AuthoringAssetMigrationReport
    {
        public int OriginalVersion;
        public int CurrentVersion;
        public bool IsValid;
        public bool WasMigrated;
        public string Message;
    }

    public static class MapGenV2AuthoringAssetMigration
    {
        public static MapGenV2AuthoringAssetMigrationReport MigrateInMemory(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return Invalid(0, 0, "Authoring asset is null.");
            }

            var originalVersion = GetVersion(asset);
            var currentVersion = GetCurrentVersion(asset);
            if (currentVersion <= 0)
            {
                return Invalid(originalVersion, currentVersion, $"{asset.name} is not a supported MapGenV2 authoring asset.");
            }

            Normalize(asset);

            if (originalVersion > currentVersion)
            {
                return Invalid(
                    originalVersion,
                    currentVersion,
                    $"{asset.name} version {originalVersion} is newer than supported authoring version {currentVersion}.");
            }

            if (originalVersion <= 0)
            {
                SetCurrentVersion(asset);
            }

            return new MapGenV2AuthoringAssetMigrationReport
            {
                OriginalVersion = originalVersion,
                CurrentVersion = currentVersion,
                IsValid = true,
                WasMigrated = originalVersion != GetVersion(asset),
                Message = originalVersion == GetVersion(asset)
                    ? $"{asset.name} is current."
                    : $"{asset.name} migrated from version {originalVersion} to {GetVersion(asset)}."
            };
        }

        private static int GetVersion(UnityEngine.Object asset)
        {
            return asset switch
            {
                MapGenProfileAsset profile => profile.Version,
                MapGenStyleSetAsset styleSet => styleSet.Version,
                MapGenRuleSetAsset ruleSet => ruleSet.Version,
                MapGenRoomShapeAsset roomShape => roomShape.Version,
                MapGenRoomTemplateAsset roomTemplate => roomTemplate.Version,
                MapGenCorridorTemplateAsset corridorTemplate => corridorTemplate.Version,
                MapGenModuleSetAsset moduleSet => moduleSet.Version,
                MapGenMockupDraftAsset draft => draft.Version,
                _ => 0
            };
        }

        private static int GetCurrentVersion(UnityEngine.Object asset)
        {
            return asset switch
            {
                MapGenProfileAsset => MapGenProfileAsset.CurrentVersion,
                MapGenStyleSetAsset => MapGenStyleSetAsset.CurrentVersion,
                MapGenRuleSetAsset => MapGenRuleSetAsset.CurrentVersion,
                MapGenRoomShapeAsset => MapGenRoomShapeAsset.CurrentVersion,
                MapGenRoomTemplateAsset => MapGenRoomTemplateAsset.CurrentVersion,
                MapGenCorridorTemplateAsset => MapGenCorridorTemplateAsset.CurrentVersion,
                MapGenModuleSetAsset => MapGenModuleSetAsset.CurrentVersion,
                MapGenMockupDraftAsset => MapGenMockupDraftAsset.CurrentVersion,
                _ => 0
            };
        }

        private static void SetCurrentVersion(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case MapGenProfileAsset profile:
                    profile.Version = MapGenProfileAsset.CurrentVersion;
                    break;
                case MapGenStyleSetAsset styleSet:
                    styleSet.Version = MapGenStyleSetAsset.CurrentVersion;
                    break;
                case MapGenRuleSetAsset ruleSet:
                    ruleSet.Version = MapGenRuleSetAsset.CurrentVersion;
                    break;
                case MapGenRoomShapeAsset roomShape:
                    roomShape.Version = MapGenRoomShapeAsset.CurrentVersion;
                    break;
                case MapGenRoomTemplateAsset roomTemplate:
                    roomTemplate.Version = MapGenRoomTemplateAsset.CurrentVersion;
                    break;
                case MapGenCorridorTemplateAsset corridorTemplate:
                    corridorTemplate.Version = MapGenCorridorTemplateAsset.CurrentVersion;
                    break;
                case MapGenModuleSetAsset moduleSet:
                    moduleSet.Version = MapGenModuleSetAsset.CurrentVersion;
                    break;
                case MapGenMockupDraftAsset draft:
                    draft.Version = MapGenMockupDraftAsset.CurrentVersion;
                    break;
            }
        }

        private static void Normalize(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case MapGenProfileAsset profile:
                    profile.RoomShapes ??= Array.Empty<MapGenRoomShapeAsset>();
                    NormalizeOutputSettings(ref profile.OutputSettings);
                    break;
                case MapGenStyleSetAsset styleSet:
                    styleSet.RoomShapePool ??= Array.Empty<MapGenRoomShapeAsset>();
                    styleSet.RoomTemplates ??= Array.Empty<MapGenRoomTemplateAsset>();
                    styleSet.CorridorTemplates ??= Array.Empty<MapGenCorridorTemplateAsset>();
                    break;
                case MapGenRuleSetAsset ruleSet:
                    ruleSet.RequiredRoomCategories ??= new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                    ruleSet.OptionalRoomCategories ??= Array.Empty<MapGenRoomCategory>();
                    ruleSet.PropPlacementRules ??= Array.Empty<MapGenPropPlacementRules>();
                    break;
                case MapGenRoomShapeAsset roomShape:
                    roomShape.Dimensions = new Vector2Int(Mathf.Max(1, roomShape.Dimensions.x), Mathf.Max(1, roomShape.Dimensions.y));
                    roomShape.Cells ??= Array.Empty<MapGenShapeCell>();
                    roomShape.Weight = Mathf.Max(1, roomShape.Weight);
                    break;
                case MapGenRoomTemplateAsset roomTemplate:
                    roomTemplate.Footprint = new Vector2Int(Mathf.Max(1, roomTemplate.Footprint.x), Mathf.Max(1, roomTemplate.Footprint.y));
                    roomTemplate.SourceRoomShapes ??= Array.Empty<MapGenRoomShapeAsset>();
                    roomTemplate.Connectors ??= Array.Empty<MapGenConnector>();
                    roomTemplate.FloorCells ??= Array.Empty<Vector2Int>();
                    roomTemplate.WallCells ??= Array.Empty<Vector2Int>();
                    roomTemplate.BlockedCells ??= Array.Empty<Vector2Int>();
                    roomTemplate.DoorHintCells ??= Array.Empty<Vector2Int>();
                    roomTemplate.PropChannels ??= Array.Empty<MapGenTemplatePropChannel>();
                    roomTemplate.Weight = Mathf.Max(1, roomTemplate.Weight);
                    break;
                case MapGenCorridorTemplateAsset corridorTemplate:
                    corridorTemplate.Width = Mathf.Max(1, corridorTemplate.Width);
                    corridorTemplate.LengthRange = new Vector2Int(
                        Mathf.Max(1, corridorTemplate.LengthRange.x),
                        Mathf.Max(Mathf.Max(1, corridorTemplate.LengthRange.x), corridorTemplate.LengthRange.y));
                    corridorTemplate.Connectors ??= Array.Empty<MapGenConnector>();
                    corridorTemplate.PropChannels ??= Array.Empty<MapGenTemplatePropChannel>();
                    corridorTemplate.Weight = Mathf.Max(1, corridorTemplate.Weight);
                    break;
                case MapGenModuleSetAsset moduleSet:
                    moduleSet.BoundsContract ??= new MapGenModuleBoundsContract();
                    moduleSet.FloorsA ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.FloorsB ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.WallsStraight ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.WallsCornerInside ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.WallsCornerOutside ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.ExteriorCeilings ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.InteriorCeilings ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.WholeDoors ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.HalfDoorFrames ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.HalfDoorPanels ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.PropCategories ??= Array.Empty<MapGenModuleEntry>();
                    moduleSet.RequiredUniqueProps ??= Array.Empty<MapGenModuleEntry>();
                    break;
                case MapGenMockupDraftAsset draft:
                    draft.GridSize = new Vector2Int(Mathf.Max(1, draft.GridSize.x), Mathf.Max(1, draft.GridSize.y));
                    draft.Cells ??= Array.Empty<MapGenMockupCell>();
                    draft.RegionOverrides ??= Array.Empty<MapGenMockupRegionOverride>();
                    break;
            }
        }

        private static void NormalizeOutputSettings(ref MapGenOutputSettings outputSettings)
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
        }

        private static MapGenV2AuthoringAssetMigrationReport Invalid(int originalVersion, int currentVersion, string message)
        {
            return new MapGenV2AuthoringAssetMigrationReport
            {
                OriginalVersion = originalVersion,
                CurrentVersion = currentVersion,
                IsValid = false,
                WasMigrated = false,
                Message = message
            };
        }
    }
}

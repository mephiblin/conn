using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Module Set", fileName = "MapGenModuleSet")]
    public sealed class MapGenModuleSetAsset : ScriptableObject
    {
        public string ModuleSetId = string.Empty;
        public MapGenModuleEntry[] FloorsA = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] FloorsB = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] WallsStraight = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] WallsCornerInside = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] WallsCornerOutside = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] ExteriorCeilings = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] InteriorCeilings = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] WholeDoors = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] HalfDoorFrames = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] HalfDoorPanels = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] PropCategories = Array.Empty<MapGenModuleEntry>();
        public MapGenModuleEntry[] RequiredUniqueProps = Array.Empty<MapGenModuleEntry>();

        public MapGenValidationReport Validate()
        {
            var report = new MapGenValidationReport();
            ValidateCategory(report, nameof(FloorsA), FloorsA, true);
            ValidateCategory(report, nameof(FloorsB), FloorsB, false);
            ValidateCategory(report, nameof(WallsStraight), WallsStraight, true);
            ValidateCategory(report, nameof(WallsCornerInside), WallsCornerInside, false);
            ValidateCategory(report, nameof(WallsCornerOutside), WallsCornerOutside, false);
            ValidateCategory(report, nameof(ExteriorCeilings), ExteriorCeilings, false);
            ValidateCategory(report, nameof(InteriorCeilings), InteriorCeilings, false);
            ValidateCategory(report, nameof(WholeDoors), WholeDoors, false);
            ValidateCategory(report, nameof(HalfDoorFrames), HalfDoorFrames, false);
            ValidateCategory(report, nameof(HalfDoorPanels), HalfDoorPanels, false);
            ValidateCategory(report, nameof(PropCategories), PropCategories, false);
            ValidateCategory(report, nameof(RequiredUniqueProps), RequiredUniqueProps, false);
            return report;
        }

        private void OnValidate()
        {
            ClampEntries(FloorsA);
            ClampEntries(FloorsB);
            ClampEntries(WallsStraight);
            ClampEntries(WallsCornerInside);
            ClampEntries(WallsCornerOutside);
            ClampEntries(ExteriorCeilings);
            ClampEntries(InteriorCeilings);
            ClampEntries(WholeDoors);
            ClampEntries(HalfDoorFrames);
            ClampEntries(HalfDoorPanels);
            ClampEntries(PropCategories);
            ClampEntries(RequiredUniqueProps);
        }

        private static void ValidateCategory(
            MapGenValidationReport report,
            string fieldName,
            MapGenModuleEntry[] entries,
            bool required)
        {
            if (required && (entries == null || entries.Length == 0))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_missing_required_category",
                    $"Module set requires at least one prefab for {fieldName}.",
                    $"Add a prefab entry to {fieldName}."));
                return;
            }

            for (var i = 0; i < (entries?.Length ?? 0); i++)
            {
                ValidateEntry(report, fieldName, i, entries[i]);
            }
        }

        private static void ValidateEntry(
            MapGenValidationReport report,
            string fieldName,
            int index,
            MapGenModuleEntry entry)
        {
            if (entry == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_null_entry",
                    $"{fieldName}[{index}] is empty.",
                    "Remove the empty entry or assign a prefab."));
                return;
            }

            if (entry.Prefab == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_missing_prefab",
                    $"{fieldName}[{index}] has no prefab.",
                    "Assign a prefab to the module entry."));
            }

            if (entry.Weight <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_invalid_weight",
                    $"{fieldName}[{index}] has invalid weight.",
                    "Set weight to at least 1."));
            }

            if (entry.Footprint.x <= 0 || entry.Footprint.y <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_invalid_footprint",
                    $"{fieldName}[{index}] has invalid grid footprint.",
                    "Set footprint X and Y to at least 1."));
            }
        }

        private static void ClampEntries(MapGenModuleEntry[] entries)
        {
            foreach (var entry in entries ?? Array.Empty<MapGenModuleEntry>())
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.Weight < 1)
                {
                    entry.Weight = 1;
                }

                entry.Footprint = new Vector2Int(Mathf.Max(1, entry.Footprint.x), Mathf.Max(1, entry.Footprint.y));
            }
        }
    }
}

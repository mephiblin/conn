using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Module Set", fileName = "MapGenModuleSet")]
    public sealed class MapGenModuleSetAsset : ScriptableObject
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string ModuleSetId = string.Empty;
        public MapGenModuleBoundsContract BoundsContract = new MapGenModuleBoundsContract();
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
            ValidateBoundsContract(report, BoundsContract);
            ValidateCategory(report, nameof(FloorsA), FloorsA, true, BoundsContract);
            ValidateCategory(report, nameof(FloorsB), FloorsB, false, BoundsContract);
            ValidateCategory(report, nameof(WallsStraight), WallsStraight, true, BoundsContract);
            ValidateCategory(report, nameof(WallsCornerInside), WallsCornerInside, false, BoundsContract);
            ValidateCategory(report, nameof(WallsCornerOutside), WallsCornerOutside, false, BoundsContract);
            ValidateCategory(report, nameof(ExteriorCeilings), ExteriorCeilings, false, BoundsContract);
            ValidateCategory(report, nameof(InteriorCeilings), InteriorCeilings, false, BoundsContract);
            ValidateCategory(report, nameof(WholeDoors), WholeDoors, false, BoundsContract);
            ValidateCategory(report, nameof(HalfDoorFrames), HalfDoorFrames, false, BoundsContract);
            ValidateCategory(report, nameof(HalfDoorPanels), HalfDoorPanels, false, BoundsContract);
            ValidateCategory(report, nameof(PropCategories), PropCategories, false, BoundsContract);
            ValidateCategory(report, nameof(RequiredUniqueProps), RequiredUniqueProps, false, BoundsContract);
            return report;
        }

        public MapGenModuleEntry[] GetEntries(MapGenModuleCategory category)
        {
            switch (category)
            {
                case MapGenModuleCategory.FloorA:
                    return FloorsA;
                case MapGenModuleCategory.FloorB:
                    return FloorsB;
                case MapGenModuleCategory.WallStraight:
                    return WallsStraight;
                case MapGenModuleCategory.WallCornerInside:
                    return WallsCornerInside;
                case MapGenModuleCategory.WallCornerOutside:
                    return WallsCornerOutside;
                case MapGenModuleCategory.CeilingInterior:
                    return InteriorCeilings;
                case MapGenModuleCategory.CeilingExterior:
                    return ExteriorCeilings;
                case MapGenModuleCategory.DoorWhole:
                    return WholeDoors;
                case MapGenModuleCategory.DoorFrameHalf:
                    return HalfDoorFrames;
                case MapGenModuleCategory.DoorPanelHalf:
                    return HalfDoorPanels;
                case MapGenModuleCategory.NavigationHelper:
                    return Array.Empty<MapGenModuleEntry>();
                case MapGenModuleCategory.Prop:
                    return PropCategories;
                default:
                    return Array.Empty<MapGenModuleEntry>();
            }
        }

        private void OnValidate()
        {
            ClampBoundsContract();
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
            bool required,
            MapGenModuleBoundsContract boundsContract)
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
                ValidateEntry(report, fieldName, i, entries[i], boundsContract);
            }
        }

        private static void ValidateBoundsContract(
            MapGenValidationReport report,
            MapGenModuleBoundsContract boundsContract)
        {
            if (boundsContract == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_missing_bounds_contract",
                    "Module set has no module bounds contract.",
                    "Create a bounds contract so prefabs can be checked before materialization."));
                return;
            }

            if (boundsContract.CellSize <= 0f || boundsContract.Height <= 0f || boundsContract.PivotTolerance < 0f)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_invalid_bounds_contract",
                    "Module bounds contract contains invalid dimensions or tolerance.",
                    "Use positive cell size and height, and a non-negative pivot tolerance."));
            }
        }

        private static void ValidateEntry(
            MapGenValidationReport report,
            string fieldName,
            int index,
            MapGenModuleEntry entry,
            MapGenModuleBoundsContract boundsContract)
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

            ValidatePrefabRoot(report, fieldName, index, entry, boundsContract);
        }

        private static void ValidatePrefabRoot(
            MapGenValidationReport report,
            string fieldName,
            int index,
            MapGenModuleEntry entry,
            MapGenModuleBoundsContract boundsContract)
        {
            if (boundsContract == null || !boundsContract.Enabled || entry?.Prefab == null)
            {
                return;
            }

            var transform = entry.Prefab.transform;
            var tolerance = Mathf.Max(0f, boundsContract.PivotTolerance);
            if (!Approximately(transform.localPosition, Vector3.zero, tolerance))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_prefab_pivot_offset",
                    $"{fieldName}[{index}] prefab root is offset from the declared {boundsContract.PivotMode} pivot.",
                    "Keep the prefab root at the declared pivot and move visual geometry into child objects if needed."));
            }

            if (boundsContract.RequireIdentityRotation
                && Quaternion.Angle(transform.localRotation, Quaternion.identity) > Mathf.Max(0.001f, tolerance))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_prefab_root_rotation",
                    $"{fieldName}[{index}] prefab root has a non-identity rotation.",
                    "Reset the prefab root rotation and use the module rotation policy for stamping."));
            }

            if (boundsContract.RequireUnitScale && !Approximately(transform.localScale, Vector3.one, tolerance))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "module_set_prefab_root_scale",
                    $"{fieldName}[{index}] prefab root has a non-unit scale.",
                    "Apply scale to child meshes or import settings so the prefab root stays at unit scale."));
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

        private void ClampBoundsContract()
        {
            BoundsContract ??= new MapGenModuleBoundsContract();
            BoundsContract.CellSize = Mathf.Max(0.01f, BoundsContract.CellSize);
            BoundsContract.Height = Mathf.Max(0.01f, BoundsContract.Height);
            BoundsContract.PivotTolerance = Mathf.Max(0f, BoundsContract.PivotTolerance);
        }

        private static bool Approximately(Vector3 actual, Vector3 expected, float tolerance)
        {
            return Mathf.Abs(actual.x - expected.x) <= tolerance
                && Mathf.Abs(actual.y - expected.y) <= tolerance
                && Mathf.Abs(actual.z - expected.z) <= tolerance;
        }
    }

    public enum MapGenModulePivotMode
    {
        CellOrigin,
        CellCenterBottom
    }

    [Serializable]
    public sealed class MapGenModuleBoundsContract
    {
        public bool Enabled = true;
        public float CellSize = 1f;
        public float Height = 3f;
        public MapGenModulePivotMode PivotMode = MapGenModulePivotMode.CellOrigin;
        public float PivotTolerance = 0.001f;
        public bool RequireIdentityRotation = true;
        public bool RequireUnitScale = true;
    }

    public static class MapGenModuleSetSignature
    {
        public static string Build(MapGenModuleSetAsset moduleSet)
        {
            unchecked
            {
                var hash = 1469598103934665603UL;
                if (moduleSet == null)
                {
                    Add(ref hash, "no_module_set");
                    return hash.ToString("x16");
                }

                Add(ref hash, moduleSet.ModuleSetId);
                AddBoundsContract(ref hash, moduleSet.BoundsContract);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.FloorsA), moduleSet.FloorsA);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.FloorsB), moduleSet.FloorsB);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.WallsStraight), moduleSet.WallsStraight);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.WallsCornerInside), moduleSet.WallsCornerInside);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.WallsCornerOutside), moduleSet.WallsCornerOutside);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.ExteriorCeilings), moduleSet.ExteriorCeilings);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.InteriorCeilings), moduleSet.InteriorCeilings);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.WholeDoors), moduleSet.WholeDoors);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.HalfDoorFrames), moduleSet.HalfDoorFrames);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.HalfDoorPanels), moduleSet.HalfDoorPanels);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.PropCategories), moduleSet.PropCategories);
                AddEntries(ref hash, nameof(MapGenModuleSetAsset.RequiredUniqueProps), moduleSet.RequiredUniqueProps);
                return hash.ToString("x16");
            }
        }

        private static void AddBoundsContract(ref ulong hash, MapGenModuleBoundsContract contract)
        {
            if (contract == null)
            {
                Add(ref hash, "no_bounds_contract");
                return;
            }

            Add(ref hash, contract.Enabled ? 1 : 0);
            Add(ref hash, Mathf.RoundToInt(contract.CellSize * 1000f));
            Add(ref hash, Mathf.RoundToInt(contract.Height * 1000f));
            Add(ref hash, (int)contract.PivotMode);
            Add(ref hash, Mathf.RoundToInt(contract.PivotTolerance * 1000000f));
            Add(ref hash, contract.RequireIdentityRotation ? 1 : 0);
            Add(ref hash, contract.RequireUnitScale ? 1 : 0);
        }

        private static void AddEntries(ref ulong hash, string fieldName, MapGenModuleEntry[] entries)
        {
            Add(ref hash, fieldName);
            Add(ref hash, entries?.Length ?? 0);
            foreach (var entry in entries ?? Array.Empty<MapGenModuleEntry>())
            {
                if (entry == null)
                {
                    Add(ref hash, "null_entry");
                    continue;
                }

                Add(ref hash, entry.Prefab != null ? entry.Prefab.name : "no_prefab");
                Add(ref hash, entry.Weight);
                Add(ref hash, (int)entry.RotationPolicy);
                Add(ref hash, Mathf.RoundToInt(entry.Offset.x * 1000f));
                Add(ref hash, Mathf.RoundToInt(entry.Offset.y * 1000f));
                Add(ref hash, Mathf.RoundToInt(entry.Offset.z * 1000f));
                Add(ref hash, entry.Footprint.x);
                Add(ref hash, entry.Footprint.y);
                AddStrings(ref hash, entry.Tags);
            }
        }

        private static void AddStrings(ref ulong hash, string[] values)
        {
            Add(ref hash, values?.Length ?? 0);
            foreach (var value in values ?? Array.Empty<string>())
            {
                Add(ref hash, value);
            }
        }

        private static void Add(ref ulong hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 1099511628211UL;
            }
        }

        private static void Add(ref ulong hash, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Add(ref hash, 0);
                return;
            }

            for (var i = 0; i < value.Length; i++)
            {
                Add(ref hash, value[i]);
            }
        }
    }
}

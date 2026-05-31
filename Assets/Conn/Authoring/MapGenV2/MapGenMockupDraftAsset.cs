using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Mockup Draft", fileName = "MapGenMockupDraft")]
    public sealed class MapGenMockupDraftAsset : ScriptableObject
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public MapGenProfileAsset Profile;
        [Header("Draft Source")]
        public string MapId = string.Empty;
        public string DisplayName = string.Empty;
        public float CellSize = 1f;
        public string StyleId = string.Empty;
        public string RuleSetId = string.Empty;
        public string LightingPreset = string.Empty;
        public MapGenOutputSettings OutputSettings = MapGenOutputSettings.Defaults();
        public MapGenNavigationAdapterSettings NavigationSettings = MapGenNavigationAdapterSettings.Defaults();
        public MapGenQuantityRules QuantityRules = MapGenQuantityRules.Defaults();
        public MapGenDistanceRules DistanceRules = MapGenDistanceRules.Defaults();
        public MapGenPostProcessRules PostProcessRules = MapGenPostProcessRules.Defaults();
        public int LoopRate;
        public MapGenPropPlacementRules[] PropPlacementRules = Array.Empty<MapGenPropPlacementRules>();
        public MapGenRoomShapeAsset[] RoomShapes = Array.Empty<MapGenRoomShapeAsset>();
        public MapGenRoomShapeAsset[] RoomShapePool = Array.Empty<MapGenRoomShapeAsset>();
        public MapGenRoomTemplateAsset[] RoomTemplates = Array.Empty<MapGenRoomTemplateAsset>();
        public MapGenCorridorTemplateAsset[] CorridorTemplates = Array.Empty<MapGenCorridorTemplateAsset>();
        public MapGenDraftModuleSet ModuleData = new MapGenDraftModuleSet();
        public bool EmbeddedSourceImported;
        public int Seed;
        public Vector2Int GridSize = new Vector2Int(32, 32);
        public MapGenDraftPrefabPalette PrefabPalette = new MapGenDraftPrefabPalette();
        public MapGenMockupCell[] Cells = Array.Empty<MapGenMockupCell>();
        public bool Accepted;
        public string AcceptedSignature = string.Empty;
        public string LastGeneratedSignature = string.Empty;
        public string LastGeneratedSourceSignature = string.Empty;
        public string AcceptedSourceSignature = string.Empty;
        [TextArea(2, 6)]
        public string GenerationNotes = string.Empty;
        public int LastDirectRouteCellsAdded;
        public int LastDeadEndCorridorsRemoved;
        public int LastIsolatedRoomsRemoved;
        public int LastEnclosedEmptyCellsFilled;
        public int LastReservedMaskCellsFilled;
        public MapGenMockupRegionOverride[] RegionOverrides = Array.Empty<MapGenMockupRegionOverride>();

        public int Width => Mathf.Max(1, GridSize.x);

        public int Height => Mathf.Max(1, GridSize.y);

        public bool IsAcceptedSignatureCurrent => !Accepted || AcceptedSignature == ComputeSignature();

        public bool IsGeneratedSignatureCurrent => string.IsNullOrEmpty(LastGeneratedSignature) || LastGeneratedSignature == ComputeSignature();

        public string CurrentSourceSignature => MapGenMockupSourceSignature.Build(this);

        private bool UseLegacyProfileSource => !EmbeddedSourceImported && Profile != null;

        public string GetMapId()
        {
            if (!string.IsNullOrWhiteSpace(MapId) || Profile == null)
            {
                return MapId ?? string.Empty;
            }

            return Profile.ProfileId ?? string.Empty;
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName) || Profile == null)
            {
                return DisplayName ?? string.Empty;
            }

            return Profile.DisplayName ?? string.Empty;
        }

        public float GetCellSize()
        {
            return UseLegacyProfileSource
                ? Mathf.Max(0.01f, Profile.CellSize)
                : Mathf.Max(0.01f, CellSize);
        }

        public string GetStyleId()
        {
            if (!string.IsNullOrWhiteSpace(StyleId) || Profile == null || Profile.StyleSet == null)
            {
                return StyleId ?? string.Empty;
            }

            return Profile.StyleSet.StyleId ?? string.Empty;
        }

        public string GetRuleSetId()
        {
            if (!string.IsNullOrWhiteSpace(RuleSetId) || Profile == null || Profile.LayoutRules == null)
            {
                return RuleSetId ?? string.Empty;
            }

            return Profile.LayoutRules.name ?? string.Empty;
        }

        public string GetModuleSetId()
        {
            if (!UseLegacyProfileSource)
            {
                return ModuleData != null ? ModuleData.ModuleSetId ?? string.Empty : string.Empty;
            }

            var moduleSet = Profile.StyleSet != null ? Profile.StyleSet.ModuleSet : null;
            return moduleSet != null ? moduleSet.ModuleSetId ?? string.Empty : string.Empty;
        }

        public MapGenOutputSettings GetOutputSettings()
        {
            var settings = UseLegacyProfileSource ? Profile.OutputSettings : OutputSettings;
            NormalizeOutputSettings(ref settings);
            return settings;
        }

        public MapGenNavigationAdapterSettings GetNavigationSettings()
        {
            return UseLegacyProfileSource ? Profile.NavigationSettings : NavigationSettings;
        }

        public MapGenQuantityRules GetQuantityRules()
        {
            return NormalizeQuantityRules(UseLegacyProfileSource && Profile.LayoutRules != null
                ? CopyQuantityRules(Profile.LayoutRules)
                : QuantityRules);
        }

        public MapGenDistanceRules GetDistanceRules()
        {
            return UseLegacyProfileSource && Profile.LayoutRules != null
                ? Profile.LayoutRules.DistanceRules
                : DistanceRules;
        }

        public MapGenPostProcessRules GetPostProcessRules()
        {
            return NormalizePostProcessRules(UseLegacyProfileSource && Profile.LayoutRules != null
                ? CopyPostProcessRules(Profile.LayoutRules)
                : PostProcessRules);
        }

        public MapGenPropPlacementRules[] GetPropPlacementRules()
        {
            return UseLegacyProfileSource && Profile.LayoutRules != null
                ? Profile.LayoutRules.PropPlacementRules ?? Array.Empty<MapGenPropPlacementRules>()
                : PropPlacementRules ?? Array.Empty<MapGenPropPlacementRules>();
        }

        public MapGenRoomCategory[] GetRequiredRoomCategories()
        {
            var quantity = GetQuantityRules();
            return quantity.RequiredCategories != null && quantity.RequiredCategories.Length > 0
                ? quantity.RequiredCategories
                : new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
        }

        public MapGenRoomShapeAsset[] GetRoomShapes()
        {
            return UseLegacyProfileSource
                ? Profile.RoomShapes ?? Array.Empty<MapGenRoomShapeAsset>()
                : RoomShapes ?? Array.Empty<MapGenRoomShapeAsset>();
        }

        public MapGenRoomShapeAsset[] GetRoomShapePool()
        {
            if (!UseLegacyProfileSource)
            {
                return RoomShapePool ?? Array.Empty<MapGenRoomShapeAsset>();
            }

            return Profile.StyleSet != null
                ? Profile.StyleSet.RoomShapePool ?? Array.Empty<MapGenRoomShapeAsset>()
                : Array.Empty<MapGenRoomShapeAsset>();
        }

        public MapGenRoomTemplateAsset[] GetRoomTemplates()
        {
            if (!UseLegacyProfileSource)
            {
                return RoomTemplates ?? Array.Empty<MapGenRoomTemplateAsset>();
            }

            return Profile.StyleSet != null
                ? Profile.StyleSet.RoomTemplates ?? Array.Empty<MapGenRoomTemplateAsset>()
                : Array.Empty<MapGenRoomTemplateAsset>();
        }

        public MapGenCorridorTemplateAsset[] GetCorridorTemplates()
        {
            if (!UseLegacyProfileSource)
            {
                return CorridorTemplates ?? Array.Empty<MapGenCorridorTemplateAsset>();
            }

            return Profile.StyleSet != null
                ? Profile.StyleSet.CorridorTemplates ?? Array.Empty<MapGenCorridorTemplateAsset>()
                : Array.Empty<MapGenCorridorTemplateAsset>();
        }

        public MapGenModuleBoundsContract GetModuleBoundsContract()
        {
            if (!UseLegacyProfileSource)
            {
                return ModuleData != null ? ModuleData.BoundsContract : null;
            }

            var moduleSet = Profile.StyleSet != null ? Profile.StyleSet.ModuleSet : null;
            return moduleSet != null ? moduleSet.BoundsContract : null;
        }

        public MapGenModuleEntry[] GetModuleEntries(MapGenModuleCategory category)
        {
            if (!UseLegacyProfileSource)
            {
                return ModuleData != null ? ModuleData.GetEntries(category) : Array.Empty<MapGenModuleEntry>();
            }

            var moduleSet = Profile.StyleSet != null ? Profile.StyleSet.ModuleSet : null;
            return moduleSet != null ? moduleSet.GetEntries(category) : Array.Empty<MapGenModuleEntry>();
        }

        public bool ImportFromProfileSource(bool importSeed = false)
        {
            return ImportFromProfileSource(Profile, importSeed);
        }

        public bool ImportFromProfileSource(MapGenProfileAsset profile, bool importSeed = false)
        {
            if (profile == null)
            {
                return false;
            }

            Profile = profile;
            MapId = profile.ProfileId ?? string.Empty;
            DisplayName = profile.DisplayName ?? string.Empty;
            GridSize = new Vector2Int(Mathf.Max(1, profile.MapSize.x), Mathf.Max(1, profile.MapSize.y));
            CellSize = Mathf.Max(0.01f, profile.CellSize);
            if (importSeed)
            {
                Seed = profile.Seed;
            }

            OutputSettings = profile.OutputSettings;
            NormalizeOutputSettings(ref OutputSettings);
            NavigationSettings = profile.NavigationSettings;
            RoomShapes = CloneArray(profile.RoomShapes);
            if (profile.LayoutRules != null)
            {
                RuleSetId = profile.LayoutRules.name ?? string.Empty;
                QuantityRules = CopyQuantityRules(profile.LayoutRules);
                DistanceRules = profile.LayoutRules.DistanceRules;
                PostProcessRules = CopyPostProcessRules(profile.LayoutRules);
                LoopRate = Mathf.Clamp(profile.LayoutRules.LoopRate, 0, 100);
                PropPlacementRules = ClonePropRules(profile.LayoutRules.PropPlacementRules);
            }
            else
            {
                RuleSetId = string.Empty;
                QuantityRules = MapGenQuantityRules.Defaults();
                DistanceRules = MapGenDistanceRules.Defaults();
                PostProcessRules = MapGenPostProcessRules.Defaults();
                LoopRate = 0;
                PropPlacementRules = Array.Empty<MapGenPropPlacementRules>();
            }

            var styleSet = profile.StyleSet;
            StyleId = styleSet != null ? styleSet.StyleId ?? string.Empty : string.Empty;
            LightingPreset = styleSet != null ? styleSet.LightingPreset ?? string.Empty : string.Empty;
            RoomShapePool = styleSet != null ? CloneArray(styleSet.RoomShapePool) : Array.Empty<MapGenRoomShapeAsset>();
            RoomTemplates = styleSet != null ? CloneArray(styleSet.RoomTemplates) : Array.Empty<MapGenRoomTemplateAsset>();
            CorridorTemplates = styleSet != null ? CloneArray(styleSet.CorridorTemplates) : Array.Empty<MapGenCorridorTemplateAsset>();
            ModuleData ??= new MapGenDraftModuleSet();
            ModuleData.CopyFrom(styleSet != null ? styleSet.ModuleSet : null);
            EmbeddedSourceImported = true;
            NormalizeEmbeddedSourceData();
            return true;
        }

        public MapGenValidationReport ValidateDraftSource()
        {
            var report = new MapGenValidationReport();
            if (!MapGenGridCoord.IsValidSize(Width, Height))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_invalid_map_size",
                    "Draft source map size must be positive.",
                    "Set both draft grid size axes to at least 1.",
                    contextPath: nameof(GridSize)));
            }

            if (GetCellSize() <= 0f)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_invalid_cell_size",
                    "Draft source cell size must be positive.",
                    "Set CellSize above zero.",
                    contextPath: nameof(CellSize)));
            }

            var outputSettings = GetOutputSettings();
            outputSettings.Validate(report);
            var quantity = GetQuantityRules();
            ValidateQuantityRules(report, quantity);
            ValidateDistanceRules(report, GetDistanceRules());
            ValidatePostProcessRules(report, GetPostProcessRules());
            ValidatePropPlacementRules(report, GetPropPlacementRules());
            ValidateRoomShapes(report, GetRoomShapes());
            ValidateRoomTemplates(report, GetRoomTemplates());
            ValidateCorridorTemplates(report, GetCorridorTemplates());
            ValidateTemplateGraph(report, quantity, GetRoomTemplates(), GetCorridorTemplates());
            return report;
        }

        public MapGenValidationReport GenerateFromProfile()
        {
            return GenerateFromProfile(false, null);
        }

        public MapGenValidationReport GenerateFromProfile(Func<bool> shouldCancel)
        {
            return GenerateFromProfile(false, shouldCancel);
        }

        public MapGenValidationReport GenerateFromDraft()
        {
            return GenerateFromDraft(false, null);
        }

        public MapGenValidationReport GenerateFromDraft(Func<bool> shouldCancel)
        {
            return GenerateFromDraft(false, shouldCancel);
        }

        public MapGenValidationReport RegenerateUnlockedFromProfile()
        {
            return GenerateFromProfile(true, null);
        }

        public MapGenValidationReport RegenerateUnlockedFromProfile(Func<bool> shouldCancel)
        {
            return GenerateFromProfile(true, shouldCancel);
        }

        public MapGenValidationReport RegenerateUnlockedFromDraft()
        {
            return GenerateFromDraft(true, null);
        }

        public MapGenValidationReport RegenerateUnlockedFromDraft(Func<bool> shouldCancel)
        {
            return GenerateFromDraft(true, shouldCancel);
        }

        public MapGenValidationReport RegenerateRegionFromProfile(int regionId)
        {
            if (Profile != null)
            {
                ImportFromProfileSource();
            }

            return RegenerateRegionFromDraft(regionId);
        }

        public MapGenValidationReport RegenerateRegionFromDraft(int regionId)
        {
            if (regionId < 0)
            {
                var report = new MapGenValidationReport();
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "mockup_draft_invalid_region_regenerate",
                    "Cannot regenerate a region without a valid region id.",
                    "Select a generated room or corridor region first."));
                return report;
            }

            var sourceReport = ValidateDraftSource();
            if (!sourceReport.IsValid)
            {
                return sourceReport;
            }

            var previousWidth = Width;
            var previousHeight = Height;
            var previousCells = Cells ?? Array.Empty<MapGenMockupCell>();
            var previousOverrides = RegionOverrides ?? Array.Empty<MapGenMockupRegionOverride>();
            var usesTemplates = MapGenTemplateMockupSolver.CanUseTemplates(this);
            var result = usesTemplates
                ? MapGenTemplateMockupSolver.Generate(this, Seed)
                : MapGenMockupSolver.Generate(
                    Width,
                    Height,
                    Seed,
                    GetRequiredRoomCategories());
            if (!result.Success)
            {
                return result.Report;
            }

            GridSize = new Vector2Int(result.Width, result.Height);
            Cells = result.Cells;
            if (!usesTemplates)
            {
                AssignRoomShapeIdsFromDraft();
            }

            MapGenMockupRegionUtility.AssignCorridorRegionIds(Width, Height, Cells, false, MaxRegionId(previousCells) + 1);
            PreserveRegionsExcept(previousWidth, previousHeight, previousCells, regionId);
            Accepted = false;
            AcceptedSignature = string.Empty;
            LastGeneratedSignature = ComputeSignature();
            LastGeneratedSourceSignature = CurrentSourceSignature;
            RegionOverrides = CopyOverridesExcept(previousOverrides, regionId);
            ClearPostProcessReport();
            return result.Report;
        }

        private MapGenValidationReport GenerateFromProfile(bool preserveLockedRegions, Func<bool> shouldCancel)
        {
            if (Profile != null)
            {
                ImportFromProfileSource();
            }

            return GenerateFromDraft(preserveLockedRegions, shouldCancel);
        }

        private MapGenValidationReport GenerateFromDraft(bool preserveLockedRegions, Func<bool> shouldCancel)
        {
            var sourceReport = ValidateDraftSource();
            if (!sourceReport.IsValid)
            {
                return sourceReport;
            }

            var usesTemplates = MapGenTemplateMockupSolver.CanUseTemplates(this);
            var result = usesTemplates
                ? MapGenTemplateMockupSolver.Generate(this, Seed, shouldCancel: shouldCancel)
                : MapGenMockupSolver.Generate(
                    Width,
                    Height,
                    Seed,
                    GetRequiredRoomCategories(),
                    shouldCancel);
            if (!result.Success)
            {
                return result.Report;
            }

            var previousWidth = Width;
            var previousHeight = Height;
            var previousCells = Cells ?? Array.Empty<MapGenMockupCell>();
            var previousOverrides = RegionOverrides ?? Array.Empty<MapGenMockupRegionOverride>();
            GridSize = new Vector2Int(result.Width, result.Height);
            Cells = result.Cells;
            if (!usesTemplates)
            {
                AssignRoomShapeIdsFromDraft();
            }

            var corridorMinimumRegionId = preserveLockedRegions ? MaxRegionId(previousCells) + 1 : 0;
            MapGenMockupRegionUtility.AssignCorridorRegionIds(Width, Height, Cells, false, corridorMinimumRegionId);
            if (preserveLockedRegions)
            {
                PreserveLockedRegions(previousWidth, previousHeight, previousCells, previousOverrides);
            }

            Accepted = false;
            AcceptedSignature = string.Empty;
            LastGeneratedSignature = ComputeSignature();
            LastGeneratedSourceSignature = CurrentSourceSignature;
            RegionOverrides = preserveLockedRegions
                ? CopyLockedOverrides(previousOverrides)
                : Array.Empty<MapGenMockupRegionOverride>();
            ClearPostProcessReport();
            return result.Report;
        }

        public MapGenPostProcessReport ApplyPostProcessingFromProfile()
        {
            return ApplyPostProcessingFromDraft(null);
        }

        public MapGenPostProcessReport ApplyPostProcessingFromProfile(Func<bool> shouldCancel)
        {
            return ApplyPostProcessingFromDraft(shouldCancel);
        }

        public MapGenPostProcessReport ApplyPostProcessingFromDraft()
        {
            return ApplyPostProcessingFromDraft(null);
        }

        public MapGenPostProcessReport ApplyPostProcessingFromDraft(Func<bool> shouldCancel)
        {
            EnsureCellArray();
            var options = new MapGenPostProcessOptions();
            var rules = GetPostProcessRules();
            options.UseDirectRoutes = rules.UseDirectRoutes;
            options.ReduceDeadEnds = rules.ReduceDeadEnds;
            options.RemoveSmallRooms = rules.RemoveSmallRooms;
            options.SplitLargeRooms = rules.SplitLargeRooms;
            options.ConsolidatePaths = rules.ConsolidatePaths;
            options.AddLoops = rules.AddLoops;
            options.NormalizeRouteLengths = rules.NormalizeRouteLengths;
            options.WidenCleanCorridors = rules.WidenCleanCorridors;
            options.MergeCompatibleAdjacentRooms = rules.MergeCompatibleAdjacentRooms;
            options.FillEnclosedEmptySpace = rules.FillEnclosedEmptySpace;
            options.FillReservedMasks = rules.FillReservedMasks;
            options.MaxPasses = rules.MaxPasses;
            options.PassOrder = rules.PassOrder;

            var report = MapGenMockupPostProcessor.Apply(Width, Height, Cells, options, shouldCancel);
            LastDirectRouteCellsAdded = report.DirectRouteCellsAdded;
            LastDeadEndCorridorsRemoved = report.DeadEndCorridorsRemoved;
            LastIsolatedRoomsRemoved = report.IsolatedRoomsRemoved;
            LastEnclosedEmptyCellsFilled = report.EnclosedEmptyCellsFilled;
            LastReservedMaskCellsFilled = report.ReservedMaskCellsFilled;
            MapGenMockupRegionUtility.AssignCorridorRegionIds(Width, Height, Cells, true);
            LastGeneratedSignature = ComputeSignature();
            LastGeneratedSourceSignature = CurrentSourceSignature;
            if (report.Changed)
            {
                ClearAcceptance();
            }

            return report;
        }

        public void EnsureCellArray()
        {
            var width = Width;
            var height = Height;
            if (Cells != null && Cells.Length == width * height)
            {
                return;
            }

            Resize(new Vector2Int(width, height));
        }

        public void Resize(Vector2Int gridSize)
        {
            var newWidth = Mathf.Max(1, gridSize.x);
            var newHeight = Mathf.Max(1, gridSize.y);
            var oldWidth = Width;
            var oldHeight = Height;
            var oldCells = Cells ?? Array.Empty<MapGenMockupCell>();
            var newCells = CreateEmptyCells(newWidth, newHeight);

            var copyWidth = Mathf.Min(oldWidth, newWidth);
            var copyHeight = Mathf.Min(oldHeight, newHeight);
            for (var y = 0; y < copyHeight; y++)
            {
                for (var x = 0; x < copyWidth; x++)
                {
                    var oldIndex = (y * oldWidth) + x;
                    var newIndex = (y * newWidth) + x;
                    if (oldIndex >= 0 && oldIndex < oldCells.Length)
                    {
                        newCells[newIndex] = oldCells[oldIndex];
                    }
                }
            }

            GridSize = new Vector2Int(newWidth, newHeight);
            Cells = newCells;
            LastGeneratedSignature = ComputeSignature();
            LastGeneratedSourceSignature = CurrentSourceSignature;
        }

        public string ComputeSignature()
        {
            return MapGenMockupSignature.Build(Width, Height, Seed, Cells, CurrentSourceSignature);
        }

        public void Accept()
        {
            EnsureCellArray();
            Accepted = true;
            AcceptedSignature = ComputeSignature();
            LastGeneratedSignature = AcceptedSignature;
            AcceptedSourceSignature = CurrentSourceSignature;
            LastGeneratedSourceSignature = AcceptedSourceSignature;
        }

        public void ClearAcceptance()
        {
            Accepted = false;
            AcceptedSignature = string.Empty;
            AcceptedSourceSignature = string.Empty;
        }

        public void Reject()
        {
            ClearAcceptance();
        }

        public void ClearDraft()
        {
            Cells = CreateEmptyCells(Width, Height);
            LastGeneratedSignature = string.Empty;
            LastGeneratedSourceSignature = string.Empty;
            RegionOverrides = Array.Empty<MapGenMockupRegionOverride>();
            ClearPostProcessReport();
            ClearAcceptance();
        }

        public bool TryGetRegionOverride(int regionId, out MapGenMockupRegionOverride regionOverride)
        {
            foreach (var candidate in RegionOverrides ?? Array.Empty<MapGenMockupRegionOverride>())
            {
                if (candidate.RegionId == regionId)
                {
                    regionOverride = candidate;
                    return true;
                }
            }

            regionOverride = default;
            return false;
        }

        public void SetRegionLocked(int regionId, bool locked)
        {
            if (regionId < 0)
            {
                return;
            }

            var regionOverride = GetOrCreateRegionOverride(regionId);
            regionOverride.Locked = locked;
            SetRegionOverride(regionOverride);
        }

        public void SetRegionCategory(int regionId, MapGenRoomCategory category)
        {
            if (regionId < 0)
            {
                return;
            }

            EnsureCellArray();
            for (var i = 0; i < Cells.Length; i++)
            {
                if (Cells[i].RegionId == regionId)
                {
                    Cells[i].RoomCategory = category;
                }
            }

            var regionOverride = GetOrCreateRegionOverride(regionId);
            regionOverride.HasCategoryOverride = true;
            regionOverride.CategoryOverride = category;
            SetRegionOverride(regionOverride);
            LastGeneratedSignature = ComputeSignature();
        }

        public void SetRegionState(int regionId, MapGenCellState state)
        {
            if (regionId < 0)
            {
                return;
            }

            EnsureCellArray();
            for (var i = 0; i < Cells.Length; i++)
            {
                if (Cells[i].RegionId != regionId)
                {
                    continue;
                }

                Cells[i] = ConvertRegionCell(Cells[i], regionId, state);
            }

            if (state == MapGenCellState.Empty)
            {
                ClearRegionOverride(regionId);
            }

            LastGeneratedSignature = ComputeSignature();
        }

        public void ClearRegionOverride(int regionId)
        {
            if (RegionOverrides == null || RegionOverrides.Length == 0)
            {
                return;
            }

            var kept = new MapGenMockupRegionOverride[RegionOverrides.Length];
            var count = 0;
            for (var i = 0; i < RegionOverrides.Length; i++)
            {
                if (RegionOverrides[i].RegionId == regionId)
                {
                    continue;
                }

                kept[count] = RegionOverrides[i];
                count++;
            }

            if (count == RegionOverrides.Length)
            {
                return;
            }

            Array.Resize(ref kept, count);
            RegionOverrides = kept;
        }

        private void ClearPostProcessReport()
        {
            LastDirectRouteCellsAdded = 0;
            LastDeadEndCorridorsRemoved = 0;
            LastIsolatedRoomsRemoved = 0;
            LastEnclosedEmptyCellsFilled = 0;
            LastReservedMaskCellsFilled = 0;
        }

        private void OnValidate()
        {
            EnsureCellArray();
            PrefabPalette ??= new MapGenDraftPrefabPalette();
            RegionOverrides ??= Array.Empty<MapGenMockupRegionOverride>();
            NormalizeEmbeddedSourceData();
        }

        private MapGenMockupRegionOverride GetOrCreateRegionOverride(int regionId)
        {
            if (TryGetRegionOverride(regionId, out var regionOverride))
            {
                return regionOverride;
            }

            return new MapGenMockupRegionOverride
            {
                RegionId = regionId,
                Locked = false,
                HasCategoryOverride = false,
                CategoryOverride = MapGenRoomCategory.Main
            };
        }

        private void SetRegionOverride(MapGenMockupRegionOverride regionOverride)
        {
            RegionOverrides ??= Array.Empty<MapGenMockupRegionOverride>();
            for (var i = 0; i < RegionOverrides.Length; i++)
            {
                if (RegionOverrides[i].RegionId == regionOverride.RegionId)
                {
                    RegionOverrides[i] = regionOverride;
                    return;
                }
            }

            Array.Resize(ref RegionOverrides, RegionOverrides.Length + 1);
            RegionOverrides[RegionOverrides.Length - 1] = regionOverride;
        }

        private void PreserveLockedRegions(
            int previousWidth,
            int previousHeight,
            MapGenMockupCell[] previousCells,
            MapGenMockupRegionOverride[] previousOverrides)
        {
            foreach (var regionOverride in previousOverrides ?? Array.Empty<MapGenMockupRegionOverride>())
            {
                if (!regionOverride.Locked)
                {
                    continue;
                }

                for (var y = 0; y < Mathf.Min(previousHeight, Height); y++)
                {
                    for (var x = 0; x < Mathf.Min(previousWidth, Width); x++)
                    {
                        var previousIndex = (y * previousWidth) + x;
                        if (previousIndex < 0 || previousIndex >= previousCells.Length)
                        {
                            continue;
                        }

                        var previousCell = previousCells[previousIndex];
                        if (previousCell.RegionId != regionOverride.RegionId)
                        {
                            continue;
                        }

                        Cells[(y * Width) + x] = previousCell;
                    }
                }
            }
        }

        private void PreserveRegionsExcept(
            int previousWidth,
            int previousHeight,
            MapGenMockupCell[] previousCells,
            int excludedRegionId)
        {
            for (var y = 0; y < Mathf.Min(previousHeight, Height); y++)
            {
                for (var x = 0; x < Mathf.Min(previousWidth, Width); x++)
                {
                    var previousIndex = (y * previousWidth) + x;
                    if (previousIndex < 0 || previousIndex >= previousCells.Length)
                    {
                        continue;
                    }

                    var previousCell = previousCells[previousIndex];
                    if (previousCell.RegionId < 0 || previousCell.RegionId == excludedRegionId)
                    {
                        continue;
                    }

                    Cells[(y * Width) + x] = previousCell;
                }
            }
        }

        private static MapGenMockupRegionOverride[] CopyLockedOverrides(MapGenMockupRegionOverride[] source)
        {
            var copied = new MapGenMockupRegionOverride[source?.Length ?? 0];
            var count = 0;
            foreach (var regionOverride in source ?? Array.Empty<MapGenMockupRegionOverride>())
            {
                if (!regionOverride.Locked)
                {
                    continue;
                }

                copied[count] = regionOverride;
                count++;
            }

            Array.Resize(ref copied, count);
            return copied;
        }

        private static MapGenMockupRegionOverride[] CopyOverridesExcept(MapGenMockupRegionOverride[] source, int excludedRegionId)
        {
            var copied = new MapGenMockupRegionOverride[source?.Length ?? 0];
            var count = 0;
            foreach (var regionOverride in source ?? Array.Empty<MapGenMockupRegionOverride>())
            {
                if (regionOverride.RegionId == excludedRegionId)
                {
                    continue;
                }

                copied[count] = regionOverride;
                count++;
            }

            Array.Resize(ref copied, count);
            return copied;
        }

        private static int MaxRegionId(MapGenMockupCell[] cells)
        {
            var maxRegionId = -1;
            foreach (var cell in cells ?? Array.Empty<MapGenMockupCell>())
            {
                if (cell.RegionId > maxRegionId)
                {
                    maxRegionId = cell.RegionId;
                }
            }

            return maxRegionId;
        }

        private void NormalizeEmbeddedSourceData()
        {
            GridSize = new Vector2Int(Mathf.Max(1, GridSize.x), Mathf.Max(1, GridSize.y));
            CellSize = Mathf.Max(0.01f, CellSize);
            NormalizeOutputSettings(ref OutputSettings);
            QuantityRules = NormalizeQuantityRules(QuantityRules);
            PostProcessRules = NormalizePostProcessRules(PostProcessRules);
            LoopRate = Mathf.Clamp(LoopRate, 0, 100);
            PropPlacementRules ??= Array.Empty<MapGenPropPlacementRules>();
            RoomShapes ??= Array.Empty<MapGenRoomShapeAsset>();
            RoomShapePool ??= Array.Empty<MapGenRoomShapeAsset>();
            RoomTemplates ??= Array.Empty<MapGenRoomTemplateAsset>();
            CorridorTemplates ??= Array.Empty<MapGenCorridorTemplateAsset>();
            ModuleData ??= new MapGenDraftModuleSet();
            ModuleData.Normalize();
        }

        private static void NormalizeOutputSettings(ref MapGenOutputSettings settings)
        {
            var defaults = MapGenOutputSettings.Defaults();
            if (string.IsNullOrWhiteSpace(settings.DraftFolder))
            {
                settings.DraftFolder = defaults.DraftFolder;
            }

            if (string.IsNullOrWhiteSpace(settings.MaterializedPrefabFolder))
            {
                settings.MaterializedPrefabFolder = defaults.MaterializedPrefabFolder;
            }

            if (string.IsNullOrWhiteSpace(settings.BakedAssetFolder))
            {
                settings.BakedAssetFolder = defaults.BakedAssetFolder;
            }
        }

        private static MapGenQuantityRules NormalizeQuantityRules(MapGenQuantityRules rules)
        {
            rules.MinRooms = Mathf.Max(1, rules.MinRooms);
            rules.MaxRooms = Mathf.Max(rules.MinRooms, rules.MaxRooms);
            rules.MinCorridorCells = Mathf.Max(0, rules.MinCorridorCells);
            rules.MaxCorridorCells = Mathf.Max(rules.MinCorridorCells, rules.MaxCorridorCells);
            rules.TargetRoomDensityPercent = Mathf.Clamp(rules.TargetRoomDensityPercent, 0, 100);
            rules.TargetCorridorDensityPercent = Mathf.Clamp(rules.TargetCorridorDensityPercent, 0, 100);
            rules.RequiredCategories ??= new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
            rules.OptionalCategories ??= Array.Empty<MapGenRoomCategory>();
            return rules;
        }

        private static MapGenPostProcessRules NormalizePostProcessRules(MapGenPostProcessRules rules)
        {
            rules.MaxPasses = Mathf.Max(0, rules.MaxPasses);
            rules.PassOrder ??= Array.Empty<MapGenPostProcessPassKind>();
            return rules;
        }

        private static MapGenQuantityRules CopyQuantityRules(MapGenRuleSetAsset ruleSet)
        {
            if (ruleSet == null)
            {
                return MapGenQuantityRules.Defaults();
            }

            var rules = ruleSet.QuantityRules;
            rules.MinRooms = Mathf.Max(1, ruleSet.MinRooms);
            rules.MaxRooms = Mathf.Max(rules.MinRooms, ruleSet.MaxRooms);
            rules.MinCorridorCells = Mathf.Max(0, ruleSet.MinCorridorCells);
            rules.MaxCorridorCells = Mathf.Max(rules.MinCorridorCells, ruleSet.MaxCorridorCells);
            rules.RequiredCategories = CloneCategories(ruleSet.RequiredRoomCategories != null && ruleSet.RequiredRoomCategories.Length > 0
                ? ruleSet.RequiredRoomCategories
                : rules.RequiredCategories);
            rules.OptionalCategories = CloneCategories(ruleSet.OptionalRoomCategories != null
                ? ruleSet.OptionalRoomCategories
                : rules.OptionalCategories);
            return NormalizeQuantityRules(rules);
        }

        private static MapGenPostProcessRules CopyPostProcessRules(MapGenRuleSetAsset ruleSet)
        {
            if (ruleSet == null)
            {
                return MapGenPostProcessRules.Defaults();
            }

            var rules = ruleSet.PostProcessRules;
            rules.UseDirectRoutes = ruleSet.UseDirectRoutes;
            rules.ReduceDeadEnds = ruleSet.ReduceDeadEnds;
            rules.SplitLargeRooms = ruleSet.SplitLargeRooms;
            rules.RemoveSmallRooms = ruleSet.RemoveSmallRooms;
            rules.PassOrder = CloneArray(rules.PassOrder);
            return NormalizePostProcessRules(rules);
        }

        private static T[] CloneArray<T>(T[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<T>();
            }

            var clone = new T[source.Length];
            Array.Copy(source, clone, source.Length);
            return clone;
        }

        private static MapGenRoomCategory[] CloneCategories(MapGenRoomCategory[] source)
        {
            return CloneArray(source);
        }

        private static MapGenPropPlacementRules[] ClonePropRules(MapGenPropPlacementRules[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<MapGenPropPlacementRules>();
            }

            var clone = new MapGenPropPlacementRules[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                clone[i] = source[i];
                clone[i].RoomCategoryFilters = CloneArray(source[i].RoomCategoryFilters);
                clone[i].CorridorKindFilters = CloneArray(source[i].CorridorKindFilters);
            }

            return clone;
        }

        private static void ValidateQuantityRules(MapGenValidationReport report, MapGenQuantityRules rules)
        {
            if (rules.MinRooms < 1)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_invalid_min_rooms",
                    "Draft source minimum room count must be at least 1.",
                    "Set MinRooms to 1 or higher.",
                    contextPath: nameof(QuantityRules)));
            }

            if (rules.MaxRooms < rules.MinRooms)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_invalid_room_range",
                    "Draft source room count range is invalid.",
                    "Set MaxRooms greater than or equal to MinRooms.",
                    contextPath: nameof(QuantityRules)));
            }

            if (rules.MinCorridorCells < 0 || rules.MaxCorridorCells < rules.MinCorridorCells)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_invalid_corridor_range",
                    "Draft source corridor cell range is invalid.",
                    "Set corridor min/max values to a valid non-negative range.",
                    contextPath: nameof(QuantityRules)));
            }
        }

        private static void ValidateDistanceRules(MapGenValidationReport report, MapGenDistanceRules rules)
        {
            if (rules.MinStartToExitDistance < 0 || rules.MinStartToBossDistance < 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_invalid_distance_rules",
                    "Draft source distance rules cannot be negative.",
                    "Set minimum distances to zero or higher.",
                    contextPath: nameof(DistanceRules)));
            }
        }

        private static void ValidatePostProcessRules(MapGenValidationReport report, MapGenPostProcessRules rules)
        {
            if (rules.MaxPasses < 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_invalid_post_process_passes",
                    "Draft source post-process max passes cannot be negative.",
                    "Set MaxPasses to zero or higher.",
                    contextPath: nameof(PostProcessRules)));
            }
        }

        private static void ValidatePropPlacementRules(MapGenValidationReport report, MapGenPropPlacementRules[] rules)
        {
            for (var i = 0; i < (rules?.Length ?? 0); i++)
            {
                var rule = rules[i];
                if (string.IsNullOrWhiteSpace(rule.Channel))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "draft_source_prop_rule_missing_channel",
                        $"Draft prop placement rule {i} has no channel.",
                        "Assign a prop placement channel id.",
                        contextPath: $"{nameof(PropPlacementRules)}[{i}]"));
                }

                if (rule.DensityPercent < 0 || rule.DensityPercent > 100)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "draft_source_prop_rule_invalid_density",
                        $"Draft prop placement rule {i} has invalid density.",
                        "Set DensityPercent within 0..100.",
                        contextPath: $"{nameof(PropPlacementRules)}[{i}]"));
                }

                if (rule.MinSpacingCells < 0)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "draft_source_prop_rule_invalid_spacing",
                        $"Draft prop placement rule {i} has invalid spacing.",
                        "Set MinSpacingCells to zero or higher.",
                        contextPath: $"{nameof(PropPlacementRules)}[{i}]"));
                }
            }
        }

        private static void ValidateRoomShapes(MapGenValidationReport report, MapGenRoomShapeAsset[] shapes)
        {
            for (var i = 0; i < (shapes?.Length ?? 0); i++)
            {
                var shape = shapes[i];
                if (shape == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "draft_source_null_room_shape",
                        $"Draft room shape slot {i} is empty.",
                        "Remove the empty slot or assign a room shape.",
                        contextPath: $"{nameof(RoomShapes)}[{i}]"));
                    continue;
                }

                report.AddRange(shape.Validate(), $"{nameof(RoomShapes)}[{i}]:{shape.name}");
            }
        }

        private static void ValidateRoomTemplates(MapGenValidationReport report, MapGenRoomTemplateAsset[] templates)
        {
            for (var i = 0; i < (templates?.Length ?? 0); i++)
            {
                var template = templates[i];
                if (template == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "draft_source_null_room_template",
                        $"Draft room template slot {i} is empty.",
                        "Remove the empty slot or assign a room template.",
                        contextPath: $"{nameof(RoomTemplates)}[{i}]"));
                    continue;
                }

                report.AddRange(template.Validate(), $"{nameof(RoomTemplates)}[{i}]:{template.name}");
            }
        }

        private static void ValidateCorridorTemplates(MapGenValidationReport report, MapGenCorridorTemplateAsset[] templates)
        {
            for (var i = 0; i < (templates?.Length ?? 0); i++)
            {
                var template = templates[i];
                if (template == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "draft_source_null_corridor_template",
                        $"Draft corridor template slot {i} is empty.",
                        "Remove the empty slot or assign a corridor template.",
                        contextPath: $"{nameof(CorridorTemplates)}[{i}]"));
                    continue;
                }

                report.AddRange(template.Validate(), $"{nameof(CorridorTemplates)}[{i}]:{template.name}");
            }
        }

        private void ValidateTemplateGraph(
            MapGenValidationReport report,
            MapGenQuantityRules quantity,
            MapGenRoomTemplateAsset[] roomTemplates,
            MapGenCorridorTemplateAsset[] corridorTemplates)
        {
            var required = quantity.RequiredCategories ?? Array.Empty<MapGenRoomCategory>();
            if (required.Length == 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_missing_required_categories",
                    "Draft source has no required room categories.",
                    "Add at least Start and Exit to required categories.",
                    contextPath: nameof(QuantityRules)));
                return;
            }

            if (quantity.MaxRooms < required.Length)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_required_categories_exceed_max_rooms",
                    $"Required room category count {required.Length} exceeds MaxRooms {quantity.MaxRooms}.",
                    "Raise MaxRooms or remove required categories.",
                    contextPath: nameof(QuantityRules)));
            }

            var cellCount = Width * Height;
            if (quantity.MinRooms > cellCount)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_min_rooms_exceeds_map_cells",
                    $"MinRooms {quantity.MinRooms} exceeds available map cells {cellCount}.",
                    "Lower MinRooms or increase the map size.",
                    contextPath: nameof(QuantityRules)));
            }

            if (quantity.MinCorridorCells > cellCount)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_min_corridors_exceeds_map_cells",
                    $"MinCorridorCells {quantity.MinCorridorCells} exceeds available map cells {cellCount}.",
                    "Lower MinCorridorCells or increase the map size.",
                    contextPath: nameof(QuantityRules)));
            }

            var distance = GetDistanceRules();
            var maxManhattanDistance = Mathf.Max(0, Width - 1) + Mathf.Max(0, Height - 1);
            if (distance.MinStartToExitDistance > maxManhattanDistance)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_start_exit_distance_impossible",
                    $"Start-to-exit distance {distance.MinStartToExitDistance} exceeds map maximum {maxManhattanDistance}.",
                    "Lower MinStartToExitDistance or increase the map size.",
                    contextPath: nameof(DistanceRules)));
            }

            if (CountNonNull(roomTemplates) == 0)
            {
                return;
            }

            foreach (var category in required)
            {
                if (CountCategoryCandidateCells(Width, Height, roomTemplates, category) > 0)
                {
                    continue;
                }

                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "draft_source_required_category_has_no_template",
                    $"Required room category {category} has no room template that fits the map.",
                    $"Add a {category} room template that fits the draft map size, or add a Main fallback template.",
                    contextPath: $"{nameof(RoomTemplates)}/{category}"));
            }

            if (!HasRoomConnector(roomTemplates) || CountNonNull(corridorTemplates) > 0)
            {
                return;
            }

            report.Add(new MapGenIssue(
                MapGenGenerationPhase.ValidateProfile,
                "draft_source_missing_corridor_templates_for_connectors",
                "Room templates define connectors but the draft has no corridor templates.",
                "Add corridor templates with compatible opposite connectors.",
                severity: MapGenIssueSeverity.Warning,
                contextPath: nameof(CorridorTemplates)));
        }

        private static bool HasRoomConnector(MapGenRoomTemplateAsset[] roomTemplates)
        {
            foreach (var room in roomTemplates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                if (room != null && (room.Connectors?.Length ?? 0) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountCategoryCandidateCells(
            int width,
            int height,
            MapGenRoomTemplateAsset[] templates,
            MapGenRoomCategory category)
        {
            var exactCount = 0;
            var fallbackCount = 0;
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
                    fallbackCount += candidateCount;
                }
            }

            return exactCount > 0 ? exactCount : fallbackCount;
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

        private void AssignRoomShapeIdsFromDraft()
        {
            if (GetRoomShapes().Length == 0)
            {
                return;
            }

            for (var i = 0; i < Cells.Length; i++)
            {
                if (Cells[i].State != MapGenCellState.Room && Cells[i].State != MapGenCellState.Connector)
                {
                    continue;
                }

                Cells[i].SourceShapeId = FindRoomShapeId(Cells[i].RoomCategory);
                Cells[i].SourceTemplateId = string.Empty;
            }
        }

        private string FindRoomShapeId(MapGenRoomCategory category)
        {
            var fallback = string.Empty;
            foreach (var shape in GetRoomShapes())
            {
                if (shape == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(fallback))
                {
                    fallback = shape.ShapeId ?? string.Empty;
                }

                if (shape.Category == category)
                {
                    return shape.ShapeId ?? string.Empty;
                }
            }

            return fallback;
        }

        private static MapGenMockupCell ConvertRegionCell(MapGenMockupCell cell, int regionId, MapGenCellState state)
        {
            if (state == MapGenCellState.Empty)
            {
                return MapGenMockupCell.Empty;
            }

            cell.State = state;
            cell.RegionId = regionId;
            cell.SocketKind = state == MapGenCellState.Blocked ? MapGenSocketKind.Blocked : MapGenSocketKind.None;
            cell.SocketId = string.Empty;
            cell.SocketWidth = state == MapGenCellState.Blocked ? 1 : 0;
            cell.PropChannel = string.Empty;
            cell.PropWeight = 1;
            return cell;
        }

        private static MapGenMockupCell[] CreateEmptyCells(int width, int height)
        {
            var cells = new MapGenMockupCell[width * height];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            return cells;
        }
    }

    [Serializable]
    public sealed class MapGenDraftPrefabPalette
    {
        public GameObject RoomFloor;
        public GameObject CorridorFloor;
        public GameObject Wall;
        public GameObject InsideCorner;
        public GameObject OutsideCorner;
        public GameObject Ceiling;
        public GameObject Door;
        public GameObject Blocker;
        public GameObject Prop;

        public bool HasAnyPrefab()
        {
            return RoomFloor != null
                || CorridorFloor != null
                || Wall != null
                || InsideCorner != null
                || OutsideCorner != null
                || Ceiling != null
                || Door != null
                || Blocker != null
                || Prop != null;
        }
    }

    [Serializable]
    public sealed class MapGenDraftModuleSet
    {
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
        public MapGenModuleEntry[] Blockers = Array.Empty<MapGenModuleEntry>();

        public void CopyFrom(MapGenModuleSetAsset source)
        {
            if (source == null)
            {
                ModuleSetId = string.Empty;
                BoundsContract = new MapGenModuleBoundsContract();
                FloorsA = Array.Empty<MapGenModuleEntry>();
                FloorsB = Array.Empty<MapGenModuleEntry>();
                WallsStraight = Array.Empty<MapGenModuleEntry>();
                WallsCornerInside = Array.Empty<MapGenModuleEntry>();
                WallsCornerOutside = Array.Empty<MapGenModuleEntry>();
                ExteriorCeilings = Array.Empty<MapGenModuleEntry>();
                InteriorCeilings = Array.Empty<MapGenModuleEntry>();
                WholeDoors = Array.Empty<MapGenModuleEntry>();
                HalfDoorFrames = Array.Empty<MapGenModuleEntry>();
                HalfDoorPanels = Array.Empty<MapGenModuleEntry>();
                PropCategories = Array.Empty<MapGenModuleEntry>();
                RequiredUniqueProps = Array.Empty<MapGenModuleEntry>();
                Blockers = Array.Empty<MapGenModuleEntry>();
                return;
            }

            ModuleSetId = source.ModuleSetId ?? string.Empty;
            BoundsContract = CloneBoundsContract(source.BoundsContract);
            FloorsA = CloneEntries(source.FloorsA);
            FloorsB = CloneEntries(source.FloorsB);
            WallsStraight = CloneEntries(source.WallsStraight);
            WallsCornerInside = CloneEntries(source.WallsCornerInside);
            WallsCornerOutside = CloneEntries(source.WallsCornerOutside);
            ExteriorCeilings = CloneEntries(source.ExteriorCeilings);
            InteriorCeilings = CloneEntries(source.InteriorCeilings);
            WholeDoors = CloneEntries(source.WholeDoors);
            HalfDoorFrames = CloneEntries(source.HalfDoorFrames);
            HalfDoorPanels = CloneEntries(source.HalfDoorPanels);
            PropCategories = CloneEntries(source.PropCategories);
            RequiredUniqueProps = CloneEntries(source.RequiredUniqueProps);
            Blockers = CloneEntries(source.Blockers);
            Normalize();
        }

        public void Normalize()
        {
            BoundsContract ??= new MapGenModuleBoundsContract();
            BoundsContract.CellSize = Mathf.Max(0.01f, BoundsContract.CellSize);
            BoundsContract.Height = Mathf.Max(0.01f, BoundsContract.Height);
            BoundsContract.PivotTolerance = Mathf.Max(0f, BoundsContract.PivotTolerance);
            FloorsA ??= Array.Empty<MapGenModuleEntry>();
            FloorsB ??= Array.Empty<MapGenModuleEntry>();
            WallsStraight ??= Array.Empty<MapGenModuleEntry>();
            WallsCornerInside ??= Array.Empty<MapGenModuleEntry>();
            WallsCornerOutside ??= Array.Empty<MapGenModuleEntry>();
            ExteriorCeilings ??= Array.Empty<MapGenModuleEntry>();
            InteriorCeilings ??= Array.Empty<MapGenModuleEntry>();
            WholeDoors ??= Array.Empty<MapGenModuleEntry>();
            HalfDoorFrames ??= Array.Empty<MapGenModuleEntry>();
            HalfDoorPanels ??= Array.Empty<MapGenModuleEntry>();
            PropCategories ??= Array.Empty<MapGenModuleEntry>();
            RequiredUniqueProps ??= Array.Empty<MapGenModuleEntry>();
            Blockers ??= Array.Empty<MapGenModuleEntry>();
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
                case MapGenModuleCategory.Prop:
                    return PropCategories;
                case MapGenModuleCategory.Blocker:
                    return Blockers;
                default:
                    return Array.Empty<MapGenModuleEntry>();
            }
        }

        private static MapGenModuleBoundsContract CloneBoundsContract(MapGenModuleBoundsContract source)
        {
            if (source == null)
            {
                return new MapGenModuleBoundsContract();
            }

            return new MapGenModuleBoundsContract
            {
                Enabled = source.Enabled,
                CellSize = source.CellSize,
                Height = source.Height,
                PivotMode = source.PivotMode,
                PivotTolerance = source.PivotTolerance,
                RequireIdentityRotation = source.RequireIdentityRotation,
                RequireUnitScale = source.RequireUnitScale
            };
        }

        private static MapGenModuleEntry[] CloneEntries(MapGenModuleEntry[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<MapGenModuleEntry>();
            }

            var clone = new MapGenModuleEntry[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                clone[i] = CloneEntry(source[i]);
            }

            return clone;
        }

        private static MapGenModuleEntry CloneEntry(MapGenModuleEntry source)
        {
            if (source == null)
            {
                return null;
            }

            return new MapGenModuleEntry
            {
                Prefab = source.Prefab,
                Weight = Mathf.Max(1, source.Weight),
                RotationPolicy = source.RotationPolicy,
                Offset = source.Offset,
                Footprint = new Vector2Int(Mathf.Max(1, source.Footprint.x), Mathf.Max(1, source.Footprint.y)),
                Tags = source.Tags != null ? (string[])source.Tags.Clone() : Array.Empty<string>()
            };
        }
    }

    internal static class MapGenMockupSourceSignature
    {
        public static string Build(MapGenMockupDraftAsset draft)
        {
            unchecked
            {
                var hash = 1469598103934665603UL;
                if (draft == null)
                {
                    Add(ref hash, "no_draft");
                    return hash.ToString("x16");
                }

                Add(ref hash, draft.GetMapId());
                Add(ref hash, draft.Width);
                Add(ref hash, draft.Height);
                Add(ref hash, Mathf.RoundToInt(draft.GetCellSize() * 1000f));
                Add(ref hash, draft.GetDisplayName());
                Add(ref hash, draft.GetStyleId());
                Add(ref hash, draft.LightingPreset);
                Add(ref hash, draft.GetRuleSetId());
                Add(ref hash, draft.LoopRate);
                AddOutputSettings(ref hash, draft.GetOutputSettings());
                AddNavigationSettings(ref hash, draft.GetNavigationSettings());
                AddQuantityRules(ref hash, draft.GetQuantityRules());
                AddDistanceRules(ref hash, draft.GetDistanceRules());
                AddPostProcessRules(ref hash, draft.GetPostProcessRules());
                AddPropRules(ref hash, draft.GetPropPlacementRules());
                AddRoomShapes(ref hash, draft.GetRoomShapes());
                AddRoomShapes(ref hash, draft.GetRoomShapePool());
                AddRoomTemplates(ref hash, draft.GetRoomTemplates());
                AddCorridorTemplates(ref hash, draft.GetCorridorTemplates());
                AddModuleData(ref hash, draft.ModuleData);
                AddPrefabPalette(ref hash, draft.PrefabPalette);
                return hash.ToString("x16");
            }
        }

        public static string Build(MapGenProfileAsset profile)
        {
            unchecked
            {
                var hash = 1469598103934665603UL;
                if (profile == null)
                {
                    Add(ref hash, "no_profile");
                    return hash.ToString("x16");
                }

                Add(ref hash, profile.ProfileId);
                Add(ref hash, profile.MapSize.x);
                Add(ref hash, profile.MapSize.y);
                Add(ref hash, Mathf.RoundToInt(profile.CellSize * 1000f));
                AddStyleSet(ref hash, profile.StyleSet);
                AddRuleSet(ref hash, profile.LayoutRules);
                AddRoomShapes(ref hash, profile.RoomShapes);
                return hash.ToString("x16");
            }
        }

        private static void AddOutputSettings(ref ulong hash, MapGenOutputSettings settings)
        {
            Add(ref hash, settings.DraftFolder);
            Add(ref hash, settings.MaterializedPrefabFolder);
            Add(ref hash, settings.BakedAssetFolder);
            Add(ref hash, (int)settings.OverwriteMode);
        }

        private static void AddNavigationSettings(ref ulong hash, MapGenNavigationAdapterSettings settings)
        {
            Add(ref hash, settings.BakeTraversalGraph ? 1 : 0);
            Add(ref hash, settings.BakeGridPathfinding ? 1 : 0);
            Add(ref hash, settings.ExportNavBuildBounds ? 1 : 0);
            Add(ref hash, Mathf.RoundToInt(settings.NavBuildPadding.x * 1000f));
            Add(ref hash, Mathf.RoundToInt(settings.NavBuildPadding.y * 1000f));
            Add(ref hash, Mathf.RoundToInt(settings.NavBuildPadding.z * 1000f));
        }

        private static void AddQuantityRules(ref ulong hash, MapGenQuantityRules rules)
        {
            Add(ref hash, rules.MinRooms);
            Add(ref hash, rules.MaxRooms);
            Add(ref hash, rules.MinCorridorCells);
            Add(ref hash, rules.MaxCorridorCells);
            Add(ref hash, rules.TargetRoomDensityPercent);
            Add(ref hash, rules.TargetCorridorDensityPercent);
            AddCategories(ref hash, rules.RequiredCategories);
            AddCategories(ref hash, rules.OptionalCategories);
        }

        private static void AddDistanceRules(ref ulong hash, MapGenDistanceRules rules)
        {
            Add(ref hash, rules.MinStartToExitDistance);
            Add(ref hash, rules.MinStartToBossDistance);
            Add(ref hash, rules.RequireQuestBeforeBoss ? 1 : 0);
        }

        private static void AddPostProcessRules(ref ulong hash, MapGenPostProcessRules rules)
        {
            Add(ref hash, rules.UseDirectRoutes ? 1 : 0);
            Add(ref hash, rules.ReduceDeadEnds ? 1 : 0);
            Add(ref hash, rules.SplitLargeRooms ? 1 : 0);
            Add(ref hash, rules.RemoveSmallRooms ? 1 : 0);
            Add(ref hash, rules.ConsolidatePaths ? 1 : 0);
            Add(ref hash, rules.AddLoops ? 1 : 0);
            Add(ref hash, rules.NormalizeRouteLengths ? 1 : 0);
            Add(ref hash, rules.WidenCleanCorridors ? 1 : 0);
            Add(ref hash, rules.MergeCompatibleAdjacentRooms ? 1 : 0);
            Add(ref hash, rules.FillEnclosedEmptySpace ? 1 : 0);
            Add(ref hash, rules.FillReservedMasks ? 1 : 0);
            Add(ref hash, rules.MaxPasses);
            foreach (var pass in rules.PassOrder ?? Array.Empty<MapGenPostProcessPassKind>())
            {
                Add(ref hash, (int)pass);
            }
        }

        private static void AddModuleData(ref ulong hash, MapGenDraftModuleSet moduleData)
        {
            if (moduleData == null)
            {
                Add(ref hash, "no_module_data");
                return;
            }

            Add(ref hash, moduleData.ModuleSetId);
            AddBoundsContract(ref hash, moduleData.BoundsContract);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.FloorsA), moduleData.FloorsA);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.FloorsB), moduleData.FloorsB);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.WallsStraight), moduleData.WallsStraight);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.WallsCornerInside), moduleData.WallsCornerInside);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.WallsCornerOutside), moduleData.WallsCornerOutside);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.ExteriorCeilings), moduleData.ExteriorCeilings);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.InteriorCeilings), moduleData.InteriorCeilings);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.WholeDoors), moduleData.WholeDoors);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.HalfDoorFrames), moduleData.HalfDoorFrames);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.HalfDoorPanels), moduleData.HalfDoorPanels);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.PropCategories), moduleData.PropCategories);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.RequiredUniqueProps), moduleData.RequiredUniqueProps);
            AddModuleEntries(ref hash, nameof(MapGenDraftModuleSet.Blockers), moduleData.Blockers);
        }

        private static void AddPrefabPalette(ref ulong hash, MapGenDraftPrefabPalette palette)
        {
            if (palette == null)
            {
                Add(ref hash, "no_prefab_palette");
                return;
            }

            Add(ref hash, palette.RoomFloor != null ? palette.RoomFloor.name : string.Empty);
            Add(ref hash, palette.CorridorFloor != null ? palette.CorridorFloor.name : string.Empty);
            Add(ref hash, palette.Wall != null ? palette.Wall.name : string.Empty);
            Add(ref hash, palette.InsideCorner != null ? palette.InsideCorner.name : string.Empty);
            Add(ref hash, palette.OutsideCorner != null ? palette.OutsideCorner.name : string.Empty);
            Add(ref hash, palette.Ceiling != null ? palette.Ceiling.name : string.Empty);
            Add(ref hash, palette.Door != null ? palette.Door.name : string.Empty);
            Add(ref hash, palette.Blocker != null ? palette.Blocker.name : string.Empty);
            Add(ref hash, palette.Prop != null ? palette.Prop.name : string.Empty);
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

        private static void AddModuleEntries(ref ulong hash, string fieldName, MapGenModuleEntry[] entries)
        {
            Add(ref hash, fieldName);
            Add(ref hash, entries?.Length ?? 0);
            foreach (var entry in entries ?? Array.Empty<MapGenModuleEntry>())
            {
                if (entry == null)
                {
                    Add(ref hash, "null_module_entry");
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

        private static void AddStyleSet(ref ulong hash, MapGenStyleSetAsset styleSet)
        {
            if (styleSet == null)
            {
                Add(ref hash, "no_style");
                return;
            }

            Add(ref hash, styleSet.StyleId);
            Add(ref hash, styleSet.LightingPreset);
            AddRoomTemplates(ref hash, styleSet.RoomTemplates);
            AddCorridorTemplates(ref hash, styleSet.CorridorTemplates);
        }

        private static void AddRuleSet(ref ulong hash, MapGenRuleSetAsset ruleSet)
        {
            if (ruleSet == null)
            {
                Add(ref hash, "no_rules");
                return;
            }

            Add(ref hash, ruleSet.MinRooms);
            Add(ref hash, ruleSet.MaxRooms);
            Add(ref hash, ruleSet.MinCorridorCells);
            Add(ref hash, ruleSet.MaxCorridorCells);
            Add(ref hash, ruleSet.LoopRate);
            Add(ref hash, ruleSet.ReduceDeadEnds ? 1 : 0);
            Add(ref hash, ruleSet.UseDirectRoutes ? 1 : 0);
            Add(ref hash, ruleSet.SplitLargeRooms ? 1 : 0);
            Add(ref hash, ruleSet.RemoveSmallRooms ? 1 : 0);
            AddCategories(ref hash, ruleSet.RequiredRoomCategories);
            AddCategories(ref hash, ruleSet.OptionalRoomCategories);
            Add(ref hash, ruleSet.QuantityRules.MinRooms);
            Add(ref hash, ruleSet.QuantityRules.MaxRooms);
            Add(ref hash, ruleSet.QuantityRules.MinCorridorCells);
            Add(ref hash, ruleSet.QuantityRules.MaxCorridorCells);
            Add(ref hash, ruleSet.QuantityRules.TargetRoomDensityPercent);
            Add(ref hash, ruleSet.QuantityRules.TargetCorridorDensityPercent);
            AddCategories(ref hash, ruleSet.QuantityRules.RequiredCategories);
            AddCategories(ref hash, ruleSet.QuantityRules.OptionalCategories);
            Add(ref hash, ruleSet.DistanceRules.MinStartToExitDistance);
            Add(ref hash, ruleSet.DistanceRules.MinStartToBossDistance);
            Add(ref hash, ruleSet.DistanceRules.RequireQuestBeforeBoss ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.UseDirectRoutes ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.ReduceDeadEnds ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.SplitLargeRooms ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.RemoveSmallRooms ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.ConsolidatePaths ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.AddLoops ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.NormalizeRouteLengths ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.WidenCleanCorridors ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.MergeCompatibleAdjacentRooms ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.FillEnclosedEmptySpace ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.FillReservedMasks ? 1 : 0);
            Add(ref hash, ruleSet.PostProcessRules.MaxPasses);
            foreach (var pass in ruleSet.PostProcessRules.PassOrder ?? Array.Empty<MapGenPostProcessPassKind>())
            {
                Add(ref hash, (int)pass);
            }

            AddPropRules(ref hash, ruleSet.PropPlacementRules);
        }

        private static void AddRoomTemplates(ref ulong hash, MapGenRoomTemplateAsset[] templates)
        {
            Add(ref hash, templates?.Length ?? 0);
            foreach (var template in templates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                if (template == null)
                {
                    Add(ref hash, "null_room_template");
                    continue;
                }

                Add(ref hash, template.TemplateId);
                Add(ref hash, template.Footprint.x);
                Add(ref hash, template.Footprint.y);
                Add(ref hash, (int)template.RoomCategory);
                Add(ref hash, (int)template.SizeClass);
                Add(ref hash, template.Weight);
                AddConnectors(ref hash, template.Connectors);
                AddCells(ref hash, template.FloorCells);
                AddCells(ref hash, template.WallCells);
                AddCells(ref hash, template.BlockedCells);
                AddCells(ref hash, template.DoorHintCells);
                AddPropChannels(ref hash, template.PropChannels);
                AddRoomShapes(ref hash, template.SourceRoomShapes);
            }
        }

        private static void AddCorridorTemplates(ref ulong hash, MapGenCorridorTemplateAsset[] templates)
        {
            Add(ref hash, templates?.Length ?? 0);
            foreach (var template in templates ?? Array.Empty<MapGenCorridorTemplateAsset>())
            {
                if (template == null)
                {
                    Add(ref hash, "null_corridor_template");
                    continue;
                }

                Add(ref hash, template.TemplateId);
                Add(ref hash, (int)template.CorridorKind);
                Add(ref hash, template.Width);
                Add(ref hash, (int)template.TurnKind);
                Add(ref hash, template.LengthRange.x);
                Add(ref hash, template.LengthRange.y);
                Add(ref hash, template.Weight);
                AddConnectors(ref hash, template.Connectors);
                AddPropChannels(ref hash, template.PropChannels);
            }
        }

        private static void AddRoomShapes(ref ulong hash, MapGenRoomShapeAsset[] shapes)
        {
            Add(ref hash, shapes?.Length ?? 0);
            foreach (var shape in shapes ?? Array.Empty<MapGenRoomShapeAsset>())
            {
                if (shape == null)
                {
                    Add(ref hash, "null_shape");
                    continue;
                }

                Add(ref hash, shape.ShapeId);
                Add(ref hash, shape.Width);
                Add(ref hash, shape.Height);
            }
        }

        private static void AddConnectors(ref ulong hash, MapGenConnector[] connectors)
        {
            Add(ref hash, connectors?.Length ?? 0);
            foreach (var connector in connectors ?? Array.Empty<MapGenConnector>())
            {
                Add(ref hash, (int)connector.Side);
                Add(ref hash, connector.LocalCell.x);
                Add(ref hash, connector.LocalCell.y);
                Add(ref hash, (int)connector.SocketKind);
                Add(ref hash, connector.SocketId);
                Add(ref hash, connector.Width);
            }
        }

        private static void AddPropChannels(ref ulong hash, MapGenTemplatePropChannel[] channels)
        {
            Add(ref hash, channels?.Length ?? 0);
            foreach (var channel in channels ?? Array.Empty<MapGenTemplatePropChannel>())
            {
                Add(ref hash, channel.Channel);
                Add(ref hash, channel.Weight);
                Add(ref hash, channel.LocalCell.x);
                Add(ref hash, channel.LocalCell.y);
            }
        }

        private static void AddPropRules(ref ulong hash, MapGenPropPlacementRules[] rules)
        {
            Add(ref hash, rules?.Length ?? 0);
            foreach (var rule in rules ?? Array.Empty<MapGenPropPlacementRules>())
            {
                Add(ref hash, rule.Channel);
                Add(ref hash, (int)rule.ChannelKind);
                Add(ref hash, (int)rule.DistributionMode);
                AddStrings(ref hash, rule.RoomCategoryFilters);
                AddStrings(ref hash, rule.CorridorKindFilters);
                Add(ref hash, rule.DensityPercent);
                Add(ref hash, rule.MinSpacingCells);
                Add(ref hash, rule.AllowTraversalBlocking ? 1 : 0);
                Add(ref hash, rule.RequiredUnique ? 1 : 0);
            }
        }

        private static void AddCategories(ref ulong hash, MapGenRoomCategory[] categories)
        {
            Add(ref hash, categories?.Length ?? 0);
            foreach (var category in categories ?? Array.Empty<MapGenRoomCategory>())
            {
                Add(ref hash, (int)category);
            }
        }

        private static void AddCells(ref ulong hash, Vector2Int[] cells)
        {
            Add(ref hash, cells?.Length ?? 0);
            foreach (var cell in cells ?? Array.Empty<Vector2Int>())
            {
                Add(ref hash, cell.x);
                Add(ref hash, cell.y);
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

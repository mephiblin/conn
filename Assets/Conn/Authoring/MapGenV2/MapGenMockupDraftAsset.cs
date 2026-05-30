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
        public int Seed;
        public Vector2Int GridSize = new Vector2Int(32, 32);
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

        public string CurrentSourceSignature => MapGenMockupSourceSignature.Build(Profile);

        public MapGenValidationReport GenerateFromProfile()
        {
            return GenerateFromProfile(false, null);
        }

        public MapGenValidationReport GenerateFromProfile(Func<bool> shouldCancel)
        {
            return GenerateFromProfile(false, shouldCancel);
        }

        public MapGenValidationReport RegenerateUnlockedFromProfile()
        {
            return GenerateFromProfile(true, null);
        }

        public MapGenValidationReport RegenerateUnlockedFromProfile(Func<bool> shouldCancel)
        {
            return GenerateFromProfile(true, shouldCancel);
        }

        public MapGenValidationReport RegenerateRegionFromProfile(int regionId)
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

            if (Profile == null)
            {
                var report = new MapGenValidationReport();
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "mockup_draft_missing_profile",
                    "Mockup draft has no profile.",
                    "Assign a MapGenProfileAsset before generating."));
                return report;
            }

            var profileReport = Profile.Validate();
            if (!profileReport.IsValid)
            {
                return profileReport;
            }

            var previousWidth = Width;
            var previousHeight = Height;
            var previousCells = Cells ?? Array.Empty<MapGenMockupCell>();
            var previousOverrides = RegionOverrides ?? Array.Empty<MapGenMockupRegionOverride>();
            var usesTemplates = MapGenTemplateMockupSolver.CanUseTemplates(Profile);
            var result = usesTemplates
                ? MapGenTemplateMockupSolver.Generate(Profile, Seed)
                : MapGenMockupSolver.Generate(
                    Profile.MapSize.x,
                    Profile.MapSize.y,
                    Seed,
                    Profile.LayoutRules != null ? Profile.LayoutRules.RequiredRoomCategories : Array.Empty<MapGenRoomCategory>());
            if (!result.Success)
            {
                return result.Report;
            }

            GridSize = new Vector2Int(result.Width, result.Height);
            Cells = result.Cells;
            if (!usesTemplates)
            {
                AssignRoomShapeIdsFromProfile();
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
            if (Profile == null)
            {
                var report = new MapGenValidationReport();
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "mockup_draft_missing_profile",
                    "Mockup draft has no profile.",
                    "Assign a MapGenProfileAsset before generating."));
                return report;
            }

            var profileReport = Profile.Validate();
            if (!profileReport.IsValid)
            {
                return profileReport;
            }

            var usesTemplates = MapGenTemplateMockupSolver.CanUseTemplates(Profile);
            var result = usesTemplates
                ? MapGenTemplateMockupSolver.Generate(Profile, Seed, shouldCancel: shouldCancel)
                : MapGenMockupSolver.Generate(
                    Profile.MapSize.x,
                    Profile.MapSize.y,
                    Seed,
                    Profile.LayoutRules != null ? Profile.LayoutRules.RequiredRoomCategories : Array.Empty<MapGenRoomCategory>(),
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
                AssignRoomShapeIdsFromProfile();
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
            return ApplyPostProcessingFromProfile(null);
        }

        public MapGenPostProcessReport ApplyPostProcessingFromProfile(Func<bool> shouldCancel)
        {
            EnsureCellArray();
            var options = new MapGenPostProcessOptions();
            if (Profile != null && Profile.LayoutRules != null)
            {
                var rules = Profile.LayoutRules.PostProcessRules;
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
            }

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
            return MapGenMockupSignature.Build(Width, Height, Seed, Cells, MapGenMockupSourceSignature.Build(Profile));
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
            RegionOverrides ??= Array.Empty<MapGenMockupRegionOverride>();
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

        private void AssignRoomShapeIdsFromProfile()
        {
            if (Profile == null || Profile.RoomShapes == null || Profile.RoomShapes.Length == 0)
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
            foreach (var shape in Profile.RoomShapes ?? Array.Empty<MapGenRoomShapeAsset>())
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

    internal static class MapGenMockupSourceSignature
    {
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

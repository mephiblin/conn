using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Mockup Draft", fileName = "MapGenMockupDraft")]
    public sealed class MapGenMockupDraftAsset : ScriptableObject
    {
        public MapGenProfileAsset Profile;
        public int Seed;
        public Vector2Int GridSize = new Vector2Int(32, 32);
        public MapGenMockupCell[] Cells = Array.Empty<MapGenMockupCell>();
        public bool Accepted;
        public string AcceptedSignature = string.Empty;
        public string LastGeneratedSignature = string.Empty;
        public int LastDirectRouteCellsAdded;
        public int LastDeadEndCorridorsRemoved;
        public int LastIsolatedRoomsRemoved;
        public MapGenMockupRegionOverride[] RegionOverrides = Array.Empty<MapGenMockupRegionOverride>();

        public int Width => Mathf.Max(1, GridSize.x);

        public int Height => Mathf.Max(1, GridSize.y);

        public bool IsAcceptedSignatureCurrent => !Accepted || AcceptedSignature == ComputeSignature();

        public MapGenValidationReport GenerateFromProfile()
        {
            return GenerateFromProfile(false);
        }

        public MapGenValidationReport RegenerateUnlockedFromProfile()
        {
            return GenerateFromProfile(true);
        }

        private MapGenValidationReport GenerateFromProfile(bool preserveLockedRegions)
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
            RegionOverrides = preserveLockedRegions
                ? CopyLockedOverrides(previousOverrides)
                : Array.Empty<MapGenMockupRegionOverride>();
            ClearPostProcessReport();
            return result.Report;
        }

        public MapGenPostProcessReport ApplyPostProcessingFromProfile()
        {
            EnsureCellArray();
            var options = new MapGenPostProcessOptions();
            if (Profile != null && Profile.LayoutRules != null)
            {
                options.UseDirectRoutes = Profile.LayoutRules.UseDirectRoutes;
                options.ReduceDeadEnds = Profile.LayoutRules.ReduceDeadEnds;
                options.RemoveSmallRooms = Profile.LayoutRules.RemoveSmallRooms;
            }

            var report = MapGenMockupPostProcessor.Apply(Width, Height, Cells, options);
            LastDirectRouteCellsAdded = report.DirectRouteCellsAdded;
            LastDeadEndCorridorsRemoved = report.DeadEndCorridorsRemoved;
            LastIsolatedRoomsRemoved = report.IsolatedRoomsRemoved;
            MapGenMockupRegionUtility.AssignCorridorRegionIds(Width, Height, Cells, true);
            LastGeneratedSignature = ComputeSignature();
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
        }

        public string ComputeSignature()
        {
            return MapGenMockupSignature.Build(Width, Height, Seed, Cells);
        }

        public void Accept()
        {
            EnsureCellArray();
            Accepted = true;
            AcceptedSignature = ComputeSignature();
            LastGeneratedSignature = AcceptedSignature;
        }

        public void ClearAcceptance()
        {
            Accepted = false;
            AcceptedSignature = string.Empty;
        }

        public void Reject()
        {
            ClearAcceptance();
        }

        public void ClearDraft()
        {
            Cells = CreateEmptyCells(Width, Height);
            LastGeneratedSignature = string.Empty;
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
            cell.PropChannel = string.Empty;
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
}

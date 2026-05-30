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

        public int Width => Mathf.Max(1, GridSize.x);

        public int Height => Mathf.Max(1, GridSize.y);

        public bool IsAcceptedSignatureCurrent => !Accepted || AcceptedSignature == ComputeSignature();

        public MapGenValidationReport GenerateFromProfile()
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

            var result = MapGenTemplateMockupSolver.CanUseTemplates(Profile)
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
            Accepted = false;
            AcceptedSignature = string.Empty;
            LastGeneratedSignature = result.Signature;
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
            ClearPostProcessReport();
            ClearAcceptance();
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

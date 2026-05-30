using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using NUnit.Framework;
using UnityEngine;

namespace Conn.Tests.EditMode
{
    public sealed class MapGenV2FoundationTests
    {
        [Test]
        public void GridCoordConvertsToAndFromIndex()
        {
            var coord = new MapGenGridCoord(2, 3);

            var index = coord.ToIndex(8);
            var restored = MapGenGridCoord.FromIndex(index, 8);

            Assert.That(index, Is.EqualTo(26));
            Assert.That(restored, Is.EqualTo(coord));
        }

        [Test]
        public void RoomShapeValidatorRejectsConnectorAwayFromEdge()
        {
            var cells = new MapGenShapeCell[9];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenShapeCell.Empty;
            }

            cells[4] = new MapGenShapeCell
            {
                State = MapGenCellState.Connector,
                SocketKind = MapGenSocketKind.Door,
                SocketId = "door"
            };

            var report = MapGenRoomShapeValidator.Validate(3, 3, cells);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "room_shape_connector_not_on_edge"));
        }

        [Test]
        public void ModuleSetValidatorRequiresBasicFloorAndWallModules()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();

            try
            {
                var report = moduleSet.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(2).Matches<MapGenIssue>(
                    issue => issue.Code == "module_set_missing_required_category"));
            }
            finally
            {
                Object.DestroyImmediate(moduleSet);
            }
        }

        [Test]
        public void ProfileValidatorRequiresStyleRuleAndRoomShapes()
        {
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();

            try
            {
                var report = profile.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_missing_style_set"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_missing_rule_set"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_missing_room_shapes"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MockupDraftAcceptanceStoresCurrentSignature()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                draft.GridSize = new Vector2Int(2, 2);
                draft.Seed = 17;
                draft.EnsureCellArray();
                draft.Cells[0].State = MapGenCellState.Room;

                draft.Accept();

                Assert.That(draft.Accepted, Is.True);
                Assert.That(draft.AcceptedSignature, Is.EqualTo(draft.ComputeSignature()));
                Assert.That(draft.IsAcceptedSignatureCurrent, Is.True);

                draft.Cells[1].State = MapGenCellState.Corridor;

                Assert.That(draft.IsAcceptedSignatureCurrent, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void MockupPreviewDataExtractsCellsAndSummary()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                draft.GridSize = new Vector2Int(3, 2);
                draft.Seed = 99;
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 7,
                    RoomCategory = MapGenRoomCategory.Start
                };
                draft.Cells[1] = new MapGenMockupCell
                {
                    State = MapGenCellState.Corridor,
                    RegionId = -1,
                    PropChannel = "floor_loot"
                };
                draft.Cells[2] = new MapGenMockupCell
                {
                    State = MapGenCellState.Blocked,
                    RegionId = -1
                };
                draft.LastGeneratedSignature = draft.ComputeSignature();
                draft.Accept();

                var preview = MapGenMockupPreviewData.FromDraft(draft);

                Assert.That(preview.Width, Is.EqualTo(3));
                Assert.That(preview.Height, Is.EqualTo(2));
                Assert.That(preview.Seed, Is.EqualTo(99));
                Assert.That(preview.LastGeneratedSignature, Is.EqualTo(draft.LastGeneratedSignature));
                Assert.That(preview.Accepted, Is.True);
                Assert.That(preview.AcceptedSignatureCurrent, Is.True);
                Assert.That(preview.Summary.RoomCells, Is.EqualTo(1));
                Assert.That(preview.Summary.CorridorCells, Is.EqualTo(1));
                Assert.That(preview.Summary.BlockedCells, Is.EqualTo(1));
                Assert.That(preview.Summary.PropChannelCells, Is.EqualTo(1));
                Assert.That(preview.Summary.RegionCount, Is.EqualTo(1));

                Assert.That(preview.TryGetCell(0, 0, out var roomCell), Is.True);
                Assert.That(roomCell.RegionId, Is.EqualTo(7));
                Assert.That(roomCell.RoomCategory, Is.EqualTo(MapGenRoomCategory.Start));
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void MockupSolverIsDeterministicForSameSeed()
        {
            var required = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Quest, MapGenRoomCategory.Exit };

            var first = MapGenMockupSolver.Generate(8, 6, 42, required);
            var second = MapGenMockupSolver.Generate(8, 6, 42, required);

            Assert.That(first.Success, Is.True);
            Assert.That(second.Signature, Is.EqualTo(first.Signature));
        }

        [Test]
        public void MockupSolverPlacesRequiredRooms()
        {
            var required = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Boss, MapGenRoomCategory.Exit };

            var result = MapGenMockupSolver.Generate(8, 6, 42, required);

            Assert.That(result.Success, Is.True);
            foreach (var category in required)
            {
                Assert.That(result.Cells, Has.Exactly(1).Matches<MapGenMockupCell>(
                    cell => cell.State == MapGenCellState.Room && cell.RoomCategory == category));
            }
        }

        [Test]
        public void PostProcessorCanAddDirectRoute()
        {
            var cells = new MapGenMockupCell[25];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            cells[0] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Start };
            cells[24] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Exit };

            var report = MapGenMockupPostProcessor.Apply(5, 5, cells, new MapGenPostProcessOptions
            {
                UseDirectRoutes = true
            });

            Assert.That(report.DirectRouteCellsAdded, Is.GreaterThan(0));
            Assert.That(report.Changed, Is.True);
        }

        [Test]
        public void MaterializationClassifierCreatesFloorAndWallRequests()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Start }
            };

            var requests = MapGenMaterializationClassifier.Classify(1, 1, cells);

            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.FloorA));
            Assert.That(requests, Has.Exactly(4).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.WallStraight));
            Assert.That(requests, Has.Exactly(4).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.WallCornerOutside));
        }

        [Test]
        public void PropPlacementValidatorRejectsNonNavigableChannel()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Empty, PropChannel = "floor_loot" }
            };

            var report = MapGenPropPlacementValidator.Validate(1, 1, cells);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "prop_channel_on_non_navigable_cell"));
        }

        [Test]
        public void RuntimeBakeBuilderCreatesTraversalEdges()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room },
                new MapGenMockupCell { State = MapGenCellState.Corridor }
            };

            var bakedCells = MapGenRuntimeBakeDataBuilder.BuildCells(2, 1, cells);
            var edges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(2, 1, cells);

            Assert.That(bakedCells, Has.Length.EqualTo(2));
            Assert.That(edges, Has.Length.EqualTo(1));
        }
    }
}

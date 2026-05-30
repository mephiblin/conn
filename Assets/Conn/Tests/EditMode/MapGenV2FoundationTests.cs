using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using Conn.MapGenV2.Editor;
using NUnit.Framework;
using UnityEditor;
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
        public void RoomShapeResizePreservesExistingCells()
        {
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();

            try
            {
                roomShape.Resize(new Vector2Int(2, 2));
                roomShape.SetCell(1, 1, new MapGenShapeCell
                {
                    State = MapGenCellState.Room,
                    SocketKind = MapGenSocketKind.None,
                    SocketId = string.Empty
                });

                roomShape.Resize(new Vector2Int(3, 3));

                Assert.That(roomShape.GetCell(1, 1).State, Is.EqualTo(MapGenCellState.Room));
                Assert.That(roomShape.GetCell(2, 2).State, Is.EqualTo(MapGenCellState.Empty));
            }
            finally
            {
                Object.DestroyImmediate(roomShape);
            }
        }

        [Test]
        public void RoomShapeRotateAndFlipPreserveCellPayloads()
        {
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();

            try
            {
                roomShape.Resize(new Vector2Int(2, 3));
                roomShape.SetCell(0, 2, new MapGenShapeCell
                {
                    State = MapGenCellState.Connector,
                    SocketKind = MapGenSocketKind.Door,
                    SocketId = "north_door"
                });

                roomShape.RotateClockwise();

                Assert.That(roomShape.Dimensions, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(roomShape.GetCell(0, 0).State, Is.EqualTo(MapGenCellState.Connector));
                Assert.That(roomShape.GetCell(0, 0).SocketId, Is.EqualTo("north_door"));

                roomShape.FlipHorizontal();

                Assert.That(roomShape.GetCell(2, 0).State, Is.EqualTo(MapGenCellState.Connector));

                roomShape.FlipVertical();

                Assert.That(roomShape.GetCell(2, 1).State, Is.EqualTo(MapGenCellState.Connector));
            }
            finally
            {
                Object.DestroyImmediate(roomShape);
            }
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
        public void RoomTemplateValidatorReportsConnectorAndFootprintIssues()
        {
            var template = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();

            try
            {
                template.TemplateId = "invalid_room_template";
                template.Footprint = new Vector2Int(2, 2);
                template.FloorCells = new[] { new Vector2Int(4, 4) };
                template.Connectors = new[]
                {
                    new MapGenConnector
                    {
                        Side = MapGenGridDirection.North,
                        LocalCell = new Vector2Int(0, 0),
                        SocketKind = MapGenSocketKind.Door,
                        SocketId = "main",
                        Width = 1
                    }
                };

                var report = template.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "room_template_cell_out_of_bounds"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "template_connector_not_on_side"));
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        [Test]
        public void CorridorTemplateValidatorRequiresConnectorsAndLengthRange()
        {
            var template = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();

            try
            {
                template.TemplateId = "invalid_corridor_template";
                template.Width = 1;
                template.LengthRange = new Vector2Int(5, 2);
                template.Connectors = System.Array.Empty<MapGenConnector>();

                var report = template.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "corridor_template_invalid_length_range"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "corridor_template_missing_connectors"));
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        [Test]
        public void ConnectorCompatibilityRequiresOppositeSideSocketAndWidth()
        {
            var roomConnector = MapGenConnector.Door(MapGenGridDirection.East, new Vector2Int(2, 1), "main");
            var matchingCorridorConnector = MapGenConnector.Door(MapGenGridDirection.West, Vector2Int.zero, "main");
            var wrongSideConnector = MapGenConnector.Door(MapGenGridDirection.North, Vector2Int.zero, "main");
            var wrongSocketConnector = MapGenConnector.Door(MapGenGridDirection.West, Vector2Int.zero, "side");

            Assert.That(MapGenTemplateValidationUtility.AreCompatible(roomConnector, matchingCorridorConnector), Is.True);
            Assert.That(MapGenTemplateValidationUtility.AreCompatible(roomConnector, wrongSideConnector), Is.False);
            Assert.That(MapGenTemplateValidationUtility.AreCompatible(roomConnector, wrongSocketConnector), Is.False);
        }

        [Test]
        public void StyleSetValidatorIncludesExplicitTemplateIssues()
        {
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var roomTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateMinimumModuleSet(moduleSet, out floor, out wall);
                styleSet.ModuleSet = moduleSet;
                styleSet.RoomTemplates = new[] { roomTemplate };

                var report = styleSet.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "room_template_missing_id"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "room_template_missing_floor_cells"));
            }
            finally
            {
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(roomTemplate);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void RuleSetValidatorReportsStructuredQuantityAndPropIssues()
        {
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();

            try
            {
                ruleSet.MinRooms = 2;
                ruleSet.MaxRooms = 1;
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.TargetRoomDensityPercent = 101;
                ruleSet.PostProcessRules = MapGenPostProcessRules.Defaults();
                ruleSet.PostProcessRules.MaxPasses = -1;
                ruleSet.PropPlacementRules = new[]
                {
                    new MapGenPropPlacementRules
                    {
                        Channel = string.Empty,
                        DensityPercent = 125,
                        MinSpacingCells = -1
                    }
                };

                var report = ruleSet.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "rule_set_invalid_room_range"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "rule_set_invalid_room_density"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "rule_set_invalid_post_process_passes"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "rule_set_prop_rule_missing_channel"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "rule_set_prop_rule_invalid_density"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "rule_set_prop_rule_invalid_spacing"));
            }
            finally
            {
                Object.DestroyImmediate(ruleSet);
            }
        }

        [Test]
        public void ProfileValidatorReportsMissingOutputFolders()
        {
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.OutputSettings = MapGenOutputSettings.Defaults();
                profile.OutputSettings.DraftFolder = string.Empty;
                profile.OutputSettings.MaterializedPrefabFolder = string.Empty;
                profile.OutputSettings.BakedAssetFolder = string.Empty;

                var report = profile.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "output_settings_missing_draft_folder"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "output_settings_missing_materialized_folder"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "output_settings_missing_baked_folder"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
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
        public void MockupDraftClearRemovesGeneratedAndAcceptedState()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                draft.GridSize = new Vector2Int(2, 2);
                draft.Seed = 17;
                draft.EnsureCellArray();
                draft.Cells[0].State = MapGenCellState.Room;
                draft.LastGeneratedSignature = draft.ComputeSignature();
                draft.LastDirectRouteCellsAdded = 2;
                draft.LastDeadEndCorridorsRemoved = 1;
                draft.LastIsolatedRoomsRemoved = 1;
                draft.Accept();

                draft.ClearDraft();

                Assert.That(draft.LastGeneratedSignature, Is.Empty);
                Assert.That(draft.Accepted, Is.False);
                Assert.That(draft.AcceptedSignature, Is.Empty);
                Assert.That(draft.LastDirectRouteCellsAdded, Is.Zero);
                Assert.That(draft.LastDeadEndCorridorsRemoved, Is.Zero);
                Assert.That(draft.LastIsolatedRoomsRemoved, Is.Zero);
                Assert.That(draft.Cells, Has.All.Matches<MapGenMockupCell>(cell => cell.State == MapGenCellState.Empty));
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void MockupDraftRegionOverridePersistsLockAndCategoryEditStalesAcceptance()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                draft.GridSize = new Vector2Int(2, 1);
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 4,
                    RoomCategory = MapGenRoomCategory.Start
                };
                draft.Cells[1] = new MapGenMockupCell
                {
                    State = MapGenCellState.Connector,
                    RegionId = 4,
                    RoomCategory = MapGenRoomCategory.Start,
                    SocketKind = MapGenSocketKind.Door,
                    SocketId = "main"
                };
                draft.Accept();

                draft.SetRegionLocked(4, true);
                draft.SetRegionCategory(4, MapGenRoomCategory.Boss);

                Assert.That(draft.TryGetRegionOverride(4, out var regionOverride), Is.True);
                Assert.That(regionOverride.Locked, Is.True);
                Assert.That(regionOverride.HasCategoryOverride, Is.True);
                Assert.That(regionOverride.CategoryOverride, Is.EqualTo(MapGenRoomCategory.Boss));
                Assert.That(draft.Cells, Has.Exactly(2).Matches<MapGenMockupCell>(
                    cell => cell.RegionId == 4 && cell.RoomCategory == MapGenRoomCategory.Boss));
                Assert.That(draft.Accepted, Is.True);
                Assert.That(draft.IsAcceptedSignatureCurrent, Is.False);

                draft.ClearRegionOverride(4);

                Assert.That(draft.TryGetRegionOverride(4, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void MockupDraftRegionStateEditsDeleteBlockAndReserveCells()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                draft.GridSize = new Vector2Int(3, 1);
                draft.EnsureCellArray();
                for (var i = 0; i < draft.Cells.Length; i++)
                {
                    draft.Cells[i] = new MapGenMockupCell
                    {
                        State = i == 1 ? MapGenCellState.Connector : MapGenCellState.Room,
                        RegionId = 7,
                        RoomCategory = MapGenRoomCategory.Main,
                        SocketKind = i == 1 ? MapGenSocketKind.Door : MapGenSocketKind.None,
                        SocketId = i == 1 ? "main" : string.Empty,
                        PropChannel = "loot"
                    };
                }

                draft.Accept();
                draft.SetRegionLocked(7, true);
                draft.SetRegionState(7, MapGenCellState.Blocked);

                Assert.That(draft.Cells, Has.Exactly(3).Matches<MapGenMockupCell>(
                    cell => cell.RegionId == 7
                        && cell.State == MapGenCellState.Blocked
                        && cell.SocketKind == MapGenSocketKind.Blocked
                        && string.IsNullOrEmpty(cell.SocketId)
                        && string.IsNullOrEmpty(cell.PropChannel)));
                Assert.That(draft.Accepted, Is.True);
                Assert.That(draft.IsAcceptedSignatureCurrent, Is.False);
                Assert.That(draft.TryGetRegionOverride(7, out var lockedOverride), Is.True);
                Assert.That(lockedOverride.Locked, Is.True);

                draft.SetRegionState(7, MapGenCellState.Reserved);

                Assert.That(draft.Cells, Has.Exactly(3).Matches<MapGenMockupCell>(
                    cell => cell.RegionId == 7
                        && cell.State == MapGenCellState.Reserved
                        && cell.SocketKind == MapGenSocketKind.None));

                draft.SetRegionState(7, MapGenCellState.Empty);

                Assert.That(draft.Cells, Has.Exactly(3).Matches<MapGenMockupCell>(
                    cell => cell.RegionId < 0 && cell.State == MapGenCellState.Empty));
                Assert.That(draft.TryGetRegionOverride(7, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void MockupDraftRegeneratePreservesLockedRegionCells()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(10, 6);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate };
                draft.Profile = profile;
                draft.Seed = 123;

                Assert.That(draft.GenerateFromProfile().IsValid, Is.True);
                draft.SetRegionCategory(0, MapGenRoomCategory.Boss);
                draft.SetRegionLocked(0, true);
                var before = CopyRegionCells(draft, 0);
                draft.Seed = 456;

                Assert.That(draft.RegenerateUnlockedFromProfile().IsValid, Is.True);

                Assert.That(draft.TryGetRegionOverride(0, out var regionOverride), Is.True);
                Assert.That(regionOverride.Locked, Is.True);
                Assert.That(regionOverride.CategoryOverride, Is.EqualTo(MapGenRoomCategory.Boss));
                Assert.That(CopyRegionCells(draft, 0), Is.EqualTo(before));
                Assert.That(draft.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => cell.RegionId != 0 && cell.State != MapGenCellState.Empty));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(exitTemplate);
                Object.DestroyImmediate(startTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void MockupDraftGenerationAssignsSelectableCorridorRegions()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(8, 4);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                draft.Profile = profile;
                draft.Seed = 321;

                Assert.That(draft.GenerateFromProfile().IsValid, Is.True);

                var corridorRegionId = System.Array.Find(
                    draft.Cells,
                    cell => cell.State == MapGenCellState.Corridor).RegionId;
                Assert.That(corridorRegionId, Is.GreaterThanOrEqualTo(2));
                var matchingCorridorCells = 0;
                foreach (var cell in draft.Cells)
                {
                    if (cell.State != MapGenCellState.Corridor)
                    {
                        continue;
                    }

                    Assert.That(cell.RegionId, Is.GreaterThanOrEqualTo(0));
                    if (cell.RegionId == corridorRegionId)
                    {
                        matchingCorridorCells++;
                    }
                }

                Assert.That(matchingCorridorCells, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void MockupDraftGenerationDoesNotCreateSceneObjects()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                draft.Profile = profile;
                draft.Seed = 44;
                var rootCountBeforeGenerate = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Length;

                Assert.That(draft.GenerateFromProfile().IsValid, Is.True);

                var rootCountAfterGenerate = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Length;
                Assert.That(rootCountAfterGenerate, Is.EqualTo(rootCountBeforeGenerate));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
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
        public void WorkflowStatusReportsNextActionAndDisabledReasons()
        {
            var emptyStatus = MapGenV2WorkflowStatus.From(null, null);
            Assert.That(emptyStatus.CanGenerate, Is.False);
            Assert.That(emptyStatus.GenerateReason, Is.EqualTo("Create or assign a draft first."));
            Assert.That(emptyStatus.NextAction, Is.EqualTo("Create Starter Setup or assign a profile."));

            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                draft.Profile = profile;
                draft.Seed = 11;

                var readyToGenerate = MapGenV2WorkflowStatus.From(profile, draft);
                Assert.That(readyToGenerate.CanGenerate, Is.True);
                Assert.That(readyToGenerate.NextAction, Is.EqualTo("Generate Mockup."));

                draft.GenerateFromProfile();
                var generated = MapGenV2WorkflowStatus.From(profile, draft);
                Assert.That(generated.CanAccept, Is.True);
                Assert.That(generated.CanMaterialize, Is.False);
                Assert.That(generated.MaterializeReason, Is.EqualTo("Accept Mockup first."));

                draft.Accept();
                var accepted = MapGenV2WorkflowStatus.From(profile, draft);
                Assert.That(accepted.CanMaterialize, Is.True);
                Assert.That(accepted.CanBakeRuntime, Is.True);

                draft.Cells[0].State = MapGenCellState.Blocked;
                var stale = MapGenV2WorkflowStatus.From(profile, draft);
                Assert.That(stale.CanMaterialize, Is.False);
                Assert.That(stale.MaterializeReason, Is.EqualTo("The accepted signature is stale. Accept the current mockup again."));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void GeneratedMapMarkerStoresDraftSourceData()
        {
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var root = new GameObject("GeneratedRoot");

            try
            {
                styleSet.StyleId = "marker_style";
                profile.ProfileId = "marker_profile";
                profile.StyleSet = styleSet;
                draft.Profile = profile;
                draft.Seed = 1234;
                draft.AcceptedSignature = "signature_a";

                var marker = root.AddComponent<MapGenV2GeneratedMapMarker>();
                marker.PopulateFromDraft(draft, "2026-05-31T00:00:00.0000000Z");

                Assert.That(marker.ProfileId, Is.EqualTo("marker_profile"));
                Assert.That(marker.Seed, Is.EqualTo(1234));
                Assert.That(marker.DraftSignature, Is.EqualTo("signature_a"));
                Assert.That(marker.StyleId, Is.EqualTo("marker_style"));
                Assert.That(marker.GeneratedUtc, Is.EqualTo("2026-05-31T00:00:00.0000000Z"));
                Assert.That(marker.SourceDraft, Is.SameAs(draft));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(styleSet);
            }
        }

        [Test]
        public void MaterializerCanFindReplaceAndClearGeneratedRoot()
        {
            const string tempFolder = "Assets/Conn/Tests/TempMapGenV2Materializer";
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject firstRoot = null;
            GameObject secondRoot = null;

            try
            {
                EnsureTempFolder(tempFolder);
                var floorPrefab = CreateTempPrefab($"{tempFolder}/Floor.prefab", "Floor");
                var wallPrefab = CreateTempPrefab($"{tempFolder}/Wall.prefab", "Wall");
                moduleSet.FloorsA = new[] { new MapGenModuleEntry { Prefab = floorPrefab, Weight = 1, Footprint = Vector2Int.one } };
                moduleSet.WallsStraight = new[] { new MapGenModuleEntry { Prefab = wallPrefab, Weight = 1, Footprint = Vector2Int.one } };
                styleSet.StyleId = "scene_controls_style";
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "scene_controls_profile";
                profile.StyleSet = styleSet;
                draft.Profile = profile;
                draft.Seed = 55;
                draft.GridSize = Vector2Int.one;
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 3,
                    RoomCategory = MapGenRoomCategory.Start
                };
                draft.Accept();

                firstRoot = MapGenMockupMaterializer.Materialize(draft, MapGenV2SceneOutputMode.ReplacePreviousRoot);
                Assert.That(firstRoot, Is.Not.Null);
                Assert.That(firstRoot.name, Is.EqualTo("MapGenV2_scene_controls_profile_55"));
                Assert.That(firstRoot.GetComponent<MapGenV2GeneratedMapMarker>(), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Floors"), Is.Not.Null);
                Assert.That(MapGenMockupMaterializer.FindExistingMarker(draft).gameObject, Is.SameAs(firstRoot));

                secondRoot = MapGenMockupMaterializer.Materialize(draft, MapGenV2SceneOutputMode.ReplacePreviousRoot);
                Assert.That(secondRoot, Is.Not.Null);
                Assert.That(secondRoot, Is.Not.SameAs(firstRoot));
                Assert.That(MapGenMockupMaterializer.FindExistingMarker(draft).gameObject, Is.SameAs(secondRoot));

                MapGenMockupMaterializer.ClearRoot(secondRoot);
                secondRoot = null;
                Assert.That(MapGenMockupMaterializer.FindExistingMarker(draft), Is.Null);
            }
            finally
            {
                if (firstRoot != null)
                {
                    Object.DestroyImmediate(firstRoot);
                }

                if (secondRoot != null)
                {
                    Object.DestroyImmediate(secondRoot);
                }

                AssetDatabase.DeleteAsset(tempFolder);
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
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
        public void TemplateMockupSolverPlacesMultiCellRequiredRoomsDeterministically()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(10, 6);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate };

                var first = MapGenTemplateMockupSolver.Generate(profile, 7001);
                var second = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(first.Success, Is.True);
                Assert.That(second.Signature, Is.EqualTo(first.Signature));
                Assert.That(first.Cells, Has.Exactly(4).Matches<MapGenMockupCell>(
                    cell => IsRoomFootprintCell(cell) && cell.RoomCategory == MapGenRoomCategory.Start));
                Assert.That(first.Cells, Has.Exactly(4).Matches<MapGenMockupCell>(
                    cell => IsRoomFootprintCell(cell) && cell.RoomCategory == MapGenRoomCategory.Exit));
                Assert.That(first.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => cell.State == MapGenCellState.Corridor));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(exitTemplate);
                Object.DestroyImmediate(startTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void TemplateMockupSolverReportsMissingRequiredCategoryTemplate()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                styleSet.RoomTemplates = new[] { startTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_no_template_for_category"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(startTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void TemplateMockupSolverUsesCompatibleCorridorTemplate()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var corridorTemplate = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(10, 6);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                PopulateCorridorTemplate(corridorTemplate, "main");
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate };
                styleSet.CorridorTemplates = new[] { corridorTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(result.Success, Is.True);
                Assert.That(result.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => cell.State == MapGenCellState.Corridor));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(corridorTemplate);
                Object.DestroyImmediate(exitTemplate);
                Object.DestroyImmediate(startTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void TemplateMockupSolverReportsIncompatibleCorridorTemplate()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var corridorTemplate = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(10, 6);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                PopulateCorridorTemplate(corridorTemplate, "side");
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate };
                styleSet.CorridorTemplates = new[] { corridorTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_missing_compatible_corridor_template"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(corridorTemplate);
                Object.DestroyImmediate(exitTemplate);
                Object.DestroyImmediate(startTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
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

        private static void PopulateValidWorkflowProfile(
            MapGenProfileAsset profile,
            MapGenStyleSetAsset styleSet,
            MapGenModuleSetAsset moduleSet,
            MapGenRuleSetAsset ruleSet,
            MapGenRoomShapeAsset roomShape,
            out GameObject floor,
            out GameObject wall)
        {
            PopulateMinimumModuleSet(moduleSet, out floor, out wall);
            roomShape.Resize(new Vector2Int(3, 3));
            roomShape.SetCell(1, 1, new MapGenShapeCell
            {
                State = MapGenCellState.Room,
                SocketKind = MapGenSocketKind.None,
                SocketId = string.Empty
            });
            styleSet.ModuleSet = moduleSet;
            profile.ProfileId = "workflow_profile";
            profile.MapSize = new Vector2Int(8, 6);
            profile.StyleSet = styleSet;
            profile.LayoutRules = ruleSet;
            profile.RoomShapes = new[] { roomShape };
        }

        private static void PopulateMinimumModuleSet(
            MapGenModuleSetAsset moduleSet,
            out GameObject floor,
            out GameObject wall)
        {
            floor = new GameObject("FloorModule");
            wall = new GameObject("WallModule");
            moduleSet.FloorsA = new[]
            {
                new MapGenModuleEntry
                {
                    Prefab = floor,
                    Weight = 1,
                    Footprint = Vector2Int.one
                }
            };
            moduleSet.WallsStraight = new[]
            {
                new MapGenModuleEntry
                {
                    Prefab = wall,
                    Weight = 1,
                    Footprint = Vector2Int.one
                }
            };
        }

        private static void PopulateRoomTemplate(
            MapGenRoomTemplateAsset template,
            string templateId,
            MapGenRoomCategory category)
        {
            template.TemplateId = templateId;
            template.RoomCategory = category;
            template.Footprint = new Vector2Int(2, 2);
            template.Weight = 1;
            template.FloorCells = new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            };
            template.Connectors = new[]
            {
                MapGenConnector.Door(MapGenGridDirection.East, new Vector2Int(1, 0), "main"),
                MapGenConnector.Door(MapGenGridDirection.West, new Vector2Int(0, 0), "main")
            };
        }

        private static void PopulateCorridorTemplate(MapGenCorridorTemplateAsset template, string socketId)
        {
            template.TemplateId = $"corridor_{socketId}";
            template.CorridorKind = MapGenCorridorKind.Straight;
            template.Width = 1;
            template.LengthRange = new Vector2Int(2, 6);
            template.Weight = 1;
            template.Connectors = new[]
            {
                MapGenConnector.Door(MapGenGridDirection.West, Vector2Int.zero, socketId),
                MapGenConnector.Door(MapGenGridDirection.East, new Vector2Int(5, 0), socketId)
            };
        }

        private static bool IsRoomFootprintCell(MapGenMockupCell cell)
        {
            return cell.State == MapGenCellState.Room || cell.State == MapGenCellState.Connector;
        }

        private static string CopyRegionCells(MapGenMockupDraftAsset draft, int regionId)
        {
            var signature = string.Empty;
            for (var i = 0; i < draft.Cells.Length; i++)
            {
                var cell = draft.Cells[i];
                if (cell.RegionId != regionId)
                {
                    continue;
                }

                signature += $"{i}:{cell.State}:{cell.RegionId}:{cell.RoomCategory}:{cell.SocketKind}:{cell.SocketId}|";
            }

            return signature;
        }

        private static void EnsureTempFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Conn/Tests"))
            {
                AssetDatabase.CreateFolder("Assets/Conn", "Tests");
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Conn/Tests", "TempMapGenV2Materializer");
            }
        }

        private static GameObject CreateTempPrefab(string path, string name)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }
    }
}

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

using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using Conn.MapGenV2.Editor;
using Conn.Core.Maps;
using Conn.Runtime.Maps;
using Conn.Runtime.Scenes;
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
        public void MapGenV2EditorTextLocalizesPrimaryWorkflowKeys()
        {
            var previous = MapGenV2EditorText.LanguagePreference;

            try
            {
                MapGenV2EditorText.LanguagePreference = MapGenV2EditorLanguage.Korean;

                Assert.That(MapGenV2EditorText.Get("mapgenv2.generateMockup"), Is.EqualTo("목업 생성"));
                Assert.That(MapGenV2EditorText.Get("mapgenv2.materializeToScene"), Is.EqualTo("씬 생성"));
                Assert.That(MapGenV2EditorText.Get("mapgenv2.profileMissing"), Is.EqualTo("프로필을 지정하세요."));

                MapGenV2EditorText.LanguagePreference = MapGenV2EditorLanguage.English;

                Assert.That(MapGenV2EditorText.Get("mapgenv2.generateMockup"), Is.EqualTo("Generate Mockup"));
                Assert.That(MapGenV2EditorText.Get("mapgenv2.materializeToScene"), Is.EqualTo("Materialize To Scene"));
                Assert.That(MapGenV2EditorText.Get("mapgenv2.profileMissing"), Is.EqualTo("Assign a profile."));
            }
            finally
            {
                MapGenV2EditorText.LanguagePreference = previous;
            }
        }

        [Test]
        public void MapGenV2EditorTextProvidesPseudoLocalizationLengthCheck()
        {
            var source = "Generate Mockup";
            var pseudo = MapGenV2EditorText.PseudoLocalize(source);

            Assert.That(pseudo.Length, Is.GreaterThan(source.Length));
            Assert.That(MapGenV2EditorText.IsLongTextSafe(source, pseudo.Length), Is.True);
            Assert.That(MapGenV2EditorText.IsLongTextSafe(source, source.Length), Is.False);
        }

        [Test]
        public void MapGenV2WindowDocumentsKoreanCoverage()
        {
            var summary = MapGenV2Window.BuildKoreanCoverageSummary();

            Assert.That(summary, Does.Contain("Korean UI coverage"));
            Assert.That(summary, Does.Contain("labels"));
            Assert.That(summary, Does.Contain("buttons"));
            Assert.That(summary, Does.Contain("tooltips"));
            Assert.That(summary, Does.Contain("warnings"));
            Assert.That(summary, Does.Contain("errors"));
            Assert.That(summary, Does.Contain("validation summaries"));
            Assert.That(summary, Does.Contain("documentation summaries"));
            Assert.That(summary, Does.Contain("workflow result text"));
            Assert.That(summary, Does.Contain("ids/enums/API names remain English"));
        }

        [Test]
        public void ManualChecklistSummaryCoversPhase15Checks()
        {
            var summary = MapGenV2ManualVerification.BuildManualChecklistSummary();

            Assert.That(summary, Does.Contain("starter setup"));
            Assert.That(summary, Does.Contain("several mockup seeds"));
            Assert.That(summary, Does.Contain("room-shape edit"));
            Assert.That(summary, Does.Contain("style-set swap"));
            Assert.That(summary, Does.Contain("real prefab materialization"));
            Assert.That(summary, Does.Contain("materialized prefab save"));
            Assert.That(summary, Does.Contain("runtime bake/load"));
            Assert.That(summary, Does.Contain("broken-profile diagnostics"));
        }

        [Test]
        public void MapGenV2WindowDefinesInspectorFriendlyMinimumSize()
        {
            Assert.That(MapGenV2Window.MinimumWindowSize.x, Is.LessThanOrEqualTo(480f));
            Assert.That(MapGenV2Window.MinimumWindowSize.x, Is.GreaterThanOrEqualTo(360f));
            Assert.That(MapGenV2Window.MinimumWindowSize.y, Is.GreaterThanOrEqualTo(620f));
        }

        [Test]
        public void MapGenV2WindowStatusStripIncludesWorkflowOutputs()
        {
            var status = MapGenV2Window.BuildTopStatusStripText(
                "profile_a",
                "draft_a",
                2001,
                "Valid",
                "Confirmed current",
                "MapGenV2_profile_a_2001",
                "Assets/Conn/Core/Maps/profile_a_2001_BakedMap.asset");

            Assert.That(status, Does.Contain("Profile profile_a"));
            Assert.That(status, Does.Contain("Current Mockup draft_a"));
            Assert.That(status, Does.Contain("Seed 2001"));
            Assert.That(status, Does.Contain("Validation Valid"));
            Assert.That(status, Does.Contain("Mockup State Confirmed current"));
            Assert.That(status, Does.Contain("Materialized Root MapGenV2_profile_a_2001"));
            Assert.That(status, Does.Contain("Baked Asset Assets/Conn/Core/Maps/profile_a_2001_BakedMap.asset"));
        }

        [Test]
        public void MapGenV2WindowOperationResultsAppendNextStepGuidance()
        {
            var result = MapGenV2Window.AppendNextStep(
                "Generate Mockup complete.",
                "Inspect the preview, then Confirm Mockup.");

            Assert.That(result, Is.EqualTo("Generate Mockup complete. Next: Inspect the preview, then Confirm Mockup."));
        }

        [Test]
        public void MapGenV2WindowDeclaresPreviewAsPrimaryMockupUx()
        {
            var summary = MapGenV2Window.BuildPrimaryMockupUxSummary();

            Assert.That(summary, Does.Contain("Primary mockup UX"));
            Assert.That(summary, Does.Contain("Generate"));
            Assert.That(summary, Does.Contain("selected-region inspect/edit"));
            Assert.That(summary, Does.Contain("Materialize"));
            Assert.That(summary, Does.Contain("Bake"));
            Assert.That(summary, Does.Contain("without requiring Scene View"));
            Assert.That(summary, Does.Contain("secondary inspection"));
        }

        [Test]
        public void MapGenV2WindowDeclaresInspectorAuthoringLayout()
        {
            var summary = MapGenV2Window.BuildInspectorLayoutSummary();
            var technology = MapGenV2Window.BuildEditorTechnologySummary();
            var help = MapGenV2Window.BuildInlineHelpCoverageSummary();

            Assert.That(summary, Does.Contain("Inspector-style authoring layout"));
            Assert.That(summary, Does.Contain("scene output"));
            Assert.That(summary, Does.Contain("runtime bake"));
            Assert.That(summary, Does.Contain("stacked vertically"));
            Assert.That(summary, Does.Contain("foldout sections"));
            Assert.That(summary, Does.Contain("utility sections"));
            Assert.That(summary, Does.Contain("responsive wrapping"));
            Assert.That(technology, Does.Contain("new Scene View overlay uses UI Toolkit"));
            Assert.That(technology, Does.Contain("existing MapGenV2 window and inspectors stay IMGUI"));
            Assert.That(technology, Does.Contain("preview drawing, Undo, and serialized inspector workflows"));
            Assert.That(help, Does.Contain("profile"));
            Assert.That(help, Does.Contain("rule set"));
            Assert.That(help, Does.Contain("style set"));
            Assert.That(help, Does.Contain("module set"));
            Assert.That(help, Does.Contain("room shape"));
            Assert.That(help, Does.Contain("connectors"));
            Assert.That(help, Does.Contain("post-process"));
            Assert.That(help, Does.Contain("prop placement"));
            Assert.That(help, Does.Contain("bake settings"));
        }

        [Test]
        public void MapGenV2WindowStateSummaryCoversPersistedPreviewAndFoldouts()
        {
            var state = new MapGenV2WindowStateSnapshot(
                "Assets/Profile.asset",
                "Assets/Draft.asset",
                18f,
                new Vector2(24f, 36f),
                true,
                new Vector2Int(3, 4),
                7,
                true,
                false,
                MapGenV2SceneOutputMode.UpdateSelectedRoot,
                false,
                MapGenV2EditorLanguage.Korean);

            var summary = MapGenV2Window.BuildPersistedWindowStateSummary(state);

            Assert.That(summary, Does.Contain("profile Assets/Profile.asset"));
            Assert.That(summary, Does.Contain("draft Assets/Draft.asset"));
            Assert.That(summary, Does.Contain("preview zoom 18"));
            Assert.That(summary, Does.Contain("pan 24,36"));
            Assert.That(summary, Does.Contain("selected cell 3,4"));
            Assert.That(summary, Does.Contain("selected region 7"));
            Assert.That(summary, Does.Contain("prop overlay True"));
            Assert.That(summary, Does.Contain("post overlay False"));
            Assert.That(summary, Does.Contain("output mode UpdateSelectedRoot"));
            Assert.That(summary, Does.Contain("diagnostics foldout False"));
            Assert.That(summary, Does.Contain("language Korean"));
        }

        [Test]
        public void MapGenV2WindowLinkedAssetShortcutSummaryListsReferenceActions()
        {
            var summary = MapGenV2Window.BuildLinkedAssetShortcutSummary();
            var validation = MapGenV2Window.BuildShortcutValidationSummary(null, new MapGenValidationReport());

            Assert.That(summary, Does.Contain("Ping"));
            Assert.That(summary, Does.Contain("Select"));
            Assert.That(summary, Does.Contain("Open"));
            Assert.That(summary, Does.Contain("Create"));
            Assert.That(summary, Does.Contain("Duplicate"));
            Assert.That(summary, Does.Contain("Validate"));
            Assert.That(summary, Does.Contain("Fix/Create Missing"));
            Assert.That(summary, Does.Contain("누락 생성"));
            Assert.That(validation, Does.Contain("Validate (none): Valid"));
        }

        [Test]
        public void MapGenV2WindowUndoCoverageSummaryListsEditorMutations()
        {
            var summary = MapGenV2Window.BuildUndoCoverageSummary();

            Assert.That(summary, Does.Contain("serialized inspector asset edits"));
            Assert.That(summary, Does.Contain("room-shape paint"));
            Assert.That(summary, Does.Contain("current mockup generate"));
            Assert.That(summary, Does.Contain("selected-region"));
            Assert.That(summary, Does.Contain("scene materialization"));
            Assert.That(summary, Does.Contain("Undo object creation/destruction"));
        }

        [Test]
        public void MapGenV2InspectorsKeepRawArraysInAdvancedFoldouts()
        {
            var profile = MapGenProfileAssetEditor.BuildInspectorUxSummary();
            var ruleSet = MapGenRuleSetAssetEditor.BuildInspectorUxSummary();
            var styleSet = MapGenStyleSetAssetEditor.BuildInspectorUxSummary();
            var moduleSet = MapGenModuleSetAssetEditor.BuildInspectorUxSummary();
            var roomTemplate = MapGenRoomTemplateAssetEditor.BuildInspectorUxSummary();
            var corridorTemplate = MapGenCorridorTemplateAssetEditor.BuildInspectorUxSummary();
            var draft = MapGenMockupDraftAssetEditor.BuildInspectorUxSummary();

            Assert.That(profile, Does.Contain("primary UX"));
            Assert.That(profile, Does.Contain("advanced/debug foldout"));
            Assert.That(profile, Does.Contain("room-shape arrays"));
            Assert.That(ruleSet, Does.Contain("advanced/debug foldout"));
            Assert.That(ruleSet, Does.Contain("raw serialized rule arrays"));
            Assert.That(styleSet, Does.Contain("primary UX"));
            Assert.That(styleSet, Does.Contain("advanced/debug foldout"));
            Assert.That(styleSet, Does.Contain("room-template"));
            Assert.That(moduleSet, Does.Contain("advanced/debug foldout"));
            Assert.That(moduleSet, Does.Contain("raw serialized module entry arrays"));
            Assert.That(roomTemplate, Does.Contain("primary UX"));
            Assert.That(roomTemplate, Does.Contain("advanced/debug foldout"));
            Assert.That(roomTemplate, Does.Contain("raw arrays"));
            Assert.That(corridorTemplate, Does.Contain("advanced/debug foldout"));
            Assert.That(corridorTemplate, Does.Contain("raw arrays"));
            Assert.That(draft, Does.Contain("primary UX"));
            Assert.That(draft, Does.Contain("preview-only"));
            Assert.That(draft, Does.Contain("generation, confirmation, materialization, bake"));
        }

        [Test]
        public void MapGenV2WindowPrimaryActionExplanationIncludesChangeReasonAndOutput()
        {
            var explanation = MapGenV2Window.BuildPrimaryActionExplanation(
                "Materialize To Scene",
                false,
                "Confirm Mockup first.",
                "instantiates prefab or placeholder modules",
                "Scene root MapGenV2_profile_2001");

            Assert.That(explanation, Does.Contain("Materialize To Scene"));
            Assert.That(explanation, Does.Contain("Disabled"));
            Assert.That(explanation, Does.Contain("Confirm Mockup first."));
            Assert.That(explanation, Does.Contain("Change: instantiates prefab or placeholder modules"));
            Assert.That(explanation, Does.Contain("Output: Scene root MapGenV2_profile_2001"));
        }

        [Test]
        public void SceneViewOverlaySummaryReportsDraftRootActionsAndToggles()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var root = new GameObject("MapGenV2_profile_2001");

            try
            {
                draft.name = "draft_a";
                draft.Accepted = true;
                draft.AcceptedSignature = draft.ComputeSignature();

                var summary = MapGenV2SceneViewOverlay.BuildOverlaySummary(draft, root, null, "Move");

                Assert.That(summary, Does.Contain("Current Mockup draft_a"));
                Assert.That(summary, Does.Contain("Root MapGenV2_profile_2001"));
                Assert.That(summary, Does.Contain("confirmed mockup"));
                Assert.That(summary, Does.Contain("Tool Move"));
                Assert.That(summary, Does.Contain("Generate/Confirm/Materialize/Clear/Frame"));
                Assert.That(summary, Does.Contain("Toggles Grid/Region IDs/Connectors/Sockets/Blocked/Props/Nav/Bounds/Diagnostics"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void SceneViewStateSummaryDistinguishesMockupSceneAndRuntimeStates()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var root = new GameObject("MapGenV2_profile_2001");
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                draft.GridSize = Vector2Int.one;
                draft.EnsureCellArray();
                draft.Seed = 2001;
                draft.Accept();
                var rootMarker = root.AddComponent<MapGenV2GeneratedMapMarker>();
                rootMarker.DraftSignature = "stale";
                baked.SourceSignature = "stale_bake";

                var summary = MapGenV2SceneViewOverlay.BuildSceneStateSummary(draft, root, baked);

                Assert.That(summary, Does.Contain("confirmed mockup"));
                Assert.That(summary, Does.Contain("stale materialized output"));
                Assert.That(summary, Does.Contain("stale baked runtime output"));
                Assert.That(MapGenV2SceneViewOverlay.BuildSceneStateSummary(draft, null, null), Does.Contain("no materialized output"));
                draft.ClearAcceptance();
                Assert.That(MapGenV2SceneViewOverlay.BuildSceneStateSummary(draft, null, null), Does.Contain("unconfirmed mockup"));
            }
            finally
            {
                Object.DestroyImmediate(baked);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void SceneViewVisibilitySummaryListsSeparateToggles()
        {
            var summary = MapGenV2SceneViewOverlay.BuildVisibilityToggleSummary();

            Assert.That(summary, Does.Contain("mockup grid"));
            Assert.That(summary, Does.Contain("region ids"));
            Assert.That(summary, Does.Contain("connectors"));
            Assert.That(summary, Does.Contain("sockets"));
            Assert.That(summary, Does.Contain("blocked cells"));
            Assert.That(summary, Does.Contain("prop channels"));
            Assert.That(summary, Does.Contain("nav graph"));
            Assert.That(summary, Does.Contain("prefab bounds"));
            Assert.That(summary, Does.Contain("diagnostics"));
        }

        [Test]
        public void SceneViewGizmoSummaryListsRequiredInspectionHandles()
        {
            var summary = MapGenV2SceneViewGizmos.BuildSceneGizmoSummary();

            Assert.That(summary, Does.Contain("generated bounds"));
            Assert.That(summary, Does.Contain("cell grid"));
            Assert.That(summary, Does.Contain("selected region outline"));
            Assert.That(summary, Does.Contain("room id"));
            Assert.That(summary, Does.Contain("connector arrows"));
            Assert.That(summary, Does.Contain("door/blocker markers"));
            Assert.That(summary, Does.Contain("prop channels"));
            Assert.That(summary, Does.Contain("stale materialized root"));
            Assert.That(summary, Does.Contain("nav/build bounds"));
        }

        [Test]
        public void ProfileInspectorSummaryIncludesAuthoringReadiness()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var corridorTemplate = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(12, 8);
                profile.Seed = 42;
                styleSet.StyleId = "ruins";
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateCorridorTemplate(corridorTemplate, "main");
                styleSet.RoomTemplates = new[] { startTemplate };
                styleSet.CorridorTemplates = new[] { corridorTemplate };
                ruleSet.QuantityRules.RequiredCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };

                var summary = MapGenProfileAssetEditor.BuildSummary(profile);

                Assert.That(summary, Does.Contain("Map 12x8"));
                Assert.That(summary, Does.Contain("Seed 42"));
                Assert.That(summary, Does.Contain("Style ruins"));
                Assert.That(summary, Does.Contain("Templates room 1/corridor 1"));
                Assert.That(summary, Does.Contain("Required Start, Exit"));
                Assert.That(summary, Does.Contain("Validation Valid"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(corridorTemplate);
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
        public void RoomShapeInspectorSummaryPrioritizesGridAuthoring()
        {
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();

            try
            {
                roomShape.ShapeId = "authoring_shape";
                roomShape.Resize(new Vector2Int(3, 2));
                roomShape.SetCell(0, 0, new MapGenShapeCell { State = MapGenCellState.Room });
                roomShape.SetCell(1, 0, new MapGenShapeCell { State = MapGenCellState.Blocked });
                roomShape.SetCell(2, 0, new MapGenShapeCell
                {
                    State = MapGenCellState.Connector,
                    SocketKind = MapGenSocketKind.Door,
                    SocketId = "main"
                });

                var summary = MapGenRoomShapeAssetEditor.BuildAuthoringSummary(roomShape);

                Assert.That(summary, Does.Contain("Grid 3x2"));
                Assert.That(summary, Does.Contain("Paint tools Room/Connector/Blocked"));
                Assert.That(summary, Does.Contain("Connectors 1"));
                Assert.That(summary, Does.Contain("Floors 1"));
                Assert.That(summary, Does.Contain("Blocked 1"));
                Assert.That(summary, Does.Contain("Rotate/Flip available"));
                Assert.That(summary, Does.Contain("Validation Valid"));
            }
            finally
            {
                Object.DestroyImmediate(roomShape);
            }
        }

        [Test]
        public void ModuleSetInspectorSummaryReportsCategoryCoverage()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateMinimumModuleSet(moduleSet, out floor, out wall);
                moduleSet.WallsCornerInside = moduleSet.WallsStraight;
                moduleSet.WallsCornerOutside = moduleSet.WallsStraight;
                moduleSet.InteriorCeilings = moduleSet.FloorsA;
                moduleSet.ExteriorCeilings = moduleSet.FloorsA;
                moduleSet.WholeDoors = moduleSet.WallsStraight;
                moduleSet.PropCategories = moduleSet.FloorsA;
                moduleSet.Blockers = moduleSet.WallsStraight;

                var summary = MapGenModuleSetAssetEditor.BuildCoverageSummary(moduleSet);

                Assert.That(summary, Does.Contain("Floors 1"));
                Assert.That(summary, Does.Contain("Walls 1"));
                Assert.That(summary, Does.Contain("Corners 2"));
                Assert.That(summary, Does.Contain("Ceilings 2"));
                Assert.That(summary, Does.Contain("Doors 1"));
                Assert.That(summary, Does.Contain("Blockers 1"));
                Assert.That(summary, Does.Contain("Props 1"));
                Assert.That(summary, Does.Contain("Missing required 0"));
                Assert.That(summary, Does.Contain("Validation Valid"));
            }
            finally
            {
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void RuleSetInspectorSummaryReportsDesignerControls()
        {
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();

            try
            {
                ruleSet.MinRooms = 3;
                ruleSet.MaxRooms = 9;
                ruleSet.MinCorridorCells = 5;
                ruleSet.MaxCorridorCells = 21;
                ruleSet.LoopRate = 35;
                ruleSet.ReduceDeadEnds = true;
                ruleSet.RequiredRoomCategories = new[]
                {
                    MapGenRoomCategory.Start,
                    MapGenRoomCategory.Exit,
                    MapGenRoomCategory.Boss
                };
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.TargetRoomDensityPercent = 30;
                ruleSet.QuantityRules.TargetCorridorDensityPercent = 18;
                ruleSet.DistanceRules = new MapGenDistanceRules
                {
                    MinStartToExitDistance = 7,
                    MinStartToBossDistance = 5,
                    RequireQuestBeforeBoss = true
                };
                ruleSet.PostProcessRules = MapGenPostProcessRules.Defaults();
                ruleSet.PostProcessRules.ReduceDeadEnds = true;
                ruleSet.PostProcessRules.AddLoops = true;
                ruleSet.PostProcessRules.FillReservedMasks = true;
                ruleSet.PostProcessRules.PassOrder = new[]
                {
                    MapGenPostProcessPassKind.ReduceDeadEnds,
                    MapGenPostProcessPassKind.AddLoops
                };
                ruleSet.PropPlacementRules = new[] { MapGenPropPlacementRules.Defaults() };
                ruleSet.PropPlacementRules[0].Channel = "loot";

                var summary = MapGenRuleSetAssetEditor.BuildDesignerSummary(ruleSet);

                Assert.That(summary, Does.Contain("Rooms 3-9"));
                Assert.That(summary, Does.Contain("Corridor cells 5-21"));
                Assert.That(summary, Does.Contain("Density room 30%/corridor 18%"));
                Assert.That(summary, Does.Contain("Loops 35%/Add loops pass"));
                Assert.That(summary, Does.Contain("Dead ends Reduce"));
                Assert.That(summary, Does.Contain("Blocked regions Fill reserved masks"));
                Assert.That(summary, Does.Contain("Required Start, Exit, Boss"));
                Assert.That(summary, Does.Contain("Distance start-exit 7, start-boss 5"));
                Assert.That(summary, Does.Contain("Post passes ReduceDeadEnds > AddLoops"));
                Assert.That(summary, Does.Contain("Prop rules 1"));
                Assert.That(summary, Does.Contain("Validation Valid"));
            }
            finally
            {
                Object.DestroyImmediate(ruleSet);
            }
        }

        [Test]
        public void BakedMapInspectorSummaryReportsRuntimeSafeData()
        {
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                baked.Width = 4;
                baked.Height = 3;
                baked.Cells = new[]
                {
                    new MapGenBakedCell { Coord = new MapGenGridCoord(0, 0), State = MapGenCellState.Room, RegionId = 1 }
                };
                baked.Regions = new[]
                {
                    new MapGenBakedRegion { RegionId = 1, RoomCategory = MapGenRoomCategory.Start, CellCount = 1 }
                };
                baked.Connectors = new[]
                {
                    new MapGenBakedConnector { Coord = new MapGenGridCoord(1, 0), RegionId = 1, SocketKind = MapGenSocketKind.Door }
                };
                baked.TraversalEdges = new[]
                {
                    new MapGenTraversalEdge { From = new MapGenGridCoord(0, 0), To = new MapGenGridCoord(1, 0) },
                    new MapGenTraversalEdge { From = new MapGenGridCoord(1, 0), To = new MapGenGridCoord(2, 0) }
                };
                baked.SpawnMarkers = new[]
                {
                    new MapGenBakedMarker { MarkerId = "spawn", Coord = new MapGenGridCoord(0, 0), RegionId = 1 }
                };
                baked.ObjectiveMarkers = new[]
                {
                    new MapGenBakedMarker { MarkerId = "goal", Coord = new MapGenGridCoord(2, 0), RegionId = 1 }
                };
                baked.ProfileId = "profile_a";
                baked.RuleSetId = "rules_a";
                baked.StyleId = "style_a";
                baked.Seed = 2001;
                baked.SourceSignature = "signature_a";

                var summary = MapGenBakedMapAssetEditor.BuildRuntimeSummary(baked);

                Assert.That(summary, Does.Contain("Grid 4x3"));
                Assert.That(summary, Does.Contain("Cells 1"));
                Assert.That(summary, Does.Contain("Regions 1"));
                Assert.That(summary, Does.Contain("Connectors 1"));
                Assert.That(summary, Does.Contain("Traversal graph 2 edges"));
                Assert.That(summary, Does.Contain("Nav data Traversal graph + markers"));
                Assert.That(summary, Does.Contain("Spawn markers 1"));
                Assert.That(summary, Does.Contain("Objective markers 1"));
                Assert.That(summary, Does.Contain("Profile profile_a"));
                Assert.That(summary, Does.Contain("Rule rules_a"));
                Assert.That(summary, Does.Contain("Style style_a"));
                Assert.That(summary, Does.Contain("Seed 2001"));
                Assert.That(summary, Does.Contain("Signature signature_a"));
            }
            finally
            {
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void StatusPresentationDefinesStableWorkflowStates()
        {
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Valid).IconName, Is.EqualTo("TestPassed"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Warning).Label, Is.EqualTo("Warning"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Error).IconName, Is.EqualTo("console.erroricon"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Stale).Label, Is.EqualTo("Stale"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Accepted).Label, Is.EqualTo("Confirmed"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Generated).Label, Is.EqualTo("Generated"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.ManualOverride).Label, Is.EqualTo("Manual Override"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Locked).Label, Is.EqualTo("Locked"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.MissingReference).Label, Is.EqualTo("Missing Reference"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.RuntimeSafe).Label, Is.EqualTo("Runtime Safe"));
            Assert.That(MapGenV2StatusPresentation.For(MapGenV2StatusKind.Pending).Label, Is.EqualTo("Pending"));
        }

        [Test]
        public void MaterializedModuleMarkerInspectorReportsSourceMetadata()
        {
            var instance = new GameObject("MaterializedModule");

            try
            {
                var marker = instance.AddComponent<MapGenV2MaterializedModuleMarker>();
                marker.RegionId = 12;
                marker.CellCoord = new Vector2Int(3, 4);
                marker.ModuleCategory = MapGenModuleCategory.FloorA;
                marker.Direction = MapGenGridDirection.East;
                marker.SourceTemplateId = "start_template";
                marker.PrefabName = "FloorPrefab";
                marker.DraftSignature = "draft_sig";
                marker.SourceSignature = "source_sig";
                marker.ModuleSetSignature = "module_sig";

                var summary = MapGenV2MaterializedModuleMarkerEditor.BuildSourceSummary(marker);

                Assert.That(summary, Does.Contain("Region 12"));
                Assert.That(summary, Does.Contain("Cell 3,4"));
                Assert.That(summary, Does.Contain("Category FloorA"));
                Assert.That(summary, Does.Contain("Direction East"));
                Assert.That(summary, Does.Contain("Template start_template"));
                Assert.That(summary, Does.Contain("Prefab FloorPrefab"));
                Assert.That(summary, Does.Contain("Draft draft_sig"));
                Assert.That(summary, Does.Contain("Source source_sig"));
                Assert.That(summary, Does.Contain("ModuleSet module_sig"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void DraftInspectorSummaryReportsSelectionOverridesAndReadiness()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                draft.GridSize = new Vector2Int(2, 2);
                draft.Cells = new[]
                {
                    new MapGenMockupCell
                    {
                        State = MapGenCellState.Room,
                        RegionId = 7,
                        RoomCategory = MapGenRoomCategory.Start,
                        SourceTemplateId = "start_template",
                        SourceShapeId = "shape_a"
                    },
                    new MapGenMockupCell
                    {
                        State = MapGenCellState.Connector,
                        RegionId = 7,
                        RoomCategory = MapGenRoomCategory.Start,
                        SocketKind = MapGenSocketKind.Door
                    },
                    MapGenMockupCell.Empty,
                    MapGenMockupCell.Empty
                };
                draft.RegionOverrides = new[]
                {
                    new MapGenMockupRegionOverride
                    {
                        RegionId = 7,
                        Locked = true,
                        HasCategoryOverride = true,
                        CategoryOverride = MapGenRoomCategory.Boss
                    }
                };
                draft.Accept();

                var summary = MapGenMockupDraftAssetEditor.BuildInspectorSummary(draft, 7);
                var selected = MapGenMockupDraftAssetEditor.BuildSelectedRegionSummary(draft, 7);

                Assert.That(summary, Does.Contain("Grid 2x2"));
                Assert.That(summary, Does.Contain("Cells 4"));
                Assert.That(summary, Does.Contain("Regions 1"));
                Assert.That(summary, Does.Contain("Selected 7"));
                Assert.That(summary, Does.Contain("Overrides 1"));
                Assert.That(summary, Does.Contain("Confirmed Yes"));
                Assert.That(summary, Does.Contain("Stale No"));
                Assert.That(summary, Does.Contain("Materialization Ready"));
                Assert.That(selected, Does.Contain("Selected region 7"));
                Assert.That(selected, Does.Contain("State Connector"));
                Assert.That(selected, Does.Contain("Category Start"));
                Assert.That(selected, Does.Contain("Cells 2"));
                Assert.That(selected, Does.Contain("Connectors 1"));
                Assert.That(selected, Does.Contain("Template start_template"));
                Assert.That(selected, Does.Contain("Shape shape_a"));
                Assert.That(selected, Does.Contain("Locked Yes"));
                Assert.That(selected, Does.Contain("Category override Boss"));
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
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
        public void ValidationReportTracksSeverityAndContextPath()
        {
            var report = new MapGenValidationReport();
            report.Add(new MapGenIssue(
                MapGenGenerationPhase.ValidateProfile,
                "diagnostic_warning",
                "Warning only.",
                "No blocking fix required.",
                severity: MapGenIssueSeverity.Warning,
                contextPath: "Profile.Warning"));

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.ErrorCount, Is.Zero);

            var child = new MapGenValidationReport();
            child.Add(new MapGenIssue(
                MapGenGenerationPhase.ValidateProfile,
                "diagnostic_error",
                "Error.",
                "Fix the child asset.",
                contextPath: "ModuleSet"));
            report.AddRange(child, "Profile.StyleSet");

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "diagnostic_error"
                    && issue.ContextPath == "Profile.StyleSet/ModuleSet"
                    && issue.BlocksGeneration));
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
        public void ModuleSetValidatorReportsPrefabBoundsContractIssues()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var floor = new GameObject("OffsetFloor");
            var wall = new GameObject("ScaledWall");

            try
            {
                floor.transform.localPosition = new Vector3(0.25f, 0f, 0f);
                wall.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
                wall.transform.localScale = new Vector3(1f, 2f, 1f);
                moduleSet.BoundsContract.PivotTolerance = 0.01f;
                moduleSet.FloorsA = new[] { new MapGenModuleEntry { Prefab = floor, Weight = 1, Footprint = Vector2Int.one } };
                moduleSet.WallsStraight = new[] { new MapGenModuleEntry { Prefab = wall, Weight = 1, Footprint = Vector2Int.one } };

                var report = moduleSet.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "module_set_prefab_pivot_offset"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "module_set_prefab_root_rotation"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "module_set_prefab_root_scale"));
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(floor);
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
        public void RoomTemplateValidatorReportsConnectorWidthOutsideSide()
        {
            var template = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();

            try
            {
                template.TemplateId = "wide_door_template";
                template.Footprint = new Vector2Int(2, 2);
                template.FloorCells = new[] { Vector2Int.zero };
                template.Connectors = new[]
                {
                    new MapGenConnector
                    {
                        Side = MapGenGridDirection.North,
                        LocalCell = new Vector2Int(1, 1),
                        SocketKind = MapGenSocketKind.Door,
                        SocketId = "wide",
                        Width = 2
                    }
                };

                var report = template.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "template_connector_width_out_of_bounds"));
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        [Test]
        public void RoomTemplateValidatorReportsNullSourceShapeSlot()
        {
            var template = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();

            try
            {
                PopulateRoomTemplate(template, "shape_ref_template", MapGenRoomCategory.Main);
                template.SourceRoomShapes = new MapGenRoomShapeAsset[] { null };

                var report = template.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "room_template_null_source_shape"));
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
        public void ProfileGraphValidatorReportsRequiredPoolAndImpossibleRanges()
        {
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();

            try
            {
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                profile.MapSize = new Vector2Int(2, 2);
                profile.StyleSet = styleSet;
                profile.LayoutRules = ruleSet;
                styleSet.RoomTemplates = new[] { startTemplate };
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.RequiredCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.MinRooms = 5;
                ruleSet.QuantityRules.MaxRooms = 1;
                ruleSet.QuantityRules.MinCorridorCells = 6;
                ruleSet.DistanceRules = MapGenDistanceRules.Defaults();
                ruleSet.DistanceRules.MinStartToExitDistance = 99;

                var report = MapGenProfileGraphValidator.Validate(profile);

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_graph_required_categories_exceed_max_rooms"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_graph_required_category_has_no_template"
                        && issue.ContextPath == "StyleSet.RoomTemplates/Exit"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_graph_min_rooms_exceeds_map_cells"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_graph_min_corridors_exceeds_map_cells"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_graph_start_exit_distance_impossible"));
            }
            finally
            {
                Object.DestroyImmediate(startTemplate);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ProfileGraphValidatorReportsConnectorMatrixWithoutCompatibleCorridor()
        {
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var corridorTemplate = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();

            try
            {
                PopulateRoomTemplate(roomTemplate, "room_template", MapGenRoomCategory.Start);
                PopulateCorridorTemplate(corridorTemplate, "different_socket");
                profile.MapSize = new Vector2Int(6, 6);
                profile.StyleSet = styleSet;
                profile.LayoutRules = ruleSet;
                styleSet.RoomTemplates = new[] { roomTemplate };
                styleSet.CorridorTemplates = new[] { corridorTemplate };
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.RequiredCategories = new[] { MapGenRoomCategory.Start };

                var report = MapGenProfileGraphValidator.Validate(profile);

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_graph_no_compatible_connector_matrix"
                        && issue.ContextPath == "StyleSet.RoomTemplates/StyleSet.CorridorTemplates"));
            }
            finally
            {
                Object.DestroyImmediate(corridorTemplate);
                Object.DestroyImmediate(roomTemplate);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(profile);
            }
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
                ruleSet.LoopRate = 125;
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.TargetRoomDensityPercent = 101;
                ruleSet.DistanceRules = MapGenDistanceRules.Defaults();
                ruleSet.DistanceRules.MinStartToExitDistance = -1;
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
                    issue => issue.Code == "rule_set_invalid_start_exit_distance"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "rule_set_invalid_loop_rate"));
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
        public void CandidateDomainSummarizesProfileTemplatesAndQuantityRules()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var roomTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var corridorTemplate = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(8, 6);
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.RequiredCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.OptionalCategories = new[] { MapGenRoomCategory.Boss };
                ruleSet.QuantityRules.MinRooms = 2;
                ruleSet.QuantityRules.MaxRooms = 5;
                ruleSet.QuantityRules.MinCorridorCells = 3;
                ruleSet.QuantityRules.MaxCorridorCells = 20;
                ruleSet.QuantityRules.TargetRoomDensityPercent = 30;
                ruleSet.QuantityRules.TargetCorridorDensityPercent = 10;
                PopulateRoomTemplate(roomTemplate, "domain_room", MapGenRoomCategory.Main);
                roomTemplate.BlockedCells = new[] { new Vector2Int(1, 1) };
                PopulateCorridorTemplate(corridorTemplate, "main");
                styleSet.RoomTemplates = new[] { roomTemplate };
                styleSet.CorridorTemplates = new[] { corridorTemplate };

                var domain = MapGenCandidateDomainBuilder.Build(profile);

                Assert.That(domain.Width, Is.EqualTo(8));
                Assert.That(domain.Height, Is.EqualTo(6));
                Assert.That(domain.RoomTemplateCount, Is.EqualTo(1));
                Assert.That(domain.CorridorTemplateCount, Is.EqualTo(1));
                Assert.That(domain.RequiredCategoryCount, Is.EqualTo(2));
                Assert.That(domain.OptionalCategoryCount, Is.EqualTo(1));
                Assert.That(domain.MinRooms, Is.EqualTo(2));
                Assert.That(domain.MaxRooms, Is.EqualTo(5));
                Assert.That(domain.MinCorridorCells, Is.EqualTo(3));
                Assert.That(domain.MaxCorridorCells, Is.EqualTo(20));
                Assert.That(domain.TargetRoomDensityPercent, Is.EqualTo(30));
                Assert.That(domain.TargetCorridorDensityPercent, Is.EqualTo(10));
                Assert.That(domain.BlockedTemplateCellCount, Is.EqualTo(1));
                Assert.That(domain.RoomFootprintCandidateCells, Is.GreaterThan(0));
                Assert.That(domain.CorridorCandidateCells, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(corridorTemplate);
                Object.DestroyImmediate(roomTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void RequiredLandmarkReservationUsesStructuredRequiredCategories()
        {
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();

            try
            {
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.RequiredCategories = new[]
                {
                    MapGenRoomCategory.Start,
                    MapGenRoomCategory.Quest,
                    MapGenRoomCategory.Boss,
                    MapGenRoomCategory.Exit
                };
                profile.LayoutRules = ruleSet;

                var landmarks = MapGenRequiredLandmarkReservation.Build(profile);

                Assert.That(landmarks, Has.Length.EqualTo(4));
                Assert.That(landmarks[0].Category, Is.EqualTo(MapGenRoomCategory.Start));
                Assert.That(landmarks[1].Category, Is.EqualTo(MapGenRoomCategory.Quest));
                Assert.That(landmarks[2].Category, Is.EqualTo(MapGenRoomCategory.Boss));
                Assert.That(landmarks[3].Category, Is.EqualTo(MapGenRoomCategory.Exit));
                Assert.That(landmarks[2].LandmarkId, Is.EqualTo("2_Boss"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(ruleSet);
            }
        }

        [Test]
        public void CandidateDomainReportsLowestEntropyRequiredLandmark()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var mainTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var bossTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(6, 6);
                ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
                ruleSet.QuantityRules.RequiredCategories = new[]
                {
                    MapGenRoomCategory.Start,
                    MapGenRoomCategory.Boss,
                    MapGenRoomCategory.Exit
                };
                PopulateRoomTemplate(mainTemplate, "main_template", MapGenRoomCategory.Main);
                PopulateRoomTemplate(bossTemplate, "boss_template", MapGenRoomCategory.Boss);
                bossTemplate.Footprint = new Vector2Int(4, 4);
                styleSet.RoomTemplates = new[] { mainTemplate, bossTemplate };

                var domain = MapGenCandidateDomainBuilder.Build(profile);

                Assert.That(domain.LandmarkEntropies, Has.Length.EqualTo(3));
                Assert.That(domain.LowestEntropyLandmarkIndex, Is.EqualTo(1));
                Assert.That(domain.LowestEntropyLandmarkCategory, Is.EqualTo(MapGenRoomCategory.Boss));
                Assert.That(domain.LowestEntropyLandmarkCandidateCount, Is.LessThan(domain.LandmarkEntropies[0].CandidateCount));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(bossTemplate);
                Object.DestroyImmediate(mainTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
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
                    issue => issue.Code == "profile_missing_style_set"
                        && issue.ContextPath == nameof(MapGenProfileAsset.StyleSet)
                        && issue.Severity == MapGenIssueSeverity.Error));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_missing_rule_set"
                        && issue.ContextPath == nameof(MapGenProfileAsset.LayoutRules)));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "profile_missing_room_shapes"
                        && issue.ContextPath == nameof(MapGenProfileAsset.RoomShapes)));
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
        public void MockupDraftSignatureIncludesProfileRuleAndTemplateSources()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var template = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                PopulateRoomTemplate(template, "source_template", MapGenRoomCategory.Start);
                styleSet.RoomTemplates = new[] { template };
                draft.Profile = profile;
                draft.GridSize = new Vector2Int(2, 2);
                draft.Seed = 17;
                draft.EnsureCellArray();
                draft.Cells[0].State = MapGenCellState.Room;

                var baseSignature = draft.ComputeSignature();
                template.Weight = 2;
                var templateSignature = draft.ComputeSignature();
                template.Weight = 1;
                ruleSet.LoopRate = 50;
                var ruleSignature = draft.ComputeSignature();
                ruleSet.LoopRate = 0;
                profile.ProfileId = "workflow_profile_changed";
                var profileSignature = draft.ComputeSignature();

                Assert.That(templateSignature, Is.Not.EqualTo(baseSignature));
                Assert.That(ruleSignature, Is.Not.EqualTo(baseSignature));
                Assert.That(profileSignature, Is.Not.EqualTo(baseSignature));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(profile);
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
                draft.LastEnclosedEmptyCellsFilled = 1;
                draft.Accept();

                draft.ClearDraft();

                Assert.That(draft.LastGeneratedSignature, Is.Empty);
                Assert.That(draft.Accepted, Is.False);
                Assert.That(draft.AcceptedSignature, Is.Empty);
                Assert.That(draft.LastDirectRouteCellsAdded, Is.Zero);
                Assert.That(draft.LastDeadEndCorridorsRemoved, Is.Zero);
                Assert.That(draft.LastIsolatedRoomsRemoved, Is.Zero);
                Assert.That(draft.LastEnclosedEmptyCellsFilled, Is.Zero);
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
        public void MockupDraftRegenerateRegionPreservesOtherRegionsAndOverrides()
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
                draft.SetRegionLocked(1, true);
                draft.SetRegionCategory(1, MapGenRoomCategory.Boss);
                draft.Accept();
                var preservedRegion = CopyRegionCells(draft, 1);
                draft.Seed = 456;

                Assert.That(draft.RegenerateRegionFromProfile(0).IsValid, Is.True);

                Assert.That(CopyRegionCells(draft, 1), Is.EqualTo(preservedRegion));
                Assert.That(draft.TryGetRegionOverride(1, out var preservedOverride), Is.True);
                Assert.That(preservedOverride.Locked, Is.True);
                Assert.That(preservedOverride.CategoryOverride, Is.EqualTo(MapGenRoomCategory.Boss));
                Assert.That(draft.TryGetRegionOverride(0, out _), Is.False);
                Assert.That(draft.Accepted, Is.False);
                Assert.That(draft.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => cell.RegionId == 0 && cell.SourceTemplateId == "start_template"));
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

                Assert.That(draft.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => cell.State == MapGenCellState.Room && cell.SourceShapeId == "workflow_shape"));
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
            Assert.That(emptyStatus.GenerateReason, Is.EqualTo("Create or assign the current mockup asset first."));
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
                Assert.That(generated.MaterializeReason, Is.EqualTo("Confirm Mockup first."));
                Assert.That(draft.IsGeneratedSignatureCurrent, Is.True);

                profile.ProfileId = "workflow_profile_changed";
                var generatedStale = MapGenV2WorkflowStatus.From(profile, draft);
                Assert.That(generatedStale.GeneratedCurrent, Is.False);
                Assert.That(generatedStale.CanAccept, Is.False);
                Assert.That(generatedStale.AcceptReason, Is.EqualTo("The generated mockup is stale because source assets changed. Regenerate Mockup."));
                profile.ProfileId = "workflow_profile";
                Assert.That(draft.IsGeneratedSignatureCurrent, Is.True);

                draft.Accept();
                var accepted = MapGenV2WorkflowStatus.From(profile, draft);
                Assert.That(accepted.CanMaterialize, Is.True);
                Assert.That(accepted.CanBakeRuntime, Is.True);

                draft.Cells[0].State = MapGenCellState.Blocked;
                var stale = MapGenV2WorkflowStatus.From(profile, draft);
                Assert.That(stale.CanMaterialize, Is.False);
                Assert.That(stale.MaterializeReason, Is.EqualTo("The confirmed mockup is stale. Confirm the current mockup."));
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
        public void PerformanceProfileSelectsTargetBudgetsByMapSize()
        {
            Assert.That(MapGenV2PerformanceProfile.SelectBudget(8, 8).Target, Is.EqualTo(MapGenV2PerformanceTarget.Small));
            Assert.That(MapGenV2PerformanceProfile.SelectBudget(32, 32).Target, Is.EqualTo(MapGenV2PerformanceTarget.Medium));
            Assert.That(MapGenV2PerformanceProfile.SelectBudget(64, 64).Target, Is.EqualTo(MapGenV2PerformanceTarget.Large));
            Assert.That(MapGenV2PerformanceProfile.SelectBudget(128, 128).Target, Is.EqualTo(MapGenV2PerformanceTarget.Stress));
            Assert.That(MapGenV2PerformanceProfile.Large.GenerationBudgetMs, Is.GreaterThan(MapGenV2PerformanceProfile.Medium.GenerationBudgetMs));
            Assert.That(MapGenV2PerformanceProfile.Stress.PreviewMemoryBudgetKb, Is.GreaterThan(MapGenV2PerformanceProfile.Large.PreviewMemoryBudgetKb));
        }

        [Test]
        public void PerformanceProfilerWritesOperationSamples()
        {
            if (System.IO.File.Exists(MapGenV2PerformanceProfiler.LogPath))
            {
                System.IO.File.Delete(MapGenV2PerformanceProfiler.LogPath);
            }

            var result = MapGenV2PerformanceProfiler.Measure(
                "UnitTestOperation",
                12,
                12,
                MapGenV2PerformanceProfile.Small.GenerationBudgetMs,
                () => 17,
                "Detail=unit",
                out var sample);

            Assert.That(result, Is.EqualTo(17));
            Assert.That(sample.Operation, Is.EqualTo("UnitTestOperation"));
            Assert.That(sample.Target, Is.EqualTo(MapGenV2PerformanceTarget.Small));
            Assert.That(sample.BudgetMs, Is.EqualTo(MapGenV2PerformanceProfile.Small.GenerationBudgetMs));
            Assert.That(System.IO.File.Exists(MapGenV2PerformanceProfiler.LogPath), Is.True);
            Assert.That(System.IO.File.ReadAllText(MapGenV2PerformanceProfiler.LogPath), Does.Contain("UnitTestOperation"));
        }

        [Test]
        public void PerformanceDetailsSummarizeContradictionsAndPassCosts()
        {
            var validation = new MapGenValidationReport();
            validation.Add(new MapGenIssue(
                MapGenGenerationPhase.SolveMockup,
                "production_solver_missing_compatible_corridor_template",
                "No corridor template can connect the selected room connectors.",
                "Add a compatible corridor template."));
            validation.Add(new MapGenIssue(
                MapGenGenerationPhase.SolveMockup,
                "production_solver_retry_exhausted",
                "Retry attempts exhausted.",
                "Relax constraints."));
            var validationDetail = MapGenV2PerformanceDetails.ForValidationReport(validation, 3, "Seed=7");

            Assert.That(validationDetail, Does.Contain("Attempts=3"));
            Assert.That(validationDetail, Does.Contain("RetryIssues=1"));
            Assert.That(validationDetail, Does.Contain("Contradictions=1"));
            Assert.That(validationDetail, Does.Contain("production_solver_retry_exhausted"));

            var postProcessDetail = MapGenV2PerformanceDetails.ForPostProcess(
                new MapGenPostProcessReport
                {
                    PassesRun = 2,
                    Rollbacks = 1,
                    DirectRouteCellsAdded = 5,
                    DeadEndCorridorsRemoved = 3
                },
                "Seed=7");

            Assert.That(postProcessDetail, Does.Contain("PassesRun=2"));
            Assert.That(postProcessDetail, Does.Contain("Rollbacks=1"));
            Assert.That(postProcessDetail, Does.Contain("DirectRouteCellsAdded=5"));
        }

        [Test]
        public void GenerationAndPostProcessSupportCooperativeCancellation()
        {
            var generation = MapGenMockupSolver.Generate(
                8,
                6,
                42,
                new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit },
                () => true);

            Assert.That(generation.Success, Is.False);
            Assert.That(generation.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "solver_cancelled"));

            var cells = new MapGenMockupCell[4];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            var postProcess = MapGenMockupPostProcessor.Apply(
                2,
                2,
                cells,
                new MapGenPostProcessOptions { MaxPasses = 3, UseDirectRoutes = true },
                () => true);

            Assert.That(postProcess.Cancelled, Is.True);
            Assert.That(postProcess.PassesRun, Is.EqualTo(0));
        }

        [Test]
        public void AuthoringPreviewTextureCacheReusesAndInvalidatesByKey()
        {
            var cache = new MapGenV2AuthoringPreviewTextureCache();

            try
            {
                var first = cache.GetOrCreate(
                    "shape:a",
                    2,
                    2,
                    (x, y) => x == y ? Color.red : Color.blue,
                    "test_authoring_preview");
                Assert.That(first, Is.Not.Null);
                Assert.That(cache.LastRequestWasCacheHit, Is.False);

                var second = cache.GetOrCreate(
                    "shape:a",
                    2,
                    2,
                    (x, y) => Color.green,
                    "test_authoring_preview");
                Assert.That(second, Is.SameAs(first));
                Assert.That(cache.LastRequestWasCacheHit, Is.True);

                var third = cache.GetOrCreate(
                    "shape:b",
                    2,
                    2,
                    (x, y) => Color.green,
                    "test_authoring_preview");
                Assert.That(third, Is.Not.Null);
                Assert.That(cache.LastRequestWasCacheHit, Is.False);
            }
            finally
            {
                cache.Dispose();
            }
        }

        [Test]
        public void PreviewTextureCacheReusesAndInvalidatesByDraftSignature()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var cache = new MapGenV2PreviewTextureCache();

            try
            {
                draft.Seed = 1;
                draft.GridSize = new Vector2Int(2, 1);
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 };
                draft.Cells[1] = new MapGenMockupCell { State = MapGenCellState.Corridor, RegionId = 2 };
                var palette = new MapGenV2PreviewPalette(
                    Color.blue,
                    Color.red,
                    Color.black,
                    Color.gray,
                    Color.yellow,
                    Color.white);

                var first = cache.GetOrCreate(MapGenMockupPreviewData.FromDraft(draft), palette);
                Assert.That(first, Is.Not.Null);
                Assert.That(cache.LastRequestWasCacheHit, Is.False);

                var second = cache.GetOrCreate(MapGenMockupPreviewData.FromDraft(draft), palette);
                Assert.That(second, Is.SameAs(first));
                Assert.That(cache.LastRequestWasCacheHit, Is.True);

                draft.Cells[1] = new MapGenMockupCell { State = MapGenCellState.Blocked, RegionId = 2 };
                var third = cache.GetOrCreate(MapGenMockupPreviewData.FromDraft(draft), palette);
                Assert.That(third, Is.Not.Null);
                Assert.That(cache.LastRequestWasCacheHit, Is.False);
            }
            finally
            {
                cache.Dispose();
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void AssetDatabaseRefreshIsCentralizedForMapGenV2EditorCode()
        {
            var directRefreshCallers = new System.Collections.Generic.List<string>();
            foreach (var path in System.IO.Directory.GetFiles("Assets/Conn/Editor/MapGenV2", "*.cs", System.IO.SearchOption.TopDirectoryOnly))
            {
                if (path.EndsWith("MapGenV2AssetDatabasePolicy.cs", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (System.IO.File.ReadAllText(path).Contains("AssetDatabase.Refresh("))
                {
                    directRefreshCallers.Add(path);
                }
            }

            Assert.That(directRefreshCallers, Is.Empty);
        }

        [Test]
        public void AuthoringAssetMigrationAddsVersionAndNormalizesDefaults()
        {
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var roomTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var corridorTemplate = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                profile.Version = 0;
                profile.OutputSettings = default;
                profile.RoomShapes = null;
                styleSet.Version = 0;
                styleSet.RoomShapePool = null;
                styleSet.RoomTemplates = null;
                styleSet.CorridorTemplates = null;
                ruleSet.Version = 0;
                ruleSet.RequiredRoomCategories = null;
                ruleSet.OptionalRoomCategories = null;
                ruleSet.PropPlacementRules = null;
                roomShape.Version = 0;
                roomShape.Dimensions = Vector2Int.zero;
                roomShape.Cells = null;
                roomShape.Weight = 0;
                roomTemplate.Version = 0;
                roomTemplate.Footprint = Vector2Int.zero;
                roomTemplate.SourceRoomShapes = null;
                roomTemplate.Connectors = null;
                roomTemplate.FloorCells = null;
                roomTemplate.WallCells = null;
                roomTemplate.BlockedCells = null;
                roomTemplate.DoorHintCells = null;
                roomTemplate.PropChannels = null;
                roomTemplate.Weight = 0;
                corridorTemplate.Version = 0;
                corridorTemplate.Width = 0;
                corridorTemplate.LengthRange = Vector2Int.zero;
                corridorTemplate.Connectors = null;
                corridorTemplate.PropChannels = null;
                corridorTemplate.Weight = 0;
                moduleSet.Version = 0;
                moduleSet.BoundsContract = null;
                moduleSet.FloorsA = null;
                moduleSet.WallsStraight = null;
                moduleSet.RequiredUniqueProps = null;
                draft.Version = 0;
                draft.GridSize = Vector2Int.zero;
                draft.Cells = null;
                draft.RegionOverrides = null;

                var profileReport = MapGenV2AuthoringAssetMigration.MigrateInMemory(profile);
                MapGenV2AuthoringAssetMigration.MigrateInMemory(styleSet);
                MapGenV2AuthoringAssetMigration.MigrateInMemory(ruleSet);
                MapGenV2AuthoringAssetMigration.MigrateInMemory(roomShape);
                MapGenV2AuthoringAssetMigration.MigrateInMemory(roomTemplate);
                MapGenV2AuthoringAssetMigration.MigrateInMemory(corridorTemplate);
                MapGenV2AuthoringAssetMigration.MigrateInMemory(moduleSet);
                MapGenV2AuthoringAssetMigration.MigrateInMemory(draft);

                Assert.That(profileReport.IsValid, Is.True);
                Assert.That(profileReport.WasMigrated, Is.True);
                Assert.That(profile.Version, Is.EqualTo(MapGenProfileAsset.CurrentVersion));
                Assert.That(profile.OutputSettings.DraftFolder, Is.Not.Empty);
                Assert.That(profile.RoomShapes, Is.Not.Null);
                Assert.That(styleSet.RoomTemplates, Is.Not.Null);
                Assert.That(ruleSet.RequiredRoomCategories, Is.Not.Null);
                Assert.That(roomShape.Width, Is.EqualTo(1));
                Assert.That(roomShape.Weight, Is.EqualTo(1));
                Assert.That(roomTemplate.Footprint, Is.EqualTo(Vector2Int.one));
                Assert.That(corridorTemplate.Width, Is.EqualTo(1));
                Assert.That(moduleSet.BoundsContract, Is.Not.Null);
                Assert.That(moduleSet.FloorsA, Is.Not.Null);
                Assert.That(draft.GridSize, Is.EqualTo(Vector2Int.one));
                Assert.That(draft.Cells, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(moduleSet);
                Object.DestroyImmediate(corridorTemplate);
                Object.DestroyImmediate(roomTemplate);
                Object.DestroyImmediate(roomShape);
                Object.DestroyImmediate(ruleSet);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void AuthoringAssetMigrationRejectsFutureVersion()
        {
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();

            try
            {
                profile.Version = MapGenProfileAsset.CurrentVersion + 1;

                var report = MapGenV2AuthoringAssetMigration.MigrateInMemory(profile);

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.WasMigrated, Is.False);
                Assert.That(report.Message, Does.Contain("newer than supported"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CleanupGeneratedAssetsDeletesOnlyStarterTaggedAssets()
        {
            const string tempFolder = "Assets/Conn/Tests/TempMapGenV2Cleanup";
            var starterProfilePath = $"{tempFolder}/StarterProfile.asset";
            var keepProfilePath = $"{tempFolder}/ProductionProfile.asset";
            var starterDraftPath = $"{tempFolder}/StarterDraft.asset";
            var starterStylePath = $"{tempFolder}/StarterStyleSet.asset";
            var starterModuleSetPath = $"{tempFolder}/StarterModuleSet.asset";
            var starterRoomShapePath = $"{tempFolder}/StarterRoomShape.asset";
            var starterRoomTemplatePath = $"{tempFolder}/StarterRoomTemplate.asset";
            var starterCorridorTemplatePath = $"{tempFolder}/StarterCorridorTemplate.asset";
            var starterPrefabPath = $"{tempFolder}/StarterFloor.prefab";
            var keepPrefabPath = $"{tempFolder}/ProductionFloor.prefab";

            try
            {
                EnsureTempFolder(tempFolder);
                var starterProfile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
                starterProfile.ProfileId = "starter_profile";
                AssetDatabase.CreateAsset(starterProfile, starterProfilePath);
                var keepProfile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
                keepProfile.ProfileId = "production_profile";
                AssetDatabase.CreateAsset(keepProfile, keepProfilePath);
                var starterDraft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
                starterDraft.Profile = starterProfile;
                AssetDatabase.CreateAsset(starterDraft, starterDraftPath);
                var starterStyle = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
                starterStyle.StyleId = "starter_style";
                AssetDatabase.CreateAsset(starterStyle, starterStylePath);
                var starterModuleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
                starterModuleSet.ModuleSetId = "starter_module_set";
                AssetDatabase.CreateAsset(starterModuleSet, starterModuleSetPath);
                var starterShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
                starterShape.ShapeId = "starter_room_shape";
                AssetDatabase.CreateAsset(starterShape, starterRoomShapePath);
                var starterRoomTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
                starterRoomTemplate.TemplateId = "starter_room_template";
                AssetDatabase.CreateAsset(starterRoomTemplate, starterRoomTemplatePath);
                var starterCorridorTemplate = ScriptableObject.CreateInstance<MapGenCorridorTemplateAsset>();
                starterCorridorTemplate.TemplateId = "starter_corridor_template";
                AssetDatabase.CreateAsset(starterCorridorTemplate, starterCorridorTemplatePath);
                CreateTempPrefab(starterPrefabPath, "StarterFloor");
                CreateTempPrefab(keepPrefabPath, "ProductionFloor");
                AssetDatabase.SaveAssets();

                var deleted = MapGenV2StarterSetupBuilder.CleanupGeneratedAssets(tempFolder);

                Assert.That(deleted, Is.EqualTo(8));
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenProfileAsset>(starterProfilePath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenMockupDraftAsset>(starterDraftPath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenStyleSetAsset>(starterStylePath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenModuleSetAsset>(starterModuleSetPath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenRoomShapeAsset>(starterRoomShapePath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenRoomTemplateAsset>(starterRoomTemplatePath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenCorridorTemplateAsset>(starterCorridorTemplatePath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(starterPrefabPath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<MapGenProfileAsset>(keepProfilePath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(keepPrefabPath), Is.Not.Null);
            }
            finally
            {
                AssetDatabase.DeleteAsset(tempFolder);
            }
        }

        [Test]
        public void StarterSetupCreatesValidLinkedAssetsAndWorkflow()
        {
            const string tempRoot = "Assets/Conn/Tests/TempMapGenV2StarterSetup";
            GameObject root = null;

            try
            {
                var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup(tempRoot);

                Assert.That(setup.Profile, Is.Not.Null);
                Assert.That(setup.Draft, Is.Not.Null);
                Assert.That(setup.Draft.Profile, Is.SameAs(setup.Profile));
                Assert.That(setup.Profile.StyleSet, Is.Not.Null);
                Assert.That(setup.Profile.StyleSet.ModuleSet, Is.Not.Null);
                Assert.That(setup.Profile.LayoutRules, Is.Not.Null);
                Assert.That(setup.Profile.RoomShapes, Is.Not.Empty);
                Assert.That(setup.Profile.Validate().IsValid, Is.True);

                var generationReport = setup.Draft.GenerateFromProfile();
                Assert.That(generationReport.IsValid, Is.True, BuildIssueSummary(generationReport));
                setup.Draft.ApplyPostProcessingFromProfile();
                setup.Draft.Accept();

                root = MapGenMockupMaterializer.Materialize(setup.Draft);
                var baked = MapGenRuntimeBakeUtility.Bake(setup.Draft);

                Assert.That(root, Is.Not.Null);
                Assert.That(root.GetComponent<MapGenV2GeneratedMapMarker>(), Is.Not.Null);
                Assert.That(baked, Is.Not.Null);
                Assert.That(baked.Cells, Is.Not.Empty);
                Assert.That(baked.TraversalEdges, Is.Not.Empty);
                Assert.That(AssetDatabase.GetAssetPath(baked), Does.StartWith(tempRoot));
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                AssetDatabase.DeleteAsset(tempRoot);
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
                draft.AcceptedSourceSignature = "source_signature_a";

                var marker = root.AddComponent<MapGenV2GeneratedMapMarker>();
                marker.PopulateFromDraft(draft, "2026-05-31T00:00:00.0000000Z");

                Assert.That(marker.ProfileId, Is.EqualTo("marker_profile"));
                Assert.That(marker.Seed, Is.EqualTo(1234));
                Assert.That(marker.DraftSignature, Is.EqualTo("signature_a"));
                Assert.That(marker.SourceSignature, Is.EqualTo("source_signature_a"));
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
                PopulateCompleteMaterializationCoverage(moduleSet, floorPrefab, wallPrefab);
                styleSet.StyleId = "scene_controls_style";
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "scene_controls_profile";
                profile.StyleSet = styleSet;
                draft.Profile = profile;
                draft.Seed = 55;
                draft.GridSize = new Vector2Int(2, 1);
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 3,
                    RoomCategory = MapGenRoomCategory.Start,
                    SourceTemplateId = "start_template"
                };
                draft.Cells[1] = new MapGenMockupCell
                {
                    State = MapGenCellState.Blocked,
                    RegionId = 4,
                    SourceTemplateId = "blocked_template"
                };
                draft.Accept();

                firstRoot = MapGenMockupMaterializer.Materialize(draft, MapGenV2SceneOutputMode.ReplacePreviousRoot);
                var moduleSetSignature = MapGenMockupMaterializer.BuildModuleSetSignature(moduleSet);
                Assert.That(firstRoot, Is.Not.Null);
                Assert.That(firstRoot.name, Is.EqualTo("MapGenV2_scene_controls_profile_55"));
                Assert.That(firstRoot.GetComponent<MapGenV2GeneratedMapMarker>(), Is.Not.Null);
                Assert.That(firstRoot.GetComponent<MapGenV2GeneratedMapMarker>().MaterializationRequestCount, Is.GreaterThan(0));
                Assert.That(firstRoot.GetComponent<MapGenV2GeneratedMapMarker>().MaterializationInstantiatedCount, Is.GreaterThan(0));
                Assert.That(firstRoot.GetComponent<MapGenV2GeneratedMapMarker>().ModuleSetSignature, Is.EqualTo(moduleSetSignature));
                Assert.That(firstRoot.transform.Find("Floors"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Corridors"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Walls"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Ceilings"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Doors"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Blockers"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Props"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Navigation"), Is.Not.Null);
                Assert.That(firstRoot.transform.Find("Debug"), Is.Not.Null);
                Assert.That(MapGenMockupMaterializer.FindExistingMarker(draft).gameObject, Is.SameAs(firstRoot));
                var moduleMarkers = firstRoot.GetComponentsInChildren<MapGenV2MaterializedModuleMarker>();
                var floorMarker = System.Array.Find(moduleMarkers, marker => marker.ModuleCategory == MapGenModuleCategory.FloorA);
                var blockerMarker = System.Array.Find(moduleMarkers, marker => marker.ModuleCategory == MapGenModuleCategory.Blocker);
                var navigationMarker = System.Array.Find(moduleMarkers, marker => marker.ModuleCategory == MapGenModuleCategory.NavigationHelper);
                Assert.That(floorMarker, Is.Not.Null);
                Assert.That(floorMarker.name, Does.Contain("FloorA_0_0_R3_Tstart_template"));
                Assert.That(floorMarker.name, Does.Contain("Floor"));
                Assert.That(floorMarker.RegionId, Is.EqualTo(3));
                Assert.That(floorMarker.SourceTemplateId, Is.EqualTo("start_template"));
                Assert.That(floorMarker.DraftSignature, Is.EqualTo(draft.AcceptedSignature));
                Assert.That(floorMarker.ModuleSetSignature, Is.EqualTo(moduleSetSignature));
                Assert.That(blockerMarker, Is.Not.Null);
                Assert.That(blockerMarker.name, Does.Contain("Blocker_1_0_R4_Tblocked_template"));
                Assert.That(blockerMarker.transform.parent.name, Is.EqualTo("Blockers"));
                Assert.That(blockerMarker.RegionId, Is.EqualTo(4));
                Assert.That(blockerMarker.SourceTemplateId, Is.EqualTo("blocked_template"));
                Assert.That(blockerMarker.DraftSignature, Is.EqualTo(draft.AcceptedSignature));
                Assert.That(blockerMarker.ModuleSetSignature, Is.EqualTo(moduleSetSignature));
                Assert.That(navigationMarker, Is.Not.Null);
                Assert.That(navigationMarker.name, Does.Contain("NavigationHelper_0_0_R3_Tstart_template_NavigationHelper"));
                Assert.That(navigationMarker.transform.parent.name, Is.EqualTo("Navigation"));
                Assert.That(navigationMarker.RegionId, Is.EqualTo(3));
                Assert.That(navigationMarker.SourceTemplateId, Is.EqualTo("start_template"));
                Assert.That(navigationMarker.DraftSignature, Is.EqualTo(draft.AcceptedSignature));
                Assert.That(navigationMarker.ModuleSetSignature, Is.EqualTo(moduleSetSignature));

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
        public void MaterializerReportsStaleMaterializedOutput()
        {
            const string tempFolder = "Assets/Conn/Tests/TempMapGenV2MaterializerStale";
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject root = null;

            try
            {
                EnsureTempFolder(tempFolder);
                var floorPrefab = CreateTempPrefab($"{tempFolder}/Floor.prefab", "Floor");
                var wallPrefab = CreateTempPrefab($"{tempFolder}/Wall.prefab", "Wall");
                moduleSet.FloorsA = new[] { new MapGenModuleEntry { Prefab = floorPrefab, Weight = 1, Footprint = Vector2Int.one } };
                moduleSet.WallsStraight = new[] { new MapGenModuleEntry { Prefab = wallPrefab, Weight = 1, Footprint = Vector2Int.one } };
                PopulateCompleteMaterializationCoverage(moduleSet, floorPrefab, wallPrefab);
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "stale_materialized_profile";
                profile.StyleSet = styleSet;
                draft.Profile = profile;
                draft.Seed = 91;
                draft.GridSize = Vector2Int.one;
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 6,
                    RoomCategory = MapGenRoomCategory.Start
                };
                draft.Accept();

                root = MapGenMockupMaterializer.Materialize(draft);
                Assert.That(MapGenMockupMaterializer.ValidateExistingOutput(draft, root).IsValid, Is.True);

                var marker = root.GetComponent<MapGenV2GeneratedMapMarker>();
                marker.DraftSignature = "old_draft_signature";
                var staleDraftReport = MapGenMockupMaterializer.ValidateExistingOutput(draft, root);
                Assert.That(staleDraftReport.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "materialized_output_stale_draft_signature"));

                marker.DraftSignature = draft.AcceptedSignature;
                moduleSet.FloorsA[0].Weight = 7;
                var staleModuleReport = MapGenMockupMaterializer.ValidateExistingOutput(draft, root);
                Assert.That(staleModuleReport.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "materialized_output_stale_module_set"));
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                AssetDatabase.DeleteAsset(tempFolder);
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
            }
        }

        [Test]
        public void MaterializerUpdateSelectedPolicyRequiresSelectedSceneRoot()
        {
            const string tempFolder = "Assets/Conn/Tests/TempMapGenV2MaterializerOverwrite";
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject selectedRoot = null;

            try
            {
                EnsureTempFolder(tempFolder);
                var floorPrefab = CreateTempPrefab($"{tempFolder}/Floor.prefab", "Floor");
                var wallPrefab = CreateTempPrefab($"{tempFolder}/Wall.prefab", "Wall");
                moduleSet.FloorsA = new[] { new MapGenModuleEntry { Prefab = floorPrefab, Weight = 1, Footprint = Vector2Int.one } };
                moduleSet.WallsStraight = new[] { new MapGenModuleEntry { Prefab = wallPrefab, Weight = 1, Footprint = Vector2Int.one } };
                PopulateCompleteMaterializationCoverage(moduleSet, floorPrefab, wallPrefab);
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "overwrite_policy_profile";
                profile.StyleSet = styleSet;
                draft.Profile = profile;
                draft.Seed = 111;
                draft.GridSize = Vector2Int.one;
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 9,
                    RoomCategory = MapGenRoomCategory.Start
                };
                draft.Accept();

                Assert.That(MapGenMockupMaterializer.Materialize(draft, MapGenV2SceneOutputMode.UpdateSelectedRoot), Is.Null);

                selectedRoot = new GameObject("SelectedRoot");
                var materialized = MapGenMockupMaterializer.Materialize(
                    draft,
                    MapGenV2SceneOutputMode.UpdateSelectedRoot,
                    selectedRoot);

                Assert.That(materialized, Is.SameAs(selectedRoot));
                Assert.That(selectedRoot.name, Is.EqualTo("MapGenV2_overwrite_policy_profile_111"));
                Assert.That(selectedRoot.GetComponent<MapGenV2GeneratedMapMarker>(), Is.Not.Null);
                Assert.That(selectedRoot.transform.Find("Floors"), Is.Not.Null);
            }
            finally
            {
                if (selectedRoot != null)
                {
                    Object.DestroyImmediate(selectedRoot);
                }

                AssetDatabase.DeleteAsset(tempFolder);
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
            }
        }

        [Test]
        public void MaterializerAppliesCellSizeOffsetAndRotationPolicy()
        {
            const string tempFolder = "Assets/Conn/Tests/TempMapGenV2Materializer";
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject root = null;

            try
            {
                EnsureTempFolder(tempFolder);
                var floorPrefab = CreateTempPrefab($"{tempFolder}/Floor.prefab", "Floor");
                var wallPrefab = CreateTempPrefab($"{tempFolder}/Wall.prefab", "Wall");
                moduleSet.FloorsA = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = floorPrefab,
                        Weight = 1,
                        Footprint = Vector2Int.one,
                        Offset = new Vector3(0.25f, 0.5f, -0.25f),
                        RotationPolicy = MapGenModuleRotationPolicy.None
                    }
                };
                moduleSet.WallsStraight = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = wallPrefab,
                        Weight = 1,
                        Footprint = Vector2Int.one,
                        RotationPolicy = MapGenModuleRotationPolicy.AnyOrthogonal
                    }
                };
                PopulateCompleteMaterializationCoverage(moduleSet, floorPrefab, wallPrefab);
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "rotation_profile";
                profile.CellSize = 2f;
                profile.StyleSet = styleSet;
                draft.Profile = profile;
                draft.Seed = 77;
                draft.GridSize = Vector2Int.one;
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 4,
                    RoomCategory = MapGenRoomCategory.Start
                };
                draft.Accept();

                root = MapGenMockupMaterializer.Materialize(draft);

                var markers = root.GetComponentsInChildren<MapGenV2MaterializedModuleMarker>();
                var floorMarker = System.Array.Find(markers, marker => marker.ModuleCategory == MapGenModuleCategory.FloorA);
                var eastWallMarker = System.Array.Find(markers, marker =>
                    marker.ModuleCategory == MapGenModuleCategory.WallStraight
                    && marker.Direction == MapGenGridDirection.East);
                Assert.That(floorMarker, Is.Not.Null);
                Assert.That(floorMarker.transform.position.x, Is.EqualTo(1.25f).Within(0.001f));
                Assert.That(floorMarker.transform.position.y, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(floorMarker.transform.position.z, Is.EqualTo(0.75f).Within(0.001f));
                Assert.That(eastWallMarker, Is.Not.Null);
                Assert.That(Mathf.DeltaAngle(eastWallMarker.transform.eulerAngles.y, 90f), Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
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
        public void MockupSolverVariesAcrossDifferentSeeds()
        {
            var required = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Quest, MapGenRoomCategory.Exit };

            var first = MapGenMockupSolver.Generate(12, 10, 42, required);
            var second = MapGenMockupSolver.Generate(12, 10, 43, required);

            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Signature, Is.Not.EqualTo(first.Signature));
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
                Assert.That(first.AttemptCount, Is.EqualTo(1));
                Assert.That(second.Signature, Is.EqualTo(first.Signature));
                Assert.That(first.Cells, Has.Exactly(4).Matches<MapGenMockupCell>(
                    cell => IsRoomFootprintCell(cell)
                        && cell.RoomCategory == MapGenRoomCategory.Start
                        && cell.SourceTemplateId == "start_template"));
                Assert.That(first.Cells, Has.Exactly(4).Matches<MapGenMockupCell>(
                    cell => IsRoomFootprintCell(cell)
                        && cell.RoomCategory == MapGenRoomCategory.Exit
                        && cell.SourceTemplateId == "exit_template"));
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
        public void TemplateMockupSolverReportsStartExitDistanceContradiction()
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
                ruleSet.DistanceRules = MapGenDistanceRules.Defaults();
                ruleSet.DistanceRules.MinStartToExitDistance = 999;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001, maxAttempts: 3);

                Assert.That(result.Success, Is.False);
                Assert.That(result.AttemptCount, Is.EqualTo(3));
                Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_start_exit_distance_too_short"));
                Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_retry_exhausted"));
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
        public void TemplateMockupSolverReportsBossAndQuestDistanceContradictions()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var bossTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var questTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(12, 6);
                ruleSet.RequiredRoomCategories = new[]
                {
                    MapGenRoomCategory.Start,
                    MapGenRoomCategory.Boss,
                    MapGenRoomCategory.Quest,
                    MapGenRoomCategory.Exit
                };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                ruleSet.DistanceRules = MapGenDistanceRules.Defaults();
                ruleSet.DistanceRules.RequireQuestBeforeBoss = true;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(bossTemplate, "boss_template", MapGenRoomCategory.Boss);
                PopulateRoomTemplate(questTemplate, "quest_template", MapGenRoomCategory.Quest);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                styleSet.RoomTemplates = new[] { startTemplate, bossTemplate, questTemplate, exitTemplate };

                var orderResult = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(orderResult.Success, Is.False);
                Assert.That(orderResult.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_quest_after_boss"));

                ruleSet.RequiredRoomCategories = new[]
                {
                    MapGenRoomCategory.Start,
                    MapGenRoomCategory.Quest,
                    MapGenRoomCategory.Boss,
                    MapGenRoomCategory.Exit
                };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                ruleSet.DistanceRules.MinStartToBossDistance = 999;

                var distanceResult = MapGenTemplateMockupSolver.Generate(profile, 7001, maxAttempts: 2);

                Assert.That(distanceResult.Success, Is.False);
                Assert.That(distanceResult.AttemptCount, Is.EqualTo(2));
                Assert.That(distanceResult.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_start_boss_distance_too_short"));
                Assert.That(distanceResult.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_retry_exhausted"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(exitTemplate);
                Object.DestroyImmediate(questTemplate);
                Object.DestroyImmediate(bossTemplate);
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
        public void TemplateMockupSolverReportsCandidateExhaustionAfterFootprintPropagation()
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
                profile.MapSize = new Vector2Int(3, 2);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_no_room_placement_candidates"));
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
        public void TemplateMockupSolverAppliesLoopPolicyWhenLoopRateIsCertain()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var questTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(12, 8);
                ruleSet.LoopRate = 100;
                ruleSet.RequiredRoomCategories = new[]
                {
                    MapGenRoomCategory.Start,
                    MapGenRoomCategory.Quest,
                    MapGenRoomCategory.Exit
                };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(questTemplate, "quest_template", MapGenRoomCategory.Quest);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                styleSet.RoomTemplates = new[] { startTemplate, questTemplate, exitTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(result.Success, Is.True);
                Assert.That(result.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => cell.State == MapGenCellState.Corridor
                        && cell.SourceTemplateId == "loop_policy"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(exitTemplate);
                Object.DestroyImmediate(questTemplate);
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
        public void TemplateMockupSolverAppliesOptionalBranchDeadEndPolicy()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var sideTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(14, 4);
                ruleSet.MinRooms = 3;
                ruleSet.MaxRooms = 3;
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.OptionalRoomCategories = new[] { MapGenRoomCategory.Side };
                ruleSet.QuantityRules.MinRooms = 3;
                ruleSet.QuantityRules.MaxRooms = 3;
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                ruleSet.QuantityRules.OptionalCategories = ruleSet.OptionalRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                PopulateRoomTemplate(sideTemplate, "side_template", MapGenRoomCategory.Side);
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate, sideTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(result.Success, Is.True);
                Assert.That(result.Report.IsValid, Is.True);
                Assert.That(result.Cells, Has.Exactly(4).Matches<MapGenMockupCell>(
                    cell => IsRoomFootprintCell(cell)
                        && cell.RoomCategory == MapGenRoomCategory.Side
                        && cell.SourceTemplateId == "side_template"));
                Assert.That(result.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => cell.State == MapGenCellState.Corridor));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(sideTemplate);
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
        public void TemplateMockupSolverCollapsesLowestEntropyOptionalBranch()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var startTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var exitTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var bossTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var sideTemplate = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            GameObject floor = null;
            GameObject wall = null;

            try
            {
                PopulateValidWorkflowProfile(profile, styleSet, moduleSet, ruleSet, roomShape, out floor, out wall);
                profile.MapSize = new Vector2Int(18, 6);
                ruleSet.MinRooms = 3;
                ruleSet.MaxRooms = 3;
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.OptionalRoomCategories = new[] { MapGenRoomCategory.Boss, MapGenRoomCategory.Side };
                ruleSet.QuantityRules.MinRooms = 3;
                ruleSet.QuantityRules.MaxRooms = 3;
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                ruleSet.QuantityRules.OptionalCategories = ruleSet.OptionalRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                PopulateRoomTemplate(bossTemplate, "boss_template", MapGenRoomCategory.Boss);
                PopulateRoomTemplate(sideTemplate, "side_template", MapGenRoomCategory.Side);
                sideTemplate.Footprint = new Vector2Int(4, 3);
                sideTemplate.FloorCells = new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                    new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2)
                };
                sideTemplate.Connectors = new MapGenConnector[0];
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate, bossTemplate, sideTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001);

                Assert.That(result.Success, Is.True);
                Assert.That(result.Cells, Has.Some.Matches<MapGenMockupCell>(
                    cell => IsRoomFootprintCell(cell)
                        && cell.RoomCategory == MapGenRoomCategory.Side
                        && cell.SourceTemplateId == "side_template"));
                Assert.That(result.Cells, Has.None.Matches<MapGenMockupCell>(
                    cell => IsRoomFootprintCell(cell)
                        && cell.RoomCategory == MapGenRoomCategory.Boss
                        && cell.SourceTemplateId == "boss_template"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(sideTemplate);
                Object.DestroyImmediate(bossTemplate);
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
        public void TemplateMockupSolverPropagatesBlockedCellsIntoPlacementCandidates()
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
                profile.MapSize = new Vector2Int(3, 1);
                ruleSet.RequiredRoomCategories = new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
                ruleSet.QuantityRules.RequiredCategories = ruleSet.RequiredRoomCategories;
                PopulateRoomTemplate(startTemplate, "start_template", MapGenRoomCategory.Start);
                PopulateRoomTemplate(exitTemplate, "exit_template", MapGenRoomCategory.Exit);
                ConfigureOneByTwoBlockingTemplate(startTemplate, MapGenRoomCategory.Start);
                ConfigureOneByTwoBlockingTemplate(exitTemplate, MapGenRoomCategory.Exit);
                styleSet.RoomTemplates = new[] { startTemplate, exitTemplate };

                var result = MapGenTemplateMockupSolver.Generate(profile, 7001, maxAttempts: 2);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_no_room_placement_candidates"));
                Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "production_solver_retry_exhausted"));
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
            Assert.That(report.PassesRun, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void PostProcessorExecutesFullConfiguredPassList()
        {
            var cells = new MapGenMockupCell[25];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            for (var y = 1; y <= 3; y++)
            {
                for (var x = 1; x <= 3; x++)
                {
                    cells[(y * 5) + x] = new MapGenMockupCell
                    {
                        State = MapGenCellState.Room,
                        RegionId = 1,
                        RoomCategory = MapGenRoomCategory.Main
                    };
                }
            }

            var passOrder = new[]
            {
                MapGenPostProcessPassKind.SplitLargeRooms,
                MapGenPostProcessPassKind.ConsolidatePaths,
                MapGenPostProcessPassKind.AddLoops,
                MapGenPostProcessPassKind.NormalizeRouteLengths,
                MapGenPostProcessPassKind.WidenCleanCorridors,
                MapGenPostProcessPassKind.MergeCompatibleAdjacentRooms
            };
            var report = MapGenMockupPostProcessor.Apply(5, 5, cells, new MapGenPostProcessOptions
            {
                SplitLargeRooms = true,
                ConsolidatePaths = true,
                AddLoops = true,
                NormalizeRouteLengths = true,
                WidenCleanCorridors = true,
                MergeCompatibleAdjacentRooms = true,
                MaxPasses = 1,
                PassOrder = passOrder
            });

            Assert.That(report.PassReports, Has.Length.EqualTo(passOrder.Length));
            for (var i = 0; i < passOrder.Length; i++)
            {
                Assert.That(report.PassReports[i].PassKind, Is.EqualTo(passOrder[i]));
            }

            Assert.That(report.PassReports, Has.Some.Matches<MapGenPostProcessPassReport>(
                pass => pass.PassKind == MapGenPostProcessPassKind.SplitLargeRooms
                    && pass.ChangedCells > 0));
            Assert.That(report.Changed, Is.True);
        }

        [Test]
        public void PostProcessorReportsConfiguredPassOrderAndSignatures()
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
                UseDirectRoutes = true,
                FillEnclosedEmptySpace = true,
                MaxPasses = 1,
                PassOrder = new[]
                {
                    MapGenPostProcessPassKind.FillEnclosedEmptySpace,
                    MapGenPostProcessPassKind.AddDirectRoutes
                }
            });

            Assert.That(report.PassReports, Has.Length.EqualTo(2));
            Assert.That(report.PassReports[0].PassKind, Is.EqualTo(MapGenPostProcessPassKind.FillEnclosedEmptySpace));
            Assert.That(report.PassReports[1].PassKind, Is.EqualTo(MapGenPostProcessPassKind.AddDirectRoutes));
            Assert.That(report.PassReports[1].ChangedCells, Is.EqualTo(report.DirectRouteCellsAdded));
            Assert.That(report.PassReports[1].BeforeSignature, Is.Not.EqualTo(report.PassReports[1].AfterSignature));
            Assert.That(report.PassReports[1].ChangedCoords, Has.Length.EqualTo(report.DirectRouteCellsAdded));
            Assert.That(report.PassReports[1].ChangedCoords, Has.Some.EqualTo(new MapGenGridCoord(1, 0)));
        }

        [Test]
        public void MockupDraftPostProcessingUsesStructuredRuleSettings()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();

            try
            {
                profile.LayoutRules = ruleSet;
                ruleSet.UseDirectRoutes = false;
                ruleSet.PostProcessRules = MapGenPostProcessRules.Defaults();
                ruleSet.PostProcessRules.UseDirectRoutes = true;
                ruleSet.PostProcessRules.MaxPasses = 2;
                draft.Profile = profile;
                draft.GridSize = new Vector2Int(5, 5);
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RoomCategory = MapGenRoomCategory.Start
                };
                draft.Cells[24] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RoomCategory = MapGenRoomCategory.Exit
                };

                var report = draft.ApplyPostProcessingFromProfile();

                Assert.That(report.DirectRouteCellsAdded, Is.GreaterThan(0));
                Assert.That(report.PassesRun, Is.EqualTo(2));
                Assert.That(draft.LastDirectRouteCellsAdded, Is.EqualTo(report.DirectRouteCellsAdded));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(ruleSet);
            }
        }

        [Test]
        public void PostProcessorRollsBackPassThatBreaksRequiredTraversal()
        {
            var cells = new MapGenMockupCell[9];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            cells[0] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Start };
            cells[8] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Exit };

            var report = MapGenMockupPostProcessor.Apply(3, 3, cells, new MapGenPostProcessOptions
            {
                RemoveSmallRooms = true,
                MaxPasses = 1
            });

            Assert.That(report.Rollbacks, Is.EqualTo(1));
            Assert.That(report.RequiredConnectivityValid, Is.False);
            Assert.That(report.IsolatedRoomsRemoved, Is.Zero);
            Assert.That(cells[0].State, Is.EqualTo(MapGenCellState.Room));
            Assert.That(cells[8].State, Is.EqualTo(MapGenCellState.Room));
        }

        [Test]
        public void MockupFeasibilityValidatorReportsBlockedRequiredTraversal()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Start },
                new MapGenMockupCell { State = MapGenCellState.Blocked },
                new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Exit }
            };

            var report = MapGenMockupFeasibilityValidator.Validate(3, 1, cells);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "mockup_blocked_required_traversal"
                    && issue.ContextPath == "Draft.Cells"
                    && issue.Cell == new MapGenGridCoord(0, 0)));
        }

        [Test]
        public void PostProcessorSafetyValidatorReportsRollbackRisk()
        {
            var cells = new MapGenMockupCell[9];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            cells[0] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Start };
            cells[8] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Exit };

            var report = MapGenMockupPostProcessor.ValidateSafety(3, 3, cells, new MapGenPostProcessOptions
            {
                RemoveSmallRooms = true,
                MaxPasses = 1
            });

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "post_process_required_connectivity_invalid"));
            Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "post_process_pass_requires_rollback"
                    && issue.Severity == MapGenIssueSeverity.Warning));
        }

        [Test]
        public void PostProcessorFillsDesignerReservedMasks()
        {
            var cells = new MapGenMockupCell[3];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            cells[0] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Start };
            cells[1] = new MapGenMockupCell { State = MapGenCellState.Reserved };
            cells[2] = new MapGenMockupCell { State = MapGenCellState.Room, RoomCategory = MapGenRoomCategory.Exit };

            var report = MapGenMockupPostProcessor.Apply(3, 1, cells, new MapGenPostProcessOptions
            {
                FillReservedMasks = true,
                MaxPasses = 1
            });

            Assert.That(report.ReservedMaskCellsFilled, Is.EqualTo(1));
            Assert.That(report.EnclosedEmptyCellsFilled, Is.EqualTo(1));
            Assert.That(report.PassReports, Has.Exactly(1).Matches<MapGenPostProcessPassReport>(
                pass => pass.PassKind == MapGenPostProcessPassKind.FillEnclosedEmptySpace
                    && pass.ChangedCoords.Length == 1
                    && pass.ChangedCoords[0] == new MapGenGridCoord(1, 0)));
            Assert.That(cells[1].State, Is.EqualTo(MapGenCellState.Corridor));
        }

        [Test]
        public void PostProcessorFillsEnclosedEmptySpace()
        {
            var cells = new MapGenMockupCell[9];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            cells[1] = new MapGenMockupCell { State = MapGenCellState.Corridor };
            cells[3] = new MapGenMockupCell { State = MapGenCellState.Room };
            cells[5] = new MapGenMockupCell { State = MapGenCellState.Connector };
            cells[7] = new MapGenMockupCell { State = MapGenCellState.Corridor };

            var report = MapGenMockupPostProcessor.Apply(3, 3, cells, new MapGenPostProcessOptions
            {
                FillEnclosedEmptySpace = true,
                MaxPasses = 1
            });

            Assert.That(report.EnclosedEmptyCellsFilled, Is.EqualTo(1));
            Assert.That(report.Changed, Is.True);
            Assert.That(cells[4].State, Is.EqualTo(MapGenCellState.Corridor));
        }

        [Test]
        public void MaterializationClassifierCreatesFloorAndWallRequests()
        {
            var cells = new[]
            {
                new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 7,
                    RoomCategory = MapGenRoomCategory.Start,
                    SourceTemplateId = "start_template"
                }
            };

            var requests = MapGenMaterializationClassifier.Classify(1, 1, cells);

            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.FloorA
                    && request.RegionId == 7
                    && request.SourceTemplateId == "start_template"));
            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.NavigationHelper
                    && request.RegionId == 7
                    && request.SourceTemplateId == "start_template"));
            Assert.That(requests, Has.Exactly(4).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.WallStraight
                    && request.RegionId == 7
                    && request.SourceTemplateId == "start_template"));
            Assert.That(requests, Has.Exactly(4).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.WallCornerOutside));
            Assert.That(requests, Has.None.Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.WallCornerInside));
        }

        [Test]
        public void MaterializationClassifierCreatesInsideCornerRequestsForConcaveCells()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Empty }
            };

            var requests = MapGenMaterializationClassifier.Classify(2, 2, cells);

            Assert.That(requests, Has.Some.Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.WallCornerInside
                    && request.Coord == new MapGenGridCoord(0, 0)
                    && request.RegionId == 1));
        }

        [Test]
        public void MaterializationClassifierCreatesExteriorCeilingForCorridors()
        {
            var cells = new[]
            {
                new MapGenMockupCell
                {
                    State = MapGenCellState.Corridor,
                    RegionId = 12,
                    SourceTemplateId = "corridor_template"
                }
            };

            var requests = MapGenMaterializationClassifier.Classify(1, 1, cells);

            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.FloorB
                    && request.RegionId == 12
                    && request.SourceTemplateId == "corridor_template"));
            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.CeilingExterior
                    && request.RegionId == 12
                    && request.SourceTemplateId == "corridor_template"));
            Assert.That(requests, Has.None.Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.CeilingInterior));
        }

        [Test]
        public void MaterializationClassifierRequestsWholeAndSplitDoorModules()
        {
            var cells = new[]
            {
                new MapGenMockupCell
                {
                    State = MapGenCellState.Connector,
                    RegionId = 9,
                    RoomCategory = MapGenRoomCategory.Start,
                    SocketKind = MapGenSocketKind.Door,
                    SocketId = "main",
                    SourceTemplateId = "door_template"
                }
            };

            var requests = MapGenMaterializationClassifier.Classify(1, 1, cells);

            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.DoorWhole
                    && request.RegionId == 9
                    && request.ConnectorWidth == 1
                    && request.SourceTemplateId == "door_template"));
            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.DoorFrameHalf));
            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.DoorPanelHalf));
        }

        [Test]
        public void MaterializationClassifierUsesConnectorWidthForDoorOpenings()
        {
            var cells = new[]
            {
                new MapGenMockupCell
                {
                    State = MapGenCellState.Connector,
                    RegionId = 9,
                    SocketKind = MapGenSocketKind.Door,
                    SocketId = "wide",
                    SocketWidth = 2
                },
                new MapGenMockupCell
                {
                    State = MapGenCellState.Connector,
                    RegionId = 9,
                    SocketKind = MapGenSocketKind.Door,
                    SocketId = "wide",
                    SocketWidth = 2
                }
            };

            var requests = MapGenMaterializationClassifier.Classify(2, 1, cells);

            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.DoorWhole
                    && request.ConnectorWidth == 2
                    && request.Coord == new MapGenGridCoord(0, 0)));
            Assert.That(requests, Has.None.Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.WallStraight
                    && request.Direction == MapGenGridDirection.North));
        }

        [Test]
        public void MaterializationPlanUsesPropPlacementRulesForPropRequests()
        {
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                ruleSet.PropPlacementRules = new[]
                {
                    new MapGenPropPlacementRules
                    {
                        Channel = "loot",
                        ChannelKind = MapGenPropPlacementChannelKind.Custom,
                        DistributionMode = MapGenPropDistributionMode.RequiredUnique,
                        DensityPercent = 100,
                        MinSpacingCells = 0,
                        RequiredUnique = true
                    }
                };
                profile.LayoutRules = ruleSet;
                draft.Profile = profile;
                draft.Seed = 101;
                draft.GridSize = new Vector2Int(2, 1);
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot" };
                draft.Cells[1] = new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot" };

                var plan = MapGenMockupMaterializer.BuildPlan(draft);

                Assert.That(plan.Requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                    request => request.Category == MapGenModuleCategory.Prop));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(ruleSet);
            }
        }

        [Test]
        public void MaterializationClassifierDoesNotRequestDoorsForCorridorSockets()
        {
            var cells = new[]
            {
                new MapGenMockupCell
                {
                    State = MapGenCellState.Connector,
                    RegionId = 9,
                    RoomCategory = MapGenRoomCategory.Start,
                    SocketKind = MapGenSocketKind.Corridor,
                    SocketId = "main"
                }
            };

            var requests = MapGenMaterializationClassifier.Classify(1, 1, cells);

            Assert.That(requests, Has.None.Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.DoorWhole
                    || request.Category == MapGenModuleCategory.DoorFrameHalf
                    || request.Category == MapGenModuleCategory.DoorPanelHalf));
        }

        [Test]
        public void MaterializationClassifierCreatesBlockerRequestsForBlockedAndReservedCells()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Corridor, RegionId = 2 },
                new MapGenMockupCell { State = MapGenCellState.Blocked, RegionId = 3, SourceTemplateId = "blocked_template" },
                new MapGenMockupCell { State = MapGenCellState.Reserved, RegionId = 4, SourceTemplateId = "reserved_template" },
                new MapGenMockupCell { State = MapGenCellState.Empty, RegionId = 5 }
            };

            var requests = MapGenMaterializationClassifier.Classify(5, 1, cells);

            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.FloorA
                    && request.Coord == new MapGenGridCoord(0, 0)));
            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.FloorB
                    && request.Coord == new MapGenGridCoord(1, 0)));
            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.Blocker
                    && request.Coord == new MapGenGridCoord(2, 0)
                    && request.RegionId == 3
                    && request.SourceTemplateId == "blocked_template"));
            Assert.That(requests, Has.Exactly(1).Matches<MapGenModuleRequest>(
                request => request.Category == MapGenModuleCategory.Blocker
                    && request.Coord == new MapGenGridCoord(3, 0)
                    && request.RegionId == 4
                    && request.SourceTemplateId == "reserved_template"));
            Assert.That(requests, Has.None.Matches<MapGenModuleRequest>(
                request => request.Coord == new MapGenGridCoord(4, 0)));
            Assert.That(requests, Has.None.Matches<MapGenModuleRequest>(
                request => (request.Coord == new MapGenGridCoord(2, 0) || request.Coord == new MapGenGridCoord(3, 0))
                    && request.Category != MapGenModuleCategory.Blocker));
        }

        [Test]
        public void MaterializationReportNamesMissingModuleCategories()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var floor = new GameObject("FloorModule");

            try
            {
                moduleSet.FloorsA = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = floor,
                        Weight = 1,
                        Footprint = Vector2Int.one
                    }
                };
                var cells = new[]
                {
                    new MapGenMockupCell
                    {
                        State = MapGenCellState.Room,
                        RegionId = 3,
                        RoomCategory = MapGenRoomCategory.Start
                    }
                };
                var plan = MapGenMaterializationPlanner.Build(1, 1, 1f, "signature", cells);

                var report = MapGenMockupMaterializer.BuildReport(moduleSet, plan);

                Assert.That(report.TotalRequests, Is.EqualTo(plan.RequestCount));
                Assert.That(report.InstantiableRequests, Is.EqualTo(plan.RequestCount));
                Assert.That(report.MissingModuleRequests, Is.GreaterThan(0));
                Assert.That(report.MissingModuleCategories, Contains.Item(nameof(MapGenModuleCategory.WallStraight)));
                Assert.That(report.SelectedPrefabNames, Contains.Item($"Placeholder({nameof(MapGenModuleCategory.WallStraight)})"));
            }
            finally
            {
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(moduleSet);
            }
        }

        [Test]
        public void MaterializationCoverageValidationWarnsForMissingModules()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var floor = new GameObject("FloorModule");

            try
            {
                moduleSet.FloorsA = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = floor,
                        Weight = 1,
                        Footprint = Vector2Int.one
                    }
                };
                var cells = new[]
                {
                    new MapGenMockupCell
                    {
                        State = MapGenCellState.Room,
                        RegionId = 3,
                        RoomCategory = MapGenRoomCategory.Start
                    }
                };
                var plan = MapGenMaterializationPlanner.Build(1, 1, 1f, "signature", cells);
                var report = MapGenMockupMaterializer.BuildReport(moduleSet, plan);

                var validation = MapGenMockupMaterializer.ValidateCoverage(report);

                Assert.That(validation.IsValid, Is.True);
                Assert.That(validation.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "materialization_missing_module_category"
                        && issue.ContextPath == $"ModuleSet.{nameof(MapGenModuleCategory.WallStraight)}"
                        && issue.Severity == MapGenIssueSeverity.Warning
                        && !issue.BlocksGeneration));
            }
            finally
            {
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(moduleSet);
            }
        }

        [Test]
        public void MaterializerCreatesReadablePlaceholdersForMissingPrefabs()
        {
            const string tempFolder = "Assets/Conn/Tests/TempMapGenV2MaterializerPlaceholders";
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            GameObject root = null;

            try
            {
                EnsureTempFolder(tempFolder);
                var floorPrefab = CreateTempPrefab($"{tempFolder}/Floor.prefab", "Floor");
                moduleSet.FloorsA = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = floorPrefab,
                        Weight = 1,
                        Footprint = Vector2Int.one
                    }
                };
                styleSet.StyleId = "placeholder_style";
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "placeholder_profile";
                profile.StyleSet = styleSet;
                draft.Profile = profile;
                draft.Seed = 77;
                draft.GridSize = Vector2Int.one;
                draft.Cells = new[]
                {
                    new MapGenMockupCell
                    {
                        State = MapGenCellState.Room,
                        RegionId = 5,
                        RoomCategory = MapGenRoomCategory.Start,
                        SourceTemplateId = "missing_wall_template"
                    }
                };
                draft.Accept();

                root = MapGenMockupMaterializer.Materialize(draft, MapGenV2SceneOutputMode.CreateNewRoot);

                Assert.That(root, Is.Not.Null);
                var marker = root.GetComponent<MapGenV2GeneratedMapMarker>();
                Assert.That(marker.MaterializationMissingModuleCount, Is.GreaterThan(0));
                var moduleMarkers = root.GetComponentsInChildren<MapGenV2MaterializedModuleMarker>();
                Assert.That(moduleMarkers, Has.Some.Matches<MapGenV2MaterializedModuleMarker>(
                    moduleMarker => moduleMarker.PrefabName == "Placeholder"
                        && moduleMarker.ModuleCategory == MapGenModuleCategory.WallStraight
                        && moduleMarker.name.Contains("Tmissing_wall_template")));
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                AssetDatabase.DeleteAsset(tempFolder);
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(styleSet);
                Object.DestroyImmediate(moduleSet);
            }
        }

        [Test]
        public void MaterializationReportUsesDeterministicWeightedSelectionAndFootprints()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var floorA = new GameObject("FloorA");
            var floorB = new GameObject("FloorB");

            try
            {
                moduleSet.FloorsA = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = floorA,
                        Weight = 1,
                        Footprint = new Vector2Int(2, 1)
                    },
                    new MapGenModuleEntry
                    {
                        Prefab = floorB,
                        Weight = 3,
                        Footprint = new Vector2Int(2, 1)
                    }
                };
                var cells = new[]
                {
                    new MapGenMockupCell
                    {
                        State = MapGenCellState.Room,
                        RegionId = 1,
                        RoomCategory = MapGenRoomCategory.Start
                    },
                    new MapGenMockupCell
                    {
                        State = MapGenCellState.Room,
                        RegionId = 2,
                        RoomCategory = MapGenRoomCategory.Exit
                    }
                };
                var plan = MapGenMaterializationPlanner.Build(2, 1, 1f, "signature", cells);

                var first = MapGenMockupMaterializer.BuildReport(moduleSet, plan, 1234);
                var second = MapGenMockupMaterializer.BuildReport(moduleSet, plan, 1234);

                Assert.That(second.SelectedPrefabNames, Is.EqualTo(first.SelectedPrefabNames));
                Assert.That(first.InstantiableRequests, Is.EqualTo(plan.RequestCount));
                Assert.That(first.FootprintOutOfBoundsRequests, Is.GreaterThan(0));
                Assert.That(first.FootprintOverlapRequests, Is.GreaterThan(0));
                Assert.That(first.HasFootprintIssues, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(floorB);
                Object.DestroyImmediate(floorA);
                Object.DestroyImmediate(moduleSet);
            }
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
        public void PropPlacementPlannerIsDeterministicAndRespectsSpacing()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot" },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot" },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot" },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot" }
            };
            var rules = new[]
            {
                new MapGenPropPlacementRules
                {
                    Channel = "loot",
                    ChannelKind = MapGenPropPlacementChannelKind.Custom,
                    DistributionMode = MapGenPropDistributionMode.MarkerBased,
                    DensityPercent = 100,
                    MinSpacingCells = 1
                }
            };

            var first = MapGenPropPlacementPlanner.Build(4, 1, cells, rules, 42);
            var second = MapGenPropPlacementPlanner.Build(4, 1, cells, rules, 42);

            Assert.That(first.Report.IsValid, Is.True);
            Assert.That(first.PlacedProps.Length, Is.EqualTo(second.PlacedProps.Length));
            for (var i = 0; i < first.PlacedProps.Length; i++)
            {
                Assert.That(first.PlacedProps[i].Coord, Is.EqualTo(second.PlacedProps[i].Coord));
            }

            for (var a = 0; a < first.PlacedProps.Length; a++)
            {
                for (var b = a + 1; b < first.PlacedProps.Length; b++)
                {
                    var distance = Mathf.Abs(first.PlacedProps[a].Coord.X - first.PlacedProps[b].Coord.X)
                        + Mathf.Abs(first.PlacedProps[a].Coord.Y - first.PlacedProps[b].Coord.Y);
                    Assert.That(distance, Is.GreaterThan(1));
                }
            }
        }

        [Test]
        public void PropPlacementPlannerPlacesOnePropPerRegion()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, RoomCategory = MapGenRoomCategory.Start },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, RoomCategory = MapGenRoomCategory.Start },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 2, RoomCategory = MapGenRoomCategory.Exit },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 2, RoomCategory = MapGenRoomCategory.Exit }
            };
            var rules = new[]
            {
                new MapGenPropPlacementRules
                {
                    Channel = "reward",
                    ChannelKind = MapGenPropPlacementChannelKind.Floor,
                    DistributionMode = MapGenPropDistributionMode.OnePerRegion,
                    DensityPercent = 100,
                    MinSpacingCells = 0
                }
            };

            var result = MapGenPropPlacementPlanner.Build(4, 1, cells, rules, 7);

            Assert.That(result.Report.IsValid, Is.True);
            Assert.That(result.PlacedProps, Has.Length.EqualTo(2));
            Assert.That(result.PlacedProps[0].RegionId, Is.Not.EqualTo(result.PlacedProps[1].RegionId));
        }

        [Test]
        public void PropPlacementPlannerAppliesRoomAndCorridorFilters()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, RoomCategory = MapGenRoomCategory.Start },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 2, RoomCategory = MapGenRoomCategory.Exit },
                new MapGenMockupCell { State = MapGenCellState.Corridor, RegionId = 3, SourceTemplateId = "main_corridor" },
                new MapGenMockupCell { State = MapGenCellState.Corridor, RegionId = 4, SourceTemplateId = "side_corridor" }
            };
            var rules = new[]
            {
                new MapGenPropPlacementRules
                {
                    Channel = "start_reward",
                    ChannelKind = MapGenPropPlacementChannelKind.Floor,
                    DistributionMode = MapGenPropDistributionMode.MarkerBased,
                    RoomCategoryFilters = new[] { nameof(MapGenRoomCategory.Start) },
                    DensityPercent = 100,
                    MinSpacingCells = 0
                },
                new MapGenPropPlacementRules
                {
                    Channel = "corridor_decor",
                    ChannelKind = MapGenPropPlacementChannelKind.Floor,
                    DistributionMode = MapGenPropDistributionMode.MarkerBased,
                    CorridorKindFilters = new[] { "side_corridor" },
                    DensityPercent = 100,
                    MinSpacingCells = 0
                }
            };

            var result = MapGenPropPlacementPlanner.Build(4, 1, cells, rules, 5);

            Assert.That(result.Report.IsValid, Is.True);
            Assert.That(result.PlacedProps, Has.Exactly(1).Matches<MapGenPlacedProp>(
                prop => prop.Channel == "start_reward" && prop.Coord == new MapGenGridCoord(0, 0)));
            Assert.That(result.PlacedProps, Has.Exactly(1).Matches<MapGenPlacedProp>(
                prop => prop.Channel == "corridor_decor" && prop.Coord == new MapGenGridCoord(3, 0)));
        }

        [Test]
        public void PropPlacementPlannerBuildsPreviewFromDraftRules()
        {
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();

            try
            {
                ruleSet.PropPlacementRules = new[]
                {
                    new MapGenPropPlacementRules
                    {
                        Channel = "objective",
                        ChannelKind = MapGenPropPlacementChannelKind.Objective,
                        DistributionMode = MapGenPropDistributionMode.RequiredUnique,
                        DensityPercent = 100,
                        MinSpacingCells = 0,
                        RequiredUnique = true
                    }
                };
                profile.LayoutRules = ruleSet;
                draft.Profile = profile;
                draft.Seed = 909;
                draft.GridSize = Vector2Int.one;
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 3,
                    RoomCategory = MapGenRoomCategory.Quest,
                    PropChannel = "objective"
                };

                var result = MapGenPropPlacementPlanner.BuildForDraft(draft);

                Assert.That(result.Report.IsValid, Is.True);
                Assert.That(result.Report.PlacedCount, Is.EqualTo(1));
                Assert.That(result.PlacedProps[0].Channel, Is.EqualTo("objective"));
                Assert.That(result.PlacedProps[0].ChannelKind, Is.EqualTo(MapGenPropPlacementChannelKind.Objective));
            }
            finally
            {
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(ruleSet);
            }
        }

        [Test]
        public void PropPlacementPlannerSupportsWallCornerAndPerimeterChannels()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 }
            };
            var wallRules = new[]
            {
                new MapGenPropPlacementRules
                {
                    Channel = "wall_decor",
                    ChannelKind = MapGenPropPlacementChannelKind.Wall,
                    DistributionMode = MapGenPropDistributionMode.Perimeter,
                    DensityPercent = 100,
                    MinSpacingCells = 0
                }
            };
            var cornerRules = new[]
            {
                new MapGenPropPlacementRules
                {
                    Channel = "corner_decor",
                    ChannelKind = MapGenPropPlacementChannelKind.Corner,
                    DistributionMode = MapGenPropDistributionMode.MarkerBased,
                    DensityPercent = 100,
                    MinSpacingCells = 0
                }
            };

            var wallResult = MapGenPropPlacementPlanner.Build(3, 3, cells, wallRules, 13);
            var cornerResult = MapGenPropPlacementPlanner.Build(3, 3, cells, cornerRules, 13);

            Assert.That(wallResult.Report.IsValid, Is.True);
            Assert.That(cornerResult.Report.IsValid, Is.True);
            Assert.That(wallResult.PlacedProps, Has.None.Matches<MapGenPlacedProp>(
                prop => prop.Channel == "wall_decor" && prop.Coord == new MapGenGridCoord(1, 1)));
            Assert.That(cornerResult.PlacedProps, Has.Some.Matches<MapGenPlacedProp>(
                prop => prop.Channel == "corner_decor" && prop.Coord == new MapGenGridCoord(0, 0)));
            Assert.That(cornerResult.PlacedProps, Has.None.Matches<MapGenPlacedProp>(
                prop => prop.Channel == "corner_decor" && prop.Coord == new MapGenGridCoord(1, 1)));
        }

        [Test]
        public void PropPlacementPlannerUsesPropChannelWeightsForWeightedRandom()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot", PropWeight = 1000 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "loot", PropWeight = 1 }
            };
            var rules = new[]
            {
                new MapGenPropPlacementRules
                {
                    Channel = "loot",
                    ChannelKind = MapGenPropPlacementChannelKind.Custom,
                    DistributionMode = MapGenPropDistributionMode.WeightedRandom,
                    DensityPercent = 50,
                    MinSpacingCells = 0
                }
            };

            var result = MapGenPropPlacementPlanner.Build(2, 1, cells, rules, 3);

            Assert.That(result.Report.IsValid, Is.True);
            Assert.That(result.PlacedProps, Has.Length.EqualTo(1));
            Assert.That(result.PlacedProps[0].Coord, Is.EqualTo(new MapGenGridCoord(0, 0)));
        }

        [Test]
        public void RoomTemplateValidatorReportsInvalidPropChannelWeight()
        {
            var template = ScriptableObject.CreateInstance<MapGenRoomTemplateAsset>();

            try
            {
                template.TemplateId = "weighted_prop_template";
                template.Footprint = Vector2Int.one;
                template.FloorCells = new[] { Vector2Int.zero };
                template.PropChannels = new[]
                {
                    new MapGenTemplatePropChannel
                    {
                        LocalCell = Vector2Int.zero,
                        Channel = "loot",
                        Weight = -1
                    }
                };

                var report = template.Validate();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "room_template_prop_channel_invalid_weight"));
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        [Test]
        public void PropPlacementPlannerReportsBlockerTraversalBreaks()
        {
            var cells = new[]
            {
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1, PropChannel = "blocker" },
                new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 }
            };
            var rules = new[]
            {
                new MapGenPropPlacementRules
                {
                    Channel = "blocker",
                    ChannelKind = MapGenPropPlacementChannelKind.Blocker,
                    DistributionMode = MapGenPropDistributionMode.MarkerBased,
                    DensityPercent = 100,
                    MinSpacingCells = 0,
                    AllowTraversalBlocking = false
                }
            };

            var result = MapGenPropPlacementPlanner.Build(3, 1, cells, rules, 11);

            Assert.That(result.Report.IsValid, Is.False);
            Assert.That(result.Report.BlockerTraversalIssues, Is.EqualTo(1));
            Assert.That(result.Report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "prop_placement_blocker_breaks_traversal"
                    && issue.Cell == new MapGenGridCoord(1, 0)));
            Assert.That(result.PlacedProps, Is.Empty);
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

        [Test]
        public void RuntimeBakeBuilderCreatesRegionsConnectorsAndQueriesPath()
        {
            var cells = new[]
            {
                new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 1,
                    RoomCategory = MapGenRoomCategory.Start,
                    SourceTemplateId = "start_template"
                },
                new MapGenMockupCell
                {
                    State = MapGenCellState.Connector,
                    RegionId = 1,
                    SocketKind = MapGenSocketKind.Door,
                    SocketId = "main",
                    SocketWidth = 2,
                    SourceTemplateId = "door_template"
                },
                new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 2,
                    RoomCategory = MapGenRoomCategory.Exit,
                    SourceTemplateId = "exit_template"
                }
            };
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                baked.Version = 1;
                baked.Width = 3;
                baked.Height = 1;
                baked.Cells = MapGenRuntimeBakeDataBuilder.BuildCells(3, 1, cells);
                baked.Regions = MapGenRuntimeBakeDataBuilder.BuildRegions(3, 1, cells);
                baked.Connectors = MapGenRuntimeBakeDataBuilder.BuildConnectors(3, 1, cells);
                baked.TraversalEdges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(3, 1, cells);

                var query = new MapGenRuntimeMapQuery(baked);

                Assert.That(baked.Regions, Has.Exactly(1).Matches<MapGenBakedRegion>(
                    region => region.RegionId == 1
                        && region.CellCount == 2
                        && region.SourceTemplateId == "start_template"));
                Assert.That(baked.Connectors, Has.Exactly(1).Matches<MapGenBakedConnector>(
                    connector => connector.SocketKind == MapGenSocketKind.Door
                        && connector.SocketId == "main"
                        && connector.SocketWidth == 2));
                Assert.That(query.TryFindPath(new MapGenGridCoord(0, 0), new MapGenGridCoord(2, 0), out var path), Is.True);
                Assert.That(path, Has.Length.EqualTo(3));
                Assert.That(query.HasTraversalEdge(new MapGenGridCoord(0, 0), new MapGenGridCoord(1, 0)), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void RuntimeBakeUtilityWritesRuntimeSafePropsAndMarkers()
        {
            const string tempFolder = "Assets/Conn/Tests/TempMapGenV2Bake";
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            MapGenBakedMapAsset baked = null;

            try
            {
                EnsureTempFolder(tempFolder);
                ruleSet.name = "runtime_rule_set";
                ruleSet.PropPlacementRules = new[]
                {
                    new MapGenPropPlacementRules
                    {
                        Channel = "objective",
                        ChannelKind = MapGenPropPlacementChannelKind.Objective,
                        DistributionMode = MapGenPropDistributionMode.RequiredUnique,
                        DensityPercent = 100,
                        MinSpacingCells = 0,
                        RequiredUnique = true
                    }
                };
                profile.ProfileId = "runtime_profile";
                profile.LayoutRules = ruleSet;
                profile.OutputSettings = MapGenOutputSettings.Defaults();
                profile.OutputSettings.BakedAssetFolder = tempFolder;
                draft.Profile = profile;
                draft.Seed = 404;
                draft.GridSize = Vector2Int.one;
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = 7,
                    RoomCategory = MapGenRoomCategory.Quest,
                    PropChannel = "objective",
                    PropWeight = 1,
                    SourceTemplateId = "quest_template"
                };
                draft.Accept();

                baked = MapGenRuntimeBakeUtility.Bake(draft);

                Assert.That(baked, Is.Not.Null);
                Assert.That(baked.Version, Is.EqualTo(1));
                Assert.That(baked.ProfileId, Is.EqualTo("runtime_profile"));
                Assert.That(baked.RuleSetId, Is.EqualTo("runtime_rule_set"));
                Assert.That(baked.Props, Has.Exactly(1).Matches<MapGenBakedPropInstance>(
                    prop => prop.Channel == "objective"
                        && prop.ChannelKind == nameof(MapGenPropPlacementChannelKind.Objective)
                        && prop.RegionId == 7));
                Assert.That(baked.ObjectiveMarkers, Has.Exactly(1).Matches<MapGenBakedMarker>(
                    marker => marker.Channel == "objective" && marker.RegionId == 7));
                Assert.That(baked.Regions, Has.Exactly(1).Matches<MapGenBakedRegion>(
                    region => region.RegionId == 7 && region.SourceTemplateId == "quest_template"));
                Assert.That(MapGenRuntimeBakeUtility.ValidateConsistency(draft, baked).IsValid, Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(tempFolder);
                Object.DestroyImmediate(baked);
                Object.DestroyImmediate(draft);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(ruleSet);
            }
        }

        [Test]
        public void RuntimeBakeConsistencyReportsStalePayload()
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                draft.Seed = 505;
                draft.GridSize = new Vector2Int(2, 1);
                draft.EnsureCellArray();
                draft.Cells[0] = new MapGenMockupCell { State = MapGenCellState.Room, RegionId = 1 };
                draft.Cells[1] = new MapGenMockupCell { State = MapGenCellState.Corridor, RegionId = 2 };
                draft.Accept();

                baked.Version = MapGenBakedMapMigration.CurrentVersion;
                baked.SourceSignature = "stale_signature";
                baked.Width = 1;
                baked.Height = 1;
                baked.Cells = System.Array.Empty<MapGenBakedCell>();
                baked.Regions = System.Array.Empty<MapGenBakedRegion>();
                baked.Connectors = System.Array.Empty<MapGenBakedConnector>();
                baked.TraversalEdges = System.Array.Empty<MapGenTraversalEdge>();

                var report = MapGenRuntimeBakeUtility.ValidateConsistency(draft, baked);

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "runtime_bake_stale_signature"
                        && issue.ContextPath == nameof(MapGenBakedMapAsset.SourceSignature)));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "runtime_bake_dimension_mismatch"));
                Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                    issue => issue.Code == "runtime_bake_payload_count_mismatch"
                        && issue.ContextPath == nameof(MapGenBakedMapAsset.Cells)));
            }
            finally
            {
                Object.DestroyImmediate(baked);
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void RuntimeMapServiceLoadsBakedMapAndExposesQuery()
        {
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();
            var host = new GameObject("runtime_map_service_test");

            try
            {
                baked.Version = MapGenBakedMapMigration.CurrentVersion;
                baked.Width = 2;
                baked.Height = 1;
                baked.Cells = new[]
                {
                    new MapGenBakedCell { Coord = new MapGenGridCoord(0, 0), State = MapGenCellState.Room },
                    new MapGenBakedCell { Coord = new MapGenGridCoord(1, 0), State = MapGenCellState.Corridor }
                };
                baked.TraversalEdges = new[]
                {
                    new MapGenTraversalEdge
                    {
                        From = new MapGenGridCoord(0, 0),
                        To = new MapGenGridCoord(1, 0)
                    }
                };

                var service = host.AddComponent<MapGenRuntimeMapService>();
                Assert.That(service.Load(baked), Is.True);

                Assert.That(service.IsLoaded, Is.True);
                Assert.That(service.TryGetQuery(out var query), Is.True);
                Assert.That(query.HasTraversalEdge(new MapGenGridCoord(0, 0), new MapGenGridCoord(1, 0)), Is.True);
                Assert.That(service.LastMigrationReport.IsValid, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void BakedMapMigrationNormalizesLegacyRuntimeData()
        {
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                baked.Version = 0;
                baked.Cells = null;
                baked.Regions = null;
                baked.Connectors = null;
                baked.TraversalEdges = null;
                baked.Props = null;
                baked.SpawnMarkers = null;
                baked.ObjectiveMarkers = null;

                var report = MapGenBakedMapMigration.MigrateInMemory(baked);

                Assert.That(report.IsValid, Is.True);
                Assert.That(report.WasMigrated, Is.True);
                Assert.That(baked.Version, Is.EqualTo(MapGenBakedMapMigration.CurrentVersion));
                Assert.That(baked.Cells, Is.Empty);
                Assert.That(baked.Regions, Is.Empty);
                Assert.That(baked.Connectors, Is.Empty);
                Assert.That(baked.TraversalEdges, Is.Empty);
                Assert.That(baked.Props, Is.Empty);
                Assert.That(baked.SpawnMarkers, Is.Empty);
                Assert.That(baked.ObjectiveMarkers, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void RuntimeMapServiceRejectsFutureBakedMapVersion()
        {
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();
            var host = new GameObject("runtime_map_service_future_version_test");

            try
            {
                baked.Version = MapGenBakedMapMigration.CurrentVersion + 1;
                var service = host.AddComponent<MapGenRuntimeMapService>();

                Assert.That(service.Load(baked), Is.False);
                Assert.That(service.IsLoaded, Is.False);
                Assert.That(service.LastMigrationReport.IsValid, Is.False);
                Assert.That(service.TryGetQuery(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void MapGenV2CompiledMapAdapterBuildsLegacyRuntimePlacements()
        {
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                baked.Version = MapGenBakedMapMigration.CurrentVersion;
                baked.ProfileId = "runtime_profile";
                baked.SourceSignature = "source_signature";
                baked.Seed = 77;
                baked.Width = 2;
                baked.Height = 1;
                baked.Cells = new[]
                {
                    new MapGenBakedCell
                    {
                        Coord = new MapGenGridCoord(0, 0),
                        State = MapGenCellState.Room,
                        RegionId = 1,
                        RoomCategory = MapGenRoomCategory.Start
                    },
                    new MapGenBakedCell
                    {
                        Coord = new MapGenGridCoord(1, 0),
                        State = MapGenCellState.Room,
                        RegionId = 2,
                        RoomCategory = MapGenRoomCategory.Quest
                    }
                };
                baked.Regions = new[]
                {
                    new MapGenBakedRegion
                    {
                        RegionId = 1,
                        RoomCategory = MapGenRoomCategory.Start,
                        CellCount = 1,
                        SourceTemplateId = "start_template"
                    },
                    new MapGenBakedRegion
                    {
                        RegionId = 2,
                        RoomCategory = MapGenRoomCategory.Quest,
                        CellCount = 1,
                        SourceTemplateId = "quest_template"
                    }
                };
                baked.Connectors = new[]
                {
                    new MapGenBakedConnector
                    {
                        Coord = new MapGenGridCoord(1, 0),
                        RegionId = 2,
                        SocketId = "door_a",
                        SocketWidth = 2
                    }
                };
                baked.Props = new[]
                {
                    new MapGenBakedPropInstance
                    {
                        Coord = new MapGenGridCoord(1, 0),
                        RegionId = 2,
                        Channel = "objective",
                        ChannelKind = "Objective"
                    }
                };
                baked.SpawnMarkers = new[]
                {
                    new MapGenBakedMarker
                    {
                        MarkerId = "spawn_0",
                        Coord = new MapGenGridCoord(0, 0),
                        RegionId = 1,
                        Channel = "spawn"
                    }
                };
                baked.ObjectiveMarkers = new[]
                {
                    new MapGenBakedMarker
                    {
                        MarkerId = "objective_0",
                        Coord = new MapGenGridCoord(1, 0),
                        RegionId = 2,
                        Channel = "objective"
                    }
                };

                var compiled = MapGenV2CompiledMapAdapter.ToCompiledMap(baked);

                Assert.That(compiled.MapId, Is.EqualTo("source_signature"));
                Assert.That(compiled.ProfileId, Is.EqualTo("runtime_profile"));
                Assert.That(compiled.Cells, Has.Count.EqualTo(2));
                Assert.That(compiled.RoomRecords, Has.Exactly(1).Matches<CompiledMapRoomRecord>(
                    room => room.Id == "region_1"
                        && room.Role == MapRoomRole.Start
                        && room.ChunkId == "start_template"));
                Assert.That(compiled.Sockets, Has.Exactly(1).Matches<CompiledMapSocketRecord>(
                    socket => socket.Id == "door_a" && socket.Width == 2 && socket.RoomId == "region_2"));
                Assert.That(compiled.Objects, Has.Exactly(1).Matches<CompiledMapObjectPlacement>(
                    prop => prop.Kind == RoomChunkObjectKind.Chest && prop.RuntimeReferenceId == "objective"));
                Assert.That(compiled.Placements, Has.Exactly(1).Matches<MapPlacement>(
                    placement => placement.Kind == MapPlacementKind.Start && placement.Id == "start_1"));
                Assert.That(compiled.Placements, Has.Exactly(1).Matches<MapPlacement>(
                    placement => placement.Kind == MapPlacementKind.Monster && placement.Id == "spawn_0"));
                Assert.That(compiled.Placements, Has.Exactly(1).Matches<MapPlacement>(
                    placement => placement.Kind == MapPlacementKind.QuestTarget && placement.Id == "objective_0"));
            }
            finally
            {
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void DungeonRuntimeServiceCanLoadMapGenV2BakedMapForSceneFlow()
        {
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                baked.Version = MapGenBakedMapMigration.CurrentVersion;
                baked.ProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId;
                baked.SourceSignature = "mapgenv2_scene_flow_signature";
                baked.Seed = CompiledMapDungeonRuntimeService.DefaultDungeonSeed;
                baked.Width = 2;
                baked.Height = 1;
                baked.Cells = new[]
                {
                    new MapGenBakedCell
                    {
                        Coord = new MapGenGridCoord(0, 0),
                        State = MapGenCellState.Room,
                        RegionId = 1,
                        RoomCategory = MapGenRoomCategory.Start
                    },
                    new MapGenBakedCell
                    {
                        Coord = new MapGenGridCoord(1, 0),
                        State = MapGenCellState.Room,
                        RegionId = 2,
                        RoomCategory = MapGenRoomCategory.Quest
                    }
                };
                baked.Regions = new[]
                {
                    new MapGenBakedRegion
                    {
                        RegionId = 1,
                        RoomCategory = MapGenRoomCategory.Start,
                        CellCount = 1,
                        SourceTemplateId = "start_template"
                    },
                    new MapGenBakedRegion
                    {
                        RegionId = 2,
                        RoomCategory = MapGenRoomCategory.Quest,
                        CellCount = 1,
                        SourceTemplateId = "quest_template"
                    }
                };
                baked.ObjectiveMarkers = new[]
                {
                    new MapGenBakedMarker
                    {
                        MarkerId = "objective_0",
                        Coord = new MapGenGridCoord(1, 0),
                        RegionId = 2,
                        Channel = "objective"
                    }
                };

                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(new[] { baked });

                var compiled = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(null);

                Assert.That(compiled.MapId, Is.EqualTo("mapgenv2_scene_flow_signature"));
                Assert.That(compiled.ProfileId, Is.EqualTo(MapGenerationCatalog.ChapterTwoFirstSliceProfileId));
                Assert.That(compiled.Placements, Has.Exactly(1).Matches<MapPlacement>(
                    placement => placement.Kind == MapPlacementKind.Start && placement.RoomId == "region_1"));
                Assert.That(compiled.Placements, Has.Exactly(1).Matches<MapPlacement>(
                    placement => placement.Kind == MapPlacementKind.QuestTarget && placement.Id == "objective_0"));
            }
            finally
            {
                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(System.Array.Empty<MapGenBakedMapAsset>());
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void SceneBootstrapExposesMapGenV2BakedMapsForSourceControlledScenes()
        {
            var host = new GameObject("scene_bootstrap_mapgenv2_contract_test");
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();

            try
            {
                var bootstrap = host.AddComponent<SceneBootstrap>();
                bootstrap.MapGenV2BakedMaps = new[] { baked };

                Assert.That(bootstrap.MapGenV2BakedMaps, Has.Length.EqualTo(1));
                Assert.That(bootstrap.MapGenV2BakedMaps[0], Is.SameAs(baked));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void MapGenV2RuntimeBuildCompatibilityValidationPasses()
        {
            var report = MapGenV2BuildValidation.ValidateRuntimeBuildCompatibility();

            Assert.That(report.IsValid, Is.True, MapGenV2BuildValidation.Format(report));
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
            roomShape.ShapeId = "workflow_shape";
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

        private static void PopulateCompleteMaterializationCoverage(
            MapGenModuleSetAsset moduleSet,
            GameObject floor,
            GameObject wall)
        {
            if (moduleSet.FloorsA == null || moduleSet.FloorsA.Length == 0)
            {
                moduleSet.FloorsA = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = floor,
                        Weight = 1,
                        Footprint = Vector2Int.one
                    }
                };
            }

            if (moduleSet.WallsStraight == null || moduleSet.WallsStraight.Length == 0)
            {
                moduleSet.WallsStraight = new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = wall,
                        Weight = 1,
                        Footprint = Vector2Int.one,
                        RotationPolicy = MapGenModuleRotationPolicy.AnyOrthogonal
                    }
                };
            }

            moduleSet.WallsCornerInside = moduleSet.WallsStraight;
            moduleSet.WallsCornerOutside = moduleSet.WallsStraight;
            moduleSet.ExteriorCeilings = moduleSet.FloorsA;
            moduleSet.InteriorCeilings = moduleSet.FloorsA;
            moduleSet.WholeDoors = moduleSet.WallsStraight;
            moduleSet.HalfDoorFrames = moduleSet.WallsStraight;
            moduleSet.HalfDoorPanels = moduleSet.WallsStraight;
            moduleSet.PropCategories = moduleSet.FloorsA;
            moduleSet.Blockers = moduleSet.WallsStraight;
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

        private static void ConfigureOneByTwoBlockingTemplate(
            MapGenRoomTemplateAsset template,
            MapGenRoomCategory category)
        {
            template.RoomCategory = category;
            template.Footprint = new Vector2Int(2, 1);
            template.FloorCells = new[] { Vector2Int.zero };
            template.BlockedCells = new[] { new Vector2Int(1, 0) };
            template.Connectors = new MapGenConnector[0];
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

                signature += $"{i}:{cell.State}:{cell.RegionId}:{cell.RoomCategory}:{cell.SocketKind}:{cell.SocketId}:{cell.SocketWidth}:{cell.PropChannel}:{cell.PropWeight}:{cell.SourceTemplateId}:{cell.SourceShapeId}|";
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
                var folderName = folder.Substring("Assets/Conn/Tests/".Length);
                AssetDatabase.CreateFolder("Assets/Conn/Tests", folderName);
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

        private static string BuildIssueSummary(MapGenValidationReport report)
        {
            var summary = string.Empty;
            foreach (var issue in report?.Issues ?? System.Array.Empty<MapGenIssue>())
            {
                summary += $"{issue.Code}: {issue.Message} Fix: {issue.SuggestedFix}\n";
            }

            return summary;
        }
    }
}

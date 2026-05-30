using Conn.Authoring.Maps;
using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.Editor.Maps;
using Conn.Core.Quests;
using Conn.Runtime.Maps;
using Conn.Runtime.Session;
using Conn.Runtime.World;
using NUnit.Framework;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Conn.Tests.EditMode
{
    public sealed class MapGenerationTests
    {
        [Test]
        public void SameProfileAndSeedGenerateSameRoomGraph()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();

            var first = MapGenerationService.Generate(profile, chunks, 2001);
            var second = MapGenerationService.Generate(profile, chunks, 2001);

            Assert.That(second.Graph.Nodes.Count, Is.EqualTo(first.Graph.Nodes.Count));
            Assert.That(second.Graph.Edges.Count, Is.EqualTo(first.Graph.Edges.Count));
            for (var i = 0; i < first.Graph.Nodes.Count; i++)
            {
                Assert.That(second.Graph.Nodes[i].Id, Is.EqualTo(first.Graph.Nodes[i].Id));
                Assert.That(second.Graph.Nodes[i].GridX, Is.EqualTo(first.Graph.Nodes[i].GridX));
                Assert.That(second.Graph.Nodes[i].GridY, Is.EqualTo(first.Graph.Nodes[i].GridY));
                Assert.That(second.Graph.Nodes[i].Role, Is.EqualTo(first.Graph.Nodes[i].Role));
                Assert.That(second.Graph.Nodes[i].SocketMask, Is.EqualTo(first.Graph.Nodes[i].SocketMask));
                Assert.That(second.Graph.Nodes[i].ChunkId, Is.EqualTo(first.Graph.Nodes[i].ChunkId));
            }
        }

        [Test]
        public void FirstSliceGuaranteesRequiredAnchorsAndCompiles()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = MapGenerationService.Generate(profile, chunks, 2001);
            var report = MapValidationService.Validate(profile, draft);
            var compiled = MapGenerationService.Compile(profile, draft);

            Assert.That(report.Passed, Is.True, string.Join("\n", report.Errors.ToArray()));
            Assert.That(compiled.Placements.Exists(p => p.Kind == MapPlacementKind.Start), Is.True);
            Assert.That(compiled.Placements.Exists(p => p.Kind == MapPlacementKind.QuestTarget), Is.True);
            Assert.That(compiled.Placements.Exists(p => p.Kind == MapPlacementKind.Boss), Is.True);
            Assert.That(compiled.Placements.Exists(p => p.Kind == MapPlacementKind.Exit), Is.True);
            Assert.That(compiled.Placements.Exists(p => p.Kind == MapPlacementKind.Monster), Is.True);
            Assert.That(compiled.Placements.Exists(p => p.Kind == MapPlacementKind.Loot), Is.True);
            Assert.That(compiled.Rooms.Count, Is.EqualTo(draft.Graph.Nodes.Count));
            Assert.That(compiled.Doors.Count, Is.EqualTo(draft.Graph.Edges.Count));
        }

        [Test]
        public void RuntimeLoaderReadsGeneratedCompiledMapPlacements()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = MapGenerationService.Generate(profile, chunks, 2001);
            var compiled = MapGenerationService.Compile(profile, draft);
            var json = JsonUtility.ToJson(compiled);

            var loaded = CompiledMapRuntimeLoader.LoadAndValidateFromJson(json, profile);
            var questPlacement = CompiledMapRuntimeLoader.FindPlacement(loaded, MapPlacementKind.QuestTarget);
            var questReport = MapValidationService.ValidateQuestMapContract(QuestCatalog.Find(QuestCatalog.TestHuntId), profile, loaded);

            Assert.That(loaded.MapId, Is.EqualTo(compiled.MapId));
            Assert.That(loaded.Placements.Count, Is.EqualTo(compiled.Placements.Count));
            Assert.That(questPlacement.RoomId, Is.Not.Empty);
            Assert.That(questReport.Passed, Is.True, string.Join("\n", questReport.Errors.ToArray()));
        }

        [Test]
        public void ChunkPresetCellGridSurvivesUnityJsonSerialization()
        {
            var preset = new ChunkPreset
            {
                Id = "cell_grid_probe",
                PresetId = "cell_grid_probe",
                Width = 2,
                Height = 2,
                LayoutKind = RoomChunkLayoutKind.DeadEnd,
                CorridorLength = 4,
                CorridorWidth = 1,
                DeadEndDepth = 2,
                OpenSides = MapDirection.North,
                DoorSockets = MapDirection.North,
                Cells = new System.Collections.Generic.List<RoomChunkCell>
                {
                    new RoomChunkCell { X = 0, Y = 0, Type = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.North, MaterialId = "stone" },
                    new RoomChunkCell { X = 1, Y = 0, Type = RoomChunkCellType.Slope, Height = 0, Direction = MapDirection.East, MaterialId = "moss" },
                    new RoomChunkCell { X = 1, Y = 1, Type = RoomChunkCellType.Stair, Height = 1, Direction = MapDirection.South, MaterialId = "broken" }
                },
                Objects = new System.Collections.Generic.List<RoomChunkObjectPlacement>
                {
                    new RoomChunkObjectPlacement { Id = "north_chest", Kind = RoomChunkObjectKind.Chest, X = 0, Y = 0, Direction = MapDirection.South, PrefabId = "chest_small" },
                    new RoomChunkObjectPlacement { Id = "east_torch", Kind = RoomChunkObjectKind.Torch, X = 1, Y = 0, Height = 1, BlocksMovement = false, PrefabId = "torch_wall" }
                }
            };

            var json = JsonUtility.ToJson(preset);
            var loaded = JsonUtility.FromJson<ChunkPreset>(json);

            Assert.That(loaded.Cells.Count, Is.EqualTo(3));
            Assert.That(loaded.LayoutKind, Is.EqualTo(RoomChunkLayoutKind.DeadEnd));
            Assert.That(loaded.CorridorLength, Is.EqualTo(4));
            Assert.That(loaded.CorridorWidth, Is.EqualTo(1));
            Assert.That(loaded.DeadEndDepth, Is.EqualTo(2));
            Assert.That(loaded.OpenSides, Is.EqualTo(MapDirection.North));
            Assert.That(loaded.Cells[1].Type, Is.EqualTo(RoomChunkCellType.Slope));
            Assert.That(loaded.Cells[1].Direction, Is.EqualTo(MapDirection.East));
            Assert.That(loaded.Cells[1].MaterialId, Is.EqualTo("moss"));
            Assert.That(loaded.Cells[2].Height, Is.EqualTo(1));
            Assert.That(loaded.Objects.Count, Is.EqualTo(2));
            Assert.That(loaded.Objects[0].Kind, Is.EqualTo(RoomChunkObjectKind.Chest));
            Assert.That(loaded.Objects[0].PrefabId, Is.EqualTo("chest_small"));
            Assert.That(loaded.Objects[1].Height, Is.EqualTo(1));
        }

        [Test]
        public void EditableMapDraftAssetSurvivesUnityJsonSerialization()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.Id = "draft_probe";
            draft.SourceProfileId = "profile_probe";
            draft.Seed = 42;
            draft.Floor = 2;
            draft.Difficulty = 3;
            draft.InitializeBlank(3, 2, 0.5f, 0.25f);
            draft.TrySetCell(new EditableMapCell
            {
                X = 1,
                Y = 0,
                RoomId = "room_a",
                ZoneId = "zone_a",
                Terrain = RoomChunkCellType.Stair,
                Height = 2,
                Direction = MapDirection.East,
                MaterialId = "stone"
            });
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "chest_01",
                    PaletteObjectId = "chest_small",
                    Kind = RoomChunkObjectKind.Chest,
                    X = 1,
                    Y = 1,
                    Width = 1,
                    Depth = 1,
                    Direction = MapDirection.South
                }
            };
            draft.Rooms = new[]
            {
                new EditableMapRoom
                {
                    Id = "room_a",
                    Role = MapRoomRole.Start,
                    LayoutKind = RoomChunkLayoutKind.Room,
                    X = 0,
                    Y = 0,
                    Width = 3,
                    Height = 2,
                    SocketMask = MapDirection.East
                }
            };
            draft.Zones = new[]
            {
                new EditableMapZone
                {
                    Id = "zone_a",
                    ThemeId = "ruins",
                    IntendedDifficulty = 2,
                    Purpose = "start"
                }
            };
            draft.Sockets = new[]
            {
                new EditableMapSocket
                {
                    Id = "socket_a",
                    RoomId = "room_a",
                    X = 2,
                    Y = 1,
                    Direction = MapDirection.East,
                    Width = 1,
                    TargetRoomId = "room_b"
                }
            };

            var json = JsonUtility.ToJson(draft);
            var loaded = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            JsonUtility.FromJsonOverwrite(json, loaded);

            Assert.That(loaded.Id, Is.EqualTo("draft_probe"));
            Assert.That(loaded.SourceProfileId, Is.EqualTo("profile_probe"));
            Assert.That(loaded.Width, Is.EqualTo(3));
            Assert.That(loaded.Height, Is.EqualTo(2));
            Assert.That(loaded.Cells.Length, Is.EqualTo(6));
            Assert.That(loaded.GetCell(1, 0).Terrain, Is.EqualTo(RoomChunkCellType.Stair));
            Assert.That(loaded.GetCell(1, 0).Direction, Is.EqualTo(MapDirection.East));
            Assert.That(loaded.Objects.Length, Is.EqualTo(1));
            Assert.That(loaded.Rooms[0].Id, Is.EqualTo("room_a"));
            Assert.That(loaded.Zones[0].ThemeId, Is.EqualTo("ruins"));
            Assert.That(loaded.Sockets[0].TargetRoomId, Is.EqualTo("room_b"));
        }

        [Test]
        public void EditableMapDraftAssetUsesVisualFirstCustomEditor()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(4, 3, 1f, 1f);

            var editor = UnityEditor.Editor.CreateEditor(draft);

            try
            {
                Assert.That(editor, Is.Not.Null);
                Assert.That(editor.GetType().FullName, Is.EqualTo("Conn.Editor.Maps.EditableMapDraftEditor"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void EditableMapPreviewRectMapsMouseToDraftCell()
        {
            var rect = new Rect(10f, 20f, 80f, 40f);

            Assert.That(EditableMapDraftEditor.TryGetCellFromPreviewRect(rect, 8, 4, new Vector2(15f, 25f), out var topLeft), Is.True);
            Assert.That(topLeft, Is.EqualTo(new Vector2Int(0, 3)));

            Assert.That(EditableMapDraftEditor.TryGetCellFromPreviewRect(rect, 8, 4, new Vector2(85f, 55f), out var bottomRight), Is.True);
            Assert.That(bottomRight, Is.EqualTo(new Vector2Int(7, 0)));

            Assert.That(EditableMapDraftEditor.TryGetCellFromPreviewRect(rect, 8, 4, new Vector2(5f, 25f), out _), Is.False);
        }

        [Test]
        public void EditableMapDraftAssetCanBeSavedClosedAndReopenedWithoutDataLoss()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.Id = "asset_round_trip_probe";
            draft.SourceProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId;
            draft.Seed = 77;
            draft.Floor = 2;
            draft.Difficulty = 1;
            draft.InitializeBlank(3, 2, 0.5f, 0.25f);
            draft.TrySetCell(new EditableMapCell
            {
                X = 2,
                Y = 1,
                RoomId = "room_probe",
                ZoneId = "zone_probe",
                Terrain = RoomChunkCellType.Stair,
                Height = 1,
                Direction = MapDirection.East,
                MaterialId = "probe_floor"
            });
            draft.Rooms = new[]
            {
                new EditableMapRoom
                {
                    Id = "room_probe",
                    Role = MapRoomRole.Start,
                    LayoutKind = RoomChunkLayoutKind.Room,
                    X = 0,
                    Y = 0,
                    Width = 3,
                    Height = 2,
                    SocketMask = MapDirection.East,
                    ZoneId = "zone_probe"
                }
            };
            draft.Zones = new[]
            {
                new EditableMapZone
                {
                    Id = "zone_probe",
                    ThemeId = "ruins",
                    IntendedDifficulty = 1,
                    Purpose = "round_trip"
                }
            };
            draft.Sockets = new[]
            {
                new EditableMapSocket
                {
                    Id = "room_probe_exit",
                    RoomId = "room_probe",
                    X = 2,
                    Y = 1,
                    Direction = MapDirection.East,
                    Width = 1,
                    TargetRoomId = "room_neighbor"
                }
            };
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "probe_torch",
                    PaletteObjectId = "torch_probe",
                    Kind = RoomChunkObjectKind.Torch,
                    X = 1,
                    Y = 0,
                    Width = 1,
                    Depth = 1,
                    RuntimeReferenceId = "torch_probe_runtime"
                }
            };

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{EditableMapDraftAsset.DefaultDraftFolder}/EditableMapDraft_RoundTripTest.asset");

            try
            {
                AssetDatabase.CreateAsset(draft, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var loaded = AssetDatabase.LoadAssetAtPath<EditableMapDraftAsset>(assetPath);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.Id, Is.EqualTo(draft.Id));
                Assert.That(loaded.SourceProfileId, Is.EqualTo(draft.SourceProfileId));
                Assert.That(loaded.GetCell(2, 1).Terrain, Is.EqualTo(RoomChunkCellType.Stair));
                Assert.That(loaded.GetCell(2, 1).MaterialId, Is.EqualTo("probe_floor"));
                Assert.That(loaded.Rooms.Length, Is.EqualTo(1));
                Assert.That(loaded.Zones.Length, Is.EqualTo(1));
                Assert.That(loaded.Sockets.Length, Is.EqualTo(1));
                Assert.That(loaded.Objects.Length, Is.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void EditableDraftBuilderCanSaveCopiedDraftAssetWithoutGeneratedGraph()
        {
            var source = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            source.Id = "copied_draft_probe";
            source.SourceProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId;
            source.Seed = 901;
            source.InitializeBlank(2, 2, 1f, 1f);
            source.TrySetCell(new EditableMapCell
            {
                X = 1,
                Y = 1,
                RoomId = "copied_room",
                Terrain = RoomChunkCellType.Floor,
                Height = 0,
                Direction = MapDirection.North,
                MaterialId = "copied_floor"
            });
            source.Rooms = new[]
            {
                new EditableMapRoom
                {
                    Id = "copied_room",
                    Role = MapRoomRole.Start,
                    LayoutKind = RoomChunkLayoutKind.Room,
                    X = 0,
                    Y = 0,
                    Width = 2,
                    Height = 2
                }
            };

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{EditableMapDraftAsset.DefaultDraftFolder}/EditableMapDraft_CopyProbe.asset");

            try
            {
                var copied = EditableMapDraftBuilder.CreateDraftAssetFromSource(assetPath, source);

                Assert.That(copied, Is.Not.Null);
                Assert.That(copied.Id, Is.EqualTo(source.Id));
                Assert.That(copied.SourceProfileId, Is.EqualTo(source.SourceProfileId));
                Assert.That(copied.GetCell(1, 1).MaterialId, Is.EqualTo("copied_floor"));
                Assert.That(copied.Rooms.Length, Is.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void SampleEditableMapDraftAssetLoadsValidatesAndBakes()
        {
            var draft = AssetDatabase.LoadAssetAtPath<EditableMapDraftAsset>(EditableMapDraftSampleAssetBuilder.SampleDraftAssetPath);

            Assert.That(draft, Is.Not.Null);

            var report = EditableMapValidationService.Validate(draft);
            Assert.That(report.Passed, Is.True, string.Join("\n", report.Errors.ToArray()));

            var compiled = EditableMapBakeService.Bake(draft);
            Assert.That(compiled.Cells.Count, Is.GreaterThan(0));
            Assert.That(compiled.Objects.Count, Is.GreaterThan(0));
            Assert.That(compiled.Placements.Exists(placement => placement.Kind == MapPlacementKind.Start), Is.True);
            Assert.That(compiled.EncounterPlacements.Count, Is.GreaterThan(0));
        }

        [Test]
        public void EditableDraftBuilderConvertsGeneratedDraftIntoCellMap()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var generated = MapGenerationService.Generate(profile, chunks, 2001);
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();

            EditableMapDraftBuilder.PopulateFromGeneratedDraft(draft, generated, profile, chunks, 1, 0, 0.5f, 0.25f);

            Assert.That(draft.Width, Is.GreaterThan(0));
            Assert.That(draft.Height, Is.GreaterThan(0));
            Assert.That(draft.Rooms.Length, Is.EqualTo(generated.Graph.Nodes.Count));
            Assert.That(draft.Sockets.Length, Is.EqualTo(generated.Graph.Edges.Count * 2));
            Assert.That(draft.Objects.Length, Is.GreaterThanOrEqualTo(0));
            Assert.That(draft.Cells.Length, Is.EqualTo(draft.Width * draft.Height));
        }

        [Test]
        public void EditableDraftBuilderIsDeterministicForSameProfileAndSeed()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();

            var first = EditableMapDraftBuilder.BuildGeneratedDraft(profile, chunks, 2112, 1, 0, 0.5f, 0.25f);
            var second = EditableMapDraftBuilder.BuildGeneratedDraft(profile, chunks, 2112, 1, 0, 0.5f, 0.25f);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void GeneratedDraftIncludesIntentionalLayoutKinds()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = EditableMapDraftBuilder.BuildGeneratedDraft(profile, chunks, 2001, 1, 0, 0.5f, 0.25f);

            Assert.That(draft.Rooms.Any(room => room.LayoutKind == RoomChunkLayoutKind.Hub), Is.True);
            Assert.That(draft.Rooms.Any(room => room.LayoutKind == RoomChunkLayoutKind.Corridor), Is.True);
            Assert.That(draft.Rooms.Any(room => room.LayoutKind == RoomChunkLayoutKind.DeadEnd), Is.True);
            Assert.That(draft.Rooms.Any(room => room.LayoutKind == RoomChunkLayoutKind.HeightTransition), Is.True);
            Assert.That(draft.Zones.Length, Is.GreaterThanOrEqualTo(1));

            Object.DestroyImmediate(draft);
        }

        [Test]
        public void GeneratedEditableDraftUsesUniqueObjectIdsAndWalkableSockets()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = EditableMapDraftBuilder.BuildGeneratedDraft(profile, chunks, 2001, 1, 0, 0.5f, 0.25f);

            try
            {
                var validation = EditableMapValidationService.Validate(draft);
                var duplicateObjectErrors = validation.Errors.Where(error => error.Contains("Duplicate object id")).ToArray();
                var socketWalkabilityErrors = validation.Errors.Where(error => error.Contains("does not touch a walkable cell")).ToArray();

                Assert.That(duplicateObjectErrors, Is.Empty, string.Join("\n", validation.Errors));
                Assert.That(socketWalkabilityErrors, Is.Empty, string.Join("\n", validation.Errors));
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void GeneratedEditableDraftValidatesAndBuildsCompiledResult()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();

            var result = EditableMapGeneratedResultBuilder.Build(profile, chunks, 2001, 1, 0, 0.5f, 0.25f);

            try
            {
                Assert.That(result.Report.Passed, Is.True, string.Join("\n", result.Report.Errors));
                Assert.That(result.Compiled, Is.Not.Null);
                Assert.That(result.Compiled.Doors.Count, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(result.Draft);
            }
        }

        [Test]
        public void GeneratedEditableDraftUsesTransientPalettesUntilSavedAsAsset()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var draft = EditableCellMapGenerator.Generate(profile, 2001, 1, 0, 0.5f, 0.25f);

            try
            {
                Assert.That(draft.TilePalette, Is.Not.Null);
                Assert.That(draft.ObjectPalette, Is.Not.Null);
                Assert.That(EditorUtility.IsPersistent(draft.TilePalette), Is.False);
                Assert.That(EditorUtility.IsPersistent(draft.ObjectPalette), Is.False);

                var report = EditableMapValidationService.Validate(draft);

                Assert.That(report.Passed, Is.True, string.Join("\n", report.Errors));
            }
            finally
            {
                Object.DestroyImmediate(draft.TilePalette);
                Object.DestroyImmediate(draft.ObjectPalette);
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void GeneratedEditableResultUsesCellFirstDungeonLayout()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();

            var result = EditableMapGeneratedResultBuilder.Build(profile, chunks, 2001, 1, 0, 0.5f, 0.25f);

            try
            {
                var floorCount = result.Draft.Cells.Count(cell => cell.Terrain == RoomChunkCellType.Floor);
                var wallCount = result.Draft.Cells.Count(cell => cell.Terrain == RoomChunkCellType.Wall);
                var gapCount = result.Draft.Cells.Count(cell => cell.Terrain == RoomChunkCellType.Gap);
                var slopeCount = result.Draft.Cells.Count(cell => cell.Terrain == RoomChunkCellType.Slope);
                var stairCount = result.Draft.Cells.Count(cell => cell.Terrain == RoomChunkCellType.Stair);
                var doorCount = result.Draft.Cells.Count(cell => cell.MaterialId == "generated_door");
                var wallCornerCount = result.Draft.Cells.Count(cell => cell.WallVariantId == "wall_corner");
                var wallEdgeCount = result.Draft.Cells.Count(cell => cell.WallVariantId == "wall_edge");
                var tileIds = result.Draft.TilePalette.Tiles.Select(tile => tile.Id).ToHashSet();
                var objectIds = result.Draft.ObjectPalette.Objects.Select(entry => entry.Id).ToHashSet();

                Assert.That(result.Report.Passed, Is.True, string.Join("\n", result.Report.Errors));
                Assert.That(result.Compiled, Is.Not.Null);
                Assert.That(result.Draft.TilePalette, Is.Not.Null);
                Assert.That(result.Draft.ObjectPalette, Is.Not.Null);
                Assert.That(floorCount, Is.GreaterThan(80));
                Assert.That(wallCount, Is.GreaterThan(40));
                Assert.That(gapCount, Is.GreaterThan(40));
                Assert.That(slopeCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(stairCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(doorCount, Is.GreaterThanOrEqualTo(4));
                Assert.That(wallCornerCount, Is.GreaterThanOrEqualTo(4));
                Assert.That(wallEdgeCount, Is.GreaterThanOrEqualTo(4));
                Assert.That(result.Draft.Objects.Any(placement => placement.Kind == RoomChunkObjectKind.Chest), Is.True);
                Assert.That(result.Draft.Objects.Any(placement => placement.Kind == RoomChunkObjectKind.SpawnHint), Is.True);
                Assert.That(result.Draft.Objects.Any(placement => placement.Kind == RoomChunkObjectKind.Torch), Is.True);
                Assert.That(result.Draft.Objects.Any(placement => placement.Kind == RoomChunkObjectKind.Barrel), Is.True);
                Assert.That(result.Draft.Objects.Count(placement => placement.RuntimeReferenceId == "door"), Is.GreaterThanOrEqualTo(4));
                Assert.That(result.Draft.Cells.Where(cell => !string.IsNullOrWhiteSpace(cell.MaterialId)).All(cell => tileIds.Contains(cell.MaterialId)), Is.True);
                Assert.That(result.Draft.Objects.Where(placement => !string.IsNullOrWhiteSpace(placement.PaletteObjectId)).All(placement => objectIds.Contains(placement.PaletteObjectId)), Is.True);
                Assert.That(result.Draft.Rooms.Any(room => room.Id == "hub" && room.LayoutKind == RoomChunkLayoutKind.Hub), Is.True);
                Assert.That(result.Draft.Rooms.Any(room => room.Id == "boss" && room.LayoutKind == RoomChunkLayoutKind.HeightTransition), Is.True);
                Assert.That(result.Draft.Rooms.Count(room => room.LayoutKind == RoomChunkLayoutKind.DeadEnd), Is.GreaterThanOrEqualTo(1));
                Assert.That(result.Draft.Sockets.Any(socket => socket.RoomId == "hub" && socket.TargetRoomId == "treasure_branch"), Is.True);
                Assert.That(result.Draft.Sockets.Any(socket => socket.RoomId == "treasure_branch" && socket.TargetRoomId == "quest"), Is.True);
                Assert.That(result.Compiled.Doors.Any(edge => edge.FromNodeId == "treasure_branch" && edge.ToNodeId == "quest"), Is.True);
                Assert.That(result.Compiled.Cells.Count, Is.EqualTo(result.Draft.Cells.Length));
                Assert.That(result.Compiled.Cells.Count(cell => cell.WallVariantId == "wall_corner"), Is.EqualTo(wallCornerCount));
                Assert.That(result.Compiled.Objects.Count(placement => placement.RuntimeReferenceId == "door"), Is.GreaterThanOrEqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(result.Draft);
            }
        }

        [Test]
        public void GeneratedEditableDraftBuildsNamedSceneMapPreview()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var draft = EditableCellMapGenerator.Generate(profile, 2001, 1, 0, 0.5f, 0.25f);

            try
            {
                var root = EditableMapPreviewMeshBuilder.RebuildPreview(draft);

                Assert.That(draft.name, Is.EqualTo(draft.Id));
                Assert.That(root.name, Does.Contain(draft.Id));
                Assert.That(root.transform.Find("Terrain Mesh"), Is.Not.Null);
                Assert.That(root.transform.Find("Wall Mesh"), Is.Not.Null);
                Assert.That(root.transform.Find("Slope Mesh"), Is.Not.Null);
                Assert.That(root.transform.Find("Stair Mesh"), Is.Not.Null);
                var objectRoot = root.transform.Find("Object Preview Root");
                var door = objectRoot.Cast<Transform>().FirstOrDefault(child => child.name.Contains("generated_door"));
                Assert.That(door, Is.Not.Null);
                Assert.That(Mathf.Min(door.localScale.x, door.localScale.z), Is.LessThan(draft.CellSize * 0.3f));

                Object.DestroyImmediate(root);
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void EditablePreviewBuilderRebuildsWithoutWorkspaceDependency()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.name = "PreviewProbe";
            draft.InitializeBlank(2, 2, 1f, 0.5f);
            draft.TrySetCell(new EditableMapCell { X = 0, Y = 0, Terrain = RoomChunkCellType.Floor, Direction = MapDirection.North, MaterialId = "floor" });
            draft.TrySetCell(new EditableMapCell { X = 1, Y = 0, Terrain = RoomChunkCellType.Wall, Height = 0, Direction = MapDirection.North, MaterialId = "wall" });
            draft.TrySetCell(new EditableMapCell { X = 0, Y = 1, Terrain = RoomChunkCellType.Slope, Height = 0, Direction = MapDirection.East, MaterialId = "slope" });
            draft.TrySetCell(new EditableMapCell { X = 1, Y = 1, Terrain = RoomChunkCellType.Stair, Height = 0, Direction = MapDirection.South, MaterialId = "stair" });
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "torch_01",
                    Kind = RoomChunkObjectKind.Torch,
                    X = 0,
                    Y = 0,
                    Width = 1,
                    Depth = 1,
                    Direction = MapDirection.North
                }
            };

            var root = EditableMapPreviewMeshBuilder.RebuildPreview(draft);

            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.Find("Terrain Mesh"), Is.Not.Null);
            Assert.That(root.transform.Find("Wall Mesh"), Is.Not.Null);
            Assert.That(root.transform.Find("Slope Mesh"), Is.Not.Null);
            Assert.That(root.transform.Find("Stair Mesh"), Is.Not.Null);
            Assert.That(root.transform.Find("Object Preview Root"), Is.Not.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DrawnEditableDraftCanBuildPlayableMetadataValidateAndBake()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.Id = "drawn_probe";
            draft.SourceProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId;
            draft.InitializeBlank(12, 5, 1f, 1f);
            for (var x = 1; x <= 10; x++)
            {
                draft.TrySetCell(new EditableMapCell
                {
                    X = x,
                    Y = 2,
                    Terrain = RoomChunkCellType.Floor,
                    Height = 0,
                    Direction = MapDirection.East,
                    MaterialId = "drawn_floor"
                });
            }

            EditableMapDraftMetadataBuilder.BuildPlayableMetadataFromDrawing(draft);

            var report = EditableMapValidationService.Validate(draft);
            var compiled = EditableMapBakeService.Bake(draft);

            Assert.That(report.Passed, Is.True, string.Join("\n", report.Errors));
            Assert.That(draft.Rooms.Any(room => room.Role == MapRoomRole.Start), Is.True);
            Assert.That(draft.Rooms.Any(room => room.Role == MapRoomRole.QuestTarget), Is.True);
            Assert.That(draft.Rooms.Any(room => room.Role == MapRoomRole.Boss), Is.True);
            Assert.That(draft.Rooms.Any(room => room.Role == MapRoomRole.Exit), Is.True);
            Assert.That(draft.Sockets.Length, Is.GreaterThanOrEqualTo(7));
            Assert.That(draft.Sockets.Any(socket => socket.RoomId == "drawn_area" && socket.TargetRoomId == "drawn_start"), Is.True);
            Assert.That(draft.Sockets.Any(socket => socket.RoomId == "drawn_start" && socket.TargetRoomId == "drawn_area"), Is.True);
            Assert.That(compiled.Placements.Exists(placement => placement.Kind == MapPlacementKind.Start), Is.True);
            Assert.That(compiled.Placements.Exists(placement => placement.Kind == MapPlacementKind.Exit), Is.True);

            Object.DestroyImmediate(draft);
        }

        [Test]
        public void EditableValidationReportsBlockedRequiredRoute()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(8, 1, 1f, 1f);
            for (var x = 0; x < 8; x++)
            {
                draft.TrySetCell(new EditableMapCell
                {
                    X = x,
                    Y = 0,
                    Terrain = RoomChunkCellType.Floor,
                    Height = 0,
                    Direction = MapDirection.North
                });
            }

            draft.Rooms = new[]
            {
                new EditableMapRoom { Id = "start", Role = MapRoomRole.Start, X = 0, Y = 0, Width = 2, Height = 1 },
                new EditableMapRoom { Id = "quest", Role = MapRoomRole.QuestTarget, X = 2, Y = 0, Width = 2, Height = 1 },
                new EditableMapRoom { Id = "boss", Role = MapRoomRole.Boss, X = 4, Y = 0, Width = 2, Height = 1 },
                new EditableMapRoom { Id = "exit", Role = MapRoomRole.Exit, X = 6, Y = 0, Width = 2, Height = 1 }
            };
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "blocker",
                    Kind = RoomChunkObjectKind.Blocker,
                    X = 2,
                    Y = 0,
                    Width = 1,
                    Depth = 1,
                    BlocksMovement = true
                }
            };

            var report = EditableMapValidationService.Validate(draft);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Errors.Exists(error => error.Contains("start-to-quest")), Is.True);
        }

        [Test]
        public void EditableValidationReportsSocketThatCannotReachRequiredAnchor()
        {
            var draft = BuildRequiredAnchorSocketValidationDraft();

            var report = EditableMapValidationService.Validate(draft);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Errors.Exists(error => error.Contains("quest_exit") && error.Contains("required anchor")), Is.True);
        }

        [Test]
        public void EditableValidationReportsInvalidSocketTopology()
        {
            var draft = BuildLinearValidationDraft();
            draft.Sockets = new[]
            {
                new EditableMapSocket { Id = "bad_socket", RoomId = "start", X = 1, Y = 0, Direction = MapDirection.East, TargetRoomId = "missing_room", Width = 1 },
                new EditableMapSocket { Id = "bad_socket", RoomId = "missing_owner", X = 2, Y = 0, Direction = MapDirection.West, TargetRoomId = "start", Width = 1 },
                new EditableMapSocket { Id = "outside_room", RoomId = "quest", X = 0, Y = 0, Direction = MapDirection.West, TargetRoomId = "start", Width = 1 }
            };

            var report = EditableMapValidationService.Validate(draft);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Errors.Exists(error => error.Contains("Duplicate socket id: bad_socket")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("missing target room id missing_room")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("missing room id missing_owner")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("outside_room") && error.Contains("outside its room quest")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("bad_socket") && error.Contains("no reciprocal socket")), Is.True);
        }

        [Test]
        public void EditableValidationReportsInvalidRoomAndZoneReferences()
        {
            var draft = BuildLinearValidationDraft();
            draft.Zones = new[]
            {
                new EditableMapZone { Id = "zone_a", ThemeId = "ruins" },
                new EditableMapZone { Id = "zone_a", ThemeId = "duplicate" }
            };
            draft.Rooms = new[]
            {
                new EditableMapRoom { Id = "start", Role = MapRoomRole.Start, X = 0, Y = 0, Width = 1, Height = 1, ZoneId = "missing_zone" },
                new EditableMapRoom { Id = "start", Role = MapRoomRole.QuestTarget, X = 1, Y = 0, Width = 1, Height = 1, ZoneId = "zone_a" },
                new EditableMapRoom { Id = "boss", Role = MapRoomRole.Boss, X = 2, Y = 0, Width = 1, Height = 1, ZoneId = "zone_a" },
                new EditableMapRoom { Id = "exit", Role = MapRoomRole.Exit, X = 7, Y = 0, Width = 3, Height = 1, ZoneId = "zone_a" }
            };
            draft.TrySetCell(new EditableMapCell
            {
                X = 0,
                Y = 0,
                RoomId = "missing_room",
                ZoneId = "missing_cell_zone",
                Terrain = RoomChunkCellType.Floor,
                Height = 0,
                Direction = MapDirection.North
            });

            var report = EditableMapValidationService.Validate(draft);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Errors.Exists(error => error.Contains("Duplicate room id: start")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("Duplicate zone id: zone_a")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("Room start references missing zone id missing_zone")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("Room exit bounds leave the draft")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("Cell (0, 0) references missing room id missing_room")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("Cell (0, 0) references missing zone id missing_cell_zone")), Is.True);
        }

        [Test]
        public void EditableValidationReportsBlockedOptionalTreasureRoute()
        {
            var draft = BuildOptionalTreasureValidationDraft();

            var report = EditableMapValidationService.Validate(draft);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Errors.Exists(error => error.Contains("optional treasure object treasure_hidden")), Is.True);
        }

        [Test]
        public void EditableValidationReportsInvalidSlopeHeightConnection()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(2, 2, 1f, 1f);
            draft.TrySetCell(new EditableMapCell { X = 0, Y = 0, Terrain = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.North });
            draft.TrySetCell(new EditableMapCell { X = 1, Y = 0, Terrain = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.North });
            draft.TrySetCell(new EditableMapCell { X = 0, Y = 1, Terrain = RoomChunkCellType.Slope, Height = 0, Direction = MapDirection.East });
            draft.TrySetCell(new EditableMapCell { X = 1, Y = 1, Terrain = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.North });
            draft.Rooms = new[]
            {
                new EditableMapRoom { Id = "start", Role = MapRoomRole.Start, X = 0, Y = 0, Width = 1, Height = 1 },
                new EditableMapRoom { Id = "quest", Role = MapRoomRole.QuestTarget, X = 1, Y = 0, Width = 1, Height = 1 },
                new EditableMapRoom { Id = "boss", Role = MapRoomRole.Boss, X = 0, Y = 1, Width = 1, Height = 1 },
                new EditableMapRoom { Id = "exit", Role = MapRoomRole.Exit, X = 1, Y = 1, Width = 1, Height = 1 }
            };

            var report = EditableMapValidationService.Validate(draft);

            Assert.That(report.Errors.Exists(error => error.Contains("requires a +1 height step")), Is.True);
        }

        [Test]
        public void EditableValidationReportsMissingPaletteReferences()
        {
            var draft = BuildLinearValidationDraft();
            var tilePalette = ScriptableObject.CreateInstance<MapTilePaletteAsset>();
            tilePalette.Tiles = new[]
            {
                new MapTilePaletteEntry { Id = "valid_floor", TerrainType = RoomChunkCellType.Floor, RuntimeMaterialId = "runtime_floor" }
            };
            var objectPalette = ScriptableObject.CreateInstance<MapObjectPaletteAsset>();
            objectPalette.Objects = new[]
            {
                new MapObjectPaletteEntry { Id = "valid_torch", Kind = RoomChunkObjectKind.Torch, FootprintWidth = 1, FootprintDepth = 1, RuntimeReferenceId = "torch_runtime" }
            };
            draft.TilePalette = tilePalette;
            draft.ObjectPalette = objectPalette;
            draft.TrySetCell(new EditableMapCell { X = 0, Y = 0, Terrain = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.North, MaterialId = "missing_tile" });
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "torch",
                    Kind = RoomChunkObjectKind.Torch,
                    X = 0,
                    Y = 0,
                    Width = 1,
                    Depth = 1,
                    PaletteObjectId = "missing_object"
                }
            };

            var report = EditableMapValidationService.Validate(draft);

            Assert.That(report.Errors.Exists(error => error.Contains("missing tile palette id missing_tile")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("missing object palette id missing_object")), Is.True);

            Object.DestroyImmediate(tilePalette);
            Object.DestroyImmediate(objectPalette);
        }

        [Test]
        public void MapAuthoringValidationReportsDuplicatePaletteIds()
        {
            var tilePalette = ScriptableObject.CreateInstance<MapTilePaletteAsset>();
            tilePalette.Id = "tiles";
            tilePalette.DisplayName = "Tiles";
            tilePalette.ThemeId = "ruins";
            tilePalette.Tiles = new[]
            {
                new MapTilePaletteEntry { Id = "stone", TerrainType = RoomChunkCellType.Floor, RuntimeMaterialId = "stone_runtime" },
                new MapTilePaletteEntry { Id = "stone", TerrainType = RoomChunkCellType.Wall, RuntimeMaterialId = "stone_wall_runtime" }
            };

            var objectPalette = ScriptableObject.CreateInstance<MapObjectPaletteAsset>();
            objectPalette.Id = "objects";
            objectPalette.DisplayName = "Objects";
            objectPalette.ThemeId = "ruins";
            objectPalette.Objects = new[]
            {
                new MapObjectPaletteEntry { Id = "torch", Kind = RoomChunkObjectKind.Torch, FootprintWidth = 1, FootprintDepth = 1, RuntimeReferenceId = "torch_runtime" },
                new MapObjectPaletteEntry { Id = "torch", Kind = RoomChunkObjectKind.Decor, FootprintWidth = 1, FootprintDepth = 1, RuntimeReferenceId = "torch_runtime_2" }
            };

            var report = MapAuthoringValidationService.Validate(new MapAuthoringSnapshot
            {
                TilePalettes = new[] { tilePalette },
                ObjectPalettes = new[] { objectPalette }
            });

            Assert.That(report.Errors.Exists(error => error.Contains("duplicate tile id stone")), Is.True);
            Assert.That(report.Errors.Exists(error => error.Contains("duplicate object id torch")), Is.True);

            Object.DestroyImmediate(tilePalette);
            Object.DestroyImmediate(objectPalette);
        }

        [Test]
        public void EditableBakeIsDeterministicForUnchangedDraft()
        {
            var draft = BuildBakeDraft();

            var first = EditableMapBakeService.Bake(draft);
            var second = EditableMapBakeService.Bake(draft);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
        }

        [Test]
        public void EditableBakeRoundTripLoadsFromCompiledJson()
        {
            var draft = BuildBakeDraft();

            var compiled = EditableMapBakeService.Bake(draft);
            var json = JsonUtility.ToJson(compiled);
            var loaded = CompiledMapRuntimeLoader.LoadFromJson(json);

            Assert.That(loaded.MapId, Is.EqualTo(compiled.MapId));
            Assert.That(loaded.Cells.Count, Is.EqualTo(compiled.Cells.Count));
            Assert.That(loaded.Objects.Count, Is.EqualTo(compiled.Objects.Count));
            Assert.That(loaded.RoomRecords.Count, Is.EqualTo(compiled.RoomRecords.Count));
            Assert.That(loaded.Sockets.Count, Is.EqualTo(compiled.Sockets.Count));
            Assert.That(CompiledMapRuntimeLoader.FindObjectPlacement(loaded, "spawn_hint")?.MaterialId, Is.EqualTo("spawn_marker"));
            Assert.That(CompiledMapRuntimeLoader.FindObjectPlacement(loaded, "treasure")?.MaterialId, Is.EqualTo("treasure_gold"));
            Assert.That(loaded.Placements.Exists(placement => placement.Kind == MapPlacementKind.Start), Is.True);
            Assert.That(loaded.Placements.Exists(placement => placement.Kind == MapPlacementKind.Monster), Is.True);
            Assert.That(loaded.EncounterPlacements.Count, Is.EqualTo(compiled.EncounterPlacements.Count));
        }

        [Test]
        public void EditableBakeProducesAnchorAndEncounterPlacements()
        {
            var draft = BuildBakeDraft();

            var compiled = EditableMapBakeService.Bake(draft);
            var questEncounter = CompiledMapRuntimeLoader.FindEncounterPlacement(compiled, "questtarget_quest");
            var bossEncounter = CompiledMapRuntimeLoader.FindEncounterPlacement(compiled, "boss_boss");
            var monsterEncounter = CompiledMapRuntimeLoader.FindEncounterPlacement(compiled, "spawn_spawn_hint");

            Assert.That(compiled.Placements.Exists(placement => placement.Kind == MapPlacementKind.Start), Is.True);
            Assert.That(compiled.Placements.Exists(placement => placement.Kind == MapPlacementKind.QuestTarget), Is.True);
            Assert.That(compiled.Placements.Exists(placement => placement.Kind == MapPlacementKind.Boss), Is.True);
            Assert.That(compiled.Placements.Exists(placement => placement.Kind == MapPlacementKind.Exit), Is.True);
            Assert.That(questEncounter, Is.Not.Null);
            Assert.That(questEncounter.RequiredForQuest, Is.True);
            Assert.That(bossEncounter, Is.Not.Null);
            Assert.That(monsterEncounter, Is.Not.Null);
            Assert.That(monsterEncounter.SpawnRole, Is.EqualTo("trash"));
        }

        [Test]
        public void CompiledRuntimeRecordsContainNoUnityOrEditorReferences()
        {
            AssertRuntimeSafeType(typeof(CompiledMap));
            AssertRuntimeSafeType(typeof(MapPlacement));
            AssertRuntimeSafeType(typeof(CompiledEncounterPlacement));
            AssertRuntimeSafeType(typeof(CompiledMapCell));
            AssertRuntimeSafeType(typeof(CompiledMapObjectPlacement));
            AssertRuntimeSafeType(typeof(CompiledMapRoomRecord));
            AssertRuntimeSafeType(typeof(CompiledMapZoneRecord));
            AssertRuntimeSafeType(typeof(CompiledMapSocketRecord));
        }

        [Test]
        public void DungeonRuntimeLoadsBakedCompiledDraftPayload()
        {
            var draft = BuildBakeDraft();
            var compiled = EditableMapBakeService.Bake(draft);
            var asset = ScriptableObject.CreateInstance<CompiledMapAsset>();
            asset.ProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId;
            asset.Seed = compiled.Seed;
            asset.Json = JsonUtility.ToJson(compiled);
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);

            CompiledMapDungeonRuntimeService.SetCompiledMapAssets(new[] { asset });
            var loaded = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);

            Assert.That(loaded.Cells.Count, Is.EqualTo(compiled.Cells.Count));
            Assert.That(loaded.Objects.Count, Is.EqualTo(compiled.Objects.Count));
            Assert.That(CompiledMapDungeonRuntimeService.CurrentCompiledMap, Is.Not.Null);
            Assert.That(CompiledMapDungeonRuntimeService.CountBakedCells(loaded), Is.EqualTo(compiled.Cells.Count));
            Assert.That(CompiledMapDungeonRuntimeService.CountBakedObjects(loaded), Is.EqualTo(compiled.Objects.Count));
            Assert.That(CompiledMapDungeonRuntimeService.CountInteractiveObjects(loaded), Is.EqualTo(1));

            CompiledMapDungeonRuntimeService.SetCompiledMapAssets(null);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void DungeonObjectSpawnerCreatesInteractableActorsFromBakedObjects()
        {
            var draft = BuildBakeDraft();
            var compiled = EditableMapBakeService.Bake(draft);
            var root = new GameObject("Dungeon Object Root").transform;
            var sessionGameObject = new GameObject("GameSession Test");
            var gameSession = sessionGameObject.AddComponent<GameSession>();
            gameSession.State.StartNewGame();
            var startingGold = gameSession.State.Gold;

            var spawned = DungeonObjectActorSpawner.SpawnFromCompiledMap(compiled, root);
            var interactable = root.GetComponentInChildren<DungeonObjectInteractable>();

            Assert.That(spawned, Is.EqualTo(compiled.Objects.Count));
            Assert.That(interactable, Is.Not.Null);
            Assert.That(interactable.Prompt, Is.EqualTo("Open Chest"));

            interactable.Interact();
            Assert.That(gameSession.State.Gold, Is.GreaterThan(startingGold));
            Assert.That(gameSession.State.LastNotice.Contains("Opened chest"), Is.True);

            Object.DestroyImmediate(root.gameObject);
            Object.DestroyImmediate(sessionGameObject);
        }

        [Test]
        public void SceneToolsConvertWorldPositionToDraftCell()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(4, 4, 0.5f, 1f);

            var found = EditableMapDraftSceneTools.TryGetCellFromWorld(draft, new Vector3(1.1f, 0f, 0.6f), out var cell);

            Assert.That(found, Is.True);
            Assert.That(cell, Is.EqualTo(new Vector2Int(2, 1)));
        }

        [Test]
        public void SceneToolsBuildValidationMarkersForCellsObjectsAndSockets()
        {
            var draft = BuildLinearValidationDraft();
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "barrel_a",
                    Kind = RoomChunkObjectKind.Barrel,
                    X = 1,
                    Y = 0,
                    Width = 1,
                    Depth = 1
                }
            };
            draft.Sockets = new[]
            {
                new EditableMapSocket
                {
                    Id = "socket_a",
                    RoomId = "start",
                    X = 0,
                    Y = 0,
                    Direction = MapDirection.East,
                    TargetRoomId = "quest",
                    Width = 1
                }
            };

            var report = new MapValidationReport();
            report.Errors.Add("Cell (2, 0) is invalid.");
            report.Errors.Add("Object barrel_a overlaps non-walkable cell.");
            report.Errors.Add("Socket socket_a does not touch a walkable cell.");
            var markers = EditableMapDraftSceneTools.BuildValidationMarkers(draft, report).ToArray();

            Assert.That(markers.Length, Is.EqualTo(3));
            Assert.That(markers.Any(marker => marker.Kind == ValidationMarkerKind.Cell && marker.Position == new Vector2Int(2, 0)), Is.True);
            Assert.That(markers.Any(marker => marker.Kind == ValidationMarkerKind.Object && marker.Position == new Vector2Int(1, 0)), Is.True);
            Assert.That(markers.Any(marker => marker.Kind == ValidationMarkerKind.Socket && marker.Position == new Vector2Int(0, 0)), Is.True);
        }

        [Test]
        public void UndoRedoRestoresEditableDraftCellAndObjectChanges()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(2, 2, 1f, 1f);
            var originalCell = draft.GetCell(0, 0);

            Undo.RecordObject(draft, "Edit Draft For Undo");
            draft.TrySetCell(new EditableMapCell
            {
                X = 0,
                Y = 0,
                Terrain = RoomChunkCellType.Wall,
                Height = 2,
                Direction = MapDirection.South,
                MaterialId = "stone_wall"
            });
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "torch_undo",
                    Kind = RoomChunkObjectKind.Torch,
                    X = 1,
                    Y = 1,
                    Width = 1,
                    Depth = 1
                }
            };
            EditorUtility.SetDirty(draft);

            Undo.PerformUndo();

            Assert.That(draft.GetCell(0, 0).Terrain, Is.EqualTo(originalCell.Terrain));
            Assert.That(draft.Objects.Length, Is.EqualTo(0));

            Undo.PerformRedo();

            Assert.That(draft.GetCell(0, 0).Terrain, Is.EqualTo(RoomChunkCellType.Wall));
            Assert.That(draft.GetCell(0, 0).Height, Is.EqualTo(2));
            Assert.That(draft.Objects.Length, Is.EqualTo(1));

            Object.DestroyImmediate(draft);
        }

        private static EditableMapDraftAsset BuildLinearValidationDraft()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(4, 1, 1f, 1f);
            for (var x = 0; x < 4; x++)
            {
                draft.TrySetCell(new EditableMapCell
                {
                    X = x,
                    Y = 0,
                    Terrain = RoomChunkCellType.Floor,
                    Height = 0,
                    Direction = MapDirection.North
                });
            }

            draft.Rooms = new[]
            {
                new EditableMapRoom { Id = "start", Role = MapRoomRole.Start, X = 0, Y = 0, Width = 1, Height = 1 },
                new EditableMapRoom { Id = "quest", Role = MapRoomRole.QuestTarget, X = 1, Y = 0, Width = 1, Height = 1 },
                new EditableMapRoom { Id = "boss", Role = MapRoomRole.Boss, X = 2, Y = 0, Width = 1, Height = 1 },
                new EditableMapRoom { Id = "exit", Role = MapRoomRole.Exit, X = 3, Y = 0, Width = 1, Height = 1 }
            };
            return draft;
        }

        private static EditableMapDraftAsset BuildRequiredAnchorSocketValidationDraft()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(12, 3, 1f, 1f);
            for (var x = 0; x < 12; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var roomId = x < 3 ? "start" : x < 6 ? "quest" : x < 9 ? "boss" : "exit";
                    draft.TrySetCell(new EditableMapCell
                    {
                        X = x,
                        Y = y,
                        RoomId = roomId,
                        Terrain = RoomChunkCellType.Floor,
                        Height = 0,
                        Direction = MapDirection.North
                    });
                }
            }

            for (var y = 0; y < 3; y++)
            {
                draft.TrySetCell(new EditableMapCell
                {
                    X = 4,
                    Y = y,
                    RoomId = "quest",
                    Terrain = RoomChunkCellType.Wall,
                    Height = 0,
                    Direction = MapDirection.North
                });
            }

            draft.Rooms = new[]
            {
                new EditableMapRoom { Id = "start", Role = MapRoomRole.Start, X = 0, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.East },
                new EditableMapRoom { Id = "quest", Role = MapRoomRole.QuestTarget, X = 3, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.East | MapDirection.West },
                new EditableMapRoom { Id = "boss", Role = MapRoomRole.Boss, X = 6, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.East | MapDirection.West },
                new EditableMapRoom { Id = "exit", Role = MapRoomRole.Exit, X = 9, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.West }
            };
            draft.Sockets = new[]
            {
                new EditableMapSocket { Id = "start_exit", RoomId = "start", X = 2, Y = 1, Direction = MapDirection.East, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "quest_entry", RoomId = "quest", X = 3, Y = 1, Direction = MapDirection.West, TargetRoomId = "start", Width = 1 },
                new EditableMapSocket { Id = "quest_exit", RoomId = "quest", X = 5, Y = 1, Direction = MapDirection.East, TargetRoomId = "boss", Width = 1 },
                new EditableMapSocket { Id = "boss_entry", RoomId = "boss", X = 6, Y = 1, Direction = MapDirection.West, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "boss_exit", RoomId = "boss", X = 8, Y = 1, Direction = MapDirection.East, TargetRoomId = "exit", Width = 1 },
                new EditableMapSocket { Id = "exit_entry", RoomId = "exit", X = 9, Y = 1, Direction = MapDirection.West, TargetRoomId = "boss", Width = 1 }
            };
            return draft;
        }

        private static EditableMapDraftAsset BuildOptionalTreasureValidationDraft()
        {
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.InitializeBlank(15, 3, 1f, 1f);
            for (var x = 0; x < 15; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    string roomId;
                    if (x < 3)
                    {
                        roomId = "start";
                    }
                    else if (x < 6)
                    {
                        roomId = "quest";
                    }
                    else if (x < 9)
                    {
                        roomId = "boss";
                    }
                    else if (x < 12)
                    {
                        roomId = "exit";
                    }
                    else
                    {
                        roomId = "treasure_room";
                    }

                    draft.TrySetCell(new EditableMapCell
                    {
                        X = x,
                        Y = y,
                        RoomId = roomId,
                        Terrain = RoomChunkCellType.Floor,
                        Height = 0,
                        Direction = MapDirection.North
                    });
                }
            }

            for (var y = 0; y < 3; y++)
            {
                draft.TrySetCell(new EditableMapCell
                {
                    X = 13,
                    Y = y,
                    RoomId = "treasure_room",
                    Terrain = RoomChunkCellType.Wall,
                    Height = 0,
                    Direction = MapDirection.North
                });
            }

            draft.Rooms = new[]
            {
                new EditableMapRoom { Id = "start", Role = MapRoomRole.Start, X = 0, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.East },
                new EditableMapRoom { Id = "quest", Role = MapRoomRole.QuestTarget, X = 3, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.East | MapDirection.West },
                new EditableMapRoom { Id = "boss", Role = MapRoomRole.Boss, X = 6, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.East | MapDirection.West },
                new EditableMapRoom { Id = "exit", Role = MapRoomRole.Exit, X = 9, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.East | MapDirection.West },
                new EditableMapRoom { Id = "treasure_room", Role = MapRoomRole.SideBranch, LayoutKind = RoomChunkLayoutKind.DeadEnd, X = 12, Y = 0, Width = 3, Height = 3, SocketMask = MapDirection.West }
            };
            draft.Sockets = new[]
            {
                new EditableMapSocket { Id = "start_exit", RoomId = "start", X = 2, Y = 1, Direction = MapDirection.East, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "quest_entry", RoomId = "quest", X = 3, Y = 1, Direction = MapDirection.West, TargetRoomId = "start", Width = 1 },
                new EditableMapSocket { Id = "quest_exit", RoomId = "quest", X = 5, Y = 1, Direction = MapDirection.East, TargetRoomId = "boss", Width = 1 },
                new EditableMapSocket { Id = "boss_entry", RoomId = "boss", X = 6, Y = 1, Direction = MapDirection.West, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "boss_exit", RoomId = "boss", X = 8, Y = 1, Direction = MapDirection.East, TargetRoomId = "exit", Width = 1 },
                new EditableMapSocket { Id = "exit_entry", RoomId = "exit", X = 9, Y = 1, Direction = MapDirection.West, TargetRoomId = "boss", Width = 1 },
                new EditableMapSocket { Id = "exit_treasure", RoomId = "exit", X = 11, Y = 1, Direction = MapDirection.East, TargetRoomId = "treasure_room", Width = 1 },
                new EditableMapSocket { Id = "treasure_entry", RoomId = "treasure_room", X = 12, Y = 1, Direction = MapDirection.West, TargetRoomId = "exit", Width = 1 }
            };
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "treasure_hidden",
                    Kind = RoomChunkObjectKind.Chest,
                    X = 14,
                    Y = 1,
                    Width = 1,
                    Depth = 1
                }
            };
            return draft;
        }

        private static EditableMapDraftAsset BuildBakeDraft()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.Id = "bake_probe";
            draft.SourceProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId;
            draft.Seed = 17;
            draft.InitializeBlank(profile.Width, profile.Height, 1f, 1f);
            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    var roomId = x < 2
                        ? "start"
                        : x < 4
                        ? "quest"
                        : x < 6
                        ? "boss"
                        : "exit";
                    draft.TrySetCell(new EditableMapCell
                    {
                        X = x,
                        Y = y,
                        RoomId = roomId,
                        Terrain = RoomChunkCellType.Floor,
                        Height = 0,
                        Direction = MapDirection.North,
                        MaterialId = "stone_floor"
                    });
                }
            }

            draft.Rooms = new[]
            {
                new EditableMapRoom { Id = "start", Role = MapRoomRole.Start, LayoutKind = RoomChunkLayoutKind.Room, X = 0, Y = 0, Width = 2, Height = 2, SocketMask = MapDirection.East },
                new EditableMapRoom { Id = "quest", Role = MapRoomRole.QuestTarget, LayoutKind = RoomChunkLayoutKind.Room, X = 2, Y = 0, Width = 2, Height = 2, SocketMask = MapDirection.East | MapDirection.West },
                new EditableMapRoom { Id = "boss", Role = MapRoomRole.Boss, LayoutKind = RoomChunkLayoutKind.Room, X = 4, Y = 0, Width = 2, Height = 2, SocketMask = MapDirection.East | MapDirection.West },
                new EditableMapRoom { Id = "exit", Role = MapRoomRole.Exit, LayoutKind = RoomChunkLayoutKind.Room, X = 6, Y = 0, Width = 2, Height = 2, SocketMask = MapDirection.West }
            };
            draft.Sockets = new[]
            {
                new EditableMapSocket { Id = "start_quest", RoomId = "start", X = 1, Y = 0, Direction = MapDirection.East, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "quest_start", RoomId = "quest", X = 2, Y = 0, Direction = MapDirection.West, TargetRoomId = "start", Width = 1 },
                new EditableMapSocket { Id = "quest_boss", RoomId = "quest", X = 3, Y = 0, Direction = MapDirection.East, TargetRoomId = "boss", Width = 1 },
                new EditableMapSocket { Id = "boss_quest", RoomId = "boss", X = 4, Y = 0, Direction = MapDirection.West, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "boss_exit", RoomId = "boss", X = 5, Y = 0, Direction = MapDirection.East, TargetRoomId = "exit", Width = 1 },
                new EditableMapSocket { Id = "exit_boss", RoomId = "exit", X = 6, Y = 0, Direction = MapDirection.West, TargetRoomId = "boss", Width = 1 }
            };
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "spawn_hint",
                    PaletteObjectId = "spawn_hint",
                    Kind = RoomChunkObjectKind.SpawnHint,
                    X = 3,
                    Y = 1,
                    Width = 1,
                    Depth = 1,
                    RuntimeReferenceId = "spawn_runtime",
                    MaterialId = "spawn_marker"
                },
                new EditableMapObjectPlacement
                {
                    Id = "treasure",
                    PaletteObjectId = "treasure_chest",
                    Kind = RoomChunkObjectKind.Chest,
                    X = 6,
                    Y = 1,
                    Width = 1,
                    Depth = 1,
                    RuntimeReferenceId = "treasure_runtime",
                    MaterialId = "treasure_gold"
                }
            };
            return draft;
        }

        private static void AssertRuntimeSafeType(System.Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var fieldType = field.FieldType;
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(fieldType), Is.False, $"{type.Name}.{field.Name} must not store UnityEngine.Object.");
                Assert.That((fieldType.Namespace ?? string.Empty).StartsWith("UnityEditor"), Is.False, $"{type.Name}.{field.Name} must not store UnityEditor type.");
                Assert.That((fieldType.Namespace ?? string.Empty).StartsWith("Conn.Editor"), Is.False, $"{type.Name}.{field.Name} must not store editor type.");
                Assert.That((fieldType.Namespace ?? string.Empty).StartsWith("Conn.Authoring"), Is.False, $"{type.Name}.{field.Name} must not store authoring type.");
            }
        }
    }
}

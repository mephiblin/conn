using Conn.Authoring.Maps;
using Conn.Core.Maps;
using Conn.Editor.Maps;
using Conn.Core.Quests;
using Conn.Runtime.Maps;
using NUnit.Framework;
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
        public void EditableDraftBuilderConvertsGeneratedDraftIntoCellMap()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var generated = MapGenerationService.Generate(profile, chunks, 2001);
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();

            EditableMapDraftBuilder.PopulateFromGeneratedDraft(draft, generated, profile, 1, 0, 0.5f, 0.25f);

            Assert.That(draft.Width, Is.GreaterThan(0));
            Assert.That(draft.Height, Is.GreaterThan(0));
            Assert.That(draft.Rooms.Length, Is.EqualTo(generated.Graph.Nodes.Count));
            Assert.That(draft.Sockets.Length, Is.EqualTo(generated.Graph.Edges.Count * 2));
            Assert.That(draft.Objects.Length, Is.GreaterThanOrEqualTo(0));
            Assert.That(draft.Cells.Length, Is.EqualTo(draft.Width * draft.Height));
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
        public void EditableValidationReportsBlockedRequiredRoute()
        {
            var draft = BuildLinearValidationDraft();
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "blocker",
                    Kind = RoomChunkObjectKind.Blocker,
                    X = 1,
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
    }
}

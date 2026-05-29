using Conn.Core.Maps;
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
            Assert.That(loaded.Cells[1].Type, Is.EqualTo(RoomChunkCellType.Slope));
            Assert.That(loaded.Cells[1].Direction, Is.EqualTo(MapDirection.East));
            Assert.That(loaded.Cells[1].MaterialId, Is.EqualTo("moss"));
            Assert.That(loaded.Cells[2].Height, Is.EqualTo(1));
            Assert.That(loaded.Objects.Count, Is.EqualTo(2));
            Assert.That(loaded.Objects[0].Kind, Is.EqualTo(RoomChunkObjectKind.Chest));
            Assert.That(loaded.Objects[0].PrefabId, Is.EqualTo("chest_small"));
            Assert.That(loaded.Objects[1].Height, Is.EqualTo(1));
        }
    }
}

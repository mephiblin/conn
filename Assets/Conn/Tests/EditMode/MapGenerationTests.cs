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
    }
}

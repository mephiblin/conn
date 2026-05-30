using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using NUnit.Framework;
using UnityEngine;

namespace Conn.Tests.EditMode
{
    public sealed class MapGenV2IntegrationTests
    {
        [Test]
        public void ProfileDraftGenerateAcceptAndBakeDataFlow()
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
                PopulateValidRoomShape(roomShape);
                PopulateMinimumModuleSet(moduleSet, out floor, out wall);
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "integration_profile";
                profile.MapSize = new Vector2Int(8, 6);
                profile.StyleSet = styleSet;
                profile.LayoutRules = ruleSet;
                profile.RoomShapes = new[] { roomShape };
                draft.Profile = profile;
                draft.Seed = 2001;

                var profileReport = profile.Validate();
                Assert.That(profileReport.IsValid, Is.True);

                var generationReport = draft.GenerateFromProfile();
                Assert.That(generationReport.IsValid, Is.True);
                Assert.That(draft.Cells, Has.Length.EqualTo(profile.MapSize.x * profile.MapSize.y));

                draft.ApplyPostProcessingFromProfile();
                draft.Accept();

                var bakedCells = MapGenRuntimeBakeDataBuilder.BuildCells(draft.Width, draft.Height, draft.Cells);
                var traversalEdges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells);

                Assert.That(draft.Accepted, Is.True);
                Assert.That(draft.IsAcceptedSignatureCurrent, Is.True);
                Assert.That(bakedCells, Is.Not.Empty);
                Assert.That(traversalEdges, Is.Not.Empty);
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

        private static void PopulateValidRoomShape(MapGenRoomShapeAsset roomShape)
        {
            roomShape.Resize(new Vector2Int(3, 3));
            roomShape.SetCell(1, 1, new MapGenShapeCell
            {
                State = MapGenCellState.Room,
                SocketKind = MapGenSocketKind.None,
                SocketId = string.Empty
            });
        }

        private static void PopulateMinimumModuleSet(MapGenModuleSetAsset moduleSet, out GameObject floor, out GameObject wall)
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
    }
}

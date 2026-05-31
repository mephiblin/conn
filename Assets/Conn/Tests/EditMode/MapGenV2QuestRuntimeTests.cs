using Conn.Core.Combat;
using Conn.Core.Content;
using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.MapGenV2.Core;
using Conn.Runtime.Content;
using Conn.Runtime.Maps;
using Conn.Runtime.Session;
using Conn.Runtime.World;
using NUnit.Framework;
using UnityEngine;

namespace Conn.Tests.EditMode
{
    public sealed class MapGenV2QuestRuntimeTests
    {
        [Test]
        public void ActiveQuestProfileIdSelectsMatchingMapGenV2BakedMap()
        {
            const string targetProfileId = "mapgenv2_quest_runtime_target_profile";
            var other = BuildBakedMap("mapgenv2_quest_runtime_other_profile", "other_signature");
            var target = BuildBakedMap(targetProfileId, "target_signature");
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(
                session,
                "generated_mapgenv2_profile_quest",
                "Generated MapGenV2 Profile Quest",
                MonsterCatalog.TestGuardId,
                11,
                EncounterCatalog.TestGuardId,
                targetProfileId);

            try
            {
                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(new[] { other, target });

                var compiled = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);

                Assert.That(compiled.MapId, Is.EqualTo("target_signature"));
                Assert.That(compiled.ProfileId, Is.EqualTo(targetProfileId));
            }
            finally
            {
                ResetDungeonRuntimeSources();
                Object.DestroyImmediate(other);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void MapGenV2QuestRegionRegistersAndSpawnsQuestTargetMonster()
        {
            const string profileId = "mapgenv2_quest_runtime_spawn_profile";
            var baked = BuildBakedMap(profileId, "spawn_signature");
            var root = new GameObject("MapGenV2 Quest Monster Root").transform;
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(
                session,
                "generated_mapgenv2_spawn_quest",
                "Generated MapGenV2 Spawn Quest",
                MonsterCatalog.TestGuardId,
                12,
                EncounterCatalog.TestGuardId,
                profileId);

            try
            {
                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(new[] { baked });

                var compiled = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);
                var questTarget = CompiledMapRuntimeLoader.FindPlacement(compiled, MapPlacementKind.QuestTarget);

                Assert.That(CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster(session, compiled), Is.True);
                var stateKey = CompiledMapDungeonRuntimeService.StateKeyFor(compiled, questTarget);
                var state = session.World.FindFieldMonster(stateKey);

                Assert.That(state, Is.Not.Null);
                Assert.That(state.MonsterId, Is.EqualTo(MonsterCatalog.TestGuardId));
                Assert.That(state.EncounterId, Is.EqualTo(EncounterCatalog.TestGuardId));
                Assert.That(FieldMonsterActorSpawner.SpawnFromCompiledMap(session, compiled, root), Is.EqualTo(1));
                Assert.That(root.GetComponentInChildren<FieldMonsterContact>(), Is.Not.Null);
            }
            finally
            {
                ResetDungeonRuntimeSources();
                Object.DestroyImmediate(root.gameObject);
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void RequiredQuestEncounterPlacementUsesAcceptedQuestMonster()
        {
            var compiled = new CompiledMap
            {
                MapId = "quest_override_map",
                ProfileId = "quest_override_profile",
                Seed = 2001,
                Width = 3,
                Height = 1
            };
            compiled.Placements.Add(new MapPlacement
            {
                Id = "quest_target",
                Kind = MapPlacementKind.QuestTarget,
                RoomId = "quest",
                X = 1,
                Y = 0
            });
            compiled.EncounterPlacements.Add(new CompiledEncounterPlacement
            {
                PlacementId = "quest_target_encounter",
                MapPlacementId = "quest_target",
                RoomId = "quest",
                EncounterId = EncounterCatalog.TestGuardId,
                PrimaryMonsterId = MonsterCatalog.TestGuardId,
                RequiredForQuest = true,
                X = 1,
                Y = 0
            });

            var root = new GameObject("Quest Override Monster Root").transform;
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(
                session,
                "quest_override",
                "Quest Override",
                "desert_rat",
                12,
                "encounter_desert_rat",
                compiled.ProfileId);

            try
            {
                Assert.That(CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster(session, compiled), Is.True);

                var stateKey = CompiledMapDungeonRuntimeService.StateKeyFor(compiled, compiled.Placements[0]);
                var state = session.World.FindFieldMonster(stateKey);
                Assert.That(state, Is.Not.Null);
                Assert.That(state.MonsterId, Is.EqualTo("desert_rat"));
                Assert.That(state.EncounterId, Is.EqualTo("encounter_desert_rat"));

                Assert.That(FieldMonsterActorSpawner.SpawnFromCompiledMap(session, compiled, root), Is.EqualTo(1));
                state = session.World.FindFieldMonster(stateKey);
                Assert.That(state.MonsterId, Is.EqualTo("desert_rat"));
                Assert.That(state.EncounterId, Is.EqualTo("encounter_desert_rat"));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        [Test]
        public void GeneratedBoardQuestUnlocksDungeonGateWithMapGenV2Profile()
        {
            const string profileId = "mapgenv2_quest_runtime_board_profile";
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            var session = new GameSessionState();

            try
            {
                database.Quests = new[]
                {
                    new ContentQuestDefinition
                    {
                        Id = "generated_mapgenv2_board_quest",
                        DisplayName = "Generated MapGenV2 Board Quest",
                        TargetMonsterId = MonsterCatalog.TestGuardId,
                        TargetEncounterId = $"generated_single_primary_{MonsterCatalog.TestGuardId}",
                        MapProfileId = profileId,
                        GoldReward = 13
                    }
                };
                RuntimeContentDatabase.SetActive(database);
                session.StartNewGame();

                QuestRuntimeService.AcceptCurrentBoardOffer(session);

                Assert.That(session.Quest.HasActiveQuest, Is.True);
                Assert.That(session.Quest.ActiveQuestId, Is.EqualTo("generated_mapgenv2_board_quest"));
                Assert.That(session.Quest.MapProfileId, Is.EqualTo(profileId));
                Assert.That(GateInteractable.CanEnterDungeon(session), Is.True);
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
                ResetDungeonRuntimeSources();
            }
        }

        private static MapGenBakedMapAsset BuildBakedMap(string profileId, string sourceSignature)
        {
            var baked = ScriptableObject.CreateInstance<MapGenBakedMapAsset>();
            baked.Version = MapGenBakedMapMigration.CurrentVersion;
            baked.ProfileId = profileId;
            baked.SourceSignature = sourceSignature;
            baked.Seed = CompiledMapDungeonRuntimeService.DefaultDungeonSeed;
            baked.Width = 3;
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
                },
                new MapGenBakedCell
                {
                    Coord = new MapGenGridCoord(2, 0),
                    State = MapGenCellState.Room,
                    RegionId = 3,
                    RoomCategory = MapGenRoomCategory.Exit
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
                },
                new MapGenBakedRegion
                {
                    RegionId = 3,
                    RoomCategory = MapGenRoomCategory.Exit,
                    CellCount = 1,
                    SourceTemplateId = "exit_template"
                }
            };
            return baked;
        }

        private static void ResetDungeonRuntimeSources()
        {
            CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
            CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
            CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(System.Array.Empty<MapGenBakedMapAsset>());
        }
    }
}

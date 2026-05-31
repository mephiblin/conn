using Conn.Core.Content;
using Conn.Core.Combat;
using Conn.Core.Equipment;
using Conn.Core.Maps;
using Conn.Core.Quests;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.MapGenV2.Core;
using Conn.Runtime.Combat;
using Conn.Runtime.Content;
using Conn.Runtime.Maps;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using Conn.Runtime.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Conn.Tests.EditMode
{
    public sealed class GameFlowPlaytestTests
    {
        private const string ContentDatabasePath = "Assets/Conn/Core/Content/ContentDatabase.asset";
        private const string PlaytestBakedMapFolder = "Assets/Conn/Core/MapGenV2/PlaytestBakedMaps";
        private const string TwistedTempleCompiledMapPath = "Assets/Conn/Core/Maps/twisted_temple_2001_CompiledMap.asset";
        private const string TwistedTempleProfileId = "twisted_temple";
        private const string TwistedTempleQuestId = "quest_twisted_temple_clear";
        private const string PlaytestProfilePrefix = "mapgenv2_playtest_";

        [Test]
        public void StartCharacterTownQuestDungeonCombatFlowReachesCombatAndCompletesTarget()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(ContentDatabasePath);
            Assert.That(database, Is.Not.Null, "The runtime content database asset must exist.");
            RuntimeContentDatabase.SetActive(database);

            var session = new GameSessionState();
            var actorRoot = new GameObject("Game Flow Test Field Monsters").transform;

            try
            {
                session.BeginCharacterCreation();
                Assert.That(session.Mode, Is.EqualTo(GameMode.CharacterCreation));
                Assert.That(SaveRuntimeService.SceneForLoadedState(session), Is.EqualTo(GameSceneId.Title));

                session.StartNewGame(new CharacterCreationOptions
                {
                    CharacterName = "Flow Tester",
                    SelectedPortraitIndex = 1,
                    SelectedPortraitId = "portrait_duelist",
                    Strength = 6,
                    Dexterity = 7,
                    Vitality = 5,
                    Energy = 4,
                    StarterWeaponId = EquipmentCatalog.RustySwordId
                });
                Assert.That(session.Mode, Is.EqualTo(GameMode.Town));
                Assert.That(session.Character.CharacterName, Is.EqualTo("Flow Tester"));
                Assert.That(session.Inventory.HasItem(EquipmentCatalog.RustySwordId), Is.True);

                var offer = FindMapGenV2BoardOffer();
                Assert.That(offer, Is.Not.Null, "Quest board must expose at least one MapGenV2 playtest quest.");
                QuestRuntimeService.AcceptQuest(session, offer);

                Assert.That(session.Quest.HasActiveQuest, Is.True);
                Assert.That(session.Quest.MapProfileId, Does.StartWith(PlaytestProfilePrefix));
                Assert.That(GateInteractable.CanEnterDungeon(session), Is.True);

                session.Mode = GameMode.Dungeon;
                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(LoadPlaytestBakedMaps());

                var compiledMap = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);
                Assert.That(compiledMap.ProfileId, Is.EqualTo(session.Quest.MapProfileId));
                var questTarget = CompiledMapRuntimeLoader.FindPlacement(compiledMap, MapPlacementKind.QuestTarget);
                Assert.That(questTarget, Is.Not.Null);
                Assert.That(CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster(session, compiledMap), Is.True);
                Assert.That(FieldMonsterActorSpawner.SpawnFromCompiledMap(session, compiledMap, actorRoot), Is.GreaterThanOrEqualTo(1));

                var stateKey = CompiledMapDungeonRuntimeService.StateKeyFor(compiledMap, questTarget);
                Assert.That(FieldMonsterRuntimeService.TryBeginCombatHandoff(session, stateKey, 0f), Is.True);

                session.Mode = GameMode.Combat;
                CombatRuntimeService.StartTestCombat(session);
                Assert.That(session.Combat.Active, Is.True);
                Assert.That(session.Combat.FieldMonsterStateKey, Is.EqualTo(stateKey));
                Assert.That(session.Combat.EncounterId, Is.EqualTo(session.Quest.TargetEncounterId));
                Assert.That(session.Combat.MonsterId, Is.EqualTo(session.Quest.TargetMonsterId));

                CombatRuntimeService.StopReels(session);
                session.Combat.Enemy.Setup(session.Quest.TargetMonsterId, "Flow Target", 1);
                CombatRuntimeService.ToggleDieSelection(session, 0);
                CombatRuntimeService.ResolveSelectedDice(session);

                Assert.That(session.Combat.Active, Is.False);
                Assert.That(session.Quest.TargetDefeated, Is.True);
                Assert.That(session.Quest.ReturnAvailable, Is.True);
                Assert.That(FieldMonsterRuntimeService.IsDefeated(session, stateKey), Is.True);
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(System.Array.Empty<MapGenBakedMapAsset>());
                Object.DestroyImmediate(actorRoot.gameObject);
            }
        }

        [Test]
        public void QuestBoardRerollKeepsBoardOpenAndCyclesGeneratedMapGenV2Offers()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(ContentDatabasePath);
            Assert.That(database, Is.Not.Null, "The runtime content database asset must exist.");
            RuntimeContentDatabase.SetActive(database);
            TownNpcInteractionState.Open(TownNpcInteractionKind.QuestBoard, "Quest Board", "Pick a contract.");

            try
            {
                var session = new GameSessionState();
                session.StartNewGame();

                var before = RuntimeContentDatabase.BoardQuestAt(session.Quest.BoardOfferIndex);
                QuestRuntimeService.RerollBoard(session);
                var after = RuntimeContentDatabase.BoardQuestAt(session.Quest.BoardOfferIndex);

                Assert.That(TownNpcInteractionState.IsOpen, Is.True);
                Assert.That(TownQuestBoardPanelState.IsOpen, Is.True);
                Assert.That(before, Is.Not.Null);
                Assert.That(after, Is.Not.Null);
                Assert.That(after.QuestId, Is.Not.EqualTo(before.QuestId));
                Assert.That(FindMapGenV2BoardOffer(), Is.Not.Null);
            }
            finally
            {
                TownNpcInteractionState.Close();
                RuntimeContentDatabase.SetActive(null);
            }
        }

        [Test]
        public void QuestBoardNpcInteractionReopensDesyncedPanelState()
        {
            TownNpcInteractionState.Open(TownNpcInteractionKind.QuestBoard, "Quest Board", "Pick a contract.");
            TownQuestBoardPanelState.CloseForNpcInteraction();

            try
            {
                Assert.That(TownNpcInteractionState.IsOpen, Is.True);
                Assert.That(TownQuestBoardPanelState.IsOpen, Is.False);

                TownQuestBoardPanelState.OpenForNpcInteraction();

                Assert.That(TownNpcInteractionState.IsOpen, Is.True);
                Assert.That(TownQuestBoardPanelState.IsOpen, Is.True);
            }
            finally
            {
                TownNpcInteractionState.Close();
            }
        }

        [Test]
        public void ShopNpcInteractionReopensDesyncedPanelState()
        {
            TownNpcInteractionState.Open(TownNpcInteractionKind.Blacksmith, "Blacksmith", "Need steel?");
            TownShopPanelState.CloseForNpcInteraction();

            try
            {
                Assert.That(TownNpcInteractionState.IsOpen, Is.True);
                Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.None));

                TownShopPanelState.OpenForNpcInteraction(TownShopPanelKind.Blacksmith);

                Assert.That(TownNpcInteractionState.IsOpen, Is.True);
                Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.Blacksmith));
            }
            finally
            {
                TownNpcInteractionState.Close();
            }
        }

        [Test]
        public void ClosingOneTownNpcDoesNotPoisonNextNpcInteraction()
        {
            try
            {
                TownNpcInteractionState.Open(TownNpcInteractionKind.Blacksmith, "Blacksmith", "Need steel?");
                Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.Blacksmith));

                TownNpcInteractionState.Close();

                Assert.That(TownNpcInteractionState.IsOpen, Is.False);
                Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.None));
                Assert.That(TownQuestBoardPanelState.IsOpen, Is.False);

                TownNpcInteractionState.Open(TownNpcInteractionKind.SkillMerchant, "Skill Merchant", "Need power?");

                Assert.That(TownNpcInteractionState.IsOpen, Is.True);
                Assert.That(TownNpcInteractionState.Kind, Is.EqualTo(TownNpcInteractionKind.SkillMerchant));
                Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.SkillMerchant));

                TownNpcInteractionState.Close();
                TownNpcInteractionState.Open(TownNpcInteractionKind.QuestBoard, "Quest Board", "Pick a contract.");

                Assert.That(TownNpcInteractionState.IsOpen, Is.True);
                Assert.That(TownNpcInteractionState.Kind, Is.EqualTo(TownNpcInteractionKind.QuestBoard));
                Assert.That(TownQuestBoardPanelState.IsOpen, Is.True);
                Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.None));
            }
            finally
            {
                TownNpcInteractionState.Close();
            }
        }

        [Test]
        public void CharacterCreationHasThreePortraitSpritesAndSelectableStarterWeapons()
        {
            var portraitPaths = new[]
            {
                "Assets/Conn/UI/Runtime/Resources/CharacterCreation/portrait_vanguard.png",
                "Assets/Conn/UI/Runtime/Resources/CharacterCreation/portrait_duelist.png",
                "Assets/Conn/UI/Runtime/Resources/CharacterCreation/portrait_arcanist.png"
            };

            for (var i = 0; i < portraitPaths.Length; i++)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(portraitPaths[i]), Is.Not.Null, portraitPaths[i]);
            }

            var session = new GameSessionState();
            session.StartNewGame(new CharacterCreationOptions
            {
                CharacterName = "Creation Tester",
                SelectedPortraitIndex = 2,
                SelectedPortraitId = "portrait_arcanist",
                Strength = 12,
                Dexterity = 18,
                Vitality = 22,
                Energy = 30,
                StarterWeaponId = EquipmentCatalog.GreatAxeId
            });

            Assert.That(session.Character.SelectedPortraitId, Is.EqualTo("portrait_arcanist"));
            Assert.That(session.Character.StarterWeaponId, Is.EqualTo(EquipmentCatalog.GreatAxeId));
            Assert.That(session.Inventory.HasItem(EquipmentCatalog.GreatAxeId), Is.True);
            Assert.That(session.Equipment.EquippedWeaponId, Is.EqualTo(EquipmentCatalog.GreatAxeId));
        }

        [Test]
        public void TwistedTempleQuestUsesDedicatedCompiledMapAndBakedDesertRatEncounters()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(ContentDatabasePath);
            var compiledAsset = AssetDatabase.LoadAssetAtPath<CompiledMapAsset>(TwistedTempleCompiledMapPath);
            Assert.That(database, Is.Not.Null, "The runtime content database asset must exist.");
            Assert.That(compiledAsset, Is.Not.Null, "The twisted temple compiled map asset must exist.");
            RuntimeContentDatabase.SetActive(database);

            var session = new GameSessionState();
            var actorRoot = new GameObject("Twisted Temple Field Monsters").transform;

            try
            {
                var quest = RuntimeContentDatabase.FindQuest(TwistedTempleQuestId);
                Assert.That(quest, Is.Not.Null);
                Assert.That(quest.MapProfileId, Is.EqualTo(TwistedTempleProfileId));
                Assert.That(quest.TargetMonsterId, Is.EqualTo("desert_rat"));
                Assert.That(quest.TargetEncounterId, Is.EqualTo("encounter_desert_rat"));

                session.StartNewGame();
                QuestRuntimeService.AcceptQuest(session, quest);

                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(new[] { compiledAsset });
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(System.Array.Empty<MapGenBakedMapAsset>());

                var compiledMap = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);
                Assert.That(compiledMap.ProfileId, Is.EqualTo(TwistedTempleProfileId));
                Assert.That(compiledMap.MapId, Is.EqualTo("twisted_temple_2001"));
                Assert.That(compiledMap.EncounterPlacements, Is.Not.Empty);

                for (var i = 0; i < compiledMap.EncounterPlacements.Count; i++)
                {
                    Assert.That(compiledMap.EncounterPlacements[i].EncounterId, Is.EqualTo(session.Quest.TargetEncounterId));
                    Assert.That(compiledMap.EncounterPlacements[i].PrimaryMonsterId, Is.EqualTo(session.Quest.TargetMonsterId));
                }

                var questTarget = CompiledMapRuntimeLoader.FindPlacement(compiledMap, MapPlacementKind.QuestTarget);
                Assert.That(CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster(session, compiledMap), Is.True);
                Assert.That(FieldMonsterActorSpawner.SpawnFromCompiledMap(session, compiledMap, actorRoot), Is.GreaterThanOrEqualTo(1));

                var state = session.World.FindFieldMonster(CompiledMapDungeonRuntimeService.StateKeyFor(compiledMap, questTarget));
                Assert.That(state, Is.Not.Null);
                Assert.That(state.EncounterId, Is.EqualTo("encounter_desert_rat"));
                Assert.That(state.MonsterId, Is.EqualTo("desert_rat"));
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(System.Array.Empty<MapGenBakedMapAsset>());
                Object.DestroyImmediate(actorRoot.gameObject);
            }
        }

        [Test]
        public void DungeonBuildRefreshesStaleAcceptedQuestFromRuntimeContent()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(ContentDatabasePath);
            var compiledAsset = AssetDatabase.LoadAssetAtPath<CompiledMapAsset>(TwistedTempleCompiledMapPath);
            Assert.That(database, Is.Not.Null, "The runtime content database asset must exist.");
            Assert.That(compiledAsset, Is.Not.Null, "The twisted temple compiled map asset must exist.");
            RuntimeContentDatabase.SetActive(database);

            var session = new GameSessionState();

            try
            {
                session.StartNewGame();
                QuestRuntimeService.AcceptQuest(
                    session,
                    TwistedTempleQuestId,
                    "Stale Twisted Temple",
                    MonsterCatalog.TestGuardId,
                    1,
                    EncounterCatalog.TestGuardId,
                    MapGenerationCatalog.ChapterTwoFirstSliceProfileId);

                Assert.That(session.Quest.MapProfileId, Is.EqualTo(MapGenerationCatalog.ChapterTwoFirstSliceProfileId));

                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(new[] { compiledAsset });
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(System.Array.Empty<MapGenBakedMapAsset>());

                var compiledMap = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);

                Assert.That(session.Quest.MapProfileId, Is.EqualTo(TwistedTempleProfileId));
                Assert.That(session.Quest.TargetMonsterId, Is.EqualTo("desert_rat"));
                Assert.That(session.Quest.TargetEncounterId, Is.EqualTo("encounter_desert_rat"));
                Assert.That(compiledMap.ProfileId, Is.EqualTo(TwistedTempleProfileId));
                Assert.That(compiledMap.MapId, Is.EqualTo("twisted_temple_2001"));
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
                CompiledMapDungeonRuntimeService.SetCompiledMapAssets(System.Array.Empty<CompiledMapAsset>());
                CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(System.Array.Empty<RuntimeMapGenerationBundleAsset>());
                CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(System.Array.Empty<MapGenBakedMapAsset>());
            }
        }

        [Test]
        public void DungeonSceneBindsTwistedTempleCompiledMap()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Conn/Scenes/Dungeon.unity", OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);

            var bootstrap = Object.FindObjectOfType<SceneBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.ContentDatabase, Is.Not.Null);
            Assert.That(bootstrap.CompiledMaps, Is.Not.Null);
            Assert.That(
                System.Array.Exists(bootstrap.CompiledMaps, asset => asset != null && asset.ProfileId == TwistedTempleProfileId),
                Is.True,
                "Dungeon scene must bind the dedicated twisted temple compiled map.");

            RuntimeContentDatabase.SetActive(bootstrap.ContentDatabase);
            try
            {
                var quest = RuntimeContentDatabase.FindQuest(TwistedTempleQuestId);
                Assert.That(quest, Is.Not.Null);
                Assert.That(quest.MapProfileId, Is.EqualTo(TwistedTempleProfileId));
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static QuestDefinition FindMapGenV2BoardOffer()
        {
            for (var i = 0; i < 32; i++)
            {
                var offer = RuntimeContentDatabase.BoardQuestAt(i);
                if (offer != null && offer.MapProfileId.StartsWith(PlaytestProfilePrefix, System.StringComparison.Ordinal))
                {
                    return offer;
                }
            }

            return null;
        }

        private static MapGenBakedMapAsset[] LoadPlaytestBakedMaps()
        {
            var result = new System.Collections.Generic.List<MapGenBakedMapAsset>();
            foreach (var guid in AssetDatabase.FindAssets("t:MapGenBakedMapAsset", new[] { PlaytestBakedMapFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<MapGenBakedMapAsset>(path);
                if (asset != null)
                {
                    result.Add(asset);
                }
            }

            Assert.That(result, Has.Count.EqualTo(3), "Expected the three generated MapGenV2 playtest baked maps.");
            return result.ToArray();
        }
    }
}

using System;
using Conn.Core.Combat;
using Conn.Core.Content;
using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Maps;
using Conn.Core.Quests;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Equipment;
using Conn.Runtime.Inventory;
using Conn.Runtime.Maps;
using Conn.Runtime.Session;
using Conn.Runtime.Combat;
using Conn.Runtime.Content;
using Conn.Runtime.Skills;
using Conn.Runtime.World;
using Conn.Rendering.Interaction;
using Conn.UI.Runtime;
using UnityEngine;

namespace Conn.Editor.Tools
{
    public static class RuntimeRuleVerifier
    {
        public static void VerifyChapterOneCoreRules()
        {
            ContentDatabaseVerifier.VerifyContentDatabase();
            VerifyP1VerticalSliceFlow();
            VerifyP1DeathContinueRouting();
            VerifyEquipmentDiceRules();
            VerifyArmorSlotEquipRules();
            VerifyArmorStatEffects();
            VerifyEquipmentLoadoutToggle();
            VerifyDirectEquipmentChangesResizeSkillFaces();
            VerifyNewGameState();
            VerifyDiceSkillEffects();
            VerifyCombatStatusEffects();
            VerifyCombatWinGrantsXp();
            VerifyCombatContentDefinitions();
            VerifyRuntimeContentDatabaseMonsterLookup();
            VerifyChapterTwoRuntimeDataConsumption();
            VerifyCombatFleeRestoresDungeonState();
            VerifyCombatDeathRoutesToEndingState();
            VerifyFieldMonsterExpeditionStatus();
            VerifySkillFaceCycling();
            VerifyCombatHandoffStateKey();
            VerifyQuestBoardFlow();
            VerifyTownPanelState();
            VerifyQuestReturnRewardSummary();
            VerifyKeepExploringReturnPrompt();
            VerifyTownServices();
            VerifyShopServices();
            VerifySkillMerchantStockRefresh();
            VerifyRuntimeNotice();
            VerifyChapterOneUxDisplayStrings();
            VerifyP1HudLayoutContracts();
            VerifyRuntimeCanvasUiLayoutContracts();
            VerifySaveContractRoundTrip();
            VerifyEquipmentAndSkillDisplayData();
            VerifyEquipmentComparisonDisplayData();
            VerifyConsumables();
            VerifySkillSaleProtection();
            VerifyCompiledMapRuntimeLoader();
            Debug.Log("Conn runtime core rule verification passed.");
        }

        public static void VerifyChapterTwoRuntimeDataConsumption()
        {
            VerifyRuntimeContentDatabaseEquipmentRules();
            VerifyRuntimeContentDatabaseQuestEncounterRuntime();
            VerifyVendorRotationRuntimeStock();
            VerifyDatabaseTownServiceRuntime();
            VerifyCompiledMapDungeonRuntimeConnection();
            Debug.Log("Conn Chapter 2 runtime data consumption verification passed.");
        }

        private static void VerifyCompiledMapRuntimeLoader()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = MapGenerationService.Generate(profile, chunks, 2001);
            var compiled = MapGenerationService.Compile(profile, draft);
            var json = JsonUtility.ToJson(compiled);
            var loaded = CompiledMapRuntimeLoader.LoadAndValidateFromJson(json, profile);
            var quest = QuestCatalog.Find(QuestCatalog.TestHuntId);
            var questReport = MapValidationService.ValidateQuestMapContract(quest, profile, loaded);
            var questPlacement = CompiledMapRuntimeLoader.FindPlacement(loaded, MapPlacementKind.QuestTarget);

            Expect(loaded.ProfileId == profile.ProfileId, "Runtime compiled map loader must preserve profile id.");
            Expect(loaded.Rooms.Count == compiled.Rooms.Count, "Runtime compiled map loader must preserve rooms.");
            Expect(loaded.Doors.Count == compiled.Doors.Count, "Runtime compiled map loader must preserve doors.");
            Expect(loaded.Placements.Count == compiled.Placements.Count, "Runtime compiled map loader must preserve placements.");
            Expect(questReport.Passed, "Quest map contract must accept the generated compiled map.");
            Expect(!string.IsNullOrEmpty(questPlacement.RoomId), "Runtime compiled map loader must expose the quest target placement room.");
        }

        private static void VerifyP1HudLayoutContracts()
        {
            var smallOverlay = P0SceneOverlay.OverlayAreaRect(320, 240);
            var tinyOverlay = P0SceneOverlay.OverlayAreaRect(220, 160);
            var prompt = PlayerWorldInteractor.PromptRect(220, 160);

            Expect(smallOverlay.x >= 0f && smallOverlay.xMax <= 320f, "P1 HUD overlay must stay inside a 320px wide screen.");
            Expect(smallOverlay.y >= 0f && smallOverlay.yMax <= 240f, "P1 HUD overlay must stay inside a 240px tall screen.");
            Expect(tinyOverlay.x >= 0f && tinyOverlay.xMax <= 220f, "P1 HUD overlay must clamp inside very small screen widths.");
            Expect(tinyOverlay.y >= 0f && tinyOverlay.yMax <= 160f, "P1 HUD overlay must clamp inside very small screen heights.");
            Expect(prompt.x >= 0f && prompt.xMax <= 220f, "Interaction prompt must stay inside small screen widths.");
            Expect(prompt.y >= 0f && prompt.yMax <= 160f, "Interaction prompt must stay inside small screen heights.");
        }

        private static void VerifyRuntimeCanvasUiLayoutContracts()
        {
            VerifyPanelSafeRects(RuntimeCanvasUiBuilder.CommonPanelNames);
            VerifyPanelSafeRects(RuntimeCanvasUiBuilder.TitlePanelNames);
            VerifyPanelSafeRects(RuntimeCanvasUiBuilder.TownPanelNames);
            VerifyPanelSafeRects(RuntimeCanvasUiBuilder.DungeonPanelNames);
            VerifyPanelSafeRects(RuntimeCanvasUiBuilder.CombatPanelNames);
            VerifyPanelSafeRects(RuntimeCanvasUiBuilder.EndingPanelNames);
        }

        private static void VerifyPanelSafeRects(string[] panelNames)
        {
            for (var i = 0; i < panelNames.Length; i++)
            {
                var rect = RuntimeCanvasUiBuilder.NormalizedSafeRectForPanel(panelNames[i]);
                Expect(rect.xMin >= 0f && rect.xMax <= 1f, $"Runtime uGUI panel {panelNames[i]} must clamp horizontally.");
                Expect(rect.yMin >= 0f && rect.yMax <= 1f, $"Runtime uGUI panel {panelNames[i]} must clamp vertically.");
                Expect(rect.width > 0f && rect.height > 0f, $"Runtime uGUI panel {panelNames[i]} must have positive normalized size.");
            }
        }

        private static void VerifyRuntimeContentDatabaseMonsterLookup()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Monsters = new[]
            {
                new ContentMonsterDefinition
                {
                    Id = "monster_runtime_database_probe",
                    DisplayName = "Runtime Database Probe",
                    MaxHp = 9,
                    AttackPower = 3,
                    XpReward = 7,
                    Ai = "Probe strike"
                }
            };

            try
            {
                RuntimeContentDatabase.SetActive(database);
                var session = new GameSessionState();
                session.StartNewGame();
                QuestRuntimeService.AcceptQuest(session, "quest_runtime_database_probe", "Runtime Probe", "monster_runtime_database_probe", 1);
                session.Mode = GameMode.Combat;
                CombatRuntimeService.StartTestCombat(session);

                Expect(session.Combat.MonsterId == "monster_runtime_database_probe", "Runtime combat must consume ContentDatabase monster ids before catalog fallback.");
                Expect(session.Combat.Enemy.DisplayName == "Runtime Database Probe", "Runtime combat must use ContentDatabase monster display name.");
                Expect(session.Combat.Enemy.MaxHp == 9, "Runtime combat must use ContentDatabase monster HP.");
                Expect(session.Combat.EnemyAttackPower == 3, "Runtime combat must use ContentDatabase monster attack power.");
                Expect(session.Combat.XpReward == 7, "Runtime combat must use ContentDatabase monster XP fallback.");
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
            }
        }

        private static void VerifyRuntimeContentDatabaseEquipmentRules()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Equipment = new[]
            {
                new ContentEquipmentDefinition
                {
                    Id = "db_only_greatsword",
                    DisplayName = "DB Only Greatsword",
                    Kind = "two_hand_weapon",
                    BuyPrice = 15,
                    SellPrice = 7
                },
                new ContentEquipmentDefinition
                {
                    Id = "db_only_helm",
                    DisplayName = "DB Only Helm",
                    Kind = "head_armor",
                    BuyPrice = 5,
                    SellPrice = 2,
                    ArmorValue = 2
                }
            };

            try
            {
                RuntimeContentDatabase.SetActive(database);
                var session = new GameSessionState();
                session.StartNewGame();
                session.Inventory.AddItem("db_only_greatsword");
                session.Inventory.AddItem("db_only_helm");

                Expect(EquipmentRuntimeService.TryEquip(session, "db_only_greatsword"), "Runtime must equip database-only two-hand equipment.");
                Expect(session.Equipment.WeaponGrip == WeaponGrip.TwoHand, "Database-only two-hand weapon must drive weapon grip.");
                Expect(session.Equipment.DiceCount == 5, "Database-only two-hand weapon must drive dice count.");

                Expect(EquipmentRuntimeService.TryEquip(session, "db_only_helm"), "Runtime must equip database-only armor.");
                Expect(session.Equipment.EquippedHeadId == "db_only_helm", "Database-only armor must occupy the correct armor slot.");
                Expect(session.Equipment.ArmorValue >= 2, "Database-only armor must contribute armor value.");
                Expect(session.Equipment.DefenseBonus >= 2, "Database-only armor must contribute combat defense.");
                Expect(session.Equipment.ComparisonLineFor("db_only_helm").Contains("DB Only Helm"), "Database-only equipment must resolve comparison display data.");
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
            }
        }

        private static void VerifyRuntimeContentDatabaseQuestEncounterRuntime()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Quests = new[]
            {
                new ContentQuestDefinition
                {
                    Id = "db_only_guard_quest",
                    DisplayName = "DB Only Guard Quest",
                    TargetMonsterId = "db_only_guard",
                    TargetEncounterId = "db_only_guard_encounter",
                    MapProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId,
                    GoldReward = 3
                }
            };
            database.Monsters = new[]
            {
                new ContentMonsterDefinition
                {
                    Id = "db_only_guard",
                    DisplayName = "DB Only Guard",
                    MaxHp = 8,
                    AttackPower = 2,
                    XpReward = 4,
                    Ai = "DB Strike"
                }
            };
            database.Encounters = new[]
            {
                new ContentEncounterDefinition
                {
                    Id = "db_only_guard_encounter",
                    DisplayName = "DB Only Guard Encounter",
                    MonsterId = "db_only_guard",
                    XpReward = 9,
                    RewardId = "db_reward_probe",
                    Pattern = "single_primary",
                    EnemySlots = new[]
                    {
                        new ContentEncounterEnemySlot
                        {
                            SlotId = "primary",
                            MonsterId = "db_only_guard",
                            Count = 1,
                            Primary = true
                        }
                    }
                }
            };

            try
            {
                RuntimeContentDatabase.SetActive(database);
                var session = new GameSessionState();
                session.StartNewGame();
                var boardOffer = QuestRuntimeService.CurrentBoardOffer(session);
                Expect(boardOffer.QuestId == "db_only_guard_quest", "Quest board must prefer validated database quest offers.");
                QuestRuntimeService.AcceptCurrentBoardOffer(session);

                Expect(session.Quest.ActiveQuestId == "db_only_guard_quest", "Runtime quest acceptance must consume database-only quest ids.");
                Expect(session.Quest.TargetEncounterId == "db_only_guard_encounter", "Runtime quest state must keep database encounter reference.");
                Expect(session.Quest.MapProfileId == MapGenerationCatalog.ChapterTwoFirstSliceProfileId, "Runtime quest state must keep database map profile reference.");

                var compiled = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);
                var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
                var quest = RuntimeContentDatabase.FindQuest("db_only_guard_quest");
                var report = MapValidationService.ValidateQuestMapContract(quest, profile, compiled);
                Expect(report.Passed, "Database quest map profile must validate against generated compiled map.");

                session.Mode = GameMode.Combat;
                CombatRuntimeService.StartTestCombat(session);
                Expect(session.Combat.EncounterId == "db_only_guard_encounter", "Combat must resolve the database quest encounter before monster fallback.");
                Expect(session.Combat.EncounterPattern == "single_primary", "Combat must preserve the database encounter pattern contract.");
                Expect(session.Combat.EncounterRewardId == "db_reward_probe", "Combat must preserve the database encounter reward id contract.");
                Expect(session.Combat.MonsterId == "db_only_guard", "Combat must resolve the database encounter monster.");
                Expect(session.Combat.XpReward == 9, "Combat must use database encounter XP reward before monster fallback.");
                Expect(session.Combat.EnemySlots.Count == 1, "Combat must expose encounter enemy slots for runtime UI and later multi-enemy execution.");
                Expect(session.Combat.EnemySlots[0].MonsterId == "db_only_guard", "Combat enemy slot must preserve the primary monster reference.");
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
            }
        }

        private static void VerifyVendorRotationRuntimeStock()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Skills = new[]
            {
                new ContentSkillDefinition
                {
                    Id = "db_vendor_guard",
                    DisplayName = "DB Vendor Guard",
                    EffectKind = "guard",
                    BuyPrice = 4,
                    SellPrice = 2,
                    Power = 1,
                    CatalogIds = new[] { "merchant_basic" }
                },
                new ContentSkillDefinition
                {
                    Id = "db_vendor_focus",
                    DisplayName = "DB Vendor Focus",
                    EffectKind = "attack",
                    BuyPrice = 6,
                    SellPrice = 3,
                    Power = 2,
                    CatalogIds = new[] { "merchant_basic" }
                },
                new ContentSkillDefinition
                {
                    Id = "db_vendor_floor_two",
                    DisplayName = "DB Vendor Floor Two",
                    EffectKind = "attack",
                    BuyPrice = 8,
                    SellPrice = 4,
                    Power = 3,
                    CatalogIds = new[] { "merchant_advanced" }
                }
            };
            database.Equipment = new[]
            {
                new ContentEquipmentDefinition
                {
                    Id = "db_vendor_sword",
                    DisplayName = "DB Vendor Sword",
                    Kind = "one_hand_weapon",
                    BuyPrice = 5,
                    SellPrice = 2
                },
                new ContentEquipmentDefinition
                {
                    Id = "db_vendor_axe",
                    DisplayName = "DB Vendor Axe",
                    Kind = "two_hand_weapon",
                    BuyPrice = 9,
                    SellPrice = 4
                }
            };
            database.Vendors = new[]
            {
                new ContentVendorDefinition
                {
                    Id = "merchant_basic",
                    ServiceType = "skill_shop",
                    StockSkillIds = new[] { "db_vendor_guard", "db_vendor_focus" },
                    Rotations = new[]
                    {
                        new ContentVendorRotationDefinition
                        {
                            MinFloor = 2,
                            StockSkillIds = new[] { "db_vendor_floor_two" }
                        }
                    }
                },
                new ContentVendorDefinition
                {
                    Id = "vendor_smith",
                    ServiceType = "equipment_shop",
                    StockItemIds = new[] { "db_vendor_sword" },
                    Rotations = new[]
                    {
                        new ContentVendorRotationDefinition
                        {
                            MinFloor = 2,
                            StockItemIds = new[] { "db_vendor_axe" }
                        }
                    }
                }
            };

            try
            {
                RuntimeContentDatabase.SetActive(database);
                var session = new GameSessionState();
                session.StartNewGame();

                var baseStock = SkillShopRuntimeService.SkillMerchantStock(session);
                Expect(baseStock.Length == 2, "Database vendor stock must flow into runtime skill shop stock.");
                Expect(baseStock[0] == "db_vendor_guard", "Runtime skill shop must use database vendor stock order.");
                Expect(SkillShopRuntimeService.BuyAndEquip(session, "db_vendor_guard"), "Runtime skill shop must sell database-only skill stock.");
                Expect(session.Skills.HasSkill("db_vendor_guard"), "Runtime skill purchase must add database-only skill to inventory.");

                SkillShopRuntimeService.RefreshSkillMerchantStock(session, 2, 0);
                var rotatedStock = SkillShopRuntimeService.SkillMerchantStock(session, 2, 0);
                Expect(rotatedStock.Length == 1, "Vendor rotation must replace runtime skill shop stock.");
                Expect(rotatedStock[0] == "db_vendor_floor_two", "Runtime skill shop must consume floor-based vendor rotation stock.");

                var smithBaseStock = RuntimeContentDatabase.EquipmentIdsForVendor("vendor_smith");
                Expect(smithBaseStock.Length == 1 && smithBaseStock[0] == "db_vendor_sword", "Blacksmith stock must consume database vendor equipment stock.");
                var smithRotatedStock = RuntimeContentDatabase.EquipmentIdsForVendor("vendor_smith", 2, 0);
                Expect(smithRotatedStock.Length == 1 && smithRotatedStock[0] == "db_vendor_axe", "Blacksmith stock must consume database equipment vendor rotation.");
                Expect(EquipmentShopRuntimeService.BuyAndEquip(session, "db_vendor_axe"), "Runtime equipment shop must sell database-only rotated blacksmith stock.");
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
            }
        }

        private static void VerifyCompiledMapDungeonRuntimeConnection()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var generated = MapGenerationService.Compile(profile, MapGenerationService.Generate(profile, MapGenerationCatalog.ChapterTwoFirstSliceChunks(), 2001));
            var asset = ScriptableObject.CreateInstance<CompiledMapAsset>();
            asset.ProfileId = profile.ProfileId;
            asset.Seed = 2001;
            asset.Json = JsonUtility.ToJson(generated);
            CompiledMapDungeonRuntimeService.SetCompiledMapAssets(new[] { asset });
            var compiled = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session);
            var start = CompiledMapDungeonRuntimeService.FindStartAnchor(compiled);
            var exit = CompiledMapDungeonRuntimeService.FindExitAnchor(compiled);
            var questTarget = CompiledMapRuntimeLoader.FindPlacement(compiled, MapPlacementKind.QuestTarget);

            Expect(start.Kind == MapPlacementKind.Start, "Compiled map runtime connection must expose a start anchor.");
            Expect(compiled.MapId == generated.MapId, "Runtime dungeon map must prefer saved compiled map assets before generator fallback.");
            Expect(exit.Kind == MapPlacementKind.Exit, "Compiled map runtime connection must expose an exit anchor.");
            Expect(CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster(session, compiled), "Compiled map quest placement must register a field monster.");

            var stateKey = CompiledMapDungeonRuntimeService.StateKeyFor(compiled, questTarget);
            var state = session.World.FindFieldMonster(stateKey);
            Expect(state != null, "Compiled map placement must create runtime field monster state.");
            Expect(state.PlacementId == questTarget.Id, "Runtime field monster must remember compiled placement id.");
            Expect(state.EncounterId == session.Quest.TargetEncounterId, "Runtime field monster must use quest encounter reference.");
            Expect(state.MonsterId == session.Quest.TargetMonsterId, "Runtime field monster must use quest monster reference.");
            CompiledMapDungeonRuntimeService.SetCompiledMapAssets(null);
        }

        private static void VerifyDatabaseTownServiceRuntime()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Items = new[]
            {
                new ContentItemDefinition
                {
                    Id = ConsumableCatalog.MinorPotionId,
                    DisplayName = "DB Minor Potion",
                    Kind = "consumable",
                    BuyPrice = 2,
                    SellPrice = 1,
                    HealAmount = 4
                }
            };
            database.Vendors = new[]
            {
                new ContentVendorDefinition
                {
                    Id = "vendor_inn",
                    ServiceType = "heal_party",
                    GoldCost = 3
                },
                new ContentVendorDefinition
                {
                    Id = "vendor_apothecary",
                    ServiceType = "sell_bundle",
                    GoldCost = 2,
                    StockItemIds = new[] { ConsumableCatalog.MinorPotionId },
                    Rotations = new[]
                    {
                        new ContentVendorRotationDefinition
                        {
                            MinFloor = 2,
                            GoldCost = 4,
                            StockItemIds = new[] { ConsumableCatalog.MinorPotionId }
                        }
                    }
                }
            };
            database.Npcs = new[]
            {
                new ContentNpcDefinition
                {
                    Id = "npc_apothecary",
                    DisplayName = "DB Apothecary",
                    ServiceType = "sell_bundle",
                    VendorId = "vendor_apothecary"
                }
            };

            try
            {
                RuntimeContentDatabase.SetActive(database);
                var session = new GameSessionState();
                session.StartNewGame();
                session.Player.Damage(5);

                Expect(TownServiceRuntimeService.CostFor(TownServiceKind.Inn, 99) == 3, "Town service must prefer database vendor service cost.");
                Expect(TownServiceRuntimeService.Rest(session, TownServiceRuntimeService.CostFor(TownServiceKind.Inn, 99)), "Database-backed inn service must run in runtime.");
                Expect(session.Player.Hp == session.Player.MaxHp, "Database-backed inn service must heal the player.");
                Expect(RuntimeContentDatabase.FindNpc("npc_apothecary")?.VendorId == "vendor_apothecary", "Runtime must expose database NPC vendor links.");
                Expect(RuntimeContentDatabase.SelectVendorRotation("vendor_apothecary", 2, 0)?.GoldCost == 4, "Runtime must expose non-skill vendor rotation conditions.");
                Expect(ConsumableRuntimeService.Buy(session, ConsumableCatalog.MinorPotionId), "Apothecary must buy database item definitions at runtime.");
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
            }
        }

        private static void VerifyP1VerticalSliceFlow()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            Expect(session.Mode == GameMode.Town, "P1 flow must start in Town after new game.");
            Expect(!session.Quest.HasActiveQuest, "P1 flow must start without active quest.");

            var boardOffer = QuestRuntimeService.CurrentBoardOffer(session);
            Expect(boardOffer != null, "P1 flow requires a quest board offer.");
            QuestRuntimeService.AcceptCurrentBoardOffer(session);
            Expect(session.Quest.HasActiveQuest, "P1 flow quest board must create an active quest.");
            Expect(session.Quest.TargetMonsterId == boardOffer.TargetMonsterId, "P1 flow quest target must match board offer.");

            session.Mode = GameMode.Dungeon;
            FieldMonsterRuntimeService.Register(session, "field_monster_test_guard", "placement_test_guard", "encounter_test_guard", session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_test_guard");
            Expect(FieldMonsterRuntimeService.FindCombatHandoff(session) != null, "P1 flow monster contact must create combat handoff state.");

            session.Mode = GameMode.Combat;
            CombatRuntimeService.StartTestCombat(session);
            Expect(session.Combat.Active, "P1 flow combat scene must start combat session.");
            Expect(session.Combat.FieldMonsterStateKey == "field_monster_test_guard", "P1 flow combat must remember source monster state.");
            Expect(session.Combat.EncounterId == EncounterCatalog.TestGuardId, "P1 flow combat must resolve the test guard encounter.");
            Expect(session.Combat.EncounterPattern == "single_primary", "P1 flow combat must expose the single-primary encounter fallback pattern.");
            Expect(session.Combat.Enemy.Id == MonsterCatalog.TestGuardId, "P1 flow combat must resolve the test guard monster.");
            Expect(session.Combat.Enemy.MaxHp == MonsterCatalog.Find(MonsterCatalog.TestGuardId).MaxHp, "P1 flow combat must use monster HP data.");
            Expect(session.Combat.EnemyAttackPower == MonsterCatalog.Find(MonsterCatalog.TestGuardId).EnemyActionPower, "P1 flow combat must use monster enemy action power data.");
            Expect(session.Combat.EnemyActionName == MonsterCatalog.Find(MonsterCatalog.TestGuardId).EnemyActionName, "P1 flow combat must use monster enemy action name data.");
            Expect(session.Combat.XpReward == EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward, "P1 flow combat must use encounter XP reward data.");
            Expect(CombatRuntimeService.DescribeDiceFace(session.Combat.DiceFaces[0]).Contains("ready"), "P1 combat HUD helper must report ready dice state.");

            session.Combat.Enemy.Setup(session.Quest.TargetMonsterId, "Loop Target", 1);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            Expect(CombatRuntimeService.DescribeDiceFace(session.Combat.DiceFaces[0]).Contains("selected"), "P1 combat HUD helper must report selected dice state.");
            CombatRuntimeService.ResolveSelectedDice(session);
            session.Mode = GameMode.Dungeon;
            Expect(session.Quest.TargetDefeated, "P1 flow combat win must mark quest target defeated.");
            Expect(session.Quest.ReturnAvailable, "P1 flow combat win must enable return.");
            Expect(FieldMonsterRuntimeService.IsDefeated(session, "field_monster_test_guard"), "P1 flow combat win must clear field monster.");
            Expect(session.Player.Xp >= EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward, "P1 flow combat win must grant XP.");
            Expect(session.Combat.LastMessage.Contains("Victory"), "P1 flow combat win must report victory feedback.");

            QuestRuntimeService.KeepExploring(session);
            Expect(session.Quest.ReturnPromptSeen, "P1 flow keep exploring must dismiss prompt.");

            var goldBeforeReturn = session.Gold;
            var reward = session.Quest.GoldReward;
            QuestRuntimeService.CompleteReturn(session);
            session.Mode = GameMode.Town;
            Expect(!session.Quest.HasActiveQuest, "P1 flow return must clear quest.");
            Expect(session.Gold == goldBeforeReturn + reward, "P1 flow return must grant reward.");
            Expect(session.Quest.LastGoldReward == reward, "P1 flow return must store reward summary.");
        }

        private static void VerifyP1DeathContinueRouting()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            session.Mode = GameMode.Combat;
            CombatRuntimeService.StartTestCombat(session);

            CombatRuntimeService.Die(session);

            Expect(session.Mode == GameMode.Ending, "P1 death flow must route to Ending.");
            Expect(session.LastNotice.Contains("Defeat"), "P1 death flow must report defeat feedback.");

            var loaded = new GameSessionState();
            SaveRuntimeService.OverwriteFromJson(SaveRuntimeService.ToJson(session), loaded);
            loaded.Combat.Clear();

            Expect(loaded.Mode == GameMode.Ending, "P1 continue must preserve Ending mode after death save.");
            Expect(SaveRuntimeService.SceneForLoadedState(loaded) == GameSceneId.Ending, "P1 continue must route death save to Ending scene.");

            loaded.StartNewGame();

            Expect(loaded.Mode == GameMode.Town, "P1 Ending New Game must overwrite death state with Town mode.");
            Expect(!loaded.Player.IsDead, "P1 Ending New Game must restore a living player.");
        }

        private static void VerifyEquipmentDiceRules()
        {
            var equipment = new PlayerEquipmentState
            {
                EquippedWeaponId = string.Empty,
                EquippedShieldId = string.Empty
            };
            Expect(equipment.DiceCount == 2, "No weapon must provide 2 dice.");

            equipment.Equip(EquipmentCatalog.RustySwordId);
            Expect(equipment.DiceCount == 4, "One-hand weapon must provide 4 dice.");
            Expect(equipment.DefenseBonus == 0, "One-hand weapon alone must not grant defense.");

            equipment.Equip(EquipmentCatalog.IronShieldId);
            Expect(equipment.DiceCount == 3, "One-hand weapon plus shield must provide 3 dice.");
            Expect(equipment.DefenseBonus == 1, "Shield loadout must grant defense.");

            equipment.Equip(EquipmentCatalog.GreatAxeId);
            Expect(equipment.DiceCount == 5, "Two-hand weapon must provide 5 dice.");
            Expect(string.IsNullOrEmpty(equipment.EquippedShieldId), "Two-hand weapon must clear shield.");
        }

        private static void VerifyArmorSlotEquipRules()
        {
            var equipment = new PlayerEquipmentState();

            equipment.Equip(EquipmentCatalog.LeatherCapId);
            equipment.Equip(EquipmentCatalog.PaddedVestId);
            equipment.Equip(EquipmentCatalog.TravelerGlovesId);
            equipment.Equip(EquipmentCatalog.ReinforcedPantsId);
            equipment.Equip(EquipmentCatalog.WornBootsId);

            Expect(equipment.EquippedHeadId == EquipmentCatalog.LeatherCapId, "Head armor must equip into head slot.");
            Expect(equipment.EquippedChestId == EquipmentCatalog.PaddedVestId, "Chest armor must equip into chest slot.");
            Expect(equipment.EquippedArmsId == EquipmentCatalog.TravelerGlovesId, "Arms armor must equip into arms slot.");
            Expect(equipment.EquippedLegsId == EquipmentCatalog.ReinforcedPantsId, "Leg armor must equip into legs slot.");
            Expect(equipment.EquippedFeetId == EquipmentCatalog.WornBootsId, "Feet armor must equip into feet slot.");
            Expect(equipment.IsEquipped(EquipmentCatalog.PaddedVestId), "Equipped armor must be reported as equipped.");
        }

        private static void VerifyArmorStatEffects()
        {
            var equipment = new PlayerEquipmentState();

            equipment.Equip(EquipmentCatalog.LeatherCapId);
            equipment.Equip(EquipmentCatalog.PaddedVestId);
            equipment.Equip(EquipmentCatalog.TravelerGlovesId);
            equipment.Equip(EquipmentCatalog.ReinforcedPantsId);
            equipment.Equip(EquipmentCatalog.WornBootsId);

            Expect(equipment.ArmorValue == 6, "Equipped armor must contribute aggregate armor value.");
            Expect(equipment.DefenseBonus == 6, "Armor value must contribute to defense bonus.");

            equipment.Equip(EquipmentCatalog.IronShieldId);

            Expect(equipment.DefenseBonus == 7, "Shield defense must stack with armor defense.");
        }

        private static void VerifyNewGameState()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            Expect(session.Mode == GameMode.Town, "New game must start in Town mode.");
            Expect(session.Gold == 25, "New game must start with 25 gold.");
            Expect(session.Inventory.HasItem(EquipmentCatalog.RustySwordId), "New game must own rusty sword.");
            Expect(session.Equipment.EquippedWeaponId == EquipmentCatalog.RustySwordId, "New game must equip rusty sword.");
            Expect(session.Skills.HasSkill(SkillCatalog.SlashId), "New game must own Slash.");
            Expect(session.Skills.EquippedSkillIds.Count > 0, "New game must equip at least one skill face.");
            Expect(session.Skills.EquippedSkillIds[0] == SkillCatalog.SlashId, "New game must equip Slash on first face.");
            Expect(QuestRuntimeService.CurrentBoardOffer(session)?.QuestId == QuestCatalog.TestHuntId, "New game board must start on test hunt.");

            QuestRuntimeService.RerollBoard(session);
            Expect(QuestRuntimeService.CurrentBoardOffer(session)?.QuestId == QuestCatalog.GuardPatrolId, "Quest board reroll must advance to next offer.");
        }

        private static void VerifyEquipmentLoadoutToggle()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Inventory.AddItem(EquipmentCatalog.IronShieldId);
            session.Inventory.AddItem(EquipmentCatalog.GreatAxeId);

            Expect(EquipmentRuntimeService.ToggleOwnedLoadout(session), "Owned loadout toggle must equip two-hand weapon when available.");
            Expect(session.Equipment.WeaponGrip == WeaponGrip.TwoHand, "First loadout toggle must switch to two-hand.");
            Expect(session.Equipment.DiceCount == 5, "Two-hand loadout must provide 5 dice.");

            Expect(EquipmentRuntimeService.ToggleOwnedLoadout(session), "Owned loadout toggle must return to one-hand shield when available.");
            Expect(session.Equipment.WeaponGrip == WeaponGrip.OneHandAndShield, "Second loadout toggle must switch to one-hand shield.");
            Expect(session.Equipment.DiceCount == 3, "One-hand shield loadout must provide 3 dice.");
        }

        private static void VerifyDirectEquipmentChangesResizeSkillFaces()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Inventory.AddItem(EquipmentCatalog.IronShieldId);
            session.Inventory.AddItem(EquipmentCatalog.GreatAxeId);
            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.AddSkill(SkillCatalog.MendId);
            session.Skills.ResizeEquippedFaces(4);
            session.Skills.EquippedSkillIds.Add(SkillCatalog.GuardId);
            session.Skills.EquippedSkillIds.Add(SkillCatalog.MendId);
            session.Skills.EquippedSkillIds.Add(SkillCatalog.SlashId);

            Expect(EquipmentRuntimeService.TryEquip(session, EquipmentCatalog.GreatAxeId), "Direct equipment panel must equip owned two-hand weapon.");
            Expect(session.Equipment.WeaponGrip == WeaponGrip.TwoHand, "Direct equipment panel must enter two-hand loadout.");
            Expect(session.Skills.EquippedSkillIds.Count <= 5, "Two-hand loadout must allow up to five skill faces.");

            Expect(EquipmentRuntimeService.TryEquip(session, EquipmentCatalog.IronShieldId), "Direct equipment panel must equip shield from two-hand state.");
            Expect(session.Equipment.EquippedWeaponId == EquipmentCatalog.RustySwordId, "Equipping shield from two-hand must restore one-hand weapon.");
            Expect(session.Equipment.WeaponGrip == WeaponGrip.OneHandAndShield, "Equipping shield must result in one-hand shield loadout.");
            Expect(session.Skills.EquippedSkillIds.Count == 3, "Shield loadout must resize skill faces down to three dice.");
        }

        private static void VerifyDiceSkillEffects()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.AddSkill(SkillCatalog.MendId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.SlashId;
            session.Skills.EquippedSkillIds[1] = SkillCatalog.GuardId;
            session.Skills.EquippedSkillIds[2] = SkillCatalog.MendId;
            session.Player.Damage(4);

            CombatRuntimeService.StartTestCombat(session);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ToggleDieSelection(session, 1);
            CombatRuntimeService.ToggleDieSelection(session, 2);
            CombatRuntimeService.ResolveSelectedDice(session);

            Expect(session.Combat.Enemy.Hp == 10, "Slash must deal 2 damage to the test monster.");
            Expect(session.Player.Hp == 17, "Guard and Mend result must leave player at 17 HP.");
            Expect(session.Combat.Player.Hp == 17, "Combat player HP must sync with persistent player HP.");
            Expect(session.Combat.Round == 2, "Resolving a non-lethal turn must advance the combat round.");
            Expect(session.Combat.DiceFaces[0].Cooldown == 1, "Resolved dice must enter cooldown then tick to 1.");
            Expect(session.Combat.LastMessage.Contains("2 damage"), "Combat message must report damage.");
            Expect(session.Combat.LastMessage.Contains("2 guard"), "Combat message must report guard.");
            Expect(session.Combat.LastMessage.Contains("3 heal"), "Combat message must report healing.");
            Expect(session.Combat.LastMessage.Contains("Test Gate Guard uses Halberd thrust for 2 damage"), "Combat message must report enemy action and reduced damage.");
            Expect(session.Combat.LastMessage.Contains("4 power"), "Combat message must report enemy action power.");
            Expect(session.Combat.LastMessage.Contains("2 blocked"), "Combat message must report blocked enemy damage.");
        }

        private static void VerifySkillSaleProtection()
        {
            var skills = new SkillInventoryState();
            skills.AddSkill(SkillCatalog.SlashId);
            skills.EquipFirstOpenFace(SkillCatalog.SlashId, 4);

            Expect(!skills.RemoveLooseSkill(SkillCatalog.SlashId), "Equipped skill without a loose copy must not be removable.");

            skills.AddSkill(SkillCatalog.SlashId);

            Expect(skills.RemoveLooseSkill(SkillCatalog.SlashId), "Loose duplicate skill must be removable.");
            Expect(skills.CountOwned(SkillCatalog.SlashId) == 1, "Selling loose duplicate must leave one owned Slash.");
            Expect(skills.CountEquipped(SkillCatalog.SlashId) == 1, "Selling loose duplicate must preserve equipped Slash.");
        }

        private static void VerifyCombatStatusEffects()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.FocusStrikeId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.FocusStrikeId;

            CombatRuntimeService.StartTestCombat(session);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Expect(session.Combat.Enemy.Hp == 7, "Focus Strike must deal direct damage plus one Bleed tick.");
            Expect(session.Combat.Enemy.StatusEffects.Count == 1, "Focus Strike must leave Bleed active after its first tick.");
            Expect(session.Combat.Enemy.StatusEffects[0].Kind == CombatStatusEffectKind.Bleed, "Focus Strike status must be Bleed.");
            Expect(session.Combat.Enemy.StatusEffects[0].RemainingTurns == 1, "Bleed duration must decrement after ticking.");
            Expect(session.Combat.LastMessage.Contains("Focus Strike effect: applied Bleed"), "Combat log must report Bleed application.");
            Expect(session.Combat.LastMessage.Contains("Test Gate Guard suffers 1 Bleed damage"), "Combat log must report Bleed tick damage.");
            Expect(CombatRuntimeService.DescribeDiceFace(session.Combat.DiceFaces[0]).Contains("effect Bleed"), "Combat HUD helper must expose Focus Strike Bleed effect.");
            Expect(CombatRuntimeService.DescribeCombatantStatuses(session.Combat.Enemy).Contains("Bleed"), "Combat HUD helper must expose active Bleed status.");

            CombatRuntimeService.ToggleDieSelection(session, 1);
            CombatRuntimeService.ResolveSelectedDice(session);

            Expect(session.Combat.Enemy.Hp == 5, "Bleed must tick again on the next resolved turn.");
            Expect(session.Combat.Enemy.StatusEffects.Count == 0, "Bleed must expire when duration reaches zero.");
            Expect(session.Combat.LastMessage.Contains("Bleed ended"), "Combat log must report Bleed expiration.");

            session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.FocusStrikeId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.FocusStrikeId;

            CombatRuntimeService.StartTestCombat(session);
            session.Combat.Enemy.Setup(MonsterCatalog.TestGuardId, "Bleeding Target", 5);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Expect(!session.Combat.Active, "Status damage must be able to defeat the enemy.");
            Expect(session.Combat.Enemy.IsDead, "Status defeat must leave the enemy dead.");
            Expect(session.Combat.LastMessage.Contains("Bleeding Target suffers 1 Bleed damage"), "Status defeat log must include the final Bleed tick.");
            Expect(session.Combat.LastMessage.Contains("Enemy defeated"), "Status defeat log must include the win result.");
            Expect(session.Player.Xp == EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward, "Status defeat must grant combat XP.");
        }

        private static void VerifyCombatWinGrantsXp()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", EncounterCatalog.TestGuardId, session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");
            CombatRuntimeService.StartTestCombat(session);
            session.Combat.Enemy.Setup(session.Quest.TargetMonsterId, "Test Monster", 1);

            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Expect(session.Player.Xp == EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward, "Combat win must grant XP from encounter data.");
            Expect(session.Quest.TargetDefeated, "Combat win must complete quest target.");
            Expect(session.LastNotice.Contains($"Gained {EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward} XP"), "Combat win must report XP gain.");
        }

        private static void VerifyCombatContentDefinitions()
        {
            var monster = MonsterCatalog.Find(MonsterCatalog.TestGuardId);
            var encounter = EncounterCatalog.Find(EncounterCatalog.TestGuardId);
            var quest = QuestCatalog.Find(QuestCatalog.TestHuntId);

            Expect(monster != null, "Test guard monster definition must exist.");
            Expect(encounter != null, "Test guard encounter definition must exist.");
            Expect(quest != null, "Test hunt quest definition must exist.");
            Expect(monster.MaxHp == 12, "Test guard monster HP must match combat tuning data.");
            Expect(monster.AttackPower == 4, "Test guard monster attack must match combat tuning data.");
            Expect(monster.EnemyActionName == "Halberd thrust", "Test guard monster enemy action name must match combat tuning data.");
            Expect(monster.EnemyActionPower == 4, "Test guard monster enemy action power must match combat tuning data.");
            Expect(encounter.MonsterId == monster.MonsterId, "Test guard encounter must reference the test guard monster.");
            Expect(encounter.XpReward == 5, "Test guard encounter must define the combat XP reward.");
            Expect(quest.TargetMonsterId == monster.MonsterId, "Test hunt quest target must link to the test guard monster.");
        }

        private static void VerifyCombatFleeRestoresDungeonState()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", EncounterCatalog.TestGuardId, session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");
            CombatRuntimeService.StartTestCombat(session);

            CombatRuntimeService.Flee(session);

            Expect(session.Mode == GameMode.Dungeon, "Flee must return runtime state to Dungeon mode.");
            Expect(!session.Combat.Active, "Flee must clear active combat.");
            Expect(FieldMonsterRuntimeService.FindCombatHandoff(session) == null, "Flee must clear combat handoff state.");
            Expect(!FieldMonsterRuntimeService.IsDefeated(session, "field_monster_alpha"), "Flee must not defeat the field monster.");
        }

        private static void VerifyCombatDeathRoutesToEndingState()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            CombatRuntimeService.StartTestCombat(session);

            CombatRuntimeService.Die(session);

            Expect(session.Mode == GameMode.Ending, "Combat death must route runtime state to Ending mode.");
            Expect(!session.Combat.Active, "Combat death must clear active combat.");
            Expect(session.LastNotice.Contains("died"), "Combat death must report death notice.");
        }

        private static void VerifyFieldMonsterExpeditionStatus()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);

            Expect(FieldMonsterRuntimeService.ExpeditionStatus(session).Contains("none registered"), "Expedition HUD must expose missing field monster registration.");

            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", EncounterCatalog.TestGuardId, session.Quest.TargetMonsterId);

            Expect(FieldMonsterRuntimeService.CountActive(session) == 1, "Field monster runtime must count active monsters.");
            Expect(FieldMonsterRuntimeService.ExpeditionStatus(session).Contains("1 active"), "Expedition HUD must expose active field monster count.");

            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");

            Expect(FieldMonsterRuntimeService.ExpeditionStatus(session).Contains(session.Quest.TargetMonsterId), "Expedition HUD must expose combat handoff monster.");

            QuestRuntimeService.CompleteTarget(session, "field_monster_alpha");

            Expect(FieldMonsterRuntimeService.CountActive(session) == 0, "Defeated field monster must not count as active.");
            Expect(FieldMonsterRuntimeService.CountDefeated(session) == 1, "Field monster runtime must count defeated monsters.");
            Expect(FieldMonsterRuntimeService.ExpeditionStatus(session).Contains("Target defeated"), "Expedition HUD must expose target completion.");
        }

        private static void VerifySkillFaceCycling()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.AddSkill(SkillCatalog.MendId);

            Expect(SkillRuntimeService.CycleNextEditFace(session), "Cycling skill face must equip another owned skill.");
            Expect(session.Skills.EquippedSkillIds[0] == SkillCatalog.GuardId, "First cycle must move Slash to Guard.");
            Expect(session.Skills.NextEditFaceIndex == 1, "Cycling skill face must advance the edit face.");

            Expect(SkillRuntimeService.CycleNextEditFace(session), "Cycling next face must equip owned skill on following face.");
            Expect(session.Skills.EquippedSkillIds[1] == SkillCatalog.SlashId, "Second cycle must edit face 2.");
            Expect(session.Skills.NextEditFaceIndex == 2, "Second cycle must advance to face 3.");
        }

        private static void VerifyConsumables()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var startingGold = session.Gold;

            Expect(ConsumableRuntimeService.Buy(session, ConsumableCatalog.MinorPotionId), "Apothecary potion purchase must succeed.");
            Expect(session.Gold == startingGold - ConsumableCatalog.Find(ConsumableCatalog.MinorPotionId).BuyPrice, "Potion purchase must spend gold.");
            Expect(ConsumableRuntimeService.Count(session, ConsumableCatalog.MinorPotionId) == 1, "Potion purchase must add one potion.");

            session.Player.Damage(8);

            Expect(ConsumableRuntimeService.Use(session, ConsumableCatalog.MinorPotionId), "Owned potion must be usable.");
            Expect(session.Player.Hp == 18, "Potion must heal the player by its configured amount.");
            Expect(ConsumableRuntimeService.Count(session, ConsumableCatalog.MinorPotionId) == 0, "Potion use must consume one potion.");
            Expect(!ConsumableRuntimeService.Use(session, ConsumableCatalog.MinorPotionId), "Using a missing potion must fail.");
        }

        private static void VerifyCombatHandoffStateKey()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", EncounterCatalog.TestGuardId, session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");

            CombatRuntimeService.StartTestCombat(session);

            Expect(session.Combat.FieldMonsterStateKey == "field_monster_alpha", "Combat session must remember the field monster handoff key.");

            FieldMonsterRuntimeService.MarkIdle(session, session.Combat.FieldMonsterStateKey);

            Expect(FieldMonsterRuntimeService.FindCombatHandoff(session) == null, "Returning a combat handoff to idle must clear active handoff lookup.");
        }

        private static void VerifyQuestBoardFlow()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            var offer = QuestRuntimeService.CurrentBoardOffer(session);
            Expect(offer != null, "Quest board must expose a current offer.");

            QuestRuntimeService.RerollBoard(session);
            var rerolledOffer = QuestRuntimeService.CurrentBoardOffer(session);

            Expect(rerolledOffer != null, "Quest board reroll must expose a new current offer.");
            Expect(rerolledOffer.QuestId != offer.QuestId, "Quest board reroll must rotate the offer.");
            Expect(session.Quest.BoardRerollCount == 1, "Quest board reroll must increment visible reroll count.");

            QuestRuntimeService.AcceptCurrentBoardOffer(session);

            Expect(session.Quest.HasActiveQuest, "Accepting board offer must activate a quest.");
            Expect(session.Quest.ActiveQuestId == rerolledOffer.QuestId, "Accepted quest must match current board offer.");
            Expect(session.Quest.GoldReward == rerolledOffer.GoldReward, "Accepted quest must use current board reward.");
        }

        private static void VerifyTownPanelState()
        {
            TownShopPanelState.Close();
            TownQuestBoardPanelState.Close();

            TownShopPanelState.Open(TownShopPanelKind.Blacksmith);

            Expect(TownShopPanelState.Current == TownShopPanelKind.Blacksmith, "Opening blacksmith must set shop panel state.");
            Expect(!TownQuestBoardPanelState.IsOpen, "Opening shop must close quest board panel.");

            TownQuestBoardPanelState.Open();

            Expect(TownQuestBoardPanelState.IsOpen, "Opening quest board must set board panel state.");
            Expect(TownShopPanelState.Current == TownShopPanelKind.None, "Opening quest board must close shop panel.");

            TownQuestBoardPanelState.Close();
        }

        private static void VerifyEquipmentAndSkillDisplayData()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            Expect(EquipmentCatalog.Find(session.Equipment.EquippedWeaponId) != null, "Equipped weapon must resolve to display data.");
            Expect(session.Equipment.DiceCount > 0, "Equipment must expose positive dice count.");
            Expect(session.Equipment.DefenseBonus >= session.Equipment.ArmorValue, "Equipment display must expose aggregate defense.");
            Expect(SkillCatalog.Find(session.Skills.EquippedSkillIds[0]) != null, "Equipped skill face must resolve to display data.");
        }

        private static void VerifyEquipmentComparisonDisplayData()
        {
            var equipment = new PlayerEquipmentState();

            Expect(
                equipment.ComparisonLineFor(EquipmentCatalog.LeatherCapId) == "Head: None -> Leather Cap Defense +1",
                "Equipment comparison must show empty current slot and candidate armor defense.");

            equipment.Equip(EquipmentCatalog.LeatherCapId);
            equipment.Equip(EquipmentCatalog.IronShieldId);

            Expect(
                equipment.ComparisonLineFor(EquipmentCatalog.LeatherCapId) == "Head: Leather Cap -> Leather Cap",
                "Equipment comparison must show current item for matching armor slot.");
            Expect(
                equipment.ComparisonLineFor(EquipmentCatalog.GreatAxeId) == "Weapon: Rusty Sword -> Great Axe Defense -1",
                "Equipment comparison must show defense tradeoff when a two-hand weapon clears shield.");
        }

        private static void VerifyQuestReturnRewardSummary()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            var title = session.Quest.ActiveQuestTitle;
            var reward = session.Quest.GoldReward;
            var goldBefore = session.Gold;

            QuestRuntimeService.CompleteReturn(session);

            Expect(!session.Quest.HasActiveQuest, "Returning to town must clear active quest.");
            Expect(session.Gold == goldBefore + reward, "Returning to town must grant quest gold reward.");
            Expect(session.Quest.LastCompletedQuestTitle == title, "Returning to town must record completed quest title.");
            Expect(session.Quest.LastGoldReward == reward, "Returning to town must record gold reward summary.");
        }

        private static void VerifyKeepExploringReturnPrompt()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            QuestRuntimeService.CompleteTarget(session, "field_monster_alpha");

            Expect(session.Quest.ReturnAvailable, "Completing target must allow return.");
            Expect(!session.Quest.ReturnPromptSeen, "Completing target must show return prompt.");

            QuestRuntimeService.KeepExploring(session);

            Expect(session.Quest.ReturnPromptSeen, "Keeping exploration must persist return prompt dismissal.");
            Expect(session.Quest.ReturnAvailable, "Keeping exploration must keep return available.");
        }

        private static void VerifyTownServices()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var goldBefore = session.Gold;
            session.Player.GainXp(5);
            var xpBefore = session.Player.Xp;

            Expect(TownServiceRuntimeService.Train(session, 5), "Trainer service must spend gold and train player.");
            Expect(session.Gold == goldBefore, "Trainer service must not spend gold.");
            Expect(session.Player.Xp == xpBefore - 5, "Trainer service must spend configured XP.");
            Expect(session.Player.MaxHp == 22, "Trainer service must increase max HP.");
            Expect(session.Player.Hp == session.Player.MaxHp, "Trainer service must heal to trained max HP.");
            Expect(TownServiceRuntimeService.ScholarHint(session).Contains("board offer"), "Scholar must provide current board information without an active quest.");
        }

        private static void VerifyShopServices()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            Expect(EquipmentShopRuntimeService.CanBuy(session, EquipmentCatalog.IronShieldId), "Blacksmith must expose affordable shield purchase.");
            Expect(EquipmentShopRuntimeService.BuyAndEquip(session, EquipmentCatalog.IronShieldId), "Blacksmith must buy and equip shield.");
            Expect(session.Inventory.HasItem(EquipmentCatalog.IronShieldId), "Blacksmith purchase must add equipment to inventory.");
            Expect(session.Equipment.EquippedShieldId == EquipmentCatalog.IronShieldId, "Blacksmith purchase must equip shield.");
            Expect(!EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.IronShieldId), "Equipped shield must not be sellable.");
            Expect(!EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.RustySwordId), "Required starter sword must not be sellable.");

            Expect(EquipmentShopRuntimeService.CanBuy(session, EquipmentCatalog.LeatherCapId), "Blacksmith must expose affordable head armor purchase.");
            Expect(EquipmentShopRuntimeService.BuyAndEquip(session, EquipmentCatalog.LeatherCapId), "Blacksmith must buy and equip head armor.");
            Expect(session.Equipment.EquippedHeadId == EquipmentCatalog.LeatherCapId, "Blacksmith armor purchase must equip the matching armor slot.");
            Expect(!EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.LeatherCapId), "Equipped armor must not be sellable.");

            session.Inventory.AddItem(EquipmentCatalog.GreatAxeId);
            Expect(EquipmentShopRuntimeService.Sell(session, EquipmentCatalog.GreatAxeId), "Blacksmith must sell unequipped equipment.");
            Expect(!session.Inventory.HasItem(EquipmentCatalog.GreatAxeId), "Blacksmith sale must remove equipment from inventory.");

            session.Inventory.AddItem(EquipmentCatalog.PaddedVestId);
            Expect(EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.PaddedVestId), "Unequipped armor must be sellable.");
            Expect(EquipmentShopRuntimeService.Sell(session, EquipmentCatalog.PaddedVestId), "Blacksmith must sell unequipped armor.");
            Expect(!session.Inventory.HasItem(EquipmentCatalog.PaddedVestId), "Blacksmith armor sale must remove equipment from inventory.");

            var goldBeforeSkill = session.Gold;
            Expect(SkillShopRuntimeService.BuyAndEquip(session, SkillCatalog.GuardId), "Skill merchant must buy and equip skill.");
            Expect(session.Gold == goldBeforeSkill - SkillCatalog.Find(SkillCatalog.GuardId).BuyPrice, "Skill purchase must spend gold.");
            Expect(session.Skills.HasSkill(SkillCatalog.GuardId), "Skill purchase must add owned skill.");
            Expect(!SkillShopRuntimeService.CanSellLoose(session, SkillCatalog.GuardId), "Equipped skill without duplicate must not be sellable.");

            Expect(SkillShopRuntimeService.BuyAndEquip(session, SkillCatalog.GuardId), "Skill merchant must support duplicate skill cards.");
            Expect(SkillShopRuntimeService.CanSellLoose(session, SkillCatalog.GuardId), "Duplicate loose skill must be sellable.");
            Expect(SkillShopRuntimeService.SellLoose(session, SkillCatalog.GuardId), "Skill merchant must sell loose duplicate skill.");
        }

        private static void VerifySkillMerchantStockRefresh()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            var firstStock = SkillShopRuntimeService.SkillMerchantStock(session);
            Expect(firstStock.Length == SkillShopRuntimeService.SkillMerchantStockSize, "Skill merchant stock must be limited to the configured stock size.");
            Expect(firstStock[0] == SkillCatalog.GuardId, "Initial skill merchant stock must start at the first purchasable skill.");
            Expect(firstStock[1] == SkillCatalog.FocusStrikeId, "Initial skill merchant stock must include the next purchasable skill.");
            Expect(!SkillShopRuntimeService.IsSkillMerchantStocked(session, SkillCatalog.MendId), "Initial skill merchant stock must not expose every purchasable skill.");

            SkillShopRuntimeService.RefreshSkillMerchantStock(session);
            var refreshedStock = SkillShopRuntimeService.SkillMerchantStock(session);
            Expect(session.Shop.SkillMerchantRefreshIndex == 1, "Skill merchant stock refresh must advance the persisted refresh index.");
            Expect(refreshedStock[0] == SkillCatalog.FocusStrikeId, "Skill merchant refresh must rotate the first stock slot deterministically.");
            Expect(refreshedStock[1] == SkillCatalog.MendId, "Skill merchant refresh must rotate the second stock slot deterministically.");
            Expect(SkillShopRuntimeService.BuyAndEquip(session, SkillCatalog.GuardId), "Fixed catalog skill purchase path must keep working even when UI stock rotates.");
        }

        private static void VerifyRuntimeNotice()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            RuntimeNoticeService.Set(session, "notice check");

            Expect(session.LastNotice == "notice check", "Runtime notices must be stored on the session for HUD display.");
        }

        private static void VerifyChapterOneUxDisplayStrings()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            Expect(ChapterOneUxText.EquipmentStatus(session, EquipmentCatalog.RustySwordId).Contains("Equipped"), "Equipment UX text must expose equipped status.");
            Expect(ChapterOneUxText.EquipmentStatus(session, EquipmentCatalog.RustySwordId).Contains("starter weapon"), "Equipment UX text must explain starter sale lock.");

            session.Inventory.AddItem(EquipmentCatalog.PaddedVestId);
            Expect(ChapterOneUxText.EquipmentStatus(session, EquipmentCatalog.PaddedVestId).Contains("Sellable"), "Equipment UX text must expose sellable bag gear.");
            Expect(ChapterOneUxText.EquipmentBuyStatus(session, EquipmentCatalog.IronShieldId).Contains("Buyable"), "Blacksmith UX text must expose buyable stock.");

            Expect(ChapterOneUxText.ConsumableStatus(session, ConsumableCatalog.MinorPotionId).Contains("Owned x0"), "Consumable UX text must expose owned count.");

            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.EquipFirstOpenFace(SkillCatalog.GuardId, session.Equipment.DiceCount);
            Expect(ChapterOneUxText.SkillStatus(session, SkillCatalog.GuardId).Contains("Equipped x1"), "Skill UX text must expose equipped skill count.");
            Expect(ChapterOneUxText.SkillStatus(session, SkillCatalog.GuardId).Contains("No loose copy"), "Skill UX text must explain sale lock for equipped-only cards.");

            Expect(ChapterOneUxText.BlacksmithOpenNotice(session).Contains("Equipped items cannot be sold"), "Blacksmith notice must explain sale lock.");
            Expect(ChapterOneUxText.SkillMerchantOpenNotice(session).Contains("Stock:"), "Skill merchant notice must list stock.");
            SkillShopRuntimeService.RefreshSkillMerchantStock(session);
            Expect(session.LastNotice.Contains("refreshed to #1"), "Skill merchant refresh notice must expose refresh number.");
            Expect(ChapterOneUxText.GateBlockedNotice().Contains("Accept a quest"), "Gate blocked notice must explain required action.");
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            Expect(ChapterOneUxText.GateAllowedNotice(session).Contains(session.Quest.ActiveQuestTitle), "Gate allowed notice must name the active quest.");
        }

        private static void VerifySaveContractRoundTrip()
        {
            var source = new GameSessionState();
            source.StartNewGame();
            source.Mode = GameMode.Combat;
            source.Player.GainXp(7);
            source.Gold = 42;
            source.Inventory.AddItem(EquipmentCatalog.IronShieldId);
            source.Inventory.AddItem(EquipmentCatalog.LeatherCapId);
            source.Inventory.AddItem(EquipmentCatalog.PaddedVestId);
            source.Inventory.AddItem(EquipmentCatalog.TravelerGlovesId);
            source.Inventory.AddItem(EquipmentCatalog.ReinforcedPantsId);
            source.Inventory.AddItem(EquipmentCatalog.WornBootsId);
            source.Equipment.Equip(EquipmentCatalog.IronShieldId);
            source.Equipment.Equip(EquipmentCatalog.LeatherCapId);
            source.Equipment.Equip(EquipmentCatalog.PaddedVestId);
            source.Equipment.Equip(EquipmentCatalog.TravelerGlovesId);
            source.Equipment.Equip(EquipmentCatalog.ReinforcedPantsId);
            source.Equipment.Equip(EquipmentCatalog.WornBootsId);
            source.Skills.AddSkill(SkillCatalog.GuardId);
            SkillRuntimeService.CycleNextEditFace(source);
            SkillShopRuntimeService.RefreshSkillMerchantStock(source);
            QuestRuntimeService.AcceptQuest(source, QuestCatalog.TestHuntId);
            source.LastNotice = "saved notice";
            source.Combat.Active = true;

            var json = SaveRuntimeService.ToJson(source);
            var loaded = new GameSessionState();
            SaveRuntimeService.OverwriteFromJson(json, loaded);
            loaded.Combat.Clear();

            Expect(loaded.Mode == GameMode.Combat, "Save contract must preserve mode for continue routing.");
            Expect(loaded.Player.Xp == 7, "Save contract must preserve XP.");
            Expect(loaded.Gold == 42, "Save contract must preserve gold.");
            Expect(loaded.LastNotice == "saved notice", "Save contract must preserve last notice.");
            Expect(loaded.Equipment.WeaponGrip == WeaponGrip.OneHandAndShield, "Save contract must preserve equipment.");
            Expect(loaded.Equipment.EquippedHeadId == EquipmentCatalog.LeatherCapId, "Save contract must preserve head armor.");
            Expect(loaded.Equipment.EquippedChestId == EquipmentCatalog.PaddedVestId, "Save contract must preserve chest armor.");
            Expect(loaded.Equipment.EquippedArmsId == EquipmentCatalog.TravelerGlovesId, "Save contract must preserve arms armor.");
            Expect(loaded.Equipment.EquippedLegsId == EquipmentCatalog.ReinforcedPantsId, "Save contract must preserve legs armor.");
            Expect(loaded.Equipment.EquippedFeetId == EquipmentCatalog.WornBootsId, "Save contract must preserve feet armor.");
            Expect(loaded.Equipment.ArmorValue == 6, "Save contract must restore armor-derived stat value.");
            Expect(loaded.Equipment.DefenseBonus == 7, "Save contract must restore armor and shield defense bonus.");
            Expect(loaded.Skills.NextEditFaceIndex == source.Skills.NextEditFaceIndex, "Save contract must preserve next skill edit face.");
            Expect(loaded.Shop.SkillMerchantRefreshIndex == 1, "Save contract must preserve skill merchant refresh index.");
            Expect(loaded.Shop.SkillMerchantStockSkillIds.Count == source.Shop.SkillMerchantStockSkillIds.Count, "Save contract must preserve skill merchant stock count.");
            Expect(loaded.Shop.SkillMerchantStockSkillIds[0] == source.Shop.SkillMerchantStockSkillIds[0], "Save contract must preserve skill merchant stock order.");
            Expect(loaded.Quest.ActiveQuestId == QuestCatalog.TestHuntId, "Save contract must preserve active quest.");
            Expect(!loaded.Combat.Active, "Continue load must clear active combat.");
            Expect(SaveRuntimeService.SceneForLoadedState(loaded) == GameSceneId.Dungeon, "Continue from combat mode must resume in Dungeon.");

            source.Mode = GameMode.Ending;
            source.Player.Damage(source.Player.MaxHp);
            source.LastNotice = "You died.";
            json = SaveRuntimeService.ToJson(source);
            SaveRuntimeService.OverwriteFromJson(json, loaded);
            loaded.Combat.Clear();

            Expect(loaded.Mode == GameMode.Ending, "Save contract must preserve terminal ending mode.");
            Expect(loaded.Player.IsDead, "Save contract must preserve death state.");
            Expect(SaveRuntimeService.SceneForLoadedState(loaded) == GameSceneId.Ending, "Continue from death must return to Ending.");
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

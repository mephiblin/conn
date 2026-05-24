using System;
using Conn.Core.Combat;
using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Quests;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Equipment;
using Conn.Runtime.Inventory;
using Conn.Runtime.Session;
using Conn.Runtime.Combat;
using Conn.Runtime.Skills;
using Conn.Runtime.World;
using UnityEngine;

namespace Conn.Editor.Tools
{
    public static class RuntimeRuleVerifier
    {
        public static void VerifyChapterOneCoreRules()
        {
            ContentDatabaseVerifier.VerifyContentDatabase();
            VerifyP1VerticalSliceFlow();
            VerifyEquipmentDiceRules();
            VerifyArmorSlotEquipRules();
            VerifyEquipmentLoadoutToggle();
            VerifyDirectEquipmentChangesResizeSkillFaces();
            VerifyNewGameState();
            VerifyDiceSkillEffects();
            VerifyCombatWinGrantsXp();
            VerifyCombatContentDefinitions();
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
            VerifyRuntimeNotice();
            VerifySaveContractRoundTrip();
            VerifyEquipmentAndSkillDisplayData();
            VerifyConsumables();
            VerifySkillSaleProtection();
            Debug.Log("Conn runtime core rule verification passed.");
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
            Expect(session.Combat.Enemy.Id == MonsterCatalog.TestGuardId, "P1 flow combat must resolve the test guard monster.");
            Expect(session.Combat.Enemy.MaxHp == MonsterCatalog.Find(MonsterCatalog.TestGuardId).MaxHp, "P1 flow combat must use monster HP data.");
            Expect(session.Combat.EnemyAttackPower == MonsterCatalog.Find(MonsterCatalog.TestGuardId).AttackPower, "P1 flow combat must use monster attack data.");
            Expect(session.Combat.XpReward == EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward, "P1 flow combat must use encounter XP reward data.");

            QuestRuntimeService.CompleteTarget(session, session.Combat.FieldMonsterStateKey);
            session.Combat.Active = false;
            session.Mode = GameMode.Dungeon;
            Expect(session.Quest.TargetDefeated, "P1 flow combat win must mark quest target defeated.");
            Expect(session.Quest.ReturnAvailable, "P1 flow combat win must enable return.");
            Expect(FieldMonsterRuntimeService.IsDefeated(session, "field_monster_test_guard"), "P1 flow combat win must clear field monster.");
            session.Player.GainXp(session.Combat.XpReward);
            Expect(session.Player.Xp >= EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward, "P1 flow combat win must grant XP.");

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
            Expect(session.Combat.LastMessage.Contains("Enemy deals 2"), "Combat message must report reduced enemy damage.");
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
            Expect(SkillCatalog.Find(session.Skills.EquippedSkillIds[0]) != null, "Equipped skill face must resolve to display data.");
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

        private static void VerifyRuntimeNotice()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            RuntimeNoticeService.Set(session, "notice check");

            Expect(session.LastNotice == "notice check", "Runtime notices must be stored on the session for HUD display.");
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
            Expect(loaded.Skills.NextEditFaceIndex == source.Skills.NextEditFaceIndex, "Save contract must preserve next skill edit face.");
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

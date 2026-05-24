using System;
using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Quests;
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
            VerifyEquipmentDiceRules();
            VerifyEquipmentLoadoutToggle();
            VerifyNewGameState();
            VerifyDiceSkillEffects();
            VerifySkillFaceCycling();
            VerifyCombatHandoffStateKey();
            VerifyQuestBoardFlow();
            VerifyQuestReturnRewardSummary();
            VerifyKeepExploringReturnPrompt();
            VerifyTownServices();
            VerifyRuntimeNotice();
            VerifyEquipmentAndSkillDisplayData();
            VerifyConsumables();
            VerifySkillSaleProtection();
            Debug.Log("Conn runtime core rule verification passed.");
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
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", "encounter_alpha", session.Quest.TargetMonsterId);
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

            QuestRuntimeService.AcceptCurrentBoardOffer(session);

            Expect(session.Quest.HasActiveQuest, "Accepting board offer must activate a quest.");
            Expect(session.Quest.ActiveQuestId == offer.QuestId, "Accepted quest must match current board offer.");
            Expect(session.Quest.GoldReward == offer.GoldReward, "Accepted quest must use current board reward.");
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

            Expect(TownServiceRuntimeService.Train(session, 5), "Trainer service must spend gold and train player.");
            Expect(session.Gold == goldBefore - 5, "Trainer service must spend configured gold.");
            Expect(session.Player.MaxHp == 22, "Trainer service must increase max HP.");
            Expect(session.Player.Hp == session.Player.MaxHp, "Trainer service must heal to trained max HP.");
            Expect(TownServiceRuntimeService.ScholarHint(session).Contains("board offer"), "Scholar must provide current board information without an active quest.");
        }

        private static void VerifyRuntimeNotice()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            RuntimeNoticeService.Set(session, "notice check");

            Expect(session.LastNotice == "notice check", "Runtime notices must be stored on the session for HUD display.");
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

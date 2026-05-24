using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Quests;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Combat;
using Conn.Runtime.Equipment;
using Conn.Runtime.Inventory;
using Conn.Runtime.Session;
using Conn.Runtime.Skills;
using Conn.Runtime.World;
using NUnit.Framework;

namespace Conn.Tests.EditMode
{
    public sealed class RuntimeCoreRulesTests
    {
        [Test]
        public void EquipmentDiceCountMatchesChapterOneRules()
        {
            var equipment = new PlayerEquipmentState
            {
                EquippedWeaponId = string.Empty,
                EquippedShieldId = string.Empty
            };
            Assert.That(equipment.DiceCount, Is.EqualTo(2));

            equipment.Equip(EquipmentCatalog.RustySwordId);
            Assert.That(equipment.DiceCount, Is.EqualTo(4));
            Assert.That(equipment.DefenseBonus, Is.EqualTo(0));

            equipment.Equip(EquipmentCatalog.IronShieldId);
            Assert.That(equipment.DiceCount, Is.EqualTo(3));
            Assert.That(equipment.DefenseBonus, Is.EqualTo(1));

            equipment.Equip(EquipmentCatalog.GreatAxeId);
            Assert.That(equipment.DiceCount, Is.EqualTo(5));
            Assert.That(equipment.EquippedShieldId, Is.Empty);
        }

        [Test]
        public void NewGameStartsWithQuestReadyTownLoadout()
        {
            var session = new GameSessionState();

            session.StartNewGame();

            Assert.That(session.Mode, Is.EqualTo(GameMode.Town));
            Assert.That(session.Gold, Is.EqualTo(25));
            Assert.That(session.Inventory.HasItem(EquipmentCatalog.RustySwordId), Is.True);
            Assert.That(session.Equipment.EquippedWeaponId, Is.EqualTo(EquipmentCatalog.RustySwordId));
            Assert.That(session.Skills.HasSkill(SkillCatalog.SlashId), Is.True);
            Assert.That(session.Skills.EquippedSkillIds[0], Is.EqualTo(SkillCatalog.SlashId));
            Assert.That(QuestRuntimeService.CurrentBoardOffer(session)?.QuestId, Is.EqualTo(QuestCatalog.TestHuntId));

            QuestRuntimeService.RerollBoard(session);

            Assert.That(QuestRuntimeService.CurrentBoardOffer(session)?.QuestId, Is.EqualTo(QuestCatalog.GuardPatrolId));
        }

        [Test]
        public void OwnedLoadoutToggleSwitchesDiceRules()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Inventory.AddItem(EquipmentCatalog.IronShieldId);
            session.Inventory.AddItem(EquipmentCatalog.GreatAxeId);

            Assert.That(EquipmentRuntimeService.ToggleOwnedLoadout(session), Is.True);
            Assert.That(session.Equipment.WeaponGrip, Is.EqualTo(WeaponGrip.TwoHand));
            Assert.That(session.Equipment.DiceCount, Is.EqualTo(5));

            Assert.That(EquipmentRuntimeService.ToggleOwnedLoadout(session), Is.True);
            Assert.That(session.Equipment.WeaponGrip, Is.EqualTo(WeaponGrip.OneHandAndShield));
            Assert.That(session.Equipment.DiceCount, Is.EqualTo(3));
        }

        [Test]
        public void DiceResolutionAppliesAttackGuardAndHealEffects()
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

            Assert.That(session.Combat.Enemy.Hp, Is.EqualTo(10));
            Assert.That(session.Player.Hp, Is.EqualTo(17));
            Assert.That(session.Combat.Player.Hp, Is.EqualTo(17));
            Assert.That(session.Combat.Round, Is.EqualTo(2));
            Assert.That(session.Combat.DiceFaces[0].Cooldown, Is.EqualTo(1));
            Assert.That(session.Combat.LastMessage, Does.Contain("2 damage"));
            Assert.That(session.Combat.LastMessage, Does.Contain("2 guard"));
            Assert.That(session.Combat.LastMessage, Does.Contain("3 heal"));
            Assert.That(session.Combat.LastMessage, Does.Contain("Enemy deals 2"));
        }

        [Test]
        public void EquippedSkillCannotBeSoldWithoutLooseCopy()
        {
            var skills = new SkillInventoryState();

            skills.AddSkill(SkillCatalog.SlashId);
            skills.EquipFirstOpenFace(SkillCatalog.SlashId, 4);

            Assert.That(skills.RemoveLooseSkill(SkillCatalog.SlashId), Is.False);

            skills.AddSkill(SkillCatalog.SlashId);

            Assert.That(skills.RemoveLooseSkill(SkillCatalog.SlashId), Is.True);
            Assert.That(skills.CountOwned(SkillCatalog.SlashId), Is.EqualTo(1));
            Assert.That(skills.CountEquipped(SkillCatalog.SlashId), Is.EqualTo(1));
        }

        [Test]
        public void SkillFaceCycleMovesThroughOwnedSkills()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.AddSkill(SkillCatalog.MendId);

            Assert.That(SkillRuntimeService.CycleNextEditFace(session), Is.True);
            Assert.That(session.Skills.EquippedSkillIds[0], Is.EqualTo(SkillCatalog.GuardId));
            Assert.That(session.Skills.NextEditFaceIndex, Is.EqualTo(1));

            Assert.That(SkillRuntimeService.CycleNextEditFace(session), Is.True);
            Assert.That(session.Skills.EquippedSkillIds[1], Is.EqualTo(SkillCatalog.SlashId));
            Assert.That(session.Skills.NextEditFaceIndex, Is.EqualTo(2));
        }

        [Test]
        public void ConsumablePurchaseAndUseUpdatesInventoryGoldAndHp()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var startingGold = session.Gold;

            Assert.That(ConsumableRuntimeService.Buy(session, ConsumableCatalog.MinorPotionId), Is.True);
            Assert.That(session.Gold, Is.EqualTo(startingGold - ConsumableCatalog.Find(ConsumableCatalog.MinorPotionId).BuyPrice));
            Assert.That(ConsumableRuntimeService.Count(session, ConsumableCatalog.MinorPotionId), Is.EqualTo(1));

            session.Player.Damage(8);

            Assert.That(ConsumableRuntimeService.Use(session, ConsumableCatalog.MinorPotionId), Is.True);
            Assert.That(session.Player.Hp, Is.EqualTo(18));
            Assert.That(ConsumableRuntimeService.Count(session, ConsumableCatalog.MinorPotionId), Is.EqualTo(0));
            Assert.That(ConsumableRuntimeService.Use(session, ConsumableCatalog.MinorPotionId), Is.False);
        }

        [Test]
        public void CombatSessionRemembersFieldMonsterHandoffKey()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", "encounter_alpha", session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");

            CombatRuntimeService.StartTestCombat(session);

            Assert.That(session.Combat.FieldMonsterStateKey, Is.EqualTo("field_monster_alpha"));

            FieldMonsterRuntimeService.MarkIdle(session, session.Combat.FieldMonsterStateKey);

            Assert.That(FieldMonsterRuntimeService.FindCombatHandoff(session), Is.Null);
        }

        [Test]
        public void BoardOfferCanActivateQuest()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var offer = QuestRuntimeService.CurrentBoardOffer(session);

            QuestRuntimeService.AcceptCurrentBoardOffer(session);

            Assert.That(session.Quest.HasActiveQuest, Is.True);
            Assert.That(session.Quest.ActiveQuestId, Is.EqualTo(offer.QuestId));
            Assert.That(session.Quest.GoldReward, Is.EqualTo(offer.GoldReward));
        }

        [Test]
        public void ReturnToTownRecordsRewardSummary()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            var title = session.Quest.ActiveQuestTitle;
            var reward = session.Quest.GoldReward;
            var goldBefore = session.Gold;

            QuestRuntimeService.CompleteReturn(session);

            Assert.That(session.Quest.HasActiveQuest, Is.False);
            Assert.That(session.Gold, Is.EqualTo(goldBefore + reward));
            Assert.That(session.Quest.LastCompletedQuestTitle, Is.EqualTo(title));
            Assert.That(session.Quest.LastGoldReward, Is.EqualTo(reward));
        }

        [Test]
        public void KeepExploringDismissesReturnPromptButKeepsReturnAvailable()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            QuestRuntimeService.CompleteTarget(session, "field_monster_alpha");

            QuestRuntimeService.KeepExploring(session);

            Assert.That(session.Quest.ReturnPromptSeen, Is.True);
            Assert.That(session.Quest.ReturnAvailable, Is.True);
        }
    }
}

using Conn.Core.Equipment;
using Conn.Core.Quests;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Combat;
using Conn.Runtime.Session;
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
    }
}

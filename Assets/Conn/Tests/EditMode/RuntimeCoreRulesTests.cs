using Conn.Core.Combat;
using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Quests;
using Conn.Core.Scenes;
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
        public void ArmorPiecesEquipIntoDocumentedSlots()
        {
            var equipment = new PlayerEquipmentState();

            equipment.Equip(EquipmentCatalog.LeatherCapId);
            equipment.Equip(EquipmentCatalog.PaddedVestId);
            equipment.Equip(EquipmentCatalog.TravelerGlovesId);
            equipment.Equip(EquipmentCatalog.ReinforcedPantsId);
            equipment.Equip(EquipmentCatalog.WornBootsId);

            Assert.That(equipment.EquippedHeadId, Is.EqualTo(EquipmentCatalog.LeatherCapId));
            Assert.That(equipment.EquippedChestId, Is.EqualTo(EquipmentCatalog.PaddedVestId));
            Assert.That(equipment.EquippedArmsId, Is.EqualTo(EquipmentCatalog.TravelerGlovesId));
            Assert.That(equipment.EquippedLegsId, Is.EqualTo(EquipmentCatalog.ReinforcedPantsId));
            Assert.That(equipment.EquippedFeetId, Is.EqualTo(EquipmentCatalog.WornBootsId));
            Assert.That(equipment.IsEquipped(EquipmentCatalog.PaddedVestId), Is.True);
            Assert.That(equipment.ArmorValue, Is.EqualTo(6));
            Assert.That(equipment.DefenseBonus, Is.EqualTo(6));
        }

        [Test]
        public void EquipmentComparisonLineReportsCurrentSlotAndStatDelta()
        {
            var equipment = new PlayerEquipmentState();

            Assert.That(equipment.ComparisonLineFor(EquipmentCatalog.LeatherCapId), Is.EqualTo("Head: None -> Leather Cap Defense +1"));

            equipment.Equip(EquipmentCatalog.LeatherCapId);
            equipment.Equip(EquipmentCatalog.IronShieldId);

            Assert.That(equipment.ComparisonLineFor(EquipmentCatalog.LeatherCapId), Is.EqualTo("Head: Leather Cap -> Leather Cap"));
            Assert.That(equipment.ComparisonLineFor(EquipmentCatalog.GreatAxeId), Is.EqualTo("Weapon: Rusty Sword -> Great Axe Defense -1"));
        }

        [Test]
        public void P1VerticalSliceRuntimeStateFlowCloses()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var boardOffer = QuestRuntimeService.CurrentBoardOffer(session);

            QuestRuntimeService.AcceptCurrentBoardOffer(session);
            session.Mode = GameMode.Dungeon;
            FieldMonsterRuntimeService.Register(session, "field_monster_test_guard", "placement_test_guard", "encounter_test_guard", session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_test_guard");
            session.Mode = GameMode.Combat;
            CombatRuntimeService.StartTestCombat(session);

            Assert.That(session.Combat.Active, Is.True);
            Assert.That(session.Combat.FieldMonsterStateKey, Is.EqualTo("field_monster_test_guard"));
            Assert.That(session.Quest.TargetMonsterId, Is.EqualTo(boardOffer.TargetMonsterId));
            Assert.That(session.Combat.EncounterId, Is.EqualTo(EncounterCatalog.TestGuardId));
            Assert.That(session.Combat.Enemy.Id, Is.EqualTo(MonsterCatalog.TestGuardId));
            Assert.That(session.Combat.Enemy.MaxHp, Is.EqualTo(MonsterCatalog.Find(MonsterCatalog.TestGuardId).MaxHp));
            Assert.That(session.Combat.EnemyAttackPower, Is.EqualTo(MonsterCatalog.Find(MonsterCatalog.TestGuardId).EnemyActionPower));
            Assert.That(session.Combat.EnemyActionName, Is.EqualTo(MonsterCatalog.Find(MonsterCatalog.TestGuardId).EnemyActionName));
            Assert.That(session.Combat.XpReward, Is.EqualTo(EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward));

            QuestRuntimeService.CompleteTarget(session, session.Combat.FieldMonsterStateKey);
            session.Combat.Active = false;
            session.Mode = GameMode.Dungeon;

            Assert.That(session.Quest.ReturnAvailable, Is.True);
            Assert.That(FieldMonsterRuntimeService.IsDefeated(session, "field_monster_test_guard"), Is.True);
            session.Player.GainXp(session.Combat.XpReward);
            Assert.That(session.Player.Xp, Is.GreaterThanOrEqualTo(EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward));

            var goldBeforeReturn = session.Gold;
            var reward = session.Quest.GoldReward;
            QuestRuntimeService.CompleteReturn(session);
            session.Mode = GameMode.Town;

            Assert.That(session.Quest.HasActiveQuest, Is.False);
            Assert.That(session.Gold, Is.EqualTo(goldBeforeReturn + reward));
            Assert.That(session.Quest.LastGoldReward, Is.EqualTo(reward));
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
        public void DirectEquipmentChangesKeepLoadoutAndSkillFacesConsistent()
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

            Assert.That(EquipmentRuntimeService.TryEquip(session, EquipmentCatalog.GreatAxeId), Is.True);
            Assert.That(session.Equipment.WeaponGrip, Is.EqualTo(WeaponGrip.TwoHand));
            Assert.That(session.Skills.EquippedSkillIds.Count, Is.LessThanOrEqualTo(5));

            Assert.That(EquipmentRuntimeService.TryEquip(session, EquipmentCatalog.IronShieldId), Is.True);
            Assert.That(session.Equipment.EquippedWeaponId, Is.EqualTo(EquipmentCatalog.RustySwordId));
            Assert.That(session.Equipment.WeaponGrip, Is.EqualTo(WeaponGrip.OneHandAndShield));
            Assert.That(session.Skills.EquippedSkillIds.Count, Is.EqualTo(3));
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
            Assert.That(session.Combat.LastMessage, Does.Contain("Test Gate Guard uses Halberd thrust for 2 damage"));
            Assert.That(session.Combat.LastMessage, Does.Contain("4 power"));
            Assert.That(session.Combat.LastMessage, Does.Contain("2 blocked"));
        }

        [Test]
        public void EnemyTurnUsesAttackPowerFromMonsterActionData()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var monster = MonsterCatalog.Find(MonsterCatalog.TestGuardId);

            CombatRuntimeService.StartTestCombat(session);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Combat.EnemyAttackPower, Is.EqualTo(monster.EnemyActionPower));
            Assert.That(session.Combat.EnemyActionName, Is.EqualTo(monster.EnemyActionName));
            Assert.That(session.Player.Hp, Is.EqualTo(session.Player.MaxHp - monster.EnemyActionPower));
        }

        [Test]
        public void EnemyTurnDamageIsReducedByGuard()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.GuardId;

            CombatRuntimeService.StartTestCombat(session);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Player.Hp, Is.EqualTo(session.Player.MaxHp - 2));
            Assert.That(session.Combat.LastMessage, Does.Contain("2 blocked"));
        }

        [Test]
        public void EnemyTurnDamageHasMinimumOneAfterGuardAndDefense()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Inventory.AddItem(EquipmentCatalog.IronShieldId);
            Assert.That(EquipmentRuntimeService.TryEquip(session, EquipmentCatalog.IronShieldId), Is.True);
            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.GuardId;
            session.Skills.EquippedSkillIds[1] = SkillCatalog.GuardId;

            CombatRuntimeService.StartTestCombat(session);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ToggleDieSelection(session, 1);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Player.Hp, Is.EqualTo(session.Player.MaxHp - 1));
            Assert.That(session.Combat.LastMessage, Does.Contain("1 damage"));
            Assert.That(session.Combat.LastMessage, Does.Contain("3 blocked"));
        }

        [Test]
        public void EnemyTurnMessageNamesActionPowerAndBlockedDamage()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.GuardId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.GuardId;

            CombatRuntimeService.StartTestCombat(session);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Combat.LastMessage, Does.Contain("Test Gate Guard uses Halberd thrust"));
            Assert.That(session.Combat.LastMessage, Does.Contain("4 power"));
            Assert.That(session.Combat.LastMessage, Does.Contain("2 blocked"));
        }

        [Test]
        public void FocusStrikeAppliesBleedThatTicksAndExpires()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.FocusStrikeId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.FocusStrikeId;

            CombatRuntimeService.StartTestCombat(session);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Combat.Enemy.Hp, Is.EqualTo(7));
            Assert.That(session.Combat.Enemy.StatusEffects, Has.Count.EqualTo(1));
            Assert.That(session.Combat.Enemy.StatusEffects[0].Kind, Is.EqualTo(CombatStatusEffectKind.Bleed));
            Assert.That(session.Combat.Enemy.StatusEffects[0].RemainingTurns, Is.EqualTo(1));
            Assert.That(session.Combat.LastMessage, Does.Contain("Focus Strike applied Bleed"));
            Assert.That(session.Combat.LastMessage, Does.Contain("Test Gate Guard suffers 1 Bleed damage"));

            CombatRuntimeService.ToggleDieSelection(session, 1);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Combat.Enemy.Hp, Is.EqualTo(5));
            Assert.That(session.Combat.Enemy.StatusEffects, Is.Empty);
            Assert.That(session.Combat.LastMessage, Does.Contain("Bleed ended"));
        }

        [Test]
        public void StatusDamageCanDefeatEnemyAndPreservesCombatLog()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            session.Skills.AddSkill(SkillCatalog.FocusStrikeId);
            session.Skills.EquippedSkillIds[0] = SkillCatalog.FocusStrikeId;

            CombatRuntimeService.StartTestCombat(session);
            session.Combat.Enemy.Setup(MonsterCatalog.TestGuardId, "Bleeding Target", 5);
            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Combat.Active, Is.False);
            Assert.That(session.Combat.Enemy.IsDead, Is.True);
            Assert.That(session.Combat.LastMessage, Does.Contain("Bleeding Target suffers 1 Bleed damage"));
            Assert.That(session.Combat.LastMessage, Does.Contain("Enemy defeated"));
            Assert.That(session.Player.Xp, Is.EqualTo(EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward));
        }

        [Test]
        public void CombatContentDefinitionsLinkQuestEncounterAndMonster()
        {
            var monster = MonsterCatalog.Find(MonsterCatalog.TestGuardId);
            var encounter = EncounterCatalog.Find(EncounterCatalog.TestGuardId);
            var quest = QuestCatalog.Find(QuestCatalog.TestHuntId);

            Assert.That(monster, Is.Not.Null);
            Assert.That(encounter, Is.Not.Null);
            Assert.That(quest, Is.Not.Null);
            Assert.That(monster.MaxHp, Is.EqualTo(12));
            Assert.That(monster.AttackPower, Is.EqualTo(4));
            Assert.That(monster.EnemyActionName, Is.EqualTo("Halberd thrust"));
            Assert.That(monster.EnemyActionPower, Is.EqualTo(4));
            Assert.That(encounter.MonsterId, Is.EqualTo(monster.MonsterId));
            Assert.That(encounter.XpReward, Is.EqualTo(5));
            Assert.That(quest.TargetMonsterId, Is.EqualTo(monster.MonsterId));
        }

        [Test]
        public void CombatWinGrantsXpAndCompletesQuestTarget()
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

            Assert.That(session.Player.Xp, Is.EqualTo(EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward));
            Assert.That(session.Quest.TargetDefeated, Is.True);
            Assert.That(session.LastNotice, Does.Contain($"Gained {EncounterCatalog.Find(EncounterCatalog.TestGuardId).XpReward} XP"));
        }

        [Test]
        public void CombatFleeReturnsToDungeonWithoutDefeatingMonster()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", EncounterCatalog.TestGuardId, session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");
            CombatRuntimeService.StartTestCombat(session);

            CombatRuntimeService.Flee(session);

            Assert.That(session.Mode, Is.EqualTo(GameMode.Dungeon));
            Assert.That(session.Combat.Active, Is.False);
            Assert.That(FieldMonsterRuntimeService.FindCombatHandoff(session), Is.Null);
            Assert.That(FieldMonsterRuntimeService.IsDefeated(session, "field_monster_alpha"), Is.False);
        }

        [Test]
        public void CombatDeathRoutesToEndingState()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            CombatRuntimeService.StartTestCombat(session);

            CombatRuntimeService.Die(session);

            Assert.That(session.Mode, Is.EqualTo(GameMode.Ending));
            Assert.That(session.Combat.Active, Is.False);
            Assert.That(session.LastNotice, Does.Contain("died"));
        }

        [Test]
        public void FieldMonsterExpeditionStatusReportsRuntimeProgress()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);

            Assert.That(FieldMonsterRuntimeService.ExpeditionStatus(session), Does.Contain("none registered"));

            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", EncounterCatalog.TestGuardId, session.Quest.TargetMonsterId);

            Assert.That(FieldMonsterRuntimeService.CountActive(session), Is.EqualTo(1));
            Assert.That(FieldMonsterRuntimeService.ExpeditionStatus(session), Does.Contain("1 active"));

            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");

            Assert.That(FieldMonsterRuntimeService.ExpeditionStatus(session), Does.Contain(session.Quest.TargetMonsterId));

            QuestRuntimeService.CompleteTarget(session, "field_monster_alpha");

            Assert.That(FieldMonsterRuntimeService.CountActive(session), Is.EqualTo(0));
            Assert.That(FieldMonsterRuntimeService.CountDefeated(session), Is.EqualTo(1));
            Assert.That(FieldMonsterRuntimeService.ExpeditionStatus(session), Does.Contain("Target defeated"));
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
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", EncounterCatalog.TestGuardId, session.Quest.TargetMonsterId);
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

            QuestRuntimeService.RerollBoard(session);
            var rerolledOffer = QuestRuntimeService.CurrentBoardOffer(session);

            Assert.That(rerolledOffer, Is.Not.Null);
            Assert.That(rerolledOffer.QuestId, Is.Not.EqualTo(offer.QuestId));
            Assert.That(session.Quest.BoardRerollCount, Is.EqualTo(1));

            QuestRuntimeService.AcceptCurrentBoardOffer(session);

            Assert.That(session.Quest.HasActiveQuest, Is.True);
            Assert.That(session.Quest.ActiveQuestId, Is.EqualTo(rerolledOffer.QuestId));
            Assert.That(session.Quest.GoldReward, Is.EqualTo(rerolledOffer.GoldReward));
        }

        [Test]
        public void TownPanelsAreMutuallyExclusive()
        {
            TownShopPanelState.Close();
            TownQuestBoardPanelState.Close();

            TownShopPanelState.Open(TownShopPanelKind.Blacksmith);

            Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.Blacksmith));
            Assert.That(TownQuestBoardPanelState.IsOpen, Is.False);

            TownQuestBoardPanelState.Open();

            Assert.That(TownQuestBoardPanelState.IsOpen, Is.True);
            Assert.That(TownShopPanelState.Current, Is.EqualTo(TownShopPanelKind.None));

            TownQuestBoardPanelState.Close();
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

        [Test]
        public void TrainerAndScholarProvideTownServices()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var goldBefore = session.Gold;
            session.Player.GainXp(5);
            var xpBefore = session.Player.Xp;

            Assert.That(TownServiceRuntimeService.Train(session, 5), Is.True);
            Assert.That(session.Gold, Is.EqualTo(goldBefore));
            Assert.That(session.Player.Xp, Is.EqualTo(xpBefore - 5));
            Assert.That(session.Player.MaxHp, Is.EqualTo(22));
            Assert.That(session.Player.Hp, Is.EqualTo(session.Player.MaxHp));
            Assert.That(TownServiceRuntimeService.ScholarHint(session), Does.Contain("board offer"));
        }

        [Test]
        public void ShopServicesBuyEquipAndSellInventoryItems()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            Assert.That(EquipmentShopRuntimeService.CanBuy(session, EquipmentCatalog.IronShieldId), Is.True);
            Assert.That(EquipmentShopRuntimeService.BuyAndEquip(session, EquipmentCatalog.IronShieldId), Is.True);
            Assert.That(session.Inventory.HasItem(EquipmentCatalog.IronShieldId), Is.True);
            Assert.That(session.Equipment.EquippedShieldId, Is.EqualTo(EquipmentCatalog.IronShieldId));
            Assert.That(EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.IronShieldId), Is.False);
            Assert.That(EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.RustySwordId), Is.False);

            Assert.That(EquipmentShopRuntimeService.CanBuy(session, EquipmentCatalog.LeatherCapId), Is.True);
            Assert.That(EquipmentShopRuntimeService.BuyAndEquip(session, EquipmentCatalog.LeatherCapId), Is.True);
            Assert.That(session.Equipment.EquippedHeadId, Is.EqualTo(EquipmentCatalog.LeatherCapId));
            Assert.That(EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.LeatherCapId), Is.False);

            session.Inventory.AddItem(EquipmentCatalog.GreatAxeId);

            Assert.That(EquipmentShopRuntimeService.Sell(session, EquipmentCatalog.GreatAxeId), Is.True);
            Assert.That(session.Inventory.HasItem(EquipmentCatalog.GreatAxeId), Is.False);

            session.Inventory.AddItem(EquipmentCatalog.PaddedVestId);

            Assert.That(EquipmentShopRuntimeService.CanSell(session, EquipmentCatalog.PaddedVestId), Is.True);
            Assert.That(EquipmentShopRuntimeService.Sell(session, EquipmentCatalog.PaddedVestId), Is.True);
            Assert.That(session.Inventory.HasItem(EquipmentCatalog.PaddedVestId), Is.False);
        }

        [Test]
        public void SkillShopSupportsDuplicateBuyAndLooseSale()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            var goldBefore = session.Gold;

            Assert.That(SkillShopRuntimeService.BuyAndEquip(session, SkillCatalog.GuardId), Is.True);
            Assert.That(session.Gold, Is.EqualTo(goldBefore - SkillCatalog.Find(SkillCatalog.GuardId).BuyPrice));
            Assert.That(session.Skills.HasSkill(SkillCatalog.GuardId), Is.True);
            Assert.That(SkillShopRuntimeService.CanSellLoose(session, SkillCatalog.GuardId), Is.False);

            Assert.That(SkillShopRuntimeService.BuyAndEquip(session, SkillCatalog.GuardId), Is.True);
            Assert.That(SkillShopRuntimeService.CanSellLoose(session, SkillCatalog.GuardId), Is.True);
            Assert.That(SkillShopRuntimeService.SellLoose(session, SkillCatalog.GuardId), Is.True);
        }

        [Test]
        public void SkillMerchantStockRefreshesDeterministicallyButCatalogPurchasesStillWork()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            var firstStock = SkillShopRuntimeService.SkillMerchantStock(session);

            Assert.That(firstStock, Has.Length.EqualTo(SkillShopRuntimeService.SkillMerchantStockSize));
            Assert.That(firstStock[0], Is.EqualTo(SkillCatalog.GuardId));
            Assert.That(firstStock[1], Is.EqualTo(SkillCatalog.FocusStrikeId));
            Assert.That(SkillShopRuntimeService.IsSkillMerchantStocked(session, SkillCatalog.MendId), Is.False);

            SkillShopRuntimeService.RefreshSkillMerchantStock(session);
            var refreshedStock = SkillShopRuntimeService.SkillMerchantStock(session);

            Assert.That(session.Shop.SkillMerchantRefreshIndex, Is.EqualTo(1));
            Assert.That(refreshedStock[0], Is.EqualTo(SkillCatalog.FocusStrikeId));
            Assert.That(refreshedStock[1], Is.EqualTo(SkillCatalog.MendId));
            Assert.That(SkillShopRuntimeService.BuyAndEquip(session, SkillCatalog.GuardId), Is.True);
        }

        [Test]
        public void RuntimeNoticeStoresLastMessageOnSession()
        {
            var session = new GameSessionState();
            session.StartNewGame();

            RuntimeNoticeService.Set(session, "notice check");

            Assert.That(session.LastNotice, Is.EqualTo("notice check"));
        }

        [Test]
        public void SaveContractRoundTripPreservesChapterOneState()
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

            Assert.That(loaded.Mode, Is.EqualTo(GameMode.Combat));
            Assert.That(loaded.Player.Xp, Is.EqualTo(7));
            Assert.That(loaded.Gold, Is.EqualTo(42));
            Assert.That(loaded.LastNotice, Is.EqualTo("saved notice"));
            Assert.That(loaded.Equipment.WeaponGrip, Is.EqualTo(WeaponGrip.OneHandAndShield));
            Assert.That(loaded.Equipment.EquippedHeadId, Is.EqualTo(EquipmentCatalog.LeatherCapId));
            Assert.That(loaded.Equipment.EquippedChestId, Is.EqualTo(EquipmentCatalog.PaddedVestId));
            Assert.That(loaded.Equipment.EquippedArmsId, Is.EqualTo(EquipmentCatalog.TravelerGlovesId));
            Assert.That(loaded.Equipment.EquippedLegsId, Is.EqualTo(EquipmentCatalog.ReinforcedPantsId));
            Assert.That(loaded.Equipment.EquippedFeetId, Is.EqualTo(EquipmentCatalog.WornBootsId));
            Assert.That(loaded.Equipment.ArmorValue, Is.EqualTo(6));
            Assert.That(loaded.Equipment.DefenseBonus, Is.EqualTo(7));
            Assert.That(loaded.Skills.NextEditFaceIndex, Is.EqualTo(source.Skills.NextEditFaceIndex));
            Assert.That(loaded.Shop.SkillMerchantRefreshIndex, Is.EqualTo(1));
            Assert.That(loaded.Shop.SkillMerchantStockSkillIds, Is.EqualTo(source.Shop.SkillMerchantStockSkillIds));
            Assert.That(loaded.Quest.ActiveQuestId, Is.EqualTo(QuestCatalog.TestHuntId));
            Assert.That(loaded.Combat.Active, Is.False);
            Assert.That(SaveRuntimeService.SceneForLoadedState(loaded), Is.EqualTo(GameSceneId.Dungeon));

            source.Mode = GameMode.Ending;
            source.Player.Damage(source.Player.MaxHp);
            source.LastNotice = "You died.";
            json = SaveRuntimeService.ToJson(source);
            SaveRuntimeService.OverwriteFromJson(json, loaded);
            loaded.Combat.Clear();

            Assert.That(loaded.Mode, Is.EqualTo(GameMode.Ending));
            Assert.That(loaded.Player.IsDead, Is.True);
            Assert.That(SaveRuntimeService.SceneForLoadedState(loaded), Is.EqualTo(GameSceneId.Ending));
        }
    }
}

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

            QuestRuntimeService.CompleteTarget(session, session.Combat.FieldMonsterStateKey);
            session.Combat.Active = false;
            session.Mode = GameMode.Dungeon;

            Assert.That(session.Quest.ReturnAvailable, Is.True);
            Assert.That(FieldMonsterRuntimeService.IsDefeated(session, "field_monster_test_guard"), Is.True);
            session.Player.GainXp(5);
            Assert.That(session.Player.Xp, Is.GreaterThanOrEqualTo(5));

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
            Assert.That(session.Combat.LastMessage, Does.Contain("Enemy deals 2"));
        }

        [Test]
        public void CombatWinGrantsXpAndCompletesQuestTarget()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", "encounter_alpha", session.Quest.TargetMonsterId);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, "field_monster_alpha");
            CombatRuntimeService.StartTestCombat(session);
            session.Combat.Enemy.Setup(session.Quest.TargetMonsterId, "Test Monster", 1);

            CombatRuntimeService.ToggleDieSelection(session, 0);
            CombatRuntimeService.ResolveSelectedDice(session);

            Assert.That(session.Player.Xp, Is.EqualTo(5));
            Assert.That(session.Quest.TargetDefeated, Is.True);
            Assert.That(session.LastNotice, Does.Contain("Gained 5 XP"));
        }

        [Test]
        public void CombatFleeReturnsToDungeonWithoutDefeatingMonster()
        {
            var session = new GameSessionState();
            session.StartNewGame();
            QuestRuntimeService.AcceptQuest(session, QuestCatalog.TestHuntId);
            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", "encounter_alpha", session.Quest.TargetMonsterId);
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

            FieldMonsterRuntimeService.Register(session, "field_monster_alpha", "placement_alpha", "encounter_alpha", session.Quest.TargetMonsterId);

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

            session.Inventory.AddItem(EquipmentCatalog.GreatAxeId);

            Assert.That(EquipmentShopRuntimeService.Sell(session, EquipmentCatalog.GreatAxeId), Is.True);
            Assert.That(session.Inventory.HasItem(EquipmentCatalog.GreatAxeId), Is.False);
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
            source.Equipment.Equip(EquipmentCatalog.IronShieldId);
            source.Skills.AddSkill(SkillCatalog.GuardId);
            SkillRuntimeService.CycleNextEditFace(source);
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
            Assert.That(loaded.Skills.NextEditFaceIndex, Is.EqualTo(source.Skills.NextEditFaceIndex));
            Assert.That(loaded.Quest.ActiveQuestId, Is.EqualTo(QuestCatalog.TestHuntId));
            Assert.That(loaded.Combat.Active, Is.False);
            Assert.That(SaveRuntimeService.SceneForLoadedState(loaded), Is.EqualTo(GameSceneId.Dungeon));
        }
    }
}

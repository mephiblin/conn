using Conn.Core.Combat;
using Conn.Core.Content;
using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Maps;
using Conn.Core.Quests;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Core.World;
using System;
using System.Collections.Generic;

namespace Conn.Runtime.Content
{
    public static class RuntimeContentDatabase
    {
        private static ContentDatabaseDefinition activeDatabase;
        private static ContentIdRegistry activeRegistry;

        public static bool HasDatabase => activeDatabase != null;
        public static ContentDatabaseDefinition ActiveDatabase => activeDatabase;

        public static void SetActive(ContentDatabaseDefinition database)
        {
            activeDatabase = database;
            activeRegistry = database != null ? database.BuildRegistry() : null;
            PlayerEquipmentState.EquipmentResolver = database != null ? FindEquipment : EquipmentCatalog.Find;
            SkillInventoryState.SkillResolver = database != null ? FindSkill : SkillCatalog.Find;
            GameSessionState.StarterEquipmentIdResolver = database != null
                ? StarterEquipmentId
                : DefaultStarterEquipmentId;
            GameSessionState.StarterSkillIdResolver = database != null
                ? StarterSkillId
                : DefaultStarterSkillId;
        }

        private static string DefaultStarterEquipmentId() => EquipmentCatalog.RustySwordId;

        private static string DefaultStarterSkillId() => SkillCatalog.SlashId;

        private static string StarterEquipmentId()
        {
            var starterEquipmentId = string.IsNullOrWhiteSpace(activeDatabase?.StarterEquipmentId)
                ? EquipmentCatalog.RustySwordId
                : activeDatabase.StarterEquipmentId;
            return activeRegistry?.FindEquipment(starterEquipmentId) != null
                ? starterEquipmentId
                : EquipmentCatalog.RustySwordId;
        }

        private static string StarterSkillId()
        {
            var starterSkillId = string.IsNullOrWhiteSpace(activeDatabase?.StarterSkillId)
                ? SkillCatalog.SlashId
                : activeDatabase.StarterSkillId;
            return activeRegistry?.FindSkill(starterSkillId) != null
                ? starterSkillId
                : SkillCatalog.SlashId;
        }

        public static MonsterDefinition FindMonster(string monsterId)
        {
            var contentMonster = activeRegistry?.FindMonster(monsterId);
            if (contentMonster == null)
            {
                return MonsterCatalog.Find(monsterId);
            }

            var actionName = string.IsNullOrWhiteSpace(contentMonster.Ai)
                ? "Attack"
                : contentMonster.Ai;
            return new MonsterDefinition(
                contentMonster.Id,
                contentMonster.DisplayName,
                contentMonster.MaxHp,
                contentMonster.AttackPower,
                contentMonster.XpReward,
                actionName,
                contentMonster.AttackPower);
        }

        public static FieldMonsterAiProfile FindFieldMonsterAiProfile(string monsterId)
        {
            var profile = activeRegistry?.FindMonster(monsterId)?.FieldAiProfile;
            return profile != null ? profile.Clone() : FieldMonsterAiProfile.Default();
        }

        public static EquipmentItemDefinition FindEquipment(string itemId)
        {
            var contentItem = activeRegistry?.FindEquipment(itemId);
            if (contentItem == null)
            {
                return EquipmentCatalog.Find(itemId);
            }

            return new EquipmentItemDefinition(
                contentItem.Id,
                contentItem.DisplayName,
                EquipmentKindFor(contentItem.Kind),
                contentItem.BuyPrice,
                contentItem.SellPrice,
                contentItem.ArmorValue);
        }

        public static ConsumableItemDefinition FindConsumable(string itemId)
        {
            var contentItem = activeRegistry?.FindItem(itemId);
            if (contentItem == null)
            {
                return ConsumableCatalog.Find(itemId);
            }

            return new ConsumableItemDefinition(
                contentItem.Id,
                contentItem.DisplayName,
                contentItem.BuyPrice,
                contentItem.SellPrice,
                contentItem.HealAmount);
        }

        public static SkillDefinition FindSkill(string skillId)
        {
            var contentSkill = activeRegistry?.FindSkill(skillId);
            if (contentSkill == null)
            {
                return SkillCatalog.Find(skillId);
            }

            return new SkillDefinition(
                contentSkill.Id,
                contentSkill.DisplayName,
                SkillEffectKindFor(contentSkill.EffectKind),
                contentSkill.BuyPrice,
                contentSkill.SellPrice,
                contentSkill.Power,
                contentSkill.SpecialEffectId);
        }

        public static QuestDefinition FindQuest(string questId)
        {
            var contentQuest = activeRegistry?.FindQuest(questId);
            if (contentQuest == null)
            {
                return QuestCatalog.Find(questId);
            }

            return new QuestDefinition(
                contentQuest.Id,
                contentQuest.DisplayName,
                contentQuest.TargetMonsterId,
                contentQuest.GoldReward,
                string.IsNullOrWhiteSpace(contentQuest.MapProfileId) ? string.Empty : contentQuest.MapProfileId,
                MapPlacementKind.QuestTarget,
                contentQuest.TargetEncounterId);
        }

        public static EncounterDefinition FindEncounter(string encounterId)
        {
            var contentEncounter = activeRegistry?.FindEncounter(encounterId);
            if (contentEncounter == null)
            {
                var generated = TryGeneratedSinglePrimaryEncounter(encounterId);
                if (generated != null)
                {
                    return generated;
                }

                return EncounterCatalog.Find(encounterId);
            }

            return new EncounterDefinition(
                contentEncounter.Id,
                contentEncounter.DisplayName,
                contentEncounter.MonsterId,
                contentEncounter.XpReward,
                contentEncounter.RewardId,
                contentEncounter.Pattern,
                ConvertEnemySlots(contentEncounter.EnemySlots));
        }

        private static EncounterDefinition TryGeneratedSinglePrimaryEncounter(string encounterId)
        {
            const string prefix = "generated_single_primary_";
            if (string.IsNullOrWhiteSpace(encounterId) || !encounterId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            var monsterId = encounterId.Substring(prefix.Length);
            if (string.IsNullOrWhiteSpace(monsterId))
            {
                return null;
            }

            return new EncounterDefinition(
                encounterId,
                $"Generated {monsterId}",
                monsterId,
                0,
                string.Empty,
                "single_primary",
                new[]
                {
                    new EncounterEnemySlotDefinition("primary", monsterId, 1, true)
                });
        }

        public static EncounterDefinition FindEncounterForMonster(string monsterId)
        {
            if (!string.IsNullOrWhiteSpace(monsterId) && activeDatabase != null && activeDatabase.Encounters != null)
            {
                for (var i = 0; i < activeDatabase.Encounters.Length; i++)
                {
                    var encounter = activeDatabase.Encounters[i];
                    if (encounter.MonsterId == monsterId)
                    {
                        return FindEncounter(encounter.Id);
                    }
                }
            }

            return EncounterCatalog.FindForMonster(monsterId);
        }

        public static QuestDefinition BoardQuestAt(int offerIndex)
        {
            if (activeDatabase != null && activeDatabase.Quests != null && activeDatabase.Quests.Length > 0)
            {
                var candidateCount = 0;
                for (var i = 0; i < activeDatabase.Quests.Length; i++)
                {
                    if (IsValidBoardQuest(activeDatabase.Quests[i]))
                    {
                        candidateCount++;
                    }
                }

                if (candidateCount > 0)
                {
                    var targetIndex = PositiveModulo(offerIndex, candidateCount);
                    var current = 0;
                    for (var i = 0; i < activeDatabase.Quests.Length; i++)
                    {
                        if (!IsValidBoardQuest(activeDatabase.Quests[i]))
                        {
                            continue;
                        }

                        if (current == targetIndex)
                        {
                            return FindQuest(activeDatabase.Quests[i].Id);
                        }

                        current++;
                    }
                }
            }

            return QuestCatalog.BoardOffer(offerIndex);
        }

        public static ContentVendorDefinition FindVendor(string vendorId)
        {
            return activeRegistry?.FindVendor(vendorId);
        }

        public static ContentNpcDefinition FindNpc(string npcId)
        {
            return activeRegistry?.FindNpc(npcId);
        }

        public static ContentVendorRotationDefinition SelectVendorRotation(string vendorId, int floor, int bossesDefeated)
        {
            var vendor = FindVendor(vendorId);
            if (vendor == null || vendor.Rotations == null || vendor.Rotations.Length == 0)
            {
                return null;
            }

            ContentVendorRotationDefinition selected = null;
            for (var i = 0; i < vendor.Rotations.Length; i++)
            {
                var rotation = vendor.Rotations[i];
                if (floor < rotation.MinFloor || bossesDefeated < rotation.BossesDefeated)
                {
                    continue;
                }

                if (selected == null
                    || rotation.MinFloor > selected.MinFloor
                    || rotation.BossesDefeated > selected.BossesDefeated)
                {
                    selected = rotation;
                }
            }

            return selected;
        }

        public static string[] SkillIdsForVendor(string vendorId, int floor = 1, int bossesDefeated = 0)
        {
            var vendor = FindVendor(vendorId);
            if (vendor == null)
            {
                return Array.Empty<string>();
            }

            var rotation = SelectVendorRotation(vendorId, floor, bossesDefeated);
            var directSkillIds = rotation != null && rotation.StockSkillIds != null && rotation.StockSkillIds.Length > 0
                ? rotation.StockSkillIds
                : vendor.StockSkillIds;
            if (directSkillIds != null && directSkillIds.Length > 0)
            {
                return directSkillIds;
            }

            var catalogIds = rotation != null && rotation.CatalogIds != null && rotation.CatalogIds.Length > 0
                ? rotation.CatalogIds
                : vendor.CatalogIds;
            return SkillIdsInCatalogs(catalogIds);
        }

        public static string[] EquipmentIdsForVendor(string vendorId, int floor = 1, int bossesDefeated = 0)
        {
            var vendor = FindVendor(vendorId);
            if (vendor == null)
            {
                return Array.Empty<string>();
            }

            var rotation = SelectVendorRotation(vendorId, floor, bossesDefeated);
            var stockItemIds = rotation != null && rotation.StockItemIds != null && rotation.StockItemIds.Length > 0
                ? rotation.StockItemIds
                : vendor.StockItemIds;
            if (stockItemIds == null || stockItemIds.Length == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            for (var i = 0; i < stockItemIds.Length; i++)
            {
                if (FindEquipment(stockItemIds[i]) != null)
                {
                    result.Add(stockItemIds[i]);
                }
            }

            return result.ToArray();
        }

        public static string[] ConsumableIdsForVendor(string vendorId, int floor = 1, int bossesDefeated = 0)
        {
            var vendor = FindVendor(vendorId);
            if (vendor == null)
            {
                return Array.Empty<string>();
            }

            var rotation = SelectVendorRotation(vendorId, floor, bossesDefeated);
            var stockItemIds = rotation != null && rotation.StockItemIds != null && rotation.StockItemIds.Length > 0
                ? rotation.StockItemIds
                : vendor.StockItemIds;
            if (stockItemIds == null || stockItemIds.Length == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            for (var i = 0; i < stockItemIds.Length; i++)
            {
                if (FindConsumable(stockItemIds[i]) != null)
                {
                    result.Add(stockItemIds[i]);
                }
            }

            return result.ToArray();
        }

        private static string[] SkillIdsInCatalogs(string[] catalogIds)
        {
            if (activeDatabase == null || catalogIds == null || catalogIds.Length == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            for (var i = 0; i < activeDatabase.Skills.Length; i++)
            {
                var skill = activeDatabase.Skills[i];
                for (var j = 0; j < skill.CatalogIds.Length; j++)
                {
                    if (Contains(catalogIds, skill.CatalogIds[j]))
                    {
                        result.Add(skill.Id);
                        break;
                    }
                }
            }

            return result.ToArray();
        }

        private static bool Contains(string[] values, string value)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidBoardQuest(ContentQuestDefinition quest)
        {
            return quest != null
                && !string.IsNullOrWhiteSpace(quest.TargetMonsterId)
                && !string.IsNullOrWhiteSpace(quest.TargetEncounterId)
                && !string.IsNullOrWhiteSpace(quest.MapProfileId)
                && quest.GoldReward > 0
                && FindEncounter(quest.TargetEncounterId) != null
                && FindMonster(quest.TargetMonsterId) != null;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static EquipmentKind EquipmentKindFor(string kind)
        {
            return kind switch
            {
                "one_hand_weapon" => EquipmentKind.OneHandWeapon,
                "two_hand_weapon" => EquipmentKind.TwoHandWeapon,
                "shield" => EquipmentKind.Shield,
                "head_armor" => EquipmentKind.HeadArmor,
                "chest_armor" => EquipmentKind.ChestArmor,
                "arms_armor" => EquipmentKind.ArmsArmor,
                "legs_armor" => EquipmentKind.LegsArmor,
                "feet_armor" => EquipmentKind.FeetArmor,
                _ => EquipmentKind.OneHandWeapon
            };
        }

        private static SkillEffectKind SkillEffectKindFor(string kind)
        {
            return kind switch
            {
                "attack" => SkillEffectKind.Attack,
                "guard" => SkillEffectKind.Guard,
                "heal" => SkillEffectKind.Heal,
                "support" => SkillEffectKind.Support,
                "buff" => SkillEffectKind.Buff,
                "debuff" => SkillEffectKind.Debuff,
                "lifesteal" => SkillEffectKind.Lifesteal,
                "summon" => SkillEffectKind.Summon,
                _ => SkillEffectKind.Attack
            };
        }

        private static EncounterEnemySlotDefinition[] ConvertEnemySlots(ContentEncounterEnemySlot[] slots)
        {
            if (slots == null || slots.Length == 0)
            {
                return Array.Empty<EncounterEnemySlotDefinition>();
            }

            var result = new EncounterEnemySlotDefinition[slots.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                result[i] = new EncounterEnemySlotDefinition(
                    string.IsNullOrWhiteSpace(slot.SlotId) ? $"slot_{i}" : slot.SlotId,
                    slot.MonsterId,
                    slot.Count <= 0 ? 1 : slot.Count,
                    slot.Primary);
            }

            return result;
        }
    }
}

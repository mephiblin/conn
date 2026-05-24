using Conn.Core.Combat;
using Conn.Core.Content;
using Conn.Core.Equipment;
using Conn.Core.Maps;
using Conn.Core.Quests;
using Conn.Core.Skills;
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
                contentSkill.Power);
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

        public static ContentVendorDefinition FindVendor(string vendorId)
        {
            return activeRegistry?.FindVendor(vendorId);
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
                _ => SkillEffectKind.Attack
            };
        }
    }
}

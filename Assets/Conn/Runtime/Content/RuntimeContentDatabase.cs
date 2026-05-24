using Conn.Core.Combat;
using Conn.Core.Content;
using Conn.Core.Equipment;
using Conn.Core.Maps;
using Conn.Core.Quests;
using Conn.Core.Skills;

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
                MapPlacementKind.QuestTarget);
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

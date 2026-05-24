using System;
using System.Collections.Generic;

namespace Conn.Core.Content
{
    public static class ContentDatabaseValidator
    {
        public static ContentValidationReport Validate(ContentDatabaseDefinition database)
        {
            var report = new ContentValidationReport();
            if (database == null)
            {
                report.Error("Content database asset is missing.");
                return report;
            }

            ContentIdRegistry registry = null;
            try
            {
                registry = database.BuildRegistry();
            }
            catch (Exception exception)
            {
                report.Error(exception.Message);
            }

            if (registry == null)
            {
                return report;
            }

            ValidateItems(database, report);
            ValidateEquipment(database, report);
            ValidateSkills(database, report);
            ValidateMonsters(database, report);
            ValidateQuests(database, registry, report);
            ValidateVendors(database, registry, report);
            ValidateNpcs(database, registry, report);
            return report;
        }

        private static void ValidateItems(ContentDatabaseDefinition database, ContentValidationReport report)
        {
            foreach (var item in database.Items)
            {
                RequireName(item.Id, item.DisplayName, "item", report);
                if (item.Kind == "consumable" && item.HealAmount < 0)
                {
                    report.Error($"Item {item.Id} heal amount must not be negative.");
                }
            }
        }

        private static void ValidateEquipment(ContentDatabaseDefinition database, ContentValidationReport report)
        {
            foreach (var item in database.Equipment)
            {
                RequireName(item.Id, item.DisplayName, "equipment", report);
                if (item.BuyPrice < 0 || item.SellPrice < 0)
                {
                    report.Error($"Equipment {item.Id} prices must not be negative.");
                }
            }
        }

        private static void ValidateSkills(ContentDatabaseDefinition database, ContentValidationReport report)
        {
            foreach (var skill in database.Skills)
            {
                RequireName(skill.Id, skill.DisplayName, "skill", report);
                if (skill.BuyPrice < 0 || skill.SellPrice < 0)
                {
                    report.Error($"Skill {skill.Id} prices must not be negative.");
                }
            }
        }

        private static void ValidateMonsters(ContentDatabaseDefinition database, ContentValidationReport report)
        {
            foreach (var monster in database.Monsters)
            {
                RequireName(monster.Id, monster.DisplayName, "monster", report);
                if (monster.MaxHp <= 0)
                {
                    report.Error($"Monster {monster.Id} max HP must be positive.");
                }

                if (monster.AttackPower <= 0)
                {
                    report.Error($"Monster {monster.Id} attack power must be positive.");
                }
            }
        }

        private static void ValidateQuests(ContentDatabaseDefinition database, ContentIdRegistry registry, ContentValidationReport report)
        {
            foreach (var quest in database.Quests)
            {
                RequireName(quest.Id, quest.DisplayName, "quest", report);
                if (!string.IsNullOrWhiteSpace(quest.TargetMonsterId) && registry.FindMonster(quest.TargetMonsterId) == null)
                {
                    report.Error($"Quest {quest.Id} target monster is missing: {quest.TargetMonsterId}");
                }

                if (quest.GoldReward < 0 || quest.XpReward < 0)
                {
                    report.Error($"Quest {quest.Id} rewards must not be negative.");
                }

                foreach (var reward in quest.RewardItems)
                {
                    if (!registry.ContainsAnyItemLikeId(reward.ItemId))
                    {
                        report.Error($"Quest {quest.Id} reward item is missing: {reward.ItemId}");
                    }

                    if (reward.Quantity <= 0)
                    {
                        report.Error($"Quest {quest.Id} reward item quantity must be positive: {reward.ItemId}");
                    }
                }
            }
        }

        private static void ValidateVendors(ContentDatabaseDefinition database, ContentIdRegistry registry, ContentValidationReport report)
        {
            var skillCatalogs = new HashSet<string>();
            foreach (var skill in database.Skills)
            {
                foreach (var catalogId in skill.CatalogIds)
                {
                    if (!string.IsNullOrWhiteSpace(catalogId))
                    {
                        skillCatalogs.Add(catalogId);
                    }
                }
            }

            foreach (var vendor in database.Vendors)
            {
                if (string.IsNullOrWhiteSpace(vendor.Id))
                {
                    report.Error("Vendor id must not be empty.");
                }

                foreach (var itemId in vendor.StockItemIds)
                {
                    if (!registry.ContainsAnyItemLikeId(itemId))
                    {
                        report.Error($"Vendor {vendor.Id} stock item is missing: {itemId}");
                    }
                }

                foreach (var skillId in vendor.StockSkillIds)
                {
                    if (registry.FindSkill(skillId) == null && !skillCatalogs.Contains(skillId))
                    {
                        report.Error($"Vendor {vendor.Id} stock skill/catalog is missing: {skillId}");
                    }
                }
            }
        }

        private static void ValidateNpcs(ContentDatabaseDefinition database, ContentIdRegistry registry, ContentValidationReport report)
        {
            foreach (var npc in database.Npcs)
            {
                RequireName(npc.Id, npc.DisplayName, "npc", report);
                if (!string.IsNullOrWhiteSpace(npc.VendorId) && registry.FindVendor(npc.VendorId) == null)
                {
                    report.Error($"NPC {npc.Id} vendor is missing: {npc.VendorId}");
                }

                foreach (var questId in npc.QuestIds)
                {
                    if (registry.FindQuest(questId) == null)
                    {
                        report.Warning($"NPC {npc.Id} references quest seed not imported as board quest: {questId}");
                    }
                }
            }
        }

        private static void RequireName(string id, string displayName, string kind, ContentValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                report.Error($"{kind} id must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                report.Error($"{kind} {id} display name must not be empty.");
            }
        }
    }

    public sealed class ContentValidationReport
    {
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool Passed => errors.Count == 0;

        public void Error(string message)
        {
            errors.Add(message);
        }

        public void Warning(string message)
        {
            warnings.Add(message);
        }
    }
}

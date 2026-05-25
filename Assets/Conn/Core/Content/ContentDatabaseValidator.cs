using System;
using System.Collections.Generic;

namespace Conn.Core.Content
{
    public static class ContentDatabaseValidator
    {
        private static readonly HashSet<string> SupportedEquipmentKinds = new HashSet<string>
        {
            "one_hand_weapon",
            "two_hand_weapon",
            "shield",
            "head_armor",
            "chest_armor",
            "arms_armor",
            "legs_armor",
            "feet_armor"
        };

        private static readonly HashSet<string> SupportedSkillEffectKinds = new HashSet<string>
        {
            "attack",
            "guard",
            "heal",
            "support",
            "buff",
            "debuff",
            "lifesteal",
            "summon"
        };

        private static readonly HashSet<string> SupportedSkillSpecialEffectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bleed"
        };

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
            ValidateEncounters(database, registry, report);
            ValidateQuests(database, registry, report);
            ValidateVendors(database, registry, report);
            ValidateNpcs(database, registry, report);
            ValidateStarterLoadout(database, registry, report);
            return report;
        }

        private static void ValidateStarterLoadout(ContentDatabaseDefinition database, ContentIdRegistry registry, ContentValidationReport report)
        {
            if (!string.IsNullOrWhiteSpace(database.StarterEquipmentId))
            {
                var starterEquipment = registry.FindEquipment(database.StarterEquipmentId);
                if (starterEquipment == null)
                {
                    report.Error($"Starter equipment is missing: {database.StarterEquipmentId}");
                }
                else if (starterEquipment.Kind != "one_hand_weapon" && starterEquipment.Kind != "two_hand_weapon")
                {
                    report.Error($"Starter equipment must be a weapon: {database.StarterEquipmentId}");
                }
            }

            if (!string.IsNullOrWhiteSpace(database.StarterSkillId) && registry.FindSkill(database.StarterSkillId) == null)
            {
                report.Error($"Starter skill is missing: {database.StarterSkillId}");
            }
        }

        private static void ValidateItems(ContentDatabaseDefinition database, ContentValidationReport report)
        {
            foreach (var item in database.Items ?? Array.Empty<ContentItemDefinition>())
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
            foreach (var item in database.Equipment ?? Array.Empty<ContentEquipmentDefinition>())
            {
                RequireName(item.Id, item.DisplayName, "equipment", report);
                if (string.IsNullOrWhiteSpace(item.Kind))
                {
                    report.Error($"Equipment {item.Id} kind must not be empty.");
                }
                else if (!SupportedEquipmentKinds.Contains(item.Kind))
                {
                    report.Error($"Equipment {item.Id} kind is not supported by RuntimeContentDatabase: {item.Kind}");
                }

                if (item.BuyPrice < 0 || item.SellPrice < 0)
                {
                    report.Error($"Equipment {item.Id} prices must not be negative.");
                }

                if (item.ArmorValue < 0)
                {
                    report.Error($"Equipment {item.Id} armor value must not be negative.");
                }

                if (item.Generated && string.IsNullOrWhiteSpace(item.RarityId))
                {
                    report.Error($"Generated equipment {item.Id} rarity id must not be empty.");
                }

                if (item.Generated && string.IsNullOrWhiteSpace(item.AffixPoolId))
                {
                    report.Error($"Generated equipment {item.Id} affix pool id must not be empty.");
                }
            }
        }

        private static void ValidateSkills(ContentDatabaseDefinition database, ContentValidationReport report)
        {
            foreach (var skill in database.Skills ?? Array.Empty<ContentSkillDefinition>())
            {
                RequireName(skill.Id, skill.DisplayName, "skill", report);
                if (string.IsNullOrWhiteSpace(skill.EffectKind))
                {
                    report.Error($"Skill {skill.Id} effect kind must not be empty.");
                }
                else if (!SupportedSkillEffectKinds.Contains(skill.EffectKind))
                {
                    report.Error($"Skill {skill.Id} effect kind is not supported by RuntimeContentDatabase: {skill.EffectKind}");
                }

                if (skill.BuyPrice < 0 || skill.SellPrice < 0)
                {
                    report.Error($"Skill {skill.Id} prices must not be negative.");
                }

                if (!string.IsNullOrWhiteSpace(skill.SpecialEffectId) && !SupportedSkillSpecialEffectIds.Contains(skill.SpecialEffectId))
                {
                    report.Error($"Skill {skill.Id} special effect id is not runtime-supported: {skill.SpecialEffectId}");
                }
            }
        }

        private static void ValidateMonsters(ContentDatabaseDefinition database, ContentValidationReport report)
        {
            foreach (var monster in database.Monsters ?? Array.Empty<ContentMonsterDefinition>())
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

                if (monster.XpReward < 0)
                {
                    report.Error($"Monster {monster.Id} XP reward must not be negative.");
                }

                if (monster.FieldAiProfile == null)
                {
                    report.Error($"Monster {monster.Id} field AI profile must not be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(monster.FieldAiProfile.ProfileId))
                {
                    report.Error($"Monster {monster.Id} field AI profile id must not be empty.");
                }

                if (monster.FieldAiProfile.DetectionRadius < 0f)
                {
                    report.Error($"Monster {monster.Id} field AI detection radius must not be negative.");
                }

                if (monster.FieldAiProfile.PatrolRadius < 0f)
                {
                    report.Error($"Monster {monster.Id} field AI patrol radius must not be negative.");
                }

                if (monster.FieldAiProfile.MoveSpeed < 0f)
                {
                    report.Error($"Monster {monster.Id} field AI move speed must not be negative.");
                }

                if (monster.FieldAiProfile.ContactCooldownSeconds < 0f)
                {
                    report.Error($"Monster {monster.Id} field AI contact cooldown must not be negative.");
                }
            }
        }

        private static void ValidateEncounters(ContentDatabaseDefinition database, ContentIdRegistry registry, ContentValidationReport report)
        {
            foreach (var encounter in database.Encounters ?? Array.Empty<ContentEncounterDefinition>())
            {
                RequireName(encounter.Id, encounter.DisplayName, "encounter", report);
                if (string.IsNullOrWhiteSpace(encounter.MonsterId))
                {
                    report.Error($"Encounter {encounter.Id} monster id must not be empty.");
                }
                else if (registry.FindMonster(encounter.MonsterId) == null)
                {
                    report.Error($"Encounter {encounter.Id} monster is missing: {encounter.MonsterId}");
                }

                if (encounter.XpReward < 0)
                {
                    report.Error($"Encounter {encounter.Id} XP reward must not be negative.");
                }

                if (string.IsNullOrWhiteSpace(encounter.Pattern))
                {
                    report.Error($"Encounter {encounter.Id} pattern must not be empty.");
                }

                ValidateEncounterEnemySlots(encounter, registry, report);
            }
        }

        private static void ValidateEncounterEnemySlots(ContentEncounterDefinition encounter, ContentIdRegistry registry, ContentValidationReport report)
        {
            var slots = encounter.EnemySlots ?? Array.Empty<ContentEncounterEnemySlot>();
            if (slots.Length == 0)
            {
                return;
            }

            var hasPrimary = false;
            var slotIds = new HashSet<string>();
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var slotId = string.IsNullOrWhiteSpace(slot.SlotId) ? $"slot_{i}" : slot.SlotId;
                if (!slotIds.Add(slotId))
                {
                    report.Error($"Encounter {encounter.Id} enemy slot id is duplicated: {slotId}");
                }

                if (string.IsNullOrWhiteSpace(slot.MonsterId))
                {
                    report.Error($"Encounter {encounter.Id} enemy slot {slotId} monster id must not be empty.");
                }
                else if (registry.FindMonster(slot.MonsterId) == null)
                {
                    report.Error($"Encounter {encounter.Id} enemy slot {slotId} monster is missing: {slot.MonsterId}");
                }

                if (slot.Count <= 0)
                {
                    report.Error($"Encounter {encounter.Id} enemy slot {slotId} count must be positive.");
                }

                hasPrimary |= slot.Primary || slot.MonsterId == encounter.MonsterId;
            }

            if (!hasPrimary)
            {
                report.Error($"Encounter {encounter.Id} enemy slots must include the primary monster: {encounter.MonsterId}");
            }
        }

        private static void ValidateQuests(ContentDatabaseDefinition database, ContentIdRegistry registry, ContentValidationReport report)
        {
            foreach (var quest in database.Quests ?? Array.Empty<ContentQuestDefinition>())
            {
                RequireName(quest.Id, quest.DisplayName, "quest", report);
                if (string.IsNullOrWhiteSpace(quest.TargetMonsterId))
                {
                    report.Error($"Quest {quest.Id} target monster must not be empty.");
                }
                else if (registry.FindMonster(quest.TargetMonsterId) == null)
                {
                    report.Error($"Quest {quest.Id} target monster is missing: {quest.TargetMonsterId}");
                }

                if (string.IsNullOrWhiteSpace(quest.TargetEncounterId))
                {
                    report.Error($"Quest {quest.Id} target encounter must not be empty.");
                }
                else
                {
                    var encounter = registry.FindEncounter(quest.TargetEncounterId);
                    if (encounter == null)
                    {
                        report.Error($"Quest {quest.Id} target encounter is missing: {quest.TargetEncounterId}");
                    }
                    else if (!string.IsNullOrWhiteSpace(quest.TargetMonsterId) && encounter.MonsterId != quest.TargetMonsterId)
                    {
                        report.Error($"Quest {quest.Id} target encounter monster mismatch: {quest.TargetEncounterId} -> {encounter.MonsterId}, expected {quest.TargetMonsterId}");
                    }
                }

                if (string.IsNullOrWhiteSpace(quest.MapProfileId))
                {
                    report.Error($"Quest {quest.Id} map profile id must not be empty.");
                }

                if (quest.GoldReward < 0 || quest.XpReward < 0)
                {
                    report.Error($"Quest {quest.Id} rewards must not be negative.");
                }

                foreach (var reward in quest.RewardItems ?? Array.Empty<ContentItemStack>())
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
            foreach (var skill in database.Skills ?? Array.Empty<ContentSkillDefinition>())
            {
                foreach (var catalogId in skill.CatalogIds ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(catalogId))
                    {
                        skillCatalogs.Add(catalogId);
                    }
                }
            }

            foreach (var vendor in database.Vendors ?? Array.Empty<ContentVendorDefinition>())
            {
                if (string.IsNullOrWhiteSpace(vendor.Id))
                {
                    report.Error("Vendor id must not be empty.");
                }

                ValidateVendorStock(vendor.Id, vendor.StockItemIds, vendor.StockSkillIds, vendor.CatalogIds, registry, skillCatalogs, report);

                foreach (var rotation in vendor.Rotations ?? Array.Empty<ContentVendorRotationDefinition>())
                {
                    if (rotation.MinFloor < 0)
                    {
                        report.Error($"Vendor {vendor.Id} rotation min floor must not be negative.");
                    }

                    if (rotation.BossesDefeated < 0)
                    {
                        report.Error($"Vendor {vendor.Id} rotation bosses defeated must not be negative.");
                    }

                    if (rotation.GoldCost < 0)
                    {
                        report.Error($"Vendor {vendor.Id} rotation gold cost must not be negative.");
                    }

                    ValidateVendorStock(vendor.Id, rotation.StockItemIds, rotation.StockSkillIds, rotation.CatalogIds, registry, skillCatalogs, report);
                }
            }
        }

        private static void ValidateVendorStock(
            string vendorId,
            IEnumerable<string> itemIds,
            IEnumerable<string> skillIds,
            IEnumerable<string> catalogIds,
            ContentIdRegistry registry,
            HashSet<string> skillCatalogs,
            ContentValidationReport report)
        {
            foreach (var itemId in itemIds ?? Array.Empty<string>())
            {
                if (!registry.ContainsAnyItemLikeId(itemId))
                {
                    report.Error($"Vendor {vendorId} stock item is missing: {itemId}");
                }
            }

            foreach (var skillId in skillIds ?? Array.Empty<string>())
            {
                if (registry.FindSkill(skillId) == null)
                {
                    report.Error($"Vendor {vendorId} stock skill is missing: {skillId}");
                }
            }

            foreach (var catalogId in catalogIds ?? Array.Empty<string>())
            {
                if (!skillCatalogs.Contains(catalogId))
                {
                    report.Error($"Vendor {vendorId} stock catalog is missing: {catalogId}");
                }
            }
        }

        private static void ValidateNpcs(ContentDatabaseDefinition database, ContentIdRegistry registry, ContentValidationReport report)
        {
            foreach (var npc in database.Npcs ?? Array.Empty<ContentNpcDefinition>())
            {
                RequireName(npc.Id, npc.DisplayName, "npc", report);
                if (!string.IsNullOrWhiteSpace(npc.VendorId) && registry.FindVendor(npc.VendorId) == null)
                {
                    report.Error($"NPC {npc.Id} vendor is missing: {npc.VendorId}");
                }

                foreach (var questId in npc.QuestIds ?? Array.Empty<string>())
                {
                    if (registry.FindQuest(questId) == null && !IsNpcQuestSeedId(questId))
                    {
                        report.Warning($"NPC {npc.Id} references quest seed not imported as board quest: {questId}");
                    }
                }
            }
        }

        private static bool IsNpcQuestSeedId(string questId)
        {
            return !string.IsNullOrWhiteSpace(questId) && questId.StartsWith("quest_seed_", StringComparison.Ordinal);
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

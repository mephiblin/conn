using System;
using System.Collections.Generic;
using Conn.Core.Content;
using Conn.Core.Combat;
using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Quests;
using Conn.Core.Skills;
using Conn.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Tools
{
    public static class ContentDatabaseVerifier
    {
        [MenuItem("Conn/Verify Content Database")]
        public static void VerifyContentDatabase()
        {
            VerifyEquipment();
            VerifyConsumables();
            VerifySkills();
            VerifyMonsters();
            VerifyEncounters();
            VerifyQuests();
            VerifyImportedContentDatabase();
            Debug.Log("Conn content database verification passed.");
        }

        private static void VerifyEquipment()
        {
            var ids = new HashSet<string>();
            foreach (var item in EquipmentCatalog.All)
            {
                ExpectId(ids, item.ItemId, "equipment");
                Expect(!string.IsNullOrWhiteSpace(item.DisplayName), $"Equipment {item.ItemId} must have display name.");
                Expect(item.BuyPrice >= 0, $"Equipment {item.ItemId} buy price must be non-negative.");
                Expect(item.SellPrice >= 0, $"Equipment {item.ItemId} sell price must be non-negative.");
                Expect(item.SellPrice <= item.BuyPrice || item.BuyPrice == 0, $"Equipment {item.ItemId} sell price must not exceed buy price.");
                Expect(item.ArmorValue >= 0, $"Equipment {item.ItemId} armor value must be non-negative.");
            }
        }

        private static void VerifyConsumables()
        {
            var ids = new HashSet<string>();
            foreach (var item in ConsumableCatalog.All)
            {
                ExpectId(ids, item.ItemId, "consumable");
                Expect(!string.IsNullOrWhiteSpace(item.DisplayName), $"Consumable {item.ItemId} must have display name.");
                Expect(item.BuyPrice >= 0, $"Consumable {item.ItemId} buy price must be non-negative.");
                Expect(item.SellPrice >= 0, $"Consumable {item.ItemId} sell price must be non-negative.");
                Expect(item.HealAmount > 0, $"Consumable {item.ItemId} heal amount must be positive.");
            }
        }

        private static void VerifySkills()
        {
            var ids = new HashSet<string>();
            foreach (var skill in SkillCatalog.All)
            {
                ExpectId(ids, skill.SkillId, "skill");
                Expect(!string.IsNullOrWhiteSpace(skill.DisplayName), $"Skill {skill.SkillId} must have display name.");
                Expect(skill.BuyPrice >= 0, $"Skill {skill.SkillId} buy price must be non-negative.");
                Expect(skill.SellPrice >= 0, $"Skill {skill.SkillId} sell price must be non-negative.");
                Expect(skill.Power >= 0, $"Skill {skill.SkillId} power must be non-negative.");
            }
        }

        private static void VerifyQuests()
        {
            var ids = new HashSet<string>();
            foreach (var quest in QuestCatalog.AllBoardQuests)
            {
                ExpectId(ids, quest.QuestId, "quest");
                Expect(!string.IsNullOrWhiteSpace(quest.DisplayName), $"Quest {quest.QuestId} must have display name.");
                Expect(!string.IsNullOrWhiteSpace(quest.TargetMonsterId), $"Quest {quest.QuestId} must have target monster id.");
                Expect(MonsterCatalog.Find(quest.TargetMonsterId) != null, $"Quest {quest.QuestId} target monster must exist: {quest.TargetMonsterId}");
                Expect(EncounterCatalog.FindForMonster(quest.TargetMonsterId) != null, $"Quest {quest.QuestId} target monster must have an encounter: {quest.TargetMonsterId}");
                Expect(quest.GoldReward > 0, $"Quest {quest.QuestId} gold reward must be positive.");
            }
        }

        private static void VerifyMonsters()
        {
            var ids = new HashSet<string>();
            foreach (var monster in MonsterCatalog.All)
            {
                ExpectId(ids, monster.MonsterId, "monster");
                Expect(!string.IsNullOrWhiteSpace(monster.DisplayName), $"Monster {monster.MonsterId} must have display name.");
                Expect(monster.MaxHp > 0, $"Monster {monster.MonsterId} max HP must be positive.");
                Expect(monster.AttackPower > 0, $"Monster {monster.MonsterId} attack power must be positive.");
                Expect(!string.IsNullOrWhiteSpace(monster.EnemyActionName), $"Monster {monster.MonsterId} enemy action name must not be empty.");
                Expect(monster.EnemyActionPower > 0, $"Monster {monster.MonsterId} enemy action power must be positive.");
                Expect(monster.XpReward >= 0, $"Monster {monster.MonsterId} XP reward must be non-negative.");
            }
        }

        private static void VerifyEncounters()
        {
            var ids = new HashSet<string>();
            foreach (var encounter in EncounterCatalog.All)
            {
                ExpectId(ids, encounter.EncounterId, "encounter");
                Expect(!string.IsNullOrWhiteSpace(encounter.DisplayName), $"Encounter {encounter.EncounterId} must have display name.");
                Expect(MonsterCatalog.Find(encounter.MonsterId) != null, $"Encounter {encounter.EncounterId} monster must exist: {encounter.MonsterId}");
                Expect(encounter.XpReward >= 0, $"Encounter {encounter.EncounterId} XP reward must be non-negative.");
            }
        }

        private static void VerifyImportedContentDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            if (database == null)
            {
                Debug.LogWarning($"Content database asset not found yet: {LegacyContentJsonImporter.DefaultDatabaseAssetPath}");
                return;
            }

            var report = ContentDatabaseValidator.Validate(database);
            foreach (var warning in report.Warnings)
            {
                Debug.LogWarning(warning);
            }

            foreach (var error in report.Errors)
            {
                Debug.LogError(error);
            }

            Expect(report.Passed, $"Imported content database validation failed with {report.Errors.Count} error(s).");
        }

        private static void ExpectId(HashSet<string> ids, string id, string kind)
        {
            Expect(!string.IsNullOrWhiteSpace(id), $"{kind} id must not be empty.");
            Expect(ids.Add(id), $"Duplicate {kind} id: {id}");
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

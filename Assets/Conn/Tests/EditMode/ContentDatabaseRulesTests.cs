using Conn.Core.Content;
using NUnit.Framework;
using UnityEngine;

namespace Conn.Tests.EditMode
{
    public sealed class ContentDatabaseRulesTests
    {
        [Test]
        public void RegistryLooksUpEveryChapterTwoContentKind()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Items = new[] { new ContentItemDefinition { Id = "bandage", DisplayName = "Bandage", Kind = "consumable" } };
            database.Equipment = new[] { new ContentEquipmentDefinition { Id = "rusty_sword", DisplayName = "Rusty Sword", Kind = "one_hand_weapon" } };
            database.Skills = new[] { new ContentSkillDefinition { Id = "skill_slash", DisplayName = "Slash", EffectKind = "attack" } };
            database.Monsters = new[] { new ContentMonsterDefinition { Id = "monster_guard", DisplayName = "Guard", MaxHp = 10, AttackPower = 3 } };
            database.Quests = new[] { new ContentQuestDefinition { Id = "quest_guard", DisplayName = "Guard Quest", TargetMonsterId = "monster_guard" } };
            database.Vendors = new[] { new ContentVendorDefinition { Id = "vendor_apothecary", ServiceType = "sell_bundle", StockItemIds = new[] { "bandage" } } };
            database.Npcs = new[] { new ContentNpcDefinition { Id = "npc_apothecary", DisplayName = "Apothecary", VendorId = "vendor_apothecary" } };

            var registry = database.BuildRegistry();

            Assert.That(registry.FindItem("bandage"), Is.Not.Null);
            Assert.That(registry.FindEquipment("rusty_sword"), Is.Not.Null);
            Assert.That(registry.FindSkill("skill_slash"), Is.Not.Null);
            Assert.That(registry.FindMonster("monster_guard"), Is.Not.Null);
            Assert.That(registry.FindQuest("quest_guard"), Is.Not.Null);
            Assert.That(registry.FindVendor("vendor_apothecary"), Is.Not.Null);
            Assert.That(registry.FindNpc("npc_apothecary"), Is.Not.Null);
            Assert.That(ContentDatabaseValidator.Validate(database).Passed, Is.True);
        }

        [Test]
        public void ValidatorReportsMissingQuestRewardAndVendorStockIds()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Quests = new[]
            {
                new ContentQuestDefinition
                {
                    Id = "quest_bad",
                    DisplayName = "Bad Quest",
                    TargetMonsterId = "missing_monster",
                    RewardItems = new[] { new ContentItemStack { ItemId = "missing_item", Quantity = 1 } }
                }
            };
            database.Vendors = new[] { new ContentVendorDefinition { Id = "vendor_bad", StockItemIds = new[] { "missing_stock" } } };

            var report = ContentDatabaseValidator.Validate(database);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Errors, Has.Some.Contains("missing_monster"));
            Assert.That(report.Errors, Has.Some.Contains("missing_item"));
            Assert.That(report.Errors, Has.Some.Contains("missing_stock"));
        }
    }
}

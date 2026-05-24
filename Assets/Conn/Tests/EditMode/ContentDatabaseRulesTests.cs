using Conn.Core.Content;
using Conn.Runtime.Content;
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
            database.Skills = new[] { new ContentSkillDefinition { Id = "skill_slash", DisplayName = "Slash", EffectKind = "attack", CatalogIds = new[] { "starter_catalog" } } };
            database.Monsters = new[] { new ContentMonsterDefinition { Id = "monster_guard", DisplayName = "Guard", MaxHp = 10, AttackPower = 3 } };
            database.Quests = new[] { new ContentQuestDefinition { Id = "quest_guard", DisplayName = "Guard Quest", TargetMonsterId = "monster_guard" } };
            database.Vendors = new[]
            {
                new ContentVendorDefinition
                {
                    Id = "vendor_apothecary",
                    ServiceType = "sell_bundle",
                    StockItemIds = new[] { "bandage" },
                    CatalogIds = new[] { "starter_catalog" },
                    Rotations = new[]
                    {
                        new ContentVendorRotationDefinition
                        {
                            MinFloor = 2,
                            BossesDefeated = 1,
                            StockItemIds = new[] { "rusty_sword" },
                            StockSkillIds = new[] { "skill_slash" },
                            CatalogIds = new[] { "starter_catalog" }
                        }
                    }
                }
            };
            database.Npcs = new[] { new ContentNpcDefinition { Id = "npc_apothecary", DisplayName = "Apothecary", VendorId = "vendor_apothecary" } };

            var registry = database.BuildRegistry();

            Assert.That(registry.FindItem("bandage"), Is.Not.Null);
            Assert.That(registry.FindEquipment("rusty_sword"), Is.Not.Null);
            Assert.That(registry.FindSkill("skill_slash"), Is.Not.Null);
            Assert.That(registry.FindMonster("monster_guard"), Is.Not.Null);
            Assert.That(registry.FindQuest("quest_guard"), Is.Not.Null);
            Assert.That(registry.FindVendor("vendor_apothecary"), Is.Not.Null);
            Assert.That(registry.FindNpc("npc_apothecary"), Is.Not.Null);
            Assert.That(registry.TryFindEquipment("rusty_sword", out var equipment), Is.True);
            Assert.That(equipment.DisplayName, Is.EqualTo("Rusty Sword"));
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

        [Test]
        public void ValidatorReportsInvalidVendorRotationContract()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Vendors = new[]
            {
                new ContentVendorDefinition
                {
                    Id = "vendor_bad_rotation",
                    Rotations = new[]
                    {
                        new ContentVendorRotationDefinition
                        {
                            MinFloor = -1,
                            BossesDefeated = -1,
                            GoldCost = -1,
                            StockItemIds = new[] { "missing_item" },
                            StockSkillIds = new[] { "missing_skill" },
                            CatalogIds = new[] { "missing_catalog" }
                        }
                    }
                }
            };

            var report = ContentDatabaseValidator.Validate(database);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Errors, Has.Some.Contains("min floor"));
            Assert.That(report.Errors, Has.Some.Contains("bosses defeated"));
            Assert.That(report.Errors, Has.Some.Contains("gold cost"));
            Assert.That(report.Errors, Has.Some.Contains("missing_item"));
            Assert.That(report.Errors, Has.Some.Contains("missing_skill"));
            Assert.That(report.Errors, Has.Some.Contains("missing_catalog"));
        }

        [Test]
        public void RuntimeContentLookupUsesDatabaseBeforeCatalogFallback()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
            database.Equipment = new[]
            {
                new ContentEquipmentDefinition
                {
                    Id = "rusty_sword",
                    DisplayName = "Database Sword",
                    Kind = "one_hand_weapon",
                    BuyPrice = 3,
                    SellPrice = 1
                }
            };

            try
            {
                RuntimeContentDatabase.SetActive(database);

                Assert.That(RuntimeContentDatabase.FindEquipment("rusty_sword").DisplayName, Is.EqualTo("Database Sword"));
                Assert.That(RuntimeContentDatabase.FindSkill("skill_slash").DisplayName, Is.EqualTo("Slash"));
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
            }
        }
    }
}

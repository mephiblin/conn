using Conn.Authoring.Content;
using Conn.Core.Content;
using Conn.Core.Skills;
using Conn.Runtime.Content;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Content
{
    public static class AuthoringContentBuildService
    {
        public static AuthoringContentSnapshot FindAuthoringAssets()
        {
            return new AuthoringContentSnapshot
            {
                Monsters = FindAssets<MonsterDefinitionAsset>(),
                Encounters = FindAssets<EncounterDefinitionAsset>(),
                SpawnTables = FindAssets<SpawnTableAsset>(),
                Npcs = FindAssets<NpcDefinitionAsset>(),
                Skills = FindAssets<SkillDefinitionAsset>(),
                Vendors = FindAssets<VendorDefinitionAsset>()
            };
        }

        public static ContentValidationReport Validate(AuthoringContentSnapshot snapshot)
        {
            var report = new ContentValidationReport();
            snapshot ??= new AuthoringContentSnapshot();
            ValidateMonsters(snapshot.Monsters, report);
            ValidateEncounters(snapshot, report);
            ValidateSpawnTables(snapshot, report);
            ValidateNpcs(snapshot, report);
            ValidateSkills(snapshot.Skills, report);
            ValidateVendors(snapshot.Vendors, report);
            return report;
        }

        public static ContentValidationReport BakeInto(ContentDatabaseDefinition database, AuthoringContentSnapshot snapshot)
        {
            var report = Validate(snapshot);
            if (!report.Passed)
            {
                return report;
            }

            if (database == null)
            {
                report.Error("Content database asset is missing.");
                return report;
            }

            var monsters = new List<ContentMonsterDefinition>(database.Monsters ?? Array.Empty<ContentMonsterDefinition>());
            foreach (var asset in snapshot.Monsters ?? Array.Empty<MonsterDefinitionAsset>())
            {
                UpsertMonster(monsters, asset.ToContentDefinition());
            }

            var encounters = new List<ContentEncounterDefinition>(database.Encounters ?? Array.Empty<ContentEncounterDefinition>());
            foreach (var asset in snapshot.Encounters ?? Array.Empty<EncounterDefinitionAsset>())
            {
                UpsertEncounter(encounters, asset.ToContentDefinition());
            }

            database.Monsters = monsters.ToArray();
            database.Encounters = encounters.ToArray();
            var skills = new List<ContentSkillDefinition>(database.Skills ?? Array.Empty<ContentSkillDefinition>());
            foreach (var asset in snapshot.Skills ?? Array.Empty<SkillDefinitionAsset>())
            {
                UpsertSkill(skills, asset.ToContentDefinition());
            }

            database.Skills = skills.ToArray();
            var vendors = new List<ContentVendorDefinition>(database.Vendors ?? Array.Empty<ContentVendorDefinition>());
            foreach (var asset in snapshot.Vendors ?? Array.Empty<VendorDefinitionAsset>())
            {
                UpsertVendor(vendors, asset.ToContentDefinition());
            }

            database.Vendors = vendors.ToArray();
            var npcs = new List<ContentNpcDefinition>(database.Npcs ?? Array.Empty<ContentNpcDefinition>());
            foreach (var asset in snapshot.Npcs ?? Array.Empty<NpcDefinitionAsset>())
            {
                UpsertNpc(npcs, asset.ToContentDefinition());
            }

            database.Npcs = npcs.ToArray();

            var databaseReport = ContentDatabaseValidator.Validate(database);
            foreach (var warning in databaseReport.Warnings)
            {
                report.Warning(warning);
            }

            foreach (var error in databaseReport.Errors)
            {
                report.Error(error);
            }

            if (report.Passed)
            {
                EditorUtility.SetDirty(database);
            }

            return report;
        }

        public static void VerifyRuntimeCanReadBakedAuthoringContent()
        {
            var monster = ScriptableObject.CreateInstance<MonsterDefinitionAsset>();
            var encounter = ScriptableObject.CreateInstance<EncounterDefinitionAsset>();
            var npc = ScriptableObject.CreateInstance<NpcDefinitionAsset>();
            var skill = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            var vendor = ScriptableObject.CreateInstance<VendorDefinitionAsset>();
            var database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();

            try
            {
                monster.Id = "authoring_runtime_probe_monster";
                monster.DisplayName = "Authoring Runtime Probe Monster";
                monster.MaxHp = 7;
                monster.AttackPower = 3;
                monster.XpReward = 5;
                monster.Ai = "Probe Attack";
                monster.ThemeTags = new[] { "probe" };
                monster.SpawnRoleTags = new[] { "trash" };

                encounter.Id = "authoring_runtime_probe_encounter";
                encounter.DisplayName = "Authoring Runtime Probe Encounter";
                encounter.PrimaryMonster = monster;
                encounter.Pattern = "single_primary";
                encounter.XpReward = 5;
                encounter.EnemySlots = new[]
                {
                    new EncounterEnemySlotAsset
                    {
                        SlotId = "primary",
                        Monster = monster,
                        Count = 1,
                        Primary = true
                    }
                };

                npc.Id = "authoring_runtime_probe_npc";
                npc.DisplayName = "Authoring Runtime Probe NPC";
                npc.Description = "Probe NPC";
                npc.ServiceType = "talk";
                npc.VendorId = string.Empty;
                npc.QuestIds = new[] { "quest_seed_authoring_probe" };

                skill.Id = "authoring_runtime_probe_skill";
                skill.DisplayName = "Authoring Runtime Probe Skill";
                skill.EffectKind = "guard";
                skill.TargetMode = "self";
                skill.Formula = "die_plus_effect";
                skill.BuyPrice = 3;
                skill.SellPrice = 1;
                skill.Power = 2;
                skill.CatalogIds = new[] { "probe_skill_catalog" };

                vendor.Id = "authoring_runtime_probe_vendor";
                vendor.ServiceType = "skill_shop";
                vendor.GoldCost = 1;
                vendor.Summary = "Probe vendor";
                vendor.StockSkillIds = new[] { skill.Id };
                vendor.CatalogIds = new[] { "probe_skill_catalog" };
                vendor.Rotations = new[]
                {
                    new VendorRotationAsset
                    {
                        MinFloor = 2,
                        BossesDefeated = 1,
                        GoldCost = 2,
                        Summary = "Probe rotation",
                        StockSkillIds = new[] { skill.Id }
                    }
                };

                var snapshot = new AuthoringContentSnapshot
                {
                    Monsters = new[] { monster },
                    Encounters = new[] { encounter },
                    SpawnTables = Array.Empty<SpawnTableAsset>(),
                    Npcs = new[] { npc },
                    Skills = new[] { skill },
                    Vendors = new[] { vendor }
                };

                var report = BakeInto(database, snapshot);
                if (!report.Passed)
                {
                    throw new InvalidOperationException(string.Join("\n", report.Errors));
                }

                RuntimeContentDatabase.SetActive(database);
                var runtimeMonster = RuntimeContentDatabase.FindMonster(monster.Id);
                var runtimeEncounter = RuntimeContentDatabase.FindEncounter(encounter.Id);
                var runtimeNpc = RuntimeContentDatabase.FindNpc(npc.Id);
                var runtimeSkill = RuntimeContentDatabase.FindSkill(skill.Id);
                var runtimeVendor = RuntimeContentDatabase.FindVendor(vendor.Id);

                if (runtimeMonster.MonsterId != monster.Id || runtimeMonster.MaxHp != monster.MaxHp)
                {
                    throw new InvalidOperationException("RuntimeContentDatabase failed to read baked authoring monster data.");
                }

                if (runtimeEncounter.EncounterId != encounter.Id || runtimeEncounter.MonsterId != monster.Id || runtimeEncounter.EnemySlots.Length != 1)
                {
                    throw new InvalidOperationException("RuntimeContentDatabase failed to read baked authoring encounter data.");
                }

                if (runtimeNpc == null || runtimeNpc.Id != npc.Id || runtimeNpc.ServiceType != npc.ServiceType || runtimeNpc.QuestIds.Length != 1)
                {
                    throw new InvalidOperationException("RuntimeContentDatabase failed to read baked authoring NPC data.");
                }

                if (runtimeSkill == null || runtimeSkill.SkillId != skill.Id || runtimeSkill.EffectKind != SkillEffectKind.Guard || runtimeSkill.Power != skill.Power)
                {
                    throw new InvalidOperationException("RuntimeContentDatabase failed to read baked authoring skill data.");
                }

                if (runtimeVendor == null || runtimeVendor.Id != vendor.Id || runtimeVendor.StockSkillIds.Length != 1)
                {
                    throw new InvalidOperationException("RuntimeContentDatabase failed to read baked authoring vendor data.");
                }

                var stock = RuntimeContentDatabase.SkillIdsForVendor(vendor.Id);
                if (stock.Length != 1 || stock[0] != skill.Id)
                {
                    throw new InvalidOperationException("RuntimeContentDatabase failed to expose baked authoring vendor skill stock.");
                }

                var rotation = RuntimeContentDatabase.SelectVendorRotation(vendor.Id, 2, 1);
                if (rotation == null || rotation.GoldCost != 2)
                {
                    throw new InvalidOperationException("RuntimeContentDatabase failed to expose baked authoring vendor rotation.");
                }
            }
            finally
            {
                RuntimeContentDatabase.SetActive(null);
                UnityEngine.Object.DestroyImmediate(monster);
                UnityEngine.Object.DestroyImmediate(encounter);
                UnityEngine.Object.DestroyImmediate(npc);
                UnityEngine.Object.DestroyImmediate(skill);
                UnityEngine.Object.DestroyImmediate(vendor);
                UnityEngine.Object.DestroyImmediate(database);
            }
        }

        private static T[] FindAssets<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var assets = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            assets.Sort((left, right) => string.Compare(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right), StringComparison.Ordinal));
            return assets.ToArray();
        }

        private static void ValidateMonsters(MonsterDefinitionAsset[] monsters, ContentValidationReport report)
        {
            var ids = new HashSet<string>();
            foreach (var monster in monsters ?? Array.Empty<MonsterDefinitionAsset>())
            {
                if (monster == null)
                {
                    continue;
                }

                RequireId(monster.Id, "Monster authoring asset", report);
                if (!string.IsNullOrWhiteSpace(monster.Id) && !ids.Add(monster.Id))
                {
                    report.Error($"Monster authoring id is duplicated: {monster.Id}");
                }

                if (string.IsNullOrWhiteSpace(monster.DisplayName))
                {
                    report.Error($"Monster authoring {monster.Id} display name must not be empty.");
                }

                if (monster.MaxHp <= 0)
                {
                    report.Error($"Monster authoring {monster.Id} max HP must be positive.");
                }

                if (monster.AttackPower <= 0)
                {
                    report.Error($"Monster authoring {monster.Id} attack power must be positive.");
                }

                if (monster.XpReward < 0)
                {
                    report.Error($"Monster authoring {monster.Id} XP reward must not be negative.");
                }
            }
        }

        private static void ValidateEncounters(AuthoringContentSnapshot snapshot, ContentValidationReport report)
        {
            var monsterIds = MonsterIds(snapshot.Monsters);
            var encounterIds = new HashSet<string>();
            foreach (var encounter in snapshot.Encounters ?? Array.Empty<EncounterDefinitionAsset>())
            {
                if (encounter == null)
                {
                    continue;
                }

                RequireId(encounter.Id, "Encounter authoring asset", report);
                if (!string.IsNullOrWhiteSpace(encounter.Id) && !encounterIds.Add(encounter.Id))
                {
                    report.Error($"Encounter authoring id is duplicated: {encounter.Id}");
                }

                if (string.IsNullOrWhiteSpace(encounter.DisplayName))
                {
                    report.Error($"Encounter authoring {encounter.Id} display name must not be empty.");
                }

                var primaryMonsterId = ResolveMonsterId(encounter.PrimaryMonster, encounter.PrimaryMonsterId);
                if (string.IsNullOrWhiteSpace(primaryMonsterId))
                {
                    report.Error($"Encounter authoring {encounter.Id} primary monster must not be empty.");
                }
                else if (!monsterIds.Contains(primaryMonsterId))
                {
                    report.Error($"Encounter authoring {encounter.Id} primary monster is missing: {primaryMonsterId}");
                }

                if (encounter.XpReward < 0)
                {
                    report.Error($"Encounter authoring {encounter.Id} XP reward must not be negative.");
                }

                if (string.IsNullOrWhiteSpace(encounter.Pattern))
                {
                    report.Error($"Encounter authoring {encounter.Id} pattern must not be empty.");
                }

                ValidateEncounterSlots(encounter, monsterIds, primaryMonsterId, report);
            }
        }

        private static void ValidateEncounterSlots(EncounterDefinitionAsset encounter, HashSet<string> monsterIds, string primaryMonsterId, ContentValidationReport report)
        {
            var slots = encounter.EnemySlots ?? Array.Empty<EncounterEnemySlotAsset>();
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
                    report.Error($"Encounter authoring {encounter.Id} enemy slot id is duplicated: {slotId}");
                }

                var monsterId = ResolveMonsterId(slot.Monster, slot.MonsterId);
                if (string.IsNullOrWhiteSpace(monsterId))
                {
                    report.Error($"Encounter authoring {encounter.Id} enemy slot {slotId} monster must not be empty.");
                }
                else if (!monsterIds.Contains(monsterId))
                {
                    report.Error($"Encounter authoring {encounter.Id} enemy slot {slotId} monster is missing: {monsterId}");
                }

                if (slot.Count <= 0)
                {
                    report.Error($"Encounter authoring {encounter.Id} enemy slot {slotId} count must be positive.");
                }

                hasPrimary |= slot.Primary || monsterId == primaryMonsterId;
            }

            if (!hasPrimary)
            {
                report.Error($"Encounter authoring {encounter.Id} enemy slots must include the primary monster: {primaryMonsterId}");
            }
        }

        private static void ValidateSpawnTables(AuthoringContentSnapshot snapshot, ContentValidationReport report)
        {
            var monsterIds = MonsterIds(snapshot.Monsters);
            var encounterIds = EncounterIds(snapshot.Encounters);
            var spawnTableIds = new HashSet<string>();
            foreach (var spawnTable in snapshot.SpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                if (spawnTable == null)
                {
                    continue;
                }

                RequireId(spawnTable.Id, "Spawn table authoring asset", report);
                if (!string.IsNullOrWhiteSpace(spawnTable.Id) && !spawnTableIds.Add(spawnTable.Id))
                {
                    report.Error($"Spawn table authoring id is duplicated: {spawnTable.Id}");
                }

                var validPoolEntries = 0;
                foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
                {
                    if (entry.Weight <= 0)
                    {
                        report.Error($"Spawn table {spawnTable.Id} encounter entry weight must be positive.");
                    }

                    var encounterId = ResolveEncounterId(entry.Encounter, entry.EncounterId);
                    if (string.IsNullOrWhiteSpace(encounterId) || !encounterIds.Contains(encounterId))
                    {
                        report.Error($"Spawn table {spawnTable.Id} encounter entry is missing: {encounterId}");
                    }
                    else
                    {
                        validPoolEntries++;
                    }
                }

                foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
                {
                    if (entry.Weight <= 0)
                    {
                        report.Error($"Spawn table {spawnTable.Id} direct monster entry weight must be positive.");
                    }

                    var monsterId = ResolveMonsterId(entry.Monster, entry.MonsterId);
                    if (string.IsNullOrWhiteSpace(monsterId) || !monsterIds.Contains(monsterId))
                    {
                        report.Error($"Spawn table {spawnTable.Id} direct monster entry is missing: {monsterId}");
                    }
                    else
                    {
                        validPoolEntries++;
                    }
                }

                if (validPoolEntries == 0)
                {
                    report.Error($"Spawn table {spawnTable.Id} must resolve to at least one valid encounter or monster.");
                }
            }
        }

        private static void UpsertMonster(List<ContentMonsterDefinition> monsters, ContentMonsterDefinition definition)
        {
            for (var i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].Id == definition.Id)
                {
                    monsters[i] = definition;
                    return;
                }
            }

            monsters.Add(definition);
        }

        private static void UpsertEncounter(List<ContentEncounterDefinition> encounters, ContentEncounterDefinition definition)
        {
            for (var i = 0; i < encounters.Count; i++)
            {
                if (encounters[i].Id == definition.Id)
                {
                    encounters[i] = definition;
                    return;
                }
            }

            encounters.Add(definition);
        }

        private static void UpsertSkill(List<ContentSkillDefinition> skills, ContentSkillDefinition definition)
        {
            for (var i = 0; i < skills.Count; i++)
            {
                if (skills[i].Id == definition.Id)
                {
                    skills[i] = definition;
                    return;
                }
            }

            skills.Add(definition);
        }

        private static void UpsertNpc(List<ContentNpcDefinition> npcs, ContentNpcDefinition definition)
        {
            for (var i = 0; i < npcs.Count; i++)
            {
                if (npcs[i].Id == definition.Id)
                {
                    npcs[i] = definition;
                    return;
                }
            }

            npcs.Add(definition);
        }

        private static void UpsertVendor(List<ContentVendorDefinition> vendors, ContentVendorDefinition definition)
        {
            for (var i = 0; i < vendors.Count; i++)
            {
                if (vendors[i].Id == definition.Id)
                {
                    vendors[i] = definition;
                    return;
                }
            }

            vendors.Add(definition);
        }

        private static HashSet<string> MonsterIds(MonsterDefinitionAsset[] monsters)
        {
            var ids = new HashSet<string>();
            foreach (var monster in monsters ?? Array.Empty<MonsterDefinitionAsset>())
            {
                if (monster != null && !string.IsNullOrWhiteSpace(monster.Id))
                {
                    ids.Add(monster.Id);
                }
            }

            return ids;
        }

        private static HashSet<string> EncounterIds(EncounterDefinitionAsset[] encounters)
        {
            var ids = new HashSet<string>();
            foreach (var encounter in encounters ?? Array.Empty<EncounterDefinitionAsset>())
            {
                if (encounter != null && !string.IsNullOrWhiteSpace(encounter.Id))
                {
                    ids.Add(encounter.Id);
                }
            }

            return ids;
        }

        private static void ValidateNpcs(AuthoringContentSnapshot snapshot, ContentValidationReport report)
        {
            var ids = new HashSet<string>();
            foreach (var npc in snapshot.Npcs ?? Array.Empty<NpcDefinitionAsset>())
            {
                if (npc == null)
                {
                    continue;
                }

                RequireId(npc.Id, "NPC authoring asset", report);
                if (!string.IsNullOrWhiteSpace(npc.Id) && !ids.Add(npc.Id))
                {
                    report.Error($"NPC authoring id is duplicated: {npc.Id}");
                }

                if (string.IsNullOrWhiteSpace(npc.DisplayName))
                {
                    report.Error($"NPC authoring {npc.Id} display name must not be empty.");
                }

                foreach (var questId in npc.QuestIds ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(questId))
                    {
                        report.Error($"NPC authoring {npc.Id} quest id must not be empty.");
                    }
                }
            }
        }

        private static void ValidateSkills(SkillDefinitionAsset[] skills, ContentValidationReport report)
        {
            var ids = new HashSet<string>();
            foreach (var skill in skills ?? Array.Empty<SkillDefinitionAsset>())
            {
                if (skill == null)
                {
                    continue;
                }

                RequireId(skill.Id, "Skill authoring asset", report);
                if (!string.IsNullOrWhiteSpace(skill.Id) && !ids.Add(skill.Id))
                {
                    report.Error($"Skill authoring id is duplicated: {skill.Id}");
                }

                if (string.IsNullOrWhiteSpace(skill.DisplayName))
                {
                    report.Error($"Skill authoring {skill.Id} display name must not be empty.");
                }

                if (string.IsNullOrWhiteSpace(skill.EffectKind))
                {
                    report.Error($"Skill authoring {skill.Id} effect kind must not be empty.");
                }
                else if (!Enum.TryParse<SkillEffectKind>(skill.EffectKind, true, out _))
                {
                    report.Error($"Skill authoring {skill.Id} effect kind is not runtime-supported: {skill.EffectKind}");
                }

                if (string.IsNullOrWhiteSpace(skill.TargetMode))
                {
                    report.Warning($"Skill authoring {skill.Id} target mode is empty.");
                }

                if (string.IsNullOrWhiteSpace(skill.Formula))
                {
                    report.Warning($"Skill authoring {skill.Id} formula is empty.");
                }

                if (skill.BuyPrice < 0 || skill.SellPrice < 0)
                {
                    report.Error($"Skill authoring {skill.Id} prices must not be negative.");
                }
            }
        }

        private static void ValidateVendors(VendorDefinitionAsset[] vendors, ContentValidationReport report)
        {
            var ids = new HashSet<string>();
            foreach (var vendor in vendors ?? Array.Empty<VendorDefinitionAsset>())
            {
                if (vendor == null)
                {
                    continue;
                }

                RequireId(vendor.Id, "Vendor authoring asset", report);
                if (!string.IsNullOrWhiteSpace(vendor.Id) && !ids.Add(vendor.Id))
                {
                    report.Error($"Vendor authoring id is duplicated: {vendor.Id}");
                }

                if (vendor.GoldCost < 0)
                {
                    report.Error($"Vendor authoring {vendor.Id} gold cost must not be negative.");
                }

                foreach (var rotation in vendor.Rotations ?? Array.Empty<VendorRotationAsset>())
                {
                    if (rotation.MinFloor < 0)
                    {
                        report.Error($"Vendor authoring {vendor.Id} rotation min floor must not be negative.");
                    }

                    if (rotation.BossesDefeated < 0)
                    {
                        report.Error($"Vendor authoring {vendor.Id} rotation bosses defeated must not be negative.");
                    }

                    if (rotation.GoldCost < 0)
                    {
                        report.Error($"Vendor authoring {vendor.Id} rotation gold cost must not be negative.");
                    }
                }
            }
        }

        private static string ResolveMonsterId(MonsterDefinitionAsset asset, string fallbackId)
        {
            return asset != null && !string.IsNullOrWhiteSpace(asset.Id) ? asset.Id : fallbackId;
        }

        private static string ResolveEncounterId(EncounterDefinitionAsset asset, string fallbackId)
        {
            return asset != null && !string.IsNullOrWhiteSpace(asset.Id) ? asset.Id : fallbackId;
        }

        private static void RequireId(string id, string label, ContentValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                report.Error($"{label} id must not be empty.");
            }
        }
    }

    public sealed class AuthoringContentSnapshot
    {
        public MonsterDefinitionAsset[] Monsters = Array.Empty<MonsterDefinitionAsset>();
        public EncounterDefinitionAsset[] Encounters = Array.Empty<EncounterDefinitionAsset>();
        public SpawnTableAsset[] SpawnTables = Array.Empty<SpawnTableAsset>();
        public NpcDefinitionAsset[] Npcs = Array.Empty<NpcDefinitionAsset>();
        public SkillDefinitionAsset[] Skills = Array.Empty<SkillDefinitionAsset>();
        public VendorDefinitionAsset[] Vendors = Array.Empty<VendorDefinitionAsset>();
    }
}

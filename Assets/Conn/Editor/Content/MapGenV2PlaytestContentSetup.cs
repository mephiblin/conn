using Conn.Authoring.Content;
using Conn.Core.Content;
using Conn.Core.World;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Content
{
    public static class MapGenV2PlaytestContentSetup
    {
        private const string RootFolder = "Assets/Conn/Authoring/Content/MapGenV2Playtest";
        private const string MonsterFolder = RootFolder + "/Monsters";
        private const string EncounterFolder = RootFolder + "/Encounters";
        private const string SpawnTableFolder = RootFolder + "/SpawnTables";
        private const string QuestFolder = RootFolder + "/Quests";
        private const string StarterProfileId = "mapgenv2_playtest_starter";
        private const string BranchingProfileId = "mapgenv2_playtest_branching";
        private const string DepthsProfileId = "mapgenv2_playtest_depths";

        [MenuItem("Conn/Content Database/Create MapGenV2 Playtest Content")]
        public static void CreateOrUpdateMenu()
        {
            var result = CreateOrUpdate();
            Selection.activeObject = result.PrimaryQuest;
            EditorGUIUtility.PingObject(result.PrimaryQuest);
            Debug.Log($"MapGenV2 playtest content updated for primary profile '{result.MapProfileId}'.");
        }

        [MenuItem("Conn/Content Database/Create MapGenV2 Playtest Content and Bake")]
        public static void CreateOrUpdateAndBakeMenu()
        {
            CreateOrUpdateAndBake();
        }

        public static MapGenV2PlaytestContentResult CreateOrUpdateAndBake()
        {
            var result = CreateOrUpdate();
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
                EnsureFolder(ParentFolder(LegacyContentJsonImporter.DefaultDatabaseAssetPath));
                AssetDatabase.CreateAsset(database, LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            }

            var report = AuthoringContentBuildService.BakeInto(database, AuthoringContentBuildService.FindAuthoringAssets());
            if (!report.Passed)
            {
                throw new InvalidOperationException("MapGenV2 playtest content bake failed:\n" + string.Join("\n", report.Errors));
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"MapGenV2 playtest content updated and baked into {LegacyContentJsonImporter.DefaultDatabaseAssetPath} for profile '{result.MapProfileId}'.");
            return result;
        }

        public static MapGenV2PlaytestContentResult CreateOrUpdate()
        {
            EnsureFolder(MonsterFolder);
            EnsureFolder(EncounterFolder);
            EnsureFolder(SpawnTableFolder);
            EnsureFolder(QuestFolder);

            var scout = UpsertMonster(
                "mapgenv2_monster_echo_scout",
                "Echo Scout",
                MonsterSpecies.Human,
                MonsterGrade.Normal,
                10,
                3,
                0,
                5,
                "Quick Probe",
                new[] { "mapgenv2", "ruins" },
                new[] { "starter" },
                new[] { "trash", "scout" });
            var husk = UpsertMonster(
                "mapgenv2_monster_cinder_husk",
                "Cinder Husk",
                MonsterSpecies.Undead,
                MonsterGrade.Normal,
                14,
                4,
                1,
                7,
                "Ashen Swipe",
                new[] { "mapgenv2", "ash" },
                new[] { "starter" },
                new[] { "trash", "ambush" });
            var sentinel = UpsertMonster(
                "mapgenv2_monster_gate_sentinel",
                "Gate Sentinel",
                MonsterSpecies.Aberration,
                MonsterGrade.Elite,
                24,
                5,
                2,
                12,
                "Sentinel Crush",
                new[] { "mapgenv2", "stone" },
                new[] { "starter" },
                new[] { "elite", "quest_target" });

            var scoutPair = UpsertEncounter(
                "mapgenv2_encounter_echo_scout_pair",
                "Echo Scout Pair",
                scout,
                10,
                "multi_slot",
                new[]
                {
                    Slot("primary", scout, 1, true),
                    Slot("flank", husk, 1, false)
                },
                1,
                2,
                new[] { "mapgenv2", "starter" },
                new[] { "trash" });
            var cinderPack = UpsertEncounter(
                "mapgenv2_encounter_cinder_pack",
                "Cinder Pack",
                husk,
                14,
                "multi_slot",
                new[]
                {
                    Slot("primary", husk, 2, true),
                    Slot("scout", scout, 1, false)
                },
                2,
                4,
                new[] { "mapgenv2", "ash" },
                new[] { "trash", "ambush" });
            var sentinelTrial = UpsertEncounter(
                "mapgenv2_encounter_gate_sentinel_trial",
                "Gate Sentinel Trial",
                sentinel,
                24,
                "elite_primary",
                new[]
                {
                    Slot("primary", sentinel, 1, true),
                    Slot("guard", husk, 1, false)
                },
                3,
                6,
                new[] { "mapgenv2", "stone" },
                new[] { "elite", "quest_target" });

            var spawnTable = UpsertSpawnTable(
                "mapgenv2_spawn_starter_playtest",
                "MapGenV2 Starter Playtest Spawns",
                new[] { scoutPair, cinderPack, sentinelTrial },
                new[] { scout, husk },
                "Reusable pool for MapGenV2 starter-profile field monster and encounter previews. Runtime MapGenV2 currently falls back to active quest targets when baked maps do not provide encounter placements.");

            var firstQuest = UpsertQuest(
                "mapgenv2_quest_echo_sweep",
                "MapGenV2 Echo Sweep",
                "Use the MapGenV2 starter playtest profile and clear a lightweight scout target.",
                StarterProfileId,
                scoutPair,
                scout,
                12,
                5);
            UpsertQuest(
                "mapgenv2_quest_cinder_route",
                "MapGenV2 Cinder Route",
                "Use the MapGenV2 branching playtest profile and fight through a small mixed pack.",
                BranchingProfileId,
                cinderPack,
                husk,
                16,
                7);
            UpsertQuest(
                "mapgenv2_quest_sentinel_check",
                "MapGenV2 Sentinel Check",
                "Use the MapGenV2 depths playtest profile and verify the elite target path.",
                DepthsProfileId,
                sentinelTrial,
                sentinel,
                24,
                10);

            EditorUtility.SetDirty(spawnTable);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new MapGenV2PlaytestContentResult(StarterProfileId, firstQuest);
        }

        private static MonsterDefinitionAsset UpsertMonster(
            string id,
            string displayName,
            MonsterSpecies species,
            MonsterGrade grade,
            int maxHp,
            int attackPower,
            int defense,
            int xpReward,
            string ai,
            string[] themeTags,
            string[] biomeTags,
            string[] spawnRoleTags)
        {
            var monster = LoadOrCreate<MonsterDefinitionAsset>(MonsterFolder, id);
            monster.Id = id;
            monster.DisplayName = displayName;
            monster.Species = species;
            monster.Grade = grade;
            monster.DefaultGroupCount = 1;
            monster.MaxHp = maxHp;
            monster.AttackPower = attackPower;
            monster.Defense = defense;
            monster.EvasionRate = 0f;
            monster.XpReward = xpReward;
            monster.Boss = grade == MonsterGrade.Boss;
            monster.Ai = ai;
            monster.FieldAiProfile = new FieldMonsterAiProfile
            {
                ProfileId = $"field_ai_{id}",
                DetectionRadius = grade == MonsterGrade.Elite ? 7f : 5f,
                PatrolRadius = grade == MonsterGrade.Elite ? 4f : 2.5f,
                MoveSpeed = grade == MonsterGrade.Elite ? 2.7f : 2.35f,
                ContactCooldownSeconds = 1f
            };
            monster.Traits = Array.Empty<MonsterTraitAsset>();
            monster.ThemeTags = themeTags ?? Array.Empty<string>();
            monster.BiomeTags = biomeTags ?? Array.Empty<string>();
            monster.SpawnRoleTags = spawnRoleTags ?? Array.Empty<string>();
            monster.CompatibilityTags = new[] { "mapgenv2", "dungeon", "mapgenv2_playtest" };
            EditorUtility.SetDirty(monster);
            return monster;
        }

        private static EncounterDefinitionAsset UpsertEncounter(
            string id,
            string displayName,
            MonsterDefinitionAsset primaryMonster,
            int xpReward,
            string pattern,
            EncounterEnemySlotAsset[] slots,
            int minDifficulty,
            int maxDifficulty,
            string[] themeTags,
            string[] spawnRoleTags)
        {
            var encounter = LoadOrCreate<EncounterDefinitionAsset>(EncounterFolder, id);
            encounter.Id = id;
            encounter.DisplayName = displayName;
            encounter.PrimaryMonster = primaryMonster;
            encounter.PrimaryMonsterId = primaryMonster != null ? primaryMonster.Id : string.Empty;
            encounter.XpReward = xpReward;
            encounter.RewardId = string.Empty;
            encounter.Pattern = pattern;
            encounter.EnemySlots = slots ?? Array.Empty<EncounterEnemySlotAsset>();
            encounter.MinDifficulty = minDifficulty;
            encounter.MaxDifficulty = maxDifficulty;
            encounter.ThemeTags = themeTags ?? Array.Empty<string>();
            encounter.SpawnRoleTags = spawnRoleTags ?? Array.Empty<string>();
            encounter.AllowedMapTags = new[] { "mapgenv2", "mapgenv2_playtest" };
            encounter.CompatibilityTags = new[] { "mapgenv2", "dungeon", "mapgenv2_playtest" };
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static SpawnTableAsset UpsertSpawnTable(
            string id,
            string displayName,
            EncounterDefinitionAsset[] encounters,
            MonsterDefinitionAsset[] directMonsters,
            string notes)
        {
            var spawnTable = LoadOrCreate<SpawnTableAsset>(SpawnTableFolder, id);
            spawnTable.Id = id;
            spawnTable.DisplayName = displayName;
            spawnTable.RequiredThemeTags = new[] { "mapgenv2" };
            spawnTable.RequiredBiomeTags = Array.Empty<string>();
            spawnTable.RequiredSpawnRoleTags = Array.Empty<string>();
            spawnTable.AllowedRoomRoles = new[] { "MainPath", "SideBranch", "QuestTarget", "Boss" };
            spawnTable.Notes = notes;

            var encounterEntries = new List<SpawnEncounterEntry>();
            for (var i = 0; i < encounters.Length; i++)
            {
                encounterEntries.Add(new SpawnEncounterEntry
                {
                    Encounter = encounters[i],
                    EncounterId = encounters[i] != null ? encounters[i].Id : string.Empty,
                    Weight = i == encounters.Length - 1 ? 2 : 4,
                    MinFloor = 1,
                    MaxFloor = 99,
                    MinDifficulty = i + 1,
                    MaxDifficulty = i == encounters.Length - 1 ? 6 : 4,
                    RoomRoleConstraints = Array.Empty<string>()
                });
            }

            var monsterEntries = new List<SpawnMonsterEntry>();
            foreach (var monster in directMonsters ?? Array.Empty<MonsterDefinitionAsset>())
            {
                monsterEntries.Add(new SpawnMonsterEntry
                {
                    Monster = monster,
                    MonsterId = monster != null ? monster.Id : string.Empty,
                    Weight = 1,
                    MinFloor = 1,
                    MaxFloor = 99,
                    GeneratedEncounterPolicy = "single_primary",
                    RoomRoleConstraints = Array.Empty<string>()
                });
            }

            spawnTable.EncounterEntries = encounterEntries.ToArray();
            spawnTable.DirectMonsterEntries = monsterEntries.ToArray();
            EditorUtility.SetDirty(spawnTable);
            return spawnTable;
        }

        private static QuestDefinitionAsset UpsertQuest(
            string id,
            string displayName,
            string description,
            string mapProfileId,
            EncounterDefinitionAsset targetEncounter,
            MonsterDefinitionAsset targetMonster,
            int goldReward,
            int xpReward)
        {
            var quest = LoadOrCreate<QuestDefinitionAsset>(QuestFolder, id);
            quest.Id = id;
            quest.DisplayName = displayName;
            quest.Description = description;
            quest.MapKind = "dungeon";
            quest.MapProfileId = mapProfileId;
            quest.TargetEncounter = targetEncounter;
            quest.TargetEncounterId = targetEncounter != null ? targetEncounter.Id : string.Empty;
            quest.TargetMonster = targetMonster;
            quest.TargetMonsterId = targetMonster != null ? targetMonster.Id : string.Empty;
            quest.GoldReward = goldReward;
            quest.XpReward = xpReward;
            quest.RewardItems = Array.Empty<ContentItemStack>();
            EditorUtility.SetDirty(quest);
            return quest;
        }

        private static EncounterEnemySlotAsset Slot(string slotId, MonsterDefinitionAsset monster, int count, bool primary)
        {
            return new EncounterEnemySlotAsset
            {
                SlotId = slotId,
                Monster = monster,
                MonsterId = monster != null ? monster.Id : string.Empty,
                Count = count,
                Primary = primary
            };
        }

        private static T LoadOrCreate<T>(string folder, string id) where T : ScriptableObject
        {
            var path = $"{folder}/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static string ParentFolder(string assetPath)
        {
            var slash = assetPath.LastIndexOf('/');
            return slash <= 0 ? "Assets" : assetPath.Substring(0, slash);
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = ParentFolder(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
        }
    }

    public readonly struct MapGenV2PlaytestContentResult
    {
        public MapGenV2PlaytestContentResult(string mapProfileId, QuestDefinitionAsset primaryQuest)
        {
            MapProfileId = mapProfileId;
            PrimaryQuest = primaryQuest;
        }

        public string MapProfileId { get; }
        public QuestDefinitionAsset PrimaryQuest { get; }
    }
}

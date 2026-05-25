using Conn.Core.Content;
using Conn.Authoring.Content;
using Conn.Authoring.Maps;
using Conn.Editor.Content;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Windows
{
    public sealed class ContentDatabaseWindow : EditorWindow
    {
        private const string DefaultNewDatabasePath = "Assets/Conn/Core/Content/ContentDatabase.asset";
        private static readonly string[] TabNames =
        {
            "Summary",
            "Quest",
            "Monster",
            "Encounter",
            "NPC",
            "Skill",
            "Vendor",
            "Authoring",
            "Validation"
        };

        private ContentDatabaseDefinition database;
        private Vector2 scroll;
        private string legacyDataPath = LegacyContentJsonImporter.DefaultLegacyDataPath;
        private string createAssetPath = DefaultNewDatabasePath;
        private int selectedTab;
        private int selectedQuestIndex;
        private int selectedMonsterIndex;
        private int selectedEncounterIndex;
        private ContentValidationReport validationReport;
        private ContentValidationReport authoringValidationReport;
        private AuthoringContentSnapshot authoringSnapshot;

        [MenuItem("Conn/Content Database/Window")]
        public static void Open()
        {
            GetWindow<ContentDatabaseWindow>("Content Database");
        }

        private void OnEnable()
        {
            database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
        }

        private void OnGUI()
        {
            DrawToolbar();

            selectedTab = GUILayout.Toolbar(selectedTab, TabNames);
            EditorGUILayout.Space();

            if (database == null)
            {
                EditorGUILayout.HelpBox("Select, create, or import a content database asset.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (selectedTab)
            {
                case 0:
                    DrawSummaryTab();
                    break;
                case 1:
                    DrawQuestTab();
                    break;
                case 2:
                    DrawMonsterTab();
                    break;
                case 3:
                    DrawEncounterTab();
                    break;
                case 7:
                    DrawAuthoringTab();
                    break;
                case 8:
                    DrawValidationTab();
                    break;
                default:
                    DrawPlaceholderTab(TabNames[selectedTab]);
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUI.BeginChangeCheck();
            database = (ContentDatabaseDefinition)EditorGUILayout.ObjectField("Database", database, typeof(ContentDatabaseDefinition), false);
            if (EditorGUI.EndChangeCheck())
            {
                validationReport = null;
            }

            createAssetPath = EditorGUILayout.TextField("Create Path", createAssetPath);
            legacyDataPath = EditorGUILayout.TextField("Legacy JSON Path", legacyDataPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(createAssetPath)))
                {
                    if (GUILayout.Button("Create Database"))
                    {
                        CreateDatabaseAsset();
                    }
                }

                if (GUILayout.Button("Import Legacy JSON"))
                {
                    var importTargetPath = ImportTargetPath();
                    EnsureFolderForAsset(importTargetPath);
                    database = LegacyContentJsonImporter.Import(legacyDataPath, importTargetPath);
                    validationReport = null;
                }

                using (new EditorGUI.DisabledScope(database == null || !EditorUtility.IsDirty(database)))
                {
                    if (GUILayout.Button("Save"))
                    {
                        SaveDatabase();
                    }
                }

                using (new EditorGUI.DisabledScope(database == null))
                {
                    if (GUILayout.Button("Validate"))
                    {
                        RunValidation();
                        selectedTab = Array.IndexOf(TabNames, "Validation");
                    }
                }
            }

            if (database != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(database);
                var dirty = EditorUtility.IsDirty(database);
                EditorGUILayout.LabelField("Asset", string.IsNullOrEmpty(assetPath) ? "(unsaved asset)" : assetPath);
                EditorGUILayout.LabelField("State", dirty ? "Dirty - unsaved changes" : "Saved");
            }
        }

        private void DrawSummaryTab()
        {
            EditorGUILayout.LabelField("Items", Count(database.Items).ToString());
            EditorGUILayout.LabelField("Equipment", Count(database.Equipment).ToString());
            EditorGUILayout.LabelField("Skills", Count(database.Skills).ToString());
            EditorGUILayout.LabelField("Monsters", Count(database.Monsters).ToString());
            EditorGUILayout.LabelField("Encounters", (database.Encounters?.Length ?? 0).ToString());
            EditorGUILayout.LabelField("Quests", Count(database.Quests).ToString());
            EditorGUILayout.LabelField("Vendors", Count(database.Vendors).ToString());
            EditorGUILayout.LabelField("NPCs", Count(database.Npcs).ToString());
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vendor Rotation Entries", CountVendorRotations(database).ToString());
            EditorGUILayout.LabelField("Vendor Catalog References", CountVendorCatalogReferences(database).ToString());
            EditorGUILayout.LabelField("Quest -> Encounter -> Monster Links", CountQuestEncounterMonsterLinks(database).ToString());
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Direct database editing remains available as a bootstrap bridge. Production source-of-truth work should move to typed authoring assets and use this window for browsing, validation, and build/export.",
                MessageType.Info);
        }

        private static void DrawPlaceholderTab(string tabName)
        {
            EditorGUILayout.HelpBox($"{tabName} editor is scheduled for the next Phase 1 checklist items.", MessageType.Info);
        }

        private void DrawQuestTab()
        {
            database.Quests ??= Array.Empty<ContentQuestDefinition>();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(280f)))
                {
                    EditorGUILayout.LabelField("Quests", EditorStyles.boldLabel);
                    for (var i = 0; i < database.Quests.Length; i++)
                    {
                        var quest = database.Quests[i];
                        var label = string.IsNullOrWhiteSpace(quest.Id) ? "(missing id)" : quest.Id;
                        if (!string.IsNullOrWhiteSpace(quest.DisplayName))
                        {
                            label = $"{label} - {quest.DisplayName}";
                        }

                        if (GUILayout.Toggle(selectedQuestIndex == i, label, "Button"))
                        {
                            selectedQuestIndex = i;
                        }
                    }

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Create Quest"))
                    {
                        AddQuest();
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawQuestDetail();
                }
            }
        }

        private void DrawQuestDetail()
        {
            if (database.Quests.Length == 0)
            {
                EditorGUILayout.HelpBox("Create a quest to edit board and dungeon target data.", MessageType.Info);
                return;
            }

            selectedQuestIndex = Mathf.Clamp(selectedQuestIndex, 0, database.Quests.Length - 1);
            var quest = database.Quests[selectedQuestIndex];
            quest.RewardItems ??= Array.Empty<ContentItemStack>();

            EditorGUILayout.LabelField("Quest Detail", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            quest.Id = EditorGUILayout.TextField("Id", quest.Id);
            quest.DisplayName = EditorGUILayout.TextField("Display Name", quest.DisplayName);
            quest.Description = EditorGUILayout.TextField("Description", quest.Description);
            quest.TargetMonsterId = DrawMonsterIdField("Target Monster", quest.TargetMonsterId);
            quest.TargetEncounterId = DrawEncounterIdField("Target Encounter", quest.TargetEncounterId);
            quest.MapKind = EditorGUILayout.TextField("Map Kind", quest.MapKind);
            quest.MapProfileId = EditorGUILayout.TextField("Map Profile Id", quest.MapProfileId);
            quest.GoldReward = EditorGUILayout.IntField("Gold Reward", quest.GoldReward);
            quest.XpReward = EditorGUILayout.IntField("XP Reward", quest.XpReward);
            if (EditorGUI.EndChangeCheck())
            {
                MarkDatabaseDirty();
            }

            DrawRewardItems(quest);

            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Quest"))
            {
                if (ConfirmDelete("quest", quest.Id))
                {
                    DeleteQuest(selectedQuestIndex);
                }
            }
        }

        private void DrawRewardItems(ContentQuestDefinition quest)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reward Items", EditorStyles.boldLabel);
            for (var i = 0; i < quest.RewardItems.Length; i++)
            {
                var reward = quest.RewardItems[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    reward.ItemId = EditorGUILayout.TextField("Item Id", reward.ItemId);
                    reward.Quantity = EditorGUILayout.IntField("Qty", reward.Quantity, GUILayout.Width(120f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        MarkDatabaseDirty();
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(90f)))
                    {
                        RemoveRewardItem(quest, i);
                        return;
                    }
                }
            }

            if (GUILayout.Button("Add Reward Item"))
            {
                AddRewardItem(quest);
            }
        }

        private void DrawMonsterTab()
        {
            database.Monsters ??= Array.Empty<ContentMonsterDefinition>();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
                {
                    EditorGUILayout.LabelField("Monsters", EditorStyles.boldLabel);
                    for (var i = 0; i < database.Monsters.Length; i++)
                    {
                        var monster = database.Monsters[i];
                        var label = string.IsNullOrWhiteSpace(monster.Id) ? "(missing id)" : monster.Id;
                        if (!string.IsNullOrWhiteSpace(monster.DisplayName))
                        {
                            label = $"{label} - {monster.DisplayName}";
                        }

                        if (GUILayout.Toggle(selectedMonsterIndex == i, label, "Button"))
                        {
                            selectedMonsterIndex = i;
                        }
                    }

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Create Monster"))
                    {
                        AddMonster();
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawMonsterDetail();
                }
            }
        }

        private void DrawMonsterDetail()
        {
            if (database.Monsters.Length == 0)
            {
                EditorGUILayout.HelpBox("Create a monster to edit its runtime definition.", MessageType.Info);
                return;
            }

            selectedMonsterIndex = Mathf.Clamp(selectedMonsterIndex, 0, database.Monsters.Length - 1);
            var monster = database.Monsters[selectedMonsterIndex];
            EditorGUILayout.LabelField("Monster Detail", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            monster.Id = EditorGUILayout.TextField("Id", monster.Id);
            monster.DisplayName = EditorGUILayout.TextField("Display Name", monster.DisplayName);
            monster.MaxHp = EditorGUILayout.IntField("Max HP", monster.MaxHp);
            monster.AttackPower = EditorGUILayout.IntField("Attack Power", monster.AttackPower);
            monster.Defense = EditorGUILayout.IntField("Defense", monster.Defense);
            monster.XpReward = EditorGUILayout.IntField("XP Reward", monster.XpReward);
            monster.Boss = EditorGUILayout.Toggle("Boss", monster.Boss);
            monster.Ai = EditorGUILayout.TextField("AI", monster.Ai);
            if (EditorGUI.EndChangeCheck())
            {
                MarkDatabaseDirty();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Monster"))
            {
                if (ConfirmDelete("monster", monster.Id))
                {
                    DeleteMonster(selectedMonsterIndex);
                }
            }
        }

        private void DrawEncounterTab()
        {
            database.Encounters ??= Array.Empty<ContentEncounterDefinition>();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(280f)))
                {
                    EditorGUILayout.LabelField("Encounters", EditorStyles.boldLabel);
                    for (var i = 0; i < database.Encounters.Length; i++)
                    {
                        var encounter = database.Encounters[i];
                        var label = string.IsNullOrWhiteSpace(encounter.Id) ? "(missing id)" : encounter.Id;
                        if (!string.IsNullOrWhiteSpace(encounter.DisplayName))
                        {
                            label = $"{label} - {encounter.DisplayName}";
                        }

                        if (GUILayout.Toggle(selectedEncounterIndex == i, label, "Button"))
                        {
                            selectedEncounterIndex = i;
                        }
                    }

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Create Encounter"))
                    {
                        AddEncounter();
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawEncounterDetail();
                }
            }
        }

        private void DrawEncounterDetail()
        {
            if (database.Encounters.Length == 0)
            {
                EditorGUILayout.HelpBox("Create an encounter to edit combat data.", MessageType.Info);
                return;
            }

            selectedEncounterIndex = Mathf.Clamp(selectedEncounterIndex, 0, database.Encounters.Length - 1);
            var encounter = database.Encounters[selectedEncounterIndex];
            encounter.EnemySlots ??= Array.Empty<ContentEncounterEnemySlot>();

            EditorGUILayout.LabelField("Encounter Detail", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            encounter.Id = EditorGUILayout.TextField("Id", encounter.Id);
            encounter.DisplayName = EditorGUILayout.TextField("Display Name", encounter.DisplayName);
            encounter.MonsterId = DrawMonsterIdField("Primary Monster", encounter.MonsterId);
            encounter.Pattern = EditorGUILayout.TextField("Pattern", encounter.Pattern);
            encounter.XpReward = EditorGUILayout.IntField("XP Reward", encounter.XpReward);
            encounter.RewardId = EditorGUILayout.TextField("Reward Id", encounter.RewardId);
            if (EditorGUI.EndChangeCheck())
            {
                MarkDatabaseDirty();
            }

            DrawEnemySlots(encounter);

            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Encounter"))
            {
                if (ConfirmDelete("encounter", encounter.Id))
                {
                    DeleteEncounter(selectedEncounterIndex);
                }
            }
        }

        private void DrawEnemySlots(ContentEncounterDefinition encounter)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Slots", EditorStyles.boldLabel);

            for (var i = 0; i < encounter.EnemySlots.Length; i++)
            {
                var slot = encounter.EnemySlots[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Slot {i + 1}", EditorStyles.boldLabel);
                        if (GUILayout.Button("Remove", GUILayout.Width(90f)))
                        {
                            RemoveEnemySlot(encounter, i);
                            return;
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    slot.SlotId = EditorGUILayout.TextField("Slot Id", slot.SlotId);
                    slot.MonsterId = DrawMonsterIdField("Monster", slot.MonsterId);
                    slot.Count = EditorGUILayout.IntField("Count", slot.Count);
                    slot.Primary = EditorGUILayout.Toggle("Primary", slot.Primary);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MarkDatabaseDirty();
                    }
                }
            }

            if (GUILayout.Button("Add Enemy Slot"))
            {
                AddEnemySlot(encounter);
            }
        }

        private void DrawValidationTab()
        {
            if (validationReport == null)
            {
                EditorGUILayout.HelpBox("Run validation to see content database errors and warnings.", MessageType.Info);
                return;
            }

            var messageType = validationReport.Passed ? MessageType.Info : MessageType.Error;
            EditorGUILayout.HelpBox(
                validationReport.Passed
                    ? $"Validation passed with {validationReport.Warnings.Count} warning(s)."
                    : $"Validation failed with {validationReport.Errors.Count} error(s) and {validationReport.Warnings.Count} warning(s).",
                messageType);

            DrawMessages("Errors", validationReport.Errors, MessageType.Error);
            DrawMessages("Warnings", validationReport.Warnings, MessageType.Warning);
        }

        private void DrawAuthoringTab()
        {
            if (authoringSnapshot == null)
            {
                authoringSnapshot = AuthoringContentBuildService.FindAuthoringAssets();
            }

            EditorGUILayout.LabelField("Authoring Assets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tab is the bridge from Inspector-first authoring assets to runtime-safe ContentDatabase output. Existing DB row editors are retained for bootstrap/fallback editing.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Authoring Assets"))
                {
                    RefreshAuthoringAssets();
                }

                if (GUILayout.Button("Validate Authoring Assets"))
                {
                    ValidateAuthoringAssets();
                }

                using (new EditorGUI.DisabledScope(database == null))
                {
                    if (GUILayout.Button("Bake Authoring Assets Into Database"))
                    {
                        BakeAuthoringAssetsIntoDatabase();
                    }
                }
            }

            EditorGUILayout.Space();
            DrawAuthoringCounts(authoringSnapshot);
            EditorGUILayout.Space();
            DrawAuthoringAssetList("MonsterDefinitionAsset", authoringSnapshot.Monsters);
            DrawAuthoringAssetList("EncounterDefinitionAsset", authoringSnapshot.Encounters);
            DrawAuthoringAssetList("SkillDefinitionAsset", authoringSnapshot.Skills);
            DrawAuthoringAssetList("NpcDefinitionAsset", authoringSnapshot.Npcs);
            DrawAuthoringAssetList("VendorDefinitionAsset", authoringSnapshot.Vendors);
            DrawSpawnTablePreview(authoringSnapshot.SpawnTables);

            EditorGUILayout.Space();
            if (authoringValidationReport != null)
            {
                DrawValidationReport("Authoring Validation", authoringValidationReport);
            }
        }

        private static void DrawAuthoringCounts(AuthoringContentSnapshot snapshot)
        {
            EditorGUILayout.LabelField("Monsters", Count(snapshot.Monsters).ToString());
            EditorGUILayout.LabelField("Encounters", Count(snapshot.Encounters).ToString());
            EditorGUILayout.LabelField("Skills", Count(snapshot.Skills).ToString());
            EditorGUILayout.LabelField("NPCs", Count(snapshot.Npcs).ToString());
            EditorGUILayout.LabelField("Vendors", Count(snapshot.Vendors).ToString());
            EditorGUILayout.LabelField("Spawn Tables", Count(snapshot.SpawnTables).ToString());
        }

        private static void DrawAuthoringAssetList<T>(string label, T[] assets) where T : UnityEngine.Object
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            if (assets == null || assets.Length == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            foreach (var asset in assets)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(asset, typeof(T), false);
                    if (GUILayout.Button("Ping", GUILayout.Width(60f)))
                    {
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }
            }
        }

        private static void DrawSpawnTablePreview(SpawnTableAsset[] spawnTables)
        {
            EditorGUILayout.LabelField("SpawnTableAsset", EditorStyles.boldLabel);
            if (spawnTables == null || spawnTables.Length == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            var profileUsage = BuildSpawnTableUsage();
            foreach (var spawnTable in spawnTables)
            {
                if (spawnTable == null)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(spawnTable, typeof(SpawnTableAsset), false);
                        if (GUILayout.Button("Ping", GUILayout.Width(60f)))
                        {
                            EditorGUIUtility.PingObject(spawnTable);
                            Selection.activeObject = spawnTable;
                        }
                    }

                    var tableId = string.IsNullOrWhiteSpace(spawnTable.Id) ? "(missing id)" : spawnTable.Id;
                    EditorGUILayout.LabelField("Id", tableId);
                    EditorGUILayout.LabelField("Pool", $"{Count(spawnTable.EncounterEntries)} encounter entries, {Count(spawnTable.DirectMonsterEntries)} direct monster entries");
                    EditorGUILayout.LabelField("Required Tags", FormatTags(spawnTable.RequiredThemeTags, spawnTable.RequiredBiomeTags, spawnTable.RequiredSpawnRoleTags));
                    EditorGUILayout.LabelField("Allowed Room Roles", FormatTags(spawnTable.AllowedRoomRoles));
                    EditorGUILayout.LabelField("Resolved Members", FormatSpawnMembers(spawnTable));
                    EditorGUILayout.LabelField("Used By Map Profiles", FormatUsage(profileUsage, spawnTable));
                }
            }
        }

        private static Dictionary<SpawnTableAsset, List<string>> BuildSpawnTableUsage()
        {
            var usage = new Dictionary<SpawnTableAsset, List<string>>();
            foreach (var guid in AssetDatabase.FindAssets("t:MapProfileAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<MapProfileAsset>(path);
                if (profile == null)
                {
                    continue;
                }

                var profileId = string.IsNullOrWhiteSpace(profile.Id) ? profile.name : profile.Id;
                foreach (var spawnTable in profile.AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
                {
                    if (spawnTable == null)
                    {
                        continue;
                    }

                    if (!usage.TryGetValue(spawnTable, out var profiles))
                    {
                        profiles = new List<string>();
                        usage.Add(spawnTable, profiles);
                    }

                    if (!profiles.Contains(profileId))
                    {
                        profiles.Add(profileId);
                    }
                }
            }

            return usage;
        }

        private static string FormatSpawnMembers(SpawnTableAsset spawnTable)
        {
            var members = new List<string>();
            foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
            {
                var encounterId = entry.Encounter != null && !string.IsNullOrWhiteSpace(entry.Encounter.Id)
                    ? entry.Encounter.Id
                    : entry.EncounterId;
                if (!string.IsNullOrWhiteSpace(encounterId))
                {
                    members.Add($"{encounterId} w={entry.Weight} f={entry.MinFloor}-{entry.MaxFloor}");
                }
            }

            foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
            {
                var monsterId = entry.Monster != null && !string.IsNullOrWhiteSpace(entry.Monster.Id)
                    ? entry.Monster.Id
                    : entry.MonsterId;
                if (!string.IsNullOrWhiteSpace(monsterId))
                {
                    members.Add($"{monsterId} -> generated single-primary w={entry.Weight} f={entry.MinFloor}-{entry.MaxFloor}");
                }
            }

            return members.Count == 0 ? "(empty)" : string.Join(", ", members);
        }

        private static string FormatUsage(Dictionary<SpawnTableAsset, List<string>> usage, SpawnTableAsset spawnTable)
        {
            return usage.TryGetValue(spawnTable, out var profiles) && profiles.Count > 0
                ? string.Join(", ", profiles)
                : "(unused)";
        }

        private static string FormatTags(params string[][] tagSets)
        {
            var tags = new List<string>();
            foreach (var tagSet in tagSets ?? Array.Empty<string[]>())
            {
                foreach (var tag in tagSet ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(tag) && !tags.Contains(tag))
                    {
                        tags.Add(tag);
                    }
                }
            }

            return tags.Count == 0 ? "(none)" : string.Join(", ", tags);
        }

        private void DrawValidationReport(string label, ContentValidationReport report)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            var messageType = report.Passed ? MessageType.Info : MessageType.Error;
            EditorGUILayout.HelpBox(
                report.Passed
                    ? $"Validation passed with {report.Warnings.Count} warning(s)."
                    : $"Validation failed with {report.Errors.Count} error(s) and {report.Warnings.Count} warning(s).",
                messageType);

            DrawMessages("Errors", report.Errors, MessageType.Error);
            DrawMessages("Warnings", report.Warnings, MessageType.Warning);
        }

        private static void DrawMessages(string label, System.Collections.Generic.IReadOnlyList<string> messages, MessageType messageType)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            if (messages.Count == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            for (var i = 0; i < messages.Count; i++)
            {
                EditorGUILayout.HelpBox(messages[i], messageType);
            }
        }

        private void CreateDatabaseAsset()
        {
            var path = string.IsNullOrWhiteSpace(createAssetPath) ? DefaultNewDatabasePath : createAssetPath.Trim();
            EnsureFolderForAsset(path);
            var existing = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(path);
            if (existing != null)
            {
                database = existing;
                validationReport = null;
                return;
            }

            database = CreateInstance<ContentDatabaseDefinition>();
            AssetDatabase.CreateAsset(database, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            validationReport = null;
        }

        private void SaveDatabase()
        {
            var report = ContentDatabaseValidator.Validate(database);
            validationReport = report;
            if (!report.Passed)
            {
                EditorUtility.DisplayDialog(
                    "Content Database Validation Failed",
                    $"Fix {report.Errors.Count} validation error(s) before saving. See the Validation tab for details.",
                    "OK");
                selectedTab = Array.IndexOf(TabNames, "Validation");
                return;
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private string ImportTargetPath()
        {
            if (database != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(database);
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    return assetPath;
                }
            }

            return string.IsNullOrWhiteSpace(createAssetPath)
                ? DefaultNewDatabasePath
                : createAssetPath.Trim();
        }

        private static bool ConfirmDelete(string kind, string id)
        {
            var displayId = string.IsNullOrWhiteSpace(id) ? "(missing id)" : id;
            return EditorUtility.DisplayDialog(
                $"Delete {kind}",
                $"Delete {kind} '{displayId}'? Existing references are not automatically rewritten; validation will report broken links.",
                "Delete",
                "Cancel");
        }

        private void RunValidation()
        {
            validationReport = ContentDatabaseValidator.Validate(database);
            LogReport(validationReport);
        }

        private void RefreshAuthoringAssets()
        {
            authoringSnapshot = AuthoringContentBuildService.FindAuthoringAssets();
            authoringValidationReport = null;
        }

        private void ValidateAuthoringAssets()
        {
            authoringSnapshot = AuthoringContentBuildService.FindAuthoringAssets();
            authoringValidationReport = AuthoringContentBuildService.Validate(authoringSnapshot);
            LogReport(authoringValidationReport);
        }

        private void BakeAuthoringAssetsIntoDatabase()
        {
            authoringSnapshot = AuthoringContentBuildService.FindAuthoringAssets();
            authoringValidationReport = AuthoringContentBuildService.BakeInto(database, authoringSnapshot);
            LogReport(authoringValidationReport);
            validationReport = ContentDatabaseValidator.Validate(database);
            if (authoringValidationReport.Passed)
            {
                AssetDatabase.SaveAssets();
                selectedTab = Array.IndexOf(TabNames, "Validation");
            }
        }

        private static void EnsureFolderForAsset(string assetPath)
        {
            var slash = assetPath.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            EnsureFolder(assetPath.Substring(0, slash));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = slash > 0 ? path.Substring(0, slash) : "Assets";
            var folder = slash > 0 ? path.Substring(slash + 1) : path;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private void AddQuest()
        {
            var quests = database.Quests ?? Array.Empty<ContentQuestDefinition>();
            var nextIndex = quests.Length + 1;
            var encounterId = FirstEncounterId();
            var monsterId = MonsterIdForEncounter(encounterId);
            if (string.IsNullOrWhiteSpace(monsterId))
            {
                monsterId = FirstMonsterId();
            }

            Array.Resize(ref quests, quests.Length + 1);
            quests[quests.Length - 1] = new ContentQuestDefinition
            {
                Id = UniqueQuestId($"quest_new_{nextIndex}"),
                DisplayName = "New Quest",
                TargetMonsterId = monsterId,
                TargetEncounterId = encounterId,
                MapKind = "dungeon",
                MapProfileId = Conn.Core.Maps.MapGenerationCatalog.ChapterTwoFirstSliceProfileId,
                GoldReward = 1,
                XpReward = 0,
                RewardItems = Array.Empty<ContentItemStack>()
            };

            database.Quests = quests;
            selectedQuestIndex = quests.Length - 1;
            MarkDatabaseDirty();
        }

        private void DeleteQuest(int index)
        {
            var quests = database.Quests ?? Array.Empty<ContentQuestDefinition>();
            if (index < 0 || index >= quests.Length)
            {
                return;
            }

            var next = new ContentQuestDefinition[quests.Length - 1];
            for (int source = 0, target = 0; source < quests.Length; source++)
            {
                if (source == index)
                {
                    continue;
                }

                next[target++] = quests[source];
            }

            database.Quests = next;
            selectedQuestIndex = Mathf.Clamp(index, 0, Math.Max(0, next.Length - 1));
            MarkDatabaseDirty();
        }

        private void AddRewardItem(ContentQuestDefinition quest)
        {
            var rewards = quest.RewardItems ?? Array.Empty<ContentItemStack>();
            Array.Resize(ref rewards, rewards.Length + 1);
            rewards[rewards.Length - 1] = new ContentItemStack
            {
                Quantity = 1
            };
            quest.RewardItems = rewards;
            MarkDatabaseDirty();
        }

        private void RemoveRewardItem(ContentQuestDefinition quest, int index)
        {
            var rewards = quest.RewardItems ?? Array.Empty<ContentItemStack>();
            if (index < 0 || index >= rewards.Length)
            {
                return;
            }

            var next = new ContentItemStack[rewards.Length - 1];
            for (int source = 0, target = 0; source < rewards.Length; source++)
            {
                if (source == index)
                {
                    continue;
                }

                next[target++] = rewards[source];
            }

            quest.RewardItems = next;
            MarkDatabaseDirty();
        }

        private void AddMonster()
        {
            var monsters = database.Monsters ?? Array.Empty<ContentMonsterDefinition>();
            var nextIndex = monsters.Length + 1;
            Array.Resize(ref monsters, monsters.Length + 1);
            monsters[monsters.Length - 1] = new ContentMonsterDefinition
            {
                Id = UniqueMonsterId($"monster_new_{nextIndex}"),
                DisplayName = "New Monster",
                MaxHp = 1,
                AttackPower = 1,
                XpReward = 0,
                Ai = "Attack"
            };

            database.Monsters = monsters;
            selectedMonsterIndex = monsters.Length - 1;
            MarkDatabaseDirty();
        }

        private void DeleteMonster(int index)
        {
            var monsters = database.Monsters ?? Array.Empty<ContentMonsterDefinition>();
            if (index < 0 || index >= monsters.Length)
            {
                return;
            }

            var next = new ContentMonsterDefinition[monsters.Length - 1];
            for (int source = 0, target = 0; source < monsters.Length; source++)
            {
                if (source == index)
                {
                    continue;
                }

                next[target++] = monsters[source];
            }

            database.Monsters = next;
            selectedMonsterIndex = Mathf.Clamp(index, 0, Math.Max(0, next.Length - 1));
            MarkDatabaseDirty();
        }

        private void AddEncounter()
        {
            var encounters = database.Encounters ?? Array.Empty<ContentEncounterDefinition>();
            var nextIndex = encounters.Length + 1;
            var primaryMonsterId = FirstMonsterId();
            Array.Resize(ref encounters, encounters.Length + 1);
            encounters[encounters.Length - 1] = new ContentEncounterDefinition
            {
                Id = UniqueEncounterId($"encounter_new_{nextIndex}"),
                DisplayName = "New Encounter",
                MonsterId = primaryMonsterId,
                XpReward = 0,
                Pattern = "single_primary",
                EnemySlots = string.IsNullOrWhiteSpace(primaryMonsterId)
                    ? Array.Empty<ContentEncounterEnemySlot>()
                    : new[]
                    {
                        new ContentEncounterEnemySlot
                        {
                            SlotId = "primary",
                            MonsterId = primaryMonsterId,
                            Count = 1,
                            Primary = true
                        }
                    }
            };

            database.Encounters = encounters;
            selectedEncounterIndex = encounters.Length - 1;
            MarkDatabaseDirty();
        }

        private void DeleteEncounter(int index)
        {
            var encounters = database.Encounters ?? Array.Empty<ContentEncounterDefinition>();
            if (index < 0 || index >= encounters.Length)
            {
                return;
            }

            var next = new ContentEncounterDefinition[encounters.Length - 1];
            for (int source = 0, target = 0; source < encounters.Length; source++)
            {
                if (source == index)
                {
                    continue;
                }

                next[target++] = encounters[source];
            }

            database.Encounters = next;
            selectedEncounterIndex = Mathf.Clamp(index, 0, Math.Max(0, next.Length - 1));
            MarkDatabaseDirty();
        }

        private void AddEnemySlot(ContentEncounterDefinition encounter)
        {
            var slots = encounter.EnemySlots ?? Array.Empty<ContentEncounterEnemySlot>();
            var monsterId = string.IsNullOrWhiteSpace(encounter.MonsterId) ? FirstMonsterId() : encounter.MonsterId;
            Array.Resize(ref slots, slots.Length + 1);
            slots[slots.Length - 1] = new ContentEncounterEnemySlot
            {
                SlotId = UniqueSlotId(slots, $"slot_{slots.Length}"),
                MonsterId = monsterId,
                Count = 1,
                Primary = slots.Length == 1
            };
            encounter.EnemySlots = slots;
            MarkDatabaseDirty();
        }

        private void RemoveEnemySlot(ContentEncounterDefinition encounter, int index)
        {
            var slots = encounter.EnemySlots ?? Array.Empty<ContentEncounterEnemySlot>();
            if (index < 0 || index >= slots.Length)
            {
                return;
            }

            var next = new ContentEncounterEnemySlot[slots.Length - 1];
            for (int source = 0, target = 0; source < slots.Length; source++)
            {
                if (source == index)
                {
                    continue;
                }

                next[target++] = slots[source];
            }

            encounter.EnemySlots = next;
            MarkDatabaseDirty();
        }

        private string UniqueMonsterId(string baseId)
        {
            var candidate = baseId;
            var suffix = 2;
            while (MonsterIdExists(candidate))
            {
                candidate = $"{baseId}_{suffix++}";
            }

            return candidate;
        }

        private bool MonsterIdExists(string id)
        {
            foreach (var monster in database.Monsters ?? Array.Empty<ContentMonsterDefinition>())
            {
                if (monster.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private string DrawMonsterIdField(string label, string current)
        {
            var monsterIds = MonsterIds();
            if (monsterIds.Length == 0)
            {
                return EditorGUILayout.TextField(label, current);
            }

            var selected = 0;
            for (var i = 0; i < monsterIds.Length; i++)
            {
                if (monsterIds[i] == current)
                {
                    selected = i + 1;
                    break;
                }
            }

            var options = new string[monsterIds.Length + 1];
            options[0] = string.IsNullOrWhiteSpace(current) ? "(None)" : $"Custom: {current}";
            Array.Copy(monsterIds, 0, options, 1, monsterIds.Length);
            var next = EditorGUILayout.Popup(label, selected, options);
            return next == 0 ? current : monsterIds[next - 1];
        }

        private string[] MonsterIds()
        {
            var monsters = database.Monsters ?? Array.Empty<ContentMonsterDefinition>();
            var ids = new System.Collections.Generic.List<string>();
            foreach (var monster in monsters)
            {
                if (!string.IsNullOrWhiteSpace(monster.Id))
                {
                    ids.Add(monster.Id);
                }
            }

            return ids.ToArray();
        }

        private string FirstMonsterId()
        {
            var ids = MonsterIds();
            return ids.Length > 0 ? ids[0] : string.Empty;
        }

        private string DrawEncounterIdField(string label, string current)
        {
            var encounterIds = EncounterIds();
            if (encounterIds.Length == 0)
            {
                return EditorGUILayout.TextField(label, current);
            }

            var selected = 0;
            for (var i = 0; i < encounterIds.Length; i++)
            {
                if (encounterIds[i] == current)
                {
                    selected = i + 1;
                    break;
                }
            }

            var options = new string[encounterIds.Length + 1];
            options[0] = string.IsNullOrWhiteSpace(current) ? "(None)" : $"Custom: {current}";
            Array.Copy(encounterIds, 0, options, 1, encounterIds.Length);
            var next = EditorGUILayout.Popup(label, selected, options);
            return next == 0 ? current : encounterIds[next - 1];
        }

        private string[] EncounterIds()
        {
            var encounters = database.Encounters ?? Array.Empty<ContentEncounterDefinition>();
            var ids = new System.Collections.Generic.List<string>();
            foreach (var encounter in encounters)
            {
                if (!string.IsNullOrWhiteSpace(encounter.Id))
                {
                    ids.Add(encounter.Id);
                }
            }

            return ids.ToArray();
        }

        private string FirstEncounterId()
        {
            var ids = EncounterIds();
            return ids.Length > 0 ? ids[0] : string.Empty;
        }

        private string MonsterIdForEncounter(string encounterId)
        {
            foreach (var encounter in database.Encounters ?? Array.Empty<ContentEncounterDefinition>())
            {
                if (encounter.Id == encounterId)
                {
                    return encounter.MonsterId;
                }
            }

            return string.Empty;
        }

        private string UniqueEncounterId(string baseId)
        {
            var candidate = baseId;
            var suffix = 2;
            while (EncounterIdExists(candidate))
            {
                candidate = $"{baseId}_{suffix++}";
            }

            return candidate;
        }

        private bool EncounterIdExists(string id)
        {
            foreach (var encounter in database.Encounters ?? Array.Empty<ContentEncounterDefinition>())
            {
                if (encounter.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private string UniqueQuestId(string baseId)
        {
            var candidate = baseId;
            var suffix = 2;
            while (QuestIdExists(candidate))
            {
                candidate = $"{baseId}_{suffix++}";
            }

            return candidate;
        }

        private bool QuestIdExists(string id)
        {
            foreach (var quest in database.Quests ?? Array.Empty<ContentQuestDefinition>())
            {
                if (quest.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static string UniqueSlotId(ContentEncounterEnemySlot[] slots, string baseId)
        {
            var candidate = baseId;
            var suffix = 2;
            while (SlotIdExists(slots, candidate))
            {
                candidate = $"{baseId}_{suffix++}";
            }

            return candidate;
        }

        private static bool SlotIdExists(ContentEncounterEnemySlot[] slots, string id)
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.SlotId == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void MarkDatabaseDirty()
        {
            EditorUtility.SetDirty(database);
            validationReport = null;
        }

        private static int Count<T>(T[] values)
        {
            return values?.Length ?? 0;
        }

        private static int CountVendorRotations(ContentDatabaseDefinition database)
        {
            var count = 0;
            foreach (var vendor in database.Vendors ?? Array.Empty<ContentVendorDefinition>())
            {
                count += vendor.Rotations?.Length ?? 0;
            }

            return count;
        }

        private static int CountVendorCatalogReferences(ContentDatabaseDefinition database)
        {
            var count = 0;
            foreach (var vendor in database.Vendors ?? Array.Empty<ContentVendorDefinition>())
            {
                count += vendor.CatalogIds?.Length ?? 0;
                foreach (var rotation in vendor.Rotations ?? System.Array.Empty<ContentVendorRotationDefinition>())
                {
                    count += rotation.CatalogIds?.Length ?? 0;
                }
            }

            return count;
        }

        private static int CountQuestEncounterMonsterLinks(ContentDatabaseDefinition database)
        {
            try
            {
                var registry = database.BuildRegistry();
                var count = 0;
                foreach (var quest in database.Quests ?? Array.Empty<ContentQuestDefinition>())
                {
                    var encounter = registry.FindEncounter(quest.TargetEncounterId);
                    if (encounter != null && encounter.MonsterId == quest.TargetMonsterId && registry.FindMonster(quest.TargetMonsterId) != null)
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static void LogReport(ContentValidationReport report)
        {
            foreach (var warning in report.Warnings)
            {
                Debug.LogWarning(warning);
            }

            if (report.Passed)
            {
                Debug.Log($"Content database validation passed with {report.Warnings.Count} warning(s).");
                return;
            }

            foreach (var error in report.Errors)
            {
                Debug.LogError(error);
            }
        }
    }
}

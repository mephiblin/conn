using Conn.Core.Content;
using Conn.Core.Maps;
using Conn.Core.Quests;
using Conn.Editor.Content;
using Conn.Editor.Tools;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class ChapterTwoBuildValidator
    {
        public const string DefaultCompiledMapAssetPath = "Assets/Conn/Core/Maps/ch2_first_slice_ruins_2001_CompiledMap.asset";

        [MenuItem("Conn/Build & Validate Chapter 2")]
        public static void BuildAndValidateChapterTwo()
        {
            var database = ImportAndValidateChapterTwoContentSlice();
            BuildAndValidateChapterTwoMapSlice(database);
            RuntimeRuleVerifier.VerifyChapterTwoRuntimeDataConsumption();
            Debug.Log("Conn Chapter 2 data and editor pipeline validation passed.");
        }

        [MenuItem("Conn/Build & Validate Chapter 2/Content Slice")]
        public static void BuildAndValidateChapterTwoContentSlice()
        {
            ImportAndValidateChapterTwoContentSlice();
        }

        private static ContentDatabaseDefinition ImportAndValidateChapterTwoContentSlice()
        {
            var database = LegacyContentJsonImporter.Import(
                LegacyContentJsonImporter.DefaultLegacyDataPath,
                LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            var report = Conn.Core.Content.ContentDatabaseValidator.Validate(database);
            if (!report.Passed)
            {
                throw new System.InvalidOperationException(string.Join("\n", report.Errors));
            }

            Debug.Log($"Conn Chapter 2 content slice validation passed. items={database.Items.Length} skills={database.Skills.Length} monsters={database.Monsters.Length} encounters={database.Encounters.Length} quests={database.Quests.Length} vendors={database.Vendors.Length} npcs={database.Npcs.Length}");
            return database;
        }

        [MenuItem("Conn/Build & Validate Chapter 2/Map Slice")]
        public static void BuildAndValidateChapterTwoMapSlice()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            BuildAndValidateChapterTwoMapSlice(database);
        }

        private static void BuildAndValidateChapterTwoMapSlice(ContentDatabaseDefinition database)
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = MapGenerationService.Generate(profile, chunks, 2001);
            var report = MapValidationService.Validate(profile, draft);
            MapValidationService.ThrowIfFailed(report);
            var compiled = MapGenerationService.Compile(profile, draft);
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(profile, compiled));
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateQuestMapContract(QuestCatalog.Find(QuestCatalog.TestHuntId), profile, compiled));
            ValidateDatabaseQuestMapContracts(database, profile, compiled);
            SaveCompiledMapAsset(compiled, 2001);

            if (compiled.Placements.Count < profile.RequiredAnchors.Count)
            {
                throw new System.InvalidOperationException("Compiled map is missing required placements.");
            }

            Debug.Log($"Conn Chapter 2 map slice validation passed. compiledMap={compiled.MapId} rooms={compiled.Rooms.Count} placements={compiled.Placements.Count}");
        }

        private static void ValidateDatabaseQuestMapContracts(ContentDatabaseDefinition database, MapProfile profile, CompiledMap compiled)
        {
            if (database == null || database.Quests == null)
            {
                return;
            }

            foreach (var contentQuest in database.Quests)
            {
                if (contentQuest == null)
                {
                    continue;
                }

                var quest = new QuestDefinition(
                    contentQuest.Id,
                    contentQuest.DisplayName,
                    contentQuest.TargetMonsterId,
                    contentQuest.GoldReward,
                    contentQuest.MapProfileId,
                    MapPlacementKind.QuestTarget,
                    contentQuest.TargetEncounterId);
                MapValidationService.ThrowIfFailed(MapValidationService.ValidateQuestMapContract(quest, profile, compiled));
            }
        }

        public static CompiledMapAsset SaveCompiledMapAsset(CompiledMap compiled, int seed)
        {
            var asset = AssetDatabase.LoadAssetAtPath<CompiledMapAsset>(DefaultCompiledMapAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CompiledMapAsset>();
                AssetDatabase.CreateAsset(asset, DefaultCompiledMapAssetPath);
            }

            asset.ProfileId = compiled.ProfileId;
            asset.Seed = seed;
            asset.Json = JsonUtility.ToJson(compiled, true);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}

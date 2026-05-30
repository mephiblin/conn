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
        private static readonly int[] RequiredCompiledMapSeeds = { 2001, 2112 };
        public const string DefaultCompiledMapAssetPath = "Assets/Conn/Core/Maps/ch2_first_slice_ruins_2001_CompiledMap.asset";

        [MenuItem("Conn/Legacy/Maps/Debug/Build & Validate Chapter 2 All")]
        public static void BuildAndValidateChapterTwo()
        {
            var database = ImportAndValidateChapterTwoContentSlice();
            BuildAndValidateChapterTwoMapSlice(database);
            RuntimeRuleVerifier.VerifyChapterTwoRuntimeDataConsumption();
            Debug.Log("Conn Chapter 2 data and editor pipeline validation passed.");
        }

        [MenuItem("Conn/Legacy/Maps/Debug/Build & Validate Chapter 2/Content Slice")]
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

        [MenuItem("Conn/Legacy/Maps/Debug/Build & Validate Chapter 2/Map Slice")]
        public static void BuildAndValidateChapterTwoMapSlice()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            BuildAndValidateChapterTwoMapSlice(database);
        }

        private static void BuildAndValidateChapterTwoMapSlice(ContentDatabaseDefinition database)
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var savedCount = 0;
            CompiledMap lastCompiled = null;
            foreach (var seed in RequiredCompiledMapSeeds)
            {
                var draft = EditableMapDraftBuilder.BuildGeneratedDraft(profile, chunks, seed, floor: 1, difficulty: 0, cellSize: 0.28f, heightStep: 0.28f);
                var compiled = EditableMapBakeService.Bake(draft);
                var compiledProfile = BuildCompiledValidationProfile(profile, compiled);
                MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(compiledProfile, compiled));
                MapValidationService.ThrowIfFailed(MapValidationService.ValidateQuestMapContract(QuestCatalog.Find(QuestCatalog.TestHuntId), compiledProfile, compiled));
                ValidateDatabaseQuestMapContracts(database, compiledProfile, compiled);
                SaveCompiledMapAsset(compiled, seed);
                Object.DestroyImmediate(draft);
                savedCount++;
                lastCompiled = compiled;
            }

            SaveRuntimeMapGenerationBundleAsset();

            if (lastCompiled == null || lastCompiled.Placements.Count < profile.RequiredAnchors.Count)
            {
                throw new System.InvalidOperationException("Compiled map is missing required placements.");
            }

            Debug.Log($"Conn Chapter 2 map slice validation passed. compiledMaps={savedCount} lastMap={lastCompiled.MapId} rooms={lastCompiled.Rooms.Count} placements={lastCompiled.Placements.Count}");
        }

        private static MapProfile BuildCompiledValidationProfile(MapProfile sourceProfile, CompiledMap compiled)
        {
            return new MapProfile
            {
                ProfileId = sourceProfile.ProfileId,
                MapKind = sourceProfile.MapKind,
                Theme = sourceProfile.Theme,
                Width = compiled.Width,
                Height = compiled.Height,
                RequiredAnchors = new System.Collections.Generic.List<MapAnchorKind>(sourceProfile.RequiredAnchors)
            };
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
            var assetPath = $"Assets/Conn/Core/Maps/{compiled.ProfileId}_{seed}_CompiledMap.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CompiledMapAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CompiledMapAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.ProfileId = compiled.ProfileId;
            asset.Seed = seed;
            asset.Json = JsonUtility.ToJson(compiled, true);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static RuntimeMapGenerationBundleAsset SaveRuntimeMapGenerationBundleAsset()
        {
            var bundle = RuntimeMapGenerationBundleBuilder.BuildChapterTwoCatalogBundle();
            var asset = RuntimeMapGenerationBundleBuilder.SaveBundleAsset(bundle);
            var generated = RuntimeMapGenerationService.GenerateCompiled(bundle, MapGenerationCatalog.ChapterTwoFirstSliceProfileId, 2001);
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(MapGenerationCatalog.ChapterTwoFirstSliceProfile(), generated));
            return asset;
        }
    }
}

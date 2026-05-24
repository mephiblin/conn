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
            BuildAndValidateChapterTwoContentSlice();
            BuildAndValidateChapterTwoMapSlice();
            RuntimeRuleVerifier.VerifyChapterTwoRuntimeDataConsumption();
            Debug.Log("Conn Chapter 2 data and editor pipeline validation passed.");
        }

        [MenuItem("Conn/Build & Validate Chapter 2/Content Slice")]
        public static void BuildAndValidateChapterTwoContentSlice()
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
        }

        [MenuItem("Conn/Build & Validate Chapter 2/Map Slice")]
        public static void BuildAndValidateChapterTwoMapSlice()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = MapGenerationService.Generate(profile, chunks, 2001);
            var report = MapValidationService.Validate(profile, draft);
            MapValidationService.ThrowIfFailed(report);
            var compiled = MapGenerationService.Compile(profile, draft);
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(profile, compiled));
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateQuestMapContract(QuestCatalog.Find(QuestCatalog.TestHuntId), profile, compiled));
            SaveCompiledMapAsset(compiled, 2001);

            if (compiled.Placements.Count < profile.RequiredAnchors.Count)
            {
                throw new System.InvalidOperationException("Compiled map is missing required placements.");
            }

            Debug.Log($"Conn Chapter 2 map slice validation passed. compiledMap={compiled.MapId} rooms={compiled.Rooms.Count} placements={compiled.Placements.Count}");
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

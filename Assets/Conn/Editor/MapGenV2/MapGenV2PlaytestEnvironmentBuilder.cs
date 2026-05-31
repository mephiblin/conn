using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using Conn.Runtime.Scenes;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenV2PlaytestEnvironmentBuilder
    {
        public const string BakedMapFolder = "Assets/Conn/Core/MapGenV2/PlaytestBakedMaps";
        private const string DungeonScenePath = "Assets/Conn/Scenes/Dungeon.unity";
        private const string TempRoot = "Assets/Conn/Authoring/MapGenV2/PlaytestBuildTemp";

        private static readonly PlaytestMapDefinition[] Definitions =
        {
            new PlaytestMapDefinition(
                "mapgenv2_playtest_starter",
                "MapGenV2 Starter Playtest",
                2001,
                new Vector2Int(10, 8)),
            new PlaytestMapDefinition(
                "mapgenv2_playtest_branching",
                "MapGenV2 Branching Playtest",
                2112,
                new Vector2Int(12, 9)),
            new PlaytestMapDefinition(
                "mapgenv2_playtest_depths",
                "MapGenV2 Depths Playtest",
                2441,
                new Vector2Int(14, 10))
        };

        public static IReadOnlyList<string> PlaytestProfileIds
        {
            get
            {
                var ids = new string[Definitions.Length];
                for (var i = 0; i < Definitions.Length; i++)
                {
                    ids[i] = Definitions[i].ProfileId;
                }

                return ids;
            }
        }

        [MenuItem("Conn/MapGenV2/Build Playtest Baked Maps")]
        public static void BuildPlaytestBakedMapsFromMenu()
        {
            var built = BuildPlaytestBakedMaps();
            var report = ValidatePlaytestBakedMaps();
            if (!report.IsValid)
            {
                throw new InvalidOperationException(MapGenV2BuildValidation.Format(report));
            }

            Debug.Log($"Built {built.Count} MapGenV2 playtest baked maps in {BakedMapFolder}.");
        }

        [MenuItem("Conn/MapGenV2/Validate Playtest Baked Maps")]
        public static void ValidatePlaytestBakedMapsFromMenu()
        {
            var report = ValidatePlaytestBakedMaps();
            if (!report.IsValid)
            {
                throw new InvalidOperationException(MapGenV2BuildValidation.Format(report));
            }

            Debug.Log($"MapGenV2 playtest baked map validation passed for {Definitions.Length} profiles.");
        }

        [MenuItem("Conn/MapGenV2/Build Full Playtest Environment")]
        public static void BuildFullPlaytestEnvironment()
        {
            var built = BuildPlaytestBakedMaps();
            var report = ValidatePlaytestBakedMaps();
            if (!report.IsValid)
            {
                throw new InvalidOperationException(MapGenV2BuildValidation.Format(report));
            }

            InvokePlaytestContentBake();
            var bakedMaps = LoadPlaytestBakedMaps();
            var updated = BindPlaytestBakedMapsToScene(DungeonScenePath, bakedMaps);
            if (updated == 0)
            {
                throw new InvalidOperationException($"No SceneBootstrap with a mapGenV2BakedMaps field was found in {DungeonScenePath}.");
            }

            Debug.Log($"MapGenV2 full playtest environment built: {built.Count} baked maps, playtest content baked, {DungeonScenePath} updated.");
        }

        [MenuItem("Conn/MapGenV2/Bind Playtest Baked Maps To Open SceneBootstrap")]
        public static void BindPlaytestBakedMapsToOpenSceneBootstrapsFromMenu()
        {
            var bakedMaps = LoadPlaytestBakedMaps();
            var updated = BindPlaytestBakedMapsToOpenSceneBootstraps(bakedMaps);
            if (updated == 0)
            {
                throw new InvalidOperationException("No open SceneBootstrap with a mapGenV2BakedMaps field was found.");
            }

            Debug.Log($"Bound {bakedMaps.Length} MapGenV2 playtest baked maps to {updated} open SceneBootstrap object(s). Save the scene to keep the binding.");
        }

        public static IReadOnlyList<MapGenBakedMapAsset> BuildPlaytestBakedMaps()
        {
            MapGenV2AssetFolderUtility.EnsureAssetFolder(BakedMapFolder);
            DeleteExistingPlaytestBakedMaps();
            AssetDatabase.DeleteAsset(TempRoot);

            var built = new List<MapGenBakedMapAsset>();
            try
            {
                foreach (var definition in Definitions)
                {
                    var setupRoot = $"{TempRoot}/{definition.ProfileId}";
                    var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup(
                        setupRoot,
                        definition.GridSize,
                        1f);
                    ConfigureSetup(setup, definition);

                    var generationReport = setup.Draft.GenerateFromProfile();
                    ThrowIfInvalid(generationReport, $"Failed to generate {definition.ProfileId}.");
                    setup.Draft.ApplyPostProcessingFromProfile();
                    StampRuntimeMarkers(setup.Draft);
                    setup.Draft.Accept();

                    var baked = MapGenRuntimeBakeUtility.Bake(setup.Draft);
                    if (baked == null)
                    {
                        throw new InvalidOperationException($"Failed to bake {definition.ProfileId}.");
                    }

                    built.Add(baked);
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                AssetDatabase.SaveAssets();
                MapGenV2AssetDatabasePolicy.RefreshAfterBulkAssetChanges();
            }

            return built;
        }

        public static int BindPlaytestBakedMapsToScene(string scenePath, MapGenBakedMapAsset[] bakedMaps)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException("Scene path is required.", nameof(scenePath));
            }

            if (bakedMaps == null || bakedMaps.Length == 0)
            {
                throw new ArgumentException("At least one baked map is required.", nameof(bakedMaps));
            }

            for (var i = 0; i < bakedMaps.Length; i++)
            {
                if (bakedMaps[i] == null)
                {
                    throw new ArgumentException($"Baked map at index {i} is null.", nameof(bakedMaps));
                }
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var updated = BindPlaytestBakedMapsToOpenSceneBootstraps(bakedMaps);
            if (updated > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return updated;
        }

        public static MapGenValidationReport ValidatePlaytestBakedMaps()
        {
            var report = new MapGenValidationReport();
            foreach (var definition in Definitions)
            {
                var path = BakedMapPath(definition);
                var baked = AssetDatabase.LoadAssetAtPath<MapGenBakedMapAsset>(path);
                ValidateBakedMap(report, definition, path, baked);
            }

            return report;
        }

        public static MapGenBakedMapAsset[] LoadPlaytestBakedMaps()
        {
            var report = ValidatePlaytestBakedMaps();
            if (!report.IsValid)
            {
                throw new InvalidOperationException(MapGenV2BuildValidation.Format(report));
            }

            var assets = new MapGenBakedMapAsset[Definitions.Length];
            for (var i = 0; i < Definitions.Length; i++)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<MapGenBakedMapAsset>(BakedMapPath(Definitions[i]));
            }

            return assets;
        }

        public static string BakedMapPathForProfile(string profileId)
        {
            foreach (var definition in Definitions)
            {
                if (string.Equals(definition.ProfileId, profileId, StringComparison.Ordinal))
                {
                    return BakedMapPath(definition);
                }
            }

            return string.Empty;
        }

        public static int BindPlaytestBakedMapsToOpenSceneBootstraps(MapGenBakedMapAsset[] bakedMaps)
        {
            if (bakedMaps == null || bakedMaps.Length == 0)
            {
                return 0;
            }

            var updated = 0;
            foreach (var bootstrap in Resources.FindObjectsOfTypeAll<SceneBootstrap>())
            {
                if (bootstrap == null || EditorUtility.IsPersistent(bootstrap))
                {
                    continue;
                }

                bootstrap.MapGenV2BakedMaps = bakedMaps;
                EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                updated++;
            }

            return updated;
        }

        private static void ConfigureSetup(MapGenV2StarterSetup setup, PlaytestMapDefinition definition)
        {
            setup.Profile.ProfileId = definition.ProfileId;
            setup.Profile.DisplayName = definition.DisplayName;
            setup.Profile.Seed = definition.Seed;
            setup.Profile.MapSize = definition.GridSize;
            setup.Profile.AuthoringNotes =
                "Generated by Conn/MapGenV2/Build Playtest Baked Maps. "
                + "The source draft is temporary; rebuild this baked map through the builder.";
            setup.Profile.OutputSettings = new MapGenOutputSettings
            {
                DraftFolder = $"{TempRoot}/{definition.ProfileId}/Drafts",
                MaterializedPrefabFolder = $"{TempRoot}/{definition.ProfileId}/MaterializedPrefabs",
                BakedAssetFolder = BakedMapFolder,
                OverwriteMode = MapGenV2OutputOverwriteMode.ReplacePrevious
            };

            setup.Profile.LayoutRules.PropPlacementRules = new[]
            {
                RequiredMarkerRule("objective", MapGenPropPlacementChannelKind.Objective, MapGenRoomCategory.Quest),
                RequiredMarkerRule("spawn", MapGenPropPlacementChannelKind.Custom, MapGenRoomCategory.Boss)
            };

            EditorUtility.SetDirty(setup.Profile.LayoutRules);
            EditorUtility.SetDirty(setup.Profile);
            setup.Draft.ImportFromProfileSource(setup.Profile, true);
            EditorUtility.SetDirty(setup.Draft);
        }

        private static void DeleteExistingPlaytestBakedMaps()
        {
            if (!AssetDatabase.IsValidFolder(BakedMapFolder))
            {
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:MapGenBakedMapAsset", new[] { BakedMapFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static void InvokePlaytestContentBake()
        {
            const string typeName = "Conn.Editor.Content.MapGenV2PlaytestContentSetup, Conn.Editor";
            var type = Type.GetType(typeName);
            var method = type?.GetMethod("CreateOrUpdateAndBake", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                throw new InvalidOperationException($"Could not find {typeName}.CreateOrUpdateAndBake.");
            }

            method.Invoke(null, null);
        }

        private static MapGenPropPlacementRules RequiredMarkerRule(
            string channel,
            MapGenPropPlacementChannelKind channelKind,
            MapGenRoomCategory category)
        {
            return new MapGenPropPlacementRules
            {
                Channel = channel,
                ChannelKind = channelKind,
                DistributionMode = MapGenPropDistributionMode.RequiredUnique,
                RoomCategoryFilters = new[] { category.ToString() },
                CorridorKindFilters = Array.Empty<string>(),
                DensityPercent = 100,
                MinSpacingCells = 0,
                AllowTraversalBlocking = false,
                RequiredUnique = true
            };
        }

        private static void StampRuntimeMarkers(MapGenMockupDraftAsset draft)
        {
            if (!TryStampFirstRoomCell(draft, MapGenRoomCategory.Quest, "objective"))
            {
                throw new InvalidOperationException($"{draft.GetMapId()} has no quest room cell for an objective marker.");
            }

            if (!TryStampFirstRoomCell(draft, MapGenRoomCategory.Boss, "spawn"))
            {
                throw new InvalidOperationException($"{draft.GetMapId()} has no boss room cell for a spawn marker.");
            }

            EditorUtility.SetDirty(draft);
        }

        private static bool TryStampFirstRoomCell(MapGenMockupDraftAsset draft, MapGenRoomCategory category, string channel)
        {
            var cells = draft.Cells ?? Array.Empty<MapGenMockupCell>();
            for (var i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                if (cell.State != MapGenCellState.Room || cell.RoomCategory != category)
                {
                    continue;
                }

                cell.PropChannel = channel;
                cell.PropWeight = Mathf.Max(1, cell.PropWeight);
                cells[i] = cell;
                draft.Cells = cells;
                return true;
            }

            return false;
        }

        private static void ValidateBakedMap(
            MapGenValidationReport report,
            PlaytestMapDefinition definition,
            string path,
            MapGenBakedMapAsset baked)
        {
            if (baked == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.BakeRuntime,
                    "playtest_baked_map_missing",
                    $"Missing MapGenV2 playtest baked map for profile '{definition.ProfileId}'.",
                    "Run Conn/MapGenV2/Build Playtest Baked Maps.",
                    severity: MapGenIssueSeverity.Error,
                    contextPath: path));
                return;
            }

            var migration = MapGenBakedMapMigration.MigrateInMemory(baked);
            if (!migration.IsValid)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.BakeRuntime,
                    "playtest_baked_map_incompatible",
                    migration.Message,
                    "Rebuild the playtest baked maps.",
                    severity: MapGenIssueSeverity.Fatal,
                    contextPath: path));
                return;
            }

            Expect(report, baked.ProfileId == definition.ProfileId, "playtest_baked_map_profile_mismatch", "Baked map profile id does not match its playtest definition.", path);
            Expect(report, baked.Seed == definition.Seed, "playtest_baked_map_seed_mismatch", "Baked map seed does not match its playtest definition.", path);
            Expect(report, baked.Width == definition.GridSize.x && baked.Height == definition.GridSize.y, "playtest_baked_map_size_mismatch", "Baked map dimensions do not match its playtest definition.", path);
            Expect(report, baked.Cells != null && baked.Cells.Length > 0, "playtest_baked_map_empty_cells", "Baked map has no cells.", path);
            Expect(report, baked.TraversalEdges != null && baked.TraversalEdges.Length > 0, "playtest_baked_map_empty_traversal", "Baked map has no traversal edges.", path);
            Expect(report, HasRegion(baked, MapGenRoomCategory.Start), "playtest_baked_map_missing_start", "Baked map has no start region.", path);
            Expect(report, HasRegion(baked, MapGenRoomCategory.Quest), "playtest_baked_map_missing_quest", "Baked map has no quest region.", path);
            Expect(report, HasRegion(baked, MapGenRoomCategory.Boss), "playtest_baked_map_missing_boss", "Baked map has no boss region.", path);
            Expect(report, HasRegion(baked, MapGenRoomCategory.Exit), "playtest_baked_map_missing_exit", "Baked map has no exit region.", path);
            Expect(report, baked.ObjectiveMarkers != null && baked.ObjectiveMarkers.Length > 0, "playtest_baked_map_missing_objective_marker", "Baked map has no objective marker for runtime quest target placement.", path);
        }

        private static bool HasRegion(MapGenBakedMapAsset baked, MapGenRoomCategory category)
        {
            foreach (var region in baked.Regions ?? Array.Empty<MapGenBakedRegion>())
            {
                if (region.RoomCategory == category)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Expect(
            MapGenValidationReport report,
            bool condition,
            string code,
            string message,
            string path)
        {
            if (condition)
            {
                return;
            }

            report.Add(new MapGenIssue(
                MapGenGenerationPhase.BakeRuntime,
                code,
                message,
                "Run Conn/MapGenV2/Build Playtest Baked Maps.",
                severity: MapGenIssueSeverity.Error,
                contextPath: path));
        }

        private static void ThrowIfInvalid(MapGenValidationReport report, string message)
        {
            if (report == null || report.IsValid)
            {
                return;
            }

            throw new InvalidOperationException($"{message}\n{MapGenV2BuildValidation.Format(report)}");
        }

        private static string BakedMapPath(PlaytestMapDefinition definition)
        {
            return $"{BakedMapFolder}/{definition.ProfileId}_{definition.Seed}_BakedMap.asset";
        }

        private readonly struct PlaytestMapDefinition
        {
            public PlaytestMapDefinition(string profileId, string displayName, int seed, Vector2Int gridSize)
            {
                ProfileId = profileId;
                DisplayName = displayName;
                Seed = seed;
                GridSize = gridSize;
            }

            public string ProfileId { get; }

            public string DisplayName { get; }

            public int Seed { get; }

            public Vector2Int GridSize { get; }
        }
    }
}

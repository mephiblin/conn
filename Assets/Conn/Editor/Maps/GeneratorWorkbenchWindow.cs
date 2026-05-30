using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public sealed class GeneratorWorkbenchWindow : EditorWindow
    {
        private int seed = 2001;
        private int floor = 1;
        private int difficulty = 0;
        private EditableMapDraftAsset draft;
        private CompiledMap compiled;
        private MapValidationReport report;
        private MapAuthoringSnapshot authoringSnapshot;
        private MapValidationReport authoringReport;
        private RuntimeMapGenerationBundle builtBundle;
        private MapProfileAsset selectedProfile;
        private Vector2 scroll;

        [MenuItem("Conn/Map/Generator Workbench")]
        public static void Open()
        {
            GetWindow<GeneratorWorkbenchWindow>("Generator");
        }

        private void OnDisable()
        {
            ReleaseTransientDraft();
            draft = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Map Generator Workbench", EditorStyles.boldLabel);
            selectedProfile = (MapProfileAsset)EditorGUILayout.ObjectField("Map Profile Asset", selectedProfile, typeof(MapProfileAsset), false);
            seed = EditorGUILayout.IntField("Seed", seed);
            floor = Mathf.Max(1, EditorGUILayout.IntField("Floor", floor));
            difficulty = Mathf.Max(0, EditorGUILayout.IntField("Difficulty", difficulty));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate"))
                {
                    Generate();
                }

                if (GUILayout.Button("Random Seed"))
                {
                    seed = UnityEngine.Random.Range(1, int.MaxValue);
                    Generate();
                }

                GUI.enabled = compiled != null;
                if (GUILayout.Button("Save Compiled Map"))
                {
                    SaveCompiledMap();
                }

                GUI.enabled = true;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authoring Asset Validation", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Authoring Assets"))
                {
                    authoringSnapshot = MapAuthoringValidationService.FindAuthoringAssets();
                    authoringReport = null;
                }

                if (GUILayout.Button("Validate Map Authoring Assets"))
                {
                    authoringSnapshot = MapAuthoringValidationService.FindAuthoringAssets();
                    authoringReport = MapAuthoringValidationService.Validate(authoringSnapshot);
                }

                using (new EditorGUI.DisabledScope(authoringSnapshot == null))
                {
                    if (GUILayout.Button("Build Runtime Bundle"))
                    {
                        BuildRuntimeBundleFromAuthoring();
                    }
                }

                if (GUILayout.Button("Save Catalog Runtime Bundle"))
                {
                    builtBundle = RuntimeMapGenerationBundleBuilder.BuildChapterTwoCatalogBundle(floor, difficulty);
                    RuntimeMapGenerationBundleBuilder.SaveBundleAsset(builtBundle);
                }
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (authoringSnapshot != null)
            {
                EditorGUILayout.Space();
                DrawSelectedProfileSummary();
                EditorGUILayout.LabelField("Authoring Profiles", authoringSnapshot.Profiles.Length.ToString());
                EditorGUILayout.LabelField("Resource Sets", authoringSnapshot.ResourceSets.Length.ToString());
                EditorGUILayout.LabelField("Room Chunks", authoringSnapshot.RoomChunks.Length.ToString());
                EditorGUILayout.LabelField("Landmark Rooms", authoringSnapshot.LandmarkRooms.Length.ToString());
                EditorGUILayout.LabelField("Spawn Tables", authoringSnapshot.SpawnTables.Length.ToString());
                EditorGUILayout.LabelField("Weight Profiles", authoringSnapshot.WeightProfiles.Length.ToString());
                if (builtBundle != null)
                {
                    EditorGUILayout.LabelField("Built Runtime Bundle Profiles", builtBundle.Profiles.Count.ToString());
                }

                if (authoringReport != null)
                {
                    EditorGUILayout.LabelField("Authoring Validation", authoringReport.Passed ? "Passed" : "Failed");
                    for (var i = 0; i < authoringReport.Errors.Count; i++)
                    {
                        EditorGUILayout.HelpBox(authoringReport.Errors[i], MessageType.Error);
                    }

                    for (var i = 0; i < authoringReport.Warnings.Count; i++)
                    {
                        EditorGUILayout.HelpBox(authoringReport.Warnings[i], MessageType.Warning);
                    }
                }
            }

            if (draft != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Profile", draft.SourceProfileId);
                EditorGUILayout.LabelField("Rooms", draft.Rooms.Length.ToString());
                EditorGUILayout.LabelField("Sockets", draft.Sockets.Length.ToString());
                EditorGUILayout.LabelField("Placements", (compiled?.Placements?.Count ?? 0).ToString());
                EditorGUILayout.LabelField("Encounter Placements", (compiled?.EncounterPlacements?.Count ?? 0).ToString());
                if (compiled != null)
                {
                    EditorGUILayout.LabelField("Compiled Map", compiled.MapId);
                    EditorGUILayout.LabelField("Compiled Size", $"{compiled.Width}x{compiled.Height}");
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Validation", report != null && report.Passed ? "Passed" : "Failed");
                if (report != null)
                {
                    for (var i = 0; i < report.Errors.Count; i++)
                    {
                        EditorGUILayout.HelpBox(report.Errors[i], MessageType.Error);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Placement Details", EditorStyles.boldLabel);
                for (var i = 0; i < (compiled?.Placements?.Count ?? 0); i++)
                {
                    var placement = compiled.Placements[i];
                    EditorGUILayout.LabelField($"{placement.Id}: {placement.Kind} room={placement.RoomId} pos=({placement.X},{placement.Y}) ref={placement.ReferenceId}");
                }

                if (compiled != null && compiled.EncounterPlacements != null && compiled.EncounterPlacements.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Encounter Placement Details", EditorStyles.boldLabel);
                    for (var i = 0; i < compiled.EncounterPlacements.Count; i++)
                    {
                        var placement = compiled.EncounterPlacements[i];
                        EditorGUILayout.LabelField($"{placement.PlacementId}: map={placement.MapPlacementId} encounter={placement.EncounterId} monster={placement.PrimaryMonsterId} source={placement.SpawnSourceId} role={placement.SpawnRole}");
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Room Details", EditorStyles.boldLabel);
                for (var i = 0; i < draft.Rooms.Length; i++)
                {
                    var room = draft.Rooms[i];
                    EditorGUILayout.LabelField($"{room.Id}: {room.Role} ({room.X},{room.Y}) {room.SocketMask} chunk={room.ChunkId}");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            if (selectedProfile != null && GenerateFromSelectedProfile())
            {
                return;
            }

            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            ApplyGeneratedResult(EditableMapGeneratedResultBuilder.Build(profile, chunks, seed, floor, difficulty));
        }

        private bool GenerateFromSelectedProfile()
        {
            authoringSnapshot = MapAuthoringValidationService.FindAuthoringAssets();
            authoringReport = MapAuthoringValidationService.Validate(authoringSnapshot);
            if (!authoringReport.Passed)
            {
                ReleaseTransientDraft();
                draft = null;
                compiled = null;
                report = null;
                return true;
            }

            builtBundle = RuntimeMapGenerationBundleBuilder.Build(authoringSnapshot, floor, difficulty);
            var profileId = selectedProfile.Id;
            var entry = builtBundle.FindProfile(profileId);
            if (entry == null)
            {
                report = new MapValidationReport();
                report.Errors.Add($"Selected map profile is not in the built runtime bundle: {profileId}");
                ReleaseTransientDraft();
                draft = null;
                compiled = null;
                return true;
            }

            ApplyGeneratedResult(EditableMapGeneratedResultBuilder.Build(entry.Profile, entry.Chunks, seed, floor, difficulty));
            return true;
        }

        private void BuildRuntimeBundleFromAuthoring()
        {
            authoringSnapshot ??= MapAuthoringValidationService.FindAuthoringAssets();
            authoringReport = MapAuthoringValidationService.Validate(authoringSnapshot);
            if (!authoringReport.Passed)
            {
                return;
            }

            builtBundle = RuntimeMapGenerationBundleBuilder.Build(authoringSnapshot, floor, difficulty);
            RuntimeMapGenerationBundleBuilder.SaveBundleAsset(builtBundle);
        }

        private void DrawSelectedProfileSummary()
        {
            if (selectedProfile == null)
            {
                EditorGUILayout.HelpBox("No MapProfileAsset selected. Generate uses the Chapter 2 catalog profile.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Selected Profile", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Id", selectedProfile.Id);
            EditorGUILayout.LabelField("Generation Context", $"floor={floor}, difficulty={difficulty}");
            EditorGUILayout.LabelField("Theme", selectedProfile.ThemeId);
            EditorGUILayout.ObjectField("Resource Set", selectedProfile.ResourceSet, typeof(MapResourceSetAsset), false);
            EditorGUILayout.ObjectField("Generation Weights", selectedProfile.GenerationWeightProfile, typeof(GenerationWeightProfileAsset), false);
            EditorGUILayout.LabelField("Required Landmarks", Count(selectedProfile.RequiredLandmarkRooms).ToString());
            EditorGUILayout.LabelField("Optional Chunks", Count(selectedProfile.OptionalChunks).ToString());
            EditorGUILayout.LabelField("Optional Landmarks", Count(selectedProfile.OptionalLandmarks).ToString());
            EditorGUILayout.LabelField("Spawn Tables", Count(selectedProfile.AllowedSpawnTables).ToString());
            EditorGUILayout.LabelField("Tag Filters", string.Join(", ", selectedProfile.SpawnTagFilters ?? Array.Empty<string>()));
            EditorGUILayout.LabelField("Direct Overrides", Count(selectedProfile.DirectEncounterOverrides).ToString());
        }

        private static int Count<T>(T[] values)
        {
            return values?.Length ?? 0;
        }

        private void ApplyGeneratedResult(EditableMapGeneratedResultBuilder.GeneratedEditableMapResult generated)
        {
            ReleaseTransientDraft();
            draft = generated.Draft;
            compiled = generated.Compiled;
            report = generated.Report;
        }

        private void ReleaseTransientDraft()
        {
            if (draft == null || (draft.hideFlags & HideFlags.DontSave) == 0)
            {
                return;
            }

            DestroyImmediate(draft);
        }

        private void SaveCompiledMap()
        {
            if (compiled == null)
            {
                return;
            }

            var assetPath = $"Assets/Conn/Core/Maps/{compiled.ProfileId}_{seed}_CompiledMap.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CompiledMapAsset>(assetPath);
            if (asset == null)
            {
                asset = CreateInstance<CompiledMapAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.ProfileId = compiled.ProfileId;
            asset.Seed = seed;
            asset.Json = JsonUtility.ToJson(compiled, true);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"Saved compiled map asset: {assetPath}");
        }
    }
}

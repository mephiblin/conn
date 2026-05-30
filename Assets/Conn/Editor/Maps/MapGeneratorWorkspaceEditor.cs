using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Conn.Editor.Maps
{
    [CustomEditor(typeof(MapGeneratorWorkspace))]
    public sealed class MapGeneratorWorkspaceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var workspace = (MapGeneratorWorkspace)target;
            serializedObject.Update();

            EditorGUILayout.LabelField("Layout Snapshot Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.MapProfile)));
            DrawProfileSummary(workspace.MapProfile);
            DrawProfileEditor(workspace.MapProfile);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.Seed)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.Floor)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.Difficulty)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.RoomSpacingMin)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.RoomSpacingMax)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewCellSize)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.ClearBeforePreview)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.DrawSceneGizmos)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewRoot)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.CurrentEditableDraft)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.CurrentCompiledMapAsset)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Production Scene Workflow", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generate a visual cell-map preview first. When it is acceptable, save it as an editable draft, then validate and bake the runtime CompiledMap asset.",
                MessageType.Info);
            if (workspace.MapProfile == null)
            {
                EditorGUILayout.HelpBox("Map Profile is required. Select an existing profile asset, create a new empty profile, or create an example profile.", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create New Profile"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        AssignProfileAsset(workspace, MapProfileAuthoringAssetFactory.CreateEmptyProfileAsset(), "Create Empty Map Profile");
                    }

                    if (GUILayout.Button("Create Example Profile"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        AssignProfileAsset(workspace, MapProfileAuthoringSampleBuilder.CreateChapterTwoSampleProfileAssets(), "Create Example Map Profile");
                    }
                }
            }
            else if (!ProfileValidationPassed(workspace.MapProfile, workspace))
            {
                EditorGUILayout.HelpBox("Run Validate Profile after editing profile rules or pools. Generate Preview stays disabled until the latest validation passes.", MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var canGenerate = workspace.MapProfile != null && ProfileValidationPassed(workspace.MapProfile, workspace);
                using (new EditorGUI.DisabledScope(!canGenerate))
                {
                    if (GUILayout.Button("Generate Preview"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        GeneratePreview(workspace);
                    }

                    if (GUILayout.Button("Random Seed + Generate Preview"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RecordObject(workspace, "Random Map Preview Seed");
                        workspace.Seed = UnityEngine.Random.Range(1, int.MaxValue);
                        GeneratePreview(workspace);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(workspace.MapProfile == null))
                {
                    if (GUILayout.Button("Validate Profile"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        ValidateProfile(workspace);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(workspace.LastEditableDraft == null))
                {
                    if (GUILayout.Button("Accept Preview + Save Draft"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        AcceptPreviewAndSaveDraft(workspace);
                    }
                }

                if (GUILayout.Button("Clear Scene Preview Objects"))
                {
                    ClearPreviewWithUndo(workspace, "Clear Map Preview");
                    MarkSceneDirty(workspace);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Accepted Draft Tools", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(workspace.CurrentEditableDraft == null))
                {
                    if (GUILayout.Button("Select Draft"))
                    {
                        Selection.activeObject = workspace.CurrentEditableDraft;
                        EditorGUIUtility.PingObject(workspace.CurrentEditableDraft);
                    }

                    if (GUILayout.Button("Build Scene From Draft"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        BuildSceneMapFromCurrentDraft(workspace);
                    }

                    if (GUILayout.Button("Validate Draft"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        ValidateCurrentDraft(workspace);
                    }

                    if (GUILayout.Button("Bake + Save Compiled Map"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        BakeAndSaveCurrentDraft(workspace);
                    }
                }

                using (new EditorGUI.DisabledScope(workspace.CurrentCompiledMapAsset == null))
                {
                    if (GUILayout.Button("Select Compiled Map"))
                    {
                        Selection.activeObject = workspace.CurrentCompiledMapAsset;
                        EditorGUIUtility.PingObject(workspace.CurrentCompiledMapAsset);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Profile Validation", workspace.LastProfileValidation);
            EditorGUILayout.LabelField("Required Rooms", workspace.LastProfileRequiredRoomCount.ToString());
            EditorGUILayout.LabelField("Pool Count", workspace.LastProfilePoolCount.ToString());
            EditorGUILayout.LabelField("Socket Coverage", $"{workspace.LastProfileSocketCoverage}/4");
            EditorGUILayout.LabelField("Map", string.IsNullOrWhiteSpace(workspace.LastGeneratedMapId) ? "(none)" : workspace.LastGeneratedMapId);
            EditorGUILayout.LabelField("Profile", string.IsNullOrWhiteSpace(workspace.LastGeneratedProfileId) ? "(none)" : workspace.LastGeneratedProfileId);
            EditorGUILayout.LabelField("Seed", workspace.LastGeneratedSeed.ToString());
            EditorGUILayout.LabelField("Validation", workspace.LastValidation);
            EditorGUILayout.LabelField("Rooms", workspace.LastRoomCount.ToString());
            EditorGUILayout.LabelField("Edges", workspace.LastEdgeCount.ToString());
            EditorGUILayout.LabelField("Placements", workspace.LastPlacementCount.ToString());
            EditorGUILayout.LabelField("Chunks Selected", workspace.LastGeneratedChunkCount.ToString());
            EditorGUILayout.LabelField("Retry Count", workspace.LastGeneratedRetryCount.ToString());
            EditorGUILayout.LabelField("Failure Reason", string.IsNullOrWhiteSpace(workspace.LastGenerationFailureReason) ? "(none)" : workspace.LastGenerationFailureReason);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewRooms)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewEdges)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewPlacements)), true);

            if (workspace.LastProfileReport != null)
            {
                for (var i = 0; i < workspace.LastProfileReport.Errors.Count; i++)
                {
                    EditorGUILayout.HelpBox(workspace.LastProfileReport.Errors[i], MessageType.Error);
                }

                for (var i = 0; i < workspace.LastProfileReport.Warnings.Count; i++)
                {
                    EditorGUILayout.HelpBox(workspace.LastProfileReport.Warnings[i], MessageType.Warning);
                }
            }

            if (workspace.LastReport != null)
            {
                for (var i = 0; i < workspace.LastReport.Errors.Count; i++)
                {
                    EditorGUILayout.HelpBox(workspace.LastReport.Errors[i], MessageType.Error);
                }

                for (var i = 0; i < workspace.LastReport.Warnings.Count; i++)
                {
                    EditorGUILayout.HelpBox(workspace.LastReport.Warnings[i], MessageType.Warning);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawProfileSummary(MapProfileAsset profile)
        {
            if (profile == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Room Count", $"{profile.RoomCountMin} - {profile.RoomCountMax}");
            EditorGUILayout.LabelField("Critical Path", $"{profile.CriticalPathMin} - {profile.CriticalPathMax}");
            EditorGUILayout.LabelField("Side Branches", profile.SideBranchCount.ToString());
            EditorGUILayout.LabelField("Required Rooms", CountRequiredRooms(profile).ToString());
            EditorGUILayout.LabelField("Socket Coverage", $"{CountSocketCoverage(profile)}/4 directions");
            EditorGUILayout.LabelField(
                "Room Asset Pools",
                $"typed pools={Count(profile.RoomPools)}, legacy chunks={Count(profile.OptionalChunks)}, required landmarks={Count(profile.RequiredLandmarkRooms)}, optional landmarks={Count(profile.OptionalLandmarks)}");

            foreach (var pool in GetEffectiveRoomPools(profile))
            {
                if (pool == null)
                {
                    continue;
                }

                EditorGUILayout.LabelField(
                    $"{pool.Role}/{pool.LayoutKind}",
                    $"required={pool.Required}, min={pool.MinCount}, max={FormatMax(pool.MaxCount)}, weight={pool.Weight}, chunks={Count(pool.AllowedChunks)}");
            }
        }

        private static void DrawProfileEditor(MapProfileAsset profile)
        {
            if (profile == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profile Rules", EditorStyles.boldLabel);
            var profileObject = new SerializedObject(profile);
            profileObject.Update();
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.Id)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.DisplayName)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.MapKind)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.ThemeId)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.GridSize)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.RoomSize)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.RoomCountMin)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.RoomCountMax)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.CriticalPathMin)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.CriticalPathMax)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.SideBranchCount)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.LoopMin)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.LoopMax)));
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.RequiredAnchors)), true);
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.ResourceSet)));
            DrawPoolEditor(profileObject.FindProperty(nameof(MapProfileAsset.RoomPools)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legacy Bridge", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.OptionalChunks)), true);
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.RequiredLandmarkRooms)), true);
            EditorGUILayout.PropertyField(profileObject.FindProperty(nameof(MapProfileAsset.OptionalLandmarks)), true);

            profileObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(profile);
            }
        }

        private static void DrawPoolEditor(SerializedProperty poolsProperty)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room Pools", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Pool"))
            {
                var index = poolsProperty.arraySize;
                poolsProperty.InsertArrayElementAtIndex(index);
                var pool = poolsProperty.GetArrayElementAtIndex(index);
                pool.FindPropertyRelative(nameof(MapRoomPoolRule.Role)).enumValueIndex = 0;
                pool.FindPropertyRelative(nameof(MapRoomPoolRule.LayoutKind)).enumValueIndex = 0;
                pool.FindPropertyRelative(nameof(MapRoomPoolRule.Weight)).intValue = 1;
            }

            for (var i = 0; i < poolsProperty.arraySize; i++)
            {
                var pool = poolsProperty.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Pool {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        poolsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(nameof(MapRoomPoolRule.Role)));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(nameof(MapRoomPoolRule.LayoutKind)));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(nameof(MapRoomPoolRule.MinCount)));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(nameof(MapRoomPoolRule.MaxCount)));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(nameof(MapRoomPoolRule.Weight)));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(nameof(MapRoomPoolRule.Required)));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(nameof(MapRoomPoolRule.AllowedChunks)), true);
                }
            }
        }

        private static int Count<T>(T[] values)
        {
            return values?.Length ?? 0;
        }

        private static string FormatMax(int value)
        {
            return value > 0 ? value.ToString() : "-";
        }

        private static IEnumerable<MapRoomPoolRule> GetEffectiveRoomPools(MapProfileAsset profile)
        {
            if (profile.RoomPools != null && profile.RoomPools.Length > 0)
            {
                return profile.RoomPools;
            }

            var pools = new List<MapRoomPoolRule>();
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Start, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Start, RoomChunkLayoutKind.Room);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Main, RoomChunkLayoutKind.Room, 1, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Room);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Corridor, RoomChunkLayoutKind.Corridor, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Corridor);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Hub, RoomChunkLayoutKind.Hub, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Hub);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Side, RoomChunkLayoutKind.Room, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.Room);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.DeadEnd, RoomChunkLayoutKind.DeadEnd, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.DeadEnd);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Quest, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.QuestTarget, RoomChunkLayoutKind.Room);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Boss, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Boss, RoomChunkLayoutKind.Room);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.Exit, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Exit, RoomChunkLayoutKind.Room);
            AddLegacyBridgePool(pools, profile, MapRoomPoolRole.HeightTransition, RoomChunkLayoutKind.HeightTransition, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.HeightTransition);
            return pools;
        }

        private static void AddLegacyBridgePool(
            List<MapRoomPoolRule> pools,
            MapProfileAsset profile,
            MapRoomPoolRole poolRole,
            RoomChunkLayoutKind layoutKind,
            int minCount,
            int maxCount,
            bool required,
            MapRoomRole chunkRole,
            RoomChunkLayoutKind chunkLayoutKind)
        {
            var chunks = new List<RoomChunkAsset>();
            foreach (var chunk in profile.OptionalChunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (chunk != null && chunk.LayoutKind == chunkLayoutKind && RoleTagsContain(chunk.RoleTags, chunkRole))
                {
                    chunks.Add(chunk);
                }
            }

            pools.Add(new MapRoomPoolRule
            {
                Role = poolRole,
                LayoutKind = layoutKind,
                MinCount = minCount,
                MaxCount = maxCount,
                Weight = 1,
                Required = required,
                AllowedChunks = chunks.ToArray()
            });
        }

        private static bool RoleTagsContain(string[] roleTags, MapRoomRole role)
        {
            foreach (var tag in roleTags ?? Array.Empty<string>())
            {
                if (Enum.TryParse<MapRoomRole>(tag, true, out var parsed) && parsed == role)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountRequiredRooms(MapProfileAsset profile)
        {
            var count = 0;
            foreach (var pool in GetEffectiveRoomPools(profile))
            {
                if (pool != null && pool.Required)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSocketCoverage(MapProfileAsset profile)
        {
            var covered = MapDirection.None;
            foreach (var pool in GetEffectiveRoomPools(profile))
            {
                if (pool == null)
                {
                    continue;
                }

                foreach (var chunk in pool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
                {
                    if (chunk == null)
                    {
                        continue;
                    }

                    foreach (var socket in ResolveSocketDefinitions(chunk))
                    {
                        if (RoomChunkSocketRules.AllowsConnection(socket))
                        {
                            covered |= socket.Side;
                        }
                    }
                }
            }

            return CountDirections(covered);
        }

        private static RoomChunkSocketDefinition[] ResolveSocketDefinitions(RoomChunkAsset chunk)
        {
            if (chunk.SocketDefinitions != null && chunk.SocketDefinitions.Length > 0)
            {
                return chunk.SocketDefinitions;
            }

            var sockets = new List<RoomChunkSocketDefinition>();
            foreach (var side in RoomChunkSocketRules.EnumerateSides(MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West))
            {
                var isOpen = (chunk.OpenSides & side) != MapDirection.None;
                sockets.Add(new RoomChunkSocketDefinition
                {
                    Side = side,
                    SocketType = !isOpen
                        ? RoomChunkSocketType.Blocked
                        : chunk.LayoutKind == RoomChunkLayoutKind.Corridor
                            ? RoomChunkSocketType.Corridor
                            : RoomChunkSocketType.Door,
                    SocketId = !isOpen
                        ? string.Empty
                        : chunk.LayoutKind == RoomChunkLayoutKind.Corridor
                            ? "corridor"
                            : "door"
                });
            }

            return sockets.ToArray();
        }

        private static int CountDirections(MapDirection sides)
        {
            var count = 0;
            foreach (var _ in RoomChunkSocketRules.EnumerateSides(sides))
            {
                count++;
            }

            return count;
        }

        private static bool ProfileValidationPassed(MapProfileAsset profile, MapGeneratorWorkspace workspace)
        {
            if (profile == null || workspace == null)
            {
                return false;
            }

            return workspace.LastProfileValidation == "Passed"
                && string.Equals(workspace.LastValidatedProfileSignature, BuildProfileSignature(profile), StringComparison.Ordinal);
        }

        private static void ValidateProfile(MapGeneratorWorkspace workspace)
        {
            var profile = workspace.MapProfile;
            if (profile == null)
            {
                return;
            }

            var snapshot = MapAuthoringValidationService.BuildScopedSnapshot(profile);
            var report = MapAuthoringValidationService.Validate(snapshot);
            Undo.RecordObject(workspace, "Validate Map Profile");
            workspace.SetProfileValidationResult(
                report,
                BuildProfileSignature(profile),
                CountRequiredRooms(profile),
                CountEffectivePools(profile),
                CountSocketCoverage(profile));
            EditorUtility.SetDirty(workspace);
            MarkSceneDirty(workspace);
        }

        private static void AssignProfileAsset(MapGeneratorWorkspace workspace, MapProfileAsset profile, string undoLabel)
        {
            if (workspace == null || profile == null)
            {
                return;
            }

            Undo.RecordObject(workspace, undoLabel);
            workspace.MapProfile = profile;
            EditorUtility.SetDirty(workspace);
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            MarkSceneDirty(workspace);
        }

        private static string BuildProfileSignature(MapProfileAsset profile)
        {
            if (profile == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append(profile.Id).Append('|');
            builder.Append(profile.DisplayName).Append('|');
            builder.Append(profile.MapKind).Append('|');
            builder.Append(profile.ThemeId).Append('|');
            builder.Append(profile.GridSize.x).Append(',').Append(profile.GridSize.y).Append('|');
            builder.Append(profile.RoomSize.x).Append(',').Append(profile.RoomSize.y).Append('|');
            builder.Append(profile.RoomCountMin).Append('|').Append(profile.RoomCountMax).Append('|');
            builder.Append(profile.CriticalPathMin).Append('|').Append(profile.CriticalPathMax).Append('|');
            builder.Append(profile.SideBranchCount).Append('|').Append(profile.LoopMin).Append('|').Append(profile.LoopMax).Append('|');
            foreach (var pool in GetEffectiveRoomPools(profile))
            {
                if (pool == null)
                {
                    continue;
                }

                builder.Append(pool.Role).Append(':')
                    .Append(pool.LayoutKind).Append(':')
                    .Append(pool.MinCount).Append(':')
                    .Append(pool.MaxCount).Append(':')
                    .Append(pool.Weight).Append(':')
                    .Append(pool.Required).Append(':');
                foreach (var chunk in pool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
                {
                    builder.Append(chunk != null ? chunk.Id : "null").Append(',');
                }

                builder.Append('|');
            }

            return builder.ToString();
        }

        private static int CountEffectivePools(MapProfileAsset profile)
        {
            var count = 0;
            foreach (var pool in GetEffectiveRoomPools(profile))
            {
                if (pool != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void GeneratePreview(MapGeneratorWorkspace workspace)
        {
            try
            {
                var generated = Generate(workspace);
                Undo.RecordObject(workspace, "Generate Map Preview");
                workspace.SetGeneratedResult(generated.Draft, generated.Compiled, generated.Report);
                EditorUtility.SetDirty(workspace);

                if (workspace.ClearBeforePreview)
                {
                    ClearPreviewWithUndo(workspace, "Generate Map Preview");
                }

                EditableMapPreviewMeshBuilder.RebuildPreview(generated.Draft, workspace.ResolvePreviewRoot());
                MarkSceneDirty(workspace);
            }
            catch (Exception exception)
            {
                RecordGenerationException(workspace, "Generate Map Preview", exception);
            }
        }

        private static void GenerateLayoutSnapshot(MapGeneratorWorkspace workspace)
        {
            try
            {
                var generated = Generate(workspace);
                Undo.RecordObject(workspace, "Generate Map Layout");
                workspace.SetGeneratedResult(generated.Draft, generated.Compiled, generated.Report);
                EditorUtility.SetDirty(workspace);
                MarkSceneDirty(workspace);
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                RecordGenerationException(workspace, "Generate Map Layout", exception);
            }
        }

        private static void BuildScenePreviewFromSnapshot(MapGeneratorWorkspace workspace)
        {
            if (!workspace.HasPreviewSnapshot || workspace.LastEditableDraft == null)
            {
                return;
            }

            if (workspace.ClearBeforePreview)
            {
                ClearPreviewWithUndo(workspace, "Build Map Scene Preview");
                EditableMapPreviewMeshBuilder.ClearPreview(workspace.LastEditableDraft, workspace.ResolvePreviewRoot());
            }

            EditableMapPreviewMeshBuilder.RebuildPreview(workspace.LastEditableDraft, workspace.ResolvePreviewRoot());
            MarkSceneDirty(workspace);
        }

        private static void RecordGenerationException(MapGeneratorWorkspace workspace, string undoName, Exception exception)
        {
            var report = new MapValidationReport();
            report.Errors.Add(exception.Message);
            Undo.RecordObject(workspace, undoName);
            workspace.SetGeneratedResult(null, null, report);
            EditorUtility.SetDirty(workspace);
            MarkSceneDirty(workspace);
            Debug.LogException(exception);
        }

        private static void AcceptPreviewAndSaveDraft(MapGeneratorWorkspace workspace)
        {
            try
            {
                var generated = workspace.LastEditableDraft != null
                    ? new GeneratedMapResult(workspace.LastEditableDraft, workspace.LastCompiled, workspace.LastReport)
                    : Generate(workspace);
                var assetPath = EditableMapDraftBuilder.BuildDefaultAssetPath($"{generated.Draft.SourceProfileId}_{generated.Draft.Seed}_draft");
                var draftAsset = EditableMapDraftBuilder.CreateDraftAssetFromSource(assetPath, generated.Draft);
                var report = EditableMapValidationService.Validate(draftAsset);

                Undo.RecordObject(workspace, "Accept Preview And Save Draft");
                workspace.CurrentEditableDraft = draftAsset;
                workspace.SetGeneratedResult(draftAsset, generated.Compiled, report);
                EditorUtility.SetDirty(workspace);
                MarkSceneDirty(workspace);
                Selection.activeObject = draftAsset;
                EditorGUIUtility.PingObject(draftAsset);
            }
            catch (Exception exception)
            {
                RecordGenerationException(workspace, "Accept Preview And Save Draft", exception);
            }
        }

        private static void GenerateDraftAsset(MapGeneratorWorkspace workspace)
        {
            try
            {
                var generated = Generate(workspace);
                var assetPath = EditableMapDraftBuilder.BuildDefaultAssetPath($"{generated.Draft.SourceProfileId}_{generated.Draft.Seed}_draft");
                var draftAsset = EditableMapDraftBuilder.CreateDraftAssetFromSource(assetPath, generated.Draft);
                var report = EditableMapValidationService.Validate(draftAsset);
                var compiled = report.Passed ? EditableMapBakeService.Bake(draftAsset) : null;

                Undo.RecordObject(workspace, "Generate Connected Editable Draft");
                workspace.CurrentEditableDraft = draftAsset;
                workspace.SetGeneratedResult(draftAsset, compiled, report);
                EditorUtility.SetDirty(workspace);
                MarkSceneDirty(workspace);
                Selection.activeObject = draftAsset;
                EditorGUIUtility.PingObject(draftAsset);

                if (workspace.ClearBeforePreview)
                {
                    ClearPreviewWithUndo(workspace, "Generate Connected Editable Draft");
                    EditableMapPreviewMeshBuilder.ClearPreview(draftAsset, workspace.ResolvePreviewRoot());
                }

                EditableMapPreviewMeshBuilder.RebuildPreview(draftAsset, workspace.ResolvePreviewRoot());
            }
            catch (Exception exception)
            {
                RecordGenerationException(workspace, "Generate Connected Editable Draft", exception);
            }
        }

        private static void BuildSceneMapFromCurrentDraft(MapGeneratorWorkspace workspace)
        {
            if (workspace.CurrentEditableDraft == null)
            {
                return;
            }

            if (workspace.ClearBeforePreview)
            {
                ClearPreviewWithUndo(workspace, "Build Scene Map From Draft");
                EditableMapPreviewMeshBuilder.ClearPreview(workspace.CurrentEditableDraft, workspace.ResolvePreviewRoot());
            }

            EditableMapPreviewMeshBuilder.RebuildPreview(workspace.CurrentEditableDraft, workspace.ResolvePreviewRoot());
            var report = EditableMapValidationService.Validate(workspace.CurrentEditableDraft);
            CompiledMap compiled = null;
            if (report.Passed)
            {
                compiled = EditableMapBakeService.Bake(workspace.CurrentEditableDraft);
            }

            Undo.RecordObject(workspace, "Build Scene Map From Draft");
            workspace.SetGeneratedResult(workspace.CurrentEditableDraft, compiled, report);
            EditorUtility.SetDirty(workspace);
            MarkSceneDirty(workspace);
        }

        private static void ValidateCurrentDraft(MapGeneratorWorkspace workspace)
        {
            if (workspace.CurrentEditableDraft == null)
            {
                return;
            }

            var report = EditableMapValidationService.Validate(workspace.CurrentEditableDraft);
            Undo.RecordObject(workspace, "Validate Connected Draft");
            workspace.SetGeneratedResult(workspace.CurrentEditableDraft, workspace.LastCompiled, report);
            EditorUtility.SetDirty(workspace);
            MarkSceneDirty(workspace);
        }

        private static void BakeAndSaveCurrentDraft(MapGeneratorWorkspace workspace)
        {
            if (workspace.CurrentEditableDraft == null)
            {
                return;
            }

            try
            {
                var compiled = EditableMapBakeService.Bake(workspace.CurrentEditableDraft);
                var report = EditableMapValidationService.Validate(workspace.CurrentEditableDraft);
                var assetPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Conn/Core/Maps/{compiled.MapId}_CompiledMap.asset");
                var asset = EditableMapBakeService.SaveCompiledMapAsset(compiled, assetPath);

                Undo.RecordObject(workspace, "Bake Connected Draft");
                workspace.CurrentCompiledMapAsset = asset;
                workspace.SetGeneratedResult(workspace.CurrentEditableDraft, compiled, report);
                EditorUtility.SetDirty(workspace);
                MarkSceneDirty(workspace);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            catch (Exception exception)
            {
                RecordGenerationException(workspace, "Bake Connected Draft", exception);
            }
        }

        private static GeneratedMapResult Generate(MapGeneratorWorkspace workspace)
        {
            if (workspace.MapProfile == null)
            {
                throw new InvalidOperationException("Map Profile is required before generating a map preview.");
            }

            var snapshot = MapAuthoringValidationService.BuildScopedSnapshot(workspace.MapProfile);
            var authoringReport = MapAuthoringValidationService.Validate(snapshot);
            MapValidationService.ThrowIfFailed(authoringReport);
            var bundle = RuntimeMapGenerationBundleBuilder.Build(
                snapshot,
                Mathf.Max(1, workspace.Floor),
                Mathf.Max(0, workspace.Difficulty));
            var entry = bundle.FindProfile(workspace.MapProfile.Id);
            if (entry == null)
            {
                throw new InvalidOperationException($"Map profile is not in the runtime bundle: {workspace.MapProfile.Id}");
            }

            return BuildEditableResult(
                entry.Profile,
                entry.Chunks,
                workspace.Seed,
                workspace.Floor,
                workspace.Difficulty,
                workspace.PreviewCellSize);
        }

        private static GeneratedMapResult BuildEditableResult(
            MapProfile profile,
            System.Collections.Generic.IReadOnlyList<ChunkPreset> chunks,
            int seed,
            int floor,
            int difficulty,
            float cellSize)
        {
            var generated = EditableMapGeneratedResultBuilder.Build(
                profile,
                chunks,
                seed,
                floor,
                difficulty,
                cellSize,
                cellSize);
            return new GeneratedMapResult(generated.Draft, generated.Compiled, generated.Report);
        }

        private static void DrawPreview(MapGeneratorWorkspace workspace, string undoName)
        {
            if (!workspace.HasPreviewSnapshot)
            {
                return;
            }

            var root = workspace.ResolvePreviewRoot();
            var materialCache = new PreviewMaterialCache();
            var rooms = workspace.PreviewRooms ?? Array.Empty<PreviewRoom>();
            var edges = workspace.PreviewEdges ?? Array.Empty<PreviewEdge>();
            var placements = workspace.PreviewPlacements ?? Array.Empty<PreviewPlacement>();

            for (var i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                if (!workspace.TryFindPreviewRoom(edge.FromNodeId, out var from) || !workspace.TryFindPreviewRoom(edge.ToNodeId, out var to))
                {
                    continue;
                }

                var link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                link.name = $"Edge {edge.FromNodeId} -> {edge.ToNodeId}";
                link.transform.SetParent(root, false);
                var fromPosition = workspace.PreviewRoomPosition(from);
                var toPosition = workspace.PreviewRoomPosition(to);
                var midpoint = (fromPosition + toPosition) * 0.5f;
                var delta = toPosition - fromPosition;
                link.transform.position = midpoint + Vector3.up * 0.03f;
                link.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                link.transform.localScale = new Vector3(0.16f, 0.08f, Mathf.Max(0.2f, delta.magnitude));
                link.GetComponent<MeshRenderer>().sharedMaterial = materialCache.Edge;
                Undo.RegisterCreatedObjectUndo(link, undoName);
            }

            for (var i = 0; i < rooms.Length; i++)
            {
                var roomSnapshot = rooms[i];
                var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
                room.name = $"Room {roomSnapshot.Id} ({roomSnapshot.Role})";
                room.transform.SetParent(root, false);
                room.transform.position = workspace.PreviewRoomPosition(roomSnapshot);
                room.transform.localScale = new Vector3(1.8f, Mathf.Max(0.05f, workspace.RoomHeight), 1.8f);
                room.GetComponent<MeshRenderer>().sharedMaterial = materialCache.ForRole(roomSnapshot.Role);
                Undo.RegisterCreatedObjectUndo(room, undoName);

                var label = CreateLabel(root, room.name, room.transform.position + Vector3.up * 0.35f, 0.22f);
                Undo.RegisterCreatedObjectUndo(label, undoName);
            }

            for (var i = 0; i < placements.Length; i++)
            {
                var placement = placements[i];
                if (!workspace.TryFindPreviewRoom(placement.RoomId, out var room))
                {
                    continue;
                }

                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Placement {placement.Id} ({placement.Kind})";
                marker.transform.SetParent(root, false);
                marker.transform.position = workspace.PreviewRoomPosition(room) + PlacementOffset(placement.Kind);
                marker.transform.localScale = Vector3.one * 0.32f;
                marker.GetComponent<MeshRenderer>().sharedMaterial = materialCache.ForPlacement(placement.Kind);
                Undo.RegisterCreatedObjectUndo(marker, undoName);
            }
        }

        private static GameObject CreateLabel(Transform root, string text, Vector3 position, float size)
        {
            var label = new GameObject($"Label {text}");
            label.transform.SetParent(root, false);
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 48;
            mesh.color = Color.white;
            return label;
        }

        private static Vector3 PlacementOffset(MapPlacementKind kind)
        {
            switch (kind)
            {
                case MapPlacementKind.Start:
                    return new Vector3(-0.45f, 0.35f, -0.45f);
                case MapPlacementKind.QuestTarget:
                    return new Vector3(0.45f, 0.35f, -0.45f);
                case MapPlacementKind.Boss:
                    return new Vector3(0f, 0.35f, 0.45f);
                case MapPlacementKind.Exit:
                    return new Vector3(0.45f, 0.35f, 0.45f);
                case MapPlacementKind.Monster:
                    return new Vector3(0f, 0.35f, 0f);
                default:
                    return new Vector3(-0.45f, 0.35f, 0.45f);
            }
        }

        private static void SaveCompiledMap(MapGeneratorWorkspace workspace)
        {
            var compiled = workspace.LastCompiled;
            if (compiled == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Conn/Core/Maps/{compiled.MapId}_CompiledMap.asset");
            var asset = EditableMapBakeService.SaveCompiledMapAsset(compiled, assetPath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void ClearPreviewWithUndo(MapGeneratorWorkspace workspace, string undoName)
        {
            Undo.SetCurrentGroupName(undoName);
            var root = workspace.ResolvePreviewRoot();
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
            }
        }

        private static void MarkSceneDirty(MapGeneratorWorkspace workspace)
        {
            if (workspace != null && workspace.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(workspace.gameObject.scene);
            }
        }

        private readonly struct GeneratedMapResult
        {
            public readonly EditableMapDraftAsset Draft;
            public readonly CompiledMap Compiled;
            public readonly MapValidationReport Report;

            public GeneratedMapResult(EditableMapDraftAsset draft, CompiledMap compiled, MapValidationReport report)
            {
                Draft = draft;
                Compiled = compiled;
                Report = report;
            }
        }

        private sealed class PreviewMaterialCache
        {
            public readonly Material Edge = CreateMaterial("Map Preview Edge", new Color(0.75f, 0.75f, 0.75f));
            private readonly Material start = CreateMaterial("Map Preview Start", new Color(0.2f, 0.55f, 0.95f));
            private readonly Material main = CreateMaterial("Map Preview Main", new Color(0.32f, 0.32f, 0.36f));
            private readonly Material quest = CreateMaterial("Map Preview Quest", new Color(0.95f, 0.78f, 0.25f));
            private readonly Material boss = CreateMaterial("Map Preview Boss", new Color(0.78f, 0.18f, 0.18f));
            private readonly Material exit = CreateMaterial("Map Preview Exit", new Color(0.2f, 0.75f, 0.35f));
            private readonly Material side = CreateMaterial("Map Preview Side", new Color(0.38f, 0.28f, 0.55f));
            private readonly Material placement = CreateMaterial("Map Preview Placement", new Color(0.95f, 0.95f, 0.95f));

            public Material ForRole(MapRoomRole role)
            {
                switch (role)
                {
                    case MapRoomRole.Start:
                        return start;
                    case MapRoomRole.QuestTarget:
                        return quest;
                    case MapRoomRole.Boss:
                        return boss;
                    case MapRoomRole.Exit:
                        return exit;
                    case MapRoomRole.SideBranch:
                        return side;
                    default:
                        return main;
                }
            }

            public Material ForPlacement(MapPlacementKind kind)
            {
                switch (kind)
                {
                    case MapPlacementKind.Start:
                        return start;
                    case MapPlacementKind.QuestTarget:
                        return quest;
                    case MapPlacementKind.Boss:
                        return boss;
                    case MapPlacementKind.Exit:
                        return exit;
                    default:
                        return placement;
                }
            }

            private static Material CreateMaterial(string name, Color color)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = name,
                    color = color
                };
                return material;
            }
        }
    }

    public static class MapGeneratorWorkspaceSceneBuilder
    {
        private const string ScenePath = "Assets/Conn/Scenes/Editor/MapGenerator.unity";

        [MenuItem("Conn/Map/Create Map Generator Workspace Scene")]
        public static void CreateWorkspaceScene()
        {
            CreateWorkspaceSceneAsset(promptToSaveOpenScenes: true);
        }

        public static void CreateWorkspaceSceneBatch()
        {
            CreateWorkspaceSceneAsset(promptToSaveOpenScenes: false);
        }

        private static void CreateWorkspaceSceneAsset(bool promptToSaveOpenScenes)
        {
            if (promptToSaveOpenScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder("Assets/Conn/Scenes");
            EnsureFolder("Assets/Conn/Scenes/Editor");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MapGenerator";

            var workspaceObject = new GameObject("Map Generator Workspace");
            var workspace = workspaceObject.AddComponent<MapGeneratorWorkspace>();

            var previewRoot = new GameObject("Preview Root").transform;
            previewRoot.SetParent(workspaceObject.transform, false);
            workspace.PreviewRoot = previewRoot;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Preview Floor";
            floor.transform.position = new Vector3(6f, -0.05f, 6f);
            floor.transform.localScale = new Vector3(2.4f, 1f, 2.4f);

            var cameraObject = new GameObject("Preview Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.position = new Vector3(6f, 9f, -9f);
            cameraObject.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            var lightObject = new GameObject("Preview Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save map generator workspace scene: {ScenePath}");
            }

            Selection.activeObject = workspaceObject;
            EditorGUIUtility.PingObject(workspaceObject);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}

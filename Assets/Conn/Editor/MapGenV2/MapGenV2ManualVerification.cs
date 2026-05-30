using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenV2ManualVerification
    {
        private const string LogPath = "Logs/MapGenV2ManualVerification.log";
        private const string ChecklistRoot = "Assets/Conn/Tests/TempMapGenV2ManualChecklist";

        [MenuItem("Conn/MapGenV2/Run Manual Verification")]
        public static void Run()
        {
            var result = RunTransientWorkflow();
            WriteResult(result);
        }

        public static void RunBatch()
        {
            var result = RunTransientWorkflow();
            WriteResult(result);

            if (result.StartsWith("PASS:", StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(result);
        }

        public static void RunStarterSetupBatch()
        {
            var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup();
            var result = VerifyStarterSetup(setup);
            WriteResult(result);

            if (result.StartsWith("PASS:", StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(result);
        }

        public static void RunChecklistBatch()
        {
            var result = VerifyManualChecklist();
            WriteResult(result);

            if (result.StartsWith("PASS:", StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(result);
        }

        public static string BuildManualChecklistSummary()
        {
            return "Manual checklist verifier covers starter setup, several mockup seeds/preview, room-shape edit and regenerate, style-set swap preserving abstract layout, real prefab materialization, materialized prefab save, runtime bake/load, and broken-profile diagnostics.";
        }

        private static string RunTransientWorkflow()
        {
            var moduleSet = ScriptableObject.CreateInstance<MapGenModuleSetAsset>();
            var styleSet = ScriptableObject.CreateInstance<MapGenStyleSetAsset>();
            var ruleSet = ScriptableObject.CreateInstance<MapGenRuleSetAsset>();
            var roomShape = ScriptableObject.CreateInstance<MapGenRoomShapeAsset>();
            var profile = ScriptableObject.CreateInstance<MapGenProfileAsset>();
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            var floor = new GameObject("MapGenV2_Verify_Floor");
            var wall = new GameObject("MapGenV2_Verify_Wall");

            try
            {
                PopulateValidRoomShape(roomShape);
                PopulateMinimumModuleSet(moduleSet, floor, wall);
                styleSet.ModuleSet = moduleSet;
                profile.ProfileId = "manual_verify_profile";
                profile.MapSize = new Vector2Int(8, 6);
                profile.StyleSet = styleSet;
                profile.LayoutRules = ruleSet;
                profile.RoomShapes = new[] { roomShape };
                draft.Profile = profile;
                draft.Seed = 2001;

                var profileReport = profile.Validate();
                if (!profileReport.IsValid)
                {
                    return BuildFailure("Profile validation failed", profileReport);
                }

                var generationReport = draft.GenerateFromProfile();
                if (!generationReport.IsValid)
                {
                    return BuildFailure("Mockup generation failed", generationReport);
                }

                draft.ApplyPostProcessingFromProfile();
                draft.Accept();

                var requests = MapGenMaterializationClassifier.Classify(draft.Width, draft.Height, draft.Cells);
                var propReport = MapGenPropPlacementValidator.Validate(draft.Width, draft.Height, draft.Cells);
                if (!propReport.IsValid)
                {
                    return BuildFailure("Prop validation failed", propReport);
                }

                var bakedCells = MapGenRuntimeBakeDataBuilder.BuildCells(draft.Width, draft.Height, draft.Cells);
                var traversalEdges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells);
                if (!draft.Accepted || !draft.IsAcceptedSignatureCurrent || bakedCells.Length == 0 || traversalEdges.Length == 0)
                {
                    return "FAIL: Runtime bake data check failed.";
                }

                return string.Join(Environment.NewLine, new[]
                {
                    "PASS: MapGenV2 transient workflow completed.",
                    $"Signature: {draft.AcceptedSignature}",
                    $"Cells: {draft.Cells.Length}",
                    $"Module requests: {requests.Count}",
                    $"Baked cells: {bakedCells.Length}",
                    $"Traversal edges: {traversalEdges.Length}"
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(draft);
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(roomShape);
                UnityEngine.Object.DestroyImmediate(ruleSet);
                UnityEngine.Object.DestroyImmediate(styleSet);
                UnityEngine.Object.DestroyImmediate(moduleSet);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(wall);
            }
        }

        private static void PopulateValidRoomShape(MapGenRoomShapeAsset roomShape)
        {
            roomShape.Resize(new Vector2Int(3, 3));
            roomShape.SetCell(1, 1, new MapGenShapeCell
            {
                State = MapGenCellState.Room,
                SocketKind = MapGenSocketKind.None,
                SocketId = string.Empty
            });
        }

        private static void PopulateMinimumModuleSet(MapGenModuleSetAsset moduleSet, GameObject floor, GameObject wall)
        {
            moduleSet.FloorsA = new[]
            {
                new MapGenModuleEntry
                {
                    Prefab = floor,
                    Weight = 1,
                    Footprint = Vector2Int.one
                }
            };
            moduleSet.WallsStraight = new[]
            {
                new MapGenModuleEntry
                {
                    Prefab = wall,
                    Weight = 1,
                    Footprint = Vector2Int.one
                }
            };
        }

        private static string BuildFailure(string header, MapGenValidationReport report)
        {
            using (var writer = new StringWriter())
            {
                writer.WriteLine($"FAIL: {header}");
                foreach (var issue in report.Issues)
                {
                    writer.WriteLine($"{issue.Code}: {issue.Message} Fix: {issue.SuggestedFix}");
                }

                return writer.ToString();
            }
        }

        private static string VerifyStarterSetup(MapGenV2StarterSetup setup)
        {
            if (setup == null || setup.Profile == null || setup.Draft == null)
            {
                return "FAIL: Starter setup did not create profile and draft.";
            }

            var profileReport = setup.Profile.Validate();
            if (!profileReport.IsValid)
            {
                return BuildFailure("Starter profile validation failed", profileReport);
            }

            if (string.IsNullOrWhiteSpace(setup.Profile.AuthoringNotes)
                || !setup.Profile.AuthoringNotes.Contains("placeholder prefabs"))
            {
                return "FAIL: Starter profile notes do not explain placeholder content.";
            }

            var generationReport = setup.Draft.GenerateFromProfile();
            if (!generationReport.IsValid)
            {
                return BuildFailure("Starter draft generation failed", generationReport);
            }

            setup.Draft.ApplyPostProcessingFromProfile();
            setup.Draft.Accept();
            var root = MapGenMockupMaterializer.Materialize(setup.Draft);
            var baked = MapGenRuntimeBakeUtility.Bake(setup.Draft);
            if (root == null || baked == null || baked.Cells.Length == 0 || baked.TraversalEdges.Length == 0)
            {
                return "FAIL: Starter setup materialize/bake failed.";
            }

            UnityEngine.Object.DestroyImmediate(root);
            return string.Join(Environment.NewLine, new[]
            {
                "PASS: MapGenV2 starter setup workflow completed.",
                $"Profile: {setup.Profile.ProfileId}",
                $"Signature: {setup.Draft.AcceptedSignature}",
                $"Baked cells: {baked.Cells.Length}",
                $"Traversal edges: {baked.TraversalEdges.Length}"
            });
        }

        private static string VerifyManualChecklist()
        {
            var checks = new System.Collections.Generic.List<string>();
            GameObject root = null;
            GameObject host = null;

            try
            {
                DeleteAssetIfExists(ChecklistRoot);
                var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup(ChecklistRoot);
                if (setup == null || setup.Profile == null || setup.Draft == null || !setup.Profile.Validate().IsValid)
                {
                    return "FAIL: create starter setup from empty scene.";
                }

                checks.Add("create starter setup from empty scene");

                var seedSignatures = new System.Collections.Generic.HashSet<string>();
                for (var seed = 2001; seed <= 2003; seed++)
                {
                    setup.Draft.Seed = seed;
                    var report = setup.Draft.GenerateFromProfile();
                    var preview = MapGenMockupPreviewData.FromDraft(setup.Draft);
                    if (!report.IsValid || preview.Summary.RoomCells <= 0 || preview.Summary.RegionCount <= 0)
                    {
                        return $"FAIL: generate several mockup seeds and inspect preview at seed {seed}.";
                    }

                    seedSignatures.Add(preview.CurrentSignature);
                }

                if (seedSignatures.Count < 2)
                {
                    return "FAIL: several mockup seeds did not produce distinguishable preview signatures.";
                }

                checks.Add("generate several mockup seeds and inspect preview");

                var shape = setup.Profile.RoomShapes != null && setup.Profile.RoomShapes.Length > 0
                    ? setup.Profile.RoomShapes[0]
                    : null;
                if (shape == null)
                {
                    return "FAIL: starter setup has no editable room shape.";
                }

                var beforeShapeSummary = MapGenRoomShapeAssetEditor.BuildAuthoringSummary(shape);
                Undo.RecordObject(shape, "Manual Checklist Room Shape Edit");
                shape.SetCell(0, 1, new MapGenShapeCell { State = MapGenCellState.Blocked, SocketKind = MapGenSocketKind.Blocked });
                EditorUtility.SetDirty(shape);
                setup.Draft.Seed = 2010;
                var editedShapeReport = setup.Draft.GenerateFromProfile();
                var afterShapeSummary = MapGenRoomShapeAssetEditor.BuildAuthoringSummary(shape);
                if (!editedShapeReport.IsValid || beforeShapeSummary == afterShapeSummary)
                {
                    return "FAIL: edit room shapes and regenerate.";
                }

                checks.Add("edit room shapes and regenerate");

                setup.Draft.ApplyPostProcessingFromProfile();
                setup.Draft.Accept();
                var acceptedLayout = BuildAbstractLayoutSignature(setup.Draft);
                var altStyle = ScriptableObjectUtility.CreateAsset<MapGenStyleSetAsset>(
                    AssetDatabase.GenerateUniqueAssetPath($"{ChecklistRoot}/StyleSets/ChecklistAltStyleSet.asset"));
                altStyle.StyleId = "checklist_alt_style";
                altStyle.ModuleSet = setup.Profile.StyleSet.ModuleSet;
                altStyle.RoomShapePool = setup.Profile.StyleSet.RoomShapePool;
                altStyle.RoomTemplates = setup.Profile.StyleSet.RoomTemplates;
                altStyle.CorridorTemplates = setup.Profile.StyleSet.CorridorTemplates;
                altStyle.LightingPreset = "alternate";
                setup.Profile.StyleSet = altStyle;
                EditorUtility.SetDirty(setup.Profile);
                if (BuildAbstractLayoutSignature(setup.Draft) != acceptedLayout)
                {
                    return "FAIL: swap style sets without changing abstract layout.";
                }

                setup.Draft.Accept();
                checks.Add("swap style sets without changing abstract layout");

                root = MapGenMockupMaterializer.Materialize(setup.Draft);
                var moduleMarkers = root != null
                    ? root.GetComponentsInChildren<MapGenV2MaterializedModuleMarker>()
                    : Array.Empty<MapGenV2MaterializedModuleMarker>();
                if (root == null || Array.Find(moduleMarkers, marker => marker.PrefabName != "Placeholder" && marker.ModuleCategory != MapGenModuleCategory.NavigationHelper) == null)
                {
                    return "FAIL: materialize real project prefabs.";
                }

                checks.Add("materialize real project prefabs");

                var prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{ChecklistRoot}/MaterializedPrefabs/{root.name}_Checklist.prefab");
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    return "FAIL: save materialized prefab.";
                }

                checks.Add("save materialized prefab");

                var baked = MapGenRuntimeBakeUtility.Bake(setup.Draft);
                host = new GameObject("MapGenV2ManualChecklistRuntimeHost");
                var service = host.AddComponent<MapGenRuntimeMapService>();
                if (baked == null || !service.Load(baked) || !service.TryGetQuery(out var query) || query == null)
                {
                    return "FAIL: bake runtime asset and load it in a runtime scene.";
                }

                checks.Add("bake runtime asset and load it in a runtime scene");

                var originalStyle = setup.Profile.StyleSet;
                setup.Profile.StyleSet = null;
                var brokenReport = setup.Profile.Validate();
                setup.Profile.StyleSet = originalStyle;
                if (brokenReport.IsValid || !HasActionableIssue(brokenReport, "profile_missing_style_set"))
                {
                    return "FAIL: intentionally break profile data and confirm diagnostics are actionable.";
                }

                checks.Add("intentionally break profile data and confirm diagnostics are actionable");

                return string.Join(Environment.NewLine, new[]
                {
                    "PASS: MapGenV2 manual checklist verification completed.",
                    BuildManualChecklistSummary(),
                    $"Checks: {string.Join(", ", checks)}"
                });
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                if (host != null)
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }

                DeleteAssetIfExists(ChecklistRoot);
            }
        }

        private static string BuildAbstractLayoutSignature(MapGenMockupDraftAsset draft)
        {
            if (draft == null)
            {
                return string.Empty;
            }

            draft.EnsureCellArray();
            using (var writer = new StringWriter())
            {
                writer.Write($"{draft.Width}x{draft.Height};");
                foreach (var cell in draft.Cells ?? Array.Empty<MapGenMockupCell>())
                {
                    writer.Write((int)cell.State);
                    writer.Write(':');
                    writer.Write(cell.RegionId);
                    writer.Write(':');
                    writer.Write((int)cell.RoomCategory);
                    writer.Write('|');
                }

                return writer.ToString();
            }
        }

        private static bool HasActionableIssue(MapGenValidationReport report, string code)
        {
            foreach (var issue in report?.Issues ?? Array.Empty<MapGenIssue>())
            {
                if (issue.Code == code && !string.IsNullOrWhiteSpace(issue.SuggestedFix))
                {
                    return true;
                }
            }

            return false;
        }

        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void WriteResult(string result)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(LogPath, result);
            Debug.Log($"MapGenV2 manual verification written to {LogPath}\n{result}");
        }
    }
}

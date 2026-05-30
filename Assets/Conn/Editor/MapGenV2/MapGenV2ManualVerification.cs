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

        private static void WriteResult(string result)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(LogPath, result);
            Debug.Log($"MapGenV2 manual verification written to {LogPath}\n{result}");
        }
    }
}

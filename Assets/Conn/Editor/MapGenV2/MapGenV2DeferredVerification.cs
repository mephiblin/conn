using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenV2DeferredVerification
    {
        private const string TempRoot = "Assets/Conn/Editor/MapGenV2/VerificationGenerated";
        private const string LogPath = "Logs/MapGenV2DeferredVerification.log";
        private const string PersistenceLogPath = "Logs/MapGenV2DeferredPersistenceVerification.log";

        public static void RunBatch()
        {
            var log = new VerificationLog();
            try
            {
                CleanupTempAssets();
                EnsureTempFolders();

                VerifySeedSweep(log);
                var context = CreateDesignerAuthoredAssetSet();
                VerifyDesignerProfiles(log, context);
                VerifyStyleSwap(log, context);
                VerifyPrefabModuleMaterialization(log, context);
                VerifySceneOutput(log, context);
                VerifyRuntimeConsumerQueries(log, context);
                VerifyPropBlockerValidation(log, context);
                VerifyUndoRedoRoomShapeEditing(log, context);
                VerifyLegacyBoundary(log);
                VerifyPrefabOutput(log, context);
                PersistAssetsForRestartCheck(log, context);

                WriteLog(LogPath, log.Build("PASS: MapGenV2 deferred verification completed."));
            }
            catch (Exception ex)
            {
                WriteLog(LogPath, log.Build($"FAIL: {ex.Message}\n{ex}"));
                throw;
            }
        }

        public static void RunPersistenceBatch()
        {
            var log = new VerificationLog();
            try
            {
                var profile = LoadRequired<MapGenProfileAsset>($"{TempRoot}/Profiles/DesignerProfile_A.asset");
                var shape = LoadRequired<MapGenRoomShapeAsset>($"{TempRoot}/Shapes/DesignerShape_A.asset");
                var draft = LoadRequired<MapGenMockupDraftAsset>($"{TempRoot}/Drafts/DesignerDraft_A.asset");
                var baked = LoadRequired<MapGenBakedMapAsset>($"{TempRoot}/Baked/DesignerProfile_A_3100_BakedMap.asset");

                Require(profile.Validate().IsValid, "Persisted profile failed validation.");
                Require(shape.Validate().IsValid, "Persisted room shape failed validation.");
                Require(draft.Accepted, "Persisted current mockup lost confirmed state.");
                Require(draft.IsAcceptedSignatureCurrent, "Persisted current mockup confirmation signature is stale.");
                Require(baked.Cells.Length > 0, "Persisted baked map has no cells.");
                Require(baked.TraversalEdges.Length > 0, "Persisted baked map has no traversal edges.");
                Require(baked.SourceSignature == draft.AcceptedSignature, "Persisted baked map signature does not match draft.");

                log.Pass("Saved assets reloaded in a fresh Unity batch process.");
                log.Pass($"Reloaded profile={profile.ProfileId}, draftSignature={draft.AcceptedSignature}, bakedCells={baked.Cells.Length}.");
                CleanupTempAssets();
                WriteLog(PersistenceLogPath, log.Build("PASS: MapGenV2 deferred persistence verification completed."));
            }
            catch (Exception ex)
            {
                WriteLog(PersistenceLogPath, log.Build($"FAIL: {ex.Message}\n{ex}"));
                throw;
            }
        }

        private static VerificationContext CreateDesignerAuthoredAssetSet()
        {
            var floorA = CreatePrefab("FloorA", PrimitiveType.Cube);
            var floorB = CreatePrefab("FloorB", PrimitiveType.Cube);
            var wall = CreatePrefab("WallStraight", PrimitiveType.Cube);
            var corner = CreatePrefab("WallCornerOutside", PrimitiveType.Cube);
            var ceiling = CreatePrefab("CeilingInterior", PrimitiveType.Cube);
            var door = CreatePrefab("DoorWhole", PrimitiveType.Cube);
            var prop = CreatePrefab("PropMarker", PrimitiveType.Sphere);

            var moduleSetA = CreateAsset<MapGenModuleSetAsset>($"{TempRoot}/Modules/ModuleSet_A.asset");
            moduleSetA.ModuleSetId = "deferred_module_set_a";
            PopulateFullModuleSet(moduleSetA, floorA, floorB, wall, corner, ceiling, door, prop);

            var moduleSetB = CreateAsset<MapGenModuleSetAsset>($"{TempRoot}/Modules/ModuleSet_B.asset");
            moduleSetB.ModuleSetId = "deferred_module_set_b";
            PopulateFullModuleSet(moduleSetB, floorB, floorA, wall, corner, ceiling, door, prop);

            var styleA = CreateAsset<MapGenStyleSetAsset>($"{TempRoot}/Styles/Style_A.asset");
            styleA.StyleId = "deferred_style_a";
            styleA.ModuleSet = moduleSetA;

            var styleB = CreateAsset<MapGenStyleSetAsset>($"{TempRoot}/Styles/Style_B.asset");
            styleB.StyleId = "deferred_style_b";
            styleB.ModuleSet = moduleSetB;

            var rules = CreateAsset<MapGenRuleSetAsset>($"{TempRoot}/Rules/RuleSet_A.asset");
            rules.RequiredRoomCategories = new[]
            {
                MapGenRoomCategory.Start,
                MapGenRoomCategory.Quest,
                MapGenRoomCategory.Boss,
                MapGenRoomCategory.Exit
            };
            rules.UseDirectRoutes = true;
            rules.ReduceDeadEnds = true;

            var shapeA = CreateAsset<MapGenRoomShapeAsset>($"{TempRoot}/Shapes/DesignerShape_A.asset");
            PopulateShape(shapeA, "designer_shape_a", MapGenRoomCategory.Main, new Vector2Int(3, 3));

            var shapeB = CreateAsset<MapGenRoomShapeAsset>($"{TempRoot}/Shapes/DesignerShape_B.asset");
            PopulateShape(shapeB, "designer_shape_b", MapGenRoomCategory.Quest, new Vector2Int(4, 3));

            var profileA = CreateAsset<MapGenProfileAsset>($"{TempRoot}/Profiles/DesignerProfile_A.asset");
            PopulateProfile(profileA, "DesignerProfile_A", styleA, rules, new[] { shapeA, shapeB }, new Vector2Int(10, 8), 3100);

            var profileB = CreateAsset<MapGenProfileAsset>($"{TempRoot}/Profiles/DesignerProfile_B.asset");
            PopulateProfile(profileB, "DesignerProfile_B", styleA, rules, new[] { shapeA }, new Vector2Int(12, 8), 3200);

            var profileC = CreateAsset<MapGenProfileAsset>($"{TempRoot}/Profiles/DesignerProfile_C.asset");
            PopulateProfile(profileC, "DesignerProfile_C", styleB, rules, new[] { shapeB }, new Vector2Int(8, 6), 3300);

            EditorUtility.SetDirty(moduleSetA);
            EditorUtility.SetDirty(moduleSetB);
            EditorUtility.SetDirty(styleA);
            EditorUtility.SetDirty(styleB);
            EditorUtility.SetDirty(rules);
            EditorUtility.SetDirty(shapeA);
            EditorUtility.SetDirty(shapeB);
            EditorUtility.SetDirty(profileA);
            EditorUtility.SetDirty(profileB);
            EditorUtility.SetDirty(profileC);
            AssetDatabase.SaveAssets();

            return new VerificationContext(profileA, profileB, profileC, styleA, styleB);
        }

        private static void VerifySeedSweep(VerificationLog log)
        {
            var sizes = new[]
            {
                new Vector2Int(6, 6),
                new Vector2Int(8, 6),
                new Vector2Int(10, 8),
                new Vector2Int(12, 10)
            };
            var required = new[]
            {
                MapGenRoomCategory.Start,
                MapGenRoomCategory.Quest,
                MapGenRoomCategory.Boss,
                MapGenRoomCategory.Exit
            };
            var signatures = new HashSet<string>();
            var generated = 0;

            foreach (var size in sizes)
            {
                for (var seed = 1000; seed < 1010; seed++)
                {
                    var result = MapGenMockupSolver.Generate(size.x, size.y, seed, required);
                    Require(result.Success, $"Seed sweep failed for {size.x}x{size.y} seed {seed}.");
                    Require(MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(result.Width, result.Height, result.Cells).Length > 0,
                        $"Seed sweep produced no traversal edges for {size.x}x{size.y} seed {seed}.");
                    signatures.Add(result.Signature);
                    generated++;
                }
            }

            Require(signatures.Count > sizes.Length, "Seed sweep did not produce enough varied signatures.");
            log.Pass($"Seed sweep generated {generated} layouts across {sizes.Length} map sizes with {signatures.Count} unique signatures.");
        }

        private static void VerifyDesignerProfiles(VerificationLog log, VerificationContext context)
        {
            foreach (var profile in context.Profiles)
            {
                Require(profile.Validate().IsValid, $"{profile.ProfileId} failed validation.");
                var draft = GenerateAcceptedDraft(profile);
                Require(draft.Accepted && draft.IsAcceptedSignatureCurrent, $"{profile.ProfileId} draft was not accepted.");
                log.Pass($"Designer-authored profile verified: {profile.ProfileId}, signature={draft.AcceptedSignature}.");
            }
        }

        private static void VerifyStyleSwap(VerificationLog log, VerificationContext context)
        {
            var profile = context.ProfileA;
            var originalStyle = profile.StyleSet;
            var draft = GenerateAcceptedDraft(profile);
            var originalSignature = draft.AcceptedSignature;
            profile.StyleSet = context.StyleB;
            Require(profile.Validate().IsValid, "Profile failed validation after style swap.");
            Require(draft.ComputeSignature() == originalSignature, "Style swap changed abstract mockup layout signature.");
            profile.StyleSet = originalStyle;
            log.Pass("Multiple style sets verified on the same confirmed mockup without changing layout signature.");
        }

        private static void VerifyPrefabModuleMaterialization(VerificationLog log, VerificationContext context)
        {
            var draft = GenerateAcceptedDraft(context.ProfileA);
            var requests = MapGenMaterializationClassifier.Classify(draft.Width, draft.Height, draft.Cells);
            Require(requests.Count > 0, "Materialization classifier produced no requests.");
            var categories = new HashSet<MapGenModuleCategory>();
            foreach (var request in requests)
            {
                categories.Add(request.Category);
                var entries = context.ProfileA.StyleSet.ModuleSet.GetEntries(request.Category);
                if (entries.Length > 0)
                {
                    Require(entries[0].Prefab != null, $"Module category {request.Category} has no real prefab asset.");
                }
            }

            Require(categories.Contains(MapGenModuleCategory.FloorA), "Materialization did not request room floor modules.");
            Require(categories.Contains(MapGenModuleCategory.WallStraight), "Materialization did not request wall modules.");
            log.Pass($"Prefab-backed module selection verified for {requests.Count} materialization requests.");
        }

        private static void VerifySceneOutput(VerificationLog log, VerificationContext context)
        {
            var draft = GenerateAcceptedDraft(context.ProfileA);
            var root = MapGenMockupMaterializer.Materialize(draft);
            Require(root != null, "Materialized scene root was not created.");
            Require(root.transform.childCount > 1, "Materialized scene root has no module groups.");
            var scene = root.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            var scenePath = $"{TempRoot}/Scenes/DeferredMaterializedScene.unity";
            Require(EditorSceneManager.SaveScene(scene, scenePath), "Materialized verification scene was not saved.");
            log.Pass($"Materialized scene output saved with {root.transform.childCount} root groups.");
        }

        private static void VerifyRuntimeConsumerQueries(VerificationLog log, VerificationContext context)
        {
            var draft = GenerateAcceptedDraft(context.ProfileA);
            var cells = MapGenRuntimeBakeDataBuilder.BuildCells(draft.Width, draft.Height, draft.Cells);
            var edges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells);
            Require(cells.Length > 0, "Runtime consumer query check has no baked cells.");
            Require(edges.Length > 0, "Runtime consumer query check has no traversal edges.");
            Require(ContainsRoom(cells, MapGenRoomCategory.Start), "Runtime baked data has no start room.");
            Require(ContainsRoom(cells, MapGenRoomCategory.Exit), "Runtime baked data has no exit room.");
            log.Pass($"Runtime consumer query data verified: cells={cells.Length}, edges={edges.Length}.");
        }

        private static void VerifyPropBlockerValidation(VerificationLog log, VerificationContext context)
        {
            var draft = GenerateAcceptedDraft(context.ProfileA);
            var navigableIndex = Array.FindIndex(draft.Cells, cell => cell.State == MapGenCellState.Room);
            Require(navigableIndex >= 0, "Prop verification could not find navigable cell.");
            draft.Cells[navigableIndex].PropChannel = "blocker";
            Require(MapGenPropPlacementValidator.Validate(draft.Width, draft.Height, draft.Cells).IsValid,
                "Blocker prop channel on navigable cell failed validation.");

            var blockedIndex = Array.FindIndex(draft.Cells, cell => cell.State == MapGenCellState.Empty);
            Require(blockedIndex >= 0, "Prop verification could not find empty cell.");
            draft.Cells[blockedIndex].PropChannel = "blocker";
            Require(!MapGenPropPlacementValidator.Validate(draft.Width, draft.Height, draft.Cells).IsValid,
                "Blocker prop channel on non-navigable cell was not rejected.");
            log.Pass("Prop blocker validation verified for navigable and non-navigable cells.");
        }

        private static void VerifyUndoRedoRoomShapeEditing(VerificationLog log, VerificationContext context)
        {
            var shape = context.ProfileA.RoomShapes[0];
            var original = shape.GetCell(0, 0);
            Undo.RecordObject(shape, "Deferred Room Shape Edit");
            shape.SetCell(0, 0, new MapGenShapeCell
            {
                State = MapGenCellState.Room,
                SocketKind = MapGenSocketKind.Door,
                SocketId = "undo_check"
            });
            EditorUtility.SetDirty(shape);
            Undo.PerformUndo();
            Require(shape.GetCell(0, 0).State == original.State, "Undo did not restore room shape cell state.");
            Undo.PerformRedo();
            Require(shape.GetCell(0, 0).SocketId == "undo_check", "Redo did not restore room shape cell edit.");
            log.Pass("Room shape grid undo/redo editing verified.");
        }

        private static void VerifyLegacyBoundary(VerificationLog log)
        {
            Require(Directory.Exists("doc/dev/map_generator/legacy"), "Legacy map generator document folder is missing.");
            Require(AssetDatabase.IsValidFolder("Assets/Conn/Core/MapGenV2"), "MapGenV2 core folder is missing.");
            Require(AssetDatabase.IsValidFolder("Assets/Conn/Authoring/MapGenV2"), "MapGenV2 authoring folder is missing.");
            Require(AssetDatabase.IsValidFolder("Assets/Conn/Editor/MapGenV2"), "MapGenV2 editor folder is missing.");
            log.Pass("Legacy quarantine and MapGenV2 folder boundaries verified.");
        }

        private static void VerifyPrefabOutput(VerificationLog log, VerificationContext context)
        {
            var draft = GenerateAcceptedDraft(context.ProfileA);
            var root = MapGenMockupMaterializer.Materialize(draft);
            Require(root != null, "Generated prefab source root was not created.");
            var prefabPath = $"{TempRoot}/Prefabs/GeneratedMapPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Require(prefab != null, "Generated prefab output was not saved.");
            UnityEngine.Object.DestroyImmediate(root);
            log.Pass("Generated prefab output verified.");
        }

        private static void PersistAssetsForRestartCheck(VerificationLog log, VerificationContext context)
        {
            var draft = CreateAsset<MapGenMockupDraftAsset>($"{TempRoot}/Drafts/DesignerDraft_A.asset");
            draft.Profile = context.ProfileA;
            draft.Seed = context.ProfileA.Seed;
            Require(draft.GenerateFromProfile().IsValid, "Persistent draft generation failed.");
            draft.ApplyPostProcessingFromProfile();
            draft.Accept();
            EditorUtility.SetDirty(draft);

            var baked = CreateAsset<MapGenBakedMapAsset>($"{TempRoot}/Baked/DesignerProfile_A_3100_BakedMap.asset");
            baked.ProfileId = context.ProfileA.ProfileId;
            baked.Seed = draft.Seed;
            baked.SourceSignature = draft.AcceptedSignature;
            baked.Width = draft.Width;
            baked.Height = draft.Height;
            baked.Cells = MapGenRuntimeBakeDataBuilder.BuildCells(draft.Width, draft.Height, draft.Cells);
            baked.TraversalEdges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells);
            EditorUtility.SetDirty(baked);
            AssetDatabase.SaveAssets();
            log.Pass("Persistent profile, room shape, accepted draft, and baked map assets saved for fresh-process reload verification.");
        }

        private static MapGenMockupDraftAsset GenerateAcceptedDraft(MapGenProfileAsset profile)
        {
            var draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            draft.Profile = profile;
            draft.Seed = profile.Seed;
            var report = draft.GenerateFromProfile();
            Require(report.IsValid, $"{profile.ProfileId} generation failed. {FormatIssues(report)}");
            draft.ApplyPostProcessingFromProfile();
            draft.Accept();
            return draft;
        }

        private static bool ContainsRoom(MapGenBakedCell[] cells, MapGenRoomCategory category)
        {
            foreach (var cell in cells)
            {
                if (cell.State == MapGenCellState.Room && cell.RoomCategory == category)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject CreatePrefab(string name, PrimitiveType primitive)
        {
            var instance = GameObject.CreatePrimitive(primitive);
            instance.name = name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{TempRoot}/Prefabs/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            Require(prefab != null, $"Failed to create prefab asset {name}.");
            return prefab;
        }

        private static void PopulateFullModuleSet(
            MapGenModuleSetAsset moduleSet,
            GameObject floorA,
            GameObject floorB,
            GameObject wall,
            GameObject corner,
            GameObject ceiling,
            GameObject door,
            GameObject prop)
        {
            moduleSet.FloorsA = Entries(floorA);
            moduleSet.FloorsB = Entries(floorB);
            moduleSet.WallsStraight = Entries(wall);
            moduleSet.WallsCornerOutside = Entries(corner);
            moduleSet.InteriorCeilings = Entries(ceiling);
            moduleSet.ExteriorCeilings = Entries(ceiling);
            moduleSet.WholeDoors = Entries(door);
            moduleSet.PropCategories = Entries(prop);
        }

        private static MapGenModuleEntry[] Entries(GameObject prefab)
        {
            return new[]
            {
                new MapGenModuleEntry
                {
                    Prefab = prefab,
                    Weight = 1,
                    Footprint = Vector2Int.one
                }
            };
        }

        private static void PopulateShape(
            MapGenRoomShapeAsset shape,
            string shapeId,
            MapGenRoomCategory category,
            Vector2Int dimensions)
        {
            shape.ShapeId = shapeId;
            shape.Category = category;
            shape.Resize(dimensions);
            for (var y = 0; y < dimensions.y; y++)
            {
                for (var x = 0; x < dimensions.x; x++)
                {
                    var isCenter = x > 0 && y > 0 && x < dimensions.x - 1 && y < dimensions.y - 1;
                    shape.SetCell(x, y, new MapGenShapeCell
                    {
                        State = isCenter ? MapGenCellState.Room : MapGenCellState.Empty,
                        SocketKind = isCenter ? MapGenSocketKind.None : MapGenSocketKind.Door,
                        SocketId = isCenter ? string.Empty : "default"
                    });
                }
            }
        }

        private static void PopulateProfile(
            MapGenProfileAsset profile,
            string profileId,
            MapGenStyleSetAsset style,
            MapGenRuleSetAsset rules,
            MapGenRoomShapeAsset[] shapes,
            Vector2Int size,
            int seed)
        {
            profile.ProfileId = profileId;
            profile.DisplayName = profileId;
            profile.MapSize = size;
            profile.Seed = seed;
            profile.StyleSet = style;
            profile.LayoutRules = rules;
            profile.RoomShapes = shapes;
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, $"Required asset missing: {path}");
            return asset;
        }

        private static void EnsureTempFolders()
        {
            EnsureFolder("Assets/Conn/Editor/MapGenV2", "VerificationGenerated");
            EnsureFolder(TempRoot, "Prefabs");
            EnsureFolder(TempRoot, "Modules");
            EnsureFolder(TempRoot, "Styles");
            EnsureFolder(TempRoot, "Rules");
            EnsureFolder(TempRoot, "Shapes");
            EnsureFolder(TempRoot, "Profiles");
            EnsureFolder(TempRoot, "Drafts");
            EnsureFolder(TempRoot, "Baked");
            EnsureFolder(TempRoot, "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CleanupTempAssets()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }
        }

        private static void WriteLog(string path, string result)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(path, result);
            Debug.Log(result);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string FormatIssues(MapGenValidationReport report)
        {
            return string.Join(" | ", report.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
        }

        private sealed class VerificationContext
        {
            public VerificationContext(
                MapGenProfileAsset profileA,
                MapGenProfileAsset profileB,
                MapGenProfileAsset profileC,
                MapGenStyleSetAsset styleA,
                MapGenStyleSetAsset styleB)
            {
                ProfileA = profileA;
                ProfileB = profileB;
                ProfileC = profileC;
                StyleA = styleA;
                StyleB = styleB;
            }

            public MapGenProfileAsset ProfileA { get; }

            public MapGenProfileAsset ProfileB { get; }

            public MapGenProfileAsset ProfileC { get; }

            public MapGenStyleSetAsset StyleA { get; }

            public MapGenStyleSetAsset StyleB { get; }

            public MapGenProfileAsset[] Profiles => new[] { ProfileA, ProfileB, ProfileC };
        }

        private sealed class VerificationLog
        {
            private readonly List<string> lines = new List<string>();

            public void Pass(string message)
            {
                lines.Add($"PASS: {message}");
            }

            public string Build(string header)
            {
                return string.Join(Environment.NewLine, new[] { header }.Concat(lines));
            }
        }
    }
}

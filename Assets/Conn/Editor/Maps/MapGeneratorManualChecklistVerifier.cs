using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class MapGeneratorManualChecklistVerifier
    {
        private const string DraftProbePath = "Assets/Conn/Authoring/Maps/Drafts/manual_check_probe.asset";
        private const string CompiledProbePath = "Assets/Conn/Core/Maps/manual_check_probe_CompiledMap.asset";

        public static void RunBatch()
        {
            var createdProfileFolder = !AssetDatabase.IsValidFolder(MapProfileAuthoringSampleBuilder.Folder);
            var workspaceObject = default(GameObject);
            var profile = default(MapProfileAsset);
            var summary = new StringBuilder();

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(DraftProbePath);
                AssetDatabase.DeleteAsset(CompiledProbePath);

                profile = MapProfileAuthoringSampleBuilder.CreateChapterTwoSampleProfileAssets();
                Require(profile != null, "Create sample profile assets");
                Require(AssetDatabase.Contains(profile), "Create sample profile assets");
                summary.AppendLine("[x] Create sample profile assets");

                workspaceObject = new GameObject("Manual Checklist Workspace");
                var workspace = workspaceObject.AddComponent<MapGeneratorWorkspace>();
                var previewRoot = new GameObject("Manual Checklist Preview Root").transform;
                previewRoot.SetParent(workspaceObject.transform, false);
                workspace.PreviewRoot = previewRoot;
                workspace.MapProfile = profile;
                Require(ReferenceEquals(workspace.MapProfile, profile), "Assign profile to MapGeneratorWorkspace");
                summary.AppendLine("[x] Assign profile to MapGeneratorWorkspace");

                InvokeWorkspaceEditorAction("ValidateProfile", workspace);
                if (workspace.LastProfileReport == null || !workspace.LastProfileReport.Passed)
                {
                    var errors = workspace.LastProfileReport == null
                        ? "no validation report"
                        : string.Join("\n", workspace.LastProfileReport.Errors);
                    throw new InvalidOperationException($"Manual checklist failed at step: Validate profile\n{errors}");
                }
                summary.AppendLine("[x] Validate profile");

                workspace.Seed = 2001;
                InvokeWorkspaceEditorAction("GeneratePreview", workspace);
                var firstPreviewSignature = BuildPreviewSignature(workspace);
                Require(workspace.LastEditableDraft != null, "Generate preview");
                Require(previewRoot.childCount > 0, "Generate preview");
                Require(!string.IsNullOrWhiteSpace(firstPreviewSignature), "Generate preview");
                summary.AppendLine("[x] Generate preview");

                workspace.Seed = 2112;
                InvokeWorkspaceEditorAction("GeneratePreview", workspace);
                var secondPreviewSignature = BuildPreviewSignature(workspace);
                Require(previewRoot.childCount > 0, "Randomize seed and confirm visible layout changes");
                Require(!string.Equals(firstPreviewSignature, secondPreviewSignature, StringComparison.Ordinal), "Randomize seed and confirm visible layout changes");
                summary.AppendLine("[x] Randomize seed and confirm visible layout changes");

                AssetDatabase.DeleteAsset(DraftProbePath);
                InvokeWorkspaceEditorAction("AcceptPreviewAndSaveDraft", workspace);
                var acceptedDraft = workspace.CurrentEditableDraft;
                Require(acceptedDraft != null, "Accept preview as draft");
                var acceptedDraftPath = AssetDatabase.GetAssetPath(acceptedDraft);
                Require(!string.IsNullOrWhiteSpace(acceptedDraftPath), "Accept preview as draft");
                AssetDatabase.MoveAsset(acceptedDraftPath, DraftProbePath);
                acceptedDraft = AssetDatabase.LoadAssetAtPath<EditableMapDraftAsset>(DraftProbePath);
                workspace.CurrentEditableDraft = acceptedDraft;
                Require(acceptedDraft != null, "Accept preview as draft");
                summary.AppendLine("[x] Accept preview as draft");

                EditDraft(acceptedDraft);
                summary.AppendLine("[x] Edit draft");

                InvokeWorkspaceEditorAction("ValidateCurrentDraft", workspace);
                Require(workspace.LastReport != null && workspace.LastReport.Passed, "Validate draft");
                summary.AppendLine("[x] Validate draft");

                AssetDatabase.DeleteAsset(CompiledProbePath);
                InvokeWorkspaceEditorAction("BakeAndSaveCurrentDraft", workspace);
                Require(workspace.CurrentCompiledMapAsset != null, "Bake compiled map");
                var compiledPath = AssetDatabase.GetAssetPath(workspace.CurrentCompiledMapAsset);
                Require(!string.IsNullOrWhiteSpace(compiledPath), "Bake compiled map");
                AssetDatabase.MoveAsset(compiledPath, CompiledProbePath);
                Require(AssetDatabase.LoadAssetAtPath<CompiledMapAsset>(CompiledProbePath) != null, "Bake compiled map");
                summary.AppendLine("[x] Bake compiled map");

                Debug.Log("Manual checklist passed:\n" + summary);
            }
            finally
            {
                if (workspaceObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(workspaceObject);
                }

                AssetDatabase.DeleteAsset(DraftProbePath);
                AssetDatabase.DeleteAsset(CompiledProbePath);

                if (createdProfileFolder && AssetDatabase.IsValidFolder(MapProfileAuthoringSampleBuilder.Folder))
                {
                    AssetDatabase.DeleteAsset(MapProfileAuthoringSampleBuilder.Folder);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void InvokeWorkspaceEditorAction(string methodName, MapGeneratorWorkspace workspace)
        {
            var method = typeof(MapGeneratorWorkspaceEditor).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(typeof(MapGeneratorWorkspaceEditor).FullName, methodName);
            }

            method.Invoke(null, new object[] { workspace });
        }

        private static void EditDraft(EditableMapDraftAsset draft)
        {
            Require(draft != null, "Edit draft");
            var edited = false;
            for (var y = 0; y < draft.Height && !edited; y++)
            {
                for (var x = 0; x < draft.Width && !edited; x++)
                {
                    var cell = draft.GetCell(x, y);
                    if (cell.Terrain != RoomChunkCellType.Floor)
                    {
                        continue;
                    }

                    cell.MaterialId = "manual_check_edit";
                    Require(draft.TrySetCell(cell), "Edit draft");
                    edited = true;
                }
            }

            Require(edited, "Edit draft");
            EditorUtility.SetDirty(draft);
            AssetDatabase.SaveAssets();
        }

        private static string BuildPreviewSignature(MapGeneratorWorkspace workspace)
        {
            var draft = workspace.LastEditableDraft;
            if (draft == null || draft.Rooms == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append(draft.Seed).Append('|');
            foreach (var room in draft.Rooms.OrderBy(room => room.Id, StringComparer.Ordinal))
            {
                builder.Append(room.Id).Append(':')
                    .Append(room.Role).Append(':')
                    .Append(room.LayoutKind).Append(':')
                    .Append(room.X).Append(',').Append(room.Y).Append(':')
                    .Append(room.Width).Append(',').Append(room.Height).Append(':')
                    .Append(room.ChunkId).Append('|');
            }

            return builder.ToString();
        }

        private static void Require(bool condition, string step)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Manual checklist failed at step: {step}");
            }
        }
    }
}

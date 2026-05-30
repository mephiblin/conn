using Conn.MapGenV2.Authoring;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenModuleSetAsset))]
    public sealed class MapGenModuleSetAssetEditor : UnityEditor.Editor
    {
        private static bool showAdvancedModuleData;

        public override void OnInspectorGUI()
        {
            var moduleSet = (MapGenModuleSetAsset)target;
            DrawCoverageSummary(moduleSet);
            DrawAdvancedModuleData();
            MapGenValidationReportEditorGUI.Draw(moduleSet.Validate(), moduleSet, "Module set is valid.");
        }

        public static string BuildInspectorUxSummary()
        {
            return "Module set primary UX shows category coverage, missing required categories, and validation first; "
                + "advanced/debug foldout contains raw serialized module entry arrays and bounds contract settings.";
        }

        public static string BuildCoverageSummary(MapGenModuleSetAsset moduleSet)
        {
            if (moduleSet == null)
            {
                return "Module Set: (none)";
            }

            var report = moduleSet.Validate();
            var builder = new StringBuilder();
            builder.Append($"Floors {Count(moduleSet.FloorsA) + Count(moduleSet.FloorsB)}");
            builder.Append($", Walls {Count(moduleSet.WallsStraight)}");
            builder.Append($", Corners {Count(moduleSet.WallsCornerInside) + Count(moduleSet.WallsCornerOutside)}");
            builder.Append($", Ceilings {Count(moduleSet.InteriorCeilings) + Count(moduleSet.ExteriorCeilings)}");
            builder.Append($", Doors {Count(moduleSet.WholeDoors) + Count(moduleSet.HalfDoorFrames) + Count(moduleSet.HalfDoorPanels)}");
            builder.Append($", Blockers {Count(moduleSet.Blockers)}");
            builder.Append($", Props {Count(moduleSet.PropCategories) + Count(moduleSet.RequiredUniqueProps)}");
            builder.Append($", Missing required {CountMissingRequired(moduleSet)}");
            builder.Append($", Validation {(report.IsValid ? "Valid" : $"Issues {report.Issues.Count}")}");
            return builder.ToString();
        }

        private static void DrawCoverageSummary(MapGenModuleSetAsset moduleSet)
        {
            EditorGUILayout.LabelField("모듈 커버리지 / Module Coverage", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(BuildCoverageSummary(moduleSet), style);
                EditorGUILayout.HelpBox(BuildInspectorUxSummary(), MessageType.Info);
            }
        }

        private void DrawAdvancedModuleData()
        {
            EditorGUILayout.Space();
            showAdvancedModuleData = EditorGUILayout.Foldout(
                showAdvancedModuleData,
                "고급/디버그 모듈 데이터 / Advanced Debug Module Data",
                true);
            if (!showAdvancedModuleData)
            {
                return;
            }

            DrawDefaultInspector();
        }

        private static int Count(MapGenModuleEntry[] entries)
        {
            return entries?.Length ?? 0;
        }

        private static int CountMissingRequired(MapGenModuleSetAsset moduleSet)
        {
            var missing = 0;
            if (Count(moduleSet.FloorsA) == 0)
            {
                missing++;
            }

            if (Count(moduleSet.WallsStraight) == 0)
            {
                missing++;
            }

            return missing;
        }
    }
}

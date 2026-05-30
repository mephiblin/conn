using Conn.MapGenV2.Authoring;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenStyleSetAsset))]
    public sealed class MapGenStyleSetAssetEditor : UnityEditor.Editor
    {
        private static bool showAdvancedStyleData;

        public override void OnInspectorGUI()
        {
            var styleSet = (MapGenStyleSetAsset)target;
            DrawSummary(styleSet);
            DrawAdvancedStyleData();
            MapGenValidationReportEditorGUI.Draw(styleSet.Validate(), styleSet, "Style set is valid.");
        }

        public static string BuildSummary(MapGenStyleSetAsset styleSet)
        {
            if (styleSet == null)
            {
                return "Style Set: (none)";
            }

            var report = styleSet.Validate();
            var builder = new StringBuilder();
            builder.Append($"Style {Format(styleSet.StyleId)}");
            builder.Append($", Module Set {Format(styleSet.ModuleSet != null ? styleSet.ModuleSet.ModuleSetId : string.Empty)}");
            builder.Append($", Room Shapes {Count(styleSet.RoomShapePool)}");
            builder.Append($", Room Templates {Count(styleSet.RoomTemplates)}");
            builder.Append($", Corridor Templates {Count(styleSet.CorridorTemplates)}");
            builder.Append($", Lighting {Format(styleSet.LightingPreset)}");
            builder.Append($", Validation {(report.IsValid ? "Valid" : $"Issues {report.Issues.Count}")}");
            return builder.ToString();
        }

        public static string BuildInspectorUxSummary()
        {
            return "Style set primary UX shows style id, module set, room/corridor template counts, lighting preset, and validation first; "
                + "advanced/debug foldout contains raw serialized module, shape, room-template, corridor-template, and lighting fields.";
        }

        private static void DrawSummary(MapGenStyleSetAsset styleSet)
        {
            EditorGUILayout.LabelField("스타일 세트 요약 / Style Set Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(BuildSummary(styleSet), style);
                EditorGUILayout.HelpBox(
                    "스타일 세트는 abstract layout을 유지한 채 module/template palette를 교체하는 기준입니다.",
                    MessageType.Info);
                EditorGUILayout.HelpBox(BuildInspectorUxSummary(), MessageType.Info);
            }
        }

        private void DrawAdvancedStyleData()
        {
            EditorGUILayout.Space();
            showAdvancedStyleData = EditorGUILayout.Foldout(
                showAdvancedStyleData,
                "고급/디버그 스타일 데이터 / Advanced Debug Style Data",
                true);
            if (!showAdvancedStyleData)
            {
                return;
            }

            DrawDefaultInspector();
        }

        private static int Count(Object[] values)
        {
            return values?.Length ?? 0;
        }

        private static string Format(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}

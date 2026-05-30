using Conn.MapGenV2.Authoring;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenCorridorTemplateAsset))]
    public sealed class MapGenCorridorTemplateAssetEditor : UnityEditor.Editor
    {
        private static bool showAdvancedTemplateData;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var template = (MapGenCorridorTemplateAsset)target;

            EditorGUILayout.LabelField("복도 템플릿 요약 / Corridor Template Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Template Id", string.IsNullOrWhiteSpace(template.TemplateId) ? "(missing)" : template.TemplateId);
                EditorGUILayout.LabelField("Kind", $"{template.CorridorKind} / {template.TurnKind}");
                EditorGUILayout.LabelField("Width", template.Width.ToString());
                EditorGUILayout.LabelField("Length Range", $"{template.LengthRange.x} - {template.LengthRange.y}");
                EditorGUILayout.LabelField("Max Footprint", $"{Mathf.Max(1, template.LengthRange.y)} x {Mathf.Max(1, template.Width)}");
                EditorGUILayout.LabelField("Connectors", (template.Connectors?.Length ?? 0).ToString());
                EditorGUILayout.LabelField("Prop Channels", (template.PropChannels?.Length ?? 0).ToString());
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("기본 정보 / Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.TemplateId)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.CorridorKind)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.TurnKind)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.Width)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.LengthRange)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.Weight)));

            DrawAdvancedTemplateData();

            serializedObject.ApplyModifiedProperties();
            MapGenValidationReportEditorGUI.Draw(template.Validate(), template, "Corridor template is valid.");
        }

        public static string BuildInspectorUxSummary()
        {
            return "Corridor template primary UX shows kind, width, length range, and validation summary first; "
                + "advanced/debug foldout contains connector and prop-channel raw arrays.";
        }

        private void DrawAdvancedTemplateData()
        {
            EditorGUILayout.Space();
            showAdvancedTemplateData = EditorGUILayout.Foldout(
                showAdvancedTemplateData,
                "고급/디버그 복도 데이터 / Advanced Debug Corridor Data",
                true);
            if (!showAdvancedTemplateData)
            {
                EditorGUILayout.HelpBox(BuildInspectorUxSummary(), MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "Straight/turn/T/cross/variable-length corridor behavior is represented by kind, turn kind, width, length range, and connector placement.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.Connectors)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.PropChannels)), true);
        }
    }
}

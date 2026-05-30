using Conn.MapGenV2.Authoring;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenCorridorTemplateAsset))]
    public sealed class MapGenCorridorTemplateAssetEditor : UnityEditor.Editor
    {
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("커넥터/프롭 / Connectors And Props", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Straight/turn/T/cross/variable-length corridor behavior is represented by kind, turn kind, width, length range, and connector placement.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.Connectors)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenCorridorTemplateAsset.PropChannels)), true);

            serializedObject.ApplyModifiedProperties();
            MapGenValidationReportEditorGUI.Draw(template.Validate(), template, "Corridor template is valid.");
        }
    }
}

using Conn.MapGenV2.Authoring;
using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenRoomTemplateAsset))]
    public sealed class MapGenRoomTemplateAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var template = (MapGenRoomTemplateAsset)target;

            EditorGUILayout.LabelField("방 템플릿 요약 / Room Template Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Template Id", string.IsNullOrWhiteSpace(template.TemplateId) ? "(missing)" : template.TemplateId);
                EditorGUILayout.LabelField("Footprint", $"{template.Footprint.x} x {template.Footprint.y}");
                EditorGUILayout.LabelField("Category", template.RoomCategory.ToString());
                EditorGUILayout.LabelField("Floor Cells", (template.FloorCells?.Length ?? 0).ToString());
                EditorGUILayout.LabelField("Connectors", (template.Connectors?.Length ?? 0).ToString());
                EditorGUILayout.LabelField("Source Shapes", CountAssignedShapes(template).ToString());
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("기본 정보 / Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.TemplateId)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.Footprint)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.RoomCategory)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.SizeClass)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.Weight)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("소스 룸 셰이프 / Source Room Shapes", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "템플릿이 어떤 룸 셰이프에서 만들어졌는지 추적합니다. / Reference the room shape assets used to author this template.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.SourceRoomShapes)), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("셀/커넥터 데이터 / Cells And Connectors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.Connectors)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.FloorCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.WallCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.BlockedCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.DoorHintCells)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGenRoomTemplateAsset.PropChannels)), true);

            serializedObject.ApplyModifiedProperties();
            MapGenValidationReportEditorGUI.Draw(template.Validate(), template, "Room template is valid.");
        }

        private static int CountAssignedShapes(MapGenRoomTemplateAsset template)
        {
            var count = 0;
            foreach (var shape in template.SourceRoomShapes ?? System.Array.Empty<MapGenRoomShapeAsset>())
            {
                if (shape != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

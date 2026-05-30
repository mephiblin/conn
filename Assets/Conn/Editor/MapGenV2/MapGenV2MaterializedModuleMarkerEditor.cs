using Conn.MapGenV2.Authoring;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenV2MaterializedModuleMarker))]
    public sealed class MapGenV2MaterializedModuleMarkerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var marker = (MapGenV2MaterializedModuleMarker)target;
            DrawSourceSummary(marker);
            DrawDefaultInspector();
        }

        public static string BuildSourceSummary(MapGenV2MaterializedModuleMarker marker)
        {
            if (marker == null)
            {
                return "Materialized Source: (none)";
            }

            return $"Region {marker.RegionId}, Cell {marker.CellCoord.x},{marker.CellCoord.y}, "
                + $"Category {marker.ModuleCategory}, Direction {marker.Direction}, "
                + $"Template {FormatValue(marker.SourceTemplateId)}, Prefab {FormatValue(marker.PrefabName)}, "
                + $"Draft {FormatValue(marker.DraftSignature)}, Source {FormatValue(marker.SourceSignature)}, "
                + $"ModuleSet {FormatValue(marker.ModuleSetSignature)}";
        }

        private static void DrawSourceSummary(MapGenV2MaterializedModuleMarker marker)
        {
            EditorGUILayout.LabelField("소스 메타데이터 / Source Metadata", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(BuildSourceSummary(marker), style);
                EditorGUILayout.HelpBox(
                    "씬에서 생성된 모듈을 선택하면 원본 mockup region, grid cell, template, prefab, signature를 확인할 수 있습니다.",
                    MessageType.Info);
            }
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}

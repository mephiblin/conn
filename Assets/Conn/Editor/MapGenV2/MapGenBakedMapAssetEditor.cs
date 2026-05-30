using Conn.MapGenV2.Core;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenBakedMapAsset))]
    public sealed class MapGenBakedMapAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var baked = (MapGenBakedMapAsset)target;
            DrawRuntimeSummary(baked);
            DrawDefaultInspector();
        }

        public static string BuildRuntimeSummary(MapGenBakedMapAsset baked)
        {
            if (baked == null)
            {
                return "Baked Map: (none)";
            }

            var builder = new StringBuilder();
            builder.Append($"Grid {baked.Width}x{baked.Height}");
            builder.Append($", Cells {Count(baked.Cells)}");
            builder.Append($", Regions {Count(baked.Regions)}");
            builder.Append($", Connectors {Count(baked.Connectors)}");
            builder.Append($", Traversal graph {Count(baked.TraversalEdges)} edges");
            builder.Append($", Nav data {FormatNavAvailability(baked)}");
            builder.Append($", Props {Count(baked.Props)}");
            builder.Append($", Spawn markers {Count(baked.SpawnMarkers)}");
            builder.Append($", Objective markers {Count(baked.ObjectiveMarkers)}");
            builder.Append($", Profile {FormatValue(baked.ProfileId)}");
            builder.Append($", Rule {FormatValue(baked.RuleSetId)}");
            builder.Append($", Style {FormatValue(baked.StyleId)}");
            builder.Append($", Seed {baked.Seed}");
            builder.Append($", Signature {FormatValue(baked.SourceSignature)}");
            return builder.ToString();
        }

        private static void DrawRuntimeSummary(MapGenBakedMapAsset baked)
        {
            EditorGUILayout.LabelField("런타임 베이크 요약 / Runtime Bake Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(BuildRuntimeSummary(baked), style);
                EditorGUILayout.HelpBox(
                    "이 에셋은 런타임 로딩용 데이터만 포함해야 합니다. Editor-only draft, scene object, prefab reference는 저장하지 않습니다.",
                    MessageType.Info);
            }
        }

        private static string FormatNavAvailability(MapGenBakedMapAsset baked)
        {
            if (Count(baked.TraversalEdges) == 0)
            {
                return "No traversal graph";
            }

            return Count(baked.SpawnMarkers) > 0 || Count(baked.ObjectiveMarkers) > 0
                ? "Traversal graph + markers"
                : "Traversal graph only";
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        private static int Count<T>(T[] values)
        {
            return values?.Length ?? 0;
        }
    }
}

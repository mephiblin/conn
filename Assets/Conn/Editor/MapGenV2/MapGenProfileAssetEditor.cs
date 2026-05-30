using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenProfileAsset))]
    public sealed class MapGenProfileAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var profile = (MapGenProfileAsset)target;
            DrawSummary(profile);
            DrawDefaultInspector();
            MapGenValidationReportEditorGUI.Draw(profile.Validate(), profile, "Profile is valid.");
        }

        public static string BuildSummary(MapGenProfileAsset profile)
        {
            if (profile == null)
            {
                return "Profile: (none)";
            }

            var report = profile.Validate();
            var style = profile.StyleSet != null ? profile.StyleSet.StyleId : "(none)";
            var rule = profile.LayoutRules != null ? profile.LayoutRules.name : "(none)";
            var roomTemplateCount = profile.StyleSet != null ? profile.StyleSet.RoomTemplates?.Length ?? 0 : 0;
            var corridorTemplateCount = profile.StyleSet != null ? profile.StyleSet.CorridorTemplates?.Length ?? 0 : 0;
            var requiredCategories = profile.LayoutRules != null
                ? FormatCategories(profile.LayoutRules.QuantityRules.RequiredCategories)
                : "(none)";

            var builder = new StringBuilder();
            builder.Append($"Map {profile.MapSize.x}x{profile.MapSize.y}, Seed {profile.Seed}");
            builder.Append($", Style {style}, Rule {rule}");
            builder.Append($", Templates room {roomTemplateCount}/corridor {corridorTemplateCount}");
            builder.Append($", Required {requiredCategories}");
            builder.Append($", Validation {(report.IsValid ? "Valid" : $"Issues {report.Issues.Count}")}");
            return builder.ToString();
        }

        private static void DrawSummary(MapGenProfileAsset profile)
        {
            EditorGUILayout.LabelField("프로필 요약 / Profile Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(BuildSummary(profile), style);
            }
        }

        private static string FormatCategories(MapGenRoomCategory[] categories)
        {
            if (categories == null || categories.Length == 0)
            {
                return "(none)";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < categories.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(categories[i]);
            }

            return builder.ToString();
        }
    }
}

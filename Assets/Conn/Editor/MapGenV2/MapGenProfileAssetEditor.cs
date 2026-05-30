using Conn.MapGenV2.Authoring;
using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenProfileAsset))]
    public sealed class MapGenProfileAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var profile = (MapGenProfileAsset)target;
            var report = profile.Validate();
            if (report.IsValid)
            {
                EditorGUILayout.HelpBox("Profile is valid.", MessageType.Info);
                return;
            }

            foreach (var issue in report.Issues)
            {
                EditorGUILayout.HelpBox($"{issue.Message}\nFix: {issue.SuggestedFix}", MessageType.Warning);
            }
        }
    }
}

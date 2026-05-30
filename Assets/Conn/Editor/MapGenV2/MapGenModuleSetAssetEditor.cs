using Conn.MapGenV2.Authoring;
using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenModuleSetAsset))]
    public sealed class MapGenModuleSetAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var moduleSet = (MapGenModuleSetAsset)target;
            var report = moduleSet.Validate();
            if (report.IsValid)
            {
                EditorGUILayout.HelpBox("Module set is valid.", MessageType.Info);
                return;
            }

            foreach (var issue in report.Issues)
            {
                EditorGUILayout.HelpBox($"{issue.Message}\nFix: {issue.SuggestedFix}", MessageType.Warning);
            }
        }
    }
}

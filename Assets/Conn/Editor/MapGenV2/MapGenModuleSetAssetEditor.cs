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
            MapGenValidationReportEditorGUI.Draw(moduleSet.Validate(), moduleSet, "Module set is valid.");
        }
    }
}

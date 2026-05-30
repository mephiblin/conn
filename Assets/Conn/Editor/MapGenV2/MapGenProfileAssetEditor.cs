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
            MapGenValidationReportEditorGUI.Draw(profile.Validate(), profile, "Profile is valid.");
        }
    }
}

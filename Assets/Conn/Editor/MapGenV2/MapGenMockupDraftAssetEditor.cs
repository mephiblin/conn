using Conn.MapGenV2.Authoring;
using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenMockupDraftAsset))]
    public sealed class MapGenMockupDraftAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var draft = (MapGenMockupDraftAsset)target;
            EditorGUILayout.LabelField("Current Signature", draft.ComputeSignature());
            EditorGUILayout.LabelField("Accepted Current", draft.IsAcceptedSignatureCurrent ? "Yes" : "No");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayoutButton("Accept Mockup"))
                {
                    Undo.RecordObject(draft, "Accept Mockup Draft");
                    draft.Accept();
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayoutButton("Clear Acceptance"))
                {
                    Undo.RecordObject(draft, "Clear Mockup Acceptance");
                    draft.ClearAcceptance();
                    EditorUtility.SetDirty(draft);
                }
            }
        }

        private static bool GUILayoutButton(string text)
        {
            return UnityEngine.GUILayout.Button(text);
        }
    }
}

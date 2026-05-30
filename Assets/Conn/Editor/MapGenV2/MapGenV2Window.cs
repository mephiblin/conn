using Conn.MapGenV2.Authoring;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public sealed class MapGenV2Window : EditorWindow
    {
        private MapGenProfileAsset profile;
        private MapGenMockupDraftAsset draft;
        private Vector2 scroll;

        [MenuItem("Conn/MapGenV2/Map Generator")]
        public static void Open()
        {
            GetWindow<MapGenV2Window>("MapGenV2");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("MapGenV2", EditorStyles.boldLabel);
            profile = (MapGenProfileAsset)EditorGUILayout.ObjectField("Profile", profile, typeof(MapGenProfileAsset), false);
            draft = (MapGenMockupDraftAsset)EditorGUILayout.ObjectField("Draft", draft, typeof(MapGenMockupDraftAsset), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Default Folders"))
                {
                    MapGenV2AssetFolderUtility.CreateDefaultFolders();
                }

                if (GUILayout.Button("Create Draft"))
                {
                    CreateDraft();
                }
            }

            DrawProfileValidation();
            DrawDraftActions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawProfileValidation()
        {
            if (profile == null)
            {
                EditorGUILayout.HelpBox("Assign a profile.", MessageType.Info);
                return;
            }

            MapGenValidationReportEditorGUI.Draw(profile.Validate(), profile, "Profile is valid.");
        }

        private void DrawDraftActions()
        {
            if (draft == null)
            {
                EditorGUILayout.HelpBox("Create or assign a draft.", MessageType.Info);
                return;
            }

            if (profile != null && draft.Profile != profile)
            {
                if (GUILayout.Button("Assign Profile To Draft"))
                {
                    Undo.RecordObject(draft, "Assign MapGen Profile");
                    draft.Profile = profile;
                    EditorUtility.SetDirty(draft);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Mockup"))
                {
                    Undo.RecordObject(draft, "Generate Mockup");
                    draft.GenerateFromProfile();
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayout.Button("Run Post-Process"))
                {
                    Undo.RecordObject(draft, "Post-Process Mockup");
                    draft.ApplyPostProcessingFromProfile();
                    EditorUtility.SetDirty(draft);
                }

                if (GUILayout.Button("Accept"))
                {
                    Undo.RecordObject(draft, "Accept Mockup");
                    draft.Accept();
                    EditorUtility.SetDirty(draft);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!draft.Accepted || !draft.IsAcceptedSignatureCurrent))
                {
                    if (GUILayout.Button("Materialize"))
                    {
                        MapGenMockupMaterializer.Materialize(draft);
                    }

                    if (GUILayout.Button("Bake Runtime"))
                    {
                        MapGenRuntimeBakeUtility.Bake(draft);
                    }
                }
            }
        }

        private void CreateDraft()
        {
            MapGenV2AssetFolderUtility.CreateDefaultFolders();
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Conn/Authoring/MapGenV2/Drafts/MapGenMockupDraft.asset");
            draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            draft.Profile = profile;
            AssetDatabase.CreateAsset(draft, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = draft;
        }
    }
}

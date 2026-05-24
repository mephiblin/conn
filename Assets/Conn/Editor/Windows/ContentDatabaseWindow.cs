using Conn.Core.Content;
using Conn.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Windows
{
    public sealed class ContentDatabaseWindow : EditorWindow
    {
        private ContentDatabaseDefinition database;
        private Vector2 scroll;
        private string legacyDataPath = LegacyContentJsonImporter.DefaultLegacyDataPath;

        [MenuItem("Conn/Content Database/Window")]
        public static void Open()
        {
            GetWindow<ContentDatabaseWindow>("Content Database");
        }

        private void OnEnable()
        {
            database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
        }

        private void OnGUI()
        {
            database = (ContentDatabaseDefinition)EditorGUILayout.ObjectField("Database", database, typeof(ContentDatabaseDefinition), false);
            legacyDataPath = EditorGUILayout.TextField("Legacy JSON Path", legacyDataPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import Legacy JSON"))
                {
                    database = LegacyContentJsonImporter.Import(legacyDataPath, LegacyContentJsonImporter.DefaultDatabaseAssetPath);
                }

                if (GUILayout.Button("Validate"))
                {
                    LogReport(ContentDatabaseValidator.Validate(database));
                }
            }

            if (database == null)
            {
                EditorGUILayout.HelpBox("Create or import a content database asset.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Items", database.Items.Length.ToString());
            EditorGUILayout.LabelField("Equipment", database.Equipment.Length.ToString());
            EditorGUILayout.LabelField("Skills", database.Skills.Length.ToString());
            EditorGUILayout.LabelField("Monsters", database.Monsters.Length.ToString());
            EditorGUILayout.LabelField("Quests", database.Quests.Length.ToString());
            EditorGUILayout.LabelField("Vendors", database.Vendors.Length.ToString());
            EditorGUILayout.LabelField("NPCs", database.Npcs.Length.ToString());
            EditorGUILayout.EndScrollView();
        }

        private static void LogReport(ContentValidationReport report)
        {
            foreach (var warning in report.Warnings)
            {
                Debug.LogWarning(warning);
            }

            if (report.Passed)
            {
                Debug.Log($"Content database validation passed with {report.Warnings.Count} warning(s).");
                return;
            }

            foreach (var error in report.Errors)
            {
                Debug.LogError(error);
            }
        }
    }
}

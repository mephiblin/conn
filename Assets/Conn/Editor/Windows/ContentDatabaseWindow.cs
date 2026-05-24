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
            EditorGUILayout.LabelField("Encounters", (database.Encounters?.Length ?? 0).ToString());
            EditorGUILayout.LabelField("Quests", database.Quests.Length.ToString());
            EditorGUILayout.LabelField("Vendors", database.Vendors.Length.ToString());
            EditorGUILayout.LabelField("NPCs", database.Npcs.Length.ToString());
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vendor Rotation Entries", CountVendorRotations(database).ToString());
            EditorGUILayout.LabelField("Vendor Catalog References", CountVendorCatalogReferences(database).ToString());
            EditorGUILayout.LabelField("Quest -> Encounter -> Monster Links", CountQuestEncounterMonsterLinks(database).ToString());
            EditorGUILayout.EndScrollView();
        }

        private static int CountVendorRotations(ContentDatabaseDefinition database)
        {
            var count = 0;
            foreach (var vendor in database.Vendors)
            {
                count += vendor.Rotations?.Length ?? 0;
            }

            return count;
        }

        private static int CountVendorCatalogReferences(ContentDatabaseDefinition database)
        {
            var count = 0;
            foreach (var vendor in database.Vendors)
            {
                count += vendor.CatalogIds?.Length ?? 0;
                foreach (var rotation in vendor.Rotations ?? System.Array.Empty<ContentVendorRotationDefinition>())
                {
                    count += rotation.CatalogIds?.Length ?? 0;
                }
            }

            return count;
        }

        private static int CountQuestEncounterMonsterLinks(ContentDatabaseDefinition database)
        {
            var registry = database.BuildRegistry();
            var count = 0;
            foreach (var quest in database.Quests)
            {
                var encounter = registry.FindEncounter(quest.TargetEncounterId);
                if (encounter != null && encounter.MonsterId == quest.TargetMonsterId && registry.FindMonster(quest.TargetMonsterId) != null)
                {
                    count++;
                }
            }

            return count;
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

using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenV2AssetFolderUtility
    {
        private const string Root = "Assets/Conn/Authoring/MapGenV2";

        [MenuItem("Conn/MapGenV2/Create Default Folders")]
        public static void CreateDefaultFolders()
        {
            EnsureFolder("Assets/Conn/Authoring", "MapGenV2");
            EnsureFolder(Root, "Profiles");
            EnsureFolder(Root, "StyleSets");
            EnsureFolder(Root, "ModuleSets");
            EnsureFolder(Root, "RuleSets");
            EnsureFolder(Root, "RoomShapes");
            EnsureFolder(Root, "Templates");
            EnsureFolder(Root, "Drafts");
            EnsureFolder(Root, "MaterializedPrefabs");
            EnsureFolder("Assets/Conn/Core/MapGenV2", "BakedMaps");
            MapGenV2AssetDatabasePolicy.RefreshAfterBulkAssetChanges();
        }

        public static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var normalized = folderPath.Replace("\\", "/").Trim('/');
            var parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                return;
            }

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}

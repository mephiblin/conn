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
            AssetDatabase.Refresh();
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

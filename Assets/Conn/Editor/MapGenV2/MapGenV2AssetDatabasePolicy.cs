using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenV2AssetDatabasePolicy
    {
        public static void RefreshAfterBulkAssetChanges()
        {
            AssetDatabase.Refresh();
        }
    }
}

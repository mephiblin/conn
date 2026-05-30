using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public static class ScriptableObjectUtility
    {
        public static T CreateAsset<T>(string path)
            where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}

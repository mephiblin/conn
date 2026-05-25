using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.World
{
    public static class TownEnvironmentPrefabBuilder
    {
        public const string BackEnvironmentFolder = "Assets/Conn/2D/Back_Env";
        public const string MaterialFolder = "Assets/Conn/2D/Mat";
        public const string FloorTileTexturePath = BackEnvironmentFolder + "/Tile.png";
        public const string FloorTileMaterialPath = MaterialFolder + "/Tile.mat";
        public const string EnvironmentMaterialPath = MaterialFolder + "/TownEnvironment.mat";
        public const string PrefabFolder = "Assets/Conn/World/Prefabs";
        public const string FloorTilePrefabPath = PrefabFolder + "/TownFloorTile.prefab";
        public const string EnvironmentPrefabPath = PrefabFolder + "/TownEnvironment.prefab";

        private static readonly Vector2 DefaultFloorTileSize = new Vector2(1f, 1f);
        private static readonly Vector2Int DefaultEnvironmentTileCount = new Vector2Int(8, 8);
        private static readonly Color DefaultEnvironmentTint = new Color(0.82f, 0.78f, 0.68f, 1f);

        [MenuItem("Conn/World/Ensure Town Environment Assets")]
        public static void EnsureDefaultTownEnvironmentAssetsMenu()
        {
            var result = EnsureDefaultTownEnvironmentAssets();
            if (result.HasErrors)
            {
                throw new System.InvalidOperationException(string.Join("\n", result.Errors));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Town environment prefab/material assets are ready.");
        }

        public static TownEnvironmentBuildResult EnsureDefaultTownEnvironmentAssets()
        {
            return EnsureDefaultTownEnvironmentAssets(TownEnvironmentBuildOptions.Default());
        }

        public static TownEnvironmentBuildResult EnsureDefaultTownEnvironmentAssets(TownEnvironmentBuildOptions options)
        {
            options = options ?? TownEnvironmentBuildOptions.Default();

            var result = ValidateSourceAssets();
            if (result.HasErrors)
            {
                return result;
            }

            EnsureFolder(PrefabFolder);

            result.FloorMaterial = EnsureFloorTileMaterial(options.FloorTileMaterialPath);
            result.EnvironmentMaterial = EnsureEnvironmentMaterial(
                options.EnvironmentMaterialPath,
                result.FloorMaterial,
                options.EnvironmentTint);
            result.FloorTilePrefab = EnsureFloorTilePrefab(
                options.FloorTilePrefabPath,
                result.FloorMaterial,
                options.FloorTileSize);
            result.EnvironmentPrefab = EnsureEnvironmentPrefab(
                options.EnvironmentPrefabPath,
                result.FloorTilePrefab,
                result.EnvironmentMaterial,
                options.EnvironmentTileCount);

            return result;
        }

        public static TownEnvironmentBuildResult ValidateSourceAssets()
        {
            var result = new TownEnvironmentBuildResult();

            if (!AssetDatabase.IsValidFolder(BackEnvironmentFolder))
            {
                result.Errors.Add("Missing town environment source folder: " + BackEnvironmentFolder);
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                result.Errors.Add("Missing town material source folder: " + MaterialFolder);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FloorTileTexturePath);
            if (texture == null)
            {
                result.Errors.Add("Missing floor tile texture: " + FloorTileTexturePath);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(FloorTileMaterialPath);
            if (material == null)
            {
                result.Errors.Add("Missing floor tile material: " + FloorTileMaterialPath);
            }

            result.FloorTileTexture = texture;
            result.FloorMaterial = material;
            return result;
        }

        public static Texture2D LoadFloorTileTexture()
        {
            var importer = AssetImporter.GetAtPath(FloorTileTexturePath) as TextureImporter;
            if (importer != null)
            {
                var changed = false;
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(FloorTileTexturePath);
        }

        public static Material LoadFloorTileMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(FloorTileMaterialPath);
        }

        public static Material LoadEnvironmentMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(EnvironmentMaterialPath);
        }

        public static GameObject LoadFloorTilePrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(FloorTilePrefabPath);
        }

        public static GameObject LoadEnvironmentPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPrefabPath);
        }

        public static Material EnsureFloorTileMaterial(string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                EnsureAssetFolder(materialPath);
                material = new Material(DefaultShader());
                AssetDatabase.CreateAsset(material, materialPath);
            }

            ApplyMaterialTexture(material, LoadFloorTileTexture());
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material EnsureEnvironmentMaterial(string materialPath, Material sourceMaterial, Color tint)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                EnsureAssetFolder(materialPath);
                material = sourceMaterial != null
                    ? new Material(sourceMaterial)
                    : new Material(DefaultShader());
                AssetDatabase.CreateAsset(material, materialPath);
            }

            ApplyMaterialTexture(material, LoadFloorTileTexture());
            ApplyMaterialColor(material, tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static GameObject EnsureFloorTilePrefab(string prefabPath, Material material, Vector2 size)
        {
            EnsureAssetFolder(prefabPath);

            var tile = CreateFloorTileObject("TownFloorTile", material, size);
            try
            {
                return PrefabUtility.SaveAsPrefabAsset(tile, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(tile);
            }
        }

        public static GameObject EnsureEnvironmentPrefab(
            string prefabPath,
            GameObject floorTilePrefab,
            Material environmentMaterial,
            Vector2Int tileCount)
        {
            EnsureAssetFolder(prefabPath);

            var root = new GameObject("TownEnvironment");
            try
            {
                var countX = Mathf.Max(1, tileCount.x);
                var countY = Mathf.Max(1, tileCount.y);
                var offsetX = (countX - 1) * 0.5f;
                var offsetY = (countY - 1) * 0.5f;

                for (var y = 0; y < countY; y++)
                {
                    for (var x = 0; x < countX; x++)
                    {
                        GameObject tile;
                        if (floorTilePrefab != null)
                        {
                            tile = (GameObject)PrefabUtility.InstantiatePrefab(floorTilePrefab, root.transform);
                            tile.name = "FloorTile";
                        }
                        else
                        {
                            tile = CreateFloorTileObject("FloorTile", environmentMaterial, DefaultFloorTileSize);
                            tile.transform.SetParent(root.transform, false);
                        }

                        tile.transform.localPosition = new Vector3(x - offsetX, 0f, y - offsetY);
                    }
                }

                return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static GameObject CreateFloorTileObject(string name, Material material, Vector2 size)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = string.IsNullOrEmpty(name) ? "FloorTile" : name;
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tile.transform.localScale = new Vector3(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y), 1f);

            var collider = tile.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = tile.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return tile;
        }

        private static Shader DefaultShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
        }

        private static void ApplyMaterialTexture(Material material, Texture2D texture)
        {
            if (material == null || texture == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = slash > 0 ? path.Substring(0, slash) : "Assets";
            var folder = slash > 0 ? path.Substring(slash + 1) : path;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var slash = assetPath.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            EnsureFolder(assetPath.Substring(0, slash));
        }

        public sealed class TownEnvironmentBuildOptions
        {
            public string FloorTileMaterialPath = TownEnvironmentPrefabBuilder.FloorTileMaterialPath;
            public string EnvironmentMaterialPath = TownEnvironmentPrefabBuilder.EnvironmentMaterialPath;
            public string FloorTilePrefabPath = TownEnvironmentPrefabBuilder.FloorTilePrefabPath;
            public string EnvironmentPrefabPath = TownEnvironmentPrefabBuilder.EnvironmentPrefabPath;
            public Vector2 FloorTileSize = DefaultFloorTileSize;
            public Vector2Int EnvironmentTileCount = DefaultEnvironmentTileCount;
            public Color EnvironmentTint = DefaultEnvironmentTint;

            public static TownEnvironmentBuildOptions Default()
            {
                return new TownEnvironmentBuildOptions();
            }
        }

        public sealed class TownEnvironmentBuildResult
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public Texture2D FloorTileTexture;
            public Material FloorMaterial;
            public Material EnvironmentMaterial;
            public GameObject FloorTilePrefab;
            public GameObject EnvironmentPrefab;

            public bool HasErrors
            {
                get { return Errors.Count > 0; }
            }
        }
    }
}

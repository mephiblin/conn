using System;
using System.Collections.Generic;
using System.IO;
using Conn.Rendering.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Conn.Editor.World
{
    public static class NpcWorldPrefabBuilder
    {
        public const string NpcTextureFolder = "Assets/Conn/2D/NPC_2D";
        public const string MaterialFolder = "Assets/Conn/2D/Mat";
        public const string PrefabFolder = "Assets/Conn/Prefabs/NPC";
        public const string BillboardMeshAssetPath = PrefabFolder + "/NpcWorldBillboardQuad.asset";
        public const string VisualChildName = "Visual";

        public const float DefaultNpcHeight = 1.75f;
        public const float QuestBoardHeight = 1.4f;
        public const float GateHeight = 3.8f;

        [MenuItem("Conn/Build NPC World Prefabs")]
        public static void BuildAllNpcWorldPrefabs()
        {
            EnsureAllNpcWorldPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static GameObject[] EnsureAllNpcWorldPrefabs()
        {
            EnsureFolder("Assets/Conn");
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            var texturePaths = FindNpcTexturePaths();
            var prefabs = new List<GameObject>(texturePaths.Length);
            for (var i = 0; i < texturePaths.Length; i++)
            {
                var prefab = EnsureNpcWorldPrefab(texturePaths[i]);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }

            return prefabs.ToArray();
        }

        public static GameObject EnsureNpcWorldPrefab(string texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                throw new ArgumentException("NPC texture path must not be empty.", nameof(texturePath));
            }

            if (!texturePath.StartsWith(NpcTextureFolder + "/", StringComparison.Ordinal))
            {
                throw new ArgumentException($"NPC texture must be under {NpcTextureFolder}: {texturePath}", nameof(texturePath));
            }

            var texture = LoadNpcTexture(texturePath);
            if (texture == null)
            {
                throw new FileNotFoundException($"NPC texture was not found or could not be imported: {texturePath}", texturePath);
            }

            EnsureFolder("Assets/Conn");
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            var material = EnsureNpcWorldMaterial(texturePath);
            var prefabPath = PrefabPathForTexture(texturePath);
            var npcName = NpcNameForTexture(texturePath);

            var root = new GameObject(npcName);
            try
            {
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                ConfigureVisualChild(
                    root.transform,
                    texture,
                    material,
                    FallbackColorForNpc(npcName),
                    HeightForNpc(npcName),
                    FaceCameraForNpc(npcName));

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        public static GameObject InstantiateTownNpcPrefab(
            string name,
            Vector3 position,
            string texturePath,
            Color fallbackColor,
            float visualHeight,
            bool faceCamera,
            Type interactableType,
            Vector3 colliderSize,
            Vector3 colliderCenter)
        {
            var prefab = EnsureTownNpcWorldPrefab(
                name,
                texturePath,
                fallbackColor,
                visualHeight,
                faceCamera,
                interactableType,
                colliderSize,
                colliderCenter);
            var instance = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : new GameObject(string.IsNullOrWhiteSpace(name) ? "Town NPC" : name);

            instance.name = string.IsNullOrWhiteSpace(name) ? instance.name : name;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        public static GameObject EnsureNpcWorldPrefab(
            string npcName,
            string texturePath,
            Color fallbackColor,
            float visualHeight,
            bool faceCamera)
        {
            EnsureFolder("Assets/Conn");
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            var resolvedName = string.IsNullOrWhiteSpace(npcName) ? NpcNameForTexture(texturePath) : npcName;
            var texture = string.IsNullOrWhiteSpace(texturePath) ? null : LoadNpcTexture(texturePath);
            var material = string.IsNullOrWhiteSpace(texturePath)
                ? EnsureFallbackMaterial(resolvedName)
                : EnsureNpcWorldMaterial(texturePath);
            var prefabPath = $"{PrefabFolder}/{resolvedName}.prefab";

            var root = new GameObject(resolvedName);
            try
            {
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                ConfigureVisualChild(root.transform, texture, material, fallbackColor, visualHeight, faceCamera);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        public static GameObject EnsureTownNpcWorldPrefab(
            string npcName,
            string texturePath,
            Color fallbackColor,
            float visualHeight,
            bool faceCamera,
            Type interactableType,
            Vector3 colliderSize,
            Vector3 colliderCenter)
        {
            EnsureFolder("Assets/Conn");
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            var resolvedName = string.IsNullOrWhiteSpace(npcName) ? NpcNameForTexture(texturePath) : npcName;
            var texture = string.IsNullOrWhiteSpace(texturePath) ? null : LoadNpcTexture(texturePath);
            var material = string.IsNullOrWhiteSpace(texturePath)
                ? EnsureFallbackMaterial(resolvedName)
                : EnsureNpcWorldMaterial(texturePath);
            var prefabPath = $"{PrefabFolder}/{resolvedName}.prefab";

            var root = new GameObject(resolvedName);
            try
            {
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                ConfigureVisualChild(root.transform, texture, material, fallbackColor, visualHeight, faceCamera);
                EnsureBoxCollider(root, colliderSize, colliderCenter);
                EnsureInteractable(root, interactableType);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        public static Material EnsureNpcWorldMaterial(string texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                throw new ArgumentException("NPC texture path must not be empty.", nameof(texturePath));
            }

            var texture = LoadNpcTexture(texturePath);
            if (texture == null)
            {
                throw new FileNotFoundException($"NPC texture was not found or could not be imported: {texturePath}", texturePath);
            }

            EnsureFolder("Assets/Conn");
            EnsureFolder(MaterialFolder);

            var materialPath = MaterialPathForTexture(texturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(FindTransparentShader())
                {
                    name = Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader == null)
            {
                material.shader = FindTransparentShader();
            }

            ConfigureMaterial(material, texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureFallbackMaterial(string npcName)
        {
            EnsureFolder("Assets/Conn");
            EnsureFolder(MaterialFolder);

            var materialPath = $"{MaterialFolder}/{(string.IsNullOrWhiteSpace(npcName) ? "FallbackNpc" : npcName)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(FindTransparentShader())
                {
                    name = Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader == null)
            {
                material.shader = FindTransparentShader();
            }

            SetColorIfPresent(material, "_Color", Color.white);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureVisualChild(
            Transform root,
            Texture2D texture,
            Material material,
            Color fallbackColor,
            float visualHeight,
            bool faceCamera)
        {
            var visual = root.Find(VisualChildName)?.gameObject;
            if (visual == null)
            {
                visual = new GameObject(VisualChildName, typeof(MeshFilter), typeof(MeshRenderer), typeof(NpcWorldBillboard));
                visual.transform.SetParent(root, false);
            }

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            var meshFilter = visual.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = EnsureBillboardMeshAsset();

            var meshRenderer = visual.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var billboard = visual.GetComponent<NpcWorldBillboard>();
            billboard.Texture = texture;
            billboard.FallbackColor = fallbackColor;
            billboard.Height = visualHeight;
            billboard.FaceCamera = faceCamera;
        }

        private static void EnsureBoxCollider(GameObject root, Vector3 size, Vector3 center)
        {
            var collider = root.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = root.AddComponent<BoxCollider>();
            }

            collider.size = new Vector3(
                Mathf.Max(0.01f, size.x),
                Mathf.Max(0.01f, size.y),
                Mathf.Max(0.01f, size.z));
            collider.center = center;
        }

        private static void EnsureInteractable(GameObject root, Type interactableType)
        {
            if (interactableType == null)
            {
                return;
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(interactableType))
            {
                throw new ArgumentException($"Interactable type must derive from MonoBehaviour: {interactableType}", nameof(interactableType));
            }

            if (root.GetComponent(interactableType) == null)
            {
                root.AddComponent(interactableType);
            }
        }

        public static string MaterialPathForTexture(string texturePath)
        {
            return $"{MaterialFolder}/{NpcNameForTexture(texturePath)}.mat";
        }

        public static string PrefabPathForTexture(string texturePath)
        {
            return $"{PrefabFolder}/{NpcNameForTexture(texturePath)}.prefab";
        }

        private static string[] FindNpcTexturePaths()
        {
            if (!AssetDatabase.IsValidFolder(NpcTextureFolder))
            {
                return Array.Empty<string>();
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { NpcTextureFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith(NpcTextureFolder + "/", StringComparison.Ordinal)
                    && string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths.ToArray();
        }

        private static Texture2D LoadNpcTexture(string texturePath)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
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

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        private static void ConfigureMaterial(Material material, Texture2D texture)
        {
            material.mainTexture = texture;
            SetTextureIfPresent(material, "_MainTex", texture);
            SetTextureIfPresent(material, "_BaseMap", texture);
            SetColorIfPresent(material, "_Color", Color.white);
            SetColorIfPresent(material, "_BaseColor", Color.white);

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private static Shader FindTransparentShader()
        {
            return Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Standard");
        }

        private static Mesh EnsureBillboardMeshAsset()
        {
            EnsureFolder(PrefabFolder);

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BillboardMeshAssetPath);
            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "NpcWorldBillboardQuad"
                };
                ConfigureBillboardMesh(mesh);
                AssetDatabase.CreateAsset(mesh, BillboardMeshAssetPath);
            }
            else
            {
                ConfigureBillboardMesh(mesh);
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static void ConfigureBillboardMesh(Mesh mesh)
        {
            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(-0.5f, 1f, 0f),
                new Vector3(0.5f, 1f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
        }

        private static string NpcNameForTexture(string texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return "FallbackNpc";
            }

            return Path.GetFileNameWithoutExtension(texturePath);
        }

        private static float HeightForNpc(string npcName)
        {
            return npcName switch
            {
                "게시판" => QuestBoardHeight,
                "gate" => GateHeight,
                _ => DefaultNpcHeight
            };
        }

        private static bool FaceCameraForNpc(string npcName)
        {
            return npcName != "게시판" && npcName != "gate";
        }

        private static Color FallbackColorForNpc(string npcName)
        {
            return npcName switch
            {
                "게시판" => new Color(0.55f, 0.44f, 0.28f, 1f),
                "gate" => new Color(0.35f, 0.36f, 0.42f, 1f),
                "대장장이" => new Color(0.7f, 0.36f, 0.26f, 1f),
                "스킬상인" => new Color(0.35f, 0.48f, 0.78f, 1f),
                "여관주인" => new Color(0.66f, 0.48f, 0.3f, 1f),
                "약재상" => new Color(0.28f, 0.62f, 0.38f, 1f),
                "학자" => new Color(0.48f, 0.42f, 0.72f, 1f),
                _ => new Color(0.78f, 0.68f, 0.52f, 1f)
            };
        }

        private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
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
    }
}

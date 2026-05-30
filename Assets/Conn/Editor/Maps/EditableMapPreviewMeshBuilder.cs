using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapPreviewMeshBuilder
    {
        public const string RootPrefix = "Editable Map Preview Root";

        public static GameObject RebuildPreview(EditableMapDraftAsset draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            var root = GetOrCreatePreviewRoot(draft);
            ClearChildren(root.transform);

            var terrainRoot = CreateChild(root.transform, "Terrain Mesh");
            var wallRoot = CreateChild(root.transform, "Wall Mesh");
            var slopeRoot = CreateChild(root.transform, "Slope Mesh");
            var stairRoot = CreateChild(root.transform, "Stair Mesh");
            var objectRoot = CreateChild(root.transform, "Object Preview Root");
            CreateChild(root.transform, "Overlay Root");

            var terrainMeshes = new Dictionary<string, MeshBuffer>(StringComparer.Ordinal);
            var wallMeshes = new Dictionary<string, MeshBuffer>(StringComparer.Ordinal);
            var slopeMeshes = new Dictionary<string, MeshBuffer>(StringComparer.Ordinal);
            var stairMeshes = new Dictionary<string, MeshBuffer>(StringComparer.Ordinal);

            foreach (var cell in draft.Cells ?? Array.Empty<EditableMapCell>())
            {
                var baseHeight = cell.Height * Mathf.Max(0.01f, draft.HeightStep);
                var materialKey = ResolveMaterialKey(cell.MaterialId, cell.Terrain.ToString());
                switch (cell.Terrain)
                {
                    case RoomChunkCellType.Floor:
                        AddFloorQuad(GetBuffer(terrainMeshes, materialKey), draft, cell, baseHeight);
                        break;
                    case RoomChunkCellType.Wall:
                        AddWallCube(GetBuffer(wallMeshes, materialKey), draft, cell, baseHeight);
                        break;
                    case RoomChunkCellType.Slope:
                        AddSlope(GetBuffer(slopeMeshes, materialKey), draft, cell, baseHeight);
                        break;
                    case RoomChunkCellType.Stair:
                        AddStairs(GetBuffer(stairMeshes, materialKey), draft, cell, baseHeight);
                        break;
                }
            }

            BuildMeshObjects(terrainRoot.transform, terrainMeshes, draft);
            BuildMeshObjects(wallRoot.transform, wallMeshes, draft);
            BuildMeshObjects(slopeRoot.transform, slopeMeshes, draft);
            BuildMeshObjects(stairRoot.transform, stairMeshes, draft);
            BuildObjectPreview(objectRoot.transform, draft);

            return root;
        }

        public static void ClearPreview(EditableMapDraftAsset draft)
        {
            var root = FindPreviewRoot(draft);
            if (root == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject FindPreviewRoot(EditableMapDraftAsset draft)
        {
            return GameObject.Find(BuildRootName(draft));
        }

        private static GameObject GetOrCreatePreviewRoot(EditableMapDraftAsset draft)
        {
            var root = FindPreviewRoot(draft);
            if (root != null)
            {
                return root;
            }

            root = new GameObject(BuildRootName(draft));
            Undo.RegisterCreatedObjectUndo(root, "Create Editable Map Preview Root");
            return root;
        }

        private static string BuildRootName(EditableMapDraftAsset draft)
        {
            return $"{RootPrefix} ({draft.name})";
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child, "Create Editable Map Preview Child");
            return child;
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        private static MeshBuffer GetBuffer(Dictionary<string, MeshBuffer> buffers, string key)
        {
            if (!buffers.TryGetValue(key, out var buffer))
            {
                buffer = new MeshBuffer();
                buffers.Add(key, buffer);
            }

            return buffer;
        }

        private static void BuildMeshObjects(Transform parent, Dictionary<string, MeshBuffer> buffers, EditableMapDraftAsset draft)
        {
            foreach (var pair in buffers)
            {
                if (pair.Value.Vertices.Count == 0)
                {
                    continue;
                }

                var child = new GameObject(pair.Key);
                child.transform.SetParent(parent, false);
                var mesh = new Mesh
                {
                    name = pair.Key
                };
                mesh.SetVertices(pair.Value.Vertices);
                mesh.SetTriangles(pair.Value.Triangles, 0);
                mesh.SetUVs(0, pair.Value.UVs);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.AddComponent<MeshRenderer>().sharedMaterial = PreviewMaterialCache.ForId(pair.Key, draft);
                Undo.RegisterCreatedObjectUndo(child, "Create Editable Map Preview Mesh");
            }
        }

        private static void AddFloorQuad(MeshBuffer buffer, EditableMapDraftAsset draft, EditableMapCell cell, float baseHeight)
        {
            var x = cell.X * draft.CellSize;
            var z = cell.Y * draft.CellSize;
            var y = baseHeight;
            var a = new Vector3(x, y, z);
            var b = new Vector3(x + draft.CellSize, y, z);
            var c = new Vector3(x + draft.CellSize, y, z + draft.CellSize);
            var d = new Vector3(x, y, z + draft.CellSize);
            AddQuad(buffer, a, b, c, d, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        }

        private static void AddWallCube(MeshBuffer buffer, EditableMapDraftAsset draft, EditableMapCell cell, float baseHeight)
        {
            var min = new Vector3(cell.X * draft.CellSize, baseHeight, cell.Y * draft.CellSize);
            var max = new Vector3(min.x + draft.CellSize, baseHeight + draft.HeightStep, min.z + draft.CellSize);
            AddCube(buffer, min, max);
        }

        private static void AddSlope(MeshBuffer buffer, EditableMapDraftAsset draft, EditableMapCell cell, float baseHeight)
        {
            var x = cell.X * draft.CellSize;
            var z = cell.Y * draft.CellSize;
            var y = baseHeight;
            var rise = draft.HeightStep;
            var sw = new Vector3(x, y, z);
            var se = new Vector3(x + draft.CellSize, y, z);
            var ne = new Vector3(x + draft.CellSize, y + rise, z + draft.CellSize);
            var nw = new Vector3(x, y + rise, z + draft.CellSize);
            RotateCardinal(ref sw, ref se, ref ne, ref nw, cell.Direction, x, z, draft.CellSize, y, rise);
            AddQuad(buffer, sw, se, ne, nw, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        }

        private static void AddStairs(MeshBuffer buffer, EditableMapDraftAsset draft, EditableMapCell cell, float baseHeight)
        {
            const int stepCount = 3;
            var stepDepth = draft.CellSize / stepCount;
            for (var i = 0; i < stepCount; i++)
            {
                var min = new Vector3(cell.X * draft.CellSize, baseHeight, cell.Y * draft.CellSize);
                var stepMin = min + new Vector3(0f, draft.HeightStep * i / stepCount, stepDepth * i);
                var stepMax = new Vector3(
                    min.x + draft.CellSize,
                    baseHeight + draft.HeightStep * (i + 1) / stepCount,
                    min.z + stepDepth * (i + 1));
                AddCube(buffer, RotateMin(stepMin, stepMax, cell.Direction, min, draft.CellSize).min, RotateMin(stepMin, stepMax, cell.Direction, min, draft.CellSize).max);
            }
        }

        private static (Vector3 min, Vector3 max) RotateMin(Vector3 stepMin, Vector3 stepMax, MapDirection direction, Vector3 cellMin, float cellSize)
        {
            if (direction == MapDirection.North)
            {
                return (stepMin, stepMax);
            }

            var center = cellMin + new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);
            var points = new[]
            {
                new Vector3(stepMin.x, stepMin.y, stepMin.z),
                new Vector3(stepMax.x, stepMin.y, stepMin.z),
                new Vector3(stepMax.x, stepMax.y, stepMax.z),
                new Vector3(stepMin.x, stepMax.y, stepMax.z)
            };

            var rotation = Quaternion.Euler(0f, DirectionToAngle(direction), 0f);
            var rotatedMin = new Vector3(float.MaxValue, stepMin.y, float.MaxValue);
            var rotatedMax = new Vector3(float.MinValue, stepMax.y, float.MinValue);
            foreach (var point in points)
            {
                var rotated = center + rotation * (point - center);
                rotatedMin.x = Mathf.Min(rotatedMin.x, rotated.x);
                rotatedMin.z = Mathf.Min(rotatedMin.z, rotated.z);
                rotatedMax.x = Mathf.Max(rotatedMax.x, rotated.x);
                rotatedMax.z = Mathf.Max(rotatedMax.z, rotated.z);
            }

            return (rotatedMin, rotatedMax);
        }

        private static void RotateCardinal(
            ref Vector3 sw,
            ref Vector3 se,
            ref Vector3 ne,
            ref Vector3 nw,
            MapDirection direction,
            float originX,
            float originZ,
            float cellSize,
            float baseHeight,
            float rise)
        {
            if (direction == MapDirection.North)
            {
                return;
            }

            var center = new Vector3(originX + cellSize * 0.5f, baseHeight + rise * 0.5f, originZ + cellSize * 0.5f);
            var rotation = Quaternion.Euler(0f, DirectionToAngle(direction), 0f);
            sw = center + rotation * (sw - center);
            se = center + rotation * (se - center);
            ne = center + rotation * (ne - center);
            nw = center + rotation * (nw - center);
        }

        private static float DirectionToAngle(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return 90f;
                case MapDirection.South:
                    return 180f;
                case MapDirection.West:
                    return 270f;
                default:
                    return 0f;
            }
        }

        private static void AddCube(MeshBuffer buffer, Vector3 min, Vector3 max)
        {
            var p000 = new Vector3(min.x, min.y, min.z);
            var p100 = new Vector3(max.x, min.y, min.z);
            var p110 = new Vector3(max.x, max.y, min.z);
            var p010 = new Vector3(min.x, max.y, min.z);
            var p001 = new Vector3(min.x, min.y, max.z);
            var p101 = new Vector3(max.x, min.y, max.z);
            var p111 = new Vector3(max.x, max.y, max.z);
            var p011 = new Vector3(min.x, max.y, max.z);

            AddQuad(buffer, p001, p101, p111, p011, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            AddQuad(buffer, p100, p000, p010, p110, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            AddQuad(buffer, p000, p001, p011, p010, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            AddQuad(buffer, p101, p100, p110, p111, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            AddQuad(buffer, p010, p011, p111, p110, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            AddQuad(buffer, p000, p100, p101, p001, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        }

        private static void AddQuad(
            MeshBuffer buffer,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC,
            Vector2 uvD)
        {
            var start = buffer.Vertices.Count;
            buffer.Vertices.Add(a);
            buffer.Vertices.Add(b);
            buffer.Vertices.Add(c);
            buffer.Vertices.Add(d);
            buffer.UVs.Add(uvA);
            buffer.UVs.Add(uvB);
            buffer.UVs.Add(uvC);
            buffer.UVs.Add(uvD);
            buffer.Triangles.Add(start + 0);
            buffer.Triangles.Add(start + 1);
            buffer.Triangles.Add(start + 2);
            buffer.Triangles.Add(start + 0);
            buffer.Triangles.Add(start + 2);
            buffer.Triangles.Add(start + 3);
        }

        private static void BuildObjectPreview(Transform parent, EditableMapDraftAsset draft)
        {
            foreach (var placement in draft.Objects ?? Array.Empty<EditableMapObjectPlacement>())
            {
                var instance = CreateObjectInstance(draft, placement);
                instance.name = string.IsNullOrWhiteSpace(placement.Id)
                    ? $"Object {placement.Kind}"
                    : $"Object {placement.Id}";
                instance.transform.SetParent(parent, false);
                var scale = ResolveObjectScale(draft, placement);
                instance.transform.position = new Vector3(
                    placement.X * draft.CellSize + draft.CellSize * 0.5f,
                    placement.Height * draft.HeightStep + scale.y * 0.5f + 0.05f,
                    placement.Y * draft.CellSize + draft.CellSize * 0.5f);
                instance.transform.rotation = Quaternion.Euler(0f, DirectionToAngle(placement.Direction), 0f);
                instance.transform.localScale = scale;
                ApplyObjectPreviewMaterial(draft, placement, instance);
                Undo.RegisterCreatedObjectUndo(instance, "Create Editable Map Preview Object");
            }
        }

        private static GameObject CreateObjectInstance(EditableMapDraftAsset draft, EditableMapObjectPlacement placement)
        {
            var entry = FindObjectPaletteEntry(draft, placement.PaletteObjectId);
            if (entry?.Prefab != null)
            {
                return PrefabUtility.InstantiatePrefab(entry.Prefab) as GameObject ?? UnityEngine.Object.Instantiate(entry.Prefab);
            }

            var primitiveType = ResolvePrimitiveType(placement);
            return GameObject.CreatePrimitive(primitiveType);
        }

        private static PrimitiveType ResolvePrimitiveType(EditableMapObjectPlacement placement)
        {
            if (IsDoor(placement))
            {
                return PrimitiveType.Cube;
            }

            switch (placement.Kind)
            {
                case RoomChunkObjectKind.Torch:
                case RoomChunkObjectKind.Barrel:
                    return PrimitiveType.Cylinder;
                case RoomChunkObjectKind.SpawnHint:
                    return PrimitiveType.Sphere;
                case RoomChunkObjectKind.Blocker:
                    return PrimitiveType.Cube;
                default:
                    return PrimitiveType.Cube;
            }
        }

        private static Vector3 ResolveObjectScale(EditableMapDraftAsset draft, EditableMapObjectPlacement placement)
        {
            var width = Mathf.Max(1, placement.Width) * draft.CellSize;
            var depth = Mathf.Max(1, placement.Depth) * draft.CellSize;
            if (IsDoor(placement))
            {
                var horizontal = placement.Direction == MapDirection.East || placement.Direction == MapDirection.West;
                return horizontal
                    ? new Vector3(width * 0.18f, draft.HeightStep * 1.2f, depth * 0.9f)
                    : new Vector3(width * 0.9f, draft.HeightStep * 1.2f, depth * 0.18f);
            }

            switch (placement.Kind)
            {
                case RoomChunkObjectKind.Chest:
                    return new Vector3(width * 0.65f, draft.HeightStep * 0.35f, depth * 0.45f);
                case RoomChunkObjectKind.Torch:
                    return new Vector3(width * 0.18f, draft.HeightStep * 0.9f, depth * 0.18f);
                case RoomChunkObjectKind.Barrel:
                    return new Vector3(width * 0.42f, draft.HeightStep * 0.65f, depth * 0.42f);
                case RoomChunkObjectKind.SpawnHint:
                    return new Vector3(width * 0.35f, draft.HeightStep * 0.35f, depth * 0.35f);
                case RoomChunkObjectKind.Blocker:
                    return new Vector3(width * 0.85f, draft.HeightStep * 0.55f, depth * 0.85f);
                default:
                    return new Vector3(width * 0.7f, draft.HeightStep * 0.5f, depth * 0.7f);
            }
        }

        private static bool IsDoor(EditableMapObjectPlacement placement)
        {
            return placement.PaletteObjectId == "door" || placement.RuntimeReferenceId == "door";
        }

        private static void ApplyObjectPreviewMaterial(EditableMapDraftAsset draft, EditableMapObjectPlacement placement, GameObject instance)
        {
            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            var entry = FindObjectPaletteEntry(draft, placement.PaletteObjectId);
            renderer.sharedMaterial = entry?.PreviewMaterial != null
                ? entry.PreviewMaterial
                : PreviewMaterialCache.ForObject(placement.Kind, placement.BlocksMovement);
        }

        private static MapObjectPaletteEntry FindObjectPaletteEntry(EditableMapDraftAsset draft, string paletteObjectId)
        {
            return draft?.ObjectPalette?.Objects?.FirstOrDefault(entry => entry != null && entry.Id == paletteObjectId);
        }

        private static string ResolveMaterialKey(string materialId, string fallback)
        {
            return string.IsNullOrWhiteSpace(materialId) ? fallback : materialId.Trim();
        }

        private sealed class MeshBuffer
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();
            public readonly List<Vector2> UVs = new List<Vector2>();
        }

        private static class PreviewMaterialCache
        {
            private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>(StringComparer.Ordinal);

            public static Material ForId(string id, EditableMapDraftAsset draft = null)
            {
                var paletteMaterial = draft?.TilePalette?.Tiles?.FirstOrDefault(entry => entry != null && entry.Id == id)?.EditorMaterial;
                if (paletteMaterial != null)
                {
                    return paletteMaterial;
                }

                if (!Cache.TryGetValue(id, out var material))
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                    {
                        name = $"Editable Map Preview {id}",
                        color = ColorForId(id)
                    };
                    Cache.Add(id, material);
                }

                return material;
            }

            public static Material ForObject(RoomChunkObjectKind kind, bool blocksMovement)
            {
                return ForId(blocksMovement ? $"{kind}_blocked" : kind.ToString());
            }

            private static Color ColorForId(string id)
            {
                var hash = string.IsNullOrWhiteSpace(id) ? 0 : id.GetHashCode();
                var hue = Mathf.Abs(hash % 1000) / 1000f;
                var saturation = 0.35f + Mathf.Abs((hash / 7) % 250) / 1000f;
                var value = 0.55f + Mathf.Abs((hash / 13) % 250) / 1000f;
                return Color.HSVToRGB(hue, Mathf.Clamp01(saturation), Mathf.Clamp01(value));
            }
        }
    }
}

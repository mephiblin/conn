using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    internal static class MapChunkCellPreviewBuilder
    {
        public static bool TryBuildRoom(
            MapGeneratorWorkspace workspace,
            PreviewRoom room,
            Transform root,
            string undoName,
            Material fallbackMaterial)
        {
            if (workspace == null || !workspace.UseCellPreviewWhenAvailable)
            {
                return false;
            }

            var chunk = workspace.UseTestCellPreviewGrid ? null : FindChunk(room.ChunkId);
            var cells = workspace.UseTestCellPreviewGrid
                ? TestCells()
                : ExtractCells(chunk);
            var objects = workspace.UseTestCellPreviewGrid
                ? TestObjects()
                : ExtractObjects(chunk);
            if (cells.Count == 0)
            {
                return false;
            }

            var roomRoot = new GameObject($"Room {room.Id} ({room.Role}) Cells");
            roomRoot.transform.SetParent(root, false);
            roomRoot.transform.position = workspace.PreviewRoomPosition(room);
            Undo.RegisterCreatedObjectUndo(roomRoot, undoName);

            var size = Mathf.Max(0.05f, workspace.PreviewCellSize);
            var wallHeight = Mathf.Max(size, workspace.PreviewWallHeight);
            var materials = new CellMaterialCache();
            var bounds = CalculateBounds(cells);
            var origin = new Vector3(
                -(bounds.x - 1) * size * 0.5f,
                0f,
                -(bounds.y - 1) * size * 0.5f);

            for (var i = 0; i < cells.Count; i++)
            {
                CreateCellPrimitive(cells[i], roomRoot.transform, origin, size, wallHeight, fallbackMaterial, materials, undoName);
            }

            for (var i = 0; i < objects.Count; i++)
            {
                CreateObjectPrimitive(objects[i], roomRoot.transform, origin, size, materials, undoName);
            }

            return true;
        }

        private static RoomChunkAsset FindChunk(string chunkId)
        {
            if (string.IsNullOrWhiteSpace(chunkId))
            {
                return null;
            }

            var guids = new List<string>();
            guids.AddRange(AssetDatabase.FindAssets("t:RoomChunkAsset"));
            guids.AddRange(AssetDatabase.FindAssets("t:LandmarkRoomAsset"));
            for (var i = 0; i < guids.Count; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var chunk = AssetDatabase.LoadAssetAtPath<RoomChunkAsset>(path);
                if (chunk != null && chunk.Id == chunkId)
                {
                    return chunk;
                }
            }

            return null;
        }

        private static List<CellPreview> ExtractCells(RoomChunkAsset chunk)
        {
            if (chunk == null || chunk.Cells == null || chunk.Cells.Length == 0)
            {
                return EmptyCells();
            }

            var cells = new List<CellPreview>();
            for (var i = 0; i < chunk.Cells.Length; i++)
            {
                var source = chunk.Cells[i];
                if (source == null)
                {
                    continue;
                }

                cells.Add(new CellPreview
                {
                    X = source.X,
                    Y = source.Y,
                    Kind = ToPreviewKind(source.Type),
                    Height = source.Height,
                    RotationQuarterTurns = DirectionToQuarterTurns(source.Direction)
                });
            }

            return cells;
        }

        private static List<ObjectPreview> ExtractObjects(RoomChunkAsset chunk)
        {
            if (chunk == null || chunk.Objects == null || chunk.Objects.Length == 0)
            {
                return EmptyObjects();
            }

            var objects = new List<ObjectPreview>();
            for (var i = 0; i < chunk.Objects.Length; i++)
            {
                var source = chunk.Objects[i];
                if (source == null)
                {
                    continue;
                }

                objects.Add(new ObjectPreview
                {
                    Id = source.Id,
                    Kind = source.Kind,
                    X = source.X,
                    Y = source.Y,
                    Height = source.Height,
                    Width = Mathf.Max(1, source.Width),
                    Depth = Mathf.Max(1, source.Depth),
                    RotationQuarterTurns = DirectionToQuarterTurns(source.Direction),
                    BlocksMovement = source.BlocksMovement
                });
            }

            return objects;
        }

        private static void CreateCellPrimitive(
            CellPreview cell,
            Transform parent,
            Vector3 origin,
            float size,
            float wallHeight,
            Material fallbackMaterial,
            CellMaterialCache materials,
            string undoName)
        {
            if (cell.Kind == CellKind.Gap)
            {
                CreateGapMarker(cell, parent, origin, size, materials.Gap, undoName);
                return;
            }

            if (cell.Kind == CellKind.Slope)
            {
                CreateSlopePrimitive(cell, parent, origin, size, fallbackMaterial, materials, undoName);
                return;
            }

            if (cell.Kind == CellKind.Stair)
            {
                CreateStairPrimitives(cell, parent, origin, size, materials.Stair, undoName);
                return;
            }

            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = $"Cell {cell.X},{cell.Y} {cell.Kind}";
            instance.transform.SetParent(parent, false);

            var local = origin + new Vector3(cell.X * size, 0f, cell.Y * size);
            switch (cell.Kind)
            {
                case CellKind.Wall:
                    var cellWallHeight = wallHeight + Mathf.Max(0, cell.Height) * size;
                    instance.transform.localPosition = local + Vector3.up * (cellWallHeight * 0.5f);
                    instance.transform.localScale = new Vector3(size, cellWallHeight, size);
                    break;
                default:
                    instance.transform.localPosition = local + Vector3.up * (Mathf.Max(0, cell.Height) * size);
                    instance.transform.localScale = new Vector3(size, Mathf.Max(0.03f, size * 0.12f), size);
                    break;
            }

            var renderer = instance.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = materials.ForKind(cell.Kind, fallbackMaterial);
            }

            Undo.RegisterCreatedObjectUndo(instance, undoName);
        }

        private static void CreateSlopePrimitive(
            CellPreview cell,
            Transform parent,
            Vector3 origin,
            float size,
            Material fallbackMaterial,
            CellMaterialCache materials,
            string undoName)
        {
            var instance = new GameObject($"Cell {cell.X},{cell.Y} Slope");
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = origin + new Vector3(cell.X * size, Mathf.Max(0, cell.Height) * size, cell.Y * size);
            instance.transform.localRotation = Quaternion.Euler(0f, cell.RotationQuarterTurns * 90f, 0f);

            var half = size * 0.5f;
            var rise = size;
            var vertices = new[]
            {
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(-half, rise, half),
                new Vector3(half, rise, half),
                new Vector3(-half, -size * 0.08f, -half),
                new Vector3(half, -size * 0.08f, -half)
            };
            var triangles = new[]
            {
                0, 2, 1,
                1, 2, 3,
                4, 0, 5,
                5, 0, 1
            };
            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            };

            var mesh = new Mesh
            {
                name = instance.name,
                vertices = vertices,
                triangles = triangles,
                uv = uvs
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            instance.AddComponent<MeshRenderer>().sharedMaterial = materials.ForKind(CellKind.Slope, fallbackMaterial);
            Undo.RegisterCreatedObjectUndo(instance, undoName);
        }

        private static void CreateStairPrimitives(CellPreview cell, Transform parent, Vector3 origin, float size, Material material, string undoName)
        {
            var baseLocal = origin + new Vector3(cell.X * size, Mathf.Max(0, cell.Height) * size, cell.Y * size);
            var rotation = Quaternion.Euler(0f, cell.RotationQuarterTurns * 90f, 0f);
            const int stepCount = 3;
            for (var i = 0; i < stepCount; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Cell {cell.X},{cell.Y} Stair Step {i + 1}";
                step.transform.SetParent(parent, false);

                var depth = size / stepCount;
                var height = size * (i + 1) / stepCount;
                var z = -size * 0.5f + depth * (i + 0.5f);
                step.transform.localPosition = baseLocal + rotation * new Vector3(0f, height * 0.5f, z);
                step.transform.localRotation = rotation;
                step.transform.localScale = new Vector3(size, height, depth);
                step.GetComponent<MeshRenderer>().sharedMaterial = material;
                Undo.RegisterCreatedObjectUndo(step, undoName);
            }
        }

        private static void CreateGapMarker(CellPreview cell, Transform parent, Vector3 origin, float size, Material material, string undoName)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = $"Cell {cell.X},{cell.Y} Gap";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = origin + new Vector3(cell.X * size, 0.01f, cell.Y * size);
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            marker.transform.localScale = Vector3.one * size * 0.72f;
            marker.GetComponent<MeshRenderer>().sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(marker, undoName);
        }

        private static void CreateObjectPrimitive(
            ObjectPreview placement,
            Transform parent,
            Vector3 origin,
            float size,
            CellMaterialCache materials,
            string undoName)
        {
            var primitive = placement.Kind == RoomChunkObjectKind.Torch ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var instance = GameObject.CreatePrimitive(primitive);
            instance.name = string.IsNullOrWhiteSpace(placement.Id)
                ? $"Object {placement.X},{placement.Y} {placement.Kind}"
                : $"Object {placement.Id} ({placement.Kind})";
            instance.transform.SetParent(parent, false);

            var width = Mathf.Max(1, placement.Width) * size;
            var depth = Mathf.Max(1, placement.Depth) * size;
            var baseHeight = Mathf.Max(0, placement.Height) * size;
            var objectHeight = ObjectHeight(placement.Kind, size);
            instance.transform.localPosition = origin
                + new Vector3(placement.X * size, baseHeight + objectHeight * 0.5f + size * 0.08f, placement.Y * size);
            instance.transform.localRotation = Quaternion.Euler(0f, placement.RotationQuarterTurns * 90f, 0f);
            instance.transform.localScale = ObjectScale(placement.Kind, width, objectHeight, depth, size);
            instance.GetComponent<MeshRenderer>().sharedMaterial = materials.ForObject(placement.Kind, placement.BlocksMovement);
            Undo.RegisterCreatedObjectUndo(instance, undoName);
        }

        private static float ObjectHeight(RoomChunkObjectKind kind, float size)
        {
            switch (kind)
            {
                case RoomChunkObjectKind.Torch:
                    return size * 0.9f;
                case RoomChunkObjectKind.Chest:
                    return size * 0.45f;
                case RoomChunkObjectKind.Barrel:
                    return size * 0.55f;
                case RoomChunkObjectKind.Blocker:
                    return size * 0.7f;
                default:
                    return size * 0.35f;
            }
        }

        private static Vector3 ObjectScale(RoomChunkObjectKind kind, float width, float height, float depth, float size)
        {
            switch (kind)
            {
                case RoomChunkObjectKind.Torch:
                    return new Vector3(size * 0.22f, height * 0.5f, size * 0.22f);
                case RoomChunkObjectKind.Barrel:
                    return new Vector3(width * 0.55f, height * 0.5f, depth * 0.55f);
                case RoomChunkObjectKind.Chest:
                    return new Vector3(width * 0.8f, height, depth * 0.55f);
                case RoomChunkObjectKind.SpawnHint:
                    return new Vector3(width * 0.42f, height, depth * 0.42f);
                default:
                    return new Vector3(width * 0.75f, height, depth * 0.75f);
            }
        }

        private static Vector2Int CalculateBounds(IReadOnlyList<CellPreview> cells)
        {
            var maxX = 0;
            var maxY = 0;
            for (var i = 0; i < cells.Count; i++)
            {
                maxX = Mathf.Max(maxX, cells[i].X);
                maxY = Mathf.Max(maxY, cells[i].Y);
            }

            return new Vector2Int(maxX + 1, maxY + 1);
        }

        private static List<CellPreview> TestCells()
        {
            return new List<CellPreview>
            {
                new CellPreview { X = 0, Y = 0, Kind = CellKind.Wall },
                new CellPreview { X = 1, Y = 0, Kind = CellKind.Wall },
                new CellPreview { X = 2, Y = 0, Kind = CellKind.Wall },
                new CellPreview { X = 3, Y = 0, Kind = CellKind.Wall },
                new CellPreview { X = 0, Y = 1, Kind = CellKind.Floor },
                new CellPreview { X = 1, Y = 1, Kind = CellKind.Slope, RotationQuarterTurns = 1 },
                new CellPreview { X = 2, Y = 1, Kind = CellKind.Stair, RotationQuarterTurns = 1 },
                new CellPreview { X = 3, Y = 1, Kind = CellKind.Floor, Height = 1 },
                new CellPreview { X = 0, Y = 2, Kind = CellKind.Floor },
                new CellPreview { X = 1, Y = 2, Kind = CellKind.Gap },
                new CellPreview { X = 2, Y = 2, Kind = CellKind.Floor },
                new CellPreview { X = 3, Y = 2, Kind = CellKind.Wall },
            };
        }

        private static List<ObjectPreview> TestObjects()
        {
            return new List<ObjectPreview>
            {
                new ObjectPreview { Id = "chest", X = 0, Y = 1, Kind = RoomChunkObjectKind.Chest, RotationQuarterTurns = 1 },
                new ObjectPreview { Id = "torch", X = 3, Y = 1, Kind = RoomChunkObjectKind.Torch },
                new ObjectPreview { Id = "barrel", X = 2, Y = 2, Kind = RoomChunkObjectKind.Barrel, BlocksMovement = true },
                new ObjectPreview { Id = "spawn_hint", X = 1, Y = 1, Kind = RoomChunkObjectKind.SpawnHint, Height = 1 }
            };
        }

        private static List<CellPreview> EmptyCells()
        {
            return new List<CellPreview>(0);
        }

        private static List<ObjectPreview> EmptyObjects()
        {
            return new List<ObjectPreview>(0);
        }

        private static CellKind ToPreviewKind(RoomChunkCellType type)
        {
            switch (type)
            {
                case RoomChunkCellType.Wall:
                    return CellKind.Wall;
                case RoomChunkCellType.Slope:
                    return CellKind.Slope;
                case RoomChunkCellType.Stair:
                    return CellKind.Stair;
                case RoomChunkCellType.Gap:
                    return CellKind.Gap;
                default:
                    return CellKind.Floor;
            }
        }

        private static int DirectionToQuarterTurns(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return 1;
                case MapDirection.South:
                    return 2;
                case MapDirection.West:
                    return 3;
                default:
                    return 0;
            }
        }

        private enum CellKind
        {
            Floor,
            Wall,
            Slope,
            Stair,
            Gap
        }

        private struct CellPreview
        {
            public int X;
            public int Y;
            public CellKind Kind;
            public int RotationQuarterTurns;
            public int Height;
        }

        private struct ObjectPreview
        {
            public string Id;
            public RoomChunkObjectKind Kind;
            public int X;
            public int Y;
            public int Height;
            public int Width;
            public int Depth;
            public int RotationQuarterTurns;
            public bool BlocksMovement;
        }

        private sealed class CellMaterialCache
        {
            public readonly Material Floor = CreateMaterial("Map Cell Preview Floor", new Color(0.36f, 0.40f, 0.42f));
            public readonly Material Wall = CreateMaterial("Map Cell Preview Wall", new Color(0.22f, 0.24f, 0.26f));
            public readonly Material Slope = CreateMaterial("Map Cell Preview Slope", new Color(0.38f, 0.55f, 0.78f));
            public readonly Material Stair = CreateMaterial("Map Cell Preview Stair", new Color(0.72f, 0.58f, 0.34f));
            public readonly Material Gap = CreateMaterial("Map Cell Preview Gap", new Color(0.08f, 0.08f, 0.09f, 0.7f));
            public readonly Material Decor = CreateMaterial("Map Object Preview Decor", new Color(0.52f, 0.48f, 0.42f));
            public readonly Material Chest = CreateMaterial("Map Object Preview Chest", new Color(0.72f, 0.46f, 0.24f));
            public readonly Material Barrel = CreateMaterial("Map Object Preview Barrel", new Color(0.48f, 0.32f, 0.18f));
            public readonly Material Torch = CreateMaterial("Map Object Preview Torch", new Color(1f, 0.48f, 0.12f));
            public readonly Material SpawnHint = CreateMaterial("Map Object Preview Spawn Hint", new Color(0.35f, 0.85f, 0.55f));
            public readonly Material Blocker = CreateMaterial("Map Object Preview Blocker", new Color(0.58f, 0.1f, 0.1f));

            public Material ForKind(CellKind kind, Material fallback)
            {
                switch (kind)
                {
                    case CellKind.Wall:
                        return Wall;
                    case CellKind.Slope:
                        return Slope;
                    case CellKind.Stair:
                        return Stair;
                    case CellKind.Gap:
                        return Gap;
                    default:
                        return fallback != null ? fallback : Floor;
                }
            }

            public Material ForObject(RoomChunkObjectKind kind, bool blocksMovement)
            {
                if (blocksMovement && kind == RoomChunkObjectKind.Decor)
                {
                    return Blocker;
                }

                switch (kind)
                {
                    case RoomChunkObjectKind.Chest:
                        return Chest;
                    case RoomChunkObjectKind.Barrel:
                        return Barrel;
                    case RoomChunkObjectKind.Torch:
                        return Torch;
                    case RoomChunkObjectKind.SpawnHint:
                        return SpawnHint;
                    case RoomChunkObjectKind.Blocker:
                        return Blocker;
                    default:
                        return Decor;
                }
            }

            private static Material CreateMaterial(string name, Color color)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = name,
                    color = color
                };
                return material;
            }
        }
    }
}

using Conn.Core.Maps;
using Conn.Runtime.Maps;
using UnityEngine;

namespace Conn.Runtime.World
{
    public static class DungeonMapActorSpawner
    {
        public const string RootName = "Compiled Dungeon Map";
        private const float SmallAuthoredUnit = 0.28f;
        private const float SmallAuthoredUnitCutoff = 0.75f;
        private const float DefaultRuntimeCellSize = 2.4f;
        private const float DefaultRuntimeHeightStep = 0.8f;

        public static int SpawnFromCompiledMap(CompiledMap compiledMap, Transform parent = null)
        {
            if (compiledMap == null)
            {
                return 0;
            }

            DisableLegacyDungeonGround();
            var root = parent != null ? parent : RecreateRoot();
            var spawned = 0;
            for (var i = 0; i < (compiledMap.Cells?.Count ?? 0); i++)
            {
                var cell = compiledMap.Cells[i];
                if (cell == null || cell.Terrain == RoomChunkCellType.Gap)
                {
                    continue;
                }

                CreateCellActor(root, compiledMap, cell);
                spawned++;
            }

            return spawned;
        }

        public static bool PlacePlayerAtStart(CompiledMap compiledMap)
        {
            if (compiledMap == null)
            {
                return false;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return false;
            }

            var position = InitialPlayerPosition(compiledMap);
            var rotation = InitialPlayerRotation(compiledMap);

            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(position, rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }

            return true;
        }

        public static Vector3 InitialPlayerPosition(CompiledMap compiledMap)
        {
            if (compiledMap == null)
            {
                return Vector3.up * 1.15f;
            }

            var start = TryFindPlacement(compiledMap, MapPlacementKind.Start);
            return start != null
                ? WorldPosition(compiledMap, start.X, start.Y, 1.15f)
                : FallbackPlayerPosition(compiledMap);
        }

        public static Quaternion InitialPlayerRotation(CompiledMap compiledMap)
        {
            if (compiledMap == null)
            {
                return Quaternion.identity;
            }

            var start = TryFindPlacement(compiledMap, MapPlacementKind.Start);
            var target = InitialFramingTarget(compiledMap);
            if (start == null || target == null)
            {
                return Quaternion.identity;
            }

            var startPosition = WorldPosition(compiledMap, start.X, start.Y, 0f);
            var targetPosition = WorldPosition(compiledMap, target.X, target.Y, 0f);
            var direction = targetPosition - startPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        public static MapPlacement InitialFramingTarget(CompiledMap compiledMap)
        {
            if (compiledMap == null)
            {
                return null;
            }

            return TryFindPlacement(compiledMap, MapPlacementKind.QuestTarget)
                ?? TryFindPlacement(compiledMap, MapPlacementKind.Boss)
                ?? TryFindPlacement(compiledMap, MapPlacementKind.Exit);
        }

        private static Transform RecreateRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(existing);
                }
                else
                {
                    Object.DestroyImmediate(existing);
                }
            }

            return new GameObject(RootName).transform;
        }

        private static void DisableLegacyDungeonGround()
        {
            var legacy = GameObject.Find("Dungeon Ground");
            if (legacy != null)
            {
                legacy.SetActive(false);
            }
        }

        private static void CreateCellActor(Transform root, CompiledMap compiledMap, CompiledMapCell cell)
        {
            if (cell.Terrain == RoomChunkCellType.Slope || cell.Terrain == RoomChunkCellType.Stair)
            {
                CreateHeightTransitionActor(root, compiledMap, cell);
                return;
            }

            var actor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            actor.name = $"Map Cell - {cell.Terrain} {cell.X},{cell.Y}";
            actor.transform.SetParent(root, false);
            actor.transform.position = WorldPosition(compiledMap, cell.X, cell.Y, CellCenterY(compiledMap, cell));
            actor.transform.localScale = CellScale(compiledMap, cell);

            var renderer = actor.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MaterialFor(compiledMap.ProfileId, cell);
            }

            var collider = actor.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        private static void CreateHeightTransitionActor(Transform root, CompiledMap compiledMap, CompiledMapCell cell)
        {
            var actor = new GameObject($"Map Cell - {cell.Terrain} {cell.X},{cell.Y}");
            actor.transform.SetParent(root, false);
            actor.transform.position = WorldPosition(compiledMap, cell.X, cell.Y, 0f);

            var mesh = BuildHeightTransitionMesh(compiledMap, cell);
            var meshFilter = actor.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = actor.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialFor(compiledMap.ProfileId, cell);

            var collider = actor.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        public static float WorldCellSize(CompiledMap compiledMap)
        {
            return NormalizeCellUnit(compiledMap != null ? compiledMap.CellSize : 1f);
        }

        public static float WorldHeightStep(CompiledMap compiledMap)
        {
            return NormalizeHeightUnit(compiledMap != null ? compiledMap.HeightStep : 1f);
        }

        public static Vector3 WorldPosition(CompiledMap compiledMap, int x, int y, float worldY)
        {
            var cellSize = WorldCellSize(compiledMap);
            var offsetX = compiledMap != null ? compiledMap.Width * cellSize * 0.5f : 0f;
            var offsetZ = compiledMap != null ? compiledMap.Height * cellSize * 0.5f : 0f;
            return new Vector3(
                x * cellSize + cellSize * 0.5f - offsetX,
                worldY,
                y * cellSize + cellSize * 0.5f - offsetZ);
        }

        private static Vector3 FallbackPlayerPosition(CompiledMap compiledMap)
        {
            for (var i = 0; i < (compiledMap.Cells?.Count ?? 0); i++)
            {
                var cell = compiledMap.Cells[i];
                if (cell != null && cell.Terrain == RoomChunkCellType.Floor)
                {
                    return WorldPosition(compiledMap, cell.X, cell.Y, 1.15f);
                }
            }

            return Vector3.up * 1.15f;
        }

        private static MapPlacement TryFindPlacement(CompiledMap compiledMap, MapPlacementKind kind)
        {
            for (var i = 0; i < (compiledMap?.Placements?.Count ?? 0); i++)
            {
                var placement = compiledMap.Placements[i];
                if (placement != null && placement.Kind == kind)
                {
                    return placement;
                }
            }

            return null;
        }

        private static float CellCenterY(CompiledMap compiledMap, CompiledMapCell cell)
        {
            var heightStep = WorldHeightStep(compiledMap);
            if (cell.Terrain == RoomChunkCellType.Wall)
            {
                return cell.Height * heightStep + heightStep;
            }

            return cell.Height * heightStep - 0.05f;
        }

        private static Vector3 CellScale(CompiledMap compiledMap, CompiledMapCell cell)
        {
            var cellSize = WorldCellSize(compiledMap);
            var heightStep = WorldHeightStep(compiledMap);
            if (cell.Terrain == RoomChunkCellType.Wall)
            {
                return new Vector3(cellSize, heightStep * 2f, cellSize);
            }

            return new Vector3(cellSize, 0.1f, cellSize);
        }

        private static Mesh BuildHeightTransitionMesh(CompiledMap compiledMap, CompiledMapCell cell)
        {
            var cellSize = WorldCellSize(compiledMap);
            var heightStep = WorldHeightStep(compiledMap);
            var half = cellSize * 0.5f;
            var low = cell.Height * heightStep;
            var high = low + heightStep;
            var thickness = Mathf.Max(0.05f, Mathf.Min(cellSize, heightStep) * 0.08f);

            var west = low;
            var east = low;
            var north = low;
            var south = low;
            switch (NormalizeDirection(cell.Direction))
            {
                case MapDirection.East:
                    east = high;
                    break;
                case MapDirection.West:
                    west = high;
                    break;
                case MapDirection.North:
                    north = high;
                    break;
                case MapDirection.South:
                    south = high;
                    break;
            }

            var topWestSouth = Mathf.Max(west, south);
            var topEastSouth = Mathf.Max(east, south);
            var topEastNorth = Mathf.Max(east, north);
            var topWestNorth = Mathf.Max(west, north);

            var vertices = new[]
            {
                new Vector3(-half, topWestSouth, -half),
                new Vector3(half, topEastSouth, -half),
                new Vector3(half, topEastNorth, half),
                new Vector3(-half, topWestNorth, half),
                new Vector3(-half, topWestSouth - thickness, -half),
                new Vector3(half, topEastSouth - thickness, -half),
                new Vector3(half, topEastNorth - thickness, half),
                new Vector3(-half, topWestNorth - thickness, half)
            };

            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };

            var mesh = new Mesh
            {
                name = $"Runtime {cell.Terrain} {cell.X},{cell.Y}"
            };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static MapDirection NormalizeDirection(MapDirection direction)
        {
            if (direction == MapDirection.North
                || direction == MapDirection.East
                || direction == MapDirection.South
                || direction == MapDirection.West)
            {
                return direction;
            }

            return MapDirection.East;
        }

        private static float NormalizeCellUnit(float authoredUnit)
        {
            var unit = Mathf.Max(0.1f, authoredUnit);
            if (unit < SmallAuthoredUnitCutoff)
            {
                return Mathf.Max(DefaultRuntimeCellSize, unit / SmallAuthoredUnit * DefaultRuntimeCellSize);
            }

            return Mathf.Max(DefaultRuntimeCellSize, unit);
        }

        private static float NormalizeHeightUnit(float authoredUnit)
        {
            var unit = Mathf.Max(0.1f, authoredUnit);
            if (unit < SmallAuthoredUnitCutoff)
            {
                return Mathf.Max(DefaultRuntimeHeightStep, unit / SmallAuthoredUnit * DefaultRuntimeHeightStep);
            }

            return Mathf.Max(DefaultRuntimeHeightStep, unit);
        }

        private static Material MaterialFor(string profileId, CompiledMapCell cell)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = ColorFor(profileId, cell)
            };
            material.name = $"Dungeon Map {profileId} {cell.Terrain}";
            return material;
        }

        private static Color ColorFor(string profileId, CompiledMapCell cell)
        {
            var twistedTemple = profileId == "twisted_temple";
            if (cell.Terrain == RoomChunkCellType.Wall)
            {
                return twistedTemple ? new Color(0.32f, 0.27f, 0.21f) : new Color(0.28f, 0.28f, 0.31f);
            }

            if (cell.Terrain == RoomChunkCellType.Stair || cell.Terrain == RoomChunkCellType.Slope)
            {
                return twistedTemple ? new Color(0.48f, 0.38f, 0.24f) : new Color(0.42f, 0.42f, 0.46f);
            }

            return twistedTemple ? new Color(0.58f, 0.46f, 0.28f) : new Color(0.36f, 0.36f, 0.38f);
        }
    }
}

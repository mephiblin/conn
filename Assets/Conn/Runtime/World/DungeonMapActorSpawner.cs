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

            var start = CompiledMapDungeonRuntimeService.FindStartAnchor(compiledMap);
            var position = start != null
                ? WorldPosition(compiledMap, start.X, start.Y, 1.15f)
                : FallbackPlayerPosition(compiledMap);

            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(position, Quaternion.identity);

            if (controller != null)
            {
                controller.enabled = true;
            }

            return true;
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
                collider.enabled = cell.Terrain != RoomChunkCellType.Slope;
            }
        }

        public static float WorldCellSize(CompiledMap compiledMap)
        {
            return NormalizeMapUnit(compiledMap != null ? compiledMap.CellSize : 1f);
        }

        public static float WorldHeightStep(CompiledMap compiledMap)
        {
            return NormalizeMapUnit(compiledMap != null ? compiledMap.HeightStep : 1f);
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

        private static float NormalizeMapUnit(float authoredUnit)
        {
            var unit = Mathf.Max(0.1f, authoredUnit);
            if (unit < SmallAuthoredUnitCutoff)
            {
                return Mathf.Max(1f, unit / SmallAuthoredUnit);
            }

            return unit;
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

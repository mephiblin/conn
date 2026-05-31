using Conn.Core.Maps;
using UnityEngine;

namespace Conn.Runtime.World
{
    public static class DungeonObjectActorSpawner
    {
        public const string RootName = "Compiled Dungeon Objects";

        public static int SpawnFromCompiledMap(CompiledMap compiledMap, Transform parent = null)
        {
            if (compiledMap == null)
            {
                return 0;
            }

            var root = parent != null ? parent : EnsureRoot();
            var spawned = 0;
            for (var i = 0; i < (compiledMap.Objects?.Count ?? 0); i++)
            {
                var placement = compiledMap.Objects[i];
                if (placement == null)
                {
                    continue;
                }

                CreateActor(root, compiledMap, placement);
                spawned++;
            }

            return spawned;
        }

        private static Transform EnsureRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                return existing.transform;
            }

            return new GameObject(RootName).transform;
        }

        private static void CreateActor(Transform root, CompiledMap compiledMap, CompiledMapObjectPlacement placement)
        {
            var primitive = PrimitiveFor(placement.Kind);
            var actor = GameObject.CreatePrimitive(primitive);
            actor.name = $"Dungeon Object - {placement.PlacementId}";
            actor.transform.SetParent(root, false);
            actor.transform.position = WorldPosition(compiledMap, placement);
            actor.transform.rotation = Quaternion.Euler(0f, DirectionToAngle(placement.Direction), 0f);
            actor.transform.localScale = ScaleFor(compiledMap, placement);

            var renderer = actor.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MaterialFor(placement.Kind, placement.BlocksMovement);
            }

            if (ShouldInteract(placement.Kind))
            {
                var interactable = actor.AddComponent<DungeonObjectInteractable>();
                interactable.Configure(placement);
            }
        }

        private static PrimitiveType PrimitiveFor(RoomChunkObjectKind kind)
        {
            switch (kind)
            {
                case RoomChunkObjectKind.Torch:
                    return PrimitiveType.Cylinder;
                case RoomChunkObjectKind.Barrel:
                    return PrimitiveType.Capsule;
                default:
                    return PrimitiveType.Cube;
            }
        }

        private static Vector3 ScaleFor(CompiledMap compiledMap, CompiledMapObjectPlacement placement)
        {
            var cellSize = DungeonMapActorSpawner.WorldCellSize(compiledMap);
            var height = DungeonMapActorSpawner.WorldHeightStep(compiledMap);
            switch (placement.Kind)
            {
                case RoomChunkObjectKind.Torch:
                    return new Vector3(cellSize * 0.18f, height * 0.5f, cellSize * 0.18f);
                case RoomChunkObjectKind.Barrel:
                    return new Vector3(cellSize * 0.45f, height * 0.45f, cellSize * 0.45f);
                case RoomChunkObjectKind.Chest:
                    return new Vector3(cellSize * 0.65f, height * 0.35f, cellSize * 0.45f);
                default:
                    return new Vector3(Mathf.Max(1, placement.Width) * cellSize * 0.7f, height * 0.5f, Mathf.Max(1, placement.Depth) * cellSize * 0.7f);
            }
        }

        private static Vector3 WorldPosition(CompiledMap compiledMap, CompiledMapObjectPlacement placement)
        {
            var heightStep = DungeonMapActorSpawner.WorldHeightStep(compiledMap);
            return DungeonMapActorSpawner.WorldPosition(
                compiledMap,
                placement.X,
                placement.Y,
                placement.Height * heightStep + heightStep * 0.5f);
        }

        private static bool ShouldInteract(RoomChunkObjectKind kind)
        {
            return kind == RoomChunkObjectKind.Chest
                || kind == RoomChunkObjectKind.Barrel
                || kind == RoomChunkObjectKind.Torch;
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

        private static Material MaterialFor(RoomChunkObjectKind kind, bool blocksMovement)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = ColorFor(kind, blocksMovement)
            };
            material.name = $"Dungeon Object {kind}";
            return material;
        }

        private static Color ColorFor(RoomChunkObjectKind kind, bool blocksMovement)
        {
            if (kind == RoomChunkObjectKind.Chest)
            {
                return new Color(0.72f, 0.46f, 0.24f);
            }

            if (kind == RoomChunkObjectKind.Barrel)
            {
                return new Color(0.48f, 0.32f, 0.18f);
            }

            if (kind == RoomChunkObjectKind.Torch)
            {
                return new Color(1f, 0.55f, 0.12f);
            }

            if (blocksMovement)
            {
                return new Color(0.58f, 0.1f, 0.1f);
            }

            return new Color(0.52f, 0.48f, 0.42f);
        }
    }
}

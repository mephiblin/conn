using Conn.Core.Maps;
using Conn.Runtime.Maps;
using UnityEngine;

namespace Conn.Runtime.World
{
    public static class DungeonVisualDebugOverlay
    {
        public const string RootName = "Dungeon Visual Debug";

        public static void SpawnForCompiledMap(CompiledMap compiledMap)
        {
            if (compiledMap == null)
            {
                return;
            }

            var root = RecreateRoot();
            SpawnMapSummary(root, compiledMap);
            SpawnBounds(root, compiledMap);
            for (var i = 0; i < (compiledMap.Placements?.Count ?? 0); i++)
            {
                var placement = compiledMap.Placements[i];
                if (placement == null)
                {
                    continue;
                }

                SpawnPlacementMarker(root, compiledMap, placement);
            }
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

        private static void SpawnMapSummary(Transform root, CompiledMap compiledMap)
        {
            var cellSize = DungeonMapActorSpawner.WorldCellSize(compiledMap);
            var position = DungeonMapActorSpawner.WorldPosition(compiledMap, 0, -1, 2.2f);
            CreateLabel(
                root,
                "Map Debug Summary",
                position,
                $"MAP {compiledMap.MapId}\nPROFILE {compiledMap.ProfileId}\nAUTH CELL {compiledMap.CellSize:0.##} -> WORLD {cellSize:0.##}\nSIZE {compiledMap.Width}x{compiledMap.Height}",
                Color.white,
                0.22f);
        }

        private static void SpawnBounds(Transform root, CompiledMap compiledMap)
        {
            var maxX = Mathf.Max(0, compiledMap.Width - 1);
            var maxY = Mathf.Max(0, compiledMap.Height - 1);
            SpawnBoundPost(root, compiledMap, 0, 0, "NW");
            SpawnBoundPost(root, compiledMap, maxX, 0, "NE");
            SpawnBoundPost(root, compiledMap, 0, maxY, "SW");
            SpawnBoundPost(root, compiledMap, maxX, maxY, "SE");
        }

        private static void SpawnBoundPost(Transform root, CompiledMap compiledMap, int x, int y, string label)
        {
            var position = DungeonMapActorSpawner.WorldPosition(compiledMap, x, y, 0.9f);
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = $"Debug Bound {label}";
            post.transform.SetParent(root, false);
            post.transform.position = position;
            post.transform.localScale = new Vector3(0.16f, 1.8f, 0.16f);
            DisableCollider(post);
            SetMaterial(post, new Color(0.1f, 0.7f, 1f, 0.92f));
        }

        private static void SpawnPlacementMarker(Transform root, CompiledMap compiledMap, MapPlacement placement)
        {
            var color = ColorFor(placement.Kind);
            var basePosition = DungeonMapActorSpawner.WorldPosition(compiledMap, placement.X, placement.Y, 0.12f);
            var marker = GameObject.CreatePrimitive(PrimitiveFor(placement.Kind));
            marker.name = $"Debug Marker - {placement.Kind} - {placement.Id}";
            marker.transform.SetParent(root, false);
            marker.transform.position = basePosition + Vector3.up * MarkerCenterY(placement.Kind);
            marker.transform.localScale = MarkerScale(placement.Kind);
            DisableCollider(marker);
            SetMaterial(marker, color);

            var label = $"{placement.Kind}\n{placement.Id}\n{placement.X},{placement.Y}";
            var labelPosition = DungeonMapActorSpawner.WorldPosition(compiledMap, placement.X, placement.Y, LabelY(placement.Kind));
            CreateLabel(root, $"Debug Label - {placement.Kind} - {placement.Id}", labelPosition, label, color, 0.18f);
        }

        private static PrimitiveType PrimitiveFor(MapPlacementKind kind)
        {
            return kind == MapPlacementKind.Start || kind == MapPlacementKind.Exit
                ? PrimitiveType.Cylinder
                : PrimitiveType.Sphere;
        }

        private static float MarkerCenterY(MapPlacementKind kind)
        {
            return kind == MapPlacementKind.QuestTarget || kind == MapPlacementKind.Boss ? 1.75f : 0.45f;
        }

        private static float LabelY(MapPlacementKind kind)
        {
            return kind == MapPlacementKind.QuestTarget || kind == MapPlacementKind.Boss ? 3.6f : 1.8f;
        }

        private static Vector3 MarkerScale(MapPlacementKind kind)
        {
            switch (kind)
            {
                case MapPlacementKind.Start:
                    return new Vector3(0.9f, 0.12f, 0.9f);
                case MapPlacementKind.Exit:
                    return new Vector3(0.8f, 0.16f, 0.8f);
                case MapPlacementKind.QuestTarget:
                    return new Vector3(0.9f, 1.6f, 0.9f);
                case MapPlacementKind.Boss:
                    return new Vector3(1.25f, 1.9f, 1.25f);
                case MapPlacementKind.Monster:
                    return new Vector3(0.65f, 0.65f, 0.65f);
                default:
                    return new Vector3(0.45f, 0.45f, 0.45f);
            }
        }

        private static Color ColorFor(MapPlacementKind kind)
        {
            switch (kind)
            {
                case MapPlacementKind.Start:
                    return new Color(0.15f, 1f, 0.25f, 0.95f);
                case MapPlacementKind.QuestTarget:
                    return new Color(1f, 0.15f, 0.85f, 0.95f);
                case MapPlacementKind.Boss:
                    return new Color(1f, 0.08f, 0.08f, 0.95f);
                case MapPlacementKind.Exit:
                    return new Color(0.05f, 0.9f, 1f, 0.95f);
                case MapPlacementKind.Monster:
                    return new Color(1f, 0.48f, 0.05f, 0.95f);
                case MapPlacementKind.Loot:
                    return new Color(1f, 0.9f, 0.12f, 0.95f);
                default:
                    return Color.white;
            }
        }

        private static void CreateLabel(Transform root, string name, Vector3 position, string text, Color color, float scale)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(root, false);
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            obj.transform.localScale = Vector3.one * scale;

            var mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 32;
            mesh.color = color;
        }

        private static void SetMaterial(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = color
            };
            material.name = $"{obj.name} Material";
            renderer.sharedMaterial = material;
        }

        private static void DisableCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }
}

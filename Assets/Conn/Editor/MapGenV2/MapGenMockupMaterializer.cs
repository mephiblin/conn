using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenMockupMaterializer
    {
        public static GameObject Materialize(MapGenMockupDraftAsset draft)
        {
            if (draft == null || draft.Profile == null || draft.Profile.StyleSet == null || draft.Profile.StyleSet.ModuleSet == null)
            {
                return null;
            }

            if (!draft.Accepted || !draft.IsAcceptedSignatureCurrent)
            {
                return null;
            }

            if (!MapGenPropPlacementValidator.Validate(draft.Width, draft.Height, draft.Cells).IsValid)
            {
                return null;
            }

            var moduleSet = draft.Profile.StyleSet.ModuleSet;
            var requests = MapGenMaterializationClassifier.Classify(draft.Width, draft.Height, draft.Cells);
            var root = new GameObject($"GeneratedMap_{draft.Profile.ProfileId}_{draft.Seed}");
            CreateGroup(root.transform, "MockupReference");
            var groups = new Dictionary<MapGenModuleCategory, Transform>();
            var rng = new MapGenRandom(draft.Seed).Fork("materialize");

            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                var entry = PickEntry(moduleSet.GetEntries(request.Category), ref rng);
                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                if (!groups.TryGetValue(request.Category, out var group))
                {
                    group = CreateGroup(root.transform, request.Category.ToString());
                    groups.Add(request.Category, group);
                }

                var instance = PrefabUtility.InstantiatePrefab(entry.Prefab) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Materialize MapGen Mockup");
                instance.transform.SetParent(group, false);
                instance.transform.position = ToWorld(draft, request.Coord) + entry.Offset;
                instance.transform.rotation = RotationFor(request.Direction, entry.RotationPolicy);
            }

            Undo.RegisterCreatedObjectUndo(root, "Materialize MapGen Mockup");
            return root;
        }

        private static Transform CreateGroup(Transform root, string name)
        {
            var group = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(group, "Create MapGen Materialization Group");
            group.transform.SetParent(root, false);
            return group.transform;
        }

        private static MapGenModuleEntry PickEntry(MapGenModuleEntry[] entries, ref MapGenRandom rng)
        {
            var total = 0;
            foreach (var entry in entries ?? System.Array.Empty<MapGenModuleEntry>())
            {
                if (entry != null && entry.Prefab != null && entry.Weight > 0)
                {
                    total += entry.Weight;
                }
            }

            if (total <= 0)
            {
                return null;
            }

            var roll = rng.NextInt(0, total);
            foreach (var entry in entries)
            {
                if (entry == null || entry.Prefab == null || entry.Weight <= 0)
                {
                    continue;
                }

                if (roll < entry.Weight)
                {
                    return entry;
                }

                roll -= entry.Weight;
            }

            return null;
        }

        private static Vector3 ToWorld(MapGenMockupDraftAsset draft, MapGenGridCoord coord)
        {
            var cellSize = draft.Profile != null ? draft.Profile.CellSize : 1f;
            return new Vector3((coord.X + 0.5f) * cellSize, 0f, (coord.Y + 0.5f) * cellSize);
        }

        private static Quaternion RotationFor(MapGenGridDirection direction, MapGenModuleRotationPolicy policy)
        {
            if (policy == MapGenModuleRotationPolicy.None)
            {
                return Quaternion.identity;
            }

            var y = direction switch
            {
                MapGenGridDirection.East => 90f,
                MapGenGridDirection.South => 180f,
                MapGenGridDirection.West => 270f,
                _ => 0f
            };
            return Quaternion.Euler(0f, y, 0f);
        }
    }
}

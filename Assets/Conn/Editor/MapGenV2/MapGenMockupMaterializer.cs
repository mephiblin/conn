using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
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
            var root = new GameObject($"MapGenV2_{draft.Profile.ProfileId}_{draft.Seed}");
            Undo.RegisterCreatedObjectUndo(root, "Materialize MapGen Mockup");
            var marker = Undo.AddComponent<MapGenV2GeneratedMapMarker>(root);
            marker.PopulateFromDraft(draft, DateTime.UtcNow.ToString("O"));

            var groups = new Dictionary<MapGenModuleCategory, Transform>();
            CreateStandardGroups(root, groups);
            var rng = new MapGenRandom(draft.Seed).Fork("materialize");

            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                var entry = PickEntry(moduleSet.GetEntries(request.Category), ref rng);
                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                var group = GetOrCreateGroup(root, groups, request.Category);

                var instance = PrefabUtility.InstantiatePrefab(entry.Prefab) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Materialize MapGen Mockup");
                instance.name = BuildInstanceName(draft, request, entry.Prefab.name);
                instance.transform.SetParent(group, false);
                instance.transform.position = ToWorld(draft, request.Coord) + entry.Offset;
                instance.transform.rotation = RotationFor(request.Direction, entry.RotationPolicy);
            }

            EditorUtility.SetDirty(marker);
            return root;
        }

        private static void CreateStandardGroups(GameObject root, Dictionary<MapGenModuleCategory, Transform> groups)
        {
            var floors = CreateGroup(root.transform, "Floors");
            var corridors = CreateGroup(root.transform, "Corridors");
            var walls = CreateGroup(root.transform, "Walls");
            var ceilings = CreateGroup(root.transform, "Ceilings");
            var doors = CreateGroup(root.transform, "Doors");
            var props = CreateGroup(root.transform, "Props");
            CreateGroup(root.transform, "Navigation");
            CreateGroup(root.transform, "Debug");

            groups[MapGenModuleCategory.FloorA] = floors;
            groups[MapGenModuleCategory.FloorB] = corridors;
            groups[MapGenModuleCategory.WallStraight] = walls;
            groups[MapGenModuleCategory.WallCornerInside] = walls;
            groups[MapGenModuleCategory.WallCornerOutside] = walls;
            groups[MapGenModuleCategory.CeilingInterior] = ceilings;
            groups[MapGenModuleCategory.CeilingExterior] = ceilings;
            groups[MapGenModuleCategory.DoorWhole] = doors;
            groups[MapGenModuleCategory.DoorFrameHalf] = doors;
            groups[MapGenModuleCategory.DoorPanelHalf] = doors;
            groups[MapGenModuleCategory.Prop] = props;
        }

        private static Transform GetOrCreateGroup(
            GameObject root,
            Dictionary<MapGenModuleCategory, Transform> groups,
            MapGenModuleCategory category)
        {
            if (groups.TryGetValue(category, out var group))
            {
                return group;
            }

            group = CreateGroup(root.transform, category.ToString());
            groups.Add(category, group);
            return group;
        }

        private static Transform CreateGroup(Transform root, string name)
        {
            var group = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(group, "Create MapGen Materialization Group");
            group.transform.SetParent(root, false);
            return group.transform;
        }

        private static string BuildInstanceName(
            MapGenMockupDraftAsset draft,
            MapGenModuleRequest request,
            string moduleId)
        {
            var regionId = -1;
            var index = request.Coord.ToIndex(draft.Width);
            if (index >= 0 && index < (draft.Cells?.Length ?? 0))
            {
                regionId = draft.Cells[index].RegionId;
            }

            return $"{request.Category}_{request.Coord.X}_{request.Coord.Y}_R{regionId}_{moduleId}";
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

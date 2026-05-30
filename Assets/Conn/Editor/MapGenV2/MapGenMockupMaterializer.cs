using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Conn.MapGenV2.Editor
{
    public enum MapGenV2SceneOutputMode
    {
        CreateNewRoot,
        ReplacePreviousRoot,
        UpdateSelectedRoot
    }

    public static class MapGenMockupMaterializer
    {
        public static GameObject Materialize(
            MapGenMockupDraftAsset draft,
            MapGenV2SceneOutputMode outputMode = MapGenV2SceneOutputMode.CreateNewRoot,
            GameObject selectedRoot = null)
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
            var plan = BuildPlan(draft);
            var report = BuildReport(moduleSet, plan, draft.Seed);
            if (!ValidateCoverage(report).IsValid)
            {
                return null;
            }

            var targetRoot = ResolveTargetRoot(draft, outputMode, selectedRoot);
            if (targetRoot != null)
            {
                ClearChildren(targetRoot);
            }

            var root = new GameObject($"MapGenV2_{draft.Profile.ProfileId}_{draft.Seed}");
            if (targetRoot != null)
            {
                root = targetRoot;
                root.name = $"MapGenV2_{draft.Profile.ProfileId}_{draft.Seed}";
            }
            else
            {
                Undo.RegisterCreatedObjectUndo(root, "Materialize MapGen Mockup");
            }

            var marker = Undo.AddComponent<MapGenV2GeneratedMapMarker>(root);
            marker.PopulateFromDraft(draft, DateTime.UtcNow.ToString("O"));
            marker.PopulateMaterializationSummary(report);

            var groups = new Dictionary<MapGenModuleCategory, Transform>();
            CreateStandardGroups(root, groups);
            var rng = new MapGenRandom(draft.Seed).Fork("materialize");

            for (var i = 0; i < plan.RequestCount; i++)
            {
                var request = plan.Requests[i];
                if (request.Category == MapGenModuleCategory.NavigationHelper)
                {
                    var navigationGroup = GetOrCreateGroup(root, groups, request.Category);
                    var helper = new GameObject(BuildInstanceName(draft, request, "NavigationHelper"));
                    Undo.RegisterCreatedObjectUndo(helper, "Materialize MapGen Navigation Helper");
                    helper.transform.SetParent(navigationGroup, false);
                    helper.transform.position = ToWorld(draft, request.Coord);
                    AttachSourceMarker(helper, draft, request, "NavigationHelper");
                    continue;
                }

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
                AttachSourceMarker(instance, draft, request, entry.Prefab.name);
            }

            EditorUtility.SetDirty(marker);
            return root;
        }

        public static MapGenMaterializationPlan BuildPlan(MapGenMockupDraftAsset draft)
        {
            if (draft == null || draft.Profile == null)
            {
                return new MapGenMaterializationPlan();
            }

            MapGenGridCoord[] propCoords = null;
            var propRules = draft.Profile.LayoutRules != null ? draft.Profile.LayoutRules.PropPlacementRules : null;
            if (propRules != null && propRules.Length > 0)
            {
                var propPlacement = MapGenPropPlacementPlanner.BuildForDraft(draft);
                propCoords = new MapGenGridCoord[propPlacement.PlacedProps.Length];
                for (var i = 0; i < propPlacement.PlacedProps.Length; i++)
                {
                    propCoords[i] = propPlacement.PlacedProps[i].Coord;
                }
            }

            return MapGenMaterializationPlanner.Build(
                draft.Width,
                draft.Height,
                draft.Profile.CellSize,
                draft.AcceptedSignature,
                draft.Cells,
                propCoords);
        }

        public static MapGenMaterializationReport BuildReport(
            MapGenModuleSetAsset moduleSet,
            MapGenMaterializationPlan plan,
            int seed = 0)
        {
            var report = new MapGenMaterializationReport
            {
                TotalRequests = plan != null ? plan.RequestCount : 0
            };

            var missingCategories = new List<string>();
            var selectedPrefabNames = new List<string>();
            var occupied = new HashSet<string>();
            var rng = new MapGenRandom(seed).Fork("materialize");
            foreach (var request in plan?.Requests ?? Array.Empty<MapGenModuleRequest>())
            {
                if (request.Category == MapGenModuleCategory.NavigationHelper)
                {
                    report.InstantiableRequests++;
                    selectedPrefabNames.Add("NavigationHelper");
                    continue;
                }

                var entry = PickEntry(moduleSet != null ? moduleSet.GetEntries(request.Category) : null, ref rng);
                if (entry != null && entry.Prefab != null)
                {
                    report.InstantiableRequests++;
                    selectedPrefabNames.Add(entry.Prefab.name);
                    AddFootprintIssues(report, occupied, plan, request, entry);
                    continue;
                }

                report.MissingModuleRequests++;
                var category = request.Category.ToString();
                if (!missingCategories.Contains(category))
                {
                    missingCategories.Add(category);
                }
            }

            report.MissingModuleCategories = missingCategories.ToArray();
            report.SelectedPrefabNames = selectedPrefabNames.ToArray();
            return report;
        }

        public static MapGenValidationReport ValidateCoverage(MapGenMaterializationReport report)
        {
            var validation = new MapGenValidationReport();
            if (report == null)
            {
                validation.Add(new MapGenIssue(
                    MapGenGenerationPhase.Materialize,
                    "materialization_report_missing",
                    "Materialization coverage report is missing.",
                    "Rebuild the materialization plan before instantiating modules.",
                    severity: MapGenIssueSeverity.Fatal));
                return validation;
            }

            foreach (var category in report.MissingModuleCategories ?? Array.Empty<string>())
            {
                validation.Add(new MapGenIssue(
                    MapGenGenerationPhase.Materialize,
                    "materialization_missing_module_category",
                    $"Materialization has no prefab coverage for {category}.",
                    $"Add at least one valid prefab to module category {category}.",
                    contextPath: $"ModuleSet.{category}"));
            }

            if (report.FootprintOutOfBoundsRequests > 0)
            {
                validation.Add(new MapGenIssue(
                    MapGenGenerationPhase.Materialize,
                    "materialization_footprint_out_of_bounds",
                    $"{report.FootprintOutOfBoundsRequests} materialization requests have prefab footprints outside the map bounds.",
                    "Use one-cell modules for edge-sensitive categories or adjust prefab footprints.",
                    severity: MapGenIssueSeverity.Warning));
            }

            if (report.FootprintOverlapRequests > 0)
            {
                validation.Add(new MapGenIssue(
                    MapGenGenerationPhase.Materialize,
                    "materialization_footprint_overlap",
                    $"{report.FootprintOverlapRequests} materialization requests overlap existing prefab footprints.",
                    "Reduce prefab footprints or split large modules into category-specific one-cell pieces.",
                    severity: MapGenIssueSeverity.Warning));
            }

            return validation;
        }

        public static MapGenV2GeneratedMapMarker FindExistingMarker(MapGenMockupDraftAsset draft)
        {
            foreach (var marker in Resources.FindObjectsOfTypeAll<MapGenV2GeneratedMapMarker>())
            {
                if (marker == null || marker.gameObject == null)
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(marker) || !marker.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (draft != null && marker.SourceDraft == draft)
                {
                    return marker;
                }

                if (draft != null
                    && draft.Profile != null
                    && marker.ProfileId == draft.Profile.ProfileId
                    && marker.Seed == draft.Seed
                    && marker.DraftSignature == draft.AcceptedSignature)
                {
                    return marker;
                }
            }

            return null;
        }

        public static void ClearRoot(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(root);
        }

        private static GameObject ResolveTargetRoot(
            MapGenMockupDraftAsset draft,
            MapGenV2SceneOutputMode outputMode,
            GameObject selectedRoot)
        {
            switch (outputMode)
            {
                case MapGenV2SceneOutputMode.ReplacePreviousRoot:
                    var previous = FindExistingMarker(draft);
                    if (previous != null)
                    {
                        ClearRoot(previous.gameObject);
                    }

                    return null;
                case MapGenV2SceneOutputMode.UpdateSelectedRoot:
                    return IsSceneRoot(selectedRoot) ? selectedRoot : null;
                default:
                    return null;
            }
        }

        private static bool IsSceneRoot(GameObject root)
        {
            return root != null && root.scene.IsValid() && !EditorUtility.IsPersistent(root);
        }

        private static void ClearChildren(GameObject root)
        {
            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);
            }

            foreach (var marker in root.GetComponents<MapGenV2GeneratedMapMarker>())
            {
                Undo.DestroyObjectImmediate(marker);
            }
        }

        private static void CreateStandardGroups(GameObject root, Dictionary<MapGenModuleCategory, Transform> groups)
        {
            var floors = CreateGroup(root.transform, "Floors");
            var corridors = CreateGroup(root.transform, "Corridors");
            var walls = CreateGroup(root.transform, "Walls");
            var ceilings = CreateGroup(root.transform, "Ceilings");
            var doors = CreateGroup(root.transform, "Doors");
            var props = CreateGroup(root.transform, "Props");
            var navigation = CreateGroup(root.transform, "Navigation");
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
            groups[MapGenModuleCategory.NavigationHelper] = navigation;
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
            return $"{request.Category}_{request.Coord.X}_{request.Coord.Y}_R{request.RegionId}_{moduleId}";
        }

        private static void AttachSourceMarker(
            GameObject instance,
            MapGenMockupDraftAsset draft,
            MapGenModuleRequest request,
            string prefabName)
        {
            var marker = Undo.AddComponent<MapGenV2MaterializedModuleMarker>(instance);
            marker.DraftSignature = draft != null ? draft.AcceptedSignature : string.Empty;
            marker.RegionId = request.RegionId;
            marker.SourceTemplateId = request.SourceTemplateId ?? string.Empty;
            marker.ModuleCategory = request.Category;
            marker.Direction = request.Direction;
            marker.PrefabName = prefabName ?? string.Empty;
            marker.CellCoord = new Vector2Int(request.Coord.X, request.Coord.Y);
            EditorUtility.SetDirty(marker);
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

        private static void AddFootprintIssues(
            MapGenMaterializationReport report,
            HashSet<string> occupied,
            MapGenMaterializationPlan plan,
            MapGenModuleRequest request,
            MapGenModuleEntry entry)
        {
            var footprint = entry != null ? entry.Footprint : Vector2Int.one;
            footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            for (var y = 0; y < footprint.y; y++)
            {
                for (var x = 0; x < footprint.x; x++)
                {
                    var coord = new MapGenGridCoord(request.Coord.X + x, request.Coord.Y + y);
                    if (!coord.IsInBounds(plan.Width, plan.Height))
                    {
                        report.FootprintOutOfBoundsRequests++;
                        continue;
                    }

                    var key = $"{request.Category}:{coord.X}:{coord.Y}";
                    if (!occupied.Add(key))
                    {
                        report.FootprintOverlapRequests++;
                    }
                }
            }
        }

        private static Vector3 ToWorld(MapGenMockupDraftAsset draft, MapGenGridCoord coord)
        {
            var cellSize = draft.Profile != null ? draft.Profile.CellSize : 1f;
            return new Vector3((coord.X + 0.5f) * cellSize, 0f, (coord.Y + 0.5f) * cellSize);
        }

        private static Quaternion RotationFor(MapGenGridDirection direction, MapGenModuleRotationPolicy policy)
        {
            switch (policy)
            {
                case MapGenModuleRotationPolicy.Rotate90:
                    return Quaternion.Euler(0f, 90f, 0f);
                case MapGenModuleRotationPolicy.Rotate180:
                    return Quaternion.Euler(0f, 180f, 0f);
                case MapGenModuleRotationPolicy.Rotate270:
                    return Quaternion.Euler(0f, 270f, 0f);
                case MapGenModuleRotationPolicy.AnyOrthogonal:
                    var y = direction switch
                    {
                        MapGenGridDirection.East => 90f,
                        MapGenGridDirection.South => 180f,
                        MapGenGridDirection.West => 270f,
                        _ => 0f
                    };
                    return Quaternion.Euler(0f, y, 0f);
                default:
                    return Quaternion.identity;
            }
        }
    }
}

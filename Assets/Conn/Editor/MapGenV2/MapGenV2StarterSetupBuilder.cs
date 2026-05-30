using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public sealed class MapGenV2StarterSetup
    {
        public MapGenProfileAsset Profile;
        public MapGenMockupDraftAsset Draft;
    }

    public static class MapGenV2StarterSetupBuilder
    {
        private const string Root = "Assets/Conn/Authoring/MapGenV2";

        [MenuItem("Conn/MapGenV2/Create Starter Profile Setup")]
        public static void CreateStarterProfileSetupFromMenu()
        {
            var setup = CreateStarterProfileSetup();
            Selection.activeObject = setup.Draft != null ? setup.Draft : setup.Profile;
            MapGenV2Window.Open(setup.Profile, setup.Draft);
        }

        [MenuItem("Conn/MapGenV2/Cleanup Starter Generated Assets")]
        public static void CleanupStarterGeneratedAssetsFromMenu()
        {
            var deleted = CleanupStarterGeneratedAssets();
            Debug.Log($"MapGenV2 starter cleanup deleted {deleted} generated assets.");
        }

        public static MapGenV2StarterSetup CreateStarterProfileSetup()
        {
            return CreateStarterProfileSetup(Root);
        }

        public static MapGenV2StarterSetup CreateStarterProfileSetup(string root)
        {
            EnsureStarterFolders(root);

            var floorPrefab = CreatePrimitivePrefab(root, "StarterFloor", PrimitiveType.Cube, new Color(0.78f, 0.16f, 0.12f), new Vector3(0.92f, 0.08f, 0.92f), new Vector3(0f, 0.04f, 0f));
            var corridorPrefab = CreatePrimitivePrefab(root, "StarterCorridorFloor", PrimitiveType.Cube, new Color(0.05f, 0.05f, 0.05f), new Vector3(0.82f, 0.06f, 0.82f), new Vector3(0f, 0.07f, 0f));
            var wallPrefab = CreatePrimitivePrefab(root, "StarterWall", PrimitiveType.Cube, new Color(0.2f, 0.32f, 0.85f), new Vector3(0.16f, 1.25f, 0.92f), new Vector3(0f, 0.625f, 0f));
            var cornerPrefab = CreatePrimitivePrefab(root, "StarterCorner", PrimitiveType.Cube, new Color(0.12f, 0.22f, 0.58f), new Vector3(0.24f, 1.35f, 0.24f), new Vector3(0f, 0.675f, 0f));
            var ceilingPrefab = CreatePrimitivePrefab(root, "StarterCeiling", PrimitiveType.Cube, new Color(0.85f, 0.86f, 0.82f), new Vector3(0.92f, 0.05f, 0.92f), new Vector3(0f, 1.42f, 0f));
            var doorPrefab = CreatePrimitivePrefab(root, "StarterDoor", PrimitiveType.Cube, new Color(0.95f, 0.66f, 0.18f), new Vector3(0.56f, 1f, 0.12f), new Vector3(0f, 0.5f, 0f));
            var propPrefab = CreatePrimitivePrefab(root, "StarterProp", PrimitiveType.Sphere, new Color(0.2f, 0.72f, 0.42f), new Vector3(0.32f, 0.32f, 0.32f), new Vector3(0f, 0.2f, 0f));

            var moduleSet = ScriptableObjectUtility.CreateAsset<MapGenModuleSetAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/ModuleSets/StarterModuleSet.asset"));
            moduleSet.ModuleSetId = "starter_module_set";
            moduleSet.FloorsA = Entries(floorPrefab);
            moduleSet.FloorsB = Entries(corridorPrefab);
            moduleSet.WallsStraight = Entries(wallPrefab);
            moduleSet.WallsCornerInside = Entries(cornerPrefab);
            moduleSet.WallsCornerOutside = Entries(cornerPrefab);
            moduleSet.InteriorCeilings = Entries(ceilingPrefab);
            moduleSet.ExteriorCeilings = Entries(ceilingPrefab);
            moduleSet.WholeDoors = Entries(doorPrefab);
            moduleSet.HalfDoorFrames = Entries(doorPrefab);
            moduleSet.HalfDoorPanels = Entries(doorPrefab);
            moduleSet.PropCategories = Entries(propPrefab);

            var styleSet = ScriptableObjectUtility.CreateAsset<MapGenStyleSetAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/StyleSets/StarterStyleSet.asset"));
            styleSet.StyleId = "starter_style";
            styleSet.ModuleSet = moduleSet;

            var ruleSet = ScriptableObjectUtility.CreateAsset<MapGenRuleSetAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/RuleSets/StarterRuleSet.asset"));
            ruleSet.RequiredRoomCategories = new[]
            {
                MapGenRoomCategory.Start,
                MapGenRoomCategory.Quest,
                MapGenRoomCategory.Boss,
                MapGenRoomCategory.Exit
            };
            ruleSet.UseDirectRoutes = true;
            ruleSet.ReduceDeadEnds = true;
            ruleSet.QuantityRules = MapGenQuantityRules.Defaults();
            ruleSet.PostProcessRules = MapGenPostProcessRules.Defaults();
            ruleSet.PostProcessRules.UseDirectRoutes = true;
            ruleSet.PostProcessRules.ReduceDeadEnds = true;

            var roomShape = ScriptableObjectUtility.CreateAsset<MapGenRoomShapeAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/RoomShapes/StarterRoomShape.asset"));
            PopulateStarterRoomShape(roomShape);

            var roomTemplates = new[]
            {
                CreateStarterRoomTemplate(root, roomShape, "StarterRoomTemplate_Start", "starter_room_template_start", MapGenRoomCategory.Start),
                CreateStarterRoomTemplate(root, roomShape, "StarterRoomTemplate_Quest", "starter_room_template_quest", MapGenRoomCategory.Quest),
                CreateStarterRoomTemplate(root, roomShape, "StarterRoomTemplate_Boss", "starter_room_template_boss", MapGenRoomCategory.Boss),
                CreateStarterRoomTemplate(root, roomShape, "StarterRoomTemplate_Exit", "starter_room_template_exit", MapGenRoomCategory.Exit)
            };

            var corridorTemplate = ScriptableObjectUtility.CreateAsset<MapGenCorridorTemplateAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/Templates/StarterCorridorTemplate.asset"));
            PopulateStarterCorridorTemplate(corridorTemplate);

            styleSet.RoomTemplates = roomTemplates;
            styleSet.CorridorTemplates = new[] { corridorTemplate };

            var profile = ScriptableObjectUtility.CreateAsset<MapGenProfileAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/Profiles/StarterProfile.asset"));
            profile.ProfileId = "starter_profile";
            profile.DisplayName = "Starter Profile";
            profile.AuthoringNotes =
                "Starter MapGenV2 profile for learning the production workflow.\n"
                + "- Uses generated placeholder prefabs only; replace the module set with real project prefabs for production.\n"
                + "- Red floor modules are rooms, black floor modules are corridors, blue blocks are walls/corners, pale slabs are ceilings, yellow blocks are doors, and green spheres are prop markers.\n"
                + "- Generate Mockup is editor-only. Accept the mockup before Materialize To Scene or Bake Runtime Asset.";
            profile.MapSize = new Vector2Int(10, 8);
            profile.Seed = 2001;
            profile.StyleSet = styleSet;
            profile.LayoutRules = ruleSet;
            profile.RoomShapes = new[] { roomShape };
            profile.OutputSettings = MapGenOutputSettings.Defaults();
            if (root != Root)
            {
                profile.OutputSettings.DraftFolder = $"{root}/Drafts";
                profile.OutputSettings.MaterializedPrefabFolder = $"{root}/MaterializedPrefabs";
                profile.OutputSettings.BakedAssetFolder = $"{root}/BakedMaps";
            }

            profile.NavigationSettings = MapGenNavigationAdapterSettings.Defaults();

            var draft = ScriptableObjectUtility.CreateAsset<MapGenMockupDraftAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/Drafts/StarterDraft.asset"));
            draft.Profile = profile;
            draft.Seed = profile.Seed;

            MarkDirty(moduleSet, styleSet, ruleSet, roomShape, corridorTemplate, profile, draft);
            MarkDirty(roomTemplates);
            AssetDatabase.SaveAssets();
            MapGenV2AssetDatabasePolicy.RefreshAfterBulkAssetChanges();

            return new MapGenV2StarterSetup
            {
                Profile = profile,
                Draft = draft
            };
        }

        public static int CleanupStarterGeneratedAssets()
        {
            return CleanupGeneratedAssets(Root);
        }

        public static int CleanupGeneratedAssets(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !AssetDatabase.IsValidFolder(root))
            {
                return 0;
            }

            var paths = new System.Collections.Generic.HashSet<string>();
            AddMatchingAssets(paths, root, "t:MapGenProfileAsset", asset =>
            {
                var profile = asset as MapGenProfileAsset;
                return profile != null && profile.ProfileId == "starter_profile";
            });
            AddMatchingAssets(paths, root, "t:MapGenMockupDraftAsset", asset =>
            {
                var draft = asset as MapGenMockupDraftAsset;
                return draft != null
                    && (draft.Profile != null && draft.Profile.ProfileId == "starter_profile"
                        || asset.name.StartsWith("StarterDraft", System.StringComparison.Ordinal));
            });
            AddMatchingAssets(paths, root, "t:MapGenStyleSetAsset", asset =>
            {
                var styleSet = asset as MapGenStyleSetAsset;
                return styleSet != null && styleSet.StyleId == "starter_style";
            });
            AddMatchingAssets(paths, root, "t:MapGenModuleSetAsset", asset =>
            {
                var moduleSet = asset as MapGenModuleSetAsset;
                return moduleSet != null && moduleSet.ModuleSetId == "starter_module_set";
            });
            AddMatchingAssets(paths, root, "t:MapGenRoomShapeAsset", asset =>
            {
                var shape = asset as MapGenRoomShapeAsset;
                return shape != null && shape.ShapeId == "starter_room_shape";
            });
            AddMatchingAssets(paths, root, "t:MapGenRoomTemplateAsset", asset =>
            {
                var template = asset as MapGenRoomTemplateAsset;
                return template != null && template.TemplateId.StartsWith("starter_room_template", System.StringComparison.Ordinal);
            });
            AddMatchingAssets(paths, root, "t:MapGenCorridorTemplateAsset", asset =>
            {
                var template = asset as MapGenCorridorTemplateAsset;
                return template != null && template.TemplateId == "starter_corridor_template";
            });
            AddMatchingAssets(paths, root, "t:GameObject", asset => IsStarterGeneratedName(asset.name));
            AddMatchingAssets(paths, root, "t:Material", asset => IsStarterGeneratedName(asset.name));

            var deleted = 0;
            foreach (var path in paths)
            {
                if (AssetDatabase.DeleteAsset(path))
                {
                    deleted++;
                }
            }

            if (deleted > 0)
            {
                AssetDatabase.SaveAssets();
                MapGenV2AssetDatabasePolicy.RefreshAfterBulkAssetChanges();
            }

            return deleted;
        }

        private static void AddMatchingAssets(
            System.Collections.Generic.HashSet<string> paths,
            string root,
            string filter,
            System.Func<Object, bool> predicate)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset != null && predicate(asset))
                {
                    paths.Add(path);
                }
            }
        }

        private static bool IsStarterGeneratedName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return false;
            }

            return assetName.StartsWith("StarterFloor", System.StringComparison.Ordinal)
                || assetName.StartsWith("StarterCorridorFloor", System.StringComparison.Ordinal)
                || assetName.StartsWith("StarterWall", System.StringComparison.Ordinal)
                || assetName.StartsWith("StarterCorner", System.StringComparison.Ordinal)
                || assetName.StartsWith("StarterCeiling", System.StringComparison.Ordinal)
                || assetName.StartsWith("StarterDoor", System.StringComparison.Ordinal)
                || assetName.StartsWith("StarterProp", System.StringComparison.Ordinal);
        }

        private static void EnsureStarterFolders(string root)
        {
            if (root == Root)
            {
                MapGenV2AssetFolderUtility.CreateDefaultFolders();
                return;
            }

            EnsureAssetFolder(root);
            EnsureAssetFolder($"{root}/Drafts");
            EnsureAssetFolder($"{root}/MaterializedPrefabs");
            EnsureAssetFolder($"{root}/ModuleSets");
            EnsureAssetFolder($"{root}/Profiles");
            EnsureAssetFolder($"{root}/RoomShapes");
            EnsureAssetFolder($"{root}/RuleSets");
            EnsureAssetFolder($"{root}/StyleSets");
            EnsureAssetFolder($"{root}/Templates");
            EnsureAssetFolder($"{root}/BakedMaps");
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static GameObject CreatePrimitivePrefab(
            string root,
            string name,
            PrimitiveType primitiveType,
            Color materialColor,
            Vector3 meshScale,
            Vector3 meshLocalPosition)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath($"{root}/MaterializedPrefabs/{name}.prefab");
            var instance = new GameObject(name);
            var mesh = GameObject.CreatePrimitive(primitiveType);
            mesh.name = $"{name}_Visual";
            mesh.transform.SetParent(instance.transform, false);
            mesh.transform.localPosition = meshLocalPosition;
            mesh.transform.localScale = meshScale;
            var renderer = mesh.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateStarterMaterial(root, name, materialColor);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static Material CreateStarterMaterial(string root, string name, Color color)
        {
            var materialPath = AssetDatabase.GenerateUniqueAssetPath($"{root}/MaterializedPrefabs/{name}Material.mat");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = $"{name}Material",
                color = color
            };
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static MapGenModuleEntry[] Entries(GameObject prefab)
        {
            return new[]
            {
                new MapGenModuleEntry
                {
                    Prefab = prefab,
                    Weight = 1,
                    Footprint = Vector2Int.one
                }
            };
        }

        private static void PopulateStarterRoomShape(MapGenRoomShapeAsset roomShape)
        {
            roomShape.ShapeId = "starter_room_shape";
            roomShape.Category = MapGenRoomCategory.Main;
            roomShape.Resize(new Vector2Int(3, 3));
            for (var y = 0; y < roomShape.Height; y++)
            {
                for (var x = 0; x < roomShape.Width; x++)
                {
                    roomShape.SetCell(x, y, new MapGenShapeCell
                    {
                        State = MapGenCellState.Room,
                        SocketKind = MapGenSocketKind.None,
                        SocketId = string.Empty
                    });
                }
            }
        }

        private static MapGenRoomTemplateAsset CreateStarterRoomTemplate(
            string root,
            MapGenRoomShapeAsset roomShape,
            string assetName,
            string templateId,
            MapGenRoomCategory category)
        {
            var template = ScriptableObjectUtility.CreateAsset<MapGenRoomTemplateAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{root}/Templates/{assetName}.asset"));
            PopulateStarterRoomTemplate(template, templateId, category);
            template.SourceRoomShapes = new[] { roomShape };
            return template;
        }

        private static void PopulateStarterRoomTemplate(
            MapGenRoomTemplateAsset template,
            string templateId,
            MapGenRoomCategory category)
        {
            template.TemplateId = templateId;
            template.Footprint = new Vector2Int(3, 3);
            template.RoomCategory = category;
            template.SizeClass = MapGenRoomSizeClass.Medium;
            template.Weight = 1;
            template.FloorCells = new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1),
                new Vector2Int(0, 2),
                new Vector2Int(1, 2),
                new Vector2Int(2, 2)
            };
            template.Connectors = new[]
            {
                MapGenConnector.Door(MapGenGridDirection.North, new Vector2Int(1, 2), "main"),
                MapGenConnector.Door(MapGenGridDirection.East, new Vector2Int(2, 1), "main"),
                MapGenConnector.Door(MapGenGridDirection.South, new Vector2Int(1, 0), "main"),
                MapGenConnector.Door(MapGenGridDirection.West, new Vector2Int(0, 1), "main")
            };
        }

        private static void PopulateStarterCorridorTemplate(MapGenCorridorTemplateAsset template)
        {
            template.TemplateId = "starter_corridor_template";
            template.CorridorKind = MapGenCorridorKind.Straight;
            template.Width = 1;
            template.LengthRange = new Vector2Int(2, 6);
            template.Weight = 1;
            template.Connectors = new[]
            {
                MapGenConnector.Door(MapGenGridDirection.North, new Vector2Int(0, 0), "main"),
                MapGenConnector.Door(MapGenGridDirection.West, new Vector2Int(0, 0), "main"),
                MapGenConnector.Door(MapGenGridDirection.East, new Vector2Int(5, 0), "main"),
                MapGenConnector.Door(MapGenGridDirection.South, new Vector2Int(5, 0), "main")
            };
        }

        private static void MarkDirty(params Object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    EditorUtility.SetDirty(obj);
                }
            }
        }
    }
}

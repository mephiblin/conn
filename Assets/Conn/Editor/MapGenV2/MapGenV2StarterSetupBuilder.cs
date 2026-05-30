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

        public static MapGenV2StarterSetup CreateStarterProfileSetup()
        {
            MapGenV2AssetFolderUtility.CreateDefaultFolders();

            var floorPrefab = CreatePrimitivePrefab("StarterFloor", PrimitiveType.Cube, new Color(0.78f, 0.16f, 0.12f), new Vector3(0.92f, 0.08f, 0.92f));
            var corridorPrefab = CreatePrimitivePrefab("StarterCorridorFloor", PrimitiveType.Cube, new Color(0.05f, 0.05f, 0.05f), new Vector3(0.82f, 0.06f, 0.82f));
            var wallPrefab = CreatePrimitivePrefab("StarterWall", PrimitiveType.Cube, new Color(0.2f, 0.32f, 0.85f), new Vector3(0.16f, 1.25f, 0.92f));
            var cornerPrefab = CreatePrimitivePrefab("StarterCorner", PrimitiveType.Cube, new Color(0.12f, 0.22f, 0.58f), new Vector3(0.24f, 1.35f, 0.24f));
            var ceilingPrefab = CreatePrimitivePrefab("StarterCeiling", PrimitiveType.Cube, new Color(0.85f, 0.86f, 0.82f), new Vector3(0.92f, 0.05f, 0.92f));
            var doorPrefab = CreatePrimitivePrefab("StarterDoor", PrimitiveType.Cube, new Color(0.95f, 0.66f, 0.18f), new Vector3(0.56f, 1f, 0.12f));
            var propPrefab = CreatePrimitivePrefab("StarterProp", PrimitiveType.Sphere, new Color(0.2f, 0.72f, 0.42f), new Vector3(0.32f, 0.32f, 0.32f));

            var moduleSet = ScriptableObjectUtility.CreateAsset<MapGenModuleSetAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{Root}/ModuleSets/StarterModuleSet.asset"));
            moduleSet.ModuleSetId = "starter_module_set";
            moduleSet.FloorsA = Entries(floorPrefab);
            moduleSet.FloorsB = Entries(corridorPrefab);
            moduleSet.WallsStraight = Entries(wallPrefab);
            moduleSet.WallsCornerOutside = Entries(cornerPrefab);
            moduleSet.InteriorCeilings = Entries(ceilingPrefab);
            moduleSet.ExteriorCeilings = Entries(ceilingPrefab);
            moduleSet.WholeDoors = Entries(doorPrefab);
            moduleSet.PropCategories = Entries(propPrefab);

            var styleSet = ScriptableObjectUtility.CreateAsset<MapGenStyleSetAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{Root}/StyleSets/StarterStyleSet.asset"));
            styleSet.StyleId = "starter_style";
            styleSet.ModuleSet = moduleSet;

            var ruleSet = ScriptableObjectUtility.CreateAsset<MapGenRuleSetAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{Root}/RuleSets/StarterRuleSet.asset"));
            ruleSet.RequiredRoomCategories = new[]
            {
                MapGenRoomCategory.Start,
                MapGenRoomCategory.Quest,
                MapGenRoomCategory.Boss,
                MapGenRoomCategory.Exit
            };
            ruleSet.UseDirectRoutes = true;
            ruleSet.ReduceDeadEnds = true;

            var roomShape = ScriptableObjectUtility.CreateAsset<MapGenRoomShapeAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{Root}/RoomShapes/StarterRoomShape.asset"));
            PopulateStarterRoomShape(roomShape);

            var profile = ScriptableObjectUtility.CreateAsset<MapGenProfileAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{Root}/Profiles/StarterProfile.asset"));
            profile.ProfileId = "starter_profile";
            profile.DisplayName = "Starter Profile";
            profile.MapSize = new Vector2Int(10, 8);
            profile.Seed = 2001;
            profile.StyleSet = styleSet;
            profile.LayoutRules = ruleSet;
            profile.RoomShapes = new[] { roomShape };

            var draft = ScriptableObjectUtility.CreateAsset<MapGenMockupDraftAsset>(
                AssetDatabase.GenerateUniqueAssetPath($"{Root}/Drafts/StarterDraft.asset"));
            draft.Profile = profile;
            draft.Seed = profile.Seed;

            MarkDirty(moduleSet, styleSet, ruleSet, roomShape, profile, draft);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new MapGenV2StarterSetup
            {
                Profile = profile,
                Draft = draft
            };
        }

        private static GameObject CreatePrimitivePrefab(
            string name,
            PrimitiveType primitiveType,
            Color materialColor,
            Vector3 localScale)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath($"{Root}/MaterializedPrefabs/{name}.prefab");
            var instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = name;
            instance.transform.localScale = localScale;
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateStarterMaterial(name, materialColor);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static Material CreateStarterMaterial(string name, Color color)
        {
            var materialPath = AssetDatabase.GenerateUniqueAssetPath($"{Root}/MaterializedPrefabs/{name}Material.mat");
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

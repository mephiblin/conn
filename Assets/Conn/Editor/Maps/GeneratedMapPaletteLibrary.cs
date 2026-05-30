using Conn.Authoring.Maps;
using Conn.Core.Maps;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class GeneratedMapPaletteLibrary
    {
        public const string Folder = "Assets/Conn/Authoring/Maps/GeneratedPalettes";
        public const string TilePalettePath = Folder + "/GeneratedDungeonTilePalette.asset";
        public const string ObjectPalettePath = Folder + "/GeneratedDungeonObjectPalette.asset";

        public static void AssignGeneratedPalettes(EditableMapDraftAsset draft, bool persistAssets = false)
        {
            if (draft == null)
            {
                return;
            }

            draft.TilePalette = persistAssets ? EnsureTilePalette() : CreateTilePalette();
            draft.ObjectPalette = persistAssets ? EnsureObjectPalette() : CreateObjectPalette();
        }

        public static bool UsesGeneratedPalettes(EditableMapDraftAsset draft)
        {
            return draft != null
                && draft.TilePalette != null
                && draft.TilePalette.Id == "generated_dungeon_tiles"
                && draft.ObjectPalette != null
                && draft.ObjectPalette.Id == "generated_dungeon_objects";
        }

        public static MapTilePaletteAsset EnsureTilePalette()
        {
            EnsureFolder();
            var palette = AssetDatabase.LoadAssetAtPath<MapTilePaletteAsset>(TilePalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MapTilePaletteAsset>();
                AssetDatabase.CreateAsset(palette, TilePalettePath);
            }

            PopulateTilePalette(palette);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        public static MapObjectPaletteAsset EnsureObjectPalette()
        {
            EnsureFolder();
            var palette = AssetDatabase.LoadAssetAtPath<MapObjectPaletteAsset>(ObjectPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MapObjectPaletteAsset>();
                AssetDatabase.CreateAsset(palette, ObjectPalettePath);
            }

            PopulateObjectPalette(palette);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static MapTilePaletteAsset CreateTilePalette()
        {
            var palette = ScriptableObject.CreateInstance<MapTilePaletteAsset>();
            palette.hideFlags = HideFlags.HideAndDontSave;
            PopulateTilePalette(palette);
            return palette;
        }

        private static MapObjectPaletteAsset CreateObjectPalette()
        {
            var palette = ScriptableObject.CreateInstance<MapObjectPaletteAsset>();
            palette.hideFlags = HideFlags.HideAndDontSave;
            PopulateObjectPalette(palette);
            return palette;
        }

        private static void PopulateTilePalette(MapTilePaletteAsset palette)
        {
            palette.Id = "generated_dungeon_tiles";
            palette.DisplayName = "Generated Dungeon Tile Palette";
            palette.ThemeId = "generated_dungeon";
            palette.Tiles = new[]
            {
                Tile("generated_floor", RoomChunkCellType.Floor, "generated_floor_runtime", true),
                Tile("generated_corridor", RoomChunkCellType.Floor, "generated_corridor_runtime", true),
                Tile("generated_upper_floor", RoomChunkCellType.Floor, "generated_upper_floor_runtime", true),
                Tile("generated_slope", RoomChunkCellType.Slope, "generated_slope_runtime", true),
                Tile("generated_stair", RoomChunkCellType.Stair, "generated_stair_runtime", true),
                Tile("generated_door", RoomChunkCellType.Floor, "generated_door_runtime", true),
                Tile("generated_wall", RoomChunkCellType.Wall, "generated_wall_runtime", false),
                Tile("generated_wall_corner", RoomChunkCellType.Wall, "generated_wall_corner_runtime", false),
                Tile("generated_wall_edge", RoomChunkCellType.Wall, "generated_wall_edge_runtime", false),
                Tile("generated_wall_solid", RoomChunkCellType.Wall, "generated_wall_solid_runtime", false)
            };
        }

        private static void PopulateObjectPalette(MapObjectPaletteAsset palette)
        {
            palette.Id = "generated_dungeon_objects";
            palette.DisplayName = "Generated Dungeon Object Palette";
            palette.ThemeId = "generated_dungeon";
            palette.Objects = new[]
            {
                Object("door", RoomChunkObjectKind.Decor, "door", false),
                Object("treasure_chest", RoomChunkObjectKind.Chest, "treasure_chest", false),
                Object("spawn_hint", RoomChunkObjectKind.SpawnHint, "spawn_hint", false),
                Object("torch_wall", RoomChunkObjectKind.Torch, "torch_wall", false),
                Object("barrel", RoomChunkObjectKind.Barrel, "barrel", false),
                Object("rubble_blocker", RoomChunkObjectKind.Blocker, "rubble_blocker", true)
            };
        }

        private static MapTilePaletteEntry Tile(string id, RoomChunkCellType terrainType, string runtimeMaterialId, bool walkable)
        {
            return new MapTilePaletteEntry
            {
                Id = id,
                TerrainType = terrainType,
                RuntimeMaterialId = runtimeMaterialId,
                DefaultWalkable = walkable,
                DefaultHeightCost = walkable ? 1 : 0
            };
        }

        private static MapObjectPaletteEntry Object(string id, RoomChunkObjectKind kind, string runtimeReferenceId, bool blocksMovement)
        {
            return new MapObjectPaletteEntry
            {
                Id = id,
                Kind = kind,
                FootprintWidth = 1,
                FootprintDepth = 1,
                BlocksMovement = blocksMovement,
                RuntimeReferenceId = runtimeReferenceId
            };
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Conn/Authoring/Maps"))
            {
                AssetDatabase.CreateFolder("Assets/Conn/Authoring", "Maps");
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Conn/Authoring/Maps", "GeneratedPalettes");
            }
        }
    }
}

using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class MapProfileAuthoringAssetFactory
    {
        public const string ProfilesFolder = "Assets/Conn/Authoring/Maps/Profiles";

        public static MapProfileAsset CreateEmptyProfileAsset()
        {
            EnsureFolder(ProfilesFolder);

            var asset = ScriptableObject.CreateInstance<MapProfileAsset>();
            asset.Id = "new_map_profile";
            asset.DisplayName = "New Map Profile";
            asset.MapKind = "dungeon";
            asset.ThemeId = "default";
            asset.GridSize = new Vector2Int(5, 5);
            asset.RoomSize = new Vector2Int(8, 8);
            asset.RoomCountMin = 6;
            asset.RoomCountMax = 12;
            asset.TargetModuleCount = 6;
            asset.CriticalPathMin = 3;
            asset.CriticalPathMax = 5;
            asset.SideBranchCount = 1;
            asset.LoopMin = 0;
            asset.LoopMax = 1;
            asset.MergeChancePer1000 = 0;
            asset.LockedDoorKeyId = string.Empty;
            asset.RequiredAnchors = Array.Empty<MapAnchorKind>();
            asset.RequiredLandmarkRooms = Array.Empty<LandmarkRoomAsset>();
            asset.RoomPools = Array.Empty<MapRoomPoolRule>();
            asset.OptionalChunks = Array.Empty<RoomChunkAsset>();
            asset.OptionalLandmarks = Array.Empty<LandmarkRoomAsset>();
            asset.AllowedSpawnTables = Array.Empty<Conn.Authoring.Content.SpawnTableAsset>();
            asset.SpawnTagFilters = Array.Empty<string>();
            asset.DirectEncounterOverrides = Array.Empty<Conn.Authoring.Content.EncounterDefinitionAsset>();
            asset.CompatibilityTags = Array.Empty<string>();

            var path = AssetDatabase.GenerateUniqueAssetPath($"{ProfilesFolder}/MapProfile.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            var parent = path.Substring(0, slash);
            var folder = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}

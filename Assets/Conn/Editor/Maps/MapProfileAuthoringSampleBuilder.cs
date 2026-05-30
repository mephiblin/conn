using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class MapProfileAuthoringSampleBuilder
    {
        public const string Folder = "Assets/Conn/Authoring/Maps/Profiles/ChapterTwoFirstSlice";
        public const string ProfilePath = Folder + "/ch2_first_slice_ruins_Profile.asset";

        [MenuItem("Conn/Map/Create Chapter 2 Sample Map Profile Assets")]
        public static void CreateChapterTwoSampleProfileAssetsMenu()
        {
            var profile = CreateChapterTwoSampleProfileAssets();
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }

        public static MapProfileAsset CreateChapterTwoSampleProfileAssets()
        {
            EnsureFolder(Folder);

            var runtimeProfile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var resourceSet = LoadOrCreate<MapResourceSetAsset>(Folder + "/ch2_first_slice_ruins_ResourceSet.asset");
            resourceSet.Id = "ch2_first_slice_ruins_resources";
            resourceSet.DisplayName = "Chapter 2 First Slice Ruins Resources";
            resourceSet.ThemeId = runtimeProfile.Theme;
            EditorUtility.SetDirty(resourceSet);

            var weights = LoadOrCreate<GenerationWeightProfileAsset>(Folder + "/ch2_first_slice_ruins_Weights.asset");
            weights.Id = "ch2_first_slice_ruins_weights";
            weights.DisplayName = "Chapter 2 First Slice Ruins Weights";
            weights.MapProfileId = runtimeProfile.ProfileId;
            EditorUtility.SetDirty(weights);

            var chunkAssets = new List<RoomChunkAsset>();
            foreach (var chunk in MapGenerationCatalog.ChapterTwoFirstSliceChunks())
            {
                var asset = LoadOrCreate<RoomChunkAsset>($"{Folder}/{chunk.Id}.asset");
                PopulateChunk(asset, chunk, runtimeProfile.Theme);
                chunkAssets.Add(asset);
                EditorUtility.SetDirty(asset);
            }

            var profile = LoadOrCreate<MapProfileAsset>(ProfilePath);
            profile.Id = runtimeProfile.ProfileId;
            profile.DisplayName = "Chapter 2 First Slice Ruins";
            profile.MapKind = runtimeProfile.MapKind;
            profile.ThemeId = runtimeProfile.Theme;
            profile.GridSize = new Vector2Int(runtimeProfile.Width, runtimeProfile.Height);
            profile.RoomSize = new Vector2Int(runtimeProfile.RoomWidth, runtimeProfile.RoomHeight);
            profile.RoomCountMin = runtimeProfile.RoomCountMin;
            profile.RoomCountMax = runtimeProfile.RoomCountMax;
            profile.TargetModuleCount = runtimeProfile.TargetModuleCount;
            profile.CriticalPathMin = runtimeProfile.CriticalPathMin;
            profile.CriticalPathMax = runtimeProfile.CriticalPathMax;
            profile.SideBranchCount = runtimeProfile.SideBranchCount;
            profile.LoopMin = runtimeProfile.LoopMin;
            profile.LoopMax = runtimeProfile.LoopMax;
            profile.MergeChancePer1000 = runtimeProfile.MergeChancePer1000;
            profile.LockedDoorKeyId = runtimeProfile.LockedDoorKeyId;
            profile.RequiredAnchors = runtimeProfile.RequiredAnchors.ToArray();
            profile.ResourceSet = resourceSet;
            profile.RoomPools = BuildRoomPools(chunkAssets).ToArray();
            profile.OptionalChunks = chunkAssets.ToArray();
            profile.RequiredLandmarkRooms = System.Array.Empty<LandmarkRoomAsset>();
            profile.OptionalLandmarks = System.Array.Empty<LandmarkRoomAsset>();
            profile.GenerationWeightProfile = weights;
            EditorUtility.SetDirty(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        private static void PopulateChunk(RoomChunkAsset asset, ChunkPreset chunk, string theme)
        {
            asset.Id = chunk.Id;
            asset.DisplayName = ObjectNames.NicifyVariableName(chunk.Id);
            asset.ThemeId = theme;
            asset.Size = new Vector2Int(chunk.Width, chunk.Height);
            asset.LayoutKind = chunk.LayoutKind;
            asset.CorridorLength = chunk.CorridorLength;
            asset.CorridorWidth = chunk.CorridorWidth;
            asset.DeadEndDepth = chunk.DeadEndDepth;
            asset.OpenSides = chunk.OpenSides;
            asset.DoorSockets = chunk.DoorSockets;
            asset.SocketDefinitions = ToSocketDefinitions(chunk).ToArray();
            asset.PopulationAllowed = chunk.PopulationAllowed;
            asset.RoleTags = ToRoleTags(chunk.RoleTags);
            asset.Anchors = ToAuthoringAnchors(chunk.Anchors);
            asset.Cells = chunk.Cells.ToArray();
            asset.Objects = chunk.Objects.ToArray();
        }

        private static List<MapRoomPoolRule> BuildRoomPools(List<RoomChunkAsset> chunkAssets)
        {
            var pools = new List<MapRoomPoolRule>();
            AddPool(pools, chunkAssets, MapRoomPoolRole.Start, RoomChunkLayoutKind.Room, 1, 1, 1, true, chunk => HasRole(chunk, MapRoomRole.Start));
            AddPool(pools, chunkAssets, MapRoomPoolRole.Main, RoomChunkLayoutKind.Room, 1, 0, 1, true, chunk => HasRole(chunk, MapRoomRole.MainPath) && chunk.LayoutKind == RoomChunkLayoutKind.Room);
            AddPool(pools, chunkAssets, MapRoomPoolRole.Corridor, RoomChunkLayoutKind.Corridor, 0, 0, 1, true, chunk => HasRole(chunk, MapRoomRole.MainPath) && chunk.LayoutKind == RoomChunkLayoutKind.Corridor);
            AddPool(pools, chunkAssets, MapRoomPoolRole.Hub, RoomChunkLayoutKind.Hub, 0, 0, 1, true, chunk => HasRole(chunk, MapRoomRole.MainPath) && chunk.LayoutKind == RoomChunkLayoutKind.Hub);
            AddPool(pools, chunkAssets, MapRoomPoolRole.Side, RoomChunkLayoutKind.Room, 0, 0, 1, false, chunk => HasRole(chunk, MapRoomRole.SideBranch) && chunk.LayoutKind == RoomChunkLayoutKind.Room);
            AddPool(pools, chunkAssets, MapRoomPoolRole.DeadEnd, RoomChunkLayoutKind.DeadEnd, 0, 0, 1, false, chunk => HasRole(chunk, MapRoomRole.SideBranch) && chunk.LayoutKind == RoomChunkLayoutKind.DeadEnd);
            AddPool(pools, chunkAssets, MapRoomPoolRole.Quest, RoomChunkLayoutKind.Room, 1, 1, 1, true, chunk => HasRole(chunk, MapRoomRole.QuestTarget));
            AddPool(pools, chunkAssets, MapRoomPoolRole.Boss, RoomChunkLayoutKind.Room, 1, 1, 1, true, chunk => HasRole(chunk, MapRoomRole.Boss));
            AddPool(pools, chunkAssets, MapRoomPoolRole.Exit, RoomChunkLayoutKind.Room, 1, 1, 1, true, chunk => HasRole(chunk, MapRoomRole.Exit));
            AddPool(pools, chunkAssets, MapRoomPoolRole.HeightTransition, RoomChunkLayoutKind.HeightTransition, 0, 0, 1, true, chunk => HasRole(chunk, MapRoomRole.MainPath) && chunk.LayoutKind == RoomChunkLayoutKind.HeightTransition);
            return pools;
        }

        private static void AddPool(
            List<MapRoomPoolRule> pools,
            List<RoomChunkAsset> chunkAssets,
            MapRoomPoolRole role,
            RoomChunkLayoutKind layoutKind,
            int minCount,
            int maxCount,
            int weight,
            bool required,
            Func<RoomChunkAsset, bool> predicate)
        {
            var allowedChunks = new List<RoomChunkAsset>();
            foreach (var chunk in chunkAssets)
            {
                if (chunk != null && predicate(chunk))
                {
                    allowedChunks.Add(chunk);
                }
            }

            pools.Add(new MapRoomPoolRule
            {
                Role = role,
                LayoutKind = layoutKind,
                MinCount = minCount,
                MaxCount = maxCount,
                Weight = weight,
                Required = required,
                AllowedChunks = allowedChunks.ToArray()
            });
        }

        private static bool HasRole(RoomChunkAsset chunk, MapRoomRole role)
        {
            foreach (var tag in chunk.RoleTags ?? Array.Empty<string>())
            {
                if (Enum.TryParse<MapRoomRole>(tag, true, out var parsed) && parsed == role)
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] ToRoleTags(List<MapRoomRole> roles)
        {
            var tags = new string[roles?.Count ?? 0];
            for (var i = 0; i < tags.Length; i++)
            {
                tags[i] = roles[i].ToString();
            }

            return tags;
        }

        private static AuthoringChunkAnchor[] ToAuthoringAnchors(List<ChunkAnchor> anchors)
        {
            var values = new AuthoringChunkAnchor[anchors?.Count ?? 0];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new AuthoringChunkAnchor
                {
                    Id = anchors[i].Id,
                    Kind = anchors[i].Kind,
                    Cell = new Vector2Int(anchors[i].X, anchors[i].Y)
                };
            }

            return values;
        }

        private static List<RoomChunkSocketDefinition> ToSocketDefinitions(ChunkPreset chunk)
        {
            var sockets = new List<RoomChunkSocketDefinition>();
            foreach (var side in RoomChunkSocketRules.EnumerateSides(
                MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West))
            {
                var isOpen = (chunk.OpenSides & side) != MapDirection.None;
                sockets.Add(new RoomChunkSocketDefinition
                {
                    Side = side,
                    SocketType = ResolveSocketType(chunk.LayoutKind, isOpen),
                    SocketId = ResolveSocketId(chunk.LayoutKind, isOpen)
                });
            }

            return sockets;
        }

        private static RoomChunkSocketType ResolveSocketType(RoomChunkLayoutKind layoutKind, bool isOpen)
        {
            if (!isOpen)
            {
                return RoomChunkSocketType.Blocked;
            }

            return layoutKind == RoomChunkLayoutKind.Corridor
                ? RoomChunkSocketType.Corridor
                : RoomChunkSocketType.Door;
        }

        private static string ResolveSocketId(RoomChunkLayoutKind layoutKind, bool isOpen)
        {
            if (!isOpen)
            {
                return string.Empty;
            }

            return layoutKind == RoomChunkLayoutKind.Corridor ? "corridor" : "door";
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            EnsureFolderForAsset(path);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolderForAsset(string path)
        {
            var slash = path.LastIndexOf('/');
            if (slash > 0)
            {
                EnsureFolder(path.Substring(0, slash));
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}

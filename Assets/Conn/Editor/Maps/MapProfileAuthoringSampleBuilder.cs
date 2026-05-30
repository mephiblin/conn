using Conn.Authoring.Maps;
using Conn.Core.Maps;
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
            asset.OpenSides = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West;
            asset.DoorSockets = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West;
            asset.PopulationAllowed = chunk.PopulationAllowed;
            asset.RoleTags = ToRoleTags(chunk.RoleTags);
            asset.Anchors = ToAuthoringAnchors(chunk.Anchors);
            asset.Cells = chunk.Cells.ToArray();
            asset.Objects = chunk.Objects.ToArray();
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

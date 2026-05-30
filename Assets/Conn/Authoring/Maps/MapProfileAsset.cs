using Conn.Authoring.Content;
using Conn.Core.Maps;
using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Map Profile", fileName = "MapProfile")]
    public sealed class MapProfileAsset : ScriptableObject
    {
        [Header("Runtime Identity")]
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string MapKind = string.Empty;
        public string ThemeId = string.Empty;

        [Header("Generation Rules")]
        public Vector2Int GridSize = new Vector2Int(5, 5);
        public Vector2Int RoomSize = new Vector2Int(8, 8);
        public int RoomCountMin = 6;
        public int RoomCountMax = 12;
        public int TargetModuleCount = 6;
        public int CriticalPathMin = 3;
        public int CriticalPathMax = 5;
        public int SideBranchCount = 1;
        public int LoopMin;
        public int LoopMax = 1;
        public int MergeChancePer1000;
        public string LockedDoorKeyId = string.Empty;
        public MapAnchorKind[] RequiredAnchors = Array.Empty<MapAnchorKind>();

        [Header("Authoring Links")]
        public MapResourceSetAsset ResourceSet;
        public LandmarkRoomAsset[] RequiredLandmarkRooms = Array.Empty<LandmarkRoomAsset>();
        public MapRoomPoolRule[] RoomPools = Array.Empty<MapRoomPoolRule>();
        public RoomChunkAsset[] OptionalChunks = Array.Empty<RoomChunkAsset>();
        public LandmarkRoomAsset[] OptionalLandmarks = Array.Empty<LandmarkRoomAsset>();
        public SpawnTableAsset[] AllowedSpawnTables = Array.Empty<SpawnTableAsset>();
        public string[] SpawnTagFilters = Array.Empty<string>();
        public EncounterDefinitionAsset[] DirectEncounterOverrides = Array.Empty<EncounterDefinitionAsset>();
        public GenerationWeightProfileAsset GenerationWeightProfile;
        public string[] CompatibilityTags = Array.Empty<string>();

        public MapProfile ToRuntimeProfile()
        {
            return new MapProfile
            {
                ProfileId = Id,
                MapKind = MapKind,
                Theme = ThemeId,
                Width = GridSize.x,
                Height = GridSize.y,
                RoomWidth = RoomSize.x,
                RoomHeight = RoomSize.y,
                RoomCountMin = RoomCountMin,
                RoomCountMax = RoomCountMax,
                TargetModuleCount = TargetModuleCount,
                CriticalPathMin = CriticalPathMin,
                CriticalPathMax = CriticalPathMax,
                SideBranchCount = SideBranchCount,
                LoopMin = LoopMin,
                LoopMax = LoopMax,
                MergeChancePer1000 = MergeChancePer1000,
                LockedDoorKeyId = LockedDoorKeyId,
                RequiredAnchors = new System.Collections.Generic.List<MapAnchorKind>(RequiredAnchors ?? Array.Empty<MapAnchorKind>()),
                ResourceSetId = ResourceSet != null ? ResourceSet.Id : string.Empty,
                OptionalChunkIds = ResolveOptionalChunkIds(),
                RoomPools = ResolveRoomPools(),
                SpawnTableIds = ResolveSpawnTableIds(),
                SpawnTagFilters = new System.Collections.Generic.List<string>(SpawnTagFilters ?? Array.Empty<string>()),
                DirectEncounterOverrideIds = ResolveEncounterOverrideIds(),
                GenerationWeightProfileId = GenerationWeightProfile != null ? GenerationWeightProfile.Id : string.Empty
            };
        }

        private System.Collections.Generic.List<RuntimeMapRoomPoolRule> ResolveRoomPools()
        {
            var pools = new System.Collections.Generic.List<RuntimeMapRoomPoolRule>();
            var authoringPools = RoomPools;
            if (authoringPools == null || authoringPools.Length == 0)
            {
                authoringPools = BuildLegacyBridgeRoomPools();
            }

            foreach (var pool in authoringPools ?? Array.Empty<MapRoomPoolRule>())
            {
                if (pool == null)
                {
                    continue;
                }

                var runtimePool = new RuntimeMapRoomPoolRule
                {
                    Role = pool.Role,
                    LayoutKind = pool.LayoutKind,
                    MinCount = pool.MinCount,
                    MaxCount = pool.MaxCount,
                    Weight = pool.Weight,
                    Required = pool.Required
                };

                foreach (var chunk in pool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
                {
                    if (chunk != null && !string.IsNullOrWhiteSpace(chunk.Id))
                    {
                        runtimePool.AllowedChunkIds.Add(chunk.Id);
                    }
                }

                pools.Add(runtimePool);
            }

            return pools;
        }

        private System.Collections.Generic.List<string> ResolveOptionalChunkIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (var chunk in OptionalChunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (chunk != null && !string.IsNullOrWhiteSpace(chunk.Id))
                {
                    ids.Add(chunk.Id);
                }
            }

            return ids;
        }

        private MapRoomPoolRule[] BuildLegacyBridgeRoomPools()
        {
            return new[]
            {
                CreateLegacyBridgePool(MapRoomPoolRole.Start, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Start, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(MapRoomPoolRole.Main, RoomChunkLayoutKind.Room, 1, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(MapRoomPoolRole.Corridor, RoomChunkLayoutKind.Corridor, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Corridor),
                CreateLegacyBridgePool(MapRoomPoolRole.Hub, RoomChunkLayoutKind.Hub, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Hub),
                CreateLegacyBridgePool(MapRoomPoolRole.Side, RoomChunkLayoutKind.Room, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(MapRoomPoolRole.DeadEnd, RoomChunkLayoutKind.DeadEnd, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.DeadEnd),
                CreateLegacyBridgePool(MapRoomPoolRole.Quest, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.QuestTarget, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(MapRoomPoolRole.Boss, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Boss, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(MapRoomPoolRole.Exit, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Exit, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(MapRoomPoolRole.HeightTransition, RoomChunkLayoutKind.HeightTransition, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.HeightTransition)
            };
        }

        private MapRoomPoolRule CreateLegacyBridgePool(
            MapRoomPoolRole poolRole,
            RoomChunkLayoutKind layoutKind,
            int minCount,
            int maxCount,
            bool required,
            MapRoomRole chunkRole,
            RoomChunkLayoutKind chunkLayoutKind)
        {
            var allowedChunks = new System.Collections.Generic.List<RoomChunkAsset>();
            foreach (var chunk in OptionalChunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (chunk != null && chunk.LayoutKind == chunkLayoutKind && RoleTagsContain(chunk.RoleTags, chunkRole))
                {
                    allowedChunks.Add(chunk);
                }
            }

            return new MapRoomPoolRule
            {
                Role = poolRole,
                LayoutKind = layoutKind,
                MinCount = minCount,
                MaxCount = maxCount,
                Weight = 1,
                Required = required,
                AllowedChunks = allowedChunks.ToArray()
            };
        }

        private static bool RoleTagsContain(string[] roleTags, MapRoomRole role)
        {
            foreach (var tag in roleTags ?? Array.Empty<string>())
            {
                if (Enum.TryParse<MapRoomRole>(tag, true, out var parsed) && parsed == role)
                {
                    return true;
                }
            }

            return false;
        }

        private System.Collections.Generic.List<string> ResolveSpawnTableIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (var spawnTable in AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                if (spawnTable != null && !string.IsNullOrWhiteSpace(spawnTable.Id))
                {
                    ids.Add(spawnTable.Id);
                }
            }

            return ids;
        }

        private System.Collections.Generic.List<string> ResolveEncounterOverrideIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (var encounter in DirectEncounterOverrides ?? Array.Empty<EncounterDefinitionAsset>())
            {
                if (encounter != null && !string.IsNullOrWhiteSpace(encounter.Id))
                {
                    ids.Add(encounter.Id);
                }
            }

            return ids;
        }
    }
}

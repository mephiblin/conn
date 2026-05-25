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
                SpawnTableIds = ResolveSpawnTableIds(),
                SpawnTagFilters = new System.Collections.Generic.List<string>(SpawnTagFilters ?? Array.Empty<string>()),
                DirectEncounterOverrideIds = ResolveEncounterOverrideIds(),
                GenerationWeightProfileId = GenerationWeightProfile != null ? GenerationWeightProfile.Id : string.Empty
            };
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

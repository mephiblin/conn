using System;
using System.Collections.Generic;
using UnityEngine;

namespace Conn.Core.Maps
{
    [CreateAssetMenu(menuName = "Conn/Runtime Map Generation Bundle", fileName = "RuntimeMapGenerationBundle")]
    public sealed class RuntimeMapGenerationBundleAsset : ScriptableObject
    {
        public RuntimeMapGenerationBundle Bundle = new RuntimeMapGenerationBundle();
    }

    [Serializable]
    public sealed class RuntimeMapGenerationBundle
    {
        public string BundleId = string.Empty;
        public string Version = string.Empty;
        public List<RuntimeMapProfileEntry> Profiles = new List<RuntimeMapProfileEntry>();

        public RuntimeMapProfileEntry FindProfile(string profileId)
        {
            foreach (var profile in Profiles ?? new List<RuntimeMapProfileEntry>())
            {
                if (profile != null && profile.Profile != null && profile.Profile.ProfileId == profileId)
                {
                    return profile;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class RuntimeMapProfileEntry
    {
        public MapProfile Profile = new MapProfile();
        public int Floor = 1;
        public int Difficulty;
        public List<ChunkPreset> Chunks = new List<ChunkPreset>();
        public List<string> ResourceSetRuntimeIds = new List<string>();
        public List<string> SpawnTableIds = new List<string>();
        public List<RuntimeEncounterPlacementRule> EncounterPlacementRules = new List<RuntimeEncounterPlacementRule>();
        public List<string> GenerationWeightProfileIds = new List<string>();
        public string ValidationHash = string.Empty;
    }

    [Serializable]
    public sealed class RuntimeEncounterPlacementRule
    {
        public string Id = string.Empty;
        public MapPlacementKind PlacementKind = MapPlacementKind.Monster;
        public string RoomRole = string.Empty;
        public string SpawnSourceId = string.Empty;
        public string EncounterId = string.Empty;
        public string PrimaryMonsterId = string.Empty;
        public string SpawnRole = string.Empty;
        public bool RequiredForQuest;
        public List<RuntimeSpawnEntry> SpawnEntries = new List<RuntimeSpawnEntry>();
    }

    [Serializable]
    public sealed class RuntimeSpawnEntry
    {
        public string EncounterId = string.Empty;
        public string PrimaryMonsterId = string.Empty;
        public string SpawnSourceId = string.Empty;
        public string SpawnRole = string.Empty;
        public int Weight = 1;
        public int MinFloor = 1;
        public int MaxFloor = 99;
        public int MinDifficulty;
        public int MaxDifficulty;
        public List<string> ThemeTags = new List<string>();
        public List<string> BiomeTags = new List<string>();
        public List<string> SpawnRoleTags = new List<string>();
        public List<string> AllowedMapTags = new List<string>();
        public List<string> CompatibilityTags = new List<string>();
        public List<string> RoomRoleConstraints = new List<string>();
    }
}

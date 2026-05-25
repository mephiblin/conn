using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Spawn Table", fileName = "SpawnTable")]
    public sealed class SpawnTableAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string[] RequiredThemeTags = Array.Empty<string>();
        public string[] RequiredBiomeTags = Array.Empty<string>();
        public string[] RequiredSpawnRoleTags = Array.Empty<string>();
        public SpawnEncounterEntry[] EncounterEntries = Array.Empty<SpawnEncounterEntry>();
        public SpawnMonsterEntry[] DirectMonsterEntries = Array.Empty<SpawnMonsterEntry>();
        public string[] AllowedRoomRoles = Array.Empty<string>();
        [TextArea]
        public string Notes = string.Empty;
    }

    [Serializable]
    public sealed class SpawnEncounterEntry
    {
        public EncounterDefinitionAsset Encounter;
        public string EncounterId = string.Empty;
        public int Weight = 1;
        public int MinFloor = 1;
        public int MaxFloor = 99;
        public int MinDifficulty;
        public int MaxDifficulty;
        public string[] RoomRoleConstraints = Array.Empty<string>();
    }

    [Serializable]
    public sealed class SpawnMonsterEntry
    {
        public MonsterDefinitionAsset Monster;
        public string MonsterId = string.Empty;
        public int Weight = 1;
        public int MinFloor = 1;
        public int MaxFloor = 99;
        public string GeneratedEncounterPolicy = "single_primary";
        public string[] RoomRoleConstraints = Array.Empty<string>();
    }
}

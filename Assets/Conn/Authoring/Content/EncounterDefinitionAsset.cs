using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Encounter Definition", fileName = "EncounterDefinition")]
    public sealed class EncounterDefinitionAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public MonsterDefinitionAsset PrimaryMonster;
        public string PrimaryMonsterId = string.Empty;
        public int XpReward;
        public string RewardId = string.Empty;
        public string Pattern = "single_primary";
        public EncounterEnemySlotAsset[] EnemySlots = Array.Empty<EncounterEnemySlotAsset>();
        public int MinDifficulty;
        public int MaxDifficulty;
        public string[] ThemeTags = Array.Empty<string>();
        public string[] SpawnRoleTags = Array.Empty<string>();
        public string[] AllowedMapTags = Array.Empty<string>();
        public string[] CompatibilityTags = Array.Empty<string>();

        public ContentEncounterDefinition ToContentDefinition()
        {
            var slots = EnemySlots ?? Array.Empty<EncounterEnemySlotAsset>();
            var contentSlots = new ContentEncounterEnemySlot[slots.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                contentSlots[i] = slots[i].ToContentSlot();
            }

            return new ContentEncounterDefinition
            {
                Id = Id,
                DisplayName = DisplayName,
                MonsterId = ResolvePrimaryMonsterId(),
                XpReward = XpReward,
                RewardId = RewardId,
                Pattern = Pattern,
                EnemySlots = contentSlots,
                MinDifficulty = MinDifficulty,
                MaxDifficulty = MaxDifficulty,
                ThemeTags = ThemeTags ?? Array.Empty<string>(),
                SpawnRoleTags = SpawnRoleTags ?? Array.Empty<string>(),
                AllowedMapTags = AllowedMapTags ?? Array.Empty<string>(),
                CompatibilityTags = CompatibilityTags ?? Array.Empty<string>()
            };
        }

        private string ResolvePrimaryMonsterId()
        {
            if (PrimaryMonster != null && !string.IsNullOrWhiteSpace(PrimaryMonster.Id))
            {
                return PrimaryMonster.Id;
            }

            return PrimaryMonsterId;
        }
    }

    [Serializable]
    public sealed class EncounterEnemySlotAsset
    {
        public string SlotId = string.Empty;
        public MonsterDefinitionAsset Monster;
        public string MonsterId = string.Empty;
        public int Count = 1;
        public bool Primary;

        public ContentEncounterEnemySlot ToContentSlot()
        {
            return new ContentEncounterEnemySlot
            {
                SlotId = SlotId,
                MonsterId = Monster != null && !string.IsNullOrWhiteSpace(Monster.Id) ? Monster.Id : MonsterId,
                Count = Count,
                Primary = Primary
            };
        }
    }
}

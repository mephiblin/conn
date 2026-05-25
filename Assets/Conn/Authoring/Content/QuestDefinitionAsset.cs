using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Quest Definition", fileName = "QuestDefinition")]
    public sealed class QuestDefinitionAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        [TextArea]
        public string Description = string.Empty;
        public string MapKind = "dungeon";
        public string MapProfileId = string.Empty;
        public EncounterDefinitionAsset TargetEncounter;
        public string TargetEncounterId = string.Empty;
        public MonsterDefinitionAsset TargetMonster;
        public string TargetMonsterId = string.Empty;
        public int GoldReward;
        public int XpReward;
        public ContentItemStack[] RewardItems = Array.Empty<ContentItemStack>();

        public ContentQuestDefinition ToContentDefinition()
        {
            return new ContentQuestDefinition
            {
                Id = Id,
                DisplayName = DisplayName,
                Description = Description,
                MapKind = MapKind,
                MapProfileId = MapProfileId,
                TargetEncounterId = ResolveEncounterId(),
                TargetMonsterId = ResolveMonsterId(),
                GoldReward = GoldReward,
                XpReward = XpReward,
                RewardItems = RewardItems ?? Array.Empty<ContentItemStack>()
            };
        }

        private string ResolveEncounterId()
        {
            return TargetEncounter != null && !string.IsNullOrWhiteSpace(TargetEncounter.Id)
                ? TargetEncounter.Id
                : TargetEncounterId;
        }

        private string ResolveMonsterId()
        {
            return TargetMonster != null && !string.IsNullOrWhiteSpace(TargetMonster.Id)
                ? TargetMonster.Id
                : TargetMonsterId;
        }
    }
}

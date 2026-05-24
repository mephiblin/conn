namespace Conn.Core.Combat
{
    public sealed class EncounterDefinition
    {
        public EncounterDefinition(string encounterId, string displayName, string monsterId, int xpReward)
            : this(encounterId, displayName, monsterId, xpReward, string.Empty, string.Empty)
        {
        }

        public EncounterDefinition(string encounterId, string displayName, string monsterId, int xpReward, string rewardId, string pattern)
            : this(encounterId, displayName, monsterId, xpReward, rewardId, pattern, System.Array.Empty<EncounterEnemySlotDefinition>())
        {
        }

        public EncounterDefinition(
            string encounterId,
            string displayName,
            string monsterId,
            int xpReward,
            string rewardId,
            string pattern,
            EncounterEnemySlotDefinition[] enemySlots)
        {
            EncounterId = encounterId;
            DisplayName = displayName;
            MonsterId = monsterId;
            XpReward = xpReward;
            RewardId = rewardId;
            Pattern = pattern;
            EnemySlots = enemySlots ?? System.Array.Empty<EncounterEnemySlotDefinition>();
        }

        public string EncounterId { get; }
        public string DisplayName { get; }
        public string MonsterId { get; }
        public int XpReward { get; }
        public string RewardId { get; }
        public string Pattern { get; }
        public EncounterEnemySlotDefinition[] EnemySlots { get; }
    }

    public sealed class EncounterEnemySlotDefinition
    {
        public EncounterEnemySlotDefinition(string slotId, string monsterId, int count, bool primary)
        {
            SlotId = slotId;
            MonsterId = monsterId;
            Count = count;
            Primary = primary;
        }

        public string SlotId { get; }
        public string MonsterId { get; }
        public int Count { get; }
        public bool Primary { get; }
    }
}

namespace Conn.Core.Combat
{
    public sealed class EncounterDefinition
    {
        public EncounterDefinition(string encounterId, string displayName, string monsterId, int xpReward)
            : this(encounterId, displayName, monsterId, xpReward, string.Empty, string.Empty)
        {
        }

        public EncounterDefinition(string encounterId, string displayName, string monsterId, int xpReward, string rewardId, string pattern)
        {
            EncounterId = encounterId;
            DisplayName = displayName;
            MonsterId = monsterId;
            XpReward = xpReward;
            RewardId = rewardId;
            Pattern = pattern;
        }

        public string EncounterId { get; }
        public string DisplayName { get; }
        public string MonsterId { get; }
        public int XpReward { get; }
        public string RewardId { get; }
        public string Pattern { get; }
    }
}

namespace Conn.Core.Combat
{
    public sealed class EncounterDefinition
    {
        public EncounterDefinition(string encounterId, string displayName, string monsterId, int xpReward)
        {
            EncounterId = encounterId;
            DisplayName = displayName;
            MonsterId = monsterId;
            XpReward = xpReward;
        }

        public string EncounterId { get; }
        public string DisplayName { get; }
        public string MonsterId { get; }
        public int XpReward { get; }
    }
}

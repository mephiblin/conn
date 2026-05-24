namespace Conn.Core.Quests
{
    public sealed class QuestDefinition
    {
        public QuestDefinition(string questId, string displayName, string targetMonsterId, int goldReward)
        {
            QuestId = questId;
            DisplayName = displayName;
            TargetMonsterId = targetMonsterId;
            GoldReward = goldReward;
        }

        public string QuestId { get; }
        public string DisplayName { get; }
        public string TargetMonsterId { get; }
        public int GoldReward { get; }
    }
}

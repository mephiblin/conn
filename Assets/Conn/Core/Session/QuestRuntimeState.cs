namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class QuestRuntimeState
    {
        public string ActiveQuestId = string.Empty;
        public string TargetMonsterId = string.Empty;
        public bool TargetDefeated;
        public bool ReturnAvailable;
        public int GoldReward;

        public bool HasActiveQuest => !string.IsNullOrWhiteSpace(ActiveQuestId);

        public void Clear()
        {
            ActiveQuestId = string.Empty;
            TargetMonsterId = string.Empty;
            TargetDefeated = false;
            ReturnAvailable = false;
            GoldReward = 0;
        }
    }
}

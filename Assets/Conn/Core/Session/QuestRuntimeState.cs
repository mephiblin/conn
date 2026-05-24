namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class QuestRuntimeState
    {
        public string ActiveQuestId = string.Empty;
        public string ActiveQuestTitle = string.Empty;
        public string TargetMonsterId = string.Empty;
        public bool TargetDefeated;
        public bool ReturnAvailable;
        public bool ReturnPromptSeen;
        public int GoldReward;
        public int BoardOfferIndex;
        public int BoardRerollCount;
        public string LastCompletedQuestTitle = string.Empty;
        public int LastGoldReward;

        public bool HasActiveQuest => !string.IsNullOrWhiteSpace(ActiveQuestId);

        public void Clear()
        {
            ActiveQuestId = string.Empty;
            ActiveQuestTitle = string.Empty;
            TargetMonsterId = string.Empty;
            TargetDefeated = false;
            ReturnAvailable = false;
            ReturnPromptSeen = false;
            GoldReward = 0;
        }

        public void ClearLastReward()
        {
            LastCompletedQuestTitle = string.Empty;
            LastGoldReward = 0;
        }
    }
}

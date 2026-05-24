namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class GameSessionState
    {
        public GameMode Mode = GameMode.Title;
        public int Gold;
        public QuestRuntimeState Quest = new QuestRuntimeState();
        public PlayerPoseSnapshot PreEncounterSnapshot = new PlayerPoseSnapshot();

        public void StartNewGame()
        {
            Mode = GameMode.Town;
            Gold = 25;
            Quest.Clear();
            PreEncounterSnapshot.Clear();
        }
    }
}

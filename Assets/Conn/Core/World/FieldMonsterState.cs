namespace Conn.Core.World
{
    [System.Serializable]
    public sealed class FieldMonsterState
    {
        public string StateKey = string.Empty;
        public string PlacementId = string.Empty;
        public string EncounterId = string.Empty;
        public string MonsterId = string.Empty;
        public FieldMonsterStatus Status;
        public bool Defeated;

        public void Setup(string stateKey, string placementId, string encounterId, string monsterId)
        {
            StateKey = stateKey;
            PlacementId = placementId;
            EncounterId = encounterId;
            MonsterId = monsterId;
            if (!Defeated)
            {
                Status = FieldMonsterStatus.Idle;
            }
        }
    }
}

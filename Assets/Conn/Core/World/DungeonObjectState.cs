namespace Conn.Core.World
{
    [System.Serializable]
    public sealed class DungeonObjectState
    {
        public string StateKey = string.Empty;
        public string PlacementId = string.Empty;
        public bool Opened;
        public int GoldCollected;
        public int RiskDamageTaken;

        public void Setup(string stateKey, string placementId)
        {
            StateKey = stateKey;
            PlacementId = placementId;
        }
    }
}

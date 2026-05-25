namespace Conn.Core.World
{
    [System.Serializable]
    public sealed class FieldMonsterState
    {
        public string StateKey = string.Empty;
        public string PlacementId = string.Empty;
        public string EncounterId = string.Empty;
        public string MonsterId = string.Empty;
        public string AiProfileId = string.Empty;
        public float DetectionRadius;
        public float PatrolRadius;
        public float MoveSpeed;
        public float ContactCooldownSeconds;
        public int AnchorX;
        public int AnchorY;
        public float LastContactTime = -9999f;
        public FieldMonsterStatus Status;
        public bool Defeated;

        public void Setup(string stateKey, string placementId, string encounterId, string monsterId)
        {
            Setup(stateKey, placementId, encounterId, monsterId, FieldMonsterAiProfile.Default());
        }

        public void Setup(string stateKey, string placementId, string encounterId, string monsterId, FieldMonsterAiProfile aiProfile)
        {
            StateKey = stateKey;
            PlacementId = placementId;
            EncounterId = encounterId;
            MonsterId = monsterId;
            var profile = aiProfile ?? FieldMonsterAiProfile.Default();
            AiProfileId = profile.ProfileId;
            DetectionRadius = profile.DetectionRadius;
            PatrolRadius = profile.PatrolRadius;
            MoveSpeed = profile.MoveSpeed;
            ContactCooldownSeconds = profile.ContactCooldownSeconds;
            if (!Defeated)
            {
                Status = FieldMonsterStatus.Idle;
            }
        }

        public void SetAnchor(int x, int y)
        {
            AnchorX = x;
            AnchorY = y;
        }

        public bool CanContact(float now)
        {
            return now - LastContactTime >= ContactCooldownSeconds;
        }

        public void MarkContact(float now)
        {
            LastContactTime = now;
        }
    }
}

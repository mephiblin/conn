using System;

namespace Conn.Core.World
{
    [Serializable]
    public sealed class FieldMonsterAiProfile
    {
        public string ProfileId = "field_ai_static_contact";
        public float DetectionRadius = 6f;
        public float PatrolRadius = 3f;
        public float MoveSpeed = 2.5f;
        public float ContactCooldownSeconds = 1f;

        public static FieldMonsterAiProfile Default()
        {
            return new FieldMonsterAiProfile();
        }

        public FieldMonsterAiProfile Clone()
        {
            return new FieldMonsterAiProfile
            {
                ProfileId = ProfileId,
                DetectionRadius = DetectionRadius,
                PatrolRadius = PatrolRadius,
                MoveSpeed = MoveSpeed,
                ContactCooldownSeconds = ContactCooldownSeconds
            };
        }
    }
}

namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class CombatantState
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public int MaxHp;
        public int Hp;

        public bool IsDead => Hp <= 0;

        public void Setup(string id, string displayName, int maxHp)
        {
            Id = id;
            DisplayName = displayName;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public void Damage(int amount)
        {
            Hp -= amount;
            if (Hp < 0)
            {
                Hp = 0;
            }
        }
    }
}

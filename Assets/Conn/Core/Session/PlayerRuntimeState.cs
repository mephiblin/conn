namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class PlayerRuntimeState
    {
        public int MaxHp = 20;
        public int Hp = 20;
        public int Xp;

        public bool IsDead => Hp <= 0;

        public void Reset()
        {
            MaxHp = 20;
            Hp = MaxHp;
            Xp = 0;
        }

        public void Damage(int amount)
        {
            Hp -= amount;
            if (Hp < 0)
            {
                Hp = 0;
            }
        }

        public void Heal(int amount)
        {
            Hp += amount;
            if (Hp > MaxHp)
            {
                Hp = MaxHp;
            }
        }

        public void HealToFull()
        {
            Hp = MaxHp;
        }

        public void GainXp(int amount)
        {
            if (amount > 0)
            {
                Xp += amount;
            }
        }

        public bool SpendXp(int amount)
        {
            if (amount < 0 || Xp < amount)
            {
                return false;
            }

            Xp -= amount;
            return true;
        }
    }
}

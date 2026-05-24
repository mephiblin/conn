namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class PlayerRuntimeState
    {
        public int MaxHp = 20;
        public int Hp = 20;

        public bool IsDead => Hp <= 0;

        public void Reset()
        {
            MaxHp = 20;
            Hp = MaxHp;
        }

        public void Damage(int amount)
        {
            Hp -= amount;
            if (Hp < 0)
            {
                Hp = 0;
            }
        }

        public void HealToFull()
        {
            Hp = MaxHp;
        }
    }
}

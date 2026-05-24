namespace Conn.Core.Combat
{
    public sealed class MonsterDefinition
    {
        public MonsterDefinition(string monsterId, string displayName, int maxHp, int attackPower, int xpReward)
        {
            MonsterId = monsterId;
            DisplayName = displayName;
            MaxHp = maxHp;
            AttackPower = attackPower;
            XpReward = xpReward;
        }

        public string MonsterId { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int AttackPower { get; }
        public int XpReward { get; }
    }
}

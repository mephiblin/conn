namespace Conn.Core.Combat
{
    public sealed class MonsterDefinition
    {
        public MonsterDefinition(string monsterId, string displayName, int maxHp, int attackPower, int xpReward)
            : this(monsterId, displayName, maxHp, attackPower, xpReward, "Attack", attackPower)
        {
        }

        public MonsterDefinition(string monsterId, string displayName, int maxHp, int attackPower, int xpReward, string enemyActionName, int enemyActionPower)
        {
            MonsterId = monsterId;
            DisplayName = displayName;
            MaxHp = maxHp;
            AttackPower = attackPower;
            XpReward = xpReward;
            EnemyActionName = enemyActionName;
            EnemyActionPower = enemyActionPower;
        }

        public string MonsterId { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int AttackPower { get; }
        public int XpReward { get; }
        public string EnemyActionName { get; }
        public int EnemyActionPower { get; }
    }
}

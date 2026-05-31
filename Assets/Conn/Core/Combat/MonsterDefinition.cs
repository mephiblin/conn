namespace Conn.Core.Combat
{
    public sealed class MonsterDefinition
    {
        public MonsterDefinition(string monsterId, string displayName, int maxHp, int attackPower, int xpReward)
            : this(monsterId, displayName, maxHp, attackPower, xpReward, "Attack", attackPower, string.Empty, null, string.Empty)
        {
        }

        public MonsterDefinition(string monsterId, string displayName, int maxHp, int attackPower, int xpReward, string enemyActionName, int enemyActionPower, string species, string[] traitIds, string combatCgResourcePath = "")
        {
            MonsterId = monsterId;
            DisplayName = displayName;
            MaxHp = maxHp;
            AttackPower = attackPower;
            XpReward = xpReward;
            EnemyActionName = enemyActionName;
            EnemyActionPower = enemyActionPower;
            Species = species;
            TraitIds = traitIds ?? System.Array.Empty<string>();
            CombatCgResourcePath = combatCgResourcePath ?? string.Empty;
        }

        public string MonsterId { get; }
        public string DisplayName { get; }
        public string CombatCgResourcePath { get; }
        public int MaxHp { get; }
        public int AttackPower { get; }
        public int XpReward { get; }
        public string EnemyActionName { get; }
        public int EnemyActionPower { get; }
        public string Species { get; }
        public string[] TraitIds { get; }
    }
}

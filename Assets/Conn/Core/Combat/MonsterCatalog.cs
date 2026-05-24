namespace Conn.Core.Combat
{
    public static class MonsterCatalog
    {
        public const string TestGuardId = "monster_test_guard";

        private static readonly MonsterDefinition[] Monsters =
        {
            new MonsterDefinition(TestGuardId, "Test Gate Guard", 12, 4, 5)
        };

        public static MonsterDefinition[] All => Monsters;

        public static MonsterDefinition Find(string monsterId)
        {
            for (var i = 0; i < Monsters.Length; i++)
            {
                if (Monsters[i].MonsterId == monsterId)
                {
                    return Monsters[i];
                }
            }

            return null;
        }
    }
}

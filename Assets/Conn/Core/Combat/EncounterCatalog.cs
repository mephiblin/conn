namespace Conn.Core.Combat
{
    public static class EncounterCatalog
    {
        public const string TestGuardId = "encounter_test_guard";

        private static readonly EncounterDefinition[] Encounters =
        {
            new EncounterDefinition(TestGuardId, "Test Guard Encounter", MonsterCatalog.TestGuardId, 5)
        };

        public static EncounterDefinition[] All => Encounters;

        public static EncounterDefinition Find(string encounterId)
        {
            for (var i = 0; i < Encounters.Length; i++)
            {
                if (Encounters[i].EncounterId == encounterId)
                {
                    return Encounters[i];
                }
            }

            return null;
        }

        public static EncounterDefinition FindForMonster(string monsterId)
        {
            for (var i = 0; i < Encounters.Length; i++)
            {
                if (Encounters[i].MonsterId == monsterId)
                {
                    return Encounters[i];
                }
            }

            return null;
        }
    }
}

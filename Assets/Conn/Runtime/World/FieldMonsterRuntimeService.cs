using Conn.Core.Session;
using Conn.Core.World;

namespace Conn.Runtime.World
{
    public static class FieldMonsterRuntimeService
    {
        public static FieldMonsterState Register(GameSessionState session, string stateKey, string placementId, string encounterId, string monsterId)
        {
            return session.World.GetOrCreateFieldMonster(stateKey, placementId, encounterId, monsterId);
        }

        public static bool IsDefeated(GameSessionState session, string stateKey)
        {
            var state = session.World.FindFieldMonster(stateKey);
            return state != null && state.Defeated;
        }

        public static void MarkCombatHandoff(GameSessionState session, string stateKey)
        {
            var state = session.World.FindFieldMonster(stateKey);
            if (state != null && !state.Defeated)
            {
                state.Status = FieldMonsterStatus.CombatHandoff;
            }
        }

        public static void MarkDefeated(GameSessionState session, string stateKey)
        {
            var state = session.World.FindFieldMonster(stateKey);
            if (state == null)
            {
                state = session.World.GetOrCreateFieldMonster(stateKey, stateKey, stateKey, session.Quest.TargetMonsterId);
            }

            state.Status = FieldMonsterStatus.Defeated;
            state.Defeated = true;
        }

        public static void ClearForNewQuest(GameSessionState session)
        {
            session.World.Clear();
        }
    }
}

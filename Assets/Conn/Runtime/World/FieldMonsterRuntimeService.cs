using Conn.Core.Session;
using Conn.Core.World;
using Conn.Runtime.Content;

namespace Conn.Runtime.World
{
    public static class FieldMonsterRuntimeService
    {
        public static FieldMonsterState Register(GameSessionState session, string stateKey, string placementId, string encounterId, string monsterId)
        {
            return session.World.GetOrCreateFieldMonster(
                stateKey,
                placementId,
                encounterId,
                monsterId,
                RuntimeContentDatabase.FindFieldMonsterAiProfile(monsterId));
        }

        public static FieldMonsterState RegisterAt(GameSessionState session, string stateKey, string placementId, string encounterId, string monsterId, int anchorX, int anchorY)
        {
            var state = Register(session, stateKey, placementId, encounterId, monsterId);
            state.SetAnchor(anchorX, anchorY);
            return state;
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

        public static bool TryMarkContact(GameSessionState session, string stateKey, float now)
        {
            var state = session.World.FindFieldMonster(stateKey);
            if (state == null || state.Defeated || !state.CanContact(now))
            {
                return false;
            }

            state.MarkContact(now);
            return true;
        }

        public static bool TryBeginCombatHandoff(GameSessionState session, string stateKey, float now)
        {
            if (FindCombatHandoff(session) != null)
            {
                return false;
            }

            if (!TryMarkContact(session, stateKey, now))
            {
                return false;
            }

            MarkCombatHandoff(session, stateKey);
            return FindCombatHandoff(session)?.StateKey == stateKey;
        }

        public static FieldMonsterState FindCombatHandoff(GameSessionState session)
        {
            for (var i = 0; i < session.World.FieldMonsters.Count; i++)
            {
                var state = session.World.FieldMonsters[i];
                if (state.Status == FieldMonsterStatus.CombatHandoff && !state.Defeated)
                {
                    return state;
                }
            }

            return null;
        }

        public static int CountActive(GameSessionState session)
        {
            var count = 0;
            for (var i = 0; i < session.World.FieldMonsters.Count; i++)
            {
                if (!session.World.FieldMonsters[i].Defeated)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountDefeated(GameSessionState session)
        {
            var count = 0;
            for (var i = 0; i < session.World.FieldMonsters.Count; i++)
            {
                if (session.World.FieldMonsters[i].Defeated)
                {
                    count++;
                }
            }

            return count;
        }

        public static string ExpeditionStatus(GameSessionState session)
        {
            var handoff = FindCombatHandoff(session);
            if (handoff != null)
            {
                return $"Combat handoff: {handoff.MonsterId}";
            }

            if (session.Quest.TargetDefeated)
            {
                return "Target defeated";
            }

            var active = CountActive(session);
            return active > 0 ? $"Field monsters: {active} active" : "Field monsters: none registered";
        }

        public static void MarkIdle(GameSessionState session, string stateKey)
        {
            var state = session.World.FindFieldMonster(stateKey);
            if (state != null && !state.Defeated)
            {
                state.Status = FieldMonsterStatus.Idle;
            }
        }

        public static void MarkPatrol(GameSessionState session, string stateKey)
        {
            var state = session.World.FindFieldMonster(stateKey);
            if (state != null && !state.Defeated)
            {
                state.Status = FieldMonsterStatus.Patrol;
            }
        }

        public static void MarkChase(GameSessionState session, string stateKey)
        {
            var state = session.World.FindFieldMonster(stateKey);
            if (state != null && !state.Defeated)
            {
                state.Status = FieldMonsterStatus.Chase;
            }
        }

        public static void MarkReturnToAnchor(GameSessionState session, string stateKey)
        {
            var state = session.World.FindFieldMonster(stateKey);
            if (state != null && !state.Defeated)
            {
                state.Status = FieldMonsterStatus.ReturnToAnchor;
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

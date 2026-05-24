using System.Collections.Generic;

namespace Conn.Core.World
{
    [System.Serializable]
    public sealed class WorldRuntimeState
    {
        public List<FieldMonsterState> FieldMonsters = new List<FieldMonsterState>();

        public FieldMonsterState GetOrCreateFieldMonster(string stateKey, string placementId, string encounterId, string monsterId)
        {
            for (var i = 0; i < FieldMonsters.Count; i++)
            {
                if (FieldMonsters[i].StateKey == stateKey)
                {
                    FieldMonsters[i].Setup(stateKey, placementId, encounterId, monsterId);
                    return FieldMonsters[i];
                }
            }

            var state = new FieldMonsterState();
            state.Setup(stateKey, placementId, encounterId, monsterId);
            FieldMonsters.Add(state);
            return state;
        }

        public FieldMonsterState FindFieldMonster(string stateKey)
        {
            for (var i = 0; i < FieldMonsters.Count; i++)
            {
                if (FieldMonsters[i].StateKey == stateKey)
                {
                    return FieldMonsters[i];
                }
            }

            return null;
        }

        public void Clear()
        {
            FieldMonsters.Clear();
        }
    }
}

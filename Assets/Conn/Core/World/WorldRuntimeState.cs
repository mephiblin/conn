using System.Collections.Generic;

namespace Conn.Core.World
{
    [System.Serializable]
    public sealed class WorldRuntimeState
    {
        public List<FieldMonsterState> FieldMonsters = new List<FieldMonsterState>();
        public List<DungeonObjectState> DungeonObjects = new List<DungeonObjectState>();

        public FieldMonsterState GetOrCreateFieldMonster(string stateKey, string placementId, string encounterId, string monsterId)
        {
            return GetOrCreateFieldMonster(stateKey, placementId, encounterId, monsterId, FieldMonsterAiProfile.Default());
        }

        public FieldMonsterState GetOrCreateFieldMonster(string stateKey, string placementId, string encounterId, string monsterId, FieldMonsterAiProfile aiProfile)
        {
            for (var i = 0; i < FieldMonsters.Count; i++)
            {
                if (FieldMonsters[i].StateKey == stateKey)
                {
                    FieldMonsters[i].Setup(stateKey, placementId, encounterId, monsterId, aiProfile);
                    return FieldMonsters[i];
                }
            }

            var state = new FieldMonsterState();
            state.Setup(stateKey, placementId, encounterId, monsterId, aiProfile);
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

        public DungeonObjectState GetOrCreateDungeonObject(string stateKey, string placementId)
        {
            for (var i = 0; i < DungeonObjects.Count; i++)
            {
                if (DungeonObjects[i].StateKey == stateKey)
                {
                    DungeonObjects[i].Setup(stateKey, placementId);
                    return DungeonObjects[i];
                }
            }

            var state = new DungeonObjectState();
            state.Setup(stateKey, placementId);
            DungeonObjects.Add(state);
            return state;
        }

        public DungeonObjectState FindDungeonObject(string stateKey)
        {
            for (var i = 0; i < DungeonObjects.Count; i++)
            {
                if (DungeonObjects[i].StateKey == stateKey)
                {
                    return DungeonObjects[i];
                }
            }

            return null;
        }

        public void Clear()
        {
            FieldMonsters.Clear();
            DungeonObjects.Clear();
        }
    }
}

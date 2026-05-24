using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class DungeonPlayerSpawnRestorer : MonoBehaviour
    {
        private void Start()
        {
            var session = GameSession.Instance.State;
            if (!session.Quest.HasActiveQuest || !session.PreEncounterSnapshot.Valid)
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }

            QuestRuntimeService.TryApplyPreEncounterSnapshot(session, player.transform);
        }
    }
}

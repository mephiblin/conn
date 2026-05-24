using Conn.Core.Scenes;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class FieldMonsterContact : MonoBehaviour
    {
        [SerializeField] private string monsterId = "monster_test_guard";

        private bool consumed;

        private void OnTriggerEnter(Collider other)
        {
            if (consumed || !other.CompareTag("Player"))
            {
                return;
            }

            var session = GameSession.Instance.State;
            if (!session.Quest.HasActiveQuest || session.Quest.TargetDefeated)
            {
                return;
            }

            var player = other.transform;
            session.PreEncounterSnapshot.Capture(
                player.position.x,
                player.position.y,
                player.position.z,
                player.eulerAngles.y);

            consumed = true;
            Debug.Log($"Field monster contact: {monsterId}");
            SceneFlowService.Load(GameSceneId.Combat);
        }
    }
}

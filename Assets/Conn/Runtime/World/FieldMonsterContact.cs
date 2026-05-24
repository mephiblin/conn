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
        private Collider contactCollider;

        private void Awake()
        {
            contactCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            ApplyDefeatedState();
        }

        private void OnTriggerEnter(Collider other)
        {
            ApplyDefeatedState();
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

        private void ApplyDefeatedState()
        {
            var session = GameSession.Instance.State;
            if (!session.Quest.TargetDefeated)
            {
                return;
            }

            consumed = true;
            if (contactCollider != null)
            {
                contactCollider.enabled = false;
            }

            var renderers = GetComponentsInChildren<Renderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }
    }
}

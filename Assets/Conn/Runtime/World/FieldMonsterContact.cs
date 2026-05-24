using Conn.Core.Scenes;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class FieldMonsterContact : MonoBehaviour
    {
        [SerializeField] private string stateKey = "field_monster_test_guard";
        [SerializeField] private string placementId = "placement_test_guard";
        [SerializeField] private string encounterId = "encounter_test_guard";
        [SerializeField] private string monsterId = "monster_test_guard";

        private bool consumed;
        private Collider contactCollider;

        private void Awake()
        {
            contactCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            var session = GameSession.Instance.State;
            FieldMonsterRuntimeService.Register(session, stateKey, placementId, encounterId, monsterId);
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

            QuestRuntimeService.CapturePreEncounter(session, other.transform);
            FieldMonsterRuntimeService.MarkCombatHandoff(session, stateKey);
            GameSession.Instance.SaveGame();

            consumed = true;
            if (contactCollider != null)
            {
                contactCollider.enabled = false;
            }

            Debug.Log($"Field monster contact: {monsterId}");
            SceneFlowService.Load(GameSceneId.Combat);
        }

        private void ApplyDefeatedState()
        {
            var session = GameSession.Instance.State;
            if (!FieldMonsterRuntimeService.IsDefeated(session, stateKey))
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

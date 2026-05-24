using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class QuestBoardInteractable : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private string questId = "quest_test_hunt";
        [SerializeField] private string targetMonsterId = "monster_test_guard";
        [SerializeField] private int goldReward = 10;

        public string Prompt
        {
            get
            {
                var quest = GameSession.Instance.State.Quest;
                return quest.HasActiveQuest ? "Quest already accepted" : "Accept test hunt";
            }
        }

        public bool CanInteract => !GameSession.Instance.State.Quest.HasActiveQuest;

        public void Interact()
        {
            var quest = GameSession.Instance.State.Quest;
            if (quest.HasActiveQuest)
            {
                Debug.Log("A quest is already active.");
                return;
            }

            QuestRuntimeService.AcceptQuest(GameSession.Instance.State, questId, targetMonsterId, goldReward);
            Debug.Log($"Accepted quest: {questId}");
        }
    }
}

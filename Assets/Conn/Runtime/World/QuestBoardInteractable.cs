using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class QuestBoardInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt
        {
            get
            {
                var quest = GameSession.Instance.State.Quest;
                if (quest.HasActiveQuest)
                {
                    return "View active quest";
                }

                var offer = QuestRuntimeService.CurrentBoardOffer(GameSession.Instance.State);
                return offer != null
                    ? $"View {offer.DisplayName} ({offer.GoldReward}g)"
                    : "No quest available";
            }
        }

        public bool CanInteract => true;

        public void Interact()
        {
            TownQuestBoardPanelState.Open();
        }
    }
}

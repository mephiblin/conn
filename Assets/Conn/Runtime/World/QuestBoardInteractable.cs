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
            var session = GameSession.Instance.State;
            TownNpcInteractionState.Open(TownNpcInteractionKind.QuestBoard, "Quest Board", DialogueFor(session));
        }

        private static string DialogueFor(Conn.Core.Session.GameSessionState session)
        {
            if (session.Quest.HasActiveQuest)
            {
                return string.IsNullOrWhiteSpace(session.Quest.ActiveQuestTitle)
                    ? "Review your active quest."
                    : $"Review {session.Quest.ActiveQuestTitle}.";
            }

            var offer = QuestRuntimeService.CurrentBoardOffer(session);
            return offer != null
                ? $"{offer.DisplayName}: reward {offer.GoldReward}g."
                : "No quest is available right now.";
        }
    }
}

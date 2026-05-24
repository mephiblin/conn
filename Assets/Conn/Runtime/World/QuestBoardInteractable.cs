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
                    return "Quest already accepted";
                }

                var offer = QuestRuntimeService.CurrentBoardOffer(GameSession.Instance.State);
                return offer != null
                    ? $"Accept {offer.DisplayName} ({offer.GoldReward}g)"
                    : "No quest available";
            }
        }

        public bool CanInteract => !GameSession.Instance.State.Quest.HasActiveQuest
            && QuestRuntimeService.CurrentBoardOffer(GameSession.Instance.State) != null;

        public void Interact()
        {
            var quest = GameSession.Instance.State.Quest;
            if (quest.HasActiveQuest)
            {
                RuntimeNoticeService.Set(GameSession.Instance.State, "A quest is already active.");
                return;
            }

            var offer = QuestRuntimeService.CurrentBoardOffer(GameSession.Instance.State);
            QuestRuntimeService.AcceptCurrentBoardOffer(GameSession.Instance.State);
            RuntimeNoticeService.Set(GameSession.Instance.State, $"Accepted quest: {offer?.QuestId}");
        }
    }
}

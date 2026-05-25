using UnityEngine;
using Conn.Runtime.Session;

namespace Conn.Runtime.World
{
    public sealed class BlacksmithInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt => "Open Blacksmith";

        public bool CanInteract => true;

        public void Interact()
        {
            var session = GameSession.Instance.State;
            var dialogue = ChapterOneUxText.BlacksmithOpenNotice(session);
            TownNpcInteractionState.Open(TownNpcInteractionKind.Blacksmith, "Blacksmith", dialogue);
            RuntimeNoticeService.Set(session, dialogue);
        }
    }
}

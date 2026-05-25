using UnityEngine;
using Conn.Runtime.Session;

namespace Conn.Runtime.World
{
    public sealed class SkillMerchantInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt => "Open Skill Merchant";

        public bool CanInteract => true;

        public void Interact()
        {
            var session = GameSession.Instance.State;
            var dialogue = ChapterOneUxText.SkillMerchantOpenNotice(session);
            TownNpcInteractionState.Open(TownNpcInteractionKind.SkillMerchant, "Skill Merchant", dialogue);
            RuntimeNoticeService.Set(session, dialogue);
        }
    }
}

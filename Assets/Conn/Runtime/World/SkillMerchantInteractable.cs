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
            TownShopPanelState.Open(TownShopPanelKind.SkillMerchant);
            RuntimeNoticeService.Set(GameSession.Instance.State, ChapterOneUxText.SkillMerchantOpenNotice(GameSession.Instance.State));
        }
    }
}

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
            TownShopPanelState.Open(TownShopPanelKind.Blacksmith);
            RuntimeNoticeService.Set(GameSession.Instance.State, ChapterOneUxText.BlacksmithOpenNotice(GameSession.Instance.State));
        }
    }
}

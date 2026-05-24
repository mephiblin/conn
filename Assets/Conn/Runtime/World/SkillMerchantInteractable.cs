using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class SkillMerchantInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt => "Open Skill Merchant";

        public bool CanInteract => true;

        public void Interact()
        {
            TownShopPanelState.Open(TownShopPanelKind.SkillMerchant);
        }
    }
}

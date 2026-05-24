using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class BlacksmithInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt => "Open Blacksmith";

        public bool CanInteract => true;

        public void Interact()
        {
            TownShopPanelState.Open(TownShopPanelKind.Blacksmith);
        }
    }
}

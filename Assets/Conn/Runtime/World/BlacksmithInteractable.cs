using Conn.Core.Equipment;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class BlacksmithInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt
        {
            get
            {
                var session = GameSession.Instance.State;
                var nextOffer = GetNextOfferId(session.Inventory.HasItem(EquipmentCatalog.IronShieldId), session.Inventory.HasItem(EquipmentCatalog.GreatAxeId));
                if (!string.IsNullOrEmpty(nextOffer))
                {
                    var item = EquipmentCatalog.Find(nextOffer);
                    return $"Buy {item.DisplayName} ({item.BuyPrice}g)";
                }

                return "Switch equipped weapon";
            }
        }

        public bool CanInteract => true;

        public void Interact()
        {
            var session = GameSession.Instance.State;
            var nextOffer = GetNextOfferId(session.Inventory.HasItem(EquipmentCatalog.IronShieldId), session.Inventory.HasItem(EquipmentCatalog.GreatAxeId));
            if (!string.IsNullOrEmpty(nextOffer))
            {
                BuyAndEquip(nextOffer);
                return;
            }

            ToggleOwnedLoadout();
        }

        private static string GetNextOfferId(bool hasShield, bool hasGreatAxe)
        {
            if (!hasShield)
            {
                return EquipmentCatalog.IronShieldId;
            }

            return hasGreatAxe ? string.Empty : EquipmentCatalog.GreatAxeId;
        }

        private static void BuyAndEquip(string itemId)
        {
            var session = GameSession.Instance.State;
            var item = EquipmentCatalog.Find(itemId);
            if (item == null)
            {
                return;
            }

            if (session.Gold < item.BuyPrice)
            {
                Debug.Log($"Not enough gold for {item.DisplayName}.");
                return;
            }

            session.Gold -= item.BuyPrice;
            session.Inventory.AddItem(itemId);
            session.Equipment.Equip(itemId);
            Debug.Log($"Bought and equipped {item.DisplayName}.");
        }

        private static void ToggleOwnedLoadout()
        {
            var session = GameSession.Instance.State;
            if (session.Equipment.WeaponGrip == WeaponGrip.TwoHand && session.Inventory.HasItem(EquipmentCatalog.RustySwordId))
            {
                session.Equipment.Equip(EquipmentCatalog.RustySwordId);
                if (session.Inventory.HasItem(EquipmentCatalog.IronShieldId))
                {
                    session.Equipment.Equip(EquipmentCatalog.IronShieldId);
                }
                return;
            }

            if (session.Inventory.HasItem(EquipmentCatalog.GreatAxeId))
            {
                session.Equipment.Equip(EquipmentCatalog.GreatAxeId);
            }
        }
    }
}

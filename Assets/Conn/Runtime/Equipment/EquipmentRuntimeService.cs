using Conn.Core.Equipment;
using Conn.Core.Session;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Equipment
{
    public static class EquipmentRuntimeService
    {
        public static bool TryEquip(GameSessionState session, string itemId)
        {
            if (!session.Inventory.HasItem(itemId) || EquipmentCatalog.Find(itemId) == null)
            {
                return false;
            }

            session.Equipment.Equip(itemId);
            SaveIfPlaying();
            return true;
        }

        public static bool ToggleOwnedLoadout(GameSessionState session)
        {
            if (session.Equipment.WeaponGrip == WeaponGrip.TwoHand && session.Inventory.HasItem(EquipmentCatalog.RustySwordId))
            {
                session.Equipment.Equip(EquipmentCatalog.RustySwordId);
                if (session.Inventory.HasItem(EquipmentCatalog.IronShieldId))
                {
                    session.Equipment.Equip(EquipmentCatalog.IronShieldId);
                }

                SaveIfPlaying();
                Debug.Log("Switched to one-hand loadout.");
                return true;
            }

            if (session.Inventory.HasItem(EquipmentCatalog.GreatAxeId))
            {
                session.Equipment.Equip(EquipmentCatalog.GreatAxeId);
                SaveIfPlaying();
                Debug.Log("Switched to two-hand loadout.");
                return true;
            }

            if (session.Inventory.HasItem(EquipmentCatalog.IronShieldId) && session.Equipment.WeaponGrip == WeaponGrip.OneHand)
            {
                session.Equipment.Equip(EquipmentCatalog.IronShieldId);
                SaveIfPlaying();
                Debug.Log("Equipped shield loadout.");
                return true;
            }

            return false;
        }

        private static void SaveIfPlaying()
        {
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
            }
        }
    }
}

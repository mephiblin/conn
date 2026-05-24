using Conn.Core.Equipment;
using Conn.Core.Session;
using Conn.Runtime.Content;
using Conn.Runtime.Session;
using Conn.Runtime.Skills;
using UnityEngine;

namespace Conn.Runtime.Equipment
{
    public static class EquipmentRuntimeService
    {
        public static bool TryEquip(GameSessionState session, string itemId)
        {
            var item = RuntimeContentDatabase.FindEquipment(itemId);
            if (!session.Inventory.HasItem(itemId) || item == null)
            {
                return false;
            }

            if (item.Kind == EquipmentKind.Shield && session.Equipment.WeaponGrip == WeaponGrip.TwoHand)
            {
                if (!session.Inventory.HasItem(EquipmentCatalog.RustySwordId))
                {
                    return false;
                }

                session.Equipment.Equip(EquipmentCatalog.RustySwordId);
            }

            session.Equipment.Equip(itemId);
            session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Equipped {item.DisplayName}.");
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

                session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
                SaveIfPlaying();
                RuntimeNoticeService.Set(session, "Switched to one-hand loadout.");
                return true;
            }

            if (session.Inventory.HasItem(EquipmentCatalog.GreatAxeId))
            {
                session.Equipment.Equip(EquipmentCatalog.GreatAxeId);
                session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
                SaveIfPlaying();
                RuntimeNoticeService.Set(session, "Switched to two-hand loadout.");
                return true;
            }

            if (session.Inventory.HasItem(EquipmentCatalog.IronShieldId) && session.Equipment.WeaponGrip == WeaponGrip.OneHand)
            {
                session.Equipment.Equip(EquipmentCatalog.IronShieldId);
                session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
                SaveIfPlaying();
                RuntimeNoticeService.Set(session, "Equipped shield loadout.");
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

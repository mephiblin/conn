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
                var oneHandWeaponId = FirstOwnedOneHandWeaponId(session);
                if (string.IsNullOrWhiteSpace(oneHandWeaponId))
                {
                    return false;
                }

                session.Equipment.Equip(oneHandWeaponId);
            }

            session.Equipment.Equip(itemId);
            session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Equipped {item.DisplayName}.");
            return true;
        }

        public static bool ToggleOwnedLoadout(GameSessionState session)
        {
            var oneHandWeaponId = FirstOwnedOneHandWeaponId(session);
            if (session.Equipment.WeaponGrip == WeaponGrip.TwoHand && !string.IsNullOrWhiteSpace(oneHandWeaponId))
            {
                session.Equipment.Equip(oneHandWeaponId);
                var shieldId = FirstOwnedEquipmentIdByKind(session, EquipmentKind.Shield);
                if (!string.IsNullOrWhiteSpace(shieldId))
                {
                    session.Equipment.Equip(shieldId);
                }

                session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
                SaveIfPlaying();
                RuntimeNoticeService.Set(session, "Switched to one-hand loadout.");
                return true;
            }

            var twoHandWeaponId = FirstOwnedEquipmentIdByKind(session, EquipmentKind.TwoHandWeapon);
            if (!string.IsNullOrWhiteSpace(twoHandWeaponId))
            {
                session.Equipment.Equip(twoHandWeaponId);
                session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
                SaveIfPlaying();
                RuntimeNoticeService.Set(session, "Switched to two-hand loadout.");
                return true;
            }

            var ownedShieldId = FirstOwnedEquipmentIdByKind(session, EquipmentKind.Shield);
            if (!string.IsNullOrWhiteSpace(ownedShieldId) && session.Equipment.WeaponGrip == WeaponGrip.OneHand)
            {
                session.Equipment.Equip(ownedShieldId);
                session.Skills.ResizeEquippedFaces(session.Equipment.DiceCount);
                SaveIfPlaying();
                RuntimeNoticeService.Set(session, "Equipped shield loadout.");
                return true;
            }

            return false;
        }

        private static string FirstOwnedOneHandWeaponId(GameSessionState session)
        {
            var starterId = GameSessionState.StarterEquipmentIdResolver();
            var starter = RuntimeContentDatabase.FindEquipment(starterId);
            if (starter != null && starter.Kind == EquipmentKind.OneHandWeapon && session.Inventory.HasItem(starterId))
            {
                return starterId;
            }

            return FirstOwnedEquipmentIdByKind(session, EquipmentKind.OneHandWeapon);
        }

        private static string FirstOwnedEquipmentIdByKind(GameSessionState session, EquipmentKind kind)
        {
            for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
            {
                var itemId = session.Inventory.ItemIds[i];
                var item = RuntimeContentDatabase.FindEquipment(itemId);
                if (item != null && item.Kind == kind)
                {
                    return itemId;
                }
            }

            return string.Empty;
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

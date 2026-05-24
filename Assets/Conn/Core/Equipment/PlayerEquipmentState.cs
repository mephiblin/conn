namespace Conn.Core.Equipment
{
    using System;

    [System.Serializable]
    public sealed class PlayerEquipmentState
    {
        public static Func<string, EquipmentItemDefinition> EquipmentResolver = EquipmentCatalog.Find;

        public string EquippedWeaponId = EquipmentCatalog.RustySwordId;
        public string EquippedShieldId = string.Empty;
        public string EquippedHeadId = string.Empty;
        public string EquippedChestId = string.Empty;
        public string EquippedArmsId = string.Empty;
        public string EquippedLegsId = string.Empty;
        public string EquippedFeetId = string.Empty;

        public WeaponGrip WeaponGrip
        {
            get
            {
                var weapon = ResolveEquipment(EquippedWeaponId);
                if (weapon == null)
                {
                    return WeaponGrip.None;
                }

                if (weapon.Kind == EquipmentKind.TwoHandWeapon)
                {
                    return WeaponGrip.TwoHand;
                }

                return string.IsNullOrWhiteSpace(EquippedShieldId)
                    ? WeaponGrip.OneHand
                    : WeaponGrip.OneHandAndShield;
            }
        }

        public int DiceCount
        {
            get
            {
                return WeaponGrip switch
                {
                    WeaponGrip.OneHand => 4,
                    WeaponGrip.OneHandAndShield => 3,
                    WeaponGrip.TwoHand => 5,
                    _ => 2
                };
            }
        }

        public int ArmorValue =>
            ArmorValueFor(EquippedHeadId)
            + ArmorValueFor(EquippedChestId)
            + ArmorValueFor(EquippedArmsId)
            + ArmorValueFor(EquippedLegsId)
            + ArmorValueFor(EquippedFeetId);

        public int DefenseBonus => (WeaponGrip == WeaponGrip.OneHandAndShield ? 1 : 0) + ArmorValue;

        public bool IsEquipped(string itemId)
        {
            return EquippedWeaponId == itemId
                || EquippedShieldId == itemId
                || EquippedHeadId == itemId
                || EquippedChestId == itemId
                || EquippedArmsId == itemId
                || EquippedLegsId == itemId
                || EquippedFeetId == itemId;
        }

        public void Equip(string itemId)
        {
            var item = ResolveEquipment(itemId);
            if (item == null)
            {
                return;
            }

            if (item.Kind == EquipmentKind.Shield)
            {
                EquippedShieldId = itemId;
                return;
            }

            if (item.Kind == EquipmentKind.HeadArmor)
            {
                EquippedHeadId = itemId;
                return;
            }

            if (item.Kind == EquipmentKind.ChestArmor)
            {
                EquippedChestId = itemId;
                return;
            }

            if (item.Kind == EquipmentKind.ArmsArmor)
            {
                EquippedArmsId = itemId;
                return;
            }

            if (item.Kind == EquipmentKind.LegsArmor)
            {
                EquippedLegsId = itemId;
                return;
            }

            if (item.Kind == EquipmentKind.FeetArmor)
            {
                EquippedFeetId = itemId;
                return;
            }

            EquippedWeaponId = itemId;
            if (item.Kind == EquipmentKind.TwoHandWeapon)
            {
                EquippedShieldId = string.Empty;
            }
        }

        public string EquippedItemIdForKind(EquipmentKind kind)
        {
            return kind switch
            {
                EquipmentKind.Shield => EquippedShieldId,
                EquipmentKind.HeadArmor => EquippedHeadId,
                EquipmentKind.ChestArmor => EquippedChestId,
                EquipmentKind.ArmsArmor => EquippedArmsId,
                EquipmentKind.LegsArmor => EquippedLegsId,
                EquipmentKind.FeetArmor => EquippedFeetId,
                _ => EquippedWeaponId
            };
        }

        public string ComparisonLineFor(string candidateItemId)
        {
            var candidate = ResolveEquipment(candidateItemId);
            if (candidate == null)
            {
                return "Unknown equipment";
            }

            var current = ResolveEquipment(EquippedItemIdForKind(candidate.Kind));
            var currentName = current != null ? current.DisplayName : "None";
            var defenseDelta = DefenseDeltaFor(candidate);
            var diceDelta = DiceDeltaFor(candidate);
            var detail = string.Empty;
            if (defenseDelta != 0)
            {
                detail = $" Defense {FormatDelta(defenseDelta)}";
            }
            else if (diceDelta != 0)
            {
                detail = $" Dice {FormatDelta(diceDelta)}";
            }

            return $"{SlotLabelFor(candidate.Kind)}: {currentName} -> {candidate.DisplayName}{detail}";
        }

        public static string SlotLabelFor(EquipmentKind kind)
        {
            return kind switch
            {
                EquipmentKind.OneHandWeapon => "Weapon",
                EquipmentKind.TwoHandWeapon => "Weapon",
                EquipmentKind.Shield => "Shield",
                EquipmentKind.HeadArmor => "Head",
                EquipmentKind.ChestArmor => "Chest",
                EquipmentKind.ArmsArmor => "Arms",
                EquipmentKind.LegsArmor => "Legs",
                EquipmentKind.FeetArmor => "Feet",
                _ => "Equipment"
            };
        }

        private int DiceDeltaFor(EquipmentItemDefinition candidate)
        {
            var currentDice = DiceCount;
            var preview = new PlayerEquipmentState
            {
                EquippedWeaponId = EquippedWeaponId,
                EquippedShieldId = EquippedShieldId,
                EquippedHeadId = EquippedHeadId,
                EquippedChestId = EquippedChestId,
                EquippedArmsId = EquippedArmsId,
                EquippedLegsId = EquippedLegsId,
                EquippedFeetId = EquippedFeetId
            };
            preview.Equip(candidate.ItemId);
            return preview.DiceCount - currentDice;
        }

        private int DefenseDeltaFor(EquipmentItemDefinition candidate)
        {
            var currentDefense = DefenseBonus;
            var preview = new PlayerEquipmentState
            {
                EquippedWeaponId = EquippedWeaponId,
                EquippedShieldId = EquippedShieldId,
                EquippedHeadId = EquippedHeadId,
                EquippedChestId = EquippedChestId,
                EquippedArmsId = EquippedArmsId,
                EquippedLegsId = EquippedLegsId,
                EquippedFeetId = EquippedFeetId
            };
            preview.Equip(candidate.ItemId);
            return preview.DefenseBonus - currentDefense;
        }

        private static int ArmorValueFor(string itemId)
        {
            var item = ResolveEquipment(itemId);
            return item != null ? item.ArmorValue : 0;
        }

        private static EquipmentItemDefinition ResolveEquipment(string itemId)
        {
            return EquipmentResolver != null ? EquipmentResolver(itemId) : EquipmentCatalog.Find(itemId);
        }

        private static string FormatDelta(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }
}

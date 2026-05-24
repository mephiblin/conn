namespace Conn.Core.Equipment
{
    [System.Serializable]
    public sealed class PlayerEquipmentState
    {
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
                var weapon = EquipmentCatalog.Find(EquippedWeaponId);
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

        public int DefenseBonus => WeaponGrip == WeaponGrip.OneHandAndShield ? 1 : 0;

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
            var item = EquipmentCatalog.Find(itemId);
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
    }
}

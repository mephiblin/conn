namespace Conn.Core.Equipment
{
    [System.Serializable]
    public sealed class PlayerEquipmentState
    {
        public string EquippedWeaponId = EquipmentCatalog.RustySwordId;
        public string EquippedShieldId = string.Empty;

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
            return EquippedWeaponId == itemId || EquippedShieldId == itemId;
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

            EquippedWeaponId = itemId;
            if (item.Kind == EquipmentKind.TwoHandWeapon)
            {
                EquippedShieldId = string.Empty;
            }
        }
    }
}

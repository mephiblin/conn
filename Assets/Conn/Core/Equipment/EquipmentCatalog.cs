namespace Conn.Core.Equipment
{
    public static class EquipmentCatalog
    {
        public const string RustySwordId = "rusty_sword";
        public const string IronShieldId = "iron_shield";
        public const string GreatAxeId = "great_axe";

        private static readonly EquipmentItemDefinition[] Items =
        {
            new EquipmentItemDefinition(RustySwordId, "Rusty Sword", EquipmentKind.OneHandWeapon, 0, 2),
            new EquipmentItemDefinition(IronShieldId, "Iron Shield", EquipmentKind.Shield, 6, 3),
            new EquipmentItemDefinition(GreatAxeId, "Great Axe", EquipmentKind.TwoHandWeapon, 12, 6)
        };

        public static EquipmentItemDefinition[] All => Items;

        public static EquipmentItemDefinition Find(string itemId)
        {
            for (var i = 0; i < Items.Length; i++)
            {
                if (Items[i].ItemId == itemId)
                {
                    return Items[i];
                }
            }

            return null;
        }
    }
}

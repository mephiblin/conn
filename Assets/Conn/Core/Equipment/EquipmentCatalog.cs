namespace Conn.Core.Equipment
{
    public static class EquipmentCatalog
    {
        public const string RustySwordId = "rusty_sword";
        public const string IronShieldId = "iron_shield";
        public const string GreatAxeId = "great_axe";
        public const string LeatherCapId = "leather_cap";
        public const string PaddedVestId = "padded_vest";
        public const string TravelerGlovesId = "traveler_gloves";
        public const string ReinforcedPantsId = "reinforced_pants";
        public const string WornBootsId = "worn_boots";

        private static readonly EquipmentItemDefinition[] Items =
        {
            new EquipmentItemDefinition(RustySwordId, "Rusty Sword", EquipmentKind.OneHandWeapon, 0, 2),
            new EquipmentItemDefinition(IronShieldId, "Iron Shield", EquipmentKind.Shield, 6, 3),
            new EquipmentItemDefinition(GreatAxeId, "Great Axe", EquipmentKind.TwoHandWeapon, 12, 6),
            new EquipmentItemDefinition(LeatherCapId, "Leather Cap", EquipmentKind.HeadArmor, 4, 2, 1),
            new EquipmentItemDefinition(PaddedVestId, "Padded Vest", EquipmentKind.ChestArmor, 8, 4, 2),
            new EquipmentItemDefinition(TravelerGlovesId, "Traveler Gloves", EquipmentKind.ArmsArmor, 5, 2, 1),
            new EquipmentItemDefinition(ReinforcedPantsId, "Reinforced Pants", EquipmentKind.LegsArmor, 7, 3, 1),
            new EquipmentItemDefinition(WornBootsId, "Worn Boots", EquipmentKind.FeetArmor, 5, 2, 1)
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

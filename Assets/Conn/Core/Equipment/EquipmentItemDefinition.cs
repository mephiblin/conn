namespace Conn.Core.Equipment
{
    public sealed class EquipmentItemDefinition
    {
        public EquipmentItemDefinition(string itemId, string displayName, EquipmentKind kind, int buyPrice, int sellPrice, int armorValue = 0)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Kind = kind;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            ArmorValue = armorValue;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public EquipmentKind Kind { get; }
        public int BuyPrice { get; }
        public int SellPrice { get; }
        public int ArmorValue { get; }
    }
}

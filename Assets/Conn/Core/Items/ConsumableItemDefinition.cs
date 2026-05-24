namespace Conn.Core.Items
{
    public sealed class ConsumableItemDefinition
    {
        public ConsumableItemDefinition(string itemId, string displayName, int buyPrice, int sellPrice, int healAmount)
        {
            ItemId = itemId;
            DisplayName = displayName;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            HealAmount = healAmount;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public int BuyPrice { get; }
        public int SellPrice { get; }
        public int HealAmount { get; }
    }
}

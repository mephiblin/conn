namespace Conn.Core.Items
{
    public static class ConsumableCatalog
    {
        public const string MinorPotionId = "minor_potion";

        private static readonly ConsumableItemDefinition[] Items =
        {
            new ConsumableItemDefinition(MinorPotionId, "Minor Potion", 4, 2, 6)
        };

        public static ConsumableItemDefinition Find(string itemId)
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

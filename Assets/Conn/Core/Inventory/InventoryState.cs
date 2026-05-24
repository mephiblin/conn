using System.Collections.Generic;

namespace Conn.Core.Inventory
{
    [System.Serializable]
    public sealed class InventoryState
    {
        public List<string> ItemIds = new List<string>();

        public bool HasItem(string itemId)
        {
            return ItemIds.Contains(itemId);
        }

        public void AddItem(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                ItemIds.Add(itemId);
            }
        }

        public bool RemoveItem(string itemId)
        {
            return ItemIds.Remove(itemId);
        }

        public void Clear()
        {
            ItemIds.Clear();
        }
    }
}

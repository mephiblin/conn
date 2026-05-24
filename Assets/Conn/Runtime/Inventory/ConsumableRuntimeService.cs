using Conn.Core.Items;
using Conn.Core.Session;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Inventory
{
    public static class ConsumableRuntimeService
    {
        public static int Count(GameSessionState session, string itemId)
        {
            var count = 0;
            for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
            {
                if (session.Inventory.ItemIds[i] == itemId)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool Buy(GameSessionState session, string itemId)
        {
            var item = ConsumableCatalog.Find(itemId);
            if (item == null)
            {
                return false;
            }

            if (session.Gold < item.BuyPrice)
            {
                Debug.Log($"Not enough gold for {item.DisplayName}.");
                return false;
            }

            session.Gold -= item.BuyPrice;
            session.Inventory.AddItem(item.ItemId);
            SaveIfPlaying();
            Debug.Log($"Bought {item.DisplayName}.");
            return true;
        }

        public static bool Use(GameSessionState session, string itemId)
        {
            var item = ConsumableCatalog.Find(itemId);
            if (item == null || !session.Inventory.RemoveItem(itemId))
            {
                return false;
            }

            session.Player.Heal(item.HealAmount);
            if (session.Combat.Active)
            {
                session.Combat.Player.Hp = session.Player.Hp;
            }

            SaveIfPlaying();
            Debug.Log($"Used {item.DisplayName}.");
            return true;
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

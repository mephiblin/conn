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
                RuntimeNoticeService.Set(session, $"Apothecary: not enough gold for {item.DisplayName}. Need {item.BuyPrice}g.");
                return false;
            }

            session.Gold -= item.BuyPrice;
            session.Inventory.AddItem(item.ItemId);
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Apothecary: bought {item.DisplayName}. Owned x{Count(session, item.ItemId)}.");
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
            RuntimeNoticeService.Set(session, $"Used {item.DisplayName}. HP {session.Player.Hp}/{session.Player.MaxHp}.");
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

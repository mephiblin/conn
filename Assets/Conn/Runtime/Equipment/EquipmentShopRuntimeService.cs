using Conn.Core.Equipment;
using Conn.Core.Session;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Equipment
{
    public static class EquipmentShopRuntimeService
    {
        public static bool CanBuy(GameSessionState session, string itemId)
        {
            var item = EquipmentCatalog.Find(itemId);
            return item != null && item.BuyPrice > 0 && !session.Inventory.HasItem(itemId) && session.Gold >= item.BuyPrice;
        }

        public static bool BuyAndEquip(GameSessionState session, string itemId)
        {
            var item = EquipmentCatalog.Find(itemId);
            if (item == null || item.BuyPrice <= 0)
            {
                return false;
            }

            if (session.Inventory.HasItem(itemId))
            {
                RuntimeNoticeService.Set(session, $"Already own {item.DisplayName}.");
                return false;
            }

            if (session.Gold < item.BuyPrice)
            {
                RuntimeNoticeService.Set(session, $"Not enough gold for {item.DisplayName}.");
                return false;
            }

            session.Gold -= item.BuyPrice;
            session.Inventory.AddItem(itemId);
            EquipmentRuntimeService.TryEquip(session, itemId);
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Bought and equipped {item.DisplayName}.");
            return true;
        }

        public static bool CanSell(GameSessionState session, string itemId)
        {
            var item = EquipmentCatalog.Find(itemId);
            return item != null
                && itemId != EquipmentCatalog.RustySwordId
                && session.Inventory.HasItem(itemId)
                && !session.Equipment.IsEquipped(itemId);
        }

        public static bool Sell(GameSessionState session, string itemId)
        {
            var item = EquipmentCatalog.Find(itemId);
            if (item == null || !CanSell(session, itemId) || !session.Inventory.RemoveItem(itemId))
            {
                RuntimeNoticeService.Set(session, "Cannot sell equipped or required equipment.");
                return false;
            }

            session.Gold += item.SellPrice;
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Sold {item.DisplayName}.");
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

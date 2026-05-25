using Conn.Core.Session;
using Conn.Runtime.Content;
using Conn.Runtime.Session;
using System.Collections.Generic;
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

        public static string[] OwnedConsumableIds(GameSessionState session)
        {
            var ids = new List<string>();
            for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
            {
                var itemId = session.Inventory.ItemIds[i];
                if (ids.Contains(itemId) || RuntimeContentDatabase.FindConsumable(itemId) == null)
                {
                    continue;
                }

                ids.Add(itemId);
            }

            return ids.ToArray();
        }

        public static string FirstOwnedConsumableId(GameSessionState session)
        {
            var ids = OwnedConsumableIds(session);
            return ids.Length > 0 ? ids[0] : string.Empty;
        }

        public static bool Buy(GameSessionState session, string itemId)
        {
            var item = RuntimeContentDatabase.FindConsumable(itemId);
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
            var item = RuntimeContentDatabase.FindConsumable(itemId);
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

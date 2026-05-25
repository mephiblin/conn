using Conn.Core.Session;
using Conn.Core.Items;
using Conn.Runtime.Content;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public static class TownServiceRuntimeService
    {
        public static bool Rest(GameSessionState session, int cost)
        {
            if (!Pay(session, cost, "Inn"))
            {
                return false;
            }

            session.Player.HealToFull();
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Inn: rested. HP restored to {session.Player.Hp}/{session.Player.MaxHp}.");
            return true;
        }

        public static bool Train(GameSessionState session, int xpCost)
        {
            if (!session.Player.SpendXp(xpCost))
            {
                RuntimeNoticeService.Set(session, $"Trainer: not enough XP. Need {xpCost} XP.");
                return false;
            }

            session.Player.MaxHp += 2;
            session.Player.HealToFull();
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Trainer: Max HP increased to {session.Player.MaxHp}.");
            return true;
        }

        public static string ScholarHint(GameSessionState session)
        {
            if (session.Quest.HasActiveQuest)
            {
                return $"Scholar: current target is {session.Quest.TargetMonsterId}.";
            }

            var offer = RuntimeContentDatabase.BoardQuestAt(session.Quest.BoardOfferIndex);
            return offer != null
                ? $"Scholar: board offer is {offer.DisplayName}."
                : "Scholar: no quest offer is available.";
        }

        public static int CostFor(TownServiceKind serviceKind, int fallbackCost, int floor = 1, int bossesDefeated = 0)
        {
            var vendorId = VendorIdFor(serviceKind);
            var rotation = RuntimeContentDatabase.SelectVendorRotation(vendorId, floor, bossesDefeated);
            if (rotation != null && rotation.GoldCost > 0)
            {
                return rotation.GoldCost;
            }

            var vendor = RuntimeContentDatabase.FindVendor(vendorId);
            return vendor != null && vendor.GoldCost > 0 ? vendor.GoldCost : fallbackCost;
        }

        public static string FirstConsumableStockIdFor(TownServiceKind serviceKind, int floor = 1, int bossesDefeated = 0)
        {
            var vendorId = VendorIdFor(serviceKind);
            var consumableIds = RuntimeContentDatabase.ConsumableIdsForVendor(vendorId, floor, bossesDefeated);
            return consumableIds.Length > 0 ? consumableIds[0] : ConsumableCatalog.MinorPotionId;
        }

        public static string VendorIdFor(TownServiceKind serviceKind)
        {
            return serviceKind switch
            {
                TownServiceKind.Inn => "vendor_inn",
                TownServiceKind.Trainer => "vendor_trainer",
                TownServiceKind.Apothecary => "vendor_apothecary",
                _ => string.Empty
            };
        }

        private static bool Pay(GameSessionState session, int cost, string serviceName)
        {
            if (cost > 0 && session.Gold < cost)
            {
                RuntimeNoticeService.Set(session, $"{serviceName}: not enough gold.");
                return false;
            }

            session.Gold -= cost;
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

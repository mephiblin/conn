using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Session;
using Conn.Runtime.Content;
using Conn.Runtime.Equipment;
using Conn.Runtime.Inventory;
using Conn.Runtime.Skills;

namespace Conn.Runtime.World
{
    public static class ChapterOneUxText
    {
        public static string EquipmentStatus(GameSessionState session, string itemId)
        {
            var item = RuntimeContentDatabase.FindEquipment(itemId);
            if (item == null)
            {
                return "Unknown equipment";
            }

            var equipped = session.Equipment.IsEquipped(itemId);
            var sellStatus = EquipmentSellStatus(session, itemId);
            return $"{PlayerEquipmentState.SlotLabelFor(item.Kind)} | {item.DisplayName} | {(equipped ? "Equipped" : "In bag")} | {sellStatus}";
        }

        public static string EquipmentBuyStatus(GameSessionState session, string itemId)
        {
            var item = RuntimeContentDatabase.FindEquipment(itemId);
            if (item == null)
            {
                return "Unknown equipment";
            }

            if (session.Inventory.HasItem(itemId))
            {
                return $"Owned | {item.DisplayName}";
            }

            if (session.Gold < item.BuyPrice)
            {
                return $"Need {item.BuyPrice - session.Gold}g more | {item.DisplayName} ({item.BuyPrice}g)";
            }

            return $"Buyable | {item.DisplayName} ({item.BuyPrice}g)";
        }

        public static string SkillStatus(GameSessionState session, string skillId)
        {
            var skill = RuntimeContentDatabase.FindSkill(skillId);
            if (skill == null)
            {
                return "Unknown skill";
            }

            var owned = session.Skills.CountOwned(skillId);
            var equipped = session.Skills.CountEquipped(skillId);
            var loose = owned - equipped;
            var sellStatus = loose > 0 ? $"Sellable loose x{loose} ({skill.SellPrice}g)" : "No loose copy";
            return $"{skill.DisplayName} | Owned x{owned} | Equipped x{equipped} | {sellStatus}";
        }

        public static string SkillBuyStatus(GameSessionState session, string skillId)
        {
            var skill = RuntimeContentDatabase.FindSkill(skillId);
            if (skill == null)
            {
                return "Unknown skill";
            }

            if (session.Gold < skill.BuyPrice)
            {
                return $"Need {skill.BuyPrice - session.Gold}g more | {skill.DisplayName} ({skill.BuyPrice}g)";
            }

            return $"Buyable | {skill.DisplayName} ({skill.BuyPrice}g)";
        }

        public static string ConsumableStatus(GameSessionState session, string itemId)
        {
            var item = RuntimeContentDatabase.FindConsumable(itemId);
            if (item == null)
            {
                return "Unknown consumable";
            }

            var count = ConsumableRuntimeService.Count(session, itemId);
            return $"{item.DisplayName} | Owned x{count} | Use: heal {item.HealAmount} HP";
        }

        public static string BlacksmithOpenNotice(GameSessionState session)
        {
            return $"Blacksmith: equipment shop open. Gold {session.Gold}g. Equipped items cannot be sold.";
        }

        public static string SkillMerchantOpenNotice(GameSessionState session)
        {
            var stock = SkillShopRuntimeService.SkillMerchantStock(session);
            return $"Skill Merchant: stock refresh #{session.Shop.SkillMerchantRefreshIndex}. Stock: {JoinSkillNames(stock)}.";
        }

        public static string SkillMerchantRefreshNotice(GameSessionState session)
        {
            var stock = SkillShopRuntimeService.SkillMerchantStock(session);
            return $"Skill Merchant: refreshed to #{session.Shop.SkillMerchantRefreshIndex}. Stock: {JoinSkillNames(stock)}.";
        }

        public static string GateBlockedNotice()
        {
            return "Gate: blocked. Accept a quest at the board before entering the dungeon.";
        }

        public static string GateAllowedNotice(GameSessionState session)
        {
            var title = string.IsNullOrWhiteSpace(session.Quest.ActiveQuestTitle)
                ? "active quest"
                : session.Quest.ActiveQuestTitle;
            return $"Gate: entering dungeon for {title}.";
        }

        private static string EquipmentSellStatus(GameSessionState session, string itemId)
        {
            if (itemId == EquipmentCatalog.RustySwordId)
            {
                return "Sell locked: starter weapon";
            }

            if (session.Equipment.IsEquipped(itemId))
            {
                return "Sell locked: equipped";
            }

            var item = RuntimeContentDatabase.FindEquipment(itemId);
            return EquipmentShopRuntimeService.CanSell(session, itemId)
                ? $"Sellable ({item.SellPrice}g)"
                : "Not sellable";
        }

        private static string JoinSkillNames(string[] skillIds)
        {
            if (skillIds == null || skillIds.Length == 0)
            {
                return "none";
            }

            var result = string.Empty;
            for (var i = 0; i < skillIds.Length; i++)
            {
                var skill = RuntimeContentDatabase.FindSkill(skillIds[i]);
                if (skill == null)
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += ", ";
                }

                result += skill.DisplayName;
            }

            return result.Length > 0 ? result : "none";
        }
    }
}

using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Skills
{
    public static class SkillShopRuntimeService
    {
        public const int SkillMerchantStockSize = 2;

        public static string[] SkillMerchantStock(GameSessionState session)
        {
            EnsureSkillMerchantStock(session);
            return session.Shop.SkillMerchantStockSkillIds.ToArray();
        }

        public static bool IsSkillMerchantStocked(GameSessionState session, string skillId)
        {
            EnsureSkillMerchantStock(session);
            return session.Shop.SkillMerchantStockSkillIds.Contains(skillId);
        }

        public static void RefreshSkillMerchantStock(GameSessionState session)
        {
            EnsureShopState(session);
            session.Shop.SkillMerchantRefreshIndex++;
            GenerateSkillMerchantStock(session);
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, Conn.Runtime.World.ChapterOneUxText.SkillMerchantRefreshNotice(session));
        }

        public static bool CanBuy(GameSessionState session, string skillId)
        {
            var skill = SkillCatalog.Find(skillId);
            return skill != null && skill.BuyPrice > 0 && session.Gold >= skill.BuyPrice;
        }

        public static bool BuyAndEquip(GameSessionState session, string skillId)
        {
            var skill = SkillCatalog.Find(skillId);
            if (skill == null)
            {
                return false;
            }

            if (session.Gold < skill.BuyPrice)
            {
                RuntimeNoticeService.Set(session, $"Not enough gold for {skill.DisplayName}.");
                return false;
            }

            var shouldEquip = session.Skills.CountEquipped(skillId) == 0;
            session.Gold -= skill.BuyPrice;
            session.Skills.AddSkill(skillId);
            if (shouldEquip)
            {
                session.Skills.EquipFirstOpenFace(skillId, session.Equipment.DiceCount);
            }

            SaveIfPlaying();
            RuntimeNoticeService.Set(session, shouldEquip
                ? $"Bought and equipped {skill.DisplayName}."
                : $"Bought {skill.DisplayName}.");
            return true;
        }

        public static bool CanSellLoose(GameSessionState session, string skillId)
        {
            var skill = SkillCatalog.Find(skillId);
            return skill != null && session.Skills.CountOwned(skillId) > session.Skills.CountEquipped(skillId);
        }

        public static bool SellLoose(GameSessionState session, string skillId)
        {
            var skill = SkillCatalog.Find(skillId);
            if (skill == null || !session.Skills.RemoveLooseSkill(skillId))
            {
                RuntimeNoticeService.Set(session, "Cannot sell equipped skill.");
                return false;
            }

            session.Gold += skill.SellPrice;
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Sold {skill.DisplayName}.");
            return true;
        }

        private static void SaveIfPlaying()
        {
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
            }
        }

        private static void EnsureSkillMerchantStock(GameSessionState session)
        {
            EnsureShopState(session);
            if (session.Shop.SkillMerchantStockSkillIds.Count == 0)
            {
                GenerateSkillMerchantStock(session);
            }
        }

        private static void EnsureShopState(GameSessionState session)
        {
            if (session.Shop == null)
            {
                session.Shop = new ShopRuntimeState();
            }

            if (session.Shop.SkillMerchantStockSkillIds == null)
            {
                session.Shop.SkillMerchantStockSkillIds = new System.Collections.Generic.List<string>();
            }
        }

        private static void GenerateSkillMerchantStock(GameSessionState session)
        {
            session.Shop.SkillMerchantStockSkillIds.Clear();
            var purchasableCount = CountPurchasableSkills();
            if (purchasableCount == 0)
            {
                return;
            }

            var stockSize = Mathf.Min(SkillMerchantStockSize, purchasableCount);
            var startIndex = PositiveModulo(session.Shop.SkillMerchantRefreshIndex, purchasableCount);
            var added = 0;
            for (var offset = 0; offset < SkillCatalog.All.Length && added < stockSize; offset++)
            {
                var skill = PurchasableSkillAt((startIndex + offset) % purchasableCount);
                if (skill == null)
                {
                    continue;
                }

                session.Shop.SkillMerchantStockSkillIds.Add(skill.SkillId);
                added++;
            }
        }

        private static int CountPurchasableSkills()
        {
            var count = 0;
            for (var i = 0; i < SkillCatalog.All.Length; i++)
            {
                if (SkillCatalog.All[i].BuyPrice > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static SkillDefinition PurchasableSkillAt(int purchasableIndex)
        {
            var current = 0;
            for (var i = 0; i < SkillCatalog.All.Length; i++)
            {
                var skill = SkillCatalog.All[i];
                if (skill.BuyPrice <= 0)
                {
                    continue;
                }

                if (current == purchasableIndex)
                {
                    return skill;
                }

                current++;
            }

            return null;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}

using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Skills
{
    public static class SkillShopRuntimeService
    {
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
    }
}

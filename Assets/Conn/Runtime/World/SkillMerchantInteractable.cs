using Conn.Core.Skills;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class SkillMerchantInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt
        {
            get
            {
                var nextSkillId = GetNextOfferId();
                if (string.IsNullOrEmpty(nextSkillId))
                {
                    var sellable = FindFirstSellableSkillId();
                    if (!string.IsNullOrEmpty(sellable))
                    {
                        var sellSkill = SkillCatalog.Find(sellable);
                        return $"Sell {sellSkill.DisplayName} ({sellSkill.SellPrice}g)";
                    }

                    var duplicate = SkillCatalog.Find(SkillCatalog.FocusStrikeId);
                    return $"Buy {duplicate.DisplayName} ({duplicate.BuyPrice}g)";
                }

                var skill = SkillCatalog.Find(nextSkillId);
                return $"Buy {skill.DisplayName} ({skill.BuyPrice}g)";
            }
        }

        public bool CanInteract => true;

        public void Interact()
        {
            var nextSkillId = GetNextOfferId();
            if (!string.IsNullOrEmpty(nextSkillId))
            {
                BuyAndEquip(nextSkillId);
                return;
            }

            var sellable = FindFirstSellableSkillId();
            if (!string.IsNullOrEmpty(sellable))
            {
                SellSkill(sellable);
                return;
            }

            BuyAndEquip(SkillCatalog.FocusStrikeId);
            EquipBestKnownSkill();
        }

        private static string GetNextOfferId()
        {
            var skills = GameSession.Instance.State.Skills;
            if (!skills.HasSkill(SkillCatalog.GuardId))
            {
                return SkillCatalog.GuardId;
            }

            if (!skills.HasSkill(SkillCatalog.MendId))
            {
                return SkillCatalog.MendId;
            }

            return skills.HasSkill(SkillCatalog.FocusStrikeId) ? string.Empty : SkillCatalog.FocusStrikeId;
        }

        private static void BuyAndEquip(string skillId)
        {
            var session = GameSession.Instance.State;
            var skill = SkillCatalog.Find(skillId);
            if (skill == null)
            {
                return;
            }

            if (session.Gold < skill.BuyPrice)
            {
                RuntimeNoticeService.Set(session, $"Not enough gold for {skill.DisplayName}.");
                return;
            }

            session.Gold -= skill.BuyPrice;
            session.Skills.AddSkill(skillId);
            session.Skills.EquipFirstOpenFace(skillId, session.Equipment.DiceCount);
            GameSession.Instance.SaveGame();
            RuntimeNoticeService.Set(session, $"Bought and equipped {skill.DisplayName}.");
        }

        private static void EquipBestKnownSkill()
        {
            var session = GameSession.Instance.State;
            if (session.Skills.HasSkill(SkillCatalog.FocusStrikeId))
            {
                session.Skills.EquipFirstOpenFace(SkillCatalog.FocusStrikeId, session.Equipment.DiceCount);
                return;
            }

            if (session.Skills.HasSkill(SkillCatalog.GuardId))
            {
                session.Skills.EquipFirstOpenFace(SkillCatalog.GuardId, session.Equipment.DiceCount);
            }
        }

        private static string FindFirstSellableSkillId()
        {
            var skills = GameSession.Instance.State.Skills;
            for (var i = 0; i < skills.OwnedSkillIds.Count; i++)
            {
                var skillId = skills.OwnedSkillIds[i];
                if (skills.CountOwned(skillId) > skills.CountEquipped(skillId) && SkillCatalog.Find(skillId) != null)
                {
                    return skillId;
                }
            }

            return string.Empty;
        }

        private static void SellSkill(string skillId)
        {
            var session = GameSession.Instance.State;
            var skill = SkillCatalog.Find(skillId);
            if (skill == null || !session.Skills.RemoveLooseSkill(skillId))
            {
                RuntimeNoticeService.Set(session, "Cannot sell equipped skill.");
                return;
            }

            session.Gold += skill.SellPrice;
            GameSession.Instance.SaveGame();
            RuntimeNoticeService.Set(session, $"Sold {skill.DisplayName}.");
        }
    }
}

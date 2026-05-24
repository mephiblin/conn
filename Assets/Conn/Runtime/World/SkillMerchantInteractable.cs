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
                    return "Equip owned skill";
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

            EquipBestKnownSkill();
        }

        private static string GetNextOfferId()
        {
            var skills = GameSession.Instance.State.Skills;
            if (!skills.HasSkill(SkillCatalog.GuardId))
            {
                return SkillCatalog.GuardId;
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
                Debug.Log($"Not enough gold for {skill.DisplayName}.");
                return;
            }

            session.Gold -= skill.BuyPrice;
            session.Skills.AddSkill(skillId);
            session.Skills.EquipFirstOpenFace(skillId, session.Equipment.DiceCount);
            Debug.Log($"Bought and equipped {skill.DisplayName}.");
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
    }
}

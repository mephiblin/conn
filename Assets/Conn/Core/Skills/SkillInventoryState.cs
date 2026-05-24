using System.Collections.Generic;

namespace Conn.Core.Skills
{
    [System.Serializable]
    public sealed class SkillInventoryState
    {
        public List<string> OwnedSkillIds = new List<string>();
        public List<string> EquippedSkillIds = new List<string>();

        public int OwnedCount => OwnedSkillIds.Count;
        public int EquippedCount => EquippedSkillIds.Count;

        public bool HasSkill(string skillId)
        {
            return OwnedSkillIds.Contains(skillId);
        }

        public void AddSkill(string skillId)
        {
            if (!string.IsNullOrWhiteSpace(skillId))
            {
                OwnedSkillIds.Add(skillId);
            }
        }

        public void EquipFirstOpenFace(string skillId, int diceCount)
        {
            if (!HasSkill(skillId))
            {
                return;
            }

            while (EquippedSkillIds.Count < diceCount)
            {
                EquippedSkillIds.Add(string.Empty);
            }

            for (var i = 0; i < diceCount; i++)
            {
                if (string.IsNullOrWhiteSpace(EquippedSkillIds[i]))
                {
                    EquippedSkillIds[i] = skillId;
                    return;
                }
            }

            if (diceCount > 0)
            {
                EquippedSkillIds[0] = skillId;
            }
        }

        public int EquippedPower(int diceCount)
        {
            var total = 0;
            var count = EquippedSkillIds.Count < diceCount ? EquippedSkillIds.Count : diceCount;
            for (var i = 0; i < count; i++)
            {
                var skill = SkillCatalog.Find(EquippedSkillIds[i]);
                if (skill != null)
                {
                    total += skill.Power;
                }
            }

            return total;
        }

        public void ResizeEquippedFaces(int diceCount)
        {
            while (EquippedSkillIds.Count > diceCount)
            {
                EquippedSkillIds.RemoveAt(EquippedSkillIds.Count - 1);
            }
        }

        public void Clear()
        {
            OwnedSkillIds.Clear();
            EquippedSkillIds.Clear();
        }
    }
}

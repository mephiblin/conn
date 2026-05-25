using Conn.Core.Session;
using Conn.Runtime.Content;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Skills
{
    public static class SkillRuntimeService
    {
        public static bool CycleEquippedFace(GameSessionState session, int faceIndex)
        {
            var diceCount = session.Equipment.DiceCount;
            if (faceIndex < 0 || faceIndex >= diceCount || session.Skills.OwnedSkillIds.Count == 0)
            {
                return false;
            }

            while (session.Skills.EquippedSkillIds.Count < diceCount)
            {
                session.Skills.EquippedSkillIds.Add(string.Empty);
            }

            var currentSkillId = session.Skills.EquippedSkillIds[faceIndex];
            var currentOwnedIndex = FindOwnedIndexAfter(session, currentSkillId);
            for (var i = 0; i < session.Skills.OwnedSkillIds.Count; i++)
            {
                var nextIndex = (currentOwnedIndex + i) % session.Skills.OwnedSkillIds.Count;
                var nextSkillId = session.Skills.OwnedSkillIds[nextIndex];
                var skill = RuntimeContentDatabase.FindSkill(nextSkillId);
                if (skill == null || nextSkillId == currentSkillId)
                {
                    continue;
                }

                session.Skills.EquippedSkillIds[faceIndex] = nextSkillId;
                SaveIfPlaying();
                RuntimeNoticeService.Set(session, $"Equipped face {faceIndex + 1}: {skill.DisplayName}.");
                return true;
            }

            return false;
        }

        public static bool CycleNextEditFace(GameSessionState session)
        {
            var diceCount = session.Equipment.DiceCount;
            if (diceCount <= 0)
            {
                return false;
            }

            if (session.Skills.NextEditFaceIndex >= diceCount)
            {
                session.Skills.NextEditFaceIndex = 0;
            }

            var faceIndex = session.Skills.NextEditFaceIndex;
            var changed = CycleEquippedFace(session, faceIndex);
            session.Skills.NextEditFaceIndex = (faceIndex + 1) % diceCount;
            SaveIfPlaying();
            return changed;
        }

        public static bool EquipSkillToFace(GameSessionState session, string skillId, int faceIndex)
        {
            var diceCount = session.Equipment.DiceCount;
            if (faceIndex < 0 || faceIndex >= diceCount || string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            if (!session.Skills.HasSkill(skillId))
            {
                return false;
            }

            var skill = RuntimeContentDatabase.FindSkill(skillId);
            if (skill == null)
            {
                return false;
            }

            while (session.Skills.EquippedSkillIds.Count < diceCount)
            {
                session.Skills.EquippedSkillIds.Add(string.Empty);
            }

            if (EquippedCountExcludingFace(session, skillId, faceIndex) >= session.Skills.CountOwned(skillId))
            {
                RuntimeNoticeService.Set(session, $"No loose copy to equip: {skill.DisplayName}.");
                return false;
            }

            session.Skills.EquippedSkillIds[faceIndex] = skillId;
            session.Skills.NextEditFaceIndex = faceIndex;
            SaveIfPlaying();
            RuntimeNoticeService.Set(session, $"Equipped face {faceIndex + 1}: {skill.DisplayName}.");
            return true;
        }

        private static int EquippedCountExcludingFace(GameSessionState session, string skillId, int excludedFaceIndex)
        {
            var count = 0;
            for (var i = 0; i < session.Skills.EquippedSkillIds.Count; i++)
            {
                if (i != excludedFaceIndex && session.Skills.EquippedSkillIds[i] == skillId)
                {
                    count++;
                }
            }

            return count;
        }

        private static int FindOwnedIndexAfter(GameSessionState session, string skillId)
        {
            for (var i = 0; i < session.Skills.OwnedSkillIds.Count; i++)
            {
                if (session.Skills.OwnedSkillIds[i] == skillId)
                {
                    return (i + 1) % session.Skills.OwnedSkillIds.Count;
                }
            }

            return 0;
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

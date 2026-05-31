namespace Conn.Core.Skills
{
    using System;
    using System.Collections.Generic;

    [System.Serializable]
    public sealed class SkillInventoryState
    {
        public static Func<string, SkillDefinition> SkillResolver = SkillCatalog.Find;

        public List<string> OwnedSkillIds = new List<string>();
        public List<string> EquippedSkillIds = new List<string>();
        public List<string> DiceFaceSkillIds = new List<string>();
        public int NextEditFaceIndex;

        public int OwnedCount => OwnedSkillIds.Count;
        public int EquippedCount => EquippedSkillIds.Count;

        public bool HasSkill(string skillId)
        {
            return OwnedSkillIds.Contains(skillId);
        }

        public bool IsEquipped(string skillId)
        {
            return EquippedSkillIds.Contains(skillId);
        }

        public int CountOwned(string skillId)
        {
            var count = 0;
            for (var i = 0; i < OwnedSkillIds.Count; i++)
            {
                if (OwnedSkillIds[i] == skillId)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountEquipped(string skillId)
        {
            var count = 0;
            var equipped = HasDiceFaceLoadout ? DiceFaceSkillIds : EquippedSkillIds;
            for (var i = 0; i < equipped.Count; i++)
            {
                if (equipped[i] == skillId)
                {
                    count++;
                }
            }

            return count;
        }

        public void AddSkill(string skillId)
        {
            if (!string.IsNullOrWhiteSpace(skillId))
            {
                OwnedSkillIds.Add(skillId);
            }
        }

        public bool RemoveLooseSkill(string skillId)
        {
            if (CountOwned(skillId) <= CountEquipped(skillId))
            {
                return false;
            }

            return OwnedSkillIds.Remove(skillId);
        }

        public void EquipFirstOpenFace(string skillId, int diceCount)
        {
            if (!HasSkill(skillId))
            {
                return;
            }

            EnsureDiceFaceLoadout(diceCount);
            for (var i = 0; i < DiceFaceSkillIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(DiceFaceSkillIds[i]))
                {
                    DiceFaceSkillIds[i] = skillId;
                    SyncLegacyEquippedFaces(diceCount);
                    return;
                }
            }

            if (DiceFaceSkillIds.Count > 0)
            {
                DiceFaceSkillIds[0] = skillId;
                SyncLegacyEquippedFaces(diceCount);
            }
        }

        public int EquippedPower(int diceCount)
        {
            var total = 0;
            var count = EquippedSkillIds.Count < diceCount ? EquippedSkillIds.Count : diceCount;
            for (var i = 0; i < count; i++)
            {
                var skill = ResolveSkill(EquippedSkillIds[i]);
                if (skill != null)
                {
                    total += skill.Power;
                }
            }

            return total;
        }

        public void ResizeEquippedFaces(int diceCount)
        {
            ResizeDiceFaceLoadout(diceCount);
            SyncLegacyEquippedFaces(diceCount);

            if (diceCount <= 0)
            {
                NextEditFaceIndex = 0;
            }
            else if (NextEditFaceIndex >= diceCount)
            {
                NextEditFaceIndex = 0;
            }
        }

        public void Clear()
        {
            OwnedSkillIds.Clear();
            EquippedSkillIds.Clear();
            DiceFaceSkillIds.Clear();
            NextEditFaceIndex = 0;
        }

        public const int FacesPerDie = 6;

        public bool HasDiceFaceLoadout
        {
            get
            {
                for (var i = 0; i < DiceFaceSkillIds.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(DiceFaceSkillIds[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void EnsureDiceFaceLoadout(int diceCount)
        {
            var targetCount = Math.Max(0, diceCount) * FacesPerDie;
            while (DiceFaceSkillIds.Count < targetCount)
            {
                DiceFaceSkillIds.Add(string.Empty);
            }
        }

        public void ResizeDiceFaceLoadout(int diceCount)
        {
            EnsureDiceFaceLoadout(diceCount);
            var targetCount = Math.Max(0, diceCount) * FacesPerDie;
            while (DiceFaceSkillIds.Count > targetCount)
            {
                DiceFaceSkillIds.RemoveAt(DiceFaceSkillIds.Count - 1);
            }
        }

        public string SkillIdForDieFace(int dieIndex, int faceIndex)
        {
            var index = DiceFaceIndex(dieIndex, faceIndex);
            return index >= 0 && index < DiceFaceSkillIds.Count ? DiceFaceSkillIds[index] : string.Empty;
        }

        public bool SetSkillForDieFace(string skillId, int dieIndex, int faceIndex, int diceCount)
        {
            if (dieIndex < 0 || dieIndex >= diceCount || faceIndex < 0 || faceIndex >= FacesPerDie)
            {
                return false;
            }

            EnsureDiceFaceLoadout(diceCount);
            var index = DiceFaceIndex(dieIndex, faceIndex);
            DiceFaceSkillIds[index] = skillId ?? string.Empty;
            SyncLegacyEquippedFaces(diceCount);
            return true;
        }

        public int EquippedCountExcludingDieFace(string skillId, int excludedDieIndex, int excludedFaceIndex)
        {
            var excludedIndex = DiceFaceIndex(excludedDieIndex, excludedFaceIndex);
            var count = 0;
            for (var i = 0; i < DiceFaceSkillIds.Count; i++)
            {
                if (i != excludedIndex && DiceFaceSkillIds[i] == skillId)
                {
                    count++;
                }
            }

            return count;
        }

        private static int DiceFaceIndex(int dieIndex, int faceIndex)
        {
            return dieIndex < 0 || faceIndex < 0 ? -1 : dieIndex * FacesPerDie + faceIndex;
        }

        private void SyncLegacyEquippedFaces(int diceCount)
        {
            while (EquippedSkillIds.Count < diceCount)
            {
                EquippedSkillIds.Add(string.Empty);
            }

            while (EquippedSkillIds.Count > diceCount)
            {
                EquippedSkillIds.RemoveAt(EquippedSkillIds.Count - 1);
            }

            for (var dieIndex = 0; dieIndex < diceCount; dieIndex++)
            {
                var firstSkillId = string.Empty;
                for (var faceIndex = 0; faceIndex < FacesPerDie; faceIndex++)
                {
                    var skillId = SkillIdForDieFace(dieIndex, faceIndex);
                    if (!string.IsNullOrWhiteSpace(skillId))
                    {
                        firstSkillId = skillId;
                        break;
                    }
                }

                EquippedSkillIds[dieIndex] = firstSkillId;
            }
        }

        private static SkillDefinition ResolveSkill(string skillId)
        {
            return SkillResolver != null ? SkillResolver(skillId) : SkillCatalog.Find(skillId);
        }
    }
}

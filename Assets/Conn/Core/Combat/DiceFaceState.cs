namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class DiceFaceState
    {
        public int Index;
        public string SkillId = string.Empty;
        public int Power;
        public bool Selected;

        public string Label => string.IsNullOrWhiteSpace(SkillId)
            ? $"Die {Index + 1}: Strike +{Power}"
            : $"Die {Index + 1}: {SkillId} +{Power}";
    }
}

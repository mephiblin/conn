using Conn.Core.Skills;

namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class DiceFaceState
    {
        public int Index;
        public string SkillId = string.Empty;
        public string DisplayName = string.Empty;
        public SkillEffectKind EffectKind;
        public int Power;
        public bool Selected;
        public int Cooldown;

        public bool IsCoolingDown => Cooldown > 0;

        public string Label => $"Die {Index + 1}: {DisplayName} {EffectKind} +{Power}";
    }
}

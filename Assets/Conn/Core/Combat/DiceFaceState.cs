using Conn.Core.Skills;

namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class DiceFaceState
    {
        public int Index;
        public string SkillId = string.Empty;
        public string DisplayName = string.Empty;
        public int RolledValue = 1;
        public SkillEffectKind EffectKind;
        public string SpecialEffectId = string.Empty;
        public int Power;
        public bool Selected;
        public int Cooldown;
        public bool ReelStopped;
        public string[] ReelSkillIds = new string[0];
        public int ReelStopIndex;

        public bool IsCoolingDown => Cooldown > 0;

        public string Label => $"Die {Index + 1}: {RolledValue} {DisplayName} {EffectKind} +{Power}";
    }
}

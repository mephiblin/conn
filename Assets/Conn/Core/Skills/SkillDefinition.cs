namespace Conn.Core.Skills
{
    public sealed class SkillDefinition
    {
        public SkillDefinition(
            string skillId,
            string displayName,
            SkillEffectKind effectKind,
            int buyPrice,
            int sellPrice,
            int power,
            string specialEffectId = "")
        {
            SkillId = skillId;
            DisplayName = displayName;
            EffectKind = effectKind;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            Power = power;
            SpecialEffectId = specialEffectId;
        }

        public string SkillId { get; }
        public string DisplayName { get; }
        public SkillEffectKind EffectKind { get; }
        public int BuyPrice { get; }
        public int SellPrice { get; }
        public int Power { get; }
        public string SpecialEffectId { get; }
    }
}

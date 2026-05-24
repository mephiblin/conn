namespace Conn.Core.Skills
{
    public sealed class SkillDefinition
    {
        public SkillDefinition(string skillId, string displayName, int buyPrice, int sellPrice, int power)
        {
            SkillId = skillId;
            DisplayName = displayName;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            Power = power;
        }

        public string SkillId { get; }
        public string DisplayName { get; }
        public int BuyPrice { get; }
        public int SellPrice { get; }
        public int Power { get; }
    }
}

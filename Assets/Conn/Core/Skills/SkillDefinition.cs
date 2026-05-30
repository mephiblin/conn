using System;

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
            string specialEffectId = "",
            SkillSpeciesModifier[] speciesModifiers = null)
        {
            SkillId = skillId;
            DisplayName = displayName;
            EffectKind = effectKind;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            Power = power;
            SpecialEffectId = specialEffectId;
            SpeciesModifiers = speciesModifiers ?? Array.Empty<SkillSpeciesModifier>();
        }

        public string SkillId { get; }
        public string DisplayName { get; }
        public SkillEffectKind EffectKind { get; }
        public int BuyPrice { get; }
        public int SellPrice { get; }
        public int Power { get; }
        public string SpecialEffectId { get; }
        public SkillSpeciesModifier[] SpeciesModifiers { get; }

        public int AdjustPowerForSpecies(string species, int basePower)
        {
            var adjusted = basePower;
            for (var i = 0; i < SpeciesModifiers.Length; i++)
            {
                var modifier = SpeciesModifiers[i];
                if (!modifier.Matches(species))
                {
                    continue;
                }

                adjusted += modifier.FlatPowerDelta;
                adjusted = (int)Math.Round(adjusted * modifier.PowerMultiplier, MidpointRounding.AwayFromZero);
            }

            return adjusted < 0 ? 0 : adjusted;
        }
    }

    public sealed class SkillSpeciesModifier
    {
        public SkillSpeciesModifier(string species, int flatPowerDelta, float powerMultiplier)
        {
            Species = species ?? string.Empty;
            FlatPowerDelta = flatPowerDelta;
            PowerMultiplier = powerMultiplier < 0f ? 0f : powerMultiplier;
        }

        public string Species { get; }
        public int FlatPowerDelta { get; }
        public float PowerMultiplier { get; }

        public bool Matches(string species)
        {
            return string.Equals(Species, species, StringComparison.Ordinal);
        }
    }
}

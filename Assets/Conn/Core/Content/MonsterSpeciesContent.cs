using System;

namespace Conn.Core.Content
{
    public static class MonsterSpeciesCatalog
    {
        public const string Human = "Human";
        public const string Beast = "Beast";
        public const string Aberration = "Aberration";
        public const string Undead = "Undead";

        public static readonly string[] All =
        {
            Human,
            Beast,
            Aberration,
            Undead
        };

        public static bool IsSupported(string species)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i], species, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static string NormalizeOrDefault(string species, string fallback = Human)
        {
            return IsSupported(species) ? species : fallback;
        }
    }

    [Serializable]
    public sealed class ContentMonsterSpeciesProfileDefinition
    {
        public string Species = string.Empty;
        public int TurnRegenHp;
        public string[] TraitTags = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ContentSkillSpeciesModifierDefinition
    {
        public string Species = string.Empty;
        public int FlatPowerDelta;
        public float PowerMultiplier = 1f;
    }

    public sealed class MonsterSpeciesProfile
    {
        public MonsterSpeciesProfile(string species, int turnRegenHp, string[] traitTags)
        {
            Species = species ?? string.Empty;
            TurnRegenHp = turnRegenHp < 0 ? 0 : turnRegenHp;
            TraitTags = traitTags ?? Array.Empty<string>();
        }

        public string Species { get; }
        public int TurnRegenHp { get; }
        public string[] TraitTags { get; }
    }
}

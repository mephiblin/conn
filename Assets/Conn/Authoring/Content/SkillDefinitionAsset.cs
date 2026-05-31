using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Skill Definition", fileName = "SkillDefinition")]
    public sealed class SkillDefinitionAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string EffectKind = "attack";
        public string TargetMode = string.Empty;
        public string Formula = string.Empty;
        public string SpecialEffectId = string.Empty;
        public int Cooldown = 1;
        public int Duration;
        public string Status = string.Empty;
        public string[] Tags = Array.Empty<string>();
        [TextArea] public string Description = string.Empty;
        public int BuyPrice;
        public int SellPrice;
        public int Power = 1;
        public SkillSpeciesModifierAsset[] SpeciesModifiers = Array.Empty<SkillSpeciesModifierAsset>();
        public string[] CatalogIds = Array.Empty<string>();

        public ContentSkillDefinition ToContentDefinition()
        {
            return new ContentSkillDefinition
            {
                Id = Id,
                DisplayName = DisplayName,
                EffectKind = EffectKind,
                TargetMode = TargetMode,
                Formula = Formula,
                SpecialEffectId = SpecialEffectId,
                Cooldown = Cooldown,
                Duration = Duration,
                Status = Status,
                Tags = Tags ?? Array.Empty<string>(),
                Description = Description,
                BuyPrice = BuyPrice,
                SellPrice = SellPrice,
                Power = Power,
                SpeciesModifiers = ToContentSpeciesModifiers(SpeciesModifiers),
                CatalogIds = CatalogIds ?? Array.Empty<string>()
            };
        }

        private static ContentSkillSpeciesModifierDefinition[] ToContentSpeciesModifiers(SkillSpeciesModifierAsset[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                return Array.Empty<ContentSkillSpeciesModifierDefinition>();
            }

            var content = new ContentSkillSpeciesModifierDefinition[modifiers.Length];
            for (var i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i] ?? new SkillSpeciesModifierAsset();
                content[i] = new ContentSkillSpeciesModifierDefinition
                {
                    Species = modifier.Species.ToString(),
                    FlatPowerDelta = modifier.FlatPowerDelta,
                    PowerMultiplier = modifier.PowerMultiplier
                };
            }

            return content;
        }
    }

    [Serializable]
    public sealed class SkillSpeciesModifierAsset
    {
        public MonsterSpecies Species;
        public int FlatPowerDelta;
        public float PowerMultiplier = 1f;
    }
}

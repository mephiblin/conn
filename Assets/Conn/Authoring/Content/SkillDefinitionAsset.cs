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
        public int BuyPrice;
        public int SellPrice;
        public int Power = 1;
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
                BuyPrice = BuyPrice,
                SellPrice = SellPrice,
                Power = Power,
                CatalogIds = CatalogIds ?? Array.Empty<string>()
            };
        }
    }
}

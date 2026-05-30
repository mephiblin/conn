using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Monster Trait", fileName = "MonsterTrait")]
    public sealed class MonsterTraitAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string[] TraitTags = Array.Empty<string>();
        public int TurnRegenHp;
        public int FlatDamageReduction;
        public float IncomingDamageMultiplier = 1f;
        public float OutgoingDamageMultiplier = 1f;
        public string[] StatusImmunityIds = Array.Empty<string>();
        public string ReactiveEffectId = string.Empty;
        [TextArea]
        public string Notes = string.Empty;

        public ContentMonsterTraitDefinition ToContentDefinition()
        {
            return new ContentMonsterTraitDefinition
            {
                Id = Id,
                DisplayName = DisplayName,
                TraitTags = TraitTags ?? Array.Empty<string>(),
                TurnRegenHp = Mathf.Max(0, TurnRegenHp),
                FlatDamageReduction = Mathf.Max(0, FlatDamageReduction),
                IncomingDamageMultiplier = Mathf.Max(0f, IncomingDamageMultiplier),
                OutgoingDamageMultiplier = Mathf.Max(0f, OutgoingDamageMultiplier),
                StatusImmunityIds = StatusImmunityIds ?? Array.Empty<string>(),
                ReactiveEffectId = ReactiveEffectId ?? string.Empty,
                Notes = string.IsNullOrWhiteSpace(Notes) ? Array.Empty<string>() : new[] { Notes.Trim() }
            };
        }

        private void OnValidate()
        {
            TurnRegenHp = Mathf.Max(0, TurnRegenHp);
            FlatDamageReduction = Mathf.Max(0, FlatDamageReduction);
            IncomingDamageMultiplier = Mathf.Max(0f, IncomingDamageMultiplier);
            OutgoingDamageMultiplier = Mathf.Max(0f, OutgoingDamageMultiplier);
        }
    }
}

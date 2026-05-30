using System;

namespace Conn.Core.Content
{
    [Serializable]
    public sealed class ContentMonsterTraitDefinition
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
        public string[] Notes = Array.Empty<string>();
    }

    public sealed class MonsterTraitDefinition
    {
        public MonsterTraitDefinition(
            string id,
            string displayName,
            string[] traitTags,
            int turnRegenHp,
            int flatDamageReduction,
            float incomingDamageMultiplier,
            float outgoingDamageMultiplier,
            string[] statusImmunityIds,
            string reactiveEffectId,
            string[] notes)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            TraitTags = traitTags ?? Array.Empty<string>();
            TurnRegenHp = turnRegenHp < 0 ? 0 : turnRegenHp;
            FlatDamageReduction = flatDamageReduction < 0 ? 0 : flatDamageReduction;
            IncomingDamageMultiplier = incomingDamageMultiplier < 0f ? 0f : incomingDamageMultiplier;
            OutgoingDamageMultiplier = outgoingDamageMultiplier < 0f ? 0f : outgoingDamageMultiplier;
            StatusImmunityIds = statusImmunityIds ?? Array.Empty<string>();
            ReactiveEffectId = reactiveEffectId ?? string.Empty;
            Notes = notes ?? Array.Empty<string>();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string[] TraitTags { get; }
        public int TurnRegenHp { get; }
        public int FlatDamageReduction { get; }
        public float IncomingDamageMultiplier { get; }
        public float OutgoingDamageMultiplier { get; }
        public string[] StatusImmunityIds { get; }
        public string ReactiveEffectId { get; }
        public string[] Notes { get; }
    }
}

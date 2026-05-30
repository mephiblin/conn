using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Monster Species Profile", fileName = "MonsterSpeciesProfile")]
    public sealed class MonsterSpeciesProfileAsset : ScriptableObject
    {
        public MonsterSpecies Species;
        public int TurnRegenHp;
        public string[] TraitTags = Array.Empty<string>();

        public ContentMonsterSpeciesProfileDefinition ToContentDefinition()
        {
            return new ContentMonsterSpeciesProfileDefinition
            {
                Species = Species.ToString(),
                TurnRegenHp = Mathf.Max(0, TurnRegenHp),
                TraitTags = TraitTags ?? Array.Empty<string>()
            };
        }

        private void OnValidate()
        {
            TurnRegenHp = Mathf.Max(0, TurnRegenHp);
        }
    }
}

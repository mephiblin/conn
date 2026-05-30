using Conn.Core.Content;
using Conn.Core.World;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    public enum MonsterSpecies
    {
        Human = 0,
        Beast = 1,
        Aberration = 2,
        Undead = 3
    }

    public enum MonsterGrade
    {
        Normal = 0,
        Elite = 1,
        Boss = 2
    }

    [CreateAssetMenu(menuName = "Conn/Authoring/Monster Definition", fileName = "MonsterDefinition")]
    public sealed class MonsterDefinitionAsset : ScriptableObject
    {
        [Header("Runtime Identity")]
        public string Id = string.Empty;
        public string DisplayName = string.Empty;

        [Header("Monster Design")]
        public Texture2D VisualImage;
        public MonsterSpecies Species;
        public MonsterGrade Grade;
        public int DefaultGroupCount = 1;

        [Header("Runtime Stats")]
        public int MaxHp = 1;
        public int AttackPower = 1;
        public int Defense;
        [Range(0f, 1f)]
        public float EvasionRate;
        public int XpReward;
        public bool Boss;
        public string Ai = string.Empty;

        [Header("Field AI")]
        public FieldMonsterAiProfile FieldAiProfile = FieldMonsterAiProfile.Default();
        public MonsterTraitAsset[] Traits = Array.Empty<MonsterTraitAsset>();

        [Header("Authoring References")]
        public GameObject Prefab;
        public GameObject FbxOrMeshSource;
        public RuntimeAnimatorController AnimatorController;
        public AudioClip[] AudioReferences = Array.Empty<AudioClip>();
        public UnityEngine.Object[] VfxReferences = Array.Empty<UnityEngine.Object>();

        [Header("Spawn Metadata")]
        public string[] ThemeTags = Array.Empty<string>();
        public string[] BiomeTags = Array.Empty<string>();
        public string[] SpawnRoleTags = Array.Empty<string>();
        public string[] CompatibilityTags = Array.Empty<string>();

        public ContentMonsterDefinition ToContentDefinition()
        {
            return new ContentMonsterDefinition
            {
                Id = Id,
                DisplayName = DisplayName,
                MaxHp = MaxHp,
                AttackPower = AttackPower,
                Defense = Defense,
                EvasionRate = EvasionRate,
                XpReward = XpReward,
                Boss = Boss || Grade == MonsterGrade.Boss,
                Ai = Ai,
                Species = Species.ToString(),
                Grade = Grade.ToString(),
                DefaultGroupCount = DefaultGroupCount,
                FieldAiProfile = FieldAiProfile != null ? FieldAiProfile.Clone() : FieldMonsterAiProfile.Default(),
                TraitIds = ResolveTraitIds(Traits),
                ThemeTags = ThemeTags ?? Array.Empty<string>(),
                BiomeTags = BiomeTags ?? Array.Empty<string>(),
                SpawnRoleTags = SpawnRoleTags ?? Array.Empty<string>(),
                CompatibilityTags = CompatibilityTags ?? Array.Empty<string>()
            };
        }

        private void OnValidate()
        {
            DefaultGroupCount = Mathf.Max(1, DefaultGroupCount);
            MaxHp = Mathf.Max(1, MaxHp);
            AttackPower = Mathf.Max(1, AttackPower);
            Defense = Mathf.Max(0, Defense);
            EvasionRate = Mathf.Clamp01(EvasionRate);
            XpReward = Mathf.Max(0, XpReward);
        }

        private static string[] ResolveTraitIds(MonsterTraitAsset[] traits)
        {
            if (traits == null || traits.Length == 0)
            {
                return Array.Empty<string>();
            }

            var ids = new string[traits.Length];
            for (var i = 0; i < traits.Length; i++)
            {
                ids[i] = !string.IsNullOrWhiteSpace(traits[i]?.Id) ? traits[i].Id : string.Empty;
            }

            return ids;
        }
    }
}

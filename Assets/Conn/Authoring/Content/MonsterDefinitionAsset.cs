using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Monster Definition", fileName = "MonsterDefinition")]
    public sealed class MonsterDefinitionAsset : ScriptableObject
    {
        [Header("Runtime Identity")]
        public string Id = string.Empty;
        public string DisplayName = string.Empty;

        [Header("Runtime Stats")]
        public int MaxHp = 1;
        public int AttackPower = 1;
        public int Defense;
        public int XpReward;
        public bool Boss;
        public string Ai = string.Empty;

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
                XpReward = XpReward,
                Boss = Boss,
                Ai = Ai,
                ThemeTags = ThemeTags ?? Array.Empty<string>(),
                BiomeTags = BiomeTags ?? Array.Empty<string>(),
                SpawnRoleTags = SpawnRoleTags ?? Array.Empty<string>(),
                CompatibilityTags = CompatibilityTags ?? Array.Empty<string>()
            };
        }
    }
}

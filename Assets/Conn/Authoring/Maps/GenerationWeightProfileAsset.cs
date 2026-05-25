using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Generation Weight Profile", fileName = "GenerationWeightProfile")]
    public sealed class GenerationWeightProfileAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string MapProfileId = string.Empty;
        public WeightedAssetReference[] LandmarkWeights = Array.Empty<WeightedAssetReference>();
        public WeightedAssetReference[] ChunkWeights = Array.Empty<WeightedAssetReference>();
        public WeightedAssetReference[] SpawnSourceWeights = Array.Empty<WeightedAssetReference>();
        public WeightedAssetReference[] DecorWeights = Array.Empty<WeightedAssetReference>();
        public WeightedAssetReference[] LootWeights = Array.Empty<WeightedAssetReference>();
    }
}

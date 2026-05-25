using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Vendor Definition", fileName = "VendorDefinition")]
    public sealed class VendorDefinitionAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string ServiceType = string.Empty;
        public int GoldCost;
        [TextArea]
        public string Summary = string.Empty;
        public string[] StockItemIds = Array.Empty<string>();
        public string[] StockSkillIds = Array.Empty<string>();
        public string[] CatalogIds = Array.Empty<string>();
        public VendorRotationAsset[] Rotations = Array.Empty<VendorRotationAsset>();

        public ContentVendorDefinition ToContentDefinition()
        {
            var rotations = Rotations ?? Array.Empty<VendorRotationAsset>();
            var contentRotations = new ContentVendorRotationDefinition[rotations.Length];
            for (var i = 0; i < rotations.Length; i++)
            {
                contentRotations[i] = rotations[i].ToContentDefinition();
            }

            return new ContentVendorDefinition
            {
                Id = Id,
                ServiceType = ServiceType,
                GoldCost = GoldCost,
                Summary = Summary,
                StockItemIds = StockItemIds ?? Array.Empty<string>(),
                StockSkillIds = StockSkillIds ?? Array.Empty<string>(),
                CatalogIds = CatalogIds ?? Array.Empty<string>(),
                Rotations = contentRotations
            };
        }
    }

    [Serializable]
    public sealed class VendorRotationAsset
    {
        public int MinFloor;
        public int BossesDefeated;
        public int GoldCost;
        [TextArea]
        public string Summary = string.Empty;
        public string[] StockItemIds = Array.Empty<string>();
        public string[] StockSkillIds = Array.Empty<string>();
        public string[] CatalogIds = Array.Empty<string>();

        public ContentVendorRotationDefinition ToContentDefinition()
        {
            return new ContentVendorRotationDefinition
            {
                MinFloor = MinFloor,
                BossesDefeated = BossesDefeated,
                GoldCost = GoldCost,
                Summary = Summary,
                StockItemIds = StockItemIds ?? Array.Empty<string>(),
                StockSkillIds = StockSkillIds ?? Array.Empty<string>(),
                CatalogIds = CatalogIds ?? Array.Empty<string>()
            };
        }
    }
}

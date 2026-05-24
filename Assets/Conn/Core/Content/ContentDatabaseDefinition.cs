using System;
using UnityEngine;

namespace Conn.Core.Content
{
    [CreateAssetMenu(menuName = "Conn/Content Database", fileName = "ContentDatabase")]
    public sealed class ContentDatabaseDefinition : ScriptableObject
    {
        public ContentItemDefinition[] Items = Array.Empty<ContentItemDefinition>();
        public ContentEquipmentDefinition[] Equipment = Array.Empty<ContentEquipmentDefinition>();
        public ContentSkillDefinition[] Skills = Array.Empty<ContentSkillDefinition>();
        public ContentMonsterDefinition[] Monsters = Array.Empty<ContentMonsterDefinition>();
        public ContentEncounterDefinition[] Encounters = Array.Empty<ContentEncounterDefinition>();
        public ContentQuestDefinition[] Quests = Array.Empty<ContentQuestDefinition>();
        public ContentVendorDefinition[] Vendors = Array.Empty<ContentVendorDefinition>();
        public ContentNpcDefinition[] Npcs = Array.Empty<ContentNpcDefinition>();

        public ContentIdRegistry BuildRegistry()
        {
            var registry = new ContentIdRegistry();
            registry.RegisterItems(Items);
            registry.RegisterEquipment(Equipment);
            registry.RegisterSkills(Skills);
            registry.RegisterMonsters(Monsters);
            registry.RegisterEncounters(Encounters);
            registry.RegisterQuests(Quests);
            registry.RegisterVendors(Vendors);
            registry.RegisterNpcs(Npcs);
            return registry;
        }
    }

    [Serializable]
    public sealed class ContentItemDefinition
    {
        public string Id;
        public string DisplayName;
        public string Kind;
        public int BuyPrice;
        public int SellPrice;
        public int HealAmount;
    }

    [Serializable]
    public sealed class ContentEquipmentDefinition
    {
        public string Id;
        public string DisplayName;
        public string Kind;
        public int BuyPrice;
        public int SellPrice;
        public int ArmorValue;
        public bool Generated;
        public string RarityId;
        public string AffixPoolId;
    }

    [Serializable]
    public sealed class ContentSkillDefinition
    {
        public string Id;
        public string DisplayName;
        public string EffectKind;
        public string TargetMode;
        public string Formula;
        public int BuyPrice;
        public int SellPrice;
        public int Power;
        public string[] CatalogIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ContentMonsterDefinition
    {
        public string Id;
        public string DisplayName;
        public int MaxHp;
        public int AttackPower;
        public int Defense;
        public int XpReward;
        public bool Boss;
        public string Ai;
    }

    [Serializable]
    public sealed class ContentEncounterDefinition
    {
        public string Id;
        public string DisplayName;
        public string MonsterId;
        public int XpReward;
        public string RewardId;
        public string Pattern;
        public ContentEncounterEnemySlot[] EnemySlots = Array.Empty<ContentEncounterEnemySlot>();
    }

    [Serializable]
    public sealed class ContentEncounterEnemySlot
    {
        public string SlotId;
        public string MonsterId;
        public int Count = 1;
        public bool Primary;
    }

    [Serializable]
    public sealed class ContentQuestDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string MapKind;
        public string MapProfileId;
        public string TargetMonsterId;
        public string TargetEncounterId;
        public int GoldReward;
        public int XpReward;
        public ContentItemStack[] RewardItems = Array.Empty<ContentItemStack>();
    }

    [Serializable]
    public sealed class ContentItemStack
    {
        public string ItemId;
        public int Quantity;
    }

    [Serializable]
    public sealed class ContentVendorDefinition
    {
        public string Id;
        public string ServiceType;
        public int GoldCost;
        public string Summary;
        public string[] StockItemIds = Array.Empty<string>();
        public string[] StockSkillIds = Array.Empty<string>();
        public string[] CatalogIds = Array.Empty<string>();
        public ContentVendorRotationDefinition[] Rotations = Array.Empty<ContentVendorRotationDefinition>();
    }

    [Serializable]
    public sealed class ContentVendorRotationDefinition
    {
        public int MinFloor;
        public int BossesDefeated;
        public int GoldCost;
        public string Summary;
        public string[] StockItemIds = Array.Empty<string>();
        public string[] StockSkillIds = Array.Empty<string>();
        public string[] CatalogIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ContentNpcDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string ServiceType;
        public string VendorId;
        public string[] QuestIds = Array.Empty<string>();
    }
}

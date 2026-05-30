using System;
using System.Collections.Generic;

namespace Conn.Core.Content
{
    public enum ContentIdKind
    {
        Item,
        Equipment,
        Skill,
        MonsterTrait,
        Monster,
        Encounter,
        Quest,
        Vendor,
        Npc
    }

    public sealed class ContentIdRegistry
    {
        private readonly Dictionary<string, ContentItemDefinition> items = new Dictionary<string, ContentItemDefinition>();
        private readonly Dictionary<string, ContentEquipmentDefinition> equipment = new Dictionary<string, ContentEquipmentDefinition>();
        private readonly Dictionary<string, ContentSkillDefinition> skills = new Dictionary<string, ContentSkillDefinition>();
        private readonly Dictionary<string, ContentMonsterTraitDefinition> monsterTraits = new Dictionary<string, ContentMonsterTraitDefinition>();
        private readonly Dictionary<string, ContentMonsterDefinition> monsters = new Dictionary<string, ContentMonsterDefinition>();
        private readonly Dictionary<string, ContentEncounterDefinition> encounters = new Dictionary<string, ContentEncounterDefinition>();
        private readonly Dictionary<string, ContentQuestDefinition> quests = new Dictionary<string, ContentQuestDefinition>();
        private readonly Dictionary<string, ContentVendorDefinition> vendors = new Dictionary<string, ContentVendorDefinition>();
        private readonly Dictionary<string, ContentNpcDefinition> npcs = new Dictionary<string, ContentNpcDefinition>();

        public ContentItemDefinition FindItem(string id) => Find(items, id);
        public ContentEquipmentDefinition FindEquipment(string id) => Find(equipment, id);
        public ContentSkillDefinition FindSkill(string id) => Find(skills, id);
        public ContentMonsterTraitDefinition FindMonsterTrait(string id) => Find(monsterTraits, id);
        public ContentMonsterDefinition FindMonster(string id) => Find(monsters, id);
        public ContentEncounterDefinition FindEncounter(string id) => Find(encounters, id);
        public ContentQuestDefinition FindQuest(string id) => Find(quests, id);
        public ContentVendorDefinition FindVendor(string id) => Find(vendors, id);
        public ContentNpcDefinition FindNpc(string id) => Find(npcs, id);

        public bool TryFindItem(string id, out ContentItemDefinition definition) => TryFind(items, id, out definition);
        public bool TryFindEquipment(string id, out ContentEquipmentDefinition definition) => TryFind(equipment, id, out definition);
        public bool TryFindSkill(string id, out ContentSkillDefinition definition) => TryFind(skills, id, out definition);
        public bool TryFindMonsterTrait(string id, out ContentMonsterTraitDefinition definition) => TryFind(monsterTraits, id, out definition);
        public bool TryFindMonster(string id, out ContentMonsterDefinition definition) => TryFind(monsters, id, out definition);
        public bool TryFindEncounter(string id, out ContentEncounterDefinition definition) => TryFind(encounters, id, out definition);
        public bool TryFindQuest(string id, out ContentQuestDefinition definition) => TryFind(quests, id, out definition);
        public bool TryFindVendor(string id, out ContentVendorDefinition definition) => TryFind(vendors, id, out definition);
        public bool TryFindNpc(string id, out ContentNpcDefinition definition) => TryFind(npcs, id, out definition);

        public bool ContainsAnyItemLikeId(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && (items.ContainsKey(id) || equipment.ContainsKey(id));
        }

        public void RegisterItems(IEnumerable<ContentItemDefinition> definitions)
        {
            Register(items, definitions, item => item.Id, ContentIdKind.Item);
        }

        public void RegisterEquipment(IEnumerable<ContentEquipmentDefinition> definitions)
        {
            Register(equipment, definitions, item => item.Id, ContentIdKind.Equipment);
        }

        public void RegisterSkills(IEnumerable<ContentSkillDefinition> definitions)
        {
            Register(skills, definitions, skill => skill.Id, ContentIdKind.Skill);
        }

        public void RegisterMonsterTraits(IEnumerable<ContentMonsterTraitDefinition> definitions)
        {
            Register(monsterTraits, definitions, trait => trait.Id, ContentIdKind.MonsterTrait);
        }

        public void RegisterMonsters(IEnumerable<ContentMonsterDefinition> definitions)
        {
            Register(monsters, definitions, monster => monster.Id, ContentIdKind.Monster);
        }

        public void RegisterEncounters(IEnumerable<ContentEncounterDefinition> definitions)
        {
            Register(encounters, definitions, encounter => encounter.Id, ContentIdKind.Encounter);
        }

        public void RegisterQuests(IEnumerable<ContentQuestDefinition> definitions)
        {
            Register(quests, definitions, quest => quest.Id, ContentIdKind.Quest);
        }

        public void RegisterVendors(IEnumerable<ContentVendorDefinition> definitions)
        {
            Register(vendors, definitions, vendor => vendor.Id, ContentIdKind.Vendor);
        }

        public void RegisterNpcs(IEnumerable<ContentNpcDefinition> definitions)
        {
            Register(npcs, definitions, npc => npc.Id, ContentIdKind.Npc);
        }

        private static T Find<T>(Dictionary<string, T> definitions, string id)
        {
            return !string.IsNullOrWhiteSpace(id) && definitions.TryGetValue(id, out var definition) ? definition : default;
        }

        private static bool TryFind<T>(Dictionary<string, T> definitions, string id, out T definition)
        {
            if (!string.IsNullOrWhiteSpace(id) && definitions.TryGetValue(id, out definition))
            {
                return true;
            }

            definition = default;
            return false;
        }

        private static void Register<T>(Dictionary<string, T> target, IEnumerable<T> definitions, Func<T, string> getId, ContentIdKind kind)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                var id = getId(definition);
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException($"{kind} id must not be empty.");
                }

                if (target.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Duplicate {kind} id: {id}");
                }

                target.Add(id, definition);
            }
        }
    }
}

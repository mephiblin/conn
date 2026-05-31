using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    public sealed class MonsterGeneratorWorkspace : MonoBehaviour
    {
        public MonsterDefinitionAsset Monster;
        public EncounterDefinitionAsset Encounter;
        public SpawnTableAsset SpawnTable;
        public Transform PreviewRoot;
        public Transform[] SpawnPoints = Array.Empty<Transform>();
        public float EncounterSpacing = 2f;
        public bool ClearBeforePreview = true;

        [Header("몬스터 생성")]
        public string CreateId = "monster_new";
        public string CreateDisplayName = "New Monster";
        public Texture2D CreateVisualImage;
        public MonsterSpecies CreateSpecies;
        public MonsterGrade CreateGrade;
        public int CreateDefaultGroupCount = 1;
        public int CreateMaxHp = 8;
        public int CreateAttackPower = 2;
        public int CreateDefense;
        [Range(0f, 1f)]
        public float CreateEvasionRate;
        public int CreateXpReward = 3;
        public string CreateAi = "Attack";
        public string CreateAssetFolder = "Assets/Conn/Authoring/Content/GeneratedMonsters";
        public string CreatePrefabFolder = "Assets/Conn/Prefabs/Monsters";
        public MonsterDefinitionAsset LastCreatedMonster;

        public void PreviewMonster()
        {
            if (Monster == null)
            {
                return;
            }

            if (ClearBeforePreview)
            {
                ClearPreview();
            }

            SpawnMonster(Monster, "Monster", SpawnPosition(0));
        }

        public void PreviewEncounter()
        {
            if (Encounter == null)
            {
                return;
            }

            if (ClearBeforePreview)
            {
                ClearPreview();
            }

            var spawned = 0;
            if (Encounter.PrimaryMonster != null)
            {
                SpawnMonster(Encounter.PrimaryMonster, "Primary", SpawnPosition(spawned));
                spawned++;
            }

            foreach (var slot in Encounter.EnemySlots ?? Array.Empty<EncounterEnemySlotAsset>())
            {
                if (slot == null || slot.Monster == null)
                {
                    continue;
                }

                var count = Mathf.Max(1, slot.Count);
                for (var i = 0; i < count; i++)
                {
                    SpawnMonster(slot.Monster, string.IsNullOrWhiteSpace(slot.SlotId) ? "Slot" : slot.SlotId, SpawnPosition(spawned));
                    spawned++;
                }
            }
        }

        public void PreviewSpawnTable(int seed)
        {
            if (SpawnTable == null)
            {
                return;
            }

            if (ClearBeforePreview)
            {
                ClearPreview();
            }

            var selection = SelectSpawnTableEntry(SpawnTable, seed);
            if (selection.Encounter != null)
            {
                Encounter = selection.Encounter;
                PreviewEncounter();
                return;
            }

            if (selection.Monster != null)
            {
                SpawnMonster(selection.Monster, "SpawnTable Monster", SpawnPosition(0));
            }
        }

        public void ClearPreview()
        {
            var root = ResolvePreviewRoot();
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private GameObject SpawnMonster(MonsterDefinitionAsset monster, string label, Vector3 position)
        {
            var prefab = monster.Prefab;
            var instance = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity, ResolvePreviewRoot())
                : GameObject.CreatePrimitive(PrimitiveType.Capsule);

            instance.name = $"{label} - {monster.DisplayNameOrId()}";
            if (prefab == null)
            {
                instance.transform.SetParent(ResolvePreviewRoot(), false);
                instance.transform.position = position;
                instance.transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);
            }

            return instance;
        }

        private Transform ResolvePreviewRoot()
        {
            return PreviewRoot != null ? PreviewRoot : transform;
        }

        private Vector3 SpawnPosition(int index)
        {
            if (SpawnPoints != null && index >= 0 && index < SpawnPoints.Length && SpawnPoints[index] != null)
            {
                return SpawnPoints[index].position;
            }

            var origin = ResolvePreviewRoot().position;
            return origin + new Vector3(index * Mathf.Max(0.1f, EncounterSpacing), 0f, 0f);
        }

        private static SpawnTableSelection SelectSpawnTableEntry(SpawnTableAsset spawnTable, int seed)
        {
            var totalWeight = 0;
            foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
            {
                if (entry != null && entry.Encounter != null && entry.Weight > 0)
                {
                    totalWeight += entry.Weight;
                }
            }

            foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
            {
                if (entry != null && entry.Monster != null && entry.Weight > 0)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return default;
            }

            var roll = PositiveHash(seed, spawnTable.Id, "spawn_table") % totalWeight;
            foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
            {
                if (entry != null && entry.Encounter != null && entry.Weight > 0)
                {
                    if (roll < entry.Weight)
                    {
                        return SpawnTableSelection.ForEncounter(entry.Encounter);
                    }

                    roll -= entry.Weight;
                }
            }

            foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
            {
                if (entry != null && entry.Monster != null && entry.Weight > 0)
                {
                    if (roll < entry.Weight)
                    {
                        return SpawnTableSelection.ForMonster(entry.Monster);
                    }

                    roll -= entry.Weight;
                }
            }

            return default;
        }

        private static int PositiveHash(int seed, string a, string b)
        {
            unchecked
            {
                var hash = seed;
                hash = (hash * 397) ^ StringHash(a);
                hash = (hash * 397) ^ StringHash(b);
                return hash & int.MaxValue;
            }
        }

        private static int StringHash(string value)
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in value ?? string.Empty)
                {
                    hash = hash * 31 + c;
                }

                return hash;
            }
        }
    }

    internal readonly struct SpawnTableSelection
    {
        private SpawnTableSelection(EncounterDefinitionAsset encounter, MonsterDefinitionAsset monster)
        {
            Encounter = encounter;
            Monster = monster;
        }

        public EncounterDefinitionAsset Encounter { get; }
        public MonsterDefinitionAsset Monster { get; }

        public static SpawnTableSelection ForEncounter(EncounterDefinitionAsset encounter)
        {
            return new SpawnTableSelection(encounter, null);
        }

        public static SpawnTableSelection ForMonster(MonsterDefinitionAsset monster)
        {
            return new SpawnTableSelection(null, monster);
        }
    }

    internal static class MonsterDefinitionAssetDisplayExtensions
    {
        public static string DisplayNameOrId(this MonsterDefinitionAsset monster)
        {
            if (monster == null)
            {
                return "Missing";
            }

            if (!string.IsNullOrWhiteSpace(monster.DisplayName))
            {
                return monster.DisplayName;
            }

            return string.IsNullOrWhiteSpace(monster.Id) ? monster.name : monster.Id;
        }
    }
}

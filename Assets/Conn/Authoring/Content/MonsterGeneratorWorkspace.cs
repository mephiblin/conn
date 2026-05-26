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

        [Header("Create Monster")]
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

            var monster = SelectDirectMonster(SpawnTable, seed);
            if (monster != null)
            {
                SpawnMonster(monster, "SpawnTable Monster", SpawnPosition(0));
                return;
            }

            var encounter = SelectEncounter(SpawnTable, seed);
            if (encounter != null)
            {
                Encounter = encounter;
                PreviewEncounter();
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

        private static MonsterDefinitionAsset SelectDirectMonster(SpawnTableAsset spawnTable, int seed)
        {
            var entries = spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>();
            var totalWeight = 0;
            foreach (var entry in entries)
            {
                if (entry != null && entry.Monster != null && entry.Weight > 0)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = PositiveHash(seed, spawnTable.Id, "monster") % totalWeight;
            foreach (var entry in entries)
            {
                if (entry == null || entry.Monster == null || entry.Weight <= 0)
                {
                    continue;
                }

                if (roll < entry.Weight)
                {
                    return entry.Monster;
                }

                roll -= entry.Weight;
            }

            return null;
        }

        private static EncounterDefinitionAsset SelectEncounter(SpawnTableAsset spawnTable, int seed)
        {
            var entries = spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>();
            var totalWeight = 0;
            foreach (var entry in entries)
            {
                if (entry != null && entry.Encounter != null && entry.Weight > 0)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = PositiveHash(seed, spawnTable.Id, "encounter") % totalWeight;
            foreach (var entry in entries)
            {
                if (entry == null || entry.Encounter == null || entry.Weight <= 0)
                {
                    continue;
                }

                if (roll < entry.Weight)
                {
                    return entry.Encounter;
                }

                roll -= entry.Weight;
            }

            return null;
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

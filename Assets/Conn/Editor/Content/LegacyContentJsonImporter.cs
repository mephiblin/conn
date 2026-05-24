using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Conn.Core.Content;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Content
{
    public static class LegacyContentJsonImporter
    {
        public const string DefaultLegacyDataPath = "doc/webgame_ref/src/data";
        public const string DefaultDatabaseAssetPath = "Assets/Conn/Core/Content/ContentDatabase.asset";

        [MenuItem("Conn/Content Database/Import Legacy JSON")]
        public static void ImportDefaultLegacyJson()
        {
            var database = Import(DefaultLegacyDataPath, DefaultDatabaseAssetPath);
            Debug.Log($"Imported legacy content database: {AssetDatabase.GetAssetPath(database)}");
        }

        public static ContentDatabaseDefinition Import(string legacyDataPath, string assetPath)
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(assetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
                AssetDatabase.CreateAsset(database, assetPath);
            }

            database.Items = ImportItems(Path.Combine(legacyDataPath, "items.json"));
            database.Equipment = System.Array.Empty<ContentEquipmentDefinition>();
            database.Skills = ImportSkills(Path.Combine(legacyDataPath, "skills.json"));
            database.Monsters = ImportMonsters(Path.Combine(legacyDataPath, "monsters.json"));
            database.Quests = ImportQuests(Path.Combine(legacyDataPath, "quests.json"));
            database.Vendors = ImportVendors(Path.Combine(legacyDataPath, "vendors.json"), Path.Combine(legacyDataPath, "npcs.json"));
            database.Npcs = ImportNpcs(Path.Combine(legacyDataPath, "npcs.json"));

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return database;
        }

        private static ContentItemDefinition[] ImportItems(string path)
        {
            var result = new List<ContentItemDefinition>();
            foreach (var entry in ReadObject(path))
            {
                var data = AsObject(entry.Value);
                result.Add(new ContentItemDefinition
                {
                    Id = entry.Key,
                    DisplayName = Text(data, "name", entry.Key),
                    Kind = Text(data, "kind", "item"),
                    BuyPrice = Int(data, "buyPrice"),
                    SellPrice = Int(data, "sellPrice"),
                    HealAmount = Int(data, "heal")
                });
            }

            return result.ToArray();
        }

        private static ContentSkillDefinition[] ImportSkills(string path)
        {
            var result = new List<ContentSkillDefinition>();
            foreach (var entry in ReadObject(path))
            {
                var data = AsObject(entry.Value);
                result.Add(new ContentSkillDefinition
                {
                    Id = entry.Key,
                    DisplayName = Text(data, "name", entry.Key),
                    EffectKind = Text(data, "kind", "support"),
                    TargetMode = Text(data, "targetMode", string.Empty),
                    Formula = Text(data, "formula", string.Empty),
                    BuyPrice = Int(data, "buyPrice"),
                    SellPrice = Int(data, "sellPrice"),
                    Power = Int(data, "effect"),
                    CatalogIds = StringArray(data, "catalogIds")
                });
            }

            return result.ToArray();
        }

        private static ContentMonsterDefinition[] ImportMonsters(string path)
        {
            var result = new List<ContentMonsterDefinition>();
            foreach (var entry in ReadObject(path))
            {
                var data = AsObject(entry.Value);
                result.Add(new ContentMonsterDefinition
                {
                    Id = entry.Key,
                    DisplayName = Text(data, "name", entry.Key),
                    MaxHp = Int(data, "hp"),
                    AttackPower = Int(data, "atk"),
                    Defense = Int(data, "def"),
                    XpReward = Int(data, "xp"),
                    Boss = Bool(data, "boss"),
                    Ai = Text(data, "ai", string.Empty)
                });
            }

            return result.ToArray();
        }

        private static ContentQuestDefinition[] ImportQuests(string path)
        {
            var result = new List<ContentQuestDefinition>();
            foreach (var entry in ReadObject(path))
            {
                var data = AsObject(entry.Value);
                var rewards = Object(data, "rewards");
                result.Add(new ContentQuestDefinition
                {
                    Id = entry.Key,
                    DisplayName = Text(data, "name", entry.Key),
                    Description = Text(data, "description", string.Empty),
                    MapKind = Text(data, "mapKind", string.Empty),
                    GoldReward = Int(rewards, "gold"),
                    XpReward = Int(rewards, "xp"),
                    RewardItems = ItemStacks(rewards, "items")
                });
            }

            return result.ToArray();
        }

        private static ContentVendorDefinition[] ImportVendors(string path, string npcPath)
        {
            var result = new List<ContentVendorDefinition>();
            foreach (var entry in ReadObject(path))
            {
                var data = AsObject(entry.Value);
                result.Add(new ContentVendorDefinition
                {
                    Id = entry.Key,
                    ServiceType = Text(data, "serviceType", string.Empty),
                    GoldCost = Int(Object(data, "cost"), "gold"),
                    Summary = Text(data, "summary", string.Empty),
                    StockItemIds = StringArray(data, "inventory")
                });
            }

            foreach (var vendor in ImportNpcSkillShopVendors(npcPath))
            {
                result.Add(vendor);
            }

            return result.ToArray();
        }

        private static IEnumerable<ContentVendorDefinition> ImportNpcSkillShopVendors(string path)
        {
            foreach (var entry in ReadObject(path))
            {
                var data = AsObject(entry.Value);
                foreach (var service in Array(data, "services"))
                {
                    var serviceData = AsObject(service);
                    if (Text(serviceData, "type", string.Empty) != "skill_shop")
                    {
                        continue;
                    }

                    var catalogId = Text(serviceData, "catalogId", string.Empty);
                    yield return new ContentVendorDefinition
                    {
                        Id = string.IsNullOrWhiteSpace(catalogId) ? $"{entry.Key}_skill_shop" : catalogId,
                        ServiceType = "skill_shop",
                        Summary = Text(serviceData, "note", string.Empty),
                        StockSkillIds = StringArray(serviceData, "skillIds")
                    };
                }
            }
        }

        private static ContentNpcDefinition[] ImportNpcs(string path)
        {
            var result = new List<ContentNpcDefinition>();
            foreach (var entry in ReadObject(path))
            {
                var data = AsObject(entry.Value);
                var serviceTypes = new List<string>();
                foreach (var service in Array(data, "services"))
                {
                    var serviceData = AsObject(service);
                    serviceTypes.Add(Text(serviceData, "type", string.Empty));
                }

                result.Add(new ContentNpcDefinition
                {
                    Id = entry.Key,
                    DisplayName = Text(data, "name", entry.Key),
                    Description = Text(data, "description", string.Empty),
                    ServiceType = string.Join(",", serviceTypes.FindAll(value => !string.IsNullOrWhiteSpace(value))),
                    VendorId = GuessVendorId(entry.Key),
                    QuestIds = QuestSeedIds(data)
                });
            }

            return result.ToArray();
        }

        private static string GuessVendorId(string npcId)
        {
            if (npcId == "npc_innkeeper") return "vendor_inn";
            if (npcId == "npc_trainer") return "vendor_trainer";
            if (npcId == "npc_smith") return "vendor_smith";
            if (npcId == "npc_apothecary") return "vendor_apothecary";
            return string.Empty;
        }

        private static string[] QuestSeedIds(Dictionary<string, object> data)
        {
            var ids = new List<string>();
            foreach (var seed in Array(data, "questSeeds"))
            {
                var seedData = AsObject(seed);
                var id = Text(seedData, "id", string.Empty);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }

            return ids.ToArray();
        }

        private static Dictionary<string, object> ReadObject(string path)
        {
            return AsObject(MiniJson.Parse(File.ReadAllText(path)));
        }

        private static Dictionary<string, object> AsObject(object value)
        {
            return value as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        private static Dictionary<string, object> Object(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? AsObject(value) : new Dictionary<string, object>();
        }

        private static IEnumerable<object> Array(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) && value is List<object> list ? list : System.Array.Empty<object>();
        }

        private static string Text(Dictionary<string, object> data, string key, string fallback)
        {
            return data.TryGetValue(key, out var value) && value != null ? value.ToString() : fallback;
        }

        private static int Int(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var value) || value == null)
            {
                return 0;
            }

            return value is long longValue ? (int)longValue : Convert.ToInt32(value);
        }

        private static bool Bool(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) && value is bool boolValue && boolValue;
        }

        private static string[] StringArray(Dictionary<string, object> data, string key)
        {
            var result = new List<string>();
            foreach (var value in Array(data, key))
            {
                if (value != null)
                {
                    result.Add(value.ToString());
                }
            }

            return result.ToArray();
        }

        private static ContentItemStack[] ItemStacks(Dictionary<string, object> data, string key)
        {
            var result = new List<ContentItemStack>();
            foreach (var value in Array(data, key))
            {
                var stack = AsObject(value);
                result.Add(new ContentItemStack
                {
                    ItemId = Text(stack, "itemId", string.Empty),
                    Quantity = Math.Max(1, Int(stack, "quantity"))
                });
            }

            return result.ToArray();
        }

        private sealed class MiniJson
        {
            private readonly string json;
            private int index;

            private MiniJson(string json)
            {
                this.json = json;
            }

            public static object Parse(string json)
            {
                return new MiniJson(json).ParseValue();
            }

            private object ParseValue()
            {
                Skip();
                if (index >= json.Length) return null;
                var c = json[index];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == 't' || c == 'f') return ParseBool();
                if (c == 'n')
                {
                    index += 4;
                    return null;
                }

                return ParseNumber();
            }

            private Dictionary<string, object> ParseObject()
            {
                var result = new Dictionary<string, object>();
                index++;
                while (true)
                {
                    Skip();
                    if (json[index] == '}')
                    {
                        index++;
                        return result;
                    }

                    var key = ParseString();
                    Skip();
                    index++;
                    result[key] = ParseValue();
                    Skip();
                    if (json[index] == ',')
                    {
                        index++;
                    }
                }
            }

            private List<object> ParseArray()
            {
                var result = new List<object>();
                index++;
                while (true)
                {
                    Skip();
                    if (json[index] == ']')
                    {
                        index++;
                        return result;
                    }

                    result.Add(ParseValue());
                    Skip();
                    if (json[index] == ',')
                    {
                        index++;
                    }
                }
            }

            private string ParseString()
            {
                index++;
                var chars = new List<char>();
                while (index < json.Length)
                {
                    var c = json[index++];
                    if (c == '"') return new string(chars.ToArray());
                    if (c != '\\')
                    {
                        chars.Add(c);
                        continue;
                    }

                    var escape = json[index++];
                    if (escape == 'u')
                    {
                        chars.Add((char)Convert.ToInt32(json.Substring(index, 4), 16));
                        index += 4;
                    }
                    else
                    {
                        chars.Add(escape == 'n' ? '\n' : escape == 't' ? '\t' : escape);
                    }
                }

                return new string(chars.ToArray());
            }

            private bool ParseBool()
            {
                if (json[index] == 't')
                {
                    index += 4;
                    return true;
                }

                index += 5;
                return false;
            }

            private object ParseNumber()
            {
                var start = index;
                while (index < json.Length && "-0123456789.eE+".IndexOf(json[index]) >= 0)
                {
                    index++;
                }

                var text = json.Substring(start, index - start);
                return text.IndexOf('.') >= 0 ? (object)double.Parse(text) : long.Parse(text);
            }

            private void Skip()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                {
                    index++;
                }
            }
        }
    }
}

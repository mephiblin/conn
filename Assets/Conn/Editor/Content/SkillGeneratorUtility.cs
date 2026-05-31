using System;
using System.Collections.Generic;
using Conn.Core.Content;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Content
{
    public static class SkillGeneratorUtility
    {
        private const string DefaultCatalogId = "merchant_basic";

        [MenuItem("Conn/Content Database/Generate Skill Starter Set")]
        public static void GenerateStarterSet()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabaseDefinition>(LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ContentDatabaseDefinition>();
                AssetDatabase.CreateAsset(database, LegacyContentJsonImporter.DefaultDatabaseAssetPath);
            }

            var skills = new List<ContentSkillDefinition>(database.Skills ?? Array.Empty<ContentSkillDefinition>());
            Upsert(skills, Skill("generated_skill_cleave", "Cleave", "attack", "enemy", "die_plus_effect", 2, 2, 8, 4, "melee", "Generated attack skill."));
            Upsert(skills, Skill("generated_skill_guard", "Guard Line", "guard", "self", "die_plus_effect", 2, 2, 8, 4, "defense", "Generated guard skill."));
            Upsert(skills, Skill("generated_skill_mend", "Mend", "heal", "ally", "die_plus_effect", 3, 2, 9, 4, "heal", "Generated heal skill."));
            Upsert(skills, Skill("generated_skill_mark", "Mark Weakness", "support", "enemy", "die_as_effect", 0, 1, 7, 3, "support", "Generated support skill."));
            Upsert(skills, Skill("generated_skill_focus", "Battle Focus", "buff", "self", "die_as_effect", 0, 3, 10, 5, "buff", "Generated buff skill."));
            Upsert(skills, Skill("generated_skill_rattle", "Rattle Curse", "debuff", "enemy", "die_minus_effect", 1, 3, 10, 5, "debuff", "Generated debuff skill."));
            Upsert(skills, Skill("generated_skill_siphon", "Soul Siphon", "lifesteal", "enemy", "die_divide_effect", 2, 4, 12, 6, "lifesteal", "Generated lifesteal skill."));
            Upsert(skills, Skill("generated_skill_echo", "Guardian Echo", "summon", "self", "die_equals_effect", 1, 5, 14, 7, "summon", "Generated summon skill."));

            database.Skills = skills.ToArray();
            if (string.IsNullOrWhiteSpace(database.StarterSkillId))
            {
                database.StarterSkillId = "generated_skill_cleave";
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated skill starter set into {AssetDatabase.GetAssetPath(database)}");
        }

        private static ContentSkillDefinition Skill(
            string id,
            string displayName,
            string effectKind,
            string targetMode,
            string formula,
            int power,
            int cooldown,
            int buyPrice,
            int sellPrice,
            string tag,
            string description)
        {
            return new ContentSkillDefinition
            {
                Id = id,
                DisplayName = displayName,
                EffectKind = effectKind,
                TargetMode = targetMode,
                Formula = formula,
                Power = power,
                Cooldown = cooldown,
                BuyPrice = buyPrice,
                SellPrice = sellPrice,
                Tags = new[] { tag, "generated" },
                CatalogIds = new[] { DefaultCatalogId },
                Description = description
            };
        }

        private static void Upsert(List<ContentSkillDefinition> skills, ContentSkillDefinition generated)
        {
            for (var i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null && skills[i].Id == generated.Id)
                {
                    skills[i] = generated;
                    return;
                }
            }

            skills.Add(generated);
        }
    }
}

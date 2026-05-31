using System;
using System.Collections.Generic;
using Conn.Core.Equipment;
using Conn.Core.Skills;
using Conn.Runtime.Content;
using UnityEngine;

namespace Conn.UI.Runtime
{
    public static class ShopIconSpriteResolver
    {
        public const string ResourceRoot = "Conn/UI/Art/ShopIcons";

        private const string EquipmentRoot = ResourceRoot + "/Equipment";
        private const string SkillRoot = ResourceRoot + "/Skills";
        private const string FallbackPath = ResourceRoot + "/shop_icon_fallback";

        private static readonly Dictionary<string, string> EquipmentPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { EquipmentCatalog.RustySwordId, EquipmentRoot + "/shop_icon_rusty_sword" },
            { EquipmentCatalog.IronShieldId, EquipmentRoot + "/shop_icon_iron_shield" },
            { EquipmentCatalog.GreatAxeId, EquipmentRoot + "/shop_icon_great_axe" },
            { EquipmentCatalog.LeatherCapId, EquipmentRoot + "/shop_icon_leather_cap" },
            { EquipmentCatalog.PaddedVestId, EquipmentRoot + "/shop_icon_padded_vest" },
            { EquipmentCatalog.TravelerGlovesId, EquipmentRoot + "/shop_icon_traveler_gloves" },
            { EquipmentCatalog.ReinforcedPantsId, EquipmentRoot + "/shop_icon_reinforced_pants" },
            { EquipmentCatalog.WornBootsId, EquipmentRoot + "/shop_icon_worn_boots" }
        };

        private static readonly Dictionary<EquipmentKind, string> EquipmentKindPaths = new Dictionary<EquipmentKind, string>
        {
            { EquipmentKind.OneHandWeapon, EquipmentRoot + "/shop_icon_one_hand_weapon" },
            { EquipmentKind.TwoHandWeapon, EquipmentRoot + "/shop_icon_two_hand_weapon" },
            { EquipmentKind.Shield, EquipmentRoot + "/shop_icon_shield" },
            { EquipmentKind.HeadArmor, EquipmentRoot + "/shop_icon_head_armor" },
            { EquipmentKind.ChestArmor, EquipmentRoot + "/shop_icon_chest_armor" },
            { EquipmentKind.ArmsArmor, EquipmentRoot + "/shop_icon_arms_armor" },
            { EquipmentKind.LegsArmor, EquipmentRoot + "/shop_icon_legs_armor" },
            { EquipmentKind.FeetArmor, EquipmentRoot + "/shop_icon_feet_armor" }
        };

        private static readonly Dictionary<string, string> SkillPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { SkillCatalog.SlashId, SkillRoot + "/shop_icon_skill_slash" },
            { SkillCatalog.GuardId, SkillRoot + "/shop_icon_skill_guard" },
            { SkillCatalog.FocusStrikeId, SkillRoot + "/shop_icon_skill_focus_strike" },
            { SkillCatalog.MendId, SkillRoot + "/shop_icon_skill_mend" },
            { "skill_berserk", SkillRoot + "/shop_icon_skill_berserk" },
            { "skill_guard_stance", SkillRoot + "/shop_icon_skill_guard_stance" },
            { "skill_vital_stab", SkillRoot + "/shop_icon_skill_vital_stab" },
            { "skill_piercing_shot", SkillRoot + "/shop_icon_skill_piercing_shot" },
            { "skill_serpent_phantasm", SkillRoot + "/shop_icon_skill_serpent_phantasm" },
            { "skill_purify", SkillRoot + "/shop_icon_skill_purify" },
            { "skill_weakness_read", SkillRoot + "/shop_icon_skill_weakness_read" },
            { "skill_blood_cleave", SkillRoot + "/shop_icon_skill_blood_cleave" },
            { "skill_bastion_vow", SkillRoot + "/shop_icon_skill_bastion_vow" },
            { "skill_chain_lightning", SkillRoot + "/shop_icon_skill_chain_lightning" },
            { "skill_mending_wave", SkillRoot + "/shop_icon_skill_mending_wave" },
            { "skill_venom_mark", SkillRoot + "/shop_icon_skill_venom_mark" },
            { "skill_storm_bolt", SkillRoot + "/shop_icon_skill_storm_bolt" },
            { "skill_thunder_clap", SkillRoot + "/shop_icon_skill_thunder_clap" },
            { "skill_holy_light", SkillRoot + "/shop_icon_skill_holy_light" },
            { "skill_death_coil", SkillRoot + "/shop_icon_skill_death_coil" },
            { "skill_avatar_vow", SkillRoot + "/shop_icon_skill_avatar_vow" },
            { "skill_mana_burn", SkillRoot + "/shop_icon_skill_mana_burn" },
            { "skill_divide_guard", SkillRoot + "/shop_icon_skill_divide_guard" },
            { "skill_summon_serpent_egg", SkillRoot + "/shop_icon_skill_summon_serpent_egg" },
            { "phase6_skill_quick_cut", SkillRoot + "/shop_icon_phase6_skill_quick_cut" },
            { "phase6_skill_guard_step", SkillRoot + "/shop_icon_phase6_skill_guard_step" },
            { "phase6_skill_field_mend", SkillRoot + "/shop_icon_phase6_skill_field_mend" },
            { "phase6_skill_smoke_feint", SkillRoot + "/shop_icon_phase6_skill_smoke_feint" },
            { "phase6_skill_battle_focus", SkillRoot + "/shop_icon_phase6_skill_battle_focus" },
            { "phase6_skill_bleeding_edge", SkillRoot + "/shop_icon_phase6_skill_bleeding_edge" },
            { "phase6_skill_soul_siphon", SkillRoot + "/shop_icon_phase6_skill_soul_siphon" },
            { "phase6_skill_stonebreaker", SkillRoot + "/shop_icon_phase6_skill_stonebreaker" },
            { "phase6_skill_guardian_echo", SkillRoot + "/shop_icon_phase6_skill_guardian_echo" },
            { "phase6_skill_last_light", SkillRoot + "/shop_icon_phase6_skill_last_light" },
            { "phase6_skill_rallying_cry", SkillRoot + "/shop_icon_phase6_skill_rallying_cry" },
            { "phase6_skill_rattle_curse", SkillRoot + "/shop_icon_phase6_skill_rattle_curse" }
        };

        private static readonly Dictionary<SkillEffectKind, string> SkillEffectPaths = new Dictionary<SkillEffectKind, string>
        {
            { SkillEffectKind.Attack, SkillRoot + "/shop_icon_attack" },
            { SkillEffectKind.Guard, SkillRoot + "/shop_icon_guard" },
            { SkillEffectKind.Heal, SkillRoot + "/shop_icon_heal" },
            { SkillEffectKind.Support, SkillRoot + "/shop_icon_support" },
            { SkillEffectKind.Buff, SkillRoot + "/shop_icon_buff" },
            { SkillEffectKind.Debuff, SkillRoot + "/shop_icon_debuff" },
            { SkillEffectKind.Lifesteal, SkillRoot + "/shop_icon_lifesteal" },
            { SkillEffectKind.Summon, SkillRoot + "/shop_icon_summon" }
        };

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public static Sprite EquipmentSpriteFor(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return LoadFirst(FallbackPath);
            }

            var item = RuntimeContentDatabase.FindEquipment(itemId);
            return item != null
                ? EquipmentSpriteFor(itemId, item.Kind)
                : LoadFirst(EquipmentPathFor(itemId), FallbackPath);
        }

        public static Sprite EquipmentSpriteFor(string itemId, EquipmentKind fallbackKind)
        {
            return LoadFirst(EquipmentPathFor(itemId), EquipmentPathFor(fallbackKind), FallbackPath);
        }

        public static Sprite EquipmentSpriteFor(EquipmentKind kind)
        {
            return LoadFirst(EquipmentPathFor(kind), FallbackPath);
        }

        public static Sprite SkillSpriteFor(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return LoadFirst(FallbackPath);
            }

            var skill = RuntimeContentDatabase.FindSkill(skillId);
            return skill != null
                ? SkillSpriteFor(skillId, skill.EffectKind)
                : LoadFirst(SkillPathFor(skillId), FallbackPath);
        }

        public static Sprite SkillSpriteFor(string skillId, SkillEffectKind fallbackEffectKind)
        {
            return LoadFirst(SkillPathFor(skillId), SkillPathFor(fallbackEffectKind), FallbackPath);
        }

        public static Sprite SkillSpriteFor(SkillEffectKind effectKind)
        {
            return LoadFirst(SkillPathFor(effectKind), FallbackPath);
        }

        public static string EquipmentPathFor(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && EquipmentPaths.TryGetValue(itemId, out var path)
                ? path
                : string.Empty;
        }

        public static string EquipmentPathFor(EquipmentKind kind)
        {
            return EquipmentKindPaths.TryGetValue(kind, out var path) ? path : FallbackPath;
        }

        public static string SkillPathFor(string skillId)
        {
            return !string.IsNullOrWhiteSpace(skillId) && SkillPaths.TryGetValue(skillId, out var path)
                ? path
                : string.Empty;
        }

        public static string SkillPathFor(SkillEffectKind effectKind)
        {
            return SkillEffectPaths.TryGetValue(effectKind, out var path) ? path : FallbackPath;
        }

        private static Sprite LoadFirst(params string[] paths)
        {
            for (var i = 0; i < paths.Length; i++)
            {
                var sprite = Load(paths[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Sprite Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (SpriteCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                SpriteCache[path] = sprite;
            }

            return sprite;
        }
    }
}

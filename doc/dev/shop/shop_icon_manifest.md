# Shop Icon Manifest

Generated shop icons should be placed in
`Assets/Conn/UI/Art/Resources/Conn/UI/Art/ShopIcons` using the filenames below.
Runtime code loads them through `Resources.Load<Sprite>` with the root
`Conn/UI/Art/ShopIcons`.

Sources inspected:
- Runtime equipment shop: `EquipmentShopRuntimeService.BlacksmithStockItemIds()` -> `vendor_smith`
- Runtime skill shop: `SkillShopRuntimeService.SkillMerchantStock()` -> `merchant_basic`
- Phase 6 authoring shop assets: `phase6_vendor_wayfarer_smith`, `phase6_vendor_tactical_trainer`, `phase6_vendor_relic_peddler`

## Equipment

| id | desired filename | source | visual concept |
| --- | --- | --- | --- |
| `rusty_sword` | `shop_icon_rusty_sword.png` | starter/fallback equipment | Rusty one-handed sword, worn steel blade, readable weapon fallback. |
| `iron_shield` | `shop_icon_iron_shield.png` | runtime `vendor_smith`, phase6 wayfarer smith | Round iron shield, worn rim, cool steel face with a small defensive gleam. |
| `leather_cap` | `shop_icon_leather_cap.png` | runtime `vendor_smith`, phase6 wayfarer smith | Soft brown leather cap with stitched panels and a small adventurer scuff. |
| `padded_vest` | `shop_icon_padded_vest.png` | runtime `vendor_smith`, phase6 wayfarer smith | Quilted cloth vest with reinforced seams, front-facing and readable at small size. |
| `traveler_gloves` | `shop_icon_traveler_gloves.png` | runtime `vendor_smith` | Pair of fingerless travel gloves, tan leather palms and dark cloth cuffs. |
| `reinforced_pants` | `shop_icon_reinforced_pants.png` | runtime `vendor_smith`, phase6 wayfarer smith | Rugged trousers with knee plates and belt straps, sturdy but lightweight. |
| `worn_boots` | `shop_icon_worn_boots.png` | runtime `vendor_smith` | Pair of worn boots with patched toes, practical travel gear silhouette. |
| `great_axe` | `shop_icon_great_axe.png` | runtime `vendor_smith` rotation, phase6 wayfarer smith rotation | Heavy two-handed axe head with nicked steel edge and wrapped wooden haft. |

## Runtime Skills

| id | desired filename | source | visual concept |
| --- | --- | --- | --- |
| `skill_berserk` | `shop_icon_skill_berserk.png` | runtime `merchant_basic` | Red battle aura around a raised blade, aggressive attack icon. |
| `skill_guard_stance` | `shop_icon_skill_guard_stance.png` | runtime `merchant_basic` | Shield planted before a figure, blue guard lines forming a stance. |
| `skill_vital_stab` | `shop_icon_skill_vital_stab.png` | runtime `merchant_basic` | Dagger point striking a bright weak point mark. |
| `skill_piercing_shot` | `shop_icon_skill_piercing_shot.png` | runtime `merchant_basic` | Arrow punching through layered target rings with a clean trail. |
| `skill_serpent_phantasm` | `shop_icon_skill_serpent_phantasm.png` | runtime `merchant_basic` | Ghostly serpent silhouette coiling from a blade slash. |
| `skill_purify` | `shop_icon_skill_purify.png` | runtime `merchant_basic` | White-gold cleansing flame dissolving dark motes. |
| `skill_weakness_read` | `shop_icon_skill_weakness_read.png` | runtime `merchant_basic` | Focused eye over a marked enemy plate or cracked target. |
| `skill_blood_cleave` | `shop_icon_skill_blood_cleave.png` | runtime `merchant_basic` | Crimson sweeping axe arc with droplets kept stylized. |
| `skill_bastion_vow` | `shop_icon_skill_bastion_vow.png` | runtime `merchant_basic` | Fortress shield and oath ribbon, defensive gold-blue palette. |
| `skill_chain_lightning` | `shop_icon_skill_chain_lightning.png` | runtime `merchant_basic` | Forking lightning linking two small enemy silhouettes. |
| `skill_mending_wave` | `shop_icon_skill_mending_wave.png` | runtime `merchant_basic` | Green-blue healing wave with plus-shaped spark highlights. |
| `skill_venom_mark` | `shop_icon_skill_venom_mark.png` | runtime `merchant_basic` | Poison sigil stamped on a target, green venom drop accent. |
| `skill_storm_bolt` | `shop_icon_skill_storm_bolt.png` | runtime `merchant_basic` | Dark cloud bolt striking down, debuff energy ring. |
| `skill_thunder_clap` | `shop_icon_skill_thunder_clap.png` | runtime `merchant_basic` | Shockwave from clashing hands or gauntlets, yellow thunder burst. |
| `skill_holy_light` | `shop_icon_skill_holy_light.png` | runtime `merchant_basic` | Radiant beam over a small healing cross, warm white center. |
| `skill_death_coil` | `shop_icon_skill_death_coil.png` | runtime `merchant_basic` | Purple-black coil drawing life into a skull-shaped spark. |
| `skill_avatar_vow` | `shop_icon_skill_avatar_vow.png` | runtime `merchant_basic` direct stock | Golden empowered silhouette with oath halo and rising flame. |
| `skill_mana_burn` | `shop_icon_skill_mana_burn.png` | runtime `merchant_basic` | Blue mana flame turning orange at the edges, draining glyph. |
| `skill_divide_guard` | `shop_icon_skill_divide_guard.png` | runtime `merchant_basic` | Split shield icon with two mirrored guard halves. |
| `skill_summon_serpent_egg` | `shop_icon_skill_summon_serpent_egg.png` | runtime `merchant_basic` direct stock | Cracked serpent egg glowing green with a tiny spectral coil. |

## Phase 6 Authoring Skills

| id | desired filename | source | visual concept |
| --- | --- | --- | --- |
| `phase6_skill_quick_cut` | `shop_icon_phase6_skill_quick_cut.png` | phase6 tactical trainer | Fast diagonal blade streak, compact and sharp. |
| `phase6_skill_guard_step` | `shop_icon_phase6_skill_guard_step.png` | phase6 tactical trainer | Boot stepping behind a small shield, motion line showing repositioning. |
| `phase6_skill_field_mend` | `shop_icon_phase6_skill_field_mend.png` | phase6 tactical trainer | Bandaged hand with green healing thread, field-care feel. |
| `phase6_skill_smoke_feint` | `shop_icon_phase6_skill_smoke_feint.png` | phase6 tactical trainer rotation | Smoke cloud masking a dagger silhouette, evasive support icon. |
| `phase6_skill_battle_focus` | `shop_icon_phase6_skill_battle_focus.png` | phase6 tactical trainer rotation | Bright focus mark centered over a helm or eye, disciplined buff read. |
| `phase6_skill_bleeding_edge` | `shop_icon_phase6_skill_bleeding_edge.png` | phase6 relic peddler | Razor edge with crimson glint, stylized bleed trail. |
| `phase6_skill_soul_siphon` | `shop_icon_phase6_skill_soul_siphon.png` | phase6 relic peddler | Pale soul stream pulled into a dark hand or vial. |
| `phase6_skill_stonebreaker` | `shop_icon_phase6_skill_stonebreaker.png` | phase6 relic peddler | Hammer or blade cracking a stone slab, dust burst. |
| `phase6_skill_guardian_echo` | `shop_icon_phase6_skill_guardian_echo.png` | phase6 relic peddler rotation | Translucent guardian shield echo behind a small figure. |
| `phase6_skill_last_light` | `shop_icon_phase6_skill_last_light.png` | phase6 relic peddler rotation | Final lantern-like light over a wounded silhouette, hopeful heal icon. |

## Ambiguity Notes

- `phase6_skill_rallying_cry` and `phase6_skill_rattle_curse` exist as Phase 6 skill assets with buy prices, but no inspected Phase 6 vendor stock or rotation references them.
- Phase 6 vendor ids are authoring content ids. The current runtime services still call `vendor_smith` and `merchant_basic` directly.
- Apothecary stock uses consumable item ids (`bandage`, `antivenom`, `throwing_knife`, `firebomb`), outside this equipment/skill icon manifest.

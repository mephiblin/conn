# Data Pipeline Status

Last updated: 2026-05-25

This audit captures the baseline before expanding the Content Database editor.
The current goal is DB-first runtime consumption with catalog fallback preserved
until each authored path is validated.

## Runtime DB-First Paths

| Area | DB-first entry point | Runtime consumer | Current fallback |
| --- | --- | --- | --- |
| Active database binding | `SceneBootstrap.ContentDatabase` calls `RuntimeContentDatabase.SetActive` | All runtime scenes created by `P0SceneBuilder` | Null database keeps catalog resolvers active |
| Monsters | `RuntimeContentDatabase.FindMonster` | `CombatRuntimeService`, encounter slot display | `MonsterCatalog.Find` |
| Encounters | `RuntimeContentDatabase.FindEncounter`, `FindEncounterForMonster` | `CombatRuntimeService.StartTestCombat` | `EncounterCatalog.Find`, `EncounterCatalog.FindForMonster` |
| Quests | `RuntimeContentDatabase.FindQuest`, `BoardQuestAt` | `QuestRuntimeService`, Quest Board | `QuestCatalog.Find`, `QuestCatalog.BoardOffer` |
| Equipment | `RuntimeContentDatabase.FindEquipment` | `EquipmentRuntimeService`, `EquipmentShopRuntimeService`, `PlayerEquipmentState.EquipmentResolver`, display helpers | `EquipmentCatalog.Find` |
| Consumables | `RuntimeContentDatabase.FindConsumable` | `ConsumableRuntimeService` | `ConsumableCatalog.Find` |
| Skills | `RuntimeContentDatabase.FindSkill` | `SkillRuntimeService`, `SkillShopRuntimeService`, combat dice display | `SkillCatalog.Find` |
| Skill vendor stock | `RuntimeContentDatabase.SkillIdsForVendor` | `SkillShopRuntimeService.SkillMerchantStock` | `SkillCatalog.All` stock generation |
| Equipment vendor stock | `RuntimeContentDatabase.EquipmentIdsForVendor` | Blacksmith/shop runtime paths | Catalog stock only when no DB is active |
| Vendor service cost/rotation | `RuntimeContentDatabase.FindVendor`, `SelectVendorRotation` | `TownServiceRuntimeService.CostFor`, shop stock refresh | Serialized fallback service cost |
| NPC lookup | `RuntimeContentDatabase.FindNpc` | Runtime exposure and validation for service/vendor links | Scene interactables still hardcode service components |
| Compiled map | `CompiledMapDungeonRuntimeService.SetCompiledMapAssets`, `BuildQuestCompiledMap` | Dungeon start/exit/quest target/monster state registration | `MapGenerationService` with `MapGenerationCatalog` profile/chunks |
| Field monster state | `CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster`, `RegisterMonsterPlacements` | `FieldMonsterRuntimeService`, `FieldMonsterContact`, combat handoff | Legacy visible monster contact marker |

## Remaining C# Catalog Fallback Paths

- `RuntimeContentDatabase` falls back to `MonsterCatalog`, `EncounterCatalog`,
  `QuestCatalog`, `EquipmentCatalog`, `ConsumableCatalog`, and `SkillCatalog`
  when an active database or definition is missing.
- `QuestRuntimeService.AcceptDefaultQuest` still accepts
  `QuestCatalog.TestHuntId`.
- `GameSessionState.StartNewGame` seeds starter equipment and skills from
  `EquipmentCatalog.RustySwordId` and `SkillCatalog.SlashId`.
- `PlayerEquipmentState` defaults to `EquipmentCatalog.Find` when no runtime DB
  resolver is installed.
- `SkillInventoryState` now resolves equipped skill power through a DB-installed
  skill resolver when `RuntimeContentDatabase` is active, with `SkillCatalog.Find`
  retained only as the no-DB emergency resolver.
- `SkillShopRuntimeService` uses DB vendor stock when a database is active.
  `SkillCatalog.All` stock generation is now limited to the no-DB emergency
  fallback path.
- Blacksmith shop UI uses DB vendor stock when a database is active.
  `EquipmentCatalog.All` stock display is now limited to the no-DB emergency
  fallback path.
- Consumable UX text now resolves consumables through
  `RuntimeContentDatabase.FindConsumable`, so DB-authored consumables display
  before catalog fallback.
- `TownServiceInteractable` Apothecary prompt/action now uses DB-first
  consumable vendor stock through `RuntimeContentDatabase.ConsumableIdsForVendor`,
  with `ConsumableCatalog.MinorPotionId` retained only as the no-stock fallback.
- `TownServiceRuntimeService.ScholarHint` now uses
  `RuntimeContentDatabase.BoardQuestAt`, so DB-authored board offers are shown
  before catalog fallback.
- `CombatRuntimeService` keeps `SkillCatalog.FocusStrikeId` for the current
  special-case Bleed effect and uses `MonsterCatalog.TestGuardId` when neither
  handoff nor quest target exists.
- `CompiledMapDungeonRuntimeService` uses `EncounterCatalog.TestGuardId`,
  `MonsterCatalog.TestGuardId`, and `MapGenerationCatalog` as generator fallback.
- Chapter validation tests intentionally exercise catalog fallbacks to preserve
  the existing Chapter 1 loop while DB paths are expanded.

## Fallback Reduction Classification

Fallbacks must be reduced one verified path at a time. The current classification
is intentionally conservative: production runtime should prefer database,
authoring-baked database, saved compiled maps, or runtime generation bundles, but
debug and validation fallback paths remain until equivalent authored content is
present and covered by batch validation.

| Fallback path | Classification | Removal condition |
| --- | --- | --- |
| `RuntimeContentDatabase` catalog lookup for missing active DB | Required emergency fallback | Keep until every player entry path binds a validated DB or runtime bundle in build and Play Mode. |
| `RuntimeContentDatabase` monster/encounter/quest/equipment/consumable/skill catalog lookup after DB miss | Removable per content category | Remove category-by-category only after test content is authored, baked, and validators prove missing IDs fail loudly where appropriate. |
| `generated_single_primary_{monsterId}` encounter synthesis | Required compatibility fallback | Keep through spawn table rollout because direct monster spawn entries intentionally depend on it. |
| `QuestRuntimeService.AcceptDefaultQuest` using `QuestCatalog.TestHuntId` | Debug-only | Replace after new-game/continue flows select DB board offers in Play Mode without needing a hardcoded quest. |
| Starter equipment/skill constants in `GameSessionState` and equipment/skill state | Required bootstrap fallback | Replace after starter loadout is authored in DB and save/load validators cover missing starter data. |
| `PlayerEquipmentState.EquipmentResolver = EquipmentCatalog.Find` default | Required emergency fallback | Keep until all runtime bootstrap paths install a DB resolver before equipment display/combat calculations. |
| Blacksmith UI stock fallback from `EquipmentCatalog.All` | Restricted to no-DB emergency fallback | Runtime UI and debug overlay now use `EquipmentShopRuntimeService.BlacksmithStockItemIds`; active DB with empty blacksmith stock stays empty instead of showing catalog stock. |
| Consumable UX text direct `ConsumableCatalog.Find` | Replaced with DB-first lookup | `ChapterOneUxText.ConsumableStatus` uses `RuntimeContentDatabase.FindConsumable`; batch validation proves a DB-only consumable displays. |
| `SkillInventoryState` equipped power lookup through `SkillCatalog.Find` | Replaced for DB-active runtime | `RuntimeContentDatabase.SetActive` installs `FindSkill`; batch validation proves DB-only equipped skills contribute power. No-DB emergency resolver remains. |
| `SkillShopRuntimeService` fallback stock from `SkillCatalog.All` | Restricted to no-DB emergency fallback | Active DB with empty skill merchant stock now stays empty instead of falling back to catalog stock; batch validation covers DB stock and empty-stock behavior. |
| `TownServiceInteractable` apothecary fixed `ConsumableCatalog.MinorPotionId` | Replaced with DB-first consumable vendor stock lookup | `TownServiceRuntimeService.FirstConsumableStockIdFor` reads DB vendor stock first; batch validation proves a DB-only apothecary consumable can be purchased. Fixed potion remains as no-stock fallback. |
| `TownServiceRuntimeService.ScholarHint` using `QuestCatalog.BoardOffer` | Replaced with DB-first board offer lookup | `RuntimeContentDatabase.BoardQuestAt` is used; batch validation proves a DB-only quest appears in scholar hint text. Catalog fallback remains inside `BoardQuestAt` for no-DB/debug cases. |
| `CombatRuntimeService` `FocusStrikeId` special-case and `TestGuardId` fallback | Required until combat content pass | Remove only after combat effects and no-handoff encounter handling are data-driven and validators cover both. |
| `CompiledMapDungeonRuntimeService` `MapGenerationCatalog` fallback generation | Debug-only | Keep for batch/debug repro until runtime bundle assets are bound by scene bootstrap and Play Mode validates bundle generation. |
| Legacy visible monster contact marker | Debug-only | Remove after field monster actors are spawned from compiled/runtime generated placements and Play Mode confirms contact handoff. |
| `P0SceneBuilder` hardcoded scene/NPC/service object generation | Required build bootstrap | Replace in slices as scene/prefab authoring and NPC/service asset wiring become validated. |

## Hardcoded Scene Generation Paths

`P0SceneBuilder` currently generates all P0 scenes in code:

- Scene list and build settings: `Title`, `Town`, `Dungeon`, `Combat`, `Ending`.
- Shared scene bootstrap object, runtime canvas roots, light, player, camera,
  ground primitives.
- Bootstrap asset references:
  - `ContentDatabase.asset` at `LegacyContentJsonImporter.DefaultDatabaseAssetPath`
  - Chapter 2 compiled map asset at `ChapterTwoBuildValidator.DefaultCompiledMapAssetPath`
- Town interactable primitives:
  - `Quest Board` with `QuestBoardInteractable`
  - `Dungeon Gate` with `GateInteractable`
  - `Blacksmith` with `BlacksmithInteractable`
  - `Skill Merchant` with `SkillMerchantInteractable`
  - `Inn`, `Trainer`, `Apothecary`, `Scholar` with `TownServiceInteractable`
- Town service positions, names, service kinds, and fallback costs are hardcoded.
- Dungeon still creates a legacy `Visible Monster Contact` capsule with
  `FieldMonsterContact` even though compiled map placements register runtime
  field monster state.
- If the compiled map asset is missing, the builder generates one from
  `MapGenerationCatalog.ChapterTwoFirstSliceProfile` and fixed seed `2001`.

## Hardcoded NPC and Service Runtime Paths

- Scene objects determine NPC/service availability. NPC definitions are
  validated and exposed, but town service GameObjects are not yet spawned from
  `ContentNpcDefinition`.
- `TownServiceKind` maps service kinds to fixed vendor IDs:
  `vendor_inn`, `vendor_trainer`, `vendor_apothecary`.
- Inn, trainer, apothecary, and scholar interaction behavior is branch-based in
  `TownServiceInteractable`.
- Quest board, gate, blacksmith, and skill merchant are separate hardcoded
  interactable component types.
- Apothecary purchase is fixed to `minor_potion`.
- Scholar hint uses the current board offer rather than NPC-authored dialogue or
  service content.

## Validation Entry Points

- `Conn > Build & Validate Chapter 1`
  - `ChapterOneBuildValidator.BuildAndValidateChapterOne`
  - Verifies content DB, generated P0 scenes, Runtime core rules, build settings,
    and runtime canvas scene contracts.
- `Conn > Build & Validate Chapter 2`
  - `ChapterTwoBuildValidator.BuildAndValidateChapterTwo`
  - Imports legacy JSON, validates the content slice, validates/generated/saves
    the compiled map slice, and verifies Chapter 2 runtime data consumption.
- Forbidden Editor reference scan:
  - `rg -n "UnityEditor|EditorWindow|AssetDatabase|MenuItem|Conn\\.Editor" Assets/Conn/Core Assets/Conn/Runtime Assets/Conn/UI/Runtime`
  - Current audit result: no matches.

## Baseline Result

Runtime content lookup is DB-first for monsters, encounters, quests, equipment,
consumables, skills, vendors, and compiled maps. The largest remaining hardcoded
areas are scene object creation, NPC/service object wiring, starter-state seeds,
shop fallback stock, and a small number of combat/catalog constants retained to
protect the existing Chapter 1 loop.

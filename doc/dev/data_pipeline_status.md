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
| Equipment vendor stock | `RuntimeContentDatabase.EquipmentIdsForVendor` | Blacksmith/shop runtime paths | Empty DB stock with catalog starter-item exclusions |
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
- `SkillInventoryState` still resolves equipped skill display/rules through
  `SkillCatalog.Find`.
- `SkillShopRuntimeService` uses `SkillCatalog.All` to generate fallback stock
  when DB vendor stock is unavailable.
- `TownServiceInteractable` still uses `ConsumableCatalog.MinorPotionId` for
  the apothecary action and prompt.
- `TownServiceRuntimeService.ScholarHint` uses `QuestCatalog.BoardOffer`.
- `CombatRuntimeService` keeps `SkillCatalog.FocusStrikeId` for the current
  special-case Bleed effect and uses `MonsterCatalog.TestGuardId` when neither
  handoff nor quest target exists.
- `CompiledMapDungeonRuntimeService` uses `EncounterCatalog.TestGuardId`,
  `MonsterCatalog.TestGuardId`, and `MapGenerationCatalog` as generator fallback.
- Chapter validation tests intentionally exercise catalog fallbacks to preserve
  the existing Chapter 1 loop while DB paths are expanded.

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

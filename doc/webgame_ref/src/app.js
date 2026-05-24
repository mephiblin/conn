import {
  buildPresetCatalog,
  createDraftPreset,
  createPreviewGrid,
  draftFromGrid,
  getPresetById,
  instantiatePreset,
  loadCustomPresets,
  saveCustomPresets,
  upsertCustomPreset,
  deleteCustomPreset,
} from "./presets.js";
import {
  blocksMovement,
  collectPlacementsAt,
  getCell,
  logicalCellKey,
  logicalPlayerCell,
  resolveActionDirection,
  resolveInteractionCandidate,
  resolveLookDirection,
  resolveDoorAtFront,
  resolveStairsOutcome,
} from "./runtimeCore.js";
import {
  applyPartyDefeatEndingState,
  applyFinalEndingState,
  buildPartyDefeatEndingState,
  buildFinalEndingState,
  createInitialQuestState,
  normalizeQuestState,
  questEndingComplete,
  availableQuestDefinitions,
  activateBoardQuest,
  boardQuestAllowsDungeonEntry,
  boardQuestEntryTarget,
  boardQuestCanReturn,
  grantBoardQuestReturnRewards,
  updateBoardQuestCompletion,
} from "./runtimeQuest.js";
import { createInitialRuntimeFloorBundle } from "./runtimeFloorBundle.js";
import { createViewController } from "./viewController.js";
import { createPlayerController } from "./playerController.js";
import { createPlayerActionRunner } from "./playerActions.js";
import {
  attachEditorStateAccessors,
  createEditorState,
  defaultEventSelection,
} from "./editorState.js";
import {
  EDITOR_PROJECT_STORAGE_KEY,
  applyEditorProject as applyEditorProjectModule,
  buildEditorProject as buildEditorProjectModule,
  loadEditorProject as loadEditorProjectModule,
  normalizeMapLight as normalizeMapLightModule,
  normalizeMapMetadata as normalizeMapMetadataModule,
  saveEditorProject as saveEditorProjectModule,
} from "./editorProject.js";
import {
  placementKindForTool as placementKindForToolModule,
  removeCellFromRooms as removeCellFromRoomsModule,
  replaceRoomBounds as replaceRoomBoundsModule,
  selectionBoundsFromPoints as selectionBoundsFromPointsModule,
  syncRoomRegistryFromCells as syncRoomRegistryFromCellsModule,
} from "./editorMapEditing.js";
import { handleEditorCellInteraction } from "./editorCellInteraction.js";
import { bindEditorWorkspaceInteractions } from "./editorBindings.js";
import { createEditorArchiveBridge } from "./editorArchiveBridge.js";
import { createEditorContentPanelBridge } from "./editorContentPanelBridge.js";
import { createEditorEventAuthoringBridge } from "./editorEventAuthoringBridge.js";
import { createEditorEventCompareBridge } from "./editorEventCompareBridge.js";
import { createEditorEventReviewBridge } from "./editorEventReviewBridge.js";
import { createEditorEventPanelBridge } from "./editorEventPanelBridge.js";
import { createEditorEventSnapshotBridge } from "./editorEventSnapshotBridge.js";
import { createEditorNpcSnapshotBridge } from "./editorNpcSnapshotBridge.js";
import { createEditorNpcSupportPanelBridge } from "./editorNpcSupportPanelBridge.js";
import { createEditorPlacementClassPanelBridge } from "./editorPlacementClassPanelBridge.js";
import {
  buildEditorEventPanelBodyDeps,
  buildEditorMainWorkspaceArgs,
  buildEditorWorkspaceBindingArgs,
  buildEditorWorkspacePanelArgs,
} from "./editorRenderAssemblyBridge.js";
import { createEditorRenderSnapshotBridge } from "./editorRenderSnapshotBridge.js";
import { createEditorSupportBridge } from "./editorSupportBridge.js";
import { createEditorWorkspacePanelBridge } from "./editorWorkspacePanelBridge.js";
import { createInventoryOverlayBridge } from "./inventoryOverlayBridge.js";
import { createInventoryRuntimeBridge } from "./inventoryRuntimeBridge.js";
import { createEventRuntimeBridge } from "./eventRuntimeBridge.js";
import { createNpcRuntimeBridge } from "./npcRuntimeBridge.js";
import { createCombatRuntimeBridge } from "./combatRuntimeBridge.js";
import { createDungeonRuntimeBridge } from "./dungeonRuntimeBridge.js";
import { createEditorNpcPresetBridge } from "./editorNpcPresetBridge.js";
import { createEditorNpcEventStateBridge } from "./editorNpcEventStateBridge.js";
import { createEditorValidationBridge } from "./editorValidationBridge.js";
import { createClassProgressionBridge } from "./classProgressionBridge.js";
import { createRuntimePartyBridge } from "./runtimePartyBridge.js";
import { bootstrapAppRuntime } from "./appBootstrapBridge.js";
import { createEditorContentStateBridge } from "./editorContentStateBridge.js";
import { createEditorDependencyBridge } from "./editorDependencyBridge.js";
import { createEditorMapEditingBridge } from "./editorMapEditingBridge.js";
import { createEditorProjectBridge } from "./editorProjectBridge.js";
import { renderAppFrame } from "./gameRenderBridge.js";
import { createDebugHarness, registerDebugHarness } from "./debugHarness.js";
import {
  CONTENT_BUILD_DATA_FILES,
  DEFAULT_CEILING_TEXTURE_ID,
  DEFAULT_FLOOR_TEXTURE_ID,
  DEFAULT_WALL_TEXTURE_ID,
  FLOOR_TEXTURE_IDS,
  CEILING_TEXTURE_IDS,
  GENERATED_NORMAL_MAP_KEYS,
  loadContentData,
  resolveRendererLodProfile,
  THEME_BATTLE_BACKGROUNDS,
  VALID_MATERIAL_LIGHTING_HINTS,
  VALID_MATERIAL_LODS,
  WALL_TEXTURE_IDS,
} from "./contentRegistry.js";
import {
  computeWalls as computeWallsModule,
  createRuntimeFloorMaps as createRuntimeFloorMapsModule,
  createValidatedRuntimeFloorMaps as createValidatedRuntimeFloorMapsModule,
  makeMap as makeMapModule,
  randomMapSeed as randomMapSeedModule,
} from "./mapGeneration.js";
import { createMapCompiler } from "./mapCompiler.js";
import {
  consumeSkillCard,
  grantSkillCard,
  skillInventoryCount,
} from "./diceSkillLoadout.js";
import {
  activateTownState,
  ensureTownFloorMaps,
  ensureTownNpcServices,
} from "./townRuntime.js";
import { createEventRuntime } from "./eventRuntime.js";
import { createNpcRuntime } from "./npcRuntime.js";
import {
  renderEditorClassProgressionPanel,
  renderEditorContentBuildDashboardPanel,
  renderEditorDensityHistogramPanel,
  renderEditorAffixRarityPanel,
  renderEditorEventGraphBody,
  renderEditorEventExportArchiveBody,
  bindEditorFrame,
  renderEditorFrame,
  renderEditorMainWorkspace,
  renderEditorItemBasePanel,
  renderEditorQuestDefinitionPanel,
  renderEditorMonsterDefinitionPanel,
  renderEditorSkillDefinitionPanel,
  renderEditorLootTablePanel,
  renderEditorNpcPlacementPanel,
  renderEditorNpcCustomPresetSection,
  renderEditorNpcProgressionHooksPanel,
  renderEditorNpcServiceEditorSection,
  renderEditorNpcServicePanel,
  renderEditorQuestSeedBody,
  renderEditorPlacementOverridePanel,
  renderEditorSelectedBlockPanel,
  renderEditorPresetStudioPanel,
  renderEditorQuestSeedPanel,
  renderEditorSampleItemPanel,
  renderEditorSurfaceBrushPanel,
  renderEditorPresetLibraryPanel,
  renderEditorPlacementRecommendationPanel,
  renderEditorRangeBrushPanel,
  renderEditorValidationPanel,
  renderEditorVendorInventoryPanel,
} from "./renderEditor.js";
import {
  bindCombatFrame,
  bindTownFrame,
  inventoryEntryOverlayMarkup as inventoryEntryOverlayMarkupFrame,
  inventoryOverlayMarkup as inventoryOverlayMarkupFrame,
  renderLogFrame,
  renderMiniMapFrame,
  renderPartyFrame,
  renderQuestFrame,
  renderResourcesFrame,
} from "./renderGame.js";
import { renderTitleShell } from "./renderTitle.js";
import { renderTownFrame } from "./renderTown.js";
import { renderCombatFrame } from "./renderCombat.js";
import { createRuntimeSessionManager } from "./runtimeSession.js";
import { createCombatRuntime } from "./combatRuntime.js";
import { createFieldMonsterRuntime } from "./fieldMonsterRuntime.js";
import { renderDungeonViewBridge } from "./dungeonViewBridge.js";
import {
  buildRecentStatusLabel as buildRecentStatusLabelModule,
  createSaveSlotManager,
  formatPlaytimeLabel as formatPlaytimeLabelModule,
  parseSaveSlotPayload as parseSaveSlotPayloadModule,
  saveContentVersionMatchesCurrent as saveContentVersionMatchesCurrentModule,
  saveSlotLabel as saveSlotLabelModule,
  saveSlotStorageKey as saveSlotStorageKeyModule,
  saveUsesEmbeddedContentDefinitions as saveUsesEmbeddedContentDefinitionsModule,
  summarizeSaveData as summarizeSaveDataModule,
} from "./saveSlots.js";
import { createProductShell } from "./productShell.js";

const DIRS = ["north", "east", "south", "west"];
const VEC = {
  north: { x: 0, y: -1 },
  east: { x: 1, y: 0 },
  south: { x: 0, y: 1 },
  west: { x: -1, y: 0 },
};
const RENDERER_LOD_PROFILE = resolveRendererLodProfile();

const {
  classes,
  monsters,
  items,
  encounters,
  eventDefinitions,
  skills,
  questDefinitions,
  npcs,
  vendors,
  lootTables,
  materialManifest,
  mapProfiles,
} = await loadContentData();

ensureTownNpcServices(npcs);


const LEGACY_MONSTER_TO_ENCOUNTER = {
  desert_rat: "encounter_desert_rat",
  grave_robber: "encounter_grave_robber",
  serpent_guard: "encounter_serpent_guard",
  poisoned_raider: "encounter_poisoned_raider",
  cursed_gladiator: "encounter_cursed_gladiator",
  black_water_beast: "encounter_black_water_beast",
  serpent_priest: "encounter_serpent_priest",
  blind_priest: "encounter_blind_priest",
};
const LEGACY_EVENT_TO_TRIGGER = {
  altar_01: "event_blood_altar_unlock",
};
const DEFAULT_EDITOR_ENCOUNTER_ID = "encounter_grave_robber";
const DEFAULT_EDITOR_EVENT_ID = "event_blood_altar_unlock";
const DEFAULT_EDITOR_TRAP_EVENT_ID = "event_trap_poison_dart";
const DEFAULT_EDITOR_SHRINE_EVENT_ID = "event_shrine_healing_spring";
const DEFAULT_EDITOR_REST_EVENT_ID = "event_rest_guard_post";
const DEFAULT_EDITOR_CAMP_EVENT_ID = "event_camp_guard_post";
const SAVE_SLOT_SCHEMA_VERSION = 1;
const CURRENT_CONTENT_VERSION = "serpent_temple_mvp_3_floor";
const INTERACTIVE_EVENT_PLACEMENT_KINDS = new Set(["event_trigger", "shrine", "rest_site", "camp"]);
const INTERACTIVE_PLACEMENT_KINDS = new Set([...INTERACTIVE_EVENT_PLACEMENT_KINDS, "npc", "encounter", "monster"]);
const EVENT_OBJECT_PLACEMENT_KINDS = new Set(["trap", "event_trigger", "rest_site", "shrine", "camp"]);
const MOVEMENT_BLOCKING_PLACEMENT_KINDS = new Set(["encounter", "monster", "npc"]);
const EVENT_EFFECT_TYPES = new Set([
  "log",
  "set_flag",
  "set_quest_seed_state",
  "open_npc_service",
  "damage_front",
  "heal_party",
  "cure_status_party",
  "consume_resource",
  "add_status_front",
  "mark_done",
  "grant_xp_party",
  "grant_item",
  "restore_resource",
]);
const EVENT_TRIGGER_TYPES = new Set(["interact", "onEnter", "onExit", "onRest", "onCamp"]);
const RESOURCE_KEYS = new Set(["torch", "food", "water", "gold"]);
const COMPANION_STATE_KEYS = new Set(["absent", "recruited", "joined_party", "dismissed"]);
const PARTY_STAT_KEYS = new Set(["hp", "maxHp", "atk", "def", "xp", "trainingLevel"]);
const PARTY_MODEL_LIMITS = {
  protagonist: 1,
  companion: 1,
  maxMembers: 2,
};
const PROTAGONIST_BACKGROUNDS = [
  {
    id: "arena_escapee",
    label: "투기장 탈주자",
    summary: "피 냄새와 격전의 호흡에 익숙하다.",
    questNote: "핏빛 모래 위에서 살아남은 기억이 아직 손끝에 남아 있다.",
    log: "투기장의 쇠사슬을 끊고 나온 기억이 세트의 사원 앞에서 다시 끓어오른다.",
  },
  {
    id: "temple_scout",
    label: "변방 정찰병",
    summary: "사막 길과 폐사원의 흔적을 읽는 데 능하다.",
    questNote: "변방 정찰 중 사라진 선행 탐사대를 추적한다.",
    log: "변방 초소에서 잃어버린 정찰대의 흔적이 이 미궁 안으로 이어져 있다.",
  },
  {
    id: "black_library_exile",
    label: "추방된 필경사",
    summary: "금서와 봉인, 신전의 기호에 집착한다.",
    questNote: "검은 도서관에서 금지된 사본 한 장을 되찾으려 한다.",
    log: "추방 직전 훔쳐 본 금서의 단서가 세트의 침묵 사원 깊은 곳을 가리킨다.",
  },
];
const STARTER_LOADOUTS = [
  {
    id: "balanced",
    label: "균형 보급",
    summary: "기본 보급을 유지한다.",
    resources: { torch: 60, food: 8, water: 8, gold: 45 },
    inventory: ["bandage", "antivenom", "throwing_knife", "firebomb"],
  },
  {
    id: "expedition",
    label: "탐사 보급",
    summary: "긴 탐사를 위해 횃불과 식수를 더 챙긴다.",
    resources: { torch: 84, food: 9, water: 11, gold: 30 },
    inventory: ["bandage", "throwing_knife"],
  },
  {
    id: "scavenger",
    label: "약탈 보급",
    summary: "현금과 치료제를 더 챙기지만 식량은 빈약하다.",
    resources: { torch: 54, food: 6, water: 7, gold: 70 },
    inventory: ["bandage", "bandage", "antivenom", "firebomb"],
  },
];
const DEFAULT_RARITY_DEFINITIONS = {
  common: { label: "Common", weight: 8, valueMultiplier: 1, affixCount: 0 },
  rare: { label: "Rare", weight: 3, valueMultiplier: 1.5, affixCount: 1 },
  relic: { label: "Relic", weight: 1, valueMultiplier: 2.5, affixCount: 2 },
};
const DEFAULT_AFFIX_DEFINITIONS = {
  prefix_sharp: { label: "날카로운", slot: "prefix", stat: "attack", amount: 1, rarity: "rare" },
  prefix_guarded: { label: "굳건한", slot: "prefix", stat: "defense", amount: 1, rarity: "rare" },
  suffix_venomward: { label: "해독의", slot: "suffix", stat: "cure", amount: 1, value: "독", rarity: "rare" },
};
const DEFAULT_AFFIX_POOL_DEFINITIONS = {
  pool_weapon_prefix: { label: "무기 prefix", itemKinds: ["artifact", "equipment"], affixIds: ["prefix_sharp"] },
  pool_armor_prefix: { label: "방어구 prefix", itemKinds: ["equipment"], affixIds: ["prefix_guarded"] },
  pool_curative_suffix: { label: "치유 suffix", itemKinds: ["consumable"], affixIds: ["suffix_venomward"] },
};
const rarityDefinitions = JSON.parse(JSON.stringify(DEFAULT_RARITY_DEFINITIONS));
const affixDefinitions = JSON.parse(JSON.stringify(DEFAULT_AFFIX_DEFINITIONS));
const affixPoolDefinitions = JSON.parse(JSON.stringify(DEFAULT_AFFIX_POOL_DEFINITIONS));
const {
  createEventBranchTemplate,
  createEventEffectTemplate,
  matchesRequiredCompanionState,
  eventEffectFieldMeta,
  updatePlacementOverrides,
  parseJsonField,
  createEventValidationEntry,
  eventEffectValidationEntries,
  eventEffectValidationIssues,
  eventStepValidationEntries,
  eventStepValidationIssues,
  buildEventValidationSnapshot,
  eventDefinitionValidationIssues,
  eventDefinitionValidationEntries,
  validateEventDefinitionsTable,
  effectJson,
  eventStepsJson,
  collectEventEffects,
} = createEditorEventAuthoringBridge({
  getState: () => state,
  items,
  eventEffectTypes: EVENT_EFFECT_TYPES,
  eventTriggerTypes: EVENT_TRIGGER_TYPES,
  resourceKeys: RESOURCE_KEYS,
  companionStateKeys: COMPANION_STATE_KEYS,
  partyStatKeys: PARTY_STAT_KEYS,
  classes,
});
const {
  buildEventGraphPreview,
  buildEventGraphExportSummary,
  buildEventGraphCompactExport,
  buildEventGraphSummaryDiff,
  buildProjectEventGraphReviewBundle,
  buildEventExportArchiveBatchCompare,
  buildEventExportArchiveBatchCompareExport,
  buildEventExportArchiveBatchShareExport,
  buildEventExportArchiveBatchShareLink,
  parseEventExportArchiveBatchShareLink,
  importEventExportArchiveBatchShareLink,
  autoImportEventExportArchiveBatchShareLinkFromLocation,
  buildFullCompactEventRestoreOptions,
  hasRestorableCompactEventPayload,
  applyEventExportArchiveEntryPayload,
  isRestorableEventExportEntry,
  applyEventExportArchiveBatchCompareTargets,
} = createEditorEventReviewBridge({
  getState: () => state,
  eventDefinitions,
  npcs,
  buildEventValidationSnapshot,
  buildEventExportSummaryDiff: (...args) => buildEventExportSummaryDiff(...args),
  recordEventExportHistory: (...args) => recordEventExportHistory(...args),
  recordEventExportArchive: (...args) => recordEventExportArchive(...args),
  applyPartialCompactEventRowToDefinition: (...args) => applyPartialCompactEventRowToDefinition(...args),
  activeEventEditorTool: (...args) => activeEventEditorTool(...args),
  addLog: (...args) => addLog(...args),
});
validateEventDefinitionsTable(eventDefinitions);
const EVENT_EDITOR_TO_PLACEMENT_KIND = {
  eventTrigger: "event_trigger",
  trap: "trap",
  shrine: "shrine",
  restSite: "rest_site",
  camp: "camp",
};
const PLACEMENT_TOOL_BUTTONS = new Set(["stairs", "encounter", "npc", "eventTrigger", "trap", "shrine", "restSite", "camp"]);

const PLACEMENT_KINDS = new Set([
  "stairs", "entry_marker", "transition", "encounter", "npc", "item", "trap", "event_trigger",
  "rest_site", "shrine", "camp", "container", "device", "hazard", "lore",
  "environment", "monster", "event",
]);
const LEGACY_PLACEMENT_KINDS = new Set(["monster", "event"]);
const ROOM_TYPES = [
  "entrance_room", "combat_room", "ambush_room", "trap_room", "trap_corridor",
  "treasure_room", "shrine_room", "camp_room", "safe_room", "npc_room",
  "puzzle_room", "boss_room", "transition_room", "lore_room",
];
const CELL_TAGS = [
  "safe", "camp_allowed", "save_allowed", "no_random_encounter", "trap_zone",
  "hazard_zone", "npc_anchor", "loot_anchor", "boss_anchor",
  "secret_candidate", "rest_ambush_allowed",
];
const BATTLE_BACKGROUNDS = [
  "",
  "battle_bg_buried_temple_corridor",
  "battle_bg_buried_temple_shrine",
  "battle_bg_black_water_pool",
  "battle_bg_serpent_altar",
];
const DENSITY_OVERLAY_MODES = new Set(["none", "encounter", "trap", "reward", "recovery", "camp", "npc", "event"]);
const EVENT_EXPORT_ARCHIVE_STORAGE_KEY = "serpent_event_export_archive_v1";
const EVENT_BUNDLE_PATCH_ARCHIVE_STORAGE_KEY = "serpent_event_bundle_patch_archive_v1";
const NPC_PRESET_PATCH_ARCHIVE_STORAGE_KEY = "serpent_npc_preset_patch_archive_v1";
const NPC_PRESET_REDO_ARCHIVE_STORAGE_KEY = "serpent_npc_preset_redo_archive_v1";
const NPC_CUSTOM_PRESET_STORAGE_KEY = "serpent_npc_custom_presets_v1";
const SAVE_SLOT_IDS = ["slot_1", "slot_2", "slot_3"];
const SAVE_SLOT_STORAGE_PREFIX = "serpent_save_";
const PRESET_DRAFT_SIZE = 7;
let viewController = null;
let playerController = null;
let dragLookEnabled = true;
const EDITOR_ONLY_BOUNDARY_KEYS = new Set([
  "editorProject",
  "editorSessionState",
  "brushPresets",
  "selectedPresetId",
  "presetRotation",
  "generationPresetIds",
  "presetDraft",
  "presetDraftGrid",
  "presetDraftSelectedId",
  "editorTool",
  "editorCursor",
  "selectedCellTag",
  "selectedBattleBackgroundId",
  "selectedRoomType",
  "selectedFloorTextureId",
  "selectedCeilingTextureId",
  "selectedWallTextureId",
  "activeRoomId",
  "roomRangeStart",
  "metadataRangeStart",
  "metadataSelectionMode",
  "lassoSelectionAction",
  "editorBrushDrag",
  "editorLassoSelectionDrag",
  "suppressRangeClick",
  "lastBrushSelection",
  "eventInspectorTool",
  "selectedPlacementOverrideId",
  "selectedNpcPlacementId",
  "selectedEventDefinitionIds",
  "selectedEventStepIndex",
  "selectedClassDefinitionIndex",
  "selectedSkillDefinitionId",
  "selectedItemDefinitionId",
  "selectedVendorDefinitionId",
  "selectedVendorRotationIndex",
  "selectedLootTableId",
  "selectedLootTierIndex",
  "selectedLootBonusIndex",
  "selectedCombatRewardProfileIndex",
  "selectedRarityDefinitionId",
  "selectedAffixDefinitionId",
  "selectedAffixPoolId",
  "sampleItemPreview",
  "selectedNpcDefinitionId",
  "selectedNpcQuestSeedIndex",
  "selectedNpcServiceIndex",
  "selectedNpcDialogueStepIndex",
  "selectedNpcCustomPresetId",
  "selectedNpcCustomPresetApplyMode",
  "selectedNpcCustomPresetConflictMode",
  "selectedNpcCustomPresetServiceIndexes",
  "selectedNpcCustomPresetSeedIndexes",
  "selectedNpcCustomPresetDialogueStepSelections",
  "selectedNpcCustomPresetDialogueChoiceSelections",
  "selectedNpcCustomPresetServiceFieldSelections",
  "selectedNpcCustomPresetSeedFieldSelections",
  "selectedNpcCustomPresetMergePatchDraft",
  "selectedNpcCustomPresetPatchHistory",
  "densityOverlayMode",
  "eventTestSession",
  "eventExportHistory",
  "contentDefinitions",
  "authoredMaps",
  "customPresets",
  "npcCustomPresets",
]);

const EDITOR_TEXTURE_SWATCH = {
  floor_sandstone_01: "#7d694e",
  floor_obsidian_01: "#45414a",
  floor_moss_01: "#58684a",
  floor_bloodstone_01: "#7e4d43",
  ceiling_stone_01: "#4e4438",
  ceiling_vault_01: "#61544a",
  ceiling_soot_01: "#3a352f",
  ceiling_gold_01: "#76613d",
  wall_buried_temple_01: "#5c4937",
  wall_black_brick_01: "#3c383d",
  wall_mossy_01: "#526448",
  wall_sacred_relief_01: "#756047",
  door_bronze_01: "#ab8043",
};

const ROOM_RECOMMENDATION_RULES = {
  entrance_room: [
    { tool: "eventTrigger", reason: "진입 연출과 설명 이벤트를 두기 쉽다." },
    { tool: "npc", reason: "안내 NPC나 시작 안내 지점을 두기 쉽다." },
  ],
  combat_room: [
    { tool: "encounter", reason: "combat_room 기본 활동은 조우다." },
    { tool: "eventTrigger", reason: "전투 보상/장치 이벤트를 같이 둘 수 있다." },
  ],
  ambush_room: [
    { tool: "encounter", reason: "기습 조우 배치와 잘 맞는다." },
    { tool: "trap", reason: "기습 구역은 함정과 함께 설계하기 쉽다." },
  ],
  trap_room: [
    { tool: "trap", reason: "trap_room 핵심 규칙이다." },
    { tool: "eventTrigger", reason: "함정 해제/보상 이벤트를 곁들일 수 있다." },
    { tool: "encounter", reason: "경비 조우를 추가하면 긴장감을 올릴 수 있다." },
  ],
  trap_corridor: [
    { tool: "trap", reason: "복도형 함정 구간에 맞다." },
    { tool: "eventTrigger", reason: "경고/해제 장치를 배치하기 쉽다." },
  ],
  treasure_room: [
    { tool: "eventTrigger", reason: "보물 상호작용 이벤트에 맞다." },
    { tool: "encounter", reason: "보상 방 수호 조우를 둘 수 있다." },
  ],
  shrine_room: [
    { tool: "shrine", reason: "shrine_room 핵심 규칙이다." },
    { tool: "eventTrigger", reason: "제단 장치나 의식 이벤트를 추가할 수 있다." },
  ],
  camp_room: [
    { tool: "camp", reason: "camp_room의 핵심 휴식 지점이다." },
    { tool: "restSite", reason: "짧은 회복 anchor와 잘 맞는다." },
    { tool: "npc", reason: "안전 구역 NPC를 두기 쉽다." },
  ],
  safe_room: [
    { tool: "restSite", reason: "안전 구역 회복 지점과 잘 맞는다." },
    { tool: "camp", reason: "캠프/세이브 anchor로 쓰기 쉽다." },
    { tool: "npc", reason: "전투 없는 NPC 방으로 설계하기 쉽다." },
  ],
  npc_room: [
    { tool: "npc", reason: "npc_room 핵심 규칙이다." },
    { tool: "eventTrigger", reason: "대화 전후 이벤트를 붙이기 쉽다." },
  ],
  puzzle_room: [
    { tool: "eventTrigger", reason: "장치/퍼즐 이벤트 배치와 잘 맞는다." },
    { tool: "trap", reason: "실패 페널티 함정을 붙일 수 있다." },
  ],
  boss_room: [
    { tool: "encounter", reason: "boss_room 핵심 규칙이다." },
    { tool: "stairs", reason: "보스 이후 다음 층/종료 계단을 두기 쉽다." },
    { tool: "shrine", reason: "의식/제단 연출 anchor와 맞는다." },
  ],
  transition_room: [
    { tool: "stairs", reason: "transition_room 핵심 규칙이다." },
    { tool: "eventTrigger", reason: "층 이동 전후 장치 이벤트를 붙일 수 있다." },
  ],
  lore_room: [
    { tool: "eventTrigger", reason: "기록/연출 이벤트와 잘 맞는다." },
    { tool: "npc", reason: "서사 NPC anchor로 쓸 수 있다." },
  ],
};

const CELL_TAG_RECOMMENDATION_RULES = {
  safe: [
    { tool: "restSite", reason: "safe 태그는 회복 anchor와 잘 맞는다." },
    { tool: "npc", reason: "안전 구역 NPC 배치 후보가 된다." },
  ],
  camp_allowed: [
    { tool: "camp", reason: "camp_allowed 태그는 캠프 허용 구역이다." },
    { tool: "restSite", reason: "짧은 휴식 지점도 같이 둘 수 있다." },
  ],
  save_allowed: [
    { tool: "camp", reason: "세이브/휴식 anchor 후보가 된다." },
    { tool: "restSite", reason: "회복 지점과 함께 쓰기 쉽다." },
  ],
  trap_zone: [
    { tool: "trap", reason: "trap_zone 태그와 직접 맞는다." },
    { tool: "eventTrigger", reason: "해제/경고 이벤트를 붙일 수 있다." },
  ],
  hazard_zone: [
    { tool: "trap", reason: "hazard_zone에 위험 장치를 두기 쉽다." },
    { tool: "eventTrigger", reason: "환경 위험 이벤트 anchor가 된다." },
  ],
  npc_anchor: [
    { tool: "npc", reason: "npc_anchor 태그와 직접 맞는다." },
  ],
  loot_anchor: [
    { tool: "eventTrigger", reason: "보상 상호작용 이벤트 anchor가 된다." },
  ],
  boss_anchor: [
    { tool: "encounter", reason: "boss_anchor는 보스/경비 조우와 직접 맞는다." },
    { tool: "stairs", reason: "보스 뒤 계단 anchor 후보가 된다." },
  ],
  rest_ambush_allowed: [
    { tool: "camp", reason: "휴식-기습 연출에 쓸 수 있다." },
    { tool: "restSite", reason: "휴식 지점 anchor가 된다." },
  ],
};

function normalizeTextureId(id, list, fallback) {
  return list.includes(id) ? id : fallback;
}

function editorEventSelectionDefaults() {
  return {
    eventTrigger: DEFAULT_EDITOR_EVENT_ID,
    trap: DEFAULT_EDITOR_TRAP_EVENT_ID,
    shrine: DEFAULT_EDITOR_SHRINE_EVENT_ID,
    restSite: DEFAULT_EDITOR_REST_EVENT_ID,
    camp: DEFAULT_EDITOR_CAMP_EVENT_ID,
  };
}

function buildEditorStateConfig() {
  return {
    presetDraftSize: PRESET_DRAFT_SIZE,
    textureDefaults: {
      floor: DEFAULT_FLOOR_TEXTURE_ID,
      ceiling: DEFAULT_CEILING_TEXTURE_ID,
      wall: DEFAULT_WALL_TEXTURE_ID,
    },
    densityOverlayModes: DENSITY_OVERLAY_MODES,
    eventSelectionDefaults: editorEventSelectionDefaults(),
    definitionIds: {
      questDefinitionIds: Object.keys(questDefinitions),
      monsterIds: Object.keys(monsters),
      itemIds: Object.keys(items),
      vendorIds: Object.keys(vendors),
      lootTableIds: Object.keys(lootTables).filter((id) => id !== "combatRewardProfiles"),
      rarityDefinitionIds: Object.keys(rarityDefinitions),
      affixDefinitionIds: Object.keys(affixDefinitions),
      affixPoolIds: Object.keys(affixPoolDefinitions),
      npcDefinitionIds: Object.keys(npcs),
    },
  };
}

function collectBoundaryIssues(value, context, issues = [], seen = new WeakSet()) {
  if (!value || typeof value !== "object") return issues;
  if (seen.has(value)) return issues;
  seen.add(value);
  for (const [key, nested] of Object.entries(value)) {
    const path = `${context}.${key}`;
    if (EDITOR_ONLY_BOUNDARY_KEYS.has(key)) issues.push({ severity: "error", message: `${path}는 runtime boundary에 들어가면 안 된다.`, code: "runtime_editor_boundary_leak" });
    collectBoundaryIssues(nested, path, issues, seen);
  }
  return issues;
}

function withBoundaryIssues(report, issues) {
  if (!issues.length) return report;
  const mergedIssues = [...(report?.issues || []), ...issues];
  return {
    ...(report || {}),
    summary: {
      error: mergedIssues.filter((issue) => issue.severity === "error").length,
      warning: mergedIssues.filter((issue) => issue.severity === "warning").length,
      info: mergedIssues.filter((issue) => issue.severity === "info").length,
    },
    issues: mergedIssues,
  };
}

function ensureBoundaryClean(value, context, report = null) {
  const issues = collectBoundaryIssues(value, context);
  if (report) return withBoundaryIssues(report, issues);
  if (issues.length) {
    const first = issues[0];
    throw new Error(`${context} 경계 검증 실패: ${first.message}`);
  }
  return report;
}

function textureSwatchColor(textureId, fallback = "#655846") {
  return EDITOR_TEXTURE_SWATCH[textureId] || fallback;
}

function randomMapSeed() {
  return randomMapSeedModule();
}

function createRuntimeFloorMaps(presetPool = buildPresetCatalog(), seedByFloor = {}) {
  return createRuntimeFloorMapsModule(presetPool, seedByFloor, {
    defaultFloorTextureId: DEFAULT_FLOOR_TEXTURE_ID,
    defaultCeilingTextureId: DEFAULT_CEILING_TEXTURE_ID,
    defaultWallTextureId: DEFAULT_WALL_TEXTURE_ID,
    computeWalls,
    validateMap,
    hasValidationErrors,
    sortedUniqueStrings,
    monsters,
  });
}

function createValidatedRuntimeFloorMaps(presetPool = buildPresetCatalog(), maxAttempts = 16) {
  return createValidatedRuntimeFloorMapsModule(presetPool, maxAttempts, {
    defaultFloorTextureId: DEFAULT_FLOOR_TEXTURE_ID,
    defaultCeilingTextureId: DEFAULT_CEILING_TEXTURE_ID,
    defaultWallTextureId: DEFAULT_WALL_TEXTURE_ID,
    computeWalls,
    validateMap,
    hasValidationErrors,
    sortedUniqueStrings,
    monsters,
  });
}

function currentMapSeed() {
  return state.map?.generation?.seed ?? null;
}

function makeMap(floor = 1, seed = 18422, options = {}) {
  return makeMapModule(floor, seed, {
    ...options,
    defaultFloorTextureId: DEFAULT_FLOOR_TEXTURE_ID,
    defaultCeilingTextureId: DEFAULT_CEILING_TEXTURE_ID,
    defaultWallTextureId: DEFAULT_WALL_TEXTURE_ID,
    computeWalls,
    sortedUniqueStrings,
    monsters,
  });
}

function computeWalls(map) {
  return computeWallsModule(map, {
    wallTextureIds: WALL_TEXTURE_IDS,
    defaultWallTextureId: DEFAULT_WALL_TEXTURE_ID,
    normalizeTextureId,
  });
}

function cloneVisitedByFloor(visitedByFloor) {
  return ensureRuntimeSessionManager().cloneVisitedByFloor(visitedByFloor);
}

function cloneFloorMaps(floorMaps) {
  return ensureRuntimeSessionManager().cloneFloorMaps(floorMaps);
}

function completeFinalEnding(placement) {
  const ending = buildFinalEndingState({
    quest: state.quest,
    flags: state.flags,
    player: state.player,
    placement,
  });
  state.quest = applyFinalEndingState(state.quest, ending);
  return state.quest.ending;
}

function completePartyDefeatEnding(combat) {
  const ending = buildPartyDefeatEndingState({
    quest: state.quest,
    flags: state.flags,
    player: state.player,
    combat,
  });
  state.quest = applyPartyDefeatEndingState(state.quest, ending);
  return state.quest.ending;
}

function returnFromBoardQuest() {
  const result = grantBoardQuestReturnRewards(state.quest, {
    addGold: (amount) => { state.resources.gold += amount; },
    addXp: (amount) => { state.party.forEach((hero) => { hero.xp += amount; }); },
    addItem: (itemId) => pushInventoryItemId(itemId),
    setFlag: (flag, value) => { state.flags[flag] = value; },
  });
  activateTownState(state);
  state.combat = null;
  state.interaction = null;
  state.preEncounterSnapshot = null;
  if (result.granted) {
    const rewards = result.rewards || {};
    addLog(`${result.runtime.title} 의뢰를 보고하고 귀환했다. 금화 ${Number(rewards.gold || 0)}, XP ${Number(rewards.xp || 0)} 보상을 정산했다.`);
  } else {
    addLog("마을로 귀환했다.");
  }
  render();
  return true;
}

function makeBootstrapHero(slot, classIndex, name) {
  const classDef = classes[classIndex] || classes[0];
  return {
    id: `hero_${slot}`,
    name: name || ["코르", "사디아", "타렉", "나부"][slot] || `용병 ${slot + 1}`,
    classIndex,
    row: slot === 0 ? "전열" : "후열",
    ...classDef,
    maxHp: classDef.hp,
    hp: classDef.hp,
    status: [],
    xp: 0,
    prof: { [classDef.category]: 0 },
    trainingLevel: 0,
    passive: false,
    defend: false,
  };
}

function initialState() {
  const presetCatalog = buildPresetCatalog();
  const runtimeFloorBundle = createInitialRuntimeFloorBundle({
    presetCatalog,
    createRuntimeFloorMaps,
    compileProjectForRuntime,
    buildRuntimeSessionFloorMaps,
  });
  const floorMaps = ensureTownFloorMaps(runtimeFloorBundle.floorMaps);
  const map = floorMaps[1];
  const defaultParty = [makeBootstrapHero(0, 0, "코난")];
  const nextState = {
    mode: "title",
    floorMaps,
    map,
    player: { floor: 1, x: map.start.x, y: map.start.y, facing: map.start.facing },
    visitedByFloor: { 1: new Set([`${map.start.x},${map.start.y}`]), 2: new Set(), 3: new Set() },
    visited: new Set([`${map.start.x},${map.start.y}`]),
    flags: {},
    inventory: ["bandage", "antivenom", "throwing_knife", "firebomb"],
    resources: { torch: 60, food: 8, water: 8, gold: 45 },
    quest: createInitialQuestState(),
    party: defaultParty,
    companion: null,
    npcState: {},
    fieldMonsters: {},
    shell: {
      titlePanel: "menu",
      selectedSaveSlotId: SAVE_SLOT_IDS[0],
      newGameDraft: {
        name: "코난",
        classIndex: 0,
        backgroundId: PROTAGONIST_BACKGROUNDS[0].id,
        loadoutId: STARTER_LOADOUTS[0].id,
      },
    },
    combat: null,
    interaction: null,
    inventoryPanelOpen: false,
    inventoryPanelFilter: "all",
    inventoryPanelSort: "default",
    inventoryPanelQuery: "",
    inventoryPanelDragIndex: -1,
    inventoryPanelPreviewIndex: -1,
    skillDeckOpen: false,
    skillDeckHeroId: defaultParty[0]?.id || "",
    skillDeckSelectedSkillId: "",
    skillDeckDieIndex: 0,
    skillShopOpen: false,
    skillShopNpcId: "",
    skillShopTitle: "",
    skillShopNote: "",
    skillShopHeroId: defaultParty[0]?.id || "",
    skillShopCatalogId: "",
    skillShopSkillIds: [],
    log: ["카라쉬의 문을 떠나 세트의 침묵 사원에 진입했다."],
    presetCatalog,
    editor: null,
    runtimeSession: {
      kind: "game",
      source: runtimeFloorBundle.source,
      startedAt: new Date().toISOString(),
      accumulatedPlaytimeMs: 0,
      returnSnapshot: null,
      sourceFloor: 1,
      compileFailures: runtimeFloorBundle.compileFailures,
    },
    preEncounterSnapshot: null,
  };
  attachEditorStateAccessors(nextState, () => ensureEditorState(nextState));
  return nextState;
}

let state = initialState();

function ensureEditorState(targetState = state) {
  if (targetState.editor) return targetState.editor;
  const map = targetState.map || targetState.floorMaps?.[targetState.player?.floor] || Object.values(targetState.floorMaps || {})[0];
  const start = map?.start || { x: 1, y: 1 };
  const presetCatalog = Array.isArray(targetState.presetCatalog) && targetState.presetCatalog.length
    ? targetState.presetCatalog
    : buildPresetCatalog();
  targetState.presetCatalog = presetCatalog;
  targetState.editor = createEditorState(start, presetCatalog, buildEditorStateConfig());
  return targetState.editor;
}

function createEmptyPresetGrid(width, height) {
  return Array.from({ length: height }, () => Array.from({ length: width }, () => 0));
}

function refreshPresetCatalog(preserveSelection = true) {
  ensureEditorState();
  const catalog = buildPresetCatalog();
  state.presetCatalog = catalog;
  if (!preserveSelection || !catalog.some((preset) => preset.id === state.editor.selectedPresetId)) state.editor.selectedPresetId = catalog[0]?.id || "";
  state.editor.generationPresetIds = state.editor.generationPresetIds.filter((id) => catalog.some((preset) => preset.id === id));
  if (!state.editor.generationPresetIds.length) state.editor.generationPresetIds = catalog.map((preset) => preset.id);
}

function previewGridMarkup(grid, cellClassName) {
  return grid.map((row) => row.map((filled) => `<div class="${cellClassName} ${filled ? "is-filled" : ""}"></div>`).join("")).join("");
}

function baseLootTableDefinitionIds() {
  return Object.keys(lootTables).filter((id) => id !== "combatRewardProfiles");
}

function applyDraftFromPreset(presetId) {
  const preset = getPresetById(presetId, state.presetCatalog);
  if (!preset) return;
  state.presetDraft = {
    id: preset.kind === "builtin" ? "" : preset.id,
    name: preset.name,
    width: PRESET_DRAFT_SIZE,
    height: PRESET_DRAFT_SIZE,
    tags: [...(preset.tags || [])],
    notes: preset.notes || "",
    cells: preset.cells,
  };
  const grid = createEmptyPresetGrid(PRESET_DRAFT_SIZE, PRESET_DRAFT_SIZE);
  createPreviewGrid(preset, { width: PRESET_DRAFT_SIZE, height: PRESET_DRAFT_SIZE }).forEach((row, y) => row.forEach((value, x) => { grid[y][x] = value; }));
  state.presetDraftGrid = grid;
  state.presetDraftSelectedId = preset.kind === "builtin" ? "" : preset.id;
}

function selectedPreset() {
  return getPresetById(state.selectedPresetId, state.presetCatalog);
}

function skillName(skillId) {
  if (skillId === "fallback_basic_attack") return "기본공격";
  return skills[skillId]?.name || skillId || "기술";
}

function skillCatalogSkillIds(catalogId = "") {
  const normalizedCatalogId = String(catalogId || "").trim();
  return Object.keys(skills)
    .filter((skillId) => {
      const definition = skills[skillId] || {};
      if (normalizedCatalogId) return Array.isArray(definition.catalogIds) && definition.catalogIds.includes(normalizedCatalogId);
      return Number(definition.buyPrice || 0) > 0;
    })
    .sort((left, right) => Number(skills[left]?.buyPrice || 0) - Number(skills[right]?.buyPrice || 0));
}

function activeSkillShopHero() {
  return state.party.find((hero) => hero.id === state.skillShopHeroId) || state.party[0] || null;
}

function buySkillCard(skillId = "") {
  const hero = activeSkillShopHero();
  const definition = skills[skillId] || null;
  const price = Math.max(0, Number(definition?.buyPrice || 0));
  if (!hero || !definition || price <= 0) return false;
  if (state.resources.gold < price) {
    addLog(`${skillName(skillId)} 카드를 살 금화가 부족하다.`);
    return false;
  }
  state.resources.gold -= price;
  const nextCount = grantSkillCard(hero, skillId, 1);
  addLog(`${hero.name}이 ${skillName(skillId)} 카드를 샀다. 남은 카드 ${nextCount}.`);
  return true;
}

function sellSkillCard(skillId = "") {
  const hero = activeSkillShopHero();
  const definition = skills[skillId] || null;
  const price = Math.max(0, Number(definition?.sellPrice || 0));
  const looseCount = hero ? skillInventoryCount(hero, skillId) : 0;
  if (!hero || !definition || price <= 0 || looseCount <= 0) return false;
  consumeSkillCard(hero, skillId, 1);
  state.resources.gold += price;
  addLog(`${hero.name}이 ${skillName(skillId)} 카드를 팔았다. 금화 ${price} 획득.`);
  return true;
}

function sampleItemPreviewJson(preview) {
  return JSON.stringify(preview || {}, null, 2);
}

function shuffleCopy(list = []) {
  const next = [...list];
  for (let index = next.length - 1; index > 0; index -= 1) {
    const swapIndex = Math.floor(Math.random() * (index + 1));
    [next[index], next[swapIndex]] = [next[swapIndex], next[index]];
  }
  return next;
}

function buildSampleItemPreview(baseItemId, rarityId, poolId) {
  const base = items[baseItemId];
  const rarity = rarityDefinitions[rarityId];
  const pool = affixPoolDefinitions[poolId];
  if (!base || !rarity || !pool) return null;
  const allowedAffixes = (pool.affixIds || [])
    .map((affixId) => ({ affixId, affix: affixDefinitions[affixId] }))
    .filter(({ affix }) => affix)
    .filter(({ affix }) => !affix.rarity || affix.rarity === rarityId)
    .filter(({ affix }) => !(pool.itemKinds || []).length || (pool.itemKinds || []).includes(base.kind));
  const picked = [];
  const usedSlots = new Set();
  for (const entry of shuffleCopy(allowedAffixes)) {
    if (picked.length >= Math.max(0, Number(rarity.affixCount || 0))) break;
    if (entry.affix.slot && usedSlots.has(entry.affix.slot)) continue;
    picked.push(entry);
    if (entry.affix.slot) usedSlots.add(entry.affix.slot);
  }
  const resolved = {
    attack: Number(base.attack || 0),
    defense: Number(base.defense || 0),
    heal: Number(base.heal || 0),
    cure: base.cure || "",
    curse: Number(base.curse || 0),
    gold: 0,
  };
  for (const { affix } of picked) {
    if (affix.stat === "cure") resolved.cure = affix.value || affix.label || "";
    else if (affix.stat in resolved) resolved[affix.stat] = Number(resolved[affix.stat] || 0) + Number(affix.amount || 0);
  }
  const prefix = picked.find((entry) => entry.affix.slot === "prefix")?.affix?.label || "";
  const suffix = picked.find((entry) => entry.affix.slot === "suffix")?.affix?.label || "";
  const name = `${prefix ? `${prefix} ` : ""}${base.name || baseItemId}${suffix ? ` ${suffix}` : ""}`.trim();
  return {
    baseItemId,
    rarityId,
    affixPoolId: poolId,
    kind: base.kind,
    name,
    rarityLabel: rarity.label || rarityId,
    affixes: picked.map(({ affixId, affix }) => ({
      id: affixId,
      label: affix.label || affixId,
      slot: affix.slot,
      stat: affix.stat,
      amount: affix.amount ?? 0,
      value: affix.value ?? null,
    })),
    stats: resolved,
    valueEstimate: Math.max(1, Math.round((1 + Math.max(0, picked.length)) * Number(rarity.valueMultiplier || 1) * 10)),
  };
}

function makeGeneratedItemInstance(preview) {
  if (!preview?.baseItemId) return null;
  return {
    kind: "generated_item",
    instanceId: `generated_item_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
    itemId: preview.baseItemId,
    name: preview.name || items[preview.baseItemId]?.name || preview.baseItemId,
    rarityId: preview.rarityId || "",
    rarityLabel: preview.rarityLabel || preview.rarityId || "",
    affixPoolId: preview.affixPoolId || "",
    affixes: JSON.parse(JSON.stringify(preview.affixes || [])),
    stats: JSON.parse(JSON.stringify(preview.stats || {})),
    valueEstimate: Number(preview.valueEstimate || 0),
    identified: false,
    cursed: Number(preview.stats?.curse || 0) > 0,
  };
}

function baseItemShouldUseInstance(itemId) {
  const item = items[itemId];
  return item && ["equipment", "artifact", "quest"].includes(item.kind);
}

function createBaseItemInstance(itemId, overrides = {}) {
  const item = items[itemId];
  if (!item) return "";
  return {
    kind: "item_instance",
    itemId,
    identified: item.kind === "artifact" || item.kind === "quest" ? false : Number(item.curse || 0) <= 0,
    cursed: Number(item.curse || 0) > 0,
    ...JSON.parse(JSON.stringify(overrides || {})),
  };
}

const {
  eventEffectFlagValueType,
  eventEffectFlagValueText,
  renderEventEffectFields,
  npcHookJson,
  buildQuestSeedRegistry,
  dialogueStepLookup,
} = createEditorSupportBridge({
  eventEffectFieldMeta,
  escapeHtml,
  resourceKeys: RESOURCE_KEYS,
  items,
});

const {
  classDefinition,
  nextClassMilestone,
  nextClassMilestoneText,
  syncHeroClassDefinition,
  syncPartyClassDefinitions,
  classMilestonesJson,
  classDefinitionValidationIssues,
  validateClassDefinitionsTable,
} = createClassProgressionBridge({
  classes,
  getState: () => state,
  normalizeHeroState: (...args) => normalizeHeroState(...args),
});

const VALID_ITEM_KINDS = new Set(["consumable", "equipment", "artifact", "key", "quest"]);
const VALID_ITEM_TARGET_MODES = new Set(["enemy", "all_enemies"]);
const VALID_SKILL_KINDS = new Set(["attack", "skill", "guard", "defend", "heal", "support", "buff", "debuff", "lifesteal", "summon"]);
const VALID_SKILL_TARGET_MODES = new Set(["enemy", "ally", "self", "all_enemies", "party"]);
const VALID_SKILL_FORMULAS = new Set(["die_as_effect", "die_plus_effect", "die_minus_effect", "die_times_effect", "die_divide_effect", "die_equals_effect"]);
const VALID_VENDOR_SERVICE_TYPES = new Set(["sell_bundle", "heal_party", "buff_frontline", "train_party"]);
const VALID_NPC_SERVICE_TYPES = new Set(["talk", "quest", "quest_board", "quest_gate", "heal", "identify", "trade", "recruit", "dismiss", "fight", "travel", "skill_shop"]);
const VALID_QUEST_SEED_STATUSES = new Set(["active", "completed", "failed"]);

function itemDefinitionValidationIssues(itemId, item) {
  const issues = [];
  if (!item || typeof item !== "object" || Array.isArray(item)) {
    issues.push(`${itemId} item 정의가 object가 아니다.`);
    return issues;
  }
  if (!item.name || typeof item.name !== "string") issues.push(`${itemId} name이 비어 있거나 string이 아니다.`);
  if (!item.kind || typeof item.kind !== "string") issues.push(`${itemId} kind가 비어 있거나 string이 아니다.`);
  if (item.kind && !VALID_ITEM_KINDS.has(item.kind)) issues.push(`${itemId} kind가 지원되지 않는다: ${item.kind}`);
  for (const stat of ["heal", "attack", "defense", "curse"]) {
    if (item[stat] != null && typeof item[stat] !== "number") issues.push(`${itemId} ${stat}는 number여야 한다.`);
  }
  if (item.throwDamage != null && typeof item.throwDamage !== "number") issues.push(`${itemId} throwDamage는 number여야 한다.`);
  if (item.cure != null && typeof item.cure !== "string") issues.push(`${itemId} cure는 string이어야 한다.`);
  if (item.targetMode != null && typeof item.targetMode !== "string") issues.push(`${itemId} targetMode는 string이어야 한다.`);
  if (item.targetMode != null && typeof item.targetMode === "string" && !VALID_ITEM_TARGET_MODES.has(item.targetMode)) {
    issues.push(`${itemId} targetMode가 지원되지 않는다: ${item.targetMode}`);
  }
  if (item.slot != null && typeof item.slot !== "string") issues.push(`${itemId} slot은 string이어야 한다.`);
  if (item.rarity != null && typeof item.rarity !== "string") issues.push(`${itemId} rarity는 string이어야 한다.`);
  if (item.kind === "equipment" && !String(item.slot || "").trim()) issues.push(`${itemId} equipment에는 slot이 필요하다.`);
  if ((item.kind === "key" || item.kind === "quest") && (item.heal != null || item.throwDamage != null || item.cure)) {
    issues.push(`${itemId} ${item.kind} item은 consumable 전용 field를 가지면 안 된다.`);
  }
  return issues;
}

function validateItemDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("itemDefinitions 검증 실패: object가 아니다.");
  const issues = Object.entries(definitions).flatMap(([itemId, item]) => itemDefinitionValidationIssues(itemId, item));
  if (issues.length) throw new Error(`itemDefinitions 검증 실패: ${issues[0]}`);
}

function monsterDefinitionValidationIssues(monsterId, monster) {
  const issues = [];
  if (!monster || typeof monster !== "object" || Array.isArray(monster)) {
    issues.push(`${monsterId} monster 정의가 object가 아니다.`);
    return issues;
  }
  if (!monster.name || typeof monster.name !== "string") issues.push(`${monsterId} name이 비어 있거나 string이 아니다.`);
  for (const field of ["hp", "atk", "def", "xp"]) {
    if (monster[field] == null || typeof monster[field] !== "number") issues.push(`${monsterId} ${field}는 number여야 한다.`);
  }
  if (monster.atkMin != null && typeof monster.atkMin !== "number") issues.push(`${monsterId} atkMin은 number여야 한다.`);
  if (monster.atkMax != null && typeof monster.atkMax !== "number") issues.push(`${monsterId} atkMax는 number여야 한다.`);
  if (monster.atkMin != null && monster.atkMax != null && monster.atkMin > monster.atkMax) issues.push(`${monsterId} atkMin은 atkMax보다 클 수 없다.`);
  if (monster.ai != null && typeof monster.ai !== "string") issues.push(`${monsterId} ai는 string이어야 한다.`);
  if (monster.spawn != null) {
    if (typeof monster.spawn !== "object" || Array.isArray(monster.spawn)) issues.push(`${monsterId} spawn은 object여야 한다.`);
    if (monster.spawn?.mapKinds != null && !Array.isArray(monster.spawn.mapKinds)) issues.push(`${monsterId} spawn.mapKinds는 array여야 한다.`);
    if (monster.spawn?.themes != null && !Array.isArray(monster.spawn.themes)) issues.push(`${monsterId} spawn.themes는 array여야 한다.`);
    if (monster.spawn?.roles != null && !Array.isArray(monster.spawn.roles)) issues.push(`${monsterId} spawn.roles는 array여야 한다.`);
    if (monster.spawn?.minFloor != null && typeof monster.spawn.minFloor !== "number") issues.push(`${monsterId} spawn.minFloor는 number여야 한다.`);
    if (monster.spawn?.maxFloor != null && typeof monster.spawn.maxFloor !== "number") issues.push(`${monsterId} spawn.maxFloor는 number여야 한다.`);
    if (monster.spawn?.weight != null && typeof monster.spawn.weight !== "number") issues.push(`${monsterId} spawn.weight는 number여야 한다.`);
  }
  if (monster.scaling != null) {
    if (typeof monster.scaling !== "object" || Array.isArray(monster.scaling)) issues.push(`${monsterId} scaling은 object여야 한다.`);
    if (monster.scaling?.hpPerFloor != null && typeof monster.scaling.hpPerFloor !== "number") issues.push(`${monsterId} scaling.hpPerFloor는 number여야 한다.`);
    if (monster.scaling?.atkPerFloor != null && typeof monster.scaling.atkPerFloor !== "number") issues.push(`${monsterId} scaling.atkPerFloor는 number여야 한다.`);
  }
  return issues;
}

function validateMonsterDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("monsterDefinitions 검증 실패: object가 아니다.");
  const issues = Object.entries(definitions).flatMap(([monsterId, monster]) => monsterDefinitionValidationIssues(monsterId, monster));
  if (issues.length) throw new Error(`monsterDefinitions 검증 실패: ${issues[0]}`);
}

function skillDefinitionValidationIssues(skillId, skill) {
  const issues = [];
  if (!skill || typeof skill !== "object" || Array.isArray(skill)) {
    issues.push(`${skillId} skill 정의가 object가 아니다.`);
    return issues;
  }
  if (!skill.name || typeof skill.name !== "string") issues.push(`${skillId} name이 비어 있거나 string이 아니다.`);
  if (!skill.kind || typeof skill.kind !== "string") issues.push(`${skillId} kind가 비어 있거나 string이 아니다.`);
  if (skill.kind && !VALID_SKILL_KINDS.has(skill.kind)) issues.push(`${skillId} kind가 지원되지 않는다: ${skill.kind}`);
  if (skill.kind === "summon" && !skill.deferred) issues.push(`${skillId} summon은 아직 보류 로직이므로 deferred: true가 필요하다.`);
  if (skill.targetMode != null && typeof skill.targetMode !== "string") issues.push(`${skillId} targetMode는 string이어야 한다.`);
  if (skill.targetMode && !VALID_SKILL_TARGET_MODES.has(skill.targetMode)) issues.push(`${skillId} targetMode가 지원되지 않는다: ${skill.targetMode}`);
  if (skill.formula != null && typeof skill.formula !== "string") issues.push(`${skillId} formula는 string이어야 한다.`);
  if (skill.formula && !VALID_SKILL_FORMULAS.has(skill.formula)) issues.push(`${skillId} formula가 지원되지 않는다: ${skill.formula}`);
  for (const field of ["effect", "cooldown", "buyPrice", "sellPrice", "duration", "priority"]) {
    if (skill[field] != null && typeof skill[field] !== "number") issues.push(`${skillId} ${field}는 number여야 한다.`);
  }
  if (skill.catalogIds != null && !Array.isArray(skill.catalogIds)) issues.push(`${skillId} catalogIds는 array여야 한다.`);
  if (skill.tags != null && !Array.isArray(skill.tags)) issues.push(`${skillId} tags는 array여야 한다.`);
  if (skill.status != null && typeof skill.status !== "string") issues.push(`${skillId} status는 string이어야 한다.`);
  if (skill.description != null && typeof skill.description !== "string") issues.push(`${skillId} description은 string이어야 한다.`);
  return issues;
}

function validateSkillDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("skillDefinitions 검증 실패: object가 아니다.");
  const issues = Object.entries(definitions).flatMap(([skillId, skill]) => skillDefinitionValidationIssues(skillId, skill));
  if (issues.length) throw new Error(`skillDefinitions 검증 실패: ${issues[0]}`);
}

function questDefinitionValidationIssues(questId, questDef) {
  const issues = [];
  if (!questDef || typeof questDef !== "object" || Array.isArray(questDef)) {
    issues.push(`${questId} quest 정의가 object가 아니다.`);
    return issues;
  }
  if (!questDef.name || typeof questDef.name !== "string") issues.push(`${questId} name이 비어 있거나 string이 아니다.`);
  if (!questDef.mapKind || typeof questDef.mapKind !== "string") issues.push(`${questId} mapKind가 비어 있거나 string이 아니다.`);
  if (questDef.startFloor != null && typeof questDef.startFloor !== "number") issues.push(`${questId} startFloor는 number여야 한다.`);
  if (questDef.conditions != null && (typeof questDef.conditions !== "object" || Array.isArray(questDef.conditions))) issues.push(`${questId} conditions는 object여야 한다.`);
  if (questDef.conditions?.bossesDefeatedAtLeast != null && typeof questDef.conditions.bossesDefeatedAtLeast !== "number") issues.push(`${questId} conditions.bossesDefeatedAtLeast는 number여야 한다.`);
  if (questDef.conditions?.requiredCount != null && typeof questDef.conditions.requiredCount !== "number") issues.push(`${questId} conditions.requiredCount는 number여야 한다.`);
  if (questDef.conditions?.targetMonsterIds != null && !Array.isArray(questDef.conditions.targetMonsterIds)) issues.push(`${questId} conditions.targetMonsterIds는 array여야 한다.`);
  for (const [index, monsterId] of (questDef.conditions?.targetMonsterIds || []).entries()) {
    if (!monsterId || !monsters[monsterId]) issues.push(`${questId} conditions.targetMonsterIds[${index}]가 알 수 없는 monster를 참조한다: ${monsterId || "(empty)"}`);
  }
  if (questDef.rewards != null && (typeof questDef.rewards !== "object" || Array.isArray(questDef.rewards))) issues.push(`${questId} rewards는 object여야 한다.`);
  if (questDef.rewards?.gold != null && typeof questDef.rewards.gold !== "number") issues.push(`${questId} rewards.gold는 number여야 한다.`);
  if (questDef.rewards?.xp != null && typeof questDef.rewards.xp !== "number") issues.push(`${questId} rewards.xp는 number여야 한다.`);
  for (const [index, reward] of (questDef.rewards?.items || []).entries()) {
    if (!reward?.itemId || !items[reward.itemId]) issues.push(`${questId} rewards.items[${index}]가 알 수 없는 item을 참조한다: ${reward?.itemId || "(empty)"}`);
    if (reward.quantity != null && typeof reward.quantity !== "number") issues.push(`${questId} rewards.items[${index}].quantity는 number여야 한다.`);
  }
  return issues;
}

function validateQuestDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("questDefinitions 검증 실패: object가 아니다.");
  const issues = Object.entries(definitions).flatMap(([questId, questDef]) => questDefinitionValidationIssues(questId, questDef));
  if (issues.length) throw new Error(`questDefinitions 검증 실패: ${issues[0]}`);
}

function vendorDefinitionValidationIssues(vendorId, vendor) {
  const issues = [];
  if (!vendor || typeof vendor !== "object" || Array.isArray(vendor)) {
    issues.push(`${vendorId} vendor 정의가 object가 아니다.`);
    return issues;
  }
  if (!vendor.serviceType || typeof vendor.serviceType !== "string") issues.push(`${vendorId} serviceType이 비어 있거나 string이 아니다.`);
  if (vendor.serviceType && !VALID_VENDOR_SERVICE_TYPES.has(vendor.serviceType)) issues.push(`${vendorId} serviceType이 지원되지 않는다: ${vendor.serviceType}`);
  if (vendor.summary != null && typeof vendor.summary !== "string") issues.push(`${vendorId} summary는 string이어야 한다.`);
  if (vendor.cost?.gold != null && typeof vendor.cost.gold !== "number") issues.push(`${vendorId} cost.gold는 number여야 한다.`);
  if (vendor.inventory != null && !Array.isArray(vendor.inventory)) issues.push(`${vendorId} inventory는 array여야 한다.`);
  for (const [index, entry] of (vendor.inventory || []).entries()) {
    const itemId = vendorInventoryEntryItemId(entry);
    if (!itemId || !items[itemId]) issues.push(`${vendorId} inventory[${index}]가 알 수 없는 item을 참조한다: ${itemId || "(empty)"}`);
    if (entry?.generated) {
      if (!entry.rarityId || !rarityDefinitions[entry.rarityId]) issues.push(`${vendorId} inventory[${index}] generated rarityId가 지원되지 않는다: ${entry?.rarityId || "(empty)"}`);
      if (!entry.affixPoolId || !affixPoolDefinitions[entry.affixPoolId]) issues.push(`${vendorId} inventory[${index}] generated affixPoolId가 지원되지 않는다: ${entry?.affixPoolId || "(empty)"}`);
    }
  }
  if (vendor.rotation != null && !Array.isArray(vendor.rotation)) issues.push(`${vendorId} rotation은 array여야 한다.`);
  for (const [index, rotation] of (vendor.rotation || []).entries()) {
    if (!rotation || typeof rotation !== "object") {
      issues.push(`${vendorId} rotation[${index}]가 object가 아니다.`);
      continue;
    }
    if (rotation.summary != null && typeof rotation.summary !== "string") issues.push(`${vendorId} rotation[${index}] summary는 string이어야 한다.`);
    if (rotation.cost?.gold != null && typeof rotation.cost.gold !== "number") issues.push(`${vendorId} rotation[${index}] cost.gold는 number여야 한다.`);
    if (rotation.inventory != null && !Array.isArray(rotation.inventory)) issues.push(`${vendorId} rotation[${index}] inventory는 array여야 한다.`);
    for (const [itemIndex, entry] of (rotation.inventory || []).entries()) {
      const itemId = vendorInventoryEntryItemId(entry);
      if (!itemId || !items[itemId]) issues.push(`${vendorId} rotation[${index}] inventory[${itemIndex}]가 알 수 없는 item을 참조한다: ${itemId || "(empty)"}`);
      if (entry?.generated) {
        if (!entry.rarityId || !rarityDefinitions[entry.rarityId]) issues.push(`${vendorId} rotation[${index}] inventory[${itemIndex}] generated rarityId가 지원되지 않는다: ${entry?.rarityId || "(empty)"}`);
        if (!entry.affixPoolId || !affixPoolDefinitions[entry.affixPoolId]) issues.push(`${vendorId} rotation[${index}] inventory[${itemIndex}] generated affixPoolId가 지원되지 않는다: ${entry?.affixPoolId || "(empty)"}`);
      }
    }
    if (rotation.when?.minFloor != null && typeof rotation.when.minFloor !== "number") issues.push(`${vendorId} rotation[${index}] when.minFloor는 number여야 한다.`);
    if (rotation.when?.maxFloor != null && typeof rotation.when.maxFloor !== "number") issues.push(`${vendorId} rotation[${index}] when.maxFloor는 number여야 한다.`);
    if (rotation.when?.bossesDefeatedAtLeast != null && typeof rotation.when.bossesDefeatedAtLeast !== "number") issues.push(`${vendorId} rotation[${index}] when.bossesDefeatedAtLeast는 number여야 한다.`);
  }
  return issues;
}

function validateVendorDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("vendorDefinitions 검증 실패: object가 아니다.");
  const issues = Object.entries(definitions).flatMap(([vendorId, vendor]) => vendorDefinitionValidationIssues(vendorId, vendor));
  if (issues.length) throw new Error(`vendorDefinitions 검증 실패: ${issues[0]}`);
}

function lootEntryValidationIssues(ownerLabel, entries) {
  const issues = [];
  if (entries != null && !Array.isArray(entries)) {
    issues.push(`${ownerLabel} entries는 array여야 한다.`);
    return issues;
  }
  for (const [index, entry] of (entries || []).entries()) {
    if (!entry?.itemId || !items[entry.itemId]) issues.push(`${ownerLabel} entries[${index}]가 알 수 없는 item을 참조한다: ${entry?.itemId || "(empty)"}`);
    if (entry?.quantity != null && typeof entry.quantity !== "number") issues.push(`${ownerLabel} entries[${index}] quantity는 number여야 한다.`);
    if (entry?.weight != null && typeof entry.weight !== "number") issues.push(`${ownerLabel} entries[${index}] weight는 number여야 한다.`);
    if (entry?.generated) {
      if (!entry.rarityId || !rarityDefinitions[entry.rarityId]) issues.push(`${ownerLabel} entries[${index}] generated reward rarityId가 지원되지 않는다: ${entry?.rarityId || "(empty)"}`);
      if (!entry.affixPoolId || !affixPoolDefinitions[entry.affixPoolId]) issues.push(`${ownerLabel} entries[${index}] generated reward affixPoolId가 지원되지 않는다: ${entry?.affixPoolId || "(empty)"}`);
    }
  }
  return issues;
}

function lootTableDefinitionValidationIssues(tableId, table) {
  const issues = [];
  if (!table || typeof table !== "object" || Array.isArray(table)) {
    issues.push(`${tableId} loot table 정의가 object가 아니다.`);
    return issues;
  }
  if (table.rolls != null && typeof table.rolls !== "number") issues.push(`${tableId} rolls는 number여야 한다.`);
  issues.push(...lootEntryValidationIssues(`${tableId} guaranteed`, table.guaranteed || []));
  if (table.tierEntries != null && !Array.isArray(table.tierEntries)) issues.push(`${tableId} tierEntries는 array여야 한다.`);
  for (const [index, tier] of (table.tierEntries || []).entries()) {
    if (tier?.weight != null && typeof tier.weight !== "number") issues.push(`${tableId} tierEntries[${index}] weight는 number여야 한다.`);
    issues.push(...lootEntryValidationIssues(`${tableId} tierEntries[${index}]`, tier?.entries || []));
  }
  if (table.bonusRolls != null && !Array.isArray(table.bonusRolls)) issues.push(`${tableId} bonusRolls는 array여야 한다.`);
  for (const [index, bonus] of (table.bonusRolls || []).entries()) {
    if (bonus?.chance != null && typeof bonus.chance !== "number") issues.push(`${tableId} bonusRolls[${index}] chance는 number여야 한다.`);
    issues.push(...lootEntryValidationIssues(`${tableId} bonusRolls[${index}]`, bonus?.entries || []));
  }
  return issues;
}

function validateLootTableDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("lootTableDefinitions 검증 실패: object가 아니다.");
  const issues = [];
  if (definitions.combatRewardProfiles != null) {
    const profiles = definitions.combatRewardProfiles?.default;
    if (profiles != null && !Array.isArray(profiles)) issues.push("combatRewardProfiles.default는 array여야 한다.");
    for (const [index, profile] of (profiles || []).entries()) {
      if (!profile?.tableId || !definitions[profile.tableId]) issues.push(`combatRewardProfiles.default[${index}]가 알 수 없는 tableId를 참조한다: ${profile?.tableId || "(empty)"}`);
    }
  }
  for (const [tableId, table] of Object.entries(definitions)) {
    if (tableId === "combatRewardProfiles") continue;
    issues.push(...lootTableDefinitionValidationIssues(tableId, table));
  }
  if (issues.length) throw new Error(`lootTableDefinitions 검증 실패: ${issues[0]}`);
}

function validateRarityDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("rarityDefinitions 검증 실패: object가 아니다.");
  const issues = [];
  for (const [rarityId, rarity] of Object.entries(definitions)) {
    if (!rarity?.label || typeof rarity.label !== "string") issues.push(`${rarityId} label이 비어 있거나 string이 아니다.`);
    if (rarity?.weight != null && typeof rarity.weight !== "number") issues.push(`${rarityId} weight는 number여야 한다.`);
    if (rarity?.valueMultiplier != null && typeof rarity.valueMultiplier !== "number") issues.push(`${rarityId} valueMultiplier는 number여야 한다.`);
    if (rarity?.affixCount != null && typeof rarity.affixCount !== "number") issues.push(`${rarityId} affixCount는 number여야 한다.`);
  }
  if (issues.length) throw new Error(`rarityDefinitions 검증 실패: ${issues[0]}`);
}

function validateAffixDefinitionsTable(definitions, rarityDefs = rarityDefinitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("affixDefinitions 검증 실패: object가 아니다.");
  const issues = [];
  for (const [affixId, affix] of Object.entries(definitions)) {
    if (!affix?.label || typeof affix.label !== "string") issues.push(`${affixId} label이 비어 있거나 string이 아니다.`);
    if (!affix?.slot || typeof affix.slot !== "string") issues.push(`${affixId} slot이 비어 있거나 string이 아니다.`);
    if (!affix?.stat || typeof affix.stat !== "string") issues.push(`${affixId} stat이 비어 있거나 string이 아니다.`);
    if (affix?.amount != null && typeof affix.amount !== "number") issues.push(`${affixId} amount는 number여야 한다.`);
    if (affix?.rarity != null && !rarityDefs[affix.rarity]) issues.push(`${affixId} rarity가 알 수 없는 rarity를 참조한다: ${affix.rarity}`);
  }
  if (issues.length) throw new Error(`affixDefinitions 검증 실패: ${issues[0]}`);
}

function validateAffixPoolDefinitionsTable(definitions, affixDefs = affixDefinitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("affixPoolDefinitions 검증 실패: object가 아니다.");
  const issues = [];
  for (const [poolId, pool] of Object.entries(definitions)) {
    if (!pool?.label || typeof pool.label !== "string") issues.push(`${poolId} label이 비어 있거나 string이 아니다.`);
    if (pool?.itemKinds != null && !Array.isArray(pool.itemKinds)) issues.push(`${poolId} itemKinds는 array여야 한다.`);
    if (pool?.affixIds != null && !Array.isArray(pool.affixIds)) issues.push(`${poolId} affixIds는 array여야 한다.`);
    for (const affixId of pool?.affixIds || []) {
      if (!affixDefs[affixId]) issues.push(`${poolId} affixIds가 알 수 없는 affix를 참조한다: ${affixId}`);
    }
  }
  if (issues.length) throw new Error(`affixPoolDefinitions 검증 실패: ${issues[0]}`);
}

function npcDefinitionValidationIssues(npcId, npc, questSeedRegistry = buildQuestSeedRegistry(npcs)) {
  const issues = [];
  if (!npc || typeof npc !== "object") {
    issues.push(`${npcId} npc 정의가 object가 아니다.`);
    return issues;
  }
  if (!npc.name) issues.push(`${npcId} name이 비어 있다.`);
  if (npc.progressionHooks != null && typeof npc.progressionHooks !== "object") issues.push(`${npcId} progressionHooks는 object여야 한다.`);
  const focus = npc?.progressionHooks?.focusClassIndices;
  if (focus != null && !Array.isArray(focus)) issues.push(`${npcId} progressionHooks.focusClassIndices는 array여야 한다.`);
  for (const classIndex of focus || []) {
    if (!Number.isInteger(classIndex) || classIndex < 0 || classIndex >= classes.length) issues.push(`${npcId} progressionHooks.focusClassIndices에 잘못된 class index가 있다: ${classIndex}`);
  }
  if (npc.questHooks != null && !Array.isArray(npc.questHooks)) issues.push(`${npcId} questHooks는 array여야 한다.`);
  for (const [hookIndex, hook] of (npc.questHooks || []).entries()) {
    if (!hook?.note) issues.push(`${npcId} questHooks[${hookIndex}] note가 비어 있다.`);
    if (hook?.bossesDefeatedAtLeast != null && typeof hook.bossesDefeatedAtLeast !== "number") issues.push(`${npcId} questHooks[${hookIndex}] bossesDefeatedAtLeast는 number여야 한다.`);
  }
  if (npc.questSeeds != null && !Array.isArray(npc.questSeeds)) issues.push(`${npcId} questSeeds는 array여야 한다.`);
  for (const [seedIndex, seed] of (npc.questSeeds || []).entries()) {
    if (!seed?.id) issues.push(`${npcId} questSeeds[${seedIndex}] id가 비어 있다.`);
    if (!seed?.title) issues.push(`${npcId} questSeeds[${seedIndex}] title이 비어 있다.`);
    if (!seed?.note) issues.push(`${npcId} questSeeds[${seedIndex}] note가 비어 있다.`);
    if (seed?.bossesDefeatedAtLeast != null && typeof seed.bossesDefeatedAtLeast !== "number") issues.push(`${npcId} questSeeds[${seedIndex}] bossesDefeatedAtLeast는 number여야 한다.`);
    if (seed?.requiredFlag != null && typeof seed.requiredFlag !== "string") issues.push(`${npcId} questSeeds[${seedIndex}] requiredFlag는 string이어야 한다.`);
    if (seed?.grantFlag != null && typeof seed.grantFlag !== "string") issues.push(`${npcId} questSeeds[${seedIndex}] grantFlag는 string이어야 한다.`);
    if (seed?.failureFlag != null && typeof seed.failureFlag !== "string") issues.push(`${npcId} questSeeds[${seedIndex}] failureFlag는 string이어야 한다.`);
    if (seed?.objectives != null && !Array.isArray(seed.objectives)) issues.push(`${npcId} questSeeds[${seedIndex}] objectives는 array여야 한다.`);
    if (seed?.rewards != null && typeof seed.rewards !== "object") issues.push(`${npcId} questSeeds[${seedIndex}] rewards는 object여야 한다.`);
    if (Array.isArray(seed?.objectives) && seed.objectives.some((entry) => !String(entry || "").trim())) {
      issues.push(`${npcId} questSeeds[${seedIndex}] objectives에는 빈 문자열을 둘 수 없다.`);
    }
    if (seed?.rewards?.gold != null && typeof seed.rewards.gold !== "number") issues.push(`${npcId} questSeeds[${seedIndex}] rewards.gold는 number여야 한다.`);
    if (seed?.rewards?.xp != null && typeof seed.rewards.xp !== "number") issues.push(`${npcId} questSeeds[${seedIndex}] rewards.xp는 number여야 한다.`);
    if (seed?.rewards?.flag != null && typeof seed.rewards.flag !== "string") issues.push(`${npcId} questSeeds[${seedIndex}] rewards.flag는 string이어야 한다.`);
    if (seed?.rewards?.items != null && !Array.isArray(seed.rewards.items)) issues.push(`${npcId} questSeeds[${seedIndex}] rewards.items는 array여야 한다.`);
    for (const [rewardIndex, reward] of (seed?.rewards?.items || []).entries()) {
      if (typeof reward === "string") {
        if (!items[reward]) issues.push(`${npcId} questSeeds[${seedIndex}] rewards.items[${rewardIndex}]가 알 수 없는 item을 참조한다: ${reward}`);
        continue;
      }
      if (!reward?.itemId || !items[reward.itemId]) issues.push(`${npcId} questSeeds[${seedIndex}] rewards.items[${rewardIndex}]가 알 수 없는 item을 참조한다: ${reward?.itemId || "(empty)"}`);
      if (reward?.quantity != null && typeof reward.quantity !== "number") issues.push(`${npcId} questSeeds[${seedIndex}] rewards.items[${rewardIndex}] quantity는 number여야 한다.`);
    }
    if (seed?.completeEventId != null && typeof seed.completeEventId !== "string") issues.push(`${npcId} questSeeds[${seedIndex}] completeEventId는 string이어야 한다.`);
    if (seed?.completeEventId && !eventDefinitions[seed.completeEventId]) issues.push(`${npcId} questSeeds[${seedIndex}] completeEventId가 알 수 없는 event를 참조한다: ${seed.completeEventId}`);
    if (seed?.id && questSeedRegistry.duplicates.has(seed.id)) issues.push(`${npcId} questSeeds[${seedIndex}] id가 다른 NPC와 중복된다: ${seed.id}`);
  }
  if (npc.services != null && !Array.isArray(npc.services)) issues.push(`${npcId} services는 array여야 한다.`);
  for (const [serviceIndex, service] of (npc.services || []).entries()) {
    if (!service?.type || typeof service.type !== "string") issues.push(`${npcId} services[${serviceIndex}] type이 비어 있거나 string이 아니다.`);
    if (service?.label != null && typeof service.label !== "string") issues.push(`${npcId} services[${serviceIndex}] label은 string이어야 한다.`);
    if (service?.type && !VALID_NPC_SERVICE_TYPES.has(service.type)) issues.push(`${npcId} services[${serviceIndex}] type이 지원되지 않는다: ${service.type}`);
    if (service?.cost?.gold != null && typeof service.cost.gold !== "number") issues.push(`${npcId} services[${serviceIndex}] cost.gold는 number여야 한다.`);
    if (service?.avoidCost?.gold != null && typeof service.avoidCost.gold !== "number") issues.push(`${npcId} services[${serviceIndex}] avoidCost.gold는 number여야 한다.`);
    if (service?.type === "quest" && !(npc.questSeeds || []).length) issues.push(`${npcId} services[${serviceIndex}] quest service인데 questSeeds가 없다.`);
    if (service?.type === "trade") {
      if (!service.vendorId || !vendors[service.vendorId]) issues.push(`${npcId} services[${serviceIndex}] trade vendorId가 알 수 없는 vendor를 참조한다: ${service?.vendorId || "(empty)"}`);
    }
    if (service?.type === "fight") {
      if (!service.encounterId || !encounters[service.encounterId]) issues.push(`${npcId} services[${serviceIndex}] fight encounterId가 알 수 없는 encounter를 참조한다: ${service?.encounterId || "(empty)"}`);
    }
    if (service?.type === "travel") {
      if (service.targetFloor != null && !Number.isInteger(Number(service.targetFloor))) {
        issues.push(`${npcId} services[${serviceIndex}] travel targetFloor는 integer여야 한다: ${service.targetFloor}`);
      }
      if (service.targetMode != null && !["town", "dungeon"].includes(service.targetMode)) {
        issues.push(`${npcId} services[${serviceIndex}] travel targetMode가 지원되지 않는다: ${service.targetMode}`);
      }
    }
    if (service?.type === "skill_shop") {
      if (service.catalogId != null && typeof service.catalogId !== "string") {
        issues.push(`${npcId} services[${serviceIndex}] skill_shop catalogId는 string이어야 한다.`);
      }
      if (service.catalogId && !skillCatalogSkillIds(service.catalogId).length) {
        issues.push(`${npcId} services[${serviceIndex}] skill_shop catalogId에 연결된 스킬이 없다: ${service.catalogId}`);
      }
      if (service.skillIds != null && !Array.isArray(service.skillIds)) {
        issues.push(`${npcId} services[${serviceIndex}] skill_shop skillIds는 array여야 한다.`);
      }
      (service.skillIds || []).forEach((skillId, skillIndex) => {
        if (!skills[skillId]) issues.push(`${npcId} services[${serviceIndex}] skill_shop skillIds[${skillIndex}]가 알 수 없는 skill을 참조한다: ${skillId}`);
      });
    }
    if (service?.type === "recruit") {
      if (service.companionProfile != null && typeof service.companionProfile !== "object") issues.push(`${npcId} services[${serviceIndex}] companionProfile은 object여야 한다.`);
      if (!String(service.companionProfile?.name || "").trim()) issues.push(`${npcId} services[${serviceIndex}] recruit companionProfile.name이 비어 있다.`);
      if (!Number.isInteger(Number(service.companionProfile?.classIndex ?? NaN)) || !classes[Number(service.companionProfile?.classIndex)]) {
        issues.push(`${npcId} services[${serviceIndex}] recruit companionProfile.classIndex가 잘못됐다: ${service.companionProfile?.classIndex ?? "(empty)"}`);
      }
    }
    if (service?.type === "talk" && service.dialogue != null) {
      if (!service.dialogue || typeof service.dialogue !== "object" || Array.isArray(service.dialogue)) {
        issues.push(`${npcId} services[${serviceIndex}] dialogue는 object여야 한다.`);
      } else {
        const { steps, ids } = dialogueStepLookup(service.dialogue);
        if (!Array.isArray(service.dialogue.steps)) issues.push(`${npcId} services[${serviceIndex}] dialogue.steps는 array여야 한다.`);
        steps.forEach((step, stepIndex) => {
          const stepId = String(step?.id || "").trim();
          if (!stepId) issues.push(`${npcId} services[${serviceIndex}] dialogue.steps[${stepIndex}] id가 비어 있다.`);
          if (stepId && ids.has(stepId)) issues.push(`${npcId} services[${serviceIndex}] dialogue.steps[${stepIndex}] id가 중복된다: ${stepId}`);
          if (stepId) ids.add(stepId);
          if (step?.nextStepId && !ids.has(step.nextStepId) && !steps.some((candidate) => candidate?.id === step.nextStepId)) {
            issues.push(`${npcId} services[${serviceIndex}] dialogue.steps[${stepIndex}] nextStepId가 존재하지 않는다: ${step.nextStepId}`);
          }
          if (step?.choices != null && !Array.isArray(step.choices)) issues.push(`${npcId} services[${serviceIndex}] dialogue.steps[${stepIndex}] choices는 array여야 한다.`);
          (step?.choices || []).forEach((choice, choiceIndex) => {
            if (!String(choice?.label || "").trim()) issues.push(`${npcId} services[${serviceIndex}] dialogue.steps[${stepIndex}] choices[${choiceIndex}] label이 비어 있다.`);
            if (choice?.nextStepId && !steps.some((candidate) => candidate?.id === choice.nextStepId)) {
              issues.push(`${npcId} services[${serviceIndex}] dialogue.steps[${stepIndex}] choices[${choiceIndex}] nextStepId가 존재하지 않는다: ${choice.nextStepId}`);
            }
          });
        });
        if (service.dialogue.entryStepId && !steps.some((step) => step?.id === service.dialogue.entryStepId)) {
          issues.push(`${npcId} services[${serviceIndex}] dialogue.entryStepId가 존재하지 않는다: ${service.dialogue.entryStepId}`);
        }
      }
    }
  }
  return issues;
}

function validateNpcDefinitionsTable(definitions) {
  if (!definitions || typeof definitions !== "object" || Array.isArray(definitions)) throw new Error("npcDefinitions 검증 실패: object가 아니다.");
  const questSeedRegistry = buildQuestSeedRegistry(definitions);
  const issues = Object.entries(definitions).flatMap(([npcId, npc]) => npcDefinitionValidationIssues(npcId, npc, questSeedRegistry));
  if (issues.length) throw new Error(`npcDefinitions 검증 실패: ${issues[0]}`);
}

function validateQuestSeedReferenceIntegrity(npcDefinitions = npcs, definitions = eventDefinitions) {
  const questSeedRegistry = buildQuestSeedRegistry(npcDefinitions);
  const questSeedIds = new Set(Object.keys(questSeedRegistry.byId));
  const issues = [];
  Object.entries(definitions || {}).forEach(([eventId, eventDef]) => {
    (eventDef?.effects || []).forEach((effect, index) => {
      if (effect?.kind !== "set_quest_seed_state" || !effect.questSeedId) return;
      if (!questSeedIds.has(effect.questSeedId)) issues.push(`${eventId} effects[${index}]가 알 수 없는 quest seed를 참조한다: ${effect.questSeedId}`);
      if (effect?.status != null && !VALID_QUEST_SEED_STATUSES.has(effect.status)) issues.push(`${eventId} effects[${index}] status가 지원되지 않는다: ${effect.status}`);
    });
    (eventDef?.steps || []).forEach((step, stepIndex) => {
      (step?.branches || []).forEach((branch, branchIndex) => {
        if (branch?.requiredQuestSeedId && !questSeedIds.has(branch.requiredQuestSeedId)) {
          issues.push(`${eventId} steps[${stepIndex}] branches[${branchIndex}]가 알 수 없는 quest seed를 참조한다: ${branch.requiredQuestSeedId}`);
        }
        if (branch?.requiredQuestSeedStatus != null && !VALID_QUEST_SEED_STATUSES.has(branch.requiredQuestSeedStatus)) {
          issues.push(`${eventId} steps[${stepIndex}] branches[${branchIndex}] requiredQuestSeedStatus가 지원되지 않는다: ${branch.requiredQuestSeedStatus}`);
        }
      });
      (step?.choices || []).forEach((choice, choiceIndex) => {
        if (choice?.requiredQuestSeedId && !questSeedIds.has(choice.requiredQuestSeedId)) {
          issues.push(`${eventId} steps[${stepIndex}] choices[${choiceIndex}]가 알 수 없는 quest seed를 참조한다: ${choice.requiredQuestSeedId}`);
        }
        if (choice?.requiredQuestSeedStatus != null && !VALID_QUEST_SEED_STATUSES.has(choice.requiredQuestSeedStatus)) {
          issues.push(`${eventId} steps[${stepIndex}] choices[${choiceIndex}] requiredQuestSeedStatus가 지원되지 않는다: ${choice.requiredQuestSeedStatus}`);
        }
      });
      (step?.effects || []).forEach((effect, effectIndex) => {
        if (effect?.kind !== "set_quest_seed_state" || !effect.questSeedId) return;
        if (!questSeedIds.has(effect.questSeedId)) issues.push(`${eventId} steps[${stepIndex}] effects[${effectIndex}]가 알 수 없는 quest seed를 참조한다: ${effect.questSeedId}`);
        if (effect?.status != null && !VALID_QUEST_SEED_STATUSES.has(effect.status)) issues.push(`${eventId} steps[${stepIndex}] effects[${effectIndex}] status가 지원되지 않는다: ${effect.status}`);
      });
    });
  });
  if (issues.length) throw new Error(`questSeed reference 검증 실패: ${issues[0]}`);
}

function replaceClassDefinitions(definitions) {
  validateClassDefinitionsTable(definitions);
  classes.splice(0, classes.length, ...JSON.parse(JSON.stringify(definitions)));
  syncPartyClassDefinitions();
}

function replaceItemDefinitions(definitions) {
  validateItemDefinitionsTable(definitions);
  Object.keys(items).forEach((key) => delete items[key]);
  Object.assign(items, JSON.parse(JSON.stringify(definitions)));
}

function replaceMonsterDefinitions(definitions) {
  validateMonsterDefinitionsTable(definitions);
  Object.keys(monsters).forEach((key) => delete monsters[key]);
  Object.assign(monsters, JSON.parse(JSON.stringify(definitions)));
}

function replaceSkillDefinitions(definitions) {
  validateSkillDefinitionsTable(definitions);
  Object.keys(skills).forEach((key) => delete skills[key]);
  Object.assign(skills, JSON.parse(JSON.stringify(definitions)));
}

function replaceQuestDefinitions(definitions) {
  validateQuestDefinitionsTable(definitions);
  Object.keys(questDefinitions).forEach((key) => delete questDefinitions[key]);
  Object.assign(questDefinitions, JSON.parse(JSON.stringify(definitions)));
}

function replaceVendorDefinitions(definitions) {
  validateVendorDefinitionsTable(definitions);
  Object.keys(vendors).forEach((key) => delete vendors[key]);
  Object.assign(vendors, JSON.parse(JSON.stringify(definitions)));
}

function replaceLootTableDefinitions(definitions) {
  validateLootTableDefinitionsTable(definitions);
  Object.keys(lootTables).forEach((key) => delete lootTables[key]);
  Object.assign(lootTables, JSON.parse(JSON.stringify(definitions)));
}

function replaceRarityDefinitions(definitions) {
  validateRarityDefinitionsTable(definitions);
  Object.keys(rarityDefinitions).forEach((key) => delete rarityDefinitions[key]);
  Object.assign(rarityDefinitions, JSON.parse(JSON.stringify(definitions)));
}

function replaceAffixDefinitions(definitions) {
  validateAffixDefinitionsTable(definitions);
  Object.keys(affixDefinitions).forEach((key) => delete affixDefinitions[key]);
  Object.assign(affixDefinitions, JSON.parse(JSON.stringify(definitions)));
}

function replaceAffixPoolDefinitions(definitions) {
  validateAffixPoolDefinitionsTable(definitions);
  Object.keys(affixPoolDefinitions).forEach((key) => delete affixPoolDefinitions[key]);
  Object.assign(affixPoolDefinitions, JSON.parse(JSON.stringify(definitions)));
}

function replaceNpcDefinitions(definitions) {
  validateNpcDefinitionsTable(definitions);
  Object.keys(npcs).forEach((key) => delete npcs[key]);
  Object.assign(npcs, JSON.parse(JSON.stringify(definitions)));
}

validateClassDefinitionsTable(classes);
validateSkillDefinitionsTable(skills);
validateNpcDefinitionsTable(npcs);

function stepBranchMatches(branch) {
  if (!branch) return false;
  if (branch.requiredFlag && !state.flags[branch.requiredFlag]) return false;
  if (branch.missingFlag && state.flags[branch.missingFlag]) return false;
  if (branch.requiredResource && Number(state.resources?.[branch.requiredResource] || 0) < Number(branch.requiredResourceAtLeast || 1)) return false;
  if (branch.requiredCompanionState && !matchesRequiredCompanionState(branch.requiredCompanionState)) return false;
  if (branch.requiredClassIndex != null && !state.party.some((hero) => hero.classIndex === Number(branch.requiredClassIndex))) return false;
  if (branch.requiredStatKey && !state.party.some((hero) => Number(hero?.[branch.requiredStatKey] || 0) >= Number(branch.requiredStatAtLeast || 1))) return false;
  if (branch.requiredQuestSeedId) {
    const runtime = state.quest?.seeds?.[branch.requiredQuestSeedId];
    if (!runtime) return false;
    if (branch.requiredQuestSeedStatus && runtime.status !== branch.requiredQuestSeedStatus) return false;
  }
  return true;
}

function stepChoiceMatches(choice) {
  if (!choice) return false;
  if (choice.requiredFlag && !state.flags[choice.requiredFlag]) return false;
  if (choice.missingFlag && state.flags[choice.missingFlag]) return false;
  if (choice.requiredResource && Number(state.resources?.[choice.requiredResource] || 0) < Number(choice.requiredResourceAtLeast || 1)) return false;
  if (choice.requiredCompanionState && !matchesRequiredCompanionState(choice.requiredCompanionState)) return false;
  if (choice.requiredClassIndex != null && !state.party.some((hero) => hero.classIndex === Number(choice.requiredClassIndex))) return false;
  if (choice.requiredStatKey && !state.party.some((hero) => Number(hero?.[choice.requiredStatKey] || 0) >= Number(choice.requiredStatAtLeast || 1))) return false;
  if (choice.requiredQuestSeedId) {
    const runtime = state.quest?.seeds?.[choice.requiredQuestSeedId];
    if (!runtime) return false;
    if (choice.requiredQuestSeedStatus && runtime.status !== choice.requiredQuestSeedStatus) return false;
  }
  return true;
}

function eventStepMap(event) {
  return new Map((event?.steps || []).map((step) => [step.id, step]));
}

function closeInteraction() {
  state.interaction = null;
}

const {
  currentEditorEventTestSession,
  stopEditorEventTestSession,
  openEventChoiceInteraction,
  continueEventFlow,
  resolveEventChoice,
  startEditorEventTestSession,
  applyEventEffects,
  runTypedEventEffects,
  canDetectTrap,
  canDisarmTrap,
  ensureEventRuntimeState,
  eventUsageState,
  spendEventUsage,
  advanceWorldTurn,
} = createEventRuntimeBridge({
  getState: () => state,
  ensureEventRuntime: (...args) => ensureEventRuntime(...args),
  closeInteraction: (...args) => closeInteraction(...args),
});

const {
  ensureNpcRuntimeState,
  npcTalkText,
  npcTalkService,
  npcDialogueStepMap,
  hasNpcDialogueTree,
  openNpcDialogueInteraction,
  hasCompanionFromNpc,
  npcQuestServiceSnapshot,
  npcQuestServiceLabel,
  npcQuestServiceText,
  npcServicePreviewText,
  npcServicePreviewList,
  buildNpcInteractionOptions,
  openNpcInteraction,
  resolveNpcHandoffPlacement,
  queueNpcHandoff,
  flushPendingNpcHandoff,
  recruitNpcCompanion,
  dismissNpcCompanion,
  identifyWithNpc,
  tradeWithNpc,
  healWithNpc,
  fightNpc,
  avoidNpcFight,
  resolveNpcQuestService,
  resolveNpcService,
  runNpcPlacement,
} = createNpcRuntimeBridge({
  getState: () => state,
  ensureNpcRuntime: (...args) => ensureNpcRuntime(...args),
});

const {
  placementEncounterId,
  startCombat,
  livingParty,
  livingCombatEnemies,
  currentCombatHero,
  combatConsumableEntries,
  syncPartyRows,
  handlePartyDefeatInCombat,
  endCombatRound,
  restorePreEncounterPosition,
  finishHeroAction,
  swapHeroForCombat,
  resolveHeroAction,
  queueCombatAction,
  clearCombatDiceSelection,
  selectCombatDie,
  assignHeroSkillToDieFace,
  useCombatConsumable,
  useCombatThrowItem,
  enemyTurn,
  winCombat,
} = createCombatRuntimeBridge({
  ensureCombatRuntime: (...args) => ensureCombatRuntime(...args),
});

const {
  inventoryOverlayOpen,
  clearInventoryPreviewHoldTimer,
  closeInventoryOverlay,
  toggleInventoryOverlay,
  openInventoryPreview,
  closeInventoryPreview,
  scheduleInventoryPreviewHold,
} = createInventoryOverlayBridge({
  getState: () => state,
  closeInteraction: (...args) => closeInteraction(...args),
  releasePointerLook: (...args) => releasePointerLook(...args),
  render: (...args) => render(...args),
});

function buildLineDiffText(currentText = "", previousText = "") {
  const currentLines = String(currentText || "").split("\n");
  const previousLines = String(previousText || "").split("\n");
  const dp = Array.from({ length: previousLines.length + 1 }, () => Array(currentLines.length + 1).fill(0));
  for (let i = previousLines.length - 1; i >= 0; i -= 1) {
    for (let j = currentLines.length - 1; j >= 0; j -= 1) {
      dp[i][j] = previousLines[i] === currentLines[j]
        ? dp[i + 1][j + 1] + 1
        : Math.max(dp[i + 1][j], dp[i][j + 1]);
    }
  }
  const lines = ["--- previous", "+++ current"];
  let i = 0;
  let j = 0;
  while (i < previousLines.length && j < currentLines.length) {
    if (previousLines[i] === currentLines[j]) {
      lines.push(`  ${previousLines[i]}`);
      i += 1;
      j += 1;
    } else if (dp[i + 1][j] >= dp[i][j + 1]) {
      lines.push(`- ${previousLines[i]}`);
      i += 1;
    } else {
      lines.push(`+ ${currentLines[j]}`);
      j += 1;
    }
  }
  while (i < previousLines.length) {
    lines.push(`- ${previousLines[i]}`);
    i += 1;
  }
  while (j < currentLines.length) {
    lines.push(`+ ${currentLines[j]}`);
    j += 1;
  }
  return lines.join("\n");
}

function buildEventBundleStructuralCompare(currentBundle, previousBundle) {
  const currentEvents = Array.isArray(currentBundle?.events) ? currentBundle.events : [];
  const previousEvents = Array.isArray(previousBundle?.events) ? previousBundle.events : [];
  const currentMap = new Map(currentEvents.map((row) => [row.eventId, row]));
  const previousMap = new Map(previousEvents.map((row) => [row.eventId, row]));
  const currentIds = new Set(currentMap.keys());
  const previousIds = new Set(previousMap.keys());
  const added = [...currentIds].filter((eventId) => !previousIds.has(eventId)).sort();
  const removed = [...previousIds].filter((eventId) => !currentIds.has(eventId)).sort();
  const changed = [];
  const unchanged = [];
  [...currentIds].filter((eventId) => previousIds.has(eventId)).sort().forEach((eventId) => {
    const currentRow = currentMap.get(eventId);
    const previousRow = previousMap.get(eventId);
    const diffs = [];
    if (JSON.stringify(currentRow?.summaryDiff || {}) !== JSON.stringify(previousRow?.summaryDiff || {})) diffs.push("graph summary");
    if (JSON.stringify(currentRow?.validationSummary || {}) !== JSON.stringify(previousRow?.validationSummary || {})) diffs.push("validation");
    if (JSON.stringify(currentRow?.linkedPlacementIds || []) !== JSON.stringify(previousRow?.linkedPlacementIds || [])) diffs.push("placements");
    if (JSON.stringify(currentRow?.danglingDefaultTargets || []) !== JSON.stringify(previousRow?.danglingDefaultTargets || [])) diffs.push("default targets");
    if (JSON.stringify(currentRow?.danglingBranchTargets || []) !== JSON.stringify(previousRow?.danglingBranchTargets || [])) diffs.push("branch targets");
    if (JSON.stringify(currentRow?.danglingChoiceTargets || []) !== JSON.stringify(previousRow?.danglingChoiceTargets || [])) diffs.push("choice targets");
    if (JSON.stringify(currentRow?.npcHandoffIssues || []) !== JSON.stringify(previousRow?.npcHandoffIssues || [])) diffs.push("npc handoff");
    if (diffs.length) changed.push({ eventId, diffs, current: currentRow, previous: previousRow });
    else unchanged.push(eventId);
  });
  return {
    added,
    removed,
    changed,
    unchanged,
  };
}

function buildEventArchiveRollbackPlan(currentTarget, restoreTarget) {
  if (!currentTarget || !restoreTarget) return null;
  const fieldKeys = ["name", "type", "interaction", "entryStepId", "effects"];
  const fieldChanges = fieldKeys
    .filter((key) => JSON.stringify(currentTarget?.[key] ?? null) !== JSON.stringify(restoreTarget?.[key] ?? null))
    .map((key) => ({
      kind: "field",
      key,
      current: currentTarget?.[key] ?? null,
      restore: restoreTarget?.[key] ?? null,
    }));
  const currentSteps = Array.isArray(currentTarget?.steps) ? currentTarget.steps : [];
  const restoreSteps = Array.isArray(restoreTarget?.steps) ? restoreTarget.steps : [];
  const currentStepMap = new Map(currentSteps.map((step, index) => [step?.id || `step_${index}`, step]));
  const restoreStepMap = new Map(restoreSteps.map((step, index) => [step?.id || `step_${index}`, step]));
  const currentStepIds = new Set(currentStepMap.keys());
  const restoreStepIds = new Set(restoreStepMap.keys());
  const addedSteps = [...restoreStepIds].filter((id) => !currentStepIds.has(id)).sort().map((id) => ({ kind: "step_add", id }));
  const removedSteps = [...currentStepIds].filter((id) => !restoreStepIds.has(id)).sort().map((id) => ({ kind: "step_remove", id }));
  const updatedSteps = [...restoreStepIds]
    .filter((id) => currentStepIds.has(id))
    .sort()
    .map((id) => {
      const currentStep = currentStepMap.get(id) || {};
      const restoreStep = restoreStepMap.get(id) || {};
      const changedFields = ["title", "text", "nextStepId"].filter((key) => JSON.stringify(currentStep?.[key] ?? null) !== JSON.stringify(restoreStep?.[key] ?? null));
      return changedFields.length ? { kind: "step_update", id, changedFields } : null;
    })
    .filter(Boolean);
  const lines = [
    ...fieldChanges.map((entry) => ({ severity: "info", text: `field ${entry.key}: ${summarizeEventBundleDiffValue(entry.current, 40)} -> ${summarizeEventBundleDiffValue(entry.restore, 40)}` })),
    ...addedSteps.map((entry) => ({ severity: "info", text: `step add ${entry.id}` })),
    ...removedSteps.map((entry) => ({ severity: "warning", text: `step remove ${entry.id}` })),
    ...updatedSteps.map((entry) => ({ severity: "info", text: `step update ${entry.id}: ${entry.changedFields.join(", ")}` })),
  ];
  return { fieldChanges, addedSteps, removedSteps, updatedSteps, lines };
}

function buildDiffBadgeSpec(text, tone = "info") {
  if (!text) return null;
  return { text: String(text), tone };
}

function buildDiffCountScaleLabel(count = 0) {
  const normalized = Number(count || 0);
  if (normalized >= 10) return `peak ${normalized}`;
  if (normalized >= 6) return `high ${normalized}`;
  if (normalized >= 3) return `medium ${normalized}`;
  if (normalized >= 1) return `low ${normalized}`;
  return "none 0";
}

function renderDiffBadgeHtml(spec) {
  if (!spec?.text) return "";
  return ` <span class="preset-tag">${escapeHtml(spec.text)}</span>`;
}

function interactRuntime() {
  interact();
}

function activateFloor(floor, target) {
  const map = state.floorMaps[floor];
  if (!map) return;
  state.visitedByFloor[state.player.floor] = state.visited;
  state.map = map;
  state.player.floor = floor;
  state.player.x = target?.x ?? map.start.x;
  state.player.y = target?.y ?? map.start.y;
  state.player.facing = target?.facing ?? map.start.facing;
  state.visited = state.visitedByFloor[floor] || new Set();
  state.visited.add(logicalCellKey(state.player));
  state.visitedByFloor[floor] = state.visited;
  addLog(`${map.name}에 도착했다.`);
}

function wallKey(x, y, dir) {
  return `${x},${y},${dir}`;
}

function opposite(dir) {
  return DIRS[(DIRS.indexOf(dir) + 2) % 4];
}

function oppositeDoor(map, x, y, dir) {
  const v = VEC[dir];
  const key = wallKey(x + v.x, y + v.y, opposite(dir));
  return map.doors[key];
}

function addLog(text) {
  state.log.push(text);
  if (state.log.length > 80) state.log.shift();
}

async function copyTextToClipboard(text) {
  const value = String(text || "");
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(value);
    return true;
  }
  const textarea = document.createElement("textarea");
  textarea.value = value;
  textarea.setAttribute("readonly", "readonly");
  textarea.style.position = "fixed";
  textarea.style.left = "-9999px";
  document.body.appendChild(textarea);
  textarea.select();
  const copied = document.execCommand("copy");
  document.body.removeChild(textarea);
  if (!copied) throw new Error("clipboard API unavailable");
  return true;
}

async function readTextFromClipboard() {
  if (navigator.clipboard?.readText) {
    const value = await navigator.clipboard.readText();
    return String(value || "");
  }
  throw new Error("clipboard read API unavailable");
}

function downloadTextFile(filename, text, mimeType = "application/json") {
  const blob = new Blob([String(text || "")], { type: `${mimeType};charset=utf-8` });
  const href = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = href;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  setTimeout(() => URL.revokeObjectURL(href), 0);
}

function isEditableTarget(target) {
  if (!(target instanceof HTMLElement)) return false;
  return target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT", "BUTTON"].includes(target.tagName);
}

function releasePointerLook() {
  viewController?.releasePointerLook();
}

function toggleDragLook(force) {
  const next = typeof force === "boolean" ? force : !dragLookEnabled;
  if (dragLookEnabled === next) return;
  dragLookEnabled = next;
  viewController?.setDragLookEnabled(next);
  addLog(next ? "마우스 FPS 시점을 활성화했다." : "마우스 FPS 시점을 비활성화했다.");
  render();
}

function hasSavedEditorProject() {
  return Boolean(localStorage.getItem(EDITOR_PROJECT_STORAGE_KEY));
}

function roomCells(map, roomId) {
  return map.cells.filter((cell) => cell.roomId === roomId);
}

function roomPlacements(map, roomId) {
  return map.placements.filter((placement) => {
    if (!placement.position) return false;
    return getCell(map, placement.position.x, placement.position.y)?.roomId === roomId;
  });
}

function roomTagSet(cells) {
  return new Set(cells.flatMap((cell) => cell.tags || []));
}

function validateMap(map) {
  return ensureMapCompiler().validateMap(map);
}

function hasValidationErrors(report) {
  return ensureMapCompiler().hasValidationErrors(report);
}

function roomRectFromPoints(a, b) {
  const minX = Math.min(a.x, b.x);
  const minY = Math.min(a.y, b.y);
  const maxX = Math.max(a.x, b.x);
  const maxY = Math.max(a.y, b.y);
  return {
    x: minX,
    y: minY,
    width: maxX - minX + 1,
    height: maxY - minY + 1,
  };
}

function cellCoordKey(x, y) {
  return `${x},${y}`;
}

function pointFromCellCoordKey(key) {
  const [x, y] = String(key).split(",").map(Number);
  return { x, y };
}

function selectionBoundsFromPoints(points) {
  return selectionBoundsFromPointsModule(points);
}

function replaceRoomBounds(map, roomId, roomType, rect) {
  return replaceRoomBoundsModule(map, roomId, roomType, rect);
}

function removeCellFromRooms(map, x, y) {
  return removeCellFromRoomsModule(map, x, y);
}

function syncRoomRegistryFromCells(map, roomId, roomType) {
  return syncRoomRegistryFromCellsModule(map, roomId, roomType);
}

function isValidHexColor(value) {
  return typeof value === "string" && /^#([0-9a-fA-F]{6})$/.test(value.trim());
}

function normalizeRequiredPlacementContract(map) {
  return ensureMapCompiler().normalizeRequiredPlacementContract(map);
}

function requiredNpcPlacementIds(map) {
  return ensureMapCompiler().requiredNpcPlacementIds(map);
}

function requiredEventPlacementIds(map) {
  return ensureMapCompiler().requiredEventPlacementIds(map);
}

function isRequiredNpcPlacement(map, placementId) {
  return ensureMapCompiler().isRequiredNpcPlacement(map, placementId);
}

function isRequiredEventPlacement(map, placementId) {
  return ensureMapCompiler().isRequiredEventPlacement(map, placementId);
}

function toggleRequiredPlacementContract(map, placementId, category) {
  return ensureMapCompiler().toggleRequiredPlacementContract(map, placementId, category);
}

function placementKindForTool(tool) {
  return placementKindForToolModule(tool);
}

function validationIssueMarkup(report) {
  const visible = report.issues.length ? report.issues : [{ severity: "info", message: "검증 통과", code: "ok" }];
  return visible.map((issue) => `<div class="validation-line is-${issue.severity}"><strong>${issue.severity}</strong> ${escapeHtml(issue.message)}</div>`).join("");
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, (char) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    "\"": "&quot;",
    "'": "&#39;",
  }[char]));
}

function normalizePlacement(map, placement) {
  return ensureMapCompiler().normalizePlacement(map, placement);
}

function serializePlacement(placement) {
  return ensureMapCompiler().serializePlacement(placement);
}

function compileMapForRuntime(map, report = validateMap(map)) {
  return ensureMapCompiler().compileMapForRuntime(map, report);
}

function cloneEditorMap(map) {
  return ensureMapCompiler().cloneEditorMap(map);
}

function buildCompiledMapForRuntime(map, report = validateMap(map)) {
  return ensureMapCompiler().buildCompiledMapForRuntime(map, report);
}

function compileProjectForRuntime(floorMaps) {
  return ensureMapCompiler().compileProjectForRuntime(floorMaps);
}

function sortedUniqueStrings(values) {
  return [...new Set((values || []).filter((value) => typeof value === "string" && value.trim()))].sort();
}

function buildContentValidationReport() {
  return ensureMapCompiler().buildContentValidationReport();
}

function buildAssetValidationReport(floorMaps) {
  return ensureMapCompiler().buildAssetValidationReport(floorMaps);
}

function buildProjectProgressionValidationReport(floorMaps) {
  return ensureMapCompiler().buildProjectProgressionValidationReport(floorMaps);
}

function buildProjectKeyLockValidationReport(floorMaps) {
  return ensureMapCompiler().buildProjectKeyLockValidationReport(floorMaps);
}

function buildRequiredPlacementContractReport(floorMaps) {
  return ensureMapCompiler().buildRequiredPlacementContractReport(floorMaps);
}

function buildRequiredContentReachabilityReport(floorMaps) {
  return ensureMapCompiler().buildRequiredContentReachabilityReport(floorMaps);
}

function buildProjectValidationReport(floorMaps) {
  return ensureMapCompiler().buildProjectValidationReport(floorMaps);
}

function mergeProjectCompileFailures(report, failures) {
  return ensureMapCompiler().mergeProjectCompileFailures(report, failures);
}

function buildContentReferenceSummary(floorMaps, compiledProject) {
  return ensureMapCompiler().buildContentReferenceSummary(floorMaps, compiledProject);
}

function buildContentBuildManifest(floorMaps) {
  return ensureMapCompiler().buildContentBuildManifest(floorMaps);
}

var runtimeSessionManager = null;
var saveSlotManager = null;
var productShellManager = null;
var mapCompiler = null;
var eventRuntime = null;
var npcRuntime = null;
var combatRuntime = null;
var fieldMonsterRuntime = null;

function setState(nextState) {
  state = nextState;
  runtimeSessionManager = null;
  saveSlotManager = null;
  productShellManager = null;
  mapCompiler = null;
  eventRuntime = null;
  npcRuntime = null;
  combatRuntime = null;
  fieldMonsterRuntime = null;
}

function ensureRuntimeSessionManager() {
  if (runtimeSessionManager) return runtimeSessionManager;
  runtimeSessionManager = createRuntimeSessionManager({
    state,
    normalizeMapMetadata,
    computeWalls,
    normalizePartyModel,
    normalizeFieldMonsterStateTable: (value) => ensureFieldMonsterRuntime().normalizeFieldMonsterStateTable(value),
    compileProjectForRuntime,
    releasePointerLook,
    logicalCellKey,
  });
  return runtimeSessionManager;
}

function ensureFieldMonsterRuntime() {
  if (fieldMonsterRuntime) return fieldMonsterRuntime;
  fieldMonsterRuntime = createFieldMonsterRuntime({
    getState: () => state,
    encounters,
    monsters,
    vec: VEC,
    movementBlockingPlacementKinds: MOVEMENT_BLOCKING_PLACEMENT_KINDS,
  });
  return fieldMonsterRuntime;
}

function advanceDungeonFieldMonsters() {
  const placement = ensureFieldMonsterRuntime().tickFieldMonsters();
  if (placement && !placement.done) startCombat(placement);
  return placement;
}

function ensureSaveSlotManager() {
  if (saveSlotManager) return saveSlotManager;
  saveSlotManager = createSaveSlotManager({
    getState: () => state,
    setState,
    addLog,
    render,
    storage: localStorage,
    storagePrefix: SAVE_SLOT_STORAGE_PREFIX,
    saveSlotIds: SAVE_SLOT_IDS,
    currentSaveSlotId: () => currentSaveSlotId(),
    currentContentVersion: CURRENT_CONTENT_VERSION,
    saveSlotSchemaVersion: SAVE_SLOT_SCHEMA_VERSION,
    windowObject: window,
    questEndingComplete,
    buildRecentStatusLabel: (data) => buildRecentStatusLabel(data),
    validateEventDefinitionsTable,
    validateClassDefinitionsTable: (...args) => validateClassDefinitionsTable(...args),
    validateNpcDefinitionsTable,
    eventDefinitions,
    classes,
    npcs,
    normalizeQuestState,
    normalizeFieldMonsterStateTable: (value) => ensureFieldMonsterRuntime().normalizeFieldMonsterStateTable(value),
    cloneEditorMap,
    ensureBoundaryClean,
    initialState,
    normalizeMapMetadata,
    computeWalls,
    normalizePartyModel,
    normalizeInventoryList,
    endTestPlaySession: (nextMode = "editor") => ensureRuntimeSessionManager().endTestPlaySession(nextMode),
    ensureTownFloorMaps,
    activateTownState,
  });
  return saveSlotManager;
}

function ensureProductShellManager() {
  if (productShellManager) return productShellManager;
  productShellManager = createProductShell({
    getState: () => state,
    setState,
    render,
    addLog,
    initialState,
    normalizePartyModel,
    makeHero,
    currentSaveSlotId: () => currentSaveSlotId(),
    setMode,
    activateTownState,
    openSavedEditorWorkspace,
    createFreshEditorWorkspace,
    loadGame,
    renameSaveSlot,
    deleteSaveSlot,
    classes,
    protagonistBackgrounds: PROTAGONIST_BACKGROUNDS,
    starterLoadouts: STARTER_LOADOUTS,
    documentObject: document,
  });
  return productShellManager;
}

function ensureMapCompiler() {
  if (mapCompiler) return mapCompiler;
  mapCompiler = createMapCompiler({
    classes,
    monsters,
    items,
    encounters,
    eventDefinitions,
    skills,
    npcs,
    vendors,
    lootTables,
    materialManifest,
    rarityDefinitions,
    affixDefinitions,
    affixPoolDefinitions,
    contentBuildDataFiles: CONTENT_BUILD_DATA_FILES,
    roomTypes: ROOM_TYPES,
    cellTags: CELL_TAGS,
    battleBackgrounds: BATTLE_BACKGROUNDS,
    themeBattleBackgrounds: THEME_BATTLE_BACKGROUNDS,
    placementKinds: PLACEMENT_KINDS,
    legacyPlacementKinds: LEGACY_PLACEMENT_KINDS,
    eventObjectPlacementKinds: EVENT_OBJECT_PLACEMENT_KINDS,
    movementBlockingPlacementKinds: MOVEMENT_BLOCKING_PLACEMENT_KINDS,
    eventEffectTypes: EVENT_EFFECT_TYPES,
    eventTriggerTypes: EVENT_TRIGGER_TYPES,
    resourceKeys: RESOURCE_KEYS,
    companionStateKeys: COMPANION_STATE_KEYS,
    partyStatKeys: PARTY_STAT_KEYS,
    validMaterialLightingHints: VALID_MATERIAL_LIGHTING_HINTS,
    validMaterialLods: VALID_MATERIAL_LODS,
    generatedNormalMapKeys: GENERATED_NORMAL_MAP_KEYS,
    legacyMonsterToEncounter: LEGACY_MONSTER_TO_ENCOUNTER,
    legacyEventToTrigger: LEGACY_EVENT_TO_TRIGGER,
    editorOnlyBoundaryKeys: EDITOR_ONLY_BOUNDARY_KEYS,
    validateEventDefinitionsTable,
    validateClassDefinitionsTable: (...args) => validateClassDefinitionsTable(...args),
    validateItemDefinitionsTable,
    validateVendorDefinitionsTable,
    validateLootTableDefinitionsTable,
    validateRarityDefinitionsTable,
    validateAffixDefinitionsTable,
    validateAffixPoolDefinitionsTable,
    validateNpcDefinitionsTable,
    validateQuestSeedReferenceIntegrity,
  });
  return mapCompiler;
}

function ensureCombatRuntime() {
  if (combatRuntime) return combatRuntime;
  combatRuntime = createCombatRuntime({
    getState: () => state,
    addLog,
    render,
    closeInteraction,
    closeInventoryOverlay,
    focusCameraOnPlacement,
    releasePointerLook,
    capturePreEncounterSnapshot: () => ensureRuntimeSessionManager().capturePreEncounterSnapshot(),
    normalizePartyModel,
    normalizeFieldMonsterStateTable: (value) => ensureFieldMonsterRuntime().normalizeFieldMonsterStateTable(value),
    normalizeInventoryList,
    inventoryEntryItemId,
    useInventoryEntryOnHero,
    removeInventoryEntryAt,
    pushInventoryItemId,
    pushInventoryEntry,
    lootItems,
    combatLootTable,
    inventoryEntryLabel,
    skillName,
    skills,
    monsters,
    encounters,
    items,
    logicalCellKey,
    legacyMonsterToEncounter: LEGACY_MONSTER_TO_ENCOUNTER,
    completePartyDefeatEnding,
    updateBoardQuestCompletion,
  });
  return combatRuntime;
}

function ensureNpcRuntime() {
  if (npcRuntime) return npcRuntime;
  npcRuntime = createNpcRuntime({
    getState: () => state,
    addLog,
    render,
    closeInteraction,
    releasePointerLook,
    focusCameraOnPlacement,
    ensureNpcRuntimeState,
    activeNpcQuestHook,
    activeNpcQuestSeed,
    vendorOffer,
    nextClassMilestone,
    grantVendorInventoryEntry,
    vendorInventoryEntryLabel,
    createCompanionRecord,
    normalizePartyModel,
    buildCompanionHero,
    activateQuestSeed,
    availableQuestDefinitions,
    activateBoardQuest,
    boardQuestAllowsDungeonEntry,
    boardQuestEntryTarget,
    skillCatalogSkillIds,
    allInventoryAndEquipmentEntries,
    inventoryEntryItemId,
    inventoryEntryIsIdentified,
    inventoryEntryIsCursed,
    identifyInventoryEntry,
    purifyInventoryEntry,
    startCombat,
    advanceWorldTurn: () => {
      advanceWorldTurn();
      advanceDungeonFieldMonsters();
    },
    activateFloor,
    setMode,
    classes,
    items,
    encounters,
    npcs,
    questDefinitions,
  });
  return npcRuntime;
}

function ensureEventRuntime() {
  if (eventRuntime) return eventRuntime;
  eventRuntime = createEventRuntime({
    getState: () => state,
    addLog,
    render,
    closeInteraction,
    releasePointerLook,
    resolvePlacementEvent: (placement) => ensureMapCompiler().resolvePlacementEvent(placement),
    eventStepMap,
    stepChoiceMatches,
    stepBranchMatches,
    currentEditorEventTestSession,
    flushPendingNpcHandoff,
    queueNpcHandoff,
    livingParty,
    setQuestSeedState,
    pushInventoryItemId,
    items,
    afterAdvanceWorldTurn: () => advanceDungeonFieldMonsters(),
  });
  return eventRuntime;
}

function buildRuntimeSessionFloorMaps(compiledMaps) {
  return Object.fromEntries(Object.entries(compiledMaps).map(([floor, compiled]) => {
    const runtimeMap = JSON.parse(JSON.stringify(compiled.map));
    normalizeMapMetadataModule(runtimeMap, {
      normalizePlacement,
      normalizeTextureId,
      FLOOR_TEXTURE_IDS,
      CEILING_TEXTURE_IDS,
      WALL_TEXTURE_IDS,
      DEFAULT_FLOOR_TEXTURE_ID,
      DEFAULT_CEILING_TEXTURE_ID,
      DEFAULT_WALL_TEXTURE_ID,
    });
    computeWalls(runtimeMap);
    return [floor, runtimeMap];
  }));
}

const {
  normalizeInventoryEntry,
  normalizeInventoryList,
  inventoryEntryItemId,
  inventoryEntryBaseName,
  inventoryEntryIsIdentified,
  inventoryEntryIsCursed,
  inventoryEntryKindLabel,
  identifyInventoryEntry,
  purifyInventoryEntry,
  allInventoryAndEquipmentEntries,
  inventoryEntryLabel,
  inventoryEntryDetailParts,
  inventoryEntryDetailText,
  compareEquipmentCandidate,
  compareDeltaText,
  compareCandidateSummary,
  inventorySummaryText,
  inventoryFilterOptions,
  inventoryEntryMatchesFilter,
  inventorySortOptions,
  normalizeInventorySearchQuery,
  inventoryEntryMatchesSearch,
  sortedFilteredInventoryEntries,
  pushInventoryEntry,
  pushInventoryItemId,
  inventoryManualReorderEnabled,
  reorderInventoryEntries,
  useInventoryEntryOnHero,
  hasInventoryItem,
  consumeInventoryItem,
  vendorInventoryEntryItemId,
  vendorInventoryEntryLabel,
  buildGeneratedRewardInstance,
  grantVendorInventoryEntry,
  inventoryEntryEquipmentSlot,
  inventoryEntryStatPayload,
  equipmentEntryLabel,
  removeInventoryEntryAt,
  heroEquipmentEntries,
  availableEquipmentEntries,
  equipInventoryEntryToHero,
  matchesRule,
  pickWeighted,
  pushLootReward,
  lootItems,
  combatLootTable,
  vendorOffer,
} = createInventoryRuntimeBridge({
  getState: () => state,
  items,
  vendors,
  lootTables,
  rarityDefinitions,
  buildSampleItemPreview,
  makeGeneratedItemInstance,
  baseItemShouldUseInstance,
  createBaseItemInstance,
  addLog,
});

const {
  recordEventExportHistory,
  recordEventBundlePatchHistory,
  loadEventBundlePatchArchive,
  saveEventBundlePatchArchive,
  recordEventBundlePatchArchive,
  normalizeEventBundlePatchArchiveQuery,
  eventBundlePatchArchiveLine,
  eventBundlePatchEntryMatchesQuery,
  recordNpcPresetPatchHistory,
  captureNpcPresetUndoSnapshot,
  restoreNpcDefinitionSnapshot,
  applyNpcPresetUndoSnapshot,
  clearNpcPresetRedoEntry,
  pushNpcPresetRedoEntry,
  loadNpcPresetRedoArchive,
  saveNpcPresetRedoArchive,
  recordNpcPresetRedoArchive,
  npcPresetRedoArchiveLine,
  normalizeNpcPresetRedoArchiveQuery,
  npcPresetRedoArchiveMatchesQuery,
  npcPresetRedoSnapshotSummary,
  buildNpcPresetRedoArchiveBatchCompare,
  deleteNpcPresetRedoArchiveEntry,
  loadNpcPresetPatchArchive,
  saveNpcPresetPatchArchive,
  recordNpcPresetPatchArchive,
  npcPresetPatchArchiveLine,
  normalizeNpcPresetPatchArchiveQuery,
  npcPresetPatchArchiveMatchesQuery,
  deleteNpcPresetPatchArchiveEntry,
  deleteNpcPresetPatchArchiveEntries,
  loadEventExportArchive,
  saveEventExportArchive,
  recordEventExportArchive,
  deleteEventExportArchiveEntry,
  eventExportArchiveLine,
  buildEventExportSummaryDiff,
} = createEditorArchiveBridge({
  getState: () => state,
  localStorageObject: localStorage,
  npcs,
  eventExportArchiveStorageKey: EVENT_EXPORT_ARCHIVE_STORAGE_KEY,
  eventBundlePatchArchiveStorageKey: EVENT_BUNDLE_PATCH_ARCHIVE_STORAGE_KEY,
  npcPresetPatchArchiveStorageKey: NPC_PRESET_PATCH_ARCHIVE_STORAGE_KEY,
  npcPresetRedoArchiveStorageKey: NPC_PRESET_REDO_ARCHIVE_STORAGE_KEY,
});

const {
  buildEventArchiveRestoreBadgeLookup,
  eventBundleCompareRowOptions,
  buildEventBundleComparePatch,
  parsePatchPathTokens,
  setValueAtPatchPath,
  getValueAtPatchPath,
  applyEventBundleComparePatchPreview,
  applyCompactEventRowToDefinition,
  applyResolvedEventBundleRow,
  applyPartialCompactEventRowToDefinition,
  summarizeEventBundleDiffValue,
  buildEventBundleVisualDiffRows,
  eventExportHistoryLine,
  normalizeEventExportArchiveQuery,
  eventExportEntryMatchesQuery,
} = createEditorEventCompareBridge({
  getState: () => state,
  buildDiffBadgeSpec,
  buildEventArchiveRollbackPlan,
  eventDefinitions,
  updateEventDefinition: (eventId, updater) => {
    if (!eventId || !eventDefinitions[eventId]) return;
    updater(eventDefinitions[eventId]);
  },
  eventExportArchiveLine,
});

const buildEditorEventSnapshot = createEditorEventSnapshotBridge({
  getState: () => state,
  eventDefinitions,
  buildEventValidationSnapshot,
  buildEventGraphPreview,
  buildEventGraphCompactExport,
  buildEventGraphSummaryDiff,
  buildProjectEventGraphReviewBundle,
  loadEventExportArchive,
  eventExportEntryMatchesQuery,
  buildEventExportArchiveBatchCompare,
  buildEventExportArchiveBatchCompareExport,
  buildEventExportArchiveBatchShareExport,
  buildEventExportArchiveBatchShareLink,
  buildEventExportSummaryDiff,
  buildEventArchiveRollbackPlan,
  buildLineDiffText,
  buildEventArchiveRestoreBadgeLookup,
  buildEventBundleStructuralCompare,
  eventBundleCompareRowOptions,
  buildEventBundleComparePatch,
  applyEventBundleComparePatchPreview,
  loadEventBundlePatchArchive,
  eventBundlePatchEntryMatchesQuery,
  buildEventBundleVisualDiffRows,
  getValueAtPatchPath,
  currentEditorEventTestSession,
});

const {
  validationSummaryText,
  firstValidationIssue,
  validationIssueRepairHint,
  validationBlockerMarkup,
  buildProjectDashboardSnapshot,
  buildDensityOverlaySnapshot,
  classifyDensityBand,
  buildDensityHistogramSnapshot,
  densityHistogramMarkup,
  toolLabelForRecommendation,
  densityModeForRecommendationTool,
  buildRoomPlacementSummary,
  buildPlacementRecommendations,
} = createEditorValidationBridge({
  escapeHtml,
  buildProjectValidationReport,
  buildAssetValidationReport,
  buildProjectProgressionValidationReport,
  buildProjectKeyLockValidationReport,
  buildRequiredPlacementContractReport,
  buildRequiredContentReachabilityReport,
  compileProjectForRuntime,
  hasValidationErrors,
  validateMap,
  cellCoordKey,
  getCell,
  placementKindForTool: placementKindForToolModule,
  densityOverlayModes: DENSITY_OVERLAY_MODES,
  roomRecommendationRules: ROOM_RECOMMENDATION_RULES,
  cellTagRecommendationRules: CELL_TAG_RECOMMENDATION_RULES,
  placementToolButtons: PLACEMENT_TOOL_BUTTONS,
});

const {
  activeEventEditorTool,
  activeEventDefinitionId,
  activeEventDefinition,
  updateEventDefinition,
  activeNpcDefinitionId,
  activeNpcDefinition,
  updateNpcDefinition,
  activeNpcQuestSeed,
  ensureQuestSeedState,
  activateQuestSeed,
  grantQuestSeedRewards,
  setQuestSeedState,
  syncQuestSeedFailureStates,
  questSeedJson,
  questSeedRewardItemsText,
  questRewardFlagValueType,
  questRewardFlagValueText,
  uniqueNpcQuestSeedId,
  createQuestSeedTemplate,
  duplicateQuestSeedTemplate,
  activeNpcQuestSeedDefinition,
  createNpcServiceTemplate,
  createNpcServiceGroupTemplates,
  activeNpcServiceDefinition,
  createNpcDialogueStepTemplate,
  createNpcDialogueChoiceTemplate,
  activeNpcDialogueStepDefinition,
  npcPlacementsAtCursor,
  activeNpcPlacement,
  activeNpcPlacementDefinition,
  allowedInteractionsForPlacementKind,
  defaultInteractionTypeForPlacementKind,
  focusedNpcClassIndices,
  focusedNpcClassNames,
  activeNpcQuestHook,
  createNpcQuestHookTemplate,
  compatibleEventDefinitions,
  uniqueEventPresetId,
  updateEventReferences,
  renameEventPreset,
  createEventPresetFromDefinition,
  uniqueEventStepId,
  createEventStepTemplate,
  createEventChoiceTemplate,
  createEventGraphTemplate,
  activeEventStepDefinition,
  eventPlacementsAtCursor,
  activePlacementOverride,
} = createEditorNpcEventStateBridge({
  getState: () => state,
  normalizeQuestState,
  pushInventoryItemId,
  addLog,
  items,
  vendors,
  classes,
  npcs,
  eventDefinitions,
  defaultEventSelection,
  editorEventSelectionDefaults,
  eventEditorToPlacementKind: EVENT_EDITOR_TO_PLACEMENT_KIND,
  eventObjectPlacementKinds: EVENT_OBJECT_PLACEMENT_KINDS,
});

const {
  normalizeNpcCustomPresetDefinition,
  loadNpcCustomPresets,
  saveNpcCustomPresets,
  uniqueNpcCustomPresetId,
  buildNpcCustomPresetFromDefinition,
  npcCustomPresetSummary,
  npcPresetServiceIdentity,
  npcPresetSeedIdentity,
  npcDialogueStepIdentity,
  npcDialogueChoiceIdentity,
  npcDialogueBranchIdentity,
  diffObjectFieldKeys,
  buildNpcCustomPresetDiff,
  defaultNpcPresetSelectionIndexes,
  defaultNpcPresetDialogueSelectionMap,
  defaultNpcPresetDialogueChoiceSelectionMap,
  defaultNpcPresetDialogueBranchSelectionMap,
  defaultNpcPresetServiceFieldSelectionMap,
  defaultNpcPresetSeedFieldSelectionMap,
  selectedNpcPresetServiceIndexes,
  selectedNpcPresetSeedIndexes,
  selectedNpcPresetDialogueStepIndexes,
  selectedNpcPresetDialogueChoiceIndexes,
  selectedNpcPresetDialogueBranchIndexes,
  selectedNpcPresetServiceFieldNames,
  selectedNpcPresetSeedFieldNames,
  buildNpcPresetResolvedDialogueStep,
  buildNpcPresetResolvedServicePreview,
  buildNpcPresetResolvedSeedPreview,
  npcPresetThreeWayPreviewMarkup,
  npcPresetSideBySideDiffMarkup,
  buildNpcPresetMergePatch,
  applyNpcPresetMergePatchToDefinition,
  buildNpcPresetApplyComparePreview,
  validateNpcPresetMergePatch,
} = createEditorNpcPresetBridge({
  getState: () => state,
  localStorageObject: localStorage,
  storageKey: NPC_CUSTOM_PRESET_STORAGE_KEY,
  npcs,
  escapeHtml,
  buildLineDiffText,
});

const buildEditorNpcSnapshot = createEditorNpcSnapshotBridge({
  getState: () => state,
  npcs,
  items,
  eventDefinitions,
  vendors,
  classes,
  encounters,
  loadNpcCustomPresets,
  buildNpcCustomPresetDiff,
  selectedNpcPresetServiceIndexes,
  selectedNpcPresetSeedIndexes,
  buildNpcPresetMergePatch,
  validateNpcPresetMergePatch,
  loadNpcPresetRedoArchive,
  npcPresetRedoArchiveMatchesQuery,
  buildNpcPresetRedoArchiveBatchCompare,
  loadNpcPresetPatchArchive,
  npcPresetPatchArchiveMatchesQuery,
  buildLineDiffText,
  buildNpcPresetApplyComparePreview,
  renderEditorQuestSeedPanel,
  renderEditorQuestSeedBody,
  renderEditorNpcServicePanel,
  renderEditorNpcCustomPresetSection,
  renderEditorNpcServiceEditorSection,
  questSeedJson,
  questSeedRewardItemsText,
  questRewardFlagValueType,
  questRewardFlagValueText,
  npcPresetRedoArchiveLine,
  npcPresetPatchArchiveLine,
  npcCustomPresetSummary,
  renderDiffBadgeHtml,
  buildDiffBadgeSpec,
  buildDiffCountScaleLabel,
  validationSummaryText,
  selectedNpcPresetServiceFieldNames,
  selectedNpcPresetDialogueStepIndexes,
  selectedNpcPresetDialogueChoiceIndexes,
  selectedNpcPresetDialogueBranchIndexes,
  selectedNpcPresetSeedFieldNames,
  npcPresetSideBySideDiffMarkup,
  npcPresetThreeWayPreviewMarkup,
  buildNpcPresetResolvedDialogueStep,
  buildNpcPresetResolvedServicePreview,
  buildNpcPresetResolvedSeedPreview,
  npcHookJson,
  npcServicePreviewText,
  npcServicePreviewList,
  escapeHtml,
});

const buildEditorNpcSupportPanels = createEditorNpcSupportPanelBridge({
  npcs,
  renderEditorNpcProgressionHooksPanel,
  renderEditorNpcPlacementPanel,
  renderEditorPresetStudioPanel,
  npcHookJson,
  npcServicePreviewList,
  escapeHtml,
});

const buildEditorPlacementClassPanels = createEditorPlacementClassPanelBridge({
  compatibleEventDefinitions,
  renderEditorPlacementOverridePanel,
  renderEditorClassProgressionPanel,
  validationSummaryText,
  escapeHtml,
  classMilestonesJson,
  classes,
});

const buildEditorEventPanelBody = createEditorEventPanelBridge();
const buildEditorWorkspacePanels = createEditorWorkspacePanelBridge({
  renderEditorSurfaceBrushPanel,
  renderEditorRangeBrushPanel,
  renderEditorDensityHistogramPanel,
  renderEditorPlacementRecommendationPanel,
  renderEditorSelectedBlockPanel,
  renderEditorValidationPanel,
  renderEditorContentBuildDashboardPanel,
  renderEditorPresetLibraryPanel,
  createPreviewGrid,
  previewGridMarkup,
  validationSummaryText,
  validationBlockerMarkup,
  densityHistogramMarkup,
  textureSwatchColor,
  escapeHtml,
});

const {
  makeHero,
  normalizeHeroState,
  createCompanionRecord,
  normalizePartyModel,
  buildCompanionHero,
} = createRuntimePartyBridge({
  getState: () => state,
  classes,
  npcs,
  partyModelLimits: PARTY_MODEL_LIMITS,
  normalizeInventoryEntry,
  vendorOffer: (...args) => vendorOffer(...args),
  focusedNpcClassIndices,
  focusedNpcClassNames,
  activeNpcQuestHook,
  activeNpcQuestSeed,
  nextClassMilestone,
  addLog,
  setMode: (...args) => setMode(...args),
  grantVendorInventoryEntry: (...args) => grantVendorInventoryEntry(...args),
  vendorInventoryEntryLabel: (...args) => vendorInventoryEntryLabel(...args),
});

function resolvePlacementEvent(placement) {
  const eventId = placement?.interaction?.eventId || placement?.refId || "";
  const baseEvent = eventDefinitions[eventId];
  if (!baseEvent) return null;
  const resolved = JSON.parse(JSON.stringify(baseEvent));
  const overrides = placement?.eventOverrides || {};
  if (overrides.usage) resolved.usage = { ...(resolved.usage || {}), ...overrides.usage };
  if (overrides.detection) resolved.detection = { ...(resolved.detection || {}), ...overrides.detection };
  if (overrides.disarm) resolved.disarm = { ...(resolved.disarm || {}), ...overrides.disarm };
  return resolved;
}

function allowedInteractionsForPlacementKindForValidation(kind) {
  if (kind === "trap") return ["onEnter"];
  if (kind === "event_trigger") return ["interact", "onEnter", "onExit"];
  if (kind === "rest_site") return ["interact", "onRest"];
  if (kind === "camp") return ["interact", "onCamp"];
  return ["interact"];
}

function resolvePlacementEventForValidation(placement) {
  return resolvePlacementEvent(placement);
}

const {
  createEditorProjectDependencies,
  createMapMetadataDependencies,
  createEditorMapEditingDependencies,
} = createEditorDependencyBridge({
  ensureEditorState,
  getState: () => state,
  buildEditorStateConfig,
  eventDefinitions,
  classes,
  questDefinitions,
  monsters,
  skills,
  items,
  vendors,
  lootTables,
  rarityDefinitions,
  affixDefinitions,
  affixPoolDefinitions,
  npcs,
  validateEventDefinitionsTable,
  validateClassDefinitionsTable,
  validateQuestDefinitionsTable,
  validateMonsterDefinitionsTable,
  validateSkillDefinitionsTable,
  validateItemDefinitionsTable,
  validateVendorDefinitionsTable,
  validateLootTableDefinitionsTable,
  validateRarityDefinitionsTable,
  validateAffixDefinitionsTable,
  validateAffixPoolDefinitionsTable,
  validateNpcDefinitionsTable,
  validateQuestSeedReferenceIntegrity,
  validateMap,
  cloneEditorMap,
  buildCompiledMapForRuntime,
  ensureBoundaryClean,
  loadNpcCustomPresets,
  saveNpcCustomPresets,
  replaceClassDefinitions,
  replaceQuestDefinitions,
  replaceMonsterDefinitions,
  replaceSkillDefinitions,
  replaceItemDefinitions,
  replaceVendorDefinitions,
  replaceLootTableDefinitions,
  replaceRarityDefinitions,
  replaceAffixDefinitions,
  replaceAffixPoolDefinitions,
  replaceNpcDefinitions,
  normalizePlacement,
  computeWalls,
  refreshPresetCatalog,
  normalizeTextureId,
  editorEventSelectionDefaults,
  lootTableDefinitionIds: baseLootTableDefinitionIds,
  floorTextureIds: FLOOR_TEXTURE_IDS,
  ceilingTextureIds: CEILING_TEXTURE_IDS,
  wallTextureIds: WALL_TEXTURE_IDS,
  defaultFloorTextureId: DEFAULT_FLOOR_TEXTURE_ID,
  defaultCeilingTextureId: DEFAULT_CEILING_TEXTURE_ID,
  defaultWallTextureId: DEFAULT_WALL_TEXTURE_ID,
  eventEditorToPlacementKind: EVENT_EDITOR_TO_PLACEMENT_KIND,
  densityOverlayModes: DENSITY_OVERLAY_MODES,
  addLog,
  render,
  localStorageObject: localStorage,
  getCell,
  roomRectFromPoints,
  cellCoordKey,
  pointFromCellCoordKey,
  roomCells,
  roomPlacements,
  dirs: DIRS,
  vec: VEC,
  activeNpcDefinitionId,
  defaultEditorEncounterId: DEFAULT_EDITOR_ENCOUNTER_ID,
  defaultEditorEventId: DEFAULT_EDITOR_EVENT_ID,
  defaultEditorTrapEventId: DEFAULT_EDITOR_TRAP_EVENT_ID,
  defaultEditorShrineEventId: DEFAULT_EDITOR_SHRINE_EVENT_ID,
  defaultEditorRestEventId: DEFAULT_EDITOR_REST_EVENT_ID,
  defaultEditorCampEventId: DEFAULT_EDITOR_CAMP_EVENT_ID,
});

const editorMapEditingBridge = createEditorMapEditingBridge({
  getEditorMapEditingDependencies: createEditorMapEditingDependencies,
});

const {
  selectedTextureBrush,
  applySurfaceTexturesToCell,
  applyTextureRange,
  applyTextureSelection,
  uniqueSelectionPoints,
  selectionPointsFromRect,
  cellsInRoomRect,
  activeBrushRangeStart,
  isRangeBrushTool,
  isLassoBrushMode,
  activeBrushPreviewRect,
  committedBrushSelectionPoints,
  currentBrushSelectionKeys,
  activeBrushSelectionPoints,
  activeBrushSelectionRect,
  beginRangeBrushDrag,
  updateRangeBrushDrag,
  beginLassoBrushDrag,
  updateLassoBrushDrag,
  rememberBrushSelection,
  commitLassoBrushSelection,
  clearRangeBrushState,
  applyRangeBrushAtCurrentCursor,
  applyCommittedBrushSelection,
  transformCommittedBrushSelection,
  floodSelectionPoints,
  currentBrushFloodSelectionPoints,
  currentBrushMatchingSelectionPoints,
  rememberExplicitBrushSelection,
  applyRoomRange,
  applyRoomSelection,
  applyCellTagRange,
  applyCellTagSelection,
  applyBattleBgRange,
  applyBattleBgSelection,
  grownSelectionPoints,
  shrunkSelectionPoints,
  invertedSelectionPoints,
  createEditorPlacement,
  canAutoPlaceRecommendationTool,
  autoPlacementCandidateCells,
  applyRecommendationAutoPlacement,
} = editorMapEditingBridge;

const {
  activeClassDefinitionIndex,
  activeClassDefinition,
  updateClassDefinition,
  activeQuestDefinitionId,
  activeQuestDefinition,
  updateQuestDefinition,
  questDefinitionsJson,
  uniqueQuestDefinitionId,
  createQuestDefinitionTemplate,
  questGeneratorSelection,
  buildGeneratedQuestDefinition,
  mapKindLabel,
  activeMonsterDefinitionId,
  activeMonsterDefinition,
  updateMonsterDefinition,
  monsterDefinitionsJson,
  uniqueMonsterDefinitionId,
  createMonsterDefinitionTemplate,
  activeSkillDefinitionId,
  activeSkillDefinition,
  updateSkillDefinition,
  skillDefinitionsJson,
  uniqueSkillDefinitionId,
  createSkillDefinitionTemplate,
  activeItemDefinitionId,
  activeItemDefinition,
  updateItemDefinition,
  itemDefinitionsJson,
  uniqueItemDefinitionId,
  createItemDefinitionTemplate,
  activeVendorDefinitionId,
  activeVendorDefinition,
  updateVendorDefinition,
  vendorsJson,
  uniqueVendorDefinitionId,
  createVendorDefinitionTemplate,
  createVendorRotationTemplate,
  activeVendorRotationDefinition,
  vendorInventorySummary,
  lootTableDefinitionIds,
  activeLootTableId,
  activeLootTableDefinition,
  updateLootTableDefinition,
  lootTablesJson,
  uniqueLootTableId,
  createLootEntryTemplate,
  ensureVendorInventoryEntryObject,
  ensureLootEntryObject,
  setGeneratedEntryFields,
  itemDefinitionOptionListHtml,
  rarityDefinitionOptionListHtml,
  affixPoolOptionListHtml,
  createLootTierTemplate,
  createLootBonusTemplate,
  createLootTableDefinitionTemplate,
  createCombatRewardProfileTemplate,
  activeLootTierDefinition,
  activeLootBonusDefinition,
  activeCombatRewardProfile,
  rarityDefinitionsJson,
  affixDefinitionsJson,
  affixPoolDefinitionsJson,
  activeRarityDefinitionId,
  activeRarityDefinition,
  activeAffixDefinitionId,
  activeAffixDefinition,
  activeAffixPoolId,
  activeAffixPoolDefinition,
  updateRarityDefinition,
  updateAffixDefinition,
  updateAffixPoolDefinition,
  uniqueSchemaId,
  createRarityDefinitionTemplate,
  createAffixDefinitionTemplate,
  createAffixPoolDefinitionTemplate,
} = createEditorContentStateBridge({
  getState: () => state,
  classes,
  questDefinitions,
  monsters,
  skills,
  mapProfiles,
  items,
  vendors,
  lootTables,
  rarityDefinitions,
  affixDefinitions,
  affixPoolDefinitions,
  syncPartyClassDefinitions,
  vendorInventoryEntryLabel,
  escapeHtml,
});

const buildEditorContentPanels = createEditorContentPanelBridge({
  questDefinitions,
  monsters,
  skills,
  mapProfiles,
  items,
  vendors,
  lootTables,
  rarityDefinitions,
  affixDefinitions,
  affixPoolDefinitions,
  renderEditorItemBasePanel,
  renderEditorQuestDefinitionPanel,
  renderEditorMonsterDefinitionPanel,
  renderEditorSkillDefinitionPanel,
  renderEditorVendorInventoryPanel,
  renderEditorLootTablePanel,
  renderEditorAffixRarityPanel,
  renderEditorSampleItemPanel,
  escapeHtml,
  questDefinitionsJson,
  questGeneratorSelection,
  mapKindLabel,
  monsterDefinitionsJson,
  skillDefinitionsJson,
  itemDefinitionsJson,
  itemDefinitionOptionListHtml,
  rarityDefinitionOptionListHtml,
  affixPoolOptionListHtml,
  vendorsJson,
  vendorInventoryEntryItemId,
  vendorInventorySummary,
  lootTableDefinitionIds,
  lootTablesJson,
  rarityDefinitionsJson,
  affixDefinitionsJson,
  affixPoolDefinitionsJson,
  sampleItemPreviewJson,
  activeRarityDefinitionId,
  activeAffixPoolId,
});

const {
  buildEditorProject,
  applyEditorProject,
  saveEditorProject,
  normalizeMapMetadata,
  normalizeMapLight,
  loadEditorProject,
} = createEditorProjectBridge({
  buildEditorProjectModule,
  applyEditorProjectModule,
  saveEditorProjectModule,
  loadEditorProjectModule,
  normalizeMapMetadataModule,
  normalizeMapLightModule,
  createEditorProjectDependencies,
  createMapMetadataDependencies,
});

const {
  blocks,
  activeMovementBlockersAt,
  focusCameraOnPlacement,
  spendTorch,
  afterMove,
  interact,
  runInteractivePlacement,
  triggerTrap,
  runEventPlacement,
  rest,
} = createDungeonRuntimeBridge({
  getState: () => state,
  getViewController: () => viewController,
  dirs: DIRS,
  vec: VEC,
  interactivePlacementKinds: INTERACTIVE_PLACEMENT_KINDS,
  movementBlockingPlacementKinds: MOVEMENT_BLOCKING_PLACEMENT_KINDS,
  blocksMovement,
  collectPlacementsAt,
  getCell,
  logicalPlayerCell,
  logicalCellKey,
  resolveLookDirection,
  resolveDoorAtFront,
  resolveInteractionCandidate,
  resolveStairsOutcome,
  wallKey,
  oppositeDoor,
  pushInventoryItemId,
  resolvePlacementEvent,
  eventUsageState,
  canDetectTrap,
  canDisarmTrap,
  runNpcPlacement,
  runTypedEventEffects,
  startCombat,
  livingParty,
  hasInventoryItem,
  computeWalls,
  addLog,
  render,
  advanceWorldTurn,
  tickFieldMonsters: () => ensureFieldMonsterRuntime().tickFieldMonsters(),
  activateFloor,
  activateTownState,
  completeFinalEnding,
  items,
  monsters,
});

function rand(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function render() {
  renderAppFrame({
    state,
    documentObject: document,
    activeProductEntry,
    setProductEntry,
    currentModeStatusText,
    renderTitle,
    renderParty,
    renderResources,
    renderQuest,
    renderLog,
    renderMiniMap,
    renderTown,
    renderCombat,
    renderEditor,
    createViewController,
    viewController,
    setViewController: (next) => {
      viewController = next;
    },
    dragLookEnabled,
    inventoryOverlayOpen,
    materialManifest,
    rendererLodProfile: RENDERER_LOD_PROFILE,
    selectedPreset,
    resolveInteractionCandidate,
    dirs: DIRS,
    vec: VEC,
    logicalPlayerCell,
    interactivePlacementKinds: INTERACTIVE_PLACEMENT_KINDS,
    currentMapSeed,
    escapeHtml,
    items,
    sortedFilteredInventoryEntries,
    inventoryEntryEquipmentSlot,
    inventoryEntryItemId,
    inventoryEntryDetailText,
    compareCandidateSummary,
    inventoryFilterOptions,
    inventorySortOptions,
    inventorySummaryText,
    heroEquipmentEntries,
    inventoryEntryLabel,
    inventoryEntryKindLabel,
    resolveEventChoice,
    resolveNpcService,
    closeInteraction,
    equipInventoryEntryToHero,
    closeInventoryOverlay,
    closeInventoryPreview,
    inventoryManualReorderEnabled,
    reorderInventoryEntries,
    clearInventoryPreviewHoldTimer,
    scheduleInventoryPreviewHold,
    openInventoryPreview,
    useInventoryEntryOnHero,
    assignHeroSkillToDieFace,
    buySkillCard,
    sellSkillCard,
    normalizeInventoryList,
    boardQuestCanReturn,
    returnFromBoardQuest,
    render,
  });
}

function renderEditor() {
  return renderEditorFrame({
    renderEditorImpl,
    bindEditorFrame: () => bindEditorFrame({
      getState: () => state,
      render,
      documentObject: document,
      randomMapSeed,
      makeMap,
      addLog,
      startTestPlaySession: () => ensureRuntimeSessionManager().startTestPlaySession(),
      firstValidationIssue,
      validationIssueRepairHint,
      validationSummaryText,
      saveEditorProject,
      loadEditorProject,
    }),
  });
}

function renderParty() {
  return renderPartyFrame({
    state,
    documentObject: document,
    availableEquipmentEntries,
    nextClassMilestoneText,
    heroEquipmentEntries,
    equipmentEntryLabel,
    escapeHtml,
    compareCandidateSummary,
    inventoryEntryDetailText,
    inventoryEntryLabel,
  });
}

function renderResources() {
  return renderResourcesFrame({
    state,
    documentObject: document,
    inventorySummaryText,
    normalizeInventoryList,
    inventoryEntryLabel,
    inventoryEntryDetailText,
    skills,
    skillName,
    escapeHtml,
  });
}

function renderTitle() {
  renderTitleShell({
    state,
    documentObject: document,
    readSaveSlots,
    currentSaveSlotId,
    classes,
    selectedBackgroundForDraft,
    selectedLoadoutForDraft,
    buildProjectDashboardSnapshot,
    hasSavedEditorProject,
    saveSlotLabel,
    protagonistBackgrounds: PROTAGONIST_BACKGROUNDS,
    starterLoadouts: STARTER_LOADOUTS,
    items,
    escapeHtml,
    handleTitleAction,
    render,
  });
}

function renderQuest() {
  return renderQuestFrame({
    state,
    documentObject: document,
    normalizeQuestState,
    syncQuestSeedFailureStates,
    questEndingComplete,
    boardQuestCanReturn,
    escapeHtml,
  });
}

function renderLog() {
  return renderLogFrame({
    state,
    documentObject: document,
  });
}

function renderMiniMap() {
  return renderMiniMapFrame({
    state,
    documentObject: document,
    currentMapSeed,
    getCell,
  });
}

function renderTown() {
  const townFrame = renderTownFrame({
    state,
    inventoryOverlayOpen,
    inventoryOverlayMarkup: inventoryOverlayMarkupFrame,
    items,
    normalizeInventoryList,
    inventoryEntryEquipmentSlot,
    compareCandidateSummary,
    escapeHtml,
    inventoryFilterOptions,
    inventorySortOptions,
    inventorySummaryText,
    heroEquipmentEntries,
    inventoryEntryLabel,
    inventoryEntryDetailText,
    inventoryEntryKindLabel,
    sortedFilteredInventoryEntries,
    inventoryManualReorderEnabled,
    inventoryEntryItemId,
    inventoryEntryOverlayMarkupImpl: inventoryEntryOverlayMarkupFrame,
    classes,
  });
  const townRoot = document.getElementById("town");
  if (townRoot) {
    if (!townRoot.querySelector("#townView")) {
      townRoot.innerHTML = townFrame.overlayMarkup;
    } else {
      const runtimeOverlay = townRoot.querySelector("#townRuntimeOverlay");
      if (runtimeOverlay) {
        runtimeOverlay.innerHTML = townFrame.controlsMarkup;
      }
      const inventoryOverlayHost = townRoot.querySelector("#townInventoryOverlayHost");
      if (inventoryOverlayHost) {
        inventoryOverlayHost.innerHTML = townFrame.inventoryMarkup;
      }
    }
  }
  viewController = renderDungeonViewBridge({
    state,
    documentObject: document,
    createViewController,
    viewController,
    setViewController: (next) => {
      viewController = next;
    },
    dragLookEnabled,
    inventoryOverlayOpen,
    materialManifest,
    rendererLodProfile: RENDERER_LOD_PROFILE,
    selectedPreset,
    resolveInteractionCandidate,
    dirs: DIRS,
    vec: VEC,
    interactivePlacementKinds: INTERACTIVE_PLACEMENT_KINDS,
    currentMapSeed,
    escapeHtml,
    items,
    sortedFilteredInventoryEntries,
    inventoryManualReorderEnabled,
    normalizeInventoryList,
    inventoryEntryEquipmentSlot,
    inventoryEntryItemId,
    inventoryEntryDetailText,
    compareCandidateSummary,
    inventoryFilterOptions,
    inventorySortOptions,
    inventorySummaryText,
    heroEquipmentEntries,
    inventoryEntryLabel,
    inventoryEntryKindLabel,
    resolveEventChoice,
    resolveNpcService,
    closeInteraction,
    render,
    hostId: "townView",
    overlayId: "townViewOverlay",
    activeModes: ["town"],
    inventoryMode: "town",
  });
  bindTownFrame({
    state,
    documentObject: document,
    render,
    toggleInventoryOverlay,
    makeHero,
    normalizePartyModel,
    syncPartyRows,
    addLog,
  });
}

function renderCombat() {
  renderCombatFrame({
    state,
    documentObject: document,
    inventoryOverlayOpen,
    inventoryOverlayMarkup: inventoryOverlayMarkupFrame,
    items,
    sortedFilteredInventoryEntries,
    inventoryManualReorderEnabled,
    normalizeInventoryList,
    inventoryEntryEquipmentSlot,
    compareCandidateSummary,
    escapeHtml,
    inventoryFilterOptions,
    inventorySortOptions,
    inventorySummaryText,
    heroEquipmentEntries,
    inventoryEntryLabel,
    inventoryEntryDetailText,
    inventoryEntryKindLabel,
    inventoryEntryItemId,
    inventoryEntryOverlayMarkupImpl: inventoryEntryOverlayMarkupFrame,
    currentCombatHero,
    livingCombatEnemies,
    combatConsumableEntries,
  });
  bindCombatFrame({
    state,
    documentObject: document,
    render,
    toggleInventoryOverlay,
    queueCombatAction,
    resolveHeroAction,
    clearCombatDiceSelection,
    selectCombatDie,
    assignHeroSkillToDieFace,
    useCombatConsumable,
    useCombatThrowItem,
  });
}

const buildEditorRenderSnapshot = createEditorRenderSnapshotBridge({
  getState: () => state,
  validateMap,
  buildProjectDashboardSnapshot,
  buildCompiledMapForRuntime,
  selectedPreset,
  loadCustomPresets,
  activeBrushPreviewRect,
  currentBrushSelectionKeys,
  activeBrushSelectionRect,
  activeEventEditorTool,
  compatibleEventDefinitions,
  activeEventDefinitionId,
  activeEventDefinition,
  activeEventStepDefinition,
  buildEditorEventSnapshot,
  eventPlacementsAtCursor,
  activePlacementOverride,
  resolvePlacementEvent,
  activeClassDefinitionIndex,
  activeClassDefinition,
  activeQuestDefinitionId,
  activeQuestDefinition,
  activeMonsterDefinitionId,
  activeMonsterDefinition,
  activeSkillDefinitionId,
  activeSkillDefinition,
  activeItemDefinitionId,
  activeItemDefinition,
  activeVendorDefinitionId,
  activeVendorDefinition,
  activeVendorRotationDefinition,
  activeLootTableId,
  activeLootTableDefinition,
  activeLootTierDefinition,
  activeLootBonusDefinition,
  activeCombatRewardProfile,
  activeRarityDefinitionId,
  activeRarityDefinition,
  activeAffixDefinitionId,
  activeAffixDefinition,
  activeAffixPoolId,
  activeAffixPoolDefinition,
  activeNpcDefinitionId,
  activeNpcDefinition,
  activeNpcQuestSeed,
  activeNpcQuestSeedDefinition,
  activeNpcServiceDefinition,
  activeNpcDialogueStepDefinition,
  npcPlacementsAtCursor,
  activeNpcPlacement,
  activeNpcPlacementDefinition,
  getCell,
  buildDensityOverlaySnapshot,
  buildDensityHistogramSnapshot,
  buildRoomPlacementSummary,
  buildPlacementRecommendations,
  isRequiredEventPlacement,
  isRequiredNpcPlacement,
  buildEditorNpcSnapshot,
  buildEditorContentPanels,
  buildEditorNpcSupportPanels,
  buildEditorPlacementClassPanels,
  eventEditorToPlacementKind: EVENT_EDITOR_TO_PLACEMENT_KIND,
});

function renderEditorImpl() {
  ensureEditorState();
  const snapshot = buildEditorRenderSnapshot();
  const {
    validationReport,
    projectDashboard,
    projectValidationReport,
    compiled,
    preset,
    customPresets,
    activeRoom,
    previewRect,
    selectedKeys,
    selectedRect,
    selectedCount,
    eventTool,
    eventPlacementKind,
    compatiblePresets,
    eventDefId,
    eventDef,
    selectedEventStepDefState,
    selectedEventStepDef,
    eventSnapshot,
    cursorEventPlacements,
    selectedPlacement,
    selectedPlacementEvent,
    linkedPlacements,
    linkedIssues,
    classDefIndex,
    classDef,
    questDefId,
    questDef,
    monsterDefId,
    monsterDef,
    itemDefId,
    itemDef,
    vendorDefId,
    vendorDef,
    selectedVendorRotationState,
    selectedVendorRotation,
    lootTableId,
    lootTableDef,
    selectedLootTierState,
    selectedLootTier,
    selectedLootBonusState,
    selectedLootBonus,
    selectedCombatRewardProfileState,
    selectedCombatRewardProfile,
    rarityDefId,
    rarityDef,
    affixDefId,
    affixDef,
    affixPoolId,
    affixPoolDef,
    sampleItemPreview,
    npcDefId,
    npcDef,
    npcQuestSeed,
    selectedNpcQuestSeedDefState,
    selectedNpcQuestSeedDef,
    selectedNpcQuestSeedRewards,
    selectedNpcQuestSeedRuntime,
    linkedNpcQuestServices,
    selectedNpcServiceDefState,
    selectedNpcServiceDef,
    selectedNpcDialogueStepDefState,
    selectedNpcDialogueStepDef,
    cursorNpcPlacements,
    selectedNpcPlacement,
    selectedNpcPlacementDef,
    cursorCell,
    densityOverlay,
    recommendationRoomId,
    densityHistogram,
    recommendationRoomSummary,
    placementRecommendations,
    selectedPlacementRequired,
    selectedNpcPlacementRequired,
    npcCustomPresets,
    selectedNpcCustomPresetId,
    selectedNpcCustomPreset,
    selectedNpcCustomPresetApplyMode,
    selectedNpcCustomPresetConflictMode,
    npcCustomPresetDiff,
    selectedPresetServiceIndexes,
    selectedPresetSeedIndexes,
    npcCustomPresetMergePatch,
    npcCustomPresetMergePatchDraftValue,
    npcCustomPresetMergePatchPreview,
    npcCustomPresetMergePatchValidation,
    npcPresetPatchHistory,
    latestNpcPresetUndoEntry,
    npcPresetRedoEntries,
    latestNpcPresetRedoEntry,
    npcPresetRedoArchive,
    npcPresetRedoArchiveQuery,
    filteredNpcPresetRedoArchive,
    npcPresetRedoArchiveBatchCompare,
    selectedNpcPresetRedoArchiveId,
    selectedNpcPresetRedoArchiveEntry,
    npcPresetPatchArchive,
    npcPresetPatchArchiveQuery,
    filteredNpcPresetPatchArchive,
    selectedNpcPresetPatchArchiveId,
    selectedNpcPresetPatchArchiveEntry,
    npcPresetPatchArchivePreviewText,
    npcPresetPatchArchiveLineDiffText,
    npcPresetPatchArchiveCompare,
    npcPresetApplyComparePreview,
    npcQuestEditorPanelMarkup,
    npcServicePanelMarkup,
    questPanelMarkup,
    monsterPanelMarkup,
    skillPanelMarkup,
    itemPanelMarkup,
    vendorPanelMarkup,
    lootPanelMarkup,
    affixPanelMarkup,
    sampleItemPanelMarkup,
    npcProgressionHooksPanelMarkup,
    npcPlacementPanelMarkup,
    presetStudioPanelMarkup,
    placementOverridePanelMarkup,
    classProgressionPanelMarkup,
  } = snapshot;
  const {
    eventValidationSnapshot,
    eventGraphPreview,
    eventGraphCompactExport,
    eventGraphSummaryDiff,
    eventProjectReviewBundle,
    eventExportHistory,
    eventExportArchive,
    eventExportArchiveQuery,
    filteredEventExportHistory,
    filteredEventExportArchive,
    eventExportArchiveBatchCompare,
    eventExportArchiveBatchCompareExport,
    eventExportArchiveBatchShareLabel,
    eventExportArchiveBatchShareExport,
    eventExportArchiveBatchShareLink,
    eventExportArchiveBatchShareLinkDraft,
    selectedEventExportArchiveId,
    selectedEventExportArchiveEntry,
    selectedEventExportArchiveBundleRows,
    selectedEventExportArchiveBundleRowId,
    selectedEventExportArchiveBundleRowsForRestore,
    effectiveEventExportArchiveBundleRowIds,
    selectedEventExportArchiveTargetEventId,
    selectedEventExportArchiveCompareDiff,
    selectedEventExportArchiveRollbackPlan,
    eventExportArchiveFieldOptions,
    eventExportArchiveStepOptions,
    eventExportArchiveStepPartOptions,
    eventExportArchiveStepItemOptions,
    effectiveEventExportArchiveFieldKeys,
    effectiveEventExportArchiveStepIds,
    effectiveEventExportArchiveStepPartKeys,
    effectiveEventExportArchiveStepItemKeys,
    selectedEventExportArchiveCurrentTargetJson,
    selectedEventExportArchiveRestoreTargetJson,
    selectedEventExportArchiveRollbackDiffText,
    eventExportArchiveRestoreBadgeLookup,
    previousGraphArchive,
    previousGraphArchiveDiff,
    currentGraphExportJson,
    eventGraphJsonDiffText,
    currentBundleSummary,
    previousBundleArchive,
    previousBundleArchiveDiff,
    currentBundleExportJson,
    eventBundleJsonDiffText,
    eventBundleStructuralCompare,
    eventBundleCompareOptions,
    selectedEventBundleCompareEventId,
    selectedEventBundleCompareRow,
    selectedEventBundleCompareCurrent,
    selectedEventBundleComparePrevious,
    selectedEventBundleComparePatch,
    defaultEventBundlePatchJson,
    eventBundlePatchDraftValue,
    selectedEventBundleComparePatchPreview,
    selectedEventBundleResolvedPreview,
    eventBundlePatchHistory,
    eventBundlePatchArchive,
    eventBundlePatchArchiveQuery,
    filteredEventBundlePatchHistory,
    filteredEventBundlePatchArchive,
    selectedEventBundlePatchArchiveEntryId,
    selectedEventBundlePatchArchiveEntry,
    selectedEventBundleVisualDiffRows,
    eventBundleFocusOptions,
    selectedEventBundleFocusPath,
    focusedEventBundlePreviousValue,
    focusedEventBundleCurrentValue,
    focusedEventBundleResolvedValue,
    activeEditorEventTest,
    activeEditorEventInteraction,
  } = eventSnapshot;
  const {
    toolPanelsMarkup,
    presetLibraryMarkup,
  } = buildEditorWorkspacePanels(buildEditorWorkspacePanelArgs({
    state,
    activeRoom,
    previewRect,
    selectedCount,
    selectedRect,
    densityOverlay,
    densityHistogram,
    recommendationRoomId,
    recommendationRoomSummary,
    placementRecommendations,
    preset,
    compiled,
    validationReport,
    projectValidationReport,
    validationIssueMarkup,
    projectDashboard,
    floorTextureIds: FLOOR_TEXTURE_IDS,
    ceilingTextureIds: CEILING_TEXTURE_IDS,
    wallTextureIds: WALL_TEXTURE_IDS,
    battleBackgrounds: BATTLE_BACKGROUNDS,
    cellTags: CELL_TAGS,
    roomTypes: ROOM_TYPES,
    isRangeBrushTool,
    activeBrushRangeStart,
    currentMapSeed,
    customPresets,
    selectedPreset,
  }));
  const eventPanelBodyDeps = buildEditorEventPanelBodyDeps({
    eventDef,
    eventTool,
    eventDefId,
    compatiblePresets,
    eventValidationSnapshot,
    validationSummaryText,
    eventProjectReviewBundle,
    currentBundleExportJson,
    previousBundleArchiveDiff,
    previousBundleArchive,
    eventExportArchiveLine,
    eventBundleJsonDiffText,
    eventBundleStructuralCompare,
    eventBundleCompareOptions,
    selectedEventBundleCompareEventId,
    selectedEventBundleCompareRow,
    selectedEventBundleComparePrevious,
    selectedEventBundleCompareCurrent,
    selectedEventBundleVisualDiffRows,
    selectedEventBundleComparePatch,
    eventBundlePatchDraftValue,
    selectedEventBundleResolvedPreview,
    filteredEventBundlePatchHistory,
    eventBundlePatchHistory,
    filteredEventBundlePatchArchive,
    eventBundlePatchArchive,
    eventBundlePatchArchiveQuery,
    selectedEventBundlePatchArchiveEntryId,
    selectedEventBundlePatchArchiveEntry,
    eventBundlePatchArchiveLine,
    eventBundleFocusOptions,
    selectedEventBundleFocusPath,
    focusedEventBundlePreviousValue,
    focusedEventBundleCurrentValue,
    focusedEventBundleResolvedValue,
    renderEditorEventExportArchiveBody,
    eventExportArchiveDeps: {
      eventExportArchiveQuery,
      filteredEventExportHistory,
      eventExportHistory,
      filteredEventExportArchive,
      eventExportArchive,
      eventExportHistoryLine,
      eventExportArchiveLine,
      eventExportArchiveBatchCompare,
      eventExportArchiveBatchCompareExport,
      eventExportArchiveBatchShareLabel,
      eventExportArchiveBatchShareExport,
      eventExportArchiveBatchShareLink,
      eventExportArchiveBatchShareLinkDraft,
      selectedEventExportArchiveId,
      selectedEventExportArchiveEntry,
      selectedEventExportArchiveBundleRows,
      selectedEventExportArchiveBundleRowId,
      selectedEventExportArchiveBundleRowsForRestore,
      effectiveEventExportArchiveBundleRowIds,
      eventExportArchiveFieldOptions,
      eventExportArchiveStepOptions,
      eventExportArchiveStepPartOptions,
      eventExportArchiveStepItemOptions,
      effectiveEventExportArchiveFieldKeys,
      effectiveEventExportArchiveStepIds,
      effectiveEventExportArchiveStepPartKeys,
      effectiveEventExportArchiveStepItemKeys,
      eventExportArchiveRestoreBadgeLookup,
      selectedEventExportArchiveCompareDiff,
      selectedEventExportArchiveTargetEventId,
      selectedEventExportArchiveRollbackPlan,
      selectedEventExportArchiveCurrentTargetJson,
      selectedEventExportArchiveRestoreTargetJson,
      selectedEventExportArchiveRollbackDiffText,
      renderDiffBadgeHtml,
      buildDiffBadgeSpec,
      buildDiffCountScaleLabel,
      escapeHtml,
    },
    renderEditorEventGraphBody,
    selectedEventStepDef,
    selectedEventStepDefState,
    eventGraphPreview,
    currentGraphExportJson,
    previousGraphArchiveDiff,
    previousGraphArchive,
    eventGraphJsonDiffText,
    eventGraphSummaryDiff,
    activeEditorEventTest,
    activeEditorEventInteraction,
    classes,
    resourceKeys: [...RESOURCE_KEYS],
    partyStatKeys: [...PARTY_STAT_KEYS],
    eventEffectTypes: [...EVENT_EFFECT_TYPES],
    eventPlacementKind,
    allowedInteractionsForPlacementKind,
    linkedPlacements,
    linkedIssues,
    effectJson,
    eventStepsJson,
    renderEventEffectFields,
    escapeHtml,
    buildDiffBadgeSpec,
    buildDiffCountScaleLabel,
    renderDiffBadgeHtml,
    eventEditorToPlacementKind: EVENT_EDITOR_TO_PLACEMENT_KIND,
    eventTriggerTypes: [...EVENT_TRIGGER_TYPES],
  });
  document.getElementById("editor").innerHTML = renderEditorMainWorkspace(buildEditorMainWorkspaceArgs({
    state,
    cursorCell,
    currentMapSeed,
    defaultFloorTextureId: DEFAULT_FLOOR_TEXTURE_ID,
    defaultCeilingTextureId: DEFAULT_CEILING_TEXTURE_ID,
    defaultWallTextureId: DEFAULT_WALL_TEXTURE_ID,
    toolPanelsMarkup,
    presetLibraryMarkup,
    buildEditorEventPanelBody,
    eventTool,
    eventDefId,
    eventEditorToPlacementKind: EVENT_EDITOR_TO_PLACEMENT_KIND,
    eventPanelBodyDeps,
    placementOverridePanelMarkup,
    classProgressionPanelMarkup,
    questPanelMarkup,
    monsterPanelMarkup,
    skillPanelMarkup,
    itemPanelMarkup,
    vendorPanelMarkup,
    lootPanelMarkup,
    affixPanelMarkup,
    sampleItemPanelMarkup,
    npcProgressionHooksPanelMarkup,
    npcQuestEditorPanelMarkup,
    npcServicePanelMarkup,
    npcPlacementPanelMarkup,
    presetStudioPanelMarkup,
  }));
  if (state.editorWorkspaceMode === "generator_workbench") return;
  bindEditorWorkspaceInteractions(buildEditorWorkspaceBindingArgs({
    state,
    documentObject: document,
    getCell,
    cellCoordKey,
    classifyDensityBand,
    textureSwatchColor,
    cellTags: CELL_TAGS,
    previewRect,
    selectedKeys,
    densityOverlay,
    defaultFloorTextureId: DEFAULT_FLOOR_TEXTURE_ID,
    defaultCeilingTextureId: DEFAULT_CEILING_TEXTURE_ID,
    defaultWallTextureId: DEFAULT_WALL_TEXTURE_ID,
    isLassoBrushMode,
    activeBrushRangeStart,
    isRangeBrushTool,
    beginLassoBrushDrag,
    beginRangeBrushDrag,
    updateLassoBrushDrag,
    updateRangeBrushDrag,
    commitLassoBrushSelection,
    applyRangeBrushAtCurrentCursor,
    computeWalls,
    editCell,
    render,
    eventEditorToPlacementKind: EVENT_EDITOR_TO_PLACEMENT_KIND,
    applyRecommendationAutoPlacement,
    recommendationRoomId,
    placementRecommendations,
    normalizeTextureId,
    floorTextureIds: FLOOR_TEXTURE_IDS,
    ceilingTextureIds: CEILING_TEXTURE_IDS,
    wallTextureIds: WALL_TEXTURE_IDS,
    clearRangeBrushState,
    densityOverlayModes: DENSITY_OVERLAY_MODES,
    applyCommittedBrushSelection,
    rememberExplicitBrushSelection,
    currentBrushFloodSelectionPoints,
    currentBrushMatchingSelectionPoints,
    transformCommittedBrushSelection,
    grownSelectionPoints,
    shrunkSelectionPoints,
    invertedSelectionPoints,

    eventDef,
    eventTool,
    eventDefId,
    eventPlacementKind,
    state,
    render,
    documentObject: document,
    addLog,
    parseJsonField,
    updateEventDefinition,
    activeEventDefinitionId,
    stopEditorEventTestSession,
    renameEventPreset,
    createEventPresetFromDefinition,
    defaultInteractionTypeForPlacementKind,
    createEventGraphTemplate,
    uniqueEventStepId,
    eventEffectFlagValueType,
    selectedEventBundleCompareEventId,
    eventBundleFocusOptions,
    selectedEventBundleFocusPath,
    focusedEventBundlePreviousValue,
    focusedEventBundleCurrentValue,
    focusedEventBundleResolvedValue,
    copyTextToClipboard,
    recordEventBundlePatchHistory,
    recordEventBundlePatchArchive,
    selectedEventBundleCompareRow,
    selectedEventBundleComparePatch,
    selectedEventBundleComparePatchPreview,
    eventBundlePatchDraftValue,
    defaultEventBundlePatchJson,
    selectedEventBundlePatchArchiveEntry,
    selectedEventBundleResolvedPreview,
    downloadTextFile,
    applyResolvedEventBundleRow,
    eventGraphCompactExport,
    eventProjectReviewBundle,
    currentBundleSummary,
    recordEventExportHistory,
    recordEventExportArchive,
    selectedEventExportArchiveEntry,
    selectedEventExportArchiveBundleRowsForRestore,
    effectiveEventExportArchiveFieldKeys,
    effectiveEventExportArchiveStepIds,
    effectiveEventExportArchiveStepPartKeys,
    effectiveEventExportArchiveStepItemKeys,
    hasRestorableCompactEventPayload,
    applyPartialCompactEventRowToDefinition,
    selectedEventExportArchiveTargetEventId,
    deleteEventExportArchiveEntry,
    eventExportArchiveQuery,
    eventExportArchiveBatchCompare,
    eventExportArchiveBatchCompareExport,
    applyEventExportArchiveBatchCompareTargets,
    eventExportArchiveBatchShareExport,
    eventExportArchiveBatchShareLink,
    loadEventExportArchive,
    isRestorableEventExportEntry,
    eventExportEntryMatchesQuery,
    buildEventExportArchiveBatchCompare,
    importEventExportArchiveBatchShareLink,
    eventGraphJsonDiffText,
    eventBundleJsonDiffText,
    selectedEventStepDef,
    startEditorEventTestSession,
    resolveEventChoice,
    createEventStepTemplate,
    createEventBranchTemplate,
    createEventChoiceTemplate,
    createEventEffectTemplate,
    selectedPlacement,
    selectedPlacementEvent,
    eventDefinitions,
    updatePlacementOverrides,
    resolvePlacementEvent,
    toggleRequiredPlacementContract,
    isRequiredEventPlacement,

    npcDef,
    npcDefId,
    state,
    render,
    documentObject: document,
    addLog,
    parseJsonField,
    updateNpcDefinition,
    createNpcQuestHookTemplate,
    createQuestSeedTemplate,
    duplicateQuestSeedTemplate,
    loadNpcCustomPresets,
    saveNpcCustomPresets,
    buildNpcCustomPresetFromDefinition,
    defaultNpcPresetSelectionIndexes,
    defaultNpcPresetDialogueSelectionMap,
    defaultNpcPresetDialogueChoiceSelectionMap,
    defaultNpcPresetDialogueBranchSelectionMap,
    defaultNpcPresetServiceFieldSelectionMap,
    defaultNpcPresetSeedFieldSelectionMap,
    buildNpcCustomPresetDiff,
    selectedNpcCustomPreset,
    npcCustomPresetDiff,
    selectedNpcPresetServiceIndexes,
    selectedNpcPresetSeedIndexes,
    selectedNpcPresetDialogueStepIndexes,
    selectedNpcPresetDialogueChoiceIndexes,
    selectedNpcPresetDialogueBranchIndexes,
    selectedNpcPresetServiceFieldNames,
    selectedNpcPresetSeedFieldNames,
    npcPresetServiceIdentity,
    npcPresetSeedIdentity,
    npcDialogueStepIdentity,
    npcDialogueChoiceIdentity,
    npcDialogueBranchIdentity,
    npcCustomPresetMergePatch,
    npcCustomPresetMergePatchDraftValue,
    npcCustomPresetMergePatchPreview,
    selectedNpcPresetPatchArchiveEntry,
    filteredNpcPresetPatchArchive,
    npcPresetPatchArchiveQuery,
    latestNpcPresetUndoEntry,
    latestNpcPresetRedoEntry,
    npcPresetPatchHistory,
    npcPresetRedoEntries,
    selectedNpcPresetRedoArchiveEntry,
    downloadTextFile,
    copyTextToClipboard,
    readTextFromClipboard,
    captureNpcPresetUndoSnapshot,
    clearNpcPresetRedoEntry,
    recordNpcPresetPatchHistory,
    recordNpcPresetPatchArchive,
    applyNpcPresetMergePatchToDefinition,
    deleteNpcPresetPatchArchiveEntry,
    deleteNpcPresetPatchArchiveEntries,
    pushNpcPresetRedoEntry,
    applyNpcPresetUndoSnapshot,
    deleteNpcPresetRedoArchiveEntry,
    selectedNpcServiceDef,
    createNpcServiceTemplate,
    createNpcServiceGroupTemplates,
    items,
    selectedNpcQuestSeedDef,
    questRewardFlagValueType,
    createNpcDialogueStepTemplate,
    createNpcDialogueChoiceTemplate,
    selectedNpcPlacement,
    toggleRequiredPlacementContract,
    isRequiredNpcPlacement,

    state,
    render,
    documentObject: document,
    addLog,
    classDef,
    classDefIndex,
    parseJsonField,
    updateClassDefinition,
    questDef,
    questDefId,
    questDefinitions,
    replaceQuestDefinitions,
    activeQuestDefinitionId,
    uniqueQuestDefinitionId,
    createQuestDefinitionTemplate,
    updateQuestDefinition,
    buildGeneratedQuestDefinition,
    monsterDef,
    monsterDefId,
    monsters,
    replaceMonsterDefinitions,
    activeMonsterDefinitionId,
    uniqueMonsterDefinitionId,
    createMonsterDefinitionTemplate,
    updateMonsterDefinition,
    skillDef: activeSkillDefinition(),
    skillDefId: activeSkillDefinitionId(),
    skills,
    replaceSkillDefinitions,
    activeSkillDefinitionId,
    uniqueSkillDefinitionId,
    createSkillDefinitionTemplate,
    updateSkillDefinition,
    itemDef,
    itemDefId,
    items,
    replaceItemDefinitions,
    activeItemDefinitionId,
    uniqueItemDefinitionId,
    createItemDefinitionTemplate,
    updateItemDefinition,
    vendorDef,
    vendorDefId,
    vendors,
    replaceVendorDefinitions,
    activeVendorDefinitionId,
    uniqueVendorDefinitionId,
    createVendorDefinitionTemplate,
    updateVendorDefinition,
    ensureVendorInventoryEntryObject,
    setGeneratedEntryFields,
    selectedVendorRotation,
    selectedVendorRotationState,
    createVendorRotationTemplate,
    lootTableDef,
    lootTableId,
    lootTables,
    replaceLootTableDefinitions,
    lootTableDefinitionIds,
    activeLootTableId,
    uniqueLootTableId,
    createLootTableDefinitionTemplate,
    updateLootTableDefinition,
    ensureLootEntryObject,
    createLootEntryTemplate,
    selectedLootTier,
    selectedLootTierState,
    selectedLootBonus,
    selectedLootBonusState,
    selectedCombatRewardProfile,
    selectedCombatRewardProfileState,
    createLootTierTemplate,
    createLootBonusTemplate,
    createCombatRewardProfileTemplate,
    rarityDef,
    rarityDefId,
    affixDef,
    affixDefId,
    affixPoolDef,
    affixPoolId,
    rarityDefinitions,
    affixDefinitions,
    affixPoolDefinitions,
    uniqueSchemaId,
    createRarityDefinitionTemplate,
    updateRarityDefinition,
    replaceRarityDefinitions,
    createAffixDefinitionTemplate,
    updateAffixDefinition,
    replaceAffixDefinitions,
    createAffixPoolDefinitionTemplate,
    updateAffixPoolDefinition,
    replaceAffixPoolDefinitions,
    buildSampleItemPreview,
    makeGeneratedItemInstance,
    pushInventoryEntry,
    inventoryEntryLabel,
    preset,
    randomMapSeed,
    makeMap,
    applyDraftFromPreset,
    startTestPlaySession: () => ensureRuntimeSessionManager().startTestPlaySession(),
    firstValidationIssue,
    validationIssueRepairHint,
    validationSummaryText,
    saveEditorProject,
    loadEditorProject,
    cloneEditorMap,
    buildEditorProject,
    buildCompiledMapForRuntime,
    buildContentBuildManifest,
    applyEditorProject,
    normalizeMapMetadata,
    computeWalls,
    presetDraftSize: PRESET_DRAFT_SIZE,
    createEmptyPresetGrid,
    createDraftPreset,
    draftFromGrid,
    upsertCustomPreset,
    refreshPresetCatalog,
    deleteCustomPreset,
  }));
}

function editCell(x, y, event) {
  const c = getCell(state.map, x, y);
  if (!c) return;
  state.editorCursor = { x, y };
  if (isRangeBrushTool() && state.editorBrushDrag) return;
  if (isRangeBrushTool() && state.editorLassoSelectionDrag) return;
  if (isRangeBrushTool() && state.suppressRangeClick) {
    state.suppressRangeClick = false;
    return;
  }
  handleEditorCellInteraction({
    x,
    y,
    event,
    state,
    cell: c,
    getCell,
    wallKey,
    createEditorPlacement,
    isLassoBrushMode,
    applyRangeBrushAtCurrentCursor,
    selectedPreset,
    instantiatePreset,
    computeWalls,
    render,
    eventObjectPlacementKinds: EVENT_OBJECT_PLACEMENT_KINDS,
  });
}

const {
  handleTitleAction,
  saveSlotStorageKey,
  saveSlotLabel,
  saveUsesEmbeddedContentDefinitions,
  saveContentVersionMatchesCurrent,
  formatPlaytimeLabel,
  buildRecentStatusLabel,
  selectedBackgroundForDraft,
  selectedLoadoutForDraft,
  summarizeSaveData,
  readSaveSlotData,
  readSaveSlots,
  currentSaveSlotId,
  renameSaveSlot,
  saveGame,
  deleteSaveSlot,
  parseSaveSlotPayload,
  loadGame,
  setMode,
  activeProductEntry,
  currentModeStatusText,
  createFreshEditorWorkspace,
  openSavedEditorWorkspace,
  setProductEntry,
  playerRuntimeControls,
} = bootstrapAppRuntime({
  shellBridgeDeps: {
    getState: () => state,
    saveSlotIds: SAVE_SLOT_IDS,
    saveSlotStoragePrefix: SAVE_SLOT_STORAGE_PREFIX,
    currentContentVersion: CURRENT_CONTENT_VERSION,
    classes,
    questEndingComplete,
    ensureProductShellManager,
    ensureSaveSlotManager,
    saveSlotStorageKeyModule,
    saveSlotLabelModule,
    saveUsesEmbeddedContentDefinitionsModule,
    saveContentVersionMatchesCurrentModule,
    formatPlaytimeLabelModule,
    buildRecentStatusLabelModule,
    summarizeSaveDataModule,
    parseSaveSlotPayloadModule,
  },
  productModeBridgeDeps: {
    getState: () => state,
    setState,
    render,
    addLog,
    ensureEditorState,
    currentSaveSlotId: () => state.shell?.selectedSaveSlotId || SAVE_SLOT_IDS[0],
    initialState,
    createValidatedRuntimeFloorMaps,
    buildProjectValidationReport,
    compileProjectForRuntime,
    hasValidationErrors,
    setModeFallback: (mode) => setMode(mode),
    closeInteraction,
    closeInventoryOverlay,
    releasePointerLook,
    hasSavedEditorProject,
    loadEditorProject,
    endTestPlaySession: (mode) => ensureRuntimeSessionManager().endTestPlaySession(mode),
  },
  debugHarnessDeps: {
    windowObject: typeof window !== "undefined" ? window : null,
    registerDebugHarness,
    createDebugHarness,
    getState: () => state,
    dirs: DIRS,
    vec: VEC,
    encounters,
    monsters,
    getCell,
    blocks,
    render,
    addLog,
    normalizeInventoryList,
    normalizeQuestState,
    updateBoardQuestCompletion,
    resolveStairsOutcome,
    completeFinalEnding,
    activateFloor,
    activateTownState,
    getPointerLookState: () => viewController?.getPointerLookState?.() || null,
    logicalCellKey,
  },
  playerRuntimeDeps: {
    createPlayerActionRunner,
    createPlayerController,
    getState: () => state,
    dirs: DIRS,
    getPointerLookState: () => viewController?.getPointerLookState?.() || null,
    inventoryOverlayOpen,
    toggleInventoryOverlay,
    closeInventoryOverlay,
    closeInteraction,
    interact: interactRuntime,
    rest,
    toggleDragLook,
    spendTorch,
    addLog,
    afterMove,
    render,
    runEventPlacement,
    allowedInteractionsForPlacementKind,
    activeMovementBlockersAt,
    blocks,
    documentObject: document,
    target: window,
    isEditableTarget,
  },
});
const playerActions = playerRuntimeControls.playerActions;
playerController = playerRuntimeControls.playerController;

autoImportEventExportArchiveBatchShareLinkFromLocation();
render();

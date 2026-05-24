import { defaultEventSelection, serializeEditorSessionState } from "./editorState.js";
import { loadCustomPresets, saveCustomPresets } from "./presets.js";

export const EDITOR_PROJECT_STORAGE_KEY = "serpent_editor_project_v1";

export function buildEditorProject(deps) {
  const {
    state,
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
    buildEditorStateConfig,
    validateMap,
    cloneEditorMap,
    buildCompiledMapForRuntime,
    ensureBoundaryClean,
    loadNpcCustomPresets,
  } = deps;
  validateEventDefinitionsTable(eventDefinitions);
  validateClassDefinitionsTable(classes);
  validateQuestDefinitionsTable(questDefinitions);
  validateMonsterDefinitionsTable(monsters);
  validateSkillDefinitionsTable(skills);
  validateItemDefinitionsTable(items);
  validateVendorDefinitionsTable(vendors);
  validateLootTableDefinitionsTable(lootTables);
  validateRarityDefinitionsTable(rarityDefinitions);
  validateAffixDefinitionsTable(affixDefinitions, rarityDefinitions);
  validateAffixPoolDefinitionsTable(affixPoolDefinitions, affixDefinitions);
  validateNpcDefinitionsTable(npcs);
  validateQuestSeedReferenceIntegrity(npcs, eventDefinitions);
  const editorSessionState = serializeEditorSessionState(state.editor, buildEditorStateConfig());
  const validationReport = validateMap(state.map);
  const draftMaps = Object.entries(state.floorMaps).map(([floor, map]) => ({
    floor: Number(floor),
    map: cloneEditorMap(map),
  }));
  const project = {
    schemaVersion: 1,
    id: "project_serpent_temple",
    name: "Serpent Temple",
    updatedAt: new Date().toISOString(),
    editorProject: {
      activeFloor: state.player.floor,
      activeMapId: state.map.id,
      draftMaps,
      editorSessionState: {
        ...editorSessionState,
        customPresets: loadCustomPresets(),
        npcCustomPresets: loadNpcCustomPresets(),
      },
      validationReport,
      contentDefinitions: {
        eventDefinitions: JSON.parse(JSON.stringify(eventDefinitions)),
        classDefinitions: JSON.parse(JSON.stringify(classes)),
        questDefinitions: JSON.parse(JSON.stringify(questDefinitions)),
        monsterDefinitions: JSON.parse(JSON.stringify(monsters)),
        skillDefinitions: JSON.parse(JSON.stringify(skills)),
        itemDefinitions: JSON.parse(JSON.stringify(items)),
        vendorDefinitions: JSON.parse(JSON.stringify(vendors)),
        lootTableDefinitions: JSON.parse(JSON.stringify(lootTables)),
        rarityDefinitions: JSON.parse(JSON.stringify(rarityDefinitions)),
        affixDefinitions: JSON.parse(JSON.stringify(affixDefinitions)),
        affixPoolDefinitions: JSON.parse(JSON.stringify(affixPoolDefinitions)),
        npcDefinitions: JSON.parse(JSON.stringify(npcs)),
      },
    },
    compiledMaps: Object.values(state.floorMaps)
      .filter((map) => buildCompiledMapForRuntime(map).ok)
      .map((map) => map.id),
  };
  ensureBoundaryClean(project.compiledMaps, "editorProject.compiledMaps");
  return project;
}

export function applyEditorProject(project, deps) {
  const {
    state,
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
    saveNpcCustomPresets,
    validateEventDefinitionsTable,
    validateQuestSeedReferenceIntegrity,
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
    computeWalls,
    refreshPresetCatalog,
    normalizeTextureId,
    editorEventSelectionDefaults,
    lootTableDefinitionIds,
    FLOOR_TEXTURE_IDS,
    CEILING_TEXTURE_IDS,
    WALL_TEXTURE_IDS,
    DEFAULT_FLOOR_TEXTURE_ID,
    DEFAULT_CEILING_TEXTURE_ID,
    DEFAULT_WALL_TEXTURE_ID,
    EVENT_EDITOR_TO_PLACEMENT_KIND,
    DENSITY_OVERLAY_MODES,
  } = deps;
  if (!project || project.schemaVersion !== 1 || !project.editorProject) throw new Error("editorProject 형식이 아니다.");
  const draftMaps = project.editorProject.draftMaps || [];
  if (!draftMaps.length) throw new Error("프로젝트에 draftMaps가 없다.");
  const editorSessionState = project.editorProject.editorSessionState || project.editorProject.brushPresets || {};
  const contentDefinitions = project.editorProject.contentDefinitions || {};
  if (Array.isArray(editorSessionState.customPresets)) saveCustomPresets(editorSessionState.customPresets);
  if (Array.isArray(editorSessionState.npcCustomPresets)) saveNpcCustomPresets(editorSessionState.npcCustomPresets);
  if (contentDefinitions.eventDefinitions && typeof contentDefinitions.eventDefinitions === "object") {
    validateEventDefinitionsTable(contentDefinitions.eventDefinitions);
    Object.keys(eventDefinitions).forEach((key) => delete eventDefinitions[key]);
    Object.assign(eventDefinitions, JSON.parse(JSON.stringify(contentDefinitions.eventDefinitions)));
  }
  if (Array.isArray(contentDefinitions.classDefinitions)) replaceClassDefinitions(contentDefinitions.classDefinitions);
  if (contentDefinitions.questDefinitions && typeof contentDefinitions.questDefinitions === "object") replaceQuestDefinitions(contentDefinitions.questDefinitions);
  if (contentDefinitions.monsterDefinitions && typeof contentDefinitions.monsterDefinitions === "object") replaceMonsterDefinitions(contentDefinitions.monsterDefinitions);
  if (contentDefinitions.skillDefinitions && typeof contentDefinitions.skillDefinitions === "object") replaceSkillDefinitions(contentDefinitions.skillDefinitions);
  if (contentDefinitions.itemDefinitions && typeof contentDefinitions.itemDefinitions === "object") replaceItemDefinitions(contentDefinitions.itemDefinitions);
  if (contentDefinitions.vendorDefinitions && typeof contentDefinitions.vendorDefinitions === "object") replaceVendorDefinitions(contentDefinitions.vendorDefinitions);
  if (contentDefinitions.lootTableDefinitions && typeof contentDefinitions.lootTableDefinitions === "object") replaceLootTableDefinitions(contentDefinitions.lootTableDefinitions);
  if (contentDefinitions.rarityDefinitions && typeof contentDefinitions.rarityDefinitions === "object") replaceRarityDefinitions(contentDefinitions.rarityDefinitions);
  if (contentDefinitions.affixDefinitions && typeof contentDefinitions.affixDefinitions === "object") replaceAffixDefinitions(contentDefinitions.affixDefinitions);
  if (contentDefinitions.affixPoolDefinitions && typeof contentDefinitions.affixPoolDefinitions === "object") replaceAffixPoolDefinitions(contentDefinitions.affixPoolDefinitions);
  if (contentDefinitions.npcDefinitions && typeof contentDefinitions.npcDefinitions === "object") replaceNpcDefinitions(contentDefinitions.npcDefinitions);
  validateQuestSeedReferenceIntegrity(npcs, eventDefinitions);
  const floorMaps = {};
  for (const entry of draftMaps) {
    const floor = Number(entry.floor || entry.map?.start?.floor || 1);
    const map = entry.map;
    if (!map?.start || !Array.isArray(map.cells) || !Array.isArray(map.placements)) throw new Error(`층 ${floor} 맵 형식이 올바르지 않다.`);
    normalizeMapMetadata(map, deps);
    computeWalls(map);
    floorMaps[floor] = map;
  }
  state.floorMaps = floorMaps;
  const activeFloor = Number(project.editorProject.activeFloor || Object.keys(floorMaps)[0] || 1);
  state.map = state.floorMaps[activeFloor] || Object.values(state.floorMaps)[0];
  state.player = {
    floor: state.map.start.floor || activeFloor,
    x: state.map.start.x,
    y: state.map.start.y,
    facing: state.map.start.facing,
  };
  state.visited = new Set([`${state.player.x},${state.player.y}`]);
  state.visitedByFloor = Object.fromEntries(Object.keys(state.floorMaps).map((floor) => [floor, new Set()]));
  state.visitedByFloor[state.player.floor] = state.visited;
  refreshPresetCatalog(false);
  state.editor.selectedPresetId = editorSessionState.selectedPresetId && state.presetCatalog.some((preset) => preset.id === editorSessionState.selectedPresetId)
    ? editorSessionState.selectedPresetId
    : state.presetCatalog[0]?.id || "";
  state.editor.presetRotation = Number(editorSessionState.presetRotation || 0) % 4;
  state.editor.generationPresetIds = Array.isArray(editorSessionState.generationPresetIds)
    ? editorSessionState.generationPresetIds.filter((id) => state.presetCatalog.some((preset) => preset.id === id))
    : state.presetCatalog.map((preset) => preset.id);
  if (!state.editor.generationPresetIds.length) state.editor.generationPresetIds = state.presetCatalog.map((preset) => preset.id);
  state.editor.editorWorkspaceMode = editorSessionState.editorWorkspaceMode === "generator_workbench" ? "generator_workbench" : "legacy_cell_editor";
  state.editor.editorContentTab = ["map", "monster", "skill", "item", "event", "npc"].includes(editorSessionState.editorContentTab)
    ? editorSessionState.editorContentTab
    : "map";
  state.editor.workbenchFloor = Math.max(1, Number(editorSessionState.workbenchFloor || state.player.floor || 1));
  state.editor.workbenchProfileId = String(editorSessionState.workbenchProfileId || "");
  state.editor.workbenchAlgorithm = editorSessionState.workbenchAlgorithm === "block_modules_and_connectors" ? "block_modules_and_connectors" : "room_grid_chunks";
  state.editor.workbenchSeed = String(editorSessionState.workbenchSeed || "");
  state.editor.workbenchBatchCount = Math.max(1, Number(editorSessionState.workbenchBatchCount || 8));
  state.editor.workbenchBatchSummary = editorSessionState.workbenchBatchSummary ? JSON.parse(JSON.stringify(editorSessionState.workbenchBatchSummary)) : null;
  state.editor.workbenchProfileOverrides = editorSessionState.workbenchProfileOverrides && typeof editorSessionState.workbenchProfileOverrides === "object"
    ? JSON.parse(JSON.stringify(editorSessionState.workbenchProfileOverrides))
    : {};
  state.editor.selectedWorkbenchChunkId = String(editorSessionState.selectedWorkbenchChunkId || "");
  state.editor.workbenchChunkOverrides = editorSessionState.workbenchChunkOverrides && typeof editorSessionState.workbenchChunkOverrides === "object"
    ? JSON.parse(JSON.stringify(editorSessionState.workbenchChunkOverrides))
    : {};
  state.editor.workbenchCompareSnapshotLabel = String(editorSessionState.workbenchCompareSnapshotLabel || "");
  state.editor.workbenchCompareSnapshots = Array.isArray(editorSessionState.workbenchCompareSnapshots)
    ? JSON.parse(JSON.stringify(editorSessionState.workbenchCompareSnapshots.slice(0, 8)))
    : [];
  state.editor.selectedWorkbenchCompareSnapshotId = String(editorSessionState.selectedWorkbenchCompareSnapshotId || "");
  state.editor.editorTool = editorSessionState.editorTool || "wall";
  state.editor.editorCursor = editorSessionState.editorCursor || { x: state.map.start.x, y: state.map.start.y };
  state.editor.selectedCellTag = editorSessionState.selectedCellTag || "safe";
  state.editor.selectedBattleBackgroundId = editorSessionState.selectedBattleBackgroundId || "";
  state.editor.selectedRoomType = editorSessionState.selectedRoomType || "combat_room";
  state.editor.selectedFloorTextureId = normalizeTextureId(editorSessionState.selectedFloorTextureId, FLOOR_TEXTURE_IDS, DEFAULT_FLOOR_TEXTURE_ID);
  state.editor.selectedCeilingTextureId = normalizeTextureId(editorSessionState.selectedCeilingTextureId, CEILING_TEXTURE_IDS, DEFAULT_CEILING_TEXTURE_ID);
  state.editor.selectedWallTextureId = normalizeTextureId(editorSessionState.selectedWallTextureId, WALL_TEXTURE_IDS, DEFAULT_WALL_TEXTURE_ID);
  state.editor.activeRoomId = editorSessionState.activeRoomId || "";
  state.editor.metadataSelectionMode = editorSessionState.metadataSelectionMode === "lasso" ? "lasso" : "rect";
  state.editor.lassoSelectionAction = editorSessionState.lassoSelectionAction === "subtract" ? "subtract" : "add";
  state.editor.roomRangeStart = editorSessionState.roomRangeStart || null;
  state.editor.metadataRangeStart = editorSessionState.metadataRangeStart || null;
  state.editor.editorBrushDrag = editorSessionState.editorBrushDrag || null;
  state.editor.editorLassoSelectionDrag = editorSessionState.editorLassoSelectionDrag || null;
  state.editor.lastBrushSelection = editorSessionState.lastBrushSelection || null;
  state.editor.eventInspectorTool = EVENT_EDITOR_TO_PLACEMENT_KIND[editorSessionState.eventInspectorTool] ? editorSessionState.eventInspectorTool : "trap";
  state.editor.selectedPlacementOverrideId = editorSessionState.selectedPlacementOverrideId || "";
  state.editor.selectedNpcPlacementId = editorSessionState.selectedNpcPlacementId || "";
  state.editor.selectedEventDefinitionIds = {
    ...defaultEventSelection(editorEventSelectionDefaults()),
    ...(editorSessionState.selectedEventDefinitionIds || {}),
  };
  state.editor.selectedEventStepIndex = Math.max(0, Number(editorSessionState.selectedEventStepIndex || 0));
  state.editor.selectedClassDefinitionIndex = Math.min(Math.max(0, Number(editorSessionState.selectedClassDefinitionIndex || 0)), Math.max(0, classes.length - 1));
  state.editor.selectedQuestDefinitionId = questDefinitions[editorSessionState.selectedQuestDefinitionId] ? editorSessionState.selectedQuestDefinitionId : Object.keys(questDefinitions)[0] || "";
  state.editor.selectedMonsterDefinitionId = monsters[editorSessionState.selectedMonsterDefinitionId] ? editorSessionState.selectedMonsterDefinitionId : Object.keys(monsters)[0] || "";
  state.editor.selectedItemDefinitionId = items[editorSessionState.selectedItemDefinitionId] ? editorSessionState.selectedItemDefinitionId : Object.keys(items)[0] || "";
  state.editor.selectedVendorDefinitionId = vendors[editorSessionState.selectedVendorDefinitionId] ? editorSessionState.selectedVendorDefinitionId : Object.keys(vendors)[0] || "";
  state.editor.selectedVendorRotationIndex = Math.max(0, Number(editorSessionState.selectedVendorRotationIndex || 0));
  state.editor.selectedLootTableId = lootTableDefinitionIds().includes(editorSessionState.selectedLootTableId) ? editorSessionState.selectedLootTableId : lootTableDefinitionIds()[0] || "";
  state.editor.selectedLootTierIndex = Math.max(0, Number(editorSessionState.selectedLootTierIndex || 0));
  state.editor.selectedLootBonusIndex = Math.max(0, Number(editorSessionState.selectedLootBonusIndex || 0));
  state.editor.selectedCombatRewardProfileIndex = Math.max(0, Number(editorSessionState.selectedCombatRewardProfileIndex || 0));
  state.editor.selectedRarityDefinitionId = rarityDefinitions[editorSessionState.selectedRarityDefinitionId] ? editorSessionState.selectedRarityDefinitionId : Object.keys(rarityDefinitions)[0] || "";
  state.editor.selectedAffixDefinitionId = affixDefinitions[editorSessionState.selectedAffixDefinitionId] ? editorSessionState.selectedAffixDefinitionId : Object.keys(affixDefinitions)[0] || "";
  state.editor.selectedAffixPoolId = affixPoolDefinitions[editorSessionState.selectedAffixPoolId] ? editorSessionState.selectedAffixPoolId : Object.keys(affixPoolDefinitions)[0] || "";
  state.editor.sampleItemPreview = editorSessionState.sampleItemPreview ? JSON.parse(JSON.stringify(editorSessionState.sampleItemPreview)) : null;
  state.editor.selectedNpcDefinitionId = npcs[editorSessionState.selectedNpcDefinitionId] ? editorSessionState.selectedNpcDefinitionId : Object.keys(npcs)[0] || "";
  state.editor.selectedNpcQuestSeedIndex = Math.max(0, Number(editorSessionState.selectedNpcQuestSeedIndex || 0));
  state.editor.selectedNpcServiceIndex = Math.max(0, Number(editorSessionState.selectedNpcServiceIndex || 0));
  state.editor.selectedNpcDialogueStepIndex = Math.max(0, Number(editorSessionState.selectedNpcDialogueStepIndex || 0));
  state.editor.selectedNpcCustomPresetId = editorSessionState.selectedNpcCustomPresetId || "";
  state.editor.selectedNpcCustomPresetApplyMode = editorSessionState.selectedNpcCustomPresetApplyMode === "append" ? "append" : "replace";
  state.editor.selectedNpcCustomPresetConflictMode = editorSessionState.selectedNpcCustomPresetConflictMode === "keep_current" ? "keep_current" : "preset_wins";
  state.editor.selectedNpcCustomPresetServiceIndexes = Array.isArray(editorSessionState.selectedNpcCustomPresetServiceIndexes) ? editorSessionState.selectedNpcCustomPresetServiceIndexes.map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0) : [];
  state.editor.selectedNpcCustomPresetSeedIndexes = Array.isArray(editorSessionState.selectedNpcCustomPresetSeedIndexes) ? editorSessionState.selectedNpcCustomPresetSeedIndexes.map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0) : [];
  state.editor.selectedNpcCustomPresetDialogueStepSelections = editorSessionState.selectedNpcCustomPresetDialogueStepSelections && typeof editorSessionState.selectedNpcCustomPresetDialogueStepSelections === "object"
    ? Object.fromEntries(Object.entries(editorSessionState.selectedNpcCustomPresetDialogueStepSelections).map(([serviceIndex, indexes]) => [serviceIndex, (Array.isArray(indexes) ? indexes : []).map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0)]))
    : {};
  state.editor.selectedNpcCustomPresetDialogueChoiceSelections = editorSessionState.selectedNpcCustomPresetDialogueChoiceSelections && typeof editorSessionState.selectedNpcCustomPresetDialogueChoiceSelections === "object"
    ? Object.fromEntries(Object.entries(editorSessionState.selectedNpcCustomPresetDialogueChoiceSelections).map(([key, indexes]) => [key, (Array.isArray(indexes) ? indexes : []).map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0)]))
    : {};
  state.editor.selectedNpcCustomPresetDialogueBranchSelections = editorSessionState.selectedNpcCustomPresetDialogueBranchSelections && typeof editorSessionState.selectedNpcCustomPresetDialogueBranchSelections === "object"
    ? Object.fromEntries(Object.entries(editorSessionState.selectedNpcCustomPresetDialogueBranchSelections).map(([key, indexes]) => [key, (Array.isArray(indexes) ? indexes : []).map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0)]))
    : {};
  state.editor.selectedNpcCustomPresetServiceFieldSelections = editorSessionState.selectedNpcCustomPresetServiceFieldSelections && typeof editorSessionState.selectedNpcCustomPresetServiceFieldSelections === "object"
    ? Object.fromEntries(Object.entries(editorSessionState.selectedNpcCustomPresetServiceFieldSelections).map(([key, fields]) => [key, (Array.isArray(fields) ? fields : []).filter((value) => typeof value === "string" && value.trim())]))
    : {};
  state.editor.selectedNpcCustomPresetSeedFieldSelections = editorSessionState.selectedNpcCustomPresetSeedFieldSelections && typeof editorSessionState.selectedNpcCustomPresetSeedFieldSelections === "object"
    ? Object.fromEntries(Object.entries(editorSessionState.selectedNpcCustomPresetSeedFieldSelections).map(([key, fields]) => [key, (Array.isArray(fields) ? fields : []).filter((value) => typeof value === "string" && value.trim())]))
    : {};
  state.editor.selectedNpcCustomPresetMergePatchDraft = String(editorSessionState.selectedNpcCustomPresetMergePatchDraft || "");
  state.editor.selectedNpcCustomPresetPatchHistory = Array.isArray(editorSessionState.selectedNpcCustomPresetPatchHistory) ? JSON.parse(JSON.stringify(editorSessionState.selectedNpcCustomPresetPatchHistory.slice(0, 12))) : [];
  state.editor.selectedNpcCustomPresetRedoEntries = Array.isArray(editorSessionState.selectedNpcCustomPresetRedoEntries) ? JSON.parse(JSON.stringify(editorSessionState.selectedNpcCustomPresetRedoEntries.slice(0, 8))) : [];
  state.editor.selectedNpcCustomPresetRedoArchiveId = String(editorSessionState.selectedNpcCustomPresetRedoArchiveId || "");
  state.editor.selectedNpcCustomPresetRedoArchiveQuery = String(editorSessionState.selectedNpcCustomPresetRedoArchiveQuery || "");
  state.editor.selectedNpcCustomPresetPatchArchiveId = String(editorSessionState.selectedNpcCustomPresetPatchArchiveId || "");
  state.editor.selectedNpcCustomPresetPatchArchiveQuery = String(editorSessionState.selectedNpcCustomPresetPatchArchiveQuery || "");
  state.editor.densityOverlayMode = DENSITY_OVERLAY_MODES.has(editorSessionState.densityOverlayMode) ? editorSessionState.densityOverlayMode : "none";
  state.editor.eventExportHistory = Array.isArray(editorSessionState.eventExportHistory) ? JSON.parse(JSON.stringify(editorSessionState.eventExportHistory.slice(0, 12))) : [];
  state.editor.eventExportArchiveQuery = String(editorSessionState.eventExportArchiveQuery || "");
  state.editor.eventExportArchiveBatchShareLabel = String(editorSessionState.eventExportArchiveBatchShareLabel || "");
  state.editor.eventExportArchiveBatchShareLinkDraft = String(editorSessionState.eventExportArchiveBatchShareLinkDraft || "");
  state.editor.selectedEventExportArchiveId = String(editorSessionState.selectedEventExportArchiveId || "");
  state.editor.selectedEventExportArchiveBundleRowId = String(editorSessionState.selectedEventExportArchiveBundleRowId || "");
  state.editor.selectedEventExportArchiveBundleRowIds = Array.isArray(editorSessionState.selectedEventExportArchiveBundleRowIds) ? editorSessionState.selectedEventExportArchiveBundleRowIds.filter((value) => typeof value === "string" && value.trim()) : [];
  state.editor.selectedEventExportArchiveFieldKeys = Array.isArray(editorSessionState.selectedEventExportArchiveFieldKeys) ? editorSessionState.selectedEventExportArchiveFieldKeys.filter((value) => typeof value === "string" && value.trim()) : [];
  state.editor.selectedEventExportArchiveStepIds = Array.isArray(editorSessionState.selectedEventExportArchiveStepIds) ? editorSessionState.selectedEventExportArchiveStepIds.filter((value) => typeof value === "string" && value.trim()) : [];
  state.editor.selectedEventExportArchiveStepPartKeys = Array.isArray(editorSessionState.selectedEventExportArchiveStepPartKeys) ? editorSessionState.selectedEventExportArchiveStepPartKeys.filter((value) => typeof value === "string" && value.trim()) : [];
  state.editor.selectedEventExportArchiveStepItemKeys = Array.isArray(editorSessionState.selectedEventExportArchiveStepItemKeys) ? editorSessionState.selectedEventExportArchiveStepItemKeys.filter((value) => typeof value === "string" && value.trim()) : [];
  state.editor.eventBundleCompareEventId = String(editorSessionState.eventBundleCompareEventId || "");
  state.editor.eventBundlePatchArchiveQuery = String(editorSessionState.eventBundlePatchArchiveQuery || "");
  state.editor.eventBundlePatchArchiveEntryId = String(editorSessionState.eventBundlePatchArchiveEntryId || "");
  state.editor.eventBundlePatchDraft = String(editorSessionState.eventBundlePatchDraft || "");
  state.editor.eventBundleFocusPath = String(editorSessionState.eventBundleFocusPath || "");
  state.editor.eventBundlePatchHistory = Array.isArray(editorSessionState.eventBundlePatchHistory) ? JSON.parse(JSON.stringify(editorSessionState.eventBundlePatchHistory.slice(0, 12))) : [];
}

export function saveEditorProject(deps) {
  const project = buildEditorProject(deps);
  deps.localStorage.setItem(EDITOR_PROJECT_STORAGE_KEY, JSON.stringify(project));
  deps.addLog(`${project.name} 프로젝트를 저장했다.`);
  deps.render();
}

export function normalizeMapMetadata(map, deps) {
  const {
    normalizePlacement,
    normalizeTextureId,
    FLOOR_TEXTURE_IDS,
    CEILING_TEXTURE_IDS,
    WALL_TEXTURE_IDS,
    DEFAULT_FLOOR_TEXTURE_ID,
    DEFAULT_CEILING_TEXTURE_ID,
    DEFAULT_WALL_TEXTURE_ID,
  } = deps;
  map.rooms = Array.isArray(map.rooms) ? map.rooms : [];
  map.placements = Array.isArray(map.placements) ? map.placements.map((placement) => normalizePlacement(map, placement)) : [];
  map.lights = Array.isArray(map.lights) ? map.lights.map(normalizeMapLight).filter(Boolean) : [];
  for (const cell of map.cells || []) {
    if (!("roomId" in cell)) cell.roomId = null;
    if (!Array.isArray(cell.tags)) cell.tags = [];
    if (!("floorTexture" in cell)) cell.floorTexture = DEFAULT_FLOOR_TEXTURE_ID;
    if (!("ceilingTexture" in cell)) cell.ceilingTexture = DEFAULT_CEILING_TEXTURE_ID;
    if (!("wallTexture" in cell)) cell.wallTexture = DEFAULT_WALL_TEXTURE_ID;
    cell.floorTexture = normalizeTextureId(cell.floorTexture, FLOOR_TEXTURE_IDS, DEFAULT_FLOOR_TEXTURE_ID);
    cell.ceilingTexture = normalizeTextureId(cell.ceilingTexture, CEILING_TEXTURE_IDS, DEFAULT_CEILING_TEXTURE_ID);
    cell.wallTexture = normalizeTextureId(cell.wallTexture, WALL_TEXTURE_IDS, DEFAULT_WALL_TEXTURE_ID);
    if (!("floorMaterialId" in cell)) cell.floorMaterialId = cell.floorTexture;
    if (!("ceilingMaterialId" in cell)) cell.ceilingMaterialId = cell.ceilingTexture;
    if (!("wallMaterialId" in cell)) cell.wallMaterialId = cell.wallTexture;
    cell.floorMaterialId = normalizeTextureId(cell.floorMaterialId, FLOOR_TEXTURE_IDS, DEFAULT_FLOOR_TEXTURE_ID);
    cell.ceilingMaterialId = normalizeTextureId(cell.ceilingMaterialId, CEILING_TEXTURE_IDS, DEFAULT_CEILING_TEXTURE_ID);
    cell.wallMaterialId = normalizeTextureId(cell.wallMaterialId, WALL_TEXTURE_IDS, DEFAULT_WALL_TEXTURE_ID);
    if (!("battleBackgroundId" in cell)) cell.battleBackgroundId = null;
  }
}

export function normalizeMapLight(light) {
  if (!light || typeof light !== "object") return null;
  return {
    id: light.id || `light_${Math.random().toString(36).slice(2, 8)}`,
    type: light.type === "point" ? "point" : "point",
    x: Number(light.x || 0),
    y: Number(light.y || 0),
    height: Number.isFinite(Number(light.height)) ? Number(light.height) : 1.8,
    color: typeof light.color === "string" ? light.color : "#f0b46d",
    intensity: Number.isFinite(Number(light.intensity)) ? Number(light.intensity) : 0.72,
    range: Number.isFinite(Number(light.range)) ? Number(light.range) : 8,
  };
}

export function loadEditorProject(deps) {
  const raw = deps.localStorage.getItem(EDITOR_PROJECT_STORAGE_KEY);
  if (!raw) return deps.addLog("저장된 편집기 프로젝트가 없다."), deps.render();
  try {
    applyEditorProject(JSON.parse(raw), deps);
    deps.addLog("편집기 프로젝트를 불러왔다.");
  } catch (error) {
    deps.addLog(`편집기 프로젝트 불러오기 실패: ${error.message}`);
  }
  deps.render();
}

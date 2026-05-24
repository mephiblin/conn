export function createEditorRenderSnapshotBridge(deps = {}) {
  const {
    getState = () => ({}),
    validateMap = () => ({ issues: [], summary: { error: 0, warning: 0, info: 0 } }),
    buildProjectDashboardSnapshot = () => ({}),
    buildCompiledMapForRuntime = () => ({ ok: false }),
    selectedPreset = () => null,
    loadCustomPresets = () => [],
    activeBrushPreviewRect = () => null,
    currentBrushSelectionKeys = () => new Set(),
    activeBrushSelectionRect = () => null,
    activeEventEditorTool = () => "",
    compatibleEventDefinitions = () => [],
    activeEventDefinitionId = () => "",
    activeEventDefinition = () => null,
    activeEventStepDefinition = () => ({ step: null }),
    buildEditorEventSnapshot = () => ({}),
    eventPlacementsAtCursor = () => [],
    activePlacementOverride = () => null,
    resolvePlacementEvent = () => null,
    activeClassDefinitionIndex = () => 0,
    activeClassDefinition = () => null,
    activeQuestDefinitionId = () => "",
    activeQuestDefinition = () => null,
    activeMonsterDefinitionId = () => "",
    activeMonsterDefinition = () => null,
    activeSkillDefinitionId = () => "",
    activeSkillDefinition = () => null,
    activeItemDefinitionId = () => "",
    activeItemDefinition = () => null,
    activeVendorDefinitionId = () => "",
    activeVendorDefinition = () => null,
    activeVendorRotationDefinition = () => ({ rotation: null }),
    activeLootTableId = () => "",
    activeLootTableDefinition = () => null,
    activeLootTierDefinition = () => ({ tier: null }),
    activeLootBonusDefinition = () => ({ bonus: null }),
    activeCombatRewardProfile = () => ({ profile: null }),
    activeRarityDefinitionId = () => "",
    activeRarityDefinition = () => null,
    activeAffixDefinitionId = () => "",
    activeAffixDefinition = () => null,
    activeAffixPoolId = () => "",
    activeAffixPoolDefinition = () => null,
    activeNpcDefinitionId = () => "",
    activeNpcDefinition = () => null,
    activeNpcQuestSeed = () => null,
    activeNpcQuestSeedDefinition = () => ({ seed: null }),
    activeNpcServiceDefinition = () => ({ service: null }),
    activeNpcDialogueStepDefinition = () => ({ step: null }),
    npcPlacementsAtCursor = () => [],
    activeNpcPlacement = () => null,
    activeNpcPlacementDefinition = () => null,
    getCell = () => null,
    buildDensityOverlaySnapshot = () => ({ mode: "none" }),
    buildDensityHistogramSnapshot = () => ({ mode: "none", bandSummary: {}, topCells: [] }),
    buildRoomPlacementSummary = () => ({}),
    buildPlacementRecommendations = () => [],
    isRequiredEventPlacement = () => false,
    isRequiredNpcPlacement = () => false,
    buildEditorNpcSnapshot = () => ({}),
    buildEditorContentPanels = () => ({}),
    buildEditorNpcSupportPanels = () => ({}),
    buildEditorPlacementClassPanels = () => ({}),
    eventEditorToPlacementKind = {},
  } = deps;

  return function buildEditorRenderSnapshot() {
    const state = getState();
    const validationReport = validateMap(state.map);
    const projectDashboard = buildProjectDashboardSnapshot(state.floorMaps);
    const projectValidationReport = projectDashboard.projectValidationReport;
    const compiled = buildCompiledMapForRuntime(state.map, validationReport);
    const preset = selectedPreset();
    const customPresets = loadCustomPresets();
    const activeRoom = state.activeRoomId ? (state.map.rooms || []).find((room) => room.id === state.activeRoomId) : null;
    const previewRect = activeBrushPreviewRect();
    const selectedKeys = currentBrushSelectionKeys();
    const selectedRect = activeBrushSelectionRect();
    const selectedCount = selectedKeys.size;
    const eventTool = activeEventEditorTool();
    const eventPlacementKind = eventEditorToPlacementKind[eventTool];
    const compatiblePresets = compatibleEventDefinitions(eventPlacementKind);
    const eventDefId = activeEventDefinitionId();
    const eventDef = activeEventDefinition();
    const selectedEventStepDefState = activeEventStepDefinition(eventDef);
    const selectedEventStepDef = selectedEventStepDefState.step;
    const eventSnapshot = buildEditorEventSnapshot({
      eventDefId,
      eventDef,
      selectedEventStepDefState,
    });
    const cursorEventPlacements = eventPlacementsAtCursor();
    const selectedPlacement = activePlacementOverride();
    const selectedPlacementEvent = selectedPlacement ? resolvePlacementEvent(selectedPlacement) : null;
    const linkedPlacements = eventTool
      ? state.map.placements.filter((placement) => placement.kind === eventEditorToPlacementKind[eventTool] && (placement.interaction?.eventId || placement.refId) === eventDefId)
      : [];
    const linkedIssues = eventDefId
      ? validationReport.issues.filter((issue) => issue.message.includes(eventDefId))
      : [];
    const classDefIndex = activeClassDefinitionIndex();
    const classDef = activeClassDefinition();
    const questDefId = activeQuestDefinitionId();
    const questDef = activeQuestDefinition();
    const monsterDefId = activeMonsterDefinitionId();
    const monsterDef = activeMonsterDefinition();
    const skillDefId = activeSkillDefinitionId();
    const skillDef = activeSkillDefinition();
    const itemDefId = activeItemDefinitionId();
    const itemDef = activeItemDefinition();
    const vendorDefId = activeVendorDefinitionId();
    const vendorDef = activeVendorDefinition();
    const selectedVendorRotationState = activeVendorRotationDefinition(vendorDef);
    const selectedVendorRotation = selectedVendorRotationState.rotation;
    const lootTableId = activeLootTableId();
    const lootTableDef = activeLootTableDefinition();
    const selectedLootTierState = activeLootTierDefinition(lootTableDef);
    const selectedLootTier = selectedLootTierState.tier;
    const selectedLootBonusState = activeLootBonusDefinition(lootTableDef);
    const selectedLootBonus = selectedLootBonusState.bonus;
    const selectedCombatRewardProfileState = activeCombatRewardProfile();
    const selectedCombatRewardProfile = selectedCombatRewardProfileState.profile;
    const rarityDefId = activeRarityDefinitionId();
    const rarityDef = activeRarityDefinition();
    const affixDefId = activeAffixDefinitionId();
    const affixDef = activeAffixDefinition();
    const affixPoolId = activeAffixPoolId();
    const affixPoolDef = activeAffixPoolDefinition();
    const sampleItemPreview = state.editor.sampleItemPreview;
    const npcDefId = activeNpcDefinitionId();
    const npcDef = activeNpcDefinition();
    const npcQuestSeed = activeNpcQuestSeed(npcDef);
    const selectedNpcQuestSeedDefState = activeNpcQuestSeedDefinition(npcDef);
    const selectedNpcQuestSeedDef = selectedNpcQuestSeedDefState.seed;
    const selectedNpcQuestSeedRewards = selectedNpcQuestSeedDef?.rewards || {};
    const selectedNpcQuestSeedRuntime = selectedNpcQuestSeedDef?.id ? state.quest?.seeds?.[selectedNpcQuestSeedDef.id] || null : null;
    const linkedNpcQuestServices = (npcDef?.services || [])
      .map((service, index) => (service?.type === "quest" ? `${index} · ${service.label || service.type}` : ""))
      .filter(Boolean);
    const selectedNpcServiceDefState = activeNpcServiceDefinition(npcDef);
    const selectedNpcServiceDef = selectedNpcServiceDefState.service;
    const selectedNpcDialogueStepDefState = activeNpcDialogueStepDefinition(selectedNpcServiceDef);
    const selectedNpcDialogueStepDef = selectedNpcDialogueStepDefState.step;
    const cursorNpcPlacements = npcPlacementsAtCursor();
    const selectedNpcPlacement = activeNpcPlacement();
    const selectedNpcPlacementDef = activeNpcPlacementDefinition();
    const cursorCell = getCell(state.map, state.editorCursor.x, state.editorCursor.y);
    const densityOverlay = buildDensityOverlaySnapshot(state.map, state.densityOverlayMode);
    const recommendationRoomId = state.activeRoomId || cursorCell?.roomId || "";
    const densityHistogram = buildDensityHistogramSnapshot(
      state.map,
      densityOverlay,
      recommendationRoomId,
      state.selectedCellTag
    );
    const recommendationRoomSummary = buildRoomPlacementSummary(state.map, recommendationRoomId);
    const placementRecommendations = buildPlacementRecommendations(
      state.map,
      state.selectedRoomType,
      state.selectedCellTag,
      densityOverlay,
      recommendationRoomSummary
    );
    const selectedPlacementRequired = selectedPlacement ? isRequiredEventPlacement(state.map, selectedPlacement.id) : false;
    const selectedNpcPlacementRequired = selectedNpcPlacement ? isRequiredNpcPlacement(state.map, selectedNpcPlacement.id) : false;
    const npcSnapshot = buildEditorNpcSnapshot({
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
    });
    const contentPanels = buildEditorContentPanels({
      questDefId,
      questDef,
      monsterDefId,
      monsterDef,
      skillDefId,
      skillDef,
      itemDefId,
      itemDef,
      vendorDefId,
      vendorDef,
      selectedVendorRotation,
      selectedVendorRotationState,
      lootTableId,
      lootTableDef,
      selectedLootTier,
      selectedLootTierState,
      selectedLootBonus,
      selectedLootBonusState,
      selectedCombatRewardProfile,
      selectedCombatRewardProfileState,
      rarityDefId,
      rarityDef,
      affixDefId,
      affixDef,
      affixPoolId,
      affixPoolDef,
      sampleItemPreview,
    });
    const npcSupportPanels = buildEditorNpcSupportPanels({
      npcDefId,
      npcDef,
      npcQuestSeed,
      cursorNpcPlacements,
      selectedNpcPlacement,
      selectedNpcPlacementDef,
      selectedNpcPlacementRequired,
      state,
      customPresets,
      compiled,
    });
    const placementClassPanels = buildEditorPlacementClassPanels({
      selectedPlacement,
      selectedPlacementEvent,
      selectedPlacementRequired,
      cursorEventPlacements,
      classDef,
      classDefIndex,
      state,
    });
    return {
      state,
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
      ...npcSnapshot,
      ...contentPanels,
      ...npcSupportPanels,
      ...placementClassPanels,
    };
  };
}

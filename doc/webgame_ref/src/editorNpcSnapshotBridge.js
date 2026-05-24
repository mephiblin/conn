export function createEditorNpcSnapshotBridge(deps = {}) {
  const {
    getState = () => ({}),
    npcs = {},
    items = {},
    eventDefinitions = {},
    vendors = {},
    classes = [],
    encounters = {},
    loadNpcCustomPresets = () => [],
    buildNpcCustomPresetDiff = () => null,
    selectedNpcPresetServiceIndexes = () => [],
    selectedNpcPresetSeedIndexes = () => [],
    buildNpcPresetMergePatch = () => null,
    validateNpcPresetMergePatch = () => ({ summary: { error: 0, warning: 0, info: 0 }, issues: [] }),
    loadNpcPresetRedoArchive = () => [],
    npcPresetRedoArchiveMatchesQuery = () => true,
    buildNpcPresetRedoArchiveBatchCompare = () => null,
    loadNpcPresetPatchArchive = () => [],
    npcPresetPatchArchiveMatchesQuery = () => true,
    buildLineDiffText = () => "",
    buildNpcPresetApplyComparePreview = () => null,
    renderEditorQuestSeedPanel = () => "",
    renderEditorQuestSeedBody = () => "",
    renderEditorNpcServicePanel = () => "",
    renderEditorNpcCustomPresetSection = () => "",
    renderEditorNpcServiceEditorSection = () => "",
    questSeedJson = () => "",
    questSeedRewardItemsText = () => "",
    questRewardFlagValueType = () => "",
    questRewardFlagValueText = () => "",
    npcPresetRedoArchiveLine = () => "",
    npcPresetPatchArchiveLine = () => "",
    npcCustomPresetSummary = () => "",
    renderDiffBadgeHtml = () => "",
    buildDiffBadgeSpec = () => ({}),
    buildDiffCountScaleLabel = () => "",
    validationSummaryText = () => "",
    selectedNpcPresetServiceFieldNames = () => [],
    selectedNpcPresetDialogueStepIndexes = () => [],
    selectedNpcPresetDialogueChoiceIndexes = () => [],
    selectedNpcPresetDialogueBranchIndexes = () => [],
    selectedNpcPresetSeedFieldNames = () => [],
    npcPresetSideBySideDiffMarkup = () => "",
    npcPresetThreeWayPreviewMarkup = () => "",
    buildNpcPresetResolvedDialogueStep = () => null,
    buildNpcPresetResolvedServicePreview = () => null,
    buildNpcPresetResolvedSeedPreview = () => null,
    npcHookJson = () => "",
    npcServicePreviewText = () => "",
    npcServicePreviewList = () => "",
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  return function buildEditorNpcSnapshot({
    npcDefId = "",
    npcDef = null,
    npcQuestSeed = null,
    selectedNpcQuestSeedDefState = {},
    selectedNpcQuestSeedDef = null,
    selectedNpcQuestSeedRewards = {},
    selectedNpcQuestSeedRuntime = null,
    linkedNpcQuestServices = [],
    selectedNpcServiceDefState = {},
    selectedNpcServiceDef = null,
    selectedNpcDialogueStepDefState = {},
    selectedNpcDialogueStepDef = null,
  } = {}) {
    const state = getState();
    const npcCustomPresets = loadNpcCustomPresets();
    const selectedNpcCustomPresetId = state.editor?.selectedNpcCustomPresetId || npcCustomPresets[0]?.id || "";
    const selectedNpcCustomPreset = npcCustomPresets.find((entry) => entry.id === selectedNpcCustomPresetId) || npcCustomPresets[0] || null;
    const selectedNpcCustomPresetApplyMode = state.editor?.selectedNpcCustomPresetApplyMode === "append" ? "append" : "replace";
    const selectedNpcCustomPresetConflictMode = state.editor?.selectedNpcCustomPresetConflictMode === "keep_current" ? "keep_current" : "preset_wins";
    const npcCustomPresetDiff = buildNpcCustomPresetDiff(npcDef, selectedNpcCustomPreset);
    const selectedPresetServiceIndexes = selectedNpcCustomPreset ? selectedNpcPresetServiceIndexes(selectedNpcCustomPreset) : [];
    const selectedPresetSeedIndexes = selectedNpcCustomPreset ? selectedNpcPresetSeedIndexes(selectedNpcCustomPreset) : [];
    const npcCustomPresetMergePatch = selectedNpcCustomPreset
      ? buildNpcPresetMergePatch(
        npcDefId,
        selectedNpcCustomPreset,
        npcCustomPresetDiff,
        selectedNpcCustomPresetApplyMode,
        selectedNpcCustomPresetConflictMode,
      )
      : null;
    const defaultNpcCustomPresetMergePatchJson = JSON.stringify(npcCustomPresetMergePatch || {}, null, 2);
    const npcCustomPresetMergePatchDraft = String(state.editor?.selectedNpcCustomPresetMergePatchDraft || "");
    const npcCustomPresetMergePatchDraftValue = npcCustomPresetMergePatchDraft || defaultNpcCustomPresetMergePatchJson;
    let npcCustomPresetMergePatchPreview = npcCustomPresetMergePatch;
    try {
      if (npcCustomPresetMergePatchDraft.trim()) npcCustomPresetMergePatchPreview = JSON.parse(npcCustomPresetMergePatchDraft);
    } catch {
      npcCustomPresetMergePatchPreview = null;
    }
    const npcCustomPresetMergePatchValidation = validateNpcPresetMergePatch(npcCustomPresetMergePatchPreview);
    const npcPresetPatchHistory = Array.isArray(state.editor?.selectedNpcCustomPresetPatchHistory) ? state.editor.selectedNpcCustomPresetPatchHistory : [];
    const latestNpcPresetUndoEntry = npcPresetPatchHistory.find((entry) => entry?.undoSnapshot);
    const npcPresetRedoEntries = Array.isArray(state.editor?.selectedNpcCustomPresetRedoEntries) ? state.editor.selectedNpcCustomPresetRedoEntries : [];
    const latestNpcPresetRedoEntry = npcPresetRedoEntries[0] && typeof npcPresetRedoEntries[0] === "object"
      ? npcPresetRedoEntries[0]
      : null;
    const npcPresetRedoArchive = loadNpcPresetRedoArchive();
    const npcPresetRedoArchiveQuery = state.editor?.selectedNpcCustomPresetRedoArchiveQuery || "";
    const filteredNpcPresetRedoArchive = npcPresetRedoArchive.filter((entry) => npcPresetRedoArchiveMatchesQuery(entry, npcPresetRedoArchiveQuery));
    const npcPresetRedoArchiveBatchCompare = buildNpcPresetRedoArchiveBatchCompare(filteredNpcPresetRedoArchive);
    const selectedNpcPresetRedoArchiveId = filteredNpcPresetRedoArchive.some((entry) => entry.id === state.editor?.selectedNpcCustomPresetRedoArchiveId)
      ? state.editor.selectedNpcCustomPresetRedoArchiveId
      : (filteredNpcPresetRedoArchive[0]?.id || "");
    const selectedNpcPresetRedoArchiveEntry = filteredNpcPresetRedoArchive.find((entry) => entry.id === selectedNpcPresetRedoArchiveId) || null;
    const npcPresetPatchArchive = loadNpcPresetPatchArchive();
    const npcPresetPatchArchiveQuery = state.editor?.selectedNpcCustomPresetPatchArchiveQuery || "";
    const filteredNpcPresetPatchArchive = npcPresetPatchArchive.filter((entry) => npcPresetPatchArchiveMatchesQuery(entry, npcPresetPatchArchiveQuery));
    const selectedNpcPresetPatchArchiveId = filteredNpcPresetPatchArchive.some((entry) => entry.id === state.editor?.selectedNpcCustomPresetPatchArchiveId)
      ? state.editor.selectedNpcCustomPresetPatchArchiveId
      : (filteredNpcPresetPatchArchive[0]?.id || "");
    const selectedNpcPresetPatchArchiveEntry = filteredNpcPresetPatchArchive.find((entry) => entry.id === selectedNpcPresetPatchArchiveId) || null;
    let npcPresetPatchArchivePreview = null;
    try {
      npcPresetPatchArchivePreview = selectedNpcPresetPatchArchiveEntry?.patchDraft
        ? JSON.parse(selectedNpcPresetPatchArchiveEntry.patchDraft)
        : (selectedNpcPresetPatchArchiveEntry?.payload || null);
    } catch {
      npcPresetPatchArchivePreview = selectedNpcPresetPatchArchiveEntry?.payload || null;
    }
    const npcPresetPatchArchivePreviewText = JSON.stringify(npcPresetPatchArchivePreview || {}, null, 2);
    const npcPresetPatchArchiveLineDiffText = buildLineDiffText(
      npcCustomPresetMergePatchDraftValue || "",
      npcPresetPatchArchivePreviewText,
    );
    const npcPresetPatchArchiveCompare = {
      currentServiceCount: npcCustomPresetMergePatchPreview?.serviceCount || 0,
      currentQuestSeedCount: npcCustomPresetMergePatchPreview?.questSeedCount || 0,
      archiveServiceCount: npcPresetPatchArchivePreview?.serviceCount || 0,
      archiveQuestSeedCount: npcPresetPatchArchivePreview?.questSeedCount || 0,
    };
    const npcPresetApplyComparePreview = buildNpcPresetApplyComparePreview(npcDef, npcCustomPresetMergePatchPreview);

    const npcQuestEditorPanelMarkup = npcDef ? renderEditorQuestSeedPanel({
      subtitle: selectedNpcQuestSeedDef ? `${npcDefId} · ${escapeHtml(selectedNpcQuestSeedDef.title || selectedNpcQuestSeedDef.id || "unnamed")}` : `${npcDefId} · quest seed 없음`,
      bodyMarkup: renderEditorQuestSeedBody({
        npcDef,
        npcs,
        npcDefId,
        selectedNpcQuestSeedDef,
        selectedNpcQuestSeedDefState,
        selectedNpcQuestSeedRewards,
        selectedNpcQuestSeedRuntime,
        linkedNpcQuestServices,
        items,
        eventDefinitions,
        npcQuestSeed,
        escapeHtml,
        questSeedJson,
        questSeedRewardItemsText,
        questRewardFlagValueType,
        questRewardFlagValueText,
      }),
    }) : renderEditorQuestSeedPanel({
      subtitle: "npc 없음",
      bodyMarkup: `<div class="preset-inspector"><div class="muted">선택된 NPC definition을 찾지 못했다.</div></div>`,
    });

    const npcServicePanelMarkup = npcDef ? renderEditorNpcServicePanel({
      subtitle: selectedNpcServiceDef ? `${selectedNpcServiceDef.type} · ${escapeHtml(selectedNpcServiceDef.label || "unnamed")}` : `${npcDefId} · service 없음`,
      bodyMarkup: `
      <div class="preset-inspector">
        ${renderEditorNpcCustomPresetSection({
          npcDefId,
          npcCustomPresets,
          selectedNpcCustomPreset,
          selectedNpcCustomPresetApplyMode,
          selectedNpcCustomPresetConflictMode,
          npcCustomPresetDiff,
          npcCustomPresetMergePatch,
          npcCustomPresetMergePatchDraftValue,
          npcCustomPresetMergePatchValidation,
          npcCustomPresetMergePatchPreview,
          npcPresetApplyComparePreview,
          npcPresetPatchHistory,
          latestNpcPresetUndoEntry,
          latestNpcPresetRedoEntry,
          npcPresetRedoEntries,
          npcPresetRedoArchive,
          npcPresetRedoArchiveQuery,
          filteredNpcPresetRedoArchive,
          npcPresetRedoArchiveBatchCompare,
          selectedNpcPresetRedoArchiveId,
          selectedNpcPresetRedoArchiveEntry,
          npcPresetRedoArchiveLine,
          npcPresetPatchArchive,
          filteredNpcPresetPatchArchive,
          npcPresetPatchArchiveQuery,
          selectedNpcPresetPatchArchiveId,
          selectedNpcPresetPatchArchiveEntry,
          npcPresetPatchArchiveLine,
          npcPresetPatchArchiveCompare,
          npcPresetPatchArchivePreviewText,
          npcPresetPatchArchiveLineDiffText,
          selectedPresetServiceIndexes,
          selectedPresetSeedIndexes,
          selectedNpcCustomPresetConflictModeForPreview: selectedNpcCustomPresetConflictMode,
          selectedNpcCustomPresetRef: selectedNpcCustomPreset,
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
          escapeHtml,
        })}
        ${renderEditorNpcServiceEditorSection({
          npcDef,
          npcQuestSeed,
          selectedNpcServiceDef,
          selectedNpcServiceDefState,
          selectedNpcDialogueStepDef,
          selectedNpcDialogueStepDefState,
          vendors,
          classes,
          encounters,
          npcHookJson,
          npcServicePreviewText,
          npcServicePreviewList,
          escapeHtml,
        })}
      </div>
    `,
    }) : "";

    return {
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
    };
  };
}

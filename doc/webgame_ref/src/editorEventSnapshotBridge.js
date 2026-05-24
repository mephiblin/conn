export function createEditorEventSnapshotBridge(deps = {}) {
  const {
    getState = () => ({}),
    eventDefinitions = {},
    buildEventValidationSnapshot = () => null,
    buildEventGraphPreview = () => [],
    buildEventGraphCompactExport = () => null,
    buildEventGraphSummaryDiff = () => null,
    buildProjectEventGraphReviewBundle = () => ({ events: [], totals: { eventCount: 0 }, compactExport: null }),
    loadEventExportArchive = () => [],
    eventExportEntryMatchesQuery = () => true,
    buildEventExportArchiveBatchCompare = () => null,
    buildEventExportArchiveBatchCompareExport = () => null,
    buildEventExportArchiveBatchShareExport = () => null,
    buildEventExportArchiveBatchShareLink = () => "",
    buildEventExportSummaryDiff = () => null,
    buildEventArchiveRollbackPlan = () => null,
    buildLineDiffText = () => "",
    buildEventArchiveRestoreBadgeLookup = () => ({ fieldBadges: {}, stepBadges: {}, stepPartBadges: {}, stepItemBadges: {} }),
    buildEventBundleStructuralCompare = () => null,
    eventBundleCompareRowOptions = () => [],
    buildEventBundleComparePatch = () => null,
    applyEventBundleComparePatchPreview = () => null,
    loadEventBundlePatchArchive = () => [],
    eventBundlePatchEntryMatchesQuery = () => true,
    buildEventBundleVisualDiffRows = () => [],
    getValueAtPatchPath = () => null,
    currentEditorEventTestSession = () => null,
  } = deps;

  return function buildEditorEventSnapshot({
    eventDefId,
    eventDef,
    selectedEventStepDefState,
  } = {}) {
    const state = getState();
    const eventValidationSnapshot = eventDef ? buildEventValidationSnapshot(eventDefId, eventDef, selectedEventStepDefState?.index || 0) : null;
    const eventGraphPreview = eventDef ? buildEventGraphPreview(eventDef) : [];
    const eventGraphCompactExport = eventDef ? buildEventGraphCompactExport(eventDefId, eventDef) : null;
    const eventGraphSummaryDiff = eventDef ? buildEventGraphSummaryDiff(eventDefId, eventDef) : null;
    const eventProjectReviewBundle = buildProjectEventGraphReviewBundle(state.floorMaps);
    const eventExportHistory = Array.isArray(state.editor.eventExportHistory) ? state.editor.eventExportHistory : [];
    const eventExportArchive = loadEventExportArchive();
    const eventExportArchiveQuery = state.editor.eventExportArchiveQuery || "";
    const filteredEventExportHistory = eventExportHistory.filter((entry) => eventExportEntryMatchesQuery(entry, eventExportArchiveQuery));
    const filteredEventExportArchive = eventExportArchive.filter((entry) => eventExportEntryMatchesQuery(entry, eventExportArchiveQuery));
    const eventExportArchiveBatchCompare = buildEventExportArchiveBatchCompare(filteredEventExportArchive);
    const eventExportArchiveBatchCompareExport = buildEventExportArchiveBatchCompareExport(filteredEventExportArchive, eventExportArchiveQuery);
    const eventExportArchiveBatchShareLabel = String(state.editor.eventExportArchiveBatchShareLabel || "");
    const eventExportArchiveBatchShareExport = buildEventExportArchiveBatchShareExport(
      eventExportArchiveBatchCompareExport,
      eventExportArchiveBatchShareLabel,
      eventExportArchiveQuery,
    );
    const eventExportArchiveBatchShareLink = buildEventExportArchiveBatchShareLink(eventExportArchiveBatchShareExport);
    const eventExportArchiveBatchShareLinkDraft = String(state.editor.eventExportArchiveBatchShareLinkDraft || "");
    const selectedEventExportArchiveId = filteredEventExportArchive.some((entry) => entry.id === state.editor.selectedEventExportArchiveId)
      ? state.editor.selectedEventExportArchiveId
      : (filteredEventExportArchive[0]?.id || "");
    const selectedEventExportArchiveEntry = filteredEventExportArchive.find((entry) => entry.id === selectedEventExportArchiveId) || null;
    const selectedEventExportArchiveBundleRows = Array.isArray(selectedEventExportArchiveEntry?.payload?.events)
      ? selectedEventExportArchiveEntry.payload.events
      : [];
    const selectedEventExportArchiveBundleRowId = selectedEventExportArchiveBundleRows.some((row) => row?.eventId === state.editor.selectedEventExportArchiveBundleRowId)
      ? state.editor.selectedEventExportArchiveBundleRowId
      : (selectedEventExportArchiveBundleRows[0]?.eventId || "");
    const selectedEventExportArchiveBundleRow = selectedEventExportArchiveBundleRows.find((row) => row?.eventId === selectedEventExportArchiveBundleRowId) || null;
    const selectedEventExportArchiveBundleRowIds = Array.isArray(state.editor.selectedEventExportArchiveBundleRowIds)
      ? state.editor.selectedEventExportArchiveBundleRowIds.filter((eventId) => selectedEventExportArchiveBundleRows.some((row) => row?.eventId === eventId))
      : [];
    const effectiveEventExportArchiveBundleRowIds = selectedEventExportArchiveBundleRowIds.length
      ? selectedEventExportArchiveBundleRowIds
      : (selectedEventExportArchiveBundleRowId ? [selectedEventExportArchiveBundleRowId] : []);
    const selectedEventExportArchiveBundleRowsForRestore = effectiveEventExportArchiveBundleRowIds
      .map((eventId) => selectedEventExportArchiveBundleRows.find((row) => row?.eventId === eventId) || null)
      .filter(Boolean);
    const selectedEventExportArchiveTargetEventId = selectedEventExportArchiveEntry?.kind === "bundle"
      ? (selectedEventExportArchiveBundleRow?.eventId || selectedEventExportArchiveBundleRow?.compact?.eventId || "")
      : (selectedEventExportArchiveEntry?.targetId || selectedEventExportArchiveEntry?.payload?.eventId || "");
    const selectedEventExportArchiveCurrentTarget = selectedEventExportArchiveTargetEventId && eventDefinitions[selectedEventExportArchiveTargetEventId]
      ? buildEventGraphCompactExport(selectedEventExportArchiveTargetEventId, eventDefinitions[selectedEventExportArchiveTargetEventId])
      : null;
    const selectedEventExportArchiveRestoreTarget = selectedEventExportArchiveEntry?.kind === "bundle"
      ? (selectedEventExportArchiveBundleRow?.compact || null)
      : (selectedEventExportArchiveEntry?.payload || null);
    const selectedEventExportArchiveCompareDiff = buildEventExportSummaryDiff(
      selectedEventExportArchiveCurrentTarget,
      selectedEventExportArchiveRestoreTarget,
    );
    const selectedEventExportArchiveRollbackPlan = buildEventArchiveRollbackPlan(
      selectedEventExportArchiveCurrentTarget,
      selectedEventExportArchiveRestoreTarget,
    );
    const eventExportArchiveFieldOptions = selectedEventExportArchiveRollbackPlan
      ? selectedEventExportArchiveRollbackPlan.fieldChanges.map((entry) => entry.key)
      : [];
    const eventExportArchiveStepOptions = selectedEventExportArchiveRollbackPlan
      ? [
        ...selectedEventExportArchiveRollbackPlan.addedSteps.map((entry) => entry.id),
        ...selectedEventExportArchiveRollbackPlan.removedSteps.map((entry) => entry.id),
        ...selectedEventExportArchiveRollbackPlan.updatedSteps.map((entry) => entry.id),
      ]
      : [];
    const eventExportArchiveStepPartOptions = selectedEventExportArchiveRestoreTarget && Array.isArray(selectedEventExportArchiveRestoreTarget.steps)
      ? selectedEventExportArchiveRestoreTarget.steps
        .filter((step, index) => eventExportArchiveStepOptions.includes(step?.id || `step_${index}`))
        .flatMap((step, index) => {
          const id = step?.id || `step_${index}`;
          return ["branches", "choices", "effects"].map((part) => `${id}:${part}`);
        })
      : [];
    const eventExportArchiveStepItemOptions = selectedEventExportArchiveRestoreTarget && Array.isArray(selectedEventExportArchiveRestoreTarget.steps)
      ? selectedEventExportArchiveRestoreTarget.steps
        .filter((step, index) => eventExportArchiveStepOptions.includes(step?.id || `step_${index}`))
        .flatMap((step, index) => {
          const id = step?.id || `step_${index}`;
          const parts = [
            ["branches", Array.isArray(step?.branches) ? step.branches : []],
            ["choices", Array.isArray(step?.choices) ? step.choices : []],
            ["effects", Array.isArray(step?.effects) ? step.effects : []],
          ];
          return parts.flatMap(([part, entries]) => entries.map((_, itemIndex) => `${id}:${part}:${itemIndex}`));
        })
      : [];
    const selectedEventExportArchiveFieldKeys = (Array.isArray(state.editor.selectedEventExportArchiveFieldKeys) ? state.editor.selectedEventExportArchiveFieldKeys : [])
      .filter((key) => eventExportArchiveFieldOptions.includes(key));
    const effectiveEventExportArchiveFieldKeys = selectedEventExportArchiveFieldKeys.length
      ? selectedEventExportArchiveFieldKeys
      : eventExportArchiveFieldOptions;
    const selectedEventExportArchiveStepIds = (Array.isArray(state.editor.selectedEventExportArchiveStepIds) ? state.editor.selectedEventExportArchiveStepIds : [])
      .filter((id) => eventExportArchiveStepOptions.includes(id));
    const effectiveEventExportArchiveStepIds = selectedEventExportArchiveStepIds.length
      ? selectedEventExportArchiveStepIds
      : eventExportArchiveStepOptions;
    const selectedEventExportArchiveStepPartKeys = (Array.isArray(state.editor.selectedEventExportArchiveStepPartKeys) ? state.editor.selectedEventExportArchiveStepPartKeys : [])
      .filter((key) => eventExportArchiveStepPartOptions.includes(key));
    const effectiveEventExportArchiveStepPartKeys = selectedEventExportArchiveStepPartKeys.length
      ? selectedEventExportArchiveStepPartKeys
      : eventExportArchiveStepPartOptions;
    const selectedEventExportArchiveStepItemKeys = (Array.isArray(state.editor.selectedEventExportArchiveStepItemKeys) ? state.editor.selectedEventExportArchiveStepItemKeys : [])
      .filter((key) => eventExportArchiveStepItemOptions.includes(key));
    const effectiveEventExportArchiveStepItemKeys = selectedEventExportArchiveStepItemKeys.length
      ? selectedEventExportArchiveStepItemKeys
      : eventExportArchiveStepItemOptions;
    const selectedEventExportArchiveCurrentTargetJson = JSON.stringify(selectedEventExportArchiveCurrentTarget || {}, null, 2);
    const selectedEventExportArchiveRestoreTargetJson = JSON.stringify(selectedEventExportArchiveRestoreTarget || {}, null, 2);
    const selectedEventExportArchiveRollbackDiffText = selectedEventExportArchiveCurrentTarget && selectedEventExportArchiveRestoreTarget
      ? buildLineDiffText(selectedEventExportArchiveCurrentTargetJson, selectedEventExportArchiveRestoreTargetJson)
      : "";
    const eventExportArchiveRestoreBadgeLookup = buildEventArchiveRestoreBadgeLookup(
      selectedEventExportArchiveCurrentTarget,
      selectedEventExportArchiveRestoreTarget,
      selectedEventExportArchiveRollbackPlan,
    );
    const previousGraphArchive = eventDefId
      ? eventExportArchive.find((entry) => entry.kind === "graph" && entry.targetId === eventDefId)
      : null;
    const previousGraphArchiveDiff = buildEventExportSummaryDiff(eventGraphCompactExport, previousGraphArchive);
    const currentGraphExportJson = JSON.stringify(eventGraphCompactExport || {}, null, 2);
    const previousGraphArchivePayload = previousGraphArchive?.payload ? JSON.stringify(previousGraphArchive.payload, null, 2) : "";
    const eventGraphJsonDiffText = previousGraphArchivePayload
      ? buildLineDiffText(currentGraphExportJson, previousGraphArchivePayload)
      : "";
    const currentBundleSummary = {
      stepCount: eventProjectReviewBundle.events.reduce((sum, row) => sum + Number(row.summaryDiff.stepCount || 0), 0),
      branchCount: eventProjectReviewBundle.events.reduce((sum, row) => sum + Number(row.summaryDiff.branchCount || 0), 0),
      choiceCount: eventProjectReviewBundle.events.reduce((sum, row) => sum + Number(row.summaryDiff.choiceCount || 0), 0),
    };
    const previousBundleArchive = eventExportArchive.find((entry) => entry.kind === "bundle" && entry.targetId === `event_count_${eventProjectReviewBundle.totals.eventCount}`);
    const previousBundleArchiveDiff = buildEventExportSummaryDiff({ summary: currentBundleSummary }, previousBundleArchive);
    const currentBundleExportJson = JSON.stringify(eventProjectReviewBundle.compactExport || {}, null, 2);
    const previousBundleArchivePayload = previousBundleArchive?.payload ? JSON.stringify(previousBundleArchive.payload, null, 2) : "";
    const eventBundleJsonDiffText = previousBundleArchivePayload
      ? buildLineDiffText(currentBundleExportJson, previousBundleArchivePayload)
      : "";
    const eventBundleStructuralCompare = previousBundleArchive?.payload
      ? buildEventBundleStructuralCompare(eventProjectReviewBundle.compactExport, previousBundleArchive.payload)
      : null;
    const eventBundleCompareOptions = eventBundleStructuralCompare ? eventBundleCompareRowOptions(eventBundleStructuralCompare) : [];
    const defaultEventBundleCompareEventId = eventBundleCompareOptions[0]?.eventId || "";
    const selectedEventBundleCompareEventId = eventBundleCompareOptions.some((row) => row.eventId === state.editor.eventBundleCompareEventId)
      ? state.editor.eventBundleCompareEventId
      : defaultEventBundleCompareEventId;
    const selectedEventBundleCompareRow = eventBundleCompareOptions.find((row) => row.eventId === selectedEventBundleCompareEventId) || null;
    const selectedEventBundleCompareCurrent = selectedEventBundleCompareRow?.status === "removed"
      ? null
      : (selectedEventBundleCompareRow?.current || eventProjectReviewBundle.compactExport?.events?.find((row) => row.eventId === selectedEventBundleCompareEventId) || null);
    const selectedEventBundleComparePrevious = selectedEventBundleCompareRow?.status === "added"
      ? null
      : (selectedEventBundleCompareRow?.previous || previousBundleArchive?.payload?.events?.find((row) => row.eventId === selectedEventBundleCompareEventId) || null);
    const selectedEventBundleComparePatch = buildEventBundleComparePatch(
      selectedEventBundleCompareRow,
      selectedEventBundleComparePrevious,
      selectedEventBundleCompareCurrent,
    );
    const defaultEventBundlePatchJson = JSON.stringify(selectedEventBundleComparePatch || {}, null, 2);
    const eventBundlePatchDraft = String(state.editor.eventBundlePatchDraft || "");
    const eventBundlePatchDraftValue = eventBundlePatchDraft || defaultEventBundlePatchJson;
    let selectedEventBundleComparePatchPreview = selectedEventBundleComparePatch;
    try {
      if (eventBundlePatchDraft.trim()) selectedEventBundleComparePatchPreview = JSON.parse(eventBundlePatchDraft);
    } catch {
      selectedEventBundleComparePatchPreview = null;
    }
    const selectedEventBundleResolvedPreview = applyEventBundleComparePatchPreview(
      selectedEventBundleComparePatchPreview,
      selectedEventBundleComparePrevious,
      selectedEventBundleCompareCurrent,
    );
    const eventBundlePatchHistory = Array.isArray(state.editor.eventBundlePatchHistory) ? state.editor.eventBundlePatchHistory : [];
    const eventBundlePatchArchive = loadEventBundlePatchArchive();
    const eventBundlePatchArchiveQuery = state.editor.eventBundlePatchArchiveQuery || "";
    const filteredEventBundlePatchHistory = eventBundlePatchHistory.filter((entry) => eventBundlePatchEntryMatchesQuery(entry, eventBundlePatchArchiveQuery));
    const filteredEventBundlePatchArchive = eventBundlePatchArchive.filter((entry) => eventBundlePatchEntryMatchesQuery(entry, eventBundlePatchArchiveQuery));
    const selectedEventBundlePatchArchiveEntryId = filteredEventBundlePatchArchive.some((entry) => entry.id === state.editor.eventBundlePatchArchiveEntryId)
      ? state.editor.eventBundlePatchArchiveEntryId
      : (filteredEventBundlePatchArchive[0]?.id || "");
    const selectedEventBundlePatchArchiveEntry = filteredEventBundlePatchArchive.find((entry) => entry.id === selectedEventBundlePatchArchiveEntryId) || null;
    const selectedEventBundleVisualDiffRows = buildEventBundleVisualDiffRows(
      selectedEventBundleCompareRow,
      selectedEventBundleComparePatchPreview,
    );
    const eventBundleFocusOptions = selectedEventBundleVisualDiffRows.map((entry) => entry.path);
    const selectedEventBundleFocusPath = eventBundleFocusOptions.includes(state.editor.eventBundleFocusPath)
      ? state.editor.eventBundleFocusPath
      : (eventBundleFocusOptions[0] || "");
    const focusedEventBundlePreviousValue = selectedEventBundleFocusPath
      ? getValueAtPatchPath(selectedEventBundleComparePrevious, selectedEventBundleFocusPath)
      : null;
    const focusedEventBundleCurrentValue = selectedEventBundleFocusPath
      ? getValueAtPatchPath(selectedEventBundleCompareCurrent, selectedEventBundleFocusPath)
      : null;
    const focusedEventBundleResolvedValue = selectedEventBundleFocusPath
      ? getValueAtPatchPath(selectedEventBundleResolvedPreview, selectedEventBundleFocusPath)
      : null;
    const eventTestSession = currentEditorEventTestSession();
    const activeEditorEventTest = eventTestSession?.eventId === eventDefId ? eventTestSession : null;
    const activeEditorEventInteraction = state.interaction?.type === "event" && state.interaction.testSessionId === activeEditorEventTest?.id
      ? state.interaction
      : null;

    return {
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
    };
  };
}

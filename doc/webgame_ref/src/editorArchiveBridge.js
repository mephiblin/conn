export function createEditorArchiveBridge(deps = {}) {
  const {
    getState = () => ({}),
    localStorageObject = null,
    npcs = {},
    eventExportArchiveStorageKey = "",
    eventBundlePatchArchiveStorageKey = "",
    npcPresetPatchArchiveStorageKey = "",
    npcPresetRedoArchiveStorageKey = "",
  } = deps;

  function recordEventExportHistory(entry) {
    const state = getState();
    const history = Array.isArray(state.editor?.eventExportHistory) ? state.editor.eventExportHistory : [];
    const nextEntry = {
      id: `event_export_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      exportedAt: new Date().toISOString(),
      ...JSON.parse(JSON.stringify(entry || {})),
    };
    state.editor.eventExportHistory = [nextEntry, ...history].slice(0, 12);
    return nextEntry;
  }

  function recordEventBundlePatchHistory(entry) {
    const state = getState();
    const history = Array.isArray(state.editor?.eventBundlePatchHistory) ? state.editor.eventBundlePatchHistory : [];
    const nextEntry = {
      id: `event_bundle_patch_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      actedAt: new Date().toISOString(),
      ...JSON.parse(JSON.stringify(entry || {})),
    };
    state.editor.eventBundlePatchHistory = [nextEntry, ...history].slice(0, 12);
    return nextEntry;
  }

  function loadEventBundlePatchArchive() {
    try {
      const raw = localStorageObject?.getItem(eventBundlePatchArchiveStorageKey);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  function saveEventBundlePatchArchive(entries) {
    localStorageObject?.setItem(eventBundlePatchArchiveStorageKey, JSON.stringify((entries || []).slice(0, 24)));
  }

  function recordEventBundlePatchArchive(entry) {
    const archive = loadEventBundlePatchArchive();
    const nextEntry = {
      id: `event_bundle_patch_archive_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      archivedAt: new Date().toISOString(),
      ...JSON.parse(JSON.stringify(entry || {})),
    };
    saveEventBundlePatchArchive([nextEntry, ...archive]);
    return nextEntry;
  }

  function normalizeEventBundlePatchArchiveQuery(query = "") {
    return String(query || "").trim().toLowerCase();
  }

  function eventBundlePatchArchiveLine(entry) {
    return [
      entry?.action || "patch",
      entry?.eventId || "",
      entry?.label || "",
      entry?.archivedAt || entry?.actedAt || "",
    ].filter(Boolean).join(" · ");
  }

  function eventBundlePatchEntryMatchesQuery(entry, query = getState().editor?.eventBundlePatchArchiveQuery || "") {
    const normalizedQuery = normalizeEventBundlePatchArchiveQuery(query);
    if (!normalizedQuery) return true;
    const haystack = [
      entry?.action || "",
      entry?.eventId || "",
      entry?.label || "",
      entry?.archivedAt || "",
      entry?.actedAt || "",
      eventBundlePatchArchiveLine(entry),
    ].join(" ").toLowerCase();
    return haystack.includes(normalizedQuery);
  }

  function recordNpcPresetPatchHistory(entry) {
    const state = getState();
    const history = Array.isArray(state.editor?.selectedNpcCustomPresetPatchHistory) ? state.editor.selectedNpcCustomPresetPatchHistory : [];
    const nextEntry = {
      id: `npc_patch_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      actedAt: new Date().toISOString(),
      ...JSON.parse(JSON.stringify(entry || {})),
    };
    state.editor.selectedNpcCustomPresetPatchHistory = [nextEntry, ...history].slice(0, 12);
    return nextEntry;
  }

  function loadNpcPresetPatchArchive() {
    try {
      const raw = localStorageObject?.getItem(npcPresetPatchArchiveStorageKey);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  function saveNpcPresetPatchArchive(entries) {
    localStorageObject?.setItem(npcPresetPatchArchiveStorageKey, JSON.stringify((entries || []).slice(0, 24)));
  }

  function captureNpcPresetUndoSnapshot(npcId) {
    const state = getState();
    return {
      npcId: npcId || "",
      npcDefinition: npcId && npcs[npcId] ? JSON.parse(JSON.stringify(npcs[npcId])) : null,
      patchDraft: String(state.editor?.selectedNpcCustomPresetMergePatchDraft || ""),
      patchArchiveEntries: JSON.parse(JSON.stringify(loadNpcPresetPatchArchive())),
      selectedPatchArchiveId: String(state.editor?.selectedNpcCustomPresetPatchArchiveId || ""),
    };
  }

  function restoreNpcDefinitionSnapshot(npcId, snapshot) {
    if (!npcId || !npcs[npcId] || !snapshot || typeof snapshot !== "object") return false;
    const target = npcs[npcId];
    Object.keys(target).forEach((key) => delete target[key]);
    Object.assign(target, JSON.parse(JSON.stringify(snapshot)));
    return true;
  }

  function applyNpcPresetUndoSnapshot(snapshot) {
    const state = getState();
    if (!snapshot || typeof snapshot !== "object") return false;
    let restored = false;
    if (snapshot.npcId && snapshot.npcDefinition) {
      restored = restoreNpcDefinitionSnapshot(snapshot.npcId, snapshot.npcDefinition) || restored;
    }
    if (Array.isArray(snapshot.patchArchiveEntries)) {
      saveNpcPresetPatchArchive(snapshot.patchArchiveEntries);
      restored = true;
    }
    state.editor.selectedNpcCustomPresetMergePatchDraft = String(snapshot.patchDraft || "");
    state.editor.selectedNpcCustomPresetPatchArchiveId = String(snapshot.selectedPatchArchiveId || "");
    return restored;
  }

  function clearNpcPresetRedoEntry() {
    getState().editor.selectedNpcCustomPresetRedoEntries = [];
  }

  function loadNpcPresetRedoArchive() {
    try {
      const raw = localStorageObject?.getItem(npcPresetRedoArchiveStorageKey);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  function saveNpcPresetRedoArchive(entries) {
    localStorageObject?.setItem(npcPresetRedoArchiveStorageKey, JSON.stringify((entries || []).slice(0, 24)));
  }

  function recordNpcPresetRedoArchive(entry) {
    const archive = loadNpcPresetRedoArchive();
    const nextEntry = {
      id: `npc_redo_archive_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      archivedAt: new Date().toISOString(),
      ...JSON.parse(JSON.stringify(entry || {})),
    };
    saveNpcPresetRedoArchive([nextEntry, ...archive]);
    return nextEntry;
  }

  function pushNpcPresetRedoEntry(entry) {
    const state = getState();
    const stack = Array.isArray(state.editor?.selectedNpcCustomPresetRedoEntries) ? state.editor.selectedNpcCustomPresetRedoEntries : [];
    const nextEntry = JSON.parse(JSON.stringify(entry || {}));
    state.editor.selectedNpcCustomPresetRedoEntries = [nextEntry, ...stack].slice(0, 8);
    recordNpcPresetRedoArchive(nextEntry);
    return nextEntry;
  }

  function npcPresetRedoArchiveLine(entry) {
    return [
      entry?.action || "redo",
      entry?.npcId || "",
      entry?.label || "",
      entry?.archivedAt || "",
    ].filter(Boolean).join(" · ");
  }

  function normalizeNpcPresetRedoArchiveQuery(query = "") {
    return String(query || "").trim().toLowerCase();
  }

  function npcPresetRedoArchiveMatchesQuery(entry, query = getState().editor?.selectedNpcCustomPresetRedoArchiveQuery || "") {
    const normalizedQuery = normalizeNpcPresetRedoArchiveQuery(query);
    if (!normalizedQuery) return true;
    const haystack = [
      entry?.action || "",
      entry?.npcId || "",
      entry?.label || "",
      entry?.archivedAt || "",
      npcPresetRedoArchiveLine(entry),
    ].join(" ").toLowerCase();
    return haystack.includes(normalizedQuery);
  }

  function npcPresetRedoSnapshotSummary(entry) {
    const snapshot = entry?.redoSnapshot || {};
    const npcDefinitions = snapshot.contentDefinitions?.npcs || snapshot.npcs || {};
    const npcId = entry?.npcId || Object.keys(npcDefinitions)[0] || "";
    const npcDefinition = npcDefinitions[npcId] || {};
    return {
      serviceCount: Array.isArray(npcDefinition.services) ? npcDefinition.services.length : 0,
      questSeedCount: Array.isArray(npcDefinition.questSeeds) ? npcDefinition.questSeeds.length : 0,
    };
  }

  function buildNpcPresetRedoArchiveBatchCompare(entries = []) {
    const filtered = Array.isArray(entries) ? entries.filter(Boolean) : [];
    if (!filtered.length) return null;
    const actionCounts = {};
    const groupedByNpc = new Map();
    filtered.forEach((entry) => {
      const action = entry?.action || "redo";
      const npcId = entry?.npcId || "(npc 없음)";
      actionCounts[action] = (actionCounts[action] || 0) + 1;
      if (!groupedByNpc.has(npcId)) groupedByNpc.set(npcId, []);
      groupedByNpc.get(npcId).push(entry);
    });
    const npcComparisons = [...groupedByNpc.entries()].map(([npcId, npcEntries]) => {
      const sorted = [...npcEntries].sort((a, b) => String(a.archivedAt || "").localeCompare(String(b.archivedAt || "")));
      const first = sorted[0] || null;
      const last = sorted[sorted.length - 1] || null;
      const firstSummary = npcPresetRedoSnapshotSummary(first);
      const lastSummary = npcPresetRedoSnapshotSummary(last);
      return {
        npcId,
        count: sorted.length,
        first,
        last,
        serviceDelta: lastSummary.serviceCount - firstSummary.serviceCount,
        questSeedDelta: lastSummary.questSeedCount - firstSummary.questSeedCount,
        lastServiceCount: lastSummary.serviceCount,
        lastQuestSeedCount: lastSummary.questSeedCount,
      };
    }).sort((a, b) => b.count - a.count || a.npcId.localeCompare(b.npcId));
    return {
      totalCount: filtered.length,
      actionCounts,
      npcComparisons,
    };
  }

  function deleteNpcPresetRedoArchiveEntry(entryId) {
    if (!entryId) return false;
    const archive = loadNpcPresetRedoArchive();
    const nextArchive = archive.filter((entry) => entry?.id !== entryId);
    if (nextArchive.length === archive.length) return false;
    saveNpcPresetRedoArchive(nextArchive);
    return true;
  }

  function recordNpcPresetPatchArchive(entry) {
    const archive = loadNpcPresetPatchArchive();
    const nextEntry = {
      id: `npc_patch_archive_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      archivedAt: new Date().toISOString(),
      ...JSON.parse(JSON.stringify(entry || {})),
    };
    saveNpcPresetPatchArchive([nextEntry, ...archive]);
    return nextEntry;
  }

  function npcPresetPatchArchiveLine(entry) {
    return [
      entry?.action || "patch",
      entry?.npcId || "",
      entry?.label || "",
      entry?.archivedAt || "",
    ].filter(Boolean).join(" · ");
  }

  function normalizeNpcPresetPatchArchiveQuery(query = "") {
    return String(query || "").trim().toLowerCase();
  }

  function npcPresetPatchArchiveMatchesQuery(entry, query = getState().editor?.selectedNpcCustomPresetPatchArchiveQuery || "") {
    const normalizedQuery = normalizeNpcPresetPatchArchiveQuery(query);
    if (!normalizedQuery) return true;
    const haystack = [
      entry?.action || "",
      entry?.npcId || "",
      entry?.label || "",
      entry?.archivedAt || "",
      npcPresetPatchArchiveLine(entry),
    ].join(" ").toLowerCase();
    return haystack.includes(normalizedQuery);
  }

  function deleteNpcPresetPatchArchiveEntry(entryId) {
    if (!entryId) return false;
    const archive = loadNpcPresetPatchArchive();
    const nextArchive = archive.filter((entry) => entry?.id !== entryId);
    if (nextArchive.length === archive.length) return false;
    saveNpcPresetPatchArchive(nextArchive);
    return true;
  }

  function deleteNpcPresetPatchArchiveEntries(entryIds = []) {
    const ids = [...new Set((Array.isArray(entryIds) ? entryIds : []).filter(Boolean))];
    if (!ids.length) return 0;
    const archive = loadNpcPresetPatchArchive();
    const nextArchive = archive.filter((entry) => !ids.includes(entry?.id));
    const deletedCount = archive.length - nextArchive.length;
    if (!deletedCount) return 0;
    saveNpcPresetPatchArchive(nextArchive);
    return deletedCount;
  }

  function loadEventExportArchive() {
    try {
      const raw = localStorageObject?.getItem(eventExportArchiveStorageKey);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  function saveEventExportArchive(entries) {
    localStorageObject?.setItem(eventExportArchiveStorageKey, JSON.stringify((entries || []).slice(0, 24)));
  }

  function recordEventExportArchive(entry) {
    const archive = loadEventExportArchive();
    const nextEntry = {
      id: `event_export_archive_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      archivedAt: new Date().toISOString(),
      ...JSON.parse(JSON.stringify(entry || {})),
    };
    saveEventExportArchive([nextEntry, ...archive]);
    return nextEntry;
  }

  function deleteEventExportArchiveEntry(entryId) {
    if (!entryId) return false;
    const archive = loadEventExportArchive();
    const nextArchive = archive.filter((entry) => entry?.id !== entryId);
    if (nextArchive.length === archive.length) return false;
    saveEventExportArchive(nextArchive);
    return true;
  }

  function eventExportArchiveLine(entry) {
    const counts = entry?.summary
      ? `step ${entry.summary.stepCount || 0} · branch ${entry.summary.branchCount || 0} · choice ${entry.summary.choiceCount || 0}`
      : "";
    return [
      entry?.label || "archive",
      entry?.mode || "",
      entry?.eventId || "",
      counts,
      entry?.archivedAt || "",
    ].filter(Boolean).join(" · ");
  }

  function buildEventExportSummaryDiff(currentEntry, previousEntry) {
    const currentSummary = currentEntry?.summary || {};
    const previousSummary = previousEntry?.summary || {};
    return {
      stepDelta: Number(currentSummary.stepCount || 0) - Number(previousSummary.stepCount || 0),
      branchDelta: Number(currentSummary.branchCount || 0) - Number(previousSummary.branchCount || 0),
      choiceDelta: Number(currentSummary.choiceCount || 0) - Number(previousSummary.choiceCount || 0),
      effectDelta: Number(currentSummary.effectCount || 0) - Number(previousSummary.effectCount || 0),
      placementDelta: Number(currentSummary.linkedPlacementCount || 0) - Number(previousSummary.linkedPlacementCount || 0),
    };
  }

  return {
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
  };
}

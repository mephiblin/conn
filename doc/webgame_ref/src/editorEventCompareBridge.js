export function createEditorEventCompareBridge(deps = {}) {
  const {
    getState = () => ({}),
    buildDiffBadgeSpec = () => ({}),
    buildEventArchiveRollbackPlan = () => null,
    eventDefinitions = {},
    updateEventDefinition = () => {},
    eventExportArchiveLine = () => "",
  } = deps;

  function buildEventArchiveRestoreBadgeLookup(currentTarget, restoreTarget, rollbackPlan = null) {
    const fieldBadges = {};
    const stepBadges = {};
    const stepPartBadges = {};
    const stepItemBadges = {};
    if (!currentTarget || !restoreTarget) {
      return { fieldBadges, stepBadges, stepPartBadges, stepItemBadges };
    }
    const plan = rollbackPlan || buildEventArchiveRollbackPlan(currentTarget, restoreTarget);
    (plan?.fieldChanges || []).forEach((entry) => {
      fieldBadges[entry.key] = buildDiffBadgeSpec("changed");
    });
    (plan?.addedSteps || []).forEach((entry) => {
      stepBadges[entry.id] = buildDiffBadgeSpec("add");
    });
    (plan?.removedSteps || []).forEach((entry) => {
      stepBadges[entry.id] = buildDiffBadgeSpec("remove", "warning");
    });
    (plan?.updatedSteps || []).forEach((entry) => {
      stepBadges[entry.id] = buildDiffBadgeSpec(`update ${entry.changedFields.length}`);
    });
    const currentSteps = Array.isArray(currentTarget?.steps) ? currentTarget.steps : [];
    const restoreSteps = Array.isArray(restoreTarget?.steps) ? restoreTarget.steps : [];
    const currentStepMap = new Map(currentSteps.map((step, index) => [step?.id || `step_${index}`, step]));
    const restoreStepMap = new Map(restoreSteps.map((step, index) => [step?.id || `step_${index}`, step]));
    [...new Set([...currentStepMap.keys(), ...restoreStepMap.keys()])].forEach((stepId) => {
      const currentStep = currentStepMap.get(stepId) || {};
      const restoreStep = restoreStepMap.get(stepId) || {};
      ["branches", "choices", "effects"].forEach((part) => {
        const key = `${stepId}:${part}`;
        const currentEntries = Array.isArray(currentStep?.[part]) ? currentStep[part] : [];
        const restoreEntries = Array.isArray(restoreStep?.[part]) ? restoreStep[part] : [];
        if (!currentEntries.length && !restoreEntries.length) return;
        if (!currentEntries.length && restoreEntries.length) {
          stepPartBadges[key] = buildDiffBadgeSpec(`add ${restoreEntries.length}`);
        } else if (currentEntries.length && !restoreEntries.length) {
          stepPartBadges[key] = buildDiffBadgeSpec(`remove ${currentEntries.length}`, "warning");
        } else if (JSON.stringify(currentEntries) !== JSON.stringify(restoreEntries)) {
          const delta = restoreEntries.length - currentEntries.length;
          stepPartBadges[key] = buildDiffBadgeSpec(delta ? `change ${delta >= 0 ? "+" : ""}${delta}` : "change");
        } else {
          stepPartBadges[key] = buildDiffBadgeSpec("same", "muted");
        }
        const maxLength = Math.max(currentEntries.length, restoreEntries.length);
        for (let itemIndex = 0; itemIndex < maxLength; itemIndex += 1) {
          const itemKey = `${stepId}:${part}:${itemIndex}`;
          const currentEntry = itemIndex < currentEntries.length ? currentEntries[itemIndex] : undefined;
          const restoreEntry = itemIndex < restoreEntries.length ? restoreEntries[itemIndex] : undefined;
          if (currentEntry === undefined && restoreEntry === undefined) continue;
          if (currentEntry === undefined) {
            stepItemBadges[itemKey] = buildDiffBadgeSpec("add");
          } else if (restoreEntry === undefined) {
            stepItemBadges[itemKey] = buildDiffBadgeSpec("remove", "warning");
          } else if (JSON.stringify(currentEntry) !== JSON.stringify(restoreEntry)) {
            stepItemBadges[itemKey] = buildDiffBadgeSpec("change");
          } else {
            stepItemBadges[itemKey] = buildDiffBadgeSpec("same", "muted");
          }
        }
      });
    });
    return { fieldBadges, stepBadges, stepPartBadges, stepItemBadges };
  }

  function eventBundleCompareRowOptions(compare) {
    if (!compare) return [];
    return [
      ...compare.changed.map((row) => ({ eventId: row.eventId, status: "changed", current: row.current, previous: row.previous, detail: row.diffs.join(" · ") })),
      ...compare.added.map((eventId) => ({ eventId, status: "added", current: null, previous: null, detail: "added" })),
      ...compare.removed.map((eventId) => ({ eventId, status: "removed", current: null, previous: null, detail: "removed" })),
    ];
  }

  function buildEventBundleComparePatch(row, previousRow, currentRow) {
    if (!row) return null;
    if (row.status === "added") {
      return {
        kind: "eventBundleComparePatch",
        status: "added",
        eventId: row.eventId,
        current: JSON.parse(JSON.stringify(currentRow || {})),
      };
    }
    if (row.status === "removed") {
      return {
        kind: "eventBundleComparePatch",
        status: "removed",
        eventId: row.eventId,
        previous: JSON.parse(JSON.stringify(previousRow || {})),
      };
    }
    const changedEntries = [];
    function visit(prevValue, nextValue, path = "") {
      if (JSON.stringify(prevValue ?? null) === JSON.stringify(nextValue ?? null)) return;
      const prevIsObject = prevValue && typeof prevValue === "object";
      const nextIsObject = nextValue && typeof nextValue === "object";
      const prevIsArray = Array.isArray(prevValue);
      const nextIsArray = Array.isArray(nextValue);
      if ((prevIsObject || nextIsObject) && (prevIsArray === nextIsArray)) {
        const keys = prevIsArray || nextIsArray
          ? Array.from({ length: Math.max(prevValue?.length || 0, nextValue?.length || 0) }, (_, index) => index)
          : [...new Set([...Object.keys(prevValue || {}), ...Object.keys(nextValue || {})])];
        let nestedChanged = false;
        keys.forEach((key) => {
          const nextPath = prevIsArray || nextIsArray ? `${path}[${key}]` : (path ? `${path}.${key}` : String(key));
          const beforeLength = changedEntries.length;
          visit(prevValue?.[key], nextValue?.[key], nextPath);
          if (changedEntries.length > beforeLength) nestedChanged = true;
        });
        if (nestedChanged) return;
      }
      changedEntries.push({
        path: path || "(root)",
        previous: JSON.parse(JSON.stringify(prevValue ?? null)),
        current: JSON.parse(JSON.stringify(nextValue ?? null)),
      });
    }
    visit(previousRow || null, currentRow || null, "");
    return {
      kind: "eventBundleComparePatch",
      status: "changed",
      eventId: row.eventId,
      changedFields: changedEntries.map((entry) => entry.path),
      changes: changedEntries,
    };
  }

  function parsePatchPathTokens(path = "") {
    if (!path || path === "(root)") return [];
    const tokens = [];
    String(path).split(".").forEach((part) => {
      const baseMatch = part.match(/^[^\[]+/);
      if (baseMatch) tokens.push(baseMatch[0]);
      const bracketMatches = part.match(/\[(\d+)\]/g) || [];
      bracketMatches.forEach((match) => {
        tokens.push(Number(match.slice(1, -1)));
      });
    });
    return tokens;
  }

  function setValueAtPatchPath(target, path, value) {
    const tokens = parsePatchPathTokens(path);
    if (!tokens.length) return JSON.parse(JSON.stringify(value ?? null));
    let cursor = target;
    for (let index = 0; index < tokens.length - 1; index += 1) {
      const token = tokens[index];
      const nextToken = tokens[index + 1];
      if (cursor[token] == null || typeof cursor[token] !== "object") {
        cursor[token] = typeof nextToken === "number" ? [] : {};
      }
      cursor = cursor[token];
    }
    cursor[tokens[tokens.length - 1]] = JSON.parse(JSON.stringify(value ?? null));
    return target;
  }

  function getValueAtPatchPath(target, path) {
    const tokens = parsePatchPathTokens(path);
    if (!tokens.length) return JSON.parse(JSON.stringify(target ?? null));
    let cursor = target;
    for (const token of tokens) {
      if (cursor == null) return null;
      cursor = cursor[token];
    }
    return JSON.parse(JSON.stringify(cursor ?? null));
  }

  function applyEventBundleComparePatchPreview(patch, previousRow, currentRow) {
    if (!patch || patch.kind !== "eventBundleComparePatch") return null;
    if (patch.status === "added") return JSON.parse(JSON.stringify(patch.current || currentRow || {}));
    if (patch.status === "removed") return null;
    const base = JSON.parse(JSON.stringify(previousRow || {}));
    const changes = Array.isArray(patch.changes) ? patch.changes : [];
    if (!changes.length) return base;
    return changes.reduce((acc, entry) => setValueAtPatchPath(acc, entry.path, entry.current), base);
  }

  function applyCompactEventRowToDefinition(eventId, compactRow) {
    if (!eventId || !compactRow || typeof compactRow !== "object") return false;
    const compact = compactRow.compact && typeof compactRow.compact === "object" ? compactRow.compact : compactRow;
    const nextSteps = Array.isArray(compact.steps) ? compact.steps : [];
    if (!eventDefinitions[eventId]) {
      eventDefinitions[eventId] = {
        name: compact.name || eventId,
        type: compact.type || compact.source?.type || "",
        interaction: compact.interaction || compact.source?.interaction || "interact",
        entryStepId: compact.entryStepId || nextSteps[0]?.id || "",
        effects: [],
        steps: [],
      };
    }
    updateEventDefinition(eventId, (event) => {
      event.name = compact.name || event.name || eventId;
      if (compact.type || compact.source?.type) event.type = compact.type || compact.source?.type || "";
      event.interaction = compact.interaction || compact.source?.interaction || event.interaction || "interact";
      if (compact.entryStepId) event.entryStepId = compact.entryStepId;
      event.effects = JSON.parse(JSON.stringify(Array.isArray(compact.effects) ? compact.effects : []));
      event.steps = nextSteps.map((step, index) => ({
        id: step?.id || `step_${index}`,
        title: step?.title || "",
        text: step?.text || "",
        nextStepId: step?.nextStepId || "",
        branches: JSON.parse(JSON.stringify(step?.branches || [])),
        choices: JSON.parse(JSON.stringify(step?.choices || [])),
        effects: JSON.parse(JSON.stringify(step?.effects || [])),
      }));
    });
    return true;
  }

  function applyResolvedEventBundleRow(eventId, resolvedRow) {
    if (!eventId || !resolvedRow || typeof resolvedRow !== "object") return { ok: false, reason: "invalid_row" };
    if (resolvedRow.status === "removed") return { ok: false, reason: "removed_unsupported" };
    if (!resolvedRow.compact || typeof resolvedRow.compact !== "object") return { ok: false, reason: "missing_compact" };
    const applied = applyCompactEventRowToDefinition(eventId, resolvedRow);
    return applied ? { ok: true } : { ok: false, reason: "apply_failed" };
  }

  function applyPartialCompactEventRowToDefinition(eventId, compactRow, options = {}) {
    if (!eventId || !compactRow || typeof compactRow !== "object") return false;
    const compact = compactRow.compact && typeof compactRow.compact === "object" ? compactRow.compact : compactRow;
    const selectedFieldKeys = new Set(Array.isArray(options.fieldKeys) ? options.fieldKeys : []);
    const selectedStepIds = new Set(Array.isArray(options.stepIds) ? options.stepIds : []);
    const selectedStepPartKeys = new Set(Array.isArray(options.stepPartKeys) ? options.stepPartKeys : []);
    const selectedStepItemKeys = new Set(Array.isArray(options.stepItemKeys) ? options.stepItemKeys : []);
    if (!eventDefinitions[eventId]) {
      eventDefinitions[eventId] = {
        name: compact.name || eventId,
        type: compact.type || compact.source?.type || "",
        interaction: compact.interaction || compact.source?.interaction || "interact",
        entryStepId: compact.entryStepId || compact.steps?.[0]?.id || "",
        effects: [],
        steps: [],
      };
    }
    updateEventDefinition(eventId, (event) => {
      const existingSteps = Array.isArray(event.steps) ? event.steps : [];
      const existingStepMap = new Map(existingSteps.map((step, index) => [step?.id || `step_${index}`, step]));
      const restoreSteps = Array.isArray(compact.steps) ? compact.steps : [];
      const restoreStepMap = new Map(restoreSteps.map((step, index) => [step?.id || `step_${index}`, step]));
      function mergeStepPart(stepId, partName, existingItems, restoreItems) {
        const baseItems = Array.isArray(existingItems) ? JSON.parse(JSON.stringify(existingItems)) : [];
        const nextRestoreItems = Array.isArray(restoreItems) ? restoreItems : [];
        const partKey = `${stepId}:${partName}`;
        const itemKeys = [...selectedStepItemKeys].filter((key) => key.startsWith(`${partKey}:`));
        if (!selectedStepPartKeys.has(partKey)) return baseItems;
        if (!itemKeys.length) return JSON.parse(JSON.stringify(nextRestoreItems));
        const merged = [...baseItems];
        itemKeys.forEach((key) => {
          const index = Number(key.split(":").pop());
          if (!Number.isInteger(index) || index < 0) return;
          if (index < nextRestoreItems.length) merged[index] = JSON.parse(JSON.stringify(nextRestoreItems[index]));
        });
        return merged;
      }
      if (selectedFieldKeys.has("name")) event.name = compact.name || event.name || eventId;
      if (selectedFieldKeys.has("type")) event.type = compact.type || compact.source?.type || event.type || "";
      if (selectedFieldKeys.has("interaction")) event.interaction = compact.interaction || compact.source?.interaction || event.interaction || "interact";
      if (selectedFieldKeys.has("entryStepId")) event.entryStepId = compact.entryStepId || event.entryStepId || "";
      if (selectedFieldKeys.has("effects")) event.effects = JSON.parse(JSON.stringify(Array.isArray(compact.effects) ? compact.effects : []));
      const nextSteps = [];
      const pushed = new Set();
      restoreSteps.forEach((step, index) => {
        const id = step?.id || `step_${index}`;
        if (selectedStepIds.has(id)) {
          const existing = existingStepMap.get(id) || {};
          const restoreBranches = Array.isArray(step?.branches) ? step.branches : [];
          const restoreChoices = Array.isArray(step?.choices) ? step.choices : [];
          const restoreEffects = Array.isArray(step?.effects) ? step.effects : [];
          nextSteps.push({
            ...existing,
            id,
            title: step?.title || "",
            text: step?.text || "",
            nextStepId: step?.nextStepId || "",
            branches: mergeStepPart(id, "branches", existing.branches, restoreBranches),
            choices: mergeStepPart(id, "choices", existing.choices, restoreChoices),
            effects: mergeStepPart(id, "effects", existing.effects, restoreEffects),
          });
          pushed.add(id);
          return;
        }
        if (existingStepMap.has(id)) {
          nextSteps.push(existingStepMap.get(id));
          pushed.add(id);
        }
      });
      existingSteps.forEach((step, index) => {
        const id = step?.id || `step_${index}`;
        if (pushed.has(id)) return;
        if (selectedStepIds.has(id) && !restoreStepMap.has(id)) return;
        nextSteps.push(step);
      });
      event.steps = nextSteps;
    });
    return true;
  }

  function summarizeEventBundleDiffValue(value, maxLength = 120) {
    const text = JSON.stringify(value ?? null);
    if (!text) return "null";
    return text.length > maxLength ? `${text.slice(0, maxLength - 3)}...` : text;
  }

  function buildEventBundleVisualDiffRows(row, patch) {
    if (!row || !patch) return [];
    if (patch.status === "added") {
      return [{
        path: "(row)",
        status: "added",
        previousText: "없음",
        currentText: summarizeEventBundleDiffValue(patch.current),
      }];
    }
    if (patch.status === "removed") {
      return [{
        path: "(row)",
        status: "removed",
        previousText: summarizeEventBundleDiffValue(patch.previous),
        currentText: "없음",
      }];
    }
    return (patch.changes || []).map((entry) => ({
      path: entry.path || "(root)",
      status: "changed",
      previousText: summarizeEventBundleDiffValue(entry.previous),
      currentText: summarizeEventBundleDiffValue(entry.current),
    }));
  }

  function eventExportHistoryLine(entry) {
    const counts = entry?.summary
      ? `step ${entry.summary.stepCount || 0} · branch ${entry.summary.branchCount || 0} · choice ${entry.summary.choiceCount || 0}`
      : "";
    return [
      entry?.label || "export",
      entry?.mode || "",
      entry?.targetId || "",
      entry?.formatVersion ? `v${entry.formatVersion}` : "",
      counts,
      entry?.exportedAt || "",
    ].filter(Boolean).join(" · ");
  }

  function normalizeEventExportArchiveQuery(query = "") {
    return String(query || "").trim().toLowerCase();
  }

  function eventExportEntryMatchesQuery(entry, query = getState().editor?.eventExportArchiveQuery || "") {
    const normalizedQuery = normalizeEventExportArchiveQuery(query);
    if (!normalizedQuery) return true;
    const haystack = [
      entry?.kind || "",
      entry?.label || "",
      entry?.mode || "",
      entry?.targetId || "",
      eventExportArchiveLine(entry),
      eventExportHistoryLine(entry),
    ].join(" ").toLowerCase();
    return haystack.includes(normalizedQuery);
  }

  return {
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
  };
}

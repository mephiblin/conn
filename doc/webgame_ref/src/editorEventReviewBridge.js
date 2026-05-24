export function createEditorEventReviewBridge(deps = {}) {
  const {
    getState = () => ({}),
    eventDefinitions = {},
    npcs = {},
    buildEventValidationSnapshot = () => ({ summary: { error: 0, warning: 0, info: 0 } }),
    buildEventExportSummaryDiff = () => null,
    recordEventExportHistory = () => {},
    recordEventExportArchive = () => {},
    applyPartialCompactEventRowToDefinition = () => false,
    activeEventEditorTool = () => "eventTrigger",
    addLog = () => {},
  } = deps;

  function buildEventGraphPreview(event) {
    const steps = Array.isArray(event?.steps) ? event.steps : [];
    const entryStepId = event?.entryStepId || steps[0]?.id || "";
    return steps.map((step, index) => {
      const nextTargets = [
        step.nextStepId || "",
        ...(step.branches || []).map((branch) => branch.nextStepId || ""),
        ...(step.choices || []).map((choice) => choice.nextStepId || ""),
      ].filter(Boolean);
      return {
        index,
        id: step.id || `step_${index}`,
        title: step.title || "",
        isEntry: (step.id || `step_${index}`) === entryStepId,
        branchCount: (step.branches || []).length,
        choiceCount: (step.choices || []).length,
        effectCount: (step.effects || []).length,
        nextTargets,
      };
    });
  }

  function buildEventGraphExportSummary(event) {
    const steps = Array.isArray(event?.steps) ? event.steps : [];
    return {
      stepCount: steps.length,
      rootEffectCount: (event?.effects || []).length,
      branchCount: steps.reduce((sum, step) => sum + (step?.branches || []).length, 0),
      choiceCount: steps.reduce((sum, step) => sum + (step?.choices || []).length, 0),
      stepEffectCount: steps.reduce((sum, step) => sum + (step?.effects || []).length, 0),
    };
  }

  function buildEventGraphCompactExport(eventId, event) {
    const summary = buildEventGraphExportSummary(event);
    return {
      kind: "eventGraphCompactExport",
      formatVersion: 3,
      generatedAt: new Date().toISOString(),
      source: {
        eventId,
        name: event?.name || "",
        type: event?.type || "",
        interaction: event?.interaction || "interact",
      },
      summary,
      eventId,
      name: event?.name || "",
      type: event?.type || "",
      interaction: event?.interaction || "interact",
      entryStepId: event?.entryStepId || event?.steps?.[0]?.id || "",
      rootEffectCount: summary.rootEffectCount,
      effects: JSON.parse(JSON.stringify(event?.effects || [])),
      steps: (event?.steps || []).map((step, index) => ({
        index,
        id: step?.id || `step_${index}`,
        title: step?.title || "",
        text: step?.text || "",
        nextStepId: step?.nextStepId || "",
        branchCount: (step?.branches || []).length,
        choiceCount: (step?.choices || []).length,
        effectCount: (step?.effects || []).length,
        branches: JSON.parse(JSON.stringify(step?.branches || [])),
        choices: JSON.parse(JSON.stringify(step?.choices || [])),
        effects: JSON.parse(JSON.stringify(step?.effects || [])),
      })),
    };
  }

  function buildEventGraphSummaryDiff(eventId, event) {
    const compact = buildEventGraphCompactExport(eventId, event);
    const stepIds = new Set(compact.steps.map((step) => step.id));
    const danglingBranchTargets = [];
    const danglingChoiceTargets = [];
    const danglingDefaultTargets = [];
    for (const step of event?.steps || []) {
      if (step?.nextStepId && !stepIds.has(step.nextStepId)) danglingDefaultTargets.push(`${step.id || "(step)"} -> ${step.nextStepId}`);
      for (const branch of step?.branches || []) {
        if (branch?.nextStepId && !stepIds.has(branch.nextStepId)) danglingBranchTargets.push(`${step.id || "(step)"} -> ${branch.nextStepId}`);
      }
      for (const choice of step?.choices || []) {
        if (choice?.nextStepId && !stepIds.has(choice.nextStepId)) danglingChoiceTargets.push(`${step.id || "(step)"} -> ${choice.nextStepId}`);
      }
    }
    return {
      eventId,
      summary: {
        ...compact.summary,
      },
      danglingDefaultTargets,
      danglingBranchTargets,
      danglingChoiceTargets,
    };
  }

  function buildProjectEventGraphReviewBundle(floorMaps = getState().floorMaps) {
    const placementRegistry = Object.values(floorMaps || {}).flatMap((map) => (map?.placements || []).map((placement) => ({
      floor: map.floor,
      mapId: map.id,
      placement,
    })));
    const eventRows = Object.entries(eventDefinitions).map(([eventId, event]) => {
      const validation = buildEventValidationSnapshot(eventId, event, 0);
      const summaryDiff = buildEventGraphSummaryDiff(eventId, event);
      const placements = placementRegistry.filter(({ placement }) => (placement.interaction?.eventId || placement.refId) === eventId);
      const npcHandoffIssues = [];
      for (const [stepIndex, step] of (event?.steps || []).entries()) {
        for (const [effectIndex, effect] of (step?.effects || []).entries()) {
          if (effect?.kind !== "open_npc_service") continue;
          const targetPlacement = placements.find(({ placement }) => placement.id === effect.npcPlacementId)
            || placementRegistry.find(({ placement }) => placement.id === effect.npcPlacementId);
          if (!targetPlacement) {
            npcHandoffIssues.push(`step ${step.id || stepIndex} effect ${effectIndex}: npcPlacementId 누락 또는 미존재 (${effect.npcPlacementId || "(empty)"})`);
            continue;
          }
          const npc = npcs[targetPlacement.placement.npcId || targetPlacement.placement.refId];
          if (!npc) {
            npcHandoffIssues.push(`step ${step.id || stepIndex} effect ${effectIndex}: NPC 정의 없음 (${targetPlacement.placement.npcId || targetPlacement.placement.refId || "(empty)"})`);
            continue;
          }
          const serviceIndex = Number(effect.serviceIndex || 0);
          if (!Array.isArray(npc.services) || !npc.services[serviceIndex]) {
            npcHandoffIssues.push(`step ${step.id || stepIndex} effect ${effectIndex}: serviceIndex ${serviceIndex}가 ${targetPlacement.placement.id}에 없다.`);
          }
        }
      }
      return {
        eventId,
        name: event?.name || "",
        interaction: event?.interaction || "interact",
        linkedPlacementCount: placements.length,
        linkedPlacementIds: placements.map(({ placement }) => placement.id),
        validationSummary: validation.summary,
        summaryDiff: summaryDiff.summary,
        danglingDefaultTargets: summaryDiff.danglingDefaultTargets,
        danglingBranchTargets: summaryDiff.danglingBranchTargets,
        danglingChoiceTargets: summaryDiff.danglingChoiceTargets,
        npcHandoffIssues,
        compact: buildEventGraphCompactExport(eventId, event),
      };
    });
    const totals = eventRows.reduce((acc, row) => {
      acc.eventCount += 1;
      acc.error += Number(row.validationSummary.error || 0);
      acc.warning += Number(row.validationSummary.warning || 0);
      acc.info += Number(row.validationSummary.info || 0);
      acc.linkedPlacementCount += row.linkedPlacementCount;
      acc.danglingDefaultTargets += row.danglingDefaultTargets.length;
      acc.danglingBranchTargets += row.danglingBranchTargets.length;
      acc.danglingChoiceTargets += row.danglingChoiceTargets.length;
      acc.npcHandoffIssues += row.npcHandoffIssues.length;
      return acc;
    }, {
      eventCount: 0,
      error: 0,
      warning: 0,
      info: 0,
      linkedPlacementCount: 0,
      danglingDefaultTargets: 0,
      danglingBranchTargets: 0,
      danglingChoiceTargets: 0,
      npcHandoffIssues: 0,
    });
    const issueLines = eventRows.flatMap((row) => [
      ...row.danglingDefaultTargets.map((message) => ({ severity: "error", eventId: row.eventId, message: `default target 끊김 · ${message}` })),
      ...row.danglingBranchTargets.map((message) => ({ severity: "error", eventId: row.eventId, message: `branch target 끊김 · ${message}` })),
      ...row.danglingChoiceTargets.map((message) => ({ severity: "error", eventId: row.eventId, message: `choice target 끊김 · ${message}` })),
      ...row.npcHandoffIssues.map((message) => ({ severity: "warning", eventId: row.eventId, message: `npc handoff · ${message}` })),
    ]);
    return {
      totals,
      issueLines,
      events: eventRows,
      compactExport: {
        kind: "eventGraphReviewBundle",
        formatVersion: 2,
        generatedAt: new Date().toISOString(),
        source: {
          eventCount: totals.eventCount,
          linkedPlacementCount: totals.linkedPlacementCount,
        },
        summary: {
          errorCount: totals.error,
          warningCount: totals.warning,
          danglingDefaultTargets: totals.danglingDefaultTargets,
          danglingBranchTargets: totals.danglingBranchTargets,
          danglingChoiceTargets: totals.danglingChoiceTargets,
          npcHandoffIssues: totals.npcHandoffIssues,
        },
        totals,
        events: eventRows.map((row) => ({
          eventId: row.eventId,
          name: row.name,
          interaction: row.interaction,
          linkedPlacementIds: row.linkedPlacementIds,
          validationSummary: row.validationSummary,
          summaryDiff: row.summaryDiff,
          danglingDefaultTargets: row.danglingDefaultTargets,
          danglingBranchTargets: row.danglingBranchTargets,
          danglingChoiceTargets: row.danglingChoiceTargets,
          npcHandoffIssues: row.npcHandoffIssues,
          compact: row.compact,
        })),
      },
    };
  }

  function buildEventExportArchiveBatchCompare(entries = []) {
    const filtered = Array.isArray(entries) ? entries.filter((entry) => entry && isRestorableEventExportEntry(entry)) : [];
    if (!filtered.length) return null;
    const kindCounts = {};
    const targetCounts = {};
    const groupedByTarget = new Map();
    filtered.forEach((entry) => {
      const kind = entry?.kind || "unknown";
      const targetId = entry?.targetId || "(none)";
      kindCounts[kind] = (kindCounts[kind] || 0) + 1;
      targetCounts[targetId] = (targetCounts[targetId] || 0) + 1;
      if (!groupedByTarget.has(targetId)) groupedByTarget.set(targetId, []);
      groupedByTarget.get(targetId).push(entry);
    });
    const targetComparisons = [...groupedByTarget.entries()].map(([targetId, targetEntries]) => {
      const sorted = [...targetEntries].sort((a, b) => String(a.archivedAt || "").localeCompare(String(b.archivedAt || "")));
      const first = sorted[0] || null;
      const last = sorted[sorted.length - 1] || null;
      const diff = sorted.length >= 2 ? buildEventExportSummaryDiff(last, first) : null;
      return {
        targetId,
        count: sorted.length,
        first,
        last,
        diff,
      };
    }).sort((a, b) => b.count - a.count || a.targetId.localeCompare(b.targetId));
    return {
      totalCount: filtered.length,
      kindCounts,
      targetCounts,
      targetComparisons,
    };
  }

  function buildEventExportArchiveBatchCompareExport(entries = [], query = "") {
    const batchCompare = buildEventExportArchiveBatchCompare(entries);
    if (!batchCompare) return null;
    return {
      kind: "eventExportArchiveBatchCompare",
      formatVersion: 1,
      generatedAt: new Date().toISOString(),
      source: {
        query: String(query || ""),
        archiveCount: Array.isArray(entries) ? entries.filter(Boolean).length : 0,
      },
      summary: {
        totalCount: batchCompare.totalCount,
        kindCount: Object.keys(batchCompare.kindCounts || {}).length,
        targetCount: Object.keys(batchCompare.targetCounts || {}).length,
      },
      kindCounts: batchCompare.kindCounts,
      targetCounts: batchCompare.targetCounts,
      targetComparisons: (batchCompare.targetComparisons || []).map((row) => ({
        targetId: row.targetId,
        count: row.count,
        firstArchivedAt: row.first?.archivedAt || row.first?.exportedAt || "",
        lastArchivedAt: row.last?.archivedAt || row.last?.exportedAt || "",
        firstSummary: row.first?.summary || null,
        lastSummary: row.last?.summary || null,
        diff: row.diff || null,
      })),
    };
  }

  function buildEventExportArchiveBatchShareExport(batchExport, label = "", query = "") {
    if (!batchExport || typeof batchExport !== "object") return null;
    return {
      kind: "eventExportArchiveBatchShare",
      formatVersion: 1,
      sharedAt: new Date().toISOString(),
      label: String(label || "").trim() || "batch diff share",
      query: String(query || ""),
      summary: batchExport.summary || null,
      batchDiff: JSON.parse(JSON.stringify(batchExport)),
    };
  }

  function buildEventExportArchiveBatchShareLink(shareExport) {
    if (!shareExport || typeof shareExport !== "object") return "";
    try {
      const encoded = btoa(unescape(encodeURIComponent(JSON.stringify(shareExport))));
      const base = typeof window !== "undefined" && window.location
        ? `${window.location.origin || ""}${window.location.pathname || ""}`
        : "";
      return `${base}#eventBatchShare=${encoded}`;
    } catch {
      return "";
    }
  }

  function parseEventExportArchiveBatchShareLink(raw = "") {
    const text = String(raw || "").trim();
    if (!text) return null;
    const token = text.includes("#eventBatchShare=")
      ? text.split("#eventBatchShare=")[1]
      : text.startsWith("eventBatchShare=")
        ? text.slice("eventBatchShare=".length)
        : text;
    if (!token) return null;
    try {
      return JSON.parse(decodeURIComponent(escape(atob(token))));
    } catch {
      return null;
    }
  }

  let didAutoImportEventBatchShareLink = false;

  function importEventExportArchiveBatchShareLink(raw = "", options = {}) {
    const text = String(raw || "").trim();
    if (!text) throw new Error("불러올 batch share link text가 없다.");
    const parsed = parseEventExportArchiveBatchShareLink(text);
    if (!parsed || parsed.kind !== "eventExportArchiveBatchShare") {
      throw new Error("batch share link 형식이 아니다.");
    }
    const state = getState();
    state.editor.eventExportArchiveQuery = String(parsed.query || "");
    state.editor.eventExportArchiveBatchShareLabel = String(parsed.label || "");
    if (Object.prototype.hasOwnProperty.call(options, "draftValue")) {
      state.editor.eventExportArchiveBatchShareLinkDraft = String(options.draftValue || "");
    }
    const archiveEntry = {
      kind: "archive_batch_share",
      mode: options.mode || "share_link_import",
      label: parsed.label || "batch share link",
      targetId: `archive_count_${parsed.summary?.totalCount || 0}`,
      summary: parsed.summary || null,
      formatVersion: parsed.formatVersion || 0,
      payload: parsed,
    };
    recordEventExportHistory(archiveEntry);
    recordEventExportArchive(archiveEntry);
    addLog(options.logMessage || `batch share link를 불러왔다. query "${parsed.query || ""}" · label "${parsed.label || "(empty)"}"`);
    return parsed;
  }

  function autoImportEventExportArchiveBatchShareLinkFromLocation() {
    if (didAutoImportEventBatchShareLink || typeof window === "undefined" || !window.location) return false;
    didAutoImportEventBatchShareLink = true;
    const hash = String(window.location.hash || "").trim();
    if (!hash.includes("eventBatchShare=")) return false;
    const raw = `${window.location.origin || ""}${window.location.pathname || ""}${hash}` || hash;
    try {
      importEventExportArchiveBatchShareLink(raw, {
        mode: "share_link_auto_import",
        draftValue: raw,
        logMessage: `batch share link를 URL hash에서 자동 불러왔다. query "${getState().editor.eventExportArchiveQuery || ""}" · label "${getState().editor.eventExportArchiveBatchShareLabel || "(empty)"}"`,
      });
      return true;
    } catch (error) {
      addLog(`batch share link 자동 불러오기 실패: ${error.message}`);
      return false;
    }
  }

  function buildFullCompactEventRestoreOptions(compactRow) {
    const compact = compactRow?.compact && typeof compactRow.compact === "object" ? compactRow.compact : compactRow;
    const steps = Array.isArray(compact?.steps) ? compact.steps : [];
    const fieldKeys = ["name", "type", "interaction", "entryStepId", "effects"];
    const stepIds = [];
    const stepPartKeys = [];
    const stepItemKeys = [];
    steps.forEach((step, index) => {
      const stepId = step?.id || `step_${index}`;
      stepIds.push(stepId);
      ["branches", "choices", "effects"].forEach((partName) => {
        stepPartKeys.push(`${stepId}:${partName}`);
        const items = Array.isArray(step?.[partName]) ? step[partName] : [];
        items.forEach((_, itemIndex) => {
          stepItemKeys.push(`${stepId}:${partName}:${itemIndex}`);
        });
      });
    });
    return { fieldKeys, stepIds, stepPartKeys, stepItemKeys };
  }

  function hasRestorableCompactEventPayload(compactRow) {
    const compact = compactRow?.compact && typeof compactRow.compact === "object" ? compactRow.compact : compactRow;
    if (!compact || typeof compact !== "object") return false;
    if (!Array.isArray(compact.effects)) return false;
    const steps = Array.isArray(compact.steps) ? compact.steps : [];
    return steps.every((step) => Array.isArray(step?.branches) && Array.isArray(step?.choices) && Array.isArray(step?.effects));
  }

  function applyEventExportArchiveEntryPayload(entry) {
    if (!entry || typeof entry !== "object") return { ok: false, reason: "missing_entry" };
    if (entry.kind === "graph") {
      const compact = entry.payload;
      const eventId = entry.targetId || compact?.eventId || "";
      if (!compact || !eventId) return { ok: false, reason: "missing_graph_payload" };
      if (!hasRestorableCompactEventPayload(compact)) return { ok: false, reason: "legacy_graph_payload" };
      const applied = applyPartialCompactEventRowToDefinition(eventId, compact, buildFullCompactEventRestoreOptions(compact));
      return applied ? { ok: true, eventIds: [eventId] } : { ok: false, reason: "graph_apply_failed" };
    }
    if (entry.kind === "bundle") {
      const rows = Array.isArray(entry.payload?.events) ? entry.payload.events : [];
      if (!rows.length) return { ok: false, reason: "missing_bundle_rows" };
      const eventIds = [];
      for (const row of rows) {
        const compact = row?.compact || row;
        const eventId = row?.eventId || compact?.eventId || "";
        if (!compact || !eventId) return { ok: false, reason: "missing_bundle_row" };
        if (!hasRestorableCompactEventPayload(compact)) return { ok: false, reason: `legacy_bundle_row:${eventId}` };
        const applied = applyPartialCompactEventRowToDefinition(eventId, compact, buildFullCompactEventRestoreOptions(compact));
        if (!applied) return { ok: false, reason: `bundle_apply_failed:${eventId}` };
        eventIds.push(eventId);
      }
      return { ok: true, eventIds };
    }
    return { ok: false, reason: "unsupported_kind" };
  }

  function isRestorableEventExportEntry(entry) {
    if (entry?.kind === "graph") return hasRestorableCompactEventPayload(entry.payload);
    if (entry?.kind === "bundle") {
      const rows = Array.isArray(entry?.payload?.events) ? entry.payload.events : [];
      return rows.length > 0 && rows.every((row) => hasRestorableCompactEventPayload(row?.compact || row));
    }
    return false;
  }

  function applyEventExportArchiveBatchCompareTargets(batchCompare, query = "", eventTool = activeEventEditorTool()) {
    const state = getState();
    if (!batchCompare?.targetComparisons?.length) return { ok: false, reason: "missing_targets" };
    const appliedTargetIds = [];
    const appliedEventIds = [];
    for (const row of batchCompare.targetComparisons) {
      const latestEntry = row?.last;
      if (!latestEntry) continue;
      const result = applyEventExportArchiveEntryPayload(latestEntry);
      if (!result.ok) {
        return { ok: false, reason: result.reason || "apply_failed", failedTargetId: row.targetId || latestEntry.targetId || latestEntry.kind || "" };
      }
      appliedTargetIds.push(row.targetId || latestEntry.targetId || "");
      appliedEventIds.push(...(Array.isArray(result.eventIds) ? result.eventIds : []));
    }
    const uniqueEventIds = [...new Set(appliedEventIds.filter(Boolean))];
    const uniqueTargetIds = [...new Set(appliedTargetIds.filter(Boolean))];
    if (!uniqueTargetIds.length) return { ok: false, reason: "empty_result" };
    if (uniqueEventIds.length) {
      const lastEventId = uniqueEventIds[uniqueEventIds.length - 1];
      state.editor.selectedEventDefinitionIds = { ...state.editor.selectedEventDefinitionIds, [eventTool]: lastEventId };
    }
    return {
      ok: true,
      query: String(query || ""),
      targetIds: uniqueTargetIds,
      eventIds: uniqueEventIds,
    };
  }

  return {
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
  };
}

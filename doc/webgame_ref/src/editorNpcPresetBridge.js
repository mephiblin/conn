export function createEditorNpcPresetBridge(deps = {}) {
  const {
    getState = () => ({}),
    localStorageObject = null,
    storageKey = "",
    npcs = {},
    escapeHtml = (value) => String(value ?? ""),
    buildLineDiffText = () => "",
  } = deps;

  function normalizeNpcCustomPresetDefinition(definition = {}) {
    return {
      id: definition.id || `npc_preset_${Date.now()}`,
      name: definition.name || "새 NPC preset",
      note: definition.note || "",
      services: JSON.parse(JSON.stringify(Array.isArray(definition.services) ? definition.services : [])),
      questSeeds: JSON.parse(JSON.stringify(Array.isArray(definition.questSeeds) ? definition.questSeeds : [])),
    };
  }

  function loadNpcCustomPresets() {
    try {
      const raw = localStorageObject?.getItem(storageKey);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed.map((entry) => normalizeNpcCustomPresetDefinition(entry)) : [];
    } catch {
      return [];
    }
  }

  function saveNpcCustomPresets(presets) {
    localStorageObject?.setItem(
      storageKey,
      JSON.stringify((presets || []).map((entry) => normalizeNpcCustomPresetDefinition(entry)))
    );
  }

  function uniqueNpcCustomPresetId(baseId = "npc_preset_custom", presets = loadNpcCustomPresets()) {
    const slug = (baseId || "npc_preset_custom")
      .replace(/[^a-zA-Z0-9_]+/g, "_")
      .replace(/^_+|_+$/g, "")
      .toLowerCase() || "npc_preset_custom";
    const used = new Set((presets || []).map((entry) => entry.id).filter(Boolean));
    let candidate = slug;
    let index = 1;
    while (used.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function buildNpcCustomPresetFromDefinition(npcId, npc, existingPresets = loadNpcCustomPresets()) {
    return normalizeNpcCustomPresetDefinition({
      id: uniqueNpcCustomPresetId(npcId || "npc_preset_custom", existingPresets),
      name: `${npc?.name || npcId || "NPC"} preset`,
      note: npc?.description || "",
      services: npc?.services || [],
      questSeeds: npc?.questSeeds || [],
    });
  }

  function npcCustomPresetSummary(preset) {
    if (!preset) return "preset 없음";
    return `service ${(preset.services || []).length} · quest seed ${(preset.questSeeds || []).length}`;
  }

  function npcPresetServiceIdentity(entry) {
    return `${entry?.type || ""}:${entry?.label || ""}`;
  }

  function npcPresetSeedIdentity(entry) {
    return entry?.id || "";
  }

  function npcDialogueStepIdentity(entry) {
    return entry?.id || "";
  }

  function npcDialogueChoiceIdentity(entry) {
    return `${entry?.label || entry?.text || ""}:${entry?.nextStepId || ""}`;
  }

  function npcDialogueBranchIdentity(entry) {
    return `${entry?.label || ""}:${entry?.nextStepId || ""}:${entry?.requiredFlag || ""}:${entry?.requiredSeedId || ""}:${entry?.requiredQuestSeedId || ""}`;
  }

  function diffObjectFieldKeys(currentEntry, nextEntry) {
    const keys = new Set([
      ...Object.keys(currentEntry || {}),
      ...Object.keys(nextEntry || {}),
    ]);
    return [...keys].filter((key) => JSON.stringify(currentEntry?.[key] ?? null) !== JSON.stringify(nextEntry?.[key] ?? null));
  }

  function buildNpcCustomPresetDiff(npc, preset) {
    const currentServices = Array.isArray(npc?.services) ? npc.services : [];
    const currentSeeds = Array.isArray(npc?.questSeeds) ? npc.questSeeds : [];
    const presetServices = Array.isArray(preset?.services) ? preset.services : [];
    const presetSeeds = Array.isArray(preset?.questSeeds) ? preset.questSeeds : [];
    const currentServiceMap = new Map(currentServices.map((entry) => [npcPresetServiceIdentity(entry), entry]));
    const currentSeedMap = new Map(currentSeeds.map((entry) => [npcPresetSeedIdentity(entry), entry]).filter(([key]) => key));
    const currentServiceLabels = new Set(currentServiceMap.keys());
    const currentSeedIds = new Set(currentSeedMap.keys());
    const duplicateServices = presetServices.filter((entry) => currentServiceLabels.has(npcPresetServiceIdentity(entry)));
    const duplicateSeeds = presetSeeds.filter((entry) => currentSeedIds.has(npcPresetSeedIdentity(entry)));
    const serviceRows = presetServices.map((entry, index) => {
      const existing = currentServiceMap.get(npcPresetServiceIdentity(entry)) || null;
      const changedFields = diffObjectFieldKeys(existing, entry);
      const existingDialogueSteps = Array.isArray(existing?.dialogue?.steps) ? existing.dialogue.steps : [];
      const presetDialogueSteps = Array.isArray(entry?.dialogue?.steps) ? entry.dialogue.steps : [];
      const existingDialogueMap = new Map(existingDialogueSteps.map((step) => [npcDialogueStepIdentity(step), step]).filter(([key]) => key));
      const dialogueStepRows = presetDialogueSteps.map((step, stepIndex) => {
        const existingStep = existingDialogueMap.get(npcDialogueStepIdentity(step)) || null;
        const stepChangedFields = diffObjectFieldKeys(existingStep, step);
        const existingChoices = Array.isArray(existingStep?.choices) ? existingStep.choices : [];
        const presetChoices = Array.isArray(step?.choices) ? step.choices : [];
        const existingBranches = Array.isArray(existingStep?.branches) ? existingStep.branches : [];
        const presetBranches = Array.isArray(step?.branches) ? step.branches : [];
        const existingChoiceMap = new Map(existingChoices.map((choice) => [npcDialogueChoiceIdentity(choice), choice]));
        const existingBranchMap = new Map(existingBranches.map((branch) => [npcDialogueBranchIdentity(branch), branch]));
        const choiceRows = presetChoices.map((choice, choiceIndex) => {
          const existingChoice = existingChoiceMap.get(npcDialogueChoiceIdentity(choice)) || null;
          const choiceChangedFields = diffObjectFieldKeys(existingChoice, choice);
          return {
            index: choiceIndex,
            choice,
            existingChoice,
            changedFields: choiceChangedFields,
            status: !existingChoice ? "new" : choiceChangedFields.length ? "update" : "same",
          };
        });
        const branchRows = presetBranches.map((branch, branchIndex) => {
          const existingBranch = existingBranchMap.get(npcDialogueBranchIdentity(branch)) || null;
          const branchChangedFields = diffObjectFieldKeys(existingBranch, branch);
          return {
            index: branchIndex,
            branch,
            existingBranch,
            changedFields: branchChangedFields,
            status: !existingBranch ? "new" : branchChangedFields.length ? "update" : "same",
          };
        });
        return {
          index: stepIndex,
          step,
          existingStep,
          changedFields: stepChangedFields,
          choiceRows,
          branchRows,
          status: !existingStep ? "new" : stepChangedFields.length ? "update" : "same",
        };
      });
      return {
        index,
        entry,
        existing,
        changedFields,
        dialogueStepRows,
        status: !existing ? "new" : changedFields.length ? "update" : "same",
      };
    });
    const seedRows = presetSeeds.map((entry, index) => {
      const existing = currentSeedMap.get(npcPresetSeedIdentity(entry)) || null;
      const changedFields = diffObjectFieldKeys(existing, entry);
      return {
        index,
        entry,
        existing,
        changedFields,
        status: !existing ? "new" : changedFields.length ? "update" : "same",
      };
    });
    return {
      serviceDelta: presetServices.length - currentServices.length,
      questSeedDelta: presetSeeds.length - currentSeeds.length,
      duplicateServices,
      duplicateSeeds,
      newServices: presetServices.filter((entry) => !currentServiceLabels.has(`${entry?.type || ""}:${entry?.label || ""}`)),
      newSeeds: presetSeeds.filter((entry) => !currentSeedIds.has(entry?.id || "")),
      serviceRows,
      seedRows,
    };
  }

  function defaultNpcPresetSelectionIndexes(entries = []) {
    return (entries || []).map((_, index) => index);
  }

  function defaultNpcPresetDialogueSelectionMap(preset) {
    const services = Array.isArray(preset?.services) ? preset.services : [];
    return Object.fromEntries(services.map((service, serviceIndex) => [serviceIndex, defaultNpcPresetSelectionIndexes(service?.dialogue?.steps || [])]));
  }

  function defaultNpcPresetDialogueChoiceSelectionMap(preset) {
    const services = Array.isArray(preset?.services) ? preset.services : [];
    const entries = [];
    services.forEach((service, serviceIndex) => {
      const steps = Array.isArray(service?.dialogue?.steps) ? service.dialogue.steps : [];
      steps.forEach((step, stepIndex) => {
        entries.push([`${serviceIndex}:${stepIndex}`, defaultNpcPresetSelectionIndexes(step?.choices || [])]);
      });
    });
    return Object.fromEntries(entries);
  }

  function defaultNpcPresetDialogueBranchSelectionMap(preset) {
    const services = Array.isArray(preset?.services) ? preset.services : [];
    const entries = [];
    services.forEach((service, serviceIndex) => {
      const steps = Array.isArray(service?.dialogue?.steps) ? service.dialogue.steps : [];
      steps.forEach((step, stepIndex) => {
        entries.push([`${serviceIndex}:${stepIndex}`, defaultNpcPresetSelectionIndexes(step?.branches || [])]);
      });
    });
    return Object.fromEntries(entries);
  }

  function defaultNpcPresetServiceFieldSelectionMap(diff) {
    return Object.fromEntries((diff?.serviceRows || []).map((row) => [row.index, [...(row.changedFields || [])]]));
  }

  function defaultNpcPresetSeedFieldSelectionMap(diff) {
    return Object.fromEntries((diff?.seedRows || []).map((row) => [row.index, [...(row.changedFields || [])]]));
  }

  function selectedNpcPresetServiceIndexes(preset) {
    const state = getState();
    const max = Array.isArray(preset?.services) ? preset.services.length : 0;
    const selected = Array.isArray(state.editor.selectedNpcCustomPresetServiceIndexes)
      ? state.editor.selectedNpcCustomPresetServiceIndexes.map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0 && value < max)
      : [];
    if (preset?.id && state.editor.selectedNpcCustomPresetId === preset.id) return selected;
    return defaultNpcPresetSelectionIndexes(preset?.services || []);
  }

  function selectedNpcPresetSeedIndexes(preset) {
    const state = getState();
    const max = Array.isArray(preset?.questSeeds) ? preset.questSeeds.length : 0;
    const selected = Array.isArray(state.editor.selectedNpcCustomPresetSeedIndexes)
      ? state.editor.selectedNpcCustomPresetSeedIndexes.map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0 && value < max)
      : [];
    if (preset?.id && state.editor.selectedNpcCustomPresetId === preset.id) return selected;
    return defaultNpcPresetSelectionIndexes(preset?.questSeeds || []);
  }

  function selectedNpcPresetDialogueStepIndexes(preset, serviceIndex) {
    const state = getState();
    const steps = Array.isArray(preset?.services?.[serviceIndex]?.dialogue?.steps) ? preset.services[serviceIndex].dialogue.steps : [];
    const stored = state.editor.selectedNpcCustomPresetDialogueStepSelections && typeof state.editor.selectedNpcCustomPresetDialogueStepSelections === "object"
      ? state.editor.selectedNpcCustomPresetDialogueStepSelections[String(serviceIndex)]
      : null;
    const selected = Array.isArray(stored)
      ? stored.map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0 && value < steps.length)
      : [];
    if (preset?.id && state.editor.selectedNpcCustomPresetId === preset.id) return selected;
    return defaultNpcPresetSelectionIndexes(steps);
  }

  function selectedNpcPresetDialogueChoiceIndexes(preset, serviceIndex, stepIndex) {
    const state = getState();
    const choices = Array.isArray(preset?.services?.[serviceIndex]?.dialogue?.steps?.[stepIndex]?.choices)
      ? preset.services[serviceIndex].dialogue.steps[stepIndex].choices
      : [];
    const key = `${serviceIndex}:${stepIndex}`;
    const stored = state.editor.selectedNpcCustomPresetDialogueChoiceSelections && typeof state.editor.selectedNpcCustomPresetDialogueChoiceSelections === "object"
      ? state.editor.selectedNpcCustomPresetDialogueChoiceSelections[key]
      : null;
    const selected = Array.isArray(stored)
      ? stored.map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0 && value < choices.length)
      : [];
    if (preset?.id && state.editor.selectedNpcCustomPresetId === preset.id) return selected;
    return defaultNpcPresetSelectionIndexes(choices);
  }

  function selectedNpcPresetDialogueBranchIndexes(preset, serviceIndex, stepIndex) {
    const state = getState();
    const branches = Array.isArray(preset?.services?.[serviceIndex]?.dialogue?.steps?.[stepIndex]?.branches)
      ? preset.services[serviceIndex].dialogue.steps[stepIndex].branches
      : [];
    const key = `${serviceIndex}:${stepIndex}`;
    const stored = state.editor.selectedNpcCustomPresetDialogueBranchSelections && typeof state.editor.selectedNpcCustomPresetDialogueBranchSelections === "object"
      ? state.editor.selectedNpcCustomPresetDialogueBranchSelections[key]
      : null;
    const selected = Array.isArray(stored)
      ? stored.map((value) => Number(value)).filter((value) => Number.isInteger(value) && value >= 0 && value < branches.length)
      : [];
    if (preset?.id && state.editor.selectedNpcCustomPresetId === preset.id) return selected;
    return defaultNpcPresetSelectionIndexes(branches);
  }

  function selectedNpcPresetServiceFieldNames(diffRow) {
    const state = getState();
    const key = String(diffRow?.index ?? "");
    const stored = state.editor.selectedNpcCustomPresetServiceFieldSelections && typeof state.editor.selectedNpcCustomPresetServiceFieldSelections === "object"
      ? state.editor.selectedNpcCustomPresetServiceFieldSelections[key]
      : null;
    const selected = Array.isArray(stored) ? stored.filter((value) => (diffRow?.changedFields || []).includes(value)) : [];
    return selected.length ? selected : [...(diffRow?.changedFields || [])];
  }

  function selectedNpcPresetSeedFieldNames(diffRow) {
    const state = getState();
    const key = String(diffRow?.index ?? "");
    const stored = state.editor.selectedNpcCustomPresetSeedFieldSelections && typeof state.editor.selectedNpcCustomPresetSeedFieldSelections === "object"
      ? state.editor.selectedNpcCustomPresetSeedFieldSelections[key]
      : null;
    const selected = Array.isArray(stored) ? stored.filter((value) => (diffRow?.changedFields || []).includes(value)) : [];
    return selected.length ? selected : [...(diffRow?.changedFields || [])];
  }

  function buildNpcPresetResolvedDialogueStep(stepRow, preset, serviceIndex, conflictMode = "preset_wins") {
    if (!stepRow?.step) return null;
    if (!stepRow.existingStep || conflictMode === "preset_wins") return JSON.parse(JSON.stringify(stepRow.step));
    const nextStep = JSON.parse(JSON.stringify(stepRow.existingStep || {}));
    const selectedChoiceIndexes = selectedNpcPresetDialogueChoiceIndexes(preset, serviceIndex, stepRow.index);
    const selectedBranchIndexes = selectedNpcPresetDialogueBranchIndexes(preset, serviceIndex, stepRow.index);
    const selectedChoices = (stepRow.step.choices || []).filter((_, choiceIndex) => selectedChoiceIndexes.includes(choiceIndex));
    const selectedBranches = (stepRow.step.branches || []).filter((_, branchIndex) => selectedBranchIndexes.includes(branchIndex));
    nextStep.choices = Array.isArray(nextStep.choices) ? nextStep.choices : [];
    nextStep.branches = Array.isArray(nextStep.branches) ? nextStep.branches : [];
    selectedChoices.forEach((choice) => {
      const choiceIdentity = npcDialogueChoiceIdentity(choice);
      const existingChoiceIndex = nextStep.choices.findIndex((candidate) => npcDialogueChoiceIdentity(candidate) === choiceIdentity);
      if (existingChoiceIndex >= 0) nextStep.choices[existingChoiceIndex] = JSON.parse(JSON.stringify(choice));
      else nextStep.choices.push(JSON.parse(JSON.stringify(choice)));
    });
    selectedBranches.forEach((branch) => {
      const branchIdentity = npcDialogueBranchIdentity(branch);
      const existingBranchIndex = nextStep.branches.findIndex((candidate) => npcDialogueBranchIdentity(candidate) === branchIdentity);
      if (existingBranchIndex >= 0) nextStep.branches[existingBranchIndex] = JSON.parse(JSON.stringify(branch));
      else nextStep.branches.push(JSON.parse(JSON.stringify(branch)));
    });
    if (stepRow.step.text) nextStep.text = stepRow.step.text;
    if (stepRow.step.title) nextStep.title = stepRow.step.title;
    if (stepRow.step.nextStepId) nextStep.nextStepId = stepRow.step.nextStepId;
    return nextStep;
  }

  function buildNpcPresetResolvedServicePreview(diffRow, preset, conflictMode = "preset_wins") {
    if (!diffRow?.entry) return null;
    if (!diffRow.existing || conflictMode === "preset_wins") return JSON.parse(JSON.stringify(diffRow.entry));
    const nextService = JSON.parse(JSON.stringify(diffRow.existing || {}));
    const selectedFieldNames = selectedNpcPresetServiceFieldNames(diffRow);
    selectedFieldNames.forEach((fieldName) => {
      if (fieldName === "dialogue") return;
      const nextValue = diffRow.entry?.[fieldName];
      if (nextValue == null) delete nextService[fieldName];
      else nextService[fieldName] = JSON.parse(JSON.stringify(nextValue));
    });
    if (diffRow.entry?.type === "talk" && Array.isArray(diffRow.entry?.dialogue?.steps)) {
      const selectedStepIndexes = selectedNpcPresetDialogueStepIndexes(preset, diffRow.index);
      nextService.dialogue = nextService.dialogue && typeof nextService.dialogue === "object" ? nextService.dialogue : {};
      nextService.dialogue.steps = Array.isArray(nextService.dialogue.steps) ? nextService.dialogue.steps : [];
      const selectedSteps = (diffRow.dialogueStepRows || []).filter((stepRow) => selectedStepIndexes.includes(stepRow.index));
      selectedSteps.forEach((stepRow) => {
        const stepId = npcDialogueStepIdentity(stepRow.step);
        const existingStepIndex = nextService.dialogue.steps.findIndex((candidate) => npcDialogueStepIdentity(candidate) === stepId);
        const resolvedStep = buildNpcPresetResolvedDialogueStep(stepRow, preset, diffRow.index, conflictMode);
        if (existingStepIndex >= 0) nextService.dialogue.steps[existingStepIndex] = resolvedStep;
        else nextService.dialogue.steps.push(resolvedStep);
      });
    }
    return nextService;
  }

  function buildNpcPresetResolvedSeedPreview(diffRow, conflictMode = "preset_wins") {
    if (!diffRow?.entry) return null;
    if (!diffRow.existing || conflictMode === "preset_wins") return JSON.parse(JSON.stringify(diffRow.entry));
    const nextSeed = JSON.parse(JSON.stringify(diffRow.existing || {}));
    const selectedFieldNames = selectedNpcPresetSeedFieldNames(diffRow);
    selectedFieldNames.forEach((fieldName) => {
      const nextValue = diffRow.entry?.[fieldName];
      if (nextValue == null) delete nextSeed[fieldName];
      else nextSeed[fieldName] = JSON.parse(JSON.stringify(nextValue));
    });
    return nextSeed;
  }

  function npcPresetThreeWayPreviewMarkup(currentEntry, presetEntry, resolvedEntry, elementIdPrefix) {
    return `
      <div class="preset-stack">
        <label for="${elementIdPrefix}Current">현재</label>
        <textarea id="${elementIdPrefix}Current" rows="6" spellcheck="false" readonly>${escapeHtml(JSON.stringify(currentEntry || {}, null, 2))}</textarea>
        <label for="${elementIdPrefix}Preset">Preset</label>
        <textarea id="${elementIdPrefix}Preset" rows="6" spellcheck="false" readonly>${escapeHtml(JSON.stringify(presetEntry || {}, null, 2))}</textarea>
        <label for="${elementIdPrefix}Resolved">적용 결과</label>
        <textarea id="${elementIdPrefix}Resolved" rows="6" spellcheck="false" readonly>${escapeHtml(JSON.stringify(resolvedEntry || {}, null, 2))}</textarea>
      </div>
    `;
  }

  function npcPresetSideBySideDiffMarkup(currentEntry, presetEntry, changedFields = [], elementIdPrefix, status = "changed") {
    const currentJson = JSON.stringify(currentEntry || {}, null, 2);
    const presetJson = JSON.stringify(presetEntry || {}, null, 2);
    const lineDiffText = buildLineDiffText(presetJson, currentJson);
    return `
      <div class="preset-stack">
        <div class="muted">status · ${escapeHtml(status)}${changedFields.length ? ` · field ${escapeHtml(changedFields.join(", "))}` : ""}</div>
        <label for="${elementIdPrefix}CurrentOnly">현재</label>
        <textarea id="${elementIdPrefix}CurrentOnly" rows="6" spellcheck="false" readonly>${escapeHtml(currentJson)}</textarea>
        <label for="${elementIdPrefix}PresetOnly">Preset</label>
        <textarea id="${elementIdPrefix}PresetOnly" rows="6" spellcheck="false" readonly>${escapeHtml(presetJson)}</textarea>
        <label for="${elementIdPrefix}LineDiff">Line diff</label>
        <textarea id="${elementIdPrefix}LineDiff" rows="8" spellcheck="false" readonly>${escapeHtml(lineDiffText)}</textarea>
      </div>
    `;
  }

  function buildNpcPresetMergePatch(npcId, preset, diff, applyMode = "replace", conflictMode = "preset_wins") {
    if (!preset || !diff) return null;
    const serviceRows = (diff.serviceRows || [])
      .filter((row) => selectedNpcPresetServiceIndexes(preset).includes(row.index))
      .map((row) => ({
        index: row.index,
        identity: npcPresetServiceIdentity(row.entry),
        status: row.status,
        changedFields: row.changedFields || [],
        selectedFields: selectedNpcPresetServiceFieldNames(row),
        selectedDialogueStepIndexes: selectedNpcPresetDialogueStepIndexes(preset, row.index),
        dialogueChoiceSelections: Object.fromEntries((row.dialogueStepRows || [])
          .filter((stepRow) => selectedNpcPresetDialogueStepIndexes(preset, row.index).includes(stepRow.index))
          .map((stepRow) => [
            String(stepRow.index),
            {
              selectedChoiceIndexes: selectedNpcPresetDialogueChoiceIndexes(preset, row.index, stepRow.index),
              selectedBranchIndexes: selectedNpcPresetDialogueBranchIndexes(preset, row.index, stepRow.index),
            },
          ])),
        resolved: buildNpcPresetResolvedServicePreview(row, preset, conflictMode),
      }));
    const seedRows = (diff.seedRows || [])
      .filter((row) => selectedNpcPresetSeedIndexes(preset).includes(row.index))
      .map((row) => ({
        index: row.index,
        identity: npcPresetSeedIdentity(row.entry),
        status: row.status,
        changedFields: row.changedFields || [],
        selectedFields: selectedNpcPresetSeedFieldNames(row),
        resolved: buildNpcPresetResolvedSeedPreview(row, conflictMode),
      }));
    return {
      kind: "npcPresetMergePatch",
      npcId: npcId || "",
      presetId: preset.id || "",
      presetName: preset.name || "",
      applyMode,
      conflictMode,
      serviceCount: serviceRows.length,
      questSeedCount: seedRows.length,
      services: serviceRows,
      questSeeds: seedRows,
    };
  }

  function applyNpcPresetMergePatchToDefinition(entry, patch) {
    if (!entry || !patch || patch.kind !== "npcPresetMergePatch") return false;
    const applyMode = patch.applyMode === "append" ? "append" : "replace";
    const serviceRows = Array.isArray(patch.services) ? patch.services : [];
    const seedRows = Array.isArray(patch.questSeeds) ? patch.questSeeds : [];
    if (!serviceRows.length && !seedRows.length) return false;
    if (applyMode === "replace") {
      if (serviceRows.length) entry.services = serviceRows.map((row) => JSON.parse(JSON.stringify(row.resolved || {})));
      if (seedRows.length) entry.questSeeds = seedRows.map((row) => JSON.parse(JSON.stringify(row.resolved || {})));
      return true;
    }
    entry.services = Array.isArray(entry.services) ? entry.services : [];
    entry.questSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
    serviceRows.forEach((row) => {
      const resolved = JSON.parse(JSON.stringify(row.resolved || {}));
      const identity = row.identity || npcPresetServiceIdentity(resolved);
      const existingIndex = entry.services.findIndex((candidate) => npcPresetServiceIdentity(candidate) === identity);
      if (existingIndex >= 0) entry.services[existingIndex] = resolved;
      else entry.services.push(resolved);
    });
    seedRows.forEach((row) => {
      const resolved = JSON.parse(JSON.stringify(row.resolved || {}));
      const identity = row.identity || npcPresetSeedIdentity(resolved);
      const existingIndex = entry.questSeeds.findIndex((candidate) => npcPresetSeedIdentity(candidate) === identity);
      if (existingIndex >= 0) entry.questSeeds[existingIndex] = resolved;
      else entry.questSeeds.push(resolved);
    });
    return true;
  }

  function buildNpcPresetApplyComparePreview(npcDefinition, patch) {
    if (!npcDefinition || !patch || patch.kind !== "npcPresetMergePatch") return null;
    const current = {
      id: npcDefinition.id || "",
      services: JSON.parse(JSON.stringify(Array.isArray(npcDefinition.services) ? npcDefinition.services : [])),
      questSeeds: JSON.parse(JSON.stringify(Array.isArray(npcDefinition.questSeeds) ? npcDefinition.questSeeds : [])),
    };
    const resolved = JSON.parse(JSON.stringify(current));
    const changed = applyNpcPresetMergePatchToDefinition(resolved, patch);
    const currentJson = JSON.stringify(current, null, 2);
    const resolvedJson = JSON.stringify(resolved, null, 2);
    return {
      changed,
      current,
      resolved,
      currentJson,
      resolvedJson,
      lineDiffText: buildLineDiffText(resolvedJson, currentJson),
      currentServiceCount: current.services.length,
      currentQuestSeedCount: current.questSeeds.length,
      resolvedServiceCount: resolved.services.length,
      resolvedQuestSeedCount: resolved.questSeeds.length,
    };
  }

  function validateNpcPresetMergePatch(patch) {
    const issues = [];
    if (!patch || typeof patch !== "object") {
      issues.push({ severity: "error", message: "patch JSON object가 아니다." });
    } else {
      if (patch.kind !== "npcPresetMergePatch") issues.push({ severity: "error", message: "kind가 npcPresetMergePatch가 아니다." });
      if (!String(patch.npcId || "").trim()) issues.push({ severity: "warning", message: "npcId가 비어 있다." });
      if (!["replace", "append"].includes(patch.applyMode)) issues.push({ severity: "error", message: "applyMode는 replace 또는 append여야 한다." });
      if (!["preset_wins", "keep_current"].includes(patch.conflictMode)) issues.push({ severity: "error", message: "conflictMode는 preset_wins 또는 keep_current여야 한다." });
      const serviceRows = Array.isArray(patch.services) ? patch.services : [];
      const seedRows = Array.isArray(patch.questSeeds) ? patch.questSeeds : [];
      if (!Array.isArray(patch.services)) issues.push({ severity: "error", message: "services는 배열이어야 한다." });
      if (!Array.isArray(patch.questSeeds)) issues.push({ severity: "error", message: "questSeeds는 배열이어야 한다." });
      serviceRows.forEach((row, index) => {
        if (!row || typeof row !== "object") issues.push({ severity: "error", message: `services[${index}] row object가 없다.` });
        if (!String(row?.identity || "").trim()) issues.push({ severity: "warning", message: `services[${index}] identity가 비어 있다.` });
        if (!row || !("resolved" in row)) issues.push({ severity: "error", message: `services[${index}] resolved entry가 없다.` });
      });
      seedRows.forEach((row, index) => {
        if (!row || typeof row !== "object") issues.push({ severity: "error", message: `questSeeds[${index}] row object가 없다.` });
        if (!String(row?.identity || "").trim()) issues.push({ severity: "warning", message: `questSeeds[${index}] identity가 비어 있다.` });
        if (!row || !("resolved" in row)) issues.push({ severity: "error", message: `questSeeds[${index}] resolved entry가 없다.` });
      });
    }
    return {
      summary: {
        error: issues.filter((issue) => issue.severity === "error").length,
        warning: issues.filter((issue) => issue.severity === "warning").length,
        info: issues.filter((issue) => issue.severity === "info").length,
      },
      issues,
    };
  }

  return {
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
  };
}

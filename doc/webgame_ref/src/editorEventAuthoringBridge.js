export function createEditorEventAuthoringBridge(deps = {}) {
  const {
    getState = () => ({}),
    items = {},
    eventEffectTypes = new Set(),
    eventTriggerTypes = new Set(),
    resourceKeys = new Set(),
    companionStateKeys = new Set(),
    partyStatKeys = new Set(),
    classes = [],
  } = deps;

  function createEventBranchTemplate() {
    return {
      label: "새 분기",
      nextStepId: "",
    };
  }

  function createEventEffectTemplate(kind = "log") {
    if (kind === "set_flag") return { kind, flag: "" };
    if (kind === "set_quest_seed_state") return { kind, questSeedId: "", status: "active" };
    if (kind === "open_npc_service") return { kind, npcPlacementId: "", serviceIndex: 0 };
    if (kind === "grant_item") return { kind, itemId: Object.keys(items)[0] || "", quantity: 1 };
    if (kind === "consume_resource" || kind === "restore_resource") return { kind, resource: "gold", amount: 1 };
    if (kind === "damage_front" || kind === "heal_party" || kind === "grant_xp_party") return { kind, amount: 1 };
    if (kind === "cure_status_party" || kind === "add_status_front") return { kind, status: "" };
    return { kind: "log", message: "새 이벤트 로그" };
  }

  function matchesRequiredCompanionState(requiredState) {
    if (!requiredState) return true;
    const state = getState();
    const recruited = Boolean(state.companion?.recruited);
    const joinedParty = Boolean(state.companion?.recruited && state.companion?.joinedParty);
    if (requiredState === "absent") return !recruited;
    if (requiredState === "recruited") return recruited;
    if (requiredState === "joined_party") return joinedParty;
    if (requiredState === "dismissed") return recruited && !joinedParty;
    return false;
  }

  function eventEffectFieldMeta(effect = {}) {
    if (effect.kind === "log") return { note: "로그 한 줄을 남긴다.", fields: ["message"] };
    if (effect.kind === "set_flag") return { note: "flag를 설정하고 typed value를 저장한다.", fields: ["flag", "flagValueType", "flagValue"] };
    if (effect.kind === "set_quest_seed_state") return { note: "quest seed 상태를 active/completed/failed 같은 값으로 갱신한다.", fields: ["questSeedId", "status"] };
    if (effect.kind === "open_npc_service") return { note: "현재 floor의 NPC placement/service로 handoff 한다.", fields: ["npcPlacementId", "serviceIndex"] };
    if (effect.kind === "damage_front") return { note: "전열 대상에게 피해를 주고 min HP 바닥값을 둘 수 있다.", fields: ["amount", "minHp"] };
    if (effect.kind === "heal_party") return { note: "파티 전체를 amount만큼 회복한다.", fields: ["amount"] };
    if (effect.kind === "cure_status_party") return { note: "파티 전체에서 지정 status를 제거한다.", fields: ["status"] };
    if (effect.kind === "consume_resource") return { note: "지정 자원을 amount만큼 소모한다.", fields: ["resource", "amount"] };
    if (effect.kind === "restore_resource") return { note: "지정 자원을 amount만큼 회복한다.", fields: ["resource", "amount"] };
    if (effect.kind === "grant_xp_party") return { note: "파티 전체에 XP를 지급한다.", fields: ["amount"] };
    if (effect.kind === "grant_item") return { note: "아이템과 수량을 지정해 inventory에 지급한다.", fields: ["itemId", "quantity"] };
    if (effect.kind === "add_status_front") return { note: "전열 대상에게 status를 추가한다.", fields: ["status"] };
    if (effect.kind === "mark_done") return { note: "현재 placement를 done 상태로 표시한다.", fields: [] };
    return { note: "지원 effect contract를 확인한다.", fields: [] };
  }

  function prunePlacementOverrides(placement) {
    if (!placement?.eventOverrides) return;
    for (const key of ["usage", "detection", "disarm"]) {
      if (!placement.eventOverrides[key] || !Object.keys(placement.eventOverrides[key]).length) delete placement.eventOverrides[key];
    }
    if (!Object.keys(placement.eventOverrides).length) delete placement.eventOverrides;
  }

  function updatePlacementOverrides(placementId, updater) {
    const state = getState();
    const placement = state.map?.placements?.find((entry) => entry.id === placementId);
    if (!placement) return;
    placement.eventOverrides = placement.eventOverrides || {};
    updater(placement);
    prunePlacementOverrides(placement);
  }

  function parseJsonField(value, fallback) {
    const trimmed = value.trim();
    if (!trimmed) return fallback;
    return JSON.parse(trimmed);
  }

  function createEventValidationEntry(scope, message, extra = {}) {
    return { severity: extra.severity || "error", scope, message, ...extra };
  }

  function eventEffectValidationEntries(ownerLabel, effects, scope = "event") {
    const issues = [];
    if (effects != null && !Array.isArray(effects)) {
      issues.push(createEventValidationEntry(scope, `${ownerLabel} effects는 array여야 한다.`));
      return issues;
    }
    for (const [index, effect] of (effects || []).entries()) {
      if (!eventEffectTypes.has(effect?.kind)) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] kind가 registry에 없다: ${effect?.kind || "(empty)"}`, { effectIndex: index }));
      if (effect?.kind === "log" && !(effect.message || effect.text)) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] log에는 message/text가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "set_flag" && !effect.flag) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] set_flag에는 flag가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "set_quest_seed_state" && !effect.questSeedId) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] set_quest_seed_state에는 questSeedId가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "set_quest_seed_state" && effect.status != null && typeof effect.status !== "string") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] set_quest_seed_state status는 string이어야 한다.`, { effectIndex: index }));
      if (effect?.kind === "open_npc_service" && !effect.npcPlacementId) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] open_npc_service에는 npcPlacementId가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "open_npc_service" && effect.npcPlacementId != null && typeof effect.npcPlacementId !== "string") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] open_npc_service npcPlacementId는 string이어야 한다.`, { effectIndex: index }));
      if (effect?.kind === "open_npc_service" && effect.serviceIndex != null && typeof effect.serviceIndex !== "number") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] open_npc_service serviceIndex는 number여야 한다.`, { effectIndex: index }));
      if ((effect?.kind === "damage_front" || effect?.kind === "heal_party" || effect?.kind === "consume_resource") && typeof effect.amount !== "number") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] ${effect.kind}에는 number amount가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "damage_front" && effect.minHp != null && typeof effect.minHp !== "number") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] damage_front minHp는 number여야 한다.`, { effectIndex: index }));
      if ((effect?.kind === "grant_xp_party" || effect?.kind === "restore_resource") && typeof effect.amount !== "number") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] ${effect.kind}에는 number amount가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "consume_resource" && !effect.resource) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] consume_resource에는 resource가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "consume_resource" && effect.resource && !resourceKeys.has(effect.resource)) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] consume_resource resource가 지원되지 않는다: ${effect.resource}`, { effectIndex: index }));
      if (effect?.kind === "restore_resource" && !effect.resource) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] restore_resource에는 resource가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "restore_resource" && effect.resource && !resourceKeys.has(effect.resource)) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] restore_resource resource가 지원되지 않는다: ${effect.resource}`, { effectIndex: index }));
      if (effect?.kind === "grant_item" && !effect.itemId) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] grant_item에는 itemId가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "grant_item" && effect.itemId && !items[effect.itemId]) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] grant_item itemId가 지원되지 않는다: ${effect.itemId}`, { effectIndex: index }));
      if (effect?.kind === "grant_item" && effect.quantity != null && typeof effect.quantity !== "number") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] grant_item quantity는 number여야 한다.`, { effectIndex: index }));
      if ((effect?.kind === "cure_status_party" || effect?.kind === "add_status_front") && !effect.status) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] ${effect.kind}에는 status가 필요하다.`, { effectIndex: index }));
    }
    return issues;
  }

  function eventEffectValidationIssues(ownerLabel, effects) {
    return eventEffectValidationEntries(ownerLabel, effects).map((issue) => issue.message);
  }

  function eventStepValidationEntries(eventId, event) {
    const issues = [];
    if (event.entryStepId != null && typeof event.entryStepId !== "string") issues.push(createEventValidationEntry("event", `${eventId} entryStepId는 string이어야 한다.`));
    if (event.steps != null && !Array.isArray(event.steps)) issues.push(createEventValidationEntry("event", `${eventId} steps는 array여야 한다.`));
    const steps = Array.isArray(event.steps) ? event.steps : [];
    const stepIds = new Set();
    for (const [index, step] of steps.entries()) {
      const stepLabel = `${eventId} step[${index}]`;
      const stepScope = `step:${step.id || index}`;
      if (!step || typeof step !== "object") {
        issues.push(createEventValidationEntry(stepScope, `${stepLabel} 정의가 object가 아니다.`, { stepIndex: index }));
        continue;
      }
      if (!step.id) issues.push(createEventValidationEntry(stepScope, `${stepLabel} id가 비어 있다.`, { stepIndex: index }));
      else if (stepIds.has(step.id)) issues.push(createEventValidationEntry(stepScope, `${eventId} step id가 중복된다: ${step.id}`, { stepIndex: index }));
      else stepIds.add(step.id);
      if (step.text != null && typeof step.text !== "string") issues.push(createEventValidationEntry(stepScope, `${stepLabel} text는 string이어야 한다.`, { stepIndex: index }));
      if (step.title != null && typeof step.title !== "string") issues.push(createEventValidationEntry(stepScope, `${stepLabel} title은 string이어야 한다.`, { stepIndex: index }));
      if (step.nextStepId != null && typeof step.nextStepId !== "string") issues.push(createEventValidationEntry(stepScope, `${stepLabel} nextStepId는 string이어야 한다.`, { stepIndex: index }));
      if (step.branches != null && !Array.isArray(step.branches)) issues.push(createEventValidationEntry(stepScope, `${stepLabel} branches는 array여야 한다.`, { stepIndex: index }));
      if (step.choices != null && !Array.isArray(step.choices)) issues.push(createEventValidationEntry(stepScope, `${stepLabel} choices는 array여야 한다.`, { stepIndex: index }));
      for (const [branchIndex, branch] of (step.branches || []).entries()) {
        const branchLabel = `${stepLabel} branch[${branchIndex}]`;
        const branchScope = `${stepScope}:branch:${branchIndex}`;
        if (!branch || typeof branch !== "object") {
          issues.push(createEventValidationEntry(branchScope, `${branchLabel} 정의가 object가 아니다.`, { stepIndex: index, branchIndex }));
          continue;
        }
        if (!branch.nextStepId || typeof branch.nextStepId !== "string") issues.push(createEventValidationEntry(branchScope, `${branchLabel} nextStepId가 비어 있거나 string이 아니다.`, { stepIndex: index, branchIndex }));
        if (branch.label != null && typeof branch.label !== "string") issues.push(createEventValidationEntry(branchScope, `${branchLabel} label은 string이어야 한다.`, { stepIndex: index, branchIndex }));
        if (branch.requiredFlag != null && typeof branch.requiredFlag !== "string") issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredFlag는 string이어야 한다.`, { stepIndex: index, branchIndex }));
        if (branch.missingFlag != null && typeof branch.missingFlag !== "string") issues.push(createEventValidationEntry(branchScope, `${branchLabel} missingFlag는 string이어야 한다.`, { stepIndex: index, branchIndex }));
        if (branch.requiredResource != null && !resourceKeys.has(branch.requiredResource)) issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredResource가 지원되지 않는다: ${branch.requiredResource}`, { stepIndex: index, branchIndex }));
        if (branch.requiredResourceAtLeast != null && typeof branch.requiredResourceAtLeast !== "number") issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredResourceAtLeast는 number여야 한다.`, { stepIndex: index, branchIndex }));
        if (branch.requiredCompanionState != null && !companionStateKeys.has(branch.requiredCompanionState)) issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredCompanionState가 지원되지 않는다: ${branch.requiredCompanionState}`, { stepIndex: index, branchIndex }));
        if (branch.requiredClassIndex != null && (!Number.isInteger(branch.requiredClassIndex) || !classes[branch.requiredClassIndex])) issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredClassIndex가 지원되지 않는다: ${branch.requiredClassIndex}`, { stepIndex: index, branchIndex }));
        if (branch.requiredStatKey != null && !partyStatKeys.has(branch.requiredStatKey)) issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredStatKey가 지원되지 않는다: ${branch.requiredStatKey}`, { stepIndex: index, branchIndex }));
        if (branch.requiredStatAtLeast != null && typeof branch.requiredStatAtLeast !== "number") issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredStatAtLeast는 number여야 한다.`, { stepIndex: index, branchIndex }));
        if (branch.requiredQuestSeedId != null && typeof branch.requiredQuestSeedId !== "string") issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredQuestSeedId는 string이어야 한다.`, { stepIndex: index, branchIndex }));
        if (branch.requiredQuestSeedStatus != null && typeof branch.requiredQuestSeedStatus !== "string") issues.push(createEventValidationEntry(branchScope, `${branchLabel} requiredQuestSeedStatus는 string이어야 한다.`, { stepIndex: index, branchIndex }));
      }
      for (const [choiceIndex, choice] of (step.choices || []).entries()) {
        const choiceLabel = `${stepLabel} choice[${choiceIndex}]`;
        const choiceScope = `${stepScope}:choice:${choiceIndex}`;
        if (!choice || typeof choice !== "object") {
          issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} 정의가 object가 아니다.`, { stepIndex: index, choiceIndex }));
          continue;
        }
        if (!(choice.label || choice.text) || typeof (choice.label || choice.text) !== "string") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} label/text가 비어 있거나 string이 아니다.`, { stepIndex: index, choiceIndex }));
        if (choice.nextStepId != null && typeof choice.nextStepId !== "string") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} nextStepId는 string이어야 한다.`, { stepIndex: index, choiceIndex }));
        if (choice.requiredFlag != null && typeof choice.requiredFlag !== "string") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredFlag는 string이어야 한다.`, { stepIndex: index, choiceIndex }));
        if (choice.missingFlag != null && typeof choice.missingFlag !== "string") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} missingFlag는 string이어야 한다.`, { stepIndex: index, choiceIndex }));
        if (choice.requiredResource != null && !resourceKeys.has(choice.requiredResource)) issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredResource가 지원되지 않는다: ${choice.requiredResource}`, { stepIndex: index, choiceIndex }));
        if (choice.requiredResourceAtLeast != null && typeof choice.requiredResourceAtLeast !== "number") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredResourceAtLeast는 number여야 한다.`, { stepIndex: index, choiceIndex }));
        if (choice.requiredCompanionState != null && !companionStateKeys.has(choice.requiredCompanionState)) issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredCompanionState가 지원되지 않는다: ${choice.requiredCompanionState}`, { stepIndex: index, choiceIndex }));
        if (choice.requiredClassIndex != null && (!Number.isInteger(choice.requiredClassIndex) || !classes[choice.requiredClassIndex])) issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredClassIndex가 지원되지 않는다: ${choice.requiredClassIndex}`, { stepIndex: index, choiceIndex }));
        if (choice.requiredStatKey != null && !partyStatKeys.has(choice.requiredStatKey)) issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredStatKey가 지원되지 않는다: ${choice.requiredStatKey}`, { stepIndex: index, choiceIndex }));
        if (choice.requiredStatAtLeast != null && typeof choice.requiredStatAtLeast !== "number") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredStatAtLeast는 number여야 한다.`, { stepIndex: index, choiceIndex }));
        if (choice.requiredQuestSeedId != null && typeof choice.requiredQuestSeedId !== "string") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredQuestSeedId는 string이어야 한다.`, { stepIndex: index, choiceIndex }));
        if (choice.requiredQuestSeedStatus != null && typeof choice.requiredQuestSeedStatus !== "string") issues.push(createEventValidationEntry(choiceScope, `${choiceLabel} requiredQuestSeedStatus는 string이어야 한다.`, { stepIndex: index, choiceIndex }));
        issues.push(...eventEffectValidationEntries(choiceLabel, choice.effects || [], choiceScope));
      }
      issues.push(...eventEffectValidationEntries(stepLabel, step.effects || [], stepScope));
    }
    if (steps.length) {
      const entryStepId = event.entryStepId || steps[0]?.id;
      if (!entryStepId) issues.push(createEventValidationEntry("event", `${eventId} step graph에 entry step이 없다.`));
      else if (!stepIds.has(entryStepId)) issues.push(createEventValidationEntry("event", `${eventId} entryStepId가 존재하지 않는 step를 가리킨다: ${entryStepId}`));
      for (const [index, step] of steps.entries()) {
        const stepScope = `step:${step?.id || index}`;
        if (step?.nextStepId && !stepIds.has(step.nextStepId)) issues.push(createEventValidationEntry(stepScope, `${eventId} step[${index}] nextStepId가 존재하지 않는다: ${step.nextStepId}`, { stepIndex: index }));
        for (const [branchIndex, branch] of (step?.branches || []).entries()) {
          if (branch?.nextStepId && !stepIds.has(branch.nextStepId)) issues.push(createEventValidationEntry(`${stepScope}:branch:${branchIndex}`, `${eventId} step[${index}] branch[${branchIndex}] nextStepId가 존재하지 않는다: ${branch.nextStepId}`, { stepIndex: index, branchIndex }));
        }
        for (const [choiceIndex, choice] of (step?.choices || []).entries()) {
          if (choice?.nextStepId && !stepIds.has(choice.nextStepId)) issues.push(createEventValidationEntry(`${stepScope}:choice:${choiceIndex}`, `${eventId} step[${index}] choice[${choiceIndex}] nextStepId가 존재하지 않는다: ${choice.nextStepId}`, { stepIndex: index, choiceIndex }));
        }
      }
    }
    return issues;
  }

  function eventStepValidationIssues(eventId, event) {
    return eventStepValidationEntries(eventId, event).map((issue) => issue.message);
  }

  function buildEventValidationSnapshot(eventId, event, selectedStepIndex = 0) {
    const issues = eventDefinitionValidationEntries(eventId, event);
    const steps = Array.isArray(event?.steps) ? event.steps : [];
    const selectedStep = steps[Math.min(Math.max(0, Number(selectedStepIndex || 0)), Math.max(0, steps.length - 1))] || null;
    const selectedScopePrefix = selectedStep ? `step:${selectedStep.id || Math.min(Math.max(0, Number(selectedStepIndex || 0)), Math.max(0, steps.length - 1))}` : "";
    const selectedIssues = selectedScopePrefix ? issues.filter((issue) => issue.scope === selectedScopePrefix || issue.scope.startsWith(`${selectedScopePrefix}:`)) : [];
    return {
      summary: {
        error: issues.filter((issue) => issue.severity === "error").length,
        warning: issues.filter((issue) => issue.severity === "warning").length,
        info: issues.filter((issue) => issue.severity === "info").length,
      },
      issues,
      selectedStep,
      selectedIssues,
    };
  }

  function eventDefinitionValidationIssues(eventId, event) {
    return eventDefinitionValidationEntries(eventId, event).map((issue) => issue.message);
  }

  function eventDefinitionValidationEntries(eventId, event) {
    const issues = [];
    if (!event || typeof event !== "object") {
      issues.push(createEventValidationEntry("event", `${eventId} event 정의가 object가 아니다.`));
      return issues;
    }
    if (!event.type) issues.push(createEventValidationEntry("event", `${eventId} event.type이 비어 있다.`));
    if (event.interaction && !eventTriggerTypes.has(event.interaction)) issues.push(createEventValidationEntry("event", `${eventId} interaction이 지원되지 않는다: ${event.interaction}`));
    issues.push(...eventEffectValidationEntries(eventId, event.effects || [], "event:root"));
    issues.push(...eventStepValidationEntries(eventId, event));
    return issues;
  }

  function validateEventDefinitionsTable(definitions) {
    const issues = Object.entries(definitions || {}).flatMap(([eventId, event]) => eventDefinitionValidationIssues(eventId, event));
    if (issues.length) throw new Error(`eventDefinitions 검증 실패: ${issues[0]}`);
  }

  function effectJson(event) {
    return JSON.stringify(event?.effects || [], null, 2);
  }

  function eventStepsJson(event) {
    return JSON.stringify(event?.steps || [], null, 2);
  }

  function collectEventEffects(event) {
    const effects = [...(event?.effects || [])];
    for (const step of event?.steps || []) {
      effects.push(...(step?.effects || []));
      for (const choice of step?.choices || []) effects.push(...(choice?.effects || []));
    }
    return effects;
  }

  return {
    createEventBranchTemplate,
    createEventEffectTemplate,
    matchesRequiredCompanionState,
    eventEffectFieldMeta,
    prunePlacementOverrides,
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
  };
}

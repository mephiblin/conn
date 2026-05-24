function required(name) {
  throw new Error(`eventRuntime dependency missing: ${name}`);
}

function cloneJson(value) {
  return JSON.parse(JSON.stringify(value));
}

export function createEventRuntime(deps = {}) {
  const getState = deps.getState || (() => required("getState"));
  const addLog = deps.addLog || (() => required("addLog"));
  const render = deps.render || (() => required("render"));
  const closeInteraction = deps.closeInteraction || (() => required("closeInteraction"));
  const releasePointerLook = deps.releasePointerLook || (() => required("releasePointerLook"));
  const resolvePlacementEvent = deps.resolvePlacementEvent || (() => required("resolvePlacementEvent"));
  const eventStepMap = deps.eventStepMap || (() => required("eventStepMap"));
  const stepChoiceMatches = deps.stepChoiceMatches || (() => true);
  const stepBranchMatches = deps.stepBranchMatches || (() => false);
  const currentEditorEventTestSession = deps.currentEditorEventTestSession || (() => null);
  const flushPendingNpcHandoff = deps.flushPendingNpcHandoff || (() => false);
  const queueNpcHandoff = deps.queueNpcHandoff || (() => false);
  const livingParty = deps.livingParty || (() => []);
  const setQuestSeedState = deps.setQuestSeedState || (() => required("setQuestSeedState"));
  const pushInventoryItemId = deps.pushInventoryItemId || (() => required("pushInventoryItemId"));
  const items = deps.items || {};
  const afterAdvanceWorldTurn = deps.afterAdvanceWorldTurn || (() => {});

  function state() {
    return getState();
  }

  function openEventChoiceInteraction(placement, event, step) {
    releasePointerLook();
    state().interaction = {
      type: "event",
      placementId: placement.id,
      eventId: placement.interaction?.eventId || placement.refId,
      stepId: step.id,
      testSessionId: placement.testSessionId || null,
      title: step.title || event.name,
      text: step.text || "",
      options: (step.choices || [])
        .filter(stepChoiceMatches)
        .map((choice, index) => ({
          id: `event_choice_${index}`,
          label: choice.label || choice.text || `선택지 ${index + 1}`,
          choiceIndex: index,
        })),
    };
  }

  function continueEventFlow(placement, event, runtime, startStepId) {
    if (!Array.isArray(event?.steps) || !event.steps.length) {
      applyEventEffects(placement, event, event?.effects || []);
      spendEventUsage(placement, runtime, event);
      return { handled: true, pending: false };
    }
    const stepMap = eventStepMap(event);
    const visited = new Set();
    let currentStepId = startStepId || event.entryStepId || event.steps[0]?.id || "";
    let guard = 0;
    while (currentStepId && stepMap.has(currentStepId) && guard < 32) {
      if (visited.has(currentStepId)) break;
      visited.add(currentStepId);
      const step = stepMap.get(currentStepId);
      const availableChoices = (step?.choices || []).filter(stepChoiceMatches);
      if (availableChoices.length) {
        openEventChoiceInteraction(placement, event, step);
        return { handled: true, pending: true };
      }
      if (step?.text) addLog(step.text);
      applyEventEffects(placement, event, step?.effects || []);
      const branch = (step?.branches || []).find((entry) => stepBranchMatches(entry));
      currentStepId = branch?.nextStepId || step?.nextStepId || "";
      guard += 1;
    }
    spendEventUsage(placement, runtime, event);
    return { handled: true, pending: false };
  }

  function resolveEventChoice(choiceIndex) {
    const currentState = state();
    const interaction = currentState.interaction;
    if (!interaction || interaction.type !== "event") return false;
    const testSession = interaction.testSessionId ? currentEditorEventTestSession() : null;
    const placement = testSession?.id === interaction.testSessionId
      ? testSession.placement
      : currentState.map.placements.find((entry) => entry.id === interaction.placementId);
    const event = testSession?.id === interaction.testSessionId
      ? testSession.event
      : placement ? resolvePlacementEvent(placement) : null;
    if (!placement || !event) {
      closeInteraction();
      return false;
    }
    const runtime = testSession?.id === interaction.testSessionId
      ? testSession.runtime
      : eventUsageState(placement, event);
    const step = eventStepMap(event).get(interaction.stepId);
    const choice = step?.choices?.[choiceIndex];
    if (!step || !choice) {
      closeInteraction();
      return false;
    }
    if (step.text) addLog(step.text);
    applyEventEffects(placement, event, choice.effects || []);
    closeInteraction();
    continueEventFlow(placement, event, runtime, choice.nextStepId || "");
    if (testSession?.id === interaction.testSessionId && !state().interaction) {
      state().editor.eventTestSession = { ...testSession, completed: true };
    }
    if (!state().interaction) flushPendingNpcHandoff();
    if (!state().interaction && state().mode === "dungeon") advanceWorldTurn();
    render();
    return true;
  }

  function applyEventEffects(placement, event, effects = event.effects || []) {
    const target = livingParty()[0];
    for (const effect of effects || []) {
      if (!effect?.kind) continue;
      if (effect.kind === "log") {
        addLog(effect.message || effect.text || `${event.name}의 여파가 퍼진다.`);
        continue;
      }
      if (effect.kind === "set_flag") {
        if (effect.flag) state().flags[effect.flag] = effect.value ?? true;
        continue;
      }
      if (effect.kind === "set_quest_seed_state") {
        if (effect.questSeedId) setQuestSeedState(effect.questSeedId, effect.status || "active");
        continue;
      }
      if (effect.kind === "open_npc_service") {
        queueNpcHandoff(effect, placement);
        continue;
      }
      if (effect.kind === "damage_front") {
        if (!target) continue;
        const amount = Math.max(0, Number(effect.amount || 0));
        const minHp = effect.minHp == null ? 0 : Number(effect.minHp);
        target.hp = Math.max(minHp, target.hp - amount);
        continue;
      }
      if (effect.kind === "heal_party") {
        const amount = Math.max(0, Number(effect.amount || 0));
        for (const hero of livingParty()) hero.hp = Math.min(hero.maxHp, hero.hp + amount);
        continue;
      }
      if (effect.kind === "cure_status_party") {
        const status = effect.status;
        if (!status) continue;
        for (const hero of livingParty()) hero.status = hero.status.filter((entry) => entry !== status);
        continue;
      }
      if (effect.kind === "consume_resource") {
        const resource = effect.resource;
        if (!resource || !(resource in state().resources)) continue;
        state().resources[resource] = Math.max(0, state().resources[resource] - Math.max(0, Number(effect.amount || 0)));
        continue;
      }
      if (effect.kind === "restore_resource") {
        const resource = effect.resource;
        if (!resource || !(resource in state().resources)) continue;
        state().resources[resource] += Math.max(0, Number(effect.amount || 0));
        continue;
      }
      if (effect.kind === "grant_xp_party") {
        const amount = Math.max(0, Number(effect.amount || 0));
        for (const hero of state().party) hero.xp = Math.max(0, Number(hero.xp || 0) + amount);
        continue;
      }
      if (effect.kind === "grant_item") {
        if (!effect.itemId || !items[effect.itemId]) continue;
        const quantity = Math.max(1, Number(effect.quantity || 1));
        for (let index = 0; index < quantity; index += 1) pushInventoryItemId(effect.itemId);
        continue;
      }
      if (effect.kind === "add_status_front") {
        if (!target || !effect.status) continue;
        target.status.push(effect.status);
        continue;
      }
      if (effect.kind === "mark_done") placement.done = true;
    }
  }

  function runTypedEventEffects(placement, event, runtime) {
    if (!event?.type) return false;
    const result = continueEventFlow(placement, event, runtime, "");
    if (!state().interaction) flushPendingNpcHandoff();
    return Boolean(result?.handled);
  }

  function canDetectTrap() {
    return livingParty().some((hero) => hero.category === "traps" || hero.category === "lore");
  }

  function canDisarmTrap() {
    return livingParty().some((hero) => hero.category === "traps" || hero.category === "ritual");
  }

  function ensureEventRuntimeState(placement) {
    if (!placement.eventRuntime) placement.eventRuntime = {};
    return placement.eventRuntime;
  }

  function eventUsageState(placement, event) {
    const runtime = ensureEventRuntimeState(placement);
    if (runtime.usesRemaining == null && typeof event?.usage?.usesRemaining === "number") {
      runtime.usesRemaining = event.usage.usesRemaining;
    }
    if (runtime.cooldownRemaining == null) runtime.cooldownRemaining = 0;
    if (runtime.detected == null) runtime.detected = false;
    if (runtime.disarmed == null) runtime.disarmed = false;
    return runtime;
  }

  function spendEventUsage(placement, runtime, event) {
    if (event?.usage?.mode === "uses" && typeof runtime.usesRemaining === "number") {
      runtime.usesRemaining = Math.max(0, runtime.usesRemaining - 1);
      if (runtime.usesRemaining === 0) placement.done = true;
    }
    if (event?.usage?.mode === "cooldown") {
      runtime.cooldownRemaining = Math.max(0, Number(event.usage.cooldownSteps || 0)) + 1;
    }
  }

  function advanceWorldTurn() {
    for (const map of Object.values(state().floorMaps || {})) {
      for (const placement of map.placements || []) {
        if (!placement.eventRuntime || typeof placement.eventRuntime.cooldownRemaining !== "number") continue;
        if (placement.eventRuntime.cooldownRemaining > 0) placement.eventRuntime.cooldownRemaining -= 1;
      }
    }
    afterAdvanceWorldTurn();
  }

  function triggerTrap(placement, eventId = "event_trap_poison_dart") {
    placement.done = true;
    const target = livingParty()[0];
    if (!target) return;
    target.hp = Math.max(1, target.hp - 6);
    if (eventId === "event_trap_poison_dart") {
      target.status.push("독");
      addLog(`${target.name}이 독침 함정에 당했다.`);
      return;
    }
    if (eventId === "event_trap_bleed_blade") {
      target.status.push("출혈");
      addLog(`${target.name}이 숨겨진 칼날 함정에 베였다.`);
      return;
    }
    if (eventId === "event_trap_curse_rune") {
      target.status.push("저주");
      addLog(`${target.name}이 저주 룬의 검은 불꽃에 휩싸였다.`);
      return;
    }
    addLog(`${target.name}이 함정에 걸렸다.`);
  }

  function runEventPlacement(placement, triggerType) {
    const event = resolvePlacementEvent(placement);
    if (!event) {
      addLog(`${placement.id} 이벤트를 찾지 못했다.`);
      return false;
    }
    const runtime = eventUsageState(placement, event);
    if (event.interaction && triggerType && event.interaction !== triggerType) return false;
    if (runtime.disarmed) {
      addLog(`${event.name}은 이미 해제됐다.`);
      return true;
    }
    if (event.usage?.mode === "cooldown" && runtime.cooldownRemaining > 0) {
      addLog(`${event.name}은 아직 ${runtime.cooldownRemaining}턴 동안 잠잠하지 않다.`);
      return true;
    }
    if (typeof runtime.usesRemaining === "number" && runtime.usesRemaining <= 0) {
      addLog(`${event.name}은 더 이상 힘을 내지 못한다.`);
      return true;
    }
    if (runTypedEventEffects(placement, event, runtime)) return true;
    addLog(`${event.name} 이벤트 처리기가 아직 없다.`);
    return false;
  }

  function startEditorEventTestSession(eventId, event, startStepId = "") {
    if (!eventId || !event) return false;
    const session = {
      id: `event_test_${Date.now()}`,
      eventId,
      event: cloneJson(event),
      placement: {
        id: `event_test_placement_${Date.now()}`,
        kind: "event_trigger",
        refId: eventId,
        interaction: { type: event.interaction || "interact", eventId },
        eventRuntime: {},
        testSessionId: `event_test_${Date.now()}`,
      },
      runtime: {},
      startedAt: new Date().toISOString(),
      startStepId: startStepId || event.entryStepId || event.steps?.[0]?.id || "",
      completed: false,
    };
    session.id = session.placement.testSessionId;
    session.runtime = eventUsageState(session.placement, session.event);
    state().editor.eventTestSession = session;
    closeInteraction();
    continueEventFlow(session.placement, session.event, session.runtime, startStepId || "");
    if (!state().interaction) state().editor.eventTestSession.completed = true;
    return true;
  }

  return {
    openEventChoiceInteraction,
    continueEventFlow,
    resolveEventChoice,
    applyEventEffects,
    runTypedEventEffects,
    canDetectTrap,
    canDisarmTrap,
    ensureEventRuntimeState,
    eventUsageState,
    spendEventUsage,
    advanceWorldTurn,
    triggerTrap,
    runEventPlacement,
    startEditorEventTestSession,
  };
}

export function createEditorNpcEventStateBridge(deps = {}) {
  const {
    getState = () => ({}),
    normalizeQuestState = (quest) => quest,
    pushInventoryItemId = () => null,
    addLog = () => {},
    items = {},
    vendors = {},
    classes = [],
    npcs = {},
    eventDefinitions = {},
    defaultEventSelection = (selection) => selection || {},
    editorEventSelectionDefaults = () => ({}),
    eventEditorToPlacementKind = {},
    eventObjectPlacementKinds = new Set(),
    getNow = () => new Date().toISOString(),
  } = deps;

  function activeEventEditorTool() {
    const state = getState();
    return eventEditorToPlacementKind[state.editorTool] ? state.editorTool : (state.eventInspectorTool || "trap");
  }

  function activeEventDefinitionId() {
    const state = getState();
    const tool = activeEventEditorTool();
    return tool ? state.selectedEventDefinitionIds?.[tool] || defaultEventSelection(editorEventSelectionDefaults())[tool] : "";
  }

  function activeEventDefinition() {
    const eventId = activeEventDefinitionId();
    return eventDefinitions[eventId] || null;
  }

  function updateEventDefinition(eventId, updater) {
    if (!eventId || !eventDefinitions[eventId]) return;
    updater(eventDefinitions[eventId]);
  }

  function activeNpcDefinitionId() {
    const state = getState();
    return npcs[state.selectedNpcDefinitionId] ? state.selectedNpcDefinitionId : Object.keys(npcs)[0] || "";
  }

  function activeNpcDefinition() {
    const npcId = activeNpcDefinitionId();
    return npcs[npcId] || null;
  }

  function updateNpcDefinition(npcId, updater) {
    if (!npcId || !npcs[npcId]) return;
    updater(npcs[npcId]);
  }

  function activeNpcQuestSeed(npc) {
    const state = getState();
    const defeated = Object.keys(state.quest?.bossesDefeated || {}).length;
    return (npc?.questSeeds || [])
      .filter((seed) => defeated >= Number(seed.bossesDefeatedAtLeast || 0))
      .filter((seed) => !seed.requiredFlag || state.flags?.[seed.requiredFlag])
      .filter((seed) => state.quest?.seeds?.[seed.id]?.status !== "completed")
      .sort((a, b) => Number(b.bossesDefeatedAtLeast || 0) - Number(a.bossesDefeatedAtLeast || 0))[0] || null;
  }

  function ensureQuestSeedState(seedId, defaults = {}) {
    const state = getState();
    state.quest = normalizeQuestState(state.quest);
    if (!state.quest.seeds[seedId]) {
      state.quest.seeds[seedId] = {
        status: defaults.status || "active",
        title: defaults.title || seedId,
        note: defaults.note || "",
        objectives: JSON.parse(JSON.stringify(defaults.objectives || [])),
        rewards: JSON.parse(JSON.stringify(defaults.rewards || {})),
        failureFlag: defaults.failureFlag || null,
        sourceNpcId: defaults.sourceNpcId || null,
        activatedAt: defaults.activatedAt || getNow(),
        completedAt: defaults.completedAt || null,
        failedAt: defaults.failedAt || null,
        rewardsGranted: Boolean(defaults.rewardsGranted),
      };
    }
    return state.quest.seeds[seedId];
  }

  function activateQuestSeed(seed, npcId = null) {
    const state = getState();
    if (!seed?.id) return null;
    const runtime = ensureQuestSeedState(seed.id, {
      status: "active",
      title: seed.title || seed.id,
      note: seed.note || "",
      objectives: seed.objectives || [],
      rewards: seed.rewards || {},
      failureFlag: seed.failureFlag || null,
      sourceNpcId: npcId,
    });
    runtime.status = runtime.status === "completed" ? "completed" : "active";
    runtime.title = seed.title || runtime.title;
    runtime.note = seed.note || runtime.note;
    runtime.objectives = JSON.parse(JSON.stringify(seed.objectives || runtime.objectives || []));
    runtime.rewards = JSON.parse(JSON.stringify(seed.rewards || runtime.rewards || {}));
    runtime.failureFlag = seed.failureFlag || runtime.failureFlag || null;
    if (!runtime.sourceNpcId && npcId) runtime.sourceNpcId = npcId;
    if (!runtime.activatedAt) runtime.activatedAt = getNow();
    if (seed.grantFlag) state.flags[seed.grantFlag] = true;
    return runtime;
  }

  function grantQuestSeedRewards(runtime) {
    const state = getState();
    if (!runtime || runtime.rewardsGranted) return;
    const rewards = runtime.rewards || {};
    const gold = Math.max(0, Number(rewards.gold || 0));
    const xp = Math.max(0, Number(rewards.xp || 0));
    const itemsToGrant = Array.isArray(rewards.items) ? rewards.items : [];
    if (gold > 0) state.resources.gold += gold;
    if (xp > 0) {
      for (const hero of state.party) hero.xp = Math.max(0, Number(hero.xp || 0) + xp);
    }
    for (const reward of itemsToGrant) {
      if (typeof reward === "string") pushInventoryItemId(reward);
      else if (reward?.itemId) {
        const quantity = Math.max(1, Number(reward.quantity || 1));
        for (let index = 0; index < quantity; index += 1) pushInventoryItemId(reward.itemId);
      }
    }
    if (rewards.flag) state.flags[rewards.flag] = rewards.value ?? true;
    runtime.rewardsGranted = true;
    const rewardParts = [
      gold > 0 ? `금화 ${gold}` : "",
      xp > 0 ? `XP ${xp}` : "",
      itemsToGrant.length ? `아이템 ${itemsToGrant.map((entry) => {
        if (typeof entry === "string") return items[entry]?.name || entry;
        const itemName = items[entry?.itemId]?.name || entry?.itemId || "아이템";
        return entry?.quantity > 1 ? `${itemName} x${entry.quantity}` : itemName;
      }).join(", ")}` : "",
    ].filter(Boolean);
    if (rewardParts.length) addLog(`${runtime.title} 보상 획득: ${rewardParts.join(" · ")}`);
  }

  function setQuestSeedState(seedId, status = "active") {
    const runtime = ensureQuestSeedState(seedId, { status });
    runtime.status = status || "active";
    if (runtime.status === "completed") {
      if (!runtime.completedAt) runtime.completedAt = getNow();
      grantQuestSeedRewards(runtime);
    }
    if (runtime.status === "failed" && !runtime.failedAt) runtime.failedAt = getNow();
    return runtime;
  }

  function syncQuestSeedFailureStates() {
    const state = getState();
    if (!state.quest?.seeds) return;
    Object.values(state.quest.seeds).forEach((runtime) => {
      if (!runtime?.failureFlag || runtime.status === "completed" || runtime.status === "failed") return;
      if (state.flags[runtime.failureFlag]) {
        runtime.status = "failed";
        if (!runtime.failedAt) runtime.failedAt = getNow();
        addLog(`${runtime.title} 퀘스트가 실패 상태로 전환됐다.`);
      }
    });
  }

  function questSeedJson(npc) {
    return JSON.stringify(npc?.questSeeds || [], null, 2);
  }

  function questSeedRewardItemsText(seed) {
    const itemsToGrant = Array.isArray(seed?.rewards?.items) ? seed.rewards.items : [];
    return itemsToGrant.map((entry) => {
      if (typeof entry === "string") return entry;
      if (!entry?.itemId) return "";
      return Number(entry.quantity || 1) > 1 ? `${entry.itemId}:${entry.quantity}` : entry.itemId;
    }).filter(Boolean).join(", ");
  }

  function questRewardFlagValueType(rewards = {}) {
    if (rewards?.value === false) return "boolean_false";
    if (typeof rewards?.value === "number") return "number";
    if (typeof rewards?.value === "string") return "string";
    return "boolean_true";
  }

  function questRewardFlagValueText(rewards = {}) {
    if (typeof rewards?.value === "number") return String(rewards.value);
    if (typeof rewards?.value === "string") return rewards.value;
    return "";
  }

  function uniqueNpcQuestSeedId(npc, baseId = "quest_seed_custom") {
    const slug = (baseId || "quest_seed_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "quest_seed_custom";
    const existing = new Set((npc?.questSeeds || []).map((seed) => seed?.id).filter(Boolean));
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createQuestSeedTemplate(npc) {
    const seedId = uniqueNpcQuestSeedId(npc);
    return {
      id: seedId,
      title: `새 의뢰 ${((npc?.questSeeds || []).length || 0) + 1}`,
      note: "",
      objectives: ["새 목표를 입력한다."],
      rewards: {
        gold: 0,
        xp: 0,
        items: [],
      },
      bossesDefeatedAtLeast: 0,
      grantFlag: seedId,
    };
  }

  function duplicateQuestSeedTemplate(npc, seed) {
    const clone = JSON.parse(JSON.stringify(seed || {}));
    const nextId = uniqueNpcQuestSeedId(npc, `${seed?.id || "quest_seed_custom"}_copy`);
    clone.id = nextId;
    clone.title = clone.title ? `${clone.title} 사본` : "새 의뢰 사본";
    if (clone.grantFlag === seed?.id || clone.grantFlag === seed?.grantFlag) clone.grantFlag = nextId;
    return clone;
  }

  function activeNpcQuestSeedDefinition(npc) {
    const state = getState();
    const seeds = Array.isArray(npc?.questSeeds) ? npc.questSeeds : [];
    if (!seeds.length) return { seed: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedNpcQuestSeedIndex || 0)), seeds.length - 1);
    return { seed: seeds[index], index };
  }

  function createNpcServiceTemplate(type = "talk") {
    if (type === "quest") return { type: "quest", label: "의뢰를 확인한다" };
    if (type === "heal") return { type: "heal", label: "상처를 돌본다", heal: 6, cost: { gold: 0 } };
    if (type === "identify") return { type: "identify", label: "유물을 감정한다", cost: { gold: 0 } };
    if (type === "trade") return { type: "trade", label: "거래를 연다", vendorId: Object.keys(vendors)[0] || "" };
    if (type === "recruit") {
      return {
        type: "recruit",
        label: "동행을 제안한다",
        companionProfile: { name: "새 동료", classIndex: 0, note: "" },
      };
    }
    if (type === "dismiss") return { type: "dismiss", label: "동행을 해산한다" };
    if (type === "fight") return { type: "fight", label: "칼을 뽑는다", encounterId: "" };
    return { type: "talk", label: "대화를 나눈다" };
  }

  function createNpcServiceGroupTemplates(group = "quest_hub") {
    if (group === "quest_hub") {
      return [
        { ...createNpcServiceTemplate("quest"), label: "의뢰를 확인한다" },
        { ...createNpcServiceTemplate("talk"), label: "단서를 묻는다" },
        { ...createNpcServiceTemplate("identify"), label: "유물을 감정한다", cost: { gold: 5 } },
      ];
    }
    if (group === "support") {
      return [
        { ...createNpcServiceTemplate("heal"), label: "상처를 돌본다", heal: 8, cost: { gold: 6 } },
        { ...createNpcServiceTemplate("identify"), label: "저주를 살핀다", cost: { gold: 4 } },
      ];
    }
    if (group === "merchant") {
      return [
        { ...createNpcServiceTemplate("talk"), label: "물건 이야기를 묻는다" },
        { ...createNpcServiceTemplate("trade"), label: "거래를 연다" },
      ];
    }
    if (group === "companion") {
      return [
        { ...createNpcServiceTemplate("talk"), label: "동행 조건을 묻는다" },
        { ...createNpcServiceTemplate("recruit"), label: "동행을 제안한다" },
        { ...createNpcServiceTemplate("dismiss"), label: "동행을 해산한다" },
      ];
    }
    if (group === "hostile") {
      return [
        { ...createNpcServiceTemplate("talk"), label: "통행 조건을 묻는다" },
        { ...createNpcServiceTemplate("fight"), label: "칼을 뽑는다" },
      ];
    }
    return [createNpcServiceTemplate("talk")];
  }

  function activeNpcServiceDefinition(npc) {
    const state = getState();
    const services = Array.isArray(npc?.services) ? npc.services : [];
    if (!services.length) return { service: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), services.length - 1);
    return { service: services[index], index };
  }

  function createNpcDialogueStepTemplate(service = {}) {
    const steps = Array.isArray(service?.dialogue?.steps) ? service.dialogue.steps : [];
    let counter = steps.length + 1;
    let id = `dialogue_step_${counter}`;
    const used = new Set(steps.map((step) => step?.id).filter(Boolean));
    while (used.has(id)) {
      counter += 1;
      id = `dialogue_step_${counter}`;
    }
    return {
      id,
      text: "새 대화 단계를 입력한다.",
      choices: [],
    };
  }

  function createNpcDialogueChoiceTemplate(step = {}) {
    return {
      label: `선택지 ${(step?.choices || []).length + 1}`,
      nextStepId: "",
      note: "",
    };
  }

  function activeNpcDialogueStepDefinition(service) {
    const state = getState();
    const steps = Array.isArray(service?.dialogue?.steps) ? service.dialogue.steps : [];
    if (!steps.length) return { step: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedNpcDialogueStepIndex || 0)), steps.length - 1);
    return { step: steps[index], index };
  }

  function npcPlacementsAtCursor() {
    const state = getState();
    return state.map.placements.filter((placement) => placement.position?.x === state.editorCursor.x
      && placement.position?.y === state.editorCursor.y
      && placement.kind === "npc");
  }

  function activeNpcPlacement() {
    const state = getState();
    const placements = npcPlacementsAtCursor();
    if (!placements.length) return null;
    if (state.selectedNpcPlacementId) {
      const matched = placements.find((placement) => placement.id === state.selectedNpcPlacementId);
      if (matched) return matched;
    }
    return placements[0];
  }

  function activeNpcPlacementDefinition() {
    const placement = activeNpcPlacement();
    return placement?.npcId ? npcs[placement.npcId] || null : null;
  }

  function allowedInteractionsForPlacementKind(kind) {
    if (kind === "trap") return ["onEnter"];
    if (kind === "event_trigger") return ["interact", "onEnter", "onExit"];
    if (kind === "rest_site") return ["interact", "onRest"];
    if (kind === "camp") return ["interact", "onCamp"];
    return ["interact"];
  }

  function defaultInteractionTypeForPlacementKind(kind) {
    if (kind === "rest_site") return "onRest";
    if (kind === "camp") return "onCamp";
    return allowedInteractionsForPlacementKind(kind)[0] || "interact";
  }

  function focusedNpcClassIndices(npc) {
    return (npc?.progressionHooks?.focusClassIndices || []).filter((classIndex) => Number.isInteger(classIndex) && classes[classIndex]);
  }

  function focusedNpcClassNames(npc) {
    return focusedNpcClassIndices(npc).map((classIndex) => classes[classIndex]?.cls).filter(Boolean);
  }

  function activeNpcQuestHook(npc) {
    const state = getState();
    const defeated = Object.keys(state.quest?.bossesDefeated || {}).length;
    return (npc?.questHooks || [])
      .filter((hook) => defeated >= Number(hook.bossesDefeatedAtLeast || 0))
      .sort((a, b) => Number(b.bossesDefeatedAtLeast || 0) - Number(a.bossesDefeatedAtLeast || 0))[0] || null;
  }

  function createNpcQuestHookTemplate() {
    return {
      bossesDefeatedAtLeast: 0,
      note: "새 quest hook 메모를 입력한다.",
    };
  }

  function compatibleEventDefinitions(kind) {
    const allowed = new Set(allowedInteractionsForPlacementKind(kind));
    return Object.entries(eventDefinitions).filter(([, event]) => allowed.has(event?.interaction || "interact"));
  }

  function uniqueEventPresetId(baseId = "event_custom") {
    const slug = (baseId || "event_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "event_custom";
    let candidate = slug;
    let index = 1;
    while (eventDefinitions[candidate]) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function updateEventReferences(oldId, newId) {
    const state = getState();
    Object.keys(state.selectedEventDefinitionIds || {}).forEach((tool) => {
      if (state.selectedEventDefinitionIds[tool] === oldId) state.selectedEventDefinitionIds[tool] = newId;
    });
    for (const map of Object.values(state.floorMaps || {})) {
      for (const placement of map.placements || []) {
        if ((placement.refId === oldId || placement.interaction?.eventId === oldId) && eventObjectPlacementKinds.has(placement.kind)) {
          placement.refId = newId;
          placement.interaction = {
            ...(placement.interaction || {}),
            eventId: newId,
          };
        }
      }
    }
  }

  function renameEventPreset(oldId, nextId) {
    const normalizedId = nextId.trim();
    if (!oldId || !normalizedId || oldId === normalizedId) return true;
    if (eventDefinitions[normalizedId]) return false;
    const definition = eventDefinitions[oldId];
    if (!definition) return false;
    eventDefinitions[normalizedId] = JSON.parse(JSON.stringify(definition));
    delete eventDefinitions[oldId];
    updateEventReferences(oldId, normalizedId);
    return true;
  }

  function createEventPresetFromDefinition(baseDefinition, baseId, selectTool, name) {
    const state = getState();
    const nextId = uniqueEventPresetId(baseId);
    eventDefinitions[nextId] = {
      ...JSON.parse(JSON.stringify(baseDefinition)),
      name: name || `${baseDefinition?.name || "새 이벤트"} 사본`,
    };
    if (selectTool) state.selectedEventDefinitionIds[selectTool] = nextId;
    return nextId;
  }

  function uniqueEventStepId(event, baseId = "step_start") {
    const slug = (baseId || "step_start").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "step_start";
    const used = new Set((event?.steps || []).map((step) => step?.id).filter(Boolean));
    let candidate = slug;
    let index = 1;
    while (used.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createEventStepTemplate(event) {
    return {
      id: uniqueEventStepId(event, "step_custom"),
      title: "새 단계",
      text: "",
      effects: [],
      branches: [],
      choices: [],
      nextStepId: "",
    };
  }

  function createEventChoiceTemplate() {
    return {
      label: "새 선택지",
      nextStepId: "",
      effects: [],
    };
  }

  function createEventGraphTemplate(templateId, eventDefId, eventTool) {
    if (templateId === "altar_choice") {
      return {
        name: "제단 선택 템플릿",
        interaction: "interact",
        effects: [],
        entryStepId: "step_altar_intro",
        steps: [
          {
            id: "step_altar_intro",
            title: "제단 앞",
            text: "제단은 피와 금화 중 하나를 요구하는 듯 낮게 진동한다.",
            choices: [
              { label: "피를 바친다", nextStepId: "step_altar_blood", effects: [{ kind: "damage_front", amount: 3, minHp: 1 }] },
              { label: "금화를 바친다", nextStepId: "step_altar_gold", effects: [{ kind: "consume_resource", resource: "gold", amount: 8 }] },
            ],
          },
          {
            id: "step_altar_blood",
            title: "피의 응답",
            text: "제단이 붉게 빛나며 짧은 축복을 흘린다.",
            effects: [{ kind: "grant_xp_party", amount: 4 }, { kind: "mark_done" }],
          },
          {
            id: "step_altar_gold",
            title: "금속의 메아리",
            text: "금속 조각이 제단 속으로 스며들며 길이 잠시 열린다.",
            effects: [{ kind: "set_flag", flag: `${eventDefId || "event"}_altar_paid`, value: true }, { kind: "mark_done" }],
          },
        ],
      };
    }
    if (templateId === "trap_resolution") {
      return {
        name: "함정 해제 템플릿",
        interaction: eventTool === "trap" ? "onEnter" : "interact",
        effects: [],
        entryStepId: "step_trap_intro",
        steps: [
          {
            id: "step_trap_intro",
            title: "함정 발견",
            text: "바닥 홈 사이로 독침 장치가 보인다.",
            branches: [
              { label: "already detected", requiredFlag: `${eventDefId || "event"}_trap_safe`, nextStepId: "step_trap_safe" },
            ],
            nextStepId: "step_trap_fire",
          },
          {
            id: "step_trap_fire",
            title: "함정 발동",
            text: "독침이 전열로 튀어 오른다.",
            effects: [{ kind: "damage_front", amount: 5, minHp: 1 }, { kind: "add_status_front", status: "독" }, { kind: "mark_done" }],
          },
          {
            id: "step_trap_safe",
            title: "안전 통과",
            text: "이미 구조를 파악해 함정을 비껴 지나간다.",
            effects: [{ kind: "mark_done" }],
          },
        ],
      };
    }
    if (templateId === "npc_handoff") {
      return {
        name: "NPC handoff 템플릿",
        interaction: "interact",
        effects: [],
        entryStepId: "step_handoff_intro",
        steps: [
          {
            id: "step_handoff_intro",
            title: "부름",
            text: "기척이 벽 너머로 흘러가며 누군가를 호출한다.",
            choices: [
              { label: "호출을 따른다", nextStepId: "step_handoff_open" },
              { label: "지금은 지나친다", nextStepId: "step_handoff_skip" },
            ],
          },
          {
            id: "step_handoff_open",
            title: "등장",
            text: "기다리던 인물이 모습을 드러낸다.",
            effects: [{ kind: "open_npc_service", npcPlacementId: "", serviceIndex: 0 }, { kind: "mark_done" }],
          },
          {
            id: "step_handoff_skip",
            title: "유보",
            text: "기척은 다시 벽 너머로 사라진다.",
            effects: [{ kind: "mark_done" }],
          },
        ],
      };
    }
    return {
      name: "새 이벤트 그래프",
      interaction: defaultInteractionTypeForPlacementKind(eventEditorToPlacementKind[eventTool] || "event_trigger"),
      effects: [],
      entryStepId: "step_start",
      steps: [
        {
          id: "step_start",
          title: "시작",
          text: "새 이벤트 단계를 작성한다.",
          effects: [{ kind: "mark_done" }],
          branches: [],
          choices: [],
          nextStepId: "",
        },
      ],
    };
  }

  function activeEventStepDefinition(event) {
    const state = getState();
    const steps = Array.isArray(event?.steps) ? event.steps : [];
    if (!steps.length) return { step: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedEventStepIndex || 0)), steps.length - 1);
    return { step: steps[index], index };
  }

  function resolvePlacementEvent(placement) {
    const eventId = placement?.interaction?.eventId || placement?.refId || "";
    const baseEvent = eventDefinitions[eventId];
    if (!baseEvent) return null;
    const resolved = JSON.parse(JSON.stringify(baseEvent));
    const overrides = placement?.eventOverrides || {};
    if (overrides.usage) resolved.usage = { ...(resolved.usage || {}), ...overrides.usage };
    if (overrides.detection) resolved.detection = { ...(resolved.detection || {}), ...overrides.detection };
    if (overrides.disarm) resolved.disarm = { ...(resolved.disarm || {}), ...overrides.disarm };
    return resolved;
  }

  function eventPlacementsAtCursor() {
    const state = getState();
    return state.map.placements.filter((placement) => placement.position?.x === state.editorCursor.x
      && placement.position?.y === state.editorCursor.y
      && eventObjectPlacementKinds.has(placement.kind));
  }

  function activePlacementOverride() {
    const state = getState();
    const placements = eventPlacementsAtCursor();
    if (!placements.length) return null;
    if (state.selectedPlacementOverrideId) {
      const matched = placements.find((placement) => placement.id === state.selectedPlacementOverrideId);
      if (matched) return matched;
    }
    const preferredKind = eventEditorToPlacementKind[activeEventEditorTool()];
    return placements.find((placement) => placement.kind === preferredKind) || placements[0];
  }

  return {
    activeEventEditorTool,
    activeEventDefinitionId,
    activeEventDefinition,
    updateEventDefinition,
    activeNpcDefinitionId,
    activeNpcDefinition,
    updateNpcDefinition,
    activeNpcQuestSeed,
    ensureQuestSeedState,
    activateQuestSeed,
    grantQuestSeedRewards,
    setQuestSeedState,
    syncQuestSeedFailureStates,
    questSeedJson,
    questSeedRewardItemsText,
    questRewardFlagValueType,
    questRewardFlagValueText,
    uniqueNpcQuestSeedId,
    createQuestSeedTemplate,
    duplicateQuestSeedTemplate,
    activeNpcQuestSeedDefinition,
    createNpcServiceTemplate,
    createNpcServiceGroupTemplates,
    activeNpcServiceDefinition,
    createNpcDialogueStepTemplate,
    createNpcDialogueChoiceTemplate,
    activeNpcDialogueStepDefinition,
    npcPlacementsAtCursor,
    activeNpcPlacement,
    activeNpcPlacementDefinition,
    allowedInteractionsForPlacementKind,
    defaultInteractionTypeForPlacementKind,
    focusedNpcClassIndices,
    focusedNpcClassNames,
    activeNpcQuestHook,
    createNpcQuestHookTemplate,
    compatibleEventDefinitions,
    uniqueEventPresetId,
    updateEventReferences,
    renameEventPreset,
    createEventPresetFromDefinition,
    uniqueEventStepId,
    createEventStepTemplate,
    createEventChoiceTemplate,
    createEventGraphTemplate,
    activeEventStepDefinition,
    resolvePlacementEvent,
    eventPlacementsAtCursor,
    activePlacementOverride,
  };
}

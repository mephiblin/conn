function required(name) {
  throw new Error(`npcRuntime dependency missing: ${name}`);
}

export function createNpcRuntime(deps = {}) {
  const getState = deps.getState || (() => required("getState"));
  const addLog = deps.addLog || (() => required("addLog"));
  const render = deps.render || (() => required("render"));
  const closeInteraction = deps.closeInteraction || (() => required("closeInteraction"));
  const releasePointerLook = deps.releasePointerLook || (() => required("releasePointerLook"));
  const focusCameraOnPlacement = deps.focusCameraOnPlacement || (() => {});
  const ensureNpcRuntimeState = deps.ensureNpcRuntimeState || (() => required("ensureNpcRuntimeState"));
  const activeNpcQuestHook = deps.activeNpcQuestHook || (() => null);
  const activeNpcQuestSeed = deps.activeNpcQuestSeed || (() => null);
  const vendorOffer = deps.vendorOffer || (() => null);
  const nextClassMilestone = deps.nextClassMilestone || (() => null);
  const grantVendorInventoryEntry = deps.grantVendorInventoryEntry || (() => required("grantVendorInventoryEntry"));
  const vendorInventoryEntryLabel = deps.vendorInventoryEntryLabel || (() => required("vendorInventoryEntryLabel"));
  const createCompanionRecord = deps.createCompanionRecord || (() => required("createCompanionRecord"));
  const normalizePartyModel = deps.normalizePartyModel || (() => required("normalizePartyModel"));
  const buildCompanionHero = deps.buildCompanionHero || (() => required("buildCompanionHero"));
  const activateQuestSeed = deps.activateQuestSeed || (() => required("activateQuestSeed"));
  const allInventoryAndEquipmentEntries = deps.allInventoryAndEquipmentEntries || (() => []);
  const inventoryEntryItemId = deps.inventoryEntryItemId || (() => "");
  const inventoryEntryIsIdentified = deps.inventoryEntryIsIdentified || (() => false);
  const inventoryEntryIsCursed = deps.inventoryEntryIsCursed || (() => false);
  const identifyInventoryEntry = deps.identifyInventoryEntry || (() => false);
  const purifyInventoryEntry = deps.purifyInventoryEntry || (() => false);
  const startCombat = deps.startCombat || (() => required("startCombat"));
  const advanceWorldTurn = deps.advanceWorldTurn || (() => required("advanceWorldTurn"));
  const activateFloor = deps.activateFloor || (() => required("activateFloor"));
  const setMode = deps.setMode || (() => required("setMode"));
  const availableQuestDefinitions = deps.availableQuestDefinitions || (() => []);
  const activateBoardQuest = deps.activateBoardQuest || (() => ({ ok: false }));
  const boardQuestAllowsDungeonEntry = deps.boardQuestAllowsDungeonEntry || (() => false);
  const boardQuestEntryTarget = deps.boardQuestEntryTarget || (() => null);
  const skillCatalogSkillIds = deps.skillCatalogSkillIds || (() => []);
  const classes = deps.classes || [];
  const items = deps.items || {};
  const encounters = deps.encounters || {};
  const npcs = deps.npcs || {};
  const questDefinitions = deps.questDefinitions || {};

  function state() {
    return getState();
  }

  function npcTalkText(placement, npc, service = null) {
    const hook = activeNpcQuestHook(npc);
    const seed = activeNpcQuestSeed(npc);
    const runtimeSeed = seed?.id ? state().quest?.seeds?.[seed.id] || null : null;
    return [
      npc.log || `${npc.name}와 대화를 나눴다.`,
      placement.note,
      service?.note,
      hook?.note,
      seed?.title ? `${seed.title}: ${seed.note}` : "",
      runtimeSeed?.status === "active" ? `퀘스트 기록 갱신: ${runtimeSeed.title}` : "",
    ].filter(Boolean).join(" ");
  }

  function npcTalkService(npc) {
    return (npc?.services || []).find((service) => service?.type === "talk") || null;
  }

  function npcDialogueStepMap(service = {}) {
    return new Map((service?.dialogue?.steps || []).map((step) => [step.id, step]));
  }

  function hasNpcDialogueTree(service = {}) {
    return Array.isArray(service?.dialogue?.steps) && service.dialogue.steps.length > 0;
  }

  function openNpcDialogueInteraction(placement, npc, service, serviceIndex, startStepId = "") {
    if (!hasNpcDialogueTree(service)) return false;
    focusCameraOnPlacement(placement, -0.04);
    releasePointerLook();
    const stepMap = npcDialogueStepMap(service);
    const entryStepId = startStepId || service.dialogue?.entryStepId || service.dialogue?.steps?.[0]?.id || "";
    let stepId = entryStepId;
    let step = stepMap.get(stepId);
    let guard = 0;
    while (step && !(step.choices || []).length && step.nextStepId && guard < 32) {
      stepId = step.nextStepId;
      step = stepMap.get(stepId);
      guard += 1;
    }
    if (!step) return false;
    state().interaction = {
      type: "npc",
      placementId: placement.id,
      npcId: placement.npcId || placement.refId,
      title: step.title || service.label || npc.name,
      text: step.text || npcTalkText(placement, npc, service),
      details: [],
      dialogueServiceIndex: serviceIndex,
      dialogueStepId: step.id,
      options: (step.choices || []).length
        ? step.choices.map((choice, index) => ({
          id: `npc_dialogue_choice_${index}`,
          label: choice.label || `선택지 ${index + 1}`,
          serviceIndex,
          dialogueChoiceIndex: index,
        }))
        : [{ id: "npc_dialogue_close", label: "대화를 마친다", serviceIndex, dialogueClose: true }],
    };
    return true;
  }

  function hasCompanionFromNpc(npcId) {
    return Boolean(state().companion?.npcId === npcId && state().companion?.recruited);
  }

  function npcQuestServiceSnapshot(npc, placement) {
    const seed = activeNpcQuestSeed(npc) || (npc?.questSeeds || []).find((entry) => state().quest?.seeds?.[entry.id]);
    const runtime = seed?.id ? state().quest?.seeds?.[seed.id] || null : null;
    const status = runtime?.status || (seed ? "available" : "none");
    return { seed, runtime, status, npcId: placement.npcId || placement.refId };
  }

  function npcQuestServiceLabel(npc, placement, service = {}) {
    const snapshot = npcQuestServiceSnapshot(npc, placement);
    if (snapshot.status === "completed") return service.completedLabel || "의뢰 완료를 확인한다";
    if (snapshot.status === "failed") return service.failedLabel || "실패한 의뢰를 정리한다";
    if (snapshot.status === "active") return service.activeLabel || "의뢰 현황을 확인한다";
    if (snapshot.status === "available") return service.availableLabel || "의뢰를 받는다";
    return service.label || "의뢰를 확인한다";
  }

  function npcQuestServiceText(npc, placement) {
    const snapshot = npcQuestServiceSnapshot(npc, placement);
    if (!snapshot.seed) return "";
    const objectives = (snapshot.runtime?.objectives || snapshot.seed.objectives || []).join(" / ");
    const rewards = snapshot.runtime?.rewards || snapshot.seed.rewards || {};
    const rewardBits = [
      Number(rewards.gold || 0) > 0 ? `금화 ${rewards.gold}` : "",
      Number(rewards.xp || 0) > 0 ? `XP ${rewards.xp}` : "",
      Array.isArray(rewards.items) && rewards.items.length
        ? `아이템 ${rewards.items.map((entry) => items[entry.itemId]?.name || entry.itemId).join(", ")}`
        : "",
    ].filter(Boolean);
    const statusLabel = snapshot.status === "completed"
      ? "완료"
      : snapshot.status === "failed"
        ? "실패"
        : snapshot.status === "active"
          ? "진행 중"
          : "수락 가능";
    return [
      `의뢰 상태: ${statusLabel}`,
      snapshot.seed.title,
      snapshot.seed.note,
      objectives ? `목표: ${objectives}` : "",
      rewardBits.length ? `보상: ${rewardBits.join(" · ")}` : "",
    ].filter(Boolean).join(" · ");
  }

  function npcServicePreviewText(service = {}, npc = null, placement = null) {
    if (!service || typeof service !== "object") return "정의되지 않은 서비스";
    if (service.opensService?.kind === "skill_shop") {
      const parts = [service.opensService.title || service.label || "기술 상점"];
      if (service.opensService.note) parts.push(service.opensService.note);
      if (service.opensService.catalogId) parts.push(`catalog ${service.opensService.catalogId}`);
      return parts.join(" · ");
    }
    if (service.type === "quest") return placement && npc ? npcQuestServiceText(npc, placement) : (service.label || "의뢰 서비스");
    if (service.type === "quest_board") {
      const active = state().quest?.activeQuestId ? state().quest.seeds?.[state().quest.activeQuestId] : null;
      if (active?.status === "active") return `진행 중: ${active.title}`;
      return `수주 가능 의뢰 ${availableQuestDefinitions(questDefinitions, state().quest).length}개`;
    }
    if (service.type === "quest_gate") {
      const target = boardQuestEntryTarget(state().quest);
      return target ? `입장 가능 · ${target.mapKind} · floor ${target.floor}` : "활성 의뢰가 있어야 열린다.";
    }
    if (service.type === "heal") {
      const parts = [`회복 ${Math.max(0, Number(service.heal || 0))}`];
      if (service.cureStatus) parts.push(`상태 해제 ${service.cureStatus}`);
      if (service.cost?.gold) parts.push(`금화 ${service.cost.gold}`);
      return parts.join(" · ");
    }
    if (service.type === "identify") return `유물 감정${service.cost?.gold ? ` · 금화 ${service.cost.gold}` : ""}`;
    if (service.type === "trade") {
      const offer = vendorOffer(service.vendorId);
      return offer?.summary || `거래 · ${service.vendorId || "vendor 미지정"}`;
    }
    if (service.type === "skill_shop") {
      return [service.note || "스킬 카드를 사고판다.", service.catalogId ? `catalog ${service.catalogId}` : ""].filter(Boolean).join(" · ");
    }
    if (service.type === "recruit") return `동행 영입 · ${(service.companionProfile?.name || "동료")} · ${classes[service.companionProfile?.classIndex || 0]?.cls || "모험가"}`;
    if (service.type === "dismiss") return "현재 동행을 파티에서 해산";
    if (service.type === "fight") {
      const parts = [`적대 전환 · ${(encounters[service.encounterId]?.name || service.encounterId || "조우")}`];
      if (service.avoidCost?.gold) parts.push(`회피 금화 ${service.avoidCost.gold}`);
      if (service.avoidFlag) parts.push(`avoid flag ${service.avoidFlag}`);
      if (service.hostileFlag) parts.push(`flag ${service.hostileFlag}`);
      if (service.hostileLog) parts.push(service.hostileLog);
      return parts.join(" · ");
    }
    if (service.type === "talk") {
      const steps = service?.dialogue?.steps || [];
      const choiceCount = steps.reduce((sum, step) => sum + ((step?.choices || []).length), 0);
      const parts = [];
      if (steps.length) parts.push(`대화 ${steps.length} step · choice ${choiceCount}`);
      else parts.push("대화와 단서 제공");
      if (service.note) parts.push(service.note);
      return parts.join(" · ");
    }
    return service.label || service.type;
  }

  function npcServicePreviewList(npc, placement = null) {
    const services = Array.isArray(npc?.services) && npc.services.length
      ? npc.services
      : [{ type: "talk", label: "대화를 나눈다" }];
    return services.map((service) => ({
      label: service.type === "quest" && placement
        ? npcQuestServiceLabel(npc, placement, service)
        : (service.label || service.type),
      summary: npcServicePreviewText(service, npc, placement),
    }));
  }

  function buildNpcInteractionOptions(placement, npc) {
    const runtime = ensureNpcRuntimeState(placement.npcId || placement.refId);
    const baseServices = Array.isArray(npc.services) && npc.services.length
      ? npc.services
      : [{ type: "talk", label: "대화를 나눈다" }];
    const options = [];
    baseServices.forEach((service, index) => {
      if (service.type === "recruit" && state().companion?.recruited && !hasCompanionFromNpc(placement.npcId || placement.refId)) return;
      if (service.type === "dismiss" && !hasCompanionFromNpc(placement.npcId || placement.refId)) return;
      if (service.type === "recruit" && hasCompanionFromNpc(placement.npcId || placement.refId) && state().companion?.joinedParty) {
        options.push({ id: `npc_service_dismiss_${index}`, label: "동행을 해산한다", serviceIndex: index, forceType: "dismiss" });
        return;
      }
      const label = service.type === "quest"
        ? npcQuestServiceLabel(npc, placement, service)
        : (service.label || service.type);
      options.push({ id: `npc_service_${index}`, label, serviceIndex: index, forceType: null });
      if (service.type === "fight" && (Number(service.avoidCost?.gold || 0) > 0 || service.avoidFlag || service.avoidLabel)) {
        options.push({
          id: `npc_service_avoid_${index}`,
          label: service.avoidLabel || "길을 양보받는다",
          serviceIndex: index,
          forceType: "avoid",
        });
      }
    });
    if (!runtime.met) options.unshift({ id: "npc_service_talk_intro", label: "처음 말을 건다", serviceIndex: -1, forceType: "talk" });
    return options;
  }

  function openNpcInteraction(placement, npc) {
    focusCameraOnPlacement(placement, -0.04);
    releasePointerLook();
    const options = buildNpcInteractionOptions(placement, npc);
    const questText = npcQuestServiceText(npc, placement);
    state().interaction = {
      type: "npc",
      placementId: placement.id,
      npcId: placement.npcId || placement.refId,
      title: npc.name,
      text: [npc.description, placement.note, questText].filter(Boolean).join(" · "),
      details: npcServicePreviewList(npc, placement),
      options,
    };
  }

  function resolveNpcHandoffPlacement(effect = {}, fallbackPlacement = null) {
    const directPlacement = effect.npcPlacementId
      ? state().map.placements.find((entry) => entry.id === effect.npcPlacementId && entry.kind === "npc")
      : null;
    if (directPlacement) return directPlacement;
    if (fallbackPlacement?.kind === "npc") return fallbackPlacement;
    return null;
  }

  function queueNpcHandoff(effect = {}, fallbackPlacement = null) {
    const placement = resolveNpcHandoffPlacement(effect, fallbackPlacement);
    if (!placement) return false;
    const npc = npcs[placement.npcId || placement.refId];
    if (!npc) return false;
    state().pendingNpcHandoff = {
      placementId: placement.id,
      serviceIndex: Math.max(0, Number(effect.serviceIndex || 0)),
    };
    return true;
  }

  function flushPendingNpcHandoff() {
    if (!state().pendingNpcHandoff) return false;
    const pending = state().pendingNpcHandoff;
    state().pendingNpcHandoff = null;
    const placement = state().map.placements.find((entry) => entry.id === pending.placementId && entry.kind === "npc");
    const npc = placement ? npcs[placement.npcId || placement.refId] : null;
    if (!placement || !npc) return false;
    openNpcInteraction(placement, npc);
    if (state().interaction && Array.isArray(state().interaction.options) && state().interaction.options.length) {
      const matching = state().interaction.options.findIndex((option) => Number(option.serviceIndex) === Number(pending.serviceIndex));
      if (matching > 0) {
        const [picked] = state().interaction.options.splice(matching, 1);
        state().interaction.options.unshift(picked);
      }
    }
    return true;
  }

  function recruitNpcCompanion(placement, npc, service) {
    const npcId = placement.npcId || placement.refId;
    const runtime = ensureNpcRuntimeState(npcId);
    if (state().companion?.recruited && !hasCompanionFromNpc(npcId)) {
      addLog("이미 다른 동료가 있어 새 동행을 받을 수 없다.");
      return;
    }
    if (!state().companion?.recruited) {
      const hero = buildCompanionHero(service.companionProfile || {}, npc.name);
      state().companion = createCompanionRecord(hero, {
        npcId,
        joinedParty: true,
        placementStateKey: placement.stateKey || placement.id,
      });
      ({ party: state().party, companion: state().companion } = normalizePartyModel([...state().party, hero], state().companion));
      runtime.recruited = true;
      runtime.met = true;
      state().flags[`${npcId}_recruited`] = true;
      addLog(`${npc.name}이 파티에 합류했다. ${hero.note || "후열 지원과 탐지 보조를 제공한다."}`);
      return;
    }
    if (!state().companion.joinedParty) {
      state().companion.joinedParty = true;
      ({ party: state().party, companion: state().companion } = normalizePartyModel([...state().party, state().companion.hero], state().companion));
      addLog(`${npc.name}이 다시 파티에 합류했다.`);
    }
  }

  function dismissNpcCompanion(placement, npc) {
    const npcId = placement.npcId || placement.refId;
    if (!hasCompanionFromNpc(npcId) || !state().companion?.joinedParty) {
      addLog(`${npc.name}과 현재 동행 중이 아니다.`);
      return;
    }
    const heroId = state().companion.hero?.id;
    state().party = state().party.filter((hero) => hero.id !== heroId);
    state().companion.joinedParty = false;
    ({ party: state().party, companion: state().companion } = normalizePartyModel(state().party, state().companion));
    state().flags[`${npcId}_dismissed`] = true;
    addLog(`${npc.name}이 보급을 챙겨 잠시 파티를 떠났다.`);
  }

  function identifyWithNpc(npc, service = {}) {
    const cost = Number(service.cost?.gold || 0);
    if (state().resources.gold < cost) {
      addLog(`${npc.name}의 감정 비용이 부족하다.`);
      return;
    }
    const candidates = allInventoryAndEquipmentEntries().filter(({ entry }) => {
      const item = items[inventoryEntryItemId(entry)];
      return item && ["equipment", "artifact", "quest"].includes(item.kind);
    });
    const unidentified = candidates.find(({ entry }) => !inventoryEntryIsIdentified(entry));
    const cursed = candidates.find(({ entry }) => inventoryEntryIsIdentified(entry) && inventoryEntryIsCursed(entry));
    const target = unidentified || cursed;
    if (!target) {
      addLog(`${npc.name}은 감정하거나 정화할 물건이 없다고 말한다.`);
      return;
    }
    state().resources.gold -= cost;
    const targetId = inventoryEntryItemId(target.entry);
    const item = items[targetId];
    if (unidentified && identifyInventoryEntry(target.entry)) {
      state().flags[`identified_${targetId}`] = true;
      addLog(`${npc.name}이 ${item.name}의 정체를 밝혀냈다.${inventoryEntryIsCursed(target.entry) ? " 검은 기운이 저주를 머금고 있다." : ""}`);
      return;
    }
    if (purifyInventoryEntry(target.entry)) {
      addLog(`${npc.name}이 ${item.name}에 깃든 저주를 정화했다.${target.scope === "equipment" ? ` ${target.owner?.name}의 장비가 다시 풀렸다.` : ""}`);
      return;
    }
    addLog(`${npc.name}은 더 손댈 것이 없다고 말한다.`);
  }

  function tradeWithNpc(npc, service = {}) {
    const offer = vendorOffer(service.vendorId);
    if (!offer) {
      addLog(`${npc.name}의 거래 데이터를 찾지 못했다.`);
      return;
    }
    const cost = Number(offer.cost?.gold || 0);
    if (state().resources.gold < cost) {
      addLog(`${npc.name}과 거래할 금화가 부족하다.`);
      return;
    }
    state().resources.gold -= cost;
    if (offer.serviceType === "sell_bundle") {
      (offer.inventory || []).forEach((entry) => grantVendorInventoryEntry(entry));
      addLog(service.log || `${npc.name}: ${(offer.summary || npc.description || "물품을 건넸다.")} (${(offer.inventory || []).map((entry) => vendorInventoryEntryLabel(entry)).join(", ")})`);
      return;
    }
    if (offer.serviceType === "heal_party") {
      state().party.forEach((hero) => { hero.hp = hero.maxHp; });
      addLog(service.log || `${npc.name}: ${offer.summary || "파티를 회복시켰다."}`);
      return;
    }
    if (offer.serviceType === "buff_frontline") {
      state().party.slice(0, 2).forEach((hero) => { hero.atk += offer?.rewards?.frontlineAtkGain || 0; });
      addLog(service.log || `${npc.name}: ${offer.summary || "전열을 강화했다."}`);
      return;
    }
    if (offer.serviceType === "train_party") {
      const trained = [];
      state().party.forEach((hero) => {
        const milestone = nextClassMilestone(hero);
        if (!milestone || hero.xp < (milestone.xpCost || 0)) return;
        hero.xp -= milestone.xpCost || 0;
        hero.maxHp += milestone.hpGain || 0;
        hero.hp = Math.min(hero.maxHp, hero.hp + (milestone.hpGain || 0));
        hero.atk += milestone.atkGain || 0;
        hero.def += milestone.defGain || 0;
        hero.prof[hero.category] = (hero.prof[hero.category] || 0) + (milestone.profGain || 0);
        if (milestone.passiveUnlock) hero.passive = true;
        hero.trainingLevel = (hero.trainingLevel || 0) + 1;
        trained.push(`${hero.name}:${milestone.label}`);
      });
      addLog(trained.length ? (service.log ? `${service.log} (${trained.join(", ")})` : `${npc.name}: ${trained.join(", ")}`) : `${npc.name}: 훈련할 준비가 된 인원이 없다.`);
      return;
    }
    addLog(service.log || `${npc.name}: ${offer.summary || "거래가 완료됐다."}`);
  }

  function healWithNpc(npc, service = {}) {
    const cost = Number(service.cost?.gold || 0);
    if (state().resources.gold < cost) {
      addLog(`${npc.name}의 치료 비용이 부족하다.`);
      return;
    }
    state().resources.gold -= cost;
    const amount = Math.max(0, Number(service.heal || 6));
    const status = service.cureStatus;
    state().party.forEach((hero) => {
      hero.hp = Math.min(hero.maxHp, hero.hp + amount);
      if (status) hero.status = hero.status.filter((entry) => entry !== status);
    });
    addLog(`${npc.name}이 상처를 돌봤다.${status ? ` ${status} 상태도 추슬렀다.` : ""}`);
  }

  function fightNpc(placement, npc, service = {}) {
    const encounterId = service.encounterId;
    if (!encounterId || !encounters[encounterId]) {
      addLog(`${npc.name}과의 충돌에 사용할 조우 데이터를 찾지 못했다.`);
      return;
    }
    placement.done = true;
    state().flags[service.hostileFlag || `${placement.id}_hostile`] = true;
    if (service.hostileLog) addLog(service.hostileLog);
    closeInteraction();
    startCombat({ id: `${placement.id}_fight`, refId: encounterId });
  }

  function avoidNpcFight(placement, npc, service = {}) {
    const cost = Math.max(0, Number(service.avoidCost?.gold || 0));
    if (state().resources.gold < cost) {
      addLog(`${npc.name}을 지나칠 금화가 부족하다.`);
      return false;
    }
    if (cost > 0) state().resources.gold -= cost;
    placement.done = true;
    if (service.avoidFlag) state().flags[service.avoidFlag] = true;
    addLog(service.avoidLog || `${npc.name}이 길을 비켜 주었다.`);
    return true;
  }

  function resolveNpcQuestService(placement, npc, service = {}) {
    const snapshot = npcQuestServiceSnapshot(npc, placement);
    if (!snapshot.seed) {
      addLog(`${npc.name}에게 지금 받을 의뢰가 없다.`);
      return;
    }
    if (snapshot.status === "available") {
      const runtime = activateQuestSeed(snapshot.seed, placement.npcId || placement.refId);
      addLog(`${npc.name}: ${runtime.title} 의뢰를 넘겨주었다.`);
      return;
    }
    if (snapshot.status === "active") {
      addLog(`${npc.name}: ${npcQuestServiceText(npc, placement)}`);
      return;
    }
    if (snapshot.status === "completed") {
      addLog(`${npc.name}: ${snapshot.seed.title} 의뢰는 이미 끝났다. ${snapshot.runtime?.rewardsGranted ? "보상도 정산됐다." : "정산 확인이 필요하다."}`);
      return;
    }
    if (snapshot.status === "failed") {
      addLog(`${npc.name}: ${snapshot.seed.title} 의뢰는 더 이어갈 수 없다.`);
    }
  }

  function openQuestBoardInteraction(placement, npc) {
    const active = state().quest?.activeQuestId ? state().quest.seeds?.[state().quest.activeQuestId] : null;
    const available = availableQuestDefinitions(questDefinitions, state().quest);
    if (active?.status === "active") {
      addLog(`이미 진행 중인 의뢰가 있다: ${active.title}`);
      return false;
    }
    if (!available.length) {
      addLog(`${npc.name}: 지금 받을 수 있는 의뢰가 없다.`);
      return false;
    }
    state().interaction = {
      type: "npc",
      placementId: placement.id,
      npcId: placement.npcId || placement.refId,
      title: npc.name,
      text: "촌장이 남긴 의뢰 목록이다. 한 번에 하나만 수주할 수 있다.",
      details: available.map(({ id, definition }) => ({
        label: definition.name || id,
        summary: [
          definition.description || "",
          definition.mapKind ? `map ${definition.mapKind}` : "",
          definition.conditions?.summary || "",
          definition.rewards?.gold ? `금화 ${definition.rewards.gold}` : "",
        ].filter(Boolean).join(" · "),
      })),
      options: available.map(({ id, definition }) => ({
        id: `quest_board_accept_${id}`,
        label: `${definition.name || id} 수주`,
        serviceIndex: -1,
        forceType: "accept_quest_definition",
        questId: id,
      })),
    };
    return true;
  }

  function acceptQuestDefinition(placement, npc, questId = "") {
    const definition = questDefinitions[questId];
    if (!definition) {
      addLog(`${npc.name}: 의뢰 데이터를 찾지 못했다.`);
      return false;
    }
    const result = activateBoardQuest(state().quest, questId, definition, placement.npcId || placement.refId);
    if (!result.ok) {
      addLog(`이미 진행 중인 의뢰가 있다: ${result.active?.title || state().quest.activeQuestId}`);
      return false;
    }
    addLog(`${npc.name}: ${result.runtime.title} 의뢰를 수주했다. 마을의 문으로 가면 던전에 입장할 수 있다.`);
    return true;
  }

  function travelThroughQuestGate(npc, service = {}) {
    if (!boardQuestAllowsDungeonEntry(state().quest)) {
      addLog(`${npc.name}: 게시판에서 의뢰를 먼저 수주해야 문이 열린다.`);
      return false;
    }
    const target = boardQuestEntryTarget(state().quest) || {};
    activateFloor(Number(service.targetFloor || target.floor || 1), service.target);
    if (service.note) addLog(service.note);
    setMode("dungeon");
    addLog(`${npc.name}: 수주한 의뢰 전표가 빛나며 던전 입구가 열린다.`);
    return true;
  }

  function travelWithNpc(npc, service = {}) {
    if (Number.isFinite(service.targetFloor)) {
      activateFloor(service.targetFloor, service.target);
    }
    if (service.note) addLog(service.note);
    if (service.log) addLog(service.log);
    setMode(service.targetMode || "dungeon");
    return true;
  }

  function openSkillShopWithNpc(placement, npc, service = {}) {
    const explicitSkillIds = Array.isArray(service.skillIds) && service.skillIds.length ? [...service.skillIds] : [];
    const catalogSkillIds = explicitSkillIds.length ? explicitSkillIds : skillCatalogSkillIds(service.catalogId || "");
    const stockSize = Math.max(1, Math.min(catalogSkillIds.length || 1, Number(service.stockSize || 6)));
    const salt = [
      placement.npcId || placement.refId || "skill_shop",
      service.catalogId || "all",
      state().combat?.round || 0,
      state().player?.floor || 1,
      state().quest?.activeBoardQuestId || "",
      Math.floor(Date.now() / Math.max(1, Number(service.refreshSeconds || 45)) / 1000),
    ].join("|");
    const score = (skillId) => {
      let hash = 2166136261;
      const text = `${salt}|${skillId}`;
      for (let index = 0; index < text.length; index += 1) {
        hash ^= text.charCodeAt(index);
        hash = Math.imul(hash, 16777619);
      }
      return hash >>> 0;
    };
    const stock = [...catalogSkillIds]
      .sort((left, right) => score(left) - score(right))
      .slice(0, stockSize);
    state().skillDeckOpen = false;
    state().skillShopOpen = true;
    state().skillShopNpcId = placement.npcId || placement.refId;
    state().skillShopTitle = service.label || `${npc.name}의 스킬 상점`;
    state().skillShopNote = service.note || npc.description || "스킬 카드를 거래할 수 있다.";
    state().skillShopHeroId = state().skillShopHeroId || state().party?.[0]?.id || "";
    state().skillShopCatalogId = service.catalogId || "";
    state().skillShopSkillIds = stock;
    closeInteraction();
    render();
    return true;
  }

  function openNpcLinkedService(placement, npc, service = {}) {
    const contract = service.opensService;
    if (!contract || typeof contract !== "object") return false;
    const npcId = placement.npcId || placement.refId;
    state().activeTownService = {
      source: "npc",
      serviceKind: contract.kind || "service",
      serviceId: contract.serviceId || `${npcId}_${contract.kind || "service"}`,
      placementId: placement.id,
      npcId,
      npcName: npc.name,
      title: contract.title || service.label || npc.name,
      note: contract.note || service.note || npc.description || "",
      vendorId: contract.vendorId || service.vendorId || "",
      catalogId: contract.catalogId || "",
      currency: contract.currency || "gold",
      sourceServiceType: service.type || "talk",
    };
    if (service.log) addLog(service.log);
    closeInteraction();
    render();
    return true;
  }

  function resolveNpcService(choiceIndex) {
    const interaction = state().interaction;
    if (!interaction || interaction.type !== "npc") return false;
    const placement = state().map.placements.find((entry) => entry.id === interaction.placementId);
    const npc = placement ? npcs[interaction.npcId] : null;
    if (!placement || !npc) {
      closeInteraction();
      return false;
    }
    const option = interaction.options[choiceIndex];
    if (!option) return false;
    const runtime = ensureNpcRuntimeState(interaction.npcId);
    runtime.met = true;
    const fallbackTalkService = npcTalkService(npc);
    const service = option.serviceIndex >= 0
      ? (npc.services || [])[option.serviceIndex]
      : (option.forceType === "talk" ? fallbackTalkService : null);
    if (interaction.dialogueServiceIndex != null && service?.type === "talk" && hasNpcDialogueTree(service)) {
      const step = npcDialogueStepMap(service).get(interaction.dialogueStepId);
      const choice = step?.choices?.[option.dialogueChoiceIndex];
      if (option.dialogueClose || !step) {
        closeInteraction();
        advanceWorldTurn();
        render();
        return true;
      }
      if (!choice) return false;
      if (choice.note) addLog(choice.note);
      if (choice.nextStepId && openNpcDialogueInteraction(placement, npc, service, interaction.dialogueServiceIndex, choice.nextStepId)) {
        render();
        return true;
      }
      closeInteraction();
      advanceWorldTurn();
      render();
      return true;
    }
    const serviceType = option.forceType || service?.type || "talk";
    if (serviceType === "accept_quest_definition") {
      acceptQuestDefinition(placement, npc, option.questId || "");
      closeInteraction();
      advanceWorldTurn();
      render();
      return true;
    }
    if (service?.opensService && openNpcLinkedService(placement, npc, service)) return true;
    if (serviceType === "talk") {
      if (service && hasNpcDialogueTree(service)) {
        openNpcDialogueInteraction(placement, npc, service, option.serviceIndex >= 0 ? option.serviceIndex : (npc.services || []).indexOf(service), "");
        render();
        return true;
      }
      addLog(npcTalkText(placement, npc, service || {}));
    } else if (serviceType === "quest") resolveNpcQuestService(placement, npc, service || {});
    else if (serviceType === "quest_board") {
      if (openQuestBoardInteraction(placement, npc)) return render(), true;
    }
    else if (serviceType === "quest_gate") return closeInteraction(), travelThroughQuestGate(npc, service || {}), render(), true;
    else if (serviceType === "trade") tradeWithNpc(npc, service || {});
    else if (serviceType === "skill_shop") return openSkillShopWithNpc(placement, npc, service || {});
    else if (serviceType === "recruit") recruitNpcCompanion(placement, npc, service || {});
    else if (serviceType === "dismiss") dismissNpcCompanion(placement, npc);
    else if (serviceType === "heal") healWithNpc(npc, service || {});
    else if (serviceType === "identify") identifyWithNpc(npc, service || {});
    else if (serviceType === "avoid") avoidNpcFight(placement, npc, service || {});
    else if (serviceType === "travel") return closeInteraction(), travelWithNpc(npc, service || {});
    else if (serviceType === "fight") return fightNpc(placement, npc, service || {}), true;
    closeInteraction();
    advanceWorldTurn();
    render();
    return true;
  }

  function runNpcPlacement(placement) {
    const npc = npcs[placement.npcId || placement.refId];
    if (!npc) {
      addLog(`${placement.id} NPC 데이터를 찾지 못했다.`);
      return false;
    }
    openNpcInteraction(placement, npc);
    return true;
  }

  return {
    npcTalkText,
    npcTalkService,
    npcDialogueStepMap,
    hasNpcDialogueTree,
    openNpcDialogueInteraction,
    hasCompanionFromNpc,
    npcQuestServiceSnapshot,
    npcQuestServiceLabel,
    npcQuestServiceText,
    npcServicePreviewText,
    npcServicePreviewList,
    buildNpcInteractionOptions,
    openNpcInteraction,
    resolveNpcHandoffPlacement,
    queueNpcHandoff,
    flushPendingNpcHandoff,
    recruitNpcCompanion,
    dismissNpcCompanion,
    identifyWithNpc,
    tradeWithNpc,
    healWithNpc,
    openSkillShopWithNpc,
    fightNpc,
    avoidNpcFight,
    resolveNpcQuestService,
    openQuestBoardInteraction,
    acceptQuestDefinition,
    travelThroughQuestGate,
    openNpcLinkedService,
    resolveNpcService,
    runNpcPlacement,
  };
}

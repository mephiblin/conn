import {
  normalizeCompanionDiceProfile,
  normalizeHeroDiceProfile,
} from "./diceSkillLoadout.js";
import { TOWN_MAP_ID } from "./townRuntime.js";

export function saveSlotStorageKey(slotId, prefix = "connan_save_slot_") {
  return `${prefix}${slotId}`;
}

export function saveSlotLabel(slotId) {
  return `슬롯 ${String(slotId).split("_")[1]}`;
}

export function saveUsesEmbeddedContentDefinitions(data) {
  return Boolean(data?.contentDefinitions && typeof data.contentDefinitions === "object" && Object.keys(data.contentDefinitions).length);
}

export function saveContentVersionMatchesCurrent(data, currentContentVersion) {
  return String(data?.contentVersion || "") === String(currentContentVersion || "");
}

export function formatPlaytimeLabel(playtimeMs = 0) {
  const totalMinutes = Math.max(0, Math.floor(Number(playtimeMs || 0) / 60000));
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  if (hours > 0) return `${hours}시간 ${minutes}분`;
  return `${minutes}분`;
}

export function buildRecentStatusLabel(data = {}, deps = {}) {
  const questEndingComplete = deps.questEndingComplete || (() => false);
  if (questEndingComplete(data.quest)) return data.quest?.ending?.title || "최종 계단 확보";
  if (data.mode === "combat") return `전투 중 · F${data.player?.floor ?? "?"}`;
  if (data.mode === "town") return "도시에서 원정 준비";
  const floor = data.player?.floor ?? "?";
  const x = data.player?.x ?? "?";
  const y = data.player?.y ?? "?";
  return `던전 탐사 중 · F${floor} ${x},${y}`;
}

export function summarizeSaveData(slotId, data, deps = {}) {
  const classes = deps.classes || [];
  const questEndingComplete = deps.questEndingComplete || (() => false);
  if (!data) return { id: slotId, label: saveSlotLabel(slotId), hasSave: false, summary: "비어 있음", meta: null };
  const protagonist = data.party?.[0];
  const companionName = data.companion?.hero?.name || data.party?.[1]?.name || "";
  const bossesDefeated = Object.keys(data.quest?.bossesDefeated || {}).length;
  const slotAlias = (data.slotName || "").trim();
  const playtimeLabel = formatPlaytimeLabel(data.playtimeMs || 0);
  const recentStatusLabel = data.recentStatus || buildRecentStatusLabel(data, { questEndingComplete });
  return {
    id: slotId,
    label: slotAlias || saveSlotLabel(slotId),
    hasSave: true,
    summary: `${protagonist?.name || "주인공"}${companionName ? ` + ${companionName}` : ""} · ${recentStatusLabel}`,
    meta: {
      slotAlias: slotAlias || "",
      protagonistName: protagonist?.name || "주인공",
      protagonistClass: classes[protagonist?.classIndex || 0]?.cls || "모험가",
      backgroundLabel: data.flags?.protagonistBackgroundLabel || null,
      companionName: companionName || null,
      floor: data.player?.floor ?? "-",
      seed: data.runtimeMaps?.[data.player?.floor]?.generation?.seed ?? "-",
      partyLabel: `${classes[protagonist?.classIndex || 0]?.cls || "모험가"}${companionName ? ` · 동료 ${companionName}` : " · 단독 원정"}`,
      resourceLabel: `금화 ${data.resources?.gold ?? 0} · 식량 ${data.resources?.food ?? 0} · 물 ${data.resources?.water ?? 0}`,
      progressLabel: `보스 ${bossesDefeated}/2 · ${questEndingComplete(data.quest) ? "최종 계단 도달" : "탐사 중"}`,
      recentStatusLabel,
      playtimeLabel,
      savedAtLabel: data.savedAt ? new Date(data.savedAt).toLocaleString("ko-KR") : "저장 시각 없음",
    },
  };
}

export function readSaveSlotData(slotId, deps = {}) {
  const storage = deps.storage || localStorage;
  const storagePrefix = deps.storagePrefix || "connan_save_slot_";
  const raw = storage.getItem(saveSlotStorageKey(slotId, storagePrefix));
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function readSaveSlots(slotIds = [], deps = {}) {
  const storage = deps.storage || localStorage;
  const storagePrefix = deps.storagePrefix || "connan_save_slot_";
  return slotIds.map((slotId) => {
    const data = readSaveSlotData(slotId, { storage, storagePrefix });
    if (!data) {
      const raw = storage.getItem(saveSlotStorageKey(slotId, storagePrefix));
      if (raw) return { id: slotId, label: saveSlotLabel(slotId), hasSave: false, summary: "손상된 저장", meta: null };
      return summarizeSaveData(slotId, null, deps);
    }
    return summarizeSaveData(slotId, data, deps);
  });
}

export function parseSaveSlotPayload(raw, slotId) {
  let data;
  try {
    data = JSON.parse(raw);
  } catch {
    throw new Error(`${saveSlotLabel(slotId)} 저장 JSON을 해석할 수 없다.`);
  }
  if (!data || typeof data !== "object" || Array.isArray(data)) {
    throw new Error(`${saveSlotLabel(slotId)} 저장 형식이 올바르지 않다.`);
  }
  if (data.kind !== "saveSlot") {
    throw new Error(`${saveSlotLabel(slotId)}은 현재 saveSlot contract와 맞지 않는 데이터다.`);
  }
  if (!data.player || typeof data.player !== "object") {
    throw new Error(`${saveSlotLabel(slotId)}에는 player 데이터가 없다.`);
  }
  if (!data.runtimeMaps && !data.authoredMaps) {
    throw new Error(`${saveSlotLabel(slotId)}에는 복원 가능한 runtime/authored map 데이터가 없다.`);
  }
  return data;
}

function normalizeSavedPartySnapshot(party = [], companion = null) {
  const normalizedParty = Array.isArray(party)
    ? party.map((hero, slot) => normalizeHeroDiceProfile(JSON.parse(JSON.stringify(hero || {})), {
      heroId: hero?.id || `hero_${slot}`,
    }))
    : [];
  return {
    party: normalizedParty,
    companion: normalizeCompanionDiceProfile(companion),
  };
}

export function createSaveSlotManager(deps = {}) {
  const getState = deps.getState;
  const setState = deps.setState;
  const addLog = deps.addLog;
  const render = deps.render;
  const storage = deps.storage || localStorage;
  const storagePrefix = deps.storagePrefix || "connan_save_slot_";
  const saveSlotIds = deps.saveSlotIds || [];
  const currentSaveSlotId = deps.currentSaveSlotId || (() => saveSlotIds[0]);
  const currentContentVersion = deps.currentContentVersion || "";
  const saveSlotSchemaVersion = deps.saveSlotSchemaVersion || 1;
  const windowObject = deps.windowObject || window;
  const buildRecentStatus = deps.buildRecentStatusLabel || ((data) => buildRecentStatusLabel(data, deps));
  const validateEventDefinitionsTable = deps.validateEventDefinitionsTable || (() => {});
  const validateClassDefinitionsTable = deps.validateClassDefinitionsTable || (() => {});
  const validateNpcDefinitionsTable = deps.validateNpcDefinitionsTable || (() => {});
  const normalizeQuestState = deps.normalizeQuestState || ((value) => value);
  const normalizeFieldMonsterStateTable = deps.normalizeFieldMonsterStateTable || ((value) => value || {});
  const cloneEditorMap = deps.cloneEditorMap || ((value) => JSON.parse(JSON.stringify(value)));
  const ensureBoundaryClean = deps.ensureBoundaryClean || (() => {});
  const initialState = deps.initialState;
  const normalizeMapMetadata = deps.normalizeMapMetadata || (() => {});
  const computeWalls = deps.computeWalls || (() => {});
  const normalizePartyModel = deps.normalizePartyModel;
  const normalizeInventoryList = deps.normalizeInventoryList || ((value) => value || []);
  const endTestPlaySession = deps.endTestPlaySession || (() => {});
  const ensureTownFloorMaps = deps.ensureTownFloorMaps || ((value) => value);
  const activateTownState = deps.activateTownState || ((value) => value);

  function state() {
    return getState();
  }

  function writeState(nextState) {
    if (!setState) throw new Error("saveSlots dependency missing: setState");
    setState(nextState);
  }

  function renameSaveSlot(slotId = currentSaveSlotId(), nextName = "") {
    const existingSave = readSaveSlotData(slotId, { storage, storagePrefix });
    if (!existingSave) {
      addLog(`${saveSlotLabel(slotId)}은 아직 이름을 붙일 저장이 없다.`);
      return render();
    }
    existingSave.slotName = (nextName || "").trim().slice(0, 24);
    storage.setItem(saveSlotStorageKey(slotId, storagePrefix), JSON.stringify(existingSave));
    addLog(existingSave.slotName ? `${saveSlotLabel(slotId)} 이름을 ${existingSave.slotName}(으)로 바꿨다.` : `${saveSlotLabel(slotId)} 이름을 기본값으로 되돌렸다.`);
    render();
  }

  function saveGame(slotId = currentSaveSlotId(), options = {}) {
    const currentState = state();
    if (currentState.runtimeSession?.kind === "test_play") {
      addLog("테스트 플레이 임시 세션에서는 실제 저장 슬롯을 사용할 수 없다.");
      return render();
    }
    if (currentState.mode === "editor" || currentState.mode === "title") {
      addLog("실제 저장 슬롯은 게임 런타임에서만 저장한다. editor는 프로젝트 저장을 사용하고, title에서는 새 게임이나 이어하기로 진입한다.");
      return render();
    }
    const existingSave = readSaveSlotData(slotId, { storage, storagePrefix });
    if (existingSave && options.confirmOverwrite !== false) {
      const accepted = windowObject.confirm(`${saveSlotLabel(slotId)}에는 ${existingSave.party?.[0]?.name || "이전 원정"} 진행이 있다. 현재 상태로 덮어쓸까?`);
      if (!accepted) {
        addLog(`${saveSlotLabel(slotId)} 덮어쓰기를 취소했다.`);
        return render();
      }
    }
    validateEventDefinitionsTable(deps.eventDefinitions || {});
    validateClassDefinitionsTable(deps.classes || []);
    validateNpcDefinitionsTable(deps.npcs || {});
    currentState.visitedByFloor[currentState.player.floor] = currentState.visited;
    const normalizedPartySnapshot = normalizeSavedPartySnapshot(currentState.party, currentState.companion);
    const savedAt = new Date().toISOString();
    const accumulatedPlaytimeMs = Number(currentState.runtimeSession?.accumulatedPlaytimeMs || 0);
    const sessionPlaytimeMs = Math.max(0, Date.now() - Date.parse(currentState.runtimeSession?.startedAt || savedAt));
    const playtimeMs = accumulatedPlaytimeMs + sessionPlaytimeMs;
    const data = {
      kind: "saveSlot",
      slotId,
      slotLabel: saveSlotLabel(slotId),
      slotName: (existingSave?.slotName || "").trim(),
      saveVersion: saveSlotSchemaVersion,
      contentVersion: currentContentVersion,
      savedAt,
      playtimeMs,
      recentStatus: buildRecentStatus(currentState),
      mode: currentState.mode,
      player: currentState.player,
      visitedByFloor: Object.fromEntries(Object.entries(currentState.visitedByFloor).map(([floor, seen]) => [floor, [...seen]])),
      party: normalizedPartySnapshot.party,
      companion: normalizedPartySnapshot.companion,
      npcState: currentState.npcState,
      resources: currentState.resources,
      inventory: normalizeInventoryList(currentState.inventory),
      flags: currentState.flags,
      quest: normalizeQuestState(currentState.quest),
      fieldMonsters: normalizeFieldMonsterStateTable(currentState.fieldMonsters),
      runtimeMaps: Object.fromEntries(Object.entries(currentState.floorMaps).map(([floor, map]) => [floor, cloneEditorMap(map)])),
      floorState: Object.fromEntries(Object.entries(currentState.floorMaps).map(([floor, map]) => [floor, {
        doors: map.doors,
        placementsDone: map.placements.filter((entry) => entry.done).map((entry) => entry.stateKey || entry.id),
        placementRuntime: Object.fromEntries(map.placements
          .filter((entry) => entry.eventRuntime && Object.keys(entry.eventRuntime).length)
          .map((entry) => [entry.stateKey || entry.id, JSON.parse(JSON.stringify(entry.eventRuntime))])),
      }])),
    };
    ensureBoundaryClean(data, "saveSlot");
    storage.setItem(saveSlotStorageKey(slotId, storagePrefix), JSON.stringify(data));
    currentState.runtimeSession.accumulatedPlaytimeMs = playtimeMs;
    currentState.runtimeSession.startedAt = savedAt;
    addLog(`${saveSlotLabel(slotId)}에 진행 상태를 저장했다.`);
    render();
  }

  function deleteSaveSlot(slotId = currentSaveSlotId()) {
    const existingSave = readSaveSlotData(slotId, { storage, storagePrefix });
    if (!existingSave) {
      addLog(`${saveSlotLabel(slotId)}은 이미 비어 있다.`);
      return render();
    }
    const accepted = windowObject.confirm(`${saveSlotLabel(slotId)}의 저장을 삭제할까? 이 작업은 되돌릴 수 없다.`);
    if (!accepted) {
      addLog(`${saveSlotLabel(slotId)} 삭제를 취소했다.`);
      return render();
    }
    storage.removeItem(saveSlotStorageKey(slotId, storagePrefix));
    addLog(`${saveSlotLabel(slotId)} 저장을 삭제했다.`);
    render();
  }

  function loadGame(slotId = currentSaveSlotId()) {
    const currentState = state();
    if (currentState.runtimeSession?.kind === "test_play") {
      addLog("테스트 플레이 임시 세션에서는 실제 저장 슬롯을 불러올 수 없다. editor로 복귀해 종료한다.");
      endTestPlaySession("editor");
      return render();
    }
    if (currentState.mode === "editor") {
      addLog("editor workspace에서는 실제 저장 슬롯을 바로 불러오지 않는다. title의 Continue 흐름으로 돌아가거나 프로젝트 불러오기를 사용한다.");
      return render();
    }
    const raw = storage.getItem(saveSlotStorageKey(slotId, storagePrefix));
    if (!raw) return addLog(`${saveSlotLabel(slotId)}에 저장된 진행이 없다.`), render();
    let data;
    try {
      data = parseSaveSlotPayload(raw, slotId);
    } catch (error) {
      addLog(`저장 불러오기 실패: ${error.message}`);
      return render();
    }
    const previousLog = currentState.log.slice(-20);
    const nextState = initialState();
    nextState.log = previousLog;
    nextState.shell.selectedSaveSlotId = slotId;
    nextState.runtimeSession.accumulatedPlaytimeMs = Number(data.playtimeMs || 0);
    nextState.runtimeSession.startedAt = new Date().toISOString();
    if (data.runtimeMaps && typeof data.runtimeMaps === "object") {
      const restoredFloorMaps = {};
      for (const [floor, map] of Object.entries(data.runtimeMaps)) {
        normalizeMapMetadata(map);
        computeWalls(map);
        restoredFloorMaps[floor] = map;
      }
      if (Object.keys(restoredFloorMaps).length) nextState.floorMaps = restoredFloorMaps;
    } else if (data.authoredMaps && typeof data.authoredMaps === "object") {
      const restoredFloorMaps = {};
      for (const [floor, map] of Object.entries(data.authoredMaps)) {
        normalizeMapMetadata(map);
        computeWalls(map);
        restoredFloorMaps[floor] = map;
      }
      if (Object.keys(restoredFloorMaps).length) nextState.floorMaps = restoredFloorMaps;
    }
    nextState.floorMaps = ensureTownFloorMaps(nextState.floorMaps);
    nextState.player = data.player;
    nextState.visitedByFloor = Object.fromEntries(Object.entries(data.visitedByFloor || { 1: data.visited || [] }).map(([floor, seen]) => [floor, new Set(seen)]));
    nextState.visited = nextState.visitedByFloor[nextState.player.floor] || new Set();
    const normalizedModel = normalizePartyModel(data.party, data.companion);
    nextState.party = normalizedModel.party;
    nextState.companion = normalizedModel.companion;
    nextState.npcState = JSON.parse(JSON.stringify(data.npcState || {}));
    nextState.resources = data.resources;
    nextState.inventory = normalizeInventoryList(data.inventory);
    nextState.flags = data.flags;
    nextState.quest = normalizeQuestState(data.quest);
    nextState.fieldMonsters = normalizeFieldMonsterStateTable(data.fieldMonsters);
    for (const [floor, saved] of Object.entries(data.floorState || { 1: { doors: data.doors, placementsDone: data.placementsDone || [], placementRuntime: {} } })) {
      const map = nextState.floorMaps[floor];
      if (!map) continue;
      map.doors = saved.doors || map.doors;
      for (const entry of map.placements) {
        const key = entry.stateKey || entry.id;
        if ((saved.placementsDone || []).includes(key) || (saved.placementsDone || []).includes(entry.id)) entry.done = true;
      }
      for (const entry of map.placements) {
        const key = entry.stateKey || entry.id;
        const runtime = saved.placementRuntime?.[key] || saved.placementRuntime?.[entry.id];
        if (runtime) entry.eventRuntime = JSON.parse(JSON.stringify(runtime));
      }
      computeWalls(map);
    }
    nextState.map = nextState.floorMaps[nextState.player.floor] || nextState.floorMaps[1];
    nextState.interaction = null;
    nextState.inventoryPanelOpen = false;
    nextState.inventoryPanelDragIndex = -1;
    nextState.mode = data.mode === "dungeon" ? "dungeon" : "town";
    if (nextState.mode === "town" && !nextState.floorMaps[nextState.player.floor]) {
      activateTownState(nextState);
    }
    if (nextState.mode === "town" && nextState.map?.id !== TOWN_MAP_ID) {
      activateTownState(nextState);
    }
    writeState(nextState);
    if (normalizedModel.trimmedCount > 0) addLog(`기존 저장의 추가 파티원 ${normalizedModel.trimmedCount}명은 현재 2인 파티 모델에 맞게 정리했다.`);
    if (Number(data.saveVersion || 0) !== saveSlotSchemaVersion) {
      addLog(`주의: ${saveSlotLabel(slotId)} saveVersion ${data.saveVersion || 0}을(를) 현재 schema ${saveSlotSchemaVersion} 기준으로 읽었다.`);
    }
    if (!saveContentVersionMatchesCurrent(data, currentContentVersion)) {
      addLog(`주의: ${saveSlotLabel(slotId)}은 contentVersion ${data.contentVersion || "(empty)"} 기준으로 저장됐다. 현재 authored content ${currentContentVersion}로 계속 진행한다.`);
    }
    if (saveUsesEmbeddedContentDefinitions(data)) {
      addLog(`주의: ${saveSlotLabel(slotId)}에는 legacy embedded contentDefinitions가 있지만, 현재 saveSlot contract에 따라 authored content는 현재 프로젝트 기준을 사용한다.`);
    }
    addLog(`${saveSlotLabel(slotId)}에서 저장된 진행을 불러왔다.`);
    render();
  }

  return {
    saveSlotStorageKey: (slotId) => saveSlotStorageKey(slotId, storagePrefix),
    saveSlotLabel,
    formatPlaytimeLabel,
    buildRecentStatusLabel: buildRecentStatus,
    readSaveSlotData: (slotId) => readSaveSlotData(slotId, { storage, storagePrefix }),
    readSaveSlots: () => readSaveSlots(saveSlotIds, { ...deps, storage, storagePrefix }),
    renameSaveSlot,
    saveGame,
    deleteSaveSlot,
    parseSaveSlotPayload,
    loadGame,
  };
}

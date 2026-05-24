export const ENDING_STATE_SCHEMA_VERSION = 1;
export const FINAL_ENDING_ID = "serpent_heart_seal_confirmed";
export const PARTY_DEFEAT_ENDING_ID = "party_defeated_in_labyrinth";

export function createInitialQuestState() {
  return {
    main: "세 층을 돌파하고 검은 물의 파충수와 눈먼 사제를 쓰러뜨려 뱀의 심장 봉인을 확인하라.",
    bossesDefeated: {},
    complete: false,
    ending: null,
    activeQuestId: "",
    returnRewardPending: false,
    returnRewardGranted: false,
    seeds: {},
  };
}

export function normalizeQuestState(quest = {}) {
  const next = quest && typeof quest === "object" && !Array.isArray(quest)
    ? quest
    : createInitialQuestState();
  if (typeof next.main !== "string") next.main = createInitialQuestState().main;
  if (!next.bossesDefeated || typeof next.bossesDefeated !== "object" || Array.isArray(next.bossesDefeated)) next.bossesDefeated = {};
  if (!next.seeds || typeof next.seeds !== "object" || Array.isArray(next.seeds)) next.seeds = {};
  if (typeof next.activeQuestId !== "string") next.activeQuestId = "";
  next.returnRewardPending = Boolean(next.returnRewardPending);
  next.returnRewardGranted = Boolean(next.returnRewardGranted);
  if (next.ending && typeof next.ending === "object" && !Array.isArray(next.ending)) {
    next.ending = {
      schemaVersion: Number(next.ending.schemaVersion || ENDING_STATE_SCHEMA_VERSION),
      endingId: String(next.ending.endingId || FINAL_ENDING_ID),
      status: next.ending.status === "complete" ? "complete" : "active",
      completedAt: next.ending.completedAt || null,
      sourcePlacementId: next.ending.sourcePlacementId || null,
      floor: Number(next.ending.floor || 0) || null,
      title: String(next.ending.title || "뱀의 심장 봉인"),
      summary: String(next.ending.summary || "최종 계단 아래에서 뱀의 심장 봉인이 드러났다."),
      continueBehavior: ["resume_after_ending", "death_return_title"].includes(next.ending.continueBehavior) ? next.ending.continueBehavior : "resume_at_final_floor",
      bossesDefeated: JSON.parse(JSON.stringify(next.ending.bossesDefeated || next.bossesDefeated || {})),
      flags: JSON.parse(JSON.stringify(next.ending.flags || {})),
    };
    next.complete = next.ending.status === "complete";
  } else if (next.complete) {
    next.ending = {
      schemaVersion: ENDING_STATE_SCHEMA_VERSION,
      endingId: "legacy_final_victory",
      status: "complete",
      completedAt: null,
      sourcePlacementId: null,
      floor: null,
      title: "최종 계단 확보",
      summary: "legacy save에서 quest.complete만 발견되어 ending state로 승격했다.",
      continueBehavior: "resume_at_final_floor",
      bossesDefeated: JSON.parse(JSON.stringify(next.bossesDefeated || {})),
      flags: {},
    };
  } else {
    next.complete = false;
    next.ending = null;
  }
  return next;
}

export function activeBoardQuest(quest = {}) {
  const normalizedQuest = normalizeQuestState(quest);
  const questId = normalizedQuest.activeQuestId || "";
  return questId ? normalizedQuest.seeds?.[questId] || null : null;
}

export function availableQuestDefinitions(questDefinitions = {}, quest = {}) {
  const active = activeBoardQuest(quest);
  if (active && !active.rewardsGranted) return [];
  return Object.entries(questDefinitions || {})
    .filter(([, definition]) => definition && typeof definition === "object")
    .map(([id, definition]) => ({ id, definition }));
}

export function createRuntimeQuestFromDefinition(questId = "", definition = {}, sourceNpcId = "") {
  return {
    id: questId,
    source: "quest_definition",
    sourceNpcId,
    title: definition.name || questId,
    note: definition.description || "",
    status: "active",
    mapKind: definition.mapKind || "twisted_temple",
    startFloor: Math.max(1, Number(definition.startFloor || 1)),
    conditions: JSON.parse(JSON.stringify(definition.conditions || { kind: "bosses_defeated", bossesDefeatedAtLeast: 1 })),
    objectives: [
      definition.conditions?.summary || "던전 조건을 완료한다.",
      `${definition.return?.label || "귀환"}으로 마을에 돌아와 보상을 정산한다.`,
    ],
    rewards: JSON.parse(JSON.stringify(definition.rewards || {})),
    return: JSON.parse(JSON.stringify(definition.return || { label: "귀환", rewardOnReturn: true })),
    rewardsGranted: false,
    completedAt: null,
  };
}

export function activateBoardQuest(quest = {}, questId = "", definition = {}, sourceNpcId = "") {
  const next = normalizeQuestState(quest);
  const active = activeBoardQuest(next);
  if (active && !active.rewardsGranted && active.id !== questId) {
    return { ok: false, reason: "active_quest_exists", active };
  }
  const runtime = createRuntimeQuestFromDefinition(questId, definition, sourceNpcId);
  next.seeds[questId] = runtime;
  next.activeQuestId = questId;
  next.returnRewardPending = false;
  next.returnRewardGranted = false;
  return { ok: true, runtime };
}

export function boardQuestAllowsDungeonEntry(quest = {}) {
  const active = activeBoardQuest(quest);
  return Boolean(active && active.status === "active");
}

export function boardQuestEntryTarget(quest = {}) {
  const active = activeBoardQuest(quest);
  if (!active || active.status !== "active") return null;
  return {
    floor: Math.max(1, Number(active.startFloor || 1)),
    mapKind: active.mapKind || "twisted_temple",
  };
}

export function boardQuestConditionComplete(runtime = null, quest = {}) {
  if (!runtime || runtime.status !== "active") return false;
  const conditions = runtime.conditions || {};
  if ((conditions.kind || "bosses_defeated") === "bosses_defeated") {
    const required = Math.max(1, Number(conditions.bossesDefeatedAtLeast || 1));
    return Object.keys(quest?.bossesDefeated || {}).length >= required;
  }
  if (conditions.kind === "specific_monsters_defeated") {
    const defeated = quest?.bossesDefeated || {};
    const targetMonsterIds = Array.isArray(conditions.targetMonsterIds)
      ? conditions.targetMonsterIds.map((entry) => String(entry || "").trim()).filter(Boolean)
      : [];
    if (!targetMonsterIds.length) return false;
    const required = Math.max(1, Number(conditions.requiredCount || 1));
    const completed = targetMonsterIds.filter((monsterId) => defeated[monsterId]).length;
    return completed >= Math.min(required, targetMonsterIds.length);
  }
  if (conditions.kind === "flag") return Boolean(quest?.flags?.[conditions.flag]);
  return false;
}

export function updateBoardQuestCompletion(quest = {}) {
  const next = normalizeQuestState(quest);
  const active = activeBoardQuest(next);
  if (!active || active.status !== "active") return { completed: false, runtime: active };
  if (!boardQuestConditionComplete(active, next)) return { completed: false, runtime: active };
  active.status = "completed";
  active.completedAt = new Date().toISOString();
  next.returnRewardPending = Boolean(active.return?.rewardOnReturn !== false);
  return { completed: true, runtime: active };
}

export function boardQuestCanReturn(quest = {}) {
  const active = activeBoardQuest(quest);
  return Boolean(active && active.status === "completed" && quest.returnRewardPending);
}

export function grantBoardQuestReturnRewards(quest = {}, grant = {}) {
  const active = activeBoardQuest(quest);
  if (!active || active.status !== "completed" || active.rewardsGranted) return { granted: false, runtime: active };
  const rewards = active.rewards || {};
  if (Number(rewards.gold || 0) > 0 && grant.addGold) grant.addGold(Number(rewards.gold || 0));
  if (Number(rewards.xp || 0) > 0 && grant.addXp) grant.addXp(Number(rewards.xp || 0));
  if (Array.isArray(rewards.items) && grant.addItem) {
    rewards.items.forEach((entry) => {
      const quantity = Math.max(1, Number(entry.quantity || 1));
      for (let index = 0; index < quantity; index += 1) grant.addItem(entry.itemId);
    });
  }
  if (rewards.flag && grant.setFlag) grant.setFlag(rewards.flag, rewards.value ?? true);
  active.rewardsGranted = true;
  quest.returnRewardPending = false;
  quest.returnRewardGranted = true;
  quest.activeQuestId = "";
  return { granted: true, runtime: active, rewards };
}

export function questEndingComplete(quest = {}) {
  return Boolean(quest?.ending?.status === "complete" || quest?.complete);
}

export function buildFinalEndingState({
  quest = {},
  placement = null,
  player = null,
  flags = {},
  completedAt = new Date().toISOString(),
} = {}) {
  const normalizedQuest = normalizeQuestState(quest);
  return {
    schemaVersion: ENDING_STATE_SCHEMA_VERSION,
    endingId: FINAL_ENDING_ID,
    status: "complete",
    completedAt,
    sourcePlacementId: placement?.id || "final_stairs_03",
    floor: player?.floor || placement?.targetFloor || null,
    title: "뱀의 심장 봉인",
    summary: "세 번째 계단 아래에서 뱀의 심장 봉인이 드러났다. MVP 승리 조건을 달성했다.",
    continueBehavior: "resume_at_final_floor",
    bossesDefeated: JSON.parse(JSON.stringify(normalizedQuest.bossesDefeated || {})),
    flags: JSON.parse(JSON.stringify(flags || {})),
  };
}

export const buildCompletedFinalEnding = buildFinalEndingState;

export function buildPartyDefeatEndingState({
  quest = {},
  player = null,
  combat = null,
  flags = {},
  completedAt = new Date().toISOString(),
} = {}) {
  const normalizedQuest = normalizeQuestState(quest);
  return {
    schemaVersion: ENDING_STATE_SCHEMA_VERSION,
    endingId: PARTY_DEFEAT_ENDING_ID,
    status: "complete",
    completedAt,
    sourcePlacementId: combat?.placementId || null,
    floor: player?.floor || combat?.floor || null,
    title: "미궁에서 쓰러진 원정",
    summary: "파티가 전멸했다. 이 run은 사망 엔딩으로 종료됐다.",
    continueBehavior: "death_return_title",
    bossesDefeated: JSON.parse(JSON.stringify(normalizedQuest.bossesDefeated || {})),
    flags: JSON.parse(JSON.stringify(flags || {})),
  };
}

export function applyFinalEndingState(quest = {}, ending = buildFinalEndingState({ quest })) {
  const next = normalizeQuestState(quest);
  next.complete = true;
  next.ending = ending;
  return next;
}

export function applyPartyDefeatEndingState(quest = {}, ending = buildPartyDefeatEndingState({ quest })) {
  const next = normalizeQuestState(quest);
  next.complete = true;
  next.ending = ending;
  return next;
}

export function completeFinalEnding(inputs = {}) {
  const ending = buildFinalEndingState(inputs);
  return applyFinalEndingState(inputs.quest, ending);
}

export function completePartyDefeatEnding(inputs = {}) {
  const ending = buildPartyDefeatEndingState(inputs);
  return applyPartyDefeatEndingState(inputs.quest, ending);
}

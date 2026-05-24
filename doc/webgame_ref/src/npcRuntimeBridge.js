export function createNpcRuntimeBridge(deps = {}) {
  const {
    getState = () => ({}),
    ensureNpcRuntime = () => {
      throw new Error("npcRuntimeBridge dependency missing: ensureNpcRuntime");
    },
  } = deps;

  function ensureNpcRuntimeState(npcId) {
    if (!npcId) return {};
    const state = getState();
    if (!state.npcState[npcId]) state.npcState[npcId] = {};
    return state.npcState[npcId];
  }

  function npcTalkText(...args) {
    return ensureNpcRuntime().npcTalkText(...args);
  }

  function npcTalkService(...args) {
    return ensureNpcRuntime().npcTalkService(...args);
  }

  function npcDialogueStepMap(...args) {
    return ensureNpcRuntime().npcDialogueStepMap(...args);
  }

  function hasNpcDialogueTree(...args) {
    return ensureNpcRuntime().hasNpcDialogueTree(...args);
  }

  function openNpcDialogueInteraction(...args) {
    return ensureNpcRuntime().openNpcDialogueInteraction(...args);
  }

  function hasCompanionFromNpc(...args) {
    return ensureNpcRuntime().hasCompanionFromNpc(...args);
  }

  function npcQuestServiceSnapshot(...args) {
    return ensureNpcRuntime().npcQuestServiceSnapshot(...args);
  }

  function npcQuestServiceLabel(...args) {
    return ensureNpcRuntime().npcQuestServiceLabel(...args);
  }

  function npcQuestServiceText(...args) {
    return ensureNpcRuntime().npcQuestServiceText(...args);
  }

  function npcServicePreviewText(...args) {
    return ensureNpcRuntime().npcServicePreviewText(...args);
  }

  function npcServicePreviewList(...args) {
    return ensureNpcRuntime().npcServicePreviewList(...args);
  }

  function buildNpcInteractionOptions(...args) {
    return ensureNpcRuntime().buildNpcInteractionOptions(...args);
  }

  function openNpcInteraction(...args) {
    return ensureNpcRuntime().openNpcInteraction(...args);
  }

  function resolveNpcHandoffPlacement(...args) {
    return ensureNpcRuntime().resolveNpcHandoffPlacement(...args);
  }

  function queueNpcHandoff(...args) {
    return ensureNpcRuntime().queueNpcHandoff(...args);
  }

  function flushPendingNpcHandoff(...args) {
    return ensureNpcRuntime().flushPendingNpcHandoff(...args);
  }

  function recruitNpcCompanion(...args) {
    return ensureNpcRuntime().recruitNpcCompanion(...args);
  }

  function dismissNpcCompanion(...args) {
    return ensureNpcRuntime().dismissNpcCompanion(...args);
  }

  function identifyWithNpc(...args) {
    return ensureNpcRuntime().identifyWithNpc(...args);
  }

  function tradeWithNpc(...args) {
    return ensureNpcRuntime().tradeWithNpc(...args);
  }

  function healWithNpc(...args) {
    return ensureNpcRuntime().healWithNpc(...args);
  }

  function fightNpc(...args) {
    return ensureNpcRuntime().fightNpc(...args);
  }

  function avoidNpcFight(...args) {
    return ensureNpcRuntime().avoidNpcFight(...args);
  }

  function resolveNpcQuestService(...args) {
    return ensureNpcRuntime().resolveNpcQuestService(...args);
  }

  function openQuestBoardInteraction(...args) {
    return ensureNpcRuntime().openQuestBoardInteraction(...args);
  }

  function acceptQuestDefinition(...args) {
    return ensureNpcRuntime().acceptQuestDefinition(...args);
  }

  function travelThroughQuestGate(...args) {
    return ensureNpcRuntime().travelThroughQuestGate(...args);
  }

  function resolveNpcService(...args) {
    return ensureNpcRuntime().resolveNpcService(...args);
  }

  function runNpcPlacement(...args) {
    return ensureNpcRuntime().runNpcPlacement(...args);
  }

  return {
    ensureNpcRuntimeState,
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
    fightNpc,
    avoidNpcFight,
    resolveNpcQuestService,
    openQuestBoardInteraction,
    acceptQuestDefinition,
    travelThroughQuestGate,
    resolveNpcService,
    runNpcPlacement,
  };
}

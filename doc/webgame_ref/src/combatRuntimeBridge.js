export function createCombatRuntimeBridge(deps = {}) {
  const {
    ensureCombatRuntime = () => {
      throw new Error("combatRuntimeBridge dependency missing: ensureCombatRuntime");
    },
  } = deps;

  function placementEncounterId(...args) {
    return ensureCombatRuntime().placementEncounterId(...args);
  }

  function startCombat(...args) {
    return ensureCombatRuntime().startCombat(...args);
  }

  function livingParty(...args) {
    return ensureCombatRuntime().livingParty(...args);
  }

  function livingCombatEnemies(...args) {
    return ensureCombatRuntime().livingCombatEnemies(...args);
  }

  function currentCombatHero(...args) {
    return ensureCombatRuntime().currentCombatHero(...args);
  }

  function combatConsumableEntries(...args) {
    return ensureCombatRuntime().combatConsumableEntries(...args);
  }

  function syncPartyRows(...args) {
    return ensureCombatRuntime().syncPartyRows(...args);
  }

  function handlePartyDefeatInCombat(...args) {
    return ensureCombatRuntime().handlePartyDefeatInCombat(...args);
  }

  function endCombatRound(...args) {
    return ensureCombatRuntime().endCombatRound(...args);
  }

  function restorePreEncounterPosition(...args) {
    return ensureCombatRuntime().restorePreEncounterPosition(...args);
  }

  function finishHeroAction(...args) {
    return ensureCombatRuntime().finishHeroAction(...args);
  }

  function swapHeroForCombat(...args) {
    return ensureCombatRuntime().swapHeroForCombat(...args);
  }

  function resolveHeroAction(...args) {
    return ensureCombatRuntime().resolveHeroAction(...args);
  }

  function queueCombatAction(...args) {
    return ensureCombatRuntime().queueCombatAction(...args);
  }

  function clearCombatDiceSelection(...args) {
    return ensureCombatRuntime().clearCombatDiceSelection(...args);
  }

  function selectCombatDie(...args) {
    return ensureCombatRuntime().selectCombatDie(...args);
  }

  function assignHeroSkillToDieFace(...args) {
    return ensureCombatRuntime().assignHeroSkillToDieFace(...args);
  }

  function useCombatConsumable(...args) {
    return ensureCombatRuntime().useCombatConsumable(...args);
  }

  function useCombatThrowItem(...args) {
    return ensureCombatRuntime().useCombatThrowItem(...args);
  }

  function enemyTurn(...args) {
    return ensureCombatRuntime().enemyTurn(...args);
  }

  function winCombat(...args) {
    return ensureCombatRuntime().winCombat(...args);
  }

  return {
    placementEncounterId,
    startCombat,
    livingParty,
    livingCombatEnemies,
    currentCombatHero,
    combatConsumableEntries,
    syncPartyRows,
    handlePartyDefeatInCombat,
    endCombatRound,
    restorePreEncounterPosition,
    finishHeroAction,
    swapHeroForCombat,
    resolveHeroAction,
    queueCombatAction,
    clearCombatDiceSelection,
    selectCombatDie,
    assignHeroSkillToDieFace,
    useCombatConsumable,
    useCombatThrowItem,
    enemyTurn,
    winCombat,
  };
}

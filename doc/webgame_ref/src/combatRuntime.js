import { createDiceCombatRuntime } from "./diceCombatRuntime.js";
import { assignSkillToDieFace } from "./diceSkillLoadout.js";

const EMPTY_FACE_FALLBACK_SKILL_ID = "fallback_basic_attack";

function required(name) {
  throw new Error(`combatRuntime dependency missing: ${name}`);
}

export function createCombatRuntime(deps = {}) {
  const getState = deps.getState || (() => required("getState"));
  const addLog = deps.addLog || (() => required("addLog"));
  const render = deps.render || (() => required("render"));
  const closeInteraction = deps.closeInteraction || (() => required("closeInteraction"));
  const closeInventoryOverlay = deps.closeInventoryOverlay || (() => {});
  const focusCameraOnPlacement = deps.focusCameraOnPlacement || (() => {});
  const releasePointerLook = deps.releasePointerLook || (() => {});
  const capturePreEncounterSnapshot = deps.capturePreEncounterSnapshot || (() => required("capturePreEncounterSnapshot"));
  const normalizePartyModel = deps.normalizePartyModel || (() => required("normalizePartyModel"));
  const normalizeInventoryList = deps.normalizeInventoryList || ((value) => value || []);
  const inventoryEntryItemId = deps.inventoryEntryItemId || (() => "");
  const useInventoryEntryOnHero = deps.useInventoryEntryOnHero || (() => false);
  const removeInventoryEntryAt = deps.removeInventoryEntryAt || (() => false);
  const pushInventoryItemId = deps.pushInventoryItemId || (() => required("pushInventoryItemId"));
  const pushInventoryEntry = deps.pushInventoryEntry || (() => required("pushInventoryEntry"));
  const lootItems = deps.lootItems || (() => []);
  const combatLootTable = deps.combatLootTable || (() => "");
  const inventoryEntryLabel = deps.inventoryEntryLabel || ((entry) => String(entry));
  const skillName = deps.skillName || ((skillId) => skillId || "기술");
  const skills = deps.skills || {};
  const monsters = deps.monsters || {};
  const encounters = deps.encounters || {};
  const items = deps.items || {};
  const legacyMonsterToEncounter = deps.legacyMonsterToEncounter || {};
  const completePartyDefeatEnding = deps.completePartyDefeatEnding || (() => null);
  const updateBoardQuestCompletion = deps.updateBoardQuestCompletion || (() => ({ completed: false }));
  const logicalCellKey = deps.logicalCellKey || ((player) => `${Math.floor(Number(player?.x) + 0.5)},${Math.floor(Number(player?.y) + 0.5)}`);
  const diceCombat = createDiceCombatRuntime({
    rand: (min, max) => rand(min, max),
    skillName,
    skills,
  });

  function state() {
    return getState();
  }

  function placementEncounterId(placement) {
    return placement.refId || legacyMonsterToEncounter[placement.monsterId] || "";
  }

  function livingParty() {
    return state().party.filter((member) => member.hp > 0);
  }

  function livingCombatEnemies() {
    return state().combat?.enemies?.filter((enemy) => enemy.hp > 0) || [];
  }

  function currentCombatHero() {
    if (!state().combat) return null;
    return state().party.find((hero) => hero.hp > 0 && !state().combat.actedHeroIds.includes(hero.id)) || null;
  }

  function currentDiceState() {
    return state().combat?.diceState || null;
  }

  function combatRollCooldownRemaining(hero = {}, roll = {}) {
    return Math.max(0, Number(hero.skillCooldowns?.[roll.cooldownKey] || 0));
  }

  function hasUsableCombatRoll(hero = currentCombatHero()) {
    const diceState = currentDiceState();
    if (!hero || !diceState?.rolls?.length || diceState.phase !== "select") return false;
    return diceState.rolls.some((roll) => combatRollCooldownRemaining(hero, roll) <= 0);
  }

  function ensureHeroDiceTurn(hero = currentCombatHero(), options = {}) {
    if (!state().combat || !hero) return null;
    return diceCombat.beginHeroTurn(state().combat, hero, options);
  }

  function describeCurrentRolls(hero = currentCombatHero()) {
    const diceState = ensureHeroDiceTurn(hero);
    if (!hero || !diceState?.rolls?.length) return;
    state().combat.log.push(`${hero.name}의 주사위: ${diceState.rolls.map((roll) => diceCombat.describeRoll(hero, roll)).join(" / ")}`);
  }

  function prepareHeroTurn(hero = currentCombatHero()) {
    if (!state().combat || !hero) return null;
    const diceState = ensureHeroDiceTurn(hero, { force: true, tickCooldowns: true });
    if (!diceState?.rolls?.length) return diceState;
    const cooldownSummary = Object.entries(hero.skillCooldowns || {})
      .filter(([, turns]) => Number(turns) > 0)
      .map(([cooldownKey, turns]) => {
        const [dieId = "", faceIndexText = ""] = String(cooldownKey).split("::");
        const faceIndex = Math.max(0, Number(faceIndexText || 0));
        const die = hero?.diceLoadout?.dice?.find((entry) => entry.id === dieId) || null;
        const face = die?.faces?.[faceIndex] || null;
        const skillId = typeof face?.skillId === "string" ? face.skillId.trim() : "";
        const label = !skillId
          ? "기본공격"
          : diceCombat.skillDefinition(hero, skillId === EMPTY_FACE_FALLBACK_SKILL_ID ? "" : skillId).name;
        return `${label} [${dieId || "die"}:${faceIndex + 1}] ${turns}`;
      });
    state().combat.log.push(`${hero.name}의 슬롯 릴이 돌아가기 시작했다.`);
    if (cooldownSummary.length) state().combat.log.push(`${hero.name}의 쿨타임: ${cooldownSummary.join(" · ")}`);
    return diceState;
  }

  function clearCombatDiceSelection() {
    if (!state().combat?.diceState) return false;
    diceCombat.clearSelection(state().combat);
    render();
    return true;
  }

  function selectCombatDie(rollId = "") {
    if (!state().combat || !currentCombatHero()) return false;
    const hero = currentCombatHero();
    const diceState = ensureHeroDiceTurn(hero);
    if (!diceState || diceState.phase !== "select") return false;
    const roll = diceState.rolls.find((entry) => entry.id === rollId);
    if (!roll) return false;
    if (combatRollCooldownRemaining(hero, roll) > 0) return false;
    const changed = diceCombat.toggleRollSelection(state().combat, rollId);
    if (changed) render();
    return changed;
  }

  function stopCombatDice() {
    if (!state().combat) return false;
    const hero = currentCombatHero();
    if (!hero) return false;
    const diceState = ensureHeroDiceTurn(hero);
    if (!diceState || diceState.phase !== "spinning") return false;
    const result = diceCombat.stopAllRolls(state().combat);
    if (!result) return false;
    if (result.allStopped) {
      state().combat.log.push(`${hero.name}의 슬롯 릴이 순서대로 멈췄다.`);
      state().combat.log.push(`${hero.name}의 주사위: ${diceState.rolls.map((roll) => diceCombat.describeRoll(hero, roll)).join(" / ")}`);
      state().combat.log.push(`${hero.name}의 모든 주사위가 멈췄다. ${Number(diceState.selectLimit || 0)}개를 선택한다.`);
    }
    render();
    return true;
  }

  function combatConsumableEntries() {
    return normalizeInventoryList(state().inventory)
      .map((entry, index) => ({ entry, index, item: items[inventoryEntryItemId(entry)] || null }))
      .filter(({ item }) => item?.kind === "consumable");
  }

  function syncPartyRows() {
    state().party.forEach((hero, index) => {
      hero.row = index === 0 ? "전열" : "후열";
      hero.isCompanion = index > 0;
    });
  }

  function startCombat(placement) {
    closeInteraction();
    focusCameraOnPlacement(placement, 0.02);
    releasePointerLook();
    const encounterId = placementEncounterId(placement);
    const encounter = encounters[encounterId];
    const encounterEnemySpecs = Array.isArray(placement.generatedEnemies) && placement.generatedEnemies.length
      ? placement.generatedEnemies
      : encounter?.enemies;
    const enemyList = encounterEnemySpecs
      ?.map((enemySpec, index) => {
        const base = monsters[enemySpec.monsterId];
        if (!base) return null;
        return {
          ...base,
          id: enemySpec.id || `${enemySpec.monsterId}_${index + 1}`,
          monsterId: enemySpec.monsterId,
          maxHp: typeof enemySpec.hp === "number" ? enemySpec.hp : base.hp,
          hp: typeof enemySpec.hp === "number" ? enemySpec.hp : base.hp,
          atk: typeof enemySpec.atk === "number" ? enemySpec.atk : base.atk,
          def: typeof enemySpec.def === "number" ? enemySpec.def : base.def,
          xp: typeof enemySpec.xp === "number" ? enemySpec.xp : base.xp,
          row: enemySpec.row || (index < 2 ? "전열" : "후열"),
          boss: enemySpec.boss == null ? Boolean(base.boss) : Boolean(enemySpec.boss),
          defend: false,
          exposed: 0,
        };
      })
      .filter(Boolean) || [];
    if (!encounter || !enemyList.length) {
      addLog(`${placement.id} 조우 데이터를 찾지 못해 전투를 시작할 수 없다.`);
      return render();
    }
    capturePreEncounterSnapshot();
    state().combat = {
      placementId: placement.id,
      encounterId,
      encounterName: encounter.name,
      floor: state().player.floor,
      enemies: enemyList,
      round: 1,
      actedHeroIds: [],
      pendingItemIndex: -1,
      pendingItemPreviewIndex: -1,
      itemMode: false,
      diceState: null,
      log: [`${encounter.name}: ${enemyList.map((enemy) => enemy.name).join(", ")}이 어둠 속에서 모습을 드러냈다.`],
    };
    state().mode = "combat";
    const hero = currentCombatHero();
    if (hero) prepareHeroTurn(hero);
  }

  function handlePartyDefeatInCombat() {
    if (!state().combat) return false;
    closeInventoryOverlay();
    completePartyDefeatEnding(state().combat);
    state().combat = null;
    state().preEncounterSnapshot = null;
    state().mode = "title";
    state().shell.titlePanel = "ending";
    addLog("파티가 전멸했다. 이 원정은 사망 엔딩으로 종료됐다.");
    render();
    return true;
  }

  function enemyTurn(enemy) {
    const party = livingParty();
    if (!party.length) return false;
    if (Number(enemy.stunnedTurns || 0) > 0) {
      enemy.stunnedTurns = Math.max(0, Number(enemy.stunnedTurns || 0) - 1);
      state().combat.log.push(`${enemy.name}은 기절해 행동하지 못했다.`);
      return false;
    }
    const target = party[rand(0, party.length - 1)];
    if (!target) return false;
    const weakened = Math.max(0, Number(enemy.timedEffects?.weakened?.amount || 0));
    let dmg = Math.max(1, Number(enemy.atk || 0) - weakened - Math.max(0, Number(target.def || 0) - Number(enemy.exposed || 0)) + rand(0, 3));
    if (target.defend) dmg = Math.ceil(dmg / 2);
    if (target.guard) {
      const blocked = Math.min(dmg, Math.max(0, Number(target.guard || 0)));
      dmg -= blocked;
      target.guard = Math.max(0, Number(target.guard || 0) - blocked);
      if (blocked > 0) state().combat.log.push(`${target.name}이 방어 준비로 ${blocked} 피해를 흡수했다.`);
    }
    target.hp = Math.max(0, target.hp - Math.max(1, dmg));
    state().combat.log.push(`${enemy.name}이 ${target.name}에게 ${Math.max(1, dmg)} 피해를 입혔다.`);
    if (target.hp <= 0) state().combat.log.push(`${target.name}이 쓰러졌다. 도시에서 시체 회수와 부활이 필요하다.`);
    if (livingParty().length === 0) return handlePartyDefeatInCombat();
    return false;
  }

  function endCombatRound() {
    if (!state().combat) return false;
    for (const enemy of livingCombatEnemies()) {
      const defeated = enemyTurn(enemy);
      if (defeated || !state().combat) return Boolean(defeated);
    }
    if (!state().combat) return true;
    if (livingParty().length === 0) return handlePartyDefeatInCombat();
    state().combat.round += 1;
    state().combat.actedHeroIds = [];
    state().combat.pendingItemIndex = -1;
    state().combat.pendingItemPreviewIndex = -1;
    state().combat.itemMode = false;
    state().combat.diceState = null;
    state().party.forEach((hero) => {
      hero.defend = false;
      hero.guard = 0;
    });
    state().combat.enemies.forEach((enemy) => {
      enemy.exposed = 0;
      const effects = enemy.timedEffects && typeof enemy.timedEffects === "object" ? enemy.timedEffects : {};
      Object.keys(effects).forEach((key) => {
        const nextTurns = Math.max(0, Number(effects[key]?.turns || 0) - 1);
        if (nextTurns > 0) effects[key].turns = nextTurns;
        else delete effects[key];
      });
    });
    const hero = currentCombatHero();
    if (hero) prepareHeroTurn(hero);
    return false;
  }

  function restorePreEncounterPosition() {
    const snapshot = state().preEncounterSnapshot;
    if (!snapshot) return false;
    state().player = JSON.parse(JSON.stringify(snapshot.player));
    ({ party: state().party, companion: state().companion } = normalizePartyModel(snapshot.party, snapshot.companion));
    state().npcState = JSON.parse(JSON.stringify(snapshot.npcState || {}));
    state().resources = JSON.parse(JSON.stringify(snapshot.resources));
    state().inventory = JSON.parse(JSON.stringify(snapshot.inventory));
    state().flags = JSON.parse(JSON.stringify(snapshot.flags));
    state().quest = JSON.parse(JSON.stringify(snapshot.quest));
    if ("fieldMonsters" in snapshot || "fieldMonsters" in state()) {
      state().fieldMonsters = JSON.parse(JSON.stringify(snapshot.fieldMonsters || state().fieldMonsters || {}));
    }
    state().map = state().floorMaps[state().player.floor] || state().map;
    state().visited = state().visitedByFloor[state().player.floor] || new Set();
    state().visited.add(logicalCellKey(state().player));
    state().visitedByFloor[state().player.floor] = state().visited;
    return true;
  }

  function finishHeroAction(hero) {
    if (!state().combat) return;
    if (!state().combat.actedHeroIds.includes(hero.id)) state().combat.actedHeroIds.push(hero.id);
    state().combat.pendingItemIndex = -1;
    state().combat.pendingItemPreviewIndex = -1;
    state().combat.itemMode = false;
    state().combat.diceState = null;
    if (livingCombatEnemies().length === 0) return winCombat();
    const nextHero = currentCombatHero();
    if (nextHero) {
      prepareHeroTurn(nextHero);
      return render();
    }
    if (endCombatRound()) return render();
    render();
  }

  function swapHeroForCombat(hero) {
    const heroIndex = state().party.findIndex((member) => member.id === hero.id);
    const swapIndex = state().party.findIndex((member, index) => index !== heroIndex && member.hp > 0 && member.row !== hero.row);
    if (heroIndex < 0 || swapIndex < 0) {
      state().combat.log.push(`${hero.name}이 교대할 자리를 찾지 못했다.`);
      return finishHeroAction(hero);
    }
    [state().party[heroIndex], state().party[swapIndex]] = [state().party[swapIndex], state().party[heroIndex]];
    syncPartyRows();
    state().combat.log.push(`${hero.name}이 ${state().party[heroIndex].name}과 자리를 교대했다.`);
    finishHeroAction(hero);
  }

  function applyHeroSkillProgress(hero, result) {
    if (!hero || !result?.usedSignature) return;
    hero.prof = hero.prof || {};
    hero.prof[hero.category] = (hero.prof[hero.category] || 0) + 8;
    state().combat.log.push(`${hero.name}의 ${skillName(hero.skillId)}: 숙련도 +8`);
    if (!hero.passive && hero.prof[hero.category] >= 16) {
      hero.passive = true;
      hero.atk += 1;
      hero.def += 1;
      state().combat.log.push(`${hero.name}이 패시브 노드를 해금했다.`);
    }
  }

  function resolveSelectedDice(targetId = "") {
    if (!state().combat) return false;
    const hero = currentCombatHero();
    if (!hero) return false;
    const diceState = ensureHeroDiceTurn(hero);
    if (!diceState || diceState.phase !== "select") return false;
    if (!diceState?.selectedRollIds?.length) {
      if (hasUsableCombatRoll(hero)) return false;
      state().combat.log.push(`${hero.name}은 사용 가능한 주사위가 없어 턴을 넘긴다.`);
      finishHeroAction(hero);
      return true;
    }
    const result = diceCombat.resolveSelectedRolls({
      combat: state().combat,
      hero,
      party: state().party,
      enemies: livingCombatEnemies(),
      targetId,
    });
    if (result.targetRequired) {
      render();
      return false;
    }
    result.logs.forEach((entry) => state().combat.log.push(entry));
    applyHeroSkillProgress(hero, result);
    finishHeroAction(hero);
    return true;
  }

  function resolveHeroAction(action, targetId = "") {
    if (!state().combat) return false;
    const hero = currentCombatHero();
    if (!hero) return false;
    if (action === "stop") return stopCombatDice();
    if (action === "flee") {
      const blockedByBoss = livingCombatEnemies().some((enemy) => enemy.boss);
      if (blockedByBoss || Math.random() > 0.35) {
        state().combat.log.push(blockedByBoss ? "보스의 저주가 도주로를 막았다." : "도주에 실패했다.");
        return finishHeroAction(hero);
      }
      restorePreEncounterPosition();
      state().combat = null;
      state().preEncounterSnapshot = null;
      state().mode = "dungeon";
      addLog("파티가 조우 직전 위치로 후퇴했다.");
      render();
      return true;
    }
    if (action === "item") {
      const consumables = combatConsumableEntries();
      if (!consumables.length) {
        state().combat.log.push(`${hero.name}이 전투 중에 쓸 소모품을 찾지 못했다.`);
        render();
        return false;
      }
      state().combat.itemMode = true;
      state().combat.pendingItemIndex = -1;
      state().combat.pendingItemPreviewIndex = -1;
      render();
      return true;
    }
    if (action === "swap") {
      swapHeroForCombat(hero);
      return true;
    }
    return resolveSelectedDice(targetId);
  }

  function queueCombatAction(action) {
    if (!state().combat || !currentCombatHero()) return;
    resolveHeroAction(action);
  }

  function assignHeroSkillToDieFace(heroId = "", dieId = "", faceIndex = -1, skillId = "") {
    const hero = state().party.find((entry) => entry.id === heroId);
    if (!hero) return false;
    const changed = assignSkillToDieFace(hero, dieId, faceIndex, skillId);
    if (changed && state().combat?.diceState?.heroId === hero.id) ensureHeroDiceTurn(hero, { force: true });
    return changed;
  }

  function useCombatConsumable(heroIndex, inventoryIndex) {
    if (!state().combat) return false;
    const actor = currentCombatHero();
    const targetHero = state().party[heroIndex];
    const entry = state().inventory[inventoryIndex];
    const itemId = inventoryEntryItemId(entry);
    const item = items[itemId];
    if (!actor) return false;
    if (useInventoryEntryOnHero(heroIndex, inventoryIndex)) {
      if (item?.heal) state().combat.log.push(`${actor.name}이 ${targetHero?.name || "대상"}에게 ${item.name}를 써 HP를 회복시켰다.`);
      else if (item?.cure) state().combat.log.push(`${actor.name}이 ${targetHero?.name || "대상"}에게 ${item.name}를 써 ${item.cure} 상태를 치료했다.`);
      else state().combat.log.push(`${actor.name}이 ${item?.name || "소모품"}을 사용했다.`);
      finishHeroAction(actor);
      return true;
    }
    render();
    return false;
  }

  function useCombatThrowItem(inventoryIndex, targetEnemyId = "") {
    if (!state().combat) return false;
    const actor = currentCombatHero();
    const entry = state().inventory[inventoryIndex];
    const itemId = inventoryEntryItemId(entry);
    const item = items[itemId];
    if (!actor || !item || !item.throwDamage) return false;
    const targets = item.targetMode === "all_enemies"
      ? livingCombatEnemies()
      : [livingCombatEnemies().find((enemy) => enemy.id === targetEnemyId) || livingCombatEnemies()[0]].filter(Boolean);
    if (!targets.length) return false;
    if (!removeInventoryEntryAt(inventoryIndex)) return false;
    const damage = Number(item.throwDamage || 0);
    targets.forEach((enemy) => {
      const dealt = Math.max(1, damage + rand(0, 2) - Math.max(0, Math.floor(Number(enemy.def || 0) / 2)));
      enemy.hp = Math.max(0, enemy.hp - dealt);
      state().combat.log.push(`${actor.name}이 ${item.name}를 ${item.targetMode === "all_enemies" ? "던져" : "투척해"} ${enemy.name}에게 ${dealt} 피해를 입혔다.`);
      if (enemy.hp <= 0) state().combat.log.push(`${enemy.name}이 쓰러졌다.`);
    });
    finishHeroAction(actor);
    return true;
  }

  function winCombat() {
    const defeatedEnemies = state().combat.enemies;
    const totalXp = defeatedEnemies.reduce((sum, enemy) => sum + Number(enemy.xp || 0), 0);
    livingParty().forEach((hero) => {
      hero.xp += totalXp;
    });
    defeatedEnemies.filter((enemy) => enemy.boss).forEach((enemy) => {
      state().quest.bossesDefeated[enemy.monsterId || enemy.id] = true;
    });
    const questCompletion = updateBoardQuestCompletion(state().quest);
    if (questCompletion.completed) {
      addLog(`${questCompletion.runtime.title} 의뢰 조건을 완료했다. 귀환 버튼으로 마을에 돌아가 보상을 정산할 수 있다.`);
    }
    const combatMap = state().floorMaps[state().combat.floor];
    const placement = combatMap?.placements.find((entry) => entry.id === state().combat.placementId);
    if (placement) {
      placement.done = true;
      if (state().fieldMonsters && typeof state().fieldMonsters === "object") {
        delete state().fieldMonsters[placement.stateKey || placement.id];
      }
    }
    const rewards = defeatedEnemies.flatMap((enemy) => lootItems(combatLootTable(enemy), {
      enemy,
      floor: state().combat.floor,
      bossesDefeated: Object.keys(state().quest.bossesDefeated || {}).length,
    }));
    rewards.forEach((reward) => {
      if (typeof reward === "string") pushInventoryItemId(reward);
      else pushInventoryEntry(reward);
    });
    addLog(`${defeatedEnemies.map((enemy) => enemy.name).join(", ")}을 쓰러뜨렸다. XP ${totalXp}, ${rewards.map((entry) => inventoryEntryLabel(entry)).join(", ") || "보상 없음"} 획득.`);
    state().combat = null;
    state().preEncounterSnapshot = null;
    state().mode = "dungeon";
    render();
  }

  function rand(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }

  return {
    placementEncounterId,
    startCombat,
    livingParty,
    livingCombatEnemies,
    currentCombatHero,
    currentDiceState,
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
    stopCombatDice,
    resolveSelectedDice,
    assignHeroSkillToDieFace,
    useCombatConsumable,
    useCombatThrowItem,
    enemyTurn,
    winCombat,
    rand,
  };
}

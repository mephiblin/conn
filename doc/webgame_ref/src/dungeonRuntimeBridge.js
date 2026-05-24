export function createDungeonRuntimeBridge(deps = {}) {
  const {
    getState = () => ({}),
    getViewController = () => null,
    dirs = [],
    vec = {},
    interactivePlacementKinds = new Set(),
    movementBlockingPlacementKinds = new Set(),
    blocksMovement = () => false,
    collectPlacementsAt = () => [],
    getCell = () => null,
    logicalPlayerCell = (player) => ({
      x: Math.floor(Number(player?.x) + 0.5),
      y: Math.floor(Number(player?.y) + 0.5),
    }),
    logicalCellKey = (player) => {
      const cell = logicalPlayerCell(player);
      return `${cell.x},${cell.y}`;
    },
    resolveLookDirection = () => "north",
    resolveDoorAtFront = () => null,
    resolveInteractionCandidate = () => ({ placements: [] }),
    resolveStairsOutcome = () => ({ kind: "none" }),
    wallKey = () => "",
    oppositeDoor = () => "",
    pushInventoryItemId = () => {},
    resolvePlacementEvent = () => null,
    eventUsageState = () => ({}),
    canDetectTrap = () => false,
    canDisarmTrap = () => false,
    runNpcPlacement = () => false,
    runTypedEventEffects = () => false,
    startCombat = () => {},
    livingParty = () => [],
    hasInventoryItem = () => false,
    computeWalls = () => {},
    addLog = () => {},
    render = () => {},
    advanceWorldTurn = () => {},
    tickFieldMonsters = () => null,
    activateFloor = () => {},
    activateTownState = () => {},
    completeFinalEnding = () => {},
    items = {},
    monsters = {},
  } = deps;

  function blocks(map, x, y, dir, allowUnlockableDoors = false) {
    return blocksMovement(map, x, y, dir, vec, { allowUnlockableDoors });
  }

  function activeMovementBlockersAt(map, x, y) {
    return collectPlacementsAt(
      map,
      x,
      y,
      (placement) => !placement.done && movementBlockingPlacementKinds.has(placement.kind)
    );
  }

  function yawForFacing(facing) {
    if (facing === "east") return -Math.PI / 2;
    if (facing === "south") return Math.PI;
    if (facing === "west") return Math.PI / 2;
    return 0;
  }

  function normalizeRadians(value) {
    const circle = Math.PI * 2;
    return ((((value + Math.PI) % circle) + circle) % circle) - Math.PI;
  }

  function focusCameraOnPlacement(placement, pitch = 0) {
    const state = getState();
    const viewController = getViewController();
    if (!placement?.position || !viewController?.setPointerLook) return;
    const dx = placement.position.x - state.player.x;
    const dy = placement.position.y - state.player.y;
    if (!dx && !dy) return;
    const desiredYaw = Math.atan2(-dx, -dy);
    viewController.setPointerLook({
      yaw: normalizeRadians(desiredYaw - yawForFacing(state.player.facing)),
      pitch,
    });
  }

  function spendTorch(n) {
    const state = getState();
    state.resources.torch = Math.max(0, state.resources.torch - n);
    if (state.resources.torch === 0 && !state.flags.darkWarned) {
      state.flags.darkWarned = true;
      addLog("횃불이 꺼졌다. 어둠 속에서 명중과 회피가 낮아진다.");
    }
  }

  function runEventPlacement(placement, triggerType) {
    if (!placement) return false;
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
    if (runTypedEventEffects(placement, event, runtime)) {
      return true;
    }
    addLog(`${event.name} 이벤트 처리기가 아직 없다.`);
    return false;
  }

  function runInteractivePlacement(placement) {
    if (!placement || placement.done) return false;
    if (placement.kind === "npc") return runNpcPlacement(placement);
    if (placement.kind === "encounter" || placement.kind === "monster") {
      startCombat(placement);
      return true;
    }
    return runEventPlacement(placement, "interact");
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

  function advanceDungeonWorld() {
    advanceWorldTurn();
    const combatPlacement = tickFieldMonsters();
    if (combatPlacement && !combatPlacement.done) startCombat(combatPlacement);
    return combatPlacement;
  }

  function afterMove() {
    const state = getState();
    const currentCell = logicalPlayerCell(state.player);
    const placements = collectPlacementsAt(state.map, currentCell.x, currentCell.y, (placement) => !placement.done);
    for (const placement of placements) {
      if (placement.kind === "encounter" || placement.kind === "monster") startCombat(placement);
      if (placement.kind === "item") {
        placement.done = true;
        const itemId = placement.refId || placement.itemId;
        pushInventoryItemId(itemId);
        addLog(`${items[itemId].name}을 획득했다.`);
      }
      if (placement.kind === "trap") {
        const event = resolvePlacementEvent(placement);
        const runtime = eventUsageState(placement, event);
        if (!runtime.disarmed && !runtime.detected && canDetectTrap() && event?.detection) {
          runtime.detected = true;
          addLog("파티가 바닥의 미세한 균열을 보고 함정 기척을 감지했다.");
        } else if (!runtime.disarmed) {
          runEventPlacement(placement, "onEnter");
        }
      }
      if (placement.kind === "stairs") {
        const outcome = resolveStairsOutcome(placement, state.quest.bossesDefeated, state.flags);
        if (outcome.kind === "blockedBoss") {
          addLog(`${monsters[outcome.bossId].name}의 봉인이 계단을 막고 있다.`);
        } else if (outcome.kind === "blockedFlag") {
          addLog(outcome.message || "어딘가의 봉인이 아직 풀리지 않아 계단이 움직이지 않는다.");
        } else if (outcome.kind === "finalVictory") {
          completeFinalEnding(placement);
          addLog("세 번째 계단 아래에서 뱀의 심장 봉인이 드러난다. MVP 승리 조건 달성.");
        } else if (outcome.kind === "targetMode" && outcome.targetMode === "town") {
          activateTownState(state, outcome.target || {});
          addLog(placement.note || "지상으로 이어지는 출구를 통해 마을로 복귀했다.");
        } else if (outcome.kind === "targetFloor") {
          activateFloor(outcome.targetFloor);
        } else {
          addLog("계단이 아래 어둠으로 이어진다.");
        }
      }
    }
    advanceDungeonWorld();
    render();
  }

  function interact() {
    const state = getState();
    const currentCell = logicalPlayerCell(state.player);
    const lookYaw = getViewController()?.getPointerLookState?.().lookYaw || 0;
    const dir = resolveLookDirection(state.player.facing, lookYaw, dirs);
    const door = resolveDoorAtFront(state.map, state.player, dir, wallKey, oppositeDoor);
    if (door) {
      if (door.locked && !hasInventoryItem(door.keyId)) {
        addLog(`${items[door.keyId]?.name || "열쇠"}가 없어 문이 꿈쩍하지 않는다.`);
      } else {
        door.locked = false;
        door.open = true;
        computeWalls(state.map);
        addLog(door.type === "secret" ? "비밀문이 모래를 흘리며 열렸다." : "무거운 문이 열렸다.");
        advanceDungeonWorld();
      }
      render();
      return;
    }
    const lookCandidate = resolveInteractionCandidate(
      state.map,
      state.player,
      lookYaw,
      dirs,
      vec,
      (placement) => interactivePlacementKinds.has(placement.kind) && !placement.done
    );
    const aimedPlacement = lookCandidate.placements[0];
    if (aimedPlacement) {
      const handled = runInteractivePlacement(aimedPlacement);
      if (handled) {
        if (!state.interaction) advanceDungeonWorld();
        render();
        return;
      }
    }
    const here = collectPlacementsAt(
      state.map,
      currentCell.x,
      currentCell.y,
      (placement) => interactivePlacementKinds.has(placement.kind) && !placement.done
    )[0];
    if (here) {
      const handled = runInteractivePlacement(here);
      if (handled) {
        if (!state.interaction) advanceDungeonWorld();
        render();
        return;
      }
      if (here.kind === "camp" || here.kind === "rest_site") {
        addLog("이 자리에서는 상호작용보다 야영 명령으로 쉬어야 한다.");
        render();
        return;
      }
    }
    const trapHere = collectPlacementsAt(
      state.map,
      currentCell.x,
      currentCell.y,
      (placement) => placement.kind === "trap" && !placement.done
    )[0];
    if (trapHere) {
      const event = resolvePlacementEvent(trapHere);
      const runtime = eventUsageState(trapHere, event);
      if (runtime.detected && !runtime.disarmed) {
        if (canDisarmTrap()) {
          runtime.disarmed = true;
          trapHere.done = true;
          addLog("도적이 함정 구조를 읽고 안전하게 해제했다.");
          advanceDungeonWorld();
        } else if (event?.disarm?.onFailure === "trigger") {
          addLog("함정 해제에 실패해 기제가 튀어 올랐다.");
          runEventPlacement(trapHere, "onEnter");
          advanceDungeonWorld();
        }
        render();
        return;
      }
    }
    addLog("손에 닿는 것은 차갑고 낡은 돌뿐이다.");
    render();
  }

  function rest() {
    const state = getState();
    if (state.interaction) return;
    const currentCell = logicalPlayerCell(state.player);
    const herePlacements = state.map.placements.filter((placement) => (
      placement.position.x === currentCell.x
      && placement.position.y === currentCell.y
      && !placement.done
    ));
    const campPlacement = herePlacements.find((placement) => placement.kind === "camp");
    const restPlacement = herePlacements.find((placement) => placement.kind === "rest_site");
    if (campPlacement && runEventPlacement(campPlacement, "onCamp")) {
      if (!state.interaction) advanceDungeonWorld();
      render();
      return;
    }
    if (restPlacement && runEventPlacement(restPlacement, "onRest")) {
      if (!state.interaction) advanceDungeonWorld();
      render();
      return;
    }
    const cell = getCell(state.map, currentCell.x, currentCell.y);
    if (!cell?.tags?.includes("safe") && !cell?.tags?.includes("camp_allowed")) {
      addLog("이 칸은 너무 불안정해 야영할 수 없다.");
      render();
      return;
    }
    if (state.resources.food <= 0 || state.resources.water <= 0) {
      addLog("식량이나 물이 부족해 야영할 수 없다.");
      render();
      return;
    }
    state.resources.food -= 1;
    state.resources.water -= 1;
    for (const hero of livingParty()) hero.hp = Math.min(hero.maxHp, hero.hp + 6);
    addLog("짧은 야영으로 상처를 돌봤다.");
    advanceDungeonWorld();
    render();
  }

  return {
    blocks,
    activeMovementBlockersAt,
    yawForFacing,
    normalizeRadians,
    focusCameraOnPlacement,
    logicalCellKey,
    spendTorch,
    afterMove,
    interact,
    runInteractivePlacement,
    triggerTrap,
    runEventPlacement,
    rest,
  };
}

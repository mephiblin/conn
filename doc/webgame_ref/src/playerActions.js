import {
  blocksMovement,
  collectExitPlacements,
  collectPlacementsAt,
  getCell,
  logicalCellKey,
  logicalPlayerCell,
  movementBlocked as runtimeMovementBlocked,
  movementDeltaForAction as runtimeMovementDeltaForAction,
  resolveTurnFacing,
} from "./runtimeCore.js";

const DIRS = Object.freeze(["north", "east", "south", "west"]);
const VEC = Object.freeze({
  north: Object.freeze({ x: 0, y: -1 }),
  east: Object.freeze({ x: 1, y: 0 }),
  south: Object.freeze({ x: 0, y: 1 }),
  west: Object.freeze({ x: -1, y: 0 }),
});
const MOVEMENT_BLOCKING_PLACEMENT_KINDS = new Set(["encounter", "monster", "npc"]);
const DEFAULT_MOVE_SPEED = 2.8;
const PLAYER_COLLISION_RADIUS = 0.18;
const MOVEMENT_MODES = new Set(["town", "dungeon"]);

function defaultAllowedInteractionsForPlacementKind(kind) {
  if (kind === "trap") return ["onEnter"];
  if (kind === "event_trigger") return ["interact", "onEnter", "onExit"];
  if (kind === "rest_site") return ["interact", "onRest"];
  if (kind === "camp") return ["interact", "onCamp"];
  return ["interact"];
}

function yawForFacing(facing) {
  if (facing === "east") return -Math.PI / 2;
  if (facing === "south") return Math.PI;
  if (facing === "west") return Math.PI / 2;
  return 0;
}

export function movementDeltaForAction(kind, player, getPointerLookState = () => null) {
  const lookYaw = getPointerLookState()?.lookYaw || 0;
  return runtimeMovementDeltaForAction(kind, player?.facing, lookYaw, yawForFacing, DIRS, VEC);
}

export function activeMovementBlockersAt(map, x, y) {
  return collectPlacementsAt(
    map,
    x,
    y,
    (placement) => !placement.done && MOVEMENT_BLOCKING_PLACEMENT_KINDS.has(placement.kind)
  );
}

export function movementBlocked(map, x, y, dx, dy, {
  blockersAt = activeMovementBlockersAt,
  blocks = (targetMap, targetX, targetY, dir) => blocksMovement(targetMap, targetX, targetY, dir, VEC),
} = {}) {
  return runtimeMovementBlocked(map, x, y, dx, dy, {
    getCell,
    activeMovementBlockersAt: blockersAt,
    blocks,
  });
}

function normalizeRadians(value) {
  const circle = Math.PI * 2;
  return ((((value + Math.PI) % circle) + circle) % circle) - Math.PI;
}

function resolveMoveYaw(player, getPointerLookState = () => null) {
  const lookYaw = getPointerLookState()?.lookYaw || 0;
  return normalizeRadians(yawForFacing(player?.facing) + lookYaw);
}

function cellBlocksContinuousMovement(map, cellX, cellY, blockersAt) {
  const cell = getCell(map, cellX, cellY);
  if (!cell?.walkable) return true;
  return blockersAt(map, cellX, cellY).length > 0;
}

function moveAxisWithCollision(state, axis, delta, blockersAt, blocks) {
  if (!delta) return { moved: false, changedCell: false };
  const player = state.player;
  const currentCell = logicalPlayerCell(player);
  const direction = delta > 0 ? 1 : -1;
  const dirKey = axis === "x"
    ? (direction > 0 ? "east" : "west")
    : (direction > 0 ? "south" : "north");
  const currentValue = axis === "x" ? player.x : player.y;
  const currentCellValue = axis === "x" ? currentCell.x : currentCell.y;
  const boundary = currentCellValue + (direction > 0 ? 0.5 : -0.5);
  const boundaryLimit = boundary - direction * PLAYER_COLLISION_RADIUS;
  let nextValue = currentValue + delta;

  const crossesBoundary = direction > 0
    ? nextValue > boundaryLimit
    : nextValue < boundaryLimit;

  if (crossesBoundary) {
    const nextCellX = axis === "x" ? currentCell.x + direction : currentCell.x;
    const nextCellY = axis === "y" ? currentCell.y + direction : currentCell.y;
    if (blocks(state.map, currentCell.x, currentCell.y, dirKey)
      || cellBlocksContinuousMovement(state.map, nextCellX, nextCellY, blockersAt)) {
      nextValue = boundaryLimit;
    }
  }

  const moved = Math.abs(nextValue - currentValue) > 0.00001;
  if (!moved) return { moved: false, changedCell: false };

  if (axis === "x") player.x = nextValue;
  else player.y = nextValue;

  const nextCell = logicalPlayerCell(player);
  const changedCell = nextCell.x !== currentCell.x || nextCell.y !== currentCell.y;
  return { moved: true, changedCell };
}

export function createPlayerActionRunner(deps = {}) {
  const getState = deps.getState || (() => deps.state);
  const getPointerLookState = deps.getPointerLookState || (() => deps.viewController?.getPointerLookState?.() || null);
  const addLog = deps.addLog || (() => {});
  const render = deps.render || (() => {});
  const spendTorch = deps.spendTorch || ((n) => {
    const state = getState();
    if (!state?.resources) return;
    state.resources.torch = Math.max(0, state.resources.torch - n);
    if (state.resources.torch === 0 && !state.flags?.darkWarned) {
      state.flags = state.flags || {};
      state.flags.darkWarned = true;
      addLog("횃불이 꺼졌다. 어둠 속에서 명중과 회피가 낮아진다.");
    }
  });
  const afterMove = deps.afterMove || (() => render());
  const allowedInteractionsForPlacementKind = deps.allowedInteractionsForPlacementKind
    || defaultAllowedInteractionsForPlacementKind;
  const blockersAt = deps.activeMovementBlockersAt || activeMovementBlockersAt;
  const blocks = deps.blocks || ((map, x, y, dir) => blocksMovement(map, x, y, dir, VEC));
  const moveSpeed = Number.isFinite(deps.moveSpeed) ? deps.moveSpeed : DEFAULT_MOVE_SPEED;

  function interactionOpen(state = getState()) {
    if (deps.interactionOpen) return Boolean(deps.interactionOpen(state));
    return Boolean(state?.interaction);
  }

  function inventoryOpen(state = getState()) {
    if (deps.inventoryOverlayOpen) return Boolean(deps.inventoryOverlayOpen(state?.mode));
    return ["town", "dungeon", "combat"].includes(state?.mode)
      && Boolean(state?.inventoryPanelOpen);
  }

  function skillDeckOpen(state = getState()) {
    return Boolean(state?.skillDeckOpen);
  }

  function skillShopOpen(state = getState()) {
    return Boolean(state?.skillShopOpen);
  }

  function closeInventory() {
    deps.closeInventoryOverlay?.();
  }

  function closeInteraction() {
    deps.closeInteraction?.();
  }

  function toggleInventory() {
    if (!deps.toggleInventoryOverlay) return false;
    return deps.toggleInventoryOverlay();
  }

  function toggleDragLook(force) {
    if (deps.toggleDragLook) return deps.toggleDragLook(force);

    const current = typeof deps.dragLookEnabled === "function"
      ? deps.dragLookEnabled()
      : deps.dragLookEnabled;
    const next = typeof force === "boolean" ? force : !current;
    if (current === next) return false;
    deps.setDragLookEnabled?.(next);
    addLog(next ? "마우스 FPS 시점을 활성화했다." : "마우스 FPS 시점을 비활성화했다.");
    render();
    return true;
  }

  function move(kind) {
    const state = getState();
    if (!MOVEMENT_MODES.has(state?.mode)) return false;
    if (kind === "turnLeft" || kind === "turnRight") {
      state.player.facing = resolveTurnFacing(kind, state.player.facing, deps.dirs || DIRS);
      if (state.mode === "dungeon") spendTorch(1);
      addLog(`${state.player.facing} 방향으로 몸을 돌렸다.`);
      afterMove();
      return true;
    }

    const { dx, dy } = movementDeltaForAction(kind, state.player, getPointerLookState);
    if (movementBlocked(state.map, state.player.x, state.player.y, dx, dy, { blockersAt, blocks })) {
      const blocker = blockersAt(state.map, state.player.x + dx, state.player.y + dy)[0];
      addLog(blocker ? "누군가가 길을 막고 있다. 상호작용으로 대응할 수 있다." : "돌벽이나 닫힌 문이 길을 막고 있다.");
      render();
      return false;
    }

    const previousPosition = logicalPlayerCell(state.player);
    state.player.x = previousPosition.x + dx;
    state.player.y = previousPosition.y + dy;
    const exitPlacements = collectExitPlacements(state.map, previousPosition, allowedInteractionsForPlacementKind);
    for (const placement of exitPlacements) deps.runEventPlacement?.(placement, "onExit");
    if (state.mode === "dungeon") spendTorch(2);
    state.visited?.add?.(logicalCellKey(state.player));
    afterMove();
    return true;
  }

  function applyMoveInput({ forward = 0, strafe = 0, dt = 0 } = {}) {
    const state = getState();
    if (!MOVEMENT_MODES.has(state?.mode) || interactionOpen(state) || inventoryOpen(state) || skillDeckOpen(state) || skillShopOpen(state)) return false;
    if ((!forward && !strafe) || !(dt > 0)) return false;

    const startCell = logicalPlayerCell(state.player);
    const yaw = resolveMoveYaw(state.player, getPointerLookState);
    const forwardX = -Math.sin(yaw);
    const forwardY = -Math.cos(yaw);
    const strafeX = Math.cos(yaw);
    const strafeY = -Math.sin(yaw);
    let moveX = forwardX * forward + strafeX * strafe;
    let moveY = forwardY * forward + strafeY * strafe;
    const length = Math.hypot(moveX, moveY);
    if (!length) return false;
    const distance = moveSpeed * dt;
    moveX = (moveX / length) * distance;
    moveY = (moveY / length) * distance;

    const firstAxis = Math.abs(moveX) >= Math.abs(moveY) ? "x" : "y";
    const secondAxis = firstAxis === "x" ? "y" : "x";
    const firstDelta = firstAxis === "x" ? moveX : moveY;
    const secondDelta = secondAxis === "x" ? moveX : moveY;

    const firstResult = moveAxisWithCollision(state, firstAxis, firstDelta, blockersAt, blocks);
    const secondResult = moveAxisWithCollision(state, secondAxis, secondDelta, blockersAt, blocks);
    if (!firstResult.moved && !secondResult.moved) return false;

    const endCell = logicalPlayerCell(state.player);
    if (endCell.x === startCell.x && endCell.y === startCell.y) {
      render();
      return true;
    }

    const exitPlacements = collectExitPlacements(state.map, startCell, allowedInteractionsForPlacementKind);
    for (const placement of exitPlacements) deps.runEventPlacement?.(placement, "onExit");
    if (state.mode === "dungeon") spendTorch(1);
    state.visited?.add?.(logicalCellKey(endCell));
    afterMove();
    return true;
  }

  function performAction(action) {
    const state = getState();
    if (action === "escape" || action === "cancel") {
      if (skillDeckOpen(state)) {
        state.skillDeckOpen = false;
        render();
        return true;
      }
      if (skillShopOpen(state)) {
        state.skillShopOpen = false;
        render();
        return true;
      }
      if (inventoryOpen(state)) {
        closeInventory();
        render();
        return true;
      }
      if (interactionOpen(state)) {
        closeInteraction();
        render();
        return true;
      }
      return false;
    }

    if (action === "inventory") {
      if (toggleInventory()) render();
      return true;
    }

    if (interactionOpen(state) || inventoryOpen(state) || skillDeckOpen(state) || skillShopOpen(state)) return false;
    if (action === "interact") {
      deps.interact?.();
      return true;
    }
    if (action === "rest") {
      deps.rest?.();
      return true;
    }
    if (action === "toggleDragLook") return toggleDragLook();
    return move(action);
  }

  return {
    performAction,
    move,
    applyMoveInput,
    movementDeltaForAction: (kind) => movementDeltaForAction(kind, getState()?.player, getPointerLookState),
    movementBlocked: (map, x, y, dx, dy) => movementBlocked(map, x, y, dx, dy, { blockersAt, blocks }),
    toggleDragLook,
  };
}

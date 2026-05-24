export function getCell(map, x, y) {
  return map?.cells?.find((cell) => cell.x === x && cell.y === y) || null;
}

export function logicalCellCoord(value) {
  return Math.floor(Number(value) + 0.5);
}

export function logicalPlayerCell(playerOrX, y) {
  if (typeof playerOrX === "object" && playerOrX) {
    return {
      x: logicalCellCoord(playerOrX.x),
      y: logicalCellCoord(playerOrX.y),
    };
  }
  return {
    x: logicalCellCoord(playerOrX),
    y: logicalCellCoord(y),
  };
}

export function logicalCellKey(playerOrX, y) {
  const cell = logicalPlayerCell(playerOrX, y);
  return `${cell.x},${cell.y}`;
}

export function blocksMovement(map, x, y, dir, vectors, { allowUnlockableDoors = false } = {}) {
  const cell = getCell(map, x, y);
  const wall = cell?.walls?.[dir];
  if (allowUnlockableDoors && wall && (wall.type === "door" || wall.type === "secret")) return false;
  if (wall?.blocksMovement) return true;
  const vector = vectors[dir];
  const next = vector ? getCell(map, x + vector.x, y + vector.y) : null;
  return !next || !next.walkable;
}

export function resolveActionDirection(action, facing, dirs) {
  if (action === "backward") return dirs[(dirs.indexOf(facing) + 2) % 4];
  if (action === "strafeLeft") return dirs[(dirs.indexOf(facing) + 3) % 4];
  if (action === "strafeRight") return dirs[(dirs.indexOf(facing) + 1) % 4];
  return facing;
}

export function resolveTurnFacing(action, facing, dirs) {
  const delta = action === "turnLeft" ? 3 : 1;
  return dirs[(dirs.indexOf(facing) + delta) % 4];
}

export function resolveLookDirection(facing, lookYaw = 0, dirs) {
  const baseIndex = Math.max(0, dirs.indexOf(facing));
  const quarterTurn = Math.PI / 2;
  const offsetSteps = Math.round(lookYaw / quarterTurn);
  return dirs[(baseIndex - offsetSteps + dirs.length * 4) % dirs.length];
}

export function directionFromDelta(dx, dy) {
  if (dx === 0 && dy < 0) return "north";
  if (dx > 0 && dy === 0) return "east";
  if (dx === 0 && dy > 0) return "south";
  if (dx < 0 && dy === 0) return "west";
  return "";
}

export function movementDeltaForAction(kind, facing, lookYaw = 0, yawForFacing, dirs, vectors) {
  let yaw = typeof yawForFacing === "function"
    ? yawForFacing(facing)
    : 0;
  yaw += lookYaw || 0;
  if (kind === "backward") yaw += Math.PI;
  if (kind === "strafeLeft") yaw += Math.PI / 2;
  if (kind === "strafeRight") yaw -= Math.PI / 2;
  if (kind === "forwardLeft") yaw += Math.PI / 4;
  if (kind === "forwardRight") yaw -= Math.PI / 4;

  const dx = Math.round(-Math.sin(yaw));
  const dy = Math.round(-Math.cos(yaw));
  if (vectors && dirs?.length) {
    const matchingDir = dirs.find((dir) => vectors[dir]?.x === dx && vectors[dir]?.y === dy);
    if (matchingDir) return { dx: vectors[matchingDir].x, dy: vectors[matchingDir].y };
  }
  return { dx, dy };
}

export function movementBlocked(map, x, y, dx, dy, deps = {}) {
  const {
    getCell: getCellFn = getCell,
    activeMovementBlockersAt,
    movementBlockingPlacementKinds,
    blocks,
    vectors,
    allowUnlockableDoors = false,
    directionFromDelta: directionFromDeltaFn = directionFromDelta,
  } = deps;
  const blockersAt = typeof activeMovementBlockersAt === "function"
    ? activeMovementBlockersAt
    : (blockerMap, blockerX, blockerY) => {
      if (!movementBlockingPlacementKinds) return [];
      return collectPlacementsAt(
        blockerMap,
        blockerX,
        blockerY,
        (placement) => !placement.done && movementBlockingPlacementKinds.has(placement.kind)
      );
    };
  const blocksFn = typeof blocks === "function"
    ? blocks
    : (blockMap, blockX, blockY, dir) => blocksMovement(
      blockMap,
      blockX,
      blockY,
      dir,
      vectors || {},
      { allowUnlockableDoors }
    );

  if (!dx && !dy) return true;
  const targetX = x + dx;
  const targetY = y + dy;
  const targetCell = getCellFn(map, targetX, targetY);
  if (!targetCell?.walkable) return true;
  if (blockersAt(map, targetX, targetY).length) return true;
  const xDir = directionFromDeltaFn(dx, 0);
  const yDir = directionFromDeltaFn(0, dy);
  if (xDir && blocksFn(map, x, y, xDir)) return true;
  if (yDir && blocksFn(map, x, y, yDir)) return true;
  if (xDir && yDir) {
    if (blockersAt(map, x + dx, y).length || blockersAt(map, x, y + dy).length) return true;
    if (blocksFn(map, x + dx, y, yDir) || blocksFn(map, x, y + dy, xDir)) return true;
  }
  return false;
}

export function collectPlacementsAt(map, x, y, predicate = () => true) {
  return (map?.placements || []).filter((placement) => placement.position?.x === x
    && placement.position?.y === y
    && predicate(placement));
}

export function collectExitPlacements(map, from, allowedInteractionsForPlacementKind) {
  return collectPlacementsAt(
    map,
    from.x,
    from.y,
    (placement) => !placement.done && allowedInteractionsForPlacementKind(placement.kind).includes("onExit")
  );
}

export function resolveDoorAtFront(map, player, dir, wallKeyFn, oppositeDoorFn) {
  const current = logicalPlayerCell(player);
  const key = wallKeyFn(current.x, current.y, dir);
  return map.doors[key] || oppositeDoorFn(map, current.x, current.y, dir) || null;
}

export function resolveInteractionCandidate(map, player, lookYaw, dirs, vectors, predicate = () => true) {
  const current = logicalPlayerCell(player);
  const dir = resolveLookDirection(player.facing, lookYaw, dirs);
  const vector = vectors[dir];
  const target = vector ? { x: current.x + vector.x, y: current.y + vector.y } : { x: current.x, y: current.y };
  return {
    dir,
    target,
    cell: getCell(map, target.x, target.y),
    placements: collectPlacementsAt(map, target.x, target.y, predicate),
  };
}

export function resolveStairsOutcome(placement, bossesDefeated = {}, flags = {}) {
  if (placement.requiredBoss && !bossesDefeated[placement.requiredBoss]) {
    return { kind: "blockedBoss", bossId: placement.requiredBoss };
  }
  if (placement.requiredFlag && !flags[placement.requiredFlag]) {
    return {
      kind: "blockedFlag",
      flagId: placement.requiredFlag,
      message: placement.blockedMessage || "",
    };
  }
  if (placement.final) return { kind: "finalVictory" };
  if (placement.targetMode === "town") {
    return {
      kind: "targetMode",
      targetMode: "town",
      target: placement.target && typeof placement.target === "object"
        ? {
          x: Number.isFinite(Number(placement.target.x)) ? Number(placement.target.x) : undefined,
          y: Number.isFinite(Number(placement.target.y)) ? Number(placement.target.y) : undefined,
          facing: typeof placement.target.facing === "string" ? placement.target.facing : undefined,
        }
        : null,
    };
  }
  if (placement.targetFloor) return { kind: "targetFloor", targetFloor: placement.targetFloor };
  return { kind: "message" };
}

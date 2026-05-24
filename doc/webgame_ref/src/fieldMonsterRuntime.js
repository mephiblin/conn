import {
  blocksMovement,
  collectPlacementsAt,
  directionFromDelta,
  getCell,
  logicalPlayerCell,
} from "./runtimeCore.js";

function cloneJson(value) {
  return JSON.parse(JSON.stringify(value));
}

function clampTurns(value, fallback) {
  const turns = Number(value);
  if (Number.isFinite(turns) && turns >= 0) return Math.max(0, Math.round(turns));
  return fallback;
}

function turnsFromSeconds(value, fallback) {
  const seconds = Number(value);
  if (!Number.isFinite(seconds) || seconds < 0) return fallback;
  return Math.max(1, Math.round(seconds * 2));
}

function manhattanDistance(a, b) {
  return Math.abs(Number(a?.x || 0) - Number(b?.x || 0)) + Math.abs(Number(a?.y || 0) - Number(b?.y || 0));
}

function normalizeRoute(route = [], home = null) {
  const normalized = Array.isArray(route)
    ? route
      .filter((point) => Number.isFinite(Number(point?.x)) && Number.isFinite(Number(point?.y)))
      .map((point) => ({ x: Number(point.x), y: Number(point.y) }))
    : [];
  if (!normalized.length && home) normalized.push({ x: Number(home.x), y: Number(home.y) });
  return normalized;
}

function normalizeFieldAiConfig(placement, deps = {}) {
  const encounters = deps.encounters || {};
  const monsters = deps.monsters || {};
  const home = {
    x: Number(placement?.fieldAi?.home?.x ?? placement?.position?.x ?? 0),
    y: Number(placement?.fieldAi?.home?.y ?? placement?.position?.y ?? 0),
  };
  const encounter = encounters[placement?.refId] || null;
  const enemyIds = (encounter?.enemies || []).map((enemy) => enemy?.monsterId).filter(Boolean);
  const boss = enemyIds.some((monsterId) => monsters[monsterId]?.boss);
  const initialState = placement?.fieldAi?.initialState
    || (placement?.fieldAi?.archetype === "ambush" ? "ambush" : "idle");
  const route = normalizeRoute(placement?.fieldAi?.patrolRoute, home);
  return {
    enabled: placement?.fieldAi?.enabled !== false,
    home,
    patrolRoute: route,
    initialState: ["idle", "patrol", "ambush"].includes(initialState) ? initialState : (route.length > 1 ? "patrol" : "idle"),
    visionRange: Math.max(1, Number(placement?.fieldAi?.visionRange ?? (boss ? 6 : 5))),
    hearingRange: Math.max(0, Number(placement?.fieldAi?.hearingRange ?? 1)),
    engageRange: Math.max(0, Number(placement?.fieldAi?.engageRange ?? 1)),
    leashRange: Math.max(1, Number(placement?.fieldAi?.leashRange ?? (boss ? 9 : 7))),
    alertTurns: clampTurns(placement?.fieldAi?.alertTurns, turnsFromSeconds(placement?.fieldAi?.alertSeconds, 2)),
    loseSightTurns: clampTurns(placement?.fieldAi?.loseSightTurns, turnsFromSeconds(placement?.fieldAi?.loseSightSeconds, 4)),
    patrolInterval: clampTurns(placement?.fieldAi?.patrolInterval, 2),
    returnMode: placement?.fieldAi?.returnMode === "route" ? "route" : "home",
  };
}

function isFieldMonsterPlacement(placement) {
  return Boolean(placement && !placement.done && (placement.kind === "encounter" || placement.kind === "monster"));
}

function placementRuntimeKey(placement) {
  return placement?.stateKey || placement?.id || "";
}

function clearLineOfSight(map, from, to, vec) {
  if (!map || !from || !to) return false;
  if (from.x !== to.x && from.y !== to.y) return false;
  const dx = Math.sign(to.x - from.x);
  const dy = Math.sign(to.y - from.y);
  if (!dx && !dy) return true;
  const dir = directionFromDelta(dx, dy);
  if (!dir || !vec[dir]) return false;
  let x = from.x;
  let y = from.y;
  while (x !== to.x || y !== to.y) {
    if (blocksMovement(map, x, y, dir, vec)) return false;
    x += dx;
    y += dy;
  }
  return true;
}

function candidateSteps(from, to) {
  const dx = Number(to?.x || 0) - Number(from?.x || 0);
  const dy = Number(to?.y || 0) - Number(from?.y || 0);
  const steps = [];
  if (Math.abs(dx) >= Math.abs(dy)) {
    if (dx) steps.push({ x: Math.sign(dx), y: 0 });
    if (dy) steps.push({ x: 0, y: Math.sign(dy) });
  } else {
    if (dy) steps.push({ x: 0, y: Math.sign(dy) });
    if (dx) steps.push({ x: Math.sign(dx), y: 0 });
  }
  return steps;
}

export function createFieldMonsterRuntime(deps = {}) {
  const getState = deps.getState || (() => deps.state || {});
  const encounters = deps.encounters || {};
  const monsters = deps.monsters || {};
  const vec = deps.vec || {};
  const movementBlockingPlacementKinds = deps.movementBlockingPlacementKinds || new Set(["encounter", "monster", "npc"]);

  function state() {
    return getState();
  }

  function fieldMonsterTable() {
    const currentState = state();
    if (!currentState.fieldMonsters || typeof currentState.fieldMonsters !== "object") currentState.fieldMonsters = {};
    return currentState.fieldMonsters;
  }

  function normalizeFieldMonsterStateTable(value = {}) {
    if (!value || typeof value !== "object" || Array.isArray(value)) return {};
    const normalized = {};
    Object.entries(value).forEach(([key, entry]) => {
      if (!entry || typeof entry !== "object") return;
      normalized[key] = {
        state: typeof entry.state === "string" ? entry.state : "idle",
        routeIndex: Math.max(0, Number(entry.routeIndex || 0)),
        alertTurns: Math.max(0, Number(entry.alertTurns || 0)),
        lostSightTurns: Math.max(0, Number(entry.lostSightTurns || 0)),
        patrolTurns: Math.max(0, Number(entry.patrolTurns || 0)),
        lastKnownPlayerCell: entry.lastKnownPlayerCell
          && Number.isFinite(Number(entry.lastKnownPlayerCell.x))
          && Number.isFinite(Number(entry.lastKnownPlayerCell.y))
          ? {
            x: Number(entry.lastKnownPlayerCell.x),
            y: Number(entry.lastKnownPlayerCell.y),
          }
          : null,
        home: entry.home
          && Number.isFinite(Number(entry.home.x))
          && Number.isFinite(Number(entry.home.y))
          ? {
            x: Number(entry.home.x),
            y: Number(entry.home.y),
          }
          : null,
      };
    });
    return normalized;
  }

  function ensureFieldMonsterState(placement) {
    const currentMap = state().map;
    const key = placementRuntimeKey(placement);
    if (!key || !isFieldMonsterPlacement(placement)) return null;
    const config = normalizeFieldAiConfig(placement, { encounters, monsters });
    if (!config.enabled) return null;
    const table = fieldMonsterTable();
    if (!table[key] || typeof table[key] !== "object") {
      table[key] = {
        state: config.initialState,
        routeIndex: 0,
        alertTurns: 0,
        lostSightTurns: 0,
        patrolTurns: 0,
        lastKnownPlayerCell: null,
        home: cloneJson(config.home),
      };
    }
    const runtime = table[key];
    runtime.home = runtime.home || cloneJson(config.home);
    if (!Number.isFinite(Number(placement.position?.x)) || !Number.isFinite(Number(placement.position?.y))) {
      placement.position = { ...(placement.position || {}), x: runtime.home.x, y: runtime.home.y };
    }
    if (currentMap && placement.position?.floor == null && Number.isFinite(Number(currentMap.floor))) {
      placement.position.floor = Number(currentMap.floor);
    }
    return runtime;
  }

  function syncFieldMonsterTableForMap(map) {
    if (!map) return fieldMonsterTable();
    const table = fieldMonsterTable();
    const activeKeys = new Set();
    const mapKeys = new Set();
    for (const placement of map.placements || []) {
      const key = placementRuntimeKey(placement);
      if (key) mapKeys.add(key);
      if (!isFieldMonsterPlacement(placement)) continue;
      if (!key) continue;
      const runtime = ensureFieldMonsterState(placement);
      if (!runtime) continue;
      activeKeys.add(key);
    }
    Object.keys(table).forEach((key) => {
      if (mapKeys.has(key) && !activeKeys.has(key)) delete table[key];
    });
    return table;
  }

  function syncAllFieldMonsterTables() {
    const activeKeys = new Set();
    Object.values(state().floorMaps || {}).forEach((map) => {
      syncFieldMonsterTableForMap(map);
      (map?.placements || []).forEach((placement) => {
        if (isFieldMonsterPlacement(placement)) activeKeys.add(placementRuntimeKey(placement));
      });
    });
    Object.keys(fieldMonsterTable()).forEach((key) => {
      if (!activeKeys.has(key)) delete fieldMonsterTable()[key];
    });
    return fieldMonsterTable();
  }

  function canPlacementStepTo(placement, next, map, playerCell) {
    const current = logicalPlayerCell(placement.position);
    if (!next || (next.x === playerCell.x && next.y === playerCell.y)) return false;
    const cell = getCell(map, next.x, next.y);
    if (!cell?.walkable) return false;
    const dir = directionFromDelta(next.x - current.x, next.y - current.y);
    if (!dir || blocksMovement(map, current.x, current.y, dir, vec)) return false;
    const blockers = collectPlacementsAt(
      map,
      next.x,
      next.y,
      (entry) => !entry.done
        && placementRuntimeKey(entry) !== placementRuntimeKey(placement)
        && movementBlockingPlacementKinds.has(entry.kind)
    );
    return blockers.length === 0;
  }

  function movePlacementToward(placement, targetCell, map, playerCell) {
    const current = logicalPlayerCell(placement.position);
    if (current.x === targetCell.x && current.y === targetCell.y) return false;
    for (const step of candidateSteps(current, targetCell)) {
      const next = { x: current.x + step.x, y: current.y + step.y };
      if (!canPlacementStepTo(placement, next, map, playerCell)) continue;
      placement.position.x = next.x;
      placement.position.y = next.y;
      return true;
    }
    return false;
  }

  function patrolTarget(runtime, config) {
    const route = config.patrolRoute;
    if (!route.length) return config.home;
    const index = Math.min(Math.max(0, Number(runtime.routeIndex || 0)), route.length - 1);
    return route[index];
  }

  function advancePatrolIndex(runtime, config) {
    const route = config.patrolRoute;
    if (!route.length) return 0;
    const current = Math.min(Math.max(0, Number(runtime.routeIndex || 0)), route.length - 1);
    runtime.routeIndex = (current + 1) % route.length;
    return runtime.routeIndex;
  }

  function monsterSensesPlayer(placement, playerCell, map, config) {
    const origin = logicalPlayerCell(placement.position);
    const distance = manhattanDistance(origin, playerCell);
    if (distance <= config.hearingRange) return true;
    if (distance > config.visionRange) return false;
    return clearLineOfSight(map, origin, playerCell, vec);
  }

  function shouldEngage(placement, playerCell, config) {
    return manhattanDistance(logicalPlayerCell(placement.position), playerCell) <= config.engageRange;
  }

  function returnTarget(runtime, config) {
    if (config.returnMode === "route" && config.patrolRoute.length) {
      const index = Math.min(Math.max(0, Number(runtime.routeIndex || 0)), config.patrolRoute.length - 1);
      return config.patrolRoute[index];
    }
    return runtime.home || config.home;
  }

  function tickFieldMonsters() {
    const currentState = state();
    const map = currentState.map;
    if (currentState.mode !== "dungeon" || !map) return null;
    syncFieldMonsterTableForMap(map);
    const playerCell = logicalPlayerCell(currentState.player);
    for (const placement of map.placements || []) {
      if (!isFieldMonsterPlacement(placement)) continue;
      const runtime = ensureFieldMonsterState(placement);
      if (!runtime) continue;
      const config = normalizeFieldAiConfig(placement, { encounters, monsters });
      const homeDistance = manhattanDistance(logicalPlayerCell(placement.position), runtime.home || config.home);
      const sensesPlayer = monsterSensesPlayer(placement, playerCell, map, config);
      if (placement.done) {
        delete fieldMonsterTable()[placementRuntimeKey(placement)];
        continue;
      }
      if (runtime.state === "ambush" && shouldEngage(placement, playerCell, config)) {
        runtime.state = "combat";
        return placement;
      }
      if (runtime.state === "idle" || runtime.state === "patrol" || runtime.state === "ambush") {
        if (sensesPlayer) {
          runtime.state = "alert";
          runtime.alertTurns = 1;
          runtime.lostSightTurns = 0;
          runtime.lastKnownPlayerCell = cloneJson(playerCell);
        } else if (runtime.state === "patrol" && config.patrolRoute.length > 1) {
          runtime.patrolTurns = Number(runtime.patrolTurns || 0) + 1;
          if (runtime.patrolTurns >= Math.max(1, config.patrolInterval)) {
            runtime.patrolTurns = 0;
            const target = patrolTarget(runtime, config);
            const current = logicalPlayerCell(placement.position);
            if (current.x === target.x && current.y === target.y) advancePatrolIndex(runtime, config);
            movePlacementToward(placement, patrolTarget(runtime, config), map, playerCell);
          }
        }
        continue;
      }
      if (runtime.state === "alert") {
        if (sensesPlayer) {
          runtime.alertTurns = Number(runtime.alertTurns || 0) + 1;
          runtime.lastKnownPlayerCell = cloneJson(playerCell);
          if (shouldEngage(placement, playerCell, config)) {
            runtime.state = "combat";
            return placement;
          }
          if (runtime.alertTurns >= Math.max(1, config.alertTurns)) runtime.state = "chase";
        } else {
          runtime.state = config.initialState === "patrol" ? "patrol" : config.initialState;
          runtime.alertTurns = 0;
          runtime.lastKnownPlayerCell = null;
        }
        continue;
      }
      if (runtime.state === "chase") {
        if (shouldEngage(placement, playerCell, config)) {
          runtime.state = "combat";
          return placement;
        }
        if (homeDistance > config.leashRange) {
          runtime.state = "give_up";
          runtime.lostSightTurns = config.loseSightTurns;
          continue;
        }
        if (sensesPlayer) {
          runtime.lostSightTurns = 0;
          runtime.lastKnownPlayerCell = cloneJson(playerCell);
          movePlacementToward(placement, playerCell, map, playerCell);
          if (shouldEngage(placement, playerCell, config)) {
            runtime.state = "combat";
            return placement;
          }
        } else {
          runtime.lostSightTurns = Number(runtime.lostSightTurns || 0) + 1;
          if (runtime.lastKnownPlayerCell) movePlacementToward(placement, runtime.lastKnownPlayerCell, map, playerCell);
          if (runtime.lostSightTurns >= Math.max(1, config.loseSightTurns)) runtime.state = "give_up";
        }
        continue;
      }
      if (runtime.state === "give_up") {
        runtime.state = "return";
        runtime.alertTurns = 0;
        runtime.lostSightTurns = 0;
        continue;
      }
      if (runtime.state === "return") {
        const target = returnTarget(runtime, config);
        const current = logicalPlayerCell(placement.position);
        if (current.x === target.x && current.y === target.y) {
          runtime.state = config.initialState;
          runtime.lastKnownPlayerCell = null;
          runtime.alertTurns = 0;
          runtime.lostSightTurns = 0;
          if (runtime.state === "patrol" && config.patrolRoute.length > 1) advancePatrolIndex(runtime, config);
          continue;
        }
        movePlacementToward(placement, target, map, playerCell);
      }
    }
    return null;
  }

  function serializeFieldMonsters() {
    const serialized = {};
    Object.entries(fieldMonsterTable()).forEach(([key, entry]) => {
      serialized[key] = {
        state: entry.state || "idle",
        routeIndex: Number(entry.routeIndex || 0),
        alertTurns: Number(entry.alertTurns || 0),
        lostSightTurns: Number(entry.lostSightTurns || 0),
        patrolTurns: Number(entry.patrolTurns || 0),
        lastKnownPlayerCell: entry.lastKnownPlayerCell ? cloneJson(entry.lastKnownPlayerCell) : null,
        home: entry.home ? cloneJson(entry.home) : null,
      };
    });
    return serialized;
  }

  return {
    isFieldMonsterPlacement,
    normalizeFieldMonsterStateTable,
    syncFieldMonsterTableForMap,
    syncAllFieldMonsterTables,
    ensureFieldMonsterState,
    tickFieldMonsters,
    serializeFieldMonsters,
  };
}

export const CONNAN_DEBUG_API_VERSION = 1;

export const DEBUG_HARNESS_CONTRACT = Object.freeze({
  name: "connan-smoke-debug-api",
  version: CONNAN_DEBUG_API_VERSION,
  owner: "scripts/smoke",
  scope: "local deterministic smoke scripts only; do not persist into saveSlot data",
  methods: ["snapshot", "teleportPlayer", "teleportToPlacementFront", "defeatEncounterPlacement", "triggerPlacement"],
});

function cloneJson(value) {
  return JSON.parse(JSON.stringify(value));
}

export function createDebugHarness({
  state,
  getState,
  dirs,
  vec,
  encounters,
  monsters,
  getCell,
  blocks,
  render,
  addLog,
  normalizeInventoryList,
  normalizeQuestState,
  updateBoardQuestCompletion = () => ({ completed: false }),
  resolveStairsOutcome,
  completeFinalEnding,
  activateFloor,
  activateTownState = () => {},
  getPointerLookState = () => null,
  logicalCellKey = (player) => `${Math.floor(Number(player?.x) + 0.5)},${Math.floor(Number(player?.y) + 0.5)}`,
}) {
  const readState = typeof getState === "function" ? getState : () => state;

  function buildDebugSnapshot() {
    const state = readState();
    state.quest = normalizeQuestState(state.quest);
    const map = state.map || null;
    return {
      debugApiVersion: CONNAN_DEBUG_API_VERSION,
      mode: state.mode,
      runtimeSession: state.runtimeSession ? {
        kind: state.runtimeSession.kind || "",
        source: state.runtimeSession.source || "",
        sourceFloor: state.runtimeSession.sourceFloor ?? null,
      } : null,
      player: cloneJson(state.player),
      resources: cloneJson(state.resources),
      flags: cloneJson(state.flags || {}),
      quest: cloneJson(state.quest || {}),
      fieldMonsters: cloneJson(state.fieldMonsters || {}),
      inventory: cloneJson(normalizeInventoryList(state.inventory)),
      interaction: state.interaction ? cloneJson(state.interaction) : null,
      controls: cloneJson(getPointerLookState() || {}),
      currentFloor: state.player?.floor ?? null,
      map: map ? {
        id: map.id,
        name: map.name,
        start: cloneJson(map.start || {}),
        size: cloneJson(map.size || {}),
        doors: cloneJson(map.doors || {}),
        cells: (map.cells || []).map((cell) => ({
          x: cell.x,
          y: cell.y,
          walkable: Boolean(cell.walkable),
          roomId: cell.roomId || null,
          walls: cloneJson(cell.walls || {}),
        })),
        placements: (map.placements || []).map((placement) => ({
          id: placement.id,
          stateKey: placement.stateKey || placement.id,
          kind: placement.kind,
          refId: placement.refId || null,
          npcId: placement.npcId || null,
          itemId: placement.itemId || null,
          targetFloor: placement.targetFloor || null,
          targetMode: placement.targetMode || null,
          requiredBoss: placement.requiredBoss || null,
          requiredFlag: placement.requiredFlag || null,
          done: Boolean(placement.done),
          position: cloneJson(placement.position || {}),
        })),
      } : null,
    };
  }

  function debugTeleportPlayer(x, y, facing = state.player?.facing || "north") {
    const state = readState();
    if ((state.mode !== "dungeon" && state.mode !== "town") || !state.map) return buildDebugSnapshot();
    const cell = getCell(state.map, Number(x), Number(y));
    if (!cell?.walkable) return buildDebugSnapshot();
    state.player.x = Number(x);
    state.player.y = Number(y);
    if (dirs.includes(facing)) state.player.facing = facing;
    state.visited.add(logicalCellKey(state.player));
    state.visitedByFloor[state.player.floor] = state.visited;
    render();
    return buildDebugSnapshot();
  }

  function debugDefeatEncounterPlacement(placementId) {
    const state = readState();
    if (!state.map) return buildDebugSnapshot();
    const placement = (state.map.placements || []).find((entry) => entry.id === placementId);
    if (!placement || (placement.kind !== "encounter" && placement.kind !== "monster")) return buildDebugSnapshot();
    placement.done = true;
    if (state.fieldMonsters && typeof state.fieldMonsters === "object") delete state.fieldMonsters[placement.stateKey || placement.id];
    const encounter = encounters[placement.refId];
    const generatedEnemies = Array.isArray(placement.generatedEnemies) && placement.generatedEnemies.length
      ? placement.generatedEnemies
      : null;
    (generatedEnemies || encounter?.enemies || []).forEach((enemy) => {
      const monster = monsters[enemy?.monsterId];
      if ((enemy?.boss || monster?.boss) && enemy?.monsterId) state.quest.bossesDefeated[enemy.monsterId] = true;
    });
    updateBoardQuestCompletion(state.quest);
    addLog(`${placement.id} 조우를 디버그 완료 처리했다.`);
    render();
    return buildDebugSnapshot();
  }

  function debugTriggerPlacement(placementId) {
    const state = readState();
    if (!state.map) return buildDebugSnapshot();
    const placement = (state.map.placements || []).find((entry) => entry.id === placementId);
    if (!placement || placement.done) return buildDebugSnapshot();
    if (placement.kind === "stairs") {
      const outcome = resolveStairsOutcome(placement, state.quest.bossesDefeated, state.flags);
      if (outcome.kind === "finalVictory") completeFinalEnding(placement);
      else if (outcome.kind === "targetMode" && outcome.targetMode === "town") activateTownState(state, outcome.target || {});
      else if (outcome.kind === "targetFloor") activateFloor(outcome.targetFloor);
      render();
      return buildDebugSnapshot();
    }
    if (placement.kind === "encounter" || placement.kind === "monster") return debugDefeatEncounterPlacement(placementId);
    return buildDebugSnapshot();
  }

  function debugTeleportToPlacementFront(placementId) {
    const state = readState();
    if ((state.mode !== "dungeon" && state.mode !== "town") || !state.map) return buildDebugSnapshot();
    const placement = (state.map.placements || []).find((entry) => entry.id === placementId);
    if (!placement?.position) return buildDebugSnapshot();
    for (const dir of dirs) {
      const x = placement.position.x - vec[dir].x;
      const y = placement.position.y - vec[dir].y;
      const cell = getCell(state.map, x, y);
      if (!cell?.walkable) continue;
      if (blocks(state.map, x, y, dir)) continue;
      return debugTeleportPlayer(x, y, dir);
    }
    const targetCell = getCell(state.map, placement.position.x, placement.position.y);
    if (targetCell?.walkable) return debugTeleportPlayer(targetCell.x, targetCell.y, state.player?.facing || "north");
    return buildDebugSnapshot();
  }

  return {
    contract: DEBUG_HARNESS_CONTRACT,
    snapshot: () => buildDebugSnapshot(),
    teleportPlayer: (x, y, facing) => debugTeleportPlayer(x, y, facing),
    teleportToPlacementFront: (placementId) => debugTeleportToPlacementFront(placementId),
    defeatEncounterPlacement: (placementId) => debugDefeatEncounterPlacement(placementId),
    triggerPlacement: (placementId) => debugTriggerPlacement(placementId),
  };
}

export function registerDebugHarness(targetWindow, debugHarness) {
  if (!targetWindow) return;
  targetWindow.__connanDebug = debugHarness;
  targetWindow.render_game_to_text = () => JSON.stringify(debugHarness.snapshot());
}

export function createRuntimeSessionManager(deps) {
  const {
    state,
    normalizeMapMetadata,
    computeWalls,
    normalizePartyModel,
    normalizeFieldMonsterStateTable = (value) => value || {},
    compileProjectForRuntime,
    releasePointerLook = () => {},
    logicalCellKey = (player) => `${Math.floor(Number(player?.x) + 0.5)},${Math.floor(Number(player?.y) + 0.5)}`,
  } = deps;

  function cloneVisitedByFloor(visitedByFloor) {
    return Object.fromEntries(Object.entries(visitedByFloor || {}).map(([floor, seen]) => [floor, new Set([...(seen || [])])]));
  }

  function cloneFloorMaps(floorMaps) {
    return Object.fromEntries(Object.entries(floorMaps || {}).map(([floor, map]) => {
      const next = JSON.parse(JSON.stringify(map));
      normalizeMapMetadata(next);
      computeWalls(next);
      return [floor, next];
    }));
  }

  function activateFloor(floor, target) {
    const nextMap = state.floorMaps[floor];
    if (!nextMap) return false;
    state.player.floor = floor;
    state.map = nextMap;
    state.player.x = target?.x ?? nextMap.start.x;
    state.player.y = target?.y ?? nextMap.start.y;
    state.player.facing = target?.facing || nextMap.start.facing;
    if (!state.visitedByFloor[floor]) state.visitedByFloor[floor] = new Set();
    state.visited = state.visitedByFloor[floor];
    state.visited.add(logicalCellKey(state.player));
    state.visitedByFloor[floor] = state.visited;
    return true;
  }

  function makeSessionSnapshot() {
    return {
      mode: state.mode,
      floorMaps: state.floorMaps,
      map: state.map,
      player: JSON.parse(JSON.stringify(state.player)),
      visitedByFloor: cloneVisitedByFloor(state.visitedByFloor),
      visited: new Set([...state.visited]),
      party: JSON.parse(JSON.stringify(state.party)),
      companion: state.companion ? JSON.parse(JSON.stringify(state.companion)) : null,
      npcState: JSON.parse(JSON.stringify(state.npcState || {})),
      resources: JSON.parse(JSON.stringify(state.resources)),
      inventory: JSON.parse(JSON.stringify(state.inventory)),
      flags: JSON.parse(JSON.stringify(state.flags)),
      quest: JSON.parse(JSON.stringify(state.quest)),
      fieldMonsters: JSON.parse(JSON.stringify(state.fieldMonsters || {})),
      combat: state.combat ? JSON.parse(JSON.stringify(state.combat)) : null,
      interaction: state.interaction ? JSON.parse(JSON.stringify(state.interaction)) : null,
      preEncounterSnapshot: state.preEncounterSnapshot ? JSON.parse(JSON.stringify(state.preEncounterSnapshot)) : null,
      log: [...state.log],
    };
  }

  function restoreSessionSnapshot(snapshot, nextMode = "editor") {
    state.floorMaps = snapshot.floorMaps;
    state.map = snapshot.map;
    state.player = snapshot.player;
    state.visitedByFloor = snapshot.visitedByFloor;
    state.visited = snapshot.visited;
    ({ party: state.party, companion: state.companion } = normalizePartyModel(snapshot.party, snapshot.companion));
    state.npcState = snapshot.npcState || {};
    state.resources = snapshot.resources;
    state.inventory = snapshot.inventory;
    state.flags = snapshot.flags;
    state.quest = snapshot.quest;
    state.fieldMonsters = normalizeFieldMonsterStateTable(snapshot.fieldMonsters);
    state.combat = snapshot.combat;
    state.interaction = snapshot.interaction;
    state.preEncounterSnapshot = snapshot.preEncounterSnapshot;
    state.log = snapshot.log;
    state.runtimeSession = {
      kind: "game",
      startedAt: new Date().toISOString(),
      returnSnapshot: null,
      sourceFloor: state.player.floor,
    };
    state.mode = nextMode;
  }

  function startTestPlaySession() {
    const compiledProject = compileProjectForRuntime(state.floorMaps);
    if (!compiledProject.ok) return compiledProject;
    const runtimeFloorMaps = buildRuntimeSessionFloorMaps(compiledProject.compiledMaps);
    const sessionSnapshot = makeSessionSnapshot();
    const startFloor = state.player.floor;
    const runtimeMap = runtimeFloorMaps[startFloor] || Object.values(runtimeFloorMaps)[0];
    state.runtimeSession = {
      kind: "test_play",
      source: "editor_compiled_project",
      startedAt: new Date().toISOString(),
      returnSnapshot: sessionSnapshot,
      sourceFloor: startFloor,
    };
    state.floorMaps = runtimeFloorMaps;
    state.map = runtimeMap;
    state.player = {
      floor: runtimeMap.start.floor || startFloor,
      x: runtimeMap.start.x,
      y: runtimeMap.start.y,
      facing: runtimeMap.start.facing,
    };
    state.visitedByFloor = Object.fromEntries(Object.keys(runtimeFloorMaps).map((floor) => [floor, new Set()]));
    state.visited = state.visitedByFloor[state.player.floor] || new Set();
    state.visited.add(logicalCellKey(state.player));
    state.visitedByFloor[state.player.floor] = state.visited;
    state.combat = null;
    state.interaction = null;
    state.fieldMonsters = {};
    state.preEncounterSnapshot = null;
    state.mode = "dungeon";
    return { ok: true, compiledProject };
  }

  function endTestPlaySession(nextMode = "editor") {
    if (state.runtimeSession?.kind !== "test_play" || !state.runtimeSession.returnSnapshot) return;
    const sessionInfo = state.runtimeSession;
    releasePointerLook();
    restoreSessionSnapshot(sessionInfo.returnSnapshot, nextMode);
    state.log.push(`테스트 플레이 임시 세션을 종료하고 ${nextMode} 화면으로 복귀했다.`);
  }

  function capturePreEncounterSnapshot() {
    state.preEncounterSnapshot = {
      capturedAt: new Date().toISOString(),
      player: JSON.parse(JSON.stringify(state.player)),
      party: JSON.parse(JSON.stringify(state.party)),
      companion: state.companion ? JSON.parse(JSON.stringify(state.companion)) : null,
      npcState: JSON.parse(JSON.stringify(state.npcState || {})),
      resources: JSON.parse(JSON.stringify(state.resources)),
      inventory: JSON.parse(JSON.stringify(state.inventory)),
      flags: JSON.parse(JSON.stringify(state.flags)),
      quest: JSON.parse(JSON.stringify(state.quest)),
      fieldMonsters: JSON.parse(JSON.stringify(state.fieldMonsters || {})),
    };
  }

  return {
    cloneVisitedByFloor,
    cloneFloorMaps,
    activateFloor,
    makeSessionSnapshot,
    restoreSessionSnapshot,
    buildRuntimeSessionFloorMaps,
    startTestPlaySession,
    endTestPlaySession,
    capturePreEncounterSnapshot,
  };

  function buildRuntimeSessionFloorMaps(compiledMaps) {
    return Object.fromEntries(Object.entries(compiledMaps).map(([floor, compiled]) => {
      const runtimeMap = JSON.parse(JSON.stringify(compiled.map));
      normalizeMapMetadata(runtimeMap);
      computeWalls(runtimeMap);
      return [floor, runtimeMap];
    }));
  }
}

import { buildPresetCatalog, getPresetById, instantiatePreset } from "./presets.js";
import { getCell } from "./runtimeCore.js";
import {
  MAP_CHUNK_VALIDATION_ISSUES,
  MAP_PROFILE_VALIDATION_ISSUES,
  evaluateChunkFitForNode,
  firstChunkAnchor,
  mapProfileForFloor,
  resolveChunkCatalog,
  selectChunkMatchForNode,
  selectChunkForNode,
} from "./mapGenerationData.js";
import { buildRoomGraph, summarizeRoomGraph, validateRoomGraph } from "./mapGraph.js";
import { objectThemeForTheme, tileSubstitutionsForTheme } from "./mapGenerationVisualData.js";

export const DIRS = ["north", "east", "south", "west"];
export const VEC = {
  north: { x: 0, y: -1 },
  east: { x: 1, y: 0 },
  south: { x: 0, y: 1 },
  west: { x: -1, y: 0 },
};
const GENERATED_MOVEMENT_BLOCKING_KINDS = new Set(["npc", "encounter", "monster"]);

export const MODULE_SHAPES = [
  (ox, oy, rng) => rectCells(ox, oy, 3 + Math.floor(rng() * 3), 3 + Math.floor(rng() * 2)),
  (ox, oy, rng) => rectCells(ox, oy, 2 + Math.floor(rng() * 2), 5 + Math.floor(rng() * 3)),
  (ox, oy, rng) => crossCells(ox, oy, 2 + Math.floor(rng() * 2), 2 + Math.floor(rng() * 2)),
  (ox, oy, rng) => lShapeCells(ox, oy, 3 + Math.floor(rng() * 3), 3 + Math.floor(rng() * 3), rng() > 0.5),
];

export function randomMapSeed() {
  return Math.floor(Math.random() * 0xffffffff) >>> 0;
}

export function createRuntimeFloorMaps(presetPool = buildPresetCatalog(), seedByFloor = {}, options = {}) {
  const floorMaps = {};
  for (const floor of [1, 2, 3]) {
    const seed = Number(seedByFloor[floor]) || randomMapSeed();
    floorMaps[floor] = makeMap(floor, seed, { ...options, presetPool });
  }
  return floorMaps;
}

export function createValidatedRuntimeFloorMaps(presetPool = buildPresetCatalog(), maxAttempts = 16, options = {}) {
  const { validateMap = () => ({ summary: { error: 0 } }), hasValidationErrors = (report) => Number(report?.summary?.error || 0) > 0 } = options;
  const floorMaps = {};
  for (const floor of [1, 2, 3]) {
    let selectedMap = null;
    for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
      const candidate = makeMap(floor, randomMapSeed(), { ...options, presetPool });
      selectedMap = candidate;
      if (!hasValidationErrors(validateMap(candidate))) break;
    }
    floorMaps[floor] = selectedMap;
  }
  return floorMaps;
}

export function createAuthoredRectangularMap(options = {}) {
  const {
    schemaVersion = 1,
    id = "authored_rectangular_map",
    name = "Authored Map",
    theme = "authored",
    floor = 1,
    width = 12,
    height = 12,
    start = { floor, x: 1, y: 1, facing: "north" },
    walkableKeys = new Set(),
    rooms = [],
    doors = {},
    placements = [],
    lights = [],
    decor = [],
    generation = {},
    tags = [],
    defaultBlockedTags = ["blocked"],
    defaultWalkableTags = ["walkable"],
    defaultFloorTextureId = "floor_stone_01",
    defaultCeilingTextureId = "ceiling_stone_01",
    defaultWallTextureId = "wall_stone_01",
    defaultBattleBackgroundId = null,
    tileSubstitutions = tileSubstitutionsForTheme(theme),
    computeWalls: computeWallsFn = computeWalls,
  } = options;
  const normalizedWalkableKeys = walkableKeys instanceof Set
    ? walkableKeys
    : new Set(Array.isArray(walkableKeys) ? walkableKeys : []);
  const cells = [];
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const walkable = normalizedWalkableKeys.has(`${x},${y}`);
      cells.push({
        x,
        y,
        walkable,
        roomId: null,
        tags: walkable ? [...defaultWalkableTags] : [...defaultBlockedTags],
        floorTexture: defaultFloorTextureId,
        ceilingTexture: defaultCeilingTextureId,
        wallTexture: defaultWallTextureId,
        floorMaterialId: defaultFloorTextureId,
        ceilingMaterialId: defaultCeilingTextureId,
        wallMaterialId: defaultWallTextureId,
        battleBackgroundId: defaultBattleBackgroundId,
        walls: {},
      });
    }
  }
  const map = {
    schemaVersion,
    id,
    name,
    theme,
    size: { width, height, floors: 1 },
    start: { ...start, floor },
    cells,
    rooms: JSON.parse(JSON.stringify(rooms || [])),
    doors: JSON.parse(JSON.stringify(doors || {})),
    placements: JSON.parse(JSON.stringify(placements || [])),
    lights: JSON.parse(JSON.stringify(lights || [])),
    decor: JSON.parse(JSON.stringify(decor || [])),
    generation: JSON.parse(JSON.stringify(generation || {})),
    tags: [...tags],
  };
  resolveTileAdjacency(map, { substitutions: tileSubstitutions });
  computeWallsFn(map, options);
  return map;
}

export function progressionPlacementIdForFloor(floor) {
  if (floor === 3) return "final_stairs_03";
  return `stairs_down_0${floor}`;
}

export function roomBoundsFromCells(cells = []) {
  if (!cells.length) return null;
  const bounds = boundsOf(cells);
  return {
    x: bounds.minX,
    y: bounds.minY,
    width: bounds.maxX - bounds.minX + 1,
    height: bounds.maxY - bounds.minY + 1,
  };
}

export function authoredModuleProfilesForFloor(floor) {
  if (floor === 1) {
    return {
      start: { roomType: "lore_room", cellTags: ["npc_anchor"] },
      key: { roomType: "treasure_room", cellTags: ["loot_anchor"] },
      side: { roomType: "shrine_room", cellTags: ["loot_anchor"] },
      guard: { roomType: "combat_room", cellTags: ["trap_zone"] },
      boss: { roomType: "puzzle_room", cellTags: ["boss_anchor"] },
    };
  }
  if (floor === 2) {
    return {
      start: { roomType: "combat_room", cellTags: [] },
      key: { roomType: "treasure_room", cellTags: ["loot_anchor"] },
      side: { roomType: "camp_room", cellTags: ["safe", "camp_allowed", "save_allowed", "npc_anchor"] },
      guard: { roomType: "combat_room", cellTags: ["trap_zone"] },
      boss: { roomType: "boss_room", cellTags: ["boss_anchor"] },
    };
  }
  return {
    start: { roomType: "combat_room", cellTags: [] },
    key: { roomType: "shrine_room", cellTags: ["safe", "camp_allowed", "save_allowed", "loot_anchor"] },
    side: { roomType: "combat_room", cellTags: [] },
    guard: { roomType: "npc_room", cellTags: ["npc_anchor"] },
    boss: { roomType: "boss_room", cellTags: ["boss_anchor"] },
  };
}

export function applyAuthoredModuleMetadata(map, generated, floor, options = {}) {
  const { sortedUniqueStrings = (values) => [...new Set((values || []).filter((value) => typeof value === "string" && value.trim()))].sort() } = options;
  const profiles = authoredModuleProfilesForFloor(floor);
  const rooms = [];
  Object.entries(generated.roles || {}).forEach(([role, moduleIndex]) => {
    const module = generated.modules?.[moduleIndex];
    const profile = profiles[role];
    if (!module || !profile) return;
    const roomId = `room_f${floor}_${role}`;
    const roomBounds = roomBoundsFromCells(module.cells);
    if (!roomBounds) return;
    module.cells.forEach((point) => {
      const cell = getCell(map, point.x, point.y);
      if (!cell) return;
      cell.roomId = roomId;
      cell.tags = sortedUniqueStrings([...(cell.tags || []), ...(profile.cellTags || [])]);
    });
    rooms.push({
      id: roomId,
      roomType: profile.roomType,
      bounds: roomBounds,
      tags: [...(profile.cellTags || [])],
    });
  });
  map.rooms = rooms;
}

function mergeGenerationProfile(baseProfile = {}, override = null) {
  if (!override || typeof override !== "object") return baseProfile;
  return {
    ...baseProfile,
    ...override,
    gridRoomSize: {
      ...(baseProfile.gridRoomSize || {}),
      ...(override.gridRoomSize || {}),
    },
    layout: {
      ...(baseProfile.layout || {}),
      ...(override.layout || {}),
    },
    criticalPath: {
      ...(baseProfile.criticalPath || {}),
      ...(override.criticalPath || {}),
    },
    loopCount: {
      ...(baseProfile.loopCount || {}),
      ...(override.loopCount || {}),
    },
    requiredAnchors: Array.isArray(override.requiredAnchors)
      ? [...override.requiredAnchors]
      : [...(baseProfile.requiredAnchors || [])],
  };
}

function summarizeVariantUsage(generated = {}) {
  const chunkCatalog = Array.isArray(generated.chunkCatalog) ? generated.chunkCatalog : [];
  const chunkById = new Map(chunkCatalog.map((chunk) => [String(chunk.id || ""), chunk]));
  const variantGroupCounts = new Map();
  const chunkIdCounts = new Map();
  for (const module of Array.isArray(generated.modules) ? generated.modules : []) {
    const chunkId = String(module.chunkId || "");
    if (!chunkId) continue;
    chunkIdCounts.set(chunkId, Number(chunkIdCounts.get(chunkId) || 0) + 1);
    const variantGroup = String(chunkById.get(chunkId)?.variantGroup || "");
    if (!variantGroup) continue;
    variantGroupCounts.set(variantGroup, Number(variantGroupCounts.get(variantGroup) || 0) + 1);
  }
  return {
    chunkIdCounts: [...chunkIdCounts.entries()]
      .map(([chunkId, count]) => ({ chunkId, count }))
      .sort((left, right) => right.count - left.count || left.chunkId.localeCompare(right.chunkId)),
    variantGroupCounts: [...variantGroupCounts.entries()]
      .map(([variantGroup, count]) => ({ variantGroup, count }))
      .sort((left, right) => right.count - left.count || left.variantGroup.localeCompare(right.variantGroup)),
  };
}

function buildVariantUsageIssues(generated = {}) {
  const moduleCount = Math.max(0, Number(generated.modules?.length || 0));
  const summary = summarizeVariantUsage(generated);
  const issues = [];
  for (const entry of summary.variantGroupCounts) {
    const ratio = moduleCount > 0 ? entry.count / moduleCount : 0;
    if (entry.count >= 4 && ratio >= 0.45) {
      issues.push({
        severity: "warning",
        code: "variant_group_repetition_high",
        variantGroup: entry.variantGroup,
        count: entry.count,
        moduleCount,
        ratio,
      });
    }
  }
  for (const entry of summary.chunkIdCounts) {
    const ratio = moduleCount > 0 ? entry.count / moduleCount : 0;
    if (entry.count >= 3 && ratio >= 0.35) {
      issues.push({
        severity: "warning",
        code: "chunk_repetition_high",
        chunkId: entry.chunkId,
        count: entry.count,
        moduleCount,
        ratio,
      });
    }
  }
  return {
    issues,
    summary,
  };
}

export function makeMap(floor = 1, seed = 18422, options = {}) {
  const {
    presetPool,
    profileOverride = null,
    chunkCatalogOverride = null,
    generationAlgorithm = "",
    defaultFloorTextureId = "floor_stone_01",
    defaultCeilingTextureId = "ceiling_stone_01",
    defaultWallTextureId = "wall_stone_01",
    computeWalls: computeWallsFn = computeWalls,
    monsters = {},
  } = options;
  const floorInfo = mergeGenerationProfile(mapProfileForFloor(floor), profileOverride);
  const resolvedChunkCatalog = resolveChunkCatalog(floorInfo.theme, chunkCatalogOverride);
  const resolvedPresetPool = presetPool || buildPresetPoolForFloor(floor, buildPresetCatalog(), resolvedChunkCatalog, floorInfo);
  const roomGraph = buildRoomGraph(floorInfo, seed);
  const roomGraphIssues = validateRoomGraph(floorInfo, roomGraph);
  const socketAssemblyIssues = [];
  roomGraph.nodes = roomGraph.nodes.map((node) => {
    const chunkMatch = selectChunkMatchForNode(node, floorInfo.theme, resolvedChunkCatalog);
    socketAssemblyIssues.push(...chunkMatch.issues);
    const chunk = chunkMatch.chunk;
    return {
      ...node,
      chunkId: chunk?.id || "",
      presetId: chunk?.presetId || "",
      anchorIds: Array.isArray(chunk?.anchors) ? chunk.anchors.map((anchor) => anchor.id) : [],
      chunkMatch: {
        exactSocketMatch: Boolean(chunkMatch.exactSocketMatch),
        roleCompatible: Boolean(chunkMatch.roleCompatible),
        rotation: Number(chunkMatch.rotation || 0),
        rotatedOpenSides: [...(chunkMatch.rotatedOpenSides || [])],
      },
    };
  });
  const resolvedAlgorithm = generationAlgorithm || floorInfo.algorithm || "block_modules_and_connectors";
  const generated = resolvedAlgorithm === "room_grid_chunks"
    ? generateRoomGridLayout(floorInfo, seed, roomGraph, resolvedPresetPool, resolvedChunkCatalog)
    : generateBlockLayout(floor, seed, resolvedPresetPool, floorInfo);
  generated.chunkCatalog = resolvedChunkCatalog;
  const variantUsage = buildVariantUsageIssues(generated);
  if (Array.isArray(generated.assemblyIssues) && generated.assemblyIssues.length) {
    socketAssemblyIssues.push(...generated.assemblyIssues);
  }
  const cells = [];
  for (let y = 0; y < generated.height; y++) {
    for (let x = 0; x < generated.width; x++) {
      const walkable = generated.open.has(`${x},${y}`);
      cells.push({
        x,
        y,
        walkable,
        roomId: null,
        tags: walkable ? ["buried_temple"] : ["stone"],
        floorTexture: defaultFloorTextureId,
        ceilingTexture: defaultCeilingTextureId,
        wallTexture: defaultWallTextureId,
        battleBackgroundId: null,
        walls: {},
      });
    }
  }
  const map = {
    schemaVersion: 1,
    id: floorInfo.mapId,
    name: floorInfo.name,
    theme: floorInfo.theme,
    size: { width: generated.width, height: generated.height, floors: 1 },
    start: { floor, x: generated.start.x, y: generated.start.y, facing: generated.start.facing },
    cells,
    rooms: [],
    doors: generated.doors,
    placements: makePlacements(floor, generated, floorInfo, { monsters, seed }),
    lights: makeAuthoredLights(floor, generated),
    decor: [],
    generation: {
      seed,
      profileId: floorInfo.profileId,
      profileSnapshot: JSON.parse(JSON.stringify({
        floor: floorInfo.floor,
        profileId: floorInfo.profileId,
        mapKind: floorInfo.mapKind,
        mapKindName: floorInfo.mapKindName,
        theme: floorInfo.theme,
        algorithm: floorInfo.algorithm,
        targetModuleCount: floorInfo.targetModuleCount,
        mergeChancePer1000: floorInfo.mergeChancePer1000,
        gridRoomSize: floorInfo.gridRoomSize,
        criticalPath: floorInfo.criticalPath,
        loopCount: floorInfo.loopCount,
        sideBranchCount: floorInfo.sideBranchCount,
        requiredAnchors: floorInfo.requiredAnchors,
      })),
      algorithm: resolvedAlgorithm,
      moduleCount: generated.modules.length,
      lockedPoints: ["start", progressionPlacementIdForFloor(floor)],
      profileValidationIssueCount: MAP_PROFILE_VALIDATION_ISSUES.length,
      chunkValidationIssueCount: MAP_CHUNK_VALIDATION_ISSUES.length,
      roomGraph,
      roomGraphSummary: summarizeRoomGraph(roomGraph, roomGraphIssues),
      roomGraphIssues,
      socketAssemblyIssues,
      socketAssemblySummary: {
        issueCount: socketAssemblyIssues.length,
        errorCount: socketAssemblyIssues.filter((issue) => issue.severity === "error").length,
        warningCount: socketAssemblyIssues.filter((issue) => issue.severity === "warning").length,
      },
      qualityIssues: variantUsage.issues,
      qualitySummary: {
        issueCount: variantUsage.issues.length,
        warningCount: variantUsage.issues.filter((issue) => issue.severity === "warning").length,
        variantGroupCounts: variantUsage.summary.variantGroupCounts,
        chunkIdCounts: variantUsage.summary.chunkIdCounts,
      },
    },
    tags: ["mvp", "intro", `floor_${floor}`],
  };
  applyAuthoredModuleMetadata(map, generated, floor, options);
  resolveTileAdjacency(map, { substitutions: tileSubstitutionsForTheme(floorInfo.theme) });
  applyDecorationPass(map, {
    seed,
    objectTheme: objectThemeForTheme(floorInfo.theme),
  });
  computeWallsFn(map, options);
  return map;
}

export function buildPresetPoolForFloor(floor, presetCatalog = buildPresetCatalog(), chunkCatalogOverride = null) {
  const profile = arguments.length > 3 ? arguments[3] : mapProfileForFloor(floor);
  const chunkCatalog = resolveChunkCatalog(profile.theme, chunkCatalogOverride);
  const allowedPresetIds = new Set(chunkCatalog.map((chunk) => chunk.presetId));
  const filtered = presetCatalog.filter((preset) => allowedPresetIds.has(preset.id));
  return filtered.length ? filtered : presetCatalog;
}

export function makeAuthoredLights(floor, generated) {
  const lights = [];
  const addLight = (id, module, extra = {}) => {
    if (!module) return;
    lights.push({
      id,
      type: "point",
      x: module.center.x,
      y: module.center.y,
      height: extra.height ?? 1.8,
      color: extra.color ?? "#f0b46d",
      intensity: extra.intensity ?? 0.72,
      range: extra.range ?? 8,
      ...extra,
    });
  };
  if (floor === 1) {
    addLight("light_intro_start", generated.modules[generated.roles.start], { color: "#7fdcd0", intensity: 0.5, range: 6.5 });
    addLight("light_intro_boss_gate", generated.modules[generated.roles.boss], { color: "#d29d5a", intensity: 0.66, range: 8.5 });
  } else if (floor === 2) {
    addLight("light_camp_core", generated.modules[generated.roles.side], { color: "#f0a35c", intensity: 0.82, range: 9.5 });
    addLight("light_black_water", generated.modules[generated.roles.boss], { color: "#5fa7c8", intensity: 0.6, range: 8 });
  } else {
    addLight("light_priest_corridor", generated.modules[generated.roles.start], { color: "#8f7fdc", intensity: 0.58, range: 7.2 });
    addLight("light_priest_boss", generated.modules[generated.roles.boss], { color: "#d2a44b", intensity: 0.7, range: 9 });
  }
  return lights;
}

function monsterSpawnRoles(monster = {}) {
  const roles = monster.spawn?.roles;
  return Array.isArray(roles) && roles.length ? roles.map((role) => String(role)) : ["start", "key", "guard"];
}

function monsterCanSpawn(monster = {}, floorInfo = {}, role = "start") {
  const spawn = monster.spawn || {};
  const floor = Number(floorInfo.floor || 1);
  const minFloor = Math.max(1, Number(spawn.minFloor || 1));
  const maxFloor = spawn.maxFloor == null || spawn.maxFloor === "" ? Infinity : Number(spawn.maxFloor);
  if (floor < minFloor || floor > maxFloor) return false;
  const mapKinds = Array.isArray(spawn.mapKinds) ? spawn.mapKinds.map((value) => String(value)) : [];
  const themes = Array.isArray(spawn.themes) ? spawn.themes.map((value) => String(value)) : [];
  if (mapKinds.length && !mapKinds.includes(String(floorInfo.mapKind || ""))) return false;
  if (themes.length && !themes.includes(String(floorInfo.theme || ""))) return false;
  return monsterSpawnRoles(monster).includes(role);
}

function scaledMonsterEnemySpec(monsterId = "", monster = {}, floor = 1, rng = Math.random) {
  const floorIndex = Math.max(0, Number(floor || 1) - 1);
  const hpGrowth = Math.max(0, Number(monster.scaling?.hpPerFloor || 0));
  const atkGrowth = Math.max(0, Number(monster.scaling?.atkPerFloor || 0));
  const baseHp = Math.max(1, Number(monster.hp || 1));
  const minAtk = Number.isFinite(Number(monster.atkMin)) ? Number(monster.atkMin) : Number(monster.atk || 1);
  const maxAtk = Number.isFinite(Number(monster.atkMax)) ? Number(monster.atkMax) : Number(monster.atk || minAtk || 1);
  const baseAtk = minAtk === maxAtk
    ? minAtk
    : minAtk + Math.floor(rng() * (Math.max(minAtk, maxAtk) - minAtk + 1));
  return {
    monsterId,
    hp: Math.max(1, Math.round(baseHp * (1 + hpGrowth * floorIndex))),
    atk: Math.max(1, Math.round(baseAtk * (1 + atkGrowth * floorIndex))),
    def: Math.max(0, Number(monster.def || 0)),
    xp: Math.max(0, Math.round(Number(monster.xp || 0) * (1 + hpGrowth * floorIndex * 0.5))),
  };
}

function fallbackMonsterIdForRole(floor = 1, role = "start") {
  if (role === "boss") {
    if (floor >= 3) return "blind_priest";
    if (floor >= 2) return "black_water_beast";
    return "serpent_guard";
  }
  if (role === "guard") {
    if (floor >= 3) return "serpent_priest";
    if (floor >= 2) return "cursed_gladiator";
    return "serpent_guard";
  }
  if (floor >= 3) return "serpent_priest";
  if (floor >= 2) return "poisoned_raider";
  return role === "key" ? "grave_robber" : "desert_rat";
}

function selectMonsterForPlacement(role = "start", floorInfo = {}, options = {}) {
  const monsters = options.monsters || {};
  const rng = options.rng || Math.random;
  const candidates = Object.entries(monsters)
    .filter(([, monster]) => monsterCanSpawn(monster, floorInfo, role))
    .map(([monsterId, monster]) => ({
      monsterId,
      monster,
      weight: Math.max(1, Number(monster.spawn?.weight || 1)),
    }));
  const fallbackId = fallbackMonsterIdForRole(Number(floorInfo.floor || 1), role);
  if (!candidates.length) {
    return { monsterId: fallbackId, monster: monsters[fallbackId] || {}, tableFallback: true };
  }
  const totalWeight = candidates.reduce((sum, entry) => sum + entry.weight, 0);
  let roll = rng() * totalWeight;
  for (const candidate of candidates) {
    roll -= candidate.weight;
    if (roll <= 0) return candidate;
  }
  return candidates[candidates.length - 1];
}

function proceduralEncounterExtra(role = "start", floorInfo = {}, options = {}) {
  const rng = options.rng || Math.random;
  const selected = selectMonsterForPlacement(role, floorInfo, options);
  const monsterId = selected.monsterId;
  const enemySpec = scaledMonsterEnemySpec(monsterId, selected.monster, floorInfo.floor, rng);
  enemySpec.boss = role === "boss" || Boolean(selected.monster?.boss);
  const encounterId = `encounter_${monsterId}`;
  return {
    refType: "encounter",
    refId: encounterId,
    generatedEnemies: [enemySpec],
    spawnSource: {
      mode: "monster_definition_table",
      role,
      mapKind: floorInfo.mapKind || "",
      mapKindName: floorInfo.mapKindName || "",
      theme: floorInfo.theme || "",
      floor: Number(floorInfo.floor || 1),
      monsterId,
      tableFallback: Boolean(selected.tableFallback),
    },
  };
}

export function makePlacements(floor, generated, floorInfo = null, options = {}) {
  const resolvedFloorInfo = floorInfo || mapProfileForFloor(floor);
  const rng = mulberry32(Number(options.seed || generated?.seed || floor * 7919) + floor * 313);
  const startModule = generated.modules[generated.roles.start];
  const keyModule = generated.modules[generated.roles.key];
  const bossModule = generated.modules[generated.roles.boss];
  const sideModule = generated.modules[generated.roles.side];
  const guardModule = generated.modules[generated.roles.guard];
  const placements = [];
  const reservedKeys = [];
  if (Number.isFinite(Number(generated?.start?.x)) && Number.isFinite(Number(generated?.start?.y))) {
    reservedKeys.push(`${Number(generated.start.x)},${Number(generated.start.y)}`);
  }
  const placementAt = (id, kind, module, extra = {}, anchorKinds = []) => {
    if (!module) return;
    const occupiedKeys = [
      ...reservedKeys,
      ...placements.map((p) => `${p.position.x},${p.position.y}`),
    ];
    const blockingOccupiedKeys = placements
      .filter((entry) => GENERATED_MOVEMENT_BLOCKING_KINDS.has(entry.kind))
      .map((entry) => `${entry.position.x},${entry.position.y}`);
    const requiredReachableKeys = [
      ...reservedKeys,
      ...placements
        .filter((entry) => entry.kind === "stairs")
        .map((entry) => `${entry.position.x},${entry.position.y}`),
    ];
    const point = placementPointFromModule(module, generated, kind, anchorKinds, {
      occupied: occupiedKeys,
      blockingOccupied: blockingOccupiedKeys,
      requiredReachable: requiredReachableKeys,
    });
    placements.push({ id, kind, position: { floor, x: point.x, y: point.y }, ...extra });
  };
  if (floor === 1) {
    placementAt("entry_marker_01", "entry_marker", startModule, {
      note: "사원 입구에서 내려온 시작 지점이다. 복귀 출구는 별도 통로에 있다.",
    }, ["generic", "junction"]);
    placementAt("stairs_down_01", "stairs", sideModule, {
      targetFloor: 2,
      requiredFlag: "quest_seed_black_mural_rewarded",
      blockedMessage: "학자의 기록이 아직 완성되지 않아 아래로 이어지는 봉인이 닫혀 있다.",
    }, ["generic", "junction"]);
    placementAt("stairs_exit_town_01", "stairs", keyModule, {
      targetMode: "town",
      note: "사원 입구로 되돌아가 바깥 빛과 함께 정착지로 복귀했다.",
    }, ["loot", "generic"]);
    placementAt("npc_scholar_01", "npc", startModule, {
      refType: "npc",
      refId: "npc_scholar",
      npcId: "npc_scholar",
      note: "검은 물 자국이 번진 벽 앞에서 학자가 탁본과 장부를 펼쳐 든다.",
    }, ["generic", "junction"]);
    placementAt("npc_exile_01", "npc", sideModule, {
      refType: "npc",
      refId: "npc_exile_scout",
      npcId: "npc_exile_scout",
      note: "무너진 벽 틈에서 정찰병이 바깥 통로를 살핀다.",
    }, ["generic", "loot"]);
    placementAt("rat_01", "encounter", startModule, proceduralEncounterExtra("start", resolvedFloorInfo, { ...options, rng }), ["junction", "generic"]);
    placementAt("robber_01", "encounter", keyModule, proceduralEncounterExtra("key", resolvedFloorInfo, { ...options, rng }), ["loot", "generic"]);
    placementAt("guard_01", "encounter", guardModule, proceduralEncounterExtra("guard", resolvedFloorInfo, { ...options, rng }), ["corridor", "generic"]);
    placementAt("boss_01", "encounter", bossModule, proceduralEncounterExtra("boss", resolvedFloorInfo, { ...options, rng }), ["boss_spawn", "generic"]);
    placementAt("key_01", "item", keyModule, { itemId: "bronze_key" }, ["loot", "generic"]);
    placementAt("dagger_01", "item", sideModule, { itemId: "black_dagger" }, ["loot", "generic"]);
    placementAt("trap_01", "trap", guardModule, {
      refType: "event",
      refId: "event_trap_poison_dart",
      interaction: { type: "onEnter", eventId: "event_trap_poison_dart" },
    }, ["corridor", "generic"]);
    placementAt("altar_01", "shrine", sideModule, {
      refType: "event",
      refId: "event_blood_altar_unlock",
      interaction: { type: "interact", eventId: "event_blood_altar_unlock" },
    }, ["boss_spawn", "generic"]);
    return placements;
  }
  if (floor === 2) {
    placementAt("entry_marker_02", "entry_marker", startModule, {
      note: "윗층에서 내려온 시작 지점이다. 복귀 출구는 별도 통로에 있다.",
    }, ["generic", "junction"]);
    placementAt("stairs_down_02", "stairs", bossModule, { targetFloor: 3, requiredBoss: "black_water_beast" }, ["boss_spawn", "generic"]);
    placementAt("stairs_exit_town_02", "stairs", keyModule, {
      targetMode: "town",
      note: "무너진 통로 옆 사다리를 타고 지상 정착지로 철수했다.",
    }, ["loot", "generic"]);
    placementAt("npc_mystic_02", "npc", sideModule, {
      refType: "npc",
      refId: "npc_wounded_mystic",
      npcId: "npc_wounded_mystic",
      note: "검은 물 흔적 옆에서 신비가가 상처를 감싼다.",
    }, ["generic", "junction"]);
    placementAt("raider_01", "encounter", startModule, proceduralEncounterExtra("start", resolvedFloorInfo, { ...options, rng }), ["generic", "junction"]);
    placementAt("gladiator_01", "encounter", guardModule, proceduralEncounterExtra("guard", resolvedFloorInfo, { ...options, rng }), ["corridor", "generic"]);
    placementAt("boss_02", "encounter", bossModule, proceduralEncounterExtra("boss", resolvedFloorInfo, { ...options, rng }), ["boss_spawn", "generic"]);
    placementAt("obsidian_key_01", "item", keyModule, { itemId: "obsidian_key" }, ["loot", "generic"]);
    placementAt("trap_02", "trap", guardModule, {
      refType: "event",
      refId: "event_trap_bleed_blade",
      interaction: { type: "onEnter", eventId: "event_trap_bleed_blade" },
    }, ["corridor", "generic"]);
    placementAt("camp_02", "camp", sideModule, {
      refType: "event",
      refId: "event_camp_guard_post",
      interaction: { type: "onCamp", eventId: "event_camp_guard_post" },
    }, ["generic", "junction"]);
    return placements;
  }
  placementAt("entry_marker_03", "entry_marker", startModule, {
    note: "윗층에서 내려온 시작 지점이다. 복귀 출구는 별도 통로에 있다.",
  }, ["generic", "junction"]);
  placementAt("final_stairs_03", "stairs", bossModule, {
    final: true,
    requiredBoss: "blind_priest",
    requiredFlag: "quest_seed_black_water_vow_rewarded",
    blockedMessage: "검은 물의 의식이 아직 끝나지 않아 마지막 봉인이 미세하게 꿈틀거리기만 한다.",
  }, ["boss_spawn", "generic"]);
  placementAt("stairs_exit_town_03", "stairs", keyModule, {
    targetMode: "town",
    note: "낡은 승강통로를 따라 지상 정착지로 복귀했다.",
  }, ["loot", "generic"]);
  placementAt("npc_captain_03", "npc", guardModule, {
    refType: "npc",
    refId: "npc_deserter_captain",
    npcId: "npc_deserter_captain",
    note: "경비대장이 청동 사슬을 감은 채 통로를 지킨다.",
  }, ["generic", "corridor"]);
  placementAt("priest_01", "encounter", startModule, proceduralEncounterExtra("start", resolvedFloorInfo, { ...options, rng }), ["generic", "junction"]);
  placementAt("guard_03", "encounter", sideModule, proceduralEncounterExtra("guard", resolvedFloorInfo, { ...options, rng }), ["corridor", "generic"]);
  placementAt("boss_03", "encounter", bossModule, proceduralEncounterExtra("boss", resolvedFloorInfo, { ...options, rng }), ["boss_spawn", "generic"]);
  placementAt("mask_01", "item", keyModule, { itemId: "priest_mask" }, ["loot", "generic"]);
  placementAt("trap_03", "trap", sideModule, {
    refType: "event",
    refId: "event_trap_curse_rune",
    interaction: { type: "onEnter", eventId: "event_trap_curse_rune" },
  }, ["corridor", "generic"]);
  placementAt("altar_03", "shrine", keyModule, {
    refType: "event",
    refId: "event_black_water_rite",
    interaction: { type: "interact", eventId: "event_black_water_rite" },
  }, ["loot", "generic"]);
  placementAt("rest_03", "rest_site", keyModule, {
    refType: "event",
    refId: "event_rest_guard_post",
    interaction: { type: "onRest", eventId: "event_rest_guard_post" },
  }, ["loot", "generic"]);
  return placements;
}

export function generateBlockLayout(floor, seed, presetPool = buildPresetCatalog(), profileOverride = null) {
  const profile = mergeGenerationProfile(mapProfileForFloor(floor), profileOverride);
  const rng = mulberry32(seed + floor * 9973);
  const width = Number(profile.layout?.width || 19);
  const height = Number(profile.layout?.height || 19);
  const open = new Set();
  const doors = {};
  const modules = [];
  const presets = presetPool?.length ? presetPool : buildPresetCatalog();
  const targetModules = Math.max(5, Number(profile.targetModuleCount || (7 + floor)));
  let attempts = 0;
  while (modules.length < targetModules && attempts < 220) {
    attempts += 1;
    const preset = presets[Math.floor(rng() * presets.length)];
    const rotation = Math.floor(rng() * 4);
    const originX = 1 + Math.floor(rng() * (width - Math.max(4, preset.width + 1)));
    const originY = 1 + Math.floor(rng() * (height - Math.max(4, preset.height + 1)));
    const cells = instantiatePreset(preset, { rotation, originX, originY }).cells
      .filter((p) => p.x > 0 && p.x < width - 1 && p.y > 0 && p.y < height - 1)
      .filter((p, index, arr) => arr.findIndex((q) => q.x === p.x && q.y === p.y) === index);
    if (cells.length < 4 || overlapsModule(cells, modules)) continue;
    const bounds = boundsOf(cells);
    const center = {
      x: Math.round(cells.reduce((sum, p) => sum + p.x, 0) / cells.length),
      y: Math.round(cells.reduce((sum, p) => sum + p.y, 0) / cells.length),
    };
    modules.push({
      id: `module_${modules.length}`,
      presetId: preset.id,
      rotation,
      originX,
      originY,
      baseWidth: preset.width,
      baseHeight: preset.height,
      cells,
      bounds,
      center,
    });
    cells.forEach((p) => open.add(`${p.x},${p.y}`));
  }
  if (modules.length < 5) return generateFallbackLayout(floor, seed, profile);
  modules.sort((a, b) => a.center.x - b.center.x);
  const startIndex = modules.reduce((best, module, index) => {
    const score = module.center.x + module.center.y * 0.35;
    return score < best.score ? { index, score } : best;
  }, { index: 0, score: Infinity }).index;
  const bossIndex = furthestModuleIndex(modules, modules[startIndex].center);
  const connections = buildModuleConnections(modules, startIndex, rng);
  connections.forEach(({ a, b }) => carveConnector(open, modules[a], modules[b], rng));
  const sideCandidates = modules.map((_, i) => i).filter((i) => i !== startIndex && i !== bossIndex);
  const keyIndex = furthestModuleIndex(sideCandidates.map((i) => modules[i]), modules[startIndex].center, sideCandidates);
  const sideIndex = furthestModuleIndex(sideCandidates.filter((i) => i !== keyIndex).map((i) => modules[i]), modules[bossIndex].center, sideCandidates.filter((i) => i !== keyIndex));
  const guardIndex = nearestDifferentModule(modules, modules[keyIndex].center, new Set([startIndex, keyIndex])) || sideIndex || bossIndex;
  const secretWall = carveSecretSpur(open, modules[sideIndex] || modules[keyIndex], rng, width, height);
  const lockedWall = placeLockedDoor(open, modules[bossIndex], modules[keyIndex], doors, floor, modules[startIndex].center, modules[keyIndex].center);
  const commonDoor = placeCommonDoor(open, modules[startIndex], modules[guardIndex] || modules[bossIndex], doors);
  if (secretWall) doors[secretWall] = { type: "secret", open: false, locked: false };
  const lockedDoorKeyId = profile.lockedDoorKeyId || (floor === 1 ? "bronze_key" : floor === 2 ? "obsidian_key" : "priest_mask");
  if (lockedWall) doors[lockedWall] = { type: "door", open: false, locked: true, keyId: lockedDoorKeyId };
  if (commonDoor && !doors[commonDoor]) doors[commonDoor] = { type: "door", open: false, locked: false };
  return {
    width,
    height,
    theme: profile.theme,
    open,
    doors,
    modules,
    rng,
    roles: { start: startIndex, boss: bossIndex, key: keyIndex, side: sideIndex, guard: guardIndex },
    start: { ...randomCellFromModule(modules[startIndex], rng), facing: "north" },
  };
}

export function generateRoomGridLayout(profile, seed, roomGraph, presetPool = buildPresetCatalog(), chunkCatalogOverride = null) {
  const rng = mulberry32((Number(seed) || 0) ^ 0x51f15e);
  const roomWidth = Math.max(3, Number(profile.gridRoomSize?.width || 7));
  const roomHeight = Math.max(3, Number(profile.gridRoomSize?.height || 7));
  const outerMargin = 1;
  const nodes = Array.isArray(roomGraph?.nodes) ? roomGraph.nodes : [];
  const maxX = Math.max(0, ...nodes.map((node) => Number(node.x || 0)));
  const maxY = Math.max(0, ...nodes.map((node) => Number(node.y || 0)));
  const width = Math.max(roomWidth + outerMargin * 2, (maxX + 1) * roomWidth + outerMargin * 2);
  const height = Math.max(roomHeight + outerMargin * 2, (maxY + 1) * roomHeight + outerMargin * 2);
  const open = new Set();
  const doors = {};
  const modules = [];
  const catalog = presetPool?.length ? presetPool : buildPresetCatalog();
  const moduleIndexByNodeId = new Map();
  const assemblyIssues = [];

  const placeSocketPath = (originX, originY, side) => {
    const midX = originX + Math.floor(roomWidth / 2);
    const midY = originY + Math.floor(roomHeight / 2);
    if (side === "north") {
      for (let y = originY; y <= midY; y += 1) open.add(`${midX},${y}`);
    } else if (side === "south") {
      for (let y = midY; y < originY + roomHeight; y += 1) open.add(`${midX},${y}`);
    } else if (side === "west") {
      for (let x = originX; x <= midX; x += 1) open.add(`${x},${midY}`);
    } else if (side === "east") {
      for (let x = midX; x < originX + roomWidth; x += 1) open.add(`${x},${midY}`);
    }
  };

  const connectPointToRoomCenter = (point, originX, originY) => {
    const targetX = originX + Math.floor(roomWidth / 2);
    const targetY = originY + Math.floor(roomHeight / 2);
    let x = Number(point?.x || targetX);
    let y = Number(point?.y || targetY);
    while (x !== targetX) {
      open.add(`${x},${y}`);
      x += x < targetX ? 1 : -1;
    }
    while (y !== targetY) {
      open.add(`${x},${y}`);
      y += y < targetY ? 1 : -1;
    }
    open.add(`${targetX},${targetY}`);
  };

  const chunkCatalog = resolveChunkCatalog(profile.theme, chunkCatalogOverride);

  for (const node of nodes) {
    const chunkMatch = selectChunkMatchForNode(node, profile.theme, chunkCatalog);
    assemblyIssues.push(...chunkMatch.issues);
    const chunk = chunkMatch.chunk;
    const preset = getPresetById(chunk?.presetId || "", catalog) || catalog[0];
    if (!preset) continue;
    const roomOriginX = outerMargin + Number(node.x || 0) * roomWidth;
    const roomOriginY = outerMargin + Number(node.y || 0) * roomHeight;
    const rotation = Number.isInteger(chunkMatch.rotation) ? chunkMatch.rotation : rotationForSocketMask(node.socketMask || []);
    const originX = roomOriginX + Math.max(0, Math.floor((roomWidth - preset.width) / 2));
    const originY = roomOriginY + Math.max(0, Math.floor((roomHeight - preset.height) / 2));
    const cells = instantiatePreset(preset, { rotation, originX, originY }).cells
      .filter((cell) => cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height);
    const bounds = boundsOf(cells);
    const center = {
      x: Math.round(cells.reduce((sum, cell) => sum + cell.x, 0) / Math.max(1, cells.length)),
      y: Math.round(cells.reduce((sum, cell) => sum + cell.y, 0) / Math.max(1, cells.length)),
    };
    cells.forEach((cell) => open.add(`${cell.x},${cell.y}`));
    connectPointToRoomCenter(center, roomOriginX, roomOriginY);
    (node.socketMask || []).forEach((side) => placeSocketPath(roomOriginX, roomOriginY, side));
    modules.push({
      id: `module_${modules.length}`,
      nodeId: node.id,
      chunkId: chunk?.id || "",
      presetId: preset.id,
      rotation,
      originX,
      originY,
      baseWidth: preset.width,
      baseHeight: preset.height,
      cells,
      bounds,
      center,
      role: node.role || "",
      socketMask: [...(node.socketMask || [])],
      chunkMatch: {
        exactSocketMatch: Boolean(chunkMatch.exactSocketMatch),
        roleCompatible: Boolean(chunkMatch.roleCompatible),
        rotation,
        rotatedOpenSides: [...(chunkMatch.rotatedOpenSides || [])],
      },
    });
    moduleIndexByNodeId.set(node.id, modules.length - 1);
  }

  const roleNodeId = (role) => nodes.find((node) => node.role === role)?.id || nodes[0]?.id || "";
  const startNodeId = roleNodeId("start");
  const bossNodeId = roleNodeId("boss");
  const keyNodeId = roleNodeId("key");
  const sideNodeId = nodes.find((node) => node.role === "side_reward")?.id || nodes.find((node) => (node.branchDepth || 0) > 0)?.id || bossNodeId;
  const guardNodeId = roleNodeId("locked_gate") || nodes.find((node) => node.role === "combat")?.id || bossNodeId;

  const addDoorBetweenNodes = (nodeAId, nodeBId, edgeType = "main", locked = false) => {
    const nodeA = nodes.find((node) => node.id === nodeAId);
    const nodeB = nodes.find((node) => node.id === nodeBId);
    if (!nodeA || !nodeB) return;
    const dx = Number(nodeB.x || 0) - Number(nodeA.x || 0);
    const dy = Number(nodeB.y || 0) - Number(nodeA.y || 0);
    const roomOriginX = outerMargin + Number(nodeA.x || 0) * roomWidth;
    const roomOriginY = outerMargin + Number(nodeA.y || 0) * roomHeight;
    const midX = roomOriginX + roomWidth - 1;
    const midY = roomOriginY + Math.floor(roomHeight / 2);
    const side = dx === 1 && dy === 0 ? "east"
      : dx === -1 && dy === 0 ? "west"
      : dx === 0 && dy === 1 ? "south"
      : dx === 0 && dy === -1 ? "north"
      : "";
    const oppositeSide = side === "east" ? "west"
      : side === "west" ? "east"
      : side === "north" ? "south"
      : side === "south" ? "north"
      : "";
    const moduleA = modules[moduleIndexByNodeId.get(nodeAId) ?? -1] || null;
    const moduleB = modules[moduleIndexByNodeId.get(nodeBId) ?? -1] || null;
    const moduleASides = new Set(moduleA?.chunkMatch?.rotatedOpenSides || []);
    const moduleBSides = new Set(moduleB?.chunkMatch?.rotatedOpenSides || []);
    if (side && (!moduleASides.has(side) || !moduleBSides.has(oppositeSide))) {
      assemblyIssues.push({
        severity: "error",
        code: "edge_socket_connection_missing",
        edgeType,
        from: nodeAId,
        to: nodeBId,
        requiredFromSide: side,
        requiredToSide: oppositeSide,
      });
    }
    const doorType = edgeType === "secret" ? "secret" : "door";
    if (dx === 1 && dy === 0) {
      doors[wallKey(midX, midY, "east")] = { type: doorType, open: false, locked, keyId: locked ? profile.lockedDoorKeyId : undefined };
    } else if (dx === -1 && dy === 0) {
      doors[wallKey(roomOriginX, midY, "west")] = { type: doorType, open: false, locked, keyId: locked ? profile.lockedDoorKeyId : undefined };
    } else if (dx === 0 && dy === 1) {
      doors[wallKey(roomOriginX + Math.floor(roomWidth / 2), roomOriginY + roomHeight - 1, "south")] = { type: doorType, open: false, locked, keyId: locked ? profile.lockedDoorKeyId : undefined };
    } else if (dx === 0 && dy === -1) {
      doors[wallKey(roomOriginX + Math.floor(roomWidth / 2), roomOriginY, "north")] = { type: doorType, open: false, locked, keyId: locked ? profile.lockedDoorKeyId : undefined };
    }
  };

  for (const edge of Array.isArray(roomGraph?.edges) ? roomGraph.edges : []) {
    addDoorBetweenNodes(edge.from, edge.to, edge.type, edge.id === roomGraph.lockedEdge);
  }

  const startModule = modules[moduleIndexByNodeId.get(startNodeId) ?? 0] || modules[0];
  return {
    width,
    height,
    theme: profile.theme,
    chunkCatalog,
    open,
    doors,
    modules,
    rng,
    roomGraph,
    assemblyIssues,
    roles: {
      start: moduleIndexByNodeId.get(startNodeId) ?? 0,
      boss: moduleIndexByNodeId.get(bossNodeId) ?? 0,
      key: moduleIndexByNodeId.get(keyNodeId) ?? 0,
      side: moduleIndexByNodeId.get(sideNodeId) ?? 0,
      guard: moduleIndexByNodeId.get(guardNodeId) ?? 0,
    },
    start: { ...randomCellFromModule(startModule, rng), facing: "north" },
  };
}

export function generateFallbackLayout(floor, seed, profileOverride = null) {
  const profile = mergeGenerationProfile(mapProfileForFloor(floor), profileOverride);
  const open = new Set();
  const width = 15;
  const height = 15;
  for (let y = 2; y < height - 2; y++) open.add(`2,${y}`);
  for (let x = 2; x < width - 2; x++) open.add(`${x},${3 + (x % 2)}`);
  for (let x = 5; x < 12; x++) for (let y = 8; y < 11; y++) open.add(`${x},${y}`);
  const cells = [...open].map((key) => {
    const [x, y] = key.split(",").map(Number);
    return { x, y };
  });
  const module = { cells, center: { x: 7, y: 8 } };
  return {
    width,
    height,
    theme: profile.theme,
    open,
    doors: {},
    modules: [module, module, module, module, module],
    rng: mulberry32(seed),
    roles: { start: 0, boss: 1, key: 2, side: 3, guard: 4 },
    start: { x: 2, y: 12, facing: "north" },
  };
}

export function mulberry32(seed) {
  let t = seed >>> 0;
  return () => {
    t += 0x6D2B79F5;
    let r = Math.imul(t ^ (t >>> 15), 1 | t);
    r ^= r + Math.imul(r ^ (r >>> 7), 61 | r);
    return ((r ^ (r >>> 14)) >>> 0) / 4294967296;
  };
}

export function rectCells(x, y, width, height) {
  const cells = [];
  for (let iy = 0; iy < height; iy++) for (let ix = 0; ix < width; ix++) cells.push({ x: x + ix, y: y + iy });
  return cells;
}

export function crossCells(x, y, armX, armY) {
  const cells = [];
  cells.push(...rectCells(x + 1, y, 2, armY + 2));
  cells.push(...rectCells(x, y + 1, armX + 2, 2));
  return cells;
}

export function lShapeCells(x, y, width, height, flip) {
  const cells = rectCells(x, y, width, 2);
  const legX = flip ? x + width - 2 : x;
  cells.push(...rectCells(legX, y, 2, height));
  return cells;
}

export function overlapsModule(cells, modules) {
  return cells.some((cell) => modules.some((module) => cellNearModule(cell, module)));
}

export function cellNearModule(cell, module) {
  return module.cells.some((other) => Math.abs(other.x - cell.x) <= 1 && Math.abs(other.y - cell.y) <= 1);
}

export function boundsOf(cells) {
  return cells.reduce((acc, cell) => ({
    minX: Math.min(acc.minX, cell.x),
    maxX: Math.max(acc.maxX, cell.x),
    minY: Math.min(acc.minY, cell.y),
    maxY: Math.max(acc.maxY, cell.y),
  }), { minX: Infinity, maxX: -Infinity, minY: Infinity, maxY: -Infinity });
}

export function buildModuleConnections(modules, startIndex, rng) {
  const connected = new Set([startIndex]);
  const edges = [];
  while (connected.size < modules.length) {
    let best = null;
    for (const a of connected) {
      for (let b = 0; b < modules.length; b++) {
        if (connected.has(b)) continue;
        const dist = manhattan(modules[a].center, modules[b].center);
        if (!best || dist < best.dist) best = { a, b, dist };
      }
    }
    connected.add(best.b);
    edges.push(best);
  }
  const extraLinks = 1 + Math.floor(rng() * 2);
  for (let i = 0; i < extraLinks; i++) {
    const a = Math.floor(rng() * modules.length);
    const sorted = modules
      .map((module, index) => ({ index, dist: index === a ? Infinity : manhattan(modules[a].center, module.center) }))
      .sort((left, right) => left.dist - right.dist);
    const b = sorted[1 + Math.floor(rng() * 2)]?.index;
    if (b !== undefined && !edges.some((edge) => (edge.a === a && edge.b === b) || (edge.a === b && edge.b === a))) edges.push({ a, b });
  }
  return edges;
}

export function carveConnector(open, from, to, rng) {
  const start = randomEdgeCell(from, to.center, rng);
  const end = randomEdgeCell(to, from.center, rng);
  let x = start.x;
  let y = start.y;
  open.add(`${x},${y}`);
  while (x !== end.x || y !== end.y) {
    const horizontalFirst = rng() > 0.45;
    if (horizontalFirst && x !== end.x) x += Math.sign(end.x - x);
    else if (y !== end.y) y += Math.sign(end.y - y);
    else x += Math.sign(end.x - x);
    open.add(`${x},${y}`);
  }
}

export function randomEdgeCell(module, toward, rng) {
  const edgeCells = module.cells.filter((cell) =>
    cell.x === module.bounds.minX || cell.x === module.bounds.maxX || cell.y === module.bounds.minY || cell.y === module.bounds.maxY
  );
  edgeCells.sort((a, b) => manhattan(a, toward) - manhattan(b, toward));
  return edgeCells[Math.floor(rng() * Math.min(3, edgeCells.length))] || module.center;
}

export function carveSecretSpur(open, module, rng, width, height) {
  if (!module) return null;
  const origin = module.cells[Math.floor(rng() * module.cells.length)];
  const options = DIRS.map((dir) => ({ dir, ...VEC[dir] })).filter(({ x, y }) => {
    const sx = origin.x + x;
    const sy = origin.y + y;
    const tx = origin.x + x * 2;
    const ty = origin.y + y * 2;
    return sx > 0 && sx < width - 1 && sy > 0 && sy < height - 1 && tx > 0 && tx < width - 1 && ty > 0 && ty < height - 1
      && !open.has(`${sx},${sy}`) && !open.has(`${tx},${ty}`);
  });
  const pick = options[Math.floor(rng() * options.length)];
  if (!pick) return null;
  const sx = origin.x + pick.x;
  const sy = origin.y + pick.y;
  const tx = origin.x + pick.x * 2;
  const ty = origin.y + pick.y * 2;
  open.add(`${sx},${sy}`);
  open.add(`${tx},${ty}`);
  return wallKey(origin.x, origin.y, pick.dir);
}

export function placeCommonDoor(open, from, to, doors) {
  return findBoundaryDoor(open, from, to, doors);
}

export function placeLockedDoor(open, from, to, doors, floor, requiredStart = null, requiredTarget = null) {
  return findBoundaryDoor(open, from, to, doors, floor > 1 ? 2 : 1, requiredStart, requiredTarget);
}

export function findBoundaryDoor(open, from, to, doors, minDistance = 1, requiredStart = null, requiredTarget = null) {
  if (!from || !to) return null;
  const candidates = from.cells.flatMap((cell) => DIRS.map((dir) => ({ cell, dir, next: { x: cell.x + VEC[dir].x, y: cell.y + VEC[dir].y } })))
    .filter(({ cell, next }) =>
      open.has(`${cell.x},${cell.y}`) &&
      open.has(`${next.x},${next.y}`) &&
      !from.cells.some((p) => p.x === next.x && p.y === next.y) &&
      manhattan(next, to.center) >= minDistance
    )
    .sort((left, right) => manhattan(left.next, to.center) - manhattan(right.next, to.center));
  const pick = requiredStart && requiredTarget
    ? candidates.find((candidate) => reachableOpenCell(open, requiredStart, requiredTarget, wallKey(candidate.cell.x, candidate.cell.y, candidate.dir)))
    : candidates[0];
  if (!pick) return null;
  const key = wallKey(pick.cell.x, pick.cell.y, pick.dir);
  if (doors[key]) return null;
  return key;
}

function reachableOpenCell(open, start, target, blockedWallKey = "") {
  const startKey = `${start.x},${start.y}`;
  const targetKey = `${target.x},${target.y}`;
  if (!open.has(startKey) || !open.has(targetKey)) return false;
  const seen = new Set([startKey]);
  const queue = [{ x: start.x, y: start.y }];
  while (queue.length) {
    const current = queue.shift();
    if (`${current.x},${current.y}` === targetKey) return true;
    for (const dir of DIRS) {
      if (wallKey(current.x, current.y, dir) === blockedWallKey) continue;
      const v = VEC[dir];
      const next = { x: current.x + v.x, y: current.y + v.y };
      if (wallKey(next.x, next.y, opposite(dir)) === blockedWallKey) continue;
      const key = `${next.x},${next.y}`;
      if (!open.has(key) || seen.has(key)) continue;
      seen.add(key);
      queue.push(next);
    }
  }
  return false;
}

export function furthestModuleIndex(modules, from, originalIndexes) {
  if (!modules.length) return 0;
  const best = modules.reduce((winner, module, index) => {
    const dist = manhattan(module.center, from);
    return dist > winner.dist ? { index, dist } : winner;
  }, { index: 0, dist: -Infinity });
  return originalIndexes ? originalIndexes[best.index] : best.index;
}

export function nearestDifferentModule(modules, from, excluded) {
  const options = modules
    .map((module, index) => ({ index, dist: excluded.has(index) ? Infinity : manhattan(module.center, from) }))
    .sort((a, b) => a.dist - b.dist);
  return options.find((option) => Number.isFinite(option.dist))?.index;
}

export function manhattan(a, b) {
  return Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
}

export function randomCellFromModule(module, rng, occupied = []) {
  const blocked = new Set(occupied);
  const choices = module.cells.filter((cell) => !blocked.has(`${cell.x},${cell.y}`));
  if (choices.length) return choices[Math.floor(rng() * choices.length)];
  return null;
}

function randomOpenCellFromGenerated(generated, rng, occupied = []) {
  const blocked = new Set(occupied);
  const choices = [...(generated?.open || [])]
    .map((entry) => {
      const [x, y] = String(entry).split(",").map(Number);
      return { x, y };
    })
    .filter((cell) => Number.isFinite(cell.x) && Number.isFinite(cell.y) && !blocked.has(cellKey(cell)));
  if (choices.length) return choices[Math.floor(rng() * choices.length)];
  return null;
}

function openNeighborsByKey(generated) {
  const open = new Set([...(generated?.open || [])].map((entry) => String(entry)));
  const neighbors = new Map();
  open.forEach((entry) => {
    const [x, y] = entry.split(",").map(Number);
    const adjacent = [];
    DIRS.forEach((dir) => {
      const next = `${x + VEC[dir].x},${y + VEC[dir].y}`;
      if (open.has(next)) adjacent.push(next);
    });
    neighbors.set(entry, adjacent);
  });
  return neighbors;
}

function cellKey(point) {
  return `${point.x},${point.y}`;
}

function moduleNeighborsByKey(module) {
  const cells = Array.isArray(module?.cells) ? module.cells : [];
  const keys = new Set(cells.map((cell) => cellKey(cell)));
  const neighbors = new Map();
  cells.forEach((cell) => {
    const adjacent = [];
    DIRS.forEach((dir) => {
      const next = { x: cell.x + VEC[dir].x, y: cell.y + VEC[dir].y };
      if (keys.has(cellKey(next))) adjacent.push(next);
    });
    neighbors.set(cellKey(cell), adjacent);
  });
  return neighbors;
}

function moduleCellsReachableWithoutCandidate(module, candidateKey) {
  const cells = Array.isArray(module?.cells) ? module.cells : [];
  const start = cells.find((cell) => cellKey(cell) !== candidateKey);
  if (!start) return 0;
  const neighbors = moduleNeighborsByKey(module);
  const seen = new Set([cellKey(start)]);
  const queue = [start];
  while (queue.length) {
    const current = queue.shift();
    (neighbors.get(cellKey(current)) || []).forEach((next) => {
      const key = cellKey(next);
      if (key === candidateKey || seen.has(key)) return;
      seen.add(key);
      queue.push(next);
    });
  }
  return seen.size;
}

function safeNpcCellsFromModule(module, occupied = []) {
  const blocked = new Set(occupied);
  const candidates = (Array.isArray(module?.cells) ? module.cells : []).filter((cell) => !blocked.has(cellKey(cell)));
  if (candidates.length <= 1) return candidates;
  const remainingCellCount = candidates.length - 1;
  return candidates.filter((cell) => moduleCellsReachableWithoutCandidate(module, cellKey(cell)) === remainingCellCount);
}

function generatedCriticalKeysRemainReachable(generated, blockedActorKeys = [], requiredReachableKeys = []) {
  const open = new Set([...(generated?.open || [])].map((entry) => String(entry)));
  const criticalKeys = [...new Set((requiredReachableKeys || []).map((entry) => String(entry)).filter((entry) => open.has(entry)))];
  if (!criticalKeys.length) return true;
  const blocked = new Set((blockedActorKeys || []).map((entry) => String(entry)));
  const startKey = criticalKeys.find((entry) => !blocked.has(entry));
  if (!startKey) return false;
  const neighbors = openNeighborsByKey(generated);
  const seen = new Set([startKey]);
  const queue = [startKey];
  while (queue.length) {
    const current = queue.shift();
    (neighbors.get(current) || []).forEach((next) => {
      if (blocked.has(next) || seen.has(next)) return;
      seen.add(next);
      queue.push(next);
    });
  }
  return criticalKeys.every((entry) => blocked.has(entry) || seen.has(entry));
}

function safeActorCellsFromModule(module, generated, occupied = [], blockingOccupied = [], requiredReachable = [], preserveModuleConnectivity = false) {
  const blocked = new Set(occupied);
  const moduleCandidates = (Array.isArray(module?.cells) ? module.cells : []).filter((cell) => !blocked.has(cellKey(cell)));
  return moduleCandidates.filter((cell) => {
    const candidateKey = cellKey(cell);
    if (preserveModuleConnectivity && moduleCandidates.length > 1) {
      const remainingCellCount = moduleCandidates.length - 1;
      if (moduleCellsReachableWithoutCandidate(module, candidateKey) !== remainingCellCount) return false;
    }
    return generatedCriticalKeysRemainReachable(generated, [...blockingOccupied, candidateKey], requiredReachable);
  });
}

function safeCriticalCellsFromModule(module, generated, occupied = [], requiredReachable = []) {
  const blocked = new Set(occupied);
  const moduleCandidates = (Array.isArray(module?.cells) ? module.cells : []).filter((cell) => !blocked.has(cellKey(cell)));
  return moduleCandidates.filter((cell) => generatedCriticalKeysRemainReachable(generated, [], [...requiredReachable, cellKey(cell)]));
}

function safeActorOpenCellsFromGenerated(generated, occupied = [], blockingOccupied = [], requiredReachable = []) {
  const blocked = new Set(occupied);
  const candidates = [...(generated?.open || [])]
    .map((entry) => {
      const [x, y] = String(entry).split(",").map(Number);
      return { x, y };
    })
    .filter((cell) => Number.isFinite(cell.x) && Number.isFinite(cell.y) && !blocked.has(cellKey(cell)));
  return candidates.filter((cell) => generatedCriticalKeysRemainReachable(generated, [...blockingOccupied, cellKey(cell)], requiredReachable));
}

function safeCriticalOpenCellsFromGenerated(generated, occupied = [], requiredReachable = []) {
  const blocked = new Set(occupied);
  const candidates = [...(generated?.open || [])]
    .map((entry) => {
      const [x, y] = String(entry).split(",").map(Number);
      return { x, y };
    })
    .filter((cell) => Number.isFinite(cell.x) && Number.isFinite(cell.y) && !blocked.has(cellKey(cell)));
  return candidates.filter((cell) => generatedCriticalKeysRemainReachable(generated, [], [...requiredReachable, cellKey(cell)]));
}

export function placementPointFromModule(module, generated, kind, anchorKinds = [], occupancy = []) {
  const occupied = Array.isArray(occupancy) ? occupancy : (occupancy?.occupied || []);
  const blockingOccupied = Array.isArray(occupancy) ? [] : (occupancy?.blockingOccupied || []);
  const requiredReachable = Array.isArray(occupancy) ? [] : (occupancy?.requiredReachable || []);
  const safeActorCells = GENERATED_MOVEMENT_BLOCKING_KINDS.has(kind)
    ? safeActorCellsFromModule(module, generated, occupied, blockingOccupied, requiredReachable, kind === "npc")
    : null;
  const safeStairsCells = kind === "stairs"
    ? safeCriticalCellsFromModule(module, generated, occupied, requiredReachable)
    : null;
  const allowedCells = safeActorCells || safeStairsCells;
  const allowedKeys = allowedCells ? new Set(allowedCells.map((cell) => cellKey(cell))) : null;
  const chunk = selectChunkForLegacyModule(module, generated);
  const anchor = firstChunkAnchor(chunk, anchorKinds.length ? anchorKinds : [kind, "generic"]);
  if (anchor) {
    const point = resolveModuleAnchorPoint(module, anchor);
    if (point && !occupied.includes(cellKey(point)) && (!allowedKeys || allowedKeys.has(cellKey(point)))) return point;
  }
  if (safeActorCells?.length) return safeActorCells[Math.floor(generated.rng() * safeActorCells.length)];
  if (safeStairsCells?.length) return safeStairsCells[Math.floor(generated.rng() * safeStairsCells.length)];
  const safeGlobalActorCells = GENERATED_MOVEMENT_BLOCKING_KINDS.has(kind)
    ? safeActorOpenCellsFromGenerated(generated, occupied, blockingOccupied, requiredReachable)
    : null;
  if (safeGlobalActorCells?.length) return safeGlobalActorCells[Math.floor(generated.rng() * safeGlobalActorCells.length)];
  const safeGlobalCriticalCells = kind === "stairs"
    ? safeCriticalOpenCellsFromGenerated(generated, occupied, requiredReachable)
    : null;
  if (safeGlobalCriticalCells?.length) return safeGlobalCriticalCells[Math.floor(generated.rng() * safeGlobalCriticalCells.length)];
  return randomCellFromModule(module, generated.rng, occupied)
    || randomOpenCellFromGenerated(generated, generated.rng, occupied)
    || module?.cells?.[0]
    || module?.center
    || generated?.start
    || { x: 0, y: 0 };
}

export function selectChunkForLegacyModule(module, generated) {
  if (!module) return null;
  const role = Object.entries(generated.roles || {}).find(([, index]) => generated.modules[index] === module)?.[0] || "";
  const theme = generated.theme || "buried_temple";
  const chunkCatalog = resolveChunkCatalog(theme, generated.chunkCatalog);
  const exact = chunkCatalog.find((chunk) => chunk.id === module.chunkId)
    || chunkCatalog.find((chunk) => chunk.presetId === module.presetId && (!role || chunk.roleTags.includes(role)));
  return exact || selectChunkForNode({ role, socketMask: [] }, theme, chunkCatalog);
}

export function resolveModuleAnchorPoint(module, anchor) {
  if (!module || !anchor) return null;
  const rotation = Number(module.rotation || 0);
  const rotated = rotateLocalPoint(anchor.x, anchor.y, Number(module.baseWidth || 1), Number(module.baseHeight || 1), rotation);
  const point = { x: Number(module.originX || 0) + rotated.x, y: Number(module.originY || 0) + rotated.y };
  return module.cells.some((cell) => cell.x === point.x && cell.y === point.y) ? point : null;
}

export function rotateLocalPoint(x, y, width, height, rotation = 0) {
  let px = Number(x || 0);
  let py = Number(y || 0);
  let currentWidth = Math.max(1, Number(width || 1));
  let currentHeight = Math.max(1, Number(height || 1));
  const turns = ((Number(rotation || 0) % 4) + 4) % 4;
  for (let i = 0; i < turns; i += 1) {
    const nextX = currentHeight - 1 - py;
    const nextY = px;
    px = nextX;
    py = nextY;
    [currentWidth, currentHeight] = [currentHeight, currentWidth];
  }
  return { x: px, y: py };
}

export function rotationForSocketMask(socketMask = []) {
  const sides = new Set(socketMask);
  if (sides.has("north") && sides.has("south") && !sides.has("east") && !sides.has("west")) return 1;
  if (sides.has("north") && sides.has("east") && !sides.has("south") && !sides.has("west")) return 1;
  if (sides.has("east") && sides.has("south") && !sides.has("north") && !sides.has("west")) return 2;
  if (sides.has("south") && sides.has("west") && !sides.has("north") && !sides.has("east")) return 3;
  return 0;
}

export function wallKey(x, y, dir) {
  return `${x},${y},${dir}`;
}

export function opposite(dir) {
  return DIRS[(DIRS.indexOf(dir) + 2) % 4];
}

export function oppositeDoor(map, x, y, dir) {
  const v = VEC[dir];
  const key = wallKey(x + v.x, y + v.y, opposite(dir));
  return map.doors[key];
}

export function computeWalls(map, options = {}) {
  const {
    wallTextureIds = [],
    defaultWallTextureId = "wall_stone_01",
    normalizeTextureId = (id, list, fallback) => (list.includes(id) ? id : fallback),
  } = options;
  for (const cell of map.cells) {
    cell.walls = {};
    for (const dir of DIRS) {
      const v = VEC[dir];
      const n = getCell(map, cell.x + v.x, cell.y + v.y);
      const key = wallKey(cell.x, cell.y, dir);
      const door = map.doors[key] || oppositeDoor(map, cell.x, cell.y, dir);
      if (door) {
        cell.walls[dir] = {
          type: door.type,
          texture: "door_bronze_01",
          blocksMovement: !door.open,
          blocksSight: !door.open,
          locked: door.locked,
          keyId: door.keyId,
          variant: "doorframe",
        };
      } else if (!n || !cell.walkable || !n.walkable) {
        cell.walls[dir] = {
          type: "solid",
          texture: normalizeTextureId(cell.wallTexture || n?.wallTexture, wallTextureIds, defaultWallTextureId),
          materialId: normalizeTextureId(cell.wallMaterialId || cell.wallTexture || n?.wallMaterialId || n?.wallTexture, wallTextureIds, defaultWallTextureId),
          variant: wallVariantForCell(cell, dir),
          blocksMovement: true,
          blocksSight: true,
        };
      }
    }
  }
}

export function resolveTileAdjacency(map, options = {}) {
  const substitutions = Array.isArray(options.substitutions) ? options.substitutions : [];
  for (const cell of map.cells) {
    const role = classifyTileRole(map, cell);
    cell.tileRole = role;
    cell.floorVariant = role;
    cell.decorTags = Array.isArray(cell.decorTags) ? cell.decorTags : [];
    applyTileSubstitution(cell, substitutions);
  }
}

export function classifyTileRole(map, cell) {
  if (!cell?.walkable) return "solid";
  const north = Boolean(getCell(map, cell.x, cell.y - 1)?.walkable);
  const east = Boolean(getCell(map, cell.x + 1, cell.y)?.walkable);
  const south = Boolean(getCell(map, cell.x, cell.y + 1)?.walkable);
  const west = Boolean(getCell(map, cell.x - 1, cell.y)?.walkable);
  const count = [north, east, south, west].filter(Boolean).length;
  if (count <= 1) return "end_cap";
  if (count === 4) return "intersection";
  if (count === 3) return "junction";
  if ((north && south) || (east && west)) return "corridor";
  return "corner";
}

export function applyTileSubstitution(cell, substitutions = []) {
  const matches = substitutions.filter((entry) => entry.target === "floor" && (entry.whenTileRoles || []).includes(cell.tileRole));
  if (!matches.length) return;
  const substitution = matches[Math.abs(hashCoord(cell.x, cell.y)) % matches.length];
  const variants = Array.isArray(substitution.variants) ? substitution.variants : [];
  if (!variants.length) return;
  const weighted = [];
  variants.forEach((variant) => {
    const weight = Math.max(1, Number(variant.weight || 1));
    for (let index = 0; index < weight; index += 1) weighted.push(variant);
  });
  const selected = weighted[Math.abs(hashCoord(cell.x * 13, cell.y * 17)) % weighted.length];
  if (!selected) return;
  cell.floorMaterialId = selected.materialId || cell.floorMaterialId || cell.floorTexture;
  cell.floorTexture = selected.materialId || cell.floorTexture;
  if (selected.tag && !cell.decorTags.includes(selected.tag)) cell.decorTags.push(selected.tag);
}

export function wallVariantForCell(cell, dir) {
  if (!cell?.walkable) return "solid";
  if (cell.tileRole === "intersection") return "pillar";
  if (cell.tileRole === "junction") return "t_junction";
  if (cell.tileRole === "corner") return "corner";
  if (cell.tileRole === "end_cap") return "end_cap";
  if (cell.tileRole === "corridor") return (dir === "north" || dir === "south") ? "wall_trim_ns" : "wall_trim_ew";
  return "room_wall";
}

export function applyDecorationPass(map, options = {}) {
  const objectTheme = options.objectTheme;
  if (!objectTheme?.decor?.length) return;
  const rng = mulberry32((Number(options.seed) || 0) ^ 0x13572468);
  const decor = [];
  const counts = new Map();
  for (const cell of map.cells) {
    if (!cell.walkable) continue;
    for (const rule of objectTheme.decor) {
      const current = counts.get(rule.kind) || 0;
      if (current >= Number(rule.maxPerMap || 0)) continue;
      if (!Array.isArray(rule.tileRoles) || !rule.tileRoles.includes(cell.tileRole)) continue;
      const threshold = Number(rule.weight || 1) / 20;
      if (rng() > threshold) continue;
      decor.push({
        id: `decor_${rule.kind}_${decor.length}`,
        kind: rule.kind,
        x: cell.x,
        y: cell.y,
        color: rule.color || "#8a6747",
        visualOnly: true,
        tileRole: cell.tileRole,
      });
      counts.set(rule.kind, current + 1);
      if (!cell.decorTags.includes(`decor_${rule.kind}`)) cell.decorTags.push(`decor_${rule.kind}`);
      break;
    }
  }
  map.decor = decor;
}

export function hashCoord(x, y) {
  return ((Number(x) * 73856093) ^ (Number(y) * 19349663)) | 0;
}

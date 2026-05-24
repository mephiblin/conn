import { loadJsonAsset } from "./contentRegistry.js";

const DEFAULT_MAP_PROFILES = [
  {
    floor: 1,
    mapId: "serpent_temple_floor_1",
    name: "매몰된 입구",
    profileId: "buried_temple_intro",
    theme: "buried_temple",
    algorithm: "block_modules_and_connectors",
    gridRoomSize: { width: 7, height: 7 },
    layout: { width: 19, height: 19 },
    targetModuleCount: 8,
    mergeChancePer1000: 160,
    criticalPath: { min: 5, max: 7 },
    loopCount: { min: 1, max: 2 },
    sideBranchCount: 2,
    lockedDoorKeyId: "bronze_key",
    requiredAnchors: ["start", "key", "boss", "stairs_down_01"],
  },
  {
    floor: 2,
    mapId: "serpent_temple_floor_2",
    name: "도굴꾼의 야영지",
    profileId: "robber_camp",
    theme: "buried_temple",
    algorithm: "block_modules_and_connectors",
    gridRoomSize: { width: 7, height: 7 },
    layout: { width: 19, height: 19 },
    targetModuleCount: 9,
    mergeChancePer1000: 220,
    criticalPath: { min: 6, max: 8 },
    loopCount: { min: 1, max: 2 },
    sideBranchCount: 2,
    lockedDoorKeyId: "obsidian_key",
    requiredAnchors: ["start", "key", "boss", "stairs_down_02"],
  },
  {
    floor: 3,
    mapId: "serpent_temple_floor_3",
    name: "뱀 사제의 회랑",
    profileId: "serpent_priest_corridor",
    theme: "buried_temple",
    algorithm: "block_modules_and_connectors",
    gridRoomSize: { width: 7, height: 7 },
    layout: { width: 19, height: 19 },
    targetModuleCount: 10,
    mergeChancePer1000: 260,
    criticalPath: { min: 7, max: 9 },
    loopCount: { min: 2, max: 3 },
    sideBranchCount: 3,
    lockedDoorKeyId: "priest_mask",
    requiredAnchors: ["start", "key", "boss", "final_stairs_03"],
  },
];

const DEFAULT_MAP_CHUNKS = [
  {
    id: "rect_hall_ns",
    presetId: "rect_hall",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "south"],
    doorSockets: ["north", "south"],
    anchors: [{ id: "center", kind: "generic", x: 2, y: 1 }],
    variantGroup: "rect_hall",
    roleTags: ["start", "key", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "rect_hall_ew",
    presetId: "rect_hall",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["east", "west"],
    doorSockets: ["east", "west"],
    anchors: [{ id: "center", kind: "generic", x: 2, y: 1 }],
    variantGroup: "rect_hall",
    roleTags: ["start", "key", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "long_hall_ns",
    presetId: "long_hall",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "south"],
    doorSockets: ["north", "south"],
    anchors: [{ id: "spine", kind: "corridor", x: 1, y: 2 }],
    variantGroup: "long_hall",
    roleTags: ["start", "key", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "long_hall_ew",
    presetId: "long_hall",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["east", "west"],
    doorSockets: ["east", "west"],
    anchors: [{ id: "spine", kind: "corridor", x: 3, y: 1 }],
    variantGroup: "long_hall",
    roleTags: ["start", "key", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "cross_block_junction",
    presetId: "cross_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "east", "south", "west"],
    doorSockets: ["north", "east", "south", "west"],
    anchors: [{ id: "center", kind: "junction", x: 1, y: 1 }],
    variantGroup: "cross_block",
    roleTags: ["start", "key", "side", "combat"],
  },
  {
    id: "cross_block_t_nes",
    presetId: "cross_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "east", "south"],
    doorSockets: ["north", "east", "south"],
    anchors: [{ id: "center", kind: "junction", x: 1, y: 1 }],
    variantGroup: "cross_block_t",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "cross_block_t_new",
    presetId: "cross_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "east", "west"],
    doorSockets: ["north", "east", "west"],
    anchors: [{ id: "center", kind: "junction", x: 1, y: 1 }],
    variantGroup: "cross_block_t",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "cross_block_t_nsw",
    presetId: "cross_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "south", "west"],
    doorSockets: ["north", "south", "west"],
    anchors: [{ id: "center", kind: "junction", x: 1, y: 1 }],
    variantGroup: "cross_block_t",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "cross_block_t_esw",
    presetId: "cross_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["east", "south", "west"],
    doorSockets: ["east", "south", "west"],
    anchors: [{ id: "center", kind: "junction", x: 1, y: 1 }],
    variantGroup: "cross_block_t",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "l_block_turn_ne",
    presetId: "l_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "east"],
    doorSockets: ["north", "east"],
    anchors: [{ id: "turn", kind: "junction", x: 1, y: 1 }],
    variantGroup: "l_block",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "l_block_turn_nw",
    presetId: "l_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "west"],
    doorSockets: ["north", "west"],
    anchors: [{ id: "turn", kind: "junction", x: 1, y: 1 }],
    variantGroup: "l_block",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "l_block_turn_sw",
    presetId: "l_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["south", "west"],
    doorSockets: ["south", "west"],
    anchors: [{ id: "turn", kind: "junction", x: 1, y: 1 }],
    variantGroup: "l_block",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "l_block_turn_es",
    presetId: "l_block",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["east", "south"],
    doorSockets: ["east", "south"],
    anchors: [{ id: "turn", kind: "junction", x: 1, y: 1 }],
    variantGroup: "l_block",
    roleTags: ["start", "key", "locked_gate", "boss", "combat", "guard", "side", "side_reward"],
  },
  {
    id: "shrine_boss_anchor",
    presetId: "shrine",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["south"],
    doorSockets: ["south"],
    anchors: [{ id: "altar", kind: "boss_spawn", x: 2, y: 4 }],
    variantGroup: "shrine",
    roleTags: ["start", "boss", "reward", "side_reward"],
  },
  {
    id: "crypt_cluster_loot",
    presetId: "crypt_cluster",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["west", "south"],
    doorSockets: ["west", "south"],
    anchors: [{ id: "treasure", kind: "loot", x: 2, y: 2 }],
    variantGroup: "crypt_cluster",
    roleTags: ["key", "reward", "side", "side_reward"],
  },
  {
    id: "crypt_cluster_loot_ne",
    presetId: "crypt_cluster",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "east"],
    doorSockets: ["north", "east"],
    anchors: [{ id: "treasure", kind: "loot", x: 2, y: 2 }],
    variantGroup: "crypt_cluster",
    roleTags: ["key", "reward", "side", "side_reward"],
  },
  {
    id: "crypt_cluster_loot_nw",
    presetId: "crypt_cluster",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["north", "west"],
    doorSockets: ["north", "west"],
    anchors: [{ id: "treasure", kind: "loot", x: 2, y: 2 }],
    variantGroup: "crypt_cluster",
    roleTags: ["key", "reward", "side", "side_reward"],
  },
  {
    id: "crypt_cluster_loot_es",
    presetId: "crypt_cluster",
    theme: "buried_temple",
    size: { width: 7, height: 7 },
    openSides: ["east", "south"],
    doorSockets: ["east", "south"],
    anchors: [{ id: "treasure", kind: "loot", x: 2, y: 2 }],
    variantGroup: "crypt_cluster",
    roleTags: ["key", "reward", "side", "side_reward"],
  },
];

function normalizeStringArray(values = []) {
  return [...new Set((Array.isArray(values) ? values : []).filter((value) => typeof value === "string" && value.trim()))];
}

function normalizeProfileDefinition(definition = {}, fallbackFloor = 1) {
  return {
    floor: Math.max(1, Number(definition.floor || fallbackFloor)),
    mapId: String(definition.mapId || `generated_floor_${fallbackFloor}`),
    name: String(definition.name || `Floor ${fallbackFloor}`),
    profileId: String(definition.profileId || `profile_floor_${fallbackFloor}`),
    mapKind: String(definition.mapKind || definition.theme || "twisted_temple"),
    mapKindName: String(definition.mapKindName || definition.name || `Floor ${fallbackFloor}`),
    theme: String(definition.theme || "buried_temple"),
    algorithm: String(definition.algorithm || "block_modules_and_connectors"),
    gridRoomSize: {
      width: Math.max(3, Number(definition.gridRoomSize?.width || 7)),
      height: Math.max(3, Number(definition.gridRoomSize?.height || 7)),
    },
    layout: {
      width: Math.max(7, Number(definition.layout?.width || 19)),
      height: Math.max(7, Number(definition.layout?.height || 19)),
    },
    targetModuleCount: Math.max(5, Number(definition.targetModuleCount || (7 + fallbackFloor))),
    mergeChancePer1000: Math.max(0, Math.min(1000, Number(definition.mergeChancePer1000 || 0))),
    criticalPath: {
      min: Math.max(2, Number(definition.criticalPath?.min || 4)),
      max: Math.max(2, Number(definition.criticalPath?.max || 6)),
    },
    loopCount: {
      min: Math.max(0, Number(definition.loopCount?.min || 0)),
      max: Math.max(0, Number(definition.loopCount?.max || 1)),
    },
    sideBranchCount: Math.max(0, Number(definition.sideBranchCount || 0)),
    lockedDoorKeyId: String(definition.lockedDoorKeyId || ""),
    requiredAnchors: normalizeStringArray(definition.requiredAnchors || []),
  };
}

function normalizeChunkDefinition(definition = {}, index = 0) {
  return {
    id: String(definition.id || `chunk_${index}`),
    presetId: String(definition.presetId || ""),
    theme: String(definition.theme || "buried_temple"),
    size: {
      width: Math.max(1, Number(definition.size?.width || 7)),
      height: Math.max(1, Number(definition.size?.height || 7)),
    },
    openSides: normalizeStringArray(definition.openSides || []),
    doorSockets: normalizeStringArray(definition.doorSockets || []),
    anchors: Array.isArray(definition.anchors)
      ? definition.anchors
        .filter((anchor) => anchor && typeof anchor === "object")
        .map((anchor, anchorIndex) => ({
          id: String(anchor.id || `anchor_${anchorIndex}`),
          kind: String(anchor.kind || "generic"),
          x: Number(anchor.x || 0),
          y: Number(anchor.y || 0),
        }))
      : [],
    variantGroup: String(definition.variantGroup || definition.presetId || `variant_group_${index}`),
    roleTags: normalizeStringArray(definition.roleTags || []),
  };
}

export function validateMapProfileDefinitions(definitions = []) {
  const issues = [];
  definitions.forEach((definition, index) => {
    if (!definition.profileId) issues.push({ severity: "error", code: "missing_profile_id", index });
    if (!definition.mapId) issues.push({ severity: "error", code: "missing_map_id", index });
    if (!definition.lockedDoorKeyId) issues.push({ severity: "error", code: "missing_locked_door_key", index, profileId: definition.profileId || "" });
    if (Number(definition.layout?.width || 0) < 7 || Number(definition.layout?.height || 0) < 7) issues.push({ severity: "error", code: "invalid_layout_size", index, profileId: definition.profileId || "" });
    if (Number(definition.targetModuleCount || 0) < 5) issues.push({ severity: "error", code: "invalid_target_module_count", index, profileId: definition.profileId || "" });
    if (Number(definition.criticalPath?.min || 0) > Number(definition.criticalPath?.max || 0)) issues.push({ severity: "error", code: "invalid_critical_path_range", index, profileId: definition.profileId || "" });
    if (Number(definition.loopCount?.min || 0) > Number(definition.loopCount?.max || 0)) issues.push({ severity: "error", code: "invalid_loop_count_range", index, profileId: definition.profileId || "" });
  });
  return issues;
}

export function validateMapChunkDefinitions(definitions = []) {
  const issues = [];
  definitions.forEach((definition, index) => {
    if (!definition.id) issues.push({ severity: "error", code: "missing_chunk_id", index });
    if (!definition.presetId) issues.push({ severity: "error", code: "missing_preset_id", index, chunkId: definition.id || "" });
    if (!definition.openSides?.length) issues.push({ severity: "error", code: "missing_open_sides", index, chunkId: definition.id || "" });
    if (!definition.anchors?.length) issues.push({ severity: "error", code: "missing_anchors", index, chunkId: definition.id || "" });
    if (Number(definition.size?.width || 0) < 1 || Number(definition.size?.height || 0) < 1) issues.push({ severity: "error", code: "invalid_chunk_size", index, chunkId: definition.id || "" });
  });
  return issues;
}

async function loadMapGenerationJson(path, fallback) {
  try {
    const loaded = await loadJsonAsset(path);
    return Array.isArray(loaded) ? loaded : fallback;
  } catch {
    return fallback;
  }
}

const rawMapProfiles = await loadMapGenerationJson("./data/map_profiles.json", DEFAULT_MAP_PROFILES);
const rawMapChunks = await loadMapGenerationJson("./data/map_chunks.json", DEFAULT_MAP_CHUNKS);

export const MAP_PROFILE_DEFINITIONS = rawMapProfiles.map((definition, index) => normalizeProfileDefinition(definition, index + 1));
export const MAP_CHUNK_DEFINITIONS = rawMapChunks.map((definition, index) => normalizeChunkDefinition(definition, index));
export const MAP_PROFILE_VALIDATION_ISSUES = validateMapProfileDefinitions(MAP_PROFILE_DEFINITIONS);
export const MAP_CHUNK_VALIDATION_ISSUES = validateMapChunkDefinitions(MAP_CHUNK_DEFINITIONS);

const MAP_PROFILE_BY_FLOOR = new Map(MAP_PROFILE_DEFINITIONS.map((definition) => [Number(definition.floor), definition]));

export function mapProfileForFloor(floor) {
  return MAP_PROFILE_BY_FLOOR.get(Number(floor)) || MAP_PROFILE_DEFINITIONS[0];
}

export function mapChunkCatalogForTheme(theme = "buried_temple") {
  const filtered = MAP_CHUNK_DEFINITIONS.filter((definition) => definition.theme === theme);
  return filtered.length ? filtered : MAP_CHUNK_DEFINITIONS;
}

export function resolveChunkCatalog(theme = "buried_temple", catalogOverride = null) {
  if (Array.isArray(catalogOverride) && catalogOverride.length) return catalogOverride;
  return mapChunkCatalogForTheme(theme);
}

function roleRequiresExactCompatibility(role = "") {
  return !["", "combat", "empty"].includes(String(role || ""));
}

const SOCKET_DIRS = ["north", "east", "south", "west"];

function compatibleRoleTagsForRole(role = "") {
  const normalized = String(role || "");
  if (!normalized) return [];
  if (normalized === "locked_gate") return ["locked_gate", "guard", "combat"];
  if (normalized === "side_reward") return ["side_reward", "side", "reward"];
  if (normalized === "key") return ["key", "reward", "side"];
  if (normalized === "stairs") return ["stairs", "side_reward", "reward"];
  return [normalized];
}

function rotateSide(side, turns = 0) {
  const index = SOCKET_DIRS.indexOf(side);
  if (index < 0) return side;
  return SOCKET_DIRS[(index + turns + SOCKET_DIRS.length) % SOCKET_DIRS.length];
}

function rotateSides(sides = [], turns = 0) {
  return normalizeStringArray(sides.map((side) => rotateSide(side, turns)));
}

function overlapCount(requiredSides = [], candidateSides = []) {
  const candidate = new Set(candidateSides);
  return requiredSides.filter((side) => candidate.has(side)).length;
}

function roleMatchScore(role = "", roleTags = []) {
  const normalizedRole = String(role || "");
  const tags = normalizeStringArray(roleTags);
  if (!normalizedRole || !tags.length) return 0;
  if (tags.includes(normalizedRole)) return 4;
  const compatible = compatibleRoleTagsForRole(normalizedRole);
  const compatibleIndex = compatible.findIndex((entry) => tags.includes(entry));
  if (compatibleIndex >= 0) return 3 - Math.min(compatibleIndex, 2);
  if (tags.includes("combat")) return 1;
  return 0;
}

function anchorMatchScore(role = "", anchors = []) {
  const kinds = (Array.isArray(anchors) ? anchors : []).map((anchor) => String(anchor?.kind || ""));
  if (!kinds.length) return 0;
  if (role === "boss" && kinds.includes("boss_spawn")) return 3;
  if ((role === "key" || role === "side_reward") && kinds.includes("loot")) return 3;
  if (role === "locked_gate" && kinds.includes("junction")) return 2;
  if (role === "start" && (kinds.includes("generic") || kinds.includes("junction"))) return 2;
  if (kinds.includes("junction")) return 1;
  return 0;
}

function hashSelectionSeed(node = {}, role = "", socketMask = []) {
  const text = [
    String(node.id || ""),
    String(role || ""),
    String(node.x ?? ""),
    String(node.y ?? ""),
    ...normalizeStringArray(socketMask),
  ].join("|");
  let hash = 0;
  for (let index = 0; index < text.length; index += 1) hash = ((hash * 33) ^ text.charCodeAt(index)) >>> 0;
  return hash >>> 0;
}

function chooseDeterministicCandidate(candidates = [], node = {}, role = "", socketMask = []) {
  if (!candidates.length) return null;
  const scored = candidates.map((entry) => ({
    ...entry,
    roleScore: roleMatchScore(role, entry.chunk?.roleTags || []),
    anchorScore: anchorMatchScore(role, entry.chunk?.anchors || []),
  }));
  scored.sort((left, right) => {
    if (right.roleScore !== left.roleScore) return right.roleScore - left.roleScore;
    if (right.anchorScore !== left.anchorScore) return right.anchorScore - left.anchorScore;
    const leftVariant = String(left.chunk?.variantGroup || "");
    const rightVariant = String(right.chunk?.variantGroup || "");
    if (leftVariant !== rightVariant) return leftVariant.localeCompare(rightVariant);
    return String(left.chunk?.id || "").localeCompare(String(right.chunk?.id || ""));
  });
  const topRoleScore = scored[0].roleScore;
  const topAnchorScore = scored[0].anchorScore;
  const topCandidates = scored.filter((entry) => entry.roleScore === topRoleScore && entry.anchorScore === topAnchorScore);
  const pickIndex = hashSelectionSeed(node, role, socketMask) % topCandidates.length;
  return topCandidates[pickIndex];
}

export function evaluateChunkFitForNode(node = {}, chunk = null) {
  const socketMask = normalizeStringArray(node.socketMask || []);
  const role = String(node.role || "");
  const openSides = normalizeStringArray(chunk?.openSides || []);
  const roleTags = normalizeStringArray(chunk?.roleTags || []);
  const compatibleRoles = compatibleRoleTagsForRole(role);
  let exactSocketMatch = !socketMask.length;
  let rotation = 0;
  let bestOverlap = overlapCount(socketMask, openSides);
  for (let turns = 0; turns < 4; turns += 1) {
    const rotatedSides = rotateSides(openSides, turns);
    const rotatedOverlap = overlapCount(socketMask, rotatedSides);
    if (rotatedOverlap > bestOverlap) {
      bestOverlap = rotatedOverlap;
      rotation = turns;
    }
    if (socketMask.length && rotatedSides.length === socketMask.length && socketMask.every((side) => rotatedSides.includes(side))) {
      exactSocketMatch = true;
      rotation = turns;
      break;
    }
  }
  const roleCompatible = !role || !roleTags.length || compatibleRoles.some((entry) => roleTags.includes(entry)) || ["combat", "empty"].includes(role);
  const issues = [];
  if (!exactSocketMatch) issues.push("socket_mismatch");
  if (!roleCompatible) issues.push("role_mismatch");
  return {
    chunk,
    exactSocketMatch,
    roleCompatible,
    rotation,
    rotatedOpenSides: rotateSides(openSides, rotation),
    issues,
  };
}

export function selectChunkMatchForNode(node = {}, theme = "buried_temple", catalogOverride = null) {
  const socketMask = normalizeStringArray(node.socketMask || []);
  const role = String(node.role || "");
  const catalog = resolveChunkCatalog(theme, catalogOverride);
  const evaluated = catalog.map((chunk) => evaluateChunkFitForNode({ role, socketMask }, chunk));
  const exactCandidates = evaluated.filter((entry) => entry.exactSocketMatch && entry.roleCompatible);
  const roleCompatibleCandidates = evaluated.filter((entry) => entry.roleCompatible);
  const exact = chooseDeterministicCandidate(exactCandidates, node, role, socketMask);
  const roleFallback = chooseDeterministicCandidate(roleCompatibleCandidates, node, role, socketMask);
  const fallback = evaluated[0] || null;
  const selected = exact || roleFallback || fallback;
  const issues = [];
  if (selected && !selected.exactSocketMatch) {
    issues.push({
      severity: roleRequiresExactCompatibility(role) ? "error" : "warning",
      code: "socket_chunk_fallback",
      nodeId: String(node.id || ""),
      role,
      requiredSockets: socketMask,
      selectedChunkId: selected.chunk?.id || "",
      selectedOpenSides: normalizeStringArray(selected.chunk?.openSides || []),
      rotatedOpenSides: normalizeStringArray(selected.rotatedOpenSides || []),
    });
  }
  if (selected && !selected.roleCompatible) {
    issues.push({
      severity: roleRequiresExactCompatibility(role) ? "error" : "warning",
      code: "role_chunk_fallback",
      nodeId: String(node.id || ""),
      role,
      selectedChunkId: selected.chunk?.id || "",
      selectedRoleTags: normalizeStringArray(selected.chunk?.roleTags || []),
    });
  }
  return {
    chunk: selected?.chunk || null,
    exactSocketMatch: Boolean(selected?.exactSocketMatch),
    roleCompatible: Boolean(selected?.roleCompatible),
    rotation: Number(selected?.rotation || 0),
    rotatedOpenSides: normalizeStringArray(selected?.rotatedOpenSides || []),
    issues,
  };
}

export function selectChunkForNode(node = {}, theme = "buried_temple", catalogOverride = null) {
  return selectChunkMatchForNode(node, theme, catalogOverride).chunk;
}

export function firstChunkAnchor(chunk = null, preferredKinds = []) {
  if (!chunk || !Array.isArray(chunk.anchors)) return null;
  const normalizedKinds = normalizeStringArray(preferredKinds);
  return chunk.anchors.find((anchor) => normalizedKinds.includes(anchor.kind)) || chunk.anchors[0] || null;
}

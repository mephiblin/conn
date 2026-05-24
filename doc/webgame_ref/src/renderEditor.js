import {
  MAP_PROFILE_DEFINITIONS,
  mapProfileForFloor,
  resolveChunkCatalog,
  selectChunkMatchForNode,
} from "./mapGenerationData.js";
import { makeMap as buildGeneratedMapPreview, wallVariantForCell } from "./mapGeneration.js";
import { buildRoomGraph, summarizeRoomGraph, validateRoomGraph } from "./mapGraph.js";

const WORKBENCH_SIDES = ["north", "east", "south", "west"];
const WORKBENCH_ROLE_TAGS = ["start", "key", "boss", "combat", "guard", "side", "side_reward", "locked_gate", "reward"];
const WORKBENCH_ANCHOR_KINDS = ["generic", "junction", "corridor", "loot", "boss_spawn"];
const WORKBENCH_REQUIRED_ANCHOR_OPTIONS = ["start", "key", "boss", "stairs_down_01", "stairs_down_02", "final_stairs_03", "locked_gate", "side_reward"];

function escapeWorkbenchHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;");
}

function cloneWorkbenchValue(value) {
  return JSON.parse(JSON.stringify(value));
}

function normalizeWorkbenchStringList(values = [], allowedValues = null) {
  const allowed = allowedValues ? new Set(allowedValues) : null;
  return [...new Set((Array.isArray(values) ? values : [])
    .map((value) => String(value || "").trim())
    .filter((value) => value && (!allowed || allowed.has(value))))]
    .sort((left, right) => left.localeCompare(right));
}

function sanitizeWorkbenchAnchor(anchor = {}, fallback = {}) {
  return {
    id: String(anchor.id || fallback.id || "anchor").trim() || String(fallback.id || "anchor"),
    kind: WORKBENCH_ANCHOR_KINDS.includes(String(anchor.kind || fallback.kind || "generic"))
      ? String(anchor.kind || fallback.kind || "generic")
      : "generic",
    x: Math.max(0, Math.floor(Number(anchor.x ?? fallback.x ?? 0) || 0)),
    y: Math.max(0, Math.floor(Number(anchor.y ?? fallback.y ?? 0) || 0)),
  };
}

function sanitizeWorkbenchChunkOverride(baseChunk = {}, override = {}) {
  const anchors = Array.isArray(override.anchors) && override.anchors.length
    ? [sanitizeWorkbenchAnchor(override.anchors[0], (baseChunk.anchors || [])[0] || {})]
    : (Array.isArray(baseChunk.anchors) ? [sanitizeWorkbenchAnchor(baseChunk.anchors[0], (baseChunk.anchors || [])[0] || {})] : []);
  return {
    ...baseChunk,
    ...override,
    openSides: normalizeWorkbenchStringList(override.openSides ?? baseChunk.openSides, WORKBENCH_SIDES),
    doorSockets: normalizeWorkbenchStringList(override.doorSockets ?? baseChunk.doorSockets, WORKBENCH_SIDES),
    roleTags: normalizeWorkbenchStringList(override.roleTags ?? baseChunk.roleTags, WORKBENCH_ROLE_TAGS),
    anchors,
  };
}

function resolveWorkbenchChunkCatalog(state, profile) {
  const baseCatalog = resolveChunkCatalog(profile.theme);
  const overrides = state.editor.workbenchChunkOverrides && typeof state.editor.workbenchChunkOverrides === "object"
    ? state.editor.workbenchChunkOverrides
    : {};
  return baseCatalog.map((chunk) => {
    const override = overrides[chunk.id];
    return override ? sanitizeWorkbenchChunkOverride(chunk, override) : chunk;
  });
}

function sanitizeWorkbenchProfileOverride(baseProfile = {}, override = {}) {
  const nextCriticalPathMin = Math.max(2, Math.floor(Number(override.criticalPath?.min ?? baseProfile.criticalPath?.min ?? 4) || 4));
  const nextCriticalPathMax = Math.max(nextCriticalPathMin, Math.floor(Number(override.criticalPath?.max ?? baseProfile.criticalPath?.max ?? nextCriticalPathMin) || nextCriticalPathMin));
  const nextLoopMin = Math.max(0, Math.floor(Number(override.loopCount?.min ?? baseProfile.loopCount?.min ?? 0) || 0));
  const nextLoopMax = Math.max(nextLoopMin, Math.floor(Number(override.loopCount?.max ?? baseProfile.loopCount?.max ?? nextLoopMin) || nextLoopMin));
  return {
    floor: Math.max(1, Number(baseProfile.floor || 1)),
    profileId: String(baseProfile.profileId || ""),
    theme: String(baseProfile.theme || "buried_temple"),
    algorithm: String(override.algorithm || baseProfile.algorithm || "room_grid_chunks"),
    targetModuleCount: Math.max(5, Math.floor(Number(override.targetModuleCount ?? baseProfile.targetModuleCount ?? 8) || 8)),
    mergeChancePer1000: Math.max(0, Math.min(1000, Math.floor(Number(override.mergeChancePer1000 ?? baseProfile.mergeChancePer1000 ?? 0) || 0))),
    gridRoomSize: {
      width: Math.max(3, Math.floor(Number(override.gridRoomSize?.width ?? baseProfile.gridRoomSize?.width ?? 7) || 7)),
      height: Math.max(3, Math.floor(Number(override.gridRoomSize?.height ?? baseProfile.gridRoomSize?.height ?? 7) || 7)),
    },
    criticalPath: {
      min: nextCriticalPathMin,
      max: nextCriticalPathMax,
    },
    loopCount: {
      min: nextLoopMin,
      max: nextLoopMax,
    },
    sideBranchCount: Math.max(0, Math.floor(Number(override.sideBranchCount ?? baseProfile.sideBranchCount ?? 0) || 0)),
    requiredAnchors: normalizeWorkbenchStringList(override.requiredAnchors ?? baseProfile.requiredAnchors, WORKBENCH_REQUIRED_ANCHOR_OPTIONS),
  };
}

function countBy(values = []) {
  const counts = new Map();
  for (const value of values) {
    const key = String(value || "");
    counts.set(key, Number(counts.get(key) || 0) + 1);
  }
  return counts;
}

function formatCountMap(counts) {
  return [...counts.entries()]
    .filter(([, count]) => count > 0)
    .sort((left, right) => left[0].localeCompare(right[0]))
    .map(([key, count]) => `${key}:${count}`)
    .join(", ");
}

function summarizeGeneratedMap(map = null) {
  if (!map) return null;
  const walkableCount = Array.isArray(map.cells) ? map.cells.filter((cell) => cell.walkable).length : 0;
  const doorCount = map.doors ? Object.keys(map.doors).length : 0;
  const socketAssemblySummary = map.generation?.socketAssemblySummary || { issueCount: 0, errorCount: 0, warningCount: 0 };
  const qualitySummary = map.generation?.qualitySummary || { issueCount: 0, warningCount: 0, variantGroupCounts: [], chunkIdCounts: [] };
  return {
    algorithm: map.generation?.algorithm || "unknown_algorithm",
    moduleCount: Number(map.generation?.moduleCount || 0),
    walkableCount,
    doorCount,
    socketAssemblyIssueCount: Number(socketAssemblySummary.issueCount || 0),
    socketAssemblyErrorCount: Number(socketAssemblySummary.errorCount || 0),
    socketAssemblyWarningCount: Number(socketAssemblySummary.warningCount || 0),
    qualityIssueCount: Number(qualitySummary.issueCount || 0),
    qualityWarningCount: Number(qualitySummary.warningCount || 0),
    variantGroupCounts: Array.isArray(qualitySummary.variantGroupCounts) ? qualitySummary.variantGroupCounts : [],
    chunkIdCounts: Array.isArray(qualitySummary.chunkIdCounts) ? qualitySummary.chunkIdCounts : [],
  };
}

function summarizeMapCellsForCompare(map = null) {
  if (!map) return null;
  const cells = Array.isArray(map.cells) ? map.cells : [];
  return {
    walkableCount: cells.filter((cell) => cell.walkable).length,
    tileRoleCounts: countBy(cells.filter((cell) => cell.walkable).map((cell) => cell.tileRole || "unknown")),
    floorVariantCounts: countBy(cells.filter((cell) => cell.walkable).map((cell) => cell.floorVariant || "unknown")),
    decorTagCounts: countBy(cells.flatMap((cell) => Array.isArray(cell.decorTags) ? cell.decorTags : [])),
    doorCoordCounts: countBy(Object.keys(map.doors || {})),
  };
}

function summarizeVisualPass(map = null) {
  if (!map) return null;
  const cells = Array.isArray(map.cells) ? map.cells.filter((cell) => cell.walkable) : [];
  const wallVariants = [];
  for (const cell of cells) {
    for (const dir of ["north", "east", "south", "west"]) {
      wallVariants.push(wallVariantForCell(cell, dir));
    }
  }
  const decor = Array.isArray(map.decor) ? map.decor : [];
  return {
    tileRoleCounts: countBy(cells.map((cell) => cell.tileRole || "unknown")),
    floorVariantCounts: countBy(cells.map((cell) => cell.floorVariant || "unknown")),
    wallVariantCounts: countBy(wallVariants),
    floorMaterialCounts: countBy(cells.map((cell) => cell.floorMaterialId || cell.floorTexture || "unknown")),
    decorTagCounts: countBy(cells.flatMap((cell) => Array.isArray(cell.decorTags) ? cell.decorTags : [])),
    decorKindCounts: countBy(decor.map((entry) => entry.kind || "unknown")),
    decorCount: decor.length,
  };
}

function compareMaps(selectedMap = null, baselineMap = null) {
  const selected = summarizeMapCellsForCompare(selectedMap);
  const alternate = summarizeMapCellsForCompare(baselineMap);
  if (!selected || !alternate) return null;
  const selectedCells = new Map((selectedMap?.cells || []).map((cell) => [`${cell.x},${cell.y}`, cell]));
  const alternateCells = new Map((baselineMap?.cells || []).map((cell) => [`${cell.x},${cell.y}`, cell]));
  const selectedDoors = new Set(Object.keys(selectedMap?.doors || {}));
  const alternateDoors = new Set(Object.keys(baselineMap?.doors || {}));
  const coords = [...new Set([...selectedCells.keys(), ...alternateCells.keys()])].sort((left, right) => {
    const [leftX, leftY] = left.split(",").map(Number);
    const [rightX, rightY] = right.split(",").map(Number);
    return leftY - rightY || leftX - rightX;
  });
  const cellDiffs = coords.flatMap((coord) => {
    const selectedCell = selectedCells.get(coord);
    const alternateCell = alternateCells.get(coord);
    if (!selectedCell || !alternateCell) {
      return [{
        coord,
        kinds: ["presence"],
        selected: selectedCell ? "present" : "missing",
        baseline: alternateCell ? "present" : "missing",
      }];
    }
    const kinds = [];
    if (Boolean(selectedCell.walkable) !== Boolean(alternateCell.walkable)) kinds.push("walkable");
    if ((selectedCell.tileRole || "") !== (alternateCell.tileRole || "")) kinds.push("tileRole");
    if ((selectedCell.floorVariant || "") !== (alternateCell.floorVariant || "")) kinds.push("floorVariant");
    const selectedDecor = (Array.isArray(selectedCell.decorTags) ? selectedCell.decorTags : []).join("/");
    const alternateDecor = (Array.isArray(alternateCell.decorTags) ? alternateCell.decorTags : []).join("/");
    if (selectedDecor !== alternateDecor) kinds.push("decor");
    const selectedDoor = selectedDoors.has(coord);
    const alternateDoor = alternateDoors.has(coord);
    if (selectedDoor !== alternateDoor) kinds.push("door");
    if (!kinds.length) return [];
    return [{
      coord,
      kinds,
      selected: `walkable ${selectedCell.walkable ? "1" : "0"} · role ${selectedCell.tileRole || "-"} · variant ${selectedCell.floorVariant || "-"} · decor ${selectedDecor || "-"} · door ${selectedDoor ? "1" : "0"}`,
      baseline: `walkable ${alternateCell.walkable ? "1" : "0"} · role ${alternateCell.tileRole || "-"} · variant ${alternateCell.floorVariant || "-"} · decor ${alternateDecor || "-"} · door ${alternateDoor ? "1" : "0"}`,
    }];
  });
  return {
    selected,
    alternate,
    cellDiffs,
  };
}

function buildWorkbenchCompareSnapshot(preview, state) {
  const graphDiffCount = Number(preview.graphCompare?.nodeDiffs?.length || 0);
  const cellDiffCount = Number(preview.cellCompare?.cellDiffs?.length || 0);
  return {
    id: `workbench_compare_${Date.now()}`,
    label: String(state.editor.workbenchCompareSnapshotLabel || "").trim() || `${preview.profile.profileId} · seed ${preview.seed}`,
    createdAt: new Date().toISOString(),
    floor: preview.floor,
    profileId: preview.profile.profileId,
    algorithm: preview.algorithm,
    seed: preview.seed,
    batchCount: Math.max(1, Number(state.editor.workbenchBatchCount || 8)),
    profileOverride: state.editor.workbenchProfileOverrides?.[preview.profile.profileId]
      ? cloneWorkbenchValue(state.editor.workbenchProfileOverrides[preview.profile.profileId])
      : null,
    chunkOverrides: state.editor.workbenchChunkOverrides && typeof state.editor.workbenchChunkOverrides === "object"
      ? cloneWorkbenchValue(state.editor.workbenchChunkOverrides)
      : {},
    baseline: preview.baselineGraphMeta ? {
      profileId: preview.baselineGraphMeta.profileId || "",
      algorithm: preview.baselineGraphMeta.algorithm || "",
      seed: Number(preview.baselineGraphMeta.seed || 0),
    } : null,
    selectedMapSummary: preview.selectedMapSummary ? JSON.parse(JSON.stringify(preview.selectedMapSummary)) : null,
    alternateMapSummary: preview.alternateMapSummary ? JSON.parse(JSON.stringify(preview.alternateMapSummary)) : null,
    graphDiffCount,
    cellDiffCount,
    socketIssueCount: Number(preview.socketIssues?.length || 0),
    validationIssueCount: Number(preview.issues?.length || 0),
  };
}

function summarizeChunkCatalogForPreview(chunkCatalog = [], graph = null) {
  const nodes = Array.isArray(graph?.nodes) ? graph.nodes : [];
  return chunkCatalog.map((chunk) => {
    const matchedNodes = nodes.filter((node) => node.chunkId === chunk.id);
    const exactNodes = matchedNodes.filter((node) => node.chunkMatch?.exactSocketMatch);
    const fallbackNodes = matchedNodes.filter((node) => !node.chunkMatch?.exactSocketMatch);
    return {
      ...chunk,
      matchedNodes,
      exactCount: exactNodes.length,
      fallbackCount: fallbackNodes.length,
    };
  }).sort((left, right) => {
    if (right.exactCount !== left.exactCount) return right.exactCount - left.exactCount;
    if (right.fallbackCount !== left.fallbackCount) return right.fallbackCount - left.fallbackCount;
    return String(left.id || "").localeCompare(String(right.id || ""));
  });
}

function summarizeProfileContract(profile = {}, graph = {}, summary = {}, issues = []) {
  const nodes = Array.isArray(graph?.nodes) ? graph.nodes : [];
  const sideBranchRootCount = nodes.filter((node) => Number(node.branchDepth || 0) === 1).length;
  const roles = new Set(nodes.map((node) => String(node.role || "")));
  const requiredAnchors = Array.isArray(profile.requiredAnchors) ? profile.requiredAnchors : [];
  const requiredAnchorStatus = requiredAnchors.map((anchor) => ({
    anchor,
    matched: roles.has(anchor) || (anchor.startsWith("stairs") && roles.has("stairs")),
  }));
  return {
    targetRoomCount: Number(profile.targetModuleCount || 0),
    actualRoomCount: Number(summary.roomCount || 0),
    criticalPathMin: Number(profile.criticalPath?.min || 0),
    criticalPathMax: Number(profile.criticalPath?.max || 0),
    actualCriticalPath: Number(summary.criticalPathLength || 0),
    mergeMin: Number(profile.loopCount?.min || 0),
    mergeMax: Number(profile.loopCount?.max || 0),
    actualMergeCount: Number(summary.mergeEdgeCount || 0),
    sideBranchTarget: Number(profile.sideBranchCount || 0),
    actualSideBranchRootCount: sideBranchRootCount,
    requiredAnchorStatus,
    issueCodes: issues.map((issue) => issue.code || "").filter(Boolean),
  };
}

function summarizeGraphForCompare(graph = null) {
  if (!graph) return null;
  const nodes = Array.isArray(graph.nodes) ? graph.nodes : [];
  const edges = Array.isArray(graph.edges) ? graph.edges : [];
  return {
    roomCount: nodes.length,
    roleCounts: countBy(nodes.map((node) => node.role || "empty")),
    edgeTypeCounts: countBy(edges.map((edge) => edge.type || "main")),
    socketDegreeCounts: countBy(nodes.map((node) => String((node.socketMask || []).length))),
  };
}

function compareGraphs(selectedGraph = null, alternateGraph = null) {
  const selected = summarizeGraphForCompare(selectedGraph);
  const alternate = summarizeGraphForCompare(alternateGraph);
  if (!selected || !alternate) return null;
  const selectedNodes = new Map((selectedGraph?.nodes || []).map((node) => [node.id, node]));
  const alternateNodes = new Map((alternateGraph?.nodes || []).map((node) => [node.id, node]));
  const ids = [...new Set([...selectedNodes.keys(), ...alternateNodes.keys()])].sort();
  const nodeDiffs = ids.flatMap((id) => {
    const current = selectedNodes.get(id);
    const other = alternateNodes.get(id);
    if (!current || !other) {
      return [{
        id,
        kind: "presence",
        selected: current ? "present" : "missing",
        alternate: other ? "present" : "missing",
      }];
    }
    const diffs = [];
    if (current.role !== other.role) {
      diffs.push({ id, kind: "role", selected: current.role || "-", alternate: other.role || "-" });
    }
    if (current.x !== other.x || current.y !== other.y) {
      diffs.push({ id, kind: "coord", selected: `${current.x},${current.y}`, alternate: `${other.x},${other.y}` });
    }
    const currentSockets = (current.socketMask || []).join("/");
    const alternateSockets = (other.socketMask || []).join("/");
    if (currentSockets !== alternateSockets) {
      diffs.push({ id, kind: "socket", selected: currentSockets || "-", alternate: alternateSockets || "-" });
    }
    return diffs;
  });
  return {
    selected,
    alternate,
    nodeDiffs,
  };
}

function normalizeWorkbenchFloor(state) {
  return Math.max(1, Number(state.editor.workbenchFloor || state.player?.floor || 1));
}

function profileForWorkbenchState(state) {
  const baseProfile = MAP_PROFILE_DEFINITIONS.find((entry) => entry.profileId === state.editor.workbenchProfileId)
    || mapProfileForFloor(normalizeWorkbenchFloor(state));
  const overrides = state.editor.workbenchProfileOverrides && typeof state.editor.workbenchProfileOverrides === "object"
    ? state.editor.workbenchProfileOverrides
    : {};
  const override = overrides[baseProfile.profileId] || null;
  return override ? {
    ...baseProfile,
    ...sanitizeWorkbenchProfileOverride(baseProfile, override),
  } : baseProfile;
}

function seedForWorkbenchState(state, fallback = 18422) {
  const parsed = Number(state.editor.workbenchSeed);
  return Number.isFinite(parsed) && parsed > 0 ? Math.floor(parsed) : fallback;
}

function buildWorkbenchPreview(state) {
  const profile = profileForWorkbenchState(state);
  const floor = Math.max(1, Number(profile.floor || normalizeWorkbenchFloor(state)));
  const fallbackSeed = Number(state.map?.generation?.seed || 18422);
  const seed = seedForWorkbenchState(state, fallbackSeed);
  const algorithm = state.editor.workbenchAlgorithm === "block_modules_and_connectors" ? "block_modules_and_connectors" : "room_grid_chunks";
  const chunkCatalog = resolveWorkbenchChunkCatalog(state, profile);
  const graph = buildRoomGraph(profile, seed);
  const socketIssues = [];
  const nodes = graph.nodes.map((node) => {
    const chunkMatch = selectChunkMatchForNode(node, profile.theme, chunkCatalog);
    socketIssues.push(...chunkMatch.issues);
    const chunk = chunkMatch.chunk;
    return {
      ...node,
      chunkId: chunk?.id || "",
      presetId: chunk?.presetId || "",
      anchors: Array.isArray(chunk?.anchors) ? chunk.anchors : [],
      chunkMatch: {
        exactSocketMatch: Boolean(chunkMatch.exactSocketMatch),
        roleCompatible: Boolean(chunkMatch.roleCompatible),
        rotation: Number(chunkMatch.rotation || 0),
        rotatedOpenSides: [...(chunkMatch.rotatedOpenSides || [])],
      },
    };
  });
  const enrichedGraph = { ...graph, nodes };
  const issues = [...validateRoomGraph(profile, enrichedGraph), ...socketIssues];
  const summary = summarizeRoomGraph(enrichedGraph, issues);
  const profileContract = summarizeProfileContract(profile, enrichedGraph, summary, issues);
  const alternateAlgorithm = algorithm === "room_grid_chunks" ? "block_modules_and_connectors" : "room_grid_chunks";
  const selectedMap = buildGeneratedMapPreview(floor, seed, {
    profileOverride: profile,
    generationAlgorithm: algorithm,
    chunkCatalogOverride: chunkCatalog,
  });
  const alternateMap = buildGeneratedMapPreview(floor, seed, {
    profileOverride: profile,
    generationAlgorithm: alternateAlgorithm,
    chunkCatalogOverride: chunkCatalog,
  });
  const selectedMapSummary = summarizeGeneratedMap(selectedMap);
  const alternateMapSummary = summarizeGeneratedMap(alternateMap);
  const selectedVisualSummary = summarizeVisualPass(selectedMap);
  const generationIssues = [
    ...((selectedMap?.generation?.qualityIssues || []).map((issue) => ({ ...issue, source: "quality" }))),
  ];
  const baselineMap = state.map || null;
  const baselineGraph = state.map?.generation?.roomGraph || null;
  const graphCompare = compareGraphs(enrichedGraph, baselineGraph);
  const cellCompare = compareMaps(selectedMap, baselineMap);
  return {
    floor,
    profile,
    seed,
    algorithm,
    graph: enrichedGraph,
    issues,
    summary,
    profileContract,
    socketIssues,
    selectedMapSummary,
    selectedVisualSummary,
    generationIssues,
    alternateAlgorithm,
    alternateMapSummary,
    graphCompare,
    cellCompare,
    baselineGraphMeta: baselineGraph ? {
      profileId: state.map?.generation?.profileId || "",
      algorithm: state.map?.generation?.algorithm || "",
      seed: Number(state.map?.generation?.seed || 0),
    } : null,
    chunkCatalog,
    chunkCatalogSummary: summarizeChunkCatalogForPreview(chunkCatalog, enrichedGraph),
  };
}

export function bindEditorFrame(deps = {}) {
  const {
    getState = () => ({}),
    render = () => {},
    documentObject = document,
    randomMapSeed = () => 0,
    makeMap = () => ({}),
    addLog = () => {},
    startTestPlaySession = () => ({ ok: false, failures: [] }),
    firstValidationIssue = () => null,
    validationIssueRepairHint = () => "",
    validationSummaryText = () => "",
    saveEditorProject = () => {},
    loadEditorProject = () => {},
  } = deps;
  const state = getState();
  const getWorkbenchProfileOverrides = () => (
    state.editor.workbenchProfileOverrides && typeof state.editor.workbenchProfileOverrides === "object"
      ? state.editor.workbenchProfileOverrides
      : {}
  );
  const activeWorkbenchBaseProfile = () => (
    MAP_PROFILE_DEFINITIONS.find((entry) => entry.profileId === state.editor.workbenchProfileId)
    || mapProfileForFloor(normalizeWorkbenchFloor(state))
  );
  const updateWorkbenchProfileOverride = (updater) => {
    const baseProfile = activeWorkbenchBaseProfile();
    const currentOverrides = getWorkbenchProfileOverrides();
    const currentOverride = currentOverrides[baseProfile.profileId] ? cloneWorkbenchValue(currentOverrides[baseProfile.profileId]) : {};
    const nextOverride = updater(currentOverride, baseProfile) || currentOverride;
    state.editor.workbenchProfileOverrides = {
      ...currentOverrides,
      [baseProfile.profileId]: sanitizeWorkbenchProfileOverride(baseProfile, nextOverride),
    };
    state.editor.workbenchBatchSummary = null;
  };
  const defaultWorkbenchChunkId = () => {
    const profile = profileForWorkbenchState(state);
    return String(resolveWorkbenchChunkCatalog(state, profile)[0]?.id || "");
  };
  const getWorkbenchChunkOverrides = () => (
    state.editor.workbenchChunkOverrides && typeof state.editor.workbenchChunkOverrides === "object"
      ? state.editor.workbenchChunkOverrides
      : {}
  );
  const selectedWorkbenchChunkId = () => String(state.editor.selectedWorkbenchChunkId || defaultWorkbenchChunkId());
  const updateWorkbenchChunkOverride = (chunkId, updater) => {
    if (!chunkId) return;
    const profile = profileForWorkbenchState(state);
    const baseChunk = resolveChunkCatalog(profile.theme).find((entry) => entry.id === chunkId);
    if (!baseChunk) return;
    const currentOverrides = getWorkbenchChunkOverrides();
    const currentOverride = currentOverrides[chunkId] ? cloneWorkbenchValue(currentOverrides[chunkId]) : {};
    const nextOverride = updater(currentOverride, baseChunk) || currentOverride;
    const sanitized = sanitizeWorkbenchChunkOverride(baseChunk, nextOverride);
    state.editor.workbenchChunkOverrides = {
      ...currentOverrides,
      [chunkId]: {
        openSides: sanitized.openSides,
        doorSockets: sanitized.doorSockets,
        roleTags: sanitized.roleTags,
        anchors: sanitized.anchors,
      },
    };
    state.editor.workbenchBatchSummary = null;
  };

  const applyWorkbenchMapToEditor = () => {
    const preview = buildWorkbenchPreview(state);
    state.editor.workbenchFloor = preview.floor;
    state.editor.workbenchProfileId = preview.profile.profileId;
    state.editor.workbenchSeed = String(preview.seed);
    const nextMap = makeMap(preview.floor, preview.seed, {
      profileOverride: preview.profile,
      generationAlgorithm: preview.algorithm,
      chunkCatalogOverride: preview.chunkCatalog,
    });
    nextMap.generation.algorithm = preview.algorithm;
    state.floorMaps[preview.floor] = nextMap;
    state.map = nextMap;
    state.player.floor = preview.floor;
    state.player.x = nextMap.start.x;
    state.player.y = nextMap.start.y;
    state.player.facing = nextMap.start.facing;
    state.visitedByFloor[preview.floor] = new Set([`${nextMap.start.x},${nextMap.start.y}`]);
    state.visited = state.visitedByFloor[preview.floor];
    state.editor.editorWorkspaceMode = "legacy_cell_editor";
    addLog(`${nextMap.name} generator workbench preview를 legacy cell editor floor로 적용했다. seed ${preview.seed} · profile ${preview.profile.profileId} · algorithm ${preview.algorithm}`);
    return nextMap;
  };

  const updateWorkbenchProfile = (profileId) => {
    const profile = MAP_PROFILE_DEFINITIONS.find((entry) => entry.profileId === profileId) || mapProfileForFloor(normalizeWorkbenchFloor(state));
    state.editor.workbenchProfileId = profile.profileId;
    state.editor.workbenchFloor = profile.floor;
    const nextCatalog = resolveWorkbenchChunkCatalog(state, profile);
    const selectedId = selectedWorkbenchChunkId();
    state.editor.selectedWorkbenchChunkId = nextCatalog.some((chunk) => chunk.id === selectedId) ? selectedId : String(nextCatalog[0]?.id || "");
  };

  const workbenchCompareSnapshots = Array.isArray(state.editor.workbenchCompareSnapshots)
    ? state.editor.workbenchCompareSnapshots
    : [];
  const syncWorkbenchSessionForProject = () => {
    const preview = buildWorkbenchPreview(state);
    state.editor.workbenchFloor = preview.floor;
    state.editor.workbenchProfileId = preview.profile.profileId;
    state.editor.workbenchAlgorithm = preview.algorithm;
    state.editor.workbenchSeed = String(preview.seed);
    if (!state.editor.selectedWorkbenchChunkId) {
      state.editor.selectedWorkbenchChunkId = String(preview.chunkCatalog[0]?.id || "");
    }
  };

  if (documentObject.getElementById("openLegacyCellEditorBtn")) {
    documentObject.getElementById("openLegacyCellEditorBtn").onclick = () => {
      state.editor.editorWorkspaceMode = "legacy_cell_editor";
      render();
    };
  }
  if (documentObject.getElementById("openGeneratorWorkbenchBtn")) {
    documentObject.getElementById("openGeneratorWorkbenchBtn").onclick = () => {
      state.editor.editorWorkspaceMode = "generator_workbench";
      if (!state.editor.workbenchProfileId) {
        const profile = mapProfileForFloor(normalizeWorkbenchFloor(state));
        state.editor.workbenchProfileId = profile.profileId;
      }
      if (!state.editor.workbenchSeed) state.editor.workbenchSeed = String(state.map?.generation?.seed || randomMapSeed());
      if (!state.editor.selectedWorkbenchChunkId) state.editor.selectedWorkbenchChunkId = defaultWorkbenchChunkId();
      render();
    };
  }
  if (documentObject.getElementById("workbenchProfileSelect")) {
    documentObject.getElementById("workbenchProfileSelect").onchange = (e) => {
      updateWorkbenchProfile(e.target.value || "");
      state.editor.workbenchBatchSummary = null;
      render();
    };
  }
  if (documentObject.getElementById("workbenchAlgorithmSelect")) {
    documentObject.getElementById("workbenchAlgorithmSelect").onchange = (e) => {
      state.editor.workbenchAlgorithm = e.target.value === "block_modules_and_connectors" ? "block_modules_and_connectors" : "room_grid_chunks";
      state.editor.workbenchBatchSummary = null;
      render();
    };
  }
  if (documentObject.getElementById("workbenchSeedInput")) {
    documentObject.getElementById("workbenchSeedInput").oninput = (e) => {
      state.editor.workbenchSeed = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("workbenchBatchCountInput")) {
    documentObject.getElementById("workbenchBatchCountInput").oninput = (e) => {
      state.editor.workbenchBatchCount = Math.max(1, Number(e.target.value || 1));
      render();
    };
  }
  if (documentObject.getElementById("workbenchCompareSnapshotLabelInput")) {
    documentObject.getElementById("workbenchCompareSnapshotLabelInput").oninput = (e) => {
      state.editor.workbenchCompareSnapshotLabel = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("saveWorkbenchProjectBtn")) {
    documentObject.getElementById("saveWorkbenchProjectBtn").onclick = () => {
      syncWorkbenchSessionForProject();
      saveEditorProject();
    };
  }
  if (documentObject.getElementById("loadWorkbenchProjectBtn")) {
    documentObject.getElementById("loadWorkbenchProjectBtn").onclick = loadEditorProject;
  }
  if (documentObject.getElementById("workbenchCompareSnapshotSelect")) {
    documentObject.getElementById("workbenchCompareSnapshotSelect").onchange = (e) => {
      state.editor.selectedWorkbenchCompareSnapshotId = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("workbenchChunkSelect")) {
    documentObject.getElementById("workbenchChunkSelect").onchange = (e) => {
      state.editor.selectedWorkbenchChunkId = e.target.value || "";
      render();
    };
  }
  [
    ["workbenchTargetRoomCountInput", "targetModuleCount"],
    ["workbenchMergeChanceInput", "mergeChancePer1000"],
    ["workbenchSideBranchCountInput", "sideBranchCount"],
  ].forEach(([id, field]) => {
    const element = documentObject.getElementById(id);
    if (!element) return;
    element.oninput = (e) => {
      const value = Math.floor(Number(e.target.value || 0) || 0);
      updateWorkbenchProfileOverride((override) => ({ ...override, [field]: value }));
      render();
    };
  });
  [
    ["workbenchRoomWidthInput", "gridRoomSize", "width"],
    ["workbenchRoomHeightInput", "gridRoomSize", "height"],
    ["workbenchCriticalPathMinInput", "criticalPath", "min"],
    ["workbenchCriticalPathMaxInput", "criticalPath", "max"],
    ["workbenchLoopCountMinInput", "loopCount", "min"],
    ["workbenchLoopCountMaxInput", "loopCount", "max"],
  ].forEach(([id, field, subField]) => {
    const element = documentObject.getElementById(id);
    if (!element) return;
    element.oninput = (e) => {
      const value = Math.floor(Number(e.target.value || 0) || 0);
      updateWorkbenchProfileOverride((override, baseProfile) => ({
        ...override,
        [field]: {
          ...(baseProfile[field] || {}),
          ...(override[field] || {}),
          [subField]: value,
        },
      }));
      render();
    };
  });
  documentObject.querySelectorAll("[data-workbench-required-anchor]").forEach((input) => {
    input.onchange = (e) => {
      const anchor = e.target.dataset.workbenchRequiredAnchor || "";
      updateWorkbenchProfileOverride((override, baseProfile) => {
        const next = new Set(normalizeWorkbenchStringList(override.requiredAnchors ?? baseProfile.requiredAnchors, WORKBENCH_REQUIRED_ANCHOR_OPTIONS));
        if (e.target.checked) next.add(anchor);
        else next.delete(anchor);
        return { ...override, requiredAnchors: [...next] };
      });
      render();
    };
  });
  if (documentObject.getElementById("resetWorkbenchProfileOverrideBtn")) {
    documentObject.getElementById("resetWorkbenchProfileOverrideBtn").onclick = () => {
      const baseProfile = activeWorkbenchBaseProfile();
      const currentOverrides = getWorkbenchProfileOverrides();
      if (!currentOverrides[baseProfile.profileId]) {
        addLog("reset할 profile override가 없다.");
        return render();
      }
      const nextOverrides = { ...currentOverrides };
      delete nextOverrides[baseProfile.profileId];
      state.editor.workbenchProfileOverrides = nextOverrides;
      state.editor.workbenchBatchSummary = null;
      addLog(`generator workbench profile override를 초기화했다. ${baseProfile.profileId}`);
      render();
    };
  }
  if (documentObject.getElementById("clearWorkbenchProfileOverridesBtn")) {
    documentObject.getElementById("clearWorkbenchProfileOverridesBtn").onclick = () => {
      state.editor.workbenchProfileOverrides = {};
      state.editor.workbenchBatchSummary = null;
      addLog("generator workbench profile override 목록을 비웠다.");
      render();
    };
  }
  if (documentObject.getElementById("randomWorkbenchSeedBtn")) {
    documentObject.getElementById("randomWorkbenchSeedBtn").onclick = () => {
      state.editor.workbenchSeed = String(randomMapSeed());
      state.editor.workbenchBatchSummary = null;
      render();
    };
  }
  if (documentObject.getElementById("refreshWorkbenchPreviewBtn")) {
    documentObject.getElementById("refreshWorkbenchPreviewBtn").onclick = () => {
      const preview = buildWorkbenchPreview(state);
      state.editor.workbenchFloor = preview.floor;
      state.editor.workbenchProfileId = preview.profile.profileId;
      state.editor.workbenchSeed = String(preview.seed);
      state.editor.workbenchBatchSummary = null;
      addLog(`generator workbench preview를 갱신했다. floor ${preview.floor} · seed ${preview.seed} · profile ${preview.profile.profileId}`);
      render();
    };
  }
  documentObject.querySelectorAll("[data-workbench-chunk-side]").forEach((input) => {
    input.onchange = (e) => {
      const chunkId = e.target.dataset.workbenchChunkId || selectedWorkbenchChunkId();
      const field = e.target.dataset.workbenchChunkField || "openSides";
      const side = e.target.dataset.workbenchChunkSide || "";
      updateWorkbenchChunkOverride(chunkId, (override, baseChunk) => {
        const baseValues = field === "doorSockets" ? baseChunk.doorSockets : baseChunk.openSides;
        const currentValues = override[field] ?? baseValues;
        const nextValues = new Set(normalizeWorkbenchStringList(currentValues, WORKBENCH_SIDES));
        if (e.target.checked) nextValues.add(side);
        else nextValues.delete(side);
        return { ...override, [field]: [...nextValues] };
      });
      render();
    };
  });
  documentObject.querySelectorAll("[data-workbench-chunk-role-tag]").forEach((input) => {
    input.onchange = (e) => {
      const chunkId = e.target.dataset.workbenchChunkId || selectedWorkbenchChunkId();
      const roleTag = e.target.dataset.workbenchChunkRoleTag || "";
      updateWorkbenchChunkOverride(chunkId, (override, baseChunk) => {
        const currentValues = override.roleTags ?? baseChunk.roleTags;
        const nextValues = new Set(normalizeWorkbenchStringList(currentValues, WORKBENCH_ROLE_TAGS));
        if (e.target.checked) nextValues.add(roleTag);
        else nextValues.delete(roleTag);
        return { ...override, roleTags: [...nextValues] };
      });
      render();
    };
  });
  if (documentObject.getElementById("workbenchChunkAnchorKindSelect")) {
    documentObject.getElementById("workbenchChunkAnchorKindSelect").onchange = (e) => {
      const chunkId = selectedWorkbenchChunkId();
      updateWorkbenchChunkOverride(chunkId, (override, baseChunk) => {
        const anchor = sanitizeWorkbenchAnchor(
          { ...(override.anchors || [])[0], kind: e.target.value || "generic" },
          (baseChunk.anchors || [])[0] || {}
        );
        return { ...override, anchors: [anchor] };
      });
      render();
    };
  }
  ["Id", "X", "Y"].forEach((suffix) => {
    const element = documentObject.getElementById(`workbenchChunkAnchor${suffix}Input`);
    if (!element) return;
    element.oninput = (e) => {
      const chunkId = selectedWorkbenchChunkId();
      updateWorkbenchChunkOverride(chunkId, (override, baseChunk) => {
        const anchor = sanitizeWorkbenchAnchor((override.anchors || [])[0] || {}, (baseChunk.anchors || [])[0] || {});
        if (suffix === "Id") anchor.id = String(e.target.value || "").trim() || anchor.id;
        if (suffix === "X") anchor.x = Math.max(0, Math.floor(Number(e.target.value || 0) || 0));
        if (suffix === "Y") anchor.y = Math.max(0, Math.floor(Number(e.target.value || 0) || 0));
        return { ...override, anchors: [anchor] };
      });
      render();
    };
  });
  if (documentObject.getElementById("resetWorkbenchChunkOverrideBtn")) {
    documentObject.getElementById("resetWorkbenchChunkOverrideBtn").onclick = () => {
      const chunkId = selectedWorkbenchChunkId();
      const currentOverrides = getWorkbenchChunkOverrides();
      if (!chunkId || !currentOverrides[chunkId]) {
        addLog("reset할 chunk override가 없다.");
        return render();
      }
      const nextOverrides = { ...currentOverrides };
      delete nextOverrides[chunkId];
      state.editor.workbenchChunkOverrides = nextOverrides;
      state.editor.workbenchBatchSummary = null;
      addLog(`generator workbench chunk override를 초기화했다. ${chunkId}`);
      render();
    };
  }
  if (documentObject.getElementById("clearWorkbenchChunkOverridesBtn")) {
    documentObject.getElementById("clearWorkbenchChunkOverridesBtn").onclick = () => {
      state.editor.workbenchChunkOverrides = {};
      state.editor.workbenchBatchSummary = null;
      addLog("generator workbench chunk override 목록을 비웠다.");
      render();
    };
  }
  if (documentObject.getElementById("analyzeWorkbenchBatchBtn")) {
    documentObject.getElementById("analyzeWorkbenchBatchBtn").onclick = () => {
      const profile = profileForWorkbenchState(state);
      const baseSeed = seedForWorkbenchState(state, Number(state.map?.generation?.seed || randomMapSeed()));
      const count = Math.max(1, Number(state.editor.workbenchBatchCount || 8));
      const algorithm = state.editor.workbenchAlgorithm === "block_modules_and_connectors" ? "block_modules_and_connectors" : "room_grid_chunks";
      const chunkCatalog = resolveWorkbenchChunkCatalog(state, profile);
      const batch = [];
      for (let offset = 0; offset < count; offset += 1) {
        const seed = baseSeed + offset;
        const graph = buildRoomGraph(profile, seed);
        const issues = validateRoomGraph(profile, graph);
        const summary = summarizeRoomGraph(graph, issues);
        const generatedMap = makeMap(profile.floor, seed, {
          profileOverride: profile,
          generationAlgorithm: algorithm,
          chunkCatalogOverride: chunkCatalog,
        });
        const mapSummary = summarizeGeneratedMap(generatedMap);
        const visualSummary = summarizeVisualPass(generatedMap);
        const nodeDemand = (generatedMap.generation?.roomGraph?.nodes || []).map((node) => ({
          role: String(node.role || ""),
          socketMask: Array.isArray(node.socketMask) ? node.socketMask.join("/") : "",
          chunkId: String(node.chunkId || ""),
          exact: Boolean(node.chunkMatch?.exactSocketMatch),
        }));
        const chunkUsage = countBy((generatedMap.generation?.roomGraph?.nodes || []).map((node) => node.chunkId || ""));
        const fallbackPatterns = (generatedMap.generation?.socketAssemblyIssues || []).map((issue) => ({
          code: issue.code || "",
          role: issue.role || "",
          requiredSockets: Array.isArray(issue.requiredSockets) ? issue.requiredSockets.join("/") : "",
          selectedChunkId: issue.selectedChunkId || "",
        }));
        const qualityIssues = (generatedMap.generation?.qualityIssues || []).map((issue) => ({
          code: issue.code || "",
          variantGroup: issue.variantGroup || "",
          chunkId: issue.chunkId || "",
          count: Number(issue.count || 0),
        }));
        batch.push({
          seed,
          summary,
          algorithm,
          mapSummary,
          nodeDemand,
          chunkUsage: formatCountMap(chunkUsage),
          fallbackPatterns,
          qualityIssues,
          visualSummary: {
            decorCount: Number(visualSummary?.decorCount || 0),
            wallVariantCounts: visualSummary?.wallVariantCounts ? formatCountMap(visualSummary.wallVariantCounts) : "",
          },
        });
      }
      const totalErrors = batch.reduce((sum, entry) => sum + Number(entry.summary.errorCount || 0), 0);
      const totalWarnings = batch.reduce((sum, entry) => sum + Number(entry.summary.warningCount || 0), 0);
      const totalSocketIssues = batch.reduce((sum, entry) => sum + Number(entry.mapSummary?.socketAssemblyIssueCount || 0), 0);
      const zeroSocketIssueSeeds = batch.filter((entry) => Number(entry.mapSummary?.socketAssemblyIssueCount || 0) === 0).length;
      const usedChunkIds = new Set();
      const fallbackPatternCounts = new Map();
      const socketDemandCounts = new Map();
      const qualityPatternCounts = new Map();
      batch.forEach((entry) => {
        String(entry.chunkUsage || "").split(", ").filter(Boolean).forEach((token) => {
          const [chunkId] = token.split(":");
          if (chunkId) usedChunkIds.add(chunkId);
        });
        (entry.nodeDemand || []).forEach((demand) => {
          const key = `${demand.role}|${demand.socketMask}|${demand.chunkId}|${demand.exact ? "exact" : "fallback"}`;
          socketDemandCounts.set(key, Number(socketDemandCounts.get(key) || 0) + 1);
        });
        (entry.fallbackPatterns || []).forEach((pattern) => {
          const key = `${pattern.code}|${pattern.role}|${pattern.requiredSockets}|${pattern.selectedChunkId}`;
          fallbackPatternCounts.set(key, Number(fallbackPatternCounts.get(key) || 0) + 1);
        });
        (entry.qualityIssues || []).forEach((issue) => {
          const key = `${issue.code}|${issue.variantGroup}|${issue.chunkId}|${issue.count}`;
          qualityPatternCounts.set(key, Number(qualityPatternCounts.get(key) || 0) + 1);
        });
      });
      state.editor.workbenchBatchSummary = {
        profileId: profile.profileId,
        baseSeed,
        count,
        algorithm,
        totalErrors,
        totalWarnings,
        totalSocketIssues,
        zeroSocketIssueSeeds,
        usedChunkIds: [...usedChunkIds].sort(),
        unusedChunkIds: chunkCatalog.map((entry) => entry.id).filter((id) => !usedChunkIds.has(id)),
        socketDemandCounts: [...socketDemandCounts.entries()]
          .map(([key, usageCount]) => {
            const [role, socketMask, chunkId, exactState] = key.split("|");
            return { role, socketMask, chunkId, exactState, usageCount };
          })
          .sort((left, right) => right.usageCount - left.usageCount),
        fallbackPatternCounts: [...fallbackPatternCounts.entries()]
          .map(([key, usageCount]) => {
            const [code, role, requiredSockets, selectedChunkId] = key.split("|");
            return { code, role, requiredSockets, selectedChunkId, usageCount };
          })
          .sort((left, right) => right.usageCount - left.usageCount),
        qualityPatternCounts: [...qualityPatternCounts.entries()]
          .map(([key, usageCount]) => {
            const [code, variantGroup, chunkId, countValue] = key.split("|");
            return { code, variantGroup, chunkId, count: Number(countValue || 0), usageCount };
          })
          .sort((left, right) => right.usageCount - left.usageCount || right.count - left.count),
        entries: batch,
      };
      addLog(`generator workbench batch 분석 ${count}개를 실행했다. base seed ${baseSeed} · graph error ${totalErrors} · graph warning ${totalWarnings} · socket issue ${totalSocketIssues}`);
      render();
    };
  }
  if (documentObject.getElementById("saveWorkbenchCompareSnapshotBtn")) {
    documentObject.getElementById("saveWorkbenchCompareSnapshotBtn").onclick = () => {
      const preview = buildWorkbenchPreview(state);
      const snapshot = buildWorkbenchCompareSnapshot(preview, state);
      state.editor.workbenchCompareSnapshots = [snapshot, ...workbenchCompareSnapshots].slice(0, 8);
      state.editor.selectedWorkbenchCompareSnapshotId = snapshot.id;
      state.editor.workbenchCompareSnapshotLabel = snapshot.label;
      addLog(`generator workbench compare snapshot을 저장했다. ${snapshot.label} · seed ${snapshot.seed} · graph diff ${snapshot.graphDiffCount} · cell diff ${snapshot.cellDiffCount}`);
      render();
    };
  }
  if (documentObject.getElementById("restoreWorkbenchCompareSnapshotBtn")) {
    documentObject.getElementById("restoreWorkbenchCompareSnapshotBtn").onclick = () => {
      const snapshot = workbenchCompareSnapshots.find((entry) => entry.id === state.editor.selectedWorkbenchCompareSnapshotId);
      if (!snapshot) {
        addLog("복원할 workbench compare snapshot이 없다.");
        return render();
      }
      state.editor.workbenchFloor = Math.max(1, Number(snapshot.floor || state.editor.workbenchFloor || 1));
      state.editor.workbenchProfileId = String(snapshot.profileId || "");
      state.editor.workbenchAlgorithm = snapshot.algorithm === "block_modules_and_connectors" ? "block_modules_and_connectors" : "room_grid_chunks";
      state.editor.workbenchSeed = String(snapshot.seed || "");
      state.editor.workbenchBatchCount = Math.max(1, Number(snapshot.batchCount || state.editor.workbenchBatchCount || 8));
      {
        const nextProfileOverrides = {
          ...(state.editor.workbenchProfileOverrides && typeof state.editor.workbenchProfileOverrides === "object" ? state.editor.workbenchProfileOverrides : {}),
        };
        const snapshotProfileId = String(snapshot.profileId || "");
        if (snapshot.profileOverride) nextProfileOverrides[snapshotProfileId] = cloneWorkbenchValue(snapshot.profileOverride);
        else delete nextProfileOverrides[snapshotProfileId];
        state.editor.workbenchProfileOverrides = nextProfileOverrides;
      }
      state.editor.workbenchChunkOverrides = snapshot.chunkOverrides && typeof snapshot.chunkOverrides === "object"
        ? cloneWorkbenchValue(snapshot.chunkOverrides)
        : {};
      state.editor.workbenchBatchSummary = null;
      state.editor.workbenchCompareSnapshotLabel = String(snapshot.label || "");
      addLog(`generator workbench compare snapshot을 복원했다. ${snapshot.label} · seed ${snapshot.seed} · algorithm ${snapshot.algorithm}`);
      render();
    };
  }
  if (documentObject.getElementById("clearWorkbenchCompareSnapshotsBtn")) {
    documentObject.getElementById("clearWorkbenchCompareSnapshotsBtn").onclick = () => {
      state.editor.workbenchCompareSnapshots = [];
      state.editor.selectedWorkbenchCompareSnapshotId = "";
      addLog("generator workbench compare snapshot 목록을 비웠다.");
      render();
    };
  }
  if (documentObject.getElementById("applyWorkbenchMapBtn")) {
    documentObject.getElementById("applyWorkbenchMapBtn").onclick = () => {
      applyWorkbenchMapToEditor();
      render();
    };
  }
  if (documentObject.getElementById("applyWorkbenchMapAndTestBtn")) {
    documentObject.getElementById("applyWorkbenchMapAndTestBtn").onclick = () => {
      const nextMap = applyWorkbenchMapToEditor();
      const result = startTestPlaySession();
      if (!result.ok) {
        const firstFailure = result.failures[0];
        const firstIssue = firstValidationIssue(firstFailure?.report);
        const issueText = firstIssue?.message ? ` · 먼저 볼 항목: ${firstIssue.message}` : "";
        const repairHint = firstIssue ? ` · 힌트: ${validationIssueRepairHint(firstIssue)}` : "";
        addLog(`${nextMap.name} workbench test play 차단: 층 ${firstFailure.floor} ${validationSummaryText(firstFailure.report)}${issueText}${repairHint}`);
        return render();
      }
      addLog(`${nextMap.name} generator workbench 결과로 compiledMap 테스트 플레이를 시작한다.`);
      render();
    };
  }

  if (documentObject.getElementById("eventExportArchiveQueryInput")) {
    documentObject.getElementById("eventExportArchiveQueryInput").oninput = (e) => {
      state.editor.eventExportArchiveQuery = e.target.value || "";
      state.editor.selectedEventExportArchiveId = "";
      state.editor.selectedEventExportArchiveBundleRowId = "";
      state.editor.selectedEventExportArchiveBundleRowIds = [];
      render();
    };
  }
  if (documentObject.getElementById("eventExportArchiveBatchShareLabelInput")) {
    documentObject.getElementById("eventExportArchiveBatchShareLabelInput").oninput = (e) => {
      state.editor.eventExportArchiveBatchShareLabel = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("eventExportArchiveBatchShareLinkInput")) {
    documentObject.getElementById("eventExportArchiveBatchShareLinkInput").oninput = (e) => {
      state.editor.eventExportArchiveBatchShareLinkDraft = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("eventExportArchiveEntrySelect")) {
    documentObject.getElementById("eventExportArchiveEntrySelect").onchange = (e) => {
      state.editor.selectedEventExportArchiveId = e.target.value || "";
      state.editor.selectedEventExportArchiveBundleRowId = "";
      state.editor.selectedEventExportArchiveBundleRowIds = [];
      state.editor.selectedEventExportArchiveFieldKeys = [];
      state.editor.selectedEventExportArchiveStepIds = [];
      state.editor.selectedEventExportArchiveStepPartKeys = [];
      state.editor.selectedEventExportArchiveStepItemKeys = [];
      render();
    };
  }
  if (documentObject.getElementById("eventExportArchiveBundleRowSelect")) {
    documentObject.getElementById("eventExportArchiveBundleRowSelect").onchange = (e) => {
      state.editor.selectedEventExportArchiveBundleRowId = e.target.value || "";
      state.editor.selectedEventExportArchiveBundleRowIds = state.editor.selectedEventExportArchiveBundleRowId ? [state.editor.selectedEventExportArchiveBundleRowId] : [];
      state.editor.selectedEventExportArchiveFieldKeys = [];
      state.editor.selectedEventExportArchiveStepIds = [];
      state.editor.selectedEventExportArchiveStepPartKeys = [];
      state.editor.selectedEventExportArchiveStepItemKeys = [];
      render();
    };
  }
  documentObject.querySelectorAll("[data-event-export-bundle-row-id]").forEach((input) => {
    input.onchange = () => {
      const next = Array.from(documentObject.querySelectorAll("[data-event-export-bundle-row-id]:checked"))
        .map((node) => node.getAttribute("data-event-export-bundle-row-id") || "")
        .filter(Boolean);
      state.editor.selectedEventExportArchiveBundleRowIds = next;
      state.editor.selectedEventExportArchiveBundleRowId = next[0] || "";
      render();
    };
  });
  documentObject.querySelectorAll("[data-event-export-field-key]").forEach((input) => {
    input.onchange = () => {
      const next = Array.from(documentObject.querySelectorAll("[data-event-export-field-key]:checked"))
        .map((node) => node.getAttribute("data-event-export-field-key") || "")
        .filter(Boolean);
      state.editor.selectedEventExportArchiveFieldKeys = next;
      render();
    };
  });
  documentObject.querySelectorAll("[data-event-export-step-id]").forEach((input) => {
    input.onchange = () => {
      const next = Array.from(documentObject.querySelectorAll("[data-event-export-step-id]:checked"))
        .map((node) => node.getAttribute("data-event-export-step-id") || "")
        .filter(Boolean);
      state.editor.selectedEventExportArchiveStepIds = next;
      render();
    };
  });
  documentObject.querySelectorAll("[data-event-export-step-part-key]").forEach((input) => {
    input.onchange = () => {
      const next = Array.from(documentObject.querySelectorAll("[data-event-export-step-part-key]:checked"))
        .map((node) => node.getAttribute("data-event-export-step-part-key") || "")
        .filter(Boolean);
      state.editor.selectedEventExportArchiveStepPartKeys = next;
      render();
    };
  });
  documentObject.querySelectorAll("[data-event-export-step-item-key]").forEach((input) => {
    input.onchange = () => {
      const next = Array.from(documentObject.querySelectorAll("[data-event-export-step-item-key]:checked"))
        .map((node) => node.getAttribute("data-event-export-step-item-key") || "")
        .filter(Boolean);
      state.editor.selectedEventExportArchiveStepItemKeys = next;
      render();
    };
  });
  documentObject.querySelectorAll("[data-event-export-select-all]").forEach((button) => {
    button.onclick = () => {
      const group = button.getAttribute("data-event-export-select-all") || "";
      if (group === "bundle-rows") state.editor.selectedEventExportArchiveBundleRowIds = Array.from(documentObject.querySelectorAll("[data-event-export-bundle-row-id]"))
        .map((node) => node.getAttribute("data-event-export-bundle-row-id") || "")
        .filter(Boolean);
      if (group === "fields") state.editor.selectedEventExportArchiveFieldKeys = Array.from(documentObject.querySelectorAll("[data-event-export-field-key]"))
        .map((node) => node.getAttribute("data-event-export-field-key") || "")
        .filter(Boolean);
      if (group === "steps") state.editor.selectedEventExportArchiveStepIds = Array.from(documentObject.querySelectorAll("[data-event-export-step-id]"))
        .map((node) => node.getAttribute("data-event-export-step-id") || "")
        .filter(Boolean);
      if (group === "step-parts") state.editor.selectedEventExportArchiveStepPartKeys = Array.from(documentObject.querySelectorAll("[data-event-export-step-part-key]"))
        .map((node) => node.getAttribute("data-event-export-step-part-key") || "")
        .filter(Boolean);
      if (group === "step-items") state.editor.selectedEventExportArchiveStepItemKeys = Array.from(documentObject.querySelectorAll("[data-event-export-step-item-key]"))
        .map((node) => node.getAttribute("data-event-export-step-item-key") || "")
        .filter(Boolean);
      if (group === "bundle-rows" && state.editor.selectedEventExportArchiveBundleRowIds.length) {
        state.editor.selectedEventExportArchiveBundleRowId = state.editor.selectedEventExportArchiveBundleRowIds[0];
      }
      render();
    };
  });
  documentObject.querySelectorAll("[data-event-export-clear-all]").forEach((button) => {
    button.onclick = () => {
      const group = button.getAttribute("data-event-export-clear-all") || "";
      if (group === "bundle-rows") {
        state.editor.selectedEventExportArchiveBundleRowIds = [];
        state.editor.selectedEventExportArchiveBundleRowId = "";
      }
      if (group === "fields") state.editor.selectedEventExportArchiveFieldKeys = [];
      if (group === "steps") state.editor.selectedEventExportArchiveStepIds = [];
      if (group === "step-parts") state.editor.selectedEventExportArchiveStepPartKeys = [];
      if (group === "step-items") state.editor.selectedEventExportArchiveStepItemKeys = [];
      render();
    };
  });
  if (documentObject.getElementById("eventBundlePatchArchiveQueryInput")) {
    documentObject.getElementById("eventBundlePatchArchiveQueryInput").oninput = (e) => {
      state.editor.eventBundlePatchArchiveQuery = e.target.value || "";
      state.editor.eventBundlePatchArchiveEntryId = "";
      render();
    };
  }
  if (documentObject.getElementById("eventBundlePatchArchiveEntrySelect")) {
    documentObject.getElementById("eventBundlePatchArchiveEntrySelect").onchange = (e) => {
      state.editor.eventBundlePatchArchiveEntryId = e.target.value || "";
      render();
    };
  }
}

export function bindNpcPlacementEditorControls(deps = {}) {
  const {
    selectedNpcPlacement = null,
    state,
    render = () => {},
    documentObject = document,
    toggleRequiredPlacementContract = () => {},
    isRequiredNpcPlacement = () => false,
    addLog = () => {},
  } = deps;

  if (!selectedNpcPlacement) return;

  documentObject.getElementById("npcPlacementSelect").onchange = (e) => {
    state.selectedNpcPlacementId = e.target.value;
    render();
  };
  documentObject.getElementById("npcPlacementDefinitionSelect").onchange = (e) => {
    selectedNpcPlacement.npcId = e.target.value;
    selectedNpcPlacement.refType = "npc";
    selectedNpcPlacement.refId = e.target.value;
    render();
  };
  documentObject.getElementById("npcPlacementNoteInput").oninput = (e) => {
    selectedNpcPlacement.note = e.target.value.trim();
  };
  documentObject.getElementById("clearNpcPlacementNoteBtn").onclick = () => {
    delete selectedNpcPlacement.note;
    render();
  };
  documentObject.getElementById("toggleRequiredNpcPlacementBtn").onclick = () => {
    toggleRequiredPlacementContract(state.map, selectedNpcPlacement.id, "npc");
    addLog(`${selectedNpcPlacement.id} NPC placement required target을 ${isRequiredNpcPlacement(state.map, selectedNpcPlacement.id) ? "표시" : "해제"}했다.`);
    render();
  };
}

export function bindNpcDefinitionEditorControls(deps = {}) {
  const {
    state,
    npcDefId = "",
    npcDef = null,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    parseJsonField = (value) => value,
    updateNpcDefinition = () => {},
    createNpcQuestHookTemplate = () => ({}),
    createQuestSeedTemplate = () => ({}),
    duplicateQuestSeedTemplate = (_, seed) => ({ ...seed }),
    loadNpcCustomPresets = () => [],
    saveNpcCustomPresets = () => {},
    buildNpcCustomPresetFromDefinition = () => ({}),
    defaultNpcPresetSelectionIndexes = () => [],
    defaultNpcPresetDialogueSelectionMap = () => ({}),
    defaultNpcPresetDialogueChoiceSelectionMap = () => ({}),
    defaultNpcPresetDialogueBranchSelectionMap = () => ({}),
    defaultNpcPresetServiceFieldSelectionMap = () => ({}),
    defaultNpcPresetSeedFieldSelectionMap = () => ({}),
    buildNpcCustomPresetDiff = () => ({ seedRows: [] }),
  } = deps;

  if (!npcDef) return;
  if (!documentObject.getElementById("npcNameInput")) return;

  const applySelectedNpcDefinition = (value) => {
    state.selectedNpcDefinitionId = value;
    state.selectedNpcQuestSeedIndex = 0;
    state.selectedNpcServiceIndex = 0;
    render();
  };

  const npcDefinitionSelect = documentObject.getElementById("npcDefinitionSelect");
  if (npcDefinitionSelect) {
    npcDefinitionSelect.onchange = (e) => {
      applySelectedNpcDefinition(e.target.value);
    };
  }
  const questEditorNpcDefinitionSelect = documentObject.getElementById("questEditorNpcDefinitionSelect");
  if (questEditorNpcDefinitionSelect) {
    questEditorNpcDefinitionSelect.onchange = (e) => {
      applySelectedNpcDefinition(e.target.value);
    };
  }
  documentObject.getElementById("npcNameInput").oninput = (e) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.name = e.target.value.trim();
    });
  };
  documentObject.getElementById("npcDescriptionInput").oninput = (e) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.description = e.target.value.trim();
    });
  };
  documentObject.getElementById("npcLogInput").oninput = (e) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.log = e.target.value.trim();
    });
  };
  documentObject.getElementById("npcProgressionHooksJsonInput").onchange = (e) => {
    try {
      const hooks = parseJsonField(e.target.value, {});
      if (!hooks || typeof hooks !== "object" || Array.isArray(hooks)) throw new Error("progressionHooks must be object");
      updateNpcDefinition(npcDefId, (entry) => {
        entry.progressionHooks = hooks;
      });
    } catch (error) {
      addLog(`npc progressionHooks JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  documentObject.getElementById("npcQuestHooksJsonInput").onchange = (e) => {
    try {
      const hooks = parseJsonField(e.target.value, []);
      if (!Array.isArray(hooks)) throw new Error("questHooks must be array");
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questHooks = hooks;
      });
      render();
    } catch (error) {
      addLog(`npc questHooks JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  if (documentObject.getElementById("addNpcQuestHookBtn")) {
    documentObject.getElementById("addNpcQuestHookBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questHooks = Array.isArray(entry.questHooks) ? entry.questHooks : [];
        entry.questHooks.push(createNpcQuestHookTemplate());
      });
      render();
    };
  }
  documentObject.querySelectorAll("[data-npc-quest-hook-bosses]").forEach((input) => {
    input.onchange = (e) => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questHooks = Array.isArray(entry.questHooks) ? entry.questHooks : [];
        const index = Number(e.target.dataset.npcQuestHookBosses || 0);
        if (index < 0 || index >= entry.questHooks.length) return;
        entry.questHooks[index].bossesDefeatedAtLeast = Math.max(0, Number(e.target.value || 0));
      });
    };
  });
  documentObject.querySelectorAll("[data-npc-quest-hook-note]").forEach((input) => {
    input.onchange = (e) => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questHooks = Array.isArray(entry.questHooks) ? entry.questHooks : [];
        const index = Number(e.target.dataset.npcQuestHookNote || 0);
        if (index < 0 || index >= entry.questHooks.length) return;
        entry.questHooks[index].note = e.target.value.trim();
      });
    };
  });
  documentObject.querySelectorAll("[data-remove-npc-quest-hook]").forEach((button) => {
    button.onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questHooks = Array.isArray(entry.questHooks) ? entry.questHooks : [];
        const index = Number(button.dataset.removeNpcQuestHook || 0);
        if (index < 0 || index >= entry.questHooks.length) return;
        entry.questHooks.splice(index, 1);
      });
      render();
    };
  });
  documentObject.getElementById("npcQuestSeedsJsonInput").onchange = (e) => {
    try {
      const seeds = parseJsonField(e.target.value, []);
      if (!Array.isArray(seeds)) throw new Error("questSeeds must be array");
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questSeeds = seeds;
      });
      state.selectedNpcQuestSeedIndex = Math.min(Math.max(0, Number(state.selectedNpcQuestSeedIndex || 0)), Math.max(0, seeds.length - 1));
      render();
    } catch (error) {
      addLog(`npc questSeeds JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  if (documentObject.getElementById("addNpcQuestSeedBtn")) {
    documentObject.getElementById("addNpcQuestSeedBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
        entry.questSeeds.push(createQuestSeedTemplate(entry));
        state.selectedNpcQuestSeedIndex = entry.questSeeds.length - 1;
      });
      render();
    };
  }
  if (documentObject.getElementById("removeNpcQuestSeedBtn")) {
    documentObject.getElementById("removeNpcQuestSeedBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcQuestSeedIndex || 0)), entry.questSeeds.length - 1);
        if (index < 0 || !entry.questSeeds[index]) return;
        entry.questSeeds.splice(index, 1);
        state.selectedNpcQuestSeedIndex = Math.min(index, Math.max(0, entry.questSeeds.length - 1));
      });
      render();
    };
  }
  if (documentObject.getElementById("duplicateNpcQuestSeedBtn")) {
    documentObject.getElementById("duplicateNpcQuestSeedBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcQuestSeedIndex || 0)), entry.questSeeds.length - 1);
        if (index < 0 || !entry.questSeeds[index]) return;
        const clone = duplicateQuestSeedTemplate(entry, entry.questSeeds[index]);
        entry.questSeeds.splice(index + 1, 0, clone);
        state.selectedNpcQuestSeedIndex = index + 1;
      });
      render();
    };
  }
  if (documentObject.getElementById("moveNpcQuestSeedUpBtn")) {
    documentObject.getElementById("moveNpcQuestSeedUpBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcQuestSeedIndex || 0)), entry.questSeeds.length - 1);
        if (index <= 0 || !entry.questSeeds[index]) return;
        [entry.questSeeds[index - 1], entry.questSeeds[index]] = [entry.questSeeds[index], entry.questSeeds[index - 1]];
        state.selectedNpcQuestSeedIndex = index - 1;
      });
      render();
    };
  }
  if (documentObject.getElementById("moveNpcQuestSeedDownBtn")) {
    documentObject.getElementById("moveNpcQuestSeedDownBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.questSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcQuestSeedIndex || 0)), entry.questSeeds.length - 1);
        if (index < 0 || index >= entry.questSeeds.length - 1 || !entry.questSeeds[index]) return;
        [entry.questSeeds[index], entry.questSeeds[index + 1]] = [entry.questSeeds[index + 1], entry.questSeeds[index]];
        state.selectedNpcQuestSeedIndex = index + 1;
      });
      render();
    };
  }
  documentObject.getElementById("npcServicesJsonInput").onchange = (e) => {
    try {
      const services = parseJsonField(e.target.value, []);
      if (!Array.isArray(services)) throw new Error("services must be array");
      updateNpcDefinition(npcDefId, (entry) => {
        entry.services = services;
      });
      state.selectedNpcServiceIndex = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), Math.max(0, services.length - 1));
      state.selectedNpcDialogueStepIndex = 0;
      render();
    } catch (error) {
      addLog(`npc services JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  if (documentObject.getElementById("npcCustomPresetSelect")) {
    documentObject.getElementById("npcCustomPresetSelect").onchange = (e) => {
      state.editor.selectedNpcCustomPresetId = e.target.value || "";
      state.editor.selectedNpcCustomPresetMergePatchDraft = "";
      const preset = loadNpcCustomPresets().find((entry) => entry.id === state.editor.selectedNpcCustomPresetId);
      state.editor.selectedNpcCustomPresetServiceIndexes = defaultNpcPresetSelectionIndexes(preset?.services || []);
      state.editor.selectedNpcCustomPresetSeedIndexes = defaultNpcPresetSelectionIndexes(preset?.questSeeds || []);
      state.editor.selectedNpcCustomPresetDialogueStepSelections = defaultNpcPresetDialogueSelectionMap(preset);
      state.editor.selectedNpcCustomPresetDialogueChoiceSelections = defaultNpcPresetDialogueChoiceSelectionMap(preset);
      state.editor.selectedNpcCustomPresetDialogueBranchSelections = defaultNpcPresetDialogueBranchSelectionMap(preset);
      state.editor.selectedNpcCustomPresetServiceFieldSelections = defaultNpcPresetServiceFieldSelectionMap(buildNpcCustomPresetDiff(npcDef, preset));
      state.editor.selectedNpcCustomPresetSeedFieldSelections = defaultNpcPresetSeedFieldSelectionMap(buildNpcCustomPresetDiff(npcDef, preset));
      render();
    };
  }
  if (documentObject.getElementById("npcCustomPresetApplyModeSelect")) {
    documentObject.getElementById("npcCustomPresetApplyModeSelect").onchange = (e) => {
      state.editor.selectedNpcCustomPresetApplyMode = e.target.value === "append" ? "append" : "replace";
      state.editor.selectedNpcCustomPresetMergePatchDraft = "";
      render();
    };
  }
  if (documentObject.getElementById("npcCustomPresetConflictModeSelect")) {
    documentObject.getElementById("npcCustomPresetConflictModeSelect").onchange = (e) => {
      state.editor.selectedNpcCustomPresetConflictMode = e.target.value === "keep_current" ? "keep_current" : "preset_wins";
      state.editor.selectedNpcCustomPresetMergePatchDraft = "";
      render();
    };
  }
  if (documentObject.getElementById("saveNpcCustomPresetBtn")) {
    documentObject.getElementById("saveNpcCustomPresetBtn").onclick = () => {
      const presets = loadNpcCustomPresets();
      const nextPreset = buildNpcCustomPresetFromDefinition(npcDefId, npcDef, presets);
      presets.push(nextPreset);
      saveNpcCustomPresets(presets);
      state.editor.selectedNpcCustomPresetId = nextPreset.id;
      state.editor.selectedNpcCustomPresetServiceIndexes = defaultNpcPresetSelectionIndexes(nextPreset.services || []);
      state.editor.selectedNpcCustomPresetSeedIndexes = defaultNpcPresetSelectionIndexes(nextPreset.questSeeds || []);
      state.editor.selectedNpcCustomPresetDialogueStepSelections = defaultNpcPresetDialogueSelectionMap(nextPreset);
      state.editor.selectedNpcCustomPresetDialogueChoiceSelections = defaultNpcPresetDialogueChoiceSelectionMap(nextPreset);
      state.editor.selectedNpcCustomPresetDialogueBranchSelections = defaultNpcPresetDialogueBranchSelectionMap(nextPreset);
      state.editor.selectedNpcCustomPresetServiceFieldSelections = defaultNpcPresetServiceFieldSelectionMap(buildNpcCustomPresetDiff(npcDef, nextPreset));
      state.editor.selectedNpcCustomPresetSeedFieldSelections = defaultNpcPresetSeedFieldSelectionMap(buildNpcCustomPresetDiff(npcDef, nextPreset));
      state.editor.selectedNpcCustomPresetMergePatchDraft = "";
      addLog(`${nextPreset.name} NPC preset을 저장했다.`);
      render();
    };
  }
}

export function bindNpcQuestSeedEditorControls(deps = {}) {
  const {
    state,
    items = {},
    selectedNpcQuestSeedDef = null,
    render = () => {},
    documentObject = document,
    updateNpcDefinition = () => {},
    npcDefId = "",
    questRewardFlagValueType = () => "string",
  } = deps;

  if (!selectedNpcQuestSeedDef) return;

  const updateSelectedNpcQuestSeed = (updater) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.questSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
      const index = Math.min(Math.max(0, Number(state.selectedNpcQuestSeedIndex || 0)), entry.questSeeds.length - 1);
      if (!entry.questSeeds[index]) return;
      updater(entry.questSeeds[index]);
    });
    render();
  };

  documentObject.getElementById("npcQuestSeedSelect").onchange = (e) => {
    state.selectedNpcQuestSeedIndex = Math.max(0, Number(e.target.value || 0));
    render();
  };
  documentObject.getElementById("npcQuestSeedIdInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.id = e.target.value.trim();
    });
  };
  documentObject.getElementById("npcQuestSeedTitleInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.title = e.target.value.trim();
    });
  };
  documentObject.getElementById("npcQuestSeedNoteInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.note = e.target.value.trim();
    });
  };
  documentObject.getElementById("npcQuestSeedObjectivesInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.objectives = e.target.value
        .split("\n")
        .map((entry) => entry.trim())
        .filter(Boolean);
    });
  };
  if (documentObject.getElementById("addNpcQuestObjectiveBtn")) {
    documentObject.getElementById("addNpcQuestObjectiveBtn").onclick = () => {
      updateSelectedNpcQuestSeed((seed) => {
        seed.objectives = Array.isArray(seed.objectives) ? seed.objectives : [];
        seed.objectives.push("새 목표를 입력한다.");
      });
    };
  }
  documentObject.querySelectorAll("[data-npc-quest-objective-input]").forEach((input) => {
    input.onchange = (e) => {
      updateSelectedNpcQuestSeed((seed) => {
        seed.objectives = Array.isArray(seed.objectives) ? seed.objectives : [];
        const index = Number(e.target.dataset.npcQuestObjectiveInput || 0);
        if (index < 0 || index >= seed.objectives.length) return;
        seed.objectives[index] = e.target.value.trim();
      });
    };
  });
  documentObject.querySelectorAll("[data-remove-npc-quest-objective]").forEach((button) => {
    button.onclick = () => {
      updateSelectedNpcQuestSeed((seed) => {
        seed.objectives = Array.isArray(seed.objectives) ? seed.objectives : [];
        const index = Number(button.dataset.removeNpcQuestObjective || 0);
        if (index < 0 || index >= seed.objectives.length) return;
        seed.objectives.splice(index, 1);
      });
    };
  });
  documentObject.getElementById("npcQuestSeedRewardGoldInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.rewards = seed.rewards || {};
      seed.rewards.gold = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("npcQuestSeedRewardXpInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.rewards = seed.rewards || {};
      seed.rewards.xp = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("npcQuestSeedRewardItemsInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.rewards = seed.rewards || {};
      seed.rewards.items = e.target.value
        .split(",")
        .map((entry) => entry.trim())
        .filter(Boolean)
        .map((entry) => {
          const [itemId, quantityText] = entry.split(":").map((value) => value.trim());
          const quantity = Math.max(1, Number(quantityText || 1));
          return quantity > 1 ? { itemId, quantity } : { itemId, quantity: 1 };
        });
    });
  };
  if (documentObject.getElementById("addNpcQuestRewardItemBtn")) {
    documentObject.getElementById("addNpcQuestRewardItemBtn").onclick = () => {
      updateSelectedNpcQuestSeed((seed) => {
        seed.rewards = seed.rewards || {};
        seed.rewards.items = Array.isArray(seed.rewards.items) ? seed.rewards.items : [];
        const firstItemId = Object.keys(items)[0] || "";
        seed.rewards.items.push({ itemId: firstItemId, quantity: 1 });
      });
    };
  }
  documentObject.querySelectorAll("[data-npc-quest-reward-item-id]").forEach((select) => {
    select.onchange = (e) => {
      updateSelectedNpcQuestSeed((seed) => {
        seed.rewards = seed.rewards || {};
        seed.rewards.items = Array.isArray(seed.rewards.items) ? seed.rewards.items : [];
        const index = Number(e.target.dataset.npcQuestRewardItemId || 0);
        if (index < 0 || index >= seed.rewards.items.length) return;
        seed.rewards.items[index] = {
          ...(seed.rewards.items[index] || {}),
          itemId: e.target.value,
          quantity: Math.max(1, Number(seed.rewards.items[index]?.quantity || 1)),
        };
      });
    };
  });
  documentObject.querySelectorAll("[data-npc-quest-reward-item-qty]").forEach((input) => {
    input.onchange = (e) => {
      updateSelectedNpcQuestSeed((seed) => {
        seed.rewards = seed.rewards || {};
        seed.rewards.items = Array.isArray(seed.rewards.items) ? seed.rewards.items : [];
        const index = Number(e.target.dataset.npcQuestRewardItemQty || 0);
        if (index < 0 || index >= seed.rewards.items.length) return;
        seed.rewards.items[index] = {
          ...(seed.rewards.items[index] || {}),
          quantity: Math.max(1, Number(e.target.value || 1)),
        };
      });
    };
  });
  documentObject.querySelectorAll("[data-remove-npc-quest-reward-item]").forEach((button) => {
    button.onclick = () => {
      updateSelectedNpcQuestSeed((seed) => {
        seed.rewards = seed.rewards || {};
        seed.rewards.items = Array.isArray(seed.rewards.items) ? seed.rewards.items : [];
        const index = Number(button.dataset.removeNpcQuestRewardItem || 0);
        if (index < 0 || index >= seed.rewards.items.length) return;
        seed.rewards.items.splice(index, 1);
      });
    };
  });
  documentObject.getElementById("npcQuestSeedRewardFlagInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.rewards = seed.rewards || {};
      if (e.target.value.trim()) seed.rewards.flag = e.target.value.trim();
      else delete seed.rewards.flag;
    });
  };
  const applyNpcQuestRewardFlagValue = () => {
    const type = documentObject.getElementById("npcQuestSeedRewardFlagValueTypeSelect").value;
    const rawValue = documentObject.getElementById("npcQuestSeedRewardFlagValueInput").value;
    updateSelectedNpcQuestSeed((seed) => {
      seed.rewards = seed.rewards || {};
      if (type === "boolean_true") seed.rewards.value = true;
      else if (type === "boolean_false") seed.rewards.value = false;
      else if (type === "number") seed.rewards.value = Number(rawValue || 0);
      else seed.rewards.value = rawValue;
    });
  };
  documentObject.getElementById("npcQuestSeedRewardFlagValueTypeSelect").onchange = applyNpcQuestRewardFlagValue;
  documentObject.getElementById("npcQuestSeedRewardFlagValueInput").onchange = applyNpcQuestRewardFlagValue;
  documentObject.getElementById("npcQuestSeedFailureFlagInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      if (e.target.value.trim()) seed.failureFlag = e.target.value.trim();
      else delete seed.failureFlag;
    });
  };
  documentObject.getElementById("npcQuestSeedGrantFlagInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      if (e.target.value.trim()) seed.grantFlag = e.target.value.trim();
      else delete seed.grantFlag;
    });
  };
  documentObject.getElementById("npcQuestSeedCompleteEventIdInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      if (e.target.value.trim()) seed.completeEventId = e.target.value.trim();
      else delete seed.completeEventId;
    });
  };
  documentObject.getElementById("npcQuestSeedBossFloorInput").onchange = (e) => {
    updateSelectedNpcQuestSeed((seed) => {
      seed.bossesDefeatedAtLeast = Math.max(0, Number(e.target.value || 0));
    });
  };
}

export function bindEventDefinitionEditorControls(deps = {}) {
  const {
    state,
    eventTool = "",
    eventDef = null,
    eventDefId = "",
    eventPlacementKind = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    parseJsonField = (value) => value,
    updateEventDefinition = () => {},
    activeEventDefinitionId = () => "",
    stopEditorEventTestSession = () => {},
    renameEventPreset = () => false,
    createEventPresetFromDefinition = () => "",
    defaultInteractionTypeForPlacementKind = () => "interact",
    createEventGraphTemplate = () => null,
    uniqueEventStepId = () => "step_start",
  } = deps;

  if (!eventDef) return;
  if (!documentObject.getElementById("eventInspectorToolSelect")) return;

  documentObject.getElementById("eventInspectorToolSelect").onchange = (e) => {
    state.eventInspectorTool = e.target.value;
    render();
  };
  documentObject.getElementById("eventDefinitionSelect").onchange = (e) => {
    stopEditorEventTestSession();
    state.selectedEventDefinitionIds[eventTool] = e.target.value;
    state.selectedEventStepIndex = 0;
    render();
  };
  documentObject.getElementById("eventPresetIdInput").onchange = (e) => {
    const nextId = e.target.value.trim();
    if (!nextId) return render();
    if (!renameEventPreset(eventDefId, nextId)) addLog(`event preset ID를 ${nextId}(으)로 바꿀 수 없다.`);
    render();
  };
  documentObject.getElementById("eventPresetNameInput").oninput = (e) => {
    updateEventDefinition(activeEventDefinitionId(), (event) => {
      event.name = e.target.value.trim();
    });
  };
  documentObject.getElementById("eventTypeInput").oninput = (e) => {
    updateEventDefinition(activeEventDefinitionId(), (event) => {
      event.type = e.target.value.trim();
    });
  };
  documentObject.getElementById("eventInteractionSelect").onchange = (e) => {
    updateEventDefinition(activeEventDefinitionId(), (event) => {
      event.interaction = e.target.value;
    });
    render();
  };
  documentObject.getElementById("duplicateEventPresetBtn").onclick = () => {
    stopEditorEventTestSession();
    const newId = createEventPresetFromDefinition(eventDef, `${eventDefId}_copy`, eventTool, `${eventDef.name || eventDefId} 사본`);
    addLog(`${newId} event preset을 복제했다.`);
    state.selectedEventStepIndex = 0;
    render();
  };
  documentObject.getElementById("newEventPresetBtn").onclick = () => {
    stopEditorEventTestSession();
    const baseInteraction = defaultInteractionTypeForPlacementKind(eventPlacementKind);
    const newId = createEventPresetFromDefinition({
      name: `${eventPlacementKind} 새 이벤트`,
      type: `${eventPlacementKind}_custom`,
      interaction: baseInteraction,
      usage: { mode: "repeat" },
      effects: [],
    }, `${eventPlacementKind}_custom`, eventTool, `${eventPlacementKind} 새 이벤트`);
    addLog(`${newId} event preset을 만들었다.`);
    state.selectedEventStepIndex = 0;
    render();
  };
  const applyEventGraphTemplate = (templateId) => {
    const template = createEventGraphTemplate(templateId, eventDefId, eventTool);
    if (!template) return;
    stopEditorEventTestSession();
    updateEventDefinition(eventDefId, (event) => {
      if (template.name) event.name = template.name;
      if (template.interaction) event.interaction = template.interaction;
      event.effects = JSON.parse(JSON.stringify(template.effects || []));
      event.steps = JSON.parse(JSON.stringify(template.steps || []));
      if (template.entryStepId) event.entryStepId = template.entryStepId;
      else if (event.steps[0]?.id) event.entryStepId = event.steps[0].id;
    });
    state.selectedEventStepIndex = 0;
    addLog(`${eventDefId}에 ${templateId} graph template를 적용했다.`);
    render();
  };
  if (documentObject.getElementById("applyAltarChoiceTemplateBtn")) {
    documentObject.getElementById("applyAltarChoiceTemplateBtn").onclick = () => applyEventGraphTemplate("altar_choice");
  }
  if (documentObject.getElementById("applyTrapResolutionTemplateBtn")) {
    documentObject.getElementById("applyTrapResolutionTemplateBtn").onclick = () => applyEventGraphTemplate("trap_resolution");
  }
  if (documentObject.getElementById("applyNpcHandoffTemplateBtn")) {
    documentObject.getElementById("applyNpcHandoffTemplateBtn").onclick = () => applyEventGraphTemplate("npc_handoff");
  }
  if (documentObject.getElementById("applyBossGateTemplateBtn")) {
    documentObject.getElementById("applyBossGateTemplateBtn").onclick = () => applyEventGraphTemplate("boss_gate");
  }
  documentObject.getElementById("eventUsageModeSelect").onchange = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      event.usage = event.usage || {};
      event.usage.mode = e.target.value;
      if (event.usage.mode !== "uses") delete event.usage.usesRemaining;
      if (event.usage.mode !== "cooldown") delete event.usage.cooldownSteps;
      if (event.usage.mode === "uses" && typeof event.usage.usesRemaining !== "number") event.usage.usesRemaining = 1;
      if (event.usage.mode === "cooldown" && typeof event.usage.cooldownSteps !== "number") event.usage.cooldownSteps = 3;
    });
    render();
  };
  documentObject.getElementById("eventUsesRemainingInput").oninput = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      event.usage = event.usage || {};
      event.usage.usesRemaining = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("eventCooldownStepsInput").oninput = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      event.usage = event.usage || {};
      event.usage.cooldownSteps = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("eventEffectsJsonInput").onchange = (e) => {
    try {
      const effects = parseJsonField(e.target.value, []);
      if (!Array.isArray(effects)) throw new Error("effects must be array");
      updateEventDefinition(eventDefId, (event) => {
        event.effects = effects;
      });
      render();
    } catch (error) {
      addLog(`effects JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  documentObject.getElementById("eventEntryStepIdInput").oninput = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      const nextId = e.target.value.trim();
      if (!nextId) delete event.entryStepId;
      else event.entryStepId = nextId;
    });
  };
  documentObject.getElementById("eventStepsJsonInput").onchange = (e) => {
    try {
      const steps = parseJsonField(e.target.value, []);
      if (!Array.isArray(steps)) throw new Error("steps must be array");
      stopEditorEventTestSession();
      updateEventDefinition(eventDefId, (event) => {
        event.steps = steps;
        if (!steps.length) delete event.entryStepId;
        else if (!event.entryStepId && steps[0]?.id) event.entryStepId = steps[0].id;
      });
      state.selectedEventStepIndex = Math.min(Math.max(0, Number(state.selectedEventStepIndex || 0)), Math.max(0, steps.length - 1));
      render();
    } catch (error) {
      addLog(`event steps JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  documentObject.getElementById("promoteEventEffectsToGraphBtn").onclick = () => {
    let promotedId = "";
    updateEventDefinition(eventDefId, (event) => {
      if (Array.isArray(event.steps) && event.steps.length) return;
      promotedId = uniqueEventStepId(event, "step_start");
      event.steps = [{
        id: promotedId,
        title: event.name || eventDefId,
        effects: JSON.parse(JSON.stringify(event.effects || [])),
      }];
      event.entryStepId = promotedId;
    });
    addLog(promotedId ? `${eventDefId} root effects를 ${promotedId} step graph로 승격했다.` : `${eventDefId}에는 이미 step graph가 있다.`);
    render();
  };
}

export function bindPlacementOverrideEditorControls(deps = {}) {
  const {
    state,
    selectedPlacement = null,
    selectedPlacementEvent = null,
    eventDefinitions = {},
    eventTool = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    updatePlacementOverrides = () => {},
    resolvePlacementEvent = () => null,
    createEventPresetFromDefinition = () => "",
    defaultInteractionTypeForPlacementKind = () => "interact",
    toggleRequiredPlacementContract = () => {},
    isRequiredEventPlacement = () => false,
  } = deps;

  if (!selectedPlacement || !selectedPlacementEvent) return;

  documentObject.getElementById("placementOverrideTargetSelect").onchange = (e) => {
    state.selectedPlacementOverrideId = e.target.value;
    render();
  };
  documentObject.getElementById("placementEventPresetSelect").onchange = (e) => {
    const nextEvent = eventDefinitions[e.target.value];
    selectedPlacement.refId = e.target.value;
    selectedPlacement.interaction = {
      ...(selectedPlacement.interaction || {}),
      type: nextEvent?.interaction || selectedPlacement.interaction?.type || "interact",
      eventId: e.target.value,
    };
    render();
  };
  documentObject.getElementById("placementUsageModeOverrideSelect").onchange = (e) => {
    updatePlacementOverrides(selectedPlacement.id, (placement) => {
      placement.eventOverrides.usage = placement.eventOverrides.usage || {};
      if (!e.target.value) {
        delete placement.eventOverrides.usage.mode;
        delete placement.eventOverrides.usage.usesRemaining;
        delete placement.eventOverrides.usage.cooldownSteps;
      } else {
        placement.eventOverrides.usage.mode = e.target.value;
        if (e.target.value !== "uses") delete placement.eventOverrides.usage.usesRemaining;
        if (e.target.value !== "cooldown") delete placement.eventOverrides.usage.cooldownSteps;
        if (e.target.value === "uses" && typeof placement.eventOverrides.usage.usesRemaining !== "number") {
          placement.eventOverrides.usage.usesRemaining = 1;
        }
        if (e.target.value === "cooldown" && typeof placement.eventOverrides.usage.cooldownSteps !== "number") {
          placement.eventOverrides.usage.cooldownSteps = 3;
        }
      }
    });
    render();
  };
  documentObject.getElementById("placementUsesOverrideInput").oninput = (e) => {
    updatePlacementOverrides(selectedPlacement.id, (placement) => {
      placement.eventOverrides.usage = placement.eventOverrides.usage || {};
      if (e.target.value === "") delete placement.eventOverrides.usage.usesRemaining;
      else placement.eventOverrides.usage.usesRemaining = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("placementCooldownOverrideInput").oninput = (e) => {
    updatePlacementOverrides(selectedPlacement.id, (placement) => {
      placement.eventOverrides.usage = placement.eventOverrides.usage || {};
      if (e.target.value === "") delete placement.eventOverrides.usage.cooldownSteps;
      else placement.eventOverrides.usage.cooldownSteps = Math.max(0, Number(e.target.value || 0));
    });
  };
  if (selectedPlacement.kind === "trap") {
    documentObject.getElementById("placementDetectionDifficultyOverrideInput").oninput = (e) => {
      updatePlacementOverrides(selectedPlacement.id, (placement) => {
        placement.eventOverrides.detection = placement.eventOverrides.detection || {};
        if (e.target.value === "") delete placement.eventOverrides.detection.difficulty;
        else placement.eventOverrides.detection.difficulty = Math.max(0, Number(e.target.value || 0));
      });
    };
    documentObject.getElementById("placementDisarmDifficultyOverrideInput").oninput = (e) => {
      updatePlacementOverrides(selectedPlacement.id, (placement) => {
        placement.eventOverrides.disarm = placement.eventOverrides.disarm || {};
        if (e.target.value === "") delete placement.eventOverrides.disarm.difficulty;
        else placement.eventOverrides.disarm.difficulty = Math.max(0, Number(e.target.value || 0));
      });
    };
  }
  documentObject.getElementById("clearPlacementOverrideBtn").onclick = () => {
    delete selectedPlacement.eventOverrides;
    render();
  };
  documentObject.getElementById("promoteOverrideToPresetBtn").onclick = () => {
    const promoted = resolvePlacementEvent(selectedPlacement);
    if (!promoted) return;
    const newId = createEventPresetFromDefinition(promoted, `${selectedPlacement.id}_variant`, eventTool, `${selectedPlacement.id} 변형`);
    selectedPlacement.refId = newId;
    selectedPlacement.interaction = {
      ...(selectedPlacement.interaction || {}),
      type: defaultInteractionTypeForPlacementKind(selectedPlacement.kind),
      eventId: newId,
    };
    delete selectedPlacement.eventOverrides;
    state.selectedEventDefinitionIds[eventTool] = newId;
    addLog(`${selectedPlacement.id} override를 ${newId} preset으로 승격했다.`);
    render();
  };
  documentObject.getElementById("toggleRequiredEventPlacementBtn").onclick = () => {
    toggleRequiredPlacementContract(state.map, selectedPlacement.id, "event");
    addLog(`${selectedPlacement.id} event placement required target을 ${isRequiredEventPlacement(state.map, selectedPlacement.id) ? "표시" : "해제"}했다.`);
    render();
  };
}

export function bindClassDefinitionEditorControls(deps = {}) {
  const {
    state,
    classDef = null,
    classDefIndex = 0,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    parseJsonField = (value) => value,
    updateClassDefinition = () => {},
  } = deps;

  if (!classDef) return;
  if (!documentObject.getElementById("classDefinitionSelect")) return;

  documentObject.getElementById("classDefinitionSelect").onchange = (e) => {
    state.selectedClassDefinitionIndex = Number(e.target.value || 0);
    render();
  };
  documentObject.getElementById("classNameInput").oninput = (e) => {
    updateClassDefinition(classDefIndex, (entry) => {
      entry.cls = e.target.value.trim();
    });
  };
  documentObject.getElementById("classMilestonesJsonInput").onchange = (e) => {
    try {
      const milestones = parseJsonField(e.target.value, []);
      if (!Array.isArray(milestones)) throw new Error("milestones must be array");
      updateClassDefinition(classDefIndex, (entry) => {
        entry.progression = entry.progression || {};
        entry.progression.milestones = milestones;
      });
    } catch (error) {
      addLog(`class milestones JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
}

export function bindPresetGenerationControls(deps = {}) {
  const {
    state,
    preset = null,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    randomMapSeed = () => 0,
    makeMap = () => ({}),
    applyDraftFromPreset = () => {},
  } = deps;

  documentObject.querySelectorAll("[data-preset-select]").forEach((button) => {
    button.onclick = () => {
      state.selectedPresetId = button.dataset.presetSelect;
      render();
    };
  });
  documentObject.querySelectorAll("[data-preset-active]").forEach((input) => {
    input.onchange = () => {
      const id = input.dataset.presetActive;
      if (input.checked) state.generationPresetIds = [...new Set([...state.generationPresetIds, id])];
      else state.generationPresetIds = state.generationPresetIds.filter((entry) => entry !== id);
    };
  });
  documentObject.getElementById("generateBtn").onclick = () => {
    const floor = state.player.floor;
    const presetPool = state.presetCatalog.filter((entry) => state.generationPresetIds.includes(entry.id));
    const seed = randomMapSeed();
    state.map = makeMap(floor, seed, { presetPool });
    state.floorMaps[floor] = state.map;
    state.roomRangeStart = null;
    state.player.x = state.map.start.x;
    state.player.y = state.map.start.y;
    state.player.facing = state.map.start.facing;
    state.visited = new Set([`${state.player.x},${state.player.y}`]);
    state.visitedByFloor[floor] = state.visited;
    addLog(`${state.map.name} legacy block module generator로 생성했다. seed ${seed} · profile ${state.map.generation?.profileId || "-"} · preset ${presetPool.length}개`);
    render();
  };
  documentObject.getElementById("rotatePresetBtn").onclick = () => {
    state.presetRotation = (state.presetRotation + 1) % 4;
    render();
  };
  documentObject.getElementById("loadPresetToDraftBtn").onclick = () => {
    if (preset) applyDraftFromPreset(preset.id);
    render();
  };
}

export function bindEventRootEffectControls(deps = {}) {
  const {
    eventDefId = "",
    render = () => {},
    documentObject = document,
    updateEventDefinition = () => {},
    createEventEffectTemplate = () => ({}),
    eventEffectFlagValueType = () => "boolean_true",
  } = deps;

  const rerenderEventDefinition = (updater) => {
    updateEventDefinition(eventDefId, updater);
    render();
  };

  if (documentObject.getElementById("addEventRootEffectBtn")) {
    documentObject.getElementById("addEventRootEffectBtn").onclick = () => {
      rerenderEventDefinition((event) => {
        event.effects = Array.isArray(event.effects) ? event.effects : [];
        event.effects.push(createEventEffectTemplate());
      });
    };
  }
  documentObject.querySelectorAll("[data-event-root-effect-kind]").forEach((select) => {
    select.onchange = (e) => {
      rerenderEventDefinition((event) => {
        event.effects = Array.isArray(event.effects) ? event.effects : [];
        const index = Number(e.target.dataset.eventRootEffectKind || 0);
        if (index < 0 || index >= event.effects.length) return;
        event.effects[index] = createEventEffectTemplate(e.target.value);
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-message]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectMessage || 0)];
        if (!entry) return;
        if (e.target.value.trim()) entry.message = e.target.value.trim();
        else {
          delete entry.message;
          delete entry.text;
        }
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-flag]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectFlag || 0)];
        if (!entry) return;
        if (e.target.value.trim()) entry.flag = e.target.value.trim();
        else delete entry.flag;
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-flag-value-type]").forEach((select) => {
    select.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectFlagValueType || 0)];
        if (!entry) return;
        if (e.target.value === "boolean_true") entry.value = true;
        else if (e.target.value === "boolean_false") entry.value = false;
        else if (e.target.value === "number") entry.value = Number(entry.value || 0);
        else if (e.target.value === "string") entry.value = typeof entry.value === "string" ? entry.value : "";
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-flag-value]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectFlagValue || 0)];
        if (!entry) return;
        const valueType = eventEffectFlagValueType(entry);
        if (valueType === "number") entry.value = Number(e.target.value || 0);
        else if (valueType === "string") entry.value = e.target.value;
        else entry.value = valueType === "boolean_false" ? false : true;
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-seed]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectSeed || 0)];
        if (!entry) return;
        if (e.target.value.trim()) entry.questSeedId = e.target.value.trim();
        else delete entry.questSeedId;
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-status]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectStatus || 0)];
        if (!entry) return;
        if (e.target.value.trim()) entry.status = e.target.value.trim();
        else delete entry.status;
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-npc-placement]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectNpcPlacement || 0)];
        if (!entry) return;
        if (e.target.value.trim()) entry.npcPlacementId = e.target.value.trim();
        else delete entry.npcPlacementId;
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-service-index]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectServiceIndex || 0)];
        if (!entry) return;
        entry.serviceIndex = Math.max(0, Number(e.target.value || 0));
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-resource]").forEach((select) => {
    select.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectResource || 0)];
        if (!entry) return;
        if (e.target.value) entry.resource = e.target.value;
        else delete entry.resource;
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-amount]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectAmount || 0)];
        if (!entry) return;
        const value = Number(e.target.value || 0);
        if (entry.kind === "grant_item") entry.quantity = Math.max(1, value || 1);
        else entry.amount = value;
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-min-hp]").forEach((input) => {
    input.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectMinHp || 0)];
        if (!entry) return;
        entry.minHp = Number(e.target.value || 0);
      });
    };
  });
  documentObject.querySelectorAll("[data-event-root-effect-item]").forEach((select) => {
    select.onchange = (e) => {
      rerenderEventDefinition((event) => {
        const entry = event.effects?.[Number(e.target.dataset.eventRootEffectItem || 0)];
        if (!entry) return;
        if (e.target.value) entry.itemId = e.target.value;
        else delete entry.itemId;
      });
    };
  });
  documentObject.querySelectorAll("[data-remove-event-root-effect]").forEach((button) => {
    button.onclick = () => {
      rerenderEventDefinition((event) => {
        event.effects = Array.isArray(event.effects) ? event.effects : [];
        const index = Number(button.dataset.removeEventRootEffect || 0);
        if (index >= 0 && index < event.effects.length) event.effects.splice(index, 1);
      });
    };
  });
}

export function bindEventStepEffectControls(deps = {}) {
  const {
    updateSelectedEventStep = () => {},
    documentObject = document,
    createEventEffectTemplate = () => ({}),
    eventEffectFlagValueType = () => "boolean_true",
  } = deps;

  if (documentObject.getElementById("addEventEffectBtn")) {
    documentObject.getElementById("addEventEffectBtn").onclick = () => {
      updateSelectedEventStep((step) => {
        step.effects = Array.isArray(step.effects) ? step.effects : [];
        step.effects.push(createEventEffectTemplate());
      });
    };
  }
  documentObject.querySelectorAll("[data-event-effect-kind]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const index = Number(e.target.dataset.eventEffectKind || 0);
      if (index < 0 || index >= (step.effects || []).length) return;
      step.effects[index] = createEventEffectTemplate(e.target.value);
    });
  });
  documentObject.querySelectorAll("[data-event-effect-message]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectMessage || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.message = e.target.value.trim();
      else {
        delete entry.message;
        delete entry.text;
      }
    });
  });
  documentObject.querySelectorAll("[data-event-effect-flag]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectFlag || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.flag = e.target.value.trim();
      else delete entry.flag;
    });
  });
  documentObject.querySelectorAll("[data-event-effect-flag-value-type]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectFlagValueType || 0)];
      if (!entry) return;
      if (e.target.value === "boolean_true") entry.value = true;
      else if (e.target.value === "boolean_false") entry.value = false;
      else if (e.target.value === "number") entry.value = Number(entry.value || 0);
      else if (e.target.value === "string") entry.value = typeof entry.value === "string" ? entry.value : "";
    });
  });
  documentObject.querySelectorAll("[data-event-effect-flag-value]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectFlagValue || 0)];
      if (!entry) return;
      const valueType = eventEffectFlagValueType(entry);
      if (valueType === "number") entry.value = Number(e.target.value || 0);
      else if (valueType === "string") entry.value = e.target.value;
      else entry.value = valueType === "boolean_false" ? false : true;
    });
  });
  documentObject.querySelectorAll("[data-event-effect-seed]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectSeed || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.questSeedId = e.target.value.trim();
      else delete entry.questSeedId;
    });
  });
  documentObject.querySelectorAll("[data-event-effect-status]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectStatus || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.status = e.target.value.trim();
      else delete entry.status;
    });
  });
  documentObject.querySelectorAll("[data-event-effect-npc-placement]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectNpcPlacement || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.npcPlacementId = e.target.value.trim();
      else delete entry.npcPlacementId;
    });
  });
  documentObject.querySelectorAll("[data-event-effect-service-index]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectServiceIndex || 0)];
      if (!entry) return;
      entry.serviceIndex = Math.max(0, Number(e.target.value || 0));
    });
  });
  documentObject.querySelectorAll("[data-event-effect-resource]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectResource || 0)];
      if (!entry) return;
      if (e.target.value) entry.resource = e.target.value;
      else delete entry.resource;
    });
  });
  documentObject.querySelectorAll("[data-event-effect-amount]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectAmount || 0)];
      if (!entry) return;
      const value = Number(e.target.value || 0);
      if (entry.kind === "grant_item") entry.quantity = Math.max(1, value || 1);
      else entry.amount = value;
    });
  });
  documentObject.querySelectorAll("[data-event-effect-min-hp]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectMinHp || 0)];
      if (!entry) return;
      entry.minHp = Number(e.target.value || 0);
    });
  });
  documentObject.querySelectorAll("[data-event-effect-item]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.effects?.[Number(e.target.dataset.eventEffectItem || 0)];
      if (!entry) return;
      if (e.target.value) entry.itemId = e.target.value;
      else delete entry.itemId;
    });
  });
  documentObject.querySelectorAll("[data-remove-event-effect]").forEach((button) => {
    button.onclick = () => updateSelectedEventStep((step) => {
      const index = Number(button.dataset.removeEventEffect || 0);
      if (index >= 0 && index < (step.effects || []).length) step.effects.splice(index, 1);
    });
  });
}

export function bindEventStepMetaControls(deps = {}) {
  const {
    state,
    eventDefId = "",
    render = () => {},
    documentObject = document,
    updateEventDefinition = () => {},
    stopEditorEventTestSession = () => {},
    createEventStepTemplate = () => ({}),
    updateSelectedEventStep = () => {},
  } = deps;

  if (documentObject.getElementById("addEventStepBtn")) {
    documentObject.getElementById("addEventStepBtn").onclick = () => {
      stopEditorEventTestSession();
      updateEventDefinition(eventDefId, (event) => {
        event.steps = Array.isArray(event.steps) ? event.steps : [];
        const next = createEventStepTemplate(event);
        event.steps.push(next);
        if (!event.entryStepId) event.entryStepId = next.id;
        state.selectedEventStepIndex = event.steps.length - 1;
      });
      render();
    };
  }
  if (documentObject.getElementById("removeEventStepBtn")) {
    documentObject.getElementById("removeEventStepBtn").onclick = () => {
      stopEditorEventTestSession();
      updateEventDefinition(eventDefId, (event) => {
        event.steps = Array.isArray(event.steps) ? event.steps : [];
        const index = Math.min(Math.max(0, Number(state.selectedEventStepIndex || 0)), event.steps.length - 1);
        const removed = event.steps[index];
        if (!removed) return;
        event.steps.splice(index, 1);
        if (event.entryStepId === removed.id) {
          if (event.steps[0]?.id) event.entryStepId = event.steps[0].id;
          else delete event.entryStepId;
        }
        state.selectedEventStepIndex = Math.min(index, Math.max(0, event.steps.length - 1));
      });
      render();
    };
  }
  documentObject.getElementById("eventStepSelect").onchange = (e) => {
    state.selectedEventStepIndex = Math.max(0, Number(e.target.value || 0));
    render();
  };
  documentObject.getElementById("eventStepIdInput").onchange = (e) => {
    updateSelectedEventStep((step, event) => {
      const previousId = step.id;
      step.id = e.target.value.trim();
      if (event.entryStepId === previousId) event.entryStepId = step.id;
      for (const candidate of event.steps || []) {
        for (const branch of candidate.branches || []) {
          if (branch.nextStepId === previousId) branch.nextStepId = step.id;
        }
        for (const choice of candidate.choices || []) {
          if (choice.nextStepId === previousId) choice.nextStepId = step.id;
        }
        if (candidate.nextStepId === previousId) candidate.nextStepId = step.id;
      }
    });
  };
  documentObject.getElementById("eventStepTitleInput").oninput = (e) => {
    updateSelectedEventStep((step) => {
      step.title = e.target.value.trim();
    });
  };
  documentObject.getElementById("eventStepTextInput").onchange = (e) => {
    updateSelectedEventStep((step) => {
      step.text = e.target.value.trim();
    });
  };
  documentObject.getElementById("eventStepNextIdInput").onchange = (e) => {
    updateSelectedEventStep((step) => {
      if (e.target.value.trim()) step.nextStepId = e.target.value.trim();
      else delete step.nextStepId;
    });
  };
}

export function bindEventStepBranchControls(deps = {}) {
  const {
    updateSelectedEventStep = () => {},
    documentObject = document,
    createEventBranchTemplate = () => ({}),
  } = deps;

  if (documentObject.getElementById("addEventBranchBtn")) {
    documentObject.getElementById("addEventBranchBtn").onclick = () => {
      updateSelectedEventStep((step) => {
        step.branches = Array.isArray(step.branches) ? step.branches : [];
        step.branches.push(createEventBranchTemplate());
      });
    };
  }
  documentObject.querySelectorAll("[data-event-branch-label]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchLabel || 0)];
      if (entry) entry.label = e.target.value.trim();
    });
  });
  documentObject.querySelectorAll("[data-event-branch-next]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchNext || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.nextStepId = e.target.value.trim();
      else delete entry.nextStepId;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-flag]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredFlag || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.requiredFlag = e.target.value.trim();
      else delete entry.requiredFlag;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-missing-flag]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchMissingFlag || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.missingFlag = e.target.value.trim();
      else delete entry.missingFlag;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-resource]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredResource || 0)];
      if (!entry) return;
      if (e.target.value) entry.requiredResource = e.target.value;
      else delete entry.requiredResource;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-resource-amount]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredResourceAmount || 0)];
      if (!entry) return;
      entry.requiredResourceAtLeast = Math.max(0, Number(e.target.value || 0));
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-companion-state]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredCompanionState || 0)];
      if (!entry) return;
      if (e.target.value) entry.requiredCompanionState = e.target.value;
      else delete entry.requiredCompanionState;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-class]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredClass || 0)];
      if (!entry) return;
      if (e.target.value !== "") entry.requiredClassIndex = Number(e.target.value);
      else delete entry.requiredClassIndex;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-stat]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredStat || 0)];
      if (!entry) return;
      if (e.target.value) entry.requiredStatKey = e.target.value;
      else delete entry.requiredStatKey;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-stat-amount]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredStatAmount || 0)];
      if (!entry) return;
      entry.requiredStatAtLeast = Math.max(0, Number(e.target.value || 0));
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-seed]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredSeed || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.requiredQuestSeedId = e.target.value.trim();
      else delete entry.requiredQuestSeedId;
    });
  });
  documentObject.querySelectorAll("[data-event-branch-required-status]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.branches?.[Number(e.target.dataset.eventBranchRequiredStatus || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.requiredQuestSeedStatus = e.target.value.trim();
      else delete entry.requiredQuestSeedStatus;
    });
  });
  documentObject.querySelectorAll("[data-remove-event-branch]").forEach((button) => {
    button.onclick = () => updateSelectedEventStep((step) => {
      const index = Number(button.dataset.removeEventBranch || 0);
      if (index >= 0 && index < (step.branches || []).length) step.branches.splice(index, 1);
    });
  });
}

export function bindEventStepChoiceControls(deps = {}) {
  const {
    updateSelectedEventStep = () => {},
    documentObject = document,
    createEventChoiceTemplate = () => ({}),
  } = deps;

  if (documentObject.getElementById("addEventChoiceBtn")) {
    documentObject.getElementById("addEventChoiceBtn").onclick = () => {
      updateSelectedEventStep((step) => {
        step.choices = Array.isArray(step.choices) ? step.choices : [];
        step.choices.push(createEventChoiceTemplate());
      });
    };
  }
  documentObject.querySelectorAll("[data-event-choice-label]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceLabel || 0)];
      if (!entry) return;
      entry.label = e.target.value.trim();
      delete entry.text;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-next]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceNext || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.nextStepId = e.target.value.trim();
      else delete entry.nextStepId;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-flag]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredFlag || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.requiredFlag = e.target.value.trim();
      else delete entry.requiredFlag;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-missing-flag]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceMissingFlag || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.missingFlag = e.target.value.trim();
      else delete entry.missingFlag;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-resource]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredResource || 0)];
      if (!entry) return;
      if (e.target.value) entry.requiredResource = e.target.value;
      else delete entry.requiredResource;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-resource-amount]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredResourceAmount || 0)];
      if (!entry) return;
      entry.requiredResourceAtLeast = Math.max(0, Number(e.target.value || 0));
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-companion-state]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredCompanionState || 0)];
      if (!entry) return;
      if (e.target.value) entry.requiredCompanionState = e.target.value;
      else delete entry.requiredCompanionState;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-class]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredClass || 0)];
      if (!entry) return;
      if (e.target.value !== "") entry.requiredClassIndex = Number(e.target.value);
      else delete entry.requiredClassIndex;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-stat]").forEach((select) => {
    select.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredStat || 0)];
      if (!entry) return;
      if (e.target.value) entry.requiredStatKey = e.target.value;
      else delete entry.requiredStatKey;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-stat-amount]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredStatAmount || 0)];
      if (!entry) return;
      entry.requiredStatAtLeast = Math.max(0, Number(e.target.value || 0));
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-seed]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredSeed || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.requiredQuestSeedId = e.target.value.trim();
      else delete entry.requiredQuestSeedId;
    });
  });
  documentObject.querySelectorAll("[data-event-choice-required-status]").forEach((input) => {
    input.onchange = (e) => updateSelectedEventStep((step) => {
      const entry = step.choices?.[Number(e.target.dataset.eventChoiceRequiredStatus || 0)];
      if (!entry) return;
      if (e.target.value.trim()) entry.requiredQuestSeedStatus = e.target.value.trim();
      else delete entry.requiredQuestSeedStatus;
    });
  });
  documentObject.querySelectorAll("[data-remove-event-choice]").forEach((button) => {
    button.onclick = () => updateSelectedEventStep((step) => {
      const index = Number(button.dataset.removeEventChoice || 0);
      if (index >= 0 && index < (step.choices || []).length) step.choices.splice(index, 1);
    });
  });
}

export function bindEventStepEditorControls(deps = {}) {
  const {
    state,
    eventDefId = "",
    selectedEventStepDef = null,
    render = () => {},
    documentObject = document,
    updateEventDefinition = () => {},
    stopEditorEventTestSession = () => {},
    createEventStepTemplate = () => ({}),
    createEventBranchTemplate = () => ({}),
    createEventChoiceTemplate = () => ({}),
    createEventEffectTemplate = () => ({}),
    eventEffectFlagValueType = () => "boolean_true",
  } = deps;

  if (!selectedEventStepDef) return;

  const updateSelectedEventStep = (updater) => {
    updateEventDefinition(eventDefId, (event) => {
      event.steps = Array.isArray(event.steps) ? event.steps : [];
      const index = Math.min(Math.max(0, Number(state.selectedEventStepIndex || 0)), event.steps.length - 1);
      if (!event.steps[index]) return;
      updater(event.steps[index], event);
    });
    render();
  };

  bindEventStepMetaControls({
    state,
    eventDefId,
    render,
    documentObject,
    updateEventDefinition,
    stopEditorEventTestSession,
    createEventStepTemplate,
    updateSelectedEventStep,
  });
  bindEventStepBranchControls({
    updateSelectedEventStep,
    documentObject,
    createEventBranchTemplate,
  });
  bindEventStepChoiceControls({
    updateSelectedEventStep,
    documentObject,
    createEventChoiceTemplate,
  });
  bindEventStepEffectControls({
    updateSelectedEventStep,
    documentObject,
    createEventEffectTemplate,
    eventEffectFlagValueType,
  });
}

export function bindEventGraphTestControls(deps = {}) {
  const {
    eventDefId = "",
    eventDef = null,
    selectedEventStepDef = null,
    render = () => {},
    documentObject = document,
    startEditorEventTestSession = () => {},
    stopEditorEventTestSession = () => {},
    resolveEventChoice = () => {},
  } = deps;

  if (documentObject.getElementById("startEventEntryTestBtn")) {
    documentObject.getElementById("startEventEntryTestBtn").onclick = () => {
      startEditorEventTestSession(eventDefId, eventDef, eventDef.entryStepId || eventDef.steps?.[0]?.id || "");
      render();
    };
  }
  if (documentObject.getElementById("startSelectedEventStepTestBtn")) {
    documentObject.getElementById("startSelectedEventStepTestBtn").onclick = () => {
      if (!selectedEventStepDef?.id) return;
      startEditorEventTestSession(eventDefId, eventDef, selectedEventStepDef.id);
      render();
    };
  }
  if (documentObject.getElementById("stopEventTestBtn")) {
    documentObject.getElementById("stopEventTestBtn").onclick = () => {
      stopEditorEventTestSession();
      render();
    };
  }
  documentObject.querySelectorAll("[data-editor-event-test-option]").forEach((button) => {
    button.onclick = () => {
      resolveEventChoice(Number(button.dataset.editorEventTestOption || 0));
      render();
    };
  });
}

export function bindTrapEventEditorControls(deps = {}) {
  const {
    eventDefId = "",
    documentObject = document,
    updateEventDefinition = () => {},
  } = deps;

  if (!documentObject.getElementById("eventDetectionCheckInput")) return;

  documentObject.getElementById("eventDetectionCheckInput").oninput = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      event.detection = event.detection || {};
      event.detection.check = e.target.value.trim();
    });
  };
  documentObject.getElementById("eventDetectionDifficultyInput").oninput = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      event.detection = event.detection || {};
      event.detection.difficulty = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("eventDisarmCheckInput").oninput = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      event.disarm = event.disarm || {};
      event.disarm.check = e.target.value.trim();
    });
  };
  documentObject.getElementById("eventDisarmDifficultyInput").oninput = (e) => {
    updateEventDefinition(eventDefId, (event) => {
      event.disarm = event.disarm || {};
      event.disarm.difficulty = Math.max(0, Number(e.target.value || 0));
    });
  };
}

export function bindEventExportControls(deps = {}) {
  const {
    eventDefId = "",
    eventGraphCompactExport = null,
    eventProjectReviewBundle = null,
    currentBundleSummary = null,
    render = () => {},
    documentObject = document,
    copyTextToClipboard = async () => {},
    downloadTextFile = () => {},
    recordEventExportHistory = () => {},
    recordEventExportArchive = () => {},
    addLog = () => {},
  } = deps;

  if (documentObject.getElementById("copyEventGraphExportBtn")) {
    documentObject.getElementById("copyEventGraphExportBtn").onclick = async () => {
      try {
        await copyTextToClipboard(JSON.stringify(eventGraphCompactExport || {}, null, 2));
        const archiveEntry = {
          kind: "graph",
          mode: "copy",
          label: "current graph export",
          targetId: eventDefId || "",
          summary: eventGraphCompactExport?.summary || null,
          formatVersion: eventGraphCompactExport?.formatVersion || 0,
          payload: eventGraphCompactExport || {},
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`${eventDefId} graph export를 클립보드에 복사했다.`);
        render();
      } catch (error) {
        addLog(`graph export 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("downloadEventGraphExportBtn")) {
    documentObject.getElementById("downloadEventGraphExportBtn").onclick = () => {
      try {
        downloadTextFile(`${eventDefId || "event"}_graph.json`, JSON.stringify(eventGraphCompactExport || {}, null, 2));
        const archiveEntry = {
          kind: "graph",
          mode: "download",
          label: "current graph export",
          targetId: eventDefId || "",
          summary: eventGraphCompactExport?.summary || null,
          formatVersion: eventGraphCompactExport?.formatVersion || 0,
          payload: eventGraphCompactExport || {},
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`${eventDefId} graph export를 다운로드했다.`);
        render();
      } catch (error) {
        addLog(`graph export 다운로드 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("copyEventProjectReviewBundleBtn")) {
    documentObject.getElementById("copyEventProjectReviewBundleBtn").onclick = async () => {
      try {
        await copyTextToClipboard(JSON.stringify(eventProjectReviewBundle.compactExport, null, 2));
        const archiveEntry = {
          kind: "bundle",
          mode: "copy",
          label: "project review bundle",
          targetId: `event_count_${eventProjectReviewBundle.totals.eventCount}`,
          summary: currentBundleSummary,
          formatVersion: eventProjectReviewBundle.compactExport?.formatVersion || 0,
          payload: eventProjectReviewBundle.compactExport || {},
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog("project event review bundle을 클립보드에 복사했다.");
        render();
      } catch (error) {
        addLog(`project event review bundle 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("downloadEventProjectReviewBundleBtn")) {
    documentObject.getElementById("downloadEventProjectReviewBundleBtn").onclick = () => {
      try {
        downloadTextFile("event_project_review_bundle.json", JSON.stringify(eventProjectReviewBundle.compactExport, null, 2));
        const archiveEntry = {
          kind: "bundle",
          mode: "download",
          label: "project review bundle",
          targetId: `event_count_${eventProjectReviewBundle.totals.eventCount}`,
          summary: currentBundleSummary,
          formatVersion: eventProjectReviewBundle.compactExport?.formatVersion || 0,
          payload: eventProjectReviewBundle.compactExport || {},
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog("project event review bundle을 다운로드했다.");
        render();
      } catch (error) {
        addLog(`project event review bundle 다운로드 실패: ${error.message}`);
      }
    };
  }
}

export function bindEventExportArchiveRestoreControl(deps = {}) {
  const {
    state,
    eventTool = "",
    selectedEventExportArchiveEntry = null,
    selectedEventExportArchiveBundleRowsForRestore = [],
    effectiveEventExportArchiveFieldKeys = [],
    effectiveEventExportArchiveStepIds = [],
    effectiveEventExportArchiveStepPartKeys = [],
    effectiveEventExportArchiveStepItemKeys = [],
    render = () => {},
    documentObject = document,
    hasRestorableCompactEventPayload = () => false,
    applyPartialCompactEventRowToDefinition = () => false,
    recordEventExportHistory = () => {},
    recordEventExportArchive = () => {},
    addLog = () => {},
  } = deps;

  if (!documentObject.getElementById("restoreEventExportArchiveBtn")) return;

  documentObject.getElementById("restoreEventExportArchiveBtn").onclick = () => {
    try {
      if (!selectedEventExportArchiveEntry) {
        addLog("복원할 export archive entry가 없다.");
        return;
      }
      if (selectedEventExportArchiveEntry.kind === "graph") {
        const graphPayload = selectedEventExportArchiveEntry.payload;
        const graphEventId = selectedEventExportArchiveEntry.targetId || graphPayload?.eventId || "";
        if (!graphPayload || !graphEventId) {
          addLog("graph archive payload가 불완전해 복원할 수 없다.");
          return;
        }
        if (!hasRestorableCompactEventPayload(graphPayload)) {
          addLog(`${graphEventId} graph archive는 구형 summary-only payload라 안전하게 복원할 수 없다.`);
          return;
        }
        const applied = applyPartialCompactEventRowToDefinition(graphEventId, graphPayload, {
          fieldKeys: effectiveEventExportArchiveFieldKeys,
          stepIds: effectiveEventExportArchiveStepIds,
          stepPartKeys: effectiveEventExportArchiveStepPartKeys,
          stepItemKeys: effectiveEventExportArchiveStepItemKeys,
        });
        const result = applied ? { ok: true } : { ok: false, reason: "apply_failed" };
        if (!result.ok) {
          addLog(`${graphEventId} graph archive 복원 실패: ${result.reason || "unknown"}`);
          return;
        }
        state.editor.selectedEventDefinitionIds = { ...state.editor.selectedEventDefinitionIds, [eventTool]: graphEventId };
        const archiveEntry = {
          kind: "graph",
          mode: "restore",
          label: selectedEventExportArchiveEntry.label || "graph archive restore",
          targetId: graphEventId,
          summary: graphPayload?.summary || null,
          formatVersion: graphPayload?.formatVersion || 0,
          payload: graphPayload,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`${graphEventId} graph archive payload를 event definition에 복원했다.`);
        render();
        return;
      }
      if (selectedEventExportArchiveEntry.kind === "bundle") {
        if (!selectedEventExportArchiveBundleRowsForRestore.length) {
          addLog("복원할 bundle row selection이 없다.");
          return;
        }
        const restoredEventIds = [];
        for (const selectedRow of selectedEventExportArchiveBundleRowsForRestore) {
          const compact = selectedRow?.compact;
          const bundleEventId = selectedRow?.eventId || compact?.eventId || "";
          if (!compact || !bundleEventId) {
            addLog("복원할 bundle row compact가 없다.");
            return;
          }
          if (!hasRestorableCompactEventPayload(compact)) {
            addLog(`${bundleEventId} bundle archive row는 구형 summary-only payload라 안전하게 복원할 수 없다.`);
            return;
          }
          const applied = applyPartialCompactEventRowToDefinition(bundleEventId, compact, {
            fieldKeys: effectiveEventExportArchiveFieldKeys,
            stepIds: effectiveEventExportArchiveStepIds,
            stepPartKeys: effectiveEventExportArchiveStepPartKeys,
            stepItemKeys: effectiveEventExportArchiveStepItemKeys,
          });
          const result = applied ? { ok: true } : { ok: false, reason: "apply_failed" };
          if (!result.ok) {
            addLog(`${bundleEventId} bundle archive row 복원 실패: ${result.reason || "unknown"}`);
            return;
          }
          restoredEventIds.push(bundleEventId);
        }
        const lastEventId = restoredEventIds[restoredEventIds.length - 1] || "";
        if (!lastEventId) {
          addLog("복원된 bundle row가 없다.");
          return;
        }
        state.editor.selectedEventDefinitionIds = { ...state.editor.selectedEventDefinitionIds, [eventTool]: lastEventId };
        const archiveEntry = {
          kind: "bundle",
          mode: "restore",
          label: `${selectedEventExportArchiveEntry.label || "bundle archive restore"}:${restoredEventIds.join(",")}`,
          targetId: restoredEventIds.join(","),
          summary: {
            selectedRowCount: restoredEventIds.length,
            fieldCount: effectiveEventExportArchiveFieldKeys.length,
            stepCount: effectiveEventExportArchiveStepIds.length,
            stepPartCount: effectiveEventExportArchiveStepPartKeys.length,
            stepItemCount: effectiveEventExportArchiveStepItemKeys.length,
          },
          formatVersion: selectedEventExportArchiveEntry.payload?.formatVersion || 0,
          payload: selectedEventExportArchiveEntry.payload || {},
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`${restoredEventIds.length}개 bundle archive row를 event definition에 복원했다: ${restoredEventIds.join(", ")}`);
        render();
        return;
      }
      if (selectedEventExportArchiveEntry.kind === "archive_batch_share") {
        const sharePayload = selectedEventExportArchiveEntry.payload;
        const restoredQuery = String(sharePayload?.query || "");
        const restoredLabel = String(sharePayload?.label || selectedEventExportArchiveEntry.label || "");
        state.editor.eventExportArchiveQuery = restoredQuery;
        state.editor.eventExportArchiveBatchShareLabel = restoredLabel;
        state.editor.selectedEventExportArchiveId = "";
        state.editor.selectedEventExportArchiveBundleRowId = "";
        state.editor.selectedEventExportArchiveBundleRowIds = [];
        const archiveEntry = {
          kind: "archive_batch_share",
          mode: "restore",
          label: restoredLabel || "batch diff share restore",
          targetId: selectedEventExportArchiveEntry.targetId || "",
          summary: sharePayload?.summary || selectedEventExportArchiveEntry.summary || null,
          formatVersion: sharePayload?.formatVersion || selectedEventExportArchiveEntry.formatVersion || 0,
          payload: sharePayload || selectedEventExportArchiveEntry.payload || null,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`batch share snapshot을 복원했다. query "${restoredQuery}" · label "${restoredLabel || "(empty)"}"`);
        render();
        return;
      }
      addLog(`${selectedEventExportArchiveEntry.kind || "archive"} kind는 아직 복원하지 않는다.`);
    } catch (error) {
      addLog(`export archive 복원 실패: ${error.message}`);
    }
  };
}

export function bindEventExportArchiveDeleteControl(deps = {}) {
  const {
    state,
    selectedEventExportArchiveEntry = null,
    selectedEventExportArchiveTargetEventId = "",
    render = () => {},
    documentObject = document,
    deleteEventExportArchiveEntry = () => false,
    recordEventExportHistory = () => {},
    addLog = () => {},
  } = deps;

  if (!documentObject.getElementById("deleteEventExportArchiveBtn")) return;

  documentObject.getElementById("deleteEventExportArchiveBtn").onclick = () => {
    if (!selectedEventExportArchiveEntry?.id) {
      addLog("삭제할 export archive entry가 없다.");
      return;
    }
    const deleted = deleteEventExportArchiveEntry(selectedEventExportArchiveEntry.id);
    if (!deleted) {
      addLog("선택 export archive entry를 삭제하지 못했다.");
      return;
    }
    const archiveEntry = {
      kind: selectedEventExportArchiveEntry.kind || "archive",
      mode: "delete",
      label: selectedEventExportArchiveEntry.label || "export archive delete",
      targetId: selectedEventExportArchiveEntry.targetId || selectedEventExportArchiveTargetEventId || "",
      summary: selectedEventExportArchiveEntry.summary || null,
      formatVersion: selectedEventExportArchiveEntry.formatVersion || 0,
    };
    recordEventExportHistory(archiveEntry);
    state.editor.selectedEventExportArchiveId = "";
    state.editor.selectedEventExportArchiveBundleRowId = "";
    state.editor.selectedEventExportArchiveBundleRowIds = [];
    addLog(`${selectedEventExportArchiveEntry.targetId || selectedEventExportArchiveEntry.kind || "archive"} export archive entry를 삭제했다.`);
    render();
  };
}

export function bindEventExportArchiveBatchCompareControls(deps = {}) {
  const {
    eventTool = "",
    eventExportArchiveQuery = "",
    eventExportArchiveBatchCompare = null,
    eventExportArchiveBatchCompareExport = null,
    render = () => {},
    documentObject = document,
    copyTextToClipboard = async () => {},
    downloadTextFile = () => {},
    applyEventExportArchiveBatchCompareTargets = () => ({ ok: false }),
    recordEventExportHistory = () => {},
    recordEventExportArchive = () => {},
    addLog = () => {},
  } = deps;

  if (documentObject.getElementById("downloadEventExportArchiveBatchCompareBtn")) {
    documentObject.getElementById("downloadEventExportArchiveBatchCompareBtn").onclick = () => {
      try {
        if (!eventExportArchiveBatchCompareExport) {
          addLog("내보낼 archive batch diff가 없다.");
          return;
        }
        downloadTextFile("event_export_archive_batch_diff.json", JSON.stringify(eventExportArchiveBatchCompareExport, null, 2));
        const archiveEntry = {
          kind: "archive_batch",
          mode: "download",
          label: `archive batch diff${eventExportArchiveQuery ? `:${eventExportArchiveQuery}` : ""}`,
          targetId: `archive_count_${eventExportArchiveBatchCompareExport.summary?.totalCount || 0}`,
          summary: eventExportArchiveBatchCompareExport.summary || null,
          formatVersion: eventExportArchiveBatchCompareExport.formatVersion || 0,
          payload: eventExportArchiveBatchCompareExport,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog("event export archive batch diff를 다운로드했다.");
        render();
      } catch (error) {
        addLog(`event export archive batch diff 다운로드 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("copyEventExportArchiveBatchCompareBtn")) {
    documentObject.getElementById("copyEventExportArchiveBatchCompareBtn").onclick = async () => {
      try {
        if (!eventExportArchiveBatchCompareExport) {
          addLog("복사할 archive batch diff가 없다.");
          return;
        }
        await copyTextToClipboard(JSON.stringify(eventExportArchiveBatchCompareExport, null, 2));
        const archiveEntry = {
          kind: "archive_batch",
          mode: "copy",
          label: `archive batch diff${eventExportArchiveQuery ? `:${eventExportArchiveQuery}` : ""}`,
          targetId: `archive_count_${eventExportArchiveBatchCompareExport.summary?.totalCount || 0}`,
          summary: eventExportArchiveBatchCompareExport.summary || null,
          formatVersion: eventExportArchiveBatchCompareExport.formatVersion || 0,
          payload: eventExportArchiveBatchCompareExport,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog("event export archive batch diff를 클립보드에 복사했다.");
        render();
      } catch (error) {
        addLog(`event export archive batch diff 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("applyEventExportArchiveBatchCompareBtn")) {
    documentObject.getElementById("applyEventExportArchiveBatchCompareBtn").onclick = () => {
      try {
        if (!eventExportArchiveBatchCompare?.targetComparisons?.length) {
          addLog("적용할 export archive batch compare target이 없다.");
          return;
        }
        const applyResult = applyEventExportArchiveBatchCompareTargets(eventExportArchiveBatchCompare, eventExportArchiveQuery, eventTool);
        if (!applyResult.ok) {
          addLog(`${applyResult.failedTargetId || "archive"} batch apply 실패: ${applyResult.reason || "unknown"}`);
          return;
        }
        const archiveEntry = {
          kind: "archive_batch",
          mode: "apply",
          label: `archive batch apply${eventExportArchiveQuery ? `:${eventExportArchiveQuery}` : ""}`,
          targetId: applyResult.targetIds.join(","),
          summary: {
            targetCount: applyResult.targetIds.length,
            eventCount: applyResult.eventIds.length,
          },
          formatVersion: eventExportArchiveBatchCompareExport?.formatVersion || 0,
          payload: eventExportArchiveBatchCompareExport || null,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`event export archive batch latest snapshot을 적용했다. target ${applyResult.targetIds.length}개 · event ${applyResult.eventIds.length}개`);
        render();
      } catch (error) {
        addLog(`event export archive batch apply 실패: ${error.message}`);
      }
    };
  }
}

export function bindEventExportArchiveBatchShareControls(deps = {}) {
  const {
    state,
    eventTool = "",
    selectedEventExportArchiveEntry = null,
    eventExportArchiveBatchShareExport = null,
    eventExportArchiveBatchShareLink = "",
    render = () => {},
    documentObject = document,
    copyTextToClipboard = async () => {},
    loadEventExportArchive = () => [],
    isRestorableEventExportEntry = () => false,
    eventExportEntryMatchesQuery = () => false,
    buildEventExportArchiveBatchCompare = () => null,
    applyEventExportArchiveBatchCompareTargets = () => ({ ok: false }),
    importEventExportArchiveBatchShareLink = () => {},
    recordEventExportHistory = () => {},
    recordEventExportArchive = () => {},
    addLog = () => {},
  } = deps;

  if (documentObject.getElementById("saveEventExportArchiveBatchShareBtn")) {
    documentObject.getElementById("saveEventExportArchiveBatchShareBtn").onclick = () => {
      try {
        if (!eventExportArchiveBatchShareExport) {
          addLog("저장할 archive batch share snapshot이 없다.");
          return;
        }
        const archiveEntry = {
          kind: "archive_batch_share",
          mode: "share",
          label: eventExportArchiveBatchShareExport.label || "batch diff share",
          targetId: `archive_count_${eventExportArchiveBatchShareExport.summary?.totalCount || 0}`,
          summary: eventExportArchiveBatchShareExport.summary || null,
          formatVersion: eventExportArchiveBatchShareExport.formatVersion || 0,
          payload: eventExportArchiveBatchShareExport,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`${eventExportArchiveBatchShareExport.label || "batch diff share"} snapshot을 export archive에 저장했다.`);
        render();
      } catch (error) {
        addLog(`event export archive batch share 저장 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("copyEventExportArchiveBatchShareLinkBtn")) {
    documentObject.getElementById("copyEventExportArchiveBatchShareLinkBtn").onclick = async () => {
      try {
        if (!eventExportArchiveBatchShareLink) {
          addLog("복사할 batch share link가 없다.");
          return;
        }
        await copyTextToClipboard(eventExportArchiveBatchShareLink);
        state.editor.eventExportArchiveBatchShareLinkDraft = eventExportArchiveBatchShareLink;
        const archiveEntry = {
          kind: "archive_batch_share",
          mode: "share_link_copy",
          label: eventExportArchiveBatchShareExport?.label || "batch share link",
          targetId: `archive_count_${eventExportArchiveBatchShareExport?.summary?.totalCount || 0}`,
          summary: eventExportArchiveBatchShareExport?.summary || null,
          formatVersion: eventExportArchiveBatchShareExport?.formatVersion || 0,
          payload: eventExportArchiveBatchShareExport || null,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog("batch share link를 클립보드에 복사했다.");
        render();
      } catch (error) {
        addLog(`batch share link 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("importEventExportArchiveBatchShareLinkBtn")) {
    documentObject.getElementById("importEventExportArchiveBatchShareLinkBtn").onclick = () => {
      try {
        importEventExportArchiveBatchShareLink(state.editor.eventExportArchiveBatchShareLinkDraft || "", {
          mode: "share_link_import",
          draftValue: state.editor.eventExportArchiveBatchShareLinkDraft || "",
        });
        render();
      } catch (error) {
        addLog(`batch share link 불러오기 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("applyEventExportArchiveShareBtn")) {
    documentObject.getElementById("applyEventExportArchiveShareBtn").onclick = () => {
      try {
        if (selectedEventExportArchiveEntry?.kind !== "archive_batch_share") {
          addLog("적용할 batch share snapshot이 없다.");
          return;
        }
        const restoredQuery = String(selectedEventExportArchiveEntry.payload?.query || "");
        const matchingEntries = loadEventExportArchive()
          .filter((entry) => isRestorableEventExportEntry(entry))
          .filter((entry) => eventExportEntryMatchesQuery(entry, restoredQuery));
        const batchCompare = buildEventExportArchiveBatchCompare(matchingEntries);
        if (!batchCompare?.targetComparisons?.length) {
          addLog(`share query "${restoredQuery}" 기준으로 적용할 archive snapshot이 없다.`);
          return;
        }
        const applyResult = applyEventExportArchiveBatchCompareTargets(batchCompare, restoredQuery, eventTool);
        if (!applyResult.ok) {
          addLog(`${applyResult.failedTargetId || "share"} share apply 실패: ${applyResult.reason || "unknown"}`);
          return;
        }
        const archiveEntry = {
          kind: "archive_batch_share",
          mode: "apply",
          label: selectedEventExportArchiveEntry.payload?.label || selectedEventExportArchiveEntry.label || "batch share",
          targetId: applyResult.targetIds.join(","),
          summary: {
            query: restoredQuery,
            targetCount: applyResult.targetIds.length,
            eventCount: applyResult.eventIds.length,
          },
          formatVersion: selectedEventExportArchiveEntry.payload?.formatVersion || selectedEventExportArchiveEntry.formatVersion || 0,
          payload: selectedEventExportArchiveEntry.payload || null,
        };
        recordEventExportHistory(archiveEntry);
        recordEventExportArchive(archiveEntry);
        addLog(`batch share snapshot을 적용했다. query "${restoredQuery}" · target ${applyResult.targetIds.length}개 · event ${applyResult.eventIds.length}개`);
        render();
      } catch (error) {
        addLog(`batch share apply 실패: ${error.message}`);
      }
    };
  }
}

export function bindEventBundlePatchControls(deps = {}) {
  const {
    state,
    selectedEventBundleCompareEventId = "",
    selectedEventBundleCompareRow = null,
    selectedEventBundleComparePatch = null,
    selectedEventBundleComparePatchPreview = null,
    eventBundlePatchDraftValue = "",
    defaultEventBundlePatchJson = "",
    selectedEventBundlePatchArchiveEntry = null,
    selectedEventBundleResolvedPreview = null,
    render = () => {},
    documentObject = document,
    downloadTextFile = () => {},
    recordEventBundlePatchHistory = () => {},
    recordEventBundlePatchArchive = () => {},
    applyResolvedEventBundleRow = () => ({ ok: false }),
    addLog = () => {},
  } = deps;

  if (documentObject.getElementById("downloadEventBundlePatchBtn")) {
    documentObject.getElementById("downloadEventBundlePatchBtn").onclick = () => {
      try {
        if (!selectedEventBundleComparePatch) {
          addLog("내보낼 bundle patch가 없다.");
          return;
        }
        downloadTextFile(`${selectedEventBundleCompareEventId || "event"}_bundle_patch.json`, JSON.stringify(selectedEventBundleComparePatch, null, 2));
        const entry = {
          action: "download_patch",
          eventId: selectedEventBundleCompareEventId || "",
          label: selectedEventBundleCompareRow?.status || "patch",
          patchDraft: eventBundlePatchDraftValue,
          payload: selectedEventBundleComparePatchPreview || selectedEventBundleComparePatch || null,
        };
        recordEventBundlePatchHistory(entry);
        recordEventBundlePatchArchive(entry);
        addLog(`${selectedEventBundleCompareEventId || "event"} bundle patch를 다운로드했다.`);
        render();
      } catch (error) {
        addLog(`bundle patch 다운로드 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("resetEventBundlePatchDraftBtn")) {
    documentObject.getElementById("resetEventBundlePatchDraftBtn").onclick = () => {
      state.editor.eventBundlePatchDraft = "";
      state.editor.eventBundleFocusPath = "";
      const entry = {
        action: "reset_patch_draft",
        eventId: selectedEventBundleCompareEventId || "",
        label: selectedEventBundleCompareRow?.status || "patch",
        patchDraft: defaultEventBundlePatchJson,
        payload: selectedEventBundleComparePatch || null,
      };
      recordEventBundlePatchHistory(entry);
      recordEventBundlePatchArchive(entry);
      addLog("bundle patch 초안을 generated patch 기준으로 되돌렸다.");
      render();
    };
  }
  if (documentObject.getElementById("restoreEventBundlePatchArchiveBtn")) {
    documentObject.getElementById("restoreEventBundlePatchArchiveBtn").onclick = () => {
      const draft = selectedEventBundlePatchArchiveEntry?.patchDraft
        || (selectedEventBundlePatchArchiveEntry?.payload ? JSON.stringify(selectedEventBundlePatchArchiveEntry.payload, null, 2) : "");
      if (!draft) {
        addLog("복원할 patch archive draft가 없다.");
        return;
      }
      state.editor.eventBundlePatchDraft = draft;
      state.editor.eventBundleFocusPath = "";
      if (selectedEventBundlePatchArchiveEntry?.eventId) state.editor.eventBundleCompareEventId = selectedEventBundlePatchArchiveEntry.eventId;
      const entry = {
        action: "restore_archive_patch",
        eventId: selectedEventBundlePatchArchiveEntry?.eventId || selectedEventBundleCompareEventId || "",
        label: selectedEventBundlePatchArchiveEntry?.label || "archive",
        patchDraft: draft,
        payload: selectedEventBundlePatchArchiveEntry?.payload || null,
      };
      recordEventBundlePatchHistory(entry);
      recordEventBundlePatchArchive(entry);
      addLog(`${selectedEventBundlePatchArchiveEntry?.eventId || "event"} patch archive를 draft textarea로 복원했다.`);
      render();
    };
  }
  if (documentObject.getElementById("applyResolvedEventBundleRowBtn")) {
    documentObject.getElementById("applyResolvedEventBundleRowBtn").onclick = () => {
      try {
        if (!selectedEventBundleCompareEventId || !selectedEventBundleResolvedPreview) {
          addLog("적용할 resolved event row가 없다.");
          return;
        }
        const result = applyResolvedEventBundleRow(selectedEventBundleCompareEventId, selectedEventBundleResolvedPreview);
        if (!result.ok) {
          if (result.reason === "removed_unsupported") {
            addLog(`${selectedEventBundleCompareEventId} removed row delete는 아직 직접 apply하지 않는다.`);
          } else if (result.reason === "missing_compact") {
            addLog(`${selectedEventBundleCompareEventId} resolved row에 compact payload가 없어 apply를 건너뛰었다.`);
          } else {
            addLog(`${selectedEventBundleCompareEventId} resolved row apply 실패: ${result.reason || "unknown"}`);
          }
          return;
        }
        const entry = {
          action: "apply_resolved_row",
          eventId: selectedEventBundleCompareEventId || "",
          label: selectedEventBundleCompareRow?.status || "changed",
          patchDraft: eventBundlePatchDraftValue,
          payload: selectedEventBundleComparePatchPreview || null,
        };
        recordEventBundlePatchHistory(entry);
        recordEventBundlePatchArchive(entry);
        addLog(`${selectedEventBundleCompareEventId} resolved row를 event definition에 적용했다.`);
        render();
      } catch (error) {
        addLog(`resolved row apply 실패: ${error.message}`);
      }
    };
  }
}

export function bindEventBundleCompareControls(deps = {}) {
  const {
    state,
    eventBundleFocusOptions = [],
    render = () => {},
    documentObject = document,
  } = deps;

  if (documentObject.getElementById("eventBundleCompareEventSelect")) {
    documentObject.getElementById("eventBundleCompareEventSelect").onchange = (e) => {
      state.editor.eventBundleCompareEventId = e.target.value || "";
      state.editor.eventBundlePatchDraft = "";
      state.editor.eventBundleFocusPath = "";
      render();
    };
  }
  if (documentObject.getElementById("eventBundleComparePatchJson")) {
    documentObject.getElementById("eventBundleComparePatchJson").oninput = (e) => {
      state.editor.eventBundlePatchDraft = e.target.value || "";
      if (!eventBundleFocusOptions.includes(state.editor.eventBundleFocusPath)) state.editor.eventBundleFocusPath = "";
      render();
    };
  }
  if (documentObject.getElementById("eventBundleFocusPathSelect")) {
    documentObject.getElementById("eventBundleFocusPathSelect").onchange = (e) => {
      state.editor.eventBundleFocusPath = e.target.value || "";
      render();
    };
  }
}

export function bindEventBundleFocusedCopyControls(deps = {}) {
  const {
    selectedEventBundleFocusPath = "",
    selectedEventBundleCompareEventId = "",
    focusedEventBundlePreviousValue = null,
    focusedEventBundleCurrentValue = null,
    focusedEventBundleResolvedValue = null,
    render = () => {},
    documentObject = document,
    copyTextToClipboard = async () => {},
    recordEventBundlePatchHistory = () => {},
    recordEventBundlePatchArchive = () => {},
    addLog = () => {},
  } = deps;

  if (documentObject.getElementById("copyEventBundleFocusedValueBtn")) {
    documentObject.getElementById("copyEventBundleFocusedValueBtn").onclick = async () => {
      try {
        if (!selectedEventBundleFocusPath) {
          addLog("복사할 focused path가 없다.");
          return;
        }
        await copyTextToClipboard(JSON.stringify(focusedEventBundleResolvedValue, null, 2));
        const entry = { action: "copy_resolved", eventId: selectedEventBundleCompareEventId || "", label: selectedEventBundleFocusPath };
        recordEventBundlePatchHistory(entry);
        recordEventBundlePatchArchive(entry);
        addLog(`${selectedEventBundleFocusPath} resolved value를 클립보드에 복사했다.`);
        render();
      } catch (error) {
        addLog(`focused path value 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("copyEventBundleFocusedPreviousBtn")) {
    documentObject.getElementById("copyEventBundleFocusedPreviousBtn").onclick = async () => {
      try {
        if (!selectedEventBundleFocusPath) {
          addLog("복사할 focused previous path가 없다.");
          return;
        }
        await copyTextToClipboard(JSON.stringify(focusedEventBundlePreviousValue, null, 2));
        const entry = { action: "copy_previous", eventId: selectedEventBundleCompareEventId || "", label: selectedEventBundleFocusPath };
        recordEventBundlePatchHistory(entry);
        recordEventBundlePatchArchive(entry);
        addLog(`${selectedEventBundleFocusPath} previous value를 클립보드에 복사했다.`);
        render();
      } catch (error) {
        addLog(`focused previous value 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("copyEventBundleFocusedCurrentBtn")) {
    documentObject.getElementById("copyEventBundleFocusedCurrentBtn").onclick = async () => {
      try {
        if (!selectedEventBundleFocusPath) {
          addLog("복사할 focused current path가 없다.");
          return;
        }
        await copyTextToClipboard(JSON.stringify(focusedEventBundleCurrentValue, null, 2));
        const entry = { action: "copy_current", eventId: selectedEventBundleCompareEventId || "", label: selectedEventBundleFocusPath };
        recordEventBundlePatchHistory(entry);
        recordEventBundlePatchArchive(entry);
        addLog(`${selectedEventBundleFocusPath} current value를 클립보드에 복사했다.`);
        render();
      } catch (error) {
        addLog(`focused current value 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("copyEventBundleFocusedResolvedBtn")) {
    documentObject.getElementById("copyEventBundleFocusedResolvedBtn").onclick = async () => {
      try {
        if (!selectedEventBundleFocusPath) {
          addLog("복사할 focused resolved path가 없다.");
          return;
        }
        await copyTextToClipboard(JSON.stringify(focusedEventBundleResolvedValue, null, 2));
        const entry = { action: "copy_resolved_only", eventId: selectedEventBundleCompareEventId || "", label: selectedEventBundleFocusPath };
        recordEventBundlePatchHistory(entry);
        recordEventBundlePatchArchive(entry);
        addLog(`${selectedEventBundleFocusPath} resolved value를 클립보드에 복사했다.`);
        render();
      } catch (error) {
        addLog(`focused resolved value 복사 실패: ${error.message}`);
      }
    };
  }
}

export function bindNpcServiceEditorControls(deps = {}) {
  const {
    state,
    npcDefId = "",
    selectedNpcServiceDef = null,
    render = () => {},
    documentObject = document,
    updateNpcDefinition = () => {},
    createNpcServiceTemplate = () => ({}),
    createNpcServiceGroupTemplates = () => [],
  } = deps;

  const appendNpcService = (type) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.services = Array.isArray(entry.services) ? entry.services : [];
      entry.services.push(createNpcServiceTemplate(type));
      state.selectedNpcServiceIndex = entry.services.length - 1;
    });
    render();
  };
  const appendNpcServiceGroup = (group) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.services = Array.isArray(entry.services) ? entry.services : [];
      const next = createNpcServiceGroupTemplates(group);
      entry.services.push(...next);
      state.selectedNpcServiceIndex = Math.max(0, entry.services.length - 1);
      state.selectedNpcDialogueStepIndex = 0;
    });
    render();
  };

  if (documentObject.getElementById("addQuestHubNpcServiceGroupBtn")) {
    documentObject.getElementById("addQuestHubNpcServiceGroupBtn").onclick = () => appendNpcServiceGroup("quest_hub");
  }
  if (documentObject.getElementById("addSupportNpcServiceGroupBtn")) {
    documentObject.getElementById("addSupportNpcServiceGroupBtn").onclick = () => appendNpcServiceGroup("support");
  }
  if (documentObject.getElementById("addMerchantNpcServiceGroupBtn")) {
    documentObject.getElementById("addMerchantNpcServiceGroupBtn").onclick = () => appendNpcServiceGroup("merchant");
  }
  if (documentObject.getElementById("addCompanionNpcServiceGroupBtn")) {
    documentObject.getElementById("addCompanionNpcServiceGroupBtn").onclick = () => appendNpcServiceGroup("companion");
  }
  if (documentObject.getElementById("addHostileNpcServiceGroupBtn")) {
    documentObject.getElementById("addHostileNpcServiceGroupBtn").onclick = () => appendNpcServiceGroup("hostile");
  }
  if (documentObject.getElementById("addTalkNpcServiceBtn")) {
    documentObject.getElementById("addTalkNpcServiceBtn").onclick = () => appendNpcService("talk");
  }
  if (documentObject.getElementById("addQuestNpcServiceBtn")) {
    documentObject.getElementById("addQuestNpcServiceBtn").onclick = () => appendNpcService("quest");
  }
  if (documentObject.getElementById("addHealNpcServiceBtn")) {
    documentObject.getElementById("addHealNpcServiceBtn").onclick = () => appendNpcService("heal");
  }
  if (documentObject.getElementById("addIdentifyNpcServiceBtn")) {
    documentObject.getElementById("addIdentifyNpcServiceBtn").onclick = () => appendNpcService("identify");
  }
  if (documentObject.getElementById("addTradeNpcServiceBtn")) {
    documentObject.getElementById("addTradeNpcServiceBtn").onclick = () => appendNpcService("trade");
  }
  if (documentObject.getElementById("addRecruitNpcServiceBtn")) {
    documentObject.getElementById("addRecruitNpcServiceBtn").onclick = () => appendNpcService("recruit");
  }
  if (documentObject.getElementById("addDismissNpcServiceBtn")) {
    documentObject.getElementById("addDismissNpcServiceBtn").onclick = () => appendNpcService("dismiss");
  }
  if (documentObject.getElementById("addFightNpcServiceBtn")) {
    documentObject.getElementById("addFightNpcServiceBtn").onclick = () => appendNpcService("fight");
  }

  if (!selectedNpcServiceDef) return;

  const updateSelectedNpcService = (updater) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.services = Array.isArray(entry.services) ? entry.services : [];
      const index = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), entry.services.length - 1);
      if (!entry.services[index]) return;
      updater(entry.services[index]);
    });
    render();
  };

  documentObject.getElementById("npcServiceSelect").onchange = (e) => {
    state.selectedNpcServiceIndex = Math.max(0, Number(e.target.value || 0));
    state.selectedNpcDialogueStepIndex = 0;
    render();
  };
  if (documentObject.getElementById("duplicateNpcServiceBtn")) {
    documentObject.getElementById("duplicateNpcServiceBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.services = Array.isArray(entry.services) ? entry.services : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), entry.services.length - 1);
        if (!entry.services[index]) return;
        const clone = JSON.parse(JSON.stringify(entry.services[index]));
        if (clone.label) clone.label = `${clone.label} 사본`;
        entry.services.splice(index + 1, 0, clone);
        state.selectedNpcServiceIndex = index + 1;
        state.selectedNpcDialogueStepIndex = 0;
      });
      render();
    };
  }
  if (documentObject.getElementById("moveNpcServiceUpBtn")) {
    documentObject.getElementById("moveNpcServiceUpBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.services = Array.isArray(entry.services) ? entry.services : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), entry.services.length - 1);
        if (index <= 0 || !entry.services[index]) return;
        [entry.services[index - 1], entry.services[index]] = [entry.services[index], entry.services[index - 1]];
        state.selectedNpcServiceIndex = index - 1;
        state.selectedNpcDialogueStepIndex = 0;
      });
      render();
    };
  }
  if (documentObject.getElementById("moveNpcServiceDownBtn")) {
    documentObject.getElementById("moveNpcServiceDownBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.services = Array.isArray(entry.services) ? entry.services : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), entry.services.length - 1);
        if (index < 0 || index >= entry.services.length - 1 || !entry.services[index]) return;
        [entry.services[index], entry.services[index + 1]] = [entry.services[index + 1], entry.services[index]];
        state.selectedNpcServiceIndex = index + 1;
        state.selectedNpcDialogueStepIndex = 0;
      });
      render();
    };
  }
  documentObject.getElementById("npcServiceTypeSelect").onchange = (e) => {
    updateSelectedNpcService((service) => {
      const next = createNpcServiceTemplate(e.target.value);
      Object.keys(service).forEach((key) => delete service[key]);
      Object.assign(service, next);
    });
    state.selectedNpcDialogueStepIndex = 0;
  };
  documentObject.getElementById("npcServiceLabelInput").onchange = (e) => {
    updateSelectedNpcService((service) => {
      service.label = e.target.value.trim();
    });
  };
  if (documentObject.getElementById("removeNpcServiceBtn")) {
    documentObject.getElementById("removeNpcServiceBtn").onclick = () => {
      updateNpcDefinition(npcDefId, (entry) => {
        entry.services = Array.isArray(entry.services) ? entry.services : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), entry.services.length - 1);
        entry.services.splice(index, 1);
        state.selectedNpcServiceIndex = Math.min(index, Math.max(0, entry.services.length - 1));
        state.selectedNpcDialogueStepIndex = 0;
      });
      render();
    };
  }
}

export function bindNpcDialogueEditorControls(deps = {}) {
  const {
    state,
    selectedNpcServiceDef = null,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    parseJsonField = () => ({}),
    updateSelectedNpcService = () => {},
    createNpcDialogueStepTemplate = () => ({}),
    createNpcDialogueChoiceTemplate = () => ({}),
  } = deps;

  if (!selectedNpcServiceDef || selectedNpcServiceDef.type !== "talk") return;

  const updateSelectedNpcDialogueStep = (updater) => {
    updateSelectedNpcService((service) => {
      service.dialogue = service.dialogue && typeof service.dialogue === "object" ? service.dialogue : { steps: [] };
      service.dialogue.steps = Array.isArray(service.dialogue.steps) ? service.dialogue.steps : [];
      const index = Math.min(Math.max(0, Number(state.selectedNpcDialogueStepIndex || 0)), service.dialogue.steps.length - 1);
      if (!service.dialogue.steps[index]) return;
      updater(service.dialogue.steps[index], service.dialogue);
    });
  };

  if (documentObject.getElementById("npcServiceTalkNoteInput")) {
    documentObject.getElementById("npcServiceTalkNoteInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.note = e.target.value.trim();
        else delete service.note;
      });
    };
  }
  if (documentObject.getElementById("npcServiceDialogueJsonInput")) {
    documentObject.getElementById("npcServiceDialogueJsonInput").onchange = (e) => {
      try {
        const dialogue = parseJsonField(e.target.value, { steps: [] });
        if (!dialogue || typeof dialogue !== "object") throw new Error("dialogue must be object");
        if (!Array.isArray(dialogue.steps)) throw new Error("dialogue.steps must be array");
        updateSelectedNpcService((service) => {
          service.dialogue = dialogue;
        });
        state.selectedNpcDialogueStepIndex = Math.min(Math.max(0, Number(state.selectedNpcDialogueStepIndex || 0)), Math.max(0, dialogue.steps.length - 1));
        render();
      } catch (error) {
        addLog(`npc dialogue JSON 파싱 실패: ${error.message}`);
        render();
      }
    };
  }
  if (documentObject.getElementById("addNpcDialogueStepBtn")) {
    documentObject.getElementById("addNpcDialogueStepBtn").onclick = () => {
      updateSelectedNpcService((service) => {
        service.dialogue = service.dialogue && typeof service.dialogue === "object" ? service.dialogue : { steps: [] };
        service.dialogue.steps = Array.isArray(service.dialogue.steps) ? service.dialogue.steps : [];
        const step = createNpcDialogueStepTemplate(service);
        service.dialogue.steps.push(step);
        if (!service.dialogue.entryStepId) service.dialogue.entryStepId = step.id;
        state.selectedNpcDialogueStepIndex = service.dialogue.steps.length - 1;
      });
    };
  }
  if (documentObject.getElementById("removeNpcDialogueStepBtn")) {
    documentObject.getElementById("removeNpcDialogueStepBtn").onclick = () => {
      updateSelectedNpcService((service) => {
        service.dialogue = service.dialogue && typeof service.dialogue === "object" ? service.dialogue : { steps: [] };
        service.dialogue.steps = Array.isArray(service.dialogue.steps) ? service.dialogue.steps : [];
        const index = Math.min(Math.max(0, Number(state.selectedNpcDialogueStepIndex || 0)), service.dialogue.steps.length - 1);
        const removed = service.dialogue.steps[index];
        if (!removed) return;
        service.dialogue.steps.splice(index, 1);
        if (service.dialogue.entryStepId === removed.id) service.dialogue.entryStepId = service.dialogue.steps[0]?.id || "";
        if (!service.dialogue.entryStepId) delete service.dialogue.entryStepId;
        state.selectedNpcDialogueStepIndex = Math.min(index, Math.max(0, service.dialogue.steps.length - 1));
      });
    };
  }
  if (documentObject.getElementById("npcDialogueStepSelect")) {
    documentObject.getElementById("npcDialogueStepSelect").onchange = (e) => {
      state.selectedNpcDialogueStepIndex = Math.max(0, Number(e.target.value || 0));
      render();
    };
  }
  if (documentObject.getElementById("npcDialogueEntryStepIdInput")) {
    documentObject.getElementById("npcDialogueEntryStepIdInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        service.dialogue = service.dialogue && typeof service.dialogue === "object" ? service.dialogue : { steps: [] };
        if (e.target.value.trim()) service.dialogue.entryStepId = e.target.value.trim();
        else delete service.dialogue.entryStepId;
      });
    };
  }
  if (documentObject.getElementById("npcDialogueStepIdInput")) {
    documentObject.getElementById("npcDialogueStepIdInput").onchange = (e) => {
      updateSelectedNpcDialogueStep((step, dialogue) => {
        const previousId = step.id;
        step.id = e.target.value.trim();
        if (dialogue.entryStepId === previousId) dialogue.entryStepId = step.id;
        for (const candidate of dialogue.steps || []) {
          if (candidate.nextStepId === previousId) candidate.nextStepId = step.id;
          for (const choice of candidate.choices || []) {
            if (choice.nextStepId === previousId) choice.nextStepId = step.id;
          }
        }
      });
    };
  }
  if (documentObject.getElementById("npcDialogueStepTitleInput")) {
    documentObject.getElementById("npcDialogueStepTitleInput").onchange = (e) => {
      updateSelectedNpcDialogueStep((step) => {
        if (e.target.value.trim()) step.title = e.target.value.trim();
        else delete step.title;
      });
    };
  }
  if (documentObject.getElementById("npcDialogueStepTextInput")) {
    documentObject.getElementById("npcDialogueStepTextInput").onchange = (e) => {
      updateSelectedNpcDialogueStep((step) => {
        step.text = e.target.value.trim();
      });
    };
  }
  if (documentObject.getElementById("npcDialogueStepNextIdInput")) {
    documentObject.getElementById("npcDialogueStepNextIdInput").onchange = (e) => {
      updateSelectedNpcDialogueStep((step) => {
        if (e.target.value.trim()) step.nextStepId = e.target.value.trim();
        else delete step.nextStepId;
      });
    };
  }
  if (documentObject.getElementById("addNpcDialogueChoiceBtn")) {
    documentObject.getElementById("addNpcDialogueChoiceBtn").onclick = () => {
      updateSelectedNpcDialogueStep((step) => {
        step.choices = Array.isArray(step.choices) ? step.choices : [];
        step.choices.push(createNpcDialogueChoiceTemplate(step));
      });
    };
  }
  documentObject.querySelectorAll("[data-npc-dialogue-choice-label]").forEach((input) => {
    input.onchange = (e) => {
      updateSelectedNpcDialogueStep((step) => {
        step.choices = Array.isArray(step.choices) ? step.choices : [];
        const index = Number(e.target.dataset.npcDialogueChoiceLabel || 0);
        if (!step.choices[index]) return;
        step.choices[index].label = e.target.value.trim();
      });
    };
  });
  documentObject.querySelectorAll("[data-npc-dialogue-choice-next]").forEach((input) => {
    input.onchange = (e) => {
      updateSelectedNpcDialogueStep((step) => {
        step.choices = Array.isArray(step.choices) ? step.choices : [];
        const index = Number(e.target.dataset.npcDialogueChoiceNext || 0);
        if (!step.choices[index]) return;
        if (e.target.value.trim()) step.choices[index].nextStepId = e.target.value.trim();
        else delete step.choices[index].nextStepId;
      });
    };
  });
  documentObject.querySelectorAll("[data-npc-dialogue-choice-note]").forEach((input) => {
    input.onchange = (e) => {
      updateSelectedNpcDialogueStep((step) => {
        step.choices = Array.isArray(step.choices) ? step.choices : [];
        const index = Number(e.target.dataset.npcDialogueChoiceNote || 0);
        if (!step.choices[index]) return;
        if (e.target.value.trim()) step.choices[index].note = e.target.value.trim();
        else delete step.choices[index].note;
      });
    };
  });
  documentObject.querySelectorAll("[data-remove-npc-dialogue-choice]").forEach((button) => {
    button.onclick = () => {
      updateSelectedNpcDialogueStep((step) => {
        step.choices = Array.isArray(step.choices) ? step.choices : [];
        const index = Number(button.dataset.removeNpcDialogueChoice || 0);
        if (index < 0 || index >= step.choices.length) return;
        step.choices.splice(index, 1);
      });
    };
  });
}

export function bindNpcServiceTypeSpecificControls(deps = {}) {
  const {
    selectedNpcServiceDef = null,
    documentObject = document,
    updateSelectedNpcService = () => {},
  } = deps;

  if (!selectedNpcServiceDef) return;

  if (documentObject.getElementById("npcServiceHealAmountInput")) {
    documentObject.getElementById("npcServiceHealAmountInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        service.heal = Math.max(0, Number(e.target.value || 0));
      });
    };
  }
  if (documentObject.getElementById("npcServiceHealStatusInput")) {
    documentObject.getElementById("npcServiceHealStatusInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.cureStatus = e.target.value.trim();
        else delete service.cureStatus;
      });
    };
  }
  if (documentObject.getElementById("npcServiceGoldCostInput")) {
    documentObject.getElementById("npcServiceGoldCostInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        const gold = Math.max(0, Number(e.target.value || 0));
        if (gold > 0) {
          service.cost = service.cost || {};
          service.cost.gold = gold;
        } else if (service.cost) {
          delete service.cost.gold;
          if (!Object.keys(service.cost).length) delete service.cost;
        }
      });
    };
  }
  if (documentObject.getElementById("npcServiceVendorSelect")) {
    documentObject.getElementById("npcServiceVendorSelect").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.vendorId = e.target.value.trim();
        else delete service.vendorId;
      });
    };
  }
  if (documentObject.getElementById("npcServiceCompanionNameInput")) {
    documentObject.getElementById("npcServiceCompanionNameInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        service.companionProfile = service.companionProfile || {};
        service.companionProfile.name = e.target.value.trim();
      });
    };
  }
  if (documentObject.getElementById("npcServiceCompanionClassSelect")) {
    documentObject.getElementById("npcServiceCompanionClassSelect").onchange = (e) => {
      updateSelectedNpcService((service) => {
        service.companionProfile = service.companionProfile || {};
        service.companionProfile.classIndex = Math.max(0, Number(e.target.value || 0));
      });
    };
  }
  if (documentObject.getElementById("npcServiceCompanionNoteInput")) {
    documentObject.getElementById("npcServiceCompanionNoteInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        service.companionProfile = service.companionProfile || {};
        service.companionProfile.note = e.target.value.trim();
      });
    };
  }
  if (documentObject.getElementById("npcServiceEncounterSelect")) {
    documentObject.getElementById("npcServiceEncounterSelect").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.encounterId = e.target.value.trim();
        else delete service.encounterId;
      });
    };
  }
  if (documentObject.getElementById("npcServiceHostileFlagInput")) {
    documentObject.getElementById("npcServiceHostileFlagInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.hostileFlag = e.target.value.trim();
        else delete service.hostileFlag;
      });
    };
  }
  if (documentObject.getElementById("npcServiceHostileLogInput")) {
    documentObject.getElementById("npcServiceHostileLogInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.hostileLog = e.target.value.trim();
        else delete service.hostileLog;
      });
    };
  }
  if (documentObject.getElementById("npcServiceAvoidLabelInput")) {
    documentObject.getElementById("npcServiceAvoidLabelInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.avoidLabel = e.target.value.trim();
        else delete service.avoidLabel;
      });
    };
  }
  if (documentObject.getElementById("npcServiceAvoidGoldCostInput")) {
    documentObject.getElementById("npcServiceAvoidGoldCostInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        const gold = Math.max(0, Number(e.target.value || 0));
        if (gold > 0) {
          service.avoidCost = service.avoidCost || {};
          service.avoidCost.gold = gold;
        } else if (service.avoidCost) {
          delete service.avoidCost.gold;
          if (!Object.keys(service.avoidCost).length) delete service.avoidCost;
        }
      });
    };
  }
  if (documentObject.getElementById("npcServiceAvoidFlagInput")) {
    documentObject.getElementById("npcServiceAvoidFlagInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.avoidFlag = e.target.value.trim();
        else delete service.avoidFlag;
      });
    };
  }
  if (documentObject.getElementById("npcServiceAvoidLogInput")) {
    documentObject.getElementById("npcServiceAvoidLogInput").onchange = (e) => {
      updateSelectedNpcService((service) => {
        if (e.target.value.trim()) service.avoidLog = e.target.value.trim();
        else delete service.avoidLog;
      });
    };
  }
}

export function bindNpcSelectedServiceEditorControls(deps = {}) {
  const {
    state,
    npcDefId = "",
    selectedNpcServiceDef = null,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    parseJsonField = (value) => value,
    updateNpcDefinition = () => {},
    createNpcDialogueStepTemplate = () => ({}),
    createNpcDialogueChoiceTemplate = () => ({}),
  } = deps;

  if (!selectedNpcServiceDef) return;

  const updateSelectedNpcService = (updater) => {
    updateNpcDefinition(npcDefId, (entry) => {
      entry.services = Array.isArray(entry.services) ? entry.services : [];
      const index = Math.min(Math.max(0, Number(state.selectedNpcServiceIndex || 0)), entry.services.length - 1);
      if (!entry.services[index]) return;
      updater(entry.services[index]);
    });
    render();
  };

  bindNpcDialogueEditorControls({
    state,
    selectedNpcServiceDef,
    render,
    documentObject,
    addLog,
    parseJsonField,
    updateSelectedNpcService,
    createNpcDialogueStepTemplate,
    createNpcDialogueChoiceTemplate,
  });
  bindNpcServiceTypeSpecificControls({
    selectedNpcServiceDef,
    documentObject,
    updateSelectedNpcService,
  });
}

export function bindNpcCustomPresetArchiveControls(deps = {}) {
  const {
    state,
    npcDefId = "",
    selectedNpcCustomPreset = null,
    npcCustomPresetMergePatch = null,
    npcCustomPresetMergePatchDraftValue = "",
    npcCustomPresetMergePatchPreview = null,
    selectedNpcPresetPatchArchiveEntry = null,
    filteredNpcPresetPatchArchive = [],
    npcPresetPatchArchiveQuery = "",
    latestNpcPresetUndoEntry = null,
    latestNpcPresetRedoEntry = null,
    npcPresetPatchHistory = [],
    npcPresetRedoEntries = [],
    selectedNpcPresetRedoArchiveEntry = null,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    downloadTextFile = () => {},
    copyTextToClipboard = async () => {},
    readTextFromClipboard = async () => "",
    captureNpcPresetUndoSnapshot = () => null,
    clearNpcPresetRedoEntry = () => {},
    recordNpcPresetPatchHistory = () => {},
    recordNpcPresetPatchArchive = () => {},
    applyNpcPresetMergePatchToDefinition = () => false,
    updateNpcDefinition = () => {},
    deleteNpcPresetPatchArchiveEntry = () => false,
    deleteNpcPresetPatchArchiveEntries = () => 0,
    pushNpcPresetRedoEntry = () => {},
    applyNpcPresetUndoSnapshot = () => false,
    deleteNpcPresetRedoArchiveEntry = () => false,
  } = deps;

  if (documentObject.getElementById("npcCustomPresetMergePatchJson")) {
    documentObject.getElementById("npcCustomPresetMergePatchJson").oninput = (e) => {
      state.editor.selectedNpcCustomPresetMergePatchDraft = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("npcPresetRedoArchiveSelect")) {
    documentObject.getElementById("npcPresetRedoArchiveSelect").onchange = (e) => {
      state.editor.selectedNpcCustomPresetRedoArchiveId = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("npcPresetRedoArchiveQueryInput")) {
    documentObject.getElementById("npcPresetRedoArchiveQueryInput").oninput = (e) => {
      state.editor.selectedNpcCustomPresetRedoArchiveQuery = e.target.value || "";
      state.editor.selectedNpcCustomPresetRedoArchiveId = "";
      render();
    };
  }
  if (documentObject.getElementById("npcPresetPatchArchiveSelect")) {
    documentObject.getElementById("npcPresetPatchArchiveSelect").onchange = (e) => {
      state.editor.selectedNpcCustomPresetPatchArchiveId = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("npcPresetPatchArchiveQueryInput")) {
    documentObject.getElementById("npcPresetPatchArchiveQueryInput").oninput = (e) => {
      state.editor.selectedNpcCustomPresetPatchArchiveQuery = e.target.value || "";
      state.editor.selectedNpcCustomPresetPatchArchiveId = "";
      render();
    };
  }
  if (documentObject.getElementById("downloadNpcCustomPresetMergePatchBtn")) {
    documentObject.getElementById("downloadNpcCustomPresetMergePatchBtn").onclick = () => {
      try {
        if (!npcCustomPresetMergePatch) {
          addLog("내보낼 NPC preset merge patch가 없다.");
          return;
        }
        downloadTextFile(`${npcDefId || "npc"}_preset_merge_patch.json`, JSON.stringify(npcCustomPresetMergePatch, null, 2));
        const entry = { action: "download", npcId: npcDefId || "", label: selectedNpcCustomPreset?.name || "merge patch", patchDraft: npcCustomPresetMergePatchDraftValue, payload: npcCustomPresetMergePatchPreview || npcCustomPresetMergePatch || null };
        recordNpcPresetPatchHistory(entry);
        recordNpcPresetPatchArchive(entry);
        addLog(`${npcDefId || "npc"} preset merge patch를 다운로드했다.`);
        render();
      } catch (error) {
        addLog(`NPC preset merge patch 다운로드 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("copyNpcCustomPresetMergePatchBtn")) {
    documentObject.getElementById("copyNpcCustomPresetMergePatchBtn").onclick = async () => {
      try {
        const text = documentObject.getElementById("npcCustomPresetMergePatchJson")?.value || "";
        if (!text.trim()) {
          addLog("복사할 NPC preset merge patch JSON이 비어 있다.");
          return;
        }
        await copyTextToClipboard(text);
        const entry = { action: "copy", npcId: npcDefId || "", label: selectedNpcCustomPreset?.name || "merge patch", patchDraft: text, payload: npcCustomPresetMergePatchPreview || null };
        recordNpcPresetPatchHistory(entry);
        recordNpcPresetPatchArchive(entry);
        addLog(`${npcDefId || "npc"} merge patch를 클립보드에 복사했다.`);
        render();
      } catch (error) {
        addLog(`NPC preset merge patch 복사 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("importNpcCustomPresetMergePatchBtn")) {
    documentObject.getElementById("importNpcCustomPresetMergePatchBtn").onclick = async () => {
      try {
        const undoSnapshot = captureNpcPresetUndoSnapshot(npcDefId);
        const text = await readTextFromClipboard();
        if (!text.trim()) {
          addLog("클립보드에 불러올 NPC preset merge patch 텍스트가 없다.");
          return;
        }
        const textarea = documentObject.getElementById("npcCustomPresetMergePatchJson");
        if (textarea) textarea.value = text;
        state.editor.selectedNpcCustomPresetMergePatchDraft = text;
        clearNpcPresetRedoEntry();
        const entry = { action: "import", npcId: npcDefId || "", label: "clipboard import", patchDraft: text, undoSnapshot };
        recordNpcPresetPatchHistory(entry);
        recordNpcPresetPatchArchive(entry);
        addLog(`${npcDefId || "npc"} merge patch를 클립보드에서 불러왔다.`);
        render();
      } catch (error) {
        addLog(`NPC preset merge patch 클립보드 불러오기 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("applyNpcCustomPresetMergePatchBtn")) {
    documentObject.getElementById("applyNpcCustomPresetMergePatchBtn").onclick = () => {
      try {
        const undoSnapshot = captureNpcPresetUndoSnapshot(npcDefId);
        const raw = documentObject.getElementById("npcCustomPresetMergePatchJson")?.value || "";
        if (!raw.trim()) {
          addLog("적용할 NPC preset merge patch JSON이 비어 있다.");
          return;
        }
        const patch = JSON.parse(raw);
        if (!patch || patch.kind !== "npcPresetMergePatch") {
          addLog("NPC preset merge patch 형식이 아니다.");
          return;
        }
        let applied = false;
        updateNpcDefinition(npcDefId, (entry) => {
          applied = applyNpcPresetMergePatchToDefinition(entry, patch);
        });
        if (!applied) {
          addLog("적용할 service/quest seed patch entry가 없다.");
          return render();
        }
        clearNpcPresetRedoEntry();
        const entry = { action: "apply", npcId: npcDefId || "", label: patch.presetName || patch.presetId || "merge patch", patchDraft: raw, payload: patch, undoSnapshot };
        recordNpcPresetPatchHistory(entry);
        recordNpcPresetPatchArchive(entry);
        addLog(`${npcDefId || "npc"}에 preset merge patch를 적용했다.`);
        render();
      } catch (error) {
        addLog(`NPC preset merge patch 적용 실패: ${error.message}`);
        render();
      }
    };
  }
  if (documentObject.getElementById("restoreNpcPresetPatchArchiveBtn")) {
    documentObject.getElementById("restoreNpcPresetPatchArchiveBtn").onclick = () => {
      const undoSnapshot = captureNpcPresetUndoSnapshot(npcDefId);
      const draft = selectedNpcPresetPatchArchiveEntry?.patchDraft
        || (selectedNpcPresetPatchArchiveEntry?.payload ? JSON.stringify(selectedNpcPresetPatchArchiveEntry.payload, null, 2) : "");
      if (!draft) {
        addLog("복원할 NPC preset patch archive draft가 없다.");
        return;
      }
      const textarea = documentObject.getElementById("npcCustomPresetMergePatchJson");
      if (textarea) textarea.value = draft;
      state.editor.selectedNpcCustomPresetMergePatchDraft = draft;
      clearNpcPresetRedoEntry();
      const entry = { action: "restore", npcId: selectedNpcPresetPatchArchiveEntry?.npcId || npcDefId || "", label: selectedNpcPresetPatchArchiveEntry?.label || "patch archive", patchDraft: draft, payload: selectedNpcPresetPatchArchiveEntry?.payload || null, undoSnapshot };
      recordNpcPresetPatchHistory(entry);
      recordNpcPresetPatchArchive(entry);
      addLog(`${selectedNpcPresetPatchArchiveEntry?.npcId || npcDefId || "npc"} patch archive를 textarea로 복원했다.`);
      render();
    };
  }
  if (documentObject.getElementById("deleteNpcPresetPatchArchiveBtn")) {
    documentObject.getElementById("deleteNpcPresetPatchArchiveBtn").onclick = () => {
      if (!selectedNpcPresetPatchArchiveEntry?.id) {
        addLog("삭제할 NPC patch archive entry가 없다.");
        return;
      }
      const undoSnapshot = captureNpcPresetUndoSnapshot(npcDefId);
      const deleted = deleteNpcPresetPatchArchiveEntry(selectedNpcPresetPatchArchiveEntry.id);
      if (!deleted) {
        addLog("선택 NPC patch archive entry를 삭제하지 못했다.");
        return;
      }
      recordNpcPresetPatchHistory({ action: "archive_delete", npcId: selectedNpcPresetPatchArchiveEntry?.npcId || npcDefId || "", label: selectedNpcPresetPatchArchiveEntry?.label || "patch archive", undoSnapshot });
      clearNpcPresetRedoEntry();
      state.editor.selectedNpcCustomPresetPatchArchiveId = "";
      addLog(`${selectedNpcPresetPatchArchiveEntry?.npcId || npcDefId || "npc"} patch archive entry를 삭제했다.`);
      render();
    };
  }
  if (documentObject.getElementById("deleteFilteredNpcPresetPatchArchiveBtn")) {
    documentObject.getElementById("deleteFilteredNpcPresetPatchArchiveBtn").onclick = () => {
      if (!filteredNpcPresetPatchArchive.length) {
        addLog("삭제할 검색 결과 patch archive가 없다.");
        return;
      }
      const undoSnapshot = captureNpcPresetUndoSnapshot(npcDefId);
      const deletedCount = deleteNpcPresetPatchArchiveEntries(filteredNpcPresetPatchArchive.map((entry) => entry.id));
      if (!deletedCount) {
        addLog("검색 결과 patch archive를 삭제하지 못했다.");
        return;
      }
      recordNpcPresetPatchHistory({
        action: "archive_bulk_delete",
        npcId: npcDefId || "",
        label: `${npcPresetPatchArchiveQuery || "filtered"}:${deletedCount}`,
        undoSnapshot,
      });
      clearNpcPresetRedoEntry();
      state.editor.selectedNpcCustomPresetPatchArchiveId = "";
      addLog(`${deletedCount}개 NPC patch archive 검색 결과를 삭제했다.`);
      render();
    };
  }
  if (documentObject.getElementById("undoNpcCustomPresetPatchBtn")) {
    documentObject.getElementById("undoNpcCustomPresetPatchBtn").onclick = () => {
      if (!latestNpcPresetUndoEntry?.undoSnapshot) {
        addLog("되돌릴 NPC patch action snapshot이 없다.");
        return;
      }
      const redoSnapshot = captureNpcPresetUndoSnapshot(npcDefId);
      const restored = applyNpcPresetUndoSnapshot(latestNpcPresetUndoEntry.undoSnapshot);
      if (!restored) {
        addLog("마지막 NPC patch action을 되돌리지 못했다.");
        return;
      }
      const textarea = documentObject.getElementById("npcCustomPresetMergePatchJson");
      if (textarea) textarea.value = state.editor.selectedNpcCustomPresetMergePatchDraft || "";
      state.editor.selectedNpcCustomPresetPatchHistory = npcPresetPatchHistory.filter((entry) => entry.id !== latestNpcPresetUndoEntry.id);
      pushNpcPresetRedoEntry({
        action: latestNpcPresetUndoEntry.action || "patch",
        npcId: latestNpcPresetUndoEntry.npcId || npcDefId || "",
        label: latestNpcPresetUndoEntry.label || "",
        redoSnapshot,
      });
      addLog(`${latestNpcPresetUndoEntry.npcId || npcDefId || "npc"} patch action을 이전 snapshot으로 되돌렸다.`);
      render();
    };
  }
  if (documentObject.getElementById("redoNpcCustomPresetPatchBtn")) {
    documentObject.getElementById("redoNpcCustomPresetPatchBtn").onclick = () => {
      if (!latestNpcPresetRedoEntry?.redoSnapshot) {
        addLog("다시 적용할 NPC patch redo snapshot이 없다.");
        return;
      }
      const restored = applyNpcPresetUndoSnapshot(latestNpcPresetRedoEntry.redoSnapshot);
      if (!restored) {
        addLog("NPC patch redo를 적용하지 못했다.");
        return;
      }
      const textarea = documentObject.getElementById("npcCustomPresetMergePatchJson");
      if (textarea) textarea.value = state.editor.selectedNpcCustomPresetMergePatchDraft || "";
      state.editor.selectedNpcCustomPresetRedoEntries = npcPresetRedoEntries.slice(1);
      addLog(`${latestNpcPresetRedoEntry.npcId || npcDefId || "npc"} patch action을 redo snapshot으로 다시 적용했다.`);
      render();
    };
  }
  if (documentObject.getElementById("restoreNpcPresetRedoArchiveBtn")) {
    documentObject.getElementById("restoreNpcPresetRedoArchiveBtn").onclick = () => {
      if (!selectedNpcPresetRedoArchiveEntry?.redoSnapshot) {
        addLog("복원할 redo archive snapshot이 없다.");
        return;
      }
      const restoredEntry = JSON.parse(JSON.stringify(selectedNpcPresetRedoArchiveEntry));
      delete restoredEntry.id;
      delete restoredEntry.archivedAt;
      const stack = Array.isArray(state.editor.selectedNpcCustomPresetRedoEntries) ? state.editor.selectedNpcCustomPresetRedoEntries : [];
      state.editor.selectedNpcCustomPresetRedoEntries = [restoredEntry, ...stack].slice(0, 8);
      addLog(`${selectedNpcPresetRedoArchiveEntry.npcId || npcDefId || "npc"} redo archive snapshot을 현재 redo stack에 복원했다.`);
      render();
    };
  }
  if (documentObject.getElementById("deleteNpcPresetRedoArchiveBtn")) {
    documentObject.getElementById("deleteNpcPresetRedoArchiveBtn").onclick = () => {
      if (!selectedNpcPresetRedoArchiveEntry?.id) {
        addLog("삭제할 redo archive entry가 없다.");
        return;
      }
      const deleted = deleteNpcPresetRedoArchiveEntry(selectedNpcPresetRedoArchiveEntry.id);
      if (!deleted) {
        addLog("redo archive entry를 삭제하지 못했다.");
        return;
      }
      state.editor.selectedNpcCustomPresetRedoArchiveId = "";
      addLog(`${selectedNpcPresetRedoArchiveEntry.npcId || npcDefId || "npc"} redo archive entry를 삭제했다.`);
      render();
    };
  }
}

export function bindNpcCustomPresetSelectionControls(deps = {}) {
  const {
    state,
    selectedNpcCustomPreset = null,
    npcCustomPresetDiff = null,
    render = () => {},
    documentObject = document,
    selectedNpcPresetServiceIndexes = () => [],
    selectedNpcPresetSeedIndexes = () => [],
    selectedNpcPresetDialogueStepIndexes = () => [],
    selectedNpcPresetDialogueChoiceIndexes = () => [],
    selectedNpcPresetDialogueBranchIndexes = () => [],
    selectedNpcPresetServiceFieldNames = () => [],
    selectedNpcPresetSeedFieldNames = () => [],
  } = deps;

  if (!selectedNpcCustomPreset) return;

  documentObject.querySelectorAll("[data-npc-preset-service-index]").forEach((checkbox) => {
    checkbox.onchange = () => {
      const index = Number(checkbox.dataset.npcPresetServiceIndex || -1);
      const selected = new Set(selectedNpcPresetServiceIndexes(selectedNpcCustomPreset));
      if (checkbox.checked) selected.add(index);
      else selected.delete(index);
      state.editor.selectedNpcCustomPresetServiceIndexes = [...selected].sort((a, b) => a - b);
      render();
    };
  });
  documentObject.querySelectorAll("[data-npc-preset-seed-index]").forEach((checkbox) => {
    checkbox.onchange = () => {
      const index = Number(checkbox.dataset.npcPresetSeedIndex || -1);
      const selected = new Set(selectedNpcPresetSeedIndexes(selectedNpcCustomPreset));
      if (checkbox.checked) selected.add(index);
      else selected.delete(index);
      state.editor.selectedNpcCustomPresetSeedIndexes = [...selected].sort((a, b) => a - b);
      render();
    };
  });
  documentObject.querySelectorAll("[data-npc-preset-dialogue-step-index]").forEach((checkbox) => {
    checkbox.onchange = () => {
      const [serviceIndexText, stepIndexText] = String(checkbox.dataset.npcPresetDialogueStepIndex || "").split(":");
      const serviceIndex = Number(serviceIndexText || -1);
      const stepIndex = Number(stepIndexText || -1);
      const selected = new Set(selectedNpcPresetDialogueStepIndexes(selectedNpcCustomPreset, serviceIndex));
      if (checkbox.checked) selected.add(stepIndex);
      else selected.delete(stepIndex);
      state.editor.selectedNpcCustomPresetDialogueStepSelections = {
        ...(state.editor.selectedNpcCustomPresetDialogueStepSelections || {}),
        [serviceIndex]: [...selected].sort((a, b) => a - b),
      };
      render();
    };
  });
  documentObject.querySelectorAll("[data-npc-preset-dialogue-choice-index]").forEach((checkbox) => {
    checkbox.onchange = () => {
      const [serviceIndexText, stepIndexText, choiceIndexText] = String(checkbox.dataset.npcPresetDialogueChoiceIndex || "").split(":");
      const serviceIndex = Number(serviceIndexText || -1);
      const stepIndex = Number(stepIndexText || -1);
      const choiceIndex = Number(choiceIndexText || -1);
      const key = `${serviceIndex}:${stepIndex}`;
      const selected = new Set(selectedNpcPresetDialogueChoiceIndexes(selectedNpcCustomPreset, serviceIndex, stepIndex));
      if (checkbox.checked) selected.add(choiceIndex);
      else selected.delete(choiceIndex);
      state.editor.selectedNpcCustomPresetDialogueChoiceSelections = {
        ...(state.editor.selectedNpcCustomPresetDialogueChoiceSelections || {}),
        [key]: [...selected].sort((a, b) => a - b),
      };
      render();
    };
  });
  documentObject.querySelectorAll("[data-npc-preset-dialogue-branch-index]").forEach((checkbox) => {
    checkbox.onchange = () => {
      const [serviceIndexText, stepIndexText, branchIndexText] = String(checkbox.dataset.npcPresetDialogueBranchIndex || "").split(":");
      const serviceIndex = Number(serviceIndexText || -1);
      const stepIndex = Number(stepIndexText || -1);
      const branchIndex = Number(branchIndexText || -1);
      const key = `${serviceIndex}:${stepIndex}`;
      const selected = new Set(selectedNpcPresetDialogueBranchIndexes(selectedNpcCustomPreset, serviceIndex, stepIndex));
      if (checkbox.checked) selected.add(branchIndex);
      else selected.delete(branchIndex);
      state.editor.selectedNpcCustomPresetDialogueBranchSelections = {
        ...(state.editor.selectedNpcCustomPresetDialogueBranchSelections || {}),
        [key]: [...selected].sort((a, b) => a - b),
      };
      render();
    };
  });
  documentObject.querySelectorAll("[data-npc-preset-service-field]").forEach((checkbox) => {
    checkbox.onchange = () => {
      const [rowIndexText, ...fieldParts] = String(checkbox.dataset.npcPresetServiceField || "").split(":");
      const rowIndex = Number(rowIndexText || -1);
      const fieldName = fieldParts.join(":");
      const key = String(rowIndex);
      const row = npcCustomPresetDiff.serviceRows?.find((entry) => entry.index === rowIndex);
      const selected = new Set(selectedNpcPresetServiceFieldNames(row));
      if (checkbox.checked) selected.add(fieldName);
      else selected.delete(fieldName);
      state.editor.selectedNpcCustomPresetServiceFieldSelections = {
        ...(state.editor.selectedNpcCustomPresetServiceFieldSelections || {}),
        [key]: [...selected].sort(),
      };
      render();
    };
  });
  documentObject.querySelectorAll("[data-npc-preset-seed-field]").forEach((checkbox) => {
    checkbox.onchange = () => {
      const [rowIndexText, ...fieldParts] = String(checkbox.dataset.npcPresetSeedField || "").split(":");
      const rowIndex = Number(rowIndexText || -1);
      const fieldName = fieldParts.join(":");
      const key = String(rowIndex);
      const row = npcCustomPresetDiff.seedRows?.find((entry) => entry.index === rowIndex);
      const selected = new Set(selectedNpcPresetSeedFieldNames(row));
      if (checkbox.checked) selected.add(fieldName);
      else selected.delete(fieldName);
      state.editor.selectedNpcCustomPresetSeedFieldSelections = {
        ...(state.editor.selectedNpcCustomPresetSeedFieldSelections || {}),
        [key]: [...selected].sort(),
      };
      render();
    };
  });
}

export function bindNpcCustomPresetApplyControls(deps = {}) {
  const {
    state,
    npcDefId = "",
    npcDef = null,
    npcCustomPresetDiff = null,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    loadNpcCustomPresets = () => [],
    saveNpcCustomPresets = () => {},
    updateNpcDefinition = () => {},
    npcPresetServiceIdentity = () => "",
    npcPresetSeedIdentity = () => "",
    selectedNpcPresetServiceIndexes = () => [],
    selectedNpcPresetSeedIndexes = () => [],
    selectedNpcPresetServiceFieldNames = () => [],
    selectedNpcPresetSeedFieldNames = () => [],
    selectedNpcPresetDialogueStepIndexes = () => [],
    selectedNpcPresetDialogueChoiceIndexes = () => [],
    selectedNpcPresetDialogueBranchIndexes = () => [],
    npcDialogueStepIdentity = () => "",
    npcDialogueChoiceIdentity = () => "",
    npcDialogueBranchIdentity = () => "",
    defaultNpcPresetSelectionIndexes = () => [],
    defaultNpcPresetDialogueSelectionMap = () => ({}),
    defaultNpcPresetDialogueChoiceSelectionMap = () => ({}),
    defaultNpcPresetDialogueBranchSelectionMap = () => ({}),
    defaultNpcPresetServiceFieldSelectionMap = () => ({}),
    defaultNpcPresetSeedFieldSelectionMap = () => ({}),
    buildNpcCustomPresetDiff = () => null,
  } = deps;

  if (documentObject.getElementById("applyNpcCustomPresetBtn")) {
    documentObject.getElementById("applyNpcCustomPresetBtn").onclick = () => {
      const preset = loadNpcCustomPresets().find((entry) => entry.id === state.editor.selectedNpcCustomPresetId);
      if (!preset) {
        addLog("적용할 NPC custom preset이 없다.");
        return render();
      }
      const applyMode = state.editor.selectedNpcCustomPresetApplyMode === "append" ? "append" : "replace";
      const conflictMode = state.editor.selectedNpcCustomPresetConflictMode === "keep_current" ? "keep_current" : "preset_wins";
      const selectedServiceIndexes = selectedNpcPresetServiceIndexes(preset);
      const selectedSeedIndexes = selectedNpcPresetSeedIndexes(preset);
      const selectedServices = (preset.services || []).filter((_, index) => selectedServiceIndexes.includes(index));
      const selectedSeeds = (preset.questSeeds || []).filter((_, index) => selectedSeedIndexes.includes(index));
      if (!selectedServices.length && !selectedSeeds.length) {
        addLog("적용 대상으로 선택된 preset service/quest seed가 없다.");
        return render();
      }
      updateNpcDefinition(npcDefId, (entry) => {
        const currentServices = Array.isArray(entry.services) ? entry.services : [];
        const currentSeeds = Array.isArray(entry.questSeeds) ? entry.questSeeds : [];
        if (selectedServices.length) {
          if (applyMode === "append") {
            entry.services = [...currentServices];
            selectedServices.forEach((service) => {
              const identity = npcPresetServiceIdentity(service);
              const existingIndex = entry.services.findIndex((candidate) => npcPresetServiceIdentity(candidate) === identity);
              const serviceIndex = (preset.services || []).findIndex((candidate) => candidate === service);
              const diffRow = npcCustomPresetDiff.serviceRows?.find((row) => row.index === serviceIndex) || null;
              const selectedFieldNames = selectedNpcPresetServiceFieldNames(diffRow);
              if (existingIndex < 0) {
                entry.services.push(JSON.parse(JSON.stringify(service)));
                return;
              }
              if (conflictMode === "preset_wins") {
                entry.services[existingIndex] = JSON.parse(JSON.stringify(service));
                return;
              }
              const existingService = entry.services[existingIndex] || {};
              selectedFieldNames.forEach((fieldName) => {
                if (fieldName === "dialogue") return;
                const nextValue = service?.[fieldName];
                if (nextValue == null) delete existingService[fieldName];
                else existingService[fieldName] = JSON.parse(JSON.stringify(nextValue));
              });
              if (service?.type === "talk" && Array.isArray(service?.dialogue?.steps)) {
                const selectedStepIndexes = selectedNpcPresetDialogueStepIndexes(preset, serviceIndex);
                existingService.dialogue = existingService.dialogue && typeof existingService.dialogue === "object" ? existingService.dialogue : {};
                existingService.dialogue.steps = Array.isArray(existingService.dialogue.steps) ? existingService.dialogue.steps : [];
                const selectedSteps = service.dialogue.steps.filter((_, stepIndex) => selectedStepIndexes.includes(stepIndex));
                selectedSteps.forEach((step) => {
                  const stepId = npcDialogueStepIdentity(step);
                  const existingStepIndex = existingService.dialogue.steps.findIndex((candidate) => npcDialogueStepIdentity(candidate) === stepId);
                  if (existingStepIndex >= 0) {
                    const currentStep = existingService.dialogue.steps[existingStepIndex] || {};
                    const presetStepIndex = (service.dialogue.steps || []).findIndex((candidate) => candidate === step);
                    const selectedChoiceIndexes = selectedNpcPresetDialogueChoiceIndexes(preset, serviceIndex, presetStepIndex);
                    const selectedBranchIndexes = selectedNpcPresetDialogueBranchIndexes(preset, serviceIndex, presetStepIndex);
                    const selectedChoices = (step.choices || []).filter((_, choiceIndex) => selectedChoiceIndexes.includes(choiceIndex));
                    const selectedBranches = (step.branches || []).filter((_, branchIndex) => selectedBranchIndexes.includes(branchIndex));
                    const nextStep = JSON.parse(JSON.stringify(currentStep));
                    nextStep.choices = Array.isArray(nextStep.choices) ? nextStep.choices : [];
                    nextStep.branches = Array.isArray(nextStep.branches) ? nextStep.branches : [];
                    selectedChoices.forEach((choice) => {
                      const choiceIdentity = npcDialogueChoiceIdentity(choice);
                      const existingChoiceIndex = nextStep.choices.findIndex((candidate) => npcDialogueChoiceIdentity(candidate) === choiceIdentity);
                      if (existingChoiceIndex >= 0) nextStep.choices[existingChoiceIndex] = JSON.parse(JSON.stringify(choice));
                      else nextStep.choices.push(JSON.parse(JSON.stringify(choice)));
                    });
                    selectedBranches.forEach((branch) => {
                      const branchIdentity = npcDialogueBranchIdentity(branch);
                      const existingBranchIndex = nextStep.branches.findIndex((candidate) => npcDialogueBranchIdentity(candidate) === branchIdentity);
                      if (existingBranchIndex >= 0) nextStep.branches[existingBranchIndex] = JSON.parse(JSON.stringify(branch));
                      else nextStep.branches.push(JSON.parse(JSON.stringify(branch)));
                    });
                    if (step.text) nextStep.text = step.text;
                    if (step.title) nextStep.title = step.title;
                    if (step.nextStepId) nextStep.nextStepId = step.nextStepId;
                    existingService.dialogue.steps[existingStepIndex] = nextStep;
                  } else {
                    existingService.dialogue.steps.push(JSON.parse(JSON.stringify(step)));
                  }
                });
              }
              entry.services[existingIndex] = existingService;
            });
          } else {
            entry.services = JSON.parse(JSON.stringify(selectedServices));
          }
        }
        if (selectedSeeds.length) {
          if (applyMode === "append") {
            entry.questSeeds = [...currentSeeds];
            selectedSeeds.forEach((seed) => {
              const identity = npcPresetSeedIdentity(seed);
              const existingIndex = entry.questSeeds.findIndex((candidate) => npcPresetSeedIdentity(candidate) === identity);
              const seedIndex = (preset.questSeeds || []).findIndex((candidate) => candidate === seed);
              const diffRow = npcCustomPresetDiff.seedRows?.find((row) => row.index === seedIndex) || null;
              const selectedFieldNames = selectedNpcPresetSeedFieldNames(diffRow);
              if (existingIndex < 0) {
                entry.questSeeds.push(JSON.parse(JSON.stringify(seed)));
                return;
              }
              if (conflictMode === "preset_wins") {
                entry.questSeeds[existingIndex] = JSON.parse(JSON.stringify(seed));
                return;
              }
              const existingSeed = entry.questSeeds[existingIndex] || {};
              selectedFieldNames.forEach((fieldName) => {
                const nextValue = seed?.[fieldName];
                if (nextValue == null) delete existingSeed[fieldName];
                else existingSeed[fieldName] = JSON.parse(JSON.stringify(nextValue));
              });
              entry.questSeeds[existingIndex] = existingSeed;
            });
          } else {
            entry.questSeeds = JSON.parse(JSON.stringify(selectedSeeds));
          }
        }
      });
      state.selectedNpcServiceIndex = 0;
      state.selectedNpcQuestSeedIndex = 0;
      state.selectedNpcDialogueStepIndex = 0;
      addLog(`${preset.name} preset을 ${npcDef?.name || npcDefId}에 ${applyMode === "append" ? "append" : "replace"} 모드로 적용했다. (service ${selectedServices.length} · seed ${selectedSeeds.length})`);
      render();
    };
  }
  if (documentObject.getElementById("deleteNpcCustomPresetBtn")) {
    documentObject.getElementById("deleteNpcCustomPresetBtn").onclick = () => {
      const presets = loadNpcCustomPresets();
      const targetId = state.editor.selectedNpcCustomPresetId;
      const target = presets.find((entry) => entry.id === targetId);
      if (!target) {
        addLog("삭제할 NPC custom preset이 없다.");
        return render();
      }
      const next = presets.filter((entry) => entry.id !== targetId);
      saveNpcCustomPresets(next);
      state.editor.selectedNpcCustomPresetId = next[0]?.id || "";
      state.editor.selectedNpcCustomPresetServiceIndexes = defaultNpcPresetSelectionIndexes(next[0]?.services || []);
      state.editor.selectedNpcCustomPresetSeedIndexes = defaultNpcPresetSelectionIndexes(next[0]?.questSeeds || []);
      state.editor.selectedNpcCustomPresetDialogueStepSelections = defaultNpcPresetDialogueSelectionMap(next[0]);
      state.editor.selectedNpcCustomPresetDialogueChoiceSelections = defaultNpcPresetDialogueChoiceSelectionMap(next[0]);
      state.editor.selectedNpcCustomPresetDialogueBranchSelections = defaultNpcPresetDialogueBranchSelectionMap(next[0]);
      state.editor.selectedNpcCustomPresetServiceFieldSelections = defaultNpcPresetServiceFieldSelectionMap(buildNpcCustomPresetDiff(npcDef, next[0]));
      state.editor.selectedNpcCustomPresetSeedFieldSelections = defaultNpcPresetSeedFieldSelectionMap(buildNpcCustomPresetDiff(npcDef, next[0]));
      state.editor.selectedNpcCustomPresetMergePatchDraft = "";
      addLog(`${target.name} NPC preset을 삭제했다.`);
      render();
    };
  }
}

export function bindEditorWorkspaceActionControls(deps = {}) {
  const {
    state,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    startTestPlaySession = () => ({ ok: false, failures: [] }),
    firstValidationIssue = () => null,
    validationIssueRepairHint = () => "",
    validationSummaryText = () => "",
    saveEditorProject = () => {},
    loadEditorProject = () => {},
    cloneEditorMap = (map) => map,
    buildEditorProject = () => ({}),
    buildCompiledMapForRuntime = () => ({ ok: false, report: null }),
    buildContentBuildManifest = () => ({ ok: false, report: null }),
    applyEditorProject = () => {},
    normalizeMapMetadata = () => {},
    computeWalls = () => {},
  } = deps;

  if (documentObject.getElementById("testBtn")) {
    documentObject.getElementById("testBtn").onclick = () => {
      const result = startTestPlaySession();
      if (!result.ok) {
        const firstFailure = result.failures[0];
        const firstIssue = firstValidationIssue(firstFailure.report);
        const issueText = firstIssue?.message ? ` · 먼저 볼 항목: ${firstIssue.message}` : "";
        const repairHint = firstIssue ? ` · 힌트: ${validationIssueRepairHint(firstIssue)}` : "";
        addLog(`테스트 플레이 차단: 층 ${firstFailure.floor} ${validationSummaryText(firstFailure.report)}${issueText}${repairHint}`);
        return render();
      }
      addLog(`${state.map.name} compiledMap 묶음으로 임시 테스트 플레이 세션을 시작한다.`);
      render();
    };
  }
  if (documentObject.getElementById("saveProjectBtn")) {
    documentObject.getElementById("saveProjectBtn").onclick = saveEditorProject;
  }
  if (documentObject.getElementById("loadProjectBtn")) {
    documentObject.getElementById("loadProjectBtn").onclick = loadEditorProject;
  }
  if (documentObject.getElementById("exportBtn")) {
    documentObject.getElementById("exportBtn").onclick = () => {
      documentObject.getElementById("jsonBox").value = JSON.stringify(cloneEditorMap(state.map), null, 2);
    };
  }
  if (documentObject.getElementById("exportProjectBtn")) {
    documentObject.getElementById("exportProjectBtn").onclick = () => {
      documentObject.getElementById("jsonBox").value = JSON.stringify(buildEditorProject(), null, 2);
      addLog("편집기 프로젝트 JSON을 내보냈다.");
    };
  }
  if (documentObject.getElementById("compileBtn")) {
    documentObject.getElementById("compileBtn").onclick = () => {
      const result = buildCompiledMapForRuntime(state.map);
      if (!result.ok) {
        const firstIssue = firstValidationIssue(result.report);
        const issueText = firstIssue?.message ? ` · 먼저 볼 항목: ${firstIssue.message}` : "";
        const repairHint = firstIssue ? ` · 힌트: ${validationIssueRepairHint(firstIssue)}` : "";
        addLog(`compiledMap 내보내기 실패: ${validationSummaryText(result.report)}${issueText}${repairHint}`);
        return render();
      }
      documentObject.getElementById("jsonBox").value = JSON.stringify(result.compiledMap, null, 2);
      addLog(`${state.map.name} compiledMap을 내보냈다.`);
    };
  }
  if (documentObject.getElementById("exportManifestBtn")) {
    documentObject.getElementById("exportManifestBtn").onclick = () => {
      const result = buildContentBuildManifest(state.floorMaps);
      if (!result.ok) {
        const firstIssue = firstValidationIssue(result.report);
        const issueText = firstIssue?.message ? ` · 먼저 볼 항목: ${firstIssue.message}` : "";
        const repairHint = firstIssue ? ` · 힌트: ${validationIssueRepairHint(firstIssue)}` : "";
        addLog(`contentBuildManifest 내보내기 실패: ${validationSummaryText(result.report)}${issueText}${repairHint}`);
        return render();
      }
      documentObject.getElementById("jsonBox").value = JSON.stringify(result.manifest, null, 2);
      addLog("contentBuildManifest를 내보냈다.");
    };
  }
  if (documentObject.getElementById("importBtn")) {
    documentObject.getElementById("importBtn").onclick = () => {
      try {
        const map = JSON.parse(documentObject.getElementById("jsonBox").value);
        normalizeMapMetadata(map);
        computeWalls(map);
        const floor = map.start.floor || state.player.floor;
        state.map = map;
        state.floorMaps[floor] = map;
        state.player = { floor, x: map.start.x, y: map.start.y, facing: map.start.facing };
        state.visited = new Set([`${map.start.x},${map.start.y}`]);
        state.visitedByFloor[floor] = state.visited;
        state.roomRangeStart = null;
        addLog("JSON 맵을 불러왔다.");
      } catch (error) {
        addLog(`JSON 불러오기 실패: ${error.message}`);
      }
      render();
    };
  }
  if (documentObject.getElementById("importProjectBtn")) {
    documentObject.getElementById("importProjectBtn").onclick = () => {
      try {
        applyEditorProject(JSON.parse(documentObject.getElementById("jsonBox").value));
        addLog("편집기 프로젝트 JSON을 불러왔다.");
      } catch (error) {
        addLog(`편집기 프로젝트 JSON 불러오기 실패: ${error.message}`);
      }
      render();
    };
  }
}

export function bindPresetDraftControls(deps = {}) {
  const {
    state,
    presetDraftSize = 0,
    render = () => {},
    documentObject = document,
    addLog = () => {},
    createEmptyPresetGrid = () => [],
    createDraftPreset = () => ({}),
    draftFromGrid = () => ({}),
    upsertCustomPreset = () => {},
    refreshPresetCatalog = () => {},
    deleteCustomPreset = () => {},
  } = deps;

  const draftGrid = documentObject.getElementById("presetDraftGrid");
  if (draftGrid) {
    draftGrid.style.gridTemplateColumns = `repeat(${presetDraftSize}, 16px)`;
    for (let y = 0; y < presetDraftSize; y++) {
      for (let x = 0; x < presetDraftSize; x++) {
        const cell = documentObject.createElement("button");
        cell.className = `draft-cell ${state.presetDraftGrid[y][x] ? "is-filled" : ""}`;
        cell.onclick = () => {
          state.presetDraftGrid[y][x] = state.presetDraftGrid[y][x] ? 0 : 1;
          render();
        };
        draftGrid.appendChild(cell);
      }
    }
  }
  if (documentObject.getElementById("clearPresetDraftBtn")) {
    documentObject.getElementById("clearPresetDraftBtn").onclick = () => {
      state.presetDraftGrid = createEmptyPresetGrid(presetDraftSize, presetDraftSize);
      state.presetDraft = createDraftPreset(presetDraftSize, presetDraftSize);
      state.presetDraftSelectedId = "";
      render();
    };
  }
  if (documentObject.getElementById("savePresetBtn")) {
    documentObject.getElementById("savePresetBtn").onclick = () => {
      const saved = draftFromGrid(state.presetDraftGrid, {
        id: documentObject.getElementById("presetId").value.trim() || `custom_${Date.now()}`,
        name: documentObject.getElementById("presetName").value.trim() || "Custom Block",
        kind: "custom",
        tags: documentObject.getElementById("presetTags").value.split(",").map((tag) => tag.trim()).filter(Boolean),
      });
      upsertCustomPreset(saved);
      refreshPresetCatalog(false);
      state.selectedPresetId = saved.id;
      state.presetDraftSelectedId = saved.id;
      addLog(`${saved.name} 프리셋을 저장했다.`);
      render();
    };
  }
  if (documentObject.getElementById("deletePresetBtn")) {
    documentObject.getElementById("deletePresetBtn").onclick = () => {
      if (!state.presetDraftSelectedId) return;
      deleteCustomPreset(state.presetDraftSelectedId);
      refreshPresetCatalog(false);
      state.presetDraftSelectedId = "";
      state.presetDraft = createDraftPreset(presetDraftSize, presetDraftSize);
      state.presetDraftGrid = createEmptyPresetGrid(presetDraftSize, presetDraftSize);
      addLog("커스텀 프리셋을 삭제했다.");
      render();
    };
  }
}

export function bindItemAuthoringSupportControls(deps = {}) {
  const {
    state,
    rarityDef = null,
    rarityDefId = "",
    affixDef = null,
    affixDefId = "",
    affixPoolDef = null,
    affixPoolId = "",
    itemDefId = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    rarityDefinitions = {},
    affixDefinitions = {},
    affixPoolDefinitions = {},
    parseJsonField = () => ({}),
    uniqueSchemaId = () => "",
    createRarityDefinitionTemplate = () => ({}),
    updateRarityDefinition = () => {},
    replaceRarityDefinitions = () => {},
    createAffixDefinitionTemplate = () => ({}),
    updateAffixDefinition = () => {},
    replaceAffixDefinitions = () => {},
    createAffixPoolDefinitionTemplate = () => ({}),
    updateAffixPoolDefinition = () => {},
    replaceAffixPoolDefinitions = () => {},
    buildSampleItemPreview = () => null,
    makeGeneratedItemInstance = () => null,
    pushInventoryEntry = () => {},
    inventoryEntryLabel = () => "",
  } = deps;

  if (documentObject.getElementById("addRarityDefinitionBtn")) {
    documentObject.getElementById("addRarityDefinitionBtn").onclick = () => {
      const newId = uniqueSchemaId(Object.keys(rarityDefinitions), "rarity_custom");
      rarityDefinitions[newId] = createRarityDefinitionTemplate();
      state.selectedRarityDefinitionId = newId;
      render();
    };
  }
  if (rarityDef) {
    documentObject.getElementById("duplicateRarityDefinitionBtn").onclick = () => {
      const newId = uniqueSchemaId(Object.keys(rarityDefinitions), rarityDefId);
      rarityDefinitions[newId] = JSON.parse(JSON.stringify(rarityDefinitions[rarityDefId]));
      state.selectedRarityDefinitionId = newId;
      render();
    };
    documentObject.getElementById("removeRarityDefinitionBtn").onclick = () => {
      delete rarityDefinitions[rarityDefId];
      state.selectedRarityDefinitionId = Object.keys(rarityDefinitions)[0] || "";
      render();
    };
    documentObject.getElementById("rarityDefinitionSelect").onchange = (e) => {
      state.selectedRarityDefinitionId = e.target.value;
      render();
    };
    documentObject.getElementById("rarityDefinitionLabelInput").oninput = (e) => updateRarityDefinition(rarityDefId, (entry) => {
      entry.label = e.target.value.trim();
    });
    documentObject.getElementById("rarityDefinitionWeightInput").onchange = (e) => updateRarityDefinition(rarityDefId, (entry) => {
      entry.weight = Math.max(0, Number(e.target.value || 0));
    });
    documentObject.getElementById("rarityDefinitionValueMultiplierInput").onchange = (e) => updateRarityDefinition(rarityDefId, (entry) => {
      entry.valueMultiplier = Math.max(0, Number(e.target.value || 0));
    });
    documentObject.getElementById("rarityDefinitionAffixCountInput").onchange = (e) => updateRarityDefinition(rarityDefId, (entry) => {
      entry.affixCount = Math.max(0, Number(e.target.value || 0));
    });
  }
  if (documentObject.getElementById("rarityDefinitionsJsonInput")) {
    documentObject.getElementById("rarityDefinitionsJsonInput").onchange = (e) => {
      try {
        replaceRarityDefinitions(parseJsonField(e.target.value, {}));
        state.selectedRarityDefinitionId = rarityDefinitions[state.selectedRarityDefinitionId] ? state.selectedRarityDefinitionId : Object.keys(rarityDefinitions)[0] || "";
        render();
      } catch (error) {
        addLog(`rarity definitions JSON 파싱 실패: ${error.message}`);
        render();
      }
    };
  }

  if (documentObject.getElementById("addPrefixAffixBtn")) {
    documentObject.getElementById("addPrefixAffixBtn").onclick = () => {
      const newId = uniqueSchemaId(Object.keys(affixDefinitions), "affix_prefix");
      affixDefinitions[newId] = createAffixDefinitionTemplate("prefix");
      state.selectedAffixDefinitionId = newId;
      render();
    };
  }
  if (documentObject.getElementById("addSuffixAffixBtn")) {
    documentObject.getElementById("addSuffixAffixBtn").onclick = () => {
      const newId = uniqueSchemaId(Object.keys(affixDefinitions), "affix_suffix");
      affixDefinitions[newId] = createAffixDefinitionTemplate("suffix");
      state.selectedAffixDefinitionId = newId;
      render();
    };
  }
  if (affixDef) {
    documentObject.getElementById("duplicateAffixDefinitionBtn").onclick = () => {
      const newId = uniqueSchemaId(Object.keys(affixDefinitions), affixDefId);
      affixDefinitions[newId] = JSON.parse(JSON.stringify(affixDefinitions[affixDefId]));
      state.selectedAffixDefinitionId = newId;
      render();
    };
    documentObject.getElementById("removeAffixDefinitionBtn").onclick = () => {
      delete affixDefinitions[affixDefId];
      state.selectedAffixDefinitionId = Object.keys(affixDefinitions)[0] || "";
      render();
    };
    documentObject.getElementById("affixDefinitionSelect").onchange = (e) => {
      state.selectedAffixDefinitionId = e.target.value;
      render();
    };
    documentObject.getElementById("affixDefinitionLabelInput").oninput = (e) => updateAffixDefinition(affixDefId, (entry) => {
      entry.label = e.target.value.trim();
    });
    documentObject.getElementById("affixDefinitionSlotSelect").onchange = (e) => updateAffixDefinition(affixDefId, (entry) => {
      entry.slot = e.target.value;
    });
    documentObject.getElementById("affixDefinitionStatSelect").onchange = (e) => updateAffixDefinition(affixDefId, (entry) => {
      entry.stat = e.target.value;
    });
    documentObject.getElementById("affixDefinitionAmountInput").onchange = (e) => updateAffixDefinition(affixDefId, (entry) => {
      entry.amount = Number(e.target.value || 0);
    });
    documentObject.getElementById("affixDefinitionValueInput").oninput = (e) => updateAffixDefinition(affixDefId, (entry) => {
      if (e.target.value.trim()) entry.value = e.target.value.trim();
      else delete entry.value;
    });
    documentObject.getElementById("affixDefinitionRaritySelect").onchange = (e) => updateAffixDefinition(affixDefId, (entry) => {
      entry.rarity = e.target.value;
    });
  }
  if (documentObject.getElementById("affixDefinitionsJsonInput")) {
    documentObject.getElementById("affixDefinitionsJsonInput").onchange = (e) => {
      try {
        replaceAffixDefinitions(parseJsonField(e.target.value, {}));
        state.selectedAffixDefinitionId = affixDefinitions[state.selectedAffixDefinitionId] ? state.selectedAffixDefinitionId : Object.keys(affixDefinitions)[0] || "";
        render();
      } catch (error) {
        addLog(`affix definitions JSON 파싱 실패: ${error.message}`);
        render();
      }
    };
  }

  if (documentObject.getElementById("addAffixPoolBtn")) {
    documentObject.getElementById("addAffixPoolBtn").onclick = () => {
      const newId = uniqueSchemaId(Object.keys(affixPoolDefinitions), "affix_pool");
      affixPoolDefinitions[newId] = createAffixPoolDefinitionTemplate();
      state.selectedAffixPoolId = newId;
      render();
    };
  }
  if (affixPoolDef) {
    documentObject.getElementById("duplicateAffixPoolBtn").onclick = () => {
      const newId = uniqueSchemaId(Object.keys(affixPoolDefinitions), affixPoolId);
      affixPoolDefinitions[newId] = JSON.parse(JSON.stringify(affixPoolDefinitions[affixPoolId]));
      state.selectedAffixPoolId = newId;
      render();
    };
    documentObject.getElementById("removeAffixPoolBtn").onclick = () => {
      delete affixPoolDefinitions[affixPoolId];
      state.selectedAffixPoolId = Object.keys(affixPoolDefinitions)[0] || "";
      render();
    };
    documentObject.getElementById("affixPoolDefinitionSelect").onchange = (e) => {
      state.selectedAffixPoolId = e.target.value;
      render();
    };
    documentObject.getElementById("affixPoolLabelInput").oninput = (e) => updateAffixPoolDefinition(affixPoolId, (entry) => {
      entry.label = e.target.value.trim();
    });
    documentObject.getElementById("affixPoolItemKindsInput").onchange = (e) => updateAffixPoolDefinition(affixPoolId, (entry) => {
      entry.itemKinds = e.target.value.split(",").map((value) => value.trim()).filter(Boolean);
    });
    if (documentObject.getElementById("addAffixPoolAffixBtn")) {
      documentObject.getElementById("addAffixPoolAffixBtn").onclick = () => {
        updateAffixPoolDefinition(affixPoolId, (entry) => {
          entry.affixIds = Array.isArray(entry.affixIds) ? entry.affixIds : [];
          entry.affixIds.push(Object.keys(affixDefinitions)[0] || "");
        });
        render();
      };
    }
    documentObject.querySelectorAll("[data-affix-pool-affix-id]").forEach((select) => {
      select.onchange = (e) => updateAffixPoolDefinition(affixPoolId, (entry) => {
        entry.affixIds = Array.isArray(entry.affixIds) ? entry.affixIds : [];
        const index = Number(e.target.dataset.affixPoolAffixId || 0);
        if (index >= 0 && index < entry.affixIds.length) entry.affixIds[index] = e.target.value;
      });
    });
    documentObject.querySelectorAll("[data-remove-affix-pool-affix]").forEach((button) => {
      button.onclick = () => {
        updateAffixPoolDefinition(affixPoolId, (entry) => {
          entry.affixIds = Array.isArray(entry.affixIds) ? entry.affixIds : [];
          const index = Number(button.dataset.removeAffixPoolAffix || 0);
          if (index >= 0 && index < entry.affixIds.length) entry.affixIds.splice(index, 1);
        });
        render();
      };
    });
  }
  if (documentObject.getElementById("affixPoolDefinitionsJsonInput")) {
    documentObject.getElementById("affixPoolDefinitionsJsonInput").onchange = (e) => {
      try {
        replaceAffixPoolDefinitions(parseJsonField(e.target.value, {}));
        state.selectedAffixPoolId = affixPoolDefinitions[state.selectedAffixPoolId] ? state.selectedAffixPoolId : Object.keys(affixPoolDefinitions)[0] || "";
        render();
      } catch (error) {
        addLog(`affix pool definitions JSON 파싱 실패: ${error.message}`);
        render();
      }
    };
  }

  if (documentObject.getElementById("sampleItemBaseSelect")) {
    documentObject.getElementById("sampleItemBaseSelect").onchange = (e) => {
      state.selectedItemDefinitionId = e.target.value;
      render();
    };
  }
  if (documentObject.getElementById("sampleItemRaritySelect")) {
    documentObject.getElementById("sampleItemRaritySelect").onchange = (e) => {
      state.selectedRarityDefinitionId = e.target.value;
      render();
    };
  }
  if (documentObject.getElementById("sampleItemAffixPoolSelect")) {
    documentObject.getElementById("sampleItemAffixPoolSelect").onchange = (e) => {
      state.selectedAffixPoolId = e.target.value;
      render();
    };
  }
  if (documentObject.getElementById("generateSampleItemBtn")) {
    documentObject.getElementById("generateSampleItemBtn").onclick = () => {
      state.editor.sampleItemPreview = buildSampleItemPreview(itemDefId, rarityDefId, affixPoolId);
      render();
    };
  }
  if (documentObject.getElementById("pushSampleItemToInventoryBtn")) {
    documentObject.getElementById("pushSampleItemToInventoryBtn").onclick = () => {
      const generated = makeGeneratedItemInstance(state.editor.sampleItemPreview);
      if (!generated) {
        addLog("sample preview가 없어 generated item instance를 만들지 못했다.");
        return render();
      }
      pushInventoryEntry(generated);
      addLog(`${inventoryEntryLabel(generated)}를 가방에 추가했다.`);
      render();
    };
  }
  if (documentObject.getElementById("clearSampleItemBtn")) {
    documentObject.getElementById("clearSampleItemBtn").onclick = () => {
      state.editor.sampleItemPreview = null;
      render();
    };
  }
}

export function bindItemDefinitionEditorControls(deps = {}) {
  const {
    state,
    itemDef = null,
    itemDefId = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    items = {},
    parseJsonField = () => ({}),
    replaceItemDefinitions = () => {},
    activeItemDefinitionId = () => "",
    uniqueItemDefinitionId = () => "",
    createItemDefinitionTemplate = () => ({}),
    updateItemDefinition = () => {},
  } = deps;

  if (documentObject.getElementById("addConsumableItemBtn")) {
    documentObject.getElementById("addConsumableItemBtn").onclick = () => {
      const newId = uniqueItemDefinitionId("item_consumable");
      items[newId] = createItemDefinitionTemplate("consumable");
      state.selectedItemDefinitionId = newId;
      render();
    };
  }
  if (documentObject.getElementById("addArtifactItemBtn")) {
    documentObject.getElementById("addArtifactItemBtn").onclick = () => {
      const newId = uniqueItemDefinitionId("item_artifact");
      items[newId] = createItemDefinitionTemplate("artifact");
      state.selectedItemDefinitionId = newId;
      render();
    };
  }
  if (documentObject.getElementById("addQuestItemBtn")) {
    documentObject.getElementById("addQuestItemBtn").onclick = () => {
      const newId = uniqueItemDefinitionId("item_quest");
      items[newId] = createItemDefinitionTemplate("quest");
      state.selectedItemDefinitionId = newId;
      render();
    };
  }

  if (!itemDef) return;

  documentObject.getElementById("itemDefinitionSelect").onchange = (e) => {
    state.selectedItemDefinitionId = e.target.value;
    render();
  };
  documentObject.getElementById("duplicateItemDefinitionBtn").onclick = () => {
    const sourceId = activeItemDefinitionId();
    if (!sourceId || !items[sourceId]) return;
    const newId = uniqueItemDefinitionId(sourceId);
    items[newId] = JSON.parse(JSON.stringify(items[sourceId]));
    items[newId].name = `${items[sourceId].name || sourceId} 복제`;
    state.selectedItemDefinitionId = newId;
    render();
  };
  documentObject.getElementById("removeItemDefinitionBtn").onclick = () => {
    const sourceId = activeItemDefinitionId();
    if (!sourceId || !items[sourceId]) return;
    delete items[sourceId];
    state.selectedItemDefinitionId = Object.keys(items)[0] || "";
    render();
  };
  documentObject.getElementById("itemDefinitionNameInput").oninput = (e) => {
    updateItemDefinition(itemDefId, (entry) => {
      entry.name = e.target.value.trim();
    });
  };
  documentObject.getElementById("itemDefinitionKindSelect").onchange = (e) => {
    updateItemDefinition(itemDefId, (entry) => {
      const next = createItemDefinitionTemplate(e.target.value);
      next.name = entry.name || next.name;
      if (entry.rarity) next.rarity = entry.rarity;
      Object.keys(entry).forEach((key) => delete entry[key]);
      Object.assign(entry, next);
    });
    render();
  };
  documentObject.getElementById("itemDefinitionsJsonInput").onchange = (e) => {
    try {
      const definitions = parseJsonField(e.target.value, {});
      replaceItemDefinitions(definitions);
      state.selectedItemDefinitionId = items[state.selectedItemDefinitionId] ? state.selectedItemDefinitionId : Object.keys(items)[0] || "";
      render();
    } catch (error) {
      addLog(`item definitions JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  if (documentObject.getElementById("itemDefinitionHealInput")) {
    documentObject.getElementById("itemDefinitionHealInput").onchange = (e) => {
      updateItemDefinition(itemDefId, (entry) => {
        entry.heal = Number(e.target.value || 0);
      });
    };
  }
  if (documentObject.getElementById("itemDefinitionCureInput")) {
    documentObject.getElementById("itemDefinitionCureInput").oninput = (e) => {
      updateItemDefinition(itemDefId, (entry) => {
        if (e.target.value.trim()) entry.cure = e.target.value.trim();
        else delete entry.cure;
      });
    };
  }
  if (documentObject.getElementById("itemDefinitionAttackInput")) {
    documentObject.getElementById("itemDefinitionAttackInput").onchange = (e) => {
      updateItemDefinition(itemDefId, (entry) => {
        entry.attack = Number(e.target.value || 0);
      });
    };
  }
  if (documentObject.getElementById("itemDefinitionDefenseInput")) {
    documentObject.getElementById("itemDefinitionDefenseInput").onchange = (e) => {
      updateItemDefinition(itemDefId, (entry) => {
        entry.defense = Number(e.target.value || 0);
      });
    };
  }
  if (documentObject.getElementById("itemDefinitionCurseInput")) {
    documentObject.getElementById("itemDefinitionCurseInput").onchange = (e) => {
      updateItemDefinition(itemDefId, (entry) => {
        entry.curse = Number(e.target.value || 0);
      });
    };
  }
  if (documentObject.getElementById("itemDefinitionSlotInput")) {
    documentObject.getElementById("itemDefinitionSlotInput").oninput = (e) => {
      updateItemDefinition(itemDefId, (entry) => {
        if (e.target.value.trim()) entry.slot = e.target.value.trim();
        else delete entry.slot;
      });
    };
  }
  documentObject.getElementById("itemDefinitionRarityInput").oninput = (e) => {
    updateItemDefinition(itemDefId, (entry) => {
      if (e.target.value.trim()) entry.rarity = e.target.value.trim();
      else delete entry.rarity;
    });
  };
}

export function bindMonsterDefinitionEditorControls(deps = {}) {
  const {
    state,
    monsterDef = null,
    monsterDefId = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    monsters = {},
    parseJsonField = () => ({}),
    replaceMonsterDefinitions = () => {},
    activeMonsterDefinitionId = () => "",
    uniqueMonsterDefinitionId = () => "",
    createMonsterDefinitionTemplate = () => ({}),
    updateMonsterDefinition = () => {},
  } = deps;

  if (documentObject.getElementById("addMonsterDefinitionBtn")) {
    documentObject.getElementById("addMonsterDefinitionBtn").onclick = () => {
      const newId = uniqueMonsterDefinitionId("monster_custom");
      monsters[newId] = createMonsterDefinitionTemplate();
      state.selectedMonsterDefinitionId = newId;
      render();
    };
  }

  if (!monsterDef) return;

  documentObject.getElementById("monsterDefinitionSelect").onchange = (e) => {
    state.selectedMonsterDefinitionId = e.target.value;
    render();
  };
  documentObject.getElementById("duplicateMonsterDefinitionBtn").onclick = () => {
    const sourceId = activeMonsterDefinitionId();
    if (!sourceId || !monsters[sourceId]) return;
    const newId = uniqueMonsterDefinitionId(sourceId);
    monsters[newId] = JSON.parse(JSON.stringify(monsters[sourceId]));
    monsters[newId].name = `${monsters[sourceId].name || sourceId} 복제`;
    state.selectedMonsterDefinitionId = newId;
    render();
  };
  documentObject.getElementById("removeMonsterDefinitionBtn").onclick = () => {
    const sourceId = activeMonsterDefinitionId();
    if (!sourceId || !monsters[sourceId]) return;
    delete monsters[sourceId];
    state.selectedMonsterDefinitionId = Object.keys(monsters)[0] || "";
    render();
  };
  documentObject.getElementById("monsterDefinitionsJsonInput").onchange = (e) => {
    try {
      replaceMonsterDefinitions(parseJsonField(e.target.value, {}));
      state.selectedMonsterDefinitionId = monsters[state.selectedMonsterDefinitionId] ? state.selectedMonsterDefinitionId : Object.keys(monsters)[0] || "";
      render();
    } catch (error) {
      addLog(`monster definitions JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  const setNumberField = (id, field, options = {}) => {
    const input = documentObject.getElementById(id);
    if (!input) return;
    input.onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
      if (options.optional && e.target.value === "") delete entry[field];
      else entry[field] = Math.max(options.min ?? 0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("monsterDefinitionNameInput").oninput = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.name = e.target.value.trim();
  });
  documentObject.getElementById("monsterDefinitionAiInput").oninput = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    if (e.target.value.trim()) entry.ai = e.target.value.trim();
    else delete entry.ai;
  });
  setNumberField("monsterDefinitionHpInput", "hp", { min: 1 });
  setNumberField("monsterDefinitionAtkInput", "atk", { min: 1 });
  setNumberField("monsterDefinitionDefInput", "def", { min: 0 });
  setNumberField("monsterDefinitionXpInput", "xp", { min: 0 });
  setNumberField("monsterDefinitionAtkMinInput", "atkMin", { min: 0, optional: true });
  setNumberField("monsterDefinitionAtkMaxInput", "atkMax", { min: 0, optional: true });
  documentObject.getElementById("monsterDefinitionMapKindsInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.spawn = entry.spawn || {};
    entry.spawn.mapKinds = e.target.value.split(",").map((value) => value.trim()).filter(Boolean);
  });
  documentObject.getElementById("monsterDefinitionRolesInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.spawn = entry.spawn || {};
    entry.spawn.roles = e.target.value.split(",").map((value) => value.trim()).filter(Boolean);
  });
  documentObject.getElementById("monsterDefinitionMinFloorInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.spawn = entry.spawn || {};
    entry.spawn.minFloor = Math.max(1, Number(e.target.value || 1));
  });
  documentObject.getElementById("monsterDefinitionMaxFloorInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.spawn = entry.spawn || {};
    if (e.target.value === "") delete entry.spawn.maxFloor;
    else entry.spawn.maxFloor = Math.max(1, Number(e.target.value || 1));
  });
  documentObject.getElementById("monsterDefinitionSpawnWeightInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.spawn = entry.spawn || {};
    entry.spawn.weight = Math.max(1, Number(e.target.value || 1));
  });
  documentObject.getElementById("monsterDefinitionHpScaleInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.scaling = entry.scaling || {};
    entry.scaling.hpPerFloor = Math.max(0, Number(e.target.value || 0));
  });
  documentObject.getElementById("monsterDefinitionAtkScaleInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    entry.scaling = entry.scaling || {};
    entry.scaling.atkPerFloor = Math.max(0, Number(e.target.value || 0));
  });
  documentObject.getElementById("monsterDefinitionBossInput").onchange = (e) => updateMonsterDefinition(monsterDefId, (entry) => {
    if (e.target.checked) entry.boss = true;
    else delete entry.boss;
  });
}

export function bindSkillDefinitionEditorControls(deps = {}) {
  const {
    state,
    skillDef = null,
    skillDefId = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    skills = {},
    parseJsonField = () => ({}),
    replaceSkillDefinitions = () => {},
    activeSkillDefinitionId = () => "",
    uniqueSkillDefinitionId = () => "",
    createSkillDefinitionTemplate = () => ({}),
    updateSkillDefinition = () => {},
  } = deps;

  const addSkill = (buttonId, kind, baseId) => {
    const button = documentObject.getElementById(buttonId);
    if (!button) return;
    button.onclick = () => {
      const newId = uniqueSkillDefinitionId(baseId);
      skills[newId] = createSkillDefinitionTemplate(kind);
      state.selectedSkillDefinitionId = newId;
      render();
    };
  };
  addSkill("addAttackSkillDefinitionBtn", "attack", "skill_attack");
  addSkill("addHealSkillDefinitionBtn", "heal", "skill_heal");
  addSkill("addBuffSkillDefinitionBtn", "buff", "skill_buff");
  addSkill("addDebuffSkillDefinitionBtn", "debuff", "skill_debuff");
  addSkill("addLifestealSkillDefinitionBtn", "lifesteal", "skill_lifesteal");

  if (!skillDef) return;

  documentObject.getElementById("skillDefinitionSelect").onchange = (e) => {
    state.selectedSkillDefinitionId = e.target.value;
    render();
  };
  documentObject.getElementById("duplicateSkillDefinitionBtn").onclick = () => {
    const sourceId = activeSkillDefinitionId();
    if (!sourceId || !skills[sourceId]) return;
    const newId = uniqueSkillDefinitionId(sourceId);
    skills[newId] = JSON.parse(JSON.stringify(skills[sourceId]));
    skills[newId].name = `${skills[sourceId].name || sourceId} 복제`;
    state.selectedSkillDefinitionId = newId;
    render();
  };
  documentObject.getElementById("removeSkillDefinitionBtn").onclick = () => {
    const sourceId = activeSkillDefinitionId();
    if (!sourceId || !skills[sourceId]) return;
    delete skills[sourceId];
    state.selectedSkillDefinitionId = Object.keys(skills)[0] || "";
    render();
  };
  documentObject.getElementById("skillDefinitionsJsonInput").onchange = (e) => {
    try {
      replaceSkillDefinitions(parseJsonField(e.target.value, {}));
      state.selectedSkillDefinitionId = skills[state.selectedSkillDefinitionId] ? state.selectedSkillDefinitionId : Object.keys(skills)[0] || "";
      render();
    } catch (error) {
      addLog(`skill definitions JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  const setStringField = (id, field) => {
    const input = documentObject.getElementById(id);
    if (!input) return;
    input.oninput = (e) => updateSkillDefinition(skillDefId, (entry) => {
      const value = e.target.value.trim();
      if (value) entry[field] = value;
      else delete entry[field];
    });
  };
  const setNumberField = (id, field) => {
    const input = documentObject.getElementById(id);
    if (!input) return;
    input.onchange = (e) => updateSkillDefinition(skillDefId, (entry) => {
      const value = Number(e.target.value || 0);
      if (value || field === "effect" || field === "cooldown") entry[field] = value;
      else delete entry[field];
    });
  };
  documentObject.getElementById("skillDefinitionNameInput").oninput = (e) => updateSkillDefinition(skillDefId, (entry) => {
    entry.name = e.target.value.trim();
  });
  documentObject.getElementById("skillDefinitionKindSelect").onchange = (e) => updateSkillDefinition(skillDefId, (entry) => {
    entry.kind = e.target.value;
  });
  documentObject.getElementById("skillDefinitionTargetModeSelect").onchange = (e) => updateSkillDefinition(skillDefId, (entry) => {
    entry.targetMode = e.target.value;
  });
  documentObject.getElementById("skillDefinitionFormulaSelect").onchange = (e) => updateSkillDefinition(skillDefId, (entry) => {
    entry.formula = e.target.value;
  });
  setNumberField("skillDefinitionEffectInput", "effect");
  setNumberField("skillDefinitionCooldownInput", "cooldown");
  setNumberField("skillDefinitionDurationInput", "duration");
  setNumberField("skillDefinitionBuyPriceInput", "buyPrice");
  setNumberField("skillDefinitionSellPriceInput", "sellPrice");
  setStringField("skillDefinitionStatusInput", "status");
  documentObject.getElementById("skillDefinitionCatalogIdsInput").onchange = (e) => updateSkillDefinition(skillDefId, (entry) => {
    entry.catalogIds = e.target.value.split(",").map((value) => value.trim()).filter(Boolean);
  });
  documentObject.getElementById("skillDefinitionTagsInput").onchange = (e) => updateSkillDefinition(skillDefId, (entry) => {
    entry.tags = e.target.value.split(",").map((value) => value.trim()).filter(Boolean);
  });
  documentObject.getElementById("skillDefinitionDescriptionInput").oninput = (e) => updateSkillDefinition(skillDefId, (entry) => {
    entry.description = e.target.value.trim();
  });
  documentObject.getElementById("skillDefinitionDeferredInput").onchange = (e) => updateSkillDefinition(skillDefId, (entry) => {
    if (e.target.checked) entry.deferred = true;
    else delete entry.deferred;
  });
}

export function bindQuestDefinitionEditorControls(deps = {}) {
  const {
    state,
    questDef = null,
    questDefId = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    questDefinitions = {},
    parseJsonField = () => ({}),
    replaceQuestDefinitions = () => {},
    activeQuestDefinitionId = () => "",
    uniqueQuestDefinitionId = () => "",
    createQuestDefinitionTemplate = () => ({}),
    updateQuestDefinition = () => {},
    buildGeneratedQuestDefinition = () => null,
  } = deps;

  if (documentObject.getElementById("addQuestDefinitionBtn")) {
    documentObject.getElementById("addQuestDefinitionBtn").onclick = () => {
      const newId = uniqueQuestDefinitionId("quest_custom");
      questDefinitions[newId] = createQuestDefinitionTemplate();
      state.selectedQuestDefinitionId = newId;
      render();
    };
  }
  if (documentObject.getElementById("addGeneratedQuestDefinitionBtn")) {
    documentObject.getElementById("addGeneratedQuestDefinitionBtn").onclick = () => {
      const generated = buildGeneratedQuestDefinition(questDef ? {
        archetype: questDef.generator?.archetype,
        mapKind: questDef.generator?.mapKind || questDef.mapKind,
        floor: questDef.generator?.floor || questDef.startFloor,
        targetMonsterId: questDef.generator?.targetMonsterId,
      } : {});
      if (!generated) {
        addLog("quest generator가 조건에 맞는 몬스터를 찾지 못했다.");
        render();
        return;
      }
      questDefinitions[generated.id] = generated.definition;
      state.selectedQuestDefinitionId = generated.id;
      render();
    };
  }
  if (!questDef) return;
  documentObject.getElementById("questDefinitionSelect").onchange = (e) => {
    state.selectedQuestDefinitionId = e.target.value;
    render();
  };
  documentObject.getElementById("duplicateQuestDefinitionBtn").onclick = () => {
    const sourceId = activeQuestDefinitionId();
    if (!sourceId || !questDefinitions[sourceId]) return;
    const newId = uniqueQuestDefinitionId(sourceId);
    questDefinitions[newId] = JSON.parse(JSON.stringify(questDefinitions[sourceId]));
    questDefinitions[newId].name = `${questDefinitions[sourceId].name || sourceId} 복제`;
    state.selectedQuestDefinitionId = newId;
    render();
  };
  documentObject.getElementById("removeQuestDefinitionBtn").onclick = () => {
    const sourceId = activeQuestDefinitionId();
    if (!sourceId || !questDefinitions[sourceId]) return;
    delete questDefinitions[sourceId];
    state.selectedQuestDefinitionId = Object.keys(questDefinitions)[0] || "";
    render();
  };
  documentObject.getElementById("questDefinitionsJsonInput").onchange = (e) => {
    try {
      replaceQuestDefinitions(parseJsonField(e.target.value, {}));
      state.selectedQuestDefinitionId = questDefinitions[state.selectedQuestDefinitionId] ? state.selectedQuestDefinitionId : Object.keys(questDefinitions)[0] || "";
      render();
    } catch (error) {
      addLog(`quest definitions JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  documentObject.getElementById("questDefinitionNameInput").oninput = (e) => updateQuestDefinition(questDefId, (entry) => { entry.name = e.target.value.trim(); });
  documentObject.getElementById("questDefinitionDescriptionInput").oninput = (e) => updateQuestDefinition(questDefId, (entry) => { entry.description = e.target.value.trim(); });
  documentObject.getElementById("questDefinitionMapKindInput").onchange = (e) => {
    updateQuestDefinition(questDefId, (entry) => {
      entry.mapKind = e.target.value.trim();
      entry.generator = entry.generator || {};
      entry.generator.mapKind = entry.mapKind;
    });
    render();
  };
  documentObject.getElementById("questDefinitionStartFloorInput").onchange = (e) => {
    updateQuestDefinition(questDefId, (entry) => {
      entry.startFloor = Math.max(1, Number(e.target.value || 1));
      entry.generator = entry.generator || {};
      entry.generator.floor = entry.startFloor;
    });
    render();
  };
  documentObject.getElementById("questDefinitionGeneratorArchetypeSelect").onchange = (e) => {
    updateQuestDefinition(questDefId, (entry) => {
      entry.generator = entry.generator || {};
      entry.generator.archetype = e.target.value === "subjugation" ? "subjugation" : "boss_hunt";
    });
    render();
  };
  documentObject.getElementById("questDefinitionGeneratorTargetSelect").onchange = (e) => {
    updateQuestDefinition(questDefId, (entry) => {
      entry.generator = entry.generator || {};
      entry.generator.targetMonsterId = e.target.value || "";
    });
  };
  documentObject.getElementById("generateQuestDefinitionBtn").onclick = () => {
    const generated = buildGeneratedQuestDefinition({
      questId: questDefId,
      archetype: questDef.generator?.archetype,
      mapKind: questDef.generator?.mapKind || questDef.mapKind,
      floor: questDef.generator?.floor || questDef.startFloor,
      targetMonsterId: questDef.generator?.targetMonsterId,
    });
    if (!generated) {
      addLog("quest generator가 조건에 맞는 몬스터를 찾지 못했다.");
      render();
      return;
    }
    questDefinitions[questDefId] = generated.definition;
    state.selectedQuestDefinitionId = questDefId;
    render();
  };
  documentObject.getElementById("questDefinitionConditionKindInput").onchange = (e) => {
    updateQuestDefinition(questDefId, (entry) => {
      entry.conditions = entry.conditions || {};
      entry.conditions.kind = e.target.value === "specific_monsters_defeated" ? "specific_monsters_defeated" : "bosses_defeated";
    });
    render();
  };
  documentObject.getElementById("questDefinitionConditionSummaryInput").oninput = (e) => updateQuestDefinition(questDefId, (entry) => {
    entry.conditions = entry.conditions || {};
    entry.conditions.summary = e.target.value.trim();
  });
  documentObject.getElementById("questDefinitionBossCountInput").onchange = (e) => updateQuestDefinition(questDefId, (entry) => {
    entry.conditions = entry.conditions || {};
    const nextCount = Math.max(1, Number(e.target.value || 1));
    if ((entry.conditions.kind || "bosses_defeated") === "specific_monsters_defeated") {
      entry.conditions.requiredCount = nextCount;
    } else {
      entry.conditions.kind = "bosses_defeated";
      entry.conditions.bossesDefeatedAtLeast = nextCount;
    }
  });
  documentObject.getElementById("questDefinitionTargetMonsterIdsInput").onchange = (e) => updateQuestDefinition(questDefId, (entry) => {
    entry.conditions = entry.conditions || {};
    entry.conditions.targetMonsterIds = e.target.value.split(",").map((value) => value.trim()).filter(Boolean);
  });
  documentObject.getElementById("questDefinitionGoldRewardInput").onchange = (e) => updateQuestDefinition(questDefId, (entry) => {
    entry.rewards = entry.rewards || {};
    entry.rewards.gold = Math.max(0, Number(e.target.value || 0));
  });
  documentObject.getElementById("questDefinitionXpRewardInput").onchange = (e) => updateQuestDefinition(questDefId, (entry) => {
    entry.rewards = entry.rewards || {};
    entry.rewards.xp = Math.max(0, Number(e.target.value || 0));
  });
  documentObject.getElementById("questDefinitionRewardFlagInput").oninput = (e) => updateQuestDefinition(questDefId, (entry) => {
    entry.rewards = entry.rewards || {};
    if (e.target.value.trim()) entry.rewards.flag = e.target.value.trim();
    else delete entry.rewards.flag;
  });
  documentObject.getElementById("questDefinitionRewardItemsInput").onchange = (e) => updateQuestDefinition(questDefId, (entry) => {
    entry.rewards = entry.rewards || {};
    entry.rewards.items = e.target.value.split(",")
      .map((value) => value.trim())
      .filter(Boolean)
      .map((value) => {
        const [itemId, quantity] = value.split(":").map((part) => part.trim());
        return { itemId, quantity: Math.max(1, Number(quantity || 1)) };
      });
  });
}

export function bindVendorDefinitionEditorControls(deps = {}) {
  const {
    state,
    vendorDef = null,
    vendorDefId = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    items = {},
    vendors = {},
    parseJsonField = () => ({}),
    replaceVendorDefinitions = () => {},
    activeVendorDefinitionId = () => "",
    uniqueVendorDefinitionId = () => "",
    createVendorDefinitionTemplate = () => ({}),
    updateVendorDefinition = () => {},
    ensureVendorInventoryEntryObject = () => null,
    setGeneratedEntryFields = () => {},
  } = deps;

  if (documentObject.getElementById("addSellBundleVendorBtn")) {
    documentObject.getElementById("addSellBundleVendorBtn").onclick = () => {
      const newId = uniqueVendorDefinitionId("vendor_bundle");
      vendors[newId] = createVendorDefinitionTemplate("sell_bundle");
      state.selectedVendorDefinitionId = newId;
      state.selectedVendorRotationIndex = 0;
      render();
    };
  }
  if (documentObject.getElementById("addTrainerVendorBtn")) {
    documentObject.getElementById("addTrainerVendorBtn").onclick = () => {
      const newId = uniqueVendorDefinitionId("vendor_trainer");
      vendors[newId] = createVendorDefinitionTemplate("train_party");
      state.selectedVendorDefinitionId = newId;
      state.selectedVendorRotationIndex = 0;
      render();
    };
  }

  if (!vendorDef) return;

  documentObject.getElementById("vendorDefinitionSelect").onchange = (e) => {
    state.selectedVendorDefinitionId = e.target.value;
    state.selectedVendorRotationIndex = 0;
    render();
  };
  documentObject.getElementById("vendorDefinitionServiceTypeSelect").onchange = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      const next = createVendorDefinitionTemplate(e.target.value);
      next.summary = entry.summary || next.summary;
      next.rotation = Array.isArray(entry.rotation) ? entry.rotation : [];
      Object.keys(entry).forEach((key) => delete entry[key]);
      Object.assign(entry, next);
    });
    render();
  };
  documentObject.getElementById("vendorDefinitionSummaryInput").oninput = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      entry.summary = e.target.value.trim();
    });
  };
  documentObject.getElementById("vendorDefinitionGoldCostInput").onchange = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      entry.cost = entry.cost || {};
      entry.cost.gold = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("vendorDefinitionsJsonInput").onchange = (e) => {
    try {
      const definitions = parseJsonField(e.target.value, {});
      replaceVendorDefinitions(definitions);
      state.selectedVendorDefinitionId = vendors[state.selectedVendorDefinitionId] ? state.selectedVendorDefinitionId : Object.keys(vendors)[0] || "";
      state.selectedVendorRotationIndex = 0;
      render();
    } catch (error) {
      addLog(`vendor definitions JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  if (documentObject.getElementById("duplicateVendorDefinitionBtn")) {
    documentObject.getElementById("duplicateVendorDefinitionBtn").onclick = () => {
      const sourceId = activeVendorDefinitionId();
      if (!sourceId || !vendors[sourceId]) return;
      const newId = uniqueVendorDefinitionId(sourceId);
      vendors[newId] = JSON.parse(JSON.stringify(vendors[sourceId]));
      state.selectedVendorDefinitionId = newId;
      state.selectedVendorRotationIndex = 0;
      render();
    };
  }
  if (documentObject.getElementById("removeVendorDefinitionBtn")) {
    documentObject.getElementById("removeVendorDefinitionBtn").onclick = () => {
      const sourceId = activeVendorDefinitionId();
      if (!sourceId || !vendors[sourceId]) return;
      delete vendors[sourceId];
      state.selectedVendorDefinitionId = Object.keys(vendors)[0] || "";
      state.selectedVendorRotationIndex = 0;
      render();
    };
  }
  if (documentObject.getElementById("addVendorBaseItemBtn")) {
    documentObject.getElementById("addVendorBaseItemBtn").onclick = () => {
      updateVendorDefinition(vendorDefId, (entry) => {
        entry.inventory = Array.isArray(entry.inventory) ? entry.inventory : [];
        entry.inventory.push(Object.keys(items)[0] || "");
      });
      render();
    };
  }
  documentObject.querySelectorAll("[data-vendor-base-item-id]").forEach((select) => {
    select.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        entry.inventory = Array.isArray(entry.inventory) ? entry.inventory : [];
        const index = Number(e.target.dataset.vendorBaseItemId || 0);
        if (index < 0 || index >= entry.inventory.length) return;
        if (typeof entry.inventory[index] === "string") entry.inventory[index] = e.target.value;
        else ensureVendorInventoryEntryObject(entry.inventory, index).itemId = e.target.value;
      });
    };
  });
  documentObject.querySelectorAll("[data-vendor-base-generated]").forEach((input) => {
    input.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        entry.inventory = Array.isArray(entry.inventory) ? entry.inventory : [];
        const index = Number(e.target.dataset.vendorBaseGenerated || 0);
        const target = ensureVendorInventoryEntryObject(entry.inventory, index);
        if (!target) return;
        setGeneratedEntryFields(target, e.target.checked);
      });
      render();
    };
  });
  documentObject.querySelectorAll("[data-vendor-base-rarity-id]").forEach((select) => {
    select.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const target = ensureVendorInventoryEntryObject(entry.inventory, Number(e.target.dataset.vendorBaseRarityId || 0));
        if (!target) return;
        target.rarityId = e.target.value;
      });
    };
  });
  documentObject.querySelectorAll("[data-vendor-base-affix-pool-id]").forEach((select) => {
    select.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const target = ensureVendorInventoryEntryObject(entry.inventory, Number(e.target.dataset.vendorBaseAffixPoolId || 0));
        if (!target) return;
        target.affixPoolId = e.target.value;
      });
    };
  });
  documentObject.querySelectorAll("[data-remove-vendor-base-item]").forEach((button) => {
    button.onclick = () => {
      updateVendorDefinition(vendorDefId, (entry) => {
        entry.inventory = Array.isArray(entry.inventory) ? entry.inventory : [];
        const index = Number(button.dataset.removeVendorBaseItem || 0);
        if (index < 0 || index >= entry.inventory.length) return;
        entry.inventory.splice(index, 1);
      });
      render();
    };
  });
}

export function bindVendorRotationEditorControls(deps = {}) {
  const {
    state,
    vendorDefId = "",
    selectedVendorRotation = null,
    selectedVendorRotationState = { index: 0, rotation: null },
    render = () => {},
    documentObject = document,
    items = {},
    updateVendorDefinition = () => {},
    createVendorRotationTemplate = () => ({}),
    ensureVendorInventoryEntryObject = () => null,
    setGeneratedEntryFields = () => {},
  } = deps;

  if (!vendorDefId) return;

  if (documentObject.getElementById("addVendorRotationBtn")) {
    documentObject.getElementById("addVendorRotationBtn").onclick = () => {
      updateVendorDefinition(vendorDefId, (entry) => {
        entry.rotation = Array.isArray(entry.rotation) ? entry.rotation : [];
        entry.rotation.push(createVendorRotationTemplate());
        state.selectedVendorRotationIndex = entry.rotation.length - 1;
      });
      render();
    };
  }

  if (!selectedVendorRotation || !documentObject.getElementById("removeVendorRotationBtn")) return;

  documentObject.getElementById("removeVendorRotationBtn").onclick = () => {
    updateVendorDefinition(vendorDefId, (entry) => {
      entry.rotation = Array.isArray(entry.rotation) ? entry.rotation : [];
      const index = Math.min(Math.max(0, Number(state.selectedVendorRotationIndex || 0)), entry.rotation.length - 1);
      entry.rotation.splice(index, 1);
      state.selectedVendorRotationIndex = Math.min(index, Math.max(0, entry.rotation.length - 1));
    });
    render();
  };
  documentObject.getElementById("vendorRotationSelect").onchange = (e) => {
    state.selectedVendorRotationIndex = Math.max(0, Number(e.target.value || 0));
    render();
  };
  documentObject.getElementById("vendorRotationSummaryInput").oninput = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      const rotation = entry.rotation?.[selectedVendorRotationState.index];
      if (!rotation) return;
      rotation.summary = e.target.value.trim();
    });
  };
  documentObject.getElementById("vendorRotationMinFloorInput").onchange = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      const rotation = entry.rotation?.[selectedVendorRotationState.index];
      if (!rotation) return;
      rotation.when = rotation.when || {};
      rotation.when.minFloor = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("vendorRotationMaxFloorInput").onchange = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      const rotation = entry.rotation?.[selectedVendorRotationState.index];
      if (!rotation) return;
      rotation.when = rotation.when || {};
      if (e.target.value === "") delete rotation.when.maxFloor;
      else rotation.when.maxFloor = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("vendorRotationBossesInput").onchange = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      const rotation = entry.rotation?.[selectedVendorRotationState.index];
      if (!rotation) return;
      rotation.when = rotation.when || {};
      rotation.when.bossesDefeatedAtLeast = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("vendorRotationGoldCostInput").onchange = (e) => {
    updateVendorDefinition(vendorDefId, (entry) => {
      const rotation = entry.rotation?.[selectedVendorRotationState.index];
      if (!rotation) return;
      rotation.cost = rotation.cost || {};
      rotation.cost.gold = Math.max(0, Number(e.target.value || 0));
    });
  };
  if (documentObject.getElementById("addVendorRotationItemBtn")) {
    documentObject.getElementById("addVendorRotationItemBtn").onclick = () => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const rotation = entry.rotation?.[selectedVendorRotationState.index];
        if (!rotation) return;
        rotation.inventory = Array.isArray(rotation.inventory) ? rotation.inventory : [];
        rotation.inventory.push(Object.keys(items)[0] || "");
      });
      render();
    };
  }
  documentObject.querySelectorAll("[data-vendor-rotation-item-id]").forEach((select) => {
    select.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const rotation = entry.rotation?.[selectedVendorRotationState.index];
        if (!rotation) return;
        rotation.inventory = Array.isArray(rotation.inventory) ? rotation.inventory : [];
        const index = Number(e.target.dataset.vendorRotationItemId || 0);
        if (index < 0 || index >= rotation.inventory.length) return;
        if (typeof rotation.inventory[index] === "string") rotation.inventory[index] = e.target.value;
        else ensureVendorInventoryEntryObject(rotation.inventory, index).itemId = e.target.value;
      });
    };
  });
  documentObject.querySelectorAll("[data-vendor-rotation-generated]").forEach((input) => {
    input.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const rotation = entry.rotation?.[selectedVendorRotationState.index];
        if (!rotation) return;
        rotation.inventory = Array.isArray(rotation.inventory) ? rotation.inventory : [];
        const target = ensureVendorInventoryEntryObject(rotation.inventory, Number(e.target.dataset.vendorRotationGenerated || 0));
        if (!target) return;
        setGeneratedEntryFields(target, e.target.checked);
      });
      render();
    };
  });
  documentObject.querySelectorAll("[data-vendor-rotation-rarity-id]").forEach((select) => {
    select.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const rotation = entry.rotation?.[selectedVendorRotationState.index];
        if (!rotation) return;
        const target = ensureVendorInventoryEntryObject(rotation.inventory, Number(e.target.dataset.vendorRotationRarityId || 0));
        if (!target) return;
        target.rarityId = e.target.value;
      });
    };
  });
  documentObject.querySelectorAll("[data-vendor-rotation-affix-pool-id]").forEach((select) => {
    select.onchange = (e) => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const rotation = entry.rotation?.[selectedVendorRotationState.index];
        if (!rotation) return;
        const target = ensureVendorInventoryEntryObject(rotation.inventory, Number(e.target.dataset.vendorRotationAffixPoolId || 0));
        if (!target) return;
        target.affixPoolId = e.target.value;
      });
    };
  });
  documentObject.querySelectorAll("[data-remove-vendor-rotation-item]").forEach((button) => {
    button.onclick = () => {
      updateVendorDefinition(vendorDefId, (entry) => {
        const rotation = entry.rotation?.[selectedVendorRotationState.index];
        if (!rotation) return;
        rotation.inventory = Array.isArray(rotation.inventory) ? rotation.inventory : [];
        const index = Number(button.dataset.removeVendorRotationItem || 0);
        if (index < 0 || index >= rotation.inventory.length) return;
        rotation.inventory.splice(index, 1);
      });
      render();
    };
  });
}

export function bindLootTableEditorControls(deps = {}) {
  const {
    state,
    lootTableDef = null,
    lootTableId = "",
    render = () => {},
    documentObject = document,
    addLog = () => {},
    lootTables = {},
    parseJsonField = () => ({}),
    replaceLootTableDefinitions = () => {},
    lootTableDefinitionIds = () => [],
    activeLootTableId = () => "",
    uniqueLootTableId = () => "",
    createLootTableDefinitionTemplate = () => ({}),
    updateLootTableDefinition = () => {},
    ensureLootEntryObject = () => null,
    setGeneratedEntryFields = () => {},
    createLootEntryTemplate = () => ({}),
  } = deps;

  if (documentObject.getElementById("addLootTableBtn")) {
    documentObject.getElementById("addLootTableBtn").onclick = () => {
      const newId = uniqueLootTableId("loot_custom");
      lootTables[newId] = createLootTableDefinitionTemplate();
      state.selectedLootTableId = newId;
      state.selectedLootTierIndex = 0;
      state.selectedLootBonusIndex = 0;
      render();
    };
  }

  if (!lootTableDef) return;

  documentObject.getElementById("lootTableDefinitionSelect").onchange = (e) => {
    state.selectedLootTableId = e.target.value;
    state.selectedLootTierIndex = 0;
    state.selectedLootBonusIndex = 0;
    render();
  };
  documentObject.getElementById("lootTableRollsInput").onchange = (e) => {
    updateLootTableDefinition(lootTableId, (entry) => {
      entry.rolls = Math.max(0, Number(e.target.value || 0));
    });
  };
  documentObject.getElementById("lootTablesJsonInput").onchange = (e) => {
    try {
      const definitions = parseJsonField(e.target.value, {});
      replaceLootTableDefinitions(definitions);
      state.selectedLootTableId = lootTableDefinitionIds().includes(state.selectedLootTableId) ? state.selectedLootTableId : lootTableDefinitionIds()[0] || "";
      state.selectedLootTierIndex = 0;
      state.selectedLootBonusIndex = 0;
      state.selectedCombatRewardProfileIndex = 0;
      render();
    } catch (error) {
      addLog(`loot tables JSON 파싱 실패: ${error.message}`);
      render();
    }
  };
  documentObject.getElementById("duplicateLootTableBtn").onclick = () => {
    const sourceId = activeLootTableId();
    if (!sourceId || !lootTables[sourceId]) return;
    const newId = uniqueLootTableId(sourceId);
    lootTables[newId] = JSON.parse(JSON.stringify(lootTables[sourceId]));
    state.selectedLootTableId = newId;
    state.selectedLootTierIndex = 0;
    state.selectedLootBonusIndex = 0;
    render();
  };
  documentObject.getElementById("removeLootTableBtn").onclick = () => {
    const sourceId = activeLootTableId();
    if (!sourceId || !lootTables[sourceId]) return;
    delete lootTables[sourceId];
    state.selectedLootTableId = lootTableDefinitionIds()[0] || "";
    state.selectedLootTierIndex = 0;
    state.selectedLootBonusIndex = 0;
    render();
  };
  if (documentObject.getElementById("addLootGuaranteedBtn")) {
    documentObject.getElementById("addLootGuaranteedBtn").onclick = () => {
      updateLootTableDefinition(lootTableId, (entry) => {
        entry.guaranteed = Array.isArray(entry.guaranteed) ? entry.guaranteed : [];
        entry.guaranteed.push(createLootEntryTemplate());
      });
      render();
    };
  }
  documentObject.querySelectorAll("[data-loot-guaranteed-item-id]").forEach((select) => {
    select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
      const target = ensureLootEntryObject(entry.guaranteed, Number(e.target.dataset.lootGuaranteedItemId || 0));
      if (!target) return;
      target.itemId = e.target.value;
    });
  });
  documentObject.querySelectorAll("[data-loot-guaranteed-quantity]").forEach((input) => {
    input.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
      const target = ensureLootEntryObject(entry.guaranteed, Number(e.target.dataset.lootGuaranteedQuantity || 0));
      if (!target) return;
      target.quantity = Math.max(1, Number(e.target.value || 1));
    });
  });
  documentObject.querySelectorAll("[data-loot-guaranteed-generated]").forEach((input) => {
    input.onchange = (e) => {
      updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.guaranteed, Number(e.target.dataset.lootGuaranteedGenerated || 0));
        if (!target) return;
        setGeneratedEntryFields(target, e.target.checked);
      });
      render();
    };
  });
  documentObject.querySelectorAll("[data-loot-guaranteed-rarity-id]").forEach((select) => {
    select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
      const target = ensureLootEntryObject(entry.guaranteed, Number(e.target.dataset.lootGuaranteedRarityId || 0));
      if (!target) return;
      target.rarityId = e.target.value;
    });
  });
  documentObject.querySelectorAll("[data-loot-guaranteed-affix-pool-id]").forEach((select) => {
    select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
      const target = ensureLootEntryObject(entry.guaranteed, Number(e.target.dataset.lootGuaranteedAffixPoolId || 0));
      if (!target) return;
      target.affixPoolId = e.target.value;
    });
  });
  documentObject.querySelectorAll("[data-remove-loot-guaranteed]").forEach((button) => {
    button.onclick = () => {
      updateLootTableDefinition(lootTableId, (entry) => {
        const index = Number(button.dataset.removeLootGuaranteed || 0);
        if (index >= 0 && index < (entry.guaranteed || []).length) entry.guaranteed.splice(index, 1);
      });
      render();
    };
  });
}

export function bindLootTableAdvancedControls(deps = {}) {
  const {
    state,
    lootTableId = "",
    selectedLootTier = null,
    selectedLootTierState = { index: 0, tier: null },
    selectedLootBonus = null,
    selectedLootBonusState = { index: 0, bonus: null },
    selectedCombatRewardProfile = null,
    selectedCombatRewardProfileState = { index: 0, profile: null },
    render = () => {},
    documentObject = document,
    items = {},
    lootTables = {},
    updateLootTableDefinition = () => {},
    ensureLootEntryObject = () => null,
    setGeneratedEntryFields = () => {},
    createLootEntryTemplate = () => ({}),
    createLootTierTemplate = () => ({}),
    createLootBonusTemplate = () => ({}),
    createCombatRewardProfileTemplate = () => ({}),
  } = deps;

  if (!lootTableId) return;

  if (documentObject.getElementById("addLootTierBtn")) {
    documentObject.getElementById("addLootTierBtn").onclick = () => {
      updateLootTableDefinition(lootTableId, (entry) => {
        entry.tierEntries = Array.isArray(entry.tierEntries) ? entry.tierEntries : [];
        entry.tierEntries.push(createLootTierTemplate());
        state.selectedLootTierIndex = entry.tierEntries.length - 1;
      });
      render();
    };
  }
  if (selectedLootTier) {
    documentObject.getElementById("removeLootTierBtn").onclick = () => {
      updateLootTableDefinition(lootTableId, (entry) => {
        const index = Math.min(Math.max(0, Number(state.selectedLootTierIndex || 0)), (entry.tierEntries || []).length - 1);
        entry.tierEntries.splice(index, 1);
        state.selectedLootTierIndex = Math.min(index, Math.max(0, entry.tierEntries.length - 1));
      });
      render();
    };
    documentObject.getElementById("lootTierSelect").onchange = (e) => {
      state.selectedLootTierIndex = Math.max(0, Number(e.target.value || 0));
      render();
    };
    documentObject.getElementById("lootTierWeightInput").onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
      const tier = entry.tierEntries?.[selectedLootTierState.index];
      if (!tier) return;
      tier.weight = Math.max(0, Number(e.target.value || 0));
    });
    documentObject.getElementById("lootTierMinFloorInput").onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
      const tier = entry.tierEntries?.[selectedLootTierState.index];
      if (!tier) return;
      tier.when = tier.when || {};
      if (e.target.value === "") delete tier.when.floorAtLeast;
      else tier.when.floorAtLeast = Math.max(0, Number(e.target.value || 0));
    });
    if (documentObject.getElementById("addLootTierItemBtn")) {
      documentObject.getElementById("addLootTierItemBtn").onclick = () => {
        updateLootTableDefinition(lootTableId, (entry) => {
          const tier = entry.tierEntries?.[selectedLootTierState.index];
          if (!tier) return;
          tier.entries = Array.isArray(tier.entries) ? tier.entries : [];
          tier.entries.push(createLootEntryTemplate());
        });
        render();
      };
    }
    documentObject.querySelectorAll("[data-loot-tier-item-id]").forEach((select) => {
      select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.tierEntries?.[selectedLootTierState.index]?.entries, Number(e.target.dataset.lootTierItemId || 0));
        if (!target) return;
        target.itemId = e.target.value;
      });
    });
    documentObject.querySelectorAll("[data-loot-tier-item-quantity]").forEach((input) => {
      input.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.tierEntries?.[selectedLootTierState.index]?.entries, Number(e.target.dataset.lootTierItemQuantity || 0));
        if (!target) return;
        target.quantity = Math.max(1, Number(e.target.value || 1));
      });
    });
    documentObject.querySelectorAll("[data-loot-tier-item-weight]").forEach((input) => {
      input.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.tierEntries?.[selectedLootTierState.index]?.entries, Number(e.target.dataset.lootTierItemWeight || 0));
        if (!target) return;
        target.weight = Math.max(0, Number(e.target.value || 0));
      });
    });
    documentObject.querySelectorAll("[data-loot-tier-generated]").forEach((input) => {
      input.onchange = (e) => {
        updateLootTableDefinition(lootTableId, (entry) => {
          const target = ensureLootEntryObject(entry.tierEntries?.[selectedLootTierState.index]?.entries, Number(e.target.dataset.lootTierGenerated || 0));
          if (!target) return;
          setGeneratedEntryFields(target, e.target.checked);
        });
        render();
      };
    });
    documentObject.querySelectorAll("[data-loot-tier-rarity-id]").forEach((select) => {
      select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.tierEntries?.[selectedLootTierState.index]?.entries, Number(e.target.dataset.lootTierRarityId || 0));
        if (!target) return;
        target.rarityId = e.target.value;
      });
    });
    documentObject.querySelectorAll("[data-loot-tier-affix-pool-id]").forEach((select) => {
      select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.tierEntries?.[selectedLootTierState.index]?.entries, Number(e.target.dataset.lootTierAffixPoolId || 0));
        if (!target) return;
        target.affixPoolId = e.target.value;
      });
    });
    documentObject.querySelectorAll("[data-remove-loot-tier-item]").forEach((button) => {
      button.onclick = () => {
        updateLootTableDefinition(lootTableId, (entry) => {
          const entries = entry.tierEntries?.[selectedLootTierState.index]?.entries;
          const index = Number(button.dataset.removeLootTierItem || 0);
          if (Array.isArray(entries) && index >= 0 && index < entries.length) entries.splice(index, 1);
        });
        render();
      };
    });
  }

  if (documentObject.getElementById("addLootBonusBtn")) {
    documentObject.getElementById("addLootBonusBtn").onclick = () => {
      updateLootTableDefinition(lootTableId, (entry) => {
        entry.bonusRolls = Array.isArray(entry.bonusRolls) ? entry.bonusRolls : [];
        entry.bonusRolls.push(createLootBonusTemplate());
        state.selectedLootBonusIndex = entry.bonusRolls.length - 1;
      });
      render();
    };
  }
  if (selectedLootBonus) {
    documentObject.getElementById("removeLootBonusBtn").onclick = () => {
      updateLootTableDefinition(lootTableId, (entry) => {
        const index = Math.min(Math.max(0, Number(state.selectedLootBonusIndex || 0)), (entry.bonusRolls || []).length - 1);
        entry.bonusRolls.splice(index, 1);
        state.selectedLootBonusIndex = Math.min(index, Math.max(0, entry.bonusRolls.length - 1));
      });
      render();
    };
    documentObject.getElementById("lootBonusSelect").onchange = (e) => {
      state.selectedLootBonusIndex = Math.max(0, Number(e.target.value || 0));
      render();
    };
    documentObject.getElementById("lootBonusChanceInput").onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
      const bonus = entry.bonusRolls?.[selectedLootBonusState.index];
      if (!bonus) return;
      bonus.chance = Math.max(0, Number(e.target.value || 0));
    });
    if (documentObject.getElementById("addLootBonusItemBtn")) {
      documentObject.getElementById("addLootBonusItemBtn").onclick = () => {
        updateLootTableDefinition(lootTableId, (entry) => {
          const bonus = entry.bonusRolls?.[selectedLootBonusState.index];
          if (!bonus) return;
          bonus.entries = Array.isArray(bonus.entries) ? bonus.entries : [];
          bonus.entries.push(createLootEntryTemplate());
        });
        render();
      };
    }
    documentObject.querySelectorAll("[data-loot-bonus-item-id]").forEach((select) => {
      select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.bonusRolls?.[selectedLootBonusState.index]?.entries, Number(e.target.dataset.lootBonusItemId || 0));
        if (!target) return;
        target.itemId = e.target.value;
      });
    });
    documentObject.querySelectorAll("[data-loot-bonus-item-quantity]").forEach((input) => {
      input.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.bonusRolls?.[selectedLootBonusState.index]?.entries, Number(e.target.dataset.lootBonusItemQuantity || 0));
        if (!target) return;
        target.quantity = Math.max(1, Number(e.target.value || 1));
      });
    });
    documentObject.querySelectorAll("[data-loot-bonus-item-weight]").forEach((input) => {
      input.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.bonusRolls?.[selectedLootBonusState.index]?.entries, Number(e.target.dataset.lootBonusItemWeight || 0));
        if (!target) return;
        target.weight = Math.max(0, Number(e.target.value || 0));
      });
    });
    documentObject.querySelectorAll("[data-loot-bonus-generated]").forEach((input) => {
      input.onchange = (e) => {
        updateLootTableDefinition(lootTableId, (entry) => {
          const target = ensureLootEntryObject(entry.bonusRolls?.[selectedLootBonusState.index]?.entries, Number(e.target.dataset.lootBonusGenerated || 0));
          if (!target) return;
          setGeneratedEntryFields(target, e.target.checked);
        });
        render();
      };
    });
    documentObject.querySelectorAll("[data-loot-bonus-rarity-id]").forEach((select) => {
      select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.bonusRolls?.[selectedLootBonusState.index]?.entries, Number(e.target.dataset.lootBonusRarityId || 0));
        if (!target) return;
        target.rarityId = e.target.value;
      });
    });
    documentObject.querySelectorAll("[data-loot-bonus-affix-pool-id]").forEach((select) => {
      select.onchange = (e) => updateLootTableDefinition(lootTableId, (entry) => {
        const target = ensureLootEntryObject(entry.bonusRolls?.[selectedLootBonusState.index]?.entries, Number(e.target.dataset.lootBonusAffixPoolId || 0));
        if (!target) return;
        target.affixPoolId = e.target.value;
      });
    });
    documentObject.querySelectorAll("[data-remove-loot-bonus-item]").forEach((button) => {
      button.onclick = () => {
        updateLootTableDefinition(lootTableId, (entry) => {
          const entries = entry.bonusRolls?.[selectedLootBonusState.index]?.entries;
          const index = Number(button.dataset.removeLootBonusItem || 0);
          if (Array.isArray(entries) && index >= 0 && index < entries.length) entries.splice(index, 1);
        });
        render();
      };
    });
  }

  if (documentObject.getElementById("addCombatRewardProfileBtn")) {
    documentObject.getElementById("addCombatRewardProfileBtn").onclick = () => {
      lootTables.combatRewardProfiles = lootTables.combatRewardProfiles || { default: [] };
      lootTables.combatRewardProfiles.default = Array.isArray(lootTables.combatRewardProfiles.default) ? lootTables.combatRewardProfiles.default : [];
      lootTables.combatRewardProfiles.default.push(createCombatRewardProfileTemplate());
      state.selectedCombatRewardProfileIndex = lootTables.combatRewardProfiles.default.length - 1;
      render();
    };
  }
  if (selectedCombatRewardProfile) {
    documentObject.getElementById("removeCombatRewardProfileBtn").onclick = () => {
      const profiles = lootTables.combatRewardProfiles?.default || [];
      const index = Math.min(Math.max(0, Number(state.selectedCombatRewardProfileIndex || 0)), profiles.length - 1);
      profiles.splice(index, 1);
      state.selectedCombatRewardProfileIndex = Math.min(index, Math.max(0, profiles.length - 1));
      render();
    };
    documentObject.getElementById("combatRewardProfileSelect").onchange = (e) => {
      state.selectedCombatRewardProfileIndex = Math.max(0, Number(e.target.value || 0));
      render();
    };
    documentObject.getElementById("combatRewardProfileTableSelect").onchange = (e) => {
      const profile = lootTables.combatRewardProfiles?.default?.[selectedCombatRewardProfileState.index];
      if (!profile) return;
      profile.tableId = e.target.value;
    };
    documentObject.getElementById("combatRewardProfileMinXpInput").onchange = (e) => {
      const profile = lootTables.combatRewardProfiles?.default?.[selectedCombatRewardProfileState.index];
      if (!profile) return;
      profile.when = profile.when || {};
      profile.when.minXp = Math.max(0, Number(e.target.value || 0));
    };
    documentObject.getElementById("combatRewardProfileBossSelect").onchange = (e) => {
      const profile = lootTables.combatRewardProfiles?.default?.[selectedCombatRewardProfileState.index];
      if (!profile) return;
      profile.when = profile.when || {};
      if (e.target.value === "true") profile.when.boss = true;
      else delete profile.when.boss;
    };
  }
}

export function bindEventDiffDownloadControls(deps = {}) {
  const {
    eventDefId = "",
    eventGraphJsonDiffText = "",
    eventBundleJsonDiffText = "",
    render = () => {},
    documentObject = document,
    downloadTextFile = () => {},
    addLog = () => {},
  } = deps;

  if (documentObject.getElementById("downloadEventGraphDiffBtn")) {
    documentObject.getElementById("downloadEventGraphDiffBtn").onclick = () => {
      try {
        if (!eventGraphJsonDiffText) {
          addLog("비교 가능한 graph diff payload가 없다.");
          return;
        }
        downloadTextFile(`${eventDefId || "event"}_graph_diff.txt`, eventGraphJsonDiffText);
        addLog(`${eventDefId} graph diff를 다운로드했다.`);
        render();
      } catch (error) {
        addLog(`graph diff 다운로드 실패: ${error.message}`);
      }
    };
  }
  if (documentObject.getElementById("downloadEventBundleDiffBtn")) {
    documentObject.getElementById("downloadEventBundleDiffBtn").onclick = () => {
      try {
        if (!eventBundleJsonDiffText) {
          addLog("비교 가능한 bundle diff payload가 없다.");
          return;
        }
        downloadTextFile("event_project_review_bundle_diff.txt", eventBundleJsonDiffText);
        addLog("project review bundle diff를 다운로드했다.");
        render();
      } catch (error) {
        addLog(`project review bundle diff 다운로드 실패: ${error.message}`);
      }
    };
  }
}

export function renderEditorGridFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    getCell = () => null,
    cellCoordKey = () => "",
    classifyDensityBand = () => "none",
    textureSwatchColor = () => "",
    cellTags = [],
    previewRect = null,
    selectedKeys = new Set(),
    densityOverlay = { mode: "none", counts: {}, maxCount: 0 },
    defaultFloorTextureId = "",
    defaultCeilingTextureId = "",
    defaultWallTextureId = "",
    isLassoBrushMode = () => false,
    activeBrushRangeStart = () => null,
    isRangeBrushTool = () => false,
    beginLassoBrushDrag = () => {},
    beginRangeBrushDrag = () => {},
    updateLassoBrushDrag = () => {},
    updateRangeBrushDrag = () => {},
    commitLassoBrushSelection = () => {},
    applyRangeBrushAtCurrentCursor = () => {},
    computeWalls = () => {},
    editCell = () => {},
    render = () => {},
  } = deps;

  const grid = documentObject.getElementById("editorGrid");
  if (!grid) return;
  grid.innerHTML = "";
  grid.style.gridTemplateColumns = `repeat(${state.map.size.width}, 24px)`;
  for (let y = 0; y < state.map.size.height; y++) {
    for (let x = 0; x < state.map.size.width; x++) {
      const c = getCell(state.map, x, y);
      const d = documentObject.createElement("button");
      const cellKey = cellCoordKey(x, y);
      const densityCount = densityOverlay.counts[cellKey] || 0;
      const densityBand = classifyDensityBand(densityCount, densityOverlay.maxCount);
      d.className = `edit-cell ${c.walkable ? "" : "wall"}`;
      d.style.setProperty("--cell-floor", textureSwatchColor(c.floorTexture, "#655846"));
      d.style.setProperty("--cell-wall", textureSwatchColor(c.wallTexture, "#41382f"));
      d.style.setProperty("--cell-ceiling", textureSwatchColor(c.ceilingTexture, "#2b2621"));
      if (state.editorCursor.x === x && state.editorCursor.y === y) d.classList.add("cursor");
      if (previewRect && x >= previewRect.x && x < previewRect.x + previewRect.width && y >= previewRect.y && y < previewRect.y + previewRect.height) d.classList.add("preview");
      if (selectedKeys.has(cellKey)) d.classList.add("selection");
      if ((c.tags || []).some((tag) => cellTags.includes(tag))) d.classList.add("preset");
      if (Object.keys(state.map.doors).some((k) => k.startsWith(`${x},${y},`) && state.map.doors[k].type === "door")) d.classList.add("door");
      if (Object.keys(state.map.doors).some((k) => k.startsWith(`${x},${y},`) && state.map.doors[k].type === "secret")) d.classList.add("secret");
      if (state.map.start.x === x && state.map.start.y === y) d.classList.add("start");
      if (state.map.placements.some((p) => !p.done && p.position.x === x && p.position.y === y && p.kind === "stairs")) d.classList.add("exit");
      if (state.map.placements.some((p) => !p.done && p.position.x === x && p.position.y === y && p.kind === "encounter")) d.classList.add("monster");
      if (state.map.placements.some((p) => !p.done && p.position.x === x && p.position.y === y && p.kind === "npc")) d.classList.add("npc");
      if (state.map.placements.some((p) => !p.done && p.position.x === x && p.position.y === y && p.kind === "trap")) d.classList.add("trap");
      if (densityOverlay.mode !== "none" && densityCount > 0) {
        d.classList.add("density-hot");
        d.dataset.densityCount = String(densityCount);
        d.dataset.densityBand = densityBand;
        d.style.setProperty("--density-alpha", `${Math.min(0.82, 0.2 + densityCount * 0.18)}`);
      }
      d.title = [
        c.roomId ? `room ${c.roomId}` : "",
        (c.tags || []).filter((tag) => cellTags.includes(tag)).join(", "),
        c.battleBackgroundId ? `bg ${c.battleBackgroundId}` : "",
        `floor ${c.floorTexture || defaultFloorTextureId}`,
        `ceiling ${c.ceilingTexture || defaultCeilingTextureId}`,
        `wall ${c.wallTexture || defaultWallTextureId}`,
        densityOverlay.mode !== "none" && densityCount > 0 ? `${densityOverlay.mode} density ${densityCount}` : "",
      ].filter(Boolean).join(" | ");
      d.onmouseenter = () => {
        if (isLassoBrushMode()) return;
        if (!activeBrushRangeStart()) return;
        if (state.editorCursor.x === x && state.editorCursor.y === y) return;
        state.editorCursor = { x, y };
        render();
      };
      d.onpointerdown = (pointerEvent) => {
        if (!isRangeBrushTool() || pointerEvent.button !== 0) return;
        if (isLassoBrushMode()) beginLassoBrushDrag(x, y, pointerEvent.pointerId);
        else beginRangeBrushDrag(x, y, pointerEvent.pointerId);
        render();
      };
      d.onpointerenter = (pointerEvent) => {
        if (state.editorLassoSelectionDrag?.pointerId === pointerEvent.pointerId) {
          updateLassoBrushDrag(x, y);
          return render();
        }
        if (!state.editorBrushDrag || state.editorBrushDrag.pointerId !== pointerEvent.pointerId) return;
        updateRangeBrushDrag(x, y);
        render();
      };
      d.onpointerup = (pointerEvent) => {
        if (state.editorLassoSelectionDrag?.pointerId === pointerEvent.pointerId) {
          updateLassoBrushDrag(x, y);
          commitLassoBrushSelection(true);
          state.suppressRangeClick = true;
          return render();
        }
        if (!state.editorBrushDrag || state.editorBrushDrag.pointerId !== pointerEvent.pointerId) return;
        updateRangeBrushDrag(x, y);
        applyRangeBrushAtCurrentCursor(true);
        state.suppressRangeClick = true;
        computeWalls(state.map);
        render();
      };
      d.onclick = (event) => editCell(x, y, event);
      grid.appendChild(d);
    }
  }
}

export function bindEditorMapFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    render = () => {},
    eventEditorToPlacementKind = {},
    applyRecommendationAutoPlacement = () => {},
    recommendationRoomId = "",
    placementRecommendations = [],
    normalizeTextureId = (value) => value,
    floorTextureIds = [],
    ceilingTextureIds = [],
    wallTextureIds = [],
    defaultFloorTextureId = "",
    defaultCeilingTextureId = "",
    defaultWallTextureId = "",
    clearRangeBrushState = () => {},
    densityOverlayModes = new Set(),
    applyCommittedBrushSelection = () => false,
    computeWalls = () => {},
    rememberExplicitBrushSelection = () => false,
    currentBrushFloodSelectionPoints = () => [],
    currentBrushMatchingSelectionPoints = () => [],
    transformCommittedBrushSelection = () => false,
    grownSelectionPoints = () => [],
    shrunkSelectionPoints = () => [],
    invertedSelectionPoints = () => [],
  } = deps;

  documentObject.querySelectorAll("[data-tool]").forEach((b) => b.onclick = () => {
    state.editorTool = b.dataset.tool;
    if (eventEditorToPlacementKind[state.editorTool]) state.eventInspectorTool = state.editorTool;
    if (state.editorTool !== "room") state.roomRangeStart = null;
    if (state.editorTool !== "cellTag" && state.editorTool !== "battleBg" && state.editorTool !== "texture") state.metadataRangeStart = null;
    render();
  });
  documentObject.querySelectorAll("[data-recommend-tool]").forEach((button) => button.onclick = () => {
    state.editorTool = button.dataset.recommendTool;
    if (eventEditorToPlacementKind[state.editorTool]) state.eventInspectorTool = state.editorTool;
    if (state.editorTool !== "room") state.roomRangeStart = null;
    if (state.editorTool !== "cellTag" && state.editorTool !== "battleBg" && state.editorTool !== "texture") state.metadataRangeStart = null;
    render();
  });
  const autoPlaceRecommendationsBtn = documentObject.getElementById("autoPlaceRecommendationsBtn");
  if (autoPlaceRecommendationsBtn) {
    autoPlaceRecommendationsBtn.onclick = () => {
      applyRecommendationAutoPlacement(recommendationRoomId, placementRecommendations, state.selectedCellTag);
      render();
    };
  }
  const floorTextureSelect = documentObject.getElementById("floorTextureSelect");
  if (!floorTextureSelect) return;
  const ceilingTextureSelect = documentObject.getElementById("ceilingTextureSelect");
  const wallTextureSelect = documentObject.getElementById("wallTextureSelect");
  const cellTagSelect = documentObject.getElementById("cellTagSelect");
  const battleBgSelect = documentObject.getElementById("battleBgSelect");
  const roomTypeSelect = documentObject.getElementById("roomTypeSelect");
  const roomIdInput = documentObject.getElementById("roomIdInput");
  const metadataSelectionModeSelect = documentObject.getElementById("metadataSelectionModeSelect");
  const lassoSelectionActionSelect = documentObject.getElementById("lassoSelectionActionSelect");
  const densityOverlayModeSelect = documentObject.getElementById("densityOverlayModeSelect");
  const applyBrushSelectionBtn = documentObject.getElementById("applyBrushSelectionBtn");
  const floodBrushSelectionBtn = documentObject.getElementById("floodBrushSelectionBtn");
  const matchBrushSelectionBtn = documentObject.getElementById("matchBrushSelectionBtn");
  floorTextureSelect.onchange = (e) => { state.selectedFloorTextureId = normalizeTextureId(e.target.value, floorTextureIds, defaultFloorTextureId); render(); };
  if (ceilingTextureSelect) ceilingTextureSelect.onchange = (e) => { state.selectedCeilingTextureId = normalizeTextureId(e.target.value, ceilingTextureIds, defaultCeilingTextureId); render(); };
  if (wallTextureSelect) wallTextureSelect.onchange = (e) => { state.selectedWallTextureId = normalizeTextureId(e.target.value, wallTextureIds, defaultWallTextureId); render(); };
  if (cellTagSelect) cellTagSelect.onchange = (e) => { state.selectedCellTag = e.target.value; };
  if (battleBgSelect) battleBgSelect.onchange = (e) => { state.selectedBattleBackgroundId = e.target.value; };
  if (roomTypeSelect) roomTypeSelect.onchange = (e) => { state.selectedRoomType = e.target.value; };
  if (roomIdInput) roomIdInput.oninput = (e) => { state.activeRoomId = e.target.value.trim(); };
  if (metadataSelectionModeSelect) metadataSelectionModeSelect.onchange = (e) => {
    state.metadataSelectionMode = e.target.value === "lasso" ? "lasso" : "rect";
    clearRangeBrushState();
    render();
  };
  if (lassoSelectionActionSelect) lassoSelectionActionSelect.onchange = (e) => {
    state.lassoSelectionAction = e.target.value === "subtract" ? "subtract" : "add";
  };
  if (densityOverlayModeSelect) densityOverlayModeSelect.onchange = (e) => {
    state.densityOverlayMode = densityOverlayModes.has(e.target.value) ? e.target.value : "none";
    render();
  };
  if (applyBrushSelectionBtn) applyBrushSelectionBtn.onclick = () => {
    if (applyCommittedBrushSelection(true)) {
      computeWalls(state.map);
      render();
    }
  };
  if (floodBrushSelectionBtn) floodBrushSelectionBtn.onclick = () => {
    if (rememberExplicitBrushSelection(currentBrushFloodSelectionPoints(), "연결 selection")) render();
  };
  if (matchBrushSelectionBtn) matchBrushSelectionBtn.onclick = () => {
    if (rememberExplicitBrushSelection(currentBrushMatchingSelectionPoints(), "같은 값 selection")) render();
  };
  documentObject.getElementById("growBrushSelectionBtn").onclick = () => {
    if (transformCommittedBrushSelection(grownSelectionPoints, "grow")) render();
  };
  documentObject.getElementById("shrinkBrushSelectionBtn").onclick = () => {
    if (transformCommittedBrushSelection(shrunkSelectionPoints, "shrink")) render();
  };
  documentObject.getElementById("invertBrushSelectionBtn").onclick = () => {
    if (transformCommittedBrushSelection(invertedSelectionPoints, "invert")) render();
  };
  documentObject.getElementById("clearRoomRangeBtn").onclick = () => {
    clearRangeBrushState();
    state.lastBrushSelection = null;
    render();
  };
}

export function renderEditorWorkspaceShell(deps = {}) {
  const {
    state,
    cursorCell = null,
    currentMapSeed = () => null,
    defaultFloorTextureId = "",
    defaultCeilingTextureId = "",
    defaultWallTextureId = "",
  } = deps;
  return `
    <div class="editor-main preset-stack">
      <div class="preset-panel editor-hero-panel">
        <div class="preset-header">
          <h2>Editor Workspace</h2>
          <span class="preset-subtitle">플레이 HUD를 치우고 저작 패널과 authored data에만 집중한다.</span>
        </div>
        <div class="editor-hero-grid">
          <div>
            <strong>${state.map.name}</strong>
            <div class="muted">floor ${state.player.floor} · cursor ${state.editorCursor.x},${state.editorCursor.y}${currentMapSeed() != null ? ` · seed ${currentMapSeed()}` : ""}</div>
          </div>
          <div>
            <strong>${cursorCell?.roomId || "room 없음"}</strong>
            <div class="muted">cell tag ${(cursorCell?.tags || []).join(", ") || "없음"} · bg ${cursorCell?.battleBackgroundId || "(clear)"}</div>
          </div>
          <div>
            <strong>surface</strong>
            <div class="muted">floor ${cursorCell?.floorTexture || defaultFloorTextureId} · ceiling ${cursorCell?.ceilingTexture || defaultCeilingTextureId} · wall ${cursorCell?.wallTexture || defaultWallTextureId}</div>
          </div>
        </div>
      </div>
      <div class="preset-panel editor-map-panel">
        <div class="preset-header">
          <h2>맵 편집기</h2>
          <span class="preset-subtitle">의도된 블록을 찍고 즉시 3D에서 본다.</span>
        </div>
        <div id="editorGrid"></div>
      </div>
    </div>
  `;
}

export function renderEditorSurfaceBrushPanel(deps = {}) {
  const {
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Surface Brush</h3><span class="preset-subtitle">floor, ceiling, wall texture를 셀 단위 또는 범위 단위로 저작한다.</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorRangeBrushPanel(deps = {}) {
  const {
    cellTagOptions = "",
    battleBackgroundOptions = "",
    roomTypeOptions = "",
    roomIdValue = "",
    metadataSelectionMode = "rect",
    lassoSelectionAction = "add",
    densityOverlayMode = "none",
    rangeModeSummary = "",
    activeRoomSummary = "",
    selectedCountSummary = "",
    densityOverlaySummary = "",
    applyDisabled = true,
    selectionDisabled = true,
    clearDisabled = true,
  } = deps;

  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>범위 브러시</h3><span class="preset-subtitle">texture, roomType, cellTag, 전투 배경을 같은 selection UX로 다룬다.</span></div>
      <div class="preset-inspector">
        <div class="preset-field">
          <label for="cellTagSelect">Cell tag</label>
          <select id="cellTagSelect">${cellTagOptions}</select>
        </div>
        <div class="preset-field">
          <label for="battleBgSelect">Battle background</label>
          <select id="battleBgSelect">${battleBackgroundOptions}</select>
        </div>
        <div class="preset-field">
          <label for="roomTypeSelect">Room type</label>
          <select id="roomTypeSelect">${roomTypeOptions}</select>
        </div>
        <div class="preset-field">
          <label for="roomIdInput">Room ID</label>
          <input id="roomIdInput" value="${roomIdValue}" placeholder="room_auto or custom id" />
        </div>
        <div class="preset-field">
          <label for="metadataSelectionModeSelect">Selection mode</label>
          <select id="metadataSelectionModeSelect">
            <option value="rect" ${metadataSelectionMode === "rect" ? "selected" : ""}>rect</option>
            <option value="lasso" ${metadataSelectionMode === "lasso" ? "selected" : ""}>lasso</option>
          </select>
        </div>
        <div class="preset-field">
          <label for="lassoSelectionActionSelect">Lasso action</label>
          <select id="lassoSelectionActionSelect" ${metadataSelectionMode === "lasso" ? "" : "disabled"}>
            <option value="add" ${lassoSelectionAction === "add" ? "selected" : ""}>add</option>
            <option value="subtract" ${lassoSelectionAction === "subtract" ? "selected" : ""}>subtract</option>
          </select>
        </div>
        <div class="preset-field">
          <label for="densityOverlayModeSelect">Density overlay</label>
          <select id="densityOverlayModeSelect">
            <option value="none" ${densityOverlayMode === "none" ? "selected" : ""}>none</option>
            <option value="encounter" ${densityOverlayMode === "encounter" ? "selected" : ""}>encounter</option>
            <option value="trap" ${densityOverlayMode === "trap" ? "selected" : ""}>trap</option>
            <option value="reward" ${densityOverlayMode === "reward" ? "selected" : ""}>reward</option>
            <option value="recovery" ${densityOverlayMode === "recovery" ? "selected" : ""}>recovery</option>
            <option value="camp" ${densityOverlayMode === "camp" ? "selected" : ""}>camp</option>
            <option value="npc" ${densityOverlayMode === "npc" ? "selected" : ""}>npc</option>
            <option value="event" ${densityOverlayMode === "event" ? "selected" : ""}>event</option>
          </select>
        </div>
        <div class="muted">${rangeModeSummary}</div>
        <div class="muted">${activeRoomSummary}</div>
        <div class="muted">${selectedCountSummary}</div>
        <div class="muted">${densityOverlaySummary}</div>
        <div class="preset-toolbar">
          <button id="applyBrushSelectionBtn" ${applyDisabled ? "disabled" : ""}>선택 적용</button>
          <button id="floodBrushSelectionBtn" ${selectionDisabled ? "disabled" : ""}>연결 선택</button>
          <button id="matchBrushSelectionBtn" ${selectionDisabled ? "disabled" : ""}>같은 값 선택</button>
        </div>
        <div class="preset-toolbar">
          <button id="growBrushSelectionBtn" ${applyDisabled ? "disabled" : ""}>확장</button>
          <button id="shrinkBrushSelectionBtn" ${applyDisabled ? "disabled" : ""}>축소</button>
          <button id="invertBrushSelectionBtn" ${selectionDisabled ? "disabled" : ""}>반전</button>
        </div>
        <div class="preset-toolbar">
          <button id="clearRoomRangeBtn" ${clearDisabled ? "disabled" : ""}>범위/선택 취소</button>
        </div>
      </div>
    </div>
  `;
}

export function renderEditorSelectedBlockPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>선택 블록</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorDensityHistogramPanel(deps = {}) {
  const {
    subtitle = "overlay off",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Density Histogram</h3><span class="preset-subtitle">${subtitle}</span></div>
      <div class="preset-inspector">
        ${bodyMarkup}
      </div>
    </div>
  `;
}

export function renderEditorPlacementRecommendationPanel(deps = {}) {
  const {
    subtitle = "",
    roomSummary = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Placement Recommendation</h3><span class="preset-subtitle">${subtitle}</span></div>
      <div class="preset-inspector">
        <div class="muted">${roomSummary}</div>
        ${bodyMarkup}
      </div>
    </div>
  `;
}

export function renderEditorValidationPanel(deps = {}) {
  const {
    title = "",
    subtitle = "",
    blockerMarkup = "",
    reportMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>${title}</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${blockerMarkup}
      <div class="validation-report">${reportMarkup}</div>
    </div>
  `;
}

export function renderEditorContentBuildDashboardPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Content Build Dashboard</h3><span class="preset-subtitle">${subtitle}</span></div>
      <div class="preset-stack">
        ${bodyMarkup}
      </div>
    </div>
  `;
}

export function renderEditorPresetLibraryPanel(deps = {}) {
  const {
    listMarkup = "",
  } = deps;
  return `
    <div class="preset-panel editor-library-panel">
      <div class="preset-header">
        <h3>블록 라이브러리</h3>
        <span class="preset-subtitle">생성기와 수동 배치가 같은 프리셋을 공유한다.</span>
      </div>
      <div class="preset-list">
        ${listMarkup}
      </div>
    </div>
  `;
}

export function renderEditorClassProgressionPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Class Progression Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorItemBasePanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Item Base Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorQuestDefinitionPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Quest Definition Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorMonsterDefinitionPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Monster Definition Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorSkillDefinitionPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Skill Definition Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorVendorInventoryPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Vendor Inventory Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorLootTablePanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Loot Table Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorAffixRarityPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Affix And Rarity Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorNpcProgressionHooksPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>NPC Progression Hooks Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorNpcPlacementPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>NPC Placement Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorPresetStudioPanel(deps = {}) {
  const {
    presetName = "",
    presetId = "",
    presetTags = "",
    deleteDisabled = true,
    customPresetCount = 0,
    compiledOk = false,
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>프리셋 스튜디오</h3><span class="preset-subtitle">7x7 초안으로 의도된 블록을 만든다.</span></div>
      <div id="presetDraftGrid" class="draft-grid"></div>
      <div class="preset-inspector">
        <div class="preset-field"><label for="presetName">이름</label><input id="presetName" value="${presetName}" maxlength="24" /></div>
        <div class="preset-field"><label for="presetId">ID</label><input id="presetId" value="${presetId}" maxlength="32" /></div>
        <div class="preset-field"><label for="presetTags">태그</label><input id="presetTags" value="${presetTags}" /></div>
        <div class="preset-toolbar">
          <button id="savePresetBtn">커스텀 저장</button>
          <button id="clearPresetDraftBtn">초안 비우기</button>
          <button id="deletePresetBtn" ${deleteDisabled ? "disabled" : ""}>커스텀 삭제</button>
        </div>
        <div class="muted">커스텀 프리셋 ${customPresetCount}개</div>
      </div>
    </div>
    <textarea id="jsonBox" spellcheck="false"></textarea>
    <div class="muted">검증: ${compiledOk ? "컴파일 가능" : "오류 수정 필요"}</div>
  `;
}

export function renderEditorQuestSeedPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Quest Seed Editor</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorQuestSeedBody(deps = {}) {
  const {
    npcDef = null,
    npcs = {},
    npcDefId = "",
    selectedNpcQuestSeedDef = null,
    selectedNpcQuestSeedDefState = { index: 0 },
    selectedNpcQuestSeedRewards = {},
    selectedNpcQuestSeedRuntime = null,
    linkedNpcQuestServices = [],
    items = {},
    eventDefinitions = {},
    npcQuestSeed = null,
    escapeHtml = (value) => String(value ?? ""),
    questSeedJson = () => "[]",
    questSeedRewardItemsText = () => "",
    questRewardFlagValueType = () => "boolean_true",
    questRewardFlagValueText = () => "",
  } = deps;

  return `
    <div class="preset-inspector">
      <div class="preset-field">
        <label for="questEditorNpcDefinitionSelect">Source NPC</label>
        <select id="questEditorNpcDefinitionSelect">${Object.entries(npcs).map(([id, npc]) => `<option value="${id}" ${id === npcDefId ? "selected" : ""}>${id} · ${npc.name}</option>`).join("")}</select>
      </div>
      <div class="preset-field">
        <label for="npcQuestSeedsJsonInput">Quest seeds JSON</label>
        <textarea id="npcQuestSeedsJsonInput" rows="8" spellcheck="false">${escapeHtml(questSeedJson(npcDef))}</textarea>
      </div>
      <div class="muted">quest service ${linkedNpcQuestServices.length}개${linkedNpcQuestServices.length ? ` · ${escapeHtml(linkedNpcQuestServices.join(", "))}` : " · quest service 없음"}</div>
      ${selectedNpcQuestSeedDef ? `
        <div class="preset-toolbar">
          <button id="addNpcQuestSeedBtn">quest seed 추가</button>
          <button id="duplicateNpcQuestSeedBtn">선택 seed 복제</button>
          <button id="moveNpcQuestSeedUpBtn">위로</button>
          <button id="moveNpcQuestSeedDownBtn">아래로</button>
          <button id="removeNpcQuestSeedBtn">선택 seed 삭제</button>
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedSelect">Selected quest seed</label>
          <select id="npcQuestSeedSelect">${(npcDef.questSeeds || []).map((seed, index) => `<option value="${index}" ${index === selectedNpcQuestSeedDefState.index ? "selected" : ""}>${index} · ${escapeHtml(seed.title || seed.id || `seed_${index}`)}</option>`).join("")}</select>
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedIdInput">Seed id</label>
          <input id="npcQuestSeedIdInput" value="${escapeHtml(selectedNpcQuestSeedDef.id || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedTitleInput">Seed title</label>
          <input id="npcQuestSeedTitleInput" value="${escapeHtml(selectedNpcQuestSeedDef.title || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedNoteInput">Seed note</label>
          <textarea id="npcQuestSeedNoteInput" rows="3" spellcheck="false">${escapeHtml(selectedNpcQuestSeedDef.note || "")}</textarea>
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedObjectivesInput">Objectives</label>
          <textarea id="npcQuestSeedObjectivesInput" rows="4" spellcheck="false">${escapeHtml((selectedNpcQuestSeedDef.objectives || []).join("\n"))}</textarea>
        </div>
        <div class="preset-field">
          <label>Objective fields</label>
          <div class="preset-stack">
            ${(selectedNpcQuestSeedDef.objectives || []).map((objective, index) => `
              <div class="preset-toolbar">
                <input data-npc-quest-objective-input="${index}" value="${escapeHtml(objective || "")}" placeholder="목표 설명" />
                <button data-remove-npc-quest-objective="${index}">삭제</button>
              </div>
            `).join("") || `<div class="muted">objective 없음</div>`}
            <button id="addNpcQuestObjectiveBtn">objective 추가</button>
          </div>
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedRewardGoldInput">Reward gold</label>
          <input id="npcQuestSeedRewardGoldInput" type="number" min="0" value="${selectedNpcQuestSeedRewards.gold ?? 0}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedRewardXpInput">Reward XP</label>
          <input id="npcQuestSeedRewardXpInput" type="number" min="0" value="${selectedNpcQuestSeedRewards.xp ?? 0}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedRewardItemsInput">Reward items</label>
          <input id="npcQuestSeedRewardItemsInput" value="${escapeHtml(questSeedRewardItemsText(selectedNpcQuestSeedDef))}" placeholder="bandage, antidote:2" />
        </div>
        <div class="preset-field">
          <label>Reward item fields</label>
          <div class="preset-stack">
            ${(selectedNpcQuestSeedRewards.items || []).map((entry, index) => `
              <div class="preset-toolbar">
                <select data-npc-quest-reward-item-id="${index}">
                  ${Object.entries(items).map(([itemId, item]) => `<option value="${itemId}" ${itemId === entry?.itemId ? "selected" : ""}>${itemId} · ${item.name}</option>`).join("")}
                </select>
                <input data-npc-quest-reward-item-qty="${index}" type="number" min="1" value="${Math.max(1, Number(entry?.quantity || 1))}" />
                <button data-remove-npc-quest-reward-item="${index}">삭제</button>
              </div>
            `).join("") || `<div class="muted">reward item 없음</div>`}
            <button id="addNpcQuestRewardItemBtn">reward item 추가</button>
          </div>
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedRewardFlagInput">Reward flag</label>
          <input id="npcQuestSeedRewardFlagInput" value="${escapeHtml(selectedNpcQuestSeedRewards.flag || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedRewardFlagValueTypeSelect">Reward flag value type</label>
          <select id="npcQuestSeedRewardFlagValueTypeSelect">
            ${[
              ["boolean_true", "boolean true"],
              ["boolean_false", "boolean false"],
              ["number", "number"],
              ["string", "string"],
            ].map(([value, label]) => `<option value="${value}" ${questRewardFlagValueType(selectedNpcQuestSeedRewards) === value ? "selected" : ""}>${label}</option>`).join("")}
          </select>
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedRewardFlagValueInput">Reward flag value</label>
          <input id="npcQuestSeedRewardFlagValueInput" value="${escapeHtml(questRewardFlagValueText(selectedNpcQuestSeedRewards))}" placeholder="string 또는 number 값" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedFailureFlagInput">Failure flag</label>
          <input id="npcQuestSeedFailureFlagInput" value="${escapeHtml(selectedNpcQuestSeedDef.failureFlag || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedGrantFlagInput">Grant flag</label>
          <input id="npcQuestSeedGrantFlagInput" value="${escapeHtml(selectedNpcQuestSeedDef.grantFlag || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedCompleteEventIdInput">Complete event</label>
          <input id="npcQuestSeedCompleteEventIdInput" value="${escapeHtml(selectedNpcQuestSeedDef.completeEventId || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcQuestSeedBossFloorInput">Bosses defeated at least</label>
          <input id="npcQuestSeedBossFloorInput" type="number" min="0" value="${Number(selectedNpcQuestSeedDef.bossesDefeatedAtLeast || 0)}" />
        </div>
        <div class="muted">guided seed ${escapeHtml(selectedNpcQuestSeedDef.title || selectedNpcQuestSeedDef.id || "unnamed")} · objective ${(selectedNpcQuestSeedDef.objectives || []).length}개 · active service ${linkedNpcQuestServices.length}개</div>
        <div class="muted">runtime ${selectedNpcQuestSeedRuntime ? `${selectedNpcQuestSeedRuntime.status || "active"}${selectedNpcQuestSeedRuntime.rewardsGranted ? " · rewards granted" : ""}` : "미활성"}${selectedNpcQuestSeedDef.completeEventId ? ` · complete event ${eventDefinitions[selectedNpcQuestSeedDef.completeEventId] ? "연결됨" : "누락"}` : ""}</div>
      ` : `
        <div class="preset-toolbar">
          <button id="addNpcQuestSeedBtn">quest seed 추가</button>
        </div>
        <div class="muted">quest seed가 없어서 guided editor를 숨겼다. 버튼으로 기본 seed를 먼저 추가한다.</div>
      `}
      <div class="muted">active seed ${npcQuestSeed?.title || "없음"}${npcQuestSeed?.grantFlag ? ` · grant ${npcQuestSeed.grantFlag}` : ""}</div>
    </div>
  `;
}

export function renderEditorNpcServicePanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>NPC Service Panel</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorNpcCustomPresetSection(deps = {}) {
  const {
    npcDefId = "",
    npcCustomPresets = [],
    selectedNpcCustomPreset = null,
    selectedNpcCustomPresetApplyMode = "replace",
    selectedNpcCustomPresetConflictMode = "preset_wins",
    npcCustomPresetDiff = {
      serviceDelta: 0,
      questSeedDelta: 0,
      newServices: [],
      duplicateServices: [],
      newSeeds: [],
      duplicateSeeds: [],
      serviceRows: [],
      seedRows: [],
    },
    npcCustomPresetMergePatch = null,
    npcCustomPresetMergePatchDraftValue = "",
    npcCustomPresetMergePatchValidation = { summary: { error: 0 }, issues: [] },
    npcCustomPresetMergePatchPreview = null,
    npcPresetApplyComparePreview = null,
    npcPresetPatchHistory = [],
    latestNpcPresetUndoEntry = null,
    latestNpcPresetRedoEntry = null,
    npcPresetRedoEntries = [],
    npcPresetRedoArchive = [],
    npcPresetRedoArchiveQuery = "",
    filteredNpcPresetRedoArchive = [],
    npcPresetRedoArchiveBatchCompare = null,
    selectedNpcPresetRedoArchiveId = "",
    selectedNpcPresetRedoArchiveEntry = null,
    npcPresetRedoArchiveLine = () => "",
    npcPresetPatchArchive = [],
    filteredNpcPresetPatchArchive = [],
    npcPresetPatchArchiveQuery = "",
    selectedNpcPresetPatchArchiveId = "",
    selectedNpcPresetPatchArchiveEntry = null,
    npcPresetPatchArchiveLine = () => "",
    npcPresetPatchArchiveCompare = {
      currentServiceCount: 0,
      archiveServiceCount: 0,
      currentQuestSeedCount: 0,
      archiveQuestSeedCount: 0,
    },
    npcPresetPatchArchivePreviewText = "{}",
    npcPresetPatchArchiveLineDiffText = "",
    selectedPresetServiceIndexes = [],
    selectedPresetSeedIndexes = [],
    selectedNpcCustomPresetConflictModeForPreview = "",
    selectedNpcCustomPresetRef = null,
    npcCustomPresetSummary = () => "",
    renderDiffBadgeHtml = () => "",
    buildDiffBadgeSpec = () => null,
    buildDiffCountScaleLabel = () => "",
    validationSummaryText = () => "",
    selectedNpcPresetServiceFieldNames = () => [],
    selectedNpcPresetDialogueStepIndexes = () => [],
    selectedNpcPresetDialogueChoiceIndexes = () => [],
    selectedNpcPresetDialogueBranchIndexes = () => [],
    selectedNpcPresetSeedFieldNames = () => [],
    npcPresetSideBySideDiffMarkup = () => "",
    npcPresetThreeWayPreviewMarkup = () => "",
    buildNpcPresetResolvedDialogueStep = () => ({}),
    buildNpcPresetResolvedServicePreview = () => ({}),
    buildNpcPresetResolvedSeedPreview = () => ({}),
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  const selectedPreset = selectedNpcCustomPresetRef || selectedNpcCustomPreset;
  const conflictMode = selectedNpcCustomPresetConflictModeForPreview || selectedNpcCustomPresetConflictMode;

  return `
    <div class="preset-field">
      <label>Custom preset library</label>
      <div class="preset-toolbar">
        <button id="saveNpcCustomPresetBtn">현재 NPC preset 저장</button>
        <button id="applyNpcCustomPresetBtn" ${selectedPreset ? "" : "disabled"}>선택 preset 적용</button>
        <button id="deleteNpcCustomPresetBtn" ${selectedPreset ? "" : "disabled"}>선택 preset 삭제</button>
      </div>
    </div>
    <div class="preset-field">
      <label for="npcCustomPresetSelect">Selected custom preset</label>
      <select id="npcCustomPresetSelect">
        ${npcCustomPresets.length ? npcCustomPresets.map((preset) => `<option value="${preset.id}" ${selectedPreset?.id === preset.id ? "selected" : ""}>${preset.id} · ${escapeHtml(preset.name)}</option>`).join("") : `<option value="">preset 없음</option>`}
      </select>
    </div>
    <div class="preset-field">
      <label for="npcCustomPresetApplyModeSelect">Preset apply mode</label>
      <select id="npcCustomPresetApplyModeSelect">
        <option value="replace" ${selectedNpcCustomPresetApplyMode === "replace" ? "selected" : ""}>replace</option>
        <option value="append" ${selectedNpcCustomPresetApplyMode === "append" ? "selected" : ""}>append</option>
      </select>
    </div>
    <div class="preset-field">
      <label for="npcCustomPresetConflictModeSelect">Duplicate conflict mode</label>
      <select id="npcCustomPresetConflictModeSelect">
        <option value="preset_wins" ${selectedNpcCustomPresetConflictMode === "preset_wins" ? "selected" : ""}>preset 우선</option>
        <option value="keep_current" ${selectedNpcCustomPresetConflictMode === "keep_current" ? "selected" : ""}>현재 유지</option>
      </select>
    </div>
    ${selectedPreset ? `
      <div class="muted">${escapeHtml(selectedPreset.name)} · ${npcCustomPresetSummary(selectedPreset)}${selectedPreset.note ? ` · ${escapeHtml(selectedPreset.note)}` : ""}</div>
      <div class="muted">diff${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(Math.max(Math.abs(npcCustomPresetDiff.serviceDelta || 0), Math.abs(npcCustomPresetDiff.questSeedDelta || 0)))))} · service Δ ${npcCustomPresetDiff.serviceDelta >= 0 ? "+" : ""}${npcCustomPresetDiff.serviceDelta} · quest seed Δ ${npcCustomPresetDiff.questSeedDelta >= 0 ? "+" : ""}${npcCustomPresetDiff.questSeedDelta}</div>
      <div class="muted">new service ${npcCustomPresetDiff.newServices.length}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcCustomPresetDiff.newServices.length)))} · duplicate service ${npcCustomPresetDiff.duplicateServices.length}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcCustomPresetDiff.duplicateServices.length)))} · new seed ${npcCustomPresetDiff.newSeeds.length}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcCustomPresetDiff.newSeeds.length)))} · duplicate seed ${npcCustomPresetDiff.duplicateSeeds.length}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcCustomPresetDiff.duplicateSeeds.length)))}</div>
      ${(npcCustomPresetDiff.duplicateServices.length || npcCustomPresetDiff.duplicateSeeds.length) ? `<div class="muted">중복 preview · service ${escapeHtml(npcCustomPresetDiff.duplicateServices.slice(0, 3).map((entry) => entry.label || entry.type || "service").join(", ") || "없음")} · seed ${escapeHtml(npcCustomPresetDiff.duplicateSeeds.slice(0, 3).map((entry) => entry.id || entry.title || "seed").join(", ") || "없음")}</div>` : ""}
      <div class="preset-field">
        <label for="npcCustomPresetMergePatchJson">Merge patch export</label>
        <div class="preset-toolbar">
          <button id="copyNpcCustomPresetMergePatchBtn" ${npcDefId ? "" : "disabled"}>patch 복사</button>
          <button id="downloadNpcCustomPresetMergePatchBtn" ${npcCustomPresetMergePatch ? "" : "disabled"}>patch 다운로드</button>
          <button id="importNpcCustomPresetMergePatchBtn" ${npcDefId ? "" : "disabled"}>클립보드 불러오기</button>
          <button id="applyNpcCustomPresetMergePatchBtn" ${npcDefId ? "" : "disabled"}>patch 적용</button>
          <button id="undoNpcCustomPresetPatchBtn" ${latestNpcPresetUndoEntry ? "" : "disabled"}>마지막 patch 되돌리기</button>
          <button id="redoNpcCustomPresetPatchBtn" ${latestNpcPresetRedoEntry?.redoSnapshot ? "" : "disabled"}>redo</button>
        </div>
        <textarea id="npcCustomPresetMergePatchJson" rows="10" spellcheck="false">${escapeHtml(npcCustomPresetMergePatchDraftValue)}</textarea>
        <div class="validation-report">
          <div class="validation-line is-${npcCustomPresetMergePatchValidation.summary.error ? "error" : "info"}"><strong>patch</strong> ${validationSummaryText(npcCustomPresetMergePatchValidation)}</div>
          <div class="validation-line is-info"><strong>entry</strong> service ${npcCustomPresetMergePatchPreview?.serviceCount || 0}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcCustomPresetMergePatchPreview?.serviceCount || 0)))} · quest seed ${npcCustomPresetMergePatchPreview?.questSeedCount || 0}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcCustomPresetMergePatchPreview?.questSeedCount || 0)))}</div>
          ${npcPresetApplyComparePreview
            ? `<div class="validation-line is-info"><strong>apply compare</strong>${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(Math.max(Math.abs((npcPresetApplyComparePreview.resolvedServiceCount || 0) - (npcPresetApplyComparePreview.currentServiceCount || 0)), Math.abs((npcPresetApplyComparePreview.resolvedQuestSeedCount || 0) - (npcPresetApplyComparePreview.currentQuestSeedCount || 0))))))} current service ${npcPresetApplyComparePreview.currentServiceCount} -> ${npcPresetApplyComparePreview.resolvedServiceCount} · current seed ${npcPresetApplyComparePreview.currentQuestSeedCount} -> ${npcPresetApplyComparePreview.resolvedQuestSeedCount}</div>`
            : ""}
          ${(npcCustomPresetMergePatchValidation.issues || []).slice(0, 6).map((issue) => `<div class="validation-line is-${issue.severity}"><strong>${issue.severity}</strong> ${escapeHtml(issue.message)}</div>`).join("") || `<div class="validation-line is-info"><strong>ok</strong> merge patch 구조 issue 없음</div>`}
        </div>
        ${npcPresetApplyComparePreview ? `
          <div class="preset-stack">
            <label for="npcPresetApplyCompareCurrentJson">Current definition</label>
            <textarea id="npcPresetApplyCompareCurrentJson" rows="8" spellcheck="false" readonly>${escapeHtml(npcPresetApplyComparePreview.currentJson)}</textarea>
            <label for="npcPresetApplyCompareResolvedJson">Simulated apply result</label>
            <textarea id="npcPresetApplyCompareResolvedJson" rows="8" spellcheck="false" readonly>${escapeHtml(npcPresetApplyComparePreview.resolvedJson)}</textarea>
            <label for="npcPresetApplyCompareLineDiffJson">Apply line diff</label>
            <textarea id="npcPresetApplyCompareLineDiffJson" rows="8" spellcheck="false" readonly>${escapeHtml(npcPresetApplyComparePreview.lineDiffText)}</textarea>
          </div>
        ` : ""}
        <div class="validation-report">
          <div class="validation-line is-info"><strong>history</strong> ${npcPresetPatchHistory.length}개 recent action${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcPresetPatchHistory.length)))}</div>
          ${latestNpcPresetUndoEntry ? `<div class="validation-line is-info"><strong>undo</strong>${renderDiffBadgeHtml(buildDiffBadgeSpec("ready"))} ${escapeHtml(latestNpcPresetUndoEntry.action || "patch")} · ${escapeHtml(latestNpcPresetUndoEntry.label || "")}</div>` : ""}
          ${latestNpcPresetRedoEntry?.redoSnapshot ? `<div class="validation-line is-info"><strong>redo</strong>${renderDiffBadgeHtml(buildDiffBadgeSpec("ready"))} ${escapeHtml(latestNpcPresetRedoEntry.action || "patch")} · ${escapeHtml(latestNpcPresetRedoEntry.label || "")} · stack ${npcPresetRedoEntries.length}</div>` : ""}
          <div class="validation-line is-info"><strong>redo archive</strong> ${npcPresetRedoArchive.length}개 persistent redo snapshot${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcPresetRedoArchive.length)))}</div>
          ${npcPresetPatchHistory.slice(0, 6).map((entry) => `<div class="validation-line is-info"><strong>${escapeHtml(entry.action || "patch")}</strong> ${escapeHtml(entry.npcId || npcDefId || "npc")} · ${escapeHtml(entry.label || "")} · ${escapeHtml(entry.actedAt || "")}</div>`).join("") || `<div class="validation-line is-info"><strong>info</strong> 아직 기록된 patch action이 없다.</div>`}
        </div>
        <div class="preset-field">
          <label for="npcPresetRedoArchiveQueryInput">Redo archive search</label>
          <input id="npcPresetRedoArchiveQueryInput" value="${escapeHtml(npcPresetRedoArchiveQuery)}" placeholder="action, npcId, label 검색" />
          <div class="muted">archive ${filteredNpcPresetRedoArchive.length}/${npcPresetRedoArchive.length}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(filteredNpcPresetRedoArchive.length)))}</div>
          ${npcPresetRedoArchiveBatchCompare ? `
            <div class="validation-report">
              <div class="validation-line is-info"><strong>redo batch compare</strong>${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcPresetRedoArchiveBatchCompare.totalCount)))} filtered ${npcPresetRedoArchiveBatchCompare.totalCount} · npc ${npcPresetRedoArchiveBatchCompare.npcComparisons.length} · action ${Object.entries(npcPresetRedoArchiveBatchCompare.actionCounts).map(([action, count]) => `${escapeHtml(action)} ${count}`).join(" / ")}</div>
              ${npcPresetRedoArchiveBatchCompare.npcComparisons.slice(0, 5).map((entry) => `<div class="validation-line is-info"><strong>${escapeHtml(entry.npcId)}</strong> ${entry.count}개 · service ${entry.lastServiceCount} (${entry.serviceDelta >= 0 ? "+" : ""}${entry.serviceDelta}) · seed ${entry.lastQuestSeedCount} (${entry.questSeedDelta >= 0 ? "+" : ""}${entry.questSeedDelta}) · latest ${escapeHtml(entry.last?.archivedAt || "")}</div>`).join("")}
            </div>
          ` : ""}
        </div>
        ${filteredNpcPresetRedoArchive.length ? `
          <div class="preset-stack">
            <label for="npcPresetRedoArchiveSelect">Redo archive restore</label>
            <select id="npcPresetRedoArchiveSelect">
              ${filteredNpcPresetRedoArchive.map((entry) => `<option value="${entry.id}" ${entry.id === selectedNpcPresetRedoArchiveId ? "selected" : ""}>${escapeHtml(npcPresetRedoArchiveLine(entry))}</option>`).join("")}
            </select>
            <div class="preset-toolbar">
              <button id="restoreNpcPresetRedoArchiveBtn" ${selectedNpcPresetRedoArchiveEntry?.redoSnapshot ? "" : "disabled"}>redo archive 복원</button>
              <button id="deleteNpcPresetRedoArchiveBtn" ${selectedNpcPresetRedoArchiveEntry ? "" : "disabled"}>redo archive 삭제</button>
            </div>
            <label for="npcPresetRedoArchivePreviewJson">Redo archive preview</label>
            <textarea id="npcPresetRedoArchivePreviewJson" rows="8" spellcheck="false" readonly>${escapeHtml(JSON.stringify(selectedNpcPresetRedoArchiveEntry || {}, null, 2))}</textarea>
          </div>
        ` : ""}
        <div class="validation-report">
          <div class="validation-line is-info"><strong>archive</strong> ${npcPresetPatchArchive.length}개 persistent patch action${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(npcPresetPatchArchive.length)))}</div>
          ${filteredNpcPresetPatchArchive.slice(0, 6).map((entry) => `<div class="validation-line is-info"><strong>${escapeHtml(entry.action || "patch")}</strong> ${escapeHtml(entry.npcId || npcDefId || "npc")} · ${escapeHtml(entry.label || "")} · ${escapeHtml(entry.archivedAt || "")}</div>`).join("") || `<div class="validation-line is-info"><strong>info</strong> 검색 결과 patch archive가 없다.</div>`}
        </div>
        <div class="preset-field">
          <label for="npcPresetPatchArchiveQueryInput">Patch archive search</label>
          <input id="npcPresetPatchArchiveQueryInput" value="${escapeHtml(npcPresetPatchArchiveQuery)}" placeholder="action, npcId, label 검색" />
          <div class="muted">archive ${filteredNpcPresetPatchArchive.length}/${npcPresetPatchArchive.length}${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(filteredNpcPresetPatchArchive.length)))}</div>
        </div>
        ${filteredNpcPresetPatchArchive.length ? `
          <div class="preset-stack">
            <label for="npcPresetPatchArchiveSelect">Patch archive restore</label>
            <select id="npcPresetPatchArchiveSelect">
              ${filteredNpcPresetPatchArchive.map((entry) => `<option value="${entry.id}" ${entry.id === selectedNpcPresetPatchArchiveId ? "selected" : ""}>${escapeHtml(npcPresetPatchArchiveLine(entry))}</option>`).join("")}
            </select>
            <div class="preset-toolbar">
              <button id="restoreNpcPresetPatchArchiveBtn" ${selectedNpcPresetPatchArchiveEntry?.patchDraft ? "" : "disabled"}>archive 복원</button>
              <button id="deleteNpcPresetPatchArchiveBtn" ${selectedNpcPresetPatchArchiveEntry ? "" : "disabled"}>archive 삭제</button>
              <button id="deleteFilteredNpcPresetPatchArchiveBtn" ${filteredNpcPresetPatchArchive.length ? "" : "disabled"}>검색 결과 삭제</button>
            </div>
            <div class="validation-report">
              <div class="validation-line is-info"><strong>compare</strong>${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(Math.max(Math.abs((npcPresetPatchArchiveCompare.archiveServiceCount || 0) - (npcPresetPatchArchiveCompare.currentServiceCount || 0)), Math.abs((npcPresetPatchArchiveCompare.archiveQuestSeedCount || 0) - (npcPresetPatchArchiveCompare.currentQuestSeedCount || 0))))))} current service ${npcPresetPatchArchiveCompare.currentServiceCount} / archive ${npcPresetPatchArchiveCompare.archiveServiceCount} · current seed ${npcPresetPatchArchiveCompare.currentQuestSeedCount} / archive ${npcPresetPatchArchiveCompare.archiveQuestSeedCount}</div>
            </div>
            <label for="npcPresetPatchArchivePreviewJson">Archive patch preview</label>
            <textarea id="npcPresetPatchArchivePreviewJson" rows="8" spellcheck="false" readonly>${escapeHtml(selectedNpcPresetPatchArchiveEntry?.patchDraft || npcPresetPatchArchivePreviewText)}</textarea>
            <label for="npcPresetPatchArchiveLineDiffJson">Archive line diff</label>
            <textarea id="npcPresetPatchArchiveLineDiffJson" rows="8" spellcheck="false" readonly>${escapeHtml(npcPresetPatchArchiveLineDiffText)}</textarea>
          </div>
        ` : ""}
      </div>
      <div class="preset-field">
        <label>Selective service merge</label>
        <div class="preset-stack">
          ${npcCustomPresetDiff.serviceRows.length
            ? npcCustomPresetDiff.serviceRows.map((row) => `
              <div class="preset-stack">
                <label class="muted"><input type="checkbox" data-npc-preset-service-index="${row.index}" ${selectedPresetServiceIndexes.includes(row.index) ? "checked" : ""} /> ${escapeHtml(row.entry.label || row.entry.type || `service_${row.index}`)} · ${row.status}${row.changedFields.length ? ` · field ${escapeHtml(row.changedFields.slice(0, 5).join(", "))}` : ""}</label>
                ${row.changedFields?.length ? `<div class="preset-stack">${row.changedFields.map((fieldName) => `<label class="muted"><input type="checkbox" data-npc-preset-service-field="${row.index}:${escapeHtml(fieldName)}" ${selectedNpcPresetServiceFieldNames(row).includes(fieldName) ? "checked" : ""} /> field ${escapeHtml(fieldName)}</label>`).join("")}</div>` : ""}
                ${row.dialogueStepRows?.length ? `<div class="preset-stack">${row.dialogueStepRows.map((stepRow) => `
                  <div class="preset-stack">
                    <label class="muted"><input type="checkbox" data-npc-preset-dialogue-step-index="${row.index}:${stepRow.index}" ${selectedNpcPresetDialogueStepIndexes(selectedPreset, row.index).includes(stepRow.index) ? "checked" : ""} /> dialogue ${escapeHtml(stepRow.step.id || `step_${stepRow.index}`)} · ${stepRow.status}${stepRow.changedFields.length ? ` · field ${escapeHtml(stepRow.changedFields.slice(0, 5).join(", "))}` : ""}</label>
                    ${stepRow.choiceRows?.length ? `<div class="preset-stack">${stepRow.choiceRows.map((choiceRow) => `<label class="muted"><input type="checkbox" data-npc-preset-dialogue-choice-index="${row.index}:${stepRow.index}:${choiceRow.index}" ${selectedNpcPresetDialogueChoiceIndexes(selectedPreset, row.index, stepRow.index).includes(choiceRow.index) ? "checked" : ""} /> choice ${escapeHtml(choiceRow.choice.label || choiceRow.choice.text || `choice_${choiceRow.index}`)} · ${choiceRow.status}${choiceRow.changedFields.length ? ` · field ${escapeHtml(choiceRow.changedFields.slice(0, 5).join(", "))}` : ""}</label>`).join("")}</div>` : ""}
                    ${stepRow.branchRows?.length ? `<div class="preset-stack">${stepRow.branchRows.map((branchRow) => `<label class="muted"><input type="checkbox" data-npc-preset-dialogue-branch-index="${row.index}:${stepRow.index}:${branchRow.index}" ${selectedNpcPresetDialogueBranchIndexes(selectedPreset, row.index, stepRow.index).includes(branchRow.index) ? "checked" : ""} /> branch ${escapeHtml(branchRow.branch.label || branchRow.branch.nextStepId || `branch_${branchRow.index}`)} · ${branchRow.status}${branchRow.changedFields.length ? ` · field ${escapeHtml(branchRow.changedFields.slice(0, 5).join(", "))}` : ""}</label>`).join("")}</div>` : ""}
                    ${stepRow.existingStep ? `<details><summary class="muted">step side-by-side diff</summary>${npcPresetSideBySideDiffMarkup(stepRow.existingStep, stepRow.step, stepRow.changedFields, `npcPresetStepDiff${row.index}_${stepRow.index}`, stepRow.status)}</details>` : ""}
                    ${stepRow.existingStep ? `<details><summary class="muted">step 3-way preview</summary>${npcPresetThreeWayPreviewMarkup(stepRow.existingStep, stepRow.step, buildNpcPresetResolvedDialogueStep(stepRow, selectedPreset, row.index, conflictMode), `npcPresetStepPreview${row.index}_${stepRow.index}`)}</details>` : ""}
                  </div>
                `).join("")}</div>` : ""}
                ${row.existing ? `<details><summary class="muted">service side-by-side diff</summary>${npcPresetSideBySideDiffMarkup(row.existing, row.entry, row.changedFields, `npcPresetServiceDiff${row.index}`, row.status)}</details>` : ""}
                ${row.existing ? `<details><summary class="muted">service 3-way preview</summary>${npcPresetThreeWayPreviewMarkup(row.existing, row.entry, buildNpcPresetResolvedServicePreview(row, selectedPreset, conflictMode), `npcPresetServicePreview${row.index}`)}</details>` : ""}
              </div>
            `).join("")
            : `<div class="muted">preset service 없음</div>`}
        </div>
      </div>
      <div class="preset-field">
        <label>Selective quest seed merge</label>
        <div class="preset-stack">
          ${npcCustomPresetDiff.seedRows.length
            ? npcCustomPresetDiff.seedRows.map((row) => `<div class="preset-stack"><label class="muted"><input type="checkbox" data-npc-preset-seed-index="${row.index}" ${selectedPresetSeedIndexes.includes(row.index) ? "checked" : ""} /> ${escapeHtml(row.entry.id || row.entry.title || `seed_${row.index}`)} · ${row.status}${row.changedFields.length ? ` · field ${escapeHtml(row.changedFields.slice(0, 5).join(", "))}` : ""}</label>${row.changedFields?.length ? `<div class="preset-stack">${row.changedFields.map((fieldName) => `<label class="muted"><input type="checkbox" data-npc-preset-seed-field="${row.index}:${escapeHtml(fieldName)}" ${selectedNpcPresetSeedFieldNames(row).includes(fieldName) ? "checked" : ""} /> field ${escapeHtml(fieldName)}</label>`).join("")}</div>` : ""}${row.existing ? `<details><summary class="muted">seed side-by-side diff</summary>${npcPresetSideBySideDiffMarkup(row.existing, row.entry, row.changedFields, `npcPresetSeedDiff${row.index}`, row.status)}</details>` : ""}${row.existing ? `<details><summary class="muted">seed 3-way preview</summary>${npcPresetThreeWayPreviewMarkup(row.existing, row.entry, buildNpcPresetResolvedSeedPreview(row, conflictMode), `npcPresetSeedPreview${row.index}`)}</details>` : ""}</div>`).join("")
            : `<div class="muted">preset quest seed 없음</div>`}
        </div>
      </div>
    ` : `<div class="muted">저장된 NPC custom preset이 없다.</div>`}
  `;
}

export function renderEditorNpcServiceEditorSection(deps = {}) {
  const {
    npcDef = null,
    npcQuestSeed = null,
    selectedNpcServiceDef = null,
    selectedNpcServiceDefState = { index: 0 },
    selectedNpcDialogueStepDef = null,
    selectedNpcDialogueStepDefState = { index: 0 },
    vendors = {},
    classes = [],
    encounters = {},
    npcHookJson = () => "[]",
    npcServicePreviewText = () => "",
    npcServicePreviewList = () => [],
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  return `
    <div class="preset-field">
      <label for="npcServicesJsonInput">Services JSON</label>
      <textarea id="npcServicesJsonInput" rows="8" spellcheck="false">${escapeHtml(npcHookJson(npcDef, "services", []))}</textarea>
    </div>
    <div class="preset-field">
      <label>Service group quick-add</label>
      <div class="preset-toolbar">
        <button id="addQuestHubNpcServiceGroupBtn">quest hub</button>
        <button id="addSupportNpcServiceGroupBtn">support</button>
        <button id="addMerchantNpcServiceGroupBtn">merchant</button>
        <button id="addCompanionNpcServiceGroupBtn">companion</button>
        <button id="addHostileNpcServiceGroupBtn">hostile</button>
      </div>
    </div>
    ${selectedNpcServiceDef ? `
      <div class="preset-toolbar">
        <button id="duplicateNpcServiceBtn">선택 service 복제</button>
        <button id="moveNpcServiceUpBtn">위로</button>
        <button id="moveNpcServiceDownBtn">아래로</button>
      </div>
      <div class="preset-field">
        <label for="npcServiceSelect">Selected service</label>
        <select id="npcServiceSelect">${(npcDef.services || []).map((service, index) => `<option value="${index}" ${index === selectedNpcServiceDefState.index ? "selected" : ""}>${index} · ${escapeHtml(service.label || service.type || `service_${index}`)}</option>`).join("")}</select>
      </div>
      <div class="preset-field">
        <label for="npcServiceTypeSelect">Service type</label>
        <select id="npcServiceTypeSelect">
          ${["talk", "quest", "heal", "identify", "trade", "recruit", "dismiss", "fight"].map((type) => `<option value="${type}" ${selectedNpcServiceDef.type === type ? "selected" : ""}>${type}</option>`).join("")}
        </select>
      </div>
      <div class="preset-field">
        <label for="npcServiceLabelInput">Service label</label>
        <input id="npcServiceLabelInput" value="${escapeHtml(selectedNpcServiceDef.label || "")}" />
      </div>
      ${selectedNpcServiceDef.type === "talk" ? `
        <div class="preset-field">
          <label for="npcServiceTalkNoteInput">Talk note</label>
          <textarea id="npcServiceTalkNoteInput" rows="3" spellcheck="false">${escapeHtml(selectedNpcServiceDef.note || "")}</textarea>
        </div>
        <div class="preset-field">
          <label for="npcServiceDialogueJsonInput">Dialogue JSON</label>
          <textarea id="npcServiceDialogueJsonInput" rows="8" spellcheck="false">${escapeHtml(JSON.stringify(selectedNpcServiceDef.dialogue || { steps: [] }, null, 2))}</textarea>
        </div>
        ${selectedNpcDialogueStepDef ? `
          <div class="preset-toolbar">
            <button id="addNpcDialogueStepBtn">dialogue step 추가</button>
            <button id="removeNpcDialogueStepBtn">선택 step 삭제</button>
          </div>
          <div class="preset-field">
            <label for="npcDialogueStepSelect">Selected dialogue step</label>
            <select id="npcDialogueStepSelect">${(selectedNpcServiceDef.dialogue?.steps || []).map((step, index) => `<option value="${index}" ${index === selectedNpcDialogueStepDefState.index ? "selected" : ""}>${index} · ${escapeHtml(step.id || `dialogue_${index}`)}</option>`).join("")}</select>
          </div>
          <div class="preset-field">
            <label for="npcDialogueEntryStepIdInput">Dialogue entry step</label>
            <input id="npcDialogueEntryStepIdInput" value="${escapeHtml(selectedNpcServiceDef.dialogue?.entryStepId || "")}" placeholder="비우면 첫 step 사용" />
          </div>
          <div class="preset-field">
            <label for="npcDialogueStepIdInput">Dialogue step id</label>
            <input id="npcDialogueStepIdInput" value="${escapeHtml(selectedNpcDialogueStepDef.id || "")}" />
          </div>
          <div class="preset-field">
            <label for="npcDialogueStepTitleInput">Dialogue step title</label>
            <input id="npcDialogueStepTitleInput" value="${escapeHtml(selectedNpcDialogueStepDef.title || "")}" />
          </div>
          <div class="preset-field">
            <label for="npcDialogueStepTextInput">Dialogue step text</label>
            <textarea id="npcDialogueStepTextInput" rows="4" spellcheck="false">${escapeHtml(selectedNpcDialogueStepDef.text || "")}</textarea>
          </div>
          <div class="preset-field">
            <label for="npcDialogueStepNextIdInput">Dialogue default next</label>
            <input id="npcDialogueStepNextIdInput" value="${escapeHtml(selectedNpcDialogueStepDef.nextStepId || "")}" placeholder="선택지가 없을 때 자동 이동" />
          </div>
          <div class="preset-field">
            <label>Dialogue choices</label>
            <div class="preset-stack">
              ${(selectedNpcDialogueStepDef.choices || []).map((choice, index) => `
                <div class="preset-stack">
                  <div class="preset-toolbar">
                    <input data-npc-dialogue-choice-label="${index}" value="${escapeHtml(choice.label || "")}" placeholder="선택지 라벨" />
                    <input data-npc-dialogue-choice-next="${index}" value="${escapeHtml(choice.nextStepId || "")}" placeholder="next step id" />
                    <button data-remove-npc-dialogue-choice="${index}">삭제</button>
                  </div>
                  <textarea data-npc-dialogue-choice-note="${index}" rows="2" spellcheck="false" placeholder="선택 시 로그 note">${escapeHtml(choice.note || "")}</textarea>
                </div>
              `).join("") || `<div class="muted">choice 없음</div>`}
              <button id="addNpcDialogueChoiceBtn">choice 추가</button>
            </div>
          </div>
        ` : `
          <div class="preset-toolbar">
            <button id="addNpcDialogueStepBtn">dialogue step 추가</button>
          </div>
          <div class="muted">dialogue step이 없어서 guided editor를 숨겼다. 버튼으로 기본 step을 먼저 추가한다.</div>
        `}
      ` : ""}
      ${selectedNpcServiceDef.type === "heal" ? `
        <div class="preset-field">
          <label for="npcServiceHealAmountInput">Heal amount</label>
          <input id="npcServiceHealAmountInput" type="number" min="0" value="${Math.max(0, Number(selectedNpcServiceDef.heal || 0))}" />
        </div>
        <div class="preset-field">
          <label for="npcServiceHealStatusInput">Cure status</label>
          <input id="npcServiceHealStatusInput" value="${escapeHtml(selectedNpcServiceDef.cureStatus || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcServiceGoldCostInput">Gold cost</label>
          <input id="npcServiceGoldCostInput" type="number" min="0" value="${Math.max(0, Number(selectedNpcServiceDef.cost?.gold || 0))}" />
        </div>
      ` : ""}
      ${selectedNpcServiceDef.type === "identify" ? `
        <div class="preset-field">
          <label for="npcServiceGoldCostInput">Gold cost</label>
          <input id="npcServiceGoldCostInput" type="number" min="0" value="${Math.max(0, Number(selectedNpcServiceDef.cost?.gold || 0))}" />
        </div>
      ` : ""}
      ${selectedNpcServiceDef.type === "trade" ? `
        <div class="preset-field">
          <label for="npcServiceVendorSelect">Vendor</label>
          <select id="npcServiceVendorSelect">
            <option value="" ${selectedNpcServiceDef.vendorId ? "" : "selected"}>(unset)</option>
            ${Object.entries(vendors).map(([id, vendor]) => `<option value="${id}" ${selectedNpcServiceDef.vendorId === id ? "selected" : ""}>${id} · ${vendor.summary || vendor.serviceType}</option>`).join("")}
          </select>
        </div>
      ` : ""}
      ${selectedNpcServiceDef.type === "recruit" ? `
        <div class="preset-field">
          <label for="npcServiceCompanionNameInput">Companion name</label>
          <input id="npcServiceCompanionNameInput" value="${escapeHtml(selectedNpcServiceDef.companionProfile?.name || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcServiceCompanionClassSelect">Companion class</label>
          <select id="npcServiceCompanionClassSelect">${classes.map((entry, index) => `<option value="${index}" ${Number(selectedNpcServiceDef.companionProfile?.classIndex || 0) === index ? "selected" : ""}>${index} · ${entry.cls}</option>`).join("")}</select>
        </div>
        <div class="preset-field">
          <label for="npcServiceCompanionNoteInput">Companion note</label>
          <textarea id="npcServiceCompanionNoteInput" rows="3" spellcheck="false">${escapeHtml(selectedNpcServiceDef.companionProfile?.note || "")}</textarea>
        </div>
      ` : ""}
      ${selectedNpcServiceDef.type === "fight" ? `
        <div class="preset-field">
          <label for="npcServiceEncounterSelect">Encounter</label>
          <select id="npcServiceEncounterSelect">
            <option value="" ${selectedNpcServiceDef.encounterId ? "" : "selected"}>(unset)</option>
            ${Object.entries(encounters).map(([id, encounter]) => `<option value="${id}" ${selectedNpcServiceDef.encounterId === id ? "selected" : ""}>${id} · ${encounter.name}</option>`).join("")}
          </select>
        </div>
        <div class="preset-field">
          <label for="npcServiceHostileFlagInput">Hostile flag</label>
          <input id="npcServiceHostileFlagInput" value="${escapeHtml(selectedNpcServiceDef.hostileFlag || "")}" placeholder="기본값: placement_id_hostile" />
        </div>
        <div class="preset-field">
          <label for="npcServiceHostileLogInput">Hostile log</label>
          <textarea id="npcServiceHostileLogInput" rows="3" spellcheck="false">${escapeHtml(selectedNpcServiceDef.hostileLog || "")}</textarea>
        </div>
        <div class="preset-field">
          <label for="npcServiceAvoidLabelInput">Avoid label</label>
          <input id="npcServiceAvoidLabelInput" value="${escapeHtml(selectedNpcServiceDef.avoidLabel || "")}" placeholder="예: 금화를 내고 지나간다" />
        </div>
        <div class="preset-field">
          <label for="npcServiceAvoidGoldCostInput">Avoid gold cost</label>
          <input id="npcServiceAvoidGoldCostInput" type="number" min="0" value="${Math.max(0, Number(selectedNpcServiceDef.avoidCost?.gold || 0))}" />
        </div>
        <div class="preset-field">
          <label for="npcServiceAvoidFlagInput">Avoid flag</label>
          <input id="npcServiceAvoidFlagInput" value="${escapeHtml(selectedNpcServiceDef.avoidFlag || "")}" />
        </div>
        <div class="preset-field">
          <label for="npcServiceAvoidLogInput">Avoid log</label>
          <textarea id="npcServiceAvoidLogInput" rows="3" spellcheck="false">${escapeHtml(selectedNpcServiceDef.avoidLog || "")}</textarea>
        </div>
      ` : ""}
      <div class="preset-toolbar">
        <button id="addTalkNpcServiceBtn">talk 추가</button>
        <button id="addQuestNpcServiceBtn">quest 추가</button>
        <button id="addHealNpcServiceBtn">heal 추가</button>
        <button id="addIdentifyNpcServiceBtn">identify 추가</button>
        <button id="addTradeNpcServiceBtn">trade 추가</button>
        <button id="addRecruitNpcServiceBtn">recruit 추가</button>
        <button id="addDismissNpcServiceBtn">dismiss 추가</button>
        <button id="addFightNpcServiceBtn">fight 추가</button>
        <button id="removeNpcServiceBtn">선택 service 삭제</button>
      </div>
      <div class="muted">guided service ${escapeHtml(selectedNpcServiceDef.label || selectedNpcServiceDef.type || "unnamed")} · ${escapeHtml(npcServicePreviewText(selectedNpcServiceDef, npcDef))}</div>
    ` : `
      <div class="preset-toolbar">
        <button id="addTalkNpcServiceBtn">talk 추가</button>
        <button id="addQuestNpcServiceBtn">quest 추가</button>
        <button id="addHealNpcServiceBtn">heal 추가</button>
        <button id="addIdentifyNpcServiceBtn">identify 추가</button>
        <button id="addTradeNpcServiceBtn">trade 추가</button>
        <button id="addRecruitNpcServiceBtn">recruit 추가</button>
        <button id="addDismissNpcServiceBtn">dismiss 추가</button>
        <button id="addFightNpcServiceBtn">fight 추가</button>
      </div>
      <div class="muted">service가 없어서 guided editor를 숨겼다. 버튼으로 기본 service를 먼저 추가한다.</div>
    `}
    <div class="preset-field">
      <label>Service preview</label>
      <div class="muted">${npcServicePreviewList(npcDef).map((entry) => `${escapeHtml(entry.label)}: ${escapeHtml(entry.summary)}`).join("<br />") || "service 없음"}</div>
    </div>
    <div class="muted">active seed ${npcQuestSeed?.title || "없음"}${npcQuestSeed?.grantFlag ? ` · grant ${npcQuestSeed.grantFlag}` : ""}</div>
  `;
}

export function renderEditorEventGraphBody(deps = {}) {
  const {
    eventDef = null,
    selectedEventStepDef = null,
    selectedEventStepDefState = { index: 0 },
    eventGraphPreview = null,
    currentGraphExportJson = "",
    previousGraphArchiveDiff = null,
    previousGraphArchive = null,
    eventExportArchiveLine = () => "",
    eventGraphJsonDiffText = "",
    eventGraphSummaryDiff = null,
    activeEditorEventTest = null,
    activeEditorEventInteraction = null,
    eventValidationSnapshot = null,
    classes = [],
    resourceKeys = [],
    partyStatKeys = [],
    eventEffectTypes = [],
    eventPlacementKind = "",
    allowedInteractionsForPlacementKind = () => [],
    linkedPlacements = [],
    linkedIssues = [],
    renderEventEffectFields = () => "",
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  return `
    <div class="preset-field">
      <label>Graph preview</label>
      <div class="validation-report">
        ${eventGraphPreview?.steps?.length
          ? eventGraphPreview.steps.map((step) => `
              <div class="validation-line is-${step.isEntry ? "info" : "muted"}">
                <strong>${escapeHtml(step.id)}</strong>
                ${step.title ? ` · ${escapeHtml(step.title)}` : ""}
                ${step.isEntry ? " · entry" : ""}
                ${step.index === selectedEventStepDefState.index ? " · selected" : ""}
                ${step.nextTargets.length ? ` · next ${escapeHtml(step.nextTargets.join(", "))}` : " · terminal"}
                · branch ${step.branchCount}
                · choice ${step.choiceCount}
                · effect ${step.effectCount}
              </div>
            `).join("")
          : `<div class="validation-line is-info"><strong>info</strong> step graph 없음</div>`}
      </div>
    </div>
    <div class="preset-field">
      <label for="eventGraphCompactExportJson">Graph export</label>
      <div class="preset-toolbar">
        <button id="copyEventGraphExportBtn">graph 복사</button>
        <button id="downloadEventGraphExportBtn">graph 다운로드</button>
      </div>
      <textarea id="eventGraphCompactExportJson" rows="10" spellcheck="false" readonly>${escapeHtml(currentGraphExportJson)}</textarea>
    </div>
    <div class="preset-field">
      <label>Previous graph summary diff</label>
      <div class="validation-report">
        ${previousGraphArchiveDiff
          ? `<div class="validation-line is-info"><strong>delta</strong> step ${previousGraphArchiveDiff.stepDelta >= 0 ? "+" : ""}${previousGraphArchiveDiff.stepDelta} · branch ${previousGraphArchiveDiff.branchDelta >= 0 ? "+" : ""}${previousGraphArchiveDiff.branchDelta} · choice ${previousGraphArchiveDiff.choiceDelta >= 0 ? "+" : ""}${previousGraphArchiveDiff.choiceDelta}</div>
             <div class="validation-line is-muted"><strong>prev</strong> ${escapeHtml(eventExportArchiveLine(previousGraphArchive))}</div>`
          : `<div class="validation-line is-info"><strong>info</strong> 비교할 이전 graph archive 없음</div>`}
      </div>
    </div>
    <div class="preset-field">
      <label for="eventGraphJsonDiffInput">Graph JSON diff</label>
      <div class="preset-toolbar">
        <button id="downloadEventGraphDiffBtn" ${eventGraphJsonDiffText ? "" : "disabled"}>diff 다운로드</button>
      </div>
      <textarea id="eventGraphJsonDiffInput" rows="12" spellcheck="false" readonly>${escapeHtml(eventGraphJsonDiffText || "비교 가능한 이전 graph archive payload가 없다.")}</textarea>
    </div>
    <div class="preset-field">
      <label>Graph summary diff</label>
      <div class="validation-report">
        ${eventGraphSummaryDiff ? `
          <div class="validation-line is-info"><strong>summary</strong> step ${eventGraphSummaryDiff.summary.stepCount} · root effect ${eventGraphSummaryDiff.summary.rootEffectCount} · branch ${eventGraphSummaryDiff.summary.branchCount} · choice ${eventGraphSummaryDiff.summary.choiceCount} · step effect ${eventGraphSummaryDiff.summary.stepEffectCount}</div>
          ${eventGraphSummaryDiff.danglingDefaultTargets.map((entry) => `<div class="validation-line is-warning"><strong>default</strong> ${escapeHtml(entry)}</div>`).join("")}
          ${eventGraphSummaryDiff.danglingBranchTargets.map((entry) => `<div class="validation-line is-warning"><strong>branch</strong> ${escapeHtml(entry)}</div>`).join("")}
          ${eventGraphSummaryDiff.danglingChoiceTargets.map((entry) => `<div class="validation-line is-warning"><strong>choice</strong> ${escapeHtml(entry)}</div>`).join("")}
          ${!eventGraphSummaryDiff.danglingDefaultTargets.length && !eventGraphSummaryDiff.danglingBranchTargets.length && !eventGraphSummaryDiff.danglingChoiceTargets.length
            ? `<div class="validation-line is-info"><strong>ok</strong> dangling target 없음</div>`
            : ""}
        ` : `<div class="validation-line is-info"><strong>info</strong> graph summary 없음</div>`}
      </div>
    </div>
    <div class="preset-field">
      <label>Graph test control</label>
      <div class="preset-stack">
        <div class="preset-toolbar">
          <button id="startEventEntryTestBtn">entry step 테스트</button>
          <button id="startSelectedEventStepTestBtn" ${selectedEventStepDef ? "" : "disabled"}>selected step 테스트</button>
          <button id="stopEventTestBtn" ${activeEditorEventTest ? "" : "disabled"}>테스트 종료</button>
        </div>
        <div class="validation-report">
          ${activeEditorEventTest
            ? `
              <div class="validation-line is-info"><strong>test</strong> ${escapeHtml(activeEditorEventTest.eventId)} · start ${escapeHtml(activeEditorEventTest.startStepId || "(entry)")}</div>
              <div class="validation-line is-${activeEditorEventInteraction ? "info" : activeEditorEventTest.completed ? "info" : "muted"}">
                <strong>state</strong>
                ${activeEditorEventInteraction
                  ? `${escapeHtml(activeEditorEventInteraction.stepId || "")} step에서 선택 대기 중`
                  : activeEditorEventTest.completed
                    ? "flow 완료"
                    : "진행 중"}
              </div>
              ${activeEditorEventInteraction?.text ? `<div class="validation-line is-muted"><strong>text</strong> ${escapeHtml(activeEditorEventInteraction.text)}</div>` : ""}
              ${activeEditorEventInteraction?.options?.length
                ? `<div class="preset-toolbar">${activeEditorEventInteraction.options.map((option, index) => `<button data-editor-event-test-option="${index}">${escapeHtml(option.label)}</button>`).join("")}</div>`
                : ""}
            `
            : `<div class="validation-line is-info"><strong>info</strong> 현재 editor event test session 없음</div>`}
        </div>
      </div>
    </div>
    ${selectedEventStepDef ? `
      <div class="preset-toolbar">
        <button id="addEventStepBtn">step 추가</button>
        <button id="removeEventStepBtn">선택 step 삭제</button>
      </div>
      <div class="preset-field">
        <label for="eventStepSelect">Selected step</label>
        <select id="eventStepSelect">${(eventDef.steps || []).map((step, index) => `<option value="${index}" ${index === selectedEventStepDefState.index ? "selected" : ""}>${index} · ${escapeHtml(step.id || `step_${index}`)}</option>`).join("")}</select>
      </div>
      <div class="preset-field">
        <label for="eventStepIdInput">Step id</label>
        <input id="eventStepIdInput" value="${escapeHtml(selectedEventStepDef.id || "")}" />
      </div>
      <div class="preset-field">
        <label for="eventStepTitleInput">Step title</label>
        <input id="eventStepTitleInput" value="${escapeHtml(selectedEventStepDef.title || "")}" />
      </div>
      <div class="preset-field">
        <label for="eventStepTextInput">Step text</label>
        <textarea id="eventStepTextInput" rows="3" spellcheck="false">${escapeHtml(selectedEventStepDef.text || "")}</textarea>
      </div>
      <div class="preset-field">
        <label for="eventStepNextIdInput">Default next step</label>
        <input id="eventStepNextIdInput" value="${escapeHtml(selectedEventStepDef.nextStepId || "")}" />
      </div>
      <div class="preset-field">
        <label>Selected step validation</label>
        <div class="validation-report">
          ${(eventValidationSnapshot?.selectedIssues || []).length
            ? eventValidationSnapshot.selectedIssues.map((issue) => `<div class="validation-line is-${issue.severity}"><strong>${escapeHtml(issue.scope)}</strong> ${escapeHtml(issue.message)}</div>`).join("")
            : `<div class="validation-line is-info"><strong>ok</strong> 선택 step issue 없음</div>`}
        </div>
      </div>
      <div class="preset-field">
        <label>Branches</label>
        <div class="preset-stack">
          ${(selectedEventStepDef.branches || []).map((branch, index) => `
            <div class="preset-stack">
              <div class="preset-toolbar">
                <input data-event-branch-label="${index}" value="${escapeHtml(branch.label || "")}" placeholder="branch label" />
                <input data-event-branch-next="${index}" value="${escapeHtml(branch.nextStepId || "")}" placeholder="next step id" />
                <button data-remove-event-branch="${index}">삭제</button>
              </div>
              <div class="preset-toolbar">
                <input data-event-branch-required-flag="${index}" value="${escapeHtml(branch.requiredFlag || "")}" placeholder="required flag" />
                <input data-event-branch-missing-flag="${index}" value="${escapeHtml(branch.missingFlag || "")}" placeholder="missing flag" />
              </div>
              <div class="preset-toolbar">
                <select data-event-branch-required-resource="${index}">
                  <option value="" ${(branch.requiredResource || "") ? "" : "selected"}>(resource 없음)</option>
                  ${resourceKeys.map((resource) => `<option value="${resource}" ${branch.requiredResource === resource ? "selected" : ""}>${resource}</option>`).join("")}
                </select>
                <input data-event-branch-required-resource-amount="${index}" type="number" min="0" value="${branch.requiredResourceAtLeast ?? 1}" placeholder="최소 자원" />
                <select data-event-branch-required-companion-state="${index}">
                  <option value="" ${(branch.requiredCompanionState || "") ? "" : "selected"}>(동료 조건 없음)</option>
                  <option value="absent" ${branch.requiredCompanionState === "absent" ? "selected" : ""}>동료 없음</option>
                  <option value="recruited" ${branch.requiredCompanionState === "recruited" ? "selected" : ""}>영입됨</option>
                  <option value="joined_party" ${branch.requiredCompanionState === "joined_party" ? "selected" : ""}>동행 중</option>
                  <option value="dismissed" ${branch.requiredCompanionState === "dismissed" ? "selected" : ""}>해산됨</option>
                </select>
              </div>
              <div class="preset-toolbar">
                <select data-event-branch-required-class="${index}">
                  <option value="" ${(branch.requiredClassIndex != null) ? "" : "selected"}>(class 없음)</option>
                  ${classes.map((entry, classIndex) => `<option value="${classIndex}" ${Number(branch.requiredClassIndex) === classIndex ? "selected" : ""}>${classIndex} · ${entry.cls}</option>`).join("")}
                </select>
                <select data-event-branch-required-stat="${index}">
                  <option value="" ${(branch.requiredStatKey || "") ? "" : "selected"}>(stat 없음)</option>
                  ${partyStatKeys.map((stat) => `<option value="${stat}" ${branch.requiredStatKey === stat ? "selected" : ""}>${stat}</option>`).join("")}
                </select>
                <input data-event-branch-required-stat-amount="${index}" type="number" min="0" value="${branch.requiredStatAtLeast ?? 1}" placeholder="최소 스탯" />
              </div>
              <div class="preset-toolbar">
                <input data-event-branch-required-seed="${index}" value="${escapeHtml(branch.requiredQuestSeedId || "")}" placeholder="required quest seed" />
                <input data-event-branch-required-status="${index}" value="${escapeHtml(branch.requiredQuestSeedStatus || "")}" placeholder="required quest seed status" />
              </div>
            </div>
          `).join("") || `<div class="muted">branch 없음</div>`}
          <button id="addEventBranchBtn">branch 추가</button>
        </div>
      </div>
      <div class="preset-field">
        <label>Choices</label>
        <div class="preset-stack">
          ${(selectedEventStepDef.choices || []).map((choice, index) => `
            <div class="preset-stack">
              <div class="preset-toolbar">
                <input data-event-choice-label="${index}" value="${escapeHtml(choice.label || choice.text || "")}" placeholder="choice label" />
                <input data-event-choice-next="${index}" value="${escapeHtml(choice.nextStepId || "")}" placeholder="next step id" />
                <button data-remove-event-choice="${index}">삭제</button>
              </div>
              <div class="preset-toolbar">
                <input data-event-choice-required-flag="${index}" value="${escapeHtml(choice.requiredFlag || "")}" placeholder="required flag" />
                <input data-event-choice-missing-flag="${index}" value="${escapeHtml(choice.missingFlag || "")}" placeholder="missing flag" />
              </div>
              <div class="preset-toolbar">
                <select data-event-choice-required-resource="${index}">
                  <option value="" ${(choice.requiredResource || "") ? "" : "selected"}>(resource 없음)</option>
                  ${resourceKeys.map((resource) => `<option value="${resource}" ${choice.requiredResource === resource ? "selected" : ""}>${resource}</option>`).join("")}
                </select>
                <input data-event-choice-required-resource-amount="${index}" type="number" min="0" value="${choice.requiredResourceAtLeast ?? 1}" placeholder="최소 자원" />
                <select data-event-choice-required-companion-state="${index}">
                  <option value="" ${(choice.requiredCompanionState || "") ? "" : "selected"}>(동료 조건 없음)</option>
                  <option value="absent" ${choice.requiredCompanionState === "absent" ? "selected" : ""}>동료 없음</option>
                  <option value="recruited" ${choice.requiredCompanionState === "recruited" ? "selected" : ""}>영입됨</option>
                  <option value="joined_party" ${choice.requiredCompanionState === "joined_party" ? "selected" : ""}>동행 중</option>
                  <option value="dismissed" ${choice.requiredCompanionState === "dismissed" ? "selected" : ""}>해산됨</option>
                </select>
              </div>
              <div class="preset-toolbar">
                <select data-event-choice-required-class="${index}">
                  <option value="" ${(choice.requiredClassIndex != null) ? "" : "selected"}>(class 없음)</option>
                  ${classes.map((entry, classIndex) => `<option value="${classIndex}" ${Number(choice.requiredClassIndex) === classIndex ? "selected" : ""}>${classIndex} · ${entry.cls}</option>`).join("")}
                </select>
                <select data-event-choice-required-stat="${index}">
                  <option value="" ${(choice.requiredStatKey || "") ? "" : "selected"}>(stat 없음)</option>
                  ${partyStatKeys.map((stat) => `<option value="${stat}" ${choice.requiredStatKey === stat ? "selected" : ""}>${stat}</option>`).join("")}
                </select>
                <input data-event-choice-required-stat-amount="${index}" type="number" min="0" value="${choice.requiredStatAtLeast ?? 1}" placeholder="최소 스탯" />
              </div>
              <div class="preset-toolbar">
                <input data-event-choice-required-seed="${index}" value="${escapeHtml(choice.requiredQuestSeedId || "")}" placeholder="required quest seed" />
                <input data-event-choice-required-status="${index}" value="${escapeHtml(choice.requiredQuestSeedStatus || "")}" placeholder="required quest seed status" />
              </div>
            </div>
          `).join("") || `<div class="muted">choice 없음</div>`}
          <button id="addEventChoiceBtn">choice 추가</button>
        </div>
      </div>
      <div class="preset-field">
        <label>Step effects</label>
        <div class="preset-stack">
          ${(selectedEventStepDef.effects || []).map((effect, index) => `
            <div class="preset-stack">
              <div class="preset-toolbar">
                <select data-event-effect-kind="${index}">
                  ${eventEffectTypes.map((kind) => `<option value="${kind}" ${effect.kind === kind ? "selected" : ""}>${kind}</option>`).join("")}
                </select>
                <button data-remove-event-effect="${index}">삭제</button>
              </div>
              ${renderEventEffectFields(effect, index, "event-effect")}
            </div>
          `).join("") || `<div class="muted">effect 없음</div>`}
          <button id="addEventEffectBtn">effect 추가</button>
        </div>
      </div>
    ` : `
      <div class="preset-toolbar">
        <button id="addEventStepBtn">step 추가</button>
      </div>
      <div class="muted">step graph가 비어 있다. 버튼으로 첫 step을 추가한다.</div>
    `}
    <div class="preset-toolbar">
      <button id="promoteEventEffectsToGraphBtn">기본 effects를 step graph로 승격</button>
    </div>
    <div class="muted">지원 trigger ${allowedInteractionsForPlacementKind(eventPlacementKind).join(", ")} · 지원 effect ${eventEffectTypes.join(", ")}</div>
    <div class="muted">연결 placement ${linkedPlacements.length}개 · 관련 검증 이슈 ${linkedIssues.length}개 · steps ${(eventDef.steps || []).length}개</div>
  `;
}

export function renderEditorEventExportArchiveBody(deps = {}) {
  const {
    eventExportArchiveQuery = "",
    filteredEventExportHistory = [],
    eventExportHistory = [],
    filteredEventExportArchive = [],
    eventExportArchive = [],
    eventExportHistoryLine = () => "",
    eventExportArchiveLine = () => "",
    eventExportArchiveBatchCompare = null,
    eventExportArchiveBatchCompareExport = null,
    eventExportArchiveBatchShareLabel = "",
    eventExportArchiveBatchShareExport = null,
    eventExportArchiveBatchShareLink = "",
    eventExportArchiveBatchShareLinkDraft = "",
    selectedEventExportArchiveId = "",
    selectedEventExportArchiveEntry = null,
    selectedEventExportArchiveBundleRows = [],
    selectedEventExportArchiveBundleRowId = "",
    selectedEventExportArchiveBundleRowsForRestore = [],
    effectiveEventExportArchiveBundleRowIds = [],
    eventExportArchiveFieldOptions = [],
    eventExportArchiveStepOptions = [],
    eventExportArchiveStepPartOptions = [],
    eventExportArchiveStepItemOptions = [],
    effectiveEventExportArchiveFieldKeys = [],
    effectiveEventExportArchiveStepIds = [],
    effectiveEventExportArchiveStepPartKeys = [],
    effectiveEventExportArchiveStepItemKeys = [],
    eventExportArchiveRestoreBadgeLookup = {
      fieldBadges: {},
      stepBadges: {},
      stepPartBadges: {},
      stepItemBadges: {},
    },
    selectedEventExportArchiveCompareDiff = null,
    selectedEventExportArchiveTargetEventId = "",
    selectedEventExportArchiveRollbackPlan = null,
    selectedEventExportArchiveCurrentTargetJson = "",
    selectedEventExportArchiveRestoreTargetJson = "",
    selectedEventExportArchiveRollbackDiffText = "",
    renderDiffBadgeHtml = () => "",
    buildDiffBadgeSpec = () => null,
    buildDiffCountScaleLabel = () => "",
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  return `
    <div class="preset-field">
      <label for="eventExportArchiveQueryInput">Export archive search</label>
      <input id="eventExportArchiveQueryInput" value="${escapeHtml(eventExportArchiveQuery)}" placeholder="kind, label, targetId 검색" />
      <div class="muted">history ${filteredEventExportHistory.length}/${eventExportHistory.length} · archive ${filteredEventExportArchive.length}/${eventExportArchive.length}</div>
    </div>
    <div class="preset-field">
      <label>Recent export history</label>
      <div class="validation-report">
        ${filteredEventExportHistory.length
          ? filteredEventExportHistory.map((entry) => `<div class="validation-line is-info"><strong>${escapeHtml(entry.kind || "export")}</strong> ${escapeHtml(eventExportHistoryLine(entry))}</div>`).join("")
          : `<div class="validation-line is-info"><strong>info</strong> 검색 결과 history 없음</div>`}
      </div>
    </div>
    <div class="preset-field">
      <label>Persistent export archive</label>
      <div class="validation-report">
        ${filteredEventExportArchive.length
          ? filteredEventExportArchive.slice(0, 8).map((entry) => `<div class="validation-line is-info"><strong>${escapeHtml(entry.kind || "archive")}</strong> ${escapeHtml(eventExportArchiveLine(entry))}</div>`).join("")
          : `<div class="validation-line is-info"><strong>info</strong> 검색 결과 archive 없음</div>`}
      </div>
      ${eventExportArchiveBatchCompare ? `
        <div class="validation-report">
          <div class="validation-line is-info"><strong>batch</strong> total ${eventExportArchiveBatchCompare.totalCount} · kind ${Object.keys(eventExportArchiveBatchCompare.kindCounts).length} · target ${Object.keys(eventExportArchiveBatchCompare.targetCounts).length}</div>
          ${Object.entries(eventExportArchiveBatchCompare.kindCounts).map(([kind, count]) => `<div class="validation-line is-info"><strong>${escapeHtml(kind)}</strong> ${count}개</div>`).join("")}
          ${eventExportArchiveBatchCompare.targetComparisons.slice(0, 6).map((row) => `
            <div class="validation-line is-info">
              <strong>${escapeHtml(row.targetId)}</strong>
              ${row.count}개
              ${row.diff ? `${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(Math.max(Math.abs(row.diff.stepDelta), Math.abs(row.diff.branchDelta), Math.abs(row.diff.choiceDelta)))))}
              step ${row.diff.stepDelta >= 0 ? "+" : ""}${row.diff.stepDelta} · branch ${row.diff.branchDelta >= 0 ? "+" : ""}${row.diff.branchDelta} · choice ${row.diff.choiceDelta >= 0 ? "+" : ""}${row.diff.choiceDelta}` : "· single snapshot"}
            </div>
          `).join("")}
        </div>
        <label for="eventExportArchiveBatchCompareJson">Batch diff export</label>
        <div class="preset-toolbar">
          <button id="copyEventExportArchiveBatchCompareBtn" ${eventExportArchiveBatchCompareExport ? "" : "disabled"}>batch diff 복사</button>
          <button id="downloadEventExportArchiveBatchCompareBtn" ${eventExportArchiveBatchCompareExport ? "" : "disabled"}>batch diff 다운로드</button>
          <button id="applyEventExportArchiveBatchCompareBtn" ${eventExportArchiveBatchCompare?.targetComparisons?.length ? "" : "disabled"}>batch 적용</button>
        </div>
        <textarea id="eventExportArchiveBatchCompareJson" rows="8" spellcheck="false" readonly>${escapeHtml(JSON.stringify(eventExportArchiveBatchCompareExport || {}, null, 2))}</textarea>
        <label for="eventExportArchiveBatchShareLabelInput">Batch share label</label>
        <input id="eventExportArchiveBatchShareLabelInput" value="${escapeHtml(eventExportArchiveBatchShareLabel)}" placeholder="share label" />
        <div class="preset-toolbar">
          <button id="saveEventExportArchiveBatchShareBtn" ${eventExportArchiveBatchShareExport ? "" : "disabled"}>batch share 저장</button>
        </div>
        <label for="eventExportArchiveBatchShareJson">Batch share preview</label>
        <textarea id="eventExportArchiveBatchShareJson" rows="8" spellcheck="false" readonly>${escapeHtml(JSON.stringify(eventExportArchiveBatchShareExport || {}, null, 2))}</textarea>
        <label for="eventExportArchiveBatchShareLinkInput">Batch share link</label>
        <div class="preset-toolbar">
          <button id="copyEventExportArchiveBatchShareLinkBtn" ${eventExportArchiveBatchShareLink ? "" : "disabled"}>share link 복사</button>
          <button id="importEventExportArchiveBatchShareLinkBtn" ${eventExportArchiveBatchShareLinkDraft.trim() ? "" : "disabled"}>share link 불러오기</button>
        </div>
        <textarea id="eventExportArchiveBatchShareLinkInput" rows="4" spellcheck="false" placeholder="#eventBatchShare=..." >${escapeHtml(eventExportArchiveBatchShareLinkDraft || eventExportArchiveBatchShareLink || "")}</textarea>
      ` : ""}
      ${filteredEventExportArchive.length ? `
        <div class="preset-stack">
          <label for="eventExportArchiveEntrySelect">Export archive restore target</label>
          <select id="eventExportArchiveEntrySelect">
            ${filteredEventExportArchive.map((entry) => `<option value="${entry.id}" ${entry.id === selectedEventExportArchiveId ? "selected" : ""}>${escapeHtml(eventExportArchiveLine(entry))}</option>`).join("")}
          </select>
          ${selectedEventExportArchiveEntry?.kind === "bundle" && selectedEventExportArchiveBundleRows.length ? `
            <label for="eventExportArchiveBundleRowSelect">Bundle row</label>
            <select id="eventExportArchiveBundleRowSelect">
              ${selectedEventExportArchiveBundleRows.map((row) => `<option value="${row.eventId}" ${row.eventId === selectedEventExportArchiveBundleRowId ? "selected" : ""}>${escapeHtml(row.eventId || "(event)")} · step ${Number(row.summaryDiff?.stepCount || row.compact?.summary?.stepCount || 0)}</option>`).join("")}
            </select>
            <label>Multi-select restore rows</label>
            <div class="preset-toolbar">
              <button type="button" data-event-export-select-all="bundle-rows">전체 선택</button>
              <button type="button" data-event-export-clear-all="bundle-rows">전체 해제</button>
            </div>
            <div class="validation-report">
              <div class="validation-line is-info"><strong>selected</strong> ${selectedEventExportArchiveBundleRowsForRestore.length}/${selectedEventExportArchiveBundleRows.length} row</div>
              ${selectedEventExportArchiveBundleRows.map((row) => `
                <label class="validation-line is-info">
                  <input type="checkbox" data-event-export-bundle-row-id="${escapeHtml(row.eventId || "")}" ${effectiveEventExportArchiveBundleRowIds.includes(row.eventId || "") ? "checked" : ""} />
                  <strong>${escapeHtml(row.eventId || "(event)")}</strong>
                  step ${Number(row.summaryDiff?.stepCount || row.compact?.summary?.stepCount || 0)}
                </label>
              `).join("")}
            </div>
          ` : ""}
          <div class="preset-toolbar">
            <button id="restoreEventExportArchiveBtn" ${selectedEventExportArchiveEntry && (selectedEventExportArchiveEntry.kind !== "bundle" || selectedEventExportArchiveBundleRowsForRestore.length) ? "" : "disabled"}>archive 복원</button>
            <button id="applyEventExportArchiveShareBtn" ${selectedEventExportArchiveEntry?.kind === "archive_batch_share" ? "" : "disabled"}>share 적용</button>
            <button id="deleteEventExportArchiveBtn" ${selectedEventExportArchiveEntry ? "" : "disabled"}>archive 삭제</button>
          </div>
          ${eventExportArchiveFieldOptions.length || eventExportArchiveStepOptions.length ? `
            <div class="preset-stack">
              ${eventExportArchiveFieldOptions.length ? `
                <label>Partial restore fields</label>
                <div class="preset-toolbar">
                  <button type="button" data-event-export-select-all="fields">전체 선택</button>
                  <button type="button" data-event-export-clear-all="fields">전체 해제</button>
                </div>
                <div class="validation-report">
                  ${eventExportArchiveFieldOptions.map((key) => `
                    <label class="validation-line is-info">
                      <input type="checkbox" data-event-export-field-key="${escapeHtml(key)}" ${effectiveEventExportArchiveFieldKeys.includes(key) ? "checked" : ""} />
                      <strong>${escapeHtml(key)}</strong>${renderDiffBadgeHtml(eventExportArchiveRestoreBadgeLookup.fieldBadges[key])}
                    </label>
                  `).join("")}
                </div>
              ` : ""}
              ${eventExportArchiveStepOptions.length ? `
                <label>Partial restore steps</label>
                <div class="preset-toolbar">
                  <button type="button" data-event-export-select-all="steps">전체 선택</button>
                  <button type="button" data-event-export-clear-all="steps">전체 해제</button>
                </div>
                <div class="validation-report">
                  ${eventExportArchiveStepOptions.map((id) => `
                    <label class="validation-line is-info">
                      <input type="checkbox" data-event-export-step-id="${escapeHtml(id)}" ${effectiveEventExportArchiveStepIds.includes(id) ? "checked" : ""} />
                      <strong>${escapeHtml(id)}</strong>${renderDiffBadgeHtml(eventExportArchiveRestoreBadgeLookup.stepBadges[id])}
                    </label>
                  `).join("")}
                </div>
              ` : ""}
              ${eventExportArchiveStepPartOptions.length ? `
                <label>Granular step parts</label>
                <div class="preset-toolbar">
                  <button type="button" data-event-export-select-all="step-parts">전체 선택</button>
                  <button type="button" data-event-export-clear-all="step-parts">전체 해제</button>
                </div>
                <div class="validation-report">
                  ${eventExportArchiveStepPartOptions.map((key) => `
                    <label class="validation-line is-info">
                      <input type="checkbox" data-event-export-step-part-key="${escapeHtml(key)}" ${effectiveEventExportArchiveStepPartKeys.includes(key) ? "checked" : ""} />
                      <strong>${escapeHtml(key)}</strong>${renderDiffBadgeHtml(eventExportArchiveRestoreBadgeLookup.stepPartBadges[key])}
                    </label>
                  `).join("")}
                </div>
              ` : ""}
              ${eventExportArchiveStepItemOptions.length ? `
                <label>Item level restore</label>
                <div class="preset-toolbar">
                  <button type="button" data-event-export-select-all="step-items">전체 선택</button>
                  <button type="button" data-event-export-clear-all="step-items">전체 해제</button>
                </div>
                <div class="validation-report">
                  ${eventExportArchiveStepItemOptions.map((key) => `
                    <label class="validation-line is-info">
                      <input type="checkbox" data-event-export-step-item-key="${escapeHtml(key)}" ${effectiveEventExportArchiveStepItemKeys.includes(key) ? "checked" : ""} />
                      <strong>${escapeHtml(key)}</strong>${renderDiffBadgeHtml(eventExportArchiveRestoreBadgeLookup.stepItemBadges[key])}
                    </label>
                  `).join("")}
                </div>
              ` : ""}
            </div>
          ` : ""}
          <div class="validation-report">
            ${selectedEventExportArchiveCompareDiff
              ? `<div class="validation-line is-info"><strong>rollback delta</strong> step ${selectedEventExportArchiveCompareDiff.stepDelta >= 0 ? "+" : ""}${selectedEventExportArchiveCompareDiff.stepDelta} · branch ${selectedEventExportArchiveCompareDiff.branchDelta >= 0 ? "+" : ""}${selectedEventExportArchiveCompareDiff.branchDelta} · choice ${selectedEventExportArchiveCompareDiff.choiceDelta >= 0 ? "+" : ""}${selectedEventExportArchiveCompareDiff.choiceDelta}</div>
                 <div class="validation-line is-muted"><strong>target</strong> ${escapeHtml(selectedEventExportArchiveTargetEventId || "(none)")}</div>`
              : `<div class="validation-line is-info"><strong>info</strong> 비교 가능한 current target이 없거나 archive payload가 비어 있다.</div>`}
          </div>
          <div class="validation-report">
            ${selectedEventExportArchiveRollbackPlan
              ? `
                <div class="validation-line is-info"><strong>plan</strong> field ${selectedEventExportArchiveRollbackPlan.fieldChanges.length} · add ${selectedEventExportArchiveRollbackPlan.addedSteps.length} · remove ${selectedEventExportArchiveRollbackPlan.removedSteps.length} · update ${selectedEventExportArchiveRollbackPlan.updatedSteps.length}</div>
                ${selectedEventExportArchiveRollbackPlan.lines.slice(0, 8).map((entry) => `<div class="validation-line is-${entry.severity}">${escapeHtml(entry.text)}</div>`).join("") || `<div class="validation-line is-info"><strong>ok</strong> structured rollback change 없음</div>`}
                ${selectedEventExportArchiveRollbackPlan.lines.length > 8 ? `<div class="validation-line is-muted"><strong>more</strong> ${selectedEventExportArchiveRollbackPlan.lines.length - 8}개 계획 항목이 더 있다.</div>` : ""}
              `
              : `<div class="validation-line is-info"><strong>info</strong> structured rollback plan을 계산할 current/archive target이 없다.</div>`}
          </div>
          <label for="eventExportArchiveCurrentTargetJson">Current target preview</label>
          <textarea id="eventExportArchiveCurrentTargetJson" rows="8" spellcheck="false" readonly>${escapeHtml(selectedEventExportArchiveCurrentTargetJson || "{}")}</textarea>
          <label for="eventExportArchivePreviewJson">Archive payload preview</label>
          <textarea id="eventExportArchivePreviewJson" rows="8" spellcheck="false" readonly>${escapeHtml(selectedEventExportArchiveRestoreTargetJson || "{}")}</textarea>
          <label for="eventExportArchiveRollbackDiffJson">Rollback diff</label>
          <textarea id="eventExportArchiveRollbackDiffJson" rows="10" spellcheck="false" readonly>${escapeHtml(selectedEventExportArchiveRollbackDiffText || "비교 가능한 current/archive payload가 없다.")}</textarea>
        </div>
      ` : ""}
    </div>
  `;
}

export function renderEditorEventObjectPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Event Object Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorMainWorkspace(deps = {}) {
  const {
    state,
    cursorCell = null,
    currentMapSeed = () => null,
    defaultFloorTextureId = "",
    defaultCeilingTextureId = "",
    defaultWallTextureId = "",
    toolPanelsMarkup = "",
    presetLibraryMarkup = "",
    eventObjectPanelDeps = {},
    buildEditorEventPanelBody = () => "",
    placementOverridePanelMarkup = "",
    classProgressionPanelMarkup = "",
    questPanelMarkup = "",
    monsterPanelMarkup = "",
    skillPanelMarkup = "",
    itemPanelMarkup = "",
    vendorPanelMarkup = "",
    lootPanelMarkup = "",
    affixPanelMarkup = "",
    sampleItemPanelMarkup = "",
    npcProgressionHooksPanelMarkup = "",
    npcQuestEditorPanelMarkup = "",
    npcServicePanelMarkup = "",
    npcPlacementPanelMarkup = "",
    presetStudioPanelMarkup = "",
  } = deps;
  const {
    eventTool = "",
    eventDefId = "",
    eventEditorToPlacementKind = {},
    bodyDeps = {},
  } = eventObjectPanelDeps;
  const workspaceMode = state.editorWorkspaceMode === "generator_workbench" ? "generator_workbench" : "legacy_cell_editor";
  const editorContentTabs = [
    { id: "map", label: "맵", subtitle: "격자, 브러시, 프리셋" },
    { id: "monster", label: "몬스터", subtitle: "몬스터/전투 정의" },
    { id: "skill", label: "스킬", subtitle: "스킬/주사위 정의" },
    { id: "item", label: "아이템", subtitle: "아이템/상점/루트" },
    { id: "event", label: "이벤트", subtitle: "이벤트/퀘스트" },
    { id: "npc", label: "NPC", subtitle: "NPC/서비스/배치" },
  ];
  const activeEditorContentTab = editorContentTabs.some((tab) => tab.id === state.editorContentTab) ? state.editorContentTab : "map";
  const editorContentTabButton = (tab) => `
    <button
      type="button"
      role="tab"
      data-editor-content-tab="${tab.id}"
      aria-selected="${activeEditorContentTab === tab.id ? "true" : "false"}"
      class="${activeEditorContentTab === tab.id ? "active" : ""}"
    >
      <span>${tab.label}</span>
      <small>${tab.subtitle}</small>
    </button>
  `;
  const editorContentTabPanel = (tabId, markup) => `
    <section
      class="editor-tab-panel ${activeEditorContentTab === tabId ? "is-active" : ""}"
      role="tabpanel"
      data-editor-content-tab-panel="${tabId}"
      ${activeEditorContentTab === tabId ? "" : "hidden"}
    >
      ${markup}
    </section>
  `;
  const workbenchPreview = buildWorkbenchPreview(state);
  const workbenchBatchSummary = state.editor.workbenchBatchSummary && state.editor.workbenchBatchSummary.profileId === workbenchPreview.profile.profileId
    ? state.editor.workbenchBatchSummary
    : null;
  const workbenchProfileOverrides = state.editor.workbenchProfileOverrides && typeof state.editor.workbenchProfileOverrides === "object"
    ? state.editor.workbenchProfileOverrides
    : {};
  const selectedWorkbenchProfileOverride = workbenchProfileOverrides[workbenchPreview.profile.profileId] || null;
  const workbenchChunkOverrides = state.editor.workbenchChunkOverrides && typeof state.editor.workbenchChunkOverrides === "object"
    ? state.editor.workbenchChunkOverrides
    : {};
  const selectedWorkbenchChunk = workbenchPreview.chunkCatalog.find((entry) => entry.id === state.editor.selectedWorkbenchChunkId)
    || workbenchPreview.chunkCatalog[0]
    || null;
  const selectedWorkbenchChunkOverride = selectedWorkbenchChunk ? workbenchChunkOverrides[selectedWorkbenchChunk.id] || null : null;
  const selectedWorkbenchChunkAnchor = selectedWorkbenchChunk?.anchors?.[0] || null;
  const workbenchCompareSnapshots = Array.isArray(state.editor.workbenchCompareSnapshots)
    ? state.editor.workbenchCompareSnapshots
    : [];
  const selectedWorkbenchCompareSnapshot = workbenchCompareSnapshots.find((entry) => entry.id === state.editor.selectedWorkbenchCompareSnapshotId) || workbenchCompareSnapshots[0] || null;
  const workspaceSwitcherMarkup = `
    <div class="preset-panel">
      <div class="preset-header"><h3>Workspace Mode</h3><span class="preset-subtitle">${workspaceMode === "generator_workbench" ? "Generator Workbench" : "Legacy Cell Editor"}</span></div>
      <div class="preset-toolbar">
        <button id="openLegacyCellEditorBtn" ${workspaceMode === "legacy_cell_editor" ? "disabled" : ""}>Legacy Cell Editor</button>
        <button id="openGeneratorWorkbenchBtn" ${workspaceMode === "generator_workbench" ? "disabled" : ""}>Generator Workbench</button>
      </div>
      <div class="muted">M0 boundary: 기존 cell 결과 편집 경로와 새 generator workbench 경로를 session state에서 분리한다.</div>
    </div>
  `;

  if (workspaceMode === "generator_workbench") {
    return `
      <div class="editor-wrap preset-shell">
        ${renderEditorWorkspaceShell({
          state,
          cursorCell,
          currentMapSeed,
          defaultFloorTextureId,
          defaultCeilingTextureId,
          defaultWallTextureId,
        })}
        <div class="editor-tools preset-stack">
          <h2>Generator Workbench</h2>
          <p class="muted">profile, graph, socket, anchor 중심 경로다. preview를 만들고 legacy cell editor나 compiledMap test play로 넘긴다.</p>
          ${workspaceSwitcherMarkup}
          <div class="preset-panel">
            <div class="preset-header"><h3>Generator Controls</h3><span class="preset-subtitle">profile + seed + batch</span></div>
            <div class="preset-stack">
              <label>Profile</label>
              <select id="workbenchProfileSelect">
                ${MAP_PROFILE_DEFINITIONS.map((profile) => `<option value="${profile.profileId}" ${profile.profileId === workbenchPreview.profile.profileId ? "selected" : ""}>F${profile.floor} · ${profile.profileId}</option>`).join("")}
              </select>
              <label>Algorithm</label>
              <select id="workbenchAlgorithmSelect">
                <option value="room_grid_chunks" ${workbenchPreview.algorithm === "room_grid_chunks" ? "selected" : ""}>room_grid_chunks</option>
                <option value="block_modules_and_connectors" ${workbenchPreview.algorithm === "block_modules_and_connectors" ? "selected" : ""}>block_modules_and_connectors</option>
              </select>
              <div class="preset-toolbar">
                <label>Seed <input id="workbenchSeedInput" type="number" value="${workbenchPreview.seed}" /></label>
                <button id="randomWorkbenchSeedBtn">Random Seed</button>
                <button id="refreshWorkbenchPreviewBtn">Preview Refresh</button>
              </div>
              <div class="preset-toolbar">
                <label>Batch <input id="workbenchBatchCountInput" type="number" min="1" max="50" value="${Math.max(1, Number(state.editor.workbenchBatchCount || 8))}" /></label>
                <button id="analyzeWorkbenchBatchBtn">Batch Analyze</button>
              </div>
              <div class="preset-toolbar">
                <label>Snapshot <input id="workbenchCompareSnapshotLabelInput" type="text" value="${String(state.editor.workbenchCompareSnapshotLabel || "").replaceAll("\"", "&quot;")}" placeholder="profile · seed note" /></label>
                <button id="saveWorkbenchCompareSnapshotBtn">Compare Snapshot 저장</button>
              </div>
              <div class="preset-toolbar">
                <button id="applyWorkbenchMapBtn">Legacy Editor로 적용</button>
                <button id="applyWorkbenchMapAndTestBtn">적용 후 Test Play</button>
              </div>
              <div class="preset-toolbar">
                <button id="saveWorkbenchProjectBtn">프로젝트 저장</button>
                <button id="loadWorkbenchProjectBtn">프로젝트 불러오기</button>
              </div>
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Profile Editor</h3><span class="preset-subtitle">${workbenchPreview.profile.profileId}${selectedWorkbenchProfileOverride ? " · override" : ""}</span></div>
            <div class="preset-stack">
              <div class="preset-toolbar">
                <button id="resetWorkbenchProfileOverrideBtn" ${selectedWorkbenchProfileOverride ? "" : "disabled"}>선택 profile override 초기화</button>
                <button id="clearWorkbenchProfileOverridesBtn" ${Object.keys(workbenchProfileOverrides).length ? "" : "disabled"}>전체 profile override 비우기</button>
              </div>
              <div class="muted">theme ${workbenchPreview.profile.theme} · algorithm ${workbenchPreview.profile.algorithm || workbenchPreview.algorithm} · locked key ${workbenchPreview.profile.lockedDoorKeyId || "-"}</div>
              <div class="preset-toolbar">
                <label>Target Rooms <input id="workbenchTargetRoomCountInput" type="number" min="5" value="${Number(workbenchPreview.profile.targetModuleCount || 8)}" /></label>
                <label>Merge <input id="workbenchMergeChanceInput" type="number" min="0" max="1000" value="${Number(workbenchPreview.profile.mergeChancePer1000 || 0)}" /></label>
                <label>Side Branch <input id="workbenchSideBranchCountInput" type="number" min="0" value="${Number(workbenchPreview.profile.sideBranchCount || 0)}" /></label>
              </div>
              <div class="preset-toolbar">
                <label>Room W <input id="workbenchRoomWidthInput" type="number" min="3" value="${Number(workbenchPreview.profile.gridRoomSize?.width || 7)}" /></label>
                <label>Room H <input id="workbenchRoomHeightInput" type="number" min="3" value="${Number(workbenchPreview.profile.gridRoomSize?.height || 7)}" /></label>
              </div>
              <div class="preset-toolbar">
                <label>Critical Min <input id="workbenchCriticalPathMinInput" type="number" min="2" value="${Number(workbenchPreview.profile.criticalPath?.min || 4)}" /></label>
                <label>Critical Max <input id="workbenchCriticalPathMaxInput" type="number" min="2" value="${Number(workbenchPreview.profile.criticalPath?.max || 4)}" /></label>
              </div>
              <div class="preset-toolbar">
                <label>Loop Min <input id="workbenchLoopCountMinInput" type="number" min="0" value="${Number(workbenchPreview.profile.loopCount?.min || 0)}" /></label>
                <label>Loop Max <input id="workbenchLoopCountMaxInput" type="number" min="0" value="${Number(workbenchPreview.profile.loopCount?.max || 0)}" /></label>
              </div>
              <label>Required Anchors</label>
              <div class="preset-toolbar">
                ${WORKBENCH_REQUIRED_ANCHOR_OPTIONS.map((anchor) => `
                  <label><input
                    type="checkbox"
                    data-workbench-required-anchor="${anchor}"
                    ${(workbenchPreview.profile.requiredAnchors || []).includes(anchor) ? "checked" : ""}
                  /> ${anchor}</label>
                `).join("")}
              </div>
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Workbench Preview</h3><span class="preset-subtitle">${workbenchPreview.profile.profileId}</span></div>
            <div class="preset-stack">
              <div class="muted">floor ${workbenchPreview.floor} · algorithm ${workbenchPreview.algorithm}</div>
              <div class="muted">seed ${workbenchPreview.seed} · room target ${workbenchPreview.profile.targetModuleCount}</div>
              <div class="muted">room graph ${workbenchPreview.summary.roomCount} rooms · critical path ${workbenchPreview.summary.criticalPathLength} · merge ${workbenchPreview.summary.mergeEdgeCount}</div>
              <div class="muted">graph issues error ${workbenchPreview.summary.errorCount} · warning ${workbenchPreview.summary.warningCount}</div>
              <div class="muted">socket assembly issue ${workbenchPreview.socketIssues.length}</div>
              <div class="muted">quality issue ${workbenchPreview.generationIssues.length} · variant groups ${workbenchPreview.selectedMapSummary?.variantGroupCounts.map((entry) => `${entry.variantGroup}:${entry.count}`).join(", ") || "-"}</div>
              <div class="muted">required anchors ${(workbenchPreview.profile.requiredAnchors || []).join(", ") || "-"}</div>
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Profile Contract</h3><span class="preset-subtitle">${workbenchPreview.profile.theme} · room ${workbenchPreview.profile.gridRoomSize?.width || 0}x${workbenchPreview.profile.gridRoomSize?.height || 0}</span></div>
            <div class="preset-stack">
              <div class="muted">target rooms ${workbenchPreview.profileContract.targetRoomCount} · actual ${workbenchPreview.profileContract.actualRoomCount}</div>
              <div class="muted">critical path ${workbenchPreview.profileContract.criticalPathMin}-${workbenchPreview.profileContract.criticalPathMax} · actual ${workbenchPreview.profileContract.actualCriticalPath}</div>
              <div class="muted">merge ${workbenchPreview.profileContract.mergeMin}-${workbenchPreview.profileContract.mergeMax} · actual ${workbenchPreview.profileContract.actualMergeCount}</div>
              <div class="muted">side branch target ${workbenchPreview.profileContract.sideBranchTarget} · actual root ${workbenchPreview.profileContract.actualSideBranchRootCount}</div>
              <div class="muted">required anchors ${workbenchPreview.profileContract.requiredAnchorStatus.map((entry) => `${entry.anchor}:${entry.matched ? "ok" : "missing"}`).join(", ") || "-"}</div>
              <div class="muted">contract issues ${workbenchPreview.profileContract.issueCodes.join(", ") || "none"}</div>
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Algorithm Compare</h3><span class="preset-subtitle">${workbenchPreview.alternateAlgorithm}</span></div>
            <div class="preset-stack">
              <div class="muted">selected ${workbenchPreview.selectedMapSummary?.algorithm || "-" } · modules ${workbenchPreview.selectedMapSummary?.moduleCount ?? "-"} · walkable ${workbenchPreview.selectedMapSummary?.walkableCount ?? "-"} · doors ${workbenchPreview.selectedMapSummary?.doorCount ?? "-"}</div>
              <div class="muted">selected socket issue ${workbenchPreview.selectedMapSummary?.socketAssemblyIssueCount ?? 0} · error ${workbenchPreview.selectedMapSummary?.socketAssemblyErrorCount ?? 0} · warning ${workbenchPreview.selectedMapSummary?.socketAssemblyWarningCount ?? 0}</div>
              <div class="muted">selected quality issue ${workbenchPreview.selectedMapSummary?.qualityIssueCount ?? 0} · warning ${workbenchPreview.selectedMapSummary?.qualityWarningCount ?? 0}</div>
              <div class="muted">alternate ${workbenchPreview.alternateMapSummary?.algorithm || "-"} · modules ${workbenchPreview.alternateMapSummary?.moduleCount ?? "-"} · walkable ${workbenchPreview.alternateMapSummary?.walkableCount ?? "-"} · doors ${workbenchPreview.alternateMapSummary?.doorCount ?? "-"}</div>
              <div class="muted">alternate socket issue ${workbenchPreview.alternateMapSummary?.socketAssemblyIssueCount ?? 0} · error ${workbenchPreview.alternateMapSummary?.socketAssemblyErrorCount ?? 0} · warning ${workbenchPreview.alternateMapSummary?.socketAssemblyWarningCount ?? 0}</div>
              <div class="muted">alternate quality issue ${workbenchPreview.alternateMapSummary?.qualityIssueCount ?? 0} · warning ${workbenchPreview.alternateMapSummary?.qualityWarningCount ?? 0}</div>
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Graph Diff</h3><span class="preset-subtitle">${workbenchPreview.baselineGraphMeta ? `${workbenchPreview.baselineGraphMeta.algorithm} · seed ${workbenchPreview.baselineGraphMeta.seed}` : "baseline 없음"}</span></div>
            <div class="preset-stack">
              ${workbenchPreview.graphCompare
                ? `
                  <div class="muted">selected roles ${formatCountMap(workbenchPreview.graphCompare.selected.roleCounts) || "-"}</div>
                  <div class="muted">baseline roles ${formatCountMap(workbenchPreview.graphCompare.alternate.roleCounts) || "-"}</div>
                  <div class="muted">selected edge types ${formatCountMap(workbenchPreview.graphCompare.selected.edgeTypeCounts) || "-"}</div>
                  <div class="muted">baseline edge types ${formatCountMap(workbenchPreview.graphCompare.alternate.edgeTypeCounts) || "-"}</div>
                  <div class="muted">selected socket degree ${formatCountMap(workbenchPreview.graphCompare.selected.socketDegreeCounts) || "-"}</div>
                  <div class="muted">baseline socket degree ${formatCountMap(workbenchPreview.graphCompare.alternate.socketDegreeCounts) || "-"}</div>
                  ${workbenchPreview.graphCompare.nodeDiffs.slice(0, 8).map((entry) => `<div class="muted">${entry.id} · ${entry.kind} · selected ${entry.selected} · baseline ${entry.alternate}</div>`).join("") || `<div class="muted">node diff 없음</div>`}
                `
                : `<div class="muted">현재 editor floor에 비교 가능한 baseline graph가 없다.</div>`}
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Cell Diff</h3><span class="preset-subtitle">${workbenchPreview.baselineGraphMeta ? `current floor baseline · seed ${workbenchPreview.baselineGraphMeta.seed}` : "baseline 없음"}</span></div>
            <div class="preset-stack">
              ${workbenchPreview.cellCompare
                ? `
                  <div class="muted">selected walkable ${workbenchPreview.cellCompare.selected.walkableCount} · baseline walkable ${workbenchPreview.cellCompare.alternate.walkableCount}</div>
                  <div class="muted">selected roles ${formatCountMap(workbenchPreview.cellCompare.selected.tileRoleCounts) || "-"}</div>
                  <div class="muted">baseline roles ${formatCountMap(workbenchPreview.cellCompare.alternate.tileRoleCounts) || "-"}</div>
                  <div class="muted">selected floor variants ${formatCountMap(workbenchPreview.cellCompare.selected.floorVariantCounts) || "-"}</div>
                  <div class="muted">baseline floor variants ${formatCountMap(workbenchPreview.cellCompare.alternate.floorVariantCounts) || "-"}</div>
                  <div class="muted">selected decor ${formatCountMap(workbenchPreview.cellCompare.selected.decorTagCounts) || "-"}</div>
                  <div class="muted">baseline decor ${formatCountMap(workbenchPreview.cellCompare.alternate.decorTagCounts) || "-"}</div>
                  <div class="muted">changed cells ${workbenchPreview.cellCompare.cellDiffs.length}</div>
                  ${workbenchPreview.cellCompare.cellDiffs.slice(0, 8).map((entry) => `<div class="muted">${entry.coord} · ${entry.kinds.join("/")} · selected ${entry.selected} · baseline ${entry.baseline}</div>`).join("") || `<div class="muted">cell diff 없음</div>`}
                `
                : `<div class="muted">현재 editor floor에 비교 가능한 baseline map이 없다.</div>`}
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Tile Resolver Preview</h3><span class="preset-subtitle">${workbenchPreview.selectedMapSummary?.algorithm || workbenchPreview.algorithm}</span></div>
            <div class="preset-stack">
              <div class="muted">tile roles ${formatCountMap(workbenchPreview.selectedVisualSummary?.tileRoleCounts || new Map()) || "-"}</div>
              <div class="muted">floor variants ${formatCountMap(workbenchPreview.selectedVisualSummary?.floorVariantCounts || new Map()) || "-"}</div>
              <div class="muted">wall variants ${formatCountMap(workbenchPreview.selectedVisualSummary?.wallVariantCounts || new Map()) || "-"}</div>
              <div class="muted">floor materials ${formatCountMap(workbenchPreview.selectedVisualSummary?.floorMaterialCounts || new Map()) || "-"}</div>
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Substitution/Decor Preview</h3><span class="preset-subtitle">${workbenchPreview.profile.theme}</span></div>
            <div class="preset-stack">
              <div class="muted">decor tags ${formatCountMap(workbenchPreview.selectedVisualSummary?.decorTagCounts || new Map()) || "-"}</div>
              <div class="muted">decor kinds ${formatCountMap(workbenchPreview.selectedVisualSummary?.decorKindCounts || new Map()) || "-"}</div>
              <div class="muted">visual decor count ${workbenchPreview.selectedVisualSummary?.decorCount ?? 0}</div>
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Validation</h3><span class="preset-subtitle">${workbenchPreview.summary.errorCount ? "blocked" : "preview ok"}</span></div>
            <div class="preset-stack">
              ${workbenchPreview.issues.length
                ? workbenchPreview.issues.map((issue) => `<div class="muted">${issue.severity} · ${issue.code}${issue.actual !== undefined ? ` · actual ${issue.actual}` : ""}${issue.expected !== undefined ? ` · expected ${issue.expected}` : ""}${issue.selectedChunkId ? ` · chunk ${issue.selectedChunkId}` : ""}</div>`).join("")
                : `<div class="muted">room graph preview issue 없음</div>`}
              ${workbenchPreview.generationIssues.length
                ? workbenchPreview.generationIssues.map((issue) => `<div class="muted">${issue.severity} · ${issue.code}${issue.variantGroup ? ` · group ${issue.variantGroup}` : ""}${issue.chunkId ? ` · chunk ${issue.chunkId}` : ""}${issue.count !== undefined ? ` · count ${issue.count}` : ""}</div>`).join("")
                : `<div class="muted">generation quality issue 없음</div>`}
            </div>
          </div>
        </div>
        <div class="editor-support preset-stack">
          <div class="preset-panel">
            <div class="preset-header"><h3>Graph Overlay</h3><span class="preset-subtitle">${workbenchPreview.graph.edges.length} edges</span></div>
            <div class="preset-stack">
              ${workbenchPreview.graph.nodes.map((node) => `<div class="muted">${node.id} · ${node.role} · ${node.x},${node.y} · sockets ${node.socketMask.join("/") || "-"} · chunk ${node.chunkId || "-"}</div>`).join("")}
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Chunk Catalog</h3><span class="preset-subtitle">${workbenchPreview.chunkCatalog.length} defs · theme ${workbenchPreview.profile.theme}</span></div>
            <div class="preset-stack">
              ${workbenchPreview.chunkCatalogSummary.map((entry) => `
                <div class="muted"><strong>${entry.id}</strong> · preset ${entry.presetId} · group ${entry.variantGroup}</div>
                <div class="muted">open ${entry.openSides.join(", ") || "-"} · doors ${entry.doorSockets.join(", ") || "-"} · roles ${entry.roleTags.join(", ") || "-"}</div>
                <div class="muted">anchors ${(entry.anchors || []).map((anchor) => `${anchor.id}:${anchor.kind}@${anchor.x},${anchor.y}`).join(" · ") || "-"}</div>
                <div class="muted">exact usage ${entry.exactCount} · fallback usage ${entry.fallbackCount} · nodes ${entry.matchedNodes.map((node) => `${node.id}:${node.role}`).join(", ") || "-"}</div>
              `).join("")}
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Chunk Editor</h3><span class="preset-subtitle">${selectedWorkbenchChunk ? `${selectedWorkbenchChunk.id}${selectedWorkbenchChunkOverride ? " · override" : ""}` : "chunk 없음"}</span></div>
            <div class="preset-stack">
              ${selectedWorkbenchChunk
                ? `
                  <select id="workbenchChunkSelect">
                    ${workbenchPreview.chunkCatalog.map((entry) => `<option value="${entry.id}" ${entry.id === selectedWorkbenchChunk.id ? "selected" : ""}>${entry.id} · ${entry.presetId}</option>`).join("")}
                  </select>
                  <div class="preset-toolbar">
                    <button id="resetWorkbenchChunkOverrideBtn" ${selectedWorkbenchChunkOverride ? "" : "disabled"}>선택 override 초기화</button>
                    <button id="clearWorkbenchChunkOverridesBtn" ${Object.keys(workbenchChunkOverrides).length ? "" : "disabled"}>전체 override 비우기</button>
                  </div>
                  <div class="muted">preset ${selectedWorkbenchChunk.presetId} · group ${selectedWorkbenchChunk.variantGroup} · anchor ${selectedWorkbenchChunkAnchor ? `${selectedWorkbenchChunkAnchor.id}:${selectedWorkbenchChunkAnchor.kind}@${selectedWorkbenchChunkAnchor.x},${selectedWorkbenchChunkAnchor.y}` : "-"}</div>
                  <label>Open Sides</label>
                  <div class="preset-toolbar">
                    ${WORKBENCH_SIDES.map((side) => `
                      <label><input
                        type="checkbox"
                        data-workbench-chunk-id="${selectedWorkbenchChunk.id}"
                        data-workbench-chunk-field="openSides"
                        data-workbench-chunk-side="${side}"
                        ${selectedWorkbenchChunk.openSides.includes(side) ? "checked" : ""}
                      /> ${side}</label>
                    `).join("")}
                  </div>
                  <label>Door Sockets</label>
                  <div class="preset-toolbar">
                    ${WORKBENCH_SIDES.map((side) => `
                      <label><input
                        type="checkbox"
                        data-workbench-chunk-id="${selectedWorkbenchChunk.id}"
                        data-workbench-chunk-field="doorSockets"
                        data-workbench-chunk-side="${side}"
                        ${selectedWorkbenchChunk.doorSockets.includes(side) ? "checked" : ""}
                      /> ${side}</label>
                    `).join("")}
                  </div>
                  <label>Role Tags</label>
                  <div class="preset-toolbar">
                    ${WORKBENCH_ROLE_TAGS.map((roleTag) => `
                      <label><input
                        type="checkbox"
                        data-workbench-chunk-id="${selectedWorkbenchChunk.id}"
                        data-workbench-chunk-role-tag="${roleTag}"
                        ${selectedWorkbenchChunk.roleTags.includes(roleTag) ? "checked" : ""}
                      /> ${roleTag}</label>
                    `).join("")}
                  </div>
                  <label>Primary Anchor</label>
                  <div class="preset-toolbar">
                    <label>Id <input id="workbenchChunkAnchorIdInput" type="text" value="${escapeWorkbenchHtml(selectedWorkbenchChunkAnchor?.id || "")}" /></label>
                    <label>Kind
                      <select id="workbenchChunkAnchorKindSelect">
                        ${WORKBENCH_ANCHOR_KINDS.map((kind) => `<option value="${kind}" ${selectedWorkbenchChunkAnchor?.kind === kind ? "selected" : ""}>${kind}</option>`).join("")}
                      </select>
                    </label>
                  </div>
                  <div class="preset-toolbar">
                    <label>X <input id="workbenchChunkAnchorXInput" type="number" min="0" value="${Number(selectedWorkbenchChunkAnchor?.x || 0)}" /></label>
                    <label>Y <input id="workbenchChunkAnchorYInput" type="number" min="0" value="${Number(selectedWorkbenchChunkAnchor?.y || 0)}" /></label>
                  </div>
                `
                : `<div class="muted">편집 가능한 chunk 정의가 없다.</div>`}
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Socket Inspector</h3><span class="preset-subtitle">${workbenchPreview.chunkCatalog.length} chunk defs</span></div>
            <div class="preset-stack">
              ${workbenchPreview.graph.nodes.map((node) => `<div class="muted">${node.id} · preset ${node.presetId || "-"} · sockets ${node.socketMask.join(", ") || "-"} · rotated ${(node.chunkMatch?.rotatedOpenSides || []).join(", ") || "-"} · anchors ${(node.anchors || []).map((anchor) => anchor.id).join(", ") || "-"} · ${node.chunkMatch?.exactSocketMatch ? "exact" : "fallback"}</div>`).join("")}
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Anchor Inspector</h3><span class="preset-subtitle">selected chunk anchors</span></div>
            <div class="preset-stack">
              ${workbenchPreview.graph.nodes.map((node) => `
                <div class="muted">${node.id} · ${node.chunkId || "-"}</div>
                ${(node.anchors || []).map((anchor) => `<div class="muted">${anchor.id} · ${anchor.kind} · ${anchor.x},${anchor.y}</div>`).join("") || `<div class="muted">anchor 없음</div>`}
              `).join("")}
            </div>
          </div>
          ${workbenchBatchSummary ? `
            <div class="preset-panel">
              <div class="preset-header"><h3>Batch Summary</h3><span class="preset-subtitle">${workbenchBatchSummary.count} seeds · ${workbenchBatchSummary.algorithm}</span></div>
              <div class="preset-stack">
                <div class="muted">base seed ${workbenchBatchSummary.baseSeed} · graph error ${workbenchBatchSummary.totalErrors} · graph warning ${workbenchBatchSummary.totalWarnings}</div>
                <div class="muted">socket issue total ${workbenchBatchSummary.totalSocketIssues} · zero-issue seeds ${workbenchBatchSummary.zeroSocketIssueSeeds}/${workbenchBatchSummary.count}</div>
                <div class="muted">used chunk defs ${workbenchBatchSummary.usedChunkIds.length} · unused defs ${workbenchBatchSummary.unusedChunkIds.length}</div>
                <div class="muted">used chunks ${workbenchBatchSummary.usedChunkIds.join(", ") || "-"}</div>
                <div class="muted">unused chunks ${workbenchBatchSummary.unusedChunkIds.join(", ") || "-"}</div>
                ${workbenchBatchSummary.socketDemandCounts.length
                  ? workbenchBatchSummary.socketDemandCounts.slice(0, 8).map((entry) => `<div class="muted">demand ${entry.usageCount} · ${entry.role || "-"} · sockets ${entry.socketMask || "-"} · chunk ${entry.chunkId || "-"} · ${entry.exactState}</div>`).join("")
                  : `<div class="muted">socket demand 없음</div>`}
                ${workbenchBatchSummary.fallbackPatternCounts.length
                  ? workbenchBatchSummary.fallbackPatternCounts.slice(0, 6).map((entry) => `<div class="muted">fallback ${entry.usageCount} · ${entry.code} · ${entry.role || "-"} · sockets ${entry.requiredSockets || "-"} · chunk ${entry.selectedChunkId || "-"}</div>`).join("")
                  : `<div class="muted">fallback pattern 없음</div>`}
                ${workbenchBatchSummary.qualityPatternCounts.length
                  ? workbenchBatchSummary.qualityPatternCounts.slice(0, 6).map((entry) => `<div class="muted">quality ${entry.usageCount} · ${entry.code} · group ${entry.variantGroup || "-"} · chunk ${entry.chunkId || "-"} · count ${entry.count || 0}</div>`).join("")
                  : `<div class="muted">quality warning 없음</div>`}
                ${workbenchBatchSummary.entries.map((entry) => `<div class="muted">seed ${entry.seed} · rooms ${entry.summary.roomCount} · critical path ${entry.summary.criticalPathLength} · graph error ${entry.summary.errorCount} · graph warning ${entry.summary.warningCount} · socket issue ${entry.mapSummary?.socketAssemblyIssueCount ?? 0} · walkable ${entry.mapSummary?.walkableCount ?? "-"} · decor ${entry.visualSummary?.decorCount ?? 0}</div>`).join("")}
              </div>
            </div>
          ` : ""}
          <div class="preset-panel">
            <div class="preset-header"><h3>Compare Snapshots</h3><span class="preset-subtitle">${workbenchCompareSnapshots.length} entries</span></div>
            <div class="preset-stack">
              <select id="workbenchCompareSnapshotSelect">
                <option value="">snapshot 선택</option>
                ${workbenchCompareSnapshots.map((entry) => `<option value="${entry.id}" ${selectedWorkbenchCompareSnapshot?.id === entry.id ? "selected" : ""}>${entry.label}</option>`).join("")}
              </select>
              <div class="preset-toolbar">
                <button id="restoreWorkbenchCompareSnapshotBtn" ${selectedWorkbenchCompareSnapshot ? "" : "disabled"}>Snapshot 복원</button>
                <button id="clearWorkbenchCompareSnapshotsBtn" ${workbenchCompareSnapshots.length ? "" : "disabled"}>목록 비우기</button>
              </div>
              ${selectedWorkbenchCompareSnapshot
                ? `
                  <div class="muted">${selectedWorkbenchCompareSnapshot.createdAt} · floor ${selectedWorkbenchCompareSnapshot.floor} · seed ${selectedWorkbenchCompareSnapshot.seed}</div>
                  <div class="muted">${selectedWorkbenchCompareSnapshot.profileId} · ${selectedWorkbenchCompareSnapshot.algorithm} · batch ${selectedWorkbenchCompareSnapshot.batchCount}</div>
                  <div class="muted">graph diff ${selectedWorkbenchCompareSnapshot.graphDiffCount} · cell diff ${selectedWorkbenchCompareSnapshot.cellDiffCount} · socket issue ${selectedWorkbenchCompareSnapshot.socketIssueCount} · validation issue ${selectedWorkbenchCompareSnapshot.validationIssueCount}</div>
                  <div class="muted">selected modules ${selectedWorkbenchCompareSnapshot.selectedMapSummary?.moduleCount ?? "-"} · walkable ${selectedWorkbenchCompareSnapshot.selectedMapSummary?.walkableCount ?? "-"} · doors ${selectedWorkbenchCompareSnapshot.selectedMapSummary?.doorCount ?? "-"}</div>
                  <div class="muted">alternate modules ${selectedWorkbenchCompareSnapshot.alternateMapSummary?.moduleCount ?? "-"} · walkable ${selectedWorkbenchCompareSnapshot.alternateMapSummary?.walkableCount ?? "-"} · doors ${selectedWorkbenchCompareSnapshot.alternateMapSummary?.doorCount ?? "-"}</div>
                `
                : `<div class="muted">저장된 compare snapshot이 없다.</div>`}
            </div>
          </div>
          <div class="preset-panel">
            <div class="preset-header"><h3>Legacy Handoff</h3><span class="preset-subtitle">manual override path</span></div>
            <div class="preset-stack">
              <div class="muted">Legacy Editor로 적용 버튼은 선택한 floor map을 실제 editor working copy로 교체한다.</div>
              <div class="muted">적용 후 Test Play는 compiledMap 경로 검증까지 바로 이어진다.</div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  return `
    <div class="editor-wrap preset-shell editor-tab-shell">
      ${renderEditorWorkspaceShell({
        state,
        cursorCell,
        currentMapSeed,
        defaultFloorTextureId,
        defaultCeilingTextureId,
        defaultWallTextureId,
      })}
      <div class="editor-tab-workspace preset-stack">
        <div class="preset-panel editor-tab-header-panel">
          <div class="preset-header">
            <div>
              <h2>Legacy Cell Editor</h2>
              <div class="muted">기능별 탭으로 저작 패널을 나눠 필요한 영역만 읽는다.</div>
            </div>
            <span class="preset-subtitle">${editorContentTabs.find((tab) => tab.id === activeEditorContentTab)?.label || "맵"}</span>
          </div>
          <div class="editor-tab-list" role="tablist" aria-label="Editor workspace tabs">
            ${editorContentTabs.map(editorContentTabButton).join("")}
          </div>
        </div>
        ${editorContentTabPanel("map", `
          <div class="editor-tools preset-stack">
            <h2>맵 편집기</h2>
            <p class="muted">Legacy cell editor 경로다. 도구를 고른 뒤 격자를 클릭한다. preset 도구는 선택한 블록을 찍는다.</p>
            ${workspaceSwitcherMarkup}
            <div class="actions">
              ${["wall", "floor", "door", "secret", "start", "stairs", "encounter", "npc", "eventTrigger", "trap", "shrine", "restSite", "camp", "preset", "texture", "cellTag", "battleBg", "room"].map((t) => `<button data-tool="${t}" class="${state.editorTool === t ? "active" : ""}">${t}</button>`).join("")}
            </div>
            ${toolPanelsMarkup}
            ${presetLibraryMarkup}
            ${placementOverridePanelMarkup}
            ${presetStudioPanelMarkup}
          </div>
        `)}
        ${editorContentTabPanel("monster", `
          <div class="editor-support preset-stack">
            ${monsterPanelMarkup}
          </div>
        `)}
        ${editorContentTabPanel("skill", `
          <div class="editor-support preset-stack">
            ${skillPanelMarkup}
          </div>
        `)}
        ${editorContentTabPanel("item", `
          <div class="editor-support preset-stack">
            ${itemPanelMarkup}
            ${vendorPanelMarkup}
            ${lootPanelMarkup}
            ${affixPanelMarkup}
            ${sampleItemPanelMarkup}
          </div>
        `)}
        ${editorContentTabPanel("event", `
          <div class="editor-support preset-stack">
            ${renderEditorEventObjectPanel({
              subtitle: `${eventEditorToPlacementKind[eventTool]} · ${eventDefId || "preset 없음"}`,
              bodyMarkup: buildEditorEventPanelBody(bodyDeps),
            })}
            ${classProgressionPanelMarkup}
            ${questPanelMarkup}
          </div>
        `)}
        ${editorContentTabPanel("npc", `
          <div class="editor-support preset-stack">
            ${npcProgressionHooksPanelMarkup}
            ${npcQuestEditorPanelMarkup}
            ${npcServicePanelMarkup}
            ${npcPlacementPanelMarkup}
          </div>
        `)}
      </div>
    </div>
  `;
}

export function renderEditorSampleItemPanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Sample Item Generator</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorPlacementOverridePanel(deps = {}) {
  const {
    subtitle = "",
    bodyMarkup = "",
  } = deps;
  return `
    <div class="preset-panel">
      <div class="preset-header"><h3>Placement Override Inspector</h3><span class="preset-subtitle">${subtitle}</span></div>
      ${bodyMarkup}
    </div>
  `;
}

export function renderEditorFrame(deps = {}) {
  const {
    renderEditorImpl = () => {},
    bindEditorFrame = () => {},
  } = deps;
  const result = renderEditorImpl();
  bindEditorFrame();
  return result;
}

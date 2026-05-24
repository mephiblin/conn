const DIRS = ["north", "east", "south", "west"];
const VEC = {
  north: { x: 0, y: -1 },
  east: { x: 1, y: 0 },
  south: { x: 0, y: 1 },
  west: { x: -1, y: 0 },
};

function mulberry32(seed) {
  let t = seed >>> 0;
  return () => {
    t += 0x6D2B79F5;
    let r = Math.imul(t ^ (t >>> 15), 1 | t);
    r ^= r + Math.imul(r ^ (r >>> 7), 61 | r);
    return ((r ^ (r >>> 14)) >>> 0) / 4294967296;
  };
}

function graphEdgeId(a, b) {
  return [a, b].sort().join("::");
}

function coordKey(x, y) {
  return `${x},${y}`;
}

function roleForMainPathIndex(index, pathLength) {
  if (index === 0) return "start";
  if (index === pathLength - 1) return "boss";
  if (index === Math.max(1, Math.floor((pathLength - 1) * 0.55))) return "key";
  if (index === Math.max(2, pathLength - 2)) return "locked_gate";
  return "combat";
}

function sideRole(index) {
  return index % 2 === 0 ? "side_reward" : "combat";
}

function availableNeighbors(point, occupied) {
  return DIRS
    .map((dir) => ({ dir, x: point.x + VEC[dir].x, y: point.y + VEC[dir].y }))
    .filter((entry) => !occupied.has(coordKey(entry.x, entry.y)));
}

function normalizeGraphCoordinates(nodes = []) {
  if (!nodes.length) return nodes;
  const minX = Math.min(...nodes.map((node) => node.x));
  const minY = Math.min(...nodes.map((node) => node.y));
  return nodes.map((node) => ({ ...node, x: node.x - minX, y: node.y - minY }));
}

function socketMaskForNode(nodeId, nodesById, adjacency) {
  const node = nodesById.get(nodeId);
  const neighbors = adjacency.get(nodeId) || [];
  return DIRS.filter((dir) => neighbors.some((neighborId) => {
    const neighbor = nodesById.get(neighborId);
    return neighbor && neighbor.x === node.x + VEC[dir].x && neighbor.y === node.y + VEC[dir].y;
  }));
}

export function buildRoomGraph(profile = {}, seed = 0) {
  const roomCount = Math.max(5, Number(profile.targetModuleCount || 8));
  const criticalPathMin = Math.max(2, Number(profile.criticalPath?.min || 4));
  const criticalPathMax = Math.max(criticalPathMin, Number(profile.criticalPath?.max || criticalPathMin));
  const sideBranchTarget = Math.max(0, Number(profile.sideBranchCount || 0));
  const mergeChancePer1000 = Math.max(0, Math.min(1000, Number(profile.mergeChancePer1000 || 0)));
  const rng = mulberry32((Number(seed) || 0) ^ 0x6d2b79f5);
  const desiredCriticalPath = Math.min(roomCount, criticalPathMin + Math.floor(rng() * Math.max(1, criticalPathMax - criticalPathMin + 1)));
  const nodes = [];
  const edges = [];
  const occupied = new Set();
  const edgeIds = new Set();
  const branchRoots = [];
  let nextId = 0;

  const addNode = (x, y, extra = {}) => {
    const node = {
      id: `room_${nextId}`,
      x,
      y,
      role: "empty",
      branchDepth: 0,
      pathIndex: null,
      ...extra,
    };
    nextId += 1;
    occupied.add(coordKey(x, y));
    nodes.push(node);
    return node;
  };

  const addEdge = (from, to, type = "main") => {
    const id = graphEdgeId(from, to);
    if (edgeIds.has(id)) return false;
    edgeIds.add(id);
    edges.push({ id: `edge_${edges.length}`, from, to, type });
    return true;
  };

  const start = addNode(0, 0, { role: "start", branchDepth: 0, pathIndex: 0 });
  let cursor = start;
  for (let index = 1; index < desiredCriticalPath; index += 1) {
    const options = availableNeighbors(cursor, occupied);
    const fallbackBase = nodes[Math.floor(rng() * nodes.length)];
    const fallbackOptions = availableNeighbors(fallbackBase, occupied);
    const pickPool = options.length ? options : fallbackOptions;
    if (!pickPool.length) break;
    const pick = pickPool[Math.floor(rng() * pickPool.length)];
    const parent = options.length ? cursor : fallbackBase;
    const node = addNode(pick.x, pick.y, {
      role: roleForMainPathIndex(index, desiredCriticalPath),
      branchDepth: 0,
      pathIndex: index,
    });
    addEdge(parent.id, node.id, "main");
    cursor = node;
  }

  while (nodes.length < roomCount) {
    const parent = nodes[Math.floor(rng() * nodes.length)];
    const options = availableNeighbors(parent, occupied);
    if (!options.length) {
      const fallback = nodes.find((candidate) => availableNeighbors(candidate, occupied).length);
      if (!fallback) break;
      const fallbackOptions = availableNeighbors(fallback, occupied);
      const pick = fallbackOptions[Math.floor(rng() * fallbackOptions.length)];
      const node = addNode(pick.x, pick.y, {
        role: branchRoots.length < sideBranchTarget ? sideRole(branchRoots.length) : "combat",
        branchDepth: (fallback.branchDepth || 0) + 1,
      });
      addEdge(fallback.id, node.id, "branch");
      if ((node.branchDepth || 0) === 1) branchRoots.push(node.id);
      continue;
    }
    const pick = options[Math.floor(rng() * options.length)];
    const node = addNode(pick.x, pick.y, {
      role: branchRoots.length < sideBranchTarget && (parent.branchDepth || 0) === 0 ? sideRole(branchRoots.length) : "combat",
      branchDepth: (parent.branchDepth || 0) + 1,
    });
    addEdge(parent.id, node.id, "branch");
    if ((node.branchDepth || 0) === 1) branchRoots.push(node.id);
  }

  const normalizedNodes = normalizeGraphCoordinates(nodes);
  const nodesById = new Map(normalizedNodes.map((node) => [node.id, node]));
  const adjacency = new Map(normalizedNodes.map((node) => [node.id, []]));
  edges.forEach((edge) => {
    adjacency.get(edge.from)?.push(edge.to);
    adjacency.get(edge.to)?.push(edge.from);
  });

  for (const node of normalizedNodes) {
    for (const dir of DIRS) {
      const nx = node.x + VEC[dir].x;
      const ny = node.y + VEC[dir].y;
      const neighbor = normalizedNodes.find((candidate) => candidate.x === nx && candidate.y === ny);
      if (!neighbor) continue;
      if (edgeIds.has(graphEdgeId(node.id, neighbor.id))) continue;
      if (rng() * 1000 > mergeChancePer1000) continue;
      if (addEdge(node.id, neighbor.id, "merge")) {
        adjacency.get(node.id)?.push(neighbor.id);
        adjacency.get(neighbor.id)?.push(node.id);
      }
    }
  }

  normalizedNodes.forEach((node) => {
    node.socketMask = socketMaskForNode(node.id, nodesById, adjacency);
  });

  const criticalPath = normalizedNodes
    .filter((node) => Number.isInteger(node.pathIndex))
    .sort((left, right) => left.pathIndex - right.pathIndex)
    .map((node) => node.id);
  const sideBranches = normalizedNodes.filter((node) => (node.branchDepth || 0) > 0).map((node) => node.id);
  const mergeEdges = edges.filter((edge) => edge.type === "merge").map((edge) => edge.id);
  const lockedEdge = edges.find((edge) => {
    const from = nodesById.get(edge.from);
    const to = nodesById.get(edge.to);
    if (from?.role === "locked_gate") return Number(to?.pathIndex ?? -1) > Number(from.pathIndex ?? -1);
    if (to?.role === "locked_gate") return Number(from?.pathIndex ?? -1) > Number(to.pathIndex ?? -1);
    return false;
  })?.id || edges.find((edge) => {
    const from = nodesById.get(edge.from);
    const to = nodesById.get(edge.to);
    return from?.role === "locked_gate" || to?.role === "locked_gate";
  })?.id || "";

  return {
    seed: Number(seed) || 0,
    profileId: String(profile.profileId || ""),
    roomCountTarget: roomCount,
    nodes: normalizedNodes,
    edges,
    criticalPath,
    sideBranches,
    mergeEdges,
    lockedEdge,
  };
}

export function validateRoomGraph(profile = {}, graph = {}) {
  const issues = [];
  const nodes = Array.isArray(graph.nodes) ? graph.nodes : [];
  const edges = Array.isArray(graph.edges) ? graph.edges : [];
  const adjacency = new Map(nodes.map((node) => [node.id, []]));
  edges.forEach((edge) => {
    adjacency.get(edge.from)?.push(edge.to);
    adjacency.get(edge.to)?.push(edge.from);
  });
  if (nodes.length) {
    const visited = new Set();
    const queue = [nodes[0].id];
    while (queue.length) {
      const next = queue.shift();
      if (visited.has(next)) continue;
      visited.add(next);
      (adjacency.get(next) || []).forEach((neighbor) => {
        if (!visited.has(neighbor)) queue.push(neighbor);
      });
    }
    if (visited.size !== nodes.length) issues.push({ severity: "error", code: "disconnected_graph", expected: nodes.length, actual: visited.size });
  }
  const roomCountTarget = Math.max(5, Number(profile.targetModuleCount || nodes.length));
  if (nodes.length !== roomCountTarget) issues.push({ severity: "error", code: "room_count_mismatch", expected: roomCountTarget, actual: nodes.length });
  const criticalPathLength = Array.isArray(graph.criticalPath) ? graph.criticalPath.length : 0;
  if (criticalPathLength < Number(profile.criticalPath?.min || 0) || criticalPathLength > Number(profile.criticalPath?.max || Infinity)) {
    issues.push({ severity: "error", code: "critical_path_out_of_range", actual: criticalPathLength, min: Number(profile.criticalPath?.min || 0), max: Number(profile.criticalPath?.max || 0) });
  }
  const mergeCount = edges.filter((edge) => edge.type === "merge").length;
  if (mergeCount < Number(profile.loopCount?.min || 0) || mergeCount > Number(profile.loopCount?.max || Infinity)) {
    issues.push({ severity: "warning", code: "loop_count_out_of_range", actual: mergeCount, min: Number(profile.loopCount?.min || 0), max: Number(profile.loopCount?.max || 0) });
  }
  const sideBranchCount = nodes.filter((node) => Number(node.branchDepth || 0) === 1).length;
  if (sideBranchCount < Number(profile.sideBranchCount || 0)) {
    issues.push({ severity: "warning", code: "side_branch_count_low", actual: sideBranchCount, expected: Number(profile.sideBranchCount || 0) });
  }
  return issues;
}

export function summarizeRoomGraph(graph = {}, issues = []) {
  const nodes = Array.isArray(graph.nodes) ? graph.nodes : [];
  const edges = Array.isArray(graph.edges) ? graph.edges : [];
  return {
    roomCount: nodes.length,
    edgeCount: edges.length,
    criticalPathLength: Array.isArray(graph.criticalPath) ? graph.criticalPath.length : 0,
    mergeEdgeCount: edges.filter((edge) => edge.type === "merge").length,
    branchNodeCount: nodes.filter((node) => (node.branchDepth || 0) > 0).length,
    issueCount: issues.length,
    errorCount: issues.filter((issue) => issue.severity === "error").length,
    warningCount: issues.filter((issue) => issue.severity === "warning").length,
  };
}

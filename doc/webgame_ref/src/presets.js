const STORAGE_KEY = "serpent_room_presets_v1";

const BUILTIN_PRESETS = [
  makeBuiltin("rect_hall", "Rect Hall", rectCells(4, 3), { tags: ["hub", "hall"] }),
  makeBuiltin("long_hall", "Long Hall", rectCells(6, 2), { tags: ["spine", "corridor"] }),
  makeBuiltin("cross_block", "Cross Block", crossCells(3, 3), { tags: ["junction"] }),
  makeBuiltin("l_block", "L Block", lCells(5, 4, false), { tags: ["turn"] }),
  makeBuiltin("shrine", "Shrine", shrineCells(), { tags: ["ritual", "boss"] }),
  makeBuiltin("crypt_cluster", "Crypt Cluster", cryptClusterCells(), { tags: ["loot", "crypt"] }),
];

function makeBuiltin(id, name, cells, extra = {}) {
  return normalizePresetDefinition({
    id,
    name,
    kind: "builtin",
    width: 7,
    height: 7,
    cells,
    ...extra,
  });
}

function rectCells(width, height) {
  const cells = [];
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) cells.push({ x, y });
  return cells;
}

function crossCells(width, height) {
  const cx = Math.floor(width / 2);
  const cy = Math.floor(height / 2);
  const cells = [];
  for (let x = 0; x < width; x++) cells.push({ x, y: cy });
  for (let y = 0; y < height; y++) cells.push({ x: cx, y });
  cells.push({ x: cx - 1, y: cy });
  cells.push({ x: cx + 1, y: cy });
  cells.push({ x: cx, y: cy - 1 });
  cells.push({ x: cx, y: cy + 1 });
  return cells;
}

function lCells(width, height, flip) {
  const cells = [];
  for (let x = 0; x < width; x++) cells.push({ x, y: 0 }, { x, y: 1 });
  const stemX = flip ? width - 2 : 0;
  for (let y = 0; y < height; y++) cells.push({ x: stemX, y }, { x: stemX + 1, y });
  return cells;
}

function shrineCells() {
  return [
    ...rectCells(5, 4),
    { x: 2, y: 4 },
    { x: 1, y: 4 },
    { x: 3, y: 4 },
    { x: 2, y: 5 },
  ];
}

function cryptClusterCells() {
  return [
    ...rectCells(2, 3),
    ...offsetCells(rectCells(2, 3), 3, 0),
    ...offsetCells(rectCells(2, 2), 1, 3),
    { x: 2, y: 2 },
  ];
}

function offsetCells(cells, dx, dy) {
  return cells.map((cell) => ({ x: cell.x + dx, y: cell.y + dy }));
}

export function normalizePresetDefinition(definition) {
  const cells = uniqueCells((definition.cells || []).map((cell) => ({
    x: Number(cell.x) || 0,
    y: Number(cell.y) || 0,
    type: cell.type || "floor",
  })));
  const normalized = normalizeCells(cells);
  const bounds = measureCells(normalized);
  return {
    id: definition.id || `preset_${Date.now()}`,
    name: definition.name || "Untitled Preset",
    kind: definition.kind || "custom",
    tags: [...new Set(definition.tags || [])],
    notes: definition.notes || "",
    width: Math.max(definition.width || bounds.width || 1, bounds.width || 1),
    height: Math.max(definition.height || bounds.height || 1, bounds.height || 1),
    cells: normalized,
  };
}

export function uniqueCells(cells) {
  const seen = new Set();
  const unique = [];
  for (const cell of cells) {
    const key = `${cell.x},${cell.y}`;
    if (seen.has(key)) continue;
    seen.add(key);
    unique.push(cell);
  }
  return unique;
}

export function normalizeCells(cells) {
  if (!cells.length) return [];
  const minX = Math.min(...cells.map((cell) => cell.x));
  const minY = Math.min(...cells.map((cell) => cell.y));
  return cells.map((cell) => ({ ...cell, x: cell.x - minX, y: cell.y - minY }));
}

export function measureCells(cells) {
  if (!cells.length) return { width: 0, height: 0 };
  return {
    width: Math.max(...cells.map((cell) => cell.x)) + 1,
    height: Math.max(...cells.map((cell) => cell.y)) + 1,
  };
}

export function rotateCells(cells, rotation = 0) {
  const turns = ((rotation % 4) + 4) % 4;
  if (!turns) return normalizeCells(cells.map((cell) => ({ ...cell })));
  let rotated = cells.map((cell) => ({ ...cell }));
  for (let i = 0; i < turns; i++) {
    const size = measureCells(rotated);
    rotated = rotated.map((cell) => ({
      ...cell,
      x: size.height - 1 - cell.y,
      y: cell.x,
    }));
  }
  return normalizeCells(rotated);
}

export function instantiatePreset(definition, options = {}) {
  const rotation = options.rotation || 0;
  const originX = options.originX || 0;
  const originY = options.originY || 0;
  const cells = rotateCells(definition.cells, rotation).map((cell) => ({
    ...cell,
    x: cell.x + originX,
    y: cell.y + originY,
  }));
  return {
    ...definition,
    rotation,
    cells,
    bounds: measureCells(cells),
  };
}

export function createPreviewGrid(definition, options = {}) {
  const rotation = options.rotation || 0;
  const cells = rotateCells(definition.cells, rotation);
  const bounds = measureCells(cells);
  const width = Math.max(options.width || definition.width || bounds.width, bounds.width, 1);
  const height = Math.max(options.height || definition.height || bounds.height, bounds.height, 1);
  const grid = Array.from({ length: height }, () => Array.from({ length: width }, () => 0));
  cells.forEach((cell) => {
    if (grid[cell.y] && grid[cell.y][cell.x] !== undefined) grid[cell.y][cell.x] = 1;
  });
  return grid;
}

export function createDraftPreset(width = 7, height = 7) {
  return {
    id: "",
    name: "",
    width,
    height,
    tags: [],
    notes: "",
    cells: [],
  };
}

export function draftFromGrid(grid, metadata = {}) {
  const cells = [];
  for (let y = 0; y < grid.length; y++) {
    for (let x = 0; x < grid[y].length; x++) {
      if (grid[y][x]) cells.push({ x, y });
    }
  }
  return normalizePresetDefinition({
    ...metadata,
    width: grid[0]?.length || 1,
    height: grid.length || 1,
    cells,
  });
}

export function clonePreset(definition) {
  return JSON.parse(JSON.stringify(definition));
}

export function listBuiltinPresets() {
  return BUILTIN_PRESETS.map(clonePreset);
}

export function loadCustomPresets() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.map(normalizePresetDefinition) : [];
  } catch {
    return [];
  }
}

export function saveCustomPresets(presets) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(presets.map(normalizePresetDefinition)));
}

export function upsertCustomPreset(definition) {
  const normalized = normalizePresetDefinition(definition);
  const presets = loadCustomPresets();
  const next = presets.filter((preset) => preset.id !== normalized.id && preset.name !== normalized.name);
  next.push(normalized);
  saveCustomPresets(next);
  return next;
}

export function deleteCustomPreset(id) {
  const next = loadCustomPresets().filter((preset) => preset.id !== id);
  saveCustomPresets(next);
  return next;
}

export function buildPresetCatalog() {
  return [...listBuiltinPresets(), ...loadCustomPresets()];
}

export function getPresetById(id, catalog = buildPresetCatalog()) {
  return catalog.find((preset) => preset.id === id) || null;
}

export function serializePresetDefinition(definition) {
  return JSON.stringify(normalizePresetDefinition(definition), null, 2);
}

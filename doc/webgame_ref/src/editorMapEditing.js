function clonePoint(point) {
  return point ? { x: point.x, y: point.y } : null;
}

export function selectedTextureBrush(deps) {
  const {
    state,
    normalizeTextureId,
    FLOOR_TEXTURE_IDS,
    CEILING_TEXTURE_IDS,
    WALL_TEXTURE_IDS,
    DEFAULT_FLOOR_TEXTURE_ID,
    DEFAULT_CEILING_TEXTURE_ID,
    DEFAULT_WALL_TEXTURE_ID,
  } = deps;
  return {
    floorTexture: normalizeTextureId(state.editor.selectedFloorTextureId, FLOOR_TEXTURE_IDS, DEFAULT_FLOOR_TEXTURE_ID),
    ceilingTexture: normalizeTextureId(state.editor.selectedCeilingTextureId, CEILING_TEXTURE_IDS, DEFAULT_CEILING_TEXTURE_ID),
    wallTexture: normalizeTextureId(state.editor.selectedWallTextureId, WALL_TEXTURE_IDS, DEFAULT_WALL_TEXTURE_ID),
  };
}

export function applySurfaceTexturesToCell(cell, deps, textures = selectedTextureBrush(deps)) {
  const {
    normalizeTextureId,
    FLOOR_TEXTURE_IDS,
    CEILING_TEXTURE_IDS,
    WALL_TEXTURE_IDS,
    DEFAULT_FLOOR_TEXTURE_ID,
    DEFAULT_CEILING_TEXTURE_ID,
    DEFAULT_WALL_TEXTURE_ID,
  } = deps;
  if (!cell) return;
  cell.floorTexture = normalizeTextureId(textures.floorTexture, FLOOR_TEXTURE_IDS, DEFAULT_FLOOR_TEXTURE_ID);
  cell.ceilingTexture = normalizeTextureId(textures.ceilingTexture, CEILING_TEXTURE_IDS, DEFAULT_CEILING_TEXTURE_ID);
  cell.wallTexture = normalizeTextureId(textures.wallTexture, WALL_TEXTURE_IDS, DEFAULT_WALL_TEXTURE_ID);
  cell.floorMaterialId = cell.floorTexture;
  cell.ceilingMaterialId = cell.ceilingTexture;
  cell.wallMaterialId = cell.wallTexture;
}

export function applyTextureRange(map, start, end, deps, textures = selectedTextureBrush(deps)) {
  const { roomRectFromPoints, getCell } = deps;
  const rect = roomRectFromPoints(start, end);
  let count = 0;
  for (let y = rect.y; y < rect.y + rect.height; y += 1) {
    for (let x = rect.x; x < rect.x + rect.width; x += 1) {
      const cell = getCell(map, x, y);
      if (!cell) continue;
      applySurfaceTexturesToCell(cell, deps, textures);
      count += 1;
    }
  }
  return { rect, count, ...textures };
}

export function applyTextureSelection(map, points, deps, textures = selectedTextureBrush(deps)) {
  const { getCell } = deps;
  let count = 0;
  for (const point of points) {
    const cell = getCell(map, point.x, point.y);
    if (!cell) continue;
    applySurfaceTexturesToCell(cell, deps, textures);
    count += 1;
  }
  return { count, ...textures };
}

export function selectionBoundsFromPoints(points) {
  if (!points.length) return null;
  const xs = points.map((point) => point.x);
  const ys = points.map((point) => point.y);
  return {
    x: Math.min(...xs),
    y: Math.min(...ys),
    width: Math.max(...xs) - Math.min(...xs) + 1,
    height: Math.max(...ys) - Math.min(...ys) + 1,
  };
}

export function uniqueSelectionPoints(points, deps) {
  const { state, getCell, cellCoordKey } = deps;
  const seen = new Set();
  const unique = [];
  for (const point of points) {
    const cell = getCell(state.map, point.x, point.y);
    if (!cell) continue;
    const key = cellCoordKey(point.x, point.y);
    if (seen.has(key)) continue;
    seen.add(key);
    unique.push({ x: point.x, y: point.y });
  }
  return unique;
}

export function selectionPointsFromRect(rect, deps) {
  const { state, getCell } = deps;
  if (!rect) return [];
  const points = [];
  for (let y = rect.y; y < rect.y + rect.height; y += 1) {
    for (let x = rect.x; x < rect.x + rect.width; x += 1) {
      if (getCell(state.map, x, y)) points.push({ x, y });
    }
  }
  return points;
}

export function cellsInRoomRect(map, rect, deps) {
  const { getCell } = deps;
  const cells = [];
  for (let y = rect.y; y < rect.y + rect.height; y += 1) {
    for (let x = rect.x; x < rect.x + rect.width; x += 1) {
      const cell = getCell(map, x, y);
      if (cell) cells.push(cell);
    }
  }
  return cells;
}

export function activeBrushRangeStart(deps) {
  const { state } = deps;
  if (state.editorTool === "room") return state.roomRangeStart;
  if (state.editorTool === "cellTag" || state.editorTool === "battleBg" || state.editorTool === "texture") return state.metadataRangeStart;
  return null;
}

export function isRangeBrushTool(deps, tool = deps.state.editorTool) {
  return tool === "room" || tool === "cellTag" || tool === "battleBg" || tool === "texture";
}

export function isLassoBrushMode(deps, tool = deps.state.editorTool) {
  return isRangeBrushTool(deps, tool) && deps.state.metadataSelectionMode === "lasso";
}

export function activeBrushPreviewRect(deps) {
  const { state, roomRectFromPoints } = deps;
  if (isLassoBrushMode(deps)) return null;
  const start = activeBrushRangeStart(deps);
  if (!start) return null;
  return roomRectFromPoints(start, state.editorCursor);
}

export function committedBrushSelectionPoints(deps, tool = deps.state.editorTool) {
  const { state, pointFromCellCoordKey } = deps;
  if (!state.lastBrushSelection || state.lastBrushSelection.tool !== tool) return [];
  if (Array.isArray(state.lastBrushSelection.cells) && state.lastBrushSelection.cells.length) {
    return uniqueSelectionPoints(state.lastBrushSelection.cells.map((key) => pointFromCellCoordKey(key)), deps);
  }
  if (state.lastBrushSelection.rect) return uniqueSelectionPoints(selectionPointsFromRect(state.lastBrushSelection.rect, deps), deps);
  return [];
}

export function currentBrushSelectionKeys(deps, tool = deps.state.editorTool) {
  const { state, cellCoordKey } = deps;
  const base = new Set(committedBrushSelectionPoints(deps, tool).map((point) => cellCoordKey(point.x, point.y)));
  if (!state.editorLassoSelectionDrag || state.editorLassoSelectionDrag.tool !== tool) return base;
  for (const key of state.editorLassoSelectionDrag.keys || []) {
    if (state.editorLassoSelectionDrag.action === "subtract") base.delete(key);
    else base.add(key);
  }
  return base;
}

export function activeBrushSelectionPoints(deps, tool = deps.state.editorTool) {
  const { pointFromCellCoordKey } = deps;
  return [...currentBrushSelectionKeys(deps, tool)].map((key) => pointFromCellCoordKey(key));
}

export function activeBrushSelectionRect(deps) {
  const { state } = deps;
  const points = activeBrushSelectionPoints(deps);
  if (points.length) return selectionBoundsFromPoints(points);
  if (!state.lastBrushSelection?.rect) return null;
  if (state.lastBrushSelection.tool !== state.editorTool) return null;
  return state.lastBrushSelection.rect;
}

export function beginRangeBrushDrag(x, y, deps, pointerId = null) {
  const { state } = deps;
  if (!isRangeBrushTool(deps)) return;
  state.editorCursor = { x, y };
  if (state.editorTool === "room") state.roomRangeStart = { x, y };
  else state.metadataRangeStart = { x, y };
  state.editorBrushDrag = {
    tool: state.editorTool,
    pointerId,
  };
}

export function updateRangeBrushDrag(x, y, deps) {
  const { state } = deps;
  if (!state.editorBrushDrag || state.editorBrushDrag.tool !== state.editorTool) return;
  state.editorCursor = { x, y };
}

export function beginLassoBrushDrag(x, y, deps, pointerId = null) {
  const { state, cellCoordKey } = deps;
  if (!isLassoBrushMode(deps)) return;
  state.editorCursor = { x, y };
  state.editorLassoSelectionDrag = {
    tool: state.editorTool,
    pointerId,
    action: state.lassoSelectionAction || "add",
    keys: [cellCoordKey(x, y)],
    lastKey: cellCoordKey(x, y),
  };
}

export function updateLassoBrushDrag(x, y, deps) {
  const { state, cellCoordKey } = deps;
  if (!state.editorLassoSelectionDrag || state.editorLassoSelectionDrag.tool !== state.editorTool) return;
  state.editorCursor = { x, y };
  const key = cellCoordKey(x, y);
  if (state.editorLassoSelectionDrag.lastKey === key) return;
  state.editorLassoSelectionDrag.lastKey = key;
  if (!state.editorLassoSelectionDrag.keys.includes(key)) state.editorLassoSelectionDrag.keys.push(key);
}

export function rememberBrushSelection(tool, selection, deps, details = {}) {
  const { state, pointFromCellCoordKey } = deps;
  const rect = selection?.rect || null;
  const cells = Array.isArray(selection?.cells) ? [...new Set(selection.cells)] : null;
  state.lastBrushSelection = {
    tool,
    shape: selection?.shape || (cells ? "lasso" : "rect"),
    rect: rect ? { ...rect } : (cells ? selectionBoundsFromPoints(cells.map((key) => pointFromCellCoordKey(key))) : null),
    cells,
    details: JSON.parse(JSON.stringify(details)),
  };
}

export function commitLassoBrushSelection(deps, logResult = false) {
  const { state, addLog } = deps;
  if (!state.editorLassoSelectionDrag || state.editorLassoSelectionDrag.tool !== state.editorTool) return false;
  const keys = [...currentBrushSelectionKeys(deps)];
  state.editorLassoSelectionDrag = null;
  if (!keys.length) {
    state.lastBrushSelection = null;
    if (logResult) addLog(`${state.editorTool} lasso selection을 비웠다.`);
    return true;
  }
  rememberBrushSelection(state.editorTool, { shape: "lasso", cells: keys }, deps, state.lastBrushSelection?.details || {});
  if (logResult) addLog(`${state.editorTool} lasso selection ${keys.length}칸을 잡았다.`);
  return true;
}

export function clearRangeBrushState(deps) {
  const { state } = deps;
  state.roomRangeStart = null;
  state.metadataRangeStart = null;
  state.editorBrushDrag = null;
  state.editorLassoSelectionDrag = null;
}

export function replaceRoomBounds(map, roomId, roomType, rect) {
  if (!map.rooms) map.rooms = [];
  let room = map.rooms.find((entry) => entry.id === roomId);
  if (!room) {
    room = {
      id: roomId,
      roomType,
      bounds: { ...rect },
      tags: [],
    };
    map.rooms.push(room);
  }
  room.roomType = roomType;
  room.bounds = { ...rect };
  return room;
}

export function removeCellFromRooms(map) {
  map.rooms = (map.rooms || []).filter((room) => {
    const cells = map.cells.filter((cell) => cell.roomId === room.id);
    if (!cells.length) return false;
    const xs = cells.map((cell) => cell.x);
    const ys = cells.map((cell) => cell.y);
    room.bounds = {
      x: Math.min(...xs),
      y: Math.min(...ys),
      width: Math.max(...xs) - Math.min(...xs) + 1,
      height: Math.max(...ys) - Math.min(...ys) + 1,
    };
    return true;
  });
}

export function applyRoomRange(map, start, end, roomId, roomType, deps) {
  const { roomRectFromPoints } = deps;
  const rect = roomRectFromPoints(start, end);
  const targetCells = cellsInRoomRect(map, rect, deps);
  const normalizedRoomId = roomId || `room_${roomType}_${Date.now()}`;
  const clearRange = targetCells.length > 0 && targetCells.every((cell) => cell.roomId === normalizedRoomId);
  const affectedRoomIds = new Set(targetCells.map((cell) => cell.roomId).filter(Boolean));
  if (clearRange) {
    for (const cell of targetCells) cell.roomId = null;
    removeCellFromRooms(map);
    return { mode: "clear", roomId: normalizedRoomId, rect };
  }
  for (const cell of map.cells) {
    if (cell.roomId === normalizedRoomId) cell.roomId = null;
  }
  for (const cell of targetCells) {
    cell.walkable = true;
    cell.roomId = normalizedRoomId;
  }
  affectedRoomIds.add(normalizedRoomId);
  map.rooms = (map.rooms || []).filter((room) => room.id !== normalizedRoomId);
  removeCellFromRooms(map);
  replaceRoomBounds(map, normalizedRoomId, roomType, rect);
  return { mode: "paint", roomId: normalizedRoomId, rect };
}

export function syncRoomRegistryFromCells(map, roomId, roomType) {
  const remaining = map.cells.filter((cell) => cell.roomId === roomId);
  map.rooms = (map.rooms || []).filter((room) => room.id !== roomId);
  const bounds = selectionBoundsFromPoints(remaining.map((cell) => ({ x: cell.x, y: cell.y })));
  if (bounds) replaceRoomBounds(map, roomId, roomType, bounds);
}

export function applyRoomSelection(map, points, roomId, roomType, deps) {
  const { getCell } = deps;
  const targetCells = uniqueSelectionPoints(points, deps).map((point) => getCell(map, point.x, point.y)).filter(Boolean);
  const normalizedRoomId = roomId || targetCells.find((cell) => cell.roomId)?.roomId || `room_${roomType}_${Date.now()}`;
  const clearSelection = targetCells.length > 0 && targetCells.every((cell) => cell.roomId === normalizedRoomId);
  if (clearSelection) {
    for (const cell of targetCells) cell.roomId = null;
    syncRoomRegistryFromCells(map, normalizedRoomId, roomType);
    return { mode: "clear", roomId: normalizedRoomId, rect: selectionBoundsFromPoints(points), count: targetCells.length };
  }
  for (const cell of map.cells) {
    if (cell.roomId === normalizedRoomId) cell.roomId = null;
  }
  for (const cell of targetCells) {
    cell.walkable = true;
    cell.roomId = normalizedRoomId;
  }
  syncRoomRegistryFromCells(map, normalizedRoomId, roomType);
  return { mode: "paint", roomId: normalizedRoomId, rect: selectionBoundsFromPoints(points), count: targetCells.length };
}

export function applyCellTagRange(map, start, end, tag, deps) {
  const { roomRectFromPoints } = deps;
  const rect = roomRectFromPoints(start, end);
  const targetCells = cellsInRoomRect(map, rect, deps);
  const remove = targetCells.length > 0 && targetCells.every((cell) => (cell.tags || []).includes(tag));
  for (const cell of targetCells) {
    cell.walkable = true;
    const nextTags = new Set(cell.tags || []);
    if (remove) nextTags.delete(tag);
    else nextTags.add(tag);
    cell.tags = [...nextTags];
  }
  return { rect, mode: remove ? "clear" : "paint", tag };
}

export function applyCellTagSelection(map, points, tag, deps) {
  const { getCell } = deps;
  const targetCells = uniqueSelectionPoints(points, deps).map((point) => getCell(map, point.x, point.y)).filter(Boolean);
  const remove = targetCells.length > 0 && targetCells.every((cell) => (cell.tags || []).includes(tag));
  for (const cell of targetCells) {
    cell.walkable = true;
    const nextTags = new Set(cell.tags || []);
    if (remove) nextTags.delete(tag);
    else nextTags.add(tag);
    cell.tags = [...nextTags];
  }
  return { rect: selectionBoundsFromPoints(points), mode: remove ? "clear" : "paint", tag, count: targetCells.length };
}

export function applyBattleBgRange(map, start, end, battleBackgroundId, deps) {
  const { roomRectFromPoints } = deps;
  const rect = roomRectFromPoints(start, end);
  const targetCells = cellsInRoomRect(map, rect, deps);
  for (const cell of targetCells) {
    cell.walkable = true;
    cell.battleBackgroundId = battleBackgroundId || null;
  }
  return { rect, battleBackgroundId: battleBackgroundId || null };
}

export function applyBattleBgSelection(map, points, battleBackgroundId, deps) {
  const { getCell } = deps;
  const targetCells = uniqueSelectionPoints(points, deps).map((point) => getCell(map, point.x, point.y)).filter(Boolean);
  for (const cell of targetCells) {
    cell.walkable = true;
    cell.battleBackgroundId = battleBackgroundId || null;
  }
  return { rect: selectionBoundsFromPoints(points), battleBackgroundId: battleBackgroundId || null, count: targetCells.length };
}

export function applyRangeBrushAtCurrentCursor(deps, logResult = true) {
  const { state, getCell, addLog } = deps;
  const { x, y } = state.editorCursor;
  const c = getCell(state.map, x, y);
  if (!c || !isRangeBrushTool(deps)) return false;
  c.walkable = true;
  if (state.editorTool === "cellTag") {
    const start = state.metadataRangeStart || { x, y };
    const result = applyCellTagRange(state.map, start, { x, y }, state.selectedCellTag, deps);
    rememberBrushSelection("cellTag", { shape: "rect", rect: result.rect }, deps, { tag: result.tag, mode: result.mode });
    clearRangeBrushState(deps);
    if (logResult) {
      addLog(
        result.mode === "paint"
          ? `${result.tag} cellTag를 ${result.rect.x},${result.rect.y} ${result.rect.width}x${result.rect.height} 범위에 적용했다.`
          : `${result.tag} cellTag를 ${result.rect.x},${result.rect.y} ${result.rect.width}x${result.rect.height} 범위에서 제거했다.`
      );
    }
    return true;
  }
  if (state.editorTool === "battleBg") {
    const start = state.metadataRangeStart || { x, y };
    const result = applyBattleBgRange(state.map, start, { x, y }, state.selectedBattleBackgroundId, deps);
    rememberBrushSelection("battleBg", { shape: "rect", rect: result.rect }, deps, { battleBackgroundId: result.battleBackgroundId || "" });
    clearRangeBrushState(deps);
    if (logResult) addLog(`${result.battleBackgroundId || "(clear)"} battleBg를 ${result.rect.x},${result.rect.y} ${result.rect.width}x${result.rect.height} 범위에 적용했다.`);
    return true;
  }
  if (state.editorTool === "texture") {
    const start = state.metadataRangeStart || { x, y };
    const result = applyTextureRange(state.map, start, { x, y }, deps, selectedTextureBrush(deps));
    rememberBrushSelection("texture", { shape: "rect", rect: result.rect }, deps, {
      floorTexture: result.floorTexture,
      ceilingTexture: result.ceilingTexture,
      wallTexture: result.wallTexture,
    });
    clearRangeBrushState(deps);
    if (logResult) addLog(`texture를 ${result.rect.x},${result.rect.y} ${result.rect.width}x${result.rect.height} 범위에 적용했다. floor ${result.floorTexture} · ceiling ${result.ceilingTexture} · wall ${result.wallTexture}`);
    return true;
  }
  if (state.editorTool === "room") {
    const start = state.roomRangeStart || { x, y };
    const seedCell = getCell(state.map, start.x, start.y);
    const roomId = state.activeRoomId || seedCell?.roomId || `room_${state.selectedRoomType}_${Date.now()}`;
    const result = applyRoomRange(state.map, start, { x, y }, roomId, state.selectedRoomType, deps);
    state.activeRoomId = roomId;
    rememberBrushSelection("room", { shape: "rect", rect: result.rect }, deps, { roomId, mode: result.mode, roomType: state.selectedRoomType });
    clearRangeBrushState(deps);
    if (logResult) {
      addLog(
        result.mode === "paint"
          ? `${roomId} room 범위를 ${result.rect.x},${result.rect.y} ${result.rect.width}x${result.rect.height}로 지정했다.`
          : `${roomId} room 범위에서 ${result.rect.x},${result.rect.y} ${result.rect.width}x${result.rect.height}를 제거했다.`
      );
    }
    return true;
  }
  return false;
}

export function applyCommittedBrushSelection(deps, logResult = true) {
  const { state, getCell, addLog, cellCoordKey } = deps;
  const points = committedBrushSelectionPoints(deps);
  if (!points.length || !isRangeBrushTool(deps)) return false;
  if (state.editorTool === "cellTag") {
    const result = applyCellTagSelection(state.map, points, state.selectedCellTag, deps);
    rememberBrushSelection("cellTag", { shape: "lasso", cells: points.map((point) => cellCoordKey(point.x, point.y)) }, deps, { tag: result.tag, mode: result.mode });
    if (logResult) {
      addLog(
        result.mode === "paint"
          ? `${result.tag} cellTag를 선택 ${result.count}칸에 적용했다.`
          : `${result.tag} cellTag를 선택 ${result.count}칸에서 제거했다.`
      );
    }
    return true;
  }
  if (state.editorTool === "battleBg") {
    const result = applyBattleBgSelection(state.map, points, state.selectedBattleBackgroundId, deps);
    rememberBrushSelection("battleBg", { shape: "lasso", cells: points.map((point) => cellCoordKey(point.x, point.y)) }, deps, { battleBackgroundId: result.battleBackgroundId || "" });
    if (logResult) addLog(`${result.battleBackgroundId || "(clear)"} battleBg를 선택 ${result.count}칸에 적용했다.`);
    return true;
  }
  if (state.editorTool === "texture") {
    const result = applyTextureSelection(state.map, points, deps, selectedTextureBrush(deps));
    rememberBrushSelection("texture", { shape: "lasso", cells: points.map((point) => cellCoordKey(point.x, point.y)) }, deps, {
      floorTexture: result.floorTexture,
      ceilingTexture: result.ceilingTexture,
      wallTexture: result.wallTexture,
    });
    if (logResult) addLog(`texture를 선택 ${result.count}칸에 적용했다. floor ${result.floorTexture} · ceiling ${result.ceilingTexture} · wall ${result.wallTexture}`);
    return true;
  }
  if (state.editorTool === "room") {
    const roomId = state.activeRoomId || getCell(state.map, points[0].x, points[0].y)?.roomId || `room_${state.selectedRoomType}_${Date.now()}`;
    const result = applyRoomSelection(state.map, points, roomId, state.selectedRoomType, deps);
    state.activeRoomId = result.roomId;
    rememberBrushSelection("room", { shape: "lasso", cells: points.map((point) => cellCoordKey(point.x, point.y)) }, deps, { roomId: result.roomId, mode: result.mode, roomType: state.selectedRoomType });
    if (logResult) {
      addLog(
        result.mode === "paint"
          ? `${result.roomId} room을 선택 ${result.count}칸에 적용했다.`
          : `${result.roomId} room을 선택 ${result.count}칸에서 제거했다.`
      );
    }
    return true;
  }
  return false;
}

export function transformCommittedBrushSelection(transform, deps, label) {
  const { state, addLog, cellCoordKey } = deps;
  const points = committedBrushSelectionPoints(deps);
  if (!points.length || !isRangeBrushTool(deps)) return false;
  const nextPoints = uniqueSelectionPoints(transform(points, deps), deps);
  if (!nextPoints.length) {
    state.lastBrushSelection = null;
    addLog(`${state.editorTool} selection을 ${label}한 결과 비었다.`);
    return true;
  }
  rememberBrushSelection(state.editorTool, { shape: "lasso", cells: nextPoints.map((point) => cellCoordKey(point.x, point.y)) }, deps, state.lastBrushSelection?.details || {});
  addLog(`${state.editorTool} selection을 ${label}해 ${nextPoints.length}칸으로 바꿨다.`);
  return true;
}

export function floodSelectionPoints(map, seed, matcher, deps) {
  const { getCell, cellCoordKey, DIRS, VEC } = deps;
  const startCell = getCell(map, seed?.x, seed?.y);
  if (!startCell || typeof matcher !== "function" || !matcher(startCell)) return [];
  const seen = new Set([cellCoordKey(startCell.x, startCell.y)]);
  const queue = [{ x: startCell.x, y: startCell.y }];
  const points = [{ x: startCell.x, y: startCell.y }];
  while (queue.length) {
    const point = queue.shift();
    for (const dir of DIRS) {
      const next = getCell(map, point.x + VEC[dir].x, point.y + VEC[dir].y);
      if (!next) continue;
      const key = cellCoordKey(next.x, next.y);
      if (seen.has(key) || !matcher(next)) continue;
      seen.add(key);
      queue.push({ x: next.x, y: next.y });
      points.push({ x: next.x, y: next.y });
    }
  }
  return points;
}

export function currentBrushFloodSelectionPoints(deps) {
  const {
    state,
    getCell,
    DEFAULT_FLOOR_TEXTURE_ID,
    DEFAULT_CEILING_TEXTURE_ID,
    DEFAULT_WALL_TEXTURE_ID,
  } = deps;
  const seed = state.editorCursor;
  const seedCell = getCell(state.map, seed?.x, seed?.y);
  if (!seedCell || !isRangeBrushTool(deps)) return [];
  if (state.editorTool === "room") {
    const roomId = state.activeRoomId || seedCell.roomId || "";
    if (!roomId) return [];
    return floodSelectionPoints(state.map, seed, (cell) => cell.roomId === roomId, deps);
  }
  if (state.editorTool === "cellTag") {
    const tag = state.selectedCellTag;
    if (!tag || !(seedCell.tags || []).includes(tag)) return [];
    return floodSelectionPoints(state.map, seed, (cell) => (cell.tags || []).includes(tag), deps);
  }
  if (state.editorTool === "battleBg") {
    const battleBackgroundId = seedCell.battleBackgroundId || "";
    return floodSelectionPoints(state.map, seed, (cell) => (cell.battleBackgroundId || "") === battleBackgroundId, deps);
  }
  if (state.editorTool === "texture") {
    const signature = [
      seedCell.floorTexture || DEFAULT_FLOOR_TEXTURE_ID,
      seedCell.ceilingTexture || DEFAULT_CEILING_TEXTURE_ID,
      seedCell.wallTexture || DEFAULT_WALL_TEXTURE_ID,
    ].join("|");
    return floodSelectionPoints(state.map, seed, (cell) => ([
      cell.floorTexture || DEFAULT_FLOOR_TEXTURE_ID,
      cell.ceilingTexture || DEFAULT_CEILING_TEXTURE_ID,
      cell.wallTexture || DEFAULT_WALL_TEXTURE_ID,
    ].join("|") === signature), deps);
  }
  return [];
}

export function currentBrushMatchingSelectionPoints(deps) {
  const {
    state,
    getCell,
    DEFAULT_FLOOR_TEXTURE_ID,
    DEFAULT_CEILING_TEXTURE_ID,
    DEFAULT_WALL_TEXTURE_ID,
  } = deps;
  const seed = state.editorCursor;
  const seedCell = getCell(state.map, seed?.x, seed?.y);
  if (!seedCell || !isRangeBrushTool(deps)) return [];
  if (state.editorTool === "room") {
    const roomId = state.activeRoomId || seedCell.roomId || "";
    if (!roomId) return [];
    return state.map.cells.filter((cell) => cell.roomId === roomId).map((cell) => ({ x: cell.x, y: cell.y }));
  }
  if (state.editorTool === "cellTag") {
    const tag = state.selectedCellTag;
    if (!tag) return [];
    return state.map.cells.filter((cell) => (cell.tags || []).includes(tag)).map((cell) => ({ x: cell.x, y: cell.y }));
  }
  if (state.editorTool === "battleBg") {
    const battleBackgroundId = state.selectedBattleBackgroundId || "";
    return state.map.cells.filter((cell) => (cell.battleBackgroundId || "") === battleBackgroundId).map((cell) => ({ x: cell.x, y: cell.y }));
  }
  if (state.editorTool === "texture") {
    return state.map.cells.filter((cell) => (
      (cell.floorTexture || DEFAULT_FLOOR_TEXTURE_ID) === state.selectedFloorTextureId
      && (cell.ceilingTexture || DEFAULT_CEILING_TEXTURE_ID) === state.selectedCeilingTextureId
      && (cell.wallTexture || DEFAULT_WALL_TEXTURE_ID) === state.selectedWallTextureId
    )).map((cell) => ({ x: cell.x, y: cell.y }));
  }
  return [];
}

export function rememberExplicitBrushSelection(points, deps, label, details = deps.state.lastBrushSelection?.details || {}) {
  const { state, addLog, cellCoordKey } = deps;
  const uniquePoints = uniqueSelectionPoints(points, deps);
  if (!uniquePoints.length) {
    state.lastBrushSelection = null;
    addLog(`${state.editorTool} ${label} 결과가 비었다.`);
    return false;
  }
  rememberBrushSelection(
    state.editorTool,
    { shape: "lasso", cells: uniquePoints.map((point) => cellCoordKey(point.x, point.y)) },
    deps,
    details
  );
  addLog(`${state.editorTool} ${label} ${uniquePoints.length}칸을 잡았다.`);
  return true;
}

export function grownSelectionPoints(points, deps) {
  const { state, getCell, cellCoordKey } = deps;
  const next = new Map(uniqueSelectionPoints(points, deps).map((point) => [cellCoordKey(point.x, point.y), point]));
  for (const point of uniqueSelectionPoints(points, deps)) {
    for (const delta of [{ x: 1, y: 0 }, { x: -1, y: 0 }, { x: 0, y: 1 }, { x: 0, y: -1 }]) {
      const neighbor = { x: point.x + delta.x, y: point.y + delta.y };
      if (getCell(state.map, neighbor.x, neighbor.y)) next.set(cellCoordKey(neighbor.x, neighbor.y), neighbor);
    }
  }
  return [...next.values()];
}

export function shrunkSelectionPoints(points, deps) {
  const { cellCoordKey } = deps;
  const current = uniqueSelectionPoints(points, deps);
  const set = new Set(current.map((point) => cellCoordKey(point.x, point.y)));
  return current.filter((point) => (
    set.has(cellCoordKey(point.x + 1, point.y))
    && set.has(cellCoordKey(point.x - 1, point.y))
    && set.has(cellCoordKey(point.x, point.y + 1))
    && set.has(cellCoordKey(point.x, point.y - 1))
  ));
}

export function invertedSelectionPoints(points, deps) {
  const { state, cellCoordKey } = deps;
  const selected = new Set(uniqueSelectionPoints(points, deps).map((point) => cellCoordKey(point.x, point.y)));
  return state.map.cells.filter((cell) => !selected.has(cellCoordKey(cell.x, cell.y))).map((cell) => ({ x: cell.x, y: cell.y }));
}

export function placementKindForTool(tool) {
  if (tool === "eventTrigger") return "event_trigger";
  if (tool === "restSite") return "rest_site";
  return tool;
}

export function createEditorPlacement(tool, x, y, deps) {
  const {
    state,
    getCell,
    activeNpcDefinitionId,
    DEFAULT_EDITOR_ENCOUNTER_ID,
    DEFAULT_EDITOR_EVENT_ID,
    DEFAULT_EDITOR_TRAP_EVENT_ID,
    DEFAULT_EDITOR_SHRINE_EVENT_ID,
    DEFAULT_EDITOR_REST_EVENT_ID,
    DEFAULT_EDITOR_CAMP_EVENT_ID,
  } = deps;
  const cell = getCell(state.map, x, y);
  if (!cell) return null;
  const floor = state.player.floor;
  const pushPlacement = (placement) => {
    state.map.placements.push(placement);
    return placement;
  };
  if (tool === "stairs") {
    state.map.placements = state.map.placements.filter((placement) => placement.kind !== "stairs");
    return pushPlacement({ id: `stairs_${Date.now()}`, kind: "stairs", position: { floor, x, y } });
  }
  if (tool === "encounter") {
    const id = `encounter_${Date.now()}`;
    return pushPlacement({
      id,
      kind: "encounter",
      refType: "encounter",
      refId: DEFAULT_EDITOR_ENCOUNTER_ID,
      stateKey: `${state.map.id}:${id}`,
      position: { floor, x, y },
    });
  }
  if (tool === "npc") {
    const id = `npc_${Date.now()}`;
    const npcId = activeNpcDefinitionId();
    return pushPlacement({
      id,
      kind: "npc",
      refType: "npc",
      refId: npcId,
      npcId,
      stateKey: `${state.map.id}:${id}`,
      position: { floor, x, y },
    });
  }
  if (tool === "eventTrigger") {
    const id = `event_trigger_${Date.now()}`;
    const eventId = state.selectedEventDefinitionIds.eventTrigger || DEFAULT_EDITOR_EVENT_ID;
    return pushPlacement({
      id,
      kind: "event_trigger",
      refType: "event",
      refId: eventId,
      interaction: { type: "interact", eventId },
      stateKey: `${state.map.id}:${id}`,
      position: { floor, x, y },
    });
  }
  if (tool === "trap") {
    const id = `trap_${Date.now()}`;
    const eventId = state.selectedEventDefinitionIds.trap || DEFAULT_EDITOR_TRAP_EVENT_ID;
    return pushPlacement({
      id,
      kind: "trap",
      refType: "event",
      refId: eventId,
      interaction: { type: "onEnter", eventId },
      stateKey: `${state.map.id}:${id}`,
      position: { floor, x, y },
    });
  }
  if (tool === "shrine") {
    const id = `shrine_${Date.now()}`;
    const eventId = state.selectedEventDefinitionIds.shrine || DEFAULT_EDITOR_SHRINE_EVENT_ID;
    return pushPlacement({
      id,
      kind: "shrine",
      refType: "event",
      refId: eventId,
      interaction: { type: "interact", eventId },
      stateKey: `${state.map.id}:${id}`,
      position: { floor, x, y },
    });
  }
  if (tool === "restSite") {
    const id = `rest_site_${Date.now()}`;
    const eventId = state.selectedEventDefinitionIds.restSite || DEFAULT_EDITOR_REST_EVENT_ID;
    return pushPlacement({
      id,
      kind: "rest_site",
      refType: "event",
      refId: eventId,
      interaction: { type: "onRest", eventId },
      stateKey: `${state.map.id}:${id}`,
      position: { floor, x, y },
    });
  }
  if (tool === "camp") {
    const id = `camp_${Date.now()}`;
    const eventId = state.selectedEventDefinitionIds.camp || DEFAULT_EDITOR_CAMP_EVENT_ID;
    return pushPlacement({
      id,
      kind: "camp",
      refType: "event",
      refId: eventId,
      interaction: { type: "onCamp", eventId },
      stateKey: `${state.map.id}:${id}`,
      position: { floor, x, y },
    });
  }
  return null;
}

export function canAutoPlaceRecommendationTool(map, roomId, tool, deps) {
  const { roomPlacements } = deps;
  const kind = placementKindForTool(tool);
  if (kind === "stairs") {
    return !(map.placements || []).some((placement) => placement.kind === "stairs");
  }
  return !roomPlacements(map, roomId).some((placement) => placement.kind === kind);
}

export function autoPlacementCandidateCells(map, roomId, selectedCellTag, tool, deps) {
  const { roomPlacements, roomCells, cellCoordKey } = deps;
  const placements = roomPlacements(map, roomId);
  const occupied = new Set(placements.map((placement) => cellCoordKey(placement.position?.x, placement.position?.y)));
  const kind = placementKindForTool(tool);
  const baseCells = roomCells(map, roomId)
    .filter((cell) => cell.walkable)
    .filter((cell) => !occupied.has(cellCoordKey(cell.x, cell.y)));
  const tagPreferred = baseCells.filter((cell) => (cell.tags || []).includes(selectedCellTag));
  const safeCells = baseCells.filter((cell) => (cell.tags || []).includes("safe") || (cell.tags || []).includes("camp_allowed"));
  const combatCells = baseCells.filter((cell) => !(cell.tags || []).includes("safe"));
  if (kind === "rest_site" || kind === "camp") return [...tagPreferred, ...safeCells, ...baseCells];
  if (kind === "trap" || kind === "encounter") return [...tagPreferred, ...combatCells, ...baseCells];
  if (kind === "npc") return [...tagPreferred, ...safeCells, ...baseCells];
  return [...tagPreferred, ...baseCells];
}

export function applyRecommendationAutoPlacement(roomId, placementRecommendations, selectedCellTag, deps) {
  const { state, addLog, computeWalls } = deps;
  if (!roomId) {
    addLog("auto placement를 실행할 room이 없다.");
    return { placed: [], skipped: ["room 없음"] };
  }
  const placed = [];
  const skipped = [];
  const queuedTools = placementRecommendations
    .map((entry) => entry.tool)
    .filter((tool, index, arr) => arr.indexOf(tool) === index)
    .slice(0, 3);
  queuedTools.forEach((tool) => {
    if (!canAutoPlaceRecommendationTool(state.map, roomId, tool, deps)) {
      skipped.push(`${tool}: 이미 배치됨 또는 정책상 건너뜀`);
      return;
    }
    const candidates = autoPlacementCandidateCells(state.map, roomId, selectedCellTag, tool, deps);
    const target = candidates[0];
    if (!target) {
      skipped.push(`${tool}: 배치 가능한 cell 없음`);
      return;
    }
    const placement = createEditorPlacement(tool, target.x, target.y, deps);
    if (!placement) {
      skipped.push(`${tool}: 생성 helper 없음`);
      return;
    }
    placed.push(`${tool}@${target.x},${target.y}`);
  });
  computeWalls(state.map);
  if (placed.length) addLog(`room auto placement: ${placed.join(", ")}`);
  if (skipped.length) addLog(`room auto placement skip: ${skipped.join(" / ")}`);
  return { placed, skipped };
}

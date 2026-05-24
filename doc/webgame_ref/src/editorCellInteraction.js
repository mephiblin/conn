export function handleEditorCellInteraction(deps = {}) {
  const {
    x = 0,
    y = 0,
    event = null,
    state,
    cell = null,
    getCell = () => null,
    wallKey = () => "",
    createEditorPlacement = () => {},
    isLassoBrushMode = () => false,
    applyRangeBrushAtCurrentCursor = () => {},
    selectedPreset = () => null,
    instantiatePreset = () => ({ cells: [] }),
    computeWalls = () => {},
    render = () => {},
    eventObjectPlacementKinds = new Set(),
  } = deps;

  if (!state || !cell) return;

  const cursorPlacements = () => state.map.placements.filter((placement) => placement.position?.x === x
    && placement.position?.y === y
    && eventObjectPlacementKinds.has(placement.kind));
  const cursorNpcPlacements = () => state.map.placements.filter((placement) => placement.position?.x === x
    && placement.position?.y === y
    && placement.kind === "npc");

  if (event?.shiftKey) {
    if (state.editorTool === "npc") {
      const placements = cursorNpcPlacements();
      if (!placements.length) state.selectedNpcPlacementId = "";
      else {
        const currentIndex = placements.findIndex((placement) => placement.id === state.selectedNpcPlacementId);
        const nextIndex = currentIndex >= 0 ? (currentIndex + 1) % placements.length : 0;
        state.selectedNpcPlacementId = placements[nextIndex]?.id || "";
      }
    } else {
      const placements = cursorPlacements();
      if (!placements.length) state.selectedPlacementOverrideId = "";
      else {
        const currentIndex = placements.findIndex((placement) => placement.id === state.selectedPlacementOverrideId);
        const nextIndex = currentIndex >= 0 ? (currentIndex + 1) % placements.length : 0;
        state.selectedPlacementOverrideId = placements[nextIndex]?.id || "";
      }
    }
    render();
    return;
  }

  if (state.editorTool === "wall") cell.walkable = false;
  if (state.editorTool === "floor") cell.walkable = true;
  if (state.editorTool === "door" || state.editorTool === "secret") {
    cell.walkable = true;
    state.map.doors[wallKey(x, y, "north")] = { type: state.editorTool, open: false, locked: false };
  }
  if (state.editorTool === "start") {
    cell.walkable = true;
    state.map.start.x = x;
    state.map.start.y = y;
    state.player.x = x;
    state.player.y = y;
  }
  if (state.editorTool === "stairs") {
    cell.walkable = true;
    createEditorPlacement("stairs", x, y);
  }
  if (state.editorTool === "encounter") {
    cell.walkable = true;
    createEditorPlacement("encounter", x, y);
  }
  if (state.editorTool === "npc") {
    cell.walkable = true;
    createEditorPlacement("npc", x, y);
  }
  if (state.editorTool === "eventTrigger") {
    cell.walkable = true;
    createEditorPlacement("eventTrigger", x, y);
  }
  if (state.editorTool === "trap") {
    cell.walkable = true;
    createEditorPlacement("trap", x, y);
  }
  if (state.editorTool === "shrine") {
    cell.walkable = true;
    createEditorPlacement("shrine", x, y);
  }
  if (state.editorTool === "restSite") {
    cell.walkable = true;
    createEditorPlacement("restSite", x, y);
  }
  if (state.editorTool === "camp") {
    cell.walkable = true;
    createEditorPlacement("camp", x, y);
  }
  if (state.editorTool === "cellTag") {
    if (isLassoBrushMode()) {
      render();
      return;
    }
    cell.walkable = true;
    if (!state.metadataRangeStart) state.metadataRangeStart = { x, y };
    applyRangeBrushAtCurrentCursor(true);
  }
  if (state.editorTool === "battleBg") {
    if (isLassoBrushMode()) {
      render();
      return;
    }
    cell.walkable = true;
    if (!state.metadataRangeStart) state.metadataRangeStart = { x, y };
    applyRangeBrushAtCurrentCursor(true);
  }
  if (state.editorTool === "texture") {
    if (isLassoBrushMode()) {
      render();
      return;
    }
    if (!state.metadataRangeStart) state.metadataRangeStart = { x, y };
    applyRangeBrushAtCurrentCursor(true);
  }
  if (state.editorTool === "room") {
    if (isLassoBrushMode()) {
      render();
      return;
    }
    cell.walkable = true;
    if (!state.roomRangeStart) state.roomRangeStart = { x, y };
    applyRangeBrushAtCurrentCursor(true);
  }
  if (state.editorTool === "preset") {
    const preset = selectedPreset();
    if (preset) {
      const stamped = instantiatePreset(preset, { rotation: state.presetRotation, originX: x, originY: y });
      for (const stampedCell of stamped.cells) {
        const target = getCell(state.map, stampedCell.x, stampedCell.y);
        if (target) target.walkable = true;
      }
    }
  }

  const eventPlacements = cursorPlacements();
  if (!eventPlacements.some((placement) => placement.id === state.selectedPlacementOverrideId)) {
    state.selectedPlacementOverrideId = eventPlacements[eventPlacements.length - 1]?.id || "";
  }
  const npcPlacements = cursorNpcPlacements();
  if (!npcPlacements.some((placement) => placement.id === state.selectedNpcPlacementId)) {
    state.selectedNpcPlacementId = npcPlacements[npcPlacements.length - 1]?.id || "";
  }
  state.metadataRangeStart = state.editorTool === "cellTag" || state.editorTool === "battleBg" || state.editorTool === "texture"
    ? state.metadataRangeStart
    : null;
  computeWalls(state.map);
  render();
}

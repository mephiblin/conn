import {
  activeBrushPreviewRect as activeBrushPreviewRectModule,
  activeBrushRangeStart as activeBrushRangeStartModule,
  activeBrushSelectionPoints as activeBrushSelectionPointsModule,
  activeBrushSelectionRect as activeBrushSelectionRectModule,
  applyBattleBgRange as applyBattleBgRangeModule,
  applyBattleBgSelection as applyBattleBgSelectionModule,
  applyCellTagRange as applyCellTagRangeModule,
  applyCellTagSelection as applyCellTagSelectionModule,
  applyCommittedBrushSelection as applyCommittedBrushSelectionModule,
  applyRangeBrushAtCurrentCursor as applyRangeBrushAtCurrentCursorModule,
  applyRecommendationAutoPlacement as applyRecommendationAutoPlacementModule,
  applyRoomRange as applyRoomRangeModule,
  applyRoomSelection as applyRoomSelectionModule,
  applySurfaceTexturesToCell as applySurfaceTexturesToCellModule,
  applyTextureRange as applyTextureRangeModule,
  applyTextureSelection as applyTextureSelectionModule,
  autoPlacementCandidateCells as autoPlacementCandidateCellsModule,
  beginLassoBrushDrag as beginLassoBrushDragModule,
  beginRangeBrushDrag as beginRangeBrushDragModule,
  canAutoPlaceRecommendationTool as canAutoPlaceRecommendationToolModule,
  cellsInRoomRect as cellsInRoomRectModule,
  clearRangeBrushState as clearRangeBrushStateModule,
  commitLassoBrushSelection as commitLassoBrushSelectionModule,
  committedBrushSelectionPoints as committedBrushSelectionPointsModule,
  createEditorPlacement as createEditorPlacementModule,
  currentBrushFloodSelectionPoints as currentBrushFloodSelectionPointsModule,
  currentBrushMatchingSelectionPoints as currentBrushMatchingSelectionPointsModule,
  currentBrushSelectionKeys as currentBrushSelectionKeysModule,
  floodSelectionPoints as floodSelectionPointsModule,
  grownSelectionPoints as grownSelectionPointsModule,
  invertedSelectionPoints as invertedSelectionPointsModule,
  isLassoBrushMode as isLassoBrushModeModule,
  isRangeBrushTool as isRangeBrushToolModule,
  rememberBrushSelection as rememberBrushSelectionModule,
  rememberExplicitBrushSelection as rememberExplicitBrushSelectionModule,
  selectedTextureBrush as selectedTextureBrushModule,
  selectionPointsFromRect as selectionPointsFromRectModule,
  shrunkSelectionPoints as shrunkSelectionPointsModule,
  syncRoomRegistryFromCells as syncRoomRegistryFromCellsModule,
  transformCommittedBrushSelection as transformCommittedBrushSelectionModule,
  uniqueSelectionPoints as uniqueSelectionPointsModule,
  updateLassoBrushDrag as updateLassoBrushDragModule,
  updateRangeBrushDrag as updateRangeBrushDragModule,
} from "./editorMapEditing.js";

export function createEditorMapEditingBridge(deps = {}) {
  const {
    getEditorMapEditingDependencies = () => ({}),
  } = deps;

  const withDeps = (runner) => runner(getEditorMapEditingDependencies());

  return {
    selectedTextureBrush() {
      return withDeps((editorDeps) => selectedTextureBrushModule(editorDeps));
    },
    applySurfaceTexturesToCell(cell, textures) {
      return withDeps((editorDeps) => applySurfaceTexturesToCellModule(cell, editorDeps, textures));
    },
    applyTextureRange(map, start, end, textures) {
      return withDeps((editorDeps) => applyTextureRangeModule(map, start, end, editorDeps, textures));
    },
    applyTextureSelection(map, points, textures) {
      return withDeps((editorDeps) => applyTextureSelectionModule(map, points, editorDeps, textures));
    },
    uniqueSelectionPoints(points) {
      return withDeps((editorDeps) => uniqueSelectionPointsModule(points, editorDeps));
    },
    selectionPointsFromRect(rect) {
      return withDeps((editorDeps) => selectionPointsFromRectModule(rect, editorDeps));
    },
    cellsInRoomRect(map, rect) {
      return withDeps((editorDeps) => cellsInRoomRectModule(map, rect, editorDeps));
    },
    activeBrushRangeStart() {
      return withDeps((editorDeps) => activeBrushRangeStartModule(editorDeps));
    },
    isRangeBrushTool(tool) {
      return withDeps((editorDeps) => isRangeBrushToolModule(editorDeps, tool));
    },
    isLassoBrushMode(tool) {
      return withDeps((editorDeps) => isLassoBrushModeModule(editorDeps, tool));
    },
    activeBrushPreviewRect() {
      return withDeps((editorDeps) => activeBrushPreviewRectModule(editorDeps));
    },
    committedBrushSelectionPoints(tool) {
      return withDeps((editorDeps) => committedBrushSelectionPointsModule(editorDeps, tool));
    },
    currentBrushSelectionKeys(tool) {
      return withDeps((editorDeps) => currentBrushSelectionKeysModule(editorDeps, tool));
    },
    activeBrushSelectionPoints(tool) {
      return withDeps((editorDeps) => activeBrushSelectionPointsModule(editorDeps, tool));
    },
    activeBrushSelectionRect() {
      return withDeps((editorDeps) => activeBrushSelectionRectModule(editorDeps));
    },
    beginRangeBrushDrag(x, y, pointerId = null) {
      return withDeps((editorDeps) => beginRangeBrushDragModule(x, y, editorDeps, pointerId));
    },
    updateRangeBrushDrag(x, y) {
      return withDeps((editorDeps) => updateRangeBrushDragModule(x, y, editorDeps));
    },
    beginLassoBrushDrag(x, y, pointerId = null) {
      return withDeps((editorDeps) => beginLassoBrushDragModule(x, y, editorDeps, pointerId));
    },
    updateLassoBrushDrag(x, y) {
      return withDeps((editorDeps) => updateLassoBrushDragModule(x, y, editorDeps));
    },
    rememberBrushSelection(tool, selection, details = {}) {
      return withDeps((editorDeps) => rememberBrushSelectionModule(tool, selection, editorDeps, details));
    },
    commitLassoBrushSelection(logResult = false) {
      return withDeps((editorDeps) => commitLassoBrushSelectionModule(editorDeps, logResult));
    },
    clearRangeBrushState() {
      return withDeps((editorDeps) => clearRangeBrushStateModule(editorDeps));
    },
    applyRangeBrushAtCurrentCursor(logResult = true) {
      return withDeps((editorDeps) => applyRangeBrushAtCurrentCursorModule(editorDeps, logResult));
    },
    applyCommittedBrushSelection(logResult = true) {
      return withDeps((editorDeps) => applyCommittedBrushSelectionModule(editorDeps, logResult));
    },
    transformCommittedBrushSelection(transform, label) {
      return withDeps((editorDeps) => transformCommittedBrushSelectionModule(transform, editorDeps, label));
    },
    floodSelectionPoints(map, seed, matcher) {
      return withDeps((editorDeps) => floodSelectionPointsModule(map, seed, matcher, editorDeps));
    },
    currentBrushFloodSelectionPoints() {
      return withDeps((editorDeps) => currentBrushFloodSelectionPointsModule(editorDeps));
    },
    currentBrushMatchingSelectionPoints() {
      return withDeps((editorDeps) => currentBrushMatchingSelectionPointsModule(editorDeps));
    },
    rememberExplicitBrushSelection(points, label, details) {
      return withDeps((editorDeps) => rememberExplicitBrushSelectionModule(points, editorDeps, label, details));
    },
    applyRoomRange(map, start, end, roomId, roomType) {
      return withDeps((editorDeps) => applyRoomRangeModule(map, start, end, roomId, roomType, editorDeps));
    },
    syncRoomRegistryFromCells(map, roomId, roomType) {
      return syncRoomRegistryFromCellsModule(map, roomId, roomType);
    },
    applyRoomSelection(map, points, roomId, roomType) {
      return withDeps((editorDeps) => applyRoomSelectionModule(map, points, roomId, roomType, editorDeps));
    },
    applyCellTagRange(map, start, end, tag) {
      return withDeps((editorDeps) => applyCellTagRangeModule(map, start, end, tag, editorDeps));
    },
    applyCellTagSelection(map, points, tag) {
      return withDeps((editorDeps) => applyCellTagSelectionModule(map, points, tag, editorDeps));
    },
    applyBattleBgRange(map, start, end, battleBackgroundId) {
      return withDeps((editorDeps) => applyBattleBgRangeModule(map, start, end, battleBackgroundId, editorDeps));
    },
    applyBattleBgSelection(map, points, battleBackgroundId) {
      return withDeps((editorDeps) => applyBattleBgSelectionModule(map, points, battleBackgroundId, editorDeps));
    },
    grownSelectionPoints(points) {
      return withDeps((editorDeps) => grownSelectionPointsModule(points, editorDeps));
    },
    shrunkSelectionPoints(points) {
      return withDeps((editorDeps) => shrunkSelectionPointsModule(points, editorDeps));
    },
    invertedSelectionPoints(points) {
      return withDeps((editorDeps) => invertedSelectionPointsModule(points, editorDeps));
    },
    createEditorPlacement(tool, x, y) {
      return withDeps((editorDeps) => createEditorPlacementModule(tool, x, y, editorDeps));
    },
    canAutoPlaceRecommendationTool(map, roomId, tool) {
      return withDeps((editorDeps) => canAutoPlaceRecommendationToolModule(map, roomId, tool, editorDeps));
    },
    autoPlacementCandidateCells(map, roomId, selectedCellTag, tool) {
      return withDeps((editorDeps) => autoPlacementCandidateCellsModule(map, roomId, selectedCellTag, tool, editorDeps));
    },
    applyRecommendationAutoPlacement(roomId, placementRecommendations, selectedCellTag) {
      return withDeps((editorDeps) => applyRecommendationAutoPlacementModule(roomId, placementRecommendations, selectedCellTag, editorDeps));
    },
  };
}

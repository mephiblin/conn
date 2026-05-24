export function createInventoryOverlayBridge(deps = {}) {
  const {
    getState = () => ({}),
    closeInteraction = () => {},
    releasePointerLook = () => {},
    render = () => {},
  } = deps;

  let inventoryPreviewHoldTimer = null;

  function inventoryOverlayOpen(mode = getState().mode) {
    const state = getState();
    return ["town", "dungeon", "combat"].includes(mode)
      && mode === state.mode
      && Boolean(state.inventoryPanelOpen);
  }

  function clearInventoryPreviewHoldTimer() {
    if (!inventoryPreviewHoldTimer) return;
    clearTimeout(inventoryPreviewHoldTimer);
    inventoryPreviewHoldTimer = null;
  }

  function closeInventoryOverlay() {
    const state = getState();
    state.inventoryPanelOpen = false;
    state.inventoryPanelDragIndex = -1;
    state.inventoryPanelPreviewIndex = -1;
    clearInventoryPreviewHoldTimer();
  }

  function toggleInventoryOverlay(force) {
    const state = getState();
    if (!["town", "dungeon", "combat"].includes(state.mode)) return false;
    const next = typeof force === "boolean" ? force : !state.inventoryPanelOpen;
    if (next) closeInteraction();
    if (next) releasePointerLook();
    state.inventoryPanelOpen = next;
    if (!next) {
      state.inventoryPanelDragIndex = -1;
      state.inventoryPanelPreviewIndex = -1;
      clearInventoryPreviewHoldTimer();
    }
    return true;
  }

  function openInventoryPreview(index) {
    getState().inventoryPanelPreviewIndex = Number.isInteger(index) ? index : -1;
  }

  function closeInventoryPreview() {
    getState().inventoryPanelPreviewIndex = -1;
  }

  function scheduleInventoryPreviewHold(previewIndex) {
    inventoryPreviewHoldTimer = setTimeout(() => {
      inventoryPreviewHoldTimer = null;
      openInventoryPreview(previewIndex);
      render();
    }, 420);
  }

  return {
    inventoryOverlayOpen,
    clearInventoryPreviewHoldTimer,
    closeInventoryOverlay,
    toggleInventoryOverlay,
    openInventoryPreview,
    closeInventoryPreview,
    scheduleInventoryPreviewHold,
  };
}

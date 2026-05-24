export function bindGlobalUiControls(deps = {}) {
  const {
    documentObject = document,
    setProductEntry = () => {},
    setMode = () => {},
    performPlayerAction = () => {},
    saveGame = () => {},
    loadGame = () => {},
  } = deps;

  documentObject.querySelectorAll("[data-mode]").forEach((button) => {
    button.addEventListener("click", () => setMode(button.dataset.mode));
  });
  documentObject.addEventListener("click", (event) => {
    const button = event.target?.closest?.("[data-action]");
    if (!button || !documentObject.contains(button)) return;
    performPlayerAction(button.dataset.action);
  });
  documentObject.getElementById("saveBtn").addEventListener("click", () => saveGame());
  documentObject.getElementById("loadBtn").addEventListener("click", () => loadGame());
}

export function registerAppDebugHarness(deps = {}) {
  const {
    windowObject,
    registerDebugHarness = () => {},
    createDebugHarness = () => ({}),
    getState = () => ({}),
    dirs = [],
    vec = {},
    encounters = {},
    monsters = {},
    getCell = () => null,
    blocks = () => false,
    render = () => {},
    addLog = () => {},
    normalizeInventoryList = (value) => value || [],
    normalizeQuestState = (value) => value,
    updateBoardQuestCompletion = () => ({ completed: false }),
    resolveStairsOutcome = () => null,
    completeFinalEnding = () => {},
    activateFloor = () => {},
    activateTownState = () => {},
    getPointerLookState = () => null,
  } = deps;

  if (!windowObject) return;
  registerDebugHarness(windowObject, createDebugHarness({
    getState,
    dirs,
    vec,
    encounters,
    monsters,
    getCell,
    blocks,
    render,
    addLog,
    normalizeInventoryList,
    normalizeQuestState,
    updateBoardQuestCompletion,
    resolveStairsOutcome,
    completeFinalEnding,
    activateFloor,
    activateTownState,
    getPointerLookState,
  }));
}

export function bootstrapPlayerRuntimeControls(deps = {}) {
  const {
    createPlayerActionRunner = () => ({ performAction: () => {} }),
    createPlayerController = () => ({ destroy() {} }),
    getState = () => ({}),
    dirs = [],
    getPointerLookState = () => null,
    inventoryOverlayOpen = () => false,
    toggleInventoryOverlay = () => false,
    closeInventoryOverlay = () => {},
    closeInteraction = () => {},
    interact = () => {},
    rest = () => {},
    toggleDragLook = () => false,
    spendTorch = () => {},
    addLog = () => {},
    afterMove = () => {},
    render = () => {},
    runEventPlacement = () => {},
    allowedInteractionsForPlacementKind = () => [],
    activeMovementBlockersAt = () => [],
    blocks = () => false,
    bindGlobalUiControls = () => {},
    documentObject = document,
    setProductEntry = () => {},
    setMode = () => {},
    saveGame = () => {},
    loadGame = () => {},
    target = null,
    isEditableTarget = () => false,
  } = deps;

  const playerActions = createPlayerActionRunner({
    getState,
    dirs,
    getPointerLookState,
    inventoryOverlayOpen,
    toggleInventoryOverlay,
    closeInventoryOverlay,
    closeInteraction,
    interact,
    rest,
    toggleDragLook,
    spendTorch,
    addLog,
    afterMove,
    render,
    runEventPlacement,
    allowedInteractionsForPlacementKind,
    activeMovementBlockersAt,
    blocks,
  });

  bindGlobalUiControls({
    documentObject,
    setProductEntry,
    setMode,
    performPlayerAction: (action) => playerActions.performAction(action),
    saveGame,
    loadGame,
  });

  const playerController = createPlayerController({
    target,
    isEditableTarget,
    onAction: (action, event) => playerActions.performAction(action, event),
    onMoveInput: (input) => playerActions.applyMoveInput(input),
  });

  return {
    playerActions,
    playerController,
  };
}

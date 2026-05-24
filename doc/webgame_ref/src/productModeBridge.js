export function createProductModeBridge(deps = {}) {
  const {
    getState = () => ({}),
    setState = () => {},
    render = () => {},
    addLog = () => {},
    ensureEditorState = () => {},
    currentSaveSlotId = () => "slot_1",
    initialState = () => ({}),
    createValidatedRuntimeFloorMaps = () => ({}),
    buildProjectValidationReport = () => ({ summary: { error: 0, warning: 0, info: 0 } }),
    compileProjectForRuntime = () => ({ ok: true }),
    hasValidationErrors = () => false,
    setModeFallback = () => {},
    closeInteraction = () => {},
    closeInventoryOverlay = () => {},
    releasePointerLook = () => {},
    hasSavedEditorProject = () => false,
    loadEditorProject = () => {},
    endTestPlaySession = () => {},
  } = deps;

  function setMode(mode) {
    const state = getState();
    if (state.runtimeSession?.kind === "test_play" && mode !== "dungeon" && mode !== "combat") {
      endTestPlaySession(mode);
      return render();
    }
    if (mode === "editor") ensureEditorState();
    state.mode = mode;
    if (mode !== "dungeon") closeInteraction();
    if (mode !== "dungeon") closeInventoryOverlay();
    if (mode !== "dungeon") releasePointerLook();
    if (mode === "title") state.shell.titlePanel = "menu";
    render();
  }

  function activeProductEntry() {
    const state = getState();
    return state.mode === "editor" ? "editor" : "game";
  }

  function currentModeStatusText() {
    const state = getState();
    if (state.mode === "title") {
      if (state.shell.titlePanel === "creator") return "Game · New Game 준비";
      if (state.shell.titlePanel === "continue") return "Game · Continue 슬롯 선택";
      if (state.shell.titlePanel === "editor") return "Editor · Project Gateway";
      return "Title · Entry Shell";
    }
    if (state.mode === "editor") {
      return state.editor?.editorWorkspaceMode === "generator_workbench"
        ? "Editor · Generator Workbench"
        : "Editor · Legacy Cell Workspace";
    }
    if (state.runtimeSession?.kind === "test_play") {
      if (state.mode === "combat") return "Test Play · Combat Runtime";
      if (state.mode === "dungeon") return "Test Play · Dungeon Runtime";
    }
    if (state.mode === "town") return "Game · Town Runtime";
    if (state.mode === "combat") return "Game · Combat Runtime";
    if (state.mode === "dungeon") return "Game · Dungeon Runtime";
    return "Game";
  }

  function createFreshEditorWorkspace() {
    const state = getState();
    const selectedSaveSlotId = currentSaveSlotId();
    const next = initialState();
    next.floorMaps = createValidatedRuntimeFloorMaps(next.presetCatalog);
    for (let attempt = 0; attempt < 64; attempt += 1) {
      const candidateFloorMaps = createValidatedRuntimeFloorMaps(next.presetCatalog);
      const projectReport = buildProjectValidationReport(candidateFloorMaps);
      const compiledProject = compileProjectForRuntime(candidateFloorMaps);
      next.floorMaps = candidateFloorMaps;
      if (!hasValidationErrors(projectReport) && compiledProject.ok) break;
    }
    next.map = next.floorMaps[1];
    next.player = {
      floor: 1,
      x: next.map.start.x,
      y: next.map.start.y,
      facing: next.map.start.facing,
    };
    next.visitedByFloor = { 1: new Set([`${next.map.start.x},${next.map.start.y}`]), 2: new Set(), 3: new Set() };
    next.visited = next.visitedByFloor[1];
    next.shell.selectedSaveSlotId = selectedSaveSlotId;
    setState(next);
    ensureEditorState();
    const nextState = getState();
    nextState.editor.editorWorkspaceMode = "generator_workbench";
    nextState.shell.titlePanel = "editor";
    addLog("새 editor project workspace를 만들고 generator workbench를 기본 진입 경로로 연다. game save slot과는 분리된 임시 authoring 상태다.");
    setModeFallback("editor");
  }

  function openSavedEditorWorkspace() {
    if (!hasSavedEditorProject()) {
      addLog("저장된 editor project가 없다. 새 project를 만들거나 workspace에서 프로젝트 저장을 먼저 사용한다.");
      return render();
    }
    loadEditorProject();
    setModeFallback("editor");
  }

  function setProductEntry(entry) {
    const state = getState();
    if (entry === "editor") {
      if (state.runtimeSession?.kind === "test_play") {
        endTestPlaySession("editor");
        return render();
      }
      if (state.mode !== "title") {
        state.mode = "title";
        closeInteraction();
        closeInventoryOverlay();
        releasePointerLook();
      }
      state.shell.titlePanel = "editor";
      return render();
    }
    if (state.mode === "editor") return setModeFallback("title");
    render();
  }

  return {
    setMode,
    activeProductEntry,
    currentModeStatusText,
    createFreshEditorWorkspace,
    openSavedEditorWorkspace,
    setProductEntry,
  };
}

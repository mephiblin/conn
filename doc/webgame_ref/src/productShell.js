export function selectedBackgroundForDraft(draft = {}, protagonistBackgrounds = []) {
  return protagonistBackgrounds.find((entry) => entry.id === draft.backgroundId) || protagonistBackgrounds[0];
}

export function selectedLoadoutForDraft(draft = {}, starterLoadouts = []) {
  return starterLoadouts.find((entry) => entry.id === draft.loadoutId) || starterLoadouts[0];
}

export function createProductShell(deps = {}) {
  const getState = deps.getState;
  const setState = deps.setState;
  const render = deps.render;
  const addLog = deps.addLog;
  const initialState = deps.initialState;
  const normalizePartyModel = deps.normalizePartyModel;
  const makeHero = deps.makeHero;
  const currentSaveSlotId = deps.currentSaveSlotId;
  const setMode = deps.setMode;
  const activateTownState = deps.activateTownState || ((nextState) => nextState);
  const openSavedEditorWorkspace = deps.openSavedEditorWorkspace;
  const createFreshEditorWorkspace = deps.createFreshEditorWorkspace;
  const loadGame = deps.loadGame;
  const renameSaveSlot = deps.renameSaveSlot;
  const deleteSaveSlot = deps.deleteSaveSlot;
  const classes = deps.classes || [];
  const protagonistBackgrounds = deps.protagonistBackgrounds || [];
  const starterLoadouts = deps.starterLoadouts || [];
  const documentObject = deps.documentObject || document;

  function state() {
    return getState();
  }

  function writeState(nextState) {
    setState(nextState);
  }

  function handleTitleAction(action) {
    if (action === "new-game") {
      state().shell.titlePanel = "creator";
      return render();
    }
    if (action === "continue") {
      state().shell.titlePanel = "continue";
      return render();
    }
    if (action === "back-menu") {
      state().shell.titlePanel = "menu";
      return render();
    }
    if (action === "editor") {
      state().shell.titlePanel = "editor";
      return render();
    }
    if (action === "open-editor-workspace") return setMode("editor");
    if (action === "load-editor-project") return openSavedEditorWorkspace();
    if (action === "create-editor-project") return createFreshEditorWorkspace();
    if (action === "load-slot") return loadGame(currentSaveSlotId());
    if (action === "rename-slot") {
      const slotAliasInput = documentObject.getElementById("slotAliasInput");
      return renameSaveSlot(currentSaveSlotId(), slotAliasInput?.value || "");
    }
    if (action === "delete-slot") return deleteSaveSlot(currentSaveSlotId());
    if (action === "start-run") {
      const draft = state().shell.newGameDraft || { name: "코난", classIndex: 0 };
      const background = selectedBackgroundForDraft(draft, protagonistBackgrounds);
      const loadout = selectedLoadoutForDraft(draft, starterLoadouts);
      const next = initialState();
      ({ party: next.party, companion: next.companion } = normalizePartyModel([
        makeHero(0, Number(draft.classIndex || 0), (draft.name || "").trim() || "코난"),
      ], null));
      next.flags.protagonistBackground = background.id;
      next.flags.protagonistBackgroundLabel = background.label;
      next.resources = JSON.parse(JSON.stringify(loadout.resources));
      next.inventory = [...(loadout.inventory || [])];
      next.quest.main = `${next.quest.main} ${background.questNote}`;
      next.shell.selectedSaveSlotId = currentSaveSlotId();
      next.shell.newGameDraft = { ...draft };
      activateTownState(next);
      writeState(next);
      addLog(background.log);
      addLog(`${next.party[0].name}이(가) ${classes[next.party[0].classIndex]?.cls || "모험가"}로 원정을 시작한다. ${loadout.label}을 챙겼다.`);
      return render();
    }
  }

  return {
    handleTitleAction,
    selectedBackgroundForDraft: (draft) => selectedBackgroundForDraft(draft, protagonistBackgrounds),
    selectedLoadoutForDraft: (draft) => selectedLoadoutForDraft(draft, starterLoadouts),
  };
}

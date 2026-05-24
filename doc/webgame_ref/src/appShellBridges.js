export function createAppShellBridges(deps = {}) {
  const {
    getState = () => deps.state || {},
    saveSlotIds = [],
    saveSlotStoragePrefix = "",
    currentContentVersion = "",
    classes = [],
    questEndingComplete = () => false,
    ensureProductShellManager = () => ({
      handleTitleAction: () => {},
      selectedBackgroundForDraft: () => null,
      selectedLoadoutForDraft: () => null,
    }),
    ensureSaveSlotManager = () => ({
      readSaveSlotData: () => null,
      readSaveSlots: () => [],
      renameSaveSlot: () => false,
      saveGame: () => false,
      deleteSaveSlot: () => false,
      loadGame: () => false,
    }),
    saveSlotStorageKeyModule = () => "",
    saveSlotLabelModule = () => "",
    saveUsesEmbeddedContentDefinitionsModule = () => false,
    saveContentVersionMatchesCurrentModule = () => false,
    formatPlaytimeLabelModule = () => "",
    buildRecentStatusLabelModule = () => "",
    summarizeSaveDataModule = () => ({}),
    parseSaveSlotPayloadModule = () => null,
  } = deps;

  const currentSaveSlotId = () => getState().shell?.selectedSaveSlotId || saveSlotIds[0];

  return {
    handleTitleAction(action) {
      return ensureProductShellManager().handleTitleAction(action);
    },
    saveSlotStorageKey(slotId) {
      return saveSlotStorageKeyModule(slotId, saveSlotStoragePrefix);
    },
    saveSlotLabel(slotId) {
      return saveSlotLabelModule(slotId);
    },
    saveUsesEmbeddedContentDefinitions(data) {
      return saveUsesEmbeddedContentDefinitionsModule(data);
    },
    saveContentVersionMatchesCurrent(data) {
      return saveContentVersionMatchesCurrentModule(data, currentContentVersion);
    },
    formatPlaytimeLabel(playtimeMs = 0) {
      return formatPlaytimeLabelModule(playtimeMs);
    },
    buildRecentStatusLabel(data = {}) {
      return buildRecentStatusLabelModule(data, { questEndingComplete });
    },
    selectedBackgroundForDraft(draft = {}) {
      return ensureProductShellManager().selectedBackgroundForDraft(draft);
    },
    selectedLoadoutForDraft(draft = {}) {
      return ensureProductShellManager().selectedLoadoutForDraft(draft);
    },
    summarizeSaveData(slotId, data) {
      return summarizeSaveDataModule(slotId, data, { classes, questEndingComplete });
    },
    readSaveSlotData(slotId) {
      return ensureSaveSlotManager().readSaveSlotData(slotId);
    },
    readSaveSlots() {
      return ensureSaveSlotManager().readSaveSlots();
    },
    currentSaveSlotId,
    renameSaveSlot(slotId = currentSaveSlotId(), nextName = "") {
      return ensureSaveSlotManager().renameSaveSlot(slotId, nextName);
    },
    saveGame(slotId = currentSaveSlotId(), options = {}) {
      return ensureSaveSlotManager().saveGame(slotId, options);
    },
    deleteSaveSlot(slotId = currentSaveSlotId()) {
      return ensureSaveSlotManager().deleteSaveSlot(slotId);
    },
    parseSaveSlotPayload(raw, slotId) {
      return parseSaveSlotPayloadModule(raw, slotId);
    },
    loadGame(slotId = currentSaveSlotId()) {
      return ensureSaveSlotManager().loadGame(slotId);
    },
  };
}

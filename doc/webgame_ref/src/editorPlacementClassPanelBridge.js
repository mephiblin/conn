export function createEditorPlacementClassPanelBridge(deps = {}) {
  const {
    compatibleEventDefinitions = () => [],
    renderEditorPlacementOverridePanel = () => "",
    renderEditorClassProgressionPanel = () => "",
    validationSummaryText = () => "",
    escapeHtml = (value) => String(value ?? ""),
    classMilestonesJson = () => "",
    classes = [],
  } = deps;

  return function buildEditorPlacementClassPanels({
    selectedPlacement,
    selectedPlacementEvent,
    selectedPlacementRequired,
    cursorEventPlacements = [],
    classDef,
    classDefIndex,
    state,
  } = {}) {
    const placementOverridePanelMarkup = renderEditorPlacementOverridePanel({
      subtitle: selectedPlacement ? `${selectedPlacement.id} · ${selectedPlacement.kind}` : `${state.editorCursor.x},${state.editorCursor.y} cursor`,
      bodyMarkup: selectedPlacement && selectedPlacementEvent ? `
            <div class="preset-inspector">
              <div class="preset-field">
                <label for="placementOverrideTargetSelect">Cursor placement</label>
                <select id="placementOverrideTargetSelect">${cursorEventPlacements.map((placement) => `<option value="${placement.id}" ${placement.id === selectedPlacement.id ? "selected" : ""}>${placement.id} · ${placement.kind}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="placementEventPresetSelect">Placement event preset</label>
                <select id="placementEventPresetSelect">${compatibleEventDefinitions(selectedPlacement.kind).map(([id, def]) => `<option value="${id}" ${id === (selectedPlacement.interaction?.eventId || selectedPlacement.refId) ? "selected" : ""}>${id} · ${def.name}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="placementUsageModeOverrideSelect">Usage override</label>
                <select id="placementUsageModeOverrideSelect">
                  <option value="" ${selectedPlacement.eventOverrides?.usage?.mode ? "" : "selected"}>(inherit)</option>
                  ${["repeat", "uses", "cooldown"].map((mode) => `<option value="${mode}" ${selectedPlacement.eventOverrides?.usage?.mode === mode ? "selected" : ""}>${mode}</option>`).join("")}
                </select>
              </div>
              <div class="preset-field">
                <label for="placementUsesOverrideInput">Uses override</label>
                <input id="placementUsesOverrideInput" type="number" min="0" value="${selectedPlacement.eventOverrides?.usage?.usesRemaining ?? ""}" placeholder="inherit" />
              </div>
              <div class="preset-field">
                <label for="placementCooldownOverrideInput">Cooldown override</label>
                <input id="placementCooldownOverrideInput" type="number" min="0" value="${selectedPlacement.eventOverrides?.usage?.cooldownSteps ?? ""}" placeholder="inherit" />
              </div>
              ${selectedPlacement.kind === "trap" ? `
                <div class="preset-field">
                  <label for="placementDetectionDifficultyOverrideInput">Detection diff override</label>
                  <input id="placementDetectionDifficultyOverrideInput" type="number" min="0" value="${selectedPlacement.eventOverrides?.detection?.difficulty ?? ""}" placeholder="inherit" />
                </div>
                <div class="preset-field">
                  <label for="placementDisarmDifficultyOverrideInput">Disarm diff override</label>
                  <input id="placementDisarmDifficultyOverrideInput" type="number" min="0" value="${selectedPlacement.eventOverrides?.disarm?.difficulty ?? ""}" placeholder="inherit" />
                </div>
              ` : ""}
              <div class="preset-toolbar">
                <button id="clearPlacementOverrideBtn">Override 비우기</button>
                <button id="promoteOverrideToPresetBtn">Override를 프리셋으로 승격</button>
                <button id="toggleRequiredEventPlacementBtn">${selectedPlacementRequired ? "필수 event 해제" : "필수 event 표시"}</button>
              </div>
              <div class="muted">resolved usage ${selectedPlacementEvent.usage?.mode || "repeat"} · cooldown ${selectedPlacementEvent.usage?.cooldownSteps ?? 0} · runtime cooldown ${selectedPlacement.eventRuntime?.cooldownRemaining ?? 0}</div>
              <div class="muted">required event target ${selectedPlacementRequired ? "예" : "아니오"}</div>
            </div>
          ` : `<div class="preset-inspector"><div class="muted">커서 셀에 event placement가 없다. event/trap/shrine/rest/camp 칸을 클릭하면 override를 편집할 수 있다.</div></div>`,
    });

    const classProgressionPanelMarkup = renderEditorClassProgressionPanel({
      subtitle: classDef ? `${classDef.cls} · milestone ${classDef.progression?.milestones?.length || 0}개` : "class 없음",
      bodyMarkup: classDef ? `
            <div class="preset-inspector">
              <div class="preset-field">
                <label for="classDefinitionSelect">Class</label>
                <select id="classDefinitionSelect">${classes.map((entry, index) => `<option value="${index}" ${index === classDefIndex ? "selected" : ""}>${index} · ${entry.cls}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="classNameInput">Class name</label>
                <input id="classNameInput" value="${classDef.cls || ""}" />
              </div>
              <div class="preset-field">
                <label for="classMilestonesJsonInput">Milestones JSON</label>
                <textarea id="classMilestonesJsonInput" rows="10" spellcheck="false">${escapeHtml(classMilestonesJson(classDef))}</textarea>
              </div>
              <div class="muted">category ${classDef.category} · base HP ${classDef.hp} · ATK ${classDef.atk} · DEF ${classDef.def}</div>
            </div>
          ` : `<div class="preset-inspector"><div class="muted">선택된 class definition을 찾지 못했다.</div></div>`,
    });

    return {
      placementOverridePanelMarkup,
      classProgressionPanelMarkup,
    };
  };
}

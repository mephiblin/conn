export function createEditorNpcSupportPanelBridge(deps = {}) {
  const {
    npcs = {},
    renderEditorNpcProgressionHooksPanel = () => "",
    renderEditorNpcPlacementPanel = () => "",
    renderEditorPresetStudioPanel = () => "",
    npcHookJson = () => "",
    npcServicePreviewList = () => [],
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  return function buildEditorNpcSupportPanels({
    npcDefId,
    npcDef,
    npcQuestSeed,
    cursorNpcPlacements = [],
    selectedNpcPlacement,
    selectedNpcPlacementDef,
    selectedNpcPlacementRequired,
    state,
    customPresets = [],
    compiled = { ok: false },
  } = {}) {
    const npcProgressionHooksPanelMarkup = renderEditorNpcProgressionHooksPanel({
      subtitle: npcDef ? `${npcDefId} · ${npcDef.name}` : "npc 없음",
      bodyMarkup: npcDef ? `
            <div class="preset-inspector">
              <div class="preset-field">
                <label for="npcDefinitionSelect">NPC</label>
                <select id="npcDefinitionSelect">${Object.entries(npcs).map(([id, npc]) => `<option value="${id}" ${id === npcDefId ? "selected" : ""}>${id} · ${npc.name}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="npcNameInput">NPC name</label>
                <input id="npcNameInput" value="${npcDef.name || ""}" />
              </div>
              <div class="preset-field">
                <label for="npcDescriptionInput">Description</label>
                <textarea id="npcDescriptionInput" rows="3" spellcheck="false">${escapeHtml(npcDef.description || "")}</textarea>
              </div>
              <div class="preset-field">
                <label for="npcLogInput">Visit log</label>
                <textarea id="npcLogInput" rows="3" spellcheck="false">${escapeHtml(npcDef.log || "")}</textarea>
              </div>
              <div class="preset-field">
                <label for="npcProgressionHooksJsonInput">Progression hooks JSON</label>
                <textarea id="npcProgressionHooksJsonInput" rows="6" spellcheck="false">${escapeHtml(npcHookJson(npcDef, "progressionHooks", {}))}</textarea>
              </div>
              <div class="preset-field">
                <label for="npcQuestHooksJsonInput">Quest hooks JSON</label>
                <textarea id="npcQuestHooksJsonInput" rows="6" spellcheck="false">${escapeHtml(npcHookJson(npcDef, "questHooks", []))}</textarea>
              </div>
              <div class="preset-field">
                <label>Quest hook fields</label>
                <div class="preset-stack">
                  ${(npcDef.questHooks || []).map((hook, index) => `
                    <div class="preset-toolbar">
                      <input data-npc-quest-hook-bosses="${index}" type="number" min="0" value="${Math.max(0, Number(hook?.bossesDefeatedAtLeast || 0))}" />
                      <input data-npc-quest-hook-note="${index}" value="${escapeHtml(hook?.note || "")}" placeholder="hook note" />
                      <button data-remove-npc-quest-hook="${index}">삭제</button>
                    </div>
                  `).join("") || `<div class="muted">quest hook 없음</div>`}
                  <button id="addNpcQuestHookBtn">quest hook 추가</button>
                </div>
              </div>
              <div class="muted">quest seed ${Array.isArray(npcDef.questSeeds) ? npcDef.questSeeds.length : 0}개 · guided 편집은 Quest Seed Editor panel에서 수행한다.</div>
              <div class="muted">active seed ${npcQuestSeed?.title || "없음"}${npcQuestSeed?.grantFlag ? ` · grant ${npcQuestSeed.grantFlag}` : ""}</div>
            </div>
          ` : `<div class="preset-inspector"><div class="muted">선택된 NPC definition을 찾지 못했다.</div></div>`,
    });

    const npcPlacementPanelMarkup = renderEditorNpcPlacementPanel({
      subtitle: selectedNpcPlacement ? `${selectedNpcPlacement.id} · ${selectedNpcPlacement.npcId || "(empty)"}` : `${state.editorCursor.x},${state.editorCursor.y} cursor`,
      bodyMarkup: selectedNpcPlacement ? `
            <div class="preset-inspector">
              <div class="preset-field">
                <label for="npcPlacementSelect">Cursor NPC placement</label>
                <select id="npcPlacementSelect">${cursorNpcPlacements.map((placement) => `<option value="${placement.id}" ${placement.id === selectedNpcPlacement.id ? "selected" : ""}>${placement.id}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="npcPlacementDefinitionSelect">NPC definition</label>
                <select id="npcPlacementDefinitionSelect">${Object.entries(npcs).map(([id, npc]) => `<option value="${id}" ${id === (selectedNpcPlacement.npcId || selectedNpcPlacement.refId) ? "selected" : ""}>${id} · ${npc.name}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="npcPlacementNoteInput">Placement note</label>
                <textarea id="npcPlacementNoteInput" rows="4" spellcheck="false">${escapeHtml(selectedNpcPlacement.note || "")}</textarea>
              </div>
              <div class="preset-toolbar">
                <button id="clearNpcPlacementNoteBtn">Placement note 비우기</button>
                <button id="toggleRequiredNpcPlacementBtn">${selectedNpcPlacementRequired ? "필수 NPC 해제" : "필수 NPC 표시"}</button>
              </div>
              <div class="muted">${selectedNpcPlacementDef ? `${selectedNpcPlacementDef.description || ""}${selectedNpcPlacementDef.log ? ` · ${selectedNpcPlacementDef.log}` : ""}` : "연결된 NPC definition이 없다."}</div>
              <div class="muted">required npc target ${selectedNpcPlacementRequired ? "예" : "아니오"}</div>
              ${selectedNpcPlacementDef ? `<div class="muted">${npcServicePreviewList(selectedNpcPlacementDef, selectedNpcPlacement).map((entry) => `${escapeHtml(entry.label)}: ${escapeHtml(entry.summary)}`).join("<br />")}</div>` : ""}
            </div>
          ` : `<div class="preset-inspector"><div class="muted">커서 셀에 npc placement가 없다. npc 도구로 authored NPC anchor를 찍을 수 있다.</div></div>`,
    });

    const presetStudioPanelMarkup = renderEditorPresetStudioPanel({
      presetName: state.presetDraft.name || "",
      presetId: state.presetDraft.id || "",
      presetTags: (state.presetDraft.tags || []).join(", "),
      deleteDisabled: !state.presetDraftSelectedId,
      customPresetCount: customPresets.length,
      compiledOk: compiled.ok,
    });

    return {
      npcProgressionHooksPanelMarkup,
      npcPlacementPanelMarkup,
      presetStudioPanelMarkup,
    };
  };
}

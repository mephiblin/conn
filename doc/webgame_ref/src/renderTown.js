export function renderTownFrame(deps = {}) {
  const {
    state,
    inventoryOverlayOpen = () => false,
    inventoryOverlayMarkup = () => "",
    items = {},
    normalizeInventoryList = (value) => value || [],
    inventoryEntryEquipmentSlot = () => "",
    compareCandidateSummary = () => "",
    escapeHtml = (value) => String(value ?? ""),
    inventoryFilterOptions = () => [],
    inventorySortOptions = () => [],
    inventorySummaryText = () => "",
    heroEquipmentEntries = () => ({}),
    inventoryEntryLabel = () => "",
    inventoryEntryDetailText = () => "",
    inventoryEntryKindLabel = () => "",
    sortedFilteredInventoryEntries = () => [],
    inventoryManualReorderEnabled = () => false,
    inventoryEntryItemId = () => "",
    inventoryEntryOverlayMarkupImpl = () => "",
    classes = [],
  } = deps;

  const controlsMarkup = `
    <div class="town-runtime-hud">
      <div class="town-runtime-title">
        <strong>카라쉬 전초기지</strong>
        <span class="muted">시설 담당자를 직접 찾아 원정을 준비한다.</span>
      </div>
      <div class="town-runtime-toolbar">
        <button id="openTownInventoryBtn">가방</button>
        <button data-action="toggleDragLook">시점</button>
      </div>
    </div>
    <div class="interaction-panel town-runtime-panel">
      <details class="town-party-details">
        <summary>파티 편성</summary>
        <div class="town-party-grid">
          ${state.party.map((p, i) => `
            <div class="place">
              <label>이름 <input id="partyName${i}" value="${escapeHtml(p.name)}" maxlength="12" /></label>
              <label>클래스
                <select id="partyClass${i}" ${p.isCompanion ? "disabled" : ""}>
                  ${classes.map((c, idx) => `<option value="${idx}" ${p.classIndex === idx ? "selected" : ""}>${escapeHtml(c.cls)}</option>`).join("")}
                </select>
              </label>
              ${p.isCompanion ? `<p class="muted">던전 영입 동료는 여기서 클래스 변경을 하지 않는다.</p>` : ""}
            </div>
          `).join("")}
        </div>
        <div class="town-party-actions">
          <button id="applyPartyBtn">파티 적용</button>
        </div>
      </details>
    </div>`;

  const inventoryMarkup = inventoryOverlayOpen("town") ? inventoryOverlayMarkup({
    state,
    items,
    inventoryOverlayOpen: () => inventoryOverlayOpen("town"),
    sortedFilteredInventoryEntries,
    inventoryManualReorderEnabled,
    normalizeInventoryList,
    inventoryEntryEquipmentSlot,
    compareCandidateSummary,
    escapeHtml,
    inventoryFilterOptions,
    inventorySortOptions,
    inventorySummaryText,
    heroEquipmentEntries,
    inventoryEntryLabel,
    inventoryEntryDetailText,
    inventoryEntryKindLabel,
    inventoryEntryOverlayMarkupImpl: ({ entry, index }) => inventoryEntryOverlayMarkupImpl({
      entry,
      index,
      state,
      items,
      escapeHtml,
      inventoryEntryEquipmentSlot,
      inventoryEntryItemId,
      inventoryEntryDetailText,
      inventoryManualReorderEnabled,
      normalizeInventoryList,
      compareCandidateSummary,
      inventoryEntryLabel,
      inventoryEntryKindLabel,
    }),
  }) : "";

  return {
    controlsMarkup,
    inventoryMarkup,
    overlayMarkup: `
    <div class="town-runtime-shell">
      <div class="town-runtime-screen">
        <div id="townView"></div>
        <div id="townViewOverlay" class="view-overlay"></div>
        <div id="townRuntimeOverlay" class="town-runtime-overlay">${controlsMarkup}</div>
      </div>
      <div id="townInventoryOverlayHost">${inventoryMarkup}</div>
    </div>`,
  };
}

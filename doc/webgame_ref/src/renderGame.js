import { activeSkillDeckHero, renderSkillDeckOverlay } from "./renderSkillDeck.js";
import { renderSkillShopOverlay } from "./renderSkillShop.js";
import { heroSkillLibraryIds, skillInventoryCount } from "./diceSkillLoadout.js";

function preferredSkillSelectionId(hero = {}) {
  const skillIds = heroSkillLibraryIds(hero);
  return skillIds.find((skillId) => skillInventoryCount(hero, skillId) > 0) || "";
}

export function renderGameFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    activeProductEntry = () => "game",
    setProductEntry = () => {},
    currentModeStatusText = () => "",
    renderTitle = () => {},
    renderParty = () => {},
    renderResources = () => {},
    renderQuest = () => {},
    renderLog = () => {},
    renderMiniMap = () => {},
    renderTown = () => {},
    renderCombat = () => {},
    renderEditor = () => {},
    drawView = () => {},
    equipInventoryEntryToHero = () => false,
    closeInventoryOverlay = () => {},
    closeInventoryPreview = () => {},
    inventoryManualReorderEnabled = () => false,
    reorderInventoryEntries = () => false,
    clearInventoryPreviewHoldTimer = () => {},
    scheduleInventoryPreviewHold = () => {},
    openInventoryPreview = () => {},
    useInventoryEntryOnHero = () => false,
    normalizeInventoryList = (value) => value || [],
    assignHeroSkillToDieFace = () => false,
    buySkillCard = () => false,
    sellSkillCard = () => false,
    render = () => {},
    boardQuestCanReturn = () => false,
    returnFromBoardQuest = () => false,
  } = deps;

  documentObject.body.classList.toggle("editor-mode", state.mode === "editor");
  documentObject.body.classList.toggle("title-mode", state.mode === "title");
  documentObject.querySelectorAll(".screen").forEach((el) => el.classList.toggle("active", el.id === state.mode));
  documentObject.querySelectorAll(".mode-tabs button[data-mode]").forEach((b) => b.classList.toggle("active", b.dataset.mode === state.mode));
  documentObject.querySelectorAll(".mode-tabs button[data-entry]").forEach((b) => b.classList.toggle("active", b.dataset.entry === activeProductEntry()));
  documentObject.querySelectorAll(".mode-tabs button[data-entry]").forEach((button) => {
    button.onclick = () => setProductEntry(button.dataset.entry);
  });
  const runtimeGroup = documentObject.querySelector(".runtime-group");
  const utilityGroup = documentObject.querySelector(".utility-group");
  const modeStatusLabel = documentObject.getElementById("modeStatusLabel");
  if (modeStatusLabel) modeStatusLabel.textContent = currentModeStatusText();
  if (runtimeGroup) runtimeGroup.hidden = true;
  if (utilityGroup) utilityGroup.hidden = state.mode === "editor" || state.mode === "title";

  renderTitle();
  renderParty();
  renderResources();
  renderQuest();
  renderLog();
  renderMiniMap();
  if (state.mode === "town") renderTown();
  if (state.mode === "combat") renderCombat();
  if (state.mode === "editor") renderEditor();
  if (state.mode === "dungeon") drawView();

  documentObject.querySelectorAll("[data-equip-hero]").forEach((button) => {
    button.onclick = () => {
      const heroId = button.dataset.equipHero;
      const heroIndex = state.party.findIndex((hero) => hero.id === heroId);
      const inventoryIndex = Number(button.dataset.equipIndex || -1);
      if (equipInventoryEntryToHero(heroIndex, inventoryIndex)) render();
    };
  });
  documentObject.querySelectorAll("[data-skill-deck-open]").forEach((button) => {
    button.onclick = () => {
      const heroId = button.dataset.skillDeckOpen || "";
      state.skillShopOpen = false;
      state.skillDeckOpen = true;
      state.skillDeckHeroId = heroId || state.party?.[0]?.id || "";
      state.skillDeckSelectedSkillId = preferredSkillSelectionId(activeSkillDeckHero(state));
      state.skillDeckDieIndex = 0;
      render();
    };
  });
  documentObject.querySelectorAll("[data-skill-deck-close]").forEach((button) => {
    button.onclick = () => {
      state.skillDeckOpen = false;
      render();
    };
  });
  documentObject.querySelectorAll("[data-skill-deck-hero]").forEach((button) => {
    button.onclick = () => {
      state.skillDeckHeroId = button.dataset.skillDeckHero || state.party?.[0]?.id || "";
      state.skillDeckSelectedSkillId = preferredSkillSelectionId(activeSkillDeckHero(state));
      state.skillDeckDieIndex = 0;
      render();
    };
  });
  documentObject.querySelectorAll("[data-skill-die-tab]").forEach((button) => {
    button.onclick = () => {
      state.skillDeckDieIndex = Math.max(0, Number(button.dataset.skillDieTab || 0));
      render();
    };
  });
  documentObject.querySelectorAll("[data-skill-card]").forEach((button) => {
    button.ondragstart = (event) => {
      state.skillDeckSelectedSkillId = button.dataset.skillCard || "";
      if (event.dataTransfer) event.dataTransfer.setData("text/plain", state.skillDeckSelectedSkillId);
    };
    button.onclick = () => {
      const nextSkillId = button.dataset.skillCard || "";
      state.skillDeckSelectedSkillId = state.skillDeckSelectedSkillId === nextSkillId ? "" : nextSkillId;
      render();
    };
  });
  documentObject.querySelectorAll("[data-die-face]").forEach((button) => {
    button.ondragover = (event) => {
      event.preventDefault();
    };
    button.ondrop = (event) => {
      event.preventDefault();
      const hero = activeSkillDeckHero(state);
      if (!hero) return;
      if (assignHeroSkillToDieFace(hero.id, button.dataset.dieFace || "", Number(button.dataset.faceIndex || -1), event.dataTransfer?.getData("text/plain") || state.skillDeckSelectedSkillId)) render();
    };
    button.onclick = () => {
      const hero = activeSkillDeckHero(state);
      if (!hero) return;
      if (assignHeroSkillToDieFace(hero.id, button.dataset.dieFace || "", Number(button.dataset.faceIndex || -1), state.skillDeckSelectedSkillId || "")) render();
    };
  });
  documentObject.querySelectorAll("[data-skill-shop-close]").forEach((button) => {
    button.onclick = () => {
      state.skillShopOpen = false;
      state.skillShopNpcId = "";
      state.skillShopTitle = "";
      state.skillShopNote = "";
      state.skillShopCatalogId = "";
      state.skillShopSkillIds = [];
      render();
    };
  });
  documentObject.querySelectorAll("[data-skill-shop-hero]").forEach((button) => {
    button.onclick = () => {
      state.skillShopHeroId = button.dataset.skillShopHero || state.party?.[0]?.id || "";
      render();
    };
  });
  documentObject.querySelectorAll("[data-skill-shop-buy]").forEach((button) => {
    button.onclick = () => {
      if (buySkillCard(button.dataset.skillShopBuy || "")) render();
    };
  });
  documentObject.querySelectorAll("[data-skill-shop-sell]").forEach((button) => {
    button.onclick = () => {
      if (sellSkillCard(button.dataset.skillShopSell || "")) render();
    };
  });
  documentObject.querySelectorAll("[data-board-quest-return]").forEach((button) => {
    button.onclick = () => {
      if (returnFromBoardQuest()) render();
    };
  });
  documentObject.querySelectorAll("[data-inventory-close]").forEach((button) => {
    button.onclick = () => {
      closeInventoryOverlay();
      render();
    };
  });
  documentObject.querySelectorAll("[data-inventory-preview-close]").forEach((button) => {
    button.onclick = () => {
      closeInventoryPreview();
      render();
    };
  });
  if (documentObject.getElementById("inventoryPanelFilterSelect")) {
    documentObject.getElementById("inventoryPanelFilterSelect").onchange = (e) => {
      state.inventoryPanelFilter = e.target.value || "all";
      render();
    };
  }
  if (documentObject.getElementById("inventoryPanelQueryInput")) {
    documentObject.getElementById("inventoryPanelQueryInput").oninput = (e) => {
      state.inventoryPanelQuery = e.target.value || "";
      render();
    };
  }
  if (documentObject.getElementById("inventoryPanelSortSelect")) {
    documentObject.getElementById("inventoryPanelSortSelect").onchange = (e) => {
      state.inventoryPanelSort = e.target.value || "default";
      render();
    };
  }
  documentObject.querySelectorAll("[data-inventory-drag-index]").forEach((card) => {
    card.draggable = inventoryManualReorderEnabled();
    card.ondragstart = (e) => {
      if (!inventoryManualReorderEnabled()) {
        e.preventDefault();
        return;
      }
      const dragIndex = Number(card.dataset.inventoryDragIndex || -1);
      state.inventoryPanelDragIndex = dragIndex;
      if (e.dataTransfer) {
        e.dataTransfer.effectAllowed = "move";
        e.dataTransfer.setData("text/plain", String(dragIndex));
      }
    };
    card.ondragover = (e) => {
      if (!inventoryManualReorderEnabled()) return;
      e.preventDefault();
      if (e.dataTransfer) e.dataTransfer.dropEffect = "move";
    };
    card.ondrop = (e) => {
      if (!inventoryManualReorderEnabled()) return;
      e.preventDefault();
      const fromIndex = Number(e.dataTransfer?.getData("text/plain") || state.inventoryPanelDragIndex || -1);
      const toIndex = Number(card.dataset.inventoryDropIndex || -1);
      state.inventoryPanelDragIndex = -1;
      if (reorderInventoryEntries(fromIndex, toIndex)) render();
    };
    card.ondragend = () => {
      state.inventoryPanelDragIndex = -1;
    };
  });
  documentObject.querySelectorAll("[data-inventory-preview-index]").forEach((card) => {
    card.onpointerdown = (e) => {
      if (e.pointerType !== "touch" && e.pointerType !== "pen") return;
      const previewIndex = Number(card.getAttribute("data-inventory-preview-index") || -1);
      clearInventoryPreviewHoldTimer();
      scheduleInventoryPreviewHold(previewIndex);
    };
    card.onpointerup = clearInventoryPreviewHoldTimer;
    card.onpointerleave = clearInventoryPreviewHoldTimer;
    card.onpointercancel = clearInventoryPreviewHoldTimer;
  });
  documentObject.querySelectorAll("[data-use-hero]").forEach((button) => {
    button.onclick = () => {
      const heroIndex = Number(button.dataset.useHero || -1);
      const inventoryIndex = Number(button.dataset.useIndex || -1);
      if (useInventoryEntryOnHero(heroIndex, inventoryIndex)) render();
    };
  });
  documentObject.querySelectorAll("[data-inventory-move]").forEach((button) => {
    button.onclick = () => {
      if (!inventoryManualReorderEnabled()) return;
      const direction = button.getAttribute("data-inventory-move") || "";
      const index = Number(button.getAttribute("data-inventory-move-index") || -1);
      const nextIndex = direction === "up" ? index - 1 : direction === "down" ? index + 1 : index;
      if (reorderInventoryEntries(index, nextIndex)) render();
    };
  });
  return normalizeInventoryList(state.inventory);
}

export function interactionOverlayMarkup(deps = {}) {
  const {
    state,
    mode = "",
    interactiveModes = ["dungeon"],
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  if (!state?.interaction || !interactiveModes.includes(mode)) return "";
  return `
    <div class="interaction-panel">
      <div class="panel-title"><span>${escapeHtml(state.interaction.title || "상호작용")}</span><button data-interaction-close="1">닫기</button></div>
      ${state.interaction.text ? `<p class="muted">${escapeHtml(state.interaction.text)}</p>` : ""}
      ${Array.isArray(state.interaction.details) && state.interaction.details.length ? `
        <div class="muted">${state.interaction.details.map((entry) => `${escapeHtml(entry.label)}: ${escapeHtml(entry.summary)}`).join("<br />")}</div>
      ` : ""}
      <div class="actions">
        ${state.interaction.options.map((option, index) => `<button data-interaction-option="${index}">${escapeHtml(option.label)}</button>`).join("")}
      </div>
    </div>
  `;
}

export function bindInteractionOverlay(deps = {}) {
  const {
    state,
    documentObject = document,
    resolveEventChoice = () => {},
    resolveNpcService = () => {},
    closeInteraction = () => {},
    render = () => {},
  } = deps;

  documentObject.querySelectorAll("[data-interaction-option]").forEach((button) => {
    button.onclick = () => {
      const index = Number(button.dataset.interactionOption);
      if (state.interaction?.type === "event") resolveEventChoice(index);
      else if (state.interaction?.type === "npc") resolveNpcService(index);
      render();
    };
  });
  documentObject.querySelectorAll("[data-interaction-close]").forEach((button) => {
    button.onclick = () => {
      closeInteraction();
      render();
    };
  });
}

export function inventoryEntryOverlayMarkup(deps = {}) {
  const {
    entry,
    index,
    state,
    items = {},
    escapeHtml = (value) => String(value ?? ""),
    inventoryEntryEquipmentSlot = () => "",
    inventoryEntryItemId = () => "",
    inventoryEntryDetailText = () => "",
    inventoryManualReorderEnabled = () => false,
    normalizeInventoryList = (value) => value || [],
    compareCandidateSummary = () => "",
    inventoryEntryLabel = () => "",
    inventoryEntryKindLabel = () => "",
  } = deps;

  const slot = inventoryEntryEquipmentSlot(entry);
  const item = items[inventoryEntryItemId(entry)] || {};
  const detailText = inventoryEntryDetailText(entry);
  const reorderEnabled = inventoryManualReorderEnabled();
  const canMoveUp = reorderEnabled && index > 0;
  const canMoveDown = reorderEnabled && index < normalizeInventoryList(state.inventory).length - 1;
  const compareRows = slot
    ? state.party.map((hero) => `${escapeHtml(hero.name)}: ${escapeHtml(compareCandidateSummary(hero, entry) || "장착 불가")}`).join("<br />")
    : "";
  const consumableButtons = item.kind === "consumable"
    ? state.party.map((hero, heroIndex) => `<button data-use-hero="${heroIndex}" data-use-index="${index}" title="${escapeHtml(item.heal ? `${hero.name} HP +${item.heal}` : item.cure ? `${hero.name} ${item.cure} 치료` : item.name)}">${escapeHtml(hero.name)} 사용</button>`).join("")
    : "";
  return `
    <article class="inventory-entry-card" data-inventory-preview-index="${index}" ${reorderEnabled ? `draggable="true" data-inventory-drag-index="${index}" data-inventory-drop-index="${index}"` : ""}>
      <div class="panel-title">
        <span>${escapeHtml(inventoryEntryLabel(entry))}</span>
        <span>${escapeHtml(inventoryEntryKindLabel(entry))}${reorderEnabled ? " · 드래그" : ""}</span>
      </div>
      ${reorderEnabled ? `
        <div class="actions inventory-reorder-actions">
          <button data-inventory-move="up" data-inventory-move-index="${index}" ${canMoveUp ? "" : "disabled"}>위로</button>
          <button data-inventory-move="down" data-inventory-move-index="${index}" ${canMoveDown ? "" : "disabled"}>아래로</button>
        </div>
      ` : ""}
      ${detailText ? `<div class="muted">${escapeHtml(detailText)}</div>` : ""}
      ${compareRows ? `<div class="muted">${compareRows}</div>` : ""}
      <div class="actions">
        ${consumableButtons}
        ${slot
          ? state.party.map((hero) => `<button data-equip-hero="${hero.id}" data-equip-index="${index}" title="${escapeHtml(compareCandidateSummary(hero, entry) || detailText || inventoryEntryLabel(entry))}">${escapeHtml(hero.name)} 장착<br /><span class="muted">${escapeHtml(compareCandidateSummary(hero, entry) || "장착 시도")}</span></button>`).join("")
          : !consumableButtons ? `<span class="muted">사용/장착 action 없음</span>` : ""}
      </div>
    </article>
  `;
}

export function inventoryOverlayMarkup(deps = {}) {
  const {
    state,
    items = {},
    inventoryOverlayOpen = () => false,
    sortedFilteredInventoryEntries = () => [],
    inventoryManualReorderEnabled = () => false,
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
    inventoryEntryOverlayMarkupImpl = () => "",
  } = deps;

  if (!inventoryOverlayOpen()) return "";
  const inventoryEntries = sortedFilteredInventoryEntries();
  const reorderEnabled = inventoryManualReorderEnabled();
  const previewIndex = Number(state.inventoryPanelPreviewIndex || -1);
  const previewEntry = previewIndex >= 0 ? normalizeInventoryList(state.inventory)[previewIndex] || null : null;
  const previewCompareRows = previewEntry && inventoryEntryEquipmentSlot(previewEntry)
    ? state.party.map((hero) => `${escapeHtml(hero.name)}: ${escapeHtml(compareCandidateSummary(hero, previewEntry) || "장착 불가")}`).join("<br />")
    : "";
  return `
    <div class="interaction-panel inventory-panel">
      <div class="panel-title"><span>원정 가방</span><button data-inventory-close="1">닫기</button></div>
      <div class="actions">
        <label class="muted">검색
          <input id="inventoryPanelQueryInput" type="search" value="${escapeHtml(state.inventoryPanelQuery || "")}" placeholder="이름, 종류, 상태 검색" />
        </label>
        <label class="muted">필터
          <select id="inventoryPanelFilterSelect">${inventoryFilterOptions().map(([id, label]) => `<option value="${id}" ${id === (state.inventoryPanelFilter || "all") ? "selected" : ""}>${label}</option>`).join("")}</select>
        </label>
        <label class="muted">정렬
          <select id="inventoryPanelSortSelect">${inventorySortOptions().map(([id, label]) => `<option value="${id}" ${id === (state.inventoryPanelSort || "default") ? "selected" : ""}>${label}</option>`).join("")}</select>
        </label>
      </div>
      <p class="muted">표시 ${inventoryEntries.length}개 · 전체 ${normalizeInventoryList(state.inventory).length}개 · ${escapeHtml(inventorySummaryText())}</p>
      <p class="muted">${reorderEnabled ? "기본 정렬 / 전체 보기 상태에서 카드 드래그 또는 위로/아래로 버튼으로 순서를 바꿀 수 있다." : "재정렬은 기본 정렬, 전체 보기, 검색 없음 상태에서만 가능하다."}</p>
      <div class="inventory-overlay-grid">
        <section class="inventory-column">
          <div class="panel-title"><span>파티 장착</span><span>${state.party.length}명</span></div>
          ${state.party.map((hero) => `
            <article class="inventory-entry-card">
              <div class="panel-title">
                <span>${escapeHtml(hero.name)}</span>
                <span>공 ${hero.atk} · 방 ${hero.def}</span>
              </div>
              <div class="muted">${Object.values(heroEquipmentEntries(hero)).map((entry) => inventoryEntryLabel(entry)).join(" / ") || "장비 없음"}</div>
              <div class="muted">${Object.values(heroEquipmentEntries(hero)).map((entry) => inventoryEntryDetailText(entry)).filter(Boolean).join(" / ") || "비교 가능한 장비 없음"}</div>
            </article>
          `).join("")}
        </section>
        <section class="inventory-column">
          <div class="panel-title"><span>전체 가방</span><span>${inventoryEntries.length}칸</span></div>
          ${previewEntry ? `
            <article class="inventory-entry-card inventory-preview-card">
              <div class="panel-title">
                <span>${escapeHtml(inventoryEntryLabel(previewEntry))}</span>
                <button data-inventory-preview-close="1">미리보기 닫기</button>
              </div>
              <div class="muted">${escapeHtml(inventoryEntryKindLabel(previewEntry))}</div>
              ${inventoryEntryDetailText(previewEntry) ? `<div class="muted">${escapeHtml(inventoryEntryDetailText(previewEntry))}</div>` : ""}
              ${previewCompareRows ? `<div class="muted">${previewCompareRows}</div>` : ""}
            </article>
          ` : ""}
          ${inventoryEntries.length
            ? inventoryEntries.map(({ entry, index }) => inventoryEntryOverlayMarkupImpl({ entry, index })).join("")
            : `<article class="inventory-entry-card"><div class="muted">가방이 비어 있다.</div></article>`}
        </section>
      </div>
    </div>
  `;
}

export function drawDungeonView(deps = {}) {
  const {
    state,
    documentObject = document,
    createViewController = () => null,
    viewController = null,
    setViewController = () => {},
    dragLookEnabled = false,
    inventoryOverlayOpen = () => false,
    materialManifest = {},
    rendererLodProfile = "default",
    selectedPreset = () => null,
    resolveInteractionCandidate = () => ({ dir: "north", placements: [], cell: null, target: { x: 0, y: 0 } }),
    dirs = [],
    vec = {},
    logicalPlayerCell = (player) => ({
      x: Math.floor(Number(player?.x) + 0.5),
      y: Math.floor(Number(player?.y) + 0.5),
    }),
    interactivePlacementKinds = new Set(),
    currentMapSeed = () => null,
    interactionOverlayMarkup = () => "",
    inventoryOverlayMarkup = () => "",
    hostId = "view",
    overlayId = "viewOverlay",
    activeModes = ["dungeon"],
    inventoryMode = "dungeon",
    activeMap = state.map,
    activePlayer = state.player,
    showSeed = () => true,
  } = deps;

  if (!activeModes.includes(state.mode)) return viewController;
  const host = documentObject.getElementById(hostId);
  if (!host) return viewController;
  let activeViewController = viewController;
  if (activeViewController?.host && activeViewController.host !== host) {
    activeViewController.dispose?.();
    activeViewController = null;
    setViewController(null);
  }
  if (!activeViewController) {
    activeViewController = createViewController(host, {
      enabled: dragLookEnabled,
      getMode: () => state.mode,
      interactiveModes: activeModes,
      canCapturePointer: () => activeModes.includes(state.mode)
        && !state.interaction
        && !state.skillDeckOpen
        && !state.skillShopOpen
        && !inventoryOverlayOpen(inventoryMode),
    });
    setViewController(activeViewController);
  }
  activeViewController.sync(activeMap, activePlayer, {
    materialManifest,
    lodProfile: rendererLodProfile,
  });
  const pointerLook = activeViewController.getPointerLookState();
  const overlay = documentObject.getElementById(overlayId);
  const activePreset = selectedPreset();
  const currentCell = logicalPlayerCell(activePlayer);
  const interactCandidate = resolveInteractionCandidate(
    activeMap,
    activePlayer,
    pointerLook.lookYaw || 0,
    dirs,
    vec,
    (placement) => (interactivePlacementKinds.has(placement.kind) || placement.kind === "stairs") && !placement.done
  );
  const targetSummary = interactCandidate.placements[0]
    ? `${interactCandidate.dir} · ${interactCandidate.placements[0].kind} · ${interactCandidate.placements[0].id}`
    : interactCandidate.cell?.walkable
      ? `${interactCandidate.dir} · ${interactCandidate.target.x},${interactCandidate.target.y}`
      : `${interactCandidate.dir} · blocked`;
  overlay.innerHTML = `
    <strong>${activeMap.name}</strong><br />
    <span class="muted">F${activePlayer.floor} · pos ${activePlayer.x.toFixed(2)},${activePlayer.y.toFixed(2)} · cell ${currentCell.x},${currentCell.y} · ${activePlayer.facing}${showSeed() && currentMapSeed() != null ? ` · seed ${currentMapSeed()}` : ""}</span>
    ${activeMap.generation?.profileId ? `<br /><span class="muted">generated · ${activeMap.generation.profileId} · ${activeMap.generation.algorithm || "unknown_algorithm"} · modules ${activeMap.generation.moduleCount ?? "?"}</span>` : ""}
    <br /><span class="muted">FPS mouse look ${pointerLook.enabled ? "on" : "off"} · pointer ${pointerLook.locked ? "locked" : "click view"} · yaw ${(pointerLook.lookYaw || 0).toFixed(2)} · pitch ${(pointerLook.lookPitch || 0).toFixed(2)} · lod ${rendererLodProfile}</span>
    <br /><span class="muted">interact target ${targetSummary}</span>
    ${state.runtimeSession?.kind === "test_play" ? `<br /><span class="muted">test session · compiledMap runtime · editor 버튼으로 복귀</span>` : ""}
    ${state.mode === "editor" ? `<br /><span class="muted">editor: ${state.editor.editorWorkspaceMode === "generator_workbench" ? "generator_workbench" : "legacy_cell_editor"} · tool ${state.editorTool}${activePreset ? ` · preset ${activePreset.name}` : ""}</span>` : ""}
    ${interactionOverlayMarkup()}
    ${inventoryOverlayOpen(inventoryMode) ? inventoryOverlayMarkup() : ""}
  `;
  return activeViewController;
}

export function bindTownFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    render = () => {},
    toggleInventoryOverlay = () => {},
    makeHero = () => ({}),
    normalizePartyModel = ({ party }) => ({ party, companion: null }),
    syncPartyRows = () => {},
    addLog = () => {},
  } = deps;

  const openTownInventoryBtn = documentObject.getElementById("openTownInventoryBtn");
  if (openTownInventoryBtn) {
    openTownInventoryBtn.onclick = () => {
      toggleInventoryOverlay(true);
      render();
    };
  }
  const applyPartyBtn = documentObject.getElementById("applyPartyBtn");
  if (applyPartyBtn) {
    applyPartyBtn.onclick = () => {
      const nextParty = state.party.map((oldHero, i) => {
        if (oldHero.isCompanion) return oldHero;
        const classIndex = Number(documentObject.getElementById(`partyClass${i}`).value);
        const name = documentObject.getElementById(`partyName${i}`).value.trim();
        if (oldHero.classIndex === classIndex) return { ...oldHero, name: name || oldHero.name };
        const next = makeHero(i, classIndex, name);
        next.xp = oldHero.xp;
        return next;
      });
      ({ party: state.party, companion: state.companion } = normalizePartyModel(nextParty, state.companion));
      syncPartyRows();
      addLog("파티 편성을 갱신했다.");
      render();
    };
  }
}

export function bindCombatFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    render = () => {},
    toggleInventoryOverlay = () => {},
    queueCombatAction = () => {},
    resolveHeroAction = () => {},
    clearCombatDiceSelection = () => false,
    selectCombatDie = () => false,
    useCombatConsumable = () => {},
    useCombatThrowItem = () => {},
  } = deps;

  const inventoryButton = documentObject.getElementById("openCombatInventoryBtn");
  if (inventoryButton) {
    inventoryButton.onclick = () => {
      toggleInventoryOverlay(true);
      render();
    };
  }
  documentObject.querySelectorAll("[data-combat-action]").forEach((button) => {
    button.onclick = () => {
      const action = button.dataset.combatAction || "";
      if (action === "stop") {
        const diceState = state.combat?.diceState || null;
        const phase = String(diceState?.phase || "");
        const canStop = Boolean(
          diceState
          && (diceState.canStop === true
            || diceState.stopEnabled === true
            || diceState.isSpinning === true
            || diceState.isStopping === true
            || phase === "spin"
            || phase === "spinning"
            || phase === "stopping"
            || phase === "reveal")
        );
        if (!canStop) return;
      }
      queueCombatAction(action);
    };
  });
  documentObject.querySelectorAll("[data-combat-roll]").forEach((button) => {
    button.onclick = () => {
      const diceState = state.combat?.diceState || null;
      if (!diceState) return;
      if (diceState.selectionLocked === true || diceState.lockSelection === true) return;
      if (diceState.isSpinning === true || diceState.isStopping === true) return;
      if (["spin", "spinning", "stopping", "reveal", "init"].includes(String(diceState.phase || ""))) return;
      const rollId = button.dataset.combatRoll || "";
      if (!rollId) return;
      selectCombatDie(rollId);
    };
  });
  documentObject.querySelectorAll("[data-combat-dice-target]").forEach((button) => {
    button.onclick = () => resolveHeroAction("attack", button.dataset.combatDiceTarget || "");
  });
  documentObject.querySelectorAll("[data-combat-confirm]").forEach((button) => {
    button.onclick = () => resolveHeroAction("attack", "");
  });
  documentObject.querySelectorAll("[data-combat-cancel]").forEach((button) => {
    button.onclick = () => {
      const diceState = state.combat?.diceState || null;
      if (diceState?.selectionLocked === true || diceState?.lockSelection === true) return;
      if (diceState?.isSpinning === true || diceState?.isStopping === true) return;
      if (["spin", "spinning", "stopping", "reveal", "init"].includes(String(diceState?.phase || ""))) return;
      state.combat.pendingItemIndex = -1;
      state.combat.pendingItemPreviewIndex = -1;
      clearCombatDiceSelection();
    };
  });
  documentObject.querySelectorAll("[data-combat-item-preview]").forEach((card) => {
    const previewIndex = Number(card.getAttribute("data-combat-item-preview") || -1);
    const openPreview = () => {
      if (!state.combat || state.combat.pendingItemPreviewIndex === previewIndex) return;
      state.combat.pendingItemPreviewIndex = previewIndex;
      render();
    };
    card.onmouseenter = openPreview;
    card.onfocus = openPreview;
  });
  documentObject.querySelectorAll("[data-combat-item-pick]").forEach((button) => {
    button.onclick = () => {
      if (!state.combat) return;
      state.combat.pendingItemIndex = Number(button.dataset.combatItemPick || -1);
      state.combat.pendingItemPreviewIndex = state.combat.pendingItemIndex;
      render();
    };
  });
  documentObject.querySelectorAll("[data-combat-item-hero]").forEach((button) => {
    button.onclick = () => {
      const heroIndex = Number(button.dataset.combatItemHero || -1);
      const inventoryIndex = Number(button.dataset.combatItemIndex || -1);
      useCombatConsumable(heroIndex, inventoryIndex);
    };
  });
  documentObject.querySelectorAll("[data-combat-item-enemy]").forEach((button) => {
    button.onclick = () => {
      const inventoryIndex = Number(button.dataset.combatItemIndex || -1);
      const enemyId = button.dataset.combatItemEnemy || "";
      useCombatThrowItem(inventoryIndex, enemyId);
    };
  });
  documentObject.querySelectorAll("[data-combat-item-all-enemies]").forEach((button) => {
    button.onclick = () => {
      const inventoryIndex = Number(button.dataset.combatItemAllEnemies || -1);
      useCombatThrowItem(inventoryIndex, "");
    };
  });
}

export function renderQuestFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    normalizeQuestState = (quest) => quest,
    syncQuestSeedFailureStates = () => {},
    questEndingComplete = () => false,
    boardQuestCanReturn = () => false,
    escapeHtml = (value) => String(value ?? ""),
  } = deps;
  state.quest = normalizeQuestState(state.quest);
  syncQuestSeedFailureStates();
  const defeated = Object.keys(state.quest.bossesDefeated).length;
  const activeSeed = Object.values(state.quest.seeds || {}).find((seed) => seed?.status === "active");
  const activeBoardQuest = state.quest.activeQuestId ? state.quest.seeds?.[state.quest.activeQuestId] : null;
  const failedSeed = Object.values(state.quest.seeds || {}).find((seed) => seed?.status === "failed");
  const ending = questEndingComplete(state.quest) ? state.quest.ending : null;
  documentObject.getElementById("quest").innerHTML = `
    <p>${state.quest.main}</p>
    <p class="muted">층 ${state.player.floor}/3 · 보스 ${defeated}/2 · 최종 계단: ${questEndingComplete(state.quest) ? "도달" : "미완료"}</p>
    ${ending ? `<p class="muted">엔딩: ${escapeHtml(ending.title)} · ${escapeHtml(ending.summary)}</p>` : ""}
    ${activeBoardQuest ? `<p class="muted">수주 의뢰: ${escapeHtml(activeBoardQuest.title)} · ${activeBoardQuest.status === "completed" ? "완료" : "진행 중"}</p>` : ""}
    ${activeBoardQuest?.conditions?.summary ? `<p class="muted">의뢰 조건: ${escapeHtml(activeBoardQuest.conditions.summary)}</p>` : ""}
    ${boardQuestCanReturn(state.quest) ? `<p><button data-board-quest-return="1">${escapeHtml(activeBoardQuest?.return?.label || "귀환")}</button></p>` : ""}
    ${activeSeed ? `<p class="muted">보조 목표: ${activeSeed.title} · ${activeSeed.note}</p>` : ""}
    ${activeSeed?.objectives?.length ? `<p class="muted">목표: ${activeSeed.objectives.join(" / ")}</p>` : ""}
    ${failedSeed ? `<p class="muted">실패한 목표: ${failedSeed.title}</p>` : ""}
    <p class="muted">FPS 조작: 화면 클릭 후 마우스로 시점 · WASD 연속 이동/스트레이프 · F/Space/Enter 상호작용 · Esc 마우스 해제 · 화살표 좌우 회전 보조</p>
  `;
}

export function renderLogFrame(deps = {}) {
  const {
    state,
    documentObject = document,
  } = deps;
  documentObject.getElementById("log").innerHTML = state.log.map((l) => `<div>${l}</div>`).join("");
}

export function renderMiniMapFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    currentMapSeed = () => null,
    getCell = () => null,
    logicalPlayerCell = (player) => ({
      x: Math.floor(Number(player?.x) + 0.5),
      y: Math.floor(Number(player?.y) + 0.5),
    }),
  } = deps;
  const el = documentObject.getElementById("minimap");
  const currentCell = logicalPlayerCell(state.player);
  el.style.gridTemplateColumns = `repeat(${state.map.size.width}, 13px)`;
  documentObject.getElementById("floorLabel").textContent = `F${state.player.floor} pos ${state.player.x.toFixed(2)},${state.player.y.toFixed(2)} cell ${currentCell.x},${currentCell.y} ${state.player.facing}${currentMapSeed() != null ? ` · seed ${currentMapSeed()}` : ""}`;
  el.innerHTML = "";
  for (let y = 0; y < state.map.size.height; y++) {
    for (let x = 0; x < state.map.size.width; x++) {
      const c = getCell(state.map, x, y);
      const d = documentObject.createElement("div");
      d.className = "mini-cell";
      if (!c?.walkable) d.classList.add("wall");
      if (state.visited.has(`${x},${y}`)) d.classList.add("seen");
      if (state.map.placements.some((p) => !p.done && p.position.x === x && p.position.y === y && p.kind === "stairs")) d.classList.add("exit");
      if (state.map.placements.some((p) => !p.done && p.position.x === x && p.position.y === y && (p.kind === "encounter" || p.kind === "monster"))) d.classList.add("monster");
      if (state.map.placements.some((p) => !p.done && p.position.x === x && p.position.y === y && p.kind === "npc")) d.classList.add("npc");
      if (x === currentCell.x && y === currentCell.y) d.classList.add("player");
      el.appendChild(d);
    }
  }
}

export function renderPartyFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    availableEquipmentEntries = () => [],
    nextClassMilestoneText = () => "",
    heroEquipmentEntries = () => ({}),
    equipmentEntryLabel = () => "",
    escapeHtml = (value) => String(value ?? ""),
    compareCandidateSummary = () => "",
    inventoryEntryDetailText = () => "",
    inventoryEntryLabel = () => "",
  } = deps;
  if (state.mode === "title") {
    documentObject.getElementById("party").innerHTML = "";
    return;
  }
  const equippableEntries = availableEquipmentEntries();
  documentObject.getElementById("party").innerHTML = state.party.map((p) => `
    <div class="char ${p.hp <= 0 ? "dead" : ""}">
      <div class="char-head"><strong>${p.name}</strong><span>${p.row} · ${p.cls}${p.isCompanion ? " · 동료" : ""}</span></div>
      <div class="bar"><span style="width:${Math.max(0, (p.hp / p.maxHp) * 100)}%"></span></div>
      <div class="row muted"><span>HP ${p.hp}/${p.maxHp}</span><span>공 ${p.atk} 방 ${p.def}</span></div>
      <div class="muted">숙련 ${p.prof?.[p.category] || 0} · 훈련 ${p.trainingLevel || 0}단계 · ${p.passive ? "패시브 활성" : "패시브 미해금"}</div>
      <div class="muted">${nextClassMilestoneText(p)} ${p.status.join(", ")} ${p.note || ""}</div>
      <div class="muted">장착 ${Object.values(heroEquipmentEntries(p)).map((entry) => equipmentEntryLabel(entry)).join(" / ") || "없음"}</div>
      ${state.mode !== "combat" ? `<div class="row"><button data-skill-deck-open="${p.id}">스킬창</button><span class="muted">${Number(p.diceLoadout?.diceCount || 0)}개 주사위</span></div>` : ""}
      <div class="actions">
        ${equippableEntries.slice(0, 3).map((entry) => `<button data-equip-hero="${p.id}" data-equip-index="${entry.index}" title="${escapeHtml(compareCandidateSummary(p, entry.entry) || inventoryEntryDetailText(entry.entry) || inventoryEntryLabel(entry.entry))}">${escapeHtml(inventoryEntryLabel(entry.entry))}<br /><span class="muted">${escapeHtml(compareCandidateSummary(p, entry.entry) || inventoryEntryDetailText(entry.entry) || "장착 후보")}</span></button>`).join("") || `<span class="muted">장착 후보 없음</span>`}
      </div>
      <div class="muted">비교 ${equippableEntries.slice(0, 2).map((entry) => `${inventoryEntryLabel(entry.entry)} (${compareCandidateSummary(p, entry.entry) || "장착 불가"})`).join(" · ") || "후보 없음"}</div>
    </div>
  `).join("");
}

export function renderResourcesFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    inventorySummaryText = () => "",
    normalizeInventoryList = (value) => value || [],
    inventoryEntryLabel = () => "",
    inventoryEntryDetailText = () => "",
    skills = {},
    skillName = (skillId) => skillId || "기술",
    escapeHtml = (value) => String(value ?? ""),
  } = deps;
  if (state.mode === "title") {
    documentObject.getElementById("resources").innerHTML = "";
    return;
  }
  documentObject.getElementById("resources").innerHTML = `
    <div>횃불<br><strong>${state.resources.torch}</strong></div>
    <div>식량<br><strong>${state.resources.food}</strong></div>
    <div>물<br><strong>${state.resources.water}</strong></div>
    <div>금화<br><strong>${state.resources.gold}</strong></div>
    <div>가방<br><strong>${inventorySummaryText()}</strong><br /><span class="muted">${normalizeInventoryList(state.inventory).slice(0, 4).map((entry) => `${inventoryEntryLabel(entry)}${inventoryEntryDetailText(entry) ? ` (${inventoryEntryDetailText(entry)})` : ""}`).join(" / ") || "비어 있음"}</span><br /><span class="muted">던전에서 I 또는 가방</span></div>
    <div>스킬 덱<br><strong>${escapeHtml(activeSkillDeckHero(state)?.name || "영웅")}</strong><br /><button data-skill-deck-open="${escapeHtml(activeSkillDeckHero(state)?.id || "")}" ${state.mode === "title" ? "disabled" : ""}>편집 열기</button></div>
    ${renderSkillDeckOverlay({ state, escapeHtml, skills, skillName })}
    ${renderSkillShopOverlay({ state, escapeHtml, skills, skillName })}
  `;
}

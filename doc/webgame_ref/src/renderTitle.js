function renderEndingPanel(completedEnding, state, escapeHtml) {
  return `
    <article class="slot-detail is-filled">
      <strong>${escapeHtml(completedEnding.title)}</strong>
      <div class="slot-meta">
        <div>층 ${completedEnding.floor ?? state.player?.floor ?? "-"}</div>
        <div>결말 ${escapeHtml(completedEnding.endingId || "ending")}</div>
        <div>보스 ${Object.keys(completedEnding.bossesDefeated || {}).length}</div>
        <div>${completedEnding.completedAt ? escapeHtml(new Date(completedEnding.completedAt).toLocaleString("ko-KR")) : "종료 시각 없음"}</div>
      </div>
      <p class="muted">${escapeHtml(completedEnding.summary)}</p>
      <p class="muted">현재 run은 종료 상태다. Continue에서 이전 저장을 불러오거나 Start에서 새 원정을 시작할 수 있다.</p>
    </article>
    <div class="slot-actions">
      <button data-title-action="continue">Continue</button>
      <button data-title-action="new-game">Start</button>
      <button data-title-action="back-menu">메인으로</button>
    </div>
  `;
}

function renderContinuePanel({ slots, selectedSlot, selectedSlotId, saveSlotLabel, escapeHtml }) {
  return `
    <p class="muted">Continue 씬에서 저장 슬롯을 고른 뒤 불러온다. 첫 화면에서는 슬롯 상세를 보여주지 않는다.</p>
    <div class="slot-list">
      ${slots.map((slot) => `
        <article class="slot-card ${slot.id === selectedSlotId ? "is-active" : ""}">
          <strong>${slot.label}</strong>
          <div class="muted">${slot.summary}</div>
          <div class="slot-meta">
            <div>층 ${slot.meta?.floor ?? "-"}</div>
            <div>seed ${slot.meta?.seed ?? "-"}</div>
            <div>플레이 ${slot.meta?.playtimeLabel ?? "-"}</div>
            <div>${slot.meta?.partyLabel ?? "주인공 정보 없음"}</div>
            <div>${slot.meta?.savedAtLabel ?? "저장 없음"}</div>
          </div>
          <button data-slot-select="${slot.id}">선택</button>
        </article>
      `).join("")}
    </div>
    <article class="slot-detail ${selectedSlot?.hasSave ? "is-filled" : "is-empty"}">
      <strong>${selectedSlot?.label || saveSlotLabel(selectedSlotId)} 상세</strong>
      ${selectedSlot?.hasSave ? `
        <label>슬롯 이름 <input id="slotAliasInput" value="${escapeHtml(selectedSlot.meta?.slotAlias || "")}" maxlength="24" placeholder="${saveSlotLabel(selectedSlotId)}" /></label>
        <div class="slot-meta">
          <div>주인공 ${selectedSlot.meta?.protagonistName || "-"}</div>
          <div>직업 ${selectedSlot.meta?.protagonistClass || "-"}</div>
          <div>배경 ${selectedSlot.meta?.backgroundLabel || "-"}</div>
          <div>동료 ${selectedSlot.meta?.companionName || "없음"}</div>
          <div>층 ${selectedSlot.meta?.floor ?? "-"}</div>
          <div>seed ${selectedSlot.meta?.seed ?? "-"}</div>
          <div>플레이 ${selectedSlot.meta?.playtimeLabel ?? "-"}</div>
          <div>${selectedSlot.meta?.savedAtLabel ?? "저장 시각 없음"}</div>
        </div>
        <p class="muted">${selectedSlot.meta?.progressLabel || "진행 상태 정보 없음"}</p>
        <p class="muted">${selectedSlot.meta?.recentStatusLabel || "최근 상태 정보 없음"}</p>
        <p class="muted">${selectedSlot.meta?.resourceLabel || "자원 정보 없음"}</p>
      ` : `
        <p class="muted">현재 빈 슬롯이다. 저장된 진행이 없는 슬롯은 불러올 수 없다.</p>
      `}
    </article>
    <div class="slot-actions">
      <button data-title-action="load-slot" ${slots.every((slot) => slot.id !== selectedSlotId || !slot.hasSave) ? "disabled" : ""}>선택 슬롯 불러오기</button>
      <button data-title-action="rename-slot" ${selectedSlot?.hasSave ? "" : "disabled"}>슬롯 이름 저장</button>
      <button data-title-action="delete-slot" ${selectedSlot?.hasSave ? "" : "disabled"}>선택 슬롯 삭제</button>
      <button data-title-action="back-menu">뒤로</button>
    </div>
  `;
}

function renderCreatorPanel({
  state,
  selectedClass,
  selectedBackground,
  selectedLoadout,
  selectedSlot,
  selectedSlotId,
  saveSlotLabel,
  protagonistBackgrounds,
  starterLoadouts,
  classes,
  items,
  escapeHtml,
}) {
  return `
    <p class="muted">Start 다음 씬에서 주인공을 정한 뒤 원정을 시작한다.</p>
    <label>이름 <input id="newGameName" value="${escapeHtml(state.shell.newGameDraft.name || "")}" maxlength="12" /></label>
    <label>직업
      <select id="newGameClass">
        ${classes.map((cls, index) => `<option value="${index}" ${Number(state.shell.newGameDraft.classIndex || 0) === index ? "selected" : ""}>${cls.cls}</option>`).join("")}
      </select>
    </label>
    <label>배경
      <select id="newGameBackground">
        ${protagonistBackgrounds.map((entry) => `<option value="${entry.id}" ${entry.id === selectedBackground.id ? "selected" : ""}>${entry.label}</option>`).join("")}
      </select>
    </label>
    <label>시작 보급
      <select id="newGameLoadout">
        ${starterLoadouts.map((entry) => `<option value="${entry.id}" ${entry.id === selectedLoadout.id ? "selected" : ""}>${entry.label}</option>`).join("")}
      </select>
    </label>
    <article class="slot-detail is-filled">
      <strong>캐릭터 생성 미리보기</strong>
      <div class="slot-meta">
        <div>직업 ${selectedClass.cls}</div>
        <div>HP ${selectedClass.hp}</div>
        <div>공격 ${selectedClass.atk}</div>
        <div>방어 ${selectedClass.def}</div>
        <div>배경 ${selectedBackground.label}</div>
        <div>보급 ${selectedLoadout.label}</div>
      </div>
      <p class="muted">${selectedBackground.summary} ${selectedBackground.questNote}</p>
      <p class="muted">${selectedLoadout.summary} · 횃불 ${selectedLoadout.resources.torch} · 식량 ${selectedLoadout.resources.food} · 물 ${selectedLoadout.resources.water} · 금화 ${selectedLoadout.resources.gold}</p>
      <p class="muted">시작 가방: ${(selectedLoadout.inventory || []).map((itemId) => items[itemId]?.name || itemId).join(", ") || "비어 있음"}</p>
    </article>
    <p class="muted">선택 슬롯: ${saveSlotLabel(selectedSlotId)} · 시작은 주인공 1인 파티이며 후속으로 동료 최대 1명 모델로 이어진다.</p>
    <p class="muted">${selectedSlot?.hasSave ? "이 슬롯에는 기존 저장이 있다. 새 run은 저장하기 전까지 덮어쓰지 않는다." : "현재 빈 슬롯이므로 첫 저장 시 이 슬롯을 사용한다."}</p>
    <div class="slot-actions">
      <button data-title-action="start-run">캐릭터 생성 완료</button>
      <button data-title-action="back-menu">뒤로</button>
    </div>
  `;
}

function renderEditorPanel({ editorProjectDashboard, savedEditorProject, state }) {
  return `
    <article class="slot-detail is-filled">
      <strong>Editor Gateway</strong>
      <div class="slot-meta">
        <div>floor ${editorProjectDashboard.floorEntries.length}</div>
        <div>project error ${editorProjectDashboard.projectValidationReport.summary.error}</div>
        <div>warning ${editorProjectDashboard.projectValidationReport.summary.warning}</div>
        <div>compiled ${Object.keys(editorProjectDashboard.compiledProject.compiledMaps || {}).length}</div>
        <div>manifest ${editorProjectDashboard.manifestReady ? "ready" : "blocked"}</div>
        <div>saved project ${savedEditorProject ? "available" : "none"}</div>
      </div>
      <p class="muted">Editor project 저장은 runtime save slot과 완전히 별도다. Continue 슬롯은 게임 진행만, project 저장은 authoring 상태만 다룬다.</p>
      <p class="muted">현재 workspace를 바로 열거나, 저장된 project를 불러오거나, 새 project를 만들어 generator workbench부터 시작할 수 있다.</p>
    </article>
    <div class="slot-list">
      <article class="slot-card is-active">
        <strong>현재 workspace 열기</strong>
        <div class="muted">현재 in-memory authored floor와 validation/build 상태를 그대로 이어서 연다. 기본 landing은 generator workbench다.</div>
        <div class="slot-meta">
          <div>current floor ${state.player.floor}</div>
          <div>compiled ${editorProjectDashboard.compiledProject.ok ? "ready" : "blocked"}</div>
        </div>
        <button data-title-action="open-editor-workspace">workspace 열기</button>
      </article>
      <article class="slot-card ${savedEditorProject ? "" : "is-empty"}">
        <strong>저장된 project 불러오기</strong>
        <div class="muted">${savedEditorProject ? "LocalStorage의 editor project snapshot을 workspace에 적용한다." : "아직 저장된 editor project가 없다."}</div>
        <div class="slot-meta">
          <div>source ${savedEditorProject ? "localStorage" : "-"}</div>
          <div>game slot 분리</div>
        </div>
        <button data-title-action="load-editor-project" ${savedEditorProject ? "" : "disabled"}>saved project 열기</button>
      </article>
      <article class="slot-card">
        <strong>새 project 만들기</strong>
        <div class="muted">새 generated floor seed와 빈 editor session으로 authoring workspace를 다시 시작하고 generator workbench를 먼저 연다.</div>
        <div class="slot-meta">
          <div>runtime save 비오염</div>
          <div>fresh workspace</div>
        </div>
        <button data-title-action="create-editor-project">fresh project 시작</button>
      </article>
    </div>
    <div class="slot-actions">
      <button data-title-action="back-menu">뒤로</button>
    </div>
  `;
}

export function renderTitleShell({
  state,
  documentObject,
  readSaveSlots,
  currentSaveSlotId,
  classes,
  selectedBackgroundForDraft,
  selectedLoadoutForDraft,
  buildProjectDashboardSnapshot,
  hasSavedEditorProject,
  saveSlotLabel,
  protagonistBackgrounds,
  starterLoadouts,
  items,
  escapeHtml,
  handleTitleAction,
  render,
} = {}) {
  const root = documentObject?.getElementById?.("title");
  if (!root) return;
  const slots = readSaveSlots();
  const selectedSlotId = currentSaveSlotId();
  const selectedSlot = slots.find((slot) => slot.id === selectedSlotId) || slots[0];
  const draft = state.shell.newGameDraft || {};
  const selectedClass = classes[Number(draft.classIndex || 0)] || classes[0];
  const selectedBackground = selectedBackgroundForDraft(draft);
  const selectedLoadout = selectedLoadoutForDraft(draft);
  const editorProjectDashboard = buildProjectDashboardSnapshot(state.floorMaps);
  const savedEditorProject = hasSavedEditorProject();
  const completedEnding = state.quest?.ending?.status === "complete" ? state.quest.ending : null;
  const panel = state.shell.titlePanel || "menu";
  const isMenuScene = panel === "menu";
  const activeMenu = panel === "continue" ? "continue" : panel === "editor" ? "editor" : "new-game";
  const panelTitle = panel === "creator"
    ? "캐릭터 생성"
    : panel === "continue"
      ? "Continue"
      : panel === "editor"
        ? "Editor"
        : panel === "ending"
          ? "결말"
          : "";
  let panelMarkup = "";
  if (panel === "ending" && completedEnding) {
    panelMarkup = renderEndingPanel(completedEnding, state, escapeHtml);
  } else if (panel === "continue") {
    panelMarkup = renderContinuePanel({ slots, selectedSlot, selectedSlotId, saveSlotLabel, escapeHtml });
  } else if (panel === "creator") {
    panelMarkup = renderCreatorPanel({
      state,
      selectedClass,
      selectedBackground,
      selectedLoadout,
      selectedSlot,
      selectedSlotId,
      saveSlotLabel,
      protagonistBackgrounds,
      starterLoadouts,
      classes,
      items,
      escapeHtml,
    });
  } else if (panel === "editor") {
    panelMarkup = renderEditorPanel({ editorProjectDashboard, savedEditorProject, state });
  }

  root.innerHTML = `
    <div class="title-shell ${isMenuScene ? "is-menu-scene" : `is-${panel}-scene`}">
      <section class="title-stage">
        <div class="title-stage-backdrop">
          <div class="title-stage-shape title-stage-shape-back"></div>
          <div class="title-stage-shape title-stage-shape-front"></div>
          <div class="title-stage-glow title-stage-glow-red"></div>
          <div class="title-stage-glow title-stage-glow-blue"></div>
          <div class="title-stage-glyph">ABYSSAL</div>
          <div class="title-stage-menu-ghost">
            <span class="${activeMenu === "new-game" ? "is-active" : ""}">Start</span>
            <span>Options</span>
            <span>Credits</span>
            <span>Quit game</span>
          </div>
        </div>
        <div class="title-stage-copy">
          <div class="title-kicker">Conan Prototype</div>
          <h2>Labyrinths of the Serpent</h2>
          <p>${isMenuScene ? "첫 시작 화면은 메뉴만 보여 준다. Start는 캐릭터 생성 씬으로, Continue는 저장 슬롯 씬으로 이동한다." : "현재 씬에서 메뉴 선택에 맞는 상세 흐름을 진행한다."}</p>
        </div>
      </section>
      <section class="title-sidebar">
        <div class="title-brand-panel">
          <div class="title-brand-mark">S</div>
          <div class="title-brand-copy">
            <div class="title-brand-kicker">Start Screen</div>
            <h3>ABYSSAL<br />GATE</h3>
          </div>
        </div>
        <div class="title-menu">
          <button class="${activeMenu === "new-game" ? "active" : ""}" data-title-action="new-game">Start</button>
          <button class="${activeMenu === "continue" ? "active" : ""}" data-title-action="continue">Continue</button>
          <button class="${activeMenu === "editor" ? "active" : ""}" data-title-action="editor">Editor</button>
        </div>
        ${isMenuScene ? "" : `
          <section class="title-panel">
            <div class="panel-title">
              <span>${panelTitle}</span>
              <span>${panel === "editor" || panel === "ending" ? "" : saveSlotLabel(selectedSlotId)}</span>
            </div>
            ${panelMarkup}
          </section>
        `}
      </section>
    </div>
  `;

  root.querySelectorAll("[data-title-action]").forEach((button) => {
    button.onclick = () => handleTitleAction(button.dataset.titleAction);
  });
  const newGameName = root.querySelector("#newGameName");
  if (newGameName) {
    newGameName.oninput = (event) => {
      state.shell.newGameDraft.name = event.target.value;
    };
  }
  const newGameClass = root.querySelector("#newGameClass");
  if (newGameClass) {
    newGameClass.onchange = (event) => {
      state.shell.newGameDraft.classIndex = Number(event.target.value || 0);
      render();
    };
  }
  const newGameBackground = root.querySelector("#newGameBackground");
  if (newGameBackground) {
    newGameBackground.onchange = (event) => {
      state.shell.newGameDraft.backgroundId = event.target.value || protagonistBackgrounds[0].id;
      render();
    };
  }
  const newGameLoadout = root.querySelector("#newGameLoadout");
  if (newGameLoadout) {
    newGameLoadout.onchange = (event) => {
      state.shell.newGameDraft.loadoutId = event.target.value || starterLoadouts[0].id;
      render();
    };
  }
  root.querySelectorAll("[data-slot-select]").forEach((button) => {
    button.onclick = () => {
      state.shell.selectedSaveSlotId = button.dataset.slotSelect;
      render();
    };
  });
}

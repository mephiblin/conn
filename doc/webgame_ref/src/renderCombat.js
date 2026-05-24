import { renderCombatSceneMarkup } from "./combatScene3d.js";
import { combatFormulaLabel } from "./diceSkillLoadout.js";

function evaluateTooltipFormula(formula, dieValue, effect) {
  if (formula === "die_times_effect") return dieValue * effect;
  if (formula === "die_plus_effect") return dieValue + effect;
  if (formula === "die_minus_effect") return dieValue - effect;
  if (formula === "die_divide_effect") return Math.floor(dieValue / Math.max(1, effect));
  if (formula === "die_equals_effect") return effect;
  return dieValue;
}

function tooltipResultSuffix(roll = {}) {
  if (roll.kind === "guard" || roll.kind === "defend") return "방어";
  if (roll.kind === "buff") return "강화";
  if (roll.kind === "debuff") return "약화";
  if (roll.kind === "lifesteal") return "흡수";
  if (roll.kind === "summon") return "보류";
  if (roll.kind === "heal" || roll.targetMode === "ally") return "회복";
  if (roll.kind === "support" && roll.targetMode === "enemy") return "약점 노출";
  return "피해";
}

function tooltipFormulaExpression(roll = {}) {
  const dieValue = Math.max(0, Number(roll.value || 0));
  const effect = Math.max(0, Number(roll.effect || 0));
  if (roll.formula === "die_times_effect") return `${dieValue} x ${effect}`;
  if (roll.formula === "die_plus_effect") return `${dieValue} + ${effect}`;
  if (roll.formula === "die_minus_effect") return `${dieValue} - ${effect}`;
  if (roll.formula === "die_divide_effect") return `${dieValue} / ${Math.max(1, effect)}`;
  if (roll.formula === "die_equals_effect") return `${effect}`;
  return `${dieValue}`;
}

export function renderCombatFrame(deps = {}) {
  const {
    state,
    documentObject = document,
    inventoryOverlayOpen = () => false,
    inventoryOverlayMarkup = () => "",
    items = {},
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
    inventoryEntryItemId = () => "",
    inventoryEntryOverlayMarkupImpl = () => "",
    currentCombatHero = () => null,
    livingCombatEnemies = () => [],
    combatConsumableEntries = () => [],
  } = deps;

  const el = documentObject.getElementById("combat");
  if (!el) return;

  if (!state.combat) {
    el.innerHTML = `<div class="combat-box"><div class="panel-title"><span>전투</span><button id="openCombatInventoryBtn">가방</button></div>현재 전투가 없다.</div>`;
    return;
  }

  const activeHero = currentCombatHero();
  const livingEnemies = livingCombatEnemies();
  const diceState = state.combat.diceState || null;
  const dicePhase = String(diceState?.phase || "");
  const reelMotionPhase = ["spin", "spinning", "stopping", "reveal"].includes(dicePhase)
    ? dicePhase
    : (diceState?.isSpinning ? "spinning" : (diceState?.isStopping ? "stopping" : ""));
  const isReelSpinning = reelMotionPhase === "spin" || reelMotionPhase === "spinning";
  const isReelStopping = reelMotionPhase === "stopping";
  const reelsInMotion = isReelSpinning || isReelStopping;
  const selectionLocked = Boolean(
    diceState
    && (diceState.selectionLocked === true
      || diceState.lockSelection === true
      || reelsInMotion
      || dicePhase === "init")
  );
  const canStopReels = Boolean(
    activeHero
    && diceState
    && (diceState.canStop === true
      || diceState.stopEnabled === true
      || isReelSpinning
      || isReelStopping
      || dicePhase === "spin")
  );
  const showStopButton = Boolean(
    activeHero
    && diceState
    && (canStopReels
      || diceState.showStopButton === true
      || reelsInMotion)
  );
  const selectedRollIds = diceState?.selectedRollIds || [];
  const selectedRolls = (diceState?.selectionOrder || [])
    .map((rollId) => diceState?.rolls?.find((roll) => roll.id === rollId))
    .filter(Boolean);
  const skillCooldowns = activeHero?.skillCooldowns || {};
  const usableRollCount = (diceState?.rolls || []).filter((roll) => Math.max(0, Number(skillCooldowns[roll.cooldownKey] || 0)) <= 0).length;
  const canApplyDiceSelection = Boolean(
    activeHero
    && !selectionLocked
    && diceState?.phase === "select"
    && (selectedRollIds.length > 0 || usableRollCount === 0)
  );
  const combatItems = combatConsumableEntries();
  const selectedCombatItemIndex = combatItems.some(({ index }) => index === Number(state.combat.pendingItemIndex || -1))
    ? Number(state.combat.pendingItemIndex || -1)
    : -1;
  const selectedCombatItemEntry = combatItems.find(({ index }) => index === selectedCombatItemIndex) || null;
  const selectedCombatItem = selectedCombatItemEntry?.item || null;
  const diceGuideText = !activeHero
    ? "적 페이즈가 진행 중이다."
    : selectionLocked
      ? (isReelStopping
        ? "STOP 이후 릴이 순서대로 잠기고 있다. 모든 창이 고정되면 선택이 열린다."
        : "릴이 회전 중이다. STOP으로 결과를 확정한 뒤 선택한다.")
      : diceState?.phase === "target"
        ? "선택한 조합에 적 대상 지정이 필요하다."
        : usableRollCount === 0
          ? "현재 멈춘 결과가 모두 쿨타임 중이다. 각 주사위 슬롯은 독립 쿨타임을 가진다."
        : `멈춘 주사위 중 ${Number(diceState?.selectLimit || 3)}개를 선택한다. 쿨타임은 각 주사위 슬롯별로 독립적이다.`;
  const selectedRollSummary = selectedRolls.map((roll, index) => {
    const cooldownTurns = Math.max(0, Number(skillCooldowns[roll.cooldownKey] || 0));
    return `#${index + 1} ${roll.value}:${roll.skillName}${cooldownTurns > 0 ? ` (쿨 ${cooldownTurns})` : ""}`;
  }).join(" · ");
  const reelStepPx = 48;
  const diceCardsMarkup = (diceState?.rolls || []).map((roll, index) => {
    const cooldownTurns = Math.max(0, Number(skillCooldowns[roll.cooldownKey] || 0));
    const isSelected = selectedRollIds.includes(roll.id);
    const selectionOrderIndex = (diceState.selectionOrder || []).indexOf(roll.id);
    const isUnavailable = !activeHero || cooldownTurns > 0 || selectionLocked;
    const computedResult = Math.max(0, evaluateTooltipFormula(roll.formula, Number(roll.value || 0), Number(roll.effect || 0)));
    const formulaLabel = combatFormulaLabel(roll.formula || "");
    const formulaExpression = tooltipFormulaExpression(roll);
    const resultLabel = tooltipResultSuffix(roll);
    const reelTickerFaces = Array.isArray(roll.reelPreviewFaces) && roll.reelPreviewFaces.length
      ? roll.reelPreviewFaces
      : (Array.isArray(roll.reelPreviewValues) && roll.reelPreviewValues.length
        ? roll.reelPreviewValues.map((value) => ({
            value,
            skillName: roll.skillName,
            kind: roll.kind,
          }))
        : [
            { value: ((Number(roll.value) + 4) % 6) + 1, skillName: roll.skillName, kind: roll.kind },
            { value: ((Number(roll.value) + 5) % 6) + 1, skillName: roll.skillName, kind: roll.kind },
            { value: roll.value, skillName: roll.skillName, kind: roll.kind },
            { value: (Number(roll.value) % 6) + 1, skillName: roll.skillName, kind: roll.kind },
            { value: ((Number(roll.value) + 1) % 6) + 1, skillName: roll.skillName, kind: roll.kind },
          ]);
    const faceCount = Math.max(1, reelTickerFaces.length);
    const focusIndex = Math.min(Math.max(0, Number(roll.faceIndex || 0)), faceCount - 1);
    const settledFaces = [
      reelTickerFaces[(focusIndex + faceCount - 1) % faceCount],
      reelTickerFaces[focusIndex],
      reelTickerFaces[(focusIndex + 1) % faceCount],
    ];
    const spinningFaces = [...reelTickerFaces, ...reelTickerFaces, ...reelTickerFaces];
    const reelFaces = reelsInMotion ? spinningFaces : settledFaces;
    const focusCellIndex = reelsInMotion ? faceCount + focusIndex : 1;
    const tooltipDescription = roll.description || "설명 없음";
    const tooltipFormula = `${formulaLabel} · ${formulaExpression} = ${computedResult}`;
    const tooltipResult = `결과 ${computedResult} ${resultLabel}`;
    const nativeTooltip = [
      roll.skillName,
      tooltipDescription,
      `공식 ${tooltipFormula}`,
      tooltipResult,
      cooldownTurns > 0 ? `현재 슬롯 쿨타임 ${cooldownTurns}턴` : "",
    ].filter(Boolean).join(" · ");
    const tooltipMarkup = !reelsInMotion ? `
      <span class="combat-die-tooltip-panel" role="tooltip">
        <strong>${escapeHtml(roll.skillName)}</strong>
        <span>${escapeHtml(tooltipDescription)}</span>
        <span>공식 ${escapeHtml(tooltipFormula)}</span>
        <span>${escapeHtml(tooltipResult)}</span>
        ${cooldownTurns > 0 ? `<span>현재 슬롯 쿨타임 ${escapeHtml(cooldownTurns)}턴</span>` : ""}
      </span>
    ` : "";
    const reelStripStyle = reelsInMotion
      ? `style="--reel-base-offset:${faceCount * reelStepPx}px; --reel-loop-distance:${faceCount * reelStepPx}px;"`
      : "";
    const reelCells = reelFaces.map((face, reelIndex) => `
      <span class="combat-die-reel-cell ${reelIndex === focusCellIndex ? "is-focus" : "is-adjacent"} ${(!reelsInMotion && reelIndex === focusCellIndex && cooldownTurns > 0) ? "is-cooldown-result" : ""}" ${(!reelsInMotion && reelIndex === focusCellIndex) ? `title="${escapeHtml(nativeTooltip)}"` : ""}>
        <span class="combat-die-reel-face-value">${escapeHtml(face?.value ?? roll.value)}</span>
        <span class="combat-die-reel-face-skill">${escapeHtml(face?.skillName || roll.skillName)}</span>
      </span>
    `).join("");
    return `
      <button
        class="combat-die-card combat-die-reel ${isSelected ? "is-selected" : ""} ${cooldownTurns > 0 ? "has-cooldown-result" : ""} ${selectionLocked ? "is-locked" : "is-ready"} ${reelsInMotion ? "is-spinning" : "is-settled"} ${isUnavailable ? "is-unavailable" : ""}"
        data-combat-roll="${roll.id}"
        aria-disabled="${isUnavailable ? "true" : "false"}"
        ${!reelsInMotion ? `title="${escapeHtml(nativeTooltip)}"` : ""}
        ${activeHero ? "" : "disabled"}
      >
        <span class="combat-die-index">R${index + 1}</span>
        ${cooldownTurns > 0 ? `<span class="combat-die-cooldown">슬롯 쿨 ${cooldownTurns}</span>` : ""}
        ${isSelected ? `<span class="combat-die-order">#${selectionOrderIndex + 1}</span>` : ""}
        <span class="combat-die-window">
          <span class="combat-die-window-frame"></span>
          <span class="combat-die-reel-strip ${reelsInMotion ? "is-cycling" : "is-rest"}" ${reelStripStyle}>${reelCells}</span>
        </span>
        ${tooltipMarkup}
      </button>
    `;
  }).join("");

  el.innerHTML = `
    <style>
      #combat .combat-dice-panel {
        background:
          radial-gradient(circle at top, rgba(255, 214, 102, 0.18), transparent 34%),
          linear-gradient(180deg, rgba(23, 27, 39, 0.96), rgba(9, 11, 18, 0.96));
        border: 1px solid rgba(255, 210, 120, 0.24);
        box-shadow: inset 0 1px 0 rgba(255,255,255,0.06), 0 18px 40px rgba(0,0,0,0.24);
      }
      #combat .combat-dice-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        margin-bottom: 12px;
      }
      #combat .combat-dice-title {
        display: flex;
        flex-direction: column;
        gap: 4px;
      }
      #combat .combat-dice-phase {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        font-size: 11px;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: rgba(255, 222, 150, 0.88);
      }
      #combat .combat-dice-phase-dot {
        width: 8px;
        height: 8px;
        border-radius: 999px;
        background: ${selectionLocked ? "#f6b73c" : "#66d39a"};
        box-shadow: 0 0 12px ${selectionLocked ? "rgba(246, 183, 60, 0.6)" : "rgba(102, 211, 154, 0.5)"};
      }
      #combat .combat-dice-tray {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(132px, 1fr));
        gap: 12px;
      }
      #combat .combat-die-card {
        position: relative;
        display: flex;
        flex-direction: column;
        gap: 0;
        min-height: 188px;
        padding: 14px 12px;
        border-radius: 20px;
        border: 1px solid rgba(255,255,255,0.08);
        background:
          linear-gradient(180deg, rgba(255,255,255,0.08), rgba(255,255,255,0.03)),
          linear-gradient(180deg, rgba(52, 60, 78, 0.96), rgba(18, 22, 35, 0.98));
        box-shadow: inset 0 1px 0 rgba(255,255,255,0.12), 0 14px 28px rgba(0,0,0,0.28);
        text-align: left;
      }
      #combat .combat-die-card.is-selected {
        border-color: rgba(110, 207, 255, 0.72);
        box-shadow: inset 0 1px 0 rgba(255,255,255,0.14), 0 0 0 1px rgba(110, 207, 255, 0.28), 0 18px 36px rgba(0,0,0,0.34);
      }
      #combat .combat-die-card.is-locked:not(.is-selected) {
        opacity: 0.76;
      }
      #combat .combat-die-card:disabled {
        cursor: default;
      }
      #combat .combat-die-card.is-unavailable:not(:disabled) {
        cursor: default;
      }
      #combat .combat-die-index,
      #combat .combat-die-order,
      #combat .combat-die-cooldown {
        position: absolute;
        z-index: 2;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.04em;
      }
      #combat .combat-die-index {
        top: 12px;
        left: 12px;
        color: rgba(255,255,255,0.56);
      }
      #combat .combat-die-order,
      #combat .combat-die-cooldown {
        top: 12px;
        right: 12px;
        padding: 4px 8px;
        border-radius: 999px;
        background: rgba(7, 12, 22, 0.7);
      }
      #combat .combat-die-order {
        color: #6ecfff;
      }
      #combat .combat-die-cooldown {
        color: #ffc56b;
      }
      #combat .combat-die-window {
        position: relative;
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 156px;
        border-radius: 18px;
        overflow: hidden;
        background:
          linear-gradient(180deg, rgba(255,255,255,0.94), rgba(208, 214, 228, 0.96));
        color: #0f1522;
        box-shadow: inset 0 2px 10px rgba(15, 21, 34, 0.16);
      }
      #combat .combat-die-window-frame {
        position: absolute;
        inset: 0;
        background:
          linear-gradient(180deg, rgba(20, 27, 42, 0.2), transparent 18%, transparent 82%, rgba(20, 27, 42, 0.2)),
          linear-gradient(180deg, transparent calc(50% - 24px), rgba(194, 140, 45, 0.26) calc(50% - 24px), rgba(194, 140, 45, 0.26) calc(50% + 24px), transparent calc(50% + 24px)),
          linear-gradient(180deg, rgba(0,0,0,0.12), transparent 16%, transparent 84%, rgba(0,0,0,0.12));
      }
      #combat .combat-die-reel-strip {
        position: absolute;
        top: 9px;
        left: 0;
        right: 0;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 6px;
        transform: translateY(calc(-1 * var(--reel-base-offset, 0px)));
        will-change: transform;
      }
      #combat .combat-die-card.is-spinning .combat-die-reel-strip.is-cycling {
        animation: combat-reel-spin 0.82s linear infinite;
      }
      #combat .combat-die-reel-cell {
        width: 94px;
        min-height: 42px;
        height: 42px;
        border-radius: 10px;
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 2px;
        padding: 4px 8px;
        font-weight: 700;
        color: rgba(15, 21, 34, 0.38);
        background: rgba(255,255,255,0.24);
        transition: transform 120ms ease, opacity 120ms ease, background 120ms ease, color 120ms ease;
      }
      #combat .combat-die-reel-cell.is-adjacent {
        opacity: 0.78;
      }
      #combat .combat-die-reel-cell.is-focus {
        color: rgba(15, 21, 34, 0.92);
        background: rgba(255,255,255,0.8);
        box-shadow: 0 0 0 1px rgba(15, 21, 34, 0.08);
        transform: scale(1.03);
      }
      #combat .combat-die-reel-cell.is-focus.is-cooldown-result {
        color: rgba(84, 42, 8, 0.96);
        background: linear-gradient(180deg, rgba(255, 225, 177, 0.96), rgba(255, 201, 133, 0.94));
        box-shadow: 0 0 0 1px rgba(166, 88, 14, 0.24), 0 6px 18px rgba(166, 88, 14, 0.18);
      }
      #combat .combat-die-reel-face-value {
        font-size: 18px;
        font-weight: 900;
        line-height: 1;
      }
      #combat .combat-die-reel-face-skill {
        max-width: 100%;
        font-size: 10px;
        line-height: 1.1;
        text-align: center;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      #combat .combat-die-tooltip-panel {
        position: absolute;
        left: 50%;
        bottom: calc(100% + 8px);
        z-index: 4;
        width: min(200px, calc(100vw - 48px));
        padding: 10px 12px;
        border-radius: 12px;
        border: 1px solid rgba(255, 210, 120, 0.32);
        background: rgba(11, 14, 22, 0.96);
        color: rgba(245, 247, 252, 0.96);
        box-shadow: 0 14px 28px rgba(0,0,0,0.32);
        opacity: 0;
        pointer-events: none;
        transform: translateX(-50%) translateY(6px);
        transition: opacity 120ms ease, transform 120ms ease;
      }
      #combat .combat-die-tooltip-panel strong,
      #combat .combat-die-tooltip-panel span {
        display: block;
      }
      #combat .combat-die-tooltip-panel strong {
        margin-bottom: 4px;
        font-size: 12px;
        color: #ffd27c;
      }
      #combat .combat-die-tooltip-panel span {
        font-size: 11px;
        line-height: 1.35;
      }
      #combat .combat-die-card.is-settled:hover .combat-die-tooltip-panel,
      #combat .combat-die-card.is-settled:focus-visible .combat-die-tooltip-panel {
        opacity: 1;
        transform: translateX(-50%) translateY(0);
      }
      #combat .combat-selection-actions {
        display: flex;
        flex-wrap: wrap;
        justify-content: flex-end;
        gap: 8px;
      }
      #combat .combat-stop-btn {
        min-width: 104px;
        border: 1px solid rgba(255, 184, 84, 0.36);
        background: linear-gradient(180deg, rgba(255, 174, 70, 0.24), rgba(115, 48, 18, 0.4));
      }
      #combat .combat-stop-btn:disabled {
        opacity: 0.48;
      }
      #combat .combat-slot-guide {
        margin: 0 0 12px;
        padding: 10px 12px;
        border-radius: 14px;
        background: rgba(255,255,255,0.04);
        border: 1px solid rgba(255,255,255,0.06);
      }
      @keyframes combat-reel-spin {
        from { transform: translateY(calc(-1 * var(--reel-base-offset, 0px))); }
        to { transform: translateY(calc(-1 * (var(--reel-base-offset, 0px) + var(--reel-loop-distance, 0px)))); }
      }
    </style>
    <div class="combat-stage-shell">
      <div class="combat-stage-grid">
        ${renderCombatSceneMarkup({ enemies: state.combat.enemies, escapeHtml })}
        <section class="combat-command-panel">
        <div class="panel-title">
          <span>${activeHero ? `${escapeHtml(activeHero.name)}의 턴` : "적 턴"}</span>
          <div class="combat-toolbar">
            <button id="openCombatInventoryBtn">가방</button>
            <button data-combat-action="swap" ${activeHero ? "" : "disabled"}>교대</button>
            <button data-combat-action="flee" ${activeHero ? "" : "disabled"}>도주</button>
          </div>
        </div>
        <p class="muted">
          ${activeHero
            ? (selectionLocked
              ? "릴을 멈춰 결과를 확정해야 다음 입력이 열린다."
              : `굴린 주사위 5개 중 ${Number(diceState?.selectLimit || 3)}개를 선택한다.`)
            : "적 페이즈가 진행 중이다."}
        </p>
        <div class="combat-party-strip">
          ${state.party.map((hero) => `
            <div class="combat-party-card ${activeHero?.id === hero.id ? "is-active" : ""} ${hero.hp <= 0 ? "is-defeated" : ""}">
              <strong>${escapeHtml(hero.name)}</strong>
              <span class="muted">${escapeHtml(hero.row)} · ${escapeHtml(hero.cls)}</span>
              <span class="muted">HP ${hero.hp}/${hero.maxHp} · Guard ${Number(hero.guard || 0)}</span>
            </div>
          `).join("")}
        </div>
        </section>

        <section class="combat-dice-panel">
          <div class="combat-dice-header">
            <div class="combat-dice-title">
              <div class="panel-title">
                <span>주사위 릴</span>
                <span>${selectedRollIds.length}/${Number(diceState?.selectLimit || 0)}</span>
              </div>
              <div class="combat-dice-phase">
                <span class="combat-dice-phase-dot"></span>
                <span>${selectionLocked ? (isReelStopping ? "STOPPING" : "SPIN LOCK") : (diceState?.phase === "target" ? "TARGET" : "READY")}</span>
              </div>
            </div>
            <div class="combat-selection-actions">
              ${showStopButton ? `<button data-combat-action="stop" class="combat-stop-btn" ${canStopReels ? "" : "disabled"}>STOP</button>` : ""}
              <button data-combat-action="item" ${activeHero && !selectionLocked ? "" : "disabled"}>아이템</button>
            </div>
          </div>
          ${diceState ? `
            <p class="muted combat-slot-guide">${diceGuideText}</p>
            <div class="combat-dice-tray">
              ${diceCardsMarkup}
            </div>
            <div class="combat-dice-footer">
              <span class="muted">${selectedRollSummary || "선택 없음"}</span>
              <div class="combat-selection-actions">
                <button data-combat-confirm="1" ${canApplyDiceSelection ? "" : "disabled"}>${selectedRollIds.length ? "선택 적용" : "턴 넘김"}</button>
                <button data-combat-cancel="1" class="combat-cancel-btn" ${selectedRollIds.length && !selectionLocked ? "" : "disabled"}>선택 취소</button>
              </div>
            </div>
          ` : `<p class="muted">다음 주사위 굴림을 준비 중이다.</p>`}
        </section>

        <div class="combat-tactical-panel">
          ${diceState?.phase === "target" ? `
            <section class="combat-target-panel">
              <div class="panel-title"><span>대상 선택</span><span>${selectedRolls.length}개 효과</span></div>
              <div class="combat-target-grid">
                ${livingEnemies.map((enemy) => `<button data-combat-dice-target="${enemy.id}" ${enemy.hp > 0 ? "" : "disabled"}>${escapeHtml(enemy.name)}<span class="muted">HP ${enemy.hp}/${enemy.maxHp}</span></button>`).join("")}
              </div>
            </section>
          ` : `
            <section class="combat-guidance-box">
              <div class="panel-title"><span>해석 가이드</span><span>주사위 기반</span></div>
              <p class="muted">선택한 면이 스킬 효과를 직접 결정한다. 공격, 방어, 회복, 약점 노출이 주사위 조합에 따라 함께 나올 수 있다.</p>
            </section>
          `}

          ${activeHero && selectedCombatItemEntry ? `
            <section class="combat-item-panel">
              <div class="panel-title"><span>아이템 대상 선택</span><span>${escapeHtml(inventoryEntryLabel(selectedCombatItemEntry.entry))}</span></div>
              <div class="combat-item-targets">
              ${selectedCombatItem?.throwDamage
                ? (selectedCombatItem?.targetMode === "all_enemies"
                  ? `<button data-combat-item-all-enemies="${selectedCombatItemIndex}">전체 투척</button>`
                  : livingEnemies.map((enemy) => `<button data-combat-item-enemy="${enemy.id}" data-combat-item-index="${selectedCombatItemIndex}" ${enemy.hp > 0 ? "" : "disabled"}>${escapeHtml(enemy.name)} 투척<span class="muted">단일 대상</span></button>`).join(""))
                : state.party.map((hero, heroIndex) => `<button data-combat-item-hero="${heroIndex}" data-combat-item-index="${selectedCombatItemIndex}" ${hero.hp > 0 ? "" : "disabled"}>${escapeHtml(hero.name)} 사용<span class="muted">지원 대상</span></button>`).join("")}
              </div>
            </section>
          ` : ""}

          ${activeHero && state.combat.pendingItemIndex < 0 && combatItems.length ? `
            <section class="combat-item-panel">
              <div class="panel-title"><span>전투 아이템</span><span>${combatItems.length}개</span></div>
              <div class="combat-item-grid">
                ${combatItems.map(({ entry, index, item }) => `
                  <article class="inventory-entry-card">
                    <div class="panel-title">
                      <span>${escapeHtml(inventoryEntryLabel(entry))}</span>
                      <span>${escapeHtml(inventoryEntryKindLabel(entry))}</span>
                    </div>
                    <div class="muted">${escapeHtml(inventoryEntryDetailText(entry) || item?.name || "소모품")}</div>
                    <div class="actions">
                      <button data-combat-item-pick="${index}">선택</button>
                    </div>
                  </article>
                `).join("")}
              </div>
            </section>
          ` : ""}
        </div>

        <section class="combat-log-panel">
          <div class="panel-title"><span>전투 로그</span><span>${livingEnemies.length}/${state.combat.enemies.length} 적 생존</span></div>
          <div class="combat-log-feed">
            ${state.combat.log.slice(-10).reverse().map((entry) => `<p class="muted">${escapeHtml(entry)}</p>`).join("")}
          </div>
        </section>
      </div>
    </div>
    ${inventoryOverlayOpen("combat") ? inventoryOverlayMarkup({
      state,
      items,
      inventoryOverlayOpen: () => inventoryOverlayOpen("combat"),
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
    }) : ""}
  `;
}

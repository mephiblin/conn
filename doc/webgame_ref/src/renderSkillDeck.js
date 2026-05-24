import {
  EMPTY_FACE_FALLBACK_SKILL_ID,
  combatFormulaLabel,
  equippedSkillCount,
  heroSkillLibraryIds,
  skillInventoryCount,
} from "./diceSkillLoadout.js";

function faceDisplaySkillId(face = {}) {
  const skillId = typeof face?.skillId === "string" ? face.skillId.trim() : "";
  return skillId || EMPTY_FACE_FALLBACK_SKILL_ID;
}

function faceDisplaySkillName(face = {}, skillName = (skillId) => skillId || "기술") {
  const skillId = faceDisplaySkillId(face);
  return skillId === EMPTY_FACE_FALLBACK_SKILL_ID ? "기본공격" : skillName(skillId);
}

export function activeSkillDeckHero(state = {}) {
  return state.party?.find((hero) => hero.id === state.skillDeckHeroId) || state.party?.[0] || null;
}

export function renderSkillDeckOverlay(deps = {}) {
  const {
    state,
    escapeHtml = (value) => String(value ?? ""),
    skillName = (skillId) => skillId || "기술",
    skills = {},
  } = deps;

  if (!state?.skillDeckOpen) return "";
  const hero = activeSkillDeckHero(state);
  if (!hero) return "";
  const knownSkills = heroSkillLibraryIds(hero);
  const skillEntries = knownSkills.map((skillId) => {
    const definition = skills[skillId] || {};
    const owned = skillInventoryCount(hero, skillId);
    const equipped = equippedSkillCount(hero, skillId);
    return {
      skillId,
      definition,
      owned,
      equipped,
      available: owned > 0,
    };
  });
  const selectedSkillId = state.skillDeckSelectedSkillId || "";
  const selectedEntry = skillEntries.find((entry) => entry.skillId === selectedSkillId) || null;
  const selectedSkill = selectedEntry?.definition || skills[selectedSkillId] || null;
  const dice = hero.diceLoadout?.dice || [];
  const activeDieIndex = Math.min(Math.max(0, Number(state.skillDeckDieIndex || 0)), Math.max(0, dice.length - 1));

  return `
    <div class="skill-deck-overlay">
      <div class="skill-deck-shell town-runtime-shell">
        <div class="town-runtime-overlay">
          <div class="skill-deck-header town-runtime-hud">
            <div class="town-runtime-title">
              <div class="combat-eyebrow">Skill Deck</div>
              <strong>스킬창 · ${escapeHtml(hero.name)}</strong>
              <span class="muted">렌더 뷰 화면 안에서 바로 덱을 편집한다. 카드를 끌어 주사위 면에 놓거나, 카드를 고른 뒤 면을 눌러 장착한다.</span>
            </div>
            <div class="skill-deck-tools town-runtime-toolbar">
              <span class="muted">주사위 ${Number(hero.diceLoadout?.diceCount || 0)}개 · 턴당 ${Number(hero.diceLoadout?.selectLimit || 0)}선택</span>
              <button data-skill-deck-close="1">닫기</button>
            </div>
          </div>
          <div class="town-runtime-panel">
            <div class="skill-deck-topline">
              <p class="muted">카드를 선택하지 않은 상태에서 장착된 면을 누르면 그 카드를 회수한다.</p>
              <div class="skill-deck-hero-tabs" aria-label="영웅 선택">
                ${state.party.map((entry) => `<button data-skill-deck-hero="${entry.id}" ${entry.id === hero.id ? "disabled" : ""}>${escapeHtml(entry.name)}</button>`).join("")}
              </div>
            </div>
            <div class="skill-deck-layout skill-deck-inventory-layout">
              <section class="skill-library-panel skill-inventory-panel" aria-label="스킬 인벤토리">
                <header class="skill-inventory-heading">
                  <div>
                    <div class="combat-eyebrow">Skill Inventory</div>
                    <h2>스킬 인벤토리</h2>
                  </div>
                  <span class="skill-inventory-count">${knownSkills.length}개</span>
                </header>
                <div class="skill-library-grid skill-inventory-grid">
                  ${skillEntries.map(({ skillId, definition, owned, available }) => {
                    const selected = skillId === selectedSkillId;
                    const interactive = available || selected;
                    return `
                      <button
                        class="skill-card ${selected ? "is-selected" : ""} ${available ? "" : "is-unavailable"}"
                        draggable="${available ? "true" : "false"}"
                        data-skill-card="${skillId}"
                        title="${escapeHtml(definition.description || skillName(skillId))}"
                        ${interactive ? "" : "disabled aria-disabled=\"true\""}
                      >
                        <span class="skill-card-head">
                          <strong>${escapeHtml(definition.name || skillName(skillId))}</strong>
                          <span class="skill-stock-badge ${available ? "" : "is-empty"}">보유 ${owned}</span>
                        </span>
                        <span class="muted">${escapeHtml(definition.kind || "attack")} · ${escapeHtml(definition.targetMode || "enemy")}</span>
                        <span class="muted">${escapeHtml(definition.description || "")}</span>
                        <span class="skill-formula">${escapeHtml(combatFormulaLabel(definition.formula || ""))} · 효과 ${Number(definition.effect || 0)}</span>
                        ${available ? "" : `<span class="skill-card-status">재고 0 · 장착 불가</span>`}
                      </button>
                    `;
                  }).join("")}
                </div>
              </section>
              <section class="skill-dice-panel skill-deck-right-panel" aria-label="장착 주사위">
                <div class="skill-dice-frame">
                  <header class="skill-dice-tabs" aria-label="주사위 탭">
                    ${dice.map((die, dieIndex) => `
                      <button class="skill-dice-tab ${dieIndex === activeDieIndex ? "is-active" : ""}" type="button" data-skill-die-tab="${dieIndex}" aria-selected="${dieIndex === activeDieIndex ? "true" : "false"}">
                        주사위 ${dieIndex + 1}
                      </button>
                    `).join("")}
                    <div class="skill-dice-tab-spacer">
                      ${Number(hero.diceLoadout?.diceCount || 0)}개 · 턴당 ${Number(hero.diceLoadout?.selectLimit || 0)}선택
                    </div>
                  </header>
                  <div class="skill-dice-grid skill-dice-tab-panels">
                    ${dice.map((die, dieIndex) => `
                      <article class="skill-die-card skill-dice-tab-panel ${dieIndex === activeDieIndex ? "is-active" : ""}">
                        <div class="skill-die-title">
                          <span>주사위 ${dieIndex + 1}</span>
                          <span>${escapeHtml(die.id)}</span>
                        </div>
                        <div class="skill-face-grid skill-face-grid--six" aria-label="주사위 ${dieIndex + 1} 면">
                        ${(die.faces || []).map((face, faceIndex) => `
                          <button class="skill-face-slot ${face.skillId ? "is-filled" : "is-empty"}" data-die-face="${escapeHtml(die.id)}" data-face-index="${faceIndex}" title="${escapeHtml(faceDisplaySkillName(face, skillName))}">
                            <span class="skill-face-value">${Number(face.value || faceIndex + 1)}</span>
                            <span class="skill-face-name">${escapeHtml(faceDisplaySkillName(face, skillName))}</span>
                            <span class="skill-face-formula">${escapeHtml(combatFormulaLabel((skills[faceDisplaySkillId(face)] || {}).formula || "die_as_effect"))}</span>
                          </button>
                        `).join("")}
                      </div>
                    </article>
                  `).join("")}
                  </div>
                  <aside class="skill-deck-selected skill-selected-description" aria-label="선택한 스킬 설명">
                    ${selectedSkill ? `
                      <div class="combat-eyebrow">선택한 스킬 설명</div>
                      <strong>${escapeHtml(selectedSkill.name || skillName(selectedSkillId))}</strong>
                      <div class="muted">보유 ${selectedEntry?.owned || 0} · 장착 ${selectedEntry?.equipped || 0}${selectedEntry?.available ? "" : " · 장착 불가"}</div>
                      <div class="muted">종류 ${escapeHtml(selectedSkill.kind || "attack")} · 대상 ${escapeHtml(selectedSkill.targetMode || "enemy")}</div>
                      <div class="muted">효과 ${Number(selectedSkill.effect || 0)} · 공식 ${escapeHtml(combatFormulaLabel(selectedSkill.formula || ""))}</div>
                      <p>${escapeHtml(selectedSkill.description || "설명 없음")}</p>
                    ` : `
                      <div class="combat-eyebrow">선택한 스킬 설명</div>
                      <strong>선택된 카드 없음</strong>
                      <div class="muted">지금은 회수 모드다. 장착된 면을 클릭하면 카드가 인벤토리로 돌아온다.</div>
                    `}
                  </aside>
                </div>
              </section>
            </div>
          </div>
        </div>
      </div>
    </div>
  `;
}

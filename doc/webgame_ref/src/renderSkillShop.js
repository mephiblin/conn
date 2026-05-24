import { equippedSkillCount, skillInventoryCount } from "./diceSkillLoadout.js";

export function activeSkillShopHero(state = {}) {
  return state.party?.find((hero) => hero.id === state.skillShopHeroId) || state.party?.[0] || null;
}

export function renderSkillShopOverlay(deps = {}) {
  const {
    state,
    skills = {},
    escapeHtml = (value) => String(value ?? ""),
    skillName = (skillId) => skillId || "기술",
  } = deps;

  if (!state?.skillShopOpen) return "";
  const hero = activeSkillShopHero(state);
  if (!hero) return "";
  const catalogSkillIds = Array.isArray(state.skillShopSkillIds) && state.skillShopSkillIds.length
    ? state.skillShopSkillIds
    : Object.keys(skills).filter((skillId) => {
        const definition = skills[skillId] || {};
        if (state.skillShopCatalogId) return Array.isArray(definition.catalogIds) && definition.catalogIds.includes(state.skillShopCatalogId);
        return Number(definition.buyPrice || 0) > 0;
      });

  return `
    <div class="skill-shop-overlay">
      <div class="skill-shop-shell">
        <div class="skill-shop-header">
          <div>
            <div class="combat-eyebrow">Skill Shop</div>
            <strong>${escapeHtml(state.skillShopTitle || "스킬 상점")}</strong>
          </div>
          <div class="skill-shop-tools">
            <span class="muted">금화 ${Number(state.resources?.gold || 0)}</span>
            <button data-skill-shop-close="1">닫기</button>
          </div>
        </div>
        <p class="muted">${escapeHtml(state.skillShopNote || "스킬 카드를 사고 팔 수 있다. 장착 중인 카드는 먼저 빼야 판매할 수 있다.")}</p>
        <div class="skill-shop-hero-tabs">
          ${(state.party || []).map((entry) => `<button data-skill-shop-hero="${entry.id}" ${entry.id === hero.id ? "disabled" : ""}>${escapeHtml(entry.name)}</button>`).join("")}
        </div>
        <div class="skill-shop-grid">
          ${catalogSkillIds.map((skillId) => {
            const definition = skills[skillId] || {};
            const looseCount = skillInventoryCount(hero, skillId);
            const equippedCount = equippedSkillCount(hero, skillId);
            const buyPrice = Math.max(0, Number(definition.buyPrice || 0));
            const sellPrice = Math.max(0, Number(definition.sellPrice || 0));
            return `
              <article class="skill-shop-card">
                <div class="panel-title">
                  <span>${escapeHtml(definition.name || skillName(skillId))}</span>
                  <span>쿨 ${Math.max(1, Number(definition.cooldown || 1))}</span>
                </div>
                <div class="muted">${escapeHtml(definition.kind || "attack")} · ${escapeHtml(definition.targetMode || "enemy")} · ${escapeHtml(definition.description || "")}</div>
                <div class="muted">느슨한 카드 ${looseCount} · 장착 ${equippedCount} · 판매가 ${sellPrice} · 구매가 ${buyPrice}</div>
                <div class="actions">
                  <button data-skill-shop-buy="${skillId}" ${buyPrice > 0 && Number(state.resources?.gold || 0) >= buyPrice ? "" : "disabled"}>구매</button>
                  <button data-skill-shop-sell="${skillId}" ${looseCount > 0 && sellPrice > 0 ? "" : "disabled"}>판매</button>
                </div>
              </article>
            `;
          }).join("")}
        </div>
      </div>
    </div>
  `;
}

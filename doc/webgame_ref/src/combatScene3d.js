export function renderCombatSceneMarkup(deps = {}) {
  const {
    enemies = [],
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  const livingEnemies = enemies.filter((enemy) => enemy.hp > 0);
  const frontEnemy = livingEnemies[0] || enemies[0] || null;

  return `
    <div class="combat-scene-shell">
      <div class="combat-scene-hud">
        <div>
          <div class="combat-eyebrow">Encounter</div>
          <strong>${escapeHtml(frontEnemy?.name || "전투")}</strong>
        </div>
        <div class="combat-scene-readout">
          <span>${livingEnemies.length}/${enemies.length} 적 생존</span>
        </div>
      </div>
      <div class="combat-scene-stage">
        <div class="combat-scene-backdrop">
          <div class="combat-scene-vault"></div>
          <div class="combat-scene-floor"></div>
          <div class="combat-scene-ambient"></div>
        ${frontEnemy ? `
          <div class="combat-scene-hpbar"><span class="combat-scene-hpfill" style="width:${Math.max(0, (frontEnemy.hp / Math.max(1, frontEnemy.maxHp)) * 100)}%"></span></div>
          <div class="combat-scene-boss-face ${frontEnemy.hp <= 0 ? "is-defeated" : ""}">
            <div class="combat-monster-aura"></div>
            <div class="combat-monster-core" style="--combat-enemy-tone:${enemyTone(frontEnemy)}"></div>
            <div class="combat-monster-eyes"><span></span><span></span></div>
            <div class="combat-scene-label">
              <strong>${escapeHtml(frontEnemy.name)}</strong>
              <span class="muted">HP ${frontEnemy.hp}/${frontEnemy.maxHp}</span>
            </div>
          </div>
        ` : `<div class="combat-scene-label"><strong>전투 종료</strong></div>`}
          <div class="combat-scene-party-silhouettes">
            <span class="hero hero-front"></span>
            <span class="hero hero-rear"></span>
          </div>
        </div>
      </div>
      <div class="combat-scene-enemy-row">
        ${enemies.map((enemy) => `
          <div class="combat-enemy-chip ${enemy.hp <= 0 ? "is-defeated" : ""} ${frontEnemy?.id === enemy.id ? "is-focused" : ""}">
            <div class="enemy-rank">${escapeHtml(enemy.row || "전열")} · ${escapeHtml(enemy.ai || "enemy")}</div>
            <strong>${escapeHtml(enemy.name)}</strong>
            <div class="enemy-chip-bar"><span style="width:${Math.max(0, (enemy.hp / Math.max(1, enemy.maxHp)) * 100)}%"></span></div>
            <div class="enemy-readout">HP ${enemy.hp}/${enemy.maxHp}</div>
            <div class="enemy-chip-tone" style="--combat-enemy-tone:${enemyTone(enemy)}"></div>
          </div>
        `).join("")}
      </div>
    </div>
  `;
}

function enemyTone(enemy = {}) {
  if (enemy.boss) return "rgba(188, 54, 43, 0.92)";
  if (enemy.ai === "caster") return "rgba(92, 72, 134, 0.9)";
  if (enemy.ai === "guardian") return "rgba(182, 121, 58, 0.9)";
  return "rgba(195, 61, 47, 0.9)";
}

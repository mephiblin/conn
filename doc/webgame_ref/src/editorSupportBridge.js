export function createEditorSupportBridge(deps = {}) {
  const {
    eventEffectFieldMeta = () => ({ fields: [], note: "" }),
    escapeHtml = (value) => String(value ?? ""),
    resourceKeys = [],
    items = {},
  } = deps;

  function eventEffectFlagValueType(effect = {}) {
    if (effect?.value === false) return "boolean_false";
    if (typeof effect?.value === "number") return "number";
    if (typeof effect?.value === "string") return "string";
    return "boolean_true";
  }

  function eventEffectFlagValueText(effect = {}) {
    if (typeof effect?.value === "number") return String(effect.value);
    if (typeof effect?.value === "string") return effect.value;
    return "";
  }

  function renderEventEffectFields(effect, index, prefix) {
    const meta = eventEffectFieldMeta(effect);
    const fields = new Set(meta.fields);
    return `
      <div class="muted">${escapeHtml(meta.note)}</div>
      ${fields.has("message") ? `
        <div class="preset-toolbar">
          <input data-${prefix}-message="${index}" value="${escapeHtml(effect.message || effect.text || "")}" placeholder="message / text" />
        </div>
      ` : ""}
      ${fields.has("flag") ? `
        <div class="preset-toolbar">
          <input data-${prefix}-flag="${index}" value="${escapeHtml(effect.flag || "")}" placeholder="flag" />
          <select data-${prefix}-flag-value-type="${index}">
            ${[
              ["boolean_true", "boolean true"],
              ["boolean_false", "boolean false"],
              ["number", "number"],
              ["string", "string"],
            ].map(([value, label]) => `<option value="${value}" ${eventEffectFlagValueType(effect) === value ? "selected" : ""}>${label}</option>`).join("")}
          </select>
          <input data-${prefix}-flag-value="${index}" value="${escapeHtml(eventEffectFlagValueText(effect))}" placeholder="string 또는 number 값" />
        </div>
      ` : ""}
      ${fields.has("questSeedId") || fields.has("status") ? `
        <div class="preset-toolbar">
          ${fields.has("questSeedId") ? `<input data-${prefix}-seed="${index}" value="${escapeHtml(effect.questSeedId || "")}" placeholder="quest seed id" />` : ""}
          ${fields.has("status") ? `<input data-${prefix}-status="${index}" value="${escapeHtml(effect.status || "")}" placeholder="status / status effect" />` : ""}
        </div>
      ` : ""}
      ${fields.has("npcPlacementId") || fields.has("serviceIndex") ? `
        <div class="preset-toolbar">
          ${fields.has("npcPlacementId") ? `<input data-${prefix}-npc-placement="${index}" value="${escapeHtml(effect.npcPlacementId || "")}" placeholder="npc placement id" />` : ""}
          ${fields.has("serviceIndex") ? `<input data-${prefix}-service-index="${index}" type="number" min="0" step="1" value="${effect.serviceIndex ?? 0}" placeholder="service index" />` : ""}
        </div>
      ` : ""}
      ${fields.has("resource") || fields.has("amount") || fields.has("quantity") || fields.has("minHp") ? `
        <div class="preset-toolbar">
          ${fields.has("resource") ? `
            <select data-${prefix}-resource="${index}">
              <option value="" ${(effect.resource || "") ? "" : "selected"}>(resource 없음)</option>
              ${[...resourceKeys].map((resource) => `<option value="${resource}" ${effect.resource === resource ? "selected" : ""}>${resource}</option>`).join("")}
            </select>
          ` : ""}
          ${fields.has("amount") ? `<input data-${prefix}-amount="${index}" type="number" step="1" value="${effect.amount ?? 0}" placeholder="amount" />` : ""}
          ${fields.has("quantity") ? `<input data-${prefix}-amount="${index}" type="number" step="1" min="1" value="${effect.quantity ?? 1}" placeholder="quantity" />` : ""}
          ${fields.has("minHp") ? `<input data-${prefix}-min-hp="${index}" type="number" step="1" value="${effect.minHp ?? 0}" placeholder="min HP" />` : ""}
        </div>
      ` : ""}
      ${fields.has("itemId") ? `
        <div class="preset-toolbar">
          <select data-${prefix}-item="${index}">
            <option value="" ${(effect.itemId || "") ? "" : "selected"}>(item 없음)</option>
            ${Object.entries(items).map(([itemId, item]) => `<option value="${itemId}" ${effect.itemId === itemId ? "selected" : ""}>${itemId} · ${item.name}</option>`).join("")}
          </select>
        </div>
      ` : ""}
    `;
  }

  function npcHookJson(npc, key, fallback) {
    return JSON.stringify(npc?.[key] ?? fallback, null, 2);
  }

  function buildQuestSeedRegistry(npcDefinitions = {}) {
    const byId = {};
    const duplicates = new Set();
    Object.entries(npcDefinitions || {}).forEach(([npcId, npc]) => {
      (npc?.questSeeds || []).forEach((seed, index) => {
        const seedId = String(seed?.id || "").trim();
        if (!seedId) return;
        if (byId[seedId]) duplicates.add(seedId);
        else byId[seedId] = { npcId, index, seed };
      });
    });
    return { byId, duplicates };
  }

  function dialogueStepLookup(dialogue = {}) {
    const steps = Array.isArray(dialogue?.steps) ? dialogue.steps : [];
    const ids = new Set();
    return {
      steps,
      ids,
    };
  }

  return {
    eventEffectFlagValueType,
    eventEffectFlagValueText,
    renderEventEffectFields,
    npcHookJson,
    buildQuestSeedRegistry,
    dialogueStepLookup,
  };
}

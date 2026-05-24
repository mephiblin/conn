export function createInventoryRuntimeBridge(deps = {}) {
  const {
    getState = () => ({}),
    items = {},
    vendors = {},
    lootTables = {},
    rarityDefinitions = {},
    buildSampleItemPreview = () => null,
    makeGeneratedItemInstance = () => null,
    baseItemShouldUseInstance = () => false,
    createBaseItemInstance = () => "",
    addLog = () => {},
  } = deps;

  function normalizeInventoryEntry(entry) {
    if (typeof entry === "string") {
      return baseItemShouldUseInstance(entry) ? createBaseItemInstance(entry) : entry;
    }
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return "";
    if (entry.kind === "item_instance") {
      return {
        kind: "item_instance",
        itemId: entry.itemId || "",
        identified: entry.identified !== false,
        cursed: Boolean(entry.cursed),
      };
    }
    if (entry.kind !== "generated_item") return "";
    return {
      kind: "generated_item",
      instanceId: entry.instanceId || `generated_item_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
      itemId: entry.itemId || "",
      name: entry.name || items[entry.itemId]?.name || entry.itemId || "아이템",
      rarityId: entry.rarityId || "",
      rarityLabel: entry.rarityLabel || entry.rarityId || "",
      affixPoolId: entry.affixPoolId || "",
      affixes: JSON.parse(JSON.stringify(entry.affixes || [])),
      stats: JSON.parse(JSON.stringify(entry.stats || {})),
      valueEstimate: Number(entry.valueEstimate || 0),
      identified: entry.identified !== false,
      cursed: Boolean(entry.cursed ?? (Number(entry.stats?.curse || 0) > 0)),
    };
  }

  function normalizeInventoryList(list = []) {
    return (Array.isArray(list) ? list : [])
      .map((entry) => normalizeInventoryEntry(entry))
      .filter((entry) => entry && (typeof entry === "string" || entry.itemId));
  }

  function inventoryEntryItemId(entry) {
    if (typeof entry === "string") return entry;
    if (entry?.kind === "generated_item" || entry?.kind === "item_instance") return entry.itemId || "";
    return "";
  }

  function inventoryEntryBaseName(entry) {
    const itemId = inventoryEntryItemId(entry);
    return items[itemId]?.name || itemId || "아이템";
  }

  function inventoryEntryIsIdentified(entry) {
    if (typeof entry === "string") return true;
    if (entry?.kind === "generated_item" || entry?.kind === "item_instance") return entry.identified !== false;
    return true;
  }

  function inventoryEntryIsCursed(entry) {
    if (typeof entry === "string") return Number(items[entry]?.curse || 0) > 0;
    if (entry?.kind === "generated_item") return Boolean(entry.cursed ?? (Number(entry.stats?.curse || 0) > 0));
    if (entry?.kind === "item_instance") return Boolean(entry.cursed);
    return false;
  }

  function inventoryEntryKindLabel(entry) {
    const item = items[inventoryEntryItemId(entry)];
    if (item?.kind === "equipment") return "장비";
    if (item?.kind === "artifact") return "유물";
    if (item?.kind === "quest") return "의뢰품";
    if (item?.kind === "consumable") return "소모품";
    if (item?.kind === "key") return "열쇠";
    return "아이템";
  }

  function identifyInventoryEntry(entry) {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return false;
    if (entry.kind !== "generated_item" && entry.kind !== "item_instance") return false;
    entry.identified = true;
    return true;
  }

  function purifyInventoryEntry(entry) {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return false;
    if (entry.kind !== "generated_item" && entry.kind !== "item_instance") return false;
    if (!inventoryEntryIsCursed(entry)) return false;
    entry.cursed = false;
    if (entry.kind === "generated_item" && entry.stats) entry.stats.curse = 0;
    return true;
  }

  function heroEquipmentEntries(hero) {
    return hero?.equipment && typeof hero.equipment === "object" ? hero.equipment : {};
  }

  function allInventoryAndEquipmentEntries() {
    const state = getState();
    const inventoryEntries = state.inventory.map((entry, index) => ({ scope: "inventory", owner: null, slot: "", index, entry }));
    const equipmentEntries = state.party.flatMap((hero, heroIndex) => Object.entries(heroEquipmentEntries(hero)).map(([slot, entry]) => ({
      scope: "equipment",
      owner: hero,
      heroIndex,
      slot,
      index: -1,
      entry,
    })));
    return [...inventoryEntries, ...equipmentEntries];
  }

  function inventoryEntryLabel(entry) {
    if (!inventoryEntryIsIdentified(entry)) return `미감정 ${inventoryEntryKindLabel(entry)}`;
    if (typeof entry === "string") return items[entry]?.name || entry;
    const curseLabel = inventoryEntryIsCursed(entry) ? "[저주] " : "";
    if (entry?.kind === "item_instance") return `${curseLabel}${inventoryEntryBaseName(entry)}`;
    if (entry?.kind === "generated_item") {
      const rarity = entry.rarityLabel ? `[${entry.rarityLabel}] ` : "";
      return `${curseLabel}${rarity}${entry.name || items[entry.itemId]?.name || entry.itemId || "아이템"}`;
    }
    return "아이템";
  }

  function inventoryEntryEquipmentSlot(entry) {
    const itemId = inventoryEntryItemId(entry);
    const item = items[itemId];
    if (!item || (item.kind !== "equipment" && item.kind !== "artifact")) return "";
    return item.slot || "weapon";
  }

  function inventoryEntryStatPayload(entry) {
    if (entry?.kind === "generated_item") {
      return {
        attack: Number(entry.stats?.attack || 0),
        defense: Number(entry.stats?.defense || 0),
      };
    }
    const itemId = inventoryEntryItemId(entry);
    const item = items[itemId];
    return {
      attack: Number(item?.attack || 0),
      defense: Number(item?.defense || 0),
    };
  }

  function inventoryEntryDetailParts(entry) {
    const stats = inventoryEntryStatPayload(entry);
    const item = items[inventoryEntryItemId(entry)];
    const parts = [];
    if (entry?.kind === "generated_item" && entry.rarityLabel) parts.push(`희귀 ${entry.rarityLabel}`);
    if (!inventoryEntryIsIdentified(entry)) parts.push("미감정");
    if (inventoryEntryIsCursed(entry)) parts.push("저주");
    if (item?.slot) parts.push(`슬롯 ${item.slot}`);
    if (stats.attack) parts.push(`공 ${stats.attack >= 0 ? "+" : ""}${stats.attack}`);
    if (stats.defense) parts.push(`방 ${stats.defense >= 0 ? "+" : ""}${stats.defense}`);
    if (item?.throwDamage) parts.push(`투척 ${Number(item.throwDamage || 0)}`);
    if (item?.targetMode === "all_enemies") parts.push("광역");
    if (entry?.kind === "generated_item" && entry.affixes?.length) parts.push(`affix ${entry.affixes.length}`);
    return parts;
  }

  function inventoryEntryDetailText(entry) {
    return inventoryEntryDetailParts(entry).join(" · ");
  }

  function compareEquipmentCandidate(hero, candidateEntry) {
    const slot = inventoryEntryEquipmentSlot(candidateEntry);
    if (!slot) return null;
    const current = heroEquipmentEntries(hero)[slot] || null;
    const currentStats = inventoryEntryStatPayload(current);
    const nextStats = inventoryEntryStatPayload(candidateEntry);
    return {
      slot,
      current,
      attackDelta: Number(nextStats.attack || 0) - Number(currentStats.attack || 0),
      defenseDelta: Number(nextStats.defense || 0) - Number(currentStats.defense || 0),
    };
  }

  function compareDeltaText(value, label) {
    if (!value) return "";
    return `${label} ${value > 0 ? "+" : ""}${value}`;
  }

  function compareCandidateSummary(hero, candidateEntry) {
    const compare = compareEquipmentCandidate(hero, candidateEntry);
    if (!compare) return "";
    const parts = [`${compare.slot}`];
    const atk = compareDeltaText(compare.attackDelta, "공");
    const def = compareDeltaText(compare.defenseDelta, "방");
    if (atk) parts.push(atk);
    if (def) parts.push(def);
    if (!atk && !def) parts.push("변화 없음");
    if (compare.current) parts.push(`현재 ${inventoryEntryLabel(compare.current)}`);
    else parts.push("현재 장비 없음");
    return parts.join(" · ");
  }

  function inventorySummaryText(list = getState().inventory) {
    return normalizeInventoryList(list).map((entry) => inventoryEntryLabel(entry)).join(", ") || "비어 있음";
  }

  function inventoryFilterOptions() {
    return [
      ["all", "전체"],
      ["equipment", "장비"],
      ["consumable", "소모품"],
      ["quest", "의뢰/열쇠"],
      ["cursed", "저주"],
      ["unidentified", "미감정"],
    ];
  }

  function inventoryEntryMatchesFilter(entry, filterId = getState().inventoryPanelFilter || "all") {
    const item = items[inventoryEntryItemId(entry)] || {};
    if (filterId === "equipment") return inventoryEntryEquipmentSlot(entry) !== "";
    if (filterId === "consumable") return item.kind === "consumable";
    if (filterId === "quest") return item.kind === "quest" || item.kind === "key";
    if (filterId === "cursed") return inventoryEntryIsCursed(entry);
    if (filterId === "unidentified") return !inventoryEntryIsIdentified(entry);
    return true;
  }

  function inventorySortOptions() {
    return [
      ["default", "기본"],
      ["name", "이름"],
      ["kind", "종류"],
      ["power", "공/방"],
    ];
  }

  function normalizeInventorySearchQuery(query = "") {
    return String(query || "").trim().toLowerCase();
  }

  function inventoryEntryMatchesSearch(entry, query = getState().inventoryPanelQuery || "") {
    const normalizedQuery = normalizeInventorySearchQuery(query);
    if (!normalizedQuery) return true;
    const haystack = [
      inventoryEntryLabel(entry),
      inventoryEntryBaseName(entry),
      inventoryEntryKindLabel(entry),
      inventoryEntryDetailText(entry),
    ].join(" ").toLowerCase();
    return haystack.includes(normalizedQuery);
  }

  function sortedFilteredInventoryEntries() {
    const state = getState();
    const filterId = state.inventoryPanelFilter || "all";
    const sortId = state.inventoryPanelSort || "default";
    const query = state.inventoryPanelQuery || "";
    const entries = normalizeInventoryList(state.inventory)
      .map((entry, index) => ({ entry, index }))
      .filter(({ entry }) => inventoryEntryMatchesFilter(entry, filterId))
      .filter(({ entry }) => inventoryEntryMatchesSearch(entry, query));
    if (sortId === "default") return entries;
    return entries.slice().sort((left, right) => {
      if (sortId === "name") return inventoryEntryLabel(left.entry).localeCompare(inventoryEntryLabel(right.entry), "ko");
      if (sortId === "kind") return `${inventoryEntryKindLabel(left.entry)}:${inventoryEntryLabel(left.entry)}`.localeCompare(`${inventoryEntryKindLabel(right.entry)}:${inventoryEntryLabel(right.entry)}`, "ko");
      if (sortId === "power") {
        const leftPower = Number(inventoryEntryStatPayload(left.entry).attack || 0) + Number(inventoryEntryStatPayload(left.entry).defense || 0);
        const rightPower = Number(inventoryEntryStatPayload(right.entry).attack || 0) + Number(inventoryEntryStatPayload(right.entry).defense || 0);
        return rightPower - leftPower || inventoryEntryLabel(left.entry).localeCompare(inventoryEntryLabel(right.entry), "ko");
      }
      return 0;
    });
  }

  function pushInventoryEntry(entry) {
    const state = getState();
    const normalized = normalizeInventoryEntry(entry);
    if (!normalized) return null;
    state.inventory.push(normalized);
    return normalized;
  }

  function pushInventoryItemId(itemId) {
    const state = getState();
    if (!itemId) return null;
    const entry = baseItemShouldUseInstance(itemId) ? createBaseItemInstance(itemId) : itemId;
    state.inventory.push(entry);
    return entry;
  }

  function inventoryManualReorderEnabled() {
    const state = getState();
    return (state.inventoryPanelFilter || "all") === "all"
      && (state.inventoryPanelSort || "default") === "default"
      && !normalizeInventorySearchQuery(state.inventoryPanelQuery || "");
  }

  function reorderInventoryEntries(fromIndex, toIndex) {
    const state = getState();
    const list = normalizeInventoryList(state.inventory);
    if (fromIndex === toIndex) return false;
    if (fromIndex < 0 || toIndex < 0 || fromIndex >= list.length || toIndex >= list.length) return false;
    const [entry] = list.splice(fromIndex, 1);
    if (!entry) return false;
    list.splice(toIndex, 0, entry);
    state.inventory = list;
    return true;
  }

  function removeInventoryEntryAt(index) {
    const state = getState();
    if (index < 0 || index >= state.inventory.length) return null;
    return state.inventory.splice(index, 1)[0] || null;
  }

  function useInventoryEntryOnHero(heroIndex, inventoryIndex) {
    const state = getState();
    const hero = state.party[heroIndex];
    const entry = state.inventory[inventoryIndex];
    const itemId = inventoryEntryItemId(entry);
    const item = items[itemId];
    if (!hero || !item) return false;
    if (item.kind !== "consumable") {
      addLog("이 item은 직접 사용할 수 없다.");
      return false;
    }
    if (item.heal) {
      if (!removeInventoryEntryAt(inventoryIndex)) return false;
      hero.hp = Math.min(hero.maxHp, hero.hp + Number(item.heal || 0));
      addLog(`${hero.name}이 ${item.name}를 사용했다. (HP +${Number(item.heal || 0)})`);
      return true;
    }
    if (item.cure) {
      if (!hero.status.includes(item.cure)) {
        addLog(`${hero.name}에게 ${item.cure} 상태가 없어 ${item.name}를 아꼈다.`);
        return false;
      }
      if (!removeInventoryEntryAt(inventoryIndex)) return false;
      hero.status = hero.status.filter((status) => status !== item.cure);
      addLog(`${hero.name}이 ${item.name}를 사용해 ${item.cure} 상태를 치료했다.`);
      return true;
    }
    addLog(`${item.name}의 사용 효과가 아직 연결되지 않았다.`);
    return false;
  }

  function hasInventoryItem(itemId) {
    return getState().inventory.some((entry) => inventoryEntryItemId(entry) === itemId);
  }

  function consumeInventoryItem(itemId) {
    const state = getState();
    const index = state.inventory.findIndex((entry) => inventoryEntryItemId(entry) === itemId);
    if (index < 0) return null;
    return state.inventory.splice(index, 1)[0] || null;
  }

  function vendorInventoryEntryItemId(entry) {
    if (typeof entry === "string") return entry;
    if (entry?.itemId) return entry.itemId;
    return "";
  }

  function vendorInventoryEntryLabel(entry) {
    if (typeof entry === "string") return items[entry]?.name || entry;
    if (entry?.generated) {
      const rarity = entry.rarityId ? `[${rarityDefinitions[entry.rarityId]?.label || entry.rarityId}] ` : "";
      return `${rarity}${items[entry.itemId]?.name || entry.itemId || "아이템"}`;
    }
    return items[entry?.itemId]?.name || entry?.itemId || "아이템";
  }

  function buildGeneratedRewardInstance(entry) {
    if (!entry?.generated || !entry.itemId || !entry.rarityId || !entry.affixPoolId) return null;
    const preview = buildSampleItemPreview(entry.itemId, entry.rarityId, entry.affixPoolId);
    return makeGeneratedItemInstance(preview);
  }

  function grantVendorInventoryEntry(entry) {
    if (typeof entry === "string") return pushInventoryItemId(entry);
    if (entry?.generated) return pushInventoryEntry(buildGeneratedRewardInstance(entry));
    if (entry?.itemId) return pushInventoryItemId(entry.itemId);
    return null;
  }

  function equipmentEntryLabel(entry) {
    if (!entry) return "비어 있음";
    const slot = inventoryEntryEquipmentSlot(entry);
    return `${slot} · ${inventoryEntryLabel(entry)}`;
  }

  function availableEquipmentEntries() {
    return getState().inventory
      .map((entry, index) => ({ entry, index, slot: inventoryEntryEquipmentSlot(entry) }))
      .filter((entry) => entry.slot);
  }

  function equipInventoryEntryToHero(heroIndex, inventoryIndex) {
    const state = getState();
    const hero = state.party[heroIndex];
    if (!hero) return false;
    const entry = state.inventory[inventoryIndex];
    const slot = inventoryEntryEquipmentSlot(entry);
    if (!slot) {
      addLog("장착할 수 없는 item이다.");
      return false;
    }
    hero.equipment = heroEquipmentEntries(hero);
    const previous = hero.equipment[slot] || null;
    if (previous && inventoryEntryIsCursed(previous)) {
      addLog(`${hero.name}의 ${inventoryEntryBaseName(previous)}에는 저주가 걸려 있어 해제할 수 없다.`);
      return false;
    }
    const removed = removeInventoryEntryAt(inventoryIndex);
    if (!removed) return false;
    const previousStats = inventoryEntryStatPayload(previous);
    const nextStats = inventoryEntryStatPayload(removed);
    hero.atk = Math.max(0, Number(hero.atk || 0) - previousStats.attack + nextStats.attack);
    hero.def = Math.max(0, Number(hero.def || 0) - previousStats.defense + nextStats.defense);
    hero.equipment[slot] = JSON.parse(JSON.stringify(removed));
    if (previous) pushInventoryEntry(previous);
    addLog(`${hero.name}이 ${inventoryEntryLabel(removed)}를 장착했다.${previous ? ` (${inventoryEntryLabel(previous)} 해제)` : ""}`);
    return true;
  }

  function matchesRule(when = {}, context = {}) {
    const state = getState();
    if (when.minFloor != null && (context.floor ?? state.player.floor) < when.minFloor) return false;
    if (when.maxFloor != null && (context.floor ?? state.player.floor) > when.maxFloor) return false;
    if (when.floorAtLeast != null && (context.floor ?? state.player.floor) < when.floorAtLeast) return false;
    if (when.minXp != null && (context.enemy?.xp || 0) < when.minXp) return false;
    if (when.maxXp != null && (context.enemy?.xp || 0) > when.maxXp) return false;
    if (when.boss != null && Boolean(context.enemy?.boss) !== Boolean(when.boss)) return false;
    if (when.ai && context.enemy?.ai !== when.ai) return false;
    if (when.enemyId && context.enemy?.id !== when.enemyId) return false;
    if (when.bossesDefeatedAtLeast != null && (context.bossesDefeated || 0) < when.bossesDefeatedAtLeast) return false;
    return true;
  }

  function pickWeighted(entries = []) {
    const pool = entries.filter(Boolean).filter((entry) => (entry.weight ?? 1) > 0);
    const totalWeight = pool.reduce((sum, entry) => sum + (entry.weight ?? 1), 0);
    if (!pool.length || totalWeight <= 0) return null;
    let cursor = Math.random() * totalWeight;
    for (const entry of pool) {
      cursor -= entry.weight ?? 1;
      if (cursor <= 0) return entry;
    }
    return pool[pool.length - 1] || null;
  }

  function pushLootReward(rewards, reward) {
    if (!reward) return;
    if (typeof reward === "string") {
      rewards.push(reward);
      return;
    }
    if (reward.generated) {
      const quantity = Math.max(1, Number(reward.quantity || 1));
      for (let index = 0; index < quantity; index += 1) {
        const generated = buildGeneratedRewardInstance(reward);
        if (generated) rewards.push(generated);
      }
      return;
    }
    if (!reward.itemId) return;
    const quantity = Math.max(1, Number(reward.quantity || 1));
    for (let index = 0; index < quantity; index += 1) rewards.push(reward.itemId);
  }

  function lootItems(tableId, context = {}) {
    const table = lootTables[tableId];
    if (!table) return [];
    const rewards = [];
    (table.guaranteed || []).forEach((entry) => pushLootReward(rewards, entry));
    const rolls = table.rolls || 1;
    for (let roll = 0; roll < rolls; roll += 1) {
      let entries = Array.isArray(table.entries) ? table.entries : [];
      if (Array.isArray(table.tierEntries) && table.tierEntries.length) {
        const tier = pickWeighted(table.tierEntries.filter((entry) => matchesRule(entry.when, context)));
        if (tier?.entries?.length) entries = tier.entries;
      }
      const reward = pickWeighted(entries.filter((entry) => matchesRule(entry.when, context)));
      pushLootReward(rewards, reward);
    }
    for (const bonus of table.bonusRolls || []) {
      if (Math.random() > Number(bonus.chance || 0)) continue;
      const reward = pickWeighted((bonus.entries || []).filter((entry) => matchesRule(entry.when, context)));
      pushLootReward(rewards, reward);
    }
    return rewards;
  }

  function combatLootTable(enemy) {
    const state = getState();
    const profiles = lootTables.combatRewardProfiles?.default || [];
    const context = {
      enemy,
      floor: state.player.floor,
      bossesDefeated: Object.keys(state.quest?.bossesDefeated || {}).length,
    };
    for (const profile of profiles) {
      if (profile?.tableId && matchesRule(profile.when, context)) return profile.tableId;
    }
    return "loot_combat_bandage";
  }

  function vendorOffer(vendorId) {
    const state = getState();
    const source = vendors[vendorId];
    if (!source) return null;
    const offer = JSON.parse(JSON.stringify(source));
    delete offer.rotation;
    const context = {
      floor: state.player.floor,
      bossesDefeated: Object.keys(state.quest?.bossesDefeated || {}).length,
    };
    for (const rotation of source.rotation || []) {
      if (!matchesRule(rotation.when, context)) continue;
      if (rotation.cost) offer.cost = JSON.parse(JSON.stringify(rotation.cost));
      if (rotation.rewards) offer.rewards = JSON.parse(JSON.stringify(rotation.rewards));
      if (rotation.inventory) offer.inventory = [...rotation.inventory];
      if (rotation.summary) offer.summary = rotation.summary;
    }
    return offer;
  }

  return {
    normalizeInventoryEntry,
    normalizeInventoryList,
    inventoryEntryItemId,
    inventoryEntryBaseName,
    inventoryEntryIsIdentified,
    inventoryEntryIsCursed,
    inventoryEntryKindLabel,
    identifyInventoryEntry,
    purifyInventoryEntry,
    allInventoryAndEquipmentEntries,
    inventoryEntryLabel,
    inventoryEntryDetailParts,
    inventoryEntryDetailText,
    compareEquipmentCandidate,
    compareDeltaText,
    compareCandidateSummary,
    inventorySummaryText,
    inventoryFilterOptions,
    inventoryEntryMatchesFilter,
    inventorySortOptions,
    normalizeInventorySearchQuery,
    inventoryEntryMatchesSearch,
    sortedFilteredInventoryEntries,
    pushInventoryEntry,
    pushInventoryItemId,
    inventoryManualReorderEnabled,
    reorderInventoryEntries,
    useInventoryEntryOnHero,
    hasInventoryItem,
    consumeInventoryItem,
    vendorInventoryEntryItemId,
    vendorInventoryEntryLabel,
    buildGeneratedRewardInstance,
    grantVendorInventoryEntry,
    inventoryEntryEquipmentSlot,
    inventoryEntryStatPayload,
    equipmentEntryLabel,
    removeInventoryEntryAt,
    heroEquipmentEntries,
    availableEquipmentEntries,
    equipInventoryEntryToHero,
    matchesRule,
    pickWeighted,
    pushLootReward,
    lootItems,
    combatLootTable,
    vendorOffer,
  };
}

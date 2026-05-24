export function createEditorContentStateBridge(deps = {}) {
  const {
    getState = () => ({}),
    classes = [],
    questDefinitions = {},
    monsters = {},
    skills = {},
    mapProfiles = [],
    items = {},
    vendors = {},
    lootTables = {},
    rarityDefinitions = {},
    affixDefinitions = {},
    affixPoolDefinitions = {},
    syncPartyClassDefinitions = () => {},
    vendorInventoryEntryLabel = () => "",
    escapeHtml = (value) => String(value),
  } = deps;

  function activeClassDefinitionIndex() {
    const state = getState();
    return Math.min(Math.max(0, Number(state.selectedClassDefinitionIndex || 0)), Math.max(0, classes.length - 1));
  }

  function activeClassDefinition() {
    return classes[activeClassDefinitionIndex()] || null;
  }

  function updateClassDefinition(index, updater) {
    if (!classes[index]) return;
    updater(classes[index]);
    syncPartyClassDefinitions();
  }

  function activeQuestDefinitionId() {
    const state = getState();
    return questDefinitions[state.selectedQuestDefinitionId] ? state.selectedQuestDefinitionId : Object.keys(questDefinitions)[0] || "";
  }

  function activeQuestDefinition() {
    const questId = activeQuestDefinitionId();
    return questId ? questDefinitions[questId] || null : null;
  }

  function updateQuestDefinition(questId, updater) {
    if (!questId || !questDefinitions[questId]) return;
    updater(questDefinitions[questId]);
  }

  function questDefinitionsJson() {
    return JSON.stringify(questDefinitions, null, 2);
  }

  function uniqueQuestDefinitionId(baseId = "quest_custom") {
    const slug = (baseId || "quest_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "quest_custom";
    const existing = new Set(Object.keys(questDefinitions));
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createQuestDefinitionTemplate() {
    return {
      name: "새 의뢰",
      description: "게시판에서 수주하는 원정 의뢰다.",
      mapKind: "twisted_temple",
      startFloor: 1,
      conditions: { kind: "bosses_defeated", bossesDefeatedAtLeast: 1, summary: "보스급 적 1체를 쓰러뜨린다." },
      rewards: { gold: 10, xp: 5, items: [], flag: "quest_custom_rewarded", value: true },
      return: { label: "귀환", rewardOnReturn: true },
      generator: { archetype: "boss_hunt", targetMonsterId: "" },
    };
  }

  function mapKindOptionEntries(currentMapKind = "") {
    const byKind = new Map();
    (Array.isArray(mapProfiles) ? mapProfiles : []).forEach((profile) => {
      const mapKind = String(profile?.mapKind || "").trim();
      if (!mapKind) return;
      const existing = byKind.get(mapKind) || {
        mapKind,
        mapKindName: String(profile?.mapKindName || profile?.name || mapKind),
        floors: [],
      };
      existing.mapKindName = existing.mapKindName || String(profile?.mapKindName || profile?.name || mapKind);
      const floor = Math.max(1, Number(profile?.floor || 1));
      if (!existing.floors.includes(floor)) existing.floors.push(floor);
      byKind.set(mapKind, existing);
    });
    if (currentMapKind && !byKind.has(currentMapKind)) {
      byKind.set(currentMapKind, { mapKind: currentMapKind, mapKindName: currentMapKind, floors: [] });
    }
    return Array.from(byKind.values())
      .map((entry) => ({ ...entry, floors: entry.floors.sort((a, b) => a - b) }))
      .sort((a, b) => (a.floors[0] || 999) - (b.floors[0] || 999));
  }

  function mapKindLabel(mapKind = "") {
    return mapKindOptionEntries(mapKind).find((entry) => entry.mapKind === mapKind)?.mapKindName || mapKind || "미지의 지역";
  }

  function monsterMatchesQuestTable(monster = {}, mapKind = "", floor = 1) {
    const spawn = monster?.spawn || {};
    const mapKinds = Array.isArray(spawn.mapKinds) ? spawn.mapKinds : [];
    const minFloor = Math.max(1, Number(spawn.minFloor || 1));
    const maxFloor = Math.max(minFloor, Number(spawn.maxFloor || minFloor));
    if (mapKinds.length && mapKind && !mapKinds.includes(mapKind)) return false;
    return floor >= minFloor && floor <= maxFloor;
  }

  function monsterBossLike(monster = {}) {
    const roles = Array.isArray(monster?.spawn?.roles) ? monster.spawn.roles : [];
    return Boolean(monster?.boss || roles.includes("boss"));
  }

  function questTargetMonsterCandidates(config = {}) {
    const mapKind = String(config.mapKind || "twisted_temple");
    const floor = Math.max(1, Number(config.floor || 1));
    const archetype = config.archetype === "subjugation" ? "subjugation" : "boss_hunt";
    const entries = Object.entries(monsters)
      .filter(([, monster]) => monster && typeof monster === "object")
      .filter(([, monster]) => monsterMatchesQuestTable(monster, mapKind, floor))
      .filter(([, monster]) => {
        const roles = Array.isArray(monster?.spawn?.roles) ? monster.spawn.roles : [];
        if (archetype === "boss_hunt") return monsterBossLike(monster);
        if (roles.includes("guard") || roles.includes("key") || roles.includes("start")) return true;
        return !monsterBossLike(monster);
      })
      .sort(([, left], [, right]) => {
        const bossDelta = Number(monsterBossLike(right)) - Number(monsterBossLike(left));
        if (bossDelta) return bossDelta;
        const xpDelta = Number(right?.xp || 0) - Number(left?.xp || 0);
        if (xpDelta) return xpDelta;
        return String(left?.name || "").localeCompare(String(right?.name || ""), "ko");
      })
      .map(([monsterId, monster]) => {
        const atkMin = monster.atkMin != null ? Number(monster.atkMin) : Number(monster.atk || 0);
        const atkMax = monster.atkMax != null ? Number(monster.atkMax) : Number(monster.atk || atkMin);
        const roles = Array.isArray(monster?.spawn?.roles) ? monster.spawn.roles : [];
        return {
          monsterId,
          monster,
          summary: `${monster.name || monsterId} · HP ${Number(monster.hp || 0)} · ATK ${atkMin}${atkMax !== atkMin ? `-${atkMax}` : ""} · XP ${Number(monster.xp || 0)} · ${roles.join("/") || "role 없음"}`,
        };
      });
    return entries;
  }

  function questGeneratorSelection(questDef = null) {
    const generator = questDef?.generator && typeof questDef.generator === "object" ? questDef.generator : {};
    const archetype = generator.archetype === "subjugation" ? "subjugation" : "boss_hunt";
    const mapKind = String(generator.mapKind || questDef?.mapKind || mapKindOptionEntries()[0]?.mapKind || "twisted_temple");
    const floor = Math.max(1, Number(generator.floor || questDef?.startFloor || 1));
    const candidates = questTargetMonsterCandidates({ mapKind, floor, archetype });
    const targetMonsterId = candidates.some((entry) => entry.monsterId === generator.targetMonsterId)
      ? generator.targetMonsterId
      : candidates[0]?.monsterId || "";
    return {
      archetype,
      mapKind,
      floor,
      targetMonsterId,
      candidates,
      mapOptions: mapKindOptionEntries(mapKind),
    };
  }

  function rewardItemsForGeneratedQuest(mapKind = "") {
    const preferredItemId = {
      twisted_temple: "bandage",
      coral_coast: "antivenom",
      ruins: "firebomb",
    }[mapKind];
    if (preferredItemId && items[preferredItemId]) return [{ itemId: preferredItemId, quantity: 1 }];
    if (items.bandage) return [{ itemId: "bandage", quantity: 1 }];
    return [];
  }

  function buildGeneratedQuestDefinition(config = {}) {
    const selection = questGeneratorSelection({
      mapKind: config.mapKind,
      startFloor: config.floor,
      generator: {
        archetype: config.archetype,
        mapKind: config.mapKind,
        floor: config.floor,
        targetMonsterId: config.targetMonsterId,
      },
    });
    const candidate = selection.candidates.find((entry) => entry.monsterId === selection.targetMonsterId) || selection.candidates[0];
    if (!candidate) return null;
    const { monsterId, monster } = candidate;
    const mapName = mapKindLabel(selection.mapKind);
    const archetypeLabel = selection.archetype === "subjugation" ? "정리" : "토벌";
    const questBaseId = config.questId
      ? String(config.questId)
      : uniqueQuestDefinitionId(`quest_${selection.mapKind}_${selection.floor}_${monsterId}`);
    const targetCount = 1;
    const rewardScale = selection.archetype === "boss_hunt" || monsterBossLike(monster) ? 1.4 : 1.1;
    const gold = Math.max(10, Math.round((Number(monster.xp || 8) * rewardScale) + (selection.floor * 6)));
    const xp = Math.max(5, Math.round((Number(monster.xp || 8) * 0.7) + (selection.floor * 3)));
    return {
      id: questBaseId,
      definition: {
        name: `${mapName} ${monster.name || monsterId} ${archetypeLabel}`,
        description: `${mapName} ${selection.floor}층에 출몰하는 ${monster.name || monsterId}을 정리하는 게시판 의뢰다. 수주 후 문으로 들어가 목표를 완료하고 귀환해 보상을 정산한다.`,
        mapKind: selection.mapKind,
        startFloor: selection.floor,
        conditions: {
          kind: "specific_monsters_defeated",
          targetMonsterIds: [monsterId],
          requiredCount: targetCount,
          summary: `${mapName} ${selection.floor}층에서 ${monster.name || monsterId}을 쓰러뜨린다.`,
        },
        rewards: {
          gold,
          xp,
          items: rewardItemsForGeneratedQuest(selection.mapKind),
          flag: `${questBaseId}_rewarded`,
          value: true,
        },
        return: { label: "귀환", rewardOnReturn: true },
        generator: {
          kind: "table_hunt",
          generated: true,
          archetype: selection.archetype,
          mapKind: selection.mapKind,
          floor: selection.floor,
          targetMonsterId: monsterId,
        },
      },
      selection,
    };
  }

  function activeMonsterDefinitionId() {
    const state = getState();
    return monsters[state.selectedMonsterDefinitionId] ? state.selectedMonsterDefinitionId : Object.keys(monsters)[0] || "";
  }

  function activeMonsterDefinition() {
    const monsterId = activeMonsterDefinitionId();
    return monsterId ? monsters[monsterId] || null : null;
  }

  function updateMonsterDefinition(monsterId, updater) {
    if (!monsterId || !monsters[monsterId]) return;
    updater(monsters[monsterId]);
  }

  function monsterDefinitionsJson() {
    return JSON.stringify(monsters, null, 2);
  }

  function uniqueMonsterDefinitionId(baseId = "monster_custom") {
    const slug = (baseId || "monster_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "monster_custom";
    const existing = new Set(Object.keys(monsters));
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createMonsterDefinitionTemplate() {
    return {
      name: "새 몬스터",
      hp: 10,
      atk: 4,
      atkMin: 4,
      atkMax: 8,
      def: 1,
      xp: 8,
      ai: "aggressive",
      spawn: { mapKinds: ["twisted_temple"], minFloor: 1, maxFloor: 3, roles: ["start", "guard"], weight: 5 },
      scaling: { hpPerFloor: 0.2, atkPerFloor: 0.15 },
    };
  }

  function activeSkillDefinitionId() {
    const state = getState();
    return skills[state.selectedSkillDefinitionId] ? state.selectedSkillDefinitionId : Object.keys(skills)[0] || "";
  }

  function activeSkillDefinition() {
    const skillId = activeSkillDefinitionId();
    return skillId ? skills[skillId] || null : null;
  }

  function updateSkillDefinition(skillId, updater) {
    if (!skillId || !skills[skillId]) return;
    updater(skills[skillId]);
  }

  function skillDefinitionsJson() {
    return JSON.stringify(skills, null, 2);
  }

  function uniqueSkillDefinitionId(baseId = "skill_custom") {
    const slug = (baseId || "skill_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "skill_custom";
    const existing = new Set(Object.keys(skills));
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createSkillDefinitionTemplate(kind = "attack") {
    const normalizedKind = String(kind || "attack");
    const base = {
      name: "새 기술",
      kind: normalizedKind,
      targetMode: normalizedKind === "heal" ? "ally" : (normalizedKind === "guard" || normalizedKind === "buff" ? "self" : "enemy"),
      effect: normalizedKind === "debuff" ? 1 : 2,
      cooldown: 2,
      buyPrice: 12,
      sellPrice: 6,
      catalogIds: ["merchant_basic", "trainer_skill_rotation"],
      formula: normalizedKind === "buff" || normalizedKind === "debuff" ? "die_as_effect" : "die_plus_effect",
      tags: [normalizedKind],
      description: "주사위 눈과 효과값으로 전투 효과를 만든다.",
    };
    if (normalizedKind === "debuff") {
      base.status = "weakened";
      base.duration = 2;
      base.description = "주사위 눈만큼 약화 수치를 주고 몇 턴 동안 공격을 낮춘다.";
    }
    if (normalizedKind === "buff") {
      base.status = "empowered";
      base.duration = 2;
      base.description = "주사위 눈만큼 강화 수치를 얻고 몇 턴 동안 공격을 높인다.";
    }
    if (normalizedKind === "lifesteal") {
      base.targetMode = "enemy";
      base.description = "피해를 주고 피해 일부를 HP로 흡수한다.";
    }
    if (normalizedKind === "summon") {
      base.targetMode = "self";
      base.deferred = true;
      base.description = "소환수 전투 엔티티 로직이 추가될 때 연결할 보류 스킬이다.";
    }
    return base;
  }

  function activeItemDefinitionId() {
    const state = getState();
    return items[state.selectedItemDefinitionId] ? state.selectedItemDefinitionId : Object.keys(items)[0] || "";
  }

  function activeItemDefinition() {
    const itemId = activeItemDefinitionId();
    return itemId ? items[itemId] || null : null;
  }

  function updateItemDefinition(itemId, updater) {
    if (!itemId || !items[itemId]) return;
    updater(items[itemId]);
  }

  function itemDefinitionsJson() {
    return JSON.stringify(items, null, 2);
  }

  function uniqueItemDefinitionId(baseId = "item_custom") {
    const slug = (baseId || "item_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "item_custom";
    const existing = new Set(Object.keys(items));
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createItemDefinitionTemplate(kind = "consumable") {
    if (kind === "key") return { name: "새 열쇠", kind: "key" };
    if (kind === "artifact") return { name: "새 유물", kind: "artifact", attack: 1, curse: 0 };
    if (kind === "quest") return { name: "새 퀘스트 아이템", kind: "quest" };
    if (kind === "equipment") return { name: "새 장비", kind: "equipment", slot: "weapon", attack: 1, defense: 0, curse: 0 };
    return { name: "새 소모품", kind: "consumable", heal: 0, cure: "", throwDamage: 0, targetMode: "enemy" };
  }

  function activeVendorDefinitionId() {
    const state = getState();
    return vendors[state.selectedVendorDefinitionId] ? state.selectedVendorDefinitionId : Object.keys(vendors)[0] || "";
  }

  function activeVendorDefinition() {
    const vendorId = activeVendorDefinitionId();
    return vendorId ? vendors[vendorId] || null : null;
  }

  function updateVendorDefinition(vendorId, updater) {
    if (!vendorId || !vendors[vendorId]) return;
    updater(vendors[vendorId]);
  }

  function vendorsJson() {
    return JSON.stringify(vendors, null, 2);
  }

  function uniqueVendorDefinitionId(baseId = "vendor_custom") {
    const slug = (baseId || "vendor_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "vendor_custom";
    const existing = new Set(Object.keys(vendors));
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createVendorDefinitionTemplate(serviceType = "sell_bundle") {
    if (serviceType === "heal_party") return { serviceType, cost: { gold: 5 }, summary: "금화로 파티를 회복한다." };
    if (serviceType === "train_party") return { serviceType, summary: "XP를 써서 파티를 훈련시킨다.", rotation: [] };
    if (serviceType === "buff_frontline") return { serviceType, cost: { gold: 10 }, rewards: { frontlineAtkGain: 1 }, summary: "전열을 강화한다.", rotation: [] };
    return { serviceType: "sell_bundle", cost: { gold: 8 }, inventory: [Object.keys(items)[0] || ""], summary: "아이템 묶음을 판매한다.", rotation: [] };
  }

  function createVendorRotationTemplate() {
    return {
      when: { minFloor: 1 },
      cost: { gold: 0 },
      inventory: [],
      summary: "새 vendor rotation",
    };
  }

  function activeVendorRotationDefinition(vendor) {
    const state = getState();
    const rotations = Array.isArray(vendor?.rotation) ? vendor.rotation : [];
    if (!rotations.length) return { rotation: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedVendorRotationIndex || 0)), rotations.length - 1);
    return { rotation: rotations[index], index };
  }

  function vendorInventorySummary(list = []) {
    return (list || []).map((entry) => vendorInventoryEntryLabel(entry)).filter(Boolean).join(", ");
  }

  function lootTableDefinitionIds() {
    return Object.keys(lootTables).filter((id) => id !== "combatRewardProfiles");
  }

  function activeLootTableId() {
    const state = getState();
    const ids = lootTableDefinitionIds();
    return ids.includes(state.selectedLootTableId) ? state.selectedLootTableId : ids[0] || "";
  }

  function activeLootTableDefinition() {
    const tableId = activeLootTableId();
    return tableId ? lootTables[tableId] || null : null;
  }

  function updateLootTableDefinition(tableId, updater) {
    if (!tableId || !lootTables[tableId]) return;
    updater(lootTables[tableId]);
  }

  function lootTablesJson() {
    return JSON.stringify(lootTables, null, 2);
  }

  function uniqueLootTableId(baseId = "loot_custom") {
    const slug = (baseId || "loot_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "loot_custom";
    const existing = new Set(lootTableDefinitionIds());
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createLootEntryTemplate() {
    return { itemId: Object.keys(items)[0] || "", quantity: 1, weight: 1 };
  }

  function ensureVendorInventoryEntryObject(list, index) {
    if (!Array.isArray(list) || index < 0 || index >= list.length) return null;
    const current = list[index];
    if (typeof current === "string") {
      list[index] = { itemId: current };
    } else if (!current || typeof current !== "object" || Array.isArray(current)) {
      list[index] = { itemId: Object.keys(items)[0] || "" };
    } else if (!current.itemId) {
      current.itemId = Object.keys(items)[0] || "";
    }
    return list[index];
  }

  function ensureLootEntryObject(list, index) {
    if (!Array.isArray(list) || index < 0 || index >= list.length) return null;
    const current = list[index];
    if (!current || typeof current !== "object" || Array.isArray(current)) {
      list[index] = createLootEntryTemplate();
    } else if (!current.itemId) {
      current.itemId = Object.keys(items)[0] || "";
    }
    return list[index];
  }

  function activeRarityDefinitionId() {
    const state = getState();
    return rarityDefinitions[state.selectedRarityDefinitionId] ? state.selectedRarityDefinitionId : Object.keys(rarityDefinitions)[0] || "";
  }

  function activeAffixPoolId() {
    const state = getState();
    return affixPoolDefinitions[state.selectedAffixPoolId] ? state.selectedAffixPoolId : Object.keys(affixPoolDefinitions)[0] || "";
  }

  function setGeneratedEntryFields(entry, enabled) {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return;
    if (enabled) {
      entry.generated = true;
      if (!entry.rarityId || !rarityDefinitions[entry.rarityId]) entry.rarityId = activeRarityDefinitionId();
      if (!entry.affixPoolId || !affixPoolDefinitions[entry.affixPoolId]) entry.affixPoolId = activeAffixPoolId();
    } else {
      delete entry.generated;
      delete entry.rarityId;
      delete entry.affixPoolId;
    }
  }

  function itemDefinitionOptionListHtml(selectedId) {
    return Object.entries(items).map(([id, item]) => `<option value="${id}" ${id === selectedId ? "selected" : ""}>${id} · ${item.name}</option>`).join("");
  }

  function rarityDefinitionOptionListHtml(selectedId) {
    return Object.entries(rarityDefinitions).map(([id, entry]) => `<option value="${id}" ${id === selectedId ? "selected" : ""}>${id} · ${escapeHtml(entry.label || id)}</option>`).join("");
  }

  function affixPoolOptionListHtml(selectedId) {
    return Object.entries(affixPoolDefinitions).map(([id, entry]) => `<option value="${id}" ${id === selectedId ? "selected" : ""}>${id} · ${escapeHtml(entry.label || id)}</option>`).join("");
  }

  function createLootTierTemplate() {
    return { weight: 1, entries: [createLootEntryTemplate()] };
  }

  function createLootBonusTemplate() {
    return { chance: 0.25, entries: [createLootEntryTemplate()] };
  }

  function createLootTableDefinitionTemplate() {
    return { rolls: 1, guaranteed: [], tierEntries: [createLootTierTemplate()], bonusRolls: [] };
  }

  function createCombatRewardProfileTemplate() {
    return { tableId: activeLootTableId() || "loot_combat_bandage", when: { minXp: 0 } };
  }

  function activeLootTierDefinition(table) {
    const state = getState();
    const tiers = Array.isArray(table?.tierEntries) ? table.tierEntries : [];
    if (!tiers.length) return { tier: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedLootTierIndex || 0)), tiers.length - 1);
    return { tier: tiers[index], index };
  }

  function activeLootBonusDefinition(table) {
    const state = getState();
    const bonuses = Array.isArray(table?.bonusRolls) ? table.bonusRolls : [];
    if (!bonuses.length) return { bonus: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedLootBonusIndex || 0)), bonuses.length - 1);
    return { bonus: bonuses[index], index };
  }

  function activeCombatRewardProfile() {
    const state = getState();
    const profiles = lootTables.combatRewardProfiles?.default || [];
    if (!profiles.length) return { profile: null, index: 0 };
    const index = Math.min(Math.max(0, Number(state.selectedCombatRewardProfileIndex || 0)), profiles.length - 1);
    return { profile: profiles[index], index };
  }

  function rarityDefinitionsJson() {
    return JSON.stringify(rarityDefinitions, null, 2);
  }

  function affixDefinitionsJson() {
    return JSON.stringify(affixDefinitions, null, 2);
  }

  function affixPoolDefinitionsJson() {
    return JSON.stringify(affixPoolDefinitions, null, 2);
  }

  function activeRarityDefinition() {
    const rarityId = activeRarityDefinitionId();
    return rarityId ? rarityDefinitions[rarityId] || null : null;
  }

  function activeAffixDefinitionId() {
    const state = getState();
    return affixDefinitions[state.selectedAffixDefinitionId] ? state.selectedAffixDefinitionId : Object.keys(affixDefinitions)[0] || "";
  }

  function activeAffixDefinition() {
    const affixId = activeAffixDefinitionId();
    return affixId ? affixDefinitions[affixId] || null : null;
  }

  function activeAffixPoolDefinition() {
    const poolId = activeAffixPoolId();
    return poolId ? affixPoolDefinitions[poolId] || null : null;
  }

  function updateRarityDefinition(rarityId, updater) {
    if (!rarityId || !rarityDefinitions[rarityId]) return;
    updater(rarityDefinitions[rarityId]);
  }

  function updateAffixDefinition(affixId, updater) {
    if (!affixId || !affixDefinitions[affixId]) return;
    updater(affixDefinitions[affixId]);
  }

  function updateAffixPoolDefinition(poolId, updater) {
    if (!poolId || !affixPoolDefinitions[poolId]) return;
    updater(affixPoolDefinitions[poolId]);
  }

  function uniqueSchemaId(existingIds, baseId) {
    const slug = (baseId || "schema_custom").replace(/[^a-zA-Z0-9_]+/g, "_").replace(/^_+|_+$/g, "").toLowerCase() || "schema_custom";
    const existing = new Set(existingIds);
    let candidate = slug;
    let index = 1;
    while (existing.has(candidate)) {
      candidate = `${slug}_${index}`;
      index += 1;
    }
    return candidate;
  }

  function createRarityDefinitionTemplate() {
    return { label: "새 희귀도", weight: 1, valueMultiplier: 1, affixCount: 1 };
  }

  function createAffixDefinitionTemplate(slot = "prefix") {
    return { label: slot === "suffix" ? "새 suffix" : "새 prefix", slot, stat: "attack", amount: 1, rarity: activeRarityDefinitionId() || "common" };
  }

  function createAffixPoolDefinitionTemplate() {
    return { label: "새 affix pool", itemKinds: ["equipment"], affixIds: [] };
  }

  return {
    activeClassDefinitionIndex,
    activeClassDefinition,
    updateClassDefinition,
    activeQuestDefinitionId,
    activeQuestDefinition,
    updateQuestDefinition,
    questDefinitionsJson,
    uniqueQuestDefinitionId,
    createQuestDefinitionTemplate,
    questGeneratorSelection,
    buildGeneratedQuestDefinition,
    mapKindOptionEntries,
    mapKindLabel,
    activeMonsterDefinitionId,
    activeMonsterDefinition,
    updateMonsterDefinition,
    monsterDefinitionsJson,
    uniqueMonsterDefinitionId,
    createMonsterDefinitionTemplate,
    activeSkillDefinitionId,
    activeSkillDefinition,
    updateSkillDefinition,
    skillDefinitionsJson,
    uniqueSkillDefinitionId,
    createSkillDefinitionTemplate,
    activeItemDefinitionId,
    activeItemDefinition,
    updateItemDefinition,
    itemDefinitionsJson,
    uniqueItemDefinitionId,
    createItemDefinitionTemplate,
    activeVendorDefinitionId,
    activeVendorDefinition,
    updateVendorDefinition,
    vendorsJson,
    uniqueVendorDefinitionId,
    createVendorDefinitionTemplate,
    createVendorRotationTemplate,
    activeVendorRotationDefinition,
    vendorInventorySummary,
    lootTableDefinitionIds,
    activeLootTableId,
    activeLootTableDefinition,
    updateLootTableDefinition,
    lootTablesJson,
    uniqueLootTableId,
    createLootEntryTemplate,
    ensureVendorInventoryEntryObject,
    ensureLootEntryObject,
    setGeneratedEntryFields,
    itemDefinitionOptionListHtml,
    rarityDefinitionOptionListHtml,
    affixPoolOptionListHtml,
    createLootTierTemplate,
    createLootBonusTemplate,
    createLootTableDefinitionTemplate,
    createCombatRewardProfileTemplate,
    activeLootTierDefinition,
    activeLootBonusDefinition,
    activeCombatRewardProfile,
    rarityDefinitionsJson,
    affixDefinitionsJson,
    affixPoolDefinitionsJson,
    activeRarityDefinitionId,
    activeRarityDefinition,
    activeAffixDefinitionId,
    activeAffixDefinition,
    activeAffixPoolId,
    activeAffixPoolDefinition,
    updateRarityDefinition,
    updateAffixDefinition,
    updateAffixPoolDefinition,
    uniqueSchemaId,
    createRarityDefinitionTemplate,
    createAffixDefinitionTemplate,
    createAffixPoolDefinitionTemplate,
  };
}

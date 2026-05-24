const DEFAULT_DICE_COUNT = 5;
const DEFAULT_SELECT_LIMIT = 3;
const DEFAULT_FACE_VALUES = Object.freeze([1, 2, 3, 4, 5, 6]);
export const EMPTY_FACE_FALLBACK_SKILL_ID = "fallback_basic_attack";
const DEFAULT_SKILL_ROTATION = Object.freeze([
  "skill_berserk",
  "skill_guard_stance",
  "skill_vital_stab",
  "skill_piercing_shot",
  "skill_serpent_phantasm",
  "skill_purify",
  "skill_weakness_read",
]);

function uniqueIds(values = []) {
  return [...new Set(values.filter((value) => typeof value === "string" && value.trim()))];
}

function normalizeSkillCount(value) {
  const count = Math.floor(Number(value));
  return Number.isFinite(count) ? Math.max(0, count) : 0;
}

function isSkillInventoryTable(value) {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function fallbackSkillIds(hero = {}) {
  return uniqueIds([hero.skillId, ...DEFAULT_SKILL_ROTATION]);
}

function fallbackSkillIdsForHero(hero = {}) {
  return uniqueIds([hero.skillId, ...DEFAULT_SKILL_ROTATION]);
}

function buildSkillInventoryTable(skillIds = [], count = 1) {
  return uniqueIds(skillIds).reduce((table, skillId) => {
    table[skillId] = normalizeSkillCount(count);
    return table;
  }, {});
}

function equippedSkillCountsFromDiceLoadout(loadout = {}) {
  return (Array.isArray(loadout.dice) ? loadout.dice : []).reduce((counts, die) => {
    (Array.isArray(die?.faces) ? die.faces : []).forEach((face) => {
      if (typeof face?.skillId !== "string" || !face.skillId.trim()) return;
      counts[face.skillId] = (counts[face.skillId] || 0) + 1;
    });
    return counts;
  }, {});
}

function buildHeroKnownSkillIds(hero = {}, loadout = hero.diceLoadout, extraSkillIds = []) {
  const skillInventory = hero.skillInventoryTable || hero.skillInventory;
  return uniqueIds([
    ...(Array.isArray(hero.knownSkillIds) ? hero.knownSkillIds : []),
    ...(Array.isArray(hero.defaultKnownSkillIds) ? hero.defaultKnownSkillIds : []),
    ...Object.keys(isSkillInventoryTable(skillInventory) ? skillInventory : {}),
    ...Object.keys(equippedSkillCountsFromDiceLoadout(loadout)),
    ...extraSkillIds,
    ...fallbackSkillIdsForHero(hero),
  ]);
}

export function createDefaultKnownSkillIds(hero = {}) {
  return fallbackSkillIds(hero);
}

export function buildDefaultKnownSkillIds(hero = {}) {
  return createDefaultKnownSkillIds(hero);
}

export function createDefaultSkillInventory(hero = {}) {
  return buildSkillInventoryTable(createDefaultKnownSkillIds(hero), 0);
}

export function createDefaultDiceLoadout(hero = {}) {
  const heroId = hero.heroId || hero.id || "hero";
  const skillIds = uniqueIds([
    ...(Array.isArray(hero.defaultKnownSkillIds) ? hero.defaultKnownSkillIds : []),
    ...(Array.isArray(hero.knownSkillIds) ? hero.knownSkillIds : []),
    ...Object.keys(isSkillInventoryTable(hero.skillInventoryTable) ? hero.skillInventoryTable : {}),
    ...Object.keys(isSkillInventoryTable(hero.skillInventory) ? hero.skillInventory : {}),
    hero.defaultSkillId,
    ...createDefaultKnownSkillIds(hero),
  ]);
  const baseOffset = Math.abs(String(heroId || hero.name || "hero").split("").reduce((sum, char) => sum + char.charCodeAt(0), 0)) % skillIds.length;
  return {
    diceCount: DEFAULT_DICE_COUNT,
    selectLimit: DEFAULT_SELECT_LIMIT,
    dice: Array.from({ length: DEFAULT_DICE_COUNT }, (_, dieIndex) => ({
      id: `${heroId}_die_${dieIndex + 1}`,
      faces: DEFAULT_FACE_VALUES.map((value, faceIndex) => ({
        value,
        skillId: skillIds[(baseOffset + dieIndex + faceIndex) % skillIds.length],
      })),
    })),
  };
}

export function normalizeDiceFace(face = {}, fallbackValue = 1, fallbackSkillIds = DEFAULT_SKILL_ROTATION) {
  const skillIds = uniqueIds(fallbackSkillIds);
  const normalizedSkillId = typeof face.skillId === "string" ? face.skillId.trim() : "";
  return {
    value: Number.isFinite(Number(face.value)) ? Number(face.value) : fallbackValue,
    skillId: !normalizedSkillId
      ? ""
      : (skillIds.includes(normalizedSkillId) ? normalizedSkillId : (skillIds[0] || "")),
  };
}

export function normalizeSkillInventoryTable(skillInventory = {}, fallbackSkillIds = []) {
  const sourceTable = isSkillInventoryTable(skillInventory)
    ? skillInventory
    : buildSkillInventoryTable(fallbackSkillIds, 0);
  const normalizedEntries = Object.entries(sourceTable)
    .map(([skillId, count]) => [typeof skillId === "string" ? skillId.trim() : "", normalizeSkillCount(count)])
    .filter(([skillId]) => Boolean(skillId));
  fallbackSkillIds.forEach((skillId) => {
    if (!skillId || normalizedEntries.some(([entrySkillId]) => entrySkillId === skillId)) return;
    normalizedEntries.push([skillId, 0]);
  });
  return Object.fromEntries(normalizedEntries);
}

export function normalizeDiceLoadout(loadout = {}, hero = {}) {
  const fallbackLoadout = createDefaultDiceLoadout(hero);
  const fallbackSkillIds = buildHeroKnownSkillIds(hero, loadout);
  const dice = Array.isArray(loadout.dice) ? loadout.dice : [];
  return {
    diceCount: Number.isInteger(Number(loadout.diceCount)) ? Math.max(1, Number(loadout.diceCount)) : fallbackLoadout.diceCount,
    selectLimit: Number.isInteger(Number(loadout.selectLimit)) ? Math.max(1, Number(loadout.selectLimit)) : fallbackLoadout.selectLimit,
    dice: fallbackLoadout.dice.map((fallbackDie, dieIndex) => {
      const sourceDie = dice[dieIndex] || {};
      const faces = Array.isArray(sourceDie.faces) ? sourceDie.faces : [];
      return {
        id: sourceDie.id || fallbackDie.id,
        faces: fallbackDie.faces.map((fallbackFace, faceIndex) => normalizeDiceFace(
          faces[faceIndex],
          fallbackFace.value,
          fallbackSkillIds
        )),
      };
    }),
  };
}

export function normalizeHeroCombatLoadout(hero = {}) {
  hero.diceLoadout = normalizeDiceLoadout(hero.diceLoadout, hero);
  const knownSkillIds = buildHeroKnownSkillIds(hero, hero.diceLoadout);
  const inventorySource = hero.skillInventoryTable || hero.skillInventory;
  const skillInventory = normalizeSkillInventoryTable(inventorySource, knownSkillIds);
  hero.knownSkillIds = knownSkillIds;
  hero.skillInventory = skillInventory;
  hero.skillInventoryTable = hero.skillInventory;
  return hero;
}

export function normalizeHeroDiceProfile(hero = {}, options = {}) {
  const heroId = options.heroId || hero.id || "hero";
  hero.id = heroId;
  hero.diceLoadout = normalizeDiceLoadout(hero.diceLoadout, hero);
  const knownSkillIds = buildHeroKnownSkillIds(hero, hero.diceLoadout, options.defaultKnownSkillIds || []);
  const inventorySource = hero.skillInventoryTable || hero.skillInventory;
  const skillInventory = normalizeSkillInventoryTable(inventorySource, knownSkillIds);
  hero.knownSkillIds = knownSkillIds;
  hero.skillInventory = skillInventory;
  hero.skillInventoryTable = hero.skillInventory;
  hero.diceLoadout = normalizeDiceLoadout(hero.diceLoadout, hero);
  return hero;
}

export function normalizeCompanionDiceProfile(companion = null) {
  if (!companion || typeof companion !== "object") return null;
  const nextCompanion = JSON.parse(JSON.stringify(companion));
  if (nextCompanion.hero && typeof nextCompanion.hero === "object") {
    normalizeHeroDiceProfile(nextCompanion.hero, {
      heroId: nextCompanion.hero.id || "companion_hero",
    });
  }
  return nextCompanion;
}

export function assignSkillToDieFace(hero, dieId = "", faceIndex = -1, skillId = "") {
  if (!hero?.diceLoadout?.dice) return false;
  normalizeHeroCombatLoadout(hero);
  const die = hero.diceLoadout.dice.find((entry) => entry.id === dieId);
  const face = die?.faces?.[faceIndex];
  if (!face) return false;

  const nextSkillId = typeof skillId === "string" ? skillId.trim() : "";
  const previousSkillId = typeof face.skillId === "string" ? face.skillId.trim() : "";
  if (nextSkillId === previousSkillId) return false;

  if (previousSkillId) {
    hero.skillInventory[previousSkillId] = normalizeSkillCount(hero.skillInventory[previousSkillId]) + 1;
  }

  if (nextSkillId) {
    if (!hero.knownSkillIds?.includes(nextSkillId)) {
      if (previousSkillId) hero.skillInventory[previousSkillId] = Math.max(0, hero.skillInventory[previousSkillId] - 1);
      return false;
    }
    const availableCount = normalizeSkillCount(hero.skillInventory[nextSkillId]);
    if (availableCount <= 0) {
      if (previousSkillId) hero.skillInventory[previousSkillId] = Math.max(0, hero.skillInventory[previousSkillId] - 1);
      return false;
    }
    hero.skillInventory[nextSkillId] = availableCount - 1;
  }

  face.skillId = nextSkillId;
  hero.skillInventoryTable = hero.skillInventory;
  hero.knownSkillIds = buildHeroKnownSkillIds(hero, hero.diceLoadout);
  return true;
}

export function skillInventoryCount(hero = {}, skillId = "") {
  if (!skillId) return 0;
  normalizeHeroCombatLoadout(hero);
  return normalizeSkillCount(hero.skillInventory?.[skillId]);
}

export function equippedSkillCount(hero = {}, skillId = "") {
  if (!skillId) return 0;
  normalizeHeroCombatLoadout(hero);
  return normalizeSkillCount(equippedSkillCountsFromDiceLoadout(hero.diceLoadout)?.[skillId]);
}

export function heroSkillLibraryIds(hero = {}) {
  normalizeHeroCombatLoadout(hero);
  return buildHeroKnownSkillIds(hero, hero.diceLoadout);
}

export function grantSkillCard(hero = {}, skillId = "", amount = 1) {
  const normalizedSkillId = String(skillId || "").trim();
  if (!normalizedSkillId) return 0;
  normalizeHeroCombatLoadout(hero);
  hero.skillInventory[normalizedSkillId] = normalizeSkillCount(hero.skillInventory[normalizedSkillId]) + normalizeSkillCount(amount);
  hero.skillInventoryTable = hero.skillInventory;
  hero.knownSkillIds = buildHeroKnownSkillIds(hero, hero.diceLoadout, [normalizedSkillId]);
  return hero.skillInventory[normalizedSkillId];
}

export function consumeSkillCard(hero = {}, skillId = "", amount = 1) {
  const normalizedSkillId = String(skillId || "").trim();
  if (!normalizedSkillId) return 0;
  normalizeHeroCombatLoadout(hero);
  const current = normalizeSkillCount(hero.skillInventory[normalizedSkillId]);
  const next = Math.max(0, current - normalizeSkillCount(amount));
  hero.skillInventory[normalizedSkillId] = next;
  hero.skillInventoryTable = hero.skillInventory;
  hero.knownSkillIds = buildHeroKnownSkillIds(hero, hero.diceLoadout, [normalizedSkillId]);
  return next;
}

export function combatFormulaLabel(formula = "") {
  if (formula === "die_plus_effect") return "눈 + 효과";
  if (formula === "die_minus_effect") return "눈 - 효과";
  if (formula === "die_equals_effect") return "고정 효과";
  if (formula === "die_as_effect") return "눈 = 효과";
  if (formula === "die_divide_effect") return "눈 / 효과";
  return "눈 x 효과";
}

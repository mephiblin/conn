export async function loadJsonAsset(path) {
  const url = new URL(path, import.meta.url);
  try {
    const response = await fetch(url);
    if (!response.ok) throw new Error(`${path} 로드 실패: ${response.status}`);
    return response.json();
  } catch (error) {
    if (url.protocol !== "file:") throw error;
    const { readFile } = await import("node:fs/promises");
    return JSON.parse(await readFile(url, "utf8"));
  }
}

export const CONTENT_BUILD_DATA_FILES = [
  { id: "classes", path: "src/data/classes.json", source: "file" },
  { id: "monsters", path: "src/data/monsters.json", source: "file" },
  { id: "items", path: "src/data/items.json", source: "file" },
  { id: "encounters", path: "src/data/encounters.json", source: "file" },
  { id: "events", path: "src/data/events.json", source: "file" },
  { id: "skills", path: "src/data/skills.json", source: "file" },
  { id: "quests", path: "src/data/quests.json", source: "file" },
  { id: "npcs", path: "src/data/npcs.json", source: "file" },
  { id: "vendors", path: "src/data/vendors.json", source: "file" },
  { id: "lootTables", path: "src/data/loot_tables.json", source: "file" },
  { id: "materials", path: "src/data/materials.json", source: "file" },
  { id: "mapProfiles", path: "src/data/map_profiles.json", source: "file" },
  { id: "mapChunks", path: "src/data/map_chunks.json", source: "file" },
  { id: "tileSubstitutions", path: "src/data/tile_substitutions.json", source: "file" },
  { id: "objectThemes", path: "src/data/object_themes.json", source: "file" },
  { id: "monsterDefinitions", path: "editorProject.contentDefinitions.monsterDefinitions", source: "editor_project" },
  { id: "skillDefinitions", path: "editorProject.contentDefinitions.skillDefinitions", source: "editor_project" },
  { id: "questDefinitions", path: "editorProject.contentDefinitions.questDefinitions", source: "editor_project" },
  { id: "rarityDefinitions", path: "editorProject.contentDefinitions.rarityDefinitions", source: "editor_project" },
  { id: "affixDefinitions", path: "editorProject.contentDefinitions.affixDefinitions", source: "editor_project" },
  { id: "affixPoolDefinitions", path: "editorProject.contentDefinitions.affixPoolDefinitions", source: "editor_project" },
];

export const FLOOR_TEXTURE_IDS = [
  "floor_sandstone_01",
  "floor_obsidian_01",
  "floor_moss_01",
  "floor_bloodstone_01",
];
export const CEILING_TEXTURE_IDS = [
  "ceiling_stone_01",
  "ceiling_vault_01",
  "ceiling_soot_01",
  "ceiling_gold_01",
];
export const WALL_TEXTURE_IDS = [
  "wall_buried_temple_01",
  "wall_black_brick_01",
  "wall_mossy_01",
  "wall_sacred_relief_01",
];
export const DEFAULT_FLOOR_TEXTURE_ID = FLOOR_TEXTURE_IDS[0];
export const DEFAULT_CEILING_TEXTURE_ID = CEILING_TEXTURE_IDS[0];
export const DEFAULT_WALL_TEXTURE_ID = WALL_TEXTURE_IDS[0];
export const THEME_BATTLE_BACKGROUNDS = {
  buried_temple: "battle_bg_buried_temple_corridor",
};
export const GENERATED_NORMAL_MAP_KEYS = new Set([
  "obsidian_tiles",
  "hammered_gold",
  "sacred_relief",
  "bronze_door",
]);
export const VALID_MATERIAL_LIGHTING_HINTS = new Set(["ambient", "interactive", "guiding"]);
export const VALID_MATERIAL_LODS = new Set(["default", "hero"]);

export async function loadContentData() {
  const [classes, monsters, items, encounters, eventDefinitions, skills, questDefinitions, npcs, vendors, lootTables, materialManifest, mapProfiles] = await Promise.all([
    loadJsonAsset("./data/classes.json"),
    loadJsonAsset("./data/monsters.json"),
    loadJsonAsset("./data/items.json"),
    loadJsonAsset("./data/encounters.json"),
    loadJsonAsset("./data/events.json"),
    loadJsonAsset("./data/skills.json"),
    loadJsonAsset("./data/quests.json"),
    loadJsonAsset("./data/npcs.json"),
    loadJsonAsset("./data/vendors.json"),
    loadJsonAsset("./data/loot_tables.json"),
    loadJsonAsset("./data/materials.json"),
    loadJsonAsset("./data/map_profiles.json"),
  ]);
  return {
    classes,
    monsters,
    items,
    encounters,
    eventDefinitions,
    skills,
    questDefinitions,
    npcs,
    vendors,
    lootTables,
    materialManifest,
    mapProfiles,
  };
}

export function resolveRendererLodProfile(search = globalThis?.location?.search || "") {
  const params = new URLSearchParams(search);
  const requested = (params.get("lod") || "").trim().toLowerCase();
  if (requested === "hero") return "hero";
  return "default";
}

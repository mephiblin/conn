import { loadJsonAsset } from "./contentRegistry.js";

const DEFAULT_TILE_SUBSTITUTIONS = [
  {
    id: "buried_temple_floor_variation",
    theme: "buried_temple",
    target: "floor",
    whenTileRoles: ["room", "junction", "intersection", "corner"],
    variants: [
      { materialId: "floor_sandstone_01", weight: 6, tag: "tile_base" },
      { materialId: "floor_moss_01", weight: 2, tag: "tile_moss" },
      { materialId: "floor_obsidian_01", weight: 1, tag: "tile_obsidian" },
    ],
  },
  {
    id: "buried_temple_corridor_variation",
    theme: "buried_temple",
    target: "floor",
    whenTileRoles: ["corridor", "end_cap"],
    variants: [
      { materialId: "floor_sandstone_01", weight: 5, tag: "tile_corridor" },
      { materialId: "floor_bloodstone_01", weight: 1, tag: "tile_blood_trace" },
    ],
  },
];

const DEFAULT_OBJECT_THEMES = [
  {
    id: "buried_temple_props",
    theme: "buried_temple",
    decor: [
      { kind: "torch", tileRoles: ["corridor", "end_cap"], weight: 3, maxPerMap: 8, color: "#d69a52" },
      { kind: "barrel", tileRoles: ["room", "corner"], weight: 2, maxPerMap: 6, color: "#8a6747" },
      { kind: "crate", tileRoles: ["room", "junction"], weight: 2, maxPerMap: 6, color: "#7a5b3d" },
      { kind: "bones", tileRoles: ["end_cap", "corner"], weight: 2, maxPerMap: 5, color: "#d8d0c2" },
      { kind: "altar", tileRoles: ["intersection", "room"], weight: 1, maxPerMap: 3, color: "#b79a61" },
    ],
  },
];

async function loadVisualJson(path, fallback) {
  try {
    const loaded = await loadJsonAsset(path);
    return Array.isArray(loaded) ? loaded : fallback;
  } catch {
    return fallback;
  }
}

export const TILE_SUBSTITUTION_DEFINITIONS = await loadVisualJson("./data/tile_substitutions.json", DEFAULT_TILE_SUBSTITUTIONS);
export const OBJECT_THEME_DEFINITIONS = await loadVisualJson("./data/object_themes.json", DEFAULT_OBJECT_THEMES);

export function tileSubstitutionsForTheme(theme = "buried_temple") {
  return TILE_SUBSTITUTION_DEFINITIONS.filter((definition) => definition.theme === theme);
}

export function objectThemeForTheme(theme = "buried_temple") {
  return OBJECT_THEME_DEFINITIONS.find((definition) => definition.theme === theme) || OBJECT_THEME_DEFINITIONS[0] || null;
}

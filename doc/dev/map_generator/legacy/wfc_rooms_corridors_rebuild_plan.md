# WFC Rooms/Corridors Map Generator Rebuild Plan

Date: 2026-05-30
Status: rebuild direction for replacing the legacy graph-first generator with a
profile-and-room-pool driven rooms/corridors generator.

## Reference Target

The target reference is MoraMapGen: a Unity editor/runtime dungeon generator
marketed as a rooms-and-corridors procedural level generator using a custom WFC
approach for grid-based room/corridor layouts. The Asset Store listing classifies
it as a level-design editor tool, room/corridor dungeon generator, grid-based,
runtime-capable, and WFC/procedural generation focused. The listing also shows
URP compatibility for Unity 2022.3.7, version 1.0, and an extension-asset
workflow.

Sources:

- Unity Asset Store: `Procedural Rooms & Corridors Dungeon Generator (WFC) |
  MoraMapGen`
- YouTube demo: `https://www.youtube.com/watch?v=5qo8odfwXk4&t=2s`

## Replacement Principle

The old direction was graph-first:

```text
seed -> hardcoded/sample graph -> preview -> optional draft
```

The replacement direction is asset-pool-first:

```text
MapProfileAsset
  + generation rules
  + room/chunk asset pools
  + placement rules
  -> visual cell-map preview
  -> accepted EditableMapDraftAsset
  -> validation
  -> baked CompiledMapAsset
```

## Production Pipeline

1. Create or select a `MapProfileAsset`.
2. Configure generation rules:
   room count min/max, critical path min/max, side branches, loops, map size,
   room size, floor/difficulty.
3. Assign room asset pools:
   start, main room, corridor, hub, side branch, dead end, quest, boss, exit,
   height transition.
4. Validate that every required role/socket/layout has at least one compatible
   room asset.
5. Generate a visual cell-map preview from the selected profile and seed.
6. Randomize seed until the generated cell-map shape is acceptable.
7. Accept the preview as an `EditableMapDraftAsset`.
8. Paint or correct cells, height levels, materials, objects, sockets, and rooms.
9. Validate routes, sockets, objects, and required anchors.
10. Bake a runtime-safe `CompiledMapAsset`.

## Current First Slice

- `MapProfileAsset` now exposes explicit `Room Count Min/Max`.
- `MapGenerationService` clamps critical path and side branches to stay within
  that room count range.
- `MapGeneratorWorkspace` shows selected profile rule and room-pool summaries.
- `MapProfileAuthoringSampleBuilder` creates explicit Chapter 2 sample authoring
  assets instead of using a hidden fallback profile.

## Required Next Work

- Replace role-tag arrays with explicit typed room pools per role/layout.
- Add WFC-style socket compatibility constraints between room exits.
- Add corridor-specific tiles/chunks instead of treating corridors as ordinary
  main-path rooms.
- Add overlap/footprint placement search for variable-size room assets.
- Add failure reports explaining which pool/constraint caused generation to fail.
- Add a visual profile editor so designers can manage pools without inspecting
  raw arrays.

# Map Generator and Monster Editor Cooperation Plan

Date: 2026-05-25
Status: first-pass authoring, validation, workbench, and runtime bundle bridge implemented; manual Game view verification remains.

## Current Planning Update

The current generator has moved past pure graph preview foundations, but the
next target is a new editable cell-map workflow rather than further expansion
of temporary preview objects. See `editable_cell_map_editor_plan.md` for the
plan to introduce an `EditableMapDraftAsset`, tileable cell layers, object
palettes, paint/stamp tools, mesh preview, validation, and runtime bake.
`MapGeneratorWorkspace` should remain legacy debug/reference tooling until the
new draft pipeline can fully replace it.

As of 2026-05-30, the first draft-backed path is now in the project:

- `EditableMapDraftAsset` stores the editable cell grid, object placements,
  rooms, zones, and sockets as serialized asset data.
- `EditableMapDraftBuilder` can create a blank draft asset or adapt the current
  `GeneratedMapDraft` graph into a saved editable draft under
  `Assets/Conn/Authoring/Maps/Drafts`.
- `EditableMapPreviewMeshBuilder` rebuilds a disposable scene preview directly
  from the draft asset instead of relying on `MapGeneratorWorkspace` room cubes.
- `EditableMapDraftEditor` adds first-pass draft actions for blank grid reset,
  direct preview drawing, scene map build, preview clear, coordinate brush
  edits, fill/clear, and draft validation.
- `EditableMapDraftEditor` now lets designers paint directly on the inspector
  `Map Preview` grid. It also keeps Scene View painting available for the scene
  preview mesh: hovered cell picking, left-click paint, drag paint across cells,
  and validation overlays for failed cells/objects/sockets.
- `EditableMapDraftMetadataBuilder` can rebuild minimal playable room, zone,
  socket, and required-route metadata from a drawn walkable cell map so a
  painted draft can validate and bake instead of remaining only a picture.
- `EditableMapValidationService` now validates walkability, room cell ownership
  bounds, object footprint room ownership, blocking object footprints, socket
  legality, required room-to-room routes, and slope/stair height transitions for
  editable drafts.
- `MapTilePaletteAsset` and `MapObjectPaletteAsset` now provide registered tile
  and object ids for draft authoring, validation, and preview lookup. Drafts
  can reference palette ids instead of raw Unity object references, while the
  editor still resolves preview materials and prefabs from the palette assets.
- `EditableMapBakeService` now extends `CompiledMap` with runtime-safe cell,
  object, room, zone, and socket payloads, blocks bake on validation errors,
  and can save a `CompiledMapAsset` directly from an edited draft.
- Dungeon runtime now reads baked draft payloads through
  `CompiledMapDungeonRuntimeService`, exposes baked cell/object counts in the
  dungeon UI readout, spawns field monsters from baked monster placements, and
  spawns first-pass dungeon object actors plus chest/barrel/torch interactions
  from baked object placements.
- `MapGeneratorWorkspaceEditor` now has a bridge button that saves the current
  generated result as an `EditableMapDraftAsset` without making the workspace a
  required dependency of the new pipeline.
- The fallback catalog generator is now layout-aware for the current ruins
  slice: it assigns hub, corridor, dead-end, and height-transition room kinds
  intentionally, selects matching chunk presets deterministically from role +
  layout + sockets, and rasterizes those chunk cells/objects directly into the
  generated editable draft.
- `MapGenerationQualityService` adds a production-shape gate for generated
  graphs: the current ruins slice must include non-overlapping rooms, branches
  on both sides of the main path, at least one loop, and the required hub,
  corridor, dead-end, and height-transition room kinds. Runtime bundle
  generation runs this gate before compile, so production runtime maps cannot
  bypass the graph shape contract.
- The high-level editor flow now treats `EditableMapDraftAsset` and
  `CompiledMap` as the primary outputs. The old `GeneratedMapDraft` graph
  remains only as an internal adapter stage inside the draft builder and core
  generator validation code, not as a saved/user-facing result type.

This is no longer only a Phase 1/2 slice. Draft authoring, palettes, preview,
validation, first-pass bake/runtime consumption, and initial production-shape
generation gates now exist, but deeper generator authoring and full dungeon
gameplay integration are still pending.

## Workspace Boundary

`MapGeneratorWorkspace` is still intentionally treated as legacy debug/reference
UI. It can generate a graph result and bridge that result into a new
`EditableMapDraftAsset`, but it is not the source of truth once the draft
exists.

Intentional incompatibilities today:

- Draft painting, validation overlays, bake output, and runtime payloads do not
  sync back into `MapGeneratorWorkspace`.
- Workspace preview cubes are still disposable debug output and are not kept in
  parity with the draft preview mesh.
- New validation/bake work should target `EditableMapDraftAsset` and
  `CompiledMap`, not extend the old `GeneratedMapDraft` preview path.

## Sample Asset

The repo now includes a saved sample draft at
`Assets/Conn/Authoring/Maps/Drafts/EditableMapDraft_Sample.asset`.
It is generated by `Conn.Editor.Maps.EditableMapDraftSampleAssetBuilder` and is
covered by EditMode validation/bake tests so there is a stable asset-first
example for preview, validation, and runtime bake checks.

The Chapter 2 fixed compiled samples at
`Assets/Conn/Core/Maps/ch2_first_slice_ruins_2001_CompiledMap.asset` and
`Assets/Conn/Core/Maps/ch2_first_slice_ruins_2112_CompiledMap.asset` are now
rebuilt through generated editable drafts and `EditableMapBakeService`. They
contain runtime-safe cell, room, socket, and object payloads rather than only
the legacy graph snapshot.

## Manual Unity Check Steps

Use these steps after pulling the branch:

1. Open Unity and let the project recompile.
2. Open the existing `MapGenerator` editor scene and select the
   `MapGeneratorWorkspace`.
3. In the `Production Scene Workflow` section, click `Generate Preview`.
   This creates selectable `Preview Room - ...` box nodes, edge links, and
   placement markers under the workspace `Preview Root` without saving a draft
   asset. Room nodes include a `MapPreviewRoomNode` component and an enlarged
   pick collider so they can be selected directly in Scene View.
4. Use `Random Seed + Generate Preview` until the generated candidate shape is
   acceptable.
5. Click `Accept Preview + Bake Map` to save the selected candidate as an
   `EditableMapDraftAsset`, validate it, and save the runtime
   `CompiledMapAsset`.
6. Use `Select Draft` only when you need the detailed draft inspector
   brush controls.
7. In the draft inspector, paint directly on the `Map Preview` grid. Choose a
   brush mode and terrain/material first, then left-click or drag on the
   preview.
8. Use `Build Playable From Drawing` to rebuild minimal room, zone, socket, and
   required-route metadata from the painted walkable cells.
9. Use `Build Scene Map` in the draft inspector and confirm that an
   `Editable Map Preview Root (...)` scene object appears with terrain, wall,
   slope, stair, object, and overlay children.
10. Use `Validate` and confirm the draft either passes or reports precise
   cell/object/socket errors in the inspector.
11. Use `Bake Runtime Map` or `Save Compiled Map Asset` and confirm the bake only
   succeeds when validation passes.
12. Use `Clear Preview` and confirm the preview root is deleted while the draft
   asset data remains unchanged.
13. Build the scene map again to confirm the draft asset is the source of truth
   and preview objects are disposable.

Automated coverage now verifies the checked-in `MapGenerator` scene has a
single `MapGeneratorWorkspace` with a workspace-owned preview root, camera, and
directional light. It also verifies draft preview meshes can be rebuilt under
that workspace root without mutating the draft asset.

## Purpose

The map editor and monster editor must not evolve as separate tools. A final
playable dungeon is the merge result of:

```text
Map profile + registered room/chunk assets + landmark rules
  + spawn tables / tag filters / encounter overrides
  -> validated runtime generation bundle
  -> generated compiled map per seed
```

The core design decision is that map generation is not pure randomness. It is a
designer-directed generator: creators register tile/wall/resource sets, landmark
rooms, room chunks, anchors, spawn tables, encounter pools, and weighting
rules. The generator assembles those authored pieces deterministically from a
profile and seed, then validates that the resulting map can be consumed by
Runtime. Production play should generate from validated rules and weights at
expedition start; pre-saved `CompiledMapAsset` files are still useful for fixed
maps, test fixtures, debug replay, and handcrafted content.

## References Used

Local references:

- `doc/ref/map/image.png`
- `doc/ref/map/image copy.png`
- `doc/ref/map/map01.png`
- `doc/dev/diablo_map_generation_design.md`
- Current code:
  - `Assets/Conn/Core/Maps/MapContracts.cs`
  - `Assets/Conn/Core/Maps/MapGenerationService.cs`
  - `Assets/Conn/Core/Maps/MapGenerationCatalog.cs`
  - `Assets/Conn/Editor/Maps/GeneratorWorkbenchWindow.cs`
  - `Assets/Conn/Core/Content/ContentDatabaseDefinition.cs`

User-provided video references:

- https://www.youtube.com/watch?v=frwfJM_m3JM&t=1s
- https://www.youtube.com/watch?v=okzMYiDcNKE&t=49s

External research:

- Unity Tilemap manual: https://docs.unity.cn/Manual/Tilemap.html
- Unity Rule Tiles tutorial: https://learn.unity.com/tutorial/using-rule-tiles
- Unity WeightedRandomTile API: https://docs.unity.cn/Packages/com.unity.2d.tilemap.extras%401.6/api/UnityEngine.Tilemaps.WeightedRandomTile.html
- Unity Addressables overview: https://docs.unity.cn/Packages/com.unity.addressables%401.22/manual/AddressableAssetsOverview.html
- 2D procedural generation with ScriptableObjects: https://www.gamedeveloper.com/design/2d-procedural-generation-in-unity-with-scriptableobjects
- Diablo II data file guide: https://wolfieeiflow.github.io/diabloiidatafileguide/
- Procedural 3D maps with snappable meshes: https://arxiv.org/abs/2108.00056
- Dungeon Architect graph grammar / room stitching reference: https://dungeonarchitect.dev/

## Current Baseline

Already implemented:

- `MapProfile`
- `ChunkPreset`
- `RoomGraph`
- `MapPlacement`
- `GeneratedMapDraft`
- `CompiledMap`
- `GeneratorWorkbenchWindow`
- seed-based generation
- start / quest target / boss / exit / monster / loot placement contracts
- saved `CompiledMapAsset` Runtime-first load
- compiled map placement -> field monster state registration

Still missing:

- custom inspectors and browser UX for designer-authored map profiles
- validation/build path for tile/wall/decor resource set registration
- validation/build path for landmark room/chunk registration
- validation/build path for biome/theme-aware spawn table or tag-filter selection
- encounter pool generation per map profile
- runtime generation bundle build step
- validation that a map profile cannot select unrelated spawn sources unless explicitly allowed

First-pass authoring asset schemas now exist in `Conn.Authoring`:

- `MapProfileAsset`
- `MapResourceSetAsset`
- `RoomChunkAsset`
- `LandmarkRoomAsset`
- `GenerationWeightProfileAsset`
- `SpawnTableAsset`

These assets are the source for future workbench selection and validation. They
do not make maps own monster data; map profiles reference spawn tables, tag
filters, and direct encounter overrides.

The Generator Workbench now exposes a first-pass authoring validation panel that
discovers map profiles, resource sets, chunks, landmark rooms, spawn tables, and
generation weight profiles. The validator checks id uniqueness, resource set
presence/theme compatibility, chunk sockets/anchors/population rules, required
landmark anchors, spawn table references, resolved spawn pools, invalid
weight/floor/difficulty ranges, encounter/monster theme or map-kind
compatibility, direct encounter overrides, weight profile references, and broken
Unity object references inside resource sets and chunks. Landmark validation
also checks required landmark roles, duplicate required roles, unique landmark
reuse, and invalid landmark count/repeat ranges in generation weights. Profile
validation checks linked chunk/landmark room size against the profile room size
and verifies representative role/socket chunk coverage before runtime generation.

The workbench can now select a `MapProfileAsset`, show its resource set,
landmark/chunk counts, spawn tables, tag filters, direct encounter overrides,
and generation weight profile, then generate from the selected profile through a
runtime bundle plus seed. If no profile is selected, it still uses the Chapter 2
catalog profile for existing validation and debug workflows.

A first-pass runtime-safe `RuntimeMapGenerationBundle` path now exists. The
bundle stores runtime map profile entries and chunk presets using ids/plain data,
and `RuntimeMapGenerationService` can generate a compiled map from
`bundle + profileId + seed`. The Generator Workbench can save a catalog bundle
or build one from validated authoring assets. Workbench generation context now
includes floor and difficulty, which are written into the runtime profile entry
before seed generation. This is still a minimal bridge: future work needs
resource realization ids and deeper progression UX beyond the first-pass
floor/difficulty inputs. Batch validation now reflects over the runtime bundle
contract and fails if bundle data types store `UnityEngine.Object`,
`Conn.Authoring`, `Conn.Editor`, or `UnityEditor` references.

Dungeon runtime bootstrap can receive `RuntimeMapGenerationBundleAsset`
references alongside saved `CompiledMapAsset` references. Runtime still prefers a
saved compiled map for fixed/debug/test fixtures; if no saved compiled map is
bound for the quest profile, it generates the compiled map from the matching
runtime bundle and seed before falling back to the old catalog generator.
Chapter validators and P0 scene generation now build the default
`RuntimeMapGenerationBundle.asset` and verify that the Dungeon `SceneBootstrap`
has a bundle binding.

Compiled encounter placement records are now present as a first-pass runtime
contract. Runtime generated maps can carry encounter id, primary monster id,
spawn source id, state key, and quest-required metadata for field monster
registration. Spawn table entries are now baked into runtime-safe weighted
entries and resolved deterministically from seed and placement id. Floor,
difficulty, theme tags, spawn role tags, allowed map tags, compatibility tags,
and room role constraints are part of the runtime-safe spawn filter. Batch
validation now also verifies that generated quest target and boss encounter
placements point at the correct map placement kind and resolve through
`RuntimeContentDatabase`.

## Target Workflow

```text
1. Register resources
   - floor/wall/door tiles or prefabs
   - decor objects
   - landmark rooms/chunks
   - spawn anchors

2. Author monster data
   - monster id, stats, AI
   - biome/theme tags
   - role tags: trash, elite, boss, ambush, coastal, undead, cultist
   - encounter membership

3. Author map profile
   - map kind: coast, ruins, temple, cave
   - theme id
   - room graph rules
   - allowed landmark set
   - allowed spawn tables / tag filters / encounter overrides

4. Generate draft
   - seed + profile
   - room graph
   - chunk/socket assembly
   - landmark insertion
   - placement pass
   - monster/encounter pass

5. Validate
   - map topology
   - required anchors
   - resource references
   - monster theme compatibility
   - encounter references
   - Runtime forbidden Editor references

6. Export runtime generation bundle
   - map generation rules
   - resource runtime ids
   - landmark/chunk weight tables
   - spawn source weight tables
   - validation hash/version

7. Generate or save map result
   - runtime generated map from profile + seed
   - optional `CompiledMapAsset` for fixed/debug/test maps
   - map profile id
   - spawn placements
   - encounter links
   - monster placement metadata
```

## Main Rule

The map editor owns spaces and anchors. The monster DB owns monsters and
encounters. The map editor only references spawn sources from that DB. The
generator workbench owns the merge:

```text
MapPlacement(anchor, room, theme, role)
  + SpawnTable or TagFilter(theme, role, tags, weight)
  + EncounterDefinition(enemy slots)
  + RuntimeMapGenerationBundle(profile rules, weights, content version)
  + seed
  -> CompiledEncounterPlacement
```

This prevents a beach/coast profile from randomly using desert, ruin, or temple
monsters unless a designer explicitly adds cross-theme rules.

## Runtime Generation Policy

Map generation weights should be authored in the editor, validated during the
build step, and exported into a runtime-safe generation bundle. Runtime should
roll or accept a seed at expedition start, generate the map in memory, and save
enough state to continue deterministically.

Recommended save data:

- `profileId`
- `bundleVersion` or validation hash
- `seed`
- completed/defeated placement state
- optional serialized generated map when old bundle versions cannot be kept

This keeps the map editor responsible for authoring control while still allowing
Diablo-like random variation every time the player starts a new run.

## Next Design Document

The concrete schema and editor UX plan is in:

- `map_monster_editor_coop_design.md`

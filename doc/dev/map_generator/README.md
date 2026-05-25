# Map Generator and Monster Editor Cooperation Plan

Date: 2026-05-25
Status: first-pass authoring, validation, workbench, and runtime bundle bridge implemented; manual Game view verification remains.

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

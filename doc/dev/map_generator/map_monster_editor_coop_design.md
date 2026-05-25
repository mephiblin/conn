# Map Editor and Monster Editor Cooperation Design

Date: 2026-05-25
Status: first-pass implementation tracked through Inspector-first editor, spawn table, map authoring, Generator Workbench, RuntimeMapGenerationBundle, and validation checkpoints.

## Design Goal

Build a map editor that behaves like a game production tool, not a random map
toy. The designer should be able to register rooms, landmarks, tiles, walls,
decor, spawn tables, tag filters, and encounter pools, then generate
deterministic maps that respect those authored constraints.

Example:

```text
Map profile: coastal shrine
Theme: coast
Allowed landmark rooms:
  - tide gate
  - flooded altar
  - broken pier
Allowed spawn sources:
  - spawn_coast_common
  - spawn_coast_elite
  - encounter_drowned_oracle as boss override
Forbidden by default:
  - desert_raiders
  - temple_serpents
  - ruin_constructs
```

The production result should be a validated `RuntimeMapGenerationBundle` plus a
seed-driven generated map. A `CompiledMapAsset` is still valuable as one
generated output for fixed maps, debug repro, validation snapshots, and
handcrafted test cases. Neither path should copy or own monster stats.

## Research Summary

### Unity Tilemap and Rule-Based Resource Registration

Unity Tilemaps are intended to store tile assets on a grid and connect that data
to renderers and colliders. This supports the idea that the editor should let the
user register floor, wall, door, and collision resources instead of hardcoding
them in the generator. Unity's Rule Tile workflow is relevant because it allows
adjacency-based tile selection and tile variation; this maps directly to our
future wall/corner/floor substitution pass.

Project implication:

- `TileSetProfile` should reference tile or prefab assets by role.
- `TileRuleSet` should choose wall/corner/door/floor variants after the logical
  map is generated.
- The generator should output logical cells and placements first, then render
  through user-registered resources.

Sources:

- Unity Tilemap manual: https://docs.unity.cn/Manual/Tilemap.html
- Unity Rule Tiles tutorial: https://learn.unity.com/tutorial/using-rule-tiles

### ScriptableObject Room Templates

The ScriptableObject-based procedural generation article describes a production
style where creators draw room templates in Unity Tilemaps, then save those room
templates as data. It also mentions using special tiles such as a possible
monster spawn tile. This matches our desired split:

- map editor authors reusable chunks and spawn anchors
- monster editor defines monster metadata and encounter participation
- generator joins both in a validation step

Source:

- https://www.gamedeveloper.com/design/2d-procedural-generation-in-unity-with-scriptableobjects

### Diablo II-Style Preset Rooms and Maze Rules

The Diablo II data file guide is useful because it shows the split between
high-level maze rules and preset/static pieces:

- maze files control room counts, room sizes, and merge chance
- preset files define static level pieces and the `.ds1` files used as content
  variants
- preset data can also control whether units may populate that area

Project implication:

- `MapProfile` should define room count, critical path, merge/loop chance, and
  side-branch rules.
- `RoomChunkDefinition` / `LandmarkRoomDefinition` should define designer-made room
  content.
- Each room/chunk should be able to say whether monster population is allowed.
- Landmark rooms should be selected by role, theme, rarity, and required anchor
  contracts.

Source:

- https://wolfieeiflow.github.io/diabloiidatafileguide/

### Snappable Meshes and Designer Control

The snappable-mesh paper proposes assembling premade meshes using designer
constraints, with immediate feedback about navigability. This is a good mental
model for future 3D or 2.5D dungeon pieces: pieces snap by sockets, but the
designer controls look, constraints, and allowed piece sets.

Project implication:

- `ChunkPreset` sockets must stay explicit.
- preview/debug UI should explain why a chunk failed to fit.
- generated maps must validate connectivity and required gameplay anchors before
  saving.

Source:

- https://arxiv.org/abs/2108.00056

### Graph Grammar and Room Stitching

Dungeon Architect documents a graph grammar style where designers define graph
rules, stitch prebuilt rooms, and debug generation failures. We should not copy
that tool, but the workflow is relevant: graph first, room stitching second,
debug/validation panel always visible.

Project implication:

- Map Editor should show `RoomGraph` before compiled map output.
- Designers need node roles, sockets, critical path, side branches, and failed
  chunk selection reasons.
- Key-lock, quest, boss, treasure, and spawn rooms should be graph roles, not
  post-hoc random positions.

Source:

- https://dungeonarchitect.dev/

### Runtime Generation and Weighted Authoring Data

External references support a split between authoring-time data and runtime
generation:

- Unity ScriptableObjects are appropriate shared data containers for reusable
  configuration referenced by prefabs and systems.
- Unity Addressables can reference prefabs/assets by address or `AssetReference`
  and load them at runtime.
- Diablo II-style dungeons use preset rooms and maze/level rules, then assemble
  them randomly during play.
- Unity's weighted/random tile APIs show that weighted variation is a normal
  Unity data concept, but project-level map generation should keep those
  weights in explicit profile assets instead of hiding them inside code.

Project implication:

- The editor should author and validate generation rules, weights, room sets,
  resource sets, and spawn sources.
- The runtime should be able to roll a seed and generate a new dungeon from a
  validated runtime generation bundle.
- `CompiledMapAsset` remains useful for fixed test maps, debug repro, saved
  quest maps, and validation snapshots, but it should not be the only final
  production path.

Sources:

- Unity ScriptableObject manual: https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html
- Unity Addressables overview: https://docs.unity.cn/Packages/com.unity.addressables%401.22/manual/AddressableAssetsOverview.html
- Diablo II data file guide: https://wolfieeiflow.github.io/diabloiidatafileguide/
- Unity WeightedRandomTile API: https://docs.unity.cn/Packages/com.unity.2d.tilemap.extras%401.6/api/UnityEngine.Tilemaps.WeightedRandomTile.html

## Existing Project Mapping

Current structures:

| Current type | Current role | Needed extension |
| --- | --- | --- |
| `MapProfile` | one map grammar/profile | add resource set id, landmark set id, spawn table ids, tag filters, direct encounter overrides |
| `ChunkPreset` | socket-compatible room piece | add population flags, tags, rarity, authored tile/prefab asset refs |
| `RoomGraph` | generated node/edge graph | add explicit landmark nodes, lock/key edges, spawn budgets |
| `MapPlacement` | start/quest/boss/exit/monster/loot point | add spawn source id, direct encounter id, placement difficulty, biome |
| `CompiledMap` | one generated runtime map result | add generated encounter placement records |
| new runtime generation bundle | validated runtime-safe generator input | add profile rules, chunk weights, resource ids, spawn source ids |
| `ContentMonsterDefinition` | monster stats | add biome/theme tags and spawn role tags |
| `ContentEncounterDefinition` | combat group | add theme tags, spawn roles, difficulty band |

## Proposed Data Contracts

### MapResourceSetDefinition

Purpose: user-registered tile/wall/decor resources.

```text
MapResourceSetDefinition
├─ id
├─ displayName
├─ themeId
├─ floorTiles / floorPrefabs
├─ wallTiles / wallPrefabs
├─ doorTiles / doorPrefabs
├─ blockedTiles / collisionPrefabs
├─ decorObjectIds
├─ lightProfileId
└─ nav/collider mode
```

Rules:

- A `MapProfile` must reference one resource set.
- Resource set theme should match profile theme unless explicitly compatible.
- Runtime should consume baked references or stable ids, not editor-only objects.

### LandmarkRoomDefinition

Purpose: Diablo II-style manually registered room or landmark.

```text
LandmarkRoomDefinition
├─ id
├─ displayName
├─ themeId
├─ roomRole
├─ socketMask
├─ size
├─ weight
├─ uniquePerMap
├─ populationAllowed
├─ requiredAnchors
├─ optionalAnchors
├─ tileLayerRef / prefabRef
└─ preview metadata
```

Examples:

- `coast_broken_pier_start`
- `coast_flooded_altar_boss`
- `ruins_collapsed_side_loot`
- `temple_serpent_gate`

Rules:

- Landmark rooms may be required by a profile.
- Landmark rooms may be optional weighted variants.
- If `populationAllowed=false`, monster placement pass must skip that room.

### SpawnTableDefinition

Purpose: bridge map profiles to independent monster/encounter data without
making the map own monster definitions.

```text
SpawnTableDefinition
├─ id
├─ displayName
├─ tagFilters
├─ encounterEntries
│  ├─ encounterId
│  ├─ weight
│  ├─ minFloor
│  ├─ maxFloor
│  └─ roomRoleConstraints
├─ directMonsterEntries
│  ├─ monsterId
│  ├─ weight
│  └─ generatedEncounterPolicy
├─ allowedRoomRoles
└─ notes
```

Example:

```text
id: spawn_coast_common
tagFilters: theme=coast, role=trash, floor=1..3
encounterEntries:
  - encounter_reef_cultist weight=60
  - encounter_black_water_beast weight=20
allowedRoomRoles: MainPath, SideBranch, QuestTarget
```

Rules:

- `MapProfile.spawnTableIds` controls which spawn sources can appear.
- `MapProfile.spawnTagFilters` can select DB monsters/encounters by metadata.
- Individual placements may override the spawn table with a specific encounter.
- Cross-theme usage requires an explicit compatibility tag.
- Direct monster entries are allowed for simple authoring and special placement
  overrides. During build they should either resolve to existing encounters or
  generate explicit single-primary encounter records so enemy slots, pattern, and
  rewards remain inspectable.

### EncounterPlacementRule

Purpose: tell the generator how to turn map anchors into encounter placements.

```text
EncounterPlacementRule
├─ id
├─ mapProfileId
├─ roomRole
├─ placementKind
├─ spawnSourceId
├─ encounterPoolId
├─ densityPerRoom
├─ minDistanceFromStart
├─ maxPerMap
├─ eliteChance
├─ bossRequired
└─ fallbackEncounterId
```

Rules:

- `Boss` room requires a boss encounter if the profile demands a boss anchor.
- `QuestTarget` room can use quest target encounter directly.
- `MainPath` gets predictable pressure.
- `SideBranch` gets optional danger or reward-guard encounters.

### GenerationWeightProfile

Purpose: make random generation tunable without code changes.

```text
GenerationWeightProfile
├─ id
├─ mapProfileId
├─ landmarkWeights
│  ├─ landmarkRoomId
│  ├─ weight
│  ├─ minCount
│  └─ maxCount
├─ chunkWeights
│  ├─ chunkId
│  ├─ roomRole
│  ├─ weight
│  └─ maxRepeat
├─ spawnSourceWeights
│  ├─ spawnSourceId
│  ├─ roomRole
│  ├─ weight
│  └─ difficultyBand
├─ decorWeights
└─ lootWeights
```

Rules:

- Weights are authored in editor assets.
- Runtime receives a compiled, runtime-safe copy of the weight profile.
- The same profile and seed must generate the same map.
- Changing weights changes future generation, not existing saved maps unless the
  saved map only stores seed/profile and is intentionally regenerated.

### RuntimeMapGenerationBundle

Purpose: runtime-safe generator input produced by the editor build step.

```text
RuntimeMapGenerationBundle
├─ profileId
├─ graph rules
├─ resourceSetRuntimeIds
├─ landmark room runtime ids
├─ chunk runtime ids
├─ generation weights
├─ spawn table ids
├─ encounter placement rules
└─ validation hash/version
```

This bundle is the production runtime path. `CompiledMapAsset` is the output of
one generated seed.

Runtime should never depend on editor-only object references in this bundle.
Prefab, tile, material, audio, and VFX references must be converted to stable
runtime ids, Addressables keys, or other runtime-safe references during the build
step.

### CompiledEncounterPlacement

Purpose: final merged output consumed by Runtime.

```text
CompiledEncounterPlacement
├─ placementId
├─ mapPlacementId
├─ roomId
├─ encounterId
├─ spawnSourceId
├─ primaryMonsterId
├─ spawnRole
├─ x
├─ y
├─ stateKey
└─ requiredForQuest
```

This should eventually replace the current loose pattern where `MapPlacement`
only has `ReferenceId` and Runtime derives encounter/monster from quest state or
fallback constants.

## Map Editor UX

Target window:

```text
Map Generator Workbench
├─ Profile
│  ├─ Map profile selector
│  ├─ Theme/resource set selector
│  ├─ Landmark set selector
│  └─ Spawn table / tag filter / override selectors
├─ Resources
│  ├─ tile/wall/decor registration
│  ├─ preview
│  └─ missing asset validation
├─ Rooms
│  ├─ landmark room list
│  ├─ socket/anchor editor
│  ├─ population allowed toggle
│  └─ room role tags
├─ Generation
│  ├─ seed
│  ├─ generate draft
│  ├─ graph view/list
│  ├─ chunk selection diagnostics
│  └─ placement list
├─ Encounters
│  ├─ allowed spawn sources
│  ├─ encounter pool preview
│  ├─ generated encounter placements
│  └─ invalid theme mismatch warnings
├─ Validation
│  ├─ errors
│  ├─ warnings
│  └─ save gate
└─ Build / Save
   ├─ build runtime generation bundle
   ├─ save draft preview
   ├─ save compiled map asset for fixed/debug/test use
   └─ register compiled map in scene/bootstrap test set
```

## Monster Editor UX Additions

Current Monster Editor edits stat fields. It needs production metadata:

```text
Monster Detail
├─ Basic stats
├─ Theme tags
├─ Biome tags
├─ Spawn roles
├─ Spawn table membership preview
├─ Encounter usage list
├─ Boss/elite flags
└─ Map compatibility preview
```

For a selected map profile, the Monster Editor should show:

- compatible monsters
- incompatible monsters and why
- encounters that can appear in that profile
- boss candidates
- quest target candidates

## Validation Rules

### Resource validation

- `MapProfile.resourceSetId` exists.
- resource set theme is compatible with map profile theme.
- tile/wall/door/decor references are present.
- room/chunk size matches profile room size.
- sockets are compatible with graph edges.

### Landmark validation

- required landmark role exists for each required room role.
- required anchors exist inside the selected chunk.
- unique landmarks are not repeated.
- population-disabled landmarks do not receive monster placements.

### Monster/map compatibility validation

- every allowed spawn table exists.
- every spawn table resolves to at least one valid monster or encounter.
- every selected encounter's primary monster has matching theme or compatibility tag.
- generated monster placement resolves to a valid encounter.
- boss room resolves to boss encounter when boss placement is required.
- quest target encounter primary monster matches quest target monster.

### Runtime validation

- runtime generation bundle loads without Editor-only references.
- runtime can generate a compiled map instance from profile id + bundle version + seed.
- optional fixed/debug map loads from `CompiledMapAsset`.
- compiled encounter placements register field monster state.
- generated encounter starts through `CombatRuntimeService`.
- no Runtime/Core/UI Runtime references Editor-only assemblies.

## Generation Formula

The recommended formula:

```text
MapProfile + seed
  + RuntimeMapGenerationBundle
  -> RoomGraph
  -> Landmark injection
  -> Chunk/socket selection
  -> Tile/resource realization
  -> Base placements
  -> Encounter placement pass
  -> Loot/decor/light pass
  -> Validation report
  -> runtime CompiledMap instance
  -> optional CompiledMapAsset + encounter placement bundle
```

Key point: encounter placement is after room/chunk selection, because room role,
theme, population flags, and anchors are needed before choosing monster data.

## Runtime Generation Policy

For production play, the map should usually be generated at expedition start:

```text
Quest / floor selection
  -> choose MapProfile
  -> choose or roll seed
  -> load RuntimeMapGenerationBundle
  -> generate CompiledMap in memory
  -> register field monster and encounter placements
  -> save profile id + seed + completed placement state
```

Save strategy:

- For deterministic regeneration, save `profileId`, `bundleVersion`,
  `seed`, and runtime delta such as defeated placements.
- For debug/replay or handcrafted test maps, save/use a `CompiledMapAsset`.
- If generation rules change incompatibly, either keep a versioned bundle for
  old saves or serialize the generated compiled map into the save.

This gives Diablo-like variation on every new run while keeping the editor
responsible for authoring and validating all possible pieces.

## Example: Coastal Map

Input:

```text
MapProfile
  id: coast_first_slice
  theme: coast
  resourceSetId: coast_tiles_01
  landmarkSetId: coast_landmarks_01
  spawnTableIds:
    - spawn_coast_common
    - spawn_coast_elite
  directBossEncounterId: encounter_drowned_oracle
```

Generation result:

```text
RoomGraph
  start: broken_pier_start
  main_1: tide_pool_main
  quest_target: flooded_altar
  boss: sea_cave_boss
  exit: cliff_exit

Placements
  start -> player spawn
  main_1 monster anchor -> encounter_reef_cultist
  quest_target anchor -> quest target encounter
  boss anchor -> encounter_drowned_oracle
  side_branch loot anchor -> shell_cache
```

Invalid example:

```text
coast_first_slice uses encounter_desert_rat
```

Expected validation:

```text
Error: encounter_desert_rat does not match coast_first_slice spawn tables, tag filters, or direct overrides.
```

## Implementation Phases

### Implemented Foundation

- Added first-pass authoring asset schemas in `Assets/Conn/Authoring`.
- Monster and encounter authoring assets include theme, biome/spawn role, map
  compatibility, and difficulty metadata.
- Spawn table authoring supports encounter entries, direct monster entries,
  tag filters, weights, floor constraints, difficulty constraints, and room role
  constraints.
- Map profile authoring links resource set, required landmarks, optional
  chunks/landmarks, spawn tables, tag filters, direct encounter overrides, and a
  generation weight profile.
- Map authoring validation now checks profile/resource/spawn/chunk compatibility,
  spawn table resolved pools, weight/floor/difficulty ranges, and
  encounter/monster theme or map-kind compatibility before bundle generation.
  Resource set and chunk validation also rejects null/broken Unity object
  references inside authored resource arrays.
- Landmark validation checks non-empty landmark roles, duplicate required
  landmark roles, unique landmark reuse, and generation weight count/repeat
  ranges.
- Profile validation checks linked chunk/landmark room size against profile room
  size and verifies representative role/socket coverage for generated room
  roles.
- Added a first-pass runtime-safe `RuntimeMapGenerationBundle` asset and service
  that can generate compiled maps from `bundle + profileId + seed`.
- Dungeon runtime can now bind `RuntimeMapGenerationBundleAsset` references via
  `SceneBootstrap`; saved `CompiledMapAsset` fixtures still win first, then a
  matching runtime bundle generates the map before catalog generator fallback.
- Chapter validators/P0 scene generation build the default
  `RuntimeMapGenerationBundle.asset` and verify the Dungeon scene bootstrap
  binding.
- Batch validation reflects over `RuntimeMapGenerationBundle` contract types and
  rejects `UnityEngine.Object`, `Conn.Authoring`, `Conn.Editor`, or `UnityEditor`
  fields in runtime bundle data.
- Runtime generation now supports compiled encounter placement records with
  encounter id, primary monster id, spawn source id, state key, and quest-required
  markers. Field monster registration reads those records when present and keeps
  the previous quest encounter fallback when they are absent.
- Batch validation verifies generated quest target and boss encounter placement
  records target the expected placement kind and resolve through
  `RuntimeContentDatabase`.
- Spawn table encounter entries and direct monster entries are baked to
  runtime-safe weighted entries. Runtime map generation resolves them
  deterministically from seed and placement id, with direct monster entries
  using generated single-primary encounter ids.
- Runtime spawn entries now carry floor, difficulty, theme, spawn role, allowed
  map, compatibility, and room role constraints. The resolver filters on those
  fields before weighted selection.
- Generator Workbench now exposes floor and difficulty inputs and writes them
  into built runtime map profile entries before `bundle + profileId + seed`
  generation, so spawn-table progression constraints can be tested from the
  editor.
- Content Database Window's authoring browser now shows SpawnTable membership
  and MapProfile usage preview, making the map-to-spawn-source relationship
  visible without making maps own monster data.
- Unity object references remain on authoring assets. Runtime-safe Core
  contracts only received stable id/list metadata needed by later bake/export
  steps.

### Phase MG-0: Schema Planning

- [x] Add `MapResourceSetDefinition` design.
- [x] Add `LandmarkRoomDefinition` design.
- [x] Add `SpawnTableDefinition` design.
- [x] Add `EncounterPlacementRule` design.
- [x] Add `GenerationWeightProfile` design.
- [x] Add `RuntimeMapGenerationBundle` design.
- [x] Decide whether these live inside `ContentDatabaseDefinition` or separate map
  database assets.

Recommended decision:

- content identity stays in `ContentDatabaseDefinition`
- optional stored generated outputs stay in `CompiledMapAsset`
- runtime generation inputs are built into `RuntimeMapGenerationBundle`
- heavy visual resource refs can live in Editor-only map authoring assets that
  bake stable runtime ids into compiled output

### Phase MG-1: Map Profile / Resource Editor

- [x] Profile selector.
- [x] Theme/resource set selector.
- [x] Resource missing reference validation.
- [x] Seed generation remains available.
- [x] Current hardcoded `ChapterTwoFirstSliceProfile()` becomes import/sample data,
  not the only source.

### Phase MG-2: Landmark / Chunk Editor

- [x] Landmark room list.
- [x] Socket editor.
- [x] Anchor editor.
- [x] Population allowed toggle.
- [x] Preview generated room graph and selected chunks.

### Phase MG-3: Spawn Source Integration

- [x] Add theme/spawn-role metadata to monster editor.
- [x] Add spawn table list/editor.
- [x] Add map profile spawn table selector.
- [x] Add map profile tag-filter and direct-override selectors.
- [x] Add encounter pool preview per profile.

### Phase MG-4: Encounter Placement Pass

- [x] Generate `CompiledEncounterPlacement`.
- [x] Link map anchors to encounter definitions.
- [x] Preserve current `single_primary` fallback.
- [x] Add validator proving DB encounter survives into `CombatRuntimeService`.

### Phase MG-5: Runtime Weighted Generation

- [x] Build runtime generation bundle from editor-authored profiles, chunks, weights,
  resources, and spawn sources.
- [x] At expedition start, roll or accept a seed and generate the compiled map in
  runtime code.
- [x] Keep deterministic regeneration from profile id + bundle version + seed.
- [x] Keep saved `CompiledMapAsset` support for fixed test maps and debug repro.

### Phase MG-6: Runtime Consumption

- [x] Field monster actors spawn from compiled encounter placements.
- [x] `FieldMonsterState` stores placement state key.
- [x] Combat handoff uses compiled encounter id.
- [x] Quest target placement remains compatible with current quest flow.

## Open Decisions

1. Should `MapResourceSetDefinition` be a runtime asset or editor-only authoring
   asset?
   - Recommendation: editor-only rich asset, compiled runtime ids in
     `RuntimeMapGenerationBundle` and generated/compiled map output.

2. Should spawn tables be separate definitions or tags on monsters?
   - Recommendation: both. Tags live on monsters/encounters; spawn tables are
     curated reusable pools with explicit encounter lists.

3. Should random monster placement choose monsters directly or encounters?
   - Recommendation: choose encounters. Monsters are lower-level ingredients;
     encounter definitions preserve enemy slots, pattern, rewards, and future
     multi-enemy behavior.

4. How strict should theme compatibility be?
   - Recommendation: strict error by default; explicit compatibility tags allow
     exceptions.

5. How should handcrafted landmarks override random generation?
   - Recommendation: profile can require landmark roles. Generator reserves graph
     nodes first, then fills remaining nodes with weighted random chunks.

6. Should production play load pre-saved compiled maps or generate at runtime?
   - Recommendation: generate at runtime from a validated
     `RuntimeMapGenerationBundle` and seed. Keep pre-saved compiled maps for
     test fixtures, handcrafted maps, and debug replay.

## Immediate Next Work

1. Complete manual Unity Play Mode verification for the Phase 6 three-quest
   sequence and Phase 8 Game view checklist.
2. Keep `editor_tool_content_pipeline_plan.md`, this design doc, and
   `remaining_work.md` synchronized before marking manual `[!]` items complete.
3. After manual verification, decide the next production pass: deeper map
   resource realization UX, richer landmark/chunk editing, or broader authored
   content coverage.

Monster Field FSM work now depends on preserving this map/monster placement
contract. Future FSM changes must keep compiled placement state keys and
encounter ids reliable.

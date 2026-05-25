# Inspector-First Editor Architecture

Date: 2026-05-25
Status: first-pass Inspector-first authoring assets, ContentDatabase bridge reduction, spawn/map authoring, Generator Workbench, RuntimeMapGenerationBundle, and automated validation are implemented; manual Play Mode verification remains.

## Core Claim

The current editor direction is too database-centric. It is acceptable as a
bootstrap/import step, but it should not become the final production workflow.

Unity authoring should center on:

- assets in the Project Browser
- Inspector editing
- prefab and FBX references
- Scene View manipulation
- generated previews
- validation/build windows

The final editor should feel like a Unity-native world/content editor, closer in
spirit to Warcraft III World Editor than to a web admin table.

## What Is Wrong With Direct DB Editing

Directly editing arrays in `ContentDatabase.asset` has these problems:

1. It hides Unity assets.
   - meshes, FBX files, prefabs, materials, tile assets, audio, VFX, and
     colliders are not first-class in the editing flow.

2. It encourages string-only workflows.
   - ids are edited as text instead of selected from assets or typed references.

3. It weakens preview.
   - a monster row does not show its prefab, scale, collider, animation, attack
     range, nav footprint, or spawn preview.

4. It makes Undo/Redo and prefab workflows awkward.
   - Unity already provides asset serialization, prefab overrides, inspectors,
     and object fields.

5. It blurs source data and compiled data.
   - `ContentDatabase.asset` should eventually be a runtime registry/build
     artifact, not the only authoring surface.

6. It does not match map production.
   - rooms, landmarks, chunks, spawn anchors, and resource sets need visual
     authoring.

## Target Layering

```text
Authoring Layer
  MonsterDefinitionAsset
  EncounterDefinitionAsset
  QuestDefinitionAsset
  ItemDefinitionAsset
  NpcDefinitionAsset
  VendorDefinitionAsset
  MapProfileAsset
  RoomChunkAsset
  LandmarkRoomAsset
  MapResourceSetAsset
  SpawnTableAsset

Editor Tool Layer
  Custom Inspectors
  Scene View tools
  Browser windows
  Generator workbench
  Validation window
  Build/export command

Compiled Runtime Layer
  ContentDatabase.asset
  RuntimeMapGenerationBundle
  CompiledMapAsset
  Runtime lookup tables
  Build manifest
```

The authoring layer is for designers. The compiled runtime layer is for the
game.

## Warcraft III Benchmark Adaptation

Warcraft III World Editor is a good benchmark because it separates major editing
concerns:

- terrain shaping
- unit placement
- doodad/destructible placement
- object data editing
- trigger logic
- import/resource management
- test map

For this project, the equivalent structure should be:

```text
Conn Editor
├─ World / Map Editor
│  ├─ terrain or room/chunk layout
│  ├─ landmark placement
│  ├─ spawn anchors
│  ├─ exit/gate/start anchors
│  └─ scene view preview
├─ Object / Content Editor
│  ├─ monsters
│  ├─ encounters
│  ├─ items/equipment
│  ├─ skills
│  ├─ NPCs
│  └─ vendors
├─ Resource Manager
│  ├─ FBX/mesh references
│  ├─ prefabs
│  ├─ materials
│  ├─ tiles/walls
│  ├─ VFX/audio
│  └─ import validation
├─ Trigger / Quest Editor
│  ├─ quest graph
│  ├─ event conditions
│  ├─ rewards
│  └─ NPC/service hooks
├─ Generator Workbench
│  ├─ profile selection
│  ├─ seed
│  ├─ graph/room preview
│  ├─ spawn table preview
│  └─ compiled map save
└─ Build & Validate
   ├─ content validation
   ├─ map validation
   ├─ reference scan
   └─ runtime contract tests
```

## Asset Types

### MonsterDefinitionAsset

Authoring source for monster content.

```text
MonsterDefinitionAsset
├─ id
├─ displayName
├─ stats
├─ AI profile
├─ prefab
├─ mesh/FBX reference
├─ animator/controller
├─ collider/nav footprint
├─ VFX/audio refs
├─ theme/biome tags
├─ spawn role tags
└─ encounter usage preview
```

The Inspector should show visual references and preview metadata. The compiled
runtime DB should receive ids and runtime-safe data, not editor-only objects.

### EncounterDefinitionAsset

```text
EncounterDefinitionAsset
├─ id
├─ displayName
├─ enemy slots
├─ pattern
├─ reward id/table
├─ difficulty band
├─ spawn role tags
├─ allowed map tags
└─ preview combat setup
```

Encounters should reference monster assets or monster ids, not duplicate monster
stats.

### SpawnTableAsset

This replaces the earlier overly strong "monster family belongs to map" wording.
Monsters remain independent in monster DB. Map profiles select spawn tables,
tag filters, or direct overrides.

```text
SpawnTableAsset
├─ id
├─ displayName
├─ tag filters
├─ encounter entries
│  ├─ encounter asset/id
│  ├─ weight
│  ├─ min floor
│  ├─ max floor
│  └─ room role constraints
└─ validation preview
```

Map profiles do not own monsters. They reference spawn sources.

### MapProfileAsset

```text
MapProfileAsset
├─ id
├─ map kind
├─ theme
├─ resource set
├─ graph rules
├─ required landmark rooms
├─ optional landmark sets
├─ allowed spawn tables
├─ direct encounter overrides
└─ validation rules
```

### RoomChunkAsset / LandmarkRoomAsset

```text
RoomChunkAsset
├─ id
├─ theme
├─ socket mask
├─ room role tags
├─ population allowed
├─ anchors
├─ tilemap/prefab reference
├─ preview scene or thumbnail
└─ collision/nav settings
```

This is where mesh, FBX, prefab, tile, wall, and collision references belong.

### MapResourceSetAsset

```text
MapResourceSetAsset
├─ id
├─ theme
├─ floor resources
├─ wall resources
├─ door resources
├─ decor resources
├─ material palette
├─ lighting profile
└─ collider/nav mode
```

## EditorWindow Roles

### Content Browser Window

Not a table editor. It should:

- list assets by type
- create new typed assets
- ping/select assets
- run validation for selected assets
- show dependency graph
- build compiled DB

### Object Inspector

Custom inspectors should edit the actual content.

Examples:

- monster prefab field uses `ObjectField`
- encounter slots use typed monster selectors
- spawn table entries use encounter asset selectors
- validation errors appear in inspector

### Map Editor Window

Coordinates map work:

- selected `MapProfileAsset`
- selected resource set
- selected room/chunk library
- selected spawn tables
- seed/generate controls
- graph and placement preview
- save compiled map

The actual room/chunk/landmark assets are edited in Inspector or Scene View
tools.

### Scene View Tools

Used for:

- placing anchors
- previewing room sockets
- drawing chunk bounds
- moving spawn points
- editing door/exit/start placement
- viewing nav/collision footprint

### Build & Validate Window

Runs:

- content DB build
- compiled map build
- reference validation
- forbidden runtime/editor reference scan
- Chapter 1/2 validators

## Data Flow

```text
Designer edits:
  MonsterDefinitionAsset
  EncounterDefinitionAsset
  SpawnTableAsset
  MapProfileAsset
  RoomChunkAsset
  MapResourceSetAsset

Build step:
  validate authoring assets
  assign stable ids
  resolve object references
  bake runtime-safe data
  write ContentDatabase.asset
  write RuntimeMapGenerationBundle
  optionally write CompiledMapAsset for fixed/debug/test maps

Runtime:
  read ContentDatabase.asset
  generate map from RuntimeMapGenerationBundle + seed
  optionally read CompiledMapAsset for fixed/debug/test maps
  never reference Editor assemblies
```

## Migration From Current DB Editor

### Stage 0: Keep Current Tool As Bridge

Keep `ContentDatabaseWindow` because it is already useful for:

- legacy JSON import
- quick validation
- bootstrap content edits
- DB-first runtime proof

But stop treating it as the final editor UX.

### Stage 1: Create Typed Authoring Assets

Add ScriptableObject authoring assets:

- `MonsterDefinitionAsset`
- `EncounterDefinitionAsset`
- `MapProfileAsset`
- `RoomChunkAsset`
- `LandmarkRoomAsset`
- `MapResourceSetAsset`
- `SpawnTableAsset`
- `GenerationWeightProfileAsset`

Each asset gets:

- stable id
- display name
- preview fields
- validation method or validator integration

Current first-pass implementation:

- `Conn.Authoring.Content.MonsterDefinitionAsset`
- `Conn.Authoring.Content.EncounterDefinitionAsset`
- `Conn.Authoring.Content.SpawnTableAsset`
- `Conn.Authoring.Maps.MapProfileAsset`
- `Conn.Authoring.Maps.MapResourceSetAsset`
- `Conn.Authoring.Maps.RoomChunkAsset`
- `Conn.Authoring.Maps.LandmarkRoomAsset`
- `Conn.Authoring.Maps.GenerationWeightProfileAsset`

These types are intentionally outside `Conn.Editor` and contain no
`UnityEditor` API usage. Rich Unity object references stay on the authoring
assets. Conversion helpers only emit runtime-safe ids and plain content/map
contract fields.

### Stage 2: Add Custom Inspectors

Use Inspector-first editing:

- object fields for prefabs/materials/meshes
- enum/dropdown fields for supported runtime values
- reorderable lists for slots/rules
- inline validation panel
- dependency preview

### Stage 3: Turn DB Window Into Browser/Compiler

`ContentDatabaseWindow` becomes:

```text
Content Browser / Database Build
├─ source asset folders
├─ import legacy JSON
├─ find/create content assets
├─ validate selected/all
├─ build ContentDatabase.asset
└─ build report
```

It no longer directly edits every field as the primary workflow.

Current first-pass bridge:

- Authoring tab discovers monster, encounter, and spawn table assets.
- Authoring validation checks ids, duplicate ids, missing monster/encounter
  references, invalid weights, and empty spawn pools.
- Build/export currently upserts authored monsters and encounters into
  `ContentDatabaseDefinition`.
- Existing direct DB tabs remain available for bootstrap editing and fallback
  continuity until the asset path is broader and fully validated.

### Stage 4: Map Editor Uses Assets

`GeneratorWorkbenchWindow` should select:

- `MapProfileAsset`
- `MapResourceSetAsset`
- `RoomChunkAsset` library
- `SpawnTableAsset`

Then it generates a draft, validates it, and saves a compiled asset.
For production procedural play it also builds a runtime generation bundle that
contains validated graph rules, chunk/landmark weights, resource ids, and spawn
source weights. Current first-pass Workbench generation writes seed, floor, and
difficulty into the runtime generation context so spawn-table progression
filters can be validated from editor-authored assets.

### Stage 5: Runtime Bundles Only

Runtime consumes:

- compiled content DB
- runtime generation bundle
- generated compiled map instance
- optional compiled map asset for fixed/debug/test maps
- stable ids
- prefab/runtime asset references only where safe

Editor-only authoring helpers do not enter Runtime/Core/UI Runtime.

## Monster and Map Relationship Correction

Monsters must not be owned by maps.

Correct relationship:

```text
Monster DB is independent.
MapProfile references spawn sources.
Spawn sources reference encounters/monsters by id or asset.
CompiledMap stores selected encounter placement ids.
Runtime resolves stats through ContentDatabase.
```

Map editor selection modes:

1. Spawn table selection
   - recommended default
   - best for maintainability

2. Tag filter selection
   - good for large monster databases

3. Direct monster/encounter override
   - good for boss rooms, tutorials, quest-specific moments

This keeps map production flexible without binding monster ownership to a map.

## Validation Rules

Authoring validation:

- id uniqueness
- missing prefab/mesh/material references
- unsupported runtime enum/string values
- missing collider/nav footprint
- missing room sockets
- missing required anchors
- spawn table has no valid encounter
- map profile has no resource set
- map profile references nonexistent spawn table

Build validation:

- compiled DB contains all referenced ids
- compiled map contains required anchors
- compiled map encounter placements resolve to DB encounters
- quest target encounter primary monster matches quest target monster
- scene/bootstrap references compiled assets
- Runtime/Core/UI Runtime do not reference Editor-only code

## Practical Editor UX Rules

- Use Project Browser assets as the source of truth.
- Use Inspector for editing one object deeply.
- Use EditorWindow for browsing, generation, validation, and build.
- Use Scene View for spatial authoring.
- Use object references where Unity assets matter.
- Use ids only in compiled/runtime-safe data.
- Never make designers type prefab paths.
- Never make runtime depend on editor-only assets.
- Preserve Undo/Redo for editor operations.

## Current Implementation Checkpoints

- [x] `editor_tool_content_pipeline_plan.md` marks current DB tabs as bootstrap
  editors and browser/build/validation bridges, not final editors.
- [x] Authoring asset schema exists for monsters, encounters, spawn tables,
  skills, NPCs, vendors, quests, map profiles, resource sets, room chunks,
  landmark rooms, and generation weight profiles.
- [x] `ContentDatabaseWindow` has an authoring asset discovery, validation,
  usage preview, and build/export bridge while preserving bootstrap DB tabs.
- [x] `GeneratorWorkbenchWindow` can select `MapProfileAsset`, pass seed/floor/
  difficulty, preview generated graph and placements, validate authoring assets,
  save `CompiledMapAsset`, and build `RuntimeMapGenerationBundle`.
- [x] Runtime/Core/UI Runtime forbidden Editor-reference scan is part of the
  validation gate.
- [x] Chapter 1 and Chapter 2 batch validators pass for the automated pipeline.

## Immediate Next Work

1. Complete manual Unity Play Mode verification for the Phase 6 three-quest
   sequence.
2. Complete the Phase 8 Game view checklist.
3. Only after manual verification, update the related `[!]` items in
   `editor_tool_content_pipeline_plan.md` and `p1_playtest_checklist.md`.
4. Keep the DB-first runtime path and emergency fallbacks until replacement is
   proven by both automated validation and manual Play Mode checks.

## Non-Goals

- Do not build an in-game runtime editor as the primary content workflow.
- Do not force designers to edit every content field through table rows.
- Do not store heavy Unity authoring references directly in Runtime/Core data
  unless a build step proves they are runtime-safe.
- Do not bind monsters to maps as ownership. Use spawn tables, filters, and
  placement overrides.

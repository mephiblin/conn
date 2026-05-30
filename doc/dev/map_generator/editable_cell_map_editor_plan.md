# Editable Cell Map Editor Plan

Date: 2026-05-30
Status: target architecture and implementation roadmap for a new editable
cell-map authoring pipeline. The current preview generator remains as a legacy
debug/reference path, not the foundation of the final editor.

## Purpose

The target is not a simple room-node preview. The target is a dungeon editor that
can produce complex, hand-correctable dungeon structures:

- multi-exit rooms and hubs
- short dead-end passages
- corridors with bends and width variation
- stairs, slopes, and height transitions
- tileable floors and walls
- registered map objects such as chests, barrels, torches, blockers, spawn hints,
  doors, and decor
- generated drafts that designers can paint, revise, validate, and bake

The current generator is useful as a prototype, but it is still mostly a graph
preview. The next editor direction must make the cell grid the editable source
of truth.

```text
Current:
  Generate graph
  -> serialize PreviewRooms / PreviewEdges / PreviewPlacements
  -> draw temporary cubes/primitives in Scene View

Target:
  Generate editable draft asset
  -> edit cells, layers, objects, sockets, rooms, and zones
  -> validate pathing and references
  -> build preview mesh / runtime map / optional fixed compiled map
```

## Build-New Decision

The final editor should be built as a new pipeline instead of continuing to
reshape the current preview workflow.

This does not mean deleting the existing code immediately. It means the existing
`MapGeneratorWorkspace`, `GeneratedMapDraft`, `PreviewRooms`,
`PreviewEdges`, and primitive scene preview path should be treated as legacy
debug tooling while the production editor is built around
`EditableMapDraftAsset`.

### Why A New Pipeline Is Better

The current structure is mismatched with the target:

- it stores graph snapshots, not editable cell maps
- it draws disposable scene objects, not source data
- it treats cells and objects as preview details, not authoring layers
- it has no true tile/material palette workflow
- it cannot paint, stamp, erase, validate, and re-bake a map draft
- its generator is still mostly a linear path plus short side branches

Trying to evolve this path directly would keep mixing three different concerns:

```text
procedural generation
editable authoring data
temporary scene preview
```

The final editor needs these concerns separated from the start.

### Migration Policy

Use the existing implementation only as input and reference:

- keep it available for comparison and regression checks
- reuse runtime-safe data types when they fit
- reuse validation ideas where they fit
- allow it to generate a first-pass draft only through an adapter
- do not keep adding final-editor responsibilities to `MapGeneratorWorkspace`

The new path becomes:

```text
EditableMapDraftAsset
  -> EditableMapDraftEditor
  -> EditableMapPreviewMeshBuilder
  -> EditableMapValidationService
  -> EditableMapBakeService
```

The old path remains:

```text
MapGeneratorWorkspace
  -> debug graph snapshot
  -> primitive/cell preview
  -> optional compiled map debug save
```

## Non-Goals

This plan does not try to solve every map feature at once. In particular, the
first implementation should not attempt:

- final art-quality tiles
- navmesh baking
- procedural room dressing at production quality
- full runtime dungeon rendering replacement
- multiplayer or save-game integration
- a general-purpose terrain editor unrelated to dungeons
- immediate deletion of the old map generator/debug preview

The first objective is narrower: make generated maps editable as saved cell data
and rebuild a reliable preview from that data.

## Current Gap

The project now has useful foundations:

- `RoomChunkCell`
- `RoomChunkObjectPlacement`
- `RoomChunkLayoutKind`
- cell preview primitives for floor, wall, slope, stair, gap
- object preview primitives for chest, barrel, torch, spawn hint, blocker
- authoring validation for cells, objects, and chunk layout metadata

These are necessary, but they are not enough. The generated map is still driven
by a small room graph, and the scene preview is not the editable map data. A
designer cannot yet paint a tile, erase a wall, place a registered object, or
save those edits as the authoritative draft.

The important weakness is architectural: generated preview objects are
disposable scene state. They should become the output of a draft asset, not the
source of truth.

Because this weakness is structural, the plan should avoid invasive refactors of
the current preview classes. New files and new asset types are preferable until
the replacement path is proven.

## Core Decision

Introduce an editable map draft asset as the final editor's source of truth.

```text
EditableMapDraftAsset
  Identity
    id
    source profile id
    seed
    version

  Dimensions
    width
    height
    cell size

  Layers
    terrain cells
    height cells
    material cells
    object placements
    spawn placements
    room ids
    zone ids
    socket/door data

  Build state
    validation report summary
    last generated map id
    dirty flags
```

This asset should live in `Assets/Conn/Authoring/Maps` or a child folder such as
`Assets/Conn/Authoring/Maps/Drafts`. It is an editor-time source asset, not the
runtime format.

## Ownership Boundaries

The editor must keep these responsibilities separate.

| Layer | Owns | Must not own |
| --- | --- | --- |
| `Conn.Core.Maps` | runtime-safe ids, cells, graph, compiled records | Unity objects, editor types |
| `Conn.Authoring.Maps` | ScriptableObject source assets and Unity references | generated scene preview objects |
| `Conn.Editor.Maps` | inspectors, tools, validation, preview mesh build, bake commands | runtime-only gameplay state |
| Runtime map loader | baked map consumption | editor asset mutation |

The same rule applies throughout the plan:

```text
Authoring assets may reference Unity assets.
Runtime records may only reference stable ids and primitive data.
Preview scene objects may be deleted at any time.
```

For the new pipeline, avoid direct dependencies on `MapGeneratorWorkspace`.
`MapGeneratorWorkspace` may call into the new services later, but the new
services should not require a workspace scene object.

## Coordinate And Layer Rules

Use one consistent coordinate system:

```text
cell x -> Unity local +X
cell y -> Unity local +Z
cell height -> Unity local +Y
```

Cell coordinates are integer grid addresses. The preview builder converts them
to Unity units using `cellSize` and `heightStep`.

```text
world/local position:
  x = cell.x * cellSize
  y = cell.height * heightStep
  z = cell.y * cellSize
```

Layer order:

```text
terrain layer   -> decides walkability and basic mesh type
height layer    -> decides elevation and stair/slope compatibility
material layer  -> decides floor/wall surface id
object layer    -> stamps interactive/decor objects onto cells
logic layer     -> room ids, zone ids, sockets, anchors, spawn intent
```

The editor may display these layers together, but the data should remain
separate enough that validation can reason about movement and references.

## Data Model

### Cell

Each cell should be addressable like a pixel.

```text
EditableMapCell
  x
  y
  roomId
  zoneId
  terrainType
  height
  materialId
  floorVariantId
  wallVariantId
  flags
```

`terrainType` should map to the existing ideas:

```text
floor
wall
slope
stair
gap
water/lava later
```

`height` must be part of the cell, not only a visual transform. Stairs, slopes,
pathfinding, combat visibility, and mesh generation all need the same height
source.

Suggested first C# shape:

```text
EditableMapDraftAsset : ScriptableObject
  string Id
  string SourceProfileId
  int Seed
  int Floor
  int Difficulty
  int Width
  int Height
  float CellSize
  float HeightStep
  EditableMapCell[] Cells
  EditableMapObjectPlacement[] Objects
  EditableMapRoom[] Rooms
  EditableMapZone[] Zones
  EditableMapSocket[] Sockets

EditableMapCell
  int X
  int Y
  string RoomId
  string ZoneId
  RoomChunkCellType Terrain
  int Height
  MapDirection Direction
  string MaterialId
  string FloorVariantId
  string WallVariantId
  int Flags
```

Store cells as a flat array for Unity serialization stability. Provide helper
methods for index conversion.

```text
index = y * width + x
```

Do not rely on multidimensional arrays for Unity serialization.

### Objects

Objects should be stamped onto the grid from a registered palette.

```text
EditableMapObjectPlacement
  id
  paletteObjectId
  kind
  x
  y
  height
  width
  depth
  direction
  blocksMovement
  runtimeReferenceId
```

The object palette should connect authoring references to runtime-safe ids.

```text
MapObjectPaletteAsset
  objects[]

MapObjectPaletteEntry
  id
  kind
  prefab
  previewMaterial
  footprintWidth
  footprintDepth
  blocksMovement
  runtimeReferenceId
```

Runtime data must only receive ids and primitive fields. Unity object references
stay in authoring assets.

Object placement should be stamp-based. A placement can cover more than one
cell, but it has one origin cell and a footprint.

```text
origin: x/y
footprint: width/depth
rotation: direction
```

The validation layer decides whether every footprint cell is legal.

### Rooms And Zones

The editor should separate physical grid cells from dungeon semantics.

```text
EditableMapRoom
  id
  role
  layoutKind
  bounds
  socketMask
  heightLevel
  zoneId

EditableMapZone
  id
  theme
  intendedDifficulty
  purpose
```

The generator can place rooms/zones first, then rasterize them into cells. The
designer can then fix the cells directly.

### Sockets And Doors

Sockets should exist both at room level and cell level.

```text
EditableMapSocket
  id
  roomId
  x
  y
  direction
  width
  targetRoomId
  lockedDoorKeyId
```

Room-level `SocketMask` is useful for graph/chunk selection. Cell-level sockets
are necessary for validation and preview because a door must connect to actual
walkable cells.

### Palettes

The editor needs palette assets instead of hardcoded preview types.

```text
MapTilePaletteAsset
  tiles[]

MapTilePaletteEntry
  id
  terrainType
  material
  defaultWalkable
  defaultHeightCost
  runtimeMaterialId

MapObjectPaletteAsset
  objects[]
```

`MapResourceSetAsset` can continue to own high-level theme resources. Palette
assets are the paint/stamp user-facing layer.

## Editor Workflow

The editor should use an inspector-first workflow with optional Scene View tools.
The draft asset is selected in the Project Browser, and its custom inspector
becomes the control surface.

The old workspace scene should not be the primary UI for the final editor. It
can remain a debug scene. The production workflow should start from assets:

```text
Project Browser
  -> select EditableMapDraftAsset
  -> inspect/edit/rebuild/validate/bake
```

### Generate Draft

Input:

```text
MapProfileAsset
seed
floor
difficulty
generation options
```

Output:

```text
EditableMapDraftAsset
```

The generated draft must include cells, room metadata, sockets, and initial
objects. It should not only store preview snapshots.

The generation command should have two modes:

```text
Create New Draft
  creates a new asset path and writes all generated data

Regenerate Selected Draft
  overwrites generated layers only after confirmation
  keeps designer-authored overrides only if explicit merge support exists
```

Until merge support exists, regeneration should be treated as destructive and
must require confirmation in UI.

### Paint And Stamp

The editor needs modes:

```text
Terrain brush
Height brush
Material brush
Object stamp
Room/socket tool
Validation overlay
```

Basic operations:

```text
paint floor/wall/gap
paint slope/stair with direction
raise/lower height
assign material id
stamp object
erase object
paint room id / zone id
mark socket/door
```

Minimum brush settings:

```text
brush size
terrain type
height delta / absolute height
direction
material id
object palette id
erase mode
```

Every edit must call `Undo.RecordObject(draft, "...")` before mutation and
`EditorUtility.SetDirty(draft)` after mutation.

### Preview Mesh

Preview should be generated from the editable draft.

```text
EditableMapDraftAsset
  -> MapPreviewMeshBuilder
      -> floor quads / submeshes
      -> wall meshes
      -> slope meshes
      -> stair meshes
      -> object prefabs or primitives
```

Temporary primitive previews can remain for debugging, but the main preview must
be mesh/tile based.

Preview output should be grouped under a preview root in the scene:

```text
Editable Map Preview Root
  Terrain Mesh
  Wall Mesh
  Slope Mesh
  Stair Mesh
  Object Preview Root
  Overlay Root
```

The preview root can be deleted and rebuilt. This must not lose draft edits.

### Validate

Validation should run before save/bake.

Required checks:

- all cells are inside bounds
- duplicate objects and ids are rejected
- objects fit on valid cells
- blocking objects do not cut required routes
- doors/sockets connect to walkable cells
- room entry sockets can reach required anchors
- stairs/slopes connect compatible height levels
- required start/quest/boss/exit anchors exist
- generated runtime data has no `UnityEngine.Object`, `UnityEditor`,
  `Conn.Editor`, or `Conn.Authoring` references

Validation reports should identify the smallest useful target:

```text
cell x,y
object id
room id
socket id
palette id
```

Avoid generic messages such as "map is invalid" without a location.

### Bake

The draft should bake into runtime-safe data.

```text
EditableMapDraftAsset
  -> validation
  -> CompiledMap / RuntimeMapGenerationBundle extension
  -> optional saved CompiledMapAsset
```

The runtime format should not depend on editor scene objects.

Bake should produce deterministic output from the draft. Rebuilding the same
draft without data changes should produce equivalent runtime data.

## Tool UI Shape

The target UI is closer to a tile/object editor than the current graph preview.

Recommended inspector sections:

```text
Draft
  id, profile, seed, dimensions

Brush
  mode, size, terrain, material, object, height, direction

Palette
  tile palette
  object palette

Actions
  rebuild preview
  validate
  bake
  clear preview

Overlays
  show room ids
  show sockets
  show blocked cells
  show validation errors
```

Scene View should eventually support click/drag painting. The first version can
use inspector buttons and simple cell coordinate inputs if Scene View tooling is
too large for the first pass.

## Implementation Phases

### Phase 1: Editable Draft Asset

Goal: create the new source asset without depending on the existing preview
workspace.

Tasks:

- add `EditableMapDraftAsset`
- add serializable cell, room, zone, object, and socket records
- add draft creation from current generated graph
- keep existing `PreviewRooms` path as a debug fallback
- add flat-array indexing helpers
- add explicit draft asset creation path under `Assets/Conn/Authoring/Maps/Drafts`
- add an adapter that can convert current `GeneratedMapDraft` into a basic
  `EditableMapDraftAsset`, but keep it isolated from the draft asset itself

Acceptance:

- generating a map can create a saved draft asset
- draft asset stores grid cells and object placements
- reopening Unity preserves draft data
- no runtime bundle type stores Unity object references
- draft asset APIs do not require `MapGeneratorWorkspace`

### Phase 2: Cell Map Preview Mesh

Goal: stop using room cubes as the primary visual.

Tasks:

- add `MapPreviewMeshBuilder`
- build floor quads from cells
- build walls from wall cells or cell edges
- build slope and stair meshes
- group floor triangles by material id where possible
- place object preview prefabs or fallback primitives
- put all preview objects under a disposable preview root
- keep `MapChunkCellPreviewBuilder` as debug-only fallback, not the final mesh
  builder

Acceptance:

- draft preview shows a tileable floor surface
- cells produce stable mesh positions
- object placements align to cell coordinates
- preview can be cleared and rebuilt without changing draft data
- preview mesh has UVs suitable for tiled materials
- preview can be rebuilt directly from `EditableMapDraftAsset`

### Phase 3: Inspector Painting Tools

Goal: allow designer edits.

Tasks:

- add draft custom inspector
- add brush mode enum
- add selected terrain/material/object fields
- add buttons for fill, clear, rebuild preview, validate, bake
- add simple Scene View cell picking
- record Undo for every draft mutation
- do not mutate generated scene preview objects as source data

Acceptance:

- designer can paint cells in the editor
- designer can stamp objects
- Undo/Redo works for draft edits
- validation reports precise cell/object ids

### Phase 4: Object Palette

Goal: replace hardcoded object preview kinds with registered content.

Tasks:

- add `MapObjectPaletteAsset`
- add `MapTilePaletteAsset`
- add entries for chest, barrel, torch, blocker, decor, spawn hint
- connect palette entries to prefabs/materials
- bake palette object ids to runtime-safe references

Acceptance:

- object placement can reference palette ids
- cell material can reference tile palette ids
- broken palette references are validation errors
- runtime bake contains ids, not Unity object references

### Phase 5: Generator To Draft

Goal: generator outputs editable maps, not only graphs.

Tasks:

- assign `RoomChunkLayoutKind` during graph generation
- choose chunks using role + layout kind + sockets + theme
- rasterize chosen chunks into the draft grid
- stamp chunk object placements into the draft
- create dead-end stubs and hub rooms intentionally

Acceptance:

- generated draft contains hubs, corridors, dead ends, and height transitions
- generated layout is editable before bake
- changing the seed changes the draft but preserves deterministic generation
- generated cell map is derived from selected chunks, not debug test cells
- the old `GeneratedMapDraft` path is only an adapter source, not the final
  generated artifact

### Phase 6: Connectivity And Height Validation

Goal: make complex maps safe to play.

Tasks:

- run BFS/A* over walkable cells
- treat blockers as non-walkable
- validate every door/socket reaches required anchors
- validate stair/slope height transitions
- validate start-to-quest-to-boss-to-exit route

Acceptance:

- invalid maps fail before bake
- validation messages name the blocking cell/object/room
- a map cannot bake if required routes are broken
- start-to-exit route remains valid after object stamping

### Phase 7: Runtime Bake

Goal: make the edited draft playable.

Tasks:

- extend `CompiledMap` or add a runtime cell-map payload
- bake cells, objects, anchors, rooms, and encounter placements
- ensure runtime data is object-reference-free
- load the compiled draft in Dungeon runtime

Acceptance:

- a hand-edited draft can be baked
- runtime can load the baked map
- field monsters and interactions spawn at baked positions
- runtime bake is deterministic for unchanged draft data

## Definition Of Done

The first usable version is done when all of these are true:

- a generator button creates an `EditableMapDraftAsset`
- the draft stores cells and objects as serialized asset data
- the draft can rebuild a tileable mesh preview
- the user can edit at least terrain type, height, material id, and object stamps
- validation can reject disconnected required routes
- bake produces runtime-safe data with no Unity object references
- deleting preview scene objects does not delete map data
- the workflow can be used without opening the legacy `MapGenerator` scene

The final production version is done when:

- generated maps routinely include hubs, corridors, dead ends, and height
  transitions
- designers can correct generated maps without touching code
- palette assets drive visual/object choices
- runtime can load baked edited drafts
- validation failures are precise enough to fix without guessing
- `MapGeneratorWorkspace` is optional debug tooling, not required production UI

## Risks

### Risk: Editing Scene Objects Instead Of Source Data

Scene objects are temporary. The source must be the draft asset. Preview objects
can be deleted and rebuilt at any time.

### Risk: Too Many GameObjects

Cell-per-GameObject is acceptable for debug only. The main preview should combine
floor/wall geometry into meshes by chunk or draft region.

### Risk: Runtime Data Pollution

Authoring assets may reference prefabs and materials. Runtime maps must store
stable ids and baked primitive fields.

### Risk: Generator Complexity Before Editor Control

More random generation will not solve authoring quality if designers cannot
paint or fix the result. Editable draft tooling should come before deeper
procedural complexity.

### Risk: Ambiguous Source Of Truth

If both generated graph snapshots and editable cell drafts can be modified
independently, data will drift. The draft asset must become the authoring source
after generation. Graph snapshots are debug context only.

Mitigation: once a draft is created, all edits happen on the draft. Regeneration
must either create a new draft or explicitly overwrite generated layers after
confirmation.

### Risk: Tile Painting Without Gameplay Semantics

Painting visual tiles is not enough. Terrain type, height, object blockers,
sockets, anchors, and spawn intent must remain part of the same validation model.

## Immediate Next Work

Start with Phase 1 and Phase 2.

Concrete first task:

```text
Add EditableMapDraftAsset with a flat cell array and object placement array.
Add a builder that converts the current generated room graph into a basic draft.
Add a preview builder that renders draft floor cells as tileable quads.
```

This gives the project the key missing capability: generated maps become
editable source data instead of disposable previews.

Recommended first-file scope:

```text
Assets/Conn/Authoring/Maps/EditableMapDraftAsset.cs
Assets/Conn/Editor/Maps/EditableMapDraftBuilder.cs
Assets/Conn/Editor/Maps/EditableMapPreviewMeshBuilder.cs
Assets/Conn/Editor/Maps/EditableMapDraftEditor.cs
Assets/Conn/Tests/EditMode/MapGenerationTests.cs
```

Do not remove the existing `MapGeneratorWorkspace` preview until the draft
preview can cover the same debugging use cases.

Recommended rule for the first implementation:

```text
Create new classes.
Do not expand MapGeneratorWorkspace except for an optional "Create Draft From
Current Generation" bridge button after the draft pipeline works.
```

## Implementation Checklist

Use this section as the working checklist. Keep unchecked items as `[ ]`, mark
completed work as `[x]`, and prefer adding a short note or file path when an item
is partially complete.

### Phase 0: Baseline And Guardrails

- [x] Confirm `MapGeneratorWorkspace` is treated as legacy debug/reference UI.
- [x] Confirm new classes do not require a workspace scene object.
- [x] Confirm runtime-facing records contain no `UnityEngine.Object`,
  `UnityEditor`, `Conn.Editor`, or `Conn.Authoring` references.
- [x] Keep existing map generator tests green before adding the new pipeline.
- [x] Add or update docs when source-of-truth decisions change.

### Phase 1: Editable Draft Asset

- [x] Create `Assets/Conn/Authoring/Maps/EditableMapDraftAsset.cs`.
- [x] Define `EditableMapCell`.
- [x] Define `EditableMapObjectPlacement`.
- [x] Define `EditableMapRoom`.
- [x] Define `EditableMapZone`.
- [x] Define `EditableMapSocket`.
- [x] Store cells in a flat serialized array, not a multidimensional array.
- [x] Add helper methods for `index = y * width + x`.
- [x] Add safe bounds checks for cell lookup and mutation.
- [x] Add draft identity fields: id, source profile id, seed, floor, difficulty.
- [x] Add draft dimensions: width, height, cell size, height step.
- [x] Add draft asset creation path under
  `Assets/Conn/Authoring/Maps/Drafts`.
- [x] Add a creation command or builder method for a blank draft.
- [x] Add an adapter that can convert current `GeneratedMapDraft` into a basic
  editable draft.
- [x] Ensure the draft can be saved, closed, reopened, and preserve data.
- [x] Add serialization tests for cells, rooms, sockets, and object placements.

### Phase 2: Cell Map Preview Mesh

- [x] Create `Assets/Conn/Editor/Maps/EditableMapPreviewMeshBuilder.cs`.
- [x] Build floor quads from draft cells.
- [x] Add tiled UVs for floor quads.
- [x] Build wall geometry from wall cells or wall edges.
- [x] Build slope geometry from slope cells and direction.
- [x] Build stair geometry from stair cells and direction.
- [x] Group triangles by material id or palette id where practical.
- [x] Create a disposable preview root.
- [x] Put terrain, wall, slope, stair, object, and overlay outputs under the
  preview root.
- [x] Add clear/rebuild preview commands.
- [x] Ensure deleting preview objects does not mutate the draft asset.
- [x] Keep `MapChunkCellPreviewBuilder` as debug fallback only.
- [x] Add editor test or validation utility that builds preview without
  throwing for a simple draft.

### Phase 3: Inspector Painting Tools

- [x] Create `Assets/Conn/Editor/Maps/EditableMapDraftEditor.cs`.
- [x] Add brush mode enum: terrain, height, material, object, room/socket,
  validation overlay.
- [x] Add selected terrain type field.
- [x] Add selected material or tile palette id field.
- [x] Add selected object palette id field.
- [x] Add brush size field.
- [x] Add direction field for slopes, stairs, sockets, and oriented objects.
- [x] Add absolute height and height delta controls.
- [x] Add erase mode.
- [x] Add fill/clear operations.
- [x] Add cell coordinate input for first-pass editing if Scene View picking is
  not ready.
- [x] Add Scene View cell picking.
- [x] Add click/drag painting.
- [x] Call `Undo.RecordObject` before every draft mutation.
- [x] Call `EditorUtility.SetDirty` after every draft mutation.
- [x] Confirm Undo/Redo restores cell and object changes correctly.
- [x] Add validation overlay display for failed cells/objects/sockets.

### Phase 4: Tile And Object Palettes

- [x] Create `MapTilePaletteAsset`.
- [x] Define `MapTilePaletteEntry`.
- [x] Add tile entry id.
- [x] Add terrain type.
- [x] Add editor material reference.
- [x] Add runtime material id.
- [x] Add default walkable flag.
- [x] Add default height cost or movement cost.
- [x] Create `MapObjectPaletteAsset`.
- [x] Define `MapObjectPaletteEntry`.
- [x] Add object entry id.
- [x] Add object kind.
- [x] Add prefab reference.
- [x] Add preview material reference.
- [x] Add footprint width/depth.
- [x] Add blocks movement flag.
- [x] Add runtime reference id.
- [x] Validate duplicate palette ids.
- [x] Validate broken prefab/material references.
- [x] Validate draft cells reference existing tile ids.
- [x] Validate draft objects reference existing object ids.
- [x] Validate object footprints do not overlap other object footprints.
  - 2026-05-30: generated object placement now avoids occupied door/object
    cells, and validation rejects overlapping footprints.
- [x] Ensure runtime bake uses ids, not Unity references.

### Phase 5: Generator To Draft

- [x] Add `EditableMapDraftBuilder`.
- [x] Generate rooms/zones before rasterizing cells.
- [x] Assign `RoomChunkLayoutKind` during generation.
- [x] Generate hub nodes intentionally.
- [x] Generate corridor nodes intentionally.
- [x] Generate short dead-end stubs intentionally.
  - 2026-05-30: generated dead-end stubs are registered as room metadata with
    bidirectional sockets and validated cell ownership.
- [x] Generate height-transition nodes intentionally.
- [x] Choose chunks by role, layout kind, sockets, theme, and size.
- [x] Rasterize selected chunk cells into the draft grid.
- [x] Stamp selected chunk object placements into the draft.
- [x] Create cell-level sockets from room/chunk sockets.
- [x] Preserve deterministic output for the same profile and seed.
- [x] Treat old `GeneratedMapDraft` only as adapter input.
- [x] Add tests for deterministic draft generation.
- [x] Add tests that generated drafts include requested layout kinds when the
  profile requires them.

### Phase 6: Connectivity And Height Validation

- [x] Create `EditableMapValidationService`.
- [x] Build a walkability map from terrain, height, blockers, and sockets.
- [x] Treat wall/gap cells as non-walkable.
- [x] Treat blocking objects as non-walkable across their footprint.
- [x] Validate all sockets touch walkable cells.
- [x] Validate room entry sockets can reach required anchors.
- [x] Validate start-to-quest route.
- [x] Validate quest-to-boss route.
- [x] Validate boss-to-exit route.
- [x] Validate optional treasure/dead-end routes do not break required routes.
- [x] Validate slope direction and height delta.
- [x] Validate stair direction and height delta.
- [x] Confirm generated slope/stair cells are enclosed by authored wall cells
  instead of bordering raw gap cells.
- [x] Report validation failures by cell, object id, room id, socket id, or
  palette id.
- [x] Block bake when required validation errors exist.
- [x] Add tests for disconnected routes.
- [x] Add tests for blocking object route cuts.
- [x] Add tests for invalid slope/stair height connections.

### Phase 7: Runtime Bake

- [x] Create `EditableMapBakeService`.
- [x] Decide whether to extend `CompiledMap` or add a dedicated runtime cell-map
  payload.
- [x] Bake cells to runtime-safe records.
- [x] Bake objects to runtime-safe records.
- [x] Bake rooms and zones.
- [x] Bake sockets and doors.
- [x] Bake anchors and encounter placements.
  - 2026-05-30: required anchor placements resolve to walkable cells inside
    the room instead of blindly using rectangular room centers.
- [x] Confirm no baked type stores Unity object or editor references.
- [x] Save optional `CompiledMapAsset` from an edited draft.
- [x] Load baked draft data in Dungeon runtime.
- [x] Spawn monsters/interactions from baked object/placement data.
- [x] Add deterministic bake test for unchanged draft data.
- [x] Add runtime load smoke test.

### Documentation And Review Checklist

- [x] Update this checklist after completing each phase.
- [x] Update `doc/dev/map_generator/README.md` when workflow changes.
- [x] Document any intentional incompatibility with `MapGeneratorWorkspace`.
- [x] Add manual Unity test steps for new editor tools.
- [x] Add screenshots or saved sample assets when the first usable version
  exists.
- [x] Before commit, run `git diff --check`.
- [x] Before commit, run Unity compile or note why it could not be run.

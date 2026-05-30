# New Map Generator Plan

Date: 2026-05-30
Status: planning document for replacing the quarantined legacy map generator.

## Source Review

Primary reference:

- Video: [Mora MapGen | Rooms & Corridors Dungeon Layout Generator](https://www.youtube.com/watch?v=5qo8odfwXk4)
- Uploader: Mora Games Dev
- Upload date: 2026-02-24
- Duration: 1:25

Video description states that the tool generates unlimited room/corridor maps,
uses Wave-Function-Collapse and post-processing, and gives control over style,
map size, room quantity, corridor quantity, and related generation settings.

No captions were available. The notes below are based only on visible video text,
frames, and the public video description.

## Observed Product Shape

The video presents a production-oriented Unity map generator with these visible
behaviors:

- Generates layouts made of rooms and corridors.
- Keeps all generated areas reachable for pathfinding and graph traversal.
- Can generate in editor and at runtime.
- Separates abstract layout from visual skin, allowing the same layout to be
  rendered as sci-fi, dungeon, grid crawler, or other styles.
- Supports third-person, first-person, and grid-based dungeon crawler use cases.
- Exposes customization settings such as removing small rooms, splitting large
  rooms, consolidating paths, using direct routes, filling empty space, and
  reducing dead ends.
- Provides a chunks mode for custom room shapes.
- Uses WFC with post-processing to reach a custom style.
- Includes prop placement controls with distribution modes, offsets, channels,
  and related placement constraints.

## Direction For This Project

Build a new map generator instead of extending the quarantined legacy workflow.
The new generator should be profile-driven, visual, deterministic, and designed
for producing many maps from reusable authored content.

The generator should not start from the old graph-first workspace. The legacy
implementation can remain as reference material only.

## Product Goals

- Designers can define map style, size, room/corridor quantity, connectivity,
  and post-processing rules through authored assets.
- Designers can author reusable room, corridor, door, connector, and prop
  templates.
- The generator produces connected room/corridor layouts with deterministic seed
  replay.
- The same abstract layout can be skinned with different visual template sets.
- The output can be previewed in editor, accepted as an editable draft, and baked
  into runtime-safe map data.
- Failure reports explain the missing rule, connector, template, or placement
  constraint that blocked generation.

## Non-Goals

- Do not revive `MapGeneratorWorkspace` as the main workflow.
- Do not use hidden hardcoded sample profiles.
- Do not make raw serialized arrays the primary authoring experience.
- Do not couple generation rules to one chapter, scene, biome, or sample map.
- Do not require the user to edit node/wire graphs to make normal production
  maps.
- Do not build runtime generation first. Runtime support should reuse the same
  validated data contracts after the editor workflow is stable.

## Proposed Folder Boundary

Use a new namespace and folder boundary so the replacement does not inherit
legacy assumptions accidentally.

```text
Assets/Conn/Core/MapGenV2/
  Runtime-safe data contracts, solver, validation, deterministic generation.

Assets/Conn/Authoring/MapGenV2/
  ScriptableObject assets for profiles, templates, style sets, rule sets, and
  editable generated drafts.

Assets/Conn/Editor/MapGenV2/
  Custom inspectors, generator window, visual preview, diagnostics, and asset
  creation helpers.

doc/dev/map_generator/
  New design and implementation notes.

doc/dev/map_generator/legacy/
  Quarantined reference only.
```

## Core Asset Model

### `MapGenProfileAsset`

Top-level generation profile selected by the designer.

Required fields:

- `profileId`
- `displayName`
- `mapSize`
- `seedPolicy`
- `styleSet`
- `layoutRules`
- `roomQuantityRules`
- `corridorQuantityRules`
- `postProcessRules`
- `propPlacementRules`
- `templatePools`

### `MapGenStyleSetAsset`

Defines the visual skin and compatible template pools.

Required fields:

- `styleId`
- `floorPalette`
- `wallPalette`
- `doorPalette`
- `roomTemplatePools`
- `corridorTemplatePools`
- `propSets`
- `lightingPreset`

The style set must be swappable without changing the abstract room/corridor
layout.

### `MapGenRoomTemplateAsset`

Reusable authored room shape.

Required fields:

- `templateId`
- `footprint`
- `roomCategory`
- `sizeClass`
- `connectors`
- `blockedCells`
- `floorCells`
- `wallCells`
- `doorHints`
- `propChannels`
- `weight`

Room templates must support both simple rectangles and custom chunk shapes.

### `MapGenCorridorTemplateAsset`

Reusable authored corridor shape.

Required fields:

- `templateId`
- `footprint`
- `corridorKind`
- `connectors`
- `width`
- `turnKind`
- `lengthRange`
- `propChannels`
- `weight`

Corridors are first-class templates, not leftover space between rooms.

### `MapGenConnector`

Connection contract between rooms, corridors, and doors.

Required fields:

- `side`
- `localCell`
- `socketId`
- `socketKind`
- `width`
- `required`
- `tags`

Connectors drive WFC compatibility and later door/wall realization.

### `MapGenRuleSetAsset`

Reusable generation rules that can be shared across profiles.

Required fields:

- `requiredRoomCategories`
- `optionalRoomCategories`
- `minRooms`
- `maxRooms`
- `minCorridorCells`
- `maxCorridorCells`
- `loopRate`
- `deadEndPolicy`
- `directRoutePolicy`
- `largeRoomSplitPolicy`
- `smallRoomRemovalPolicy`
- `connectivityPolicy`

## Generator Pipeline

```text
MapGenProfileAsset
  -> Validate profile and template pools
  -> Build abstract generation domain
  -> Reserve required landmarks
  -> Solve room/corridor layout with WFC-style constraints
  -> Enforce reachability and graph traversal
  -> Apply post-processing
  -> Stamp visual templates
  -> Place props by channels and distribution rules
  -> Produce preview draft
  -> Accept editable draft
  -> Bake runtime map data
```

## Solver Design

### 1. Domain Initialization

Create a grid of layout cells. Each cell starts with candidate states:

- empty
- room template candidate
- corridor template candidate
- reserved connector
- blocked/out-of-bounds

Candidate states are filtered by map size, profile style, room count range,
corridor count range, template footprint, and required room categories.

### 2. Required Landmark Reservation

Before collapse, reserve generation obligations:

- start room
- exit room
- required quest/key rooms
- required boss/encounter rooms
- required transition rooms if the profile asks for them

Reservation does not hardcode positions. It constrains candidate regions and
distance requirements.

### 3. WFC-Style Collapse

Collapse the lowest-entropy cell or region first. Entropy should consider:

- number of legal templates
- required category pressure
- connector compatibility
- path distance constraints
- density targets
- style/pool weight

After collapse, propagate connector and footprint constraints to neighbors.

### 4. Connectivity Enforcement

Every non-empty navigable area must be reachable. The solver should maintain an
abstract connectivity graph during collapse and reject states that create
unrecoverable disconnected islands.

Connectivity checks:

- all required rooms have a path from start
- all generated rooms connect to the traversal graph
- corridors do not terminate illegally unless dead ends are allowed
- direct route policy is respected when enabled

### 5. Post-Processing

Post-processing should be explicit and configurable, matching the visible
capabilities from the reference video.

Supported passes:

- remove small rooms
- split large rooms
- consolidate paths
- add direct routes
- fill selected empty space
- reduce dead ends
- add loops based on `loopRate`
- normalize route lengths
- widen or clean corridors
- merge compatible adjacent rooms

Each pass must report what it changed.

### 6. Stamping

Convert the abstract solution into an editable cell draft.

Stamping responsibilities:

- place room templates
- place corridor templates
- carve connector doors
- resolve walls between adjacent regions
- prevent footprint overlap
- preserve room/corridor identity for later editing
- attach prop placement channels

## Prop Placement Plan

Prop placement should run after layout stamping so it can use final floor, wall,
door, and region information.

Required features:

- channel-based placement such as floor, wall, corner, room center, corridor
  edge, entrance, objective, and blocker.
- distribution modes such as random, weighted random, grid, perimeter, and
  marker-based placement.
- offsets and rotation rules.
- minimum spacing.
- room/corridor category filters.
- seed-stable placement.

Prop placement must never break path connectivity unless the prop rule explicitly
marks a navigable blocker and the validator approves the remaining path.

## Editor Workflow

Create one new editor entry point for MapGenV2.

Main panels:

- Profile selection and validation.
- Map size, seed, room quantity, corridor quantity, and post-process controls.
- Template pool summary with missing-template warnings.
- Visual 2D layout preview.
- Optional 3D scene preview using the selected style set.
- Diagnostics panel for contradictions, retries, connectivity, and template
  usage.
- Generate, randomize seed, accept draft, validate draft, and bake buttons.

Debug overlays:

- room/corridor categories
- connector sockets
- reachability graph
- WFC entropy
- failed candidate reasons
- post-processing diffs
- prop channels

## Validation Requirements

Profile validation must run before generation.

Validation checks:

- profile has a style set.
- required room categories have at least one legal template.
- corridor pools can connect the required room templates.
- connector sockets have compatible counterparts.
- map size can fit the requested room and corridor quantities.
- post-process rules do not contradict quantity or connectivity rules.
- prop rules reference valid channels.
- all runtime-baked data is free of editor-only references.

Generation failure must return a structured report, not only a log string.

Report fields:

- `phase`
- `seed`
- `retryIndex`
- `failedRule`
- `failedTemplate`
- `failedConnector`
- `cell`
- `message`
- `suggestedFix`

## Runtime Data Boundary

Runtime should consume baked data only.

Runtime-safe output:

- map bounds
- cells
- regions
- doors
- traversal graph
- spawn markers
- prop instances
- objective markers
- metadata required by combat/content systems

Editor-only output:

- preview meshes
- authoring assets
- diagnostic overlays
- generator step logs
- raw template references

## MVP Milestones

### Milestone 1: Data Contracts

- Add `MapGenV2` runtime namespace and authoring assets.
- Add profile/style/template/rules/connectors.
- Add validators with actionable error reports.
- No generation yet.

Acceptance:

- A profile can be authored and validated.
- Missing style, missing templates, and incompatible connectors are reported.

### Milestone 2: Abstract Layout Solver

- Generate connected abstract room/corridor layouts.
- Support map size, seed, room count, corridor density, required rooms, and
  connector compatibility.
- Add deterministic retry sequence.

Acceptance:

- Same profile and seed produce the same layout signature.
- Different seeds produce different connected layouts.
- Required rooms are present and reachable.

### Milestone 3: Template Stamping

- Stamp room and corridor templates into an editable cell draft.
- Preserve room/corridor identity and connector metadata.
- Validate overlap, walls, doors, and reachability.

Acceptance:

- Preview shows actual cell-map output, not only graph boxes.
- Doors and corridors connect legal sockets.

### Milestone 4: Post-Processing

- Implement explicit post-processing passes.
- Add per-pass reporting.
- Keep all navigable output connected.

Acceptance:

- Designers can toggle remove-small-rooms, split-large-rooms, consolidate-paths,
  direct-routes, fill-empty-space, and reduce-dead-ends.
- The diagnostics panel shows post-process changes.

### Milestone 5: Visual Editor

- Build the new MapGenV2 editor window.
- Add profile validation, preview generation, seed replay, diagnostics, accept
  draft, and bake.

Acceptance:

- A designer can create a new profile, assign template pools, generate a map,
  inspect problems, accept a draft, and bake without using legacy UI.

### Milestone 6: Prop Placement

- Add prop channels and deterministic prop placement.
- Support distribution modes, offsets, category filters, and validation.

Acceptance:

- Props appear in legal channels without blocking required traversal.
- Same seed produces the same prop layout.

### Milestone 7: Runtime Adapter

- Bake generated drafts into runtime-safe map data.
- Add a narrow adapter to existing runtime map consumers if needed.

Acceptance:

- Runtime code can load the baked map without editor references.
- Existing combat/content systems can query traversal, regions, spawn markers,
  and objective markers.

## Test Plan

Automated tests:

- missing style fails validation.
- missing required room template fails validation.
- incompatible connectors fail validation.
- impossible room quantity fails validation.
- same seed/profile produces identical abstract layout signature.
- different seed produces different layout signature.
- every generated room is reachable.
- required rooms are present.
- post-processing preserves connectivity.
- stamping rejects overlap.
- baked runtime map has no editor-only references.
- prop placement is deterministic.

Manual Unity checks:

- create a profile from scratch.
- assign a style set.
- author simple rectangular room/corridor templates.
- validate profile.
- generate several seeds.
- compare same layout under two style sets.
- enable/disable post-process passes and inspect the diff overlay.
- accept a draft.
- bake runtime data.
- load baked data in a runtime scene.

## Implementation Rules

- Keep legacy code quarantined.
- Create new `MapGenV2` namespaces instead of expanding old map generator
  namespaces.
- Commit after each milestone or meaningful subphase.
- Keep documentation updated with each implemented milestone.
- Do not claim feature completion until validation and seed determinism tests
  exist for that feature.


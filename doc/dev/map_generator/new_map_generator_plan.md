# New Map Generator Plan

Date: 2026-05-30
Status: planning document for replacing the quarantined legacy map generator.

## Source Review

Primary reference:

- Video: [Mora MapGen | Rooms & Corridors Dungeon Layout Generator](https://www.youtube.com/watch?v=5qo8odfwXk4)
- Uploader: Mora Games Dev
- Upload date: 2026-02-24
- Duration: 1:25

Related asset page:

- Asset: [Procedural Rooms & Corridors Dungeon Generator (WFC) | MoraMapGen](https://assetstore.unity.com/packages/tools/level-design/procedural-rooms-corridors-dungeon-generator-wfc-moramapgen-289878)
- Publisher: BM Ben Mora
- Category: Tools / Level Design
- License type: Extension Asset
- Latest version: 1.0
- Latest release date: 2026-02-24
- Original Unity version: 2022.3.7
- File size: 149.2 MB
- Asset Store compatibility table: URP compatible for Unity 2022.3.7f1,
  Built-in and HDRP marked not compatible on the official page.

Video description states that the tool generates unlimited room/corridor maps,
uses Wave-Function-Collapse and post-processing, and gives control over style,
map size, room quantity, corridor quantity, and related generation settings.

No captions were available. The notes below are based only on visible video text,
frames, the public video description, and visible Asset Store/search-result
metadata.

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

## User Frame Analysis

The attached frame references clarify the intended workflow:

- The blue/red/black view is a mockup layout state, not the final generated
  mesh.
- Blue is the base grid/background domain.
- Red represents room/chunk masses.
- Black represents corridor/path/wall separation in the mockup layout.
- Gray blocks in the WFC/post-processing demonstration appear as blocked or
  excluded regions.
- The generator repeatedly changes the internal structure while still in mockup
  state.
- The user should inspect and adjust this mockup state first.
- Only after the user accepts a satisfactory mockup should the workflow advance
  to the real mesh/prefab layout state.
- Chunks mode shows that red room masses are backed by inspector-editable room
  shape assets.
- The room shape inspector is grid-based: dimensions such as 3x3, 3x5, and 4x4
  are visible, and each cell can contain a selected room-shape sprite/state or
  `None`.

This means the project workflow must be two-stage:

```text
Generate and iterate mockup layout
  -> user accepts mockup
  -> materialize selected style/prefabs/meshes
  -> place props
  -> bake runtime output
```

The mockup stage is the primary design loop. Mesh generation is a later
materialization step, not the first result.

## Observed Asset Positioning

The Asset Store page and indexed asset descriptions position MoraMapGen as a
tooling package, not an art pack.

Confirmed or indexed claims:

- It is for procedural room-and-corridor dungeon layouts.
- It uses a custom WFC system designed for room/corridor levels.
- It supports editor generation and runtime generation.
- Editor-generated maps can be baked into prefabs.
- The user supplies their own prefabs, meshes, materials, textures, and shaders.
- Demo content is URP-oriented, while the generator system is described by
  indexed text as render-pipeline independent.
- It supports graph traversal, grid-based pathfinding, and Unity AI Navigation
  through `NavMeshSurface`.
- It uses deterministic random generation.
- It includes procedural prop placement.
- It is aimed at roguelikes, dungeon crawlers, and games needing unlimited
  unique procedural levels.

Implications for this project:

- Treat the generator as an authoring/runtime system, not a bundled art content
  feature.
- Separate map layout, visual prefab skinning, navigation output, and prop
  placement.
- Keep deterministic RNG as a core data contract.
- Bake editor output into reusable runtime data or prefabs.
- Provide adapters for graph traversal, grid pathfinding, and Unity navigation
  instead of binding the generator to only one movement model.

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
- The output first appears as an editable mockup grid, can be iterated until the
  user accepts it, and only then becomes a styled mesh/prefab layout.
- Failure reports explain the missing rule, connector, template, or placement
  constraint that blocked generation.
- The generated output can provide navigation surfaces/data for graph traversal,
  grid pathfinding, and Unity AI Navigation adapters.

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
layout. It should reference project-owned prefabs and materials; the generator
must not depend on bundled production art.

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

### `MapGenRoomShapeAsset`

Inspector-editable mockup chunk shape used before mesh materialization.

Required fields:

- `shapeId`
- `dimensions`
- `cells`
- `occupiedCells`
- `connectorCells`
- `blockedCells`
- `previewSprite`
- `category`
- `weight`

This asset represents the red chunk mass visible in mockup view. It should be
edited as a small grid, not as a raw array. A room template may reference one or
more room shapes, and a style set later maps the accepted shape to real
prefabs/meshes.

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

### `MapGenMockupDraftAsset`

Accepted or in-progress mockup layout before real mesh generation.

Required fields:

- `profile`
- `seed`
- `gridSize`
- `cells`
- `roomMasses`
- `corridorPaths`
- `blockedRegions`
- `connectivityGraph`
- `postProcessHistory`
- `accepted`

This is the first saved output of generation. It is the artifact the user
iterates on before materialization.

## Generator Pipeline

```text
MapGenProfileAsset
  -> Validate profile and template pools
  -> Build mockup grid domain
  -> Reserve required landmarks
  -> Solve mockup room/corridor layout with WFC-style constraints
  -> Enforce reachability and graph traversal
  -> Apply mockup post-processing
  -> Produce mockup layout draft
  -> User iterates seed/rules/chunk shapes/post-process settings
  -> User accepts mockup draft
  -> Materialize accepted mockup into visual templates/prefabs/meshes
  -> Place props by channels and distribution rules
  -> Bake runtime map data and/or prefab output
```

## Solver Design

### 1. Domain Initialization

Create a grid of mockup layout cells. Each cell starts with candidate states:

- empty
- room shape candidate
- corridor path candidate
- reserved connector
- blocked/out-of-bounds

Candidate states are filtered by map size, profile style, room count range,
corridor count range, shape footprint, and required room categories.

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

- number of legal room shapes/corridor states
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

Convert the accepted mockup solution into a visual map.

Stamping responsibilities:

- place style-specific room prefabs/meshes from accepted red room masses
- place style-specific corridor prefabs/meshes from accepted black paths
- carve connector doors
- resolve walls between adjacent regions
- prevent footprint overlap
- preserve room/corridor identity for later editing
- attach prop placement channels

Stamping must not be required while the user is still searching for a good
layout. It runs after mockup acceptance.

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
- Mockup grid preview using blue/red/black/blocked-region states.
- Room shape grid editor for chunk dimensions and occupied cells.
- Optional materialized 3D scene preview after mockup acceptance.
- Diagnostics panel for contradictions, retries, connectivity, and template
  usage.
- Generate mockup, randomize seed, rerun post-process, accept mockup,
  materialize, validate, and bake buttons.

Debug overlays:

- room/corridor categories
- mockup cell state
- accepted vs unaccepted layout state
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
- required room categories have at least one legal mockup room shape.
- corridor pools can connect the required room templates.
- connector sockets have compatible counterparts.
- map size can fit the requested room and corridor quantities.
- room shape dimensions and occupied cells are valid for the grid.
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
- grid pathfinding data
- optional navigation surface build inputs
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
- Add profile/style/template/rules/connectors/mockup draft assets.
- Add grid-editable room shape assets.
- Add validators with actionable error reports.
- No generation yet.

Acceptance:

- A profile can be authored and validated.
- Missing style, missing room shapes, missing templates, and incompatible
  connectors are reported.

### Milestone 2: Mockup Layout Solver

- Generate connected blue/red/black mockup room/corridor layouts.
- Support map size, seed, room count, corridor density, required rooms, and
  connector compatibility.
- Add deterministic retry sequence.

Acceptance:

- Same profile and seed produce the same mockup layout signature.
- Different seeds produce different connected mockup layouts.
- Required rooms are present and reachable.

### Milestone 3: Mockup Iteration And Acceptance

- Add mockup draft save/load.
- Add seed reroll and post-process rerun without materializing meshes.
- Add user acceptance state.
- Preserve accepted mockup state for later materialization.

Acceptance:

- A designer can generate multiple mockup variants without creating meshes.
- A designer can accept one mockup and reopen the accepted draft.
- Unaccepted mockups cannot be baked as runtime maps.

### Milestone 4: Materialization

- Convert accepted red room masses into style-specific room prefabs/meshes.
- Convert accepted black corridor paths into style-specific corridor
  prefabs/meshes.
- Preserve room/corridor identity and connector metadata.
- Validate overlap, walls, doors, and reachability.

Acceptance:

- Materialization only runs after mockup acceptance.
- The materialized scene matches the accepted mockup structure.
- Doors and corridors connect legal sockets.

### Milestone 5: Post-Processing

- Implement explicit mockup post-processing passes.
- Add per-pass reporting.
- Keep all navigable output connected.

Acceptance:

- Designers can toggle remove-small-rooms, split-large-rooms, consolidate-paths,
  direct-routes, fill-empty-space, and reduce-dead-ends.
- The diagnostics panel shows post-process changes before materialization.

### Milestone 6: Visual Editor

- Build the new MapGenV2 editor window.
- Add profile validation, mockup generation, seed replay, diagnostics, room
  shape grid editing, mockup acceptance, materialization, and bake.

Acceptance:

- A designer can create a new profile, assign shape/template pools, generate
  mockups, inspect problems, accept a mockup, materialize it, and bake without
  using legacy UI.

### Milestone 7: Prop Placement

- Add prop channels and deterministic prop placement.
- Support distribution modes, offsets, category filters, and validation.

Acceptance:

- Props appear in legal channels without blocking required traversal.
- Same seed produces the same prop layout.

### Milestone 8: Runtime Adapter

- Bake accepted and materialized drafts into runtime-safe map data.
- Add a narrow adapter to existing runtime map consumers if needed.
- Add optional prefab baking for editor-generated output.
- Add navigation adapters for graph traversal, grid pathfinding, and Unity
  navigation build input.

Acceptance:

- Runtime code can load the baked map without editor references.
- Existing combat/content systems can query traversal, pathfinding, regions,
  spawn markers, and objective markers.
- Editor-generated output can be saved as reusable prefab-backed content when
  the profile requests it.

## Test Plan

Automated tests:

- missing style fails validation.
- missing required room shape fails validation.
- missing required room template fails validation.
- incompatible connectors fail validation.
- invalid room shape grid fails validation.
- impossible room quantity fails validation.
- same seed/profile produces identical mockup layout signature.
- different seed produces different mockup layout signature.
- every generated room is reachable.
- required rooms are present.
- post-processing preserves connectivity.
- materialization rejects overlap.
- unaccepted mockup cannot bake.
- baked runtime map has no editor-only references.
- prop placement is deterministic.

Manual Unity checks:

- create a profile from scratch.
- assign a style set.
- author simple grid-based room shapes.
- author simple room/corridor templates for materialization.
- validate profile.
- generate several mockup seeds.
- adjust room shape grid assets and regenerate mockups.
- accept one mockup.
- compare same layout under two style sets.
- enable/disable post-process passes and inspect the diff overlay.
- materialize the accepted mockup.
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

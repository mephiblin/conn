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
- `moduleSet`
- `roomTemplatePools`
- `corridorTemplatePools`
- `propSets`
- `lightingPreset`

The style set must be swappable without changing the abstract room/corridor
layout. It should reference project-owned prefabs and materials; the generator
must not depend on bundled production art.

### `MapGenModuleSetAsset`

Style-specific prefab module pools used when an accepted mockup is materialized.

Required module categories:

- `floorsA`
- `floorsB`
- `wallsStraight`
- `wallsCornerInside`
- `wallsCornerOutside`
- `exteriorCeilings`
- `interiorCeilings`
- `wholeDoors`
- `halfDoorFrames`
- `halfDoorPanels`
- `propCategories`
- `requiredUniqueProps`

Each category should contain weighted prefab entries:

```text
MapGenModuleEntry
  prefab
  weight
  rotationPolicy
  offset
  tags
```

This matches the observed module definition inspector: each structural category
has one or more prefab options and a weight. The materializer chooses a prefab
from the relevant category using deterministic weighted random selection.

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

Implementation decision:

- The room shape source of truth should be a `ScriptableObject`, not a hierarchy
  of child `GameObject` cells.
- The inspector grid edits serialized cell data in the `ScriptableObject`.
- Scene `GameObject`s may be created only as previews or materialized output.
- Child `GameObject` grids should not be used as the authoring data model,
  because they are harder to validate, diff, duplicate, test, and use at
  runtime.

Data shape:

```text
MapGenRoomShapeAsset
  dimensions: int2
  cells: RoomShapeCell[width * height]
```

Each `RoomShapeCell` should contain compact enum/flag data:

- `state`: none, occupied, connector, blocked.
- `socketKind`: none, door, corridor, wildcard.
- `socketId`: optional compatibility id.
- `tags`: optional authoring labels.

The inspector should draw this as a clickable grid. Clicking a cell changes the
serialized `RoomShapeCell` state. The preview sprite visible in the reference
frames can be generated from this data or drawn directly by the custom editor.
No per-cell child objects are required for editing.

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

## Room Shape Authoring Approach

The red mockup chunks should be authored as grid data assets and displayed
through a custom inspector.

Recommended structure:

```text
MapGenRoomShapeAsset
  width
  height
  cells[]
  preview cache

MapGenRoomShapeEditor
  draws clickable grid
  validates dimensions
  updates serialized cells
  rebuilds preview cache

MapGenMockupPreview
  reads accepted/generated layout data
  draws blue base grid, red room masses, black paths, gray blocked cells
```

Why this is preferable to child `GameObject` grids:

- The generator needs fast deterministic access to cell data without scene
  traversal.
- Validation can run in edit mode and tests without opening a scene.
- Git diffs and asset duplication are cleaner than nested object hierarchies.
- Runtime generation can use the same data without editor-only objects.
- Materialization can still create grouped `GameObject`s later, but only after
  the mockup is accepted.

Allowed use of `GameObject` groups:

- A generated mockup preview object may contain temporary child visuals for
  scene display.
- A materialized map may create one parent `GameObject` with room/corridor
  prefab children.
- These objects are outputs, not the source authoring model.

Inspector behavior:

- Dimensions control the editable grid size.
- Cell tools switch between none, occupied room mass, connector, and blocked.
- Drag painting should be supported for fast shape editing.
- Rotated/flipped variants should be previewed but stored either as generated
  variants or explicit user-approved variants.
- The inspector should show connector counts and invalid edge/socket warnings
  next to the grid.
- The room shape asset should expose a generated thumbnail matching the
  blue/red visual language used by mockup preview.

## Visual Module Authoring Approach

The attached module screenshots show a second authoring layer: the final visual
map is assembled from reusable prefab modules after the mockup layout is
accepted.

Technical approach:

- Artists can create modular pieces in Blender or Unity, such as floor pieces,
  straight walls, inside corners, outside corners, ceilings, door frames, door
  panels, and prop spots.
- Unity stores these pieces as prefabs.
- A `MapGenModuleSetAsset` groups those prefabs by structural category and
  assigns deterministic selection weights.
- The accepted mockup layout is scanned cell-by-cell to classify which module
  category is needed at each position.
- The materializer instantiates prefab modules under one generated parent
  `GameObject`, grouped by room/corridor/region for readability.

The module prefab hierarchy visible in the reference should be treated as output
and library content, not as the procedural layout source of truth. The source of
truth remains:

```text
Room shape assets
  -> accepted mockup draft
  -> module set weighted prefab pools
  -> materialized scene/prefab
```

Module classification rules:

- Mockup room/corridor floor cells become `floorsA` or `floorsB` based on region
  category, checker/variation rules, or style tags.
- Boundary edges between navigable and empty cells become `wallsStraight`.
- Concave navigable corners become `wallsCornerInside`.
- Convex exterior corners become `wallsCornerOutside`.
- Covered cells request `interiorCeilings` or `exteriorCeilings` depending on
  whether the cell belongs to an interior region or outer shell.
- Connector cells become door modules if the profile/style requests doors.
- Door openings can use `wholeDoors`, or split `halfDoorFrames` and
  `halfDoorPanels` if the style supports modular door assembly.
- Prop marker cells become prop category requests.

Prefab requirements:

- Every structural prefab must align to the generator grid size.
- Pivot should be consistent, preferably bottom-center or cell-origin based.
- Prefabs must declare occupied cell footprint if they cover more than one cell.
- Rotation must be legal only in directions allowed by the module entry.
- Mesh scale should be normalized so materialization can use grid coordinates
  without per-prefab correction.
- Collider/navmesh participation should be configured per prefab or through a
  module metadata component.

Materialized hierarchy:

```text
GeneratedMap_<profile>_<seed>
  MockupReference
  Floors
  Walls
  Ceilings
  Doors
  Props
  Navigation
```

This hierarchy is for inspection, editing, prefab baking, and runtime scene
loading. It should be reproducible from the accepted mockup plus module set.

## Implementation Contracts

This section defines the contracts required before code implementation starts.
Do not leave these choices implicit in the first implementation pass.

### Assembly Boundary

Use explicit assembly definitions so editor code cannot leak into runtime code.

```text
Conn.MapGenV2.Core
  Path: Assets/Conn/Core/MapGenV2/
  References: no UnityEditor references.

Conn.MapGenV2.Authoring
  Path: Assets/Conn/Authoring/MapGenV2/
  References: Conn.MapGenV2.Core.
  Contains ScriptableObject asset types only.

Conn.MapGenV2.Editor
  Path: Assets/Conn/Editor/MapGenV2/
  References: Conn.MapGenV2.Core, Conn.MapGenV2.Authoring, UnityEditor.
  Contains inspectors, windows, asset factories, preview drawing, and bake tools.
```

Runtime-safe generated/baked data must live in `Core` or another runtime-safe
folder. Editor-only preview objects, inspectors, and asset creation menus must
stay in `Editor`.

### Coordinate And Grid Contract

Use one canonical grid contract for mockup, materialization, validation, and
runtime bake.

- Logical grid coordinate: `MapGenGridCoord(int x, int y)`.
- Unity world projection: grid `x` maps to world `+X`; grid `y` maps to world
  `+Z`.
- World height: floor plane starts at `Y = 0`.
- Default cell size: `1.0f`, overridable by profile but fixed per generated map.
- Cell origin: bottom-left logical cell corner at world `(0, 0, 0)`.
- Cell center: `(x + 0.5f) * cellSize`, `0`, `(y + 0.5f) * cellSize`.
- Neighbor order: north `+y`, east `+x`, south `-y`, west `-x`.
- Serialized index: `index = y * width + x`.
- Bounds rule: coordinates are valid only when `0 <= x < width` and
  `0 <= y < height`.

All room shapes, mockup cells, module footprints, prop markers, navigation data,
and baked runtime cells must use this contract.

### Core Enum Contracts

Start with compact enums and flags. Expand only when validation requires it.

```csharp
public enum MapGenCellState
{
    Empty,
    Room,
    Corridor,
    Wall,
    Blocked,
    Connector,
    Reserved
}

public enum MapGenRoomCategory
{
    Start,
    Main,
    Side,
    Hub,
    Quest,
    Boss,
    Exit,
    Transition
}

public enum MapGenSocketKind
{
    None,
    Door,
    Corridor,
    Wildcard,
    Blocked
}

public enum MapGenModuleCategory
{
    FloorA,
    FloorB,
    WallStraight,
    WallCornerInside,
    WallCornerOutside,
    CeilingInterior,
    CeilingExterior,
    DoorWhole,
    DoorFrameHalf,
    DoorPanelHalf,
    Prop
}

public enum MapGenGenerationPhase
{
    ValidateProfile,
    BuildDomain,
    SolveMockup,
    PostProcess,
    AcceptMockup,
    Materialize,
    PlaceProps,
    BakeRuntime
}
```

### Core Type Contracts

Initial runtime-safe structs/classes should be explicit and testable.

```csharp
public readonly struct MapGenGridCoord
{
    public readonly int X;
    public readonly int Y;
}

public struct MapGenShapeCell
{
    public MapGenCellState State;
    public MapGenSocketKind SocketKind;
    public string SocketId;
    public string[] Tags;
}

public struct MapGenMockupCell
{
    public MapGenCellState State;
    public int RegionId;
    public MapGenRoomCategory RoomCategory;
    public MapGenSocketKind SocketKind;
    public string SocketId;
}

public sealed class MapGenValidationReport
{
    public bool IsValid;
    public List<MapGenIssue> Issues;
}

public sealed class MapGenIssue
{
    public MapGenGenerationPhase Phase;
    public string Code;
    public string Message;
    public string SuggestedFix;
    public MapGenGridCoord? Cell;
}

public sealed class MapGenGenerationResult
{
    public bool Success;
    public int Seed;
    public int RetryCount;
    public MapGenMockupData Draft;
    public MapGenValidationReport Report;
}

public sealed class MapGenMockupData
{
    public int Width;
    public int Height;
    public MapGenMockupCell[] Cells;
    public string Signature;
}
```

`Core` must not depend on `UnityEngine.Object`, `ScriptableObject`, or editor
asset types. If Unity object context is needed, keep it in an authoring/editor
wrapper:

```csharp
public sealed class MapGenAuthoringIssue
{
    public MapGenIssue Issue;
    public UnityEngine.Object Context;
}
```

### Deterministic RNG Contract

Do not use `UnityEngine.Random` in generation.

Requirements:

- Use one explicit RNG type, e.g. `MapGenRandom`.
- Store initial seed in every mockup draft and runtime bake.
- Derive sub-streams by named phase: solver, post-process, materialization,
  prop placement.
- Same profile GUIDs, same asset data, same seed, and same code version must
  produce the same mockup signature.
- Weighted selection must use the same RNG path regardless of editor repaint or
  scene preview calls.

Recommended API:

```csharp
public struct MapGenRandom
{
    public MapGenRandom(int seed);
    public int NextInt(int minInclusive, int maxExclusive);
    public float NextFloat01();
    public MapGenRandom Fork(string streamName);
}
```

### Asset Creation Menus

Add explicit creation paths for all authoring assets.

```text
Assets/Create/Conn/MapGenV2/Profile
Assets/Create/Conn/MapGenV2/Style Set
Assets/Create/Conn/MapGenV2/Module Set
Assets/Create/Conn/MapGenV2/Room Shape
Assets/Create/Conn/MapGenV2/Room Template
Assets/Create/Conn/MapGenV2/Corridor Template
Assets/Create/Conn/MapGenV2/Rule Set
```

Editor window path:

```text
Conn/MapGenV2/Map Generator
```

Do not put new production workflow menus under legacy paths.

### Default Asset Storage Policy

Generated and authored assets should have predictable paths.

```text
Assets/Conn/Authoring/MapGenV2/Profiles/
Assets/Conn/Authoring/MapGenV2/StyleSets/
Assets/Conn/Authoring/MapGenV2/ModuleSets/
Assets/Conn/Authoring/MapGenV2/RoomShapes/
Assets/Conn/Authoring/MapGenV2/Templates/
Assets/Conn/Authoring/MapGenV2/Drafts/
Assets/Conn/Authoring/MapGenV2/MaterializedPrefabs/
Assets/Conn/Core/MapGenV2/BakedMaps/
```

Regeneration policy:

- Mockup generation creates a new draft unless the user explicitly overwrites
  the selected draft.
- Accepting a mockup sets `accepted = true` and records an immutable
  `acceptedSignature`.
- Materialization creates or updates output under a selected output path.
- Baking writes runtime-safe data and stores the accepted mockup signature used
  for the bake.
- If source assets change after acceptance, the draft is marked stale until
  revalidated.

### Custom Inspector Editing Contract

Room shape editing must use normal Unity editor safety mechanisms.

- Use `SerializedObject` and `SerializedProperty` where possible.
- Call `Undo.RecordObject` before cell edits.
- Mark assets dirty only after actual serialized data changes.
- Support multi-cell drag painting.
- Clamp dimensions before resizing cell arrays.
- Preserve existing cells when resizing when possible.
- Rebuild preview cache only after data changes, not on every repaint.
- Never instantiate scene objects during room shape inspector painting.

### Mockup Signature Contract

Every generated mockup must expose a deterministic signature for tests and stale
asset checks.

Signature inputs:

- profile id and serialized profile settings.
- style id and module set id only when materialization-affecting rules are
  included.
- room shape ids and cell data.
- rule set data.
- seed.
- final mockup cell states, region ids, connector ids, and post-process history.

Signature excludes:

- editor window layout.
- scene camera position.
- preview mesh instance ids.
- Unity object instance ids.

### Materialization Classification Contract

Materialization converts accepted mockup cells into module requests through
neighbor inspection.

Base rules:

- A navigable room or corridor cell requests one floor module.
- A navigable cell edge adjacent to empty, blocked, or out-of-bounds requests a
  wall.
- A navigable cell edge adjacent to another navigable cell does not request a
  wall unless a connector/door rule overrides it.
- A connector between two navigable regions requests a door module when doors
  are enabled.
- A blocked cell never receives floor modules.

Corner rules:

- A convex outside corner exists when two perpendicular boundary edges meet
  around an empty/out-of-bounds diagonal.
- A concave inside corner exists when two perpendicular navigable edges meet
  around a blocked or wall-producing diagonal.
- Corner rules must run after straight wall edge detection.
- If a prefab category cannot represent a corner, fallback to straight wall
  pieces and report a warning.

Ceiling rules:

- Interior room cells request interior ceilings when the style enables ceilings.
- Exterior shell or outside boundary cells request exterior ceiling modules only
  if the style requests an outer shell.
- Corridor ceilings are controlled separately from room ceilings.

Door rules:

- Whole-door modules take priority when available.
- If whole doors are unavailable, use half frame/panel modules only when both
  required half categories are available.
- Missing door modules are validation errors when doors are required, warnings
  when doors are optional.

### Runtime Bake Contract

Runtime bake must not serialize editor-only references.

Allowed runtime data:

- primitive values.
- runtime-safe enums.
- stable content ids.
- grid coordinates.
- region ids.
- traversal edges.
- spawn/objective marker ids.
- prefab/content ids only if runtime loading is explicitly supported.

Disallowed runtime data:

- `UnityEditor` types.
- editor window state.
- `ScriptableObject` authoring references unless the runtime system already
  supports loading them.
- scene preview objects.
- generated diagnostic meshes.

### First Implementation Slice

The first development slice should be intentionally small.

1. Add asmdefs and empty namespace folders.
2. Add core enums and grid coordinate helpers.
3. Add `MapGenRoomShapeAsset` and `MapGenShapeCell`.
4. Add room shape custom inspector with clickable grid.
5. Add validation for dimensions, occupied cells, and connector edge placement.
6. Add tests for indexing, resizing, and validation.
7. Commit and push.

Do not start WFC, materialization, prop placement, or runtime bake before this
slice is stable.

## Validation Requirements

Profile validation must run before generation.

Validation checks:

- profile has a style set.
- style set has a module set.
- required room categories have at least one legal template.
- required room categories have at least one legal mockup room shape.
- required module categories have at least one prefab when materialization uses
  that category.
- corridor pools can connect the required room templates.
- connector sockets have compatible counterparts.
- map size can fit the requested room and corridor quantities.
- room shape dimensions and occupied cells are valid for the grid.
- module prefabs align to the configured grid size.
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

### Execution Checklist

Use this checklist as the actual development order. Each completed group should
be committed and pushed separately.

- [ ] Foundation: asmdefs, folders, core enums, grid coordinate helpers.
- [ ] Room shape authoring: `MapGenRoomShapeAsset`, cell data, custom grid
  inspector, undo/redo, validation.
- [ ] Module set authoring: `MapGenModuleSetAsset`, weighted prefab entries,
  prefab grid validation.
- [ ] Profile/style/rule assets: profile wiring, style set, rule set, creation
  menus, default folders.
- [ ] Validation reports: structured issue codes, suggested fixes, context
  objects, editor display.
- [ ] Mockup draft model: cells, regions, signatures, accepted/stale state,
  save/load.
- [ ] Mockup preview: blue/red/black/gray grid renderer and overlays.
- [ ] Solver MVP: deterministic connected mockup layout with required rooms and
  corridor paths.
- [ ] Post-processing MVP: direct route, dead-end reduction, small room removal,
  per-pass report.
- [ ] Mockup acceptance flow: accept, reject, regenerate, stale detection.
- [ ] Materialization MVP: floor/wall/corner/ceiling/door module classification
  and deterministic weighted prefab placement.
- [ ] Prop placement MVP: prop channels, deterministic weighted placement,
  traversal-safe validation.
- [ ] Runtime bake MVP: runtime-safe grid, regions, doors, traversal graph,
  markers.
- [ ] Editor window MVP: profile selection, validation, generate mockup, accept,
  materialize, bake.
- [ ] Integration tests and manual Unity checks.

### Milestone 1: Data Contracts

- Add `MapGenV2` runtime namespace and authoring assets.
- Add profile/style/module-set/template/rules/connectors/mockup draft assets.
- Add grid-editable room shape assets.
- Add validators with actionable error reports.
- No generation yet.

Acceptance:

- A profile can be authored and validated.
- Missing style, missing module set, missing room shapes, missing templates, and
  incompatible connectors are reported.

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

- Add weighted module set selection.
- Convert accepted red room masses into style-specific room prefabs/meshes.
- Convert accepted black corridor paths into style-specific corridor
  prefabs/meshes.
- Classify mockup cells into floor, straight wall, inside corner, outside
  corner, ceiling, door, and prop module requests.
- Preserve room/corridor identity and connector metadata.
- Validate overlap, walls, doors, and reachability.

Acceptance:

- Materialization only runs after mockup acceptance.
- The materialized scene matches the accepted mockup structure.
- Module selection is deterministic for the same accepted mockup, module set,
  and seed.
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
- missing module set fails validation.
- missing required module prefab fails validation.
- missing required room shape fails validation.
- missing required room template fails validation.
- incompatible connectors fail validation.
- module prefab grid mismatch fails validation.
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
- assign a module set with floor, wall, ceiling, and door prefab pools.
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

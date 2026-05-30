# MapGenV2 External Reference Review

Date: 2026-05-31
Status: research notes for continuing MapGenV2 after reviewing the user-provided
MoraMapGen references and adjacent Unity dungeon-generator references.

## Purpose

This document records what should be kept in mind when implementing MapGenV2
beyond the current MVP. It does not replace `new_map_generator_plan.md` or
`mapgenv2_remaining_work.md`; it supplements them with external-reference
checks.

## Sources Reviewed

- MoraMapGen Asset Store page:
  `https://assetstore.unity.com/packages/tools/level-design/procedural-rooms-corridors-dungeon-generator-wfc-moramapgen-289878`
- MoraMapGen YouTube demo:
  `https://www.youtube.com/watch?v=5qo8odfwXk4`
- Unity AI Navigation / NavMesh Surface documentation:
  `https://docs.unity.cn/Packages/com.unity.ai.navigation%401.0/manual/NavMeshSurface.html`
  and
  `https://docs.unity.cn/6000.2/Documentation/Manual/com.unity.ai.navigation.html`
- Dungeon Architect Snap Grid Flow documentation:
  `https://docs.dungeonarchitect.dev/unity/snap-grid-flow/sgf-introduction/`
  and
  `https://docs.dungeonarchitect.dev/unity/snap-grid-flow/sgf-create-flow-graph/`
- Dungeon Architect module documentation:
  `https://docs.dungeonarchitect.dev/unity/sgf-modules.html`
- DunGen Doorways and connection rules documentation:
  `https://dungen-docs.aegongames.com/2.18/core-concepts/doorways/`
  and
  `https://dungen-docs.aegongames.com/2.19/advanced-features/connection-rules/`
- WFC overview:
  `https://www.gridbugs.org/wave-function-collapse/`
- Unity UI Toolkit custom editor window documentation:
  `https://docs.unity.cn/Manual/UIE-HowTo-CreateEditorWindow.html`
- Unity UI Toolkit custom inspector documentation:
  `https://docs.unity.cn/2021.3/Documentation/Manual/UIE-HowTo-CreateCustomInspector.html`
- Unity Scene View custom overlays documentation:
  `https://docs.unity3d.com/2021.3/Manual/overlays-custom.html`
- Unity `Handles` Scene View drawing/editing API:
  `https://docs.unity3d.com/ScriptReference/Handles.html`
- Unity Localization package documentation:
  `https://docs.unity.cn/Manual/com.unity.localization.html`
- Scrum.org Definition of Done reference:
  `https://www.scrum.org/resources/definition-done`
- Unity Test Framework Edit Mode and Play Mode documentation:
  `https://docs.unity.cn/Packages/com.unity.test-framework%402.0/manual/edit-mode-vs-play-mode-tests.html`
- Unity Build Automation unit test documentation:
  `https://docs.unity.com/en-us/build-automation/reference/unit-tests`
- Unity Code Coverage package documentation:
  `https://docs.unity.cn/2023.3/Documentation/Manual/com.unity.testtools.codecoverage.html`

## Goal Execution Documentation Implications

The external references imply that MapGenV2 planning documents should separate
four kinds of criteria:

- Definition of Done:
  cross-cutting quality bar for whether a goal can be considered complete.
- Acceptance criteria:
  user-visible behavior and data outcomes specific to one goal.
- Verification criteria:
  the minimal automated/manual checks required to inspect that goal.
- Out-of-scope criteria:
  explicit boundaries that prevent a goal from expanding into unrelated work.

These are captured in `mapgenv2_goal_execution_guide.md`.

## Already Covered In Current Documents

The following reference-derived requirements are already present in
`new_map_generator_plan.md` and/or `mapgenv2_remaining_work.md`:

- Two-stage workflow:
  mockup generation and iteration first, mesh/prefab materialization later.
- Blue/red/black/gray mockup color language.
- Inspector-editable grid room shapes.
- WFC-like layout solving with post-processing.
- Deterministic seed replay.
- Style/module separation so the same layout can be skinned differently.
- Weighted prefab module entries.
- Room/corridor templates and connectors.
- Prop placement rules.
- Runtime bake data with no editor-only references.
- Navigation outputs for graph traversal, grid pathfinding, and Unity
  navigation adapters.
- Click/select/edit generated room/corridor regions in the mockup preview.

## Gaps To Add Or Keep Explicit During Implementation

### 1. Module Bounds Contract

External references such as Dungeon Architect make module bounds a first-class
contract. MapGenV2 currently mentions `CellSize`, pivots, footprint, and
alignment, but should be stricter:

- Add a `MapGenModuleBoundsAsset` or equivalent settings block.
- Store chunk/cell world size, vertical height, origin/pivot mode, door height,
  and rotation symmetry requirements.
- Validate that every room/corridor template and materialized prefab fits the
  declared bounds.
- Report bounds mismatches before generation or materialization.

Reason: without a single bounds contract, real prefabs will drift, rotate
incorrectly, overlap, or produce door-height mismatches.

### 2. Module Database Compile Step

Dungeon Architect and DunGen-style workflows treat registered modules as a
validated database. MapGenV2 has module sets and validation, but the production
workflow should include an explicit compile/cache step:

- Compile module sets into a solver/materializer cache.
- Cache connector locations, bounds, rotations, tags, prefab categories, and
  weights.
- Show a compile status and stale indicator when source modules change.
- Block generation/materialization if the compiled cache is missing or stale.

Reason: scanning prefabs and ScriptableObjects repeatedly inside generation
makes failures late, slow, and harder to diagnose.

### 3. Door Connector And Blocker Authoring

DunGen documents doorways, sockets, connector objects, and blockers as core
assembly concepts. MapGenV2 already has connectors, but should explicitly
distinguish these authoring concepts:

- Doorway/socket: abstract compatibility point used by the solver.
- Connector prefab: visual object placed when two regions connect.
- Blocker prefab: wall/plug object placed when a possible doorway is unused.
- Door category: whole door, split frame/panel, wide door, locked door, hidden
  door, one-way door, custom.

Reason: production maps need unused openings to be visually closed, not merely
ignored in data.

### 4. Connection Tags And Deny Rules

DunGen connection rules show that tags are useful not only for selecting modules
but also for denying bad pairings.

Add explicit rule support for:

- allow/deny room category pairs
- allow/deny corridor category pairs
- allow/deny door/socket tag pairs
- prevent direct corridor-to-corridor or room-to-room cases when a profile wants
  an intermediate connector
- required adjacency, forbidden adjacency, and one-way door policies

Reason: socket ids alone are not enough for designer-level constraints.

### 5. Navigation Build Strategy

Unity documentation positions `NavMeshSurface` as a component that defines which
scene objects contribute to the navmesh, and the AI Navigation package supports
edit-time and runtime navmesh workflows. MapGenV2 should decide this explicitly:

- Editor materialization can add or update one generated root-level
  `NavMeshSurface`.
- Runtime generation should optionally defer navmesh build until all modules and
  props are placed.
- Provide modes:
  no navmesh, generated root surface, per-zone surface, external user-provided
  surface.
- Validate blocker props before navmesh bake.
- Keep grid pathfinding data independent from Unity navmesh data.

Reason: running navmesh build too early or from the wrong hierarchy can produce
partial or stale navigation output.

### 6. Streaming And Large Map Boundaries

Dungeon Architect references level streaming for large dynamic levels. Current
MapGenV2 performance notes mention large maps, but streaming boundaries are not
specific enough.

Add later-production requirements:

- Optional zone/sector partitioning in the mockup graph.
- Materialized output grouped by sector.
- Runtime bake data records sector bounds and neighbor links.
- Generated root can be split into streaming-friendly child prefabs or scenes.

Reason: large procedural maps can become too heavy if the materializer only
creates one monolithic hierarchy.

### 7. WFC Frequency And Contradiction Diagnostics

WFC references emphasize adjacency rules and relative frequency/weight. Current
docs mention weighted selection and retries, but implementation should expose:

- candidate count/entropy per region or cell
- tile/template frequency weights
- contradiction location
- last eliminated candidates
- failed rule name
- retry seed/step summary

Reason: designers need to know whether failures are caused by missing
connectors, over-tight rules, bad weights, blocked regions, or insufficient
space.

### 8. User-Editable Overrides As First-Class Data

The current remaining-work document now states that selected mockup edits must
persist. Implementation should avoid storing these edits as loose mutations.

Use explicit override records:

```text
MapGenMockupOverride
  targetRegionId
  targetCells
  overrideKind
  value
  lockPolicy
  createdFromSignature
```

Possible override kinds:

- locked region
- forced category
- forced template/shape
- removed region
- blocked/reserved cells
- forced connector
- forbidden connector
- prop channel override

Reason: regeneration can then preserve designer edits intentionally instead of
accidentally keeping stale grid cells.

### 9. Editor UX Should Be Treated As Product Work

Unity's UI Toolkit documentation positions custom editor windows and custom
inspectors as normal ways to build user-friendly editor tooling. MapGenV2 should
therefore not be implemented as a thin row of buttons above raw object fields.

Required UX direction:

- Use a structured editor window:
  setup/assets, visual preview, selected item details, diagnostics.
- Prefer custom inspectors for profile, room shape, module set, rule set,
  draft, and baked map assets.
- Make every disabled action explain why it is disabled.
- Add context-sensitive help, quick-create buttons, ping/select/open buttons,
  and validation summaries next to the data they affect.
- Keep raw serialized arrays behind debug/advanced foldouts.
- Preserve Undo/Redo and dirty-state behavior for asset edits.

Reason: the reference tool is presented as a production level-design workflow,
not as a debug API surface.

### 10. Korean Localization Is A First-Class UX Requirement

Unity's Localization package supports string localization, asset localization,
pseudo-localization, and import/export flows. MapGenV2 editor UI should be ready
for Korean users instead of shipping English-only editor labels.

Required localization direction:

- Keep internal ids, enum names, asset ids, and code symbols in English.
- Localize user-visible labels, tooltips, warnings, errors, summaries, and help
  boxes.
- Add `Auto`, `한국어`, and `English` language modes in the MapGenV2 window.
- Use flexible layouts and wrapping help boxes so Korean text does not clip.
- Add a missing-localization diagnostic for MapGenV2 UI strings.

Reason: the user-facing authoring workflow is complex enough that Korean labels
and explanations materially affect usability.

### 11. Scene View Should Explain Generated Output

Unity Scene View overlays and `Handles` allow tools to expose contextual actions,
visual guides, and custom editing controls directly in the Scene View. MapGenV2
should use these only as an inspection/materialization aid, not as a replacement
for the main mockup preview.

Required Scene View direction:

- Add a MapGenV2 overlay for common actions and visibility toggles.
- Draw handles/gizmos for grid bounds, selected region outlines, connectors,
  sockets, door/blocker markers, prop channels, nav bounds, and diagnostics.
- Clearly show whether the scene content is an unaccepted mockup, accepted
  mockup, materialized output, stale materialized output, or baked output.
- Name generated scene roots and child groups predictably.
- Let selected materialized objects point back to draft id, region id, template
  id, and module id.

Reason: after materialization, designers need to understand what came from which
part of the accepted mockup and whether the scene output is still current.

## Problems To Watch For

- Treating `Generate Mockup` as scene-object creation will slow iteration and
  recreate the earlier confusion where the user expects a visible editor preview
  but only gets hierarchy objects.
- Letting materialization rerun the solver will break trust; materialization
  must consume the accepted mockup exactly.
- Using child GameObjects as the source of room-shape truth will make diffs,
  validation, tests, and runtime bake harder than serialized grid assets.
- Socket compatibility without blocker/unused-door handling will produce maps
  that are traversable in data but visually broken.
- Styling directly inside layout rules will prevent reusing one layout with
  multiple visual skins.
- Baking runtime data from scene objects only, instead of from the accepted draft
  and materialization plan, can create silent drift between editor preview and
  runtime behavior.
- Shipping English-only labels and terse error codes will make the tool hard to
  use in the current project workflow.
- Letting Scene View output be the only visible feedback repeats the current MVP
  problem where `Generate Mockup` appears to do nothing until the user performs a
  later action.

## Reference-Driven Additions Recommended For `mapgenv2_remaining_work.md`

Add or keep explicit tasks for:

1. `MapGenModuleBoundsAsset` or profile-level module bounds settings.
2. Compile/cache step for module sets and template pools.
3. Door connector/blocker authoring and materialization.
4. Connection tag allow/deny rules.
5. Generated root `NavMeshSurface` strategy and runtime build timing.
6. Optional sector/streaming partition metadata.
7. WFC candidate/contradiction diagnostics visible in the preview.
8. Explicit `MapGenMockupOverride` records for user edits.
9. UI Toolkit-based editor windows/custom inspectors with guided workflow UX.
10. Korean localization for all user-visible editor strings.
11. Scene View overlay/handles for inspecting materialized output and source
    metadata.

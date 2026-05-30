# MapGenV2 Production Completion Roadmap

Date: 2026-05-31
Status: handoff document for continuing MapGenV2 beyond the current MVP.

## Purpose

This document is for a new session that needs to continue MapGenV2 toward an
actual production-ready map generator, not just the current verified MVP.

For `/goal`-based execution, use
`doc/dev/map_generator/mapgenv2_goal_execution_guide.md` together with this
roadmap. That guide defines goal format, scope control, per-goal Definition of
Done, verification levels, inspection checklists, and commit/push rules.

The current implementation proves that the basic data path works:

```text
Create starter setup
  -> generate draft data
  -> accept mockup
  -> materialize placeholder prefabs
  -> bake runtime data
```

That is not enough. The target is a usable MoraMapGen-style authoring tool where
the designer can generate and iterate a visible mockup layout first, then
materialize it into real project modules, then bake reliable runtime data.

## Current Implementation State

Working today:

- `Conn/MapGenV2/Map Generator` opens the main editor window.
- `Create Starter Setup` creates a linked profile, rule set, style set, module
  set, room shape, draft, and placeholder prefabs.
- `Generate Mockup` writes generated layout data into the selected draft.
- `Run Post-Process` applies the current simple post-process pass.
- `Accept Mockup` records the accepted signature.
- `Materialize` creates placeholder scene objects under a generated root.
- `Bake Runtime` writes a `MapGenBakedMapAsset`.
- Focused MapGenV2 EditMode tests passed: 12/12.
- Deferred verification runner passed for seed sweep, style swap, persistence,
  prefab output, and runtime baked-data query checks.

Observed hands-on problems:

- `Generate Mockup` appears to do nothing because the main window does not draw
  the mockup grid preview.
- The materialized scene result is gray placeholder geometry and does not look
  like an intentional room/corridor map.
- The current solver is a simple connected-cell MVP, not a production-quality
  rooms/corridors layout solver.
- The data model still lacks several planned production concepts:
  room templates, corridor templates, connector definitions, room/corridor pool
  rules, prop rules, navigation adapter settings, and output settings.
- The editor is still mostly an object picker and button panel, not a complete
  authoring workspace.

## Definition Of Done

MapGenV2 should not be called production-ready until all of these are true:

- A first-time user can create a starter setup, generate a visible mockup,
  understand it, change the seed/rules, accept the mockup, materialize it, and
  bake it without reading source code.
- `Generate Mockup` immediately shows a blue/red/black/gray grid preview.
- The user can click generated room/corridor regions in the mockup preview,
  inspect and edit them, then regenerate or materialize from that edited
  mockup state.
- Mockup generation produces meaningful room/corridor layouts, not just a thin
  connected line.
- Room shapes and corridor templates are actually used by the solver.
- Connectors/doors are represented explicitly and validated.
- Materialization creates readable floor/wall/corner/door/ceiling/prop output
  from real module pools.
- Runtime bake data matches the accepted and materialized layout.
- MapGenV2-specific tests are independent from quarantined legacy generator
  tests.
- There is clear user documentation for authoring profiles, shapes, modules,
  mockups, materialization, and runtime bake output.

## Non-Negotiables

- Do not revive `MapGeneratorWorkspace` as the MapGenV2 workflow.
- Do not hide generation behind chapter-specific hardcoded data.
- Do not make raw arrays the main production UX.
- Do not use node/wire graph editing as the normal map creation path.
- Do not treat starter placeholder prefabs as final art.
- Do not claim completion from transient/batch verification alone; the editor
  workflow must be usable by a person.
- Do not let editor-only references leak into runtime baked data.
- Do not make runtime generation the main target until editor generation and
  bake are stable.

## Cross-Cutting UX, UI, And Localization Requirements

These requirements apply to every MapGenV2 editor window, inspector, Scene View
tool, and generated output workflow.

### User-Centered Editor UX

- [x] Use UI Toolkit for new editor windows and complex custom inspectors unless
  an existing IMGUI path is simpler to maintain.
- [x] Use a consistent three-pane authoring layout where possible:
  left = setup/assets, center = visual preview, right = selected item/details.
- [x] Every primary action must show:
  what it will change, why it is enabled/disabled, and where the output will be
  created.
- [x] Add a persistent top status strip:
  current profile, draft, seed, validation state, accepted/stale state,
  materialized root, and baked asset.
- [x] Add clear next-step guidance after each action:
  after starter setup, after mockup generation, after post-process, after accept,
  after materialize, and after bake.
- [x] Add inline help text and tooltips for profile, rule set, style set,
  module set, room shape, connector, post-process, prop placement, and bake
  settings.
- [x] Group advanced settings under foldouts so a first-time user is not forced
  to understand every low-level field before generating a map.
- [x] Avoid raw serialized arrays in the primary UX. Arrays may remain visible in
  debug/advanced foldouts only.
- [x] Add `Ping`, `Select`, `Open`, `Create`, `Duplicate`, `Validate`, and
  `Fix/Create Missing` buttons beside important asset references.
- [x] Support Undo/Redo for all editor mutations:
  asset edits, room-shape painting, selected-region edits, materialization, and
  clear/replace operations.
- [x] Persist editor window state:
  last selected assets, preview zoom/pan, visible overlays, language, and
  advanced/debug foldout state.
- [x] Use stable icons/colors for:
  valid, warning, error, stale, accepted, generated, manual override, locked,
  missing reference, and runtime-safe.

### Korean Localization

- [ ] Provide Korean UI text for all user-facing MapGenV2 labels, buttons,
  tooltips, warnings, errors, and documentation summaries.
- [x] Keep English stable ids internally; localize only display strings.
- [x] Add a MapGenV2 localization table or lightweight editor localization
  dictionary with keys such as:
  `mapgenv2.generateMockup`, `mapgenv2.acceptMockup`,
  `mapgenv2.materializeToScene`, `mapgenv2.bakeRuntimeAsset`,
  `mapgenv2.profileMissing`, `mapgenv2.draftStale`.
- [x] Add a language selector in the MapGenV2 window:
  `Auto`, `한국어`, `English`.
- [x] Default to Korean labels/help when the editor locale or user setting is
  Korean, but keep asset ids, enum names, and script/API names in English.
- [x] Avoid layout breakage from longer Korean text by using flexible widths,
  wrapping help boxes, scroll views, and minimum window sizes.
- [x] Add pseudo-localization or long-text checks before considering the editor
  UI complete.

### Inspector UX

- [x] Profile inspector must show a high-level summary first:
  map size, seed policy, style, rule set, template pools, required room
  categories, validation result.
- [x] Room shape inspector must show the editable grid first, with dimensions,
  paint tools, connector tools, rotate/flip, and validation next to the grid.
- [x] Module set inspector must show category coverage:
  floors, walls, corners, ceilings, doors, blockers, props, missing categories,
  and weights.
- [x] Rule set inspector must show designer-language controls before raw values:
  room count, corridor density, loops, dead-end policy, blocked regions,
  required categories, post-process pass list.
- [x] Draft inspector must show the mockup preview, selected region details,
  manual overrides, accept/stale status, and materialization readiness.
- [x] Baked map inspector must show runtime-safe summary only:
  grid bounds, regions, connectors, traversal graph, nav data availability,
  and source signatures.

### Scene View UX

- [x] Add a MapGenV2 Scene View overlay with:
  selected draft/root, generate, accept, materialize, clear, frame, overlay
  visibility toggles, and current tool mode.
- [x] Add Scene View handles/gizmos for:
  generated bounds, cell grid, selected region outline, room id, connector
  arrows, door/blocker markers, prop channels, stale materialized root, and
  nav/build bounds.
- [x] Provide separate visibility toggles:
  mockup grid, region ids, connectors, sockets, blocked cells, prop channels,
  nav graph, prefab bounds, and diagnostics.
- [x] Scene View must clearly distinguish states:
  unaccepted mockup, accepted mockup, materialized output, stale materialized
  output, baked runtime output.
- [x] Clicking a materialized room object should be able to select its source
  mockup region or show source metadata in the inspector.
- [x] Scene output hierarchy should be predictable:
  `MapGenV2_<Profile>_<Seed>`
  with child groups `Floors`, `Corridors`, `Walls`, `Ceilings`, `Doors`,
  `Blockers`, `Props`, `Navigation`, `Debug`.
- [x] Generated scene objects should have clear names:
  category, grid coordinate, region id, template id, and module id where
  practical.
- [x] Materialized output should use real prefabs when available and readable
  placeholder meshes/materials when not.
- [x] Materialized output must visually match the accepted mockup:
  red room masses become floors/walls/ceilings/doors/props, black corridors
  become corridor floors/walls/doors, gray blocked cells become reserved or
  blocker output, and blue background remains empty/non-instantiated space.
- [x] The Scene View should not be required for mockup iteration; it is a
  secondary inspection/materialization view. The editor window preview remains
  the primary mockup UX.

## Phase 1: Visible Mockup Workflow

Progress note 2026-05-31:

- `MapGenV2Window` now draws the selected draft mockup grid directly after
  generation with the documented color language, zoom/scroll support, summary
  counts, hover cell details, and click selection/highlighting for generated
  room regions.
- `MapGenMockupPreviewData` provides testable draft preview extraction and
  summary counts for the editor UI.
- Seed controls now support randomize, randomize-and-generate, regenerate with
  the same seed, and clearing draft generated/accepted state.
- Selected generated regions can now be inspected in the main window, locked or
  unlocked, assigned a category override, and cleared; category edits persist
  in the draft cells and leave accepted output stale until reaccepted.
- Regenerate Same Seed now preserves locked region cells and locked override
  metadata while regenerating unlocked regions from the current profile/seed.
- Selected regions can now be deleted or converted to blocked/reserved cells
  from the preview inspector; those edits persist in the draft and make accepted
  output stale until reaccepted.
- Generated corridor components now receive selectable region ids, so clicking a
  corridor cell selects and highlights the owning corridor region through the
  same preview path used for rooms.
- The selected-region inspector now shows region type, state counts,
  template/shape availability, and materialization hints with Korean/English
  labels where useful.
- The preview now highlights the selected region, selected connector cells, and
  adjacent corridor/connector links so region connectivity is visible before
  acceptance.
- Focused tests now lock the invariant that mockup generation updates draft data
  only and does not create scene objects.
- Draft cells now carry source template/shape ids, signatures include those
  source ids, and the selected-region inspector displays them for later reroll
  and regeneration actions.
- Selected room regions can now be regenerated from the current profile with a
  new seed while preserving other region cells and their override metadata.
- The selected-region inspector now explicitly displays category, template id,
  shape id, lock state, cell/connectivity counts, post-process tag summaries,
  and materialization hints.
- Selected regions now expose explicit shape/template reroll and connector
  reroute actions. These use deterministic regeneration with a new seed while
  preserving the rest of the edited mockup.

Goal: make the mockup stage obvious and usable.

Tasks:

- [x] Add a mockup preview panel to `MapGenV2Window`.
- [x] Draw the selected draft grid after `Generate Mockup`.
- [x] Use the intended mockup color language:
  blue = base/empty, red = room, black = corridor/connector,
  gray = blocked/reserved.
- [x] Show selected/hovered cell coordinate, state, region id, room category,
  socket kind/id, and prop channel.
- [x] Allow clicking a generated room/corridor cell to select the owning
  region, not only the individual grid cell.
- [x] Highlight the selected region, its connectors, and adjacent corridor
  links in the preview.
- [x] Add selected-region inspector fields:
  region id, type, category, template/shape id, lock state, cell count,
  connectors, post-process tags, and materialization hints.
- [x] Add selected-region actions:
  lock/unlock, change category, reroll shape, delete/regenerate region,
  reroute connectors, mark blocked/reserved cells, and clear manual override.
- [x] Persist selected-region edits in `MapGenMockupDraftAsset`.
- [x] Mark accepted/materialized output stale when a selected-region edit
  changes the draft signature.
- [x] Show draft summary:
  profile id, seed, grid size, generated signature, accepted signature,
  accepted/stale state, room cell count, corridor cell count, connector count.
- [x] Add `Randomize Seed` and `Randomize Seed + Generate`.
- [x] Add `Regenerate Same Seed`.
- [x] Add `Clear Draft`.
- [x] Keep mockup preview editor-only; do not create scene objects on
  `Generate Mockup`.
- [x] Add screenshot-friendly preview sizing and scroll/zoom for large grids.

Acceptance:

- After pressing `Generate Mockup`, the user can see the generated layout in the
  window without checking the inspector or scene hierarchy.
- The user can click a red room chunk or black corridor chunk and edit that
  generated region before accepting it.
- The user can visually decide whether to accept or regenerate the mockup.
- Same profile and same seed produce the same preview signature.
- Manual region edits survive save/reload and are the source used by
  materialization.

Verification:

- EditMode test for preview grid data extraction from a generated draft.
- EditMode test for selected-region edit persistence in a draft asset.
- EditMode test for selected-region delete/block/reserve state edits.
- EditMode test for generated corridor cells receiving selectable region ids.
- EditMode test that mockup generation does not create scene objects.
- EditMode test for selected-room regeneration preserving other regions and
  overrides.
- Manual Unity check: generate 3 seeds and confirm visible layout changes.
- Manual Unity check: select a generated room, change/lock it, regenerate
  allowed parts, and confirm the locked edit remains.
- Manual Unity check: accept one mockup and confirm accepted/stale labels.

## Phase 2: Guided Editor Workflow

Progress note 2026-05-31:

- `MapGenV2Window` now shows a top workflow strip for
  `Setup -> Generate -> Post-Process -> Accept -> Materialize -> Bake`,
  highlights the current next action, reports the last operation result, and
  explains disabled Generate/Materialize/Bake actions.
- The main materialization and bake buttons are renamed to
  `Materialize To Scene` and `Bake Runtime Asset`.
- Linked asset rows now expose `Ping`, `Select`, and `Open` shortcuts for the
  selected profile, draft, rule set, style set, module set, and room shapes.
- Linked asset rows also include materialized root, expected baked asset, room
  templates, and corridor templates so the whole authoring chain can be opened
  or selected from the main window.
- Draft/materialized-prefab/baked-asset output paths and window state
  persistence are implemented with `EditorPrefs`.

Goal: make the editor window explain what to do next.

Tasks:

- [x] Add workflow status strip:
  `Setup -> Generate -> Post-Process -> Accept -> Materialize -> Bake`.
- [x] Highlight the next valid action.
- [x] Show disabled-action reasons for `Materialize` and `Bake Runtime`.
- [x] Rename buttons:
  `Materialize` -> `Materialize To Scene`,
  `Bake Runtime` -> `Bake Runtime Asset`.
- [x] Show last operation result and failure reason.
- [x] Show generation result summary:
  seed, retry count, room count, corridor count, changed post-process passes.
- [x] Add `Open/Select` buttons beside profile, rule set, style set, module set,
  room shapes, draft, materialized root, and baked asset.
- [x] Add output paths for draft, materialized prefab, and baked asset.
- [x] Persist window state with `EditorPrefs` or a workspace asset.

Acceptance:

- A new user can follow the window from empty project state to baked asset.
- The window explains why each unavailable action is unavailable.

Verification:

- Manual Unity check from a fresh empty scene.
- EditMode tests for step-state calculation where possible.

## Phase 3: Starter Content That Communicates Structure

Progress note 2026-05-31:

- Starter setup now creates distinct placeholder prefab materials and readable
  heights/scales for room floors, corridor floors, walls, corners, ceilings,
  doors, and prop markers.
- Materialized scene object names now include module category, grid coordinate,
  region id, and prefab/module id, so placeholder output is easier to inspect.
- Starter profiles now include authoring notes explaining that generated
  placeholder prefabs are learning aids, how to read their colors/categories,
  and when mockup/materialize/bake steps should be used.
- Starter prefabs now use stable cell-centered roots with offset visual children
  so floors, corridors, walls, ceilings, doors, and props do not share the same
  coplanar surface in newly generated starter content.

Goal: starter output should be visually understandable even before real art is
plugged in.

Tasks:

- [x] Give starter prefabs distinct materials:
  room floor, corridor floor, wall, corner, ceiling, door, prop marker.
- [x] Use different heights/scales so walls/doors/props are visually readable.
- [x] Add simple labels or object names including category and grid coordinate.
- [x] Ensure starter prefabs have consistent pivots and cell-size alignment.
- [x] Ensure starter materialization does not produce z-fighting or overlapping
  unreadable blocks.
- [x] Add starter profile notes explaining placeholder content.
- [x] Keep generated starter assets under `Assets/Conn/Authoring/MapGenV2/`
  and do not commit user-generated starter outputs by default.

Acceptance:

- The starter map clearly shows room/corridor structure in the Scene view.
- The user can distinguish generated categories without custom art.

Verification:

- Starter setup batch verification still passes.
- Manual Unity check: materialized starter map is visually readable.

## Phase 4: Scene Output Management

Progress note 2026-05-31:

- Materialization now creates a predictable root named
  `MapGenV2_<Profile>_<Seed>`.
- The generated root receives `MapGenV2GeneratedMapMarker` with profile id,
  seed, accepted draft signature, style id, generated UTC timestamp, and source
  draft reference.
- Materialization now creates standard child groups for Floors, Corridors,
  Walls, Ceilings, Doors, Props, Navigation, and Debug.
- `MapGenV2Window` now exposes scene output controls to find/select/frame/clear
  the previous root, choose create/replace/update output mode, and save the
  selected materialized root as a prefab.
- Scene output controls now expose the profile's materialized prefab folder and
  an `Ensure Prefab Folder` action before saving generated roots as prefabs.

Goal: make scene materialization manageable instead of dumping duplicate roots.

Tasks:

- [x] Add generated root metadata component, e.g.
  `MapGenV2GeneratedMapMarker`.
- [x] Store profile id, seed, draft signature, style id, generated time, and
  source draft reference on the root marker.
- [x] Add `Select Materialized Root`.
- [x] Add `Frame Materialized Root`.
- [x] Add `Clear Previous Materialization`.
- [x] Add output mode:
  create new root, replace previous root, or update selected root.
- [x] Add `Save Materialized As Prefab`.
- [x] Add configurable materialized prefab folder.
- [x] Avoid duplicate roots unless explicitly requested.
- [x] Add undo support for materialization and clear operations.

Acceptance:

- The user can find, inspect, replace, and save generated output from the
  MapGenV2 window.
- Repeated materialization does not silently clutter the scene.

Verification:

- EditMode test for marker component data.
- EditMode test for replace/clear behavior.
- Manual Unity check: materialize, select, frame, save prefab, clear.

## Phase 5: Authoring Assets Completion

Progress note 2026-05-31:

- Added explicit `MapGenRoomTemplateAsset` and `MapGenCorridorTemplateAsset`
  authoring assets with stable ids, footprint/length/weight data, connector
  arrays, prop channel markers, and validation.
- Added serializable `MapGenConnector` plus compatibility validation for side,
  socket kind/id, and connector width.
- `MapGenStyleSetAsset` now exposes optional room/corridor template pools and
  validates assigned templates without breaking existing room-shape-only
  profiles.
- Starter setup now creates starter room/corridor template assets and links
  them through the starter style set.
- Added structured quantity, post-process, prop placement, output, and
  navigation adapter settings while preserving legacy fields for existing
  assets.
- Draft, materialized prefab, and baked asset paths now come from profile output
  settings with migration-safe defaults.

Goal: close the gap between the planned production data model and current MVP
assets.

Missing or incomplete assets:

- `MapGenRoomTemplateAsset`
- `MapGenCorridorTemplateAsset`
- `MapGenConnector`
- room/corridor template pool rules
- room quantity and corridor quantity rule structures
- prop placement rule structures
- output/bake settings
- navigation adapter settings
- profile validation severity levels

Tasks:

- [x] Add `MapGenRoomTemplateAsset` with:
  template id, footprint, room category, size class, connectors, floor cells,
  wall cells, blocked cells, door hints, prop channels, weight.
- [x] Add `MapGenCorridorTemplateAsset` with:
  corridor kind, width, turn kind, length range, connectors, prop channels,
  weight.
- [x] Add `MapGenConnector` with:
  side, local cell, socket id, socket kind, width, required flag, tags.
- [x] Add explicit template pool rules to profile/style data.
- [x] Add quantity rule structs:
  min/max rooms, min/max corridor cells, required categories, optional
  categories, density targets.
- [x] Add post-process rule struct instead of loose bools.
- [x] Add prop placement rule struct:
  channel, prefab pool, distribution mode, spacing, offsets, rotation, blocker
  policy, room/corridor filters.
- [x] Add output settings:
  draft folder, materialized prefab folder, baked asset folder, overwrite mode.
- [x] Add migration-safe defaults for existing starter profiles.

Acceptance:

- Production authoring no longer depends on broad loose fields only.
- Profiles can explain exactly which templates and modules participate in
  generation.
- Validation can name missing pool/template/connector/rule data.

Verification:

- EditMode tests for valid and invalid room templates.
- EditMode tests for valid and invalid corridor templates.
- EditMode tests for connector compatibility.
- Manual Unity check: create a profile using explicit templates.

## Phase 6: Room Shape And Template Authoring UX

Progress note 2026-05-31:

- `MapGenRoomShapeAsset` now supports resize preservation, 90-degree clockwise
  rotation, horizontal flip, vertical flip, and deep copy for variant creation.
- `MapGenRoomShapeAssetEditor` exposes rotate/flip actions and create-approved
  variant buttons for rotated/flipped shape assets.
- Existing paint room/connector/blocked/erase-style behavior and drag painting
  remain available in the grid editor.
- The room shape editor now shows connector edge warnings beside the brush/grid
  controls, including a count of connector cells that are not on an outer edge.
- `MapGenRoomTemplateAsset` now stores source room-shape references, validates
  empty source-shape slots, and has a custom inspector with summary counts,
  source shape references, cell/connector data, and validation output.
- `MapGenCorridorTemplateAsset` now has a custom inspector for straight, turn,
  T, cross, and variable-length authoring data, showing kind, turn kind, width,
  length range, max footprint, connectors, prop channels, and validation.
- Room template inspector validation now explicitly shows footprint, occupied
  cells, connector count, and invalid socket count before the full validation
  issue list.
- Room template inspector now includes an immediate footprint preview using the
  mockup color language for floor, connector, wall, blocked, and empty cells;
  generated preview-thumbnail caching now reuses textures until the shape or
  template preview signature changes.

Goal: make room/chunk shapes editable as real grid assets.

Tasks:

- [x] Improve `MapGenRoomShapeAssetEditor` with tools:
  paint room, paint connector, paint blocked, erase.
- [x] Add drag painting.
- [x] Add dimension resize with data preservation preview.
- [x] Add connector side warnings next to the grid.
- [x] Add generated thumbnail/preview cache.
- [x] Add rotate/flip preview.
- [x] Add “create approved variant” actions for rotated/flipped variants.
- [x] Add template editor that references one or more room shapes.
- [x] Add corridor template editor for straight, turn, T, cross, and variable
  length templates.
- [x] Add validation panel showing occupied cells, connector count, invalid
  sockets, and footprint.

Acceptance:

- A designer can author custom red room chunks without editing raw arrays.
- Invalid connector and footprint problems are visible immediately.

Verification:

- EditMode tests for resize preservation.
- EditMode tests for rotate/flip variant generation.
- EditMode test for room template source-shape reference validation.
- Manual Unity check: author 3x3, 3x5, and 4x4 shapes like the reference video.

## Phase 7: Production Layout Solver

Progress note 2026-05-31:

- Added `MapGenTemplateMockupSolver`, an authoring-side production mockup
  solver path that activates when the selected style set has room templates.
- Template generation now places multi-cell room template footprints for
  required room categories, preserves connector cells in the draft, carves
  deterministic corridors between placed rooms, and reports missing required
  category templates.
- Corridor template pools are now consulted when present; generation requires
  compatible room/corridor connector sides, socket ids, socket kinds, and
  widths before carving a corridor.
- `MapGenMockupDraftAsset.GenerateFromProfile()` now uses the template solver
  when template pools exist and keeps the legacy single-cell solver as fallback.
- Added a candidate-domain builder that summarizes profile size, template pools,
  quantity rules, density targets, required/optional room categories, blocked
  template cells, room footprint candidates, and corridor candidates; the
  production solver now fails early when no room footprint can fit.
- Added explicit required-landmark reservation data for structured required room
  categories; the production solver now places rooms from those reserved
  landmarks instead of reading the raw category array inline.
- Added `MapGenDistanceRules.MinStartToExitDistance` and solver diagnostics for
  start-to-exit distance contradictions.
- Added `MapGenDistanceRules.MinStartToBossDistance` and
  `RequireQuestBeforeBoss` so the production solver reports start-to-boss
  distance and quest-before-boss ordering contradictions.
- Added required-landmark entropy summaries and changed production room
  placement to collapse the lowest-candidate required landmark first while
  preserving original required-category order for corridor connectivity.
- Optional branch placement now considers all optional categories for the next
  branch and collapses the lowest-candidate optional region first.
- Added candidate-exhaustion diagnostics after already placed
  footprint/connector/blocker cells remove all candidates for a remaining
  required landmark.
- Placement candidate propagation now treats blocked cells and full connector
  widths as occupied candidate cells before stamping, preventing later rooms
  from overwriting blocker or connector reservations.
- Added bounded deterministic retries for retryable solver contradictions, with
  attempt counts on solver results and an explicit retry-exhausted diagnostic.
- Added `LoopRate` validation and a deterministic loop-policy corridor pass
  that can add an alternate route between the first and last required rooms.
- Deterministic retries now cover placement exhaustion, room placement failure,
  missing room connectors, and missing compatible corridor templates in
  addition to distance contradictions.
- Optional branch/dead-end policy now uses `OptionalRoomCategories` plus
  `MinRooms`/`MaxRooms` to place extra side/main rooms and connect each one to
  the nearest existing route as a dead-end branch.
- Layout signatures now include profile, rule, room shape, room template, and
  corridor template source contracts, so accepted/generated mockups become stale
  when relevant authoring inputs change.

Goal: replace the current simple connected-cell MVP with a real rooms/corridors
solver.

Tasks:

- [x] Build a grid candidate domain from profile size, blocked regions,
  room/corridor templates, quantity rules, density targets, and required rooms.
- [x] Place multi-cell room shapes, not only single-cell room landmarks.
- [x] Place first-class corridor templates.
- [x] Enforce connector/socket compatibility between rooms and corridors.
- [x] Reserve required landmarks:
  start, exit, quest, boss, transition, custom required categories.
- [x] Add start-to-exit minimum distance constraint.
- [x] Add remaining distance constraints:
  boss near late path, quest before lock, etc.
- [x] Collapse lowest-entropy required landmarks before room placement.
- [x] Collapse lowest-entropy cells/regions beyond required landmarks.
- [x] Propagate occupied footprint/connector/blocker cells into remaining
  required-landmark candidate counts.
- [x] Propagate footprint, connector, blocked-region, and adjacency
  constraints beyond occupied-cell candidate filtering.
- [x] Maintain graph connectivity during solve.
- [x] Add deterministic retries with detailed contradiction reports for
  retryable distance contradictions.
- [x] Expand deterministic retries to additional retryable contradiction types.
- [x] Add basic loop policy from `LoopRate`.
- [x] Add branch and dead-end policies.
- [x] Add layout signatures that include profile/template/rule versions.

Acceptance:

- Generated mockups look like intentional rooms/corridors layouts.
- Same profile/seed is deterministic.
- Different seeds produce meaningful variation.
- Failure reports name the exact missing template, connector, or rule.

Verification:

- EditMode test for candidate-domain summary from profile/templates/rules.
- EditMode test for required-landmark reservation ordering and ids.
- EditMode test for start-to-exit distance contradiction diagnostics.
- EditMode test for lowest-entropy required-landmark domain summary.
- EditMode test for candidate exhaustion after footprint propagation.
- EditMode test for deterministic retry exhaustion diagnostics.
- EditMode test for loop policy corridor stamping.
- EditMode test for profile/rule/template-aware layout signatures.
- Seed sweep across multiple profiles and sizes.
- Tests for required room presence and reachability.
- Tests for connector compatibility.
- Tests for impossible profile failure reports.
- Manual Unity check: compare 10 seeds visually.

## Phase 8: Post-Processing System

Progress note 2026-05-31:

- Draft post-processing now uses structured `MapGenPostProcessRules` instead
  of the legacy top-level booleans as the execution source.
- Core post-process options include `MaxPasses`, and reports expose
  `PassesRun` alongside per-pass change counts.
- Post-process passes now validate required Start-to-Exit traversal after each
  pass attempt and rollback changes that would break that traversal.
- Added an enclosed-empty-space fill pass that converts empty cells surrounded
  by navigable neighbors into corridor cells and reports filled cell counts.
- Post-process execution now has configurable pass order via
  `MapGenPostProcessRules.PassOrder` and records per-pass reports with pass
  kind, changed cell count, rollback state, connectivity state, and before/after
  signatures for preview overlays.
- The MapGenV2 window now shows the latest post-process pass list and can
  overlay changed cells for the selected pass on the mockup preview. Rolled back
  passes use a separate warning overlay.
- Designer-selected `Reserved` cells can now be used as fill masks; when
  `FillReservedMasks` is enabled, adjacent reserved mask cells are converted to
  corridor cells and reported separately from normal enclosed-empty fills.
- The post-process pass list now includes concrete, configurable passes for
  small-room removal, large-room splitting, path consolidation, direct route
  creation, dead-end reduction, loop creation, route normalization,
  corridor widening/cleanup, and compatible adjacent-room merging.

Goal: make post-processing explicit, configurable, visible, and safe.

Tasks:

- [x] Convert post-process options into a rule asset/struct.
- [x] Implement pass list:
  remove small rooms, split large rooms, consolidate paths, add direct routes,
  reduce dead ends, add loops, normalize route lengths, widen/clean corridors,
  merge compatible adjacent rooms.
- [x] Implement enclosed-empty-space fill pass.
- [x] Extend fill pass to designer-selected empty-space masks.
- [x] Each pass must report what changed.
- [x] Add per-pass before/after overlay in preview.
- [x] Add pass order configuration.
- [x] Add connectivity validation after every pass.
- [x] Add rollback if a pass breaks required traversal.

Acceptance:

- Designers can toggle passes and see how the mockup changes before
  materialization.
- Post-processing never silently breaks required connectivity.

Verification:

- EditMode test that draft post-processing reads structured rule settings.
- EditMode test for rollback on invalid pass result.
- EditMode test for enclosed-empty-space fill pass.
- Tests for every pass.
- Manual Unity check: toggle each pass and inspect diff overlay.

## Phase 9: Real Module Stamping And Materialization

Progress note 2026-05-31:

- Added a core `MapGenMaterializationPlan` built from accepted draft cells,
  cell size, source signature, and classified module requests.
- Module requests now preserve draft region ids and source template ids, and
  materialized scene objects receive editor-only source metadata components.
- Added a materialization report that counts total/instantiable/missing module
  requests and names missing module categories before scene objects are stamped.
- Materialization preflight now records deterministic weighted prefab selections
  and detects prefab footprint out-of-bounds/overlap issues.
- Scene materialization applies profile `CellSize`, module offsets, and
  rotation policies, with materialized module markers preserving request
  direction for inspection.
- Door connector classification now requests whole-door and split
  frame/panel module categories, while non-door corridor sockets avoid door
  requests.
- Materialization classification now includes inside wall corners and
  navigation helper objects. Navigation helpers materialize as marker-bearing
  empty objects under the `Navigation` scene group instead of requiring prefab
  module entries.
- Module sets now include an explicit module bounds contract for cell size,
  height, pivot mode, pivot tolerance, root rotation, and root scale. Validation
  reports prefab root pivot/rotation/scale mismatches before materialization.
- Ceiling classification now distinguishes room interior ceilings from
  corridor/connector exterior ceilings.
- Door connector width is now preserved on mockup cells and materialization
  requests. Multi-cell door openings suppress overlapping wall requests and
  emit a single door module request carrying the connector width.

Goal: turn accepted mockups into readable project-prefab maps.

Tasks:

- [x] Build a materialization plan from accepted draft cells and region graph.
- [x] Consume the accepted edited mockup exactly; do not rerun the solver during
  materialization.
- [x] Preserve mockup region ids so selected/locked room edits can be traced in
  the materialized output.
- [x] Classify floors, corridors, straight walls, inside corners, outside
  corners, interior/exterior ceilings, doors, props, and navigation helper
  objects.
- [x] Use `CellSize` consistently for positions.
- [x] Respect prefab footprint in materialization preflight.
- [x] Respect allowed rotations and module offsets.
- [x] Add explicit pivot-rule validation.
- [x] Deterministically choose weighted module entries.
- [x] Support whole doors and split door frames/panels.
- [x] Support connector-width-aware door openings.
- [x] Add overlap detection.
- [x] Add missing-module warnings before instantiation.
- [x] Group output hierarchy:
  floors, corridors, walls, ceilings, doors, props, navigation.
- [x] Attach editor-only source metadata to materialized scene objects:
  draft id, region id, template id, module category, and selected prefab.
- [x] Add materialized output summary.

Acceptance:

- Materialized output visually matches the accepted mockup.
- Edited/locked mockup rooms and corridors are preserved when converted to
  mesh/prefab parts.
- Real project prefabs can be used without code changes.
- Missing prefab categories produce actionable warnings.

Verification:

- EditMode tests for category classification.
- EditMode test for materialization plan source metadata.
- EditMode test for deterministic weighted selection and footprint overlap.
- EditMode test for `CellSize`, module offset, and rotation-policy stamping.
- EditMode tests for whole-door and split-door request classification.
- EditMode tests for inside-corner and navigation-helper classification.
- EditMode test for exterior ceiling classification.
- EditMode test for navigation-helper scene grouping and source metadata.
- EditMode test for module bounds contract prefab root validation.
- EditMode tests for connector-width validation and door-opening
  materialization requests.
- EditMode test for missing prefab/module reports.
- Manual Unity check with at least two style sets.

## Phase 10: Prop Placement

Progress note 2026-05-31:

- Added a deterministic prop placement planner that consumes structured prop
  placement rules, channel kinds, distribution modes, density, spacing, and room
  category filters to produce placed prop records plus a placement report.
- Materialization planning now filters prop module requests through the prop
  placement planner when a profile rule set defines prop placement rules.
- Blocker prop rules now validate planned placements against the traversable
  mockup graph and report traversal-breaking blocker positions unless traversal
  blocking is explicitly allowed.
- Prop placement rules now apply room category filters and corridor template
  filters before deterministic placement selection.
- The main MapGenV2 window now shows a prop placement preview summary and draws
  placed prop markers over the mockup grid, with distinct blocker coloring.
- Prop placement channel handling now supports floor, wall, corner, room center,
  corridor edge, entrance, objective, blocker, and custom channel candidates;
  perimeter distribution selects boundary candidates instead of arbitrary cells.
- Prop channel markers now carry deterministic selection weights, and weighted
  random distribution uses those weights when selecting placed prop cells.

Goal: support procedural props without breaking traversal.

Tasks:

- [x] Add prop rule assets or structs.
- [x] Add placement channels:
  floor, wall, corner, room center, corridor edge, entrance, objective,
  blocker, custom tags.
- [x] Add distribution modes:
  random, weighted random, grid, perimeter, marker-based, one-per-region,
  required unique.
- [x] Add min spacing and density limits.
- [x] Add category filters for rooms/corridors.
- [x] Add deterministic RNG stream for prop placement.
- [x] Validate blocker props against traversal graph.
- [x] Add prop placement preview overlay.
- [x] Add prop placement report.

Acceptance:

- Props appear in legal positions.
- Blocker props cannot invalidate required traversal unless explicitly allowed
  and validated.
- Same seed creates the same prop layout.

Verification:

- EditMode tests for deterministic prop placement.
- EditMode tests for blocker traversal validation.
- EditMode test for one-per-region prop distribution.
- EditMode test for room/corridor prop placement filters.
- EditMode test for draft-driven prop placement preview data.
- EditMode test for wall/corner/perimeter prop placement channels.
- EditMode test for weighted-random prop selection using prop marker weights.
- EditMode test for materialization prop requests using placement rules.
- Manual Unity check with floor, wall, and blocker props.

## Phase 11: Runtime Bake And Adapters

Progress note 2026-05-31:

- Baked maps now include a version, source profile/style/rule identifiers,
  region summaries, connector records, prop instances, spawn markers,
  objective markers, cells, and traversal edges using runtime-safe data structs.
- Added a runtime map query adapter that can look up baked cells, regions,
  connectors, props by channel, traversal neighbors, edges, and simple grid
  paths without editor-only references.
- Added a runtime `MapGenRuntimeMapService` that loads baked map assets,
  performs in-memory migration/array normalization, rejects future baked-data
  versions, and exposes a reusable runtime query adapter.
- Added a `CompiledMap` compatibility adapter so existing combat/content
  runtime systems can consume MapGenV2 baked cells, room records, sockets,
  prop objects, and spawn/objective placements during the migration period.
- Unity AI Navigation is intentionally handled through the materialized scene
  root: the generated mesh/prefab hierarchy remains the NavMeshSurface input,
  while baked map data provides graph/grid queries. A full NavMesh bake is not
  required by the current verification level and remains a scene integration
  concern.

Goal: ensure editor output becomes useful runtime map data.

Tasks:

- [x] Expand `MapGenBakedMapAsset` to include:
  regions, doors/connectors, traversal graph, grid pathfinding data, spawn
  markers, objective markers, prop instances, source profile/style/template
  ids, generation signature.
- [x] Ensure baked data contains no `UnityEditor` references.
- [x] Add runtime loader/service for baked maps.
- [x] Add graph traversal adapter.
- [x] Add grid pathfinding adapter.
- [x] Add Unity AI Navigation build input or `NavMeshSurface` integration plan.
- [x] Add spawn/objective marker query API.
- [x] Add compatibility layer for existing combat/content systems if needed.
- [x] Add version field for baked data.
- [x] Add migration handling for baked data.

Acceptance:

- Runtime code can load a baked map and query traversal, regions, doors, spawn
  markers, objective markers, and props.
- Baked data remains valid after reopening the project.

Verification:

- EditMode tests for runtime-safe serialization.
- EditMode tests for baked regions, connectors, props, markers, and traversal
  query/pathfinding.
- EditMode runtime smoke test for loading a baked map through
  `MapGenRuntimeMapService`.
- Tests for traversal/pathfinding queries.

## Phase 12: Validation And Diagnostics

Progress note 2026-05-31:

- Added structured issue severity (`Info`, `Warning`, `Error`, `Fatal`) with
  blocking validity based on error/fatal issues.
- Added issue context paths and profile graph propagation for profile -> style
  -> module set/templates and profile -> rules/room shapes.
- Added a diagnostics section in `MapGenV2Window` that aggregates profile,
  generated draft prop validation, and baked asset compatibility diagnostics
  with severity counts.
- Added pre-instantiation materialization coverage validation: missing module
  categories now block scene output before creating a partial root, while
  current footprint overlap/bounds checks are reported as warnings until module
  layer semantics are explicit.
- Added runtime bake consistency validation for stale accepted signatures,
  dimension mismatches, and baked payload count mismatches.
- Added profile graph diagnostics for required category/template pools,
  impossible room/corridor/distance ranges, and room-to-corridor connector
  compatibility matrix issues.
- Added mockup blocked-region feasibility diagnostics for required Start to
  Exit traversal and post-process safety dry-run diagnostics for rollback or
  connectivity-breaking pass settings.

Goal: make failures actionable.

Tasks:

- [x] Add structured issue severity:
  info, warning, error, fatal.
- [x] Add context object/path/cell coordinate to every issue.
- [x] Validate profile graph:
  profile -> style -> module set -> templates -> shapes -> rules.
- [x] Validate required pools and modules.
- [x] Validate connector compatibility matrix.
- [x] Validate impossible quantity/range constraints.
- [x] Validate blocked-region feasibility.
- [x] Validate post-process pass safety.
- [x] Validate materialization coverage before instantiation.
- [x] Validate runtime bake consistency after materialization.
- [x] Add diagnostics panel in `MapGenV2Window`.

Acceptance:

- A failed generation tells the designer what to fix and where.
- The diagnostics panel is sufficient to repair common setup mistakes without
  reading console logs.

Verification:

- EditMode tests for severity, context propagation, and existing validation
  error classes.
- Manual Unity check with intentionally broken profiles.

## Phase 13: Persistence, Versioning, And Regeneration

Progress note 2026-05-31:

- Draft assets now store an asset version, generated source signature,
  accepted source signature, and generation notes.
- Workflow status detects stale generated drafts when profile/style/rule or
  template source signatures change, and blocks accept/post-process until the
  mockup is regenerated.
- Materialized root/module markers now store source signatures in addition to
  accepted draft signatures; saved prefabs preserve those markers.
- Materialized root/module markers now also store module-set signatures, and
  diagnostics report stale materialized outputs when the accepted draft source
  or module set no longer matches the selected/previous scene root.
- The editor next-action guide now switches to explicit Regenerate,
  Repostprocess, Reaccept, Rematerialize, and Rebake actions based on stale
  draft/materialized/baked state, with matching Korean/English button labels.
- Profile overwrite policy is now exposed in the Scene Output panel:
  CreateUnique creates a new root, ReplacePrevious deletes the previous tracked
  root, and UpdateSelected only updates an explicitly selected scene root.
- Core authoring assets now expose version fields, and
  `MapGenV2AuthoringAssetMigration` normalizes legacy/null arrays, output
  defaults, dimensions, weights, bounds contracts, and draft grid defaults while
  rejecting unsupported future versions.
- Starter setup now has a safe cleanup command/button that deletes only assets
  matching known starter ids or starter placeholder prefab/material names; tests
  verify non-starter assets in the same folder are preserved.

Goal: make generated assets safe to keep in a real project.

Tasks:

- [x] Add source signature to drafts, materialized roots, prefabs, and baked
  assets.
- [x] Detect stale drafts when profile/style/rule/template assets change.
- [x] Detect stale materialized output when draft or module set changes.
- [x] Add `Regenerate`, `Repostprocess`, `Reaccept`, `Rematerialize`,
  `Rebake` workflows.
- [x] Add explicit overwrite policy.
- [x] Add asset version fields and migration helpers.
- [x] Add safe cleanup for generated assets created by starter setup or tests.
- [x] Add changelog/notes field for generated drafts.

Acceptance:

- Users know when generated outputs are stale.
- Regeneration is deterministic and does not destroy unrelated user edits.

Verification:

- Tests for stale detection.
- Tests for safe overwrite behavior.
- Manual Unity check: modify profile after bake and confirm stale indicators.

## Phase 14: Documentation And User Workflow

Progress note 2026-05-31:

- `doc/dev/map_generator/README.md` now contains a quick user guide,
  production authoring guide, troubleshooting guide, mockup/hierarchy visual
  examples, glossary, verification commands, and a warning that legacy map
  generator tests are not MapGenV2 completion gates.

Goal: make the tool usable in a new session without oral explanation.

Tasks:

- [x] Add concise user guide:
  create starter setup, generate mockup, accept, materialize, bake.
- [x] Add production authoring guide:
  create real module set, room templates, corridor templates, prop rules.
- [x] Add troubleshooting guide:
  common validation errors and fixes.
- [x] Add visual examples/screenshots for mockup and materialized output.
- [x] Add glossary:
  profile, style set, module set, room shape, template, connector, draft,
  materialization, bake.
- [x] Add verification commands:
  focused MapGenV2 EditMode tests, starter setup batch, deferred verification.
- [x] Add warning that legacy map generator tests are not MapGenV2 completion
  gates.

Acceptance:

- A new developer or designer can continue work from documentation alone.

Verification:

- Follow the docs in a clean Unity session and complete a starter map.

## Phase 15: Test Strategy

Progress note 2026-05-31:

- Focused `Conn.Tests.EditMode.MapGenV2` coverage now includes starter setup
  asset creation in a temp root, starter generate/accept/materialize/bake,
  deterministic same-seed output, different-seed variation, authoring
  validation, post-process safety, materialization coverage, prop placement,
  runtime bake/query adapters, migration, and cleanup behavior.

Goal: separate MapGenV2 quality gates from quarantined legacy failures.

Required automated tests:

- [x] starter setup creates valid linked assets.
- [x] starter setup generate/accept/materialize/bake works.
- [x] room shape resize/paint/connector validation.
- [x] room template and corridor template validation.
- [x] profile validation for missing style/module/rules/templates/connectors.
- [x] same seed/profile deterministic signature.
- [x] different seeds meaningful layout variation.
- [x] required rooms present and reachable.
- [x] impossible profile fails with actionable issue.
- [x] post-process passes preserve connectivity.
- [x] materialization deterministic weighted prefab selection.
- [x] materialization reports missing module categories.
- [x] prop placement deterministic and traversal-safe.
- [x] runtime baked data contains no editor-only references.
- [x] save/reload retains profile, draft, accepted state, materialized marker,
  and baked data.

Manual checks:

- [ ] create starter setup from empty scene.
- [ ] generate several mockup seeds and inspect preview.
- [ ] edit room shapes and regenerate.
- [ ] swap style sets without changing abstract layout.
- [ ] materialize real project prefabs.
- [ ] save materialized prefab.
- [ ] bake runtime asset and load it in a runtime scene.
- [ ] intentionally break profile data and confirm diagnostics are actionable.

Suggested focused command:

```bash
"/home/inri/Unity/Hub/Editor/6000.4.8f1/Editor/Unity" \
  -batchmode -nographics \
  -projectPath "/home/inri/문서/UnityProjects/My project" \
  -runTests -testPlatform EditMode \
  -assemblyNames Conn.Tests \
  -testFilter Conn.Tests.EditMode.MapGenV2 \
  -testResults "Logs/MapGenV2OnlyEditModeTestResults.xml" \
  -logFile "Logs/MapGenV2OnlyEditModeTestRunner.log"
```

## Phase 16: Performance And Scale

Progress note 2026-05-31:

- `MapGenV2PerformanceProfile` defines small, medium, large, and stress target
  map sizes with generation, materialization, and preview memory budgets.
- Main window Generate, Repostprocess, Materialize, and Bake actions now show
  editor progress bars, record operation-level elapsed time and managed memory
  delta, and append samples to `Logs/MapGenV2Performance.log`.
- Mockup preview now caches a 1-pixel-per-cell texture keyed by draft signature,
  while hover/selection/grid/prop overlays remain dynamic.
- Performance log details now include validation issue counts/codes,
  retry/contradiction counts, post-process pass/rollback/change counts,
  materialization request/footprint counts, and baked payload counts.
- Focused tests already assert mockup generation does not create scene objects.
- `AssetDatabase.Refresh()` is centralized behind
  `MapGenV2AssetDatabasePolicy.RefreshAfterBulkAssetChanges()` so normal
  mockup iteration does not add ad-hoc refresh calls.
- Solver, post-process, materialize, and bake paths now accept cooperative
  cancellation hooks; the main window connects those hooks to cancelable editor
  progress bars.
- Focused tests cover budget classification, profiler sample logging, and
  preview texture cache invalidation.

Goal: make the generator usable for larger production maps without editor
freezes or unbounded memory growth.

Tasks:

- [x] Define target map sizes for this project:
  small, medium, large, stress.
- [x] Add generation time budget per target size.
- [x] Add materialization time budget per target size.
- [x] Add memory allocation budget for solver and preview.
- [x] Add cancellation support for long generation runs.
- [x] Add progress reporting for solve, post-process, materialize, and bake.
- [x] Avoid excessive `AssetDatabase.Refresh` calls during normal generation.
- [x] Cache preview textures and invalidate them only when draft data changes.
- [x] Avoid scene object creation during mockup-only iteration.
- [x] Add profiling hooks or logs for retries, contradictions, and pass costs.

Acceptance:

- Large profile generation does not make the editor appear hung without
  progress or cancellation.
- The designer can iterate mockups repeatedly without scene/object leaks.

Verification:

- Add timing checks for representative map sizes.
- Manual Unity check with repeated 50+ seed rerolls.

## Phase 17: Package Hygiene And Team Workflow

Goal: keep generated assets, sample assets, and production assets clearly
separated so the repository remains maintainable.

Progress note 2026-05-31:

- Starter setup output is now documented as local throwaway data rather than
  committed sample content.
- Shared sample and production folder conventions are documented in
  `doc/dev/map_generator/README.md`.
- Default generated draft, materialized prefab, baked map, and verification
  temp folders are ignored in `.gitignore`.
- Verification-created assets already use the deferred verification temp root
  and cleanup path; starter-generated assets can be removed from the menu or
  MapGenV2 window cleanup button.
- Naming, owner, and review checklist guidance is documented for shared
  profile/style/module/template assets.

Tasks:

- [x] Decide which starter assets are committed samples and which are
  user-generated throwaway assets.
- [x] Add folder conventions for:
  samples, project production profiles, generated drafts, generated baked maps,
  generated prefabs, temporary verification output.
- [x] Ensure verification-created assets are cleaned up automatically.
- [x] Add `.gitignore` rules or documentation for generated local outputs if
  needed.
- [x] Add naming convention:
  profile id, seed, draft id, style id, bake version.
- [x] Add ownership guidance for designers editing shared profiles and module
  sets.
- [x] Add review checklist for new profile/style/module assets.

Acceptance:

- Running the generator does not leave confusing untracked files unless the user
  intentionally creates production assets.
- New sessions can distinguish committed examples from local generated output.

Verification:

- Run starter setup and confirm which files are expected to remain untracked.
- Confirm verification runners clean up temporary assets.

## Phase 18: Compatibility And Integration

Goal: make MapGenV2 fit the existing Unity project instead of living as an
isolated demo tool.

Progress note 2026-05-31:

- Runtime scene flow can now receive promoted `MapGenBakedMapAsset` references
  through `SceneBootstrap.MapGenV2BakedMaps`.
- `CompiledMapDungeonRuntimeService` now selects a compatible baked MapGenV2
  map before runtime generation fallback and converts it through
  `MapGenV2CompiledMapAdapter`.
- The compiled adapter exposes existing runtime contracts for region records,
  sockets/doors, props/interactables, spawn markers, objective markers, and
  start/boss/exit anchors.
- `MapGenRuntimeMapService` remains the direct baked-map query API for systems
  that do not need legacy `CompiledMap` compatibility.
- `MapGenV2BuildValidation` adds a menu/build-like validation path that checks
  runtime/core sources and asmdefs for editor-only dependencies.
- The integration contract and source-controlled scene guidance are documented
  in `doc/dev/map_generator/README.md`.

Tasks:

- [x] Decide how generated maps are referenced by existing scene flow.
- [x] Decide how combat/content systems find spawn markers, objectives, doors,
  interactables, and region metadata.
- [x] Add integration contract for runtime map loading.
- [x] Add optional adapter for existing dungeon runtime services.
- [x] Add build-time validation that production scenes do not depend on editor
  types.
- [x] Add render-pipeline-neutral starter materials where possible.
- [x] Confirm generated prefabs survive player build serialization.
- [x] Confirm generated output can be used in source-controlled scenes without
  hidden editor-only dependencies.

Acceptance:

- A baked MapGenV2 map can be referenced by game runtime code through a clear
  API.
- Generated authoring assets do not break player builds.

Verification:

- Runtime smoke scene or test loads a baked MapGenV2 map.
- Build or build-like validation confirms no editor-only references.

## Missing Items Found During Document Review

The previous remaining-work document covered immediate UX gaps but missed these
production-critical areas:

- module bounds/chunk size contract for real prefabs
- compiled module/template database or cache before generation
- door connector/blocker prefab authoring for used and unused openings
- connection tag allow/deny rules beyond socket ids
- explicit room template asset
- explicit corridor template asset
- generated mockup room/corridor region selection and editing
- preservation of manual mockup edits through materialization and bake
- connector compatibility model
- room/corridor pool and quantity rule models
- post-process pass architecture and rollback
- prop placement rules and distributions
- navigation adapters and runtime query APIs
- output settings and overwrite policy
- stale detection and versioning
- diagnostics severity/context model
- WFC candidate/entropy/contradiction diagnostics for designer debugging
- explicit mockup override records instead of loose grid mutations
- save/reload and migration strategy
- production documentation and troubleshooting
- separation of MapGenV2 test gates from legacy map generator failures
- performance budgets and cancellation
- optional sector/streaming partition metadata for large maps
- package hygiene for generated local assets
- integration contracts with existing runtime systems

These are now included above as required completion work.

See also:

- `mapgenv2_external_reference_review.md`

## Recommended Commit Order For Next Session

1. mockup preview panel and draft summary
2. workflow status/next-action UI
3. starter visuals and generated root marker
4. scene output controls
5. authoring controls in main window
6. room/corridor template assets and connector model
7. production solver domain and required room placement
8. post-process pass system
9. real module stamping
10. prop placement rules
11. runtime bake expansion and adapters
12. diagnostics and validation
13. persistence/stale detection/versioning
14. docs and focused regression tests
15. performance and scale checks
16. package hygiene and team workflow rules
17. runtime/project integration adapters

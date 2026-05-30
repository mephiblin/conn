# MapGenV2 Remaining Work

Date: 2026-05-31
Status: follow-up checklist after hands-on starter workflow review.

## Current Reality

MapGenV2 can now create a starter profile setup, generate a mockup draft, accept
it, materialize placeholder prefabs into the open scene, and bake runtime map
data.

This is still not the intended production authoring experience. The visible
result after `Materialize` is only gray placeholder geometry. `Generate Mockup`
does not show an obvious mockup preview in the main window, so users can think
nothing happened. The workflow works technically, but it does not yet feel like
a usable MoraMapGen-style map production tool.

## Immediate UX Gaps

- `Generate Mockup` changes draft data but does not visibly show the blue/red/
  black mockup layout in the `MapGenV2` window.
- The main window does not show the current generation state clearly:
  no generated/accepted/stale labels, no signature, no used cell count, no room
  count, no materialized output summary.
- `Materialize` creates scene objects, but the starter prefabs are unstyled gray
  primitives.
- The starter output does not visually distinguish rooms, corridors, walls,
  doors, ceilings, and props well enough.
- The user cannot easily randomize the seed from the main window.
- There is no clear “next action” guidance after each step.
- There is no scene focus/select button for the latest materialized root.
- There is no direct way to save the materialized output as a prefab from the
  main window.
- The window does not expose enough profile/rule controls for normal iteration.

## Phase 1: Make Mockup Generation Visible

Tasks:

- [ ] Add a `MapGenV2` window mockup preview panel.
- [ ] Draw the draft grid directly in the window after `Generate Mockup`.
- [ ] Use the intended color language:
  blue = empty/base grid, red = room, black = corridor/connector,
  gray = blocked/reserved.
- [ ] Show grid coordinates on hover or selection.
- [ ] Show state labels for selected cells.
- [ ] Show draft summary:
  seed, grid size, signature, room cells, corridor cells, accepted/stale state.
- [ ] Add `Randomize Seed + Generate` button.
- [ ] Keep `Generate Mockup` as a mockup-only action; it must not create scene
  objects.

Acceptance:

- Pressing `Generate Mockup` produces an immediately visible mockup preview in
  the window.
- A user can understand the generated layout before pressing `Accept Mockup`.
- Same seed/profile still produces the same signature.

## Phase 2: Improve Step Guidance

Tasks:

- [ ] Add a workflow status strip:
  `Setup -> Generate Mockup -> Post-Process -> Accept -> Materialize -> Bake`.
- [ ] Highlight the next recommended button.
- [ ] Disable or explain invalid actions with visible reasons.
- [ ] Rename ambiguous buttons if needed:
  `Accept Mockup`, `Materialize To Scene`, `Bake Runtime Asset`.
- [ ] Show the output path after bake.
- [ ] Show the latest materialized root name after materialization.

Acceptance:

- A first-time user can follow the window without asking which scene or button
  is next.
- The UI clearly explains why `Materialize` and `Bake Runtime` are disabled.

## Phase 3: Better Starter Visuals

Tasks:

- [ ] Replace gray starter placeholder materials with color-coded materials.
- [ ] Starter floor/corridor/wall/door/prop prefabs must be visually distinct.
- [ ] Scale starter modules so generated maps are readable in Scene view.
- [ ] Add names to materialized child objects that include category and grid
  coordinate.
- [ ] Add a generated root component or metadata marker for latest output.

Acceptance:

- Starter materialization makes it visually clear which pieces are rooms,
  corridors, walls, doors, and props.
- The output is still placeholder content, but it communicates structure.

## Phase 4: Scene Output Controls

Tasks:

- [ ] Add `Select Materialized Root` button.
- [ ] Add `Frame Materialized Root` action if possible.
- [ ] Add `Clear Previous Materialization` option.
- [ ] Add `Save Materialized As Prefab` button.
- [ ] Add configurable output folder for scene/prefab output.
- [ ] Avoid silently creating duplicate roots unless the user requests it.

Acceptance:

- A user can find, inspect, replace, and save the generated scene output from
  the MapGenV2 window.

## Phase 5: Authoring Controls In Main Window

Tasks:

- [ ] Expose profile map size and seed in the main window.
- [ ] Expose required room categories in a compact editor.
- [ ] Expose post-process toggles:
  direct route, reduce dead ends, remove small rooms.
- [ ] Show linked `StyleSet`, `ModuleSet`, `RuleSet`, and `RoomShape` assets
  with quick select buttons.
- [ ] Add a pool summary for module categories and missing prefab warnings.
- [ ] Add room shape summary and validation warnings.

Acceptance:

- Basic iteration does not require constantly jumping between raw inspector
  arrays.
- Missing or invalid authoring data is visible from the main window.

## Phase 6: Production-Grade Layout Quality

Tasks:

- [ ] Replace the current line-connected room MVP with a richer room/corridor
  solver.
- [ ] Support room area shapes from `MapGenRoomShapeAsset`, not just single-cell
  room landmarks.
- [ ] Support wider corridors and door connector cells.
- [ ] Add loop creation and dead-end controls that visibly affect the mockup.
- [ ] Add blocked/excluded regions to the profile.
- [ ] Prevent rooms from collapsing into visually meaningless thin paths.

Acceptance:

- Generated mockups look like intentional room/corridor layouts, not just a
  connected line of cells.
- Different seeds produce meaningfully different but usable layouts.

## Phase 7: Real Module Stamping

Tasks:

- [ ] Stamp floors, corridors, walls, corners, ceilings, and doors based on
  neighbor analysis with correct transforms.
- [ ] Align module pivots and dimensions using `CellSize`.
- [ ] Add deterministic weighted prefab choice per module request.
- [ ] Support door modules at connector boundaries.
- [ ] Add prop placement pass using actual prop channels.
- [ ] Validate that generated blockers do not break traversal.

Acceptance:

- Materialized output can be used as a readable prototype level with project
  prefabs.
- Runtime bake data matches the materialized structure.

## Phase 8: Save/Reload And Regression Verification

Tasks:

- [ ] Add tests for starter setup creation.
- [ ] Add tests for main-window workflow state after each action.
- [ ] Add tests for mockup preview signatures.
- [ ] Add tests for materialized root replacement behavior.
- [ ] Add tests for prefab save output.
- [ ] Add a focused MapGenV2-only Unity Test Runner command to the document.
- [ ] Keep legacy map generator tests isolated from MapGenV2 completion gates.

Acceptance:

- MapGenV2-specific tests pass independently of quarantined legacy generator
  tests.
- Future UI changes cannot regress the starter workflow silently.

## Non-Goals For This Follow-Up

- Do not revive legacy `MapGeneratorWorkspace`.
- Do not make graph/node editing the primary workflow.
- Do not hide profile requirements behind hardcoded chapter data.
- Do not treat placeholder starter prefabs as final art.
- Do not mark the tool production-ready until the mockup preview and scene
  output controls are usable by a first-time user.

## Recommended Next Commit Order

- mockup preview panel and draft summary
- workflow status/next-action UI
- starter visual prefab/material improvement
- scene output controls
- profile/rule controls in main window
- solver quality pass
- module stamping pass
- focused regression tests and updated usage docs

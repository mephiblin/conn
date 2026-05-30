# Map Generator

Status: MapGenV2 production workflow in progress.

The previous map generator is quarantined as legacy tooling under
`doc/dev/map_generator/legacy/`. Legacy tests and legacy editor behavior are
not completion gates for MapGenV2 unless a task explicitly says so.

Primary implementation tracker:

- [MapGenV2 Remaining Work](mapgenv2_remaining_work.md)
- [MapGenV2 Goal Execution Guide](mapgenv2_goal_execution_guide.md)
- [New Map Generator Plan](new_map_generator_plan.md)
- [MapGenV2 External Reference Review](mapgenv2_external_reference_review.md)

## Quick User Guide

Open `Conn > MapGenV2 > Open Window`.

1. Click `Create Starter Setup`.
2. Confirm the generated `Profile` and `Draft` are assigned in the MapGenV2
   window.
3. Click `Generate Mockup / 목업 생성`.
4. Inspect the preview. Rooms are blue, corridors are red, blocked cells are
   black, and empty cells are gray.
5. Optional: click a room/corridor region to inspect, lock, edit metadata, or
   regenerate that region.
6. Click `Repostprocess Mockup / 후처리 재실행` when post-process rules should
   be applied.
7. Click `Accept Mockup / 목업 수락`.
8. In `Scene Output`, choose the overwrite policy:
   `CreateUnique`, `ReplacePrevious`, or `UpdateSelected`.
9. Click `Materialize To Scene / 씬 생성`.
10. Click `Bake Runtime Asset / 런타임 베이크`.

The accepted mockup signature is the source of materialization and runtime
bake. Materialization does not rerun the solver.

## Production Authoring

Create these assets for a real map style:

- `MapGenModuleSetAsset`: assign real prefabs per module category. At minimum,
  provide floor and straight wall coverage. Use category coverage diagnostics to
  find missing prefabs before materialization.
- `MapGenRoomShapeAsset`: author reusable room footprints with connector cells
  on valid edges.
- `MapGenRoomTemplateAsset`: define footprint, floor/wall/blocked/door hint
  cells, connectors, prop channels, source shapes, and weight.
- `MapGenCorridorTemplateAsset`: define corridor kind, width, turn kind, length
  range, connectors, prop channels, and weight.
- `MapGenRuleSetAsset`: configure room quantity, required categories,
  start-exit distance, post-process rules, and prop placement rules.
- `MapGenStyleSetAsset`: connect the module set, room templates, and corridor
  templates.
- `MapGenProfileAsset`: connect style, rules, room shapes, output folders,
  overwrite policy, and navigation adapter settings.

Prefab module rules:

- Keep prefab roots aligned to the module bounds contract.
- Use one-cell footprints for edge-sensitive categories unless the footprint
  diagnostics are expected.
- Use stable names and categories so materialization diagnostics can report
  missing coverage clearly.

## Troubleshooting

- `Profile has no style set`: assign a `MapGenStyleSetAsset`.
- `Style set has no module set`: assign a `MapGenModuleSetAsset`.
- `Materialization has no prefab coverage`: add a prefab to the named module
  category.
- `Generated mockup is stale`: source profile/style/rule/template/shape data
  changed. Run `Regenerate Mockup / 목업 재생성`.
- `Accepted signature is stale`: draft cells changed after accept. Run
  `Reaccept Mockup / 목업 재수락`.
- `Materialized output is stale`: accepted draft or module set changed. Run
  `Rematerialize To Scene / 씬 재생성`.
- `Baked runtime asset source signature does not match`: run
  `Rebake Runtime Asset / 런타임 재베이크`.
- `UpdateSelected` does nothing: assign a scene `Materialized Root`; this policy
  will not create a new root implicitly.

## Visual Examples

Mockup preview color language:

- Blue: room cell.
- Red: corridor cell.
- Black: blocked cell.
- Gray: empty cell.
- Highlight outline: hovered or selected region.

Materialized hierarchy shape:

```text
MapGenV2_<profileId>_<seed>
  Floors
  Corridors
  Walls
  Ceilings
  Doors
  Props
  Navigation
  Debug
```

The root has `MapGenV2GeneratedMapMarker`. Stamped prefabs have
`MapGenV2MaterializedModuleMarker` so source draft, module set, region,
template, category, direction, prefab name, and cell coordinate can be traced.

## Glossary

- Profile: top-level asset that links style, rules, room shapes, output folders,
  overwrite policy, and runtime adapter settings.
- Style set: visual/template set for a profile.
- Module set: prefab pools used during materialization.
- Room shape: reusable editable room footprint.
- Room template: production room/cell layout candidate with connectors and prop
  channels.
- Corridor template: production corridor candidate with connectors and length
  rules.
- Connector: edge socket that allows room/corridor template compatibility.
- Draft: editable mockup grid generated from a profile and seed.
- Materialization: scene/prefab stamping from an accepted draft.
- Bake: runtime-safe asset generation from an accepted draft.

## Verification Commands

Focused MapGenV2 EditMode tests:

```bash
"/home/inri/Unity/Hub/Editor/6000.4.8f1/Editor/Unity" -batchmode -nographics -projectPath "/home/inri/문서/UnityProjects/My project" -runTests -testPlatform EditMode -assemblyNames Conn.Tests -testFilter Conn.Tests.EditMode.MapGenV2 -testResults "Logs/MapGenV2OnlyEditModeTestResults.xml" -logFile "Logs/MapGenV2OnlyEditModeTestRunner.log"
```

Manual verification menu items:

- `Conn > MapGenV2 > Run Transient Manual Verification`
- `Conn > MapGenV2 > Run Starter Setup Batch Verification`
- `Conn > MapGenV2 > Run Deferred Verification`

Generated starter assets can be removed with:

- `Conn > MapGenV2 > Cleanup Starter Generated Assets`
- MapGenV2 window button: `Cleanup Starter Generated Assets`

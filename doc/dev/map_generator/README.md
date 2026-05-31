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

Open `Conn > MapGenV2 > Map Generator`.

When the language selector is `Auto` on a Korean editor, the window shows Korean
button names. The English names below are only the equivalent names in English
mode.

1. In `드래프트 파일 / Draft File`, click `새 드래프트 생성`
   (`Create Draft`) or assign a draft to `임포트 항목 / Import` and click
   `드래프트 임포트` (`Import Draft`).
2. In `맵 에셋 / Map Assets`, assign the room floor, corridor floor, wall,
   corner, ceiling, door, blocker, and prop prefabs needed by the map.
3. In `시드 / Seed`, edit the seed manually or click `랜덤 시드 입력`
   (`Fill Random Seed`), then click `지정한 시드로 생성`
   (`Generate From Seed`).
4. Inspect or edit the preview in `프리뷰 & 드로잉 / Preview & Drawing`.
   Empty cells are blue, rooms are red, corridors are black, and
   blocked/reserved cells are gray.
5. Use the drawing toolbar to select, paint room/corridor cells, erase cells, or
   mark blocked/reserved cells directly in the draft.
6. Click `드래프트 저장` (`Save Draft`) when the draft should become the current
   source for scene output and runtime bake.
7. Optional: in `출력 / Output`, choose the overwrite policy:
   `CreateUnique`, `ReplacePrevious`, or `UpdateSelected`.
8. Click `씬 생성` (`Materialize To Scene`), then `런타임 베이크`
   (`Bake Runtime Asset`).

The saved draft signature is the source of materialization and runtime bake.
Materialization does not rerun the solver.

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

## Package Hygiene And Team Workflow

Default `Create Starter Setup` output is local throwaway data. It is meant for
learning, verification, and quick iteration, not as committed sample content.
Use `Conn > MapGenV2 > Cleanup Starter Generated Assets` when a local starter
setup is no longer needed.

Committed MapGenV2 content should use explicit shared folders:

- Samples: `Assets/Conn/Authoring/MapGenV2/Samples/`
- Production profiles and style data:
  `Assets/Conn/Authoring/MapGenV2/Production/`
- Production subfolders:
  `Profiles`, `StyleSets`, `ModuleSets`, `RuleSets`, `RoomShapes`,
  `Templates`

Generated local output uses ignored folders by default:

- Drafts: `Assets/Conn/Authoring/MapGenV2/Drafts/`
- Materialized prefabs:
  `Assets/Conn/Authoring/MapGenV2/MaterializedPrefabs/`
- Runtime bake output: `Assets/Conn/Core/MapGenV2/BakedMaps/`
- Verification temp output:
  `Assets/Conn/Editor/MapGenV2/VerificationGenerated/`

If a generated draft, materialized prefab, or baked map becomes production
content, move it under the shared `Production` or `Samples` convention and
review it like a hand-authored asset. Do not force-add default generated
folders unless the team has intentionally promoted those files.

Naming convention:

- Profile id: stable lowercase project id, for example
  `ch2_first_slice_ruins`.
- Style id: stable visual/template style id, for example
  `ruins_standard`.
- Draft id: `<profileId>_<seed>_draft`.
- Materialized root: `MapGenV2_<profileId>_<seed>`.
- Runtime bake version: `<profileId>_<seed>_BakedMap_vNN` when keeping
  multiple promoted bake versions.

Ownership guidance:

- Each shared profile, style set, module set, and rule set should have a named
  designer owner in the asset description or adjacent notes.
- Module set changes require coverage validation because they affect every
  profile that references the module set.
- Draft/setup changes that alter saved draft maps should be treated as layout
  changes and reviewed with before/after preview screenshots.

Review checklist for shared MapGenV2 assets:

- Profile graph validates without errors or missing references.
- Template pools cover required room/corridor categories.
- Module set coverage includes floors, walls, doors, blockers, props, and any
  style-specific categories used by templates.
- Connector, blocker, and prop-channel diagnostics have no unexpected warnings.
- Focused MapGenV2 EditMode tests pass for code changes; asset-only changes
  include a manual generate, save draft, materialize, and bake smoke check.
- No ignored local generated output is staged unless it was intentionally moved
  to `Samples` or `Production`.

## Runtime Integration

Runtime code should reference `MapGenBakedMapAsset`, not editor drafts,
profiles, module sets, or scene marker components. Use these contracts:

- Direct baked-map query: add `MapGenRuntimeMapService` to a runtime object and
  assign a `MapGenBakedMapAsset`; runtime systems can read cells, regions,
  connectors, props, markers, traversal edges, and path queries through
  `MapGenRuntimeMapQuery`.
- Existing dungeon scene flow: assign promoted baked maps to
  `SceneBootstrap.MapGenV2BakedMaps`. `CompiledMapDungeonRuntimeService`
  selects the matching profile/seed and converts it through
  `MapGenV2CompiledMapAdapter`.
- Existing combat/content systems: use the compiled map adapter output for
  start, boss, exit, monster spawn, objective, prop/interactable, socket/door,
  and region metadata.
- Build validation: run
  `Conn > MapGenV2 > Validate Runtime Build Compatibility` before promoting a
  production scene or baked map.

Runtime integration priority is:

1. Explicit `CompiledMapAsset` JSON already assigned to the scene.
2. Promoted `MapGenBakedMapAsset` in `SceneBootstrap.MapGenV2BakedMaps`.
3. Runtime map generation bundle.
4. Legacy generated fallback.

## Troubleshooting

- `Profile has no style set`: assign a `MapGenStyleSetAsset`.
- `Style set has no module set`: assign a `MapGenModuleSetAsset`.
- `Materialization has no prefab coverage`: add a prefab to the named module
  category.
- `Generated draft is stale`: source style/rule/template/shape data changed.
  Run `같은 시드 재생성` / `Regenerate Same Seed`.
- `Saved draft is stale`: preview cells changed after the last save. Run
  `드래프트 저장` / `Save Draft`.
- `Materialized output is stale`: saved draft or map asset slots changed. Run
  `Rematerialize To Scene / 씬 재생성`.
- `Baked runtime asset source signature does not match`: run
  `Rebake Runtime Asset / 런타임 재베이크`.
- `UpdateSelected` does nothing: assign a scene `Materialized Root`; this policy
  will not create a new root implicitly.

## Visual Examples

Mockup preview color language:

- Blue: empty cell.
- Red: room cell.
- Black: corridor cell.
- Gray: blocked or reserved cell.
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

- Profile: internal setup asset that links style, rules, room shapes, output
  folders, overwrite policy, and runtime adapter settings. Normal MapGenV2
  authoring should not start from this asset.
- Style set: visual/template set for a profile.
- Module set: prefab pools used during materialization.
- Room shape: reusable editable room footprint.
- Room template: production room/cell layout candidate with connectors and prop
  channels.
- Corridor template: production corridor candidate with connectors and length
  rules.
- Connector: edge socket that allows room/corridor template compatibility.
- Draft: primary user-facing map data asset. It stores seed, generated/edited
  preview cells, direct prefab slot references, and the saved source signature.
- Materialization: scene/prefab stamping from a saved draft.
- Bake: runtime-safe asset generation from a saved draft.

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

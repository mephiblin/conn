# MapGenV2 Goal Execution Guide

Date: 2026-05-31
Status: execution guide for running MapGenV2 work through `/goal` sessions.

## Purpose

Use this document when starting a new `/goal` for MapGenV2 work. The goal is to
avoid vague implementation sessions, over-testing, hallucinated UX claims, and
unfinished handoffs.

This guide defines how a goal should be written, how work should be scoped, when
it is considered done, and what inspection/verification is required before
closing the goal.

## Reference Basis

This guide follows these external principles:

- Scrum.org describes Definition of Done as the quality standard required for an
  increment to be considered done and usable for inspection.
- Unity Test Framework separates Edit Mode tests, which run in the Editor and
  can test editor extensions, from Play Mode tests, which exercise runtime game
  code.
- Unity Build Automation documentation treats Edit Mode and Play Mode test
  failures as build-quality gates when enabled.
- Unity Code Coverage documentation supports recording coverage for automated or
  manual testing, but MapGenV2 should not require broad coverage targets for
  every small goal unless the goal explicitly asks for it.

## Goal Writing Format

Every MapGenV2 `/goal` should be written in this structure:

```text
/goal
Objective:
  <one concrete result>

Source documents:
  - doc/dev/map_generator/mapgenv2_remaining_work.md:<phase or section>
  - optional supporting docs

Scope:
  - <included item 1>
  - <included item 2>

Out of scope:
  - <explicitly excluded item 1>
  - <explicitly excluded item 2>

Completion criteria:
  - <user-visible result>
  - <data/API result>
  - <documentation/checklist update>

Verification criteria:
  - <minimal automated check>
  - <manual Unity/editor check if required>
  - <what does not need to be tested>

Commit policy:
  - commit after completion
  - push after commit
```

If the user gives a short goal without this structure, infer the smallest
reasonable goal from `mapgenv2_remaining_work.md` and state the inferred scope
before editing files.

## Goal Size Rules

Prefer goals that finish one visible workflow slice.

Good goal examples:

- Add the mockup preview panel and draw the draft grid.
- Add selected-region click/hover state in the mockup preview.
- Add Korean labels for the main MapGenV2 window.
- Add generated root marker and select/frame/clear buttons.
- Add module bounds asset and validation.

Bad goal examples:

- Finish MapGenV2.
- Implement WFC.
- Make the editor good.
- Add all UX.
- Do all remaining validation.

If a requested goal is too broad, split it into sub-goals and execute only the
first coherent slice unless the user explicitly authorizes a longer run.

## Per-Goal Definition Of Done

A MapGenV2 goal is done only when all relevant items below are true:

- The requested user-facing behavior exists in Unity, not only in code comments.
- The implementation matches the referenced document section and does not add
  unrelated features.
- The editor state is understandable:
  labels, disabled reasons, output paths, and error/warning messages are clear.
- The generated asset/scene/runtime data has a stable source of truth.
- The result does not revive legacy `MapGeneratorWorkspace` as the main
  workflow.
- The result does not introduce hidden chapter-specific fallback data.
- The result does not rely on raw arrays as the primary user workflow.
- The result does not leak `UnityEditor`, `Conn.Editor`, or authoring-only
  references into runtime-safe data.
- The relevant checklist item or status note is updated.
- The work is committed and pushed if the goal requested commit/push or follows
  the existing MapGenV2 commit policy.

## Acceptance Criteria Template

Each implementation goal should include acceptance criteria in user language.

Use this form:

```text
Acceptance:
- When <precondition>, the user can <action> and sees <expected result>.
- When <invalid state>, the UI shows <specific reason> and blocks <unsafe action>.
- The result persists after <save/reload or regeneration condition>.
- Existing unrelated MapGenV2 workflows still work at the basic level.
```

Examples:

- When `Generate Mockup` is pressed, the draft grid appears immediately in the
  MapGenV2 window with blue/red/black/gray cells.
- When no valid profile is assigned, `Generate Mockup` is disabled and explains
  which profile field is missing.
- When a room region is locked, regenerating unlocked regions does not move or
  replace the locked region.
- When `Materialize To Scene` runs, the scene root is named predictably and can
  be selected/framed from the window.

## Verification Levels

Use the lightest verification level that proves the goal. Do not expand
verification beyond the goal.

### Level 0: Document-Only Goal

Use when changing only planning/reference documents.

Required checks:

- `git diff --check` for edited docs.
- Read the changed sections once for contradictions.

Not required:

- Unity tests.
- C# compile checks.
- Manual Unity execution.

### Level 1: Editor/Data Model Goal

Use when adding ScriptableObject fields, editor-only UI, validation structs, or
serialized data changes.

Required checks:

- C# compile or Unity editor compile if available.
- Focused EditMode tests if the changed code is testable without scene play.
- One manual inspector/window smoke check if the change affects visible editor
  UI.

Not required unless requested:

- Full PlayMode suite.
- Long seed sweeps.
- Build/player validation.

### Level 2: Solver/Generation Goal

Use when changing mockup generation, WFC/candidate logic, post-processing, or
determinism.

Required checks:

- Focused EditMode tests for deterministic output and failure cases touched by
  the goal.
- Small seed smoke check:
  same seed same signature, different seed different or explainable signature.
- Manual preview check if the output is visual.

Not required unless requested:

- Large map stress tests.
- Exhaustive seed sweeps.
- Runtime build validation.

### Level 3: Materialization/Scene Output Goal

Use when creating or changing scene objects, prefab stamping, generated roots,
or Scene View UX.

Required checks:

- Focused EditMode tests where possible for classification/metadata.
- Manual Unity scene smoke check:
  materialize, select/frame root, inspect hierarchy, clear/replace if touched.
- Confirm no duplicate roots are created unless requested.

Not required unless requested:

- NavMesh bake.
- Player build.
- Performance profiling.

### Level 4: Runtime/Bake/Navigation Goal

Use when changing runtime-safe baked data, runtime loaders, graph/grid queries,
or navigation integration.

Required checks:

- Focused EditMode or PlayMode tests depending on whether runtime behavior needs
  play mode.
- Confirm baked data contains no editor-only references.
- Basic runtime query smoke check for the touched query path.

Not required unless requested:

- Full game scene integration.
- Full build pipeline.
- Code coverage targets.

## Inspection Checklist Before Closing A Goal

Before marking a goal complete, check:

- Scope: only the requested MapGenV2 slice was changed.
- UX: user can see what changed and what the next action is.
- Localization: new user-facing strings have Korean-ready labels or localization
  keys if the touched area is localized.
- Data: saved assets retain source signatures or stale markers where relevant.
- Scene: generated objects are named/grouped predictably if scene output changed.
- Runtime: runtime-safe types do not reference editor-only assemblies.
- Docs: checklist/status reflects the implementation.
- Git: unrelated dirty files are not staged.
- Verification: required level-specific checks were run or explicitly reported
  as not run.

## Commit And Push Policy

For MapGenV2 goals:

- Commit only files that belong to the goal.
- Do not stage user-generated starter assets, temporary materialized prefabs, or
  unrelated scene changes unless the goal explicitly requires them.
- Commit message format:

```text
MapGenV2: <short result>
```

Examples:

- `MapGenV2: add visible mockup preview`
- `MapGenV2: add selected region editing`
- `MapGenV2: add Korean editor labels`
- `MapGenV2: add generated root marker`

- Push after each completed goal unless the user explicitly says not to push.
- If verification cannot be completed, still commit only if the user accepts the
  known gap or the change is documentation-only.

## Goal Closeout Format

Final response after a completed goal should include:

```text
완료 범위:
<1-3 lines>

검증:
<checks run, or not run with reason>

커밋:
<hash message>
```

For partial or blocked goals, use:

```text
진행 범위:
<what changed>

차단/미완료:
<specific blocker>

다음 작업:
<next concrete step>
```

## Recommended Goal Order

Use this order unless the user overrides it:

1. Visible mockup preview panel.
2. Workflow status strip and next-action UI.
3. Korean labels/help for the main window.
4. Scene output root marker and select/frame/clear controls.
5. Selected mockup region click/hover/edit state.
6. Room shape inspector grid UX.
7. Module bounds contract and validation.
8. Module/template compile cache.
9. Door connector/blocker materialization.
10. Production solver candidate domain.
11. Post-process pass system.
12. Prop placement rules.
13. Runtime bake/query adapters.
14. Scene View overlay and handles.
15. Larger-map performance and sector metadata.


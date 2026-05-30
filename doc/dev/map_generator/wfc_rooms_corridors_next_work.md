# WFC Rooms/Corridors Generator Next Work

Date: 2026-05-30
Status: execution checklist for rebuilding the map generator into a
MoraMapGen-style rooms/corridors production tool.

## Goal

Replace the current graph-first dungeon generator with an asset-pool and
constraint-driven rooms/corridors generator:

```text
MapProfileAsset
  -> explicit generation rules
  -> typed room/corridor asset pools
  -> WFC-style socket/adjacency constraints
  -> visual cell-map preview
  -> accepted EditableMapDraftAsset
  -> validation
  -> CompiledMapAsset
```

The user-facing result must feel like a production map generator, not a debug
graph preview.

## Current Baseline

- `MapGeneratorWorkspace` no longer uses hidden Chapter 2 fallback.
- `MapProfileAsset` is required before generation.
- `MapProfileAsset` has `Room Count Min/Max`.
- `MapGenerationService` clamps path/branch generation to the room count range.
- `MapProfileAuthoringSampleBuilder` can create explicit Chapter 2 profile and
  room chunk pool assets.
- `Generate Preview` creates a visual cell-map preview.
- `Accept Preview + Save Draft` saves the preview as `EditableMapDraftAsset`.
- `Bake + Save Compiled Map` saves runtime `CompiledMapAsset` from the accepted
  draft.

## Non-Negotiable Direction

- Do not reintroduce hidden hardcoded fallback profiles.
- Do not make node/wire graph editing the main workflow.
- Do not make users edit raw arrays as the primary production UX.
- Do not treat room/corridor assets as incidental debug data.
- Every generated map must come from a selected profile and explicit room pools.

## Phase 1: Explicit Room Pools

### Tasks

- [x] Add a typed room pool data structure, e.g. `MapRoomPoolRule`.
- [x] Pool rule fields:
  role, layout kind, min count, max count, weight, required flag, allowed chunks.
- [x] Add room pool rules to `MapProfileAsset`.
- [x] Keep legacy `OptionalChunks` only as a migration/debug bridge.
- [x] Update `MapProfileAuthoringSampleBuilder` to populate typed pools from
  the Chapter 2 sample chunks.
- [x] Show pool summary in `MapGeneratorWorkspace`.
- [x] Validate each required pool has at least one chunk.
- [x] Validate each pool chunk matches profile room size/theme.

### Acceptance

- A selected profile clearly shows which chunks can be used for Start, Main,
  Corridor, Hub, Side, DeadEnd, Quest, Boss, Exit, and HeightTransition.
- Generation no longer relies on broad string role tags alone.

## Phase 2: Constraint Model

### Tasks

- [x] Add explicit room socket definitions for each chunk side.
- [x] Add socket type/id, not only direction.
- [x] Add compatibility rules:
  same socket id, wildcard socket, blocked side, door side, corridor side.
- [x] Add validation for incompatible socket pairs.
- [x] Add generation failure report that names the missing socket/pool.

### Acceptance

- The generator can reject a profile before generation if no legal connection
  exists between required room categories.
- Error messages identify the exact missing pool/socket.

## Phase 3: WFC-Like Layout Solver

### Tasks

- [ ] Replace `MapGenerationService.Generate` path-first placement with a grid
  candidate solver.
- [ ] Candidate cell state includes allowed pool rules/chunks.
- [ ] Collapse cells by lowest entropy.
- [ ] Propagate socket constraints to neighbors.
- [ ] Enforce room count min/max.
- [ ] Enforce required rooms: start, quest, boss, exit.
- [ ] Enforce corridor connectivity.
- [ ] Retry with deterministic seed sequence when contradictions occur.

### Acceptance

- Different seeds produce meaningfully different layouts within the same
  profile.
- Generated output is still deterministic for the same profile and seed.
- Failure reports are actionable.

## Phase 4: Corridor And Room Stamping

### Tasks

- [ ] Treat corridors as first-class chunks/pools.
- [ ] Stamp selected room/corridor chunks into the draft grid.
- [ ] Prevent overlap between stamped chunks.
- [ ] Carve/merge door sockets between adjacent chunks.
- [ ] Support variable chunk footprints after fixed-size flow is stable.

### Acceptance

- Preview output is a visual cell-map, not graph boxes.
- Room/corridor asset cells are visibly used in generated maps.

## Phase 5: Workspace UX

### Tasks

- [ ] Replace raw profile arrays with a custom profile/pool editor.
- [ ] Add profile rule summary:
  room count, required rooms, pool count, socket coverage.
- [ ] Add `Validate Profile` button.
- [ ] Add `Generate Preview` only when profile validation passes.
- [ ] Add generation result summary:
  seed, rooms used, chunks selected, retry count, failure reason.

### Acceptance

- A designer can understand and fix generation setup from the Inspector.
- No hidden assumptions are required to generate a map.

## Phase 6: Tests And Verification

### Automated Tests

- [ ] Profile with missing start pool fails validation.
- [ ] Profile with incompatible sockets fails validation.
- [ ] Same seed/profile produces identical draft.
- [ ] Different seeds produce different layout signatures.
- [ ] Room count min/max is enforced.
- [ ] Required room roles are always present.
- [ ] Generated draft validates and bakes.
- [ ] Runtime compiled map has no Unity/editor references.

### Manual Unity Checks

- [ ] Create sample profile assets.
- [ ] Assign profile to `MapGeneratorWorkspace`.
- [ ] Validate profile.
- [ ] Generate preview.
- [ ] Randomize seed and confirm visible layout changes.
- [ ] Accept preview as draft.
- [ ] Edit draft.
- [ ] Validate draft.
- [ ] Bake compiled map.

## Cleanup Candidates

- Remove or quarantine old graph snapshot UI paths.
- Remove broad fallback catalog generation from production workflow.
- Keep `MapGenerationCatalog` only as sample-data source until all sample assets
  are checked in.
- Move debug preview helpers behind explicit debug labels.

## Commit Policy

Commit and push after each phase or meaningful subphase:

- data model changes
- validation changes
- solver changes
- workspace UX changes
- tests/docs updates

Each commit should update this checklist when relevant.

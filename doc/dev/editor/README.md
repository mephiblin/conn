# Unity Editor Direction

Date: 2026-05-25
Status: Inspector-first foundation and bridge implementation are in place; manual Play Mode verification remains before final Phase 6/8 closure.

## Problem Statement

The current `ContentDatabaseWindow` is useful as a bootstrap tool, but it is not
the final editor direction. It edits arrays inside one database asset and
therefore behaves like a primitive table/database editor. That is closer to a
web-game data admin panel than a Unity production tool.

This is structurally wrong for the final workflow because the project must author
Unity content:

- meshes
- FBX imports
- prefabs
- materials
- tile assets
- colliders
- nav/collision settings
- scene-view placements
- previewable objects
- reusable room/chunk assets

The final editor must use Unity's strengths:

- Inspector
- Custom Inspector
- ScriptableObject assets
- Prefab references
- Scene View handles/tools
- Project Browser asset organization
- Preview scenes
- Undo/Redo
- validation before build/export

## Correct Direction

The editor should be **Inspector-first and asset-first**.

`EditorWindow` should not be the main place where every field is edited. It
should primarily be a browser, workbench, validator, and build/export surface.

```text
Wrong final shape:
  One ContentDatabaseWindow directly edits every row in ContentDatabase.asset.

Correct final shape:
  Project assets and Inspectors are the authoring surface.
  EditorWindow coordinates selection, preview, validation, generation, and build.
```

## Warcraft III World Editor Benchmark

Warcraft III's editor is a useful benchmark because it separates authoring modes
clearly:

- terrain editor
- unit/object placement
- object data editor
- trigger editor
- import/resource manager
- test map flow

The important lesson is not to copy its UI literally. The lesson is that a game
editor is a set of specialized production tools around real game objects and
assets, not just a flat data table.

Project interpretation:

| Warcraft III editor idea | Unity project equivalent |
| --- | --- |
| Terrain editor | Map/Room/Chunk Scene View editor |
| Object editor | ScriptableObject inspectors for monsters, skills, items, NPCs |
| Unit palette | Placement palette using prefabs and DB references |
| Doodad/destructible palette | Decor/resource palette from prefabs/materials |
| Trigger editor | Event/Quest graph editor |
| Import manager | Unity asset importer conventions and resource set assets |
| Test map | controlled editor test play / validation scene |

## Working Principle

The editor should not modify the game during runtime play as the main workflow.
Editor-time authoring creates assets, validates them, and builds runtime data.
Runtime consumes validated assets.

```text
Authoring assets
  -> validation
  -> build/compile
  -> ContentDatabase / RuntimeMapGenerationBundle / optional CompiledMapAsset
  -> Play Mode or Player Runtime
```

## Current Tool Classification

| Tool | Current state | Final role |
| --- | --- | --- |
| `ContentDatabaseWindow` | bootstrap DB editor | content browser, import, validation, build surface |
| Monster tab | table-row stat editor | asset selector/browser for `MonsterDefinitionAsset` inspectors |
| Encounter tab | table-row encounter editor | encounter asset browser + preview + validation |
| Quest tab | table-row quest editor | quest/event graph entry + inspector asset selection |
| `GeneratorWorkbenchWindow` | hardcoded profile/seed tool | map generation workbench using profile/resource/chunk assets |
| `ContentDatabase.asset` | primary edited array store | compiled registry or build output from authored assets |

## New Documents

- `inspector_first_editor_architecture.md`
  - detailed target architecture
  - authoring asset model
  - Warcraft III benchmark adaptation
  - migration plan from DB editor to Unity editor

## Current Decision

Treat the current DB editor as a temporary bridge.

Do not continue expanding it as the final content authoring UX. Future editor
work should create typed authoring assets and custom inspectors, then compile
those assets into runtime databases.

## Current Next Gate

Automated editor, content, spawn/map, and runtime bundle validation is green in
the tracked checklist. The remaining closure gate is manual Unity Play Mode
verification for the Phase 6 three-quest sequence and Phase 8 Game view
checklist before changing the related `[!]` items to `[x]`.

## Current Implementation Note

The first Inspector-first foundation now lives in `Assets/Conn/Authoring` as a
separate `Conn.Authoring` assembly. It contains typed ScriptableObject source
assets for monsters, encounters, spawn tables, skills, NPCs, map profiles, map
resource sets, room chunks, landmark rooms, and generation weight profiles. The
map workbench can now pass seed, floor, and difficulty into the built runtime
generation bundle so authored spawn-table constraints are testable without
promoting `ContentDatabaseWindow` into the final production editor. The content
browser bridge also shows a first-pass `SpawnTableAsset` membership and
MapProfile usage preview, plus skill/NPC/vendor authoring asset browsing/bake
support, so spawn tables remain reusable sources instead of map-owned monster
lists and skills/NPCs/vendors can flow through the same build/validation bridge.

`ContentDatabaseWindow` remains in `Conn.Editor` and is still useful for legacy
JSON import, bootstrap DB edits, save, validation, and future build/export
bridging. It is not the final field-by-field production editor. Unity object
references such as prefabs, FBX/mesh sources, materials, tiles, audio, and VFX
belong in authoring assets; runtime/Core data receives stable ids and baked
runtime-safe fields.

The window now has an `Authoring` tab that discovers `MonsterDefinitionAsset`,
`EncounterDefinitionAsset`, and `SpawnTableAsset`, runs authoring validation,
and bakes authored monsters/encounters into `ContentDatabaseDefinition` by id.
The bake path upserts authored records rather than removing existing DB rows, so
legacy DB content and catalog fallbacks remain available while the new asset
route is proven.

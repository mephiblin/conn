# Diablo식 맵 생성과 에디터 데이터 설계

Date: 2026-05-24
Status: Unity redevelopment design note.

## 목적

이 문서는 Unity 재개발에서 사용할 Diablo식 던전 생성 방식과 에디터 데이터 구조를
정리한다. 목표는 Diablo II 원작 데이터를 복제하는 것이 아니라, 그 계열의
`profile -> room graph -> chunk/socket assembly -> tile/decor pass -> compiled map`
흐름을 이 프로젝트에 맞는 제작 파이프라인으로 재해석하는 것이다.

현재 웹 구현은 참조 구현이다. Unity에서는 DOM 기반 맵 에디터를 옮기지 않고,
Editor Tool이 생성 규칙과 콘텐츠 데이터를 저작/검증/빌드하고 Game Runtime이
검증된 compiled map만 읽도록 분리한다.

## 핵심 방향

- 완전 랜덤 셀 파기가 아니라 미리 저작한 방/청크를 규칙으로 조립한다.
- seed와 profile이 같으면 같은 던전 구조가 재현되어야 한다.
- 시작점, 열쇠, 잠긴 문, 보스, 계단, 퀘스트 방 같은 anchor는 생성 결과에 보장된다.
- critical path, side branch, loop, reward dead end, locked edge가 graph로 설명되어야 한다.
- chunk는 open side, door socket, local anchor, role tag를 가진다.
- tile/decor/light/encounter pass는 layout 뒤에 실행된다.
- 에디터는 cell painter가 아니라 generator workbench가 중심이다.

## Diablo식 생성 공식의 프로젝트 해석

원작 파일 구조를 그대로 쓰지는 않지만, 설계 대응은 다음처럼 잡는다.

| Diablo II 개념 | 프로젝트 개념 | Unity 에디터 데이터 |
| --- | --- | --- |
| `Levels.txt` | 층/지역 상위 정의 | `MapProfile` |
| `LvlMaze.txt` | room count, room size, merge chance | `MapProfile.layout`, `MapProfile.graphRules` |
| `LvlPrest.txt` / `.ds1` | 미리 만든 room 후보 | `ChunkPreset` |
| `LvlTypes.txt` / `Dt1Mask` | 테마별 타일셋 | `TileSetProfile`, material catalog |
| `LvlSub.txt` | 타일 치환/변주 | `TileSubstitutionRule` |
| `Themes`, `ObjGrp`, `ObjPrb` | 오브젝트/장식 확률 | `ObjectTheme` |
| `LvlWarp.txt` | 출입구/전이 규칙 | `TransitionAnchor` |

단순화한 생성 공식:

```text
mapProfile + seed
  -> roomGraph
  -> node socket mask
  -> chunk preset selection
  -> anchor placement
  -> tile adjacency/substitution pass
  -> decor/light/encounter/loot pass
  -> validation
  -> compiledMap
```

## 에디터 데이터 모델

### MapProfile

`MapProfile`은 한 층 또는 한 지역의 생성 문법이다. Unity에서는
ScriptableObject나 source JSON으로 관리할 수 있다.

필수 필드:

- `profileId`: 고유 ID
- `mapKind`: 뒤틀린 사원, 산호해안, 폐허 같은 지역 종류
- `theme`: tile/decor/material 선택 키
- `layout.width`, `layout.height`: 최종 grid 크기
- `gridRoomSize.width`, `gridRoomSize.height`: 청크 하나의 규격
- `targetModuleCount`: 목표 room/chunk 수
- `criticalPath.min/max`: 시작점에서 보스/출구까지의 주요 경로 길이
- `sideBranchCount`: 보상/위험 곁가지 수
- `loopCount.min/max`: 추가 연결 또는 merge edge 목표
- `mergeChancePer1000`: 인접 room 추가 연결 확률
- `lockedDoorKeyId`: 잠긴 문에 대응하는 열쇠 item id
- `requiredAnchors`: 반드시 배치되어야 하는 anchor 목록

현재 참조 데이터:

- [`webgame_ref/src/data/map_profiles.json`](../webgame_ref/src/data/map_profiles.json)
- [`webgame_ref/src/mapGenerationData.js`](../webgame_ref/src/mapGenerationData.js)

### ChunkPreset

`ChunkPreset`은 Diablo식 room 조각이다. 단순 셀 묶음이 아니라 socket과 anchor를
가진 제작 단위여야 한다.

필수 필드:

- `id`: 고유 chunk id
- `presetId`: 실제 cell/prefab 모양 참조
- `theme`: 사용할 수 있는 테마
- `size`: profile의 `gridRoomSize`와 호환되는 크기
- `openSides`: 연결 가능한 방향
- `doorSockets`: 문 또는 통로가 열릴 수 있는 방향
- `anchors`: 내부 배치 지점 목록
- `variantGroup`: 반복률 검사용 그룹
- `roleTags`: start, key, boss, combat, guard, side_reward 같은 역할 호환성

anchor 예:

```json
{
  "id": "altar",
  "kind": "boss_spawn",
  "x": 2,
  "y": 4
}
```

현재 참조 데이터:

- [`webgame_ref/src/data/map_chunks.json`](../webgame_ref/src/data/map_chunks.json)
- [`webgame_ref/src/presets.js`](../webgame_ref/src/presets.js)

### RoomGraph

`RoomGraph`는 생성 결과의 설계 의도를 보존하는 중간 산출물이다. Runtime map보다
상위 데이터이며, Editor Tool의 분석과 검증에 반드시 필요하다.

필수 정보:

- nodes: room id, grid 좌표, role, branch depth, path index, socket mask
- edges: main, branch, merge, locked edge
- criticalPath: start부터 boss/exit까지의 노드 목록
- sideBranches: side/reward/dead end 노드 목록
- lockedEdge: key 이후 열려야 하는 진행 edge

현재 참조 구현:

- [`webgame_ref/src/mapGraph.js`](../webgame_ref/src/mapGraph.js)

### TileSubstitutionRule

layout이 끝난 뒤 tile role과 theme에 따라 바닥/벽/장식 재질을 변주하는 규칙이다.
Diablo식 시각 밀도는 이 pass가 없으면 나오기 어렵다.

필수 필드:

- `id`
- `theme`
- `target`: floor, wall, ceiling, decor 등
- `whenTileRoles`: room, corridor, junction, corner, end_cap 등
- `variants`: material id, weight, tag

현재 참조 데이터:

- [`webgame_ref/src/data/tile_substitutions.json`](../webgame_ref/src/data/tile_substitutions.json)

### ObjectTheme

방과 복도에 torch, barrel, crate, bones, altar, urn, pillar 같은 장식을 뿌리는
규칙이다. 전투/보상/퀘스트 placement와 달리 시각 밀도와 분위기를 담당한다.

필수 필드:

- `id`
- `theme`
- `decor[]`: kind, tileRoles, weight, maxPerMap, visual/material hints

현재 참조 데이터:

- [`webgame_ref/src/data/object_themes.json`](../webgame_ref/src/data/object_themes.json)

## 생성 단계

### 1. Profile 선택

새 원정, 층 이동, 테스트 플레이는 먼저 `MapProfile`을 선택한다. 선택 기준은
맵 종류, 층, 퀘스트, 난이도, 테스트 시드다.

Game Runtime은 profile을 직접 편집하지 않는다. Runtime은 Editor Tool이 빌드한
profile/content bundle을 읽기만 한다.

### 2. RoomGraph 생성

profile과 seed로 room graph를 만든다.

현재 구현은 [`webgame_ref/src/mapGraph.js`](../webgame_ref/src/mapGraph.js)의
`buildRoomGraph()`가 다음 개념을 만든다.

- critical path
- side branch
- merge edge
- start/key/locked_gate/boss/combat role
- node socket mask

Unity에서는 이를 pure C# domain service로 옮긴다. MonoBehaviour나 Scene 오브젝트에
직접 생성 규칙을 흩뿌리지 않는다.

### 3. Chunk 선택

각 graph node의 socket mask, role, theme를 기준으로 호환되는 `ChunkPreset`을 고른다.

선택 규칙:

- chunk theme가 profile theme와 맞아야 한다.
- node socket mask를 chunk openSides가 만족해야 한다.
- node role이 chunk roleTags에 포함되어야 한다.
- 같은 `variantGroup`이 과도하게 반복되면 경고한다.
- 실패 시 fallback chunk를 고르되 validation warning을 남긴다.

현재 참조:

- [`webgame_ref/src/mapGenerationData.js`](../webgame_ref/src/mapGenerationData.js)
- [`webgame_ref/src/mapGeneration.js`](../webgame_ref/src/mapGeneration.js)

### 4. Chunk 배치와 anchor 배치

선택된 chunk를 room grid 위치에 배치한다. chunk 내부 anchor에 따라 다음 placement를
찍는다.

- start
- stairs up/down
- final stairs
- boss spawn
- key item
- locked door
- quest event
- NPC
- loot cache
- trap
- shrine/camp/rest site

이 단계의 결과는 아직 최종 runtime map이 아니라 `generated floor draft`다.

2026-05-25 구현 상태:

- `MapPlacementKind`는 start, quest target, boss, exit, monster, loot까지 포함한다.
- Chapter 2 first slice profile은 위 6종 anchor를 required contract로 검증한다.
- main path room의 monster anchor와 side branch room의 loot anchor는 graph role을 보고 배치한다.
- Runtime은 저장된 compiledMap asset을 우선 읽고, quest target과 monster placement를 field monster state로 등록한다.

### 5. Tile adjacency pass

walkable/blocked만으로 벽을 만드는 것이 아니라 tile role을 계산한다.

필요 role:

- room
- corridor
- junction
- intersection
- corner
- end_cap
- doorway
- locked_gate
- secret_spur

이 role을 기반으로 wall/corner/doorframe/pillar/material variation을 선택한다.
현재 웹 구현은 이 부분이 약하며, Unity 재개발에서 반드시 강화해야 한다.

### 6. Substitution/decor/light pass

`TileSubstitutionRule`과 `ObjectTheme`을 적용한다.

목표:

- 반복되는 바닥/벽을 줄인다.
- 보상 dead end와 위험 방의 분위기를 다르게 만든다.
- torch, bones, urn, pillar 같은 props를 room role에 맞춰 배치한다.
- 조명은 theme와 room role에 맞게 자동 생성하되 에디터에서 override할 수 있다.

### 7. Encounter/loot/quest pass

전투와 보상은 layout 뒤에 배치한다. 중요한 점은 아무 방에나 몬스터를 뿌리는 것이
아니라 graph 의미를 읽어 배치해야 한다는 것이다.

권장 규칙:

- critical path에는 필수 전투와 gate를 배치한다.
- side branch에는 보상, 함정, optional elite를 배치한다.
- dead end는 loot cache 또는 quest event 후보가 된다.
- boss role node에는 boss encounter와 출구/봉인을 배치한다.
- key role node에는 locked edge 이전에 접근 가능한 key item을 보장한다.

현재 1차 구현은 main path의 monster placement와 side branch의 loot placement까지만
자동 생성한다. boss/loot 보상 지급, optional elite, trap, key/locked edge는 후속
contract로 남긴다.

### 8. Validation

빌드 전 검증은 생성기 품질의 핵심이다.

필수 검증:

- start에서 모든 required anchor에 도달 가능
- key가 locked door보다 먼저 접근 가능
- boss와 exit가 critical path 뒤쪽에 존재
- chunk socket 실패 없음
- required role에 맞는 anchor 존재
- variantGroup 반복률 과다 경고
- placement 참조 ID 유효
- material/decor/item/monster/event/NPC 참조 ID 유효

현재 참조:

- [`webgame_ref/src/mapCompiler.js`](../webgame_ref/src/mapCompiler.js)

옛 `doc/planning/systems/data-contracts/README.md` 문서는 현재 저장소에 없다. 데이터
계약은 [`unity_game_concept_design.md`](unity_game_concept_design.md)와 이 문서의
`compiledMap` 기준으로 새로 정의한다.

### 9. Compile

Editor Tool은 검증된 draft를 Game Runtime용 `compiledMap`으로 변환한다.

Runtime compiled map에는 다음만 들어간다.

- grid cells
- walls/doors
- rooms
- placements
- lights/decor
- material ids
- battle background ids
- generation metadata

Editor-only 데이터, graph editing state, temporary selection, UI state는 들어가지 않는다.

## Editor Tool 화면 구성

Unity Editor Tool은 최소한 다음 창으로 나눈다.

### Generator Workbench

- profile 선택
- seed 입력/무작위 생성
- preview 생성
- graph overlay
- critical path/loop/branch summary
- validation summary
- compiled map build 버튼

### Profile Editor

- room count
- room size
- merge chance
- critical path length
- loop/side branch budget
- required anchors
- theme/material set
- locked door key id

### Chunk Catalog Editor

- chunk/prefab 선택
- open side 설정
- door socket 설정
- local anchor 편집
- roleTags 편집
- variantGroup 관리
- socket fit test

### Tile/Decor Rule Editor

- tile role별 material variant
- substitution weight
- decor kind/weight/maxPerMap
- light rule
- missing material/prop validation

### Build & Validation

- active profile validation
- chunk catalog validation
- generated preview validation
- content reference validation
- compiledMap export
- contentBuildManifest export
- test play session 실행

## Game Runtime 소비 방식

Game Runtime은 생성기 에디터를 포함하지 않는다. Runtime은 다음 중 하나만 소비한다.

- Editor Tool에서 빌드된 `compiledMap`
- content bundle manifest가 가리키는 compiled floor bundle
- debug 빌드에서만 허용되는 generated fallback

릴리즈 Runtime에서 profile/chunk를 즉석 편집하거나, EditorWindow 상태를 읽어서는
안 된다.

## 현재 웹 구현 참조 목록

- [`webgame_ref/src/mapGeneration.js`](../webgame_ref/src/mapGeneration.js): map 생성 본체, profile merge, chunk 배치, placement/light/decor pass 참고
- [`webgame_ref/src/mapGenerationData.js`](../webgame_ref/src/mapGenerationData.js): profile/chunk catalog loading, chunk fit/selection 참고
- [`webgame_ref/src/mapGraph.js`](../webgame_ref/src/mapGraph.js): critical path, branch, merge edge, socket mask 생성 참고
- [`webgame_ref/src/mapCompiler.js`](../webgame_ref/src/mapCompiler.js): compiledMap 변환과 validation 참고
- [`webgame_ref/src/editorMapEditing.js`](../webgame_ref/src/editorMapEditing.js): legacy cell editing와 보정 UX 참고
- [`webgame_ref/src/renderEditor.js`](../webgame_ref/src/renderEditor.js): Generator Workbench/Legacy Cell Editor 화면 구조 참고
- [`webgame_ref/src/data/map_profiles.json`](../webgame_ref/src/data/map_profiles.json): 현재 profile 데이터
- [`webgame_ref/src/data/map_chunks.json`](../webgame_ref/src/data/map_chunks.json): 현재 chunk/socket/anchor 데이터
- [`webgame_ref/src/data/tile_substitutions.json`](../webgame_ref/src/data/tile_substitutions.json): tile variation 데이터
- [`webgame_ref/src/data/object_themes.json`](../webgame_ref/src/data/object_themes.json): decor scatter 데이터

옛 `room-tile-editor/diablo-map-editor-generation-gap-report.md` 격차 분석 문서는 현재
저장소에 없다. 해당 취지는 이 문서의 생성 단계와 첫 구현 순서에 반영한다.

## Unity 구현 권장 구조

```text
Conn.Core
  MapProfile
  ChunkPreset
  RoomGraph
  MapGenerationRules
  Validators

Conn.Editor
  GeneratorWorkbenchWindow
  ProfileEditorWindow
  ChunkCatalogWindow
  TileDecorRuleWindow
  BuildValidationWindow
  JsonImporter
  CompiledMapBuilder

Conn.Runtime
  CompiledMapLoader
  GridRuntime
  PlacementRuntime
  DungeonSession

Conn.Rendering
  GridSceneBuilder
  MaterialResolver
  DecorSpawner
```

원칙:

- 생성 규칙은 `Conn.Core` pure C#에 둔다.
- Unity EditorWindow는 Core service를 호출해 결과를 보여준다.
- Runtime은 `CompiledMap`만 로드한다.
- generated ScriptableObject cache는 Editor가 만들고 Runtime은 읽기만 한다.

## 비목표

- Diablo II 원본 `.ds1`, `.dt1`, `.txt` 파일을 직접 읽는 것
- 원작 맵/타일/배치를 복제하는 것
- Game Runtime에 generator editor를 넣는 것
- 셀을 직접 칠하는 legacy editor를 새 구조의 중심에 두는 것
- 검증 실패한 map을 릴리즈 content bundle에 넣는 것

## 첫 구현 순서

1. `MapProfile`과 `ChunkPreset` ScriptableObject 또는 JSON import를 만든다.
2. `RoomGraph` 생성기를 pure C#으로 만든다.
3. socket mask와 role tag로 chunk를 고르는 selector를 만든다.
4. generated preview를 EditorWindow에서 본다.
5. start/key/locked door/boss/stairs anchor를 배치한다.
6. reachability와 key-before-lock 검증을 붙인다.
7. compiledMap을 Runtime scene builder가 읽게 한다.
8. tile substitution/decor/light pass를 붙인다.
9. legacy cell override는 compiledMap 위의 수동 보정 레이어로 제한한다.

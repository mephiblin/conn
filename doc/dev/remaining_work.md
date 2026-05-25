# 남은 개발 작업 현황

이 문서는 현재 Unity 프로젝트의 Chapter 1 Runtime Playable Core 기준으로 남은 작업을
정리한다. `development_pipeline.md`가 최초 작업 순서라면, 이 문서는 현재 구현 이후의
잔여 범위와 우선순위를 기록한다.

Editor Tool과 제작 파이프라인의 세부 진행 순서와 체크리스트는
[`editor_tool_content_pipeline_plan.md`](editor_tool_content_pipeline_plan.md)를
기준으로 한다. 해당 파이프라인 관련 작업을 할 때는 반드시 그 문서의 체크리스트
상태를 갱신해야 한다.

## 현재 진행률

대략적인 진행률은 다음과 같이 본다.

| 범위 | 진행률 | 상태 |
| --- | ---: | --- |
| P1 Runtime Vertical Slice | 98% | 자동 검증 기준 루프, Phase 8 preflight, uGUI Canvas panel 계약은 통과했고, Play Mode 체감 확인만 남음 |
| P2 전투/스킬/주사위 | 84-89% | 상태 이상/특수 효과/로그/HUD 가독성 1차 완료, encounter pattern/reward id/enemy slot Runtime uGUI 표시 계약 연결 |
| P3 장비/인벤토리/상점 | 78-86% | 장비/소모품/스킬 구분과 구매/판매 상태 표시 1차 완료, generated item 계약 필드 추가 |
| P4 마을 NPC 확장 | 72-82% | 8종 NPC와 최소 서비스/notice는 동작, NPC quest seed 네임스페이스 검증 정리 |
| P5 Editor Tool 1차 | 86-92% | Content DB bridge, authoring asset foundation, validation/bake/browser 경로 1차 완료 |
| P6 맵 생성 | 70-78% | compiledMap 2종 저장/검증, RuntimeMapGenerationBundle 경로, placement/field actor spawn 계약 1차 완료 |

Phase 6 test content production has started. The first authored content batch
adds 8 `MonsterDefinitionAsset` records under
`Assets/Conn/Authoring/Content/Phase6Monsters`, and batch validation now proves
that these authored monsters are discoverable and pass authoring validation.
The second batch adds 12 `SkillDefinitionAsset` records under
`Assets/Conn/Authoring/Content/Phase6Skills`, including supported attack, guard,
heal, support, buff, debuff, lifesteal, summon, and Bleed special-effect metadata
coverage.
The third batch adds 6 `EncounterDefinitionAsset` records under
`Assets/Conn/Authoring/Content/Phase6Encounters`, wired to the authored Phase 6
monsters through runtime-safe monster ids and validated enemy slots.
The fourth batch adds `QuestDefinitionAsset` support plus 5 authored quest assets
under `Assets/Conn/Authoring/Content/Phase6Quests`, each linked to authored
Phase 6 encounters, target monsters, and the Chapter 2 first-slice map profile.
The fifth batch adds 4 `VendorDefinitionAsset` records under
`Assets/Conn/Authoring/Content/Phase6Vendors`, covering equipment, skill,
advanced skill, and consumable stock with validated item/skill/catalog references
and rotation data.
The sixth batch verifies the existing ContentDatabase NPC set for Phase 6:
at least 8 NPC definitions are present, and the same database validation proves
their vendor and NPC quest seed references remain valid.
The map production checkpoint now saves and validates 2 compiled maps for the
Chapter 2 first-slice profile, using seeds `2001` and `2112`.
Phase 6 quest link validation now proves authored quests point to existing
Phase 6 encounters, matching target monsters, and the Chapter 2 first-slice map
profile.
Full automated content validation for this Phase 6 batch passes through
`git diff --check`, forbidden runtime Editor-reference scan, Chapter 1 runtime
rules, and Chapter 2 build/data/map validation.
Phase 7 field monster work has started with a runtime-safe AI profile data
contract. Monster authoring assets bake `FieldMonsterAiProfile` data into
`ContentMonsterDefinition`, and field monster runtime state now preserves the
profile id plus detection, patrol, move speed, and contact cooldown values.
Content and authoring validation now reject missing field AI profile data,
empty profile ids, and negative detection, patrol, move speed, or contact
cooldown values.
Dungeon bootstrap now spawns runtime `FieldMonsterContact` actors from compiled
map quest, boss, and monster placements. The spawner supports runtime bundle
encounter placements and saved compiledMap placement-only fallback maps.
Field monster runtime state now stores each actor's compiled map anchor
coordinates so follow-up Idle, patrol, chase, and return-to-anchor behavior can
use stable map placement data.
Spawned field monster actors now include a minimal runtime controller that
implements Idle by holding the actor at its spawn anchor while the field monster
state remains `Idle`.
Field monster actors now support a deterministic `Patrol` state that moves
between the anchor and an AI-profile-radius waypoint using the profile move
speed.
Field monster actor controllers now evaluate player detection against the
AI-profile detection radius, with validation covering inside/outside radius
cases.
Detection now transitions field monsters from Patrol to `Chase`, and the actor
controller moves toward the detected player using the profile move speed.
Chase now falls back to `ReturnToAnchor` when the player leaves detection
radius, then returns the actor to its anchor and resumes Patrol.
Field monster contact now records `LastContactTime` on runtime state and blocks
duplicate handoffs until the AI-profile contact cooldown has elapsed.
Combat handoff state is now explicitly verified through combat start and
save/load: the combat field monster key and world `CombatHandoff` status both
survive serialization.
Flee now clears combat, returns to Dungeon mode, and restores the source field
monster to `ReturnToAnchor` without marking it defeated.
Victory now explicitly verifies that the source field monster is marked
`Defeated` and that defeated field monster state survives save/load.
Field monster contact now routes through a single `TryBeginCombatHandoff` path
that rejects duplicate combat handoffs while one is active and still honors
contact cooldown.
Phase 8 automated preflight now covers the data/scene/runtime side of manual
Game view verification: DB quest board offer, target encounter/map profile,
compiledMap placement, field actor spawn, DB encounter combat, victory XP,
quest return reward, board reroll, Ending continue routing, and save/load
reward state.
Final pre-manual batch validation for this pass also ran the full Chapter 1
build validator and Chapter 2 build validator:
`/tmp/conn_ch1_build_validate_final_pre_manual.log` and
`/tmp/conn_ch2_build_validate_final_pre_manual.log`.
The latest continuation validation reran the same full Chapter 1 and Chapter 2
batch validators after checklist sync:
`/tmp/conn_ch1_build_validate_goal_continue.log` and
`/tmp/conn_ch2_build_validate_goal_continue.log`.
The Phase 6 repeated-play preflight now simulates 3 consecutive board quest
loops through compiledMap target handoff, DB encounter combat, victory XP,
return reward, active quest clearing, and board reroll before manual Play Mode:
`/tmp/conn_ch1_validate_phase6_three_quest_preflight.log` and
`/tmp/conn_ch2_build_validate_phase6_three_quest_preflight.log`.
The `Conn > Play Mode Verification` editor window now gathers the manual
Phase 6/8 checklist, Chapter 1/2 validation buttons, Title scene opening, and
links to the playtest/pipeline checklist docs for final Game view verification.
The same window now persists manual checklist toggles with a completion count
and reset button, so Play Mode verification progress can be tracked inside the
Unity Editor session.
The manual verification window now mirrors the exact Phase 6 and Phase 8
checklist items from `editor_tool_content_pipeline_plan.md` and
`p1_playtest_checklist.md`, avoiding bundled checks that could hide a missed
Game view observation.
Batch validation after this exact-checklist window update also passed:
`/tmp/conn_ch1_build_validate_playmode_exact_checklist.log` and
`/tmp/conn_ch2_build_validate_playmode_exact_checklist.log`.
The playtest checklist now directs the tester through `Conn > Play Mode
Verification`, and the window shows final guidance to update the matching docs
only after every checked item was observed in the actual Game view.
Batch validation after this completion-guidance update also passed:
`/tmp/conn_ch1_build_validate_playmode_completion_guidance.log` and
`/tmp/conn_ch2_build_validate_playmode_completion_guidance.log`.
Assembly boundary audit confirms `Conn.Editor` is Editor-only and references
runtime assemblies one-way, while `Conn.Core`, `Conn.Runtime`, `Conn.UI`, and
`Conn.Authoring` do not reference `Conn.Editor` or `UnityEditor`.
Chapter 1 core runtime rules now include an Editor-side guard that fails if the
`Conn > Play Mode Verification` manual checklist drifts from the tracked Phase
6/8 checklist items.
The same guard also reads `editor_tool_content_pipeline_plan.md` and
`p1_playtest_checklist.md`, so the verification window, pipeline checklist, and
playtest checklist cannot silently drift apart.
Batch validation after extending the guard to read the tracked docs passed:
`/tmp/conn_ch1_build_validate_playmode_doc_guard.log` and
`/tmp/conn_ch2_build_validate_playmode_doc_guard.log`.
Batch validation after adding that checklist drift guard passed:
`/tmp/conn_ch1_build_validate_playmode_checklist_guard.log` and
`/tmp/conn_ch2_build_validate_playmode_checklist_guard.log`.
The legacy fixed Dungeon `Visible Monster Contact` marker has been removed from
P0 scene generation after compiled placement actor spawn and contact handoff
preflight passed, preventing duplicate contact sources during manual Game view
verification.

Chapter 1 전체는 약 70-80% 진행으로 본다. 자동 검증 가능한 Runtime Core는
통과했고, 남은 위험은 Play Mode 체감, 실제 Game view 가독성, 콘텐츠 다양성 쪽이다.

## 현재 완료 감사

| 항목 | 현재 증거 | 상태 |
| --- | --- | --- |
| Inspector-first authoring asset foundation | `Conn.Authoring` asset types, authoring bake path, Chapter 1/2 validation logs | 자동 검증 통과 |
| `ContentDatabaseWindow` role reduction | Authoring browser/build/validation bridge plus preserved bootstrap DB tabs | 자동 검증 통과 |
| Spawn/monster metadata and spawn table path | authored Phase 6 monsters/encounters/quests/vendors, spawn table validation, generated single-primary fallback | 자동 검증 통과 |
| Map authoring and Generator Workbench | `MapProfileAsset` selection, resource/weight/spawn summaries, seed/floor/difficulty generation, compiled map export | 자동 검증 통과 |
| `RuntimeMapGenerationBundle + profileId + seed` | runtime-safe bundle contract scan and Chapter 2 runtime generation validation | 자동 검증 통과 |
| Runtime/Core/UI Runtime forbidden Editor references | code scan plus asmdef boundary audit | 자동 검증 통과 |
| Phase 6 repeated quest sequence | automated 3-loop preflight exists, but Game view sequence not personally observed | 수동 확인 필요 |
| Phase 8 full Game view loop | automated data/scene/runtime preflight exists, but UI readability and actual scene-flow observation remain | 수동 확인 필요 |

## 최종 컴파일/스모크/리뷰 결과

- 컴파일/스모크: Chapter 1 build validator 통과
  (`/tmp/conn_ch1_compile_smoke_review_debug.log`).
- 컴파일/스모크: Chapter 2 build validator 통과
  (`/tmp/conn_ch2_compile_smoke_review_debug.log`).
- 코드리뷰/디버깅: spawned field monster actor가 실제 런타임 경로에서
  `Player` target을 자동 연결하지 않아 detection/Chase가 Play Mode에서
  비활성화될 수 있는 문제를 발견했다.
- 수정: `FieldMonsterActorSpawner`가 `Player` 태그 Transform을 찾아
  `FieldMonsterActorController.SetPlayerTarget`에 연결한다.
- 회귀 검증: `RuntimeRuleVerifier`가 spawned actor의 player auto-bind
  detection을 검증하도록 보강했고 Chapter 1/2 validation으로 통과했다.

## 앞으로 남은 작업

수동 작업은 사용자가 수행한다. Codex 쪽 자동 구현/검증 작업은 현재 추가로
남기지 않는다.

사용자 수동 확인 대상:

- Phase 6: 같은 Play Mode 세션에서 3개 퀘스트를 연속 수주, 던전 진입,
  전투 승리, 마을 보상 수령까지 확인한다.
- Phase 8: `editor_tool_content_pipeline_plan.md`의 Game view 체크리스트
  12개 항목을 실제 Unity Game view에서 확인한다.
- 확인 후 `Conn > Play Mode Verification` 창의 토글을 완료하고,
  `editor_tool_content_pipeline_plan.md` 및 `p1_playtest_checklist.md`의
  대응 `[!]` 항목을 `[x]`로 변경한다.

## 커밋 전 범위 노트

이번 목표와 직접 연결되는 변경은 Inspector-first authoring, ContentDatabase
bridge, spawn/map validation, runtime generation bundle, field monster placement
handoff, Play Mode verification support, and related docs/assets이다.

커밋 전에는 다음 변경을 별도 UX/reference 작업으로 분리할지 확인한다:

- `Assets/Conn/Rendering/Player/FpsPlayerController.cs`
- `Assets/Conn/Runtime/Session/RuntimeCursorService.cs`
- `Assets/Conn/Runtime/Session/RuntimeCursorService.cs.meta`
- `doc/ref/`
- `Assets/Conn/UI/Runtime/RuntimeCanvasUi.cs`의 cursor 관련 hunk

## 이미 구현된 핵심

- `Title -> Town -> Quest Board -> Gate -> Dungeon -> Field Monster -> Combat -> Dungeon -> Return -> Town reward` 흐름
- 마을/던전 자유 이동 FPS
- 월드 오브젝트 `E` 상호작용
- 퀘스트 게시판 패널
- 퀘스트 리롤, 수주, 활성 퀘스트 1개 제한
- 퀘스트가 없으면 게이트 입장 차단
- 필드 몬스터 접촉 전투 handoff
- 전투 전 `preEncounterSnapshot`
- 전투 승리 후 목표 완료, 귀환 가능, 필드 몬스터 cleanup
- 전투 도주 시 던전 복귀와 필드 몬스터 idle 복구
- 사망 시 Ending 상태 전환
- 최소 주사위/스킬 전투
- `MonsterDefinition`/`EncounterDefinition` 기반 전투 데이터
- 적 행동명/행동 power 기반 전투 메시지
- Bleed 상태 이상과 Focus Strike 특수 효과
- encounter 기반 XP 보상
- encounter enemy slot/list 계약과 Runtime 표시 상태
- `single_primary` fallback 유지
- support/buff/debuff/lifesteal/summon effect kind 확장 지점
- 장비 상태별 주사위 수
- 머리/가슴/팔/다리/신발 방어구 슬롯
- 방어구 armor value와 장비 비교 문구
- 골드, XP, HP, 저장/Continue 기초
- 대장장이 장비 구매/판매
- 기술 상인 스킬 구매/판매
- 기술 상인 deterministic stock refresh
- 기술 상인 stock refresh 표시와 stock 목록 notice
- 약재상 소모품 구매, 포션 사용
- 여관 회복
- 훈련소 XP 훈련
- 학자 힌트
- 장비/소모품/스킬을 구분한 Character 패널 표시
- 장착 중/판매 가능/구매 가능 상태 표시
- 전투 HUD의 dice face, 선택 상태, cooldown, 상태 이상 표시
- 승리/패배/피해/방어/상태 이상 tick 로그
- 캐릭터 패널의 장비 장착과 스킬 face 순환
- 던전 HUD의 원정/몬스터/귀환 상태 표시
- `Conn > Build & Validate Chapter 1` batchmode 검증
- P1 IMGUI overlay와 fallback 상호작용 prompt의 작은 화면 clamp 자동 검증
- Runtime uGUI Canvas, CanvasScaler, EventSystem, SceneBootstrap binding, scene별 panel root 자동 생성/검증
- Title/Town/Dungeon/Combat/Ending Canvas 기반 1차 Runtime UI
- `Conn > Content Database > Import Legacy JSON`
- `Conn > Content Database > Window`
- `Conn > Map > Generator Workbench`
- `Conn > Build & Validate Chapter 2`
- ContentDatabase 기반 장비 장착/주사위/방어 계산
- ContentDatabase 기반 퀘스트 게시판 수주와 quest -> encounter -> monster -> map profile Runtime 연결
- ContentDatabase 기반 encounter lookup과 CombatRuntimeService 우선 소비
- CombatRuntimeService의 encounter `pattern`/`rewardId` 소비 계약
- vendor rotation 기반 기술 상인/대장장이 stock Runtime 적용
- NPC vendor/service definition 기반 여관/약재상 최소 Runtime 서비스 연결
- ContentDatabase 기반 소모품 구매 lookup
- Generator Workbench compiledMap asset 저장 버튼
- SceneBootstrap compiledMap asset 등록과 Runtime 우선 로드
- compiledMap quest target placement -> field monster state 등록
- compiledMap monster placement -> field monster state 추가 등록
- compiledMap loot placement 데이터 계약
- NPC `quest_seed_` 참조를 board quest가 아닌 NPC seed 네임스페이스로 검증

## P1에 남은 작업

P1은 자동 검증 기준으로 닫혔다. 실제 플레이 기준으로 아래 확인이 필요하다.

1. Play Mode 전체 흐름 수동 테스트
   - New Game
   - 퀘스트 게시판 수주
   - 게이트 입장
   - 몬스터 접촉
   - 전투 승리
   - 던전 복귀
   - 귀환 팝업
   - Town 보상 정산
   - Continue 확인

2. 조작감 보정
   - 마우스 감도 재확인: 기본값 `0.14`
   - 이동 속도: 기본값 `4.5`
   - 상호작용 거리: 기본값 `4.0`
   - 카메라 높이
   - 콜라이더 크기

3. HUD 배치 정리
   - uGUI Runtime Canvas 1차 전환 완료.
   - 기존 IMGUI 임시 HUD는 fallback/debug 플래그로 유지한다.
   - overlay와 fallback 상호작용 prompt는 320x240/220x160 기준 화면 안쪽 clamp 자동 검증을 통과했다.
   - 기본 상호작용 prompt는 Runtime uGUI Canvas 경로에서 표시한다.
   - Runtime Canvas panel root도 normalized safe rect 계약으로 자동 검증한다.
   - 실제 Play Mode Game view에서 긴 notice/상점/전투 로그 가독성은 확인해야 한다.
   - 이후 정식 prefab화와 visual polish가 필요하다.

4. Ending 흐름 보강
   - 사망 후 Ending 화면에서 사망 사유/결과 표시: 1차 완료
   - Back To Title 후 Continue 정책: Ending 저장을 유지하고 Continue는 Ending으로 복귀
   - Ending의 New Game은 새 저장으로 덮어쓴다.

## P2에 남은 작업: 전투/스킬/주사위

현재 전투는 최소 도메인으로만 동작한다. Chapter 1 완성에는 아래가 남아 있다.

1. 전투 데이터 분리
   - `EncounterDefinition`: 1차 완료
   - `MonsterDefinition`: 1차 완료
   - enemy HP/공격력/보상 데이터: 1차 완료
   - 퀘스트 target monster와 encounter 연결: 1차 검증 완료
   - ContentDatabase encounter import/lookup: 1차 완료
   - CombatRuntimeService DB encounter 우선 소비: 1차 완료
   - encounter `pattern`/`rewardId`를 combat session 상태에 보존: 1차 완료
   - 다중 적 encounter pattern 데이터 계약: 1차 완료
   - enemy slot/list 계약과 참조 무결성 validator: 1차 완료
   - Runtime은 현재 primary 적 1명을 실제 전투 대상으로 유지하고, enemy slot 상태와 HUD 표시 계약만 노출한다.
   - 남은 작업: 다중 적 target 선택/턴 처리와 reward table 지급 실행

2. 전투 룰 확장
   - 적 행동명/행동 power: 1차 완료
   - 상태 이상: Bleed 1차 완료
   - 스킬별 특수 효과: Focus Strike 1차 완료
   - 남은 작업: 다중 패턴 실제 실행, 추가 상태 이상, 추가 스킬 효과
   - effect kind 확장 지점: support/buff/debuff/lifesteal/summon 1차 추가
   - 방어 보정 명확화: 1차 로그 완료
   - 회복/방어/공격 외 스킬 효과 확장 지점
   - 전투 로그 정리: 1차 완료
   - 선택 가능한 주사위 수와 쿨다운 규칙 재검토

3. 보상 처리
   - XP 보상 데이터화
   - encounter reward id와 quest return reward 분리 계약: 1차 Runtime 상태 연결 완료
   - encounter XP는 CombatRuntimeService에서 지급하고, quest return gold는 QuestRuntimeService에서 유지한다.
   - 골드/아이템 보상 확장 지점
   - 퀘스트 보상과 전투 보상 분리

4. 패배 처리
   - HP 0이 되는 실제 플레이 루프 테스트
   - Ending 저장/Continue 정책: 1차 구현/검증 완료

5. Combat UI 개선
   - 버튼형 IMGUI는 fallback/debug로 유지한다.
   - uGUI Combat Canvas에 enemy stage, command, dice, log, status panel 1차 추가
   - 주사위 face, 쿨다운, 선택 상태의 가독성: 1차 개선 완료

## P3에 남은 작업: 장비/인벤토리/상점

현재 구매/판매/장착은 가능하지만 아직 프로토타입 수준이다.

1. 장비 슬롯 확장
   - 현재 실질 구현: 무기, 방패, 머리, 가슴, 팔, 다리, 신발
   - 방어구 스탯 효과: 1차 완료
   - 장비 비교 문구: 1차 완료
   - 남은 작업: 정식 장비 비교 UI와 스탯 밸런스

2. 인벤토리 UI 정리
   - uGUI Town Character/Inventory panel 1차 전환 완료.
   - 장비, 소모품, 스킬 카드 구분: 1차 완료
   - 장착 중/판매 가능/구매 가능 상태 표시: 1차 완료

3. 상점 재고 규칙
   - 현재 기술 상인은 deterministic limited stock 기반이고, 대장장이는 DB vendor stock/rotation을 읽는다.
   - 남은 Diablo II식 요소:
     - 진행도 기반 rotation: 기술 상인/대장장이 1차 완료
     - stock refresh: 기술 상인 1차 완료
     - skill stock size: 1차 완료
     - rarity/affix/generated offer 계약

4. generated item
   - 이번 Chapter 1에서는 고정 아이템만 사용한다.
   - `generated`, `rarityId`, `affixPoolId` 계약 필드는 ContentDatabase equipment에 추가했다.
   - `generated=true`인 장비는 `rarityId`와 `affixPoolId`가 비어 있으면 validator error로 처리한다.

5. 저장 검증 확장
   - 장비 슬롯 확장 후 저장/불러오기 검증 필요
   - 상점 재고가 런타임 저장 상태인지, 진입 시 재생성 상태인지 결정 필요

## P4에 남은 작업: 마을 NPC

마을 NPC 8종은 배치와 최소 상호작용이 있지만, 서비스 깊이는 다르다.

| NPC | 현재 상태 | 남은 작업 |
| --- | --- | --- |
| 여관 | 회복 가능, notice 표시 | 비용/상태이상 회복 정책 |
| 훈련소 | XP로 Max HP 증가, notice 표시 | 훈련 항목 확장 여부 결정 |
| 대장장이 | 장비/방어구 구매/판매, 상태 표시 | 재고 rotation |
| 약재상 | 포션 구매, notice 표시 | 소모품 종류 확대 |
| 기술 상인 | 스킬 구매/판매, stock 표시/refresh | catalog 분리 |
| 학자 | 힌트 제공 | 감정/정보/던전 단서 역할 결정 |
| 임무 게시판 | 퀘스트 수주/리롤 | 타이머 리롤, 던전 클리어 리롤 |
| 게이트 | 던전 입장 | 퀘스트별 던전 선택/출발 확인 |

## P5에 남은 작업: Editor Tool 1차

현재 Editor Tool은 Content Database와 Generator Workbench의 1차 골격이 생겼고,
Content Database Window에서 Monster/Encounter/Quest를 작성, 저장, 검증할 수 있다.
이 DB 직접 편집기는 최종 제작 도구가 아니라 bootstrap/browser/build/validation
bridge로 둔다. Inspector-first authoring asset foundation이 시작되었고,
Monster/Encounter authoring asset discovery, validation, DB bake bridge, RuntimeContentDatabase
소비 검증이 1차 연결되었다. MapProfile/ResourceSet/Chunk/Landmark/SpawnTable/Weight
authoring validation도 Generator Workbench에 1차 연결되었다. RuntimeMapGenerationBundle
최소 build/export와 `bundle + profileId + seed` 생성 검증, compiled encounter
placement record, deterministic weighted spawn table runtime resolution,
floor/difficulty/tag compatibility filtering, Generator Workbench의
MapProfileAsset 선택/요약/seed/floor/difficulty 생성 컨텍스트 입력도 1차
연결되었다. SpawnTableAsset membership/MapProfile usage preview도
ContentDatabaseWindow authoring browser에 1차 연결되었다. Map authoring
validation은 spawn table resolved pool과 encounter/monster theme/map-kind
compatibility까지 1차 확장되었다. RuntimeMapGenerationBundle 검증은 quest
target/boss encounter placement가 올바른 placement kind와 RuntimeContentDatabase
encounter로 resolve되는지도 확인한다. ResourceSet/Chunk의 Unity object reference
배열 내 null/broken reference 검증도 추가되었다. Landmark role, unique landmark
reuse, generation weight count/repeat range 검증도 추가되었다. Profile room size와
role/socket chunk coverage 검증도 추가되었다. RuntimeMapGenerationBundle 계약은
Editor/Authoring/Unity object reference 필드를 포함하지 않는지 batch 검증한다.
NpcDefinitionAsset browser/build bridge, vendor/quest seed validation, bake,
RuntimeContentDatabase 소비 검증도 1차 연결되었다. SkillDefinitionAsset
browser/build bridge, effect/target/formula fields, price/effect validation,
bake, RuntimeContentDatabase 소비 검증도 1차 연결되었다. VendorDefinitionAsset
browser/build bridge, stock/catalog/rotation fields, stock/rotation validation,
bake, RuntimeContentDatabase vendor stock/rotation 소비 검증도 1차 연결되었다.
Fallback reduction 1차 분류도 `data_pipeline_status.md`에 추가되어 catalog
lookup, generated single-primary encounter, starter loadout, shop stock, town
service, combat, compiled map generation, scene bootstrap fallback이 required,
debug-only, removable 범주로 나뉘었다. Dungeon runtime은 saved `CompiledMapAsset`
우선 정책을 유지하면서, 저장 compiled map이 없을 때 `SceneBootstrap`에 바인딩된
`RuntimeMapGenerationBundleAsset`으로 `profileId + seed` 기반 compiled map을
생성한 뒤 catalog generator fallback으로 내려간다. Chapter validators/P0 scene
generation은 기본 `RuntimeMapGenerationBundle.asset`을 생성하고 Dungeon
`SceneBootstrap` binding을 검증한다. `SkillInventoryState.EquippedPower`의
직접 `SkillCatalog.Find` fallback은 DB-active runtime에서
`RuntimeContentDatabase.FindSkill` resolver를 쓰도록 1차 축소되었고, DB-only
equipped skill power 계산을 batch validation이 검증한다. Scholar hint도
`RuntimeContentDatabase.BoardQuestAt`을 쓰도록 바뀌어 DB-authored board offer를
catalog fallback보다 먼저 표시하며, DB-only scholar quest hint를 batch validation이
검증한다. `GameSessionState.StartNewGame`도 DB-configured starter equipment/skill
id resolver를 먼저 쓰고 catalog starter ids는 no-DB/no-invalid-starter fallback으로
남긴다. Content Database bootstrap/browser bridge에서 starter id를 편집할 수 있으며,
equipment sale/loadout restore logic도 configured starter equipment id를 사용한다.
typed loadout authoring asset은 후속 source-of-truth 단계로 남긴다.
`QuestRuntimeService.AcceptDefaultQuest`도 현재 DB board offer를 먼저 수주하고 hardcoded
test quest는 no-offer fallback으로 남긴다. Apothecary service prompt/action도 DB-first consumable vendor stock을
사용하도록 바뀌었고, DB-only apothecary consumable 구매를 batch validation이
검증한다. Skill merchant stock도 DB-active runtime에서는 비어 있는 DB stock을
catalog stock으로 조용히 대체하지 않고, `SkillCatalog.All` stock generation은
no-DB emergency fallback으로 제한되었다. Blacksmith UI stock도 DB-active
runtime에서는 비어 있는 DB stock을 `EquipmentCatalog.All` 표시로 대체하지 않고,
catalog stock display는 no-DB emergency fallback으로 제한되었다. Combat Bleed
special effect도 skill `SpecialEffectId` metadata로 이동해 DB-authored skill이
hardcoded Focus Strike id 없이 Bleed를 적용할 수 있다. Consumable UX
text와 Runtime UI use controls도 `RuntimeContentDatabase.FindConsumable` 및
보유 inventory consumable id를 사용해 DB-authored consumable을 표시/사용한다.
아직 broader Runtime data bundle 전환과 분류된 fallback의 추가 제거는 남아 있다.

우선순위:

1. Content Database
   - item, skill, monster, quest, vendor, npc legacy JSON import: 1차 완료
   - equipment seed data: 1차 완료
   - ID registry와 definition lookup: 1차 완료
   - `ContentDatabase.asset` 생성: 1차 완료
   - Runtime lookup path: monster/encounter/equipment/skill/quest/item 1차 연결, 기존 C# catalog fallback 유지
   - 장비 장착/주사위/방어 계산의 ContentDatabase 소비: 1차 완료
   - Content Database Window shared shell: 1차 완료
   - Monster/Encounter/Quest editor: 1차 완료
   - fallback 경로 required/debug-only/removable 분류: 1차 완료
   - `SkillInventoryState.EquippedPower` DB-installed skill resolver 전환: 1차 완료
   - `GameSessionState.StartNewGame` DB-configured starter loadout resolver 전환: 1차 완료
   - Content Database bridge starter loadout id editing: 1차 완료
   - Equipment service starter sale/loadout restore DB-configured starter id 전환: 1차 완료
   - `QuestRuntimeService.AcceptDefaultQuest` DB board offer 우선 수주 전환: 1차 완료
   - `TownServiceRuntimeService.ScholarHint` DB-first board offer 전환: 1차 완료
   - Combat Bleed special effect skill metadata 전환: 1차 완료
   - Apothecary service DB-first consumable vendor stock 전환: 1차 완료
   - Skill merchant catalog stock generation no-DB fallback 제한: 1차 완료
   - Blacksmith UI catalog stock display no-DB fallback 제한: 1차 완료
   - Consumable UX text/use controls DB-first owned consumable lookup 전환: 1차 완료
   - 남은 작업: 실제 테스트 콘텐츠를 authored database로 채운 뒤 분류된 catalog 호출을 하나씩 database source로 전환

2. Build & Validation 확장
   - ID registry 검증: 1차 완료
   - quest -> monster 참조 검증: 1차 완료
   - quest -> map profile/anchor 검증: 1차 완료
   - quest -> encounter -> monster 검증: 1차 강화 완료
   - vendor stock 참조 검증: 1차 완료
   - vendor rotation 계약 검증: 1차 완료
   - vendor rotation Runtime stock 적용: 기술 상인/대장장이 기준 1차 완료
   - skill/equipment 가격 검증: 1차 완료
   - 저장 계약 검증
   - `Conn > Build & Validate Chapter 2`: 1차 완료

3. NPC/Quest Editor
   - NPC service type import: 1차 완료
   - 게시판 퀘스트 후보: ContentDatabase 우선 순환 1차 완료
   - 퀘스트 보상
   - target monster/encounter 연결: 1차 완료
   - NPC `quest_seed_` 참조는 board quest 승격 대상이 아니라 NPC seed 네임스페이스로 유지한다.

4. Map/Encounter Editor
   - 제작용 grid
   - monster placement
   - gate/exit anchor
   - loot/quest anchor
   - Generator Workbench preview: 1차 완료
   - compiledMap asset 저장/export: 1차 완료
   - compiledMap Runtime loader: 저장 asset 우선 로드 1차 완료
   - RuntimeMapGenerationBundleAsset Dungeon bootstrap fallback-before-catalog 연결: 1차 완료
   - compiledMap quest target placement의 field monster state 등록: 1차 완료

## P6에 남은 작업: 던전/맵 생성

`diablo_map_generation_design.md` 기준 1차 deterministic 생성 파이프라인이 들어갔다.
실제 씬 빌드와 authored map editor는 아직 남아 있다.

1. `MapProfile`: 1차 완료
2. seed 기반 deterministic generation: 1차 완료
3. `RoomGraph`: 1차 완료
4. chunk/socket assembly: 1차 완료
5. start/exit/quest target/boss anchor 보장: 1차 완료
6. monster/loot placement pass: graph role 기반 1차 완료
7. validation: 1차 완료
8. compiledMap 생성: 1차 완료
9. Runtime에서 compiledMap 로드: 저장 asset 우선, generator fallback 유지 1차 완료
10. compiledMap quest target/exit/start anchor Runtime 연결: 1차 완료
11. compiledMap monster placement의 field monster state 등록: 1차 완료
12. 자동 지도/fog 해제

현재 던전은 Chapter 1 검증용 단일 공간/단일 몬스터에 가깝다.

## 우선순위 제안

다음 작업 순서는 아래가 좋다.

1. P1 플레이 테스트 마감
   - Ending/Continue 정책: 자동 검증 완료, Play Mode 확인 필요
   - HUD 겹침
   - 상호작용/콜라이더

2. P2 전투 룰 확장
   - 적 턴 패턴
   - 전투 로그/보상/패배 루프 보강: 1차 완료

3. P3 인벤토리/장비 UX 정리
   - 방어구 스탯 효과: 1차 완료
   - 장비 비교와 분류 표시: 1차 완료

4. P4 상점 재고 규칙
   - skill stock refresh: 1차 완료
   - vendor rotation: 기술 상인 Runtime 적용 1차 완료

5. P5 Content Database
   - Runtime database lookup 적용 범위 확장: 장비/스킬/퀘스트/몬스터 1차 완료
   - 남은 작업: authored ContentDatabase를 더 많은 실제 콘텐츠로 채우고 fallback 의존도 축소

6. P6 맵 생성기
   - compiledMap 기반 Dungeon scene 생성으로 연결
   - quest target/monster placement는 field monster state로 1차 연결됨

## 사용자가 확인해야 할 것

Codex가 자동으로 검증하기 어려운 항목은 아래다.

- Unity Play Mode에서 전체 P1 루프가 실제로 끊기지 않는지
- 마우스 감도와 이동감: 감도 기본값은 `0.14`로 낮춤
- NPC/게이트/몬스터 collider 크기와 위치
- uGUI HUD 버튼이 화면 밖으로 밀리지 않는지
- 전투 UI가 클릭하기 불편하지 않은지
- 사망 후 Ending/Title/Continue 흐름이 원하는 정책과 맞는지
- 기존 IMGUI fallback/debug 표시를 언제 제거할지

## 현재 완료의 의미

현재 프로젝트는 자동 검증 기준으로 “게임처럼 한 바퀴 도는 최소 루프”를 갖췄다.
Chapter 1의 남은 위험은 사람이 직접 느껴야 하는 조작감과 UI 배치, 그리고 반복
플레이에 필요한 콘텐츠 다양성이다. 전투 데이터, 상점 rotation, 맵/조우 다양성,
Editor Tool 제작 파이프라인은 Chapter 2 이후에도 확장 축으로 남는다. 2026-05-25
기준 Chapter 2 Runtime Data Consumption은 자동 검증으로 장비, 퀘스트, 상점 rotation,
compiledMap placement의 1차 Runtime 연결을 통과했다. 같은 날짜에 encounter enemy slot
계약, generated item 계약 필드, NPC quest seed 네임스페이스, monster/loot placement
pass도 자동 검증 기준으로 추가되었다.

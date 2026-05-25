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
| P1 Runtime Vertical Slice | 97% | 자동 검증 기준 루프와 uGUI Canvas panel 계약은 통과했고, Play Mode 체감 확인만 남음 |
| P2 전투/스킬/주사위 | 84-89% | 상태 이상/특수 효과/로그/HUD 가독성 1차 완료, encounter pattern/reward id/enemy slot Runtime uGUI 표시 계약 연결 |
| P3 장비/인벤토리/상점 | 78-86% | 장비/소모품/스킬 구분과 구매/판매 상태 표시 1차 완료, generated item 계약 필드 추가 |
| P4 마을 NPC 확장 | 72-82% | 8종 NPC와 최소 서비스/notice는 동작, NPC quest seed 네임스페이스 검증 정리 |
| P5 Editor Tool 1차 | 70-80% | Content DB import/검증, encounter/quest/vendor/NPC Runtime 소비 1차 확대 |
| P6 맵 생성 | 62-70% | compiledMap asset 저장/Runtime 우선 로드, start/quest target/boss/exit/monster/loot placement 계약 1차 완료 |

Chapter 1 전체는 약 70-80% 진행으로 본다. 자동 검증 가능한 Runtime Core는
통과했고, 남은 위험은 Play Mode 체감, 실제 Game view 가독성, 콘텐츠 다양성 쪽이다.

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

현재 Editor Tool은 Content Database와 Generator Workbench의 1차 골격이 생겼다.
아직 본격 제작 UX와 Runtime data bundle 전환은 남아 있다.

우선순위:

1. Content Database
   - item, skill, monster, quest, vendor, npc legacy JSON import: 1차 완료
   - equipment seed data: 1차 완료
   - ID registry와 definition lookup: 1차 완료
   - `ContentDatabase.asset` 생성: 1차 완료
   - Runtime lookup path: monster/encounter/equipment/skill/quest/item 1차 연결, 기존 C# catalog fallback 유지
   - 장비 장착/주사위/방어 계산의 ContentDatabase 소비: 1차 완료
   - 남은 작업: UI 표시와 모든 catalog 호출을 점진적으로 database source로 전환

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

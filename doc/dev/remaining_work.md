# 남은 개발 작업 현황

이 문서는 현재 Unity 프로젝트의 Chapter 1 Runtime Playable Core 기준으로 남은 작업을
정리한다. `development_pipeline.md`가 최초 작업 순서라면, 이 문서는 현재 구현 이후의
잔여 범위와 우선순위를 기록한다.

## 현재 진행률

대략적인 진행률은 다음과 같이 본다.

| 범위 | 진행률 | 상태 |
| --- | ---: | --- |
| P1 Runtime Vertical Slice | 90% | 기본 루프는 닫혔고, 실제 Play Mode 수동 확인이 남음 |
| P2 전투/스킬/주사위 | 65-75% | 상태 이상과 스킬 특수 효과 1차가 들어갔고, 전투 UI가 남음 |
| P3 장비/인벤토리/상점 | 65-75% | 방어구 스탯과 비교 표시는 들어갔고, UX 정리가 남음 |
| P4 마을 NPC 확장 | 55-65% | 8종 NPC는 존재하나 일부 서비스가 얕음 |
| P5 Editor Tool 1차 | 15-25% | 검증/빌드 도구만 있고 제작용 에디터는 아직 시작 전 |
| P6 맵 생성 | 0-10% | 문서만 있고 Runtime 연결은 아직 없음 |

Chapter 1 전체는 약 55-60% 진행으로 본다.

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
- 장비 상태별 주사위 수
- 머리/가슴/팔/다리/신발 방어구 슬롯
- 방어구 armor value와 장비 비교 문구
- 골드, XP, HP, 저장/Continue 기초
- 대장장이 장비 구매/판매
- 기술 상인 스킬 구매/판매
- 기술 상인 deterministic stock refresh
- 약재상 소모품 구매, 포션 사용
- 여관 회복
- 훈련소 XP 훈련
- 학자 힌트
- 캐릭터 패널의 장비 장착과 스킬 face 순환
- 던전 HUD의 원정/몬스터/귀환 상태 표시
- `Conn > Build & Validate Chapter 1` batchmode 검증

## P1에 남은 작업

P1은 기능적으로 거의 닫혔지만, 실제 플레이 기준으로 아래 보정이 필요하다.

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
   - 마우스 감도 재확인
   - 이동 속도
   - 상호작용 거리
   - 카메라 높이
   - 콜라이더 크기

3. HUD 배치 정리
   - 현재 IMGUI 임시 HUD가 길어지고 있다.
   - 버튼이 작은 화면에서 밀리거나 겹치는지 확인해야 한다.
   - 이후 uGUI/UIToolkit으로 옮길지 결정해야 한다.

4. Ending 흐름 보강
   - 사망 후 Ending 화면에서 사망 사유/결과 표시
   - Back To Title 후 Continue 정책 확정
   - 사망 세이브를 유지할지, 새 게임만 허용할지 결정

## P2에 남은 작업: 전투/스킬/주사위

현재 전투는 최소 도메인으로만 동작한다. Chapter 1 완성에는 아래가 남아 있다.

1. 전투 데이터 분리
   - `EncounterDefinition`: 1차 완료
   - `MonsterDefinition`: 1차 완료
   - enemy HP/공격력/보상 데이터: 1차 완료
   - 퀘스트 target monster와 encounter 연결: 1차 검증 완료
   - 남은 작업: 하드코딩 catalog를 ScriptableObject/JSON import 가능한 데이터로 이전

2. 전투 룰 확장
   - 적 행동명/행동 power: 1차 완료
   - 상태 이상: Bleed 1차 완료
   - 스킬별 특수 효과: Focus Strike 1차 완료
   - 남은 작업: 다중 패턴, 추가 상태 이상, 추가 스킬 효과
   - 방어 보정 명확화
   - 회복/방어/공격 외 스킬 효과 확장 지점
   - 전투 로그 정리
   - 선택 가능한 주사위 수와 쿨다운 규칙 재검토

3. 보상 처리
   - XP 보상 데이터화
   - 골드/아이템 보상 확장 지점
   - 퀘스트 보상과 전투 보상 분리

4. 패배 처리
   - HP 0이 되는 실제 플레이 루프 테스트
   - Ending 저장/Continue 정책 확정

5. Combat UI 개선
   - 현재 버튼형 IMGUI를 임시 유지할지, 별도 전투 HUD로 분리할지 결정
   - 주사위 face, 쿨다운, 선택 상태의 가독성 개선

## P3에 남은 작업: 장비/인벤토리/상점

현재 구매/판매/장착은 가능하지만 아직 프로토타입 수준이다.

1. 장비 슬롯 확장
   - 현재 실질 구현: 무기, 방패, 머리, 가슴, 팔, 다리, 신발
   - 방어구 스탯 효과: 1차 완료
   - 장비 비교 문구: 1차 완료
   - 남은 작업: 정식 장비 비교 UI와 스탯 밸런스

2. 인벤토리 UI 정리
   - 현재는 HUD 안의 임시 Character 패널이다.
   - 장비, 소모품, 스킬 카드를 구분해서 보여줘야 한다.
   - 장착 중/판매 가능/사용 가능 상태가 더 명확해야 한다.

3. 상점 재고 규칙
   - 현재 기술 상인은 deterministic limited stock 기반이다.
   - 남은 Diablo II식 요소:
     - 진행도 기반 rotation
     - stock refresh: 1차 완료
     - skill stock size: 1차 완료
     - rarity/affix/generated offer 계약

4. generated item
   - 이번 Chapter 1에서는 고정 아이템만 사용한다.
   - 단, `generated`, `rarityId`, `affixPoolId` 계약은 문서/데이터에 유지해야 한다.

5. 저장 검증 확장
   - 장비 슬롯 확장 후 저장/불러오기 검증 필요
   - 상점 재고가 런타임 저장 상태인지, 진입 시 재생성 상태인지 결정 필요

## P4에 남은 작업: 마을 NPC

마을 NPC 8종은 배치와 최소 상호작용이 있지만, 서비스 깊이는 다르다.

| NPC | 현재 상태 | 남은 작업 |
| --- | --- | --- |
| 여관 | 회복 가능 | 비용/상태이상 회복 정책 |
| 훈련소 | XP로 Max HP 증가 | 훈련 항목 확장 여부 결정 |
| 대장장이 | 장비 구매/판매 | 장비 슬롯 확장, 재고 rotation |
| 약재상 | 포션 구매 | 소모품 종류 확대 |
| 기술 상인 | 스킬 구매/판매 | stock refresh, catalog 분리 |
| 학자 | 힌트 제공 | 감정/정보/던전 단서 역할 결정 |
| 임무 게시판 | 퀘스트 수주/리롤 | 타이머 리롤, 던전 클리어 리롤 |
| 게이트 | 던전 입장 | 퀘스트별 던전 선택/출발 확인 |

## P5에 남은 작업: Editor Tool 1차

현재 Editor Tool은 검증과 씬 생성에 가깝다. 본격 제작 도구는 남아 있다.

우선순위:

1. Content Database
   - equipment
   - skill
   - monster
   - encounter
   - quest
   - vendor
   - npc

2. Build & Validation 확장
   - ID registry 검증
   - quest -> encounter -> monster 참조 검증
   - vendor stock 참조 검증
   - skill/equipment 가격 검증
   - 저장 계약 검증

3. NPC/Quest Editor
   - 게시판 퀘스트 후보
   - 퀘스트 보상
   - target monster/encounter 연결
   - NPC service type

4. Map/Encounter Editor
   - 제작용 grid
   - monster placement
   - gate/exit anchor
   - loot/quest anchor

## P6에 남은 작업: 던전/맵 생성

`diablo_map_generation_design.md` 기준 구현은 아직 남아 있다.

1. `MapProfile`
2. seed 기반 deterministic generation
3. `RoomGraph`
4. chunk/socket assembly
5. start/exit/quest target anchor 보장
6. monster/loot placement pass
7. validation
8. compiledMap 생성
9. Runtime에서 compiledMap 로드
10. 자동 지도/fog 해제

현재 던전은 Chapter 1 검증용 단일 공간/단일 몬스터에 가깝다.

## 우선순위 제안

다음 작업 순서는 아래가 좋다.

1. P1 플레이 테스트 마감
   - Ending/Continue 정책
   - HUD 겹침
   - 상호작용/콜라이더

2. P2 전투 룰 확장
   - 적 턴 패턴
   - 전투 로그/보상/패배 루프 보강

3. P3 인벤토리/장비 UX 정리
   - 방어구 스탯 효과
   - 장비 비교와 분류 표시

4. P4 상점 재고 규칙
   - skill stock refresh
   - vendor rotation

5. P5 Content Database
   - 하드코딩 catalog를 제작 데이터로 이전하기 시작

6. P6 맵 생성기
   - Chapter 1 core가 더 안정된 뒤 시작

## 사용자가 확인해야 할 것

Codex가 자동으로 검증하기 어려운 항목은 아래다.

- Unity Play Mode에서 전체 P1 루프가 실제로 끊기지 않는지
- 마우스 감도와 이동감
- NPC/게이트/몬스터 collider 크기와 위치
- HUD 버튼이 화면 밖으로 밀리지 않는지
- 전투 UI가 클릭하기 불편하지 않은지
- 사망 후 Ending/Title/Continue 흐름이 원하는 정책과 맞는지
- 현재 IMGUI HUD를 언제까지 임시로 둘지

## 현재 완료의 의미

현재 프로젝트는 “게임처럼 한 바퀴 도는 최소 루프”는 갖췄다. 하지만 아직
“반복해서 플레이할 수 있는 Chapter 1”이라고 보기에는 전투 데이터, 인벤토리 UX,
상점 재고, 맵/조우 다양성, Editor Tool 제작 파이프라인이 부족하다.

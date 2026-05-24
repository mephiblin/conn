# 개발 작업순서와 파이프라인

이 문서는 현재 Unity 프로젝트에서 실제 개발을 시작하기 위한 작업 순서다. 목표는
전체 게임을 한 번에 만드는 것이 아니라, 가장 작은 playable vertical slice를 먼저
닫고 그 위에 상점, 장비, 스킬, 맵 생성기, Editor Tool을 붙이는 것이다.

## 현재 확정 기준

- 마을과 던전은 자유 이동 FPS 공간이다.
- 셀/격자는 플레이어 이동 스냅이 아니라 Editor Tool의 맵 생성/오브젝트 배치 기준이다.
- 전투는 `Dungeon -> Combat -> Dungeon` 별도 씬/상태 머신이다.
- 던전 입장은 활성 퀘스트가 있을 때만 가능하다.
- 초기 퀘스트는 지정 몬스터 토벌형이다.
- 퀘스트 실패 조건은 사망이다.
- 퀘스트 완료 후 `귀환 / 계속 탐험` 팝업을 띄우고, 계속 탐험 시 HUD 귀환 버튼을 켠다.
- 초기 보상은 골드만 지급한다.
- 상점 경제는 골드 1종이며, 제작 없이 구매/판매만 한다.
- 동료와 직업은 현재 범위에 넣지 않는다.
- 전투 주사위 개수는 장착 무기가 정한다.
- Editor Tool은 Game Runtime에 포함하지 않는다.

## 개발 원칙

- 먼저 Runtime vertical slice를 만든다.
- 데이터는 처음부터 하드코딩하지 말고 ScriptableObject 또는 JSON import 가능한 DTO로 둔다.
- Editor Tool은 Runtime 루프가 한 번 돈 뒤 필요한 데이터 제작 범위부터 붙인다.
- 레거시 웹 코드는 포팅 대상이 아니라 규칙과 데이터 계약 참조다.
- Unity 생성 캐시(`Library`, `Temp`, `Logs`, `UserSettings`)는 Git에 넣지 않는다.

## P0: 프로젝트 골격

목표: Unity에서 컴파일 가능한 구조와 씬 전환 뼈대를 만든다.

작업:

- `Assets/Conn` 폴더 구조 생성
- assembly definition 생성
  - `Conn.Core`
  - `Conn.Runtime`
  - `Conn.Rendering`
  - `Conn.UI`
  - `Conn.Editor`
  - `Conn.Tests`
- 기본 씬 생성
  - `Title`
  - `Town`
  - `Dungeon`
  - `Combat`
  - `Ending`
- 씬 전환 서비스 작성
- 공통 GameSession 상태 작성
- 기본 Input System 연결

완료 기준:

- Unity 에디터에서 컴파일 에러가 없다.
- Title에서 Town으로 이동할 수 있다.
- Town, Dungeon, Combat 씬을 코드로 전환할 수 있다.

## P1: Runtime Vertical Slice

목표: 첫 게임 루프를 끝까지 닫는다.

흐름:

```text
Title
-> Town
-> Quest Board
-> Gate
-> Dungeon
-> visible monster contact
-> Combat
-> Dungeon return
-> quest complete popup
-> Town reward
```

작업:

- 자유 이동 FPS PlayerController 작성
- Town 씬에 게시판 NPC와 게이트 NPC 배치
- 게시판에서 퀘스트 1개 수주
- 활성 퀘스트가 없으면 게이트 입장 차단
- 활성 퀘스트가 있으면 Dungeon 씬 로드
- Dungeon 씬에 보이는 몬스터 오브젝트 배치
- 몬스터 접촉 시 Combat 씬 전환
- 전투 시작 전 `preEncounterSnapshot` 저장
- Combat 씬에서 고정 전투 1회 처리
- 승리 시 Dungeon으로 복귀
- 승리 시 field monster placement 완료와 runtime state cleanup
- 토벌 목표 완료 시 `귀환 / 계속 탐험` 팝업
- 귀환 시 Town 복귀와 골드 보상 지급
- 사망 시 Ending 전환

완료 기준:

- 새 게임 후 퀘스트를 받고 던전에 들어갈 수 있다.
- 몬스터와 접촉하면 전투가 시작된다.
- 전투 승리 후 던전으로 돌아온다.
- 퀘스트 완료 후 귀환 정산이 된다.
- 사망 시 Ending으로 간다.

## P2: 전투와 스킬/주사위

목표: 레거시 전투 규칙을 Unity C# 도메인으로 재작성한다.

작업:

- `CombatSession`
- `Combatant`
- `EncounterDefinition`
- `FieldMonsterState`
- `DiceLoadout`
- `DiceFaceDefinition`
- `DiceRollResult`
- `DiceCombatService`
- `CombatResolver`
- `EnemyTurnResolver`
- `CombatRewardResolver`
- `CombatHudController`

필드 몬스터 관리는 [`monster/README.md`](monster/README.md)를 따른다. P1에서는 고정
배치 몬스터 접촉 트리거와 cleanup만 구현하고, patrol/chase FSM은 이후 단계로 둔다.

무기별 주사위 수:

| 장비 상태 | 주사위 수 | 추가 효과 |
| --- | ---: | --- |
| 한손무기 | 4 | 기본 |
| 한손무기 + 방패 | 3 | 방어 보정 |
| 양손무기 | 5 | 공격 중심 |
| 무기 없음 | 2 | 최소 전투 가능 |

완료 기준:

- 장착 무기에 따라 전투 주사위 개수가 달라진다.
- STOP 후 최대 3개 선택이 가능하다.
- 선택 결과가 공격/방어/회복 등으로 해석된다.
- 슬롯별 쿨다운이 적용된다.

## P3: 장비, 인벤토리, 상점

목표: 대장장이와 기술 상인까지 실제 서비스로 만든다.

작업:

- 골드 자원
- 인벤토리
- 장비 슬롯
  - 한손무기
  - 양손무기
  - 방패
  - 머리
  - 가슴
  - 팔
  - 다리
  - 신발
- 대장장이 장비 구매/판매
- 기술 상인 스킬 구매/판매
- 장착 중인 스킬 판매 방지
- 고정 아이템 판매
- generated item 계약은 문서/데이터에만 유지

완료 기준:

- 골드로 장비를 사고팔 수 있다.
- 골드로 스킬을 사고팔 수 있다.
- 장비 변경이 전투 주사위 수에 영향을 준다.

## P4: 마을 NPC 확장

목표: 모든 마을 NPC가 최소 상호작용을 가진다.

작업:

- 여관: 더미 대화 또는 즉시 회복
- 훈련소: 더미 대화
- 대장장이: 장비 상점
- 약재상: 더미 대화 또는 소모품 상점
- 기술 상인: 스킬 상점
- 학자: 더미 대화
- 게시판: 퀘스트 수주
- 게이트: 던전 입장

완료 기준:

- 마을 8개 NPC가 월드 안 서비스 제공자로 배치된다.
- 게시판/게이트/대장장이/기술 상인은 실제 기능을 가진다.
- 나머지는 더미 대화를 제공한다.

## P5: Editor Tool 1차

목표: Runtime vertical slice에 필요한 데이터 제작 도구를 만든다.

순서:

1. `Content Database`
2. `Build & Validation`
3. `Map Editor`
4. `Generator`
5. `NPC/Quest`
6. `Event Graph`

Worker 1:

- `Map Editor`
- `Generator`
- `Build & Validation`
- `MapProfile`
- `ChunkPreset`
- `RoomGraph`
- `compiledMap`

Worker 2:

- `Content Database`
- `NPC/Quest`
- `Event Graph`
- vendor, item, equipment, skill, monster, quest 데이터
- ID registry

완료 기준:

- 레거시 JSON을 Unity 데이터로 import할 수 있다.
- 퀘스트-던전-조우 참조를 검증할 수 있다.
- compiledMap 또는 임시 map data를 Runtime에서 읽을 수 있다.

## P6: Diablo식 맵 생성

목표: `diablo_map_generation_design.md`의 생성 파이프라인을 Unity Editor Tool로 옮긴다.

파이프라인:

```text
MapProfile + seed
-> RoomGraph
-> chunk/socket assembly
-> anchor placement
-> tile/decor/light pass
-> encounter/loot/quest pass
-> validation
-> compiledMap
```

완료 기준:

- 같은 profile/seed로 같은 던전 구조가 나온다.
- start, boss, quest target, exit anchor가 보장된다.
- 검증 실패한 map은 Runtime bundle에 포함되지 않는다.

## 사용자가 해줘야 할 것

- Unity Editor에서 프로젝트를 열어 컴파일 에러와 씬 로딩을 확인한다.
- Codex가 만든 씬/프리팹/ScriptableObject가 Unity에서 정상 import되는지 확인한다.
- 필요한 경우 GameObject 배치, collider 조정, 카메라 감도 같은 에디터 조작을 직접 한다.
- Unity 콘솔 에러를 복사해서 전달한다.
- Play Mode에서 실제 조작감, UI 클릭, 씬 전환이 의도와 맞는지 확인한다.
- 대형 아트/사운드/모델 에셋을 넣기 전 `git-lfs` 설치 여부를 결정한다.

## Codex가 할 수 있는 것

- C# 스크립트 작성
- asmdef 작성
- ScriptableObject 클래스 작성
- EditorWindow 코드 작성
- JSON/문서/설정 파일 작성
- Unity 프로젝트 파일 구조 정리
- Git 커밋/푸시
- Unity batchmode 실행이 가능하면 컴파일 검증

## Codex가 직접 하기 어려운 것

- Unity Editor GUI에서 Scene View 배치 조작
- Play Mode 체감 확인
- Inspector 드래그 앤 드롭
- 프리팹 시각 배치 세부 조정
- 라이선스가 필요한 Asset Store 패키지 설치
- 로컬 Unity 에디터 플러그인 설치 확인

현재 사용 가능한 MCP에는 Unity Editor를 직접 조작하는 도구가 없다. 따라서 Unity
내부 조작은 사용자가 확인하고, Codex는 파일 생성/수정과 batchmode 검증 중심으로
진행한다.

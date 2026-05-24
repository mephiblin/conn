# Codex Goal Prompt

장기 개발 세션을 시작할 때 아래 내용을 `/goal`에 넣는다.

```text
Unity 프로젝트 `/home/inri/문서/UnityProjects/My project`에서
3D FPS 던전 크롤러 RPG의 P1 Runtime Vertical Slice를 완성한다.

현재 기준:
- 문서 기준은 `doc/dev/`를 따른다.
- 레거시 웹게임은 `doc/webgame_ref/`이며, 포팅 대상이 아니라 규칙/데이터 계약 참조다.
- Runtime과 Editor Tool은 분리한다.
- 마을/던전은 자유 이동 FPS다.
- 퀘스트가 있어야 던전에 입장할 수 있다.
- 보이는 필드 몬스터와 접촉하면 `Dungeon -> Combat -> Dungeon` 흐름으로 전투한다.
- 사망은 일단 Ending으로 보낸다.

우선 완료할 범위:
1. Title -> Town -> Quest Board -> Gate -> Dungeon -> Monster Contact -> Combat -> Dungeon -> Return Popup -> Town Reward 흐름을 실제 씬 오브젝트로 닫는다.
2. 디버그 버튼만으로 진행되는 부분을 실제 상호작용 컴포넌트로 교체한다.
3. 전투는 아직 고정 결과 버튼이어도 된다. 단, 전투 전 `preEncounterSnapshot`과 승리 후 목표 완료/귀환 가능 상태는 Runtime 상태에 남긴다.
4. Unity Editor Tool은 아직 본격 구현하지 말고, P1 런타임 루프가 닫힌 뒤 Content Database와 Build & Validation부터 시작한다.
5. 변경 후 Unity batchmode 컴파일/씬 생성 검증을 실행하고, 성공하면 커밋/푸시한다.

운영 방식:
- 중간 변경은 작게 커밋한다.
- Unity가 열려 있으면 코드 작업은 계속하되, batchmode 검증이 필요할 때는 사용자에게 Unity 종료를 요청한다.
- 에디터 GUI에서 확인해야 하는 조작감, 콘솔 에러, 플레이모드 흐름은 사용자 확인이 필요하다고 명확히 말한다.
```

예상 다음 단계는 P1 완료 후 아래 목표로 갱신한다.

```text
P2 목표: 레거시 전투/스킬/주사위 규칙을 Unity C# 도메인으로 재작성하고,
장비 상태에 따라 주사위 수가 달라지는 최소 전투 루프를 구현한다.
```

## 장기 챕터용 Goal Prompt

아래 프롬프트는 작은 P단계 하나가 아니라, 기획 완성까지 가는 큰 챕터 단위로
사용한다. 한 챕터가 여러 커밋과 여러 세션에 걸쳐 진행될 수 있다.

### Chapter 1: Runtime Playable Core

P1-P4를 묶은 목표다. 먼저 플레이어가 실제 게임처럼 시작, 준비, 원정, 전투,
귀환, 보상, 상점 이용을 반복할 수 있게 만든다.

```text
Unity 프로젝트 `/home/inri/문서/UnityProjects/My project`에서
3D FPS 던전 크롤러 RPG의 Chapter 1 Runtime Playable Core를 완성한다.

문서 기준:
- `doc/dev/development_pipeline.md`
- `doc/dev/unity_game_concept_design.md`
- `doc/dev/game_features.md`
- `doc/dev/레거시_전투시스템.md`
- `doc/dev/레거시_스킬시스템.md`
- `doc/dev/monster/README.md`

핵심 원칙:
- Game Runtime과 Unity Editor Tool은 분리한다.
- 마을과 던전은 자유 이동 FPS 공간이다.
- 셀/격자는 런타임 이동용이 아니라 제작/배치용이다.
- 레거시 웹게임은 규칙과 데이터 계약 참조이며 그대로 포팅하지 않는다.
- 먼저 플레이 가능한 vertical loop를 닫고, 그 위에 전투/상점/NPC를 붙인다.

완료할 큰 범위:
1. Title -> Town -> Quest Board -> Gate -> Dungeon -> Field Monster -> Combat -> Dungeon -> Return -> Town reward 흐름을 실제 씬 오브젝트와 런타임 상태로 완성한다.
2. Combat 씬에 레거시 전투/스킬/주사위 규칙의 최소 C# 도메인을 구현한다.
3. 무기 상태에 따라 주사위 수가 달라지게 한다.
   - 한손무기: 4개
   - 한손무기 + 방패: 3개 + 방어 보정
   - 양손무기: 5개
   - 무기 없음: 2개
4. 인벤토리, 장비 슬롯, 골드 경제를 구현한다.
5. 대장장이 장비 구매/판매와 기술 상인 스킬 구매/판매를 구현한다.
6. 마을 NPC 8종을 월드 안 서비스 제공자로 배치한다.
   - 여관, 훈련소, 대장간, 약재상, 기술 상인, 학자, 임무 게시판, 문/게이트
7. 필드 몬스터는 placement definition과 runtime state를 분리한다.
8. 사망은 일단 Ending으로 보낸다.

완료 기준:
- 새 게임부터 퀘스트 수주, 던전 입장, 몬스터 접촉, 전투 승리, 귀환 정산까지 플레이 가능하다.
- 전투는 고정 버튼이 아니라 주사위/스킬 선택 결과로 승패가 처리된다.
- 골드로 장비와 스킬을 사고팔 수 있다.
- 장비 변경이 전투 주사위 수에 영향을 준다.
- Unity batchmode 컴파일/씬 생성 검증이 통과한다.
- 중요한 변경 단위마다 커밋/푸시한다.

운영 방식:
- 구현 전 기존 코드와 문서를 먼저 읽는다.
- 변경은 P1, P2, P3, P4처럼 작은 커밋 단위로 쪼갠다.
- Unity가 열려 있으면 코드 작업은 계속하되, batchmode 검증이 필요할 때는 사용자에게 Unity 종료를 요청한다.
- 사용자가 직접 확인해야 하는 조작감, 카메라, 콜라이더, 콘솔 에러는 명확히 전달한다.
```

### Chapter 2: Data And Editor Pipeline

P5-P6를 묶은 목표다. Runtime Playable Core가 어느 정도 닫힌 뒤, 데이터를
하드코딩에서 빼내고 Unity Editor Tool로 제작/검증/빌드하는 구조를 만든다.

```text
Unity 프로젝트 `/home/inri/문서/UnityProjects/My project`에서
Chapter 2 Data And Editor Pipeline을 완성한다.

문서 기준:
- `doc/dev/development_pipeline.md`
- `doc/dev/unity_game_concept_design.md`
- `doc/dev/레거시_에디터참조.md`
- `doc/dev/diablo_map_generation_design.md`
- `doc/dev/추가_의문사항.md`
- `doc/webgame_ref/`

핵심 원칙:
- Editor Tool은 플레이어 빌드에 포함하지 않는다.
- Editor Tool은 인게임 에디터가 아니라 Unity EditorWindow, Inspector, ScriptableObject, Scene View 도구다.
- Runtime은 검증된 콘텐츠 산출물만 읽는다.
- 에디터 프로젝트 상태와 플레이어 저장 상태를 섞지 않는다.
- 레거시 웹 에디터 파일은 기능 책임과 데이터 계약 참조로만 사용한다.

완료할 큰 범위:
1. Content Database를 만든다.
   - item, equipment, skill, monster, quest, vendor, npc 정의
   - ID registry와 definition lookup
   - 레거시 JSON import 경로
2. Build & Validation 도구를 만든다.
   - 미정의 ID 검출
   - 퀘스트-던전-조우 참조 검증
   - vendor stock, skill catalog, reward 참조 검증
   - validation report 출력
3. Map Editor 1차를 만든다.
   - 제작용 grid/cell 기준
   - room/chunk/preset 배치
   - monster/encounter/loot/exit anchor placement
4. Generator Workbench를 만든다.
   - `MapProfile + seed -> RoomGraph -> chunk/socket assembly -> placement pass -> validation -> compiledMap`
   - 같은 seed는 같은 구조를 만든다.
5. NPC/Quest Editor를 만든다.
   - 마을 NPC 서비스
   - 퀘스트 후보, 보상, 목표 monster/encounter 연결
   - 게시판 리롤 규칙
6. Event Graph는 후순위로 두되, 데이터 계약과 확장 지점은 확보한다.

권장 EditorWindow:
- Content Database
- Build & Validation
- Map Editor
- Generator
- NPC/Quest
- Event Graph

완료 기준:
- 레거시 JSON을 Unity 데이터로 import할 수 있다.
- Runtime vertical slice가 하드코딩이 아니라 제작 데이터 일부를 읽는다.
- 검증 실패한 데이터는 Runtime bundle에 포함되지 않는다.
- compiledMap 또는 임시 map data를 Runtime에서 읽을 수 있다.
- 같은 profile/seed로 같은 던전 구조가 나온다.
- start, boss, quest target, exit anchor가 보장된다.
- Unity batchmode 검증이 통과한다.
- 중요한 변경 단위마다 커밋/푸시한다.

운영 방식:
- 외형 UI보다 데이터 계약과 검증을 먼저 만든다.
- Editor 코드는 `Conn.Editor`에 두고 player build에 포함하지 않는다.
- Runtime 코드는 EditorWindow를 참조하지 않는다.
- 새로운 데이터 형식은 문서와 validation rule을 함께 갱신한다.
```

# Conan: Labyrinths of the Serpent 게임 기능 기준

이 문서는 현재 Unity 프로젝트에서 만들 게임의 기능 기준이다. 과거 웹게임은
레거시 참고 자료로만 사용하며, 웹 구현의 DOM, LocalStorage, Three.js 화면 구성,
인게임 에디터 UI는 이식 대상이 아니다. 유지할 것은 검증된 게임 루프, 콘텐츠
구조, 전투 기본 로직, 던전 탐험 감각이다.

Unity 재개발의 상위 원칙은 [`unity_game_concept_design.md`](unity_game_concept_design.md)를
우선한다.

레거시 전투 기본 로직과 스크린샷은 [`레거시_전투시스템.md`](레거시_전투시스템.md)를
참고한다. 필요한 레거시 웹게임 스크립트는 [`webgame_ref/src/`](../webgame_ref/src/)에
보관한다.

레거시 스킬/주사위 loadout 구조는 [`레거시_스킬시스템.md`](레거시_스킬시스템.md)를
참고한다.

Unity Editor Tool 구현 시 참조할 레거시 에디터 파일 목록은
[`레거시_에디터참조.md`](레거시_에디터참조.md)를 따른다.

Diablo식 던전 생성과 에디터 데이터 설계는
[`diablo_map_generation_design.md`](diablo_map_generation_design.md)를 따른다.

## 한 줄 소개

`Conan: Labyrinths of the Serpent`는 3D 1인칭 FPS 시점의 던전 크롤러 RPG다.
플레이어는 국경 도시에서 원정을 준비하고, 고대 뱀 신의 폐허와 연결된 지하
미궁을 직접 걸어 다니며 전투, 이벤트, 전리품, 저주, 퀘스트를 처리한다.

## 현재 개발 방향

- Unity 엔진 기반의 독립 실행 게임으로 개발한다.
- 플레이어 이동은 자유 이동 FPS로 처리한다.
- 셀/격자는 플레이어 이동 스냅이 아니라 Unity Editor Tool의 맵 생성, 오브젝트
  생성, 조우 배치 기준으로 사용한다.
- 플레이어용 Game Runtime과 제작자용 Editor Tool을 분리한다.
- 과거 웹 프로토타입에 있던 인게임 에디터는 플레이어 빌드에 포함하지 않는다.
- 맵, 아이템, 몬스터, 스킬, 퀘스트, 이벤트 생성과 검증은 Unity Editor 확장으로
  만든다.
- Game Runtime은 검증된 콘텐츠 데이터와 빌드된 월드만 읽는다.
- 런타임 저장 데이터와 제작 에디터 프로젝트 데이터는 절대 섞지 않는다.

## 핵심 플레이 경험

- 1인칭 3D 시점으로 어두운 던전을 탐험한다.
- 마을과 던전은 자유 이동 FPS 공간으로 돌아다닌다.
- 셀/격자는 런타임 이동 방식이 아니라 제작 도구의 맵/오브젝트 배치 기준이다.
- 마을에서 장비, 치료, 스킬, 보급, 퀘스트를 준비한다.
- 던전에서는 체력, 회복 수단, 식량, 횃불, 상태 이상, 저주를 관리한다.
- 전투는 별도 인스턴스에서 진행되는 주사위/릴 기반 턴제로 구성한다.
- 스킬덱과 장비는 전투뿐 아니라 탐험, 함정, 이벤트 선택지에도 영향을 준다.
- 귀환 후 보상 정산, 치료, 장비 교체, NPC 반응, 월드 상태 변화를 반영한다.

## 기본 게임 루프

1. 타이틀에서 새 게임 또는 이어하기를 선택한다.
2. 새 게임이면 주인공의 이름과 시작 보급을 정한다.
3. 마을 허브에서 NPC 서비스를 이용해 원정을 준비한다.
4. 임무 게시판 NPC에게 퀘스트를 하나 수주한다.
5. 마을의 문 또는 게이트 NPC와 상호작용해 수주 중인 퀘스트의 던전으로 이동한다.
6. 던전에 진입해 방, 복도, 문, 함정, NPC, 이벤트, 조우를 탐색한다.
7. 보이는 몬스터와 접촉하면 전투 씬으로 전환한다.
8. 전투 결과, 전리품, 퀘스트 조건, 상태 이상, 월드 상태를 반영한다.
9. 전투 후 던전으로 복귀한다.
10. 퀘스트 목표를 달성하면 탈출 선택권이 열린다.
11. 더 탐험할지 귀환할지 선택한다.
12. 마을로 돌아와 보상, 획득 아이템, 손실을 정산하고 다음 원정을 준비한다.

## 씬 흐름

게임의 기본 씬 흐름은 다음과 같다.

```text
Title
  -> New Game / Continue
  -> Town
  -> Dungeon
  -> Combat
  -> Dungeon
  -> Town / Ending
```

씬별 책임은 명확히 나눈다.

- `Title`: 새 게임, 이어하기, 설정, 종료
- `New Game`: 캐릭터 생성과 시작 보급 선택
- `Town`: NPC 상호작용, 거래, 회복, 스킬 구매, 퀘스트 수주, 던전 출발
- `Dungeon`: 3D FPS 던전 탐험, 이벤트, 함정, 전리품, 보이는 몬스터 접촉 조우
- `Combat`: 몬스터 조우 시 진입하는 별도 전투 씬
- `Ending`: 사망, 주요 퀘스트 실패/성공, 캠페인 결말 처리

전투는 던전 안의 오버레이가 아니라 독립 전투 씬으로 취급한다. 전투 종료 후에는
원래 던전 세션으로 복귀해야 하므로, 던전 위치, 방향, 진행 중인 퀘스트, 필드 상태,
인벤토리 변화를 보존하는 전환 계약이 필요하다.

## Game Runtime 범위

Game Runtime은 플레이어가 실제로 실행하는 게임이다. 다음 기능만 포함한다.

- 타이틀, 새 게임, 이어하기, 설정
- 캐릭터 생성과 시작 보급 선택
- 마을 허브와 NPC 서비스
- 3D FPS 자유 이동 던전 탐험, 자동 지도, 상호작용
- 전투, 보상, 패배, 귀환 처리
- 인벤토리, 장비, 스킬덱
- 퀘스트 상태와 월드 상태 관리
- 저장/불러오기와 저장 데이터 마이그레이션

Game Runtime에는 다음을 넣지 않는다.

- 맵 편집 UI
- 아이템/몬스터/스킬 직접 생성 UI
- 퀘스트/이벤트 그래프 편집 UI
- 디버그용 생성기 패널
- 검증되지 않은 외부 제작 데이터 직접 로드

## Unity Editor Tool 범위

제작 도구는 Unity Editor 안에서 개발한다. 초기에는 EditorWindow와 ScriptableObject
기반으로 시작하고, 필요해질 때 독립 제작 툴로 확장할 수 있다.

에디터는 게임 안에 포함되는 모드가 아니다. 게임에 쓰이는 데이터들을 생성하고
관리하는 제작 파이프라인이며, 실제 게임 씬과 분리된다. 과거 웹게임처럼 인게임
에디터를 만들지 않는다. Unity에서는 Inspector, Custom Inspector, EditorWindow,
필요 시 제작 전용 씬 형태로 구성한다.

필수 제작 기능은 다음과 같다.

- 맵/층 편집: 셀, 방, 복도, 문, 계단, 배치, 조명, 재질
- 콘텐츠 편집: 몬스터, 조우, 스킬, 아이템, 상점, 전리품, NPC, 퀘스트, 이벤트
- 이벤트 그래프: 선택지, 조건, 보상, 상태 변경
- 검증: 누락 ID, 끊긴 참조, 도달 불가 셀, 잘못된 보상, 누락 에셋 탐지
- 빌드: runtime content bundle, compiled map, build manifest 생성
- 테스트 플레이: 릴리즈 저장 슬롯을 오염시키지 않는 임시 세션 실행

Editor Tool은 개발자/디자이너용 도구이며 플레이어용 게임 메뉴가 아니다.
최종 플레이어 빌드에는 제작용 씬, EditorWindow, Custom Inspector, 검증 대시보드,
생성기 UI를 포함하지 않는다.

최종 EditorWindow 구성은 `Map Editor`, `Generator`, `Content Database`,
`Event Graph`, `NPC/Quest`, `Build & Validation`의 6개를 기준으로 한다.

## 주요 시스템

### 탐험

탐험은 Unity 3D FPS 자유 이동을 사용한다. 플레이어는 마을과 던전을 직접 걸어
다니며 시야를 자유롭게 둘러본다. 셀 스냅 이동은 사용하지 않는다.

셀/격자는 Unity Editor Tool에서 맵 생성, 오브젝트 생성, 방/복도 구성, 조우 배치,
자동 지도 기초 데이터를 만들 때 사용하는 제작 기준이다. 런타임에서는 이 데이터를
충돌체, 트리거, 상호작용 지점, 조우 영역으로 변환해 사용한다.
던전 생성은 `profile -> room graph -> chunk/socket assembly -> tile/decor pass -> compiled map`
흐름을 따른다.

던전은 방, 복도, 일반 문, 잠긴 문, 비밀문, 계단, 안전지대, 위험지대, 이벤트
지점, 전투 지점으로 구성한다. 자동 지도는 플레이어가 확인한 정보만 표시한다.

### 마을

마을은 원정 사이의 전략 허브다. 여관, 치료, 훈련, 대장간, 약재상, 스킬 상인,
학자, 게시판, 문지기 같은 NPC 서비스를 제공한다.

마을은 원정 허브다. 마을 NPC는 메뉴가 아니라 월드 안에 배치된 서비스 제공자다.
플레이어는 NPC를 바라보거나 접근한 뒤 상호작용해 회복, 거래, 훈련, 정보 확인,
퀘스트 수주, 던전 출발 같은 서비스를 이용한다. 퀘스트 결과, 평판, 저주, NPC 생존,
보물 반입 여부가 서비스 조건과 대사를 바꿀 수 있다.

마을의 기본 진행 방식은 몬스터헌터식 허브 구조를 따른다. 플레이어는 마을에서
임무 게시판 NPC에게 퀘스트를 받고, 준비를 마친 뒤 문 또는 게이트 NPC와
상호작용해 해당 퀘스트가 지정한 던전으로 이동한다.

현재 기준 마을 NPC는 다음 8종이다.

- 여관: 체력과 상태 회복
- 훈련소: 능력 성장, 전투 감각 훈련, 기초 성장 서비스
- 대장간: 무기와 방어구 거래
- 약재상: 회복 아이템과 소모품 거래
- 기술 상인: 스킬 구매와 스킬덱 관련 서비스
- 학자: 정보, 감정, 단서, 사원/고대 지식 서비스
- 임무 게시판: 퀘스트 목록 표시와 수주
- 문/게이트: 현재 수주 중인 퀘스트의 던전으로 출발

마을 구현 참고 파일은 [`webgame_ref/src/townRuntime.js`](../webgame_ref/src/townRuntime.js)와
[`webgame_ref/src/data/npcs.json`](../webgame_ref/src/data/npcs.json)이다.

퀘스트는 한 번에 1개만 소지할 수 있다. 이미 퀘스트를 받은 상태에서 다른
퀘스트를 받으려면 기존 퀘스트를 포기하거나 완료해야 한다.

임무 게시판의 퀘스트 목록은 고정 목록이 아니다. 일정 시간이 지나거나 던전을
클리어할 때마다 다시 추첨된다. 초기 구현에서는 타이머 리롤과 던전 클리어 리롤을
모두 지원하되, 실제 밸런스 값은 데이터로 조정한다.

퀘스트가 있어야만 던전에 입장할 수 있다. 초기 퀘스트는 단순 토벌형으로 제한하고,
성공 조건은 지정 몬스터 제거, 실패 조건은 사망으로 둔다. 목표 달성 후에는 던전
안에서 즉시 `귀환 / 계속 탐험` 팝업이 뜬다. `계속 탐험`을 선택하면 HUD 귀환 버튼이
활성화된다. 귀환하면 몬스터헌터식으로 보상과 획득 아이템을 정산한다.

### 전투

전투는 별도 씬/상태 머신으로 진행되는 턴제 전투다. 던전에서 직접 싸우는 방식이
아니라 `Dungeon -> Combat -> Dungeon` 흐름이다. 던전에서 보이는 몬스터와 접촉하면
전투 씬으로 전환하고, 전투 결과를 반영한 뒤 원래 던전 세션으로 복귀한다.

던전 필드의 몬스터 상태 관리는 [`monster/README.md`](monster/README.md)를 따른다.
몬스터 placement는 Editor/compiledMap 데이터이고, `FieldMonsterState`는 Runtime
save에 들어가는 상태 delta다. 승리 후에는 placement 완료와 runtime state cleanup을
모두 수행한다.

주사위/릴 결과를 멈추고 사용 가능한 결과를 선택해 이번 턴의 행동을 구성한다.

전투 시스템은 다음 요소를 포함한다.

- 레거시 기본 로직: [`레거시_전투시스템.md`](레거시_전투시스템.md)
- 전투 세션, 턴, 승패, 보상: [`webgame_ref/src/combatRuntime.js`](../webgame_ref/src/combatRuntime.js)
- 주사위/스킬 릴 규칙: [`webgame_ref/src/diceCombatRuntime.js`](../webgame_ref/src/diceCombatRuntime.js)
- 스킬 데이터: [`webgame_ref/src/data/skills.json`](../webgame_ref/src/data/skills.json)
- 주사위/릴 결과와 선택
- 스킬덱과 주사위 면 장착
- 기본 공격 대체 규칙
- 스킬 쿨다운
- 아이템 사용
- 적 행동과 보스 패턴
- 상태 이상, 버프, 디버프, 흡혈, 회복
- 보상, 도주, 패배 처리

### 스킬과 스킬덱

스킬 시스템은 주사위 loadout 기반이다. 캐릭터는 장착 무기가 정한 개수의 6면
주사위를 가지고, 각 면에는 눈 값과 스킬 ID를 배치한다. 전투에서는 각 주사위에서
나온 roll 중 최대 3개를 선택해 효과를 발동한다.

주사위의 개수는 장착한 무기가 정한다. 스킬에는 현재 직업 제한을 두지 않는다.
스킬은 중복 소지가 가능하고, 주사위 면마다 스킬을 1개씩 개별 장착할 수 있다.

무기별 주사위 수는 다음을 기준으로 한다.

- 한손무기: 4개
- 한손무기 + 방패: 3개 + 방어 보정
- 양손무기: 5개
- 무기 없음: 2개

자세한 레거시 구조는 [`레거시_스킬시스템.md`](레거시_스킬시스템.md)를 따른다.
Unity에서는 이를 `SkillDefinition`, `SkillInventory`, `DiceLoadout`,
`DiceFaceDefinition`, `SkillFormulaEvaluator`, `SkillEffectResolver`로 재설계한다.

### 캐릭터

주인공은 능력치, 장비, 보유 스킬, 스킬덱, 상태 이상으로 정의한다. 현재 범위에는
직업과 동료 시스템을 넣지 않는다. 초기 전투, UI, 저장, 퀘스트 조건은 주인공 1명
기준으로 구성한다.

### 인벤토리와 장비

인벤토리는 탐험 압박을 만드는 시스템이다. 장비, 소비품, 전리품, 저주받은 물건,
감정되지 않은 유물, 퀘스트 물품을 다룬다.

상점 경제는 골드 1종을 기준으로 시작한다. 초기에는 제작 없이 구매와 판매만
지원한다.

장비 슬롯은 일단 한손무기, 양손무기, 방패, 머리, 가슴, 팔, 다리, 신발로 제한한다.
무기는 스킬/주사위 시스템과 연결되며, 전투에서 사용하는 주사위의 개수를 정한다.
장비는 공격력과 방어력뿐 아니라 이후 이동, 함정, 협상, 해독, 저항, 특정 이벤트
선택지에도 영향을 줄 수 있다.

### 상점

상점은 Diablo II식 진행도 기반 재고와 무작위 갱신을 참고한다. 초기 경제는 골드
1종이며, 제작 없이 구매와 판매만 지원한다.

일반 vendor는 `VendorDefinition`의 기본 비용, 서비스 타입, 재고를 사용한다.
`rotation` 규칙으로 층, 보스 처치 수, 퀘스트 플래그 같은 진행도에 따라 가격과
판매 목록을 바꿀 수 있다.

기술 상점은 `catalogId`에 속한 후보 스킬 중 `stockSize`만큼을 현재 재고로 뽑고,
`refreshSeconds` 주기로 목록을 갱신한다. 스킬 구매/판매 가격은 스킬 정의의
`buyPrice`, `sellPrice`를 사용한다. 장착 중인 스킬 카드는 판매할 수 없고, 장착하지
않은 느슨한 카드만 판매할 수 있다.

장비 상점은 단순 item id 판매뿐 아니라 `generated`, `rarityId`, `affixPoolId`를
가진 generated offer를 지원할 수 있다. 이 경우 구매 시 접두/접미/희귀도 기반
아이템 인스턴스를 생성해 인벤토리에 넣는다.

첫 구현에서는 고정 아이템만 판매한다. generated item은 데이터 계약과 문서에 남겨
두고, 이후 장비 상점과 전리품 확장 단계에서 연결한다.

상점 구현 참고 파일은 다음이다.

- [`webgame_ref/src/data/vendors.json`](../webgame_ref/src/data/vendors.json)
- [`webgame_ref/src/npcRuntime.js`](../webgame_ref/src/npcRuntime.js)
- [`webgame_ref/src/inventoryRuntimeBridge.js`](../webgame_ref/src/inventoryRuntimeBridge.js)
- [`webgame_ref/src/renderSkillShop.js`](../webgame_ref/src/renderSkillShop.js)
- [`webgame_ref/src/data/npcs.json`](../webgame_ref/src/data/npcs.json)
- [`webgame_ref/src/data/skills.json`](../webgame_ref/src/data/skills.json)

### 퀘스트와 이벤트

퀘스트는 마을 게시판식 목표와 던전 내부 사건을 모두 포함한다. 이벤트는 선택지,
조건, 보상, 피해, 상태 변화, NPC 관계 변화를 만든다.

선택지는 명백한 정답보다 보상과 손실을 동시에 제시한다. 탐욕, 생존, 배신, 저주,
고대 신앙, 용병 계약을 주요 동기로 사용한다.

초기 퀘스트 시스템은 다음 제약을 가진다.

- 플레이어는 활성 퀘스트를 1개만 가질 수 있다.
- 퀘스트는 목적 던전, 토벌 목표, 보상, 실패/포기 정책을 가진다.
- 게이트는 활성 퀘스트가 있을 때만 던전 출발을 허용한다.
- 목표 달성 후 탈출 선택권이 생긴다.
- 귀환 시 퀘스트 보상, 획득 아이템, 실패/진행 상태를 정산한다.
- 사망은 퀘스트 실패 조건이다.
- 초기 퀘스트 보상은 골드만 지급한다.
- 게시판 목록은 타이머 또는 던전 클리어 이벤트로 리롤된다.

## 콘텐츠 데이터 원칙

콘텐츠는 런타임 코드에 흩어 쓰지 않는다. Unity에서 다음 계층으로 관리한다.

- Definition: 스킬, 아이템, 몬스터, NPC, 퀘스트, 이벤트 같은 재사용 정의
- World: 마을, 던전 층, 방, 셀, 문, 배치, 조명, 재질
- Encounter: 전투 배치, 적 그룹, 보상, 전투 배경, 난이도
- Rule: 공식, 조건, 상태 이상, 보상 테이블, 생성 규칙
- Build Manifest: 특정 게임 빌드에 포함되는 콘텐츠와 월드 목록

2026-05-25 기준 구현 계약:

- Encounter는 `pattern`, `rewardId`, primary monster, enemy slot/list를 가진다.
- Runtime 전투 실행은 아직 단일 primary monster를 유지하지만, enemy slot 상태를 CombatSessionState와 HUD에 노출한다.
- 장비 데이터는 generated item 확장을 위해 `generated`, `rarityId`, `affixPoolId` 필드를 가진다.
- compiledMap placement는 start, quest target, boss, exit, monster, loot를 검증한다.
- NPC의 `quest_seed_` 참조는 board quest가 아닌 NPC seed 네임스페이스로 유지한다.

초기 구현은 ScriptableObject와 JSON 직렬화 가능한 DTO를 함께 사용한다. 에디터는
제작 친화적인 ScriptableObject를 다루고, 런타임 빌드는 검증된 DTO 또는 번들을
읽는 구조를 목표로 한다.

## 레거시 웹게임 참고 자료

레거시 웹게임은 Unity로 그대로 포팅하지 않는다. 다음 파일들은 동작을 이해하고
Unity C# 시스템으로 재설계할 때 참고한다.

- [`webgame_ref/src/combatRuntime.js`](../webgame_ref/src/combatRuntime.js): 전투 시작,
  영웅/적 턴, 도주, 아이템, 승리/패배 처리
- [`webgame_ref/src/diceCombatRuntime.js`](../webgame_ref/src/diceCombatRuntime.js):
  주사위 릴, 선택 제한, 쿨다운, 공격/회복/방어/버프/디버프 해석
- [`webgame_ref/src/diceSkillLoadout.js`](../webgame_ref/src/diceSkillLoadout.js):
  스킬 카드 장착/해제와 주사위 loadout 관리
- [`webgame_ref/src/renderSkillDeck.js`](../webgame_ref/src/renderSkillDeck.js):
  스킬덱 UI 참고
- [`webgame_ref/src/renderSkillShop.js`](../webgame_ref/src/renderSkillShop.js):
  기술 상인 UI 참고
- [`webgame_ref/src/renderCombat.js`](../webgame_ref/src/renderCombat.js): 전투 UI 상태와
  릴 표시 방식
- [`webgame_ref/src/combatScene3d.js`](../webgame_ref/src/combatScene3d.js): 전투 장면
  표시 참고
- [`webgame_ref/src/townRuntime.js`](../webgame_ref/src/townRuntime.js): 마을 맵, NPC 배치,
  마을 NPC 서비스 패치 참고
- [`webgame_ref/src/data/npcs.json`](../webgame_ref/src/data/npcs.json): NPC 정의와 서비스
  정의 참고
- [`webgame_ref/src/data/*.json`](../webgame_ref/src/data/): 스킬, 몬스터, 조우, 아이템,
  퀘스트 등 초기 데이터 참고

## 첫 Vertical Slice

초기 목표는 모든 시스템을 완성하는 것이 아니라, 실제 게임 루프가 Unity에서
한 번 닫히는 것이다.

구체적인 개발 순서는 [`development_pipeline.md`](development_pipeline.md)를 따른다.

1. 타이틀에서 새 게임 시작
2. 캐릭터 생성
3. 마을 허브 진입
4. 임무 게시판에서 퀘스트 1개 수주
5. 게이트 NPC와 상호작용해 퀘스트 던전 입장
6. 작은 던전 한 층 탐험
7. 고정 조우 전투 1회
8. 보상 획득
9. 마을 귀환과 퀘스트 정산
10. 게시판 퀘스트 리롤 확인
11. 저장/불러오기 확인

## 개발 필요 항목 카운트

현재 개념을 실제 개발 항목으로 세면 최소 단위는 다음과 같다.

| 분류 | 필요 항목 수 | 내용 |
| --- | ---: | --- |
| 씬 | 5 | Title/New Game, Town, Dungeon, Combat, Ending |
| 핵심 전환 | 6 | Title->Town, Town->Dungeon, Dungeon->Combat, Combat->Dungeon, Dungeon->Town, Town->Ending |
| 마을 NPC 서비스 | 8 | 여관, 훈련소, 대장간, 약재상, 기술 상인, 학자, 임무 게시판, 게이트 |
| 주요 UI | 11 | 타이틀, 캐릭터 생성, NPC 대화, 여관/회복, 훈련, 상점, 인벤토리, 장비, 스킬, 퀘스트 게시판, 전투 HUD |
| 런타임 시스템 | 12 | 씬 전환, 플레이어 상태, NPC 상호작용, 골드 거래, 인벤토리, 장비, 무기별 주사위 수, 스킬 보유, 스킬덱, 퀘스트, 던전 탐험, 전투 |
| 데이터 정의 | 11 | NPC, vendor, 상점 catalog, 아이템, generated offer, 장비, 스킬, 퀘스트, 던전, 몬스터, 조우 |
| 저장 데이터 | 9 | 플레이어, 골드, 인벤토리, 장비, 스킬 보유, 스킬덱, 활성 퀘스트, 게시판 상태, 월드 진행 |
| Editor Tool | 6 | NPC 편집, 상점 편집, 아이템 편집, 스킬 편집, 퀘스트 편집, 던전/조우 편집 |

초기 vertical slice의 최소 구현 카운트는 `씬 4개`, `NPC 2개`, `상점 0개`,
`퀘스트 1개`, `던전 1개`, `조우 1개`, `몬스터 1종`, `보상 1종`으로 잡을 수 있다.
여기서 NPC 2개는 임무 게시판과 게이트다. 여관, 훈련소, 대장간, 약재상, 기술
상인, 학자는 두 번째 단계에서 붙여도 루프 검증에는 문제가 없다.

## Git 기준

이 프로젝트는 Unity 프로젝트 루트를 Git 저장소로 사용한다.

- 포함: `Assets/`, `Packages/`, `ProjectSettings/`, `doc/`, `.gitignore`, `.gitattributes`
- 제외: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, 빌드 산출물, IDE 임시 파일
- Unity 설정은 `Visible Meta Files`와 `Force Text` 직렬화를 유지한다.
- 대형 바이너리 에셋은 `git-lfs` 설치 후 LFS 대상으로 관리한다.

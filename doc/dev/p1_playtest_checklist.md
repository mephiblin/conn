# P1 Play Mode 테스트 체크리스트

이 문서는 Unity Play Mode에서 P1 Runtime Vertical Slice를 사람이 직접 확인하기 위한
체크리스트다. 자동 검증은 `Conn > Build & Validate Chapter 1`과 batchmode validator가
담당하지만, 조작감, collider, HUD 위치, 실제 씬 전환 체감은 직접 확인해야 한다.

## 시작 전

1. Unity Editor에서 `Conn > Build P0 Scenes`를 실행한다.
2. Console에 컴파일 에러가 없는지 확인한다.
3. `Assets/Conn/Scenes/Title.unity`를 연다.
4. Play Mode를 시작한다.

## 2026-05-25 Codex 확인 상태

- 자동 검증: `/home/inri/Unity/Hub/Editor/6000.4.8f1/Editor/Unity` batchmode 기준 Chapter 1/2 통과.
- 데이터 계약: encounter enemy slot/list, generated item 필드, NPC `quest_seed_` 네임스페이스, compiledMap monster/loot placement 추가 확인.
- UI 계약: Title/Town/Dungeon/Combat/Ending에 Runtime uGUI Canvas, CanvasScaler, EventSystem, scene별 panel root 생성/검증 추가.
- 기존 IMGUI overlay와 상호작용 prompt는 fallback/debug 플래그가 켜질 때만 보이도록 유지한다.
- 제한: 이 실행 환경에서는 실제 Unity Game view를 사람이 보는 Play Mode 수동 조작은 수행하지 못했다. 아래 항목은 Editor에서 직접 체크해야 하는 잔여 수동 확인으로 유지한다.

## 기본 루프

아래 순서가 끊기지 않아야 한다.

1. Title
   - `New Game` 클릭
   - Town으로 이동하는지 확인
   - uGUI Title 버튼이 보이고 IMGUI fallback이 기본으로 숨겨져 있는지 확인

2. Town
   - 퀘스트 게시판을 바라보고 `E`
   - Quest Board 패널이 열리는지 확인
   - uGUI Town HUD, Quest Board, Shop, Character/Inventory, Notice panel이 겹치지 않는지 확인
   - 첫 제안이 ContentDatabase quest(`quest_twisted_temple_clear`) 기반 target/encounter/map profile을 표시하는지 확인
   - `Reroll Board`가 현재 제안을 바꾸는지 확인
   - `Accept Quest` 클릭
   - HUD에 활성 퀘스트가 표시되는지 확인

3. Gate
   - 게이트를 바라보고 `E`
   - Dungeon으로 이동하는지 확인
   - 새 게임 직후 퀘스트 없이 게이트를 눌렀을 때는 입장이 막혀야 한다.

4. Dungeon
   - HUD에 활성 퀘스트, 목표, 필드 몬스터 상태가 표시되는지 확인
   - compiledMap start/quest target/exit placement 기반 marker가 등록되는지 확인
   - compiledMap monster/loot placement 목록이 생성되는지 확인
   - 몬스터 오브젝트가 quest target placement 기준으로 보이는지 확인
   - 몬스터와 접촉하면 Combat으로 이동하는지 확인
   - 접촉 로그가 중복으로 여러 번 찍히지 않는지 확인

5. Combat
   - uGUI Enemy Stage, Command, Dice, Combat Log, Status panel이 보이는지 확인
   - 주사위 face 버튼이 장비 주사위 수와 일치하는지 확인
   - 최대 3개까지 선택 가능한지 확인
   - `Resolve Selected Dice`로 공격/방어/회복 메시지가 갱신되는지 확인
   - 승리 시 Dungeon으로 돌아가는지 확인

6. Dungeon Return
   - HUD가 `Target defeated`, `Return: available`로 바뀌는지 확인
   - 최초 완료 시 `Return Now / Keep Exploring` 선택지가 보이는지 확인
   - `Keep Exploring` 후에도 `Return To Town` 버튼이 남는지 확인
   - `Return To Town` 클릭 시 Town으로 돌아가는지 확인

7. Town Reward
   - 골드 보상이 지급되는지 확인
   - Last reward가 표시되는지 확인
   - 퀘스트 게시판 제안이 리롤되었는지 확인

## 전투 실패 루프

현재 사망 정책은 다음과 같다.

- HP가 0이 되면 Ending으로 이동한다.
- Ending 상태는 저장된다.
- Continue는 Ending으로 복귀한다.
- Ending의 `New Game`이 새 저장으로 덮어쓴다.

확인 순서:

1. Combat에서 일부러 패배하거나 HP를 낮춘 상태로 전투를 진행한다.
2. Ending으로 이동하는지 확인한다.
3. Ending HUD가 `Result: death`를 표시하는지 확인한다.
   - uGUI Ending Result panel과 Back To Title/New Game 버튼이 보이는지 확인한다.
4. `Back To Title` 클릭 후 `Continue`를 누른다.
5. Ending으로 돌아오는지 확인한다.
6. Ending에서 `New Game`을 누르면 Town으로 새 게임이 시작되는지 확인한다.

## 마을 서비스

Town에서 아래 NPC를 바라보고 `E`로 상호작용한다.

| NPC | 확인 항목 |
| --- | --- |
| Inn | HP 회복 |
| Trainer | XP 5 소모, Max HP 증가 |
| Blacksmith | 장비 구매/판매, 방어구 구매, 장착 중 장비 판매 방지 |
| Apothecary | ContentDatabase item lookup으로 포션 구매 |
| Skill Merchant | 제한 stock 표시, 스킬 구매/판매 |
| Scholar | 현재 퀘스트 또는 게시판 힌트 |
| Quest Board | 리롤, 수주, 활성 퀘스트 1개 제한 |
| Gate | 퀘스트 없을 때 입장 차단, 퀘스트 있을 때 입장 |

## 장비와 스킬

1. Character 패널을 연다.
2. 장비를 직접 장착한다.
3. 장비 상태별 주사위 수가 맞는지 확인한다.
   - 한손무기: 4
   - 한손무기 + 방패: 3
   - 양손무기: 5
   - 무기 없음: 2
4. 양손무기 상태에서 방패를 장착하면 한손무기+방패로 정리되는지 확인한다.
5. 스킬 face를 순환해 전투 face가 바뀌는지 확인한다.

## 조작감 확인

- 마우스 감도: 기본값 `0.14`, 이전 테스트에서 높았던 값을 낮춘 상태. 2026-05-25 코드 기본값 재확인.
- 이동 속도: 기본값 `4.5`. 2026-05-25 코드 기본값 재확인.
- 카메라 높이
- NPC 상호작용 거리: 기본값 `4.0`. 2026-05-25 코드 기본값 재확인.
- 몬스터 접촉 collider 크기
- HUD 버튼이 화면 밖으로 밀리는지
  - 2026-05-25 자동 검증: IMGUI overlay와 `E` 상호작용 prompt rect가 320x240/220x160 화면 안에 클램프되는지 확인.
  - 2026-05-25 자동 검증: Runtime uGUI panel root normalized safe rect와 CanvasScaler contract 확인.
  - 남은 수동 확인: 실제 Play Mode Game view에서 긴 notice/상점 목록 스크롤 가독성 확인.
- Combat 버튼이 클릭하기 쉬운지

## 이번 자동 검증에 포함된 항목

- 2026-05-25 `/home/inri/Unity/Hub/Editor/6000.4.8f1/Editor/Unity` batchmode 기준 `Conn > Build & Validate Chapter 1` 통과
- 2026-05-25 같은 Unity 경로 기준 `Conn > Build & Validate Chapter 2` 통과
- 2026-05-25 같은 Unity 경로 기준 encounter enemy slot/list 계약과 primary monster fallback 검증
- 2026-05-25 같은 Unity 경로 기준 generated equipment의 `generated`, `rarityId`, `affixPoolId` 계약 검증
- 2026-05-25 같은 Unity 경로 기준 NPC `quest_seed_` 참조를 NPC seed 네임스페이스로 인정
- 2026-05-25 같은 Unity 경로 기준 compiledMap monster/loot placement pass 검증
- 전투 승리 시 quest target 완료, field monster cleanup, XP 지급
- 사망 시 Ending 저장, Continue 시 Ending 복귀, Ending New Game 초기화
- 전투 HUD용 dice face/선택/cooldown/status 문자열
- 작은 화면에서 P1 IMGUI overlay와 상호작용 prompt가 화면 밖으로 나가지 않는 layout contract
- Runtime uGUI Canvas/CanvasScaler/EventSystem과 scene별 필수 panel root 존재
- Runtime uGUI panel root normalized safe rect contract
- Focus Strike의 Bleed 적용과 상태 이상 tick 로그
- 장비/소모품/스킬 구분 표시 문자열
- 대장장이/기술 상인/약재상/여관/훈련소/게이트 notice
- 기술 상인 stock refresh notice
- ContentDatabase quest board offer, encounter lookup, NPC/vendor service cost
- compiledMap asset 우선 로드와 field monster state 등록

## 실패 기록 방식

문제가 있으면 아래 형식으로 기록한다.

```text
Scene:
Action:
Expected:
Actual:
Console log:
Screenshot:
```

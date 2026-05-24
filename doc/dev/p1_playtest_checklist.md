# P1 Play Mode 테스트 체크리스트

이 문서는 Unity Play Mode에서 P1 Runtime Vertical Slice를 사람이 직접 확인하기 위한
체크리스트다. 자동 검증은 `Conn > Build & Validate Chapter 1`과 batchmode validator가
담당하지만, 조작감, collider, HUD 위치, 실제 씬 전환 체감은 직접 확인해야 한다.

## 시작 전

1. Unity Editor에서 `Conn > Build P0 Scenes`를 실행한다.
2. Console에 컴파일 에러가 없는지 확인한다.
3. `Assets/Conn/Scenes/Title.unity`를 연다.
4. Play Mode를 시작한다.

## 기본 루프

아래 순서가 끊기지 않아야 한다.

1. Title
   - `New Game` 클릭
   - Town으로 이동하는지 확인

2. Town
   - 퀘스트 게시판을 바라보고 `E`
   - Quest Board 패널이 열리는지 확인
   - `Reroll Board`가 현재 제안을 바꾸는지 확인
   - `Accept Quest` 클릭
   - HUD에 활성 퀘스트가 표시되는지 확인

3. Gate
   - 게이트를 바라보고 `E`
   - Dungeon으로 이동하는지 확인
   - 새 게임 직후 퀘스트 없이 게이트를 눌렀을 때는 입장이 막혀야 한다.

4. Dungeon
   - HUD에 활성 퀘스트, 목표, 필드 몬스터 상태가 표시되는지 확인
   - 몬스터 오브젝트가 보이는지 확인
   - 몬스터와 접촉하면 Combat으로 이동하는지 확인
   - 접촉 로그가 중복으로 여러 번 찍히지 않는지 확인

5. Combat
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
| Apothecary | 포션 구매 |
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

- 마우스 감도: 기본값 `0.14`, 이전 테스트에서 높았던 값을 낮춘 상태
- 이동 속도: 기본값 `4.5`
- 카메라 높이
- NPC 상호작용 거리: 기본값 `4.0`
- 몬스터 접촉 collider 크기
- HUD 버튼이 화면 밖으로 밀리는지
- Combat 버튼이 클릭하기 쉬운지

## 이번 자동 검증에 포함된 항목

- 전투 승리 시 quest target 완료, field monster cleanup, XP 지급
- 사망 시 Ending 저장, Continue 시 Ending 복귀, Ending New Game 초기화
- 전투 HUD용 dice face/선택/cooldown/status 문자열
- Focus Strike의 Bleed 적용과 상태 이상 tick 로그
- 장비/소모품/스킬 구분 표시 문자열
- 대장장이/기술 상인/약재상/여관/훈련소/게이트 notice
- 기술 상인 stock refresh notice

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

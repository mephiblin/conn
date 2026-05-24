# 필드 몬스터 FSM 시스템

날짜: 2026-05-22
상태: 구현 전 활성 기획.

## 현재 Unity 기준선

- 던전 몬스터는 Editor Tool이 만든 `monster` 또는 `encounter` placement로 존재한다.
- Runtime에서는 placement definition과 별개로 `fieldMonsters[stateKey]` 상태를 저장한다.
- 마을과 던전은 자유 이동 FPS 공간이다. 셀/격자는 플레이어 이동 스냅이 아니라
  Editor Tool의 맵 생성/오브젝트 배치 기준이다.
- 초기 전투 진입은 보이는 몬스터 오브젝트와 플레이어의 collider/trigger 접촉으로
  발생한다.
- 접촉 시 `Dungeon -> Combat`으로 전환하고, 전투 종료 후 `Combat -> Dungeon`으로
  복귀한다.
- 전투 시작 전에는 `preEncounterSnapshot`을 저장한다.
- 승리하면 placement 완료 처리와 `fieldMonsters` cleanup을 모두 수행한다.
- 도주가 도입되면 snapshot으로 전투 전 위치/상태를 복구한다.
- 사망은 초기 구현에서 즉시 Ending으로 처리한다.

레거시 참조:

- [`../webgame_ref/src/fieldMonsterRuntime.js`](../../webgame_ref/src/fieldMonsterRuntime.js)
- [`../webgame_ref/src/combatRuntime.js`](../../webgame_ref/src/combatRuntime.js)
- [`../webgame_ref/src/renderGame.js`](../../webgame_ref/src/renderGame.js)

## 목표

던전 필드의 몬스터를 단순 encounter trigger가 아니라, 아래 상태를 갖는 필드
오브젝트로 관리한다.

- `idle`: 대기. 감지 범위에 플레이어가 들어오면 경고로 전환한다.
- `patrol`: 순찰. 지정된 waypoint를 이동하다가 감지하면 경고로 전환한다.
- `ambush`: 매복. 기본적으로 움직이지 않거나 숨김 표시를 갖고, 더 짧은 조건에서
  전투 또는 경고로 전환한다.
- `alert`: 경고. 짧은 확인 시간 동안 플레이어를 계속 볼 수 있으면 추적으로 전환한다.
- `chase`: 추적. 플레이어를 향해 이동하고, 접촉 또는 전투 거리 안에 들어오면
  Combat handoff로 넘긴다.
- `give_up`: 추적 포기. 플레이어가 너무 멀어지거나 시야를 오래 잃으면 복귀를 시작한다.
- `return`: 복귀. spawn cell 또는 순찰 route의 가장 가까운 지점으로 돌아간 뒤
  `idle`/`patrol`/`ambush`로 복귀한다.
- `combat_handoff`: 전투 진입 직전의 transient 상태. 실제 전투 씬에서는
  `CombatSession`이 권위 상태다.
- `defeated`/`cleared`: 전투 승리 후 정리된 상태다.

## 기본 상태 흐름

```text
idle / patrol / ambush
  -> alert        감지 범위 안에서 시야 또는 감지 조건 충족
  -> chase        alert 시간이 차고 계속 감지됨
  -> combat_handoff  chase 중 접촉 또는 engageRange 진입
  -> give_up      chase 중 loseSightTime 초과 또는 leashRange 이탈
  -> return       포기 후 home/patrol route로 이동
  -> idle/patrol  복귀 완료
  -> defeated/cleared  전투 승리 후 cleanup
```

매복형 몬스터는 `ambush -> combat` 직접 전환을 허용한다. 예: 플레이어가 같은
칸 또는 인접 칸에 들어오면 경고 시간 없이 전투.

## 데이터 계약 초안

Authored placement 확장:

```json
{
  "id": "guard_01",
  "kind": "encounter",
  "refType": "encounter",
  "refId": "encounter_serpent_guard",
  "position": { "x": 8, "y": 6 },
  "fieldAi": {
    "enabled": true,
    "archetype": "guard",
    "initialState": "patrol",
    "home": { "x": 8, "y": 6 },
    "patrolRoute": [
      { "x": 8, "y": 6 },
      { "x": 10, "y": 6 },
      { "x": 10, "y": 8 }
    ],
    "visionRange": 5,
    "hearingRange": 2,
    "fov": 90,
    "alertSeconds": 1.2,
    "chaseSpeed": 1.0,
    "patrolSpeed": 0.45,
    "engageRange": 0.8,
    "leashRange": 8,
    "loseSightSeconds": 2.5,
    "returnMode": "home"
  }
}
```

Runtime state는 authored placement를 오염시키지 않고 save/load delta에 저장한다.

```json
{
  "fieldMonsters": {
    "guard_01": {
      "state": "chase",
      "x": 9,
      "y": 6,
      "routeIndex": 1,
      "alertElapsed": 0.8,
      "lostSightElapsed": 0,
      "lastKnownPlayerCell": { "x": 7, "y": 6 },
      "home": { "x": 8, "y": 6 },
      "cooldownUntilTurn": 0
    }
  }
}
```

## MVP 규칙

- 첫 구현은 Dungeon 씬에서만 실행한다. Town의 NPC/service field와 섞지 않는다.
- 첫 구현은 고정 배치 몬스터 + 접촉 트리거만 사용한다.
- FSM의 patrol/chase/return은 두 번째 단계에서 붙인다.
- 몬스터 오브젝트는 placement data에서 생성되고, runtime state는 `FieldMonsterState`로 저장한다.
- 전투 진입은 새 전투 시스템을 만들지 않고 Combat 씬 handoff만 호출한다.
- 전투 승리 후 placement 완료와 runtime state cleanup을 모두 수행한다.
- defeated 몬스터는 저장/불러오기 또는 던전 재진입 후 다시 blocker/marker/FSM 대상이 되면 안 된다.

## 구현 모듈 경계

- `FieldMonsterRuntime`: FSM 상태 전이, 감지, 추적/복귀 이동, combat handoff 후보
  계산을 담당한다.
- `PlacementDefinition`: Editor/compiledMap이 만든 몬스터/조우 배치 데이터다.
- `FieldMonsterState`: Runtime save에 들어가는 상태 delta다.
- `DungeonSession`: Dungeon 씬의 active monster 상태와 Combat handoff를 관리한다.
- `CombatSession`: 전투 진입 이후 전투 상태를 소유한다.
- `SaveGame`: `fieldMonsters` runtime delta를 저장/복원한다.

## Editor Authoring

첫 UI는 encounter/monster placement inspector에 최소 필드를 추가한다.

- AI enabled 토글
- archetype preset: `stationary`, `guard`, `patrol`, `ambush`, `roamer`
- initial state
- vision range, alert seconds, leash range, lose sight seconds
- patrol route 편집: 현재 셀 추가/삭제/순서 변경
- home cell 재설정

Editor validation은 다음을 확인한다.

- `fieldAi.enabled`인데 `refId`가 유효한 encounter가 아니면 error.
- `patrol`인데 route가 2개 미만이면 warning 또는 idle fallback.
- route/home cell이 walkable이 아니면 error.
- `visionRange`, `leashRange`, time 값이 음수이면 error.
- 필수 진행 placement가 roaming하다가 softlock을 만들 수 있으면 warning.

## Unity 구현 Slice

1. Data contract foundation
   - `PlacementDefinition`과 `FieldMonsterState`를 분리한다.
   - `stateKey`, `placementId`, `encounterId`, `defeated`를 저장한다.
2. Visible monster contact
   - compiledMap placement에서 몬스터 GameObject를 생성한다.
   - collider/trigger 접촉으로 Combat handoff를 발생시킨다.
3. Combat cleanup
   - 전투 시작 전 `preEncounterSnapshot`을 저장한다.
   - 승리 시 placement 완료와 `fieldMonsters[stateKey]` cleanup을 수행한다.
   - 사망 시 Ending으로 전환한다.
4. Save/load
   - defeated/cleared 상태를 저장하고 불러온다.
   - defeated 몬스터가 다시 생성되지 않는지 확인한다.
5. FSM 확장
   - `idle -> alert -> chase -> combat_handoff -> give_up -> return`을 추가한다.
   - patrol/ambush는 이후 단계로 확장한다.
6. Editor and validation
   - encounter/monster inspector에 field AI authoring UI와 validation을 붙인다.

## 후속 확장

- 소리 감지: 달리기, 문 열기, 전투 후 소음.
- 무리 행동: 같은 faction 또는 같은 encounter group 호출.
- 문 처리: 문을 열 수 있는 몬스터와 문 앞에서 포기하는 몬스터 분리.
- 기습/선공 보정: 매복 성공, 측후방 접촉, 플레이어가 먼저 상호작용한 경우를
  combat initiative에 넘긴다.
- 난이도 스케일: floor depth, light/torch, party 상태에 따른 감지 보정.

## 완료 기준

- 정지 encounter와 기존 전투 진입 흐름이 깨지지 않는다.
- AI enabled 몬스터가 감지, 경고, 추적, 전투 진입, 추적 포기, 복귀를 수행한다.
- 순찰 route가 있는 몬스터가 route를 따라 움직이고, 전투 후 defeated placement는
  더 이상 blocker/marker/FSM 대상이 아니다.
- 저장 후 불러와도 몬스터의 현재 필드 상태가 유지되거나 명시된 fallback 정책으로
  일관되게 복원된다.

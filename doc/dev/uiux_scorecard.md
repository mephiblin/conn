# UI/UX Scorecard

Last updated: 2026-06-03

## 기준

점수는 기능 항목별 10점 만점이다. 기준은 상용 RPG UI에 가까운 정보 위계, 메뉴 간 분리, 전투 입력 동선, 즉시 피드백, 디버그 노출 억제, 캐릭터 대비 맵 스케일이다.

참고 기준:

- UXPin, Game UX Design: HUD는 필요한 정보만 우선순위로 보여주고, 메뉴는 일관된 탐색 구조로 인지 부하를 줄여야 한다. https://www.uxpin.com/studio/blog/game-ux/
- AND Academy, Game UI Design: UI는 필요한 정보와 조작, 진행 피드백을 직접적으로 제공해야 하며, 잘 조직된 메뉴는 인벤토리 관리 시 인지 부하를 낮춘다. https://www.andacademy.com/resources/blog/ui-ux-design/game-ui-design/
- GDC Vault, Ben Lewis-Evans, Throwing out the dopamine shots: 행동 결과와 보상의 명확한 피드백은 UX의 핵심이다. https://media.gdcvault.com/gdc2017/Presentations/Lewis-Evans_Ben_Throwing%20out%20the.pdf

## 현재 범위 점수

| 항목 | 이전 작업 직후 점수 | 현재 점수 | 근거 | 남은 작업 |
| --- | ---: | ---: | --- | --- |
| 인벤토리/스킬창 분리 | 8.0 | 9.5 | `Bag`과 `Skill` 버튼 및 패널이 분리되어 같은 공간 충돌은 해소됨. 장비 카드에는 장비 부위/상태 배지와 종류별 색상, 스킬 카드와 릴 face에는 효과 배지/색상 계층이 추가되어 한눈에 구분 가능해짐. 장착 슬롯은 클릭 가능한 카드가 되어 방패/방어구는 `Equipped | Unequip`, 기본 무기는 `Equipped | Locked`로 구분되고, 해제 시 장비/방어/스킬 face 수가 즉시 갱신됨. | 최종 아이콘 아트와 해상도별 실제 화면 확인이 들어가야 10점. |
| 스킬 주사위 장착 UX | 7.5 | 9.5 | 스킬 카드 드래그/드롭과 클릭 장착은 동작함. 보유/장착/여유 수량과 선택된 스킬 상태가 표시되고, 한손/방패/양손 장비별 스킬군 보정이 전투와 UI에 연결됨. 주사위 face 슬롯이 선택 스킬 기준으로 `Drop`/`Replace`/`Same`/`Clear` 상태와 색상을 구분해 장착 결과를 더 즉시 읽을 수 있음. | 실제 Game view에서 드래그 중 커서 위치/슬롯 hover 하이라이트가 안정적으로 보이는지 확인해야 10점. |
| 캐릭터 생성 | 7.0 | 9.5 | 개발 문구를 제거하고 초상화별 플레이어용 프로필 설명으로 교체함. 초상화별 시작 무기/스킬 프리셋과 추천 빌드 설명이 실제 새 게임 장비/스킬 장착 상태에 연결됨. Vitality가 실제 시작 Max HP에 반영되고, 캐릭터 생성 스탯 패널에 같은 규칙의 Starting HP가 표시됨. | Game view 기준 선택 피드백, 직업별 장기 성장 곡선, 최종 밸런스 플레이테스트가 필요. |
| 상점 상호작용 | 8.0 | 9.5 | 구매/판매/비교/상세 패널이 있고, 호버뿐 아니라 클릭으로도 상세 대상이 고정됨. 선택된 카드의 배경/테두리/상태 배지가 분리되어 현재 상품과 실행 가능 여부가 더 분명해짐. 카드 클릭 후 상세 패널의 Confirm/Cancel로 구매/판매를 확정하게 되어 오입력 결제를 줄임. | 최종 상점 아이콘/카드 아트와 실제 Game view 확인이 들어가면 10점. |
| 퀘스트 후 던전 진입/맵 크기 | 8.5 | 9.5 | 런타임 셀 크기 보정과 맵 제너레이터 소켓/브랜치 수정으로 끊김과 작음 문제를 완화함. 던전 입장 시 플레이어 yaw가 시작점에서 첫 주요 목표 방향을 향하도록 보정되고, Quest/Boss/Exit 월드 표식이 생성되어 진입 직후 목표 방향과 주요 지점을 읽기 쉬워짐. | 실제 Game view 캡처 기반 프레이밍, 표식 가독성, 선호 카메라 거리 조정이 들어가면 10점. |
| 던전 맵 구조/제너레이터 | 8.0 | 9.5 | MapGenerationTests 65개 통과. 허브/분기/데드엔드 강제 배치와 잔류 탐험 보상/위험에 더해, baked compiled map 기준 시작점-퀘스트-보스-출구 walkable route, 목표까지 최소 거리, 선택점, reachable 보상/몬스터를 검증하는 탐험 리듬 게이트를 추가함. 런타임 스폰은 3방향 이상 walkable 선택점 중 다른 room으로 이어지는 지점에 비차단 갈림길 표식을 생성해 분기 체감을 보강함. | 실제 Game view 캡처 기반으로 공간 스케일, 조명/랜드마크 가독성, 분기 표식의 시각 강도를 확인해야 10점. |
| 전투 씬 UX | 7.5 | 9.5 | CG가 UI 뒤에 놓이고 불필요한 전투 로그 패널은 숨김. 릴 회전 중 시간 기반 ticker가 표시되고, STOP/선택 적용/도주 동선이 하단 릴 패널과 분리되어 겹침이 줄어듦. 턴 결과는 피해/방어/회복/반격 요약과 `SPIN`/`READY`/`IMPACT`/`DANGER`/`VICTORY` 배너 색상으로 명령 패널 상단에 분리 표시됨. 승리 후에는 새 테스트 전투를 시작하지 않고 복귀 선택 패널을 보여줌. | 전투 결과 피드백 애니메이션/사운드가 없어서 상용 수준의 타격감은 아직 부족. |
| 전투 종료 후 커서/복귀 흐름 | 8.0 | 9.5 | 전투 승리 직후 복귀 선택 커서 해제와, 던전에 남은 뒤 남은 필드 몬스터 접촉 전투 재진입 규칙을 코드/회귀 테스트로 보강함. | 실제 Game view에서 승리 후 던전 잔류, 남은 몬스터 접촉, 복귀 선택 커서 상태를 연속 확인해야 10점. |
| 디버그 패널 노출/크기 | 9.0 | 10.0 | 기본 숨김, F3 토글, 축소 표시, 에디터/디버그 빌드 한정 토글로 정상 플레이 방해를 제거함. | 없음. |

평균: 9.56 / 10

## 반복 개선 로그

### 2026-05-31 스크린샷 지적사항 시작 점수

아래 항목은 사용자가 첨부한 7개 문제 범위의 시작 점수이며, 각 항목은 1/10에서 시작한다.

| 신규 문제 | 시작 점수 | 현재 점수 | 수용 기준 |
| --- | ---: | ---: | --- |
| 캐릭터 생성 폼/스탯 배치 촌스러움 | 1.0 | 7.5 | 시작 장비 박스 축소, Back 하단 배치, 스탯 배경 제거. |
| 스킬 드래그 시 카드/아이콘 미동반 | 1.0 | 8.0 | 드래그 중 스킬 고스트가 커서를 따라가고, 장착된 스킬은 보유 슬롯에서 사라짐. |
| 장착 장비가 인벤토리 칸에 남음 | 1.0 | 9.0 | 장착 중인 장비는 짐칸 목록에서 제외되고 장착칸에만 표시됨. |
| NPC 상호작용 프롬프트가 UI를 뚫고 나옴 | 1.0 | 8.5 | 인벤토리/스킬창이 열렸을 때 프롬프트/알림이 덮지 않고 패널 레이어가 재정렬됨. |
| NPC 상점 패널이 CG 영역을 침범함 | 1.0 | 8.0 | 상점 UI가 좌측으로 정렬되고 우측 NPC/배경 감상 영역을 남김. Close 위치는 헤더 우측으로 유지. |
| 뒤틀린 사원 시작점 근처 공중 공간 | 1.0 | 9.0 | 전용 compiled map의 전이 구간을 1층 평면 Floor로 고정해 현재 플레이어 컨트롤러 기준 콜라이더 없는 경사/계단 셀을 제거함. |
| 인벤토리/스킬창 닫은 뒤 커서가 남음 | 1.0 | 9.5 | 모든 보조 패널을 닫으면 수동 커서 해제 상태가 정리되고 FPS 화면 고정으로 복귀. |

### 2026-05-31 스크린샷 루프 1

- 적용:
  - 캐릭터 생성 시작 장비 박스를 축소하고 `Buy 0g` 임시 문구를 장비 요약으로 교체함.
  - 캐릭터 생성 `Back` 버튼을 하단으로 밀고, 스탯 패널의 배경 박스를 제거함.
  - 스킬 카드 드래그 중 커서를 따라가는 고스트 카드를 추가하고, 장착된 스킬은 보유 카드 목록에서 제외함.
  - 장착 중인 장비는 인벤토리 짐칸에서 제외하고 장착칸에만 표시함.
  - 인벤토리/스킬 패널이 열렸을 때 NPC 상호작용 프롬프트와 알림 패널이 위로 뚫고 나오지 않도록 숨김/레이어 순서를 재정렬함.
  - 스킬/가방 빠른 버튼은 오버레이 뒤로 그려지게 하고, 마지막 패널을 닫으면 커서 수동 해제를 정리하도록 수정함.
  - 스킬 상점 패널을 좌측으로 이동시켜 우측 NPC/배경 영역을 남김.
  - MapGenV2 프로토타입 머티리얼라이저가 천장/상층 플레이트를 실제 플레이 바닥처럼 생성하지 않도록 제외함.
- 검증:
  - `RuntimeCoreRulesTests`: 43/43 통과
  - `GameFlowPlaytestTests`: 9/9 통과
  - 전체 `EditMode`: 262/262 통과
  - `MapGenV2FoundationTests`: 134/134 통과

### 2026-05-31 스크린샷 루프 2

- 적용:
  - 마을 HUD의 `Cursor: free/locked (Esc)` 디버그성 안내 문구를 제거해 정상 플레이 화면의 잡음을 줄임.
- 점수 변경:
  - 인벤토리/스킬창 닫은 뒤 커서가 남음: 9.0 -> 9.5

### 2026-06-01 유지보수 루프 1

- 적용:
  - 최근 UI 작업 파일을 점검해 Unity obsolete API 경고를 제거함.
  - 스킬 드래그 고스트 cleanup을 보강해 드래그 도중 UI가 비활성화되어도 원래 투명도와 고스트 상태가 남지 않게 함.
- 점수 변경:
  - 없음. 플레이 UX/UI 동작 변경 없이 안정성만 보강함.
- 검증:
  - `RuntimeCoreRulesTests`: 43/43 통과
  - `GameFlowPlaytestTests`: 9/9 통과

### 2026-06-03 뒤틀린 사원 연결성 루프

- 적용:
  - `twisted_temple_2001_CompiledMap`의 `main_2` 전이 구간에서 현재 런타임이 콜라이더를 만들지 않는 `Slope`/`Stair` 셀을 `Floor`로 전환함.
  - 같은 전이 구간의 높이 1-2 walkable 셀과 spawn hint 오브젝트를 높이 0으로 평탄화해 시작점에서 퀘스트/보스/출구까지 1층 바닥으로 이어지게 함.
  - `GameFlowPlaytestTests`에 뒤틀린 사원 전용 flat floor route 회귀 테스트를 추가함.
  - 런타임 맵 스폰에서 `Slope`/`Stair` 셀이 다시 들어와도 비활성 콜라이더가 되지 않도록 경사 메쉬와 `MeshCollider`를 생성하게 보강함.
  - `RuntimeCoreRulesTests`에 높이 전이 셀이 walkable collision으로 스폰되는 회귀 테스트를 추가함.
- 점수 변경:
  - 뒤틀린 사원 시작점 근처 공중 공간: 7.0 -> 9.0
- 검증:
  - 로컬 parser/BFS 확인: start -> quest target -> boss -> exit flat floor route 통과.
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`/`GameFlowPlaytestTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단됨.
  - 커밋: `5d2bfc6`

### 2026-06-03 전투 릴 UI 루프

- 적용:
  - 전투 릴 카드가 회전 중일 때 mask 안의 `ReelStrip`이 실제로 세로 이동하도록 수정해 정지 텍스트처럼 보이지 않게 함.
  - 레거시 전투 시스템 이미지와 웹게임 `renderCombat.js`의 3칸 ticker 구조를 참고해 릴 카드 내부 window를 움직이는 슬롯형 구조로 정리함.
  - 전투 명령 패널을 중앙 오버레이에서 하단 릴 트레이 위쪽으로 이동하고, 릴 패널 폭/높이를 넓혀 게임뷰에서 4-5개 릴이 잘리지 않도록 함.
  - 전투 버튼 문구를 `STOP / 선택 적용 / 도주`로 정리함.
  - `RuntimeCoreRulesTests`에 릴 spin->stop 상태와 하단 릴 UI safe rect 회귀 테스트를 추가함.
- 점수 변경:
  - 전투 씬 UX: 9.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단됨.
  - 커밋: `866369f`
  - 추가 수정 커밋: `895e521`

### 2026-06-03 전투 후 던전 잔류 루프

- 적용:
  - 필드 몬스터 접촉 진입 조건에서 `Quest.TargetDefeated` 전체 차단을 제거하고, 현재 접촉한 몬스터의 개별 `Defeated` 상태만 차단하도록 수정함.
  - `FieldMonsterRuntimeService.CanStartContactCombat`를 추가해 던전에 남은 뒤 남은 몬스터 전투 진입 가능 여부를 테스트 가능한 규칙으로 분리함.
  - `KeepExploringAllowsRemainingFieldMonsterCombat` 회귀 테스트를 추가해 목표 몬스터 처치, `KeepExploring`, 남은 몬스터 전투 handoff 재개 흐름을 고정함.
- 점수 변경:
  - 전투 종료 후 커서/복귀 흐름: 10.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단되어 XML 결과가 생성되지 않음.
  - 커밋: `febed89`

### 2026-06-03 전투 피드백 계층 루프

- 적용:
  - `CombatSessionState`에 `LastTacticalSummary`와 `LastFeedbackKind`를 추가해 긴 전투 로그와 별개로 플레이어가 즉시 읽을 수 있는 결과 요약을 저장함.
  - 릴 회전, 릴 정지, 턴 교환, 무행동 반격, 승리 상태마다 전술 요약을 갱신하도록 `CombatRuntimeService`를 보강함.
  - 전투 명령 패널과 승리 패널 상단에 피해/방어/회복/적 반격/XP 요약을 굵은 텍스트로 표시함.
  - `RuntimeCoreRulesTests`에 릴 spin/stop, 턴 교환, 승리 요약 회귀 검증을 추가함.
- 점수 변경:
  - 전투 씬 UX: 9.5 유지. 텍스트 피드백 계층은 개선됐지만 애니메이션/사운드 타격감은 아직 남음.
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단되어 XML 결과가 생성되지 않음.
  - 커밋: `1cd79d9`

### 2026-06-03 전투 결과 배너 색상 루프

- 적용:
  - 전투 명령 패널의 전술 요약을 `CombatFeedbackBanner`로 바꾸고 feedback kind별 배경/텍스트 색을 분리함.
  - `SPIN`, `READY`, `IMPACT`, `DANGER`, `VICTORY` 배지를 추가해 회전/선택 가능/교전 결과/위험/승리 상태를 한눈에 구분하게 함.
  - 승리 후 복귀 선택 패널도 같은 배너 helper를 사용해 전투 종료 상태를 유지해서 보여줌.
  - `CombatFeedbackKindsExposeDistinctImpactBadgesAndColors` 회귀 테스트로 라벨과 색상 분기를 고정함.
- 점수 변경:
  - 전투 씬 UX: 9.5 유지. 시각 피드백은 보강됐지만 10점에는 실제 Game view 확인, 피격/회복 애니메이션, 사운드가 필요함.
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 59/59 통과 (`tmp/runtimecore-combat-feedback-banner-results.xml`, failed=0).
  - 커밋: `8e5707f`

### 2026-06-03 던전 잔류 보상/위험 루프

- 적용:
  - `DungeonObjectState`를 추가해 상자/배럴 개봉 상태, 획득 골드, 위험 피해를 `WorldRuntimeState` 저장 상태에 남기도록 함.
  - `DungeonObjectRuntimeService`를 추가해 던전 오브젝트 루팅을 단일 규칙으로 처리하고, 씬 재생성 후 같은 상자/배럴을 다시 파밍하지 못하게 함.
  - 퀘스트 목표 완료 후 던전에 남아 상자/배럴을 열면 추가 골드 보너스를 얻는 대신 HP 피해를 받는 잔류 탐험 보상/위험 규칙을 추가함.
  - 던전 HUD에 이번 원정의 열린 루팅 오브젝트 수, 획득 골드, 위험 피해 요약을 표시함.
  - `RuntimeCoreRulesTests`에 목표 완료 전 루팅, 목표 완료 후 보너스/피해, 저장/불러오기 후 개봉 상태 유지 회귀 테스트를 추가함.
- 점수 변경:
  - 던전 맵 구조/제너레이터: 8.0 -> 8.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단되어 XML 결과가 생성되지 않음.
  - 커밋: `b4e0845`

### 2026-06-03 장비-스킬 시너지 루프

- 적용:
  - `PlayerEquipmentState.SkillPowerBonusFor`와 `CombatLoadoutSummary`를 추가해 한손/방패/양손 장비가 강화하는 스킬군을 명시함.
  - 전투 주사위 해석에서 장비 그립별 보정을 적용함. 양손은 공격/약화/흡혈, 방패는 방어/지원/버프, 한손은 회복/지원/버프 face에 +1 power를 부여함.
  - 전투 결과 상세 메시지와 전술 요약에 `loadout +1`, `장비보정 +N`을 표시해 어떤 장비 선택이 결과를 바꿨는지 보이게 함.
  - 인벤토리, 스킬 주사위 패널, 전투 커맨드 패널에 현재 loadout 보정 설명을 표시함.
  - `RuntimeCoreRulesTests`와 `RuntimeRuleVerifier`에 한손 회복 보정, 양손 공격 보정, 방패 방어 보정 회귀 검증을 추가/갱신함.
- 점수 변경:
  - 스킬 주사위 장착 UX: 8.5 -> 9.0
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단되어 XML 결과가 생성되지 않음.
  - 커밋: `b918556`

### 2026-06-03 마을 귀환 정산 루프

- 적용:
  - `QuestRuntimeState`에 마지막 원정의 퀘스트 보상, 루팅 골드, 위험 피해, 처치 수, 게시판 리롤 번호를 저장하는 필드를 추가함.
  - `QuestRuntimeService.CompleteReturn`이 귀환 시 원정 중 루팅/위험/필드 몬스터 처치 수를 집계해 `ReturnSettlementSummary`로 남기도록 보강함.
  - Town Notice와 P0 overlay가 단순 `Last reward` 대신 퀘스트 보상, 루팅, 위험 피해, 처치 수, 다음 게시판 리롤 상태를 한 번에 보여주도록 수정함.
  - `RuntimeCoreRulesTests`와 `RuntimeRuleVerifier`에 귀환 정산, 저장/불러오기 보존, 루팅 포함 골드 정산 회귀 검증을 추가/갱신함.
- 점수 변경:
  - 없음. 마을 귀환 피드백은 개선됐지만 상점 카드 선택 강조/아이콘 polish는 여전히 남음.
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단되어 XML 결과가 생성되지 않음.
  - 커밋: `15e9de9`

### 2026-06-03 상점 카드 선택 강조 루프

- 적용:
  - 상점 상품 카드가 현재 선택/호버된 상품이면 배경색과 outline을 다르게 표시하도록 수정함.
  - 구매/판매 가능 여부와 선택 상태를 `Selected · Buy ready`, `Selected · Sell locked` 같은 상태 배지로 카드 안에 표시함.
  - `ShopCardStateLabel`과 `ShopCardBackgroundColor` 헬퍼를 추가해 선택/잠금/일반 상태의 문구와 색상 차이를 테스트 가능하게 분리함.
  - `RuntimeCoreRulesTests`에 선택 카드 상태 문구와 색상 분리 회귀 검증을 추가함.
- 점수 변경:
  - 상점 상호작용: 9.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests` batchmode 실행은 같은 프로젝트를 열고 있는 다른 Unity 인스턴스 때문에 차단되어 XML 결과가 생성되지 않음.
  - 커밋: `c1dea74`

### 2026-06-03 캐릭터 생성 프리셋 루프

- 적용:
  - `CharacterCreationOptions`/`CharacterCreationState`에 `StarterSkillId`를 추가해 캐릭터 생성 선택이 시작 스킬까지 저장되도록 함.
  - Canvas 캐릭터 생성 화면에 초상화별 시작 무기/스킬 요약과 추천 빌드 설명을 표시함.
  - 초상화를 바꾸면 Vanguard=Rusty Sword+Guard, Duelist=Rusty Sword+Focus Strike, Arcanist=Great Axe+Mend 프리셋이 기본으로 잡히며, 새 게임 시작 시 해당 스킬이 첫 릴 face에 장착됨.
  - 레거시 IMGUI 오버레이도 같은 `StarterSkillId` 전달과 초상화별 프리셋을 따르도록 맞춤.
  - 저장/불러오기 계약에 `StarterSkillId` 보존 검증을 추가함.
- 점수 변경:
  - 캐릭터 생성: 8.5 -> 9.0
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 51/51 통과 (`tmp/runtimecore-character-preset-results.xml`, failed=0).
  - 커밋: `42e76e8`

### 2026-06-03 캐릭터 생성 Vitality 시작 HP 루프

- 적용:
  - `PlayerRuntimeState.StartingMaxHpForVitality`와 `ApplyCharacterCreation`을 추가해 캐릭터 생성 Vitality가 실제 시작 Max HP에 반영되도록 함.
  - `GameSessionState.StartNewGame`이 캐릭터 생성 옵션 적용 직후 플레이어 시작 HP를 같은 규칙으로 초기화하게 함.
  - 캐릭터 생성 스탯 패널에 `Starting HP`를 표시해 UI 프리셋과 실제 런타임 HP 규칙이 같은 값을 보여주게 함.
  - `CharacterCreationVitalityChangesStartingPlayerHp` 회귀 테스트를 추가하고, 기본 새 게임 HP 20 계약과 초상화별 시작 HP helper를 고정함.
- 점수 변경:
  - 캐릭터 생성: 9.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 55/55 통과 (`tmp/runtimecore-character-hp-results.xml`, failed=0).
  - Unity `GameFlowPlaytestTests`: 10/10 통과 (`tmp/gameflow-character-hp-results.xml`, failed=0).
  - 커밋: `889b48a`

### 2026-06-03 인벤토리/스킬 카드 배지 루프

- 적용:
  - 장비 가방 슬롯에 `1H | Weapon | In Bag`, `SH | Shield | Equipped` 같은 분류/상태 배지를 추가함.
  - 장비 종류별 카드 배경색을 분리해 무기, 방패, 방어구가 같은 카드처럼 보이지 않게 함.
  - 스킬 카드와 릴 face에 `ATK`, `GRD`, `HEAL` 효과 배지와 효과별 배경색을 적용함.
  - 런타임 콘텐츠 DB가 초기화되기 전에도 기본 카탈로그 fallback으로 장비/스킬 카드가 표시되도록 보강함.
  - `InventoryAndSkillCardsExposeReadableBadgesAndColors` 회귀 테스트를 추가함.
- 점수 변경:
  - 인벤토리/스킬창 분리: 8.5 -> 9.0
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 52/52 통과 (`tmp/runtimecore-inventory-badges-results.xml`, failed=0).
  - 커밋: `98da070`

### 2026-06-03 장비 슬롯 해제 피드백 루프

- 적용:
  - 장착 슬롯을 텍스트 목록에서 클릭 가능한 슬롯 카드로 바꾸고, 방패/방어구는 `Equipped | Unequip`, 기본 무기는 `Equipped | Locked`, 빈 슬롯은 `Empty`로 표시함.
  - `PlayerEquipmentState.Unequip`과 `EquipmentRuntimeService.TryUnequip`을 추가해 방패/방어구 해제 시 장착 상태, 방어 보너스, 스킬 face 수, 알림 문구가 함께 갱신되도록 함.
  - 장착 슬롯 전용 상태 문구/배경색 helper를 추가해 해제 가능 슬롯과 잠긴 무기 슬롯을 시각적으로 구분함.
  - `EquippedArmorAndShieldCanBeUnequippedFromSlots`와 카드/슬롯 상태 helper 회귀 검증을 추가함.
- 점수 변경:
  - 인벤토리/스킬창 분리: 9.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 56/56 통과 (`tmp/runtimecore-equipment-unequip-results.xml`, failed=0).
  - 커밋: `4a640bd`

### 2026-06-03 스킬 주사위 드롭 슬롯 피드백 루프

- 적용:
  - 주사위 face 슬롯의 선택 스킬 기준 상태를 `Drop`/`Replace`/`Same`/`Clear`/`Empty`로 구분하는 `SkillDropSlotStateLabel`을 추가함.
  - 빈 슬롯에 선택 스킬을 넣는 상태, 기존 스킬을 교체하는 상태, 같은 스킬이 이미 들어간 상태를 서로 다른 배경색으로 표시하는 `SkillDropSlotBackgroundColor`를 추가함.
  - 실제 스킬 주사위 face 버튼 라벨에 상태 배지를 붙여 클릭/드롭 결과를 누르기 전에 읽을 수 있게 함.
  - `SkillDropSlotsExposeActionStateAndColors` 회귀 테스트로 상태 문구와 색상 분기를 고정함.
- 점수 변경:
  - 스킬 주사위 장착 UX: 9.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 54/54 통과 (`tmp/runtimecore-skill-drop-state-results.xml`, failed=0).
  - 커밋: `3ae2757`

### 2026-06-03 상점 구매/판매 확인 루프

- 적용:
  - 상점 상품 카드 클릭이 즉시 구매/판매를 실행하지 않고 선택 및 확인 대기 상태만 만들도록 변경함.
  - 상세 패널에 `Confirm Buy`/`Confirm Sell`과 `Cancel` 버튼을 추가해 실제 결제/판매를 한 번 더 확정하게 함.
  - 확인 대기 중인 상품 카드는 `Confirm · Buy ready`, `Confirm · Sell locked` 상태 배지로 표시됨.
  - pending 확인 상태가 hover보다 우선되도록 해 다른 카드를 지나가도 확인 대상이 흔들리지 않게 함.
  - `ShopCardSelectionStateLabelsAndColorsAreDistinct`에 확인 상태/문구 회귀 검증을 추가함.
- 점수 변경:
  - 상점 상호작용: 9.5 유지. 구매 확인 흐름은 보강됐지만 최종 카드/아이콘 아트와 실제 Game view 확인은 남음.
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 52/52 통과 (`tmp/runtimecore-shop-confirm-results.xml`, failed=0).
  - 커밋: `14cbc74`

### 2026-06-03 던전 진입 프레이밍 루프

- 적용:
  - 던전 플레이어 시작 위치 계산을 `InitialPlayerPosition` 헬퍼로 분리함.
  - 시작 anchor에서 첫 주요 목표 anchor를 바라보는 `InitialPlayerRotation`을 추가함. 우선순위는 QuestTarget, Boss, Exit 순서임.
  - 던전 씬 bootstrap의 `PlacePlayerAtStart`가 더 이상 무조건 `Quaternion.identity`를 쓰지 않고, 시작점에서 다음 진행 방향을 바라보는 yaw를 적용함.
  - anchor가 누락된 비정상 맵에서도 identity/fallback으로 안전하게 동작하도록 null-safe placement lookup을 추가함.
  - `DungeonRuntimeFramesPlayerTowardFirstObjectiveOnSpawn` 회귀 테스트를 추가함.
- 점수 변경:
  - 퀘스트 후 던전 진입/맵 크기: 8.5 -> 9.0
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 53/53 통과 (`tmp/runtimecore-dungeon-framing-results.xml`, failed=0).
  - 커밋: `e979a1e`

### 2026-06-03 던전 목표 표식 루프

- 적용:
  - compiled map 스폰 시 QuestTarget, Boss, Exit placement에 각각 색이 다른 월드 표식을 생성함.
  - 표식은 `QUEST`/`BOSS`/`EXIT` 라벨과 point light를 포함해 시작 회전 보정 이후에도 주요 지점을 더 빨리 읽을 수 있게 함.
  - 표식 collider는 비활성화해 현재 CharacterController 이동 경로를 막지 않도록 함.
  - `DungeonRuntimeSpawnsReadableNonBlockingObjectiveMarkers` 회귀 테스트로 생성 대상, 비대상 Start/Monster 제외, 비차단 collider, 위치, 색상 분기를 고정함.
- 점수 변경:
  - 퀘스트 후 던전 진입/맵 크기: 9.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 57/57 통과 (`tmp/runtimecore-objective-markers-results.xml`, failed=0).
  - 커밋: `8790049`

### 2026-06-03 던전 탐험 리듬 게이트 루프

- 적용:
  - `MapGenerationQualityService.ValidateCompiledExplorationRhythm`을 추가해 baked compiled map의 시작점-퀘스트-보스-출구 walkable route를 BFS로 검증함.
  - 목표까지 최소 거리, 보스/출구 거리 순서, 3방향 이상 선택점, reachable 상자/배럴 보상, reachable optional monster를 품질 지표로 분리함.
  - 런타임 생성 경로는 cell payload가 있는 compiled map에만 탐험 리듬 게이트를 적용해 graph-only fallback을 깨지 않게 함.
  - 저장된 `ch2_first_slice_ruins_2001_CompiledMap`은 오류/경고 없이 탐험 리듬 검증을 통과하고, `twisted_temple_2001_CompiledMap`은 시작점에서 목표/보스/출구까지 route 검증을 통과하도록 회귀 테스트를 추가함.
- 점수 변경:
  - 던전 맵 구조/제너레이터: 8.5 -> 9.0
- 검증:
  - Unity `MapGenerationTests`: 65/65 통과 (`tmp/mapgeneration-exploration-rhythm-results.xml`, failed=0).
  - 커밋: `f44ec50`

### 2026-06-03 던전 갈림길 표식 루프

- 적용:
  - compiled map 런타임 스폰 시 3방향 이상 walkable 이웃이 있고 다른 room으로 이어지는 셀을 readable choice point로 판정함.
  - 선택점에는 바닥 위 낮은 `Choice Marker`를 생성해 허브/분기 진입부를 플레이 중에 읽기 쉽게 함.
  - 표식 collider는 비활성화해 현재 CharacterController 이동 경로를 막지 않도록 함.
  - `DungeonRuntimeMarksReadableChoicePointsWithoutBlockingMovement` 회귀 테스트로 중앙 선택점만 표식화되고 일반 통로는 제외되는 규칙을 고정함.
- 점수 변경:
  - 던전 맵 구조/제너레이터: 9.0 -> 9.5
- 검증:
  - `git diff --check` 통과.
  - Unity `RuntimeCoreRulesTests`: 58/58 통과 (`tmp/runtimecore-choice-markers-results.xml`, failed=0).
  - 커밋: `09bb066`

### 2026-05-31 기준점

- 상태: 이전 UX 수정 작업 직후 기준점 문서화.
- 검증:
  - `MapGenerationTests`: 64/64 통과
  - `RuntimeCoreRulesTests`: 42/42 통과
  - `GameFlowPlaytestTests`: 9/9 통과
  - 전체 `EditMode`: 261/261 통과
- 다음 루프 우선순위:
  1. 캐릭터 생성의 개발 문구 제거와 선택 피드백 개선.
  2. 스킬창의 드래그/드롭 안내, 장착 수량, 슬롯 상태 피드백 강화.
  3. 전투 릴의 선택 가능/쿨다운/효과 정보 강화.

### 2026-05-31 루프 1

- 적용:
  - 캐릭터 생성의 `Mock values` 개발 문구 제거.
  - 초상화별 플레이어용 프로필 요약 추가.
  - 스킬창에 보유/장착/여유 수량과 선택 스킬 상태 추가.
  - 전투 커맨드와 릴 카드에 선택 필요/선택 가능/릴 쿨다운 상태를 한국어로 정리.
- 점수 변경:
  - 인벤토리/스킬창 분리: 8.0 -> 8.5
  - 스킬 주사위 장착 UX: 7.5 -> 8.5
  - 캐릭터 생성: 7.0 -> 8.5
  - 전투 씬 UX: 7.5 -> 8.5
- 검증:
  - `RuntimeCoreRulesTests`: 42/42 통과
  - `GameFlowPlaytestTests`: 9/9 통과

### 2026-05-31 루프 2

- 적용:
  - 상점 카드 클릭 시 상세 패널의 대상도 고정되도록 개선.
  - 상점 안내 문구를 짧은 플레이어용 상태 문구로 정리.
  - 디버그 패널 토글을 에디터/디버그 빌드로 제한.
- 점수 변경:
  - 상점 상호작용: 8.0 -> 9.0
  - 디버그 패널 노출/크기: 9.0 -> 10.0
- 검증:
  - `RuntimeCoreRulesTests`: 42/42 통과
  - `GameFlowPlaytestTests`: 9/9 통과

### 2026-05-31 루프 3

- 적용:
  - 전투 승리 후 Combat UI가 비활성 전투를 다시 시작하지 않고 승리/복귀 패널을 표시하도록 수정.
  - 전투 승리 직후 커서가 복귀 선택을 위해 풀리는 규칙 추가.
  - 커서 회귀 테스트 `CombatVictoryReleasesCursorForReturnChoice` 추가.
- 점수 변경:
  - 전투 씬 UX: 8.5 -> 9.0
  - 전투 종료 후 커서/복귀 흐름: 8.0 -> 10.0
- 검증:
  - `RuntimeCoreRulesTests`: 43/43 통과
  - `GameFlowPlaytestTests`: 9/9 통과

## 중단 조건 기록

### 2026-05-31 상용 10/10 판정 중단

현재 코드로 해결 가능한 UX 문제는 루프 3까지 처리했다. 남은 10점 미만 항목은 상용 게임 기준의 최종 품질 판단에 다음 외부 입력이 필요해 여기서 중단한다.

| 항목 | 현재 점수 | 중단 사유 | 사용자가 정하거나 제공해야 할 것 |
| --- | ---: | --- | --- |
| 인벤토리/스킬창 분리 | 8.5 | 기능 분리는 끝났지만 10점은 최종 아이콘, 색상 체계, 패널 아트, 해상도별 화면 확인이 필요함. | 목표 UI 레퍼런스 1-2개, 주 화면 해상도, 장비/스킬 아이콘 스타일. |
| 스킬 주사위 장착 UX | 8.5 | 드래그/클릭 장착은 되지만 10점 수준의 드래그 중 하이라이트와 드롭 애니메이션은 실제 화면 조작 검증이 필요함. | 드래그 중 표시 방식, 슬롯 색상 규칙, 스킬 희귀도/속성 체계. |
| 캐릭터 생성 | 8.5 | 개발 문구는 제거했지만 직업 선택이 실제 성장/스킬/장비 밸런스와 연결되어야 10점임. | 직업별 시작 스탯, 시작 스킬, 기본 장비 정책. |
| 상점 상호작용 | 9.0 | 상세 선택과 비교는 정리됐지만 상용 수준의 카드 선택 강조, 구매 확인, 아이콘 아트가 필요함. | 구매 확인을 즉시 구매로 둘지 확인창을 둘지, 상점 UI 레퍼런스. |
| 퀘스트 후 던전 진입/맵 크기 | 8.5 | 런타임 스케일과 테스트는 통과했지만 카메라 프레이밍은 실제 플레이 화면 기준으로 봐야 함. | 던전 진입 직후/전투 직후 스크린샷, 선호 카메라 거리. |
| 던전 맵 구조/제너레이터 | 8.0 | 테스트상 연결성은 통과하지만 탐색 리듬과 체감 길이는 플레이테스트 지표가 필요함. | 목표 던전 길이, 방 개수 체감, 막다른 길/루프 선호도. |
| 전투 씬 UX | 9.0 | 입력 동선과 커서 문제는 해결했지만 타격감 10점은 애니메이션, 사운드, 피격/회복 이펙트가 필요함. | 전투 피드백 레퍼런스, 공격/회복/방어/쿨다운 이펙트 방향. |

중단 판단: 평균 8.94/10에서 코드 단독 개선 루프를 멈춘다. 10/10까지는 위 입력을 받은 뒤 화면 캡처 기반 검증 루프를 다시 열어야 한다.

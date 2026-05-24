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

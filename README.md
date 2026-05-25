# Conn

Unity 기반 RPG 프로젝트입니다. 현재 구조는 런타임 규칙과 저작 도구를 분리하고,
에디터에서 검증된 콘텐츠와 맵 데이터를 게임 런타임이 읽는 방식으로 정리되어 있습니다.

## Unity Version

- Unity Editor: `6000.4.8f1`
- 확인 위치: `ProjectSettings/ProjectVersion.txt`
- Render Pipeline: Universal Render Pipeline `17.4.0`

## Project Layout

- `Assets/Conn/Core`: 전투, 인벤토리, 장비, 퀘스트, 맵 등 순수 규칙과 데이터 계약
- `Assets/Conn/Runtime`: 씬 부트스트랩, 세션, 월드 상호작용, 런타임 서비스
- `Assets/Conn/Rendering`: 플레이어 컨트롤러와 월드 표시 계층
- `Assets/Conn/UI`: 런타임 UI
- `Assets/Conn/Authoring`: ScriptableObject 기반 콘텐츠/맵 저작 데이터
- `Assets/Conn/Editor`: 콘텐츠 빌드, 검증, 에디터 윈도우, 프리팹 빌더
- `Assets/Conn/Tests`: EditMode 테스트
- `doc/dev`: 개발 설계 문서와 진행 메모

## Scenes

주요 씬은 `Assets/Conn/Scenes` 아래에 있습니다.

- `Title`
- `Town`
- `Dungeon`
- `Combat`
- `Ending`

## Development Notes

맵 생성과 에디터 데이터 설계는
`doc/dev/diablo_map_generation_design.md`를 기준 문서로 사용합니다.

핵심 방향은 다음과 같습니다.

- 미리 저작한 방/청크를 규칙으로 조립하는 Diablo식 맵 생성
- `profile -> room graph -> chunk/socket assembly -> tile/decor pass -> compiled map` 흐름
- 에디터 저작/검증 단계와 게임 런타임 데이터 소비 단계 분리

## Verification

Unity Test Runner에서 EditMode 테스트를 실행합니다.

- 테스트 위치: `Assets/Conn/Tests/EditMode`
- 콘텐츠/런타임 규칙 검증 도구: `Assets/Conn/Editor/Tools`
- 맵 생성 검증 도구: `Assets/Conn/Editor/Maps`

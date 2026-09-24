# AGENT.md

이 저장소에서 작업하는 에이전트가 따라야 할 규칙.

## 규칙

1. 커밋 메시지는 무조건 한글로 작성한다.
2. 커밋 전에 `Assets/` 아래 게임 스크립트에 에디터 전용 코드가 없는지 확인한다.
   - `using UnityEditor...`(예: `using static UnityEditor.Experimental.GraphView.GraphView;`)는 빌드를 실패시킨다. IDE 자동완성이 몰래 추가하는 경우가 많다.
   - `using System.Drawing;`도 쓰지 않는다 (빌드에서 참조되지 않고, `UnityEngine.Color`와 이름이 겹친다).
   - 에디터 코드가 꼭 필요하면 `Assets/**/Editor/` 폴더에 두거나 `#if UNITY_EDITOR ... #endif`로 감싼다.
   - 확인 명령: `git grep -nE "using UnityEditor|UnityEditor\.|System\.Drawing" -- "Assets/*.cs" ":!Assets/**/Editor/**"`

## 주의 사항

### 커밋 메시지 규칙
아래 형식을 따른다: `타입: 내용`

```bash
git commit -m "feat: 새로운 기능 추가"
```

**타입 목록**
- `feat`: 새로운 기능 추가
- `fix`: 버그 수정
- `refactor`: 코드 리팩토링 (동작 변경 없음)
- `docs`: 문서(README 등) 수정
- `chore`: 빌드, 설정 등 기타 변경

- 콜론(:) 뒤에 공백 하나
- 메시지는 한글로 작성 (파일명·고유명사·약어는 예외)

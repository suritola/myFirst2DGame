---
name: precheck
description: 커밋하기 전에 아직 커밋하지 않은 변경을 검토한다. 위험한 부분과 AGENT.md 규칙 위반(에디터 전용 코드 등)을 찾는다.
---

1. `git status` 와 `git diff` 로 커밋하지 않은 변경을 모두 본다. 바뀐 파일은 diff 줄만 보지 말고 필요한 만큼 전체를 읽는다.
2. AGENT.md 규칙 2 검사를 실행한다:
   `git grep -nE "using UnityEditor|UnityEditor\.|System\.Drawing" -- "Assets/*.cs" ":!Assets/**/Editor/**" ":!Assets/Editor/**"`
3. 다음을 찾는다:
   - 빌드 실패 가능성: 에디터 전용 API, 빠진 using, 잘못된 참조
   - 런타임 오류: null 참조, 파괴된 오브젝트 접근, 남아 도는 코루틴
   - 세이브 호환성 깨짐: PlayerPrefs 키나 직렬화 필드 이름 변경
   - 의도치 않게 바뀐 에셋: .asset, ProjectSettings, 폰트 SDF, .meta GUID
   - 남은 디버그 코드: 과한 `Debug.Log`, 테스트용 값
4. 심각한 것부터 `파일:줄` 과 함께 보고하고, 커밋에서 빼야 할 파일이 있으면 알려 준다.

코드는 수정하지 않는다.

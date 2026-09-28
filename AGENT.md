# AGENT.md

이 저장소에서 작업하는 에이전트가 따라야 할 규칙.

## 규칙

1. 커밋 메시지는 무조건 한글로 작성한다.
2. 커밋 전에 `Assets/` 아래 게임 스크립트에 에디터 전용 코드가 없는지 확인한다.
   - `using UnityEditor...`(예: `using static UnityEditor.Experimental.GraphView.GraphView;`)는 빌드를 실패시킨다. IDE 자동완성이 몰래 추가하는 경우가 많다.
   - `using System.Drawing;`도 쓰지 않는다 (빌드에서 참조되지 않고, `UnityEngine.Color`와 이름이 겹친다).
   - 에디터 코드가 꼭 필요하면 `Assets/**/Editor/` 폴더에 두거나 `#if UNITY_EDITOR ... #endif`로 감싼다.
   - 확인 명령: `git grep -nE "using UnityEditor|UnityEditor\.|System\.Drawing" -- "Assets/*.cs" ":!Assets/**/Editor/**" ":!Assets/Editor/**"`
3. 사용자와의 모든 대화는 **한국어로만** 한다. (진행 상황 보고, 중간 알림, 질문, 최종 보고 모두 · 코드 식별자와 파일명은 예외)
4. 빌드 버전은 **주.부.수** (예: `1.7.6`) + 부 버전마다 **코드네임** 으로 쓴다.
   - 배포할 때마다 끝자리(수) +1. 끝자리가 9를 넘으면 부 +1 · 끝자리 0 (`1.7.9 → 1.8.0`), 부가 9를 넘으면 주 +1 (`1.9.9 → 2.0.0`)
   - 코드네임: 부 버전(두 번째 자리)마다 영어 이름 하나. `Assets/Scripts/WindowTitle.cs` 의 `Codenames` 표 한 곳에 적는다 (게임 메인 메뉴와 릴리즈 스크립트가 같이 읽음). 1.7 = **Soul Harvest**. 부가 바뀌면 그 판의 핵심 내용에 맞는 새 이름을 추가한다
   - 메인 메뉴 표시: `v1.7.6 · Soul Harvest` (자동), 릴리즈 제목: `Soul Saver v1.7.6 — Soul Harvest` (자동)
   - 태그 · 릴리스 · 패치노트 파일 이름은 앞에 `v`를 붙인다 (`v1.7.6`, `docs/patch-notes/v1.7.6.md`). `ProjectSettings` 의 `bundleVersion` 은 번호만 (`1.7.6`)
   - `pwsh tools/release.ps1` 을 버전 없이 실행하면 지난 릴리즈 +1 로 자동 계산

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


#### 참고 사항
1. 모든 작업을 수행할 때, 진행률, 토큰 사용량, 잔여 토큰량을 게이지로 시각화해서 실시간으로 표시
2. 입력한 명령에서 기존과 동일한 내용은 무시하고 새롭게 추가되거나 변경된 것만 읽어서 작업속도를 높히기
3. 모든 작업이 완료되면 도레미파솔 사운드를 재생
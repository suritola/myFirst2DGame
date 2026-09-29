---
name: patch-notes
description: 두 버전 사이의 커밋을 읽고 기존 패치노트 형식에 맞춰 docs/patch-notes/v주.부.수.md 초안을 쓴다. 사용 예 /patch-notes 1.8.5 1.8.6
argument-hint: <이전 버전> <새 버전>
disable-model-invocation: true
---

요청: $ARGUMENTS
비어 있으면 가장 최근 패치노트 버전에서 다음 버전(AGENT.md 규칙 4)으로 잡는다.

1. `docs/patch-notes/` 의 최근 노트 두세 개를 읽고 형식과 말투를 익힌다. 제목은 `# Soul Saver 주.부.수 패치노트 — 한 줄 요약` 이고, 섹션 제목에 이모지를 쓰며, 플레이어 입장의 쉬운 말과 굵은 강조를 쓴다.
2. 커밋 메시지 끝의 `(주.부.수)` 표기로 두 버전 사이의 커밋을 찾고, 필요하면 `git show` 로 실제 변경을 확인한다.
3. 새 기능 · 변경 · 버그 수정 · 참고로 묶어서 초안을 쓴다. 플레이어가 느끼는 차이가 없는 내부 리팩토링이나 도구 변경은 뺀다.
4. `docs/patch-notes/v<새 버전>.md` 로 저장하고, 사용자가 확인하면 `docs:` 타입으로 커밋한다.

---
name: commit
description: 지금 변경 내용을 요약해서 AGENT.md 규칙(한글, 타입 접두어)대로 커밋한다.
disable-model-invocation: true
---

1. `git status` 와 `git diff` 로 변경을 확인한다. 이번 작업과 상관없는 변경(사용자가 따로 건드린 에셋 등)은 스테이징하지 않고, 무엇을 뺐는지 알린다.
2. AGENT.md 규칙 2 검사를 실행해서 걸리면 커밋하지 않고 보고한다.
3. 최근 커밋 메시지(`git log --oneline -10`) 스타일에 맞춰 `타입: 내용` 형식의 한글 메시지를 쓴다. 타입은 feat / fix / refactor / docs / chore 중 하나이고, 릴리스 버전 작업이면 끝에 `(주.부.수)` 를 붙인다.
4. 파일을 이름으로 골라 스테이징하고 커밋한다. `git add -A` 는 쓰지 않고, push도 하지 않는다.
5. 커밋 해시, 메시지, 포함한 파일 목록을 보고한다.

추가 지시: $ARGUMENTS

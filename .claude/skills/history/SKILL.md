---
name: history
description: 파일이나 기능의 커밋 기록을 읽고 어떻게, 왜 지금 모습이 되었는지 정리한다. 사용 예 /history Assets/Scripts/boss.cs
argument-hint: <파일 경로나 기능>
---

대상: $ARGUMENTS

1. 파일이면 `git log --follow` 로 커밋 기록을 본다. 기능 이름이면 `git log -S` 나 `git log --grep` 으로 관련 커밋을 찾는다.
2. 중요한 커밋은 `git show` 로 변경 내용을 확인한다. 커밋 메시지 끝의 `(1.8.x)` 버전 표기를 활용한다.
3. 시간 순서대로 주요 변화와 그 이유를 정리한다. 필요하면 `docs/patch-notes/` 의 해당 버전 노트도 참고한다.
4. 과거 결정 때문에 지금 코드에 남은 흔적(임시 코드, 호환용 분기)이 있으면 짚는다.

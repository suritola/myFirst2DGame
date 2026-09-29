---
name: add-rule
description: Claude가 반복하는 실수를 AGENT.md 규칙으로 만들어 다음부터 하지 않게 한다. 사용 예 /add-rule 패치노트에 내부 리팩토링을 자꾸 넣음
argument-hint: <반복되는 실수>
disable-model-invocation: true
---

반복되는 실수: $ARGUMENTS

1. AGENT.md 를 읽고 비슷한 규칙이 이미 있는지 확인한다. 있으면 새로 만들지 말고 그 규칙을 더 분명하게 고친다.
2. 없으면 알맞은 자리에 짧고 확인할 수 있는 규칙으로 추가한다. "~하지 않는다" 식으로 쓰고, 필요하면 확인 명령이나 예시를 붙인다.
3. 바뀐 부분을 보여 주고 `docs:` 타입으로 커밋한다.

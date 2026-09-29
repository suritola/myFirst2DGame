---
name: impact
description: 어떤 클래스·함수·필드·에셋을 지우거나 바꾸면 무엇이 깨지는지 미리 분석한다. 삭제나 이름 변경 전에 쓴다. 사용 예 /impact EnemySkill 클래스
argument-hint: <대상>
---

대상: $ARGUMENTS

1. 코드에서 대상을 참조하는 곳을 모두 찾는다 (`파일:줄`).
2. Unity 쪽 참조도 확인한다: 프리팹·씬·ScriptableObject(.prefab, .unity, .asset)에서 스크립트 GUID나 직렬화 필드 이름으로 이어진 곳, `Resources.Load` 문자열 경로, `SendMessage`/`Invoke` 문자열 호출, PlayerPrefs 키.
3. 기존 플레이어 세이브가 깨지는지 따진다.
4. 결론: 바로 지워도 되는지, 여러 곳을 함께 고쳐야 하는지, 위험한지 판단하고 필요한 수정 목록을 준다.

코드는 수정하지 않는다.

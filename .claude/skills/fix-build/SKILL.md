---
name: fix-build
description: Unity 컴파일·빌드 오류를 근본 원인에서 고친다. 오류를 숨기는 땜질은 하지 않는다. 오류 메시지를 붙여 넣거나, 없으면 Editor.log에서 찾는다.
---

오류: $ARGUMENTS

1. 오류가 주어지지 않았으면 `%LOCALAPPDATA%\Unity\Editor\Editor.log` 에서 마지막 컴파일 오류(`error CS`)를 찾는다.
2. 오류가 난 코드와, 그 원인이 된 최근 변경(`git log -p`)을 찾는다.
3. 흔한 원인부터 확인한다: 게임 스크립트의 `UnityEditor`/`System.Drawing` using(AGENT.md 규칙 2), 이름 충돌(`Color`, `Random`, `Debug`), 지운 멤버를 아직 참조하는 곳, Editor 폴더 밖의 에디터 코드.
4. 원인을 고친다. 경고 억제, 기능 통째 주석 처리, try/catch로 오류 삼키기 같은 우회는 쓰지 않는다.
5. 무거운 배치 빌드는 돌리지 않는다(AGENT.md 규칙 5). 대신 같은 종류의 문제가 다른 곳에도 있는지 검색으로 확인한다.
6. 바꾼 파일을 보고하고 `fix:` 타입으로 커밋한다.

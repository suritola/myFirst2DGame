# 익명 플레이 데이터 (Telemetry)

판이 끝날 때마다 게임이 `balance_log.csv` 와 같은 한 줄을 구글 시트로 보냅니다.
보내는 것: 날짜 · 버전 · 캐릭터 · 난이도 · 결과 · 시간 · 레벨 · 처치 수 · 영혼 조각 · 진화 · 영혼 트리 가지별 칸 수 · 카드 수 · 장 · 입힌/받은 피해(출처별) · 마지막에 맞은 공격, 그리고 **무작위 플레이어 번호** 하나.
스팀 ID · 이름 · 기기 정보는 보내지 않습니다. 플레이어는 설정 → 게임 → 「플레이 데이터 보내기」로 끌 수 있습니다 (끄면 모아 둔 것도 지움).

- 게임 쪽: `Assets/Scripts/Telemetry.cs` (보내지 못한 줄은 `telemetry_pending.txt` 에 모아 두었다가 다음에 보냄, 에디터에서는 보내지 않음)
- 서버: `tools/telemetry/Code.gs` (Google Apps Script)
- 분석: `tools/telemetry/analyze.py`
- 약관 · 개인정보: `docs/steam/eula.md`

## 1. 수집 서버 만들기 (한 번만, 5분)

1. 구글 드라이브에서 새 **구글 시트**를 만듭니다 (이름 예: `Soul Saver 플레이 데이터`)
2. 시트 메뉴 **확장 프로그램 → Apps Script**
3. 나온 `Code.gs` 내용을 모두 지우고 `tools/telemetry/Code.gs` 를 붙여 넣고 저장
4. 오른쪽 위 **배포 → 새 배포**
   - 유형: **웹 앱**
   - 다음 사용자 인증 정보로 실행: **나**
   - 액세스 권한이 있는 사용자: **모든 사용자**
   - 배포 → 권한 허용 (「안전하지 않음」 경고가 나오면 고급 → 이동)
5. 나온 **웹 앱 URL** (`https://script.google.com/macros/s/.../exec`)을 복사
6. `Assets/Scripts/Telemetry.cs` 의 `Endpoint = ""` 에 그 URL을 넣고 빌드

> 코드를 고쳐 다시 배포할 때는 **배포 관리 → 수정 → 새 버전**으로 해야 URL이 그대로입니다.

## 2. 내 컴퓨터에서 분석

1. 시트에서 `runs_28` 탭을 열고 **파일 → 다운로드 → 쉼표로 구분된 값(.csv)**
2. 실행:

```
python tools/telemetry/analyze.py "Soul Saver 플레이 데이터 - runs_28.csv"
python tools/telemetry/analyze.py runs.csv --version 2.2.1
```

난이도별 · 캐릭터별 클리어율 · 죽는 비율 · 평균 시간 · 평균 레벨, 어느 장에서 죽는지, 무엇에 죽는지, 많이 고른 진화를 보여 줍니다.
엑셀 · 구글 시트 피벗 표로 직접 봐도 됩니다.

## 참고

- 칸이 바뀌면(게임 업데이트로 `balance_log.csv` 머리줄이 바뀌면) 시트에 `runs_<칸 수>` 탭이 새로 생깁니다
- Apps Script 무료 한도는 하루 수만 건 수준이라 인디 게임에는 충분합니다
- `Token` 은 게임 파일 안에 있으므로 비밀번호 역할은 못 합니다. 이상한 줄이 많이 들어오면 `Code.gs` 와 `Telemetry.cs` 의 `TOKEN` 을 함께 바꾸고 다시 배포하세요

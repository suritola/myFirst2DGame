# 스팀 출시 준비

Gun Saver를 스팀에 올리기 위해 **이미 해 둔 것**과 **직접 해야 하는 것**을 정리했습니다.

## ✅ 해 둔 것

| 항목 | 위치 |
|---|---|
| Steamworks.NET 연동 (초기화 · 콜백 · 스팀 밖 실행 시 스팀으로 재실행) | `Assets/Scripts/SteamManager.cs`, `Packages/manifest.json` |
| 업적 26개 (코드 연결 + 4개 언어 이름 · 설명 + 아이콘) | [achievements.md](achievements.md), `art/achievements/` |
| 스팀용 빌드와 GitHub용 빌드 분리 (`-steam`일 때만 연동, 아니면 `DISABLESTEAMWORKS`) | `Assets/Editor/BuildScript.cs` |
| 게임 아이콘 (실행 파일 · 작업 표시줄) | `Assets/Art/Icon/GunSaverIcon.png` |
| SteamPipe 업로드 스크립트 | `tools/steam-upload.ps1`, `tools/steam/steam-config.json` |
| 상점 페이지 글 (한 · 영 · 일 · 중), 태그, 시스템 요구 사항 | [store-page.md](store-page.md) |
| 상점 이미지 · 라이브러리 이미지 · 스크린샷 6장 | `art/capsules/`, `art/screenshots/` |

> 이미지는 게임 그림을 합성해 만든 **초안**입니다. 상점 첫인상을 좌우하니, 여유가 되면 캡슐 이미지는 따로 그린 그림으로 바꾸는 것을 추천합니다.

## 📝 직접 해야 하는 것

### 1. 값 (채워 둠)
- [x] App ID `5328770` · Depot ID `5328771` · 빌드 계정 `phdstudy` — `tools/steam/steam-config.json`, `SteamManager.cs`
- [x] 개발사 `박민근` — `ProjectSettings` Company Name (이름이 바뀌어 저장된 설정이 한 번 초기화됨)
- [ ] steamcmd를 다른 곳에 설치했다면 `steam-config.json`의 `steamcmd` 경로 수정

### 2. steamcmd 설치
- [ ] https://developer.valvesoftware.com/wiki/SteamCMD 에서 받아 `C:\steamcmd`에 풀기

### 3. Steamworks 설정 (partner.steamgames.com)
- [ ] **SteamPipe → 디포**: 디포 1개 (Windows, 모든 언어)
- [ ] **설치 → 일반**: 실행 옵션 — 실행 파일 `Gun Saver.exe`, OS Windows, 64비트
- [ ] **스탯 및 업적**: [achievements.md](achievements.md) 대로 26개 만들고 **게시**
- [ ] **상점 페이지**: [store-page.md](store-page.md) 글, `art/` 이미지 올리기
- [ ] **가격** 정하기 · **출시일** 정하기
- [ ] **콘텐츠 설문 / 연령 등급** (폭력성: 판타지 몬스터와의 전투, 피 표현 약함)
- [ ] **지원 언어**: 한국어, 영어, 일본어, 중국어 간체 (인터페이스 · 자막 ✔, 음성 ✘)

### 4. 빌드 올리기
```powershell
pwsh tools/steam-upload.ps1 -Version v2.4 -Preview   # 먼저 미리보기 (실제로 안 올림)
pwsh tools/steam-upload.ps1 -Version v2.4            # 업로드 (처음엔 비밀번호 · Steam Guard 코드 입력)
```
- [ ] Steamworks → SteamPipe → 빌드에서 올린 빌드를 **default** 브랜치로 설정
- [ ] 스팀 클라이언트에서 설치 → 실행 → 오버레이(Shift+Tab) · 업적이 뜨는지 확인

### 5. 검토와 출시
- [ ] **"곧 출시" 페이지 공개**: 출시 최소 2주 전에 공개해야 합니다
- [ ] **상점 페이지 검토 요청** · **빌드 검토 요청** (보통 3~5 영업일)
- [ ] 검토 통과 후 출시 버튼

## ⚠️ 라이선스 확인 필요

판매용 게임은 모든 그림 · 소리 · 폰트를 상업적으로 써도 되는지 확인해야 합니다.

| 에셋 | 상태 |
|---|---|
| 캐릭터 스프라이트 (Trevor Pupkin 팩) | ✅ 상업 사용 가능 (`Assets/Sprites/Read Me.txt`) · 팩 자체 재배포만 금지 |
| Noto Sans JP / SC | ✅ SIL OFL (`Assets/Resources/Fonts/OFL-NotoSansCJK.txt`) |
| LiberationSans (TextMesh Pro 기본) | ✅ SIL OFL |
| 배경음악 5곡 · 코드로 만든 효과음 | ✅ 이 프로젝트에서 직접 만듦 |
| **카페24 단정해 폰트** | ❓ 받은 곳 · 라이선스 파일 확인 (카페24 폰트는 보통 상업 무료) |
| **효과음 mp3 5개** (`Assets/Sounds/`) | ❓ 출처 확인 필요 — 모르면 바꾸는 게 안전 |
| 지옥 · 초원 적, 타일, UI, 이펙트 스프라이트 | ❓ 직접 그린 것인지 받은 것인지 확인 |

## 이미지 규격

| 파일 (`art/capsules/`) | 크기 | Steamworks 칸 |
|---|---|---|
| `header_capsule.png` | 920×430 | 헤더 캡슐 |
| `small_capsule.png` | 462×174 | 작은 캡슐 |
| `main_capsule.png` | 1232×706 | 메인 캡슐 |
| `vertical_capsule.png` | 748×896 | 세로 캡슐 |
| `library_capsule.png` | 600×900 | 라이브러리 캡슐 |
| `library_hero.png` | 3840×1240 | 라이브러리 히어로 (글자 없음) |
| `library_logo.png` | 1280×720 | 라이브러리 로고 (투명 배경) |
| `community_icon.jpg` | 184×184 | 커뮤니티 아이콘 |
| `art/screenshots/*.png` | 1920×1080 | 스크린샷 (최소 5장) |
| `art/achievements/*.jpg` | 256×256 | 업적 아이콘 (달성 / 미달성) |

이미지는 `tools/steam/store-art/`의 PlayMode 테스트로 게임 화면을 합성해 만들었습니다. 다시 만들려면 두 파일을 프로젝트 복사본의 `Assets/StoreArt/`에 넣고 `Unity -batchmode -runTests -testPlatform PlayMode`를 실행하세요 (환경 변수 `ART_OUT` = 저장 폴더). 스크린샷은 초안이라 직접 플레이하며 찍은 화면으로 바꾸는 것을 추천합니다.

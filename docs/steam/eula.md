# Soul Saver 최종 사용자 사용권 계약 (EULA) · 개인정보 처리방침

> Steamworks → 앱 관리 → 상점 페이지 관리(또는 「앱 관리 → 설치 → EULA」)의 **사용자 정의 EULA** 에 아래 한국어 · 영어 본문을 각 언어로 넣습니다.
> `[공개용 이메일]` 은 실제 연락처로 바꿔 주세요. 법률 자문을 받은 문서가 아니므로, 상업 출시 전에 필요하면 전문가 검토를 받으세요.
> 수집 항목이 바뀌면(`Assets/Scripts/Telemetry.cs`, `RunStats.WriteBalanceLog`) 「4. 개인정보」도 함께 고칩니다.

---

## 한국어

**최종 수정일: 2026년 10월 10일**

본 계약은 Park Mingeun(이하 「개발자」)이 만든 게임 **Soul Saver**(이하 「게임」)를 설치하거나 실행하는 사용자(이하 「사용자」)와 개발자 사이의 계약입니다. 게임을 설치하거나 실행하면 본 계약에 동의한 것으로 봅니다. 동의하지 않으면 게임을 설치하거나 실행하지 마세요.

### 1. 사용권

1. 개발자는 사용자에게 Steam 계정으로 정당하게 얻은 게임을 개인적 · 비상업적 목적으로 사용할 수 있는, 양도할 수 없고 독점적이지 않은 사용권을 줍니다.
2. 게임은 판매되는 것이 아니라 사용권이 주어지는 것이며, 게임과 그 안의 코드 · 그림 · 소리 · 음악 · 문구 등 모든 권리는 개발자에게 있습니다.
3. 본 계약 외에 Valve의 **Steam 구독자 계약**도 함께 적용됩니다. 두 계약이 서로 다르면 게임 사용에 관해서는 본 계약이 우선합니다.

### 2. 금지 행위

사용자는 다음을 할 수 없습니다.

1. 게임 또는 그 일부를 복제 · 배포 · 판매 · 대여 · 재사용권 부여하는 행위
2. 법이 허용하는 범위를 넘어 게임을 역설계 · 디컴파일 · 분해하거나, 게임의 그림 · 소리 등 자원을 뽑아 다른 곳에 쓰는 행위
3. 게임의 수집 서버에 거짓 데이터를 대량으로 보내거나, 서버 운영을 방해하는 행위
4. 법령이나 제3자의 권리를 침해하는 방식으로 게임을 쓰는 행위

**허용 사항**: 게임 플레이 영상 · 스크린샷 · 방송(수익 창출 포함)은 자유롭게 만들고 공유할 수 있습니다. 개인적으로 즐기는 모드나 설정 파일 수정도 허용하지만, 그로 인한 문제는 지원하지 않습니다.

### 3. 업데이트

개발자는 게임을 개선하기 위해 내용 · 밸런스 · 기능을 예고 없이 바꾸거나 업데이트할 수 있으며, 업데이트는 Steam을 통해 자동으로 설치될 수 있습니다.

### 4. 개인정보 (익명 플레이 데이터)

게임을 더 재미있고 공정한 난이도로 다듬기 위해, 게임은 한 판이 끝날 때마다 아래 **익명 플레이 데이터**를 개발자의 수집 서버(Google Apps Script · Google 스프레드시트)로 보냅니다.

- **보내는 정보**: 날짜와 시각, 게임 버전, 고른 캐릭터와 난이도, 결과(클리어 · 사망 · 중단), 플레이 시간, 레벨, 처치 수, 영혼 조각 수, 고른 무기 진화 · 영혼 트리 가지별 칸 수 · 카드 수, 도달한 장, 입힌 피해와 받은 피해(공격 종류별 합계), 마지막에 맞은 공격, 그리고 게임이 처음 만든 **무작위 식별 번호**
- **보내지 않는 정보**: 이름, Steam ID · 계정 정보, 이메일, 결제 정보, 기기 · 운영체제 정보, 저장 파일, 채팅 등 개인을 직접 알아볼 수 있는 정보
- **목적**: 난이도 · 밸런스 조정과 오류 찾기 (통계로만 분석하며, 광고나 마케팅에 쓰지 않습니다)
- **보관**: 게임을 개선하는 동안 보관하고, 서비스를 끝내면 지웁니다. 판매하거나 제3자와 공유하지 않습니다.
- **처리 위탁 · 국외 이전**: 데이터는 Google LLC(미국)의 서버에 저장됩니다. 전송 과정에서 Google이 접속 IP 주소를 볼 수 있으나, 개발자는 IP 주소를 저장하지 않습니다.
- **거부 방법**: 게임 **설정 → 게임 → 「플레이 데이터 보내기」**를 끄면 더 이상 보내지 않으며, 아직 보내지 못하고 컴퓨터에 모아 둔 데이터도 지웁니다. 이 기능을 꺼도 게임 이용에는 아무 제한이 없습니다.
- **삭제 요청**: 이미 보낸 데이터를 지우고 싶으면 아래 연락처로 요청해 주세요. 무작위 식별 번호만으로는 본인 확인이 어려울 수 있어, 게임 저장 파일(`%LOCALAPPDATA%\Soul Saver\save.json`)의 `telemetry.id` 값을 함께 알려 주시면 해당 기록을 지웁니다.
- **아동**: 게임은 만 14세 미만 아동의 개인정보를 의도적으로 수집하지 않습니다.

게임의 저장 데이터(진행 · 설정)는 사용자의 컴퓨터와 Steam 클라우드에만 저장되며, 개발자는 이를 볼 수 없습니다. Steam 이 처리하는 정보(구매 · 업적 · 플레이 시간 등)는 Valve의 개인정보 처리방침을 따릅니다.

### 5. 보증의 부인

게임은 「있는 그대로」 제공됩니다. 개발자는 법이 허용하는 범위에서 게임에 오류가 없거나, 중단 없이 작동하거나, 특정 목적에 맞는다는 것을 보증하지 않습니다.

### 6. 책임의 제한

법이 허용하는 범위에서, 개발자는 게임 사용 또는 사용할 수 없음으로 생긴 간접 · 부수 · 특별 · 결과적 손해에 책임지지 않으며, 어떤 경우에도 개발자의 책임은 사용자가 게임에 실제로 지불한 금액을 넘지 않습니다. 다만 개발자의 고의 또는 중대한 과실로 생긴 손해는 예외로 합니다.

### 7. 계약의 종료

사용자가 본 계약을 어기면 사용권은 자동으로 끝나며, 사용자는 게임을 지워야 합니다. 사용자는 언제든 게임을 지워 본 계약을 끝낼 수 있습니다.

### 8. 준거법 · 분쟁

본 계약은 대한민국 법을 따르며, 분쟁은 민사소송법에 따른 관할 법원에서 해결합니다. 사용자가 사는 나라의 소비자 보호법이 더 유리하게 정한 권리는 본 계약으로 제한되지 않습니다.

### 9. 계약의 변경 · 연락처

개발자는 본 계약을 바꿀 수 있으며, 바뀐 내용은 Steam 상점 페이지 또는 게임 패치노트로 알립니다. 바뀐 뒤에도 게임을 계속 쓰면 바뀐 계약에 동의한 것으로 봅니다.

문의 · 데이터 삭제 요청: **[공개용 이메일]**

---

## English

**Last updated: October 10, 2026**

This End User License Agreement ("Agreement") is between you ("User") and Park Mingeun ("Developer"), the creator of **Soul Saver** ("Game"). By installing or running the Game, you agree to this Agreement. If you do not agree, do not install or run the Game.

### 1. License

1. The Developer grants you a non-exclusive, non-transferable license to use the Game, lawfully obtained through your Steam account, for personal, non-commercial purposes.
2. The Game is licensed, not sold. All rights in the Game, including its code, art, sound, music and text, remain with the Developer.
3. Valve's **Steam Subscriber Agreement** also applies. If the two conflict regarding use of the Game, this Agreement prevails.

### 2. Restrictions

You may not:

1. Copy, distribute, sell, rent or sublicense the Game or any part of it;
2. Reverse engineer, decompile or disassemble the Game beyond what applicable law permits, or extract its art, sound or other assets for use elsewhere;
3. Flood the Game's data server with false data or otherwise interfere with its operation;
4. Use the Game in a way that violates any law or third-party rights.

**You may**: freely create and share gameplay videos, screenshots and streams (including monetized ones). Personal mods and config edits are allowed but are not supported.

### 3. Updates

The Developer may change or update the Game's content, balance and features without notice. Updates may be installed automatically through Steam.

### 4. Privacy (Anonymous Play Data)

To improve the Game's difficulty and balance, the Game sends the following **anonymous play data** to the Developer's collection server (Google Apps Script / Google Sheets) at the end of each run.

- **What is sent**: date and time, game version, chosen character and difficulty, result (clear / death / quit), play time, level, kill count, soul shard counts, weapon evolutions, soul tree nodes per branch, number of cards, chapter reached, damage dealt and taken (totals by attack type), the attack that hit you last, and a **random identifier** created by the Game.
- **What is NOT sent**: your name, Steam ID or account details, email, payment details, device or OS information, save files, chat, or anything else that directly identifies you.
- **Purpose**: tuning difficulty and balance and finding bugs. The data is analyzed only in aggregate and is never used for advertising or marketing.
- **Retention**: kept while the Game is being improved and deleted when the service ends. It is never sold or shared with third parties.
- **Processor / international transfer**: the data is stored on servers of Google LLC (USA). Google may see your IP address during transmission; the Developer does not store IP addresses.
- **Opting out**: turn off **Settings → Game → "Send play data"**. The Game stops sending and deletes any data still waiting on your computer. Turning it off does not limit the Game in any way.
- **Deletion**: to delete data already sent, contact us below with the `telemetry.id` value from your save file (`%LOCALAPPDATA%\Soul Saver\save.json`) and we will delete the matching records.
- **Children**: the Game does not knowingly collect personal information from children under 14.

Your save data (progress and settings) is stored only on your computer and in Steam Cloud; the Developer cannot access it. Information processed by Steam (purchases, achievements, play time, etc.) is governed by Valve's privacy policy.

### 5. Disclaimer of Warranty

The Game is provided "as is". To the extent permitted by law, the Developer does not warrant that the Game will be error-free, uninterrupted or fit for a particular purpose.

### 6. Limitation of Liability

To the extent permitted by law, the Developer is not liable for any indirect, incidental, special or consequential damages arising from the use of or inability to use the Game, and the Developer's total liability shall not exceed the amount you actually paid for the Game, except for damages caused by the Developer's intent or gross negligence.

### 7. Termination

This license ends automatically if you breach this Agreement, and you must then delete the Game. You may end this Agreement at any time by deleting the Game.

### 8. Governing Law

This Agreement is governed by the laws of the Republic of Korea, and disputes shall be resolved in the court of competent jurisdiction under the Korean Civil Procedure Act. Nothing in this Agreement limits rights you have under the consumer protection laws of your country of residence.

### 9. Changes / Contact

The Developer may change this Agreement and will announce changes on the Steam store page or in the Game's patch notes. Continuing to use the Game after a change means you accept the updated Agreement.

Questions and deletion requests: **[공개용 이메일 / public email]**

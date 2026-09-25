# 스팀 업적 등록표

Steamworks → **앱 관리 → 스탯 및 업적 → 업적**에서 아래 **API 이름**을 그대로 써서 20개를 만듭니다.
API 이름이 다르면 게임에서 풀리지 않습니다. (코드: `Assets/Scripts/SteamManager.cs`의 `SteamAchievements`)

- 아이콘: `docs/steam/art/achievements/` — 달성 `<API>.jpg`, 미달성 `<API>_locked.jpg` (256×256)
- 모두 **숨김 아님**으로 두면 됩니다. (`ACH_CLEAR`만 숨김으로 해도 좋음)
- `ACH_KILLS_3000_TOTAL`의 누적 처치 수는 이 PC에 저장됩니다 (PlayerPrefs `stats.totalKills`).
- 다 만든 뒤 **게시(Publish)**를 눌러야 적용됩니다.

| API 이름 | 한국어 | English | 日本語 | 中文 |
|---|---|---|---|---|
| `ACH_FIRST_ULT` | **첫 필살기** — 필살기를 처음 사용했다 | **First Ultimate** — Use an ultimate for the first time | **初めての必殺技** — 初めて必殺技を使った | **首次必杀** — 第一次使用必杀技 |
| `ACH_ENTER_HELL` | **지옥의 문** — 불타는 지옥에 들어섰다 | **Gates of Hell** — Enter the Burning Hell | **地獄の門** — 燃える地獄に足を踏み入れた | **地狱之门** — 进入燃烧地狱 |
| `ACH_ENTER_MEADOW` | **바깥 공기** — 초원에 다다랐다 | **Fresh Air** — Reach the Meadow | **外の空気** — 草原にたどり着いた | **新鲜空气** — 抵达草原 |
| `ACH_MIDBOSS` | **덩치 큰 녀석** — 중간 보스를 처치했다 | **Big One Down** — Defeat a mid-boss | **大物退治** — 中ボスを倒した | **大家伙** — 击败精英首领 |
| `ACH_EVOLVE` | **진화** — 특수 능력을 처음 진화시켰다 | **Evolution** — Evolve a special ability | **進化** — 特殊能力を初めて進化させた | **进化** — 首次进化特殊能力 |
| `ACH_LEVEL_10` | **숙련된 총잡이** — 한 판에서 레벨 10에 도달했다 | **Seasoned Gunner** — Reach level 10 in one run | **熟練ガンナー** — 1回のプレイでレベル10に到達 | **老练枪手** — 单局达到10级 |
| `ACH_KILLS_500` | **학살자** — 한 판에서 적 500마리를 처치했다 | **Slayer** — Defeat 500 enemies in one run | **殲滅者** — 1回のプレイで敵を500体倒した | **屠戮者** — 单局击败500个敌人 |
| `ACH_BOSS_LICH` | **왕의 몰락** — 리치 왕을 쓰러뜨렸다 | **Fall of the King** — Defeat the Lich King | **王の失墜** — リッチ王を倒した | **王之陨落** — 击败巫妖王 |
| `ACH_BOSS_DEMON` | **지옥을 식히다** — 지옥의 군주를 쓰러뜨렸다 | **Hell Freezes Over** — Defeat the Demon Lord | **地獄を鎮めて** — 地獄の君主を倒した | **冷却地狱** — 击败地狱领主 |
| `ACH_CLEAR` | **Gun Saver** — 킹 슬라임을 쓰러뜨리고 게임을 클리어했다 | **Gun Saver** — Defeat the King Slime and clear the game | **Gun Saver** — キングスライムを倒してゲームをクリア | **Gun Saver** — 击败史莱姆王，通关游戏 |
| `ACH_FIRST_DEATH` | **다시 일어나라** — 처음으로 쓰러졌다 | **Get Back Up** — Fall in battle for the first time | **立ち上がれ** — 初めて倒れた | **重新站起来** — 第一次倒下 |
| `ACH_SHOPPER` | **단골 손님** — 떠돌이 상점을 처음 열었다 | **Regular Customer** — Open the wandering shop | **常連客** — さすらいのショップを初めて開いた | **老主顾** — 第一次打开流浪商店 |
| `ACH_FULL_SKILLS` | **손이 모자라** — 스킬 칸 3개를 모두 채웠다 | **All Hands** — Fill all 3 skill slots | **手が足りない** — スキル枠3つをすべて埋めた | **手忙脚乱** — 填满全部3个技能栏 |
| `ACH_ARSENAL` | **무기고** — 특수 무기 2개를 동시에 가졌다 | **Arsenal** — Carry 2 special weapons at once | **武器庫** — 特殊武器を2つ同時に持った | **军火库** — 同时拥有2把特殊武器 |
| `ACH_EVOLVE_3` | **완전체** — 한 판에서 능력 3개를 진화시켰다 | **Final Form** — Evolve 3 abilities in one run | **完全体** — 1回のプレイで能力を3つ進化させた | **完全体** — 单局进化3项能力 |
| `ACH_ULT_30` | **필살기 중독** — 한 판에서 필살기를 30번 사용했다 | **Ultimate Addict** — Use 30 ultimates in one run | **必殺技中毒** — 1回のプレイで必殺技を30回使った | **必杀成瘾** — 单局使用30次必杀技 |
| `ACH_LEVEL_15` | **베테랑** — 한 판에서 레벨 15에 도달했다 | **Veteran** — Reach level 15 in one run | **ベテラン** — 1回のプレイでレベル15に到達 | **老兵** — 单局达到15级 |
| `ACH_KILLS_3000_TOTAL` | **전설의 사냥꾼** — 누적 적 3000마리를 처치했다 | **Legendary Hunter** — Defeat 3,000 enemies in total | **伝説の狩人** — 累計で敵を3000体倒した | **传奇猎手** — 累计击败3000个敌人 |
| `ACH_NO_HIT_BOSS` | **완벽한 결투** — 피해를 받지 않고 보스를 쓰러뜨렸다 | **Flawless Duel** — Defeat a boss without taking damage | **完璧な決闘** — ダメージを受けずにボスを倒した | **完美对决** — 无伤击败首领 |
| `ACH_CLOSE_CALL` | **구사일생** — 체력 10% 이하로 보스를 쓰러뜨렸다 | **Close Call** — Defeat a boss with 10% HP or less | **九死に一生** — 体力10%以下でボスを倒した | **九死一生** — 以10%以下的生命值击败首领 |

## 풀리는 조건 (코드 기준)

| API 이름 | 조건 |
|---|---|
| `ACH_FIRST_ULT` | 우클릭 필살기 발동 (조준형 · 즉발형 모두) |
| `ACH_ENTER_HELL` / `ACH_ENTER_MEADOW` | 스테이지 2 / 3 시작 |
| `ACH_MIDBOSS` | 중간 보스 처치 |
| `ACH_EVOLVE` | 특수 강화(T)에서 능력 진화 |
| `ACH_LEVEL_10` | 플레이어 레벨 10 |
| `ACH_KILLS_500` | 한 판(게임 씬을 새로 시작한 뒤) 처치 수 500 |
| `ACH_BOSS_LICH` / `ACH_BOSS_DEMON` / `ACH_CLEAR` | 스테이지 1 / 2 / 3 보스 처치 |
| `ACH_FIRST_DEATH` | 게임 오버 화면에 처음 도달 |
| `ACH_SHOPPER` | 떠돌이 상점 제단에서 상점을 엶 |
| `ACH_FULL_SKILLS` / `ACH_ARSENAL` | 스킬 3개 보유 / 특수 무기 2개 보유 |
| `ACH_EVOLVE_3` / `ACH_ULT_30` | 한 판에서 진화 3회 / 필살기 30회 |
| `ACH_LEVEL_15` | 플레이어 레벨 15 |
| `ACH_KILLS_3000_TOTAL` | 여러 판 누적 처치 수 3000 |
| `ACH_NO_HIT_BOSS` | 스테이지 보스가 나온 뒤 쓰러질 때까지 체력이 한 번도 줄지 않음 |
| `ACH_CLOSE_CALL` | 스테이지 보스를 쓰러뜨린 순간 체력이 최대의 10% 이하 |

## Steamworks 현지화 파일

`docs/steam/achievement-loc/*.vdf` — Steamworks → 도전 과제 현지화에서 업로드한 파일 (영어 · 한국어 · 일본어 · 중국어 간체).
키 `NEW_ACHIEVEMENT_1_<번호>_NAME/DESC`의 번호는 위 표의 순서(0부터)와 같습니다. 도전 과제를 새로 만들면 번호가 바뀌므로 먼저 "All Languages"를 받아서 확인하세요.

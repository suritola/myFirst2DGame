# 스팀 업적 등록표

Steamworks → **앱 관리 → 스탯 및 업적 → 업적**에서 아래 **API 이름**을 그대로 써서 10개를 만듭니다.
API 이름이 다르면 게임에서 풀리지 않습니다. (코드: `Assets/Scripts/SteamManager.cs`의 `SteamAchievements`)

- 아이콘: `docs/steam/art/achievements/` — 달성 `<API>.jpg`, 미달성 `<API>_locked.jpg` (256×256)
- 모두 **숨김 아님**으로 두면 됩니다. (`ACH_CLEAR`만 숨김으로 해도 좋음)
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

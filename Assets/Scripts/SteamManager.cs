#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX) && !DISABLESTEAMWORKS
#define STEAM
#endif

using UnityEngine;
using UnityEngine.SceneManagement;
#if STEAM
using Steamworks;
#endif

// 스팀 연동 (Steamworks.NET): 초기화 · 콜백 · 업적
// 스팀용 빌드에서만 켜짐. GitHub 빌드는 BuildScript가 DISABLESTEAMWORKS를 넣어서 스팀 없이 실행됨
// 업적은 게임 이벤트를 듣고 있다가 조건을 채우면 SteamAchievements.Unlock으로 풂
public class SteamManager : MonoBehaviour
{
    // Steamworks 파트너 사이트의 App ID (tools/steam/steam-config.json 의 appId 와 같아야 함)
    public const uint AppId = 5328770;

    public static SteamManager Instance { get; private set; }
    public static bool Initialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("SteamManager");
        DontDestroyOnLoad(go);
        go.AddComponent<SteamManager>();
    }

    void Awake()
    {
        Instance = this;
#if STEAM
        if (!Packsize.Test() || !DllCheck.Test())
        {
            Debug.LogWarning("[Steam] Steamworks.NET 설정이 잘못되었습니다 (Packsize / DllCheck)");
            return;
        }
        // 스팀 밖에서 실행하면 스팀을 통해 다시 실행 (에디터 · App ID 미설정 때는 건너뜀)
        if (!Application.isEditor && AppId != 0 && SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
        {
            Application.Quit();
            return;
        }
        try { Initialized = SteamAPI.Init(); }
        catch (System.DllNotFoundException e) { Debug.LogWarning("[Steam] steam_api 라이브러리를 찾지 못했습니다: " + e.Message); }
        if (!Initialized) Debug.LogWarning("[Steam] 초기화 실패 - 스팀이 켜져 있는지, steam_appid.txt가 있는지 확인하세요");
#endif
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnermyController.Killed += OnEnemyKilled;
        SpecialAbilities.Evolved += _ => SteamAchievements.Unlock(SteamAchievements.Evolve);
        PlayerController.UltUsed += () => SteamAchievements.Unlock(SteamAchievements.FirstUlt);
    }

    // ================================================================= 업적 조건
    int runKills;
    EnemySpawner spawner;
    PlayerController player;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;
        runKills = 0;
        player = FindFirstObjectByType<PlayerController>();
        spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return;
        spawner.onMidBossDefeated += () => SteamAchievements.Unlock(SteamAchievements.MidBoss);
        spawner.onBossDefeated += stage => SteamAchievements.Unlock(
            stage == 0 ? SteamAchievements.BossLich : stage == 1 ? SteamAchievements.BossDemon : SteamAchievements.Clear);
    }

    void OnEnemyKilled(Vector3 at)
    {
        runKills++;
        if (runKills >= 500) SteamAchievements.Unlock(SteamAchievements.Kills500);
    }

    float nextPoll;

    void Update()
    {
#if STEAM
        if (Initialized) SteamAPI.RunCallbacks();
#endif
        if (Time.unscaledTime < nextPoll) return;
        nextPoll = Time.unscaledTime + 0.5f;
        if (player != null && player.level >= 10) SteamAchievements.Unlock(SteamAchievements.Level10);
        if (spawner != null && spawner.stageIndex >= 1) SteamAchievements.Unlock(SteamAchievements.EnterHell);
        if (spawner != null && spawner.stageIndex >= 2) SteamAchievements.Unlock(SteamAchievements.EnterMeadow);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EnermyController.Killed -= OnEnemyKilled;
#if STEAM
        if (Instance == this && Initialized) SteamAPI.Shutdown();
#endif
        if (Instance == this) { Instance = null; Initialized = false; }
    }
}

// 업적 API 이름 (Steamworks 파트너 사이트에 같은 이름으로 등록해야 함 · docs/steam/achievements.md)
public static class SteamAchievements
{
    public const string FirstUlt = "ACH_FIRST_ULT";
    public const string EnterHell = "ACH_ENTER_HELL";
    public const string EnterMeadow = "ACH_ENTER_MEADOW";
    public const string MidBoss = "ACH_MIDBOSS";
    public const string Evolve = "ACH_EVOLVE";
    public const string Level10 = "ACH_LEVEL_10";
    public const string Kills500 = "ACH_KILLS_500";
    public const string BossLich = "ACH_BOSS_LICH";
    public const string BossDemon = "ACH_BOSS_DEMON";
    public const string Clear = "ACH_CLEAR";

    static readonly System.Collections.Generic.HashSet<string> done = new System.Collections.Generic.HashSet<string>();

    public static void Unlock(string id)
    {
        if (!done.Add(id)) return;       // 한 번 실행 중에 같은 업적은 한 번만
        Debug.Log("[Steam] 업적: " + id);
#if STEAM
        if (!SteamManager.Initialized) return;
        if (SteamUserStats.GetAchievement(id, out bool already) && already) return;
        SteamUserStats.SetAchievement(id);
        SteamUserStats.StoreStats();
#endif
    }
}

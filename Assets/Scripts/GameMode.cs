using UnityEngine;

// 난이도: 쉬움 → 보통 → 어려움 → 무한
public enum Difficulty { Easy = 0, Normal = 1, Hard = 2, Endless = 3 }

// 고른 난이도 · 잠금 해제 · 난이도별 수치 배율 (PlayerPrefs에 저장)
// 쉬움 = 원래 게임. 한 단계 오를 때마다 "이전 난이도를 세 판쯤 해 본 사람"에게 맞게 조금씩 어려워짐
// 무한 모드는 어려움 수치에서 시작해 시간이 지날수록 천천히 강해짐 (상한 있음)
public static class GameMode
{
    const string CurrentKey = "mode.difficulty";
    const string UnlockKey = "mode.unlocked";       // 0 = 쉬움만, 1 = 보통 · 어려움, 2 = 무한까지
    const string ClearedKey = "mode.cleared.";      // + 난이도 번호 → 1이면 클리어한 적 있음

    public static readonly string[] Names = { "쉬움", "보통", "어려움", "무한" };

    // 난이도별 배율                                쉬움   보통   어려움  무한(시작)
    static readonly float[] EnemyHp =            { 1f,   1.35f, 1.75f,  1.35f };
    static readonly float[] Damage =             { 1f,   1.2f,  1.45f,  1.2f };
    static readonly float[] EnemySpeed =         { 1f,   1.05f, 1.1f,   1.05f };
    static readonly float[] SkillCooldown =      { 1f,   0.85f, 0.72f,  0.85f };   // 작을수록 스킬을 자주 씀
    static readonly float[] SpawnInterval =      { 1f,   0.9f,  0.8f,   0.8f };
    static readonly int[] ExtraAlive =           { 0,    2,     4,      2 };
    static readonly float[] BossHp =             { 1f,   1.2f,  1.4f,   1.4f };    // 보스는 1~3분 안에 잡히게 (대신 보스 공격이 셈)
    static readonly float[] Gauge =              { 1f,   0.9f,  0.8f,   0.8f };    // 필살기 게이지 차는 속도
    static readonly float[] Reward =             { 1f,   1.15f, 1.3f,   1.3f };    // 경험치 · 코인 (단단해진 만큼 조금 보상)
    // 템포: 보스까지 필요한 처치 수 · 적 생성 간격 (클리어 목표 쉬움 20분 · 보통 25분 · 어려움 30분 내외)
    // 1.8.9: 쉬움 8분 · 어려움 13분에 끝나던 판을 늘림 (처치 수 약 2.2배). 한 판의 경험치 · 코인 · 상점 횟수 총량은 그대로
    static readonly float[] Kills =              { 1.6f, 1.8f,  2.0f,   1f };
    // 보상 기준 (1.8.8까지의 처치 수 배율). 보스 · 중간 보스처럼 한 판에 나오는 수가 정해진 보상은 이 기준으로
    static readonly float[] RewardBasis =        { 0.7f, 0.8f,  0.9f,   1f };
    // 일반 적 영혼 조각을 이만큼만 (판이 길어져도 클리어 전에 영혼 트리가 다 차지 않게)
    const float KillShardRate = 0.75f;
    static readonly float[] Tempo =              { 0.8f, 0.8f,  0.85f,  1f };     // 생성 간격 배율 (작을수록 빨리 나옴)

    // 무한 모드 (1.8.7 조정): 초반이 너무 어렵고 후반이 지루하던 곡선을 바꿈
    //   처음 3분은 0.7배에서 1배로 워밍업, 그 뒤로는 멈추지 않고 가속 (10분 ≈ 2배, 20분 ≈ 3.6배, 30분 ≈ 6배)
    const float EndlessWarmup = 180f;
    const float EndlessStartMul = 0.7f;
    const float EndlessLinear = 0.06f;        // 1분마다
    const float EndlessQuad = 0.0035f;        // 1분² 마다 (갈수록 빨라짐)
    const float EndlessRampMax = 10f;

    static bool loaded;
    static Difficulty current;
    static int unlocked;

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        unlocked = Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 0), 0, 2);
        current = (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(CurrentKey, 0), 0, 3);
        if (!IsUnlocked(current)) current = Difficulty.Easy;
    }

    // 자동 테스트가 난이도를 정할 때 (저장값을 건드리지 않음)
    public static Difficulty? Override;

    public static Difficulty Current
    {
        get
        {
            if (Override.HasValue) return Override.Value;
            // 트레일러 촬영 · 배치 모드(자동 테스트)는 항상 원래 게임(쉬움)
            if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return Difficulty.Easy;
            Load();
            return current;
        }
        set
        {
            Load();
            if (!IsUnlocked(value)) return;
            current = value;
            PlayerPrefs.SetInt(CurrentKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsEndless => Current == Difficulty.Endless;

    public static bool IsUnlocked(Difficulty d)
    {
        Load();
        if (Demo.On) return d == Difficulty.Easy;        // 체험판은 쉬움만
        return d == Difficulty.Easy || (d <= Difficulty.Hard && unlocked >= 1) || (d == Difficulty.Endless && unlocked >= 2);
    }

    public static bool HasCleared(Difficulty d) => PlayerPrefs.GetInt(ClearedKey + (int)d, 0) == 1;

    // 3장 보스를 쓰러뜨렸을 때. 새로 열린 난이도가 있으면 그 이름 (없으면 null)
    public static string OnCleared(Difficulty d)
    {
        Load();
        if (GameInput.Auto || (Application.isBatchMode && !Override.HasValue)) return null;
        PlayerPrefs.SetInt(ClearedKey + (int)d, 1);
        string opened = null;
        if (d == Difficulty.Easy && unlocked < 1) { unlocked = 1; opened = "보통 · 어려움"; }
        if (d == Difficulty.Hard && unlocked < 2) { unlocked = 2; opened = "무한"; }
        PlayerPrefs.SetInt(UnlockKey, unlocked);
        PlayerPrefs.Save();
        return opened;
    }

    // ================================================================= 무한 모드 경과
    static float endlessStart = -1f;
    public static void StartEndlessClock() => endlessStart = Time.time;
    // 무한 모드 판이 진행 중일 때만 (메뉴에서는 0)
    public static float EndlessSeconds => IsEndless && endlessStart >= 0f && EndlessMode.Instance != null ? Time.time - endlessStart : 0f;
    static float Ramp
    {
        get
        {
            if (!IsEndless) return 1f;
            float s = EndlessSeconds, m = s / 60f;
            float warm = Mathf.Lerp(EndlessStartMul, 1f, Mathf.SmoothStep(0f, 1f, s / EndlessWarmup));
            return Mathf.Min(EndlessRampMax, warm * (1f + EndlessLinear * m + EndlessQuad * m * m));
        }
    }
    // 무한 모드 강도 (1 = 기본). 보스 간격 · 중간 보스 주기 등에 씀
    public static float EndlessIntensity => Ramp;

    // ================================================================= 배율 (지금 난이도 기준)
    // 1.8.7: 모든 난이도를 30% 어렵게 (적 · 보스 체력, 적이 주는 피해). 생성 속도 · 이동 속도는 그대로
    const float Harder = 1.3f;

    static int I => (int)Current;
    public static float EnemyHpMul => EnemyHp[I] * Ramp * Harder;
    public static float DamageMul => Damage[I] * Mathf.Lerp(1f, Ramp, 0.5f) * Harder;
    public static float EnemySpeedMul => EnemySpeed[I];
    public static float SkillCooldownMul => SkillCooldown[I] / Mathf.Lerp(1f, Ramp, 0.3f);
    public static float SpawnIntervalMul => SpawnInterval[I] * Tempo[I] / Mathf.Lerp(1f, Ramp, 0.4f);
    // 보스 · 페이즈까지 필요한 처치 수 배율
    public static float KillsMul => Kills[I];
    public static int ScaleKills(int kills) => kills >= int.MaxValue / 2 ? kills : Mathf.Max(1, Mathf.RoundToInt(kills * Kills[I]));
    // 무한 모드는 강도가 오를수록 한 번에 나오는 적도 늘어남
    public static int ExtraAliveCount => ExtraAlive[I] + (IsEndless ? Mathf.FloorToInt(Mathf.Max(0f, Ramp - 1f) * 4f) : 0);
    public static float BossHpMul => BossHp[I] * Ramp * Harder;
    public static float GaugeMul => Gauge[I];
    // 일반 적 한 마리 보상 (처치 수가 늘어난 만큼 줄여 한 판 총량을 맞춤)
    public static float RewardMul => Reward[I] / Kills[I];
    // 보스 · 중간 보스 보상 (처치 수와 상관없이 나오는 수가 정해져 있음)
    public static float FixedRewardMul => Reward[I] / RewardBasis[I];
    // 처치 수가 예전보다 몇 배인지 (떠돌이 상점 간격 등 처치 수로 세는 것을 맞출 때)
    public static float KillStretch => Kills[I] / RewardBasis[I];
    // 일반 적 영혼 조각 배율 (예전 한 마리 몫 기준, 무한 모드는 그대로)
    public static float KillShardMul(bool countsTowardBoss) => IsEndless ? 1f : KillShardRate / (countsTowardBoss ? KillStretch : 1f);

    // 소수 보상을 확률로 반올림 (2.4 → 40% 확률로 3, 아니면 2): 여러 번 모으면 평균이 맞음
    public static int RoundRandom(float v)
    {
        int n = Mathf.FloorToInt(v);
        return n + (Random.value < v - n ? 1 : 0);
    }

    public static string Name(Difficulty d) => Loc.T(Names[(int)d]);
}

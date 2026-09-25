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
    static readonly float[] EnemyHp =            { 1f,   1.35f, 1.75f,  1.75f };
    static readonly float[] Damage =             { 1f,   1.2f,  1.45f,  1.45f };
    static readonly float[] EnemySpeed =         { 1f,   1.05f, 1.1f,   1.1f };
    static readonly float[] SkillCooldown =      { 1f,   0.85f, 0.72f,  0.72f };   // 작을수록 스킬을 자주 씀
    static readonly float[] SpawnInterval =      { 1f,   0.9f,  0.8f,   0.8f };
    static readonly int[] ExtraAlive =           { 0,    2,     4,      4 };
    static readonly float[] BossHp =             { 1f,   1.35f, 1.8f,   1.6f };
    static readonly float[] Gauge =              { 1f,   0.9f,  0.8f,   0.8f };    // 필살기 게이지 차는 속도
    static readonly float[] Reward =             { 1f,   1.15f, 1.3f,   1.3f };    // 경험치 · 코인 (단단해진 만큼 조금 보상)

    // 무한 모드: 1분마다 +6%, 최대 +90% (약 15분)
    const float EndlessRampPerMinute = 0.06f;
    const float EndlessRampMax = 0.9f;

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
            if (GameInput.Auto || TrailerDirector.Running || Application.isBatchMode) return Difficulty.Easy;
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
    static float Ramp => IsEndless ? 1f + Mathf.Min(EndlessRampMax, EndlessSeconds / 60f * EndlessRampPerMinute) : 1f;

    // ================================================================= 배율 (지금 난이도 기준)
    static int I => (int)Current;
    public static float EnemyHpMul => EnemyHp[I] * Ramp;
    public static float DamageMul => Damage[I] * Mathf.Lerp(1f, Ramp, 0.5f);
    public static float EnemySpeedMul => EnemySpeed[I];
    public static float SkillCooldownMul => SkillCooldown[I] / Mathf.Lerp(1f, Ramp, 0.3f);
    public static float SpawnIntervalMul => SpawnInterval[I] / Mathf.Lerp(1f, Ramp, 0.4f);
    public static int ExtraAliveCount => ExtraAlive[I];
    public static float BossHpMul => BossHp[I] * Ramp;
    public static float GaugeMul => Gauge[I];
    public static float RewardMul => Reward[I];

    public static string Name(Difficulty d) => Loc.T(Names[(int)d]);
}

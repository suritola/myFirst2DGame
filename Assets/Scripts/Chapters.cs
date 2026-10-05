using UnityEngine;

// 장 순서 (2.1.1~): 초원 → 지하 묘역 → 불타는 지옥 → 영혼의 심연
// 스테이지 번호(맵 · 적 구성 · 음악 · 도감)는 그대로 두고 "몇 번째 장인지"만 바꿈 (StageManager · EnemySpawner)
// 원래 3장이던 초원이 1장으로 오면 너무 세므로, 적 체력 · 주는 피해 · 보상 · 보스 체력을 그 자리(몇 번째 장)의 원래 세기에 맞춤
// 체험판(1장 리치 왕에서 끝) · 무한 모드는 예전 순서 그대로
public static class Chapters
{
    // 스테이지 번호: 0 지하 묘역 · 1 불타는 지옥 · 2 초원 · 3 영혼의 심연
    static readonly int[] Normal = { 2, 0, 1, 3 };
    static readonly int[] Classic = { 0, 1, 2, 3 };
    public static bool IsClassic => Demo.On || GameMode.IsEndless || GameInput.TrailerRunning || Application.isBatchMode;     // 촬영 · 화면 점검 도구는 장을 직접 건너뜀
    static int[] Order => IsClassic ? Classic : Normal;

    public static readonly string[] StageNames = { "지하 묘역", "불타는 지옥", "초원", "영혼의 심연" };

    public static int First => Order[0];

    // 이 스테이지가 몇 번째 장인지 (0부터) · 다음 장의 스테이지 (마지막이면 -1)
    public static int SlotOf(int stage)
    {
        int[] o = Order;
        for (int i = 0; i < o.Length; i++) if (o[i] == stage) return i;
        return stage;
    }
    public static int Next(int stage)
    {
        int s = SlotOf(stage) + 1;
        return s < Order.Length ? Order[s] : -1;
    }
    public static int Number(int stage) => SlotOf(stage) + 1;

    // 「2장 · 지하 묘역」
    public static string Title(int stage) =>
        Loc.T("{0}장").Replace("{0}", Number(stage).ToString()) + " · " + Loc.T(StageNames[Mathf.Clamp(stage, 0, StageNames.Length - 1)]);

    // ---------------------------------------------------------------- 세기 맞추기
    // 장마다 원래 세기 (스테이지 번호 순서): 잡몹 평균 체력 · 적이 주는 피해 · 처치 보상 · 보스 체력(킹 슬라임은 분열 합계) · 보스까지 처치 수
    static readonly float[] Hp = { 9f, 51f, 53f, 63f };
    static readonly float[] Hurt = { 8.3f, 16.6f, 17.2f, 19.6f };
    static readonly float[] Reward = { 32f, 51f, 73f, 85f };
    static readonly float[] Boss = { 480f, 2400f, 5950f, 4200f };
    static readonly int[] Kills = { 60, 80, 80, 85 };

    // 지금 자리의 세기 ÷ 원래 자리의 세기 (예전 순서면 1)
    static float Ratio(float[] t, int stage)
    {
        if (IsClassic || stage < 0 || stage >= t.Length) return 1f;
        return t[Mathf.Clamp(SlotOf(stage), 0, t.Length - 1)] / t[stage];
    }

    public static float EnemyHpMul(int stage) => Ratio(Hp, stage);
    public static float RewardMul(int stage) => Ratio(Reward, stage);
    public static float BossHpMul(int stage) => Ratio(Boss, stage);
    // 플레이어가 받는 모든 피해 (몸통 · 적 스킬 · 보스 스킬 · 결계): 지금 장 기준
    public static float HurtMul => StageManager.Instance != null ? Ratio(Hurt, StageManager.Instance.CurrentStage) : 1f;
    public static float HurtMulOf(int stage) => Ratio(Hurt, stage);
    public static int BossKills(int stage, int original) => IsClassic || stage < 0 || stage >= Kills.Length ? original : Kills[Mathf.Clamp(SlotOf(stage), 0, Kills.Length - 1)];
    public static int CurrentStage => StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
}

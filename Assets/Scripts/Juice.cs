using UnityEngine;

// 손맛 연출: 적 처치 · 레벨업 · 코인 획득 때 도트 이펙트와 소리 (tools/pixelart/make_fx.py 로 만든 시트)
public static class Juice
{
    static float lastPop;

    // 스테이지마다 영혼 색이 다름 (1장 청록 · 2장 불꽃 · 3장 풀빛 · 무한 금빛)
    static Color StageColor()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
        return stage == 0 ? new Color(0.6f, 0.95f, 1f) : stage == 1 ? new Color(1f, 0.58f, 0.28f)
             : stage == 2 ? new Color(0.65f, 1f, 0.5f) : new Color(1f, 0.82f, 0.42f);
    }

    public static void EnemyDied(Vector3 pos, float size)
    {
        Color c = StageColor();
        Fx.Play("fx_deathburst", pos, 2.4f * size, c, 22f);
        if (size > 1.2f) Fx.Play("fx_shock", pos, 3.5f * size, new Color(c.r, c.g, c.b, 0.7f), 18f);
        // 한꺼번에 많이 죽어도 소리가 뭉개지지 않게
        if (Time.time - lastPop > 0.045f)
        {
            lastPop = Time.time;
            Hostile.Play("pop", 0.35f, Random.Range(0.85f, 1.2f));
        }
    }

    public static void LevelUp(Vector3 pos)
    {
        Fx.Play("fx_levelup", pos + Vector3.up * 1.2f, 4.5f, Color.white, 14f, 0f, 30);
        Fx.Play("fx_sparkle", pos + new Vector3(-0.8f, 1.8f, 0f), 0.9f, new Color(1f, 0.9f, 0.5f), 12f, 0f, 31);
        Fx.Play("fx_sparkle", pos + new Vector3(0.9f, 2.4f, 0f), 0.7f, new Color(1f, 0.9f, 0.5f), 10f, 0f, 31);
        Hostile.Play("levelup", 0.8f);
    }

    public static void CoinPicked(Vector3 pos)
    {
        Fx.Play("fx_sparkle", pos + (Vector3)(Random.insideUnitCircle * 0.3f), 0.8f, new Color(1f, 0.88f, 0.4f), 16f, 0f, 20);
        Hostile.Play("sparkle", 0.3f, Random.Range(0.95f, 1.15f));
    }
}

using UnityEngine;

// 적 머리 위에 표시되는 작은 체력바
// EnermyController가 Start에서 Attach로 붙인다 · 분열한 킹 슬라임도 AttachBoss로 슬라임마다 따로
public class EnemyHealthBar : MonoBehaviour
{
    static Sprite pixel;

    const float Width = 2.2f;
    const float Height = 0.26f;
    const float Border = 0.07f;
    const float HeadMargin = 0.25f;

    static readonly Color FullColor = new Color(0.45f, 0.85f, 0.35f);
    static readonly Color HalfColor = new Color(0.98f, 0.72f, 0.22f);
    static readonly Color LowColor = new Color(0.86f, 0.17f, 0.24f);

    EnermyController enemy;
    System.Func<float> ratioOf;     // 적이 아닌 것(보스 슬라임)의 체력 비율
    float width = Width;
    Transform fill;
    SpriteRenderer fillRenderer;
    SpriteRenderer backRenderer;
    float shownRatio = -1f;

    // 분열한 킹 슬라임: 슬라임마다 머리 위 체력바 (위쪽 보스 체력바는 남은 체력 합계)
    public static EnemyHealthBar AttachBoss(bosss boss, SpriteRenderer body)
    {
        // 분열하며 복제된 이전 체력바는 지움 (복제본은 따라갈 대상이 없어 멈춘 채 남음)
        foreach (EnemyHealthBar old in boss.GetComponentsInChildren<EnemyHealthBar>(true)) Destroy(old.gameObject);
        GameObject root = new GameObject("HealthBar");
        root.transform.SetParent(boss.transform, false);
        Vector3 s = boss.transform.lossyScale;
        root.transform.localScale = new Vector3(1f / Mathf.Abs(s.x), 1f / Mathf.Abs(s.y), 1f);
        root.transform.localPosition = new Vector3(0f, HeadTop(body) + HeadMargin / Mathf.Abs(s.y), 0f);
        EnemyHealthBar bar = root.AddComponent<EnemyHealthBar>();
        bar.ratioOf = () => boss == null || boss.IsDead || boss.setEnemyHP <= 0 ? 0f : Mathf.Clamp01(boss.EnemyHealth / boss.setEnemyHP);
        bar.width = 3.4f;
        bar.Build();
        return bar;
    }

    public static EnemyHealthBar Attach(EnermyController enemy, SpriteRenderer body)
    {
        GameObject root = new GameObject("HealthBar");
        root.transform.SetParent(enemy.transform, false);

        // 부모 스케일과 상관없이 같은 크기로 보이게 보정
        Vector3 s = enemy.transform.lossyScale;
        root.transform.localScale = new Vector3(1f / Mathf.Abs(s.x), 1f / Mathf.Abs(s.y), 1f);
        root.transform.localPosition = new Vector3(0f, HeadTop(body) + HeadMargin / Mathf.Abs(s.y), 0f);

        EnemyHealthBar bar = root.AddComponent<EnemyHealthBar>();
        bar.enemy = enemy;
        bar.Build();
        return bar;
    }

    // 스프라이트에서 실제로 그려지는 부분의 가장 윗점 (로컬 좌표)
    static float HeadTop(SpriteRenderer body)
    {
        if (body == null || body.sprite == null) return 1f;

        float top = float.MinValue;
        foreach (Vector2 v in body.sprite.vertices) top = Mathf.Max(top, v.y);
        return top;
    }

    static Sprite Pixel()
    {
        if (pixel == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            // 피벗을 왼쪽 가운데에 두어 가로 스케일로 줄어들게 함
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        }
        return pixel;
    }

    SpriteRenderer MakePart(string name, Color color, int order, float x, float width, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(x, 0f, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Pixel();
        sr.color = color;
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = order;
        return sr;
    }

    void Build()
    {
        backRenderer = MakePart("Back", new Color(0.05f, 0.04f, 0.06f, 0.9f), 0,
            -width / 2f - Border, width + Border * 2f, Height + Border * 2f);
        fillRenderer = MakePart("Fill", FullColor, 1, -width / 2f, width, Height);
        fill = fillRenderer.transform;
    }

    void LateUpdate()
    {
        float ratio;
        if (ratioOf != null) ratio = ratioOf();
        else if (enemy == null) return;
        else ratio = enemy.setEnemyHP > 0 ? Mathf.Clamp01((float)enemy.EnemyHealth / enemy.setEnemyHP) : 0f;
        // 체력이 그대로면 다시 그리지 않음 (적마다 매 프레임 하던 크기 · 색 갱신 생략)
        if (ratio == shownRatio) return;
        shownRatio = ratio;

        // 죽으면 숨김
        bool alive = ratio > 0f;
        backRenderer.enabled = alive;
        fillRenderer.enabled = alive;
        if (!alive) return;

        fill.localScale = new Vector3(width * ratio, Height, 1f);
        fillRenderer.color = ratio > 0.5f
            ? Color.Lerp(HalfColor, FullColor, (ratio - 0.5f) * 2f)
            : Color.Lerp(LowColor, HalfColor, ratio * 2f);
    }
}

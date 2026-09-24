using UnityEngine;

// 적 머리 위에 표시되는 작은 체력바
// EnermyController가 Start에서 Attach로 붙인다
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
    Transform fill;
    SpriteRenderer fillRenderer;
    SpriteRenderer backRenderer;

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
            -Width / 2f - Border, Width + Border * 2f, Height + Border * 2f);
        fillRenderer = MakePart("Fill", FullColor, 1, -Width / 2f, Width, Height);
        fill = fillRenderer.transform;
    }

    void LateUpdate()
    {
        if (enemy == null) return;

        float ratio = enemy.setEnemyHP > 0 ? Mathf.Clamp01((float)enemy.EnemyHealth / enemy.setEnemyHP) : 0f;

        // 죽으면 숨김
        bool alive = ratio > 0f;
        backRenderer.enabled = alive;
        fillRenderer.enabled = alive;
        if (!alive) return;

        fill.localScale = new Vector3(Width * ratio, Height, 1f);
        fillRenderer.color = ratio > 0.5f
            ? Color.Lerp(HalfColor, FullColor, (ratio - 0.5f) * 2f)
            : Color.Lerp(LowColor, HalfColor, ratio * 2f);
    }
}

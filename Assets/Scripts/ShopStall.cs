using TMPro;
using UnityEngine;

// 떠돌이 상점 제단: 일반 몹을 10마리 잡을 때마다 플레이어 근처에 나타나고,
// 가까이 가면 상점이 열림. 10초가 지나면 사라짐 (Shop이 만듦)
public class ShopStall : MonoBehaviour
{
    public float lifetime = 10f;
    public float openDistance = 2.6f;

    Shop shop;
    float age;
    SpriteRenderer sr;
    LineRenderer timerRing;
    TextMeshPro label;
    GameObject glow;

    static Sprite stallSprite;

    public static ShopStall Spawn(Shop shop, Vector3 pos)
    {
        GameObject go = new GameObject("ShopStall");
        go.transform.position = pos;
        ShopStall s = go.AddComponent<ShopStall>();
        s.shop = shop;
        return s;
    }

    void Start()
    {
        if (SpecialAbilities.GlowSprite != null)
        {
            glow = SpecialAbilities.MakeSprite("StallGlow", SpecialAbilities.GlowSprite, transform.position, 0.45f, new Color(1f, 0.8f, 0.35f, 0.4f), "Effect", 0);
            glow.transform.SetParent(transform, true);
        }
        GameObject body = SpecialAbilities.MakeSprite("StallBody", StallSprite, transform.position, 1f, Color.white, "Character", 0);
        body.transform.SetParent(transform, true);
        sr = body.GetComponent<SpriteRenderer>();

        timerRing = Hostile.NewLine("StallTimer", new Color(1f, 0.85f, 0.4f, 0.9f), 0.14f, 3);
        timerRing.transform.SetParent(transform, false);

        GameObject t = new GameObject("StallLabel", typeof(TextMeshPro));
        t.transform.SetParent(transform, false);
        t.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        label = t.GetComponent<TextMeshPro>();
        if (shop.priceTextInput != null)
        {
            label.font = shop.priceTextInput.font;
            label.fontSharedMaterial = shop.priceTextInput.fontSharedMaterial;
        }
        label.fontSize = 6f;
        label.alignment = TextAlignmentOptions.Center;
        label.sortingLayerID = SortingLayer.NameToID("Effect");
        label.sortingOrder = 30;
        label.rectTransform.sizeDelta = new Vector2(10f, 2f);

        ShockRing.Spawn(transform.position, 0.3f, 3f, 0.4f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.25f);
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Play("chime", 0.8f, 1.2f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float left = lifetime - age;

        // 남은 시간 고리 (줄어듦) + 둥실거림
        Hostile.SetArc(timerRing, transform.position, 1.6f, 90f, 90f + 360f * Mathf.Clamp01(left / lifetime));
        if (sr != null)
        {
            sr.transform.position = transform.position + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.12f;
            // 마지막 3초는 깜빡임
            sr.color = left < 3f && Mathf.PingPong(Time.time * 6f, 1f) < 0.5f ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
        }
        if (glow != null) glow.transform.localScale = Vector3.one * (0.45f + Mathf.Sin(Time.time * 4f) * 0.05f);
        label.text = "상점  " + Mathf.CeilToInt(left);

        PlayerController p = Hostile.Player;
        if (p != null && Time.timeScale == 1f && !p.IsSkillUsing && Vector2.Distance(p.transform.position, transform.position) < openDistance)
        {
            if (shop.OpenFromStall())
            {
                Destroy(gameObject);
                return;
            }
        }
        if (left <= 0f)
        {
            ShockRing.Spawn(transform.position, 1.5f, 0.2f, 0.3f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.2f);
            Destroy(gameObject);
        }
    }

    // 금화 표지가 달린 작은 상인 제단 (코드로 그린 도트)
    static Sprite StallSprite => stallSprite != null ? stallSprite : (stallSprite = MakeStall());

    static Sprite MakeStall()
    {
        const int W = 32, H = 32;
        string[] rows =
        {
            "................................",
            "...........oooooooooo...........",
            "..........oYYYYYYYYYYo..........",
            ".........oYyyyyyyyyyyYo.........",
            ".........oYy..oooo..yYo.........",
            ".........oYy.oYYYYo.yYo.........",
            ".........oYy.oYooYo.yYo.........",
            ".........oYy.oYYYYo.yYo.........",
            ".........oYy..oooo..yYo.........",
            ".........oYyyyyyyyyyyYo.........",
            "..........oYYYYYYYYYYo..........",
            "...........oooo..oooo...........",
            "..............o..o..............",
            "......oooooooooooooooooooo......",
            ".....oRRrRRrRRrRRrRRrRRrRRo.....",
            "....oRRrRRrRRrRRrRRrRRrRRrRo....",
            "...oooooooooooooooooooooooooo...",
            "...oWWWWWWWWWWWWWWWWWWWWWWWWo...",
            "...oWwwwwwwwwwwwwwwwwwwwwwwWo...",
            "...oWwBBBBwwwwwwwwwwwwBBBBwWo...",
            "...oWwBGGBwwwwYYYYwwwwBGGBwWo...",
            "...oWwBBBBwwwYyyyYwwwwBBBBwWo...",
            "...oWwwwwwwwwwYYYYwwwwwwwwwWo...",
            "...oWWWWWWWWWWWWWWWWWWWWWWWWo...",
            "...oDDDDDDDDDDDDDDDDDDDDDDDDo...",
            "...oDddddddddddddddddddddddDo...",
            "...oDdoooDddddddddddddoooDdDo...",
            "...oDdo.oDddddddddddddo.oDdDo...",
            "...ooo..ooooooooooooooo..ooo....",
            "................................",
            "................................",
            "................................",
        };
        var pal = new System.Collections.Generic.Dictionary<char, Color>
        {
            ['o'] = new Color(0.12f, 0.06f, 0.05f),
            ['Y'] = new Color(1f, 0.82f, 0.3f), ['y'] = new Color(0.85f, 0.6f, 0.15f),
            ['R'] = new Color(0.75f, 0.15f, 0.15f), ['r'] = new Color(0.95f, 0.9f, 0.8f),
            ['W'] = new Color(0.55f, 0.35f, 0.2f), ['w'] = new Color(0.42f, 0.26f, 0.15f),
            ['B'] = new Color(0.25f, 0.15f, 0.1f), ['G'] = new Color(0.4f, 0.9f, 0.5f),
            ['D'] = new Color(0.35f, 0.22f, 0.13f), ['d'] = new Color(0.28f, 0.17f, 0.1f),
        };
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                char c = rows[H - 1 - y][x];
                tex.SetPixel(x, y, pal.TryGetValue(c, out Color col) ? col : new Color(0f, 0f, 0f, 0f));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.3f), 10f);
    }
}

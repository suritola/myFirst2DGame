using TMPro;
using UnityEngine;

// 중간 보스 표시: 머리 위 이름표 + 발밑에서 맥박치는 빛 + 몸을 감싸는 빛나는 테두리
public class MidBossMark : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.78f, 0.3f);

    TextMeshPro tag;
    SpriteRenderer glow, halo;
    LineRenderer ring;
    SpriteRenderer body;
    EnermyController enemy;
    float radius;

    public static void Attach(GameObject go, string name)
    {
        if (go == null) return;
        go.AddComponent<MidBossMark>().Build(name);
    }

    void Build(string name)
    {
        body = GetComponent<SpriteRenderer>();
        enemy = GetComponent<EnermyController>();
        radius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.y) : 1.5f;

        // 이름표 (크기는 월드 기준이라 적 크기와 상관없이 일정)
        GameObject t = new GameObject("MidBossTag", typeof(TextMeshPro));
        tag = t.GetComponent<TextMeshPro>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) tag.font = UIKit.Font;
        if (UIKit.FontMaterial != null) tag.fontSharedMaterial = UIKit.FontMaterial;
        tag.text = "<size=70%>" + Loc.T("중간 보스") + "</size>\n" + (string.IsNullOrEmpty(name) ? "" : name);
        tag.fontSize = 5f;
        tag.color = Gold;
        tag.alignment = TextAlignmentOptions.Center;
        tag.outlineWidth = 0.25f;
        tag.outlineColor = new Color32(40, 10, 0, 255);
        tag.rectTransform.sizeDelta = new Vector2(10f, 3f);
        tag.sortingLayerID = SortingLayer.NameToID("Effect");
        tag.sortingOrder = 40;

        Sprite g = SpecialAbilities.GlowSprite;
        if (g != null)
        {
            // 발밑 빛 (몸 뒤)
            glow = SpecialAbilities.MakeSprite("MidBossGlow", g, transform.position, 1f, new Color(1f, 0.6f, 0.2f, 0.5f), "Character", -2).GetComponent<SpriteRenderer>();
            // 몸 뒤 후광 (몸보다 조금 크게 = 빛나는 테두리)
            halo = SpecialAbilities.MakeSprite("MidBossHalo", g, transform.position, 1f, new Color(1f, 0.85f, 0.4f, 0.7f), "Character", -1).GetComponent<SpriteRenderer>();
        }
        ring = Hostile.NewLine("MidBossRing", Gold, 0.12f, 9);
        ring.loop = true;
    }

    void LateUpdate()
    {
        if (enemy != null && enemy.IsDead) { Cleanup(); return; }
        Vector3 p = transform.position;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        float size = radius * 2f;

        if (tag != null) tag.transform.position = p + Vector3.up * (radius + 1.1f);
        if (glow != null)
        {
            float s = size * (1.6f + 0.25f * pulse) / Mathf.Max(0.01f, glow.sprite.bounds.size.x);
            glow.transform.position = p - Vector3.up * radius * 0.6f;
            glow.transform.localScale = new Vector3(s, s * 0.45f, 1f);
            glow.color = new Color(1f, 0.55f, 0.15f, 0.35f + 0.3f * pulse);
        }
        if (halo != null)
        {
            float s = size * (1.25f + 0.1f * pulse) / Mathf.Max(0.01f, halo.sprite.bounds.size.x);
            halo.transform.position = p;
            halo.transform.localScale = Vector3.one * s;
            halo.color = new Color(1f, 0.85f, 0.35f, 0.35f + 0.35f * pulse);
        }
        if (ring != null)
        {
            ring.enabled = true;
            Hostile.SetArc(ring, p, radius * (1.15f + 0.08f * pulse), Time.time * 90f, Time.time * 90f + 340f);
            ring.startColor = ring.endColor = new Color(Gold.r, Gold.g, Gold.b, 0.5f + 0.4f * pulse);
        }
    }

    void OnDestroy() => Cleanup();

    void Cleanup()
    {
        if (tag != null) Destroy(tag.gameObject);
        if (glow != null) Destroy(glow.gameObject);
        if (halo != null) Destroy(halo.gameObject);
        if (ring != null) Destroy(ring.gameObject);
        tag = null; glow = null; halo = null; ring = null;
    }
}

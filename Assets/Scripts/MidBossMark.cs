using TMPro;
using UnityEngine;

// 중간 보스 표시: 머리 위 이름표 (체력바는 모든 적에게 붙는 EnemyHealthBar 그대로, 이름표는 그 바로 위)
// 1.8.7~: 전투 이펙트에 묻히지 않게 크게 · 어두운 판과 금빛 테두리 · 살짝 숨 쉬듯 커졌다 작아짐 · 이펙트보다 위(TargetMark 레이어)
public class MidBossMark : MonoBehaviour
{
    TextMeshPro tag;
    SpriteRenderer body, plate, border;
    EnermyController enemy;
    Transform root;

    static Sprite white;

    public static void Attach(GameObject go, string name)
    {
        if (go == null) return;
        go.AddComponent<MidBossMark>().Build(name);
    }

    void Build(string name)
    {
        body = GetComponent<SpriteRenderer>();
        enemy = GetComponent<EnermyController>();
        if (white == null) white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 4f);

        root = new GameObject("MidBossTag").transform;
        int layer = SortingLayer.NameToID("TargetMark");

        GameObject t = new GameObject("Text", typeof(TextMeshPro));
        t.transform.SetParent(root, false);
        tag = t.GetComponent<TextMeshPro>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) tag.font = UIKit.Font;
        if (UIKit.FontMaterial != null) tag.fontSharedMaterial = UIKit.FontMaterial;
        tag.text = Loc.T("중간 보스") + (string.IsNullOrEmpty(name) ? "" : " · " + name);
        tag.fontSize = 3.6f;
        tag.fontStyle = FontStyles.Bold;
        tag.color = new Color(1f, 0.86f, 0.35f);
        tag.alignment = TextAlignmentOptions.Center;
        tag.enableWordWrapping = false;
        tag.outlineWidth = 0.3f;
        tag.outlineColor = new Color32(0, 0, 0, 255);
        tag.sortingLayerID = layer;
        tag.sortingOrder = 52;

        // 글자 크기에 맞춘 판 (금빛 테두리 → 어두운 붉은 판 → 글자)
        Vector2 size = tag.GetPreferredValues(tag.text);
        tag.rectTransform.sizeDelta = size;
        border = Plate("Border", size + new Vector2(0.55f, 0.34f), new Color(1f, 0.78f, 0.3f, 0.95f), layer, 50);
        plate = Plate("Plate", size + new Vector2(0.4f, 0.2f), new Color(0.22f, 0.03f, 0.05f, 0.88f), layer, 51);
    }

    SpriteRenderer Plate(string name, Vector2 size, Color color, int layer, int order)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(root, false);
        sr.sprite = white;                                   // 1 × 1 칸짜리 흰 판을 글자 크기로 늘림
        sr.transform.localScale = new Vector3(size.x, size.y, 1f);
        sr.color = color;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;
        return sr;
    }

    void LateUpdate()
    {
        if (enemy != null && enemy.IsDead) { Cleanup(); return; }
        if (root == null) return;
        float top = body != null ? body.bounds.max.y : transform.position.y + 1.5f;
        // 체력바(머리 위) 바로 위
        root.position = new Vector3(transform.position.x, top + 0.95f, 0f);
        root.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.time * 4f));
        if (border != null) border.color = new Color(1f, 0.78f, 0.3f, 0.7f + 0.25f * Mathf.Sin(Time.time * 4f));
    }

    void OnDestroy() => Cleanup();

    void Cleanup()
    {
        if (root != null) Destroy(root.gameObject);
        root = null;
        tag = null;
    }
}

using TMPro;
using UnityEngine;

// 중간 보스 표시: 머리 위 작은 이름표 (체력바는 모든 적에게 붙는 EnemyHealthBar 그대로, 이름표는 그 바로 위)
public class MidBossMark : MonoBehaviour
{
    TextMeshPro tag;
    SpriteRenderer body;
    EnermyController enemy;

    public static void Attach(GameObject go, string name)
    {
        if (go == null) return;
        go.AddComponent<MidBossMark>().Build(name);
    }

    void Build(string name)
    {
        body = GetComponent<SpriteRenderer>();
        enemy = GetComponent<EnermyController>();

        GameObject t = new GameObject("MidBossTag", typeof(TextMeshPro));
        tag = t.GetComponent<TextMeshPro>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) tag.font = UIKit.Font;
        if (UIKit.FontMaterial != null) tag.fontSharedMaterial = UIKit.FontMaterial;
        tag.text = Loc.T("중간 보스") + (string.IsNullOrEmpty(name) ? "" : " · " + name);
        tag.fontSize = 2.6f;
        tag.color = new Color(1f, 0.8f, 0.4f);
        tag.alignment = TextAlignmentOptions.Center;
        tag.outlineWidth = 0.25f;
        tag.outlineColor = new Color32(0, 0, 0, 255);
        tag.rectTransform.sizeDelta = new Vector2(8f, 1f);
        tag.sortingLayerID = SortingLayer.NameToID("Effect");
        tag.sortingOrder = 40;
    }

    void LateUpdate()
    {
        if (enemy != null && enemy.IsDead) { Cleanup(); return; }
        if (tag == null) return;
        float top = body != null ? body.bounds.max.y : transform.position.y + 1.5f;
        // 체력바(머리 위) 바로 위
        tag.transform.position = new Vector3(transform.position.x, top + 0.75f, 0f);
    }

    void OnDestroy() => Cleanup();

    void Cleanup()
    {
        if (tag != null) Destroy(tag.gameObject);
        tag = null;
    }
}

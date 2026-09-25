using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 적 · 보스가 피해를 입으면 머리 위로 떠올랐다 사라지는 피해 숫자
// 같은 적에게 짧은 사이에 연달아 들어온 피해(도트 · 연사)는 숫자 하나로 합침
public class DamagePopup : MonoBehaviour
{
    const float Life = 0.75f, Merge = 0.15f, Rise = 1.3f;
    const int MaxActive = 80;

    static TMP_FontAsset font;
    static Material material;
    static readonly Dictionary<int, DamagePopup> byTarget = new Dictionary<int, DamagePopup>();
    static int active;

    TextMeshPro text;
    int key;
    float amount, t, lastAdd, size = 1f;
    Vector3 start;

    public static void Show(Transform target, float damage, SpriteRenderer body)
    {
        if (target == null || damage <= 0f) return;
        int k = target.GetInstanceID();
        if (byTarget.TryGetValue(k, out DamagePopup p) && p != null && Time.time - p.lastAdd < Merge)
        {
            p.Add(damage);
            return;
        }
        if (active >= MaxActive || !EnsureFont()) return;

        float top = body != null ? body.bounds.max.y : target.position.y + 1f;
        GameObject go = new GameObject("DamagePopup");
        go.transform.position = new Vector3(target.position.x + Random.Range(-0.3f, 0.3f), top + 0.35f, 0f);
        p = go.AddComponent<DamagePopup>();
        p.key = k;
        p.start = go.transform.position;
        p.text = go.AddComponent<TextMeshPro>();
        p.text.font = font;
        if (material != null) p.text.fontSharedMaterial = material;
        p.text.fontSize = 11f;
        p.text.alignment = TextAlignmentOptions.Center;
        p.text.enableWordWrapping = false;
        p.text.rectTransform.sizeDelta = new Vector2(6f, 2f);
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sortingLayerName = "Effect";
        mr.sortingOrder = 40;
        byTarget[k] = p;
        active++;
        p.Add(damage);
    }

    static bool EnsureFont()
    {
        if (font != null) return true;
        font = UIKit.Font;
        if (font == null)
        {
            TMP_Text any = FindFirstObjectByType<TMP_Text>();
            if (any != null) font = any.font;
        }
        if (font == null) font = TMP_Settings.defaultFontAsset;
        if (font == null) return false;
        // 어느 바닥에서도 읽히게 검은 테두리 (모든 숫자가 한 재질을 같이 씀)
        material = new Material(font.material);
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.08f, 0.05f, 0.08f, 1f));
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
        return true;
    }

    void Add(float damage)
    {
        amount += damage;
        lastAdd = Time.time;
        t = 0f;
        text.text = amount < 10f ? amount.ToString("0.#") : Mathf.RoundToInt(amount).ToString();
        // 큰 피해일수록 크고 붉은 금색
        PlayerController pl = Hostile.Player;
        float baseDmg = pl != null ? Mathf.Max(0.1f, pl.damage) : 1f;
        float k = Mathf.Clamp01((amount / baseDmg - 1f) / 4f);
        text.color = Color.Lerp(new Color(1f, 1f, 0.95f), new Color(1f, 0.55f, 0.2f), k);
        size = 1f + 0.5f * k;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / Life;
        if (k >= 1f) { Destroy(gameObject); return; }
        float up = 1f - (1f - k) * (1f - k);
        float pop = t < 0.08f ? 1.35f - 0.35f * (t / 0.08f) : 1f;
        transform.position = start + new Vector3(0f, Rise * up, 0f);
        transform.localScale = Vector3.one * (size * pop);
        Color c = text.color;
        c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        text.color = c;
    }

    void OnDestroy()
    {
        active--;
        if (byTarget.TryGetValue(key, out DamagePopup p) && p == this) byTarget.Remove(key);
    }
}

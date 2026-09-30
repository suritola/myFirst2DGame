using UnityEngine;

// 도트를 더 잘 보이게 (1.8.7~): 캐릭터 · 적 · 보스 스프라이트에 1도트 윤곽선 (Resources/Shaders/SpriteOutline.shader)
// 적 · 보스는 짙은 윤곽선으로 배경과 분리, 플레이어는 밝은 윤곽선으로 적 무리 속에서도 바로 보이게
public static class SpriteOutline
{
    static readonly Color Dark = new Color(0.06f, 0.04f, 0.09f, 0.9f);
    static readonly Color Light = new Color(1f, 0.95f, 0.8f, 0.85f);
    static Material dark, light;
    static bool failed;

    public static void Enemy(GameObject go) => Apply(go, ref dark, Dark);
    public static void Player(GameObject go) => Apply(go, ref light, Light);

    static void Apply(GameObject go, ref Material mat, Color color)
    {
        if (go == null || failed) return;
        if (mat == null)
        {
            Shader s = Resources.Load<Shader>("Shaders/SpriteOutline");
            if (s == null || !s.isSupported) { failed = true; return; }
            mat = new Material(s) { name = "SpriteOutline" };
            mat.SetColor("_OutlineColor", color);
        }
        // 기본 스프라이트 재질을 쓰는 몸 그림에만 (이펙트 · 글자 · 체력바는 그대로)
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Material m = sr.sharedMaterial;
            if (m == null || m.shader == null || m.shader.name != "Sprites/Default") continue;
            if (sr.sortingLayerName == "Effect" || sr.sortingLayerName == "TargetMark") continue;
            sr.sharedMaterial = mat;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

// 도트 이펙트 재생기: Resources/FX 의 스프라이트 시트(이름_0, 이름_1 ...)를 프레임 애니메이션으로 재생
// 예) Fx.Play("fx_explosion", pos, 4f)  → 지름 4칸 크기의 폭발
public static class Fx
{
    static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    public static Sprite[] Frames(string name)
    {
        if (cache.TryGetValue(name, out Sprite[] frames)) return frames;
        frames = Resources.LoadAll<Sprite>("FX/" + name);
        System.Array.Sort(frames, (a, b) => Index(a.name).CompareTo(Index(b.name)));
        cache[name] = frames;
        return frames;
    }

    static int Index(string spriteName)
    {
        int i = spriteName.LastIndexOf('_');
        return i >= 0 && int.TryParse(spriteName.Substring(i + 1), out int n) ? n : 0;
    }

    // size: 스프라이트 높이를 이 월드 크기로 맞춤
    public static FxAnim Play(string name, Vector3 pos, float size, Color? color = null, float fps = 16f,
                              float rotation = 0f, int order = 12, bool loop = false, float life = -1f, string layer = "Effect")
    {
        Sprite[] frames = Frames(name);
        if (frames == null || frames.Length == 0) return null;

        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = color ?? Color.white;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        float h = frames[0].bounds.size.y;
        go.transform.localScale = Vector3.one * (h > 0f ? size / h : 1f);

        FxAnim a = go.AddComponent<FxAnim>();
        a.frames = frames;
        a.fps = fps;
        a.loop = loop;
        a.life = life;
        a.sr = sr;
        return a;
    }

    // 두 점 사이를 잇는 도트 빔 (fx_beam을 길이 방향으로 늘림)
    public static FxAnim Beam(Vector3 from, Vector3 to, float width, Color color, float life, int order = 14)
    {
        Vector3 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        FxAnim a = Play("fx_beam", (from + to) * 0.5f, width, color, 20f, angle, order, true, life);
        if (a != null)
        {
            float unit = a.sr.sprite.bounds.size.x;
            Vector3 s = a.transform.localScale;
            a.transform.localScale = new Vector3(d.magnitude / unit, s.y, 1f);
        }
        return a;
    }

    // 두 점 사이에 번개 (fx_bolt를 세로로 늘려 회전)
    public static FxAnim Bolt(Vector3 from, Vector3 to, float width, Color color, float life = 0.18f)
    {
        Vector3 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
        FxAnim a = Play("fx_bolt", (from + to) * 0.5f, d.magnitude, color, 24f, angle, 22, true, life);
        if (a != null)
        {
            float w = a.sr.sprite.bounds.size.x;
            Vector3 s = a.transform.localScale;
            a.transform.localScale = new Vector3(width / w, s.y, 1f);
        }
        return a;
    }
}

// 프레임 애니메이션: 한 번 재생 후 사라지거나(loop = false), life 동안 반복
public class FxAnim : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 16f;
    public bool loop;
    public float life = -1f;       // 반복일 때 수명 (-1이면 직접 지울 때까지)
    public float spin;             // 초당 회전 (룬 등)
    public Transform follow;       // 따라다닐 대상
    public SpriteRenderer sr;
    float t;
    float age;
    Color baseColor;
    bool started;

    void Update()
    {
        if (!started)
        {
            started = true;
            baseColor = sr.color;
        }
        t += Time.deltaTime * fps;
        age += Time.deltaTime;
        int i = (int)t;
        if (i >= frames.Length)
        {
            if (!loop) { Destroy(gameObject); return; }
            i %= frames.Length;
        }
        sr.sprite = frames[i];
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (follow != null) transform.position = follow.position;

        if (life > 0f)
        {
            // 끝나기 직전 0.15초 동안 흐려짐
            float left = life - age;
            if (left < 0.15f) sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Clamp01(left / 0.15f));
            if (left <= 0f) Destroy(gameObject);
        }
    }
}

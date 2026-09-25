using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 특수 능력의 소리·조준 표시·알림 글자·화면 흔들림
// 소리는 사운드 파일 없이 코드에서 파형을 만들어 씀
public class SpecialFeedback : MonoBehaviour
{
    const int Rate = 44100;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource oneShot;
    AudioSource loop;
    Material lineMaterial;
    Material dashMaterialTemplate;
    TMP_FontAsset font;
    Material fontMaterial;
    Coroutine shake;

    public float volume = 0.45f;

    public void Init(TMP_FontAsset font, Material fontMaterial)
    {
        this.font = font;
        this.fontMaterial = fontMaterial;

        oneShot = gameObject.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;
        loop = gameObject.AddComponent<AudioSource>();
        loop.playOnAwake = false;
        loop.loop = true;

        lineMaterial = new Material(Shader.Find("Sprites/Default"));

        // 점선: 절반은 채우고 절반은 비운 텍스처를 선 길이만큼 반복
        Texture2D dash = new Texture2D(8, 1, TextureFormat.RGBA32, false);
        for (int x = 0; x < 8; x++) dash.SetPixel(x, 0, x < 5 ? Color.white : Color.clear);
        dash.wrapMode = TextureWrapMode.Repeat;
        dash.filterMode = FilterMode.Point;
        dash.Apply();
        dashMaterialTemplate = new Material(lineMaterial) { mainTexture = dash };

        BuildClips();
    }

    // ================================================================= sound
    public void Play(string name, float vol = 1f, float pitch = 1f)
    {
        if (oneShot == null || !clips.TryGetValue(name, out AudioClip c)) return;
        oneShot.pitch = pitch;
        oneShot.PlayOneShot(c, vol * volume * GameSettings.SfxVolume);
    }

    public void StartLoop(string name, float vol = 1f, float pitch = 1f)
    {
        if (loop == null || !clips.TryGetValue(name, out AudioClip c)) return;
        if (loop.clip != c || !loop.isPlaying)
        {
            loop.clip = c;
            loop.Play();
        }
        loop.volume = vol * volume * GameSettings.SfxVolume;
        loop.pitch = pitch;
    }

    public void StopLoop()
    {
        if (loop != null && loop.isPlaying) loop.Stop();
    }

    delegate float Wave(float t, float dur, System.Random rng);

    AudioClip Make(string name, float dur, Wave wave)
    {
        int n = Mathf.CeilToInt(dur * Rate);
        float[] data = new float[n];
        System.Random rng = new System.Random(name.GetHashCode());
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, dur, rng), -1f, 1f);
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        clips[name] = clip;
        return clip;
    }

    static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
    static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
    static float Decay(float t, float dur, float power = 3f) => Mathf.Pow(Mathf.Clamp01(1f - t / dur), power);

    void BuildClips()
    {
        // 충전 윙윙 (1초 반복, 180Hz 정수 주기라 끊김 없음)
        Make("hum", 1f, (t, d, r) => (Sin(180f, t) * 0.5f + Sin(360f, t) * 0.25f + Sin(540f, t) * 0.1f) * (0.8f + 0.2f * Sin(6f, t)));
        // 완충 띵
        Make("ding", 0.45f, (t, d, r) => (Sin(1318f, t) + Sin(2637f, t) * 0.35f) * Decay(t, d, 2.5f) * 0.7f);
        // 적 처치: 짧게 떨어지는 퐁 + 푹 터지는 잡음
        Make("pop", 0.18f, (t, d, r) => (Sin(Mathf.Lerp(420f, 110f, t / d), t) * 0.55f + Noise(r) * 0.35f * Decay(t, 0.05f, 2f)) * Decay(t, d, 2f));
        // 레벨업: 도 · 미 · 솔 · 도 아르페지오 + 반짝임
        Make("levelup", 0.7f, (t, d, r) =>
        {
            float f = t < 0.08f ? 523f : t < 0.16f ? 659f : t < 0.24f ? 784f : 1047f;
            float shimmer = t > 0.24f ? Sin(2093f, t) * 0.15f * Sin(9f, t) : 0f;
            return (Sin(f, t) + Sin(f * 2f, t) * 0.3f + shimmer) * Decay(t, d, 1.3f) * 0.45f;
        });
        // 코인 반짝
        Make("sparkle", 0.22f, (t, d, r) => (t < 0.07f ? Sin(2093f, t) : Sin(2637f, t)) * Decay(t, d, 2f) * 0.25f);
        // 준비 알림 (두 음)
        Make("chime", 0.35f, (t, d, r) => (t < 0.12f ? Sin(880f, t) : Sin(1320f, t)) * Decay(t, d, 1.5f) * 0.5f);
        // 경고 (쿨타임, 부족)
        Make("buzz", 0.16f, (t, d, r) => Mathf.Sign(Sin(110f, t)) * 0.25f * Decay(t, d, 1f));
        // 폭발
        float low = 0f;
        Make("boom", 0.5f, (t, d, r) => { low += (Noise(r) - low) * 0.08f; return (low * 2.2f + Sin(55f, t) * 0.6f) * Decay(t, d, 2.5f); });
        // 가벼운 발사음 (주파수가 내려감)
        Make("pew", 0.12f, (t, d, r) => Mathf.Sign(Sin(Mathf.Lerp(1400f, 500f, t / d), t)) * 0.3f * Decay(t, d, 2f));
        // 저격 발사 (날카로운 파열음)
        Make("crack", 0.3f, (t, d, r) => (Noise(r) * 0.7f * Decay(t, 0.08f, 2f) + Sin(Mathf.Lerp(900f, 200f, t / d), t) * 0.5f) * Decay(t, d, 2f));
        // 전기
        Make("zap", 0.18f, (t, d, r) => (Noise(r) * 0.4f + Mathf.Sign(Sin(2200f + 800f * Sin(40f, t), t)) * 0.3f) * Decay(t, d, 1.5f));
        // 바람 (소리가 커졌다 작아짐)
        float wlow = 0f;
        Make("whoosh", 0.35f, (t, d, r) => { wlow += (Noise(r) - wlow) * 0.15f; return wlow * 1.6f * Mathf.Sin(Mathf.PI * t / d); });
        // 불 타는 소리 (1초 반복)
        Make("crackle", 1f, (t, d, r) => { float n = Noise(r); return (r.NextDouble() < 0.004 ? n : n * 0.12f) * 0.8f; });
        // 화염 방사 (1초 반복): 낮게 울리는 불길 + 딱딱 튀는 소리
        float roar = 0f, roar2 = 0f;
        Make("flame", 1f, (t, d, r) =>
        {
            float n = Noise(r);
            roar += (n - roar) * 0.04f;          // 낮은 굉음
            roar2 += (n - roar2) * 0.35f;        // 쉬익거리는 바람
            float pop = r.NextDouble() < 0.006 ? Noise(r) * 0.8f : 0f;
            float swell = 0.85f + 0.15f * Sin(3f, t);
            return (roar * 2.4f + roar2 * 0.35f) * swell + pop;
        });
        // 점화 화악 (소리가 빠르게 커졌다 잦아듦)
        float ign = 0f;
        Make("ignite", 0.4f, (t, d, r) =>
        {
            ign += (Noise(r) - ign) * Mathf.Lerp(0.05f, 0.3f, t / d);
            return ign * 2f * Mathf.Min(1f, t / 0.05f) * Decay(t, d, 1.5f);
        });
        // 과열 치익
        Make("hiss", 0.6f, (t, d, r) => Noise(r) * 0.35f * Decay(t, d, 1.2f));
        // 신비로운 화음 (마법)
        Make("shimmer", 0.6f, (t, d, r) => (Sin(660f, t) + Sin(880f * (1f + t * 0.5f), t) * 0.6f + Sin(1320f, t) * 0.3f) * Decay(t, d, 1.5f) * 0.35f);
        // 쇠사슬 철컥
        Make("clank", 0.2f, (t, d, r) => (Sin(1870f, t) + Sin(2630f, t) * 0.7f + Sin(3410f, t) * 0.5f + Noise(r) * 0.3f) * Decay(t, d, 4f) * 0.35f);
        // 유탄 발사 퉁
        Make("thump", 0.18f, (t, d, r) => Sin(Mathf.Lerp(160f, 60f, t / d), t) * Decay(t, d, 2f) * 0.8f);
        // 낮은 울림 (희생)
        Make("pulse", 0.7f, (t, d, r) => (Sin(70f, t) * 0.7f + Sin(140f, t) * 0.3f) * Mathf.Sin(Mathf.PI * t / d) * 0.8f);

        // ---------------- 강렬한 소리 (총성 · 필살기)
        // 권총 총성 "탕": 날카로운 파열 + 짧은 저음 킥 + 잔향
        float gl = 0f;
        Make("gunshot", 0.32f, (t, d, r) =>
        {
            float n = Noise(r);
            gl += (n - gl) * 0.25f;
            float crack = n * Decay(t, 0.025f, 1.5f) * 1.2f;
            float body = gl * 1.6f * Decay(t, 0.12f, 2f);
            float kick = Sin(Mathf.Lerp(180f, 50f, Mathf.Clamp01(t / 0.08f)), t) * Decay(t, 0.14f, 2f) * 0.9f;
            float tail = gl * 0.35f * Decay(t, d, 3f);
            return crack + body + kick + tail;
        });
        // 산탄총 "쾅": 더 두껍고 긴 굉음
        float sl = 0f, sl2 = 0f;
        Make("shotgun", 0.7f, (t, d, r) =>
        {
            float n = Noise(r);
            sl += (n - sl) * 0.12f; sl2 += (n - sl2) * 0.03f;
            float crack = n * Decay(t, 0.04f, 1.2f) * 1.1f;
            float kick = Sin(Mathf.Lerp(120f, 35f, Mathf.Clamp01(t / 0.15f)), t) * Decay(t, 0.3f, 1.8f) * 1.1f;
            return crack + sl * 1.8f * Decay(t, 0.25f, 2f) + sl2 * 2.5f * Decay(t, d, 2f) + kick;
        });
        // 큰 폭발: 긴 저음 굉음 + 파편
        float bl = 0f, bl2 = 0f;
        Make("bigboom", 1.2f, (t, d, r) =>
        {
            float n = Noise(r);
            bl += (n - bl) * 0.05f; bl2 += (n - bl2) * 0.012f;
            float debris = r.NextDouble() < 0.01 ? Noise(r) * Decay(t, d, 1f) * 0.6f : 0f;
            float kick = Sin(Mathf.Lerp(90f, 28f, Mathf.Clamp01(t / 0.3f)), t) * Decay(t, 0.6f, 1.5f) * 1.2f;
            return n * Decay(t, 0.05f, 1.2f) + bl * 2.2f * Decay(t, 0.5f, 1.5f) + bl2 * 3.5f * Decay(t, d, 1.5f) + kick + debris;
        });
        // 레일건 충전 (0.5초): 올라가는 전자음
        Make("railcharge", 0.55f, (t, d, r) =>
        {
            float f = Mathf.Lerp(200f, 1800f, t / d);
            return (Sin(f, t) * 0.4f + Sin(f * 1.5f, t) * 0.2f + Noise(r) * 0.1f * t / d) * Mathf.Min(1f, t / 0.05f);
        });
        // 레일건 발사: 강한 전기 파열 + 긴 윙
        Make("railgun", 1f, (t, d, r) =>
        {
            float n = Noise(r);
            float zap = (n * 0.8f + Mathf.Sign(Sin(1600f - 1200f * t, t)) * 0.5f) * Decay(t, 0.25f, 1.5f);
            float boom = Sin(Mathf.Lerp(110f, 40f, t), t) * Decay(t, 0.5f, 1.5f);
            float ring = Sin(3200f, t) * 0.15f * Decay(t, d, 2f);
            return zap + boom + ring;
        });
        // 천둥: 찢어지는 파열 + 구르는 저음
        float th = 0f, th2 = 0f;
        Make("thunder", 1.3f, (t, d, r) =>
        {
            float n = Noise(r);
            th += (n - th) * 0.2f; th2 += (n - th2) * 0.01f;
            float crack = (th * 1.8f) * Decay(t, 0.18f, 1.2f) * (0.6f + 0.4f * Mathf.Sign(Sin(23f, t)));
            return crack + th2 * 4f * Decay(t, d, 1.2f) * (0.7f + 0.3f * Sin(3f, t));
        });
        // 금속 베기 "슈악": 바람 + 쇳소리
        float sw = 0f;
        Make("slash", 0.35f, (t, d, r) =>
        {
            sw += (Noise(r) - sw) * 0.4f;
            float metal = (Sin(2400f, t) + Sin(3700f, t) * 0.6f) * 0.25f * Decay(t, d, 2.5f);
            return sw * 1.4f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / (d * 0.6f))) + metal;
        });
        // 회오리 굉음 (1초 반복)
        float ro = 0f, ro2 = 0f;
        Make("roar", 1f, (t, d, r) =>
        {
            float n = Noise(r);
            ro += (n - ro) * 0.02f; ro2 += (n - ro2) * 0.2f;
            return (ro * 4f + ro2 * 0.4f) * (0.75f + 0.25f * Sin(4f, t));
        });
    }

    // ================================================================= lines
    public LineRenderer NewLine(string name, bool dashed, int order = 20)
    {
        GameObject go = new GameObject(name);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = dashed ? new Material(dashMaterialTemplate) : lineMaterial;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingLayerName = "Effect";
        lr.sortingOrder = order;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.enabled = false;
        return lr;
    }

    // 점선 한 칸의 길이 (월드 단위)
    const float DashLength = 0.9f;

    void SetDashScale(LineRenderer lr, float length)
    {
        if (lr.material.mainTexture != null) lr.material.mainTextureScale = new Vector2(Mathf.Max(1f, length / DashLength), 1f);
    }

    public void SetLine(LineRenderer lr, Vector3 a, Vector3 b, Color color, float width)
    {
        lr.enabled = true;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        SetDashScale(lr, Vector3.Distance(a, b));
    }

    public void SetRing(LineRenderer lr, Vector3 center, float radius, Color color, float width)
    {
        const int seg = 48;
        lr.enabled = true;
        lr.positionCount = seg + 1;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        SetDashScale(lr, 2f * Mathf.PI * radius);
    }

    public void SetCone(LineRenderer lr, Vector3 origin, Vector2 dir, float halfAngle, float range, Color color, float width)
    {
        const int seg = 16;
        lr.enabled = true;
        lr.positionCount = seg + 3;
        lr.SetPosition(0, origin);
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Lerp(-halfAngle, halfAngle, i / (float)seg);
            lr.SetPosition(i + 1, origin + (Vector3)((Vector2)(Quaternion.Euler(0, 0, a) * dir) * range));
        }
        lr.SetPosition(seg + 2, origin);
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        SetDashScale(lr, range * 2f + range * halfAngle * Mathf.Deg2Rad * 2f);
    }

    public static void Hide(LineRenderer lr)
    {
        if (lr != null) lr.enabled = false;
    }

    // ================================================================= floating text
    readonly Dictionary<string, float> lastText = new Dictionary<string, float>();

    // 캐릭터 위로 떠오르며 사라지는 글자 (같은 글자는 잠깐 동안 한 번만)
    public void FloatText(Vector3 pos, string text, Color color, float size = 5f, float throttle = 0.6f)
    {
        if (lastText.TryGetValue(text, out float t) && Time.time - t < throttle) return;
        lastText[text] = Time.time;

        GameObject go = new GameObject("FloatText");
        go.transform.position = pos + new Vector3(0f, 3.2f, 0f);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.sortingLayerID = SortingLayer.NameToID("Effect");
        tmp.sortingOrder = 30;
        go.AddComponent<FloatUp>();
    }

    // ================================================================= screen shake
    public void Shake(float strength = 0.25f, float duration = 0.12f)
    {
        if (!GameSettings.ScreenShake) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        if (shake != null) StopCoroutine(shake);
        shake = StartCoroutine(ShakeRoutine(cam.transform, strength, duration));
    }

    Vector3 camRest;
    bool camRestSet;

    IEnumerator ShakeRoutine(Transform cam, float strength, float duration)
    {
        if (!camRestSet) { camRest = cam.localPosition; camRestSet = true; }
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / duration;
            shakeOffset = Random.insideUnitCircle * strength * k;
            yield return null;
        }
        shakeOffset = Vector2.zero;
        shake = null;
    }

    // ================================================================= camera hold
    // 카메라는 플레이어의 자식이라 원래는 플레이어를 따라감.
    // HoldCamera 동안에는 월드의 한 점에 머물고, ReleaseCamera 하면 부드럽게 플레이어에게 돌아감
    Vector2 shakeOffset;
    Vector3 holdTarget, holdPos;
    bool holding;
    float returnBlend = 1f;             // 1 = 플레이어에 붙음

    public void HoldCamera(Vector3 worldCenter)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (!camRestSet) { camRest = cam.transform.localPosition; camRestSet = true; }
        if (!holding) holdPos = cam.transform.position;
        holdTarget = new Vector3(worldCenter.x, worldCenter.y, cam.transform.position.z);
        holding = true;
        returnBlend = 0f;
    }

    public void ReleaseCamera() => holding = false;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || !camRestSet) return;
        Transform t = cam.transform;
        float dt = Time.unscaledDeltaTime;
        if (holding)
        {
            holdPos = Vector3.Lerp(holdPos, holdTarget, 1f - Mathf.Exp(-12f * dt));
            t.position = holdPos + (Vector3)shakeOffset;
            return;
        }
        if (returnBlend < 1f)
        {
            // 붙잡혀 있던 자리에서 플레이어 쪽으로 돌아감
            returnBlend = Mathf.Min(1f, returnBlend + dt / 0.25f);
            Vector3 follow = t.parent != null ? t.parent.TransformPoint(camRest) : camRest;
            float k = 1f - (1f - returnBlend) * (1f - returnBlend);
            holdPos = Vector3.Lerp(holdPos, follow, k);
            t.position = holdPos + (Vector3)shakeOffset;
            return;
        }
        t.localPosition = camRest + (Vector3)shakeOffset;
    }
}

// 위로 떠오르며 사라짐
public class FloatUp : MonoBehaviour
{
    public float duration = 0.9f;
    float t;
    TextMeshPro tmp;
    Color c;

    void Start()
    {
        tmp = GetComponent<TextMeshPro>();
        c = tmp.color;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        transform.position += Vector3.up * 2.2f * Time.unscaledDeltaTime;
        tmp.color = new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01(1f - (t - duration * 0.5f) / (duration * 0.5f)));
        if (t >= duration) Destroy(gameObject);
    }
}

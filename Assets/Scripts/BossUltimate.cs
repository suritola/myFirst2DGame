using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보스 필살기 "결계" (2.1~): 보스를 때릴 때마다 필살기 게이지가 차고 (한 번에 세게 때릴수록 훨씬 많이),
// 가득 차면 → 게임이 멈추고 카메라가 보스에게 → 화면 가운데에 결계 이름 · 대사가 흔들리며 터짐 (웅장한 외침)
// → 맵 전체가 어두워지고 보스 둘레에 둥근 결계가 펼쳐져 플레이어가 그 안에 갇힘 → 9초 동안 보스마다 다른 공격이 쏟아짐
// (모두 경고를 먼저 보여 줘서 피할 수는 있음) → 결계가 깨지며 끝
// 킹 슬라임은 갈라져도 게이지를 함께 씀 (한 번에 하나만)
public class BossUltimate : MonoBehaviour
{
    public static bool Active { get; private set; }

    // 보스 종류마다 게이지 (0 ~ 1)
    static readonly float[] gauge = new float[3];
    static readonly float[] refHp = new float[3];
    public static float Gauge(int kind) => kind >= 0 && kind < gauge.Length ? gauge[kind] : 0f;

    const float Radius = 35f;             // 13 은 너무 좁아 피할 틈이 없었음 (화면보다 넓게)
    const float Duration = 9f;
    const float Power = 1.25f;            // BossSkills 와 같은 피해 배율

    static readonly (string title, string line, Color color)[] Info =
    {
        ("망자의 묘역", "이 안에서는, 죽은 자만이 걷는다.", new Color(0.55f, 0.95f, 1f)),
        ("연옥 낙화", "이곳의 불은 꺼지지 않는다.", new Color(1f, 0.45f, 0.12f)),
        ("산성 범람", "전부, 녹아내려라.", new Color(0.55f, 1f, 0.35f)),
    };

    bosss boss;
    int kind;
    float spawnedAt;
    static AudioSource sfx;

    void Start()
    {
        boss = GetComponent<bosss>();
        kind = boss != null ? Mathf.Clamp(boss.bossKind, 0, 2) : 0;
        spawnedAt = Time.time;
        // 새 보스 (킹 슬라임은 첫 덩어리)가 나오면 게이지를 비우고 기준 체력을 정함
        if (boss != null && (kind != 2 || boss.slimeGen == 1))
        {
            gauge[kind] = 0f;
            refHp[kind] = Mathf.Max(1f, boss.setEnemyHP);
        }
    }

    void OnDestroy()
    {
        if (running) EndNow();
    }

    // 보스가 맞을 때 (bosss.TakeDamage): 피해 비율의 1.2제곱 → 약한 공격 여러 번보다 강한 한 방이 더 많이 채움
    // (예전 5 · 1.3제곱은 너무 늦게 차서, 한 판에 약한 공격만으로도 두세 번 · 센 공격 위주면 네댓 번 차게)
    public static void OnBossHit(bosss b, float damage)
    {
        if (b == null || Active || b.IsDead || damage <= 0f) return;
        int k = Mathf.Clamp(b.bossKind, 0, 2);
        float f = damage / Mathf.Max(1f, refHp[k] > 0f ? refHp[k] : b.setEnemyHP);
        gauge[k] = Mathf.Min(1f, gauge[k] + Mathf.Min(0.5f, 7f * Mathf.Pow(f, 1.2f)));
    }

    void Update()
    {
        if (Active || boss == null || boss.IsDead || gauge[kind] < 1f) return;
        if (Time.time - spawnedAt < 4f || Time.time < restUntil || Time.timeScale == 0f || StoryDirector.Playing || boss.casting) return;
        if (SkillEvolutionUI.Open || WeaponEvolutionUI.Open || ESCmenu.IsOpen) return;
        // 킹 슬라임: 살아 있는 덩어리 중 체력이 가장 많은 하나만 씀
        if (kind == 2 && !IsStrongestSlime()) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        StartCoroutine(Run());
    }

    bool IsStrongestSlime()
    {
        List<bosss> live = new List<bosss>();
        bosss.CollectLiveSlimes(live);
        foreach (bosss s in live) if (s != boss && s.EnemyHealth > boss.EnemyHealth) return false;
        return true;
    }

    // ================================================================= 진행
    bool running;
    Vector3 center;
    GameObject domainRoot;
    LineRenderer ring, ring2;
    FxAnim rune;
    GameObject aura;

    IEnumerator Run()
    {
        running = true;
        Active = true;
        gauge[kind] = 0f;
        boss.casting = true;
        PlayerController player = Hostile.Player;
        player.CancelSkill();
        var info = Info[kind];

        yield return Cutscene(info.title, info.line, info.color);

        center = Hostile.ClampArena(boss.transform.position);
        yield return Expand(info.color);
        float until = Time.time + Duration;
        IEnumerator attack = kind == 0 ? LichBarrage(until) : kind == 1 ? HellBarrage(until) : SlimeBarrage(until);
        StartCoroutine(attack);
        while (Time.time < until && boss != null && !boss.IsDead && player != null && !player.IsDying)
        {
            Confine(player);
            Animate(info.color);
            yield return null;
        }
        StopCoroutine(attack);
        yield return Shatter(info.color);
        EndNow();
    }

    // 결계가 끝난 뒤 쉬는 시간 (게이지가 빨리 차도 연달아 펼치지 않게)
    static float restUntil;

    void EndNow()
    {
        restUntil = Time.time + 15f;
        if (domainRoot != null) Destroy(domainRoot);
        if (aura != null) Destroy(aura);
        if (boss != null) boss.casting = false;
        Active = false;
        running = false;
    }

    // ---------------- 1. 외침: 멈춤 → 카메라가 보스로 → 결계 이름 · 대사
    IEnumerator Cutscene(string title, string line, Color color)
    {
        float before = Time.timeScale;
        Time.timeScale = 0f;
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        fx?.HoldCamera(boss.transform.position);

        Canvas canvas = NewCanvas("BossUltimateCanvas", 30500);
        RectTransform root = (RectTransform)canvas.transform;
        Image shade = Img(root, "Shade", new Color(0f, 0f, 0f, 0f));
        Stretch(shade.rectTransform);
        Image flash = Img(root, "Flash", new Color(1f, 1f, 1f, 0f));
        Stretch(flash.rectTransform);
        Image band = Img(root, "Band", new Color(0f, 0f, 0f, 0f));
        band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        band.rectTransform.sizeDelta = new Vector2(0f, 0f);

        UIKit.EnsureStyle();
        TMP_Text tag = UIKit.Text(root, "결계 침식", 34f, new Color(0.85f, 0.8f, 0.9f), new Vector2(0f, 120f), new Vector2(1400f, 50f));
        TMP_Text name = UIKit.Text(root, title, 120f, color, new Vector2(0f, 25f), new Vector2(1800f, 160f));
        name.fontStyle = FontStyles.Bold;
        name.outlineWidth = 0.25f;
        name.outlineColor = new Color32(0, 0, 0, 255);
        TMP_Text say = UIKit.Text(root, line, 44f, new Color(0.95f, 0.92f, 0.88f), new Vector2(0f, -105f), new Vector2(1600f, 70f));
        tag.alpha = name.alpha = say.alpha = 0f;

        // 카메라가 보스 쪽으로 움직이는 동안 화면이 어두워짐
        for (float t = 0f; t < 0.7f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.7f;
            shade.color = new Color(0f, 0f, 0f, 0.55f * k);
            if (boss != null) fx?.HoldCamera(boss.transform.position);
            yield return null;
        }

        // 쾅! 이름이 내려꽂히며 흔들림 + 외침
        PlaySfx("domain_shout", 1f);
        Hostile.Play("roar", 1f, kind == 2 ? 0.75f : kind == 1 ? 0.6f : 0.8f);
        fx?.Shake(0.9f, 0.6f);
        for (float t = 0f; t < 2.3f; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / 0.25f);
            flash.color = new Color(color.r, color.g, color.b, 0.55f * (1f - Mathf.Clamp01(t / 0.35f)));
            band.rectTransform.sizeDelta = new Vector2(0f, Mathf.Lerp(0f, 380f, EaseOut(k)));
            band.color = new Color(0f, 0f, 0f, 0.75f);
            tag.alpha = k;
            name.alpha = k;
            name.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, EaseOut(k));
            float shake = Mathf.Lerp(18f, 4f, Mathf.Clamp01(t / 1.5f));
            name.rectTransform.anchoredPosition = new Vector2(0f, 25f) + Random.insideUnitCircle * shake;
            say.alpha = Mathf.Clamp01((t - 0.45f) / 0.3f);
            say.rectTransform.anchoredPosition = new Vector2(0f, -105f) + Random.insideUnitCircle * shake * 0.4f;
            shade.color = new Color(color.r * 0.15f, color.g * 0.05f, color.b * 0.1f, 0.6f + 0.1f * Mathf.Sin(t * 20f));
            if (boss != null) fx?.HoldCamera(boss.transform.position);
            yield return null;
        }
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / 0.3f;
            tag.alpha = name.alpha = say.alpha = k;
            band.color = new Color(0f, 0f, 0f, 0.75f * k);
            shade.color = new Color(0f, 0f, 0f, 0.6f * k);
            yield return null;
        }
        Destroy(canvas.gameObject);
        fx?.ReleaseCamera();
        Time.timeScale = before > 0f ? before : 1f;
    }

    // ---------------- 2. 결계 침식: 맵이 어두워지고 둥근 결계가 퍼짐
    IEnumerator Expand(Color color)
    {
        domainRoot = new GameObject("BossDomain");
        domainRoot.transform.position = center;
        PlaySfx("domain_open", 1f);
        Hostile.Shake(0.5f);

        MeshRenderer dark = DarkMesh(domainRoot.transform, color);
        ring = Hostile.NewLine("DomainRing", color, 0.9f, 40);
        ring.loop = true;
        ring.transform.SetParent(domainRoot.transform, true);
        ring2 = Hostile.NewLine("DomainRing2", Color.white, 0.25f, 41);
        ring2.loop = true;
        ring2.transform.SetParent(domainRoot.transform, true);
        rune = Fx.Play("fx_rune", center, Radius * 2f, new Color(color.r, color.g, color.b, 0.35f), 1f, 0f, 1, true);
        if (rune != null) { rune.spin = 12f; rune.transform.SetParent(domainRoot.transform, true); }
        if (Hostile.Glow != null)
        {
            aura = SpecialAbilities.MakeSprite("BossDomainAura", Hostile.Glow, boss.transform.position, 1.2f, new Color(color.r, color.g, color.b, 0.5f), "Effect", 1);
        }
        Fx.Spawn("fx_shock", center, Radius * 2.6f, color, 14f);
        Fx.Spawn("fx_soulburst", center, 12f, Color.white, 14f);

        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            float k = EaseOut(t / 0.8f);
            float r = Mathf.Lerp(0.5f, Radius, k);
            SetDark(dark, Mathf.Lerp(0f, 1f, k));
            Hostile.SetArc(ring, center, r, 0f, 360f);
            Hostile.SetArc(ring2, center, r - 0.35f, 0f, 360f);
            // 결계 밖에 있으면 안쪽으로 끌려 들어옴
            PlayerController p = Hostile.Player;
            if (p != null && (p.transform.position - center).magnitude > r - 0.8f && r > 4f) Confine(p, r);
            yield return null;
        }
        SetDark(dark, 1f);
    }

    void Animate(Color color)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        if (ring != null)
        {
            Hostile.SetArc(ring, center, Radius, 0f, 360f);
            ring.startWidth = ring.endWidth = 0.8f + 0.25f * pulse;
            ring.startColor = ring.endColor = Color.Lerp(color, Color.white, 0.25f * pulse);
        }
        if (ring2 != null) Hostile.SetArc(ring2, center, Radius - 0.4f, Time.time * 40f, Time.time * 40f + 360f);
        if (aura != null && boss != null)
        {
            aura.transform.position = boss.transform.position;
            aura.transform.localScale = Vector3.one * (1.2f + 0.2f * pulse);
        }
        // 결계 가장자리에서 튀는 불꽃
        if (Random.value < 0.4f)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Fx.Spawn("fx_spark", center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius, 2f, color, 18f);
        }
    }

    void Confine(PlayerController p, float r = Radius)
    {
        Vector3 d = p.transform.position - center;
        float max = r - 0.8f;
        if (d.magnitude > max) p.transform.position = center + d.normalized * max;
    }

    // ---------------- 3. 결계 파열
    IEnumerator Shatter(Color color)
    {
        PlaySfx("domain_break", 1f);
        Hostile.Shake(0.4f);
        for (int i = 0; i < 36; i++)
        {
            float a = i * 10f * Mathf.Deg2Rad;
            Vector3 at = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius;
            Fx.Spawn("fx_spark", at, 2.6f, Color.Lerp(color, Color.white, 0.5f), 16f);
        }
        Fx.Spawn("fx_shock", center, Radius * 2.4f, Color.white, 14f);
        MeshRenderer dark = domainRoot != null ? domainRoot.GetComponentInChildren<MeshRenderer>() : null;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            float k = 1f - t / 0.6f;
            if (dark != null) SetDark(dark, k);
            if (ring != null) ring.startColor = ring.endColor = new Color(color.r, color.g, color.b, k);
            if (ring2 != null) ring2.startColor = ring2.endColor = new Color(1f, 1f, 1f, k);
            yield return null;
        }
    }

    // ================================================================= 보스마다 다른 공격
    // 망자의 묘역 (리치 왕): 회전하는 영혼 광선 넷 + 틈이 있는 영혼 고리 + 발밑 뼈 가시
    IEnumerator LichBarrage(float until)
    {
        Color soul = Info[0].color, curse = new Color(0.75f, 0.4f, 1f);
        float angle = Random.Range(0f, 90f), spin = Random.value < 0.5f ? 22f : -22f;
        float nextRing = Time.time + 0.6f, nextSpike = Time.time;
        LineRenderer[] warn = new LineRenderer[4];
        for (int i = 0; i < 4; i++) { warn[i] = Hostile.NewLine("LichBeamWarn", new Color(soul.r, soul.g, soul.b, 0.3f), 0.18f, 30); warn[i].transform.SetParent(domainRoot.transform, true); }
        float live = Time.time + 1.4f, beamHit = 0f;
        FxAnim[] beams = new FxAnim[4];
        while (Time.time < until)
        {
            angle += spin * Time.deltaTime;
            Vector3 c = boss != null ? boss.transform.position : center;
            bool on = Time.time > live;
            for (int i = 0; i < 4; i++)
            {
                float a = (angle + i * 90f) * Mathf.Deg2Rad;
                Vector3 end = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius * 2f;
                warn[i].positionCount = 2;
                warn[i].SetPosition(0, c);
                warn[i].SetPosition(1, end);
                if (on)
                {
                    if (beams[i] != null) Destroy(beams[i].gameObject);
                    beams[i] = Fx.Beam(c, end, 1.4f, soul, 0.08f, 22);
                }
            }
            PlayerController p = Hostile.Player;
            if (on && p != null && (beamHit -= Time.deltaTime) <= 0f)
                for (int i = 0; i < 4; i++)
                {
                    float a = (angle + i * 90f) * Mathf.Deg2Rad;
                    Vector3 end = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius * 2f;
                    if (Hostile.DistanceToSegment(p.transform.position, c, end) < 0.85f) { p.TryHit(16f * Power); beamHit = 0.4f; break; }
                }
            if (Time.time >= nextRing)
            {
                nextRing = Time.time + 1.5f;
                int n = 26, gap = Random.Range(0, n);
                for (int i = 0; i < n; i++)
                {
                    if (Mathf.Abs(i - gap) <= 2 || Mathf.Abs(i - gap) >= n - 2) continue;     // 다섯 칸짜리 틈
                    float a = i * Mathf.PI * 2f / n;
                    Hostile.Shoot(c, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6.5f, 14f * Power, 0.5f, curse, 1.6f, 3f);
                }
                Hostile.Play("shimmer", 0.5f, 0.6f);
            }
            if (Time.time >= nextSpike && p != null)
            {
                nextSpike = Time.time + 0.55f;
                Vector3 at = p.transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
                StartCoroutine(Spike(at, 1.7f, 0.75f, soul, 18f * Power, "fx_spike"));
            }
            yield return null;
        }
        foreach (FxAnim b in beams) if (b != null) Destroy(b.gameObject);
    }

    // 연옥 낙화 (지옥의 군주): 쏟아지는 운석 + 틈이 있는 화염 고리 + 결계를 가로지르는 용암 줄기
    IEnumerator HellBarrage(float until)
    {
        Color fire = Info[1].color;
        float nextMeteor = Time.time, nextWave = Time.time + 0.8f, nextLine = Time.time + 2f;
        while (Time.time < until)
        {
            PlayerController p = Hostile.Player;
            if (Time.time >= nextMeteor && p != null)
            {
                nextMeteor = Time.time + 0.28f;
                Vector3 at = Random.value < 0.5f ? p.transform.position + (Vector3)(Random.insideUnitCircle * 2.5f) : center + (Vector3)(Random.insideUnitCircle * (Radius - 1f));
                StartCoroutine(Spike(at, 2.2f, 0.9f, fire, 22f * Power, "fx_meteor"));
            }
            if (Time.time >= nextWave)
            {
                nextWave = Time.time + 2.2f;
                StartCoroutine(FireRing(boss != null ? boss.transform.position : center, fire));
            }
            if (Time.time >= nextLine)
            {
                nextLine = Time.time + 2.6f;
                float a = Random.Range(0f, Mathf.PI);
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a));
                Vector3 mid = p != null ? p.transform.position : center;
                StartCoroutine(LavaLine(mid - d * Radius * 1.6f, mid + d * Radius * 1.6f, fire));
            }
            yield return null;
        }
    }

    IEnumerator FireRing(Vector3 c, Color fire)
    {
        const int gaps = 3;
        float start = Random.Range(0f, 360f), gapSize = 50f;
        LineRenderer[] arcs = new LineRenderer[gaps];
        for (int i = 0; i < gaps; i++) { arcs[i] = Hostile.NewLine("DomainFireArc", fire, 1.1f, 25); arcs[i].transform.SetParent(domainRoot.transform, true); }
        bool hit = false;
        Hostile.Play("flame", 0.6f, 0.7f);
        for (float r = 1f; r < Radius * 1.1f; r += 7f * Time.deltaTime)
        {
            for (int i = 0; i < gaps; i++)
            {
                float g = start + i * 360f / gaps;
                Hostile.SetArc(arcs[i], c, r, g + gapSize * 0.5f, g + 360f / gaps - gapSize * 0.5f);
            }
            PlayerController p = Hostile.Player;
            if (!hit && p != null)
            {
                Vector2 to = p.transform.position - c;
                float ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                bool inGap = false;
                for (int i = 0; i < gaps; i++) if (Mathf.Abs(Mathf.DeltaAngle(ang, start + i * 360f / gaps)) < gapSize * 0.5f) inGap = true;
                if (Mathf.Abs(to.magnitude - r) < 0.7f && !inGap) hit = p.TryHit(20f * Power);
            }
            yield return null;
        }
        foreach (LineRenderer l in arcs) if (l != null) Destroy(l.gameObject);
    }

    IEnumerator LavaLine(Vector3 a, Vector3 b, Color fire)
    {
        Hostile.Line(a, b, 2.2f, 1.1f, fire);
        yield return new WaitForSeconds(1.1f);
        Vector3 d = (b - a);
        for (float k = 0f; k <= 1f; k += 0.06f)
        {
            Vector3 at = a + d * k;
            if ((at - center).magnitude > Radius) continue;
            Fx.Spawn("fx_firepillar", at + Vector3.up * 1.2f, 2.8f, Color.white, 20f, 0f, 14);
        }
        PlayerController p = Hostile.Player;
        if (p != null && Hostile.DistanceToSegment(p.transform.position, a, b) < 1.1f) p.TryHit(24f * Power);
        Hostile.Play("boom", 0.6f, 0.8f);
        Hostile.Shake(0.15f);
    }

    // 산성 범람 (킹 슬라임): 결계 벽에 튕기는 슬라임 덩어리 + 쏟아지는 산성 비 + 번갈아 솟는 줄무늬 간헐천
    IEnumerator SlimeBarrage(float until)
    {
        Color acid = Info[2].color;
        float nextBall = Time.time, nextRain = Time.time + 0.3f, nextStripe = Time.time + 1.2f;
        int stripe = 0;
        List<GameObject> balls = new List<GameObject>();
        while (Time.time < until)
        {
            PlayerController p = Hostile.Player;
            if (Time.time >= nextBall && balls.Count < 8)
            {
                nextBall = Time.time + 0.9f;
                balls.Add(Bouncer(boss != null ? boss.transform.position : center, acid, until - Time.time));
            }
            if (Time.time >= nextRain && p != null)
            {
                nextRain = Time.time + 0.85f;
                for (int i = 0; i < 5; i++)
                {
                    Vector3 at = i == 0 ? p.transform.position : p.transform.position + (Vector3)(Random.insideUnitCircle * 5f);
                    StartCoroutine(Spike(at, 1.8f, 1f, acid, 16f * Power, "fx_geyser"));
                }
            }
            if (Time.time >= nextStripe)
            {
                nextStripe = Time.time + 2.4f;
                StartCoroutine(Stripes(stripe++ % 2, acid));
            }
            balls.RemoveAll(g => g == null);
            yield return null;
        }
        foreach (GameObject g in balls) if (g != null) Destroy(g);
    }

    // 줄무늬 간헐천: 결계를 가로줄로 나눠 한 줄 건너 한 줄씩 (다른 줄로 피함)
    IEnumerator Stripes(int odd, Color acid)
    {
        const float w = 2.6f;
        List<(Vector3 a, Vector3 b)> rows = new List<(Vector3, Vector3)>();
        int i = 0;
        for (float y = -Radius + w * 0.5f; y < Radius; y += w, i++)
        {
            if (i % 2 != odd) continue;
            float half = Mathf.Sqrt(Mathf.Max(0f, Radius * Radius - y * y));
            Vector3 a = center + new Vector3(-half, y), b = center + new Vector3(half, y);
            rows.Add((a, b));
            Hostile.Line(a, b, w, 1.1f, new Color(acid.r, acid.g, acid.b, 0.6f));
        }
        yield return new WaitForSeconds(1.1f);
        PlayerController p = Hostile.Player;
        foreach (var r in rows)
        {
            for (float k = 0f; k <= 1f; k += 0.12f) Fx.Spawn("fx_geyser", Vector3.Lerp(r.a, r.b, k) + Vector3.up * 1.4f, 3f, Color.white, 18f, 0f, 14);
            if (p != null && Hostile.DistanceToSegment(p.transform.position, r.a, r.b) < w * 0.5f) p.TryHit(18f * Power);
        }
        Hostile.Play("hiss", 0.7f, 0.8f);
    }

    GameObject Bouncer(Vector3 from, Color acid, float life)
    {
        if (Hostile.Glow == null) return null;
        GameObject g = SpecialAbilities.MakeSprite("AcidBall", Hostile.Glow, from, 0.22f, new Color(acid.r, acid.g, acid.b, 0.95f), "Effect", 20);
        DomainBall b = g.AddComponent<DomainBall>();
        b.center = center;
        b.radius = Radius - 0.6f;
        b.velocity = Random.insideUnitCircle.normalized * 7f;
        b.damage = 15f * Power;
        b.life = Mathf.Max(1f, life);
        return g;
    }

    // 경고 원 → 위에서 떨어져(또는 솟아) 터짐
    IEnumerator Spike(Vector3 at, float r, float warn, Color color, float damage, string fx)
    {
        if ((at - center).magnitude > Radius - 0.5f) at = center + (at - center).normalized * (Radius - 0.5f);
        Hostile.Circle(at, r, warn, color);
        yield return new WaitForSeconds(warn);
        Fx.Spawn(fx, at + Vector3.up * (fx == "fx_meteor" ? 0.5f : 1.2f), r * 2.2f, Color.white, 18f, 0f, 14);
        Fx.Spawn("fx_explosion", at, r * 1.8f, color, 20f);
        Hostile.HitCircle(at, r, damage);
    }

    // ================================================================= 도우미
    // 결계 밖을 덮는 어둠 (둥근 구멍이 뚫린 큰 고리 메시): 안쪽은 보스 색으로 살짝 물듦
    MeshRenderer DarkMesh(Transform parent, Color color)
    {
        GameObject go = new GameObject("DomainDark", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        Mesh m = new Mesh();
        const int seg = 96;
        float[] radii = { 0f, Radius - 0.2f, Radius + 1.8f, 400f };
        List<Vector3> v = new List<Vector3>();
        List<int> tri = new List<int>();
        for (int ri = 0; ri < radii.Length; ri++)
            for (int s = 0; s < seg; s++)
            {
                float a = s * Mathf.PI * 2f / seg;
                v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radii[ri]);
            }
        for (int ri = 0; ri < radii.Length - 1; ri++)
            for (int s = 0; s < seg; s++)
            {
                int a = ri * seg + s, b = ri * seg + (s + 1) % seg, c = (ri + 1) * seg + s, d = (ri + 1) * seg + (s + 1) % seg;
                tri.AddRange(new[] { a, c, b, b, c, d });
            }
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        go.GetComponent<MeshFilter>().mesh = m;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.sortingLayerName = "Effect";
        mr.sortingOrder = -40;
        darkTint = color;
        return mr;
    }

    Color darkTint;

    void SetDark(MeshRenderer mr, float k)
    {
        if (mr == null) return;
        MeshFilter f = mr.GetComponent<MeshFilter>();
        Mesh m = f.mesh;
        int seg = m.vertexCount / 4;
        Color inner = new Color(darkTint.r * 0.25f, darkTint.g * 0.15f, darkTint.b * 0.3f, 0.32f * k);
        Color edge = new Color(0f, 0f, 0f, 0.55f * k);
        Color outer = new Color(0.01f, 0f, 0.02f, 0.9f * k);
        Color[] c = new Color[m.vertexCount];
        for (int i = 0; i < c.Length; i++)
        {
            int ri = i / seg;
            c[i] = ri <= 1 ? (ri == 0 ? inner : Color.Lerp(inner, edge, 0.3f)) : ri == 2 ? outer : outer;
        }
        m.colors = c;
    }

    static void PlaySfx(string name, float vol)
    {
        AudioClip clip = Resources.Load<AudioClip>("Sounds/" + name);
        if (clip == null) return;
        if (sfx == null)
        {
            GameObject go = new GameObject("BossUltimateAudio");
            DontDestroyOnLoad(go);
            sfx = go.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            sfx.ignoreListenerPause = true;
        }
        sfx.PlayOneShot(clip, vol * GameSettings.SfxVolume);
    }

    static Canvas NewCanvas(string name, int order)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        return c;
    }

    static Image Img(RectTransform parent, string name, Color c)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        Image i = go.GetComponent<Image>();
        i.color = c;
        i.raycastTarget = false;
        return i;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static float EaseOut(float k) => 1f - (1f - Mathf.Clamp01(k)) * (1f - Mathf.Clamp01(k));
}

// 결계 벽에 튕기며 굴러다니는 산성 덩어리 (닿으면 피해)
public class DomainBall : MonoBehaviour
{
    public Vector3 center;
    public float radius, damage, life;
    public Vector2 velocity;
    float age, trail;

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life || !BossUltimate.Active) { Destroy(gameObject); return; }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        Vector3 d = transform.position - center;
        if (d.magnitude > radius)
        {
            Vector2 n = d.normalized;
            velocity = Vector2.Reflect(velocity, -n);
            transform.position = center + (Vector3)(n * radius);
        }
        transform.localScale = Vector3.one * (0.22f + 0.02f * Mathf.Sin(age * 12f));
        trail += Time.deltaTime;
        if (trail > 0.12f) { trail = 0f; Fx.Spawn("fx_puddle", transform.position, 1.2f, new Color(0.55f, 1f, 0.35f, 0.6f), 14f); }
        PlayerController p = Hostile.Player;
        if (p != null && Vector2.Distance(p.transform.position, transform.position) < 1.1f) p.TryHit(damage);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스 스킬: 스킬 3개를 번갈아 쓰고, 체력이 절반 아래면 특수 스킬도 씀
// 모든 공격은 경고를 먼저 보여줘서 움직이면 피할 수 있음
// kind 0 = 리치 왕 (1장), 1 = 지옥의 군주 (2장)
public class BossSkills : MonoBehaviour
{
    public int kind;

    static readonly Color Soul = new Color(0.55f, 0.95f, 1f, 0.95f);
    static readonly Color Curse = new Color(0.75f, 0.4f, 1f, 0.9f);
    static readonly Color Fire = new Color(1f, 0.45f, 0.12f, 0.95f);
    static readonly Color Danger = new Color(1f, 0.25f, 0.2f, 0.9f);

    bosss boss;
    SpriteRenderer sr;
    float next;
    float nextSpecial;
    int step;
    bool busy;

    bool Alive => boss != null && !boss.IsDead;

    void Start()
    {
        boss = GetComponent<bosss>();
        sr = GetComponent<SpriteRenderer>();
        next = Time.time + 3f;
    }

    void Update()
    {
        if (busy || !Alive) return;
        PlayerController p = Hostile.Player;
        if (p == null) return;

        // 체력 절반 아래: 특수 스킬 (처음 한 번은 바로)
        if (boss.Enraged && Time.time >= nextSpecial)
        {
            nextSpecial = Time.time + 16f;
            string name = kind == 0 ? "리치 왕이 망자의 의식을 시작한다!" : kind == 1 ? "지옥의 군주가 십자 불길을 내뿜는다!" : "킹 슬라임이 미친 듯이 뛰어오른다!";
            if (StageManager.Instance != null) StageManager.Instance.ShowBanner(name, 2f);
            StartCoroutine(Run(kind == 0 ? DeathVortex() : kind == 1 ? HellCross() : SlimeFrenzy()));
            return;
        }

        if (Time.time < next) return;
        next = Time.time + (boss.Enraged ? 3.2f : 4.5f);
        step = (step + 1) % 3;
        IEnumerator skill = kind == 0
            ? (step == 0 ? SoulVolley() : step == 1 ? CurseMarks(p) : BoneSpears(p))
            : kind == 1
            ? (step == 0 ? FlameCharge(p) : step == 1 ? MeteorRain(p) : FireWave())
            : (step == 0 ? SlimeLeap(p, 1f) : step == 1 ? AcidRain(p) : SlimeRoll(p));
        StartCoroutine(Run(skill));
    }

    IEnumerator Run(IEnumerator routine)
    {
        busy = true;
        boss.casting = true;
        yield return StartCoroutine(routine);
        if (boss != null) boss.casting = false;
        if (sr != null && Alive) sr.color = Color.white;
        busy = false;
        next = Mathf.Max(next, Time.time + 1.2f);
    }

    IEnumerator Windup(Color color, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            sr.color = Color.Lerp(Color.white, color, Mathf.PingPong(Time.time * 6f, 1f));
            yield return null;
        }
        if (Alive) sr.color = Color.white;
    }

    Vector2 DirTo(PlayerController p) => ((Vector2)(p.transform.position - transform.position)).normalized;

    // ================================================================= 리치 왕
    // 망령의 손아귀: 보스 주변 세 겹의 고리에서 차례로 영혼의 손(가시)이 솟음 (고리 사이로 피함)
    IEnumerator SoulVolley()
    {
        Vector3 c = transform.position;
        FxAnim rune = Fx.Play("fx_rune", c, 6f, new Color(0.55f, 0.95f, 1f, 0.8f), 1f, 0f, 1, true, 2.4f);
        if (rune != null) rune.spin = 90f;
        yield return Windup(Soul, 0.6f);
        float[] rings = { 3f, 6f, 9f };
        for (int w = 0; w < rings.Length && Alive; w++)
        {
            int n = 6 + w * 4;
            float offset = Random.Range(0f, 360f);
            Vector3[] spots = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = (offset + i * 360f / n) * Mathf.Deg2Rad;
                spots[i] = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * rings[w];
                Hostile.Circle(spots[i], 1.3f, 0.7f, Soul);
            }
            yield return new WaitForSeconds(0.7f);
            if (!Alive) yield break;
            foreach (Vector3 at in spots)
            {
                Fx.Play("fx_spike", at + Vector3.up * 0.7f, 2.6f, new Color(0.6f, 0.95f, 1f), 18f);
                Fx.Play("fx_orb", at, 1f, new Color(0.7f, 0.9f, 1f), 18f);
                Hostile.HitCircle(at, 1.3f, 15f);
            }
            Hostile.Play("crack", 0.5f, 1.2f + w * 0.1f);
        }
    }

    // 저주 표식: 플레이어와 주변에 표식 → 잠시 뒤 폭발
    IEnumerator CurseMarks(PlayerController p)
    {
        Hostile.Play("shimmer", 0.6f, 0.7f);
        Vector3 center = p.transform.position;
        Vector3[] spots = new Vector3[5];
        spots[0] = center;
        for (int i = 1; i < spots.Length; i++) spots[i] = Hostile.ClampArena(center + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(3f, 7f)));
        foreach (Vector3 s in spots) Hostile.Circle(s, 2.3f, 1.2f, Curse);
        yield return Windup(Curse, 1.2f);
        if (!Alive) yield break;
        foreach (Vector3 s in spots)
        {
            Hostile.HitCircle(s, 2.3f, 18f);
            Hostile.Burst(s, 2.3f, Curse);
        }
        Hostile.Play("boom", 0.6f, 1.3f);
    }

    // 뼈 가시 격자: 플레이어 자리에 + 모양, 이어서 × 모양으로 뼈 가시가 솟음
    IEnumerator BoneSpears(PlayerController p)
    {
        Vector3 c = p.transform.position;
        Color bone = new Color(1f, 0.95f, 0.8f, 0.9f);
        for (int pass = 0; pass < 2 && Alive; pass++)
        {
            float tilt = pass == 0 ? 0f : 45f;
            List<Vector3> spots = new List<Vector3>();
            for (int arm = 0; arm < 2; arm++)
            {
                float a = (tilt + arm * 90f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a));
                Hostile.Line(c - d * 9f, c + d * 9f, 1.5f, 0.8f, bone);
                for (float k = -8f; k <= 8f; k += 1.6f) spots.Add(c + d * k);
            }
            if (pass == 0) yield return Windup(bone, 0.8f);
            else yield return new WaitForSeconds(0.8f);
            if (!Alive) yield break;
            foreach (Vector3 at in spots) Fx.Play("fx_spike", at + Vector3.up * 0.6f, 2.2f, new Color(1f, 0.97f, 0.88f), 20f);
            PlayerController pl = Hostile.Player;
            if (pl != null)
                foreach (Vector3 at in spots)
                    if (Vector2.Distance(pl.transform.position, at) < 1f) { pl.TryHit(16f); break; }
            Hostile.Play("crack", 0.7f, 1.1f);
            Hostile.Shake(0.12f);
        }
    }

    // 특수: 망자의 의식
    // 1) 떠오르며 룬 고리를 그리고 영혼을 빨아들임  2) 영혼 등불 넷이 돌며 나선 탄막
    // 3) 등불이 모여들며 경고 원이 차오르고 대폭발 + 사방 탄막
    IEnumerator DeathVortex()
    {
        Vector3 home = transform.position;
        Color cyan = Soul;
        Color violet = Curse;

        // ---------------- 1. 의식 준비 (1.4초)
        LineRenderer runeIn = Hostile.NewLine("RuneRing", violet, 0.14f, 2);
        LineRenderer runeOut = Hostile.NewLine("RuneRing", cyan, 0.1f, 2);
        GameObject aura = Hostile.Glow != null ? SpecialAbilities.MakeSprite("LichAura", Hostile.Glow, home, 0.2f, new Color(0.6f, 0.35f, 1f, 0.5f), "Effect", 1) : null;
        Hostile.Play("shimmer", 0.9f, 0.5f);
        Hostile.Play("pulse", 0.8f, 0.6f);
        float spin = 0f;
        for (float t = 0f; t < 1.4f && Alive; t += Time.deltaTime)
        {
            float k = t / 1.4f;
            spin += 90f * Time.deltaTime;
            DrawRunes(runeIn, runeOut, home, 3.2f * k, 5.2f * k, spin, 0.9f);
            transform.position = home + Vector3.up * Mathf.Sin(k * Mathf.PI * 0.5f) * 0.8f;
            if (aura != null) aura.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.1f, k);
            if (Random.value < 0.5f) SoulWisp.Spawn(home + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(7f, 11f)), home, Random.value < 0.5f ? cyan : violet);
            sr.color = Color.Lerp(Color.white, violet, Mathf.PingPong(Time.time * 8f, 1f));
            yield return null;
        }

        // ---------------- 2. 영혼 등불 (5초)
        Sprite lanternSprite = SpecialAbilities.SwirlSprite != null ? SpecialAbilities.SwirlSprite : Hostile.Glow;
        GameObject[] lanterns = new GameObject[4];
        for (int i = 0; i < lanterns.Length; i++)
            lanterns[i] = SpecialAbilities.MakeSprite("SoulLantern", lanternSprite, home, 0.55f, i % 2 == 0 ? cyan : violet, "Effect", 11);
        Hostile.Play("chime", 0.8f, 0.7f);

        float orbit = Random.Range(0f, 360f);
        float dirSign = Random.value < 0.5f ? 1f : -1f;
        // 등불을 잇는 회전 레이저 (보스 → 등불 → 바깥)
        FxAnim[] beams = new FxAnim[lanterns.Length];
        float beamHit = 0f;
        for (float t = 0f; t < 5f && Alive; t += Time.deltaTime)
        {
            orbit += 55f * dirSign * Time.deltaTime;
            spin += 60f * Time.deltaTime;
            DrawRunes(runeIn, runeOut, home, 3.2f, 5.2f, spin, 0.6f + 0.3f * Mathf.Sin(Time.time * 6f));
            transform.position = home + Vector3.up * (0.8f + Mathf.Sin(Time.time * 2.5f) * 0.25f);
            if (aura != null) aura.transform.localScale = Vector3.one * (1.1f + Mathf.Sin(Time.time * 5f) * 0.1f);
            sr.color = Color.Lerp(Color.white, violet, 0.35f + 0.25f * Mathf.Sin(Time.time * 6f));

            for (int i = 0; i < lanterns.Length; i++)
            {
                float a = (orbit + i * 90f) * Mathf.Deg2Rad;
                lanterns[i].transform.position = home + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 4.5f;
                lanterns[i].transform.Rotate(0f, 0f, -360f * Time.deltaTime);
                lanterns[i].transform.localScale = Vector3.one * (0.55f + Mathf.Sin(Time.time * 9f + i) * 0.06f);
            }

            // 등불마다 보스에서 뻗어 나가는 레이저 (처음 0.8초는 가는 경고선)
            bool live = t > 0.8f;
            for (int i = 0; i < lanterns.Length; i++)
            {
                Vector3 dir = (lanterns[i].transform.position - home).normalized;
                Vector3 end = home + dir * 13f;
                if (beams[i] != null) Destroy(beams[i].gameObject);
                beams[i] = Fx.Beam(home, end, live ? 1.3f : 0.25f, live ? (i % 2 == 0 ? cyan : violet) : new Color(1f, 1f, 1f, 0.5f), 0.1f);
            }
            if (live)
            {
                PlayerController pl = Hostile.Player;
                beamHit -= Time.deltaTime;
                if (pl != null && beamHit <= 0f)
                    for (int i = 0; i < lanterns.Length; i++)
                        if (Hostile.DistanceToSegment(pl.transform.position, home, home + (lanterns[i].transform.position - home).normalized * 13f) < 0.8f)
                        {
                            pl.TryHit(14f);
                            beamHit = 0.3f;
                            break;
                        }
            }
            yield return null;
        }

        // ---------------- 3. 대폭발 (등불이 모여들고 원이 차오름)
        const float blastRadius = 7f;
        if (Alive) Hostile.Circle(home, blastRadius, 1.1f, violet);
        Hostile.Play("hum", 0.8f, 0.6f);
        Vector3[] from = new Vector3[lanterns.Length];
        for (int i = 0; i < lanterns.Length; i++) from[i] = lanterns[i].transform.position;
        for (float t = 0f; t < 1.1f && Alive; t += Time.deltaTime)
        {
            float k = t / 1.1f;
            for (int i = 0; i < lanterns.Length; i++)
            {
                lanterns[i].transform.position = Vector3.Lerp(from[i], home, k * k);
                lanterns[i].transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0.9f, k);
            }
            DrawRunes(runeIn, runeOut, home, 3.2f * (1f - k * 0.7f), 5.2f * (1f - k * 0.7f), spin += 240f * Time.deltaTime, 1f);
            sr.color = Color.Lerp(Color.white, Color.white * 0.4f + violet * 0.6f, Mathf.PingPong(Time.time * (6f + k * 20f), 1f));
            yield return null;
        }

        foreach (GameObject l in lanterns) if (l != null) Destroy(l);
        foreach (FxAnim b in beams) if (b != null) Destroy(b.gameObject);
        Destroy(runeIn.gameObject);
        Destroy(runeOut.gameObject);
        if (aura != null) Destroy(aura);
        transform.position = home;
        if (!Alive) yield break;

        Hostile.HitCircle(home, blastRadius, 26f);
        Hostile.Burst(home, blastRadius, violet);
        ShockRing.Spawn(home, 1f, blastRadius * 1.6f, 0.6f, cyan, 0.5f);
        ShockRing.Spawn(home, 0.5f, blastRadius * 1.1f, 0.45f, Color.white, 0.3f);
        Fx.Play("fx_soulburst", home, blastRadius * 2.4f, Color.white, 14f);
        // 폭발 뒤 바깥 고리에서 가시가 솟음
        for (int i = 0; i < 20; i++)
        {
            float a = i * 18f * Mathf.Deg2Rad;
            Vector3 at = home + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (blastRadius + 1.5f);
            Fx.Play("fx_spike", at + Vector3.up * 0.7f, 2.6f, i % 2 == 0 ? cyan : violet, 16f);
        }
        for (int i = 0; i < 16; i++) SoulWisp.Spawn(home, home + (Vector3)(Random.insideUnitCircle.normalized * 9f), Random.value < 0.5f ? cyan : violet, true);
        Hostile.Play("boom", 1f, 0.6f);
        Hostile.Play("chime", 0.7f, 0.5f);
        Hostile.Shake(0.5f);
    }

    // 두 겹의 룬 고리: 안쪽은 끊긴 점선처럼, 바깥은 반대로 회전
    static void DrawRunes(LineRenderer inner, LineRenderer outer, Vector3 center, float r1, float r2, float spin, float alpha)
    {
        const int seg = 60;
        inner.positionCount = seg + 1;
        outer.positionCount = seg + 1;
        for (int i = 0; i <= seg; i++)
        {
            float k = i / (float)seg * Mathf.PI * 2f;
            // 안쪽 고리는 여섯 번 출렁이는 룬 모양
            float wobble = 1f + 0.08f * Mathf.Sin(k * 6f + spin * Mathf.Deg2Rad * 2f);
            float a1 = k + spin * Mathf.Deg2Rad;
            float a2 = k - spin * Mathf.Deg2Rad * 0.6f;
            inner.SetPosition(i, center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * r1 * wobble);
            outer.SetPosition(i, center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * r2);
        }
        inner.startColor = inner.endColor = new Color(0.75f, 0.4f, 1f, alpha);
        outer.startColor = outer.endColor = new Color(0.55f, 0.95f, 1f, alpha * 0.8f);
    }

    // ================================================================= 킹 슬라임 (분열할수록 작고 빠름)
    static readonly Color Acid = new Color(0.55f, 1f, 0.35f, 0.9f);
    float SlimeSize => Mathf.Max(0.4f, transform.localScale.x / 27f);

    // 대점프: 떨어질 곳에 그림자 원 → 높이 뛰어올라 내려찍고 산성 웅덩이
    IEnumerator SlimeLeap(PlayerController p, float warn)
    {
        float r = 3.6f * SlimeSize;
        Vector3 target = Hostile.ClampArena(p.transform.position);
        Hostile.Circle(target, r, warn + 0.35f, Acid);
        yield return Windup(Acid, warn * 0.6f);
        if (!Alive) yield break;

        Vector3 start = transform.position;
        Hostile.Play("whoosh", 0.7f, 0.6f);
        Fx.Play("fx_puddle", start, 3f * SlimeSize, Acid, 14f);
        // 위로 솟구침
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.position = start + Vector3.up * 14f * (t / 0.3f);
            yield return null;
        }
        yield return new WaitForSeconds(Mathf.Max(0f, warn * 0.4f - 0.2f));
        // 떨어짐
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(target + Vector3.up * 14f, target, t / 0.25f);
            yield return null;
        }
        transform.position = target;
        if (!Alive) yield break;
        Hostile.HitCircle(target, r, 30f);
        Fx.Play("fx_shock", target, r * 2.6f, Acid, 18f);
        Fx.Play("fx_puddle", target, r * 1.3f, Acid, 12f);
        HazardZone.Spawn(target, r * 0.8f, 3f, 10f, Acid, "fx_puddle", 0.7f);
        Hostile.Play("thump", 1f, 0.6f);
        Hostile.Shake(0.35f * SlimeSize + 0.1f);
    }

    // 산성 비: 플레이어 주변 여러 곳에 산성 덩어리가 떨어져 웅덩이
    IEnumerator AcidRain(PlayerController p)
    {
        yield return Windup(Acid, 0.5f);
        Vector3 c = p.transform.position;
        int n = 6;
        Vector3[] spots = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            spots[i] = Hostile.ClampArena(i == 0 ? c : c + (Vector3)(Random.insideUnitCircle * 7f));
            Hostile.Circle(spots[i], 2.2f, 1.1f, Acid);
        }
        Hostile.Play("hiss", 0.6f, 0.8f);
        yield return new WaitForSeconds(1.1f);
        if (!Alive) yield break;
        foreach (Vector3 at in spots)
        {
            Hostile.HitCircle(at, 2.2f, 14f);
            Fx.Play("fx_cloud", at, 4f, Acid, 16f);
            HazardZone.Spawn(at, 1.8f, 3.5f, 8f, Acid, "fx_puddle", 0.75f);
        }
        Hostile.Play("boom", 0.4f, 1.4f);
    }

    // 구르기 돌진: 경로를 보여준 뒤 굴러가며 산성 자국을 남김
    IEnumerator SlimeRoll(PlayerController p)
    {
        Vector3 start = transform.position;
        Vector3 end = Hostile.ClampArena(start + (Vector3)(DirTo(p) * 20f));
        Hostile.Line(start, end, 3.5f * SlimeSize + 1f, 0.8f, Acid);
        yield return Windup(Acid, 0.8f);
        if (!Alive) yield break;
        bool hit = false;
        float trail = 0f;
        Hostile.Play("whoosh", 0.8f, 0.5f);
        for (float t = 0f; t < 0.55f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            transform.position = Vector3.Lerp(start, end, t / 0.55f);
            transform.Rotate(0f, 0f, -900f * Time.deltaTime);
            trail += Time.deltaTime;
            if (trail > 0.08f)
            {
                trail = 0f;
                HazardZone.Spawn(transform.position, 1.4f, 2.5f, 8f, Acid, "fx_puddle", 0.8f);
            }
            if (!hit && Vector2.Distance(transform.position, p.transform.position) < 2.6f * SlimeSize + 0.6f) hit = p.TryHit(28f);
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        Fx.Play("fx_shock", transform.position, 6f * SlimeSize, Acid, 18f);
    }

    // 특수: 슬라임 폭우 - 빠른 대점프 3연속
    IEnumerator SlimeFrenzy()
    {
        for (int i = 0; i < 3 && Alive; i++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            yield return StartCoroutine(SlimeLeap(p, 0.6f));
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ================================================================= 지옥의 군주
    // 화염 돌진: 굵은 경로를 보여준 뒤 돌진
    IEnumerator FlameCharge(PlayerController p)
    {
        Vector3 start = transform.position;
        Vector3 end = Hostile.ClampArena(start + (Vector3)(DirTo(p) * 18f));
        Hostile.Line(start, end, 3.2f, 0.9f, Fire);
        yield return Windup(Fire, 0.9f);
        if (!Alive) yield break;

        Hostile.Play("whoosh", 0.8f, 0.6f);
        bool hit = false;
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            transform.position = Vector3.Lerp(start, end, t / 0.45f);
            if (Hostile.Glow != null)
                FlameParticle.Spawn(Hostile.Glow, transform.position + (Vector3)Random.insideUnitCircle, Random.insideUnitCircle * 2f, 0.5f, 0.08f, 0.35f, false);
            if (Random.value < 0.3f) Fx.Play("fx_explosion", transform.position + (Vector3)(Random.insideUnitCircle * 1.2f), 2.2f, Color.white, 20f);
            if (!hit && Vector2.Distance(transform.position, p.transform.position) < 2.4f) hit = p.TryHit(35f);
            yield return null;
        }
        Hostile.Burst(transform.position, 3f, Fire, true);
        Fx.Play("fx_shock", transform.position, 7f, new Color(1f, 0.55f, 0.2f), 18f);
        Hostile.Shake(0.3f);
    }

    // 운석 낙하: 플레이어 주변에 차례로 떨어지는 운석
    IEnumerator MeteorRain(PlayerController p)
    {
        yield return Windup(Fire, 0.5f);
        for (int i = 0; i < 7 && Alive; i++)
        {
            Vector3 spot = i == 0 ? p.transform.position : p.transform.position + (Vector3)(Random.insideUnitCircle * 7f);
            StartCoroutine(Meteor(Hostile.ClampArena(spot)));
            yield return new WaitForSeconds(0.22f);
        }
        yield return new WaitForSeconds(1.2f);
    }

    IEnumerator Meteor(Vector3 spot)
    {
        const float warn = 1.3f;
        const float r = 2.6f;
        Hostile.Circle(spot, r, warn, Fire);
        yield return new WaitForSeconds(warn - 0.4f);
        FxAnim meteor = Fx.Play("fx_meteor", spot + Vector3.up * 10f, 2.6f, Color.white, 14f, 0f, 12, true, 0.5f);
        GameObject rock = meteor != null ? meteor.gameObject : null;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (rock != null)
            {
                rock.transform.position = Vector3.Lerp(spot + Vector3.up * 10f, spot, t / 0.4f);
                FlameParticle.Spawn(Hostile.Glow, rock.transform.position, Vector2.up * 3f + Random.insideUnitCircle, 0.3f, 0.05f, 0.25f, false);
            }
            yield return null;
        }
        if (rock != null) Destroy(rock);
        Hostile.HitCircle(spot, r, 28f);
        Fx.Play("fx_explosion", spot, r * 2.6f, Color.white, 16f);
        Hostile.Burst(spot, r, Fire, true);
        Hostile.Play("boom", 0.6f, 0.9f);
        Hostile.Shake(0.15f);
    }

    // 화염 파동: 틈이 있는 불의 고리가 퍼져 나감 (틈으로 피함)
    IEnumerator FireWave()
    {
        const int gaps = 3;
        const float gapSize = 40f;
        float gapStart = Random.Range(0f, 360f);
        float[] gapAt = new float[gaps];
        for (int i = 0; i < gaps; i++) gapAt[i] = gapStart + i * 360f / gaps;

        // 경고: 불이 지나갈 부분을 보여줌 (틈은 비어 있음)
        LineRenderer[] warn = Arcs(gapAt, gapSize, 3f, new Color(1f, 0.45f, 0.12f, 0.6f), 0.25f);
        yield return Windup(Fire, 1f);
        foreach (LineRenderer l in warn) Destroy(l.gameObject);
        if (!Alive) yield break;

        Hostile.Play("boom", 0.8f, 0.6f);
        Fx.Play("fx_explosion", transform.position, 5f, Color.white, 16f);
        LineRenderer[] wave = Arcs(gapAt, gapSize, 1f, Fire, 1.2f);
        bool hit = false;
        Vector3 center = transform.position;
        for (float radius = 1f; radius < 22f; radius += 10f * Time.deltaTime)
        {
            for (int i = 0; i < gaps; i++)
                Hostile.SetArc(wave[i], center, radius, gapAt[i] + gapSize * 0.5f, gapAt[i] + 360f / gaps - gapSize * 0.5f);

            PlayerController p = Hostile.Player;
            if (!hit && p != null)
            {
                Vector2 to = p.transform.position - center;
                if (Mathf.Abs(to.magnitude - radius) < 0.9f && !InGap(Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg, gapAt, gapSize))
                    hit = p.TryHit(30f);
            }
            yield return null;
        }
        foreach (LineRenderer l in wave) Destroy(l.gameObject);
    }

    LineRenderer[] Arcs(float[] gapAt, float gapSize, float radius, Color color, float width)
    {
        LineRenderer[] arcs = new LineRenderer[gapAt.Length];
        for (int i = 0; i < gapAt.Length; i++)
        {
            arcs[i] = Hostile.NewLine("FireArc", color, width, 17);
            Hostile.SetArc(arcs[i], transform.position, radius, gapAt[i] + gapSize * 0.5f, gapAt[i] + 360f / gapAt.Length - gapSize * 0.5f);
        }
        return arcs;
    }

    static bool InGap(float angle, float[] gapAt, float gapSize)
    {
        foreach (float g in gapAt)
            if (Mathf.Abs(Mathf.DeltaAngle(angle, g)) < gapSize * 0.5f) return true;
        return false;
    }

    // 특수: 십자 불길 - 네 줄기 불기둥이 천천히 회전 (5초)
    IEnumerator HellCross()
    {
        float angle = Random.Range(0f, 90f);
        float spin = Random.value < 0.5f ? 32f : -32f;
        const float length = 24f;
        LineRenderer[] beams = new LineRenderer[4];
        for (int i = 0; i < 4; i++) beams[i] = Hostile.NewLine("HellBeam", new Color(1f, 0.45f, 0.12f, 0.35f), 0.2f, 17);

        // 1초 경고: 가는 선
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            if (!Alive) break;
            SetBeams(beams, angle, length);
            if (sr != null) sr.color = Color.Lerp(Color.white, Fire, Mathf.PingPong(Time.time * 6f, 1f));
            yield return null;
        }

        Hostile.Play("flame", 0.9f, 0.7f);
        foreach (LineRenderer b in beams) b.startWidth = b.endWidth = 0.6f;
        FxAnim[] pixelBeams = new FxAnim[4];
        Fx.Play("fx_explosion", transform.position, 5f, Color.white, 16f);
        for (float t = 0f; t < 5f && Alive; t += Time.deltaTime)
        {
            angle += spin * Time.deltaTime;
            SetBeams(beams, angle, length);
            for (int i = 0; i < 4; i++)
            {
                if (pixelBeams[i] != null) Destroy(pixelBeams[i].gameObject);
                pixelBeams[i] = Fx.Beam(beams[i].GetPosition(0), beams[i].GetPosition(1), 1.8f, new Color(1f, 0.55f, 0.2f), 0.1f);
            }
            float flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * 30f);
            foreach (LineRenderer b in beams) b.startColor = b.endColor = new Color(1f, 0.5f * flicker + 0.2f, 0.15f, 0.9f);

            PlayerController p = Hostile.Player;
            if (p != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Hostile.DistanceToSegment(p.transform.position, beams[i].GetPosition(0), beams[i].GetPosition(1)) < 1f)
                    {
                        p.TryHit(25f);
                        break;
                    }
                }
            }
            yield return null;
        }
        foreach (LineRenderer b in beams) Destroy(b.gameObject);
        foreach (FxAnim b in pixelBeams) if (b != null) Destroy(b.gameObject);
    }

    void SetBeams(LineRenderer[] beams, float angle, float length)
    {
        for (int i = 0; i < beams.Length; i++)
        {
            float a = (angle + i * 90f) * Mathf.Deg2Rad;
            beams[i].positionCount = 2;
            beams[i].SetPosition(0, transform.position);
            beams[i].SetPosition(1, transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * length);
        }
    }
}

// 영혼 조각: 한 점으로 빨려 들어가거나(수렴) 퍼져 나가며 사라짐
public class SoulWisp : MonoBehaviour
{
    Vector3 from, to;
    float t, duration;
    SpriteRenderer sr;
    Color color;

    public static void Spawn(Vector3 from, Vector3 to, Color color, bool burst = false)
    {
        if (Hostile.Glow == null) return;
        GameObject go = SpecialAbilities.MakeSprite("SoulWisp", Hostile.Glow, from, 0.07f, color, "Effect", 10);
        SoulWisp w = go.AddComponent<SoulWisp>();
        w.from = from;
        w.to = to;
        w.duration = burst ? Random.Range(0.4f, 0.7f) : Random.Range(0.5f, 0.8f);
        w.sr = go.GetComponent<SpriteRenderer>();
        w.color = color;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float ease = k * k;
        // 살짝 휘어서 날아감
        Vector3 side = Vector3.Cross(to - from, Vector3.forward).normalized * Mathf.Sin(k * Mathf.PI) * 1.2f;
        transform.position = Vector3.Lerp(from, to, ease) + side;
        transform.localScale = Vector3.one * Mathf.Lerp(0.07f, 0.03f, k);
        sr.color = new Color(color.r, color.g, color.b, color.a * Mathf.Sin(k * Mathf.PI));
        if (k >= 1f) Destroy(gameObject);
    }
}

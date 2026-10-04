using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 도적 고유 스킬 · 진화
//   r.smoke 연막 · r.feast 피의 향연 · r.burst 출혈 폭발 · r.ambush 급습 표창 · r.boomerang 회전 표창
//   r.path 궤적 칼날 · r.practice 던지기 연습 · r.overflow 넘치는 기세 · r.knot 그림자 매듭 · r.rain 빈 주머니의 비
public partial class SignatureSkills
{
    static readonly Color Shade = new Color(0.7f, 0.5f, 1f);

    readonly List<SigZone> smokes = new List<SigZone>();
    readonly Dictionary<GameObject, float> bleedSince = new Dictionary<GameObject, float>();
    readonly Dictionary<GameObject, float> knots = new Dictionary<GameObject, float>();
    float ambushUntil;
    GameObject practiceTarget;
    int practiceStacks;
    float practiceAt;
    float acrobatAt, peakAt;
    bool gaugeWasFull;

    // ---------------- 게임 코드가 부르는 도적 훅
    public static void DashStart(Vector3 from) { if (Instance != null && Instance.who == CharacterId.Rogue) Instance.OnDashStart(from); }
    public static void DashEnd(Vector3 from, Vector3 to, HashSet<Collider2D> cut) { if (Instance != null && Instance.who == CharacterId.Rogue) Instance.OnDashEnd(from, to, cut); }

    float RogueRange => CharacterData.Current.range > 0f ? CharacterData.Current.range : 12f;

    void OnDashStart(Vector3 from)
    {
        int s = L("r.smoke");
        if (s <= 0) return;
        smokes.RemoveAll(z => z == null);
        SigZone z = Zone(from, 3.5f, V(s, 2f, 1f), 0.2f, zone =>
        {
            foreach (Collider2D c in Inside(zone))
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null) e.Slow(0.3f, 0.35f);
            }
        }, "fx_cloud", new Color(0.35f, 0.3f, 0.45f, 0.75f), 8.5f);
        smokes.Add(z);
        Fx.Spawn("fx_smoke", from, 5f, new Color(0.5f, 0.45f, 0.6f), 14f);
    }

    void OnDashEnd(Vector3 from, Vector3 to, HashSet<Collider2D> cut)
    {
        if (L("r.ambush") > 0) ambushUntil = Time.time + 2f;
        // 궤적 칼날: 지나간 길에 칼날 자국
        int p = L("r.path");
        if (p > 0 && (to - from).sqrMagnitude > 0.5f)
        {
            float dmg = Atk * 0.5f;
            SigZone z = Zone((from + to) * 0.5f, 1.2f, V(p, 1.5f, 0.5f), 0.3f, null);
            z.segment = true;
            z.a = from;
            z.b = to;
            float shotAt = 0f;
            z.onTick = zone =>
            {
                foreach (Collider2D c in Inside(zone)) Deal(c.gameObject, dmg, Vector3.zero, 0f);
                // 칼날 폭풍길: 자국에서 표창이 솟구쳐 가까운 적에게
                if (E("re.road") && zone.age >= shotAt)
                {
                    shotAt = zone.age + 0.3f;
                    Vector3 at = Vector3.Lerp(zone.a, zone.b, Random.value);
                    List<Transform> near = Nearest(at, 10f, 1);
                    if (near.Count > 0) Shot(at, ((Vector2)(near[0].position - at)).normalized, Atk * 0.8f, 1, 30f, 12f, "fx_shuriken", 0.7f, Color.white, true);
                }
            };
            LineRenderer lr = Hostile.NewLine("BladePath", new Color(0.75f, 0.45f, 1f, 0.6f), 0.5f, 3);
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.transform.SetParent(z.transform, true);
        }
        // 그림자 매듭: 돌진으로 벤 적들을 4초 동안 묶음
        int k = L("r.knot");
        if (k > 0 && cut != null && cut.Count >= 2)
        {
            foreach (Collider2D c in cut)
                if (c != null) knots[c.gameObject] = Time.time + 4f;
            DrawKnots(cut);
        }
    }

    void DrawKnots(HashSet<Collider2D> cut)
    {
        Collider2D prev = null;
        foreach (Collider2D c in cut)
        {
            if (c == null) continue;
            if (prev != null) Fx.Beam(prev.transform.position, c.transform.position, 0.25f, new Color(0.55f, 0.3f, 0.9f, 0.8f), 0.6f, 12);
            prev = c;
        }
    }

    void RogueAttack(Vector3 start, Vector2 dir)
    {
        int b = L("r.boomerang");
        if (b > 0) StartCoroutine(Boomerang(start, dir, Atk * V(b, 0.4f, 0.15f)));
        // 처형자의 길: 급습 시간 동안 꿰뚫는 그림자 표창이 함께
        if (E("re.executioner") && Time.time < ambushUntil)
        {
            Bullet s = Shot(start, dir, Atk * 0.8f, 999, 34f, RogueRange * 1.3f, "fx_shuriken", 0.9f, Shade, true);
            if (s != null) s.hitOnce = new HashSet<int>();
        }
    }

    IEnumerator Boomerang(Vector3 start, Vector2 dir, float dmg)
    {
        yield return new WaitForSeconds(0.35f);
        if (!Alive) yield break;
        Vector3 far = start + (Vector3)(dir * RogueRange * 0.8f);
        Vector2 back = ((Vector2)(transform.position - far)).normalized;
        int n = E("re.return") ? 2 : 1;
        for (int i = 0; i < n; i++)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, (i - (n - 1) * 0.5f) * 10f) * back;
            Bullet b = Shot(far, d, dmg, 2, 26f, RogueRange, "fx_shuriken", 0.7f, new Color(0.85f, 0.8f, 1f), true);
            if (b != null) b.hitOnce = new HashSet<int>();
        }
        if (E("re.return"))
        {
            yield return new WaitForSeconds(RogueRange * 0.8f / 26f);
            if (player != null && player.NowBullet < player.MaxBullet) player.NowBullet++;
        }
    }

    // 표창을 다 던져 주머니가 빔 (장전 시작)
    void RogueEmpty()
    {
        int r = L("r.rain");
        if (r <= 0) return;
        List<Transform> near = Nearest(transform.position, 11f, 6);
        int n = (int)V(r, 4f, 2f);
        for (int i = 0; i < n; i++)
        {
            Vector3 at = near.Count > 0 ? near[i % near.Count].position + (Vector3)(Random.insideUnitCircle * 0.8f)
                                        : transform.position + (Vector3)(Random.insideUnitCircle * 6f);
            StartCoroutine(Drop(at, "fx_shuriken", 1.4f, Color.white, 1.2f, Atk, i * 0.06f));
        }
        Hostile.Play("whoosh", 0.5f, 1.6f);
    }

    float RogueOutgoing(GameObject target)
    {
        float mul = 1f;
        int a = L("r.ambush");
        if (a > 0 && Time.time < ambushUntil) mul *= 1f + V(a, 0.3f, 0.15f);
        int p = L("r.practice");
        if (p > 0 && !Dealing && !DotTick)
        {
            if (target == practiceTarget && Time.time - practiceAt < 2f) practiceStacks = Mathf.Min(5, practiceStacks + 1);
            else { practiceTarget = target; practiceStacks = 0; }
            practiceAt = Time.time;
            mul *= 1f + practiceStacks * V(p, 0.05f, 0.03f);
        }
        return mul;
    }

    void RogueHit(GameObject target, float dmg, bool killed, bool proc)
    {
        Bleed bleed = target.GetComponent<Bleed>();
        // 피의 향연
        if (killed && bleed != null && L("r.feast") > 0) Heal(V(L("r.feast"), 2f, 1f));
        // 출혈 폭발: 출혈이 3초 넘게 이어지면 남은 출혈이 한꺼번에
        int bu = L("r.burst");
        if (bu > 0 && !killed && bleed != null)
        {
            if (!bleedSince.TryGetValue(target, out float since)) bleedSince[target] = Time.time;
            else if (Time.time - since >= 3f)
            {
                bleedSince.Remove(target);
                float burst = bleed.dps * Mathf.Max(0f, bleed.until - Time.time) * V(bu, 1f, 0.3f);
                Vector3 p = target.transform.position;
                Destroy(bleed);
                if (burst > 0f)
                {
                    Deal(target, burst, Vector3.zero, 0f);
                    if (E("re.sea")) { Circle(p, 3f, burst, 0.5f); Heal(3f); }
                    Fx.Spawn("fx_bleed", p, 3.4f, Color.white, 16f);
                    Fx.Spawn("fx_shock", p, 4f, Blood, 22f);
                    Hostile.Play("crack", 0.5f, 0.9f);
                }
            }
        }
        if (bleed == null) bleedSince.Remove(target);
        if (bleedSince.Count > 200) bleedSince.Clear();

        // 그림자 매듭: 묶인 적이 받은 피해를 나머지도
        int k = L("r.knot");
        if (k > 0 && !Dealing && knots.TryGetValue(target, out float until) && until > Time.time)
        {
            float share = dmg * V(k, 0.2f, 0.1f);
            foreach (var kv in new List<KeyValuePair<GameObject, float>>(knots))
            {
                if (kv.Key == null || kv.Key == target || kv.Value < Time.time) continue;
                Deal(kv.Key, share, Vector3.zero, 0f);
            }
            if (killed && E("re.executioner")) ambushUntil = Time.time + 2f;
        }
        if (killed) knots.Remove(target);

        // 칼날 곡예: 던지기 연습이 최대일 때 칼날 고리
        if (proc && E("re.acrobat") && practiceStacks >= 5 && Time.time >= acrobatAt)
        {
            acrobatAt = Time.time + 0.5f;
            Vector3 p = target.transform.position;
            Circle(p, 2.6f, Atk * 1.5f, 0.6f);
            Fx.Spawn("fx_spinslash", p, 5.5f, new Color(0.85f, 0.7f, 1f, 0.9f), 24f);
        }
        // 사냥의 절정: 게이지가 가득 찬 동안 처치하면 표창 비
        if (killed && E("re.peak") && Time.time >= peakAt)
        {
            SkillGauge g = Cache<SkillGauge>.Get;
            if (g != null && g.IsFull())
            {
                peakAt = Time.time + 0.3f;
                Vector3 p = target.transform.position;
                for (int i = 0; i < 4; i++)
                    StartCoroutine(Drop(p + (Vector3)(Random.insideUnitCircle * 2.5f), "fx_shuriken", 1.2f, Color.white, 1.1f, Atk, i * 0.08f));
            }
        }
    }

    // 환영 연막: 연막 안에 있으면 공격이 빗나감
    float RogueTaken(float dmg)
    {
        if (!E("re.phantom")) return dmg;
        foreach (SigZone z in smokes)
            if (z != null && z.Contains(transform.position))
            {
                SpecialAbilities.SharedFx?.FloatText(transform.position, Loc.T("빗나감"), new Color(0.8f, 0.7f, 1f), 3.5f, 0.4f);
                return 0f;
            }
        return dmg;
    }

    void RogueTick()
    {
        // 넘치는 기세: 게이지가 가득 차는 순간 표창이 사방으로
        int o = L("r.overflow");
        SkillGauge g = Cache<SkillGauge>.Get;
        bool full = g != null && g.IsFull();
        if (o > 0 && full && !gaugeWasFull)
        {
            int n = (int)V(o, 8f, 4f);
            Vector3 p = transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i * 360f / n) * Vector2.right;
                Shot(p, d, Atk, 1, 26f, RogueRange, "fx_shuriken", 0.7f, Color.white, true);
            }
            Fx.Spawn("fx_shock", p, 6f, Shade, 20f);
            Hostile.Play("whoosh", 0.6f, 1.4f);
        }
        gaugeWasFull = full;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 궁수 고유 스킬 · 진화
//   a.wind 바람 읽기 · a.breath 집중 호흡 · a.seed 가시 씨앗 · a.ring 울림 화살촉 · a.spread 표식 전염
//   a.pierce 꿰뚫는 시선 · a.retreat 후퇴 사격 · a.ambush 매복 · a.high 높은 자리 · a.meteor 유성 화살
public partial class SignatureSkills
{
    static readonly Color Leaf = new Color(0.6f, 1f, 0.5f);
    static readonly Color Wind = new Color(0.6f, 0.95f, 1f);

    readonly List<(Vector3 a, Vector3 b, float until)> windPaths = new List<(Vector3, Vector3, float)>();
    readonly List<(Vector3 c, float r, float until)> highZones = new List<(Vector3, float, float)>();
    int breath;
    float seedAt;
    Vector3 stillPos;
    float stillFor;
    SigZone trap;

    // ---------------- 게임 코드가 부르는 궁수 훅
    // 화살 하나마다 (split: 분열 화살에서 갈라진 작은 화살)
    public static void Arrow(Bullet b, float draw, bool split = false) { if (Instance != null && b != null && Instance.who == CharacterId.Archer) Instance.OnArrow(b, draw, split); }
    // 시위를 놓은 뒤 (화살을 다 만든 다음)
    public static void BowRelease(Vector3 start, Vector2 dir, float draw) { if (Instance != null && Instance.who == CharacterId.Archer) Instance.OnBowRelease(start, dir, draw); }
    public static void Rain(Vector3 center, float radius) { if (Instance != null && Instance.who == CharacterId.Archer) Instance.OnRain(center, radius); }
    // 폭풍의 길: 바람길 위에서는 순식간에 가득 당김
    public static bool InstantDraw => Instance != null && Instance.E("ae.storm") && Instance.OnWindPath();

    void OnArrow(Bullet b, float draw, bool split)
    {
        bool full = draw >= 1f;
        if (split)
        {
            if (E("ae.bow")) PierceGrow(b);
            return;
        }
        // 집중 호흡: 가득 당긴 화살을 연달아 쏠수록
        int br = L("a.breath");
        if (br > 0 && full && breath > 0)
        {
            b.damage *= 1f + breath * V(br, 0.06f, 0.03f);
            if (E("ae.master") && breath >= 5)
            {
                b.pene = 999;
                b.hitOnce = new HashSet<int>();
                if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.85f, 0.4f);
            }
        }
        if (L("a.pierce") > 0) PierceGrow(b);
        if (L("a.seed") > 0)
        {
            bool seeded = false;
            b.onHitEnemy += (bullet, col) =>
            {
                if (seeded || Time.time < seedAt) return;
                seeded = true;
                seedAt = Time.time + 0.15f;
                ThornSeed(col.transform.position);
            };
        }
        if (L("a.ring") > 0)
            b.onHitEnemy += (bullet, col) => StartCoroutine(Ring(col.transform.position, bullet.damage * V(L("a.ring"), 0.25f, 0.1f), E("ae.echo") ? 2 : 1));
        if (full && L("a.spread") > 0)
            b.onHitEnemy += (bullet, col) =>
            {
                EnermyController e = col.GetComponent<EnermyController>();
                if (e == null || !e.IsDead) return;
                float mul = E("ae.brand") ? 1.4f : 1.2f;
                foreach (Transform t in Nearest(col.transform.position, 7f, L("a.spread"), col.gameObject))
                {
                    Mark(t.gameObject, 4f, mul);
                    Fx.Spawn("fx_reticle", t.position, 1.6f, new Color(1f, 0.45f, 0.4f), 16f);
                }
            };
    }

    // 꿰뚫는 시선: 꿰뚫을 때마다 다음 적에게 더 아프게 (만궁: 다섯 번째에 폭발)
    void PierceGrow(Bullet b)
    {
        int lvP = Mathf.Max(1, L("a.pierce"));
        int count = 0;
        b.onHitEnemy += (bullet, col) =>
        {
            count++;
            bullet.damage *= 1f + V(lvP, 0.15f, 0.07f);
            if (E("ae.bow") && count == 5)
            {
                Vector3 p = col.transform.position;
                Circle(p, 2.6f, bullet.damage, 1f);
                Fx.Spawn("fx_explosion", p, 5f, Color.white, 18f);
                Hostile.Play("boom", 0.4f, 1.3f);
            }
        };
    }

    void ThornSeed(Vector3 at)
    {
        float r = E("ae.forest") ? 1.95f : 1.3f;
        float dmg = Atk * 0.2f;
        Zone(at, r, V(L("a.seed"), 1.5f, 0.5f), 0.3f, zone =>
        {
            foreach (Collider2D c in Inside(zone))
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null) e.Slow(0.7f, 0.35f);
                if (E("ae.forest")) Mark(c.gameObject, 0.4f, 1.2f);
                Deal(c.gameObject, dmg, Vector3.zero, 0f);
            }
        }, "fx_spike", Leaf, r * 1.6f);
    }

    IEnumerator Ring(Vector3 at, float dmg, int times)
    {
        for (int i = 0; i < times; i++)
        {
            yield return new WaitForSeconds(0.4f);
            Circle(at, 1.9f, dmg, 0.3f);
            ShockRing.Spawn(at, 0.2f, 2.2f, 0.3f, new Color(1f, 0.95f, 0.7f, 0.8f), 0.12f);
            if (i == 1)
                foreach (Transform t in Nearest(at, 8f, 3))
                    Shot(at, ((Vector2)(t.position - at)).normalized, dmg, 1, 40f, 9f, "fx_arrow", 0.35f, Color.white);
        }
    }

    void OnBowRelease(Vector3 start, Vector2 dir, float draw)
    {
        bool full = draw >= 1f;
        if (L("a.breath") > 0)
            breath = full ? (E("ae.master") && breath >= 5 ? 0 : Mathf.Min(5, breath + 1)) : 0;
        if (!full) return;
        if (L("a.wind") > 0)
        {
            Vector3 end = start + (Vector3)(dir * 24f);
            windPaths.Add((start, end, Time.time + 3f));
            if (windPaths.Count > 6) windPaths.RemoveAt(0);
            Fx.Beam(start, end, 0.9f, new Color(0.6f, 0.95f, 1f, 0.35f), 3f, 2);
        }
        int rt = L("a.retreat");
        if (rt > 0) StartCoroutine(Retreat(-dir, V(rt, 2f, 0.5f)));
    }

    IEnumerator Retreat(Vector2 dir, float dist)
    {
        Vector3 from = transform.position;
        Vector3 to = from;
        for (float d = 0.25f; d <= dist; d += 0.25f)
        {
            Vector3 p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p) || (Hostile.ClampArena(p) - p).sqrMagnitude > 0.01f) break;
            to = p;
        }
        Fx.Spawn("fx_smoke", from, 2.2f, Wind, 18f);
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, to, t / 0.12f);
            yield return null;
        }
        transform.position = to;
        // 폭풍의 길: 물러난 자리에 돌풍
        if (E("ae.storm"))
        {
            Circle(from, 3f, Atk * 0.6f, 4f);
            Fx.Spawn("fx_tornado", from, 4.5f, new Color(0.8f, 1f, 1f, 0.7f), 18f);
            Hostile.Play("whoosh", 0.5f, 1.1f);
        }
    }

    void OnRain(Vector3 center, float radius)
    {
        int h = L("a.high");
        if (h > 0) highZones.Add((center, radius, Time.time + 2f));
        int m = L("a.meteor");
        if (m > 0) StartCoroutine(Meteors(center, radius, Atk * V(m, 2f, 1f)));
    }

    IEnumerator Meteors(Vector3 center, float radius, float dmg)
    {
        yield return new WaitForSeconds(1.3f);
        if (!Alive) yield break;
        int n = E("ae.sky") ? 3 : 1;
        for (int i = 0; i < n; i++)
        {
            Vector3 at = i == 0 ? center : center + (Vector3)(Random.insideUnitCircle * radius * 0.7f);
            StartCoroutine(Drop(at, "fx_meteor", 4f, new Color(1f, 0.95f, 0.7f), 3f, dmg, i * 0.2f));
            if (E("ae.sky")) StartCoroutine(Starlight(at, i * 0.2f + 0.3f));
        }
        Hostile.Play("bigboom", 0.6f, 1.2f);
        Hostile.Shake(0.2f);
    }

    IEnumerator Starlight(Vector3 at, float delay)
    {
        yield return new WaitForSeconds(delay);
        float dmg = Atk * 0.3f;
        Zone(at, 2.5f, 3f, 0.3f, zone =>
        {
            foreach (Collider2D c in Inside(zone)) { Deal(c.gameObject, dmg, Vector3.zero, 0f); Burn.Apply(c.gameObject, Atk * 0.2f, 1f); }
        }, "fx_rune", new Color(1f, 0.9f, 0.5f, 0.6f), 5.5f);
    }

    bool OnWindPath()
    {
        Vector3 p = transform.position;
        foreach (var w in windPaths)
            if (w.until > Time.time && Hostile.DistanceToSegment(p, w.a, w.b) < 1.3f) return true;
        return false;
    }

    float ArcherMove()
    {
        int w = L("a.wind");
        return w > 0 && OnWindPath() ? 1f + V(w, 0.15f, 0.07f) : 1f;
    }

    float ArcherOutgoing(GameObject target)
    {
        float mul = MarkMul(target);
        int h = L("a.high");
        if (h > 0)
            foreach (var z in highZones)
                if (z.until > Time.time && (target.transform.position - z.c).sqrMagnitude <= z.r * z.r) { mul *= 1f + V(h, 0.15f, 0.1f); break; }
        return mul;
    }

    void ArcherHit(GameObject target, float dmg, bool killed, bool proc)
    {
        // 사냥꾼의 낙인: 표식이 붙은 적이 쓰러지면 폭발
        if (killed && E("ae.brand") && Marked(target))
        {
            Vector3 p = target.transform.position;
            marks.Remove(target);
            Circle(p, 2.6f, Atk * 1.2f, 1f);
            Fx.Spawn("fx_explosion", p, 4.5f, new Color(1f, 0.6f, 0.5f), 18f);
        }
    }

    void ArcherTick()
    {
        windPaths.RemoveAll(w => w.until < Time.time);
        highZones.RemoveAll(z => z.until < Time.time);
        // 매복: 2초 동안 가만히 있으면 발밑에 덫
        int am = L("a.ambush");
        if (am <= 0) return;
        if ((transform.position - stillPos).sqrMagnitude > 0.01f) { stillPos = transform.position; stillFor = 0f; return; }
        stillFor += Time.deltaTime;
        if (stillFor < 2f || trap != null) return;
        float hold = V(am, 1f, 0.5f);
        HashSet<GameObject> caught = new HashSet<GameObject>();
        trap = Zone(transform.position, 2.2f, 8f, 0.1f, zone =>
        {
            foreach (Collider2D c in Inside(zone))
            {
                if (!caught.Add(c.gameObject)) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null) e.Slow(0f, hold);
                if (E("ae.master")) Mark(c.gameObject, hold, 2f);
                Fx.Spawn("fx_net", c.transform.position, 2.2f, Color.white, 16f);
            }
        }, "fx_net", new Color(0.85f, 0.8f, 0.6f, 0.6f), 4.6f);
        Hostile.Play("clank", 0.4f, 0.9f);
    }
}

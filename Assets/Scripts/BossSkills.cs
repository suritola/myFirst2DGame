using System.Collections;
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
            string name = kind == 0 ? "리치 왕이 죽음의 소용돌이를 부른다!" : "지옥의 군주가 십자 불길을 내뿜는다!";
            if (StageManager.Instance != null) StageManager.Instance.ShowBanner(name, 2f);
            StartCoroutine(Run(kind == 0 ? DeathVortex() : HellCross()));
            return;
        }

        if (Time.time < next) return;
        next = Time.time + (boss.Enraged ? 3.2f : 4.5f);
        step = (step + 1) % 3;
        IEnumerator skill = kind == 0
            ? (step == 0 ? SoulVolley() : step == 1 ? CurseMarks(p) : BoneSpears(p))
            : (step == 0 ? FlameCharge(p) : step == 1 ? MeteorRain(p) : FireWave());
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
    // 영혼 탄막: 사방으로 두 번 퍼지는 영혼탄 (틈 사이로 피함)
    IEnumerator SoulVolley()
    {
        Hostile.Circle(transform.position, 3f, 0.8f, Soul);
        yield return Windup(Soul, 0.8f);
        for (int wave = 0; wave < 2 && Alive; wave++)
        {
            float offset = wave * 11.25f + Random.Range(0f, 10f);
            for (int i = 0; i < 16; i++)
            {
                float a = (offset + i * 22.5f) * Mathf.Deg2Rad;
                Hostile.Shoot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 9f, 15f, 0.55f, Soul, 0.14f, 5f);
            }
            Hostile.Play("whoosh", 0.5f, 1.3f);
            yield return new WaitForSeconds(0.55f);
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

    // 뼈 창: 세 갈래 조준선 → 빠른 뼈 창
    IEnumerator BoneSpears(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        Vector2[] dirs = { Quaternion.Euler(0, 0, -16f) * dir, dir, Quaternion.Euler(0, 0, 16f) * dir };
        foreach (Vector2 d in dirs) Hostile.Line(transform.position, transform.position + (Vector3)(d * 24f), 1f, 0.75f, new Color(1f, 0.95f, 0.8f, 0.8f));
        yield return Windup(new Color(1f, 0.95f, 0.8f), 0.75f);
        if (!Alive) yield break;
        foreach (Vector2 d in dirs)
        {
            HostileProjectile b = Hostile.Shoot(transform.position, d, 24f, 16f, 0.6f, new Color(0.95f, 0.92f, 0.8f), 0.2f, 2f);
            b.transform.localScale = new Vector3(0.4f, 0.1f, 1f);
            b.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
        Hostile.Play("crack", 0.6f, 1.3f);
    }

    // 특수: 죽음의 소용돌이 - 회전하는 세 줄기 영혼탄 (5초)
    IEnumerator DeathVortex()
    {
        Hostile.Circle(transform.position, 4f, 1f, Curse);
        yield return Windup(Curse, 1f);
        float angle = Random.Range(0f, 360f);
        float spin = Random.value < 0.5f ? 70f : -70f;
        for (float t = 0f; t < 5f && Alive; t += 0.12f)
        {
            for (int arm = 0; arm < 3; arm++)
            {
                float a = (angle + arm * 120f) * Mathf.Deg2Rad;
                Hostile.Shoot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 8f, 14f, 0.5f, Curse, 0.13f, 5f);
            }
            angle += spin * 0.12f;
            if (Mathf.Repeat(t, 0.6f) < 0.12f) Hostile.Play("whoosh", 0.25f, 1.6f);
            yield return new WaitForSeconds(0.12f);
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
            if (!hit && Vector2.Distance(transform.position, p.transform.position) < 2.4f) hit = p.TryHit(35f);
            yield return null;
        }
        Hostile.Burst(transform.position, 3f, Fire, true);
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
        GameObject rock = Hostile.Glow != null ? SpecialAbilities.MakeSprite("Meteor", Hostile.Glow, spot + Vector3.up * 10f, 0.25f, Fire, "Effect", 12) : null;
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
        foreach (LineRenderer b in beams) b.startWidth = b.endWidth = 1.6f;
        for (float t = 0f; t < 5f && Alive; t += Time.deltaTime)
        {
            angle += spin * Time.deltaTime;
            SetBeams(beams, angle, length);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스 스킬: 스킬 3개를 번갈아 쓰고, 체력이 절반 아래면 특수 스킬도 씀
// 모든 공격은 경고를 먼저 보여줘서 움직이면 피할 수 있음
// kind 0 = 리치 왕 (1장), 1 = 지옥의 군주 (2장), 2 = 킹 슬라임 (3장), 3 = 거울의 군주 (4장 · 2.1.1)
public class BossSkills : MonoBehaviour
{
    public int kind;
    // 킹 슬라임 대점프로 공중에 떠 있는 동안 (이때 쓰러지면 bosss.Split 이 떨어질 자리에서 갈라지게)
    [HideInInspector] public bool airborne;
    [HideInInspector] public Vector3 landing;

    // 보스 공격 피해 배율 (체력을 줄인 대신 공격을 세게)
    const float Power = 1.25f;

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

    // 스킬이 만든 오래 가는 표시 (룬 고리 · 등불 · 불의 고리 · 빔 선)
    // 보스가 쓰러져 1초 뒤 오브젝트가 지워지면 스킬 코루틴이 중간에 끊겨 정리 코드가 돌지 않으므로, 꺼질 때 한꺼번에 지움
    readonly List<GameObject> owned = new List<GameObject>();
    T Own<T>(T c) where T : Component { if (c != null) owned.Add(c.gameObject); return c; }
    GameObject Own(GameObject g) { if (g != null) owned.Add(g); return g; }

    void OnDisable()
    {
        foreach (GameObject g in owned) if (g != null) Destroy(g);
        owned.Clear();
    }

    void Start()
    {
        boss = GetComponent<bosss>();
        sr = GetComponent<SpriteRenderer>();
        next = Time.time + 3f;
    }

    void Update()
    {
        if (busy || !Alive || BossUltimate.Active) return;      // 필살기 결계 동안은 결계 공격만
        PlayerController p = Hostile.Player;
        if (p == null) return;

        // 체력 절반 아래: 특수 스킬 (처음 한 번은 바로)
        if (boss.Enraged && Time.time >= nextSpecial)
        {
            nextSpecial = Time.time + 12f * GameMode.SkillCooldownMul;
            if (kind == AbyssStage.MirrorKind)
            {
                // 되비친 기억: 이번 판에 가장 많이 쓴 공격 (RunStats)
                string top = TopSource();
                if (StageManager.Instance != null)
                    StageManager.Instance.ShowBanner(Loc.T("거울의 군주가 「{0}」을(를) 되비춘다!").Replace("{0}", RunStats.SourceName(top)), 2.2f);
                StartCoroutine(Run(ReflectedMemory(p, top)));
                return;
            }
            string name = kind == 0 ? Loc.T("리치 왕이 망자의 의식을 시작한다!") : kind == 1 ? Loc.T("지옥의 군주가 십자 불길을 내뿜는다!") : Loc.T("킹 슬라임이 미친 듯이 뛰어오른다!");
            if (StageManager.Instance != null) StageManager.Instance.ShowBanner(name, 2f);
            StartCoroutine(Run(kind == 0 ? DeathVortex() : kind == 1 ? HellCross() : SlimeFrenzy()));
            return;
        }

        if (Time.time < next) return;
        next = Time.time + (boss.Enraged ? 2.6f : 3.6f) * GameMode.SkillCooldownMul;
        step = (step + 1) % 5;
        IEnumerator skill = kind == AbyssStage.MirrorKind
            ? (step == 0 ? MirrorStrike(3) : step == 1 ? MirrorClones(p) : step == 2 ? BrandCross(p) : step == 3 ? ShardRings() : MirrorStrike(boss.Enraged ? 5 : 4))
            : kind == 0
            ? (step == 0 ? SoulVolley() : step == 1 ? CurseMarks(p) : step == 2 ? BoneSpears(p) : step == 3 ? SoulChains(p) : GraspOfDead(p))
            : kind == 1
            ? (step == 0 ? FlameCharge(p) : step == 1 ? MeteorRain(p) : step == 2 ? FireWave() : step == 3 ? HellfirePillars(p) : LavaFissure(p))
            : (step == 0 ? SlimeLeap(p, 1f) : step == 1 ? AcidRain(p) : step == 2 ? SlimeRoll(p) : step == 3 ? AcidGeysers(p) : SlimeVortex(p));
        StartCoroutine(Run(skill));
    }

    IEnumerator Run(IEnumerator routine)
    {
        busy = true;
        boss.casting = true;
        yield return StartCoroutine(routine);
        if (boss != null) boss.casting = false;
        if (sr != null && Alive) sr.color = Color.white;
        owned.RemoveAll(g => g == null);
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

    // ================================================================= 거울의 군주 (4장 · 2.1.1)
    static readonly Color Mirror = new Color(0.78f, 0.9f, 1f, 0.95f);

    static Vector2 Rotate(Vector2 v, float deg)
    {
        float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    // 거울 공격: 지금 캐릭터의 평타를 흉내 냄 (거너 연사 · 검사 돌진 베기 · 도적 표창 부채 · 궁수 관통 화살 · 연금술사 플라스크)
    IEnumerator MirrorStrike(int rounds)
    {
        CharacterId who = CharacterData.Selected;
        yield return Windup(Mirror, 0.5f);
        for (int r = 0; r < rounds && Alive; r++)
        {
            PlayerController p = Hostile.Player;
            if (p == null) yield break;
            Vector2 d = DirTo(p);
            Vector3 c = transform.position;
            switch (who)
            {
                case CharacterId.Swordsman:
                    {
                        Vector3 at = Hostile.ClampArena(c + (Vector3)(d * 4.5f));
                        if (Hostile.IsWall(at)) at = c;
                        Hostile.Circle(at, 3.6f, 0.6f, Mirror);
                        yield return new WaitForSeconds(0.6f);
                        if (!Alive) yield break;
                        transform.position = at;
                        Fx.Spawn("fx_slash", at, 8f, new Color(0.85f, 0.95f, 1f), 30f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 14);
                        Hostile.HitCircle(at, 3.6f, 22f * Power);
                        Hostile.Play("whoosh", 0.7f, 0.8f);
                        break;
                    }
                case CharacterId.Rogue:
                    for (int i = -3; i <= 3; i++) Hostile.Shoot(c, Rotate(d, i * 12f), 12f, 14f * Power, 0.5f, Mirror, 1.3f, 3f);
                    Hostile.Play("whoosh", 0.5f, 1.4f);
                    yield return new WaitForSeconds(0.55f);
                    break;
                case CharacterId.Archer:
                    {
                        Hostile.Line(c, c + (Vector3)(d * 30f), 1.2f, 0.65f, Mirror);
                        yield return new WaitForSeconds(0.65f);
                        if (!Alive) yield break;
                        Hostile.Shoot(transform.position, d, 28f, 24f * Power, 0.7f, Mirror, 2f, 2f);
                        Hostile.Play("zap", 0.5f, 1.3f);
                        yield return new WaitForSeconds(0.2f);
                        break;
                    }
                case CharacterId.Alchemist:
                    {
                        Vector3 at = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle * 2f));
                        Hostile.Circle(at, 2.6f, 0.8f, Mirror);
                        yield return new WaitForSeconds(0.8f);
                        if (!Alive) yield break;
                        Fx.Spawn("fx_explosion", at, 5.5f, Mirror, 18f);
                        Hostile.HitCircle(at, 2.6f, 20f * Power);
                        HazardZone.Spawn(at, 2.2f, 3f, 6f * Power, new Color(0.7f, 0.85f, 1f, 0.7f), "fx_puddle", 0.7f);
                        Hostile.Play("boom", 0.5f, 1.2f);
                        break;
                    }
                default:    // 거너: 세 발 연사
                    for (int i = 0; i < 3 && Alive; i++)
                    {
                        Hostile.Shoot(transform.position, DirTo(p), 18f, 15f * Power, 0.5f, Mirror, 1.4f, 3f);
                        Hostile.Play("zap", 0.35f, 1.6f);
                        yield return new WaitForSeconds(0.12f);
                    }
                    break;
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 분신술: 플레이어 둘레 세 자리에 거울상이 나타나고 그중 진짜(보스)만 맞음. 모두 거울 조각을 쏜 뒤 가짜는 깨짐
    IEnumerator MirrorClones(PlayerController p)
    {
        Vector3 center = p.transform.position;
        float start = Random.Range(0f, 360f);
        int real = Random.Range(0, 3);
        List<GameObject> fakes = new List<GameObject>();
        Vector3[] spots = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            float a = (start + i * 120f) * Mathf.Deg2Rad;
            spots[i] = Hostile.ClampArena(center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 8f);
            if (Hostile.IsWall(spots[i])) spots[i] = Hostile.ClampArena(center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 4f);
        }
        Fx.Spawn("fx_soulburst", transform.position, 9f, Mirror, 16f);
        Hostile.Play("shimmer", 0.6f, 0.8f);
        for (int i = 0; i < 3; i++)
        {
            Fx.Spawn("fx_shock", spots[i], 6f, Mirror, 20f);
            if (i == real) { transform.position = spots[i]; continue; }
            GameObject g = Own(new GameObject("MirrorClone"));
            g.transform.position = spots[i];
            g.transform.localScale = transform.localScale;
            SpriteRenderer s = g.AddComponent<SpriteRenderer>();
            s.sprite = sr.sprite;
            s.sortingLayerID = sr.sortingLayerID;
            s.sortingOrder = sr.sortingOrder;
            s.flipX = spots[i].x > center.x;
            s.color = new Color(0.85f, 0.92f, 1f, 0.9f);
            AbyssBody body = g.AddComponent<AbyssBody>();
            body.frames = AbyssStage.Frames("mirrorlord");
            fakes.Add(g);
        }
        yield return Windup(Mirror, 0.9f);
        for (int i = 0; i < 3; i++)
        {
            Vector3 from = i == real ? transform.position : spots[i];
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector2 d = ((Vector2)(pl.transform.position - from)).normalized;
            for (int k = -1; k <= 1; k++) Hostile.Shoot(from, Rotate(d, k * 14f), 11f, 15f * Power, 0.5f, Mirror, 1.4f, 3f);
        }
        Hostile.Play("zap", 0.5f, 1.2f);
        yield return new WaitForSeconds(2.2f);
        foreach (GameObject g in fakes)
        {
            if (g == null) continue;
            Fx.Spawn("fx_spark", g.transform.position, 5f, Mirror, 18f);
            Destroy(g);
        }
        Hostile.Play("crack", 0.5f, 1.4f);
    }

    // 낙인: 플레이어 곁으로 순간이동한 뒤 자기 자리에 십자 광선
    IEnumerator BrandCross(PlayerController p)
    {
        Vector3 dest = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 5f));
        if (Hostile.IsWall(dest)) dest = transform.position;
        Hostile.Circle(dest, 2.5f, 0.6f, Mirror);
        Fx.Spawn("fx_soulburst", transform.position, 8f, Mirror, 16f);
        for (float t = 0f; t < 0.6f && Alive; t += Time.deltaTime)
        {
            sr.color = new Color(1f, 1f, 1f, 1f - t / 0.6f);
            yield return null;
        }
        if (!Alive) yield break;
        transform.position = dest;
        sr.color = Color.white;
        Fx.Spawn("fx_shock", dest, 7f, Mirror, 20f);

        const float half = 16f;
        int arms = boss.Enraged ? 2 : 1;
        for (int n = 0; n < arms && Alive; n++)
        {
            float tilt = n * 45f;
            Vector3 c = transform.position;
            Vector3[] ends = new Vector3[4];
            for (int i = 0; i < 2; i++)
            {
                float a = (tilt + i * 90f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * half;
                ends[i * 2] = c - d;
                ends[i * 2 + 1] = c + d;
                Hostile.Line(c - d, c + d, 1.8f, 0.9f, Mirror);
            }
            yield return Windup(Mirror, 0.9f);
            if (!Alive) yield break;
            for (int i = 0; i < 2; i++) Fx.Beam(ends[i * 2], ends[i * 2 + 1], 1.6f, new Color(0.8f, 0.92f, 1f), 0.35f);
            PlayerController pl = Hostile.Player;
            if (pl != null)
                for (int i = 0; i < 2; i++)
                    if (Hostile.DistanceToSegment(pl.transform.position, ends[i * 2], ends[i * 2 + 1]) < 1f) { pl.TryHit(24f * Power); break; }
            Hostile.Play("zap", 0.7f, 0.8f);
            Hostile.Shake(0.15f);
        }
    }

    // 거울 조각 고리: 세 겹으로 엇갈려 퍼지는 조각 (틈으로 피함)
    IEnumerator ShardRings()
    {
        yield return Windup(Mirror, 0.6f);
        for (int w = 0; w < 3 && Alive; w++)
        {
            const int n = 14;
            float off = w * (180f / n);
            for (int i = 0; i < n; i++)
            {
                float a = (off + i * 360f / n) * Mathf.Deg2Rad;
                Hostile.Shoot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 7.5f, 14f * Power, 0.5f, Mirror, 1.5f, 4f);
            }
            Hostile.Play("shimmer", 0.4f, 1.3f + w * 0.1f);
            yield return new WaitForSeconds(0.55f);
        }
    }

    // 이번 판에 가장 많이 준 피해의 출처 (없으면 평타)
    static string TopSource()
    {
        string top = DamageSource.Basic;
        float best = 0f;
        foreach (KeyValuePair<string, float> kv in RunStats.DamageDealt)
            if (kv.Key != DamageSource.Other && kv.Value > best) { best = kv.Value; top = kv.Key; }
        return top;
    }

    // 되비친 기억: 가장 많이 쓴 공격을 거꾸로 돌려줌
    IEnumerator ReflectedMemory(PlayerController p, string top)
    {
        if (top == DamageSource.Basic) { yield return MirrorStrike(6); yield break; }
        if (top == DamageSource.Signature) { yield return MirrorClones(p); yield return MirrorClones(Hostile.Player ?? p); yield break; }
        if (top == DamageSource.Ult)
        {
            // 거대한 광선: 굵은 경고 뒤 플레이어 쪽으로 천천히 훑음
            Vector2 d = DirTo(p);
            float baseAngle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            const float length = 32f;
            Hostile.Line(transform.position, transform.position + (Vector3)(d * length), 4f, 1.4f, Mirror);
            yield return Windup(Mirror, 1.4f);
            bool hit = false;
            for (float t = 0f; t < 1.4f && Alive; t += Time.deltaTime)
            {
                float a = (baseAngle + Mathf.Sin(t * 2.2f) * 18f) * Mathf.Deg2Rad;
                Vector3 end = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * length;
                Fx.Beam(transform.position, end, 3.2f, new Color(0.85f, 0.95f, 1f), 0.05f);
                PlayerController pl = Hostile.Player;
                if (!hit && pl != null && Hostile.DistanceToSegment(pl.transform.position, transform.position, end) < 1.7f) hit = pl.TryHit(34f * Power);
                yield return null;
            }
            Hostile.Shake(0.25f);
            yield break;
        }
        if (top == DamageSource.Tree)
        {
            // 영혼 트리: 플레이어를 끌어당기며 조각 고리
            StartCoroutine(ShardRings());
            for (float t = 0f; t < 2f && Alive; t += Time.deltaTime)
            {
                PlayerController pl = Hostile.Player;
                if (pl == null || pl.IsDying) break;
                Vector3 next = Hostile.ClampArena(pl.transform.position + (transform.position - pl.transform.position).normalized * 2.2f * Time.deltaTime);
                if (!Hostile.IsWall(next)) pl.transform.position = next;
                yield return null;
            }
            yield break;
        }
        // 특수 능력 (무기 · 스킬 이름) · 그 밖: 네 갈래 나선 조각
        yield return Windup(Mirror, 0.6f);
        float spin = Random.value < 0.5f ? 1f : -1f;
        for (float t = 0f; t < 2.6f && Alive; t += 0.11f)
        {
            for (int k = 0; k < 4; k++)
            {
                float a = (t * 75f * spin + k * 90f) * Mathf.Deg2Rad;
                Hostile.Shoot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 8f, 13f * Power, 0.45f, Mirror, 1.3f, 4f);
            }
            if (Mathf.Repeat(t, 0.44f) < 0.11f) Hostile.Play("shimmer", 0.25f, 1.5f);
            yield return new WaitForSeconds(0.11f);
        }
    }

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
                Fx.Spawn("fx_spike", at + Vector3.up * 0.7f, 2.6f, new Color(0.6f, 0.95f, 1f), 18f);
                Fx.Spawn("fx_orb", at, 1f, new Color(0.7f, 0.9f, 1f), 18f);
                Hostile.HitCircle(at, 1.3f, 15f * Power);
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
            Hostile.HitCircle(s, 2.3f, 18f * Power);
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
            foreach (Vector3 at in spots) Fx.Spawn("fx_spike", at + Vector3.up * 0.6f, 2.2f, new Color(1f, 0.97f, 0.88f), 20f);
            PlayerController pl = Hostile.Player;
            if (pl != null)
                foreach (Vector3 at in spots)
                    if (Vector2.Distance(pl.transform.position, at) < 1f) { pl.TryHit(16f * Power); break; }
            Hostile.Play("crack", 0.7f, 1.1f);
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
        LineRenderer runeIn = Own(Hostile.NewLine("RuneRing", violet, 0.14f, 2));
        LineRenderer runeOut = Own(Hostile.NewLine("RuneRing", cyan, 0.1f, 2));
        GameObject aura = Own(Hostile.Glow != null ? SpecialAbilities.MakeSprite("LichAura", Hostile.Glow, home, 0.2f, new Color(0.6f, 0.35f, 1f, 0.5f), "Effect", 1) : null);
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
            lanterns[i] = Own(SpecialAbilities.MakeSprite("SoulLantern", lanternSprite, home, 0.55f, i % 2 == 0 ? cyan : violet, "Effect", 11));
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
                            pl.TryHit(14f * Power);
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

        Hostile.HitCircle(home, blastRadius, 26f * Power);
        Hostile.Burst(home, blastRadius, violet);
        ShockRing.Spawn(home, 1f, blastRadius * 1.6f, 0.6f, cyan, 0.5f);
        ShockRing.Spawn(home, 0.5f, blastRadius * 1.1f, 0.45f, Color.white, 0.3f);
        Fx.Spawn("fx_soulburst", home, blastRadius * 2.4f, Color.white, 14f);
        // 폭발 뒤 바깥 고리에서 가시가 솟음
        for (int i = 0; i < 20; i++)
        {
            float a = i * 18f * Mathf.Deg2Rad;
            Vector3 at = home + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (blastRadius + 1.5f);
            Fx.Spawn("fx_spike", at + Vector3.up * 0.7f, 2.6f, i % 2 == 0 ? cyan : violet, 16f);
        }
        for (int i = 0; i < 16; i++) SoulWisp.Spawn(home, home + (Vector3)(Random.insideUnitCircle.normalized * 9f), Random.value < 0.5f ? cyan : violet, true);
        Hostile.Play("boom", 1f, 0.6f);
        Hostile.Play("chime", 0.7f, 0.5f);
        Hostile.Shake(0.25f);          // 리치 왕의 유일한 화면 흔들림
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
        Fx.Spawn("fx_puddle", start, 3f * SlimeSize, Acid, 14f);
        airborne = true;
        landing = target;
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
        airborne = false;
        if (!Alive) yield break;
        Hostile.HitCircle(target, r, 30f * Power);
        Fx.Spawn("fx_shock", target, r * 2.6f, Acid, 18f);
        Fx.Spawn("fx_puddle", target, r * 1.3f, Acid, 12f);
        HazardZone.Spawn(target, r * 0.8f, 3f, 10f, Acid, "fx_puddle", 0.7f);
        Hostile.Play("thump", 1f, 0.6f);
        Hostile.Shake(0.2f);           // 킹 슬라임의 유일한 화면 흔들림
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
            Hostile.HitCircle(at, 2.2f, 14f * Power);
            Fx.Spawn("fx_cloud", at, 4f, Acid, 16f);
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
            if (!hit && Vector2.Distance(transform.position, p.transform.position) < 2.6f * SlimeSize + 0.6f) hit = p.TryHit(28f * Power);
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        Fx.Spawn("fx_shock", transform.position, 6f * SlimeSize, Acid, 18f);
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
            if (Random.value < 0.3f) Fx.Spawn("fx_explosion", transform.position + (Vector3)(Random.insideUnitCircle * 1.2f), 2.2f, Color.white, 20f);
            if (!hit && Vector2.Distance(transform.position, p.transform.position) < 2.4f) hit = p.TryHit(35f * Power);
            yield return null;
        }
        Hostile.Burst(transform.position, 3f, Fire, true);
        Fx.Spawn("fx_shock", transform.position, 7f, new Color(1f, 0.55f, 0.2f), 18f);
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
        Hostile.HitCircle(spot, r, 28f * Power);
        Fx.Spawn("fx_explosion", spot, r * 2.6f, Color.white, 16f);
        Hostile.Burst(spot, r, Fire, true);
        Hostile.Play("boom", 0.6f, 0.9f);
        Hostile.Shake(0.15f);          // 지옥의 군주의 유일한 화면 흔들림
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
        Fx.Spawn("fx_explosion", transform.position, 5f, Color.white, 16f);
        LineRenderer[] wave = Arcs(gapAt, gapSize, 1f, Fire, 1.2f);
        bool hit = false;
        Vector3 center = transform.position;
        for (float radius = 1f; radius < 22f && Alive; radius += 10f * Time.deltaTime)
        {
            for (int i = 0; i < gaps; i++)
                Hostile.SetArc(wave[i], center, radius, gapAt[i] + gapSize * 0.5f, gapAt[i] + 360f / gaps - gapSize * 0.5f);

            PlayerController p = Hostile.Player;
            if (!hit && p != null)
            {
                Vector2 to = p.transform.position - center;
                if (Mathf.Abs(to.magnitude - radius) < 0.9f && !InGap(Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg, gapAt, gapSize))
                    hit = p.TryHit(30f * Power);
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
            arcs[i] = Own(Hostile.NewLine("FireArc", color, width, 17));
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
        for (int i = 0; i < 4; i++) beams[i] = Own(Hostile.NewLine("HellBeam", new Color(1f, 0.45f, 0.12f, 0.35f), 0.2f, 17));

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
        Fx.Spawn("fx_explosion", transform.position, 5f, Color.white, 16f);
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
                        p.TryHit(25f * Power);
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

    // ================================================================= 추가 스킬 (새 도트 이펙트, 화면 흔들림 없음)
    static readonly Color Bone = new Color(1f, 0.95f, 0.8f, 0.9f);

    // 리치 왕 · 영혼 사슬: 플레이어 쪽 세 갈래 → 보스에서부터 영혼 사슬이 차례로 솟음
    IEnumerator SoulChains(PlayerController p)
    {
        Vector3 c = transform.position;
        Vector2 dir = ((Vector2)(p.transform.position - c)).normalized;
        float[] spread = { -28f, 0f, 28f };
        const float len = 16f;
        foreach (float s in spread)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, s) * dir;
            Hostile.Line(c, c + (Vector3)(d * len), 1.6f, 0.9f, Soul);
        }
        Hostile.Play("shimmer", 0.6f, 0.6f);
        yield return Windup(Soul, 0.9f);
        if (!Alive) yield break;
        bool hit = false;
        for (float k = 1.2f; k <= len && Alive; k += 1.3f)
        {
            foreach (float s in spread)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, s) * dir;
                Vector3 at = c + (Vector3)(d * k);
                if (Hostile.IsWall(at)) continue;
                Fx.Spawn("fx_soulchain", at + Vector3.up * 1.5f, 3.2f, Color.white, 18f, 0f, 14);
                if (!hit) hit = Hostile.HitCircle(at, 0.9f, 16f * Power);
            }
            Hostile.Play("clank", 0.25f, 1.3f + k * 0.03f);
            yield return new WaitForSeconds(0.06f);
        }
    }

    // 리치 왕 · 망자의 손아귀: 플레이어 자리와 둘레에 뼈 손이 솟아 움켜쥠 (잡히면 잠깐 느려짐)
    IEnumerator GraspOfDead(PlayerController p)
    {
        Vector3 c = p.transform.position;
        List<Vector3> spots = new List<Vector3> { c };
        float off = Random.Range(0f, 360f);
        for (int i = 0; i < 6; i++)
        {
            float a = (off + i * 60f) * Mathf.Deg2Rad;
            spots.Add(Hostile.ClampArena(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 3.2f));
        }
        foreach (Vector3 s in spots) Hostile.Circle(s, 1.4f, 1f, Bone);
        Hostile.Play("hiss", 0.5f, 0.6f);
        yield return Windup(Curse, 1f);
        if (!Alive) yield break;
        bool hit = false;
        foreach (Vector3 s in spots)
        {
            Fx.Spawn("fx_bonehand", s + Vector3.up * 0.9f, 2.6f, Color.white, 14f, 0f, 14);
            Fx.Spawn("fx_smoke", s, 2f, new Color(0.5f, 0.45f, 0.5f, 0.7f), 18f);
            if (!hit && Hostile.HitCircle(s, 1.4f, 16f * Power)) { hit = true; p.Slow(0.45f, 1.6f); }
        }
        Hostile.Play("crack", 0.6f, 0.8f);
    }

    // 지옥의 군주 · 지옥불 기둥: 보스에서 나선을 그리며 불기둥이 차례로 치솟음
    IEnumerator HellfirePillars(PlayerController p)
    {
        Vector3 c = transform.position;
        Vector2 dir = ((Vector2)(p.transform.position - c)).normalized;
        float baseAng = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        yield return Windup(Fire, 0.5f);
        if (!Alive) yield break;
        const int n = 16;
        Vector3[] spots = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float a = (baseAng + i * 42f) * Mathf.Deg2Rad;
            spots[i] = Hostile.ClampArena(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (2.5f + i * 0.8f));
            Hostile.Circle(spots[i], 1.4f, 0.6f + i * 0.08f, Fire);
        }
        yield return new WaitForSeconds(0.6f);
        for (int i = 0; i < n && Alive; i++)
        {
            Fx.Spawn("fx_firepillar", spots[i] + Vector3.up * 1.8f, 4f, Color.white, 18f, 0f, 14);
            Hostile.HitCircle(spots[i], 1.4f, 20f * Power);
            if (i % 3 == 0) Hostile.Play("ignite", 0.4f, 1.1f);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // 지옥의 군주 · 용암 균열: 땅이 세 갈래로 갈라져 번진 뒤, 균열을 따라 용암이 치솟음
    IEnumerator LavaFissure(PlayerController p)
    {
        Vector3 c = transform.position;
        Vector2 dir = ((Vector2)(p.transform.position - c)).normalized;
        float[] spread = { -35f, 0f, 35f };
        const float len = 15f;
        foreach (float s in spread)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, s) * dir;
            Hostile.Line(c, c + (Vector3)(d * len), 1.8f, 1.1f, Fire);
        }
        Hostile.Play("thump", 0.6f, 0.7f);
        yield return Windup(Fire, 0.7f);
        // 균열이 앞으로 번짐 (보이기만)
        for (float k = 1f; k <= len && Alive; k += 2f)
        {
            foreach (float s in spread)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, s) * dir;
                Fx.Spawn("fx_fissure", c + (Vector3)(d * k), 1f, Color.white, 10f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 3);
            }
            yield return new WaitForSeconds(0.03f);
        }
        yield return new WaitForSeconds(0.35f);
        if (!Alive) yield break;
        bool hit = false;
        for (float k = 1f; k <= len; k += 1.5f)
            foreach (float s in spread)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, s) * dir;
                Vector3 at = c + (Vector3)(d * k);
                Fx.Spawn("fx_firepillar", at + Vector3.up * 1.2f, 2.6f, Color.white, 20f, 0f, 14);
                if (!hit) hit = Hostile.HitCircle(at, 1.1f, 22f * Power);
            }
        Hostile.Play("boom", 0.6f, 0.8f);
    }

    // 킹 슬라임 · 산성 간헐천: 플레이어를 따라 세 번, 발밑 둘레에서 간헐천이 솟음
    IEnumerator AcidGeysers(PlayerController p)
    {
        yield return Windup(Acid, 0.4f);
        for (int wave = 0; wave < 3 && Alive; wave++)
        {
            Vector3 c = p.transform.position;
            Vector3[] spots = new Vector3[4];
            spots[0] = c;
            for (int i = 1; i < spots.Length; i++) spots[i] = Hostile.ClampArena(c + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2.5f, 5f)));
            foreach (Vector3 s in spots) Hostile.Circle(s, 1.5f, 0.75f, Acid);
            Hostile.Play("hiss", 0.5f, 1f + wave * 0.1f);
            yield return new WaitForSeconds(0.75f);
            if (!Alive) yield break;
            bool hit = false;
            foreach (Vector3 s in spots)
            {
                Fx.Spawn("fx_geyser", s + Vector3.up * 1.7f, 3.8f, Color.white, 16f, 0f, 14);
                if (!hit) hit = Hostile.HitCircle(s, 1.5f, 16f * Power);
            }
            Hostile.Play("whoosh", 0.5f, 1.2f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 킹 슬라임 · 끈적 소용돌이: 플레이어 자리에 소용돌이가 생겨 가운데로 끌어당기다 터짐
    IEnumerator SlimeVortex(PlayerController p)
    {
        Vector3 c = p.transform.position;
        const float r = 4.5f, pullTime = 2.2f;
        FxAnim v = Fx.Play("fx_vortex", c, r * 2f, new Color(1f, 1f, 1f, 0.9f), 14f, 0f, 2, true, pullTime + 0.3f);
        if (v != null) v.spin = -200f;
        Hostile.Circle(c, 2f, pullTime, Acid);
        Hostile.Play("whoosh", 0.7f, 0.6f);
        for (float t = 0f; t < pullTime && Alive; t += Time.deltaTime)
        {
            PlayerController pl = Hostile.Player;
            if (pl != null && Vector2.Distance(pl.transform.position, c) < r)
                pl.transform.position = Vector3.MoveTowards(pl.transform.position, c, 2.2f * Time.deltaTime);
            yield return null;
        }
        if (!Alive) yield break;
        Hostile.HitCircle(c, 2f, 24f * Power);
        Fx.Spawn("fx_geyser", c + Vector3.up * 1.7f, 4.2f, Color.white, 16f, 0f, 14);
        Hostile.Burst(c, 2f, Acid);
        HazardZone.Spawn(c, 1.6f, 3f, 8f, Acid, "fx_puddle", 0.7f);
        Hostile.Play("boom", 0.5f, 1.4f);
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

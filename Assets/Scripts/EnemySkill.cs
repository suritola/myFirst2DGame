using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 값(번호)은 프리팹에 저장되므로 순서를 바꾸지 않음
public enum EnemySkillType
{
    None, BoneSpike, Leap, Blink, XLaser, SelfDestruct, Pounce, GroundSlam, Whirlwind,
    AcidPool, PollenCloud, MushroomMines, Howl, RootLine,
}

// 적이 하나씩 쓰는 스킬. 투사체 없이 점프 · 레이저 · 장판 등으로 공격하고,
// 항상 경고(원 / 선 / 머리 위 !)를 먼저 보여준 뒤 발동해서 피할 수 있음
public class EnemySkill : MonoBehaviour
{
    public EnemySkillType type;
    public float cooldown;

    static readonly Color Danger = new Color(1f, 0.25f, 0.2f, 0.9f);

    EnermyController enemy;
    SpriteRenderer sr;
    float next;
    bool busy;

    bool Alive => enemy != null && !enemy.IsDead;
    // 중간 보스는 범위가 크고 더 자주 씀
    float Size => enemy.survivesContact ? 1.5f : 1f;
    float Dmg => enemy.contactDamage;

    void Start()
    {
        enemy = GetComponent<EnermyController>();
        sr = GetComponent<SpriteRenderer>();
        if (cooldown <= 0f) cooldown = DefaultCooldown(type);
        next = Time.time + Random.Range(1.5f, cooldown);
    }

    static float DefaultCooldown(EnemySkillType t) => t switch
    {
        EnemySkillType.BoneSpike => 4.5f,
        EnemySkillType.Leap => 5f,
        EnemySkillType.Blink => 6.5f,
        EnemySkillType.XLaser => 5f,
        EnemySkillType.SelfDestruct => 1f,
        EnemySkillType.Pounce => 3.5f,
        EnemySkillType.GroundSlam => 6f,
        EnemySkillType.Whirlwind => 5.5f,
        EnemySkillType.AcidPool => 5f,
        EnemySkillType.PollenCloud => 6f,
        EnemySkillType.MushroomMines => 6.5f,
        EnemySkillType.Howl => 8f,
        EnemySkillType.RootLine => 5.5f,
        _ => 5f,
    };

    bool InRange(float d) => type switch
    {
        EnemySkillType.BoneSpike => d < 12f,
        EnemySkillType.Leap => d > 4f && d < 10f,
        EnemySkillType.Blink => d > 7f && d < 22f,
        EnemySkillType.XLaser => d < 14f,
        EnemySkillType.SelfDestruct => d < 3.2f * Size,
        EnemySkillType.Pounce => d > 4f && d < 11f,
        EnemySkillType.GroundSlam => d < 7f * Size,
        EnemySkillType.Whirlwind => d < 8f,
        EnemySkillType.AcidPool => d < 6f,
        EnemySkillType.PollenCloud => d < 13f,
        EnemySkillType.MushroomMines => d < 9f,
        EnemySkillType.Howl => d < 18f,
        EnemySkillType.RootLine => d > 3f && d < 14f,
        _ => false,
    };

    void Update()
    {
        if (busy || !Alive || type == EnemySkillType.None) return;
        PlayerController p = Hostile.Player;
        if (p == null || Time.time < next) return;
        if (!InRange(Vector2.Distance(transform.position, p.transform.position))) return;

        next = Time.time + cooldown * Random.Range(0.85f, 1.2f) * (enemy.survivesContact ? 0.7f : 1f);
        StartCoroutine(Run(p));
    }

    IEnumerator Run(PlayerController p)
    {
        busy = true;
        enemy.casting = true;
        IEnumerator routine = type switch
        {
            EnemySkillType.BoneSpike => BoneSpike(p),
            EnemySkillType.Leap => Leap(p),
            EnemySkillType.Blink => Blink(p),
            EnemySkillType.XLaser => XLaser(p),
            EnemySkillType.SelfDestruct => SelfDestruct(),
            EnemySkillType.Pounce => Pounce(p),
            EnemySkillType.GroundSlam => GroundSlam(),
            EnemySkillType.Whirlwind => Whirlwind(p),
            EnemySkillType.AcidPool => AcidPool(),
            EnemySkillType.PollenCloud => PollenCloud(p),
            EnemySkillType.MushroomMines => MushroomMines(),
            EnemySkillType.Howl => Howl(),
            EnemySkillType.RootLine => RootLine(p),
            _ => null,
        };
        if (routine != null) yield return StartCoroutine(routine);
        if (enemy != null) enemy.casting = false;
        if (sr != null && Alive) sr.color = enemy.baseColor;
        busy = false;
    }

    // 몸이 깜빡이며 기를 모음 (빛 + 머리 위 느낌표)
    IEnumerator Windup(Color color, float seconds, float speed = 8f)
    {
        GameObject aura = Hostile.Glow != null
            ? SpecialAbilities.MakeSprite("WindupAura", Hostile.Glow, transform.position, 0.1f, new Color(color.r, color.g, color.b, 0.55f), "Effect", 0) : null;
        FxAnim mark = Fx.Play("fx_warn", transform.position + Vector3.up * 1.6f * Size, 1.1f, Color.white, 1f, 0f, 30, true, seconds);
        if (mark != null) mark.transform.SetParent(transform, true);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (!Alive) break;
            float pulse = Mathf.PingPong(Time.time * speed, 1f);
            sr.color = Color.Lerp(enemy.baseColor, color, pulse);
            if (aura != null)
            {
                aura.transform.position = transform.position;
                aura.transform.localScale = Vector3.one * Size * Mathf.Lerp(0.25f, 0.45f, t / seconds) * (0.9f + 0.2f * pulse);
            }
            yield return null;
        }
        if (aura != null) Destroy(aura);
        if (mark != null) Destroy(mark.gameObject);
        if (Alive) sr.color = enemy.baseColor;
    }

    Vector2 DirTo(PlayerController p) => ((Vector2)(p.transform.position - transform.position)).normalized;

    // ================================================================= skills
    // 뼈 가시: 플레이어 발밑에서 뼈 가시가 솟음
    IEnumerator BoneSpike(PlayerController p)
    {
        Vector3 at = p.transform.position;
        Hostile.Circle(at, 1.4f, 0.8f, new Color(1f, 0.95f, 0.8f, 0.9f));
        yield return Windup(new Color(1f, 1f, 0.8f), 0.8f);
        if (!Alive) yield break;
        Fx.Play("fx_spike", at + Vector3.up * 0.6f, 2.4f, new Color(1f, 0.97f, 0.88f), 18f);
        Hostile.HitCircle(at, 1.4f, Dmg);
        Hostile.Play("crack", 0.4f, 1.5f);
    }

    // 구울: 떨어질 곳에 원을 띄운 뒤 높이 뛰어 내려찍음
    IEnumerator Leap(PlayerController p)
    {
        Vector3 target = Hostile.ClampArena(p.transform.position);
        if (Hostile.IsWall(target)) yield break;
        float r = 1.9f * Size;
        Hostile.Circle(target, r, 0.7f, Danger);
        yield return Windup(Danger, 0.7f);
        if (!Alive) yield break;

        Vector3 start = transform.position;
        Fx.Play("fx_smoke", start, 1.8f, new Color(0.8f, 0.7f, 0.6f), 14f);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            float k = t / 0.35f;
            transform.position = Vector3.Lerp(start, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.5f;
            yield return null;
        }
        transform.position = target;
        if (!Alive) yield break;
        Hostile.HitCircle(target, r, Dmg * 1.2f);
        Fx.Play("fx_shock", target, r * 2.4f, new Color(0.9f, 0.75f, 0.55f), 20f);
        for (int i = 0; i < 4; i++) Fx.Play("fx_smoke", target + (Vector3)(Random.insideUnitCircle * r), 1.6f, new Color(0.75f, 0.65f, 0.55f), 12f);
        Hostile.Play("thump", 0.6f);
        Hostile.Shake(0.15f);
    }

    // 망령: 사라졌다가 표시한 곳에 나타나며 영혼 폭발
    IEnumerator Blink(PlayerController p)
    {
        Vector3 dest = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 3f));
        if (Hostile.IsWall(dest)) yield break;
        float r = 1.8f * Size;
        Color purple = new Color(0.7f, 0.4f, 1f, 0.9f);
        Hostile.Circle(dest, r, 0.8f, purple);
        Hostile.Play("shimmer", 0.35f, 1.4f);
        Fx.Play("fx_soulburst", transform.position, 2.5f, Color.white, 18f);

        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            Color c = enemy.baseColor;
            c.a = 1f - Mathf.Clamp01(t / 0.3f);
            sr.color = c;
            yield return null;
        }
        if (!Alive) yield break;
        transform.position = dest;
        sr.color = enemy.baseColor;
        Hostile.HitCircle(dest, r, Dmg);
        Fx.Play("fx_soulburst", dest, r * 2.6f, Color.white, 16f);
        Fx.Play("fx_shock", dest, r * 2.4f, purple, 20f);
    }

    // 임프: 플레이어 자리에 X자 레이저 (두 줄기가 엇갈림)
    IEnumerator XLaser(PlayerController p)
    {
        Vector3 c = p.transform.position;
        const float half = 7f;
        float tilt = Random.Range(0f, 90f);
        Vector3[] ends = new Vector3[4];
        for (int i = 0; i < 2; i++)
        {
            float a = (tilt + 45f + i * 90f) * Mathf.Deg2Rad;
            Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * half;
            ends[i * 2] = c - d;
            ends[i * 2 + 1] = c + d;
            Hostile.Line(c - d, c + d, 1.3f, 0.9f, new Color(1f, 0.45f, 0.15f, 0.9f));
        }
        FxAnim rune = Fx.Play("fx_rune", c, 3f, new Color(1f, 0.5f, 0.2f, 0.8f), 1f, 0f, 1, true, 1.1f);
        if (rune != null) rune.spin = 120f;
        yield return Windup(new Color(1f, 0.6f, 0.2f), 0.9f, 12f);
        if (!Alive) yield break;

        Hostile.Play("zap", 0.7f, 0.7f);
        Hostile.Play("flame", 0.5f, 1.2f);
        for (int i = 0; i < 2; i++) Fx.Beam(ends[i * 2], ends[i * 2 + 1], 1.2f, new Color(1f, 0.55f, 0.2f), 0.35f);
        Fx.Play("fx_explosion", c, 2.5f, Color.white, 18f);
        PlayerController pl = Hostile.Player;
        if (pl != null)
            for (int i = 0; i < 2; i++)
                if (Hostile.DistanceToSegment(pl.transform.position, ends[i * 2], ends[i * 2 + 1]) < 0.8f) { pl.TryHit(Dmg * 1.1f); break; }
        Hostile.Shake(0.12f);
    }

    // 불꽃 해골: 가까이 오면 부풀어 오르다 폭발 (중간 보스는 폭발만)
    IEnumerator SelfDestruct()
    {
        float r = 2.8f * Size;
        Hostile.Circle(transform.position, r, 0.9f, new Color(1f, 0.5f, 0.1f, 0.9f));
        Hostile.Play("crackle", 0.5f, 1.3f);
        Vector3 baseScale = transform.localScale;
        for (float t = 0f; t < 0.9f; t += Time.deltaTime)
        {
            if (!Alive) { transform.localScale = baseScale; yield break; }
            sr.color = Color.Lerp(enemy.baseColor, Color.white, Mathf.PingPong(Time.time * (8f + t * 20f), 1f));
            transform.localScale = baseScale * (1f + 0.25f * t / 0.9f);
            yield return null;
        }
        transform.localScale = baseScale;
        if (!Alive) yield break;
        Hostile.HitCircle(transform.position, r, Dmg * 1.6f);
        Fx.Play("fx_explosion", transform.position, r * 2.4f, Color.white, 16f);
        Fx.Play("fx_shock", transform.position, r * 2.6f, new Color(1f, 0.6f, 0.2f), 20f);
        Hostile.Play("boom", 0.6f, 1.2f);
        Hostile.Shake(0.2f);
        if (!enemy.survivesContact) enemy.KillBySkill();
    }

    // 지옥견: 경로를 보여준 뒤 일직선 돌진 (먼지 꼬리)
    IEnumerator Pounce(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        const float length = 10f;
        Vector3 start = transform.position;
        Vector3 end = Hostile.ClampArena(start + (Vector3)(dir * length));
        Hostile.Line(start, end, 1.6f * Size, 0.55f, Danger);
        yield return Windup(Danger, 0.55f);
        if (!Alive) yield break;

        bool hit = false;
        float dust = 0f;
        Hostile.Play("whoosh", 0.4f, 0.9f);
        for (float t = 0f; t < 0.28f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            Vector3 nextPos = Vector3.Lerp(start, end, t / 0.28f);
            if (Hostile.IsWall(nextPos)) break;
            transform.position = nextPos;
            dust += Time.deltaTime;
            if (dust > 0.04f) { dust = 0f; Fx.Play("fx_smoke", transform.position, 1.4f, new Color(0.7f, 0.5f, 0.45f), 16f); }
            if (!hit && Vector2.Distance(transform.position, p.transform.position) < 1.3f * Size) hit = p.TryHit(Dmg);
            yield return null;
        }
    }

    // 용암 골렘: 점점 넓어지는 세 번의 지면 파동 (가시가 솟음)
    IEnumerator GroundSlam()
    {
        float[] radii = { 2.5f * Size, 4.5f * Size, 6.5f * Size };
        Vector3 c = transform.position;
        yield return Windup(new Color(1f, 0.5f, 0.2f), 0.5f, 5f);
        for (int w = 0; w < radii.Length && Alive; w++)
        {
            float r = radii[w];
            float inner = w == 0 ? 0f : radii[w - 1];
            Hostile.Circle(c, r, 0.55f, new Color(1f, 0.45f, 0.15f, 0.9f));
            yield return new WaitForSeconds(0.55f);
            if (!Alive) yield break;
            PlayerController pl = Hostile.Player;
            if (pl != null)
            {
                float d = Vector2.Distance(pl.transform.position, c);
                if (d <= r + 0.4f && d >= inner - 0.4f) pl.TryHit(Dmg);
            }
            int n = 6 + w * 4;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)n * 360f + w * 15f) * Mathf.Deg2Rad;
                Fx.Play("fx_spike", c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (r - 0.5f) + Vector3.up * 0.5f, 1.8f, new Color(1f, 0.6f, 0.35f), 18f);
            }
            Fx.Play("fx_shock", c, r * 2.2f, new Color(1f, 0.55f, 0.2f), 18f);
            Hostile.Play("boom", 0.5f, 0.7f + w * 0.1f);
            Hostile.Shake(0.15f);
        }
    }

    // 악마 기사: 칼날을 휘감고 회전하며 플레이어 쪽으로 미끄러짐
    IEnumerator Whirlwind(PlayerController p)
    {
        float r = 2.2f * Size;
        FxAnim ring = Fx.Play("fx_rune", transform.position, r * 2f, new Color(1f, 0.3f, 0.3f, 0.7f), 1f, 0f, 1, true, 0.7f);
        if (ring != null) { ring.follow = transform; ring.spin = -200f; }
        yield return Windup(Danger, 0.7f);
        if (!Alive) yield break;

        Hostile.Play("whoosh", 0.6f, 0.7f);
        float slash = 0f;
        float lastHit = -1f;
        for (float t = 0f; t < 1.4f && Alive; t += Time.deltaTime)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector3 step = (Vector3)(DirTo(pl) * enemy.speed * 0.9f * Time.deltaTime);
            if (!Hostile.IsWall(transform.position + step)) transform.position += step;
            slash += Time.deltaTime;
            if (slash > 0.12f)
            {
                slash = 0f;
                Fx.Play("fx_slash", transform.position, r * 2.2f, new Color(1f, 0.55f, 0.55f), 30f, Random.Range(0f, 360f), 14);
                Hostile.Play("whoosh", 0.2f, 1.4f);
            }
            if (Time.time - lastHit > 0.45f && Vector2.Distance(pl.transform.position, transform.position) < r)
            {
                lastHit = Time.time;
                pl.TryHit(Dmg * 0.8f);
            }
            yield return null;
        }
    }

    // 산성 웅덩이: 제자리에 밟으면 아픈 웅덩이를 남김 (4초)
    IEnumerator AcidPool()
    {
        float r = 2.2f * Size;
        Vector3 c = transform.position;
        Hostile.Circle(c, r, 0.6f, new Color(0.5f, 1f, 0.3f, 0.9f));
        yield return Windup(new Color(0.6f, 1f, 0.4f), 0.6f);
        if (!Alive) yield break;
        HazardZone.Spawn(c, r, 4f, Dmg * 0.6f, new Color(0.55f, 1f, 0.35f, 0.9f), "fx_puddle", 0f);
        Hostile.Play("hiss", 0.5f, 1.2f);
    }

    // 꽃가루 구름: 플레이어 주변에 구름 → 안에 있으면 느려지고 조금씩 아픔
    IEnumerator PollenCloud(PlayerController p)
    {
        Vector3 c = p.transform.position;
        float r = 3f * Size;
        Hostile.Circle(c, r, 0.9f, new Color(1f, 0.9f, 0.35f, 0.9f));
        yield return Windup(new Color(1f, 0.95f, 0.5f), 0.9f);
        if (!Alive) yield break;
        HazardZone z = HazardZone.Spawn(c, r, 4f, Dmg * 0.25f, new Color(1f, 0.92f, 0.45f, 0.75f), "fx_cloud", 0.55f);
        z.fps = 6f;
        Hostile.Play("shimmer", 0.4f, 0.8f);
    }

    // 버섯 지뢰: 주변에 버섯을 심고 잠시 뒤 차례로 터짐
    IEnumerator MushroomMines()
    {
        yield return Windup(new Color(1f, 0.4f, 0.35f), 0.5f);
        if (!Alive) yield break;
        int n = enemy.survivesContact ? 5 : 3;
        for (int i = 0; i < n; i++)
        {
            Vector3 at = Hostile.ClampArena(transform.position + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2f, 4.5f)));
            StartCoroutine(Mine(at, 1.3f + i * 0.25f));
        }
        Hostile.Play("thump", 0.4f, 1.4f);
    }

    IEnumerator Mine(Vector3 at, float fuse)
    {
        const float r = 1.8f;
        FxAnim cap = Fx.Play("fx_mushroom", at, 1.1f, Color.white, 6f, 0f, 6, true, fuse);
        Hostile.Circle(at, r, fuse, new Color(1f, 0.35f, 0.3f, 0.8f));
        yield return new WaitForSeconds(fuse);
        Hostile.HitCircle(at, r, Dmg * 0.9f);
        Fx.Play("fx_cloud", at, r * 2.4f, new Color(1f, 0.55f, 0.5f), 18f);
        Fx.Play("fx_shock", at, r * 2.2f, new Color(1f, 0.4f, 0.35f), 20f);
        Hostile.Play("boom", 0.35f, 1.5f);
    }

    // 울부짖음: 주변 적이 잠시 빨라짐 (붉은 파동)
    IEnumerator Howl()
    {
        yield return Windup(new Color(1f, 0.3f, 0.3f), 0.6f, 14f);
        if (!Alive) yield break;
        const float r = 9f;
        Fx.Play("fx_shock", transform.position, r * 2f, new Color(1f, 0.35f, 0.3f), 14f);
        ShockRing.Spawn(transform.position, 0.5f, r, 0.5f, new Color(1f, 0.4f, 0.35f, 0.9f), 0.35f);
        Hostile.Play("pulse", 0.7f, 0.8f);
        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, r))
        {
            EnermyController e = c.GetComponent<EnermyController>();
            if (e == null || e.IsDead) continue;
            e.Enrage(1.6f, 3.5f);
            Fx.Play("fx_spark", e.transform.position + Vector3.up, 1.2f, new Color(1f, 0.4f, 0.35f), 14f);
        }
    }

    // 뿌리 가시 행렬: 플레이어 쪽으로 가시가 차례로 솟아오름
    IEnumerator RootLine(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        const int count = 7;
        const float spacing = 1.8f;
        Vector3 start = transform.position + (Vector3)(dir * 1.5f);
        Hostile.Line(start, start + (Vector3)(dir * spacing * count), 1.6f, 0.7f, new Color(0.6f, 0.9f, 0.3f, 0.9f));
        yield return Windup(new Color(0.6f, 1f, 0.4f), 0.7f);
        for (int i = 0; i < count && Alive; i++)
        {
            Vector3 at = start + (Vector3)(dir * spacing * i);
            Fx.Play("fx_spike", at + Vector3.up * 0.6f, 2.4f, new Color(0.65f, 0.5f, 0.3f), 18f);
            Fx.Play("fx_smoke", at, 1.2f, new Color(0.55f, 0.45f, 0.3f), 14f);
            Hostile.HitCircle(at, 1.1f, Dmg);
            Hostile.Play("crack", 0.25f, 1.2f + i * 0.05f);
            yield return new WaitForSeconds(0.09f);
        }
    }
}

// 밟으면 아픈 장판 (산성 웅덩이, 꽃가루 구름): 플레이어가 안에 있으면 틱 피해 + 감속
public class HazardZone : MonoBehaviour
{
    public float radius;
    public float life;
    public float tickDamage;
    public float slow;          // 0이면 감속 없음, 0.55 = 55% 속도
    public float fps = 8f;
    float age;
    float tick;
    FxAnim visual;

    public static HazardZone Spawn(Vector3 pos, float radius, float life, float tickDamage, Color color, string fx, float slow)
    {
        GameObject go = new GameObject("HazardZone");
        go.transform.position = pos;
        HazardZone z = go.AddComponent<HazardZone>();
        z.radius = radius;
        z.life = life;
        z.tickDamage = tickDamage;
        z.slow = slow;
        z.visual = Fx.Play(fx, pos, radius * (fx == "fx_puddle" ? 1.1f : 2.2f), color, z.fps, 0f, fx == "fx_puddle" ? 1 : 16, true, life);
        if (z.visual != null && fx == "fx_puddle")
        {
            // 웅덩이는 옆으로 넓은 그림이라 가로 비율 유지
            Vector3 s = z.visual.transform.localScale;
            z.visual.transform.localScale = new Vector3(s.x, s.y, 1f);
        }
        return z;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (visual != null) visual.fps = fps;
        PlayerController p = Hostile.Player;
        if (p != null && Vector2.Distance(p.transform.position, transform.position) < radius)
        {
            if (slow > 0f) p.Slow(slow, 0.2f);
            tick += Time.deltaTime;
            if (tick >= 0.6f)
            {
                tick = 0f;
                p.TryHit(tickDamage);
            }
        }
        if (age >= life) Destroy(gameObject);
    }
}

// ===================================================================== shared helpers (적 · 보스 공용)
public static class Hostile
{
    static PlayerController player;
    static Material lineMaterial;

    public static PlayerController Player
    {
        get
        {
            if (player == null) player = Object.FindFirstObjectByType<PlayerController>();
            return player;
        }
    }

    public static Sprite Glow => SpecialAbilities.GlowSprite;

    public static void Play(string sound, float vol = 1f, float pitch = 1f)
    {
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Play(sound, vol, pitch);
    }

    public static void Shake(float strength)
    {
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.Shake(strength, 0.15f);
    }

    public static LineRenderer NewLine(string name, Color color, float width, int order = 18)
    {
        if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));
        GameObject go = new GameObject(name);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.useWorldSpace = true;
        lr.sortingLayerName = "Effect";
        lr.sortingOrder = order;
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        lr.numCapVertices = 2;
        return lr;
    }

    public static void SetArc(LineRenderer lr, Vector3 center, float radius, float fromDeg, float toDeg)
    {
        int seg = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(toDeg - fromDeg) / 6f));
        lr.positionCount = seg + 1;
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, i / (float)seg) * Mathf.Deg2Rad;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
    }

    // 원형 경고: 테두리 + 안쪽이 차오르는 원 (다 차면 발동)
    public static Telegraph Circle(Vector3 pos, float radius, float duration, Color color)
    {
        GameObject go = new GameObject("WarnCircle");
        go.transform.position = pos;
        Telegraph t = go.AddComponent<Telegraph>();
        t.InitCircle(radius, duration, color);
        return t;
    }

    // 직선 경고: 옅은 띠 + 앞으로 차오르는 띠
    public static Telegraph Line(Vector3 a, Vector3 b, float width, float duration, Color color)
    {
        GameObject go = new GameObject("WarnLine");
        Telegraph t = go.AddComponent<Telegraph>();
        t.InitLine(a, b, width, duration, color);
        return t;
    }

    public static bool HitCircle(Vector3 pos, float radius, float damage)
    {
        PlayerController p = Player;
        if (p == null || Vector2.Distance(pos, p.transform.position) > radius + 0.4f) return false;
        return p.TryHit(damage);
    }

    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float k = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * k);
    }

    public static HostileProjectile Shoot(Vector3 pos, Vector2 dir, float speed, float damage, float radius, Color color, float scale, float life = 4f)
    {
        GameObject go = SpecialAbilities.MakeSprite("HostileShot", Glow, pos, scale, color, "Effect", 9);
        HostileProjectile h = go.AddComponent<HostileProjectile>();
        h.dir = dir.normalized;
        h.speed = speed;
        h.damage = damage;
        h.radius = radius;
        h.life = life;
        return h;
    }

    // 폭발 연출: 빛 + 퍼지는 고리 (+ 불꽃)
    public static void Burst(Vector3 pos, float radius, Color color, bool fire = false)
    {
        // 도트 폭발: 불이면 화염, 아니면 색 입힌 충격파와 불꽃
        if (fire) Fx.Play("fx_explosion", pos, radius * 2.4f, Color.white, 16f);
        else
        {
            Fx.Play("fx_shock", pos, radius * 2.4f, color, 20f);
            Fx.Play("fx_spark", pos, radius * 1.2f, color, 18f);
        }
        if (Glow == null) return;
        GameObject f = SpecialAbilities.MakeSprite("Burst", Glow, pos, radius * 2f / 8f, color, "Effect", 3);
        f.AddComponent<FadeOut>().duration = 0.3f;
        ShockRing.Spawn(pos, radius * 0.3f, radius * 1.1f, 0.3f, color, 0.3f);
        ShockRing.Spawn(pos, radius * 0.1f, radius * 0.8f, 0.45f, Color.white, 0.15f);
        GameObject core = SpecialAbilities.MakeSprite("BurstCore", Glow, pos, radius * 0.8f / 8f, new Color(1f, 1f, 1f, 0.9f), "Effect", 4);
        core.AddComponent<FadeOut>().duration = 0.12f;
        for (int i = 0; i < 8; i++) SoulWisp.Spawn(pos, pos + (Vector3)(Random.insideUnitCircle.normalized * radius * 1.6f), color, true);
        if (!fire) return;
        for (int i = 0; i < 10; i++)
        {
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(radius * 2f, radius * 4f);
            FlameParticle.Spawn(Glow, pos, v, Random.Range(0.35f, 0.55f), 0.05f, Random.Range(0.3f, 0.5f), false);
        }
    }

    // 반지름 안에 벽(구조물)이 있는지
    public static bool WallNear(Vector3 pos, float radius)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius))
            if (!c.isTrigger && c.CompareTag("Wall")) return true;
        return false;
    }

    // 벽(맵 테두리, 구조물)인지
    public static bool IsWall(Vector3 pos)
    {
        foreach (Collider2D c in Physics2D.OverlapPointAll(pos))
            if (!c.isTrigger && c.CompareTag("Wall")) return true;
        return false;
    }

    public static Vector3 ClampArena(Vector3 p)
    {
        EnemySpawner sp = Object.FindFirstObjectByType<EnemySpawner>();
        if (sp == null) return p;
        return new Vector3(Mathf.Clamp(p.x, sp.spawnAreaMin.x, sp.spawnAreaMax.x), Mathf.Clamp(p.y, sp.spawnAreaMin.y, sp.spawnAreaMax.y + 1.5f), 0f);
    }
}

// 경고 표시: 굵은 테두리 + 차오르는 안쪽 + 발동 순간까지 좁혀 드는 고리
public class Telegraph : MonoBehaviour
{
    float duration;
    float t;
    Color color;
    bool circle;
    float radius;
    Vector3 a, b;
    float width;
    LineRenderer outline;
    LineRenderer closing;
    LineRenderer edgeL, edgeR;
    LineRenderer fillLine;
    SpriteRenderer fillDisc;
    SpriteRenderer glow;

    public void InitCircle(float radius, float duration, Color color)
    {
        circle = true;
        this.radius = radius;
        this.duration = duration;
        this.color = color;
        outline = Child(Hostile.NewLine("Outline", color, 0.2f, 18));
        outline.loop = true;
        Hostile.SetArc(outline, transform.position, radius, 0f, 354f);
        closing = Child(Hostile.NewLine("Closing", Color.white, 0.12f, 19));
        closing.loop = true;
        // 바닥에 도는 도트 룬 마법진
        FxAnim rune = Fx.Play("fx_rune", transform.position, radius * 2f, new Color(color.r, color.g, color.b, 0.55f), 1f, 0f, 1, true, duration);
        if (rune != null)
        {
            rune.spin = 70f;
            rune.transform.SetParent(transform, true);
        }
        if (Hostile.Glow != null)
        {
            GameObject g = SpecialAbilities.MakeSprite("Ground", Hostile.Glow, transform.position, radius * 2.4f / 8f, new Color(color.r, color.g, color.b, 0.15f), "Effect", 0);
            g.transform.SetParent(transform, true);
            glow = g.GetComponent<SpriteRenderer>();
            GameObject d = SpecialAbilities.MakeSprite("Fill", Hostile.Glow, transform.position, 0.01f, new Color(color.r, color.g, color.b, 0.6f), "Effect", 1);
            d.transform.SetParent(transform, true);
            fillDisc = d.GetComponent<SpriteRenderer>();
        }
    }

    public void InitLine(Vector3 a, Vector3 b, float width, float duration, Color color)
    {
        this.a = a;
        this.b = b;
        this.width = width;
        this.duration = duration;
        this.color = color;
        outline = Child(Hostile.NewLine("Band", new Color(color.r, color.g, color.b, 0.22f), width, 1));
        outline.positionCount = 2;
        outline.SetPosition(0, a);
        outline.SetPosition(1, b);
        // 양쪽 가장자리 선
        Vector3 side = Vector3.Cross(b - a, Vector3.forward).normalized * width * 0.5f;
        edgeL = Child(Hostile.NewLine("Edge", color, 0.1f, 18));
        edgeR = Child(Hostile.NewLine("Edge", color, 0.1f, 18));
        edgeL.positionCount = edgeR.positionCount = 2;
        edgeL.SetPosition(0, a + side); edgeL.SetPosition(1, b + side);
        edgeR.SetPosition(0, a - side); edgeR.SetPosition(1, b - side);
        fillLine = Child(Hostile.NewLine("Fill", new Color(color.r, color.g, color.b, 0.55f), width * 0.7f, 2));
        fillLine.positionCount = 2;
        fillLine.SetPosition(0, a);
        fillLine.SetPosition(1, a);
    }

    LineRenderer Child(LineRenderer lr)
    {
        lr.transform.SetParent(transform, false);
        return lr;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float blink = 0.65f + 0.35f * Mathf.PingPong(Time.time * (4f + k * 14f), 1f);
        Color c = new Color(color.r, color.g, color.b, color.a * blink);

        if (circle)
        {
            outline.startColor = outline.endColor = c;
            // 발동 순간에 테두리와 겹치도록 바깥에서 좁혀 들어오는 고리
            Hostile.SetArc(closing, transform.position, radius * Mathf.Lerp(1.8f, 1f, k), 0f, 354f);
            closing.startColor = closing.endColor = new Color(1f, 1f, 1f, 0.25f + 0.55f * k);
            if (fillDisc != null) fillDisc.transform.localScale = Vector3.one * (radius * 2f / 8f) * k;
            if (glow != null) glow.color = new Color(color.r, color.g, color.b, 0.12f + 0.2f * k * blink);
        }
        else
        {
            fillLine.SetPosition(1, Vector3.Lerp(a, b, k));
            outline.startColor = outline.endColor = new Color(color.r, color.g, color.b, 0.2f * blink + 0.06f);
            edgeL.startColor = edgeL.endColor = edgeR.startColor = edgeR.endColor = c;
        }

        if (t >= duration)
        {
            // 발동 순간 짧은 섬광
            if (circle) ShockRing.Spawn(transform.position, radius * 0.9f, radius * 1.15f, 0.18f, Color.white, 0.18f);
            Destroy(gameObject);
        }
    }
}

// 퍼져 나가며 사라지는 고리
public class ShockRing : MonoBehaviour
{
    float from, to, duration, t, width;
    Color color;
    LineRenderer lr;

    public static void Spawn(Vector3 pos, float from, float to, float duration, Color color, float width)
    {
        GameObject go = new GameObject("ShockRing");
        go.transform.position = pos;
        ShockRing s = go.AddComponent<ShockRing>();
        s.from = from;
        s.to = to;
        s.duration = duration;
        s.color = color;
        s.width = width;
        s.lr = Hostile.NewLine("Ring", color, width, 19);
        s.lr.transform.SetParent(go.transform, false);
        s.lr.loop = true;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float ease = 1f - (1f - k) * (1f - k);
        Hostile.SetArc(lr, transform.position, Mathf.Lerp(from, to, ease), 0f, 354f);
        lr.startColor = lr.endColor = new Color(color.r, color.g, color.b, color.a * (1f - k));
        lr.startWidth = lr.endWidth = width * (1f - 0.6f * k);
        if (k >= 1f) Destroy(gameObject);
    }
}

// 적이 쏘는 탄: 플레이어에 닿으면 피해, 벽에 닿거나 시간이 지나면 사라짐
public class HostileProjectile : MonoBehaviour
{
    public Vector2 dir;
    public float speed = 10f;
    public float damage = 10f;
    public float radius = 0.5f;
    public float life = 4f;
    public bool spin;
    public bool fiery;
    public bool pierceWalls;
    float puff;
    float trail;
    SpriteRenderer body;
    GameObject core;

    void Start()
    {
        body = GetComponent<SpriteRenderer>();
        // 하얀 중심으로 잘 보이게
        if (Hostile.Glow != null)
        {
            core = SpecialAbilities.MakeSprite("Core", Hostile.Glow, transform.position, transform.localScale.x * 0.45f, new Color(1f, 1f, 1f, 0.95f), "Effect", 10);
            core.transform.SetParent(transform, true);
        }
    }

    void Update()
    {
        // 시간 왜곡에 같이 느려짐
        transform.position += (Vector3)(dir * speed * EnermyController.GlobalSpeedMultiplier * Time.deltaTime);
        if (spin) transform.Rotate(0f, 0f, 720f * Time.deltaTime);

        // 색깔 꼬리
        trail += Time.deltaTime;
        if (!fiery && body != null && Hostile.Glow != null && trail >= 0.05f)
        {
            trail = 0f;
            Color c = body.color;
            GameObject tr = SpecialAbilities.MakeSprite("Trail", Hostile.Glow, transform.position, transform.localScale.y * 0.8f, new Color(c.r, c.g, c.b, 0.5f), "Effect", 8);
            tr.AddComponent<FadeOut>().duration = 0.25f;
        }

        if (fiery && Hostile.Glow != null)
        {
            puff += Time.deltaTime;
            if (puff >= 0.04f)
            {
                puff = 0f;
                FlameParticle.Spawn(Hostile.Glow, transform.position, -dir * 2f + Random.insideUnitCircle, 0.3f, 0.04f, 0.18f, false);
            }
        }

        PlayerController p = Hostile.Player;
        if (p != null && Vector2.Distance(transform.position, p.transform.position) < radius + 0.4f && p.TryHit(damage))
        {
            Hostile.Burst(transform.position, 1f, GetComponent<SpriteRenderer>().color);
            Destroy(gameObject);
            return;
        }

        life -= Time.deltaTime;
        if (life <= 0f || (!pierceWalls && Hostile.IsWall(transform.position))) Destroy(gameObject);
    }
}

using System.Collections;
using UnityEngine;

public enum EnemySkillType { None, BoneThrow, Leap, Blink, Fireball, SelfDestruct, Pounce, GroundSlam, SwordWave }

// 적이 하나씩 쓰는 스킬. 항상 경고(원 / 선)를 먼저 보여주고 잠시 뒤에 발동해서 피할 수 있음
// EnermyController.skill 값에 따라 시작할 때 붙음
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
        EnemySkillType.BoneThrow => 4f,
        EnemySkillType.Leap => 5f,
        EnemySkillType.Blink => 6.5f,
        EnemySkillType.Fireball => 3.5f,
        EnemySkillType.SelfDestruct => 1f,
        EnemySkillType.Pounce => 3.5f,
        EnemySkillType.GroundSlam => 5.5f,
        EnemySkillType.SwordWave => 4.5f,
        _ => 5f,
    };

    bool InRange(float d) => type switch
    {
        EnemySkillType.BoneThrow => d < 14f && d > 3f,
        EnemySkillType.Leap => d > 4f && d < 10f,
        EnemySkillType.Blink => d > 7f && d < 22f,
        EnemySkillType.Fireball => d < 15f && d > 3f,
        EnemySkillType.SelfDestruct => d < 3.2f * Size,
        EnemySkillType.Pounce => d > 4f && d < 11f,
        EnemySkillType.GroundSlam => d < 4f * Size,
        EnemySkillType.SwordWave => d < 13f && d > 2.5f,
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
            EnemySkillType.BoneThrow => BoneThrow(p),
            EnemySkillType.Leap => Leap(p),
            EnemySkillType.Blink => Blink(p),
            EnemySkillType.Fireball => Fireball(p),
            EnemySkillType.SelfDestruct => SelfDestruct(),
            EnemySkillType.Pounce => Pounce(p),
            EnemySkillType.GroundSlam => GroundSlam(),
            EnemySkillType.SwordWave => SwordWave(p),
            _ => null,
        };
        if (routine != null) yield return StartCoroutine(routine);
        if (enemy != null) enemy.casting = false;
        if (sr != null && Alive) sr.color = enemy.baseColor;
        busy = false;
    }

    // 몸이 깜빡이며 기를 모음
    IEnumerator Windup(Color color, float seconds, float speed = 8f)
    {
        // 몸 주위 빛 + 머리 위 느낌표로 곧 공격한다는 걸 알림
        GameObject aura = Hostile.Glow != null
            ? SpecialAbilities.MakeSprite("WindupAura", Hostile.Glow, transform.position, 0.1f, new Color(color.r, color.g, color.b, 0.55f), "Effect", 0) : null;
        if (SpecialAbilities.SharedFx != null) SpecialAbilities.SharedFx.FloatText(transform.position + Vector3.up * 0.8f * Size, "!", color, 7f, 0f);
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
        if (Alive) sr.color = enemy.baseColor;
    }

    Vector2 DirTo(PlayerController p) => ((Vector2)(p.transform.position - transform.position)).normalized;

    // ================================================================= skills
    // 해골: 뼈 던지기
    IEnumerator BoneThrow(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        Hostile.Line(transform.position, transform.position + (Vector3)(dir * 8f), 0.35f, 0.45f, new Color(1f, 0.95f, 0.8f, 0.5f));
        yield return Windup(new Color(1f, 1f, 0.8f), 0.45f);
        if (!Alive) yield break;
        HostileProjectile b = Hostile.Shoot(transform.position, dir, 12f, Dmg * 0.8f, 0.55f, new Color(0.95f, 0.92f, 0.8f), 0.12f);
        b.spin = true;
        Hostile.Play("whoosh", 0.25f, 1.6f);
    }

    // 구울: 웅크렸다가 표시한 곳으로 도약
    IEnumerator Leap(PlayerController p)
    {
        Vector3 target = Hostile.ClampArena(p.transform.position);
        if (Hostile.IsWall(target)) yield break;
        float r = 1.8f * Size;
        Hostile.Circle(target, r, 0.65f, Danger);
        yield return Windup(Danger, 0.65f);
        if (!Alive) yield break;

        Vector3 start = transform.position;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            transform.position = Vector3.Lerp(start, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.5f;
            yield return null;
        }
        transform.position = target;
        if (!Alive) yield break;
        Hostile.HitCircle(target, r, Dmg * 1.2f);
        Hostile.Burst(target, r, new Color(0.8f, 0.5f, 0.35f, 0.8f));
        Hostile.Play("thump", 0.5f);
    }

    // 망령: 사라졌다가 플레이어 근처(표시한 곳)에 나타나며 저주 폭발
    IEnumerator Blink(PlayerController p)
    {
        Vector3 dest = Hostile.ClampArena(p.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 3f));
        if (Hostile.IsWall(dest)) yield break;
        float r = 1.7f * Size;
        Color purple = new Color(0.7f, 0.4f, 1f, 0.9f);
        Hostile.Circle(dest, r, 0.8f, purple);
        Hostile.Play("shimmer", 0.35f, 1.4f);

        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            Color c = enemy.baseColor;
            c.a = 1f - Mathf.Clamp01(t / 0.4f);
            sr.color = c;
            yield return null;
        }
        if (!Alive) yield break;
        transform.position = dest;
        sr.color = enemy.baseColor;
        Hostile.HitCircle(dest, r, Dmg);
        Hostile.Burst(dest, r, purple);
    }

    // 임프: 화염구
    IEnumerator Fireball(PlayerController p)
    {
        yield return Windup(new Color(1f, 0.6f, 0.2f), 0.4f, 12f);
        if (!Alive) yield break;
        HostileProjectile b = Hostile.Shoot(transform.position, DirTo(p), 13f, Dmg * 0.8f, 0.6f, new Color(1f, 0.55f, 0.15f), 0.14f);
        b.fiery = true;
        Hostile.Play("pew", 0.3f, 0.7f);
    }

    // 불꽃 해골: 가까이 오면 부풀어 오르다 폭발
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
        if (!Alive) yield break;
        Hostile.HitCircle(transform.position, r, Dmg * 1.6f);
        Hostile.Burst(transform.position, r, new Color(1f, 0.5f, 0.1f, 0.9f), true);
        Hostile.Play("boom", 0.6f, 1.2f);
        enemy.KillBySkill();
    }

    // 지옥견: 경로를 보여준 뒤 일직선 돌진
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
        Hostile.Play("whoosh", 0.4f, 0.9f);
        for (float t = 0f; t < 0.28f; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            Vector3 nextPos = Vector3.Lerp(start, end, t / 0.28f);
            if (Hostile.IsWall(nextPos)) break;
            transform.position = nextPos;
            if (!hit && Vector2.Distance(transform.position, p.transform.position) < 1.3f * Size) hit = p.TryHit(Dmg);
            yield return null;
        }
    }

    // 용암 골렘: 제자리에서 땅을 내려쳐 주변 충격파
    IEnumerator GroundSlam()
    {
        float r = 4f * Size;
        Hostile.Circle(transform.position, r, 1f, new Color(1f, 0.45f, 0.15f, 0.9f));
        yield return Windup(new Color(1f, 0.5f, 0.2f), 1f, 5f);
        if (!Alive) yield break;
        Hostile.HitCircle(transform.position, r, Dmg * 1.2f);
        Hostile.Burst(transform.position, r, new Color(1f, 0.45f, 0.15f, 0.85f), true);
        Hostile.Play("boom", 0.7f, 0.7f);
        Hostile.Shake(0.25f);
    }

    // 악마 기사: 검을 휘둘러 날아가는 검기
    IEnumerator SwordWave(PlayerController p)
    {
        Vector2 dir = DirTo(p);
        Hostile.Line(transform.position, transform.position + (Vector3)(dir * 14f), 1.8f, 0.6f, Danger);
        yield return Windup(Danger, 0.6f);
        if (!Alive) yield break;
        HostileProjectile b = Hostile.Shoot(transform.position, dir, 14f, Dmg, 1.1f, new Color(1f, 0.3f, 0.3f), 0.25f);
        b.transform.localScale = new Vector3(0.12f, 0.4f, 1f);
        b.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        b.pierceWalls = false;
        Hostile.Play("whoosh", 0.5f, 0.7f);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 캐릭터 고유 레벨업 스킬 (LevelShop.Signature) 과 스킬 진화의 실제 효과
// 플레이어에 붙어서, 게임 곳곳의 훅(공격 · 명중 · 처치 · 피격 · 장전 · 돌진 · 플라스크 …)을 받아 효과를 냄
// 캐릭터별 효과는 SignatureSkills.<캐릭터>.cs
public partial class SignatureSkills : MonoBehaviour
{
    public static SignatureSkills Instance { get; private set; }

    PlayerController player;
    CharacterKit kit;
    LevelShop shop;
    CharacterId who;
    readonly Dictionary<string, int> lv = new Dictionary<string, int>();

    // 이 스크립트가 주는 피해 중 (명중 효과가 다시 명중 효과를 부르지 않게)
    public static bool Dealing;
    // 화상 · 독 · 출혈 같은 지속 피해 중 (맞힐 때 효과는 평타 · 스킬에만)
    public static bool DotTick;

    public static SignatureSkills Ensure()
    {
        if (Instance != null) return Instance;
        PlayerController p = Hostile.Player != null ? Hostile.Player : FindFirstObjectByType<PlayerController>();
        if (p == null) return null;
        Instance = p.GetComponent<SignatureSkills>();
        if (Instance == null) Instance = p.gameObject.AddComponent<SignatureSkills>();
        return Instance;
    }

    void Awake()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        who = CharacterData.Selected;
        EnermyController.Killed += OnKilled;
        PlayerController.UltUsed += OnUlt;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EnermyController.Killed -= OnKilled;
        PlayerController.UltUsed -= OnUlt;
        Dealing = DotTick = false;
    }

    // 레벨업 · 진화가 바뀔 때마다 단계를 다시 읽음
    public void Refresh(LevelShop s)
    {
        shop = s;
        lv.Clear();
        for (int id = LevelShop.SigFirstId; id < LevelShop.SigFirstId + LevelShop.SigCount; id++)
        {
            string key = LevelShop.SigKey(who, id);
            if (key != null) lv[key] = s.LevelOf(id);
        }
    }

    int L(string key) => lv.TryGetValue(key, out int v) ? v : 0;
    bool E(string key) => shop != null && shop.IsEvolved(key);
    // 단계별 값: 1단계 a, 단계마다 + step (0단계면 0)
    static float V(int level, float a, float step) => level <= 0 ? 0f : a + step * (level - 1);

    CharacterKit Kit => kit != null ? kit : (kit = CharacterKit.Instance);
    // 기준 공격력 (캐릭터 평타와 같은 값)
    float Atk => player != null ? player.damage * player.damageMultiplier : 1f;
    bool Alive => player != null && !player.IsDying;

    // ================================================================= 훅 (게임 코드가 부름 · 없으면 아무것도 안 함)
    public static float Outgoing(GameObject target, float dmg) => Instance != null && target != null ? Instance.OutgoingMul(target, dmg) : dmg;
    public static void Hit(GameObject target, float dmg, bool killed) { if (Instance != null && target != null) Instance.OnHit(target, dmg, killed); }
    public static float Taken(float dmg) => Instance != null ? Instance.TakenMul(dmg) : dmg;
    public static void Hurt(float taken) { if (Instance != null) Instance.OnHurt(taken); }
    public static void Attacked(Vector3 start, Vector2 dir) { if (Instance != null) Instance.OnAttack(start, dir); }
    public static float MoveMul => Instance != null ? Instance.MoveMultiplier() : 1f;
    public static float AttackRateMul => Instance != null ? Instance.RateMultiplier() : 1f;
    public static void CoinPicked(int n) { if (Instance != null) Instance.OnCoins(n); }

    float OutgoingMul(GameObject target, float dmg)
    {
        float mul = 1f;
        switch (who)
        {
            case CharacterId.Gunner: mul *= GunnerOutgoing(target); break;
            case CharacterId.Swordsman: mul *= SwordOutgoing(target); break;
            case CharacterId.Rogue: mul *= RogueOutgoing(target); break;
            case CharacterId.Archer: mul *= ArcherOutgoing(target); break;
            case CharacterId.Alchemist: mul *= AlchemistOutgoing(target); break;
        }
        return dmg * mul;
    }

    void OnHit(GameObject target, float dmg, bool killed)
    {
        bool proc = !Dealing && !DotTick;
        switch (who)
        {
            case CharacterId.Gunner: GunnerHit(target, dmg, killed, proc); break;
            case CharacterId.Swordsman: SwordHit(target, dmg, killed, proc); break;
            case CharacterId.Rogue: RogueHit(target, dmg, killed, proc); break;
            case CharacterId.Archer: ArcherHit(target, dmg, killed, proc); break;
            case CharacterId.Alchemist: AlchemistHit(target, dmg, killed, proc); break;
        }
    }

    float TakenMul(float dmg)
    {
        switch (who)
        {
            case CharacterId.Swordsman: dmg = SwordTaken(dmg); break;
            case CharacterId.Rogue: dmg = RogueTaken(dmg); break;
        }
        return dmg;
    }

    void OnHurt(float taken)
    {
        if (who == CharacterId.Gunner) GunnerHurt(taken);
        else if (who == CharacterId.Swordsman) SwordHurt(taken);
    }

    void OnAttack(Vector3 start, Vector2 dir)
    {
        if (who == CharacterId.Gunner) GunnerAttack();
        else if (who == CharacterId.Rogue) RogueAttack(start, dir);
    }

    void OnKilled(Vector3 pos)
    {
        if (!Alive) return;
        if (who == CharacterId.Gunner) GunnerKilled(pos);
        else if (who == CharacterId.Swordsman) SwordKilled(pos);
    }

    void OnUlt()
    {
        if (!Alive) return;
        if (who == CharacterId.Gunner) GunnerUlt();
    }

    float MoveMultiplier()
    {
        float m = 1f;
        if (who == CharacterId.Gunner) m *= GunnerMove();
        else if (who == CharacterId.Swordsman) m *= SwordMove();
        else if (who == CharacterId.Archer) m *= ArcherMove();
        return m;
    }

    float RateMultiplier() => who == CharacterId.Swordsman ? SwordRate() : 1f;

    void Update()
    {
        if (!Alive || Time.timeScale == 0f) return;
        switch (who)
        {
            case CharacterId.Gunner: GunnerTick(); break;
            case CharacterId.Swordsman: SwordTick(); break;
            case CharacterId.Rogue: RogueTick(); break;
            case CharacterId.Archer: ArcherTick(); break;
            case CharacterId.Alchemist: AlchemistTick(); break;
        }
        CommonTick();
    }

    // ================================================================= 공통 진화 (공용 카드끼리)
    // 황금 시대: 코인을 주울 때 경험치 · 40개마다 황금 파동 / 불멸의 육체: 체력이 가득하면 3초마다 심장 파동
    int goldCount;
    float heartAt;

    void OnCoins(int n)
    {
        if (n <= 0 || !E("ce.gold") || player == null) return;
        LevelShop ls = Cache<LevelShop>.Get;
        if (ls != null) ls.GainExp(player, 2f * n);
        goldCount += n;
        if (goldCount < 40) return;
        goldCount = 0;
        Vector3 p = transform.position;
        Circle(p, 7f, Atk * 3f, 2f);
        Fx.Spawn("fx_shock", p, 14f, new Color(1f, 0.85f, 0.3f, 0.9f), 18f);
        Fx.Spawn("fx_soulburst", p, 6f, new Color(1f, 0.9f, 0.4f), 18f);
        ShockRing.Spawn(p, 0.5f, 8f, 0.45f, new Color(1f, 0.85f, 0.35f, 0.9f), 0.3f);
        Hostile.Play("chime", 0.8f, 1.3f);
    }

    void CommonTick()
    {
        if (!E("ce.body") || player == null) return;
        if (player.PlayerHealth < player.PlayerMaxHealth - 0.01f) { heartAt = Time.time + 3f; return; }
        if (Time.time < heartAt) return;
        heartAt = Time.time + 3f;
        Vector3 p = transform.position;
        Circle(p, 5f, player.PlayerMaxHealth * 0.25f, 3f);
        ShockRing.Spawn(p, 0.4f, 6f, 0.4f, new Color(1f, 0.35f, 0.4f, 0.9f), 0.3f);
        Fx.Spawn("fx_shock", p, 11f, new Color(1f, 0.4f, 0.45f, 0.8f), 20f);
        Hostile.Play("thump", 0.6f, 0.8f);
    }

    // ================================================================= 도우미
    // 이 스크립트의 피해 (명중 효과가 연쇄되지 않게)
    public void Deal(GameObject g, float dmg, Vector3 dir, float knock)
    {
        if (g == null) return;
        bool was = Dealing;
        Dealing = true;
        try { Specials.Damage(g, dmg, dir, knock); }
        finally { Dealing = was; }
    }

    static readonly List<Collider2D> hits = new List<Collider2D>(64);

    // 반지름 안의 적 · 보스 모두에게
    public int Circle(Vector3 pos, float r, float dmg, float knock, System.Action<Collider2D> each = null)
    {
        int n = 0;
        List<Collider2D> list = new List<Collider2D>(Specials.Overlap(pos, r, hits));
        foreach (Collider2D c in list)
        {
            if (c == null || !IsFoe(c)) continue;
            if (dmg > 0f) Deal(c.gameObject, dmg, (c.transform.position - pos).normalized, knock);      // 0이면 기절 · 표식만
            if (c != null) each?.Invoke(c);
            n++;
        }
        return n;
    }

    static bool IsFoe(Collider2D c) => c.CompareTag("enermy") || c.CompareTag("boss");
    static bool IsBoss(GameObject g) => g.CompareTag("boss") || g.GetComponent<MidBossMark>() != null;

    // 가까운 적 (살아 있는) 여럿
    public static List<Transform> Nearest(Vector3 pos, float r, int count, GameObject except = null)
    {
        List<(float d, Transform t)> all = new List<(float, Transform)>();
        foreach (Collider2D c in Specials.Overlap(pos, r, hits))
        {
            if (c == null || !IsFoe(c) || c.gameObject == except) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && e.IsDead) continue;
            all.Add(((c.transform.position - pos).sqrMagnitude, c.transform));
        }
        all.Sort((a, b) => a.d.CompareTo(b.d));
        List<Transform> res = new List<Transform>();
        for (int i = 0; i < all.Count && res.Count < count; i++) res.Add(all[i].t);
        return res;
    }

    // 도트 그림을 입힌 투사체 (Bullet 판정 그대로, 스킬 총알이라 게이지를 채우지 않음)
    public Bullet Shot(Vector3 start, Vector2 dir, float dmg, int pene, float speed, float range, string fx, float size, Color tint, bool spin = false)
    {
        if (player == null) return null;
        Bullet b = player.CreateBullet(start, dir, dmg, pene, 0f, true);
        if (b == null) return null;
        b.speed = speed;
        b.lifetime = range > 0f ? range / speed : 3f;
        SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
        Sprite[] f = Fx.Frames(fx);
        if (sr != null && f.Length > 0)
        {
            sr.sprite = f[0];
            sr.color = tint;
            b.transform.localScale = Vector3.one * (size / f[0].bounds.size.y);
            FrameLoop loop = b.gameObject.AddComponent<FrameLoop>();
            loop.frames = f;
            loop.spin = spin ? -900f : 0f;
        }
        return b;
    }

    // 지속 장판 (원 또는 선): tick 마다 onTick, 끝나면 onEnd. 그림은 반복 이펙트
    // ground: 그림을 바닥 층에 (캐릭터 · 적을 가리지 않게)
    public SigZone Zone(Vector3 pos, float r, float life, float tick, System.Action<SigZone> onTick, string fx = null, Color? tint = null, float fxSize = 0f, bool ground = false)
    {
        GameObject go = new GameObject("SigZone");
        go.transform.position = pos;
        SigZone z = go.AddComponent<SigZone>();
        z.radius = r;
        z.life = life;
        z.tick = tick;
        z.onTick = onTick;
        if (fx != null)
        {
            FxAnim a = Fx.Play(fx, pos, fxSize > 0f ? fxSize : r * 2.2f, tint ?? Color.white, 8f, 0f, ground ? 8 : 3, true, life, ground ? "Background" : "Effect");
            if (a != null) { a.transform.SetParent(go.transform, true); z.visual = a; }
        }
        return z;
    }

    // 장판 안의 적 (원이면 반지름, 선이면 선분과의 거리)
    public List<Collider2D> Inside(SigZone z)
    {
        List<Collider2D> res = new List<Collider2D>();
        Vector3 c = z.segment ? (z.a + z.b) * 0.5f : z.transform.position;
        float r = z.segment ? Vector3.Distance(z.a, z.b) * 0.5f + z.radius : z.radius;
        foreach (Collider2D col in Specials.Overlap(c, r, hits))
        {
            if (col == null || !IsFoe(col)) continue;
            if (z.segment && Hostile.DistanceToSegment(col.transform.position, z.a, z.b) > z.radius) continue;
            res.Add(col);
        }
        return res;
    }

    // 하늘에서 떨어지는 것 (그림이 위에서 내려와 닿는 순간 범위 피해)
    IEnumerator Drop(Vector3 at, string fx, float size, Color tint, float r, float dmg, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        FxAnim a = Fx.Play(fx, at + Vector3.up * 7f, size, tint, 16f, 0f, 20, true, 0.4f);
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            if (a != null) a.transform.position = Vector3.Lerp(at + Vector3.up * 7f, at, t / 0.25f);
            yield return null;
        }
        if (a != null) Destroy(a.gameObject);
        Circle(at, r, dmg, 0.6f);
        Fx.Spawn("fx_spark", at, r * 1.6f, tint, 22f);
    }

    void Heal(float amount)
    {
        if (player == null || amount <= 0f) return;
        float before = player.PlayerHealth;
        player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + amount);
        if (player.PlayerHealth > before + 0.5f && SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.FloatText(transform.position, "+" + Mathf.RoundToInt(player.PlayerHealth - before), new Color(0.5f, 1f, 0.5f), 3.5f, 0.3f);
    }

    // 적 기절 · 감속 (보스는 감속만 짧게)
    static void Stun(GameObject g, float seconds, float bossMul = 0.5f)
    {
        EnermyController e = g != null ? g.GetComponent<EnermyController>() : null;
        if (e != null) e.Slow(0f, seconds);
    }

    // 표식 (몇 초 동안 받는 피해 +): 위협 · 표식 전염 · 덤불 · 빙결 등에서 같이 씀
    readonly Dictionary<GameObject, (float until, float mul)> marks = new Dictionary<GameObject, (float, float)>();

    void Mark(GameObject g, float seconds, float mul)
    {
        if (g == null) return;
        if (marks.TryGetValue(g, out var m) && m.until > Time.time) mul = Mathf.Max(mul, m.mul);
        marks[g] = (Time.time + seconds, mul);
    }

    float MarkMul(GameObject g) => g != null && marks.TryGetValue(g, out var m) && m.until > Time.time ? m.mul : 1f;
    bool Marked(GameObject g) => g != null && marks.TryGetValue(g, out var m) && m.until > Time.time;

    float pruneAt;
    void PruneMarks()
    {
        if (Time.time < pruneAt) return;
        pruneAt = Time.time + 2f;
        List<GameObject> dead = new List<GameObject>();
        foreach (var kv in marks) if (kv.Key == null || kv.Value.until < Time.time) dead.Add(kv.Key);
        foreach (GameObject g in dead) marks.Remove(g);
    }

    void LateUpdate() => PruneMarks();
}

// 스킬이 남기는 장판 (원 또는 선분)
public class SigZone : MonoBehaviour
{
    public float radius, life, tick;
    public bool segment;
    public Vector3 a, b;
    public System.Action<SigZone> onTick, onEnd;
    public FxAnim visual;
    public float age;
    float next;

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life)
        {
            onEnd?.Invoke(this);
            Destroy(gameObject);
            return;
        }
        if (tick > 0f && age >= next)
        {
            next = age + tick;
            onTick?.Invoke(this);
        }
    }

    public bool Contains(Vector3 p) => segment ? Hostile.DistanceToSegment(p, a, b) <= radius : (p - transform.position).sqrMagnitude <= radius * radius;
}

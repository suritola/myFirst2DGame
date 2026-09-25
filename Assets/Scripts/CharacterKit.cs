using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 거너가 아닌 캐릭터: 스탯 배율 · 새 몸 그림(대기/달리기) · 평타 · 우클릭 스킬
// PlayerController 가 평타 · 우클릭 · 이동을 이 컴포넌트에 맡김 (거너면 붙지 않음)
public class CharacterKit : MonoBehaviour
{
    public static CharacterKit Instance { get; private set; }

    public CharacterId Id { get; private set; }
    CharacterDef def;
    PlayerController player;
    SpriteRenderer body;
    Sprite[] frames;
    float animT;
    Vector3 lastPos;

    // 누르고 있는 우클릭 (검사 회전 베기 · 연금술사 대폭발)
    bool charging;
    float charge;
    LineRenderer ring;

    // 도적 출혈 돌진 (돌진하는 동안은 평타 · 이동 입력을 막음)
    float dashUntil;
    public bool Dashing => Time.time < dashUntil;

    // 검사 평타: 위→아래, 아래→위 번갈아 휘두름
    bool swingAlt;

    public bool Busy => charging || Dashing;
    public float AttackSpeedMul => 1f;
    public float MoveMul => 1f;
    public string WeaponName => Loc.T(def.weapon);
    // 탄창이 있는 캐릭터 (도적 표창 6발): 다 쓰면 거너처럼 재장전
    public bool UsesAmmo => def.mag > 0;

    public static void Attach(PlayerController p)
    {
        if (p == null || CharacterData.IsGunner || p.GetComponent<CharacterKit>() != null) return;
        p.gameObject.AddComponent<CharacterKit>();
    }

    void Awake()
    {
        Instance = this;
        Id = CharacterData.Selected;
        def = CharacterData.Current;
        player = GetComponent<PlayerController>();
        body = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // 거너 기준 스탯 배율
        player.PlayerMaxHealth *= def.hp;
        player.PlayerHealth = player.PlayerMaxHealth;
        player.damage *= def.damage;
        player.ShootSpeed /= Mathf.Max(0.1f, def.attackSpeed);
        player.speed *= def.move;
        if (def.mag > 0) player.MaxBullet = def.mag;          // (거너의 mag 는 표시용, 실제 탄창은 인스펙터 값)
        player.NowBullet = player.MaxBullet;

        // 새 몸 그림 (애니메이터는 거너 그림을 쓰므로 끔)
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
        List<Sprite> list = new List<Sprite>(Resources.LoadAll<Sprite>("Characters/" + def.body));
        list.Sort((a, b) => Index(a.name).CompareTo(Index(b.name)));
        frames = list.ToArray();
        if (frames.Length > 0) body.sprite = frames[0];
        lastPos = transform.position;
    }

    static int Index(string n)
    {
        int i = n.LastIndexOf('_');
        return i >= 0 && int.TryParse(n.Substring(i + 1), out int v) ? v : 0;
    }

    void LateUpdate()
    {
        // 대기 2장 (느리게) · 달리기 4장
        if (frames != null && frames.Length >= 6)
        {
            bool moving = (transform.position - lastPos).sqrMagnitude > 0.0004f && Time.timeScale > 0f;
            animT += Time.deltaTime;
            body.sprite = moving ? frames[2 + (int)(animT * 10f) % 4] : frames[(int)(animT * 2f) % 2];
        }
        lastPos = transform.position;
    }

    // ================================================================= 공통
    SpecialAbilities Special => player.special;
    float Damage => player.damage * player.damageMultiplier;
    Vector3 Mouse
    {
        get
        {
            if (Special != null) return Special.MouseWorldPos;
            Vector3 m = Camera.main.ScreenToWorldPoint(GameInput.MousePosition);
            m.z = 0f;
            return m;
        }
    }

    static void Play(string sound, float vol = 1f, float pitch = 1f) => Hostile.Play(sound, vol, pitch);

    // 반지름 안의 적 · 보스 모두에게
    public static int DamageCircle(Vector3 pos, float radius, float damage, float knock)
    {
        int n = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Specials.Damage(c.gameObject, damage, (c.transform.position - pos).normalized, knock);
            n++;
        }
        return n;
    }

    // 도트 이펙트를 입힌 투사체 (Bullet 판정 그대로)
    Bullet Projectile(Vector3 start, Vector2 dir, float damage, int pene, float speed, float range, string fx, float size, Color tint, bool spin)
    {
        Bullet b = player.CreateBullet(start, dir, damage, pene, player.getHP, false);
        if (b == null) return null;
        b.speed = speed;
        b.lifetime = range > 0f ? range / speed : 6f;
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

    IEnumerable<Vector2> Spread(Vector2 dir, int shots, float step)
    {
        for (int i = 0; i < shots; i++)
            yield return Quaternion.Euler(0f, 0f, (i - (shots - 1) * 0.5f) * step) * dir;
    }

    // ================================================================= 평타
    public void Attack()
    {
        Vector3 target = Mouse;
        player.FaceTowards(target);
        Vector3 start = player.MuzzlePosition;
        Vector2 dir = ((Vector2)(target - start)).normalized;
        int shots = Mathf.Max(1, player.multiShot);
        float dmg = Damage * player.MultiShotDamageRate(shots);
        PlayerLook.Fired(-1);

        switch (Id)
        {
            case CharacterId.Swordsman:
                Swing(dir, shots);
                break;
            case CharacterId.Rogue:
                foreach (Vector2 d in Spread(dir, shots, 8f))
                    Projectile(start, d, dmg, player.pene, 45f, 0f, "fx_shuriken", 1.1f, Color.white, true);
                Play("whoosh", 0.35f, 1.8f);
                break;
            case CharacterId.Archer:
                foreach (Vector2 d in Spread(dir, shots, 6f))
                    Projectile(start, d, dmg, player.pene + 1, 60f, 0f, "fx_arrow", 0.45f, Color.white, false);
                Play("pew", 0.5f, 0.7f);
                break;
            case CharacterId.Alchemist:
                for (int i = 0; i < shots; i++)
                {
                    Vector3 land = ClampRange(start, target, def.range) + (i == 0 ? Vector3.zero : (Vector3)(Random.insideUnitCircle * 1.6f));
                    FlaskLob.Throw(start, land, 0.4f, 0.8f, Color.white, (p) =>
                    {
                        float r = 1.8f * CatalystMul;
                        DamageCircle(p, r, dmg * 1.6f, 1.2f);
                        Fx.Play("fx_alchemyblast", p, r * 2.2f, Color.white, 18f);
                        Play("boom", 0.35f, 1.5f);
                    });
                }
                Play("whoosh", 0.4f, 1.3f);
                break;
        }
    }

    // 검사 평타: 장검을 크게 휘둘러 앞쪽 부채꼴(반지름 = 사거리)의 적을 직접 벰
    // 여러 발 강화를 얻으면 휘두르는 폭이 넓어짐
    void Swing(Vector2 dir, int shots)
    {
        if (dir.sqrMagnitude < 0.01f) dir = body.flipX ? Vector2.left : Vector2.right;
        float reach = def.range;
        float half = Mathf.Min(180f, 70f + 12f * (shots - 1));
        swingAlt = !swingAlt;
        bool left = dir.x < 0f;
        PlayerLook.Swing(swingAlt);

        Vector3 origin = transform.position;
        int hits = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(origin, reach))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector2 to = c.transform.position - origin;
            if (to.sqrMagnitude > 0.25f && Vector2.Angle(dir, to) > half) continue;
            Specials.Damage(c.gameObject, Damage, to.normalized, 1.4f);
            Fx.Play("fx_sparkle", c.transform.position, 0.9f, new Color(0.8f, 0.9f, 1f), 24f);
            hits++;
        }

        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        FxAnim a = Fx.Play("fx_swordswing", origin, reach * 2.03f, new Color(1f, 1f, 1f, 0.9f), 24f, rot, 15);
        if (a != null) a.sr.flipY = left ^ !swingAlt;
        if (shots > 1)
        {
            // 넓어진 폭: 양옆으로 한 번씩 더 그림
            FxAnim b = Fx.Play("fx_swordswing", origin, reach * 2.03f, new Color(0.8f, 0.9f, 1f, 0.5f), 24f, rot + (half - 70f), 14);
            FxAnim c = Fx.Play("fx_swordswing", origin, reach * 2.03f, new Color(0.8f, 0.9f, 1f, 0.5f), 24f, rot - (half - 70f), 14);
            if (b != null) b.sr.flipY = a != null && a.sr.flipY;
            if (c != null) c.sr.flipY = a != null && a.sr.flipY;
        }
        Play("slash", 0.8f, hits > 0 ? 0.9f : 1.15f);
        Play("whoosh", 0.5f, 0.8f);
        if (hits >= 3) Hostile.Shake(0.08f);
    }

    public float CatalystMul => Special != null && Special.Has(SpecialAbilities.KitCatalyst) ? (Special.IsEvolved(SpecialAbilities.KitCatalyst) ? 1.5f : 1.3f) : 1f;

    static Vector3 ClampRange(Vector3 from, Vector3 to, float range)
    {
        Vector3 d = to - from;
        return range > 0f && d.magnitude > range ? from + d.normalized * range : to;
    }

    // ================================================================= 우클릭 스킬
    public void UpdateUlt(SkillGauge gauge)
    {
        bool full = gauge != null && gauge.IsFull();
        switch (Id)
        {
            case CharacterId.Swordsman:
            case CharacterId.Alchemist:
                UpdateCharged(gauge, full);
                break;
            case CharacterId.Rogue:
                if (GameInput.UltDown && full && !Dashing)
                {
                    StartCoroutine(BleedDash(((Vector2)(Mouse - transform.position)).normalized));
                    Spend(gauge);
                }
                break;
            case CharacterId.Archer:
                if (GameInput.UltDown && full)
                {
                    StartCoroutine(ArrowRain(Mouse));
                    Spend(gauge);
                }
                break;
        }
    }

    void Spend(SkillGauge gauge)
    {
        gauge.ResetSkillPoint();
        player.RaiseUltUsed();
    }

    void UpdateCharged(SkillGauge gauge, bool full)
    {
        if (!charging && GameInput.UltDown && full)
        {
            charging = true;
            charge = 0f;
            if (ring == null) ring = Hostile.NewLine("UltRing", Color.white, 0.14f, 25);
            ring.loop = true;
            ring.enabled = true;
            Play("hum", 0.5f, 0.8f);
        }
        if (!charging) return;

        charge = Mathf.Min(1f, charge + Time.deltaTime / 1.5f);
        bool sword = Id == CharacterId.Swordsman;
        Vector3 at = sword ? transform.position : ClampRange(transform.position, Mouse, 14f);
        float radius = sword ? 5f : (2f + 4f * charge) * CatalystMul;
        Hostile.SetArc(ring, at, radius, 0f, 360f);
        Color c = sword ? new Color(0.55f, 0.75f, 1f) : new Color(0.55f, 1f, 0.45f);
        ring.startColor = ring.endColor = new Color(c.r, c.g, c.b, 0.35f + 0.5f * charge * (0.7f + 0.3f * Mathf.Sin(Time.time * 18f)));
        ring.startWidth = ring.endWidth = 0.1f + 0.15f * charge;
        if (sword && Random.value < 0.4f) Fx.Play("fx_sparkle", transform.position + (Vector3)(Random.insideUnitCircle * 1.5f), 0.6f, c, 18f);

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        charging = false;
        ring.enabled = false;
        if (sword)
        {
            // 회전 베기: 누른 만큼 강해짐
            float dmg = Damage * (3f + 6f * charge);
            DamageCircle(transform.position, radius, dmg, 2.5f);
            Fx.Play("fx_spinslash", transform.position, radius * 2.4f, Color.white, 22f);
            Fx.Play("fx_shock", transform.position, radius * 2.2f, new Color(0.6f, 0.8f, 1f, 0.8f), 20f);
            Play("slash", 1f, 0.8f);
            Play("whoosh", 0.8f, 0.6f);
        }
        else
        {
            float dmg = Damage * (4f + 8f * charge);
            FlaskLob.Throw(player.MuzzlePosition, at, 0.55f, 1.6f, new Color(0.8f, 1f, 0.7f), (p) =>
            {
                DamageCircle(p, radius, dmg, 3f);
                Fx.Play("fx_alchemyblast", p, radius * 2.4f, Color.white, 16f);
                if (Special != null) Special.SpawnZone(p, radius * 0.7f, 4f, Damage * 0.5f, new Color(0.45f, 1f, 0.35f, 0.7f));
                Play("boom", 1f, 0.9f);
            });
        }
        Spend(gauge);
    }

    // 도적 출혈 돌진: 무적 상태로 마우스 쪽으로 빠르게 돌진, 지나간 적마다 한 번 베고 출혈
    IEnumerator BleedDash(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.01f) dir = body.flipX ? Vector2.left : Vector2.right;
        const float dist = 9f, time = 0.22f, width = 1.4f;
        Vector3 from = transform.position;
        // 벽 · 경기장 끝 앞에서 멈춤
        float len = dist;
        for (float d = 0.5f; d <= dist; d += 0.5f)
        {
            Vector3 p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p) || (Hostile.ClampArena(p) - p).sqrMagnitude > 0.01f) { len = d - 0.5f; break; }
        }
        Vector3 to = from + (Vector3)(dir * len);

        dashUntil = Time.time + time;
        player.GrantInvincibility(time + 0.35f);
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Fx.Play("fx_stealth", from, 3f, Color.white, 16f);
        Play("whoosh", 1f, 1.4f);
        Play("slash", 0.7f, 1.5f);

        HashSet<Collider2D> hit = new HashSet<Collider2D>();
        int ghosts = 0;
        Vector3 prev = from;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            Vector3 p = Vector3.Lerp(from, to, t / time);
            transform.position = p;
            Cut(prev, p, width, dir, hit);
            prev = p;
            // 보라 잔상과 그림자 줄기
            if (t >= ghosts * 0.03f)
            {
                ghosts++;
                GameObject g = SpecialAbilities.MakeSprite("DashGhost", body.sprite, p, 1f, new Color(0.6f, 0.35f, 0.9f, 0.55f), "Character", -1);
                g.transform.localScale = transform.lossyScale;
                g.GetComponent<SpriteRenderer>().flipX = body.flipX;
                g.AddComponent<FadeOut>().duration = 0.3f;
                Fx.Play("fx_shadowdash", p - (Vector3)(dir * 1.2f), 1.2f, Color.white, 24f, rot, 13);
            }
            yield return null;
        }
        transform.position = to;
        Cut(prev, to, width, dir, hit);
        Fx.Play("fx_stealth", to, 2.4f, new Color(1f, 0.7f, 0.8f), 18f);
        if (hit.Count > 0) Hostile.Shake(0.1f);
    }

    // 이번 프레임에 지나간 구간 전체 (프레임이 길어도 건너뛰지 않게)
    void Cut(Vector3 a, Vector3 b, float width, Vector2 dir, HashSet<Collider2D> hit)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + width))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > width) continue;
            if (!hit.Add(c)) continue;
            Specials.Damage(c.gameObject, Damage * 1.5f, dir, 1f);
            Bleed.Apply(c.gameObject, Damage * 1.2f, 4f);
            Fx.Play("fx_bleed", c.transform.position, 1.4f, Color.white, 16f);
            Play("crack", 0.35f, 1.4f);
        }
    }

    // 화살비: 마우스 둘레에 화살이 1.2초 동안 쏟아짐
    IEnumerator ArrowRain(Vector3 center)
    {
        const float radius = 4f;
        Hostile.Circle(center, radius, 0.4f, new Color(0.6f, 1f, 0.5f, 0.6f));
        Play("whoosh", 0.8f, 0.9f);
        for (int i = 0; i < 20; i++)
        {
            Vector3 at = center + (Vector3)(Random.insideUnitCircle * radius);
            Fx.Play("fx_arrowrain", at + Vector3.up * 1f, 2f, Color.white, 18f);
            DamageCircle(at, 1.3f, Damage * 2.5f, 0.5f);
            if (i % 4 == 0) Play("pew", 0.3f, 0.6f);
            yield return new WaitForSeconds(0.06f);
        }
    }
}

// 출혈: 몇 초 동안 0.25초마다 피해, 핏방울이 떨어짐 (다시 걸리면 시간 갱신)
public class Bleed : MonoBehaviour
{
    public float dps;
    public float until;
    float tick, drip;

    public static void Apply(GameObject target, float dps, float duration)
    {
        if (target == null) return;
        Bleed b = target.GetComponent<Bleed>();
        if (b == null) b = target.AddComponent<Bleed>();
        b.dps = Mathf.Max(b.dps, dps);
        b.until = Time.time + duration;
    }

    void Update()
    {
        if (Time.time > until) { Destroy(this); return; }
        drip += Time.deltaTime;
        if (drip >= 0.25f)
        {
            drip = 0f;
            Fx.Play("fx_bleed", transform.position + (Vector3)(Random.insideUnitCircle * 0.4f), 1.4f, Color.white, 14f);
        }
        tick += Time.deltaTime;
        if (tick >= 0.25f)
        {
            tick = 0f;
            Specials.Damage(gameObject, dps * 0.25f, Vector3.zero, 0f);
        }
    }
}

// 투사체 · 이펙트 도트를 돌려 가며 보여줌 (필요하면 빙글빙글)
public class FrameLoop : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 14f;
    public float spin;
    SpriteRenderer sr;
    float t;

    void Start() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        t += Time.deltaTime;
        sr.sprite = frames[(int)(t * fps) % frames.Length];
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
    }
}

// 포물선으로 날아가 떨어진 자리에서 터지는 플라스크
public class FlaskLob : MonoBehaviour
{
    Vector3 from, to;
    float time, t;
    System.Action<Vector3> onLand;

    public static void Throw(Vector3 from, Vector3 to, float time, float size, Color tint, System.Action<Vector3> onLand)
    {
        Sprite[] f = Fx.Frames("fx_flask");
        GameObject go = new GameObject("Flask");
        go.transform.position = from;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = 8;
        sr.color = tint;
        if (f.Length > 0)
        {
            sr.sprite = f[0];
            go.transform.localScale = Vector3.one * (size / f[0].bounds.size.y);
            FrameLoop loop = go.AddComponent<FrameLoop>();
            loop.frames = f;
            loop.spin = -600f;
        }
        FlaskLob l = go.AddComponent<FlaskLob>();
        l.from = from;
        l.to = to;
        l.time = time;
        l.onLand = onLand;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / time);
        Vector3 p = Vector3.Lerp(from, to, k);
        p.y += Mathf.Sin(k * Mathf.PI) * 2f;
        transform.position = p;
        if (k < 1f) return;
        onLand?.Invoke(to);
        Destroy(gameObject);
    }
}

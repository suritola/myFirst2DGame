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

    // 궁수 활 시위: 누르고 있는 동안 draw 0 → 1
    public bool DrawsBow => Id == CharacterId.Archer;
    public bool BowFired { get; private set; }
    public float Draw => drawing ? draw : 0f;
    bool drawing;
    float draw;
    LineRenderer bowLine;
    bool fullPlayed;

    // 캐릭터 전용 레벨업 카드 4장의 레벨 (LevelShop.Kits, 0 = 아직 없음). 기본 스펙이 아닌 특수 능력
    //   검사: 0 검기 · 1 흡혈 베기 · 2 쳐내기 · 3 칼바람
    //   도적: 0 도탄 표창 · 1 갈고리 표창 · 2 그림자 분신 · 3 표창 폭풍
    //   궁수: 0 분열 화살 · 1 메아리 화살 · 2 바람 걸음 · 3 가시 덤불
    //   연금술사: 0 연쇄 반응 · 1 급속 냉동 · 2 호문쿨루스 · 3 파편 플라스크
    public readonly int[] card = new int[4];

    // 캐릭터 능력치 상점이 올리는 값 (Shop.Kits)
    [HideInInspector] public float reachMul = 1f;     // 검사 긴 칼날: 베기 사거리 · 손에 든 검 크기
    [HideInInspector] public float arcBonus;          // 검사 넓은 베기: 부채꼴 반각 +
    [HideInInspector] public float ultMul = 1f;       // 우클릭 강화 (회전 베기 · 출혈 돌진 · 화살비 · 대폭발)
    // 스킬 강화 상점의 첫 줄(캐릭터 우클릭) 위력 · 효과 범위까지 곱한 값
    float UltMul => ultMul * (Special != null ? Special.UltPower(SpecialAbilities.PistolUlt) * (1f + 0.1f * Special.UltTrait(SpecialAbilities.PistolUlt)) : 1f);
    [HideInInspector] public float blastMul = 1f;     // 연금술사 넓은 폭발: 평타 폭발 범위

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
        if (homunculus != null) Destroy(homunculus);
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
                    Shuriken(start, d, dmg, card[0]);
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
                    ThrowReagent(start, land, dmg);
                }
                Play("glassclink", 0.6f, Random.Range(0.9f, 1.15f));
                Play("whoosh", 0.25f, 1.5f);
                break;
        }
    }

    // 검사 평타: 장검을 크게 휘둘러 앞쪽 부채꼴(반지름 = 사거리)의 적을 직접 벰
    // 여러 발 강화를 얻으면 휘두르는 폭이 넓어짐
    void Swing(Vector2 dir, int shots)
    {
        if (dir.sqrMagnitude < 0.01f) dir = body.flipX ? Vector2.left : Vector2.right;
        float reach = def.range * reachMul;
        float half = Mathf.Min(180f, 50f + arcBonus + 12f * (shots - 1));
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
        SwingCards(origin, dir, reach, half, hits);

        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        FxAnim a = Fx.Play("fx_swordswing", origin, reach * 2.03f, new Color(1f, 1f, 1f, 0.9f), 24f, rot, 15);
        if (a != null) a.sr.flipY = left ^ !swingAlt;
        if (half > 52f)
        {
            // 넓어진 폭: 양옆으로 한 번씩 더 그림
            FxAnim b = Fx.Play("fx_swordswing", origin, reach * 2.03f, new Color(0.8f, 0.9f, 1f, 0.5f), 24f, rot + (half - 50f), 14);
            FxAnim c = Fx.Play("fx_swordswing", origin, reach * 2.03f, new Color(0.8f, 0.9f, 1f, 0.5f), 24f, rot - (half - 50f), 14);
            if (b != null) b.sr.flipY = a != null && a.sr.flipY;
            if (c != null) c.sr.flipY = a != null && a.sr.flipY;
        }
        Play("slash", 0.8f, hits > 0 ? 0.9f : 1.15f);
        Play("whoosh", 0.5f, 0.8f);
        if (hits >= 3) Hostile.Shake(0.08f);
    }

    // canDraw: 지금 당길 수 있는지 (전용 무기 · 우클릭 중이면 아님), fullTime: 가득 당기는 데 걸리는 시간
    public void UpdateBow(bool canDraw, float fullTime)
    {
        BowFired = false;
        if (!drawing)
        {
            if (canDraw && GameInput.FireHeld)
            {
                drawing = true;
                draw = 0f;
                fullPlayed = false;
                Play("bowdraw", 0.45f, 1f);
            }
            else
            {
                if (bowLine != null) bowLine.enabled = false;
                return;
            }
        }

        draw = Mathf.Min(1f, draw + Time.deltaTime / Mathf.Max(0.2f, fullTime));
        Vector3 start = player.MuzzlePosition;
        Vector3 target = Mouse;
        player.FaceTowards(target);
        Vector2 dir = ((Vector2)(target - start)).normalized;
        PlayerLook.Draw(draw);

        // 겨누는 선: 당길수록 길고 밝아짐, 가득 당기면 금색으로 반짝
        if (bowLine == null) bowLine = Hostile.NewLine("BowDraw", Color.white, 0.08f, 17);
        bowLine.enabled = true;
        bowLine.positionCount = 2;
        bowLine.SetPosition(0, start);
        bowLine.SetPosition(1, start + (Vector3)(dir * (2f + 7f * draw)));
        Color c = draw >= 1f ? new Color(1f, 0.85f, 0.3f, 0.6f + 0.3f * Mathf.Sin(Time.time * 20f)) : new Color(0.7f, 1f, 0.6f, 0.2f + 0.5f * draw);
        bowLine.startColor = bowLine.endColor = c;
        bowLine.startWidth = bowLine.endWidth = 0.05f + 0.1f * draw;
        if (draw >= 1f && !fullPlayed)
        {
            fullPlayed = true;
            Play("ding", 0.5f, 1.5f);
            Fx.Play("fx_sparkle", start, 1f, new Color(1f, 0.9f, 0.5f), 20f);
        }

        if (GameInput.FireHeld && canDraw) return;

        // 발사: 피해 30% → 250%, 화살 속도 35 → 95 (연타보다 끝까지 당기는 쪽이 초당 피해가 높게)
        drawing = false;
        bowLine.enabled = false;
        PlayerLook.Draw(0f);
        PlayerLook.Fired(-1);
        float power = 0.3f + 2.2f * draw;
        float speed = 35f + 60f * draw;
        int shots = Mathf.Max(1, player.multiShot);
        float dmg = Damage * player.MultiShotDamageRate(shots) * power;
        foreach (Vector2 d in Spread(dir, shots, 6f))
        {
            Bullet b = Projectile(start, d, dmg, player.pene + 1, speed, 0f, "fx_arrow", 0.4f + 0.25f * draw, draw >= 1f ? new Color(1f, 0.95f, 0.6f) : Color.white, false);
            if (b == null) continue;
            if (draw >= 1f) b.pene += 1;                        // 가득 당기면 하나 더 꿰뚫음
            if (card[0] > 0 && draw >= 1f) b.onHitEnemy += (arrow, c) => SplitArrow(arrow, c);
        }
        if (card[1] > 0) StartCoroutine(EchoArrow(dir, dmg * 0.5f, speed, 0.4f + 0.25f * draw));
        if (card[2] > 0 && draw >= 1f) StartCoroutine(Backstep(-dir));
        // 활시위 "퉁" + 화살 "슉" (많이 당길수록 크고 묵직하게)
        Play("bowtwang", 0.55f + 0.35f * draw, 1.15f - 0.25f * draw);
        Play("arrowfly", 0.3f + 0.4f * draw, 1f + 0.4f * draw);
        BowFired = true;
    }

    // ================================================================= 연금술사: 시약 실험
    // 화염 → 빙결 → 산성 시약을 번갈아 채워 던지고, 가끔(15%) 불안정한 플라스크가 크게 터짐
    int reagent;
    static readonly Color[] ReagentColors = { new Color(1f, 0.55f, 0.25f), new Color(0.55f, 0.85f, 1f), new Color(0.55f, 1f, 0.35f) };
    public string NextReagentName => reagent switch { 0 => "화염 시약", 1 => "빙결 시약", _ => "산성 시약" };

    void ThrowReagent(Vector3 from, Vector3 land, float dmg)
    {
        int kind = reagent;
        reagent = (reagent + 1) % 3;
        bool unstable = Random.value < 0.15f;
        Color tint = unstable ? new Color(0.85f, 0.4f, 1f) : ReagentColors[kind];
        FlaskLob.Throw(from, land, 0.4f, unstable ? 1.05f : 0.8f, tint, (p) =>
        {
            float r = 1.8f * CatalystMul * blastMul * (unstable ? 1.6f : 1f);
            float hit = dmg * 1.6f * (unstable ? 1.8f : 1f);
            Play("shatter", 0.55f, Random.Range(0.9f, 1.2f));
            if (unstable)
            {
                // 불안정! 보라색 대폭발
                Collider2D[] caught = Physics2D.OverlapCircleAll(p, r);
                DamageCircle(p, r, hit, 2.2f);
                foreach (Collider2D c in caught)
                {
                    EnermyController e = c != null ? c.GetComponent<EnermyController>() : null;
                    if (e != null && e.IsDead) ChainPop(c.transform.position, hit);
                }
                Fx.Play("fx_alchemyblast", p, r * 2.4f, new Color(0.9f, 0.55f, 1f), 16f);
                Fx.Play("fx_shock", p, r * 2.2f, new Color(0.85f, 0.5f, 1f, 0.8f), 20f);
                Hostile.Shake(0.18f);
                Play("boom", 0.7f, 1.1f);
                return;
            }
            Collider2D[] inside = Physics2D.OverlapCircleAll(p, r);
            DamageCircle(p, r, hit, 1.2f);
            Fx.Play("fx_alchemyblast", p, r * 2.2f, ReagentColors[kind], 18f);
            foreach (Collider2D c in inside)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && e.IsDead) { ChainPop(c.transform.position, hit); continue; }
                if (kind == 0) Burn.Apply(c.gameObject, hit * 0.35f, 2f);
                // 급속 냉동: 빙결 시약이 느리게 하는 대신 꽁꽁 얼림
                if (kind == 1 && e != null) e.Slow(card[1] > 0 ? 0f : 0.5f, card[1] > 0 ? 0.5f + 0.3f * card[1] : 1.5f);
            }
            if (kind == 0) Play("ignite", 0.3f, 1.3f);
            if (kind == 1) Play("shimmer", 0.35f, 1.5f);
            if (kind == 2)
            {
                if (Special != null) Special.SpawnZone(p, r * 0.8f, 2.5f, hit * 0.25f, new Color(0.45f, 1f, 0.3f, 0.6f));
                Play("fizz", 0.45f, 1f);
            }
        });
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
        // 전용 특수 무기를 들고 있으면 우클릭은 그 무기의 궁극기
        if (!charging && !Dashing && Special != null && Special.KitWeaponUltActive)
        {
            if (GameInput.UltDown && full)
            {
                Special.KitWeaponUlt();
                Spend(gauge);
            }
            return;
        }
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
                UpdateRainAim(gauge, full);
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
            Play(Id == CharacterId.Alchemist ? "bubble" : "hum", 0.5f, 0.8f);
        }
        if (!charging) return;

        charge = Mathf.Min(1f, charge + Time.deltaTime / 1.5f);
        bool sword = Id == CharacterId.Swordsman;
        Vector3 at = sword ? transform.position : ClampRange(transform.position, Mouse, 14f);
        float radius = (sword ? 5f : (2f + 4f * charge) * CatalystMul) * UltMul;
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
            float dmg = Damage * (3f + 6f * charge) * UltMul;
            DamageCircle(transform.position, radius, dmg, 2.5f);
            Fx.Play("fx_spinslash", transform.position, radius * 2.4f, Color.white, 22f);
            Fx.Play("fx_shock", transform.position, radius * 2.2f, new Color(0.6f, 0.8f, 1f, 0.8f), 20f);
            Play("slash", 1f, 0.8f);
            Play("whoosh", 0.8f, 0.6f);
            if (card[3] > 0) StartCoroutine(BladeStorm(1f + card[3], radius * 0.7f));
        }
        else
        {
            float dmg = Damage * (4f + 8f * charge) * UltMul;
            FlaskLob.Throw(player.MuzzlePosition, at, 0.55f, 1.6f, new Color(0.8f, 1f, 0.7f), (p) =>
            {
                DamageCircle(p, radius, dmg, 3f);
                Fx.Play("fx_alchemyblast", p, radius * 2.4f, Color.white, 16f);
                if (Special != null) Special.SpawnZone(p, radius * 0.7f, 4f, Damage * 0.5f, new Color(0.45f, 1f, 0.35f, 0.7f));
                Play("boom", 1f, 0.9f);
                Play("shatter", 0.8f, 0.8f);
                Play("fizz", 0.7f, 0.8f);
                if (card[3] > 0) Shards(p, radius, 2 + 2 * card[3]);
            });
        }
        Spend(gauge);
    }

    // 도적 출혈 돌진: 무적 상태로 마우스 쪽으로 빠르게 돌진, 지나간 적마다 한 번 베고 출혈
    IEnumerator BleedDash(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.01f) dir = body.flipX ? Vector2.left : Vector2.right;
        const float dist = 25f, time = 0.32f, width = 1.4f;
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
        // 돌진하는 동안 + 끝난 뒤 0.4초 무적
        player.GrantInvincibility(time + 0.4f);
        if (card[2] > 0) StartCoroutine(ShadowClone(from, 1f + card[2]));
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
        if (card[3] > 0) StarBurst(to, 4 + 4 * card[3]);
    }

    // 이번 프레임에 지나간 구간 전체 (프레임이 길어도 건너뛰지 않게)
    void Cut(Vector3 a, Vector3 b, float width, Vector2 dir, HashSet<Collider2D> hit)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + width))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > width) continue;
            if (!hit.Add(c)) continue;
            Specials.Damage(c.gameObject, Damage * 1.5f * UltMul, dir, 1f);
            Bleed.Apply(c.gameObject, Damage * 1.2f * UltMul, 4f);
            Fx.Play("fx_bleed", c.transform.position, 1.4f, Color.white, 16f);
            Play("crack", 0.35f, 1.4f);
        }
    }

    // 화살비: 마우스 둘레에 화살이 1.2초 동안 쏟아짐
    IEnumerator ArrowRain(Vector3 center)
    {
        Play("bowtwang", 0.8f, 0.85f);
        float radius = 4f * UltMul;
        Hostile.Circle(center, radius, 0.4f, new Color(0.6f, 1f, 0.5f, 0.6f));
        Play("whoosh", 0.8f, 0.9f);
        for (int i = 0; i < Mathf.RoundToInt(20 * UltMul); i++)
        {
            Vector3 at = center + (Vector3)(Random.insideUnitCircle * radius);
            Fx.Play("fx_arrowrain", at + Vector3.up * 1f, 2f, Color.white, 18f);
            DamageCircle(at, 1.3f, Damage * 2.5f, 0.5f);
            if (i % 4 == 0) Play("arrowfly", 0.35f, Random.Range(0.8f, 1.2f));
            yield return new WaitForSeconds(0.06f);
        }
        if (card[3] > 0) StartCoroutine(Thorns(center, radius, 2f + card[3]));
    }

    // 궁수 우클릭: 누르고 있는 동안 화살비가 떨어질 자리(원)가 마우스를 따라다니고, 떼면 쏟아짐
    bool aiming;
    LineRenderer aimRing;

    void UpdateRainAim(SkillGauge gauge, bool full)
    {
        if (!aiming && GameInput.UltDown && full)
        {
            aiming = true;
            Play("bowdraw", 0.5f, 0.8f);
        }
        if (!aiming) return;

        Vector3 at = Mouse;
        if (aimRing == null) aimRing = Hostile.NewLine("RainAim", Color.white, 0.12f, 25);
        aimRing.loop = true;
        aimRing.enabled = true;
        Hostile.SetArc(aimRing, at, 4f * UltMul, 0f, 360f);
        aimRing.startColor = aimRing.endColor = new Color(0.6f, 1f, 0.5f, 0.5f + 0.3f * Mathf.Sin(Time.time * 14f));

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        aiming = false;
        aimRing.enabled = false;
        StartCoroutine(ArrowRain(at));
        Spend(gauge);
    }

    // ================================================================= 캐릭터 전용 레벨업 카드 (특수 능력)
    public void CardPicked(int slot)
    {
        if (Id == CharacterId.Alchemist && slot == 2 && homunculus == null) StartCoroutine(Homunculus());
    }

    // 검사: 검기 · 흡혈 베기 · 쳐내기 (평타를 휘두를 때마다)
    void SwingCards(Vector3 origin, Vector2 dir, float reach, float half, int hits)
    {
        if (card[0] > 0)
        {
            // 검기: 휘두른 방향으로 적을 꿰뚫는 칼바람이 날아감
            Projectile(origin + (Vector3)(dir * 1f), dir, Damage * (0.2f + 0.2f * card[0]), 9999, 26f, 14f, "fx_swordwave", 2.6f, new Color(0.7f, 0.9f, 1f, 0.9f), false);
        }
        if (card[1] > 0 && hits > 0 && player.PlayerHealth > 0f)
        {
            // 흡혈 베기: 벤 적(최대 3) 하나당 체력 회복
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + Mathf.Min(hits, 3) * card[1]);
            Fx.Play("fx_sparkle", transform.position, 1.2f, new Color(1f, 0.4f, 0.45f), 20f);
        }
        if (card[2] > 0)
        {
            // 쳐내기: 휘두르는 범위 안의 적 투사체를 베어 없앰
            int parried = 0;
            foreach (HostileProjectile hp in FindObjectsByType<HostileProjectile>(FindObjectsSortMode.None))
            {
                Vector2 to = hp.transform.position - origin;
                if (to.magnitude > reach + 0.5f || (to.sqrMagnitude > 0.25f && Vector2.Angle(dir, to) > half + 10f)) continue;
                Fx.Play("fx_spark", hp.transform.position, 1.2f, new Color(1f, 0.95f, 0.6f), 24f);
                Destroy(hp.gameObject);
                parried++;
            }
            if (parried > 0) Play("ding", 0.5f, 1.8f);
        }
    }

    // 검사 칼바람: 회전 베기 뒤 몇 초 동안 칼바람이 몸을 감싸고 돌며 주변을 벰
    IEnumerator BladeStorm(float duration, float radius)
    {
        FxAnim storm = Fx.Play("fx_tornado", transform.position, radius * 2f, new Color(0.7f, 0.85f, 1f, 0.55f), 16f, 0f, 13, true, duration);
        float tick = 0f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (storm != null) storm.transform.position = transform.position;
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.3f;
                DamageCircle(transform.position, radius, Damage * 0.35f, 0.6f);
                Play("whoosh", 0.25f, 1.6f);
            }
            yield return null;
        }
    }

    // 도적 표창: 갈고리 표창(출혈) · 도탄 표창(가까운 다른 적에게 튕김)
    Bullet Shuriken(Vector3 start, Vector2 dir, float dmg, int bounces)
    {
        Bullet b = Projectile(start, dir, dmg, player.pene, 45f, 0f, "fx_shuriken", 1.1f, Color.white, true);
        if (b == null) return null;
        if (card[1] > 0) b.onHitEnemy += (s, c) => Bleed.Apply(c.gameObject, Damage * 0.2f * card[1], 3f);
        if (bounces > 0)
        {
            bool bounced = false;
            b.onHitEnemy += (s, c) =>
            {
                if (bounced || c == null) return;
                Transform next = NearestOther(c.transform.position, 9f, c.transform, s.Direction, 360f);
                if (next == null) return;
                bounced = true;
                Bullet r = Shuriken(c.transform.position, (next.position - c.transform.position).normalized, s.damage * 0.8f, bounces - 1);
                EnermyController e = c.GetComponent<EnermyController>();
                if (r != null && e != null) r.hitOnce = new HashSet<int> { e.GetInstanceID() };
            };
        }
        return b;
    }

    // 도적 그림자 분신: 돌진을 시작한 자리에 분신이 남아 가까운 적에게 표창을 던짐
    IEnumerator ShadowClone(Vector3 at, float duration)
    {
        GameObject g = SpecialAbilities.MakeSprite("ShadowClone", body.sprite, at, 1f, new Color(0.55f, 0.3f, 0.85f, 0.7f), "Character", -1);
        g.transform.localScale = transform.lossyScale;
        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
        sr.flipX = body.flipX;
        Fx.Play("fx_stealth", at, 2f, new Color(0.8f, 0.6f, 1f), 18f);
        float next = 0.1f;
        for (float t = 0f; t < duration && g != null; t += Time.deltaTime)
        {
            next -= Time.deltaTime;
            if (next <= 0f)
            {
                Transform e = Specials.NearestEnemy(at, 14f);
                next = e != null ? 0.35f : 0.1f;
                if (e != null)
                {
                    Vector2 d = ((Vector2)(e.position - at)).normalized;
                    sr.flipX = d.x < 0f;
                    Projectile(at, d, Damage * 0.7f, 1, 40f, 0f, "fx_shuriken", 1f, new Color(0.8f, 0.6f, 1f), true);
                    Play("whoosh", 0.2f, 2f);
                }
            }
            yield return null;
        }
        if (g != null) g.AddComponent<FadeOut>().duration = 0.3f;
    }

    // 도적 표창 폭풍: 돌진이 끝난 자리에서 표창이 사방으로 퍼짐
    void StarBurst(Vector3 at, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (360f / count * i) * Mathf.Deg2Rad;
            Shuriken(at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), Damage * 0.8f, 0);
        }
        Play("whoosh", 0.6f, 1.9f);
    }

    // 가장 가까운 다른 적 (except 제외, ahead 방향에서 maxAngle 안쪽만)
    static Transform NearestOther(Vector3 from, float range, Transform except, Vector2 ahead, float maxAngle)
    {
        Transform best = null;
        float bestDist = range;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(from, range))
        {
            if (c.transform == except || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && e.IsDead) continue;
            Vector2 to = c.transform.position - from;
            if (maxAngle < 180f && Vector2.Angle(ahead, to) > maxAngle) continue;
            if (to.magnitude < bestDist) { bestDist = to.magnitude; best = c.transform; }
        }
        return best;
    }

    // 궁수 분열 화살: 가득 당긴 화살이 처음 맞힌 적에게서 작은 화살 여러 갈래로 갈라짐
    void SplitArrow(Bullet arrow, Collider2D c)
    {
        if (c == null) return;
        int n = 1 + card[0];
        EnermyController e = c.GetComponent<EnermyController>();
        foreach (Vector2 d in Spread(arrow.Direction, n, 25f))
        {
            Bullet b = Projectile(c.transform.position, d, arrow.damage * 0.4f, 1, 50f, 10f, "fx_arrow", 0.35f, new Color(0.8f, 1f, 0.7f), false);
            if (b != null && e != null) b.hitOnce = new HashSet<int> { e.GetInstanceID() };
        }
    }

    // 궁수 메아리 화살: 쏜 화살을 0.25초 뒤 푸른 유령 화살이 따라 쏨 (그때의 활 위치에서)
    IEnumerator EchoArrow(Vector2 dir, float dmg, float speed, float size)
    {
        yield return new WaitForSeconds(0.25f);
        Bullet b = Projectile(player.MuzzlePosition, dir, dmg, player.pene + 1, speed, 0f, "fx_arrow", size, new Color(0.55f, 0.85f, 1f, 0.7f), false);
        if (b != null) Play("arrowfly", 0.25f, 1.6f);
    }

    // 궁수 바람 걸음: 가득 당긴 화살을 쏘면 반동으로 뒤로 휙 물러남
    IEnumerator Backstep(Vector2 dir)
    {
        const float dist = 3f, time = 0.12f;
        Vector3 from = transform.position;
        float len = dist;
        for (float d = 0.5f; d <= dist; d += 0.5f)
        {
            Vector3 p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p) || (Hostile.ClampArena(p) - p).sqrMagnitude > 0.01f) { len = d - 0.5f; break; }
        }
        if (len <= 0f) yield break;
        Vector3 to = from + (Vector3)(dir * len);
        dashUntil = Time.time + time;
        Fx.Play("fx_smoke", from, 1.4f, new Color(0.8f, 1f, 0.8f, 0.7f), 18f);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, to, t / time);
            yield return null;
        }
        transform.position = to;
    }

    // 궁수 가시 덤불: 화살비가 떨어진 자리에 가시 덤불이 남아 적을 느리게 하고 찌름
    IEnumerator Thorns(Vector3 center, float radius, float duration)
    {
        if (Special != null) Special.SpawnZone(center, radius, duration, Damage * 0.4f, new Color(0.45f, 0.7f, 0.25f, 0.6f));
        for (float t = 0f; t < duration; t += 0.25f)
        {
            foreach (Collider2D c in Physics2D.OverlapCircleAll(center, radius))
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && !e.IsDead) e.Slow(0.45f, 0.4f);
            }
            Fx.Play("fx_spike", center + (Vector3)(Random.insideUnitCircle * radius), 1.2f, new Color(0.6f, 0.9f, 0.4f), 18f);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // 연금술사 연쇄 반응: 폭발로 쓰러진 적이 그 자리에서 한 번 더 터짐 (연쇄 폭발은 다시 이어지지 않음)
    void ChainPop(Vector3 pos, float hit)
    {
        if (card[0] <= 0) return;
        DamageCircle(pos, 1.5f, hit * (0.2f + 0.2f * card[0]), 1f);
        Fx.Play("fx_alchemyblast", pos, 3.4f, new Color(1f, 0.8f, 0.4f), 20f);
        Play("shatter", 0.3f, 1.4f);
    }

    // 연금술사 파편 플라스크: 대폭발이 작은 플라스크 여러 개로 흩어져 다시 터짐
    void Shards(Vector3 p, float radius, int count)
    {
        float dmg = Damage * 1.5f;
        for (int i = 0; i < count; i++)
        {
            float a = (360f / count * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector3 land = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(radius * 0.6f, radius * 1.1f);
            FlaskLob.Throw(p, land, 0.35f, 0.6f, new Color(0.7f, 1f, 0.5f), (q) =>
            {
                DamageCircle(q, 1.5f, dmg, 1f);
                Fx.Play("fx_alchemyblast", q, 3.4f, new Color(0.6f, 1f, 0.45f), 18f);
                Play("shatter", 0.3f, 1.3f);
            });
        }
    }

    // 연금술사 호문쿨루스: 머리 위를 맴도는 작은 조수가 가까운 적에게 작은 플라스크를 던짐
    GameObject homunculus;

    IEnumerator Homunculus()
    {
        Sprite[] f = Fx.Frames("fx_flask");
        if (f.Length == 0) yield break;
        homunculus = SpecialAbilities.MakeSprite("Homunculus", f[0], transform.position, 0.7f / f[0].bounds.size.y, new Color(0.6f, 1f, 0.5f), "Character", 2);
        float cd = 1f, t = 0f;
        while (homunculus != null)
        {
            t += Time.deltaTime;
            Vector3 home = transform.position + new Vector3(Mathf.Cos(t * 2f) * 1.4f, 1.3f + Mathf.Sin(t * 4f) * 0.2f, 0f);
            homunculus.transform.position = Vector3.Lerp(homunculus.transform.position, home, 8f * Time.deltaTime);
            cd -= Time.deltaTime;
            if (cd <= 0f)
            {
                Transform e = Specials.NearestEnemy(homunculus.transform.position, 12f);
                cd = e != null ? 3.4f - 0.7f * card[2] : 0.3f;
                if (e != null)
                {
                    float dmg = Damage * 1.4f;
                    FlaskLob.Throw(homunculus.transform.position, e.position, 0.45f, 0.55f, new Color(0.6f, 1f, 0.5f), (q) =>
                    {
                        DamageCircle(q, 1.4f, dmg, 1f);
                        Fx.Play("fx_alchemyblast", q, 3f, new Color(0.6f, 1f, 0.45f), 18f);
                        Play("shatter", 0.3f, 1.4f);
                    });
                }
            }
            yield return null;
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

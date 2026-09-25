using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 거너가 아닌 캐릭터의 특수 능력 (ID 20 ~ 51): 캐릭터마다 무기 3 · 스킬 3 · 패시브 2
// 지옥의 문 · 특수 강화에서는 고른 캐릭터의 능력만 나옴 (CharacterData.pool)
// SpecialAbilities 의 나머지 부분(탄창 · 쿨타임 · 진화 · 강화 상점 · HUD)을 그대로 씀
public partial class SpecialAbilities
{
    public const int KitFirstId = 20;
    // 검사
    public const int KitGreatsword = 20, KitTwinBlades = 21, KitSpear = 22, KitDashSlash = 23, KitWhirlwind = 24, KitSwordWave = 25, KitIronWill = 26, KitBloodBlade = 27;
    // 도적
    public const int KitKnifeFan = 28, KitBlazeStar = 29, KitChakram = 30, KitShadowStep = 31, KitSmokeBomb = 32, KitAssassinate = 33, KitVitalStrike = 34, KitAfterimage = 35;
    // 궁수
    public const int KitLongbow = 36, KitRepeater = 37, KitBlastArrow = 38, KitBackstep = 39, KitHunterTrap = 40, KitPiercingVolley = 41, KitEagleEye = 42, KitTailwind = 43;
    // 연금술사
    public const int KitFireFlask = 44, KitFrostFlask = 45, KitShockFlask = 46, KitHealPotion = 47, KitTransmute = 48, KitAcidRain = 49, KitCatalyst = 50, KitGoldTouch = 51;

    static readonly (string name, SpecialKind kind, string desc)[] KitDefs =
    {
        ("대검", SpecialKind.Weapon, "앞쪽을 크게 휩쓰는 느리고 무거운 베기. (공격력 220%)\n진화: 공격력 300%, 범위 증가"),
        ("쌍검", SpecialKind.Weapon, "누르고 있으면 양손 검으로 빠르게 연속 베기. (공격력 80%)\n진화: 한 번에 두 번씩 벱니다"),
        ("창", SpecialKind.Weapon, "앞으로 길게 찔러 한 줄의 적을 모두 꿰뚫습니다. (공격력 160%)\n진화: 사거리와 피해 증가"),
        ("돌진 베기", SpecialKind.Skill, "마우스 쪽으로 빠르게 돌진하며 지나간 적을 벱니다. (공격력 300%)\n진화: 쿨타임 감소, 더 멀리"),
        ("회오리 베기", SpecialKind.Skill, "잠시 동안 검을 휘돌려 주변 적을 계속 벱니다.\n진화: 지속 시간과 범위 증가"),
        ("검기", SpecialKind.Skill, "거대한 초승달 검기를 날려 한 줄을 벱니다. (공격력 400%)\n진화: 검기 3개"),
        ("강철 의지", SpecialKind.Passive, "받는 피해가 20% 줄어듭니다.\n진화: 30%"),
        ("흡혈의 검", SpecialKind.Passive, "적을 벨 때마다 체력을 조금 회복합니다.\n진화: 회복량 두 배"),

        ("단검 부채", SpecialKind.Weapon, "단검 다섯 자루를 부채꼴로 던집니다. (자루당 공격력 60%)\n진화: 일곱 자루"),
        ("불꽃 표창", SpecialKind.Weapon, "누르고 있으면 맞은 적을 불태우는 표창을 연사합니다.\n진화: 불타는 피해 두 배"),
        ("차크람", SpecialKind.Weapon, "날아갔다 돌아오는 원반. 갈 때 한 번, 올 때 한 번 벱니다.\n진화: 더 크고 멀리"),
        ("그림자 이동", SpecialKind.Skill, "마우스 위치로 순간 이동합니다. 잠깐 무적.\n진화: 쿨타임 감소, 도착 자리에서 폭발"),
        ("연막탄", SpecialKind.Skill, "연막을 터뜨려 안에 든 적을 크게 느리게 합니다.\n진화: 연막이 적에게 피해"),
        ("암살", SpecialKind.Skill, "가장 가까운 적에게 순간 이동해 치명적인 일격. (공격력 600%)\n진화: 두 번 연속"),
        ("급소 찌르기", SpecialKind.Passive, "공격력이 25% 오릅니다.\n진화: 45%"),
        ("잔상", SpecialKind.Passive, "이동 속도 +15%, 맞은 뒤 무적 시간이 길어집니다.\n진화: 이동 속도 +30%"),

        ("장궁", SpecialKind.Weapon, "무거운 화살로 적 여섯을 꿰뚫습니다. (공격력 240%)\n진화: 무한 관통"),
        ("연사 석궁", SpecialKind.Weapon, "누르고 있으면 작은 화살을 빠르게 연사합니다. (공격력 45%)\n진화: 두 발씩"),
        ("폭발 화살", SpecialKind.Weapon, "맞은 자리에서 터지는 화살. (공격력 140% + 폭발)\n진화: 폭발 범위 증가"),
        ("후퇴 사격", SpecialKind.Skill, "뒤로 뛰어 물러나며 화살 다섯 발을 부채꼴로 쏩니다.\n진화: 화살 아홉 발"),
        ("사냥 덫", SpecialKind.Skill, "마우스 위치에 덫을 놓습니다. 밟은 적은 묶이고 큰 피해를 받습니다.\n진화: 덫 세 개"),
        ("관통 사격", SpecialKind.Skill, "거대한 화살 세 발을 한 줄로 쏘아 모두 꿰뚫습니다. (공격력 300%)\n진화: 다섯 발"),
        ("매의 눈", SpecialKind.Passive, "공격력 +15%, 모든 화살이 적을 하나 더 꿰뚫습니다.\n진화: 공격력 +30%"),
        ("순풍", SpecialKind.Passive, "공격 속도 +15%, 이동 속도 +10%.\n진화: 공격 속도 +30%"),

        ("화염 플라스크", SpecialKind.Weapon, "터진 자리에 불타는 장판을 남깁니다.\n진화: 장판이 더 크고 오래"),
        ("빙결 플라스크", SpecialKind.Weapon, "터진 자리의 적을 크게 느리게 만듭니다.\n진화: 느려지는 시간 증가"),
        ("번개 플라스크", SpecialKind.Weapon, "터지며 주변 적에게 번개가 튑니다.\n진화: 번개가 더 많이 튐"),
        ("치유 물약", SpecialKind.Skill, "체력을 25% 회복합니다.\n진화: 40%"),
        ("변이 폭탄", SpecialKind.Skill, "폭발 안의 약해진 적을 금으로 바꿉니다 (보스 제외, 체력 40% 이하).\n진화: 범위 증가"),
        ("산성 비", SpecialKind.Skill, "마우스 둘레에 산성 웅덩이가 쏟아집니다.\n진화: 웅덩이 두 배"),
        ("촉매", SpecialKind.Passive, "모든 폭발 범위가 30% 커집니다.\n진화: 50%"),
        ("황금 손", SpecialKind.Passive, "코인을 더 얻고, 적을 처치하면 체력을 조금 회복합니다.\n진화: 두 배"),
    };

    public static bool IsKit(int id) => id >= KitFirstId && id < KitFirstId + KitDefs.Length;

    // 인스펙터의 능력 목록(거너) 뒤에 새 캐릭터 능력을 붙임 (ID = 순서)
    void KitExtendAbilities()
    {
        if (abilities == null || abilities.Length >= KitFirstId + KitDefs.Length) return;
        SpecialDef[] all = new SpecialDef[KitFirstId + KitDefs.Length];
        for (int i = 0; i < abilities.Length && i < KitFirstId; i++) all[i] = abilities[i];
        for (int i = 0; i < KitDefs.Length; i++)
        {
            int id = KitFirstId + i;
            all[id] = new SpecialDef
            {
                name = KitDefs[i].name,
                kind = KitDefs[i].kind,
                description = KitDefs[i].desc,
                icon = Resources.Load<Sprite>("Icons/ability_" + id),
            };
        }
        abilities = all;
    }

    // ================================================================= 탄창 · 간격 · 장전 (0 = 탄약 없음)
    static int KitMag(int id) => id switch
    {
        KitKnifeFan => 10, KitBlazeStar => 18, KitRepeater => 20, KitBlastArrow => 6,
        KitFireFlask => 6, KitFrostFlask => 6, KitShockFlask => 6, _ => 0,
    };
    static float KitInterval(int id) => id switch
    {
        KitGreatsword => 0.9f, KitTwinBlades => 0.22f, KitSpear => 0.55f,
        KitKnifeFan => 0.5f, KitBlazeStar => 0.28f, KitChakram => 0.3f,
        KitLongbow => 0.9f, KitRepeater => 0.12f, KitBlastArrow => 0.7f,
        KitFireFlask => 0.8f, KitFrostFlask => 0.8f, KitShockFlask => 0.7f, _ => 0.45f,
    };
    static float KitReload(int id) => id switch { KitRepeater => 2f, KitKnifeFan => 1.8f, _ => 1.8f };
    static Color KitColor(int id) => id switch
    {
        >= 20 and < 28 => new Color(0.6f, 0.8f, 1f),
        >= 28 and < 36 => new Color(0.8f, 0.55f, 1f),
        >= 36 and < 44 => new Color(0.6f, 1f, 0.5f),
        >= 44 and < 52 => new Color(0.7f, 1f, 0.5f),
        _ => new Color(1f, 0.9f, 0.6f),
    };

    float KitExplodeMul => CharacterKit.Instance != null ? CharacterKit.Instance.CatalystMul : 1f;

    // ================================================================= 범위 피해 도우미
    int DamageArc(Vector3 origin, Vector2 dir, float range, float halfAngle, float damage, float knock)
    {
        int n = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(origin, range))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector2 to = c.transform.position - origin;
            if (to.sqrMagnitude > 0.25f && Vector2.Angle(dir, to) > halfAngle) continue;
            Specials.Damage(c.gameObject, damage, to.normalized, knock);
            OnKitHit();
            n++;
        }
        return n;
    }

    int DamageLine(Vector3 a, Vector3 b, float width, float damage, float knock)
    {
        int n = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + width))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > width) continue;
            Specials.Damage(c.gameObject, damage, (b - a).normalized, knock);
            OnKitHit();
            n++;
        }
        return n;
    }

    // 흡혈의 검
    void OnKitHit()
    {
        if (!Has(KitBloodBlade) || player == null || player.PlayerHealth <= 0f) return;
        player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + (IsEvolved(KitBloodBlade) ? 1f : 0.5f));
    }

    Bullet KitProjectile(Vector3 start, Vector2 dir, float dmg, int pene, float speed, string fxName, float size, Color tint, bool spin)
    {
        Bullet b = Shot(start, dir, dmg, pene, 1f, false, tint, 1f, 1f, 1f);
        if (b == null) return null;
        b.speed = speed;
        b.lifetime = 6f;
        SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
        Sprite[] f = Fx.Frames(fxName);
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

    GameObject activeChakram;

    // ================================================================= 무기 (Q로 교체, 좌클릭)
    void KitUpdateWeapon(int id, bool down, bool held, bool up)
    {
        Vector3 m = player.MuzzlePosition;
        Vector2 dir = AimDir();
        bool evo = IsEvolved(id);
        Color col = KitColor(id);
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        switch (id)
        {
            case KitGreatsword:
                {
                    float range = evo ? 5f : 4.2f;
                    fx.SetCone(previewCone, player.transform.position, dir, 75f, range, new Color(col.r, col.g, col.b, 0.35f), 0.08f);
                    if (!down || !Ready()) break;
                    BeginShot(0, out _, out _);
                    DamageArc(player.transform.position, dir, range, 75f, WDamage * (evo ? 3f : 2.2f), 2.5f);
                    Fx.Play("fx_swordwave", player.transform.position + (Vector3)(dir * range * 0.55f), range * 1.4f, Color.white, 20f, rot, 15);
                    fx.Play("slash", 0.9f, 0.7f);
                    break;
                }
            case KitTwinBlades:
                if (!held || !Ready()) break;
                BeginShot(0, out _, out _);
                for (int i = 0; i < (evo ? 2 : 1); i++)
                {
                    DamageArc(player.transform.position, dir, 3f, 55f, WDamage * 0.8f, 0.6f);
                    Fx.Play("fx_swordwave", player.transform.position + (Vector3)(dir * 1.7f), 2.6f, new Color(0.8f, 0.9f, 1f), 26f, rot + (dualToggle ? 20f : -20f), 15);
                    dualToggle = !dualToggle;
                }
                fx.Play("slash", 0.4f, 1.4f);
                break;
            case KitSpear:
                {
                    float len = evo ? 10f : 7f;
                    if (!down || !Ready()) break;
                    BeginShot(0, out _, out _);
                    DamageLine(m, m + (Vector3)(dir * len), 0.9f, WDamage * (evo ? 2.2f : 1.6f), 1.5f);
                    Fx.Play("fx_trail_dot", m + (Vector3)(dir * len * 0.5f), 1.2f, col, 1f, rot, 16, false, 0.12f);
                    for (float k = 1f; k < len; k += 1f) Fx.Play("fx_trail_dot", m + (Vector3)(dir * k), 0.7f, col, 1f, rot, 16, false, 0.1f + k * 0.01f);
                    fx.Play("whoosh", 0.6f, 1.6f);
                    break;
                }
            case KitKnifeFan:
                if (!down || !Ready()) break;
                BeginShot(1, out Vector3 s0, out _);
                int knives = evo ? 7 : 5;
                for (int i = 0; i < knives; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, (i - (knives - 1) * 0.5f) * 10f) * dir;
                    KitProjectile(s0, d, WDamage * 0.6f, player.pene, 40f, "fx_arrow", 0.35f, new Color(0.85f, 0.85f, 0.95f), false);
                }
                break;
            case KitBlazeStar:
                if (!held || !Ready()) break;
                {
                    BeginShot(1, out Vector3 s1, out _);
                    Bullet b = KitProjectile(s1, dir, WDamage * 0.6f, player.pene, 45f, "fx_shuriken", 0.7f, new Color(1f, 0.7f, 0.4f), true);
                    float burn = WDamage * (evo ? 0.6f : 0.3f);
                    if (b != null) b.onHitEnemy += (bullet, c) => Burn.Apply(c.gameObject, burn, 2f);
                }
                break;
            case KitChakram:
                if (activeChakram == null)
                    fx.SetLine(aimLine, m, m + (Vector3)(dir * 12f), new Color(col.r, col.g, col.b, 0.5f), 0.1f);
                player.ammoTextOverride = activeChakram == null ? Loc.T("원반 준비") : Loc.T("원반 회수 중");
                if (!down || activeChakram != null || !Ready()) break;
                {
                    BeginShot(0, out Vector3 s2, out _);
                    Bullet b = KitProjectile(s2, dir, WDamage * 1.3f, 9999, 0f, "fx_shuriken", evo ? 2.2f : 1.6f, new Color(0.6f, 0.95f, 1f), true);
                    if (b == null) break;
                    b.hitOnce = new HashSet<int>();
                    Scythe s = b.gameObject.AddComponent<Scythe>();
                    s.distance = evo ? 15f : 11f;
                    s.outTime = 0.4f;
                    s.owner = player.transform;
                    s.direction = dir;
                    s.returnsTo = this;
                    activeChakram = b.gameObject;
                    fx.Play("whoosh", 0.6f, 1.2f);
                }
                break;
            case KitLongbow:
                if (!down || !Ready()) break;
                BeginShot(0, out Vector3 s3, out _);
                KitProjectile(s3, dir, WDamage * 2.4f, evo ? 9999 : 6, 70f, "fx_arrow", 0.6f, new Color(1f, 1f, 0.8f), false);
                fx.Play("crack", 0.5f, 1.4f);
                break;
            case KitRepeater:
                if (!held || !Ready()) break;
                {
                    BeginShot(1, out Vector3 s4, out _);
                    for (int i = 0; i < (evo ? 2 : 1); i++)
                    {
                        Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f)) * dir;
                        KitProjectile(s4, d, WDamage * 0.45f, player.pene, 65f, "fx_arrow", 0.35f, Color.white, false);
                    }
                    fx.Play("pew", 0.35f, 1.3f);
                }
                break;
            case KitBlastArrow:
                if (!down || !Ready()) break;
                {
                    BeginShot(1, out Vector3 s5, out _);
                    Bullet b = KitProjectile(s5, dir, WDamage * 1.4f, 1, 55f, "fx_arrow", 0.5f, new Color(1f, 0.6f, 0.3f), false);
                    float boom = WDamage * 1.2f, r = (evo ? 3.2f : 2.5f) * KitExplodeMul;
                    if (b != null) b.onHitEnemy += (bullet, c) => Explode(c.transform.position, r, boom, 1.5f, new Color(1f, 0.55f, 0.2f, 0.85f));
                }
                break;
            case KitFireFlask:
            case KitFrostFlask:
            case KitShockFlask:
                {
                    Vector3 land = GrenadeLanding();
                    float r = (id == KitShockFlask ? 2f : 2.4f) * KitExplodeMul * (evo && id == KitFireFlask ? 1.3f : 1f);
                    fx.SetLine(aimLine, m, land, new Color(col.r, col.g, col.b, 0.4f), 0.08f);
                    fx.SetRing(previewRing, land, r, new Color(col.r, col.g, col.b, Ready() ? 0.7f : 0.3f), 0.1f);
                    if (!down || !Ready()) break;
                    BeginShot(1, out Vector3 s6, out _);
                    float dmg = WDamage * 1.2f;
                    Color tint = id == KitFireFlask ? new Color(1f, 0.6f, 0.3f) : id == KitFrostFlask ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.95f, 0.5f);
                    int flaskId = id;
                    FlaskLob.Throw(s6, land, 0.45f, 0.9f, tint, p => KitFlaskLand(flaskId, p, r, dmg, evo));
                    break;
                }
        }
    }

    void KitFlaskLand(int id, Vector3 p, float r, float dmg, bool evo)
    {
        switch (id)
        {
            case KitFireFlask:
                Explode(p, r, dmg, 1f, new Color(1f, 0.5f, 0.15f, 0.85f));
                DamageZone z = SpawnZone(p, r, evo ? 5f : 3.5f, dmg * 0.4f, new Color(1f, 0.4f, 0.1f, 0.75f));
                z.lava = true;
                break;
            case KitFrostFlask:
                foreach (Collider2D c in Physics2D.OverlapCircleAll(p, r))
                {
                    if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                    Specials.Damage(c.gameObject, dmg, (c.transform.position - p).normalized, 0.5f);
                    EnermyController e = c.GetComponent<EnermyController>();
                    if (e != null) e.Slow(0.35f, evo ? 4f : 2.5f);
                }
                Fx.Play("fx_alchemyblast", p, r * 2.2f, new Color(0.6f, 0.9f, 1f), 18f);
                Fx.Play("fx_sparkle", p, 1.2f, Color.white, 16f);
                fx.Play("shimmer", 0.6f, 1.4f);
                break;
            case KitShockFlask:
                Explode(p, r, dmg, 1f, new Color(1f, 0.95f, 0.5f, 0.85f));
                Collider2D first = null;
                foreach (Collider2D c in Physics2D.OverlapCircleAll(p, r + 2f))
                    if (c.CompareTag("enermy") || c.CompareTag("boss")) { first = c; break; }
                if (first != null) ChainLightning(p, first, dmg * 0.8f, evo ? 6 : 3);
                break;
        }
    }

    // ================================================================= 스킬 (E · F · C)
    bool KitUseSkill(int id)
    {
        if (!IsKit(id)) return false;
        bool evo = IsEvolved(id);
        Vector3 pos = player.transform.position;
        Vector3 mouse = MouseWorld();
        Vector2 dir = ((Vector2)(mouse - pos)).normalized;
        switch (id)
        {
            case KitDashSlash:
                StartCoroutine(KitDash(dir, evo ? 9f : 7f, Damage * 3f));
                StartCooldown(id, evo ? 3.5f : 5f);
                break;
            case KitWhirlwind:
                StartCoroutine(KitWhirl(evo ? 3.5f : 2.5f, evo ? 3.8f : 3f));
                StartCooldown(id, 12f);
                break;
            case KitSwordWave:
                for (int i = 0; i < (evo ? 3 : 1); i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, (i - (evo ? 1 : 0)) * 15f) * dir;
                    Bullet b = KitProjectile(player.MuzzlePosition, d, Damage * 4f, 9999, 26f, "fx_swordwave", 6f, new Color(0.7f, 0.9f, 1f), false);
                    if (b != null) { b.hitOnce = new HashSet<int>(); b.lifetime = 20f / 26f; }
                }
                fx.Play("slash", 1f, 0.6f);
                StartCooldown(id, 6f);
                break;
            case KitShadowStep:
                {
                    Vector3 to = KitClamp(pos + Vector3.ClampMagnitude(mouse - pos, 10f));
                    Fx.Play("fx_stealth", pos, 3f, Color.white, 16f);
                    player.transform.position = to;
                    player.GrantInvincibility(0.5f);
                    Fx.Play("fx_stealth", to, 3f, Color.white, 16f);
                    if (evo) Explode(to, 3f, Damage * 2f, 2f, new Color(0.7f, 0.5f, 1f, 0.85f));
                    fx.Play("whoosh", 0.8f, 1.4f);
                    StartCooldown(id, evo ? 2.5f : 4f);
                    break;
                }
            case KitSmokeBomb:
                StartCoroutine(KitSmoke(pos, evo));
                StartCooldown(id, 12f);
                break;
            case KitAssassinate:
                {
                    Transform t = Specials.NearestEnemy(pos, 12f);
                    if (t == null) { fx.Play("buzz", 0.6f); fx.FloatText(pos, Loc.T("대상 없음"), new Color(0.7f, 0.66f, 0.72f), 4f, 0.4f); return true; }
                    StartCoroutine(KitAssassin(t, evo ? 2 : 1));
                    StartCooldown(id, 8f);
                    break;
                }
            case KitBackstep:
                {
                    player.transform.position = KitClamp(pos - (Vector3)(dir * 4f));
                    int n = evo ? 9 : 5;
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 d = Quaternion.Euler(0f, 0f, (i - (n - 1) * 0.5f) * 9f) * dir;
                        KitProjectile(player.MuzzlePosition, d, Damage * 1.2f, player.pene + 1, 60f, "fx_arrow", 0.45f, Color.white, false);
                    }
                    Fx.Play("fx_smoke", pos, 2f, new Color(0.7f, 0.7f, 0.6f, 0.6f), 16f);
                    fx.Play("whoosh", 0.7f, 1.1f);
                    StartCooldown(id, 5f);
                    break;
                }
            case KitHunterTrap:
                for (int i = 0; i < (evo ? 3 : 1); i++)
                {
                    Vector3 at = KitClamp(mouse + (i == 0 ? Vector3.zero : (Vector3)(Random.insideUnitCircle.normalized * 2.5f)));
                    HunterTrap.Place(at, Damage * 4f, this);
                }
                fx.Play("clank", 0.7f, 0.9f);
                StartCooldown(id, 8f);
                break;
            case KitPiercingVolley:
                StartCoroutine(KitVolley(dir, evo ? 5 : 3));
                StartCooldown(id, 7f);
                break;
            case KitHealPotion:
                player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + player.PlayerMaxHealth * (evo ? 0.4f : 0.25f));
                Fx.Play("fx_levelup", pos + Vector3.up, 4f, new Color(0.6f, 1f, 0.6f), 14f, 0f, 30);
                fx.FloatText(pos, Loc.T("회복!"), new Color(0.5f, 1f, 0.5f), 5f, 0f);
                fx.Play("chime", 0.8f, 1.2f);
                StartCooldown(id, 18f);
                break;
            case KitTransmute:
                {
                    float r = (evo ? 5f : 4f) * KitExplodeMul;
                    Vector3 at = KitClamp(pos + Vector3.ClampMagnitude(mouse - pos, 12f));
                    foreach (Collider2D c in Physics2D.OverlapCircleAll(at, r))
                    {
                        EnermyController e = c.GetComponent<EnermyController>();
                        if (e != null && !e.IsDead && e.EnemyHealth <= e.setEnemyHP * 0.4f)
                        {
                            Fx.Play("fx_sparkle", c.transform.position, 1.2f, new Color(1f, 0.85f, 0.3f), 16f);
                            e.coinDrop += 2;
                            e.TakeDamage(e.EnemyHealth + 1f, 0f, Vector3.zero);
                        }
                        else if (c.CompareTag("enermy") || c.CompareTag("boss")) Specials.Damage(c.gameObject, Damage * 2f, (c.transform.position - at).normalized, 1f);
                    }
                    Fx.Play("fx_alchemyblast", at, r * 2.2f, new Color(1f, 0.9f, 0.4f), 16f);
                    fx.Play("chime", 0.7f, 0.8f);
                    StartCooldown(id, 14f);
                    break;
                }
            case KitAcidRain:
                StartCoroutine(KitAcidRainRoutine(mouse, evo ? 12 : 6));
                StartCooldown(id, 12f);
                break;
            default:
                return false;
        }
        return true;
    }

    Vector3 KitClamp(Vector3 p)
    {
        Vector3 c = Hostile.ClampArena(p);
        return Hostile.IsWall(c) ? player.transform.position : c;
    }

    IEnumerator KitDash(Vector2 dir, float dist, float dmg)
    {
        Vector3 from = player.transform.position;
        Vector3 to = KitClamp(from + (Vector3)(dir * dist));
        player.GrantInvincibility(0.3f);
        fx.Play("whoosh", 0.9f, 1.3f);
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(from, to, t / 0.15f);
            yield return null;
        }
        player.transform.position = to;
        DamageLine(from, to, 1.4f, dmg, 1.5f);
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        for (float k = 0f; k <= 1f; k += 0.25f)
            Fx.Play("fx_swordwave", Vector3.Lerp(from, to, k), 2.4f, new Color(0.7f, 0.9f, 1f, 0.9f), 20f, rot, 15);
        fx.Play("slash", 1f, 0.9f);
    }

    IEnumerator KitWhirl(float duration, float radius)
    {
        float tick = 0f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.25f;
                DamageCircle(player.transform.position, radius, Damage * 0.8f, 0.8f);
                Fx.Play("fx_spinslash", player.transform.position, radius * 2.3f, new Color(1f, 1f, 1f, 0.8f), 24f);
                fx.Play("slash", 0.4f, 1.2f + Random.Range(-0.1f, 0.1f));
            }
            yield return null;
        }
    }

    void DamageCircle(Vector3 pos, float r, float dmg, float knock)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, r))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Specials.Damage(c.gameObject, dmg, (c.transform.position - pos).normalized, knock);
            OnKitHit();
        }
    }

    IEnumerator KitSmoke(Vector3 at, bool evo)
    {
        const float r = 4f;
        FxAnim cloud = Fx.Play("fx_stealth", at, r * 2.4f, new Color(1f, 1f, 1f, 0.8f), 4f, 0f, 3, true, 4.2f);
        fx.Play("hiss", 0.8f, 0.7f);
        for (float t = 0f; t < 4f; t += 0.3f)
        {
            foreach (Collider2D c in Physics2D.OverlapCircleAll(at, r))
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e == null) continue;
                e.Slow(0.3f, 0.5f);
                if (evo) e.TakeDamage(Damage * 0.3f, 0f, Vector3.zero);
            }
            if (Random.value < 0.5f) Fx.Play("fx_stealth", at + (Vector3)(Random.insideUnitCircle * r), 2.5f, new Color(1f, 1f, 1f, 0.6f), 10f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator KitAssassin(Transform target, int strikes)
    {
        for (int i = 0; i < strikes && target != null; i++)
        {
            Vector3 p = target.position;
            Fx.Play("fx_stealth", player.transform.position, 2.5f, Color.white, 16f);
            player.transform.position = KitClamp(p + (Vector3)(Random.insideUnitCircle.normalized * 1.2f));
            player.GrantInvincibility(0.4f);
            bool boss = target.CompareTag("boss");
            Specials.Damage(target.gameObject, Damage * (boss ? 3f : 6f), (p - player.transform.position).normalized, 2f);
            Fx.Play("fx_swordwave", p, 3f, new Color(1f, 0.4f, 0.5f), 22f, Random.Range(0f, 360f), 16);
            Fx.Play("fx_sparkle", p, 1.5f, new Color(1f, 0.3f, 0.3f), 18f);
            fx.Play("slash", 1f, 1.3f);
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator KitVolley(Vector2 dir, int n)
    {
        for (int i = 0; i < n; i++)
        {
            KitProjectile(player.MuzzlePosition, dir, Damage * 3f, 9999, 70f, "fx_arrow", 1.1f, new Color(1f, 1f, 0.7f), false);
            fx.Play("crack", 0.6f, 1.2f);
            yield return new WaitForSeconds(0.08f);
        }
    }

    IEnumerator KitAcidRainRoutine(Vector3 center, int n)
    {
        float r = 5f * KitExplodeMul;
        for (int i = 0; i < n; i++)
        {
            Vector3 at = Hostile.ClampArena(center + (Vector3)(Random.insideUnitCircle * r));
            Fx.Play("fx_geyser", at + Vector3.up * 1.2f, 2.6f, Color.white, 18f, 0f, 14);
            SpawnZone(at, 1.6f * KitExplodeMul, 4f, Damage * 0.5f, new Color(0.45f, 1f, 0.35f, 0.7f));
            yield return new WaitForSeconds(0.12f);
        }
        fx.Play("hiss", 0.6f, 1f);
    }

    // ================================================================= 패시브 (장착 · 진화 때 한 번)
    void KitOnEquip(int id, bool evolving)
    {
        if (player == null) return;
        float k = evolving ? 0.5f : 1f;          // 진화 때는 절반만 더 (설명의 "진화: ..." 값에 맞춤)
        switch (id)
        {
            case KitIronWill: player.def += evolving ? 0.1f : 0.2f; break;
            case KitVitalStrike: player.damage *= evolving ? 1.16f : 1.25f; break;
            case KitAfterimage: player.speed *= evolving ? 1.13f : 1.15f; player.hurtInvincibleTime += 0.3f * k; break;
            case KitEagleEye: player.damage *= evolving ? 1.13f : 1.15f; if (!evolving) player.pene += 1; break;
            case KitTailwind: player.fireRateMultiplier *= evolving ? 1.13f : 1.15f; if (!evolving) player.speed *= 1.1f; break;
            case KitGoldTouch: player.bonusCoin += 1; player.healOnKill += evolving ? 0.5f : 0.5f; break;
        }
    }
}

// 사냥 덫: 밟은 적을 묶고 크게 피해 (한 번 발동하면 사라짐)
public class HunterTrap : MonoBehaviour
{
    float damage;
    SpecialAbilities owner;
    float life = 20f;
    bool sprung;

    public static void Place(Vector3 at, float damage, SpecialAbilities owner)
    {
        FxAnim a = Fx.Play("fx_target_rune", at, 2.2f, new Color(0.8f, 0.7f, 0.5f, 0.9f), 1f, 0f, 3, true, 20f);
        if (a == null) return;
        HunterTrap t = a.gameObject.AddComponent<HunterTrap>();
        t.damage = damage;
        t.owner = owner;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (sprung || life <= 0f) return;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 1.2f))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            sprung = true;
            Specials.Damage(c.gameObject, damage, Vector3.zero, 0f);
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null) e.Slow(0f, 2f);
            Fx.Play("fx_bonehand", transform.position + Vector3.up * 0.4f, 1.8f, new Color(0.8f, 0.7f, 0.5f), 16f);
            Hostile.Play("clank", 0.8f, 0.7f);
            Destroy(gameObject, 0.1f);
            break;
        }
    }
}

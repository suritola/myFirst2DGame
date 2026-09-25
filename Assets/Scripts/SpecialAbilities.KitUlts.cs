using System.Collections;
using UnityEngine;

// 캐릭터 전용 특수 무기의 우클릭 궁극기 (거너 무기처럼 무기마다 하나씩, 스킬 게이지를 씀)
// 그리고 전용 무기의 발사 소리 (총소리 · 총구 섬광 대신 무기에 맞는 소리)
public partial class SpecialAbilities
{
    static readonly (int id, string name, string desc)[] KitUlts =
    {
        (KitGreatsword, "대지 가르기", "대검을 내리찍어 주변을 크게 가르고 적을 날려 보냅니다. (공격력 600%)"),
        (KitTwinBlades, "칼날 폭풍", "1.5초 동안 두 검을 휘돌려 주변을 쉴 새 없이 벱니다."),
        (KitSpear, "천둥 찌르기", "부채꼴로 세 번 길게 찔러 줄지어 선 적을 모두 꿰뚫습니다. (공격력 450%)"),
        (KitKnifeFan, "칼날 비", "사방으로 단검 36자루를 뿌립니다."),
        (KitBlazeStar, "불꽃 회오리", "소용돌이치며 퍼지는 불꽃 표창 24개를 던져 적을 불태웁니다."),
        (KitChakram, "차크람 폭풍", "차크람 네 개가 3초 동안 주위를 돌며 닿는 적을 벱니다."),
        (KitLongbow, "거인의 화살", "화면을 가로지르는 거대한 화살로 한 줄을 꿰뚫습니다. (공격력 800%)"),
        (KitRepeater, "화살 폭우", "2초 동안 마우스 쪽으로 화살을 퍼붓습니다."),
        (KitBlastArrow, "융단 폭격", "마우스 쪽으로 줄지어 폭발 화살 여덟 발이 터집니다."),
        (KitFireFlask, "지옥불 장판", "마우스 위치에 거대한 불바다를 5초 동안 만듭니다."),
        (KitFrostFlask, "절대 영도", "주변의 적을 모두 얼려 4초 동안 거의 멈추게 하고 피해를 줍니다."),
        (KitShockFlask, "번개 폭풍", "하늘에서 번개 열두 줄기가 적에게 떨어집니다."),
    };

    public static bool IsKitWeapon(int id) => IsKit(id) && KitDefs[id - KitFirstId].kind == SpecialKind.Weapon;
    public static string KitName(int id) => IsKit(id) ? KitDefs[id - KitFirstId].name : "";
    public static string KitDesc(int id) => IsKit(id) ? KitDefs[id - KitFirstId].desc : "";
    public static SpecialKind KitKind(int id) => KitDefs[id - KitFirstId].kind;
    public static int KitCount => KitDefs.Length;

    // 탄약 칸 글 (탄창이 없는 캐릭터가 전용 무기를 들었을 때)
    public string AmmoText(int id) => id >= 0 ? Ammo(id).ammo + " / " + MagSize(id) : "";

    public static string KitUltName(int id)
    {
        foreach (var u in KitUlts) if (u.id == id) return u.name;
        return "";
    }

    public static string KitUltDesc(int id)
    {
        foreach (var u in KitUlts) if (u.id == id) return u.desc;
        return "";
    }

    // 전용 무기를 들고 있으면 우클릭이 그 무기의 궁극기
    public bool KitWeaponUltActive => WeaponActive && IsKitWeapon(CurrentWeapon);

    public void KitWeaponUlt()
    {
        int id = CurrentWeapon;
        fx.Play("levelup", 0.5f, 1.3f);
        Hostile.Shake(0.15f);
        StartCoroutine(KitUltRoutine(id, Damage * UltPower(id) * WeaponDamageMul(id) * (1f + 0.1f * UltTrait(id))));
    }

    // 전용 무기 발사 소리 (케이스 안에서 이미 소리를 내는 무기는 여기서 안 냄)
    void KitShotSound(int id)
    {
        switch (id)
        {
            case KitKnifeFan: fx.Play("whoosh", 0.55f, 1.8f); break;
            case KitBlazeStar: fx.Play("whoosh", 0.35f, 2f); fx.Play("ignite", 0.15f, 1.6f); break;
            case KitBlastArrow: fx.Play("pew", 0.45f, 0.75f); break;
            case KitFireFlask: case KitFrostFlask: case KitShockFlask: fx.Play("whoosh", 0.45f, 1.15f); break;
        }
    }

    IEnumerator KitUltRoutine(int id, float D)
    {
        Vector3 me = player.transform.position;
        Vector3 mouse = MouseWorld();
        Vector2 dir = ((Vector2)(mouse - me)).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bool evo = IsEvolved(id);

        switch (id)
        {
            // ---------------- 검사
            case KitGreatsword:
                {
                    float r = evo ? 8.5f : 7f;
                    Fx.Play("fx_swordswing", me, r * 2.03f, new Color(1f, 0.85f, 0.5f), 20f, 90f, 16);
                    yield return new WaitForSeconds(0.12f);
                    DamageCircle(me, r, D * 6f, 4f);
                    Fx.Play("fx_spinslash", me, r * 2.4f, new Color(1f, 0.8f, 0.4f), 20f);
                    Fx.Play("fx_shock", me, r * 2.2f, new Color(1f, 0.7f, 0.3f, 0.9f), 18f);
                    Hostile.Shake(0.5f);
                    fx.Play("bigboom", 0.8f, 0.8f);
                    fx.Play("slash", 1f, 0.6f);
                    break;
                }
            case KitTwinBlades:
                {
                    float r = evo ? 4.5f : 3.6f;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 p = player.transform.position;
                        DamageCircle(p, r, D * 0.9f, 0.6f);
                        Fx.Play("fx_swordswing", p, r * 2.03f, new Color(0.8f, 0.9f, 1f, 0.85f), 30f, i * 97f, 16);
                        if (i % 2 == 0) fx.Play("slash", 0.5f, 1.3f + 0.05f * i);
                        yield return new WaitForSeconds(0.125f);
                    }
                    break;
                }
            case KitSpear:
                {
                    float len = evo ? 18f : 14f;
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector2 d = Quaternion.Euler(0f, 0f, k * 18f) * dir;
                        Vector3 a = player.MuzzlePosition, b = a + (Vector3)(d * len);
                        DamageLine(a, b, 1.2f, D * 4.5f, 2f);
                        float dr = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                        for (float t = 1f; t < len; t += 1.4f) Fx.Play("fx_trail_dot", a + (Vector3)(d * t), 1.1f, new Color(0.7f, 0.9f, 1f), 1f, dr, 16, false, 0.18f);
                        Fx.Play("fx_markbolt", b, 2.2f, new Color(0.8f, 0.95f, 1f), 22f, dr, 16);
                        fx.Play("thunder", 0.4f, 1.4f);
                        fx.Play("whoosh", 0.7f, 1.7f);
                        yield return new WaitForSeconds(0.14f);
                    }
                    break;
                }

            // ---------------- 도적
            case KitKnifeFan:
                {
                    int n = evo ? 48 : 36;
                    for (int w = 0; w < 2; w++)
                    {
                        for (int i = 0; i < n / 2; i++)
                        {
                            Vector2 d = Quaternion.Euler(0f, 0f, i * 720f / n + w * 5f) * Vector2.right;
                            KitProjectile(player.transform.position, d, D * 1.2f, player.pene + 1, 38f, "fx_arrow", 0.4f, new Color(0.85f, 0.85f, 1f), false);
                        }
                        fx.Play("whoosh", 0.8f, 1.9f);
                        yield return new WaitForSeconds(0.15f);
                    }
                    break;
                }
            case KitBlazeStar:
                {
                    float burn = D * (evo ? 0.8f : 0.5f);
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 d = Quaternion.Euler(0f, 0f, i * 37f) * dir;
                        Bullet b = KitProjectile(player.transform.position, d, D * 1f, player.pene, 30f, "fx_shuriken", 0.9f, new Color(1f, 0.6f, 0.3f), true);
                        if (b != null) b.onHitEnemy += (bullet, c) => Burn.Apply(c.gameObject, burn, 3f);
                        if (i % 3 == 0) fx.Play("ignite", 0.35f, 1.3f);
                        yield return new WaitForSeconds(0.04f);
                    }
                    break;
                }
            case KitChakram:
                {
                    Sprite[] f = Fx.Frames("fx_shuriken");
                    GameObject[] rings = new GameObject[4];
                    for (int i = 0; i < 4; i++)
                    {
                        rings[i] = MakeSprite("UltChakram", f.Length > 0 ? f[0] : null, me, 1f, new Color(0.6f, 0.95f, 1f), "Effect", 12);
                        if (f.Length > 0) rings[i].transform.localScale = Vector3.one * (1.8f / f[0].bounds.size.y);
                        FrameLoop loop = rings[i].AddComponent<FrameLoop>();
                        loop.frames = f;
                        loop.spin = -1100f;
                    }
                    float radius = evo ? 4.5f : 3.6f, tick = 0f;
                    for (float t = 0f; t < 3f; t += Time.deltaTime)
                    {
                        tick -= Time.deltaTime;
                        for (int i = 0; i < 4; i++)
                        {
                            float a = t * 5f + i * Mathf.PI * 0.5f;
                            float rr = radius * Mathf.Min(1f, t * 3f);
                            rings[i].transform.position = player.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rr;
                            if (tick <= 0f) DamageCircle(rings[i].transform.position, 1.3f, D * 0.8f, 0.6f);
                        }
                        if (tick <= 0f) { tick = 0.2f; fx.Play("whoosh", 0.25f, 1.6f); }
                        yield return null;
                    }
                    foreach (GameObject g in rings) if (g != null) Destroy(g);
                    break;
                }

            // ---------------- 궁수
            case KitLongbow:
                {
                    Vector3 a = player.MuzzlePosition, b = a + (Vector3)(dir * 30f);
                    Fx.Play("fx_shock", a, 3f, new Color(1f, 1f, 0.7f), 20f);
                    yield return new WaitForSeconds(0.15f);
                    DamageLine(a, b, 1.6f, D * 8f, 3f);
                    for (float t = 0f; t < 30f; t += 1.2f) Fx.Play("fx_arrow", a + (Vector3)(dir * t), 1.6f, new Color(1f, 1f, 0.75f, 1f - t / 34f), 12f, rot, 16);
                    Fx.Beam(a, b, 0.8f, new Color(1f, 0.95f, 0.6f, 0.8f), 0.25f);
                    Hostile.Shake(0.35f);
                    fx.Play("railgun", 0.6f, 1.2f);
                    break;
                }
            case KitRepeater:
                for (float t = 0f; t < 2f; t += 0.05f)
                {
                    Vector3 m = MouseWorld();
                    Vector2 d = ((Vector2)(m - player.MuzzlePosition)).normalized;
                    for (int k = 0; k < (evo ? 3 : 2); k++)
                    {
                        Vector2 dd = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f)) * d;
                        KitProjectile(player.MuzzlePosition, dd, D * 0.7f, player.pene, 70f, "fx_arrow", 0.4f, Color.white, false);
                    }
                    fx.Play("pew", 0.25f, 1.4f + Random.Range(-0.1f, 0.1f));
                    yield return new WaitForSeconds(0.05f);
                }
                break;
            case KitBlastArrow:
                {
                    float r = (evo ? 3.4f : 2.8f) * KitExplodeMul;
                    for (int i = 1; i <= 8; i++)
                    {
                        Vector3 p = me + (Vector3)(dir * (1.8f * i + 1f));
                        Explode(p, r, D * 2.2f, 1.5f, new Color(1f, 0.55f, 0.2f, 0.9f));
                        if (i % 2 == 0) Hostile.Shake(0.12f);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }

            // ---------------- 연금술사
            case KitFireFlask:
                {
                    float r = (evo ? 6f : 5f) * KitExplodeMul;
                    FlaskLob.Throw(player.MuzzlePosition, mouse, 0.5f, 1.6f, new Color(1f, 0.6f, 0.3f), p =>
                    {
                        Explode(p, r, D * 3f, 2f, new Color(1f, 0.45f, 0.1f, 0.9f));
                        DamageZone z = SpawnZone(p, r, 5f, D * 0.7f, new Color(1f, 0.35f, 0.08f, 0.8f));
                        z.lava = true;
                        Hostile.Shake(0.3f);
                        fx.Play("flame", 0.8f, 0.8f);
                    });
                    break;
                }
            case KitFrostFlask:
                {
                    float r = (evo ? 10f : 8f) * KitExplodeMul;
                    Fx.Play("fx_shock", me, r * 2f, new Color(0.6f, 0.9f, 1f, 0.9f), 14f);
                    Fx.Play("fx_alchemyblast", me, r * 1.6f, new Color(0.6f, 0.9f, 1f), 14f);
                    foreach (Collider2D c in Physics2D.OverlapCircleAll(me, r))
                    {
                        if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                        Specials.Damage(c.gameObject, D * 3f, (c.transform.position - me).normalized, 0.3f);
                        EnermyController e = c.GetComponent<EnermyController>();
                        if (e != null) e.Slow(0.08f, 4f);
                        Fx.Play("fx_sparkle", c.transform.position, 1.2f, new Color(0.7f, 0.95f, 1f), 16f);
                    }
                    fx.Play("shimmer", 1f, 0.7f);
                    fx.Play("crack", 0.6f, 0.6f);
                    break;
                }
            case KitShockFlask:
                for (int i = 0; i < (evo ? 16 : 12); i++)
                {
                    Transform t = Specials.NearestEnemy(player.transform.position + (Vector3)(Random.insideUnitCircle * 6f), 14f);
                    Vector3 p = t != null ? t.position : player.transform.position + (Vector3)(Random.insideUnitCircle * 6f);
                    Fx.Beam(p + Vector3.up * 14f, p, 0.6f, new Color(1f, 0.95f, 0.5f, 0.9f), 0.15f);
                    DamageCircle(p, 1.8f, D * 2f, 0.8f);
                    Fx.Play("fx_shock", p, 2.2f, new Color(1f, 0.95f, 0.5f), 22f);
                    fx.Play("zap", 0.5f, Random.Range(0.9f, 1.3f));
                    if (i % 4 == 0) fx.Play("thunder", 0.5f, 1f);
                    yield return new WaitForSeconds(0.1f);
                }
                break;
        }
    }
}

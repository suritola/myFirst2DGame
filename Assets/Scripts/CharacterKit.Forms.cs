using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 거너가 아닌 캐릭터의 무기 진화 (1.7.8~): 평타 자체가 바뀜 → 레벨업 카드(검기 · 도탄 표창 …)는 그대로 붙음
//   1차 진화(형태): 캐릭터 전용 무기 셋 중 하나의 모양으로 평타가 바뀜
//     검사     전쟁 망치(주변 내리찍기 · 기절) · 채찍검(길고 가늘게, 끝이 아픔) · 기창(앞으로 찌르며 돌진)
//     도적     독침 대롱(빠른 독침 · 중독) · 도박 카드(카드 세 장 · 조커 폭발) · 살상 와이어(맞은 적에서 줄이 튐)
//     궁수     그물 활(맞은 자리 묶기) · 투창(모두 관통 · 멀리 갈수록 강함) · 속사 활(세 발 연사)
//     연금술사 수은 구슬(두 번 더 튕겨 터짐) · 자석 폭탄(끌어모아 터짐) · 폭죽 발사기(불꽃 다섯 갈래)
//   2차 진화(강화): 형태 위에 덧붙는 효과 (캐릭터마다 셋)
public partial class CharacterKit
{
    // 2차 진화 강화 번호 (특수 능력 번호와 겹치지 않게 100부터)
    public const int AugStorm = 100, AugExecute = 101, AugThunder = 102;       // 검사
    public const int AugEcho = 103, AugVenom = 104, AugVolley = 105;           // 도적
    public const int AugBlast = 106, AugSeek = 107, AugGale = 108;             // 궁수
    public const int AugDouble = 109, AugGiant = 110, AugOverload = 111;       // 연금술사
    public const int AugFirst = 100, AugLast = 111;

    [HideInInspector] public int form = -1;         // 1차 진화 형태 (캐릭터 전용 무기 번호), -1 = 기본
    [HideInInspector] public int augment = -1;      // 2차 진화 강화
    [HideInInspector] public float formPower = 1f;  // 영혼 트리 형태 숙련: 평타 피해 배율
    int swingCount, throwCount;

    public static int[] AugmentsFor(CharacterId who) => who switch
    {
        CharacterId.Swordsman => new[] { AugStorm, AugExecute, AugThunder },
        CharacterId.Rogue => new[] { AugEcho, AugVenom, AugVolley },
        CharacterId.Archer => new[] { AugBlast, AugSeek, AugGale },
        _ => new[] { AugDouble, AugGiant, AugOverload },
    };

    public static string AugmentName(int id) => id switch
    {
        AugStorm => "폭풍", AugExecute => "처단", AugThunder => "뇌광",
        AugEcho => "잔상", AugVenom => "맹독", AugVolley => "연발",
        AugBlast => "폭발 화살", AugSeek => "추적 화살", AugGale => "꿰뚫는 바람",
        AugDouble => "이중 폭발", AugGiant => "거대 플라스크", _ => "연금 폭주",
    };

    public static string AugmentDesc(int id) => id switch
    {
        AugStorm => "세 번 벨 때마다 한 번은 몸 주위를 한 바퀴 크게 벱니다.",
        AugExecute => "체력 25% 아래인 적은 세 배로 벱니다.",
        AugThunder => "벨 때마다 처음 맞은 적에게서 번개가 적 셋에게 튑니다.",
        AugEcho => "던질 때마다 잠시 뒤 그림자가 같은 방향으로 한 번 더 던집니다. (피해 60%)",
        AugVenom => "모든 평타가 독을 쌓습니다. (최대 6중첩)",
        AugVolley => "한 번에 하나 더 던집니다.",
        AugBlast => "화살이 맞은 자리에서 폭발합니다. (피해 60%)",
        AugSeek => "화살이 가까운 적을 쫓아갑니다.",
        AugGale => "화살이 적 셋을 더 꿰뚫고 30% 더 빠르게 날아갑니다.",
        AugDouble => "플라스크가 터진 자리에서 한 번 더 터집니다. (피해 60%)",
        AugGiant => "폭발 범위 +40%, 피해 +20%",
        _ => "네 번째 플라스크마다 반드시 불안정해져 크게 터집니다.",
    };

    // 형태 설명 (진화 카드 · 트리)
    public static string FormDesc(int id) => id switch
    {
        SpecialAbilities.KitHammer => "평타가 몸 주위를 크게 내리찍습니다. 느리지만 피해 200%, 맞은 적은 잠깐 기절.",
        SpecialAbilities.KitWhip => "평타가 길고 가는 채찍 베기가 됩니다. 사거리 180%, 끝부분에 맞은 적은 두 배 피해.",
        SpecialAbilities.KitLance => "평타가 앞으로 짧게 돌진하며 길게 찌릅니다. 사거리 150%, 피해 170%, 돌진 중 무적.",
        SpecialAbilities.KitBlowgun => "평타가 빠른 독침이 됩니다. 공격 속도 170%, 한 발 피해 55%, 맞은 적에게 독이 쌓임.",
        SpecialAbilities.KitCards => "평타가 카드 세 장이 됩니다. 장마다 피해가 제각각 (50~170%), 가끔 조커가 폭발.",
        SpecialAbilities.KitWire => "평타 표창이 맞은 적에게서 가까운 적 둘에게 줄이 튀어 벱니다. (피해 70%)",
        SpecialAbilities.KitNetBow => "화살이 맞은 자리에 그물을 펼쳐 주변 적을 잠깐 묶습니다.",
        SpecialAbilities.KitJavelin => "화살 대신 무거운 투창. 모든 적을 꿰뚫고 멀리 날아갈수록 강해집니다. (최대 300%) 당기는 속도 80%",
        SpecialAbilities.KitBurstBow => "한 번 쏠 때 화살 세 발을 연달아 쏩니다. (발당 피해 60%)",
        SpecialAbilities.KitQuicksilver => "플라스크가 터진 뒤 가까운 적에게 두 번 더 튕겨 터집니다. (튕길 때마다 피해 70%)",
        SpecialAbilities.KitMagnet => "플라스크가 터지기 전에 주변 적을 끌어모읍니다. 피해 125%",
        SpecialAbilities.KitFirework => "플라스크가 터지면 불꽃 다섯 갈래가 퍼져 한 번씩 더 터집니다. (피해 45%)",
        _ => "",
    };

    // 형태별 공격 속도 배율 (PlayerController 가 평타 간격 · 활 당기는 시간에 씀)
    float FormRate => form switch
    {
        SpecialAbilities.KitHammer => 0.6f,
        SpecialAbilities.KitLance => 0.85f,
        SpecialAbilities.KitBlowgun => 1.7f,
        SpecialAbilities.KitCards => 0.85f,
        SpecialAbilities.KitJavelin => 0.8f,
        _ => 1f,
    };

    // ================================================================= 검사
    float SwingReachMul => form switch
    {
        SpecialAbilities.KitHammer => 0.85f,
        SpecialAbilities.KitWhip => 1.8f,
        SpecialAbilities.KitLance => 1.5f,
        _ => 1f,
    };

    float SwingHalf(float half) => form switch
    {
        SpecialAbilities.KitHammer => 180f,                 // 몸 주위 전부
        SpecialAbilities.KitWhip => Mathf.Min(half, 22f),   // 가늘게
        SpecialAbilities.KitLance => Mathf.Min(half, 16f),  // 창끝 한 줄
        _ => half,
    };

    // 적 하나를 벨 때 피해 배율 (형태 · 강화)
    float SwingHitMul(Collider2D c, float dist, float reach)
    {
        float m = formPower;
        if (form == SpecialAbilities.KitHammer) m *= 2f;
        if (form == SpecialAbilities.KitWhip && dist > reach * 0.6f) m *= 2f;
        if (form == SpecialAbilities.KitLance) m *= 1.7f;
        if (augment == AugExecute && c.TryGetComponent(out EnermyController e) && e.EnemyHealth <= e.setEnemyHP * 0.25f) m *= 3f;
        return m;
    }

    void OnSwingHit(Collider2D c, bool first)
    {
        if (form == SpecialAbilities.KitHammer && c.TryGetComponent(out EnermyController e)) e.Slow(0f, 0.5f);
        if (first && augment == AugThunder) ChainBolt(c, Damage * 0.6f * formPower, 3);
    }

    void SwingFormBefore(Vector2 dir)
    {
        if (form == SpecialAbilities.KitLance) StartCoroutine(Lunge(dir));
    }

    void SwingFormAfter(Vector3 origin, float reach)
    {
        if (form == SpecialAbilities.KitHammer)
        {
            Fx.Spawn("fx_shock", origin, reach * 2.2f, new Color(0.8f, 0.85f, 1f, 0.8f), 20f);
            Hostile.Shake(0.12f);
            Play("thump", 0.8f, 0.8f);
        }
        if (augment == AugStorm && ++swingCount % 3 == 0)
        {
            DamageCircle(origin, reach * 1.2f, Damage * 1.2f * formPower, 2.5f);
            Fx.Spawn("fx_shock", origin, reach * 2.6f, new Color(0.7f, 0.9f, 1f, 0.8f), 18f);
            Play("whoosh", 0.7f, 0.7f);
        }
    }

    // 기창: 앞으로 짧게 돌진 (돌진 중 무적)
    IEnumerator Lunge(Vector2 dir)
    {
        player.GrantInvincibility(0.2f);
        Vector3 to = Hostile.ClampArena(transform.position + (Vector3)(dir * 2.5f));
        Vector3 from = transform.position;
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, to, t / 0.12f);
            yield return null;
        }
        transform.position = to;
    }

    // 번개: 처음 맞은 적에서 가까운 적 n 명에게 차례로
    void ChainBolt(Collider2D first, float dmg, int jumps)
    {
        Transform cur = first.transform;
        HashSet<Transform> hit = new HashSet<Transform> { cur };
        for (int i = 0; i < jumps; i++)
        {
            Transform next = null;
            float best = 6f;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(cur.position, 6f))
            {
                if (hit.Contains(c.transform) || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                float d = Vector2.Distance(cur.position, c.transform.position);
                if (d < best) { best = d; next = c.transform; }
            }
            if (next == null) break;
            hit.Add(next);
            SparkLine(cur.position, next.position, new Color(0.6f, 0.9f, 1f));
            Specials.Damage(next.gameObject, dmg, Vector3.zero, 0f);
            cur = next;
        }
        Play("zap", 0.4f, 1.1f);
    }

    static void SparkLine(Vector3 a, Vector3 b, Color c)
    {
        for (int i = 1; i <= 4; i++) Fx.Spawn("fx_spark", Vector3.Lerp(a, b, i / 4f), 1f, c, 26f);
    }

    // ================================================================= 도적
    void RogueThrow(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        if (augment == AugVolley) shots++;
        if (form == SpecialAbilities.KitCards) { shots += 2; dmg *= 1f; }
        float step = form == SpecialAbilities.KitCards ? 9f : 8f;
        foreach (Vector2 d in Spread(dir, shots, step)) ThrowOne(start, d, dmg, 1f);
        Play("whoosh", 0.35f, form == SpecialAbilities.KitBlowgun ? 2.3f : 1.8f);
        if (augment == AugEcho) StartCoroutine(EchoThrow(dir, dmg, shots, step));
    }

    IEnumerator EchoThrow(Vector2 dir, float dmg, int shots, float step)
    {
        yield return new WaitForSeconds(0.15f);
        Vector3 start = player.MuzzlePosition;
        foreach (Vector2 d in Spread(dir, shots, step))
        {
            Bullet b = ThrowOne(start, d, dmg * 0.6f, 0.6f);
            if (b != null && b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.55f, 0.4f, 0.8f, 0.8f);
        }
    }

    Bullet ThrowOne(Vector3 start, Vector2 d, float dmg, float alpha)
    {
        float mul = formPower;
        if (form == SpecialAbilities.KitBlowgun) mul *= 0.55f;
        if (form == SpecialAbilities.KitCards) mul *= Random.Range(0.5f, 1.7f);
        Bullet b = Shuriken(start, d, dmg * mul, card[0]);
        if (b == null) return null;
        SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
        switch (form)
        {
            case SpecialAbilities.KitBlowgun:
                b.transform.localScale *= 0.55f;
                if (sr != null) sr.color = new Color(0.6f, 1f, 0.45f, alpha);
                break;
            case SpecialAbilities.KitCards:
                bool joker = Random.value < 0.12f;
                if (sr != null) sr.color = joker ? new Color(1f, 0.85f, 0.3f, alpha) : new Color(1f, 0.55f, 0.6f, alpha);
                if (joker)
                    b.onHitEnemy += (s, c) =>
                    {
                        DamageCircle(c.transform.position, 2.2f, Damage * 1.8f * formPower, 2f);
                        Fx.Spawn("fx_explosion", c.transform.position, 4.4f, Color.white, 16f);
                        Play("boom", 0.5f, 1.3f);
                    };
                break;
            case SpecialAbilities.KitWire:
                if (sr != null) sr.color = new Color(0.8f, 0.85f, 1f, alpha);
                b.onHitEnemy += (s, c) => ChainBolt(c, Damage * 0.7f * formPower, 2);
                break;
        }
        if (form == SpecialAbilities.KitBlowgun || augment == AugVenom)
        {
            float per = Damage * 0.12f;
            int max = augment == AugVenom ? 6 : 5;
            b.onHitEnemy += (s, c) => Poison.Apply(c.gameObject, per, max);
        }
        return b;
    }

    // ================================================================= 궁수
    // 화살 하나에 형태 · 강화 (UpdateBow 가 쏠 때마다)
    void ArrowForm(Bullet b, float draw, Vector3 start)
    {
        b.damage *= formPower;
        switch (form)
        {
            case SpecialAbilities.KitNetBow:
                if (draw >= 0.5f)
                    b.onHitEnemy += (s, c) =>
                    {
                        foreach (Collider2D o in Physics2D.OverlapCircleAll(c.transform.position, 2.6f))
                            if (o.TryGetComponent(out EnermyController e)) e.Slow(0f, 0.8f + 0.6f * draw);
                        Fx.Spawn("fx_shock", c.transform.position, 5.2f, new Color(0.7f, 1f, 0.6f, 0.7f), 18f);
                    };
                break;
            case SpecialAbilities.KitJavelin:
                b.pene = 9999;
                b.transform.localScale *= 1.6f;
                b.speed *= 0.8f;
                float baseDmg = b.damage;
                b.onHitEnemy += (s, c) =>
                {
                    // 멀리 날아갈수록 강해짐 (최대 3배): 기본 피해는 이미 들어갔으니 더할 몫만
                    float k = Mathf.Min(3f, 1f + Vector2.Distance(start, c.transform.position) / 8f);
                    if (k > 1f) Specials.Damage(c.gameObject, baseDmg * (k - 1f), Vector3.zero, 0f);
                };
                break;
            case SpecialAbilities.KitBurstBow:
                b.damage *= 0.6f;
                break;
        }
        switch (augment)
        {
            case AugBlast:
                float boom = b.damage * 0.6f;
                b.onHitEnemy += (s, c) =>
                {
                    DamageCircle(c.transform.position, 2f, boom, 1.5f);
                    Fx.Spawn("fx_explosion", c.transform.position, 4f, Color.white, 16f);
                };
                break;
            case AugSeek:
                b.gameObject.AddComponent<Homing>().turnSpeed = 240f;
                break;
            case AugGale:
                b.pene += 3;
                b.speed *= 1.3f;
                break;
        }
    }

    // 속사 활: 뒤따르는 두 발
    IEnumerator BurstFollow(Vector2 dir, float dmg, float speed, float size, float draw)
    {
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForSeconds(0.08f);
            Vector3 start = player.MuzzlePosition;
            Bullet b = Projectile(start, dir, dmg, player.pene + 1, speed, 0f, "fx_arrow", size, Color.white, false);
            if (b != null) ArrowForm(b, draw, start);
            Play("pew", 0.35f, 1.1f);
        }
    }

    // ================================================================= 연금술사
    float FlaskRadiusMul => augment == AugGiant ? 1.4f : 1f;
    float FlaskDamageMul => formPower * (augment == AugGiant ? 1.2f : 1f) * (form == SpecialAbilities.KitMagnet ? 1.25f : 1f);

    bool ForceUnstable() => augment == AugOverload && ++throwCount % 4 == 0;

    // 터지는 순간 (피해를 주기 전): 끌어모으기 · 튕기기 · 불꽃 · 두 번째 폭발
    void FlaskExtras(Vector3 p, float r, float hit, int depth)
    {
        if (form == SpecialAbilities.KitMagnet)
        {
            foreach (Collider2D c in Physics2D.OverlapCircleAll(p, r * 2.2f))
                if (c.CompareTag("enermy")) c.transform.position = Vector3.Lerp(c.transform.position, p, 0.7f);
            Fx.Spawn("fx_shock", p, r * 4.4f, new Color(0.6f, 0.7f, 1f, 0.6f), 22f);
        }
        if (form == SpecialAbilities.KitQuicksilver && depth < 2)
        {
            Transform next = NearestOther(p, 7f, null, Vector2.right, 360f);
            Vector3 land = next != null ? next.position : p + (Vector3)(Random.insideUnitCircle.normalized * 3f);
            ThrowReagent(p, land, hit / 1.6f * 0.7f, depth + 1);
        }
        if (form == SpecialAbilities.KitFirework)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad + Random.Range(-0.3f, 0.3f);
                Vector3 q = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r * 1.5f;
                DamageCircle(q, r * 0.5f, hit * 0.45f, 0.8f);
                Fx.Spawn("fx_sparkle", q, r * 1.2f, Color.HSVToRGB(i / 5f, 0.6f, 1f), 20f);
            }
        }
        if (augment == AugDouble) StartCoroutine(SecondBlast(p, r * 0.8f, hit * 0.6f));
    }

    IEnumerator SecondBlast(Vector3 p, float r, float hit)
    {
        yield return new WaitForSeconds(0.35f);
        DamageCircle(p, r, hit, 1.2f);
        Fx.Spawn("fx_alchemyblast", p, r * 2.2f, new Color(1f, 0.8f, 0.5f), 18f);
        Play("boom", 0.4f, 1.3f);
    }

    // ================================================================= 영혼 트리 · 진화 연결
    public void SetForm(int id)
    {
        form = id;
        Fx.Spawn("fx_shock", transform.position, 8f, new Color(1f, 0.85f, 0.4f, 0.8f), 18f);
    }

    public void SetAugment(int id) => augment = id;
}

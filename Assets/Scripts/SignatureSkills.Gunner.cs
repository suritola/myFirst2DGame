using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 거너 고유 스킬 · 진화
//   g.mine 탄피 지뢰 · g.shrapnel 처치 파편 · g.heat 총열 과열 · g.soul 영혼 탄환 · g.flash 섬광 장전
//   g.counter 반격 사격 · g.quick 속사 장전 · g.runreload 달리며 장전 · g.scavenge 전리품 탄약 · g.threat 위협 사격
public partial class SignatureSkills
{
    static readonly Color Brass = new Color(1f, 0.82f, 0.4f);
    static readonly Color SoulBlue = new Color(0.55f, 0.95f, 1f);
    const int HeatMax = 10;

    int heat;
    float lastShot;
    int quickShots;
    float fragBudgetAt;
    int fragBudget;
    readonly List<SigZone> mines = new List<SigZone>();

    // ---------------- 게임 코드가 부르는 거너 훅
    public static void ReloadStart() { if (Instance != null) Instance.OnReloadStart(); }
    public static void ReloadEnd() { if (Instance != null) Instance.OnReloadEnd(); }
    public static void GunBullet(Bullet b) { if (Instance != null && b != null && Instance.who == CharacterId.Gunner) Instance.OnGunBullet(b); }

    void OnReloadStart()
    {
        if (who == CharacterId.Rogue) { RogueEmpty(); return; }
        if (who != CharacterId.Gunner) return;
        int m = L("g.mine");
        if (m <= 0) return;
        int n = m + 1;
        if (E("ge.thunder")) n *= 2;
        for (int i = 0; i < n; i++)
        {
            Vector3 at = Hostile.ClampArena(transform.position + (Vector3)(Random.insideUnitCircle * 2.6f));
            if (Hostile.IsWall(at)) continue;
            SpawnMine(at);
        }
        Hostile.Play("clank", 0.4f, 1.6f);
    }

    void SpawnMine(Vector3 at)
    {
        mines.RemoveAll(z => z == null);
        if (mines.Count >= 24) { if (mines[0] != null) Destroy(mines[0].gameObject); mines.RemoveAt(0); }
        SigZone z = Zone(at, 0.9f, 12f, 0.1f, null, "fx_caltrop", Brass, 0.9f);
        z.onTick = zone =>
        {
            if (Inside(zone).Count == 0) return;
            Vector3 p = zone.transform.position;
            Circle(p, 2f, Atk * 0.8f, 1f);
            Fx.Spawn("fx_explosion", p, 3.6f, Color.white, 20f);
            Hostile.Play("pop", 0.4f, 1.2f);
            Destroy(zone.gameObject);
        };
        mines.Add(z);
    }

    void OnReloadEnd()
    {
        if (who != CharacterId.Gunner) return;
        Vector3 p = transform.position;
        int f = L("g.flash");
        if (f > 0)
        {
            Circle(p, 5f, 0f, 0f, c => Stun(c.gameObject, V(f, 0.6f, 0.3f)));
            Fx.Spawn("fx_shock", p, 10f, new Color(1f, 1f, 0.85f, 0.9f), 24f);
            Fx.Spawn("fx_sparkle", p, 4f, Color.white, 20f);
            Hostile.Play("shimmer", 0.5f, 1.8f);
        }
        if (E("ge.thunder"))
        {
            foreach (Transform t in Nearest(p, 9f, 6))
            {
                Fx.Bolt(t.position + new Vector3(Random.Range(-1.5f, 1.5f), 12f), t.position, 1.2f, SoulBlue, 0.2f);
                Deal(t.gameObject, Atk * 1.5f, Vector3.zero, 0f);
                Fx.Spawn("fx_shock", t.position, 2.6f, SoulBlue, 24f);
            }
            Hostile.Play("thunder", 0.6f, 1.2f);
        }
        if (L("g.quick") > 0) quickShots = L("g.quick") + 1;
        if (E("ge.dance"))
        {
            foreach (Transform t in Nearest(p, 14f, 6))
            {
                Vector2 d = ((Vector2)(t.position - player.MuzzlePosition)).normalized;
                Bullet b = player.CreateBullet(player.MuzzlePosition, d, Atk, player.pene, 0f, true);
                if (b != null) b.critRolled = true;
            }
            Hostile.Play("gunshot", 0.5f, 1.3f);
        }
    }

    void OnGunBullet(Bullet b)
    {
        // 총열 과열: 쉬지 않고 쏠수록 (마지막 발사에서 1.5초 지나면 식음)
        int h = L("g.heat");
        if (h > 0 && heat > 0)
        {
            b.damage *= 1f + V(h, 0.2f, 0.1f) * heat / (float)HeatMax;
            if (heat >= HeatMax && b.TryGetComponent(out SpriteRenderer hs)) hs.color = Color.Lerp(hs.color, new Color(1f, 0.45f, 0.2f), 0.6f);
        }
        if (E("ge.furnace") && heat >= HeatMax)
        {
            b.onHitEnemy += (bullet, col) =>
            {
                Vector3 p = col.transform.position;
                Circle(p, 1.7f, Atk * 0.5f, 0.5f, c => Burn.Apply(c.gameObject, Atk * 0.3f, 2f));
                Fx.Spawn("fx_explosion", p, 3.2f, new Color(1f, 0.6f, 0.3f), 22f);
            };
        }
        // 영혼 탄환: 필살기 게이지가 가득 찬 동안
        int s = L("g.soul");
        SkillGauge g = Cache<SkillGauge>.Get;
        if (s > 0 && g != null && g.IsFull())
        {
            b.damage *= 1f + V(s, 0.25f, 0.15f);
            b.pene += 1;
            if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = SoulBlue;
            if (E("ge.seeker"))
            {
                if (b.GetComponent<Homing>() == null) b.gameObject.AddComponent<Homing>().turnSpeed = 540f;
                b.onHitEnemy += (bullet, col) =>
                {
                    List<Transform> near = Nearest(col.transform.position, 10f, 1, col.gameObject);
                    if (near.Count == 0) return;
                    Vector2 d = ((Vector2)(near[0].position - col.transform.position)).normalized;
                    Bullet orb = Shot(col.transform.position, d, Atk * 0.6f, 1, 22f, 12f, "fx_orb", 0.8f, SoulBlue);
                    if (orb != null) orb.gameObject.AddComponent<Homing>().turnSpeed = 720f;
                };
            }
        }
    }

    void GunnerAttack()
    {
        lastShot = Time.time;
        if (L("g.heat") > 0) heat = Mathf.Min(HeatMax, heat + 1);
        if (quickShots > 0)
        {
            quickShots--;
            player.QuickShot();
        }
    }

    void GunnerTick()
    {
        if (heat > 0 && Time.time - lastShot > 1.5f) heat = 0;
    }

    float GunnerMove()
    {
        int r = L("g.runreload");
        return r > 0 && player.IsReloading ? 1f + V(r, 0.15f, 0.1f) : 1f;
    }

    void GunnerHurt(float taken)
    {
        int c = L("g.counter");
        Vector3 p = transform.position;
        if (c > 0)
        {
            List<Transform> near = Nearest(p, 14f, 4);
            int n = 1 + 2 * c;
            for (int i = 0; i < n && near.Count > 0; i++)
            {
                Transform t = near[i % near.Count];
                if (t == null) continue;
                Vector2 d = ((Vector2)(t.position - player.MuzzlePosition)).normalized;
                d = Quaternion.Euler(0f, 0f, Random.Range(-6f, 6f)) * d;
                Bullet b = player.CreateBullet(player.MuzzlePosition, d, Atk, player.pene, 0f, true);
                if (b != null) b.critRolled = true;
            }
            Hostile.Play("gunshot", 0.6f, 1.1f);
        }
        if (E("ge.thunderback"))
        {
            Circle(p, 6f, Atk * 2f, 2f, col => Stun(col.gameObject, 0.8f));
            for (int i = 0; i < 6; i++)
            {
                Vector3 to = p + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2f, 6f));
                Fx.Bolt(p, to, 1f, SoulBlue, 0.25f);
            }
            Fx.Spawn("fx_shock", p, 12f, SoulBlue, 20f);
            Hostile.Play("thunder", 0.7f, 1f);
        }
    }

    void GunnerKilled(Vector3 pos)
    {
        int s = L("g.shrapnel");
        if (s > 0)
        {
            // 몰린 적이 한꺼번에 쓰러져도 파편이 끝없이 늘지 않게 (0.2초에 24개까지)
            if (Time.time > fragBudgetAt) { fragBudgetAt = Time.time + 0.2f; fragBudget = 24; }
            int n = (s + 2) * (E("ge.storm") ? 2 : 1);
            n = Mathf.Min(n, fragBudget);
            fragBudget -= n;
            float off = Random.Range(0f, 360f);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, off + i * 360f / Mathf.Max(1, n)) * Vector2.right;
                Bullet b = Shot(pos, d, Atk * 0.4f, 1, 24f, 6f, "fx_spark", 0.7f, Brass);
                if (b != null && E("ge.storm"))
                {
                    bool bounced = false;
                    b.onHitEnemy += (bullet, col) =>
                    {
                        if (bounced) return;
                        bounced = true;
                        List<Transform> near = Nearest(col.transform.position, 8f, 1, col.gameObject);
                        if (near.Count == 0) return;
                        Shot(col.transform.position, ((Vector2)(near[0].position - col.transform.position)).normalized, Atk * 0.4f, 1, 26f, 8f, "fx_spark", 0.6f, Brass);
                    };
                }
            }
        }
        int sc = L("g.scavenge");
        if (sc > 0 && player.NowBullet < player.MaxBullet && Random.value < V(sc, 0.15f, 0.1f)) player.NowBullet++;
    }

    void GunnerHit(GameObject target, float dmg, bool killed, bool proc)
    {
        if (!killed || !E("ge.outlaw") || !Marked(target)) return;
        player.NowBullet = player.MaxBullet;
        foreach (Transform t in Nearest(target.transform.position, 6f, 3, target))
        {
            Mark(t.gameObject, 4f, MarkMul(target));
            EnermyController e = t.GetComponent<EnermyController>();
            if (e != null) e.Slow(0.5f, 4f);
        }
        Fx.Spawn("fx_shock", target.transform.position, 6f, new Color(1f, 0.4f, 0.3f, 0.8f), 22f);
    }

    void GunnerUlt()
    {
        int t = L("g.threat");
        if (t <= 0) return;
        Vector3 p = transform.position;
        float mul = 1f + V(t, 0.1f, 0.08f);
        Circle(p, 12f, 0f, 0f, c =>
        {
            Mark(c.gameObject, 4f, mul);
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null) e.Slow(0.5f, 4f);
        });
        ShockRing.Spawn(p, 0.5f, 12f, 0.5f, new Color(1f, 0.35f, 0.3f, 0.8f), 0.3f);
    }

    float GunnerOutgoing(GameObject target) => MarkMul(target);
}

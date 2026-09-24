using System.Collections.Generic;
using UnityEngine;

// 우클릭 타겟팅 중 화면: 들고 있는 무기의 필살기에 맞춘 조준 연출
// (SpecialAbilities가 만들어 쓰는 보조 객체, MonoBehaviour 아님)
public class WeaponAim
{
    readonly SpecialAbilities owner;
    readonly List<GameObject> keep = new List<GameObject>();       // 조준이 끝날 때 지울 것
    readonly List<GameObject> frame = new List<GameObject>();      // 매 프레임 새로 그리는 것
    readonly List<LineRenderer> lines = new List<LineRenderer>();
    readonly List<FxAnim> orbs = new List<FxAnim>();
    FxAnim rune, ghost, cloud, reticle;
    int weapon;
    float age;

    public WeaponAim(SpecialAbilities owner) { this.owner = owner; }

    Transform Player => owner.PlayerTransform;
    Color C => SpecialAbilities.ColorOf(weapon);

    // 조준한 적 위에 붙는 표식 (무기마다 모양이 다름). null이면 표식 없음
    public GameObject MarkTarget(Transform target)
    {
        string sprite; float size; Color c = C; float spin = 0f;
        switch (weapon)
        {
            case SpecialAbilities.SeekerId: sprite = "fx_orb"; size = 1.2f; break;
            case SpecialAbilities.ChainId: sprite = "fx_markbolt"; size = 1.4f; break;
            case SpecialAbilities.ScytheId: sprite = "fx_markskull"; size = 1.3f; break;
            case SpecialAbilities.ShotgunId:
            case SpecialAbilities.SniperId:
            case SpecialAbilities.DualId:
            case SpecialAbilities.FlameId:
            case SpecialAbilities.GrenadeId:
                return null;                                         // 범위형: 표식 대신 범위를 보여줌
            default: sprite = "fx_reticle"; size = 2f; spin = 90f; break;
        }
        FxAnim a = Fx.Play(sprite, target.position + Vector3.up * 1.4f, size, c, 6f, 0f, 26, true);
        if (a == null) return null;
        a.follow = target;
        a.spin = spin;
        Fx.Play("fx_spark", target.position, 2f, c, 20f);
        SpecialAbilities.SharedFx?.Play("pew", 0.2f, 2f);
        keep.Add(a.gameObject);
        return a.gameObject;
    }

    public void Begin(int weaponId)
    {
        End();
        weapon = weaponId;
        age = 0f;
        switch (weapon)
        {
            case SpecialAbilities.DualId:
                rune = Keep(Fx.Play("fx_rune", Player.position, 14f, new Color(C.r, C.g, C.b, 0.5f), 1f, 0f, 2, true));
                if (rune != null) { rune.follow = Player; rune.spin = 160f; }
                for (int i = 0; i < 8; i++) orbs.Add(Keep(Fx.Play("fx_muzzle", Player.position, 1.6f, C, 10f, 0f, 20, true)));
                break;
            case SpecialAbilities.FlameId:
                rune = Keep(Fx.Play("fx_rune", owner.MouseWorldPos, 6f, new Color(1f, 0.5f, 0.15f, 0.7f), 1f, 0f, 2, true));
                if (rune != null) rune.spin = 140f;
                ghost = Keep(Fx.Play("fx_tornado", owner.MouseWorldPos, 5f, new Color(1f, 1f, 1f, 0.35f), 10f, 0f, 14, true));
                break;
            case SpecialAbilities.SeekerId:
                for (int i = 0; i < 5; i++) orbs.Add(Keep(Fx.Play("fx_orb", Player.position, 1.3f, C, 12f, 0f, 20, true)));
                break;
            case SpecialAbilities.ChainId:
                cloud = Keep(Fx.Play("fx_cloud", Player.position + Vector3.up * 6f, 4f, new Color(0.3f, 0.35f, 0.5f, 0.85f), 3f, 0f, 25, true));
                break;
            case SpecialAbilities.SniperId:
                reticle = Keep(Fx.Play("fx_reticle", owner.MouseWorldPos, 3.5f, C, 4f, 0f, 27, true));
                if (reticle != null) reticle.spin = 60f;
                break;
        }
    }

    FxAnim Keep(FxAnim a)
    {
        if (a != null) keep.Add(a.gameObject);
        return a;
    }

    LineRenderer Line(int i, Color c, float width)
    {
        while (lines.Count <= i)
        {
            LineRenderer lr = Hostile.NewLine("AimLine", c, width, 24);
            keep.Add(lr.gameObject);
            lines.Add(lr);
        }
        LineRenderer l = lines[i];
        l.enabled = true;
        l.startColor = l.endColor = c;
        l.startWidth = l.endWidth = width;
        return l;
    }

    // charge: 0 → 1 (우클릭을 누른 시간)
    public void Update(List<EnermyController> targets, float charge)
    {
        age += Time.unscaledDeltaTime;
        foreach (GameObject g in frame) if (g != null) Object.Destroy(g);
        frame.Clear();
        foreach (LineRenderer l in lines) l.enabled = false;
        float pulse = 0.6f + 0.4f * Mathf.Sin(age * 10f);
        Vector3 p = Player.position;
        Vector3 mouse = owner.MouseWorldPos;
        Vector2 dir = owner.AimDirection;

        switch (weapon)
        {
            case SpecialAbilities.ShotgunId:
            {
                // 거대한 불꽃 부채꼴 + 안에 든 적 조준경
                const float range = 11f, half = 35f;
                LineRenderer cone = Line(0, new Color(1f, 0.55f, 0.2f, 0.6f + 0.3f * pulse), 0.25f);
                const int seg = 20;
                cone.positionCount = seg + 3;
                cone.SetPosition(0, p);
                for (int i = 0; i <= seg; i++)
                {
                    Vector2 d = Quaternion.Euler(0, 0, -half + 2f * half * i / seg) * dir;
                    cone.SetPosition(i + 1, p + (Vector3)(d * range));
                }
                cone.SetPosition(seg + 2, p);
                if (Random.value < 0.6f && SpecialAbilities.GlowSprite != null)
                {
                    Vector2 d = Quaternion.Euler(0, 0, Random.Range(-half, half)) * dir;
                    FlameParticle.Spawn(SpecialAbilities.GlowSprite, p + (Vector3)(d * Random.Range(2f, range)), d * 2f, 0.4f, 0.05f, 0.35f, false);
                }
                foreach (Collider2D col in Physics2D.OverlapCircleAll(p, range))
                {
                    if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                    if (Vector2.Angle(dir, col.transform.position - p) > half) continue;
                    Frame(Fx.Play("fx_reticle", col.transform.position, 1.8f, new Color(1f, 0.6f, 0.2f), 1f, age * 120f, 26, true));
                }
                break;
            }
            case SpecialAbilities.SniperId:
            {
                // 굵어지는 레이저 조준선, 선 위의 적마다 조준경, 마우스엔 큰 조준경
                Vector3 m = owner.PlayerMuzzle;
                Vector3 end = m + (Vector3)(dir * 45f);
                Frame(Fx.Beam(m, end, 0.12f + 0.5f * charge, new Color(C.r, C.g, C.b, 0.5f + 0.3f * pulse), 0.2f, 15));
                if (reticle != null)
                {
                    reticle.transform.position = mouse;
                    reticle.transform.localScale = Vector3.one * (3.5f - 1.2f * charge) / reticle.sr.sprite.bounds.size.y;
                }
                foreach (Collider2D col in Physics2D.OverlapCircleAll((m + end) * 0.5f, 23f))
                {
                    if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                    if (Hostile.DistanceToSegment(col.transform.position, m, end) > 1.5f) continue;
                    Frame(Fx.Play("fx_reticle", col.transform.position, 2.2f, C, 1f, age * 200f, 26, true));
                }
                break;
            }
            case SpecialAbilities.DualId:
            {
                // 금빛 고리 + 공전하는 총구 섬광
                for (int i = 0; i < orbs.Count; i++)
                {
                    if (orbs[i] == null) continue;
                    float a = (age * 240f + i * 45f) * Mathf.Deg2Rad;
                    orbs[i].transform.position = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 6.5f;
                    orbs[i].transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                }
                break;
            }
            case SpecialAbilities.FlameId:
            {
                // 회오리가 생길 자리 + 빨려 들어갈 범위
                if (rune != null) rune.transform.position = mouse;
                if (ghost != null) ghost.transform.position = mouse + Vector3.up * 1.8f;
                LineRenderer pull = Line(0, new Color(1f, 0.5f, 0.15f, 0.35f + 0.3f * pulse), 0.12f);
                pull.loop = true;
                Hostile.SetArc(pull, mouse, 4.8f, 0f, 354f);
                foreach (Collider2D col in Physics2D.OverlapCircleAll(mouse, 4.8f))
                    if (col.CompareTag("enermy") && Random.value < 0.3f && SpecialAbilities.GlowSprite != null)
                        FlameParticle.Spawn(SpecialAbilities.GlowSprite, col.transform.position, ((Vector2)(mouse - col.transform.position)).normalized * 3f, 0.4f, 0.04f, 0.25f, false);
                break;
            }
            case SpecialAbilities.SeekerId:
            {
                // 영혼 구슬이 주위에 모여 돎 (충전할수록 커짐)
                for (int i = 0; i < orbs.Count; i++)
                {
                    if (orbs[i] == null) continue;
                    float a = (age * 200f + i * 72f) * Mathf.Deg2Rad;
                    orbs[i].transform.position = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (2.2f + 0.3f * Mathf.Sin(age * 6f + i));
                    orbs[i].transform.localScale = Vector3.one * (1.3f + 0.7f * charge) / orbs[i].sr.sprite.bounds.size.y;
                }
                break;
            }
            case SpecialAbilities.ChainId:
            {
                // 먹구름이 커지고, 표식을 단 적끼리 번개가 이어짐
                if (cloud != null)
                {
                    cloud.transform.position = p + Vector3.up * 6f;
                    cloud.transform.localScale = Vector3.one * (4f + 8f * charge) / cloud.sr.sprite.bounds.size.y;
                }
                int n = 0;
                for (int i = 1; i < targets.Count; i++)
                {
                    if (targets[i - 1] == null || targets[i] == null) continue;
                    if (Random.value < 0.35f) Frame(Fx.Bolt(targets[i - 1].transform.position, targets[i].transform.position, 0.7f, C, 0.08f));
                    n++;
                }
                if (targets.Count > 0 && targets[0] != null && Random.value < 0.3f)
                    Frame(Fx.Bolt(p + Vector3.up * 6f, targets[0].transform.position, 0.9f, C, 0.08f));
                break;
            }
            case SpecialAbilities.ScytheId:
            {
                // 베어 나갈 순서대로 이어지는 경로
                LineRenderer path = Line(0, new Color(0.8f, 0.55f, 1f, 0.55f + 0.3f * pulse), 0.14f);
                List<Vector3> pts = new List<Vector3> { p };
                foreach (EnermyController t in targets) if (t != null && !t.IsDead) pts.Add(t.transform.position);
                path.positionCount = pts.Count;
                for (int i = 0; i < pts.Count; i++) path.SetPosition(i, pts[i]);
                break;
            }
            case SpecialAbilities.GrenadeId:
            {
                // 떨어질 운석 자리 격자
                Vector2 side = new Vector2(-dir.y, dir.x);
                int rows = owner.GrenadeRows;
                int li = 0;
                for (int row = 1; row <= rows; row++)
                    for (int c = -1; c <= 1; c++)
                    {
                        Vector3 at = p + (Vector3)(dir * row * 2.6f + side * c * 2.6f);
                        LineRenderer ring = Line(li++, new Color(1f, 0.45f, 0.1f, 0.35f + 0.35f * Mathf.Sin(age * 10f - row * 0.8f)), 0.1f);
                        ring.loop = true;
                        Hostile.SetArc(ring, at, 1.8f, 0f, 354f);
                    }
                break;
            }
        }
    }

    void Frame(FxAnim a)
    {
        if (a != null) frame.Add(a.gameObject);
    }

    public void End()
    {
        foreach (GameObject g in keep) if (g != null) Object.Destroy(g);
        foreach (GameObject g in frame) if (g != null) Object.Destroy(g);
        keep.Clear();
        frame.Clear();
        lines.Clear();
        orbs.Clear();
        rune = ghost = cloud = reticle = null;
    }
}

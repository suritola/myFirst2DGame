using System.Collections.Generic;
using UnityEngine;

// 무기 진화 개편 (1.9.4~): 진화해도 무기를 바꾸지 않음
//   고른 무기의 느낌이 ① 기본 공격에 붙는 능력 ② 우클릭 필살기 두 가지로 들어옴
//   거너: 권총 그대로 + 화염 총구(총알이 불태움) · 영혼 탄두 · 쌍발 총구 …, 필살기는 원래 조준 사격 뒤에 그 무기의 궁극기가 절반 세기로 덧붙음 (화염 회오리 · 관통 레일건 …, 2.2.0~) · 쌍권총만 총알 폭풍으로 바뀜
//   2차 진화는 1차 능력 위에 하나 더 (같은 계열을 고르면 1차 능력이 극대화), 필살기는 2차 무기 것으로
//   다른 캐릭터: 평타 모양은 그대로 + 형태 능력 (CharacterKit.Forms), 필살기는 그 무기의 궁극기 (SpecialAbilities.KitUlts)
public partial class SpecialAbilities
{
    int gunEvo1 = -1, gunEvo2 = -1;
    public int GunEvo1 => gunEvo1;

    // 우클릭 필살기를 정하는 무기 (-1 = 캐릭터 기본 필살기)
    public int UltId => WeaponActive ? CurrentWeapon : Gunner ? (gunEvo2 >= 0 ? gunEvo2 : gunEvo1) : (Kit != null ? Kit.form : -1);
    public bool UltActive => UltId >= 0;

    bool HasAug(int id) => gunEvo1 == id || gunEvo2 == id;
    bool AugMaxed(int id) => gunEvo1 == id && gunEvo2 == id;      // 2차에서 같은 계열 = 극대화
    // 영혼 트리 숙련 특성 (단계마다 +25%)
    float AugPower(int id) => 1f + 0.25f * Trait(id);

    // ---------------- 이름 · 설명 (한국어 원문, 쓸 때 번역)
    public static string GunAugName(int id, bool maxed) => id switch
    {
        FlameId => maxed ? "지옥불 총구" : "화염 총구",
        SniperId => maxed ? "레일 탄두" : "영혼 탄두",
        DualId => maxed ? "심판의 삼연발" : "쌍발 총구",
        GrenadeId => "점착탄",
        ShotgunId => "산탄 총구",
        SeekerId => "영혼 사냥",
        ChainId => "낙뢰탄",
        ScytheId => "사신의 탄환",
        _ => "",
    };

    public static string GunAugDesc(int id, bool maxed) => id switch
    {
        // 레벨업 카드(소각탄 · 폭발 탄두 · 유도 탄두 · 전기탄)와 영혼 트리(철갑탄 · 고속탄 · 급소 사격)와 겹치지 않게
        FlameId => maxed ? "불길이 더 길고 넓어지며 (사거리 6.5칸, 피해 65%) 맞은 적이 불탑니다." : "쏠 때마다 총구에서 짧은 불길이 뿜어져 앞쪽 적을 태웁니다. (사거리 5칸, 피해 45%)",
        SniperId => maxed ? "충전이 더 빨리 되고, 충전탄 피해 450%." : "사격을 잠깐 멈췄다 쏘면 첫 발이 충전탄이 되어 모든 적을 꿰뚫고 피해 300%.",
        DualId => maxed ? "쏠 때마다 총알 두 발이 더 나갑니다. (피해 60%)" : "쏠 때마다 총알 한 발이 더 나갑니다. (피해 60%)",
        GrenadeId => "총알에 맞은 적에게 폭탄이 붙어 1.5초 뒤 터집니다. (범위 2.5칸, 피해 110%)",
        ShotgunId => "쏠 때마다 짧게 날아가는 산탄 네 발이 함께 퍼집니다. (발당 피해 55%)",
        SeekerId => "적을 처치하면 영혼탄 세 발이 튀어나와 가까운 적을 쫓아갑니다. (피해 60%)",
        ChainId => "총알이 여섯 번 맞힐 때마다 맞은 자리에 낙뢰가 떨어집니다. (범위 3칸, 피해 150%)",
        ScytheId => "적을 처치하면 35% 확률로 영혼 낫이 휘돌아 주변을 벱니다. (공격력 180%)",
        _ => "",
    };

    // 필살기 한 줄 설명 (진화 카드)
    public static string VolleyDesc(int id) => id switch
    {
        ShotgunId => "마우스 쪽으로 거대한 부채꼴 폭발 3연발",
        SniperId => "화면을 가로지르는 굵은 광선 (광선 위의 적이 많을수록 강함)",
        DualId => "1.6초 동안 주위 18칸까지 사방으로 총알을 난사",
        FlameId => "마우스 위치에 적을 빨아들이며 태우는 불기둥",
        SeekerId => "영혼 구슬 다섯이 조준한 적에게 번갈아 달려듦",
        ChainId => "조준한 적들에게 하늘에서 번개가 연달아 떨어짐",
        ScytheId => "무적 상태로 조준한 적들 사이를 순간이동하며 벰",
        GrenadeId => "마우스 쪽으로 줄지어 운석이 떨어짐",
        _ => "",
    };

    // 영혼 트리 숙련 가지의 특성 칸 (예전 무기 특성 대신 능력을 강화)
    static string AugTraitName(int id) => Loc.T(GunAugName(id, false)) + " " + Loc.T("강화");
    static string AugTraitStep() => Loc.T("능력 효과 +25%");

    // ---------------- 총알에 붙는 능력 (ApplyGunCards 에서, 권총 총알마다). main = 쏠 때 나가는 첫 발 (쌍발 · 산탄 덤 총알이 아님)
    float lastMainShot = -99f;
    int thunderHits;
    readonly HashSet<int> stuckBombs = new HashSet<int>();

    void ApplyEvoAugments(Bullet b, bool light, bool main)
    {
        if (gunEvo1 < 0 || light || b == null) return;
        // 영혼 탄두: 잠깐 쉬었다 쏘면 충전탄 (모두 관통 · 큰 피해)
        if (HasAug(SniperId) && main)
        {
            bool rail = AugMaxed(SniperId);
            // 평소 연사 간격의 두 배(극대화 1.4배) 넘게 쉬었다 쏜 첫 발 (공격 속도가 느려도 매 발이 충전되지 않게)
            float interval = player.ShootSpeed / Mathf.Max(0.1f, player.fireRateMultiplier);
            if (Time.time - lastMainShot >= Mathf.Max(rail ? 0.4f : 0.6f, interval * (rail ? 1.4f : 2f)))
            {
                b.damage *= (rail ? 4.5f : 3f) * AugPower(SniperId);
                b.pene = 9999;
                b.hitOnce = new HashSet<int>();
                b.speed *= 1.6f;
                b.transform.localScale *= 1.5f;
                if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.5f, 0.95f, 1f);
                if (fx != null) fx.Play("railgun", 0.35f, 1.6f);
            }
        }
        if (main) lastMainShot = Time.time;

        bool sticky = HasAug(GrenadeId), thunder = HasAug(ChainId);
        if (!sticky && !thunder) return;
        b.onHitEnemy += (bullet, col) =>
        {
            if (col == null) return;
            if (sticky && stuckBombs.Add(col.GetInstanceID())) StartCoroutine(StickyBomb(col.transform, col.GetInstanceID()));
            if (thunder && ++thunderHits >= 6)
            {
                thunderHits = 0;
                Vector3 at = col.transform.position;
                Fx.Bolt(at + new Vector3(Random.Range(-1.5f, 1.5f), 14f), at, 1.4f, new Color(0.6f, 0.9f, 1f), 0.2f);
                Fx.Spawn("fx_shock", at, 6f, new Color(0.6f, 0.9f, 1f), 22f);
                CharacterKit.DamageCircle(at, 3f, Damage * 1.5f * AugPower(ChainId), 0.5f);
                if (fx != null) fx.Play("thunder", 0.5f, Random.Range(0.95f, 1.15f));
            }
        };
    }

    // 점착탄: 맞은 적에 붙은 폭탄이 1.5초 뒤 터짐 (적마다 하나씩)
    System.Collections.IEnumerator StickyBomb(Transform target, int key)
    {
        Fx.Spawn("fx_spark", target.position, 1f, new Color(1f, 0.55f, 0.2f), 20f);
        Vector3 at = target.position;
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            if (target != null) at = target.position;
            yield return null;
        }
        stuckBombs.Remove(key);
        Explode(at, 2.5f + 0.3f * Trait(GrenadeId), Damage * 1.1f * AugPower(GrenadeId), 1.2f, new Color(1f, 0.55f, 0.2f, 0.85f));
    }
    // 치명타 (금색 · 크게) — 영혼 탄두 · 영혼 트리 치명타가 같이 씀
    void MakeCrit(Bullet b)
    {
        if (b.isCrit) return;
        b.isCrit = true;
        b.damage *= TreeCritDamage;
        b.transform.localScale *= 1.25f;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.85f, 0.3f);
    }

    // ---------------- 쏠 때 더 나가는 총알 (PlayerController.Shoot)
    public void EvoExtraShots(Vector3 start, Vector2 dir, float dmg)
    {
        if (!Gunner || gunEvo1 < 0) return;
        // 화염 총구: 총구 앞 짧은 불길 (부채꼴)
        if (HasAug(FlameId))
        {
            bool hell = AugMaxed(FlameId);
            float range = hell ? 6.5f : 5f, half = hell ? 30f : 24f;     // 1.0.5: 쌍발 총구만 고르던 것 → 다른 진화 강화
            float hit = Damage * (hell ? 0.65f : 0.45f) * AugPower(FlameId);
            foreach (Collider2D c in Specials.Overlap(start, range, gunHits))
            {
                if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                Vector2 to = c.transform.position - start;
                if (Vector2.Angle(dir, to) > half) continue;
                Specials.Damage(c.gameObject, hit, to.normalized, 0.3f);
                if (hell) Burn.Apply(c.gameObject, Damage * 0.3f, 2f);
            }
            for (int i = 1; i <= 3; i++)
                Fx.Spawn("fx_explosion", start + (Vector3)(dir * range * i / 3.5f), 0.8f + i * (hell ? 0.6f : 0.4f), new Color(1f, 0.55f, 0.15f, 0.8f), 30f);
        }
        if (HasAug(DualId))
        {
            int extra = AugMaxed(DualId) ? 2 : 1;
            for (int i = 0; i < extra; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i == 0 ? 4f : -4f) * dir;
                Bullet b = player.CreateBullet(start + (Vector3)(new Vector2(-d.y, d.x) * (i == 0 ? 0.25f : -0.25f)), d, dmg * 0.6f * AugPower(DualId), player.pene, 0, false, 0.5f);
                ApplyGunCards(b, false, false);
            }
        }
        if (HasAug(ShotgunId))
        {
            float[] angles = { -24f, -12f, 12f, 24f };
            foreach (float a in angles)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, a + Random.Range(-3f, 3f)) * dir;
                Bullet b = player.CreateBullet(start, d, dmg * 0.55f * AugPower(ShotgunId), 1, 0, false, 0.4f);
                if (b == null) continue;
                b.lifetime = 0.3f;
                b.transform.localScale *= 0.8f;
                if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.7f, 0.35f);
                ApplyGunCards(b, false, false);
            }
        }
    }

    // ---------------- 무한 모드: 진화를 다 마친 뒤 보스를 잡을 때마다 쌓이는 전리품 (돌아가며)
    public string BossTrophy(int n)
    {
        if (player == null) return "";
        string got;
        switch (n % 3)
        {
            case 1: AddAttack(0.12f); got = Loc.T("모든 공격 피해 +12%"); break;
            case 2:
                GrowHp(player, 1.15f);
                player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + player.PlayerMaxHealth * 0.3f);
                got = Loc.T("최대 체력 +15% · 체력 30% 회복");
                break;
            default: AddRate(0.92f); got = Loc.T("모든 무기 발사 간격 -8%"); break;
        }
        if (fx != null) { fx.Play("levelup", 0.7f, 1f); fx.FloatText(player.transform.position, got, new Color(1f, 0.85f, 0.4f), 6f, 0f); }
        Flash(player.transform.position, 6f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.5f);
        return got;
    }

    // ---------------- 처치할 때: 사신의 탄환
    void EvoOnKill(Vector3 pos)
    {
        if (!Gunner || player == null) return;
        // 영혼 사냥: 처치한 자리에서 가까운 적을 쫓는 영혼탄 두 발
        if (HasAug(SeekerId))
            for (int i = 0; i < 3; i++)
            {
                Bullet b = player.CreateBullet(pos, Random.insideUnitCircle.normalized, Damage * 0.6f * AugPower(SeekerId), 1, 0, false, 0.3f);
                if (b == null) continue;
                b.gameObject.AddComponent<Homing>().turnSpeed = 420f;
                b.transform.localScale *= 0.8f;
                if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.75f, 0.5f, 1f);
            }
        if (!HasAug(ScytheId) || Random.value >= 0.35f) return;
        float r = 3f;
        CharacterKit.DamageCircle(pos, r, Damage * 1.8f * AugPower(ScytheId), 1.5f);
        Fx.Spawn("fx_slash", pos, r * 2.2f, new Color(0.8f, 0.55f, 1f), 30f, Random.Range(0f, 360f), 16);
        if (fx != null) fx.Play("slash", 0.5f, 1.3f);
    }
}

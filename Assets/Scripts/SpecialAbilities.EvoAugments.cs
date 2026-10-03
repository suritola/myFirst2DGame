using System.Collections.Generic;
using UnityEngine;

// 무기 진화 개편 (1.9.4~): 진화해도 무기를 바꾸지 않음
//   고른 무기의 느낌이 ① 기본 공격에 붙는 능력 ② 우클릭 필살기 두 가지로 들어옴
//   거너: 권총 그대로 + 화염 총구(총알이 불태움) · 영혼 탄두 · 쌍발 총구 …, 필살기는 그 무기의 필살기 (화염 회오리 · 관통 레일건 …)
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
        GrenadeId => "작열탄",
        ShotgunId => "산탄 총구",
        SeekerId => "추적 영혼탄",
        ChainId => "뇌전 탄환",
        ScytheId => "사신의 탄환",
        _ => "",
    };

    public static string GunAugDesc(int id, bool maxed) => id switch
    {
        FlameId => maxed ? "불길이 더 세지고 (초당 공격력 60%) 맞은 적 주변으로 번집니다." : "총알이 맞힌 적을 3초 동안 불태웁니다. (초당 공격력 35%)",
        SniperId => maxed ? "총알이 모든 적을 꿰뚫고 치명타 확률이 30%가 됩니다." : "총알이 적 둘을 더 꿰뚫고 50% 빠르게 날아가며, 15% 확률로 치명타가 됩니다.",
        DualId => maxed ? "쏠 때마다 총알 두 발이 더 나갑니다. (피해 70%)" : "쏠 때마다 총알 한 발이 더 나갑니다. (피해 70%)",
        GrenadeId => "총알이 처음 맞힌 자리에서 작게 폭발합니다. (범위 2칸, 피해 70%)",
        ShotgunId => "쏠 때마다 짧게 날아가는 산탄 네 발이 함께 퍼집니다. (발당 피해 45%)",
        SeekerId => "총알이 가까운 적을 강하게 쫓아갑니다.",
        ChainId => "총알이 맞힌 적에게서 35% 확률로 번개가 적 둘에게 튑니다. (피해 50%)",
        ScytheId => "적을 처치하면 25% 확률로 영혼 낫이 휘돌아 주변을 벱니다. (공격력 150%)",
        _ => "",
    };

    // 필살기 한 줄 설명 (진화 카드)
    public static string VolleyDesc(int id) => id switch
    {
        ShotgunId => "마우스 쪽으로 거대한 부채꼴 폭발 3연발",
        SniperId => "화면을 가로지르는 굵은 광선 (광선 위의 적이 많을수록 강함)",
        DualId => "1.6초 동안 사방으로 총알을 난사",
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

    // ---------------- 총알에 붙는 능력 (ApplyGunCards 에서, 권총 총알마다)
    void ApplyEvoAugments(Bullet b, bool light)
    {
        if (gunEvo1 < 0 || light || b == null) return;
        // 영혼 탄두: 관통 · 탄속 · 치명타
        if (HasAug(SniperId))
        {
            float p = AugPower(SniperId);
            b.speed *= 1.5f;
            if (AugMaxed(SniperId)) { b.pene = 9999; b.hitOnce = new HashSet<int>(); }
            else b.pene += 2;
            if (Random.value < (AugMaxed(SniperId) ? 0.3f : 0.15f) * p) MakeCrit(b);
        }
        // 추적 영혼탄
        if (HasAug(SeekerId))
        {
            Homing h = b.GetComponent<Homing>();
            if (h == null) h = b.gameObject.AddComponent<Homing>();
            h.turnSpeed = Mathf.Max(h.turnSpeed, 300f * AugPower(SeekerId));
        }

        bool flame = HasAug(FlameId), blast = HasAug(GrenadeId), chain = HasAug(ChainId);
        if (!flame && !blast && !chain) return;
        bool first = true;
        b.onHitEnemy += (bullet, col) =>
        {
            if (col == null) return;
            Vector3 at = col.transform.position;
            if (flame)
            {
                bool hell = AugMaxed(FlameId);
                float dps = Damage * (hell ? 0.6f : 0.35f) * AugPower(FlameId);
                Burn.Apply(col.gameObject, dps, 3f);
                if (hell)
                    foreach (Collider2D o in Specials.Overlap(at, 2.5f, gunHits))
                        if (o != col && (o.CompareTag("enermy") || o.CompareTag("boss"))) Burn.Apply(o.gameObject, dps * 0.5f, 3f);
            }
            if (blast && first)
                Explode(at, 2f + 0.3f * Trait(GrenadeId), Damage * 0.7f * AugPower(GrenadeId), 1.2f, new Color(1f, 0.55f, 0.2f, 0.85f));
            if (chain && Random.value < 0.35f * AugPower(ChainId))
                ChainLightning(at, col, Damage * 0.5f, 2);
            first = false;
        };
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
        if (HasAug(DualId))
        {
            int extra = AugMaxed(DualId) ? 2 : 1;
            for (int i = 0; i < extra; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i == 0 ? 4f : -4f) * dir;
                Bullet b = player.CreateBullet(start + (Vector3)(new Vector2(-d.y, d.x) * (i == 0 ? 0.25f : -0.25f)), d, dmg * 0.7f * AugPower(DualId), player.pene, 0, false, 0.5f);
                ApplyGunCards(b, false, false);
            }
        }
        if (HasAug(ShotgunId))
        {
            float[] angles = { -24f, -12f, 12f, 24f };
            foreach (float a in angles)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, a + Random.Range(-3f, 3f)) * dir;
                Bullet b = player.CreateBullet(start, d, dmg * 0.45f * AugPower(ShotgunId), 1, 0, false, 0.4f);
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
        if (!Gunner || !HasAug(ScytheId) || player == null || Random.value >= 0.25f) return;
        float r = 3f;
        CharacterKit.DamageCircle(pos, r, Damage * 1.5f * AugPower(ScytheId), 1.5f);
        Fx.Spawn("fx_slash", pos, r * 2.2f, new Color(0.8f, 0.55f, 1f), 30f, Random.Range(0f, 360f), 16);
        if (fx != null) fx.Play("slash", 0.5f, 1.3f);
    }
}

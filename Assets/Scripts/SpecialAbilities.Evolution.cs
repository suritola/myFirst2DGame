using System.Collections.Generic;
using UnityEngine;

// 거너의 새 성장 방식 (1.7.5~)
//  · 무기 진화: 스테이지 보스를 쓰러뜨릴 때마다 지금 무기에서 3갈래 중 하나로 진화 (되돌릴 수 없음)
//      권총 → 1차 (화염 방사기 / 영혼 저격총 / 저주받은 쌍권총) → 2차 (계열마다 강화판 3갈래)
//  · 영혼 트리: 가운데 = 지금 무기, 네 갈래(무기 · 필살기 · 생존 · 영혼)로 뻗은 칸을 영혼 조각으로 배움
//      앞 칸을 배워야 이어진 다음 칸이 열림. 무기 가지는 무기(진화 단계)마다 달라짐
// 다른 캐릭터는 아직 예전 특수 능력 방식을 씀 (UsesEvolution == false)
public partial class SpecialAbilities
{
    public static bool UsesEvolution => CharacterData.IsGunner;

    // ================================================================= 무기 진화
    public int EvolutionTier { get; private set; }         // 0 = 권총, 1 = 1차, 2 = 2차

    public static readonly int[] Tier1Options = { FlameId, SniperId, DualId };

    public static int[] Tier2Options(int tier1) => tier1 switch
    {
        FlameId => new[] { FlameId, GrenadeId, ShotgunId },
        SniperId => new[] { SniperId, SeekerId, ChainId },
        _ => new[] { DualId, ScytheId, ChainId },
    };

    // 다음 진화 선택지 (더 없으면 null)
    public int[] NextEvolutionOptions()
    {
        if (!UsesEvolution) return null;
        if (EvolutionTier == 0) return Tier1Options;
        if (EvolutionTier == 1 && WeaponActive) return Tier2Options(CurrentWeapon);
        return null;
    }

    // 진화 단계에 맞는 무기 이름 (2차에서 같은 무기를 고르면 새 이름)
    public string EvolutionName(int id, int tier)
    {
        if (tier >= 2 && id == FlameId) return Loc.T("지옥불 방사기");
        if (tier >= 2 && id == SniperId) return Loc.T("영혼 레일건");
        if (tier >= 2 && id == DualId) return Loc.T("심판의 쌍권총");
        return Loc.T(abilities[id].name);
    }

    public string MainWeaponName => WeaponActive ? EvolutionName(CurrentWeapon, EvolutionTier) : Loc.T("기본 권총");

    // 진화 카드 설명: 1차는 무기 설명, 2차는 진화한 무기 설명 + 진화 효과
    public string EvolutionDesc(int id, int tier)
    {
        string body = Loc.T(abilities[id].description);
        return tier >= 2 ? body + "\n<color=#9fd8ff>" + Loc.T("진화") + "</color>  " + EvolveText(id) : body;
    }

    // 진화: 지금 무기를 새 무기로 바꾸고 강화를 이어받음
    public void EvolveWeapon(int id)
    {
        int prev = WeaponActive ? CurrentWeapon : -1;
        int tier = EvolutionTier + 1;

        // 권총 필살기(일제 사격)의 특성 강화도 첫 진화 무기로 이어받음
        if (prev < 0 && ultLevels.TryGetValue(PistolUlt, out int[] pu)) ultLevels[id] = (int[])pu.Clone();
        if (prev >= 0 && prev != id)
        {
            // 무기 강화 · 필살기 강화를 새 무기로 이어받음
            if (weaponLevels.TryGetValue(prev, out int[] wl)) weaponLevels[id] = (int[])wl.Clone();
            if (ultLevels.TryGetValue(prev, out int[] ul)) ultLevels[id] = (int[])ul.Clone();
            equipped.Remove(prev);
            weapons.Remove(prev);
            evolved.Remove(prev);
            if (flameMuzzle != null) Destroy(flameMuzzle);
            CancelSniperCharge();
            weaponIndex = -1;
        }
        if (!equipped.Contains(id))
        {
            equipped.Add(id);
            weapons.Add(id);
        }
        weaponIndex = weapons.IndexOf(id);
        EvolutionTier = tier;
        if (tier >= 2 && !evolved.Contains(id)) Evolve(id);
        if (UsesAmmo(id)) { WeaponAmmo a = Ammo(id); a.ammo = MagSize(id); a.reloadEnd = -1f; }
        nextFire = 0f;
        RebuildHudRows();
    }

    // ================================================================= 영혼 트리
    public class SoulNode
    {
        public string key, parent;          // parent == null → 가운데 무기에서 바로 이어짐
        public int branch;                  // 0 무기 · 1 필살기 · 2 생존 · 3 영혼
        public string name, desc;           // 번역된 글
        public int cost;
        public Sprite icon;
        public System.Action apply;
        public bool hidden;                 // 지금 무기에는 해당 없음
    }

    public static readonly string[] BranchNames = { "무기", "필살기", "생존", "영혼" };
    readonly HashSet<string> ownedNodes = new HashSet<string>();
    public bool OwnsNode(string key) => ownedNodes.Contains(key);

    // 트리가 올리는 값
    public float TreeUltPower { get; private set; }         // 필살기 위력 +
    public float TreeGaugeMul { get; private set; } = 1f;   // 필살기 게이지 차는 속도
    public float TreeShardMul { get; private set; } = 1f;   // 영혼 조각 획득량

    // 지금 트리 (무기 가지는 진화 단계 · 무기마다 다름)
    public List<SoulNode> BuildSoulTree()
    {
        List<SoulNode> t = new List<SoulNode>();
        Sprite atk = StatIcon(0), def = StatIcon(1), spd = StatIcon(2), rel = StatIcon(3), mov = StatIcon(4);

        // ---------------- 무기 가지
        if (!WeaponActive)
        {
            // 진화 전: 권총
            PlayerController p = player;
            t.Add(Node("p.dmg1", null, 0, "권총 화력", "권총 공격력 +0.25", 8, atk, () => p.damage += 0.25f));
            t.Add(Node("p.dmg2", "p.dmg1", 0, "권총 화력 II", "권총 공격력 +0.25", 18, atk, () => p.damage += 0.25f));
            t.Add(Node("p.rate1", "p.dmg1", 0, "빠른 손", "권총 연사 +10%", 12, spd, () => p.ShootSpeed *= 0.9f));
            t.Add(Node("p.mag1", "p.rate1", 0, "큰 탄창", "권총 탄창 +2발", 12, rel, () => { p.MaxBullet += 2; p.NowBullet += 2; }));
            t.Add(Node("p.reload1", "p.mag1", 0, "재빠른 장전", "권총 장전 시간 -15%", 15, rel, () => p.reloadTime *= 0.85f));
        }
        else
        {
            int w = CurrentWeapon;
            Sprite wi = abilities[w].icon;
            string trait = TraitName(w), step = TraitStep(w);
            t.Add(Node("w.dmg1", null, 0, "무기 화력", "무기 피해 +15%", 12, atk, () => UpgradeWeapon(w, StatDamage)));
            t.Add(Node("w.dmg2", "w.dmg1", 0, "무기 화력 II", "무기 피해 +15%", 25, atk, () => UpgradeWeapon(w, StatDamage)));
            t.Add(Node("w.dmg3", "w.dmg2", 0, "무기 화력 III", "무기 피해 +15%", 45, atk, () => UpgradeWeapon(w, StatDamage)));
            t.Add(Node("w.rate1", "w.dmg1", 0, "연사 강화", "무기 연사 +10%", 18, spd, () => UpgradeWeapon(w, StatRate)));
            t.Add(Node("w.rate2", "w.rate1", 0, "연사 강화 II", "무기 연사 +10%", 35, spd, () => UpgradeWeapon(w, StatRate)));
            SoulNode mag = Node("w.mag1", "w.rate1", 0, "큰 탄창", "무기 탄창 +25%", 15, rel, () => UpgradeWeapon(w, StatMag));
            mag.hidden = !UsesAmmo(w);
            t.Add(mag);
            // 특성: 무기마다 다름 (저격총 충전 속도 · 화염 냉각 · 산탄 사거리 …)
            t.Add(RawNode("w.trait1", "w.dmg1", 0, trait, step, 20, wi, () => UpgradeWeapon(w, StatTrait)));
            t.Add(RawNode("w.trait2", "w.trait1", 0, trait + " II", step, 40, wi, () => UpgradeWeapon(w, StatTrait)));
            t.Add(RawNode("w.trait3", "w.trait2", 0, trait + " III", step, 70, wi, () => UpgradeWeapon(w, StatTrait)));
        }

        // ---------------- 필살기 가지
        Sprite ult = WeaponActive ? abilities[CurrentWeapon].icon : atk;
        t.Add(Node("u.power1", null, 1, "필살 위력", "필살기 피해 +20%", 12, ult, () => TreeUltPower += 0.2f));
        t.Add(Node("u.power2", "u.power1", 1, "필살 위력 II", "필살기 피해 +20%", 28, ult, () => TreeUltPower += 0.2f));
        t.Add(Node("u.power3", "u.power2", 1, "필살 위력 III", "필살기 피해 +20%", 50, ult, () => TreeUltPower += 0.2f));
        t.Add(Node("u.gauge1", "u.power1", 1, "빠른 충전", "필살기 게이지 차는 속도 +15%", 15, spd, () => TreeGaugeMul += 0.15f));
        t.Add(Node("u.gauge2", "u.gauge1", 1, "빠른 충전 II", "필살기 게이지 차는 속도 +15%", 35, spd, () => TreeGaugeMul += 0.15f));
        // 필살기 특성: 무기마다 다름 (권총 타겟 수 · 화염 회오리 지속 · 레일건 굵기 …), 진화해도 이어짐
        int uw = WeaponActive ? CurrentWeapon : PistolUlt;
        string ut = UltTraitName(uw), us = UltTraitStep(uw);
        string ud = Loc.T("필살기") + " " + ut + " " + us;
        t.Add(RawNode("u.trait1", "u.power1", 1, ut, ud, 20, ult, () => UpgradeUlt(uw, UltTraitStat)));
        t.Add(RawNode("u.trait2", "u.trait1", 1, ut + " II", ud, 40, ult, () => UpgradeUlt(uw, UltTraitStat)));
        t.Add(RawNode("u.trait3", "u.trait2", 1, ut + " III", ud, 65, ult, () => UpgradeUlt(uw, UltTraitStat)));

        // ---------------- 생존 가지
        PlayerController pl = player;
        t.Add(Node("s.hp1", null, 2, "튼튼한 몸", "최대 체력 +15%", 10, def, () => GrowHp(pl, 1.15f)));
        t.Add(Node("s.hp2", "s.hp1", 2, "튼튼한 몸 II", "최대 체력 +15%", 25, def, () => GrowHp(pl, 1.15f)));
        t.Add(Node("s.hp3", "s.hp2", 2, "튼튼한 몸 III", "최대 체력 +15%", 45, def, () => GrowHp(pl, 1.15f)));
        t.Add(Node("s.regen1", "s.hp1", 2, "회복력", "초당 체력 +0.5 회복", 15, mov, () => pl.regenPerSecond += 0.5f));
        t.Add(Node("s.regen2", "s.regen1", 2, "회복력 II", "초당 체력 +0.5 회복", 35, mov, () => pl.regenPerSecond += 0.5f));
        t.Add(Node("s.def1", "s.hp1", 2, "단단한 피부", "받는 피해 -8%", 20, def, () => pl.def += 0.08f));
        t.Add(Node("s.def2", "s.def1", 2, "단단한 피부 II", "받는 피해 -8%", 45, def, () => pl.def += 0.08f));
        t.Add(Node("s.guard", "s.def1", 2, "재정비", "맞은 뒤 무적 시간 +0.15초", 30, def, () => pl.hurtInvincibleTime += 0.15f));

        // ---------------- 영혼 가지 (예전 거너 패시브가 여기로)
        t.Add(Node("o.root", null, 3, "영혼 각성", "영혼 조각 획득량 +20%", 10, abilities[OrbsId].icon, () => TreeShardMul += 0.2f));
        AddPassive(t, OrbsId, 30, 60);
        AddPassive(t, ThornsId, 30, 60);
        AddPassive(t, CurseId, 25, 50);
        AddPassive(t, UndyingId, 50, 90);
        return t;
    }

    void AddPassive(List<SoulNode> t, int id, int cost, int evoCost)
    {
        SpecialDef d = abilities[id];
        t.Add(new SoulNode { key = "o." + id, parent = "o.root", branch = 3, name = Loc.T(d.name), desc = BodyText(id, d.description), cost = cost, icon = d.icon,
                             apply = () => Equip(new[] { id }) });
        t.Add(new SoulNode { key = "o." + id + "+", parent = "o." + id, branch = 3, name = Loc.T(d.name) + Loc.T(" 진화"), desc = EvolveText(id), cost = evoCost, icon = d.icon,
                             apply = () => Evolve(id) });
    }

    static SoulNode Node(string key, string parent, int branch, string ko, string descKo, int cost, Sprite icon, System.Action apply)
        => new SoulNode { key = key, parent = parent, branch = branch, name = Loc.T(ko), desc = Loc.T(descKo), cost = cost, icon = icon, apply = apply };

    static SoulNode RawNode(string key, string parent, int branch, string name, string desc, int cost, Sprite icon, System.Action apply)
        => new SoulNode { key = key, parent = parent, branch = branch, name = name, desc = desc, cost = cost, icon = icon, apply = apply };

    static void GrowHp(PlayerController p, float mul)
    {
        float add = p.PlayerMaxHealth * (mul - 1f);
        p.PlayerMaxHealth += add;
        p.PlayerHealth += add;
    }

    // 능력치 아이콘 (StatsHUD: 공격력 · 방어력 · 공격 속도 · 재장전 · 이동 속도)
    static Sprite StatIcon(int i)
    {
        StatsHUD s = FindFirstObjectByType<StatsHUD>(FindObjectsInactive.Include);
        return s != null && s.icons != null && i < s.icons.Length ? s.icons[i] : null;
    }

    public bool CanBuy(SoulNode n) => !ownedNodes.Contains(n.key) && !n.hidden && (n.parent == null || ownedNodes.Contains(n.parent)) && SoulShards.Amount >= n.cost;
    public bool IsOpenNode(SoulNode n) => !ownedNodes.Contains(n.key) && !n.hidden && (n.parent == null || ownedNodes.Contains(n.parent));

    public bool BuyNode(SoulNode n)
    {
        if (!CanBuy(n) || !SoulShards.Spend(n.cost)) return false;
        ownedNodes.Add(n.key);
        n.apply?.Invoke();
        if (fx != null) { fx.Play("chime", 0.6f, 1.3f); fx.Play("pulse", 0.4f, 1.6f); }
        return true;
    }

    // 1차 진화 때 권총 가지는 끝나고 무기 가지가 새로 열림 (권총에 쓴 조각의 효과는 그대로 남음)
    public void OnEvolvedTier1()
    {
        List<string> drop = new List<string>();
        foreach (string k in ownedNodes) if (k.StartsWith("w.")) drop.Add(k);
        foreach (string k in drop) ownedNodes.Remove(k);
    }

    // 살 수 있는 칸이 있는지 (버튼 반짝임)
    public bool AnyAffordable()
    {
        foreach (SoulNode n in BuildSoulTree()) if (CanBuy(n)) return true;
        return false;
    }
}

// 영혼 조각: 적을 쓰러뜨리면 모이는 재화 (골드와 별개). 강한 적일수록 많이 줌. 한 판마다 새로
public static class SoulShards
{
    public static int Amount { get; private set; }
    public static int Total { get; private set; }            // 이번 판에 모은 총량
    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name != "GameScene") return;
            Amount = 0;
            Total = 0;
            Changed?.Invoke();
        };
    }

    public static void Add(int n, Vector3 at, bool announce)
    {
        if (n <= 0) return;
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp != null) n = Mathf.Max(1, Mathf.RoundToInt(n * sp.TreeShardMul));
        Amount += n;
        Total += n;
        Changed?.Invoke();
        if (SpecialAbilities.UsesEvolution)
        {
            Fx.Spawn("fx_sparkle", at + Vector3.up * 0.4f, announce ? 1.6f : 0.7f, new Color(0.7f, 0.55f, 1f), 18f);
            if (announce && SpecialAbilities.SharedFx != null)
                SpecialAbilities.SharedFx.FloatText(at, Loc.T("영혼 조각 +") + n, new Color(0.75f, 0.6f, 1f), 5f, 0f);
        }
    }

    public static bool Spend(int n)
    {
        if (Amount < n) return false;
        Amount -= n;
        Changed?.Invoke();
        return true;
    }

    // 적 수준별 양: 일반 1~2 · 강한 적 3~7 · 중간 보스 약 20~30 · 스테이지 보스 약 50
    public static int ForEnemy(EnermyController e) => Mathf.Max(1, Mathf.RoundToInt(e.expReward / 30f));
    public static int ForBoss(int expReward) => 30 + Mathf.RoundToInt(expReward / 30f);
}

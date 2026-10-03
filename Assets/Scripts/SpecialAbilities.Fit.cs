using System.Collections.Generic;
using UnityEngine;

// 무기에 맞게 실시간으로 (1.8.4~): 지금 든 무기(형태)에 쓸모없는 레벨업 카드 · 트리 칸은 나오지 않음
//   레벨업 카드: 안 맞는 거너 카드는 뽑히지 않고, 이미 고른 카드가 안 맞게 되면 단계마다 무기 피해 +6% 로 바뀜
//   영혼 트리: 안 맞는 칸은 숨김 (이미 배운 칸은 남기고 "지금 무기에는 효과 없음")
public partial class SpecialAbilities
{
    int FitWeapon => WeaponActive ? CurrentWeapon : -1;

    // 거너 카드 (GunRicochet …)가 지금 무기에 맞는지
    public bool CardFits(int gunCardIndex)
    {
        if (!CharacterData.IsGunner) return true;
        int w = FitWeapon;
        switch (gunCardIndex)
        {
            case GunRicochet: return w != FlameId && w != ShotgunId && w != GrenadeId && w != ScytheId;
            case GunExplosive: return w != FlameId;
            case GunIncendiary: return w != FlameId;                    // 화염 방사기는 이미 불태움
            case GunHoming: return w != SniperId && w != SeekerId && w != ScytheId && w != ShotgunId && w != GrenadeId && w != FlameId;
            case GunShock: return w != ChainId;                         // 번개 사슬총은 이미 번개
            case GunReloadWave: return w < 0 || UsesAmmo(w);            // 탄창이 없는 무기는 장전이 없음
        }
        return true;
    }

    // 이미 골랐는데 지금 무기에 안 맞는 카드 단계 합 → 무기 피해 +6% 씩 (WDamage)
    public int InertCardLevels()
    {
        if (!CharacterData.IsGunner) return 0;
        int n = 0;
        for (int i = 0; i < gunCard.Length; i++) if (gunCard[i] > 0 && !CardFits(i)) n += gunCard[i];
        return n;
    }

    float InertBonus => 1f + 0.06f * InertCardLevels();

    // 날아가는 총알이 있는 무기인지 (산탄총 · 용암 유탄은 총알 없이 범위로 침)
    bool BulletWeapon => !WeaponActive || (CurrentWeapon != ShotgunId && CurrentWeapon != GrenadeId);

    // 트리 칸이 지금 무기(형태)에 쓸모없는지
    bool NodeIrrelevant(string key)
    {
        int w = FitWeapon;
        if (CharacterData.IsGunner)
        {
            if ((key.StartsWith("w.mag") || key.StartsWith("w.reload")) && w >= 0 && !UsesAmmo(w)) return true;
            if (key == "w.speed" && (w == ShotgunId || w == GrenadeId || w == FlameId || w == ScytheId)) return true;
            if (key.StartsWith("w.pene") && (w == SniperId || w == FlameId || w == ScytheId || w == ShotgunId || w == GrenadeId)) return true;
            if (key.StartsWith("u.slow") && IsInstantUlt) return true;
            return false;
        }
        // 1.9.4~: 진화해도 평타 모양이 그대로라 형태 때문에 쓸모없어지는 칸은 없음
        return false;
    }

    // BuildSoulTree 끝에서: 안 맞는 칸은 숨기고, 이미 배운 칸이면 설명에 한 줄
    void ApplyFit(List<SoulNode> t)
    {
        foreach (SoulNode n in t)
        {
            if (!NodeIrrelevant(n.key)) continue;
            if (ownedNodes.Contains(n.key)) n.desc += "\n<color=#8a8494>" + Loc.T("지금 무기에는 효과가 없습니다") + "</color>";
            else n.hidden = true;
        }
    }
}

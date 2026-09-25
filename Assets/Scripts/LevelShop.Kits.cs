using UnityEngine;

// 캐릭터 전용 레벨업 카드: 무기와 관련된 카드(관통 · 노려보는 눈빛 · 멀티 샷 · 밀어내기)만
// 캐릭터의 무기 · 우클릭에 맞는 카드로 바뀜. 나머지 카드는 모든 캐릭터가 같이 씀
public partial class LevelShop
{
    // 바뀌는 카드 번호
    const int PierceId = 0, GlareId = 3, MultiId = 6, KnockId = 7;

    class KitCard
    {
        public string name, desc;       // 카드 이름 · 설명 (한국어 키)
        public int icon;                // Resources/Icons/ability_<icon>
        public int max;                 // 최대 선택 횟수
    }

    static KitCard Card(string name, string desc, int icon, int max) => new KitCard { name = name, desc = desc, icon = icon, max = max };

    static KitCard KitCardOf(int id) => KitCardOf(CharacterData.Selected, id);

    // 도감용: 이 캐릭터의 전용 레벨업 카드 (이름 · 설명 · 아이콘 번호)
    public static System.Collections.Generic.List<(string name, string desc, int icon)> KitCardsFor(CharacterId who)
    {
        var list = new System.Collections.Generic.List<(string, string, int)>();
        foreach (int id in new[] { PierceId, MultiId, KnockId, GlareId })
        {
            KitCard c = KitCardOf(who, id);
            if (c != null) list.Add((c.name, c.desc, c.icon));
        }
        return list;
    }

    static KitCard KitCardOf(CharacterId who, int id)
    {
        switch (who)
        {
            case CharacterId.Swordsman:
                return id switch
                {
                    PierceId => Card("넓은 베기", "장검을 더 넓게 휘두릅니다.\n( 베는 각도 +30° )", 20, 3),
                    GlareId => Card("회전 베기 수련", "우클릭 회전 베기의 범위와 피해가 20% 커집니다.", 24, 3),
                    MultiId => Card("긴 칼날", "장검이 길어져 더 멀리 벱니다.\n( 사거리 +15% )", 22, 3),
                    KnockId => Card("날랜 손목", "장검을 더 빨리 휘두릅니다.\n( 공격 속도 +12% )", 21, 3),
                    _ => null,
                };
            case CharacterId.Rogue:
                return id switch
                {
                    PierceId => Card("관통 표창", "표창이 적을 하나 더 꿰뚫습니다.", 29, 5),
                    GlareId => Card("깊은 상처", "우클릭 출혈 돌진의 피해와 출혈이 25% 강해집니다.", 33, 3),
                    MultiId => Card("표창 한 움큼", "한 번에 던지는 표창이 1개 늘어나지만, 개당 피해는 줄어듭니다.", 28, 4),
                    KnockId => Card("큰 표창 주머니", "표창 탄창이 2발 늘어납니다.", 30, 3),
                    _ => null,
                };
            case CharacterId.Archer:
                return id switch
                {
                    PierceId => Card("관통 화살", "화살이 적을 하나 더 꿰뚫습니다.", 41, 5),
                    GlareId => Card("넓은 화살비", "우클릭 화살비의 범위와 화살 수가 20% 늘어납니다.", 38, 3),
                    MultiId => Card("연발 사격", "한 번에 쏘는 화살이 1발 늘어나지만, 한 발당 피해는 줄어듭니다.", 37, 4),
                    KnockId => Card("강한 시위", "화살 피해가 12% 늘어납니다.", 36, 3),
                    _ => null,
                };
            case CharacterId.Alchemist:
                return id switch
                {
                    PierceId => Card("넓은 폭발", "플라스크 폭발 범위가 15% 커집니다.", 50, 3),
                    GlareId => Card("대폭발 연구", "우클릭 대폭발 플라스크의 범위와 피해가 20% 커집니다.", 49, 3),
                    MultiId => Card("연속 투척", "한 번에 던지는 플라스크가 1개 늘어나지만, 개당 피해는 줄어듭니다.", 46, 4),
                    KnockId => Card("강력한 반응", "플라스크 폭발 피해가 15% 늘어납니다.", 44, 3),
                    _ => null,
                };
        }
        return null;
    }

    public static Sprite KitIcon(int id)
    {
        KitCard c = KitCardOf(id);
        return c != null ? Resources.Load<Sprite>("Icons/ability_" + c.icon) : null;
    }

    void KitSetAbilitys()
    {
        foreach (int id in new[] { PierceId, GlareId, MultiId, KnockId })
        {
            KitCard c = KitCardOf(id);
            if (c == null || id >= ability_name.Length) continue;
            ability_name[id] = Loc.T(c.name);
            ability_content[id] = Loc.T(c.desc) + "\n( " + ability_level[id] + " / " + c.max + " )";
        }
    }

    bool KitTooltip(int id, out string tip)
    {
        tip = null;
        KitCard c = KitCardOf(id);
        if (c == null) return false;
        tip = Loc.T(c.desc) + "\n\n<color=#F5D478>" + Loc.T("현재 ") + ability_level[id] + " / " + c.max + "</color>";
        return true;
    }

    // 캐릭터 카드면 효과를 적용하고 true
    bool KitApply(int id)
    {
        KitCard c = KitCardOf(id);
        CharacterKit kit = CharacterKit.Instance;
        if (c == null || kit == null) return false;

        switch (CharacterData.Selected)
        {
            case CharacterId.Swordsman:
                if (id == PierceId) kit.arcBonus += 15f;
                if (id == GlareId) kit.ultMul += 0.2f;
                if (id == MultiId) kit.reachMul += 0.15f;
                if (id == KnockId) bul.ShootSpeed /= 1.12f;
                break;
            case CharacterId.Rogue:
                if (id == PierceId) bul.pene++;
                if (id == GlareId) kit.ultMul += 0.25f;
                if (id == MultiId) bul.multiShot++;
                if (id == KnockId) { bul.MaxBullet += 2; bul.NowBullet += 2; }
                break;
            case CharacterId.Archer:
                if (id == PierceId) bul.pene++;
                if (id == GlareId) kit.ultMul += 0.2f;
                if (id == MultiId) bul.multiShot++;
                if (id == KnockId) kit.powerMul += 0.12f;
                break;
            case CharacterId.Alchemist:
                if (id == PierceId) kit.blastMul += 0.15f;
                if (id == GlareId) kit.ultMul += 0.2f;
                if (id == MultiId) bul.multiShot++;
                if (id == KnockId) kit.powerMul += 0.15f;
                break;
        }
        if (ability_level[id] >= c.max) ability_selected[id] = true;
        return true;
    }
}

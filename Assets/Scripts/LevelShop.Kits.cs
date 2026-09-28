using UnityEngine;

// 캐릭터 전용 레벨업 카드: 무기와 관련된 카드(관통 · 노려보는 눈빛 · 멀티 샷 · 밀어내기)만
// 캐릭터의 평타 · 우클릭에 맞는 특수 능력 카드로 바뀜 (기본 스펙을 올리지 않음). 나머지 카드는 모든 캐릭터가 같이 씀
public partial class LevelShop
{
    // 바뀌는 카드 번호
    const int PierceId = 0, GlareId = 3, MultiId = 6, KnockId = 7;
    // 도적만 쓰는 다섯 번째 칸: 모두에게서 빠진 11번(피의 굶주림) 자리를 빌림
    public const int HungerId = 11;

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
        foreach (int id in new[] { PierceId, MultiId, KnockId, GlareId, HungerId })
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
                    PierceId => Card("날아가는 검기", "평타를 휘두르면 검기가 날아가 적을 꿰뚫습니다.\n( 레벨마다 검기 피해 증가 )", 60, 3),
                    MultiId => Card("흡혈 베기", "평타로 벤 적 하나당(최대 3) 체력을 회복합니다.", 61, 3),
                    KnockId => Card("쳐내기", "평타를 휘두르면 범위 안의 적 투사체를 베어 없앱니다.", 62, 1),
                    GlareId => Card("칼바람", "회전 베기 뒤 칼바람이 몸을 감싸고 돌며 주변을 벱니다.\n( 레벨마다 지속 시간 +1초 )", 63, 3),
                    _ => null,
                };
            case CharacterId.Rogue:
                return id switch
                {
                    PierceId => Card("도탄 표창", "표창이 적에 맞으면 가까운 다른 적에게 튕겨 날아갑니다.\n( 레벨마다 튕기는 횟수 +1 )", 64, 3),
                    MultiId => Card("갈고리 표창", "표창에 맞은 적이 출혈을 입습니다.", 65, 3),
                    KnockId => Card("그림자 분신", "출혈 돌진을 시작한 자리에 분신이 남아 표창을 던집니다.\n( 레벨마다 지속 시간 +1초 )", 66, 3),
                    HungerId => Card("사냥의 기세", "적을 처치할 때마다 스킬 게이지가 조금 찹니다.\n( 레벨마다 1%, 공격력이 오를수록 더 · 최대 4배 )", 76, 5),
                    GlareId => Card("표창 폭풍", "출혈 돌진이 끝나는 자리에서 표창이 사방으로 퍼집니다.\n( 레벨마다 표창 +4개 )", 67, 3),
                    _ => null,
                };
            case CharacterId.Archer:
                return id switch
                {
                    PierceId => Card("분열 화살", "가득 당긴 화살이 처음 맞힌 적에게서 여러 갈래로 갈라집니다.\n( 레벨마다 갈래 +1 )", 68, 3),
                    MultiId => Card("메아리 화살", "화살을 쏘면 잠시 뒤 유령 화살이 같은 방향으로 한 발 더 날아갑니다. (피해 50%)", 69, 1),
                    KnockId => Card("바람 걸음", "가득 당긴 화살을 쏘면 반동으로 뒤로 휙 물러납니다.", 70, 1),
                    GlareId => Card("가시 덤불", "화살비가 떨어진 자리에 가시 덤불이 남아 적을 느리게 하고 찌릅니다.\n( 레벨마다 지속 시간 +1초 )", 71, 3),
                    _ => null,
                };
            case CharacterId.Alchemist:
                return id switch
                {
                    PierceId => Card("연쇄 반응", "플라스크 폭발로 쓰러진 적이 그 자리에서 한 번 더 터집니다.", 72, 3),
                    MultiId => Card("급속 냉동", "빙결 시약이 적을 느리게 하는 대신 꽁꽁 얼립니다.\n( 레벨마다 얼리는 시간 증가 )", 73, 3),
                    KnockId => Card("호문쿨루스", "작은 조수가 머리 위를 맴돌며 적에게 플라스크를 던집니다.\n( 레벨마다 던지는 간격 감소 )", 74, 3),
                    GlareId => Card("파편 플라스크", "대폭발 플라스크가 터지며 작은 플라스크들이 흩어져 다시 터집니다.\n( 레벨마다 파편 +2개 )", 75, 3),
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
        foreach (int id in new[] { PierceId, GlareId, MultiId, KnockId, HungerId })
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

        // 카드 칸 → CharacterKit.card 번호 (0 관통 · 1 멀티 샷 · 2 밀어내기 · 3 노려보는 눈빛 자리)
        int slot = id == PierceId ? 0 : id == MultiId ? 1 : id == KnockId ? 2 : id == HungerId ? 4 : 3;
        kit.card[slot]++;
        kit.CardPicked(slot);
        if (ability_level[id] >= c.max) ability_selected[id] = true;
        return true;
    }
}

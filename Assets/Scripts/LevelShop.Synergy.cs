using TMPro;
using UnityEngine;

// 빌드 시너지 표시 (2.2.2): 레벨업 카드 중, 이미 가진 카드와 잘 어울리는 카드 아래에 「시너지 · 그 카드」
// 어떤 빌드를 노릴지 감이 오게 (예: 멀티 샷을 가졌으면 소각탄 · 전기탄 · 폭발 탄두 카드에 표시)
public partial class LevelShop
{
    // 서로 어울리는 카드 (한국어 카드 이름, 양쪽 모두에 표시)
    static readonly string[][] Synergies =
    {
        // 공용
        new[] { "코인충", "코인 자석" }, new[] { "피의 굶주림", "생명의 샘" },
        // 거너
        new[] { "멀티 샷", "소각탄" }, new[] { "멀티 샷", "전기탄" }, new[] { "멀티 샷", "폭발 탄두" },
        new[] { "관통하는 총알", "도탄 사격" }, new[] { "전기탄", "도탄 사격" }, new[] { "장전 충격파", "밀어내기" },
        // 검사
        new[] { "흡혈 베기", "연속 베기" }, new[] { "굳건한 자세", "칼바람" },
        // 도적
        new[] { "갈고리 표창", "급소 노리기" }, new[] { "갈고리 표창", "도탄 표창" }, new[] { "그림자 분신", "표창 폭풍" },
        // 궁수
        new[] { "사냥감 표식", "분열 화살" }, new[] { "사냥감 표식", "메아리 화살" }, new[] { "정조준", "강풍 화살" },
        // 연금술사
        new[] { "원소 융합", "급속 냉동" }, new[] { "원소 융합", "끈적한 산성" }, new[] { "연쇄 반응", "파편 플라스크" },
    };

    readonly TMP_Text[] synergyTags = new TMP_Text[3];

    // 지금 영웅의 카드 번호 (번역된 이름으로 찾음, 없으면 -1)
    int CardIdByName(string ko)
    {
        string t = Loc.T(ko);
        for (int i = 0; i < ability_name.Length; i++)
            if (ability_name[i] == t) return i;
        return -1;
    }

    // 이 카드와 어울리면서 이미 가진 카드 이름 (없으면 null)
    string SynergyPartner(int id)
    {
        if (id < 0 || id >= ability_name.Length || id == SupplyId) return null;
        foreach (string[] pair in Synergies)
            for (int k = 0; k < 2; k++)
            {
                if (CardIdByName(pair[k]) != id) continue;
                int other = CardIdByName(pair[1 - k]);
                if (other >= 0 && other != id && LevelOf(other) > 0) return ability_name[other];
            }
        return null;
    }

    void ShowSynergyTags(int[] ids)
    {
        for (int slot = 0; slot < 3; slot++)
        {
            Transform card = CardOf(slot);
            if (card == null) continue;
            if (synergyTags[slot] == null)
            {
                UIKit.EnsureStyle();
                RectTransform r = UIKit.Rect("SynergyTag", card, Vector2.zero, new Vector2(400f, 44f));
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, -6f);
                TMP_Text t = UIKit.Text(r, "", 24f, new Color(0.55f, 1f, 0.6f), Vector2.zero, new Vector2(400f, 44f));
                synergyTags[slot] = t;
            }
            string partner = SynergyPartner(ids[slot]);
            synergyTags[slot].gameObject.SetActive(partner != null);
            if (partner != null) synergyTags[slot].text = Loc.T("시너지") + " · " + partner;
        }
    }
}

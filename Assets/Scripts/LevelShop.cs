using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelShop : MonoBehaviour
{
    public GameObject LvshopPanel;

    [Header("첫번째 버튼 텍스트")]
    public TextMeshProUGUI FirstTitle;
    public TextMeshProUGUI FirstAbility;

    [Header("두번째 버튼 텍스트")]
    public TextMeshProUGUI SecondTitle;
    public TextMeshProUGUI SecondAbility;

    [Header("세번째 버튼 텍스트")]
    public TextMeshProUGUI ThirdTitle;
    public TextMeshProUGUI ThirdAbility;

    public int remaining_abilitys;
    public int Total_abilitys;

    public string[] ability_name = new string[10];
    public string[] ability_content = new string[10];
    public bool[] ability_selected = new bool[10];
    // 능력별로 몇 번 선택했는지 (HUD의 Lv 표시)
    public int[] ability_level = new int[10];

    [Header("능력 아이콘 표시")]
    public AbilityHUD abilityHUD;
    public Image FirstIcon;
    public Image SecondIcon;
    public Image ThirdIcon;

    PlayerController bul;
    Level lv;

    // Start is called before the first frame update
    void Start()
    {

        Total_abilitys = AbilityCount;
        remaining_abilitys = AbilityCount;

        // 씬에 저장된 배열은 10칸이라 새 능력 수에 맞게 늘림
        System.Array.Resize(ref ability_name, AbilityCount);
        System.Array.Resize(ref ability_content, AbilityCount);
        System.Array.Resize(ref ability_selected, AbilityCount);
        System.Array.Resize(ref ability_level, AbilityCount);

        bul = FindFirstObjectByType<PlayerController>();
        lv = FindFirstObjectByType<Level>();
        for (int i = 0; i< Total_abilitys; i++)
        {
            // 11번(처치 시 회복)은 더 이상 나오지 않음
            ability_selected[i] = i == 11;
        }
        //레벨업 능력들
        setAbilitys();
        

        openLevelShop();
    }

    // Update is called once per frame

    void setAbilitys()
    {
        ability_name[0] = Loc.T("관통하는 총알");
        ability_content[0] = Loc.T("총알의 관통력이 증가합니다 \n( 관통 ") + (bul.pene - 1) + " -> " + bul.pene + " )";

        ability_name[1] = Loc.T("코인충");
        ability_content[1] = Loc.T("코인을 더 많이 획득합니다.\n( 코인 획득량 ") + ( 1 + bul.bonusCoin) + " -> " + (2 + bul.bonusCoin ) + " )";

        ability_name[2] = Loc.T("재활용 에너지");
        ability_content[2] = Loc.T("스킬 게이지가 20% 감소합니다\n( 게이지 -20% )");

        ability_name[3] = Loc.T("노려보는 눈빛");
        ability_content[3] = Loc.T("스킬을 사용하는 동안 타겟이 된 적이 더 느려집니다. \n( 속도 -20% )");

        ability_name[4] = Loc.T("더 많은 경험치");
        ability_content[4] = Loc.T("킬 경험치 +10%");

        ability_name[5] = Loc.T("코인 자석");
        ability_content[5] = Loc.T("주변의 코인을 끌어옵니다.\n( 범위 ") + bul.coinMagnetRange.ToString("0") + " -> " + (bul.coinMagnetRange + MagnetStep).ToString("0") + " )";

        ability_name[6] = Loc.T("멀티 샷");
        ability_content[6] = Loc.T("한 번에 쏘는 총알이 1발 늘어나지만, 한 발당 피해는 줄어듭니다.\n( ")
            + bul.multiShot + Loc.T("발 ") + Percent(bul.MultiShotDamageRate(bul.multiShot)) + " -> "
            + (bul.multiShot + 1) + Loc.T("발 ") + Percent(bul.MultiShotDamageRate(bul.multiShot + 1)) + " )";

        ability_name[7] = Loc.T("밀어내기");
        ability_content[7] = Loc.T("총알이 적을 밀어내는 효과 +25%\n( 최대 ") + KnockBackMaxLevel + Loc.T("번 )");

        ability_name[8] = Loc.T("강철같은 심장");
        ability_content[8] = Loc.T("체력을 즉시 모두 회복하며, 최대 체력이 12% 증가합니다.");

        ability_name[9] = Loc.T("단단한 신체");
        ability_content[9] = Loc.T("받는 피해를 12% 감소시킵니다.\n( 최대 ") + DefMaxLevel + Loc.T("번 )");

        ability_name[10] = Loc.T("생명의 샘");
        ability_content[10] = Loc.T("시간이 지나면 체력이 조금씩 회복됩니다.\n( 초당 ") + bul.regenPerSecond.ToString("0.#") + " -> " + (bul.regenPerSecond + RegenStep).ToString("0.#") + " )";

        ability_name[11] = Loc.T("피의 굶주림");
        ability_content[11] = Loc.T("적을 처치할 때마다 체력을 회복합니다.\n( 처치당 ") + bul.healOnKill.ToString("0") + " -> " + (bul.healOnKill + HealOnKillStep).ToString("0") + " )";
    }
    void Update()
    {
        setAbilitys();
        UpdateSelectLock();

        // 클릭으로 고른 카드를 스페이스바로 확정
        if (selectReady && pendingSlot >= 0 && LvshopPanel != null && LvshopPanel.activeInHierarchy && Input.GetKeyDown(KeyCode.Space))
        {
            int what = pendingSlot == 0 ? first : pendingSlot == 1 ? second : third;
            pendingSlot = -1;
            onSelect(what);
        }
    }

    // 클릭한 카드 (0~2, -1 = 아직 없음)
    int pendingSlot = -1;
    TextMeshProUGUI selectHint;

    Transform CardOf(int slot)
    {
        TextMeshProUGUI title = slot == 0 ? FirstTitle : slot == 1 ? SecondTitle : ThirdTitle;
        if (title == null) return null;
        Button b = title.GetComponentInParent<Button>();
        return b != null ? b.transform : title.transform.parent;
    }

    void PickCard(int slot)
    {
        if (!selectReady) return;
        pendingSlot = slot;
        RefreshCards();
    }

    // 고른 카드는 커지고 금빛, 아래 안내 글자도 바뀜
    void RefreshCards()
    {
        for (int i = 0; i < 3; i++)
        {
            Transform card = CardOf(i);
            if (card == null) continue;
            bool on = i == pendingSlot;
            card.localScale = Vector3.one * (on ? 1.08f : 1f);
            if (card.TryGetComponent(out Image img)) img.color = on ? new Color(1f, 0.88f, 0.55f) : Color.white;
        }

        if (selectHint == null && LvshopPanel != null)
        {
            GameObject go = new GameObject("SelectHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(LvshopPanel.transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(1000f, 50f);
            r.anchoredPosition = new Vector2(0f, 70f);
            selectHint = go.GetComponent<TextMeshProUGUI>();
            if (FirstTitle != null)
            {
                selectHint.font = FirstTitle.font;
                selectHint.fontSharedMaterial = FirstTitle.fontSharedMaterial;
            }
            selectHint.fontSize = 30f;
            selectHint.alignment = TextAlignmentOptions.Center;
            selectHint.raycastTarget = false;
        }
        if (selectHint != null)
        {
            string picked = pendingSlot == 0 ? FirstTitle.text : pendingSlot == 1 ? SecondTitle.text : pendingSlot == 2 ? ThirdTitle.text : null;
            selectHint.color = picked != null ? new Color(0.96f, 0.83f, 0.47f) : new Color(0.92f, 0.88f, 0.8f);
            selectHint.text = picked != null ? Loc.T("[Space] 확정 : ") + picked : Loc.T("카드를 클릭해 고른 뒤 [Space]로 확정");
        }
    }

    // 사격하던 클릭이 열리자마자 카드를 누르지 않도록:
    // 창이 열리고 잠깐 지난 뒤 마우스 버튼을 한 번 떼야 고를 수 있음
    const float SelectDelay = 0.6f;
    float openedAt;
    bool selectReady;
    CanvasGroup cardGroup;

    void LockSelection()
    {
        openedAt = Time.unscaledTime;
        selectReady = false;
        if (cardGroup == null && LvshopPanel != null)
        {
            cardGroup = LvshopPanel.GetComponent<CanvasGroup>();
            if (cardGroup == null) cardGroup = LvshopPanel.AddComponent<CanvasGroup>();
        }
        if (cardGroup != null)
        {
            cardGroup.interactable = false;
            cardGroup.alpha = 0.6f;
        }
    }

    void UpdateSelectLock()
    {
        if (selectReady || LvshopPanel == null || !LvshopPanel.activeInHierarchy) return;

        float t = Mathf.Clamp01((Time.unscaledTime - openedAt) / SelectDelay);
        if (cardGroup != null) cardGroup.alpha = Mathf.Lerp(0.6f, 1f, t);

        bool mouseHeld = Input.GetMouseButton(0) || Input.GetMouseButton(1);
        if (t >= 1f && !mouseHeld)
        {
            selectReady = true;
            if (cardGroup != null)
            {
                cardGroup.interactable = true;
                cardGroup.alpha = 1f;
            }
        }
    }
    private bool showLv;
    public GameObject LvUpPanel;
    public void toggleLevelUp()
    {
        showLv = !showLv;
        LvUpPanel.SetActive(showLv);
    }
    public void openLevelShop()
    {
        
        LvshopPanel.SetActive(true);
        LockSelection();
        UpdateLvShopContent();
        pendingSlot = -1;
        RefreshCards();
        

    }
    void closeLevelShop()
    {
        if (LvshopPanel != null) LvshopPanel.SetActive(false);
        Time.timeScale = 1f;
    }
    private int first = 0, second = 1, third = 2;
    void UpdateLvShopContent()
    {
        //첫번째 능력 선택
        first = Random.Range(0, Total_abilitys);
        while (ability_selected[first]) first = Random.Range(0, Total_abilitys);

        FirstTitle.text = ability_name[first];
        FirstAbility.text = ability_content[first];

        //두번째 능력 선택
        second = Random.Range(0, Total_abilitys);
        //첫번째와 겹치지 않기
        while (second == first || ability_selected[second]) second = Random.Range(0, Total_abilitys);

        SecondTitle.text = ability_name[second];
        SecondAbility.text = ability_content[second];

        //세번째 능력 선택
        third = Random.Range(0, Total_abilitys);
        //첫번째, 두번째랑 겹치지 않기
        while (third == first || third == second || ability_selected[third]) third = Random.Range(0, Total_abilitys);

        ThirdTitle.text = ability_name[third];
        ThirdAbility.text = ability_content[third];

        SetCardIcon(FirstIcon, first);
        SetCardIcon(SecondIcon, second);
        SetCardIcon(ThirdIcon, third);

        Time.timeScale = 0f;
    }

    SkillGauge skill;

    const int MinSkillPoint = 10;

    static string Percent(float rate) => Mathf.RoundToInt(rate * 100f) + "%";
    const float MagnetStep = 4f;            // 코인 자석 1회당 끌어오는 범위
    const int MagnetMaxLevel = 4;
    const float DefStep = 0.12f;            // 단단한 신체 1회당 받는 피해 감소
    const int DefMaxLevel = 3;

    // 밀어내기 표시용 기본 넉백 값 (PlayerController.knockBack 초기값)
    const float BaseKnockBack = 0.3f;
    const int AbilityCount = 12;
    const float RegenStep = 0.5f;       // 생명의 샘 1회당 초당 회복량
    const int RegenMaxLevel = 4;
    const float HealOnKillStep = 1f;    // 피의 굶주림 1회당 처치 회복량
    const int HealOnKillMaxLevel = 3;
    const float KnockBackGrowth = 1.25f;
    const int KnockBackMaxLevel = 3;

    // 툴팁용: 능력 설명 + 지금 적용 중인 수치
    public string GetAbilityTooltip(int id)
    {
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        if (lv == null) lv = FindFirstObjectByType<Level>();
        if (skill == null) skill = FindFirstObjectByType<SkillGauge>();
        if (bul == null) return "";

        string summary = "";
        string current = "";

        switch (id)
        {
            case 0:
                summary = Loc.T("총알이 적을 뚫고 지나갑니다.");
                current = Loc.T("관통: 적 ") + (bul.pene - 1) + Loc.T("마리");
                break;
            case 1:
                summary = Loc.T("코인을 주울 때 더 많이 얻습니다.");
                current = Loc.T("코인 1개당 획득량: ") + (1 + bul.bonusCoin);
                break;
            case 2:
                summary = Loc.T("스킬 게이지가 더 빨리 가득 찹니다. (적을 처치하면 잠깐 더 빨라짐)");
                current = Loc.T("게이지가 가득 차는 시간: ") + (skill != null ? (skill.MaxSkillPoint / skill.pointsPerSecond).ToString("0") : "0") + Loc.T("초");
                break;
            case 3:
                summary = Loc.T("스킬을 쓰는 동안 적이 더 느려집니다.");
                current = Loc.T("스킬 중 시간 속도: ") + (bul.Skill_setTime * 100f).ToString("0.#") + "%";
                break;
            case 4:
                summary = Loc.T("적을 처치할 때 얻는 경험치가 늘어납니다.");
                current = Loc.T("경험치 배율: ") + ((lv != null ? lv.bonusEXP : 1f) * 100f).ToString("0") + "%";
                break;
            case 5:
                summary = Loc.T("주변의 코인을 끌어옵니다.");
                current = Loc.T("끌어오는 범위: ") + bul.coinMagnetRange.ToString("0") + " (" + ability_level[5] + "/" + MagnetMaxLevel + ")";
                break;
            case 6:
                summary = Loc.T("한 번에 여러 발을 부채꼴로 발사합니다. 발사 수가 늘수록 한 발당 피해는 줄어듭니다.");
                current = Loc.T("발사 수: ") + bul.multiShot + Loc.T("발 (최대 5발)\n현재 한 발당 피해: ")
                    + Percent(bul.MultiShotDamageRate(bul.multiShot));
                break;
            case 7:
                summary = Loc.T("총알이 적을 더 멀리 밀어냅니다.");
                current = Loc.T("넉백: 기본의 ") + (bul.knockBack / BaseKnockBack * 100f).ToString("0") + "% (" + ability_level[7] + "/" + KnockBackMaxLevel + ")";
                break;
            case 8:
                summary = Loc.T("최대 체력이 늘어나고, 선택하는 순간 체력을 모두 회복합니다.");
                current = Loc.T("최대 체력: ") + Mathf.RoundToInt(bul.PlayerMaxHealth);
                break;
            case 9:
                summary = Loc.T("적에게 받는 피해가 줄어듭니다.");
                current = Loc.T("받는 피해 감소: ") + (bul.def * 100f).ToString("0") + "%";
                break;
            case 10:
                summary = Loc.T("시간이 지나면 체력이 조금씩 회복됩니다.");
                current = Loc.T("초당 회복: ") + bul.regenPerSecond.ToString("0.#") + " (" + ability_level[10] + "/" + RegenMaxLevel + ")";
                break;
            case 11:
                summary = Loc.T("적을 처치할 때마다 체력을 회복합니다.");
                current = Loc.T("처치당 회복: ") + bul.healOnKill.ToString("0") + " (" + ability_level[11] + "/" + HealOnKillMaxLevel + ")";
                break;
        }

        return summary + Loc.T("\n\n<color=#F5D478>현재 ") + current + "</color>";
    }

    // 카드를 누르면 고르기만 하고, 확정은 스페이스바
    public void onFirstButton() => PickCard(0);
    public void onSecondButton() => PickCard(1);
    public void onThirdButton() => PickCard(2);
    void SetCardIcon(Image target, int id)
    {
        if (target == null || abilityHUD == null) return;

        Sprite icon = abilityHUD.GetIcon(id);
        if (icon != null) target.sprite = icon;
    }

    void onSelect(int what)
    {
        if (!selectReady) return;
        selectReady = false;
        pendingSlot = -1;
        RefreshCards();
        skill = FindFirstObjectByType<SkillGauge>();
        bul = FindFirstObjectByType<PlayerController>();
        closeLevelShop();

        ability_level[what]++;
        if (abilityHUD != null) abilityHUD.SetAbility(what, ability_level[what]);
        if (what == 0) bul.pene++;
        if (what == 1) bul.bonusCoin++;
        if (what == 2)
        {
            // 너무 쉬워지지 않도록 최소 5회까지만 줄어듦
            skill.MaxSkillPoint = Mathf.Max(MinSkillPoint, Mathf.RoundToInt(skill.MaxSkillPoint * 0.8f));
            if (skill.MaxSkillPoint <= MinSkillPoint) ability_selected[2] = true;
        }
        if (what == 3) bul.Skill_setTime *= 0.8f;
        if (what == 4) lv.bonusEXP += 0.1f;
        if (what == 5)
        {
            bul.coinMagnetRange += MagnetStep;
            if (ability_level[5] >= MagnetMaxLevel) ability_selected[5] = true;
        }
        if (what == 6)
        {
            bul.multiShot += 1;
            if (bul.multiShot >= 5) ability_selected[6] = true;
        }
        if (what == 7)
        {
            bul.knockBack *= KnockBackGrowth;
            if (ability_level[7] >= KnockBackMaxLevel) ability_selected[7] = true;
        }
        if (what == 8)
        {
            bul.PlayerMaxHealth = Mathf.Round(bul.PlayerMaxHealth * 1.12f);
            bul.PlayerHealth = bul.PlayerMaxHealth;
        }
        if (what == 9)
        {
            bul.def += DefStep;
            if (ability_level[9] >= DefMaxLevel) ability_selected[9] = true;
        }
        if (what == 10)
        {
            bul.regenPerSecond += RegenStep;
            if (ability_level[10] >= RegenMaxLevel) ability_selected[10] = true;
        }
        if (what == 11)
        {
            bul.healOnKill += HealOnKillStep;
            if (ability_level[11] >= HealOnKillMaxLevel) ability_selected[11] = true;
        }
    }
    


}

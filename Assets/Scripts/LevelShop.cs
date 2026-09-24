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

        Total_abilitys = 10;
        remaining_abilitys = 10;

        bul = FindFirstObjectByType<PlayerController>();
        lv = FindFirstObjectByType<Level>();
        for (int i = 0; i< Total_abilitys; i++)
        {
            ability_selected[i] = false;
        }
        //레벨업 능력들
        setAbilitys();
        

        openLevelShop();
    }

    // Update is called once per frame

    void setAbilitys()
    {
        ability_name[0] = "관통하는 총알";
        ability_content[0] = "총알의 관통력이 증가합니다 \n( 관통 " + (bul.pene - 1) + " -> " + bul.pene + " )";

        ability_name[1] = "코인충";
        ability_content[1] = "코인을 더 많이 획득합니다.\n( 코인 획득량 " + ( 1 + bul.bonusCoin) + " -> " + (2 + bul.bonusCoin ) + " )";

        ability_name[2] = "재활용 에너지";
        ability_content[2] = "스킬 게이지가 20% 감소합니다\n( 게이지 -20% )";

        ability_name[3] = "노려보는 눈빛";
        ability_content[3] = "스킬을 사용하는 동안 타겟이 된 적이 더 느려집니다. \n( 속도 -20% )";

        ability_name[4] = "더 많은 경험치";
        ability_content[4] = "킬 경험치 +20%";

        ability_name[5] = "흡혈 스킬";
        ability_content[5] = "스킬로 맞춘 적 1명당 체력을 회복합니다.\n( 회복량 " + bul.getHP + " -> " + (1 + bul.getHP);

        ability_name[6] = "멀티 샷";
        ability_content[6] = "멀티샷 +1";

        ability_name[7] = "밀어내기";
        ability_content[7] = "총알이 적을 밀어내는 효과 +60% ";

        ability_name[8] = "강철같은 심장";
        ability_content[8] = "체력을 즉시 모두 회복하며, 최대 체력이 30% 증가합니다.";

        ability_name[9] = "단단한 신체";
        ability_content[9] = "받는 피해를 30% 감소시킵니다.";
    }
    void Update()
    {
        setAbilitys();
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
        UpdateLvShopContent();
        

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

    // 밀어내기 표시용 기본 넉백 값 (PlayerController.knockBack 초기값)
    const float BaseKnockBack = 0.4f;

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
                summary = "총알이 적을 뚫고 지나갑니다.";
                current = "관통: 적 " + (bul.pene - 1) + "마리";
                break;
            case 1:
                summary = "코인을 주울 때 더 많이 얻습니다.";
                current = "코인 1개당 획득량: " + (1 + bul.bonusCoin);
                break;
            case 2:
                summary = "스킬 게이지가 더 빨리 가득 찹니다.";
                current = "스킬 발동에 필요한 적중: " + (skill != null ? skill.MaxSkillPoint : 0) + "회";
                break;
            case 3:
                summary = "스킬을 쓰는 동안 적이 더 느려집니다.";
                current = "스킬 중 시간 속도: " + (bul.Skill_setTime * 100f).ToString("0.#") + "%";
                break;
            case 4:
                summary = "적을 처치할 때 얻는 경험치가 늘어납니다.";
                current = "경험치 배율: " + ((lv != null ? lv.bonusEXP : 1f) * 100f).ToString("0") + "%";
                break;
            case 5:
                summary = "스킬로 맞힌 적 1명당 체력을 회복합니다.";
                current = "회복량: 적 1명당 " + bul.getHP;
                break;
            case 6:
                summary = "한 번에 여러 발을 부채꼴로 발사합니다.";
                current = "발사 수: " + bul.multiShot + "발 (최대 5발)";
                break;
            case 7:
                summary = "총알이 적을 더 멀리 밀어냅니다.";
                current = "넉백: 기본의 " + (bul.knockBack / BaseKnockBack * 100f).ToString("0") + "%";
                break;
            case 8:
                summary = "최대 체력이 늘어나고, 선택하는 순간 체력을 모두 회복합니다.";
                current = "최대 체력: " + Mathf.RoundToInt(bul.PlayerMaxHealth);
                break;
            case 9:
                summary = "적에게 받는 피해가 줄어듭니다.";
                current = "받는 피해 감소: " + (bul.def * 100f).ToString("0") + "%";
                break;
        }

        return summary + "\n\n<color=#F5D478>현재 " + current + "</color>";
    }

    public void onFirstButton()
    {
        onSelect(first);
        Debug.Log(first);
    }
    public void onSecondButton()
    {
        onSelect(second);
        Debug.Log(second);
    }
    public void onThirdButton()
    {
        onSelect(third);
        Debug.Log(third);
    }
    void SetCardIcon(Image target, int id)
    {
        if (target == null || abilityHUD == null) return;

        Sprite icon = abilityHUD.GetIcon(id);
        if (icon != null) target.sprite = icon;
    }

    void onSelect(int what)
    {
        skill = FindFirstObjectByType<SkillGauge>();
        bul = FindFirstObjectByType<PlayerController>();
        closeLevelShop();

        ability_level[what]++;
        if (abilityHUD != null) abilityHUD.SetAbility(what, ability_level[what]);
        if (what == 0) bul.pene++;
        if (what == 1) bul.bonusCoin++;
        if (what == 2) skill.MaxSkillPoint = (int) (skill.MaxSkillPoint * 0.8f);
        if (what == 3) bul.Skill_setTime *= 0.8f;
        if (what == 4) lv.bonusEXP += 0.2f;
        if (what == 5) bul.getHP += 1;
        if (what == 6)
        {
            bul.multiShot += 1;
            if (bul.multiShot >= 5) ability_selected[6] = true;
        }
        if (what == 7)
        {
            bul.knockBack *= 1.6f;
            if (bul.knockBack >= 2) ability_selected[7] = true;
        }
        if (what == 8)
        {
            bul.PlayerMaxHealth = (int)(bul.PlayerMaxHealth * 1.3);
            bul.PlayerHealth = bul.PlayerMaxHealth;
        }
        if (what == 9)
        {
            bul.def += 0.3f;
            if (bul.def >= 0.6f) ability_selected[9] = true;
        }
    }
    


}

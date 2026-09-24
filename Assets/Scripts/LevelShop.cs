using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        ability_name[4] = "더 많 경험치";
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
        ability_content[9] = "받는 피해를 20% 감소시킵니다.";
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

        Time.timeScale = 0f;
    }

    SkillGauge skill;

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
    void onSelect(int what)
    {
        skill = FindFirstObjectByType<SkillGauge>();
        bul = FindFirstObjectByType<PlayerController>();
        closeLevelShop();
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

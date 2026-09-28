using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class LevelShop : MonoBehaviour
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
            // 11번(처치 시 회복)은 더 이상 나오지 않음 (도적은 사냥의 기세), 12 · 13번은 캐릭터 카드, 14번은 비상 보급 전용
            ability_selected[i] = ((i == 11 || i == 12 || i == 13) && KitIcon(i) == null) || i == SupplyId;
        }
        CompactLevelUp();
        //레벨업 능력들
        setAbilitys();
        

        openLevelShop();
    }

    // Update is called once per frame

    void setAbilitys()
    {
        // 공용 카드 (0 ~ 11) · 비상 보급: 모두 같은 모양 (한 문장 + 수치 지금 → 다음 + Lv)
        string[] names = { "관통하는 총알", "코인충", "재활용 에너지", "노려보는 눈빛", "더 많은 경험치", "코인 자석",
                           "멀티 샷", "밀어내기", "강철같은 심장", "단단한 신체", "생명의 샘", "피의 굶주림" };
        for (int i = 0; i < names.Length && i < ability_name.Length; i++)
        {
            ability_name[i] = Loc.T(names[i]);
            ability_content[i] = SharedCardText(i);
        }
        ability_name[SupplyId] = Loc.T("비상 보급");
        ability_content[SupplyId] = SharedCardText(SupplyId);
        KitSetAbilitys();
    }
    void Update()
    {
        setAbilitys();
        UpdateSelectLock();

        // 클릭으로 고른 카드를 스페이스바로 확정
        if (selectReady && pendingSlot >= 0 && LvshopPanel != null && LvshopPanel.activeInHierarchy && KeyBindings.Down(GameAction.Interact))
        {
            int what = pendingSlot == 0 ? first : pendingSlot == 1 ? second : third;
            pendingSlot = -1;
            onSelect(what);
        }
        // 쌓인 레벨업: [Space]로 하나씩 고름 (상점 가판대 옆이면 상점이 먼저)
        else if (PendingLevels > 0 && !IsOpen && Time.timeScale == 1f && !ShopStall.PlayerNear && !ShopOpen && KeyBindings.Down(GameAction.Interact))
        {
            PendingLevels--;
            openLevelShop();
        }
        UpdatePendingBadge();
    }

    // ================================================================= 쌓인 레벨업
    public int PendingLevels { get; private set; }

    public void AddPending(int n = 1)
    {
        PendingLevels += n;
        Hints.Show("levelup", "레벨이 오르면 능력 포인트가 쌓입니다. [{INTERACT}]를 눌러 원할 때 능력을 고르세요.");
    }

    GameObject pendingBadge;
    TextMeshProUGUI pendingText;

    void UpdatePendingBadge()
    {
        bool show = PendingLevels > 0 && !IsOpen;
        if (!show)
        {
            if (pendingBadge != null && pendingBadge.activeSelf) pendingBadge.SetActive(false);
            return;
        }
        if (pendingBadge == null)
        {
            Canvas canvas = LvshopPanel != null ? LvshopPanel.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            canvas = canvas.rootCanvas;
            pendingBadge = new GameObject("LevelUpPending", typeof(RectTransform), typeof(Image));
            RectTransform r = pendingBadge.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(0f, 110f);
            r.sizeDelta = new Vector2(560f, 48f);
            Image bg = pendingBadge.GetComponent<Image>();
            bg.color = new Color(0.35f, 0.22f, 0.02f, 0.85f);
            bg.raycastTarget = false;
            GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform tr = tg.GetComponent<RectTransform>();
            tr.SetParent(r, false);
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(12f, 2f);
            tr.offsetMax = new Vector2(-12f, -2f);
            pendingText = tg.GetComponent<TextMeshProUGUI>();
            UIKit.EnsureStyle();
            if (UIKit.Font != null) pendingText.font = UIKit.Font;
            if (UIKit.FontMaterial != null) pendingText.fontSharedMaterial = UIKit.FontMaterial;
            pendingText.enableAutoSizing = true;
            pendingText.fontSizeMin = 14f;
            pendingText.fontSizeMax = 24f;
            pendingText.alignment = TextAlignmentOptions.Center;
            pendingText.raycastTarget = false;
        }
        if (!pendingBadge.activeSelf) pendingBadge.SetActive(true);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        pendingText.color = Color.Lerp(new Color(1f, 0.85f, 0.4f), Color.white, pulse);
        pendingText.text = Loc.T("레벨업!") + "  [" + KeyBindings.Name(GameAction.Interact) + "] " + Loc.T("능력 고르기") + (PendingLevels > 1 ? "  x" + PendingLevels : "");
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
            selectHint.text = picked != null ? Loc.T("[{INTERACT}] 확정 : ") + picked : Loc.T("카드를 클릭해 고른 뒤 [{INTERACT}]로 확정");
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
    // 「레벨업!」 표시를 작게 줄여 알림판 아래에 (시야를 가리지 않게)
    void CompactLevelUp()
    {
        if (LvUpPanel == null) return;
        RectTransform r = LvUpPanel.GetComponent<RectTransform>();
        if (r == null) return;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -150f);
        r.localScale = Vector3.one * 0.4f;
        foreach (Graphic g in LvUpPanel.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
    }

    public void toggleLevelUp()
    {
        showLv = !showLv;
        LvUpPanel.SetActive(showLv && !ShopOpen);
    }

    Shop shopRef;
    bool ShopOpen
    {
        get
        {
            if (shopRef == null) shopRef = FindFirstObjectByType<Shop>();
            return shopRef != null && shopRef.isShopOpen;
        }
    }

    // 상점이 열려 있는 동안은 레벨업 표시가 화면을 가리지 않게
    void LateUpdate()
    {
        if (LvUpPanel != null && LvUpPanel.activeSelf != (showLv && !ShopOpen)) LvUpPanel.SetActive(showLv && !ShopOpen);
    }
    public bool IsOpen => LvshopPanel != null && LvshopPanel.activeSelf;

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
        // 아직 고를 수 있는 카드 중에서 세 장 (모자라면 비상 보급)
        System.Collections.Generic.List<int> open = new System.Collections.Generic.List<int>();
        for (int i = 0; i < Total_abilitys; i++) if (!ability_selected[i]) open.Add(i);
        int Draw()
        {
            if (open.Count == 0) return SupplyId;
            int k = Random.Range(0, open.Count);
            int id = open[k];
            open.RemoveAt(k);
            return id;
        }

        //첫번째 능력 선택
        first = Draw();

        FirstTitle.text = ability_name[first];
        FirstAbility.text = ability_content[first];

        //두번째 능력 선택
        second = Draw();

        SecondTitle.text = ability_name[second];
        SecondAbility.text = ability_content[second];

        //세번째 능력 선택
        third = Draw();

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
    const int AbilityCount = 15;
    // 끝없이 고를 수 있던 카드의 상한
    const int CoinMaxLevel = 4, ExpMaxLevel = 4, HeartMaxLevel = 4, GlareMaxLevel = 3;
    const float RegenStep = 0.5f;       // 생명의 샘 1회당 초당 회복량
    const int RegenMaxLevel = 4;
    const float HealOnKillStep = 1f;    // 피의 굶주림 1회당 처치 회복량
    const int HealOnKillMaxLevel = 3;
    const float KnockBackGrowth = 1.25f;
    const int KnockBackMaxLevel = 3;

    // 툴팁용: 카드와 같은 글 (한 문장 + 수치 지금 → 다음 + Lv)
    public string GetAbilityTooltip(int id)
    {
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        if (lv == null) lv = FindFirstObjectByType<Level>();
        if (skill == null) skill = FindFirstObjectByType<SkillGauge>();
        if (bul == null) return "";
        if (KitTooltip(id, out string kitTip)) return kitTip;
        return SharedCardText(id);
    }

    // 공용 카드 설명: 한 문장 + 금색 "수치  지금 → 다음" + Lv
    string SharedCardText(int id)
    {
        if (bul == null) return "";
        int n = id < ability_level.Length ? ability_level[id] : 0;
        string S(string ko) => Loc.T(ko);
        switch (id)
        {
            case 0:
                return CardText(S("총알이 적을 뚫고 지나갑니다."), S("관통"), (bul.pene - 1) + S("마리"), bul.pene + S("마리"), n, 0);
            case 1:
                return CardText(S("코인을 주울 때 더 많이 얻습니다."), S("코인 1개당 획득량"), (1 + bul.bonusCoin).ToString(), (2 + bul.bonusCoin).ToString(), n, CoinMaxLevel);
            case 2:
                {
                    float pps = skill != null ? skill.pointsPerSecond : 1f;
                    int max = skill != null ? skill.MaxSkillPoint : 10;
                    int nextMax = Mathf.Max(MinSkillPoint, Mathf.RoundToInt(max * 0.8f));
                    return CardText(S("스킬 게이지가 더 빨리 가득 찹니다. (적을 처치하면 잠깐 더 빨라짐)"), S("게이지가 가득 차는 시간"),
                                    (max / pps).ToString("0") + S("초"), (nextMax / pps).ToString("0") + S("초"), n, 0);
                }
            case 3:
                return CardText(S("스킬을 쓰는 동안 적이 더 느려집니다."), S("스킬 중 시간 속도"),
                                (bul.Skill_setTime * 100f).ToString("0.#") + "%", (bul.Skill_setTime * 80f).ToString("0.#") + "%", n, GlareMaxLevel);
            case 4:
                {
                    float bonus = lv != null ? lv.bonusEXP : 1f;
                    return CardText(S("적을 처치할 때 얻는 경험치가 늘어납니다."), S("경험치 배율"),
                                    (bonus * 100f).ToString("0") + "%", ((bonus + 0.1f) * 100f).ToString("0") + "%", n, ExpMaxLevel);
                }
            case 5:
                return CardText(S("주변의 코인을 끌어옵니다."), S("끌어오는 범위"),
                                bul.coinMagnetRange.ToString("0") + S("칸"), (bul.coinMagnetRange + MagnetStep).ToString("0") + S("칸"), n, MagnetMaxLevel);
            case 6:
                return CardText(S("한 번에 여러 발을 부채꼴로 발사합니다. 발사 수가 늘수록 한 발당 피해는 줄어듭니다."), S("발사 수 · 한 발당 피해"),
                                bul.multiShot + S("발") + " " + Percent(bul.MultiShotDamageRate(bul.multiShot)),
                                (bul.multiShot + 1) + S("발") + " " + Percent(bul.MultiShotDamageRate(bul.multiShot + 1)), n, 4);
            case 7:
                return CardText(S("총알이 적을 더 멀리 밀어냅니다."), S("넉백"),
                                (bul.knockBack / BaseKnockBack * 100f).ToString("0") + "%", (bul.knockBack * KnockBackGrowth / BaseKnockBack * 100f).ToString("0") + "%", n, KnockBackMaxLevel);
            case 8:
                return CardText(S("최대 체력이 늘어나고, 선택하는 순간 체력을 모두 회복합니다."), S("최대 체력"),
                                Mathf.RoundToInt(bul.PlayerMaxHealth).ToString(), Mathf.RoundToInt(bul.PlayerMaxHealth * 1.12f).ToString(), n, HeartMaxLevel);
            case 9:
                return CardText(S("적에게 받는 피해가 줄어듭니다."), S("받는 피해 감소"),
                                (Mathf.Min(bul.def, PlayerController.MaxDef) * 100f).ToString("0") + "%", (Mathf.Min(bul.def + DefStep, PlayerController.MaxDef) * 100f).ToString("0") + "%", n, DefMaxLevel);
            case 10:
                return CardText(S("시간이 지나면 체력이 조금씩 회복됩니다."), S("초당 회복"),
                                bul.regenPerSecond.ToString("0.#"), (bul.regenPerSecond + RegenStep).ToString("0.#"), n, RegenMaxLevel);
            case 11:
                return CardText(S("적을 처치할 때마다 체력을 회복합니다."), S("처치당 회복"),
                                bul.healOnKill.ToString("0"), (bul.healOnKill + HealOnKillStep).ToString("0"), n, HealOnKillMaxLevel);
            case SupplyId:
                return S("고를 능력을 모두 배웠습니다.") + "\n<color=#F5D478>" + S("보급") + "  " + S("체력 +30% · 코인 +10") + "</color>";
        }
        return "";
    }

    // 카드를 누르면 고르기만 하고, 확정은 스페이스바
    public void onFirstButton() => PickCard(0);
    public void onSecondButton() => PickCard(1);
    public void onThirdButton() => PickCard(2);
    void SetCardIcon(Image target, int id)
    {
        if (target == null || abilityHUD == null) return;

        Sprite icon = KitIcon(id) ?? abilityHUD.GetIcon(id);
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

        // 비상 보급은 능력이 아니라 HUD에 남기지 않음
        if (what != SupplyId)
        {
            ability_level[what]++;
            if (abilityHUD != null) abilityHUD.SetAbility(what, ability_level[what]);
        }
        if (what == SupplyId)
        {
            bul.PlayerHealth = Mathf.Min(bul.PlayerMaxHealth, bul.PlayerHealth + bul.PlayerMaxHealth * 0.3f);
            Coin c = FindFirstObjectByType<Coin>();
            if (c != null) c.AddCoin(10);
            return;
        }
        if (KitApply(what)) return;
        if (what == 0) bul.pene++;
        if (what == 1)
        {
            bul.bonusCoin++;
            if (ability_level[1] >= CoinMaxLevel) ability_selected[1] = true;
        }
        if (what == 2)
        {
            // 너무 쉬워지지 않도록 최소 5회까지만 줄어듦
            skill.SetMax(Mathf.Max(MinSkillPoint, Mathf.RoundToInt(skill.MaxSkillPoint * 0.8f)));
            if (skill.MaxSkillPoint <= MinSkillPoint) ability_selected[2] = true;
        }
        if (what == 3)
        {
            bul.Skill_setTime *= 0.8f;
            if (ability_level[3] >= GlareMaxLevel) ability_selected[3] = true;
        }
        if (what == 4)
        {
            lv.bonusEXP += 0.1f;
            if (ability_level[4] >= ExpMaxLevel) ability_selected[4] = true;
        }
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
            // 최대 체력이 끝없이 불어나지 않게
            if (ability_level[8] >= HeartMaxLevel) ability_selected[8] = true;
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

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class bossbar : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject bar;
    public GameObject backBar;
    public int NowHealth;
    public int MaxHealth;

    public float MaxBarX = 1333f;

    public float BarX;
    public float BarY = 119.2869f;

    public bool bossSpawn = false;
    public int bossKind = 0;            // 0 = 리치 왕, 1 = 지옥의 군주, 2 = 킹 슬라임 (보스가 매 프레임 알려줌)

    TMP_Text nameText;
    RectTransform barRect;
    float nextNameSearch;
    int shownKind = -1;
    Loc.Lang shownLang;

    public static string BossName(int kind) => kind switch
    {
        1 => Loc.T("지옥의 군주"),
        2 => Loc.T("킹 슬라임"),
        _ => Loc.T("리치 왕"),
    };
    void Start()
    {
        // 씬에 배치된 보스바 크기를 최대치로 사용
        RectTransform barRect = bar.GetComponent<RectTransform>();
        MaxBarX = barRect.sizeDelta.x;
        BarY = barRect.sizeDelta.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (!bossSpawn)
        {
            backBar.SetActive(false);
            ShowExtraBars(0);
            return;
        }
        backBar.SetActive(true);
        UpdateName();
        if (barRect == null) barRect = bar.GetComponent<RectTransform>();

        // 킹 슬라임이 갈라진 뒤에는 슬라임마다 따로 보스 체력바 (첫 줄 + 아래로 쌓음)
        if (bossKind == 2)
        {
            bosss.CollectLiveSlimes(liveSlimes);
            bool split = liveSlimes.Exists(s => s.slimeGen >= 2);
            if (split && liveSlimes.Count > 0)
            {
                SetBar(barRect, nameText, liveSlimes[0], 0);
                ShowExtraBars(liveSlimes.Count - 1);
                for (int i = 1; i < liveSlimes.Count; i++) SetBar(extraFills[i - 1], extraNames[i - 1], liveSlimes[i], i);
                wasMulti = true;
                return;
            }
        }
        // 슬라임마다 그리던 것에서 돌아오면 첫 줄 이름을 다시 씀
        if (wasMulti) { wasMulti = false; shownKind = -1; }
        ShowExtraBars(0);
        if (MaxHealth <= 0) return;
        BarX = Mathf.Clamp01((float)NowHealth / (float)MaxHealth) * MaxBarX;
        barRect.sizeDelta = new Vector2(BarX, BarY);
    }

    // ================================================================= 분열한 킹 슬라임: 슬라임마다 보스 체력바
    const float ExtraGap = 8f;
    bool wasMulti;
    readonly List<bosss> liveSlimes = new List<bosss>();
    readonly List<GameObject> extraBars = new List<GameObject>();
    readonly List<RectTransform> extraFills = new List<RectTransform>();
    readonly List<TMP_Text> extraNames = new List<TMP_Text>();

    void SetBar(RectTransform fill, TMP_Text label, bosss s, int index)
    {
        float ratio = s.setEnemyHP > 0 ? Mathf.Clamp01(s.EnemyHealth / s.setEnemyHP) : 0f;
        if (fill != null) fill.sizeDelta = new Vector2(ratio * MaxBarX, BarY);
        string n = BossName(2) + " " + (index + 1);
        if (label != null && label.text != n) label.text = n;
    }

    // 위쪽 보스 체력바(배경 · 채움 · 이름)를 복제해 아래로 쌓음
    void ShowExtraBars(int count)
    {
        while (extraBars.Count < count)
        {
            GameObject clone = Instantiate(backBar, backBar.transform.parent);
            clone.name = backBar.name + "_" + (extraBars.Count + 1);
            RectTransform main = backBar.GetComponent<RectTransform>();
            RectTransform r = clone.GetComponent<RectTransform>();
            r.anchoredPosition = main.anchoredPosition - new Vector2(0f, (main.sizeDelta.y + ExtraGap) * (extraBars.Count + 1));
            Transform fill = clone.transform.Find(bar.name);
            TMP_Text label = null;
            foreach (TMP_Text t in clone.GetComponentsInChildren<TMP_Text>(true))
                if (t.name == "BossName") { label = t; t.name = "BossNameExtra"; }    // 원래 이름 글자 찾기와 헷갈리지 않게
            extraBars.Add(clone);
            extraFills.Add(fill as RectTransform);
            extraNames.Add(label);
        }
        for (int i = 0; i < extraBars.Count; i++)
            if (extraBars[i] != null && extraBars[i].activeSelf != (i < count)) extraBars[i].SetActive(i < count);
    }

    // 가장 아래에 보이는 보스 체력바 (알림판을 그 아래로 내릴 때)
    public RectTransform LowestBar
    {
        get
        {
            for (int i = extraBars.Count - 1; i >= 0; i--)
                if (extraBars[i] != null && extraBars[i].activeInHierarchy) return extraBars[i].GetComponent<RectTransform>();
            return backBar != null ? backBar.GetComponent<RectTransform>() : null;
        }
    }

    // 체력바 위 이름을 지금 보스의 이름으로
    void UpdateName()
    {
        if (nameText == null)
        {
            // 이름 글자를 못 찾았을 때 매 프레임 씬 전체를 뒤지지 않게 1초에 한 번만
            if (Time.unscaledTime < nextNameSearch) return;
            nextNameSearch = Time.unscaledTime + 1f;
            foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "BossName") { nameText = t; break; }
            if (nameText == null) return;
        }
        if (shownKind == bossKind && shownLang == Loc.Current) return;
        shownKind = bossKind;
        shownLang = Loc.Current;
        nameText.text = BossName(bossKind);
    }
}

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
        if (!bossSpawn) backBar.SetActive(false);
        else
        {
            backBar.SetActive(true);
            UpdateName();
            if (MaxHealth <= 0) return;
            BarX = Mathf.Clamp01((float)NowHealth / (float)MaxHealth) * MaxBarX;
            if (barRect == null) barRect = bar.GetComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(BarX, BarY);
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

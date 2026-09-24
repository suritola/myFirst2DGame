using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 오른쪽 아래에 플레이어의 현재 능력치를 보여주는 패널
public class StatsHUD : MonoBehaviour
{
    [Header("아이콘 (공격력, 방어력, 공격 속도, 재장전, 이동 속도)")]
    public Sprite[] icons = new Sprite[5];

    [Header("모양")]
    public TMP_FontAsset font;
    public Material fontMaterial;
    public float rowHeight = 34f;
    public float padding = 26f;
    public float refreshInterval = 0.2f;

    [Header("열고 닫기 탭")]
    public Sprite tabSprite;
    public Sprite arrowSprite;          // 위쪽을 가리키는 화살표
    public Vector2 tabSize = new Vector2(64f, 38f);
    public bool startOpen = false;      // 처음엔 접힌 상태
    public float animSpeed = 8f;

    const string PrefKey = "StatsPanelOpen";

    bool open;
    float openAmount;                   // 0 = 접힘, 1 = 펼침
    RectTransform panelRect;
    RectTransform arrowRect;

    // 한국어 원문 (static 초기화 중엔 설정을 읽을 수 없으므로 쓸 때 번역)
    static readonly string[] Labels = { "공격력", "방어력", "공격 속도", "재장전", "이동 속도" };

    PlayerController player;
    TextMeshProUGUI[] values;
    float baseSpeed;
    float timer;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player != null) baseSpeed = player.speed;

        values = new TextMeshProUGUI[Labels.Length];
        for (int i = 0; i < Labels.Length; i++) BuildRow(i);

        panelRect = GetComponent<RectTransform>();
        BuildTab();

        open = PlayerPrefs.GetInt(PrefKey, startOpen ? 1 : 0) == 1;
        openAmount = open ? 1f : 0f;
        ApplyOpenAmount();

        Refresh();
    }

    // 패널 바로 아래(탄약 패널 위)에 붙는 화살표 버튼
    void BuildTab()
    {
        GameObject tab = new GameObject("StatsToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform tabRect = tab.GetComponent<RectTransform>();
        tabRect.SetParent(panelRect.parent, false);
        tabRect.SetSiblingIndex(panelRect.GetSiblingIndex() + 1);
        tabRect.anchorMin = panelRect.anchorMin;
        tabRect.anchorMax = panelRect.anchorMax;
        tabRect.pivot = new Vector2(1f, 0f);
        tabRect.sizeDelta = tabSize;
        tabRect.anchoredPosition = panelRect.anchoredPosition - new Vector2(0f, tabSize.y + 4f);

        Image bg = tab.GetComponent<Image>();
        bg.sprite = tabSprite;
        bg.type = Image.Type.Sliced;

        Button button = tab.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.9f, 0.62f);
        colors.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(Toggle);

        TooltipTrigger tip = tab.AddComponent<TooltipTrigger>();
        tip.title = Loc.T("능력치");
        tip.body = Loc.T("눌러서 능력치 창을 열고 닫습니다.");

        GameObject arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.SetParent(tabRect, false);
        arrowRect.sizeDelta = new Vector2(tabSize.y - 6f, tabSize.y - 6f);
        Image arrowImage = arrow.GetComponent<Image>();
        arrowImage.sprite = arrowSprite;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;
    }

    public void Toggle()
    {
        open = !open;
        PlayerPrefs.SetInt(PrefKey, open ? 1 : 0);
        // 버튼이 선택된 채로 남아 색이 고정되지 않도록
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    // 패널은 아래쪽 기준으로 세로로 접힘
    void ApplyOpenAmount()
    {
        float eased = openAmount * openAmount * (3f - 2f * openAmount);
        panelRect.localScale = new Vector3(1f, eased, 1f);
        // 열려 있으면 ▼(접기), 닫혀 있으면 ▲(펼치기)
        if (arrowRect != null) arrowRect.localEulerAngles = new Vector3(0f, 0f, open ? 180f : 0f);
    }

    void BuildRow(int i)
    {
        float y = -padding - i * rowHeight - rowHeight * 0.5f;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.SetParent(transform, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.sizeDelta = new Vector2(rowHeight - 4f, rowHeight - 4f);
        iconRect.anchoredPosition = new Vector2(padding + rowHeight * 0.5f, y);
        Image icon = iconGo.GetComponent<Image>();
        icon.sprite = i < icons.Length ? icons[i] : null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        MakeText("Label", Loc.T(Labels[i]), new Color(0.92f, 0.88f, 0.80f), TextAlignmentOptions.Left,
                 padding + rowHeight + 8f, y);
        values[i] = MakeText("Value", "", new Color(0.96f, 0.83f, 0.47f), TextAlignmentOptions.Right,
                             padding, y);
    }

    TextMeshProUGUI MakeText(string name, string text, Color color, TextAlignmentOptions align, float inset, float y)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // 왼쪽 글자는 아이콘 오른쪽부터, 오른쪽 값은 패널 오른쪽 여백까지
        bool left = align == TextAlignmentOptions.Left;
        rect.offsetMin = new Vector2(left ? inset : padding, 0f);
        rect.offsetMax = new Vector2(-padding, 0f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, rowHeight);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontSize = 24f;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    void Update()
    {
        float target = open ? 1f : 0f;
        if (!Mathf.Approximately(openAmount, target))
        {
            openAmount = Mathf.MoveTowards(openAmount, target, Time.unscaledDeltaTime * animSpeed);
            ApplyOpenAmount();
        }

        if (!open) return;

        timer += Time.unscaledDeltaTime;
        if (timer < refreshInterval) return;
        timer = 0f;
        Refresh();
    }

    void Refresh()
    {
        if (player == null || values == null) return;

        // 공격력: 멀티샷이면 발당 피해 x 발사 수
        int shots = Mathf.Max(1, player.multiShot);
        values[0].text = shots > 1
            ? (player.damage * player.MultiShotDamageRate(shots)).ToString("0.##") + " x " + shots
            : player.damage.ToString("0.##");

        // 방어력: 받는 피해 감소율
        values[1].text = (player.def * 100f).ToString("0") + "%";

        // 공격 속도: 초당 발사 수
        values[2].text = (1f / Mathf.Max(0.01f, player.ShootSpeed)).ToString("0.0") + Loc.T("/초");

        values[3].text = player.reloadTime.ToString("0.0") + Loc.T("초");

        // 이동 속도: 시작 속도 대비
        values[4].text = baseSpeed > 0f ? (player.speed / baseSpeed * 100f).ToString("0") + "%" : player.speed.ToString("0.0");
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2장 상점의 "무기 강화" 창: 고른 특수 무기마다 피해 / 속도 / 특성을 코인으로 강화
// StageManager가 실행 중에 붙이고 Init으로 연결함 (씬에 따로 둘 필요 없음)
public class WeaponUpgradeShop : MonoBehaviour
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.6f, 0.56f, 0.62f);
    static readonly Color Warn = new Color(1f, 0.45f, 0.4f);

    // 단계별 가격: 기본 가격 x 증가율^단계
    static readonly int[] BasePrice = { 15, 15, 20, 30 };
    static readonly float[] PriceGrowth = { 1.5f, 1.5f, 1.6f, 1.8f };

    Shop shop;
    SpecialAbilities specials;
    SpecialTreeUI style;        // 스프라이트와 글꼴을 빌려 씀

    GameObject tab;
    TextMeshProUGUI tabText;
    GameObject panel;
    RectTransform rowsRoot;
    TextMeshProUGUI coinText;
    float warnUntil;

    public static int Price(int stat, int level) => Mathf.CeilToInt(BasePrice[stat] * Mathf.Pow(PriceGrowth[stat], level));

    public void Init(Shop shop, SpecialAbilities specials, SpecialTreeUI style)
    {
        this.shop = shop;
        this.specials = specials;
        this.style = style;
        if (shop == null || shop.shopPanel == null || specials == null || style == null)
        {
            enabled = false;
            return;
        }
        Build();
    }

    void Update()
    {
        bool shopOpen = shop.shopPanel.activeInHierarchy;
        bool available = StageManager.Instance != null && StageManager.Instance.CurrentStage >= 1 && specials.Weapons.Count > 0;

        // 상점을 닫으면 다음에 열 때 기본 상점부터
        if (!shopOpen && panel.activeSelf) panel.SetActive(false);
        bool showTab = shopOpen && available && !panel.activeSelf;
        if (tab.activeSelf != showTab) tab.SetActive(showTab);

        // 탭이 은은하게 반짝임 (상점은 시간이 멈춘 상태라 실제 시간 사용)
        if (showTab) tabText.color = Color.Lerp(Gold, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));

        if (panel.activeSelf) UpdateCoinText();
    }

    // ================================================================= layout
    void Build()
    {
        RectTransform shopRoot = (RectTransform)shop.shopPanel.transform;

        // 상점 오른쪽 위 탭
        tab = NewButton(shopRoot, "WeaponUpgradeTab", new Vector2(430f, 330f), new Vector2(230f, 62f), style.headerSprite, OpenPanel);
        tabText = Text((RectTransform)tab.transform, "무기 강화", 26f, Gold, Vector2.zero, new Vector2(210f, 50f), TextAlignmentOptions.Center);
        tab.SetActive(false);

        // 기본 상점 위를 덮는 창
        panel = new GameObject("WeaponUpgradePanel", typeof(RectTransform), typeof(Image));
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.SetParent(shopRoot, false);
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(1180f, 800f);
        pr.anchoredPosition = new Vector2(0f, -10f);
        Image bg = panel.GetComponent<Image>();
        bg.sprite = style.panelSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = true;            // 아래 상점 버튼이 눌리지 않게

        Text(pr, "무기 강화", 44f, Gold, new Vector2(0f, 330f), new Vector2(600f, 60f), TextAlignmentOptions.Center);
        coinText = Text(pr, "", 28f, Parch, new Vector2(0f, 272f), new Vector2(700f, 40f), TextAlignmentOptions.Center);
        Text(pr, "무기마다 따로 강화됩니다 (권총 강화와 별개) · 피해 +15% · 연사 +10% · 탄창 +25% · 특성은 무기마다 다름", 20f, Dim,
             new Vector2(0f, -340f), new Vector2(1000f, 32f), TextAlignmentOptions.Center);

        GameObject back = NewButton(pr, "Back", new Vector2(430f, 330f), new Vector2(230f, 62f), style.headerSprite, ClosePanel);
        Text((RectTransform)back.transform, "상점으로", 24f, Parch, Vector2.zero, new Vector2(210f, 50f), TextAlignmentOptions.Center);

        GameObject rows = new GameObject("Rows", typeof(RectTransform));
        rowsRoot = rows.GetComponent<RectTransform>();
        rowsRoot.SetParent(pr, false);
        rowsRoot.anchorMin = rowsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        rowsRoot.sizeDelta = Vector2.zero;

        panel.SetActive(false);
    }

    void OpenPanel()
    {
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        warnUntil = 0f;
        Refresh();
    }

    void ClosePanel()
    {
        panel.SetActive(false);
        TooltipUI.Hide();
    }

    // 무기 줄을 다시 그림 (구매할 때마다)
    void Refresh()
    {
        for (int i = rowsRoot.childCount - 1; i >= 0; i--) Destroy(rowsRoot.GetChild(i).gameObject);

        IReadOnlyList<int> weapons = specials.Weapons;
        float rowHeight = Mathf.Min(100f, 520f / Mathf.Max(1, weapons.Count));
        const float top = 225f;

        for (int i = 0; i < weapons.Count; i++)
        {
            int id = weapons[i];
            SpecialDef def = specials.abilities[id];
            float y = top - rowHeight * (i + 0.5f);
            float h = rowHeight - 12f;

            // 아이콘
            GameObject frame = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform fr = frame.GetComponent<RectTransform>();
            fr.SetParent(rowsRoot, false);
            fr.sizeDelta = new Vector2(h, h);
            fr.anchoredPosition = new Vector2(-500f, y);
            Image fimg = frame.GetComponent<Image>();
            fimg.sprite = style.nodeSprite;
            fimg.type = Image.Type.Sliced;
            fimg.raycastTarget = false;
            GameObject icon = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
            RectTransform ir = icon.GetComponent<RectTransform>();
            ir.SetParent(fr, false);
            ir.sizeDelta = new Vector2(h - 20f, h - 20f);
            Image iimg = icon.GetComponent<Image>();
            iimg.sprite = def.icon;
            iimg.preserveAspect = true;
            iimg.raycastTarget = false;

            // 이름 (진화하면 +)
            Text(rowsRoot, def.name + (specials.IsEvolved(id) ? "+" : ""), 24f, Gold, new Vector2(-335f, y), new Vector2(200f, h), TextAlignmentOptions.Left);

            // 강화 버튼 3개
            string[] names = { "피해", "연사", "탄창", SpecialAbilities.TraitName(id) };
            string[] steps = { "+15%", "+10%", "+25%", SpecialAbilities.TraitStep(id) };
            for (int stat = 0; stat < 4; stat++)
            {
                int s = stat;
                int lv = specials.WeaponLevel(id, s);
                int max = SpecialAbilities.WeaponStatMax[s];
                // 화염 방사기와 낫은 탄창이 없음
                bool none = s == SpecialAbilities.StatMag && !SpecialAbilities.UsesAmmo(id);
                bool maxed = lv >= max || none;
                GameObject b = NewButton(rowsRoot, "Upgrade", new Vector2(-130f + 190f * s, y), new Vector2(180f, h), style.headerSprite, () => Buy(id, s));
                b.GetComponent<Button>().interactable = !maxed;
                string label = none ? names[s] + "\n<color=#a39aa8>해당 없음</color>"
                    : names[s] + "  Lv " + lv + "/" + max + "\n"
                      + (maxed ? "<color=#a39aa8>최대</color>" : steps[s] + " · <color=#f5d478>" + Price(s, lv) + "</color>");
                Text((RectTransform)b.transform, label, 20f, maxed ? Dim : Parch, Vector2.zero, new Vector2(166f, h - 8f), TextAlignmentOptions.Center);
            }
        }
        UpdateCoinText();
    }

    void Buy(int id, int stat)
    {
        int lv = specials.WeaponLevel(id, stat);
        if (lv >= SpecialAbilities.WeaponStatMax[stat]) return;
        if (stat == SpecialAbilities.StatMag && !SpecialAbilities.UsesAmmo(id)) return;

        int price = Price(stat, lv);
        Coin coin = FindFirstObjectByType<Coin>();
        if (coin == null || coin.coins < price)
        {
            warnUntil = Time.unscaledTime + 1.2f;
            return;
        }

        coin.SubCoin(price);
        specials.UpgradeWeapon(id, stat);
        shop.UpdateShopText();
        Refresh();
    }

    void UpdateCoinText()
    {
        Coin coin = FindFirstObjectByType<Coin>();
        int coins = coin != null ? coin.coins : 0;
        bool warn = Time.unscaledTime < warnUntil;
        coinText.color = warn ? Warn : Parch;
        coinText.text = warn ? "코인이 부족합니다 (" + coins + ")" : "코인 : " + coins;
    }

    // ================================================================= helpers
    GameObject NewButton(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        Button b = go.GetComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.45f, 0.42f, 0.48f, 0.8f);
        b.colors = cb;
        b.onClick.AddListener(onClick);
        return go;
    }

    TextMeshProUGUI Text(RectTransform parent, string text, float size, Color color, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = box;
        r.anchoredPosition = pos;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (style.font != null) t.font = style.font;
        if (style.fontMaterial != null) t.fontSharedMaterial = style.fontMaterial;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12f;
        t.fontSizeMax = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }
}

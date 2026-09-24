using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점의 "스킬 강화" 창: 기본 권총과 보유한 특수 무기마다 필살기(우클릭 스킬)를 강화
// StageManager가 실행 중에 붙이고 Init으로 연결함
public class SkillUpgradeShop : MonoBehaviour
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.6f, 0.56f, 0.62f);
    static readonly Color Warn = new Color(1f, 0.45f, 0.4f);

    // 단계별 가격: 기본 가격 x 증가율^단계 (0 = 위력, 1 = 특성)
    static readonly int[] BasePrice = { 60, 90 };
    static readonly float[] PriceGrowth = { 1.5f, 1.7f };

    Shop shop;
    SpecialAbilities specials;
    SpecialTreeUI style;

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
        if (!shopOpen && panel.activeSelf) panel.SetActive(false);
        bool showTab = shopOpen && !panel.activeSelf;
        if (tab.activeSelf != showTab) tab.SetActive(showTab);
        if (showTab) tabText.color = Color.Lerp(Gold, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f + 1.5f));
        if (panel.activeSelf) UpdateCoinText();
    }

    // ================================================================= layout
    void Build()
    {
        RectTransform shopRoot = (RectTransform)shop.shopPanel.transform;

        // 상점 왼쪽 위 탭 (오른쪽 위는 무기 강화)
        tab = NewButton(shopRoot, "SkillUpgradeTab", new Vector2(-430f, 330f), new Vector2(230f, 62f), style.headerSprite, OpenPanel);
        tabText = Text((RectTransform)tab.transform, "스킬 강화", 26f, Gold, Vector2.zero, new Vector2(210f, 50f), TextAlignmentOptions.Center);
        tab.SetActive(false);

        panel = new GameObject("SkillUpgradePanel", typeof(RectTransform), typeof(Image));
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.SetParent(shopRoot, false);
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(1180f, 800f);
        pr.anchoredPosition = new Vector2(0f, -10f);
        Image bg = panel.GetComponent<Image>();
        bg.sprite = style.panelSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = true;

        Text(pr, "스킬 강화", 44f, Gold, new Vector2(0f, 330f), new Vector2(600f, 60f), TextAlignmentOptions.Center);
        coinText = Text(pr, "", 28f, Parch, new Vector2(0f, 272f), new Vector2(700f, 40f), TextAlignmentOptions.Center);
        Text(pr, "우클릭 필살기를 무기마다 따로 강화합니다 · 위력 +20% · 특성은 무기마다 다름", 20f, Dim,
             new Vector2(0f, -340f), new Vector2(1000f, 32f), TextAlignmentOptions.Center);

        GameObject back = NewButton(pr, "Back", new Vector2(-430f, 330f), new Vector2(230f, 62f), style.headerSprite, ClosePanel);
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

    // 기본 권총 + 보유 무기마다 한 줄
    void Refresh()
    {
        for (int i = rowsRoot.childCount - 1; i >= 0; i--) Destroy(rowsRoot.GetChild(i).gameObject);

        List<int> list = new List<int> { SpecialAbilities.PistolUlt };
        list.AddRange(specials.Weapons);
        float rowHeight = Mathf.Min(100f, 520f / list.Count);
        const float top = 225f;

        for (int i = 0; i < list.Count; i++)
        {
            int id = list[i];
            float y = top - rowHeight * (i + 0.5f);
            float h = rowHeight - 12f;

            // 아이콘 (권총은 조준경)
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
            Sprite[] reticle = Fx.Frames("fx_reticle");
            iimg.sprite = id == SpecialAbilities.PistolUlt ? (reticle.Length > 0 ? reticle[0] : null) : specials.abilities[id].icon;
            iimg.color = id == SpecialAbilities.PistolUlt ? Gold : Color.white;
            iimg.preserveAspect = true;
            iimg.raycastTarget = false;

            string weaponName = id == SpecialAbilities.PistolUlt ? "기본 권총" : specials.abilities[id].name;
            Text(rowsRoot, weaponName + "\n<color=#f5d478><size=80%>" + SpecialAbilities.UltName(id) + "</size></color>", 24f, Parch,
                 new Vector2(-300f, y), new Vector2(260f, h), TextAlignmentOptions.Left);

            string[] names = { "위력", SpecialAbilities.UltTraitName(id) };
            string[] steps = { "+20%", SpecialAbilities.UltTraitStep(id) };
            for (int stat = 0; stat < 2; stat++)
            {
                int s = stat;
                int lv = specials.UltLevel(id, s);
                int max = SpecialAbilities.UltStatMax[s];
                bool maxed = lv >= max;
                GameObject b = NewButton(rowsRoot, "Upgrade", new Vector2(100f + 290f * s, y), new Vector2(270f, h), style.headerSprite, () => Buy(id, s));
                b.GetComponent<Button>().interactable = !maxed;
                string label = names[s] + "  Lv " + lv + "/" + max + "\n"
                    + (maxed ? "<color=#a39aa8>최대</color>" : steps[s] + " · <color=#f5d478>" + Price(s, lv) + " 코인</color>");
                Text((RectTransform)b.transform, label, 21f, maxed ? Dim : Parch, Vector2.zero, new Vector2(250f, h - 8f), TextAlignmentOptions.Center);
            }
        }
        UpdateCoinText();
    }

    void Buy(int id, int stat)
    {
        int lv = specials.UltLevel(id, stat);
        if (lv >= SpecialAbilities.UltStatMax[stat]) return;
        int price = Price(stat, lv);
        Coin coin = FindFirstObjectByType<Coin>();
        if (coin == null || coin.coins < price)
        {
            warnUntil = Time.unscaledTime + 1.2f;
            return;
        }
        coin.SubCoin(price);
        specials.UpgradeUlt(id, stat);
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

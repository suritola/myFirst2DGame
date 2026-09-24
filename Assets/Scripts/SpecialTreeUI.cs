using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지옥의 문 특수 능력 선택: 뿌리에서 무기 / 패시브 / 스킬 세 갈래로 뻗는 스킬 트리
public class SpecialTreeUI : MonoBehaviour
{
    [Header("모양")]
    public Sprite nodeSprite;       // 능력 노드 틀
    public Sprite headerSprite;     // 갈래 이름판
    public Sprite panelSprite;      // 아래 설명창
    public Sprite rootIcon;
    public TMP_FontAsset font;
    public Material fontMaterial;

    [Header("포인트")]
    public int points = 3;              // 고를 수 있는 능력 수

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color LineColor = new Color(0.78f, 0.55f, 0.25f, 0.8f);
    static readonly Color Selected = new Color(1f, 0.85f, 0.4f);

    SpecialAbilities specials;
    System.Action<int[]> onConfirm;
    bool built;
    readonly List<int> picked = new List<int>();
    int viewing = -1;                   // 설명창에 보이는 노드

    readonly List<Image> nodeFrames = new List<Image>();
    TextMeshProUGUI detailName, detailKind, detailText;
    Button confirm;
    TextMeshProUGUI confirmText;

    public void Open(SpecialAbilities specials, System.Action<int[]> onConfirm)
    {
        this.specials = specials;
        this.onConfirm = onConfirm;
        if (!built) Build();
        picked.Clear();
        View(-1, null);
        gameObject.SetActive(true);
    }

    // ================================================================= layout
    void Build()
    {
        built = true;
        RectTransform root = (RectTransform)transform;

        // 뿌리
        Vector2 rootPos = new Vector2(0f, 255f);
        Node(root, rootPos, 104f, rootIcon, "지옥의 문", -1);

        // 갈래 (x = 줄기 위치, 두 열의 x)
        var branches = new[]
        {
            (kind: SpecialKind.Weapon, name: "무기  [Q] 교체", x: -630f, cols: new[] { -760f, -500f }),
            (kind: SpecialKind.Passive, name: "패시브  항상 적용", x: 0f, cols: new[] { -130f, 130f }),
            (kind: SpecialKind.Skill, name: "스킬  [E] 사용", x: 630f, cols: new[] { 500f, 760f }),
        };
        float[] rows = { 60f, -55f, -170f, -285f };
        const float headerY = 160f;

        foreach (var br in branches)
        {
            // 뿌리 → 갈래 이름판
            Line(root, rootPos + new Vector2(0f, -52f), new Vector2(0f, 205f));
            Line(root, new Vector2(0f, 205f), new Vector2(br.x, 205f));
            Line(root, new Vector2(br.x, 205f), new Vector2(br.x, headerY + 28f));
            Header(root, new Vector2(br.x, headerY), br.name);

            List<int> ids = new List<int>();
            for (int i = 0; i < specials.abilities.Length; i++)
                if (specials.abilities[i].kind == br.kind) ids.Add(i);

            int rowCount = (ids.Count + 1) / 2;
            // 줄기
            Line(root, new Vector2(br.x, headerY - 28f), new Vector2(br.x, rows[rowCount - 1]));

            for (int n = 0; n < ids.Count; n++)
            {
                float y = rows[n / 2];
                float x = br.cols[n % 2];
                Line(root, new Vector2(br.x, y), new Vector2(x, y));
                SpecialDef def = specials.abilities[ids[n]];
                Node(root, new Vector2(x, y), 88f, def.icon, def.name, ids[n]);
            }
        }

        BuildDetail(root);
    }

    void Line(RectTransform parent, Vector2 a, Vector2 b)
    {
        GameObject go = new GameObject("Line", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.SetSiblingIndex(3);                       // 노드보다 뒤, 배경보다 앞
        Vector2 d = b - a;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(d.magnitude + 4f, 4f);
        r.anchoredPosition = (a + b) * 0.5f;
        r.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        Image img = go.GetComponent<Image>();
        img.color = LineColor;
        img.raycastTarget = false;
    }

    void Header(RectTransform parent, Vector2 pos, string text)
    {
        GameObject go = new GameObject("Branch", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(250f, 56f);
        r.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.sprite = headerSprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        Text(r, text, 24f, Gold, Vector2.zero, new Vector2(240f, 50f), TextAlignmentOptions.Center);
    }

    void Node(RectTransform parent, Vector2 pos, float size, Sprite icon, string label, int id)
    {
        GameObject go = new GameObject(id < 0 ? "Root" : "Node_" + id, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(size, size);
        r.anchoredPosition = pos;
        Image frame = go.GetComponent<Image>();
        frame.sprite = nodeSprite;
        frame.type = Image.Type.Sliced;

        GameObject ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform ir = ic.GetComponent<RectTransform>();
        ir.SetParent(r, false);
        ir.sizeDelta = new Vector2(size - 28f, size - 28f);
        Image iconImg = ic.GetComponent<Image>();
        iconImg.sprite = icon;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // 뿌리는 제목 배너와 이름이 같아서 글자를 따로 달지 않음
        if (id < 0)
        {
            frame.raycastTarget = false;
            return;
        }

        Text(r, label, 20f, Parch, new Vector2(0f, -size * 0.5f - 14f), new Vector2(230f, 28f), TextAlignmentOptions.Center);

        SpecialDef def = specials.abilities[id];
        Button b = go.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        b.onClick.AddListener(() => Toggle(id));

        TooltipTrigger tip = go.AddComponent<TooltipTrigger>();
        tip.title = def.name + "  · " + KindName(def.kind);
        tip.body = def.description;

        while (nodeFrames.Count <= id) nodeFrames.Add(null);
        nodeFrames[id] = frame;
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
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
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

    void BuildDetail(RectTransform root)
    {
        GameObject go = new GameObject("Detail", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(root, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(1300f, 130f);
        r.anchoredPosition = new Vector2(0f, -420f);
        Image img = go.GetComponent<Image>();
        img.sprite = panelSprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;

        detailName = Text(r, "", 34f, Gold, new Vector2(-470f, 20f), new Vector2(300f, 44f), TextAlignmentOptions.Left);
        detailKind = Text(r, "", 22f, new Color(1f, 0.72f, 0.55f), new Vector2(-470f, -24f), new Vector2(300f, 30f), TextAlignmentOptions.Left);
        detailText = Text(r, "", 24f, Parch, new Vector2(20f, 0f), new Vector2(660f, 100f), TextAlignmentOptions.Left);

        GameObject btn = new GameObject("Confirm", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform brt = btn.GetComponent<RectTransform>();
        brt.SetParent(r, false);
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(230f, 76f);
        brt.anchoredPosition = new Vector2(510f, 0f);
        Image bimg = btn.GetComponent<Image>();
        bimg.sprite = headerSprite;
        bimg.type = Image.Type.Sliced;
        confirm = btn.GetComponent<Button>();
        ColorBlock cb = confirm.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.disabledColor = new Color(0.45f, 0.42f, 0.48f, 0.8f);
        confirm.colors = cb;
        confirm.onClick.AddListener(() => { if (picked.Count >= points) onConfirm?.Invoke(picked.ToArray()); });
        confirmText = Text(brt, "", 26f, Gold, Vector2.zero, new Vector2(210f, 60f), TextAlignmentOptions.Center);
    }

    // 노드를 누르면 선택/해제 (포인트 안에서)
    void Toggle(int id)
    {
        string note = null;
        if (picked.Contains(id))
        {
            picked.Remove(id);
            note = "선택을 취소했습니다.";
        }
        else if (picked.Count < points)
        {
            picked.Add(id);
        }
        else
        {
            note = "포인트를 모두 썼습니다. 다른 능력을 먼저 취소하세요.";
        }
        View(id, note);
    }

    void View(int id, string note)
    {
        viewing = id;
        for (int i = 0; i < nodeFrames.Count; i++)
            if (nodeFrames[i] != null) nodeFrames[i].color = picked.Contains(i) ? Selected : Color.white;

        int left = points - picked.Count;
        bool ready = left <= 0;
        confirm.interactable = ready;
        confirmText.color = ready ? Gold : new Color(0.6f, 0.56f, 0.62f);
        confirmText.text = ready ? "선택 완료" : "남은 포인트 " + left;

        if (id < 0)
        {
            detailName.text = "능력 " + points + "개를 고르세요";
            detailKind.text = "포인트 " + points + "개 · 노드를 눌러 선택";
            detailText.text = "무기는 Q로 기본 권총과 번갈아 쓰고, 스킬은 고른 순서대로 E · F · Space에 배정됩니다. 다시 누르면 선택이 취소됩니다.";
            return;
        }

        SpecialDef def = specials.abilities[id];
        detailName.text = def.name;
        detailKind.text = KindName(def.kind) + (picked.Contains(id) ? "  (선택됨)" : "");
        detailText.text = note != null ? def.description + "\n<color=#ff9d8a>" + note + "</color>" : def.description;
    }

    static string KindName(SpecialKind k) => k == SpecialKind.Weapon ? "무기 · Q로 교체" : k == SpecialKind.Skill ? "스킬 · E/F/Space" : "패시브 · 항상 적용";
}

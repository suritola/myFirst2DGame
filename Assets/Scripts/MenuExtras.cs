using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 메인 메뉴에 튜토리얼 · 설정 버튼, 게임 중 ESC 메뉴에 설정 버튼을 붙임
// 기존 버튼을 복제해서 같은 모양으로 만듦 (씬을 따로 고치지 않음)
public static class MenuExtras
{
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == "MainMenu") InstallMainMenu();
        else if (scene.name == "GameScene") InstallEsc();
    }

    static void InstallMainMenu()
    {
        GameObject start = GameObject.Find("GameStartButton");
        GameObject exit = GameObject.Find("ExitButton");
        if (start == null || GameObject.Find("TutorialButton") != null) return;

        RectTransform sr = (RectTransform)start.transform;
        GameObject tutorial = UIKit.CloneButton(start, "TutorialButton", "튜토리얼", () => TutorialUI.Open(sr.root));
        GameObject settings = UIKit.CloneButton(start, "SettingsButton", "설정", () => SettingsUI.Open(sr.root));

        // 세로로 다시 배치: 시작 · 튜토리얼 · 설정 · 종료
        float[] ys = { -20f, -150f, -280f, -410f };
        GameObject[] order = { start, tutorial, settings, exit };
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] == null) continue;
            RectTransform r = (RectTransform)order[i].transform;
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, ys[i]);
            r.sizeDelta = new Vector2(r.sizeDelta.x, 112f);
        }
    }

    static void InstallEsc()
    {
        ESCmenu esc = Object.FindFirstObjectByType<ESCmenu>(FindObjectsInactive.Include);
        if (esc == null || esc.escMenu == null) return;
        if (esc.escMenu.transform.Find("SettingsButton") != null) return;
        Button[] buttons = esc.escMenu.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0) return;

        // 가장 아래 버튼 밑에 설정 버튼
        Button lowest = buttons[0];
        foreach (Button b in buttons)
            if (((RectTransform)b.transform).anchoredPosition.y < ((RectTransform)lowest.transform).anchoredPosition.y) lowest = b;
        RectTransform lr = (RectTransform)lowest.transform;
        GameObject settings = UIKit.CloneButton(lowest.gameObject, "SettingsButton", "설정", () => SettingsUI.Open(esc.escMenu.transform.root));
        RectTransform r = (RectTransform)settings.transform;
        r.anchoredPosition = lr.anchoredPosition - new Vector2(0f, lr.sizeDelta.y + 24f);
    }
}

// ===================================================================== runtime UI helpers
public static class UIKit
{
    public static TMP_FontAsset Font;
    public static Material FontMaterial;
    public static Sprite ButtonSprite;

    public static GameObject CloneButton(GameObject template, string name, string label, UnityAction onClick)
    {
        GameObject go = Object.Instantiate(template, template.transform.parent);
        go.name = name;
        Button b = go.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();       // 복제된 원래 기능은 지움
        b.onClick.AddListener(onClick);
        TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);
        if (t != null)
        {
            t.text = Loc.T(label);
            Remember(t, label);
            if (Font == null) { Font = t.font; FontMaterial = t.fontSharedMaterial; }
        }
        Image img = go.GetComponent<Image>();
        if (img != null && ButtonSprite == null) ButtonSprite = img.sprite;
        return go;
    }

    // 언어가 바뀌면 다시 번역할 글자 (한국어 원문)
    static readonly Dictionary<TMP_Text, string> labels = new Dictionary<TMP_Text, string>();
    static bool hooked;
    public static void Remember(TMP_Text t, string ko)
    {
        labels[t] = ko;
        if (hooked) return;
        hooked = true;
        Loc.Changed += () =>
        {
            List<TMP_Text> dead = new List<TMP_Text>();
            foreach (KeyValuePair<TMP_Text, string> kv in labels)
            {
                if (kv.Key == null) dead.Add(kv.Key);
                else kv.Key.text = Loc.T(kv.Value);
            }
            foreach (TMP_Text d in dead) labels.Remove(d);
        };
    }

    static void EnsureStyle()
    {
        if (Font != null && ButtonSprite != null) return;
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (Font == null && t.font != null) { Font = t.font; FontMaterial = t.fontSharedMaterial; }
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Image img = b.GetComponent<Image>();
            if (ButtonSprite == null && img != null && img.sprite != null) ButtonSprite = img.sprite;
        }
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return r;
    }

    // 화면 전체를 어둡게 덮는 판 + 가운데 창
    public static RectTransform Modal(Transform root, string name, Vector2 size, out GameObject blocker)
    {
        EnsureStyle();
        Canvas canvas = root.GetComponentInChildren<Canvas>();
        Transform parent = canvas != null ? canvas.transform : root;
        blocker = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform br = blocker.GetComponent<RectTransform>();
        br.SetParent(parent, false);
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
        br.offsetMin = br.offsetMax = Vector2.zero;
        br.SetAsLastSibling();
        blocker.GetComponent<Image>().color = new Color(0.03f, 0.02f, 0.05f, 0.82f);

        RectTransform win = Rect("Window", br, Vector2.zero, size);
        Image wi = win.gameObject.AddComponent<Image>();
        wi.sprite = ButtonSprite;
        wi.type = Image.Type.Sliced;
        wi.color = new Color(0.55f, 0.5f, 0.6f, 1f);
        RectTransform inner = Rect("Inner", win, Vector2.zero, size - new Vector2(24f, 24f));
        inner.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.07f, 0.12f, 0.97f);
        return inner;
    }

    public static TMP_Text Text(Transform parent, string ko, float size, Color color, Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform r = Rect("Text", parent, pos, box);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) t.font = Font;
        if (FontMaterial != null) t.fontSharedMaterial = FontMaterial;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12f;
        t.fontSizeMax = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.text = Loc.T(ko);
        if (!string.IsNullOrEmpty(ko)) Remember(t, ko);
        return t;
    }

    public static Button MakeButton(Transform parent, string ko, Vector2 pos, Vector2 size, UnityAction onClick, float fontSize = 26f)
    {
        RectTransform r = Rect("Button", parent, pos, size);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = ButtonSprite;
        img.type = Image.Type.Sliced;
        Button b = r.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        b.onClick.AddListener(onClick);
        Text(r, ko, fontSize, new Color(0.96f, 0.9f, 0.8f), Vector2.zero, size - new Vector2(20f, 12f));
        return b;
    }
}

// ===================================================================== settings
public static class SettingsUI
{
    static GameObject open;
    public static bool IsOpen => open != null;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);

    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "SettingsPanel", new Vector2(960f, 640f), out open);

        UIKit.Text(win, "설정", 52f, Gold, new Vector2(0f, 250f), new Vector2(600f, 70f));

        // 언어
        UIKit.Text(win, "언어", 30f, Parch, new Vector2(-330f, 130f), new Vector2(200f, 50f), TextAlignmentOptions.Left);
        List<Button> langButtons = new List<Button>();
        for (int i = 0; i < 4; i++)
        {
            int lang = i;
            Button b = UIKit.MakeButton(win, "", new Vector2(-110f + 170f * i, 130f), new Vector2(160f, 64f), () =>
            {
                GameSettings.Language = (Loc.Lang)lang;
                Highlight(langButtons);
            }, 26f);
            b.GetComponentInChildren<TMP_Text>().text = Loc.LangNames[i];     // 언어 이름은 각 언어로 그대로
            langButtons.Add(b);
        }
        Highlight(langButtons);

        // 볼륨 (0 ~ 100%)
        UIKit.Text(win, "볼륨", 30f, Parch, new Vector2(-330f, 0f), new Vector2(200f, 50f), TextAlignmentOptions.Left);
        TMP_Text percent = UIKit.Text(win, "", 30f, Gold, new Vector2(350f, 0f), new Vector2(120f, 50f));
        Slider slider = MakeSlider(win, new Vector2(40f, 0f), new Vector2(500f, 36f));
        slider.value = GameSettings.Volume;
        percent.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            GameSettings.Volume = v;
            percent.text = Mathf.RoundToInt(v * 100f) + "%";
        });

        UIKit.MakeButton(win, "닫기", new Vector2(0f, -230f), new Vector2(260f, 76f), Close, 30f);
    }

    static void Highlight(List<Button> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool on = i == (int)GameSettings.Language;
            buttons[i].GetComponent<Image>().color = on ? Gold : new Color(0.6f, 0.58f, 0.65f);
            buttons[i].transform.localScale = Vector3.one * (on ? 1.06f : 1f);
        }
    }

    static Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size)
    {
        RectTransform root = UIKit.Rect("VolumeSlider", parent, pos, size);
        Slider s = root.gameObject.AddComponent<Slider>();

        RectTransform bg = UIKit.Rect("Background", root, Vector2.zero, Vector2.zero);
        bg.anchorMin = new Vector2(0f, 0.3f); bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.22f, 0.3f);

        RectTransform area = UIKit.Rect("Fill Area", root, Vector2.zero, Vector2.zero);
        area.anchorMin = new Vector2(0f, 0.3f); area.anchorMax = new Vector2(1f, 0.7f);
        area.offsetMin = area.offsetMax = Vector2.zero;
        RectTransform fill = UIKit.Rect("Fill", area, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.75f, 0.3f);

        RectTransform handleArea = UIKit.Rect("Handle Slide Area", root, Vector2.zero, Vector2.zero);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(12f, 0f); handleArea.offsetMax = new Vector2(-12f, 0f);
        RectTransform handle = UIKit.Rect("Handle", handleArea, Vector2.zero, new Vector2(28f, 0f));
        handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(28f, 12f);
        Image hi = handle.gameObject.AddComponent<Image>();
        hi.sprite = UIKit.ButtonSprite;
        hi.type = Image.Type.Sliced;
        hi.color = Color.white;

        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = hi;
        s.minValue = 0f;
        s.maxValue = 1f;
        return s;
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}

// ===================================================================== tutorial
public static class TutorialUI
{
    static GameObject open;
    static int page;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Key = new Color(1f, 0.72f, 0.55f);

    // 페이지: 제목, 그림(Resources/FX), 줄들(키 / 설명)
    static readonly (string title, string icon, (string key, string text)[] rows)[] Pages =
    {
        ("기본 조작", "fx_keycap", new[]
        {
            ("W A S D", "이동"),
            ("좌클릭", "마우스 방향으로 사격"),
            ("R", "재장전"),
            ("Q", "무기 교체 (특수 무기를 얻은 뒤)"),
            ("ESC", "일시정지 · 설정"),
        }),
        ("필살기", "fx_reticle", new[]
        {
            ("스킬 게이지", "시간이 지나면 차고, 적을 처치하면 잠깐 더 빨리 찹니다"),
            ("우클릭", "게이지가 가득 차면 필살기를 씁니다"),
            ("조준형", "누르고 있으면 시간이 느려지며 적을 조준, 떼면 발동"),
            ("즉발형", "산탄총 · 저격총 · 쌍권총 · 화염 · 유탄은 누르는 즉시 발동"),
        }),
        ("성장", "fx_prompt", new[]
        {
            ("레벨업", "카드를 클릭해 고르고 Space로 확정"),
            ("상점 제단", "적 35마리마다 나타남 · 다가가서 Space로 열기"),
            ("상점", "능력치 · 무기 강화 · 스킬 강화"),
            ("코인", "적이 떨어뜨림 · 코인 자석 능력으로 끌어올 수 있음"),
        }),
        ("특수 능력", "fx_orb", new[]
        {
            ("지옥의 문", "1장 보스를 쓰러뜨리고 문에 들어가면 특수 능력 3개를 고릅니다"),
            ("무기 · 스킬 · 패시브", "무기는 Q로 교체, 스킬은 E · F · Space"),
            ("특수 강화 (T)", "중간 보스를 잡으면 포인트 · 새 능력을 배우거나 가진 능력을 진화"),
        }),
        ("적과 보스", "fx_warn", new[]
        {
            ("경고 표시", "붉은 원 · 선 · 머리 위 ! 가 보이면 그 자리를 피하세요"),
            ("중간 보스", "2장부터 단계가 오를 때마다 등장 · 특수 능력 포인트를 줌"),
            ("보스", "리치 왕 → 지옥의 군주 → 킹 슬라임"),
            ("특수 스킬", "보스는 체력이 절반 아래로 떨어지면 특수 스킬을 씁니다"),
        }),
        ("목표", "fx_rune", new[]
        {
            ("3개의 스테이지", "지하 묘역 → 불타는 지옥 → 초원"),
            ("문", "보스를 쓰러뜨리면 30초 안에 문으로 들어가세요"),
            ("행운을 빌어요!", "모든 보스를 쓰러뜨리면 승리합니다"),
        }),
    };

    static RectTransform content;
    static TMP_Text counter;
    static Button prev, next;

    public static void Open(Transform root)
    {
        if (open != null) return;
        page = 0;
        RectTransform win = UIKit.Modal(root, "TutorialPanel", new Vector2(1400f, 820f), out open);
        content = UIKit.Rect("Content", win, new Vector2(0f, 30f), new Vector2(1300f, 680f));
        counter = UIKit.Text(win, "", 26f, Parch, new Vector2(0f, -350f), new Vector2(200f, 40f));
        prev = UIKit.MakeButton(win, "이전", new Vector2(-420f, -350f), new Vector2(220f, 70f), () => Show(page - 1));
        next = UIKit.MakeButton(win, "다음", new Vector2(420f, -350f), new Vector2(220f, 70f), () =>
        {
            if (page >= Pages.Length - 1) Close();
            else Show(page + 1);
        });
        UIKit.MakeButton(win, "닫기", new Vector2(610f, 360f), new Vector2(140f, 56f), Close, 22f);
        Show(0);
    }

    static void Show(int p)
    {
        page = Mathf.Clamp(p, 0, Pages.Length - 1);
        for (int i = content.childCount - 1; i >= 0; i--) Object.Destroy(content.GetChild(i).gameObject);

        var pg = Pages[page];
        UIKit.Text(content, pg.title, 54f, Gold, new Vector2(0f, 290f), new Vector2(900f, 80f));

        // 그림
        Sprite[] frames = Fx.Frames(pg.icon);
        if (frames.Length > 0)
        {
            RectTransform ir = UIKit.Rect("Icon", content, new Vector2(-470f, 60f), new Vector2(220f, 220f));
            Image img = ir.gameObject.AddComponent<Image>();
            img.sprite = frames[0];
            img.preserveAspect = true;
            img.color = pg.icon == "fx_reticle" || pg.icon == "fx_rune" || pg.icon == "fx_orb" ? Gold : Color.white;
        }

        float y = 170f;
        foreach (var (key, text) in pg.rows)
        {
            UIKit.Text(content, key, 30f, Key, new Vector2(-180f, y), new Vector2(300f, 60f), TextAlignmentOptions.Left);
            UIKit.Text(content, text, 26f, Parch, new Vector2(260f, y), new Vector2(620f, 70f), TextAlignmentOptions.Left);
            y -= 95f;
        }

        counter.text = (page + 1) + " / " + Pages.Length;
        prev.interactable = page > 0;
        TMP_Text nt = next.GetComponentInChildren<TMP_Text>();
        string label = page >= Pages.Length - 1 ? "시작하기" : "다음";
        nt.text = Loc.T(label);
        UIKit.Remember(nt, label);
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}

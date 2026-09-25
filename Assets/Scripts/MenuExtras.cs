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
        GameObject codex = UIKit.CloneButton(start, "CodexButton", "도감", () => CodexUI.Open(sr.root));
        GameObject settings = UIKit.CloneButton(start, "SettingsButton", "설정", () => SettingsUI.Open(sr.root));

        // 세로로 다시 배치: 시작 · 튜토리얼 · 도감 · 설정 · 종료
        float[] ys = { 0f, -108f, -216f, -324f, -432f };
        GameObject[] order = { start, tutorial, codex, settings, exit };
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] == null) continue;
            RectTransform r = (RectTransform)order[i].transform;
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, ys[i]);
            r.sizeDelta = new Vector2(r.sizeDelta.x, 94f);
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
// 탭 3개: 일반(언어 · 볼륨) / 화면(모드 · 해상도 · 수직 동기화 · 흔들림 · 번쩍임) / 조작(키 바꾸기)
public static class SettingsUI
{
    static GameObject open;
    public static bool IsOpen => open != null;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Off = new Color(0.6f, 0.58f, 0.65f);

    static readonly string[] TabNames = { "일반", "화면", "조작" };
    static int tab;
    static RectTransform page;
    static readonly List<Button> tabButtons = new List<Button>();

    // 키 바꾸기: 입력을 기다리는 동작 (-1 = 없음)
    static int capturing = -1;
    static readonly List<TMP_Text> keyLabels = new List<TMP_Text>();

    // ESC를 이 창이 처리한 프레임 (ESCmenu가 같은 ESC로 메뉴를 닫지 않게)
    public static int EscHandledFrame = -1;

    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "SettingsPanel", new Vector2(1180f, 860f), out open);
        open.AddComponent<SettingsInput>();

        UIKit.Text(win, "설정", 52f, Gold, new Vector2(0f, 360f), new Vector2(600f, 70f));

        tabButtons.Clear();
        for (int i = 0; i < TabNames.Length; i++)
        {
            int t = i;
            tabButtons.Add(UIKit.MakeButton(win, TabNames[i], new Vector2(-270f + 270f * i, 272f), new Vector2(250f, 64f), () => ShowTab(t), 28f));
        }
        page = UIKit.Rect("Page", win, new Vector2(0f, -30f), new Vector2(1100f, 560f));

        UIKit.MakeButton(win, "닫기", new Vector2(0f, -364f), new Vector2(260f, 76f), Close, 30f);
        ShowTab(tab);
    }

    static void ShowTab(int t)
    {
        tab = t;
        capturing = -1;
        keyLabels.Clear();
        for (int i = page.childCount - 1; i >= 0; i--) Object.Destroy(page.GetChild(i).gameObject);
        for (int i = 0; i < tabButtons.Count; i++)
        {
            tabButtons[i].GetComponent<Image>().color = i == tab ? Gold : Off;
            tabButtons[i].transform.localScale = Vector3.one * (i == tab ? 1.06f : 1f);
        }
        if (tab == 0) BuildGeneral();
        else if (tab == 1) BuildDisplay();
        else BuildControls();
    }

    static TMP_Text RowLabel(string ko, float y) =>
        UIKit.Text(page, ko, 30f, Parch, new Vector2(-400f, y), new Vector2(280f, 50f), TextAlignmentOptions.Left);

    // ================================================================= 일반
    static void BuildGeneral()
    {
        RowLabel("언어", 200f);
        List<Button> langButtons = new List<Button>();
        for (int i = 0; i < 4; i++)
        {
            int lang = i;
            Button b = UIKit.MakeButton(page, "", new Vector2(-140f + 185f * i, 200f), new Vector2(170f, 64f), () =>
            {
                GameSettings.Language = (Loc.Lang)lang;
                HighlightLang(langButtons);
            }, 26f);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.text = Loc.LangNames[i];     // 언어 이름은 각 언어로 그대로
            TMP_FontAsset native = Loc.NativeFont((Loc.Lang)i);
            if (native != null) label.font = native;   // 기본 폰트엔 일본어 · 중국어 글자가 없음
            langButtons.Add(b);
        }
        HighlightLang(langButtons);

        SliderRow("전체 볼륨", 70f, GameSettings.Volume, v => GameSettings.Volume = v);
        SliderRow("음악", -50f, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
        SliderRow("효과음", -170f, GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);
    }

    static void HighlightLang(List<Button> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool on = i == (int)GameSettings.Language;
            buttons[i].GetComponent<Image>().color = on ? Gold : Off;
            buttons[i].transform.localScale = Vector3.one * (on ? 1.06f : 1f);
        }
    }

    // 0 ~ 100%
    static void SliderRow(string ko, float y, float value, System.Action<float> set)
    {
        RowLabel(ko, y);
        TMP_Text percent = UIKit.Text(page, "", 30f, Gold, new Vector2(450f, y), new Vector2(120f, 50f));
        Slider slider = MakeSlider(page, new Vector2(80f, y), new Vector2(560f, 36f));
        slider.value = value;
        percent.text = Mathf.RoundToInt(value * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            set(v);
            percent.text = Mathf.RoundToInt(v * 100f) + "%";
        });
    }

    // ================================================================= 화면
    static void BuildDisplay()
    {
        RowLabel("화면 모드", 200f);
        Button full = null, windowed = null;
        System.Action highlightMode = () =>
        {
            bool isFull = Screen.fullScreenMode != FullScreenMode.Windowed;
            full.GetComponent<Image>().color = isFull ? Gold : Off;
            windowed.GetComponent<Image>().color = isFull ? Off : Gold;
        };
        full = UIKit.MakeButton(page, "전체 화면", new Vector2(-40f, 200f), new Vector2(250f, 64f), () =>
        {
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            Deselect();
            highlightMode();
        }, 26f);
        windowed = UIKit.MakeButton(page, "창 모드", new Vector2(230f, 200f), new Vector2(250f, 64f), () =>
        {
            // 화면을 꽉 채우는 크기면 창 테두리가 화면 밖으로 나가므로 한 단계 작은 해상도로
            Vector2Int r = new Vector2Int(Screen.width, Screen.height);
            if (r.x >= Display.main.systemWidth || r.y >= Display.main.systemHeight)
            {
                List<Vector2Int> list = Resolutions();
                for (int i = list.Count - 1; i >= 0; i--)
                    if (list[i].x < Display.main.systemWidth && list[i].y < Display.main.systemHeight) { r = list[i]; break; }
            }
            Screen.SetResolution(r.x, r.y, FullScreenMode.Windowed);
            Deselect();
            highlightMode();
        }, 26f);
        highlightMode();

        // 해상도: < 1920 × 1080 >
        RowLabel("해상도", 90f);
        TMP_Text res = UIKit.Text(page, "", 30f, Gold, new Vector2(95f, 90f), new Vector2(320f, 50f));
        res.text = Screen.width + " × " + Screen.height;
        System.Action<int> step = d =>
        {
            List<Vector2Int> list = Resolutions();
            int at = list.FindIndex(v => v.x == Screen.width && v.y == Screen.height);
            if (at < 0) at = list.Count - 1;
            at = Mathf.Clamp(at + d, 0, list.Count - 1);
            Screen.SetResolution(list[at].x, list[at].y, Screen.fullScreenMode);
            res.text = list[at].x + " × " + list[at].y;     // 실제 적용은 다음 프레임
            Deselect();
        };
        UIKit.MakeButton(page, "<", new Vector2(-110f, 90f), new Vector2(80f, 64f), () => step(-1), 30f);
        UIKit.MakeButton(page, ">", new Vector2(300f, 90f), new Vector2(80f, 64f), () => step(1), 30f);

        ToggleRow("수직 동기화", -20f, () => GameSettings.VSync, v => GameSettings.VSync = v);
        ToggleRow("화면 흔들림", -130f, () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v);
        ToggleRow("번쩍임 효과", -240f, () => GameSettings.Flashes, v => GameSettings.Flashes = v);
    }

    // 모니터가 지원하는 해상도 (가로 · 세로가 같은 것은 하나로, 작은 것부터)
    static List<Vector2Int> Resolutions()
    {
        List<Vector2Int> list = new List<Vector2Int>();
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int v = new Vector2Int(r.width, r.height);
            if (v.x >= 800 && !list.Contains(v)) list.Add(v);
        }
        Vector2Int now = new Vector2Int(Screen.width, Screen.height);
        if (!list.Contains(now)) list.Add(now);
        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return list;
    }

    static void ToggleRow(string ko, float y, System.Func<bool> get, System.Action<bool> set)
    {
        RowLabel(ko, y);
        Button b = null;
        TMP_Text label = null;
        System.Action refresh = () =>
        {
            bool on = get();
            string text = on ? "켜짐" : "꺼짐";
            label.text = Loc.T(text);
            UIKit.Remember(label, text);
            b.GetComponent<Image>().color = on ? Gold : Off;
        };
        b = UIKit.MakeButton(page, "", new Vector2(95f, y), new Vector2(250f, 64f), () =>
        {
            set(!get());
            Deselect();
            refresh();
        }, 26f);
        label = b.GetComponentInChildren<TMP_Text>();
        refresh();
    }

    // ================================================================= 조작
    static readonly string[] ActionNames =
    {
        "위로 이동", "아래로 이동", "왼쪽 이동", "오른쪽 이동", "재장전", "무기 교체",
        "스킬 1", "스킬 2", "스킬 3", "상호작용 (상점 · 확정)", "특수 강화",
    };

    static void BuildControls()
    {
        for (int i = 0; i < KeyBindings.All.Length; i++)
        {
            int index = i;
            int col = i < 6 ? 0 : 1;
            float y = 220f - 78f * (i % 6);
            float x = col == 0 ? -540f : 20f;
            UIKit.Text(page, ActionNames[i], 26f, Parch, new Vector2(x + 170f, y), new Vector2(340f, 50f), TextAlignmentOptions.Left);
            Button b = UIKit.MakeButton(page, "", new Vector2(x + 430f, y), new Vector2(170f, 60f), () =>
            {
                capturing = index;
                Deselect();                 // Space · Enter가 버튼을 다시 누르지 않게
                RefreshKeys();
            }, 26f);
            keyLabels.Add(b.GetComponentInChildren<TMP_Text>());
        }
        UIKit.Text(page, "버튼을 누른 뒤 바꿀 키를 누르세요 (ESC: 취소)", 22f, Off, new Vector2(-160f, -255f), new Vector2(760f, 44f), TextAlignmentOptions.Left);
        UIKit.MakeButton(page, "기본값으로", new Vector2(390f, -255f), new Vector2(260f, 60f), () =>
        {
            capturing = -1;
            KeyBindings.ResetAll();
            Deselect();
            RefreshKeys();
        }, 24f);
        RefreshKeys();
    }

    static void RefreshKeys()
    {
        for (int i = 0; i < keyLabels.Count; i++)
        {
            if (keyLabels[i] == null) continue;
            bool waiting = i == capturing;
            keyLabels[i].text = waiting ? Loc.T("키를 누르세요") : KeyBindings.Name(KeyBindings.All[i]);
            keyLabels[i].color = waiting ? Gold : new Color(0.96f, 0.9f, 0.8f);
        }
    }

    static readonly KeyCode[] Bindable = BuildBindable();

    static KeyCode[] BuildBindable()
    {
        List<KeyCode> list = new List<KeyCode>();
        foreach (KeyCode k in (KeyCode[])System.Enum.GetValues(typeof(KeyCode)))
            if (k != KeyCode.None && k != KeyCode.Escape && k < KeyCode.Mouse0 && !list.Contains(k)) list.Add(k);
        return list.ToArray();
    }

    // 창이 열려 있는 동안 매 프레임 (SettingsInput): 키 입력 받기 · ESC
    internal static void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscHandledFrame = Time.frameCount;
            if (capturing >= 0) { capturing = -1; RefreshKeys(); }
            else Close();
            return;
        }
        if (capturing < 0) return;
        foreach (KeyCode k in Bindable)
        {
            if (!Input.GetKeyDown(k)) continue;
            KeyBindings.Set(KeyBindings.All[capturing], k);
            capturing = -1;
            RefreshKeys();
            return;
        }
    }

    static void Deselect()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
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
        capturing = -1;
        GameSettings.Save();
        if (open != null) Object.Destroy(open);
        open = null;
    }
}

// 설정 창에 붙어서 SettingsUI.Tick을 불러 줌 (멈춘 화면에서도 Update는 돔)
public class SettingsInput : MonoBehaviour
{
    void Update() => SettingsUI.Tick();
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
            ("{MOVE}", "이동"),
            ("좌클릭", "마우스 방향으로 사격"),
            ("{RELOAD}", "재장전"),
            ("{SWAP}", "무기 교체 (특수 무기를 얻은 뒤)"),
            ("ESC", "일시정지 · 설정"),
        }),
        ("필살기", "fx_reticle", new[]
        {
            ("스킬 게이지", "시간이 지나면 차고, 적을 처치하면 잠깐 더 빨리 찹니다"),
            ("우클릭", "게이지가 가득 차면 필살기를 씁니다"),
            ("조준형", "누르고 있으면 시간이 느려지며 적을 조준, 떼면 발동"),
            ("즉발형", "산탄총 · 쌍권총 · 유탄은 누르는 즉시 발동"),
        }),
        ("성장", "fx_prompt", new[]
        {
            ("레벨업", "카드를 클릭해 고르고 {INTERACT}로 확정"),
            ("상점 제단", "적 35마리마다 나타남 · 다가가서 {INTERACT}로 열기"),
            ("상점", "능력치 · 무기 강화 · 스킬 강화"),
            ("코인", "적이 떨어뜨림 · 코인 자석 능력으로 끌어올 수 있음"),
        }),
        ("특수 능력", "fx_orb", new[]
        {
            ("지옥의 문", "1장 보스를 쓰러뜨리고 문에 들어가면 특수 능력 3개를 고릅니다"),
            ("무기 · 스킬 · 패시브", "무기는 {SWAP}로 교체, 스킬은 {SKILL1} · {SKILL2} · {SKILL3}"),
            ("특수 강화 ({UPGRADE})", "중간 보스를 잡으면 포인트 · 새 능력을 배우거나 가진 능력을 진화"),
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

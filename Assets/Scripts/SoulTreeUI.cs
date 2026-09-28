using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 영혼 트리 화면 (거너): 가운데 = 지금 무기, 네 갈래(무기 · 필살기 · 생존 · 영혼)가 네 귀퉁이 쪽으로 뻗음
// 가지 안에서는 깊이 = 가로 칸, 갈래 = 세로 줄이라 칸이 서로 겹치지 않음
// 칸을 누르면 영혼 조각으로 바로 배움. 앞 칸을 배워야 이어진 칸이 열림. 게임은 멈춘 채로 열림
public class SoulTreeUI : MonoBehaviour
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Soul = new Color(0.72f, 0.58f, 1f);
    static readonly Color Dim = new Color(0.45f, 0.42f, 0.5f);
    // 가지 색 · 방향: 무기 오른쪽 위 · 필살기 왼쪽 위 · 생존 왼쪽 아래 · 영혼 오른쪽 아래
    static readonly Color[] BranchColor = { new Color(1f, 0.62f, 0.3f), new Color(0.5f, 0.9f, 1f), new Color(0.55f, 0.95f, 0.5f), Soul };
    static readonly Vector2[] BranchDir = { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) };
    const float ColStep = 150f, ColStart = 60f;      // 깊이 1 = 210, 2 = 360, 3 = 510, 4 = 660
    const float RowStep = 110f, RowStart = 55f;      // 칸 78 + 글자 28 이 겹치지 않는 간격

    public static SoulTreeUI Instance { get; private set; }
    public static bool IsOpen => Instance != null;

    SpecialAbilities sp;
    System.Action onClose;
    RectTransform root, board;
    TMP_Text shardText;
    readonly List<NodeView> views = new List<NodeView>();
    List<SpecialAbilities.SoulNode> nodes;
    int shownShards = -1;

    class NodeView
    {
        public SpecialAbilities.SoulNode node;
        public RectTransform rect;
        public Image frame, icon, glow, line;
        public TMP_Text cost;
    }

    public static void Open(SpecialAbilities sp, System.Action onClose)
    {
        if (Instance != null || sp == null) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        UIKit.EnsureStyle();
        GameObject go = new GameObject("SoulTree", typeof(RectTransform));
        SoulTreeUI ui = go.AddComponent<SoulTreeUI>();
        ui.sp = sp;
        ui.onClose = onClose;
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.rootCanvas.transform, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.SetAsLastSibling();
        OnTop(go, 500);
        ui.root = r;
        Instance = ui;
        ui.Build();
        SpecialAbilities.SharedFx?.Play("shimmer", 0.5f, 1.2f);
    }

    public static void Close()
    {
        if (Instance == null) return;
        TooltipUI.Hide();
        SoulTreeUI ui = Instance;
        Instance = null;
        Destroy(ui.gameObject);
        ui.onClose?.Invoke();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SettingsUI.EscHandledFrame = Time.frameCount;      // 일시정지 메뉴가 같이 열리지 않게
            Close();
            return;
        }
        if (KeyBindings.Down(GameAction.Upgrade) && Time.frameCount > openFrame) { Close(); return; }

        if (shownShards != SoulShards.Amount) Refresh();
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        foreach (NodeView v in views)
        {
            if (v.glow == null) continue;
            bool can = sp.CanBuy(v.node);
            v.glow.enabled = can;
            if (can) v.glow.color = new Color(BranchColor[v.node.branch].r, BranchColor[v.node.branch].g, BranchColor[v.node.branch].b, 0.25f + 0.4f * pulse);
        }
    }

    int openFrame;

    // ================================================================= 만들기
    void Build()
    {
        openFrame = Time.frameCount;
        Image dim = Img("Dim", root, Vector2.zero, Vector2.zero, null, new Color(0.03f, 0.02f, 0.05f, 0.97f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        board = UIKit.Rect("Board", root, new Vector2(0f, -20f), new Vector2(1200f, 1000f));

        TMP_Text title = UIKit.Text(root, "영혼 트리", 54f, Gold, new Vector2(0f, 480f), new Vector2(800f, 70f));
        title.fontStyle = FontStyles.Bold;
        shardText = UIKit.Text(root, "", 34f, new Color(0.8f, 0.7f, 1f), new Vector2(-700f, 480f), new Vector2(460f, 60f), TextAlignmentOptions.Left);
        UIKit.MakeButton(root, "", new Vector2(790f, 480f), new Vector2(230f, 62f), Close, 24f)
            .GetComponentInChildren<TMP_Text>().text = Loc.T("닫기") + " [" + KeyBindings.Name(GameAction.Upgrade) + "]";
        UIKit.Text(root, "", 22f, Dim, new Vector2(0f, -505f), new Vector2(1600f, 36f)).text =
            Loc.T("칸을 누르면 배웁니다 · 영혼 조각은 강한 적일수록 많이 떨어집니다 · 무기가 진화하면 무기 가지가 새로 열립니다");

        // 가지 이름
        for (int b = 0; b < 4; b++)
        {
            Vector2 at = new Vector2(830f, 200f) * BranchDir[b];
            TMP_Text t = UIKit.Text(board, "", 28f, BranchColor[b], at, new Vector2(260f, 44f));
            t.text = Loc.T(SpecialAbilities.BranchNames[b]);
        }

        Rebuild();
    }

    void Rebuild()
    {
        foreach (NodeView v in views) if (v.rect != null) Destroy(v.rect.gameObject);
        foreach (NodeView v in views) if (v.line != null) Destroy(v.line.gameObject);
        views.Clear();
        if (centerRoot != null) Destroy(centerRoot.gameObject);

        nodes = sp.BuildSoulTree();
        nodes.RemoveAll(n => n.hidden);
        Dictionary<string, Vector2> pos = Layout(nodes);

        // 선 먼저 (칸 아래에 깔리게)
        Dictionary<string, Image> lines = new Dictionary<string, Image>();
        foreach (SpecialAbilities.SoulNode n in nodes)
        {
            Vector2 from = n.parent != null && pos.ContainsKey(n.parent) ? pos[n.parent] : Vector2.zero;
            lines[n.key] = Line(from, pos[n.key]);
        }
        BuildCenter();
        foreach (SpecialAbilities.SoulNode n in nodes)
        {
            NodeView v = BuildNode(n, pos[n.key]);
            v.line = lines[n.key];
            views.Add(v);
        }
        Refresh();
    }

    RectTransform centerRoot;

    void BuildCenter()
    {
        centerRoot = UIKit.Rect("Center", board, Vector2.zero, new Vector2(170f, 170f));
        Img("Glow", centerRoot, Vector2.zero, new Vector2(340f, 340f), sp.glowSprite, new Color(Soul.r, Soul.g, Soul.b, 0.35f));
        Image ring = Img("Ring", centerRoot, Vector2.zero, new Vector2(170f, 170f), UIKit.ButtonSprite, Gold);
        ring.type = Image.Type.Sliced;
        Img("Inner", centerRoot, Vector2.zero, new Vector2(150f, 150f), null, new Color(0.1f, 0.07f, 0.15f, 1f));
        Sprite w = sp.WeaponActive ? Resources.Load<Sprite>("Weapons/weapon_" + sp.CurrentWeapon) : Resources.Load<Sprite>("Weapons/weapon_pistol");
        Image icon = Img("Weapon", centerRoot, new Vector2(0f, 10f), new Vector2(120f, 120f), w, Color.white);
        icon.preserveAspect = true;
        TMP_Text name = UIKit.Text(centerRoot, "", 24f, Gold, new Vector2(0f, -110f), new Vector2(230f, 36f));
        name.text = sp.MainWeaponName;
        TMP_Text tier = UIKit.Text(centerRoot, "", 18f, Parch, new Vector2(0f, -138f), new Vector2(230f, 28f));
        tier.text = sp.EvolutionTier == 0 ? Loc.T("진화 전") : sp.EvolutionTier == 1 ? Loc.T("1차 진화") : Loc.T("최종 진화");
    }

    NodeView BuildNode(SpecialAbilities.SoulNode n, Vector2 at)
    {
        NodeView v = new NodeView { node = n };
        v.rect = UIKit.Rect("Node_" + n.key, board, at, new Vector2(78f, 78f));
        v.glow = Img("Glow", v.rect, Vector2.zero, new Vector2(150f, 150f), sp.glowSprite, Color.clear);
        v.frame = Img("Frame", v.rect, Vector2.zero, new Vector2(78f, 78f), UIKit.ButtonSprite, Color.white);
        v.frame.type = Image.Type.Sliced;
        v.frame.raycastTarget = true;
        Img("Back", v.rect, Vector2.zero, new Vector2(64f, 64f), null, new Color(0.08f, 0.06f, 0.12f, 1f));
        v.icon = Img("Icon", v.rect, Vector2.zero, new Vector2(56f, 56f), n.icon, Color.white);
        v.icon.preserveAspect = true;
        v.cost = UIKit.Text(v.rect, "", 20f, Parch, new Vector2(0f, -54f), new Vector2(120f, 28f));

        Button b = v.frame.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => Buy(v));
        EventTrigger et = v.frame.gameObject.AddComponent<EventTrigger>();
        AddTrigger(et, EventTriggerType.PointerEnter, () => { TooltipUI.Show(n.name, Tip(n)); v.rect.localScale = Vector3.one * 1.12f; });
        AddTrigger(et, EventTriggerType.PointerExit, () => { TooltipUI.Hide(); v.rect.localScale = Vector3.one; });
        return v;
    }

    void Buy(NodeView v)
    {
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (!sp.CanBuy(v.node))
        {
            fx?.Play("buzz", 0.5f);
            StartCoroutine(Shake(v.rect));
            return;
        }
        string key = v.node.key;
        if (!sp.BuyNode(v.node)) return;
        for (int i = 0; i < 14; i++) StartCoroutine(Burst(v.rect.anchoredPosition, BranchColor[v.node.branch]));
        // 새 칸이 열리거나 (패시브 장착으로) 설명이 바뀌므로 다시 만듦
        Rebuild();
        TooltipUI.Hide();
        foreach (NodeView nv in views) if (nv.node.key == key) nv.rect.localScale = Vector3.one * 1.12f;
    }

    void Refresh()
    {
        shownShards = SoulShards.Amount;
        shardText.text = Loc.T("영혼 조각") + "  <color=#FFFFFF>" + SoulShards.Amount + "</color>";
        foreach (NodeView v in views)
        {
            SpecialAbilities.SoulNode n = v.node;
            bool owned = sp.OwnsNode(n.key);
            bool open = sp.IsOpenNode(n);
            bool can = sp.CanBuy(n);
            Color bc = BranchColor[n.branch];
            v.frame.color = owned ? Gold : open ? bc : new Color(0.3f, 0.28f, 0.34f);
            v.icon.color = owned || open ? Color.white : new Color(0.35f, 0.33f, 0.38f, 0.8f);
            v.cost.text = owned ? Loc.T("배움") : n.cost.ToString();
            v.cost.color = owned ? Gold : can ? Color.white : open ? new Color(1f, 0.5f, 0.45f) : Dim;
            if (v.line != null) v.line.color = owned ? new Color(Gold.r, Gold.g, Gold.b, 0.9f) : open ? new Color(bc.r, bc.g, bc.b, 0.55f) : new Color(0.3f, 0.28f, 0.34f, 0.6f);
        }
    }

    string Tip(SpecialAbilities.SoulNode n)
    {
        string state = sp.OwnsNode(n.key) ? "<color=#F5D478>" + Loc.T("배움") + "</color>"
            : !sp.IsOpenNode(n) ? "<color=#8a8494>" + Loc.T("앞 칸을 먼저 배워야 합니다") + "</color>"
            : SoulShards.Amount >= n.cost ? "<color=#b8f5a0>" + Loc.T("눌러서 배우기") + "  (" + n.cost + ")</color>"
            : "<color=#ff8a80>" + Loc.T("영혼 조각이 모자랍니다") + "  (" + SoulShards.Amount + " / " + n.cost + ")</color>";
        return n.desc + "\n" + state;
    }

    // ================================================================= 배치
    // 가지마다 잎(끝 칸)에 차례로 줄을 주고, 부모는 자식 줄의 가운데 (깊이 = 가로 칸)
    Dictionary<string, Vector2> Layout(List<SpecialAbilities.SoulNode> list)
    {
        Dictionary<string, List<SpecialAbilities.SoulNode>> kids = new Dictionary<string, List<SpecialAbilities.SoulNode>>();
        HashSet<string> keys = new HashSet<string>();
        foreach (SpecialAbilities.SoulNode n in list) keys.Add(n.key);
        foreach (SpecialAbilities.SoulNode n in list)
        {
            string p = n.parent != null && keys.Contains(n.parent) ? n.parent : "#" + n.branch;
            if (!kids.TryGetValue(p, out List<SpecialAbilities.SoulNode> l)) kids[p] = l = new List<SpecialAbilities.SoulNode>();
            l.Add(n);
        }
        Dictionary<string, Vector2> pos = new Dictionary<string, Vector2>();
        for (int b = 0; b < 4; b++)
        {
            string rootKey = "#" + b;
            if (Leaves(rootKey, kids) == 0) continue;
            int cursor = 0;
            Place(rootKey, 1, kids, pos, BranchDir[b], ref cursor);
        }
        return pos;
    }

    static int Leaves(string key, Dictionary<string, List<SpecialAbilities.SoulNode>> kids)
    {
        if (!kids.TryGetValue(key, out List<SpecialAbilities.SoulNode> l) || l.Count == 0) return key.StartsWith("#") ? 0 : 1;
        int s = 0;
        foreach (SpecialAbilities.SoulNode n in l) s += Leaves(n.key, kids);
        return s;
    }

    // 잎은 차례로 줄을 받고, 부모는 자식 줄의 가운데 (가운데 선에서 바깥쪽으로)
    static float Place(string key, int depth, Dictionary<string, List<SpecialAbilities.SoulNode>> kids, Dictionary<string, Vector2> pos, Vector2 dir, ref int cursor)
    {
        if (!kids.TryGetValue(key, out List<SpecialAbilities.SoulNode> l) || l.Count == 0)
            return RowStart + RowStep * cursor++;
        float sum = 0f;
        foreach (SpecialAbilities.SoulNode n in l)
        {
            float row = Place(n.key, depth + 1, kids, pos, dir, ref cursor);
            pos[n.key] = new Vector2((ColStart + ColStep * depth) * dir.x, row * dir.y);
            sum += row;
        }
        return sum / l.Count;
    }

    Image Line(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        Image l = Img("Line", board, (a + b) / 2f, new Vector2(d.magnitude, 6f), null, Dim);
        l.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        return l;
    }

    // ================================================================= 연출
    System.Collections.IEnumerator Burst(Vector2 at, Color c)
    {
        Image s = Img("Spark", board, at, Vector2.one * Random.Range(14f, 30f), sp.glowSprite, c);
        Vector2 v = Random.insideUnitCircle.normalized * Random.Range(150f, 420f);
        for (float t = 0f; t < 0.5f && s != null; t += Time.unscaledDeltaTime)
        {
            s.rectTransform.anchoredPosition = at + v * t;
            s.color = new Color(c.r, c.g, c.b, 1f - t / 0.5f);
            yield return null;
        }
        if (s != null) Destroy(s.gameObject);
    }

    System.Collections.IEnumerator Shake(RectTransform r)
    {
        Vector2 home = r.anchoredPosition;
        for (float t = 0f; t < 0.25f && r != null; t += Time.unscaledDeltaTime)
        {
            r.anchoredPosition = home + new Vector2(Mathf.Sin(t * 80f) * 8f * (1f - t / 0.25f), 0f);
            yield return null;
        }
        if (r != null) r.anchoredPosition = home;
    }

    // 따로 그리기 순서를 가진 캔버스: HUD의 다른 캔버스보다 위에, 클릭도 받음
    public static void OnTop(GameObject go, int order)
    {
        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = order;
        go.AddComponent<GraphicRaycaster>();
    }

    static void AddTrigger(EventTrigger et, EventTriggerType type, System.Action a)
    {
        EventTrigger.Entry e = new EventTrigger.Entry { eventID = type };
        e.callback.AddListener(_ => a());
        et.triggers.Add(e);
    }

    static Image Img(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform r = UIKit.Rect(name, parent, pos, size);
        Image i = r.gameObject.AddComponent<Image>();
        i.sprite = sprite;
        i.color = color;
        i.raycastTarget = false;
        return i;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

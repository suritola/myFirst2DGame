using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 영혼 트리 화면 (거너): 가운데 = 지금 무기, 여섯 가지가 사방으로 뻗음
//   무기(위) · 필살기(오른쪽 위) · 스킬(오른쪽 아래) · 생존(아래) · 영혼(왼쪽 아래) · 재물(왼쪽 위)
// 배운 칸과 바로 다음 칸만 보이고 그 너머는 숨김 (배울수록 트리가 펼쳐짐)
// 트리는 화면보다 커서 끌어서 움직이고(좌 · 우클릭 드래그, WASD · 방향키) 휠로 확대 · 축소
// 칸을 누르면 영혼 조각으로 바로 배움. 게임은 멈춘 채로 열림
public class SoulTreeUI : MonoBehaviour
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Soul = new Color(0.72f, 0.58f, 1f);
    static readonly Color Dim = new Color(0.45f, 0.42f, 0.5f);
    static readonly Color[] BranchColor =
    {
        new Color(1f, 0.62f, 0.3f),     // 무기
        new Color(0.5f, 0.9f, 1f),      // 필살기
        new Color(0.45f, 0.75f, 1f),    // 스킬
        new Color(0.55f, 0.95f, 0.5f),  // 생존
        Soul,                           // 영혼
        new Color(1f, 0.85f, 0.35f),    // 재물
    };
    static readonly float[] BranchAngle = { 90f, 30f, -30f, -90f, -150f, 150f };
    const float Sector = 54f;           // 가지 하나가 쓸 수 있는 각도
    const float R0 = 210f, RStep = 135f; // 깊이 1 반지름 · 깊이마다 늘어나는 거리
    const float Gap = 124f;             // 옆 칸과의 최소 거리 (칸 78 + 아래 가격 글자 + 여유)
    const float MinZoom = 0.35f, MaxZoom = 1.4f;

    public static SoulTreeUI Instance { get; private set; }
    public static bool IsOpen => Instance != null;

    SpecialAbilities sp;
    System.Action onClose;
    RectTransform root, viewport, board;
    TMP_Text shardText, footText;
    readonly List<NodeView> views = new List<NodeView>();
    readonly List<GameObject> decor = new List<GameObject>();
    List<SpecialAbilities.SoulNode> nodes;
    Dictionary<string, Vector2> pos;
    Dictionary<int, float> branchScale;
    int shownShards = -1;
    float zoom = 0.7f;
    static Vector2 lastPan;             // 다시 열 때 보던 자리 그대로
    static float lastZoom = 0.7f;

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
        Stretch(r);
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
        lastPan = ui.board.anchoredPosition;
        lastZoom = ui.zoom;
        Instance = null;
        Destroy(ui.gameObject);
        ui.onClose?.Invoke();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    int openFrame;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SettingsUI.EscHandledFrame = Time.frameCount;      // 일시정지 메뉴가 같이 열리지 않게
            Close();
            return;
        }
        if (KeyBindings.Down(GameAction.Upgrade) && Time.frameCount > openFrame) { Close(); return; }

        // 휠: 마우스 위치를 기준으로 확대 · 축소
        float wheel = Input.mouseScrollDelta.y;
        if (wheel != 0f) ZoomAt(Input.mousePosition, zoom * (wheel > 0f ? 1.12f : 1f / 1.12f));
        // 키보드로 이동 (WASD · 방향키), 가운데로 (Home · C)
        Vector2 k = Vector2.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k.x += 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k.x -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) k.y -= 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) k.y += 1f;
        if (k != Vector2.zero) Pan(k * 900f * Time.unscaledDeltaTime);
        if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.C)) Recenter();
        // 우클릭 드래그로도 이동
        if (Input.GetMouseButton(1)) Pan((Vector2)Input.mousePosition - lastMouse);
        lastMouse = Input.mousePosition;

        if (shownShards != SoulShards.Amount) Refresh();
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        foreach (NodeView v in views)
        {
            bool can = sp.CanBuy(v.node);
            v.glow.enabled = can;
            if (can) { Color c = BranchColor[v.node.branch]; v.glow.color = new Color(c.r, c.g, c.b, 0.25f + 0.4f * pulse); }
        }
    }

    Vector2 lastMouse;

    // ================================================================= 이동 · 확대
    float CanvasScale => root.lossyScale.x > 0f ? root.lossyScale.x : 1f;

    void Pan(Vector2 screenDelta)
    {
        board.anchoredPosition += screenDelta / CanvasScale;
        ClampPan();
    }

    void ZoomAt(Vector2 screenPoint, float newZoom)
    {
        newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screenPoint, null, out Vector2 before);
        zoom = newZoom;
        board.localScale = Vector3.one * zoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screenPoint, null, out Vector2 after);
        board.anchoredPosition += (after - before) * zoom;
        ClampPan();
    }

    void Recenter()
    {
        board.anchoredPosition = Vector2.zero;
        zoom = 0.7f;
        board.localScale = Vector3.one * zoom;
    }

    // 보이는 칸이 화면 밖으로 다 사라지지 않게
    void ClampPan()
    {
        float reach = 1500f * zoom;
        Vector2 p = board.anchoredPosition;
        board.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -reach, reach), Mathf.Clamp(p.y, -reach, reach));
    }

    // ================================================================= 만들기
    void Build()
    {
        openFrame = Time.frameCount;
        lastMouse = Input.mousePosition;
        Image dim = Img("Dim", root, Vector2.zero, Vector2.zero, null, new Color(0.03f, 0.02f, 0.05f, 0.97f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        // 빈 곳을 끌면 트리가 움직임
        EventTrigger drag = dim.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry de = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        de.callback.AddListener(d => { PointerEventData pe = (PointerEventData)d; if (pe.button == PointerEventData.InputButton.Left) Pan(pe.delta); });
        drag.triggers.Add(de);

        viewport = UIKit.Rect("Viewport", root, Vector2.zero, Vector2.zero);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        board = UIKit.Rect("Board", viewport, Vector2.zero, new Vector2(100f, 100f));
        board.anchoredPosition = lastPan;
        zoom = lastZoom;
        board.localScale = Vector3.one * zoom;

        // 위 · 아래 띠: 트리가 그 밑으로 지나가도 글이 잘 보이게
        Image top = Img("TopBar", root, Vector2.zero, Vector2.zero, null, new Color(0.05f, 0.03f, 0.08f, 0.92f));
        top.rectTransform.anchorMin = new Vector2(0f, 1f); top.rectTransform.anchorMax = Vector2.one;
        top.rectTransform.pivot = new Vector2(0.5f, 1f); top.rectTransform.sizeDelta = new Vector2(0f, 136f);
        top.rectTransform.anchoredPosition = Vector2.zero;
        Image bottom = Img("BottomBar", root, Vector2.zero, Vector2.zero, null, new Color(0.05f, 0.03f, 0.08f, 0.92f));
        bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottom.rectTransform.pivot = new Vector2(0.5f, 0f); bottom.rectTransform.sizeDelta = new Vector2(0f, 64f);
        bottom.rectTransform.anchoredPosition = Vector2.zero;

        TMP_Text title = UIKit.Text(root, "영혼 트리", 54f, Gold, new Vector2(0f, 480f), new Vector2(800f, 70f));
        title.fontStyle = FontStyles.Bold;
        shardText = UIKit.Text(root, "", 34f, new Color(0.8f, 0.7f, 1f), new Vector2(-700f, 480f), new Vector2(460f, 60f), TextAlignmentOptions.Left);
        UIKit.MakeButton(root, "", new Vector2(790f, 480f), new Vector2(230f, 62f), Close, 24f)
            .GetComponentInChildren<TMP_Text>().text = Loc.T("닫기") + " [" + KeyBindings.Name(GameAction.Upgrade) + "]";
        UIKit.MakeButton(root, "", new Vector2(540f, 480f), new Vector2(200f, 62f), Recenter, 22f)
            .GetComponentInChildren<TMP_Text>().text = Loc.T("가운데로") + " [C]";
        footText = UIKit.Text(root, "", 22f, Dim, new Vector2(0f, -508f), new Vector2(1800f, 36f));
        // 가지 색 범례 (트리 위에 이름을 띄우면 칸과 겹쳐서, 위 띠에 한 줄로)
        string legend = "";
        for (int b = 0; b < SpecialAbilities.BranchCount; b++)
            legend += (b > 0 ? "   " : "") + "<color=#" + ColorUtility.ToHtmlStringRGB(BranchColor[b]) + ">" + Loc.T(SpecialAbilities.BranchNames[b]) + "</color>";
        UIKit.Text(root, "", 22f, Parch, new Vector2(0f, 428f), new Vector2(1200f, 30f)).text = legend;

        Rebuild();
    }

    void Rebuild()
    {
        foreach (NodeView v in views) { if (v.rect != null) Destroy(v.rect.gameObject); if (v.line != null) Destroy(v.line.gameObject); }
        views.Clear();
        foreach (GameObject g in decor) if (g != null) Destroy(g);
        decor.Clear();

        nodes = sp.BuildSoulTree();
        nodes.RemoveAll(n => n.hidden);
        pos = Layout(nodes);

        // 보이는 칸만 (배운 칸 + 바로 다음 칸). 선 먼저 (칸 아래에 깔리게)
        int hiddenCount = 0;
        List<SpecialAbilities.SoulNode> shown = new List<SpecialAbilities.SoulNode>();
        foreach (SpecialAbilities.SoulNode n in nodes)
        {
            if (sp.IsVisibleNode(n)) shown.Add(n);
            else hiddenCount++;
        }
        Dictionary<string, Image> lines = new Dictionary<string, Image>();
        foreach (SpecialAbilities.SoulNode n in shown)
        {
            Vector2 from = n.parent != null && pos.ContainsKey(n.parent) ? pos[n.parent] : Vector2.zero;
            lines[n.key] = Line(from, pos[n.key]);
        }
        BuildCenter();
        foreach (SpecialAbilities.SoulNode n in shown)
        {
            NodeView v = BuildNode(n, pos[n.key]);
            v.line = lines[n.key];
            views.Add(v);
        }
        footText.text = Loc.T("칸을 누르면 배웁니다 · 배우면 다음 칸이 드러납니다") + "  ·  " + Loc.T("숨은 칸") + " " + hiddenCount
                      + "  ·  " + Loc.T("끌기 · WASD 이동, 휠 확대");
        Refresh();
    }

    void BuildCenter()
    {
        RectTransform c = UIKit.Rect("Center", board, Vector2.zero, new Vector2(170f, 170f));
        decor.Add(c.gameObject);
        Img("Glow", c, Vector2.zero, new Vector2(340f, 340f), sp.glowSprite, new Color(Soul.r, Soul.g, Soul.b, 0.35f));
        Image ring = Img("Ring", c, Vector2.zero, new Vector2(170f, 170f), UIKit.ButtonSprite, Gold);
        ring.type = Image.Type.Sliced;
        Img("Inner", c, Vector2.zero, new Vector2(150f, 150f), null, new Color(0.1f, 0.07f, 0.15f, 1f));
        Sprite w = sp.WeaponActive ? Resources.Load<Sprite>("Weapons/weapon_" + sp.CurrentWeapon) : Resources.Load<Sprite>("Weapons/weapon_pistol");
        Image icon = Img("Weapon", c, new Vector2(0f, 14f), new Vector2(104f, 104f), w, Color.white);
        icon.preserveAspect = true;
        TMP_Text name = UIKit.Text(c, "", 22f, Gold, new Vector2(0f, -52f), new Vector2(160f, 30f));
        name.text = sp.MainWeaponName;
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
        AddTrigger(et, EventTriggerType.PointerExit, () => { TooltipUI.Hide(); if (v.rect != null) v.rect.localScale = Vector3.one; });
        return v;
    }

    void Buy(NodeView v)
    {
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (!sp.CanBuy(v.node))
        {
            fx?.Play("buzz", 0.5f);
            StartCoroutine(Shake(v.rect));
            TooltipUI.Show(v.node.name, Tip(v.node));
            return;
        }
        string key = v.node.key;
        Vector2 at = v.rect.anchoredPosition;
        Color c = BranchColor[v.node.branch];
        if (!sp.BuyNode(v.node)) return;
        // 새 칸이 드러나고 (패시브 장착 등으로) 설명이 바뀌므로 다시 만듦. 칸 자리는 그대로
        Rebuild();
        for (int i = 0; i < 14; i++) StartCoroutine(Burst(at, c));
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
            bool can = sp.CanBuy(n);
            bool blocked = !owned && sp.BlockReason(n) != null;
            Color bc = BranchColor[n.branch];
            v.frame.color = owned ? Gold : blocked ? new Color(0.35f, 0.32f, 0.38f) : bc;
            v.icon.color = owned || !blocked ? Color.white : new Color(0.5f, 0.48f, 0.52f, 0.9f);
            v.cost.text = owned ? "" : n.cost.ToString();           // 배운 칸은 금색 테두리로 충분 (글자는 지저분해서 뺌)
            v.cost.color = owned ? Gold : can ? Color.white : new Color(1f, 0.5f, 0.45f);
            if (v.line != null) v.line.color = owned ? new Color(Gold.r, Gold.g, Gold.b, 0.9f) : new Color(bc.r, bc.g, bc.b, 0.55f);
        }
    }

    string Tip(SpecialAbilities.SoulNode n)
    {
        string block = sp.BlockReason(n);
        string state = sp.OwnsNode(n.key) ? "<color=#F5D478>" + Loc.T("배움") + "</color>"
            : block != null ? "<color=#8a8494>" + block + "</color>"
            : SoulShards.Amount >= n.cost ? "<color=#b8f5a0>" + Loc.T("눌러서 배우기") + "  (" + n.cost + ")</color>"
            : "<color=#ff8a80>" + Loc.T("영혼 조각이 모자랍니다") + "  (" + SoulShards.Amount + " / " + n.cost + ")</color>";
        return n.desc + "\n" + state;
    }

    // ================================================================= 배치
    // 방사형: 가지마다 정해진 방향을 중심으로 부채꼴. 칸마다 필요한 폭(각도)을 아래부터 쌓아 올려
    // 같은 깊이의 칸끼리 Gap 보다 가까워지지 않게 함. 가지가 넓으면 그 가지만 바깥으로 밀어 냄 (트리는 끌어서 봄)
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
        Dictionary<string, Vector2> result = new Dictionary<string, Vector2>();
        for (int b = 0; b < SpecialAbilities.BranchCount; b++)
        {
            string rootKey = "#" + b;
            if (!kids.ContainsKey(rootKey)) continue;
            // 1) 배율 1로 필요한 폭을 재고, 부채꼴을 넘으면 그만큼 바깥으로 (폭은 반지름에 반비례)
            Dictionary<string, float> width = new Dictionary<string, float>();
            float total = Width(rootKey, 0, 1f, kids, width);
            float scale = Mathf.Max(1f, total / (Sector * Mathf.Deg2Rad));
            if (scale > 1f) { width.Clear(); total = Width(rootKey, 0, scale, kids, width); }
            // 2) 폭만큼 각도를 나눠 줌
            float start = BranchAngle[b] * Mathf.Deg2Rad - total / 2f;
            Assign(rootKey, 0, start, scale, kids, width, result);
        }
        return result;
    }

    static float Radius(int depth, float scale) => (R0 + RStep * (depth - 1)) * scale;

    // 이 칸(과 아래 칸들)이 필요로 하는 각도: 자식들 폭의 합과 자기 칸 폭 중 큰 것
    static float Width(string key, int depth, float scale, Dictionary<string, List<SpecialAbilities.SoulNode>> kids, Dictionary<string, float> width)
    {
        float own = depth > 0 ? Gap / Radius(depth, scale) : 0f;
        float sum = 0f;
        if (kids.TryGetValue(key, out List<SpecialAbilities.SoulNode> l))
            foreach (SpecialAbilities.SoulNode n in l) sum += Width(n.key, depth + 1, scale, kids, width);
        float w = Mathf.Max(own, sum);
        width[key] = w;
        return w;
    }

    static void Assign(string key, int depth, float start, float scale, Dictionary<string, List<SpecialAbilities.SoulNode>> kids,
                       Dictionary<string, float> width, Dictionary<string, Vector2> result)
    {
        if (!kids.TryGetValue(key, out List<SpecialAbilities.SoulNode> l)) return;
        float sum = 0f;
        foreach (SpecialAbilities.SoulNode n in l) sum += width[n.key];
        // 자식들이 부모 폭보다 좁으면 가운데로 모음
        float a = start + (width[key] - sum) / 2f;
        foreach (SpecialAbilities.SoulNode n in l)
        {
            float w = width[n.key];
            float mid = a + w / 2f;
            result[n.key] = new Vector2(Mathf.Cos(mid), Mathf.Sin(mid)) * Radius(depth + 1, scale);
            Assign(n.key, depth + 1, a, scale, kids, width, result);
            a += w;
        }
    }

    Image Line(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        Image l = Img("Line", board, (a + b) / 2f, new Vector2(d.magnitude, 6f), null, Dim);
        l.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        l.rectTransform.SetAsFirstSibling();
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 스팀 상점 이미지 · 스크린샷 · 아이콘 · 업적 아이콘을 게임 그림으로 합성해 PNG로 저장
public class StoreArt3
{
    static readonly string Out = Environment.GetEnvironmentVariable("ART_OUT") ?? Environment.GetEnvironmentVariable("V2_OUT") ?? "C:/Temp/art";
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);

    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f) => o.GetType().GetField(f, Any)?.GetValue(o);

    // ================================================================= render helpers
    static Texture2D Render(Camera cam, int w, int h, bool alpha)
    {
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        RenderTexture old = cam.targetTexture;
        int mask = cam.cullingMask;
        if (!alpha) cam.cullingMask |= 1 << 5;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        cam.targetTexture = old;
        cam.cullingMask = mask;
        RenderTexture.active = null;
        rt.Release();
        return tex;
    }

    static void Save(Texture2D tex, string rel, bool jpg = false)
    {
        string path = Path.Combine(Out, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, jpg ? tex.EncodeToJPG(95) : tex.EncodeToPNG());
        Debug.Log("[ART] " + rel + " " + tex.width + "x" + tex.height);
    }

    static Texture2D Gray(Texture2D src)
    {
        Texture2D g = new Texture2D(src.width, src.height, TextureFormat.RGB24, false);
        Color[] px = src.GetPixels();
        for (int i = 0; i < px.Length; i++) { float l = px[i].grayscale * 0.55f; px[i] = new Color(l, l, l); }
        g.SetPixels(px);
        g.Apply();
        return g;
    }

    static void OverlaysToCamera(Camera cam)
    {
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 5f; OnTop(c); }
    }

    static void GameUI(bool on)
    {
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.name != "ArtCanvas") c.enabled = on;
    }

    // 카메라 모드 캔버스는 월드 스프라이트와 함께 정렬되므로 가장 위 정렬 레이어로 올림
    static void OnTop(Canvas c)
    {
        SortingLayer[] layers = SortingLayer.layers;
        c.overrideSorting = true;
        c.sortingLayerName = layers[layers.Length - 1].name;
        c.sortingOrder = 30000 + c.sortingOrder;
    }

    static Canvas ArtCanvas(Camera cam)
    {
        GameObject go = new GameObject("ArtCanvas", typeof(RectTransform), typeof(Canvas));
        go.layer = 5;
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = cam;
        c.planeDistance = 1f;
        OnTop(c);
        c.sortingOrder = 32000;
        return c;
    }

    static RectTransform Rect(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject("R", typeof(RectTransform));
        go.layer = 5;
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    static Image Img(Transform parent, Sprite s, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Image i = Rect(parent, anchor, pos, size).gameObject.AddComponent<Image>();
        i.sprite = s;
        i.color = c;
        i.preserveAspect = s != null;
        return i;
    }

    // 스프라이트 둘레의 빈 칸을 잘라냄 (읽기 불가 텍스처도 RenderTexture로 복사해서 읽음)
    static Sprite Trim(Sprite s)
    {
        Texture2D src = s.texture;
        RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;
        Rect tr = s.textureRect;
        Texture2D tex = new Texture2D((int)tr.width, (int)tr.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(tr.x, tr.y, tr.width, tr.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        int minX = tex.width, minY = tex.height, maxX = -1, maxY = -1;
        Color32[] px = tex.GetPixels32();
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                if (px[y * tex.width + x].a > 20) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        if (maxX < 0) return s;
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1), Vector2.one * 0.5f, 100f);
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    static TMP_FontAsset titleFont;
    static Material titleMat;

    static void FindTitleFont()
    {
        titleFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name.StartsWith("Cafe24"));
        titleMat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name.StartsWith("Cafe24") && m.name.Contains("Outline"));
        Debug.Log("[ART] font " + (titleFont != null) + " mat " + (titleMat != null));
    }

    static TextMeshProUGUI Title(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        TextMeshProUGUI t = Rect(parent, anchor, pos, box).gameObject.AddComponent<TextMeshProUGUI>();
        if (titleFont != null) t.font = titleFont;
        if (titleMat != null) t.fontSharedMaterial = titleMat;
        t.enableAutoSizing = true;
        t.fontSizeMin = 8f;
        t.fontSizeMax = size;
        t.color = Gold;
        t.alignment = TextAlignmentOptions.Center;
        t.text = text;
        t.enableWordWrapping = false;
        return t;
    }

    // ================================================================= game control
    static object Player() => UnityEngine.Object.FindFirstObjectByType(T("PlayerController"));

    static void KeepAlive()
    {
        object p = Player();
        p?.GetType().GetMethod("GrantInvincibility").Invoke(p, new object[] { 9999f });
        GameObject shop = GameObject.Find("LevelShopPanel");
        if (shop != null) shop.SetActive(false);
        Time.timeScale = 1f;
    }

    static void GoStage(int stage)
    {
        object sm = UnityEngine.Object.FindFirstObjectByType(T("StageManager"));
        foreach (Renderer r in (Renderer[])Get(sm, "caveRenderers")) if (r != null) r.enabled = stage == 0;
        ((GameObject)Get(sm, "hellMap"))?.SetActive(stage == 1);
        ((GameObject)Get(sm, "meadowMap"))?.SetActive(stage == 2);
        Camera.main.backgroundColor = (Color)Get(sm, stage == 1 ? "hellBackground" : "meadowBackground");
        sm.GetType().GetProperty("CurrentStage", Any).SetValue(sm, stage);
        object sp = Get(sm, "spawner");
        sp.GetType().GetMethod("StartStage").Invoke(sp, new object[] { stage });
        object bar = UnityEngine.Object.FindFirstObjectByType(T("bossbar"));
        bar.GetType().GetField("bossSpawn").SetValue(bar, false);
        Transform pl = (Transform)Get(sm, "player");
        pl.position = (Vector2)Get(sm, "stage2PlayerStart");
    }

    static void EquipWeapon(int[] ids)
    {
        object sm = UnityEngine.Object.FindFirstObjectByType(T("StageManager"));
        object sa = Get(sm, "specials");
        sa.GetType().GetMethod("Equip").Invoke(sa, new object[] { ids });
        sa.GetType().GetField("weaponIndex", Any).SetValue(sa, 0);
    }

    static IEnumerator Volley()
    {
        object sm = UnityEngine.Object.FindFirstObjectByType(T("StageManager"));
        object sa = Get(sm, "specials");
        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(T("EnermyController")));
        Vector3 me = ((Component)Player()).transform.position;
        foreach (var e0 in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None)
                     .OrderBy(o => (((Component)o).transform.position - me).sqrMagnitude).Take(6)) list.Add(e0);
        var e = (IEnumerator)sa.GetType().GetMethod("WeaponVolley").Invoke(sa, new object[] { list, 5f, 0f });
        ((MonoBehaviour)sa).StartCoroutine(e);
        yield return null;
    }

    static void Weapon(int index)
    {
        object sm = UnityEngine.Object.FindFirstObjectByType(T("StageManager"));
        object sa = Get(sm, "specials");
        sa.GetType().GetField("weaponIndex", Any).SetValue(sa, index);
    }

    static void SpawnBoss(int stage)
    {
        object data = Resources.Load("CodexData");
        GameObject prefab = ((GameObject[])Get(data, "bosses"))[stage];
        Vector3 me = ((Component)Player()).transform.position;
        UnityEngine.Object.Instantiate(prefab, me + new Vector3(7.5f, 4f, 0f), Quaternion.identity);
        object bar = UnityEngine.Object.FindFirstObjectByType(T("bossbar"));
        bar.GetType().GetField("bossSpawn").SetValue(bar, true);
    }

    static IEnumerator Wait(float s) { float end = Time.realtimeSinceStartup + s; while (Time.realtimeSinceStartup < end) { KeepAlive(); yield return null; } }

    static Texture2D Screen(Camera cam)
    {
        GameUI(true);
        return Render(cam, 1920, 1080, false);
    }

    // ================================================================= tests
    static Type cd, cid, gi;
    static void SetChar(string n) => cd.GetField("Override", Any).SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(cid), Enum.Parse(cid, n)));
    static void In(string f, object v) => gi.GetField(f).SetValue(null, v);

    static IEnumerator Load(string ch, int stage)
    {
        SetChar(ch);
        SceneManager.LoadScene("GameScene");
        yield return null; yield return null;
        OverlaysToCamera(Camera.main);
        if (stage > 0) GoStage(stage);
        yield return null;
    }

    static Vector3 Nearest(Vector3 from, float minDist = 0f)
    {
        Vector3 best = from + Vector3.right * 6f;
        float bd = float.MaxValue;
        foreach (var e in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None))
        {
            Vector3 p = ((Component)e).transform.position;
            float d = (p - from).sqrMagnitude;
            if (d < bd && d > minDist * minDist) { bd = d; best = p; }
        }
        return best;
    }

    // 무리가 가장 많은 쪽 (주변 적 수로)
    static Vector3 Crowd(Vector3 from, float radius)
    {
        var all = UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None).Select(e => ((Component)e).transform.position).ToList();
        if (all.Count == 0) return from + Vector3.right * 6f;
        return all.OrderByDescending(p => all.Count(q => (q - p).sqrMagnitude < radius * radius) - (p - from).magnitude * 0.05f).First();
    }

    // 무적 깜빡임을 끄고 한 프레임 (캐릭터가 흐리게 찍히지 않게)
    static IEnumerator Solid()
    {
        object p = Player();
        p.GetType().GetField("invincibleUntil", Any).SetValue(p, 0f);
        foreach (SpriteRenderer r in ((Component)p).GetComponentsInChildren<SpriteRenderer>()) { Color c = r.color; c.a = 1f; r.color = c; }
        yield return null;
    }

    static Vector3 Me => ((Component)Player()).transform.position;

    static IEnumerator WaitFor(string goName, float max)
    {
        float end = Time.realtimeSinceStartup + max;
        while (GameObject.Find(goName) == null && Time.realtimeSinceStartup < end) { KeepAlive(); yield return null; }
    }

    static void FillGauge()
    {
        object g = UnityEngine.Object.FindFirstObjectByType(T("SkillGauge"));
        g.GetType().GetField("SkillPoint").SetValue(g, 99999f);
    }

    // 적을 플레이어 둘레 고리에 흩어 세우고 멈춤 (캐릭터가 가려지지 않게)
    static void GatherEnemies(Vector3 around, float radius)
    {
        Vector3 me = Me;
        var all = UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None).Take(14).ToList();
        foreach (var extra in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None).Skip(14)) UnityEngine.Object.Destroy(((Component)extra).gameObject);
        for (int i = 0; i < all.Count; i++)
        {
            Transform t = ((Component)all[i]).transform;
            float ang = (i / (float)Mathf.Max(1, all.Count)) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
            float r = UnityEngine.Random.Range(3.2f, Mathf.Max(3.5f, radius));
            t.position = me + new Vector3(Mathf.Cos(ang) * r * 1.5f, Mathf.Sin(ang) * r, 0f);
            all[i].GetType().GetField("speed").SetValue(all[i], 0f);
        }
    }

    // 보스를 화면 오른쪽 위에 세우고 기술을 끔 (플레이어 · 연출을 가리지 않게)
    static void PinBoss(object b)
    {
        if (b == null) return;
        Component c = (Component)b;
        c.transform.position = Me + new Vector3(7.5f, 3.2f, 0f);
        b.GetType().GetField("speed").SetValue(b, 0f);
        foreach (Behaviour x in c.GetComponentsInChildren<Behaviour>())
            if (x != (Behaviour)c && !(x is Animator)) x.enabled = false;
    }

    [UnityTest]
    public IEnumerator Art()
    {
        cd = T("CharacterData"); cid = T("CharacterId"); gi = T("GameInput");
        Camera cam;

        // ---------------- 01 거너 · 지하 묘역 일제 사격
        yield return Load("Gunner", 0);
        FindTitleFont();
        cam = Camera.main;
        yield return Wait(12f);
        GatherEnemies(Me, 8f);
        yield return Wait(0.6f);
        // 둘레의 적을 돌아가며 연사 (총알이 화면에 여러 발 보이게)
        In("Auto", true);
        var foes = UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None).Select(e => ((Component)e).transform.position).ToList();
        for (int i = 0; i < 4 && foes.Count > 0; i++)
        {
            In("AutoAim", foes[i % foes.Count]);
            In("FireDownFrame", Time.frameCount + 1);
            float until = Time.realtimeSinceStartup + (i < 3 ? 0.47f : 0.06f);
            while (Time.realtimeSinceStartup < until) { KeepAlive(); yield return null; }
        }
        yield return Wait(0.05f);
        yield return Solid();
        Save(Screen(cam), "screenshots/01_gunner_catacombs.png");
        In("Auto", false);

        // ---------------- 02 검사 · 지옥에서 장검 베기
        yield return Load("Swordsman", 1);
        cam = Camera.main;
        yield return Wait(9f);
        GatherEnemies(Me, 4.5f);
        yield return Wait(0.5f);
        In("Auto", true);
        In("AutoAim", Crowd(Me, 3f));
        In("AutoFire", true);
        yield return WaitFor("fx_swordswing", 3f);
        yield return null; yield return null;
        yield return Solid();
        Save(Screen(cam), "screenshots/02_swordsman_hell.png");
        In("AutoFire", false);

        // ---------------- 03 도적 · 초원 출혈 돌진
        yield return Load("Rogue", 2);
        cam = Camera.main;
        yield return Wait(9f);
        GatherEnemies(Me, 7f);
        yield return Wait(0.4f);
        In("Auto", true);
        In("AutoAim", Crowd(Me, 3f));
        In("AutoFire", true);
        yield return Wait(0.5f);
        FillGauge();
        In("AutoAim", Crowd(Me, 3f));
        In("UltDownFrame", Time.frameCount + 1);
        yield return Wait(0.12f);
        yield return Solid();
        Save(Screen(cam), "screenshots/03_rogue_meadow.png");
        In("AutoFire", false);

        // ---------------- 04 궁수 · 리치 왕에게 화살비
        yield return Load("Archer", 0);
        cam = Camera.main;
        yield return Wait(7f);
        SpawnBoss(0);
        yield return null;
        object lich = UnityEngine.Object.FindFirstObjectByType(T("bosss"));
        PinBoss(lich);
        yield return Wait(1.5f);
        PinBoss(lich);
        GatherEnemies(Me, 7f);
        Vector3 bossAt = lich != null ? ((Component)lich).transform.position : Me + Vector3.right * 6f;
        In("Auto", true);
        In("AutoAim", bossAt);
        In("AutoFire", true);
        yield return Wait(0.6f);
        FillGauge();
        In("UltDownFrame", Time.frameCount + 1);
        yield return Wait(0.45f);
        yield return Solid();
        Save(Screen(cam), "screenshots/04_archer_lich.png");
        In("AutoFire", false);

        // ---------------- 05 연금술사 · 지옥의 군주에게 대폭발
        yield return Load("Alchemist", 1);
        cam = Camera.main;
        yield return Wait(7f);
        SpawnBoss(1);
        yield return null;
        object lord = UnityEngine.Object.FindFirstObjectByType(T("bosss"));
        PinBoss(lord);
        yield return Wait(1.5f);
        PinBoss(lord);
        bossAt = lord != null ? ((Component)lord).transform.position : Me + Vector3.right * 6f;
        GatherEnemies(Me, 7f);
        In("Auto", true);
        In("AutoAim", bossAt);
        In("AutoFire", true);
        yield return Wait(0.5f);
        FillGauge();
        In("AutoUlt", true);
        In("UltDownFrame", Time.frameCount + 1);
        yield return Wait(1.3f);
        In("AutoUlt", false);
        In("UltUpFrame", Time.frameCount + 1);
        yield return WaitFor("fx_alchemyblast", 2f);
        yield return Wait(0.08f);
        yield return Solid();
        Save(Screen(cam), "screenshots/05_alchemist_demon_lord.png");
        In("AutoFire", false);

        // ---------------- 06 검사 · 킹 슬라임 앞 회전 베기
        yield return Load("Swordsman", 2);
        cam = Camera.main;
        yield return Wait(7f);
        SpawnBoss(2);
        yield return null;
        object king = UnityEngine.Object.FindFirstObjectByType(T("bosss"));
        PinBoss(king);
        yield return Wait(1.5f);
        PinBoss(king);
        GatherEnemies(Me, 4.5f);
        yield return Wait(0.3f);
        In("Auto", true);
        FillGauge();
        In("AutoUlt", true);
        In("UltDownFrame", Time.frameCount + 1);
        yield return Wait(1.2f);
        In("AutoUlt", false);
        In("UltUpFrame", Time.frameCount + 1);
        yield return WaitFor("fx_spinslash", 1f);
        yield return null; yield return null;
        yield return Solid();
        Save(Screen(cam), "screenshots/06_swordsman_king_slime.png");
        In("Auto", false);

        // ---------------- 캡슐 · 라이브러리 (지옥 배경 + 캐릭터 다섯 명 + 제목)
        yield return Load("Gunner", 1);
        cam = Camera.main;
        float baseSize = cam.orthographicSize;
        yield return Wait(2f);
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemySpawner"))).enabled = false;
        foreach (var e in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None)) UnityEngine.Object.Destroy(((Component)e).gameObject);
        yield return Wait(3f);
        foreach (var e in UnityEngine.Object.FindObjectsByType(T("EnermyController"), FindObjectsSortMode.None)) UnityEngine.Object.Destroy(((Component)e).gameObject);
        yield return Wait(0.5f);
        foreach (Renderer r in ((Component)Player()).GetComponentsInChildren<Renderer>()) r.enabled = false;
        GameUI(false);
        Canvas art = ArtCanvas(cam);
        Image dim = Img(art.transform, null, new Color(0.03f, 0f, 0.05f, 0.45f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        Stretch(dim.rectTransform);
        Sprite glow = Trim(Resources.LoadAll<Sprite>("FX/fx_orb").OrderBy(s => s.name.Length).ThenBy(s => s.name).First());
        MethodInfo portrait = T("CharacterUI").GetMethod("Portrait");
        string[] lineup = { "Slot6", "Archer", "Rogue", "Gunner", "Swordsman", "Alchemist", "Slot7" };
        var heroes = new List<Image>();
        var halos = new List<Image>();
        foreach (string n in lineup)
        {
            halos.Add(Img(art.transform, glow, new Color(0.6f, 0.85f, 1f, 0.35f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero));
            bool secret = n.StartsWith("Slot");
            if (secret) halos[halos.Count - 1].color = new Color(0.8f, 0.3f, 1f, 0.25f);
            heroes.Add(Img(art.transform, Trim((Sprite)portrait.Invoke(null, new[] { Enum.Parse(cid, n) })), secret ? Color.black : Color.white, Vector2.one * 0.5f, Vector2.zero, Vector2.zero));
        }
        TextMeshProUGUI title = Title(art.transform, "Soul Saver", 200f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

        void Layout(int w, int h, float ty, float th, float rowY, float charH, float spread)
        {
            title.enabled = th > 0f;
            title.rectTransform.anchoredPosition = new Vector2(0f, ty * h);
            title.rectTransform.sizeDelta = new Vector2(w * 0.9f, h * th);
            title.fontSizeMax = h * th;
            for (int i = 0; i < heroes.Count; i++)
            {
                int off = i - 3;
                bool on = charH > 0f;
                heroes[i].enabled = halos[i].enabled = on;
                float s = charH * h * (off == 0 ? 1.2f : Mathf.Abs(off) == 1 ? 1f : Mathf.Abs(off) == 2 ? 0.88f : 0.8f);
                Vector2 p = new Vector2(off * spread * w, rowY * h - Mathf.Abs(off) * 0.03f * h);
                heroes[i].rectTransform.anchoredPosition = p;
                heroes[i].rectTransform.sizeDelta = Vector2.one * s;
                halos[i].rectTransform.anchoredPosition = p;
                halos[i].rectTransform.sizeDelta = Vector2.one * s * 1.5f;
                heroes[i].transform.SetAsLastSibling();
            }
            // 가운데(거너)가 맨 앞
            heroes[3].transform.SetAsLastSibling();
            title.transform.SetAsLastSibling();
        }

        (string name, int w, int h, float zoom, float ty, float th, float rowY, float charH, float spread)[] caps =
        {
            ("header_capsule", 920, 430, 0.55f, 0.28f, 0.28f, -0.17f, 0.4f, 0.135f),
            ("small_capsule", 462, 174, 0.45f, 0f, 0.62f, 0f, 0f, 0f),
            ("main_capsule", 1232, 706, 0.6f, 0.29f, 0.26f, -0.17f, 0.42f, 0.125f),
            ("vertical_capsule", 748, 896, 0.62f, 0.3f, 0.16f, -0.13f, 0.24f, 0.142f),
            ("library_capsule", 600, 900, 0.6f, 0.32f, 0.14f, -0.14f, 0.2f, 0.142f),
            ("library_header", 920, 430, 0.55f, 0.28f, 0.28f, -0.17f, 0.4f, 0.135f),
        };
        foreach (var c in caps)
        {
            cam.orthographicSize = baseSize * c.zoom;
            Layout(c.w, c.h, c.ty, c.th, c.rowY, c.charH, c.spread);
            yield return null;
            Save(Render(cam, c.w, c.h, false), "capsules/" + c.name + ".png");
        }
        // 히어로: 글자 없이 캐릭터만
        cam.orthographicSize = baseSize * 0.7f;
        Layout(3840, 1240, 0f, 0f, -0.06f, 0.5f, 0.085f);
        yield return null;
        Save(Render(cam, 3840, 1240, false), "capsules/library_hero.png");

        // 로고만 (투명 배경)
        foreach (Image i in heroes.Concat(halos)) i.enabled = false;
        dim.enabled = false;
        title.enabled = true;
        title.rectTransform.anchoredPosition = Vector2.zero;
        title.rectTransform.sizeDelta = new Vector2(1180f, 420f);
        title.fontSizeMax = 360f;
        int mask = cam.cullingMask; CameraClearFlags flags = cam.clearFlags; Color bg = cam.backgroundColor;
        cam.cullingMask = 1 << 5; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        Save(Render(cam, 1280, 720, true), "capsules/library_logo.png");
        cam.cullingMask = mask; cam.clearFlags = flags; cam.backgroundColor = bg;
        UnityEngine.Object.Destroy(art.gameObject);
        cam.orthographicSize = baseSize;

        // ---------------- 07 캐릭터 도감
        SetChar("Gunner");
        SceneManager.LoadScene("MainMenu");
        yield return null; yield return null;
        yield return Wait(0.5f);
        cam = Camera.main;
        OverlaysToCamera(cam);
        T("CharacterUI").GetMethod("Open").Invoke(null, new object[] { GameObject.Find("GameStartButton").transform.root });
        yield return Wait(0.3f);
        OverlaysToCamera(cam);
        Save(Screen(cam), "screenshots/07_character_codex.png");

        cd.GetField("Override", Any).SetValue(null, null);
        In("Auto", false);
    }
}

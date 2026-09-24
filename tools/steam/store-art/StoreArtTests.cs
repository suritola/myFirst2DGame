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
public class StoreArtTests
{
    static readonly string Out = Environment.GetEnvironmentVariable("ART_OUT") ?? "C:/Temp/art";
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
    [UnityTest]
    public IEnumerator Art()
    {
        SceneManager.LoadScene("GameScene");
        yield return null; yield return null;
        FindTitleFont();
        Camera cam = Camera.main;
        OverlaysToCamera(cam);
        float baseSize = cam.orthographicSize;

        // ---------------- 1: 지하 묘역 · 권총 일제 사격
        yield return Wait(14f);
        yield return Volley();
        yield return Wait(0.3f);
        Save(Screen(cam), "screenshots/01_catacombs_volley.png");

        // ---------------- 2: 리치 왕
        SpawnBoss(0);
        yield return Wait(2.5f);
        Save(Screen(cam), "screenshots/02_lich_king.png");

        // ---------------- 3: 지옥 · 산탄총 필살기
        foreach (var b0 in UnityEngine.Object.FindObjectsByType(T("bosss"), FindObjectsSortMode.None)) UnityEngine.Object.Destroy(((Component)b0).gameObject);
        EquipWeapon(new[] { 0, 5, 16 });
        GoStage(1);
        yield return Wait(10f);
        // ---------------- 캡슐 (지옥 배경)
        GameUI(false);
        Canvas art = ArtCanvas(cam);
        Image dim = Img(art.transform, null, new Color(0f, 0f, 0f, 0.35f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        Stretch(dim.rectTransform);
        TextMeshProUGUI title = Title(art.transform, "Gun Saver", 200f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

        (string name, int w, int h, float zoom, float ty, float th)[] caps =
        {
            ("header_capsule", 920, 430, 0.55f, -0.18f, 0.34f),
            ("small_capsule", 462, 174, 0.45f, 0f, 0.62f),
            ("main_capsule", 1232, 706, 0.6f, -0.2f, 0.3f),
            ("vertical_capsule", 748, 896, 0.62f, 0.28f, 0.2f),
            ("library_capsule", 600, 900, 0.6f, 0.3f, 0.17f),
        };
        foreach (var c in caps)
        {
            cam.orthographicSize = baseSize * c.zoom;
            title.rectTransform.anchoredPosition = new Vector2(0f, c.ty * c.h);
            title.rectTransform.sizeDelta = new Vector2(c.w * 0.9f, c.h * c.th);
            title.fontSizeMax = c.h * c.th;
            yield return null;
            Save(Render(cam, c.w, c.h, false), "capsules/" + c.name + ".png");
        }
        cam.orthographicSize = baseSize * 0.7f;
        title.enabled = false; dim.enabled = false;
        Save(Render(cam, 3840, 1240, false), "capsules/library_hero.png");

        // 로고만 (투명 배경)
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

        GameUI(true);
        yield return Volley();
        yield return Wait(0.35f);
        Save(Screen(cam), "screenshots/03_hell_shotgun_ult.png");

        // ---------------- 4: 지옥의 군주
        SpawnBoss(1);
        yield return Wait(2.5f);
        Save(Screen(cam), "screenshots/04_demon_lord.png");

        // ---------------- 5: 초원 · 뇌운
        foreach (var b0 in UnityEngine.Object.FindObjectsByType(T("bosss"), FindObjectsSortMode.None)) UnityEngine.Object.Destroy(((Component)b0).gameObject);
        GoStage(2);
        Weapon(1);
        yield return Wait(10f);
        yield return Volley();
        yield return Wait(0.8f);
        Save(Screen(cam), "screenshots/05_meadow_storm.png");

        // ---------------- 6: 킹 슬라임
        SpawnBoss(2);
        yield return Wait(2.5f);
        Save(Screen(cam), "screenshots/06_king_slime.png");

        // ---------------- 아이콘 · 업적 아이콘 (UI만 그림)
        GameUI(false);
        yield return Icons(cam);
    }

    IEnumerator Icons(Camera cam)
    {
        Canvas art = ArtCanvas(cam);
        int mask = cam.cullingMask;
        cam.cullingMask = 1 << 5;
        Image bg = Img(art.transform, null, Color.white, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        Stretch(bg.rectTransform);
        Image inner = Img(art.transform, null, Color.white, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        Stretch(inner.rectTransform);
        Image pic = Img(art.transform, null, Color.white, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

        object data = Resources.Load("CodexData");
        GameObject[] bosses = (GameObject[])Get(data, "bosses");
        GameObject[] enemies = (GameObject[])Get(data, "enemies");
        Sprite Of(GameObject g) => Trim(g.GetComponentInChildren<SpriteRenderer>(true).sprite);
        Sprite Fx(string n) => Trim(Resources.LoadAll<Sprite>("FX/" + n).OrderBy(s => s.name.Length).ThenBy(s => s.name).First());
        Sprite playerSprite = Trim(((Component)Player()).GetComponent<SpriteRenderer>().sprite);

        // 게임 아이콘 1024 · 커뮤니티 아이콘 184
        bg.color = new Color(0.36f, 0.3f, 0.2f);
        inner.color = new Color(0.1f, 0.07f, 0.14f);
        inner.rectTransform.offsetMin = Vector2.one * 24f; inner.rectTransform.offsetMax = -Vector2.one * 24f;
        pic.sprite = playerSprite; pic.preserveAspect = true;
        foreach (int size in new[] { 1024, 184 })
        {
            float k = size / 1024f;
            inner.rectTransform.offsetMin = Vector2.one * 40f * k; inner.rectTransform.offsetMax = -Vector2.one * 40f * k;
            pic.rectTransform.sizeDelta = Vector2.one * size * 0.72f;
            yield return null;
            Texture2D t = Render(cam, size, size, false);
            Save(t, size == 1024 ? "icon/GunSaverIcon.png" : "capsules/community_icon.jpg", size != 1024);
        }

        (string id, Sprite s, Color c)[] ach =
        {
            ("ACH_FIRST_ULT", Fx("fx_reticle"), new Color(0.55f, 0.35f, 0.15f)),
            ("ACH_ENTER_HELL", Of(enemies[3]), new Color(0.45f, 0.08f, 0.06f)),
            ("ACH_ENTER_MEADOW", Of(enemies[8]), new Color(0.15f, 0.35f, 0.12f)),
            ("ACH_MIDBOSS", Fx("fx_warn"), new Color(0.5f, 0.12f, 0.1f)),
            ("ACH_EVOLVE", Fx("fx_rune"), new Color(0.45f, 0.35f, 0.08f)),
            ("ACH_LEVEL_10", Fx("fx_prompt"), new Color(0.2f, 0.25f, 0.5f)),
            ("ACH_KILLS_500", Fx("fx_markskull"), new Color(0.3f, 0.12f, 0.4f)),
            ("ACH_BOSS_LICH", Of(bosses[0]), new Color(0.25f, 0.18f, 0.4f)),
            ("ACH_BOSS_DEMON", Of(bosses[1]), new Color(0.5f, 0.15f, 0.05f)),
            ("ACH_CLEAR", Of(bosses[2]), new Color(0.6f, 0.5f, 0.15f)),
        };
        foreach (var a in ach)
        {
            bg.color = new Color(0.85f, 0.72f, 0.4f);
            inner.color = a.c;
            inner.rectTransform.offsetMin = Vector2.one * 10f; inner.rectTransform.offsetMax = -Vector2.one * 10f;
            pic.sprite = a.s;
            bool tint = a.id == "ACH_FIRST_ULT" || a.id == "ACH_EVOLVE" || a.id == "ACH_LEVEL_10" || a.id == "ACH_KILLS_500";
            pic.color = tint ? Gold : Color.white;
            pic.rectTransform.sizeDelta = Vector2.one * 256f * 0.66f;
            yield return null;
            Texture2D t = Render(cam, 256, 256, false);
            Save(t, "achievements/" + a.id + ".jpg", true);
            Save(Gray(t), "achievements/" + a.id + "_locked.jpg", true);
        }
        cam.cullingMask = mask;
        UnityEngine.Object.Destroy(art.gameObject);
    }
}

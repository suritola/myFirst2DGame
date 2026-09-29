using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 실제 게임 플레이 트레일러 + 스팀 스크린샷 (1.8.6~, tools/trailer/record.ps1 로 프로젝트 복사본에서 배치 실행)
// 게임 씬을 자동 조종으로 플레이하며 매 프레임 카메라를 MP4로 인코딩하고, 중요한 순간은 PNG 스크린샷으로 저장
// 흐름: 거너 전투 · 레벨업 카드 → 무기 진화 → 영혼 트리 → 전설 스킨 영웅 4명 · 보스 3종 → 스킨 상점 → 로고
public class GameplayCapture
{
    static readonly string Out = Environment.GetEnvironmentVariable("CAPTURE_OUT") ?? "C:/Temp/SoulSaverCapture";
    static readonly int Fps = int.TryParse(Environment.GetEnvironmentVariable("CAPTURE_FPS"), out int f) ? f : 30;
    static readonly string Quality = Environment.GetEnvironmentVariable("CAPTURE_QUALITY") ?? "Medium";
    const int W = 1920, H = 1080;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);

    static Type T(string n) => Type.GetType(n + ", Assembly-CSharp");
    static object Get(object o, string f) => o.GetType().GetField(f, Any)?.GetValue(o);
    static void Set(object o, string f, object v) => o.GetType().GetField(f, Any).SetValue(o, v);
    static object Call(object o, string m, params object[] a) => o.GetType().GetMethod(m, Any).Invoke(o, a);
    static void In(string f, object v) => T("GameInput").GetField(f).SetValue(null, v);
    static UnityEngine.Object Find(string type) => UnityEngine.Object.FindFirstObjectByType(T(type));

    // ================================================================= 녹화기 (씬이 바뀌어도 남음)
    class Rec : MonoBehaviour
    {
        public bool recording, autopilot, levelUpAllowed;
        public float musicVolume = 0.9f;
        public int frames;
        public Canvas captionCanvas;
        public TextMeshProUGUI caption, logo, sub;
        public Image black;

        MediaEncoder encoder;
        NativeArray<float> audio;
        RenderTexture rt;
        Texture2D frame;
        float[] music;
        int musicCh, musicRate, musicFrames, cursor, perFrame;
        const int Rate = 48000;
        float nextTap;
        bool held;

        public void Begin(string path)
        {
            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            frame = new Texture2D(W, H, TextureFormat.RGBA32, false);
            AudioClip clip = Resources.Load<AudioClip>("Music/bgm_boss");
            music = new float[clip.samples * clip.channels];
            clip.GetData(music, 0);
            musicCh = clip.channels; musicRate = clip.frequency; musicFrames = clip.samples;
            perFrame = Rate / Fps;
            var mode = (UnityEditor.VideoBitrateMode)Enum.Parse(typeof(UnityEditor.VideoBitrateMode), Quality);
            var v = new VideoTrackAttributes { frameRate = new MediaRational(Fps), width = W, height = H, includeAlpha = false, bitRateMode = mode };
            var a = new AudioTrackAttributes { sampleRate = new MediaRational(Rate), channelCount = (ushort)musicCh, language = "en" };
            encoder = new MediaEncoder(path, v, a);
            audio = new NativeArray<float>(perFrame * musicCh, Allocator.Persistent);
        }

        public void End()
        {
            recording = false;
            encoder?.Dispose();
            encoder = null;
            if (audio.IsCreated) audio.Dispose();
        }

        // 매 프레임 게임 처리가 끝난 뒤 카메라를 그려 영상에 붙임 (UI 캔버스도 카메라로 옮겨서 함께 찍힘)
        // 배치 모드에서는 WaitForEndOfFrame 이 오지 않을 수 있어 LateUpdate 에서 찍음
        void Capture()
        {
            {
                if (!recording || encoder == null) return;
                Texture2D t = Grab(true);
                encoder.AddFrame(t);
                for (int i = 0; i < perFrame; i++)
                {
                    double pos = (cursor + i) * (double)musicRate / Rate;
                    int i0 = (int)pos % musicFrames, i1 = (i0 + 1) % musicFrames;
                    float k = (float)(pos - Math.Floor(pos));
                    for (int c = 0; c < musicCh; c++)
                        audio[i * musicCh + c] = Mathf.Lerp(music[i0 * musicCh + c], music[i1 * musicCh + c], k) * musicVolume;
                }
                cursor += perFrame;
                encoder.AddSamples(audio);
                frames++;
            }
        }

        public Texture2D Grab(bool withCaption)
        {
            Camera cam = Camera.main;
            if (cam == null) return frame;
            cam.cullingMask |= 1 << 5;
            CanvasesToCamera(cam);
            captionCanvas.worldCamera = cam;
            captionCanvas.enabled = withCaption;
            RenderTexture old = cam.targetTexture;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.targetTexture = old;
            captionCanvas.enabled = true;
            RenderTexture.active = rt;
            frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            frame.Apply();
            RenderTexture.active = null;
            return frame;
        }

        // ---------------- 자동 조종: 가까운 적(보스 먼저)을 겨누고 옆으로 돌며 싸움
        void Update()
        {
            if (!autopilot) return;
            Component player = (Component)Find("PlayerController");
            if (player == null) return;
            Vector3 me = player.transform.position;
            Vector3? target = Nearest(me, out bool boss);
            if (target.HasValue)
            {
                Vector2 to = target.Value - me;
                float d = to.magnitude;
                Vector2 side = new Vector2(-to.y, to.x).normalized;
                // 보스는 덩치가 커서 멀리서 싸움 (영웅이 보스 그림에 가려지지 않게)
                float near = boss ? 6.5f : 3.5f, far = boss ? 9f : 7f;
                In("AutoAim", target.Value);
                In("AutoMove", side * 0.75f + (d < near ? -to.normalized * 1.2f : d > far ? to.normalized * 0.7f : Vector2.zero));
            }
            else
            {
                In("AutoMove", Vector2.zero);
                In("AutoAim", me + Vector3.right * 5f);
            }
            // 눌렀다 떼기를 반복 (총 · 베기 · 활 시위 · 플라스크 모두 쏘게)
            if (Time.time >= nextTap)
            {
                held = !held;
                if (held) { In("FireDownFrame", Time.frameCount + 1); In("AutoFire", true); nextTap = Time.time + 0.42f; }
                else { In("FireUpFrame", Time.frameCount + 1); In("AutoFire", false); nextTap = Time.time + 0.06f; }
            }
        }

        // 죽지 않고, 보여 주려는 때가 아니면 레벨업 창이 뜨지 않게
        void LateUpdate()
        {
            object p = Find("PlayerController");
            if (p != null)
            {
                Set(p, "PlayerHealth", Get(p, "PlayerMaxHealth"));
                if (!levelUpAllowed) Set(p, "nowEXP", 0f);
            }
            Capture();
        }

        static Vector3? Nearest(Vector3 from, out bool boss)
        {
            Vector3? best = null; float bd = 16f * 16f;
            boss = false;
            foreach (string type in new[] { "bosss", "EnermyController" })
            {
                boss = type == "bosss";
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T(type), FindObjectsSortMode.None))
                {
                    if (o.GetType().GetProperty("IsDead")?.GetValue(o) is bool dead && dead) continue;
                    Vector3 p = ((Component)o).transform.position;
                    float d = (p - from).sqrMagnitude;
                    if (d < bd) { bd = d; best = p; }
                }
                if (best.HasValue) return best;
            }
            return best;
        }
    }

    static void CanvasesToCamera(Camera cam)
    {
        SortingLayer[] layers = SortingLayer.layers;
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 5f;
                c.overrideSorting = true;
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32000, 30000 + c.sortingOrder);
            }
            // 진화 카드 · 영혼 트리처럼 자체 정렬을 쓰는 하위 캔버스도 가장 위 레이어로 (안 그러면 맵 그림 아래로 깔림)
            else if (!c.isRootCanvas && c.overrideSorting && c.sortingLayerName != layers[layers.Length - 1].name)
            {
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = Mathf.Min(32400, 31000 + c.sortingOrder);
            }
    }

    // ================================================================= 연출 도구
    static Rec rec;

    // 녹화 중에는 게임 시간이 1/Fps 씩 흐르므로 프레임 수로 기다림
    static IEnumerator F(float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
        for (int i = 0; i < n; i++) yield return null;
    }

    static void Shot(string name)
    {
        Texture2D t = rec.Grab(false);
        string path = Path.Combine(Out, "screenshots", name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, t.EncodeToPNG());
        Debug.Log("[CAP] " + name);
    }

    static IEnumerator Fade(float from, float to, float time)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(time * Fps));
        for (int i = 0; i <= n; i++) { rec.black.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, i / (float)n)); yield return null; }
    }

    static IEnumerator Caption(string text, float time)
    {
        rec.caption.text = text;
        int n = Mathf.RoundToInt(time * Fps);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Fps;
            rec.caption.alpha = Mathf.Min(Mathf.Clamp01(t / 0.3f), Mathf.Clamp01((time - t) / 0.35f));
            yield return null;
        }
        rec.caption.alpha = 0f;
    }

    static void Say(string text, float time) => rec.StartCoroutine(Caption(text, time));

    static IEnumerator Load(string hero, int stage)
    {
        Type cd = T("CharacterData"), cid = T("CharacterId");
        cd.GetField("Override", Any).SetValue(null, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(cid), Enum.Parse(cid, hero)));
        SceneManager.LoadScene("GameScene");
        yield return null; yield return null; yield return null;
        yield return CloseLevelUps();
        if (stage > 0) GoStage(stage);
        Swarm(stage, 12);
        yield return null;
    }

    // 레벨업 카드 확정 (게임에서는 카드를 누른 뒤 [Space])
    static void ConfirmCard(object shop, int slot)
    {
        string f = slot == 0 ? "first" : slot == 1 ? "second" : "third";
        Set(shop, "pendingSlot", -1);
        Call(shop, "onSelect", Get(shop, f));
    }

    // 판을 시작하면 뜨는 첫 카드 창과 쌓인 레벨업을 정리
    static IEnumerator CloseLevelUps()
    {
        for (int i = 0; i < 6; i++)
        {
            yield return null;
            object shop = Find("LevelShop");
            if (shop != null && (bool)shop.GetType().GetProperty("IsOpen").GetValue(shop)) ConfirmCard(shop, 0);
        }
        Time.timeScale = 1f;
    }

    static void GoStage(int stage)
    {
        object sm = Find("StageManager");
        foreach (Renderer r in (Renderer[])Get(sm, "caveRenderers")) if (r != null) r.enabled = stage == 0;
        ((GameObject)Get(sm, "hellMap"))?.SetActive(stage == 1);
        ((GameObject)Get(sm, "meadowMap"))?.SetActive(stage == 2);
        Camera.main.backgroundColor = (Color)Get(sm, stage == 1 ? "hellBackground" : "meadowBackground");
        sm.GetType().GetProperty("CurrentStage", Any).SetValue(sm, stage);
        object sp = Get(sm, "spawner");
        sp.GetType().GetMethod("StartStage").Invoke(sp, new object[] { stage });
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, false);
        ((Transform)Get(sm, "player")).position = (Vector2)Get(sm, "stage2PlayerStart");
    }

    // 주인공 둘레에 그 스테이지 적을 깔아 둠 (화면이 비지 않게)
    static void Swarm(int stage, int count)
    {
        object data = Resources.Load("CodexData");
        var enemies = (GameObject[])Get(data, "enemies");
        var stages = (int[])Get(data, "enemyStage");
        var pool = new List<GameObject>();
        for (int i = 0; i < enemies.Length; i++) if (stages[i] == stage) pool.Add(enemies[i]);
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            float a = i / (float)count * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
            UnityEngine.Object.Instantiate(pool[i % pool.Count], me + new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a)) * UnityEngine.Random.Range(5.5f, 8.5f), Quaternion.identity);
        }
    }

    static IEnumerator Ult(float hold)
    {
        object g = Find("SkillGauge");
        if (g != null) g.GetType().GetField("SkillPoint").SetValue(g, 99999f);
        yield return null;
        In("AutoUlt", true);
        In("UltDownFrame", Time.frameCount + 1);
        yield return F(Mathf.Max(hold, 0.05f));
        In("AutoUlt", false);
        In("UltUpFrame", Time.frameCount + 1);
        yield return null; yield return null;
    }

    static object SpawnBoss(int stage, float hp)
    {
        object data = Resources.Load("CodexData");
        GameObject prefab = ((GameObject[])Get(data, "bosses"))[stage];
        Vector3 me = ((Component)Find("PlayerController")).transform.position;
        GameObject go = UnityEngine.Object.Instantiate(prefab, me + new Vector3(8.5f, 3.5f, 0f), Quaternion.identity);
        object bar = Find("bossbar");
        bar.GetType().GetField("bossSpawn").SetValue(bar, true);
        object sp = Get(Find("StageManager"), "spawner");
        sp.GetType().GetField("bossSpawned", Any)?.SetValue(sp, true);
        object b = go.GetComponent(T("bosss"));
        if (b != null) Set(b, "EnemyHealth", Convert.ToSingle(Get(b, "setEnemyHP")) * hp);
        // 보스는 제자리에서 기술만 씀 (쫓아와서 영웅을 덮으면 화면에서 영웅이 안 보임)
        if (b != null && b.GetType().GetField("speed", Any) is FieldInfo speed) speed.SetValue(b, Convert.ChangeType(0, speed.FieldType));
        return b;
    }

    // 전설 스킨 장착 (복사본 에디터 PlayerPrefs 에만 저장됨 · 끝나면 지움)
    static readonly List<string> skinKeys = new List<string>();
    static void WearLegend(int who)
    {
        Type sd = T("SkinData");
        foreach (object s in (Array)sd.GetField("All").GetValue(null))
        {
            if (Convert.ToInt32(Get(s, "tier")) != 3 || Convert.ToInt32(Get(s, "who")) != who || Get(s, "kind").ToString() == "Effect") continue;
            string id = (string)Get(s, "id");
            PlayerPrefs.SetInt("skin.own." + id, 1);
            skinKeys.Add("skin.own." + id);
            skinKeys.Add("skin.eq." + Convert.ToInt32(Get(s, "kind")) + "." + who);
            sd.GetMethod("Equip").Invoke(null, new[] { s });
        }
    }

    static void BuildOverlay()
    {
        GameObject go = new GameObject("CaptureOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.layer = 5;
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.planeDistance = 1f;
        c.overrideSorting = true;
        SortingLayer[] layers = SortingLayer.layers;
        c.sortingLayerName = layers[layers.Length - 1].name;
        c.sortingOrder = 32500;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(W, H);
        cs.matchWidthOrHeight = 1f;
        rec.captionCanvas = c;

        rec.black = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        rec.black.transform.SetParent(go.transform, false);
        RectTransform br = rec.black.rectTransform;
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
        rec.black.color = Color.black;

        TMP_FontAsset font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name.StartsWith("Cafe24"));
        Material outline = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name.StartsWith("Cafe24") && m.name.Contains("Outline"));
        rec.caption = Text(go.transform, font, outline, 66f, new Vector2(0f, -400f));
        rec.logo = Text(go.transform, font, outline, 230f, new Vector2(0f, 60f));
        rec.logo.text = "Soul Saver";
        rec.sub = Text(go.transform, font, null, 54f, new Vector2(0f, -150f));
        rec.sub.text = "WISHLIST NOW ON STEAM";
        rec.sub.color = new Color(0.92f, 0.88f, 0.8f);
        rec.caption.alpha = rec.logo.alpha = rec.sub.alpha = 0f;
    }

    static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, Material mat, float size, Vector2 pos)
    {
        TextMeshProUGUI t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.gameObject.layer = 5;
        t.transform.SetParent(parent, false);
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(1800f, size * 1.3f);
        if (font != null) t.font = font;
        if (mat != null) t.fontSharedMaterial = mat;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Gold;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    // ================================================================= 장면
    // 한 영웅 장면: 불러오기(녹화 멈춤) → 싸움 → 필살기
    static IEnumerator Hero(string hero, int who, int stage, int boss, float fight, float ultHold, string shot)
    {
        rec.recording = false;
        rec.autopilot = false;
        WearLegend(who);
        yield return Load(hero, stage);
        object b = boss >= 0 ? SpawnBoss(boss, 0.75f) : null;
        rec.autopilot = true;
        yield return F(1.2f);                       // 적이 다가올 시간 (녹화 안 함)
        rec.recording = true;
        yield return Fade(1f, 0f, 0.2f);
        yield return F(fight * 0.6f);
        if (shot != null) Shot(shot);
        yield return F(fight * 0.4f);
        yield return Ult(ultHold);
        yield return F(1.1f);
        yield return Fade(0f, 1f, 0.2f);
    }

    [UnityTest]
    public IEnumerator Capture()
    {
        LogAssert.ignoreFailingMessages = true;
        Directory.CreateDirectory(Out);
        Time.captureFramerate = Fps;
        In("Auto", true);                          // 업적 · 누적 기록이 풀리지 않게
        GameObject host = new GameObject("CaptureRec");
        UnityEngine.Object.DontDestroyOnLoad(host);
        rec = host.AddComponent<Rec>();
        BuildOverlay();

        // ---------------- 1) 거너 · 지하 묘역 전투 → 레벨업 카드
        yield return Load("Gunner", 0);
        rec.autopilot = true;
        yield return F(1.2f);
        rec.Begin(Path.Combine(Out, "SoulSaver_Trailer.mp4"));
        rec.recording = true;
        yield return Fade(1f, 0f, 0.6f);
        Say("THE DEAD HAVE RISEN.", 2.4f);
        yield return F(2.6f);
        yield return Ult(1.2f);
        yield return F(0.8f);
        Shot("01_gunner_catacombs");
        yield return F(0.6f);

        object shop = Find("LevelShop");
        rec.levelUpAllowed = true;
        Call(shop, "openLevelShop");
        Say("BUILD YOUR RUN.", 2.2f);
        yield return F(0.9f);
        Call(shop, "onSecondButton");            // 가운데 카드를 고름 (금빛)
        yield return F(0.5f);
        Shot("02_level_up_cards");
        yield return F(0.6f);
        ConfirmCard(shop, 1);
        rec.levelUpAllowed = false;
        yield return F(0.6f);

        // ---------------- 2) 무기 진화 (보스를 쓰러뜨렸을 때 나오는 화면)
        object sm = Find("StageManager");
        rec.autopilot = false;
        In("AutoMove", Vector2.zero);
        In("AutoFire", false);
        ((MonoBehaviour)sm).StartCoroutine((IEnumerator)Call(sm, "Evolution", false));
        Say("DEFEAT A BOSS.  EVOLVE YOUR WEAPON.", 4.2f);
        yield return F(2.6f);
        object evo = Find("WeaponEvolutionUI");
        if (evo != null) Call(evo, "Select", 0);
        yield return F(0.8f);
        Shot("03_weapon_evolution");
        yield return F(0.4f);
        if (evo != null) Set(evo, "confirmed", true);
        yield return F(0.5f);
        rec.autopilot = true;
        yield return F(2.2f);
        yield return Ult(0.8f);
        yield return F(1f);

        // ---------------- 3) 영혼 트리
        rec.autopilot = false;
        In("AutoMove", Vector2.zero);
        In("AutoFire", false);
        Type shards = T("SoulShards");
        shards.GetMethod("Add").Invoke(null, new object[] { 4000, Vector3.zero, false });
        Call(sm, "OpenSoulTree");
        Say("GROW A SOUL TREE OF 100+ NODES.", 4.6f);
        yield return F(0.8f);
        object specials = Get(sm, "specials");
        // 트리 화면의 칸 누르기와 같은 처리 (새 칸이 드러나고 반짝임) · 여섯 가지를 돌아가며 배움
        object tree = T("SoulTreeUI").GetProperty("Instance").GetValue(null);
        for (int i = 0; i < 14 && tree != null; i++)
        {
            object pick = null;
            foreach (object v in (IList)Get(tree, "views"))
            {
                object n = Get(v, "node");
                if (!(bool)Call(specials, "CanBuy", n)) continue;
                if (pick == null) pick = v;
                if (Convert.ToInt32(Get(n, "branch")) == i % 6) { pick = v; break; }
            }
            if (pick != null) Call(tree, "Buy", pick);
            yield return F(0.26f);
            if (i == 11) Shot("04_soul_tree");
        }
        yield return F(0.6f);
        T("SoulTreeUI").GetMethod("Close").Invoke(null, null);
        yield return F(0.2f);
        yield return Fade(0f, 1f, 0.25f);

        // ---------------- 4) 전설 스킨을 입은 영웅들 · 세 세계의 왕
        rec.caption.alpha = 0f;
        rec.StartCoroutine(Delayed(0.4f, () => Say("FIVE HEROES.  THREE WORLDS.  THREE KINGS.", 4.5f)));
        yield return Hero("Swordsman", 1, 1, 1, 3.2f, 1.0f, "05_swordsman_demon_lord");
        yield return Hero("Archer", 3, 0, 0, 3.0f, 0.9f, "06_archer_lich_king");
        yield return Hero("Rogue", 2, 2, 2, 3.0f, 0.05f, "07_rogue_king_slime");
        yield return Hero("Alchemist", 4, 1, -1, 2.6f, 1.0f, null);

        // ---------------- 5) 스킨 상점
        rec.recording = false;
        rec.autopilot = false;
        T("CharacterData").GetField("Override", Any).SetValue(null, null);
        In("Auto", true);
        SceneManager.LoadScene("MainMenu");
        yield return F(1.5f);
        GameObject skinButton = GameObject.Find("SkinButton");
        skinButton?.GetComponent<Button>()?.onClick.Invoke();
        yield return F(0.8f);
        rec.recording = true;
        yield return Fade(1f, 0f, 0.25f);
        Say("20 CHARACTER SKINS.  10 WEAPON SKINS.", 3f);
        yield return F(1.6f);
        Shot("08_skin_shop");
        yield return F(1.4f);

        // ---------------- 6) 로고
        yield return Fade(0f, 1f, 0.5f);
        for (int i = 0; i <= Fps; i++)
        {
            float k = Mathf.SmoothStep(0f, 1f, i / (float)Fps);
            rec.logo.alpha = k;
            rec.sub.alpha = Mathf.Clamp01(k * 1.5f - 0.5f);
            rec.logo.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k);
            yield return null;
        }
        int tail = Fps * 3;
        for (int i = 0; i < tail; i++) { rec.musicVolume = 0.9f * (1f - i / (float)tail); yield return null; }

        rec.End();
        foreach (string k in skinKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Time.captureFramerate = 0;
        In("Auto", false);
        Debug.Log("[CAP] done frames=" + rec.frames + " (" + (rec.frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }

    static IEnumerator Delayed(float s, Action a) { yield return F(s); a(); }
}

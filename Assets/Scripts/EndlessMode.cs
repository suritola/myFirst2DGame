using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

// 무한 모드 (어려움을 클리어하면 열림): 불타는 사막 맵에서 1 · 2 · 3장의 적이 모두 나오고
// 리치 왕 → 지옥의 군주 → 킹 슬라임이 번갈아 주기적으로 등장. 죽을 때까지 버팀
public class EndlessMode : MonoBehaviour
{
    public static EndlessMode Instance { get; private set; }

    // 마지막 판 기록 (게임 오버 화면에서 보여 줌)
    public static float LastSeconds = -1f;
    public static int LastBosses;
    const string BestKey = "endless.bestSeconds";
    public static float BestSeconds => PlayerPrefs.GetFloat(BestKey, 0f);

    const float FirstBossAt = 90f;          // 시작 후 첫 보스
    const float BossEvery = 150f;           // 보스를 쓰러뜨린 뒤 다음 보스까지

    public int BossesDefeated { get; private set; }

    StageManager stages;
    EnemySpawner spawner;
    GameObject[] bossPrefabs;
    float nextBoss;
    int bossTurn;
    TextMeshProUGUI hud;

    public void Init(StageManager sm, GameObject[] bosses)
    {
        Instance = this;
        stages = sm;
        spawner = sm.spawner;
        bossPrefabs = bosses;
        nextBoss = Time.time + FirstBossAt;
        BuildHud();
    }

    void Update()
    {
        if (spawner == null) return;
        float s = GameMode.EndlessSeconds;
        if (hud != null)
            hud.text = Loc.T("무한 모드") + "  " + Clock(s) + "   " + Loc.T("보스 처치 ") + BossesDefeated
                     + (BestSeconds > 0f ? "   " + Loc.T("최고 ") + Clock(BestSeconds) : "");

        if (!spawner.bossSpawned && Time.time >= nextBoss && Time.timeScale > 0f && !stages.IsMenuOpen) SpawnBoss();
    }

    void SpawnBoss()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return;
        GameObject prefab = bossPrefabs[bossTurn % bossPrefabs.Length];
        bossTurn++;
        nextBoss = float.MaxValue;              // 쓰러뜨리면 다시 정함
        spawner.bossSpawned = true;
        spawner.bossCleared = false;
        if (stages.bossBar != null) stages.bossBar.bossSpawn = true;
        Instantiate(prefab, spawner.bossSpawnPoint, Quaternion.identity);
        Hostile.Play("roar", 1f, 0.9f);
        stages.ShowBanner(Loc.T("사막의 모래 폭풍 속에서 보스가 나타났다!"), 2.5f);
    }

    // StageManager가 보스 처치를 알려 줌
    public void OnBossDefeated()
    {
        BossesDefeated++;
        spawner.bossCleared = false;            // 다음 보스를 다시 부를 수 있게
        nextBoss = Time.time + BossEvery;
        stages.specialPoints++;
        stages.ShowBanner(Loc.T("보스 처치!  특수 능력 포인트 +1"), 3f);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        // 이번 판 기록 (죽어서 게임 오버로 넘어갈 때)
        LastSeconds = GameMode.EndlessSeconds;
        LastBosses = BossesDefeated;
        Instance = null;
        if (LastSeconds > BestSeconds && !GameInput.Auto)
        {
            PlayerPrefs.SetFloat(BestKey, LastSeconds);
            PlayerPrefs.Save();
        }
    }

    public static string Clock(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    void BuildHud()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        GameObject go = new GameObject("EndlessHud", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(1200f, 50f);
        r.anchoredPosition = new Vector2(0f, -40f);
        hud = go.GetComponent<TextMeshProUGUI>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) hud.font = UIKit.Font;
        if (UIKit.FontMaterial != null) hud.fontSharedMaterial = UIKit.FontMaterial;
        hud.fontSize = 30f;
        hud.color = new Color(1f, 0.78f, 0.45f);
        hud.alignment = TextAlignmentOptions.Center;
        hud.raycastTarget = false;
    }

    // ================================================================= 스테이지 구성
    // 1 · 2 · 3장의 적을 모두 모은 4번째 스테이지 (보스는 EndlessMode가 따로 부름)
    public static StageConfig[] WithEndlessStage(StageConfig[] stages, out GameObject[] bosses)
    {
        List<GameObject> enemies = new List<GameObject>();
        List<float> tiers = new List<float>();
        List<GameObject> bossList = new List<GameObject>();
        for (int s = 0; s < stages.Length && s < 3; s++)
        {
            StageConfig st = stages[s];
            if (st.bossPrefab != null) bossList.Add(st.bossPrefab);
            if (st.enemies == null) continue;
            for (int i = 0; i < st.enemies.Length; i++)
            {
                if (st.enemies[i] == null || enemies.Contains(st.enemies[i])) continue;
                enemies.Add(st.enemies[i]);
                tiers.Add(s + i / (float)Mathf.Max(1, st.enemies.Length));    // 0 ~ 3: 약한 적 → 강한 적
            }
        }
        bosses = bossList.ToArray();

        // 처치 수에 따라 강한 적 비율이 늘어나는 페이즈 6개
        int[] kills = { 0, 30, 70, 120, 180, 250 };
        float[] interval = { 1.1f, 0.95f, 0.85f, 0.75f, 0.68f, 0.6f };
        int[] alive = { 10, 12, 14, 16, 18, 20 };
        SpawnPhase[] phases = new SpawnPhase[kills.Length];
        for (int p = 0; p < phases.Length; p++)
        {
            float focus = p * 0.55f;                            // 이 페이즈에 가장 많이 나오는 등급
            float[] w = new float[enemies.Count];
            for (int i = 0; i < w.Length; i++) w[i] = Mathf.Max(0.15f, 1.2f - Mathf.Abs(tiers[i] - focus) * 0.6f);
            phases[p] = new SpawnPhase { killsToEnter = kills[p], spawnInterval = interval[p], maxAlive = alive[p], weights = w };
        }
        float[] minions = new float[enemies.Count];
        for (int i = 0; i < minions.Length; i++) minions[i] = 1f;

        StageConfig endless = new StageConfig
        {
            stageName = "Endless",
            enemies = enemies.ToArray(),
            phases = phases,
            bossKills = int.MaxValue,
            bossPrefab = null,
            minionWeights = minions,
        };
        List<StageConfig> list = new List<StageConfig>(stages);
        while (list.Count > 3) list.RemoveAt(list.Count - 1);
        list.Add(endless);
        return list.ToArray();
    }

    // ================================================================= 불타는 사막 맵
    // 지옥 맵 배치를 복제하고 타일 그림만 사막 도트(Resources/Desert)로 바꾼 뒤 소품 · 불티를 뿌림
    public static GameObject BuildDesertMap(GameObject hellMap, Vector2 areaMin, Vector2 areaMax, Vector3 keepClear)
    {
        if (hellMap == null) return null;
        GameObject map = Instantiate(hellMap, hellMap.transform.parent);
        map.name = "DesertMap";
        map.SetActive(true);

        Dictionary<string, Sprite> desert = new Dictionary<string, Sprite>();
        foreach (Sprite s in Resources.LoadAll<Sprite>("Desert")) desert[s.name] = s;
        Dictionary<Sprite, Tile> tiles = new Dictionary<Sprite, Tile>();
        int sorting = 0, order = 0;

        foreach (Tilemap tm in map.GetComponentsInChildren<Tilemap>(true))
        {
            TilemapRenderer tr = tm.GetComponent<TilemapRenderer>();
            if (tr != null && tr.sortingOrder >= order) { order = tr.sortingOrder; sorting = tr.sortingLayerID; }
            BoundsInt b = tm.cellBounds;
            foreach (Vector3Int pos in b.allPositionsWithin)
            {
                Sprite s = tm.GetSprite(pos);
                if (s == null || !desert.TryGetValue(s.name.Replace("_hell", "_desert"), out Sprite d)) continue;
                if (!tiles.TryGetValue(d, out Tile tile))
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = d;
                    tiles[d] = tile;
                }
                Matrix4x4 m = tm.GetTransformMatrix(pos);
                Color c = tm.GetColor(pos);
                tm.SetTile(pos, tile);
                tm.SetTileFlags(pos, TileFlags.None);
                tm.SetTransformMatrix(pos, m);
                tm.SetColor(pos, c);
            }
        }

        ScatterProps(map.transform, desert, areaMin, areaMax, keepClear, sorting, order + 1);
        map.AddComponent<DesertAmbience>();
        return map;
    }

    static void ScatterProps(Transform parent, Dictionary<string, Sprite> desert, Vector2 min, Vector2 max, Vector3 keepClear, int sorting, int order)
    {
        (string name, float weight, float scale)[] kinds =
        {
            ("prop_cactus", 3f, 1.6f), ("prop_cactus_small", 3f, 1.5f), ("prop_rocks", 3f, 1.6f), ("prop_bush", 3f, 1.5f),
            ("prop_skull", 1.5f, 1.4f), ("prop_ribs", 1.2f, 1.6f), ("prop_brazier", 1f, 1.5f), ("prop_flame_pillar", 1f, 1.6f),
        };
        float total = 0f;
        foreach (var k in kinds) total += k.weight;

        Random.State saved = Random.state;
        Random.InitState(7031);                  // 매 판 같은 배치
        GameObject root = new GameObject("DesertProps");
        root.transform.SetParent(parent, false);
        int placed = 0;
        for (int tries = 0; tries < 900 && placed < 95; tries++)
        {
            Vector3 pos = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0f);
            if (Vector2.Distance(pos, keepClear) < 6f || Hostile.WallNear(pos, 1.2f)) continue;
            float roll = Random.Range(0f, total);
            var kind = kinds[kinds.Length - 1];
            foreach (var k in kinds) { roll -= k.weight; if (roll < 0f) { kind = k; break; } }
            if (!desert.TryGetValue(kind.name, out Sprite sprite)) continue;

            GameObject go = new GameObject(kind.name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * kind.scale * Random.Range(0.85f, 1.15f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = sorting;
            sr.sortingOrder = order;
            sr.flipX = Random.value < 0.5f;
            if (kind.name == "prop_brazier" || kind.name == "prop_flame_pillar") go.AddComponent<FireFlicker>();
            placed++;
        }
        Random.state = saved;
    }
}

// 화로 · 불기둥: 살랑거리며 불티가 튐
public class FireFlicker : MonoBehaviour
{
    Vector3 baseScale;
    float seed, nextSpark;

    void Start()
    {
        baseScale = transform.localScale;
        seed = Random.Range(0f, 10f);
    }

    void Update()
    {
        float t = Time.time * 7f + seed;
        transform.localScale = new Vector3(baseScale.x * (1f + 0.03f * Mathf.Sin(t * 1.3f)), baseScale.y * (1f + 0.07f * Mathf.Sin(t)), 1f);
        if (Time.time >= nextSpark)
        {
            nextSpark = Time.time + Random.Range(0.25f, 0.6f);
            Vector3 top = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.7f * baseScale.y, 0f);
            FlameParticle.Spawn(Hostile.Glow, top, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1f, 2f)), Random.Range(0.6f, 1f), 0.06f, 0.02f, false);
        }
    }
}

// 사막 전체: 화면 주변에 불티가 떠오르고 가끔 모래 바람이 스침
public class DesertAmbience : MonoBehaviour
{
    float nextEmber, nextGust;

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || Time.timeScale == 0f) return;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;

        if (Time.time >= nextEmber)
        {
            nextEmber = Time.time + 0.06f;
            Vector3 p = new Vector3(c.x + Random.Range(-w, w), c.y - h - 0.5f + Random.Range(0f, h * 0.6f), 0f);
            FlameParticle.Spawn(Hostile.Glow, p, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(1.2f, 2.6f)), Random.Range(1.4f, 2.4f), 0.035f, 0.012f, false);
        }
        if (Time.time >= nextGust)
        {
            nextGust = Time.time + Random.Range(2.5f, 5f);
            Vector3 from = new Vector3(c.x - w - 2f, c.y + Random.Range(-h, h), 0f);
            Fx.Play("fx_smoke", from + new Vector3(Random.Range(0f, w * 2f), 0f, 0f), Random.Range(3f, 5f), new Color(0.95f, 0.78f, 0.5f, 0.35f), 10f);
        }
    }
}

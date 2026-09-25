using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 진행: 보스 처치 → 신전 문 열림 → 특수 능력 선택 → 지옥 맵
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("참조")]
    public EnemySpawner spawner;
    public Transform player;
    public bossbar bossBar;

    [Header("맵")]
    public Renderer[] caveRenderers;        // 동굴 바닥/벽/소품 (끄기만 함, 벽 콜라이더는 그대로)
    public GameObject hellMap;
    public GameObject portal;               // 보스를 잡으면 켜지는 신전 문
    public Color hellBackground = new Color(0.12f, 0.03f, 0.03f);
    public Vector2 stage2PlayerStart = new Vector2(0.5f, 0f);
    public GameObject meadowMap;            // 3장 초원
    public Color meadowBackground = new Color(0.18f, 0.32f, 0.16f);
    // 무한 모드 불타는 사막 (지옥 맵을 복제해 만듦)
    public Color desertBackground = new Color(0.24f, 0.13f, 0.06f);
    GameObject desertMap;
    GameObject[] endlessBosses;

    [Header("UI")]
    public Image fade;                      // 전체 화면 검은 막
    public GameObject specialPanel;         // 특수 능력 선택 화면
    public SpecialTreeUI specialTree;
    public SpecialAbilities specials;
    public GameObject banner;               // 화면 중앙 알림
    public TextMeshProUGUI bannerText;

    public int[] chosenSpecials = new int[0];

    [Header("신전 문 강제 입장")]
    public float portalTimeLimit = 30f;     // 보스를 잡은 뒤 이 시간이 지나면 자동으로 들어감
    public float portalPullTime = 8f;       // 마지막 이 시간 동안 문 쪽으로 점점 세게 끌려감
    TextMeshProUGUI countdownText;
    LineRenderer portalGuide;

    [Header("특수 능력 포인트 (2장 중간 보스 보상)")]
    public int specialPoints = 0;
    // 특수 강화 키는 KeyBindings (기본 T)

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    GameObject upgradeButton;
    RectTransform upgradeGlow;
    Image upgradeGlowImage;
    TextMeshProUGUI upgradeText;
    bool upgradeOpen;

    // 특수 능력 화면(지옥 입장 / 강화)이 열려 있는지
    public bool IsMenuOpen => upgradeOpen || transitioning;

    public int CurrentStage { get; private set; }

    bool transitioning;
    int[] pendingPicks;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.onBossDefeated += OnBossDefeated;
            spawner.onMidBossSpawned += OnMidBossSpawned;
            spawner.onMidBossDefeated += OnMidBossDefeated;
        }

        if (hellMap != null) hellMap.SetActive(false);
        if (meadowMap != null) meadowMap.SetActive(false);
        if (portal != null) portal.SetActive(false);
        if (specialPanel != null) specialPanel.SetActive(false);
        if (banner != null) banner.SetActive(false);
        SetFade(0f);

        BuildUpgradeButton();
        // 2장 상점의 무기 강화 창
        gameObject.AddComponent<WeaponUpgradeShop>().Init(FindFirstObjectByType<Shop>(), specials, specialTree);
        // 상점의 필살기(스킬) 강화 창
        gameObject.AddComponent<SkillUpgradeShop>().Init(FindFirstObjectByType<Shop>(), specials, specialTree);

        StartCoroutine(Opening());
    }

    // ================================================================= 시작 (인트로 · 무한 모드)
    IEnumerator Opening()
    {
        bool spawnWas = spawner != null && spawner.spawningEnabled;
        if (spawner != null) spawner.spawningEnabled = false;
        yield return null;                                  // 트레일러 촬영이 켜질 때까지 한 프레임
        if (GameInput.TrailerRunning)
        {
            if (spawner != null) spawner.spawningEnabled = spawnWas;
            yield break;
        }

        bool endless = GameMode.IsEndless;
        if (endless) PrepareEndless();
        yield return StoryDirector.Intro(endless);
        if (endless) yield return PickEndlessSpecials();
        if (spawner != null) spawner.spawningEnabled = true;
        if (endless)
        {
            GameMode.StartEndlessClock();
            gameObject.AddComponent<EndlessMode>().Init(this, endlessBosses);
            ShowBanner(Loc.T("무한 모드 · 불타는 사막") + "\n" + Loc.T("얼마나 버틸 수 있을까?"), 3f);
        }
    }

    void PrepareEndless()
    {
        foreach (Renderer r in caveRenderers) if (r != null) r.enabled = false;
        if (spawner == null) return;
        desertMap = EndlessMode.BuildDesertMap(hellMap, spawner.spawnAreaMin, spawner.spawnAreaMax, stage2PlayerStart);
        if (Camera.main != null) Camera.main.backgroundColor = desertBackground;
        spawner.stages = EndlessMode.WithEndlessStage(spawner.stages, out endlessBosses);
        CurrentStage = 3;
        spawner.StartStage(3);
        spawner.spawningEnabled = false;
        if (player != null) player.position = stage2PlayerStart;
    }

    // 무한 모드는 처음부터 특수 능력 3개를 고르고 시작
    IEnumerator PickEndlessSpecials()
    {
        if (specialPanel == null || specialTree == null) yield break;
        transitioning = true;
        float before = Time.timeScale;
        Time.timeScale = 0f;
        pendingPicks = null;
        specialPanel.SetActive(true);
        specialTree.Open(specials, OnPickSpecial);
        while (pendingPicks == null) yield return null;
        chosenSpecials = pendingPicks;
        if (specials != null) specials.Equip(chosenSpecials);
        TooltipUI.Hide();
        specialPanel.SetActive(false);
        Time.timeScale = before;           // 시작 능력 카드 창이 떠 있으면 그대로 멈춰 있음
        transitioning = false;
    }

    void Update()
    {
        bool show = specialPoints > 0 && CurrentStage >= 1 && !transitioning && !upgradeOpen;
        if (upgradeButton != null)
        {
            if (upgradeButton.activeSelf != show) upgradeButton.SetActive(show);
            if (show)
            {
                // 멈춘 화면에서도 반짝이도록 실제 시간 사용
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                upgradeGlow.localScale = Vector3.one * (1f + 0.15f * pulse);
                upgradeGlowImage.color = new Color(1f, 0.72f, 0.3f, 0.3f + 0.45f * pulse);
                upgradeText.color = Color.Lerp(Gold, Color.white, pulse);
                upgradeText.text = Loc.T("특수 강화 [") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("]   포인트 ") + specialPoints;
            }
        }
        if (show && KeyBindings.Down(GameAction.Upgrade)) OpenUpgrade();
    }

    // ================================================================= 특수 능력 포인트
    void OnMidBossSpawned()
    {
        ShowBanner(Loc.T("중간 보스 등장!\n쓰러뜨리면 특수 능력 포인트 +1"), 3f);
    }

    void OnMidBossDefeated()
    {
        specialPoints++;
        ShowBanner(Loc.T("특수 능력 포인트 +1!\n[") + KeyBindings.Name(GameAction.Upgrade) + Loc.T("] 또는 아래 버튼으로 강화"), 3f);
    }

    public void OpenUpgrade()
    {
        // 상점, 레벨업, 일시정지 중에는 열지 않음
        if (upgradeOpen || specialPoints <= 0 || Time.timeScale == 0f || specialTree == null) return;
        upgradeOpen = true;
        Time.timeScale = 0f;
        specialPanel.SetActive(true);
        specialTree.OpenUpgrade(specials, specialPoints, OnUpgradeConfirm, CloseUpgrade);
    }

    // 가진 능력은 진화, 새 능력은 장착
    void OnUpgradeConfirm(int[] ids)
    {
        List<int> fresh = new List<int>();
        int evolvedCount = 0;
        foreach (int id in ids)
        {
            if (specials.Has(id)) specials.Evolve(id, evolvedCount++);
            else fresh.Add(id);
        }
        if (fresh.Count > 0) specials.Equip(fresh);
        chosenSpecials = new List<int>(specials.EquippedIds).ToArray();
        specialPoints = Mathf.Max(0, specialPoints - ids.Length);
        CloseUpgrade();
    }

    // 트레일러 촬영용: 연출 없이 바로 해당 스테이지 맵으로 (0 동굴, 1 지옥, 2 초원)
    public void JumpToStage(int stage, Vector3 caveStart)
    {
        foreach (Renderer r in caveRenderers) if (r != null) r.enabled = stage == 0;
        if (hellMap != null) hellMap.SetActive(stage == 1);
        if (meadowMap != null) meadowMap.SetActive(stage == 2);
        if (portal != null) portal.SetActive(false);
        if (bossBar != null) bossBar.bossSpawn = false;
        if (Camera.main != null && stage > 0) Camera.main.backgroundColor = stage == 1 ? hellBackground : meadowBackground;
        CurrentStage = stage;
        spawner.StartStage(stage);
        if (player != null) player.position = stage == 0 ? caveStart : (Vector3)stage2PlayerStart;
    }

    // 트레일러 촬영용: 스킬 트리 닫기
    public void CloseUpgradeNow() { if (upgradeOpen) CloseUpgrade(); }

    void CloseUpgrade()
    {
        TooltipUI.Hide();
        specialPanel.SetActive(false);
        upgradeOpen = false;
        Time.timeScale = 1f;
    }

    // 경험치 바 위 가운데에 반짝이는 버튼
    void BuildUpgradeButton()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || specialTree == null) return;

        upgradeButton = new GameObject("SpecialUpgradeButton", typeof(RectTransform));
        RectTransform root = upgradeButton.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        if (banner != null) root.SetSiblingIndex(banner.transform.GetSiblingIndex());
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(400f, 64f);
        root.anchoredPosition = new Vector2(70f, 128f);

        GameObject glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        upgradeGlow = glow.GetComponent<RectTransform>();
        upgradeGlow.SetParent(root, false);
        upgradeGlow.sizeDelta = new Vector2(560f, 170f);
        upgradeGlowImage = glow.GetComponent<Image>();
        upgradeGlowImage.sprite = specials != null ? specials.glowSprite : null;
        upgradeGlowImage.raycastTarget = false;

        GameObject face = new GameObject("Face", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform fr = face.GetComponent<RectTransform>();
        fr.SetParent(root, false);
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        Image img = face.GetComponent<Image>();
        img.sprite = specialTree.headerSprite;
        img.type = Image.Type.Sliced;
        Button b = face.GetComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        b.onClick.AddListener(OpenUpgrade);

        GameObject label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform lr = label.GetComponent<RectTransform>();
        lr.SetParent(fr, false);
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(12f, 4f);
        lr.offsetMax = new Vector2(-12f, -4f);
        upgradeText = label.GetComponent<TextMeshProUGUI>();
        if (specialTree.font != null) upgradeText.font = specialTree.font;
        if (specialTree.fontMaterial != null) upgradeText.fontSharedMaterial = specialTree.fontMaterial;
        upgradeText.fontSize = 26f;
        upgradeText.enableAutoSizing = true;
        upgradeText.fontSizeMin = 14f;
        upgradeText.fontSizeMax = 26f;
        upgradeText.alignment = TextAlignmentOptions.Center;
        upgradeText.raycastTarget = false;

        upgradeButton.SetActive(false);
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.onBossDefeated -= OnBossDefeated;
            spawner.onMidBossSpawned -= OnMidBossSpawned;
            spawner.onMidBossDefeated -= OnMidBossDefeated;
        }
        if (Instance == this) Instance = null;
    }

    void OnBossDefeated(int stage)
    {
        if (GameMode.IsEndless)
        {
            if (EndlessMode.Instance != null) EndlessMode.Instance.OnBossDefeated();
            return;
        }
        if (stage == 0)
        {
            if (portal != null) portal.SetActive(true);
            ShowBanner(Loc.T("신전 문이 열렸다!\n문으로 들어가세요"), 3f);
            StartCoroutine(PortalCountdown());
        }
        else if (stage == 1)
        {
            if (portal != null) portal.SetActive(true);
            ShowBanner(Loc.T("지옥의 군주를 쓰러뜨렸다!\n성문 너머로 초원이 보인다"), 3.5f);
            StartCoroutine(PortalCountdown());
        }
        else
        {
            ShowBanner(Loc.T("킹 슬라임을 쓰러뜨렸다!\n모든 스테이지 클리어!"), 6f);
            // 클리어 기록 · 난이도 잠금 해제는 바로 저장하고, 잠시 뒤 엔딩
            Difficulty cleared = GameMode.Current;
            string opened = GameMode.OnCleared(cleared);
            Cleared?.Invoke(cleared);
            StartCoroutine(EndingAfter(cleared, opened));
        }
    }

    // 3장 보스를 쓰러뜨려 한 판을 클리어했을 때 (업적 등)
    public static event System.Action<Difficulty> Cleared;

    IEnumerator EndingAfter(Difficulty cleared, string opened)
    {
        yield return new WaitForSecondsRealtime(4f);
        if (StoryDirector.ShouldSkipAll) yield break;
        yield return StoryDirector.Ending(cleared, opened);
    }

    // PortalGate가 플레이어를 감지하면 호출
    public void EnterPortal()
    {
        if (transitioning) return;
        if (CurrentStage == 0) StartCoroutine(EnterHell());
        else if (CurrentStage == 1) StartCoroutine(EnterMeadow());
    }

    // 지옥 → 초원: 특수 능력 포인트 2개를 받고 맵 교체
    IEnumerator EnterMeadow()
    {
        transitioning = true;
        spawner.spawningEnabled = false;
        Time.timeScale = 0f;
        yield return Fade(0f, 1f, 0.8f);

        if (hellMap != null) hellMap.SetActive(false);
        if (meadowMap != null) meadowMap.SetActive(true);
        if (portal != null) portal.SetActive(false);
        if (Camera.main != null) Camera.main.backgroundColor = meadowBackground;
        if (bossBar != null) bossBar.bossSpawn = false;

        CurrentStage = 2;
        spawner.StartStage(2);
        if (player != null) player.position = stage2PlayerStart;
        specialPoints += 2;

        Time.timeScale = 1f;
        yield return Fade(1f, 0f, 0.8f);
        ShowBanner(Loc.T("3장 · 초원\n특수 능력 포인트 +2"), 3f);
        transitioning = false;
    }

    IEnumerator EnterHell()
    {
        transitioning = true;
        spawner.spawningEnabled = false;
        Time.timeScale = 0f;

        yield return Fade(0f, 1f, 0.7f);

        // 특수 능력 선택 (전체 화면 스킬 트리)
        pendingPicks = null;
        specialPanel.SetActive(true);
        if (specialTree != null) specialTree.Open(specials, OnPickSpecial);
        SetFade(0f);
        while (pendingPicks == null) yield return null;
        chosenSpecials = pendingPicks;
        if (specials != null) specials.Equip(chosenSpecials);
        TooltipUI.Hide();
        SetFade(1f);
        specialPanel.SetActive(false);

        // 지옥 맵으로 교체
        foreach (Renderer r in caveRenderers) if (r != null) r.enabled = false;
        if (hellMap != null) hellMap.SetActive(true);
        if (portal != null) portal.SetActive(false);
        if (Camera.main != null) Camera.main.backgroundColor = hellBackground;
        if (bossBar != null) bossBar.bossSpawn = false;

        CurrentStage = 1;
        spawner.StartStage(1);
        if (player != null) player.position = stage2PlayerStart;

        Time.timeScale = 1f;
        yield return Fade(1f, 0f, 0.8f);
        ShowBanner(Loc.T("2장 · 불타는 지옥"), 2.5f);
        transitioning = false;
    }

    // 보스를 잡은 뒤 제한 시간: 막바지엔 문 쪽으로 끌려가고, 끝나면 자동 입장
    IEnumerator PortalCountdown()
    {
        if (portal == null || player == null) yield break;
        EnsureCountdownUI();
        int fromStage = CurrentStage;
        // 포탈 판정 상자(문 아래쪽) 위치
        Vector3 target = portal.transform.position + new Vector3(0f, -3.8f, 0f);
        float left = portalTimeLimit;
        float wisp = 0f;

        while (left > 0f && CurrentStage == fromStage && !transitioning)
        {
            left -= Time.deltaTime;            // 멈춘 동안에는 줄지 않음
            bool urgent = left <= portalPullTime;

            countdownText.gameObject.SetActive(true);
            countdownText.text = urgent
                ? Loc.T("지옥의 문이 당신을 끌어당긴다!  ") + Mathf.CeilToInt(left)
                : Loc.T("신전 문으로 들어가세요  ") + Mathf.CeilToInt(left) + Loc.T("초");
            countdownText.color = urgent ? Color.Lerp(new Color(1f, 0.35f, 0.3f), Color.white, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : Gold;

            // 플레이어 → 문 안내선
            portalGuide.enabled = true;
            portalGuide.SetPosition(0, player.position);
            portalGuide.SetPosition(1, target);
            float a = urgent ? 0.5f + 0.3f * Mathf.Sin(Time.time * 10f) : 0.25f + 0.1f * Mathf.Sin(Time.time * 3f);
            portalGuide.startColor = new Color(1f, 0.8f, 0.4f, a);
            portalGuide.endColor = new Color(1f, 0.55f, 0.2f, a * 0.3f);

            if (urgent && Time.timeScale > 0f)
            {
                // 점점 강해지는 끌어당김 (마지막엔 이동 속도보다 강함)
                float k = 1f - left / portalPullTime;
                float pull = Mathf.Lerp(3f, 26f, k * k);
                player.position = Vector3.MoveTowards(player.position, target, pull * Time.deltaTime);

                wisp += Time.deltaTime;
                if (wisp >= 0.05f)
                {
                    wisp = 0f;
                    SoulWisp.Spawn(player.position + (Vector3)(Random.insideUnitCircle * 3f), target, new Color(1f, 0.6f, 0.25f, 0.9f));
                }
            }
            yield return null;
        }

        countdownText.gameObject.SetActive(false);
        portalGuide.enabled = false;
        if (CurrentStage == fromStage && !transitioning) EnterPortal();
    }

    void EnsureCountdownUI()
    {
        if (countdownText == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            GameObject go = new GameObject("PortalCountdown", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            if (banner != null) r.SetSiblingIndex(banner.transform.GetSiblingIndex());
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(1100f, 64f);
            r.anchoredPosition = new Vector2(0f, -150f);
            countdownText = go.GetComponent<TextMeshProUGUI>();
            if (specialTree != null && specialTree.font != null) countdownText.font = specialTree.font;
            if (specialTree != null && specialTree.fontMaterial != null) countdownText.fontSharedMaterial = specialTree.fontMaterial;
            countdownText.fontSize = 42f;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.raycastTarget = false;
        }
        if (portalGuide == null)
        {
            portalGuide = Hostile.NewLine("PortalGuide", Gold, 0.18f, 3);
            portalGuide.positionCount = 2;
            portalGuide.endWidth = 0.05f;
            portalGuide.enabled = false;
        }
    }

    // 스킬 트리에서 능력을 확정했을 때 (능력 번호)
    public void OnPickSpecial(int[] ids)
    {
        pendingPicks = ids;
    }

    public void ShowBanner(string text, float seconds)
    {
        if (banner == null) return;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(text, seconds));
    }

    Coroutine bannerRoutine;

    IEnumerator BannerRoutine(string text, float seconds)
    {
        bannerText.text = text;
        banner.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        banner.SetActive(false);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetFade(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetFade(to);
    }

    void SetFade(float alpha)
    {
        if (fade == null) return;
        Color c = fade.color;
        c.a = alpha;
        fade.color = c;
        fade.raycastTarget = alpha > 0.01f;
        fade.gameObject.SetActive(alpha > 0.001f);
    }
}

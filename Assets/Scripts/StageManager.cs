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

    [Header("UI")]
    public Image fade;                      // 전체 화면 검은 막
    public GameObject specialPanel;         // 특수 능력 선택 화면
    public SpecialTreeUI specialTree;
    public SpecialAbilities specials;
    public GameObject banner;               // 화면 중앙 알림
    public TextMeshProUGUI bannerText;

    public int[] chosenSpecials = new int[0];

    [Header("특수 능력 포인트 (2장 중간 보스 보상)")]
    public int specialPoints = 0;
    public KeyCode upgradeKey = KeyCode.T;

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
        if (portal != null) portal.SetActive(false);
        if (specialPanel != null) specialPanel.SetActive(false);
        if (banner != null) banner.SetActive(false);
        SetFade(0f);

        BuildUpgradeButton();
        // 2장 상점의 무기 강화 창
        gameObject.AddComponent<WeaponUpgradeShop>().Init(FindFirstObjectByType<Shop>(), specials, specialTree);
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
                upgradeText.text = "특수 강화 [" + upgradeKey + "]   포인트 " + specialPoints;
            }
        }
        if (show && Input.GetKeyDown(upgradeKey)) OpenUpgrade();
    }

    // ================================================================= 특수 능력 포인트
    void OnMidBossSpawned()
    {
        ShowBanner("중간 보스 등장!\n쓰러뜨리면 특수 능력 포인트 +1", 3f);
    }

    void OnMidBossDefeated()
    {
        specialPoints++;
        ShowBanner("특수 능력 포인트 +1!\n[" + upgradeKey + "] 또는 아래 버튼으로 강화", 3f);
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
        foreach (int id in ids)
        {
            if (specials.Has(id)) specials.Evolve(id);
            else fresh.Add(id);
        }
        if (fresh.Count > 0) specials.Equip(fresh);
        chosenSpecials = new List<int>(specials.EquippedIds).ToArray();
        specialPoints = Mathf.Max(0, specialPoints - ids.Length);
        CloseUpgrade();
    }

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
        if (stage == 0)
        {
            if (portal != null) portal.SetActive(true);
            ShowBanner("신전 문이 열렸다!\n문으로 들어가세요", 3f);
        }
        else
        {
            ShowBanner("지옥의 군주를 쓰러뜨렸다!", 4f);
        }
    }

    // PortalGate가 플레이어를 감지하면 호출
    public void EnterPortal()
    {
        if (transitioning || CurrentStage != 0) return;
        StartCoroutine(EnterHell());
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
        ShowBanner("2장 · 불타는 지옥", 2.5f);
        transitioning = false;
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

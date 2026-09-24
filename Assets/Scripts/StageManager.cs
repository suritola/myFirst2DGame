using System.Collections;
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
    public TextMeshProUGUI[] specialTitles;
    public TextMeshProUGUI[] specialDescriptions;
    public GameObject banner;               // 화면 중앙 알림
    public TextMeshProUGUI bannerText;

    [Header("특수 능력 (준비 중 - 선택만 기록)")]
    public string[] specialNames =
    {
        "지옥불 탄환",
        "그림자 대시",
        "수호 영혼",
    };
    [TextArea(2, 4)]
    public string[] specialTexts =
    {
        "총알에 맞은 적이 불타 3초 동안\n초당 공격력의 30% 피해를 받습니다.",
        "Space로 짧게 돌진합니다.\n돌진 중에는 무적 (쿨타임 3초).",
        "영혼 구체 2개가 주위를 돌며\n닿은 적에게 피해를 줍니다.",
    };
    public int chosenSpecial = -1;

    public int CurrentStage { get; private set; }

    bool transitioning;
    int pendingPick = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.onBossDefeated += OnBossDefeated;

        if (hellMap != null) hellMap.SetActive(false);
        if (portal != null) portal.SetActive(false);
        if (specialPanel != null) specialPanel.SetActive(false);
        if (banner != null) banner.SetActive(false);
        SetFade(0f);
    }

    void OnDestroy()
    {
        if (spawner != null) spawner.onBossDefeated -= OnBossDefeated;
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

        // 특수 능력 선택 (전체 화면)
        for (int i = 0; i < specialTitles.Length && i < specialNames.Length; i++)
        {
            specialTitles[i].text = specialNames[i];
            specialDescriptions[i].text = specialTexts[i] + "\n\n<color=#9d93a8>(준비 중)</color>";
        }
        pendingPick = -1;
        specialPanel.SetActive(true);
        SetFade(0f);
        while (pendingPick < 0) yield return null;
        chosenSpecial = pendingPick;
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

    // 특수 능력 카드 버튼
    public void OnPickSpecial(int index)
    {
        pendingPick = index;
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

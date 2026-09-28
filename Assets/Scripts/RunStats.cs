using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 이번 판 기록: 게임 씬에서 계속 적어 두고, 게임 오버 화면(결과)과 ESC 메뉴(요약)에서 보여 줌
public class RunStats : MonoBehaviour
{
    public static float Seconds;            // 멈춘 시간은 뺀 플레이 시간
    public static int Kills, Level = 1;
    public static string Character = "", Difficulty = "";
    public static readonly List<string> Abilities = new List<string>();     // 특수 능력 (진화하면 +)
    public static readonly List<string> Cards = new List<string>();         // 레벨업 카드 "이름 Lv n"

    float refresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name != "GameScene") return;
            Seconds = 0f;
            Kills = 0;
            Level = 1;
            Abilities.Clear();
            Cards.Clear();
            new GameObject("RunStats").AddComponent<RunStats>();
        };
    }

    void OnEnable() => EnermyController.Killed += OnKill;
    void OnDisable() => EnermyController.Killed -= OnKill;
    static void OnKill(Vector3 pos) => Kills++;

    void Update()
    {
        if (Time.timeScale > 0f) Seconds += Time.unscaledDeltaTime * Mathf.Min(1f, Time.timeScale);
        refresh -= Time.unscaledDeltaTime;
        if (refresh > 0f) return;
        refresh = 0.5f;
        Capture();
    }

    static SpecialAbilities sp;
    static LevelShop shop;

    public static void Capture()
    {
        CharacterDef d = CharacterData.Current;
        Character = Loc.T(d.name);
        Difficulty = GameMode.Name(GameMode.Current);
        PlayerController p = Hostile.Player;
        if (p != null) Level = p.level;

        // 0.5초마다 씬을 뒤지지 않게 한 번 찾아 두고 씀
        if (sp == null) sp = FindFirstObjectByType<SpecialAbilities>();
        if (sp != null && sp.abilities != null)
        {
            Abilities.Clear();
            foreach (int id in sp.EquippedIds)
                if (id >= 0 && id < sp.abilities.Length && sp.abilities[id] != null)
                    Abilities.Add(Loc.T(sp.abilities[id].name) + (sp.IsEvolved(id) ? "+" : ""));
        }
        if (shop == null) shop = FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);
        if (shop != null && shop.ability_level != null && shop.ability_name != null)
        {
            Cards.Clear();
            for (int i = 0; i < shop.ability_level.Length && i < shop.ability_name.Length; i++)
                if (shop.ability_level[i] > 0 && !string.IsNullOrEmpty(shop.ability_name[i]))
                    Cards.Add(shop.ability_name[i] + " Lv" + shop.ability_level[i]);
        }
    }

    // 여러 줄 요약 (ESC 메뉴 · 결과 화면이 같이 씀)
    public static string Summary(bool withStats)
    {
        Capture();
        string gold = "<color=#F5D478>", end = "</color>";
        string s = gold + Character + "  ·  " + Difficulty + end + "\n"
                 + Loc.T("플레이 시간 ") + EndlessMode.Clock(Seconds) + "     " + Loc.T("레벨 ") + Level + "     " + Loc.T("처치 ") + Kills;
        if (withStats)
        {
            PlayerController p = Hostile.Player;
            if (p != null)
                s += "\n\n" + gold + Loc.T("능력치") + end + "\n"
                   + Loc.T("공격력 ") + p.damage.ToString("0.##") + "   " + Loc.T("받는 피해 감소 ") + (Mathf.Min(p.def, PlayerController.MaxDef) * 100f).ToString("0") + "%"
                   + "   " + Loc.T("최대 체력 ") + Mathf.RoundToInt(p.PlayerMaxHealth);
        }
        s += "\n\n" + gold + Loc.T("특수 능력") + end + "\n" + (Abilities.Count > 0 ? string.Join(",  ", Abilities) : "-");
        s += "\n\n" + gold + Loc.T("레벨업 카드") + end + "\n" + (Cards.Count > 0 ? string.Join(",  ", Cards) : "-");
        return s;
    }

    // ================================================================= ESC 메뉴 오른쪽의 이번 판 요약
    static GameObject summaryPanel;

    public static void ShowSummary(bool show)
    {
        if (!show)
        {
            if (summaryPanel != null) Destroy(summaryPanel);
            return;
        }
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        if (summaryPanel != null) Destroy(summaryPanel);
        summaryPanel = new GameObject("RunSummary", typeof(RectTransform), typeof(Image));
        RectTransform r = summaryPanel.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        r.SetAsLastSibling();
        r.anchorMin = r.anchorMax = new Vector2(1f, 0.5f);
        r.pivot = new Vector2(1f, 0.5f);
        r.anchoredPosition = new Vector2(-30f, 0f);
        r.sizeDelta = new Vector2(520f, 620f);
        Image bg = summaryPanel.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.05f, 0.08f, 0.88f);
        bg.raycastTarget = false;

        GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform tr = tg.GetComponent<RectTransform>();
        tr.SetParent(r, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(26f, 22f);
        tr.offsetMax = new Vector2(-26f, -22f);
        TextMeshProUGUI t = tg.GetComponent<TextMeshProUGUI>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) t.font = UIKit.Font;
        if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
        t.fontSize = 22f;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14f;
        t.fontSizeMax = 22f;
        t.color = new Color(0.93f, 0.9f, 0.84f);
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        t.text = "<size=120%>" + Loc.T("이번 판") + "</size>\n\n" + Summary(true);
    }
}

// 처음 한 번만 뜨는 짧은 도움말 (설정에서 끌 수 있음). 화면 아래 가운데, HUD 위
public static class Hints
{
    static GameObject box;
    static TextMeshProUGUI text;
    static float until;
    static HintRunner runner;

    public static void Show(string key, string ko)
    {
        if (!GameSettings.Hints || GameInput.Auto || Application.isBatchMode || GameInput.TrailerRunning) return;
        string pref = "hint." + key;
        if (PlayerPrefs.GetInt(pref, 0) == 1) return;
        PlayerPrefs.SetInt(pref, 1);
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        if (box == null)
        {
            box = new GameObject("Hint", typeof(RectTransform), typeof(Image));
            RectTransform r = box.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(0f, 180f);
            r.sizeDelta = new Vector2(720f, 56f);
            Image bg = box.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.07f, 0.12f, 0.8f);
            bg.raycastTarget = false;
            GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform tr = tg.GetComponent<RectTransform>();
            tr.SetParent(r, false);
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(16f, 4f);
            tr.offsetMax = new Vector2(-16f, -4f);
            text = tg.GetComponent<TextMeshProUGUI>();
            UIKit.EnsureStyle();
            if (UIKit.Font != null) text.font = UIKit.Font;
            if (UIKit.FontMaterial != null) text.fontSharedMaterial = UIKit.FontMaterial;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 22f;
            text.color = new Color(0.8f, 0.95f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            runner = box.AddComponent<HintRunner>();
        }
        text.text = "<color=#9fd8ff>" + Loc.T("도움말") + "</color>  " + Loc.T(ko);
        box.SetActive(true);
        until = Time.unscaledTime + 5f;
    }

    public static void Tick()
    {
        if (box != null && box.activeSelf && Time.unscaledTime > until) box.SetActive(false);
    }
}

public class HintRunner : MonoBehaviour
{
    void Update() => Hints.Tick();
}

// 게임 오버 화면: [R]로 같은 캐릭터 · 난이도로 바로 다시 시작
public class QuickRestart : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.R) || SettingsUI.IsOpen) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }
}

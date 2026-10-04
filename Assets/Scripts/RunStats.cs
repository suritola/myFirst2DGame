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
    // 무기 진화 · 영혼 트리 (1.7.9~): 결과 화면과 밸런스 기록에 씀
    public static readonly List<KeyValuePair<string, float>> Evolutions = new List<KeyValuePair<string, float>>();
    public static int[] Branches = new int[6];
    public static int ShardsTotal, ShardsSpent;
    static bool runActive, cleared;

    float refresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        StageManager.Cleared += d => cleared = true;
        SceneManager.sceneLoaded += (s, m) =>
        {
            // 게임 씬을 떠날 때 (게임 오버 · 메인 메뉴 · 다시 시작) 이번 판을 밸런스 기록에 한 줄
            if (runActive) { runActive = false; WriteBalanceLog(cleared ? "clear" : s.name == "GameOver" ? "death" : "quit"); }
            if (s.name != "GameScene") return;
            runActive = true;
            cleared = false;
            Evolutions.Clear();
            Branches = new int[6];
            ShardsTotal = ShardsSpent = 0;
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
        if (sp != null)
        {
            Evolutions.Clear();
            Evolutions.AddRange(sp.EvolutionLog);
            Branches = sp.OwnedPerBranch();
            ShardsTotal = SoulShards.Total;
            ShardsSpent = SoulShards.Spent;
        }
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
        // 무기 진화: 기본 무기 → 1차 (시각) → 2차 (시각)
        string evo = Loc.T("기본 무기");
        foreach (KeyValuePair<string, float> e in Evolutions) evo += "  \u2192  " + e.Key + " <color=#A89C86>(" + EndlessMode.Clock(e.Value) + ")</color>";
        s += "\n\n" + gold + Loc.T("무기 진화") + end + "\n" + evo;
        // 영혼 트리: 많이 배운 가지 순서 · 조각
        List<int> order = new List<int> { 0, 1, 2, 3, 4, 5 };
        order.Sort((a, b) => Branches[b].CompareTo(Branches[a]));
        string tree = "";
        foreach (int b in order)
            if (Branches[b] > 0) tree += (tree.Length > 0 ? ",  " : "") + Loc.T(SpecialAbilities.BranchNames[b]) + " " + Branches[b];
        s += "\n\n" + gold + Loc.T("영혼 트리") + end + "\n" + (tree.Length > 0 ? tree : "-")
           + "\n" + Loc.T("영혼 조각") + " " + ShardsTotal + "  (" + Loc.T("씀") + " " + ShardsSpent + ")";
        s += "\n\n" + gold + Loc.T("특수 능력") + end + "\n" + (Abilities.Count > 0 ? string.Join(",  ", Abilities) : "-");
        s += "\n\n" + gold + Loc.T("레벨업 카드") + end + "\n" + (Cards.Count > 0 ? string.Join(",  ", Cards) : "-");
        return s;
    }

    // ================================================================= 밸런스 기록
    // 판이 끝날 때마다 한 줄 (persistentDataPath/balance_log.csv): 영혼 조각 · 진화 시점 · 가지별 칸 수를 숫자로 보고 조정하려고
    public static string BalanceLogPath => System.IO.Path.Combine(Application.persistentDataPath, "balance_log.csv");

    static void WriteBalanceLog(string result)
    {
        if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return;     // 테스트 · 자동 플레이는 기록하지 않음
        try
        {
            string path = BalanceLogPath;
            bool fresh = !System.IO.File.Exists(path);
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            if (fresh) b.AppendLine("date,version,character,difficulty,endless,result,seconds,level,kills,shards_total,shards_spent,evo1,evo1_sec,evo2,evo2_sec,weapon,ultimate,skill,survival,soul,wealth,cards");
            string E(int i, bool time) => i < Evolutions.Count ? (time ? Evolutions[i].Value.ToString("0") : Evolutions[i].Key.Replace(",", " ")) : "";
            b.Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append(',')
             .Append(Application.version).Append(',')
             .Append(CharacterData.Selected).Append(',')
             .Append(GameMode.Current).Append(',')
             .Append(GameMode.IsEndless ? 1 : 0).Append(',')
             .Append(result).Append(',')
             .Append(Seconds.ToString("0")).Append(',')
             .Append(Level).Append(',').Append(Kills).Append(',')
             .Append(ShardsTotal).Append(',').Append(ShardsSpent).Append(',')
             .Append(E(0, false)).Append(',').Append(E(0, true)).Append(',')
             .Append(E(1, false)).Append(',').Append(E(1, true));
            for (int i = 0; i < 6; i++) b.Append(',').Append(i < Branches.Length ? Branches[i] : 0);
            b.Append(',').Append(Cards.Count).AppendLine();
            System.IO.File.AppendAllText(path, b.ToString(), new System.Text.UTF8Encoding(true));
        }
        catch (System.Exception ex) { Debug.LogWarning("balance log: " + ex.Message); }
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
        r.sizeDelta = new Vector2(440f, 620f);          // 4:3 화면에서도 ESC 창(폭 720)과 안 겹치게
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
            r.anchoredPosition = new Vector2(0f, 236f);      // 레벨업 대기 배지(110 ~ 220) · 특수 강화 버튼 위
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

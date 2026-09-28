using UnityEngine;

// 창 제목을 게임 이름(Soul Saver)으로
// 제품 이름(productName)도 Soul Saver (1.7.3부터, 옛 저장은 SaveMigration 이 옮김)
public static class WindowTitle
{
    public const string GameName = "Soul Saver";

    // 부 버전(두 번째 자리)마다 코드네임 (AGENT.md 규칙 4). tools/release.ps1 도 이 표를 읽어 릴리즈 제목에 붙임
    static readonly System.Collections.Generic.Dictionary<string, string> Codenames = new System.Collections.Generic.Dictionary<string, string>
    {
        { "1.7", "Soul Harvest" },
    };

    // "1.7.6" → "Soul Harvest" (없으면 빈 글)
    public static string Codename(string version)
    {
        string[] p = version.TrimStart('v').Split('.');
        return p.Length >= 2 && Codenames.TryGetValue(p[0] + "." + p[1], out string c) ? c : "";
    }

    // 메인 메뉴에 쓰는 버전 글: "v1.7.6 · Soul Harvest"
    public static string VersionLabel
    {
        get
        {
            string v = "v" + Application.version.TrimStart('v');
            string c = Codename(v);
            return c.Length > 0 ? v + " \u00B7 " + c : v;
        }
    }

    // 메인 메뉴 오른쪽 아래 "v2.0" 같은 버전 글을 실제 버전으로
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookVersion()
    {
        FixVersion();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => FixVersion();
    }

    static void FixVersion()
    {
        string v = VersionLabel;
        foreach (TMPro.TMP_Text t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            if (System.Text.RegularExpressions.Regex.IsMatch(t.text, @"^v\d+(\.\d+)+( \u00B7 .+)?$")) t.text = v;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern bool SetWindowTextW(System.IntPtr hWnd, string text);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        Set();
        // 시작할 때 창이 아직 앞에 없을 수 있어 씬이 바뀔 때마다 한 번 더
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => Set();
    }

    static void Set()
    {
        System.IntPtr h = GetActiveWindow();
        if (h != System.IntPtr.Zero) SetWindowTextW(h, GameName);
    }
#endif
}

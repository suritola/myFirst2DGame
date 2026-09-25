using UnityEngine;

// 창 제목을 게임 이름(Soul Saver)으로
// 제품 이름(productName)은 저장 위치 · 실행 파일 이름이 바뀌지 않도록 예전 이름(Gun Saver)을 그대로 둠
public static class WindowTitle
{
    public const string GameName = "Soul Saver";

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

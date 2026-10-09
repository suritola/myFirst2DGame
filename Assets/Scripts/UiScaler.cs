using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 설정 (2.1.9~): UI 크기 · 창이 비활성일 때 소리 끄기
// UI 크기: 화면 크기에 맞춰 늘어나는 모든 캔버스(CanvasScaler · ScaleWithScreenSize)의 기준 해상도를 크기로 나눔 (작게 나누면 글자 · 버튼이 커짐)
//   씬 캔버스 · 실행 중에 만든 캔버스(HUD · 창) 모두: 1초마다 새 캔버스를 찾아 맞춤 (처음 본 기준 해상도를 기억)
// 소리: 창이 포커스를 잃으면 AudioListener.pause (음악 · 효과음 모두), 돌아오면 다시
public class UiScaler : MonoBehaviour
{
    static UiScaler instance;
    static readonly Dictionary<CanvasScaler, Vector2> baseRes = new Dictionary<CanvasScaler, Vector2>();
    float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("UiScaler");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UiScaler>();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => ApplyAll();
        ApplyAll();
    }

    // 실행 중에 만든 캔버스를 바로 맞춤 (다음 검사까지 크기가 튀지 않게)
    public static void Fit(CanvasScaler cs)
    {
        if (cs == null) return;
        baseRes[cs] = cs.referenceResolution;
        cs.referenceResolution = cs.referenceResolution / Mathf.Max(0.5f, GameSettings.UiScale);
    }

    public static void ApplyAll()
    {
        float scale = Mathf.Max(0.5f, GameSettings.UiScale);
        List<CanvasScaler> dead = null;
        foreach (KeyValuePair<CanvasScaler, Vector2> kv in baseRes)
            if (kv.Key == null) (dead ??= new List<CanvasScaler>()).Add(kv.Key);
        if (dead != null) foreach (CanvasScaler d in dead) baseRes.Remove(d);

        foreach (CanvasScaler cs in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cs.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            if (!baseRes.TryGetValue(cs, out Vector2 b)) { b = cs.referenceResolution; baseRes[cs] = b; }
            cs.referenceResolution = b / scale;
        }
    }

    void Update()
    {
        // 창 · HUD 를 실행 중에 새로 만들므로 가끔 다시 찾음 (자주 하지 않음)
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1f;
        ApplyAll();
    }

    void OnApplicationFocus(bool focus)
    {
        AudioListener.pause = !focus && GameSettings.MuteUnfocused;
    }
}

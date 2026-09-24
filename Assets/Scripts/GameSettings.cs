using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 설정값 (언어 · 볼륨): PlayerPrefs에 저장하고 게임이 켜질 때 적용
public static class GameSettings
{
    const string LangKey = "settings.language";
    const string VolumeKey = "settings.volume";

    static bool loaded;
    static Loc.Lang language = Loc.Lang.Korean;
    static float volume = 0.5f;

    public static Loc.Lang Language
    {
        get { Load(); return language; }
        set
        {
            Load();
            if (language == value) return;
            language = value;
            PlayerPrefs.SetInt(LangKey, (int)value);
            PlayerPrefs.Save();
            Loc.ApplyFonts();
            SceneLocalizer.RefreshAll();
            Loc.RaiseChanged();
        }
    }

    // 0 ~ 1 (기본 0.5)
    public static float Volume
    {
        get { Load(); return volume; }
        set
        {
            Load();
            volume = Mathf.Clamp01(value);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumeKey, volume);
        }
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        language = (Loc.Lang)Mathf.Clamp(PlayerPrefs.GetInt(LangKey, 0), 0, 3);
        volume = PlayerPrefs.GetFloat(VolumeKey, 0.5f);
    }

    // 게임이 켜질 때 볼륨 · 폰트 적용, 씬마다 글자 번역
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Load();
        AudioListener.volume = volume;
        Loc.ApplyFonts();
        SceneLocalizer.RefreshAll();
        SceneManager.sceneLoaded += (s, m) =>
        {
            AudioListener.volume = Volume;
            Loc.ApplyFonts();
            SceneLocalizer.RefreshAll();
            MenuExtras.Install();
        };
        MenuExtras.Install();
    }
}

// 씬에 적혀 있는 한국어 글자(버튼, 제목 등)를 현재 언어로 바꿔 줌
public static class SceneLocalizer
{
    static readonly System.Collections.Generic.Dictionary<int, string> original = new System.Collections.Generic.Dictionary<int, string>();

    public static void RefreshAll()
    {
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            int id = t.GetInstanceID();
            if (!original.TryGetValue(id, out string ko))
            {
                if (!Loc.Has(t.text)) continue;
                ko = t.text;
                original[id] = ko;
            }
            t.text = Loc.T(ko);
        }
    }
}

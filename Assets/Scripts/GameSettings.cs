using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 설정값 (언어 · 볼륨 · 화면): PlayerPrefs에 저장하고 게임이 켜질 때 적용
// 화면 모드와 해상도는 Unity가 스스로 기억함
public static class GameSettings
{
    const string LangKey = "settings.language";
    const string VolumeKey = "settings.volume";
    const string MusicKey = "settings.music";
    const string SfxKey = "settings.sfx";
    const string ShakeKey = "settings.shake";
    const string FlashKey = "settings.flash";
    const string VSyncKey = "settings.vsync";

    static bool loaded;
    static Loc.Lang language = Loc.Lang.Korean;
    static float volume = 0.5f;
    static float music = 1f, sfx = 1f;
    static bool shake = true, flash = true, vsync = true;

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
        music = PlayerPrefs.GetFloat(MusicKey, 1f);
        sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        shake = PlayerPrefs.GetInt(ShakeKey, 1) == 1;
        flash = PlayerPrefs.GetInt(FlashKey, 1) == 1;
        vsync = PlayerPrefs.GetInt(VSyncKey, 1) == 1;
    }

    // 음악 · 효과음 볼륨 (0 ~ 1, 전체 볼륨에 곱해짐)
    public static float MusicVolume
    {
        get { Load(); return music; }
        set { Load(); music = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, music); }
    }

    public static float SfxVolume
    {
        get { Load(); return sfx; }
        set { Load(); sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, sfx); }
    }

    // 화면 흔들림 (트레일러 촬영 중에는 항상 켬)
    public static bool ScreenShake
    {
        get { Load(); return shake || GameInput.Auto; }
        set { Load(); shake = value; PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); }
    }

    // 피격 시 붉은 번쩍임 · 저체력 깜빡임 · 폭발 섬광 (끄면 약하게, 깜빡이지 않게)
    public static bool Flashes
    {
        get { Load(); return flash || GameInput.Auto; }
        set { Load(); flash = value; PlayerPrefs.SetInt(FlashKey, value ? 1 : 0); }
    }

    public static bool VSync
    {
        get { Load(); return vsync; }
        set { Load(); vsync = value; PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0); ApplyVSync(); }
    }

    static void ApplyVSync()
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        Application.targetFrameRate = -1;
    }

    // 설정 창을 닫을 때 디스크에 바로 기록 (비정상 종료에도 남도록)
    public static void Save() => PlayerPrefs.Save();

    // 게임이 켜질 때 볼륨 · 폰트 적용, 씬마다 글자 번역
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Load();
        AudioListener.volume = volume;
        if (!Application.isBatchMode) ApplyVSync();
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

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
    const string HintsKey = "settings.hints";
    const string DamageNumKey = "settings.damageNumbers";
    const string ColorBlindKey = "settings.colorBlind";
    const string KeyIconKey = "settings.keyIcons";
    const string CursorKey = "settings.gameCursor";
    const string MouseSensKey = "settings.mouseSensitivity";
    public const float MouseSensMin = 0.25f, MouseSensMax = 3f;

    static bool loaded;
    static float mouseSens = 1f;
    static Loc.Lang language = Loc.Lang.Korean;
    static float volume = 0.5f;
    static float music = 1f, sfx = 1f;
    static bool shake = true, flash = true, vsync = true;
    static bool hints = true, colorBlind = false, keyIcons = true, gameCursor = true;
    static int damageNumbers;

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
        hints = PlayerPrefs.GetInt(HintsKey, 1) == 1;
        damageNumbers = Mathf.Clamp(PlayerPrefs.GetInt(DamageNumKey, 0), 0, 2);
        colorBlind = PlayerPrefs.GetInt(ColorBlindKey, 0) == 1;
        keyIcons = PlayerPrefs.GetInt(KeyIconKey, 1) == 1;
        gameCursor = PlayerPrefs.GetInt(CursorKey, 1) == 1;
        mouseSens = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensKey, 1f), MouseSensMin, MouseSensMax);
    }

    // 전투 중 마우스 감도 (1 = 윈도우 커서 그대로, 0.25 ~ 3) · 1.8.9~ (MouseSensitivity)
    public static float MouseSensitivity
    {
        get { Load(); return mouseSens; }
        set { Load(); mouseSens = Mathf.Clamp(value, MouseSensMin, MouseSensMax); PlayerPrefs.SetFloat(MouseSensKey, mouseSens); }
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

    // 처음 한 번씩 뜨는 도움말
    public static bool Hints
    {
        get { Load(); return hints; }
        set { Load(); hints = value; PlayerPrefs.SetInt(HintsKey, value ? 1 : 0); }
    }

    // 피해 숫자: 0 보통 · 1 크게 · 2 끄기
    public static readonly string[] DamageNumberNames = { "보통", "크게", "끄기" };
    public static int DamageNumbers
    {
        get { Load(); return damageNumbers; }
        set { Load(); damageNumbers = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt(DamageNumKey, damageNumbers); }
    }

    // 색각 이상 모드: 경고 표시를 빨강 · 초록 대신 주황 · 파랑 · 자홍으로
    public static bool ColorBlind
    {
        get { Load(); return colorBlind; }
        set { Load(); colorBlind = value; PlayerPrefs.SetInt(ColorBlindKey, value ? 1 : 0); }
    }

    // HUD 스킬 칸의 키를 키보드 모양 아이콘으로
    public static bool KeyIcons
    {
        get { Load(); return keyIcons; }
        set { Load(); keyIcons = value; PlayerPrefs.SetInt(KeyIconKey, value ? 1 : 0); }
    }

    // 게임 전용 마우스 커서 (끄면 윈도우 기본 포인터) · 1.8.7~
    public static bool GameCursor
    {
        get { Load(); return gameCursor; }
        set { Load(); gameCursor = value; PlayerPrefs.SetInt(CursorKey, value ? 1 : 0); CursorSkin.Refresh(); }
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

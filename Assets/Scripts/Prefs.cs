using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// 저장 (2.1.9~): PlayerPrefs(윈도우 레지스트리) 대신 파일 하나 — 스팀 클라우드(자동 클라우드)가 다른 PC로 옮길 수 있게
//   %LOCALAPPDATA%\<제품 이름>\save.json (회사 이름이 한글이라 Unity 기본 저장 폴더 대신 영문 경로 · 에디터는 _Editor 를 붙여 따로)
//   함수는 PlayerPrefs 와 같음 (GetInt · SetInt · GetFloat · SetFloat · GetString · SetString · HasKey · DeleteKey · Save)
// 옛 저장 옮기기: 파일에 없는 키를 읽으면 레지스트리(PlayerPrefs)에 있던 값을 가져와 파일에 넣음 (모든 키를 한 번에 알 수 없어 읽을 때마다)
// Save() 는 바뀐 게 있을 때만 파일을 씀 (임시 파일에 쓰고 바꿔치기 · 게임을 끌 때도 씀)
public static class Prefs
{
    [System.Serializable]
    class Store
    {
        public int version = 1;
        public List<string> keys = new List<string>();
        public List<string> types = new List<string>();      // i 정수 · f 실수 · s 글자
        public List<string> values = new List<string>();
    }

    static Dictionary<string, (char type, string value)> data;
    static bool dirty, hooked;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string FolderPath
    {
        get
        {
            string root = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string name = Application.productName + (Application.isEditor ? "_Editor" : "");
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return Path.Combine(root, name);
        }
    }
    public static string FilePath => Path.Combine(FolderPath, "save.json");

    static void Load()
    {
        if (data != null) return;
        data = new Dictionary<string, (char, string)>();
        if (!hooked) { hooked = true; Application.quitting += Save; }
        try
        {
            string path = File.Exists(FilePath) ? FilePath : File.Exists(FilePath + ".tmp") ? FilePath + ".tmp" : null;     // 쓰다가 꺼졌으면 임시 파일
            if (path == null) return;
            Store s = JsonUtility.FromJson<Store>(File.ReadAllText(path));
            if (s == null) return;
            for (int i = 0; i < s.keys.Count && i < s.types.Count && i < s.values.Count; i++)
                if (!string.IsNullOrEmpty(s.keys[i]) && s.types[i].Length > 0) data[s.keys[i]] = (s.types[i][0], s.values[i]);
        }
        catch (System.Exception e) { Debug.LogWarning("[Prefs] 저장 파일을 읽지 못함: " + e.Message); }
    }

    // ---------------- 읽기 (파일에 없으면 옛 레지스트리 값을 옮겨 옴)
    public static int GetInt(string key, int def = 0)
    {
        Load();
        if (data.TryGetValue(key, out var e)) return int.TryParse(e.value, NumberStyles.Integer, Inv, out int v) ? v : def;
        if (!PlayerPrefs.HasKey(key)) return def;
        int old = PlayerPrefs.GetInt(key, def);
        data[key] = ('i', old.ToString(Inv));
        dirty = true;
        return old;
    }

    public static float GetFloat(string key, float def = 0f)
    {
        Load();
        if (data.TryGetValue(key, out var e)) return float.TryParse(e.value, NumberStyles.Float, Inv, out float v) ? v : def;
        if (!PlayerPrefs.HasKey(key)) return def;
        float old = PlayerPrefs.GetFloat(key, def);
        data[key] = ('f', old.ToString("R", Inv));
        dirty = true;
        return old;
    }

    public static string GetString(string key, string def = "")
    {
        Load();
        if (data.TryGetValue(key, out var e)) return e.value ?? def;
        if (!PlayerPrefs.HasKey(key)) return def;
        string old = PlayerPrefs.GetString(key, def);
        data[key] = ('s', old);
        dirty = true;
        return old;
    }

    public static bool HasKey(string key)
    {
        Load();
        return data.ContainsKey(key) || PlayerPrefs.HasKey(key);
    }

    // ---------------- 쓰기
    public static void SetInt(string key, int value) => Set(key, 'i', value.ToString(Inv));
    public static void SetFloat(string key, float value) => Set(key, 'f', value.ToString("R", Inv));
    public static void SetString(string key, string value) => Set(key, 's', value ?? "");

    static void Set(string key, char type, string value)
    {
        Load();
        if (data.TryGetValue(key, out var e) && e.type == type && e.value == value) return;
        data[key] = (type, value);
        dirty = true;
    }

    // 옛 레지스트리 값도 지움 (지운 키가 다음에 읽을 때 되살아나지 않게)
    public static void DeleteKey(string key)
    {
        Load();
        if (data.Remove(key)) dirty = true;
        if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);
    }

    public static void Save()
    {
        if (data == null || !dirty) return;
        try
        {
            Directory.CreateDirectory(FolderPath);
            Store s = new Store();
            foreach (KeyValuePair<string, (char type, string value)> kv in data)
            {
                s.keys.Add(kv.Key);
                s.types.Add(kv.Value.type.ToString());
                s.values.Add(kv.Value.value);
            }
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(s));
            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);     // 한 번에 바꿔치기 (중간에 꺼져도 둘 중 하나는 남음)
            else File.Move(tmp, FilePath);
            dirty = false;
        }
        catch (System.Exception e) { Debug.LogWarning("[Prefs] 저장 파일을 쓰지 못함: " + e.Message); }
    }
}

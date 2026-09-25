using System.Text;
using UnityEngine;

// 바꿀 수 있는 키 (설정 → 조작)
public enum GameAction { Up, Down, Left, Right, Reload, Swap, Skill1, Skill2, Skill3, Interact, Upgrade }

// 키 설정: PlayerPrefs에 저장. 이동은 방향키도 항상 같이 됨
// 화면 글자의 {SKILL1} 같은 표시는 Loc.T가 지금 키 이름으로 바꿔 줌
public static class KeyBindings
{
    public static readonly GameAction[] All = (GameAction[])System.Enum.GetValues(typeof(GameAction));

    // 스킬 3번은 상점 · 레벨업 확정(Space)과 겹치지 않게 C
    static readonly KeyCode[] Defaults =
    {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D,
        KeyCode.R, KeyCode.Q,
        KeyCode.E, KeyCode.F, KeyCode.C,
        KeyCode.Space, KeyCode.T,
    };

    static readonly KeyCode[] Arrows = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

    static KeyCode[] keys;

    static void Load()
    {
        if (keys != null) return;
        keys = (KeyCode[])Defaults.Clone();
        for (int i = 0; i < keys.Length; i++)
            keys[i] = (KeyCode)PlayerPrefs.GetInt("keys." + (GameAction)i, (int)Defaults[i]);
    }

    public static KeyCode Get(GameAction a) { Load(); return keys[(int)a]; }

    // 다른 동작이 이미 쓰는 키면 서로 맞바꿈
    public static void Set(GameAction a, KeyCode key)
    {
        Load();
        KeyCode old = keys[(int)a];
        for (int i = 0; i < keys.Length; i++)
            if (i != (int)a && keys[i] == key) { keys[i] = old; PlayerPrefs.SetInt("keys." + (GameAction)i, (int)old); }
        keys[(int)a] = key;
        PlayerPrefs.SetInt("keys." + a, (int)key);
        Loc.RaiseChanged();
    }

    public static void ResetAll()
    {
        Load();
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = Defaults[i];
            PlayerPrefs.DeleteKey("keys." + (GameAction)i);
        }
        Loc.RaiseChanged();
    }

    public static bool Held(GameAction a)
    {
        if (Input.GetKey(Get(a))) return true;
        return a <= GameAction.Right && Input.GetKey(Arrows[(int)a]);
    }
    public static bool Down(GameAction a) => Input.GetKeyDown(Get(a));
    public static bool Up(GameAction a) => Input.GetKeyUp(Get(a));

    public static string Name(GameAction a) => KeyName(Get(a));

    public static string KeyName(KeyCode k)
    {
        if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return ((int)(k - KeyCode.Alpha0)).ToString();
        if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9) return "Num" + (int)(k - KeyCode.Keypad0);
        switch (k)
        {
            case KeyCode.LeftShift: case KeyCode.RightShift: return "Shift";
            case KeyCode.LeftControl: case KeyCode.RightControl: return "Ctrl";
            case KeyCode.LeftAlt: case KeyCode.RightAlt: return "Alt";
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            case KeyCode.Return: return "Enter";
            case KeyCode.BackQuote: return "`";
            case KeyCode.Minus: return "-";
            case KeyCode.Equals: return "=";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.Semicolon: return ";";
            case KeyCode.Quote: return "'";
            case KeyCode.Comma: return ",";
            case KeyCode.Period: return ".";
            case KeyCode.Slash: return "/";
            case KeyCode.Backslash: return "\\";
        }
        return k.ToString();
    }

    // 글자 속 {MOVE} {RELOAD} {SWAP} {SKILL1~3} {INTERACT} {UPGRADE} 를 지금 키 이름으로
    public static string Apply(string s)
    {
        if (s == null || s.IndexOf('{') < 0) return s;
        StringBuilder b = new StringBuilder(s);
        b.Replace("{MOVE}", Name(GameAction.Up) + " " + Name(GameAction.Left) + " " + Name(GameAction.Down) + " " + Name(GameAction.Right));
        b.Replace("{RELOAD}", Name(GameAction.Reload));
        b.Replace("{SWAP}", Name(GameAction.Swap));
        b.Replace("{SKILL1}", Name(GameAction.Skill1));
        b.Replace("{SKILL2}", Name(GameAction.Skill2));
        b.Replace("{SKILL3}", Name(GameAction.Skill3));
        b.Replace("{INTERACT}", Name(GameAction.Interact));
        b.Replace("{UPGRADE}", Name(GameAction.Upgrade));
        return b.ToString();
    }
}

// 씬에 하나뿐인 오브젝트를 매번 찾지 않고 기억해 둠
// FindFirstObjectByType과 같게, 기억한 오브젝트가 꺼지거나 사라지면 다시 찾음
public static class Cache<T> where T : Component
{
    static T cached;

    public static T Get
    {
        get
        {
            if (cached == null || !cached.gameObject.activeInHierarchy) cached = Object.FindFirstObjectByType<T>();
            return cached;
        }
    }
}

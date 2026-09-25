using UnityEngine;

// 캐릭터: 거너(기본) · 검사 · 도적은 처음부터, 궁수 · 연금술사는 포인트로 구매, 나머지 5칸은 준비 중
// 스탯은 모두 거너를 1(100%)로 한 배율. 고른 캐릭터 · 잠금 해제 · 보유 포인트는 PlayerPrefs에 저장
public enum CharacterId { Gunner = 0, Swordsman = 1, Rogue = 2, Archer = 3, Alchemist = 4, Slot6 = 5, Slot7 = 6, Slot8 = 7, Slot9 = 8, Slot10 = 9 }

public class CharacterDef
{
    public string name, title, description;
    public string body;             // Resources/Characters/<body> (null = 거너 원래 그림)
    public string held;             // 손에 드는 기본 무기 Resources/Weapons/weapon_<held>
    public string weapon, attack;   // 기본 무기 이름 · 평타 설명
    public string skill, skillDesc; // 우클릭 스킬
    public float hp = 1f, damage = 1f, attackSpeed = 1f, gauge = 1f, move = 1f;
    public float range;             // 평타 사거리 (칸), 0 = 무한
    public int mag;                 // 기본 탄창 수, 0 = 무한 (거너는 인스펙터 값)
    public int price;               // 0 = 처음부터, -1 = 준비 중
    public int[] pool;              // 지옥의 문에서 고를 수 있는 특수 능력 (무기 · 스킬 · 패시브)
    public Color color = Color.white;
}

public static class CharacterData
{
    public static readonly CharacterDef[] All =
    {
        new CharacterDef {
            name = "거너", title = "마지막 총잡이", body = null, held = "pistol",
            description = "리볼버와 여러 특수 총기를 다루는 총잡이. 모든 캐릭터의 기준.",
            weapon = "리볼버", attack = "마우스 방향으로 총알 발사",
            skill = "타겟팅 필살기", skillDesc = "누르고 있으면 시간이 느려지며 적을 조준, 떼면 조준한 적 모두에게 사격",
            range = 0f, mag = 8, price = 0, pool = Range(0, 20), color = new Color(0.4f, 0.85f, 0.9f) },
        new CharacterDef {
            name = "검사", title = "떠돌이 기사", body = "swordsman", held = "sword",
            description = "무겁고 느리지만 한 번에 여럿을 베는 장검의 달인.",
            weapon = "장검", attack = "검을 크게 휘둘러 앞쪽 부채꼴의 적을 직접 벱니다",
            skill = "회전 베기", skillDesc = "검을 사방으로 휘둘러 주변을 벱니다. 오래 누를수록 피해가 커집니다",
            hp = 1.2f, damage = 1.5f, attackSpeed = 0.4f, gauge = 1f, range = 5f, price = 0, pool = Range(20, 8),
            color = new Color(0.45f, 0.6f, 1f) },
        new CharacterDef {
            name = "도적", title = "그림자 칼날", body = "rogue", held = "shuriken",
            description = "약하지만 빠른 표창 세례와 그림자 돌진으로 싸우는 암살자.",
            weapon = "표창", attack = "끝없이 날아가는 표창",
            skill = "출혈 돌진", skillDesc = "무적 상태로 마우스 방향으로 돌진해, 지나간 적에게 출혈 피해를 입힙니다 (즉발)",
            hp = 0.8f, damage = 0.7f, attackSpeed = 1.4f, gauge = 1.5f, range = 0f, mag = 6, price = 0, pool = Range(28, 8),
            color = new Color(0.7f, 0.45f, 0.9f) },
        new CharacterDef {
            name = "궁수", title = "숲의 사냥꾼", body = "archer", held = "bow",
            description = "적을 꿰뚫는 화살과 하늘을 덮는 화살비의 명사수.",
            weapon = "사냥 활", attack = "적을 하나 더 꿰뚫는 화살",
            skill = "화살비", skillDesc = "마우스 위치에 화살비를 쏟아붓습니다 (즉발)",
            hp = 0.9f, damage = 1.1f, attackSpeed = 0.9f, gauge = 1.1f, move = 1.05f, range = 0f, price = 1500, pool = Range(36, 8),
            color = new Color(0.45f, 0.8f, 0.4f) },
        new CharacterDef {
            name = "연금술사", title = "미친 학자", body = "alchemist", held = "flask",
            description = "터지는 플라스크로 적 무리를 한꺼번에 녹이는 괴짜 학자.",
            weapon = "플라스크", attack = "던진 자리에서 터지는 플라스크 (범위 피해)",
            skill = "대폭발 플라스크", skillDesc = "누를수록 커지는 플라스크를 던져 크게 폭발하고 산성 웅덩이를 남깁니다",
            hp = 1f, damage = 0.9f, attackSpeed = 0.8f, gauge = 1.2f, range = 10f, price = 2500, pool = Range(44, 8),
            color = new Color(0.65f, 0.4f, 0.95f) },
        Coming(), Coming(), Coming(), Coming(), Coming(),
    };

    static CharacterDef Coming() => new CharacterDef { name = "???", title = "준비 중", body = "mystery", price = -1, pool = new int[0],
                                                        description = "아직 준비 중인 캐릭터입니다." };

    static int[] Range(int from, int count)
    {
        int[] a = new int[count];
        for (int i = 0; i < count; i++) a[i] = from + i;
        return a;
    }

    const string SelectedKey = "char.selected";
    const string UnlockKey = "char.unlocked.";
    const string PointsKey = "char.points";

    // 자동 테스트가 캐릭터를 정할 때 (저장값을 건드리지 않음)
    public static CharacterId? Override;

    public static CharacterDef Def(CharacterId id) => All[(int)id];

    public static bool IsDeveloped(CharacterId id) => Def(id).price >= 0;

    public static bool IsUnlocked(CharacterId id)
    {
        CharacterDef d = Def(id);
        if (d.price < 0) return false;
        return d.price == 0 || PlayerPrefs.GetInt(UnlockKey + (int)id, 0) == 1;
    }

    // 게임에서 고른 캐릭터 (트레일러 · 배치 모드 테스트는 거너)
    public static CharacterId Selected
    {
        get
        {
            if (Override.HasValue) return Override.Value;
            if (GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return CharacterId.Gunner;
            CharacterId id = (CharacterId)Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, All.Length - 1);
            return IsUnlocked(id) ? id : CharacterId.Gunner;
        }
        set
        {
            if (!IsUnlocked(value)) return;
            PlayerPrefs.SetInt(SelectedKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static CharacterDef Current => Def(Selected);
    public static bool IsGunner => Selected == CharacterId.Gunner;

    // ================================================================= 포인트 (적 처치 등으로 쌓이고 캐릭터 구매에 씀)
    public static int Points => PlayerPrefs.GetInt(PointsKey, 0);

    // 이번 판에 얻은 포인트 (게임 오버 화면에 보여 줌)
    public static int RunPoints { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "GameScene") RunPoints = 0;
            else Save();
        };
    }

    public static void AddPoints(int n)
    {
        if (n <= 0 || GameInput.Auto || (Application.isBatchMode && !Override.HasValue)) return;
        RunPoints += n;
        PlayerPrefs.SetInt(PointsKey, Points + n);
    }

    // 사면 true
    public static bool Buy(CharacterId id)
    {
        CharacterDef d = Def(id);
        if (d.price <= 0 || IsUnlocked(id) || Points < d.price) return false;
        PlayerPrefs.SetInt(PointsKey, Points - d.price);
        PlayerPrefs.SetInt(UnlockKey + (int)id, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void Save() => PlayerPrefs.Save();

    // 이 캐릭터가 지옥의 문에서 고를 수 있는 특수 능력인지
    public static bool InPool(int abilityId)
    {
        foreach (int id in Current.pool) if (id == abilityId) return true;
        return false;
    }
}

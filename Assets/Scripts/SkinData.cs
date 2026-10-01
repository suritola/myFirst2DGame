using System.Collections.Generic;
using UnityEngine;

// 스킨 (1.8.3~): 메인 메뉴 스킨 상점에서 포인트로 사서 장착. 겉모습 + 소리가 바뀌고 판정 · 크기는 그대로
//   캐릭터 스킨: 몸 그림 · 피격 소리 (영웅부터 이동 잔상, 전설은 오라 · 처치 연출 · 필살기 소리)
//   무기 스킨:   손에 든 무기 · 총알/투사체 색 · 공격 소리
//   이펙트 스킨: 명중 불꽃 · 처치 효과 · 코인 효과와 소리 (모든 캐릭터 공통)
//   커서 스킨 (1.8.7~): 마우스 포인터 색 · 모양 (모든 캐릭터 공통, CursorSkin)
// 등급: 0 일반 · 1 희귀 · 2 영웅 · 3 전설 (가격 5천 · 1만 5천 · 4만 · 10만)
public enum SkinKind { Character, Weapon, Effect, Cursor }

public class SkinDef
{
    public string id;               // 저장 · 그림 이름 (예: swordsman__dragon)
    public SkinKind kind;
    public int who = -1;            // 캐릭터 (-1 = 모든 캐릭터)
    public int tier;
    public string name, desc;       // 한국어 (Loc.T 로 번역)
    public Color color;             // 테마 색 (잔상 · 오라 · 투사체 · 불꽃)
    public string attackSound;      // 공격 소리를 바꿀 이름 (없으면 원래 소리 + 음높이 · 덧소리)
    public float pitch = 1f;        // 공격 소리 음높이
    public string layer;            // 공격할 때 겹쳐 나는 소리
    public string hurtSound;        // 피격 소리
    public string killSound;        // 처치 소리 (이펙트 스킨 · 전설)
    public int Price => SkinData.Prices[tier];
}

public static class SkinData
{
    public static readonly int[] Prices = { 5000, 15000, 40000, 100000 };
    public static readonly string[] TierNames = { "일반", "희귀", "영웅", "전설" };
    public static readonly Color[] TierColors =
    {
        new Color(0.72f, 0.72f, 0.76f), new Color(0.35f, 0.65f, 1f), new Color(0.75f, 0.45f, 1f), new Color(1f, 0.8f, 0.3f),
    };

    static SkinDef C(string id, int who, int tier, string name, string desc, Color color, string hurt = null, string attack = null, float pitch = 1f, string layer = null)
        => new SkinDef { id = id, kind = SkinKind.Character, who = who, tier = tier, name = name, desc = desc, color = color, hurtSound = hurt, attackSound = attack, pitch = pitch, layer = layer };
    static SkinDef W(string id, int who, int tier, string name, string desc, Color color, float pitch, string layer, string attack = null)
        => new SkinDef { id = id, kind = SkinKind.Weapon, who = who, tier = tier, name = name, desc = desc, color = color, pitch = pitch, layer = layer, attackSound = attack };
    static SkinDef E(string id, int tier, string name, string desc, Color color, string kill)
        => new SkinDef { id = id, kind = SkinKind.Effect, tier = tier, name = name, desc = desc, color = color, killSound = kill };
    static SkinDef P(string id, int tier, string name, string desc, Color color)
        => new SkinDef { id = id, kind = SkinKind.Cursor, tier = tier, name = name, desc = desc, color = color };

    // 모든 캐릭터가 같이 쓰는 종류 (캐릭터 고르기 없음)
    public static bool IsGlobal(SkinKind kind) => kind == SkinKind.Effect || kind == SkinKind.Cursor;

    const int G = 0, S = 1, R = 2, A = 3, L = 4;

    public static readonly SkinDef[] All =
    {
        // ---------------- 캐릭터
        C("gunner_nogun__desert", G, 0, "사막 방랑자", "모래빛 코트와 먼지 낀 모자. 피격 소리가 낮고 둔해집니다.", new Color(0.85f, 0.72f, 0.45f), "sk_hurt_low"),
        C("gunner_nogun__bounty", G, 1, "붉은 현상금 사냥꾼", "검은 가죽 코트에 붉은 목도리. 피격하면 짧은 휘파람 소리.", new Color(0.9f, 0.25f, 0.3f), "sk_hurt_whistle"),
        C("gunner_nogun__ghost", G, 2, "유령 총잡이", "빛바랜 푸른 코트의 유령. 움직이면 옅은 잔상이 남고, 피격하면 메아리가 울립니다.", new Color(0.7f, 0.95f, 1f), "sk_hurt_echo"),
        C("gunner_nogun__gold", G, 3, "황금 심판자", "황금빛 심판자. 금빛 오라가 감돌고, 적을 쓰러뜨리면 금빛 파편이 흩어지며 필살기에 종소리가 울립니다.", new Color(1f, 0.82f, 0.3f), "sk_hurt_bell"),

        C("swordsman__bronze", S, 0, "청동 기사", "오래된 청동 갑옷. 피격 소리가 묵직해집니다.", new Color(0.85f, 0.6f, 0.35f), "sk_hurt_low"),
        C("swordsman__black", S, 1, "흑기사", "칠흑 갑옷에 붉은 깃털. 피격하면 쇳소리가 울립니다.", new Color(0.8f, 0.15f, 0.2f), "sk_hurt_metal"),
        C("swordsman__paladin", S, 2, "성기사", "흰 갑옷과 금빛 장식, 푸른 망토. 움직이면 빛 잔상이 남고, 피격하면 맑은 방울 소리.", new Color(1f, 0.92f, 0.6f), "sk_hurt_chime"),
        C("swordsman__dragon", S, 3, "용기사", "용의 뿔 투구와 붉은 비늘 갑옷. 불씨 오라가 감돌고, 처치하면 불꽃이 튀며 필살기에 용의 포효가 울립니다.", new Color(1f, 0.45f, 0.15f), "sk_hurt_roar"),

        C("rogue__sand", R, 0, "모래 도적", "모래빛 두건. 피격 소리가 가벼워집니다.", new Color(0.85f, 0.7f, 0.45f), "sk_hurt_light"),
        C("rogue__raven", R, 1, "밤까마귀", "검은 두건과 붉은 목도리, 하얗게 빛나는 눈. 피격하면 까마귀 날갯짓 소리.", new Color(0.75f, 0.1f, 0.2f), "sk_hurt_flap"),
        C("rogue__ninja", R, 2, "닌자", "남색 복면과 길게 휘날리는 붉은 목도리. 움직이면 그림자 잔상, 피격하면 연기 소리.", new Color(0.3f, 0.4f, 0.8f), "sk_hurt_smoke"),
        C("rogue__void", R, 3, "그림자 군주", "허공의 보랏빛 망토와 빛나는 분홍 눈. 어둠의 오라가 감돌고, 처치하면 그림자가 흩어지며 필살기에 속삭임이 울립니다.", new Color(0.65f, 0.3f, 1f), "sk_hurt_void"),

        C("archer__autumn", A, 0, "가을 사냥꾼", "단풍빛 두건. 피격 소리가 부드러워집니다.", new Color(0.95f, 0.55f, 0.2f), "sk_hurt_light"),
        C("archer__snow", A, 1, "설원 궁수", "흰 모피 두건과 얼음빛 옷. 피격하면 얼음 부서지는 소리.", new Color(0.7f, 0.88f, 1f), "sk_hurt_ice"),
        C("archer__elf", A, 2, "엘프 레인저", "에메랄드 두건과 금빛 머리띠. 움직이면 나뭇잎 잔상, 피격하면 바람 소리.", new Color(0.35f, 0.9f, 0.5f), "sk_hurt_wind"),
        C("archer__sun", A, 3, "태양의 궁수", "태양빛 두건과 흰 옷. 햇살 오라가 감돌고, 처치하면 빛이 터지며 필살기에 성가가 울립니다.", new Color(1f, 0.85f, 0.35f), "sk_hurt_bell"),

        C("alchemist__herb", L, 0, "초록 약사", "약초빛 옷. 피격 소리가 보글거립니다.", new Color(0.45f, 0.8f, 0.45f), "sk_hurt_bubble"),
        C("alchemist__plague", L, 1, "역병 의사", "검은 외투와 하얀 부리 가면. 피격하면 둔탁한 가면 소리.", new Color(0.8f, 0.2f, 0.25f), "sk_hurt_metal"),
        C("alchemist__mercury", L, 2, "수은 학자", "은빛 외투와 푸른 고글. 움직이면 수은 잔상, 피격하면 유리 울림.", new Color(0.75f, 0.85f, 0.95f), "sk_hurt_chime"),
        C("alchemist__stone", L, 3, "현자의 돌", "진홍 외투와 금빛 머리. 붉은 돌 조각이 주위를 돌고, 처치하면 붉은 결정이 흩어지며 필살기에 신비로운 공명이 울립니다.", new Color(1f, 0.3f, 0.25f), "sk_hurt_void"),

        // ---------------- 무기 (공격 소리 · 투사체 색)
        W("pistol__silver", G, 1, "은장 리볼버", "은으로 새긴 권총. 총알이 푸르스름한 은빛으로 날고, 총소리에 맑은 금속 울림이 더해집니다.", new Color(0.8f, 0.9f, 1f), 1.12f, "clank"),
        W("pistol__dragon", G, 2, "용의 숨결", "붉은 금빛 권총. 총알이 불꽃색으로 날고, 쏠 때마다 불붙는 소리가 납니다. 진화한 무기에도 색과 소리가 이어집니다.", new Color(1f, 0.55f, 0.2f), 0.9f, "ignite"),
        W("sword__frost", S, 1, "푸른 서리검", "얼음빛 칼날. 베기가 서릿빛으로 빛나고 맑은 울림이 더해집니다.", new Color(0.6f, 0.9f, 1f), 1.1f, "shimmer"),
        W("sword__obsidian", S, 2, "흑요석 마검", "검보랏빛 마검. 베기가 보랏빛으로 물들고 번개 튀는 소리가 섞입니다.", new Color(0.65f, 0.35f, 1f), 0.85f, "zap"),
        W("shuriken__gold", R, 1, "황금 표창", "금으로 만든 표창. 금빛으로 날고 반짝이는 소리가 납니다.", new Color(1f, 0.85f, 0.35f), 1.15f, "sparkle"),
        W("shuriken__bloodmoon", R, 2, "혈월 표창", "핏빛 초승달 표창. 붉게 날고 날카로운 베기 소리가 섞입니다.", new Color(1f, 0.25f, 0.3f), 0.85f, "slash"),
        W("bow__silver", A, 1, "은빛 활", "은으로 감은 활. 화살이 은빛으로 날고 시위 소리가 맑아집니다.", new Color(0.85f, 0.92f, 1f), 1.12f, "ding"),
        W("bow__worldtree", A, 2, "세계수 활", "살아 있는 나무 활. 화살이 초록빛으로 날고 바람과 방울 소리가 섞입니다.", new Color(0.5f, 1f, 0.55f), 0.95f, "chime"),
        W("flask__crystal", L, 1, "수정 플라스크", "투명한 수정 플라스크. 폭발이 맑게 빛나고 유리 울림이 더해집니다.", new Color(0.7f, 0.9f, 1f), 1.15f, "shimmer"),
        W("flask__lava", L, 2, "용암 플라스크", "끓어오르는 용암 플라스크. 폭발이 주황빛으로 물들고 묵직한 폭음이 섞입니다.", new Color(1f, 0.5f, 0.15f), 0.85f, "thump"),

        // ---------------- 이펙트 (모든 캐릭터)
        E("skin_bluefire", 0, "푸른 불꽃", "명중 불꽃 · 처치 효과 · 코인 반짝임이 푸른 불꽃으로 바뀝니다.", new Color(0.45f, 0.75f, 1f), null),
        E("skin_sakura", 1, "벚꽃", "명중하면 분홍 불꽃, 적이 쓰러지면 벚꽃잎이 흩날립니다. 코인 소리가 맑아집니다.", new Color(1f, 0.6f, 0.75f), "sk_kill_petal"),
        E("skin_thunder", 2, "뇌전", "명중하면 노란 전기, 적이 쓰러지면 작은 번개가 칩니다. 코인에 전기 튀는 소리.", new Color(1f, 0.9f, 0.35f), "sk_kill_zap"),
        E("skin_starlight", 3, "별의 축복", "명중 불꽃이 무지갯빛으로 바뀌고, 적이 쓰러지면 별이 터지며 맑은 종소리가 울립니다. 코인은 별가루로 반짝입니다.", Color.white, "sk_kill_star"),

        // ---------------- 커서 (메뉴 화살표 + 전투 조준점, 설정에서 게임 커서를 켰을 때)
        P("cursor_bone", 0, "뼈 커서", "빛바랜 뼛빛 화살표와 조준점.", new Color(0.84f, 0.82f, 0.75f)),
        P("cursor_ember", 1, "불씨 커서", "타오르는 주황빛 화살표와 불꽃 조준점.", new Color(0.92f, 0.43f, 0.18f)),
        P("cursor_soul", 2, "영혼 커서", "보랏빛 영혼 화살표와 푸른 빛이 도는 조준점.", new Color(0.59f, 0.37f, 0.92f)),
        P("cursor_gold", 3, "황금 커서", "황금빛 화살표와 조준점. 둘레에 별빛이 반짝이며 돕니다.", new Color(0.96f, 0.78f, 0.31f)),
    };

    static readonly Dictionary<string, SkinDef> byId = new Dictionary<string, SkinDef>();
    public static SkinDef Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (byId.Count == 0) foreach (SkinDef s in All) byId[s.id] = s;
        return byId.TryGetValue(id, out SkinDef d) ? d : null;
    }

    // ================================================================= 저장 (PlayerPrefs)
    public static bool Owns(string id) => PlayerPrefs.GetInt("skin.own." + id, 0) == 1;

    static string EquipKey(SkinKind kind, int who) => "skin.eq." + (int)kind + "." + (IsGlobal(kind) ? -1 : who);

    // 장착한 스킨 (없으면 null = 기본)
    public static SkinDef Equipped(SkinKind kind, int who) => Get(PlayerPrefs.GetString(EquipKey(kind, who), ""));
    public static SkinDef Equipped(SkinKind kind) => Equipped(kind, (int)CharacterData.Selected);

    public static void Equip(SkinDef s)
    {
        if (s == null || !Owns(s.id)) return;
        PlayerPrefs.SetString(EquipKey(s.kind, s.who), s.id);
        PlayerPrefs.Save();
    }

    public static void Unequip(SkinKind kind, int who)
    {
        PlayerPrefs.SetString(EquipKey(kind, who), "");
        PlayerPrefs.Save();
    }

    // 스킨을 샀을 때 (업적)
    public static event System.Action<SkinDef> Bought;

    // 사면 true (포인트 차감 · 바로 장착)
    public static bool Buy(SkinDef s)
    {
        if (s == null || Owns(s.id) || CharacterData.Points < s.Price) return false;
        CharacterData.SpendPoints(s.Price);
        PlayerPrefs.SetInt("skin.own." + s.id, 1);
        Equip(s);
        Bought?.Invoke(s);
        return true;
    }

    // ================================================================= 게임에서 쓰는 값
    // 캐릭터 몸 그림 시트 이름 (Resources/Characters/…)
    public static string BodySheet(string baseSheet)
    {
        SkinDef s = Equipped(SkinKind.Character);
        return s != null ? s.id : baseSheet;
    }

    // 손에 든 기본 무기 그림 (Resources/Weapons/weapon_…)
    public static string HeldSprite(string held)
    {
        SkinDef s = Equipped(SkinKind.Weapon);
        return "Weapons/weapon_" + (s != null ? s.id : held);
    }
}

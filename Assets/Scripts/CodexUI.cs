using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴의 도감: 적 · 보스 · 무기 · 스킬 · 패시브 · 진화 · 레벨업 · 상점 정보를 탭별 카드로 한눈에 보여줌
// 자료는 Resources/CodexData (특수 능력, 아이콘, 적 프리팹)와 아래 설명 표에서 가져옴
public static class CodexUI
{
    static GameObject open;
    static int tab;
    static RectTransform grid;
    static ScrollRect scroll;
    static TMP_Text hint;
    static readonly List<Button> tabButtons = new List<Button>();

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.72f, 0.68f, 0.76f);
    static readonly Color Accent = new Color(1f, 0.72f, 0.55f);

    static readonly string[] Tabs = { "적", "보스", "무기", "스킬", "패시브", "진화", "레벨업", "상점" };
    static readonly string[] Hints =
    {
        "스테이지마다 나오는 적과 스킬 · 붉은 경고가 보이면 피하세요",
        "각 스테이지의 마지막 적 · 체력이 절반 아래로 떨어지면 특수 스킬을 씁니다",
        "Q로 교체 · 우클릭 필살기는 무기마다 다릅니다",
        "E · F · Space 순서로 배정 (최대 3개)",
        "고르면 항상 적용되는 능력",
        "특수 강화(T)에서 이미 가진 능력을 한 번 더 고르면 진화합니다",
        "레벨이 오를 때 세 장의 카드 중 하나를 고릅니다",
        "상점 제단(적 35마리마다)에서 코인으로 강화합니다",
    };

    static readonly string[] StageNames = { "지하 묘역", "불타는 지옥", "초원" };

    // 적 이름 · 설명 (프리팹 이름으로 찾음)
    static readonly Dictionary<string, (string name, string desc)> EnemyText = new Dictionary<string, (string, string)>
    {
        { "Enermy", ("해골", "지하 묘역의 기본 적. 스킬 없이 곧장 달려듭니다.") },
        { "Enemy2", ("구울", "떨어질 곳에 원을 띄운 뒤 높이 뛰어 내려찍습니다.") },
        { "Enemy3", ("망령", "사라졌다가 표시한 곳에 나타나며 영혼 폭발을 일으킵니다.") },
        { "HellImp", ("임프", "지그재그로 움직이며 플레이어 자리에 X자 레이저를 쏩니다.") },
        { "HellFlameSkull", ("불꽃 해골", "가까이 오면 부풀어 오르다 폭발합니다.") },
        { "HellHound", ("지옥견", "경로를 보여준 뒤 일직선으로 돌진합니다.") },
        { "HellGolem", ("용암 골렘", "점점 넓어지는 세 번의 지면 파동을 일으킵니다.") },
        { "HellKnight", ("악마 기사", "칼날을 휘감고 회전하며 플레이어 쪽으로 미끄러집니다.") },
        { "MeadowSlime", ("초원 슬라임", "밟으면 아픈 산성 웅덩이를 남깁니다.") },
        { "GiantBee", ("거대 벌", "플레이어 주변에 꽃가루 구름을 뿌립니다. 안에 있으면 느려지고 조금씩 아픕니다.") },
        { "MushroomBrute", ("버섯 거인", "주변에 버섯 지뢰를 심고 잠시 뒤 차례로 터뜨립니다.") },
        { "DireWolf", ("다이어울프", "울부짖어 주변 적을 잠시 빠르게 만듭니다.") },
        { "Treant", ("트렌트", "플레이어 쪽으로 뿌리 가시를 차례로 솟게 합니다.") },
    };

    static readonly string[] BossDesc =
    {
        "부하를 부르며 망령의 손아귀 · 저주 표식 · 뼈 가시 격자를 씁니다.\n특수: 망자의 의식 - 영혼 등불이 돌며 나선 탄막을 쏜 뒤 대폭발",
        "화염 돌진 · 운석 낙하 · 화염 파동(틈으로 피하세요)을 씁니다.\n특수: 십자 불길 - 네 줄기 불기둥이 천천히 회전",
        "대점프 · 산성 비 · 구르기 돌진을 씁니다. 쓰러질 때마다 작고 빠르게 분열합니다 (1 → 2 → 3마리).\n특수: 슬라임 폭우 - 빠른 대점프 3연속",
    };

    // 필살기 설명 (무기 ID 순서) · 즉발 여부
    static readonly string[] UltDesc =
    {
        "마우스 쪽으로 거대한 부채꼴 폭발을 3번 연달아 일으킵니다.",
        "우클릭을 누른 채 방향을 정하면 충전 뒤 화면을 가로지르는 굵은 광선을 쏩니다. 광선 위의 적이 많을수록 강합니다.",
        "두 줄기 나선으로 사방에 총알을 난사합니다. 움직이며 쓸 수 있습니다.",
        "우클릭을 누른 채 자리를 정하면 불기둥이 생겨 적을 빨아들이고 태웁니다.",
        "영혼 구슬들이 주위를 돌다 조준한 적에게 번갈아 달려듭니다.",
        "먹구름이 따라다니며 조준한 적들에게 번개를 연달아 떨어뜨립니다.",
        "무적 상태로 조준한 적들 사이를 순간이동하며 벤 뒤 제자리로 돌아옵니다.",
        "플레이어에서 마우스 쪽으로 줄지어 운석이 떨어집니다.",
    };
    static readonly bool[] UltInstant = { true, false, true, false, false, false, false, true };

    static readonly (string name, string desc)[] LevelUps =
    {
        ("관통하는 총알", "총알이 적을 1마리 더 관통합니다."),
        ("코인충", "적이 코인을 1개 더 떨어뜨립니다."),
        ("재활용 에너지", "스킬 게이지가 20% 줄어 필살기를 더 자주 씁니다."),
        ("노려보는 눈빛", "필살기를 조준하는 동안 타겟이 된 적이 20% 더 느려집니다."),
        ("더 많은 경험치", "처치 경험치가 10% 늘어납니다."),
        ("코인 자석", "주변의 코인을 끌어옵니다. 범위 +4 (최대 4번)"),
        ("멀티 샷", "한 번에 쏘는 총알이 1발 늘지만 한 발당 피해는 줄어듭니다."),
        ("밀어내기", "총알이 적을 밀어내는 힘 +25% (최대 3번)"),
        ("강철같은 심장", "체력을 모두 회복하고 최대 체력이 12% 늘어납니다."),
        ("단단한 신체", "받는 피해 -12% (최대 3번)"),
        ("생명의 샘", "시간이 지나면 체력이 회복됩니다. 초당 +0.5 (최대 4번)"),
        ("피의 굶주림", "적을 처치할 때마다 체력 +1 회복 (최대 3번)"),
    };

    static readonly (string icon, string name, string desc)[] Shops =
    {
        ("fx_prompt", "능력치 상점", "공격력 · 공격 속도 · 재장전 속도 · 탄창 · 이동 속도를 올립니다. 살수록 값이 오릅니다."),
        ("fx_muzzle", "무기 강화", "2장부터. 특수 무기마다 피해 · 연사 · 탄창 · 고유 특성을 올립니다."),
        ("fx_reticle", "스킬 강화", "필살기의 위력과 무기마다 다른 특성(타겟 수 · 지속 시간 등)을 올립니다."),
        ("fx_orb", "특수 능력 포인트", "중간 보스를 잡으면 얻습니다. T를 눌러 새 능력을 배우거나 가진 능력을 진화합니다."),
    };

    // ================================================================= window
    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "CodexPanel", new Vector2(1640f, 960f), out open);
        UIKit.Text(win, "도감", 50f, Gold, new Vector2(0f, 425f), new Vector2(600f, 70f));
        UIKit.MakeButton(win, "닫기", new Vector2(720f, 425f), new Vector2(150f, 58f), Close, 22f);

        tabButtons.Clear();
        float w = 184f;
        for (int i = 0; i < Tabs.Length; i++)
        {
            int t = i;
            tabButtons.Add(UIKit.MakeButton(win, Tabs[i], new Vector2((i - (Tabs.Length - 1) * 0.5f) * (w + 12f), 345f), new Vector2(w, 64f), () => Show(t), 26f));
        }
        hint = UIKit.Text(win, "", 22f, Dim, new Vector2(0f, 287f), new Vector2(1500f, 36f));

        BuildScroll(win);
        Show(0);
    }

    static void BuildScroll(RectTransform win)
    {
        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -112f), new Vector2(1560f, 700f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();

        grid = UIKit.Rect("Grid", view, Vector2.zero, new Vector2(1540f, 0f));
        grid.anchorMin = new Vector2(0.5f, 1f);
        grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = Vector2.zero;
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(756f, 240f);
        g.spacing = new Vector2(16f, 14f);
        g.padding = new RectOffset(6, 6, 10, 10);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 2;
        ContentSizeFitter fit = grid.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = grid;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
    }

    static void Show(int t)
    {
        tab = t;
        for (int i = grid.childCount - 1; i >= 0; i--) Object.Destroy(grid.GetChild(i).gameObject);
        for (int i = 0; i < tabButtons.Count; i++)
        {
            bool on = i == tab;
            tabButtons[i].GetComponent<Image>().color = on ? Gold : new Color(0.6f, 0.58f, 0.65f);
            tabButtons[i].transform.localScale = Vector3.one * (on ? 1.05f : 1f);
        }
        hint.text = Loc.T(Hints[tab]);

        CodexData data = CodexData.Load();
        switch (tab)
        {
            case 0: Enemies(data); break;
            case 1: Bosses(data); break;
            case 2: Specials(data, SpecialKind.Weapon); break;
            case 3: Specials(data, SpecialKind.Skill); break;
            case 4: Specials(data, SpecialKind.Passive); break;
            case 5: Evolutions(data); break;
            case 6: LevelUpCards(data); break;
            case 7:
                foreach (var s in Shops) Card(FxIcon(s.icon), Gold, Loc.T(s.name), "", Loc.T(s.desc));
                break;
        }
        scroll.verticalNormalizedPosition = 1f;
    }

    // ================================================================= tabs
    static void Enemies(CodexData data)
    {
        Card(FxIcon("fx_warn"), Color.white, Loc.T("중간 보스"), Loc.T("2장부터"),
             Loc.T("단계가 오를 때마다 크고 단단한 적이 나타납니다. 체력 12배 · 피해 1.5배. 쓰러뜨리면 특수 능력 포인트를 줍니다."));
        if (data == null || data.enemies == null) return;
        for (int i = 0; i < data.enemies.Length; i++)
        {
            GameObject p = data.enemies[i];
            if (p == null) continue;
            EnermyController e = p.GetComponent<EnermyController>();
            EnemyText.TryGetValue(p.name, out var txt);
            int stage = data.enemyStage != null && i < data.enemyStage.Length ? data.enemyStage[i] : 0;
            string tag = Loc.T(StageNames[Mathf.Clamp(stage, 0, 2)]);
            if (e != null) tag += "  ·  " + Stats(e.setEnemyHP, e.speed, e.contactDamage);
            Card(SpriteOf(p), Color.white, Loc.T(txt.name ?? p.name), tag, Loc.T(txt.desc ?? ""));
        }
    }

    static void Bosses(CodexData data)
    {
        if (data == null || data.bosses == null) return;
        for (int i = 0; i < data.bosses.Length; i++)
        {
            GameObject p = data.bosses[i];
            if (p == null) continue;
            bosss b = p.GetComponent<bosss>();
            int kind = b != null ? b.bossKind : i;
            string tag = Loc.T(StageNames[Mathf.Clamp(i, 0, 2)]);
            if (b != null) tag += "  ·  " + Stats(b.setEnemyHP, b.speed, b.contactDamage);
            Card(SpriteOf(p), Color.white, bossbar.BossName(kind), tag, Loc.T(BossDesc[Mathf.Clamp(kind, 0, 2)]));
        }
    }

    static void Specials(CodexData data, SpecialKind kind)
    {
        if (kind == SpecialKind.Weapon)
            Card(FxIcon("fx_muzzle"), Gold, Loc.T("기본 권총"), Loc.T("처음부터 가진 무기"),
                 Loc.T("좌클릭으로 사격, R로 재장전.") + "\n" + Accent.Tag(Loc.T("필살기") + " · " + SpecialAbilities.UltName(SpecialAbilities.PistolUlt))
                 + "  " + Loc.T("우클릭을 누른 채 적을 조준하고, 떼면 조준한 적들에게 한꺼번에 쏩니다."));
        if (data == null || data.specials == null) return;
        for (int id = 0; id < data.specials.Length; id++)
        {
            SpecialDef d = data.specials[id];
            if (d == null || d.kind != kind) continue;
            string body = Loc.T(d.description).Replace("\n", " ");
            string tag = kind == SpecialKind.Weapon ? Loc.T("특수 무기") : kind == SpecialKind.Skill ? Loc.T("스킬") : Loc.T("패시브");
            if (kind == SpecialKind.Weapon && id < UltDesc.Length)
            {
                tag += "  ·  " + Loc.T("강화 특성") + ": " + SpecialAbilities.TraitName(id) + " (" + SpecialAbilities.TraitStep(id) + ")";
                body += "\n" + Accent.Tag(Loc.T("필살기") + " · " + SpecialAbilities.UltName(id) + " (" + Loc.T(UltInstant[id] ? "즉발" : "조준") + ")")
                        + "  " + Loc.T(UltDesc[id]);
            }
            Card(d.icon, Color.white, Loc.T(d.name), tag, body);
        }
    }

    static void Evolutions(CodexData data)
    {
        if (data == null || data.specials == null) return;
        for (int id = 0; id < data.specials.Length && id < SpecialAbilities.EvolveTexts.Length; id++)
        {
            SpecialDef d = data.specials[id];
            if (d == null) continue;
            string kindName = d.kind == SpecialKind.Weapon ? Loc.T("특수 무기") : d.kind == SpecialKind.Skill ? Loc.T("스킬") : Loc.T("패시브");
            Card(d.icon, new Color(1f, 0.92f, 0.7f), Loc.T(d.name) + " · " + Loc.T("진화"), kindName,
                 Loc.T(SpecialAbilities.EvolveTexts[id]));
        }
    }

    static void LevelUpCards(CodexData data)
    {
        for (int i = 0; i < LevelUps.Length; i++)
        {
            Sprite icon = data != null && data.abilityIcons != null && i < data.abilityIcons.Length ? data.abilityIcons[i] : null;
            Card(icon, Color.white, Loc.T(LevelUps[i].name), "", Loc.T(LevelUps[i].desc));
        }
    }

    // ================================================================= card
    static void Card(Sprite icon, Color tint, string title, string tag, string body)
    {
        RectTransform card = UIKit.Rect("Card", grid, Vector2.zero, new Vector2(756f, 240f));
        Image bg = card.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.24f, 0.2f, 0.3f, 1f);
        RectTransform inner = UIKit.Rect("Inner", card, Vector2.zero, new Vector2(740f, 224f));
        inner.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.16f, 0.97f);

        // 아이콘 칸
        RectTransform frame = UIKit.Rect("IconFrame", inner, new Vector2(-282f, 0f), new Vector2(160f, 160f));
        frame.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.17f, 0.26f, 1f);
        if (icon != null)
        {
            RectTransform ir = UIKit.Rect("Icon", frame, Vector2.zero, new Vector2(140f, 140f));
            Image img = ir.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.color = tint;
            img.raycastTarget = false;
        }

        TMP_Text t = UIKit.Text(inner, "", 32f, Gold, new Vector2(90f, 80f), new Vector2(540f, 44f), TextAlignmentOptions.Left);
        t.text = title;
        TMP_Text g = UIKit.Text(inner, "", 20f, Dim, new Vector2(90f, 46f), new Vector2(540f, 28f), TextAlignmentOptions.Left);
        g.text = tag;
        TMP_Text b = UIKit.Text(inner, "", 22f, Parch, new Vector2(90f, -38f), new Vector2(540f, 136f), TextAlignmentOptions.TopLeft);
        b.fontSizeMin = 14f;
        b.text = body;
    }

    static string Stats(float hp, float speed, float damage) =>
        Loc.T("체력") + " " + hp.ToString("0") + "  ·  " + Loc.T("속도") + " " + speed.ToString("0.#") + "  ·  " + Loc.T("접촉 피해") + " " + damage.ToString("0");

    static string Tag(this Color c, string s) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";

    static Sprite SpriteOf(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
        return sr != null ? sr.sprite : null;
    }

    static Sprite FxIcon(string name)
    {
        Sprite[] f = Fx.Frames(name);
        return f.Length > 0 ? f[0] : null;
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}

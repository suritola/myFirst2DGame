using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD: 무기 · 스킬 · 패시브를 아이콘 칸으로 (왼쪽 아래)
// 칸마다 쿨타임(어두운 원), 탄창 · 남은 시간 · 중첩 같은 짧은 상태, 마우스를 올리면 자세한 설명과 지금 상태
// 창 이름표를 누르면 접었다 폈다 (접힘 상태는 저장)
public partial class SpecialAbilities
{
    class HudTile
    {
        public int id;
        public Image border, icon, cooldown;
        public TextMeshProUGUI label, key;
    }

    class HudGroup
    {
        public string title, prefKey;
        public RectTransform root, tiles;
        public TextMeshProUGUI header;
        public bool collapsed;
        public readonly List<HudTile> list = new List<HudTile>();
    }

    HudGroup weaponGroup, skillGroup, passiveGroup;
    const int PistolRow = -1;
    const float TileSize = 50f, TileGap = 6f, HeaderHeight = 26f, GroupGap = 8f;
    static readonly Color TileBorder = new Color(0.3f, 0.26f, 0.34f, 0.95f);
    static readonly Color TileActive = new Color(0.96f, 0.75f, 0.3f, 1f);
    static readonly Color TileText = new Color(0.95f, 0.92f, 0.85f);

    void BuildHud()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        weaponGroup = NewGroup(canvas, "WeaponSlot", Loc.T("무기") + " [" + KeyBindings.Name(GameAction.Swap) + "]", "hud.fold.weapon");
        skillGroup = NewGroup(canvas, "SkillSlot", Loc.T("스킬"), "hud.fold.skill");
        passiveGroup = NewGroup(canvas, "PassiveSlot", Loc.T("패시브"), "hud.fold.passive");
    }

    HudGroup NewGroup(Canvas canvas, string name, string title, string prefKey)
    {
        HudGroup g = new HudGroup { title = title, prefKey = prefKey };
        GameObject go = new GameObject(name, typeof(RectTransform));
        g.root = go.GetComponent<RectTransform>();
        g.root.SetParent(canvas.transform, false);
        Transform ammo = canvas.transform.Find("AmmoPanel");
        if (ammo != null) g.root.SetSiblingIndex(ammo.GetSiblingIndex() + 1);
        g.root.anchorMin = g.root.anchorMax = g.root.pivot = Vector2.zero;

        // 이름표 (누르면 접기 · 펴기)
        GameObject head = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform hr = head.GetComponent<RectTransform>();
        hr.SetParent(g.root, false);
        hr.anchorMin = hr.anchorMax = hr.pivot = Vector2.zero;
        hr.sizeDelta = new Vector2(150f, HeaderHeight);
        Image himg = head.GetComponent<Image>();
        himg.color = new Color(0.08f, 0.06f, 0.1f, 0.75f);
        Button hb = head.GetComponent<Button>();
        hb.navigation = new Navigation { mode = Navigation.Mode.None };
        hb.onClick.AddListener(() =>
        {
            g.collapsed = !g.collapsed;
            PlayerPrefs.SetInt(g.prefKey, g.collapsed ? 1 : 0);
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            LayoutGroups();
            fx.Play("clank", 0.25f, 1.8f);
        });
        g.header = HudText(hr, 15f, new Color(1f, 0.78f, 0.5f), TextAlignmentOptions.Left);
        g.header.margin = new Vector4(8f, 0f, 6f, 0f);

        GameObject tiles = new GameObject("Tiles", typeof(RectTransform));
        g.tiles = tiles.GetComponent<RectTransform>();
        g.tiles.SetParent(g.root, false);
        g.tiles.anchorMin = g.tiles.anchorMax = g.tiles.pivot = Vector2.zero;

        g.collapsed = PlayerPrefs.GetInt(prefKey, 0) == 1;
        go.SetActive(false);
        return g;
    }

    TextMeshProUGUI HudText(RectTransform parent, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    Image HudImage(RectTransform parent, string name, Sprite sprite, Color color, float inset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // 고른 능력을 종류별 창에 한 칸씩 넣음
    void RebuildHudRows()
    {
        if (weaponGroup == null) return;
        List<int> w = new List<int>(), s = new List<int>(), p = new List<int>();
        if (weapons.Count > 0) w.Add(PistolRow);
        foreach (int id in equipped)
        {
            if (abilities[id].kind == SpecialKind.Weapon) w.Add(id);
            else if (abilities[id].kind == SpecialKind.Skill) s.Add(id);
            else p.Add(id);
        }
        FillGroup(weaponGroup, w);
        FillGroup(skillGroup, s);
        FillGroup(passiveGroup, p);
        LayoutGroups();
    }

    void FillGroup(HudGroup g, List<int> ids)
    {
        foreach (HudTile t in g.list) Destroy(t.border.gameObject);
        g.list.Clear();
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            GameObject go = new GameObject("Tile", typeof(RectTransform), typeof(Image));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(g.tiles, false);
            r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
            r.sizeDelta = new Vector2(TileSize, TileSize);
            r.anchoredPosition = new Vector2(i * (TileSize + TileGap), 0f);

            HudTile t = new HudTile { id = id };
            t.border = go.GetComponent<Image>();
            t.border.color = TileBorder;
            t.border.raycastTarget = true;                  // 마우스를 올리면 설명
            HudImage(r, "Back", null, new Color(0.07f, 0.06f, 0.09f, 0.92f), 3f);
            t.icon = HudImage(r, "Icon", TileIcon(id), Color.white, 6f);
            t.icon.preserveAspect = true;
            t.cooldown = HudImage(r, "Cooldown", barFillSprite, new Color(0f, 0f, 0f, 0.62f), 3f);
            t.cooldown.type = Image.Type.Filled;
            t.cooldown.fillMethod = Image.FillMethod.Radial360;
            t.cooldown.fillOrigin = (int)Image.Origin360.Top;
            t.cooldown.fillClockwise = false;
            t.cooldown.fillAmount = 0f;

            RectTransform lr = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            lr.SetParent(r, false);
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = new Vector2(1f, 0f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.sizeDelta = new Vector2(0f, 18f);
            lr.anchoredPosition = new Vector2(0f, 2f);
            t.label = HudText(lr, 14f, TileText, TextAlignmentOptions.Center);
            t.label.outlineWidth = 0.25f;
            t.label.outlineColor = new Color32(0, 0, 0, 255);

            RectTransform kr = new GameObject("Key", typeof(RectTransform)).GetComponent<RectTransform>();
            kr.SetParent(r, false);
            kr.anchorMin = new Vector2(0f, 1f);
            kr.anchorMax = new Vector2(1f, 1f);
            kr.pivot = new Vector2(0.5f, 1f);
            kr.sizeDelta = new Vector2(0f, 16f);
            kr.anchoredPosition = new Vector2(0f, -1f);
            t.key = HudText(kr, 12f, new Color(1f, 0.85f, 0.45f), TextAlignmentOptions.TopLeft);
            t.key.margin = new Vector4(5f, 0f, 0f, 0f);
            t.key.outlineWidth = 0.25f;
            t.key.outlineColor = new Color32(0, 0, 0, 255);

            TooltipTrigger tip = go.AddComponent<TooltipTrigger>();
            tip.titleProvider = () => TileTitle(id);
            tip.bodyProvider = () => TileBody(id);
            g.list.Add(t);
        }
    }

    Sprite TileIcon(int id)
    {
        if (id == PistolRow) return Resources.Load<Sprite>("Weapons/weapon_" + (CharacterData.Current.held ?? "pistol"));
        return abilities[id].icon;
    }

    // 창 크기 · 위치: 무기 → 스킬 → 패시브 순서로 아래에서 위로 쌓음
    void LayoutGroups()
    {
        float y = 100f;
        foreach (HudGroup g in new[] { weaponGroup, skillGroup, passiveGroup })
        {
            if (g == null) continue;
            bool any = g.list.Count > 0;
            g.root.gameObject.SetActive(any);
            if (!any) continue;
            float tilesH = g.collapsed ? 0f : TileSize;
            g.tiles.gameObject.SetActive(!g.collapsed);
            g.tiles.anchoredPosition = Vector2.zero;
            Transform head = g.header.transform.parent;
            ((RectTransform)head).anchoredPosition = new Vector2(0f, tilesH + (g.collapsed ? 0f : 4f));
            g.header.text = (g.collapsed ? "+ " : "- ") + g.title + (g.collapsed ? "  (" + g.list.Count + ")" : "");
            g.root.anchoredPosition = new Vector2(24f, y);
            y += tilesH + (g.collapsed ? 0f : 4f) + HeaderHeight + GroupGap;
        }
    }

    // ================================================================= 매 프레임 상태
    void UpdateHud()
    {
        if (weaponGroup == null) return;
        foreach (HudGroup g in new[] { weaponGroup, skillGroup, passiveGroup })
        {
            if (g.collapsed) continue;
            foreach (HudTile t in g.list) UpdateTile(t);
        }
    }

    void UpdateTile(HudTile t)
    {
        bool active = false;
        float cool = 0f;
        string label = "", key = "";
        int id = t.id;

        if (id == PistolRow || abilities[id].kind == SpecialKind.Weapon)
        {
            active = id == PistolRow ? !WeaponActive : CurrentWeapon == id;
            WeaponStatus(id, out label, out cool);
        }
        else if (abilities[id].kind == SpecialKind.Skill)
        {
            int slot = skills.IndexOf(id);
            key = slot >= 0 && slot < SkillKeys.Length ? KeyBindings.Name(SkillKeys[slot]) : "";
            float left = CooldownUntil(id) - Time.time;
            float length = cooldownLength.TryGetValue(id, out float l) ? l : 1f;
            cool = left > 0f ? Mathf.Clamp01(left / length) : 0f;
            label = left > 0f ? left.ToString(left < 10f ? "0.0" : "0") : "";
            if (id == SoulBurstId) label = souls + "/" + SoulsNeeded;
            active = left <= 0f;
        }
        else
        {
            label = PassiveState(id, false);
        }

        bool flash = rowFlashUntil.TryGetValue(id, out float until) && Time.time < until;
        t.border.color = flash ? Color.white : active ? TileActive : TileBorder;
        t.cooldown.fillAmount = cool;
        t.label.text = label;
        t.key.text = key + (id >= 0 && IsEvolved(id) ? (key.Length > 0 ? " " : "") + "+" : "");
    }

    // 무기 칸: 짧은 상태 (탄창 · 장전 · 열기)와 가려질 비율
    void WeaponStatus(int id, out string label, out float cool)
    {
        cool = 0f;
        if (id == PistolRow)
        {
            CharacterKit kit = CharacterKit.Instance;
            if (kit != null && !kit.UsesAmmo) { label = ""; return; }
            if (player.reload > 0f)
            {
                cool = 1f - Mathf.Clamp01(player.reload / Mathf.Max(0.01f, player.reloadTime));
                label = Loc.T("장전");
                return;
            }
            label = player.NowBullet + "/" + player.MaxBullet;
            return;
        }
        if (id == FlameId) { label = overheated ? Loc.T("과열") : Mathf.RoundToInt(heat * 100f) + "%"; cool = heat; return; }
        if (id == ScytheId) { label = activeScythe == null ? "" : Loc.T("회수"); cool = activeScythe == null ? 0f : 1f; return; }
        WeaponAmmo a = Ammo(id);
        if (MagSize(id) <= 0) { label = ""; return; }
        if (a.Reloading)
        {
            cool = Mathf.Clamp01((a.reloadEnd - Time.time) / BaseReload(id));
            label = Loc.T("장전");
            return;
        }
        label = a.ammo + "/" + MagSize(id);
    }

    // 패시브 칸의 상태 (거너 능력 + 캐릭터 능력)
    string PassiveState(int id, bool detail)
    {
        if (id == UndyingId)
        {
            int left = UndyingMaxUses - undyingUses;
            return detail ? (left > 0 ? Loc.T("부활 ") + left + Loc.T("번 남음") : Loc.T("이번 판에 다 씀")) : "x" + left;
        }
        if (id == OrbsId) return detail ? Loc.T("수호 영혼 ") + orbs.Count : "x" + orbs.Count;
        return IsKit(id) ? KitPassiveState(id, detail) : "";
    }

    // ================================================================= 툴팁
    string TileTitle(int id)
    {
        if (id == PistolRow) return Loc.T(CharacterData.IsGunner ? "기본 권총" : CharacterData.Current.weapon) + "  · " + Loc.T("기본 무기");
        SpecialDef def = abilities[id];
        string kind = def.kind == SpecialKind.Weapon ? Loc.T("무기") : def.kind == SpecialKind.Skill ? Loc.T("스킬") : Loc.T("패시브");
        return Loc.T(def.name) + (IsEvolved(id) ? Loc.T(" (진화)") : "") + "  · " + kind;
    }

    string TileBody(int id)
    {
        string state;
        string body;
        if (id == PistolRow)
        {
            body = Loc.T(CharacterData.Current.attack);
            WeaponStatus(id, out string l, out float c);
            state = (!WeaponActive ? Loc.T("사용 중") : Loc.T("[{SWAP}]로 꺼내기")) + (l.Length > 0 ? "  · " + l : "");
        }
        else
        {
            SpecialDef def = abilities[id];
            body = AbilityText(id, def.description);
            if (def.kind == SpecialKind.Weapon)
            {
                WeaponStatus(id, out string l, out float c);
                state = (CurrentWeapon == id ? Loc.T("사용 중") : Loc.T("[{SWAP}]로 꺼내기")) + (l.Length > 0 ? "  · " + l : "");
            }
            else if (def.kind == SpecialKind.Skill)
            {
                float left = CooldownUntil(id) - Time.time;
                state = left > 0f ? Loc.T("쿨타임 ") + left.ToString("0.0") + Loc.T("초") : Loc.T("사용 가능");
                if (id == SoulBurstId) state += Loc.T(" · 영혼 ") + souls;
            }
            else state = PassiveState(id, true);
        }
        return body + (state.Length > 0 ? "\n\n<color=#F5D478>" + state + "</color>" : "");
    }
}

// 무한 모드가 아닐 때 화면 위 가운데에 플레이 시간 (00:00, 멈춘 시간은 빼고)
public class RunClock : MonoBehaviour
{
    TextMeshProUGUI text;
    float start;

    public static void Create()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        GameObject go = new GameObject("RunClock", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(300f, 40f);
        r.anchoredPosition = new Vector2(0f, -30f);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) t.font = UIKit.Font;
        if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
        t.fontSize = 28f;
        t.color = new Color(1f, 0.9f, 0.7f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        RunClock c = go.AddComponent<RunClock>();
        c.text = t;
        c.start = Time.time;
    }

    void Update()
    {
        if (text != null) text.text = EndlessMode.Clock(Time.time - start);
    }
}

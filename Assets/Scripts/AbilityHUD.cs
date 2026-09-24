using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 레벨업으로 얻은 능력을 아이콘 + Lv 로 보여주는 HUD
public class AbilityHUD : MonoBehaviour
{
    [Header("능력 아이콘 (LevelShop 능력 순서와 같게)")]
    public Sprite[] icons = new Sprite[10];

    [Header("칸 모양")]
    public Sprite slotSprite;
    public TMP_FontAsset font;
    public Material fontMaterial;
    public Vector2 slotSize = new Vector2(76f, 76f);
    public float spacing = 8f;
    public int columns = 6;

    class Slot
    {
        public RectTransform rect;
        public TextMeshProUGUI label;
        public int level;
    }

    LevelShop levelShop;

    readonly Dictionary<int, Slot> slots = new Dictionary<int, Slot>();

    public Sprite GetIcon(int id)
    {
        if (icons == null || id < 0 || id >= icons.Length) return null;
        return icons[id];
    }

    public void SetAbility(int id, int level)
    {
        if (!slots.TryGetValue(id, out Slot slot))
        {
            slot = CreateSlot(id, slots.Count);
            slots.Add(id, slot);
        }

        slot.level = level;
        slot.label.text = "Lv." + level;

        // 레벨이 오른 칸을 잠깐 키웠다가 되돌림
        slot.rect.localScale = Vector3.one * 1.25f;
    }

    void Update()
    {
        foreach (Slot slot in slots.Values)
        {
            slot.rect.localScale = Vector3.MoveTowards(slot.rect.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
        }
    }

    string AbilityName(int id)
    {
        if (levelShop == null) levelShop = FindFirstObjectByType<LevelShop>();
        if (levelShop == null || id >= levelShop.ability_name.Length) return "";
        return levelShop.ability_name[id];
    }

    string AbilityDescription(int id)
    {
        if (levelShop == null) levelShop = FindFirstObjectByType<LevelShop>();
        if (levelShop == null || id >= levelShop.ability_content.Length) return "";
        return levelShop.ability_content[id];
    }

    Slot CreateSlot(int id, int index)
    {
        int col = index % columns;
        int row = index / columns;

        GameObject slotGo = new GameObject("Ability_" + id, typeof(RectTransform), typeof(Image));
        RectTransform rect = slotGo.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = slotSize;
        rect.anchoredPosition = new Vector2(
            slotSize.x * 0.5f + col * (slotSize.x + spacing),
            -slotSize.y * 0.5f - row * (slotSize.y + spacing));

        Image bg = slotGo.GetComponent<Image>();
        bg.sprite = slotSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = true;   // 마우스를 올리면 설명 표시

        TooltipTrigger tip = slotGo.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => AbilityName(id) + "  Lv." + (slots.TryGetValue(id, out Slot s) ? s.level : 1);
        tip.bodyProvider = () => AbilityDescription(id);

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(10f, 14f);
        iconRect.offsetMax = new Vector2(-10f, -6f);
        Image icon = iconGo.GetComponent<Image>();
        icon.sprite = GetIcon(id);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject labelGo = new GameObject("Level", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 30f);
        labelRect.anchoredPosition = new Vector2(0f, -8f);
        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        if (fontMaterial != null) label.fontSharedMaterial = fontMaterial;
        label.fontSize = 22f;
        label.color = new Color(0.96f, 0.83f, 0.47f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;

        return new Slot { rect = rect, label = label };
    }
}

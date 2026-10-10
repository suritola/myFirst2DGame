using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 여러 장 중 하나를 고르는 창 (2.2.2): 보물 상자 · 보스 보상 3택 · 저주 제단
// 열린 동안 게임이 멈추고, 카드를 누르거나 숫자 1 · 2 · 3 으로 고름 (ESC 메뉴 · 보스 결계는 열리지 않음)
public class ChoiceUI : MonoBehaviour
{
    public struct Option
    {
        public string name, desc;       // 한국어 원문 (번역은 UIKit.Text)
        public Color color;
        public Option(string name, string desc, Color color) { this.name = name; this.desc = desc; this.color = color; }
    }

    public static bool Open { get; private set; }

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.93f, 0.9f, 0.84f);
    int picked = -1;

    public static IEnumerator Run(string title, string sub, Option[] options, System.Action<int> onPick)
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null || options == null || options.Length == 0) { onPick?.Invoke(0); yield break; }
        while (Open) yield return null;                 // 다른 고르기 창이 떠 있으면 닫힐 때까지
        UIKit.EnsureStyle();
        float before = Time.timeScale;
        Time.timeScale = 0f;
        Open = true;
        TooltipUI.Hide();

        GameObject go = new GameObject("Choice", typeof(RectTransform));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.rootCanvas.transform, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.SetAsLastSibling();
        SoulTreeUI.OnTop(go, 620);
        ChoiceUI ui = go.AddComponent<ChoiceUI>();

        RectTransform back = UIKit.Rect("Back", r, Vector2.zero, Vector2.zero);
        back.anchorMin = Vector2.zero; back.anchorMax = Vector2.one;
        back.offsetMin = back.offsetMax = Vector2.zero;
        back.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.01f, 0.04f, 0.85f);

        UIKit.Text(r, title, 54f, Gold, new Vector2(0f, 330f), new Vector2(1400f, 80f));
        if (!string.IsNullOrEmpty(sub)) UIKit.Text(r, sub, 28f, Parch, new Vector2(0f, 262f), new Vector2(1400f, 50f));

        const float w = 400f, gap = 40f;
        float x0 = -(options.Length - 1) * (w + gap) * 0.5f;
        for (int i = 0; i < options.Length; i++)
        {
            int idx = i;
            Option o = options[i];
            RectTransform card = UIKit.Rect("Card", r, new Vector2(x0 + i * (w + gap), -30f), new Vector2(w, 440f));
            Image ci = card.gameObject.AddComponent<Image>();
            ci.sprite = UIKit.ButtonSprite;
            ci.type = Image.Type.Sliced;
            ci.color = Color.Lerp(new Color(0.16f, 0.13f, 0.2f), o.color, 0.3f);
            Button b = card.gameObject.AddComponent<Button>();
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1.15f, 1.1f, 0.9f);
            cb.pressedColor = new Color(0.8f, 0.75f, 0.7f);
            b.colors = cb;
            b.onClick.AddListener(() => ui.picked = idx);
            UIKit.Text(card, o.name, 40f, o.color, new Vector2(0f, 140f), new Vector2(w - 40f, 70f));
            UIKit.Text(card, o.desc, 27f, Parch, new Vector2(0f, -20f), new Vector2(w - 50f, 230f));
            TMP_Text key = UIKit.Text(card, "", 24f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -185f), new Vector2(w, 40f));
            key.text = "[" + (i + 1) + "]";
        }
        Hostile.Play("chime", 0.7f, 1.1f);

        while (ui.picked < 0)
        {
            for (int i = 0; i < options.Length && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) ui.picked = i;
            yield return null;
        }
        int pick = ui.picked;
        Destroy(go);
        Open = false;
        Time.timeScale = before > 0f ? before : 1f;
        Hostile.Play("pulse", 0.6f, 1.2f);
        onPick?.Invoke(pick);
    }
}

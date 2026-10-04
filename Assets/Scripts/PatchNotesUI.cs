using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임을 켤 때마다 메인 메뉴에 이번 버전의 패치노트를 띄움
// 내용은 Resources/PatchNotes.txt (docs/patch-notes/v<버전>.md 를 그대로 복사 · 빌드 스크립트가 빌드할 때 덮어씀)
public static class PatchNotesUI
{
    static GameObject open;
    // 이번 실행에서 이미 띄웠는지 (게임을 하고 메인 메뉴로 돌아왔을 때는 다시 안 띄움)
    static bool shown;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);

    public static void OpenOnLaunch(Transform root)
    {
        if (shown || GameInput.Auto) return;
        shown = true;
        Open(root);
    }

    public static void Open(Transform root)
    {
        if (open != null) return;
        TextAsset src = Resources.Load<TextAsset>("PatchNotes");
        if (src == null || string.IsNullOrWhiteSpace(src.text)) return;

        RectTransform win = UIKit.Modal(root, "PatchNotes", new Vector2(1240f, 900f), out open);
        TMP_Text title = UIKit.Text(win, "", 46f, Gold, new Vector2(0f, 395f), new Vector2(900f, 64f));
        title.text = Loc.T("패치노트") + "  <size=70%><color=#A89C86>v" + Application.version + "</color></size>";
        UIKit.MakeButton(win, "닫기", new Vector2(510f, 395f), new Vector2(150f, 58f), Close, 22f);

        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -40f), new Vector2(1160f, 780f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect("Content", view, Vector2.zero, new Vector2(1110f, 0f));
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, -16f);

        TMP_Text body = UIKit.Text(content, "", 25f, Parch, Vector2.zero, new Vector2(1110f, 0f), TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = false;
        body.fontSize = 25f;
        body.enableWordWrapping = true;
        body.lineSpacing = 8f;
        RectTransform br = body.rectTransform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 1f);
        br.anchoredPosition = Vector2.zero;
        body.text = Format(src.text);
        content.gameObject.AddComponent<VerticalLayoutGroup>().childControlHeight = true;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        open.AddComponent<EscCloser>();
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }

    // 마크다운 몇 가지만 TMP 서식으로: # 제목 · ## 소제목 · - 목록 · **굵게** (글꼴에 없는 이모지는 뺌)
    static string Format(string md)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string raw in md.Replace("\r", "").Split('\n'))
        {
            string line = Regex.Replace(raw, @"[\uD800-\uDFFF️‍]", "").TrimEnd();
            line = Regex.Replace(line, @"\*\*(.+?)\*\*", "<color=#F5D478>$1</color>");
            string t = line.TrimStart();
            if (t.StartsWith("## ")) sb.Append("\n<size=120%><color=#C9A0FF>").Append(t.Substring(3).Trim()).Append("</color></size>\n");
            else if (t.StartsWith("# ")) sb.Append("<size=125%><color=#F5D478>").Append(t.Substring(2).Trim()).Append("</color></size>\n");
            else if (t.StartsWith("- ")) sb.Append("<indent=2%>•</indent><indent=5%>").Append(t.Substring(2)).Append("</indent>\n");
            else if (t.Length > 0 && line.StartsWith(" ")) sb.Append("<indent=5%><color=#A89C86>").Append(t).Append("</color></indent>\n");
            else if (t.Length > 0) sb.Append(t).Append('\n');
        }
        return sb.ToString().Trim();
    }

    // ESC 로도 닫힘
    class EscCloser : MonoBehaviour
    {
        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            SettingsUI.EscHandledFrame = Time.frameCount;
            Close();
        }
    }
}

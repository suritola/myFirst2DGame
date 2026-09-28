using TMPro;
using UnityEngine;

// 메인 화면 오른쪽 위 보유 포인트 표시 (1.8.5~) — 스킨을 사면 바로 줄어든 값으로
public class MenuPoints : MonoBehaviour
{
    public TMP_Text text;
    int shown = -1;

    void Update()
    {
        int p = CharacterData.Points;
        if (p == shown || text == null) return;
        shown = p;
        text.text = p.ToString("N0") + " P";
    }
}

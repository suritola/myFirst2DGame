using TMPro;
using UnityEngine;

public class Point : MonoBehaviour
{
    public int points = 0;

    public TextMeshProUGUI TextInput;

    void Start()
    {
        UpdatePointText();
    }

    public void AddPoint(int point)
    {
        points += point;
        // 캐릭터 구매에 쓰는 포인트로도 쌓임 (게임을 꺼도 유지)
        CharacterData.AddPoints(point);

        UpdatePointText();

        Debug.Log("현재 점수: " + points);
    }

    void UpdatePointText()
    {
        if (TextInput != null) TextInput.text = "POINT : " + points;
    }
}
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

        UpdatePointText();

        Debug.Log("현재 점수: " + points);
    }

    void UpdatePointText()
    {
        if (TextInput != null) TextInput.text = "POINT : " + points;
    }
}
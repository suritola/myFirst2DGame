using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Level : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject EXPgauge;
    public float EXPmaxWidth;
    public float playerEXP;
    public float playerMaxEXP;
    public int playerLevel;
    public float bonusEXP = 1.0f;

    public TextMeshProUGUI levelText;
    void Start()
    {
        EXPmaxWidth = GetComponent<RectTransform>().sizeDelta.x;
        playerEXP = FindFirstObjectByType<PlayerController>().nowEXP;
        playerMaxEXP = FindFirstObjectByType<PlayerController>().needEXP;
        playerLevel = FindFirstObjectByType<PlayerController>().level;
    }

    // Update is called once per frame
    void Update()
    {

        playerLevel = FindFirstObjectByType<PlayerController>().level;
        levelText.text = "Lv. " + playerLevel;
        playerEXP = FindFirstObjectByType<PlayerController>().nowEXP;
        playerMaxEXP = FindFirstObjectByType<PlayerController>().needEXP;
        if (playerEXP > 0) EXPgauge.GetComponent<RectTransform>().sizeDelta = new Vector2(playerEXP / playerMaxEXP * EXPmaxWidth, EXPgauge.GetComponent<RectTransform>().sizeDelta.y);
        else EXPgauge.GetComponent<RectTransform>().sizeDelta = new Vector2(0, EXPgauge.GetComponent<RectTransform>().sizeDelta.y);
    }
}

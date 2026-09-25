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
    RectTransform gauge;
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

        PlayerController player = Cache<PlayerController>.Get;
        playerLevel = player.level;
        levelText.text = "Lv. " + playerLevel;
        playerEXP = player.nowEXP;
        playerMaxEXP = player.needEXP;
        if (gauge == null) gauge = EXPgauge.GetComponent<RectTransform>();
        gauge.sizeDelta = new Vector2(playerEXP > 0 ? playerEXP / playerMaxEXP * EXPmaxWidth : 0, gauge.sizeDelta.y);
    }
}

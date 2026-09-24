using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHP : MonoBehaviour
{

    public GameObject HPgauge;
    // 체력바 위에 표시되는 "현재 / 최대" 텍스트
    public TextMeshProUGUI hpText;
    public float HPmaxWidth;
    public float playerHP;
    public float playerMaxHP;

    // Start is called before the first frame update
    void Start()
    {
        HPmaxWidth = GetComponent<RectTransform>().sizeDelta.x;
        playerHP = FindFirstObjectByType<PlayerController>().PlayerHealth;
        playerMaxHP = FindFirstObjectByType<PlayerController>().PlayerMaxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        playerHP = FindFirstObjectByType<PlayerController>().PlayerHealth;
        playerMaxHP = FindFirstObjectByType<PlayerController>().PlayerMaxHealth;
        if (playerHP > 0) HPgauge.GetComponent<RectTransform>().sizeDelta = new Vector2(playerHP / playerMaxHP * HPmaxWidth, HPgauge.GetComponent<RectTransform>().sizeDelta.y);
        else HPgauge.GetComponent<RectTransform>().sizeDelta = new Vector2(0, HPgauge.GetComponent<RectTransform>().sizeDelta.y);

        if (hpText != null) hpText.text = Mathf.CeilToInt(Mathf.Max(playerHP, 0)) + " / " + Mathf.CeilToInt(playerMaxHP);
    }
}

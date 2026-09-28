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
    RectTransform gauge;
    int shownHp = -1, shownMax = -1;

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
        PlayerController player = Cache<PlayerController>.Get;
        playerHP = player.PlayerHealth;
        playerMaxHP = player.PlayerMaxHealth;
        if (gauge == null) gauge = HPgauge.GetComponent<RectTransform>();
        gauge.sizeDelta = new Vector2(playerHP > 0 ? playerHP / playerMaxHP * HPmaxWidth : 0, gauge.sizeDelta.y);

        // 숫자가 바뀔 때만 글자를 새로 (매 프레임 문자열을 만들지 않게)
        int hp = Mathf.CeilToInt(Mathf.Max(playerHP, 0)), max = Mathf.CeilToInt(playerMaxHP);
        if (hpText != null && (hp != shownHp || max != shownMax))
        {
            shownHp = hp;
            shownMax = max;
            hpText.text = hp + " / " + max;
        }
    }
}

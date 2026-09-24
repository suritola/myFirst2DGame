using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ESCmenu : MonoBehaviour
{
    public GameObject escMenu;
    public GameObject shopPanel;
    private bool isEscOpen = false;
    private bool isShopOpen = false;
    // 열기 전 시간 배율 (레벨업 창 위에서 열었다 닫아도 그대로 멈춰 있도록)
    private float timeScaleBeforeOpen = 1f;
    // Start is called before the first frame update
    void Start()
    {
        shop = FindFirstObjectByType<Shop>();
    }

    // Update is called once per frame
    Shop shop;
    void Update()
    {
        isShopOpen = shop.isShopOpen;
        // 타겟팅 스킬(시간이 느려진 상태) 중에는 열지 않음
        PlayerController player = FindFirstObjectByType<PlayerController>();
        bool skillUsing = player != null && player.IsSkillUsing;
        if (Input.GetKeyDown(KeyCode.Escape) && (!skillUsing || isEscOpen))
        {
            if (isShopOpen)
            {
                shop.isShopOpen = false;
                StartCoroutine(shop.StartGameCountdown());
                shopPanel.SetActive(false);
            }
            else ToggleEsc();
        }
    }

    public void onPressRestart()
    {
        SceneManager.LoadScene("GameOver");
        SceneManager.LoadScene("GameScene");
        Time.timeScale = 1f;
    }
    public void onPressMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }

    void ToggleEsc()
    {
        isEscOpen = !isEscOpen;

        if (escMenu != null) escMenu.SetActive(isEscOpen);

        if (isEscOpen)
        {
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
        }
        else Time.timeScale = timeScaleBeforeOpen;
    }
}

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
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 설정 창이 열려 있으면 설정 창이 ESC를 처리함 (키 입력 취소 · 창 닫기)
        if (SettingsUI.IsOpen || SettingsUI.EscHandledFrame == Time.frameCount) return;
        if (isShopOpen)
        {
            shop.isShopOpen = false;
            StartCoroutine(shop.StartGameCountdown());
            shopPanel.SetActive(false);
        }
        else ToggleEsc();
    }

    // 창이 포커스를 잃으면 (Alt+Tab 등) 일시정지 메뉴를 열어 둠
    void OnApplicationFocus(bool focus)
    {
        if (focus || isEscOpen || GameInput.Auto || Application.isBatchMode) return;
        if (Time.timeScale == 0f || (shop != null && shop.isShopOpen)) return;
        ToggleEsc();
    }

    public void onPressRestart()
    {
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
            // 필살기 조준 중(시간이 느려진 상태)이면 조준을 취소하고 멈춤 (게이지는 그대로)
            PlayerController player = Hostile.Player;
            if (player != null) player.CancelSkill();
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
        }
        else Time.timeScale = timeScaleBeforeOpen;
    }
}

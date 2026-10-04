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

        // 설정 창이 열려 있으면 설정 창이 ESC를 처리함 (키 입력 취소 · 창 닫기), 연출 중에는 ESC가 건너뛰기
        if (SettingsUI.IsOpen || SettingsUI.EscHandledFrame == Time.frameCount || StoryDirector.Playing) return;
        // 영혼 트리는 ESC로 닫히고, 무기 진화는 고를 때까지 멈춤
        if (SoulTreeUI.IsOpen || WeaponEvolutionUI.Open) return;
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
        if (focus || isEscOpen || GameInput.Auto || Application.isBatchMode || StoryDirector.Playing) return;
        if (Time.timeScale == 0f || (shop != null && shop.isShopOpen)) return;
        ToggleEsc();
    }

    void OnDestroy() => IsOpen = false;

    // 일시정지 메뉴 첫 버튼: 다시 시작 대신 계속하기 (ESC 를 다시 누른 것과 같음)
    public void onPressResume()
    {
        if (isEscOpen) ToggleEsc();
    }
    public void onPressMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }

    public static bool IsOpen { get; private set; }
    // 레벨업 카드 창이 일시정지 창을 가려 버튼을 못 누르던 문제: 일시정지 동안 카드 창을 숨겼다가 닫으면 다시 띄움
    LevelShop hiddenLevelShop;

    void ToggleEsc()
    {
        isEscOpen = !isEscOpen;
        IsOpen = isEscOpen;

        if (isEscOpen)
        {
            LevelShop ls = FindFirstObjectByType<LevelShop>();
            if (ls != null && ls.IsOpen)
            {
                ls.LvshopPanel.SetActive(false);
                hiddenLevelShop = ls;
            }
        }
        else if (hiddenLevelShop != null)
        {
            hiddenLevelShop.LvshopPanel.SetActive(true);
            hiddenLevelShop = null;
        }

        if (escMenu != null) escMenu.SetActive(isEscOpen);
        RunStats.ShowSummary(isEscOpen);

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

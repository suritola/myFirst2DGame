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
        if (Input.GetKeyDown(KeyCode.Escape))
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

        if (isEscOpen) Time.timeScale = 0f;
        else Time.timeScale = 1f;
    }
}

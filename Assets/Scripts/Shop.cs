
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public GameObject shopPanel;

    [Header("공격력 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput;
    public TextMeshProUGUI statTextInput;

    [Header("공속 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput2;
    public TextMeshProUGUI statTextInput2;

    [Header("재장전속도 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput3;
    public TextMeshProUGUI statTextInput3;

    [Header("탄창 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput4;
    public TextMeshProUGUI statTextInput4;

    [Header("이동속도 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput5;
    public TextMeshProUGUI statTextInput5;

    public TextMeshProUGUI mycoins;

    [Header("코인")]
    public int coins;

    [Header("스텟")]
    public int damage = 1;
    public float ShootSpeed = 0.8f;
    public float ReloadSpeed = 3f;
    public int MaxBullet = 6;
    public float showedSpeed;
    public float moveSpeed;

    [Header("가격")]
    public int damagePrice = 5;
    public int ShootSpeedPrice = 5;
    public int ReloadSpeedPrice = 5;
    public int MaxBulletPrice = 5;
    public int moveSpeedPrice = 5;

    public bool isShopOpen = false;

    public Coin coind;
    public PlayerController playerControllerd;

    [Header("게임 재시작")]
    public GameObject pause;
    public TextMeshProUGUI pauseText;


    void Start()
    {
        Debug.Log("Shop 시작");

        coind = FindFirstObjectByType<Coin>();
        playerControllerd = FindFirstObjectByType<PlayerController>();

        if (shopPanel != null) shopPanel.SetActive(false);
        else Debug.LogError("shopPanel이 연결되지 않았습니다!");

        if (pause != null) pause.SetActive(false);

        UpdateShopText();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) ToggleShop();
    }


    // =====================================
    // 공격력 업그레이드
    // =====================================

    public void OnPressB1()
    {
        if (coind == null || playerControllerd == null) return;

        coins = coind.coins;

        if (coins < damagePrice) return;

        damage++;

        playerControllerd.damage = damage;

        coind.SubCoin(damagePrice);

        coins = coind.coins;

        damagePrice = Mathf.CeilToInt(damagePrice * 2.5213f);

        UpdateShopText();
    }


    // =====================================
    // 공속 업그레이드
    // =====================================

    public void OnPressB2()
    {
        if (coind == null || playerControllerd == null) return;

        if (showedSpeed >= 5) return;

        coins = coind.coins;

        if (coins < ShootSpeedPrice) return;

        ShootSpeed -= 0.15f;

        playerControllerd.ShootSpeed = ShootSpeed;

        coind.SubCoin(ShootSpeedPrice);

        coins = coind.coins;

        ShootSpeedPrice = (int)((1 - ShootSpeed) * 31.32f);

        UpdateShopText();
    }


    // =====================================
    // 재장전속도 업그레이드
    // =====================================

    public void OnPressB3()
    {
        if (coind == null || playerControllerd == null) return;

        if (ReloadSpeed <= 1.5f) return;

        coins = coind.coins;

        if (coins < ReloadSpeedPrice) return;

        ReloadSpeed -= 0.3f;

        playerControllerd.reloadTime = ReloadSpeed;

        coind.SubCoin(ReloadSpeedPrice);

        coins = coind.coins;

        ReloadSpeedPrice = (int)(ReloadSpeedPrice * 1.6f);

        UpdateShopText();
    }


    // =====================================
    // 탄창 업그레이드
    // =====================================

    public void OnPressB4()
    {
        if (coind == null || playerControllerd == null) return;

        coins = coind.coins;

        if (coins < MaxBulletPrice) return;

        MaxBullet++;

        playerControllerd.MaxBullet = MaxBullet;

        coind.SubCoin(MaxBulletPrice);

        coins = coind.coins;

        MaxBulletPrice = (int)(MaxBulletPrice * 1.85f);

        UpdateShopText();
    }

    public void OnPressB5()
    {
        if (coind == null || playerControllerd == null) return;

        coins = coind.coins;

        if (coins < moveSpeedPrice) return;

        playerControllerd.speed *= 1.05f;

        coind.SubCoin(moveSpeedPrice);

        coins = coind.coins;

        moveSpeedPrice = (int)(moveSpeedPrice * 2.3f);

        UpdateShopText();
    }


    // =====================================
    // 상점 열기 / 닫기
    // =====================================

    public bool isPause = false;
    void ToggleShop()
    {
        if (isShopOpen)
        {
            // 상점 닫기
            isShopOpen = false;

            if (shopPanel != null) shopPanel.SetActive(false);

            StartCoroutine(StartGameCountdown());
        }
        else
        {
            // 상점 열기
            if (isPause) return;

            isShopOpen = true;

            if (coind != null) coins = coind.coins;

            if (shopPanel != null) shopPanel.SetActive(true);

            UpdateShopText();

            Time.timeScale = 0f;
        }
    }


    // =====================================
    // 게임 재시작 카운트다운
    // =====================================

    public IEnumerator StartGameCountdown()
    {
        if (pause != null)
        {
            pause.SetActive(true);
            isPause = true;
        }


        Time.timeScale = 0f;

        for (int countdown = 1; countdown > 0; countdown--)
        {
            if (pauseText != null) pauseText.text = "일시정지 " + countdown + "초";

            // Time.timeScale = 0이어도 시간이 흐름
            yield return new WaitForSecondsRealtime(1f);
        }

        if (pause != null)
        {
            pause.SetActive(false);
            isPause = false;
        }


        Time.timeScale = 1f;
    }


    // =====================================
    // 상점 텍스트 갱신
    // =====================================

    void UpdateShopText()
    {
        if (coind == null) return;

        coins = coind.coins;
        moveSpeed = playerControllerd.speed;
        if (mycoins != null) mycoins.text = "코인 : " + coins;

        // =====================================
        // 이동속도
        // =====================================

        if (priceTextInput5 != null) priceTextInput5.text = "구매\n" + moveSpeedPrice + " 코인";

        if (statTextInput5 != null)
        {
            float tmp = moveSpeed * 1.05f;
            statTextInput5.text = "이동속도 \n" + moveSpeed + " -> " + tmp;
        }

        // =====================================
        // 공격력
        // =====================================

        if (priceTextInput != null) priceTextInput.text = "구매\n" + damagePrice + " 코인";

        if (statTextInput != null)
        {
            int tmp = damage + 1;
            statTextInput.text = "총알 공격력 \n" + damage + " -> " + tmp;
        }


        // =====================================
        // 공격속도
        // =====================================

        showedSpeed =
            Mathf.Round((1f / ShootSpeed) * 100f) / 100f;

        if (showedSpeed < 5)
        {
            if (priceTextInput2 != null) priceTextInput2.text = "구매\n" + ShootSpeedPrice + " 코인";

            if (statTextInput2 != null)
            {
                float tmp2 = ShootSpeed - 0.15f;

                float currentSpeed = Mathf.Round((1f / ShootSpeed) * 100f) / 100f;

                float nextSpeed = Mathf.Round((1f / tmp2) * 100f) / 100f;

                statTextInput2.text = "공격 속도\n" + currentSpeed + " -> " + nextSpeed;
            }
        }
        else
        {
            if (priceTextInput2 != null) priceTextInput2.text = "최대";

            if (statTextInput2 != null) statTextInput2.text = "공격 속도\n5 (최대)";
        }


        // =====================================
        // 재장전속도
        // =====================================

        if (ReloadSpeed > 1.5f)
        {
            if (priceTextInput3 != null) priceTextInput3.text = "구매\n" + ReloadSpeedPrice + " 코인";

            if (statTextInput3 != null)
            {
                float tmp3 = ReloadSpeed - 0.3f;

                statTextInput3.text = "재장전 속도\n" + ReloadSpeed.ToString("0.0") + "초 -> " + tmp3.ToString("0.0") + "초";
            }
        }
        else
        {
            if (priceTextInput3 != null) priceTextInput3.text = "최대";

            if (statTextInput3 != null) statTextInput3.text = "재장전 속도\n" + ReloadSpeed.ToString("0.0") + "초 (최대)";
        }


        // =====================================
        // 탄창
        // =====================================

        if (priceTextInput4 != null) priceTextInput4.text = "구매\n" + MaxBulletPrice + " 코인";

        if (statTextInput4 != null)
        {
            int tmp4 = MaxBullet + 1;

            statTextInput4.text = "최대 탄창\n" + MaxBullet + " -> " + tmp4;
        }
    }
}

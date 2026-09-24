
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public float damage = 1f;
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

        // 상점이 가진 능력치를 플레이어의 시작 값과 맞춤
        if (playerControllerd != null)
        {
            damage = playerControllerd.damage;
            ShootSpeed = playerControllerd.ShootSpeed;
            ReloadSpeed = playerControllerd.reloadTime;
            MaxBullet = playerControllerd.MaxBullet;
        }

        if (shopPanel != null) shopPanel.SetActive(false);
        else Debug.LogError("shopPanel이 연결되지 않았습니다!");

        if (pause != null) pause.SetActive(false);

        UpdateShopText();
    }

    // ================================================================= 떠돌이 상점 제단
    [Header("상점 제단")]
    public int killsPerStall = 35;          // 일반 몹을 이만큼 잡을 때마다 제단이 나타남
    int killsForStall;

    void OnEnable() => EnermyController.Killed += OnEnemyKilled;
    void OnDisable() => EnermyController.Killed -= OnEnemyKilled;

    void OnEnemyKilled(Vector3 pos)
    {
        killsForStall++;
        if (killsForStall < killsPerStall || playerControllerd == null) return;
        killsForStall = 0;

        // 플레이어 근처, 벽이 아닌 곳
        Vector3 at = playerControllerd.transform.position;
        for (int i = 0; i < 12; i++)
        {
            Vector3 p = Hostile.ClampArena(playerControllerd.transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(5f, 8f)));
            if (!Hostile.WallNear(p, 1.5f)) { at = p; break; }
        }
        ShopStall.Spawn(this, at);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner("떠돌이 상점이 나타났다!\n10초 안에 다가가 [Space]", 2.2f);
    }

    // 제단에 다가갔을 때 (열렸으면 true)
    public bool OpenFromStall()
    {
        if (isShopOpen || isPause || Time.timeScale != 1f) return false;
        ToggleShop();
        return isShopOpen;
    }



    void Update()
    {
        // 특수 능력 화면이 열려 있으면 상점을 열지 않음
        bool specialOpen = StageManager.Instance != null && StageManager.Instance.IsMenuOpen;
        // 상점은 떠돌이 제단으로만 열림 (P는 닫기만)
        if (Input.GetKeyDown(KeyCode.P) && isShopOpen && !specialOpen) ToggleShop();
    }


    [Header("가격 증가 / 최대치")]
    // 한 번에 확 강해지지 않도록 작게 자주 오르는 방식
    public float damageStep = 0.25f;            // 살 때마다 공격력 +0.25 (기본 1의 25%)
    public float damagePriceGrowth = 1.3f;      // 살 때마다 가격 x1.3
    public int shootSpeedPriceStep = 5;         // 살 때마다 가격 +5
    public float shootSpeedMultiplier = 0.94f;  // 발사 간격 x0.94 (6% 빨라짐)
    public float minShootSpeed = 0.22f;
    public float reloadPriceGrowth = 1.45f;
    public float reloadStep = 0.12f;
    public float minReloadTime = 0.9f;
    public float maxBulletPriceGrowth = 1.5f;
    public int maxBulletLimit = 14;
    public float moveSpeedPriceGrowth = 1.6f;
    public float moveSpeedMultiplier = 1.04f;
    public int maxMoveSpeedBuys = 5;

    int moveSpeedBuys = 0;

    bool TryPay(int price)
    {
        if (coind == null || playerControllerd == null) return false;
        if (coind.coins < price) return false;

        coind.SubCoin(price);
        coins = coind.coins;
        return true;
    }

    bool ShootSpeedMaxed => ShootSpeed <= minShootSpeed + 0.001f;
    bool ReloadMaxed => ReloadSpeed <= minReloadTime + 0.001f;
    bool MaxBulletMaxed => MaxBullet >= maxBulletLimit;
    bool MoveSpeedMaxed => moveSpeedBuys >= maxMoveSpeedBuys;

    // =====================================
    // 공격력 업그레이드
    // =====================================

    public void OnPressB1()
    {
        if (!TryPay(damagePrice)) return;

        damage += damageStep;
        playerControllerd.damage = damage;
        damagePrice = Mathf.CeilToInt(damagePrice * damagePriceGrowth);
        UpdateShopText();
    }

    // =====================================
    // 공속 업그레이드
    // =====================================

    public void OnPressB2()
    {
        if (ShootSpeedMaxed || !TryPay(ShootSpeedPrice)) return;

        ShootSpeed = Mathf.Max(minShootSpeed, ShootSpeed * shootSpeedMultiplier);
        playerControllerd.ShootSpeed = ShootSpeed;
        ShootSpeedPrice += shootSpeedPriceStep;
        UpdateShopText();
    }

    // =====================================
    // 재장전 속도 업그레이드
    // =====================================

    public void OnPressB3()
    {
        if (ReloadMaxed || !TryPay(ReloadSpeedPrice)) return;

        ReloadSpeed = Mathf.Max(minReloadTime, ReloadSpeed - reloadStep);
        playerControllerd.reloadTime = ReloadSpeed;
        ReloadSpeedPrice = Mathf.CeilToInt(ReloadSpeedPrice * reloadPriceGrowth);
        UpdateShopText();
    }

    // =====================================
    // 탄창 업그레이드
    // =====================================

    public void OnPressB4()
    {
        if (MaxBulletMaxed || !TryPay(MaxBulletPrice)) return;

        MaxBullet++;
        playerControllerd.MaxBullet = MaxBullet;
        MaxBulletPrice = Mathf.CeilToInt(MaxBulletPrice * maxBulletPriceGrowth);
        UpdateShopText();
    }

    // =====================================
    // 이동속도 업그레이드
    // =====================================

    public void OnPressB5()
    {
        if (MoveSpeedMaxed || !TryPay(moveSpeedPrice)) return;

        moveSpeedBuys++;
        playerControllerd.speed *= moveSpeedMultiplier;
        moveSpeedPrice = Mathf.CeilToInt(moveSpeedPrice * moveSpeedPriceGrowth);
        UpdateShopText();
    }

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
            // 상점 열기 (다른 메뉴나 타겟팅 스킬 중에는 열지 않음)
            if (isPause || Time.timeScale != 1f) return;

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

    string PriceText(int price, bool maxed) => maxed ? "최대" : "구매\n" + price + " 코인";

    static string PerSecond(float interval) => (1f / interval).ToString("0.0");

    public void UpdateShopText()
    {
        if (coind == null || playerControllerd == null) return;

        coins = coind.coins;
        moveSpeed = playerControllerd.speed;
        showedSpeed = 1f / ShootSpeed;
        if (mycoins != null) mycoins.text = "코인 : " + coins;


        // 공격력
        if (priceTextInput != null) priceTextInput.text = PriceText(damagePrice, false);
        if (statTextInput != null)
        {
            statTextInput.text = "총알 공격력\n" + damage.ToString("0.##") + " -> " + (damage + damageStep).ToString("0.##");

            // 멀티샷 중이면 실제 한 발당 피해도 함께 표시
            int shots = playerControllerd.multiShot;
            if (shots > 1)
            {
                float rate = playerControllerd.MultiShotDamageRate(shots);
                statTextInput.text += "  (" + shots + "발, 발당 " + (damage * rate).ToString("0.##")
                    + " -> " + ((damage + damageStep) * rate).ToString("0.##") + ")";
            }
        }

        // 공격 속도 (초당 발사 수)
        if (priceTextInput2 != null) priceTextInput2.text = PriceText(ShootSpeedPrice, ShootSpeedMaxed);
        if (statTextInput2 != null)
        {
            statTextInput2.text = ShootSpeedMaxed
                ? "공격 속도\n초당 " + PerSecond(ShootSpeed) + "발 (최대)"
                : "공격 속도 (초당)\n" + PerSecond(ShootSpeed) + " -> " + PerSecond(Mathf.Max(minShootSpeed, ShootSpeed * shootSpeedMultiplier)) + "발";
        }

        // 재장전 속도
        if (priceTextInput3 != null) priceTextInput3.text = PriceText(ReloadSpeedPrice, ReloadMaxed);
        if (statTextInput3 != null)
        {
            statTextInput3.text = ReloadMaxed
                ? "재장전 속도\n" + ReloadSpeed.ToString("0.0") + "초 (최대)"
                : "재장전 속도\n" + ReloadSpeed.ToString("0.0") + "초 -> " + Mathf.Max(minReloadTime, ReloadSpeed - reloadStep).ToString("0.0") + "초";
        }

        // 탄창
        if (priceTextInput4 != null) priceTextInput4.text = PriceText(MaxBulletPrice, MaxBulletMaxed);
        if (statTextInput4 != null)
        {
            statTextInput4.text = MaxBulletMaxed
                ? "최대 탄창\n" + MaxBullet + "발 (최대)"
                : "최대 탄창\n" + MaxBullet + " -> " + (MaxBullet + 1) + "발";
        }

        // 이동 속도
        if (priceTextInput5 != null) priceTextInput5.text = PriceText(moveSpeedPrice, MoveSpeedMaxed);
        if (statTextInput5 != null)
        {
            statTextInput5.text = MoveSpeedMaxed
                ? "이동 속도\n" + moveSpeed.ToString("0.0") + " (최대)"
                : "이동 속도\n" + moveSpeed.ToString("0.0") + " -> " + (moveSpeed * moveSpeedMultiplier).ToString("0.0");
        }
    }
}

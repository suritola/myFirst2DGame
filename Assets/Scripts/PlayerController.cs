
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // =====================================
    // 사운드
    // =====================================

    [Header("사운드")]

    public AudioClip shotSound;
    public AudioClip hitSound;
    public AudioClip chargeSound;
    public AudioClip reloadSound;
    public AudioClip getCoin;

    // =====================================
    // 레벨 설정
    // =====================================

    [Header("레벨 설정")]

    public int level = 1;
    public float nowEXP = 0f;
    public float needEXP;

    // =====================================
    // 기본 설정
    // =====================================

    [Header("기본 설정")]

    public float speed = 3f;

    public GameObject bulletPrefab;

    public int isShop = 0;
    public int bonusCoin = 0;

    [Header("일반 발사 설정")]

    public float ShootSpeed = 0.8f;

    public int damage = 1;

    public int pene = 1;

    public bool blood;

    public int getHP = 0;

    public int multiShot = 1;

    // 멀티샷 발사 수별 한 발당 피해 비율 (1발 100%, 2발 70%, 3발 55% ...)
    // 전부 맞히면 총 피해는 100% / 140% / 165% / 180% / 200%
    public float[] multiShotDamageRates = { 1f, 0.7f, 0.55f, 0.45f, 0.4f };

    public float MultiShotDamageRate(int shots)
    {
        if (multiShotDamageRates == null || multiShotDamageRates.Length == 0) return 1f;
        return multiShotDamageRates[Mathf.Clamp(shots - 1, 0, multiShotDamageRates.Length - 1)];
    }

    public float knockBack = 0.3f;

    // =====================================
    // 탄약 / 재장전
    // =====================================

    [Header("탄약 / 재장전")]

    public int MaxBullet = 6;
    public int NowBullet = 6;

    public float reload = 0f;

    public float reloadTime = 3f;

    private bool isReloading = false;

    // =====================================
    // 체력
    // =====================================

    [Header("체력")]

    public float PlayerMaxHealth = 100f;
    public float PlayerHealth = 100f;

    // =====================================
    // 스킬 설정
    // =====================================

    [Header("스킬 설정")]

    public float targetRange = 8f;

    public float targetInterval = 0.2f;

    public float shootDelay = 0.08f;

    public int ShotsPerTarget = 1;

    public float skillDamage = 500f;

    public float TimeSet = 0.3f;

    public float normalZoom = 15f;

    public float maxZoom = 30f;

    public float MaxCharge = 240f;

    public float NowCharge = 0f;

    public float Skill_setTime = 0.5f;

    // =====================================
    // 화면 효과
    // =====================================

    [Header("화면 효과")]

    public Image skillEffectPanel;

    public float screenFadeAlpha = 0.6f;

    public float screenFadeSpeed = 5f;

    // =====================================
    // 타겟 마크
    // =====================================

    [Header("타겟 마크")]

    public GameObject targetMarkPrefab;

    // =====================================
    // 스킬 게이지
    // =====================================

    [Header("스킬 게이지")]

    public SkillGauge skillGauge;

    // =====================================
    // 내부 변수
    // =====================================

    private bool isSkillUsing = false;

    private float nextTargetTime = 0f;

    private float nextShootTime = 0f;

    private List<EnermyController> targets =
        new List<EnermyController>();

    private List<GameObject> targetMarks =
        new List<GameObject>();

    private Vector3 move;

    private Animator animator;

    private SpriteRenderer spriteRenderer;

    private AudioSource audioSource;

    private Camera mainCamera;

    // 사격 후 잠깐 동안 쏜 방향을 바라봄
    public float faceShotTime = 0.35f;

    private float faceLockUntil = 0f;

    // =====================================
    // 시작
    // =====================================

    void Start()
    {
        needEXP = 100f;
        isShop = 0;

        NowBullet = MaxBullet;
        PlayerHealth = PlayerMaxHealth;
        NowCharge = 0f;

        animator = GetComponent<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();

        mainCamera = Camera.main;

        if (mainCamera != null) mainCamera.orthographicSize = normalZoom;

        if (skillGauge == null) skillGauge = FindFirstObjectByType<SkillGauge>();

        if (skillEffectPanel != null)
        {
            UnityEngine.Color color = skillEffectPanel.color;
            color.a = 0f;
            skillEffectPanel.color = color;
        }
    }



    public void addDamage(int a)
    {
        damage += a;
    }

    // =====================================
    // Update
    // =====================================

    void Update()
    {

        // 레벨이 오를수록 조금씩 더 필요 (100, 150, 200 ...)
        needEXP = 50 + level * 50;

        // =========================
        // 재장전 입력
        // =========================

        if (Input.GetKeyDown(KeyCode.R)) if (NowBullet < MaxBullet) StartCoroutine(Reload());

        // =========================
        // 이동 입력
        // =========================

        move = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) move += Vector3.left;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) move += Vector3.right;

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) move += Vector3.up;

        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) move += Vector3.down;

        move = move.normalized;

        // =========================
        // 플레이어 방향
        // =========================

        if (spriteRenderer != null && Time.time >= faceLockUntil)
        {
            if (move.x < 0 && !spriteRenderer.flipX) spriteRenderer.flipX = true;

            if (move.x > 0 && spriteRenderer.flipX) spriteRenderer.flipX = false;
        }

        // =========================
        // 좌클릭 일반 발사
        // =========================

        if (Input.GetMouseButtonDown(0) && !isSkillUsing && !isReloading && Time.time >= nextShootTime && !PointerOverUI()) Shoot();

        // =========================
        // 우클릭 스킬 시작
        // =========================

        if (Input.GetMouseButtonDown(1) && !isSkillUsing && !isReloading)
        {
            if (skillGauge != null && skillGauge.IsFull()) StartSkill();
        }

        // =========================
        // 우클릭 유지 중
        // =========================

        if (Input.GetMouseButton(1) && isSkillUsing)
        {
            NowCharge += Time.unscaledDeltaTime * 100f;

            // 충전할수록 강해짐: 공격력 x2 (즉시) ~ x8 (최대 충전)
            skillDamage = damage * (2f + 6f * Mathf.Clamp01(NowCharge / MaxCharge));

            if (NowCharge >= MaxCharge)
            {
                NowCharge = MaxCharge;
                EndSkill();
            }

            if (mainCamera != null && mainCamera.orthographicSize < maxZoom) mainCamera.orthographicSize += 0.05f * Time.unscaledDeltaTime * 60f;

            if (Time.unscaledTime >= nextTargetTime)
            {
                FindNextTarget();
                nextTargetTime = Time.unscaledTime + targetInterval;
            }
        }

        // =========================
        // 우클릭 해제
        // =========================

        if (Input.GetMouseButtonUp(1) && isSkillUsing) EndSkill();

        // =========================
        // 자동 재장전
        // =========================

        if (NowBullet <= 0 && !isReloading) StartCoroutine(Reload());

        // =========================
        // 애니메이션
        // =========================

        if (!isSkillUsing && animator != null)
        {
            if (move.magnitude > 0) animator.SetTrigger("Move");
            else animator.SetTrigger("Stop");
        }
    }

    // =====================================
    // 이동
    // =====================================

    void FixedUpdate()
    {
        if (!isSkillUsing) transform.Translate(move * speed * Time.fixedDeltaTime);
    }

    // =====================================
    // 재장전
    // =====================================

    IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        reload = 0f;

        if (audioSource != null && reloadSound != null) audioSource.PlayOneShot(reloadSound);

        while (reload < reloadTime)
        {
            reload += Time.unscaledDeltaTime;
            yield return null;
        }

        NowBullet = MaxBullet;

        reload = 0f;

        isReloading = false;
    }

    // =====================================
    // 일반 총알 발사
    // =====================================


void Shoot()
    {
        if (NowBullet <= 0 || isReloading) return;

        nextShootTime = Time.time + ShootSpeed;
        NowBullet--;

        if (animator != null) animator.SetTrigger("Shoot");

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        FaceTowards(mousePosition);

        if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);

        Vector3 startPosition = transform.position + new Vector3(spriteRenderer != null && spriteRenderer.flipX ? -0.5f : 0.5f, -0.5f, 0);

        Vector2 direction = (mousePosition - startPosition).normalized;

        // 멀티샷 퍼지는 각도
        float spreadAngle = 10f;

        float bulletDamage = damage * MultiShotDamageRate(multiShot);

        float shotRate = MultiShotDamageRate(multiShot);

        if (multiShot == 1) CreateBullet(startPosition, direction, bulletDamage, pene, 0, false);
        else
        {
            int shotCount = multiShot;

            float startAngle = -spreadAngle * (shotCount - 1) / 2f;

            for (int i = 0; i < shotCount; i++)
            {
                float angle = startAngle + spreadAngle * i;

                Vector2 shotDirection = Quaternion.Euler(0, 0, angle) * direction;

                CreateBullet(startPosition, shotDirection, bulletDamage, pene, 0, false, shotRate);
            }
        }
    }



    // =====================================
    // 총알 생성
    // =====================================

    // 마우스가 버튼 같은 UI 위에 있으면 사격하지 않음
    static bool PointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    // 잠깐 동안 목표 방향을 바라보게 함 (이동 입력보다 우선)
    void FaceTowards(Vector3 worldPos)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = worldPos.x < transform.position.x;
        faceLockUntil = Time.time + faceShotTime;
    }

    void CreateBullet(Vector3 startPosition, Vector2 direction, float damage, int penes, int blood, bool isSkill, float knockBackRate = 1f)
    {
        if (bulletPrefab == null) return;

        GameObject newBullet = Instantiate(bulletPrefab, startPosition, Quaternion.identity);

        Bullet bullet = newBullet.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Dir = direction;
            bullet.pene = penes;
            bullet.blood = blood;
            bullet.damage = damage;
            bullet.isSkill = isSkill;
            bullet.knockBack = knockBack * knockBackRate;
            bullet.skillCharge = knockBackRate;
        }

    }

    // =====================================
    // 스킬 시작
    // =====================================

    void StartSkill()
    {
        if (isSkillUsing) return;
        if (skillGauge == null || !skillGauge.IsFull()) return;
        isSkillUsing = true;
        NowCharge = 0f;
        skillDamage = damage * 2f;
        if (audioSource != null && chargeSound != null) audioSource.PlayOneShot(chargeSound);

        ClearTargets();

        nextTargetTime = Time.unscaledTime;

        StartCoroutine(FadeScreen(screenFadeAlpha));

        Time.timeScale = Skill_setTime;
    }

    // =====================================
    // 한 마리씩 타겟팅
    // =====================================

    void FindNextTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position,targetRange);

        EnermyController closestEnemy = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider2D enemyCollider in enemies)
        {
            if (!enemyCollider.CompareTag("enermy")) continue;

            EnermyController enemy = enemyCollider.GetComponent<EnermyController>();

            if (enemy == null || targets.Contains(enemy)) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy == null) return;

        targets.Add(closestEnemy);

        if (targetMarkPrefab != null)
        {
            GameObject mark = Instantiate(targetMarkPrefab, closestEnemy.transform.position, Quaternion.identity);
            TargetMark targetMark = mark.GetComponent<TargetMark>();
            if (targetMark != null) targetMark.target = closestEnemy.transform;
            targetMarks.Add(mark);
            if (NowBullet < MaxBullet) NowBullet++;
        }
    }

    // =====================================
    // 스킬 종료
    // =====================================

    void EndSkill()
    {
        if (!isSkillUsing) return;

        isSkillUsing = false;

        if (audioSource != null) audioSource.Stop();

        Time.timeScale = 1f;

        StartCoroutine(FadeScreen(0f));

        StartCoroutine(ResetZoom());

        foreach (GameObject mark in targetMarks) if (mark != null) Destroy(mark);

        targetMarks.Clear();

        StartCoroutine(ShootTargets());

        if (skillGauge != null) skillGauge.ResetSkillPoint();

        NowCharge = 0f;
    }

    // =====================================
    // 타겟들에게 순서대로 발사
    // =====================================

    IEnumerator ShootTargets()
    {
        foreach (EnermyController target in targets)
        {
            if (target == null) continue;

            Vector3 startPosition = transform.position + new Vector3(0, -0.5f, 0);

            Vector2 direction = (target.transform.position - startPosition).normalized;

            for (int i = 0; i < ShotsPerTarget;  i++)
            {
                if (target == null) break;

                FaceTowards(target.transform.position);

                if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);

                CreateBullet(startPosition, direction, skillDamage, pene, getHP, true);

                yield return new WaitForSeconds(shootDelay);
            }
        }

        targets.Clear();
    }

    // =====================================
    // 화면 페이드
    // =====================================

    IEnumerator FadeScreen(
        float targetAlpha
    )
    {
        if (skillEffectPanel == null) yield break;

        UnityEngine.Color color = skillEffectPanel.color;

        while (!Mathf.Approximately(color.a, targetAlpha))
        {
            color.a = Mathf.MoveTowards(color.a, targetAlpha, screenFadeSpeed * Time.unscaledDeltaTime);

            skillEffectPanel.color = color;

            yield return null;
        }
    }

    // =====================================
    // 카메라 줌 원상복구
    // =====================================

    IEnumerator ResetZoom()
    {
        if (mainCamera == null) yield break;

        while (mainCamera.orthographicSize > normalZoom)
        {
            mainCamera.orthographicSize = Mathf.MoveTowards( mainCamera.orthographicSize, normalZoom, 10f * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    // =====================================
    // 플레이어 피격
    // =====================================
    public float def = 0f;

    int dropCoin;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("coin"))
        {
            Coin coin = FindFirstObjectByType<Coin>();

            if (coin != null)
            {
                coin.AddCoin(1 + bonusCoin);
                if ( audioSource != null && getCoin != null ) audioSource.PlayOneShot( getCoin );
            }
            Destroy( collision.gameObject );
        }

        HandleContact(collision);
    }

    // 적과 닿아 있는 동안에도 무적 시간 간격으로 계속 피해를 받음
    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleContact(collision);
    }

    // 일반 적은 닿으면 스스로 피해를 주고 자폭함 (EnermyController)
    // 보스는 사라지지 않으므로 여기서 처리
    void HandleContact(Collider2D collision)
    {
        if (collision.CompareTag("boss")) TryHit(bossContactDamage);
    }

    [Header("피격")]
    public float bossContactDamage = 25f;
    // 맞은 뒤 이 시간 동안은 다시 맞지 않음 (여러 마리에게 동시에 맞는 것 방지)
    public float hurtInvincibleTime = 0.8f;

    private float invincibleUntil = 0f;

    public bool IsInvincible => Time.time < invincibleUntil;

    // 피해를 받았으면 true, 무적이라 무시됐으면 false
    public bool TryHit(float amount)
    {
        if (IsInvincible) return false;

        invincibleUntil = Time.time + hurtInvincibleTime;

        if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);

        PlayerHealth -= amount * (1f - def);

        if (PlayerHealth <= 0)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameOver");
        }

        return true;
    }

    // 무적 시간 동안 깜빡임
    void LateUpdate()
    {
        if (spriteRenderer == null) return;

        bool blinking = Time.time < invincibleUntil && !isSkillUsing;
        UnityEngine.Color c = spriteRenderer.color;
        c.a = blinking && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 1f;
        spriteRenderer.color = c;
    }

    // =====================================
    // 타겟 제거
    // =====================================

    void ClearTargets()
    {
        targets.Clear();

        foreach (GameObject mark in targetMarks) if (mark != null) Destroy(mark);

        targetMarks.Clear();
    }

    // =====================================
    // 타겟 범위 표시
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere( transform.position, targetRange );
    }
}



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

    public float damage = 1f;

    public int pene = 1;

    public bool blood;

    public float getHP = 0f;

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

    // 타겟팅 스킬로 조준할 수 있는 기본 적 수 (스킬 강화 상점에서 늘어남)
    public int maxTargets = 6;
    Color baseEffectColor;
    bool effectColorSaved;

    // 코인 자석 (레벨업 능력): 이 거리 안의 코인을 끌어옴
    [HideInInspector] public float coinMagnetRange = 0f;

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

    // 특수 능력 (지옥 입장 시 선택)
    [HideInInspector] public SpecialAbilities special;
    // 희생의 계약 같은 일시 강화
    [HideInInspector] public float damageMultiplier = 1f;
    [HideInInspector] public float fireRateMultiplier = 1f;
    // 분신 사용 중 반투명 등
    [HideInInspector] public float bodyAlpha = 1f;
    // 탄약 표시 대신 보여줄 글자 (과열, 충전 등)
    [HideInInspector] public string ammoTextOverride;

    public bool CanShoot => !IsSkillUsing && !isReloading && NowBullet > 0;
    // 총알 한 발이 채우는 스킬 게이지 비율
    public const float GaugeRate = 0.35f;
    // 메뉴로 게임이 멈췄는지 (타겟팅 스킬의 느린 시간은 멈춘 것이 아님)
    public static bool IsPaused => Time.timeScale == 0f;
    // 타겟팅 스킬 사용 중 + 끝난 뒤 자동 연사 중 (이때는 직접 쏠 수 없음)
    public bool IsSkillUsing => isSkillUsing || isVolleying;
    private bool isVolleying;
    public Camera MainCamera => mainCamera;

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
        EnermyController.Killed += HealOnKill;

        needEXP = 100f;
        isShop = 0;

        NowBullet = MaxBullet;
        PlayerHealth = PlayerMaxHealth;
        NowCharge = 0f;

        animator = GetComponent<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();

        // 들고 있는 무기 모습 · 발사 연출
        PlayerLook.Attach(this);
        // 거너가 아닌 캐릭터: 스탯 · 몸 그림 · 평타 · 우클릭 스킬 (CharacterData)
        CharacterKit.Attach(this);

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

        // 생명의 샘: 초당 체력 회복
        if (regenPerSecond > 0f && PlayerHealth > 0f && PlayerHealth < PlayerMaxHealth)
            PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + regenPerSecond * Time.deltaTime);

        // 코인 자석: 주변 코인이 날아옴
        if (coinMagnetRange > 0f && Time.timeScale > 0f)
        {
            foreach (GameObject c in GameObject.FindGameObjectsWithTag("coin"))
            {
                float d = Vector2.Distance(c.transform.position, transform.position);
                if (d < coinMagnetRange) c.transform.position = Vector3.MoveTowards(c.transform.position, transform.position, (10f + (coinMagnetRange - d) * 3f) * Time.deltaTime);
            }
        }

        // 상점 · ESC · 레벨업 등으로 멈춘 동안에는 입력을 받지 않음
        // (멈춘 화면에서 클릭하면 총이 나가거나 스킬이 시간을 다시 흐르게 하던 문제)
        // 시작 · 엔딩 연출 중에도 조작하지 않음
        if (IsPaused || StoryDirector.Playing)
        {
            move = Vector3.zero;
            return;
        }

        // =========================
        // 재장전 입력
        // =========================

        bool holdingSpecial = special != null && special.WeaponActive;
        if (KeyBindings.Down(GameAction.Reload) && !holdingSpecial && (CharacterKit.Instance == null || CharacterKit.Instance.UsesAmmo)) if (NowBullet < MaxBullet) StartCoroutine(Reload());

        // =========================
        // 이동 입력
        // =========================

        move = Vector3.zero;

        if (KeyBindings.Held(GameAction.Left)) move += Vector3.left;

        if (KeyBindings.Held(GameAction.Right)) move += Vector3.right;

        if (KeyBindings.Held(GameAction.Up)) move += Vector3.up;

        if (KeyBindings.Held(GameAction.Down)) move += Vector3.down;

        if (GameInput.Auto) move = GameInput.AutoMove;

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

        bool specialWeapon = special != null && special.WeaponActive;
        CharacterKit kit = CharacterKit.Instance;
        if (kit != null)
        {
            // 다른 캐릭터: 누르고 있으면 공격 속도에 맞춰 계속 (탄창이 있으면 한 발씩 씀)
            bool ammo = kit.UsesAmmo;
            if (kit.DrawsBow)
            {
                // 궁수: 좌클릭을 누르고 있으면 시위를 당기고, 떼면 발사 (오래 당길수록 강하고 빠른 화살)
                // 공격 속도가 빠를수록 빨리 가득 당김
                kit.UpdateBow(!specialWeapon && !kit.Busy && Time.time >= nextShootTime && !PointerOverUI(),
                              ShootSpeed * 2f / (fireRateMultiplier * kit.AttackSpeedMul));
                if (kit.BowFired) nextShootTime = Time.time + 0.12f;
            }
            else if (!specialWeapon && GameInput.FireHeld && !kit.Busy && Time.time >= nextShootTime && !PointerOverUI()
                && (!ammo || (NowBullet > 0 && !isReloading && !IsSkillUsing)))
            {
                kit.Attack();
                if (ammo) NowBullet--;
                nextShootTime = Time.time + ShootSpeed / (fireRateMultiplier * kit.AttackSpeedMul);
            }
            if (!specialWeapon) ammoTextOverride = ammo ? null : kit.WeaponName;
        }
        else if (!specialWeapon && GameInput.FireDown && !IsSkillUsing && !isReloading && Time.time >= nextShootTime && !PointerOverUI()) Shoot();

        // =========================
        // 우클릭 스킬 시작
        // =========================

        if (kit != null) kit.UpdateUlt(skillGauge);
        else
        {
            if (GameInput.UltDown && !IsSkillUsing && !isReloading)
            {
                if (skillGauge != null && skillGauge.IsFull())
                {
                    // 조준이 필요 없는 필살기는 누르자마자 발동 (줌 · 감속 없음)
                    if (special != null && special.IsInstantUlt) StartCoroutine(InstantUlt());
                    else StartSkill();
                }
            }

            // =========================
            // 우클릭 유지 중
            // =========================

            if (GameInput.UltHeld && isSkillUsing)
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
                special?.UpdateAim(targets, Mathf.Clamp01(NowCharge / MaxCharge));
            }

            // =========================
            // 우클릭 해제
            // =========================

            if (GameInput.UltUp && isSkillUsing) EndSkill();
        }

        // =========================
        // 자동 재장전
        // =========================

        if (NowBullet <= 0 && !isReloading && (CharacterKit.Instance == null || CharacterKit.Instance.UsesAmmo)) StartCoroutine(Reload());

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
        float slowMul = Time.time < slowUntil ? slowFactor : 1f;
        float kitMul = CharacterKit.Instance != null ? CharacterKit.Instance.MoveMul : 1f;
        if (!isSkillUsing) transform.Translate(move * speed * slowMul * kitMul * Time.fixedDeltaTime);
    }

    // =====================================
    // 재장전
    // =====================================

    IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        reload = 0f;

        if (audioSource != null && reloadSound != null) audioSource.PlayOneShot(reloadSound, GameSettings.SfxVolume);

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

        nextShootTime = Time.time + ShootSpeed / fireRateMultiplier;
        // 탄창 저주: 마지막 한 발 강화
        bool cursed = special != null && special.IsLastBulletCursed(NowBullet);
        NowBullet--;

        if (animator != null) animator.SetTrigger("Shoot");

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(GameInput.MousePosition);
        mousePosition.z = 0;

        FaceTowards(mousePosition);
        PlayerLook.Fired(-1);

        if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound, GameSettings.SfxVolume);

        Vector3 startPosition = MuzzlePosition;

        Vector2 direction = (mousePosition - startPosition).normalized;

        // 멀티샷 퍼지는 각도
        float spreadAngle = 10f;

        float bulletDamage = damage * damageMultiplier * MultiShotDamageRate(multiShot) * (cursed ? 3f : 1f);

        float shotRate = MultiShotDamageRate(multiShot);

        if (multiShot == 1) special?.CurseBullet(CreateBullet(startPosition, direction, bulletDamage, pene, 0, false), cursed);
        else
        {
            int shotCount = multiShot;

            float startAngle = -spreadAngle * (shotCount - 1) / 2f;

            for (int i = 0; i < shotCount; i++)
            {
                float angle = startAngle + spreadAngle * i;

                Vector2 shotDirection = Quaternion.Euler(0, 0, angle) * direction;

                special?.CurseBullet(CreateBullet(startPosition, shotDirection, bulletDamage, pene, 0, false, shotRate), cursed);
            }
        }
    }



    // =====================================
    // 총알 생성
    // =====================================

    // 마우스가 버튼 같은 UI 위에 있으면 사격하지 않음
    static bool PointerOverUI()
    {
        return !GameInput.Auto && UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    // 잠깐 동안 목표 방향을 바라보게 함 (이동 입력보다 우선)
    public void FaceTowards(Vector3 worldPos)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = worldPos.x < transform.position.x;
        faceLockUntil = Time.time + faceShotTime;
    }

    public Bullet CreateBullet(Vector3 startPosition, Vector2 direction, float damage, int penes, float blood, bool isSkill, float knockBackRate = 1f)
    {
        if (bulletPrefab == null) return null;

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
            newBullet.AddComponent<BulletGlow>();
            // 스킬 게이지는 화염 방사기 기준으로 천천히 참
            bullet.skillCharge = knockBackRate * GaugeRate;
        }

        return bullet;
    }

    // 총구 위치 (바라보는 쪽 손)
    public Vector3 MuzzlePosition => PlayerLook.Instance != null && PlayerLook.Instance.HasTip ? PlayerLook.Instance.TipPosition : BaseMuzzle;
    // 손에 든 무기 그림이 없을 때의 총구 (캐릭터 그림 기준)
    public Vector3 BaseMuzzle => transform.position + new Vector3(spriteRenderer != null && spriteRenderer.flipX ? -0.5f : 0.5f, -0.5f, 0);

    // 꽃가루 구름 등으로 잠깐 느려짐
    float slowUntil;
    float slowFactor = 1f;

    public void Slow(float factor, float seconds)
    {
        slowFactor = factor;
        slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
    }

    public void GrantInvincibility(float seconds)
    {
        invincibleUntil = Mathf.Max(invincibleUntil, Time.time + seconds);
    }

    // =====================================
    // 스킬 시작
    // =====================================

    void StartSkill()
    {
        if (isSkillUsing) return;
        if (skillGauge == null || !skillGauge.IsFull()) return;
        isSkillUsing = true;
        UltUsed?.Invoke();
        NowCharge = 0f;
        skillDamage = damage * 2f;
        if (audioSource != null && chargeSound != null) audioSource.PlayOneShot(chargeSound, GameSettings.SfxVolume);

        ClearTargets();

        nextTargetTime = Time.unscaledTime;

        // 화면 색은 들고 있는 무기 색, 조준 연출도 무기마다 다름
        if (skillEffectPanel != null)
        {
            if (!effectColorSaved) { baseEffectColor = skillEffectPanel.color; effectColorSaved = true; }
            Color tint = special != null && special.WeaponActive ? Color.Lerp(baseEffectColor, special.AimTint, 0.6f) : baseEffectColor;
            tint.a = skillEffectPanel.color.a;
            skillEffectPanel.color = tint;
        }
        special?.BeginAim();

        StartCoroutine(FadeScreen(screenFadeAlpha));

        Time.timeScale = Skill_setTime;
    }

    // =====================================
    // 한 마리씩 타겟팅
    // =====================================

    void FindNextTarget()
    {
        // 한 번에 조준할 수 있는 적 수 (스킬 강화로 늘어남)
        int limit = special != null ? special.MaxTargets(maxTargets) : maxTargets;
        if (targets.Count >= limit) return;

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

        // 특수 무기를 들고 있으면 무기다운 표식 (범위형 무기는 표식 없음)
        if (special != null && special.WeaponActive)
        {
            GameObject weaponMark = special.MarkTarget(closestEnemy.transform);
            if (weaponMark != null) targetMarks.Add(weaponMark);
        }
        else if (targetMarkPrefab != null)
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
        special?.EndAim();

        StartCoroutine(ShootTargets());

        if (skillGauge != null) skillGauge.ResetSkillPoint();

        NowCharge = 0f;
    }

    // 조준을 쏘지 않고 취소 (일시정지할 때): 시간 · 화면 · 줌을 되돌리고 게이지는 그대로 둠
    public void CancelSkill()
    {
        if (!isSkillUsing) return;

        isSkillUsing = false;

        if (audioSource != null) audioSource.Stop();

        Time.timeScale = 1f;

        StartCoroutine(FadeScreen(0f));

        StartCoroutine(ResetZoom());

        foreach (GameObject mark in targetMarks) if (mark != null) Destroy(mark);

        targetMarks.Clear();
        targets.Clear();
        special?.EndAim();

        NowCharge = 0f;
    }

    // =====================================
    // 타겟들에게 순서대로 발사
    // =====================================

    // 필살기를 썼을 때 (업적 등)
    public static event System.Action UltUsed;
    // 다른 캐릭터의 우클릭 스킬도 필살기로 셈 (업적 등)
    public void RaiseUltUsed() => UltUsed?.Invoke();

    IEnumerator InstantUlt()
    {
        UltUsed?.Invoke();
        isVolleying = true;
        skillGauge.ResetSkillPoint();
        FaceTowards(mainCamera.ScreenToWorldPoint(GameInput.MousePosition));
        yield return StartCoroutine(special.WeaponVolley(new List<EnermyController>(), damage * 5f, 0f));
        isVolleying = false;
    }

    IEnumerator ShootTargets()
    {
        isVolleying = true;

        // 특수 무기를 들고 있으면 그 무기다운 일제 사격
        if (special != null && special.WeaponActive)
        {
            yield return StartCoroutine(special.WeaponVolley(targets, skillDamage, getHP));
            targets.Clear();
            isVolleying = false;
            yield break;
        }

        foreach (EnermyController target in targets)
        {
            if (target == null) continue;

            Vector3 startPosition = transform.position + new Vector3(0, -0.5f, 0);

            Vector2 direction = (target.transform.position - startPosition).normalized;

            for (int i = 0; i < ShotsPerTarget;  i++)
            {
                if (target == null) break;

                FaceTowards(target.transform.position);

                if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound, GameSettings.SfxVolume);

                CreateBullet(startPosition, direction, skillDamage * (special != null ? special.UltPower(SpecialAbilities.PistolUlt) : 1f), pene, getHP, true);

                yield return new WaitForSeconds(shootDelay);
            }
        }

        targets.Clear();
        isVolleying = false;
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

    [Header("회복")]
    // 초당 체력 회복 (생명의 샘)
    public float regenPerSecond = 0f;
    // 적 처치 시 체력 회복 (피의 굶주림)
    public float healOnKill = 0f;

    void HealOnKill(Vector3 pos)
    {
        if (healOnKill > 0f && PlayerHealth > 0f) PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + healOnKill);
    }

    void OnDestroy()
    {
        EnermyController.Killed -= HealOnKill;
    }

    int dropCoin;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("coin"))
        {
            Coin coin = FindFirstObjectByType<Coin>();

            if (coin != null)
            {
                coin.AddCoin(1 + bonusCoin);
                if ( audioSource != null && getCoin != null ) audioSource.PlayOneShot(getCoin, GameSettings.SfxVolume);
                Juice.CoinPicked(collision.transform.position);
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
        if (collision.CompareTag("boss"))
        {
            bosss boss = collision.GetComponent<bosss>();
            TryHit(boss != null ? boss.contactDamage : bossContactDamage);
        }
    }

    [Header("피격")]
    public float bossContactDamage = 25f;
    // 맞은 뒤 이 시간 동안은 다시 맞지 않음 (여러 마리에게 동시에 맞는 것 방지)
    public float hurtInvincibleTime = 0.8f;

    private float invincibleUntil = 0f;

    public bool IsInvincible => Time.time < invincibleUntil;

    // 피격 연출: 화면 가장자리 붉은 번쩍임, 흔들림, 머리 위 피해 숫자
    void ShowHurt(float taken)
    {
        DamageFlash.Show(0.45f + taken / Mathf.Max(1f, PlayerMaxHealth) * 2.5f);
        if (SpecialAbilities.SharedFx != null)
        {
            SpecialAbilities.SharedFx.Shake(0.2f + Mathf.Min(0.3f, taken / 60f), 0.15f);
            SpecialAbilities.SharedFx.FloatText(transform.position, "-" + Mathf.CeilToInt(taken), new Color(1f, 0.3f, 0.28f), 5f, 0f);
        }
    }

    // 피해를 받았으면 true, 무적이라 무시됐으면 false
    public bool TryHit(float amount)
    {
        if (IsInvincible) return false;

        invincibleUntil = Time.time + hurtInvincibleTime;

        if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound, GameSettings.SfxVolume);

        float taken = amount * GameMode.DamageMul * (1f - def);

        // 불사의 맹세: 죽을 피해를 한 번 버팀
        if (PlayerHealth - taken <= 0 && special != null && special.TryUndying())
        {
            PlayerHealth = Mathf.Max(1f, special.UndyingReviveHealth(PlayerMaxHealth));
            invincibleUntil = Time.time + 3f;
            DamageFlash.Show(1f);
            return true;
        }

        PlayerHealth -= taken;
        ShowHurt(taken);
        special?.OnPlayerHurt();

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
        c.a = (blinking && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 1f) * bodyAlpha;
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


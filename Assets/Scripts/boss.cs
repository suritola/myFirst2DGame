using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bosss : MonoBehaviour
{
    public float speed = 0.2f;
    public int setEnemyHP = 300;
    public float EnemyHealth;


    private Transform player;
    private Vector3 move;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public int KilledEnemy;

    public GameObject coin;

    public int hitCount = 0;

    [Header("부하 소환")]
    // 이만큼 맞을 때마다 부하를 추가로 소환
    public int hitsPerSummon = 15;
    public int summonOnHitCount = 2;
    // 일정 시간마다 소환 (체력이 절반 아래면 더 자주, 더 많이)
    public float summonInterval = 5f;
    public int summonCount = 3;
    public float enragedSummonInterval = 3f;
    public int enragedSummonCount = 4;

    [Header("보상 / 피해")]
    public float contactDamage = 25f;
    public int expReward = 200;
    public int coinDrop = 50;

    [Header("넉백 저항")]
    // 총알 넉백을 이 비율만큼만 받음
    public float knockBackTaken = 0.25f;

    [Header("스킬")]
    public int bossKind = 0;            // 0 = 리치 왕, 1 = 지옥의 군주
    // 스킬 시전 중에는 걸어서 움직이지 않음
    [HideInInspector] public bool casting;

    float summonTimer;
    EnemySpawner spawner;

    public bool Enraged => EnemyHealth <= setEnemyHP * 0.5f;

    // 킹 슬라임 (bossKind 2): 쓰러질 때마다 분열 (1마리 → 2마리 → 3마리)
    [HideInInspector] public int slimeGen = 1;
    static readonly List<bosss> slimes = new List<bosss>();
    static int gen2Deaths;
    static int gen3Spawned;
    const int Gen2Hp = 1100;
    const int Gen3Hp = 500;
    bool IsSlime => bossKind == 2;

    // 남은 체력 합계 (아직 분열하지 않은 몫 포함)
    static int SlimeRemaining()
    {
        int sum = 0;
        bool gen1Alive = false;
        foreach (bosss s in slimes)
        {
            if (s == null || s.isDead) continue;
            sum += Mathf.CeilToInt(Mathf.Max(0f, s.EnemyHealth));
            if (s.slimeGen == 1) gen1Alive = true;
        }
        if (gen1Alive) sum += 2 * Gen2Hp;
        sum += (3 - gen3Spawned) * Gen3Hp;
        return sum;
    }

    void Split()
    {
        int count = slimeGen == 1 ? 2 : (gen2Deaths++ == 0 ? 2 : 1);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = (Vector3)(Random.insideUnitCircle.normalized * 3f);
            GameObject clone = Instantiate(gameObject, transform.position + offset, transform.rotation);
            clone.transform.localScale = transform.localScale * 0.75f;
            bosss b = clone.GetComponent<bosss>();
            b.slimeGen = slimeGen + 1;
            b.setEnemyHP = slimeGen == 1 ? Gen2Hp : Gen3Hp;
            b.EnemyHealth = b.setEnemyHP;
            b.casting = false;
            b.coinDrop = slimeGen == 1 ? 15 : 10;
            b.expReward = expReward / 2;
            b.summonCount = 1;
            b.enragedSummonCount = 2;
            clone.GetComponent<SpriteRenderer>().color = Color.white;
            if (slimeGen + 1 == 3) gen3Spawned++;
            Fx.Play("fx_puddle", clone.transform.position, 3f, new Color(0.55f, 1f, 0.35f), 12f);
        }
        Fx.Play("fx_shock", transform.position, 9f, new Color(0.55f, 1f, 0.35f), 16f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner(slimeGen == 1 ? Loc.T("킹 슬라임이 둘로 갈라졌다!") : Loc.T("슬라임이 또 갈라진다!"), 2f);
    }
    public bool IsDead => isDead;

    void Summon(int count)
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.SummonMinions(transform.position, count);
    }

    void Start()
    {
        EnemyHealth = setEnemyHP;

        // 플레이어 찾기
        player = FindFirstObjectByType<PlayerController>().transform;

        // 컴포넌트 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        KilledEnemy = 0;
        bossbar = FindFirstObjectByType<bossbar>();
        // 분열로 복제된 슬라임은 스킬 컴포넌트를 이미 가지고 있음
        BossSkills skills = GetComponent<BossSkills>();
        if (skills == null) skills = gameObject.AddComponent<BossSkills>();
        skills.kind = bossKind;
        if (IsSlime)
        {
            if (slimeGen == 1)
            {
                slimes.Clear();
                gen2Deaths = 0;
                gen3Spawned = 0;
            }
            slimes.Add(this);
        }
    }
    bossbar bossbar;
    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (isDead) return;

        bossbar.bossKind = bossKind;
        if (IsSlime)
        {
            bossbar.MaxHealth = 2600 + 2 * Gen2Hp + 3 * Gen3Hp;
            bossbar.NowHealth = SlimeRemaining();
        }
        else
        {
            bossbar.MaxHealth = setEnemyHP;
            bossbar.NowHealth = Mathf.CeilToInt(EnemyHealth);
        }

        if (player == null) return;

        // 주기적으로 부하 소환
        summonTimer += Time.deltaTime;
        if (summonTimer >= (Enraged ? enragedSummonInterval : summonInterval))
        {
            summonTimer = 0f;
            Summon(Enraged ? enragedSummonCount : summonCount);
        }

        // 적 → 플레이어 방향 계산
        move = (EnermyController.Decoy != null ? EnermyController.Decoy.position : player.position) - transform.position;

        // z축 제거
        move.z = 0;

        // 방향의 길이를 1로 맞춤
        move = move.normalized;

        // 좌우 방향 변경
        if (move.x < 0) spriteRenderer.flipX = true;
        else if (move.x > 0) spriteRenderer.flipX = false;
    }

    void FixedUpdate()
    {
        // 죽지 않았을 때만 이동
        if (!isDead && player != null && !casting) transform.Translate(move * speed * EnermyController.GlobalSpeedMultiplier * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage, float knockBack, Vector3 dir)
    {
        if (isDead) return;

        // 체력 감소
        EnemyHealth -= damage;

        transform.position += dir * knockBack * knockBackTaken;

        // 피격 애니메이션
        //animator.SetTrigger("hit");

        // 피격 색상 효과
        StartCoroutine(HitEffect());

        Debug.Log("적 체력: " + EnemyHealth);

        hitCount++;
        if (hitCount >= hitsPerSummon)
        {
            Summon(summonOnHitCount);
            hitCount = 0;
        }

        // 체력이 0 이하이면 사망
        if (EnemyHealth <= 0) Die(1);
    }

    IEnumerator HitEffect()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        // 죽지 않았을 때만 원래 색으로
        if (!isDead) spriteRenderer.color = Color.white;
    }
    PlayerController playerC;

    void Die(int a)
    {
        if (isDead) return;

        isDead = true;

        // 킹 슬라임은 분열하고, 마지막 한 마리가 쓰러질 때만 보스전이 끝남
        bool lastOne = true;
        if (IsSlime)
        {
            if (a == 1 && slimeGen < 3) Split();
            lastOne = SlimeRemaining() <= 0;
        }
        if (lastOne) bossbar.bossSpawn = false;
        spriteRenderer.color = Color.white;
        if (a == 1)
        {
            
            playerC = Cache<PlayerController>.Get;
            LevelShop levelS = Cache<LevelShop>.Get;
            Level lv = Cache<Level>.Get;

            playerC.nowEXP += expReward * lv.bonusEXP;

            if (playerC.nowEXP >= playerC.needEXP)
            {
                playerC.level++;
                playerC.nowEXP -= playerC.needEXP;

                StartCoroutine(LevelUpSequence(levelS));
            }

            Point point = Cache<Point>.Get;

            if (point != null) point.AddPoint(10);
        }

        if (animator != null) animator.SetTrigger("death");

        StartCoroutine(Death(a));
    }

    IEnumerator LevelUpSequence(LevelShop levelS)
    {
        levelS.toggleLevelUp();


        yield return new WaitForSeconds(1f);


        levelS.toggleLevelUp();
        levelS.openLevelShop();
    }

    public GameObject hp;
    [Header("힐팩")]
    public int chanceofHP = 30;

    IEnumerator Death(int a)
    {
        // 죽음 애니메이션 시간
        yield return new WaitForSeconds(1f);
        EnemySpawner enemySpawner = Cache<EnemySpawner>.Get;
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {


            enemySpawner.killedEnemy++;
            if (Random.Range(0, 100) < chanceofHP) Instantiate(hp, transform.position, Quaternion.identity);
            for (int i = 0; i < coinDrop; i++)
            {
                float rx = Random.Range(-5f, 5f);
                float ry = Random.Range(-5f, 5f);
                Vector2 drop = new Vector2(transform.position.x + rx, transform.position.y + ry);
                Instantiate(coin, drop, Quaternion.identity);
            }
        }

        // 스테이지 진행 (신전 문 열기 등) - 슬라임은 모두 쓰러졌을 때만
        bool allDown = !IsSlime || SlimeRemaining() <= 0;
        if (IsSlime) slimes.Remove(this);
        if (enemySpawner != null && allDown) enemySpawner.OnBossDefeated();

        Destroy(gameObject);
    }



}

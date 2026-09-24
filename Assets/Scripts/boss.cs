using System.Collections;
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
    // 이만큼 맞을 때마다 부하를 소환
    public int hitsPerSummon = 20;

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
    }
    bossbar bossbar;
    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (isDead) return;

        bossbar.MaxHealth = setEnemyHP;
        bossbar.NowHealth = Mathf.CeilToInt(EnemyHealth);

        if (player == null) return;

        // 적 → 플레이어 방향 계산
        move = player.position - transform.position;

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
        if (!isDead && player != null) transform.Translate(move * speed * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage, float knockBack, Vector3 dir)
    {
        if (isDead) return;

        // 체력 감소
        EnemyHealth -= damage;

        transform.position += dir * knockBack;

        // 피격 애니메이션
        //animator.SetTrigger("hit");

        // 피격 색상 효과
        StartCoroutine(HitEffect());

        Debug.Log("적 체력: " + EnemyHealth);

        hitCount++;
        if (hitCount >= hitsPerSummon)
        {
            EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
            enemySpawner.SpawnEnemy(true,transform.position);
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

        bossbar.bossSpawn = false;
        spriteRenderer.color = Color.white;
        EnemySpawner spawn = FindFirstObjectByType<EnemySpawner>();
        spawn.boss1Cleared = true;
        spawn.bossSpawned = false;
        if (a == 1)
        {
            
            playerC = FindFirstObjectByType<PlayerController>();
            LevelShop levelS = FindFirstObjectByType<LevelShop>();
            Level lv = FindFirstObjectByType<Level>();

            playerC.nowEXP += 200 * lv.bonusEXP;

            if (playerC.nowEXP >= playerC.needEXP)
            {
                playerC.level++;
                playerC.nowEXP -= playerC.needEXP;

                StartCoroutine(LevelUpSequence(levelS));
            }

            Point point = FindFirstObjectByType<Point>();

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
        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {


            enemySpawner.killedEnemy++;
            if (Random.Range(0, 100) < chanceofHP) Instantiate(hp, transform.position, Quaternion.identity);
            for (int i = 0; i < 50; i++)
            {
                float rx = Random.Range(-5f, 5f);
                float ry = Random.Range(-5f, 5f);
                Vector2 drop = new Vector2(transform.position.x + rx, transform.position.y + ry);
                Instantiate(coin, drop, Quaternion.identity);
            }
        }
        Destroy(gameObject);
    }



}

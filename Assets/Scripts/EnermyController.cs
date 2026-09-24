using System.Collections;
using UnityEngine;

public class EnermyController : MonoBehaviour
{
    public float speed = 2f;
    public int setEnemyHP = 10;
    public float EnemyHealth;


    private Transform player;
    private Vector3 move;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public int KilledEnemy;

    public GameObject coin;

    [Header("보상 / 피해")]
    public float contactDamage = 5f;   // 플레이어와 닿았을 때 주는 피해
    public int expReward = 20;          // 처치 경험치
    public int coinDrop = 1;            // 떨어뜨리는 코인 수

    [Header("움직임")]
    // 좌우로 흔들리며 다가옴 (0이면 직선)
    public float zigzagAmplitude = 0f;
    public float zigzagFrequency = 3f;
    // 주기적으로 빠르게 돌진 (0이면 없음)
    public float dashInterval = 0f;
    public float dashDuration = 0.4f;
    public float dashSpeedMultiplier = 2.5f;
    // 총알 넉백을 이 비율만큼만 받음
    public float knockBackTaken = 1f;

    float moveTime;

    [Header("겹침 방지")]
    // 다른 적과 겹친 만큼 밀어내는 속도 (초당)
    public float separationSpeed = 4f;

    [Header("중간 보스")]
    // 닿아도 자폭하지 않고 계속 싸움
    public bool survivesContact = false;
    // 평소 몸 색 (피격 후 이 색으로 돌아옴)
    public Color baseColor = Color.white;
    // 이 적이 처치됐을 때
    public System.Action onKilled;

    [Header("스킬")]
    public EnemySkillType skill = EnemySkillType.None;
    public float skillCooldown = 0f;       // 0이면 스킬 기본값
    // 스킬 시전 중에는 걸어서 움직이지 않음
    [HideInInspector] public bool casting;

    public bool IsDead => isDead;

    // 시간 왜곡 (적 전체 감속)
    public static float GlobalSpeedMultiplier = 1f;
    // 거울 분신이 있으면 플레이어 대신 분신을 쫓음
    public static Transform Decoy;
    // 적이 처치됐을 때 (영혼 모으기 등)
    public static System.Action<Vector3> Killed;

    private CircleCollider2D bodyCollider;
    private static readonly Collider2D[] nearby = new Collider2D[24];

    void Start()
    {
        EnemyHealth = setEnemyHP;
        bodyCollider = GetComponent<CircleCollider2D>();

        // 플레이어 찾기
        player = FindFirstObjectByType<PlayerController>().transform;

        // 컴포넌트 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        KilledEnemy = 0;
        spriteRenderer.color = baseColor;

        // 머리 위 체력바
        EnemyHealthBar.Attach(this, spriteRenderer);

        if (skill != EnemySkillType.None)
        {
            EnemySkill s = gameObject.AddComponent<EnemySkill>();
            s.type = skill;
            s.cooldown = skillCooldown;
        }
    }

    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (isDead) return;

        if (player == null) return;

        // 적 → 플레이어 방향 계산
        move = (Decoy != null ? Decoy.position : player.position) - transform.position;

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
        if (!isDead && player != null && !casting)
        {
            moveTime += Time.fixedDeltaTime;

            Vector3 dir = move;
            if (zigzagAmplitude > 0f)
            {
                // 진행 방향의 수직으로 흔들림
                Vector3 side = new Vector3(-move.y, move.x, 0f);
                dir = (move + side * Mathf.Sin(moveTime * zigzagFrequency) * zigzagAmplitude).normalized;
            }

            float currentSpeed = speed * GlobalSpeedMultiplier;
            if (dashInterval > 0f && Mathf.Repeat(moveTime, dashInterval) < dashDuration) currentSpeed *= dashSpeedMultiplier;

            Vector3 step = (dir * currentSpeed + Separation() * separationSpeed) * Time.fixedDeltaTime;

            // 구조물에 막히면 벽을 따라 미끄러짐 (이미 끼어 있으면 빠져나오도록 그대로 이동)
            if (!Blocked(transform.position) && Blocked(transform.position + step))
            {
                Vector3 alongX = new Vector3(step.x, 0f, 0f);
                Vector3 alongY = new Vector3(0f, step.y, 0f);
                if (!Blocked(transform.position + alongX)) step = alongX;
                else if (!Blocked(transform.position + alongY)) step = alongY;
                else step = Vector3.zero;
            }
            transform.Translate(step);
        }
    }

    static readonly Collider2D[] wallHits = new Collider2D[8];

    bool Blocked(Vector3 pos)
    {
        float r = bodyCollider != null ? WorldRadius(bodyCollider) * 0.8f : 0.5f;
        Vector2 center = (Vector2)pos + (bodyCollider != null ? bodyCollider.offset * (Vector2)transform.lossyScale : Vector2.zero);
        int n = Physics2D.OverlapCircleNonAlloc(center, r, wallHits);
        for (int i = 0; i < n; i++)
            if (!wallHits[i].isTrigger && wallHits[i].CompareTag("Wall")) return true;
        return false;
    }

    // 스킬로 스스로 터졌을 때 (처치로 인정)
    public void KillBySkill()
    {
        EnemyHealth = 0;
        Die(1);
    }

    // 원 콜라이더의 월드 반지름
    static float WorldRadius(CircleCollider2D c)
    {
        Vector3 s = c.transform.lossyScale;
        return c.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }

    // 주변 적과 겹친 정도에 비례해 바깥으로 밀어내는 방향
    Vector3 Separation()
    {
        if (bodyCollider == null) return Vector3.zero;

        Vector2 center = bodyCollider.bounds.center;
        float myRadius = WorldRadius(bodyCollider);

        // 가장 큰 적까지 잡히도록 넉넉하게 검색
        int count = Physics2D.OverlapCircleNonAlloc(center, myRadius * 3f, nearby);

        Vector2 push = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            Collider2D other = nearby[i];
            if (other == bodyCollider || !other.CompareTag("enermy")) continue;

            CircleCollider2D otherCircle = other as CircleCollider2D;
            if (otherCircle == null) continue;

            EnermyController otherEnemy = other.GetComponent<EnermyController>();
            if (otherEnemy == null || otherEnemy.IsDead) continue;

            Vector2 away = center - (Vector2)other.bounds.center;
            float dist = away.magnitude;
            float minDist = myRadius + WorldRadius(otherCircle);
            if (dist >= minDist) continue;

            // 완전히 같은 위치면 임의 방향으로 벌림
            if (dist < 0.001f) away = Random.insideUnitCircle.normalized;
            else away /= dist;

            push += away * ((minDist - dist) / minDist);
        }

        return Vector2.ClampMagnitude(push, 1.5f);
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

        // 체력이 0 이하이면 사망
        if (EnemyHealth <= 0) Die(1);
    }

    IEnumerator HitEffect()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        // 죽지 않았을 때만 원래 색으로
        if (!isDead) spriteRenderer.color = baseColor;
    }
    PlayerController playerC;
    
    void Die(int a)
    {
        if (isDead) return;

        isDead = true;
        if (a == 1) Killed?.Invoke(transform.position);
        if (a == 1) onKilled?.Invoke();
        spriteRenderer.color = baseColor;

        // 사망 연출 중에는 총알이나 플레이어와 부딪히지 않음
        if (bodyCollider != null) bodyCollider.enabled = false;

        if (a == 1)
        {
            playerC = FindFirstObjectByType<PlayerController>();
            LevelShop levelS = FindFirstObjectByType<LevelShop>();
            Level lv = FindFirstObjectByType<Level>();

            playerC.nowEXP += expReward * lv.bonusEXP;

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
        enemySpawner.spawnedEnemys--;
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {
            

            enemySpawner.killedEnemy++;
            if ( Random.Range(0,100) < chanceofHP) Instantiate(hp, transform.position, Quaternion.identity);
            for (int i = 0; i < coinDrop; i++)
            {
                Vector2 drop = transform.position;
                if (i > 0) drop += new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f));
                Instantiate(coin, drop, Quaternion.identity);
            }
        }
        Destroy(gameObject);
    }

    // 플레이어와 닿으면 피해를 주고 자폭
    // 플레이어가 무적이면 붙어 있다가 무적이 끝나는 순간 피해를 줌
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TouchPlayer(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TouchPlayer(collision);
    }

    void TouchPlayer(Collider2D collision)
    {
        if (isDead || !collision.CompareTag("Player")) return;

        PlayerController target = collision.GetComponent<PlayerController>();
        if (target != null && target.TryHit(contactDamage) && !survivesContact) Die(0);
    }


}

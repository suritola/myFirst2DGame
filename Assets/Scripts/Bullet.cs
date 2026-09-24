using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    // 일반 총알은 1
    // 스킬 총알은 500
    public float damage = 1f;

    //관통 개수
    public int pene = 1;

    public float blood = 0f;

    // 스킬로 쏜 총알은 스킬 게이지를 채우지 않음
    public bool isSkill = false;

    // 적을 맞혔을 때 스킬 게이지가 차는 양 (멀티샷이면 발당 비율만큼)
    public float skillCharge = 1f;

    // 맞은 적을 밀어내는 거리 (쏠 때 플레이어가 정해 줌)
    public float knockBack = 0.3f;

    PlayerController playerC;

    private Vector2 dir;

    public Vector2 Direction => dir;
    // 적을 맞혔을 때 추가 효과 (연쇄 번개, 폭발 등)
    public System.Action<Bullet, Collider2D> onHitEnemy;
    public float lifetime = 2f;

    public Vector2 Dir
    {
        set
        {
            dir = value.normalized;

            // 총알이 날아가는 방향을 바라보도록 회전
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private int remainPene;
    void Start()
    {
        remainPene = pene;
        playerC = FindAnyObjectByType<PlayerController>();
        mainCamera = Camera.main;
        Destroy(gameObject, lifetime);
    }


    void Update()
    {
        transform.position += (Vector3)dir * speed * Time.deltaTime;
    }

    private Camera mainCamera;
    private void OnTriggerEnter2D(
        Collider2D collision
    )
    {
        // =========================
        // 벽에 맞으면 삭제
        // =========================

        if (collision.CompareTag("Wall"))
        {
            Destroy(gameObject);

            return;
        }


        // =========================
        // 적에 맞으면
        // =========================

        if (collision.CompareTag("enermy"))
        {
            EnermyController enemy = collision.GetComponent<EnermyController>();
        

            if (enemy != null)
            {
                // 적에게 데미지
                if (enemy.EnemyHealth <= 0f) return;

                enemy.TakeDamage(damage, knockBack, dir);
                remainPene--;
                Fx.Play("fx_spark", transform.position, 1.2f, Color.white, 26f);


                // =========================
                // 스킬 포인트 추가
                // =========================

                SkillGauge skillGauge = FindFirstObjectByType<SkillGauge>();

                // 스킬 게이지는 이제 시간으로 참 (SkillGauge)

                onHitEnemy?.Invoke(this, collision);
            }


            // 총알 삭제
            if (remainPene < 1)
            Destroy(gameObject);
        }
        if (collision.CompareTag("boss"))
        {
            bosss enemy = FindFirstObjectByType<bosss>();

            if (enemy != null)
            {
                // 적에게 데미지
                if (enemy.EnemyHealth <= 0f) return;

                enemy.TakeDamage(damage, knockBack, dir);
                remainPene--;
                Fx.Play("fx_spark", transform.position, 1.2f, Color.white, 26f);


                // =========================
                // 스킬 포인트 추가
                // =========================

                SkillGauge skillGauge = FindFirstObjectByType<SkillGauge>();

                // 스킬 게이지는 이제 시간으로 참 (SkillGauge)
            }


            // 총알 삭제
            if (remainPene < 1)
                Destroy(gameObject);
        }
    }
    public void arisePene()
    {
        pene++;
        remainPene++;
}
}
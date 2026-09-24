using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    // 일반 총알은 1
    // 스킬 총알은 500
    public int damage = 1;

    //관통 개수
    public int pene = 1;

    public int blood = 0;

    // 스킬로 쏜 총알은 스킬 게이지를 채우지 않음
    public bool isSkill = false;

    PlayerController playerC;

    private Vector2 dir;

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
        Destroy(gameObject, 2f);
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
                if (enemy.EnemyHealth < 1) return;

                enemy.TakeDamage(damage, playerC.knockBack, dir);
                remainPene--;
                if (playerC.PlayerHealth < playerC.PlayerMaxHealth - blood) playerC.PlayerHealth += blood;
                else playerC.PlayerHealth = playerC.PlayerMaxHealth;


                // =========================
                // 스킬 포인트 추가
                // =========================

                SkillGauge skillGauge = FindFirstObjectByType<SkillGauge>();

                if (skillGauge != null && !isSkill) skillGauge.AddSkillPoint(1);
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
                if (enemy.EnemyHealth < 1) return;

                enemy.TakeDamage(damage, playerC.knockBack, dir);
                remainPene--;
                if (playerC.PlayerHealth < playerC.PlayerMaxHealth - blood) playerC.PlayerHealth += blood;
                else playerC.PlayerHealth = playerC.PlayerMaxHealth;


                // =========================
                // 스킬 포인트 추가
                // =========================

                SkillGauge skillGauge = FindFirstObjectByType<SkillGauge>();

                if (skillGauge != null && !isSkill) skillGauge.AddSkillPoint(1);
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
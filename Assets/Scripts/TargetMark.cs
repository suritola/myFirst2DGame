using UnityEngine;

public class TargetMark : MonoBehaviour
{
    [Header("타겟")]
    public Transform target;


    [Header("회전")]
    public float rotationSpeed = 180f;

    [Header("크기")]
    public float startScale = 2f;
    public float endScale = 1f;

    [Header("색상")]
    public Color weakColor = Color.gray;

    private SpriteRenderer spriteRenderer;
    private EnermyController enemy;
    private PlayerController player;

    private Color normalColor;

    private bool damageEnough = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        player = FindFirstObjectByType<PlayerController>();

        if (spriteRenderer != null) normalColor = spriteRenderer.color;

        // 처음에는 200%
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 적을 따라감
        transform.position = target.position;

        if (enemy == null) enemy = target.GetComponent<EnermyController>();

        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (enemy == null || player == null) return;

        // =====================================
        // 데미지가 아직 부족한 상태
        // =====================================

        if (!damageEnough)
        {
            // 회색
            if (spriteRenderer != null) spriteRenderer.color = weakColor;

            // 현재 데미지 / 적 체력 비율
            float damageRatio = player.skillDamage / enemy.EnemyHealth;

            // 0 ~ 1 사이로 제한
            damageRatio = Mathf.Clamp01(damageRatio);

            // 데미지 비율에 따라 크기 결정
            float currentScale = Mathf.Lerp(startScale,endScale,damageRatio);

            transform.localScale = Vector3.one * currentScale;

            // 데미지가 충분해졌는지 확인
            if (player.skillDamage >= enemy.EnemyHealth)
            {
                damageEnough = true;

                // 현재 크기에서 멈춤
                if (spriteRenderer != null) spriteRenderer.color = normalColor;
            }

            // 아직 부족하면 회전하지 않음
            if (!damageEnough) return;
        }

        // =====================================
        // 데미지가 충분해진 이후
        // =====================================

        // 현재 크기 그대로 유지
        // 회전만 시작
        transform.Rotate(Vector3.forward,rotationSpeed * Time.unscaledDeltaTime);
    }


}

using UnityEngine;

// 보스를 쓰러뜨리면 열리는 신전 문. 플레이어가 들어오면 다음 스테이지로
public class PortalGate : MonoBehaviour
{
    [Header("연출")]
    public Transform swirl;         // 회전하는 소용돌이
    public Transform glow;          // 커졌다 작아지는 빛
    public float spinSpeed = 90f;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.12f;

    Vector3 glowScale;

    void OnEnable()
    {
        if (glow != null) glowScale = glow.localScale;
    }

    void Update()
    {
        if (swirl != null) swirl.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        if (glow != null) glow.localScale = glowScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && StageManager.Instance != null) StageManager.Instance.EnterPortal();
    }
}

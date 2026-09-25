using UnityEngine;
using UnityEngine.UI;

public class SkillGauge : MonoBehaviour
{
    [Header("실제로 줄어들고 차오르는 Image")]
    public Image gaugeImage;

    [Header("스킬 포인트")]
    public float SkillPoint = 0f;
    public int MaxSkillPoint = 10;

    [Header("자동 충전")]
    // 적을 맞혀서가 아니라 시간이 지나면서 참 (스킬 난사 방지)
    public float pointsPerSecond = 1.4f;
    // 적을 처치하면 잠깐 더 빨리 참
    public float killBoost = 3f;
    public float killBoostTime = 1.5f;
    float boostUntil;

    void OnEnable() => EnermyController.Killed += OnKill;
    void OnDisable() => EnermyController.Killed -= OnKill;
    void OnKill(Vector3 pos) => boostUntil = Time.time + killBoostTime;

    public bool Boosted => Time.time < boostUntil;

    void Update()
    {
        PlayerController p = Hostile.Player;
        // 멈췄을 때나 스킬을 쓰는 중에는 차지 않음
        if (Time.timeScale == 0f || (p != null && p.IsSkillUsing) || IsFull()) return;
        // 캐릭터 스킬 게이지 %가 높을수록 게이지가 길어서 늦게 참
        AddSkillPoint(pointsPerSecond * GameMode.GaugeMul / Mathf.Max(0.1f, CharacterData.Current.gauge) * (Boosted ? killBoost : 1f) * Time.deltaTime);
    }

    void Start()
    {
        // Inspector에 연결 안 했으면
        // 현재 오브젝트의 Image를 자동으로 가져옴
        if (gaugeImage == null)
        {
            gaugeImage = GetComponent<Image>();
        }

        UpdateGauge();
    }

    public void AddSkillPoint(float amount)
    {
        SkillPoint += amount;

        SkillPoint = Mathf.Clamp(SkillPoint, 0, MaxSkillPoint);

        UpdateGauge();
    }

    public bool IsFull()
    {
        return SkillPoint >= MaxSkillPoint;
    }

    public void ResetSkillPoint()
    {
        SkillPoint = 0;

        UpdateGauge();
    }
    int order;

    void UpdateGauge()
    {
        if (gaugeImage == null)
        {
            Debug.LogWarning("Gauge Image가 연결되지 않았습니다!");
            return;
        }

        float value = (float)SkillPoint / MaxSkillPoint;

        gaugeImage.fillAmount = value;


        // 충전 중에는 청록색, 가득 차면 금색
        if (value >= 1) gaugeImage.color = new Color(1f, 0.82f, 0.3f);
        else gaugeImage.color = Boosted ? new Color(0.6f, 1f, 0.95f) : new Color(0.3f, 0.86f, 0.9f);

    }
}
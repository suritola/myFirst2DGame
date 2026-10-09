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

    // 영혼 트리 '빠른 충전'
    static float TreeMul => SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.TreeGaugeMul : 1f;

    // 2.1.8: 가득 차는 순간 소리 + 가득 찬 동안 게이지가 금빛으로 숨 쉬듯 반짝임 (처음 한 번 뜨는 도움말 말고는 알림이 없었음)
    bool wasFull;
    Color baseColor = Color.white;
    static readonly Color ReadyGold = new Color(1f, 0.85f, 0.35f);

    void ReadyFeedback()
    {
        bool full = IsFull();
        if (full && !wasFull && Time.timeScale > 0f && !GameInput.Auto)
        {
            Hostile.Play("chime", 0.55f, 1.4f);
            Hostile.Play("pulse", 0.35f, 1.6f);
        }
        wasFull = full;
        if (gaugeImage != null)
            gaugeImage.color = full ? Color.Lerp(baseColor, ReadyGold, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : baseColor;
    }

    void Update()
    {
        ReadyFeedback();
        PlayerController p = Hostile.Player;
        // 멈췄을 때나 스킬을 쓰는 중에는 차지 않음
        if (Time.timeScale == 0f || (p != null && p.IsSkillUsing) || IsFull()) return;
        // 캐릭터 스킬 게이지 %가 높을수록 게이지가 길어서 늦게 참
        AddSkillPoint(pointsPerSecond * GameMode.GaugeMul / Mathf.Max(0.1f, CharacterData.Current.gauge) * (Boosted ? killBoost : 1f) * TreeMul * Time.deltaTime);
        if (IsFull()) Hints.Show("ult", "스킬 게이지가 가득 찼습니다! 우클릭으로 필살기를 씁니다.");
    }

    void Start()
    {
        // Inspector에 연결 안 했으면
        // 현재 오브젝트의 Image를 자동으로 가져옴
        if (gaugeImage == null)
        {
            gaugeImage = GetComponent<Image>();
        }
        if (gaugeImage != null) baseColor = gaugeImage.color;

        UpdateGauge();
    }

    public void AddSkillPoint(float amount)
    {
        SkillPoint += amount;

        SkillPoint = Mathf.Clamp(SkillPoint, 0, MaxSkillPoint);

        UpdateGauge();
    }

    // 최대치를 바꿀 때 (재활용 에너지): 넘친 양을 맞추고 게이지를 바로 다시 그림
    public void SetMax(int max)
    {
        MaxSkillPoint = Mathf.Max(1, max);
        SkillPoint = Mathf.Clamp(SkillPoint, 0, MaxSkillPoint);
        UpdateGauge();
    }

    public bool IsFull()
    {
        return SkillPoint >= MaxSkillPoint;
    }

    public void ResetSkillPoint()
    {
        // 영혼 트리 '잔불': 필살기를 쓴 뒤 게이지가 조금 남음
        SkillPoint = SpecialAbilities.SharedInstance != null ? MaxSkillPoint * SpecialAbilities.SharedInstance.TreeUltRefund : 0f;

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
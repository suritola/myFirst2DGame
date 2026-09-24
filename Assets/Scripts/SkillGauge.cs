using UnityEngine;
using UnityEngine.UI;

public class SkillGauge : MonoBehaviour
{
    [Header("실제로 줄어들고 차오르는 Image")]
    public Image gaugeImage;

    [Header("스킬 포인트")]
    public float SkillPoint = 0f;
    public int MaxSkillPoint = 10;

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
        else gaugeImage.color = new Color(0.3f, 0.86f, 0.9f);

        Debug.Log("게이지 Fill Amount: " + value);
    }
}
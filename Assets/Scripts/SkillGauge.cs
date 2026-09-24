using UnityEngine;
using UnityEngine.UI;

public class SkillGauge : MonoBehaviour
{
    [Header("실제로 줄어들고 차오르는 Image")]
    public Image gaugeImage;

    [Header("스킬 포인트")]
    public int SkillPoint = 0;
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

    public void AddSkillPoint(int amount)
    {
        SkillPoint += amount;

        SkillPoint = Mathf.Clamp(SkillPoint, 0, MaxSkillPoint);

        Debug.Log("현재 SkillPoint: " + SkillPoint);

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


        if (value >= 1) gaugeImage.color = Color.yellow;
        else gaugeImage.color = Color.blue;

        Debug.Log("게이지 Fill Amount: " + value);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bossbar : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject bar;
    public GameObject backBar;
    public int NowHealth;
    public int MaxHealth;

    public float MaxBarX = 1333f;

    public float BarX;
    public float BarY = 119.2869f;

    public bool bossSpawn = false;
    void Start()
    {
        // 씬에 배치된 보스바 크기를 최대치로 사용
        RectTransform barRect = bar.GetComponent<RectTransform>();
        MaxBarX = barRect.sizeDelta.x;
        BarY = barRect.sizeDelta.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (!bossSpawn) backBar.SetActive(false);
        else
        {
            backBar.SetActive(true);
            if (MaxHealth <= 0) return;
            BarX = Mathf.Clamp01((float)NowHealth / (float)MaxHealth) * MaxBarX;
            bar.GetComponent<RectTransform>().sizeDelta = new Vector2(BarX, BarY);
        }


    }
}

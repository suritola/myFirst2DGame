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
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!bossSpawn) backBar.SetActive(false);
        else
        {
            backBar.SetActive(true);
            BarX = (float)( (float)NowHealth / (float)MaxHealth ) * 1333f;
            bar.GetComponent<RectTransform>().sizeDelta = new Vector2(BarX, BarY);
        }


    }
}

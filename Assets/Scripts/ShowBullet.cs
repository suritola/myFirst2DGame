using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowBullet : MonoBehaviour
{
    public TextMeshProUGUI TextInput;
    public int MaxBullet;
    public int NowBullet;
    private PlayerController player;
    int order;
    // Start is called before the first frame update
    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!string.IsNullOrEmpty(player.ammoTextOverride))
        {
            TextInput.text = player.ammoTextOverride;
        }
        else if (player.reload == 0)
        {
            TextInput.text = player.NowBullet + " / " + player.MaxBullet;
        }
        else
        {
            // 0.3초마다 점이 하나씩 늘어나는 장전 표시
            order = (int)(Time.unscaledTime / 0.3f) % 3 + 1;
            TextInput.text = Loc.T("장전 중") + new string('.', order);
        }

    }
}

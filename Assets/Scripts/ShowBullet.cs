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
    int state, shownNow = -1, shownMax = -1, shownDots;
    string shownOverride;
    // Start is called before the first frame update
    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        // 보이는 내용이 바뀔 때만 글자를 새로 (매 프레임 문자열을 만들지 않게)
        if (!string.IsNullOrEmpty(player.ammoTextOverride))
        {
            if (shownOverride != player.ammoTextOverride || state != 1)
            {
                state = 1;
                shownOverride = player.ammoTextOverride;
                TextInput.text = player.ammoTextOverride;
            }
        }
        else if (player.reload == 0)
        {
            if (state != 2 || shownNow != player.NowBullet || shownMax != player.MaxBullet)
            {
                state = 2;
                shownNow = player.NowBullet;
                shownMax = player.MaxBullet;
                TextInput.text = player.NowBullet + " / " + player.MaxBullet;
            }
        }
        else
        {
            // 0.3초마다 점이 하나씩 늘어나는 장전 표시
            order = (int)(Time.unscaledTime / 0.3f) % 3 + 1;
            if (state != 3 || shownDots != order)
            {
                state = 3;
                shownDots = order;
                TextInput.text = Loc.T("장전 중") + new string('.', order);
            }
        }

    }
}

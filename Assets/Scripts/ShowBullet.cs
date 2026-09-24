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
        if (player.reload == 0)
        {
            TextInput.text = player.NowBullet + " / " + player.MaxBullet;
            order = 1;
        }
        else
        {
            if (order == 1)
            {
                TextInput.text = "Reloading.";
                order = 2;
            }
            if (order == 2)
            {
                TextInput.text = "Reloading..";
                order = 3;
            }
            if (order == 3)
            {
                TextInput.text = "Reloading..";
                order = 1;
            }
        }

    }
}

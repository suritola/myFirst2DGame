using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPpack : MonoBehaviour
{
    public GameObject pack;
    PlayerController playerC;
    public float healAmount = 25f;
    // Start is called before the first frame update
    void Start()
    {
        playerC = FindFirstObjectByType<PlayerController>();
    }

    // Update is called once per frame
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // =========================
        // 힐팩 충돌
        // =========================

        if (collision.CompareTag("Player"))
        {
            Destroy(pack);
            playerC.PlayerHealth += healAmount;
            if (playerC.PlayerHealth > playerC.PlayerMaxHealth) playerC.PlayerHealth = playerC.PlayerMaxHealth;
            return;
        }
    }
}

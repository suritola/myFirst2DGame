using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject enemy2Prefab;
    public GameObject enemy3Prefab;
    public GameObject BossPrefab;
    public int killedEnemy = 0;
    public int paze = 1;
    PlayerController playerC;
    public bool boss1Cleared = false;
    public bool bossSpawned = false;
    public int spawnedEnemys = 0;

    public float spawnTime = 5f; // n의 초기값 = 5초

    void Start()
    {
        // 게임 시작 시 적 1마리 생성
        SpawnEnemy(false,transform.position);
        bossbar = FindFirstObjectByType<bossbar>();
        bossSpawned = false;
        boss1Cleared = false;

        // 5초마다 적 생성
        InvokeRepeating("spawnNormalEnemy", spawnTime, spawnTime);
        InvokeRepeating("clear", 30, 30);
    }

    void clear()
    {
        Coin coin1 = FindFirstObjectByType<Coin>();
        GameObject[] coins = GameObject.FindGameObjectsWithTag("coin");

        foreach (GameObject coin in coins)
        {
            Destroy(coin);
            coin1.AddCoin(1 + playerC.bonusCoin);
        }
            
    }
    void spawnNormalEnemy()
    {
        SpawnEnemy(false, transform.position);
    }

    bossbar bossbar;
    public void SpawnEnemy(bool boss, Vector3 here)
    {
        if (spawnedEnemys >= 10) return;

        if (!boss)
        {
            // 랜덤한 위치 생성
            spawnedEnemys++;
            playerC = FindFirstObjectByType<PlayerController>();


            float randomX = Random.Range(-100f, 100f);
            float randomY = Random.Range(-100f, 100f);
            while (randomX < 10f && randomX >= -10f) randomX = Random.Range(-100f, 100f);
            while (randomY < 10f && randomY >= -10f) randomY = Random.Range(-100f, 100f);

            Vector3 randomPosition = new Vector3(playerC.transform.position.x + randomX, playerC.transform.position.y + randomY, 0);

            // 적 생성
            if (paze == 1) Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
            else if (paze == 2) Instantiate(enemy2Prefab, randomPosition, Quaternion.identity);
            else if (paze == 3) Instantiate(enemy3Prefab, randomPosition, Quaternion.identity);

            if (killedEnemy > 20) paze = 2;
            if (killedEnemy > 40) paze = 3;
            if (killedEnemy > 60 && !bossSpawned && !boss1Cleared)
            {
                bossbar.bossSpawn = true;
                bossSpawned = true;
                Vector3 bossPosition = new Vector3(playerC.transform.position.x + 20f, playerC.transform.position.y + 20f, 0);
                Instantiate(BossPrefab, bossPosition, Quaternion.identity);
            }
        }
        else
        {
            // 적 생성
            Instantiate(enemyPrefab, here, Quaternion.identity);
            Instantiate(enemy2Prefab, here, Quaternion.identity);
            Instantiate(enemy3Prefab, here, Quaternion.identity);
        }
    }

}
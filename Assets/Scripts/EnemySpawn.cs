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

    [Header("스폰 위치")]
    // 적이 생성될 수 있는 맵 안쪽 범위 (벽 안쪽에서 조금 띄움)
    public Vector2 spawnAreaMin = new Vector2(-38f, -27f);
    public Vector2 spawnAreaMax = new Vector2(39f, 21f);
    // 플레이어와 최소 이만큼 떨어진 곳에만 생성
    public float minSpawnDistance = 12f;
    // 보스가 나오는 신전 문 위치
    public Vector2 bossSpawnPoint = new Vector2(0.5f, 24f);

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


            Vector3 randomPosition = GetSpawnPosition(playerC.transform.position);

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
                // 보스는 신전 문에서 등장
                Instantiate(BossPrefab, bossSpawnPoint, Quaternion.identity);
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

    // 맵 안에서 플레이어와 충분히 떨어진 위치를 고름
    // 가능하면 화면 밖에서 나오게 하고, 못 찾으면 거리 조건만 지킴
    Vector3 GetSpawnPosition(Vector3 playerPos)
    {
        Camera cam = Camera.main;
        Vector3 fallback = Vector3.zero;
        bool hasFallback = false;

        for (int i = 0; i < 30; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                0);

            if (Vector2.Distance(pos, playerPos) < minSpawnDistance) continue;

            if (cam == null) return pos;

            Vector3 vp = cam.WorldToViewportPoint(pos);
            bool offScreen = vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
            if (offScreen) return pos;

            if (!hasFallback)
            {
                fallback = pos;
                hasFallback = true;
            }
        }

        if (hasFallback) return fallback;

        // 조건에 맞는 곳이 없으면 플레이어 반대편 맵 끝에서 생성
        return new Vector3(
            playerPos.x < 0f ? spawnAreaMax.x : spawnAreaMin.x,
            playerPos.y < 0f ? spawnAreaMax.y : spawnAreaMin.y,
            0);
    }

}
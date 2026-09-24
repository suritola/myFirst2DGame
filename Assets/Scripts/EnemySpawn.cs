using System.Collections;
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

    [Header("페이즈별 난이도 (1, 2, 3 페이즈)")]
    // 적 생성 간격(초)
    public float[] spawnIntervalByPhase = { 1.8f, 1.4f, 1.1f };
    // 동시에 살아 있을 수 있는 최대 적 수
    public int[] maxAliveByPhase = { 8, 12, 16 };
    // 이 수만큼 처치하면 다음 페이즈 / 보스
    // 페이즈별 등장 비율 (해골, 구울, 망령) - 이전 적도 계속 나오고 상위 적 비율이 높아짐
    public Vector3[] spawnWeightsByPhase =
    {
        new Vector3(100f, 0f, 0f),
        new Vector3(45f, 55f, 0f),
        new Vector3(25f, 35f, 40f),
    };

    public int phase2Kills = 20;
    public int phase3Kills = 40;
    public int bossKills = 60;

    int PhaseIndex => Mathf.Clamp(paze - 1, 0, 2);
    // 보스전에는 부하가 더 나올 수 있도록 최대 수를 늘림
    public int bossExtraAlive = 10;

    int MaxAlive => maxAliveByPhase[Mathf.Min(PhaseIndex, maxAliveByPhase.Length - 1)] + (bossSpawned ? bossExtraAlive : 0);

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

        // 페이즈에 따라 간격이 짧아지는 적 생성
        StartCoroutine(SpawnLoop());
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
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float interval = spawnIntervalByPhase[Mathf.Min(PhaseIndex, spawnIntervalByPhase.Length - 1)];
            yield return new WaitForSeconds(interval);
            SpawnEnemy(false, transform.position);
        }
    }

    bossbar bossbar;
    public void SpawnEnemy(bool boss, Vector3 here)
    {
        if (spawnedEnemys >= MaxAlive) return;

        if (!boss)
        {
            // 랜덤한 위치 생성
            spawnedEnemys++;
            playerC = FindFirstObjectByType<PlayerController>();


            Vector3 randomPosition = GetSpawnPosition(playerC.transform.position);

            // 적 생성
            Instantiate(PickEnemyForPhase(), randomPosition, Quaternion.identity);

            if (killedEnemy >= phase2Kills) paze = 2;
            if (killedEnemy >= phase3Kills) paze = 3;
            if (killedEnemy >= bossKills && !bossSpawned && !boss1Cleared)
            {
                bossbar.bossSpawn = true;
                bossSpawned = true;
                // 보스는 신전 문에서 등장
                Instantiate(BossPrefab, bossSpawnPoint, Quaternion.identity);
            }
        }
        else
        {
            SummonMinions(here, 3);
        }
    }

    GameObject PickEnemyForPhase()
    {
        Vector3 w = spawnWeightsByPhase[Mathf.Min(PhaseIndex, spawnWeightsByPhase.Length - 1)];
        float roll = Random.Range(0f, w.x + w.y + w.z);
        if (roll < w.x) return enemyPrefab;
        if (roll < w.x + w.y) return enemy2Prefab;
        return enemy3Prefab;
    }

    // 보스가 부르는 부하: 처치 시 수가 줄어들므로 여기서도 세어야 함
    // 약한 적이 더 자주 나오도록 해골 3 : 구울 2 : 망령 1 비율
    public void SummonMinions(Vector3 here, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (spawnedEnemys >= MaxAlive) return;

            int roll = Random.Range(0, 6);
            GameObject minion = roll < 3 ? enemyPrefab : roll < 5 ? enemy2Prefab : enemy3Prefab;

            spawnedEnemys++;
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
            Instantiate(minion, here + (Vector3)offset, Quaternion.identity);
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
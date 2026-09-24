using System.Collections;
using UnityEngine;

// 스테이지 설정(SpawnPhase, StageConfig)은 StageConfig.cs
// 이 파일에는 컴포넌트 클래스만 둔다 (Unity가 파일의 첫 클래스를 스크립트로 연결함)
public class EnemySpawner : MonoBehaviour
{
    [Header("스테이지")]
    public StageConfig[] stages;
    public int stageIndex = 0;

    [Header("진행 상황")]
    public int killedEnemy = 0;
    public int paze = 1;
    public bool bossSpawned = false;
    public bool bossCleared = false;
    public int spawnedEnemys = 0;
    // 스테이지 전환 연출 중에는 생성을 멈춤
    public bool spawningEnabled = true;

    // 보스전에는 부하가 더 나올 수 있도록 최대 수를 늘림
    public int bossExtraAlive = 10;

    [Header("스폰 위치")]
    // 적이 생성될 수 있는 맵 안쪽 범위 (벽 안쪽에서 조금 띄움)
    public Vector2 spawnAreaMin = new Vector2(-38f, -27f);
    public Vector2 spawnAreaMax = new Vector2(39f, 21f);
    // 플레이어와 최소 이만큼 떨어진 곳에만 생성
    public float minSpawnDistance = 12f;
    // 보스가 나오는 신전 문 위치
    public Vector2 bossSpawnPoint = new Vector2(0.5f, 24f);

    // 보스를 쓰러뜨렸을 때 (스테이지 번호)
    public System.Action<int> onBossDefeated;

    PlayerController playerC;
    bossbar bossbar;

    public StageConfig Stage => stages[Mathf.Clamp(stageIndex, 0, stages.Length - 1)];
    SpawnPhase Phase => Stage.phases[Mathf.Clamp(paze - 1, 0, Stage.phases.Length - 1)];
    int MaxAlive => Phase.maxAlive + (bossSpawned ? bossExtraAlive : 0);

    void Start()
    {
        if (stages == null || stages.Length == 0 || stages[0].enemies == null || stages[0].enemies.Length == 0)
        {
            Debug.LogError("EnemySpawner: 스테이지 설정(Stages)이 비어 있습니다. 씬을 저장하지 말고 GameScene을 다시 열어 주세요.", this);
            enabled = false;
            return;
        }

        playerC = FindFirstObjectByType<PlayerController>();
        bossbar = FindFirstObjectByType<bossbar>();

        // 게임 시작 시 적 1마리 생성
        SpawnEnemy(false, transform.position);

        // 페이즈에 따라 간격이 짧아지는 적 생성
        StartCoroutine(SpawnLoop());
        InvokeRepeating("clear", 30, 30);
    }

    // 떨어진 코인을 자동으로 회수
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
            yield return new WaitForSeconds(Phase.spawnInterval);
            if (spawningEnabled) SpawnEnemy(false, transform.position);
        }
    }

    public void SpawnEnemy(bool boss, Vector3 here)
    {
        if (boss)
        {
            SummonMinions(here, 3);
            return;
        }

        if (spawnedEnemys >= MaxAlive) return;
        if (playerC == null) playerC = FindFirstObjectByType<PlayerController>();

        spawnedEnemys++;
        Vector3 randomPosition = GetSpawnPosition(playerC.transform.position);
        Instantiate(Pick(Phase.weights), randomPosition, Quaternion.identity);

        UpdatePhase();

        if (killedEnemy >= Stage.bossKills && !bossSpawned && !bossCleared && Stage.bossPrefab != null)
        {
            if (bossbar == null) bossbar = FindFirstObjectByType<bossbar>();
            if (bossbar != null) bossbar.bossSpawn = true;
            bossSpawned = true;

            // 보스는 신전 문에서 등장
            Instantiate(Stage.bossPrefab, bossSpawnPoint, Quaternion.identity);
        }
    }

    // 처치 수에 맞는 페이즈로 올림
    void UpdatePhase()
    {
        int phase = 1;
        for (int i = 1; i < Stage.phases.Length; i++)
        {
            if (killedEnemy >= Stage.phases[i].killsToEnter) phase = i + 1;
        }
        paze = phase;
    }

    // 비율에 따라 적 종류를 고름 (비율이 없는 적은 나오지 않음)
    GameObject Pick(float[] weights)
    {
        GameObject[] enemies = Stage.enemies;
        float total = 0f;
        for (int i = 0; i < enemies.Length; i++) total += Weight(weights, i);

        if (total <= 0f) return enemies[0];

        float roll = Random.Range(0f, total);
        for (int i = 0; i < enemies.Length; i++)
        {
            roll -= Weight(weights, i);
            if (roll < 0f) return enemies[i];
        }
        return enemies[enemies.Length - 1];
    }

    static float Weight(float[] weights, int i) => weights != null && i < weights.Length ? weights[i] : 0f;

    // 보스가 부르는 부하: 처치 시 수가 줄어들므로 여기서도 세어야 함
    public void SummonMinions(Vector3 here, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (spawnedEnemys >= MaxAlive) return;

            spawnedEnemys++;
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
            Instantiate(Pick(Stage.minionWeights), here + (Vector3)offset, Quaternion.identity);
        }
    }

    public void OnBossDefeated()
    {
        bossCleared = true;
        bossSpawned = false;
        onBossDefeated?.Invoke(stageIndex);
    }

    // 다음 스테이지 시작: 남은 적과 코인을 정리하고 처음 페이즈부터
    public void StartStage(int index)
    {
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("enermy")) Destroy(enemy);
        foreach (GameObject coin in GameObject.FindGameObjectsWithTag("coin")) Destroy(coin);

        stageIndex = Mathf.Clamp(index, 0, stages.Length - 1);
        killedEnemy = 0;
        paze = 1;
        bossSpawned = false;
        bossCleared = false;
        spawnedEnemys = 0;
        spawningEnabled = true;
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

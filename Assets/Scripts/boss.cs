using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bosss : MonoBehaviour
{
    public float speed = 0.2f;
    public int setEnemyHP = 300;
    public float EnemyHealth;


    private Transform player;
    private Vector3 move;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public int KilledEnemy;

    public GameObject coin;

    public int hitCount = 0;

    [Header("부하 소환")]
    // 이만큼 맞을 때마다 부하를 추가로 소환
    public int hitsPerSummon = 15;
    public int summonOnHitCount = 2;
    // 일정 시간마다 소환 (체력이 절반 아래면 더 자주, 더 많이)
    public float summonInterval = 5f;
    public int summonCount = 3;
    public float enragedSummonInterval = 3f;
    public int enragedSummonCount = 4;

    [Header("보상 / 피해")]
    public float contactDamage = 25f;
    public int expReward = 200;
    public int coinDrop = 50;

    [Header("넉백 저항")]
    // 총알 넉백을 이 비율만큼만 받음
    public float knockBackTaken = 0.25f;

    [Header("스킬")]
    public int bossKind = 0;            // 0 = 리치 왕, 1 = 지옥의 군주
    // 스킬 시전 중에는 걸어서 움직이지 않음
    [HideInInspector] public bool casting;

    float summonTimer;
    EnemySpawner spawner;

    public bool Enraged => EnemyHealth <= setEnemyHP * 0.5f;

    // 킹 슬라임 (bossKind 2): 쓰러질 때마다 분열 (1마리 → 2마리 → 3마리)
    [HideInInspector] public int slimeGen = 1;
    // 분열한 슬라임마다 고정 번호 (보스바 줄 · 이름 「킹 슬라임 n」이 끝까지 이 개체를 따라감)
    [HideInInspector] public int barSlot;
    static readonly List<bosss> slimes = new List<bosss>();
    static int gen2Deaths;
    static int gen3Spawned;
    static bool slimeBossCleared;    // 마지막 두 마리가 거의 같이 쓰러져도 보스 처치는 한 번만
    const int Gen2Hp = 900;            // 3장 마지막 보스인데 2장 보스보다 약해 금방 녹던 것 (예전 1400 · 600 · 280)
    const int Gen3Hp = 450;
    // 난이도 배율 (첫 킹 슬라임이 나올 때 정해서 분열한 슬라임에도 같게)
    static float slimeMul = 1f;
    static int ScaledHp(int hp) => Mathf.RoundToInt(hp * slimeMul);
    // 난이도 배율을 이미 적용했는지 (분열 복제에는 적용된 값이 넘어감)
    [HideInInspector] public bool difficultyApplied;
    bool IsSlime => bossKind == 2;

    // 남은 체력 합계 (아직 분열하지 않은 몫 포함)
    static int SlimeRemaining()
    {
        int sum = 0;
        bool gen1Alive = false;
        foreach (bosss s in slimes)
        {
            if (s == null || s.isDead) continue;
            sum += Mathf.CeilToInt(Mathf.Max(0f, s.EnemyHealth));
            if (s.slimeGen == 1) gen1Alive = true;
        }
        if (gen1Alive) sum += 2 * ScaledHp(Gen2Hp);
        sum += (3 - gen3Spawned) * ScaledHp(Gen3Hp);
        return sum;
    }

    // 살아 있는 킹 슬라임 (위쪽 보스 체력바가 분열한 슬라임마다 따로 그림)
    public static void CollectLiveSlimes(List<bosss> into)
    {
        into.Clear();
        foreach (bosss s in slimes) if (s != null && !s.isDead) into.Add(s);
        into.Sort((a, b) => a.barSlot.CompareTo(b.barSlot));
    }

    void Split()
    {
        int count = slimeGen == 1 ? 2 : (gen2Deaths++ == 0 ? 2 : 1);
        // 대점프로 공중에 떠 있다 쓰러지면 떨어질 자리에서 갈라짐 (구르는 중이면 기운 채로 복제되지 않게 똑바로)
        BossSkills mySkills = GetComponent<BossSkills>();
        Vector3 at = mySkills != null && mySkills.airborne ? mySkills.landing : transform.position;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = (Vector3)(Random.insideUnitCircle.normalized * 3f);
            GameObject clone = Instantiate(gameObject, Hostile.ClampArena(at + offset), Quaternion.identity);
            clone.transform.localScale = transform.localScale * 0.75f;
            BossSkills cloneSkills = clone.GetComponent<BossSkills>();
            if (cloneSkills != null) cloneSkills.airborne = false;
            bosss b = clone.GetComponent<bosss>();
            b.barSlot = FreeSlot();
            // 바로 목록에 넣음: Start(다음 프레임) 전에 다른 슬라임이 쓰러지면 남은 체력이 0으로 계산돼 보스바가 꺼지던 문제
            slimes.Add(b);
            b.slimeGen = slimeGen + 1;
            b.setEnemyHP = ScaledHp(slimeGen == 1 ? Gen2Hp : Gen3Hp);
            b.EnemyHealth = b.setEnemyHP;
            b.casting = false;
            b.coinDrop = slimeGen == 1 ? 15 : 10;
            b.expReward = expReward / 2;
            b.summonCount = 1;
            b.enragedSummonCount = 2;
            clone.GetComponent<SpriteRenderer>().color = Color.white;
            if (slimeGen + 1 == 3) gen3Spawned++;
            Fx.Spawn("fx_puddle", clone.transform.position, 3f, new Color(0.55f, 1f, 0.35f), 12f);
        }
        Fx.Spawn("fx_shock", transform.position, 9f, new Color(0.55f, 1f, 0.35f), 16f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner(slimeGen == 1 ? Loc.T("킹 슬라임이 둘로 갈라졌다!") : Loc.T("슬라임이 또 갈라진다!"), 2f);
    }
    public bool IsDead => isDead;

    // 살아 있는 슬라임이 쓰지 않는 가장 작은 번호
    static int FreeSlot()
    {
        for (int n = 0; ; n++)
        {
            bool used = false;
            foreach (bosss s in slimes) if (s != null && !s.isDead && s.barSlot == n) { used = true; break; }
            if (!used) return n;
        }
    }

    void Summon(int count)
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.SummonMinions(transform.position, count);
    }

    // 생긴 프레임에 맞아도 (분열한 슬라임 등) 색 바꾸기 · 피해 숫자가 깨지지 않게 미리
    void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();
    // Start 가 끝나 체력이 정해졌는지 (그 전에 맞으면 체력 0 에서 깎여 바로 죽을 수 있음)
    bool ready;

    void Start()
    {
        SpriteOutline.Enemy(gameObject);
        if (!difficultyApplied)
        {
            difficultyApplied = true;
            // 2.1.1~: 장 순서가 바뀌어도 그 자리(몇 번째 장)의 원래 세기로 (Chapters)
            int chapter = Chapters.CurrentStage;
            float hpMul = GameMode.BossHpMul * Chapters.BossHpMul(chapter), rw = Chapters.RewardMul(chapter);
            if (IsSlime && slimeGen == 1) slimeMul = hpMul;
            setEnemyHP = Mathf.Max(1, Mathf.RoundToInt(setEnemyHP * hpMul));
            // 보스는 처치 수와 상관없이 정해진 수만 나오므로 고정 배율
            expReward = Mathf.RoundToInt(expReward * GameMode.FixedRewardMul * rw);
            coinDrop = Mathf.RoundToInt(coinDrop * GameMode.FixedRewardMul * rw);
        }
        EnemyHealth = setEnemyHP;

        // 플레이어 찾기
        player = FindFirstObjectByType<PlayerController>().transform;

        // 컴포넌트 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        KilledEnemy = 0;
        bossbar = FindFirstObjectByType<bossbar>();
        // 처음 등장: 게임을 멈추고 카메라가 보스 쪽으로 이동한 뒤 대사 (보스마다 한 판에 한 번)
        if (!IsSlime || slimeGen == 1) StoryDirector.PlayBossIntro(transform, bossKind);
        // 분열로 복제된 슬라임은 스킬 컴포넌트를 이미 가지고 있음
        BossSkills skills = GetComponent<BossSkills>();
        if (skills == null) skills = gameObject.AddComponent<BossSkills>();
        skills.kind = bossKind;
        if (GetComponent<BossUltimate>() == null) gameObject.AddComponent<BossUltimate>();     // 필살기 "결계"
        if (IsSlime)
        {
            if (slimeGen == 1)
            {
                slimes.Clear();
                gen2Deaths = 0;
                gen3Spawned = 0;
                slimeBossCleared = false;
            }
            if (!slimes.Contains(this)) slimes.Add(this);       // 분열한 슬라임은 Split 에서 이미 넣음
        }
        ready = true;
    }
    bossbar bossbar;
    void Update()
    {
        // 죽었으면 아무것도 하지 않음
        if (isDead) return;

        bossbar.bossKind = bossKind;
        // 킹 슬라임도 개체마다 자기 체력만 (갈라질 몫을 합치지 않음 · 갈라진 뒤에는 bossbar 가 슬라임마다 따로 그림)
        bossbar.MaxHealth = setEnemyHP;
        bossbar.NowHealth = Mathf.CeilToInt(EnemyHealth);

        if (player == null) return;

        // 주기적으로 부하 소환 (필살기 결계 중에는 쉼)
        if (!BossUltimate.Active) summonTimer += Time.deltaTime;
        if (summonTimer >= (Enraged ? enragedSummonInterval : summonInterval))
        {
            summonTimer = 0f;
            Summon(Enraged ? enragedSummonCount : summonCount);
        }

        // 적 → 플레이어 방향 계산
        move = (EnermyController.Decoy != null ? EnermyController.Decoy.position : player.position) - transform.position;

        // z축 제거
        move.z = 0;

        // 방향의 길이를 1로 맞춤
        move = move.normalized;

        // 좌우 방향 변경
        if (move.x < 0) spriteRenderer.flipX = true;
        else if (move.x > 0) spriteRenderer.flipX = false;
    }

    void FixedUpdate()
    {
        // 죽지 않았을 때만 이동
        if (!isDead && player != null && !casting) transform.Translate(move * speed * EnermyController.GlobalSpeedMultiplier * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage, float knockBack, Vector3 dir)
    {
        if (isDead || !ready) return;

        // 치명타: 보스도 모든 캐릭터의 공격 · 스킬에 치명타를 맞음 (거너 평타 총알은 쏠 때 이미 굴림)
        if (!SpecialAbilities.CritRolled && SpecialAbilities.SharedInstance != null) damage = SpecialAbilities.SharedInstance.RollCrit(damage);

        damage = SignatureSkills.Outgoing(gameObject, damage);     // 고유 스킬 피해 배율 (거인 사냥꾼 · 표식 …)

        // 체력 감소
        RunStats.Dealt(Mathf.Min(damage, Mathf.Max(0f, EnemyHealth)));      // 이번 판 출처별 피해 (넘친 피해는 빼고)
        EnemyHealth -= damage;
        SignatureSkills.Hit(gameObject, damage, EnemyHealth <= 0);
        if (EnemyHealth > 0) BossUltimate.OnBossHit(this, damage);       // 필살기 게이지 (센 한 방일수록 많이)
        DamagePopup.Show(transform, damage, spriteRenderer, true);
        SkinFx.OnEnemyHit(transform.position);      // 이펙트 스킨 명중 불꽃

        transform.position += dir * knockBack * knockBackTaken;

        // 피격 애니메이션
        //animator.SetTrigger("hit");

        // 피격 색상 효과
        flashUntil = Time.time + 0.1f;


        hitCount++;
        if (hitCount >= hitsPerSummon)
        {
            Summon(summonOnHitCount);
            hitCount = 0;
        }

        // 체력이 0 이하이면 사망
        if (EnemyHealth <= 0) Die(1);
    }

    // 피격 번쩍임: 스킬 예고(BossSkills.Windup 등)가 매 프레임 몸 색을 바꿔도 덮이지 않게 맨 마지막(LateUpdate)에 칠함
    // (예전엔 맞은 순간 한 번만 빨갛게 칠해서, 스킬을 준비하던 슬라임은 맞아도 번쩍이지 않았음)
    float flashUntil;
    bool flashing;

    void LateUpdate()
    {
        if (isDead) { flashing = false; return; }
        if (Time.time < flashUntil)
        {
            spriteRenderer.color = Color.red;
            flashing = true;
        }
        else if (flashing)
        {
            flashing = false;
            spriteRenderer.color = Color.white;
        }
    }
    PlayerController playerC;

    void Die(int a)
    {
        if (isDead) return;

        isDead = true;

        // 킹 슬라임은 분열하고, 마지막 한 마리가 쓰러질 때만 보스전이 끝남
        bool lastOne = true;
        if (IsSlime)
        {
            if (a == 1 && slimeGen < 3) Split();
            lastOne = SlimeRemaining() <= 0;
        }
        if (lastOne) bossbar.bossSpawn = false;
        spriteRenderer.color = Color.white;
        if (a == 1)
        {
            
            playerC = Cache<PlayerController>.Get;
            LevelShop levelS = Cache<LevelShop>.Get;
            Level lv = Cache<Level>.Get;

            float exp = expReward * (lv != null ? lv.bonusEXP : 1f);
            if (levelS != null) levelS.GainExp(playerC, exp);
            else playerC.nowEXP += exp;

            Point point = Cache<Point>.Get;

            if (point != null) point.AddPoint(10);

            // 영혼 조각: 보스전이 끝나는 마지막 한 마리는 크게, 슬라임 분열체는 조금
            if (lastOne) SoulShards.Add(SoulShards.ForBoss(expReward), transform.position, true);
            else SoulShards.Add(Mathf.Max(1, Mathf.RoundToInt(expReward / 30f)), transform.position, false);
        }

        if (animator != null) animator.SetTrigger("death");

        StartCoroutine(Death(a));
    }

    public GameObject hp;
    [Header("힐팩")]
    public int chanceofHP = 30;

    IEnumerator Death(int a)
    {
        // 죽음 애니메이션 시간
        yield return new WaitForSeconds(1f);
        EnemySpawner enemySpawner = Cache<EnemySpawner>.Get;
        // 총알에 죽었을 때만 코인 생성
        if (a == 1)
        {


            enemySpawner.killedEnemy++;
            if (Random.Range(0, 100) < chanceofHP) Instantiate(hp, transform.position, Quaternion.identity);
            for (int i = 0; i < coinDrop; i++)
            {
                float rx = Random.Range(-5f, 5f);
                float ry = Random.Range(-5f, 5f);
                Vector2 drop = new Vector2(transform.position.x + rx, transform.position.y + ry);
                CoinTag.Register(Instantiate(coin, drop, Quaternion.identity));
            }
        }

        // 스테이지 진행 (신전 문 열기 등) - 슬라임은 모두 쓰러졌을 때만
        bool allDown = !IsSlime || SlimeRemaining() <= 0;
        if (IsSlime)
        {
            slimes.Remove(this);
            if (allDown) { if (slimeBossCleared) allDown = false; else slimeBossCleared = true; }
        }
        if (enemySpawner != null && allDown) enemySpawner.OnBossDefeated();

        Destroy(gameObject);
    }



}

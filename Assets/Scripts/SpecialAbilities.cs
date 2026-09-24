using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialKind { Weapon, Skill, Passive }

// 특수 능력 하나의 정의
[System.Serializable]
public class SpecialDef
{
    public string name;
    public SpecialKind kind;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
}

// 지옥 입장 때 고르는 특수 능력 (무기 / 스킬 / 패시브)
public class SpecialAbilities : MonoBehaviour
{
    public const int ShotgunId = 0, SniperId = 1, DualId = 2, FlameId = 3, SeekerId = 4, ChainId = 5, ScytheId = 6, GrenadeId = 7;
    public const int DashId = 8, FireZoneId = 9, TimeWarpId = 10, PactId = 11, SoulBurstId = 12, SkeletonsId = 13, MirrorId = 14, HookId = 15;
    public const int OrbsId = 16, ThornsId = 17, CurseId = 18, UndyingId = 19;

    [Header("능력 목록 (순서 = ID)")]
    public SpecialDef[] abilities;

    [Header("효과용 스프라이트")]
    public Sprite glowSprite;       // 폭발, 장판, 빛
    public Sprite swirlSprite;      // 영혼 구체
    public Sprite allySprite;       // 아군 해골
    public Sprite panelSprite;
    public Sprite barFillSprite;
    public TMP_FontAsset font;
    public Material fontMaterial;

    // 고른 능력들 (최대 3개)
    readonly List<int> equipped = new List<int>();
    readonly List<int> weapons = new List<int>();
    readonly List<int> skills = new List<int>();
    public IReadOnlyList<int> EquippedIds => equipped;

    // 스킬 키: 고른 순서대로 E, F, Space
    static readonly KeyCode[] SkillKeys = { KeyCode.E, KeyCode.F, KeyCode.Space };
    static readonly string[] SkillKeyNames = { "E", "F", "Space" };

    // 무기: Q로 기본 권총 → 무기1 → 무기2 순서로 교체 (-1 = 기본 권총)
    int weaponIndex = -1;
    public bool WeaponActive => weaponIndex >= 0 && weaponIndex < weapons.Count;
    int CurrentWeapon => WeaponActive ? weapons[weaponIndex] : -1;

    PlayerController player;
    float nextFire;
    readonly Dictionary<int, float> cooldownUntil = new Dictionary<int, float>();
    readonly Dictionary<int, float> cooldownLength = new Dictionary<int, float>();
    int souls;
    bool undyingUsed;
    float heat;
    bool overheated;
    float sniperCharge = -1f;
    bool dualToggle;
    GameObject activeScythe;
    readonly List<Transform> orbs = new List<Transform>();
    readonly Dictionary<Collider2D, float> orbHitTimes = new Dictionary<Collider2D, float>();
    Material lineMaterial;

    // HUD: 고른 능력마다 한 줄
    class HudRow
    {
        public int id;
        public TextMeshProUGUI name;
        public TextMeshProUGUI info;
        public Image bar;
    }
    RectTransform hud;
    readonly List<HudRow> hudRows = new List<HudRow>();

    void Awake()
    {
        // 씬을 다시 불러와도 정적 상태가 남지 않도록
        EnermyController.GlobalSpeedMultiplier = 1f;
        EnermyController.Decoy = null;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.special = this;
        EnermyController.Killed += OnEnemyKilled;
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        BuildHud();
    }

    void OnDestroy()
    {
        EnermyController.Killed -= OnEnemyKilled;
        EnermyController.GlobalSpeedMultiplier = 1f;
        EnermyController.Decoy = null;
    }

    public void Equip(IEnumerable<int> ids)
    {
        foreach (int id in ids)
        {
            if (id < 0 || id >= abilities.Length || equipped.Contains(id)) continue;
            equipped.Add(id);
            if (abilities[id].kind == SpecialKind.Weapon) weapons.Add(id);
            if (abilities[id].kind == SpecialKind.Skill) skills.Add(id);
            if (id == OrbsId) SpawnOrbs(3);
        }
        // 무기를 골랐다면 바로 꺼내 들고 시작
        weaponIndex = weapons.Count > 0 ? 0 : -1;
        RebuildHudRows();
    }

    public bool Has(int id) => equipped.Contains(id);

    // ================================================================= update
    void Update()
    {
        if (player == null || equipped.Count == 0 || Time.timeScale == 0f) { UpdateHud(); return; }

        if (weapons.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                // 기본 권총(-1) → 무기1 → 무기2 → 기본 권총 ...
                weaponIndex = weaponIndex + 1 >= weapons.Count ? -1 : weaponIndex + 1;
                sniperCharge = -1f;
            }
            player.ammoTextOverride = null;
            if (WeaponActive) UpdateWeapon();
        }

        for (int i = 0; i < skills.Count && i < SkillKeys.Length; i++)
        {
            int id = skills[i];
            if (Input.GetKeyDown(SkillKeys[i]) && Time.time >= CooldownUntil(id) && !player.IsSkillUsing) UseSkill(id);
        }

        if (Has(OrbsId)) UpdateOrbs();
        if (CurrentWeapon != FlameId) heat = Mathf.Max(0f, heat - Time.deltaTime * 0.35f);

        UpdateHud();
    }

    Vector3 MouseWorld()
    {
        Vector3 m = player.MainCamera.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;
        return m;
    }

    float Damage => player.damage * player.damageMultiplier;
    float Interval(float mul) => player.ShootSpeed * mul / player.fireRateMultiplier;
    bool OverUI => UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

    // ================================================================= weapons
    void UpdateWeapon()
    {
        bool down = Input.GetMouseButtonDown(0) && !OverUI;
        bool held = Input.GetMouseButton(0) && !OverUI;
        bool up = Input.GetMouseButtonUp(0);

        switch (CurrentWeapon)
        {
            case ShotgunId:
                if (down && Ready()) FireShotgun();
                break;
            case SniperId:
                if (down && Ready()) sniperCharge = 0f;
                if (sniperCharge >= 0f)
                {
                    sniperCharge = Mathf.Min(1.2f, sniperCharge + Time.deltaTime);
                    player.ammoTextOverride = "충전 " + Mathf.RoundToInt(sniperCharge / 1.2f * 100f) + "%";
                    if (up) { FireSniper(sniperCharge / 1.2f); sniperCharge = -1f; }
                }
                break;
            case DualId:
                if (held && Ready()) FireDual();
                break;
            case FlameId:
                UpdateFlame(held);
                break;
            case SeekerId:
                if (held && Ready()) FireSeeker();
                break;
            case ChainId:
                if (down && Ready()) FireChain();
                break;
            case ScytheId:
                if (down && activeScythe == null && !player.IsSkillUsing) FireScythe();
                player.ammoTextOverride = activeScythe == null ? "낫 준비" : "낫 회수 중";
                break;
            case GrenadeId:
                if (down && Ready()) FireGrenade();
                break;
        }
    }

    bool Ready() => player.CanShoot && Time.time >= nextFire;

    // 공통: 발사 준비 (방향, 소리, 탄약, 탄창 저주)
    Vector2 BeginShot(float intervalMul, int ammoCost, out Vector3 start, out bool cursed)
    {
        Vector3 target = MouseWorld();
        player.FaceTowards(target);
        start = player.MuzzlePosition;
        nextFire = Time.time + Interval(intervalMul);
        cursed = IsLastBulletCursed(player.NowBullet);
        player.NowBullet = Mathf.Max(0, player.NowBullet - ammoCost);
        if (player.shotSound != null && player.TryGetComponent(out AudioSource a)) a.PlayOneShot(player.shotSound);
        return ((Vector2)(target - start)).normalized;
    }

    Bullet Shot(Vector3 start, Vector2 dir, float dmg, int pene, float knockRate, bool cursed, Color tint, float speedMul = 1f, float scale = 1f, float skillCharge = 1f)
    {
        Bullet b = player.CreateBullet(start, dir, dmg * (cursed ? 3f : 1f), pene, 0, false, knockRate);
        if (b == null) return null;
        b.skillCharge = skillCharge;
        b.speed *= speedMul;
        b.transform.localScale *= scale;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = tint;
        CurseBullet(b, cursed);
        return b;
    }

    // 총알 없이 앞쪽 부채꼴 안의 적을 즉시 타격하는 근거리 폭발
    const float ShotgunRange = 6f;
    const float ShotgunHalfAngle = 30f;

    void FireShotgun()
    {
        Vector2 dir = BeginShot(1.4f, 1, out Vector3 start, out bool cursed);
        float dmg = Damage * 2.5f * (cursed ? 3f : 1f);
        bool hitAny = false;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(start, ShotgunRange))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector2 to = (Vector2)(c.transform.position - start);
            if (Vector2.Angle(dir, to) > ShotgunHalfAngle) continue;

            Specials.Damage(c.gameObject, dmg, to.normalized, 2.5f);
            hitAny = true;
            if (cursed) Explode(c.transform.position, 2.5f, Damage * 1.5f, 1.5f, new Color(1f, 0.3f, 0.2f, 0.85f));
        }

        // 부채꼴을 따라 불꽃이 퍼지는 연출
        for (int i = -2; i <= 2; i++)
        {
            Vector2 d = Quaternion.Euler(0, 0, i * ShotgunHalfAngle / 2f) * dir;
            for (int k = 1; k <= 3; k++)
                Flash(start + (Vector3)(d * ShotgunRange * k / 3.5f), 1.2f + k * 0.6f, new Color(1f, 0.55f, 0.2f, 0.8f), 0.18f + k * 0.04f);
        }

        // 스킬 게이지는 맞힌 경우에만 조금 참
        if (hitAny)
        {
            SkillGauge gauge = FindFirstObjectByType<SkillGauge>();
            if (gauge != null) gauge.AddSkillPoint(1f);
        }
    }

    void FireSniper(float charge)
    {
        if (!player.CanShoot) return;
        Vector2 dir = BeginShot(1.8f, 1, out Vector3 start, out bool cursed);
        Shot(start, dir, Damage * Mathf.Lerp(1.5f, 3f, charge), 999, 1.5f, cursed, new Color(0.5f, 0.95f, 1f), 1.6f, 1.4f, 1.5f);
    }

    void FireDual()
    {
        dualToggle = !dualToggle;
        Vector2 dir = BeginShot(0.5f, dualToggle ? 1 : 0, out Vector3 start, out bool cursed);
        Vector3 side = new Vector3(-dir.y, dir.x) * (dualToggle ? 0.35f : -0.35f);
        Shot(start + side, dir, Damage * 0.55f, player.pene, 0.6f, cursed, new Color(1f, 0.85f, 0.5f), 1f, 1f, 0.6f);
    }

    void UpdateFlame(bool held)
    {
        if (overheated && heat <= 0.3f) overheated = false;
        if (held && !overheated && !player.IsSkillUsing && Time.time >= nextFire)
        {
            Vector3 target = MouseWorld();
            player.FaceTowards(target);
            Vector3 start = player.MuzzlePosition;
            Vector2 dir = Quaternion.Euler(0, 0, Random.Range(-8f, 8f)) * ((Vector2)(target - start)).normalized;
            nextFire = Time.time + 0.07f / player.fireRateMultiplier;
            Bullet b = Shot(start, dir, Damage * 0.25f, 99, 0.2f, false, new Color(1f, 0.55f, 0.15f, 0.9f), 0.2f, 1.6f, 0.05f);
            if (b != null)
            {
                b.lifetime = 0.35f;
                float burnDps = Damage * 0.3f;
                b.onHitEnemy += (bullet, col) => Burn.Apply(col.gameObject, burnDps, 2f);
            }
            heat += 0.035f;
            if (heat >= 1f) { heat = 1f; overheated = true; }
        }
        else
        {
            heat = Mathf.Max(0f, heat - Time.deltaTime * 0.35f);
        }
        player.ammoTextOverride = overheated ? "과열!" : "열기 " + Mathf.RoundToInt(heat * 100f) + "%";
    }

    void FireSeeker()
    {
        Vector2 dir = BeginShot(0.9f, 1, out Vector3 start, out bool cursed);
        Bullet b = Shot(start, dir, Damage * 0.7f, 1, 0.5f, cursed, new Color(0.7f, 0.5f, 1f), 0.3f, 1.2f, 0.8f);
        if (b != null) b.gameObject.AddComponent<Homing>().turnSpeed = 360f;
    }

    void FireChain()
    {
        Vector2 dir = BeginShot(1f, 1, out Vector3 start, out bool cursed);
        Bullet b = Shot(start, dir, Damage, 1, 1f, cursed, new Color(0.6f, 0.9f, 1f));
        if (b != null)
        {
            float dmg = Damage;
            b.onHitEnemy += (bullet, col) => ChainLightning(col.transform.position, col, dmg * 0.8f, 4);
        }
    }

    void FireScythe()
    {
        Vector3 target = MouseWorld();
        player.FaceTowards(target);
        Bullet b = Shot(player.MuzzlePosition, ((Vector2)(target - player.MuzzlePosition)).normalized, Damage * 1.5f, 9999, 1.2f, false,
                        new Color(0.75f, 0.45f, 1f), 0f, 2.5f, 0.3f);
        if (b == null) return;
        b.lifetime = 5f;
        Scythe s = b.gameObject.AddComponent<Scythe>();
        s.owner = player.transform;
        s.direction = ((Vector2)(target - player.MuzzlePosition)).normalized;
        activeScythe = b.gameObject;
    }

    void FireGrenade()
    {
        Vector2 dir = BeginShot(1.8f, 2, out Vector3 start, out bool cursed);
        Vector3 target = MouseWorld();
        if (Vector2.Distance(start, target) > 14f) target = start + (Vector3)(dir * 14f);
        GameObject g = MakeSprite("LavaGrenade", glowSprite, start, 1.2f, new Color(1f, 0.45f, 0.1f), "Effect", 5);
        Grenade gr = g.AddComponent<Grenade>();
        gr.owner = this;
        gr.target = target;
        gr.damage = Damage * 2f * (cursed ? 3f : 1f);
    }

    // ================================================================= skills
    void UseSkill(int id)
    {
        switch (id)
        {
            case DashId: StartCoroutine(Dash()); StartCooldown(id, 3f); break;
            case FireZoneId: SpawnZone(MouseWorld(), 3f, 4f, Damage * 0.75f, new Color(1f, 0.4f, 0.1f, 0.8f)); StartCooldown(id, 12f); break;
            case TimeWarpId: StartCoroutine(TimeWarp()); StartCooldown(id, 20f); break;
            case PactId: StartCoroutine(Pact()); StartCooldown(id, 25f); break;
            case SoulBurstId:
                if (souls < 5) return;
                Explode(player.transform.position, 7f, Damage * (1.5f + souls * 0.25f), 3f, new Color(0.6f, 0.95f, 1f, 0.9f));
                souls = 0;
                StartCooldown(id, 5f);
                break;
            case SkeletonsId: SummonSkeletons(3); StartCooldown(id, 15f); break;
            case MirrorId: StartCoroutine(Mirror()); StartCooldown(id, 12f); break;
            case HookId: Hook(); StartCooldown(id, 4f); break;
        }
    }

    float CooldownUntil(int id) => cooldownUntil.TryGetValue(id, out float t) ? t : 0f;

    void StartCooldown(int id, float seconds)
    {
        cooldownLength[id] = seconds;
        cooldownUntil[id] = Time.time + seconds;
    }

    IEnumerator Dash()
    {
        Vector3 start = player.transform.position;
        Vector3 dir = (MouseWorld() - start).normalized;
        Vector3 end = ClampToArena(start + dir * 6f);
        player.GrantInvincibility(0.35f);
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(start, end, t / 0.15f);
            yield return null;
        }
        player.transform.position = end;
    }

    IEnumerator TimeWarp()
    {
        EnermyController.GlobalSpeedMultiplier = 0.5f;
        Flash(player.transform.position, 12f, new Color(0.5f, 0.8f, 1f, 0.5f), 0.5f);
        yield return new WaitForSeconds(5f);
        EnermyController.GlobalSpeedMultiplier = 1f;
    }

    IEnumerator Pact()
    {
        player.PlayerHealth = Mathf.Max(1f, player.PlayerHealth - player.PlayerMaxHealth * 0.2f);
        player.damageMultiplier = 2f;
        player.fireRateMultiplier = 1.5f;
        Flash(player.transform.position, 5f, new Color(1f, 0.15f, 0.2f, 0.7f), 0.4f);
        yield return new WaitForSeconds(8f);
        player.damageMultiplier = 1f;
        player.fireRateMultiplier = 1f;
    }

    IEnumerator Mirror()
    {
        SpriteRenderer body = player.GetComponent<SpriteRenderer>();
        GameObject decoy = MakeSprite("MirrorDecoy", body.sprite, player.transform.position, 1f, new Color(0.55f, 0.9f, 1f, 0.75f), "Character", 0);
        decoy.transform.localScale = player.transform.lossyScale;
        decoy.GetComponent<SpriteRenderer>().flipX = body.flipX;
        EnermyController.Decoy = decoy.transform;
        player.bodyAlpha = 0.4f;
        yield return new WaitForSeconds(3f);
        EnermyController.Decoy = null;
        player.bodyAlpha = 1f;
        Destroy(decoy);
    }

    void Hook()
    {
        Vector3 from = player.transform.position;
        Vector2 dir = ((Vector2)(MouseWorld() - from)).normalized;
        const float range = 14f;

        Collider2D enemy = null;
        float enemyDist = range;
        float wallDist = range;
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(from, 0.6f, dir, range))
        {
            if (hit.collider.CompareTag("enermy") || hit.collider.CompareTag("boss"))
            {
                if (hit.distance < enemyDist) { enemyDist = hit.distance; enemy = hit.collider; }
            }
            else if (hit.collider.CompareTag("Wall") && hit.distance < wallDist && hit.distance > 0.1f)
            {
                wallDist = hit.distance;
            }
        }

        if (enemy != null && enemyDist < wallDist)
        {
            DrawLine(from, enemy.transform.position, new Color(0.8f, 0.1f, 0.15f), 0.2f);
            Specials.Damage(enemy.gameObject, Damage * 1.5f, Vector3.zero, 0f);
            if (enemy.CompareTag("enermy")) StartCoroutine(Pull(enemy.transform, from + (Vector3)dir * 2.5f));
        }
        else
        {
            Vector3 end = ClampToArena(from + (Vector3)dir * Mathf.Max(0f, wallDist - 1.5f));
            DrawLine(from, from + (Vector3)dir * wallDist, new Color(0.8f, 0.1f, 0.15f), 0.2f);
            StartCoroutine(PullPlayer(end));
        }
    }

    IEnumerator Pull(Transform target, Vector3 to)
    {
        Vector3 start = target.position;
        for (float t = 0f; t < 0.15f && target != null; t += Time.deltaTime)
        {
            target.position = Vector3.Lerp(start, to, t / 0.15f);
            yield return null;
        }
    }

    IEnumerator PullPlayer(Vector3 to)
    {
        player.GrantInvincibility(0.3f);
        Vector3 start = player.transform.position;
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(start, to, t / 0.2f);
            yield return null;
        }
        player.transform.position = to;
    }

    void SummonSkeletons(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = player.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 2f);
            GameObject s = MakeSprite("AllySkeleton", allySprite, pos, 1f, new Color(0.6f, 0.95f, 1f), "Character", 1);
            s.transform.localScale = Vector3.one * 1.1f;
            AllySkeleton a = s.AddComponent<AllySkeleton>();
            a.owner = this;
            a.damage = Damage * 3f;
        }
    }

    // ================================================================= passives
    void SpawnOrbs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject o = MakeSprite("GuardianSoul", swirlSprite, player.transform.position, 0.45f, new Color(0.55f, 0.95f, 1f), "Effect", 2);
            orbs.Add(o.transform);
        }
    }

    void UpdateOrbs()
    {
        float baseAngle = Time.time * 180f;
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null) continue;
            float a = (baseAngle + 360f / orbs.Count * i) * Mathf.Deg2Rad;
            Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a) - 0.4f, 0f) * 3.2f;
            orbs[i].position = pos;
            orbs[i].Rotate(0f, 0f, -540f * Time.deltaTime);

            foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, 0.9f))
            {
                if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                if (orbHitTimes.TryGetValue(c, out float last) && Time.time - last < 0.5f) continue;
                orbHitTimes[c] = Time.time;
                Specials.Damage(c.gameObject, Damage * 0.8f, (c.transform.position - player.transform.position).normalized, 0.8f);
            }
        }
    }

    // 플레이어가 맞았을 때 (복수의 가시)
    public void OnPlayerHurt()
    {
        if (!Has(ThornsId) || player == null) return;
        Explode(player.transform.position, 4f, Damage * 3f, 2f, new Color(0.9f, 0.2f, 0.3f, 0.85f));
    }

    // 불사의 맹세: 한 판에 한 번
    public bool TryUndying()
    {
        if (!Has(UndyingId) || undyingUsed) return false;
        undyingUsed = true;
        Flash(player.transform.position, 6f, new Color(1f, 0.9f, 0.5f, 0.8f), 0.6f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner("불사의 맹세가 발동했다!", 2f);
        return true;
    }

    // 탄창 저주: 탄창의 마지막 한 발
    public bool IsLastBulletCursed(int bulletsBeforeShot) => Has(CurseId) && bulletsBeforeShot == 1;

    public void CurseBullet(Bullet b, bool cursed)
    {
        if (b == null || !cursed) return;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(1f, 0.3f, 0.3f);
        b.transform.localScale *= 1.5f;
        float dmg = Damage * 1.5f;
        b.onHitEnemy += (bullet, col) => Explode(col.transform.position, 2.5f, dmg, 1.5f, new Color(1f, 0.3f, 0.2f, 0.85f));
    }

    void OnEnemyKilled(Vector3 pos)
    {
        if (Has(SoulBurstId)) souls = Mathf.Min(40, souls + 1);
    }

    // ================================================================= shared effects
    public void Explode(Vector3 pos, float radius, float damage, float knock, Color color)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector3 dir = (c.transform.position - pos).normalized;
            Specials.Damage(c.gameObject, damage, dir, knock);
        }
        Flash(pos, radius * 2f, color, 0.3f);
    }

    public void SpawnZone(Vector3 pos, float radius, float duration, float tickDamage, Color color)
    {
        GameObject z = MakeSprite("DamageZone", glowSprite, pos, radius * 2f / 8f, color, "Background", 7);
        DamageZone dz = z.AddComponent<DamageZone>();
        dz.radius = radius;
        dz.duration = duration;
        dz.tickDamage = tickDamage;
    }

    void ChainLightning(Vector3 from, Collider2D first, float damage, int jumps)
    {
        HashSet<Collider2D> hit = new HashSet<Collider2D> { first };
        Vector3 current = from;
        for (int j = 0; j < jumps; j++)
        {
            Collider2D next = null;
            float best = 6f;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(current, 6f))
            {
                if (hit.Contains(c) || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                float d = Vector2.Distance(current, c.transform.position);
                if (d < best) { best = d; next = c; }
            }
            if (next == null) break;
            hit.Add(next);
            DrawLine(current, next.transform.position, new Color(0.6f, 0.9f, 1f), 0.15f);
            Specials.Damage(next.gameObject, damage, Vector3.zero, 0f);
            current = next.transform.position;
            damage *= 0.8f;
        }
    }

    public void Flash(Vector3 pos, float size, Color color, float duration)
    {
        GameObject f = MakeSprite("Flash", glowSprite, pos, size / 8f, color, "Effect", 3);
        f.AddComponent<FadeOut>().duration = duration;
    }

    void DrawLine(Vector3 a, Vector3 b, Color color, float duration)
    {
        GameObject go = new GameObject("Line");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = 0.25f;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.sortingLayerName = "Effect";
        Destroy(go, duration);
    }

    public static GameObject MakeSprite(string name, Sprite sprite, Vector3 pos, float scale, Color color, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return go;
    }

    Vector3 ClampToArena(Vector3 p)
    {
        EnemySpawner sp = FindFirstObjectByType<EnemySpawner>();
        if (sp == null) return p;
        return new Vector3(Mathf.Clamp(p.x, sp.spawnAreaMin.x, sp.spawnAreaMax.x), Mathf.Clamp(p.y, sp.spawnAreaMin.y, sp.spawnAreaMax.y + 1.5f), 0f);
    }

    public void ScytheReturned() => activeScythe = null;

    // ================================================================= HUD (탄약 패널 왼쪽)
    void BuildHud()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("SpecialSlot", typeof(RectTransform), typeof(Image));
        hud = go.GetComponent<RectTransform>();
        hud.SetParent(canvas.transform, false);
        Transform ammo = canvas.transform.Find("AmmoPanel");
        if (ammo != null) hud.SetSiblingIndex(ammo.GetSiblingIndex() + 1);
        hud.anchorMin = hud.anchorMax = new Vector2(1f, 0f);
        hud.pivot = new Vector2(1f, 0f);
        hud.anchoredPosition = new Vector2(-336f, 24f);
        hud.sizeDelta = new Vector2(360f, 104f);
        Image bg = go.GetComponent<Image>();
        bg.sprite = panelSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;

        hud.gameObject.SetActive(false);
    }

    const float HudPad = 24f;
    const float HudRowHeight = 50f;

    // 고른 능력 수만큼 줄을 만들고 패널 높이를 맞춤
    void RebuildHudRows()
    {
        if (hud == null) return;
        foreach (HudRow row in hudRows) Destroy(row.name.transform.parent.gameObject);
        hudRows.Clear();

        for (int i = 0; i < equipped.Count; i++)
        {
            GameObject rowGo = new GameObject("Row", typeof(RectTransform));
            RectTransform rr = rowGo.GetComponent<RectTransform>();
            rr.SetParent(hud, false);
            rr.anchorMin = new Vector2(0f, 1f);
            rr.anchorMax = new Vector2(1f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.offsetMin = new Vector2(HudPad, 0f);
            rr.offsetMax = new Vector2(-HudPad, 0f);
            rr.sizeDelta = new Vector2(rr.sizeDelta.x, HudRowHeight);
            rr.anchoredPosition = new Vector2(rr.anchoredPosition.x, -HudPad + 6f - i * HudRowHeight);

            HudRow row = new HudRow { id = equipped[i] };
            row.name = RowText(rr, 22f, new Color(0.96f, 0.83f, 0.47f), TextAlignmentOptions.TopLeft);
            row.info = RowText(rr, 17f, new Color(0.92f, 0.88f, 0.80f), TextAlignmentOptions.TopRight);

            GameObject bar = new GameObject("Cooldown", typeof(RectTransform), typeof(Image));
            RectTransform br = bar.GetComponent<RectTransform>();
            br.SetParent(rr, false);
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(0f, 8f);
            br.anchoredPosition = new Vector2(0f, 12f);
            row.bar = bar.GetComponent<Image>();
            row.bar.sprite = barFillSprite;
            row.bar.type = Image.Type.Filled;
            row.bar.fillMethod = Image.FillMethod.Horizontal;
            row.bar.raycastTarget = false;
            hudRows.Add(row);
        }

        hud.sizeDelta = new Vector2(hud.sizeDelta.x, HudPad * 2f - 6f + equipped.Count * HudRowHeight);
        hud.gameObject.SetActive(equipped.Count > 0);
    }

    TextMeshProUGUI RowText(RectTransform parent, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    void UpdateHud()
    {
        if (hud == null) return;

        foreach (HudRow row in hudRows)
        {
            SpecialDef def = abilities[row.id];
            row.name.text = def.name;

            float fill = 1f;
            string info;
            if (def.kind == SpecialKind.Weapon)
            {
                bool inHand = CurrentWeapon == row.id;
                info = inHand ? "사용 중 · [Q] 교체" : "[Q] 교체";
                fill = inHand ? 1f : 0f;
                if (row.id == FlameId && inHand) fill = 1f - heat;
            }
            else if (def.kind == SpecialKind.Skill)
            {
                int slot = skills.IndexOf(row.id);
                string key = slot >= 0 && slot < SkillKeyNames.Length ? SkillKeyNames[slot] : "?";
                float left = CooldownUntil(row.id) - Time.time;
                float length = cooldownLength.TryGetValue(row.id, out float l) ? l : 1f;
                fill = left > 0f ? 1f - left / length : 1f;
                info = left > 0f ? "[" + key + "] " + left.ToString("0.0") + "초" : "[" + key + "] 사용 가능";
                if (row.id == SoulBurstId) info += " · 영혼 " + souls;
            }
            else
            {
                info = "패시브";
                if (row.id == UndyingId) info = undyingUsed ? "사용함" : "부활 대기";
            }
            row.info.text = info;
            row.bar.fillAmount = fill;
            row.bar.color = fill >= 1f ? new Color(0.96f, 0.75f, 0.3f) : new Color(0.3f, 0.86f, 0.9f);
        }
    }
}

// ===================================================================== helpers
public static class Specials
{
    // 일반 적과 보스 모두에게 피해
    public static void Damage(GameObject go, float damage, Vector3 dir, float knock)
    {
        if (go == null) return;
        EnermyController e = go.GetComponent<EnermyController>();
        if (e != null)
        {
            if (!e.IsDead) e.TakeDamage(damage, knock, dir);
            return;
        }
        bosss b = go.GetComponent<bosss>();
        if (b != null) b.TakeDamage(damage, knock, dir);
    }

    public static Transform NearestEnemy(Vector3 from, float range)
    {
        Transform best = null;
        float bestDist = range;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(from, range))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && e.IsDead) continue;
            float d = Vector2.Distance(from, c.transform.position);
            if (d < bestDist) { bestDist = d; best = c.transform; }
        }
        return best;
    }
}

// 불타는 적: 초당 피해
public class Burn : MonoBehaviour
{
    public float dps;
    public float until;
    float tick;

    public static void Apply(GameObject target, float dps, float duration)
    {
        if (target == null) return;
        Burn b = target.GetComponent<Burn>();
        if (b == null) b = target.AddComponent<Burn>();
        b.dps = Mathf.Max(b.dps, dps);
        b.until = Time.time + duration;
    }

    void Update()
    {
        if (Time.time > until) { Destroy(this); return; }
        tick += Time.deltaTime;
        if (tick >= 0.25f)
        {
            tick = 0f;
            Specials.Damage(gameObject, dps * 0.25f, Vector3.zero, 0f);
        }
    }
}

// 가장 가까운 적을 향해 방향을 틀며 날아감
public class Homing : MonoBehaviour
{
    public float turnSpeed = 360f;
    Bullet bullet;

    void Start() => bullet = GetComponent<Bullet>();

    void Update()
    {
        if (bullet == null) return;
        Transform target = Specials.NearestEnemy(transform.position, 20f);
        if (target == null) return;
        Vector2 want = ((Vector2)(target.position - transform.position)).normalized;
        float angle = Vector2.SignedAngle(bullet.Direction, want);
        float step = Mathf.Clamp(angle, -turnSpeed * Time.deltaTime, turnSpeed * Time.deltaTime);
        bullet.Dir = Quaternion.Euler(0, 0, step) * bullet.Direction;
    }
}

// 부메랑 낫: 날아갔다가 주인에게 돌아옴
public class Scythe : MonoBehaviour
{
    public Transform owner;
    public Vector2 direction;
    public float distance = 12f;
    public float outTime = 0.45f;
    float t;
    Vector3 start;
    bool returning;

    void Start() => start = transform.position;

    void Update()
    {
        transform.Rotate(0f, 0f, -900f * Time.deltaTime);
        if (owner == null) { Destroy(gameObject); return; }

        t += Time.deltaTime;
        if (!returning)
        {
            float k = Mathf.Clamp01(t / outTime);
            transform.position = start + (Vector3)direction * distance * Mathf.Sin(k * Mathf.PI * 0.5f);
            if (k >= 1f) returning = true;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, owner.position, 30f * Time.deltaTime);
            if (Vector2.Distance(transform.position, owner.position) < 1f) Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SpecialAbilities s = Object.FindFirstObjectByType<SpecialAbilities>();
        if (s != null) s.ScytheReturned();
    }
}

// 용암 유탄: 목표 지점까지 날아가 폭발하고 용암 웅덩이를 남김
public class Grenade : MonoBehaviour
{
    public SpecialAbilities owner;
    public Vector3 target;
    public float damage;
    public float flightTime = 0.5f;
    Vector3 start;
    float t;

    void Start() => start = transform.position;

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / flightTime);
        Vector3 p = Vector3.Lerp(start, target, k);
        p.y += Mathf.Sin(k * Mathf.PI) * 2.5f;     // 포물선
        transform.position = p;
        if (k >= 1f)
        {
            owner.Explode(target, 3.5f, damage, 1.5f, new Color(1f, 0.45f, 0.1f, 0.9f));
            owner.SpawnZone(target, 2.5f, 2.5f, damage * 0.25f, new Color(1f, 0.35f, 0.05f, 0.7f));
            Destroy(gameObject);
        }
    }
}

// 일정 시간 동안 안에 있는 적에게 0.5초마다 피해
public class DamageZone : MonoBehaviour
{
    public float radius = 3f;
    public float duration = 4f;
    public float tickDamage = 1f;
    float t;
    float tick;
    SpriteRenderer sr;
    Color baseColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    void Update()
    {
        t += Time.deltaTime;
        tick += Time.deltaTime;
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (0.75f + Mathf.Sin(t * 8f) * 0.25f) * Mathf.Clamp01((duration - t) * 2f));
        if (tick >= 0.5f)
        {
            tick = 0f;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, radius))
                if (c.CompareTag("enermy") || c.CompareTag("boss")) Specials.Damage(c.gameObject, tickDamage, Vector3.zero, 0f);
        }
        if (t >= duration) Destroy(gameObject);
    }
}

// 아군 해골: 가까운 적에게 달려가 자폭
public class AllySkeleton : MonoBehaviour
{
    public SpecialAbilities owner;
    public float damage;
    public float speed = 16f;
    public float life = 10f;
    float t;
    SpriteRenderer sr;

    void Start() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        t += Time.deltaTime;
        Transform target = Specials.NearestEnemy(transform.position, 30f);
        if (target != null)
        {
            Vector3 d = target.position - transform.position;
            if (sr != null) sr.flipX = d.x < 0f;
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            if (d.magnitude < 1.5f) { Blow(); return; }
        }
        if (t >= life) Blow();
    }

    void Blow()
    {
        if (owner != null) owner.Explode(transform.position, 2.5f, damage, 1.5f, new Color(0.6f, 0.95f, 1f, 0.9f));
        Destroy(gameObject);
    }
}

// 잠깐 커지며 사라지는 빛
public class FadeOut : MonoBehaviour
{
    public float duration = 0.3f;
    float t;
    SpriteRenderer sr;
    Color c;
    Vector3 s;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        c = sr.color;
        s = transform.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        sr.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
        transform.localScale = s * (0.8f + 0.4f * k);
        if (k >= 1f) Destroy(gameObject);
    }
}

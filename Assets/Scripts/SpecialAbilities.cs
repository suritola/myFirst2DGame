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
    public IReadOnlyList<int> Weapons => weapons;
    public int SkillCount => skills.Count;
    public const int MaxSkills = 3;

    // 진화한 능력: 특수 능력 포인트로 이미 가진 능력을 한 번 더 고르면 진화
    readonly HashSet<int> evolved = new HashSet<int>();
    public bool IsEvolved(int id) => evolved.Contains(id);

    // 무기 강화 (2장 상점): 무기마다 [피해, 속도, 특성] 단계
    public const int StatDamage = 0, StatRate = 1, StatMag = 2, StatTrait = 3;
    public static readonly int[] WeaponStatMax = { 5, 5, 3, 3 };
    readonly Dictionary<int, int[]> weaponLevels = new Dictionary<int, int[]>();

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
    int undyingUses;
    float heat;
    bool overheated;
    float sniperCharge = -1f;
    bool dualToggle;
    GameObject activeScythe;
    readonly List<Transform> orbs = new List<Transform>();
    readonly Dictionary<Collider2D, float> orbHitTimes = new Dictionary<Collider2D, float>();
    Material lineMaterial;

    // 피드백 (소리, 조준 표시, 알림 글자)
    SpecialFeedback fx;
    LineRenderer aimLine;       // 조준 점선
    LineRenderer previewRing;   // 범위 원
    LineRenderer previewCone;   // 산탄총 부채꼴
    LineRenderer targetRing;    // 유도탄/갈고리 표적
    LineRenderer auraRing;      // 시간 왜곡 범위
    GameObject chargeGlow;      // 저격 충전 빛
    GameObject pactAura;        // 희생의 계약 붉은 기운
    GameObject curseGlow;       // 저주탄 장전 표시
    bool chargeFullPlayed;
    bool flameWasFiring;
    GameObject flameMuzzle;
    int aimingSkill = -1;       // 누르고 있는 조준형 스킬
    int lastBullets = -1;
    float lastOrbSound;
    float timeWarpUntil;
    readonly Dictionary<int, bool> wasReady = new Dictionary<int, bool>();
    readonly Dictionary<int, float> rowFlashUntil = new Dictionary<int, float>();

    // 누르고 있는 동안 미리보기를 보여주고 떼면 쓰는 스킬
    static bool IsAimedSkill(int id) => id == DashId || id == FireZoneId || id == HookId;

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

    // 적 · 보스 스킬이 같이 쓰는 빛 스프라이트와 소리
    public static Sprite GlowSprite;
    public static Sprite SwirlSprite;
    public static SpecialFeedback SharedFx;

    // 그림자 대시 거리: 이동 속도에 비례 (기본 속도 20 → 6칸)
    float DashDistance => player.speed * 0.3f;

    void Awake()
    {
        GlowSprite = glowSprite;
        SwirlSprite = swirlSprite;
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

        Burn.FireSprite = glowSprite;
        fx = gameObject.AddComponent<SpecialFeedback>();
        fx.Init(font, fontMaterial);
        SharedFx = fx;
        aimLine = fx.NewLine("AimLine", true);
        previewRing = fx.NewLine("PreviewRing", true);
        previewCone = fx.NewLine("PreviewCone", true);
        targetRing = fx.NewLine("TargetRing", false, 21);
        auraRing = fx.NewLine("AuraRing", true, 19);
    }

    void OnDestroy()
    {
        EnermyController.Killed -= OnEnemyKilled;
        EnermyController.GlobalSpeedMultiplier = 1f;
        EnermyController.Decoy = null;
    }

    public void Equip(IEnumerable<int> ids)
    {
        bool hadWeapons = weapons.Count > 0;
        foreach (int id in ids)
        {
            if (id < 0 || id >= abilities.Length || equipped.Contains(id)) continue;
            equipped.Add(id);
            if (abilities[id].kind == SpecialKind.Weapon) weapons.Add(id);
            if (abilities[id].kind == SpecialKind.Skill) skills.Add(id);
            if (id == OrbsId) SpawnOrbs(3);
        }
        // 처음 무기를 골랐다면 바로 꺼내 들고 시작 (이미 들고 있던 무기는 그대로)
        if (!hadWeapons) weaponIndex = weapons.Count > 0 ? 0 : -1;
        RebuildHudRows();
    }

    public bool Has(int id) => equipped.Contains(id);

    // ================================================================= weapon ammo (무기마다 따로)
    class WeaponAmmo
    {
        public int ammo;
        public float reloadEnd = -1f;
        public bool Reloading => reloadEnd > 0f;
    }
    readonly Dictionary<int, WeaponAmmo> ammoOf = new Dictionary<int, WeaponAmmo>();

    // 기본 탄창 (0 = 탄약 없음: 화염 방사기는 열기, 낫은 회수)
    static int BaseMag(int id) => id switch
    {
        ShotgunId => 5, SniperId => 4, DualId => 24, SeekerId => 12, ChainId => 8, GrenadeId => 6, _ => 0,
    };
    // 발사 간격 (초) — 권총의 공격 속도 강화와는 별개
    static float BaseInterval(int id) => id switch
    {
        ShotgunId => 0.65f, SniperId => 0.8f, DualId => 0.22f, SeekerId => 0.4f, ChainId => 0.45f, GrenadeId => 0.8f, _ => 0.45f,
    };
    static float BaseReload(int id) => id switch
    {
        ShotgunId => 2f, SniperId => 2.2f, DualId => 1.8f, SeekerId => 1.6f, ChainId => 1.8f, GrenadeId => 2.4f, _ => 1.8f,
    };

    public static bool UsesAmmo(int id) => BaseMag(id) > 0;
    public int MagSize(int id) => Mathf.RoundToInt(BaseMag(id) * (1f + 0.25f * WeaponLevel(id, StatMag)));

    WeaponAmmo Ammo(int id)
    {
        if (!ammoOf.TryGetValue(id, out WeaponAmmo a))
        {
            a = new WeaponAmmo { ammo = MagSize(id) };
            ammoOf[id] = a;
        }
        return a;
    }

    bool WeaponHasAmmo(int id)
    {
        if (!UsesAmmo(id)) return true;
        WeaponAmmo a = Ammo(id);
        return !a.Reloading && a.ammo > 0;
    }

    void StartWeaponReload(int id)
    {
        if (!UsesAmmo(id)) return;
        WeaponAmmo a = Ammo(id);
        if (a.Reloading || a.ammo >= MagSize(id)) return;
        a.reloadEnd = Time.time + BaseReload(id);
        if (player.reloadSound != null && player.TryGetComponent(out AudioSource src)) src.PlayOneShot(player.reloadSound);
    }

    // 들고 있지 않은 무기도 장전은 계속 진행
    void UpdateWeaponReloads()
    {
        foreach (int id in weapons)
        {
            if (!UsesAmmo(id)) continue;
            WeaponAmmo a = Ammo(id);
            if (a.Reloading && Time.time >= a.reloadEnd)
            {
                a.reloadEnd = -1f;
                a.ammo = MagSize(id);
                if (id == CurrentWeapon) fx.Play("clank", 0.4f, 1.3f);
            }
        }
    }

    string AmmoText(int id)
    {
        if (!UsesAmmo(id)) return null;
        WeaponAmmo a = Ammo(id);
        if (a.Reloading) return "장전 중" + new string('.', (int)(Time.unscaledTime / 0.3f) % 3 + 1);
        return a.ammo + " / " + MagSize(id);
    }

    // ================================================================= scythe sprite (코드로 그린 낫)
    static Sprite scytheSprite;
    static Sprite ScytheSprite => scytheSprite != null ? scytheSprite : (scytheSprite = MakeScytheSprite());

    static Sprite MakeScytheSprite()
    {
        const int S = 48;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[S * S];
        bool[] solid = new bool[S * S];
        void Put(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= S || y >= S) return;
            px[y * S + x] = c;
            solid[y * S + x] = true;
        }

        // 자루: 왼쪽 아래에서 오른쪽 위로
        for (float t = 0f; t <= 1f; t += 0.01f)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(9f, 33f, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(5f, 37f, t));
            Color wood = Color.Lerp(new Color(0.32f, 0.2f, 0.14f), new Color(0.5f, 0.34f, 0.22f), (x + y) % 5 == 0 ? 1f : 0.3f);
            Put(x, y, wood);
            Put(x + 1, y, wood * 0.85f + new Color(0f, 0f, 0f, 0.15f));
        }
        // 자루 끝 장식
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                Put(9 + dx, 5 + dy, new Color(0.6f, 0.45f, 0.8f));

        // 날: 자루 끝에서 왼쪽 아래로 휘어지는 초승달
        Vector2 outer = new Vector2(24f, 30f);
        Vector2 inner = new Vector2(20f, 25f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float dOut = Vector2.Distance(p, outer);
                float dIn = Vector2.Distance(p, inner);
                if (dOut > 16f || dIn < 15f || p.y < 27f || p.x > 38f) continue;
                // 바깥 가장자리일수록 밝게 빛나는 날
                float edge = Mathf.Clamp01((dOut - 12f) / 4f);
                Color c = Color.Lerp(new Color(0.42f, 0.2f, 0.62f), new Color(0.93f, 0.88f, 1f), edge);
                Put(x, y, c);
            }
        }

        // 어두운 외곽선
        Color line = new Color(0.12f, 0.04f, 0.18f, 1f);
        Color[] result = (Color[])px.Clone();
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                if (solid[y * S + x]) continue;
                bool near = false;
                for (int k = 0; k < 4 && !near; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    near = nx >= 0 && ny >= 0 && nx < S && ny < S && solid[ny * S + nx];
                }
                result[y * S + x] = near ? line : new Color(0f, 0f, 0f, 0f);
            }
        }
        tex.SetPixels(result);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 24f);
    }

    // ================================================================= weapon volley (타겟팅 스킬 일제 사격)
    static Color WeaponColor(int id) => id switch
    {
        ShotgunId => new Color(1f, 0.55f, 0.2f),
        SniperId => new Color(0.5f, 0.95f, 1f),
        DualId => new Color(1f, 0.85f, 0.5f),
        FlameId => new Color(1f, 0.5f, 0.15f),
        SeekerId => new Color(0.75f, 0.5f, 1f),
        ChainId => new Color(0.6f, 0.9f, 1f),
        ScytheId => new Color(0.8f, 0.55f, 1f),
        GrenadeId => new Color(1f, 0.45f, 0.1f),
        _ => new Color(1f, 0.9f, 0.6f),
    };

    static bool Alive(EnermyController e) => e != null && !e.IsDead;

    // 우클릭 타겟팅 스킬: 들고 있는 무기마다 전혀 다른 형식의 필살기
    static string VolleyName(int id) => id switch
    {
        ShotgunId => "지옥불 포격",
        SniperId => "관통 레일건",
        DualId => "총알 폭풍",
        FlameId => "화염 회오리",
        SeekerId => "영혼 떼",
        ChainId => "뇌운",
        ScytheId => "죽음의 춤",
        GrenadeId => "용암 융단폭격",
        _ => "일제 사격",
    };

    public IEnumerator WeaponVolley(List<EnermyController> targets, float baseDamage, float blood)
    {
        int id = CurrentWeapon;
        float dmg = baseDamage * WeaponDamageMul(id);
        Color c = WeaponColor(id);
        fx.FloatText(player.transform.position, VolleyName(id) + "!", c, 6f, 0f);
        fx.Play("pulse", 0.6f, 1.4f);
        List<EnermyController> alive = targets.FindAll(Alive);

        switch (id)
        {
            case ShotgunId:
                // 지옥불 포격: 마우스 쪽으로 거대한 부채꼴 폭발 3연발
                for (int blast = 0; blast < 3; blast++)
                {
                    Vector3 start = player.MuzzlePosition;
                    Vector2 dir = AimDir();
                    player.FaceTowards(MouseWorld());
                    const float range = 11f, half = 35f;
                    foreach (Collider2D col in Physics2D.OverlapCircleAll(start, range))
                    {
                        if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                        Vector2 to = col.transform.position - start;
                        if (Vector2.Angle(dir, to) <= half) Specials.Damage(col.gameObject, dmg * 0.9f, to.normalized, 3.5f);
                    }
                    for (int i = -2; i <= 2; i++)
                        for (int k = 1; k <= 3; k++)
                        {
                            Vector2 d = Quaternion.Euler(0, 0, i * half / 2.2f) * dir;
                            Fx.Play("fx_explosion", start + (Vector3)(d * range * k / 3.4f), 2.2f + k * 0.9f, Color.white, 14f + Random.Range(0f, 6f));
                        }
                    Fx.Play("fx_shock", start, 5f, c, 20f);
                    fx.Play("boom", 1f, 1.1f - blast * 0.1f);
                    fx.Shake(0.4f, 0.15f);
                    yield return new WaitForSeconds(0.28f);
                }
                break;

            case SniperId:
                // 관통 레일건: 충전 뒤 화면을 가로지르는 굵은 광선 (조준한 적이 많을수록 강함)
                {
                    float power = 1.5f + 0.25f * alive.Count;
                    FxAnim aim = null;
                    fx.StartLoop("hum", 0.6f, 1f);
                    for (float t = 0f; t < 0.5f; t += Time.deltaTime)
                    {
                        Vector3 m = player.MuzzlePosition;
                        if (aim != null) Destroy(aim.gameObject);
                        aim = Fx.Beam(m, m + (Vector3)(AimDir() * 45f), 0.2f + t * 0.6f, new Color(c.r, c.g, c.b, 0.6f), 0.1f);
                        Fx.Play("fx_orb", m, 0.8f + t * 2f, c, 20f);
                        yield return null;
                    }
                    fx.StopLoop();
                    if (aim != null) Destroy(aim.gameObject);
                    Vector3 from = player.MuzzlePosition;
                    Vector3 to = from + (Vector3)(AimDir() * 45f);
                    Fx.Beam(from, to, 2.4f, c, 0.4f, 16);
                    Fx.Beam(from, to, 0.8f, Color.white, 0.3f, 17);
                    foreach (Collider2D col in Physics2D.OverlapCircleAll((from + to) * 0.5f, 23f))
                    {
                        if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                        if (Hostile.DistanceToSegment(col.transform.position, from, to) > 1.5f) continue;
                        Specials.Damage(col.gameObject, dmg * power, (to - from).normalized, 2f);
                        Fx.Play("fx_spark", col.transform.position, 2f, c, 20f);
                    }
                    Fx.Play("fx_shock", from, 4f, c, 22f);
                    fx.Play("crack", 1f, 0.7f);
                    fx.Shake(0.5f, 0.2f);
                }
                break;

            case DualId:
                // 총알 폭풍: 1.6초 동안 두 줄기 나선으로 사방 난사 (움직이며 쓸 수 있음)
                {
                    float angle = Random.Range(0f, 360f);
                    for (float t = 0f; t < 1.6f; t += 0.04f)
                    {
                        for (int arm = 0; arm < 2; arm++)
                        {
                            float a = (angle + arm * 180f) * Mathf.Deg2Rad;
                            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                            Bullet b = player.CreateBullet(player.transform.position + (Vector3)(d * 0.6f), d, dmg * 0.3f, player.pene, 0f, true, 0.4f);
                            if (b != null && b.TryGetComponent(out SpriteRenderer sr)) sr.color = c;
                            Fx.Play("fx_muzzle", player.transform.position + (Vector3)(d * 0.7f), 1f, c, 30f, angle + arm * 180f, 15);
                        }
                        angle += 22f;
                        if (Mathf.Repeat(t, 0.12f) < 0.04f) fx.Play("pew", 0.3f, Random.Range(0.9f, 1.2f));
                        yield return new WaitForSeconds(0.04f);
                    }
                }
                break;

            case FlameId:
                // 화염 회오리: 마우스 위치에 불기둥이 생겨 적에게 다가가며 빨아들이고 태움
                {
                    GameObject go = new GameObject("FireTornado");
                    go.transform.position = MouseWorld();
                    FireTornado ft = go.AddComponent<FireTornado>();
                    ft.owner = this;
                    ft.damage = dmg * 0.25f;
                    fx.Play("ignite", 1f, 0.7f);
                    fx.Play("flame", 0.8f, 0.8f);
                }
                break;

            case SeekerId:
                // 영혼 떼: 영혼 구슬 다섯이 주위를 돌다 조준한 적에게 번갈아 달려듦 (4초)
                {
                    GameObject go = new GameObject("SoulSwarm");
                    SoulSwarm sw = go.AddComponent<SoulSwarm>();
                    sw.owner = player.transform;
                    sw.targets = alive;
                    sw.damage = dmg * 0.45f;
                    sw.color = c;
                    fx.Play("shimmer", 0.8f, 1.3f);
                }
                break;

            case ChainId:
                // 뇌운: 2.5초 동안 하늘에서 번개가 조준한 적들에게 연달아 떨어짐
                {
                    FxAnim cloud = Fx.Play("fx_cloud", player.transform.position + Vector3.up * 6f, 12f, new Color(0.35f, 0.4f, 0.55f, 0.8f), 3f, 0f, 25, true, 2.6f);
                    if (cloud != null) cloud.follow = player.transform;
                    for (float t = 0f; t < 2.5f; t += 0.15f)
                    {
                        alive.RemoveAll(e => !Alive(e));
                        Transform target = alive.Count > 0 ? alive[Random.Range(0, alive.Count)].transform
                                                           : Specials.NearestEnemy(player.transform.position + (Vector3)(Random.insideUnitCircle * 6f), 14f);
                        if (target != null)
                        {
                            Vector3 at = target.position;
                            Fx.Bolt(at + new Vector3(Random.Range(-2f, 2f), 14f), at, 1.4f, c, 0.2f);
                            Fx.Play("fx_shock", at, 3f, c, 24f);
                            Specials.Damage(target.gameObject, dmg * 0.5f, Vector3.zero, 0f);
                            Collider2D col = target.GetComponent<Collider2D>();
                            if (col != null) ChainLightning(at, col, dmg * 0.25f, 1);
                            fx.Play("zap", 0.5f, Random.Range(0.8f, 1.1f));
                            fx.Shake(0.1f, 0.06f);
                        }
                        yield return new WaitForSeconds(0.15f);
                    }
                }
                break;

            case ScytheId:
                // 죽음의 춤: 무적 상태로 조준한 적들 사이를 순간이동하며 벰
                {
                    SpriteRenderer body = player.GetComponent<SpriteRenderer>();
                    if (alive.Count == 0)
                    {
                        Transform near = Specials.NearestEnemy(player.transform.position, 12f);
                        if (near != null) alive.Add(near.GetComponent<EnermyController>());
                        alive.RemoveAll(e => e == null);
                    }
                    player.GrantInvincibility(0.2f * alive.Count + 0.4f);
                    foreach (EnermyController t in alive)
                    {
                        if (!Alive(t)) continue;
                        Vector3 from = player.transform.position;
                        Vector3 to = ClampToArena(t.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.2f));
                        // 잔상
                        if (body != null)
                        {
                            GameObject g = MakeSprite("DanceGhost", body.sprite, from, 1f, new Color(0.8f, 0.55f, 1f, 0.5f), "Character", -1);
                            g.transform.localScale = player.transform.lossyScale;
                            g.AddComponent<FadeOut>().duration = 0.3f;
                        }
                        Fx.Beam(from, to, 0.3f, new Color(0.8f, 0.55f, 1f, 0.7f), 0.15f);
                        player.transform.position = to;
                        player.FaceTowards(t.transform.position);
                        Fx.Play("fx_slash", t.transform.position, 5f, c, 30f, Random.Range(0f, 360f), 16);
                        foreach (Collider2D col in Physics2D.OverlapCircleAll(t.transform.position, 2.5f))
                            if (col.CompareTag("enermy") || col.CompareTag("boss"))
                                Specials.Damage(col.gameObject, dmg * 1.2f, (col.transform.position - to).normalized, 1.5f);
                        fx.Play("whoosh", 0.7f, 1.3f);
                        yield return new WaitForSeconds(0.12f);
                    }
                    Fx.Play("fx_soulburst", player.transform.position, 4f, Color.white, 18f);
                }
                break;

            case GrenadeId:
                // 용암 융단폭격: 플레이어에서 마우스 쪽으로 줄지어 운석이 떨어짐
                {
                    Vector3 start = player.transform.position;
                    Vector2 dir = AimDir();
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    for (int row = 1; row <= 6; row++)
                    {
                        for (int col = -1; col <= 1; col++)
                        {
                            Vector3 at = ClampToArena(start + (Vector3)(dir * row * 2.6f + side * col * 2.6f));
                            StartCoroutine(Bombard(at, dmg * 0.7f));
                        }
                        yield return new WaitForSeconds(0.12f);
                    }
                }
                break;

            default:
                yield break;
        }
    }

    IEnumerator Bombard(Vector3 at, float damage)
    {
        FxAnim rock = Fx.Play("fx_meteor", at + Vector3.up * 8f, 2.2f, Color.white, 16f, 0f, 20, true, 0.35f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (rock != null) rock.transform.position = Vector3.Lerp(at + Vector3.up * 8f, at, t / 0.3f);
            yield return null;
        }
        if (rock != null) Destroy(rock.gameObject);
        Explode(at, 2f, damage, 1.2f, new Color(1f, 0.45f, 0.1f, 0.9f));
    }

    // 지그재그 번개 선
    void DrawBolt(Vector3 a, Vector3 b, Color color, float duration)
    {
        GameObject go = new GameObject("Bolt");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.sortingLayerName = "Effect";
        lr.sortingOrder = 22;
        const int seg = 8;
        lr.positionCount = seg + 1;
        Vector3 side = Vector3.Cross(b - a, Vector3.forward).normalized;
        for (int i = 0; i <= seg; i++)
        {
            float k = i / (float)seg;
            float jitter = i == 0 || i == seg ? 0f : Random.Range(-0.6f, 0.6f);
            lr.SetPosition(i, Vector3.Lerp(a, b, k) + side * jitter);
        }
        lr.startColor = Color.white;
        lr.endColor = color;
        lr.startWidth = 0.3f;
        lr.endWidth = 0.15f;
        go.AddComponent<LineFade>().duration = duration;
        Fx.Bolt(a, b, 1.1f, color, duration + 0.05f);
        Fx.Play("fx_spark", b, 1.6f, color, 20f);
    }

    // 낫 모양 그림을 총알에 붙임 (판정은 총알 그대로)
    void AttachScytheVisual(Bullet b, float worldSize)
    {
        if (b.TryGetComponent(out SpriteRenderer bulletSr)) bulletSr.enabled = false;
        float s = Mathf.Max(0.01f, b.transform.lossyScale.x);
        GameObject blade = MakeSprite("ScytheBlade", ScytheSprite, b.transform.position, 1f, Color.white, "Effect", 8);
        blade.transform.SetParent(b.transform, true);
        blade.transform.localScale = Vector3.one * (worldSize / s);
        GameObject aura = MakeSprite("ScytheGlow", glowSprite, b.transform.position, 1f, new Color(0.7f, 0.4f, 1f, 0.35f), "Effect", 7);
        aura.transform.SetParent(b.transform, true);
        aura.transform.localScale = Vector3.one * (worldSize * 0.3f / s);
    }

    // ================================================================= evolution
    // order: 한 번에 여러 개를 진화할 때 알림 글자를 위로 쌓는 순서
    public void Evolve(int id, int order = 0)
    {
        if (!equipped.Contains(id) || evolved.Contains(id)) return;
        evolved.Add(id);
        if (id == OrbsId) SpawnOrbs(2);

        if (fx != null)
        {
            fx.Play("pulse", 0.9f, 0.9f);
            fx.Play("chime", 0.8f, 1.1f);
            if (player != null) fx.FloatText(player.transform.position + Vector3.up * (1.4f * order), abilities[id].name + " 진화!", new Color(1f, 0.85f, 0.4f), 6f, 0f);
        }
        if (player != null) Flash(player.transform.position, 7f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.6f);
    }

    // 진화 효과 설명 (ID 순서)
    public static readonly string[] EvolveTexts =
    {
        "사거리 +2, 부채꼴이 넓어지고 피해 320%. 맞은 적이 불탑니다.",
        "충전 시간 0.8초. 완충 사격이 맞은 곳에서 폭발합니다.",
        "두 총구에서 동시에 발사합니다 (탄약 소모는 그대로).",
        "열이 40% 덜 오르고 불길 화상 피해가 강해집니다.",
        "한 번에 유도탄 2발을 쏩니다.",
        "번개가 7번 튀고, 튈 때 피해가 덜 줄어듭니다.",
        "낫이 더 커지고 더 멀리 날아가며 피해 220%.",
        "폭발 후 작은 용암탄 3개로 흩어집니다.",
        "쿨타임 1.8초. 도착 지점에서 충격파가 터집니다.",
        "장판이 더 넓고 6초 동안 지속됩니다. 쿨타임 9초.",
        "적이 75% 느려지고 7초 동안 지속됩니다.",
        "체력을 잃지 않고 12초 동안 지속됩니다.",
        "영혼 3개부터 쓸 수 있고 폭발 범위가 넓어집니다.",
        "해골 5마리를 부르고 쿨타임 10초.",
        "분신이 5초 동안 남고 사라질 때 폭발합니다.",
        "쿨타임 2초, 갈고리 피해 300%.",
        "영혼이 5개로 늘고 피해가 강해집니다.",
        "반격 범위와 피해가 크게 늘어납니다.",
        "탄창의 마지막 두 발이 저주탄이 됩니다.",
        "두 번 부활하고, 부활할 때 체력 50%로 일어납니다.",
    };

    // ================================================================= weapon upgrades (shop)
    public int WeaponLevel(int id, int stat) => weaponLevels.TryGetValue(id, out int[] l) ? l[stat] : 0;

    public bool UpgradeWeapon(int id, int stat)
    {
        if (!weapons.Contains(id) || WeaponLevel(id, stat) >= WeaponStatMax[stat]) return false;
        if (!weaponLevels.ContainsKey(id)) weaponLevels[id] = new int[4];
        int oldMag = MagSize(id);
        weaponLevels[id][stat]++;
        // 탄창이 커지면 늘어난 만큼 바로 채움
        if (stat == StatMag && UsesAmmo(id)) Ammo(id).ammo += MagSize(id) - oldMag;
        if (fx != null)
        {
            fx.Play("clank", 0.6f, 1.2f);
            fx.Play("chime", 0.4f, 1.4f);
        }
        return true;
    }

    float WeaponDamageMul(int id) => 1f + 0.15f * WeaponLevel(id, StatDamage);
    float WeaponRateMul(int id) => Mathf.Pow(0.9f, WeaponLevel(id, StatRate));
    int Trait(int id) => WeaponLevel(id, StatTrait);

    // 무기별 특성 강화 이름과 한 단계 효과
    public static string TraitName(int id) => id switch
    {
        ShotgunId => "사거리",
        SniperId => "충전 속도",
        DualId => "관통",
        FlameId => "냉각",
        SeekerId => "관통",
        ChainId => "연쇄",
        ScytheId => "낫 크기",
        GrenadeId => "폭발 범위",
        _ => "특성",
    };

    public static string TraitStep(int id) => id switch
    {
        ShotgunId => "사거리 +1",
        SniperId => "충전 시간 -15%",
        DualId => "관통 +1",
        FlameId => "열 발생 -20%",
        SeekerId => "관통 +1",
        ChainId => "연쇄 +1",
        ScytheId => "크기·거리 +15%",
        GrenadeId => "범위 +0.6",
        _ => "",
    };

    // ================================================================= update
    void Update()
    {
        if (player == null || equipped.Count == 0 || Time.timeScale == 0f)
        {
            // 멈춘 동안에는 소리와 표시를 숨김
            if (fx != null) fx.StopLoop();
            HidePreviews();
            UpdateHud();
            return;
        }

        HidePreviews();

        if (weapons.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                // 기본 권총(-1) → 무기1 → 무기2 → 기본 권총 ...
                weaponIndex = weaponIndex + 1 >= weapons.Count ? -1 : weaponIndex + 1;
                CancelSniperCharge();
                if (flameMuzzle != null) Destroy(flameMuzzle);
                flameWasFiring = false;
                fx.StopLoop();
                fx.Play("clank", 0.5f, 1.4f);
                fx.FloatText(player.transform.position, WeaponActive ? abilities[CurrentWeapon].name : "기본 권총", new Color(0.96f, 0.83f, 0.47f), 4.5f, 0f);
            }
            UpdateWeaponReloads();
            // 무기를 들고 있으면 그 무기의 탄창을 표시 (R: 들고 있는 무기 장전)
            player.ammoTextOverride = WeaponActive ? AmmoText(CurrentWeapon) : null;
            if (WeaponActive && Input.GetKeyDown(KeyCode.R)) StartWeaponReload(CurrentWeapon);
            if (WeaponActive) UpdateWeapon();
        }

        for (int i = 0; i < skills.Count && i < SkillKeys.Length; i++) HandleSkillKey(skills[i], SkillKeys[i]);
        if (aimingSkill >= 0) ShowSkillPreview(aimingSkill);

        if (Has(OrbsId)) UpdateOrbs();
        if (CurrentWeapon != FlameId) heat = Mathf.Max(0f, heat - Time.deltaTime * 0.35f);

        UpdateReadyChimes();
        UpdateCurseNotice();
        UpdateAuras();
        UpdateHud();
    }

    Vector3 MouseWorld()
    {
        Vector3 m = player.MainCamera.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;
        return m;
    }

    float Damage => player.damage * player.damageMultiplier;
    // 들고 있는 무기의 강화가 반영된 피해
    float WDamage => Damage * WeaponDamageMul(CurrentWeapon);
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
                // 공격 범위(부채꼴) 표시
                fx.SetCone(previewCone, player.MuzzlePosition, AimDir(), ShotgunHalfAngle, ShotgunRange, new Color(1f, 0.55f, 0.2f, 0.45f), 0.1f);
                if (down && Ready()) FireShotgun();
                break;
            case SniperId:
                UpdateSniper(down, up);
                break;
            case DualId:
                if (held && Ready()) FireDual();
                break;
            case FlameId:
                UpdateFlame(held);
                break;
            case SeekerId:
                {
                    // 유도탄이 쫓아갈 적 표시
                    Transform target = Specials.NearestEnemy(MouseWorld(), 20f) ?? Specials.NearestEnemy(player.transform.position, 20f);
                    if (target != null) fx.SetRing(targetRing, target.position, 1.6f + Mathf.Sin(Time.time * 8f) * 0.15f, new Color(0.75f, 0.5f, 1f, 0.85f), 0.12f);
                    if (held && Ready()) FireSeeker();
                }
                break;
            case ChainId:
                if (down && Ready()) FireChain();
                break;
            case ScytheId:
                if (activeScythe == null)
                    fx.SetLine(aimLine, player.MuzzlePosition, player.MuzzlePosition + (Vector3)(AimDir() * 12f), new Color(0.75f, 0.45f, 1f, 0.5f), 0.1f);
                if (down && activeScythe == null && !player.IsSkillUsing) FireScythe();
                player.ammoTextOverride = activeScythe == null ? "낫 준비" : "낫 회수 중";
                break;
            case GrenadeId:
                {
                    // 떨어질 지점과 폭발 범위
                    Vector3 land = GrenadeLanding();
                    fx.SetLine(aimLine, player.MuzzlePosition, land, new Color(1f, 0.5f, 0.15f, 0.4f), 0.08f);
                    fx.SetRing(previewRing, land, GrenadeRadius, new Color(1f, 0.45f, 0.1f, Ready() ? 0.75f : 0.3f), 0.1f);
                    if (down && Ready()) FireGrenade();
                }
                break;
        }
    }

    bool Ready() => !player.IsSkillUsing && WeaponHasAmmo(CurrentWeapon) && Time.time >= nextFire;

    Vector2 AimDir() => ((Vector2)(MouseWorld() - player.MuzzlePosition)).normalized;

    float GrenadeRadius => 3.5f + 0.6f * Trait(GrenadeId);

    Vector3 GrenadeLanding()
    {
        Vector3 start = player.MuzzlePosition;
        Vector3 target = MouseWorld();
        if (Vector2.Distance(start, target) > 14f) target = start + (Vector3)(AimDir() * 14f);
        return target;
    }

    // 저격총: 점선 조준선은 항상, 누르고 있으면 충전 (빛, 소리, 완충 알림)
    void UpdateSniper(bool down, bool up)
    {
        Vector3 muzzle = player.MuzzlePosition;
        Vector3 mouse = MouseWorld();

        if (down && Ready())
        {
            sniperCharge = 0f;
            chargeFullPlayed = false;
            chargeGlow = MakeSprite("SniperCharge", glowSprite, muzzle, 0.05f, new Color(0.5f, 0.95f, 1f, 0.8f), "Effect", 6);
        }

        float k = sniperCharge >= 0f ? sniperCharge / SniperChargeTime : 0f;
        bool full = k >= 1f;
        Color lineColor = sniperCharge < 0f ? new Color(0.5f, 0.95f, 1f, 0.35f)
            : full ? Color.Lerp(new Color(1f, 0.85f, 0.3f, 0.7f), new Color(1f, 1f, 0.8f, 1f), Mathf.PingPong(Time.time * 6f, 1f))
            : new Color(0.5f, 0.95f, 1f, 0.4f + 0.5f * k);
        // 마우스 거리와 상관없이 조준 방향으로 길게
        fx.SetLine(aimLine, muzzle, muzzle + (Vector3)(AimDir() * SniperLineLength), lineColor, sniperCharge < 0f ? 0.08f : 0.08f + 0.1f * k);

        if (sniperCharge < 0f) return;

        sniperCharge = Mathf.Min(SniperChargeTime, sniperCharge + Time.deltaTime);
        player.ammoTextOverride = full ? "완충!" : "충전 " + Mathf.RoundToInt(k * 100f) + "%";
        fx.StartLoop("hum", 0.5f, 0.7f + 0.9f * k);

        if (chargeGlow != null)
        {
            chargeGlow.transform.position = muzzle;
            float pulse = full ? 1f + Mathf.Sin(Time.time * 18f) * 0.15f : 1f;
            chargeGlow.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.28f, k) * pulse;
            chargeGlow.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.5f, 0.95f, 1f, 0.8f), new Color(1f, 0.85f, 0.3f, 0.95f), k);
        }

        if (full && !chargeFullPlayed)
        {
            chargeFullPlayed = true;
            fx.Play("ding", 0.8f);
        }

        if (up)
        {
            FireSniper(k);
            CancelSniperCharge();
        }
    }

    const float SniperLineLength = 40f;
    float SniperChargeTime => (IsEvolved(SniperId) ? 0.8f : 1.2f) * (1f - 0.15f * Trait(SniperId));

    void CancelSniperCharge()
    {
        sniperCharge = -1f;
        if (chargeGlow != null) Destroy(chargeGlow);
        if (fx != null && CurrentWeapon != FlameId) fx.StopLoop();
    }

    // 공통: 발사 준비 (방향, 소리, 탄약, 탄창 저주)
    Vector2 BeginShot(int ammoCost, out Vector3 start, out bool cursed)
    {
        Vector3 target = MouseWorld();
        player.FaceTowards(target);
        start = player.MuzzlePosition;
        nextFire = Time.time + BaseInterval(CurrentWeapon) / player.fireRateMultiplier * WeaponRateMul(CurrentWeapon);
        // 무기마다 자기 탄창을 씀 (권총 탄창과 별개)
        Flash(start, 1.3f, new Color(WeaponColor(CurrentWeapon).r, WeaponColor(CurrentWeapon).g, WeaponColor(CurrentWeapon).b, 0.85f), 0.08f);
        Vector2 aimDir = ((Vector2)(target - start)).normalized;
        Fx.Play("fx_muzzle", start + (Vector3)(aimDir * 0.4f), 1.4f, WeaponColor(CurrentWeapon), 24f, Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg, 15);
        WeaponAmmo mag = Ammo(CurrentWeapon);
        cursed = IsLastBulletCursed(mag.ammo);
        mag.ammo = Mathf.Max(0, mag.ammo - ammoCost);
        if (mag.ammo <= 0) StartWeaponReload(CurrentWeapon);
        if (player.shotSound != null && player.TryGetComponent(out AudioSource a)) a.PlayOneShot(player.shotSound);
        return ((Vector2)(target - start)).normalized;
    }

    Bullet Shot(Vector3 start, Vector2 dir, float dmg, int pene, float knockRate, bool cursed, Color tint, float speedMul = 1f, float scale = 1f, float skillCharge = 1f)
    {
        Bullet b = player.CreateBullet(start, dir, dmg * (cursed ? 3f : 1f), pene, 0, false, knockRate);
        if (b == null) return null;
        // 스킬 게이지: 화염 방사기가 기준, 나머지 무기는 천천히
        b.skillCharge = skillCharge * (CurrentWeapon == FlameId ? 1f : PlayerController.GaugeRate);
        b.speed *= speedMul;
        b.transform.localScale *= scale;
        if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = tint;
        CurseBullet(b, cursed);
        return b;
    }

    // 총알 없이 앞쪽 부채꼴 안의 적을 즉시 타격하는 근거리 폭발
    float ShotgunRange => 6f + Trait(ShotgunId) + (IsEvolved(ShotgunId) ? 2f : 0f);
    float ShotgunHalfAngle => IsEvolved(ShotgunId) ? 40f : 30f;

    void FireShotgun()
    {
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        bool evo = IsEvolved(ShotgunId);
        float dmg = WDamage * (evo ? 3.2f : 2.5f) * (cursed ? 3f : 1f);
        bool hitAny = false;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(start, ShotgunRange))
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector2 to = (Vector2)(c.transform.position - start);
            if (Vector2.Angle(dir, to) > ShotgunHalfAngle) continue;

            Specials.Damage(c.gameObject, dmg, to.normalized, 2.5f);
            if (evo) Burn.Apply(c.gameObject, WDamage * 0.5f, 2f);
            hitAny = true;
            if (cursed) Explode(c.transform.position, 2.5f, Damage * 1.5f, 1.5f, new Color(1f, 0.3f, 0.2f, 0.85f));
        }

        fx.Play("boom", 0.8f, 1.25f);
        fx.Shake(0.3f, 0.14f);

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
            // 스킬 게이지는 시간으로 참
        }
    }

    void FireSniper(float charge)
    {
        if (player.IsSkillUsing || !WeaponHasAmmo(SniperId)) return;
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        Bullet b = Shot(start, dir, WDamage * Mathf.Lerp(1.5f, 3f, charge), 999, 1.5f, cursed, new Color(0.5f, 0.95f, 1f), 1.6f, 1.4f, 1.5f);
        // 진화: 완충 사격이 맞은 곳마다 폭발
        if (b != null && charge >= 1f && IsEvolved(SniperId))
        {
            float boom = WDamage * 1.5f;
            b.onHitEnemy += (bullet, col) => Explode(col.transform.position, 3f, boom, 1.5f, new Color(0.5f, 0.95f, 1f, 0.85f));
        }
        fx.Play("crack", 0.7f + 0.3f * charge, 1.2f - 0.3f * charge);
        fx.Shake(0.15f + 0.25f * charge, 0.12f);
    }

    void FireDual()
    {
        dualToggle = !dualToggle;
        Vector2 dir = BeginShot(dualToggle ? 1 : 0, out Vector3 start, out bool cursed);
        Vector3 side = new Vector3(-dir.y, dir.x) * 0.35f;
        fx.Play("pew", 0.35f, dualToggle ? 1f : 1.15f);
        int pene = player.pene + Trait(DualId);
        // 진화: 양쪽 총구에서 동시에
        foreach (float s in IsEvolved(DualId) ? new[] { 1f, -1f } : new[] { dualToggle ? 1f : -1f })
        {
            Flash(start + side * s, 0.9f, new Color(1f, 0.85f, 0.5f, 0.8f), 0.06f);
            Shot(start + side * s, dir, WDamage * 0.55f, pene, 0.6f, cursed, new Color(1f, 0.85f, 0.5f), 1f, 1f, 0.25f);
        }
    }

    void UpdateFlame(bool held)
    {
        if (overheated && heat <= 0.3f)
        {
            overheated = false;
            fx.Play("chime", 0.4f, 1.2f);
            fx.FloatText(player.transform.position, "냉각 완료", new Color(0.5f, 0.95f, 1f));
        }
        bool firing = held && !overheated && !player.IsSkillUsing;

        // 켤 때 점화음, 뿜는 동안 불길 소리 (열이 오를수록 조금 높아짐)
        if (firing && !flameWasFiring) fx.Play("ignite", 0.9f);
        if (firing) fx.StartLoop("flame", 0.95f, 0.9f + heat * 0.25f);
        else fx.StopLoop();
        flameWasFiring = firing;

        // 총구 불빛
        if (firing && flameMuzzle == null)
            flameMuzzle = MakeSprite("FlameMuzzle", glowSprite, player.MuzzlePosition, 0.12f, new Color(1f, 0.6f, 0.2f, 0.9f), "Effect", 7);
        if (!firing && flameMuzzle != null) Destroy(flameMuzzle);
        if (flameMuzzle != null)
        {
            flameMuzzle.transform.position = player.MuzzlePosition;
            flameMuzzle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.16f);
        }

        // 과열 중에는 총구에서 연기
        if (overheated && Random.value < Time.deltaTime * 10f)
            FlameParticle.Spawn(glowSprite, player.MuzzlePosition, Vector2.up * Random.Range(1.5f, 3f) + Random.insideUnitCircle, 0.9f, 0.08f, 0.35f, true);

        if (firing && Time.time >= nextFire)
        {
            Vector3 target = MouseWorld();
            player.FaceTowards(target);
            Vector3 start = player.MuzzlePosition;
            Vector2 aim = ((Vector2)(target - start)).normalized;
            Vector2 dir = Quaternion.Euler(0, 0, Random.Range(-8f, 8f)) * aim;
            nextFire = Time.time + 0.07f / player.fireRateMultiplier * WeaponRateMul(FlameId);

            // 실제 피해는 보이지 않는 판정용 탄이 담당
            Bullet b = Shot(start, dir, WDamage * 0.25f, 99, 0.2f, false, new Color(1f, 0.55f, 0.15f, 0.9f), 0.2f, 1.6f, 0.05f);
            if (b != null)
            {
                b.lifetime = 0.35f;
                if (b.TryGetComponent(out SpriteRenderer hitboxSr)) hitboxSr.enabled = false;
                float burnDps = WDamage * (IsEvolved(FlameId) ? 0.5f : 0.3f);
                b.onHitEnemy += (bullet, col) => Burn.Apply(col.gameObject, burnDps, 2f);
            }

            // 퍼져 나가는 불꽃과 불티
            for (int i = 0; i < 3; i++)
            {
                Vector2 v = (Vector2)(Quaternion.Euler(0, 0, Random.Range(-14f, 14f)) * aim) * Random.Range(22f, 32f);
                FlameParticle.Spawn(glowSprite, start + (Vector3)(aim * 0.3f), v, Random.Range(0.35f, 0.5f), 0.05f, Random.Range(0.45f, 0.7f), false);
            }
            if (Random.value < 0.6f)
            {
                Vector2 v = (Vector2)(Quaternion.Euler(0, 0, Random.Range(-25f, 25f)) * aim) * Random.Range(12f, 24f);
                FlameParticle.SpawnEmber(glowSprite, start, v);
            }

            heat += 0.035f * (1f - 0.2f * Trait(FlameId)) * (IsEvolved(FlameId) ? 0.6f : 1f);
            if (heat >= 1f)
            {
                heat = 1f;
                overheated = true;
                fx.StopLoop();
                fx.Play("hiss", 0.9f);
                fx.FloatText(player.transform.position, "과열!", new Color(1f, 0.35f, 0.25f), 6f);
            }
        }
        else if (!firing)
        {
            // 쏘지 않을 때만 식음
            heat = Mathf.Max(0f, heat - Time.deltaTime * 0.35f);
        }
        player.ammoTextOverride = overheated ? "과열!" : "열기 " + Mathf.RoundToInt(heat * 100f) + "%";
    }

    void FireSeeker()
    {
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        fx.Play("whoosh", 0.4f, 1.6f);
        // 진화: 양옆으로 두 발
        int count = IsEvolved(SeekerId) ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 d = count == 1 ? dir : (Vector2)(Quaternion.Euler(0, 0, i == 0 ? -14f : 14f) * dir);
            Bullet b = Shot(start, d, WDamage * 0.7f, 1 + Trait(SeekerId), 0.5f, cursed, new Color(0.7f, 0.5f, 1f), 0.3f, 1.2f, 0.8f);
            if (b != null) b.gameObject.AddComponent<Homing>().turnSpeed = 360f;
        }
    }

    void FireChain()
    {
        Vector2 dir = BeginShot(1, out Vector3 start, out bool cursed);
        Bullet b = Shot(start, dir, WDamage, 1, 1f, cursed, new Color(0.6f, 0.9f, 1f));
        fx.Play("pew", 0.35f, 0.8f);
        if (b != null)
        {
            float dmg = WDamage;
            bool evo = IsEvolved(ChainId);
            int jumps = (evo ? 7 : 4) + Trait(ChainId);
            b.onHitEnemy += (bullet, col) => ChainLightning(col.transform.position, col, dmg * 0.8f, jumps, evo ? 0.9f : 0.8f);
        }
    }

    void FireScythe()
    {
        Vector3 target = MouseWorld();
        player.FaceTowards(target);
        bool evo = IsEvolved(ScytheId);
        float grow = 1f + 0.15f * Trait(ScytheId);
        Bullet b = Shot(player.MuzzlePosition, ((Vector2)(target - player.MuzzlePosition)).normalized, WDamage * (evo ? 2.2f : 1.5f), 9999, 1.2f, false,
                        new Color(0.75f, 0.45f, 1f), 0f, (evo ? 3.5f : 2.5f) * grow, 0.3f);
        if (b == null) return;
        b.lifetime = 5f;
        Scythe s = b.gameObject.AddComponent<Scythe>();
        s.distance = (evo ? 16f : 12f) * grow;
        // 총알 대신 코드로 그린 낫을 보여줌 (판정은 그대로)
        AttachScytheVisual(b, (evo ? 1.7f : 1.3f) * grow);
        s.outTime = 0.45f * WeaponRateMul(ScytheId);
        s.owner = player.transform;
        s.direction = ((Vector2)(target - player.MuzzlePosition)).normalized;
        activeScythe = b.gameObject;
        fx.Play("whoosh", 0.7f, 0.9f);
    }

    void FireGrenade()
    {
        Vector3 target = GrenadeLanding();
        BeginShot(2, out Vector3 start, out bool cursed);
        fx.Play("thump", 0.8f);
        GameObject g = MakeSprite("LavaGrenade", glowSprite, start, 1.2f, new Color(1f, 0.45f, 0.1f), "Effect", 5);
        Grenade gr = g.AddComponent<Grenade>();
        gr.owner = this;
        gr.target = target;
        gr.damage = WDamage * 2f * (cursed ? 3f : 1f);
        gr.radius = GrenadeRadius;
        gr.cluster = IsEvolved(GrenadeId);
    }

    // ================================================================= skills
    // 스킬 키: 쿨타임 중이면 경고, 조준형은 누르고 있는 동안 미리보기 후 떼면 사용
    void HandleSkillKey(int id, KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            float left = CooldownUntil(id) - Time.time;
            if (left > 0f)
            {
                fx.Play("buzz", 0.6f);
                fx.FloatText(player.transform.position, abilities[id].name + " " + left.ToString("0.0") + "초", new Color(0.7f, 0.66f, 0.72f), 4f, 0.4f);
                return;
            }
            if (player.IsSkillUsing) return;
            if (IsAimedSkill(id)) aimingSkill = id;
            else UseSkill(id);
        }

        if (aimingSkill == id && Input.GetKeyUp(key))
        {
            aimingSkill = -1;
            if (Time.time >= CooldownUntil(id) && !player.IsSkillUsing) UseSkill(id);
        }
    }

    void ShowSkillPreview(int id)
    {
        Vector3 from = player.transform.position;
        Vector3 mouse = MouseWorld();
        Vector2 dir = ((Vector2)(mouse - from)).normalized;
        switch (id)
        {
            case DashId:
                {
                    Vector3 end = ClampToArena(from + (Vector3)(dir * DashDistance));
                    fx.SetLine(aimLine, from, end, new Color(0.5f, 0.95f, 1f, 0.7f), 0.1f);
                    fx.SetRing(previewRing, end, 0.9f, new Color(0.5f, 0.95f, 1f, 0.8f), 0.1f);
                }
                break;
            case FireZoneId:
                fx.SetRing(previewRing, mouse, FireZoneRadius, new Color(1f, 0.45f, 0.1f, 0.8f), 0.12f);
                break;
            case HookId:
                {
                    HookTarget(from, dir, out Collider2D enemy, out float dist);
                    Vector3 end = from + (Vector3)(dir * dist);
                    fx.SetLine(aimLine, from, end, new Color(0.85f, 0.15f, 0.2f, 0.8f), 0.1f);
                    if (enemy != null) fx.SetRing(targetRing, enemy.transform.position, 1.6f, new Color(1f, 0.2f, 0.25f, 0.9f), 0.14f);
                    else fx.SetRing(previewRing, end, 0.8f, new Color(0.85f, 0.15f, 0.2f, 0.6f), 0.08f);
                }
                break;
        }
    }

    float FireZoneRadius => IsEvolved(FireZoneId) ? 4.5f : 3f;
    int SoulsNeeded => IsEvolved(SoulBurstId) ? 3 : 5;

    void UseSkill(int id)
    {
        bool evo = IsEvolved(id);
        switch (id)
        {
            case DashId: StartCoroutine(Dash()); StartCooldown(id, evo ? 1.8f : 3f); fx.Play("whoosh", 0.9f, 1.2f); break;
            case FireZoneId:
                DamageZone zone = SpawnZone(MouseWorld(), FireZoneRadius, evo ? 6f : 4f, Damage * 0.75f, new Color(1f, 0.4f, 0.1f, 0.8f));
                zone.lava = true;
                ShockRing.Spawn(zone.transform.position, 0.3f, FireZoneRadius * 1.2f, 0.35f, new Color(1f, 0.55f, 0.2f, 0.9f), 0.3f);
                FxAnim fireRune = Fx.Play("fx_rune", zone.transform.position, FireZoneRadius * 2f, new Color(1f, 0.55f, 0.2f, 0.7f), 1f, 0f, 2, true, evo ? 6f : 4f);
                if (fireRune != null) fireRune.spin = 45f;
                Fx.Play("fx_explosion", zone.transform.position, FireZoneRadius * 1.6f, Color.white, 16f);
                StartCooldown(id, evo ? 9f : 12f);
                fx.Play("boom", 0.5f, 0.8f);
                fx.Play("crackle", 0.8f);
                break;
            case TimeWarpId:
                StartCoroutine(TimeWarp());
                StartCooldown(id, 20f);
                fx.Play("shimmer", 0.9f, 0.6f);
                fx.FloatText(player.transform.position, "시간 왜곡!", new Color(0.55f, 0.85f, 1f), 5f, 0f);
                break;
            case PactId:
                StartCoroutine(Pact());
                StartCooldown(id, 25f);
                fx.Play("pulse", 1f, 0.8f);
                fx.FloatText(player.transform.position, "희생의 계약! 공격력 2배", new Color(1f, 0.3f, 0.3f), 5f, 0f);
                break;
            case SoulBurstId:
                if (souls < SoulsNeeded)
                {
                    fx.Play("buzz", 0.6f);
                    fx.FloatText(player.transform.position, "영혼 부족 " + souls + "/" + SoulsNeeded, new Color(0.7f, 0.66f, 0.72f), 4.5f, 0.4f);
                    return;
                }
                Explode(player.transform.position, evo ? 10f : 7f, Damage * (1.5f + souls * 0.25f), 3f, new Color(0.6f, 0.95f, 1f, 0.9f));
                souls = 0;
                StartCooldown(id, 5f);
                break;
            case SkeletonsId:
                SummonSkeletons(evo ? 5 : 3);
                StartCooldown(id, evo ? 10f : 15f);
                fx.Play("shimmer", 0.8f, 1.2f);
                fx.FloatText(player.transform.position, "해골 소환!", new Color(0.6f, 0.95f, 1f), 4.5f, 0f);
                break;
            case MirrorId:
                StartCoroutine(Mirror());
                StartCooldown(id, 12f);
                fx.Play("shimmer", 0.8f, 1.6f);
                break;
            case HookId: Hook(); StartCooldown(id, evo ? 2f : 4f); fx.Play("clank", 0.8f); break;
        }
    }

    float CooldownUntil(int id) => cooldownUntil.TryGetValue(id, out float t) ? t : 0f;

    void StartCooldown(int id, float seconds)
    {
        cooldownLength[id] = seconds;
        cooldownUntil[id] = Time.time + seconds;
        wasReady[id] = false;
    }

    // 쿨타임이 끝나면 알림음과 HUD 반짝임
    void UpdateReadyChimes()
    {
        foreach (int id in skills)
        {
            bool ready = Time.time >= CooldownUntil(id);
            if (ready && wasReady.TryGetValue(id, out bool before) && !before)
            {
                fx.Play("chime", 0.45f);
                rowFlashUntil[id] = Time.time + 0.5f;
            }
            wasReady[id] = ready;
        }
    }

    // 탄창 저주: 마지막 한 발이 장전되면 알림과 붉은 빛
    void UpdateCurseNotice()
    {
        if (!Has(CurseId)) return;
        int now = WeaponActive && UsesAmmo(CurrentWeapon) ? Ammo(CurrentWeapon).ammo : player.NowBullet;
        if (IsLastBulletCursed(now) && !IsLastBulletCursed(lastBullets))
        {
            fx.Play("pulse", 0.6f, 1.6f);
            fx.FloatText(player.transform.position, "저주탄 장전!", new Color(1f, 0.3f, 0.3f), 4.5f, 0f);
        }
        lastBullets = now;

        bool show = IsLastBulletCursed(now) && (!WeaponActive || UsesAmmo(CurrentWeapon));
        if (show && curseGlow == null) curseGlow = MakeSprite("CurseGlow", glowSprite, player.MuzzlePosition, 0.18f, new Color(1f, 0.2f, 0.2f, 0.8f), "Effect", 6);
        if (!show && curseGlow != null) Destroy(curseGlow);
        if (curseGlow != null)
        {
            curseGlow.transform.position = player.MuzzlePosition;
            curseGlow.transform.localScale = Vector3.one * (0.16f + Mathf.Sin(Time.time * 10f) * 0.04f);
        }
    }

    // 지속형 스킬 표시 (시간 왜곡 범위, 희생의 계약 기운)
    void UpdateAuras()
    {
        if (Time.time < timeWarpUntil)
            fx.SetRing(auraRing, player.transform.position, 6f + Mathf.Sin(Time.time * 4f) * 0.4f, new Color(0.5f, 0.8f, 1f, 0.45f), 0.12f);
        else
            SpecialFeedback.Hide(auraRing);

        if (pactAura != null)
        {
            pactAura.transform.position = player.transform.position + new Vector3(0f, -0.8f, 0f);
            pactAura.transform.localScale = Vector3.one * (0.75f + Mathf.Sin(Time.time * 6f) * 0.08f);
        }
    }

    void HidePreviews()
    {
        SpecialFeedback.Hide(aimLine);
        SpecialFeedback.Hide(previewRing);
        SpecialFeedback.Hide(previewCone);
        SpecialFeedback.Hide(targetRing);
    }

    IEnumerator Dash()
    {
        Vector3 start = player.transform.position;
        Vector3 dir = (MouseWorld() - start).normalized;
        Vector3 end = ClampToArena(start + dir * DashDistance);
        player.GrantInvincibility(0.35f);
        ShockRing.Spawn(start, 0.3f, 2.2f, 0.3f, new Color(0.5f, 0.95f, 1f, 0.9f), 0.25f);
        for (int i = 0; i < 3; i++) Fx.Play("fx_smoke", start + (Vector3)(Random.insideUnitCircle * 0.6f), 1.8f, new Color(0.6f, 0.9f, 1f), 14f);
        SpriteRenderer body = player.GetComponent<SpriteRenderer>();
        int ghosts = 0;
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(start, end, t / 0.15f);
            // 잔상
            if (body != null && t >= ghosts * 0.035f)
            {
                ghosts++;
                GameObject g = MakeSprite("DashGhost", body.sprite, player.transform.position, 1f, new Color(0.5f, 0.95f, 1f, 0.5f), "Character", -1);
                g.transform.localScale = player.transform.lossyScale;
                g.GetComponent<SpriteRenderer>().flipX = body.flipX;
                g.AddComponent<FadeOut>().duration = 0.25f;
            }
            yield return null;
        }
        player.transform.position = end;
        ShockRing.Spawn(end, 0.3f, 1.8f, 0.25f, new Color(0.5f, 0.95f, 1f, 0.9f), 0.2f);
        Fx.Play("fx_shock", end, 4f, new Color(0.55f, 0.95f, 1f), 22f);
        for (int i = 0; i < 6; i++) SoulWisp.Spawn(end, end + (Vector3)(Random.insideUnitCircle.normalized * 2.5f), new Color(0.5f, 0.95f, 1f), true);
        // 진화: 도착 지점 충격파
        if (IsEvolved(DashId)) Explode(end, 2.5f, Damage * 1.5f, 2f, new Color(0.5f, 0.95f, 1f, 0.85f));
    }

    IEnumerator TimeWarp()
    {
        bool evo = IsEvolved(TimeWarpId);
        float duration = evo ? 7f : 5f;
        EnermyController.GlobalSpeedMultiplier = evo ? 0.25f : 0.5f;
        timeWarpUntil = Time.time + duration;
        Flash(player.transform.position, 12f, new Color(0.5f, 0.8f, 1f, 0.5f), 0.5f);
        ShockRing.Spawn(player.transform.position, 1f, 14f, 0.7f, new Color(0.55f, 0.85f, 1f, 0.9f), 0.5f);
        FxAnim clock = Fx.Play("fx_rune", player.transform.position, 12f, new Color(0.55f, 0.85f, 1f, 0.45f), 1f, 0f, 1, true, duration);
        if (clock != null) { clock.follow = player.transform; clock.spin = -25f; }
        Fx.Play("fx_shock", player.transform.position, 14f, new Color(0.6f, 0.9f, 1f), 12f);
        ShockRing.Spawn(player.transform.position, 0.5f, 9f, 0.5f, Color.white, 0.2f);
        yield return new WaitForSeconds(duration);
        EnermyController.GlobalSpeedMultiplier = 1f;
    }

    IEnumerator Pact()
    {
        bool evo = IsEvolved(PactId);
        if (!evo) player.PlayerHealth = Mathf.Max(1f, player.PlayerHealth - player.PlayerMaxHealth * 0.2f);
        player.damageMultiplier = 2f;
        player.fireRateMultiplier = 1.5f;
        if (pactAura != null) Destroy(pactAura);
        pactAura = MakeSprite("PactAura", glowSprite, player.transform.position, 0.75f, new Color(1f, 0.15f, 0.2f, 0.45f), "Background", 8);
        Flash(player.transform.position, 5f, new Color(1f, 0.15f, 0.2f, 0.7f), 0.4f);
        for (int i = 0; i < 14; i++)
            SoulWisp.Spawn(player.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 5f), player.transform.position, new Color(1f, 0.2f, 0.25f));
        ShockRing.Spawn(player.transform.position, 4f, 0.5f, 0.4f, new Color(1f, 0.2f, 0.25f, 0.9f), 0.3f);
        Fx.Play("fx_shock", player.transform.position, 7f, new Color(1f, 0.25f, 0.3f), 16f);
        Fx.Play("fx_spark", player.transform.position, 3f, new Color(1f, 0.3f, 0.3f), 14f);
        yield return new WaitForSeconds(evo ? 12f : 8f);
        player.damageMultiplier = 1f;
        player.fireRateMultiplier = 1f;
        if (pactAura != null) Destroy(pactAura);
        fx.FloatText(player.transform.position, "계약 종료", new Color(0.7f, 0.66f, 0.72f), 4f, 0f);
    }

    IEnumerator Mirror()
    {
        SpriteRenderer body = player.GetComponent<SpriteRenderer>();
        GameObject decoy = MakeSprite("MirrorDecoy", body.sprite, player.transform.position, 1f, new Color(0.55f, 0.9f, 1f, 0.75f), "Character", 0);
        decoy.transform.localScale = player.transform.lossyScale;
        decoy.GetComponent<SpriteRenderer>().flipX = body.flipX;
        EnermyController.Decoy = decoy.transform;
        ShockRing.Spawn(decoy.transform.position, 0.3f, 3f, 0.35f, new Color(0.55f, 0.9f, 1f, 0.9f), 0.25f);
        Fx.Play("fx_soulburst", decoy.transform.position, 3.5f, Color.white, 18f);
        for (int i = 0; i < 8; i++) SoulWisp.Spawn(decoy.transform.position, decoy.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 3f), new Color(0.55f, 0.9f, 1f), true);
        player.bodyAlpha = 0.4f;
        bool evo = IsEvolved(MirrorId);
        yield return new WaitForSeconds(evo ? 5f : 3f);
        EnermyController.Decoy = null;
        player.bodyAlpha = 1f;
        // 진화: 분신이 사라지며 폭발
        if (evo && decoy != null) Explode(decoy.transform.position, 4f, Damage * 3f, 2.5f, new Color(0.55f, 0.9f, 1f, 0.9f));
        Destroy(decoy);
    }

    void Hook()
    {
        Vector3 from = player.transform.position;
        Vector2 dir = ((Vector2)(MouseWorld() - from)).normalized;
        float wallDist = HookTarget(from, dir, out Collider2D enemy, out _);

        if (enemy != null)
        {
            DrawBolt(from, enemy.transform.position, new Color(0.9f, 0.15f, 0.2f), 0.25f);
            Flash(enemy.transform.position, 2f, new Color(1f, 0.2f, 0.25f, 0.8f), 0.15f);
            Specials.Damage(enemy.gameObject, Damage * (IsEvolved(HookId) ? 3f : 1.5f), Vector3.zero, 0f);
            if (enemy.CompareTag("enermy")) StartCoroutine(Pull(enemy.transform, from + (Vector3)dir * 2.5f));
        }
        else
        {
            Vector3 end = ClampToArena(from + (Vector3)dir * Mathf.Max(0f, wallDist - 1.5f));
            DrawLine(from, from + (Vector3)dir * wallDist, new Color(0.8f, 0.1f, 0.15f), 0.2f);
            StartCoroutine(PullPlayer(end));
        }
    }

    const float HookRange = 14f;

    // 갈고리가 걸릴 대상: 벽보다 가까운 첫 적 (없으면 null). 반환값 = 벽까지 거리
    float HookTarget(Vector3 from, Vector2 dir, out Collider2D enemy, out float reach)
    {
        enemy = null;
        float enemyDist = HookRange;
        float wallDist = HookRange;
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(from, 0.6f, dir, HookRange))
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
        if (enemy != null && enemyDist >= wallDist) enemy = null;
        reach = enemy != null ? enemyDist : wallDist;
        return wallDist;
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
            Flash(pos, 2.2f, new Color(0.6f, 0.95f, 1f, 0.8f), 0.25f);
            Fx.Play("fx_soulburst", pos, 3f, Color.white, 18f);
            ShockRing.Spawn(pos, 0.2f, 1.6f, 0.3f, new Color(0.6f, 0.95f, 1f, 0.9f), 0.15f);
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
            GameObject o = MakeSprite("GuardianSoul", swirlSprite, player.transform.position, 0.55f, new Color(0.55f, 0.95f, 1f), "Effect", 2);
            orbs.Add(o.transform);
        }
    }

    const float OrbRadius = 4.6f;

    void UpdateOrbs()
    {
        float baseAngle = Time.time * 180f;
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null) continue;
            float a = (baseAngle + 360f / orbs.Count * i) * Mathf.Deg2Rad;
            Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a) - 0.4f, 0f) * OrbRadius;
            orbs[i].position = pos;
            orbs[i].Rotate(0f, 0f, -540f * Time.deltaTime);

            foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, 1.25f))
            {
                if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                if (orbHitTimes.TryGetValue(c, out float last) && Time.time - last < 0.5f) continue;
                orbHitTimes[c] = Time.time;
                if (Time.time - lastOrbSound > 0.15f)
                {
                    lastOrbSound = Time.time;
                    fx.Play("pew", 0.18f, 2f);
                }
                Specials.Damage(c.gameObject, Damage * (IsEvolved(OrbsId) ? 1.2f : 0.8f), (c.transform.position - player.transform.position).normalized, 0.8f);
            }
        }
    }

    // 플레이어가 맞았을 때 (복수의 가시)
    public void OnPlayerHurt()
    {
        if (!Has(ThornsId) || player == null) return;
        fx.Play("boom", 0.6f, 1.4f);
        fx.FloatText(player.transform.position, "가시 반격!", new Color(1f, 0.35f, 0.4f), 4.5f, 0.3f);
        bool evo = IsEvolved(ThornsId);
        Explode(player.transform.position, evo ? 6f : 4f, Damage * (evo ? 5f : 3f), 2f, new Color(0.9f, 0.2f, 0.3f, 0.85f));
    }

    // 불사의 맹세: 한 판에 한 번
    public bool TryUndying()
    {
        if (!Has(UndyingId) || undyingUses >= UndyingMaxUses) return false;
        undyingUses++;
        fx.Play("pulse", 1f, 0.6f);
        fx.Play("chime", 1f, 0.7f);
        fx.Shake(0.4f, 0.3f);
        Flash(player.transform.position, 6f, new Color(1f, 0.9f, 0.5f, 0.8f), 0.6f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner("불사의 맹세가 발동했다!", 2f);
        return true;
    }

    int UndyingMaxUses => IsEvolved(UndyingId) ? 2 : 1;

    // 부활할 때 체력 (진화하면 절반)
    public float UndyingReviveHealth(float maxHealth) => IsEvolved(UndyingId) ? maxHealth * 0.5f : 1f;

    // 탄창 저주: 탄창의 마지막 한 발 (진화하면 두 발)
    public bool IsLastBulletCursed(int bulletsBeforeShot) => Has(CurseId) && bulletsBeforeShot >= 1 && bulletsBeforeShot <= (IsEvolved(CurseId) ? 2 : 1);

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
        if (!Has(SoulBurstId)) return;
        souls = Mathf.Min(40, souls + 1);
        if (souls == SoulsNeeded)
        {
            fx.Play("chime", 0.5f, 1.3f);
            fx.FloatText(player.transform.position, "영혼 폭발 준비", new Color(0.55f, 0.95f, 1f), 4.5f, 0f);
        }
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
        Flash(pos, radius * 0.9f, new Color(1f, 1f, 1f, 0.9f), 0.12f);
        // 도트 폭발: 따뜻한 색은 화염, 차가운 색은 영혼 폭발
        Fx.Play(color.r >= color.b ? "fx_explosion" : "fx_soulburst", pos, radius * 2.4f, Color.white, 16f);
        Fx.Play("fx_shock", pos, radius * 2.6f, color, 20f);
        ShockRing.Spawn(pos, radius * 0.3f, radius * 1.15f, 0.3f, color, 0.3f);
        for (int i = 0; i < Mathf.Clamp(Mathf.RoundToInt(radius * 3f), 4, 16); i++)
            SoulWisp.Spawn(pos, pos + (Vector3)(Random.insideUnitCircle.normalized * radius * 1.5f), color, true);
        if (fx != null)
        {
            fx.Play("boom", Mathf.Clamp(radius / 5f, 0.3f, 0.9f), Mathf.Clamp(1.6f - radius * 0.12f, 0.8f, 1.5f));
            if (radius >= 3f) fx.Shake(0.12f + radius * 0.03f, 0.12f);
        }
    }

    public DamageZone SpawnZone(Vector3 pos, float radius, float duration, float tickDamage, Color color)
    {
        GameObject z = MakeSprite("DamageZone", glowSprite, pos, radius * 2f / 8f, color, "Background", 7);
        DamageZone dz = z.AddComponent<DamageZone>();
        dz.radius = radius;
        dz.duration = duration;
        dz.tickDamage = tickDamage;
        return dz;
    }

    void ChainLightning(Vector3 from, Collider2D first, float damage, int jumps, float falloff = 0.8f)
    {
        fx.Play("zap", 0.5f, Random.Range(0.9f, 1.1f));
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
            DrawBolt(current, next.transform.position, new Color(0.6f, 0.9f, 1f), 0.18f);
            Flash(next.transform.position, 1.6f, new Color(0.6f, 0.9f, 1f, 0.8f), 0.12f);
            Specials.Damage(next.gameObject, damage, Vector3.zero, 0f);
            current = next.transform.position;
            damage *= falloff;
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

    public void ScytheReturned()
    {
        activeScythe = null;
        if (fx != null && CurrentWeapon == ScytheId) fx.Play("clank", 0.5f);
    }

    // ================================================================= HUD: 무기 / 스킬 / 패시브 세 창 (왼쪽 아래)
    class HudPanel
    {
        public RectTransform rect;
        public int count;
    }
    HudPanel weaponPanel, skillPanel, passivePanel;
    const int PistolRow = -1;

    void BuildHud()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        weaponPanel = NewPanel(canvas, "WeaponSlot", "무기  [Q] 교체  [R] 장전");
        skillPanel = NewPanel(canvas, "SkillSlot", "스킬");
        passivePanel = NewPanel(canvas, "PassiveSlot", "패시브");
        hud = weaponPanel.rect;
    }

    HudPanel NewPanel(Canvas canvas, string name, string title)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        Transform ammo = canvas.transform.Find("AmmoPanel");
        if (ammo != null) r.SetSiblingIndex(ammo.GetSiblingIndex() + 1);
        r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
        r.pivot = new Vector2(0f, 0f);
        r.sizeDelta = new Vector2(PanelWidth, 100f);
        Image bg = go.GetComponent<Image>();
        bg.sprite = panelSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;

        GameObject head = new GameObject("Title", typeof(RectTransform));
        RectTransform hr = head.GetComponent<RectTransform>();
        hr.SetParent(r, false);
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.offsetMin = new Vector2(HudPad, 0f);
        hr.offsetMax = new Vector2(-HudPad, 0f);
        hr.sizeDelta = new Vector2(hr.sizeDelta.x, TitleHeight);
        hr.anchoredPosition = new Vector2(hr.anchoredPosition.x, -HudPad + 8f);
        RowText(hr, 16f, new Color(1f, 0.72f, 0.55f), TextAlignmentOptions.TopLeft).text = title;

        go.SetActive(false);
        return new HudPanel { rect = r };
    }

    const float PanelWidth = 320f;
    const float PanelGap = 12f;
    const float HudPad = 22f;
    const float TitleHeight = 24f;
    const float HudRowHeight = 46f;

    // 고른 능력을 종류별 창에 한 줄씩 넣고 창 크기와 위치를 맞춤
    void RebuildHudRows()
    {
        if (weaponPanel == null) return;
        foreach (HudRow row in hudRows) Destroy(row.name.transform.parent.gameObject);
        hudRows.Clear();

        List<int> w = new List<int>(), s = new List<int>(), p = new List<int>();
        if (weapons.Count > 0) w.Add(PistolRow);
        foreach (int id in equipped)
        {
            if (abilities[id].kind == SpecialKind.Weapon) w.Add(id);
            else if (abilities[id].kind == SpecialKind.Skill) s.Add(id);
            else p.Add(id);
        }
        Fill(weaponPanel, w);
        Fill(skillPanel, s);
        Fill(passivePanel, p);

        // 첫 창은 왼쪽 아래, 둘째 창은 그 오른쪽, 셋째 창은 첫 창 위
        List<HudPanel> shown = new List<HudPanel>();
        foreach (HudPanel panel in new[] { weaponPanel, skillPanel, passivePanel })
            if (panel.count > 0) shown.Add(panel);
        for (int i = 0; i < shown.Count; i++)
        {
            float x = 24f + (i == 1 ? PanelWidth + PanelGap : 0f);
            float y = 100f + (i == 2 ? shown[0].rect.sizeDelta.y + PanelGap : 0f);
            shown[i].rect.anchoredPosition = new Vector2(x, y);
        }
    }

    void Fill(HudPanel panel, List<int> ids)
    {
        panel.count = ids.Count;
        for (int i = 0; i < ids.Count; i++)
        {
            GameObject rowGo = new GameObject("Row", typeof(RectTransform));
            RectTransform rr = rowGo.GetComponent<RectTransform>();
            rr.SetParent(panel.rect, false);
            rr.anchorMin = new Vector2(0f, 1f);
            rr.anchorMax = new Vector2(1f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.offsetMin = new Vector2(HudPad, 0f);
            rr.offsetMax = new Vector2(-HudPad, 0f);
            rr.sizeDelta = new Vector2(rr.sizeDelta.x, HudRowHeight);
            rr.anchoredPosition = new Vector2(rr.anchoredPosition.x, -HudPad - TitleHeight + 8f - i * HudRowHeight);

            HudRow row = new HudRow { id = ids[i] };
            row.name = RowText(rr, 20f, new Color(0.96f, 0.83f, 0.47f), TextAlignmentOptions.TopLeft);
            row.info = RowText(rr, 16f, new Color(0.92f, 0.88f, 0.80f), TextAlignmentOptions.TopRight);

            GameObject bar = new GameObject("Cooldown", typeof(RectTransform), typeof(Image));
            RectTransform br = bar.GetComponent<RectTransform>();
            br.SetParent(rr, false);
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(0f, 7f);
            br.anchoredPosition = new Vector2(0f, 12f);
            row.bar = bar.GetComponent<Image>();
            row.bar.sprite = barFillSprite;
            row.bar.type = Image.Type.Filled;
            row.bar.fillMethod = Image.FillMethod.Horizontal;
            row.bar.raycastTarget = false;
            hudRows.Add(row);
        }
        panel.rect.sizeDelta = new Vector2(PanelWidth, HudPad * 2f + TitleHeight - 8f + ids.Count * HudRowHeight);
        panel.rect.gameObject.SetActive(ids.Count > 0);
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
        if (weaponPanel == null) return;

        foreach (HudRow row in hudRows)
        {
            float fill = 1f;
            string info;
            if (row.id == PistolRow)
            {
                // 기본 권총: 권총 탄창과 장전 상태
                bool inHand = !WeaponActive;
                row.name.text = "기본 권총";
                if (player.reload > 0f)
                {
                    fill = Mathf.Clamp01(player.reload / Mathf.Max(0.01f, player.reloadTime));
                    info = "장전 중";
                }
                else
                {
                    fill = player.MaxBullet > 0 ? player.NowBullet / (float)player.MaxBullet : 1f;
                    info = player.NowBullet + "/" + player.MaxBullet;
                }
                if (inHand) info = "사용 중 · " + info;
                row.name.color = inHand ? Color.white : new Color(0.96f, 0.83f, 0.47f);
                row.info.text = info;
                row.bar.fillAmount = fill;
                row.bar.color = inHand ? new Color(0.96f, 0.75f, 0.3f) : new Color(0.55f, 0.5f, 0.45f);
                continue;
            }

            SpecialDef def = abilities[row.id];
            row.name.text = def.name + (IsEvolved(row.id) ? "+" : "");
            bool highlight = false;
            if (def.kind == SpecialKind.Weapon)
            {
                bool inHand = CurrentWeapon == row.id;
                highlight = inHand;
                if (row.id == FlameId)
                {
                    fill = 1f - heat;
                    info = overheated ? "과열" : "열기 " + Mathf.RoundToInt(heat * 100f) + "%";
                }
                else if (row.id == ScytheId)
                {
                    info = activeScythe == null ? "준비" : "회수 중";
                    fill = activeScythe == null ? 1f : 0f;
                }
                else
                {
                    WeaponAmmo a = Ammo(row.id);
                    if (a.Reloading)
                    {
                        fill = 1f - (a.reloadEnd - Time.time) / BaseReload(row.id);
                        info = "장전 중";
                    }
                    else
                    {
                        fill = a.ammo / (float)Mathf.Max(1, MagSize(row.id));
                        info = a.ammo + "/" + MagSize(row.id);
                    }
                }
                if (inHand) info = "사용 중 · " + info;
                row.bar.color = inHand ? new Color(0.96f, 0.75f, 0.3f) : new Color(0.55f, 0.5f, 0.45f);
            }
            else if (def.kind == SpecialKind.Skill)
            {
                int slot = skills.IndexOf(row.id);
                string key = slot >= 0 && slot < SkillKeyNames.Length ? SkillKeyNames[slot] : "?";
                float left = CooldownUntil(row.id) - Time.time;
                float length = cooldownLength.TryGetValue(row.id, out float l) ? l : 1f;
                fill = left > 0f ? 1f - left / length : 1f;
                info = left > 0f ? "[" + key + "] " + left.ToString("0.0") + "초" : "[" + key + "] 준비";
                if (row.id == SoulBurstId) info += " · 영혼 " + souls;
                row.bar.color = fill >= 1f ? new Color(0.96f, 0.75f, 0.3f) : new Color(0.3f, 0.86f, 0.9f);
            }
            else
            {
                info = "";
                if (row.id == UndyingId)
                {
                    int left = UndyingMaxUses - undyingUses;
                    info = left > 0 ? "부활 " + left : "사용함";
                    fill = left > 0 ? 1f : 0f;
                }
                row.bar.color = new Color(0.6f, 0.85f, 1f);
            }
            row.info.text = info;
            bool flash = rowFlashUntil.TryGetValue(row.id, out float until) && Time.time < until;
            row.name.color = flash || highlight ? Color.white : new Color(0.96f, 0.83f, 0.47f);
            row.bar.fillAmount = fill;
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
    public static Sprite FireSprite;

    public float dps;
    public float until;
    float tick;
    float puff;
    GameObject flame;

    public static void Apply(GameObject target, float dps, float duration)
    {
        if (target == null) return;
        Burn b = target.GetComponent<Burn>();
        if (b == null) b = target.AddComponent<Burn>();
        b.dps = Mathf.Max(b.dps, dps);
        b.until = Time.time + duration;
    }

    void Start()
    {
        // 몸에서 타오르는 불빛
        if (FireSprite != null)
            flame = SpecialAbilities.MakeSprite("BurnFlame", FireSprite, transform.position, 0.3f, new Color(1f, 0.5f, 0.15f, 0.7f), "Effect", 2);
    }

    void Update()
    {
        if (Time.time > until) { Destroy(this); return; }

        if (flame != null)
        {
            flame.transform.position = transform.position + new Vector3(0f, 0.3f, 0f);
            flame.transform.localScale = Vector3.one * Random.Range(0.26f, 0.36f);
        }
        puff += Time.deltaTime;
        if (puff >= 0.1f && FireSprite != null)
        {
            puff = 0f;
            FlameParticle.Spawn(FireSprite, transform.position + (Vector3)(Random.insideUnitCircle * 0.8f),
                                new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(3f, 5f)), 0.45f, 0.04f, 0.28f, false);
        }

        tick += Time.deltaTime;
        if (tick >= 0.25f)
        {
            tick = 0f;
            Specials.Damage(gameObject, dps * 0.25f, Vector3.zero, 0f);
        }
    }

    void OnDestroy()
    {
        if (flame != null) Destroy(flame);
    }
}

// 불꽃 입자: 흰노랑 → 주황 → 빨강 → 연기로 변하며 커지고 느려짐
public class FlameParticle : MonoBehaviour
{
    Vector2 velocity;
    float life;
    float age;
    float startScale;
    float endScale;
    bool smoke;
    bool ember;
    SpriteRenderer sr;

    public static void Spawn(Sprite sprite, Vector3 pos, Vector2 velocity, float life, float startScale, float endScale, bool smoke)
    {
        GameObject go = SpecialAbilities.MakeSprite(smoke ? "Smoke" : "Flame", sprite, pos, startScale, Color.white, "Effect", smoke ? 1 : 4);
        FlameParticle p = go.AddComponent<FlameParticle>();
        p.velocity = velocity;
        p.life = life;
        p.startScale = startScale;
        p.endScale = endScale;
        p.smoke = smoke;
        p.sr = go.GetComponent<SpriteRenderer>();
        p.Tint(0f);
    }

    public static void SpawnEmber(Sprite sprite, Vector3 pos, Vector2 velocity)
    {
        GameObject go = SpecialAbilities.MakeSprite("Ember", sprite, pos, 0.025f, new Color(1f, 0.9f, 0.5f), "Effect", 5);
        FlameParticle p = go.AddComponent<FlameParticle>();
        p.velocity = velocity;
        p.life = Random.Range(0.4f, 0.7f);
        p.startScale = p.endScale = 0.025f;
        p.ember = true;
        p.sr = go.GetComponent<SpriteRenderer>();
    }

    void Tint(float k)
    {
        if (smoke)
        {
            sr.color = new Color(0.3f, 0.27f, 0.28f, 0.55f * (1f - k));
            return;
        }
        Color c;
        if (k < 0.2f) c = Color.Lerp(new Color(1f, 0.97f, 0.75f), new Color(1f, 0.75f, 0.25f), k / 0.2f);
        else if (k < 0.55f) c = Color.Lerp(new Color(1f, 0.75f, 0.25f), new Color(1f, 0.42f, 0.1f), (k - 0.2f) / 0.35f);
        else if (k < 0.8f) c = Color.Lerp(new Color(1f, 0.42f, 0.1f), new Color(0.75f, 0.15f, 0.08f), (k - 0.55f) / 0.25f);
        else c = Color.Lerp(new Color(0.75f, 0.15f, 0.08f), new Color(0.25f, 0.2f, 0.2f), (k - 0.8f) / 0.2f);
        c.a = k < 0.8f ? 0.85f : 0.85f * (1f - (k - 0.8f) / 0.2f);
        sr.color = c;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / life);
        // 앞으로 나가다 점점 느려지고, 끝에서는 위로 떠오름
        velocity *= 1f - Mathf.Min(1f, Time.deltaTime * (ember ? 1.5f : 4f));
        if (smoke || k > 0.6f) velocity.y += Time.deltaTime * 4f;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, Mathf.Sqrt(k));

        if (ember) sr.color = new Color(1f, Random.Range(0.6f, 0.95f), 0.4f, 1f - k);
        else Tint(k);

        if (k >= 1f) Destroy(gameObject);
    }
}

// 가장 가까운 적을 향해 방향을 틀며 날아감
public class Homing : MonoBehaviour
{
    public float turnSpeed = 360f;
    Bullet bullet;

    float trail;

    void Start() => bullet = GetComponent<Bullet>();

    void Update()
    {
        if (bullet == null) return;
        // 보랏빛 꼬리
        trail += Time.deltaTime;
        if (trail >= 0.03f && SpecialAbilities.GlowSprite != null)
        {
            trail = 0f;
            GameObject t = SpecialAbilities.MakeSprite("SeekerTrail", SpecialAbilities.GlowSprite, transform.position, 0.06f, new Color(0.75f, 0.5f, 1f, 0.55f), "Effect", 4);
            t.AddComponent<FadeOut>().duration = 0.3f;
        }
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

// 용암 유탄: 불꼬리를 끌며 포물선으로 날아가 폭발하고 끓는 용암 웅덩이를 남김
public class Grenade : MonoBehaviour
{
    public SpecialAbilities owner;
    public Vector3 target;
    public float damage;
    public float radius = 3.5f;
    public bool cluster;        // 진화: 폭발 후 작은 용암탄 3개로 흩어짐
    public bool mini;           // 흩어진 작은 용암탄 (장판 없음)
    public float flightTime = 0.5f;
    Vector3 start;
    float t;
    float puff;
    float baseScale;
    SpriteRenderer sr;
    Color baseColor;
    GameObject shadow;
    LineRenderer marker;

    static readonly Color Hot = new Color(1f, 0.92f, 0.55f);
    static readonly Color Lava = new Color(1f, 0.45f, 0.1f, 0.9f);

    void Start()
    {
        start = transform.position;
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        baseScale = transform.localScale.x;
        // 떨어질 곳의 그림자와 범위 고리
        shadow = SpecialAbilities.MakeSprite("GrenadeShadow", sr.sprite, target, 0.05f, new Color(0f, 0f, 0f, 0.4f), "Effect", 0);
        marker = Hostile.NewLine("GrenadeMark", new Color(1f, 0.45f, 0.1f, 0.6f), mini ? 0.06f : 0.1f, 1);
        marker.loop = true;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / flightTime);
        Vector3 p = Vector3.Lerp(start, target, k);
        p.y += Mathf.Sin(k * Mathf.PI) * (mini ? 1.5f : 2.8f);     // 포물선
        transform.position = p;

        // 달아오른 핵: 회전하며 깜빡임
        transform.Rotate(0f, 0f, -720f * Time.deltaTime);
        float pulse = Mathf.PingPong(Time.time * 12f, 1f);
        transform.localScale = Vector3.one * baseScale * (0.85f + 0.3f * pulse);
        sr.color = Color.Lerp(baseColor, Hot, pulse);

        // 불꽃 꼬리와 연기
        puff += Time.deltaTime;
        if (puff >= 0.02f)
        {
            puff = 0f;
            FlameParticle.Spawn(sr.sprite, p, Random.insideUnitCircle * 1.5f, Random.Range(0.25f, 0.4f), 0.05f, mini ? 0.15f : 0.25f, false);
            if (Random.value < 0.35f) FlameParticle.Spawn(sr.sprite, p, Vector2.up * 1.5f, 0.6f, 0.04f, 0.2f, true);
            if (Random.value < 0.3f) FlameParticle.SpawnEmber(sr.sprite, p, Random.insideUnitCircle * 6f);
        }

        if (shadow != null) shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, radius * 0.12f, k);
        if (marker != null)
        {
            Hostile.SetArc(marker, target, radius * Mathf.Lerp(0.3f, 1f, k), 0f, 354f);
            marker.startColor = marker.endColor = new Color(1f, 0.45f, 0.1f, 0.3f + 0.5f * k);
        }

        if (k >= 1f) Blast();
    }

    void Blast()
    {
        owner.Explode(target, radius, damage, 1.5f, Lava);
        Sprite glow = sr.sprite;

        // 하얗게 달아오른 중심 섬광
        GameObject core = SpecialAbilities.MakeSprite("GrenadeCore", glow, target, radius * 1.1f / 8f, new Color(1f, 0.97f, 0.8f, 1f), "Effect", 6);
        core.AddComponent<FadeOut>().duration = 0.14f;
        // 두 겹의 충격파
        ShockRing.Spawn(target, radius * 0.2f, radius * 1.2f, 0.3f, new Color(1f, 0.8f, 0.4f, 0.95f), mini ? 0.2f : 0.35f);
        ShockRing.Spawn(target, radius * 0.1f, radius * 0.9f, 0.5f, new Color(0.8f, 0.2f, 0.05f, 0.8f), mini ? 0.3f : 0.6f);
        // 사방으로 튀는 불꽃, 불티, 연기
        int flames = mini ? 7 : 16;
        for (int i = 0; i < flames; i++)
        {
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(radius * 3f, radius * 6f);
            FlameParticle.Spawn(glow, target, v, Random.Range(0.4f, 0.65f), 0.07f, Random.Range(0.35f, 0.6f), false);
        }
        for (int i = 0; i < (mini ? 5 : 12); i++)
            FlameParticle.SpawnEmber(glow, target, Random.insideUnitCircle.normalized * Random.Range(9f, 20f));
        for (int i = 0; i < (mini ? 2 : 6); i++)
            FlameParticle.Spawn(glow, target + (Vector3)(Random.insideUnitCircle * radius * 0.5f), Vector2.up * Random.Range(1.5f, 3f) + Random.insideUnitCircle,
                                Random.Range(0.9f, 1.4f), 0.1f, Random.Range(0.4f, 0.7f), true);
        // 그을음 자국
        GameObject scorch = SpecialAbilities.MakeSprite("Scorch", glow, target, radius * 1.6f / 8f, new Color(0.12f, 0.04f, 0.02f, 0.55f), "Background", 6);
        scorch.AddComponent<FadeOut>().duration = 3f;

        if (!mini)
        {
            DamageZone zone = owner.SpawnZone(target, radius * 0.7f, 2.5f, damage * 0.25f, new Color(1f, 0.35f, 0.05f, 0.7f));
            if (zone != null) zone.lava = true;
        }
        if (cluster)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject g = SpecialAbilities.MakeSprite("LavaShard", glow, target, baseScale * 0.6f, baseColor, "Effect", 5);
                Grenade m = g.AddComponent<Grenade>();
                m.owner = owner;
                m.target = target + (Vector3)(Quaternion.Euler(0, 0, i * 120f + Random.Range(-20f, 20f)) * Vector2.right * Random.Range(2.5f, 4f));
                m.damage = damage * 0.5f;
                m.radius = radius * 0.6f;
                m.mini = true;
                m.flightTime = 0.35f;
            }
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (shadow != null) Destroy(shadow);
        if (marker != null) Destroy(marker.gameObject);
    }
}

// 일정 시간 동안 안에 있는 적에게 0.5초마다 피해
public class DamageZone : MonoBehaviour
{
    public float radius = 3f;
    public float duration = 4f;
    public float tickDamage = 1f;
    public bool lava;           // 끓어오르는 용암 (불꽃과 불티가 올라옴)
    float t;
    float tick;
    float bubble;
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
        if (lava)
        {
            bubble += Time.deltaTime;
            if (bubble >= 0.06f)
            {
                bubble = 0f;
                Vector3 at = transform.position + (Vector3)(Random.insideUnitCircle * radius * 0.85f);
                FlameParticle.Spawn(sr.sprite, at, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(2f, 4f)), Random.Range(0.35f, 0.55f), 0.04f, Random.Range(0.15f, 0.3f), false);
                if (Random.value < 0.3f) FlameParticle.SpawnEmber(sr.sprite, at, new Vector2(Random.Range(-2f, 2f), Random.Range(4f, 8f)));
            }
        }
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

// 계속 회전
public class Spin : MonoBehaviour
{
    public float speed = 360f;
    void Update() => transform.Rotate(0f, 0f, speed * Time.deltaTime);
}

// 선이 가늘어지며 사라짐
public class LineFade : MonoBehaviour
{
    public float duration = 0.2f;
    float t;
    LineRenderer lr;
    Color a, b;
    float w0, w1;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        a = lr.startColor; b = lr.endColor;
        w0 = lr.startWidth; w1 = lr.endWidth;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        lr.startColor = new Color(a.r, a.g, a.b, a.a * (1f - k));
        lr.endColor = new Color(b.r, b.g, b.b, b.a * (1f - k));
        lr.startWidth = w0 * (1f - k * 0.5f);
        lr.endWidth = w1 * (1f - k * 0.5f);
        if (k >= 1f) Destroy(gameObject);
    }
}

// 화염 회오리: 가까운 적에게 천천히 다가가며 주변 적을 빨아들이고 태움 (4초)
public class FireTornado : MonoBehaviour
{
    public SpecialAbilities owner;
    public float damage;
    public float life = 4f;
    const float Radius = 3f;
    float age, tick;
    FxAnim body;

    void Start()
    {
        body = Fx.Play("fx_tornado", transform.position + Vector3.up * 1.8f, 5f, Color.white, 14f, 0f, 14, true, life);
        FxAnim rune = Fx.Play("fx_rune", transform.position, Radius * 2f, new Color(1f, 0.5f, 0.15f, 0.55f), 1f, 0f, 1, true, life);
        if (rune != null) { rune.follow = transform; rune.spin = 120f; }
    }

    void Update()
    {
        age += Time.deltaTime;
        Transform target = Specials.NearestEnemy(transform.position, 14f);
        if (target != null) transform.position = Vector3.MoveTowards(transform.position, target.position, 5f * Time.deltaTime);
        if (body != null) body.transform.position = transform.position + Vector3.up * 1.8f;

        // 빨아들임
        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, Radius * 1.6f))
        {
            if (!c.CompareTag("enermy")) continue;
            c.transform.position = Vector3.MoveTowards(c.transform.position, transform.position, 4f * Time.deltaTime);
        }

        tick += Time.deltaTime;
        if (tick >= 0.25f)
        {
            tick = 0f;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, Radius))
            {
                if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
                Specials.Damage(c.gameObject, damage, Vector3.zero, 0f);
                Burn.Apply(c.gameObject, damage, 2f);
            }
            if (SpecialAbilities.GlowSprite != null)
                for (int i = 0; i < 4; i++)
                    FlameParticle.SpawnEmber(SpecialAbilities.GlowSprite, transform.position, Random.insideUnitCircle.normalized * Random.Range(6f, 12f));
        }
        if (age >= life)
        {
            Fx.Play("fx_explosion", transform.position, 5f, Color.white, 16f);
            Destroy(gameObject);
        }
    }
}

// 영혼 떼: 구슬들이 주인 주위를 돌다가 번갈아 표적에게 달려들고 돌아옴 (4초)
public class SoulSwarm : MonoBehaviour
{
    public Transform owner;
    public List<EnermyController> targets;
    public float damage;
    public Color color = Color.white;
    public float life = 4f;
    const int Count = 5;
    readonly FxAnim[] orbs = new FxAnim[Count];
    readonly Transform[] chasing = new Transform[Count];
    readonly float[] dashT = new float[Count];
    float age, next;
    int turn;

    void Start()
    {
        for (int i = 0; i < Count; i++) orbs[i] = Fx.Play("fx_orb", owner.position, 1.1f, color, 12f, 0f, 18, true, life);
    }

    Vector3 Home(int i) => owner.position + (Vector3)(Quaternion.Euler(0, 0, age * 200f + i * 72f) * Vector2.right * 2.2f);

    Transform PickTarget()
    {
        targets.RemoveAll(e => e == null || e.IsDead);
        if (targets.Count > 0) return targets[Random.Range(0, targets.Count)].transform;
        return Specials.NearestEnemy(owner.position, 14f);
    }

    void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }
        age += Time.deltaTime;

        // 0.25초마다 한 구슬씩 출격
        if (age >= next)
        {
            next = age + 0.25f;
            int i = turn++ % Count;
            if (chasing[i] == null) { chasing[i] = PickTarget(); dashT[i] = 0f; }
        }

        for (int i = 0; i < Count; i++)
        {
            if (orbs[i] == null) continue;
            Transform t = chasing[i];
            if (t == null)
            {
                orbs[i].transform.position = Vector3.MoveTowards(orbs[i].transform.position, Home(i), 30f * Time.deltaTime);
                continue;
            }
            orbs[i].transform.position = Vector3.MoveTowards(orbs[i].transform.position, t.position, 28f * Time.deltaTime);
            dashT[i] += Time.deltaTime;
            if (Vector2.Distance(orbs[i].transform.position, t.position) < 0.6f || dashT[i] > 0.8f)
            {
                if (dashT[i] <= 0.8f)
                {
                    Specials.Damage(t.gameObject, damage, Vector3.zero, 0.5f);
                    Fx.Play("fx_soulburst", t.position, 2f, Color.white, 22f);
                }
                chasing[i] = null;
            }
        }
        if (age >= life) Destroy(gameObject);
    }
}

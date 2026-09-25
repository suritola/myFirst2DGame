using UnityEngine;

// 플레이어 모습: 들고 있는 무기에 따라 손에 드는 무기 도트가 바뀌고(Resources/Weapons),
// 쏠 때마다 무기마다 다른 발사 연출 (반동 · 불티 · 탄피 · 연기 등)
// 거너는 총을 지운 캐릭터 그림(Resources/Characters/gunner_nogun)을 쓰고, 기본 권총도 따로 들어 마우스를 따라 겨눔
public class PlayerLook : MonoBehaviour
{
    public static PlayerLook Instance { get; private set; }

    PlayerController player;
    SpriteRenderer body;
    SpriteRenderer held;
    int shownWeapon = -99;
    // 거너: 애니메이션 프레임 이름 → 총을 지운 같은 프레임
    readonly System.Collections.Generic.Dictionary<string, Sprite> noGun = new System.Collections.Generic.Dictionary<string, Sprite>();
    Vector2 aim = Vector2.right;
    float kick, kickAngle;        // 반동: 뒤로 밀린 거리 · 들린 각도 (점점 돌아옴)

    // 무기마다: 반동 거리, 들리는 각도, 손에서 총구까지 길이
    static float Recoil(int id) => id switch
    {
        SpecialAbilities.ShotgunId => 0.45f, SpecialAbilities.SniperId => 0.6f, SpecialAbilities.DualId => 0.15f,
        SpecialAbilities.FlameId => 0.05f, SpecialAbilities.SeekerId => 0.3f, SpecialAbilities.ChainId => 0.2f,
        SpecialAbilities.ScytheId => 0f, SpecialAbilities.GrenadeId => 0.5f, _ => 0.18f,
    };
    static float Lift(int id) => id switch
    {
        SpecialAbilities.ShotgunId => 22f, SpecialAbilities.SniperId => 16f, SpecialAbilities.GrenadeId => 26f,
        SpecialAbilities.SeekerId => 12f, SpecialAbilities.FlameId => 2f, SpecialAbilities.ScytheId => -70f, _ => 8f,
    };

    public static void Attach(PlayerController p)
    {
        if (p == null || p.GetComponent<PlayerLook>() != null) return;
        p.gameObject.AddComponent<PlayerLook>();
    }

    void Awake()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        body = GetComponent<SpriteRenderer>();
        GameObject go = new GameObject("HeldWeapon");
        go.transform.SetParent(transform, false);
        held = go.AddComponent<SpriteRenderer>();
        if (body != null)
        {
            held.sortingLayerID = body.sortingLayerID;
            held.sortingOrder = body.sortingOrder + 1;
        }
        held.enabled = false;
        foreach (Sprite s in Resources.LoadAll<Sprite>("Characters/gunner_nogun")) noGun[s.name] = s;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // -1 = 기본 권총, -2 = 빈손 (낫을 던진 동안)
    int CurrentWeapon => player != null && player.special != null ? player.special.HeldWeapon : -1;

    void LateUpdate()
    {
        if (player == null || body == null) return;
        // 캐릭터 그림에 그려진 권총은 지우고 (애니메이션이 이번 프레임에 고른 그림을 같은 이름의 총 없는 그림으로)
        if (body.sprite != null && noGun.TryGetValue(body.sprite.name, out Sprite bare)) body.sprite = bare;

        int id = CurrentWeapon;
        if (id != shownWeapon)
        {
            shownWeapon = id;
            held.sprite = id >= 0 ? Resources.Load<Sprite>("Weapons/weapon_" + id)
                        : id == -1 ? Resources.Load<Sprite>("Weapons/weapon_pistol") : null;
        }
        // 죽는 연출 · 숨김(시작 연출) 중에는 몸과 함께 숨김
        held.enabled = held.sprite != null && body.enabled;
        if (!held.enabled) return;

        if (player.special != null) aim = player.special.AimDirection;
        if (aim.sqrMagnitude < 0.01f) aim = body.flipX ? Vector2.left : Vector2.right;
        // 몸도 겨누는 쪽을 바라봄 (총은 왼쪽, 몸은 오른쪽을 보는 어색함이 없게)
        if (Mathf.Abs(aim.x) > 0.05f) body.flipX = aim.x < 0f;

        float dt = Time.unscaledDeltaTime;
        kick = Mathf.MoveTowards(kick, 0f, dt * 5f);
        kickAngle = Mathf.MoveTowards(kickAngle, 0f, dt * 260f);

        bool left = aim.x < 0f;
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + (left ? -kickAngle : kickAngle);
        Vector3 hand = new Vector3(left ? -0.35f : 0.35f, -0.55f, 0f);
        held.transform.localPosition = hand - (Vector3)(aim * kick);
        held.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        held.flipY = left;                           // 왼쪽을 볼 때 무기가 뒤집히지 않게
        held.color = body.color;                     // 피격 깜빡임을 몸과 같이
    }

    // 총구 끝 (들고 있는 무기 그림의 앞쪽 끝): 총알도 여기서 나감
    public bool HasTip => held != null && held.enabled && held.sprite != null;
    public Vector3 TipPosition => Tip();

    Vector3 Tip()
    {
        if (!HasTip) return player.BaseMuzzle;
        float len = held.sprite.bounds.size.x * (1f - held.sprite.pivot.x / held.sprite.rect.width);
        return held.transform.position + (Vector3)(aim * len);
    }

    // 무기를 쏠 때 (SpecialAbilities · PlayerController 가 부름). id = -1 이면 기본 권총
    public static void Fired(int id)
    {
        if (Instance != null) Instance.OnFired(id);
    }

    void OnFired(int id)
    {
        kick = Recoil(id);
        kickAngle = Lift(id);
        Vector3 tip = Tip();
        float rot = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        Vector2 back = -aim;
        Vector2 side = new Vector2(-aim.y, aim.x);
        switch (id)
        {
            case SpecialAbilities.ShotgunId:
                // 불티가 부채꼴로 튀고 연기
                for (int i = 0; i < 6; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f)) * aim;
                    FlameParticle.Spawn(Hostile.Glow, tip, d * Random.Range(5f, 9f), Random.Range(0.18f, 0.3f), 0.06f, 0.02f, false);
                }
                Fx.Play("fx_smoke", tip, 1.6f, new Color(0.6f, 0.55f, 0.5f, 0.6f), 16f);
                Eject(new Color(0.85f, 0.25f, 0.15f));
                break;
            case SpecialAbilities.SniperId:
                // 청록 고리가 총구에서 퍼지고 긴 섬광
                Fx.Play("fx_shock", tip, 1.8f, new Color(0.5f, 0.95f, 1f, 0.9f), 24f);
                Fx.Play("fx_trail_dot", tip + (Vector3)(aim * 0.6f), 1f, new Color(0.8f, 1f, 1f), 1f, rot, 16, false, 0.08f);
                Eject(new Color(0.6f, 0.9f, 1f));
                break;
            case SpecialAbilities.DualId:
                Eject(new Color(1f, 0.85f, 0.4f));
                Fx.Play("fx_sparkle", tip, 0.7f, new Color(1f, 0.9f, 0.5f), 24f, 0f, 16);
                break;
            case SpecialAbilities.FlameId:
                FlameParticle.Spawn(Hostile.Glow, tip, aim * 3f + side * Random.Range(-1f, 1f), 0.25f, 0.05f, 0.12f, false);
                break;
            case SpecialAbilities.SeekerId:
                Fx.Play("fx_orb", tip, 1.2f, new Color(0.8f, 0.6f, 1f), 20f);
                Fx.Play("fx_sparkle", tip + (Vector3)(side * 0.3f), 0.6f, new Color(0.85f, 0.7f, 1f), 18f, 0f, 16);
                break;
            case SpecialAbilities.ChainId:
                Fx.Play("fx_markbolt", tip, 1f, new Color(0.7f, 0.95f, 1f), 24f, Random.Range(-20f, 20f), 16);
                break;
            case SpecialAbilities.ScytheId:
                // 던지는 동작: 낫이 한 바퀴 휘둘러짐 (손에 든 낫은 잠깐 사라졌다 돌아옴)
                Fx.Play("fx_slash", transform.position + (Vector3)(aim * 0.8f), 2.4f, new Color(0.85f, 0.6f, 1f), 26f, rot, 16);
                break;
            case SpecialAbilities.GrenadeId:
                Fx.Play("fx_smoke", tip, 2f, new Color(0.45f, 0.35f, 0.3f, 0.7f), 14f);
                FlameParticle.Spawn(Hostile.Glow, tip, aim * 4f, 0.25f, 0.08f, 0.02f, false);
                break;
            default:
                Eject(new Color(0.95f, 0.8f, 0.4f));
                break;
        }
    }

    // 탄피가 옆으로 튀어 떨어짐
    void Eject(Color c)
    {
        Vector3 at = held != null && held.enabled ? held.transform.position : player.BaseMuzzle;
        Vector2 side = new Vector2(-aim.y, aim.x) * (aim.x < 0f ? -1f : 1f);
        Casing.Spawn(at, (side * 2.5f + Vector2.up * 3f) + Random.insideUnitCircle * 0.8f, c);
    }
}

// 튀어나와 떨어지며 사라지는 탄피 (작은 도트)
public class Casing : MonoBehaviour
{
    Vector2 v;
    float age;
    SpriteRenderer sr;

    public static void Spawn(Vector3 pos, Vector2 velocity, Color color)
    {
        GameObject go = SpecialAbilities.MakeSprite("Casing", SpecialAbilities.GlowSprite, pos, 0.03f, color, "Effect", 6);
        Casing c = go.AddComponent<Casing>();
        c.v = velocity;
        c.sr = go.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        age += Time.deltaTime;
        v.y -= 14f * Time.deltaTime;
        transform.position += (Vector3)(v * Time.deltaTime);
        transform.Rotate(0f, 0f, 720f * Time.deltaTime);
        if (sr != null) { Color c = sr.color; c.a = Mathf.Clamp01(1f - (age - 0.25f) / 0.2f); sr.color = c; }
        if (age > 0.45f) Destroy(gameObject);
    }
}

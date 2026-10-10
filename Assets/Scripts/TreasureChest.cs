using System.Collections;
using UnityEngine;

// 보물 상자 (1.0.5): 습격을 이겨 내거나 중간 보스 · 엘리트를 쓰러뜨리면 나옴 (ChapterEvents)
// 다가가면 열리고 셋 중 하나를 고름: 코인 · 영혼 조각 · 체력 모두 회복 + 최대 체력 +10
public class TreasureChest : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    const float OpenRange = 1.6f, Size = 1.6f;

    GameObject label;
    SpriteRenderer sr;
    Vector3 home;
    bool opening;

    public static TreasureChest Spawn(Vector3 at)
    {
        at = Hostile.ClampArena(at);
        // 상자 그림 (tools/pixelart/make_choice_icons.py 의 chest) · 뒤에 금빛
        Sprite art = Resources.Load<Sprite>("Icons/choice_chest");
        GameObject go = SpecialAbilities.MakeSprite("TreasureChest", art != null ? art : Hostile.Glow, at, Size, art != null ? Color.white : Gold, "Effect", 8);
        GameObject halo = SpecialAbilities.MakeSprite("ChestGlow", Hostile.Glow, at, 0.35f, new Color(1f, 0.82f, 0.3f, 0.45f), "Effect", 7);
        halo.transform.SetParent(go.transform, true);
        TreasureChest c = go.AddComponent<TreasureChest>();
        c.sr = go.GetComponent<SpriteRenderer>();
        c.home = at;
        c.label = SkillTag.Show(go.transform, Loc.T("보물 상자"), 1.6f);
        Fx.Spawn("fx_soulburst", at, 3.5f, Gold, 18f);
        ShockRing.Spawn(at, 0.4f, 3f, 0.4f, Gold, 0.3f);
        Hostile.Play("chime", 0.8f, 1.3f);
        if (StageManager.Instance != null) StageManager.Instance.ShowBanner(Loc.T("보물 상자가 나타났다!"), 2f);
        return c;
    }

    void Update()
    {
        if (opening) return;
        transform.position = home + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.localScale = Vector3.one * Size * (1f + 0.06f * Mathf.Sin(Time.time * 6f));
        if (Random.value < Time.deltaTime * 4f) Fx.Spawn("fx_sparkle", home + (Vector3)(Random.insideUnitCircle * 0.8f), 0.8f, Gold, 20f);
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying || Time.timeScale == 0f) return;
        if (Vector2.Distance(p.transform.position, home) <= OpenRange) StartCoroutine(OpenChest(p));
    }

    IEnumerator OpenChest(PlayerController p)
    {
        opening = true;
        Fx.Spawn("fx_shock", home, 5f, Gold, 18f);
        Hostile.Play("shimmer", 0.8f, 1.2f);
        int slot = StageManager.Instance != null ? Chapters.SlotOf(StageManager.Instance.CurrentStage) : 0;
        int coins = 20 + 10 * slot, shards = 20 + 10 * slot;
        ChoiceUI.Option[] options =
        {
            new ChoiceUI.Option("황금", Loc.T("코인 +{0}").Replace("{0}", coins.ToString()), new Color(1f, 0.82f, 0.3f), "gold"),
            new ChoiceUI.Option("영혼", Loc.T("영혼 조각 +{0}").Replace("{0}", shards.ToString()), new Color(0.55f, 0.9f, 1f), "soul"),
            new ChoiceUI.Option("생명", "체력을 모두 회복하고 최대 체력 +10", new Color(1f, 0.45f, 0.45f), "life"),
        };
        int pick = 0;
        yield return ChoiceUI.Run("보물 상자", "하나를 고르세요", options, i => pick = i);
        if (pick == 0)
        {
            Coin coin = FindFirstObjectByType<Coin>();
            if (coin != null) coin.AddCoin(coins);
        }
        else if (pick == 1) SoulShards.Add(shards, home, false);
        else if (p != null)
        {
            p.PlayerMaxHealth += 10f;
            p.PlayerHealth = p.PlayerMaxHealth;
            RunSave.Record("h10");                  // 판 중간 저장: 이어할 때 최대 체력도 다시 (RunSave 'h')
        }
        Fx.Spawn("fx_sparkle", home, 3f, Color.white, 20f);
        if (label != null) Destroy(label);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (label != null) Destroy(label);
    }
}

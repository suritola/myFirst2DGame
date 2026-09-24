using UnityEngine;

// 도감에 보여줄 자료 (Resources/CodexData.asset)
// 특수 능력 · 레벨업 아이콘 · 적 프리팹은 게임 씬에만 있어서, 메인 메뉴에서도 읽을 수 있게 여기에 한 번 더 연결해 둠
[CreateAssetMenu(fileName = "CodexData", menuName = "Game/Codex Data")]
public class CodexData : ScriptableObject
{
    [Header("특수 능력 (SpecialAbilities.abilities와 같은 순서 = ID)")]
    public SpecialDef[] specials;

    [Header("레벨업 능력 아이콘 (AbilityHUD.icons와 같은 순서)")]
    public Sprite[] abilityIcons;

    [Header("적 (스테이지 순서) · 각 적이 나오는 스테이지 번호 (0부터)")]
    public GameObject[] enemies;
    public int[] enemyStage;

    [Header("보스 (스테이지 순서)")]
    public GameObject[] bosses;

    static CodexData cached;
    public static CodexData Load() => cached != null ? cached : (cached = Resources.Load<CodexData>("CodexData"));
}

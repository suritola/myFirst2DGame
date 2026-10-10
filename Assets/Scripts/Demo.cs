#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX) && !DISABLESTEAMWORKS
#define STEAM
#endif

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if STEAM
using Steamworks;
#endif

// 체험판 (1.9.2~): 빌드할 때 -demo 를 주면 SOULSAVER_DEMO 가 켜짐 (BuildScript, tools/steam-upload.ps1 -Demo)
//   1장 지하 묘역 + 리치 왕까지 · 거너 · 검사 · 쉬움 난이도
//   리치 왕을 쓰러뜨리면 (거너는 무기 진화까지 보여 준 뒤) 「정식판에서 계속」 화면과 위시리스트 버튼
//   2.2.1~: 메인 메뉴 · 게임 오버 오른쪽 위에도 위시리스트 버튼 (끝 화면까지 가지 않고 그만두는 사람이 많아서)
//   제품 이름이 Soul Saver Demo 라 저장 데이터는 정식판과 따로
public static class Demo
{
#if SOULSAVER_DEMO
    public static bool On => true;
#else
    public static bool On => false;
#endif

    public static string StoreUrl => "https://store.steampowered.com/app/" + SteamManager.FullAppId;

    // 체험판에서 고를 수 있는 캐릭터
    public static bool CharacterAllowed(CharacterId id) => !On || id == CharacterId.Gunner || id == CharacterId.Swordsman;

    // 정식판 상점 페이지 (스팀 오버레이가 되면 오버레이, 아니면 브라우저)
    public static void OpenStore()
    {
#if STEAM
        if (SteamManager.Initialized && SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToStore(new AppId_t(SteamManager.FullAppId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
            return;
        }
#endif
        Application.OpenURL(StoreUrl);
    }

    // 화면 구석에 붙는 금색 「위시리스트에 추가」 버튼 (체험판에서만, 정식판은 null)
    public static Button AddWishlistButton(Transform parent, Vector2 edge, Vector2 offset)
    {
        if (!On || parent == null) return null;
        UIKit.EnsureStyle();
        Button b = UIKit.MakeButton(parent, "위시리스트에 추가", Vector2.zero, new Vector2(300f, 64f), OpenStore, 26f);
        b.name = "WishlistButton";
        b.GetComponent<Image>().color = new Color(1f, 0.85f, 0.45f);
        RectTransform r = (RectTransform)b.transform;
        r.pivot = edge;
        UIKit.Pin(r, edge, offset);
        return b;
    }

    // ================================================================= 체험판 끝 화면
    static GameObject endPanel;

    public static void ShowEnd()
    {
        if (endPanel != null) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        PlayerController p = Hostile.Player;
        if (p != null) p.CancelSkill();
        Time.timeScale = 0f;

        RectTransform win = UIKit.Modal(canvas.transform, "DemoEnd", new Vector2(1100f, 700f), out endPanel);
        Color gold = new Color(0.96f, 0.83f, 0.47f), parch = new Color(0.93f, 0.9f, 0.84f);
        UIKit.Text(win, "체험판은 여기까지입니다", 54f, gold, new Vector2(0f, 250f), new Vector2(980f, 80f));
        UIKit.Text(win, "리치 왕을 쓰러뜨렸습니다! 고마워요.", 30f, parch, new Vector2(0f, 165f), new Vector2(980f, 50f));
        UIKit.Text(win,
            "정식판에서는 불타는 지옥과 초원, 지옥의 군주와 킹 슬라임,\n" +
            "영웅 5명 · 두 번째 무기 진화 · 보통 / 어려움 난이도 · 무한 모드가 기다립니다.",
            26f, parch, new Vector2(0f, 50f), new Vector2(980f, 120f));
        UIKit.Text(win, "위시리스트에 추가하면 출시할 때 알려 드립니다.", 26f, new Color(0.6f, 0.9f, 1f), new Vector2(0f, -60f), new Vector2(980f, 50f));

        Button wish = UIKit.MakeButton(win, "위시리스트에 추가", new Vector2(-200f, -200f), new Vector2(360f, 90f), OpenStore, 30f);
        wish.GetComponent<Image>().color = new Color(1f, 0.85f, 0.45f);
        UIKit.MakeButton(win, "메인 메뉴로", new Vector2(200f, -200f), new Vector2(300f, 90f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }, 30f);
        Hostile.Play("levelup", 0.8f);
    }

    // 메인 메뉴 버전 글 옆 표시
    public static string Label => On ? "  " + Loc.T("체험판") : "";
}

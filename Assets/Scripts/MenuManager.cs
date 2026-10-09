using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // 저장된 판이 있으면 한 번 묻고 새 판 (2.1.9 판 중간 저장: 새 판을 시작하면 저장된 판은 지워짐)
    GameObject confirm;

    public void OnPressStart()
    {
        if (!RunSave.Has) { SceneManager.LoadScene("GameScene"); return; }
        if (confirm != null) return;
        GameObject start = GameObject.Find("GameStartButton");
        Transform root = start != null ? start.transform.root : transform.root;
        UIKit.EnsureStyle();
        RectTransform win = UIKit.Modal(root, "ConfirmNewRun", new Vector2(820f, 340f), out confirm);
        UIKit.Text(win, "새 게임을 시작할까요?", 38f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 78f), new Vector2(760f, 60f));
        UIKit.Text(win, "저장된 판(이어하기)은 지워집니다.", 26f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, 20f), new Vector2(760f, 40f));
        UIKit.MakeButton(win, "새 게임", new Vector2(-150f, -90f), new Vector2(260f, 76f), () =>
        {
            RunSave.Delete();
            SceneManager.LoadScene("GameScene");
        }, 28f);
        UIKit.MakeButton(win, "취소", new Vector2(150f, -90f), new Vector2(260f, 76f), () =>
        {
            if (confirm != null) Destroy(confirm);
            confirm = null;
        }, 28f);
    }

    public void OnPressExit()
    {
        Application.Quit();
    }
}

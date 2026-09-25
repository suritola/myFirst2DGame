using UnityEngine;

// 마우스 · 이동 입력을 한곳에서 읽음
// 평소에는 Unity Input 그대로, 트레일러 촬영(Auto)일 때는 TrailerDirector 가 넣어 주는 값을 씀
public static class GameInput
{
    public static bool Auto;                // 자동 조종 중

    // 트레일러 촬영 중인지 (TrailerDirector는 에디터 전용이라 출시 빌드에서는 항상 false)
#if UNITY_EDITOR
    public static bool TrailerRunning => TrailerDirector.Running;
#else
    public static bool TrailerRunning => false;
#endif

    // 자동 조종 값 (TrailerDirector 가 채움)
    public static Vector2 AutoMove;         // 이동 방향 (-1 ~ 1)
    public static Vector3 AutoAim;          // 조준할 월드 좌표
    public static bool AutoFire;            // 좌클릭 누르고 있음
    public static bool AutoUlt;             // 우클릭 누르고 있음
    // 눌렀다 / 뗐다 신호는 "이 프레임에 일어남"으로 표시 (다른 스크립트의 Update 순서와 상관없게)
    public static int FireDownFrame = -1, FireUpFrame = -1, UltDownFrame = -1, UltUpFrame = -1;

    public static Vector3 MousePosition
    {
        get
        {
            if (!Auto) return Input.mousePosition;
            Camera cam = Camera.main;
            return cam != null ? cam.WorldToScreenPoint(AutoAim) : Vector3.zero;
        }
    }

    public static bool FireDown => Auto ? Time.frameCount == FireDownFrame : Input.GetMouseButtonDown(0);
    public static bool FireHeld => Auto ? AutoFire : Input.GetMouseButton(0);
    public static bool FireUp => Auto ? Time.frameCount == FireUpFrame : Input.GetMouseButtonUp(0);

    public static bool UltDown => Auto ? Time.frameCount == UltDownFrame : Input.GetMouseButtonDown(1);
    public static bool UltHeld => Auto ? AutoUlt : Input.GetMouseButton(1);
    public static bool UltUp => Auto ? Time.frameCount == UltUpFrame : Input.GetMouseButtonUp(1);
}

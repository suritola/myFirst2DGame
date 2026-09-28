using System.Diagnostics;
using UnityEngine;

// 제품 이름을 Gun Saver → Soul Saver 로 바꾸면서 저장 위치(레지스트리 키)가 바뀜
// 처음 실행할 때 옛 저장(캐릭터 해금 · 포인트 · 설정 · 난이도 해금)을 새 위치로 한 번 복사함
// (.NET Standard 2.1 이라 Registry API 대신 윈도우 기본 도구 reg.exe 를 씀)
public static class SaveMigration
{
    public const string OldProduct = "Gun Saver";
    const string DoneKey = "save.migratedFromGunSaver";

    // 다른 코드가 저장을 읽기 전에 (스팀 매니저 · 설정보다 먼저)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Run()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (Application.productName == OldProduct || PlayerPrefs.GetInt(DoneKey, 0) == 1) return;
        string root = Application.isEditor ? @"HKCU\Software\Unity\UnityEditor\" : @"HKCU\Software\";
        string from = root + Application.companyName + @"\" + OldProduct;
        string to = root + Application.companyName + @"\" + Application.productName;
        // 옛 저장이 있을 때만 복사 (새 저장을 덮지 않도록 같은 이름은 옛 값이 이김 — 처음 한 번뿐이라 새 값은 아직 없음)
        if (Reg("query \"" + from + "\"") == 0) Reg("copy \"" + from + "\" \"" + to + "\" /s /f");
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
#endif
    }

    static int Reg(string args)
    {
        try
        {
            ProcessStartInfo info = new ProcessStartInfo("reg.exe", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            using (Process p = Process.Start(info))
            {
                if (p == null) return -1;
                if (!p.WaitForExit(5000)) return -1;
                return p.ExitCode;
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning("저장 옮기기 실패: " + e.Message);
            return -1;
        }
    }
}

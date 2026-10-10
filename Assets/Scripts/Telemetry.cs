using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// 익명 플레이 데이터 (2.2.1~): 판이 끝날 때 balance_log.csv 와 같은 한 줄을 수집 서버(구글 Apps Script → 구글 시트)로 보냄
// 개인 정보(스팀 ID · 이름 · 기기 정보)는 보내지 않고, 처음 보낼 때 만든 무작위 번호만 씀 (docs/steam/eula.md 의 개인정보 항목)
// 설정 → 게임 「플레이 데이터 보내기」로 끌 수 있음. 보내지 못한 줄은 파일에 모아 두었다가 다음에 다시 보냄
// 서버 만드는 법 · 분석: tools/telemetry/README.md
public class Telemetry : MonoBehaviour
{
    // tools/telemetry/README.md 대로 배포한 웹 앱 주소 (비어 있으면 보내지 않고 모아 두기만 함)
    const string Endpoint = "";
    const string Token = "soulsaver-telemetry-v1";      // Code.gs 의 TOKEN 과 같게
    const string IdKey = "telemetry.id";
    const int MaxPending = 200;

    static Telemetry runner;
    static bool sending;
    static string PendingPath => Path.Combine(Application.persistentDataPath, "telemetry_pending.txt");

    // 플레이어 구분용 무작위 번호 (스팀 ID 와 상관없음)
    static string AnonId
    {
        get
        {
            string id = Prefs.GetString(IdKey, "");
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
                Prefs.SetString(IdKey, id);
                Prefs.Save();
            }
            return id;
        }
    }

    // 게임을 켤 때 지난번에 못 보낸 줄을 보냄
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() => Flush();

    // 판이 끝났을 때 (RunStats.WriteBalanceLog): 한 줄을 모아 두고 바로 보내 봄
    public static void Send(string header, string row)
    {
        if (!GameSettings.ShareData || Application.isEditor || Application.isBatchMode) return;
        try
        {
            string json = "{\"token\":\"" + Esc(Token) + "\",\"id\":\"" + Esc(AnonId) + "\",\"header\":\"" + Esc(header) + "\",\"row\":\"" + Esc(row) + "\"}";
            List<string> lines = ReadPending();
            lines.Add(json);
            if (lines.Count > MaxPending) lines.RemoveRange(0, lines.Count - MaxPending);     // 오래 못 보냈으면 오래된 것부터 버림
            File.WriteAllLines(PendingPath, lines, new UTF8Encoding(false));
        }
        catch (System.Exception ex) { Debug.LogWarning("telemetry: " + ex.Message); return; }
        Flush();
    }

    // 설정에서 껐을 때: 모아 둔 줄도 지움
    public static void ClearPending()
    {
        try { if (File.Exists(PendingPath)) File.Delete(PendingPath); } catch { }
    }

    static void Flush()
    {
        if (sending || string.IsNullOrEmpty(Endpoint) || !GameSettings.ShareData || Application.isEditor || Application.isBatchMode) return;
        if (!File.Exists(PendingPath)) return;
        if (runner == null)
        {
            GameObject go = new GameObject("Telemetry");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            runner = go.AddComponent<Telemetry>();
        }
        runner.StartCoroutine(runner.SendAll());
    }

    IEnumerator SendAll()
    {
        sending = true;
        List<string> lines = ReadPending();
        int sent = 0;
        foreach (string json in lines)
        {
            using (UnityWebRequest req = new UnityWebRequest(Endpoint, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 15;
                yield return req.SendWebRequest();
                // 인터넷이 없거나 서버 오류(5xx · 429)면 다음에 다시. 4xx 는 다시 보내도 안 되므로 버림
                long code = req.responseCode;
                bool retry = req.result == UnityWebRequest.Result.ConnectionError || code == 0 || code == 429 || code >= 500;
                if (retry) break;
            }
            sent++;
        }
        try
        {
            List<string> rest = ReadPending();      // 보내는 동안 새로 쌓인 줄도 남김
            rest.RemoveRange(0, Mathf.Min(sent, rest.Count));
            if (rest.Count == 0) ClearPending();
            else File.WriteAllLines(PendingPath, rest, new UTF8Encoding(false));
        }
        catch (System.Exception ex) { Debug.LogWarning("telemetry: " + ex.Message); }
        sending = false;
    }

    static List<string> ReadPending()
    {
        List<string> lines = new List<string>();
        if (File.Exists(PendingPath))
            foreach (string l in File.ReadAllLines(PendingPath, Encoding.UTF8)) if (l.Length > 0) lines.Add(l);
        return lines;
    }

    static string Esc(string s)
    {
        StringBuilder b = new StringBuilder(s.Length + 8);
        foreach (char c in s)
        {
            if (c == '"' || c == '\\') b.Append('\\').Append(c);
            else if (c < ' ') b.Append(' ');
            else b.Append(c);
        }
        return b.ToString();
    }
}

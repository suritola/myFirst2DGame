using UnityEngine;
using UnityEngine.SceneManagement;

// 배경음악: 메뉴 / 1장 / 2장 / 3장 / 보스전 곡을 상황에 맞게 부드럽게 바꿔 틀어줌
// 게임이 시작되면 스스로 만들어지고 씬이 바뀌어도 유지됨 (Resources/Music/bgm_*.wav)
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    // 음악은 효과음보다 조금 작게 (전체 볼륨은 설정의 AudioListener.volume)
    const float MusicGain = 0.5f;
    const float FadeTime = 1.2f;

    AudioSource current, previous;
    string playing;
    EnemySpawner spawner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicManager>();
    }

    void Awake()
    {
        Instance = this;
        current = NewSource();
        previous = NewSource();
        SceneManager.sceneLoaded += (s, m) => spawner = null;
    }

    AudioSource NewSource()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.loop = true;
        s.playOnAwake = false;
        s.volume = 0f;
        s.spatialBlend = 0f;
        return s;
    }

    // 지금 틀어야 할 곡
    string Wanted()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != "GameScene") return "bgm_menu";
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null && spawner.bossSpawned && !spawner.bossCleared) return "bgm_boss";
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
        return stage == 0 ? "bgm_cave" : stage == 1 ? "bgm_hell" : "bgm_meadow";
    }

    void Update()
    {
        string want = Wanted();
        if (want != playing)
        {
            AudioClip clip = Resources.Load<AudioClip>("Music/" + want);
            if (clip != null)
            {
                // 두 소스를 바꿔 가며 교차 페이드
                AudioSource t = previous; previous = current; current = t;
                current.clip = clip;
                current.volume = 0f;
                current.Play();
            }
            playing = want;
        }

        float step = Time.unscaledDeltaTime / FadeTime * MusicGain;
        current.volume = Mathf.MoveTowards(current.volume, MusicGain, step);
        previous.volume = Mathf.MoveTowards(previous.volume, 0f, step);
        if (previous.volume <= 0f && previous.isPlaying) previous.Stop();
    }
}

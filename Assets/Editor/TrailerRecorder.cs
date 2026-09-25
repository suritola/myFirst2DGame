using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메뉴 Soul Saver → 트레일러 녹화
// 게임 씬을 열고 재생하면 TrailerDirector 가 트레일러를 연기하고, Unity Recorder 가 60fps로 녹화해서
// Recordings/GunSaver_Trailer.mp4 를 만든 뒤 재생을 멈춤
[InitializeOnLoad]
public static class TrailerRecorder
{
    const string ScenePath = "Assets/Prefabs/Scenes/GameScene.unity";
    const string RecordFlag = "trailer.record";
    static RecorderController controller;

    static TrailerRecorder()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += Tick;
    }

    [MenuItem("Soul Saver/트레일러 녹화")]
    static void Record() => Begin(true);

    [MenuItem("Soul Saver/트레일러 미리보기 (녹화 없이)")]
    static void Preview() => Begin(false);

    static void Begin(bool record)
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        PlayerPrefs.SetInt(TrailerDirector.PrefKey, 1);
        PlayerPrefs.Save();
        SessionState.SetBool(RecordFlag, record);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RecordFlag, false))
        {
            SessionState.SetBool(RecordFlag, false);
            StartRecording();
        }
        if (state == PlayModeStateChange.ExitingPlayMode && controller != null)
        {
            controller.StopRecording();
            controller = null;
        }
    }

    static void StartRecording()
    {
        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "GunSaver Trailer";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
        };
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        movie.AudioInputSettings.PreserveAudio = true;
        movie.OutputFile = OutputPath();

        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = 60f;
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.CapFrameRate = true;

        controller = new RecorderController(settings);
        controller.PrepareRecording();
        controller.StartRecording();
        Debug.Log("[Trailer] 녹화 시작 → " + OutputPath() + ".mp4");
    }

    // 확장자 없이 (Recorder 가 .mp4 를 붙임)
    public static string OutputPath()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "GunSaver_Trailer");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !TrailerDirector.Finished) return;
        if (controller != null)
        {
            controller.StopRecording();
            controller = null;
            Debug.Log("[Trailer] 녹화 완료 → " + OutputPath() + ".mp4");
            EditorUtility.RevealInFinder(OutputPath() + ".mp4");
        }
        EditorApplication.isPlaying = false;
    }
}

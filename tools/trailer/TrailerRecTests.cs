using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 배치 모드 트레일러 녹화: 매 프레임 카메라 RenderTexture 를 읽어 MediaEncoder 로 MP4 인코딩 (음악은 보스전 곡)
public class TrailerRecTests
{
    static readonly string Out = Environment.GetEnvironmentVariable("TRAILER_OUT") ?? "C:/Temp/GunSaver_Trailer";
    const int W = 1920, H = 1080, Fps = 60;

    static void CanvasesToCamera(Camera cam)
    {
        SortingLayer[] layers = SortingLayer.layers;
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                bool trailer = c.name == "TrailerOverlay";
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = trailer ? 1f : 5f;
                c.sortingLayerName = layers[layers.Length - 1].name;
                c.sortingOrder = trailer ? 32500 : Mathf.Min(32000, 30000 + c.sortingOrder);
            }
    }

    [UnityTest]
    public IEnumerator Record()
    {
        LogAssert.ignoreFailingMessages = true;
        Time.captureFramerate = Fps;
        PlayerPrefs.SetInt("trailer.run", 1);
        SceneManager.LoadScene("GameScene");
        yield return null;
        yield return null;

        Camera cam = Camera.main;
        cam.cullingMask |= 1 << 5;
        RenderTexture rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        CanvasesToCamera(cam);
        Texture2D frame = new Texture2D(W, H, TextureFormat.RGBA32, false);

        AudioClip music = Resources.Load<AudioClip>("Music/bgm_boss");
        float[] samples = new float[music.samples * music.channels];
        music.GetData(samples, 0);
        int ch = music.channels, src = music.frequency, rate = 48000;   // AAC 는 44.1/48kHz 만 지원
        int srcFrames = music.samples;
        var audioAttr = new AudioTrackAttributes { sampleRate = new MediaRational(rate), channelCount = (ushort)ch, language = "en" };
        var videoAttr = new VideoTrackAttributes { frameRate = new MediaRational(Fps), width = W, height = H, includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.High };
        Directory.CreateDirectory(Path.GetDirectoryName(Out));
        string path = Out + ".mp4";
        int perFrame = rate / Fps;
        int cursor = 0, frames = 0;

        Type director = Type.GetType("TrailerDirector, Assembly-CSharp");
        var finished = director.GetProperty("Finished");
        using (var encoder = new MediaEncoder(path, videoAttr, audioAttr))
        using (var audio = new NativeArray<float>(perFrame * ch, Allocator.Persistent))
        {
            Debug.Log("[REC] start " + path + " audio " + rate + "Hz x" + ch);
            while (!(bool)finished.GetValue(null) && frames < Fps * 80)
            {
                yield return null;
                if (cam == null) { cam = Camera.main; cam.cullingMask |= 1 << 5; }
                if (cam.targetTexture != rt) cam.targetTexture = rt;
                CanvasesToCamera(cam);
                cam.Render();
                RenderTexture.active = rt;
                frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                frame.Apply();
                RenderTexture.active = null;
                encoder.AddFrame(frame);

                var buf = audio;
                for (int f = 0; f < perFrame; f++)
                {
                    double pos = (cursor + f) * (double)src / rate;
                    int i0 = (int)pos % srcFrames, i1 = (i0 + 1) % srcFrames;
                    float k = (float)(pos - Math.Floor(pos));
                    for (int c = 0; c < ch; c++) buf[f * ch + c] = Mathf.Lerp(samples[i0 * ch + c], samples[i1 * ch + c], k) * 0.9f;
                }
                cursor += perFrame;
                encoder.AddSamples(buf);

                frames++;
                if (frames % 300 == 0)
                {
                    File.WriteAllBytes(Out + "_" + (frames / Fps).ToString("00") + "s.jpg", frame.EncodeToJPG(80));
                    Debug.Log("[REC] frame " + frames);
                }
            }
        }
        Time.captureFramerate = 0;
        Debug.Log("[REC] done frames=" + frames + " (" + (frames / (float)Fps).ToString("0.0") + "s)");
        yield return null;
    }
}

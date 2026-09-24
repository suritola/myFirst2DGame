using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 명령줄 빌드용 (tools/release.ps1 에서 호출)
// Unity.exe -batchmode -quit -projectPath <경로> -executeMethod BuildScript.BuildWindows -buildOutput <폴더> -buildVersion <버전>
public static class BuildScript
{
    public static void BuildWindows()
    {
        string output = GetArg("-buildOutput") ?? "Builds/Windows";
        string version = GetArg("-buildVersion");
        if (!string.IsNullOrEmpty(version)) PlayerSettings.bundleVersion = version;

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = System.IO.Path.Combine(output, PlayerSettings.productName + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("빌드 결과: " + report.summary.result + ", 크기: " + report.summary.totalSize + " bytes");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}

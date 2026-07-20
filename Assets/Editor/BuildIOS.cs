using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

// Command-line iOS Xcode-project builder for batchmode CI/local builds.
// Usage: Unity -batchmode -quit -buildTarget iOS -executeMethod BuildIOS.Build -outPath /path/to/xcodeProject
// Produces a Unity-generated Xcode project (no signing). Sign & upload in Xcode afterwards.
public static class BuildIOS
{
    public static void Build()
    {
        string[] args = Environment.GetCommandLineArgs();
        string outPath = Arg(args, "-outPath", "/Users/hongdroid/project_unity/SwordAttack/build/iOS");

        // Scenes (enabled only) - mirrors BuildAAB
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        Debug.Log($"[BuildIOS] scenes={string.Join(",", scenes)} out={outPath}");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        Debug.Log($"[BuildIOS] result={summary.result} size={summary.totalSize} time={summary.totalTime} path={summary.outputPath}");
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("[BuildIOS] BUILD SUCCESS");
        else
            Debug.Log($"[BuildIOS] BUILD FAILED: {summary.result}");

        EditorApplication.Exit(summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    static string Arg(string[] args, string key, string fallback = "")
    {
        int i = Array.IndexOf(args, key);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : fallback;
    }
}

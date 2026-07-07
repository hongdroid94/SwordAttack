using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

// Command-line AAB builder for batchmode CI/local builds.
// Usage: Unity -batchmode -quit -executeMethod BuildAAB.Build -storePass X -keyPass Y -outPath Z
public static class BuildAAB
{
    public static void Build()
    {
        string[] args = Environment.GetCommandLineArgs();
        string storePass = Arg(args, "-storePass");
        string keyPass = Arg(args, "-keyPass", storePass);
        string outPath = Arg(args, "-outPath", "/Users/hongdroid/Downloads/SwordAttack-1.2.aab");

        Debug.Log($"[BuildAAB] keystore signing config applied (alias=hongdroid)");

        // Signing
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = "/Users/hongdroid/keystores/hongdroid.keystore";
        PlayerSettings.Android.keyaliasName = "hongdroid";
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasPass = keyPass;

        // AAB output
        EditorUserBuildSettings.buildAppBundle = true;

        // Scenes (enabled only)
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        Debug.Log($"[BuildAAB] result={summary.result} size={summary.totalSize} time={summary.totalTime} path={summary.outputPath}");
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("[BuildAAB] BUILD SUCCESS");
        else
            Debug.Log($"[BuildAAB] BUILD FAILED: {summary.result}");

        EditorApplication.Exit(summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    static string Arg(string[] args, string key, string fallback = "")
    {
        int i = Array.IndexOf(args, key);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : fallback;
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Called by the GitHub workflow: BUILD_METHOD=AutoBuild.BuildAndroid
// Also available from the menu: RIPPED > Build Android APK
public static class AutoBuild
{
    [MenuItem("RIPPED/Build Android APK")]
    public static void BuildAndroid()
    {
        string[] args = Environment.GetCommandLineArgs();

        string outPath = Arg(args, "-customBuildPath");
        if (string.IsNullOrEmpty(outPath)) outPath = "build/Android/Android.apk";
        string dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // Version info passed in by game-ci
        string ver = Arg(args, "-buildVersion");
        if (!string.IsNullOrEmpty(ver)) PlayerSettings.bundleVersion = ver;
        int code;
        if (int.TryParse(Arg(args, "-androidVersionCode"), out code))
            PlayerSettings.Android.bundleVersionCode = code;

        // Quest-friendly Android settings
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel29)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

        // Scenes: whatever is enabled in Build Settings, else everything in Assets/Scenes
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        if (scenes.Count == 0)
        {
            scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath).ToList();
        }
        if (scenes.Count == 0)
        {
            Debug.LogError("[AutoBuild] No scenes found. Add your scene to File > Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        // Generate the RIPPED map into the main scene if it isn't there yet
        try
        {
            RippedMapBuilder.EnsureMapInScene(scenes[0]);
        }
        catch (Exception e)
        {
            Debug.LogError("[AutoBuild] Map builder failed, building anyway: " + e);
        }

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[AutoBuild] Result: " + report.summary.result + ", size: " + report.summary.totalSize);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static string Arg(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == key)
            {
                string v = args[i + 1];
                return v.StartsWith("-") ? null : v;
            }
        }
        return null;
    }
}

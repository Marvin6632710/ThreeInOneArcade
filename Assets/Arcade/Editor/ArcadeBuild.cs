using System.IO;
using System.Linq;
using Arcade;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArcadeBuild
{
    [MenuItem("Arcade/Prepare Main Menu and Build Settings")]
    public static void Prepare()
    {
        if (!File.Exists(GameCatalog.MenuScene)) {
            Directory.CreateDirectory("Assets/Arcade/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Menu Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color32(12, 19, 31, 255);
            EditorSceneManager.SaveScene(scene, GameCatalog.MenuScene);
        }
        EditorBuildSettings.scenes = new[] { GameCatalog.MenuScene }.Concat(GameCatalog.Scenes).Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        PlayerSettings.productName = "Arcade Trio";
        PlayerSettings.companyName = "Zwe Khant Lin";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.zwekhantlin.arcadetrio");
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(GameCatalog.MenuScene);
    }

    public static void PrepareBatch() { Prepare(); EditorApplication.Exit(0); }

    [MenuItem("Arcade/Build macOS Game")]
    public static void BuildMac()
    {
        Prepare();
        Directory.CreateDirectory("Build");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
            locationPathName = "Build/Arcade Trio.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        File.WriteAllText("Build/build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    [MenuItem("Arcade/Build Local Menu Preview")]
    public static void BuildMenuPreview()
    {
        Prepare();
        Directory.CreateDirectory("Build/Preview");
        BuildReport report;
        try {
            PlayerSettings.productName = "Arcade Trio Preview";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.zwekhantlin.arcadetrio.preview");
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                locationPathName = "Build/Preview/Arcade Trio Preview.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
        } finally {
            PlayerSettings.productName = "Arcade Trio";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.zwekhantlin.arcadetrio");
            AssetDatabase.SaveAssets();
        }
        File.WriteAllText("Build/Preview/build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}

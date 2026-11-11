using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BlueSappBuild
{
    public const string ScenePath = "Assets/BlueSapp.unity";

    [MenuItem("BlueSapp/Create Start Scene")]
    public static void Prepare()
    {
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(0.025f, 0.035f, 0.09f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save start scene.");
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "BlueSapp";
        PlayerSettings.companyName = "Acefraction";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        AssetDatabase.SaveAssets();
    }

    [MenuItem("BlueSapp/Build Windows Player")]
    public static void BuildWindows()
    {
        Prepare();
        Directory.CreateDirectory("Build");
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Build/BlueSapp.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("BlueSapp build failed: " + report.summary.result);
    }
}

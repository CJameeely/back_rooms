#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WhiteBoxKit
{
    public static class PlaytestBuild
    {
        public static void BuildNarrativePreviewAndGame()
        {
            BuildMac();
            BuildRecording();
        }

        public static void BuildRecording()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Title.unity", "Assets/Scenes/WhiteBox_LastLight.unity", "Assets/Scenes/End.unity" },
                locationPathName = "Builds/Recording/The Last Light.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "RECORDING_CAPTURE" }
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Recording build failed");
        }

        [MenuItem("Tools/WhiteBox/Build macOS Playtest")]
        public static void BuildMac()
        {
            PlayerSettings.productName = "The Last Light";
            PlayerSettings.companyName = "CJ Evan";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Title.unity", "Assets/Scenes/WhiteBox_LastLight.unity", "Assets/Scenes/End.unity" },
                locationPathName = "Builds/The Last Light.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Playtest build failed: " + report.summary.result);
            Debug.Log("Playtest build ready: Builds/The Last Light.app");
        }
    }
}
#endif

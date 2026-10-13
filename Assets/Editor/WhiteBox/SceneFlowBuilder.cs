#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace WhiteBoxKit
{
    /// Step 4: generates Title.unity and End.unity (simple Canvas-only scenes) and fixes up
    /// Build Settings so Title is scene 0, the level is scene 1, End is scene 2 — dropping the
    /// unused default SampleScene.
    public static class SceneFlowBuilder
    {
        const string TitlePath = "Assets/Scenes/Title.unity";
        const string EndPath = "Assets/Scenes/End.unity";
        const string LevelPath = "Assets/Scenes/WhiteBox_LastLight.unity";

        [MenuItem("Tools/WhiteBox/Step 4 - Build Title & End Scenes")]
        public static void Step4_BuildTitleAndEndScenes()
        {
            BuildTitleScene();
            BuildEndScene();
            FixBuildSettings();
            Debug.Log("[WhiteBox] Step 4 complete: Title/End scenes built, Build Settings updated.");
        }

        static void BuildTitleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Canvas canvas = UIBuilder.CreateScreenCanvas("TitleCanvas");
            UIBuilder.AddPanel(canvas.transform, "Background", new Color(0.90f, 0.92f, 0.95f, 1f));

            UIBuilder.AddText(canvas.transform, "GameTitle", "THE LAST LIGHT", 64, TextAlignmentOptions.Center,
                new Color(0.15f, 0.17f, 0.2f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(900, 100));

            UIBuilder.AddText(canvas.transform, "Subtitle",
                "Recover the traces a researcher left behind before the lights go out.", 24,
                TextAlignmentOptions.Center, new Color(0.35f, 0.38f, 0.42f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(900, 60));

            UIBuilder.AddText(canvas.transform, "Credits", "Created by CJ Evan", 20, TextAlignmentOptions.Center,
                new Color(0.5f, 0.52f, 0.56f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(900, 40));

            UIBuilder.AddText(canvas.transform, "Instructions",
                "Find four symbol clues. Match their digits on the exit keypad.\nWalk through the unlocked door to win. You have 3 minutes.\nFlickering lights warn you that time is running out.\n\nWASD  Move    |    Mouse  Look    |    E  Interact\nKeypad: 0-9 / click    |    Enter  Confirm    |    Esc  Close", 25,
                TextAlignmentOptions.Center, new Color(0.4f, 0.43f, 0.47f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1100, 240));

            GameObject managerGO = new GameObject("TitleMenu");
            TitleMenu menu = managerGO.AddComponent<TitleMenu>();

            var (_, startButton, _) = UIBuilder.AddButton(canvas.transform, "StartButton", "Start",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(220, 70),
                new Color(1f, 0.45f, 0f), Color.white, 30f);
            UIBuilder.BindClick(startButton, menu, nameof(TitleMenu.OnStartPressed));

            EnsureFolder();
            EditorSceneManager.SaveScene(scene, TitlePath);
        }

        static void BuildEndScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Canvas canvas = UIBuilder.CreateScreenCanvas("EndCanvas");
            UIBuilder.AddPanel(canvas.transform, "Background", new Color(0.90f, 0.92f, 0.95f, 1f));

            TextMeshProUGUI title = UIBuilder.AddText(canvas.transform, "ResultTitle", "", 42, TextAlignmentOptions.Center,
                new Color(0.15f, 0.17f, 0.2f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1000, 90));
            TextMeshProUGUI sub = UIBuilder.AddText(canvas.transform, "ResultSub", "", 28, TextAlignmentOptions.Center,
                new Color(0.35f, 0.38f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(900, 60));

            GameObject managerGO = new GameObject("EndMenu");
            EndMenu menu = managerGO.AddComponent<EndMenu>();
            menu.titleText = title;
            menu.subText = sub;

            var (retryGO, retryButton, _) = UIBuilder.AddButton(canvas.transform, "RetryRoomButton", "Retry Room",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, -60), new Vector2(200, 60),
                new Color(1f, 0.45f, 0f), Color.white, 24f);
            UIBuilder.BindClick(retryButton, menu, nameof(EndMenu.OnRetryRoomPressed));
            menu.retryButton = retryGO;

            var (playAgainGO, playAgainButton, _) = UIBuilder.AddButton(canvas.transform, "PlayAgainButton", "Play Again",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, -60), new Vector2(200, 60),
                new Color(1f, 0.45f, 0f), Color.white, 24f);
            UIBuilder.BindClick(playAgainButton, menu, nameof(EndMenu.OnPlayAgainPressed));
            menu.playAgainButton = playAgainGO;

            var (_, titleButton, _) = UIBuilder.AddButton(canvas.transform, "TitleButton", "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, -60), new Vector2(200, 60),
                new Color(0.3f, 0.3f, 0.3f), Color.white, 26f);
            UIBuilder.BindClick(titleButton, menu, nameof(EndMenu.OnTitlePressed));

            EnsureFolder();
            EditorSceneManager.SaveScene(scene, EndPath);
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        static void FixBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(TitlePath, true),
                new EditorBuildSettingsScene(LevelPath, true),
                new EditorBuildSettingsScene(EndPath, true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif

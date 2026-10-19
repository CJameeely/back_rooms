#if UNITY_EDITOR || RECORDING_CAPTURE
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit preview harness. Captures real player frames, not a playthrough or proof of puzzle completion.
public class StoryPreviewCapture : MonoBehaviour
{
    string output;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        int index = System.Array.IndexOf(args, "--story-preview");
        if (index < 0 || index + 1 >= args.Length) return;
        Application.runInBackground = true;
        var capture = new GameObject("StoryPreviewCapture").AddComponent<StoryPreviewCapture>();
        capture.output = args[index + 1]; DontDestroyOnLoad(capture.gameObject);
    }
    IEnumerator Shot(string name)
    {
        yield return new WaitForSeconds(.2f);
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
        Destroy(texture);
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(output);
        yield return new WaitForSeconds(1);
        yield return Shot("title");
        FindFirstObjectByType<TitleMenu>().OnStartPressed();
        yield return new WaitForSeconds(7);
        yield return Shot("intro");
        yield return new WaitForSeconds(6);
        PaperCloseupUI.Instance.Open(ClueSymbol.Triangle, GameState.Room1Code[(int)ClueSymbol.Triangle]);
        yield return Shot("note"); PaperCloseupUI.Instance.Close();
        GameObject.Find("Story_Tape1").GetComponent<StoryObject>().Interact(FirstPersonController.Instance.gameObject);
        yield return Shot("tape"); StoryExperience.Instance.Close();
        StoryExperience.Instance.Read("FADED STUDENT ID", GameObject.Find("Story_StudentID_L7").GetComponent<StoryObject>().body);
        yield return Shot("id"); StoryExperience.Instance.Close();
        File.WriteAllText(Path.Combine(output, "preview-complete.txt"), "Runtime UI preview only. Interactions opened by a capture harness; not a playthrough.");
        Application.Quit();
    }
}
#endif

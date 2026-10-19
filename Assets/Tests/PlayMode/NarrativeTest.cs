using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;

public class NarrativeTest
{
    static void Capture(string path)
    {
        if (System.Environment.GetEnvironmentVariable("LASTLIGHT_CAPTURE_UI") != "1") return;
        var cameraObject = new GameObject("NarrativePreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(1000, 1000, -1000);
        camera.farClipPlane = 10;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var changed = new System.Collections.Generic.List<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            changed.Add(canvas);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
        }
        var rt = new RenderTexture(1280, 720, 24);
        camera.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        camera.Render(); camera.Render(); camera.Render();
        var old = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = old;
        foreach (var canvas in changed) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
        camera.targetTexture = null;
        Object.Destroy(image); Object.Destroy(rt); Object.Destroy(cameraObject);
    }
    [UnityTest]
    public IEnumerator NotesPreservePuzzleStateAndReopenWithoutDuplicates()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("Title");
        yield return null;
        Assert.AreEqual(NarrativeText.Premise, GameObject.Find("Subtitle").GetComponent<TMP_Text>().text);
        yield return null;
        Capture("../output/narrative_20261018/title.png");
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null;
        yield return null;
        var paper = PaperCloseupUI.Instance;
        paper.Open(ClueSymbol.Triangle, 7);
        yield return null;
        Assert.IsTrue(paper.symbolDigitText.text.Contains("7"));
        Assert.IsTrue(paper.panelRoot.transform.Find("TraceBody").gameObject.activeInHierarchy);
        yield return null;
        Capture("../output/narrative_20261018/note.png");
        paper.Close();
        paper.Open(ClueSymbol.Triangle, 7);
        Assert.AreEqual(1, System.Array.FindAll(paper.panelRoot.GetComponentsInChildren<TMP_Text>(), t => t.name == "TraceBody").Length);
        paper.Close();
        var card = CardAlignmentUI.Instance;
        card.Open();
        yield return null;
        Assert.IsFalse(card.Aligned);
        foreach (var cell in card.matrixCells) Assert.IsFalse(cell.gameObject.activeSelf);
        yield return null;
        Capture("../output/narrative_20261018/card.png");
        card.Rotate(); card.Rotate(); card.TryAttach();
        Assert.IsTrue(card.Aligned);
        foreach (int index in card.holeCellIndex) Assert.IsTrue(card.matrixCells[index].gameObject.activeSelf);
        card.Close();
        Assert.IsFalse(card.IsOpen);
    }
}

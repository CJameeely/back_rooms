using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class UIInitialStateTest
{
    [UnityTest]
    public IEnumerator KeypadAndPaperPanelsAreHiddenByDefault()
    {
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;
        yield return null;

        Assert.IsFalse(KeypadUI.Instance.IsOpen, "KeypadUI should start closed.");
        Assert.IsFalse(PaperCloseupUI.Instance.IsOpen, "PaperCloseupUI should start closed.");
        Debug.Log("[DIAG] Keypad/Paper confirmed closed at start.");

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        Debug.Log($"[DIAG] Canvas renderMode={canvas.renderMode} sortingOrder={canvas.sortingOrder}");

        foreach (var tmp in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool ugui = tmp is TMPro.TextMeshProUGUI;
            string rectInfo = "";
            if (ugui)
            {
                var rt = tmp.rectTransform;
                rectInfo = $" anchoredPos={rt.anchoredPosition} anchorMin={rt.anchorMin} anchorMax={rt.anchorMax} pivot={rt.pivot} sizeDelta={rt.sizeDelta}";
            }
            Debug.Log($"[DIAG] TMP '{tmp.text}' on '{tmp.gameObject.name}' ugui={ugui} activeInHierarchy={tmp.gameObject.activeInHierarchy} worldPos={tmp.transform.position}{rectInfo}");
        }
    }
}

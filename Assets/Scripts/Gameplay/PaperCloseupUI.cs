// @role Full-screen paper-reading overlay for Room 1's Triangle clue.
// @calls FirstPersonController

using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// Full-screen "reading the paper" overlay. E or Esc closes it again.
public class PaperCloseupUI : MonoBehaviour
{
    public static PaperCloseupUI Instance { get; private set; }

    public GameObject panelRoot;
    public TMP_Text symbolDigitText;

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    int _openedFrame;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public void Open(ClueSymbol symbol, int digit)
    {
        if (KeypadUI.Instance != null && KeypadUI.Instance.IsOpen) return;
        _openedFrame = Time.frameCount;
        panelRoot.SetActive(true);
        symbolDigitText.text = $"{CodeManager.SymbolGlyph(symbol)}   {digit}";
        FirstPersonController.Instance.SetInputEnabled(false);
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        FirstPersonController.Instance.SetInputEnabled(true);
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount == _openedFrame || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }
}

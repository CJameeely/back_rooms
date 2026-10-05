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

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public void Open(ClueSymbol symbol, int digit)
    {
        panelRoot.SetActive(true);
        symbolDigitText.text = $"{CodeManager.SymbolGlyph(symbol)}   {digit}";
        FirstPersonController.Instance.SetInputEnabled(false);
    }

    void Close()
    {
        panelRoot.SetActive(false);
        FirstPersonController.Instance.SetInputEnabled(true);
    }

    void Update()
    {
        if (!IsOpen || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }
}

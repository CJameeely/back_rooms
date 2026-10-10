using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// The 4-digit input panel: shows the fixed Circle/Triangle/Square/Bar symbols above 4 slots,
/// takes digits from the 0-9 buttons, Clear resets, Confirm checks against CodeManager. Wrong
/// code -> the panel shakes and clears (no per-digit feedback). Right code -> panel closes and
/// the exit door slides open.
public class KeypadUI : MonoBehaviour
{
    public static KeypadUI Instance { get; private set; }

    public GameObject panelRoot;
    public TMP_Text[] digitSlots = new TMP_Text[4];   // entered value display, left to right
    public TMP_Text[] symbolLabels = new TMP_Text[4]; // fixed Circle/Triangle/Square/Bar glyphs
    public SlideMover door;
    public Vector3 doorOpenLocalPosition;
    public float doorSlideDuration = 1f;

    public bool Solved { get; private set; }
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    readonly List<int> _entered = new List<int>();
    static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
    static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };
    bool _rejecting;
    RectTransform _panelRect;
    Vector2 _panelBasePos;

    void Awake()
    {
        Instance = this;
        _panelRect = panelRoot.GetComponent<RectTransform>();
        if (_panelRect != null) _panelBasePos = _panelRect.anchoredPosition;

        for (int i = 0; i < 4; i++)
            if (symbolLabels[i] != null)
                symbolLabels[i].text = CodeManager.SymbolGlyph((ClueSymbol)i);

        panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (Solved || IsOpen) return;
        if (PaperCloseupUI.Instance != null && PaperCloseupUI.Instance.IsOpen) PaperCloseupUI.Instance.Close();
        panelRoot.SetActive(true);
        _entered.Clear();
        RefreshDisplay();
        FirstPersonController.Instance.SetInputEnabled(false);
    }

    public void Close()
    {
        StopAllCoroutines();
        _rejecting = false;
        if (_panelRect != null) _panelRect.anchoredPosition = _panelBasePos;
        panelRoot.SetActive(false);
        FirstPersonController.Instance.SetInputEnabled(true);
    }

    public void PressDigit(int digit)
    {
        if (!IsOpen || _rejecting || digit < 0 || digit > 9 || _entered.Count >= 4) return;
        _entered.Add(digit);
        RefreshDisplay();
    }

    public void PressClear()
    {
        if (!IsOpen || _rejecting) return;
        _entered.Clear();
        RefreshDisplay();
    }

    public void PressConfirm()
    {
        if (!IsOpen || _rejecting || _entered.Count != 4) return;

        if (CodeManager.Instance.Validate(_entered.ToArray()))
        {
            Solved = true;
            panelRoot.SetActive(false);
            FirstPersonController.Instance.SetInputEnabled(true);
            door.SlideTo(doorOpenLocalPosition, doorSlideDuration);
        }
        else
        {
            StartCoroutine(ShakeThenClear());
        }
    }

    void RefreshDisplay()
    {
        for (int i = 0; i < 4; i++)
            if (digitSlots[i] != null)
                digitSlots[i].text = i < _entered.Count ? _entered[i].ToString() : "_";
    }

    IEnumerator ShakeThenClear()
    {
        _rejecting = true;
        if (_panelRect != null)
        {
            float duration = 0.35f, t = 0f, magnitude = 18f, frequency = 28f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float damp = 1f - (t / duration);
                float x = Mathf.Sin(t * frequency) * magnitude * damp;
                _panelRect.anchoredPosition = _panelBasePos + new Vector2(x, 0f);
                yield return null;
            }
            _panelRect.anchoredPosition = _panelBasePos;
        }
        _entered.Clear();
        RefreshDisplay();
        _rejecting = false;
    }

    void Update()
    {
        if (!IsOpen || Keyboard.current == null) return;
        var kb = Keyboard.current;
        if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
        for (int digit = 0; digit <= 9; digit++)
            if (kb[DigitKeys[digit]].wasPressedThisFrame ||
                kb[NumpadKeys[digit]].wasPressedThisFrame) PressDigit(digit);
        if (kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame) PressClear();
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) PressConfirm();
    }
}

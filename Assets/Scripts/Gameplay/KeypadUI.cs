using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// The 4-digit input panel: shows the fixed Circle/Triangle/Square/Bar symbols above 4 slots
/// (toggle off `showSymbols` for a keypad that shouldn't give the symbol hint away, e.g. Room
/// 2's), takes digits from the 0-9 buttons, Clear resets, Confirm checks the guess against
/// `validatorBehaviour` (cast to IKeypadValidator). Wrong code -> the panel shakes and clears (no
/// per-digit feedback), then docks `wrongGuessPenalty` seconds off `penaltyTimer` if both are set.
/// Right code -> panel closes, the exit door slides open, and `clueTrackerToFill` (if set) fills
/// in any still-blank clue slots.
///
/// Every cross-object hookup here is a plain serializable Component reference rather than a
/// System.Action/interface-typed field deliberately: this scene is authored once at editor time
/// and then saved/reloaded at Play time (including every Retry Room), and Unity's serializer
/// silently drops plain delegates and bare interface fields on that round trip — they'd read back
/// as null the moment the player actually plays the game, not just in the editor session that
/// built them.
///
/// Each door gets its own KeypadUI instance (no longer a hard scene-wide singleton), so several
/// can coexist. `Instance` is kept only as a convenience pointer to whichever one was built most
/// recently, for tests/tools that only ever expect one panel at a time.
public class KeypadUI : MonoBehaviour
{
    public static KeypadUI Instance { get; private set; }

    public GameObject panelRoot;
    public TMP_Text[] digitSlots = new TMP_Text[4];   // entered value display, left to right
    public TMP_Text[] symbolLabels = new TMP_Text[4]; // fixed Circle/Triangle/Square/Bar glyphs
    public bool showSymbols = true;
    public SlideMover door;
    public Vector3 doorOpenLocalPosition;
    public float doorSlideDuration = 1f;

    /// Must implement IKeypadValidator (CodeManager for Room 1; a room-specific validator for
    /// later rooms). A MonoBehaviour reference rather than the interface type so it survives
    /// Unity's scene serialization — see class remarks.
    public MonoBehaviour validatorBehaviour;
    IKeypadValidator Validator => validatorBehaviour as IKeypadValidator;

    public ClueTracker clueTrackerToFill; // filled on solve, if set
    public RoomTimer penaltyTimer;        // penalized on a wrong guess, if set
    public float wrongGuessPenalty = 0f;

    [Tooltip("1-4, or 0 to skip. If GameState already has this room marked solved when the scene loads (e.g. Retry Room after a LATER room failed), the door snaps open immediately instead of making the player re-solve an already-passed room.")]
    public int roomNumberForFastForward;

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
        {
            if (symbolLabels[i] == null) continue;
            symbolLabels[i].text = CodeManager.SymbolGlyph((ClueSymbol)i);
            symbolLabels[i].gameObject.SetActive(showSymbols);
        }

        panelRoot.SetActive(false);
    }

    void Start()
    {
        if (roomNumberForFastForward > 0 && GameState.IsRoomSolved(roomNumberForFastForward))
        {
            Solved = true;
            if (door != null) door.transform.localPosition = doorOpenLocalPosition;
        }
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
        if (!IsOpen || _rejecting || _entered.Count != 4 || Validator == null) return;

        if (Validator.Validate(_entered.ToArray()))
        {
            Solved = true;
            panelRoot.SetActive(false);
            FirstPersonController.Instance.SetInputEnabled(true);
            door.SlideTo(doorOpenLocalPosition, doorSlideDuration);
            if (clueTrackerToFill != null) clueTrackerToFill.FillAllRemaining();
        }
        else
        {
            StartCoroutine(ShakeThenClear());
            if (penaltyTimer != null && wrongGuessPenalty > 0f) penaltyTimer.Penalty(wrongGuessPenalty);
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

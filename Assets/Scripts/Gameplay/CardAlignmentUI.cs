using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// Full-screen overlay for Room 2's calibration card: a 4x4 digit grid, fully hidden until the
/// overlaid card is rotated (R, 90 degrees per press) to the one orientation where its notch
/// lines up with the frame's, then pressed on (E). A correct press reveals the 4 "hole" cells
/// (the rest stay hidden — there's no card model to visually cover them, so "hidden" stands in for
/// "covered by the card"); a wrong one bounces/rejects with no digits shown. Once solved the
/// alignment is remembered, so reopening the panel just shows the already-revealed result
/// ("退出界面后卡片留在屏幕上") instead of making the player redo the rotation.
public class CardAlignmentUI : MonoBehaviour
{
    public static CardAlignmentUI Instance { get; private set; }

    public GameObject panelRoot;
    public TMP_Text[] matrixCells = new TMP_Text[16]; // row-major, row*4+col
    public RectTransform cardRect;
    public TMP_Text statusText;

    [Tooltip("Which of the 16 cells each of the 4 holes sits over, in pinhole-number order (1-4).")]
    public int[] holeCellIndex = new int[4];
    [Tooltip("0-3: how many 90-degree steps away from aligned the card starts.")]
    public int startRotationSteps = 1;

    public bool Aligned { get; private set; }
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    int _rotationSteps;
    bool _busy;

    void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);
        PopulateMatrix();
    }

    /// The matrix's actual digits only mean anything once a real game is in progress
    /// (GameState.Room2Code), so they're filled in here at runtime rather than baked in by the
    /// editor generator — Room2GameplayBuilder only lays out 16 empty cells. All 16 start hidden
    /// (standing in for "covered by the card"); RevealHoles() only ever un-hides the 4 hole cells.
    void PopulateMatrix()
    {
        GameState.EnsureStarted();
        var holeSet = new System.Collections.Generic.HashSet<int>(holeCellIndex);
        for (int i = 0; i < matrixCells.Length; i++)
        {
            if (matrixCells[i] == null) continue;
            int digit = holeSet.Contains(i) ? GameState.Room2Code[System.Array.IndexOf(holeCellIndex, i)] : Random.Range(0, 10);
            matrixCells[i].text = digit.ToString();
            matrixCells[i].gameObject.SetActive(false);
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        panelRoot.SetActive(true);
        NarrativeText.Calibration(panelRoot);
        _rotationSteps = Aligned ? 0 : startRotationSteps;
        ApplyRotationVisual();
        FirstPersonController.Instance.SetInputEnabled(false);
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        FirstPersonController.Instance.SetInputEnabled(true);
    }

    public void Rotate()
    {
        if (Aligned || _busy) return;
        _rotationSteps = (_rotationSteps + 1) % 4;
        ApplyRotationVisual();
    }

    public void TryAttach()
    {
        if (Aligned || _busy) return;

        if (_rotationSteps == 0)
        {
            Aligned = true;
            RevealHoles();
            if (Inventory.Instance != null) Inventory.Instance.Remove("CalibrationCard");
            if (statusText != null) statusText.text = "Aligned.";
        }
        else
        {
            StartCoroutine(RejectBounce());
        }
    }

    void RevealHoles()
    {
        for (int i = 0; i < holeCellIndex.Length; i++)
        {
            int cell = holeCellIndex[i];
            if (cell >= 0 && cell < matrixCells.Length && matrixCells[cell] != null)
                matrixCells[cell].gameObject.SetActive(true);
        }
    }

    void ApplyRotationVisual()
    {
        if (cardRect != null) cardRect.localRotation = Quaternion.Euler(0, 0, -90f * _rotationSteps);
        if (statusText != null) statusText.text = "";
    }

    IEnumerator RejectBounce()
    {
        _busy = true;
        if (statusText != null) statusText.text = "Notch not aligned.";
        if (cardRect != null)
        {
            Vector3 basePos = cardRect.anchoredPosition3D;
            float t = 0f, duration = 0.25f, magnitude = 14f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(t / duration * Mathf.PI);
                cardRect.anchoredPosition3D = basePos + new Vector3(0, -k * magnitude, 0);
                yield return null;
            }
            cardRect.anchoredPosition3D = basePos;
        }
        _busy = false;
    }

    void Update()
    {
        if (!IsOpen || Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (Keyboard.current.eKey.wasPressedThisFrame) TryAttach();
        if (Keyboard.current.rKey.wasPressedThisFrame) Rotate();
    }
}

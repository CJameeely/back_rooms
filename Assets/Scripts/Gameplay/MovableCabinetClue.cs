using UnityEngine;

/// Clue 1 (Circle): the displaced cabinet. On E, slides back to its original spot (the clean
/// patch), exposing the wall text that was hidden behind it while it sat in the corner.
[RequireComponent(typeof(SlideMover))]
public class MovableCabinetClue : MonoBehaviour, IInteractable
{
    public Vector3 restingLocalPosition;
    public float slideDuration = 1f;
    [Tooltip("Wall text this reveals (SetActive(true)) once the cabinet finishes sliding away.")]
    public GameObject revealTarget;

    SlideMover _mover;
    bool _moved;

    public string InteractionPrompt => _moved ? null : "Move cabinet";
    public bool CanInteract => !_moved;

    void Awake() => _mover = GetComponent<SlideMover>();

    public void Interact(GameObject interactor)
    {
        if (_moved) return;
        _moved = true;
        _mover.SlideTo(restingLocalPosition, slideDuration, () =>
        {
            if (revealTarget != null) revealTarget.SetActive(true);
        });
    }
}

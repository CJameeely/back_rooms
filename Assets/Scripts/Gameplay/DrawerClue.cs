// @role Room 1 Clue 2 setup (Triangle) — opens the drawer, reveals the paper inside.
// @calls SlideMover
// @tune slideDuration:float:0.2-1.5:drawer slide time (s)

using UnityEngine;

/// Clue 2 setup (Triangle): the drawer front. On E, slides open and reveals the paper sitting
/// inside it (the paper itself is PaperCloseupClue, a separate IInteractable).
[RequireComponent(typeof(SlideMover))]
public class DrawerClue : MonoBehaviour, IInteractable
{
    public Vector3 openLocalPosition;
    public float slideDuration = 0.6f;
    public GameObject paperObject;

    SlideMover _mover;
    bool _opened;

    public string InteractionPrompt => _opened ? null : "Open drawer";
    public bool CanInteract => !_opened;
    public string BlockedReason => null;

    void Awake() => _mover = GetComponent<SlideMover>();

    public void Interact(GameObject interactor)
    {
        if (_opened) return;
        _opened = true;
        _mover.SlideTo(openLocalPosition, slideDuration, () =>
        {
            if (paperObject != null) paperObject.SetActive(true);
        });
    }
}

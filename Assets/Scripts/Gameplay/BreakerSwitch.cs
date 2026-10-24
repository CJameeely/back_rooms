// @role Room 3 — one small up/down breaker toggle, no feedback on correctness.

using UnityEngine;

/// One of the 4 small breakers: E flips it between up and down, no other feedback (per spec — the
/// player has to read the nameplate's arrows, not watch for a hint here).
public class BreakerSwitch : MonoBehaviour, IInteractable
{
    public Transform leverVisual;
    public Vector3 downEuler = new Vector3(20f, 0, 0);
    public Vector3 upEuler = new Vector3(-20f, 0, 0);

    public bool IsUp { get; private set; }

    public string InteractionPrompt => "Flip breaker";
    public bool CanInteract => true;
    public string BlockedReason => null;

    public void Interact(GameObject interactor) => SetUp(!IsUp);

    public void SetUp(bool up)
    {
        IsUp = up;
        if (leverVisual != null) leverVisual.localRotation = Quaternion.Euler(up ? upEuler : downEuler);
    }
}

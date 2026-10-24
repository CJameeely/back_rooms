// @role Room 2 stool's pickup trigger.
// @calls StoolCarrier

using UnityEngine;

/// Lives on the physical stool object. E picks it up via the room's StoolCarrier. Once carrying,
/// this object is inactive (StoolCarrier hides it), so there's nothing to look at/re-trigger
/// pickup on until it's placed down again.
public class StoolPickupTarget : MonoBehaviour, IInteractable
{
    public StoolCarrier carrier;

    public string InteractionPrompt => "Pick up stool";
    public bool CanInteract => carrier != null && !carrier.IsCarrying;
    public string BlockedReason => null;

    public void Interact(GameObject interactor) => carrier.PickUp();
}

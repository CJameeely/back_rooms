// @role Generic world pickup — E adds it to the inventory and hides it.
// @calls Inventory

using UnityEngine;

/// Generic "E to pick up, goes into inventory, disappears from the world" object. Reused across
/// rooms for anything that's just a carried item rather than a bespoke mechanic (Room 2's
/// calibration card, Room 3's lamp parts, ...).
public class InventoryPickup : MonoBehaviour, IInteractable
{
    public InventoryItem item;
    public string prompt = "Pick up";

    public string InteractionPrompt => prompt;
    public bool CanInteract => true;
    public string BlockedReason => null;

    public void Interact(GameObject interactor)
    {
        if (Inventory.Instance != null && Inventory.Instance.TryAdd(item))
            gameObject.SetActive(false);
    }
}

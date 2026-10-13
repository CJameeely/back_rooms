using UnityEngine;

/// The wall-mounted matrix screen. Needs the calibration card in hand to open — once the card has
/// been successfully aligned (and consumed), the screen stays usable so the player can come back
/// and re-read the revealed digits.
public class DataMatrixScreen : MonoBehaviour, IInteractable
{
    public CardAlignmentUI ui;
    public string requiredItemId = "CalibrationCard";

    public string InteractionPrompt => "Align card";
    public bool CanInteract => ui != null && (ui.Aligned || (Inventory.Instance != null && Inventory.Instance.HasItem(requiredItemId)));
    public string BlockedReason => CanInteract ? null : "You need the calibration card";

    public void Interact(GameObject interactor) => ui.Open();
}

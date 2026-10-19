using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// Raycasts forward from the player camera every frame, out to `range`. If it hits an
/// IInteractable, shows its prompt under the crosshair and calls Interact() on E. Independently,
/// if it hits an IClueSource that is currently readable, reports it to ClueTracker — clue
/// "sightings" don't require pressing E, just looking.
public class PlayerInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public TMP_Text promptText;
    public float range = 2f;

    IInteractable _current;

    void Update()
    {
        if (playerCamera == null) return;
        if (FirstPersonController.Instance != null && !FirstPersonController.Instance.InputEnabled)
        {
            if (promptText != null) promptText.text = "";
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide);

        bool carryingStool = StoolCarrier.Instance != null && StoolCarrier.Instance.IsCarrying;
        if (carryingStool) StoolCarrier.Instance.RefreshPlacement();
        _current = hitSomething ? hit.collider.GetComponentInParent<IInteractable>() : null;

        if (promptText != null)
        {
            // Carrying the stool overrides whatever's under the crosshair: E always tries to
            // place it, and being near a doorway while carrying always warns about it, regardless
            // of what the player happens to be looking at.
            if (carryingStool && StoolCarrier.Instance.NearBlockedDoor)
                promptText.text = "Put the stool down first";
            else if (carryingStool)
                promptText.text = StoolCarrier.Instance.PlacementPrompt;
            else if (_current != null && _current.CanInteract && !string.IsNullOrEmpty(_current.InteractionPrompt))
                promptText.text = $"E   {_current.InteractionPrompt}";
            else if (_current != null && !_current.CanInteract && !string.IsNullOrEmpty(_current.BlockedReason))
                promptText.text = _current.BlockedReason;
            else
                promptText.text = "";
        }

        if (hitSomething)
        {
            IClueSource clue = hit.collider.GetComponentInParent<IClueSource>();
            if (clue != null && clue.IsReadable && ClueTracker.Instance != null)
                ClueTracker.Instance.MarkSeen(clue.Symbol, clue.Digit);
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (carryingStool) StoolCarrier.Instance.TryPlace();
                else if (_current != null && _current.CanInteract) _current.Interact(gameObject);
            }

            if (Keyboard.current.tabKey.wasPressedThisFrame && ObjectiveToast.Instance != null)
                ObjectiveToast.Instance.ReplayCurrent();
        }
    }
}

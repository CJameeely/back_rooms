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

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide);

        _current = hitSomething ? hit.collider.GetComponentInParent<IInteractable>() : null;

        if (promptText != null)
        {
            bool showPrompt = _current != null && _current.CanInteract && !string.IsNullOrEmpty(_current.InteractionPrompt);
            promptText.text = showPrompt ? $"E   {_current.InteractionPrompt}" : "";
        }

        if (hitSomething)
        {
            IClueSource clue = hit.collider.GetComponentInParent<IClueSource>();
            if (clue != null && clue.IsReadable && ClueTracker.Instance != null)
                ClueTracker.Instance.MarkSeen(clue.Symbol, clue.Digit);
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame &&
            _current != null && _current.CanInteract)
        {
            _current.Interact(gameObject);
        }
    }
}

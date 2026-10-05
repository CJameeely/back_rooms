using UnityEngine;

/// Anything the player can point at within range and press E on.
public interface IInteractable
{
    /// Shown next to the crosshair, e.g. "Open drawer". Null/empty hides the prompt.
    string InteractionPrompt { get; }

    /// Whether this object currently responds to E (e.g. false once a drawer is already open).
    bool CanInteract { get; }

    void Interact(GameObject interactor);
}

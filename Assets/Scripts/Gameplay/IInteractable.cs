using UnityEngine;

/// Anything the player can point at within range and press E on.
public interface IInteractable
{
    /// Shown next to the crosshair, e.g. "Open drawer". Null/empty hides the prompt.
    string InteractionPrompt { get; }

    /// Whether this object currently responds to E (e.g. false once a drawer is already open).
    bool CanInteract { get; }

    /// Shown under the crosshair INSTEAD of the prompt when CanInteract is false and this isn't
    /// null/empty, e.g. "Out of reach" for the Room 2 light fixture when the player isn't standing
    /// on the stool yet. An interactable with nothing special to say (e.g. "already used, no
    /// further prompt needed") just returns null here, same as before this field existed.
    string BlockedReason { get; }

    void Interact(GameObject interactor);
}

using UnityEngine;

/// The world-space keypad panel. E opens the input UI (KeypadUI is a scene-wide singleton).
public class KeypadController : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => KeypadUI.Instance != null && KeypadUI.Instance.Solved ? null : "Use keypad";
    public bool CanInteract => KeypadUI.Instance != null && !KeypadUI.Instance.Solved;

    public void Interact(GameObject interactor) => KeypadUI.Instance.Open();
}

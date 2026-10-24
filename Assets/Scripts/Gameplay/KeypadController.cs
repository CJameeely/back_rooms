// @role World-space keypad prop — E opens its KeypadUI panel.
// @calls KeypadUI

using UnityEngine;

/// The world-space keypad panel. E opens its input UI. `ui` is assigned at build time to the
/// specific KeypadUI panel this keypad owns — each door gets its own KeypadUI instance now (Room
/// 1 and Room 2 both have one), so this no longer reaches for a global singleton.
public class KeypadController : MonoBehaviour, IInteractable
{
    public KeypadUI ui;

    public string InteractionPrompt => ui != null && ui.Solved ? null : "Use keypad";
    public bool CanInteract => ui != null && !ui.Solved;
    public string BlockedReason => null;

    public void Interact(GameObject interactor) => ui.Open();
}

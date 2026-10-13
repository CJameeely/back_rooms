using UnityEngine;

/// Tiny per-button helper so each of the 0-9 buttons can bind a parameterless UnityEvent
/// (Button.onClick takes no arguments) back to KeypadUI.PressDigit(int). Bound to a specific
/// `owner` panel at build time rather than a static singleton, since multiple keypads (Room 1,
/// Room 2, ...) now coexist in the same scene.
public class KeypadDigitButton : MonoBehaviour
{
    public KeypadUI owner;
    public int digit;
    public void Press() => owner.PressDigit(digit);
}

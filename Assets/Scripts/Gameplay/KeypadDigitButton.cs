using UnityEngine;

/// Tiny per-button helper so each of the 0-9 buttons can bind a parameterless UnityEvent
/// (Button.onClick takes no arguments) back to KeypadUI.PressDigit(int).
public class KeypadDigitButton : MonoBehaviour
{
    public int digit;
    public void Press() => KeypadUI.Instance.PressDigit(digit);
}

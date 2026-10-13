using UnityEngine;

/// Room 2's exit keypad checks guesses against this instead of CodeManager/Room 1's code.
public class Room2CodeValidator : MonoBehaviour, IKeypadValidator
{
    public bool Validate(int[] guess)
    {
        if (guess == null || guess.Length != 4) return false;
        for (int i = 0; i < 4; i++)
            if (guess[i] != GameState.Room2Code[i]) return false;
        return true;
    }
}

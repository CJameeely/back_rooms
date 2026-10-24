// @role Room 1's 4-digit code source — reads GameState, validates keypad guesses.
// @calls GameState

using UnityEngine;

public enum ClueSymbol { Circle, Triangle, Square, Bar }

/// Anything that can report a clue digit and whether it's currently legible (e.g. the light
/// switch clue is only readable while its lamp is off).
public interface IClueSource
{
    ClueSymbol Symbol { get; }
    int Digit { get; }
    bool IsReadable { get; }
}

/// Reads Room 1's 4-digit code (generated once per game by GameState.BeginNewGame(), so a Retry
/// Room reload never changes it) and validates keypad guesses against it. Digit i corresponds to
/// symbol i (Circle=0, Triangle=1, Square=2, Bar=3); clue scripts pull their own digit from here
/// in their own Start(), so nothing needs to push data around.
public class CodeManager : MonoBehaviour, IKeypadValidator
{
    public static CodeManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        GameState.EnsureStarted();
    }

    public int GetDigit(ClueSymbol symbol) => GameState.Room1Code[(int)symbol];

    public bool Validate(int[] guess)
    {
        if (guess == null || guess.Length != 4) return false;
        for (int i = 0; i < 4; i++)
            if (guess[i] != GameState.Room1Code[i]) return false;
        return true;
    }

    public static string SymbolGlyph(ClueSymbol s) => s switch
    {
        ClueSymbol.Circle => "●",   // ●
        ClueSymbol.Triangle => "▲", // ▲
        ClueSymbol.Square => "■",   // ■
        ClueSymbol.Bar => "|",
        _ => "?"
    };
}

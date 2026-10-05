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

/// Generates the round's 4-digit code once at scene start and validates keypad guesses.
/// Digit i corresponds to symbol i (Circle=0, Triangle=1, Square=2, Bar=3); clue scripts pull
/// their own digit from here in their own Start(), so nothing needs to push data around.
public class CodeManager : MonoBehaviour
{
    public static CodeManager Instance { get; private set; }

    int[] code = new int[4];

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < 4; i++)
            code[i] = Random.Range(0, 10);
    }

    public int GetDigit(ClueSymbol symbol) => code[(int)symbol];

    public bool Validate(int[] guess)
    {
        if (guess == null || guess.Length != 4) return false;
        for (int i = 0; i < 4; i++)
            if (guess[i] != code[i]) return false;
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

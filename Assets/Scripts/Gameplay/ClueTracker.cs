// @role Bottom-left HUD — fills in each Room 1 clue digit as the player discovers it.
// @calls GameState

using TMPro;
using UnityEngine;

/// Bottom-left HUD: 4 slots in fixed Circle/Triangle/Square/Bar order. Each starts showing the
/// hollow symbol and flips to "<symbol> <digit>" the first time that clue is seen. Stays on
/// screen for the rest of the game (Room 4 needs to read it), and mirrors every reveal into
/// GameState.Room1ClueRecord so a scene reload (Retry Room on a later room, after Room 1 is
/// already behind the player) can restore it instead of going blank.
public class ClueTracker : MonoBehaviour
{
    public static ClueTracker Instance { get; private set; }

    public TMP_Text[] slots = new TMP_Text[4]; // indexed by ClueSymbol
    [Tooltip("Set at build time so FillAllRemaining() needs no argument — a plain delegate field here wouldn't survive scene save/reload.")]
    public CodeManager room1Code;

    readonly bool[] _seen = new bool[4];

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null)
                slots[i].text = CodeManager.SymbolGlyph((ClueSymbol)i);

        // Restore from a previous visit this session (e.g. reloaded because a later room failed).
        for (int i = 0; i < 4; i++)
        {
            int digit = GameState.Room1ClueRecord[i];
            if (digit >= 0) MarkSeen((ClueSymbol)i, digit);
        }
    }

    public void MarkSeen(ClueSymbol symbol, int digit)
    {
        int i = (int)symbol;
        GameState.RecordClueDigit(symbol, digit);
        if (_seen[i]) return;
        _seen[i] = true;
        if (slots[i] != null)
            slots[i].text = $"{CodeManager.SymbolGlyph(symbol)} {digit}";
    }

    /// Called once Room 1's door opens: any clue the player never actually looked at still gets
    /// filled in, per spec ("房间 1 的门打开时，如果记录还有空格，自动全部填满").
    public void FillAllRemaining()
    {
        if (room1Code == null) return;
        for (int i = 0; i < 4; i++)
            if (!_seen[i])
                MarkSeen((ClueSymbol)i, room1Code.GetDigit((ClueSymbol)i));
    }
}

using TMPro;
using UnityEngine;

/// Bottom-left HUD: 4 slots in fixed Circle/Triangle/Square/Bar order. Each starts showing the
/// hollow symbol and flips to "<symbol> <digit>" the first time that clue is seen.
public class ClueTracker : MonoBehaviour
{
    public static ClueTracker Instance { get; private set; }

    public TMP_Text[] slots = new TMP_Text[4]; // indexed by ClueSymbol

    readonly bool[] _seen = new bool[4];

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null)
                slots[i].text = CodeManager.SymbolGlyph((ClueSymbol)i);
    }

    public void MarkSeen(ClueSymbol symbol, int digit)
    {
        int i = (int)symbol;
        if (_seen[i]) return;
        _seen[i] = true;
        if (slots[i] != null)
            slots[i].text = $"{CodeManager.SymbolGlyph(symbol)} {digit}";
    }
}

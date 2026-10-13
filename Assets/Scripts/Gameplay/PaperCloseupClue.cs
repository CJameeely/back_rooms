using UnityEngine;

/// Clue 2 (Triangle): the paper itself. E opens the full-screen close-up UI; the clue only
/// counts as "seen" once that close-up has been opened (looking at the tiny paper in the world
/// isn't legible enough on its own, per spec).
public class PaperCloseupClue : MonoBehaviour, IInteractable
{
    public ClueSymbol symbol = ClueSymbol.Triangle;

    int _digit;
    bool _digitReady;
    bool _everOpened;

    public string InteractionPrompt => "Read note";
    public bool CanInteract => true;
    public string BlockedReason => null;

    void EnsureDigit()
    {
        if (_digitReady) return;
        _digit = CodeManager.Instance.GetDigit(symbol);
        _digitReady = true;
    }

    public void Interact(GameObject interactor)
    {
        EnsureDigit();
        PaperCloseupUI.Instance.Open(symbol, _digit);
        if (!_everOpened)
        {
            _everOpened = true;
            ClueTracker.Instance.MarkSeen(symbol, _digit);
        }
    }
}

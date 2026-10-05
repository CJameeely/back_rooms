using UnityEngine;
using TMPro;

/// A passive clue: a world-space label that counts as "seen" simply by being looked at within
/// range (no E press needed). Pulls its digit from CodeManager on Start and writes it into the
/// label text as "<symbol>  <digit>". Needs a Collider on the same GameObject to be raycastable
/// (added automatically if missing).
public class ClueMarker : MonoBehaviour, IClueSource
{
    public ClueSymbol symbol;
    public TMP_Text label;

    public ClueSymbol Symbol => symbol;
    public int Digit { get; private set; }
    public virtual bool IsReadable => true;

    protected virtual void Awake()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.6f, 0.6f, 0.2f);
        }
    }

    protected virtual void Start()
    {
        Digit = CodeManager.Instance.GetDigit(symbol);
        if (label != null)
            label.text = $"{CodeManager.SymbolGlyph(symbol)}  {Digit}";
    }
}

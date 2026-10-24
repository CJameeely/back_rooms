// @role Room 3+ handheld spotlight, toggled with F — follows wherever the player looks.

using UnityEngine;

/// The handheld inspection lamp Room 3's cradle assembles. Once acquired it's permanently
/// available (not a consumable inventory item — it's needed again in Room 4), toggled with F. A
/// Spot Light that follows wherever the player looks, used both to see in the dark and, per Room
/// 3's nameplate and Room 4's mirror puzzle, to graze light across specific surfaces.
public class InspectionLamp : MonoBehaviour
{
    public static InspectionLamp Instance { get; private set; }

    public Light spotLight;
    public bool Acquired { get; private set; }
    public bool IsOn { get; private set; }

    void Awake()
    {
        Instance = this;
        if (spotLight != null) spotLight.enabled = false;
    }

    public void Acquire()
    {
        Acquired = true;
    }

    public void Toggle()
    {
        if (!Acquired) return;
        IsOn = !IsOn;
        if (spotLight != null) spotLight.enabled = IsOn;
    }
}

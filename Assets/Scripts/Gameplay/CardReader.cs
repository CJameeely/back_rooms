using System.Collections;
using UnityEngine;

/// Mounted on the sample cabinet. E with the access card in hand: consumes it, flips the
/// indicator green, slides the cabinet door open. E without it: red flash + "Access denied",
/// nothing consumed, cabinet stays locked.
public class CardReader : MonoBehaviour, IInteractable
{
    public string requiredItemId = "AccessCard";
    public SlideMover cabinetDoor;
    public Vector3 cabinetOpenLocalPosition;
    public float slideDuration = 0.8f;
    public Renderer indicatorRenderer;
    public Color deniedColor = new Color(0.9f, 0.15f, 0.1f);
    public Color grantedColor = new Color(0.2f, 0.85f, 0.3f);
    [Tooltip("Revealed (SetActive(true)) once the cabinet unlocks, e.g. the calibration card sitting inside.")]
    public GameObject revealOnUnlock;

    bool _unlocked;

    public string InteractionPrompt => _unlocked ? null : "Scan card";
    public bool CanInteract => !_unlocked;
    public string BlockedReason => null;

    void Start()
    {
        if (indicatorRenderer != null) indicatorRenderer.material.color = deniedColor;
    }

    public void Interact(GameObject interactor)
    {
        if (_unlocked) return;

        if (Inventory.Instance != null && Inventory.Instance.HasItem(requiredItemId))
        {
            _unlocked = true;
            Inventory.Instance.Remove(requiredItemId);
            if (indicatorRenderer != null) indicatorRenderer.material.color = grantedColor;
            cabinetDoor.SlideTo(cabinetOpenLocalPosition, slideDuration);
            if (revealOnUnlock != null) revealOnUnlock.SetActive(true);
            ObjectiveToast.Instance?.Show("Cabinet unlocked. Take the calibration card inside, then examine the number display.");
        }
        else
        {
            if (Inventory.Instance != null) Inventory.Instance.ShowMessage("Access denied");
            StartCoroutine(DenyFlash());
        }
    }

    IEnumerator DenyFlash()
    {
        if (indicatorRenderer == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            indicatorRenderer.material.color = Color.black;
            yield return new WaitForSeconds(0.1f);
            indicatorRenderer.material.color = deniedColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
}

// @role Room 3 — assembles the inspection lamp from two carried parts.
// @calls Inventory, InspectionLamp

using System.Collections;
using UnityEngine;

/// Room 3's equipment cradle: a dusty outline waiting for two parts (base + head). E with the
/// matching part in hand slots it in; once both are in, it auto-assembles (flash + message) and
/// the slotted parts can't be pulled back out individually. E on the assembled lamp hands it to
/// the player (InspectionLamp.Acquire()) and the cradle empties out visually.
public class InspectionLampCradle : MonoBehaviour, IInteractable
{
    public const string BaseItemId = "LampBase";
    public const string HeadItemId = "LampHead";

    public GameObject outlineVisual;
    public GameObject baseOnlyVisual;
    public GameObject headOnlyVisual;
    public GameObject assembledVisual;
    public Renderer assembledFlashRenderer;

    bool _hasBase, _hasHead, _assembled, _takenByPlayer;

    public bool CanInteract
    {
        get
        {
            if (_takenByPlayer) return false;
            if (_assembled) return true;
            bool canPlaceBase = !_hasBase && Inventory.Instance != null && Inventory.Instance.HasItem(BaseItemId);
            bool canPlaceHead = !_hasHead && Inventory.Instance != null && Inventory.Instance.HasItem(HeadItemId);
            return canPlaceBase || canPlaceHead;
        }
    }

    public string InteractionPrompt
    {
        get
        {
            if (_takenByPlayer) return null;
            if (_assembled) return "Take inspection lamp";
            return CanInteract ? "Place part" : null;
        }
    }

    public string BlockedReason
    {
        get
        {
            if (_takenByPlayer || _assembled) return null;
            return (!_hasBase && !_hasHead) ? "Empty cradle" : null;
        }
    }

    void Start() => UpdateVisual();

    public void Interact(GameObject interactor)
    {
        if (_takenByPlayer) return;

        if (_assembled)
        {
            _takenByPlayer = true;
            if (InspectionLamp.Instance != null) InspectionLamp.Instance.Acquire();
            UpdateVisual();
            return;
        }

        if (!_hasBase && Inventory.Instance != null && Inventory.Instance.HasItem(BaseItemId))
        {
            Inventory.Instance.Remove(BaseItemId);
            _hasBase = true;
        }
        else if (!_hasHead && Inventory.Instance != null && Inventory.Instance.HasItem(HeadItemId))
        {
            Inventory.Instance.Remove(HeadItemId);
            _hasHead = true;
        }

        if (_hasBase && _hasHead && !_assembled)
        {
            _assembled = true;
            Inventory.Instance?.ShowMessage("Inspection lamp assembled");
            StartCoroutine(FlashOnce());
        }

        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (outlineVisual != null) outlineVisual.SetActive(!_assembled);
        if (baseOnlyVisual != null) baseOnlyVisual.SetActive(_hasBase && !_assembled);
        if (headOnlyVisual != null) headOnlyVisual.SetActive(_hasHead && !_assembled);
        if (assembledVisual != null) assembledVisual.SetActive(_assembled && !_takenByPlayer);
    }

    IEnumerator FlashOnce()
    {
        if (assembledFlashRenderer == null) yield break;
        Color baseColor = assembledFlashRenderer.material.color;
        assembledFlashRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        assembledFlashRenderer.material.color = baseColor;
    }
}

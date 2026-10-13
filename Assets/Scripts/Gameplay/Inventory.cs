using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// A held item. Whitebox-styled: no icon art, just a flat color swatch + short label, matching
/// the rest of the level's "colored primitives" look.
[System.Serializable]
public struct InventoryItem
{
    public string id;
    public string displayName;
    public Color iconColor;
    public InventoryItem(string id, string displayName, Color iconColor)
    {
        this.id = id; this.displayName = displayName; this.iconColor = iconColor;
    }
}

/// Bottom-center held-item row, max 4 slots. The player never manually selects a slot — whatever
/// puzzle object accepts a given item id just checks HasItem(id) itself and calls Remove(id) as
/// part of its own Interact(), so "auto-use on E" needs no special wiring here.
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }
    public const int MaxSlots = 4;

    public Image[] slotIcons = new Image[MaxSlots];
    public TMP_Text pickupToastText;
    public float pickupToastDuration = 1.5f;

    readonly List<InventoryItem> _items = new List<InventoryItem>();
    Coroutine _toastRoutine;

    void Awake()
    {
        Instance = this;
        if (pickupToastText != null) pickupToastText.alpha = 0f;
        RefreshUI();
    }

    public bool HasItem(string id) => _items.Exists(i => i.id == id);

    public bool TryAdd(InventoryItem item)
    {
        if (_items.Count >= MaxSlots || HasItem(item.id)) return false;
        _items.Add(item);
        RefreshUI();
        ShowMessage($"Picked up: {item.displayName}");
        return true;
    }

    public void Remove(string id)
    {
        _items.RemoveAll(i => i.id == id);
        RefreshUI();
    }

    public void Clear()
    {
        _items.Clear();
        RefreshUI();
    }

    public List<string> SnapshotIds() => _items.ConvertAll(i => i.id);

    void RefreshUI()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] == null) continue;
            bool filled = i < _items.Count;
            slotIcons[i].gameObject.SetActive(filled);
            if (filled) slotIcons[i].color = _items[i].iconColor;
        }
    }

    /// Reused for any brief "the player should notice this" line, not just pickups — e.g. a
    /// card reader's "Access denied".
    public void ShowMessage(string text)
    {
        if (pickupToastText == null) return;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastCo(text));
    }

    IEnumerator ToastCo(string text)
    {
        pickupToastText.text = text;
        pickupToastText.alpha = 1f;
        yield return new WaitForSeconds(pickupToastDuration);
        float t = 0f;
        const float fade = 0.4f;
        while (t < fade)
        {
            t += Time.deltaTime;
            pickupToastText.alpha = Mathf.Lerp(1f, 0f, t / fade);
            yield return null;
        }
        pickupToastText.alpha = 0f;
    }
}

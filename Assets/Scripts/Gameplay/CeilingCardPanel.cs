using System.Collections;
using UnityEngine;

/// The L5 light cover: an access card sits on top of it, out of reach from the floor. Usable only
/// once the player's feet are high enough (standing on the stool) AND close enough horizontally —
/// deliberately checked by actual player height/position rather than just raycast distance, since
/// a raycast from the floor can physically reach the ceiling from across the room.
public class CeilingCardPanel : MonoBehaviour, IInteractable
{
    public float minFootHeight = 0.4f;
    public float maxHorizontalDistance = 1f;
    public Transform cover; // flips open when taken
    public InventoryItem cardItem;

    bool _taken;

    public string InteractionPrompt => _taken ? null : "Open light cover";
    public bool CanInteract => !_taken && IsPlayerCloseEnough();
    public string BlockedReason => _taken ? null : (IsPlayerCloseEnough() ? null : "Out of reach");

    bool IsPlayerCloseEnough()
    {
        var fpc = FirstPersonController.Instance;
        if (fpc == null) return false;
        float footY = fpc.transform.position.y;
        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = new Vector2(fpc.transform.position.x, fpc.transform.position.z);
        return footY >= minFootHeight && Vector2.Distance(a, b) < maxHorizontalDistance;
    }

    public void Interact(GameObject interactor)
    {
        if (_taken) return;
        _taken = true;
        StartCoroutine(FlipOpen());
        if (Inventory.Instance != null) Inventory.Instance.TryAdd(cardItem);
    }

    IEnumerator FlipOpen()
    {
        if (cover == null) yield break;
        Quaternion start = cover.localRotation;
        Quaternion end = start * Quaternion.Euler(-100f, 0, 0);
        float t = 0f, duration = 0.4f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cover.localRotation = Quaternion.Slerp(start, end, t / duration);
            yield return null;
        }
        cover.localRotation = end;
    }
}
